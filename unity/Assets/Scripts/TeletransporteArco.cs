using System.Collections.Generic;
using UnityEngine;

// Teletransporte "como el de Meta": un arco curvo sale de la mano y puedes caer en cualquier parte del piso.
// Con las manos: palma hacia abajo, junta pulgar e índice (pellizco) para apuntar; suelta para ir.
// Con controles: mantén el gatillo para apuntar y suéltalo para ir.
// Las góndolas, paredes y el mostrador bloquean el arco: no se puede caer encima ni adentro de ellos.
public class TeletransporteArco : MonoBehaviour
{
    [Tooltip("Material de la línea (Sprites/Default)")]
    public Material materialLinea;
    [Tooltip("Qué tan lejos llega el arco (velocidad inicial)")]
    public float fuerzaArco = 7f;
    public float anchoLinea = 0.008f;
    public Color colorValido = new Color(0.3f, 0.9f, 1f, 0.9f);
    public Color colorInvalido = new Color(1f, 0.35f, 0.3f, 0.7f);
    [Tooltip("Zona del piso donde se puede caer (x mínimo, x máximo, z mínimo, z máximo)")]
    public Vector4 zonaPermitida = new Vector4(-4.8f, 4.8f, -4.8f, 7.8f);
    [Tooltip("Qué tan suave se mueve el arco (más bajo = más estable)")]
    public float suavidad = 12f;

    class Mano
    {
        public bool esIzquierda;
        public Transform ancla, anclaControl;
        public OVRInput.Controller control;
        public OVRHand hand;
        public OVRSkeleton esqueleto;
        public bool apuntando;
        public Vector3 origenSuave, direccionSuave;
    }

    OVRCameraRig rig;
    Transform cabeza;
    Mano izquierda, derecha;
    LineRenderer arco, aro;
    readonly List<Vector3> puntos = new List<Vector3>();
    bool destinoValido;
    Vector3 destino;
    float bloqueadoHasta;

    void Start()
    {
        rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null)
        {
            enabled = false;
            return;
        }
        cabeza = rig.centerEyeAnchor;
        izquierda = CrearMano(true, rig.leftHandAnchor, rig.leftControllerAnchor, OVRInput.Controller.LTouch);
        derecha = CrearMano(false, rig.rightHandAnchor, rig.rightControllerAnchor, OVRInput.Controller.RTouch);
        arco = CrearLinea("Arco", anchoLinea);
        aro = CrearLinea("Aro", anchoLinea * 1.5f);
        aro.loop = true;
        aro.positionCount = 24;
    }

    Mano CrearMano(bool esIzquierda, Transform ancla, Transform anclaControl, OVRInput.Controller control)
    {
        return new Mano
        {
            esIzquierda = esIzquierda, ancla = ancla, anclaControl = anclaControl, control = control,
            hand = ManosUtil.BuscarEnAncla<OVRHand>(ancla),
            esqueleto = ManosUtil.BuscarEnAncla<OVRSkeleton>(ancla),
        };
    }

    LineRenderer CrearLinea(string nombre, float ancho)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = materialLinea;
        lr.startWidth = ancho;
        lr.endWidth = ancho;
        lr.useWorldSpace = true;
        lr.enabled = false;
        return lr;
    }

    void OnDisable()
    {
        if (arco != null) arco.enabled = false;
        if (aro != null) aro.enabled = false;
        if (izquierda != null) izquierda.apuntando = false;
        if (derecha != null) derecha.apuntando = false;
    }

    void Update()
    {
        if (izquierda == null)
            return;

        // Solo una mano apunta a la vez.
        Mano activa = izquierda.apuntando ? izquierda : derecha.apuntando ? derecha : null;
        foreach (var m in activa != null ? new[] { activa } : new[] { izquierda, derecha })
        {
            bool estaba = m.apuntando;
            bool gesto = Gesto(m, estaba, out Vector3 origen, out Vector3 direccion);
            bool apunta = gesto && Time.time >= bloqueadoHasta;
            if (apunta)
            {
                float k = 1f - Mathf.Exp(-suavidad * Time.deltaTime);
                if (!estaba)
                {
                    m.origenSuave = origen;
                    m.direccionSuave = direccion;
                }
                m.origenSuave = Vector3.Lerp(m.origenSuave, origen, k);
                m.direccionSuave = Vector3.Slerp(m.direccionSuave, direccion, k).normalized;
                m.apuntando = true;
                Calcular(m.origenSuave, m.direccionSuave);
                Dibujar();
                return;
            }
            if (estaba)
            {
                // Soltó el pellizco (o el gatillo): si el destino es válido, se va allá.
                m.apuntando = false;
                if (destinoValido)
                    Teletransportar(destino);
            }
        }
        arco.enabled = false;
        aro.enabled = false;
    }

    // ¿La mano está haciendo el gesto de apuntar? (palma abajo + pellizco, o gatillo del control)
    bool Gesto(Mano m, bool yaApuntaba, out Vector3 origen, out Vector3 direccion)
    {
        origen = direccion = Vector3.zero;
        if ((OVRInput.GetActiveController() & m.control) != 0 && m.anclaControl != null)
        {
            origen = m.anclaControl.position;
            direccion = m.anclaControl.forward;
            return OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, m.control);
        }
        if (m.hand == null || !m.hand.IsTracked)
            return false;
        if (m.esqueleto == null)
            m.esqueleto = ManosUtil.BuscarEnAncla<OVRSkeleton>(m.ancla);

        var palma = ManosUtil.LeerPalma(m.esqueleto, m.esIzquierda);
        var muneca = ManosUtil.Hueso(m.esqueleto, "WristRoot", "Wrist");
        var nudillo = ManosUtil.Hueso(m.esqueleto, "Middle1", "MiddleProximal");
        if (!palma.valida || muneca == null || nudillo == null)
            return false;

        float pellizco = m.hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        bool pellizcando = yaApuntaba ? pellizco > 0.4f : pellizco > 0.75f;
        bool palmaAbajo = palma.normal.y < (yaApuntaba ? 0f : -0.4f);
        if (!pellizcando || !palmaAbajo)
            return false;

        origen = ManosUtil.PuntoDePellizco(m.esqueleto, m.ancla);
        direccion = ((nudillo.position - muneca.position).normalized + Vector3.up * 0.15f).normalized;
        return true;
    }

    // Simula la caída de una pelota lanzada desde la mano y busca dónde toca.
    void Calcular(Vector3 origen, Vector3 direccion)
    {
        puntos.Clear();
        destinoValido = false;
        Vector3 p = origen;
        Vector3 v = direccion * fuerzaArco;
        const float paso = 0.03f;
        puntos.Add(p);
        for (int i = 0; i < 60; i++)
        {
            Vector3 siguiente = p + v * paso;
            v += Physics.gravity * paso;
            Vector3 tramo = siguiente - p;
            if (Physics.Raycast(p, tramo.normalized, out RaycastHit hit, tramo.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                puntos.Add(hit.point);
                destino = hit.point;
                destinoValido = hit.normal.y > 0.7f && hit.point.y < 0.1f
                                && hit.point.x > zonaPermitida.x && hit.point.x < zonaPermitida.y
                                && hit.point.z > zonaPermitida.z && hit.point.z < zonaPermitida.w;
                return;
            }
            puntos.Add(siguiente);
            p = siguiente;
        }
    }

    void Dibujar()
    {
        Color c = destinoValido ? colorValido : colorInvalido;
        arco.enabled = true;
        arco.positionCount = puntos.Count;
        arco.SetPositions(puntos.ToArray());
        arco.startColor = c;
        arco.endColor = c;

        aro.enabled = destinoValido;
        if (destinoValido)
        {
            for (int i = 0; i < aro.positionCount; i++)
            {
                float a = i * Mathf.PI * 2f / aro.positionCount;
                aro.SetPosition(i, destino + new Vector3(Mathf.Cos(a) * 0.25f, 0.02f, Mathf.Sin(a) * 0.25f));
            }
            aro.startColor = c;
            aro.endColor = c;
        }
    }

    void Teletransportar(Vector3 a)
    {
        Vector3 delta = a - cabeza.position;
        delta.y = 0f;
        rig.transform.position += delta;
        bloqueadoHasta = Time.time + 0.5f;
        arco.enabled = false;
        aro.enabled = false;
    }
}

using UnityEngine;

// Teletransporte a puntos fijos del piso (discos con PuntoTeletransporte).
// Con las manos: señala el disco con el dedo índice (los demás dedos doblados) y mantén el dedo
// sobre el disco un momento: el disco se llena de amarillo y apareces ahí. No hay que pellizcar.
// Con controles (opcional): apunta con el control y mantén, o aprieta el gatillo.
public class TeletransportePorPuntos : MonoBehaviour
{
    [Tooltip("Material de la línea que sale del dedo (Sprites/Default)")]
    public Material materialLinea;
    [Tooltip("Distancia máxima (metros) a la que se puede apuntar")]
    public float alcance = 12f;
    [Tooltip("Segundos señalando un disco para teletransportarte")]
    public float segundosApuntando = 0.6f;
    [Tooltip("Pausa (segundos) después de teletransportarte, para no saltar dos veces seguidas")]
    public float pausaDespues = 0.8f;
    public float anchoLinea = 0.005f;
    [Tooltip("Qué tan suave se mueve la línea (más alto = responde más rápido, más bajo = más estable)")]
    public float suavidad = 12f;
    [Tooltip("Ayuda de puntería: si apuntas a menos de estos grados de un disco, se 'pega' a él")]
    public float anguloIman = 8f;
    public Color colorLinea = new Color(1f, 1f, 1f, 0.5f);
    public Color colorLineaApuntando = new Color(1f, 0.85f, 0.1f, 1f);

    class Mano
    {
        public bool esIzquierda;
        public Transform anclaMano;
        public Transform anclaControl;
        public OVRInput.Controller control;
        public OVRHand hand;
        public OVRSkeleton esqueleto;
        public LineRenderer linea;
        public PuntoTeletransporte apuntado;
        public float tiempo;
        public Vector3 origenSuave, direccionSuave;
        public bool haySuave;
        public float perdido; // segundos desde que el rayo dejó de tocar el disco (pequeño margen antes de reiniciar)
    }

    PuntoTeletransporte[] discos = new PuntoTeletransporte[0];

    OVRCameraRig rig;
    Transform cabeza;
    Mano izquierda, derecha;
    float bloqueadoHasta;

    void Start()
    {
        rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null)
        {
            Debug.LogWarning("TeletransportePorPuntos: no encontré el OVRCameraRig.", this);
            enabled = false;
            return;
        }
        cabeza = rig.centerEyeAnchor;
        discos = FindObjectsByType<PuntoTeletransporte>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        izquierda = CrearMano(true, rig.leftHandAnchor, rig.leftControllerAnchor, OVRInput.Controller.LTouch);
        derecha = CrearMano(false, rig.rightHandAnchor, rig.rightControllerAnchor, OVRInput.Controller.RTouch);
    }

    Mano CrearMano(bool esIzquierda, Transform anclaMano, Transform anclaControl, OVRInput.Controller control)
    {
        var m = new Mano
        {
            esIzquierda = esIzquierda,
            anclaMano = anclaMano,
            anclaControl = anclaControl,
            control = control,
            hand = ManosUtil.BuscarEnAncla<OVRHand>(anclaMano),
            esqueleto = ManosUtil.BuscarEnAncla<OVRSkeleton>(anclaMano),
        };

        var go = new GameObject(esIzquierda ? "LineaIzquierda" : "LineaDerecha");
        go.transform.SetParent(transform, false);
        m.linea = go.AddComponent<LineRenderer>();
        m.linea.material = materialLinea;
        m.linea.positionCount = 2;
        m.linea.startWidth = anchoLinea;
        m.linea.endWidth = anchoLinea * 0.5f;
        m.linea.useWorldSpace = true;
        m.linea.enabled = false;
        return m;
    }

    void OnDisable()
    {
        foreach (var m in new[] { izquierda, derecha })
        {
            if (m == null)
                continue;
            m.linea.enabled = false;
            if (m.apuntado != null)
                m.apuntado.Resaltar(0f);
            m.apuntado = null;
            m.tiempo = 0f;
        }
    }

    void Update()
    {
        if (izquierda == null)
            return;
        bool bloqueado = Time.time < bloqueadoHasta;
        Procesar(izquierda, bloqueado);
        Procesar(derecha, bloqueado);
    }

    void Procesar(Mano m, bool bloqueado)
    {
        if (bloqueado || !ObtenerRayo(m, out Vector3 origen, out Vector3 direccion, out bool gatillo))
        {
            m.linea.enabled = false;
            m.haySuave = false;
            CambiarApuntado(m, null);
            return;
        }

        // Suaviza el rayo para que no tiemble.
        float k = 1f - Mathf.Exp(-suavidad * Time.deltaTime);
        if (!m.haySuave)
        {
            m.origenSuave = origen;
            m.direccionSuave = direccion;
            m.haySuave = true;
        }
        else
        {
            m.origenSuave = Vector3.Lerp(m.origenSuave, origen, k);
            m.direccionSuave = Vector3.Slerp(m.direccionSuave, direccion, k).normalized;
        }
        origen = m.origenSuave;
        direccion = m.direccionSuave;

        PuntoTeletransporte punto = null;
        Vector3 fin = origen + direccion * 1.5f;
        if (Physics.Raycast(origen, direccion, out RaycastHit hit, alcance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            punto = hit.collider.GetComponentInParent<PuntoTeletransporte>();
            fin = hit.point;
        }
        if (punto != null && EstoyEncima(punto))
            punto = null; // el disco donde ya estás parado no cuenta
        if (punto == null)
            punto = Iman(origen, direccion, ref fin); // ayuda de puntería

        // Si el rayo se sale del disco un instante, no se pierde el progreso.
        bool avanzar = true;
        if (punto == null && m.apuntado != null && m.perdido < 0.25f)
        {
            m.perdido += Time.deltaTime;
            punto = m.apuntado;
            avanzar = false;
        }
        else if (punto != null)
        {
            m.perdido = 0f;
        }

        m.linea.enabled = true;
        m.linea.SetPosition(0, origen);
        m.linea.SetPosition(1, fin);
        Color c = punto != null ? colorLineaApuntando : colorLinea;
        m.linea.startColor = c;
        m.linea.endColor = c;

        CambiarApuntado(m, punto);
        if (punto == null)
            return;

        if (avanzar)
            m.tiempo += Time.deltaTime;
        float progreso = m.tiempo / Mathf.Max(0.05f, segundosApuntando);
        punto.Resaltar(progreso);
        if (progreso >= 1f || gatillo)
            Teletransportar(punto);
    }

    bool ObtenerRayo(Mano m, out Vector3 origen, out Vector3 direccion, out bool gatillo)
    {
        origen = direccion = Vector3.zero;
        gatillo = false;

        // Controles (opcional)
        if ((OVRInput.GetActiveController() & m.control) != 0 && m.anclaControl != null)
        {
            origen = m.anclaControl.position;
            direccion = m.anclaControl.forward;
            gatillo = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, m.control);
            return true;
        }

        // Manos
        if (m.hand == null || !m.hand.IsTracked)
            return false;

        if (m.esqueleto == null)
            m.esqueleto = ManosUtil.BuscarEnAncla<OVRSkeleton>(m.anclaMano);

        var punta = ManosUtil.Hueso(m.esqueleto, "IndexTip");
        var base1 = ManosUtil.Hueso(m.esqueleto, "Index1", "IndexProximal");
        var falange = ManosUtil.Hueso(m.esqueleto, "Index3", "IndexDistal");
        var medio = ManosUtil.Hueso(m.esqueleto, "MiddleTip");
        var muneca = ManosUtil.Hueso(m.esqueleto, "WristRoot", "Wrist");

        if (punta != null && base1 != null && falange != null && medio != null && muneca != null)
        {
            // Gesto de "señalar": índice estirado y dedo medio doblado.
            Vector3 tramo1 = (falange.position - base1.position).normalized;
            Vector3 tramo2 = (punta.position - falange.position).normalized;
            bool indiceEstirado = Vector3.Dot(tramo1, tramo2) > 0.8f;
            float largoIndice = Vector3.Distance(punta.position, muneca.position);
            bool medioDoblado = Vector3.Distance(medio.position, muneca.position) < largoIndice * 0.75f;
            if (!indiceEstirado || !medioDoblado)
                return false;

            origen = punta.position;
            direccion = (punta.position - base1.position).normalized;
            return true;
        }

        // Plan B si no hay esqueleto: rayo del hombro a la mano (mano estirada al frente).
        if (m.anclaMano == null)
            return false;
        Vector3 frente = cabeza.forward; frente.y = 0f; frente.Normalize();
        Vector3 derechaPlana = Vector3.Cross(Vector3.up, frente);
        origen = m.anclaMano.position;
        if (Vector3.Dot(origen - cabeza.position, frente) < 0.25f)
            return false;
        Vector3 hombro = cabeza.position + Vector3.down * 0.2f + derechaPlana * (m.esIzquierda ? -0.18f : 0.18f);
        direccion = (origen - hombro).normalized;
        return true;
    }

    // Busca el disco más cercano a donde apuntas (dentro de "anguloIman") que se vea sin obstáculos.
    PuntoTeletransporte Iman(Vector3 origen, Vector3 direccion, ref Vector3 fin)
    {
        PuntoTeletransporte mejor = null;
        float mejorAngulo = anguloIman;
        foreach (var d in discos)
        {
            if (d == null || !d.isActiveAndEnabled || EstoyEncima(d))
                continue;
            Vector3 hacia = d.transform.position - origen;
            float angulo = Vector3.Angle(direccion, hacia);
            if (angulo >= mejorAngulo || hacia.magnitude > alcance)
                continue;
            if (Physics.Raycast(origen, hacia.normalized, out RaycastHit hit, hacia.magnitude - 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<PuntoTeletransporte>() != d)
                continue; // hay algo en medio
            mejor = d;
            mejorAngulo = angulo;
        }
        if (mejor != null)
            fin = mejor.transform.position;
        return mejor;
    }

    bool EstoyEncima(PuntoTeletransporte punto)
    {
        Vector3 d = punto.transform.position - cabeza.position;
        d.y = 0f;
        return d.magnitude < 0.35f;
    }

    void CambiarApuntado(Mano m, PuntoTeletransporte nuevo)
    {
        if (m.apuntado == nuevo)
            return;
        var otra = m == izquierda ? derecha : izquierda;
        if (m.apuntado != null && otra.apuntado != m.apuntado)
            m.apuntado.Resaltar(0f);
        m.apuntado = nuevo;
        m.tiempo = 0f;
        m.perdido = 0f;
    }

    void Teletransportar(PuntoTeletransporte punto)
    {
        // Mueve el rig para que la cabeza del jugador quede justo encima del punto
        // (con fundido a negro corto si el modo optimizado está activado).
        Vector3 destino = punto.transform.position;
        ModoOptimizado.Teletransportar(() =>
        {
            Vector3 delta = destino - cabeza.position;
            delta.y = 0f;
            rig.transform.position += delta;
        });

        bloqueadoHasta = Time.time + pausaDespues;
        CambiarApuntado(izquierda, null);
        CambiarApuntado(derecha, null);
        punto.Resaltar(0f);
    }
}

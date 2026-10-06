using UnityEngine;

// Movimiento continuo, como cuando caminas con el joystick: mientras señalas con el dedo índice
// (los demás dedos doblados), avanzas a paso normal hacia donde apunta el dedo. Al dejar de señalar, te detienes.
// No atraviesa góndolas, paredes ni el mostrador (se desliza junto a ellos).
// Con controles: apunta con el control y mantén el gatillo.
public class CaminarConDedo : MonoBehaviour
{
    [Tooltip("Velocidad al caminar (m/s). 0.9 ≈ paso tranquilo (más lento marea menos)")]
    public float velocidad = 0.9f;
    [Tooltip("Qué tan rápido arranca y frena (m/s²)")]
    public float aceleracion = 3f;
    [Tooltip("Qué tan suave cambia la dirección (más bajo = más estable)")]
    public float suavidad = 6f;
    [Tooltip("Grosor del cuerpo (metros) para no atravesar los muebles")]
    public float radioCuerpo = 0.22f;
    [Tooltip("Zona donde se puede caminar (x mínimo, x máximo, z mínimo, z máximo)")]
    public Vector4 zonaPermitida = new Vector4(-4.8f, 4.8f, -4.8f, 7.8f);

    class Mano
    {
        public bool esIzquierda;
        public Transform ancla, anclaControl;
        public OVRInput.Controller control;
        public OVRHand hand;
        public OVRSkeleton esqueleto;
    }

    OVRCameraRig rig;
    Transform cabeza;
    Mano izquierda, derecha;
    Vector3 velocidadActual, direccionSuave;
    float ultimoGesto = -10f;

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

    void OnDisable()
    {
        velocidadActual = Vector3.zero;
    }

    void Update()
    {
        if (izquierda == null)
            return;
        float dt = Time.deltaTime;

        bool senala = Direccion(derecha, out Vector3 dir);
        if (!senala)
            senala = Direccion(izquierda, out dir);
        if (senala)
        {
            direccionSuave = ultimoGesto < Time.time - 0.3f
                ? dir
                : Vector3.Slerp(direccionSuave, dir, 1f - Mathf.Exp(-suavidad * dt)).normalized;
            ultimoGesto = Time.time;
        }
        // Un pequeño margen: si el seguimiento de la mano se pierde un instante, no se frena en seco.
        bool caminando = Time.time - ultimoGesto < 0.15f;

        Vector3 objetivo = caminando ? direccionSuave * velocidad : Vector3.zero;
        velocidadActual = Vector3.MoveTowards(velocidadActual, objetivo, aceleracion * dt);
        if (velocidadActual.sqrMagnitude < 0.0001f)
            return;

        Vector3 paso = Deslizar(velocidadActual * dt);
        Vector3 nueva = cabeza.position + paso;
        if (nueva.x < zonaPermitida.x || nueva.x > zonaPermitida.y) paso.x = 0f;
        if (nueva.z < zonaPermitida.z || nueva.z > zonaPermitida.w) paso.z = 0f;
        rig.transform.position += paso;
    }

    // Dirección (horizontal) hacia donde señala el dedo. Falso si la mano no está señalando.
    bool Direccion(Mano m, out Vector3 dir)
    {
        dir = Vector3.zero;
        Vector3 apunta;
        if ((OVRInput.GetActiveController() & m.control) != 0 && m.anclaControl != null)
        {
            if (!OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, m.control))
                return false;
            apunta = m.anclaControl.forward;
        }
        else
        {
            if (m.hand == null || !m.hand.IsTracked)
                return false;
            if (m.esqueleto == null)
                m.esqueleto = ManosUtil.BuscarEnAncla<OVRSkeleton>(m.ancla);

            var punta = ManosUtil.Hueso(m.esqueleto, "IndexTip");
            var base1 = ManosUtil.Hueso(m.esqueleto, "Index1", "IndexProximal");
            var falange = ManosUtil.Hueso(m.esqueleto, "Index3", "IndexDistal");
            var medio = ManosUtil.Hueso(m.esqueleto, "MiddleTip");
            var muneca = ManosUtil.Hueso(m.esqueleto, "WristRoot", "Wrist");
            if (punta == null || base1 == null || falange == null || medio == null || muneca == null)
                return false;

            // Gesto de "señalar": índice estirado y dedo medio doblado.
            Vector3 tramo1 = (falange.position - base1.position).normalized;
            Vector3 tramo2 = (punta.position - falange.position).normalized;
            bool indiceEstirado = Vector3.Dot(tramo1, tramo2) > 0.8f;
            float largoIndice = Vector3.Distance(punta.position, muneca.position);
            bool medioDoblado = Vector3.Distance(medio.position, muneca.position) < largoIndice * 0.75f;
            if (!indiceEstirado || !medioDoblado)
                return false;
            apunta = (punta.position - base1.position).normalized;
        }

        Vector3 plano = new Vector3(apunta.x, 0f, apunta.z);
        if (plano.magnitude < 0.35f)
            return false; // señalando casi derecho al piso o al techo: no camina
        dir = plano.normalized;
        return true;
    }

    // Si hay un mueble o pared adelante, se desliza junto a él en vez de atravesarlo.
    Vector3 Deslizar(Vector3 paso)
    {
        for (int intento = 0; intento < 2; intento++)
        {
            float largo = paso.magnitude;
            if (largo < 0.00001f)
                return Vector3.zero;
            Vector3 origen = new Vector3(cabeza.position.x, Mathf.Max(0.5f, cabeza.position.y - 0.6f), cabeza.position.z);
            if (!Choca(origen, paso / largo, largo + 0.05f, out RaycastHit hit))
                return paso;
            Vector3 normal = new Vector3(hit.normal.x, 0f, hit.normal.z);
            if (normal.sqrMagnitude < 0.0001f)
                return Vector3.zero;
            normal.Normalize();
            paso -= normal * Vector3.Dot(paso, normal);
        }
        return Vector3.zero;
    }

    bool Choca(Vector3 origen, Vector3 dir, float distancia, out RaycastHit hit)
    {
        foreach (var h in Physics.SphereCastAll(origen, radioCuerpo, dir, distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // El personaje (pájaro) y los discos del piso no estorban.
            if (h.collider.GetComponentInParent<PersonajeEncontrable>() != null || h.collider.GetComponentInParent<PuntoTeletransporte>() != null)
                continue;
            if (h.distance <= 0f)
                continue; // ya estaba tocándolo al empezar: deja que se aleje
            hit = h;
            return true;
        }
        hit = default;
        return false;
    }
}

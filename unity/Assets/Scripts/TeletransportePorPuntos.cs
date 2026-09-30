using UnityEngine;

// Teletransporte a puntos fijos del piso (discos con PuntoTeletransporte).
// Con las manos: estira la mano hacia un disco (se pone amarillo) y pellizca (pulgar + índice).
// Con controles (opcional): apunta con el control y aprieta el gatillo.
public class TeletransportePorPuntos : MonoBehaviour
{
    [Tooltip("Material de la línea que sale de la mano (Sprites/Default)")]
    public Material materialLinea;
    [Tooltip("Distancia máxima (metros) a la que se puede apuntar")]
    public float alcance = 12f;
    public float anchoLinea = 0.008f;
    public Color colorLinea = new Color(1f, 1f, 1f, 0.6f);
    public Color colorLineaApuntando = new Color(1f, 0.85f, 0.1f, 1f);

    class Mano
    {
        public bool esIzquierda;
        public Transform anclaMano;
        public Transform anclaControl;
        public OVRInput.Controller control;
        public OVRHand hand;
        public LineRenderer linea;
        public bool seleccionAnterior;
        public PuntoTeletransporte apuntado;
    }

    OVRCameraRig rig;
    Transform cabeza;
    Mano izquierda, derecha;

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

        var manos = FindObjectsByType<OVRHand>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        izquierda = CrearMano(true, rig.leftHandAnchor, rig.leftControllerAnchor, OVRInput.Controller.LTouch, manos);
        derecha = CrearMano(false, rig.rightHandAnchor, rig.rightControllerAnchor, OVRInput.Controller.RTouch, manos);
    }

    Mano CrearMano(bool esIzquierda, Transform anclaMano, Transform anclaControl, OVRInput.Controller control, OVRHand[] manos)
    {
        var m = new Mano { esIzquierda = esIzquierda, anclaMano = anclaMano, anclaControl = anclaControl, control = control };
        foreach (var h in manos)
            if (h.transform.IsChildOf(anclaMano))
                m.hand = h;

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

    void Update()
    {
        if (izquierda == null)
            return;
        Procesar(izquierda);
        Procesar(derecha);
    }

    void Procesar(Mano m)
    {
        if (!ObtenerRayo(m, out Vector3 origen, out Vector3 direccion, out bool seleccion))
        {
            m.linea.enabled = false;
            CambiarApuntado(m, null);
            m.seleccionAnterior = false;
            return;
        }

        PuntoTeletransporte punto = null;
        Vector3 fin = origen + direccion * 1.5f;
        if (Physics.Raycast(origen, direccion, out RaycastHit hit, alcance, ~0, QueryTriggerInteraction.Ignore))
        {
            punto = hit.collider.GetComponentInParent<PuntoTeletransporte>();
            fin = hit.point;
        }

        m.linea.enabled = true;
        m.linea.SetPosition(0, origen);
        m.linea.SetPosition(1, fin);
        Color c = punto != null ? colorLineaApuntando : colorLinea;
        m.linea.startColor = c;
        m.linea.endColor = c;

        CambiarApuntado(m, punto);

        // Se teletransporta en el momento en que empieza el pellizco (o el gatillo).
        if (seleccion && !m.seleccionAnterior && punto != null)
            Teletransportar(punto.transform.position);
        m.seleccionAnterior = seleccion;
    }

    bool ObtenerRayo(Mano m, out Vector3 origen, out Vector3 direccion, out bool seleccion)
    {
        origen = direccion = Vector3.zero;
        seleccion = false;

        // Controles (opcional)
        if ((OVRInput.GetActiveController() & m.control) != 0 && m.anclaControl != null)
        {
            origen = m.anclaControl.position;
            direccion = m.anclaControl.forward;
            seleccion = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, m.control);
            return true;
        }

        // Manos
        if (m.hand == null || !m.hand.IsTracked || m.anclaMano == null)
            return false;

        Vector3 frente = cabeza.forward; frente.y = 0f; frente.Normalize();
        Vector3 derechaPlana = Vector3.Cross(Vector3.up, frente);

        origen = m.anclaMano.position;
        // Solo cuenta si la mano está estirada hacia adelante (no cuando está abajo o pegada al cuerpo).
        if (Vector3.Dot(origen - cabeza.position, frente) < 0.25f)
            return false;

        // Rayo "del hombro a la mano", como el puntero del menú de Meta.
        Vector3 hombro = cabeza.position + Vector3.down * 0.2f + derechaPlana * (m.esIzquierda ? -0.18f : 0.18f);
        direccion = (origen - hombro).normalized;
        seleccion = m.hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        return true;
    }

    void CambiarApuntado(Mano m, PuntoTeletransporte nuevo)
    {
        if (m.apuntado == nuevo)
            return;
        var otra = m == izquierda ? derecha : izquierda;
        if (m.apuntado != null && otra.apuntado != m.apuntado)
            m.apuntado.Resaltar(false);
        m.apuntado = nuevo;
        if (nuevo != null)
            nuevo.Resaltar(true);
    }

    void Teletransportar(Vector3 destino)
    {
        // Mueve el rig para que la cabeza del jugador quede justo encima del punto.
        Vector3 delta = destino - cabeza.position;
        delta.y = 0f;
        rig.transform.position += delta;
    }
}

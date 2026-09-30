using UnityEngine;

// El personaje como "juguete en gravedad cero", una vez que ya lo encontraste:
// - Flota esperando cerca de ti, moviéndose suavecito.
// - Lo agarras pellizcando (pulgar + índice) cerca de él, o con el botón de agarre del control.
// - Al soltarlo sale con la velocidad de tu mano y flota como en el espacio: sin gravedad,
//   frenando poco a poco, girando y rebotando en paredes y góndolas.
// - A los pocos segundos regresa hacia ti haciendo una curva, como un bumerán.
[RequireComponent(typeof(Rigidbody))]
public class AgarreAntigravedad : MonoBehaviour
{
    [Header("Agarrar")]
    [Tooltip("Distancia (metros) entre el pellizco y el personaje para poder agarrarlo")]
    public float distanciaAgarre = 0.18f;

    [Header("Flotar como en el espacio")]
    [Tooltip("Qué tanto frena al flotar. Poquito = se va lejos")]
    public float frenoLineal = 0.35f;
    public float frenoGiro = 0.2f;
    [Tooltip("Multiplica la velocidad de tu mano al soltarlo")]
    public float fuerzaLanzamiento = 1.2f;
    public float velocidadMaxima = 6f;
    [Tooltip("Qué tanto rebota al chocar (0 a 1)")]
    public float rebote = 0.7f;

    [Header("Regreso tipo bumerán")]
    public float segundosAntesDeRegresar = 2.5f;
    [Tooltip("Fuerza con la que vuelve hacia ti")]
    public float fuerzaRegreso = 5f;
    [Tooltip("Qué tanto se curva el regreso hacia un lado")]
    public float curvaBumeran = 1.5f;

    [Header("Mientras flota esperando")]
    public float amplitudFlote = 0.015f;
    public float velocidadFlote = 1.5f;
    public float giroFlote = 25f;

    enum Estado { Apagado, Esperando, Agarrado, Libre, Regresando }

    class Mano
    {
        public Transform ancla;
        public Transform anclaControl;
        public OVRInput.Controller control;
        public OVRHand hand;
        public OVRSkeleton esqueleto;
        public bool agarrandoAntes;
    }

    Estado estado = Estado.Apagado;
    Rigidbody cuerpo;
    Collider[] misColliders;
    Transform cabeza;
    Mano izquierda, derecha, manoQueAgarra;
    Vector3 posicionAgarre;
    Quaternion rotacionRelativa;
    Vector3 puntoEspera;
    float tiempoEstado, ladoCurva = 1f;

    readonly Vector3[] historial = new Vector3[8];
    readonly float[] tiempos = new float[8];
    int indiceHistorial;

    public bool Activo => estado != Estado.Apagado;

    void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        misColliders = GetComponentsInChildren<Collider>();
    }

    void Start()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null)
            return;
        cabeza = rig.centerEyeAnchor;
        izquierda = CrearMano(rig.leftHandAnchor, rig.leftControllerAnchor, OVRInput.Controller.LTouch);
        derecha = CrearMano(rig.rightHandAnchor, rig.rightControllerAnchor, OVRInput.Controller.RTouch);

        // Material físico rebotón y sin fricción, como en el espacio.
        var material = new PhysicsMaterial("Antigravedad")
        {
            bounciness = rebote,
            dynamicFriction = 0.05f,
            staticFriction = 0.05f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum,
        };
        foreach (var c in misColliders)
            c.sharedMaterial = material;
    }

    Mano CrearMano(Transform ancla, Transform anclaControl, OVRInput.Controller control)
    {
        return new Mano
        {
            ancla = ancla,
            anclaControl = anclaControl,
            control = control,
            hand = ManosUtil.BuscarEnAncla<OVRHand>(ancla),
            esqueleto = ManosUtil.BuscarEnAncla<OVRSkeleton>(ancla),
        };
    }

    // Lo llama PersonajeEncontrable cuando llega a la mano.
    public void Activar()
    {
        puntoEspera = transform.position;
        CambiarEstado(Estado.Esperando);
    }

    // Lo llama PersonajeEncontrable al reiniciar la partida.
    public void Desactivar()
    {
        CambiarEstado(Estado.Apagado);
    }

    void CambiarEstado(Estado nuevo)
    {
        estado = nuevo;
        tiempoEstado = 0f;
        bool fisica = nuevo == Estado.Libre || nuevo == Estado.Regresando;
        cuerpo.isKinematic = !fisica;
        cuerpo.useGravity = false;
        cuerpo.interpolation = fisica ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        cuerpo.collisionDetectionMode = fisica ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.Discrete;
        cuerpo.linearDamping = frenoLineal;
        cuerpo.angularDamping = frenoGiro;
        if (nuevo != Estado.Agarrado)
            manoQueAgarra = null;
    }

    void Update()
    {
        if (estado == Estado.Apagado || cabeza == null)
            return;
        tiempoEstado += Time.deltaTime;

        if (estado == Estado.Agarrado)
        {
            SeguirMano();
            if (!EstaAgarrando(manoQueAgarra))
                Soltar();
            ActualizarPellizcoAnterior();
            return;
        }

        // Se puede agarrar mientras espera, flota o regresa.
        var mano = ManoQueEmpiezaAAgarrar(izquierda) ?? ManoQueEmpiezaAAgarrar(derecha);
        ActualizarPellizcoAnterior();
        if (mano != null)
        {
            Agarrar(mano);
            return;
        }

        if (estado == Estado.Esperando)
        {
            float y = Mathf.Sin(Time.time * velocidadFlote) * amplitudFlote;
            transform.position = puntoEspera + Vector3.up * y;
            transform.Rotate(Vector3.up, giroFlote * Time.deltaTime, Space.World);
        }
        else if (estado == Estado.Libre)
        {
            if (tiempoEstado > segundosAntesDeRegresar)
            {
                ladoCurva = Random.value < 0.5f ? -1f : 1f;
                CambiarEstado(Estado.Regresando);
            }
        }
    }

    void FixedUpdate()
    {
        if (estado != Estado.Regresando || cabeza == null)
            return;

        Vector3 casa = PuntoCasa();
        Vector3 hacia = casa - cuerpo.position;
        float distancia = hacia.magnitude;

        // Resorte hacia ti + curva lateral que se apaga al llegar (efecto bumerán).
        Vector3 fuerza = hacia * fuerzaRegreso - cuerpo.linearVelocity * (2f * Mathf.Sqrt(fuerzaRegreso) * 0.8f);
        if (distancia > 0.01f)
            fuerza += Vector3.Cross(Vector3.up, hacia / distancia) * (curvaBumeran * ladoCurva * Mathf.Clamp01(distancia / 2f));
        cuerpo.AddForce(fuerza, ForceMode.Acceleration);

        if (distancia < 0.06f && cuerpo.linearVelocity.magnitude < 0.25f)
        {
            puntoEspera = cuerpo.position;
            CambiarEstado(Estado.Esperando);
        }
    }

    // Donde vuelve a esperarte: al frente, un poco a la derecha y a la altura del pecho.
    Vector3 PuntoCasa()
    {
        Vector3 frente = cabeza.forward; frente.y = 0f; frente.Normalize();
        Vector3 derechaPlana = Vector3.Cross(Vector3.up, frente);
        return cabeza.position + frente * 0.45f + derechaPlana * 0.15f + Vector3.down * 0.25f;
    }

    // ---------- Agarrar y soltar ----------

    Mano ManoQueEmpiezaAAgarrar(Mano m)
    {
        if (m == null)
            return null;
        bool agarrando = EstaAgarrando(m);
        if (!agarrando || m.agarrandoAntes)
            return null; // solo cuenta el momento en que empieza a pellizcar
        return DistanciaA(PuntoDeAgarre(m)) <= distanciaAgarre ? m : null;
    }

    void ActualizarPellizcoAnterior()
    {
        if (izquierda != null) izquierda.agarrandoAntes = EstaAgarrando(izquierda);
        if (derecha != null) derecha.agarrandoAntes = EstaAgarrando(derecha);
    }

    bool EstaAgarrando(Mano m)
    {
        if (m == null)
            return false;
        if ((OVRInput.GetActiveController() & m.control) != 0)
            return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, m.control);
        if (m.hand == null || !m.hand.IsTracked)
            return false;
        float fuerza = m.hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        // Umbral distinto para empezar y para soltar, así no se suelta solo por temblor.
        return m.agarrandoAntes ? fuerza > 0.4f : fuerza > 0.75f;
    }

    Vector3 PuntoDeAgarre(Mano m)
    {
        if ((OVRInput.GetActiveController() & m.control) != 0 && m.anclaControl != null)
            return m.anclaControl.position;
        if (m.esqueleto == null)
            m.esqueleto = ManosUtil.BuscarEnAncla<OVRSkeleton>(m.ancla);
        return ManosUtil.PuntoDePellizco(m.esqueleto, m.ancla);
    }

    Quaternion RotacionMano(Mano m)
    {
        if ((OVRInput.GetActiveController() & m.control) != 0 && m.anclaControl != null)
            return m.anclaControl.rotation;
        return m.ancla != null ? m.ancla.rotation : Quaternion.identity;
    }

    float DistanciaA(Vector3 punto)
    {
        float mejor = Vector3.Distance(punto, transform.position);
        foreach (var c in misColliders)
            if (c != null && c.enabled)
                mejor = Mathf.Min(mejor, Vector3.Distance(c.ClosestPoint(punto), punto));
        return mejor;
    }

    void Agarrar(Mano m)
    {
        CambiarEstado(Estado.Agarrado);
        manoQueAgarra = m;
        posicionAgarre = transform.position - PuntoDeAgarre(m);
        rotacionRelativa = Quaternion.Inverse(RotacionMano(m)) * transform.rotation;
        for (int i = 0; i < historial.Length; i++)
        {
            historial[i] = transform.position;
            tiempos[i] = Time.time;
        }
    }

    void SeguirMano()
    {
        transform.position = PuntoDeAgarre(manoQueAgarra) + posicionAgarre * 0.9f;
        posicionAgarre *= 0.9f; // se va centrando en el pellizco
        transform.rotation = RotacionMano(manoQueAgarra) * rotacionRelativa;

        indiceHistorial = (indiceHistorial + 1) % historial.Length;
        historial[indiceHistorial] = transform.position;
        tiempos[indiceHistorial] = Time.time;
    }

    void Soltar()
    {
        // Velocidad de la mano en los últimos cuadros.
        int masViejo = (indiceHistorial + 1) % historial.Length;
        float dt = Mathf.Max(0.01f, tiempos[indiceHistorial] - tiempos[masViejo]);
        Vector3 velocidad = (historial[indiceHistorial] - historial[masViejo]) / dt * fuerzaLanzamiento;
        velocidad = Vector3.ClampMagnitude(velocidad, velocidadMaxima);

        CambiarEstado(Estado.Libre);
        cuerpo.linearVelocity = velocidad;
        cuerpo.angularVelocity = Random.onUnitSphere * Mathf.Lerp(1f, 6f, velocidad.magnitude / velocidadMaxima);
    }
}

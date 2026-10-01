using UnityEngine;

// El personaje como "juguete en gravedad cero", una vez que ya lo encontraste.
// Se maneja como un objeto real, con las manos:
// - Las manos son sólidas: si lo tocas o le das un manotazo, lo empujas (no lo traspasas).
// - Para agarrarlo: cierra la mano alrededor de él, como agarrando una pelota.
// - Para sostenerlo: pon la mano abierta con la palma hacia arriba debajo de él y se queda encima.
// - Al soltarlo flota suave (sin gravedad), frena rápido y luego vuelve hacia ti como un bumerán.
[RequireComponent(typeof(Rigidbody))]
public class AgarreAntigravedad : MonoBehaviour
{
    [Header("Agarrar como objeto real")]
    [Tooltip("Qué tan cerrada debe estar la mano para agarrarlo (más bajo = más cerrada)")]
    public float cierreParaAgarrar = 1.3f;
    [Tooltip("Qué tan abierta debe estar la mano para soltarlo")]
    public float cierreParaSoltar = 1.5f;
    [Tooltip("Distancia extra (metros) entre la palma y el personaje para poder agarrarlo")]
    public float margenAgarre = 0.07f;

    [Header("Flotar (suave)")]
    [Tooltip("Qué tanto frena al flotar. Más alto = no se va lejos")]
    public float frenoLineal = 1.2f;
    public float frenoGiro = 0.8f;
    [Tooltip("Multiplica la velocidad de tu mano al soltarlo")]
    public float fuerzaLanzamiento = 0.6f;
    public float velocidadMaxima = 2.5f;
    [Tooltip("Qué tanto rebota al chocar (0 a 1)")]
    public float rebote = 0.5f;

    [Header("Regreso tipo bumerán")]
    public float segundosAntesDeRegresar = 2f;
    public float fuerzaRegreso = 4f;
    public float curvaBumeran = 0.8f;

    [Header("Mientras flota esperando")]
    [Tooltip("Qué tan firme se queda en su sitio cuando nadie lo toca")]
    public float fuerzaEspera = 6f;
    public float amplitudFlote = 0.015f;
    public float velocidadFlote = 1.5f;
    public float giroFlote = 0.4f;

    [Header("Manos sólidas")]
    public float radioPalma = 0.04f;
    public float radioDedo = 0.011f;

    enum Estado { Apagado, Flotando, EnPalma, Agarrado, Libre, Regresando }

    static readonly string[] puntas = { "ThumbTip", "IndexTip", "MiddleTip", "RingTip", "PinkyTip", "LittleTip" };

    class Mano
    {
        public bool esIzquierda;
        public Transform ancla, anclaControl;
        public OVRInput.Controller control;
        public OVRHand hand;
        public OVRSkeleton esqueleto;
        public ManosUtil.Palma palma;
        public bool cerradaAntes;
        public Vector3 posicionAnterior, velocidad;
        public Rigidbody[] solidos;    // [0] = palma, [1..] = puntas de los dedos
        public Collider[] colliders;
        public float ignorarHasta;
        public bool ignorada;          // la mano está "atravesando" al personaje a propósito (mientras lo sostiene)
    }

    Estado estado = Estado.Apagado;
    Rigidbody cuerpo;
    Collider[] misColliders;
    Transform cabeza;
    Mano izquierda, derecha, manoActiva;
    Vector3 offsetLocal, puntoEspera;
    Quaternion rotacionRelativa;
    float tiempoEstado, ladoCurva = 1f, mitad = 0.05f;

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
        izquierda = CrearMano(true, rig.leftHandAnchor, rig.leftControllerAnchor, OVRInput.Controller.LTouch);
        derecha = CrearMano(false, rig.rightHandAnchor, rig.rightControllerAnchor, OVRInput.Controller.RTouch);

        var material = new PhysicsMaterial("Antigravedad")
        {
            bounciness = rebote,
            dynamicFriction = 0.2f,
            staticFriction = 0.2f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Average,
        };
        foreach (var c in misColliders)
            c.sharedMaterial = material;

        Bounds b = misColliders.Length > 0 ? misColliders[0].bounds : new Bounds(transform.position, Vector3.one * 0.1f);
        mitad = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
    }

    Mano CrearMano(bool esIzquierda, Transform ancla, Transform anclaControl, OVRInput.Controller control)
    {
        var m = new Mano
        {
            esIzquierda = esIzquierda,
            ancla = ancla,
            anclaControl = anclaControl,
            control = control,
            hand = ManosUtil.BuscarEnAncla<OVRHand>(ancla),
            esqueleto = ManosUtil.BuscarEnAncla<OVRSkeleton>(ancla),
            solidos = new Rigidbody[6],
            colliders = new Collider[6],
        };
        // Esferas invisibles que siguen la palma y las puntas de los dedos, para que la mano empuje.
        for (int i = 0; i < 6; i++)
        {
            var go = new GameObject($"ManoSolida_{(esIzquierda ? "I" : "D")}_{i}");
            go.layer = 2; // "Ignore Raycast": empuja al personaje pero no tapa la mirada ni el dedo que señala
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var esfera = go.AddComponent<SphereCollider>();
            esfera.radius = i == 0 ? radioPalma : radioDedo;
            m.solidos[i] = rb;
            m.colliders[i] = esfera;
            go.transform.position = Vector3.down * 100f;
        }
        return m;
    }

    // Lo llama PersonajeEncontrable cuando llega a la mano.
    public void Activar()
    {
        IgnorarMano(izquierda, false);
        IgnorarMano(derecha, false);
        puntoEspera = transform.position;
        Cambiar(Estado.Flotando);
    }

    // Lo llama PersonajeEncontrable al reiniciar la partida.
    public void Desactivar()
    {
        Cambiar(Estado.Apagado);
        // Importante: si la partida se reinicia con el personaje en una mano, esa mano vuelve a ser sólida.
        IgnorarMano(izquierda, false);
        IgnorarMano(derecha, false);
    }

    void Cambiar(Estado nuevo)
    {
        estado = nuevo;
        tiempoEstado = 0f;
        bool fisica = nuevo == Estado.Flotando || nuevo == Estado.Libre || nuevo == Estado.Regresando;
        cuerpo.isKinematic = !fisica;
        cuerpo.useGravity = false;
        cuerpo.interpolation = fisica ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        cuerpo.collisionDetectionMode = fisica ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.Discrete;
        cuerpo.linearDamping = frenoLineal;
        cuerpo.angularDamping = frenoGiro;
        if (nuevo != Estado.Agarrado && nuevo != Estado.EnPalma)
            manoActiva = null;
    }

    void Update()
    {
        if (cabeza == null)
            return;
        LeerMano(izquierda);
        LeerMano(derecha);
        // Las manos que ya no lo sostienen vuelven a ser sólidas (revisado cada cuadro, para que nunca se queden "fantasma").
        foreach (var m in new[] { izquierda, derecha })
            if (m != null && m.ignorada && m != manoActiva && Time.time > m.ignorarHasta)
                IgnorarMano(m, false);
        if (estado == Estado.Apagado)
            return;
        tiempoEstado += Time.deltaTime;

        switch (estado)
        {
            case Estado.Agarrado:
            case Estado.EnPalma:
                // Pasarlo de una mano a la otra: si la otra mano lo agarra, se lo queda.
                var otra = manoActiva == izquierda ? derecha : izquierda;
                if (otra != null && EstaCerrada(otra) && !otra.cerradaAntes
                    && Vector3.Distance(PuntoDeMano(otra), cuerpo.position) < mitad + margenAgarre)
                {
                    var anterior = manoActiva;
                    Tomar(otra, Estado.Agarrado);
                    if (anterior != null) anterior.ignorarHasta = Time.time + 0.4f;
                    break;
                }
                if (estado == Estado.Agarrado) SeguirAgarre();
                else SeguirPalma();
                break;
            default:
                if (!IntentarTomar(izquierda) && !IntentarTomar(derecha) && estado == Estado.Libre && tiempoEstado > segundosAntesDeRegresar)
                {
                    ladoCurva = Random.value < 0.5f ? -1f : 1f;
                    Cambiar(Estado.Regresando);
                }
                break;
        }

        if (izquierda != null) izquierda.cerradaAntes = EstaCerrada(izquierda);
        if (derecha != null) derecha.cerradaAntes = EstaCerrada(derecha);
    }

    void FixedUpdate()
    {
        MoverSolidos(izquierda);
        MoverSolidos(derecha);
        if (cabeza == null)
            return;

        if (estado == Estado.Flotando)
        {
            // Se queda en su sitio con un resorte suave (si lo empujan, cede y vuelve).
            Vector3 objetivo = puntoEspera + Vector3.up * (Mathf.Sin(Time.time * velocidadFlote) * amplitudFlote);
            Vector3 fuerza = (objetivo - cuerpo.position) * fuerzaEspera - cuerpo.linearVelocity * (2f * Mathf.Sqrt(fuerzaEspera));
            cuerpo.AddForce(fuerza, ForceMode.Acceleration);
            cuerpo.angularVelocity = Vector3.Lerp(cuerpo.angularVelocity, Vector3.up * giroFlote, Time.fixedDeltaTime * 2f);
        }
        else if (estado == Estado.Regresando)
        {
            Vector3 hacia = PuntoCasa() - cuerpo.position;
            float distancia = hacia.magnitude;
            Vector3 fuerza = hacia * fuerzaRegreso - cuerpo.linearVelocity * (2f * Mathf.Sqrt(fuerzaRegreso) * 0.8f);
            if (distancia > 0.01f)
                fuerza += Vector3.Cross(Vector3.up, hacia / distancia) * (curvaBumeran * ladoCurva * Mathf.Clamp01(distancia / 2f));
            cuerpo.AddForce(fuerza, ForceMode.Acceleration);
            if (distancia < 0.06f && cuerpo.linearVelocity.magnitude < 0.25f)
            {
                puntoEspera = cuerpo.position;
                Cambiar(Estado.Flotando);
            }
        }
    }

    // Un manotazo mientras flota lo suelta para que siga moviéndose y luego regrese.
    void OnCollisionEnter(Collision choque)
    {
        if (estado == Estado.Flotando && EsDeUnaMano(choque.collider))
            Cambiar(Estado.Libre);
    }

    // ---------- Manos ----------

    void LeerMano(Mano m)
    {
        if (m == null)
            return;
        if (m.esqueleto == null)
            m.esqueleto = ManosUtil.BuscarEnAncla<OVRSkeleton>(m.ancla);
        bool rastreada = m.hand != null && m.hand.IsTracked;
        m.palma = rastreada ? ManosUtil.LeerPalma(m.esqueleto, m.esIzquierda) : default;

        Vector3 pos = PuntoDeMano(m);
        if (Time.deltaTime > 0f)
            m.velocidad = Vector3.Lerp(m.velocidad, (pos - m.posicionAnterior) / Time.deltaTime, 0.5f);
        m.posicionAnterior = pos;
    }

    bool ConControl(Mano m) => (OVRInput.GetActiveController() & m.control) != 0 && m.anclaControl != null;

    Vector3 PuntoDeMano(Mano m)
    {
        if (ConControl(m)) return m.anclaControl.position;
        if (m.palma.valida) return m.palma.centro;
        return m.ancla != null ? m.ancla.position : Vector3.zero;
    }

    bool EstaCerrada(Mano m)
    {
        if (m == null)
            return false;
        if (ConControl(m))
            return OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, m.control);
        if (!m.palma.valida)
            return false;
        return m.palma.cierre < (m.cerradaAntes ? cierreParaSoltar : cierreParaAgarrar);
    }

    bool EsDeUnaMano(Collider c)
    {
        foreach (var m in new[] { izquierda, derecha })
            if (m != null)
                foreach (var col in m.colliders)
                    if (col == c)
                        return true;
        return false;
    }

    void MoverSolidos(Mano m)
    {
        if (m == null)
            return;
        bool rastreada = (m.hand != null && m.hand.IsTracked) || ConControl(m);
        for (int i = 0; i < m.solidos.Length; i++)
        {
            Vector3 destino = Vector3.down * 100f;
            if (rastreada)
            {
                if (i == 0)
                    destino = PuntoDeMano(m);
                else if (!ConControl(m))
                {
                    var punta = ManosUtil.Hueso(m.esqueleto, puntas[i == 5 ? 4 : i - 1], i == 5 ? puntas[5] : puntas[i - 1]);
                    if (punta != null) destino = punta.position;
                }
            }
            m.solidos[i].MovePosition(destino);
        }
    }

    void IgnorarMano(Mano m, bool ignorar)
    {
        if (m == null)
            return;
        m.ignorada = ignorar;
        foreach (var c in m.colliders)
            foreach (var mio in misColliders)
                if (c != null && mio != null)
                    Physics.IgnoreCollision(c, mio, ignorar);
    }

    // ---------- Tomar, sostener y soltar ----------

    bool IntentarTomar(Mano m)
    {
        if (m == null)
            return false;
        Vector3 punto = PuntoDeMano(m);
        float distancia = Vector3.Distance(punto, cuerpo.position);

        // Agarrar: la mano se cierra cerca de él (como una pelota).
        bool cerrada = EstaCerrada(m);
        if (cerrada && !m.cerradaAntes && distancia < mitad + margenAgarre)
        {
            Tomar(m, Estado.Agarrado);
            return true;
        }

        // Sostener: mano abierta con la palma hacia arriba, justo debajo de él.
        if (m.palma.valida && !cerrada && m.palma.normal.y > 0.75f && m.palma.cierre > 1.45f)
        {
            Vector3 rel = cuerpo.position - m.palma.centro;
            float vertical = Vector3.Dot(rel, m.palma.normal);
            float horizontal = (rel - m.palma.normal * vertical).magnitude;
            if (vertical > 0f && vertical < mitad + 0.12f && horizontal < 0.08f)
            {
                Tomar(m, Estado.EnPalma);
                return true;
            }
        }
        return false;
    }

    void Tomar(Mano m, Estado nuevo)
    {
        Cambiar(nuevo);
        manoActiva = m;
        IgnorarMano(m, true);
        Quaternion rotMano = RotacionMano(m);
        offsetLocal = Quaternion.Inverse(rotMano) * (transform.position - PuntoDeMano(m));
        rotacionRelativa = Quaternion.Inverse(rotMano) * transform.rotation;
    }

    Quaternion RotacionMano(Mano m)
    {
        if (ConControl(m)) return m.anclaControl.rotation;
        return m.ancla != null ? m.ancla.rotation : Quaternion.identity;
    }

    void SeguirAgarre()
    {
        var m = manoActiva;
        if (m == null || !EstaCerrada(m))
        {
            Soltar(fuerzaLanzamiento);
            return;
        }
        Quaternion rotMano = RotacionMano(m);
        transform.position = PuntoDeMano(m) + rotMano * offsetLocal;
        transform.rotation = rotMano * rotacionRelativa;
    }

    void SeguirPalma()
    {
        var m = manoActiva;
        if (m == null || !m.palma.valida || m.palma.normal.y < 0.45f)
        {
            Soltar(0.5f); // se resbala de la mano
            return;
        }
        if (EstaCerrada(m) && !m.cerradaAntes)
        {
            Tomar(m, Estado.Agarrado); // cerró la mano con él encima
            return;
        }
        Vector3 objetivo = m.palma.centro + m.palma.normal * (mitad + 0.012f);
        transform.position = Vector3.Lerp(transform.position, objetivo, Time.deltaTime * 20f);
    }

    void Soltar(float factor)
    {
        var m = manoActiva;
        Vector3 velocidad = m != null ? Vector3.ClampMagnitude(m.velocidad * factor, velocidadMaxima) : Vector3.zero;
        Cambiar(Estado.Libre);
        cuerpo.linearVelocity = velocidad;
        cuerpo.angularVelocity = Random.onUnitSphere * Mathf.Lerp(0.5f, 3f, velocidad.magnitude / velocidadMaxima);
        // La mano vuelve a ser sólida 0.4 s después de soltarlo (así no sale disparado al soltarlo).
        if (m != null)
            m.ignorarHasta = Time.time + 0.4f;
    }

    // Donde vuelve a esperarte: al frente, un poco a la derecha y a la altura del pecho.
    Vector3 PuntoCasa()
    {
        Vector3 frente = cabeza.forward; frente.y = 0f; frente.Normalize();
        Vector3 derechaPlana = Vector3.Cross(Vector3.up, frente);
        return cabeza.position + frente * 0.45f + derechaPlana * 0.15f + Vector3.down * 0.25f;
    }

    void OnDestroy()
    {
        foreach (var m in new[] { izquierda, derecha })
            if (m != null)
                foreach (var rb in m.solidos)
                    if (rb != null)
                        Destroy(rb.gameObject);
    }
}

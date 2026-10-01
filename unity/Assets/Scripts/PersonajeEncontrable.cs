using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

// Va en el personaje escondido. Detecta cuando el jugador lo encuentra:
// - mirándolo unos segundos (cambia de color mientras lo miras). No hace falta apuntarle exacto:
//   cuenta un "círculo invisible" grande a su alrededor, siempre que no haya nada tapándolo.
// - acercando la mano (tocándolo).
// Al encontrarlo, salta hasta la mano derecha del jugador y queda flotando (ver AgarreAntigravedad).
// Cuando el jugador está cerca, se mueve un poquito de vez en cuando (pista para encontrarlo).
public class PersonajeEncontrable : MonoBehaviour
{
    [Header("Encontrar mirando")]
    [Tooltip("Segundos que hay que mirarlo")]
    public float segundosMirando = 1.5f;
    [Tooltip("Distancia máxima (metros) desde la que cuenta la mirada")]
    public float distanciaMaxima = 12f;
    [Tooltip("Radio (metros) del círculo invisible alrededor del personaje que cuenta como 'mirarlo'")]
    public float radioVision = 0.45f;
    [Tooltip("Ángulo mínimo (grados) que siempre cuenta, aunque esté lejos")]
    public float anguloMinimo = 5f;

    [Header("Encontrar tocando")]
    [Tooltip("Distancia (metros) entre la mano y el personaje para contar como toque")]
    public float distanciaToque = 0.3f;

    [Header("Efecto mientras lo miran")]
    [Tooltip("Color que toma apenas lo miras (como el 'hover' de un botón)")]
    public Color colorAlMirar = new Color(1f, 0.85f, 0.1f);
    [Tooltip("Qué tan rápido cambia de color")]
    public float velocidadColor = 8f;

    [Header("Al encontrarlo: salta a la mano derecha")]
    public float duracionSalto = 0.8f;
    [Tooltip("Altura extra (metros) del arco del salto")]
    public float alturaSalto = 0.35f;
    [Tooltip("Qué tan arriba de la mano queda flotando (metros)")]
    public float alturaSobreMano = 0.1f;

    [Header("Pista: se mueve cuando estás cerca")]
    public float distanciaPista = 2.5f;
    public float segundosEntreSacudidas = 2.5f;

    [Header("Evento (se dispara cuando llega a la mano)")]
    public UnityEvent alSerEncontrado = new UnityEvent();

    // El JuegoManager lo activa solo mientras se está buscando.
    public bool Activo { get; set; }

    Transform cabeza, manoIzquierda, manoDerecha;
    Collider[] misColliders;
    Rigidbody cuerpo;
    Vector3 escalaOriginal;
    float tiempoMirando;
    float proximaSacudida;
    Coroutine rutinaSalto, rutinaSacudida;

    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor"); // URP y shader toon
    static readonly int idColor = Shader.PropertyToID("_Color");         // Built-in
    Renderer[] misRenderers;
    Color[] coloresOriginales;
    MaterialPropertyBlock bloque;
    float mezclaColor;

    void Awake()
    {
        escalaOriginal = transform.localScale;
        cuerpo = GetComponentInChildren<Rigidbody>();
        misColliders = GetComponentsInChildren<Collider>();
        if (misColliders.Length == 0)
            Debug.LogWarning("PersonajeEncontrable: el personaje no tiene Collider, no se podrá encontrar mirándolo.", this);

        bloque = new MaterialPropertyBlock();
        misRenderers = GetComponentsInChildren<Renderer>();
        coloresOriginales = new Color[misRenderers.Length];
        for (int i = 0; i < misRenderers.Length; i++)
        {
            var mat = misRenderers[i].sharedMaterial;
            coloresOriginales[i] = mat == null ? Color.white
                : mat.HasProperty(idBaseColor) ? mat.GetColor(idBaseColor)
                : mat.HasProperty(idColor) ? mat.GetColor(idColor)
                : Color.white;
        }
    }

    void Start()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null)
        {
            cabeza = rig.centerEyeAnchor;
            manoIzquierda = rig.leftHandAnchor;
            manoDerecha = rig.rightHandAnchor;
        }
        else if (Camera.main != null)
        {
            cabeza = Camera.main.transform;
        }
    }

    void Update()
    {
        if (!Activo || cabeza == null)
            return;

        if (LoEstanTocando())
        {
            Encontrado();
            return;
        }

        bool mirando = LoEstanMirando();
        if (mirando)
            tiempoMirando += Time.deltaTime;
        else
            tiempoMirando = Mathf.Max(0f, tiempoMirando - Time.deltaTime * 2f);

        // Cambia de color apenas lo miras.
        mezclaColor = Mathf.MoveTowards(mezclaColor, mirando ? 1f : 0f, Time.deltaTime * velocidadColor);
        AplicarColor(mezclaColor);

        if (tiempoMirando >= segundosMirando)
        {
            Encontrado();
            return;
        }

        // Pista: si estás cerca, se sacude un poquito de vez en cuando.
        if (Time.time >= proximaSacudida && rutinaSacudida == null
            && Vector3.Distance(cabeza.position, transform.position) < distanciaPista)
        {
            proximaSacudida = Time.time + segundosEntreSacudidas;
            rutinaSacudida = StartCoroutine(Sacudida());
        }
    }

    IEnumerator Sacudida()
    {
        Vector3 posicion = transform.position;
        Quaternion rotacion = transform.rotation;
        const float duracion = 0.4f;
        for (float t = 0f; t < duracion && Activo; t += Time.deltaTime)
        {
            float k = t / duracion;
            transform.position = posicion + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.03f);
            transform.rotation = rotacion * Quaternion.Euler(0f, Mathf.Sin(k * Mathf.PI * 4f) * 15f, 0f);
            yield return null;
        }
        if (Activo)
            transform.SetPositionAndRotation(posicion, rotacion);
        rutinaSacudida = null;
    }

    bool LoEstanMirando()
    {
        Bounds limites = Limites();
        Vector3 hacia = limites.center - cabeza.position;
        float distancia = hacia.magnitude;
        if (distancia < 0.05f)
            return true;
        if (distancia > distanciaMaxima)
            return false;

        // ¿Está dentro del "círculo invisible" alrededor de donde miras?
        float anguloPermitido = Mathf.Max(anguloMinimo, Mathf.Atan2(radioVision, distancia) * Mathf.Rad2Deg);
        if (Vector3.Angle(cabeza.forward, hacia) > anguloPermitido)
            return false;

        // ¿Se ve de verdad? Basta con que se vea el centro o la parte de arriba.
        return SeVe(limites.center) || SeVe(new Vector3(limites.center.x, limites.max.y, limites.center.z));
    }

    bool SeVe(Vector3 punto)
    {
        Vector3 hacia = punto - cabeza.position;
        float distancia = hacia.magnitude;
        // Los triggers se ignoran para que las manos u otros objetos invisibles no bloqueen la mirada.
        if (!Physics.Raycast(cabeza.position, hacia / distancia, out RaycastHit hit, distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return true;
        // Lo que esté pegado al personaje (el producto donde está apoyado) no cuenta como estorbo.
        return hit.transform == transform || hit.transform.IsChildOf(transform) || hit.distance > distancia - 0.2f;
    }

    Bounds Limites()
    {
        if (misRenderers.Length == 0 || misRenderers[0] == null)
            return new Bounds(transform.position, Vector3.one * 0.1f);
        Bounds b = misRenderers[0].bounds;
        for (int i = 1; i < misRenderers.Length; i++)
            if (misRenderers[i] != null)
                b.Encapsulate(misRenderers[i].bounds);
        return b;
    }

    bool LoEstanTocando()
    {
        return ManoCerca(manoIzquierda) || ManoCerca(manoDerecha);
    }

    bool ManoCerca(Transform mano)
    {
        if (mano == null)
            return false;

        foreach (var col in misColliders)
        {
            if (col == null || !col.enabled)
                continue;
            Vector3 puntoMasCercano = col.ClosestPoint(mano.position);
            if (Vector3.Distance(puntoMasCercano, mano.position) <= distanciaToque)
                return true;
        }
        return false;
    }

    void Encontrado()
    {
        Activo = false;
        tiempoMirando = 0f;
        var animacion = GetComponent<AnimacionPajaro>();
        if (animacion != null)
            animacion.Celebrar(); // ¡levanta las alas de alegría!
        if (rutinaSacudida != null)
        {
            StopCoroutine(rutinaSacudida);
            rutinaSacudida = null;
        }
        if (rutinaSalto != null)
            StopCoroutine(rutinaSalto);
        rutinaSalto = StartCoroutine(SaltarALaMano());
    }

    IEnumerator SaltarALaMano()
    {
        Quieto();
        Vector3 inicio = transform.position;
        Quaternion rotacionInicio = transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.05f, duracionSalto));
            float k = Mathf.SmoothStep(0f, 1f, t);
            // El destino se recalcula cada cuadro: si mueves la mano, te sigue.
            Vector3 destino = PuntoEnLaMano();
            transform.position = Vector3.Lerp(inicio, destino, k) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * alturaSalto);
            transform.rotation = rotacionInicio * Quaternion.Euler(0f, 360f * k, 0f);
            yield return null;
        }

        mezclaColor = 0f;
        AplicarColor(0f);
        rutinaSalto = null;

        var agarre = GetComponent<AgarreAntigravedad>();
        if (agarre != null)
            agarre.Activar();
        alSerEncontrado.Invoke();
    }

    OVRSkeleton esqueletoDerecho;

    Vector3 PuntoEnLaMano()
    {
        // Mejor caso: justo encima de la palma de la mano derecha.
        if (esqueletoDerecho == null)
            esqueletoDerecho = ManosUtil.BuscarEnAncla<OVRSkeleton>(manoDerecha);
        var palma = ManosUtil.LeerPalma(esqueletoDerecho, false);
        if (palma.valida && OVRInput.GetControllerPositionTracked(OVRInput.Controller.RHand))
            return palma.centro + palma.normal * (Limites().extents.y + 0.03f);

        bool manoVisible = OVRInput.GetControllerPositionTracked(OVRInput.Controller.RHand)
                           || OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch);
        if (manoDerecha != null && manoVisible)
            return manoDerecha.position + Vector3.up * alturaSobreMano;

        // Si no se ve la mano derecha: frente al jugador, un poco a la derecha y abajo.
        Vector3 frente = cabeza.forward; frente.y = 0f; frente.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, frente);
        return cabeza.position + frente * 0.4f + derecha * 0.15f + Vector3.down * 0.3f;
    }

    // Lo deja quieto (sin gravedad) mientras está escondido o saltando.
    void Quieto()
    {
        if (cuerpo != null)
            cuerpo.isKinematic = true;
    }

    // Deja el personaje como al principio para una nueva partida.
    public void Reiniciar()
    {
        if (rutinaSalto != null)
        {
            StopCoroutine(rutinaSalto);
            rutinaSalto = null;
        }
        if (rutinaSacudida != null)
        {
            StopCoroutine(rutinaSacudida);
            rutinaSacudida = null;
        }
        var agarre = GetComponent<AgarreAntigravedad>();
        if (agarre != null)
            agarre.Desactivar();
        var animacion = GetComponent<AnimacionPajaro>();
        if (animacion != null)
            animacion.Reposo();
        Quieto();
        tiempoMirando = 0f;
        transform.localScale = escalaOriginal;
        mezclaColor = 0f;
        AplicarColor(0f);
    }

    void AplicarColor(float mezcla)
    {
        for (int i = 0; i < misRenderers.Length; i++)
        {
            var r = misRenderers[i];
            if (r == null)
                continue;
            Color c = Color.Lerp(coloresOriginales[i], colorAlMirar, mezcla);
            r.GetPropertyBlock(bloque);
            bloque.SetColor(idBaseColor, c);
            bloque.SetColor(idColor, c);
            r.SetPropertyBlock(bloque);
        }
    }
}

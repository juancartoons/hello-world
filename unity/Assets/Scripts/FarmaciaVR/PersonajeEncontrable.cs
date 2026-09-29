using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

// Va en el personaje escondido. Detecta cuando el jugador lo encuentra:
// - mirándolo fijamente unos segundos, o
// - acercando la mano (tocándolo).
// El personaje necesita un Collider (no trigger) para poder ser "mirado".
public class PersonajeEncontrable : MonoBehaviour
{
    [Header("Encontrar mirando")]
    [Tooltip("Segundos que hay que mirarlo fijamente")]
    public float segundosMirando = 1.5f;
    [Tooltip("Distancia máxima (metros) desde la que cuenta la mirada")]
    public float distanciaMaxima = 12f;
    [Tooltip("Qué tan 'gruesa' es la mirada. Más grande = más fácil")]
    public float radioMirada = 0.15f;

    [Header("Encontrar tocando")]
    [Tooltip("Distancia (metros) entre la mano y el personaje para contar como toque")]
    public float distanciaToque = 0.3f;

    [Header("Efecto mientras lo miran")]
    [Tooltip("Color que toma apenas lo miras (como el 'hover' de un botón)")]
    public Color colorAlMirar = new Color(1f, 0.85f, 0.1f);
    [Tooltip("Qué tan rápido cambia de color")]
    public float velocidadColor = 8f;
    [Tooltip("Cuánto crece mientras lo miran (0.25 = 25%)")]
    public float crecimientoMaximo = 0.25f;

    [Header("Evento")]
    public UnityEvent alSerEncontrado;

    // El JuegoManager lo activa solo mientras se está buscando.
    public bool Activo { get; set; }

    Transform cabeza, manoIzquierda, manoDerecha;
    Collider[] misColliders;
    Vector3 escalaOriginal;
    float tiempoMirando;

    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor"); // URP
    static readonly int idColor = Shader.PropertyToID("_Color");         // Built-in
    Renderer[] misRenderers;
    Color[] coloresOriginales;
    MaterialPropertyBlock bloque;
    float mezclaColor;

    void Awake()
    {
        escalaOriginal = transform.localScale;
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

        // Cambia de color apenas lo miras; el crecimiento va con el tiempo mirando.
        mezclaColor = Mathf.MoveTowards(mezclaColor, mirando ? 1f : 0f, Time.deltaTime * velocidadColor);
        AplicarColor(mezclaColor);

        float progreso = Mathf.Clamp01(tiempoMirando / segundosMirando);
        transform.localScale = escalaOriginal * (1f + crecimientoMaximo * progreso);

        if (progreso >= 1f)
            Encontrado();
    }

    bool LoEstanMirando()
    {
        // Los triggers se ignoran para que las manos u otros objetos invisibles no bloqueen la mirada.
        // Las góndolas y paredes sí la bloquean: hay que verlo de verdad.
        if (Physics.SphereCast(cabeza.position, radioMirada, cabeza.forward, out RaycastHit hit,
                distanciaMaxima, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }
        return false;
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
        alSerEncontrado.Invoke();
    }

    // Deja el personaje como al principio para una nueva partida.
    public void Reiniciar()
    {
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

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

    void Awake()
    {
        escalaOriginal = transform.localScale;
        misColliders = GetComponentsInChildren<Collider>();
        if (misColliders.Length == 0)
            Debug.LogWarning("PersonajeEncontrable: el personaje no tiene Collider, no se podrá encontrar mirándolo.", this);
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

        if (LoEstanMirando())
            tiempoMirando += Time.deltaTime;
        else
            tiempoMirando = Mathf.Max(0f, tiempoMirando - Time.deltaTime * 2f);

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
    }
}

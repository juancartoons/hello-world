using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

// Va en el personaje escondido. Detecta cuando el jugador lo encuentra:
// - mirándolo fijamente unos segundos (cambia de color mientras lo miras), o
// - acercando la mano (tocándolo).
// Al encontrarlo, salta hasta la mano derecha del jugador y queda flotando ahí para poder agarrarlo.
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

    [Header("Al encontrarlo: salta a la mano derecha")]
    public float duracionSalto = 0.8f;
    [Tooltip("Altura extra (metros) del arco del salto")]
    public float alturaSalto = 0.35f;
    [Tooltip("Qué tan arriba de la mano queda flotando (metros)")]
    public float alturaSobreMano = 0.1f;

    [Header("Evento (se dispara cuando llega a la mano)")]
    public UnityEvent alSerEncontrado;

    // El JuegoManager lo activa solo mientras se está buscando.
    public bool Activo { get; set; }

    Transform cabeza, manoIzquierda, manoDerecha;
    Collider[] misColliders;
    Rigidbody cuerpo;
    Vector3 escalaOriginal;
    float tiempoMirando;
    Coroutine rutinaSalto;

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
        alSerEncontrado.Invoke();
    }

    Vector3 PuntoEnLaMano()
    {
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

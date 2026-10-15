using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Botón 3D del panel: se presiona tocándolo con la punta del índice DERECHO.
[RequireComponent(typeof(Collider))]
public class BotonTocable : MonoBehaviour
{
    public UnityEvent alTocar = new UnityEvent();
    [System.NonSerialized] public bool Tocado; // para quien prefiera revisarlo (y bajarlo) después
    public TMP_Text etiqueta;
    public Material materialNormal;
    public Material materialMarcado;
    public Color textoNormal = Color.black;
    public Color textoMarcado = Color.white;
    [Tooltip("Distancia (metros) entre la punta del dedo y el botón para presionarlo")]
    public float distancia = 0.01f;
    [Tooltip("Segundos de espera entre toques")]
    public float pausa = 0.6f;
    // false = acercar el dedo no esconde la bolita (por ejemplo, los botoncitos de las imágenes para calcar).
    [System.NonSerialized] public bool esconderDedo = true;
    // v42: su ETIQUETA al acercar el dedo (ver EtiquetasBotones): la palabra de su comando de voz (con microfonito)
    // o un nombre (en español); si no tiene ninguno, lo que dice el botón.
    [System.NonSerialized] public ControlManos.ComandoVoz comandoVoz = ControlManos.ComandoVoz.Nada;
    [System.NonSerialized] public string nombre;
    // v43: TOQUE A PROPÓSITO (las capas). > 0: el dedo tiene que llegar DE FRENTE (encima del botón y todavía
    // delante de él; no de lado, ni porque el botón se movió hasta el dedo) y hundirse estos metros DETRÁS de su cara.
    // Se ve a la altura del panel, pero se activa un poco más adentro (la ilusión de profundidad). 0 = como siempre.
    [System.NonSerialized] public float fondoToque;

    // Cerca de cualquier botón (de cualquier menú), la bolita del dedo y el borrador se esconden y no se dibuja.
    const float DistanciaCerca = 0.045f;
    const float DistanciaEtiqueta = 0.03f;   // v42: así de cerca (y quieto un momento) sale su etiqueta
    static int cercaCuadro = -10;
    public static bool DedoCerca => Time.frameCount - cercaCuadro <= 1;

    Collider miCollider;
    Renderer miRenderer;
    Vector3 escalaOriginal;
    float bloqueadoHasta;
    bool dentro, armado;

    void Awake()
    {
        miCollider = GetComponent<Collider>();
        miRenderer = GetComponent<Renderer>();
        if (miRenderer == null)
            miRenderer = GetComponentInChildren<Renderer>();   // v43: los botones redondos (el cilindro va de hijo)
        escalaOriginal = transform.localScale;
    }

    void OnEnable()
    {
        // Al aparecer el panel no se presiona nada hasta que el dedo salga y vuelva a entrar.
        bloqueadoHasta = Time.time + 0.4f;
        dentro = true;
        armado = false;
        if (escalaOriginal != Vector3.zero)
            transform.localScale = escalaOriginal;
    }

    void Update()
    {
        var control = ControlManos.Instancia;
        if (control == null || !control.Der.valida || miCollider == null)
        {
            dentro = false;
            return;
        }
        Vector3 punta = control.Der.indice;
        float d = Vector3.Distance(miCollider.ClosestPoint(punta), punta);
        if (d < DistanciaCerca && esconderDedo)
            cercaCuadro = Time.frameCount;
        if (d < DistanciaEtiqueta)
            ProponerEtiqueta(d);
        bool toca = fondoToque > 0f ? TocaAProposito(punta) : d < distancia;
        if (toca && !dentro && Time.time >= bloqueadoHasta)
        {
            armado = false;
            bloqueadoHasta = Time.time + pausa;
            StartCoroutine(Hundir());
            alTocar.Invoke();
        }
        dentro = toca;
    }

    // v43: ¿el dedo llegó de frente y ya está "fondoToque" detrás de la cara del botón? (Su cara mira hacia -forward.)
    bool TocaAProposito(Vector3 punta)
    {
        Vector3 escala = transform.lossyScale;
        Vector3 rel = punta - transform.position;
        float prof = Vector3.Dot(rel, transform.forward) + Mathf.Abs(escala.z) * 0.5f;   // > 0: detrás de su cara
        float lx = Mathf.Abs(Vector3.Dot(rel, transform.right)) - Mathf.Abs(escala.x) * 0.5f;
        float ly = Mathf.Abs(Vector3.Dot(rel, transform.up)) - Mathf.Abs(escala.y) * 0.5f;
        bool encima = lx < 0.004f && ly < 0.004f;
        if (!encima || prof < -0.05f)
            armado = false;
        else if (prof < -0.002f)
            armado = true;
        return armado && encima && prof >= fondoToque;
    }

    // v42: para que cada menú le ponga su palabra de voz o su nombre a sus botones.
    public static void PonerVoz(BotonTocable b, ControlManos.ComandoVoz comando)
    {
        if (b != null)
            b.comandoVoz = comando;
    }

    public static void PonerNombre(BotonTocable b, string nombreEs)
    {
        if (b != null)
            b.nombre = nombreEs;
    }

    void ProponerEtiqueta(float d)
    {
        string t;
        bool voz = false;
        if (comandoVoz != ControlManos.ComandoVoz.Nada)
        {
            t = ControlManos.NombreComando(comandoVoz);
            voz = true;
        }
        else if (!string.IsNullOrEmpty(nombre))
        {
            t = Idioma.T(nombre);
        }
        else
        {
            t = etiqueta != null ? etiqueta.text : null;
        }
        if (!string.IsNullOrEmpty(t))
            EtiquetasBotones.ProponerEncima(transform, d, t, voz);
    }

    public void Marcar(bool marcado)
    {
        if (miRenderer == null)
            miRenderer = GetComponent<Renderer>();
        var mat = marcado ? materialMarcado : materialNormal;
        if (miRenderer != null && mat != null)
            miRenderer.sharedMaterial = mat;
        if (etiqueta != null)
            etiqueta.color = marcado ? textoMarcado : textoNormal;
    }

    public void PonerTexto(string texto)
    {
        if (etiqueta != null)
            Idioma.Poner(etiqueta, texto); // en el idioma elegido (Español / English)
    }

    // Pequeña animación de "presionado".
    IEnumerator Hundir()
    {
        transform.localScale = new Vector3(escalaOriginal.x * 0.95f, escalaOriginal.y * 0.95f, escalaOriginal.z * 0.5f);
        yield return new WaitForSeconds(0.15f);
        transform.localScale = escalaOriginal;
    }
}

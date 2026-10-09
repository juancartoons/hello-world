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

    // Cerca de cualquier botón (de cualquier menú), la bolita del dedo y el borrador se esconden y no se dibuja.
    const float DistanciaCerca = 0.045f;
    static int cercaCuadro = -10;
    public static bool DedoCerca => Time.frameCount - cercaCuadro <= 1;

    Collider miCollider;
    Renderer miRenderer;
    Vector3 escalaOriginal;
    float bloqueadoHasta;
    bool dentro;

    void Awake()
    {
        miCollider = GetComponent<Collider>();
        miRenderer = GetComponent<Renderer>();
        escalaOriginal = transform.localScale;
    }

    void OnEnable()
    {
        // Al aparecer el panel no se presiona nada hasta que el dedo salga y vuelva a entrar.
        bloqueadoHasta = Time.time + 0.4f;
        dentro = true;
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
        bool toca = d < distancia;
        if (toca && !dentro && Time.time >= bloqueadoHasta)
        {
            bloqueadoHasta = Time.time + pausa;
            StartCoroutine(Hundir());
            alTocar.Invoke();
        }
        dentro = toca;
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

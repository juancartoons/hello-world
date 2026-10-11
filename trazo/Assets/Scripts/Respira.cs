using System.Collections.Generic;
using TMPro;
using UnityEngine;

// UNA INTERFAZ QUE RESPIRA CON TU ATENCIÓN (v36). Se pone en la raíz de un menú (por ahora, la mesa de trabajo):
//  - Si no lo miras, se vuelve discreto: el vidrio más transparente y las letras más suaves.
//  - Al mirarlo (o acercar el dedo) "despierta" suave. Si te acercas a leer, el vidrio se pone más firme.
//  - Al aparecer, sale con un pequeño rebote (como un resorte) y un sonidito fino (cristal o burbuja, según la
//    burbujita secreta de la paleta), que suena desde donde está el menú.
// No sigue tus ojos (el Quest 2 no puede): usa hacia dónde apunta tu cabeza.
public class Respira : MonoBehaviour
{
    [Tooltip("Lo que aparece y desaparece (vacío = este mismo objeto)")]
    public Transform contenido;
    [Tooltip("Grados entre hacia donde miras y el menú para que despierte")]
    public float anguloDespierta = 26f;
    [Tooltip("Qué tan visibles quedan las letras cuando no lo miras (0 a 1)")]
    public float minimoTexto = 0.45f;
    public bool conRebote = true;

    static readonly int idAtencion = Shader.PropertyToID("_Atencion");
    readonly List<Renderer> vidrios = new List<Renderer>();
    readonly List<TMP_Text> textos = new List<TMP_Text>();
    readonly Dictionary<TMP_Text, float> alfaOriginal = new Dictionary<TMP_Text, float>();
    readonly Dictionary<TMP_Text, float> alfaPuesto = new Dictionary<TMP_Text, float>();
    MaterialPropertyBlock bloque;
    AudioSource fuente;
    float atencion = 1f, proximaBusqueda, aparecioEn = -10f, vidrioPuesto = -1f;
    bool estabaActivo, rebotando;
    Vector3 escalaBase = Vector3.one;

    void LateUpdate()
    {
        var control = ControlManos.Instancia;
        var cab = control != null ? control.Cabeza : null;
        Transform c = contenido != null ? contenido : transform;
        bool activo = c.gameObject.activeInHierarchy;
        if (activo && !estabaActivo)
            Aparecer(c);
        if (!activo && estabaActivo)
            Sonar(1.1f, 0.18f); // al guardarse, un "tin" más suave
        if (!activo && rebotando)
            TerminarRebote(c);
        estabaActivo = activo;
        if (!activo || cab == null)
            return;
        if (rebotando)
            Rebote(c);
        if (Time.time >= proximaBusqueda)
            Buscar(c);

        // ¿Lo miras? (con un poquito de margen para que no parpadee)
        Vector3 hacia = c.position - cab.position;
        float ang = Vector3.Angle(cab.forward, hacia);
        bool mira = ang < (atencion > 0.5f ? anguloDespierta + 8f : anguloDespierta);
        bool dedo = control.Der.valida && Vector3.Distance(control.Der.indice, c.position) < 0.3f;
        float objetivo = mira || dedo ? 1f : 0f;
        atencion = Mathf.MoveTowards(atencion, objetivo, Time.deltaTime * (objetivo > atencion ? 4f : 1.5f));
        // Si te acercas a leer (menos de 45 cm), el vidrio se pone firme.
        float cerca = Mathf.InverseLerp(0.75f, 0.45f, hacia.magnitude);
        Aplicar(Mathf.Clamp01(atencion + 0.3f * cerca * atencion), Mathf.Lerp(minimoTexto, 1f, atencion));
    }

    void Buscar(Transform c)
    {
        proximaBusqueda = Time.time + 1f;
        vidrios.Clear();
        foreach (var r in c.GetComponentsInChildren<Renderer>(true))
        {
            var m = r.sharedMaterial;
            if (m != null && m.HasProperty(idAtencion))
                vidrios.Add(r);
        }
        textos.Clear();
        foreach (var t in c.GetComponentsInChildren<TMP_Text>(true))
        {
            textos.Add(t);
            if (!alfaOriginal.ContainsKey(t))
                alfaOriginal[t] = t.color.a;
        }
        vidrioPuesto = -1f; // los nuevos vidrios también reciben su valor
    }

    void Aplicar(float vidrio, float letras)
    {
        if (Mathf.Abs(vidrio - vidrioPuesto) > 0.005f)
        {
            vidrioPuesto = vidrio;
            if (bloque == null)
                bloque = new MaterialPropertyBlock();
            foreach (var r in vidrios)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(bloque);
                bloque.SetFloat(idAtencion, vidrio);
                r.SetPropertyBlock(bloque);
            }
        }
        foreach (var t in textos)
        {
            if (t == null || !t.gameObject.activeInHierarchy)
                continue;
            float baseA;
            if (!alfaOriginal.TryGetValue(t, out baseA))
                baseA = 1f;
            float a = baseA * letras;
            float antes;
            // Si otro código le cambió el color (por ejemplo, un botón marcado), se vuelve a poner.
            bool cambiado = !alfaPuesto.TryGetValue(t, out antes) || Mathf.Abs(t.color.a - antes) > 0.02f;
            if (cambiado || Mathf.Abs(antes - a) > 0.01f)
            {
                t.alpha = a;
                alfaPuesto[t] = a;
            }
        }
    }

    // ---------- Aparecer con resorte y sonidito ----------

    void Aparecer(Transform c)
    {
        atencion = 1f; // al aparecer, despierto
        Buscar(c);
        if (conRebote)
        {
            if (!rebotando)
                escalaBase = c.localScale;
            rebotando = true;
            aparecioEn = Time.time;
            c.localScale = escalaBase * 0.9f;
        }
        Sonar(1.35f, 0.35f);
    }

    // Un sonidito fino que sale desde el menú (cristal o burbuja, según la burbujita secreta de la paleta).
    void Sonar(float tono, float volumen)
    {
        if (fuente == null)
        {
            fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 1f;
            fuente.minDistance = 0.4f;
        }
        fuente.pitch = tono;
        fuente.PlayOneShot(Sonidos.Toque, volumen);
    }

    // Un resorte: empieza un poco chico, se pasa un poquito y se acomoda (en medio segundo).
    void Rebote(Transform c)
    {
        float t = Time.time - aparecioEn;
        if (t >= 0.6f)
        {
            TerminarRebote(c);
            return;
        }
        float f = 1f - 0.1f * Mathf.Exp(-7f * t) * Mathf.Cos(16f * t);
        c.localScale = escalaBase * f;
    }

    void TerminarRebote(Transform c)
    {
        rebotando = false;
        c.localScale = escalaBase;
    }
}

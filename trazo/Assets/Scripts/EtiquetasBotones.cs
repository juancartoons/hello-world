using System.Collections.Generic;
using TMPro;
using UnityEngine;

// ETIQUETAS DE TODOS LOS BOTONES (v42). Como las de la paleta (desde la v38): deja el índice derecho cerquita de un
// botón un momento y sale encima una etiquetita de vidrio con su nombre.
//  - Si ese nombre también se puede DECIR (un comando de voz), la etiqueta lleva un MICROFONITO y es exactamente la
//    palabra que dices ("Capa nueva", "Gira derecha", "Rojo"...). Así se aprende la voz sin tutorial.
//  - Cada cosa propone su etiqueta cada cuadro (los botones de todos los menús, la paleta, las pastillas, el
//    parlante, el micrófono, las flechas de girar): sale la del más cercano al dedo.
// Va después de todo lo demás (DefaultExecutionOrder) para ver las propuestas de este mismo cuadro.
[DefaultExecutionOrder(10000)]
public class EtiquetasBotones : MonoBehaviour
{
    const float Espera = 0.3f;          // segundos con el dedo cerca antes de que salga
    const float Alto = 0.016f;          // alto de la etiqueta (metros)
    const float Icono = 0.0105f;        // alto del microfonito
    const float Margen = 0.005f;
    static readonly Color ColorVoz = new Color(0.16f, 0.62f, 0.58f);

    static EtiquetasBotones instancia;
    static int cuadro = -10;
    static Transform propuesto;
    static float propuestaDistancia;
    static string propuestoTexto;
    static bool propuestaVoz;
    static Vector3 propuestaPos;
    static Quaternion propuestaGiro;

    ControlManos control;
    Transform raiz, icono;
    MeshFilter fondoFiltro;
    TMP_Text texto;
    readonly Dictionary<int, Mesh> pastillas = new Dictionary<int, Mesh>();
    readonly List<Material> materiales = new List<Material>();
    Transform objetivo;
    float desde;
    string textoPuesto;
    bool vozPuesta;

    public static void Crear(ControlManos control)
    {
        if (instancia != null)
            return;
        var go = new GameObject("EtiquetasBotones");
        instancia = go.AddComponent<EtiquetasBotones>();
        instancia.control = control;
    }

    // Una etiqueta candidata para este cuadro (gana la que tiene el dedo más cerca).
    public static void Proponer(Transform objetivo, float distancia, string texto, bool voz, Vector3 pos, Quaternion giro)
    {
        if (objetivo == null || string.IsNullOrEmpty(texto))
            return;
        if (cuadro != Time.frameCount)
        {
            cuadro = Time.frameCount;
            propuesto = null;
            propuestaDistancia = float.MaxValue;
        }
        if (distancia >= propuestaDistancia)
            return;
        propuestaDistancia = distancia;
        propuesto = objetivo;
        propuestoTexto = texto;
        propuestaVoz = voz;
        propuestaPos = pos;
        propuestaGiro = giro;
    }

    // Encima de un botón (un cubo: su escala es su tamaño), un poquito hacia ti.
    public static void ProponerEncima(Transform t, float distancia, string texto, bool voz)
    {
        float medio = Mathf.Abs(t.lossyScale.y) * 0.5f;
        Vector3 pos = t.position + t.up * (medio + Alto * 0.5f + 0.004f) - t.forward * 0.006f;
        Proponer(t, distancia, texto, voz, pos, t.rotation);
    }

    // El texto de un comando de voz (para las pastillas, las flechas, el parlante...).
    public static void ProponerComando(Transform t, float distancia, ControlManos.ComandoVoz comando, Vector3 pos, Quaternion giro)
    {
        Proponer(t, distancia, ControlManos.NombreComando(comando), true, pos, giro);
    }

    void LateUpdate()
    {
        if (control == null)
            control = ControlManos.Instancia;
        Transform t = cuadro == Time.frameCount ? propuesto : null;
        if (control == null || control.Ocupado || !control.Der.valida)
            t = null;
        if (t != objetivo)
        {
            objetivo = t;
            desde = Time.time;
        }
        bool ver = t != null && Time.time - desde >= Espera;
        if (!ver)
        {
            if (raiz != null && raiz.gameObject.activeSelf)
                raiz.gameObject.SetActive(false);
            return;
        }
        if (raiz == null)
            Armar();
        if (!raiz.gameObject.activeSelf)
            raiz.gameObject.SetActive(true);
        if (propuestoTexto != textoPuesto || propuestaVoz != vozPuesta)
            Poner(propuestoTexto, propuestaVoz);
        raiz.SetPositionAndRotation(propuestaPos, propuestaGiro);
    }

    // El texto y el ancho (según cuántas letras); con voz, el microfonito a la izquierda.
    void Poner(string t, bool voz)
    {
        textoPuesto = t;
        vozPuesta = voz;
        float extra = voz ? Icono * 0.7f + 0.004f : 0f;
        float ancho = Mathf.Clamp(0.016f + 0.0058f * t.Length + extra, 0.045f, 0.16f);
        int mm = Mathf.RoundToInt(ancho * 1000f);
        Mesh m;
        if (!pastillas.TryGetValue(mm, out m))
        {
            m = Persiana.CrearPastilla(mm / 1000f, Alto);
            pastillas[mm] = m;
        }
        fondoFiltro.sharedMesh = m;
        texto.text = t;
        texto.rectTransform.sizeDelta = new Vector2(ancho - Margen * 2f - extra, Alto * 0.7f);
        texto.transform.localPosition = new Vector3(extra * 0.5f, 0f, -0.001f);
        icono.gameObject.SetActive(voz);
        if (voz)
            icono.localPosition = new Vector3(-ancho * 0.5f + Margen + Icono * 0.35f, -0.03f * (Icono / 0.46f), -0.001f);
    }

    void Armar()
    {
        raiz = new GameObject("Etiqueta").transform;
        raiz.SetParent(transform, false);
        var fondo = new GameObject("Fondo");
        fondo.transform.SetParent(raiz, false);
        fondoFiltro = fondo.AddComponent<MeshFilter>();
        var r = fondo.AddComponent<MeshRenderer>();
        Material vidrio = null;
        if (control != null && control.materialVidrio != null)
        {
            vidrio = new Material(control.materialVidrio);
            if (vidrio.HasProperty("_Esmerilado"))
                vidrio.SetFloat("_Esmerilado", 0.85f);
            if (vidrio.HasProperty("_Opacidad"))
                vidrio.SetFloat("_Opacidad", 0.92f);
        }
        else if (control != null && control.materialNodo != null)
        {
            vidrio = Tenido(control.materialNodo, new Color(0.96f, 0.96f, 0.97f));
        }
        if (vidrio != null)
        {
            materiales.Add(vidrio);
            r.sharedMaterial = vidrio;
        }
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(raiz, false);
        var t = go.AddComponent<TextMeshPro>();
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.005f;
        t.fontSizeMax = 0.1f;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.color = new Color(0.12f, 0.12f, 0.16f);
        texto = t;

        icono = new GameObject("Microfono").transform;
        icono.SetParent(raiz, false);
        icono.localScale = Vector3.one * (Icono / 0.46f);
        icono.gameObject.AddComponent<MeshFilter>().sharedMesh = BotonVoz.MallaMicrofono();
        var ri = icono.gameObject.AddComponent<MeshRenderer>();
        if (control != null && control.materialNodo != null)
        {
            var mi = Tenido(control.materialNodo, ColorVoz);
            materiales.Add(mi);
            ri.sharedMaterial = mi;
        }
        ri.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ri.receiveShadows = false;
        raiz.gameObject.SetActive(false);
    }

    static Material Tenido(Material baseMat, Color c)
    {
        var m = new Material(baseMat);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        return m;
    }

    void OnDestroy()
    {
        foreach (var m in materiales)
            if (m != null)
                Destroy(m);
        foreach (var m in pastillas.Values)
            if (m != null)
                Destroy(m);
        if (instancia == this)
            instancia = null;
    }
}

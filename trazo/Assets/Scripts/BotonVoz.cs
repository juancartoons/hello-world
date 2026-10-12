using System.Collections.Generic;
using TMPro;
using UnityEngine;

// EL MICRÓFONO DE LA VOZ (v40): arriba a la IZQUIERDA de lo que miras, en espejo del parlante (mismo tamaño).
//  - Tócalo con la punta del índice derecho: prende o apaga la voz (Voz.cs). Se recuerda.
//      Prendida y escuchando: verde azulado, con un aro que salta cuando entiende algo.
//      Preparándose (la primera vez baja el idioma): naranja. Apagada: gris con una raya.
//  - Con la voz prendida, las letras de cómic de arriba al centro NO salen: en su lugar, a la izquierda y un poco
//    más abajo, un panelito dice "Sugerencia: di en voz alta:" y debajo qué decir según lo que estés haciendo
//    («Dibuja», «Ya», «Nodos»...). Ahí también salen los avisos y lo que la voz entendió.
//  - En la vista limpia no se ve (el panelito solo un momento, cuando entiende algo).
public class BotonVoz : MonoBehaviour
{
    static readonly Color ColorEscucha = new Color(0.16f, 0.68f, 0.64f);
    static readonly Color ColorPrepara = new Color(0.95f, 0.6f, 0.2f);
    static readonly Color ColorApagado = new Color(0.22f, 0.22f, 0.24f);
    // Dónde queda respecto a tu cabeza: a la izquierda, arriba y adelante. v42: un 13 % más hacia el centro.
    public static readonly Vector3 Lugar = new Vector3(-0.226f, 0.13f, 0.45f);
    static readonly Vector3 LugarMicro = new Vector3(0f, -0.042f, 0f);
    const float Escala = 0.82f;
    const float TamanoMicro = 0.04f;
    const float RadioToque = 0.026f;
    // El panelito de las sugerencias: a la izquierda, un poco más abajo de tus ojos.
    static readonly Vector3 LugarPanel = new Vector3(-0.21f, -0.05f, 0.55f);
    const float AnchoPanel = 0.17f, AltoPanel = 0.05f;

    public static bool DedoCerca { get; private set; }

    ControlManos control;
    Transform micro, raya, aro, panel;
    Material matIcono;
    TMP_Text linea1, linea2;
    readonly List<Material> materiales = new List<Material>();
    readonly List<Mesh> mallas = new List<Mesh>();
    Quaternion giroSuave = Quaternion.identity, giroPanel = Quaternion.identity;
    bool colocado, dedoEnMicro;
    float bloqueadoHasta;
    string aviso = "";
    float avisoHora = -10f;
    string puesto1, puesto2;

    public static BotonVoz Crear(ControlManos control, Material baseMaterial)
    {
        var go = new GameObject("BotonVoz");
        var b = go.AddComponent<BotonVoz>();
        b.control = control;
        b.Armar(baseMaterial);
        return b;
    }

    void Armar(Material baseMaterial)
    {
        if (baseMaterial == null)
            return;
        transform.localScale = Vector3.one * Escala;
        micro = new GameObject("Microfono").transform;
        micro.SetParent(transform, false);
        micro.localPosition = LugarMicro;
        micro.localScale = Vector3.one * TamanoMicro;
        var blanco = NuevoMaterial(baseMaterial, Color.white);
        var negro = NuevoMaterial(baseMaterial, Color.black);
        var gris = NuevoMaterial(baseMaterial, new Color(0.35f, 0.35f, 0.38f));
        matIcono = NuevoMaterial(baseMaterial, ColorApagado);
        // Fondo redondo blanco con borde negro (igual que el parlante).
        Parte(micro, "Borde", Disco(0.62f), negro, 0.1f);
        Parte(micro, "Fondo", Disco(0.56f), blanco, 0.05f);
        // El micrófono: la cápsula, el soporte en U, el palito y la base.
        var forma = new List<Vector2[]>();
        forma.Add(Capsula(0.09f, 0.05f, 0.17f));
        ArcoU(forma, new Vector2(0f, 0.09f), 0.155f, 0.035f);
        Barra(forma, new Vector2(0f, -0.065f), new Vector2(0f, -0.18f), 0.035f);
        Barra(forma, new Vector2(-0.1f, -0.18f), new Vector2(0.1f, -0.18f), 0.04f);
        Parte(micro, "Icono", Malla(forma), matIcono, 0f);
        // La raya de "apagado".
        var r = new List<Vector2[]>();
        Barra(r, new Vector2(-0.26f, -0.26f), new Vector2(0.26f, 0.32f), 0.055f);
        raya = Parte(micro, "Raya", Malla(r), gris, -0.01f);
        // El aro de "te escucho" (salta cuando entiende algo).
        aro = Parte(micro, "Aro", Anillo(0.66f, 0.74f), matIcono, 0.05f);
        ArmarPanel();
        Refrescar();
    }

    void ArmarPanel()
    {
        panel = new GameObject("PanelVoz").transform;
        var fondo = new GameObject("Fondo");
        fondo.transform.SetParent(panel, false);
        var malla = Persiana.CrearPastilla(AnchoPanel, AltoPanel);
        mallas.Add(malla);
        fondo.AddComponent<MeshFilter>().sharedMesh = malla;
        var mr = fondo.AddComponent<MeshRenderer>();
        Material m;
        if (control != null && control.materialVidrio != null)
        {
            m = new Material(control.materialVidrio);
            if (m.HasProperty("_Esmerilado"))
                m.SetFloat("_Esmerilado", 0.8f);
            if (m.HasProperty("_Opacidad"))
                m.SetFloat("_Opacidad", 0.88f);
            materiales.Add(m);
        }
        else
        {
            m = NuevoMaterial(control != null ? control.materialNodo : null, new Color(0.96f, 0.96f, 0.97f));
        }
        if (m != null)
            mr.sharedMaterial = m;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        linea1 = Texto("Linea1", new Vector3(0f, AltoPanel * 0.22f, -0.001f), new Vector2(AnchoPanel * 0.9f, AltoPanel * 0.3f),
                       new Color(0.32f, 0.33f, 0.38f), FontStyles.Normal);
        linea2 = Texto("Linea2", new Vector3(0f, -AltoPanel * 0.15f, -0.001f), new Vector2(AnchoPanel * 0.88f, AltoPanel * 0.45f),
                       new Color(0.1f, 0.1f, 0.14f), FontStyles.Bold);
        panel.gameObject.SetActive(false);
    }

    TMP_Text Texto(string nombre, Vector3 pos, Vector2 tam, Color color, FontStyles estilo)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(panel, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.005f;
        t.fontSizeMax = 0.2f;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = estilo;
        t.color = color;
        t.rectTransform.sizeDelta = tam;
        t.text = "";
        return t;
    }

    void OnEnable()
    {
        if (control != null && control.dibujo != null)
            control.dibujo.alMensaje += AlMensaje;
    }

    void Start()
    {
        // (Armar corre antes de que el objeto se prenda la primera vez: aquí ya hay dibujo seguro.)
        if (control != null && control.dibujo != null)
        {
            control.dibujo.alMensaje -= AlMensaje;
            control.dibujo.alMensaje += AlMensaje;
        }
    }

    void OnDisable()
    {
        if (control != null && control.dibujo != null)
            control.dibujo.alMensaje -= AlMensaje;
    }

    void AlMensaje(string texto)
    {
        aviso = texto;
        avisoHora = Time.time;
    }

    void LateUpdate()
    {
        if (control == null)
            control = ControlManos.Instancia;
        if (control == null || control.Cabeza == null || matIcono == null)
        {
            DedoCerca = false;
            return;
        }
        Transform cabeza = control.Cabeza;
        Vector3 adelante = cabeza.forward;
        if (Mathf.Abs(Vector3.Dot(adelante, Vector3.up)) > 0.97f)
            adelante = Vector3.ProjectOnPlane(cabeza.up, Vector3.up);
        Quaternion giro = Quaternion.LookRotation(adelante, Vector3.up);
        if (!colocado)
        {
            giroSuave = giroPanel = giro;
            colocado = true;
        }
        giroSuave = Quaternion.Slerp(giroSuave, giro, 1f - Mathf.Exp(-4f * Time.deltaTime));
        giroPanel = Quaternion.Slerp(giroPanel, giro, 1f - Mathf.Exp(-3f * Time.deltaTime));
        ActualizarPanel(cabeza);

        // En la vista limpia el micrófono no se ve (ni se puede tocar).
        if (VistaLimpia.Activa)
        {
            DedoCerca = false;
            if (micro.gameObject.activeSelf)
                micro.gameObject.SetActive(false);
            return;
        }
        if (!micro.gameObject.activeSelf)
            micro.gameObject.SetActive(true);
        Vector3 pos = cabeza.position + giroSuave * Lugar;
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cabeza.position, Vector3.up));
        Refrescar();

        bool dedo = control.Der.valida && control.GestoIzq != ControlManos.Gesto.Transformar;
        Vector3 punta = control.Der.indice;
        Vector3 local = transform.InverseTransformPoint(punta);
        DedoCerca = dedo && Mathf.Abs(local.x) < 0.06f && local.y > -0.075f && local.y < -0.005f && Mathf.Abs(local.z) < 0.07f;
        // v42: su etiqueta, debajo (como la del parlante). El micrófono no se prende con la voz: se toca.
        float alMicro = Vector3.Distance(punta, micro.position);
        if (dedo && alMicro < RadioToque * Escala + 0.02f)
            EtiquetasBotones.Proponer(micro, alMicro, Idioma.T(Voz.Activa ? "Apagar la voz" : "Prender la voz"), false,
                                      micro.position - transform.up * 0.032f - transform.forward * 0.006f, transform.rotation);
        bool toca = dedo && Vector3.Distance(punta, micro.position) < RadioToque * Escala;
        if (toca && !dedoEnMicro && Time.time >= bloqueadoHasta)
        {
            bloqueadoHasta = Time.time + 0.6f;
            if (Voz.Instancia != null)
                Voz.Instancia.Alternar();
            var fuente = GetComponent<AudioSource>();
            if (fuente == null)
            {
                fuente = gameObject.AddComponent<AudioSource>();
                fuente.playOnAwake = false;
                fuente.spatialBlend = 0f;
            }
            if (!BotonSonido.Silenciado)
                fuente.PlayOneShot(Sonidos.Toque, 0.6f);
        }
        dedoEnMicro = toca;
    }

    void Refrescar()
    {
        bool activa = Voz.Activa, escucha = Voz.Escuchando;
        Color c = escucha ? ColorEscucha : activa ? ColorPrepara : ColorApagado;
        if (!escucha && activa)
            c = Color.Lerp(ColorPrepara, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.time * 6f)); // parpadea mientras se prepara
        if (matIcono.HasProperty("_BaseColor"))
            matIcono.SetColor("_BaseColor", c);
        if (matIcono.HasProperty("_Color"))
            matIcono.SetColor("_Color", c);
        if (raya.gameObject.activeSelf == activa)
            raya.gameObject.SetActive(!activa);
        if (aro.gameObject.activeSelf != escucha)
            aro.gameObject.SetActive(escucha);
        if (escucha)
        {
            float salto = Mathf.Clamp01(1f - (Time.time - Voz.UltimoOido) / 0.4f);
            aro.localScale = Vector3.one * (1f + 0.18f * salto);
        }
    }

    // ---------- El panelito de las sugerencias (reemplaza las letras de cómic mientras la voz está prendida) ----------

    void ActualizarPanel(Transform cabeza)
    {
        bool reciente = Time.time - ControlManos.UltimoComandoHora < 1.6f;
        bool avisoReciente = Time.time - avisoHora < 2.5f;
        bool ver = Voz.Activa && (!VistaLimpia.Activa || reciente);
        if (panel.gameObject.activeSelf != ver)
            panel.gameObject.SetActive(ver);
        if (!ver)
            return;
        Vector3 pos = cabeza.position + giroPanel * LugarPanel;
        panel.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cabeza.position, Vector3.up));

        string uno, dos;
        Color color2 = new Color(0.1f, 0.1f, 0.14f);
        string situacion = Voz.Situacion;
        if (!string.IsNullOrEmpty(situacion))
            uno = Idioma.T(situacion);
        else if (avisoReciente)
            uno = aviso;
        else
            uno = Idioma.T("Sugerencia: di en voz alta:");
        if (reciente)
        {
            dos = "«" + ControlManos.UltimoComandoVoz + "»";
            color2 = new Color(0.1f, 0.5f, 0.45f);
        }
        else if (!string.IsNullOrEmpty(situacion) || !Voz.Escuchando)
        {
            dos = "";
        }
        else
        {
            dos = "«" + control.SugerenciaVoz() + "»";
        }
        if (uno != puesto1)
        {
            puesto1 = uno;
            linea1.text = uno;
        }
        if (dos != puesto2)
        {
            puesto2 = dos;
            linea2.text = dos;
        }
        if (linea2.color != color2)
            linea2.color = color2;
    }

    // ---------- Mallas sencillas (planas, se ven por los dos lados) ----------

    Material NuevoMaterial(Material baseMaterial, Color c)
    {
        if (baseMaterial == null)
            return null;
        var m = new Material(baseMaterial);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        materiales.Add(m);
        return m;
    }

    // "atras" = cuánto más lejos de ti (en el tamaño del ícono): así las capas no se pelean.
    Transform Parte(Transform padre, string nombre, Mesh malla, Material m, float atras)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(0f, 0f, atras);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    Mesh Disco(float radio)
    {
        const int lados = 28;
        var pts = new Vector2[lados];
        for (int i = 0; i < lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio;
        }
        return Malla(new List<Vector2[]> { pts });
    }

    Mesh Anillo(float adentro, float afuera)
    {
        const int lados = 32;
        var partes = new List<Vector2[]>();
        for (int i = 0; i < lados; i++)
        {
            float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            partes.Add(new[] { d0 * adentro, d0 * afuera, d1 * afuera, d1 * adentro });
        }
        return Malla(partes);
    }

    // La cápsula del micrófono (convexa): media vuelta arriba en y1 y media vuelta abajo en y0.
    static Vector2[] Capsula(float radio, float y0, float y1)
    {
        const int pasos = 8;
        var p = new List<Vector2>();
        for (int k = 0; k <= pasos; k++)
        {
            float a = Mathf.PI * k / pasos;                      // arriba: de 0° a 180°
            p.Add(new Vector2(Mathf.Cos(a) * radio, y1 + Mathf.Sin(a) * radio));
        }
        for (int k = 0; k <= pasos; k++)
        {
            float a = Mathf.PI + Mathf.PI * k / pasos;           // abajo: de 180° a 360°
            p.Add(new Vector2(Mathf.Cos(a) * radio, y0 + Mathf.Sin(a) * radio));
        }
        return p.ToArray();
    }

    // El soporte en U alrededor de la cápsula (de 200° a 340°), hecho con cuadritos.
    static void ArcoU(List<Vector2[]> partes, Vector2 centro, float radio, float grosor)
    {
        const int pasos = 10;
        for (int i = 0; i < pasos; i++)
        {
            float a0 = Mathf.Lerp(190f, 350f, i / (float)pasos) * Mathf.Deg2Rad;
            float a1 = Mathf.Lerp(190f, 350f, (i + 1) / (float)pasos) * Mathf.Deg2Rad;
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
            Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            partes.Add(new[] { centro + d0 * (radio - grosor * 0.5f), centro + d0 * (radio + grosor * 0.5f),
                               centro + d1 * (radio + grosor * 0.5f), centro + d1 * (radio - grosor * 0.5f) });
        }
    }

    static void Barra(List<Vector2[]> partes, Vector2 a, Vector2 b, float grosor)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x) * grosor * 0.5f;
        partes.Add(new[] { a - n, a + n, b + n, b - n });
    }

    // Cada parte es un polígono convexo (se rellena en abanico), por los dos lados.
    Mesh Malla(List<Vector2[]> partes)
    {
        var m = ArmarMalla(partes);
        mallas.Add(m);
        return m;
    }

    static Mesh mallaMicrofono;

    // v42: el dibujito del micrófono, para las etiquetas de los botones que también se pueden decir.
    // Mide unos 0.31 de ancho y 0.46 de alto (de y = -0.2 a 0.26).
    public static Mesh MallaMicrofono()
    {
        if (mallaMicrofono != null)
            return mallaMicrofono;
        var forma = new List<Vector2[]>();
        forma.Add(Capsula(0.09f, 0.05f, 0.17f));
        ArcoU(forma, new Vector2(0f, 0.09f), 0.155f, 0.035f);
        Barra(forma, new Vector2(0f, -0.065f), new Vector2(0f, -0.18f), 0.035f);
        Barra(forma, new Vector2(-0.1f, -0.18f), new Vector2(0.1f, -0.18f), 0.04f);
        mallaMicrofono = ArmarMalla(forma);
        return mallaMicrofono;
    }

    static Mesh ArmarMalla(List<Vector2[]> partes)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        foreach (var p in partes)
        {
            int b = v.Count;
            foreach (var q in p)
                v.Add(new Vector3(q.x, q.y, 0f));
            for (int i = 1; i < p.Length - 1; i++)
            {
                t.Add(b); t.Add(b + i); t.Add(b + i + 1);
                t.Add(b); t.Add(b + i + 1); t.Add(b + i);
            }
        }
        var m = new Mesh { name = "IconoVoz" };
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    void OnDestroy()
    {
        foreach (var m in materiales)
            if (m != null)
                Destroy(m);
        foreach (var m in mallas)
            if (m != null)
                Destroy(m);
        if (panel != null)
            Destroy(panel.gameObject);
        DedoCerca = false;
    }
}

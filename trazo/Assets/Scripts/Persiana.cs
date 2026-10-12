using System.Collections.Generic;
using TMPro;
using UnityEngine;

// PERSIANAS (v38): una pastillita de vidrio que tocas con el índice derecho y jalas para abrir algo, como una persiana.
//  - ARRIBA: "Capa 2 · de 5". Tócala y BAJA la mano: se despliegan las capas.
//  - ABAJO: "Fotograma 12 · 0:01" (y una rayita de cuánto va de la animación). Tócala y SUBE la mano: sale la mesa de
//    trabajo (reproductor y línea de tiempo, con sus 3 pestañas guardadas).
//  - Sin texto también sirve de "tirador" para cerrar: debajo de las capas (súbelo) y debajo de la mesa (bájalo).
//  - v39: solo persiana (se quitó lo de mirar arriba / abajo y su circulito). En la VISTA LIMPIA las pastillas con
//    "seEsconde" no se ven (ver VistaLimpia.cs).
//  - v41: PANELES FLEXIBLES. Las pastillas con "alMoverA" se llevan con un PELLIZCO (sin jalarlas) a la orilla de
//    arriba o de abajo (ver PanelesFlexibles.cs); al soltarlas vuelan suave a su sitio. Con "arriba", la pastilla
//    sabe en qué orilla está: arriba se jala hacia abajo; abajo, hacia arriba.
// Mientras la jalas, una lámina de vidrio sale de la pastillita siguiendo tu dedo. Si la sueltas a más de la mitad,
// se abre; si no, vuelve.
public class Persiana : MonoBehaviour
{
    public delegate void FuncionPose(Transform cabeza, out Vector3 pos, out Quaternion rot);

    public FuncionPose pose;               // dónde va (se pregunta cada cuadro)
    public System.Func<bool> visible;      // si se ve
    public System.Func<string> texto;      // su texto (null = tirador, sin texto)
    public System.Func<float> progreso;    // la rayita de abajo, de 0 a 1 (null = sin rayita)
    public System.Action alAbrir;          // al jalarla lo suficiente
    public System.Func<bool> puedeJalar;   // si ahora se puede agarrar (null = siempre; por ejemplo, no mientras dibujas)
    public int sentido = -1;               // -1: se jala hacia abajo; +1: hacia arriba
    public bool seEsconde;                 // true = no se ve en la vista limpia (las pastillas fijas)
    public float ancho = 0.085f, alto = 0.022f;
    public System.Func<bool> arriba;       // v41: ¿está en la orilla de arriba? (null = usa "sentido" tal cual)
    public System.Action<bool> alMoverA;   // v41: se mueve con un pellizco; al soltarla: ¿quedó arriba? (null = fija)
    public ControlManos.ComandoVoz comandoVoz;   // v42: su etiqueta al acercar el dedo (la palabra que se dice)

    const float Recorrido = 0.09f;         // cuánto hay que jalar (metros)
    const float CambiaOrilla = 0.08f;      // v41: llevarla así de arriba o de abajo la cambia de orilla (metros)
    const float Vuelo = 0.3f;              // segundos que tarda en volar a su sitio

    static readonly List<Persiana> todas = new List<Persiana>();

    Transform lamina, barra;
    Renderer rFondo, rLamina;
    TMP_Text etiqueta;
    MaterialPropertyBlock bloque;
    readonly List<Material> materiales = new List<Material>();
    readonly List<Mesh> mallas = new List<Mesh>();
    bool armada, agarrada, esperarSalir;
    Vector3 inicio, eje;
    float avance, ver, atencion = 0.5f;
    string textoPuesto = "";
    bool movida, tieneObjetivo;            // v41: la llevas con un pellizco
    Vector3 desfaseMovida, inicioMovida, objetivoAntes, vueloDesde;
    Quaternion vueloGiroDesde;
    float vuelo;

    void OnEnable()
    {
        if (!todas.Contains(this))
            todas.Add(this);
    }

    void OnDisable()
    {
        todas.Remove(this);
        if (movida)
            TerminarMovida(ControlManos.Instancia, false);
    }

    // v41: ¿este punto está sobre una pastilla que se puede mover? (así ese pellizco no elige líneas)
    public static bool Contiene(Vector3 punto)
    {
        foreach (var p in todas)
            if (p != null && p.alMoverA != null && p.armada && p.ver > 0.5f && p.Cerca(punto, 0.02f))
                return true;
        return false;
    }

    void Armar(ControlManos control)
    {
        armada = true;
        var malla = CrearPastilla(ancho, alto);
        mallas.Add(malla);
        Material vidrio = null;
        if (control.materialVidrio != null)
        {
            vidrio = new Material(control.materialVidrio);
            if (vidrio.HasProperty("_Esmerilado"))
                vidrio.SetFloat("_Esmerilado", 0.7f);
            if (vidrio.HasProperty("_Opacidad"))
                vidrio.SetFloat("_Opacidad", 0.8f);
            materiales.Add(vidrio);
        }
        else
        {
            vidrio = MaterialColor(control, new Color(0.95f, 0.95f, 0.97f));
        }
        Pieza("Fondo", malla, vidrio, Vector3.zero, Vector3.one, out rFondo);
        lamina = Pieza("Lamina", malla, vidrio, Vector3.zero, Vector3.one, out rLamina);
        lamina.gameObject.SetActive(false);
        if (texto != null)
        {
            var go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, progreso != null ? alto * 0.08f : 0f, -0.001f);
            var t = go.AddComponent<TextMeshPro>();
            t.enableAutoSizing = true;
            t.fontSizeMin = 0.01f;
            t.fontSizeMax = 0.2f;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            t.color = new Color(0.12f, 0.12f, 0.16f);
            t.rectTransform.sizeDelta = new Vector2(ancho * 0.86f, alto * (progreso != null ? 0.55f : 0.7f));
            etiqueta = t;
        }
        Renderer r;
        if (progreso != null)
            barra = Pieza("Barra", ControlManos.MallaDisco(), MaterialColor(control, new Color(0.35f, 0.55f, 0.85f)),
                          new Vector3(0f, -alto * 0.3f, -0.0008f), new Vector3(0.001f, 0.0022f, 1f), out r);
    }

    Material MaterialColor(ControlManos control, Color c)
    {
        if (control.materialNodo == null)
            return null;
        var m = new Material(control.materialNodo);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        materiales.Add(m);
        return m;
    }

    Transform Pieza(string nombre, Mesh malla, Material m, Vector3 pos, Vector3 escala, out Renderer r)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        r = go.AddComponent<MeshRenderer>();
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    void LateUpdate()
    {
        var control = ControlManos.Instancia;
        var cab = control != null ? control.Cabeza : null;
        bool quiere = cab != null && pose != null && (visible == null || visible()) && !(seEsconde && VistaLimpia.Activa);
        if (movida && !quiere)
            TerminarMovida(control, false);
        ver = Mathf.MoveTowards(ver, quiere ? 1f : 0f, Time.deltaTime * 5f);
        if (!quiere && ver <= 0.01f)
        {
            agarrada = false;
            avance = 0f;
            tieneObjetivo = false;
            vuelo = 0f;
            Mostrar(false);
            return;
        }
        if (!armada)
            Armar(control);
        Mostrar(true);
        if (arriba != null && !agarrada)
            sentido = arriba() ? -1 : 1;
        Vector3 p;
        Quaternion q;
        pose(cab, out p, out q);
        // v41: si su sitio salta (cambió de orilla o se apartó para la otra pastilla), vuela suave hasta allá.
        if (tieneObjetivo && !movida && (p - objetivoAntes).sqrMagnitude > 0.06f * 0.06f)
            EmpezarVuelo();
        objetivoAntes = p;
        tieneObjetivo = true;
        if (movida)
        {
            Vector3 m = control.Der.PuntoPellizco + desfaseMovida;
            Vector3 haciaOjos = m - cab.position;
            transform.SetPositionAndRotation(m, haciaOjos.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(haciaOjos, Vector3.up) : q);
        }
        else if (!agarrada)
        {
            if (vuelo > 0f)
            {
                vuelo = Mathf.MoveTowards(vuelo, 0f, Time.deltaTime / Vuelo);
                float k = vuelo * vuelo * (3f - 2f * vuelo);
                p = Vector3.Lerp(p, vueloDesde, k);
                q = Quaternion.Slerp(q, vueloGiroDesde, k);
            }
            transform.SetPositionAndRotation(p, q);
        }
        transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, ver) * (movida ? 1.12f : 1f);

        if (etiqueta != null)
        {
            string s = texto();
            if (s != textoPuesto)
            {
                textoPuesto = s;
                Idioma.Poner(etiqueta, s);
            }
        }
        if (barra != null)
        {
            float k = Mathf.Clamp01(progreso());
            float largo = ancho * 0.72f;
            barra.localScale = new Vector3(Mathf.Max(0.001f, largo * k), 0.0022f, 1f);
            barra.localPosition = new Vector3(-largo * 0.5f + largo * k * 0.5f, -alto * 0.3f, -0.0008f);
        }
        // Discreta si no la miras; firme si la miras o acercas el dedo.
        bool dedoOk = control.Der.valida && quiere;
        Vector3 dedo = control.Der.indice;
        bool mira = Vector3.Angle(cab.forward, transform.position - cab.position) < 14f;
        bool cerca = dedoOk && Vector3.Distance(dedo, transform.position) < 0.12f;
        atencion = Mathf.MoveTowards(atencion, mira || cerca || agarrada || movida ? 1f : 0f, Time.deltaTime * 3f);
        PonerAtencion(atencion * ver);
        // v42: su etiqueta, encima (la palabra que se dice: "Capas", "Mesa").
        if (comandoVoz != ControlManos.ComandoVoz.Nada && dedoOk && !agarrada && !movida && ver > 0.9f && Cerca(dedo, 0.02f))
            EtiquetasBotones.ProponerComando(transform, Vector3.Distance(dedo, transform.position), comandoVoz,
                                             transform.position + transform.up * (alto * 0.5f + 0.013f) - transform.forward * 0.004f,
                                             transform.rotation);

        // v41: moverla con un pellizco (pulgar + índice derechos sobre ella, sin jalarla).
        if (movida)
        {
            if (!control.Der.valida || !control.Der.pellizco)
                TerminarMovida(control, true);
        }
        else if (alMoverA != null && dedoOk && control.Der.empezoPellizco && !control.Ocupado
                 && control.GestoIzq == ControlManos.Gesto.Ninguno && (puedeJalar == null || puedeJalar())
                 && Cerca(control.Der.PuntoPellizco, 0.02f))
        {
            EmpezarMovida(control);
        }

        // Jalar.
        if (movida)
        {
            avance = 0f;
        }
        else if (!agarrada)
        {
            avance = Mathf.MoveTowards(avance, 0f, Time.deltaTime * 4f);
            if (esperarSalir && (!dedoOk || !Cerca(dedo, 0.04f)))
                esperarSalir = false;
            if (dedoOk && !esperarSalir && !control.Ocupado && (puedeJalar == null || puedeJalar()) && Sobre(dedo))
            {
                agarrada = true;
                inicio = dedo;
                eje = transform.up * sentido;
            }
        }
        else if (!dedoOk)
        {
            Soltar();
        }
        else
        {
            float d = Vector3.Dot(dedo - inicio, eje);
            avance = Mathf.Clamp01(d / Recorrido);
            Vector3 deLado = dedo - inicio - eje * d;
            if (avance >= 1f)
                Abrir(control);
            else if (deLado.magnitude > 0.07f || d < -0.03f)
                Soltar();
        }
        // La lámina que sale siguiendo tu dedo.
        bool verLamina = avance > 0.01f;
        if (lamina.gameObject.activeSelf != verLamina)
            lamina.gameObject.SetActive(verLamina);
        if (verLamina)
        {
            float extra = avance * Recorrido;
            lamina.localScale = new Vector3(1f, 1f + extra / alto, 1f);
            lamina.localPosition = new Vector3(0f, sentido * extra * 0.5f, 0.0004f);
        }
    }

    void EmpezarMovida(ControlManos control)
    {
        movida = true;
        agarrada = false;
        avance = 0f;
        vuelo = 0f;
        desfaseMovida = transform.position - control.Der.PuntoPellizco;
        inicioMovida = transform.position;
        control.Ocupado = true;
        control.SonidoBurbuja(transform.position);
    }

    // Al soltarla: si la llevaste bastante arriba o abajo, cambia de orilla. Siempre vuela suave a su sitio.
    void TerminarMovida(ControlManos control, bool decidir)
    {
        if (!movida)
            return;
        movida = false;
        esperarSalir = true;
        if (control != null)
            control.Ocupado = false;
        EmpezarVuelo();
        if (!decidir || alMoverA == null)
            return;
        float dy = transform.position.y - inicioMovida.y;
        bool estabaArriba = sentido < 0;
        bool quedaArriba = estabaArriba;
        if (dy > CambiaOrilla)
            quedaArriba = true;
        else if (dy < -CambiaOrilla)
            quedaArriba = false;
        if (control != null)
            control.SonidoBurbuja(transform.position);
        alMoverA(quedaArriba);
    }

    void EmpezarVuelo()
    {
        vuelo = 1f;
        vueloDesde = transform.position;
        vueloGiroDesde = transform.rotation;
    }

    void Soltar()
    {
        if (agarrada && avance > 0.5f)
        {
            var control = ControlManos.Instancia;
            if (control != null)
            {
                Abrir(control);
                return;
            }
        }
        agarrada = false;
    }

    void Abrir(ControlManos control)
    {
        agarrada = false;
        avance = 0f;
        esperarSalir = true;
        control.SonidoBurbuja(transform.position);
        if (alAbrir != null)
            alAbrir();
    }

    // ¿El dedo está sobre la pastillita? (en sus coordenadas, con un poquito de margen)
    bool Sobre(Vector3 dedo)
    {
        Vector3 l = transform.InverseTransformPoint(dedo);
        return Mathf.Abs(l.x) < ancho * 0.5f + 0.008f && Mathf.Abs(l.y) < alto * 0.5f + 0.01f && Mathf.Abs(l.z) < 0.025f;
    }

    bool Cerca(Vector3 dedo, float margen)
    {
        Vector3 l = transform.InverseTransformPoint(dedo);
        return Mathf.Abs(l.x) < ancho * 0.5f + margen && Mathf.Abs(l.y) < alto * 0.5f + margen && Mathf.Abs(l.z) < margen + 0.02f;
    }

    void Mostrar(bool si)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var c = transform.GetChild(i).gameObject;
            if (c == (lamina != null ? lamina.gameObject : null))
                continue;
            if (c.activeSelf != si)
                c.SetActive(si);
        }
        if (!si && lamina != null && lamina.gameObject.activeSelf)
            lamina.gameObject.SetActive(false);
    }

    void PonerAtencion(float a)
    {
        if (bloque == null)
            bloque = new MaterialPropertyBlock();
        foreach (var r in new[] { rFondo, rLamina })
        {
            if (r == null || r.sharedMaterial == null || !r.sharedMaterial.HasProperty("_Atencion"))
                continue;
            r.GetPropertyBlock(bloque);
            bloque.SetFloat("_Atencion", a);
            r.SetPropertyBlock(bloque);
        }
        if (etiqueta != null)
        {
            float ta = Mathf.Lerp(0.5f, 1f, a) * ver;
            if (Mathf.Abs(etiqueta.alpha - ta) > 0.01f)
                etiqueta.alpha = ta;
        }
    }

    // Una pastilla (rectángulo con las puntas redondas), con UV de 0 a 1 (para el borde del vidrio).
    public static Mesh CrearPastilla(float ancho, float alto)
    {
        const int pasos = 10;
        float r = alto * 0.5f, medio = Mathf.Max(0f, ancho * 0.5f - r);
        var puntos = new List<Vector3>();
        for (int k = 0; k <= pasos; k++)            // punta derecha (de abajo hacia arriba)
        {
            float a = -Mathf.PI * 0.5f + Mathf.PI * k / pasos;
            puntos.Add(new Vector3(medio + Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
        for (int k = 0; k <= pasos; k++)            // punta izquierda (de arriba hacia abajo)
        {
            float a = Mathf.PI * 0.5f + Mathf.PI * k / pasos;
            puntos.Add(new Vector3(-medio + Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
        var v = new Vector3[puntos.Count + 1];
        var uv = new Vector2[v.Length];
        var n = new Vector3[v.Length];
        v[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < puntos.Count; i++)
        {
            v[i + 1] = puntos[i];
            uv[i + 1] = new Vector2(puntos[i].x / ancho + 0.5f, puntos[i].y / alto + 0.5f);
        }
        for (int i = 0; i < n.Length; i++)
            n[i] = new Vector3(0f, 0f, -1f);
        var tri = new int[puntos.Count * 3];
        for (int i = 0; i < puntos.Count; i++)
        {
            tri[i * 3] = 0;
            tri[i * 3 + 1] = 1 + (i + 1) % puntos.Count;
            tri[i * 3 + 2] = 1 + i;
        }
        var m = new Mesh { name = "Pastilla" };
        m.vertices = v;
        m.uv = uv;
        m.normals = n;
        m.triangles = tri;
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
    }
}

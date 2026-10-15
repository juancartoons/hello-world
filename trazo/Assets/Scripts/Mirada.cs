using System.Collections.Generic;
using TMPro;
using UnityEngine;

// LA MIRADA (v44): un OJO debajo del micrófono. Tócalo (con cualquier índice) o di "mirada": se prende o se apaga.
//  - Prendida, sale una MIRA chiquita en el centro de tu vista (un aro con un puntito). Tiene un IMÁN invisible:
//    no hace falta apuntar exacto; se pega a la línea o al relleno más cercano, y eso BRILLA suave.
//  - La mirada solo SEÑALA. Lo que pasa lo dices tú con la voz (ver ControlManosVoz):
//      "rojo" (o cualquier color)      → lo que brilla se vuelve rojo (si nada brilla: tus próximas líneas).
//      "línea roja" / "relleno rojo"   → solo el contorno o solo el relleno de lo que brilla.
//      "copiar" / "pegar"              → copia el color de lo que miras y lo pone en otra cosa.
//      "borra esto", "oculta esto", "esta capa", "selecciona".
//      En nodos: mira un nodo y di "esquina" (o "alas", "manijas", "tiradores"...): sus tiradores se mueven cada
//      uno por su lado. "Suave" = vuelven a quedar alineados.
//  - En el Quest 2 la mirada es hacia donde apunta tu cabeza (el centro de tu vista).
public class Mirada : MonoBehaviour
{
    const string Clave = "jcartoons_mirada";
    const float Distancia = 0.7f;               // la mira, a esta distancia de tus ojos
    const float ImanLinea = 0.061f;             // tan(3.5°): así de cerca de la mira se "pega" a una línea
    const float ImanFino = 0.021f;              // tan(1.2°): una línea así de cerca le gana a un relleno
    const float ImanNodo = 0.052f;              // tan(3°)
    const float RadioToque = 0.026f;
    static readonly Color ColorPrendida = new Color(0.16f, 0.68f, 0.64f);
    static readonly Color ColorApagada = new Color(0.22f, 0.22f, 0.24f);
    static readonly Color ColorBrilloLinea = new Color(0.45f, 0.85f, 1f);
    static readonly Color ColorBrilloRelleno = new Color(1f, 0.85f, 0.35f);

    public static Mirada Instancia { get; private set; }
    public static bool DedoCerca { get; private set; }
    // Lo que se está señalando (null = nada): una línea, o el relleno de esa línea.
    public static Trazo Objetivo { get; private set; }
    public static bool EsRelleno { get; private set; }
    // El nodo más cercano a la mira (para "esquina"/"suave"), o -1.
    public static Trazo NodoTrazo { get; private set; }
    public static int Nodo { get; private set; } = -1;

    static int activa = -1;
    public static bool Activa
    {
        get
        {
            if (activa < 0)
                activa = PlayerPrefs.GetInt(Clave, 0);
            return activa == 1;
        }
    }

    public static void Alternar()
    {
        activa = Activa ? 0 : 1;
        PlayerPrefs.SetInt(Clave, activa);
        PlayerPrefs.Save();
        var c = ControlManos.Instancia;
        if (c != null && c.dibujo != null)
            c.dibujo.Mensaje(activa == 1 ? "Mirada: Sí · mira algo y di un color, \"copiar\", \"borra esto\"..." : "Mirada: No");
    }

    ControlManos control;
    Transform ojo, raya, mira;
    LineRenderer aro, brillo;
    TMP_Text sugerencia;   // v44: qué decir (junto a la mira)
    Material matIcono, matMira, matBrillo;
    readonly List<Material> materiales = new List<Material>();
    readonly List<Mesh> mallas = new List<Mesh>();
    readonly List<Vector3> puntos = new List<Vector3>();
    readonly List<Vector2> poligono = new List<Vector2>();
    Quaternion giroSuave = Quaternion.identity;
    bool colocado, dedoEnOjo;
    float bloqueadoHasta;

    public static void Crear(ControlManos control, Material baseMaterial)
    {
        if (Instancia != null || baseMaterial == null)
            return;
        var go = new GameObject("Mirada");
        Instancia = go.AddComponent<Mirada>();
        Instancia.control = control;
        Instancia.Armar(baseMaterial);
    }

    // ---------- Armar: el ojo (como el micrófono), la mira y el brillo ----------

    void Armar(Material baseMaterial)
    {
        transform.localScale = Vector3.one * 0.82f;
        ojo = new GameObject("Ojo").transform;
        ojo.SetParent(transform, false);
        ojo.localScale = Vector3.one * 0.04f;
        var blanco = Tenido(baseMaterial, Color.white);
        var negro = Tenido(baseMaterial, Color.black);
        var gris = Tenido(baseMaterial, new Color(0.35f, 0.35f, 0.38f));
        matIcono = Tenido(baseMaterial, ColorApagada);
        Parte(ojo, "Borde", Abanico(Circulo(0.62f, 32)), negro, 0.1f);
        Parte(ojo, "Fondo", Abanico(Circulo(0.56f, 32)), blanco, 0.05f);
        Parte(ojo, "Parpado", Abanico(Almendra(0.4f, 0.24f)), matIcono, 0.02f);
        Parte(ojo, "Blanco", Abanico(Almendra(0.33f, 0.18f)), blanco, 0.01f);
        Parte(ojo, "Iris", Abanico(Circulo(0.13f, 20)), matIcono, 0f);
        Parte(ojo, "Pupila", Abanico(Circulo(0.055f, 16)), negro, -0.005f);
        raya = Parte(ojo, "Raya", Abanico(Barra(new Vector2(-0.3f, -0.3f), new Vector2(0.3f, 0.3f), 0.055f)), gris, -0.01f);

        // La mira: un aro fino y un puntito.
        matMira = Tenido(baseMaterial, ColorPrendida);
        mira = new GameObject("Mira").transform;
        mira.SetParent(null, false);
        aro = mira.gameObject.AddComponent<LineRenderer>();
        aro.useWorldSpace = false;
        aro.loop = true;
        aro.sharedMaterial = matMira;
        aro.widthMultiplier = 0.0012f;
        aro.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        aro.receiveShadows = false;
        aro.positionCount = 32;
        for (int i = 0; i < 32; i++)
        {
            float a = i / 32f * Mathf.PI * 2f;
            aro.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.007f);
        }
        Parte(mira, "Punto", Abanico(Circulo(0.0011f, 12)), negro, 0f);

        // La sugerencia de qué decir (debajo de la mira, chiquita, con borde para leerse en cualquier fondo).
        var gt = new GameObject("Sugerencia", typeof(RectTransform));
        gt.transform.SetParent(mira, false);
        gt.transform.localPosition = new Vector3(0f, -0.017f, 0f);
        var tx = gt.AddComponent<TextMeshPro>();
        tx.enableAutoSizing = true;
        tx.fontSizeMin = 0.005f;
        tx.fontSizeMax = 0.06f;
        tx.alignment = TextAlignmentOptions.Center;
        tx.fontStyle = FontStyles.Bold;
        tx.color = new Color(0.1f, 0.42f, 0.4f);
        tx.outlineWidth = 0.25f;
        tx.outlineColor = new Color32(255, 255, 255, 255);
        tx.rectTransform.sizeDelta = new Vector2(0.16f, 0.011f);
        tx.text = "";
        sugerencia = tx;

        // El brillo de lo señalado.
        matBrillo = Tenido(baseMaterial, ColorBrilloLinea);
        var gb = new GameObject("BrilloMirada");
        brillo = gb.AddComponent<LineRenderer>();
        brillo.useWorldSpace = true;
        brillo.sharedMaterial = matBrillo;
        brillo.widthMultiplier = 0.004f;
        brillo.numCapVertices = 3;
        brillo.numCornerVertices = 2;
        brillo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        brillo.receiveShadows = false;
        brillo.positionCount = 0;
    }

    Material Tenido(Material baseMaterial, Color c)
    {
        var m = new Material(baseMaterial);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        materiales.Add(m);
        return m;
    }

    static void Pintar(Material m, Color c)
    {
        if (m == null)
            return;
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }

    Transform Parte(Transform padre, string nombre, Mesh malla, Material m, float atras)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(0f, 0f, atras);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    static Vector2[] Circulo(float r, int n)
    {
        var p = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            p[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
        return p;
    }

    // La forma del ojo (convexa): dos arcos que se juntan en las puntas.
    static Vector2[] Almendra(float medioAncho, float alto)
    {
        const int N = 14;
        var p = new Vector2[N * 2];
        for (int i = 0; i < N; i++)
        {
            float x = Mathf.Lerp(-medioAncho, medioAncho, i / (float)(N - 1));
            float y = alto * (1f - (x / medioAncho) * (x / medioAncho)) * 0.5f;
            p[i] = new Vector2(x, y);
            p[N * 2 - 1 - i] = new Vector2(x, -y);
        }
        return p;
    }

    static Vector2[] Barra(Vector2 a, Vector2 b, float grosor)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x) * grosor * 0.5f;
        return new[] { a + n, b + n, b - n, a - n };
    }

    // Un polígono convexo relleno (en abanico desde su centro), visible de los dos lados.
    Mesh Abanico(Vector2[] p)
    {
        Vector2 c = Vector2.zero;
        foreach (var v in p)
            c += v;
        c /= p.Length;
        var verts = new Vector3[p.Length + 1];
        verts[0] = c;
        for (int i = 0; i < p.Length; i++)
            verts[i + 1] = p[i];
        var tris = new int[p.Length * 6];
        for (int i = 0; i < p.Length; i++)
        {
            int a = i + 1, b = (i + 1) % p.Length + 1;
            tris[i * 6] = 0;
            tris[i * 6 + 1] = a;
            tris[i * 6 + 2] = b;
            tris[i * 6 + 3] = 0;
            tris[i * 6 + 4] = b;
            tris[i * 6 + 5] = a;
        }
        var m = new Mesh();
        m.vertices = verts;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        mallas.Add(m);
        return m;
    }

    // ---------- Cada cuadro ----------

    void LateUpdate()
    {
        DedoCerca = false;
        if (control == null)
            control = ControlManos.Instancia;
        var cab = control != null ? control.Cabeza : null;
        if (cab == null || ojo == null)
            return;
        Vector3 adelante = cab.forward;
        if (Mathf.Abs(Vector3.Dot(adelante, Vector3.up)) > 0.97f)
            adelante = Vector3.ProjectOnPlane(cab.up, Vector3.up);
        Quaternion giro = Quaternion.LookRotation(adelante, Vector3.up);
        if (!colocado)
        {
            giroSuave = giro;
            colocado = true;
        }
        giroSuave = Quaternion.Slerp(giroSuave, giro, 1f - Mathf.Exp(-4f * Time.deltaTime));

        bool quiere = !VistaLimpia.Activa && !ExportadorVideo.Exportando;
        if (ojo.gameObject.activeSelf != quiere)
            ojo.gameObject.SetActive(quiere);
        if (quiere)
            Boton(cab);

        bool ver = Activa && !ExportadorVideo.Exportando && !control.PaletaAbierta;
        if (mira.gameObject.activeSelf != ver)
            mira.gameObject.SetActive(ver);
        if (!ver)
        {
            Objetivo = null;
            NodoTrazo = null;
            Nodo = -1;
            brillo.positionCount = 0;
            return;
        }
        Buscar(cab);
        float crece = Objetivo != null ? 1.35f : 1f;
        mira.SetPositionAndRotation(cab.position + cab.forward * Distancia, cab.rotation);
        mira.localScale = Vector3.one * crece;
        Pintar(matMira, Objetivo != null ? (EsRelleno ? ColorBrilloRelleno : ColorBrilloLinea) : ColorPrendida);
        PonerBrillo();
        PonerSugerencia();
    }

    // El ojo: debajo del micrófono. Se toca con cualquier índice.
    void Boton(Transform cab)
    {
        Vector3 lugar = BotonVoz.Lugar + new Vector3(0f, -0.088f, 0f);
        Vector3 pos = cab.position + giroSuave * lugar;
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cab.position, Vector3.up));
        bool prendida = Activa;
        Pintar(matIcono, prendida ? ColorPrendida : ColorApagada);
        if (raya.gameObject.activeSelf == prendida)
            raya.gameObject.SetActive(!prendida);

        var der = control.Der;
        bool dedo = der.valida && control.GestoIzq != ControlManos.Gesto.Transformar;
        float alOjo = Vector3.Distance(der.indice, ojo.position);
        DedoCerca = dedo && alOjo < 0.045f;
        if (dedo && alOjo < RadioToque * 0.82f + 0.02f)
            EtiquetasBotones.ProponerComando(ojo, alOjo, ControlManos.ComandoVoz.Mirada,
                                             ojo.position - transform.up * 0.032f - transform.forward * 0.006f, transform.rotation);
        bool toca = dedo && alOjo < RadioToque * 0.82f;
        if (control.Izq.valida && Vector3.Distance(control.Izq.indice, ojo.position) < RadioToque * 0.82f)
            toca = true;
        if (toca && !dedoEnOjo && Time.time >= bloqueadoHasta)
        {
            bloqueadoHasta = Time.time + 0.6f;
            Alternar();
            control.SonidoBurbuja(ojo.position);
        }
        dedoEnOjo = toca;
    }

    // Lo que señala la mira: una línea cerca (con imán), o el relleno en el que está dentro. También el nodo más cerca.
    void Buscar(Transform cab)
    {
        var d = control.dibujo;
        Objetivo = null;
        EsRelleno = false;
        NodoTrazo = null;
        Nodo = -1;
        if (d == null)
            return;
        Vector3 o = cab.position, f = cab.forward;
        Vector3 der = cab.right, arr = cab.up;
        Trazo mejorLinea = null, mejorRelleno = null;
        float angLinea = ImanLinea, profRelleno = float.MaxValue, angNodo = ImanNodo;
        foreach (var t in d.trazos)
        {
            if (!Dibujo.Editable(t) || t.curva.Count < 2)
                continue;
            var tr = t.transform;
            int paso = Mathf.Max(1, t.curva.Count / 64);
            bool rellenable = t.relleno && (t.cerrado || t.rellenoAbierto);
            poligono.Clear();
            float profSuma = 0f;
            int profN = 0;
            for (int i = 0; i < t.curva.Count; i += paso)
            {
                Vector3 v = tr.TransformPoint(t.curva[i]) - o;
                float a = Vector3.Dot(v, f);
                if (a < 0.05f)
                    continue;
                float ang = (v - f * a).magnitude / a;
                if (ang < angLinea)
                {
                    angLinea = ang;
                    mejorLinea = t;
                }
                if (rellenable)
                {
                    poligono.Add(new Vector2(Vector3.Dot(v, der) / a, Vector3.Dot(v, arr) / a));
                    profSuma += a;
                    profN++;
                }
            }
            if (rellenable && poligono.Count >= 3 && profN > 0 && Dentro(poligono))
            {
                float prof = profSuma / profN;
                if (prof < profRelleno)
                {
                    profRelleno = prof;
                    mejorRelleno = t;
                }
            }
            for (int i = 0; i < t.nodos.Count; i++)
            {
                Vector3 v = tr.TransformPoint(t.nodos[i]) - o;
                float a = Vector3.Dot(v, f);
                if (a < 0.05f)
                    continue;
                float ang = (v - f * a).magnitude / a;
                if (ang < angNodo)
                {
                    angNodo = ang;
                    NodoTrazo = t;
                    Nodo = i;
                }
            }
        }
        if (mejorLinea != null && (angLinea < ImanFino || mejorRelleno == null))
        {
            Objetivo = mejorLinea;
            EsRelleno = false;
        }
        else if (mejorRelleno != null)
        {
            Objetivo = mejorRelleno;
            EsRelleno = true;
        }
    }

    // v44: junto a la mira, qué se puede decir ahora (así se aprenden las palabras usándolas).
    void PonerSugerencia()
    {
        string t = "";
        bool en = Idioma.Ingles;
        if (control.GestoIzq == ControlManos.Gesto.Nodos && Nodo >= 0)
            t = en ? "say: corner · smooth" : "di: esquina · suave";
        else if (Objetivo != null && control.HayJeringa)
            t = en ? "say: paste color · copy color" : "di: pegar color · copiar color";
        else if (Objetivo != null && EsRelleno)
            t = en ? "say: red · copy color · delete this" : "di: rojo · copiar color · borra esto";
        else if (Objetivo != null)
            t = en ? "say: blue line · copy · select" : "di: línea azul · copiar (la línea) · selecciona";
        if (!Voz.Activa && t.Length > 0)
            t = en ? "turn on the microphone to talk" : "prende el micrófono para hablar";
        if (sugerencia.text != t)
            sugerencia.text = t;
    }

    // ¿El centro de la mira (0,0) está dentro de ese polígono?
    static bool Dentro(List<Vector2> p)
    {
        bool dentro = false;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
        {
            if ((p[i].y > 0f) != (p[j].y > 0f))
            {
                float x = p[j].x + (0f - p[j].y) * (p[i].x - p[j].x) / (p[i].y - p[j].y);
                if (x > 0f)
                    dentro = !dentro;
            }
        }
        return dentro;
    }

    // Lo señalado brilla: su contorno, en celeste (línea) o dorado (relleno), latiendo suave.
    void PonerBrillo()
    {
        if (Objetivo == null)
        {
            brillo.positionCount = 0;
            return;
        }
        var t = Objetivo;
        var tr = t.transform;
        int paso = Mathf.Max(1, t.curva.Count / 128);
        puntos.Clear();
        for (int i = 0; i < t.curva.Count; i += paso)
            puntos.Add(tr.TransformPoint(t.curva[i]));
        if (t.cerrado && puntos.Count > 2)
            puntos.Add(puntos[0]);
        brillo.positionCount = puntos.Count;
        brillo.SetPositions(puntos.ToArray());
        float late = 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
        brillo.widthMultiplier = Mathf.Max(0.003f, t.ancho * 1.8f) * late;
        Pintar(matBrillo, EsRelleno ? ColorBrilloRelleno : ColorBrilloLinea);
    }

    void OnDestroy()
    {
        foreach (var m in materiales)
            if (m != null)
                Destroy(m);
        foreach (var m in mallas)
            if (m != null)
                Destroy(m);
        if (mira != null)
            Destroy(mira.gameObject);
        if (brillo != null)
            Destroy(brillo.gameObject);
        if (Instancia == this)
            Instancia = null;
        Objetivo = null;
        NodoTrazo = null;
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Tutorial de RELLENOS (sección 2 del panel "?"). No es parte del tutorial de presentación.
// Una tarjeta flotando a tu izquierda (así puedes practicar a la derecha mientras la lees), con 4 temas,
// cada uno con su explicación y una animación que se repite:
//  1 · La cubeta: rellena mientras dibujas (en vivo), aunque no cierres la forma.
//  2 · Tocar dentro para rellenar (contorno negro + relleno de otro color).
//  3 · Tinta invisible (cierra y rellena sin que se vea la línea).
//  4 · Ideas (nariz de lado, manchas, caminos del títere, mejillas).
// Botones: < Atrás · Siguiente > · X. Tu dibujo no se aparta: practicas en él mismo.
public class TutorialRellenos : MonoBehaviour
{
    const int Paginas = 4;
    const float Ciclo = 4.5f; // segundos que dura cada animación (y se repite)
    static readonly Color Amarillo = new Color(1f, 0.85f, 0.35f);
    static readonly Color Piel = new Color(0.96f, 0.75f, 0.6f);
    static readonly Color Celeste = new Color(0.55f, 0.78f, 1f);
    static readonly Color Gris = new Color(0.72f, 0.74f, 0.8f);

    Tutorial tutorial;
    Transform raiz, demo;
    TextMeshPro titulo, explicacion, numeroPagina;
    BotonTocable btnAtras, btnSiguiente;
    LineRenderer linea, lineaGris;
    MeshFilter relleno;
    Material matRelleno, matGris, matFondoDemo;
    Mesh mallaRelleno;
    Transform dedo;
    int pagina;
    float desde;
    readonly List<Vector3> puntos = new List<Vector3>();
    readonly List<Vector3> forma = new List<Vector3>();

    static string Tx(string en, string es) { return Idioma.Ingles ? en : es; }

    public static void Abrir(Tutorial t)
    {
        if (t == null)
            return;
        var r = t.GetComponent<TutorialRellenos>();
        if (r == null)
            r = t.gameObject.AddComponent<TutorialRellenos>();
        r.tutorial = t;
        r.Armar();
    }

    public static void CerrarSiAbierto(Tutorial t)
    {
        var r = t != null ? t.GetComponent<TutorialRellenos>() : null;
        if (r != null)
            r.Cerrar();
    }

    void Armar()
    {
        Cerrar();
        var control = ControlManos.Instancia;
        if (control == null || control.Cabeza == null)
            return;
        Transform cab = control.Cabeza;
        Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized;
        // A la izquierda y mirando hacia ti: la derecha queda libre para practicar.
        Vector3 pos = cab.position + adelante * 0.5f - derecha * 0.16f - Vector3.up * 0.04f;
        raiz = new GameObject("TutorialRellenos").transform;
        raiz.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cab.position, Vector3.up));

        PanelTutoriales.Fondo(raiz, new Vector2(0.36f, 0.31f), tutorial.materialNegro, tutorial.materialBlanco);
        titulo = PanelTutoriales.Texto(raiz, "", new Vector3(0f, 0.128f, -0.004f), new Vector2(0.3f, 0.03f), 0.3f, tutorial.fuenteComic);
        numeroPagina = PanelTutoriales.Texto(raiz, "", new Vector3(0.155f, 0.128f, -0.004f), new Vector2(0.04f, 0.02f), 0.14f, null);
        numeroPagina.color = new Color(0.35f, 0.35f, 0.4f);
        explicacion = PanelTutoriales.Texto(raiz, "", new Vector3(0f, 0.06f, -0.004f), new Vector2(0.33f, 0.09f), 0.16f, null);
        explicacion.alignment = TextAlignmentOptions.Left;

        // La zona de la animación: un recuadro gris muy claro.
        demo = new GameObject("Demo").transform;
        demo.SetParent(raiz, false);
        demo.localPosition = new Vector3(0f, -0.045f, -0.004f);
        matFondoDemo = new Material(tutorial.materialBlanco);
        PonerColor(matFondoDemo, new Color(0.93f, 0.94f, 0.96f));
        var marco = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(marco.GetComponent<Collider>());
        marco.transform.SetParent(raiz, false);
        marco.transform.localPosition = new Vector3(0f, -0.045f, 0.002f);
        marco.transform.localScale = new Vector3(0.3f, 0.11f, 0.002f);
        Pintar(marco.GetComponent<Renderer>(), matFondoDemo);

        // Relleno (detrás), línea negra, línea gris (tinta invisible mientras editas) y el dedo.
        matRelleno = new Material(tutorial.materialBlanco);
        mallaRelleno = new Mesh { name = "RellenoDemo" };
        mallaRelleno.MarkDynamic();
        var go = new GameObject("Relleno");
        go.transform.SetParent(demo, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.0005f);
        relleno = go.AddComponent<MeshFilter>();
        relleno.sharedMesh = mallaRelleno;
        Pintar(go.AddComponent<MeshRenderer>(), matRelleno);
        linea = Linea("Linea", tutorial.materialNegro, 0.0035f);
        matGris = new Material(tutorial.materialBlanco);
        PonerColor(matGris, Gris);
        lineaGris = Linea("LineaGris", matGris, 0.003f);
        var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(bola.GetComponent<Collider>());
        bola.name = "Dedo";
        bola.transform.SetParent(demo, false);
        bola.transform.localScale = Vector3.one * 0.009f;
        Pintar(bola.GetComponent<Renderer>(), tutorial.materialAzul);
        dedo = bola.transform;

        btnAtras = PanelTutoriales.Boton(raiz, Tx("<  Back", "<  Atrás"), new Vector3(-0.1f, -0.128f, 0f), new Vector2(0.09f, 0.026f),
                                         tutorial.materialBoton, tutorial.materialBotonMarcado);
        btnAtras.alTocar.AddListener(() => IrA(pagina - 1));
        btnSiguiente = PanelTutoriales.Boton(raiz, "", new Vector3(0.1f, -0.128f, 0f), new Vector2(0.09f, 0.026f),
                                             tutorial.materialBoton, tutorial.materialBotonMarcado);
        btnSiguiente.alTocar.AddListener(() =>
        {
            if (pagina >= Paginas - 1)
                Cerrar();
            else
                IrA(pagina + 1);
        });
        var x = PanelTutoriales.Boton(raiz, "X", new Vector3(0.165f, 0.142f, 0f), new Vector2(0.022f, 0.022f), tutorial.materialBoton, tutorial.materialBotonMarcado);
        x.alTocar.AddListener(Cerrar);
        IrA(0);
    }

    LineRenderer Linea(string nombre, Material m, float ancho)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(demo, false);
        var l = go.AddComponent<LineRenderer>();
        l.useWorldSpace = true;
        l.widthMultiplier = ancho;
        l.numCapVertices = 4;
        l.numCornerVertices = 2;
        l.sharedMaterial = m;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        l.receiveShadows = false;
        l.positionCount = 0;
        return l;
    }

    void IrA(int p)
    {
        pagina = Mathf.Clamp(p, 0, Paginas - 1);
        desde = Time.time;
        numeroPagina.text = (pagina + 1) + "/" + Paginas;
        switch (pagina)
        {
            case 0:
                titulo.text = Tx("1 · The bucket: fill while you draw", "1 · La cubeta: rellena mientras dibujas");
                explicacion.text = Tx(
                    "Open the palette (left palm) and tap the BUCKET (top right): it turns yellow. Now draw a C: it fills LIVE while you draw, and stays filled when you let go, even if it isn't closed. Tap the bucket again = off.",
                    "Abre la paleta (palma izquierda) y toca la CUBETA (arriba a la derecha): se pone amarilla. Ahora dibuja una C: se va rellenando EN VIVO y al soltar queda rellena, aunque no la cierres. Otra vez la cubeta = apagada.");
                break;
            case 1:
                titulo.text = Tx("2 · Tap inside to fill", "2 · Toca dentro para rellenar");
                explicacion.text = Tx(
                    "With the palette open, pick a color and tap INSIDE a shape (open or closed): it fills with that color. Keep a black outline and a fill of another color, like a nose in profile. You can undo it.",
                    "Con la paleta abierta, elige un color y toca DENTRO de una forma (abierta o cerrada): se rellena de ese color. Así el contorno queda negro y el relleno de otro color, como una nariz de lado. Se puede deshacer.");
                break;
            case 2:
                titulo.text = Tx("3 · Invisible ink", "3 · Tinta invisible");
                explicacion.text = Tx(
                    "Tap the CHECKERED circle (top left of the palette): your line won't show, but it closes and fills. While editing (palette, nodes or eraser) it shows light gray so you can find it. Tap inside with invisible ink = remove the fill.",
                    "Toca el círculo a CUADRITOS (arriba a la izquierda de la paleta): tu línea no se verá, pero sí cierra y rellena. Mientras editas (paleta, nodos o borrador) se ve gris clarito para encontrarla. Tocar dentro con tinta invisible = quitar el relleno.");
                break;
            default:
                titulo.text = Tx("4 · Ideas", "4 · Ideas");
                explicacion.text = Tx(
                    "• Nose in profile: black outline + tap inside with skin color.\n• Spots and shadows with no outline: bucket + invisible ink.\n• Puppet path: draw it with invisible ink (it won't show in videos).\n• Cheeks: an oval with the bucket and pink.",
                    "• Nariz de lado: contorno negro + toca dentro con color piel.\n• Manchas y sombras sin borde: cubeta + tinta invisible.\n• Camino del títere: dibújalo con tinta invisible (no sale en el video).\n• Mejillas: un óvalo con la cubeta y color rosado.");
                break;
        }
        if (btnSiguiente.etiqueta != null)
            btnSiguiente.etiqueta.text = pagina >= Paginas - 1 ? Tx("Done!", "¡Listo!") : Tx("Next  >", "Siguiente  >");
        btnAtras.gameObject.SetActive(pagina > 0);
        if (btnAtras.etiqueta != null)
            btnAtras.etiqueta.gameObject.SetActive(pagina > 0);
    }

    void Update()
    {
        if (raiz == null)
            return;
        // Si empieza el tutorial de presentación, esta tarjeta se va.
        if (Tutorial.EnCurso)
        {
            Cerrar();
            return;
        }
        float t = Mathf.Repeat(Time.time - desde, Ciclo);
        switch (pagina)
        {
            case 0: AnimarCubeta(t); break;
            case 1: AnimarTocarDentro(t); break;
            case 2: AnimarInvisible(t); break;
            default: AnimarIdeas(t); break;
        }
    }

    // ---------- Las animaciones (en el recuadro, coordenadas locales en metros) ----------

    // Una "C" abierta hacia la derecha (de 50° a 310°).
    static void FormaC(List<Vector3> salida, float hasta)
    {
        salida.Clear();
        const int n = 40;
        int cuantos = Mathf.Clamp(Mathf.RoundToInt(n * hasta), 0, n);
        for (int i = 0; i <= cuantos; i++)
        {
            float a = Mathf.Lerp(50f, 310f, i / (float)n) * Mathf.Deg2Rad;
            salida.Add(new Vector3(-0.02f + Mathf.Cos(a) * 0.04f, Mathf.Sin(a) * 0.04f, 0f));
        }
    }

    // Nariz de lado (abierta: sin línea del lado de la cara).
    static void FormaNariz(List<Vector3> salida)
    {
        salida.Clear();
        Vector3[] c = { new Vector3(-0.03f, 0.045f, 0f), new Vector3(0.0f, 0.01f, 0f), new Vector3(0.035f, -0.02f, 0f),
                        new Vector3(0.025f, -0.035f, 0f), new Vector3(0.0f, -0.035f, 0f), new Vector3(-0.012f, -0.028f, 0f),
                        new Vector3(-0.03f, -0.035f, 0f) };
        for (int i = 0; i < c.Length - 1; i++)
            for (int k = 0; k < 6; k++)
            {
                // Catmull-Rom (curva suave que pasa por los puntos).
                Vector3 p0 = c[Mathf.Max(0, i - 1)], p1 = c[i], p2 = c[i + 1], p3 = c[Mathf.Min(c.Length - 1, i + 2)];
                float u = k / 6f;
                salida.Add(0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u));
            }
        salida.Add(c[c.Length - 1]);
    }

    void AnimarCubeta(float t)
    {
        float u = Mathf.Clamp01(t / 2.4f);
        FormaC(puntos, u);
        PonerLinea(linea, puntos);
        Rellenar(puntos, Amarillo, 1f);
        lineaGris.positionCount = 0;
        bool dibujando = t < 2.4f;
        dedo.gameObject.SetActive(dibujando || t < 2.8f);
        if (puntos.Count > 0)
            dedo.localPosition = puntos[puntos.Count - 1] + new Vector3(0f, 0f, -0.004f);
    }

    void AnimarTocarDentro(float t)
    {
        FormaNariz(puntos);
        PonerLinea(linea, puntos);
        lineaGris.positionCount = 0;
        // El dedo entra desde fuera y toca adentro; ¡se rellena!
        Vector3 fuera = new Vector3(0.11f, 0.03f, -0.004f), dentro = new Vector3(0.008f, -0.012f, -0.004f);
        float ir = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1.4f, t));
        dedo.gameObject.SetActive(t < 3.2f);
        dedo.localPosition = Vector3.Lerp(fuera, dentro, ir);
        float lleno = t < 1.5f ? 0f : Mathf.Clamp01((t - 1.5f) / 0.25f);
        Rellenar(puntos, Piel, lleno);
    }

    void AnimarInvisible(float t)
    {
        FormaC(puntos, 1f);
        PonerLinea(linea, puntos);
        // La tinta invisible cierra la C (gris clarito mientras "editas")...
        float cierre = Mathf.Clamp01((t - 0.6f) / 1.0f);
        forma.Clear();
        if (cierre > 0f && puntos.Count > 1)
        {
            Vector3 a = puntos[puntos.Count - 1], b = puntos[0];
            forma.Add(a);
            forma.Add(Vector3.Lerp(a, b, cierre));
        }
        // ...y al terminar de editar desaparece: queda el relleno sin línea de cierre.
        if (t < 3.0f)
            PonerLinea(lineaGris, forma);
        else
            lineaGris.positionCount = 0;
        dedo.gameObject.SetActive(t > 0.5f && t < 1.7f);
        if (forma.Count > 1)
            dedo.localPosition = forma[1] + new Vector3(0f, 0f, -0.004f);
        Rellenar(puntos, Celeste, Mathf.Clamp01((t - 1.8f) / 0.3f));
    }

    void AnimarIdeas(float t)
    {
        FormaNariz(puntos);
        PonerLinea(linea, puntos);
        lineaGris.positionCount = 0;
        dedo.gameObject.SetActive(false);
        Rellenar(puntos, Piel, 1f);
    }

    void PonerLinea(LineRenderer l, List<Vector3> locales)
    {
        if (locales.Count < 2)
        {
            l.positionCount = 0;
            return;
        }
        l.positionCount = locales.Count;
        for (int i = 0; i < locales.Count; i++)
            l.SetPosition(i, demo.TransformPoint(locales[i]));
    }

    // El relleno: abanico desde el centro de la forma (como si una línea invisible uniera las puntas).
    readonly List<Vector3> vRelleno = new List<Vector3>();
    readonly List<int> tRelleno = new List<int>();

    void Rellenar(List<Vector3> pts, Color c, float aparece)
    {
        mallaRelleno.Clear();
        if (pts.Count < 3 || aparece <= 0.01f)
            return;
        PonerColor(matRelleno, Color.Lerp(new Color(0.93f, 0.94f, 0.96f), c, aparece));
        Vector3 centro = Vector3.zero;
        foreach (var p in pts)
            centro += p;
        centro /= pts.Count;
        vRelleno.Clear();
        tRelleno.Clear();
        vRelleno.Add(centro);
        vRelleno.AddRange(pts);
        int n = pts.Count;
        for (int i = 0; i < n; i++)
        {
            int a = 1 + i, b = 1 + (i + 1) % n;
            tRelleno.Add(0); tRelleno.Add(a); tRelleno.Add(b);
            tRelleno.Add(0); tRelleno.Add(b); tRelleno.Add(a); // por los dos lados
        }
        mallaRelleno.SetVertices(vRelleno);
        mallaRelleno.SetTriangles(tRelleno, 0);
        mallaRelleno.RecalculateBounds();
    }

    static void PonerColor(Material m, Color c)
    {
        if (m == null)
            return;
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }

    static void Pintar(Renderer r, Material m)
    {
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    public void Cerrar()
    {
        if (raiz != null)
            Destroy(raiz.gameObject);
        raiz = null;
        foreach (var m in new[] { matRelleno, matGris, matFondoDemo })
            if (m != null)
                Destroy(m);
        matRelleno = matGris = matFondoDemo = null;
        if (mallaRelleno != null)
            Destroy(mallaRelleno);
        mallaRelleno = null;
    }

    void OnDestroy()
    {
        Cerrar();
    }
}

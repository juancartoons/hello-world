using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Base de las tarjetas de tutorial del panel "?" (Rellenos, Nodos...). Una tarjeta flotando a tu
// izquierda (así practicas a la derecha mientras la lees), con varios temas: cada uno con su título,
// su explicación y una animación que se repite en un recuadro. Botones: < Atrás · Siguiente > · X.
// Tu dibujo no se aparta: practicas en él mismo. Si empieza el tutorial de presentación, la tarjeta se va.
public abstract class TarjetaTutorial : MonoBehaviour
{
    protected const float Ciclo = 4.5f; // segundos que dura cada animación (y se repite)
    protected static readonly Color FondoDemo = new Color(0.93f, 0.94f, 0.96f);

    protected Tutorial tutorial;
    protected Transform raiz, demo, dedo;
    protected LineRenderer linea, lineaGris;
    protected Material matRelleno, matGris;
    protected int pagina;
    protected readonly List<Vector3> puntos = new List<Vector3>();
    protected readonly List<Vector3> forma = new List<Vector3>();
    readonly List<Material> materiales = new List<Material>();
    TextMeshPro titulo, explicacion, numeroPagina;
    BotonTocable btnAtras, btnSiguiente;
    Mesh mallaRelleno;
    float desde;

    protected abstract int Paginas { get; }
    protected abstract string Titulo(int p);
    protected abstract string Explicacion(int p);
    protected abstract void Animar(int p, float t);
    // Para piezas propias de cada tarjeta (se llama al armar).
    protected virtual void ArmarExtra() { }

    protected static string Tx(string en, string es) { return Idioma.Ingles ? en : es; }

    public static void Abrir<T>(Tutorial t) where T : TarjetaTutorial
    {
        if (t == null)
            return;
        CerrarTodas(t);
        var r = t.GetComponent<T>();
        if (r == null)
            r = t.gameObject.AddComponent<T>();
        r.tutorial = t;
        r.Armar();
    }

    public static void CerrarTodas(Tutorial t)
    {
        if (t == null)
            return;
        foreach (var r in t.GetComponents<TarjetaTutorial>())
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
        raiz = new GameObject(GetType().Name).transform;
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
        var marco = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(marco.GetComponent<Collider>());
        marco.transform.SetParent(raiz, false);
        marco.transform.localPosition = new Vector3(0f, -0.045f, 0.002f);
        marco.transform.localScale = new Vector3(0.3f, 0.11f, 0.002f);
        Pintar(marco.GetComponent<Renderer>(), Copia(tutorial.materialBlanco, FondoDemo));

        // Relleno (detrás), línea negra, línea gris y el dedo (una bolita azul).
        matRelleno = Copia(tutorial.materialBlanco, Color.white);
        mallaRelleno = new Mesh { name = "RellenoDemo" };
        mallaRelleno.MarkDynamic();
        var go = new GameObject("Relleno");
        go.transform.SetParent(demo, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.0005f);
        go.AddComponent<MeshFilter>().sharedMesh = mallaRelleno;
        Pintar(go.AddComponent<MeshRenderer>(), matRelleno);
        linea = Linea("Linea", tutorial.materialNegro, 0.0035f);
        matGris = Copia(tutorial.materialBlanco, new Color(0.72f, 0.74f, 0.8f));
        lineaGris = Linea("LineaGris", matGris, 0.003f);
        dedo = Bolita("Dedo", tutorial.materialAzul, 0.009f);
        ArmarExtra();

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

    void IrA(int p)
    {
        pagina = Mathf.Clamp(p, 0, Paginas - 1);
        desde = Time.time;
        numeroPagina.text = (pagina + 1) + "/" + Paginas;
        titulo.text = Titulo(pagina);
        explicacion.text = Explicacion(pagina);
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
        if (Tutorial.EnCurso)
        {
            Cerrar();
            return;
        }
        Animar(pagina, Mathf.Repeat(Time.time - desde, Ciclo));
    }

    // ---------- Ayudas para las animaciones (coordenadas locales del recuadro, en metros) ----------

    protected Material Copia(Material m, Color c)
    {
        var copia = new Material(m);
        PonerColor(copia, c);
        materiales.Add(copia);
        return copia;
    }

    protected LineRenderer Linea(string nombre, Material m, float ancho)
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

    protected Transform Bolita(string nombre, Material m, float diametro)
    {
        var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(bola.GetComponent<Collider>());
        bola.name = nombre;
        bola.transform.SetParent(demo, false);
        bola.transform.localScale = Vector3.one * diametro;
        Pintar(bola.GetComponent<Renderer>(), m);
        return bola.transform;
    }

    protected void PonerLinea(LineRenderer l, List<Vector3> locales)
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

    readonly List<Vector3> vRelleno = new List<Vector3>();
    readonly List<int> tRelleno = new List<int>();

    // Relleno: abanico desde el centro de la forma (como si una línea invisible uniera las puntas).
    protected void Rellenar(List<Vector3> pts, Color c, float aparece)
    {
        mallaRelleno.Clear();
        if (pts.Count < 3 || aparece <= 0.01f)
            return;
        PonerColor(matRelleno, Color.Lerp(FondoDemo, c, aparece));
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

    protected void SinRelleno()
    {
        if (mallaRelleno != null)
            mallaRelleno.Clear();
    }

    protected static void PonerColor(Material m, Color c)
    {
        if (m == null)
            return;
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }

    protected static void Pintar(Renderer r, Material m)
    {
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // Suave de 0 a 1 (para mover cosas sin brusquedad).
    protected static float Suave(float u)
    {
        u = Mathf.Clamp01(u);
        return u * u * (3f - 2f * u);
    }

    public void Cerrar()
    {
        if (raiz != null)
            Destroy(raiz.gameObject);
        raiz = null;
        foreach (var m in materiales)
            if (m != null)
                Destroy(m);
        materiales.Clear();
        if (mallaRelleno != null)
            Destroy(mallaRelleno);
        mallaRelleno = null;
    }

    void OnDestroy()
    {
        Cerrar();
    }
}

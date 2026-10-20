using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using TMPro;
using UnityEngine;

// IMPORTAR SVG (v57): un dibujo vectorial (Inkscape, Illustrator, Figma...) se vuelve LÍNEAS TUYAS, con los MISMOS
// NODOS que el original: cada curva Bézier del SVG es un tramo de la app (nodo + tiradores). Se editan como
// cualquier línea (nodos, tiradores, lazo, plastilina, relleno, animación...).
//  - Las curvas cuadráticas se vuelven cúbicas exactas (mismo número de nodos). Los ARCOS se aproximan con
//    curvas (ahí sí salen algunos nodos de más).
//  - Trae el color del RELLENO y de la LÍNEA (sin línea = tinta invisible). El GROSOR del SVG es opcional
//    (botón "Grosor del SVG"); si no, usa el grosor de tu pincel.
//  - Entiende grupos, transformaciones (matrix, translate, scale, rotate, skew), estilos y clases (<style>), y las
//    formas básicas (rect, circle, ellipse, line, polyline, polygon). No trae textos, imágenes, degradados ni huecos.
//  - Aparece frente a ti (medio metro de ancho), en una hoja 2D de la capa actual. Se puede deshacer.
//  - De dónde: Descargas, Descargas → JCartoons o Descargas → JCartoons → SVG (pon ahí tus .svg con el cable).
public class ImportadorSvg : MonoBehaviour
{
    static ImportadorSvg instancia;
    const int MaxArchivos = 8;
    const int MaxLineas = 600;
    const int MaxNodos = 30000;
    const string ClaveGrosor = "jcartoons_svg_grosor";

    ControlManos control;
    Dibujo dibujo;
    readonly List<BotonTocable> botones = new List<BotonTocable>();
    readonly List<string> archivos = new List<string>();
    BotonTocable btnGrosor;
    TextMeshPro textoEstado;

    static bool UsarGrosor
    {
        get => PlayerPrefs.GetInt(ClaveGrosor, 1) != 0;
        set => PlayerPrefs.SetInt(ClaveGrosor, value ? 1 : 0);
    }

    public static void Abrir()
    {
        if (instancia == null)
        {
            instancia = new GameObject("ImportadorSvg").AddComponent<ImportadorSvg>();
            if (!instancia.Armar())
            {
                Destroy(instancia.gameObject);
                instancia = null;
                return;
            }
        }
        instancia.gameObject.SetActive(true);
        instancia.Empezar();
    }

    public static void Cerrar()
    {
        if (instancia != null)
            instancia.gameObject.SetActive(false);
    }

    // ---------- El panel ----------

    bool Armar()
    {
        control = ControlManos.Instancia;
        dibujo = control != null ? control.dibujo : FindFirstObjectByType<Dibujo>();
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (dibujo == null || nav == null)
            return false;
        Material panel = nav.materialPanel, normal = nav.materialBoton, marcado = nav.materialBotonMarcado;
        var fondo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(transform, false);
        fondo.transform.localPosition = new Vector3(0f, -0.04f, 0.006f);
        fondo.transform.localScale = new Vector3(0.34f, 0.34f, 0.004f);
        var r = fondo.GetComponent<Renderer>();
        r.sharedMaterial = panel;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        var t = PanelTutoriales.Texto(transform, "", new Vector3(0f, 0.11f, -0.002f), new Vector2(0.3f, 0.02f), 0.2f, null);
        Idioma.Poner(t, "Importar SVG");
        t.fontStyle = FontStyles.Bold;
        for (int i = 0; i < MaxArchivos; i++)
        {
            int k = i;
            var b = PanelTutoriales.Boton(transform, "", new Vector3(0f, 0.08f - i * 0.028f, 0f), new Vector2(0.3f, 0.024f), normal, marcado);
            b.alTocar.AddListener(() => Elegir(k));
            botones.Add(b);
        }
        textoEstado = PanelTutoriales.Texto(transform, "", new Vector3(0f, -0.15f, -0.002f), new Vector2(0.31f, 0.03f), 0.08f, null);
        btnGrosor = PanelTutoriales.Boton(transform, "", new Vector3(-0.06f, -0.185f, 0f), new Vector2(0.17f, 0.024f), normal, marcado);
        btnGrosor.alTocar.AddListener(() =>
        {
            UsarGrosor = !UsarGrosor;
            PlayerPrefs.Save();
            RefrescarGrosor();
        });
        var cerrar = PanelTutoriales.Boton(transform, "", new Vector3(0.1f, -0.185f, 0f), new Vector2(0.09f, 0.024f), normal, marcado);
        Idioma.Poner(cerrar.etiqueta, "Cerrar");
        cerrar.alTocar.AddListener(Cerrar);
        var mover = gameObject.AddComponent<ArrastrePanel>();
        mover.centro = new Vector2(0f, -0.04f);
        mover.medio = new Vector2(0.17f, 0.17f);
        return true;
    }

    void RefrescarGrosor()
    {
        btnGrosor.etiqueta.text = Idioma.T(UsarGrosor ? "Grosor del SVG: sí" : "Grosor del SVG: no (tu pincel)");
    }

    void Empezar()
    {
        var cab = control != null ? control.Cabeza : null;
        if (cab != null)
        {
            Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
            if (adelante.sqrMagnitude < 1e-4f)
                adelante = Vector3.forward;
            adelante.Normalize();
            Vector3 pos = cab.position + adelante * 0.45f + Vector3.left * 0f + Vector3.up * 0.05f;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cab.position, Vector3.up));
        }
        Buscar();
        RefrescarGrosor();
    }

    static IEnumerable<string> Carpetas()
    {
        string descargas = Galeria.Descargas();
        if (!string.IsNullOrEmpty(descargas))
        {
            yield return descargas;
            yield return Path.Combine(descargas, "JCartoons");
            yield return Path.Combine(descargas, "JCartoons", "SVG");
        }
        yield return Application.persistentDataPath;
    }

    void Buscar()
    {
        archivos.Clear();
        var vistos = new HashSet<string>();
        foreach (var c in Carpetas())
        {
            try
            {
                if (!Directory.Exists(c))
                    continue;
                foreach (var f in Directory.GetFiles(c, "*.svg"))
                    if (vistos.Add(f))
                        archivos.Add(f);
                foreach (var f in Directory.GetFiles(c, "*.SVG"))
                    if (vistos.Add(f))
                        archivos.Add(f);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TrazoVR: no pude leer " + c + ": " + e.Message);
            }
        }
        // Los más nuevos primero.
        archivos.Sort((a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
        for (int i = 0; i < botones.Count; i++)
        {
            bool hay = i < archivos.Count;
            botones[i].gameObject.SetActive(hay);
            botones[i].etiqueta.gameObject.SetActive(hay);
            if (hay)
                botones[i].etiqueta.text = Path.GetFileName(archivos[i]);
        }
        textoEstado.text = archivos.Count > 0
            ? Idioma.T("Toca un SVG: sale frente a ti como líneas tuyas, con los mismos nodos")
            : Idioma.T("No encontré archivos .svg. Ponlos con el cable en Descargas (o Descargas → JCartoons → SVG)");
    }

    void Elegir(int i)
    {
        if (i < 0 || i >= archivos.Count)
            return;
        string nombre = Path.GetFileName(archivos[i]);
        int lineas, nodos;
        string error = Importar(archivos[i], out lineas, out nodos);
        if (error != null)
        {
            textoEstado.text = Idioma.T(error);
            dibujo.Mensaje(error);
            return;
        }
        dibujo.Mensaje(Idioma.T("SVG importado") + ": " + nombre + " · " + lineas + " " + Idioma.T("líneas") + " · " + nodos + " " + Idioma.T("nodos"));
        Cerrar();
    }

    // ---------- Leer el SVG ----------

    // Una matriz 2D del SVG: x' = a x + c y + e ; y' = b x + d y + f.
    struct Matriz
    {
        public float a, b, c, d, e, f;
        public static Matriz Identidad => new Matriz { a = 1f, d = 1f };

        public Vector2 Aplicar(Vector2 p)
        {
            return new Vector2(a * p.x + c * p.y + e, b * p.x + d * p.y + f);
        }

        // Primero "otra", después esta.
        public Matriz Por(Matriz o)
        {
            return new Matriz
            {
                a = a * o.a + c * o.b,
                b = b * o.a + d * o.b,
                c = a * o.c + c * o.d,
                d = b * o.c + d * o.d,
                e = a * o.e + c * o.f + e,
                f = b * o.e + d * o.f + f
            };
        }

        public float EscalaMedia => Mathf.Sqrt(Mathf.Abs(a * d - b * c));
    }

    class Estilo
    {
        public string relleno = "#000000";
        public string linea = "none";
        public float grosor = 1f;
        public bool visible = true;

        public Estilo Copia()
        {
            return (Estilo)MemberwiseClone();
        }
    }

    // Un camino: nodos con sus tiradores (todo en coordenadas absolutas del SVG ya transformadas).
    class Camino
    {
        public readonly List<Vector2> nodos = new List<Vector2>();
        public readonly List<Vector2> entra = new List<Vector2>();
        public readonly List<Vector2> sale = new List<Vector2>();
        public bool cerrado;
        public Color colorLinea;
        public Color colorRelleno;
        public bool hayLinea, hayRelleno;
        public float grosor;
    }

    readonly List<Camino> caminos = new List<Camino>();
    readonly Dictionary<string, Dictionary<string, string>> clases = new Dictionary<string, Dictionary<string, string>>();
    int nodosLeidos;

    string Importar(string ruta, out int lineas, out int nodos)
    {
        lineas = 0;
        nodos = 0;
        if (dibujo.EsCamara(dibujo.capaActual))
            return "Estás en la capa de la cámara: elige otra capa para importar";
        caminos.Clear();
        clases.Clear();
        nodosLeidos = 0;
        try
        {
            var ajustes = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
            var doc = new XmlDocument { XmlResolver = null };
            using (var lector = XmlReader.Create(ruta, ajustes))
                doc.Load(lector);
            if (doc.DocumentElement == null)
                return "Ese SVG está vacío";
            LeerEstilos(doc.DocumentElement);
            Recorrer(doc.DocumentElement, Matriz.Identidad, new Estilo());
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: SVG: " + e.Message);
            return "No pude leer ese SVG";
        }
        if (caminos.Count == 0)
            return "Ese SVG no tiene trazos que yo entienda (textos e imágenes no se importan)";
        // El tamaño: medio metro de ancho (o de alto), frente a ti, mirándote.
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (var c in caminos)
            foreach (var p in c.nodos)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
        float tam = Mathf.Max(max.x - min.x, max.y - min.y);
        if (tam < 1e-6f)
            return "Ese SVG es demasiado pequeño";
        float escala = 0.5f / tam;
        Vector2 centro = (min + max) * 0.5f;
        var cab = control != null ? control.Cabeza : null;
        Vector3 origen, derecha, arriba, haciaMi;
        if (cab != null)
        {
            Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
            if (adelante.sqrMagnitude < 1e-4f)
                adelante = Vector3.forward;
            adelante.Normalize();
            origen = cab.position + adelante * 0.8f;
            derecha = Vector3.Cross(Vector3.up, adelante).normalized;
            arriba = Vector3.up;
            haciaMi = -adelante;
        }
        else
        {
            origen = dibujo.transform.position;
            derecha = Vector3.right;
            arriba = Vector3.up;
            haciaMi = Vector3.back;
        }
        var raiz = dibujo.transform;
        System.Func<Vector2, Vector3> local = q =>
            raiz.InverseTransformPoint(origen + derecha * ((q.x - centro.x) * escala) - arriba * ((q.y - centro.y) * escala));
        Vector3 hojaPunto = raiz.InverseTransformPoint(origen);
        Vector3 hojaNormal = raiz.InverseTransformDirection(haciaMi).normalized;
        bool grosor = UsarGrosor;
        dibujo.GuardarParaDeshacer();
        Trazo ultima = null;
        foreach (var c in caminos)
        {
            int n = c.nodos.Count;
            var d = new DatosTrazo();
            for (int i = 0; i < n; i++)
            {
                Vector3 p = local(c.nodos[i]);
                d.nodos.Add(p);
                d.asaEntrada.Add(local(c.entra[i]) - p);
                d.asaSalida.Add(local(c.sale[i]) - p);
                d.asaManual.Add(true);
                d.grosorNodo.Add(1f);
            }
            d.cerrado = c.cerrado && n >= 3;
            d.relleno = c.hayRelleno;
            d.rellenoAbierto = c.hayRelleno && !d.cerrado;
            d.colorFondo = c.hayRelleno ? new Color(c.colorRelleno.r, c.colorRelleno.g, c.colorRelleno.b, 1f) : new Color(0f, 0f, 0f, 0f);
            d.color = c.hayLinea ? new Color(c.colorLinea.r, c.colorLinea.g, c.colorLinea.b, 1f) : new Color(0f, 0f, 0f, 0f);
            d.ancho = grosor && c.hayLinea && c.grosor > 0f ? Mathf.Max(0.0005f, c.grosor * escala) / dibujo.EscalaMundo : 0f;
            d.enHoja = true;
            d.hojaPunto = hojaPunto;
            d.hojaNormal = hojaNormal;
            d.velocidadTextura = 2;
            d.escalaTextura = 1f;
            ultima = dibujo.AgregarTrazo(d);
            lineas++;
            nodos += n;
        }
        if (ultima != null)
            dibujo.Seleccionar(ultima);
        dibujo.HayCambios = true;
        return null;
    }

    // Las clases de los <style> (Illustrator las usa: .st0{fill:#FF0000;}).
    void LeerEstilos(XmlNode nodo)
    {
        foreach (XmlNode h in nodo.ChildNodes)
        {
            if (h.NodeType != XmlNodeType.Element)
                continue;
            if (h.LocalName == "style")
            {
                string css = h.InnerText ?? "";
                int i = 0;
                while (i < css.Length)
                {
                    int abre = css.IndexOf('{', i);
                    if (abre < 0)
                        break;
                    int cierra = css.IndexOf('}', abre);
                    if (cierra < 0)
                        break;
                    string selectores = css.Substring(i, abre - i);
                    var props = Propiedades(css.Substring(abre + 1, cierra - abre - 1));
                    foreach (var sel in selectores.Split(','))
                    {
                        string s = sel.Trim();
                        if (s.StartsWith(".") && s.IndexOf(' ') < 0)
                        {
                            string nombre = s.Substring(1);
                            Dictionary<string, string> ya;
                            if (!clases.TryGetValue(nombre, out ya))
                                clases[nombre] = ya = new Dictionary<string, string>();
                            foreach (var kv in props)
                                ya[kv.Key] = kv.Value;
                        }
                    }
                    i = cierra + 1;
                }
            }
            else
            {
                LeerEstilos(h);
            }
        }
    }

    static Dictionary<string, string> Propiedades(string texto)
    {
        var r = new Dictionary<string, string>();
        foreach (var parte in texto.Split(';'))
        {
            int dos = parte.IndexOf(':');
            if (dos <= 0)
                continue;
            r[parte.Substring(0, dos).Trim().ToLowerInvariant()] = parte.Substring(dos + 1).Trim();
        }
        return r;
    }

    static string Atributo(XmlNode n, string nombre)
    {
        var a = n.Attributes != null ? n.Attributes[nombre] : null;
        return a != null ? a.Value : null;
    }

    Estilo EstiloDe(XmlNode n, Estilo padre)
    {
        var e = padre.Copia();
        // Orden de prioridad: atributos < clases < style="...".
        Poner(e, "fill", Atributo(n, "fill"));
        Poner(e, "stroke", Atributo(n, "stroke"));
        Poner(e, "stroke-width", Atributo(n, "stroke-width"));
        Poner(e, "display", Atributo(n, "display"));
        Poner(e, "visibility", Atributo(n, "visibility"));
        string clase = Atributo(n, "class");
        if (!string.IsNullOrEmpty(clase))
            foreach (var c in clase.Split(new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                Dictionary<string, string> props;
                if (clases.TryGetValue(c, out props))
                    foreach (var kv in props)
                        Poner(e, kv.Key, kv.Value);
            }
        string estilo = Atributo(n, "style");
        if (!string.IsNullOrEmpty(estilo))
            foreach (var kv in Propiedades(estilo))
                Poner(e, kv.Key, kv.Value);
        return e;
    }

    static void Poner(Estilo e, string clave, string valor)
    {
        if (valor == null)
            return;
        valor = valor.Trim();
        switch (clave)
        {
            case "fill": e.relleno = valor; break;
            case "stroke": e.linea = valor; break;
            case "stroke-width":
                float g;
                if (Numero(valor, out g))
                    e.grosor = g;
                break;
            case "display": if (valor == "none") e.visible = false; break;
            case "visibility": if (valor == "hidden" || valor == "collapse") e.visible = false; break;
        }
    }

    static bool Numero(string s, out float v)
    {
        s = s.Trim();
        int fin = 0;
        while (fin < s.Length && (char.IsDigit(s[fin]) || s[fin] == '.' || s[fin] == '-' || s[fin] == '+' || s[fin] == 'e' || s[fin] == 'E'))
            fin++;
        return float.TryParse(s.Substring(0, fin), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
    }

    static float Num(XmlNode n, string nombre)
    {
        string s = Atributo(n, nombre);
        float v;
        return s != null && Numero(s, out v) ? v : 0f;
    }

    void Recorrer(XmlNode nodo, Matriz m, Estilo estiloPadre)
    {
        foreach (XmlNode h in nodo.ChildNodes)
        {
            if (h.NodeType != XmlNodeType.Element)
                continue;
            if (caminos.Count >= MaxLineas || nodosLeidos >= MaxNodos)
                return;
            string tipo = h.LocalName;
            if (tipo == "defs" || tipo == "clipPath" || tipo == "mask" || tipo == "symbol" || tipo == "style" || tipo == "text"
                || tipo == "pattern" || tipo == "marker" || tipo == "metadata" || tipo == "title" || tipo == "desc" || tipo == "image")
                continue;
            var estilo = EstiloDe(h, estiloPadre);
            if (!estilo.visible)
                continue;
            var mh = m.Por(LeerTransformacion(Atributo(h, "transform")));
            int antes = caminos.Count;
            switch (tipo)
            {
                case "g":
                case "svg":
                case "a":
                case "switch":
                    Recorrer(h, mh, estilo);
                    continue;
                case "path":
                    LeerRuta(Atributo(h, "d") ?? "", mh);
                    break;
                case "rect":
                {
                    float x = Num(h, "x"), y = Num(h, "y"), w = Num(h, "width"), hh = Num(h, "height");
                    if (w > 0f && hh > 0f)
                        Poligono(new[] { new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + hh), new Vector2(x, y + hh) }, true, mh);
                    break;
                }
                case "circle":
                {
                    float r = Num(h, "r");
                    if (r > 0f)
                        Elipse(Num(h, "cx"), Num(h, "cy"), r, r, mh);
                    break;
                }
                case "ellipse":
                {
                    float rx = Num(h, "rx"), ry = Num(h, "ry");
                    if (rx > 0f && ry > 0f)
                        Elipse(Num(h, "cx"), Num(h, "cy"), rx, ry, mh);
                    break;
                }
                case "line":
                    Poligono(new[] { new Vector2(Num(h, "x1"), Num(h, "y1")), new Vector2(Num(h, "x2"), Num(h, "y2")) }, false, mh);
                    break;
                case "polyline":
                case "polygon":
                {
                    var nums = Numeros(Atributo(h, "points") ?? "");
                    var pts = new List<Vector2>();
                    for (int i = 0; i + 1 < nums.Count; i += 2)
                        pts.Add(new Vector2(nums[i], nums[i + 1]));
                    if (pts.Count >= 2)
                        Poligono(pts.ToArray(), tipo == "polygon", mh);
                    break;
                }
                default:
                    continue;
            }
            // El color y el grosor de lo que se acaba de leer.
            Color cr, cl;
            bool hayR = LeerColor(estilo.relleno, out cr);
            bool hayL = LeerColor(estilo.linea, out cl);
            if (tipo == "line" || tipo == "polyline")
                hayR = hayR && tipo == "polyline";
            for (int i = caminos.Count - 1; i >= antes; i--)
            {
                var c = caminos[i];
                if (!hayR && !hayL)
                {
                    caminos.RemoveAt(i);
                    continue;
                }
                c.hayRelleno = hayR;
                c.colorRelleno = cr;
                c.hayLinea = hayL;
                c.colorLinea = cl;
                c.grosor = estilo.grosor * mh.EscalaMedia;
            }
        }
    }

    // ---------- Formas ----------

    Camino Nuevo(Vector2 p)
    {
        var c = new Camino();
        c.nodos.Add(p);
        c.entra.Add(p);
        c.sale.Add(p);
        return c;
    }

    void Guardar(Camino c, Matriz m)
    {
        if (c == null || c.nodos.Count < 2)
            return;
        // Cerrado: si el último nodo cae sobre el primero, se juntan (el primero se queda con el tirador de entrada).
        int n = c.nodos.Count;
        if (c.cerrado && n >= 3 && (c.nodos[n - 1] - c.nodos[0]).sqrMagnitude < 1e-8f)
        {
            c.entra[0] = c.entra[n - 1];
            c.nodos.RemoveAt(n - 1);
            c.entra.RemoveAt(n - 1);
            c.sale.RemoveAt(n - 1);
        }
        if (!c.cerrado && c.nodos.Count < 2)
            return;
        for (int i = 0; i < c.nodos.Count; i++)
        {
            c.nodos[i] = m.Aplicar(c.nodos[i]);
            c.entra[i] = m.Aplicar(c.entra[i]);
            c.sale[i] = m.Aplicar(c.sale[i]);
        }
        nodosLeidos += c.nodos.Count;
        caminos.Add(c);
    }

    void Poligono(Vector2[] pts, bool cerrado, Matriz m)
    {
        var c = Nuevo(pts[0]);
        for (int i = 1; i < pts.Length; i++)
        {
            c.nodos.Add(pts[i]);
            c.entra.Add(pts[i]);
            c.sale.Add(pts[i]);
        }
        if (cerrado)
        {
            c.nodos.Add(pts[0]);
            c.entra.Add(pts[0]);
            c.sale.Add(pts[0]);
            c.cerrado = true;
        }
        Guardar(c, m);
    }

    // Círculo / elipse: 4 nodos con tiradores (como lo hacen los programas de dibujo).
    void Elipse(float cx, float cy, float rx, float ry, Matriz m)
    {
        const float k = 0.5522848f;
        var c = new Camino { cerrado = true };
        Vector2[] p = { new Vector2(cx + rx, cy), new Vector2(cx, cy + ry), new Vector2(cx - rx, cy), new Vector2(cx, cy - ry) };
        Vector2[] t = { new Vector2(0f, ry * k), new Vector2(-rx * k, 0f), new Vector2(0f, -ry * k), new Vector2(rx * k, 0f) };
        for (int i = 0; i < 4; i++)
        {
            c.nodos.Add(p[i]);
            c.entra.Add(p[i] - t[i]);
            c.sale.Add(p[i] + t[i]);
        }
        Guardar(c, m);
    }

    static List<float> Numeros(string s)
    {
        var r = new List<float>();
        int i = 0;
        float v;
        while (LeerNumero(s, ref i, out v, false))
            r.Add(v);
        return r;
    }

    // Lee el siguiente número (saltando espacios y comas). "bandera": un solo 0 o 1 (los arcos los pegan: "a5 5 0 105 5").
    static bool LeerNumero(string s, ref int i, out float v, bool bandera)
    {
        v = 0f;
        while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ','))
            i++;
        if (i >= s.Length)
            return false;
        if (bandera)
        {
            if (s[i] == '0' || s[i] == '1')
            {
                v = s[i] - '0';
                i++;
                return true;
            }
            return false;
        }
        int ini = i;
        if (s[i] == '+' || s[i] == '-')
            i++;
        bool punto = false, digitos = false;
        while (i < s.Length)
        {
            char ch = s[i];
            if (char.IsDigit(ch))
            {
                digitos = true;
                i++;
            }
            else if (ch == '.' && !punto)
            {
                punto = true;
                i++;
            }
            else
            {
                break;
            }
        }
        if (!digitos)
        {
            i = ini;
            return false;
        }
        if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
        {
            int j = i + 1;
            if (j < s.Length && (s[j] == '+' || s[j] == '-'))
                j++;
            if (j < s.Length && char.IsDigit(s[j]))
            {
                i = j;
                while (i < s.Length && char.IsDigit(s[i]))
                    i++;
            }
        }
        return float.TryParse(s.Substring(ini, i - ini), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
    }

    // El atributo "d" de un <path>: M L H V C S Q T A Z (mayúscula = absoluto, minúscula = relativo).
    void LeerRuta(string d, Matriz m)
    {
        int i = 0;
        char cmd = ' ';
        Vector2 actual = Vector2.zero, inicio = Vector2.zero;
        Vector2 ultimoControl = Vector2.zero;   // para S y T (el reflejo del tirador anterior)
        char cmdAnterior = ' ';
        Camino c = null;
        while (i < d.Length)
        {
            while (i < d.Length && (char.IsWhiteSpace(d[i]) || d[i] == ','))
                i++;
            if (i >= d.Length)
                break;
            char ch = d[i];
            if (char.IsLetter(ch) && ch != 'e' && ch != 'E')
            {
                cmd = ch;
                i++;
                if (cmd == 'Z' || cmd == 'z')
                {
                    if (c != null)
                    {
                        // Si no termina donde empezó, se cierra con una recta.
                        if ((actual - inicio).sqrMagnitude > 1e-10f)
                            Recta(c, inicio);
                        else
                            c.cerrado = true;
                        c.cerrado = true;
                        Guardar(c, m);
                        c = null;
                    }
                    actual = inicio;
                    cmdAnterior = 'Z';
                    continue;
                }
            }
            else if (cmd == ' ')
            {
                i++;   // basura antes del primer comando
                continue;
            }
            bool rel = char.IsLower(cmd);
            char C = char.ToUpperInvariant(cmd);
            Vector2 b = rel ? actual : Vector2.zero;
            float x, y, x1, y1, x2, y2;
            int antes = i;
            switch (C)
            {
                case 'M':
                    if (!LeerNumero(d, ref i, out x, false) || !LeerNumero(d, ref i, out y, false))
                        return;
                    if (c != null)
                        Guardar(c, m);
                    actual = b + new Vector2(x, y);
                    inicio = actual;
                    c = Nuevo(actual);
                    cmd = rel ? 'l' : 'L';   // los pares que siguen a una M son rectas
                    break;
                case 'L':
                    if (!LeerNumero(d, ref i, out x, false) || !LeerNumero(d, ref i, out y, false))
                        return;
                    actual = b + new Vector2(x, y);
                    if (c == null) c = Nuevo(inicio);
                    Recta(c, actual);
                    break;
                case 'H':
                    if (!LeerNumero(d, ref i, out x, false))
                        return;
                    actual = new Vector2(rel ? actual.x + x : x, actual.y);
                    if (c == null) c = Nuevo(inicio);
                    Recta(c, actual);
                    break;
                case 'V':
                    if (!LeerNumero(d, ref i, out y, false))
                        return;
                    actual = new Vector2(actual.x, rel ? actual.y + y : y);
                    if (c == null) c = Nuevo(inicio);
                    Recta(c, actual);
                    break;
                case 'C':
                    if (!LeerNumero(d, ref i, out x1, false) || !LeerNumero(d, ref i, out y1, false) || !LeerNumero(d, ref i, out x2, false)
                        || !LeerNumero(d, ref i, out y2, false) || !LeerNumero(d, ref i, out x, false) || !LeerNumero(d, ref i, out y, false))
                        return;
                    if (c == null) c = Nuevo(inicio);
                    Curva(c, b + new Vector2(x1, y1), b + new Vector2(x2, y2), b + new Vector2(x, y));
                    ultimoControl = b + new Vector2(x2, y2);
                    actual = b + new Vector2(x, y);
                    break;
                case 'S':
                {
                    if (!LeerNumero(d, ref i, out x2, false) || !LeerNumero(d, ref i, out y2, false) || !LeerNumero(d, ref i, out x, false)
                        || !LeerNumero(d, ref i, out y, false))
                        return;
                    char ca = char.ToUpperInvariant(cmdAnterior);
                    Vector2 c1 = ca == 'C' || ca == 'S' ? actual * 2f - ultimoControl : actual;
                    if (c == null) c = Nuevo(inicio);
                    Curva(c, c1, b + new Vector2(x2, y2), b + new Vector2(x, y));
                    ultimoControl = b + new Vector2(x2, y2);
                    actual = b + new Vector2(x, y);
                    break;
                }
                case 'Q':
                {
                    if (!LeerNumero(d, ref i, out x1, false) || !LeerNumero(d, ref i, out y1, false) || !LeerNumero(d, ref i, out x, false)
                        || !LeerNumero(d, ref i, out y, false))
                        return;
                    Vector2 q = b + new Vector2(x1, y1), fin = b + new Vector2(x, y);
                    if (c == null) c = Nuevo(inicio);
                    Curva(c, actual + (q - actual) * (2f / 3f), fin + (q - fin) * (2f / 3f), fin);
                    ultimoControl = q;
                    actual = fin;
                    break;
                }
                case 'T':
                {
                    if (!LeerNumero(d, ref i, out x, false) || !LeerNumero(d, ref i, out y, false))
                        return;
                    char ca = char.ToUpperInvariant(cmdAnterior);
                    Vector2 q = ca == 'Q' || ca == 'T' ? actual * 2f - ultimoControl : actual;
                    Vector2 fin = b + new Vector2(x, y);
                    if (c == null) c = Nuevo(inicio);
                    Curva(c, actual + (q - actual) * (2f / 3f), fin + (q - fin) * (2f / 3f), fin);
                    ultimoControl = q;
                    actual = fin;
                    break;
                }
                case 'A':
                {
                    float rx, ry, rot, grande, barrido;
                    if (!LeerNumero(d, ref i, out rx, false) || !LeerNumero(d, ref i, out ry, false) || !LeerNumero(d, ref i, out rot, false)
                        || !LeerNumero(d, ref i, out grande, true) || !LeerNumero(d, ref i, out barrido, true)
                        || !LeerNumero(d, ref i, out x, false) || !LeerNumero(d, ref i, out y, false))
                        return;
                    Vector2 fin = b + new Vector2(x, y);
                    if (c == null) c = Nuevo(inicio);
                    Arco(c, actual, fin, Mathf.Abs(rx), Mathf.Abs(ry), rot, grande > 0.5f, barrido > 0.5f);
                    actual = fin;
                    break;
                }
                default:
                    i++;
                    break;
            }
            cmdAnterior = cmd;
            if (i == antes)
                i++;   // nunca quedarse trabado
        }
        if (c != null)
            Guardar(c, m);
    }

    static void Recta(Camino c, Vector2 p)
    {
        c.nodos.Add(p);
        c.entra.Add(p);
        c.sale.Add(p);
    }

    static void Curva(Camino c, Vector2 c1, Vector2 c2, Vector2 p)
    {
        c.sale[c.sale.Count - 1] = c1;
        c.nodos.Add(p);
        c.entra.Add(c2);
        c.sale.Add(p);
    }

    // Arco elíptico del SVG → curvas cúbicas (una por cada cuarto de vuelta o menos).
    static void Arco(Camino c, Vector2 p0, Vector2 p1, float rx, float ry, float rotGrados, bool grande, bool barrido)
    {
        if ((p1 - p0).sqrMagnitude < 1e-12f)
            return;
        if (rx < 1e-6f || ry < 1e-6f)
        {
            Recta(c, p1);
            return;
        }
        float phi = rotGrados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(phi), sin = Mathf.Sin(phi);
        Vector2 dm = (p0 - p1) * 0.5f;
        float x1p = cos * dm.x + sin * dm.y, y1p = -sin * dm.x + cos * dm.y;
        float lambda = x1p * x1p / (rx * rx) + y1p * y1p / (ry * ry);
        if (lambda > 1f)
        {
            float sl = Mathf.Sqrt(lambda);
            rx *= sl;
            ry *= sl;
        }
        float num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
        float den = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
        float coef = den > 1e-12f ? Mathf.Sqrt(Mathf.Max(0f, num / den)) : 0f;
        if (grande == barrido)
            coef = -coef;
        float cxp = coef * rx * y1p / ry, cyp = -coef * ry * x1p / rx;
        Vector2 medio = (p0 + p1) * 0.5f;
        Vector2 centro = new Vector2(cos * cxp - sin * cyp + medio.x, sin * cxp + cos * cyp + medio.y);
        float a1 = Mathf.Atan2((y1p - cyp) / ry, (x1p - cxp) / rx);
        float a2 = Mathf.Atan2((-y1p - cyp) / ry, (-x1p - cxp) / rx);
        float delta = a2 - a1;
        if (barrido && delta < 0f)
            delta += 2f * Mathf.PI;
        else if (!barrido && delta > 0f)
            delta -= 2f * Mathf.PI;
        int partes = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(delta) / (Mathf.PI * 0.5f) - 1e-4f), 1, 8);
        float paso = delta / partes;
        float k = 4f / 3f * Mathf.Tan(paso / 4f);
        float ang = a1;
        for (int s = 0; s < partes; s++)
        {
            float b1 = ang, b2 = ang + paso;
            Vector2 e1 = new Vector2(Mathf.Cos(b1), Mathf.Sin(b1)), e2 = new Vector2(Mathf.Cos(b2), Mathf.Sin(b2));
            Vector2 q0 = Punto(centro, rx, ry, cos, sin, e1);
            Vector2 q3 = s == partes - 1 ? p1 : Punto(centro, rx, ry, cos, sin, e2);
            Vector2 q1 = q0 + Direccion(rx, ry, cos, sin, new Vector2(-e1.y, e1.x)) * k;
            Vector2 q2 = Punto(centro, rx, ry, cos, sin, e2) - Direccion(rx, ry, cos, sin, new Vector2(-e2.y, e2.x)) * k;
            Curva(c, q1, q2, q3);
            ang = b2;
        }
    }

    static Vector2 Punto(Vector2 centro, float rx, float ry, float cos, float sin, Vector2 e)
    {
        return centro + Direccion(rx, ry, cos, sin, e);
    }

    static Vector2 Direccion(float rx, float ry, float cos, float sin, Vector2 e)
    {
        float x = rx * e.x, y = ry * e.y;
        return new Vector2(cos * x - sin * y, sin * x + cos * y);
    }

    // transform="translate(10 20) rotate(30) scale(2)..." (se aplican de derecha a izquierda, como en el SVG).
    static Matriz LeerTransformacion(string s)
    {
        var m = Matriz.Identidad;
        if (string.IsNullOrEmpty(s))
            return m;
        int i = 0;
        while (i < s.Length)
        {
            int abre = s.IndexOf('(', i);
            if (abre < 0)
                break;
            int cierra = s.IndexOf(')', abre);
            if (cierra < 0)
                break;
            string nombre = s.Substring(i, abre - i).Trim().Trim(',').Trim();
            var v = Numeros(s.Substring(abre + 1, cierra - abre - 1));
            var t = Matriz.Identidad;
            switch (nombre)
            {
                case "matrix":
                    if (v.Count >= 6)
                        t = new Matriz { a = v[0], b = v[1], c = v[2], d = v[3], e = v[4], f = v[5] };
                    break;
                case "translate":
                    if (v.Count >= 1)
                    {
                        t.e = v[0];
                        t.f = v.Count >= 2 ? v[1] : 0f;
                    }
                    break;
                case "scale":
                    if (v.Count >= 1)
                    {
                        t.a = v[0];
                        t.d = v.Count >= 2 ? v[1] : v[0];
                    }
                    break;
                case "rotate":
                    if (v.Count >= 1)
                    {
                        float r = v[0] * Mathf.Deg2Rad;
                        var rot = new Matriz { a = Mathf.Cos(r), b = Mathf.Sin(r), c = -Mathf.Sin(r), d = Mathf.Cos(r) };
                        if (v.Count >= 3)
                        {
                            var ida = new Matriz { a = 1f, d = 1f, e = v[1], f = v[2] };
                            var vuelta = new Matriz { a = 1f, d = 1f, e = -v[1], f = -v[2] };
                            t = ida.Por(rot).Por(vuelta);
                        }
                        else
                        {
                            t = rot;
                        }
                    }
                    break;
                case "skewX":
                    if (v.Count >= 1)
                        t.c = Mathf.Tan(v[0] * Mathf.Deg2Rad);
                    break;
                case "skewY":
                    if (v.Count >= 1)
                        t.b = Mathf.Tan(v[0] * Mathf.Deg2Rad);
                    break;
            }
            m = m.Por(t);
            i = cierra + 1;
        }
        return m;
    }

    static readonly Dictionary<string, Color32> Nombres = new Dictionary<string, Color32>
    {
        { "black", new Color32(0, 0, 0, 255) }, { "white", new Color32(255, 255, 255, 255) },
        { "red", new Color32(255, 0, 0, 255) }, { "green", new Color32(0, 128, 0, 255) },
        { "lime", new Color32(0, 255, 0, 255) }, { "blue", new Color32(0, 0, 255, 255) },
        { "yellow", new Color32(255, 255, 0, 255) }, { "orange", new Color32(255, 165, 0, 255) },
        { "purple", new Color32(128, 0, 128, 255) }, { "pink", new Color32(255, 192, 203, 255) },
        { "brown", new Color32(165, 42, 42, 255) }, { "gray", new Color32(128, 128, 128, 255) },
        { "grey", new Color32(128, 128, 128, 255) }, { "cyan", new Color32(0, 255, 255, 255) },
        { "magenta", new Color32(255, 0, 255, 255) }, { "navy", new Color32(0, 0, 128, 255) },
        { "teal", new Color32(0, 128, 128, 255) }, { "maroon", new Color32(128, 0, 0, 255) },
        { "olive", new Color32(128, 128, 0, 255) }, { "silver", new Color32(192, 192, 192, 255) },
        { "gold", new Color32(255, 215, 0, 255) }, { "beige", new Color32(245, 245, 220, 255) },
    };

    // "none" (o un degradado url(#...), que no se importa) = no hay.
    static bool LeerColor(string s, out Color c)
    {
        c = Color.black;
        if (string.IsNullOrEmpty(s))
            return false;
        s = s.Trim().ToLowerInvariant();
        if (s == "none" || s == "transparent")
            return false;
        if (s.StartsWith("url("))
        {
            // Un degradado: se usa un gris medio (mejor que perder la forma).
            c = new Color(0.6f, 0.6f, 0.6f);
            return true;
        }
        if (s == "currentcolor")
            return true;
        if (s.StartsWith("#"))
        {
            string h = s.Substring(1);
            if (h.Length == 3 || h.Length == 4)
                h = new string(new[] { h[0], h[0], h[1], h[1], h[2], h[2] });
            if (h.Length >= 6)
            {
                Color leido;
                if (ColorUtility.TryParseHtmlString("#" + h.Substring(0, 6), out leido))
                {
                    c = leido;
                    return true;
                }
            }
            return false;
        }
        if (s.StartsWith("rgb"))
        {
            int abre = s.IndexOf('('), cierra = s.IndexOf(')');
            if (abre < 0 || cierra < abre)
                return false;
            var partes = s.Substring(abre + 1, cierra - abre - 1).Split(',');
            if (partes.Length < 3)
                return false;
            float[] v = new float[3];
            for (int i = 0; i < 3; i++)
            {
                string p = partes[i].Trim();
                bool porcentaje = p.EndsWith("%");
                float n;
                if (!Numero(p.TrimEnd('%'), out n))
                    return false;
                v[i] = Mathf.Clamp01(porcentaje ? n / 100f : n / 255f);
            }
            c = new Color(v[0], v[1], v[2]);
            return true;
        }
        Color32 nombrado;
        if (Nombres.TryGetValue(s, out nombrado))
        {
            c = nombrado;
            return true;
        }
        return true;   // un nombre que no conozco: negro (pero la forma se queda)
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}

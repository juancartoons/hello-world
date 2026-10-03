using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

// "Mis archivos": todo lo que has hecho (dibujos, videos, fotos y SVG), con una miniatura de cada uno.
//  - Arriba: filtros (Todos, Dibujos, Videos, Fotos, Ver SVG) y la X para cerrar.
//  - Debajo: el ARCHIVO ACTUAL (su nombre y si tiene cambios sin guardar) con Guardar, Copia y Renombrar,
//    y el botón de idioma (Español / English) para toda la app.
//  - Toca un archivo para elegirlo; abajo: Abrir y Borrar (Borrar pide tocar otra vez para confirmar).
//  - Abrir: un dibujo se abre para seguir editándolo; una foto o un video se ven en grande (Volver = regresar).
//  - < y > cambian de página.
// Se abre con el botón "Archivos" del panel de arriba o del menú de la mano.
public class NavegadorArchivos : MonoBehaviour
{
    public Dibujo dibujo;
    public ControlManos control;
    public Material materialPanel;
    public Material materialBoton;
    public Material materialBotonMarcado;
    [Tooltip("Material sin luz para mostrar imágenes (las miniaturas)")]
    public Material materialImagen;

    static readonly string[] Filtros = { "Todos", "Dibujos", "Videos", "Fotos", "Ver SVG" };
    const int Columnas = 4, Filas = 3, PorPagina = Columnas * Filas;

    class Archivo
    {
        public string ruta;
        public string tipo;
        public System.DateTime fecha;
    }

    class Ficha
    {
        public BotonTocable boton;
        public Renderer vista;
        public Material material;
        public TMP_Text texto;
        public TMP_Text grande; // "SVG" cuando no hay miniatura
        public Texture2D textura;
    }

    Transform raiz;
    readonly List<Archivo> archivos = new List<Archivo>();
    readonly Ficha[] fichas = new Ficha[PorPagina];
    readonly BotonTocable[] botonesFiltro = new BotonTocable[Filtros.Length];
    BotonTocable btnAbrir, btnBorrar, btnAnterior, btnSiguiente, btnCerrar, btnVolver;
    BotonTocable btnGuardar, btnCopia, btnRenombrar, btnIdioma;
    TMP_Text textoPagina, textoEstado, textoActual;
    TouchScreenKeyboard teclado;
    GameObject grilla;
    Renderer visor;
    Material materialVisor;
    Texture2D texturaVisor;
    VideoPlayer video;
    int filtro, pagina, elegido = -1;
    float confirmarBorrarHasta = -1f;

    public bool Abierto => raiz != null && raiz.gameObject.activeSelf;

    void Start()
    {
        Idioma.alCambiar += () => { if (Abierto) Mostrar(); };
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (control == null) control = FindFirstObjectByType<ControlManos>();
    }

    // ---------- Abrir y cerrar ----------

    public void Abrir(Transform cabeza)
    {
        if (dibujo == null)
            return;
        if (raiz == null)
            Armar();
        raiz.gameObject.SetActive(true);
        if (cabeza != null)
        {
            Vector3 adelante = cabeza.forward;
            adelante.y = 0f;
            adelante = adelante.sqrMagnitude > 1e-4f ? adelante.normalized : Vector3.forward;
            Vector3 pos = cabeza.position + adelante * 0.55f - Vector3.up * 0.06f;
            raiz.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cabeza.position, Vector3.up));
        }
        CerrarVisor();
        pagina = 0;
        elegido = -1;
        Listar();
        Mostrar();
        dibujo.Mensaje("Mis archivos: toca uno para elegirlo");
    }

    public void Cerrar()
    {
        CerrarVisor();
        LiberarMiniaturas();
        if (raiz != null)
            raiz.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!Abierto)
            return;
        // El archivo actual: nombre y si hay cambios sin guardar.
        if (textoActual != null)
        {
            string nombre = string.IsNullOrEmpty(dibujo.NombreArchivo) ? Idioma.T("(sin nombre)") : dibujo.NombreArchivo;
            textoActual.text = Idioma.T("Actual: ") + nombre + (dibujo.HayCambios ? Idioma.T(" · sin guardar") : "");
        }
        if (btnIdioma != null)
            btnIdioma.PonerTexto(Idioma.Ingles ? "Language: ENG" : "Idioma: ESP");
        // Teclado del visor (Renombrar).
        if (teclado != null)
        {
            if (teclado.status == TouchScreenKeyboard.Status.Done)
            {
                dibujo.Renombrar(teclado.text);
                teclado = null;
                Listar();
                Mostrar();
            }
            else if (teclado.status != TouchScreenKeyboard.Status.Visible)
            {
                teclado = null;
            }
        }
        if (confirmarBorrarHasta > 0f && Time.time > confirmarBorrarHasta)
        {
            confirmarBorrarHasta = -1f;
            RefrescarBotones();
        }
    }

    // ---------- La lista ----------

    void Listar()
    {
        archivos.Clear();
        string carpeta = dibujo.CarpetaDibujos;
        try
        {
            if (filtro == 0 || filtro == 1)
            {
                int antes = archivos.Count;
                if (Directory.Exists(dibujo.CarpetaArchivosDibujo))
                {
                    foreach (var f in Directory.GetFiles(dibujo.CarpetaArchivosDibujo, "*" + Dibujo.Extension))
                        Agregar(f, "Dibujo");
                    foreach (var f in Directory.GetFiles(dibujo.CarpetaArchivosDibujo, "*.json"))
                        Agregar(f, "Dibujo");
                }
                // El "guardado" de antes solo si aún no hay dibujos con nombre (si no, sería una copia repetida).
                if (archivos.Count == antes && File.Exists(dibujo.RutaGuardadoAnterior))
                    Agregar(dibujo.RutaGuardadoAnterior, "Dibujo");
                if (Directory.Exists(carpeta))
                    foreach (var f in Directory.GetFiles(carpeta, "dibujo_*.json"))
                        Agregar(f, "Dibujo");
            }
            if (Directory.Exists(carpeta))
            {
                if (filtro == 0 || filtro == 2)
                    foreach (var f in Directory.GetFiles(carpeta, "*.mp4"))
                        Agregar(f, "Video");
                if (filtro == 0 || filtro == 3)
                    foreach (var f in Directory.GetFiles(carpeta, "*.png"))
                        Agregar(f, "Foto");
                if (filtro == 0 || filtro == 4)
                    foreach (var f in Directory.GetFiles(carpeta, "*.svg"))
                        Agregar(f, "SVG");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo leer la carpeta: " + e.Message);
        }
        archivos.Sort((a, b) => b.fecha.CompareTo(a.fecha));
    }

    void Agregar(string ruta, string tipo)
    {
        archivos.Add(new Archivo { ruta = ruta, tipo = tipo, fecha = File.GetLastWriteTime(ruta) });
    }

    int Paginas => Mathf.Max(1, (archivos.Count + PorPagina - 1) / PorPagina);

    void Mostrar()
    {
        LiberarMiniaturas();
        pagina = Mathf.Clamp(pagina, 0, Paginas - 1);
        for (int i = 0; i < PorPagina; i++)
        {
            var f = fichas[i];
            int k = pagina * PorPagina + i;
            bool hay = k < archivos.Count;
            f.boton.gameObject.SetActive(hay);
            f.vista.gameObject.SetActive(false);
            f.texto.gameObject.SetActive(hay);
            f.grande.gameObject.SetActive(false);
            if (!hay)
                continue;
            var a = archivos[k];
            f.texto.text = Idioma.T(a.tipo) + " · " + Path.GetFileNameWithoutExtension(a.ruta) + "\n" + a.fecha.ToString("dd/MM HH:mm");
            f.textura = CargarMiniatura(a);
            if (f.textura != null)
            {
                PonerTextura(f.material, f.textura);
                f.vista.gameObject.SetActive(true);
                float aspecto = f.textura.width / (float)Mathf.Max(1, f.textura.height);
                f.vista.transform.localScale = aspecto >= 1.6f ? new Vector3(0.125f, 0.125f / aspecto, 1f)
                                                                : new Vector3(0.07f * aspecto, 0.07f, 1f);
            }
            else
            {
                f.grande.text = Idioma.T(a.tipo);
                f.grande.gameObject.SetActive(true);
            }
            f.boton.Marcar(k == elegido);
        }
        foreach (var x in botonesFiltro)
            x.Marcar(System.Array.IndexOf(botonesFiltro, x) == filtro);
        textoPagina.text = archivos.Count == 0 ? Idioma.T("Vacío") : (pagina + 1) + " / " + Paginas;
        RefrescarBotones();
    }

    void RefrescarBotones()
    {
        bool hay = elegido >= 0 && elegido < archivos.Count;
        btnAbrir.gameObject.SetActive(hay);
        btnAbrir.etiqueta.gameObject.SetActive(hay);
        btnBorrar.gameObject.SetActive(hay);
        btnBorrar.etiqueta.gameObject.SetActive(hay);
        bool confirmar = confirmarBorrarHasta > 0f;
        btnBorrar.PonerTexto(confirmar ? "¿Seguro? Toca otra vez" : "Borrar");
        btnBorrar.Marcar(confirmar);
        if (hay)
        {
            var a = archivos[elegido];
            btnAbrir.PonerTexto(a.tipo == "Dibujo" ? "Abrir dibujo" : a.tipo == "SVG" ? "Dónde está" : "Ver");
            textoEstado.text = Path.GetFileName(a.ruta);
        }
        else
        {
            textoEstado.text = archivos.Count + Idioma.T(" archivos");
        }
    }

    Texture2D CargarMiniatura(Archivo a)
    {
        string mini = dibujo.RutaMiniatura(a.ruta);
        if (File.Exists(mini))
            return Leer(mini);
        if (a.tipo != "Foto")
            return null;
        // Fotos de antes (sin miniatura): se lee en pequeño y se guarda la miniatura para la próxima vez.
        var grande = Leer(a.ruta);
        if (grande == null)
            return null;
        var chica = Reducir(grande, 320);
        Destroy(grande);
        try
        {
            Directory.CreateDirectory(dibujo.CarpetaMiniaturas);
            File.WriteAllBytes(mini, chica.EncodeToPNG());
        }
        catch (System.Exception)
        {
            // si no se puede guardar, igual se muestra
        }
        return chica;
    }

    static Texture2D Leer(string ruta)
    {
        try
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (t.LoadImage(File.ReadAllBytes(ruta)))
                return t;
            Destroy(t);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo leer la imagen: " + e.Message);
        }
        return null;
    }

    static Texture2D Reducir(Texture2D t, int ancho)
    {
        int alto = Mathf.Max(1, Mathf.RoundToInt(ancho * t.height / (float)Mathf.Max(1, t.width)));
        var rt = RenderTexture.GetTemporary(ancho, alto, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(t, rt);
        var anterior = RenderTexture.active;
        RenderTexture.active = rt;
        var chica = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        chica.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        chica.Apply(false);
        RenderTexture.active = anterior;
        RenderTexture.ReleaseTemporary(rt);
        return chica;
    }

    static void PonerTextura(Material m, Texture t)
    {
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
    }

    void LiberarMiniaturas()
    {
        foreach (var f in fichas)
        {
            if (f == null || f.textura == null)
                continue;
            Destroy(f.textura);
            f.textura = null;
        }
    }

    // ---------- Acciones ----------

    void Elegir(int i)
    {
        int k = pagina * PorPagina + i;
        if (k >= archivos.Count)
            return;
        elegido = k;
        confirmarBorrarHasta = -1f;
        for (int j = 0; j < PorPagina; j++)
            fichas[j].boton.Marcar(pagina * PorPagina + j == elegido);
        RefrescarBotones();
    }

    void AbrirElegido()
    {
        if (elegido < 0 || elegido >= archivos.Count)
            return;
        var a = archivos[elegido];
        switch (a.tipo)
        {
            case "Dibujo":
                Cerrar();
                dibujo.AbrirArchivo(a.ruta);
                break;
            case "Foto":
                VerFoto(a.ruta);
                break;
            case "Video":
                VerVideo(a.ruta);
                break;
            default:
                dibujo.Mensaje("Los SVG se abren en el PC (Inkscape, Illustrator): Android/data/<la app>/files/Dibujos/" + Path.GetFileName(a.ruta));
                break;
        }
    }

    void Borrar()
    {
        if (elegido < 0 || elegido >= archivos.Count)
            return;
        if (confirmarBorrarHasta < 0f)
        {
            confirmarBorrarHasta = Time.time + 3f;
            RefrescarBotones();
            return;
        }
        confirmarBorrarHasta = -1f;
        var a = archivos[elegido];
        try
        {
            File.Delete(a.ruta);
            string mini = dibujo.RutaMiniatura(a.ruta);
            if (File.Exists(mini))
                File.Delete(mini);
            dibujo.Mensaje("Borrado: " + Path.GetFileName(a.ruta));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo borrar: " + e.Message);
            dibujo.Mensaje("No se pudo borrar");
        }
        elegido = -1;
        Listar();
        Mostrar();
    }

    void CambiarFiltro(int f)
    {
        filtro = f;
        pagina = 0;
        elegido = -1;
        confirmarBorrarHasta = -1f;
        Listar();
        Mostrar();
    }

    void CambiarPagina(int paso)
    {
        pagina = Mathf.Clamp(pagina + paso, 0, Paginas - 1);
        Mostrar();
    }

    void AbrirTeclado()
    {
        if (!TouchScreenKeyboard.isSupported)
        {
            dibujo.Mensaje("Este visor no tiene teclado aquí (vuelve a tocar ★ Armar escena en Unity)");
            return;
        }
        teclado = TouchScreenKeyboard.Open(dibujo.NombreArchivo ?? "", TouchScreenKeyboardType.Default, false, false, false, false, Idioma.T("Nombre del dibujo"));
    }

    // ---------- Ver en grande ----------

    void VerFoto(string ruta)
    {
        CerrarVisor();
        texturaVisor = Leer(ruta);
        if (texturaVisor == null)
        {
            dibujo.Mensaje("No se pudo abrir la foto");
            return;
        }
        PonerTextura(materialVisor, texturaVisor);
        AbrirVisor(texturaVisor.width / (float)Mathf.Max(1, texturaVisor.height));
    }

    void VerVideo(string ruta)
    {
        CerrarVisor();
        if (video == null)
        {
            video = visor.gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.renderMode = VideoRenderMode.MaterialOverride;
            video.targetMaterialRenderer = visor;
            video.targetMaterialProperty = materialVisor.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            video.isLooping = true;
            video.audioOutputMode = VideoAudioOutputMode.Direct;
        }
        video.url = ruta;
        video.Play();
        AbrirVisor(16f / 9f);
    }

    void AbrirVisor(float aspecto)
    {
        grilla.SetActive(false);
        visor.gameObject.SetActive(true);
        float ancho = 0.56f, alto = 0.315f;
        if (aspecto > ancho / alto)
            alto = ancho / aspecto;
        else
            ancho = alto * aspecto;
        visor.transform.localScale = new Vector3(ancho, alto, 1f);
        btnVolver.gameObject.SetActive(true);
        btnVolver.etiqueta.gameObject.SetActive(true);
    }

    void CerrarVisor()
    {
        if (video != null)
            video.Stop();
        if (texturaVisor != null)
        {
            Destroy(texturaVisor);
            texturaVisor = null;
        }
        if (visor != null)
            visor.gameObject.SetActive(false);
        if (grilla != null)
            grilla.SetActive(true);
        if (btnVolver != null)
        {
            btnVolver.gameObject.SetActive(false);
            btnVolver.etiqueta.gameObject.SetActive(false);
        }
    }

    // ---------- Armar el panel (una sola vez) ----------

    void Armar()
    {
        raiz = new GameObject("MisArchivos").transform;
        raiz.SetParent(transform, false);

        var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "Fondo";
        Destroy(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(raiz, false);
        fondo.transform.localPosition = new Vector3(0f, 0f, 0.006f);
        fondo.transform.localScale = new Vector3(0.64f, 0.48f, 1f);
        Pintar(fondo.GetComponent<Renderer>(), materialPanel);

        Texto(raiz, "Mis archivos", new Vector3(-0.235f, 0.205f, -0.002f), new Vector2(0.15f, 0.02f), 0.2f);
        for (int i = 0; i < Filtros.Length; i++)
        {
            int f = i;
            botonesFiltro[i] = Boton(raiz, Filtros[i], new Vector3(-0.1f + i * 0.072f, 0.205f, 0f), new Vector2(0.066f, 0.022f));
            botonesFiltro[i].alTocar.AddListener(() => CambiarFiltro(f));
        }
        btnCerrar = Boton(raiz, "X", new Vector3(0.295f, 0.205f, 0f), new Vector2(0.024f, 0.022f));
        btnCerrar.alTocar.AddListener(Cerrar);

        // El archivo actual (el que estás haciendo) y el idioma de la app.
        textoActual = Texto(raiz, "", new Vector3(-0.17f, 0.172f, -0.002f), new Vector2(0.27f, 0.018f), 0.12f);
        textoActual.alignment = TextAlignmentOptions.Left;
        btnGuardar = Boton(raiz, "Guardar", new Vector3(0.035f, 0.172f, 0f), new Vector2(0.06f, 0.02f));
        btnGuardar.alTocar.AddListener(() => { dibujo.Guardar(); Listar(); Mostrar(); });
        btnCopia = Boton(raiz, "Guardar copia", new Vector3(0.11f, 0.172f, 0f), new Vector2(0.08f, 0.02f));
        btnCopia.alTocar.AddListener(() => { dibujo.GuardarCopia(); Listar(); Mostrar(); });
        btnRenombrar = Boton(raiz, "Renombrar", new Vector3(0.195f, 0.172f, 0f), new Vector2(0.08f, 0.02f));
        btnRenombrar.alTocar.AddListener(AbrirTeclado);
        btnIdioma = Boton(raiz, "Idioma: ESP", new Vector3(0.28f, 0.172f, 0f), new Vector2(0.075f, 0.02f));
        btnIdioma.alTocar.AddListener(Idioma.Alternar);

        grilla = new GameObject("Grilla");
        grilla.transform.SetParent(raiz, false);
        for (int i = 0; i < PorPagina; i++)
        {
            int col = i % Columnas, fila = i / Columnas;
            float x = -0.225f + col * 0.15f;
            float y = 0.1f - fila * 0.112f;
            var f = new Ficha();
            f.boton = Boton(grilla.transform, "", new Vector3(x, y, 0f), new Vector2(0.138f, 0.105f));
            int indice = i;
            f.boton.alTocar.AddListener(() => Elegir(indice));
            var vista = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vista.name = "Miniatura";
            Destroy(vista.GetComponent<Collider>());
            vista.transform.SetParent(grilla.transform, false);
            vista.transform.localPosition = new Vector3(x, y + 0.012f, -0.005f);
            f.vista = vista.GetComponent<Renderer>();
            f.material = materialImagen != null ? new Material(materialImagen) : null;
            Pintar(f.vista, f.material);
            f.texto = Texto(grilla.transform, "", new Vector3(x, y - 0.037f, -0.005f), new Vector2(0.13f, 0.026f), 0.1f);
            f.grande = Texto(grilla.transform, "", new Vector3(x, y + 0.012f, -0.005f), new Vector2(0.12f, 0.05f), 0.4f);
            fichas[i] = f;
        }

        var goVisor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        goVisor.name = "Visor";
        Destroy(goVisor.GetComponent<Collider>());
        goVisor.transform.SetParent(raiz, false);
        goVisor.transform.localPosition = new Vector3(0f, 0.01f, -0.006f);
        visor = goVisor.GetComponent<Renderer>();
        materialVisor = materialImagen != null ? new Material(materialImagen) : null;
        Pintar(visor, materialVisor);
        goVisor.SetActive(false);

        btnAnterior = Boton(raiz, "<", new Vector3(-0.28f, -0.205f, 0f), new Vector2(0.03f, 0.022f));
        btnAnterior.alTocar.AddListener(() => CambiarPagina(-1));
        textoPagina = Texto(raiz, "1 / 1", new Vector3(-0.235f, -0.205f, -0.002f), new Vector2(0.05f, 0.018f), 0.15f);
        btnSiguiente = Boton(raiz, ">", new Vector3(-0.19f, -0.205f, 0f), new Vector2(0.03f, 0.022f));
        btnSiguiente.alTocar.AddListener(() => CambiarPagina(1));
        textoEstado = Texto(raiz, "", new Vector3(-0.06f, -0.205f, -0.002f), new Vector2(0.18f, 0.018f), 0.12f);
        btnAbrir = Boton(raiz, "Abrir", new Vector3(0.1f, -0.205f, 0f), new Vector2(0.1f, 0.024f));
        btnAbrir.alTocar.AddListener(AbrirElegido);
        btnBorrar = Boton(raiz, "Borrar", new Vector3(0.235f, -0.205f, 0f), new Vector2(0.13f, 0.024f));
        btnBorrar.alTocar.AddListener(Borrar);
        btnVolver = Boton(raiz, "Volver", new Vector3(0.235f, 0.205f - 0.04f, 0f), new Vector2(0.08f, 0.022f));
        btnVolver.alTocar.AddListener(CerrarVisor);
        btnVolver.gameObject.SetActive(false);
        btnVolver.etiqueta.gameObject.SetActive(false);
    }

    BotonTocable Boton(Transform padre, string texto, Vector3 pos, Vector2 tam)
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Boton_" + texto;
        cubo.transform.SetParent(padre, false);
        cubo.transform.localPosition = pos;
        cubo.transform.localScale = new Vector3(tam.x, tam.y, 0.006f);
        Pintar(cubo.GetComponent<Renderer>(), materialBoton);
        var b = cubo.AddComponent<BotonTocable>();
        b.materialNormal = materialBoton;
        b.materialMarcado = materialBotonMarcado;
        b.etiqueta = Texto(padre, texto, pos + new Vector3(0f, 0f, -0.0036f), new Vector2(tam.x * 0.92f, Mathf.Min(tam.y * 0.8f, 0.018f)), 0.2f);
        b.etiqueta.fontStyle = FontStyles.Bold;
        return b;
    }

    static TMP_Text Texto(Transform padre, string texto, Vector3 pos, Vector2 tam, float maximo)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        Idioma.Poner(t, texto);
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = maximo;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.black;
        t.rectTransform.sizeDelta = tam;
        return t;
    }

    static void Pintar(Renderer r, Material m)
    {
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    void OnDestroy()
    {
        LiberarMiniaturas();
        foreach (var f in fichas)
            if (f != null && f.material != null)
                Destroy(f.material);
        if (materialVisor != null)
            Destroy(materialVisor);
        if (texturaVisor != null)
            Destroy(texturaVisor);
    }
}

using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

// "Imagen +": buscador de imágenes de TODO el Quest (Descargas, Cámara, capturas, WhatsApp, Facebook...)
// y de las que ya trajiste a la app.
//  - Arriba: las carpetas (Todas, Descargas, Cámara, WhatsApp...) y la X para cerrar.
//  - Toca una miniatura para verla en grande. Tú decides:
//      Importar  = la copia a la app y la pone frente a ti (imagen de referencia, para calcar).
//      Fondo 360 = (solo fotos 360: el doble de anchas que de altas) la pone de fondo a tu alrededor.
//      Volver    = regresar a la lista.
//  - < y > cambian de página.
// La primera vez, el Quest te pide permiso para ver tus fotos.
public class BuscadorImagenes : MonoBehaviour
{
    public Referencias referencias;
    public Escenario escenario;
    public Dibujo dibujo;
    public Material materialPanel, materialBoton, materialBotonMarcado, materialImagen;

    const int Columnas = 4, Filas = 3, PorPagina = Columnas * Filas;
    const int MaxFiltros = 6;          // "Todas" + 5 carpetas
    const int TamMiniatura = 256, TamGrande = 1600;
    const string CarpetaApp = "En la app";

    class Item
    {
        public long id = -1;     // en la galería del Quest (o -1 si es un archivo de la app)
        public string ruta;      // archivo de la app (si id = -1)
        public string nombre;
        public string carpeta;
        public long fecha;       // segundos
        public int ancho, alto;  // 0 si no se sabe
    }

    class Ficha
    {
        public BotonTocable boton;
        public Renderer vista;
        public Material material;
        public TMP_Text texto;
        public Texture2D textura;
        public int item = -1;
        public bool intentada;
    }

    Transform raiz, cabeza;
    GameObject grilla;
    readonly List<Item> todos = new List<Item>();
    readonly List<Item> lista = new List<Item>();
    readonly List<string> carpetas = new List<string>(); // los filtros ("" = Todas)
    readonly BotonTocable[] botonesFiltro = new BotonTocable[MaxFiltros];
    readonly Ficha[] fichas = new Ficha[PorPagina];
    BotonTocable btnAnterior, btnSiguiente, btnImportar, btnFondo360, btnVolver;
    TMP_Text textoPagina, textoEstado, textoAyuda;
    Renderer visor;
    Material materialVisor;
    Texture2D texturaVisor;
    int filtro, pagina, elegido = -1;
    volatile bool permisoRespondio;
    bool esperandoPermiso;

    public bool Abierto => raiz != null && raiz.gameObject.activeSelf;
    bool VisorAbierto => visor != null && visor.gameObject.activeSelf;

    // Lo abre el botón "Imagen +".
    public static void Abrir(Referencias refs, Transform cabeza)
    {
        if (refs == null)
            return;
        var b = refs.GetComponent<BuscadorImagenes>();
        if (b == null)
            b = refs.gameObject.AddComponent<BuscadorImagenes>();
        b.referencias = refs;
        b.AbrirPanel(cabeza);
    }

    void BuscarPiezas()
    {
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (nav != null)
        {
            if (materialPanel == null) materialPanel = nav.materialPanel;
            if (materialBoton == null) materialBoton = nav.materialBoton;
            if (materialBotonMarcado == null) materialBotonMarcado = nav.materialBotonMarcado;
            if (materialImagen == null) materialImagen = nav.materialImagen;
        }
        if (materialImagen == null && referencias != null)
            materialImagen = referencias.materialImagen;
        if (dibujo == null && referencias != null)
            dibujo = referencias.dibujo;
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
        if (escenario == null)
            escenario = FindFirstObjectByType<Escenario>();
    }

    void AbrirPanel(Transform cab)
    {
        BuscarPiezas();
        cabeza = cab;
        if (raiz == null)
            Armar();
        raiz.gameObject.SetActive(true);
        if (cab != null)
        {
            Vector3 adelante = cab.forward;
            adelante.y = 0f;
            adelante = adelante.sqrMagnitude > 1e-4f ? adelante.normalized : Vector3.forward;
            Vector3 pos = cab.position + adelante * 0.55f - Vector3.up * 0.06f;
            raiz.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cab.position, Vector3.up));
        }
        CerrarVisor();
        filtro = 0;
        pagina = 0;
        elegido = -1;
        if (!TienePermiso())
            PedirPermiso();
        Listar();
        Mostrar();
        Mensaje(esperandoPermiso ? "Permite que JCartoons vea tus fotos (ventana del Quest)" : "Toca una imagen para verla en grande");
    }

    public void Cerrar()
    {
        CerrarVisor();
        LiberarMiniaturas();
        if (raiz != null)
            raiz.gameObject.SetActive(false);
#if UNITY_ANDROID && !UNITY_EDITOR
        SoltarJava();
#endif
    }

    void Update()
    {
        if (!Abierto)
            return;
        // El Quest respondió a la ventana del permiso: volvemos a buscar.
        if (permisoRespondio)
        {
            permisoRespondio = false;
            esperandoPermiso = false;
            Listar();
            Mostrar();
            if (!TienePermiso())
                Mensaje("Sin permiso solo ves las imágenes de la app. Para dárselo: Ajustes del Quest > Apps > JCartoons > Permisos");
        }
        // Las miniaturas se cargan de a una por cuadro (así no se traba la vista).
        if (!VisorAbierto)
            CargarUnaMiniatura();
    }

    void Mensaje(string texto)
    {
        if (dibujo != null)
            dibujo.Mensaje(texto);
    }

    // ---------- Permiso para ver las fotos del Quest ----------

    bool TienePermiso()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission("android.permission.READ_MEDIA_IMAGES")
            || Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead)
            || Permission.HasUserAuthorizedPermission("android.permission.READ_MEDIA_VISUAL_USER_SELECTED");
#else
        return true;
#endif
    }

    void PedirPermiso()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (esperandoPermiso)
            return;
        esperandoPermiso = true;
        var respuesta = new PermissionCallbacks();
        // Estas respuestas pueden llegar desde otro hilo: solo se marca, y se atiende en Update.
        respuesta.PermissionGranted += _ => permisoRespondio = true;
        respuesta.PermissionDenied += _ => permisoRespondio = true;
        respuesta.PermissionDeniedAndDontAskAgain += _ => permisoRespondio = true;
        try
        {
            Permission.RequestUserPermissions(new[] { "android.permission.READ_MEDIA_IMAGES", Permission.ExternalStorageRead }, respuesta);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo pedir el permiso de fotos: " + e.Message);
            esperandoPermiso = false;
        }
#endif
    }

    // ---------- La galería del Quest (plugin TrazoImagenes.java) ----------

#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaObject actividad;
    AndroidJavaClass java;

    bool Java()
    {
        if (java != null)
            return true;
        try
        {
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                actividad = unity.GetStatic<AndroidJavaObject>("currentActivity");
            java = new AndroidJavaClass("com.trazovr.TrazoImagenes");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo abrir la galería del Quest: " + e.Message);
            SoltarJava();
            return false;
        }
    }

    void SoltarJava()
    {
        if (java != null)
            java.Dispose();
        if (actividad != null)
            actividad.Dispose();
        java = null;
        actividad = null;
    }
#endif

    string ListarGaleria()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Java())
            return "";
        try
        {
            return java.CallStatic<string>("listar", actividad, 3000) ?? "";
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo leer la galería del Quest: " + e.Message);
        }
#endif
        return "";
    }

    // Guarda en "destino" una versión de la imagen de "tam" pixeles como máximo (jpg).
    bool MiniaturaGaleria(long id, int tam, string destino)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Java())
            return false;
        try
        {
            return java.CallStatic<bool>("miniatura", actividad, id, tam, destino);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo leer la miniatura: " + e.Message);
        }
#endif
        return false;
    }

    // Copia la imagen completa de la galería a "destino".
    bool CopiarGaleria(long id, string destino)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Java())
            return false;
        try
        {
            return java.CallStatic<bool>("copiar", actividad, id, destino);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo copiar la imagen: " + e.Message);
        }
#endif
        return false;
    }

    // ---------- La lista ----------

    static readonly System.DateTime Epoca = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

    void Listar()
    {
        todos.Clear();
        // 1) Las de la galería del Quest: id \t nombre \t carpeta \t fecha \t ancho \t alto
        foreach (var linea in ListarGaleria().Split('\n'))
        {
            var p = linea.Split('\t');
            if (p.Length < 6)
                continue;
            long id, fecha;
            int ancho, alto;
            if (!long.TryParse(p[0], out id))
                continue;
            if (!long.TryParse(p[3], out fecha))
                fecha = 0;
            if (!int.TryParse(p[4], out ancho))
                ancho = 0;
            if (!int.TryParse(p[5], out alto))
                alto = 0;
            todos.Add(new Item
            {
                id = id,
                nombre = p[1],
                carpeta = string.IsNullOrEmpty(p[2]) ? "Otras" : p[2],
                fecha = fecha,
                ancho = ancho,
                alto = alto
            });
        }
        // 2) Las que ya están en la app (Dibujos/Imagenes).
        if (referencias != null)
        {
            try
            {
                string carpeta = referencias.CarpetaImagenes;
                if (Directory.Exists(carpeta))
                    foreach (var f in Directory.GetFiles(carpeta))
                    {
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                            continue;
                        todos.Add(new Item
                        {
                            ruta = f,
                            nombre = Path.GetFileName(f),
                            carpeta = CarpetaApp,
                            fecha = (long)(File.GetLastWriteTimeUtc(f) - Epoca).TotalSeconds
                        });
                    }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TrazoVR: no se pudo leer la carpeta de imágenes: " + e.Message);
            }
        }
        todos.Sort((a, b) => b.fecha.CompareTo(a.fecha));

        // Los filtros: Todas + las carpetas con más imágenes (y "En la app" si hay).
        var cuenta = new Dictionary<string, int>();
        foreach (var it in todos)
        {
            int n;
            cuenta.TryGetValue(it.carpeta, out n);
            cuenta[it.carpeta] = n + 1;
        }
        var nombres = new List<string>(cuenta.Keys);
        nombres.Remove(CarpetaApp);
        nombres.Sort((a, b) => cuenta[b].CompareTo(cuenta[a]));
        carpetas.Clear();
        carpetas.Add("");
        int lugares = MaxFiltros - 1 - (cuenta.ContainsKey(CarpetaApp) ? 1 : 0);
        for (int i = 0; i < nombres.Count && i < lugares; i++)
            carpetas.Add(nombres[i]);
        if (cuenta.ContainsKey(CarpetaApp))
            carpetas.Add(CarpetaApp);
        filtro = Mathf.Clamp(filtro, 0, carpetas.Count - 1);
        Filtrar();
    }

    void Filtrar()
    {
        lista.Clear();
        string c = carpetas.Count > 0 ? carpetas[filtro] : "";
        foreach (var it in todos)
            if (c.Length == 0 || it.carpeta == c)
                lista.Add(it);
    }

    // El nombre corto de cada carpeta (las conocidas, en tu idioma).
    static string NombreCarpeta(string c)
    {
        switch (c)
        {
            case "": return "Todas";
            case "Download": return "Descargas";
            case "Camera": return "Cámara";
            case "Screenshots": return "Capturas";
            case "WhatsApp Images": return "WhatsApp";
            case "Pictures": return "Imágenes";
            default: return c.Length > 14 ? c.Substring(0, 13) + "…" : c;
        }
    }

    int Paginas => Mathf.Max(1, (lista.Count + PorPagina - 1) / PorPagina);

    void Mostrar()
    {
        LiberarMiniaturas();
        pagina = Mathf.Clamp(pagina, 0, Paginas - 1);
        for (int i = 0; i < PorPagina; i++)
        {
            var f = fichas[i];
            int k = pagina * PorPagina + i;
            bool hay = k < lista.Count;
            f.item = hay ? k : -1;
            f.intentada = false;
            f.boton.gameObject.SetActive(hay);
            f.vista.gameObject.SetActive(false);
            f.texto.gameObject.SetActive(hay);
            if (!hay)
                continue;
            Idioma.Poner(f.texto, NombreCarpeta(lista[k].carpeta));
            f.boton.Marcar(k == elegido);
        }
        for (int i = 0; i < botonesFiltro.Length; i++)
        {
            bool hay = i < carpetas.Count;
            botonesFiltro[i].gameObject.SetActive(hay);
            botonesFiltro[i].etiqueta.gameObject.SetActive(hay);
            if (!hay)
                continue;
            botonesFiltro[i].PonerTexto(NombreCarpeta(carpetas[i]));
            botonesFiltro[i].Marcar(i == filtro);
        }
        Idioma.Poner(textoPagina, lista.Count == 0 ? "Vacío" : (pagina + 1) + " / " + Paginas);
        Idioma.Poner(textoEstado, lista.Count + " imágenes");
        bool flechas = !VisorAbierto;
        btnAnterior.gameObject.SetActive(flechas);
        btnSiguiente.gameObject.SetActive(flechas);
        textoPagina.gameObject.SetActive(flechas);
    }

    void CargarUnaMiniatura()
    {
        for (int i = 0; i < PorPagina; i++)
        {
            var f = fichas[i];
            if (f.item < 0 || f.intentada)
                continue;
            f.intentada = true;
            f.textura = CargarImagen(lista[f.item], TamMiniatura);
            if (f.textura != null)
            {
                PonerTextura(f.material, f.textura);
                f.vista.gameObject.SetActive(true);
                float aspecto = f.textura.width / (float)Mathf.Max(1, f.textura.height);
                f.vista.transform.localScale = aspecto >= 0.125f / 0.072f ? new Vector3(0.125f, 0.125f / aspecto, 1f)
                                                                          : new Vector3(0.072f * aspecto, 0.072f, 1f);
            }
            return; // una por cuadro
        }
    }

    // La imagen en pequeño (miniatura) o en grande (para verla).
    Texture2D CargarImagen(Item it, int tam)
    {
        if (it.id >= 0)
        {
            string ruta = Path.Combine(Application.temporaryCachePath, "img_" + tam + "_" + it.id + ".jpg");
            if (!File.Exists(ruta) && !MiniaturaGaleria(it.id, tam, ruta))
                return null;
            return Leer(ruta);
        }
        var grande = Leer(it.ruta);
        if (grande == null || grande.width <= tam)
            return grande;
        var chica = Reducir(grande, tam);
        Destroy(grande);
        return chica;
    }

    static Texture2D Leer(string ruta)
    {
        try
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (t.LoadImage(File.ReadAllBytes(ruta)))
            {
                t.wrapMode = TextureWrapMode.Clamp;
                return t;
            }
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
        chica.wrapMode = TextureWrapMode.Clamp;
        return chica;
    }

    static void PonerTextura(Material m, Texture t)
    {
        if (m == null)
            return;
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

    void CambiarFiltro(int f)
    {
        if (f < 0 || f >= carpetas.Count)
            return;
        CerrarVisor();
        filtro = f;
        pagina = 0;
        elegido = -1;
        Filtrar();
        Mostrar();
    }

    void CambiarPagina(int paso)
    {
        pagina = Mathf.Clamp(pagina + paso, 0, Paginas - 1);
        Mostrar();
    }

    // Toca una miniatura: se ve en grande y tú decides qué hacer.
    void Elegir(int i)
    {
        int k = pagina * PorPagina + i;
        if (k >= lista.Count)
            return;
        elegido = k;
        var it = lista[k];
        CerrarVisor();
        texturaVisor = CargarImagen(it, TamGrande);
        if (texturaVisor == null)
        {
            Mensaje("No se pudo abrir esa imagen");
            return;
        }
        PonerTextura(materialVisor, texturaVisor);
        float aspecto = texturaVisor.width / (float)Mathf.Max(1, texturaVisor.height);
        grilla.SetActive(false);
        visor.gameObject.SetActive(true);
        float ancho = 0.56f, alto = 0.32f;
        if (aspecto > ancho / alto)
            alto = ancho / aspecto;
        else
            ancho = alto * aspecto;
        visor.transform.localScale = new Vector3(ancho, alto, 1f);
        float real = it.ancho > 0 && it.alto > 0 ? it.ancho / (float)it.alto : aspecto;
        bool es360 = real > 1.9f && real < 2.1f;
        Ver(btnImportar, true);
        Ver(btnFondo360, es360);
        Ver(btnVolver, true);
        Idioma.Poner(textoAyuda, es360 ? "¿Importar como imagen, o usarla de fondo 360 a tu alrededor?" : "¿Importar esta imagen? (para calcar o de referencia)");
        textoEstado.text = it.nombre;
        btnAnterior.gameObject.SetActive(false);
        btnSiguiente.gameObject.SetActive(false);
        textoPagina.gameObject.SetActive(false);
    }

    void CerrarVisor()
    {
        if (texturaVisor != null)
        {
            Destroy(texturaVisor);
            texturaVisor = null;
        }
        if (visor != null)
            visor.gameObject.SetActive(false);
        if (grilla != null)
            grilla.SetActive(true);
        Ver(btnImportar, false);
        Ver(btnFondo360, false);
        Ver(btnVolver, false);
        if (textoAyuda != null)
            Idioma.Poner(textoAyuda, "Toca una imagen para verla en grande");
    }

    void Volver()
    {
        CerrarVisor();
        Mostrar();
    }

    static void Ver(BotonTocable b, bool ver)
    {
        if (b == null)
            return;
        b.gameObject.SetActive(ver);
        if (b.etiqueta != null)
            b.etiqueta.gameObject.SetActive(ver);
    }

    // Importar: la copia a la app (Dibujos/Imagenes) y la pone frente a ti.
    void Importar()
    {
        if (elegido < 0 || elegido >= lista.Count || referencias == null)
            return;
        var it = lista[elegido];
        string archivo;
        if (it.id < 0)
        {
            archivo = Path.GetFileName(it.ruta);
        }
        else
        {
            string carpeta = referencias.CarpetaImagenes;
            try
            {
                Directory.CreateDirectory(carpeta);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TrazoVR: no se pudo crear la carpeta de imágenes: " + e.Message);
            }
            // jpg y png se copian tal cual; otros formatos (webp, heic, gif...) se convierten a jpg.
            string ext = Path.GetExtension(it.nombre ?? "").ToLowerInvariant();
            bool directo = ext == ".jpg" || ext == ".jpeg" || ext == ".png";
            string baseNombre = Limpiar(Path.GetFileNameWithoutExtension(it.nombre ?? ""));
            if (baseNombre.Length == 0)
                baseNombre = "imagen_" + it.id;
            string extFinal = directo ? ext : ".jpg";
            archivo = baseNombre + extFinal;
            for (int n = 2; File.Exists(Path.Combine(carpeta, archivo)) && n < 1000; n++)
                archivo = baseNombre + " (" + n + ")" + extFinal;
            string destino = Path.Combine(carpeta, archivo);
            bool bien = directo ? CopiarGaleria(it.id, destino) : MiniaturaGaleria(it.id, 2048, destino);
            if (!bien)
            {
                Mensaje("No se pudo importar esa imagen");
                return;
            }
        }
        Cerrar();
        referencias.Poner(archivo, cabeza);
    }

    // Fondo 360: la copia como "Mi foto 360" y la pone de fondo.
    void UsarDeFondo()
    {
        if (elegido < 0 || elegido >= lista.Count)
            return;
        if (escenario == null || escenario.materialFondo360 == null)
        {
            Mensaje("Falta el fondo 360: en Unity, vuelve a tocar TrazoVR > ★ Armar escena");
            return;
        }
        var it = lista[elegido];
        string destino = Escenario.RutaFoto360;
        bool bien = false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destino));
            if (it.id >= 0)
            {
                string ext = Path.GetExtension(it.nombre ?? "").ToLowerInvariant();
                bool directo = ext == ".jpg" || ext == ".jpeg" || ext == ".png";
                bien = directo ? CopiarGaleria(it.id, destino) : MiniaturaGaleria(it.id, 4096, destino);
            }
            else
            {
                File.Copy(it.ruta, destino, true);
                bien = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo copiar la foto 360: " + e.Message);
        }
        if (!bien)
        {
            Mensaje("No se pudo usar esa foto de fondo");
            return;
        }
        Cerrar();
        escenario.UsarFoto360();
        Mensaje("Fondo: " + Escenario.Nombres[escenario.modo]);
    }

    static string Limpiar(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in s)
            sb.Append(char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' ? c : '_');
        string r = sb.ToString().Trim();
        return r.Length > 60 ? r.Substring(0, 60) : r;
    }

    // ---------- Armar el panel (una sola vez) ----------

    void Armar()
    {
        raiz = new GameObject("BuscadorImagenes").transform;

        var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "Fondo";
        Destroy(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(raiz, false);
        fondo.transform.localPosition = new Vector3(0f, 0f, 0.006f);
        fondo.transform.localScale = new Vector3(0.64f, 0.48f, 1f);
        Pintar(fondo.GetComponent<Renderer>(), materialPanel);

        Texto(raiz, "Imágenes del Quest", new Vector3(-0.235f, 0.205f, -0.002f), new Vector2(0.15f, 0.02f), 0.2f);
        for (int i = 0; i < MaxFiltros; i++)
        {
            int f = i;
            botonesFiltro[i] = Boton(raiz, "", new Vector3(-0.11f + i * 0.07f, 0.205f, 0f), new Vector2(0.066f, 0.022f));
            botonesFiltro[i].alTocar.AddListener(() => CambiarFiltro(f));
        }
        var btnCerrar = Boton(raiz, "X", new Vector3(0.295f, 0.205f, 0f), new Vector2(0.024f, 0.022f));
        btnCerrar.alTocar.AddListener(Cerrar);
        textoAyuda = Texto(raiz, "Toca una imagen para verla en grande", new Vector3(0f, 0.172f, -0.002f), new Vector2(0.5f, 0.018f), 0.12f);

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
            vista.transform.localPosition = new Vector3(x, y + 0.008f, -0.005f);
            f.vista = vista.GetComponent<Renderer>();
            f.material = materialImagen != null ? new Material(materialImagen) : null;
            Pintar(f.vista, f.material);
            f.texto = Texto(grilla.transform, "", new Vector3(x, y - 0.042f, -0.005f), new Vector2(0.13f, 0.014f), 0.1f);
            fichas[i] = f;
        }

        var goVisor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        goVisor.name = "Visor";
        Destroy(goVisor.GetComponent<Collider>());
        goVisor.transform.SetParent(raiz, false);
        goVisor.transform.localPosition = new Vector3(0f, 0f, -0.006f);
        visor = goVisor.GetComponent<Renderer>();
        materialVisor = materialImagen != null ? new Material(materialImagen) : null;
        Pintar(visor, materialVisor);
        goVisor.SetActive(false);

        btnAnterior = Boton(raiz, "<", new Vector3(-0.28f, -0.205f, 0f), new Vector2(0.03f, 0.022f));
        btnAnterior.alTocar.AddListener(() => CambiarPagina(-1));
        textoPagina = Texto(raiz, "1 / 1", new Vector3(-0.235f, -0.205f, -0.002f), new Vector2(0.05f, 0.018f), 0.15f);
        btnSiguiente = Boton(raiz, ">", new Vector3(-0.19f, -0.205f, 0f), new Vector2(0.03f, 0.022f));
        btnSiguiente.alTocar.AddListener(() => CambiarPagina(1));
        textoEstado = Texto(raiz, "", new Vector3(-0.045f, -0.205f, -0.002f), new Vector2(0.17f, 0.018f), 0.12f);
        btnVolver = Boton(raiz, "< Volver", new Vector3(-0.245f, -0.205f, 0f), new Vector2(0.09f, 0.024f));
        btnVolver.alTocar.AddListener(Volver);
        btnImportar = Boton(raiz, "Importar", new Vector3(0.1f, -0.205f, 0f), new Vector2(0.1f, 0.024f));
        btnImportar.alTocar.AddListener(Importar);
        btnFondo360 = Boton(raiz, "Fondo 360", new Vector3(0.235f, -0.205f, 0f), new Vector2(0.11f, 0.024f));
        btnFondo360.alTocar.AddListener(UsarDeFondo);
        Ver(btnVolver, false);
        Ver(btnImportar, false);
        Ver(btnFondo360, false);
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
        if (raiz != null)
            Destroy(raiz.gameObject);
#if UNITY_ANDROID && !UNITY_EDITOR
        SoltarJava();
#endif
    }
}

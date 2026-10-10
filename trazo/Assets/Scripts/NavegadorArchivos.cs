using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

// "Mis archivos": una sola galería, con pocos botones.
//  - Arriba, TU DIBUJO ACTUAL: su miniatura y su nombre (● = tiene cambios sin guardar; toca el nombre para
//    cambiarlo), Guardar, Compartir (abre: Foto, SVG, Video anim, Video proceso y su velocidad) y Grabar
//    (graba cómo dibujas; mientras graba muestra el tiempo y aparece Pausa). ES/EN = idioma. X = cerrar.
//  - Debajo: "Mostrar: Todo" (cambia entre Todo, Dibujos, Videos, Fotos y SVG), "+ Imagen" (trae imágenes del
//    Quest) e "Imágenes: ver / ocultas" (tus imágenes de referencia).
//  - La galería: toca una miniatura y sobre ella salen sus acciones (dibujo: Abrir, Duplicar, Borrar;
//    foto o video: Ver, Borrar). Borrar pide tocar otra vez. Desliza el dedo de lado sobre la galería (o < >)
//    para cambiar de página.
// Se abre con el botón "Archivos" del menú de la mano.
public class NavegadorArchivos : MonoBehaviour
{
    public Dibujo dibujo;
    public ControlManos control;
    public Material materialPanel;
    public Material materialBoton;
    public Material materialBotonMarcado;
    [Tooltip("Material sin luz para mostrar imágenes (las miniaturas)")]
    public Material materialImagen;

    static readonly string[] Filtros = { "Todo", "Dibujos", "Videos", "Fotos", "SVG" };
    const int Columnas = 4, Filas = 3, PorPagina = Columnas * Filas;
    const float ArribaGrilla = 0.048f, PasoX = 0.15f, PasoY = 0.104f;
    const string ClaveVisto = "jcartoons_archivos_visto";

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
        public Vector3 centro;
    }

    Transform raiz;
    readonly List<Archivo> archivos = new List<Archivo>();
    readonly Ficha[] fichas = new Ficha[PorPagina];
    // Tu dibujo actual
    BotonTocable btnNombre, btnGuardar, btnCompartir, btnGrabar, btnPausa, btnIdioma, btnCerrar;
    Renderer miniActual;
    Material materialMiniActual;
    Texture2D texturaMiniActual;
    // Compartir
    GameObject abanico;
    BotonTocable btnVelocidad;
    // Fila de la galería
    GameObject fila2;
    BotonTocable btnMostrar, btnVerImagenes, btnVolver;
    // Acciones sobre la miniatura elegida
    GameObject acciones;
    BotonTocable btnAccion1, btnAccion2, btnAccion3;
    int accionesEn = -1; // sobre qué miniatura están (al cambiar, se bloquean un momento: no se tocan sin querer)
    // Abajo
    BotonTocable btnAnterior, btnSiguiente;
    TMP_Text textoPagina, textoEstado;

    TouchScreenKeyboard teclado;
    GameObject grilla;
    Renderer visor;
    Material materialVisor;
    Texture2D texturaVisor;
    VideoPlayer video;
    GrabadorProceso grabador;
    ExportadorVideo exportador;
    Referencias referencias;
    int filtro, pagina, elegido = -1;
    float confirmarBorrarHasta = -1f;
    // Deslizar el dedo sobre la galería = cambiar de página
    bool deslizando, deslizoYa;
    float deslizaX, deslizaDesde;
    float proximoRefresco;

    public bool Abierto => raiz != null && raiz.gameObject.activeSelf;
    bool VisorAbierto => visor != null && visor.gameObject.activeSelf;

    void Start()
    {
        Idioma.alCambiar += () => { if (Abierto) Mostrar(); };
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (control == null) control = FindFirstObjectByType<ControlManos>();
        grabador = FindFirstObjectByType<GrabadorProceso>();
        exportador = FindFirstObjectByType<ExportadorVideo>();
        referencias = FindFirstObjectByType<Referencias>();
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
        AbrirAbanico(false);
        pagina = 0;
        elegido = -1;
        Listar();
        Mostrar();
        CargarMiniActual();
        if (PlayerPrefs.GetInt(ClaveVisto, 0) == 0)
        {
            PlayerPrefs.SetInt(ClaveVisto, 1);
            PlayerPrefs.Save();
            dibujo.Mensaje("Toca una miniatura para ver qué hacer con ella · desliza el dedo de lado para cambiar de página");
        }
        else
        {
            dibujo.Mensaje("Mis archivos");
        }
    }

    public void Cerrar()
    {
        CerrarVisor();
        LiberarMiniaturas();
        if (texturaMiniActual != null)
        {
            Destroy(texturaMiniActual);
            texturaMiniActual = null;
        }
        if (raiz != null)
            raiz.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!Abierto)
            return;
        // Teclado del visor (cambiar el nombre).
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
            RefrescarAcciones();
        }
        RevisarDeslizar();
        if (Time.time >= proximoRefresco)
        {
            proximoRefresco = Time.time + 0.2f;
            RefrescarArriba();
        }
    }

    // Tu dibujo actual, Grabar, idioma e imágenes de referencia.
    void RefrescarArriba()
    {
        string nombre = string.IsNullOrEmpty(dibujo.NombreArchivo) ? Idioma.T("(sin nombre)") : dibujo.NombreArchivo;
        if (btnNombre != null && btnNombre.etiqueta != null)
            btnNombre.etiqueta.text = nombre + (dibujo.HayCambios ? " <color=#D2560A>●</color>" : "");
        if (btnIdioma != null)
            btnIdioma.PonerTexto(Idioma.Ingles ? "EN" : "ES");
        bool grabando = grabador != null && grabador.Grabando;
        if (btnGrabar != null)
        {
            btnGrabar.PonerTexto(grabando ? "■ " + GrabadorProceso.Formato(grabador.TiempoGrabado) : "● Grabar");
            btnGrabar.Marcar(grabando);
        }
        if (btnPausa != null)
        {
            Mostrar(btnPausa, grabando);
            if (grabando)
            {
                btnPausa.PonerTexto(grabador.Pausado ? "Seguir" : "Pausa");
                btnPausa.Marcar(grabador.Pausado);
            }
        }
        if (btnVelocidad != null && exportador != null)
            btnVelocidad.PonerTexto("Vel x" + exportador.velocidad);
        if (btnVerImagenes != null && referencias != null)
        {
            btnVerImagenes.PonerTexto(referencias.Ocultas ? "Imágenes: ocultas" : "Imágenes: ver");
            btnVerImagenes.Marcar(referencias.Ocultas);
        }
    }

    static void Mostrar(BotonTocable b, bool ver)
    {
        if (b == null || b.gameObject.activeSelf == ver)
            return;
        b.gameObject.SetActive(ver);
        if (b.etiqueta != null)
            b.etiqueta.gameObject.SetActive(ver);
    }

    // Deslizar el dedo de lado sobre la galería: página siguiente (hacia la izquierda) o anterior.
    void RevisarDeslizar()
    {
        if (control == null || VisorAbierto || !control.Der.valida)
        {
            deslizando = false;
            return;
        }
        Vector3 l = raiz.InverseTransformPoint(control.Der.indice);
        bool sobreGrilla = Mathf.Abs(l.x) < 0.31f && l.y < ArribaGrilla + 0.05f && l.y > ArribaGrilla - 2f * PasoY - 0.05f
                           && l.z > -0.045f && l.z < 0.02f;
        if (!sobreGrilla)
        {
            deslizando = false;
            deslizoYa = false;
            return;
        }
        if (!deslizando)
        {
            deslizando = true;
            deslizaX = l.x;
            deslizaDesde = Time.time;
            return;
        }
        if (deslizoYa)
            return;
        float dx = l.x - deslizaX;
        if (Time.time - deslizaDesde > 0.8f)
        {
            // Muy lento: no es un deslizamiento (vuelve a medir desde aquí).
            deslizaX = l.x;
            deslizaDesde = Time.time;
            return;
        }
        if (Mathf.Abs(dx) > 0.08f)
        {
            deslizoYa = true; // hasta que el dedo salga de la galería
            CambiarPagina(dx < 0f ? 1 : -1);
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
                                                                : new Vector3(0.066f * aspecto, 0.066f, 1f);
            }
            else
            {
                f.grande.text = Idioma.T(a.tipo);
                f.grande.gameObject.SetActive(true);
            }
            f.boton.Marcar(k == elegido);
        }
        if (btnMostrar != null)
            btnMostrar.PonerTexto("Mostrar: " + Filtros[filtro]);
        textoPagina.text = archivos.Count == 0 ? Idioma.T("Vacío") : (pagina + 1) + " / " + Paginas;
        bool variasPaginas = Paginas > 1;
        Mostrar(btnAnterior, variasPaginas);
        Mostrar(btnSiguiente, variasPaginas);
        RefrescarAcciones();
    }

    // Las acciones salen SOBRE la miniatura elegida.
    void RefrescarAcciones()
    {
        bool hay = elegido >= 0 && elegido < archivos.Count && elegido / PorPagina == pagina && !VisorAbierto;
        if (acciones != null && acciones.activeSelf != hay)
            acciones.SetActive(hay);
        if (hay && acciones != null && accionesEn != elegido)
        {
            // Se movieron a otra miniatura: se vuelven a encender, así no se presionan solas bajo tu dedo.
            acciones.SetActive(false);
            acciones.SetActive(true);
        }
        accionesEn = hay ? elegido : -1;
        if (!hay)
        {
            textoEstado.text = archivos.Count + Idioma.T(" archivos");
            return;
        }
        var a = archivos[elegido];
        var f = fichas[elegido % PorPagina];
        bool esDibujo = a.tipo == "Dibujo";
        bool confirmar = confirmarBorrarHasta > 0f;
        btnAccion1.PonerTexto(esDibujo ? "Abrir" : a.tipo == "SVG" ? "Dónde" : "Ver");
        Mostrar(btnAccion2, esDibujo);
        btnAccion3.PonerTexto(confirmar ? "¿Seguro?" : "Borrar");
        btnAccion3.Marcar(confirmar);
        // En fila sobre la parte de abajo de la miniatura (2 o 3 botones, centrados).
        float y = f.centro.y - 0.018f;
        if (esDibujo)
        {
            Colocar(btnAccion1, new Vector3(f.centro.x - 0.045f, y, -0.012f));
            Colocar(btnAccion2, new Vector3(f.centro.x, y, -0.012f));
            Colocar(btnAccion3, new Vector3(f.centro.x + 0.045f, y, -0.012f));
        }
        else
        {
            Colocar(btnAccion1, new Vector3(f.centro.x - 0.025f, y, -0.012f));
            Colocar(btnAccion3, new Vector3(f.centro.x + 0.025f, y, -0.012f));
        }
        textoEstado.text = Path.GetFileName(a.ruta);
    }

    static void Colocar(BotonTocable b, Vector3 pos)
    {
        b.transform.localPosition = pos;
        if (b.etiqueta != null)
            b.etiqueta.transform.localPosition = pos + new Vector3(0f, 0f, -0.0036f);
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

    // La miniatura de tu dibujo actual (la de su último guardado).
    void CargarMiniActual()
    {
        if (miniActual == null)
            return;
        if (texturaMiniActual != null)
        {
            Destroy(texturaMiniActual);
            texturaMiniActual = null;
        }
        if (!string.IsNullOrEmpty(dibujo.NombreArchivo))
        {
            string mini = dibujo.RutaMiniatura(Path.Combine(dibujo.CarpetaArchivosDibujo, dibujo.NombreArchivo + Dibujo.Extension));
            if (File.Exists(mini))
                texturaMiniActual = Leer(mini);
        }
        if (texturaMiniActual != null && materialMiniActual != null)
        {
            PonerTextura(materialMiniActual, texturaMiniActual);
            miniActual.gameObject.SetActive(true);
        }
        else
        {
            miniActual.gameObject.SetActive(false);
        }
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

    void Elegir(int i)
    {
        int k = pagina * PorPagina + i;
        if (k >= archivos.Count || k == elegido)
            return;
        elegido = k;
        confirmarBorrarHasta = -1f;
        for (int j = 0; j < PorPagina; j++)
            fichas[j].boton.Marcar(pagina * PorPagina + j == elegido);
        RefrescarAcciones();
    }

    // Abrir (dibujo), Ver (foto o video) o Dónde (SVG).
    void Accion1()
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
                dibujo.Mensaje("Los SVG se abren en el PC (Inkscape, Illustrator). Están en la app Archivos del Quest → Descargas → JCartoons → " + Path.GetFileName(a.ruta));
                break;
        }
    }

    // Duplicar un dibujo: una copia del archivo con otro nombre ("… (copia)").
    void Duplicar()
    {
        if (elegido < 0 || elegido >= archivos.Count || archivos[elegido].tipo != "Dibujo")
            return;
        var a = archivos[elegido];
        string ext = Path.GetExtension(a.ruta);
        string baseNombre = a.ruta == dibujo.RutaGuardadoAnterior ? "Guardado anterior" : Path.GetFileNameWithoutExtension(a.ruta);
        string carpeta = dibujo.CarpetaArchivosDibujo;
        string nombre = baseNombre + " (copia)";
        for (int k = 2; File.Exists(Path.Combine(carpeta, nombre + ext)); k++)
            nombre = baseNombre + " (copia " + k + ")";
        try
        {
            Directory.CreateDirectory(carpeta);
            string nueva = Path.Combine(carpeta, nombre + ext);
            File.Copy(a.ruta, nueva);
            string mini = dibujo.RutaMiniatura(a.ruta);
            if (File.Exists(mini))
                File.Copy(mini, dibujo.RutaMiniatura(nueva), true);
            dibujo.Mensaje("Copia: " + nombre);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo duplicar: " + e.Message);
            dibujo.Mensaje("No se pudo duplicar");
            return;
        }
        elegido = -1;
        Listar();
        Mostrar();
    }

    void Borrar()
    {
        if (elegido < 0 || elegido >= archivos.Count)
            return;
        if (confirmarBorrarHasta < 0f)
        {
            confirmarBorrarHasta = Time.time + 3f;
            RefrescarAcciones();
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

    void CambiarFiltro()
    {
        filtro = (filtro + 1) % Filtros.Length;
        pagina = 0;
        elegido = -1;
        confirmarBorrarHasta = -1f;
        Listar();
        Mostrar();
    }

    void CambiarPagina(int paso)
    {
        int antes = pagina;
        pagina = Mathf.Clamp(pagina + paso, 0, Paginas - 1);
        if (pagina == antes)
            return;
        elegido = -1;
        confirmarBorrarHasta = -1f;
        Mostrar();
        if (control != null && control.Der.valida)
            control.SonidoBurbuja(control.Der.indice);
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

    void Guardar()
    {
        dibujo.Guardar();
        Listar();
        Mostrar();
        CargarMiniActual();
    }

    // ---------- Compartir ----------

    void AbrirAbanico(bool abrir)
    {
        if (abanico != null && abanico.activeSelf != abrir)
            abanico.SetActive(abrir);
        if (btnCompartir != null)
            btnCompartir.Marcar(abrir);
    }

    // Foto del dibujo: este panel se esconde un momento para no salir en ella.
    void TomarFoto()
    {
        bool estaba = raiz != null && raiz.gameObject.activeSelf;
        if (estaba)
            raiz.gameObject.SetActive(false);
        dibujo.TomarFoto();
        if (estaba)
            raiz.gameObject.SetActive(true);
        Listar();
        Mostrar();
    }

    void VideoProceso()
    {
        if (exportador == null)
            return;
        if (grabador == null || grabador.muestras.Count < 2)
        {
            dibujo.Mensaje("Primero graba cómo dibujas: toca ● Grabar (y después ■ para terminar)");
            return;
        }
        exportador.ExportarProceso();
    }

    // + Imagen: se cierra este panel y se abre el de las imágenes del Quest.
    void TraerImagen()
    {
        if (referencias == null)
            referencias = FindFirstObjectByType<Referencias>();
        if (referencias == null)
        {
            dibujo.Mensaje("No encuentro las imágenes de referencia (vuelve a tocar ★ Armar escena en Unity)");
            return;
        }
        Cerrar();
        BuscadorImagenes.Abrir(referencias, control != null ? control.Cabeza : null);
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
        fila2.SetActive(false);
        visor.gameObject.SetActive(true);
        float ancho = 0.56f, alto = 0.28f;
        if (aspecto > ancho / alto)
            alto = ancho / aspecto;
        else
            ancho = alto * aspecto;
        visor.transform.localScale = new Vector3(ancho, alto, 1f);
        Mostrar(btnVolver, true);
        RefrescarAcciones();
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
        if (fila2 != null)
            fila2.SetActive(true);
        Mostrar(btnVolver, false);
        if (acciones != null)
            RefrescarAcciones();
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
        fondo.transform.localPosition = new Vector3(0f, -0.005f, 0.006f);
        fondo.transform.localScale = new Vector3(0.64f, 0.48f, 1f);
        Pintar(fondo.GetComponent<Renderer>(), materialPanel);

        // ----- Arriba: tu dibujo actual -----
        var goMini = GameObject.CreatePrimitive(PrimitiveType.Quad);
        goMini.name = "MiniaturaActual";
        Destroy(goMini.GetComponent<Collider>());
        goMini.transform.SetParent(raiz, false);
        goMini.transform.localPosition = new Vector3(-0.29f, 0.2f, -0.002f);
        goMini.transform.localScale = new Vector3(0.042f, 0.03f, 1f);
        miniActual = goMini.GetComponent<Renderer>();
        materialMiniActual = materialImagen != null ? new Material(materialImagen) : null;
        Pintar(miniActual, materialMiniActual);
        goMini.SetActive(false);

        btnNombre = Boton(raiz, "", new Vector3(-0.19f, 0.2f, 0f), new Vector2(0.15f, 0.03f));
        btnNombre.alTocar.AddListener(AbrirTeclado);
        Texto(raiz, "toca el nombre para cambiarlo", new Vector3(-0.19f, 0.177f, -0.002f), new Vector2(0.15f, 0.01f), 0.08f).color = new Color(0.4f, 0.4f, 0.45f);
        btnGuardar = Boton(raiz, "Guardar", new Vector3(-0.06f, 0.2f, 0f), new Vector2(0.07f, 0.03f));
        btnGuardar.alTocar.AddListener(Guardar);
        btnCompartir = Boton(raiz, "Compartir ▼", new Vector3(0.03f, 0.2f, 0f), new Vector2(0.09f, 0.03f));
        btnCompartir.alTocar.AddListener(() => AbrirAbanico(abanico != null && !abanico.activeSelf));
        btnGrabar = Boton(raiz, "● Grabar", new Vector3(0.13f, 0.2f, 0f), new Vector2(0.09f, 0.03f));
        btnGrabar.alTocar.AddListener(() =>
        {
            if (grabador != null)
                grabador.Alternar();
            else
                dibujo.Mensaje("No encuentro el grabador (vuelve a tocar ★ Armar escena en Unity)");
        });
        btnPausa = Boton(raiz, "Pausa", new Vector3(0.205f, 0.2f, 0f), new Vector2(0.045f, 0.03f));
        btnPausa.alTocar.AddListener(() => { if (grabador != null) grabador.AlternarPausa(); });
        Mostrar(btnPausa, false);
        btnIdioma = Boton(raiz, "ES", new Vector3(0.252f, 0.2f, 0f), new Vector2(0.032f, 0.026f));
        btnIdioma.alTocar.AddListener(Idioma.Alternar);
        btnCerrar = Boton(raiz, "X", new Vector3(0.292f, 0.2f, 0f), new Vector2(0.026f, 0.026f));
        btnCerrar.alTocar.AddListener(Cerrar);

        // ----- Compartir (se abre bajo su botón) -----
        abanico = new GameObject("Compartir");
        abanico.transform.SetParent(raiz, false);
        var foto = Boton(abanico.transform, "Foto (PNG)", new Vector3(-0.075f, 0.157f, 0f), new Vector2(0.085f, 0.026f));
        foto.alTocar.AddListener(TomarFoto);
        var svg = Boton(abanico.transform, "SVG (2)", new Vector3(0.015f, 0.157f, 0f), new Vector2(0.085f, 0.026f));
        svg.alTocar.AddListener(() => { dibujo.ExportarSVG(); Listar(); Mostrar(); });
        var anim = Boton(abanico.transform, "Video anim", new Vector3(0.105f, 0.157f, 0f), new Vector2(0.085f, 0.026f));
        anim.alTocar.AddListener(() => { if (exportador != null) exportador.ExportarAnimacion(); });
        var proceso = Boton(abanico.transform, "Video proceso", new Vector3(0.195f, 0.157f, 0f), new Vector2(0.085f, 0.026f));
        proceso.alTocar.AddListener(VideoProceso);
        btnVelocidad = Boton(abanico.transform, "Vel x1", new Vector3(0.268f, 0.157f, 0f), new Vector2(0.05f, 0.026f));
        btnVelocidad.alTocar.AddListener(() => { if (exportador != null) exportador.CambiarVelocidad(); });
        abanico.SetActive(false);

        // ----- La galería: qué mostrar, traer imágenes y ver/ocultar las de referencia -----
        fila2 = new GameObject("FilaGaleria");
        fila2.transform.SetParent(raiz, false);
        btnMostrar = Boton(fila2.transform, "Mostrar: Todo", new Vector3(-0.235f, 0.118f, 0f), new Vector2(0.13f, 0.026f));
        btnMostrar.alTocar.AddListener(CambiarFiltro);
        var masImagen = Boton(fila2.transform, "+ Imagen", new Vector3(0.15f, 0.118f, 0f), new Vector2(0.08f, 0.026f));
        masImagen.alTocar.AddListener(TraerImagen);
        btnVerImagenes = Boton(fila2.transform, "Imágenes: ver", new Vector3(0.252f, 0.118f, 0f), new Vector2(0.1f, 0.026f));
        btnVerImagenes.alTocar.AddListener(() => { if (referencias != null) referencias.AlternarTodas(); });

        grilla = new GameObject("Grilla");
        grilla.transform.SetParent(raiz, false);
        for (int i = 0; i < PorPagina; i++)
        {
            int col = i % Columnas, fila = i / Columnas;
            float x = -0.225f + col * PasoX;
            float y = ArribaGrilla - fila * PasoY;
            var f = new Ficha { centro = new Vector3(x, y, 0f) };
            f.boton = Boton(grilla.transform, "", new Vector3(x, y, 0f), new Vector2(0.138f, 0.098f));
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
            f.texto = Texto(grilla.transform, "", new Vector3(x, y - 0.035f, -0.005f), new Vector2(0.13f, 0.024f), 0.1f);
            f.grande = Texto(grilla.transform, "", new Vector3(x, y + 0.012f, -0.005f), new Vector2(0.12f, 0.05f), 0.4f);
            fichas[i] = f;
        }

        // Las acciones de la miniatura elegida (se colocan sobre ella).
        acciones = new GameObject("Acciones");
        acciones.transform.SetParent(raiz, false);
        btnAccion1 = Boton(acciones.transform, "Abrir", Vector3.zero, new Vector2(0.042f, 0.022f));
        btnAccion1.alTocar.AddListener(Accion1);
        btnAccion2 = Boton(acciones.transform, "Duplicar", Vector3.zero, new Vector2(0.042f, 0.022f));
        btnAccion2.alTocar.AddListener(Duplicar);
        btnAccion3 = Boton(acciones.transform, "Borrar", Vector3.zero, new Vector2(0.042f, 0.022f));
        btnAccion3.alTocar.AddListener(Borrar);
        acciones.SetActive(false);

        var goVisor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        goVisor.name = "Visor";
        Destroy(goVisor.GetComponent<Collider>());
        goVisor.transform.SetParent(raiz, false);
        goVisor.transform.localPosition = new Vector3(0f, -0.06f, -0.006f);
        visor = goVisor.GetComponent<Renderer>();
        materialVisor = materialImagen != null ? new Material(materialImagen) : null;
        Pintar(visor, materialVisor);
        goVisor.SetActive(false);
        btnVolver = Boton(raiz, "Volver", new Vector3(-0.235f, 0.118f, 0f), new Vector2(0.08f, 0.026f));
        btnVolver.alTocar.AddListener(CerrarVisor);
        Mostrar(btnVolver, false);

        // ----- Abajo: página y estado -----
        textoEstado = Texto(raiz, "", new Vector3(-0.2f, -0.226f, -0.002f), new Vector2(0.2f, 0.016f), 0.11f);
        btnAnterior = Boton(raiz, "<", new Vector3(-0.045f, -0.226f, 0f), new Vector2(0.026f, 0.02f));
        btnAnterior.alTocar.AddListener(() => CambiarPagina(-1));
        textoPagina = Texto(raiz, "1 / 1", new Vector3(0f, -0.226f, -0.002f), new Vector2(0.05f, 0.016f), 0.13f);
        btnSiguiente = Boton(raiz, ">", new Vector3(0.045f, -0.226f, 0f), new Vector2(0.026f, 0.02f));
        btnSiguiente.alTocar.AddListener(() => CambiarPagina(1));
        Texto(raiz, "desliza el dedo de lado = otra página", new Vector3(0.2f, -0.226f, -0.002f), new Vector2(0.2f, 0.014f), 0.09f).color = new Color(0.45f, 0.45f, 0.5f);
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
        if (materialMiniActual != null)
            Destroy(materialMiniActual);
        if (texturaVisor != null)
            Destroy(texturaVisor);
        if (texturaMiniActual != null)
            Destroy(texturaMiniActual);
    }
}

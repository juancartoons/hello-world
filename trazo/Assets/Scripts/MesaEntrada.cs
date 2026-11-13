using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

// LA MESA DE ANIMACIÓN (v59): el "modo entrada". Al abrir la app (después del título) y al tocar Salir en el reloj,
// desaparece TODO lo de trabajar (reloj, paleta, capas, línea de tiempo, parlante, flechas...) y frente a ti queda
// solo una MESA DE ANIMACIÓN antigua: el tablero de madera inclinado, el DISCO con la luz por debajo, las dos reglas
// con pivotes, una HOJA con un dibujo de muestra, los lápices y, arriba, tus 7 DIBUJOS RECIENTES como hojas.
//  - Toca una hoja de arriba: ENTRAS a ese dibujo (la hoja crece hacia ti hasta que estás adentro).
//  - Dibuja en la hoja del disco con el ÍNDICE DERECHO (como un lápiz: más pegado = más oscuro). El lápiz va
//    "suave" (un poco arrastrado, más liso). Pellizca el borde del disco y gíralo, como en la mesa de verdad.
//  - "Hoja de tinta": pone una hoja encima (como papel de calcar) y lo que dibujas es TINTA.
//  - "✦ Entrar": entras a tu boceto: se crea un dibujo nuevo con tu lápiz en la capa 1 (lápiz, 2D) y tu tinta
//    en la capa 2 (líneas de verdad, editables). Ahí sigues entintando con todo lo de siempre.
//  - v61: la hoja 1 trae un DIBUJO DE MUESTRA al azar (Resources/BocetosMesa): bórralo con la GOMA o con "Hoja nueva".
//    Perforaciones ABAJO (como el papel de los estudios). El NÚMERO de la hoja, pequeño y a mano; tócalo para
//    marcarla como CLAVE (número en círculo; la hoja 1 ya viene marcada).
//  - "Hoja nueva": una hoja en blanco. Si sales sin entrar, tu boceto se queda en la mesa (también al cerrar la app).
//  - "Cerrar app" (abajo a la derecha, dos toques).
//  - v60: al tocar un reciente, su dibujo aparece DE GOLPE en la hoja del disco y te ZAMBULLES en ella (la mesa crece a
//    tu alrededor; la hoja se desvanece y tus líneas de verdad aparecen poco a poco).
//  - v60: ZOOM y ALTURA: pellizca con las dos manos (separar/juntar = tamaño; subir/bajar = altura). "Centrar mesa".
//  - v60: TRES LÁPICES (grafito, rojo, azul): toca uno. EL BLOC (izquierda): "+ Hoja" agrega otra hoja (la anterior
//    se ve debajo en papel cebolla), ◀ ▶ para ir y volver, "▶ Reproducir" (antes Hojear) las pasa seguidas (8 por segundo) como un
//    flip book. Al entrar, entra la hoja que está en el disco (las demás se quedan en el bloc).
public class MesaEntrada : MonoBehaviour
{
    static MesaEntrada instancia;
    // v63: Activa = la mesa de verdad (todo lo de trabajar escondido). Con "Ver en la mesa" la mesa solo acompaña a tu
    // archivo y todo funciona como siempre (entonces Activa es false).
    public static bool Activa => instancia != null && instancia.activa && !instancia.verArchivo;

    const int Recientes = 7;
    const int AnchoTex = 800, AltoTex = 600;
    const float AnchoHoja = 0.30f, AltoHoja = 0.225f;
    const float RadioDisco = 0.2f;
    const float Inclinacion = 40f;        // grados desde lo horizontal (como un tablero de dibujo)
    const float AlcanceLapiz = 0.012f;    // metros: el dedo dibuja a menos de esto del papel
    static readonly Color32 ColorPapel = new Color32(246, 242, 230, 255);
    static readonly Color ColorGrafito = new Color(0.22f, 0.22f, 0.25f);
    const float AlturaHoyos = 0.06f, SeparacionRanura = 0.28f;   // v61: perforaciones abajo (en la hoja, de 0 a 1)

    bool activa, armada, animando, arrancado, tutorialVisto;
    ControlManos control;
    Dibujo dibujo;
    Transform raiz, giratorio, hoja, hojaTinta;
    Texture2D texHoja, texTinta;
    Color32[] pxHoja, pxBase, pxTinta, pxPapel;
    Color32[] pxFondo, fondoGoma;   // v61: a lo que vuelve la goma (papel + número + papel cebolla)
    Color32 vacioTinta = new Color32(255, 255, 255, 38);
    byte[] muestraPx;
    int muestraCargada = -1;
    bool tocandoNumero;
    bool sucioHoja, sucioTinta, tintaPuesta;
    readonly List<BotonTocable> botonesRecientes = new List<BotonTocable>();
    readonly List<Renderer> minis = new List<Renderer>();
    readonly List<TextMeshPro> nombresRecientes = new List<TextMeshPro>();
    readonly List<string> rutasRecientes = new List<string>();
    readonly List<Texture2D> texturasMini = new List<Texture2D>();
    BotonTocable btnCerrarApp;
    // v64: los MODOS de la mesa: arriba el título ("Dibujando" o "Entintando") con sus herramientas, dentro de un
    // rectángulo dibujado sobre la mesa; abajo, fuera del rectángulo, el botón para pasar al otro modo.
    TextMeshPro tituloModo;
    BotonTocable btnModo;
    Transform frasco;
    float ultimoBorradoTinta = -10f;
    // v65: grosor de la tinta nueva y de cada nodo (gesto de grosor), y tamaño de la goma (puño + pellizco).
    float anchoPincelMesa = 1f, tamanoGomaMesa = 1f;
    bool grosorActivo, gomaDial;
    Linea lineaGrosor;
    int nodoGrosor;
    float alturaGrosor, grosorInicio, gomaAltura, gomaInicio;
    static bool sonidoMesaListo;
    TextMeshPro textoAyuda;
    float cerrarAppHasta;
    Material matImagen, matTransparente, matColor;

    // Lo que dibujaste (en la hoja: u, v de 0 a 1 y la presión), para pasarlo al dibujo al entrar.
    // v60: c = color del lápiz (0 grafito, 1 rojo, 2 azul). Varias HOJAS (el bloc): cada una su lápiz y su tinta.
    [System.Serializable]
    // v62: vector = tinta en líneas. v65: a = grosor de la línea (multiplica al de la mesa; 0 = 1); en las líneas
    // vector, la z de cada nodo es SU grosor (1 = normal), como el grosor de los nodos de adentro de los archivos.
    class Linea { public List<Vector3> p = new List<Vector3>(); public int c; public bool vector; public float a; }
    [System.Serializable]
    class Pagina
    {
        public List<Linea> lapiz = new List<Linea>();
        public List<Linea> tinta = new List<Linea>();
        public bool clave;   // v61: hoja clave (su número va en un círculo)
    }
    [System.Serializable]
    class Guardado
    {
        public bool antiguo = true;   // la primera hoja muestra un dibujo de muestra (v61)
        public int muestra = -1;      // cuál (de Resources/BocetosMesa)
        public bool clavesListas;     // v61: la hoja 1 empieza como clave
        public List<Linea> lapiz = new List<Linea>();   // (de la v59: una sola hoja; al cargar pasa a "paginas")
        public List<Linea> tinta = new List<Linea>();
        public List<Pagina> paginas = new List<Pagina>();
        public int actual;
    }
    Guardado boceto = new Guardado();
    Pagina Pag
    {
        get
        {
            Normalizar();
            return boceto.paginas[boceto.actual];
        }
    }

    void Normalizar()
    {
        if (boceto == null)
            boceto = new Guardado();
        if (boceto.paginas == null)
            boceto.paginas = new List<Pagina>();
        if (boceto.lapiz == null)
            boceto.lapiz = new List<Linea>();
        if (boceto.tinta == null)
            boceto.tinta = new List<Linea>();
        if (boceto.paginas.Count == 0)
        {
            var p = new Pagina();
            p.lapiz.AddRange(boceto.lapiz);
            p.tinta.AddRange(boceto.tinta);
            boceto.paginas.Add(p);
            boceto.lapiz.Clear();
            boceto.tinta.Clear();
        }
        foreach (var p in boceto.paginas)
        {
            if (p.lapiz == null) p.lapiz = new List<Linea>();
            if (p.tinta == null) p.tinta = new List<Linea>();
        }
        boceto.actual = Mathf.Clamp(boceto.actual, 0, boceto.paginas.Count - 1);
        if (!boceto.clavesListas)
        {
            boceto.paginas[0].clave = true;
            boceto.clavesListas = true;
        }
    }

    // v61: el 4.º es la GOMA (borra el lápiz, el dibujo de muestra y, con la hoja de tinta puesta, la tinta).
    static readonly Color[] ColoresLapiz = { new Color(0.22f, 0.22f, 0.25f), new Color(0.85f, 0.3f, 0.22f), new Color(0.35f, 0.55f, 0.95f),
                                             new Color(0.95f, 0.62f, 0.66f) };
    const int Goma = 3;
    int colorLapiz;

    // v63: LAS HERRAMIENTAS: 0 lápiz (de verdad), 1 lápiz rojo, 2 lápiz azul, 3 goma, 4 RAPIDÓGRAFO (tinta en líneas,
    // nítida), 5 PLUMILLA de tinta china (tinta pintada). La elegida se levanta y BRILLA.
    static readonly string[] NombresHerramienta =
        { "Lápiz", "Lápiz rojo", "Lápiz azul", "Goma", "Rapidógrafo: tinta en líneas (nítida)", "Plumilla: tinta pintada (a mano)" };
    readonly List<Transform> herramientas = new List<Transform>();
    readonly List<Transform> cuerposHerramienta = new List<Transform>();
    readonly List<Vector3> basesHerramienta = new List<Vector3>();
    int herramientaActual;
    Transform brilloHerramienta;

    // v63: el disco gira SOLO con la mano derecha pellizcando una de las REGLAS negras (brillan al acercarte).
    Material matReglas;
    float brilloReglas;
    // v63: pellizca la hoja y jálala rápido hacia afuera = la botas (se puede deshacer).
    bool agarrandoHoja;
    Vector3 agarrePrev, agarreInicio;
    float agarreVel;
    // v63: deshacer y rehacer en la mesa (los botones y la flecha de la mano, como siempre).
    readonly List<string> deshacerMesa = new List<string>(), rehacerMesa = new List<string>();
    // v63: la tinta en LÍNEAS se dibuja como una malla de verdad (nítida con cualquier zoom).
    Mesh mallaTinta, mallaNodos;
    bool mallaSucia, tintaRaster;
    // v63: "Ver en la mesa": lo que se esconde mientras la mesa acompaña a tu archivo.
    Transform tablero, orilla;
    readonly List<Transform> pivotes = new List<Transform>();
    readonly List<GameObject> escondidosVer = new List<GameObject>();

    // El bloc: hojear (ver las hojas seguidas, como un flip book).
    bool hojeando;
    float proximoCuadro;
    int cuadroHojear;
    readonly List<Color32[]> cuadrosHojear = new List<Color32[]>();
    TextMeshPro textoBloc;
    BotonTocable btnHojear;

    // Zoom y altura de la mesa (con las dos manos en pellizco). Se recuerdan.
    // v62: el zoom llega de 0,3 a 15 veces y la mesa se mueve con tus manos (a los lados, arriba, abajo...).
    const string ClaveEscala = "jcartoons_mesa_escala";
    const string ClaveDX = "jcartoons_mesa_dx", ClaveDY = "jcartoons_mesa_dy", ClaveDZ = "jcartoons_mesa_dz";
    const float EscalaMin = 0.3f, EscalaMax = 15f;
    float escalaMesa = 1f;
    Vector3 desplazaMesa;   // cuánto la moviste (en los ejes de la mesa)
    bool zoomeando;
    float zoomDistancia, zoomEscala;
    Vector3 zoomMedio, zoomPos;
    Transform flecha, puntaFlecha;   // v62: si la mesa queda fuera de tu vista, una flecha te dice dónde está

    // v62: el fondo: la mesa siempre en la realidad (passthrough); al entrar vuelve el tuyo (si no lo cambiaste aquí).
    Escenario escenario;
    int fondoAntes = -1;
    bool semiAntes, fondoMesaPuesto;

    // v62: papel cebolla del bloc (encendido / apagado), la tinta "pintada" escondida y los nodos de la tinta.
    const string ClaveCebolla = "jcartoons_mesa_cebolla", ClaveTintaPintada = "jcartoons_mesa_tinta_pintada";
    bool cebollaMesa = true, tintaPintada;
    BotonTocable btnCebolla, btnEntrar;
    float sostenTintaDesde = -1f;
    bool sostenTintaHecho, nodosVisibles;
    Linea lineaNodo;
    int nodoTomado = -1;
    Material matBurbuja, matHojaNormal, matVer;

    // v62: "Ver en la mesa": tu hoja del archivo abierto, sobre el disco (sin cerrar el archivo).
    bool verArchivo;
    Texture2D texVer;
    Vector3 baseMesa;   // dónde está la mesa sin la altura extra
    Quaternion giroMesa;
    Linea actual;
    bool actualTinta;

    // El filtro del lápiz ("Suave": un poco arrastrado, más liso).
    Vector3 filtrado, velocidad, crudoPrevio;
    bool tieneFiltro;

    // Girar el disco.
    int manoGiro = -1;   // 0 izquierda, 1 derecha
    float anguloAntes;

    void Start()
    {
        // Se mira AL ABRIR: si es la primera vez, va el tutorial (y al terminarlo te quedas trabajando).
        tutorialVisto = PlayerPrefs.GetInt("jcartoons_tutorial_visto", 0) == 1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia != null)
            return;
        instancia = new GameObject("MesaEntrada").AddComponent<MesaEntrada>();
    }

    // ---------- Entrar y salir del modo ----------

    // El botón Salir del reloj: guarda el dibujo y vuelve a la mesa.
    public static void Salir()
    {
        if (instancia != null)
            instancia.Mostrar();
    }

    // La voz ("entrar"): entra a tu boceto de la mesa.
    public static void EntrarVoz()
    {
        if (Activa && !instancia.animando)
            instancia.EntrarABoceto();
    }

    void Update()
    {
        if (control == null)
            control = ControlManos.Instancia;
        if (dibujo == null && control != null)
            dibujo = control.dibujo;
        if (control == null || dibujo == null || control.Cabeza == null)
            return;
        // Al abrir la app: después del título (y nunca la primera vez: ahí va el tutorial).
        if (!arrancado)
        {
            if (!Tutorial.ListoParaMesa)
                return;
            arrancado = true;
            if (tutorialVisto && !Tutorial.EnCurso)
                Mostrar();
            return;
        }
        if (!activa)
            return;
        if (verArchivo)
        {
            AlinearConArchivo();   // v63: la mesa sigue a tu hoja (todo lo demás funciona como siempre)
            return;
        }
        control.Ocupado = true;
        if (animando)
            return;
        if (!Zoom())
        {
            if (hojeando)
                Hojear();
            else
                Dibujar();
            // v65: con un gesto de la izquierda (puño, nodos, grosor) el pellizco derecho es de ese gesto.
            if (control.PunoIzquierdo && control.Der.valida && control.Der.pellizco)
                DialGoma();
            else if (!control.PunoIzquierdo && !control.NodosIzquierdo && !control.GrosorIzquierdo)
                GirarDisco();
            if (!control.Der.pellizco || !control.PunoIzquierdo)
                gomaDial = false;
        }
        control.MusicaMesa(actual != null && !hojeando, control.Der.indice);   // v64: suena al dibujar, como siempre
        ActualizarReglas();
        ActualizarBrilloHerramienta();
        if (mallaSucia)
            ConstruirMallaTinta();
        if (sucioHoja)
        {
            sucioHoja = false;
            texHoja.SetPixels32(pxHoja);
            texHoja.Apply(false);
        }
        if (sucioTinta)
        {
            sucioTinta = false;
            texTinta.SetPixels32(pxTinta);
            texTinta.Apply(false);
        }
        if (btnCerrarApp != null && cerrarAppHasta > 0f && Time.time > cerrarAppHasta)
        {
            cerrarAppHasta = 0f;
            btnCerrarApp.etiqueta.text = Idioma.T("Cerrar app");
        }
    }

    void LateUpdate()
    {
        if (activa && !verArchivo && control != null)
            control.Ocupado = true;
        if (control != null && control.Cabeza != null && flecha != null)
            ActualizarFlecha();
    }

    void Mostrar()
    {
        MostrarMesa(false);
    }

    // v62: "Ver en la mesa" (Propiedades en 2D o la voz): tu hoja del archivo sobre el disco, sin cerrar el archivo.
    public static void VerEnLaMesa()
    {
        if (instancia != null && !Activa)
            instancia.MostrarMesa(true);   // (si ya la estabas viendo, la quita)
    }

    // v62: la voz "centrar mesa".
    public static void CentrarVoz()
    {
        if (Activa && !instancia.animando)
            instancia.CentrarMesa();
    }

    void MostrarMesa(bool conArchivo)
    {
        if (verArchivo)
        {
            TerminarVer();
            if (conArchivo)
                return;
        }
        if (activa || animando || control == null || dibujo == null)
            return;
        if (conArchivo)
        {
            MostrarVer();
            return;
        }
        if (Titere.Activo)
        {
            dibujo.Mensaje("Primero suelta los personajes");
            return;
        }
        if (Funcion.Activa)
            Funcion.Alternar();
        // Guarda lo que estabas haciendo y deja la hoja en blanco (como Salir de siempre).
        if (dibujo.HayArchivoAbierto)
            dibujo.ArchivoNuevo();
        CerrarPaneles();
        if (!Armar())
            return;
        verArchivo = false;
        activa = true;
        control.Ocupado = true;
        PonerFondoDeMesa();
        SonidoDeMesa();
        QuizasOtraMuestra();
        Colocar();
        LlenarRecientes();
        PonerHojaDeArchivo(false);
        StartCoroutine(Aparecer());
    }

    // v64: en la mesa, el micrófono y el ojo empiezan APAGADOS; la música, la primera vez, prendida en arpa.
    void SonidoDeMesa()
    {
        if (Voz.Activa && Voz.Instancia != null)
            Voz.Instancia.Alternar();
        if (Mirada.Activa)
            Mirada.Alternar();
        if (sonidoMesaListo)
            return;
        sonidoMesaListo = true;
        if (BotonSonido.Silenciado)
            BotonSonido.Alternar();
        control.ElegirInstrumento(Sonidos.Arpa);
    }

    // ---------- v63: VER EN LA MESA: la mesa se pone DETRÁS de tu hoja 2D (su papel queda sobre el disco) y tú sigues
    // trabajando en tu archivo como siempre (paleta, gestos, capas, Propiedades...). "✦ Volver" la quita.

    void MostrarVer()
    {
        Vector3 punto, normal;
        if (!dibujo.PlanoActivo || !dibujo.PlanoDeCapa(dibujo.capaActual, out punto, out normal))
        {
            dibujo.Mensaje("Ver en la mesa: elige una capa 2D (con su hoja)");
            return;
        }
        if (!Armar())
            return;
        if (hojeando)
            PararHojear();
        TerminarLinea();
        verArchivo = true;
        activa = true;
        escondidosVer.Clear();
        foreach (Transform h in raiz)
        {
            bool queda = h == giratorio || h == tablero || h == orilla || h == btnEntrar.transform
                         || (btnEntrar.etiqueta != null && h == btnEntrar.etiqueta.transform);
            if (!queda && h.gameObject.activeSelf)
            {
                h.gameObject.SetActive(false);
                escondidosVer.Add(h.gameObject);
            }
        }
        // En el disco: tu hoja del archivo va ahí (la de la mesa, la de tinta y los pivotes se esconden).
        var delDisco = new List<Transform> { hoja, hojaTinta };
        delDisco.AddRange(pivotes);
        foreach (var h in delDisco)
            if (h != null && h.gameObject.activeSelf)
            {
                h.gameObject.SetActive(false);
                escondidosVer.Add(h.gameObject);
            }
        giratorio.localRotation = Quaternion.identity;
        Idioma.Poner(btnEntrar.etiqueta, "✦ Volver");
        raiz.gameObject.SetActive(true);
        AlinearConArchivo();
        control.SonidoBurbuja(raiz.position);
        dibujo.Mensaje("Tu hoja está en la mesa: sigue trabajando como siempre. ✦ Volver para quitarla");
    }

    void TerminarVer()
    {
        if (!verArchivo)
            return;
        verArchivo = false;
        activa = false;
        foreach (var g in escondidosVer)
            if (g != null)
                g.SetActive(true);
        escondidosVer.Clear();
        if (btnEntrar != null)
            Idioma.Poner(btnEntrar.etiqueta, "✦ Entrar");
        raiz.gameObject.SetActive(false);
        raiz.localScale = Vector3.one * escalaMesa;
        if (control != null && control.Cabeza != null)
            control.SonidoBurbuja(control.Cabeza.position + control.Cabeza.forward * 0.4f);
    }

    // El papel de tu hoja (1,2 unidades del dibujo de ancho) queda justo sobre la hoja del disco.
    void AlinearConArchivo()
    {
        Vector3 punto, normal;
        if (dibujo == null || !dibujo.gameObject.activeInHierarchy || !dibujo.PlanoActivo
            || !dibujo.PlanoDeCapa(dibujo.capaActual, out punto, out normal))
        {
            TerminarVer();
            return;
        }
        Transform d = dibujo.transform;
        Vector3 pMundo = d.TransformPoint(punto);
        Vector3 n = d.TransformDirection(normal);
        if (n.sqrMagnitude < 1e-8f)
            return;
        n.Normalize();
        Vector3 arriba = Vector3.ProjectOnPlane(d.up, n);
        if (arriba.sqrMagnitude < 1e-6f)
            arriba = Vector3.ProjectOnPlane(Vector3.forward, n);
        Quaternion giro = Quaternion.LookRotation(n, arriba.normalized);
        float escala = Mathf.Max(0.01f, 1.2f * d.lossyScale.x / AnchoHoja);
        Vector3 hojaLocal = giratorio.localPosition + new Vector3(0f, 0f, -0.0055f);
        raiz.rotation = giro;
        raiz.localScale = Vector3.one * escala;
        raiz.position = pMundo - giro * (hojaLocal * escala);
    }

    // La foto de tu dibujo (como la miniatura, con el lápiz 2D incluido) para ponerla en la hoja del disco.
    bool FotoDelArchivo()
    {
        try
        {
            Vector3 pos;
            Quaternion rot;
            float fov;
            dibujo.EncuadreExportar(1.1f, out pos, out rot, out fov);
            byte[] png = Exportar.Foto(pos, rot, fov, AnchoTex, AltoTex, Color.white, dibujo.MascaraExportar | 1);
            if (png == null)
                return false;
            if (texVer == null)
                texVer = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            return texVer.LoadImage(png);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: ver en la mesa: " + e.Message);
            return false;
        }
    }

    void PonerHojaDeArchivo(bool en)
    {
        var r = hoja.GetComponent<Renderer>();
        if (en && texVer != null)
        {
            if (matVer == null)
                matVer = new Material(matImagen);
            PonerTextura(matVer, texVer);
            r.sharedMaterial = matVer;
            tintaPintada = PlayerPrefs.GetInt(ClaveTintaPintada, 0) == 1;
            tintaPuesta = false;
            hojaTinta.gameObject.SetActive(false);
            textoAyuda.text = Idioma.T("Así se ve tu hoja en la mesa. Toca ✦ Volver al archivo");
        }
        else if (matHojaNormal != null)
        {
            r.sharedMaterial = matHojaNormal;
        }
        if (btnEntrar != null)
            Idioma.Poner(btnEntrar.etiqueta, en ? "✦ Volver al archivo" : "✦ Entrar");
    }

    void PonerFondoDeMesa()
    {
        if (escenario == null)
            escenario = FindFirstObjectByType<Escenario>(FindObjectsInactive.Include);
        if (escenario == null)
            return;
        fondoAntes = escenario.modo;
        semiAntes = escenario.Semi;
        if (escenario.modo != 2 || escenario.Semi)
            escenario.PonerModo(2, false);
        fondoMesaPuesto = true;
    }

    // Al entrar a tu boceto o volver al archivo: el fondo que tenías. Si lo cambiaste en la mesa (con el reloj), se queda.
    void DevolverFondo()
    {
        if (escenario == null || !fondoMesaPuesto)
            return;
        fondoMesaPuesto = false;
        if (escenario.modo != 2 || escenario.Semi || fondoAntes < 0)
            return;
        if (semiAntes)
            escenario.PonerSemi(false);
        else if (fondoAntes != 2)
            escenario.PonerModo(fondoAntes, false);
    }

    // v62: si la mesa queda fuera de tu vista (la moviste o la agrandaste mucho), una flecha te dice dónde está.
    void ActualizarFlecha()
    {
        var cab = control.Cabeza;
        bool ver = false;
        Vector3 dir = Vector3.zero;
        if (activa && !verArchivo && !animando && hoja != null)
        {
            dir = hoja.position - cab.position;
            ver = dir.sqrMagnitude > 1e-6f && Vector3.Angle(cab.forward, dir) > 50f;
        }
        if (flecha.gameObject.activeSelf != ver)
            flecha.gameObject.SetActive(ver);
        if (!ver)
            return;
        float a = Mathf.Atan2(Vector3.Dot(dir, cab.up), Vector3.Dot(dir, cab.right));
        Vector3 pos = cab.position + cab.forward * 0.6f + (cab.right * Mathf.Cos(a) + cab.up * Mathf.Sin(a)) * 0.15f;
        flecha.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cab.position, cab.up));
        puntaFlecha.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
    }


    // Todo lo de trabajar se cierra (paneles, ventanas, selección).
    void CerrarPaneles()
    {
        control.CerrarMenu();
        if (control.panelArriba != null)
            control.panelArriba.GuardarMesa();
        CapasProfundidad.CerrarSiAbierta();
        PanelLapiz.Cerrar();
        PanelPlastilina.Cerrar();
        PanelPropiedades.Cerrar();
        ImportadorSvg.Cerrar();
        SelectorFondos.Cerrar();
        CabezaInvisible.CerrarSiAbierta();
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (nav != null && nav.Abierto)
            nav.Cerrar();
        var buscador = FindFirstObjectByType<BuscadorImagenes>(FindObjectsInactive.Include);
        if (buscador != null && buscador.Abierto)
            buscador.Cerrar();
        dibujo.Seleccionar(null);
    }

    // Frente a ti, un poco abajo, inclinada hacia ti (como un tablero de dibujo).
    void Colocar()
    {
        var cab = control.Cabeza;
        Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        float a = Inclinacion * Mathf.Deg2Rad;
        Vector3 normal = Vector3.up * Mathf.Cos(a) - adelante * Mathf.Sin(a);       // hacia arriba y hacia ti
        Vector3 subeTablero = adelante * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a);
        baseMesa = cab.position + adelante * 0.5f - Vector3.up * 0.3f;
        giroMesa = Quaternion.LookRotation(-normal, subeTablero);
        AplicarPose();
    }

    void AplicarPose()
    {
        raiz.SetPositionAndRotation(baseMesa + giroMesa * desplazaMesa, giroMesa);
        raiz.localScale = Vector3.one * escalaMesa;
    }

    // v60/v62: ZOOM y LUGAR de la mesa: pellizca con LAS DOS manos; separar o juntar = más grande o más chica (de 0,3
    // a 15 veces, desde el punto entre tus manos); mover las manos = la mesa va con ellas (a los lados, arriba, abajo,
    // más cerca o más lejos). Se recuerda. Devuelve true mientras lo haces.
    bool Zoom()
    {
        var iz = control.Izq;
        var de = control.Der;
        bool ambas = iz.valida && de.valida && iz.pellizco && de.pellizco;
        if (!ambas)
        {
            if (zoomeando)
            {
                zoomeando = false;
                GuardarLugar();
            }
            return false;
        }
        float dist = Vector3.Distance(iz.PuntoPellizco, de.PuntoPellizco);
        Vector3 medio = (iz.PuntoPellizco + de.PuntoPellizco) * 0.5f;
        if (!zoomeando)
        {
            zoomeando = true;
            manoGiro = -1;
            agarrandoHoja = false;
            TerminarLinea();
            SoltarNodo();
            zoomDistancia = Mathf.Max(0.03f, dist);
            zoomEscala = escalaMesa;
            zoomMedio = medio;
            zoomPos = raiz.position;
        }
        escalaMesa = Mathf.Clamp(zoomEscala * dist / zoomDistancia, EscalaMin, EscalaMax);
        Vector3 pos = medio - (zoomMedio - zoomPos) * (escalaMesa / zoomEscala);
        desplazaMesa = Quaternion.Inverse(giroMesa) * (pos - baseMesa);
        AplicarPose();
        return true;
    }

    void GuardarLugar()
    {
        PlayerPrefs.SetFloat(ClaveEscala, escalaMesa);
        PlayerPrefs.SetFloat(ClaveDX, desplazaMesa.x);
        PlayerPrefs.SetFloat(ClaveDY, desplazaMesa.y);
        PlayerPrefs.SetFloat(ClaveDZ, desplazaMesa.z);
        PlayerPrefs.Save();
    }

    void CentrarMesa()
    {
        escalaMesa = 1f;
        desplazaMesa = Vector3.zero;
        GuardarLugar();
        Colocar();
        control.SonidoBurbuja(raiz.position);
    }

    // ---------- El bloc y los lápices ----------

    void ElegirHerramienta(int k)
    {
        if (verArchivo || k < 0 || k >= NombresHerramienta.Length)
            return;
        TerminarLinea();
        SoltarNodo();
        if (k <= 2 || k == Goma)
        {
            colorLapiz = k;
        }
        else
        {
            if (colorLapiz == Goma)
                colorLapiz = 0;
            tintaPintada = k == 5;
            PlayerPrefs.SetInt(ClaveTintaPintada, tintaPintada ? 1 : 0);
            PlayerPrefs.Save();
        }
        herramientaActual = k;
        AcomodarHerramientas();
        if (textoAyuda != null)
            textoAyuda.text = Idioma.T(NombresHerramienta[k]);
        if (control != null && k < herramientas.Count)
            control.SonidoBurbuja(herramientas[k].position);
    }

    // La elegida se levanta un poquito (hacia ti y hacia arriba).
    void AcomodarHerramientas()
    {
        for (int i = 0; i < herramientas.Count && i < basesHerramienta.Count; i++)
            herramientas[i].localPosition = basesHerramienta[i] + (i == herramientaActual ? new Vector3(0f, 0.006f, -0.006f) : Vector3.zero);
    }

    // El brillito (late suave) alrededor de la herramienta elegida.
    void ActualizarBrilloHerramienta()
    {
        if (brilloHerramienta == null || herramientaActual < 0 || herramientaActual >= cuerposHerramienta.Count)
            return;
        var h = herramientas[herramientaActual];
        Vector3 tam = cuerposHerramienta[herramientaActual].localScale;
        float late = 1f + 0.05f * Mathf.Sin(Time.time * 4f);
        brilloHerramienta.localPosition = h.localPosition + new Vector3(0f, 0f, 0.003f);
        brilloHerramienta.localScale = new Vector3((tam.x + 0.009f) * late, (tam.y + 0.009f) * late, 0.002f);
    }

    Transform NuevaHerramienta(Vector3 pos)
    {
        var t = new GameObject("Herramienta" + herramientas.Count).transform;
        t.SetParent(raiz, false);
        t.localPosition = pos;
        herramientas.Add(t);
        basesHerramienta.Add(pos);
        return t;
    }

    void BotonHerramienta(Transform h, Vector2 tam, Material m)
    {
        int k = herramientas.IndexOf(h);
        var b = PanelTutoriales.Boton(h, "", Vector3.zero, tam, m, m);
        b.alTocar.AddListener(() => ElegirHerramienta(k));
        BotonTocable.PonerNombre(b, NombresHerramienta[Mathf.Clamp(k, 0, NombresHerramienta.Length - 1)]);
        cuerposHerramienta.Add(b.transform);
    }

    void CambiarHoja(int paso)
    {
        if (hojeando)
            PararHojear();
        TerminarLinea();
        int n = boceto.actual + paso;
        if (n < 0 || n >= boceto.paginas.Count)
            return;
        boceto.actual = n;
        RehacerHoja();
        GuardarBoceto();
        control.SonidoBurbuja(hoja.position);
    }

    void AgregarHoja()
    {
        if (hojeando)
            PararHojear();
        TerminarLinea();
        Normalizar();
        GuardarDeshacer();
        boceto.paginas.Insert(boceto.actual + 1, new Pagina());
        boceto.actual++;
        tintaPuesta = false;
        RehacerHoja();
        GuardarBoceto();
        control.SonidoBurbuja(hoja.position);
    }

    void AlternarHojear()
    {
        if (hojeando)
            PararHojear();
        else
            EmpezarHojear();
    }

    void EmpezarHojear()
    {
        TerminarLinea();
        Normalizar();
        if (boceto.paginas.Count < 2)
        {
            textoAyuda.text = Idioma.T("Para reproducir agrega otra hoja con \"+ Hoja\" y dibuja el siguiente cuadro");
            return;
        }
        cuadrosHojear.Clear();
        for (int i = 0; i < boceto.paginas.Count; i++)
            cuadrosHojear.Add(HojaCompleta(i));
        hojeando = true;
        cuadroHojear = 0;
        proximoCuadro = 0f;
        hojaTinta.gameObject.SetActive(false);
        btnHojear.etiqueta.text = Idioma.T("■ Parar");
        btnHojear.Marcar(true);
    }

    void PararHojear()
    {
        hojeando = false;
        cuadrosHojear.Clear();
        btnHojear.etiqueta.text = Idioma.T("▶ Reproducir");
        btnHojear.Marcar(false);
        RehacerHoja();
    }

    // 8 hojas por segundo, en bucle (como pasar las hojas con el pulgar).
    void Hojear()
    {
        if (Time.time < proximoCuadro || cuadrosHojear.Count == 0)
            return;
        proximoCuadro = Time.time + 1f / 8f;
        System.Array.Copy(cuadrosHojear[cuadroHojear], pxHoja, pxHoja.Length);
        sucioHoja = true;
        cuadroHojear = (cuadroHojear + 1) % cuadrosHojear.Count;
    }

    IEnumerator Aparecer()
    {
        animando = true;
        raiz.gameObject.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            raiz.localScale = Vector3.one * Mathf.Lerp(0.05f, 1f, e) * escalaMesa;
            yield return null;
        }
        raiz.localScale = Vector3.one * escalaMesa;
        animando = false;
    }

    // ---------- ENTRAR (la magia: la hoja crece hacia ti y quedas adentro) ----------

    void EntrarABoceto()
    {
        StartCoroutine(Entrar(verArchivo ? -2 : -1));   // v62: -2 = volver a tu archivo ("Ver en la mesa")
    }

    // v64: la hoja 0 de arriba es "Archivo nuevo" (un dibujo en blanco); las demás, tus dibujos (i - 1 en la lista).
    void AbrirReciente(int i)
    {
        if (animando || i < 0)
            return;
        if (i == 0)
        {
            StartCoroutine(Entrar(-3));
            return;
        }
        if (i - 1 >= rutasRecientes.Count)
            return;
        StartCoroutine(Entrar(i - 1));
    }

    // v60: LA ZAMBULLIDA. El dibujo elegido aparece DE GOLPE en la hoja del disco (sin deslizarse). Luego tú te
    // zambulles en esa hoja: la mesa crece a tu alrededor mientras te acercas; a la mitad se abre el dibujo, la mesa se
    // va y la hoja (que ya te rodea) se desvanece mientras tus líneas de verdad aparecen poco a poco.
    IEnumerator Entrar(int reciente)
    {
        animando = true;
        control.MusicaMesa(false, Vector3.zero);
        if (hojeando)
            PararHojear();
        TerminarLinea();
        control.SonidoBurbuja(hoja.position);
        var rHoja = hoja.GetComponent<Renderer>();
        Material original = rHoja.sharedMaterial;
        var mVer = new Material(matTransparente);
        if (mVer.HasProperty("_BaseColor"))
            mVer.SetColor("_BaseColor", Color.white);
        Texture2D temporal = null;
        Texture tex;
        if (reciente == -2 && texVer != null)
        {
            tex = texVer;
        }
        else if (reciente == -3)
        {
            tex = Texture2D.whiteTexture;   // v64: archivo nuevo: una hoja en blanco
        }
        else if (reciente >= 0)
        {
            var tm = minis[reciente + 1].sharedMaterial;
            tex = tm.HasProperty("_BaseMap") ? tm.GetTexture("_BaseMap") : tm.mainTexture;
        }
        else
        {
            // Tu boceto con la tinta encima, en una sola imagen (para la transición).
            temporal = new Texture2D(AnchoTex, AltoTex, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            temporal.SetPixels32(HojaCompleta(boceto.actual));
            temporal.Apply(false);
            tex = temporal;
        }
        PonerTextura(mVer, tex);
        rHoja.sharedMaterial = mVer;
        hojaTinta.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.25f);   // un instante para verlo en la hoja

        var cab = control.Cabeza;
        Vector3 hoja0 = hoja.position;
        Quaternion r0 = raiz.rotation;
        Quaternion mira = Quaternion.LookRotation(hoja0 - cab.position, Vector3.up);
        Vector3 desde = raiz.position - hoja0;
        float esc0 = raiz.localScale.x;
        Vector3 meta = cab.position + (hoja0 - cab.position).normalized * 0.2f;
        // 1) Te acercas y la mesa crece a tu alrededor.
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            float k = Mathf.Lerp(1f, 3.5f, e * e);
            Quaternion r = Quaternion.Slerp(r0, mira, e);
            raiz.rotation = r;
            raiz.localScale = Vector3.one * esc0 * k;
            raiz.position = Vector3.Lerp(hoja0, meta, e) + (r * Quaternion.Inverse(r0)) * desde * k;
            yield return null;
        }
        // 2) Se abre el dibujo; la mesa se va y solo queda la hoja, que te rodea y se desvanece.
        hoja.SetParent(null, true);
        raiz.gameObject.SetActive(false);
        if (!dibujo.gameObject.activeSelf)
            dibujo.gameObject.SetActive(true);   // v62: venías de "Ver en la mesa"
        if (reciente == -2)
        {
            // Vuelves a tu archivo tal como estaba.
        }
        else if (reciente == -3)
        {
            // v64: un archivo nuevo, en blanco (tu boceto se queda en la mesa).
            if (dibujo.HayArchivoAbierto)
                dibujo.ArchivoNuevo();
            dibujo.Mensaje("Archivo nuevo");
        }
        else if (reciente < 0)
        {
            PasarBocetoAlDibujo();
        }
        else
        {
            fondoMesaPuesto = false;   // el archivo trae su propio fondo
            dibujo.AbrirArchivo(rutasRecientes[reciente]);
        }
        DevolverFondo();
        verArchivo = false;
        deshacerMesa.Clear();
        rehacerMesa.Clear();
        foreach (var t in dibujo.trazos)
            if (t != null)
                EfectosLinea.Materializar(t);
        Vector3 p1 = hoja.position, s1 = hoja.localScale;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            hoja.position = Vector3.Lerp(p1, cab.position + (p1 - cab.position).normalized * 0.06f, e);
            hoja.localScale = new Vector3(s1.x * Mathf.Lerp(1f, 2.2f, e), s1.y * Mathf.Lerp(1f, 2.2f, e), 1f);
            if (mVer.HasProperty("_BaseColor"))
                mVer.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f - e));
            yield return null;
        }
        // Todo vuelve a su lugar (para la próxima vez).
        hoja.SetParent(giratorio, false);
        hoja.localPosition = new Vector3(0f, 0f, -0.0055f);
        hoja.localRotation = Quaternion.identity;
        hoja.localScale = new Vector3(AnchoHoja, AltoHoja, 1f);
        rHoja.sharedMaterial = matHojaNormal != null ? matHojaNormal : original;
        Destroy(mVer);
        if (btnEntrar != null)
            Idioma.Poner(btnEntrar.etiqueta, "✦ Entrar");
        if (temporal != null)
            Destroy(temporal);
        raiz.localScale = Vector3.one * escalaMesa;
        activa = false;
        animando = false;
        control.Ocupado = false;
    }

    // Tu boceto pasa a un dibujo nuevo: el lápiz en la capa 1 (lápiz 2D) y la tinta en la capa 2 (líneas de verdad).
    void PasarBocetoAlDibujo()
    {
        // v62: con VARIAS hojas en el bloc, entran TODAS como cuadros de animación.
        if (boceto.paginas.Count > 1 && dibujo.animacion != null)
        {
            PasarBlocComoCuadros();
            boceto = new Guardado();
            tintaPuesta = false;
            RehacerHoja();
            GuardarBoceto();
            return;
        }
        bool hayLapiz = Pag.lapiz.Count > 0, hayTinta = Pag.tinta.Count > 0;
        var cab = control.Cabeza;
        Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized;
        Vector3 centro = cab.position + adelante * 0.7f;
        const float ancho = 0.9f, alto = ancho * AltoHoja / AnchoHoja;
        var raizD = dibujo.transform;
        System.Func<Vector3, Vector3> mundo = q => centro + derecha * ((q.x - 0.5f) * ancho) + Vector3.up * ((q.y - 0.5f) * alto);
        if (hayLapiz)
        {
            dibujo.SeleccionarCapa(0);
            dibujo.CapaActual.nombre = "Lápiz";
            if (dibujo.temblor != null)
                dibujo.temblor.PonerBoceto(true);
            dibujo.PonerPlano(true);
            dibujo.DefinirPlano(raizD.InverseTransformPoint(centro), adelante);
            var hojas = dibujo.GetComponent<HojasLapiz>();
            if (hojas == null)
                hojas = FindFirstObjectByType<HojasLapiz>();
            if (hojas != null)
            {
                float radio = hojas.radioLapiz * 1.6f / dibujo.EscalaMundo;
                foreach (var l in Pag.lapiz)
                {
                    bool esGoma = l.c == Goma;
                    var t = new TrazoLapiz { capa = dibujo.capaActual, borra = esGoma, radio = esGoma ? radio * 3f : radio };
                    foreach (var q in l.p)
                    {
                        Vector3 p = dibujo.ProyectarEnPlano(raizD.InverseTransformPoint(mundo(q)));
                        t.puntos.Add(new Vector4(p.x, p.y, p.z, Mathf.Clamp01(q.z)));
                    }
                    if (t.puntos.Count > 0)
                        hojas.AgregarHecho(t);
                }
            }
        }
        if (hayLapiz || hayTinta)
        {
            // La tinta va en su propia capa, encima del lápiz (en un dibujo nuevo la otra capa es la de la cámara).
            int capaTinta = hayLapiz ? dibujo.AgregarCapa("Tinta", false) : -1;
            if (capaTinta < 0)
            {
                dibujo.SeleccionarCapa(0);
                dibujo.CapaActual.nombre = "Tinta";
            }
            if (dibujo.temblor != null)
                dibujo.temblor.PonerBoceto(false);
            dibujo.PonerPlano(true);
            Vector3 centroTinta = centro - adelante * 0.002f;
            Vector3 planoLocal = raizD.InverseTransformPoint(centroTinta);
            dibujo.DefinirPlano(planoLocal, adelante);
            Vector3 normalLocal = raizD.InverseTransformDirection(adelante).normalized;
            foreach (var l in Pag.tinta)
                if (l.c != Goma)   // (la goma de la tinta solo vale en la mesa)
                    AgregarLineaVector(l, mundo, adelante, 0.002f, planoLocal, normalLocal, Color.black, 0f);
            dibujo.HayCambios = true;
            dibujo.Mensaje(hayLapiz ? "Tu boceto de la mesa: el lápiz en su capa; entinta en la capa Tinta" : "Tu tinta de la mesa ya está aquí");
        }
        else
        {
            dibujo.Mensaje("Hoja nueva");
        }
        // La hoja de la mesa queda lista para otro boceto (si hay más hojas en el bloc, se quedan; sale la que entró).
        if (boceto.paginas.Count > 1)
        {
            boceto.paginas.RemoveAt(boceto.actual);
            Normalizar();
        }
        else
        {
            boceto = new Guardado();
        }
        tintaPuesta = false;
        RehacerHoja();
        GuardarBoceto();
    }

    // v62: EL BLOC COMO ANIMACIÓN: hoja 1 = cuadro 1, hoja 2 = cuadro 2... El lápiz va en una capa CUADRO A CUADRO
    // "Lápiz" (líneas finas de su color) y la tinta en otra, "Tinta" (líneas negras). Las dos en 2D, en la misma hoja.
    void PasarBlocComoCuadros()
    {
        var anim = dibujo.animacion;
        var cab = control.Cabeza;
        Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized;
        Vector3 centro = cab.position + adelante * 0.7f;
        const float ancho = 0.9f, alto = ancho * AltoHoja / AnchoHoja;
        System.Func<Vector3, Vector3> mundo = q => centro + derecha * ((q.x - 0.5f) * ancho) + Vector3.up * ((q.y - 0.5f) * alto);
        var raizD = dibujo.transform;
        Vector3 normalLocal = raizD.InverseTransformDirection(adelante).normalized;
        bool hayLapiz = false, hayTinta = false;
        foreach (var pg in boceto.paginas)
        {
            foreach (var l in pg.lapiz)
                if (l.c != Goma && l.p.Count > 1)
                    hayLapiz = true;
            foreach (var l in pg.tinta)
                if (l.c != Goma && l.p.Count > 1)
                    hayTinta = true;
        }
        if (!hayLapiz && !hayTinta)
        {
            dibujo.Mensaje("Hoja nueva");
            return;
        }
        int cuantas = boceto.paginas.Count;
        if (hayLapiz)
        {
            dibujo.SeleccionarCapa(0);
            int capaLapiz = dibujo.capaActual;
            dibujo.CapaActual.nombre = "Lápiz";
            dibujo.CapaActual.cuadroACuadro = true;
            if (dibujo.temblor != null)
                dibujo.temblor.PonerBoceto(false);
            dibujo.PonerPlano(true);
            Vector3 planoLocal = raizD.InverseTransformPoint(centro);
            dibujo.DefinirPlano(planoLocal, adelante);
            for (int i = 0; i < cuantas; i++)
            {
                anim.IrA(i);
                foreach (var l in boceto.paginas[i].lapiz)
                    if (l.c != Goma)
                        AgregarLineaVector(l, mundo, adelante, 0f, planoLocal, normalLocal,
                                           Color.Lerp(ColoresLapiz[Mathf.Clamp(l.c, 0, Goma - 1)], Color.white, 0.25f), 0.004f);
                anim.GuardarClaveEn(i, capaLapiz);
            }
        }
        if (hayTinta)
        {
            anim.IrA(0);
            int capaTinta = hayLapiz ? dibujo.AgregarCuadroACuadro() : -1;
            if (capaTinta < 0)
            {
                dibujo.SeleccionarCapa(0);
                capaTinta = dibujo.capaActual;
                dibujo.CapaActual.cuadroACuadro = true;
            }
            dibujo.CapaActual.nombre = "Tinta";
            if (dibujo.temblor != null)
                dibujo.temblor.PonerBoceto(false);
            dibujo.PonerPlano(true);
            Vector3 planoLocal = raizD.InverseTransformPoint(centro - adelante * 0.002f);
            dibujo.DefinirPlano(planoLocal, adelante);
            for (int i = 0; i < cuantas; i++)
            {
                anim.IrA(i);
                foreach (var l in boceto.paginas[i].tinta)
                    if (l.c != Goma)
                        AgregarLineaVector(l, mundo, adelante, 0.002f, planoLocal, normalLocal, Color.black, 0f);
                anim.GuardarClaveEn(i, capaTinta);
            }
        }
        anim.IrA(0);
        PapelCebolla.Encender();
        dibujo.HayCambios = true;
        dibujo.Mensaje("Tu bloc ya es animación: " + cuantas + " cuadros (capas cuadro a cuadro). Dale Play en la línea de tiempo");
    }

    void AgregarLineaVector(Linea l, System.Func<Vector3, Vector3> mundo, Vector3 adelante, float separacion,
                            Vector3 planoLocal, Vector3 normalLocal, Color color, float ancho)
    {
        if (l.p.Count < 2)
            return;
        var raizD = dibujo.transform;
        var d = new DatosTrazo { color = color, enHoja = true, hojaPunto = planoLocal, hojaNormal = normalLocal };
        if (ancho > 0f)
            d.ancho = ancho;
        if (l.vector)
        {
            // v65: la tinta en líneas pasa con SUS nodos, su grosor y el de cada nodo (la hoja de 30 cm es de 90 cm).
            d.ancho = AnchoTintaMesa * (l.a > 0f ? l.a : 1f) * (0.9f / AnchoHoja) / Mathf.Max(1e-4f, dibujo.EscalaMundo);
            foreach (var q in l.p)
            {
                d.nodos.Add(raizD.InverseTransformPoint(mundo(q) - adelante * separacion));
                d.asaEntrada.Add(Vector3.zero);
                d.asaSalida.Add(Vector3.zero);
                d.asaManual.Add(false);
                d.grosorNodo.Add(Mathf.Clamp(q.z, 0.15f, 5f));
            }
            dibujo.AgregarTrazo(d);
            return;
        }
        var puntos = new List<Vector3>();
        foreach (var q in l.p)
            puntos.Add(raizD.InverseTransformPoint(mundo(q) - adelante * separacion));
        var simples = Simplificar(puntos, 0.0015f / dibujo.EscalaMundo);
        if (simples.Count < 2)
            return;
        foreach (var p in simples)
        {
            d.nodos.Add(p);
            d.asaEntrada.Add(Vector3.zero);
            d.asaSalida.Add(Vector3.zero);
            d.asaManual.Add(false);
            d.grosorNodo.Add(1f);
        }
        dibujo.AgregarTrazo(d);
    }

    // Douglas-Peucker: menos puntos (los nodos de la tinta), la misma forma.
    static List<Vector3> Simplificar(List<Vector3> p, float tolerancia)
    {
        var r = new List<Vector3>();
        if (p.Count < 3)
        {
            r.AddRange(p);
            return r;
        }
        var marca = new bool[p.Count];
        marca[0] = marca[p.Count - 1] = true;
        var pila = new Stack<Vector2Int>();
        pila.Push(new Vector2Int(0, p.Count - 1));
        while (pila.Count > 0)
        {
            var s = pila.Pop();
            float mejor = 0f;
            int idx = -1;
            for (int i = s.x + 1; i < s.y; i++)
            {
                float d = DistanciaASegmento(p[i], p[s.x], p[s.y]);
                if (d > mejor)
                {
                    mejor = d;
                    idx = i;
                }
            }
            if (idx >= 0 && mejor > tolerancia)
            {
                marca[idx] = true;
                pila.Push(new Vector2Int(s.x, idx));
                pila.Push(new Vector2Int(idx, s.y));
            }
        }
        for (int i = 0; i < p.Count; i++)
            if (marca[i])
                r.Add(p[i]);
        return r;
    }

    static float DistanciaASegmento(Vector3 q, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = ab.sqrMagnitude > 1e-12f ? Mathf.Clamp01(Vector3.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector3.Distance(q, a + ab * t);
    }

    // ---------- Dibujar en la hoja (índice derecho) ----------
    // v62: con el PUÑO IZQUIERDO, el índice borra (como la goma). En la hoja de tinta (modo "líneas", el de siempre),
    // la tinta son LÍNEAS DE VERDAD: el puño (o la goma) toca una línea y se va en burbujas; con el gesto de NODOS
    // (pulgar + medio izquierdos) se ven sus nodos y los arrastras con el índice.

    void Dibujar()
    {
        var der = control.Der;
        bool grosor = tintaPuesta && !tintaPintada && control.GrosorIzquierdo;
        if (grosor)
        {
            TerminarLinea();
            GrosorTinta();
        }
        else if (grosorActivo)
        {
            SoltarGrosor();
        }
        bool nodos = tintaPuesta && !tintaPintada && (control.NodosIzquierdo || grosor);
        if (nodos != nodosVisibles)
        {
            nodosVisibles = nodos;
            SoltarNodo();
            TerminarLinea();
            mallaSucia = true;
        }
        if (!der.valida || der.pellizco)
        {
            tocandoNumero = false;
            TerminarLinea();
            SoltarNodo();
            return;
        }
        Vector3 dedo = Filtrar(der.indice);
        Transform plano = tintaPuesta ? hojaTinta : hoja;
        Vector3 l = plano.InverseTransformPoint(dedo);
        float distancia = -l.z * escalaMesa;   // la hoja mira hacia ti (su -Z); v62: en metros, sea cual sea el tamaño
        bool dentro = Mathf.Abs(l.x) < 0.5f && Mathf.Abs(l.y) < 0.5f;
        var q = new Vector3(l.x + 0.5f, l.y + 0.5f, 0f);
        if (grosor)
            return;
        if (nodos)
        {
            NodosTinta(q, dentro && distancia < AlcanceLapiz * 2.5f && distancia > -0.04f);
            return;
        }
        bool toca = dentro && distancia < (actual != null ? AlcanceLapiz * 1.5f : AlcanceLapiz) && distancia > -0.03f;
        if (!toca)
        {
            tocandoNumero = false;
            TerminarLinea();
            return;
        }
        // v61: tocar el número de la hoja la marca (o desmarca) como CLAVE.
        if (tocandoNumero)
            return;
        if (actual == null && !hojeando && EnNumero(q))
        {
            tocandoNumero = true;
            AlternarClave();
            return;
        }
        float presion = Mathf.Clamp01(1f - Mathf.Max(0f, distancia) / AlcanceLapiz);
        q.z = Mathf.Max(0.15f, presion);
        bool puno = control.PunoIzquierdo;
        int c = tintaPuesta ? ((colorLapiz == Goma || puno) ? Goma : 0) : (puno ? Goma : colorLapiz);
        if (tintaPuesta && !tintaPintada && c == Goma)
        {
            TerminarLinea();
            BorrarLineaTinta(q);
            return;
        }
        bool esVector = tintaPuesta && !tintaPintada && c != Goma;
        if (esVector)
            q.z = 1f;   // v65: en la tinta en líneas, z = el grosor del nodo (1 = normal)
        if (actual == null || actualTinta != tintaPuesta || actual.c != c)
        {
            TerminarLinea();
            GuardarDeshacer();
            actual = new Linea { c = c, vector = esVector, a = esVector ? anchoPincelMesa : 0f };
            actualTinta = tintaPuesta;
            (tintaPuesta ? Pag.tinta : Pag.lapiz).Add(actual);
        }
        var lista = actual.p;
        if (lista.Count > 0)
        {
            Vector3 ultimo = lista[lista.Count - 1];
            if (Vector2.Distance(ultimo, q) < 0.0015f)
                return;
            if (actual.vector)
                mallaSucia = true;
            else
                Trazar(ultimo, q, actualTinta, actual.c);
        }
        else if (actual.vector)
        {
            mallaSucia = true;
        }
        else
        {
            Trazar(q, q, actualTinta, actual.c);
        }
        lista.Add(q);
    }

    void TerminarLinea()
    {
        if (actual == null)
            return;
        var l = actual;
        actual = null;
        if (l.p.Count == 0)
        {
            (actualTinta ? Pag.tinta : Pag.lapiz).Remove(l);
        }
        else if (l.vector && l.p.Count > 2)
        {
            // v62: la línea de tinta queda con sus NODOS (menos puntos, la misma forma) y se dibuja lisa.
            l.p = SimplificarHoja(l.p, 1.4f);
            mallaSucia = true;
        }
        GuardarBoceto();
    }

    // ---------- v62: la tinta como LÍNEAS (vectores): suaves, nodos, borrar con burbujas ----------

    void RehacerTinta()
    {
        for (int i = 0; i < pxTinta.Length; i++)
            pxTinta[i] = vacioTinta;
        foreach (var l in Pag.tinta)
            RepintarLinea(l, true);
        sucioTinta = true;
        mallaSucia = true;   // v63: la tinta en líneas (y sus nodos) va en su malla
    }

    // ---------- v63: la malla de la tinta en líneas (cintas con puntas redondas) y la de sus nodos ----------

    void HijoMalla(string nombre, Mesh malla, Material m, float z)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(hojaTinta, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    void ConstruirMallaTinta()
    {
        mallaSucia = false;
        if (mallaTinta == null || mallaNodos == null)
            return;
        var v = new List<Vector3>();
        var tri = new List<int>();
        foreach (var l in Pag.tinta)
            if (l.vector && l.p.Count > 0)
                Cinta(l == actual ? l.p : Suave(l.p), l.a > 0f ? l.a : 1f, v, tri);
        mallaTinta.Clear();
        mallaTinta.SetVertices(v);
        mallaTinta.SetTriangles(tri, 0);
        mallaTinta.RecalculateBounds();
        var vn = new List<Vector3>();
        var tn = new List<int>();
        if (nodosVisibles)
            foreach (var l in Pag.tinta)
                if (l.vector)
                    foreach (var p in l.p)
                        Disco(p, l == lineaNodo ? 0.0024f : 0.0017f, vn, tn);
        mallaNodos.Clear();
        mallaNodos.SetVertices(vn);
        mallaNodos.SetTriangles(tn, 0);
        mallaNodos.RecalculateBounds();
    }

    // De la hoja (u, v) a metros del papel, y de ahí a la malla (coordenadas de la hoja de tinta).
    static Vector2 EnMetros(Vector3 q)
    {
        return new Vector2((q.x - 0.5f) * AnchoHoja, (q.y - 0.5f) * AltoHoja);
    }

    static Vector3 EnMalla(Vector2 m)
    {
        return new Vector3(m.x / AnchoHoja, m.y / AltoHoja, 0f);
    }

    // v64: el grosor de la tinta en líneas: el mismo perfil que las líneas de adentro de los archivos (gruesa en el
    // centro y en punta en los extremos; Trazo.PerfilPunta).
    const float AnchoTintaMesa = 0.0024f;   // metros del papel, en el centro de la línea

    static void Cinta(List<Vector3> pts, float grosor, List<Vector3> v, List<int> tri)
    {
        int n = pts.Count;
        if (n == 0)
            return;
        if (n == 1)
        {
            Disco(pts[0], AnchoTintaMesa * 0.25f * grosor, v, tri);
            return;
        }
        var largos = new float[n];
        for (int i = 1; i < n; i++)
            largos[i] = largos[i - 1] + Vector2.Distance(EnMetros(pts[i - 1]), EnMetros(pts[i]));
        float total = Mathf.Max(1e-6f, largos[n - 1]);
        int inicio = v.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 antes = EnMetros(pts[Mathf.Max(0, i - 1)]), despues = EnMetros(pts[Mathf.Min(n - 1, i + 1)]);
            Vector2 t = despues - antes;
            if (t.sqrMagnitude < 1e-12f)
                t = Vector2.right;
            t.Normalize();
            float nodo = Mathf.Clamp(pts[i].z, 0.1f, 6f);   // el grosor del nodo (antes de la v65 era la presión)
            Vector2 lado = new Vector2(-t.y, t.x) * (AnchoTintaMesa * 0.5f * grosor * nodo * Trazo.PerfilPunta(largos[i] / total));
            Vector2 c = EnMetros(pts[i]);
            v.Add(EnMalla(c + lado));
            v.Add(EnMalla(c - lado));
        }
        for (int i = 0; i < n - 1; i++)
        {
            int k = inicio + i * 2;
            tri.Add(k); tri.Add(k + 2); tri.Add(k + 1);
            tri.Add(k + 1); tri.Add(k + 2); tri.Add(k + 3);
            tri.Add(k); tri.Add(k + 1); tri.Add(k + 2);       // y por detrás
            tri.Add(k + 1); tri.Add(k + 3); tri.Add(k + 2);
        }
    }

    static void Disco(Vector3 q, float radio, List<Vector3> v, List<int> tri)
    {
        const int lados = 10;
        Vector2 c = EnMetros(q);
        int i0 = v.Count;
        v.Add(EnMalla(c));
        for (int k = 0; k < lados; k++)
        {
            float ang = k * Mathf.PI * 2f / lados;
            v.Add(EnMalla(c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radio));
        }
        for (int k = 0; k < lados; k++)
        {
            int x = i0 + 1 + k, y = i0 + 1 + (k + 1) % lados;
            tri.Add(i0); tri.Add(x); tri.Add(y);
            tri.Add(i0); tri.Add(y); tri.Add(x);
        }
    }

    // La curva lisa que pasa por los nodos (Catmull-Rom).
    static List<Vector3> Suave(List<Vector3> p)
    {
        var r = new List<Vector3>();
        if (p.Count < 3)
        {
            r.AddRange(p);
            return r;
        }
        const int pasos = 6;
        for (int i = 0; i < p.Count - 1; i++)
        {
            Vector3 a = p[Mathf.Max(0, i - 1)], b = p[i], c = p[i + 1], d = p[Mathf.Min(p.Count - 1, i + 2)];
            for (int k = 0; k < pasos; k++)
            {
                float t = k / (float)pasos, t2 = t * t, t3 = t2 * t;
                Vector3 q = 0.5f * (2f * b + (c - a) * t + (2f * a - 5f * b + 4f * c - d) * t2 + (3f * b - a - 3f * c + d) * t3);
                q.z = Mathf.Lerp(b.z, c.z, t);
                r.Add(q);
            }
        }
        r.Add(p[p.Count - 1]);
        return r;
    }

    static Vector2 EnPx(Vector3 q)
    {
        return new Vector2(q.x * AnchoTex, q.y * AltoTex);
    }

    static float DistanciaPx(Vector2 q, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude > 1e-9f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector2.Distance(q, a + ab * t);
    }

    // Douglas-Peucker en píxeles de la hoja (la presión se conserva en cada nodo).
    static List<Vector3> SimplificarHoja(List<Vector3> p, float toleranciaPx)
    {
        var r = new List<Vector3>();
        if (p.Count < 3)
        {
            r.AddRange(p);
            return r;
        }
        var marca = new bool[p.Count];
        marca[0] = marca[p.Count - 1] = true;
        var pila = new Stack<Vector2Int>();
        pila.Push(new Vector2Int(0, p.Count - 1));
        while (pila.Count > 0)
        {
            var s = pila.Pop();
            Vector2 a = EnPx(p[s.x]), b = EnPx(p[s.y]);
            float peor = 0f;
            int idx = -1;
            for (int i = s.x + 1; i < s.y; i++)
            {
                float d = DistanciaPx(EnPx(p[i]), a, b);
                if (d > peor)
                {
                    peor = d;
                    idx = i;
                }
            }
            if (idx >= 0 && peor > toleranciaPx)
            {
                marca[idx] = true;
                pila.Push(new Vector2Int(s.x, idx));
                pila.Push(new Vector2Int(idx, s.y));
            }
        }
        for (int i = 0; i < p.Count; i++)
            if (marca[i])
                r.Add(p[i]);
        return r;
    }

    // v65: la goma (o el puño) borra los NODOS que toca, como dentro de los archivos: con "Unir" (Propiedades · Tinta,
    // como siempre) la línea sigue entre los nodos de al lado; con "Cortar" la línea queda partida ahí. Los nodos se van
    // en burbujas. La tinta pintada se borra por pedacitos.
    void BorrarLineaTinta(Vector3 q)
    {
        float radioPx = 8f * tamanoGomaMesa;
        Vector2 dedo = EnPx(q);
        bool cortar = Trazo.BorrarCortando;
        bool borro = false;
        var nuevas = new List<Linea>();
        var quitar = new List<Linea>();
        var burbujas = new List<Vector3>();
        foreach (var l in Pag.tinta)
        {
            if (l.c == Goma || l.p.Count == 0)
                continue;
            var pts = l.p;
            var fuera = new bool[pts.Count];
            bool toca = false;
            for (int i = 0; i < pts.Count; i++)
            {
                fuera[i] = Vector2.Distance(dedo, EnPx(pts[i])) >= radioPx;
                if (!fuera[i])
                    toca = true;
            }
            if (!toca)
                continue;
            if (!borro && Time.time - ultimoBorradoTinta > 0.5f)
                GuardarDeshacer();   // una vez por cada pasada de la goma
            borro = true;
            quitar.Add(l);
            bool partir = !l.vector || cortar;
            var tramo = new List<Vector3>();
            for (int i = 0; i <= pts.Count; i++)
            {
                if (i < pts.Count && fuera[i])
                {
                    tramo.Add(pts[i]);
                    continue;
                }
                if (i < pts.Count)
                    burbujas.Add(pts[i]);
                if (partir || i == pts.Count)
                {
                    if (tramo.Count >= 2)
                        nuevas.Add(new Linea { c = l.c, vector = l.vector, a = l.a, p = tramo });
                    tramo = new List<Vector3>();
                }
            }
        }
        if (!borro)
            return;
        foreach (var l in quitar)
            Pag.tinta.Remove(l);
        Pag.tinta.AddRange(nuevas);
        for (int i = 0; i < burbujas.Count; i++)
            StartCoroutine(Burbuja(hojaTinta.TransformPoint(new Vector3(burbujas[i].x - 0.5f, burbujas[i].y - 0.5f, 0f)), i * 0.015f));
        RehacerTinta();
        GuardarBoceto();
        if (Time.time - ultimoBorradoTinta > 0.25f)
            control.SonidoBurbuja(hojaTinta.TransformPoint(new Vector3(q.x - 0.5f, q.y - 0.5f, 0f)));
        ultimoBorradoTinta = Time.time;
    }

    // v65: EL GROSOR (pulgar + anular izquierdos), como dentro de los archivos: pellizca con la derecha SOBRE un nodo y
    // sube o baja = el grosor de ese nodo; pellizca en el aire y sube o baja = el grosor de la tinta nueva.
    void GrosorTinta()
    {
        var der = control.Der;
        if (!der.valida || !der.pellizco)
        {
            SoltarGrosor();
            return;
        }
        Vector3 p = der.PuntoPellizco;
        if (!grosorActivo)
        {
            grosorActivo = true;
            alturaGrosor = p.y;
            lineaGrosor = null;
            Vector3 l = hojaTinta.InverseTransformPoint(p);
            if (Mathf.Abs(l.z * escalaMesa) < 0.04f)
            {
                Vector2 dedo = EnPx(new Vector3(l.x + 0.5f, l.y + 0.5f, 0f));
                float mejor = 14f;
                foreach (var linea in Pag.tinta)
                {
                    if (!linea.vector)
                        continue;
                    for (int i = 0; i < linea.p.Count; i++)
                    {
                        float d = Vector2.Distance(dedo, EnPx(linea.p[i]));
                        if (d < mejor)
                        {
                            mejor = d;
                            lineaGrosor = linea;
                            nodoGrosor = i;
                        }
                    }
                }
            }
            if (lineaGrosor != null)
            {
                GuardarDeshacer();
                grosorInicio = lineaGrosor.p[nodoGrosor].z;
            }
            else
            {
                grosorInicio = anchoPincelMesa;
            }
        }
        float dy = p.y - alturaGrosor;
        if (lineaGrosor != null && nodoGrosor < lineaGrosor.p.Count)
        {
            Vector3 n = lineaGrosor.p[nodoGrosor];
            n.z = Mathf.Clamp(grosorInicio * Mathf.Exp(dy * control.SensibilidadGrosor), 0.15f, 5f);
            lineaGrosor.p[nodoGrosor] = n;
            mallaSucia = true;
            textoAyuda.text = Idioma.T("Grosor del nodo") + " x" + n.z.ToString("0.0");
        }
        else
        {
            anchoPincelMesa = Mathf.Clamp(grosorInicio * Mathf.Exp(dy * control.SensibilidadGrosor * 2f), 0.3f, 4f);
            textoAyuda.text = Idioma.T("Grosor de la tinta") + " x" + anchoPincelMesa.ToString("0.0");
        }
    }

    void SoltarGrosor()
    {
        if (!grosorActivo)
            return;
        grosorActivo = false;
        if (lineaGrosor != null)
            GuardarBoceto();
        lineaGrosor = null;
    }

    // v65: el TAMAÑO DE LA GOMA (puño izquierdo + pellizco derecho; arriba = más grande, abajo = más chica).
    void DialGoma()
    {
        float y = control.Der.PuntoPellizco.y;
        if (!gomaDial)
        {
            gomaDial = true;
            gomaAltura = y;
            gomaInicio = tamanoGomaMesa;
            TerminarLinea();
        }
        tamanoGomaMesa = Mathf.Clamp(gomaInicio * Mathf.Exp((y - gomaAltura) * control.SensibilidadGrosor * 3f), 0.3f, 6f);
        textoAyuda.text = Idioma.T("Borrador") + " x" + tamanoGomaMesa.ToString("0.0");
    }

    void NodosTinta(Vector3 q, bool tocando)
    {
        if (!tocando)
        {
            SoltarNodo();
            return;
        }
        if (lineaNodo == null)
        {
            Vector2 dedo = EnPx(q);
            float mejor = 14f;
            foreach (var l in Pag.tinta)
            {
                if (!l.vector)
                    continue;
                for (int i = 0; i < l.p.Count; i++)
                {
                    float d = Vector2.Distance(dedo, EnPx(l.p[i]));
                    if (d < mejor)
                    {
                        mejor = d;
                        lineaNodo = l;
                        nodoTomado = i;
                    }
                }
            }
            if (lineaNodo == null)
                return;
            GuardarDeshacer();
            control.SonidoBurbuja(hojaTinta.position);
        }
        if (nodoTomado < 0 || nodoTomado >= lineaNodo.p.Count)
        {
            SoltarNodo();
            return;
        }
        Vector3 p = lineaNodo.p[nodoTomado];
        lineaNodo.p[nodoTomado] = new Vector3(Mathf.Clamp01(q.x), Mathf.Clamp01(q.y), p.z);
        mallaSucia = true;
    }

    void SoltarNodo()
    {
        if (lineaNodo == null)
            return;
        lineaNodo = null;
        nodoTomado = -1;
        mallaSucia = true;
        GuardarBoceto();
    }

    void Burbujas(Linea l)
    {
        var pts = l.vector ? Suave(l.p) : l.p;
        int paso = Mathf.Max(1, pts.Count / 14);
        int n = 0;
        for (int i = 0; i < pts.Count; i += paso)
        {
            Vector3 w = hojaTinta.TransformPoint(new Vector3(pts[i].x - 0.5f, pts[i].y - 0.5f, 0f));
            StartCoroutine(Burbuja(w, n * 0.02f));
            n++;
        }
    }

    IEnumerator Burbuja(Vector3 pos, float espera)
    {
        if (espera > 0f)
            yield return new WaitForSeconds(espera);
        if (matBurbuja == null)
        {
            matBurbuja = new Material(matTransparente);
            if (matBurbuja.HasProperty("_BaseColor"))
                matBurbuja.SetColor("_BaseColor", new Color(0.78f, 0.9f, 1f, 0.55f));
        }
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = matBurbuja;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        float radio = 0.007f * escalaMesa;
        Vector3 sube = -hojaTinta.forward * 0.03f * escalaMesa + Vector3.up * 0.01f * escalaMesa;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
        {
            go.transform.position = pos + sube * t;
            go.transform.localScale = Vector3.one * radio * (0.3f + 1.3f * t);
            yield return null;
        }
        Destroy(go);
    }

    static float Alfa(float dt, float corte)
    {
        float tau = 1f / (2f * Mathf.PI * Mathf.Max(0.01f, corte));
        return 1f / (1f + tau / dt);
    }

    Vector3 Filtrar(Vector3 crudo)
    {
        float dt = Mathf.Max(1e-4f, Time.deltaTime);
        if (!tieneFiltro)
        {
            tieneFiltro = true;
            filtrado = crudo;
            crudoPrevio = crudo;
            velocidad = Vector3.zero;
            return crudo;
        }
        Vector3 v = (crudo - crudoPrevio) / dt;
        crudoPrevio = crudo;
        velocidad = Vector3.Lerp(velocidad, v, Alfa(dt, 1f));
        float corte = 1.2f + 12f * velocidad.magnitude;
        filtrado = Vector3.Lerp(filtrado, crudo, Alfa(dt, corte));
        return filtrado;
    }

    // Sellos entre dos puntos de la hoja (u, v, presión).
    void Trazar(Vector3 a, Vector3 b, bool tinta, int c = 0)
    {
        if (c == Goma)
        {
            TrazarGoma(a, b, tinta);
            return;
        }
        Color colorLinea = ColoresLapiz[Mathf.Clamp(c, 0, ColoresLapiz.Length - 1)];
        float pxA = a.x * AnchoTex, pyA = a.y * AltoTex, pxB = b.x * AnchoTex, pyB = b.y * AltoTex;
        float largo = Mathf.Sqrt((pxB - pxA) * (pxB - pxA) + (pyB - pyA) * (pyB - pyA));
        int pasos = Mathf.Max(1, Mathf.CeilToInt(largo / 0.8f));
        for (int i = 0; i <= pasos; i++)
        {
            float t = i / (float)pasos;
            float pr = Mathf.Lerp(a.z, b.z, t);
            if (tinta)
                Sello(pxTinta, Mathf.Lerp(pxA, pxB, t), Mathf.Lerp(pyA, pyB, t), 1.9f, new Color(0.05f, 0.05f, 0.07f), 0.9f, false);
            else
                Sello(pxHoja, Mathf.Lerp(pxA, pxB, t), Mathf.Lerp(pyA, pyB, t), 1.1f + 1.4f * pr, colorLinea, 0.12f + 0.3f * pr, true);
        }
        if (tinta)
            sucioTinta = true;
        else
            sucioHoja = true;
    }

    // v61: LA GOMA: devuelve el papel (en la hoja de lápiz) o deja limpio el papel de calcar (en la tinta).
    void TrazarGoma(Vector3 a, Vector3 b, bool tinta)
    {
        float pxA = a.x * AnchoTex, pyA = a.y * AltoTex, pxB = b.x * AnchoTex, pyB = b.y * AltoTex;
        float largo = Mathf.Sqrt((pxB - pxA) * (pxB - pxA) + (pyB - pyA) * (pyB - pyA));
        int pasos = Mathf.Max(1, Mathf.CeilToInt(largo / 1.5f));
        Color32[] px = tinta ? pxTinta : pxHoja;
        for (int i = 0; i <= pasos; i++)
        {
            float t = i / (float)pasos;
            float radio = (5f + 4f * Mathf.Lerp(a.z, b.z, t)) * tamanoGomaMesa;
            float cx = Mathf.Lerp(pxA, pxB, t), cy = Mathf.Lerp(pyA, pyB, t);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radio)), x1 = Mathf.Min(AnchoTex - 1, Mathf.CeilToInt(cx + radio));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radio)), y1 = Mathf.Min(AltoTex - 1, Mathf.CeilToInt(cy + radio));
            float r2 = radio * radio;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - cx, dy = y - cy, d2 = dx * dx + dy * dy;
                    if (d2 > r2)
                        continue;
                    float f = Mathf.Clamp01((1f - d2 / r2) * 2.5f);
                    int k = y * AnchoTex + x;
                    Color32 o = tinta ? vacioTinta : (fondoGoma != null ? fondoGoma[k] : ColorPapel);
                    px[k] = Color32.Lerp(px[k], o, f);
                }
        }
        if (tinta)
            sucioTinta = true;
        else
            sucioHoja = true;
    }

    // Un sello redondo y suave. "grano": como grafito sobre papel (con huequitos).
    static void Sello(Color32[] px, float cx, float cy, float radio, Color color, float fuerza, bool grano)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radio)), x1 = Mathf.Min(AnchoTex - 1, Mathf.CeilToInt(cx + radio));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radio)), y1 = Mathf.Min(AltoTex - 1, Mathf.CeilToInt(cy + radio));
        float r2 = radio * radio;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy, d2 = dx * dx + dy * dy;
                if (d2 > r2)
                    continue;
                float f = 1f - d2 / r2;
                float a = fuerza * f;
                if (grano)
                    a *= 0.55f + 0.45f * Hash(x, y);
                int i = y * AnchoTex + x;
                Color32 c = px[i];
                px[i] = new Color32((byte)Mathf.Lerp(c.r, color.r * 255f, a), (byte)Mathf.Lerp(c.g, color.g * 255f, a),
                                    (byte)Mathf.Lerp(c.b, color.b * 255f, a), (byte)Mathf.Max(c.a, Mathf.Clamp(a * 255f * 1.4f, 0f, 255f)));
            }
    }

    static float Hash(int x, int y)
    {
        uint h = (uint)(x * 374761393 + y * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h & 0xFFFF) / 65535f;
    }

    // ---------- Girar el disco (pellizco en el disco, fuera de la hoja) ----------

    // v63: el disco gira SOLO con la mano DERECHA pellizcando una de las REGLAS negras (arriba o abajo), que brillan
    // al acercarte. Pellizcar la HOJA y jalarla rápido hacia afuera = botarla (con todo lo que tenga; Deshacer la trae).
    void GirarDisco()
    {
        var m = control.Der;
        if (manoGiro >= 0)
        {
            if (!m.valida || !m.pellizco)
            {
                manoGiro = -1;
                return;
            }
            float ang = AnguloEnDisco(m.PuntoPellizco);
            giratorio.localRotation *= Quaternion.Euler(0f, 0f, Mathf.DeltaAngle(anguloAntes, ang));
            anguloAntes = ang;
            return;
        }
        if (agarrandoHoja)
        {
            SeguirAgarreHoja(m);
            return;
        }
        if (!m.valida || !m.empezoPellizco || BotonTocable.DedoCerca)
            return;
        if (ReglaCerca(m.PuntoPellizco, 0.012f))
        {
            manoGiro = 1;
            anguloAntes = AnguloEnDisco(m.PuntoPellizco);
            return;
        }
        if (SobreHoja(m.PuntoPellizco))
        {
            agarrandoHoja = true;
            agarreInicio = agarrePrev = m.PuntoPellizco;
            agarreVel = 0f;
        }
    }

    // ¿Ese punto está sobre una de las reglas negras? (margen en medidas de la mesa)
    bool ReglaCerca(Vector3 mundo, float margen)
    {
        Vector3 l = giratorio.InverseTransformPoint(mundo);
        float yRegla = AltoHoja * 0.5f + 0.008f;
        return Mathf.Abs(l.x) < (AnchoHoja + 0.05f) * 0.5f + margen && Mathf.Abs(Mathf.Abs(l.y) - yRegla) < 0.011f + margen
               && Mathf.Abs(l.z) < 0.03f + margen;
    }

    bool SobreHoja(Vector3 mundo)
    {
        Vector3 l = hoja.InverseTransformPoint(mundo);
        float d = -l.z * escalaMesa;
        return Mathf.Abs(l.x) < 0.5f && Mathf.Abs(l.y) < 0.5f && d < 0.06f && d > -0.03f;
    }

    void ActualizarReglas()
    {
        if (matReglas == null)
            return;
        var der = control.Der;
        bool cerca = manoGiro >= 0 || (der.valida && (ReglaCerca(der.PuntoPellizco, 0.03f) || ReglaCerca(der.indice, 0.03f)));
        float meta = cerca ? 1f : 0f;
        if (Mathf.Abs(brilloReglas - meta) < 0.001f)
            return;
        brilloReglas = Mathf.MoveTowards(brilloReglas, meta, Time.deltaTime * 5f);
        Color c = Color.Lerp(new Color(0.08f, 0.08f, 0.09f), new Color(0.25f, 0.55f, 1f), brilloReglas);
        if (matReglas.HasProperty("_BaseColor"))
            matReglas.SetColor("_BaseColor", c);
        if (matReglas.HasProperty("_Color"))
            matReglas.SetColor("_Color", c);
    }

    void SeguirAgarreHoja(ManoSeguida m)
    {
        if (!m.valida || !m.pellizco)
        {
            agarrandoHoja = false;
            return;
        }
        float dt = Mathf.Max(1e-3f, Time.deltaTime);
        Vector3 p = m.PuntoPellizco;
        Vector3 vel = (p - agarrePrev) / dt;
        agarrePrev = p;
        agarreVel = Mathf.Lerp(agarreVel, vel.magnitude, 1f - Mathf.Exp(-12f * dt));
        if (Vector3.Distance(p, agarreInicio) > 0.12f && agarreVel > 0.9f)
        {
            agarrandoHoja = false;
            BotarHoja(vel);
        }
    }

    void BotarHoja(Vector3 vel)
    {
        if (hojeando)
            PararHojear();
        TerminarLinea();
        SoltarNodo();
        bool hayAlgo = (boceto.actual == 0 && boceto.antiguo) || Pag.lapiz.Count > 0 || Pag.tinta.Count > 0;
        if (!hayAlgo)
            return;
        var tex = new Texture2D(AnchoTex, AltoTex, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(HojaCompleta(boceto.actual));
        tex.Apply(false);
        GuardarDeshacer();
        if (boceto.actual == 0)
            boceto.antiguo = false;
        Pag.lapiz.Clear();
        Pag.tinta.Clear();
        RehacerHoja();
        GuardarBoceto();
        StartCoroutine(HojaVolando(tex, vel));
        textoAyuda.text = Idioma.T("¡Hoja a la basura! (Deshacer la trae de vuelta)");
        control.SonidoBurbuja(hoja.position);
    }

    IEnumerator HojaVolando(Texture2D tex, Vector3 vel)
    {
        var m = new Material(matTransparente);
        PonerTextura(m, tex);
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(go.GetComponent<Collider>());
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Vector3 escala0 = hoja.lossyScale;
        go.transform.SetPositionAndRotation(hoja.position, hoja.rotation);
        go.transform.localScale = escala0;
        Vector3 v = vel.normalized * Mathf.Clamp(vel.magnitude, 1f, 3f);
        Vector3 giro = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
        giro = (giro.sqrMagnitude > 1e-4f ? giro.normalized : Vector3.up) * 540f;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f)
        {
            v += Vector3.down * 2.5f * Time.deltaTime;
            go.transform.position += v * Time.deltaTime;
            go.transform.Rotate(giro * Time.deltaTime, Space.World);
            go.transform.localScale = escala0 * Mathf.Lerp(1f, 0.55f, t);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f - t * t));
            yield return null;
        }
        Destroy(go);
        Destroy(m);
        Destroy(tex);
    }

    // ---------- v63: DESHACER y REHACER en la mesa ----------

    void GuardarDeshacer()
    {
        deshacerMesa.Add(JsonUtility.ToJson(boceto));
        if (deshacerMesa.Count > 40)
            deshacerMesa.RemoveAt(0);
        rehacerMesa.Clear();
    }

    // La flecha de la mano (ControlManos) también llega aquí cuando estás en la mesa.
    public static bool DeshacerRehacer(bool rehacer)
    {
        return Activa && !instancia.animando && instancia.DeshacerMesa(rehacer);
    }

    bool DeshacerMesa(bool rehacer)
    {
        var desde = rehacer ? rehacerMesa : deshacerMesa;
        var hacia = rehacer ? deshacerMesa : rehacerMesa;
        if (desde.Count == 0)
        {
            if (textoAyuda != null)
                textoAyuda.text = Idioma.T(rehacer ? "Nada que rehacer" : "Nada que deshacer");
            return false;
        }
        if (hojeando)
            PararHojear();
        actual = null;
        lineaNodo = null;
        nodoTomado = -1;
        hacia.Add(JsonUtility.ToJson(boceto));
        var g = JsonUtility.FromJson<Guardado>(desde[desde.Count - 1]);
        desde.RemoveAt(desde.Count - 1);
        if (g != null)
            boceto = g;
        Normalizar();
        RehacerHoja();
        GuardarBoceto();
        control.SonidoBurbuja(hoja.position);
        if (textoAyuda != null)
            textoAyuda.text = Idioma.T(rehacer ? "Rehecho" : "Deshecho");
        return true;
    }

    float AnguloEnDisco(Vector3 mundo)
    {
        Vector3 l = raiz.InverseTransformPoint(mundo) - giratorio.localPosition;
        return Mathf.Atan2(l.y, l.x) * Mathf.Rad2Deg;
    }

    // ---------- Botones ----------

    // Deja EN BLANCO la hoja que está en el disco (las otras del bloc se quedan).
    void HojaNueva()
    {
        if (hojeando)
            PararHojear();
        TerminarLinea();
        GuardarDeshacer();
        Pag.lapiz.Clear();
        Pag.tinta.Clear();
        if (boceto.actual == 0)
            boceto.antiguo = false;
        tintaPuesta = false;
        RehacerHoja();
        GuardarBoceto();
        control.SonidoBurbuja(hoja.position);
    }

    void AlternarCebolla()
    {
        cebollaMesa = !cebollaMesa;
        PlayerPrefs.SetInt(ClaveCebolla, cebollaMesa ? 1 : 0);
        PlayerPrefs.Save();
        PonerTextoCebolla();
        if (!hojeando)
            RehacerHoja();
        control.SonidoBurbuja(btnCebolla.transform.position);
    }

    void PonerTextoCebolla()
    {
        if (btnCebolla == null)
            return;
        Idioma.Poner(btnCebolla.etiqueta, cebollaMesa ? "Cebolla: sí" : "Cebolla: no");
        btnCebolla.Marcar(cebollaMesa);
    }

    // v64: DIBUJAR (lápiz) o ENTINTAR (la hoja de tinta encima). Solo se ven las herramientas del modo; la goma, en los dos.
    void PonerModoTinta(bool tinta)
    {
        TerminarLinea();
        SoltarNodo();
        tintaPuesta = tinta;
        hojaTinta.gameObject.SetActive(tinta);
        if (tinta && herramientaActual <= 2)
            herramientaActual = tintaPintada ? 5 : 4;
        else if (!tinta && herramientaActual >= 4)
            herramientaActual = colorLapiz == Goma ? 0 : colorLapiz;
        if (herramientaActual != Goma && colorLapiz == Goma)
            colorLapiz = 0;
        ActualizarModoUI();
    }

    void ActualizarModoUI()
    {
        if (tituloModo == null || herramientas.Count < 6)
            return;
        bool t = tintaPuesta;
        for (int i = 0; i < herramientas.Count; i++)
        {
            bool ver = i == Goma || (i <= 2 ? !t : t);
            if (herramientas[i].gameObject.activeSelf != ver)
                herramientas[i].gameObject.SetActive(ver);
        }
        if (frasco != null && frasco.gameObject.activeSelf != t)
            frasco.gameObject.SetActive(t);
        tituloModo.text = Idioma.T(t ? "Entintando" : "Dibujando");
        Idioma.Poner(btnModo.etiqueta, t ? "Dibujar" : "Entintar");
        AcomodarHerramientas();
    }

    // El botón de abajo: pasa al otro modo. Al pasar a DIBUJAR, el dibujo de muestra se va (la hoja queda en blanco).
    void CambiarModo()
    {
        if (hojeando)
            PararHojear();
        if (tintaPuesta)
        {
            if (boceto.actual == 0 && boceto.antiguo)
            {
                GuardarDeshacer();
                boceto.antiguo = false;
                GuardarBoceto();
            }
            PonerModoTinta(false);
            RehacerHoja();
        }
        else
        {
            PonerModoTinta(true);
        }
        if (textoAyuda != null)
            textoAyuda.text = Idioma.T(tintaPuesta ? "Entintando: rapidógrafo (líneas) o plumilla (pintada)" : "Dibujando con lápiz");
        control.SonidoBurbuja(btnModo.transform.position);
    }

    void CerrarApp()
    {
        if (cerrarAppHasta <= 0f)
        {
            cerrarAppHasta = Time.time + 3f;
            btnCerrarApp.etiqueta.text = Idioma.T("¿Cerrar? Toca otra vez");
            return;
        }
        GuardarBoceto();
        if (verArchivo && dibujo != null)
        {
            dibujo.gameObject.SetActive(true);
            if (dibujo.HayArchivoAbierto)
                dibujo.Guardar();
        }
        Application.Quit();
    }

    // ---------- Las hojas recientes ----------

    void LlenarRecientes()
    {
        rutasRecientes.Clear();
        foreach (var t in texturasMini)
            if (t != null)
                Destroy(t);
        texturasMini.Clear();
        // v64: la PRIMERA hoja de arriba es "+ Archivo nuevo"; después, tus 6 dibujos más recientes.
        rutasRecientes.AddRange(dibujo.ListaArchivos(Recientes - 1));
        botonesRecientes[0].gameObject.SetActive(true);
        minis[0].gameObject.SetActive(false);
        nombresRecientes[0].gameObject.SetActive(true);
        botonesRecientes[0].etiqueta.text = "+";
        botonesRecientes[0].etiqueta.fontSizeMax = 0.6f;
        nombresRecientes[0].text = Idioma.T("Archivo nuevo");
        for (int i = 1; i < Recientes; i++)
        {
            int r = i - 1;   // su dibujo en la lista
            bool hay = r < rutasRecientes.Count;
            botonesRecientes[i].gameObject.SetActive(hay);
            minis[i].gameObject.SetActive(hay);
            nombresRecientes[i].gameObject.SetActive(hay);
            if (!hay)
                continue;
            nombresRecientes[i].text = Path.GetFileNameWithoutExtension(rutasRecientes[r]);
            Texture2D tex = null;
            try
            {
                string mini = dibujo.RutaMiniatura(rutasRecientes[r]);
                if (File.Exists(mini))
                {
                    tex = new Texture2D(2, 2);
                    if (!tex.LoadImage(File.ReadAllBytes(mini)))
                    {
                        Destroy(tex);
                        tex = null;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TrazoVR: miniatura: " + e.Message);
            }
            if (tex != null)
                texturasMini.Add(tex);
            PonerTextura(minis[i].sharedMaterial, tex != null ? tex : Texture2D.whiteTexture);
        }
        textoAyuda.text = Idioma.T(rutasRecientes.Count > 0
            ? "Toca una hoja de arriba para abrirla · dibuja con el índice en la hoja · pellizca el disco para girarlo · ✦ Entrar = a tu boceto"
            : "Dibuja con el índice en la hoja · pellizca el disco para girarlo · ✦ Entrar = a tu boceto");
    }

    static void PonerTextura(Material m, Texture t)
    {
        if (m == null)
            return;
        if (m.HasProperty("_BaseMap"))
            m.SetTexture("_BaseMap", t);
        if (m.HasProperty("_MainTex"))
            m.SetTexture("_MainTex", t);
    }

    // ---------- Armar la mesa (una vez) ----------

    bool Armar()
    {
        if (armada)
            return true;
        var refs = FindFirstObjectByType<Referencias>(FindObjectsInactive.Include);
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (refs == null || refs.materialImagen == null || nav == null)
            return false;
        matImagen = refs.materialImagen;
        matTransparente = refs.materialImagenTransparente != null ? refs.materialImagenTransparente : refs.materialImagen;
        matColor = nav.materialBoton;
        Material normal = nav.materialBoton, marcado = nav.materialBotonMarcado;
        raiz = new GameObject("Mesa").transform;
        raiz.SetParent(transform, false);
        escalaMesa = Mathf.Clamp(PlayerPrefs.GetFloat(ClaveEscala, 1f), EscalaMin, EscalaMax);
        desplazaMesa = new Vector3(PlayerPrefs.GetFloat(ClaveDX, 0f), PlayerPrefs.GetFloat(ClaveDY, 0f), PlayerPrefs.GetFloat(ClaveDZ, 0f));
        cebollaMesa = PlayerPrefs.GetInt(ClaveCebolla, 1) == 1;
        tintaPintada = PlayerPrefs.GetInt(ClaveTintaPintada, 0) == 1;

        // El tablero de madera (su cara de arriba en z = 0; lo de encima va hacia -z).
        tablero = Pieza(PrimitiveType.Cube, new Vector3(0f, 0f, 0.012f), new Vector3(0.84f, 0.6f, 0.024f), Quaternion.identity, Textura(Madera()));
        // La orilla de aluminio de abajo (para que no se caigan los lápices).
        orilla = Pieza(PrimitiveType.Cube, new Vector3(0f, -0.302f, -0.006f), new Vector3(0.84f, 0.012f, 0.03f), Quaternion.identity, Pintura(new Color(0.75f, 0.76f, 0.78f)));

        // El disco (gira) con la luz por debajo de la hoja.
        giratorio = new GameObject("Disco").transform;
        giratorio.SetParent(raiz, false);
        giratorio.localPosition = new Vector3(0f, -0.065f, 0f);
        var giroDisco = Quaternion.Euler(90f, 0f, 0f);
        Pieza(PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.002f), new Vector3(RadioDisco * 2f, 0.002f, RadioDisco * 2f), giroDisco,
              Pintura(new Color(0.16f, 0.16f, 0.18f)), giratorio);
        Pieza(PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.0042f), new Vector3(RadioDisco * 1.62f, 0.0003f, RadioDisco * 1.62f), giroDisco,
              Pintura(new Color(1f, 0.99f, 0.94f)), giratorio);
        // La hoja (lápiz) y la hoja de tinta encima.
        texHoja = new Texture2D(AnchoTex, AltoTex, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        texTinta = new Texture2D(AnchoTex, AltoTex, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        pxHoja = new Color32[AnchoTex * AltoTex];
        pxBase = new Color32[AnchoTex * AltoTex];
        pxTinta = new Color32[AnchoTex * AltoTex];
        var mHoja = new Material(matImagen);
        PonerTextura(mHoja, texHoja);
        hoja = Pieza(PrimitiveType.Quad, new Vector3(0f, 0f, -0.0055f), new Vector3(AnchoHoja, AltoHoja, 1f), Quaternion.identity, mHoja, giratorio);
        matHojaNormal = mHoja;
        var mTinta = new Material(matTransparente);
        if (mTinta.HasProperty("_BaseColor"))
            mTinta.SetColor("_BaseColor", UnityEngine.Color.white);
        PonerTextura(mTinta, texTinta);
        hojaTinta = Pieza(PrimitiveType.Quad, new Vector3(0f, 0f, -0.0068f), new Vector3(AnchoHoja, AltoHoja, 1f), Quaternion.identity, mTinta, giratorio);
        hojaTinta.gameObject.SetActive(false);
        // Las reglas negras con pivotes (sujetan la hoja).
        var negro = Pintura(new Color(0.08f, 0.08f, 0.09f));
        matReglas = negro;   // v63: solo las reglas lo usan (brillan cuando tu mano derecha se acerca)
        var metal = Pintura(new Color(0.55f, 0.56f, 0.6f));
        for (int s = -1; s <= 1; s += 2)
        {
            float y = s * (AltoHoja * 0.5f + 0.008f);
            Pieza(PrimitiveType.Cube, new Vector3(0f, y, -0.008f), new Vector3(AnchoHoja + 0.05f, 0.022f, 0.006f), Quaternion.identity, negro, giratorio);
        }
        // v61: los PIVOTES abajo, justo en las perforaciones de la hoja (uno redondo al centro y dos planos a los lados).
        float yPivote = (AlturaHoyos - 0.5f) * AltoHoja;
        pivotes.Add(Pieza(PrimitiveType.Cylinder, new Vector3(0f, yPivote, -0.0105f), new Vector3(0.0055f, 0.003f, 0.0055f), giroDisco, metal, giratorio));
        for (int s = -1; s <= 1; s += 2)
            pivotes.Add(Pieza(PrimitiveType.Cube, new Vector3(s * SeparacionRanura * AnchoHoja, yPivote, -0.0105f), new Vector3(0.0165f, 0.0035f, 0.006f),
                              Quaternion.identity, metal, giratorio));

        // v63: la TINTA EN LÍNEAS: una malla de verdad encima del papel de calcar (nítida con cualquier zoom), y sus nodos.
        var matLineas = new Material(matImagen);
        PonerTextura(matLineas, Texture2D.whiteTexture);
        if (matLineas.HasProperty("_BaseColor"))
            matLineas.SetColor("_BaseColor", new Color(0.04f, 0.04f, 0.06f));
        mallaTinta = new Mesh { name = "TintaMesa", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        HijoMalla("TintaLineas", mallaTinta, matLineas, -0.0006f);
        var matNodos = new Material(matImagen);
        PonerTextura(matNodos, Texture2D.whiteTexture);
        if (matNodos.HasProperty("_BaseColor"))
            matNodos.SetColor("_BaseColor", new Color(0.2f, 0.45f, 1f));
        mallaNodos = new Mesh { name = "NodosMesa" };
        HijoMalla("TintaNodos", mallaNodos, matNodos, -0.0009f);

        // v64: EL MODO (a la derecha): un rectángulo dibujado sobre la mesa con el título y sus herramientas (acostadas);
        // debajo, fuera del rectángulo, el botón para pasar al otro modo.
        var trazoMesa = Pintura(new Color(0.32f, 0.21f, 0.1f));
        const float rx0 = 0.255f, rx1 = 0.425f, ry0 = -0.035f, ry1 = 0.125f;
        Pieza(PrimitiveType.Cube, new Vector3((rx0 + rx1) * 0.5f, ry1, -0.0004f), new Vector3(rx1 - rx0, 0.0022f, 0.0008f), Quaternion.identity, trazoMesa);
        Pieza(PrimitiveType.Cube, new Vector3((rx0 + rx1) * 0.5f, ry0, -0.0004f), new Vector3(rx1 - rx0, 0.0022f, 0.0008f), Quaternion.identity, trazoMesa);
        Pieza(PrimitiveType.Cube, new Vector3(rx0, (ry0 + ry1) * 0.5f, -0.0004f), new Vector3(0.0022f, ry1 - ry0, 0.0008f), Quaternion.identity, trazoMesa);
        Pieza(PrimitiveType.Cube, new Vector3(rx1, (ry0 + ry1) * 0.5f, -0.0004f), new Vector3(0.0022f, ry1 - ry0, 0.0008f), Quaternion.identity, trazoMesa);
        tituloModo = PanelTutoriales.Texto(raiz, "", new Vector3(0.34f, 0.105f, -0.002f), new Vector2(0.16f, 0.028f), 0.32f, null);
        tituloModo.fontStyle = FontStyles.Bold;
        tituloModo.color = new Color(0.23f, 0.15f, 0.07f);
        btnModo = PanelTutoriales.Boton(raiz, "", new Vector3(0.34f, -0.06f, -0.004f), new Vector2(0.12f, 0.026f),
                                        Pintura(new Color(0.78f, 0.79f, 0.82f)), marcado);
        btnModo.alTocar.AddListener(CambiarModo);

        // LAS HERRAMIENTAS (toca una; la elegida se levanta y brilla). Lápices de verdad, acostados: cuerpo (amarillo
        // el de grafito), la madera tallada y la punta a la izquierda; el de grafito con su casquillo y su gomita.
        var madera = Pintura(new Color(0.85f, 0.72f, 0.52f));
        var mina = Pintura(new Color(0.16f, 0.16f, 0.18f));
        var acostado = Quaternion.Euler(0f, 0f, 90f);
        for (int k = 0; k < 3; k++)
        {
            var h = NuevaHerramienta(new Vector3(0.35f, 0.075f - k * 0.02f, -0.008f));
            var cuerpo = Pintura(k == 0 ? new Color(0.96f, 0.76f, 0.2f) : ColoresLapiz[k]);
            BotonHerramienta(h, new Vector2(0.1f, 0.009f), cuerpo);
            Pieza(PrimitiveType.Cylinder, new Vector3(-0.056f, 0f, 0f), new Vector3(0.0075f, 0.006f, 0.0075f), acostado, madera, h);
            Pieza(PrimitiveType.Cylinder, new Vector3(-0.0635f, 0f, 0f), new Vector3(0.003f, 0.0028f, 0.003f), acostado, k == 0 ? mina : cuerpo, h);
            if (k == 0)
            {
                Pieza(PrimitiveType.Cube, new Vector3(0.0525f, 0f, 0f), new Vector3(0.006f, 0.0098f, 0.0092f), Quaternion.identity, metal, h);
                Pieza(PrimitiveType.Cube, new Vector3(0.059f, 0f, 0f), new Vector3(0.007f, 0.009f, 0.0088f), Quaternion.identity,
                      Pintura(new Color(0.95f, 0.55f, 0.6f)), h);
            }
        }
        // La goma (rosada, acostada): está en los dos modos (borra lápiz y tinta).
        var hGoma = NuevaHerramienta(new Vector3(0.33f, 0.008f, -0.008f));
        BotonHerramienta(hGoma, new Vector2(0.045f, 0.016f), Pintura(ColoresLapiz[Goma]));
        // El RAPIDÓGRAFO: cuerpo negro mate, anillo plateado y su aguja de metal = tinta en líneas.
        var hRapido = NuevaHerramienta(new Vector3(0.35f, 0.072f, -0.008f));
        BotonHerramienta(hRapido, new Vector2(0.085f, 0.01f), Pintura(new Color(0.07f, 0.07f, 0.08f)));
        Pieza(PrimitiveType.Cube, new Vector3(-0.028f, 0f, 0f), new Vector3(0.006f, 0.0115f, 0.0105f), Quaternion.identity, metal, hRapido);
        Pieza(PrimitiveType.Cube, new Vector3(-0.047f, 0f, 0f), new Vector3(0.01f, 0.006f, 0.006f), Quaternion.identity, metal, hRapido);
        Pieza(PrimitiveType.Cube, new Vector3(-0.058f, 0f, 0f), new Vector3(0.012f, 0.0012f, 0.0012f), Quaternion.identity, metal, hRapido);
        // La PLUMILLA de tinta china: mango de madera oscura, casquillo y la plumilla dorada = tinta pintada.
        var hPluma = NuevaHerramienta(new Vector3(0.35f, 0.045f, -0.008f));
        BotonHerramienta(hPluma, new Vector2(0.08f, 0.009f), Pintura(new Color(0.36f, 0.2f, 0.1f)));
        Pieza(PrimitiveType.Cube, new Vector3(-0.044f, 0f, 0f), new Vector3(0.009f, 0.0095f, 0.009f), Quaternion.identity, metal, hPluma);
        Pieza(PrimitiveType.Cube, new Vector3(-0.056f, 0f, 0f), new Vector3(0.016f, 0.005f, 0.002f), Quaternion.identity,
              Pintura(new Color(0.88f, 0.68f, 0.22f)), hPluma);
        // Su frasquito de tinta china (solo en modo Entintar).
        frasco = Pieza(PrimitiveType.Cylinder, new Vector3(0.405f, 0.01f, -0.01f), new Vector3(0.015f, 0.009f, 0.015f), giroDisco,
                       Pintura(new Color(0.05f, 0.05f, 0.08f)));
        // El brillito de la elegida.
        var matBrillo = new Material(matTransparente);
        if (matBrillo.HasProperty("_BaseColor"))
            matBrillo.SetColor("_BaseColor", new Color(0.55f, 0.85f, 1f, 0.45f));
        brilloHerramienta = Pieza(PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.01f, Quaternion.identity, matBrillo);
        colorLapiz = 0;
        herramientaActual = 0;
        AcomodarHerramientas();

        // v63: DESHACER y REHACER (también con la flecha de la mano, como siempre).
        var btnDeshacer = PanelTutoriales.Boton(raiz, "", new Vector3(-0.373f, 0.118f, -0.004f), new Vector2(0.052f, 0.018f), normal, marcado);
        Idioma.Poner(btnDeshacer.etiqueta, "Deshacer");
        btnDeshacer.alTocar.AddListener(() => DeshacerMesa(false));
        var btnRehacer = PanelTutoriales.Boton(raiz, "", new Vector3(-0.287f, 0.118f, -0.004f), new Vector2(0.052f, 0.018f), normal, marcado);
        Idioma.Poner(btnRehacer.etiqueta, "Rehacer");
        btnRehacer.alTocar.AddListener(() => DeshacerMesa(true));

        // v60: EL BLOC (izquierda): más hojas para animar, con papel cebolla, y "Hojear" para verlas seguidas.
        Pieza(PrimitiveType.Cube, new Vector3(-0.33f, 0.06f, -0.006f), new Vector3(0.11f, 0.075f, 0.012f), Quaternion.identity,
              Pintura(new Color(0.97f, 0.95f, 0.9f)));
        for (int k = 1; k <= 3; k++)
            Pieza(PrimitiveType.Cube, new Vector3(-0.33f, 0.06f - 0.0375f - 0.0012f * k, -0.006f + 0.002f * k), new Vector3(0.11f, 0.0008f, 0.002f),
                  Quaternion.identity, Pintura(new Color(0.8f, 0.78f, 0.74f)));
        textoBloc = PanelTutoriales.Texto(raiz, "", new Vector3(-0.33f, 0.07f, -0.0125f), new Vector2(0.1f, 0.014f), 0.09f, null);
        textoBloc.color = new Color(0.3f, 0.22f, 0.15f);
        var antes = PanelTutoriales.Boton(raiz, "◀", new Vector3(-0.375f, 0.005f, -0.004f), new Vector2(0.03f, 0.022f), normal, marcado);
        antes.alTocar.AddListener(() => CambiarHoja(-1));
        var mas = PanelTutoriales.Boton(raiz, "", new Vector3(-0.33f, 0.005f, -0.004f), new Vector2(0.048f, 0.022f), normal, marcado);
        Idioma.Poner(mas.etiqueta, "+ Hoja");
        mas.alTocar.AddListener(AgregarHoja);
        var despues = PanelTutoriales.Boton(raiz, "▶", new Vector3(-0.285f, 0.005f, -0.004f), new Vector2(0.03f, 0.022f), normal, marcado);
        despues.alTocar.AddListener(() => CambiarHoja(1));
        btnHojear = PanelTutoriales.Boton(raiz, "", new Vector3(-0.33f, -0.03f, -0.004f), new Vector2(0.11f, 0.024f), normal, marcado);
        btnHojear.etiqueta.text = Idioma.T("▶ Reproducir");
        btnHojear.alTocar.AddListener(AlternarHojear);
        // v62: el papel cebolla del bloc, encendido o apagado.
        btnCebolla = PanelTutoriales.Boton(raiz, "", new Vector3(-0.33f, -0.062f, -0.004f), new Vector2(0.11f, 0.022f), normal, marcado);
        btnCebolla.alTocar.AddListener(AlternarCebolla);
        PonerTextoCebolla();

        // v62: la flecha que te dice dónde quedó la mesa (fuera de la mesa, para verla siempre).
        flecha = new GameObject("FlechaMesa").transform;
        flecha.SetParent(transform, false);
        puntaFlecha = PanelTutoriales.Texto(flecha, "▶", Vector3.zero, new Vector2(0.05f, 0.05f), 0.4f, null).transform;
        var nombreFlecha = PanelTutoriales.Texto(flecha, "", new Vector3(0f, -0.035f, 0f), new Vector2(0.1f, 0.02f), 0.12f, null);
        Idioma.Poner(nombreFlecha, "Tu mesa");
        var colorFlecha = new Color(0.2f, 0.45f, 1f);
        puntaFlecha.GetComponent<TextMeshPro>().color = colorFlecha;
        nombreFlecha.color = colorFlecha;
        flecha.gameObject.SetActive(false);

        // v60: "Centrar mesa": vuelve a su tamaño y lugar (frente a ti).
        var centrar = PanelTutoriales.Boton(raiz, "", new Vector3(0.355f, 0.15f, -0.004f), new Vector2(0.08f, 0.02f), normal, marcado);
        Idioma.Poner(centrar.etiqueta, "Centrar mesa");
        centrar.alTocar.AddListener(CentrarMesa);

        // Arriba: tus dibujos recientes (hojas con su miniatura y su nombre).
        for (int i = 0; i < Recientes; i++)
        {
            float x = -0.345f + i * 0.115f;
            int k = i;
            var b = PanelTutoriales.Boton(raiz, "", new Vector3(x, 0.235f, -0.004f), new Vector2(0.1f, 0.075f), normal, marcado);
            b.alTocar.AddListener(() => AbrirReciente(k));
            botonesRecientes.Add(b);
            var mini = Pieza(PrimitiveType.Quad, new Vector3(x, 0.237f, -0.0085f), new Vector3(0.09f, 0.064f, 1f), Quaternion.identity, new Material(matImagen));
            minis.Add(mini.GetComponent<Renderer>());
            var n = PanelTutoriales.Texto(raiz, "", new Vector3(x, 0.188f, -0.002f), new Vector2(0.105f, 0.012f), 0.09f, null);
            n.color = new Color(0.22f, 0.13f, 0.06f);
            nombresRecientes.Add(n);
        }

        // Botones: entrar, hoja nueva, hoja de tinta, cerrar app.
        var entrar = PanelTutoriales.Boton(raiz, "", new Vector3(0f, -0.272f, -0.004f), new Vector2(0.16f, 0.026f), marcado, normal);
        Idioma.Poner(entrar.etiqueta, "✦ Entrar");
        entrar.etiqueta.color = UnityEngine.Color.white;
        entrar.textoNormal = UnityEngine.Color.white;
        entrar.alTocar.AddListener(() =>
        {
            if (animando)
                return;
            if (verArchivo)
                TerminarVer();
            else
                EntrarABoceto();
        });
        btnEntrar = entrar;
        // v64: "Hoja nueva" va a la izquierda, donde estaba "Hoja de tinta" (el modo ahora tiene su propio botón).
        var nueva = PanelTutoriales.Boton(raiz, "", new Vector3(-0.33f, -0.16f, -0.004f), new Vector2(0.12f, 0.03f), normal, marcado);
        Idioma.Poner(nueva.etiqueta, "Hoja nueva");
        nueva.alTocar.AddListener(HojaNueva);
        btnCerrarApp = PanelTutoriales.Boton(raiz, "", new Vector3(0.355f, -0.272f, -0.004f), new Vector2(0.09f, 0.02f), normal, marcado);
        btnCerrarApp.etiqueta.text = Idioma.T("Cerrar app");
        btnCerrarApp.alTocar.AddListener(CerrarApp);
        textoAyuda = PanelTutoriales.Texto(raiz, "", new Vector3(-0.25f, -0.272f, -0.002f), new Vector2(0.28f, 0.02f), 0.07f, null);
        textoAyuda.color = new Color(0.25f, 0.15f, 0.07f);
        var titulo = PanelTutoriales.Texto(raiz, "JCartoons", new Vector3(-0.33f, 0.15f, -0.002f), new Vector2(0.14f, 0.02f), 0.16f, null);
        titulo.color = new Color(0.35f, 0.2f, 0.08f);
        titulo.fontStyle = FontStyles.Italic | FontStyles.Bold;

        CargarBoceto();
        RehacerHoja();
        raiz.gameObject.SetActive(false);
        armada = true;
        return true;
    }

    Transform Pieza(PrimitiveType tipo, Vector3 pos, Vector3 escala, Quaternion giro, Material m, Transform padre = null)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(padre != null ? padre : raiz, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = giro;
        go.transform.localScale = escala;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    Material Pintura(Color c)
    {
        var m = new Material(matColor);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        return m;
    }

    Material Textura(Texture2D t)
    {
        var m = new Material(matImagen);
        PonerTextura(m, t);
        return m;
    }

    // Vetas de madera clara (como el tablero de la foto).
    static Texture2D Madera()
    {
        const int w = 256, h = 192;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        var claro = new Color(0.86f, 0.6f, 0.34f);
        var oscuro = new Color(0.74f, 0.47f, 0.24f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float veta = Mathf.PerlinNoise(x * 0.012f, y * 0.11f) * 0.7f + Mathf.PerlinNoise(x * 0.05f, y * 0.4f) * 0.3f;
                float anillo = 0.5f + 0.5f * Mathf.Sin(veta * 18f);
                px[y * w + x] = UnityEngine.Color.Lerp(oscuro, claro, 0.35f + 0.65f * anillo * anillo);
            }
        t.SetPixels32(px);
        t.Apply(true);
        return t;
    }

    // ---------- La hoja: papel, perforaciones, número, dibujo de muestra y lo tuyo ----------

    void RehacerHoja()
    {
        Normalizar();
        // v61: el papel con sus perforaciones ABAJO, como el papel de animación de los estudios (Acme): un hoyo
        // redondo en el centro y dos ranuras a los lados (se hace una vez).
        if (pxPapel == null)
        {
            pxPapel = new Color32[AnchoTex * AltoTex];
            for (int i = 0; i < pxPapel.Length; i++)
                pxPapel[i] = ColorPapel;
            var hueco = new Color(0.62f, 0.62f, 0.6f);
            Sello(pxPapel, 0.5f * AnchoTex, AlturaHoyos * AltoTex, 8f, hueco, 1f, false);
            foreach (float hx in new[] { 0.5f - SeparacionRanura, 0.5f + SeparacionRanura })
                for (float d = -22f; d <= 22f; d += 1f)
                    Sello(pxPapel, hx * AnchoTex + d, AlturaHoyos * AltoTex, 5f, hueco, 1f, false);
        }
        if (pxFondo == null)
            pxFondo = new Color32[AnchoTex * AltoTex];
        // El fondo: papel + su número (en círculo si es clave) + la hoja anterior en papel cebolla. La goma vuelve a esto.
        System.Array.Copy(pxPapel, pxFondo, pxFondo.Length);
        NumeroPagina(pxFondo, boceto.actual + 1, Pag.clave);
        if (boceto.actual > 0 && cebollaMesa)
            Cebolla(pxFondo, boceto.paginas[boceto.actual - 1]);
        System.Array.Copy(pxFondo, pxBase, pxBase.Length);
        // El dibujo de muestra solo en la primera hoja (se borra con la goma o con "Hoja nueva").
        if (boceto.actual == 0 && boceto.antiguo)
            PonerMuestra(pxBase);
        System.Array.Copy(pxBase, pxHoja, pxBase.Length);
        fondoGoma = pxFondo;
        foreach (var l in Pag.lapiz)
            RepintarLinea(l, false);
        RehacerTinta();   // papel de calcar (deja ver el lápiz, un poco más claro) y la tinta encima
        if (textoBloc != null)
            textoBloc.text = Idioma.T("Hoja") + " " + (boceto.actual + 1) + " / " + boceto.paginas.Count;
        hojaTinta.gameObject.SetActive(tintaPuesta);
        if (!tintaPuesta && herramientaActual >= 4)
            herramientaActual = colorLapiz == Goma ? 0 : colorLapiz;
        if (tintaPuesta && herramientaActual <= 2)
            herramientaActual = tintaPintada ? 5 : 4;
        ActualizarModoUI();
        sucioHoja = sucioTinta = true;
    }

    void RepintarLinea(Linea l, bool tinta)
    {
        if (l.p.Count == 0)
            return;
        if (tinta && l.vector && !tintaRaster)
        {
            mallaSucia = true;   // v63: en la mesa va en su malla (nítida); en imagen solo para hojear y entrar
            return;
        }
        if (tinta && l.vector && l.p.Count >= 2)
        {
            // v62: la tinta en líneas: lisa, pasando por sus nodos.
            var suave = Suave(l.p);
            Trazar(suave[0], suave[0], true, l.c);
            for (int i = 1; i < suave.Count; i++)
                Trazar(suave[i - 1], suave[i], true, l.c);
            return;
        }
        Trazar(l.p[0], l.p[0], tinta, l.c);
        for (int i = 1; i < l.p.Count; i++)
            Trazar(l.p[i - 1], l.p[i], tinta, l.c);
    }

    // v60: PAPEL CEBOLLA del bloc: la hoja anterior se ve clarita y rojiza debajo (como en la mesa de verdad).
    static void Cebolla(Color32[] px, Pagina p)
    {
        var tenue = new Color(0.95f, 0.55f, 0.5f);
        foreach (var l in p.lapiz)
            if (l.c != Goma)
                PintarSuave(px, l, tenue, 0.35f);
        foreach (var l in p.tinta)
            if (l.c != Goma)
                PintarSuave(px, l, tenue, 0.45f);
    }

    static void PintarSuave(Color32[] px, Linea l, Color color, float fuerza)
    {
        for (int i = 0; i < l.p.Count; i++)
        {
            Vector3 a = l.p[Mathf.Max(0, i - 1)], b = l.p[i];
            float ax = a.x * AnchoTex, ay = a.y * AltoTex, bx = b.x * AnchoTex, by = b.y * AltoTex;
            int pasos = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay)) / 1.2f));
            for (int k = 0; k <= pasos; k++)
            {
                float t = k / (float)pasos;
                Sello(px, Mathf.Lerp(ax, bx, t), Mathf.Lerp(ay, by, t), 1.6f, color, fuerza, false);
            }
        }
    }

    // Una hoja entera ya pintada (papel + número + muestra + su lápiz + su tinta encima), para hojear y para entrar.
    Color32[] HojaCompleta(int indice)
    {
        var pag = boceto.paginas[indice];
        var fondo = new Color32[AnchoTex * AltoTex];
        System.Array.Copy(pxPapel, fondo, fondo.Length);
        NumeroPagina(fondo, indice + 1, pag.clave);
        var px = new Color32[AnchoTex * AltoTex];
        System.Array.Copy(fondo, px, px.Length);
        if (indice == 0 && boceto.antiguo)
            PonerMuestra(px);
        var guardaHoja = pxHoja;
        var guardaGoma = fondoGoma;
        pxHoja = px;
        fondoGoma = fondo;
        foreach (var l in pag.lapiz)
            RepintarLinea(l, false);
        pxHoja = guardaHoja;
        fondoGoma = guardaGoma;
        if (pag.tinta.Count > 0)
        {
            // La tinta en una capa aparte (así la goma de la tinta funciona igual) y luego encima del papel.
            var capa = new Color32[AnchoTex * AltoTex];
            var vacio = new Color32(255, 255, 255, 0);
            for (int i = 0; i < capa.Length; i++)
                capa[i] = vacio;
            var guardaTinta = pxTinta;
            var guardaVacio = vacioTinta;
            pxTinta = capa;
            vacioTinta = vacio;
            tintaRaster = true;
            foreach (var l in pag.tinta)
                RepintarLinea(l, true);
            tintaRaster = false;
            pxTinta = guardaTinta;
            vacioTinta = guardaVacio;
            for (int i = 0; i < px.Length; i++)
            {
                Color32 c = capa[i];
                if (c.a == 0)
                    continue;
                float a = c.a / 255f;
                Color32 p = px[i];
                px[i] = new Color32((byte)Mathf.Lerp(p.r, c.r, a), (byte)Mathf.Lerp(p.g, c.g, a), (byte)Mathf.Lerp(p.b, c.b, a), 255);
            }
        }
        return px;
    }

    // ---------- v61: los DIBUJOS DE MUESTRA (Resources/BocetosMesa/boceto1, 2, 3...: imágenes en gris) ----------

    static int cantidadMuestras = -1;

    static int CantidadMuestras()
    {
        if (cantidadMuestras >= 0)
            return cantidadMuestras;
        cantidadMuestras = 0;
        for (int n = 1; n <= 30; n++)
        {
            var ta = Resources.Load<TextAsset>("BocetosMesa/boceto" + n);
            if (ta == null)
                break;
            Resources.UnloadAsset(ta);
            cantidadMuestras = n;
        }
        return cantidadMuestras;
    }

    // Cada vez que vuelves a la mesa con la hoja sin tocar, sale OTRO dibujo de muestra (al azar).
    void QuizasOtraMuestra()
    {
        Normalizar();
        if (boceto.paginas.Count != 1 || boceto.paginas[0].lapiz.Count > 0 || boceto.paginas[0].tinta.Count > 0)
            return;
        int n = CantidadMuestras();
        if (n == 0)
            return;
        int nueva = UnityEngine.Random.Range(0, n);
        if (n > 1 && nueva == boceto.muestra)
            nueva = (nueva + 1 + UnityEngine.Random.Range(0, n - 1)) % n;
        boceto.muestra = nueva;
        boceto.antiguo = true;
        boceto.actual = 0;
        tintaPuesta = true;   // v64: con un dibujo de muestra, el entintado ya viene puesto
        GuardarBoceto();
        RehacerHoja();
    }

    // Lee la imagen de la muestra y la acomoda en la hoja (una vez por muestra): 0 = papel, 255 = línea.
    bool PrepararMuestra()
    {
        int n = CantidadMuestras();
        if (n == 0)
            return false;
        if (boceto.muestra < 0 || boceto.muestra >= n)
            boceto.muestra = UnityEngine.Random.Range(0, n);
        if (muestraPx != null && muestraCargada == boceto.muestra)
            return true;
        var ta = Resources.Load<TextAsset>("BocetosMesa/boceto" + (boceto.muestra + 1));
        if (ta == null)
            return false;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        bool ok = tex.LoadImage(ta.bytes);
        Resources.UnloadAsset(ta);
        if (!ok || tex.width < 2 || tex.height < 2)
        {
            Destroy(tex);
            return false;
        }
        Color32[] src = tex.GetPixels32();
        int w = tex.width, h = tex.height;
        Destroy(tex);
        // La zona del dibujo en la hoja: deja libre arriba (el número) y abajo (las perforaciones).
        const float x0 = 24f, x1 = 776f, y0 = 62f, y1 = 540f;
        float k = Mathf.Min((x1 - x0) / w, (y1 - y0) / h);
        float ox = (x0 + x1 - w * k) * 0.5f, oy = (y0 + y1 - h * k) * 0.5f;
        muestraPx = new byte[AnchoTex * AltoTex];
        for (int y = 0; y < AltoTex; y++)
        {
            float sy = (y - oy) / k;
            if (sy < 0f || sy > h - 1)
                continue;
            int iy = Mathf.Min((int)sy, h - 2);
            float fy = sy - iy;
            for (int x = 0; x < AnchoTex; x++)
            {
                float sx = (x - ox) / k;
                if (sx < 0f || sx > w - 1)
                    continue;
                int ix = Mathf.Min((int)sx, w - 2);
                float fx = sx - ix;
                int i0 = iy * w + ix, i1 = i0 + w;
                float g = Mathf.Lerp(Mathf.Lerp(src[i0].r, src[i0 + 1].r, fx), Mathf.Lerp(src[i1].r, src[i1 + 1].r, fx), fy);
                muestraPx[y * AnchoTex + x] = (byte)Mathf.Clamp(255f - g, 0f, 255f);
            }
        }
        muestraCargada = boceto.muestra;
        return true;
    }

    void PonerMuestra(Color32[] px)
    {
        if (!PrepararMuestra())
            return;
        for (int i = 0; i < px.Length; i++)
        {
            int d = muestraPx[i];
            if (d == 0)
                continue;
            float a = d / 255f * 0.88f;
            Color32 c = px[i];
            px[i] = new Color32((byte)Mathf.Lerp(c.r, ColorGrafito.r * 255f, a), (byte)Mathf.Lerp(c.g, ColorGrafito.g * 255f, a),
                                (byte)Mathf.Lerp(c.b, ColorGrafito.b * 255f, a), c.a);
        }
    }

    // ---------- v61: el NÚMERO de la hoja, escrito a mano (pequeño, arriba a la derecha) ----------
    // En CÍRCULO (un círculo hecho a mano, no perfecto) si la hoja es CLAVE. Toca el número para marcarla o no.

    const float NumeroX = 742f, NumeroY = 556f;

    internal static readonly Vector2[][] Digitos =
    {
        new[] { new Vector2(0.5f, 1f), new Vector2(0.15f, 0.8f), new Vector2(0.08f, 0.45f), new Vector2(0.25f, 0.08f), new Vector2(0.55f, 0f),
                new Vector2(0.85f, 0.2f), new Vector2(0.9f, 0.6f), new Vector2(0.7f, 0.95f), new Vector2(0.42f, 0.98f) },
        new[] { new Vector2(0.25f, 0.75f), new Vector2(0.55f, 1f), new Vector2(0.55f, 0.5f), new Vector2(0.55f, 0f) },
        new[] { new Vector2(0.12f, 0.72f), new Vector2(0.3f, 0.95f), new Vector2(0.62f, 0.98f), new Vector2(0.85f, 0.78f), new Vector2(0.72f, 0.5f),
                new Vector2(0.12f, 0.02f), new Vector2(0.9f, 0.04f) },
        new[] { new Vector2(0.15f, 0.85f), new Vector2(0.45f, 1f), new Vector2(0.8f, 0.85f), new Vector2(0.7f, 0.62f), new Vector2(0.42f, 0.54f),
                new Vector2(0.78f, 0.44f), new Vector2(0.85f, 0.18f), new Vector2(0.5f, 0f), new Vector2(0.12f, 0.12f) },
        new[] { new Vector2(0.68f, 0f), new Vector2(0.67f, 0.5f), new Vector2(0.66f, 1f), new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.32f) },
        new[] { new Vector2(0.85f, 0.98f), new Vector2(0.25f, 0.96f), new Vector2(0.2f, 0.56f), new Vector2(0.5f, 0.62f), new Vector2(0.82f, 0.45f),
                new Vector2(0.8f, 0.14f), new Vector2(0.48f, 0f), new Vector2(0.12f, 0.12f) },
        new[] { new Vector2(0.78f, 0.95f), new Vector2(0.4f, 0.85f), new Vector2(0.15f, 0.45f), new Vector2(0.25f, 0.08f), new Vector2(0.55f, 0f),
                new Vector2(0.82f, 0.2f), new Vector2(0.78f, 0.48f), new Vector2(0.48f, 0.56f), new Vector2(0.18f, 0.4f) },
        new[] { new Vector2(0.1f, 0.96f), new Vector2(0.9f, 0.98f), new Vector2(0.65f, 0.5f), new Vector2(0.4f, 0f) },
        new[] { new Vector2(0.52f, 0.55f), new Vector2(0.2f, 0.75f), new Vector2(0.48f, 1f), new Vector2(0.8f, 0.78f), new Vector2(0.52f, 0.55f),
                new Vector2(0.15f, 0.28f), new Vector2(0.48f, 0f), new Vector2(0.85f, 0.25f), new Vector2(0.52f, 0.55f) },
        new[] { new Vector2(0.82f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(0.2f, 0.7f), new Vector2(0.45f, 1f), new Vector2(0.8f, 0.8f),
                new Vector2(0.75f, 0.3f), new Vector2(0.55f, 0f) },
    };

    static void NumeroPagina(Color32[] px, int numero, bool clave)
    {
        string s = numero.ToString();
        var azar = new System.Random(numero * 7919 + 17);   // el mismo "pulso" cada vez para el mismo número
        const float alto = 19f, ancho = 10.5f, paso = 12.5f;
        float x0 = NumeroX - (s.Length - 1) * paso * 0.5f;
        for (int k = 0; k < s.Length; k++)
        {
            int dig = s[k] - '0';
            if (dig < 0 || dig > 9)
                continue;
            var pts = new List<Vector2>();
            foreach (var p in Digitos[dig])
                pts.Add(new Vector2(x0 + k * paso + (p.x - 0.5f) * ancho + Temblorcito(azar, 0.5f),
                                    NumeroY + (p.y - 0.5f) * alto + Temblorcito(azar, 0.5f)));
            LineaAMano(px, Chaikin(Chaikin(pts)), 1.05f, 0.62f);
        }
        if (!clave)
            return;
        // El círculo: da un poco más de una vuelta, no es redondo del todo y no cierra justo donde empezó.
        float rx = 15f + (s.Length - 1) * paso * 0.5f, ry = 15.5f;
        float inicio = (float)azar.NextDouble() * Mathf.PI * 2f, fase = (float)azar.NextDouble() * 6f;
        var c = new List<Vector2>();
        const int pasos = 72;
        for (int i = 0; i <= pasos; i++)
        {
            float t = i / (float)pasos;
            float a = inicio + t * Mathf.PI * 2f * 1.09f;
            float r = 1f + 0.05f * Mathf.Sin(2f * a + fase) - 0.06f * t;
            c.Add(new Vector2(NumeroX + 0.5f + Mathf.Cos(a) * rx * r, NumeroY + Mathf.Sin(a) * ry * r));
        }
        LineaAMano(px, c, 0.95f, 0.5f);
    }

    static float Temblorcito(System.Random azar, float cuanto)
    {
        return ((float)azar.NextDouble() * 2f - 1f) * cuanto;
    }

    // Suaviza las esquinas (como la mano), dejando las puntas donde están.
    internal static List<Vector2> Chaikin(List<Vector2> p)
    {
        if (p.Count < 3)
            return p;
        var r = new List<Vector2> { p[0] };
        for (int i = 0; i < p.Count - 1; i++)
        {
            r.Add(Vector2.Lerp(p[i], p[i + 1], 0.25f));
            r.Add(Vector2.Lerp(p[i], p[i + 1], 0.75f));
        }
        r.Add(p[p.Count - 1]);
        return r;
    }

    // Una línea de lápiz (en píxeles de la hoja), con grano.
    static void LineaAMano(Color32[] px, List<Vector2> p, float radio, float fuerza)
    {
        for (int i = 1; i < p.Count; i++)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p[i - 1], p[i]) / 0.6f));
            for (int k = 0; k <= n; k++)
            {
                Vector2 q = Vector2.Lerp(p[i - 1], p[i], k / (float)n);
                Sello(px, q.x, q.y, radio, ColorGrafito, fuerza, true);
            }
        }
    }

    // ¿El dedo empezó a tocar sobre el número? (u, v de la hoja)
    static bool EnNumero(Vector3 q)
    {
        return Mathf.Abs(q.x * AnchoTex - NumeroX) < 34f && Mathf.Abs(q.y * AltoTex - NumeroY) < 30f;
    }

    void AlternarClave()
    {
        GuardarDeshacer();
        Pag.clave = !Pag.clave;
        RehacerHoja();
        GuardarBoceto();
        textoAyuda.text = Idioma.T(Pag.clave ? "Hoja clave: su número va en un círculo" : "Ya no es hoja clave");
        control.SonidoBurbuja(hoja.position);
    }

    // ---------- El boceto se guarda (si sales sin entrar, sigue ahí) ----------

    static string RutaBoceto => Path.Combine(Application.persistentDataPath, "mesa_boceto.json");

    void GuardarBoceto()
    {
        try
        {
            File.WriteAllText(RutaBoceto, JsonUtility.ToJson(boceto));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: boceto de la mesa: " + e.Message);
        }
    }

    void CargarBoceto()
    {
        try
        {
            if (File.Exists(RutaBoceto))
            {
                var g = JsonUtility.FromJson<Guardado>(File.ReadAllText(RutaBoceto));
                if (g != null)
                    boceto = g;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: boceto de la mesa: " + e.Message);
        }
        Normalizar();
    }

    // v59: "Reset" en Archivos: la próxima vez la mesa empieza limpia (con el boceto antiguo).
    public static void BorrarBocetoGuardado()
    {
        try
        {
            if (File.Exists(RutaBoceto))
                File.Delete(RutaBoceto);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: boceto de la mesa: " + e.Message);
        }
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}

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
//    se ve debajo en papel cebolla), ◀ ▶ para ir y volver, "▶ Hojear" las pasa seguidas (8 por segundo) como un
//    flip book. Al entrar, entra la hoja que está en el disco (las demás se quedan en el bloc).
public class MesaEntrada : MonoBehaviour
{
    static MesaEntrada instancia;
    public static bool Activa => instancia != null && instancia.activa;

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
    BotonTocable btnTinta, btnCerrarApp;
    TextMeshPro textoAyuda;
    float cerrarAppHasta;
    Material matImagen, matTransparente, matColor;

    // Lo que dibujaste (en la hoja: u, v de 0 a 1 y la presión), para pasarlo al dibujo al entrar.
    // v60: c = color del lápiz (0 grafito, 1 rojo, 2 azul). Varias HOJAS (el bloc): cada una su lápiz y su tinta.
    [System.Serializable]
    class Linea { public List<Vector3> p = new List<Vector3>(); public int c; }
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
    readonly List<Transform> lapices = new List<Transform>();

    // El bloc: hojear (ver las hojas seguidas, como un flip book).
    bool hojeando;
    float proximoCuadro;
    int cuadroHojear;
    readonly List<Color32[]> cuadrosHojear = new List<Color32[]>();
    TextMeshPro textoBloc;
    BotonTocable btnHojear;

    // Zoom y altura de la mesa (con las dos manos en pellizco). Se recuerdan.
    const string ClaveEscala = "jcartoons_mesa_escala", ClaveAltura = "jcartoons_mesa_altura";
    float escalaMesa = 1f, alturaMesa;
    bool zoomeando;
    float zoomDistancia, zoomEscala, zoomAltura, zoomY;
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
        control.Ocupado = true;
        if (animando)
            return;
        if (!Zoom())
        {
            if (hojeando)
                Hojear();
            else
                Dibujar();
            GirarDisco();
        }
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
        if (activa && control != null)
            control.Ocupado = true;
    }

    void Mostrar()
    {
        if (activa || animando)
            return;
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
        activa = true;
        control.Ocupado = true;
        QuizasOtraMuestra();
        Colocar();
        LlenarRecientes();
        StartCoroutine(Aparecer());
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
        raiz.SetPositionAndRotation(baseMesa + Vector3.up * alturaMesa, giroMesa);
        raiz.localScale = Vector3.one * escalaMesa;
    }

    // v60: ZOOM y ALTURA de la mesa: pellizca con LAS DOS manos; separar o juntar = más grande o más chica; subir o bajar
    // las manos = más arriba o más abajo. No se va de enfrente (así nunca la pierdes). Devuelve true mientras lo haces.
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
                PlayerPrefs.SetFloat(ClaveEscala, escalaMesa);
                PlayerPrefs.SetFloat(ClaveAltura, alturaMesa);
                PlayerPrefs.Save();
            }
            return false;
        }
        float dist = Vector3.Distance(iz.PuntoPellizco, de.PuntoPellizco);
        float y = (iz.PuntoPellizco.y + de.PuntoPellizco.y) * 0.5f;
        if (!zoomeando)
        {
            zoomeando = true;
            manoGiro = -1;
            TerminarLinea();
            zoomDistancia = Mathf.Max(0.03f, dist);
            zoomEscala = escalaMesa;
            zoomAltura = alturaMesa;
            zoomY = y;
        }
        escalaMesa = Mathf.Clamp(zoomEscala * dist / zoomDistancia, 0.6f, 2f);
        alturaMesa = Mathf.Clamp(zoomAltura + (y - zoomY), -0.35f, 0.35f);
        AplicarPose();
        return true;
    }

    void CentrarMesa()
    {
        escalaMesa = 1f;
        alturaMesa = 0f;
        PlayerPrefs.SetFloat(ClaveEscala, escalaMesa);
        PlayerPrefs.SetFloat(ClaveAltura, alturaMesa);
        PlayerPrefs.Save();
        Colocar();
        control.SonidoBurbuja(raiz.position);
    }

    // ---------- El bloc y los lápices ----------

    void ElegirLapiz(int k)
    {
        colorLapiz = Mathf.Clamp(k, 0, ColoresLapiz.Length - 1);
        for (int i = 0; i < lapices.Count; i++)
        {
            Vector3 p = lapices[i].localPosition;
            float baseY = i == Goma ? -0.03f : 0f;
            lapices[i].localPosition = new Vector3(p.x, baseY + (i == colorLapiz ? 0.012f : 0f), p.z);
        }
        if (control != null && lapices.Count > 0)
            control.SonidoBurbuja(lapices[colorLapiz].position);
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
            textoAyuda.text = Idioma.T("Para hojear agrega otra hoja con \"+ Hoja\" y dibuja el siguiente cuadro");
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
        btnHojear.etiqueta.text = Idioma.T("▶ Hojear");
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
        StartCoroutine(Entrar(-1));
    }

    void AbrirReciente(int i)
    {
        if (animando || i < 0 || i >= rutasRecientes.Count)
            return;
        StartCoroutine(Entrar(i));
    }

    // v60: LA ZAMBULLIDA. El dibujo elegido aparece DE GOLPE en la hoja del disco (sin deslizarse). Luego tú te
    // zambulles en esa hoja: la mesa crece a tu alrededor mientras te acercas; a la mitad se abre el dibujo, la mesa se
    // va y la hoja (que ya te rodea) se desvanece mientras tus líneas de verdad aparecen poco a poco.
    IEnumerator Entrar(int reciente)
    {
        animando = true;
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
        if (reciente >= 0)
        {
            var tm = minis[reciente].sharedMaterial;
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
        if (reciente < 0)
            PasarBocetoAlDibujo();
        else
            dibujo.AbrirArchivo(rutasRecientes[reciente]);
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
        rHoja.sharedMaterial = original;
        Destroy(mVer);
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
            {
                if (l.c == Goma)
                    continue;   // (la goma de la tinta solo vale en la mesa; las líneas de verdad se borran con sus gestos)
                var puntos = new List<Vector3>();
                foreach (var q in l.p)
                    puntos.Add(raizD.InverseTransformPoint(mundo(q) - adelante * 0.002f));
                var simples = Simplificar(puntos, 0.0015f / dibujo.EscalaMundo);
                if (simples.Count < 2)
                    continue;
                var d = new DatosTrazo { color = Color.black, enHoja = true, hojaPunto = planoLocal, hojaNormal = normalLocal };
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

    void Dibujar()
    {
        var der = control.Der;
        if (!der.valida || der.pellizco)
        {
            tocandoNumero = false;
            TerminarLinea();
            return;
        }
        Vector3 dedo = Filtrar(der.indice);
        Transform plano = tintaPuesta ? hojaTinta : hoja;
        Vector3 l = plano.InverseTransformPoint(dedo);
        float distancia = -l.z;   // la hoja mira hacia ti (su -Z)
        bool dentro = Mathf.Abs(l.x) < 0.5f && Mathf.Abs(l.y) < 0.5f;
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
        if (actual == null && !hojeando && EnNumero(new Vector3(l.x + 0.5f, l.y + 0.5f, 0f)))
        {
            tocandoNumero = true;
            AlternarClave();
            return;
        }
        float presion = Mathf.Clamp01(1f - Mathf.Max(0f, distancia) / AlcanceLapiz);
        var q = new Vector3(l.x + 0.5f, l.y + 0.5f, Mathf.Max(0.15f, presion));
        if (actual == null || actualTinta != tintaPuesta)
        {
            TerminarLinea();
            actual = new Linea { c = tintaPuesta ? (colorLapiz == Goma ? Goma : 0) : colorLapiz };
            actualTinta = tintaPuesta;
            (tintaPuesta ? Pag.tinta : Pag.lapiz).Add(actual);
        }
        var lista = actual.p;
        if (lista.Count > 0)
        {
            Vector3 ultimo = lista[lista.Count - 1];
            if (Vector2.Distance(ultimo, q) < 0.0015f)
                return;
            Trazar(ultimo, q, actualTinta, actual.c);
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
        if (actual.p.Count == 0)
            (actualTinta ? Pag.tinta : Pag.lapiz).Remove(actual);
        actual = null;
        GuardarBoceto();
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
            float radio = 5f + 4f * Mathf.Lerp(a.z, b.z, t);
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

    void GirarDisco()
    {
        if (manoGiro >= 0)
        {
            var m = manoGiro == 0 ? control.Izq : control.Der;
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
        for (int k = 0; k < 2; k++)
        {
            var m = k == 0 ? control.Izq : control.Der;
            if (!m.valida || !m.empezoPellizco || BotonTocable.DedoCerca)
                continue;
            Vector3 l = raiz.InverseTransformPoint(m.PuntoPellizco) - giratorio.localPosition;
            if (new Vector2(l.x, l.y).magnitude < RadioDisco + 0.03f && Mathf.Abs(l.z) < 0.06f)
            {
                manoGiro = k;
                anguloAntes = AnguloEnDisco(m.PuntoPellizco);
                return;
            }
        }
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
        Pag.lapiz.Clear();
        Pag.tinta.Clear();
        if (boceto.actual == 0)
            boceto.antiguo = false;
        tintaPuesta = false;
        RehacerHoja();
        GuardarBoceto();
        control.SonidoBurbuja(hoja.position);
    }

    void AlternarTinta()
    {
        tintaPuesta = !tintaPuesta;
        TerminarLinea();
        hojaTinta.gameObject.SetActive(tintaPuesta);
        btnTinta.etiqueta.text = Idioma.T(tintaPuesta ? "Quitar hoja de tinta" : "Hoja de tinta");
        btnTinta.Marcar(tintaPuesta);
        control.SonidoBurbuja(hoja.position);
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
        rutasRecientes.AddRange(dibujo.ListaArchivos(Recientes));
        for (int i = 0; i < Recientes; i++)
        {
            bool hay = i < rutasRecientes.Count;
            botonesRecientes[i].gameObject.SetActive(hay);
            minis[i].gameObject.SetActive(hay);
            nombresRecientes[i].gameObject.SetActive(hay);
            if (!hay)
                continue;
            nombresRecientes[i].text = Path.GetFileNameWithoutExtension(rutasRecientes[i]);
            Texture2D tex = null;
            try
            {
                string mini = dibujo.RutaMiniatura(rutasRecientes[i]);
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
        escalaMesa = Mathf.Clamp(PlayerPrefs.GetFloat(ClaveEscala, 1f), 0.6f, 2f);
        alturaMesa = Mathf.Clamp(PlayerPrefs.GetFloat(ClaveAltura, 0f), -0.35f, 0.35f);

        // El tablero de madera (su cara de arriba en z = 0; lo de encima va hacia -z).
        Pieza(PrimitiveType.Cube, new Vector3(0f, 0f, 0.012f), new Vector3(0.84f, 0.6f, 0.024f), Quaternion.identity, Textura(Madera()));
        // La orilla de aluminio de abajo (para que no se caigan los lápices).
        Pieza(PrimitiveType.Cube, new Vector3(0f, -0.302f, -0.006f), new Vector3(0.84f, 0.012f, 0.03f), Quaternion.identity, Pintura(new Color(0.75f, 0.76f, 0.78f)));

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
        var mTinta = new Material(matTransparente);
        if (mTinta.HasProperty("_BaseColor"))
            mTinta.SetColor("_BaseColor", UnityEngine.Color.white);
        PonerTextura(mTinta, texTinta);
        hojaTinta = Pieza(PrimitiveType.Quad, new Vector3(0f, 0f, -0.0068f), new Vector3(AnchoHoja, AltoHoja, 1f), Quaternion.identity, mTinta, giratorio);
        hojaTinta.gameObject.SetActive(false);
        // Las reglas negras con pivotes (sujetan la hoja).
        var negro = Pintura(new Color(0.08f, 0.08f, 0.09f));
        var metal = Pintura(new Color(0.55f, 0.56f, 0.6f));
        for (int s = -1; s <= 1; s += 2)
        {
            float y = s * (AltoHoja * 0.5f + 0.008f);
            Pieza(PrimitiveType.Cube, new Vector3(0f, y, -0.008f), new Vector3(AnchoHoja + 0.05f, 0.022f, 0.006f), Quaternion.identity, negro, giratorio);
        }
        // v61: los PIVOTES abajo, justo en las perforaciones de la hoja (uno redondo al centro y dos planos a los lados).
        float yPivote = (AlturaHoyos - 0.5f) * AltoHoja;
        Pieza(PrimitiveType.Cylinder, new Vector3(0f, yPivote, -0.0105f), new Vector3(0.0055f, 0.003f, 0.0055f), giroDisco, metal, giratorio);
        for (int s = -1; s <= 1; s += 2)
            Pieza(PrimitiveType.Cube, new Vector3(s * SeparacionRanura * AnchoHoja, yPivote, -0.0105f), new Vector3(0.0165f, 0.0035f, 0.006f),
                  Quaternion.identity, metal, giratorio);

        // Los lápices y el clip de resorte (arriba a la derecha).
        Pieza(PrimitiveType.Cube, new Vector3(0.335f, 0.07f, -0.006f), new Vector3(0.12f, 0.016f, 0.01f), Quaternion.identity, metal);
        // v60: tres lápices: grafito, rojo y azul. Toca uno para dibujar con él (el elegido se levanta).
        var madera = Pintura(new Color(0.85f, 0.72f, 0.52f));
        for (int k = 0; k < 3; k++)
        {
            float x = 0.3f + k * 0.035f;
            int kk = k;
            var cuerpo = Pintura(k == 0 ? new Color(0.12f, 0.12f, 0.14f) : ColoresLapiz[k]);
            var b = PanelTutoriales.Boton(raiz, "", new Vector3(x, 0f, -0.008f), new Vector2(0.009f, 0.12f), cuerpo, cuerpo);
            b.alTocar.AddListener(() => ElegirLapiz(kk));
            lapices.Add(b.transform);
            Pieza(PrimitiveType.Cylinder, new Vector3(x, -0.066f, -0.008f), new Vector3(0.006f, 0.006f, 0.006f), Quaternion.identity, madera);
        }
        // v61: la GOMA (rosada, al lado de los lápices): tócala y lo que "dibujas" se borra.
        var rosa = Pintura(ColoresLapiz[Goma]);
        var goma = PanelTutoriales.Boton(raiz, "", new Vector3(0.4f, -0.03f, -0.008f), new Vector2(0.018f, 0.045f), rosa, rosa);
        goma.alTocar.AddListener(() => ElegirLapiz(Goma));
        lapices.Add(goma.transform);
        ElegirLapiz(0);

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
        btnHojear.etiqueta.text = Idioma.T("▶ Hojear");
        btnHojear.alTocar.AddListener(AlternarHojear);

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
        var entrar = PanelTutoriales.Boton(raiz, "", new Vector3(0f, -0.272f, -0.004f), new Vector2(0.14f, 0.026f), marcado, normal);
        Idioma.Poner(entrar.etiqueta, "✦ Entrar");
        entrar.etiqueta.color = UnityEngine.Color.white;
        entrar.textoNormal = UnityEngine.Color.white;
        entrar.alTocar.AddListener(() => { if (!animando) EntrarABoceto(); });
        var nueva = PanelTutoriales.Boton(raiz, "", new Vector3(0.33f, -0.16f, -0.004f), new Vector2(0.11f, 0.03f), normal, marcado);
        Idioma.Poner(nueva.etiqueta, "Hoja nueva");
        nueva.alTocar.AddListener(HojaNueva);
        btnTinta = PanelTutoriales.Boton(raiz, "", new Vector3(-0.33f, -0.16f, -0.004f), new Vector2(0.12f, 0.03f), normal, marcado);
        btnTinta.etiqueta.text = Idioma.T("Hoja de tinta");
        btnTinta.alTocar.AddListener(AlternarTinta);
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
        if (boceto.actual > 0)
            Cebolla(pxFondo, boceto.paginas[boceto.actual - 1]);
        System.Array.Copy(pxFondo, pxBase, pxBase.Length);
        // El dibujo de muestra solo en la primera hoja (se borra con la goma o con "Hoja nueva").
        if (boceto.actual == 0 && boceto.antiguo)
            PonerMuestra(pxBase);
        System.Array.Copy(pxBase, pxHoja, pxBase.Length);
        fondoGoma = pxFondo;
        for (int i = 0; i < pxTinta.Length; i++)
            pxTinta[i] = vacioTinta;   // papel de calcar: deja ver el lápiz, un poco más claro
        foreach (var l in Pag.lapiz)
            RepintarLinea(l, false);
        foreach (var l in Pag.tinta)
            RepintarLinea(l, true);
        if (textoBloc != null)
            textoBloc.text = Idioma.T("Hoja") + " " + (boceto.actual + 1) + " / " + boceto.paginas.Count;
        hojaTinta.gameObject.SetActive(tintaPuesta);
        if (btnTinta != null)
        {
            btnTinta.etiqueta.text = Idioma.T(tintaPuesta ? "Quitar hoja de tinta" : "Hoja de tinta");
            btnTinta.Marcar(tintaPuesta);
        }
        sucioHoja = sucioTinta = true;
    }

    void RepintarLinea(Linea l, bool tinta)
    {
        if (l.p.Count == 0)
            return;
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
            foreach (var l in pag.tinta)
                RepintarLinea(l, true);
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
        tintaPuesta = false;
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

    static readonly Vector2[][] Digitos =
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
    static List<Vector2> Chaikin(List<Vector2> p)
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

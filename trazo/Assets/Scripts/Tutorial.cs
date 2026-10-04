using System.Collections.Generic;
using TMPro;
using UnityEngine;

// JCartoons: pantalla de título + tutorial con MANOS GUÍA (dibujar y borrar).
//  - Al abrir la app: "JCartoons" aparece letra por letra (con música de arpa mágica), un muñequito
//    corre saltando sobre las letras y todo se desvanece despacio sobre la hoja en blanco.
//  - La primera vez sigue el tutorial ("First time?"):
//    1. Mano izquierda: pellizco (activa el lápiz).  2. Índice derecho: una línea del punto a la bandera.
//       A mitad de tu línea aparece un muñeco que corre sobre ella (sorpresa).
//    3. Al llegar a la bandera, el muñeco ya sigue tu mano (es un títere de verdad).
//    4. Puño izquierdo con el dorso hacia ti (borrador).  5. Frota la línea con el índice derecho: el muñeco huye.
//  - Mientras las manos guía enseñan, tus manos se esconden y no hacen nada. Luego es tu turno.
//    Si pasan 8 segundos sin que hagas nada, las manos guía lo repiten (ya con tus manos activas).
//  - "Skip" lo termina. Se puede ver otra vez en Mis archivos > Tutorial.
//  - Tu dibujo se aparta antes de empezar y vuelve igual al terminar.
public class Tutorial : MonoBehaviour
{
    public static Tutorial Instancia { get; private set; }

    public Dibujo dibujo;
    public ControlManos control;
    public Titere titere;

    [Header("Materiales")]
    [Tooltip("Manos guía (shader TrazoVR/Guia)")]
    public Material materialGuia;
    [Tooltip("Papelitos de celebración (shader TrazoVR/Brillo, Suave = 0)")]
    public Material materialConfeti;
    [Tooltip("Escarcha (shader TrazoVR/Brillo, Suave = 1)")]
    public Material materialChispa;
    public Material materialNegro, materialBlanco, materialVerde, materialAzul, materialRojo;
    [Tooltip("La línea guía (gris transparente)")]
    public Material materialGuiaLinea;
    public Material materialBoton, materialBotonMarcado;

    const string ClaveVisto = "jcartoons_tutorial_visto";
    const string Nombre = "JCartoons";
    const float MitadLinea = 0.25f;     // de la bandera al centro (metros)
    const float AnchoMuneco = 0.006f;   // grosor de los palitos del muñeco guía

    static readonly Color[] ColoresLetras =
    {
        new Color(1f, 0.55f, 0.1f), new Color(1f, 0.3f, 0.55f), new Color(0.6f, 0.35f, 1f),
        new Color(0.2f, 0.6f, 1f), new Color(0.1f, 0.75f, 0.65f), new Color(0.35f, 0.8f, 0.25f),
        new Color(1f, 0.78f, 0.1f), new Color(1f, 0.45f, 0.2f), new Color(0.95f, 0.3f, 0.7f),
    };
    static readonly Color[] ColoresConfeti =
    {
        new Color(1f, 0.3f, 0.45f), new Color(1f, 0.75f, 0.1f), new Color(0.25f, 0.7f, 1f),
        new Color(0.4f, 0.85f, 0.35f), new Color(0.7f, 0.4f, 1f), new Color(1f, 0.5f, 0.15f),
    };

    enum Estado { Nada, Titulo, Demo12, Paso1, Paso2, Corre, Paso3, Demo45, Paso4, Paso5, Final }
    Estado estado = Estado.Nada;
    float desde;
    float T => Time.time - desde;
    bool tituloHecho;
    bool primeraVez;

    // ---------- Textos (English first) ----------
    static string Tx(string en, string es) { return Idioma.Ingles ? en : es; }

    // ---------- Título ----------
    Transform titulo;
    readonly List<TextMeshPro> letras = new List<TextMeshPro>();
    readonly List<Vector3> letrasPos = new List<Vector3>();
    readonly List<float> letrasArriba = new List<float>();
    readonly List<bool> letrasChispa = new List<bool>();
    TextMeshPro eslogan;
    MunecoGuia munecoTitulo;
    float tituloDesde;

    // ---------- Tutorial ----------
    Transform raiz;                 // marco del tutorial: x derecha, y arriba, z adelante
    GameObject raizManos;           // las manos guía (en coordenadas del mundo)
    ManoVideo manoIzq, manoDer;
    readonly Vector3[] puntosIzq = new Vector3[21];
    readonly Vector3[] puntosDer = new Vector3[21];
    Material matGuia;
    TextMeshPro encabezado, textoBurbuja;
    Transform puntoA, bandera, tela, puntoBorrador;
    LineRenderer lineaGuia, lineaDemo;
    readonly List<Vector3> curvaDemo = new List<Vector3>();
    MunecoGuia figura;
    ParticleSystem confeti, chispas;
    AudioSource fuente, fuentePasos;
    BotonTocable btnSaltar;

    class Numero
    {
        public Transform raiz;
        public Renderer relleno;
        public float desde;
    }
    readonly Dictionary<int, Numero> numeros = new Dictionary<int, Numero>();

    class Efecto
    {
        public Transform t;
        public float desde;
    }
    readonly List<Efecto> efectos = new List<Efecto>();

    // La línea del usuario (y el camino del muñeco sobre ella), en el mundo.
    Trazo trazoUsuario, lineaUsuario;
    readonly List<Vector3> camino = new List<Vector3>();
    readonly List<float> caminoLargo = new List<float>();
    float figuraS, figuraMov, figuraTam, figuraTamObjetivo, dirFigura = 1f;
    bool figuraEnLinea, huyendo;
    DatosPersonaje personaje;

    float pistaDesde = -1f, ultimaActividad;
    bool chispaPinza, exito3, borrando;
    float exito3En, movido, borrandoDesde, velDedo, ultimaChispa;
    Vector3 dedoPrevio;
    bool teniaDedo;
    int nodosInicio;

    void Awake()
    {
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
        if (raizManos != null)
            Destroy(raizManos);
        if (matGuia != null)
            Destroy(matGuia);
    }

    void Start()
    {
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (control == null) control = FindFirstObjectByType<ControlManos>();
        if (titere == null) titere = FindFirstObjectByType<Titere>();
        primeraVez = PlayerPrefs.GetInt(ClaveVisto, 0) == 0;
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        fuentePasos = gameObject.AddComponent<AudioSource>();
        fuentePasos.playOnAwake = false;
        fuentePasos.spatialBlend = 0f;
        confeti = CrearParticulas("Confeti", materialConfeti, 0.35f, 2.2f);
        chispas = CrearParticulas("Escarcha", materialChispa, -0.02f, 3f);
        // Los sonidos se calculan ahora (mientras carga), así el título no da un saltito.
        Sonidos.Preparar();
    }

    // ==================== Cada cuadro ====================

    void Update()
    {
        if (estado == Estado.Nada)
        {
            // El título sale una vez, apenas se sabe dónde está tu cabeza.
            if (!tituloHecho && control != null && control.Cabeza != null && dibujo != null && Time.timeSinceLevelLoad > 0.8f
                && control.Cabeza.position.sqrMagnitude > 0.01f)
                EmpezarTitulo();
            return;
        }
        if (control == null || dibujo == null)
            return;
        if (Bloquea(estado))
            control.Ocupado = true; // por si otro objeto lo soltó

        switch (estado)
        {
            case Estado.Titulo: ActualizarTitulo(); break;
            case Estado.Demo12:
                if (Demo12(T, true))
                    EntrarPaso1();
                break;
            case Estado.Paso1: Paso1(); break;
            case Estado.Paso2: Paso2(); break;
            case Estado.Corre: Corre(); break;
            case Estado.Paso3: Paso3(); break;
            case Estado.Demo45:
                if (Demo45(T, true))
                    EntrarPaso4();
                break;
            case Estado.Paso4: Paso4(); break;
            case Estado.Paso5: Paso5(); break;
            case Estado.Final:
                ActualizarFigura();
                if (T > 6.5f)
                    Terminar();
                break;
        }
        if (raiz != null)
        {
            ActualizarNumeros();
            ActualizarDecorado();
        }
        ActualizarEfectos();
    }

    bool Bloquea(Estado e)
    {
        return e == Estado.Demo12 || e == Estado.Corre || e == Estado.Demo45 || (e == Estado.Titulo && primeraVez);
    }

    static bool Oculta(Estado e)
    {
        return e == Estado.Demo12 || e == Estado.Demo45;
    }

    void Cambiar(Estado nuevo)
    {
        bool bloqueaba = Bloquea(estado), ocultaba = Oculta(estado);
        estado = nuevo;
        desde = Time.time;
        pistaDesde = -1f;
        ultimaActividad = Time.time;
        bool bloquea = Bloquea(nuevo), oculta = Oculta(nuevo);
        if (control != null)
        {
            if (bloquea != bloqueaba)
                control.Ocupado = bloquea;
            if (oculta != ocultaba)
                control.OcultarManos(oculta);
        }
    }

    // ==================== Título ====================

    void EmpezarTitulo()
    {
        tituloHecho = true;
        Transform cab = control.Cabeza;
        Vector3 adelante = Horizontal(cab.forward);
        titulo = new GameObject("TituloJCartoons").transform;
        titulo.SetParent(transform, false);
        Vector3 pos = cab.position + adelante * 1.1f + Vector3.up * 0.04f;
        titulo.SetPositionAndRotation(pos, Quaternion.LookRotation(adelante, Vector3.up));

        const float tam = 1.15f;
        float total = 0f;
        var anchos = new float[Nombre.Length];
        for (int i = 0; i < Nombre.Length; i++)
        {
            var t = CrearTexto(titulo, Nombre[i].ToString(), Vector3.zero, new Vector2(0.3f, 0.3f), tam, ColoresLetras[i % ColoresLetras.Length]);
            t.enableAutoSizing = false;
            t.fontSize = tam;
            t.outlineWidth = 0.22f;
            t.outlineColor = new Color32(45, 30, 80, 255);
            anchos[i] = t.preferredWidth;
            total += anchos[i];
            letras.Add(t);
            float arriba = 0.045f;
            t.ForceMeshUpdate();
            if (t.textInfo != null && t.textInfo.characterCount > 0)
                arriba = t.textInfo.characterInfo[0].topLeft.y;
            letrasArriba.Add(arriba);
            letrasChispa.Add(false);
            t.transform.localScale = Vector3.zero;
        }
        float x = -total * 0.5f;
        for (int i = 0; i < letras.Count; i++)
        {
            var p = new Vector3(x + anchos[i] * 0.5f, 0f, 0f);
            letrasPos.Add(p);
            letras[i].transform.localPosition = p;
            x += anchos[i];
        }
        eslogan = CrearTexto(titulo, Tx("The joy of creating", "El placer de crear"), new Vector3(0f, -0.1f, 0f), new Vector2(0.6f, 0.06f), 0.42f, new Color(0.25f, 0.25f, 0.35f));
        eslogan.fontStyle = FontStyles.Italic | FontStyles.Bold;
        eslogan.alpha = 0f;

        munecoTitulo = new MunecoGuia(titulo, materialNegro, AnchoMuneco * 0.8f);
        munecoTitulo.escala = 0.22f;
        munecoTitulo.Poner(Vector3.zero, Vector3.right, Vector3.up, 0f, 0f);

        tituloDesde = Time.time;
        if (fuente != null)
            fuente.PlayOneShot(Sonidos.Magia, 0.9f);
        Cambiar(Estado.Titulo);
    }

    void ActualizarTitulo()
    {
        if (titulo == null)
        {
            TerminarTitulo();
            return;
        }
        float t = Time.time - tituloDesde;
        const float fadeDesde = 4.7f, fadeDura = 1.4f;
        float f = 1f - Mathf.Clamp01((t - fadeDesde) / fadeDura);
        for (int i = 0; i < letras.Count; i++)
        {
            var l = letras[i];
            float k = (t - (Sonidos.InicioNotas + i * Sonidos.PasoNotas)) / 0.45f;
            if (k <= 0f)
            {
                l.transform.localScale = Vector3.zero;
                continue;
            }
            if (!letrasChispa[i])
            {
                letrasChispa[i] = true;
                Chispas(titulo.TransformPoint(letrasPos[i]), 7, 0.25f);
            }
            k = Mathf.Clamp01(k);
            float esc = k >= 1f ? 1f : 1f - Mathf.Exp(-5f * k) * Mathf.Cos(9f * k);
            float caer = (1f - k) * (1f - k) * 0.08f;
            float bailar = k >= 1f ? Mathf.Sin(Time.time * 2.2f + i * 0.7f) * 0.004f : 0f;
            l.transform.localPosition = letrasPos[i] + new Vector3(0f, caer + bailar, 0f);
            l.transform.localRotation = Quaternion.Euler(0f, 0f, (1f - k) * -30f);
            l.transform.localScale = Vector3.one * esc * (1f + (1f - f) * 0.15f);
            l.alpha = Mathf.Clamp01(k * 3f) * f;
        }
        if (eslogan != null)
            eslogan.alpha = Mathf.Clamp01((t - 1.65f) / 0.6f) * f;

        // El muñequito salta de letra en letra (y se va por la derecha).
        if (munecoTitulo != null && letras.Count > 0)
        {
            const float empieza = 1.75f, porLetra = 0.21f;
            float u = (t - empieza) / porLetra;
            float tam = Mathf.Clamp01((t - empieza + 0.3f) / 0.25f) * f;
            Vector3 p;
            float mov = 2f;
            int n = letras.Count;
            if (u < 0f)
            {
                p = Arriba(0);
                mov = 0f;
            }
            else if (u < n - 1)
            {
                int a = Mathf.FloorToInt(u);
                float w = u - a;
                p = Vector3.Lerp(Arriba(a), Arriba(a + 1), w) + Vector3.up * 0.035f * Mathf.Sin(Mathf.PI * w);
            }
            else
            {
                float w = u - (n - 1);
                p = Arriba(n - 1) + Vector3.right * 0.09f * w + Vector3.up * (0.04f * Mathf.Sin(Mathf.PI * Mathf.Min(w, 1f)) - Mathf.Max(0f, w - 1f) * 0.06f);
                tam *= Mathf.Clamp01(1.6f - w);
            }
            Vector3 mundo = titulo.TransformPoint(p);
            munecoTitulo.Avanzar(Time.deltaTime * (mov > 0f ? 0.33f : 0f), mov);
            munecoTitulo.Poner(mundo, titulo.right, titulo.up, mov, tam);
        }

        if (t > fadeDesde + fadeDura + 0.1f)
            TerminarTitulo();
    }

    Vector3 Arriba(int i)
    {
        i = Mathf.Clamp(i, 0, letrasPos.Count - 1);
        return letrasPos[i] + new Vector3(0f, letrasArriba[i] + 0.004f, -0.01f);
    }

    void TerminarTitulo()
    {
        if (munecoTitulo != null)
            munecoTitulo.Destruir();
        munecoTitulo = null;
        if (titulo != null)
            Destroy(titulo.gameObject);
        titulo = null;
        letras.Clear();
        letrasPos.Clear();
        letrasArriba.Clear();
        letrasChispa.Clear();
        eslogan = null;
        if (primeraVez)
            Empezar();
        else
            Cambiar(Estado.Nada);
    }

    // ==================== Empezar y terminar el tutorial ====================

    public void Empezar()
    {
        if (dibujo == null || control == null || control.Cabeza == null)
            return;
        if (estado == Estado.Titulo)
        {
            // Si lo piden durante el título, el título se va de una vez.
            if (munecoTitulo != null) munecoTitulo.Destruir();
            munecoTitulo = null;
            if (titulo != null) Destroy(titulo.gameObject);
            titulo = null;
            letras.Clear(); letrasPos.Clear(); letrasArriba.Clear(); letrasChispa.Clear();
        }
        else if (estado != Estado.Nada)
        {
            Terminar();
        }
        if (titere != null && titere.Encendido)
            titere.Parar();
        dibujo.ApartarParaTutorial();
        Armar();
        Cambiar(Estado.Demo12);
    }

    void Saltar()
    {
        Terminar();
    }

    void Terminar()
    {
        PlayerPrefs.SetInt(ClaveVisto, 1);
        PlayerPrefs.Save();
        primeraVez = false;
        if (titere != null)
        {
            if (titere.Encendido)
                titere.Parar();
            if (personaje != null)
                titere.BorrarPersonaje(personaje, true);
            if (lineaUsuario != null)
                titere.pisos.Remove(lineaUsuario.id);
        }
        personaje = null;
        trazoUsuario = null;
        lineaUsuario = null;
        if (dibujo != null)
            dibujo.RecuperarDeTutorial();
        Desarmar();
        Cambiar(Estado.Nada);
    }

    // ==================== Armar lo que se ve ====================

    void Armar()
    {
        Desarmar();
        Transform cab = control.Cabeza;
        Vector3 adelante = Horizontal(cab.forward);
        raiz = new GameObject("Tutorial").transform;
        raiz.SetParent(transform, false);
        raiz.SetPositionAndRotation(cab.position + adelante * 0.45f - Vector3.up * 0.17f, Quaternion.LookRotation(adelante, Vector3.up));

        // "First time?" arriba, con letras de colores.
        encabezado = CrearTexto(raiz, Tx("First time?", "¿Primera vez?"), new Vector3(0f, 0.3f, 0.12f), new Vector2(0.42f, 0.09f), 1.2f, Color.white);
        encabezado.enableVertexGradient = true;
        encabezado.colorGradient = new VertexGradient(new Color(1f, 0.6f, 0.1f), new Color(1f, 0.3f, 0.6f), new Color(0.55f, 0.35f, 1f), new Color(0.2f, 0.65f, 1f));
        encabezado.outlineWidth = 0.2f;
        encabezado.outlineColor = new Color32(45, 30, 80, 255);

        // Viñeta de cómic (arriba a la izquierda).
        var burbuja = new GameObject("Vineta").transform;
        burbuja.SetParent(raiz, false);
        burbuja.localPosition = new Vector3(-0.29f, 0.19f, 0.04f);
        const float bw = 0.28f, bh = 0.1f;
        Malla(burbuja, "Borde", MallaVineta(bw + 0.008f, bh + 0.008f, 0.024f, 0.006f), materialNegro, new Vector3(0f, 0f, 0.0015f));
        Malla(burbuja, "Relleno", MallaVineta(bw, bh, 0.02f, 0f), materialBlanco, Vector3.zero);
        textoBurbuja = CrearTexto(burbuja, "", new Vector3(0f, 0f, -0.002f), new Vector2(bw - 0.03f, bh - 0.02f), 0.3f, Color.black);
        textoBurbuja.fontSizeMin = 0.05f;
        textoBurbuja.fontStyle = FontStyles.Bold | FontStyles.UpperCase;

        // Botón "Skip" (abajo a la derecha).
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Boton_Saltar";
        cubo.transform.SetParent(raiz, false);
        cubo.transform.localPosition = new Vector3(0.37f, -0.21f, 0.02f);
        cubo.transform.localScale = new Vector3(0.07f, 0.026f, 0.008f);
        Pintar(cubo.GetComponent<Renderer>(), materialBoton);
        btnSaltar = cubo.AddComponent<BotonTocable>();
        btnSaltar.materialNormal = materialBoton;
        btnSaltar.materialMarcado = materialBotonMarcado;
        btnSaltar.etiqueta = CrearTexto(raiz, Tx("Skip", "Saltar"), new Vector3(0.37f, -0.21f, 0.0154f), new Vector2(0.064f, 0.02f), 0.2f, Color.black);
        btnSaltar.alTocar.AddListener(Saltar);

        // Punto A (azul) y bandera en B.
        curvaDemo.Clear();
        for (int i = 0; i <= 60; i++)
            curvaDemo.Add(raiz.TransformPoint(Curva(i / 60f)));
        puntoA = new GameObject("PuntoA").transform;
        puntoA.SetParent(raiz, false);
        puntoA.localPosition = Curva(0f) + new Vector3(0f, 0f, -0.003f);
        Disco(puntoA, materialNegro, 0.036f, new Vector3(0f, 0f, 0.001f));
        Disco(puntoA, materialAzul, 0.03f, Vector3.zero);
        bandera = new GameObject("Bandera").transform;
        bandera.SetParent(raiz, false);
        bandera.localPosition = Curva(1f);
        var palo = Pieza(PrimitiveType.Cube, bandera, materialNegro);
        palo.transform.localPosition = new Vector3(0f, 0.065f, 0f);
        palo.transform.localScale = new Vector3(0.004f, 0.13f, 0.004f);
        tela = new GameObject("Tela").transform;
        tela.SetParent(bandera, false);
        tela.localPosition = new Vector3(0.002f, 0.13f, 0f);
        var mallaTela = new Mesh { name = "Tela" };
        mallaTela.vertices = new[] { Vector3.zero, new Vector3(0.065f, -0.022f, 0f), new Vector3(0f, -0.044f, 0f) };
        mallaTela.triangles = new[] { 0, 1, 2 };
        mallaTela.RecalculateNormals();
        mallaTela.RecalculateBounds();
        Malla(tela, "Trapo", mallaTela, materialRojo, Vector3.zero);
        var baseB = Pieza(PrimitiveType.Sphere, bandera, materialRojo);
        baseB.transform.localScale = Vector3.one * 0.014f;

        lineaGuia = CrearLinea("LineaGuia", materialGuiaLinea, 0.007f);
        PonerLinea(lineaGuia, curvaDemo, curvaDemo.Count);
        lineaGuia.gameObject.SetActive(false);
        lineaDemo = CrearLinea("LineaDemo", materialNegro, Mathf.Max(0.003f, dibujo.AnchoNuevoMundo));
        lineaDemo.positionCount = 0;

        // Manos guía (sus puntos ya están en el mundo, por eso cuelgan de un objeto sin mover).
        raizManos = new GameObject("ManosGuia");
        matGuia = materialGuia != null ? new Material(materialGuia) : null;
        manoIzq = new ManoVideo(raizManos.transform, matGuia, 0, true);
        manoDer = new ManoVideo(raizManos.transform, matGuia, 0, true);
        manoIzq.Poner(null);
        manoDer.Poner(null);
        var bola = Pieza(PrimitiveType.Sphere, raiz, materialRojo);
        bola.name = "PuntoBorrador";
        bola.transform.localScale = Vector3.one * 0.016f;
        puntoBorrador = bola.transform;
        puntoBorrador.gameObject.SetActive(false);

        figura = new MunecoGuia(transform, materialNegro, AnchoMuneco);
        figura.alPisar = () => Pasito(0.28f);
        figura.Poner(Vector3.zero, Vector3.right, Vector3.up, 0f, 0f);
        figuraTam = 0f;
        figuraTamObjetivo = 0f;
        figuraEnLinea = false;
        huyendo = false;
        exito3 = false;
        borrando = false;
        movido = 0f;
        teniaDedo = false;
        chispaPinza = false;
    }

    void Desarmar()
    {
        if (raiz != null)
            Destroy(raiz.gameObject);
        raiz = null;
        if (manoIzq != null) manoIzq.Destruir();
        if (manoDer != null) manoDer.Destruir();
        manoIzq = manoDer = null;
        if (raizManos != null)
            Destroy(raizManos);
        raizManos = null;
        if (matGuia != null)
            Destroy(matGuia);
        matGuia = null;
        if (figura != null)
            figura.Destruir();
        figura = null;
        if (lineaGuia != null)
            Destroy(lineaGuia.gameObject);
        if (lineaDemo != null)
            Destroy(lineaDemo.gameObject);
        lineaGuia = lineaDemo = null;
        numeros.Clear();
        encabezado = null;
        textoBurbuja = null;
        btnSaltar = null;
    }

    // ==================== Pasos 1 y 2: dibujar ====================

    // La curva de ejemplo (en el marco del tutorial): una ola suave de A (izquierda) a B (derecha).
    static Vector3 Curva(float u)
    {
        float y = 0.03f * (Mathf.Sin(u * Mathf.PI * 2f - 0.4f) + Mathf.Sin(0.4f));
        return new Vector3(Mathf.Lerp(-MitadLinea, MitadLinea, u), y, 0f);
    }

    Vector3 A => raiz.TransformPoint(Curva(0f));
    Vector3 B => raiz.TransformPoint(Curva(1f));

    // Las manos guía enseñan el pellizco (1) y la línea (2). "completa" = la primera vez (con números y línea).
    bool Demo12(float t, bool completa)
    {
        float alfa = Mathf.Clamp01(t / 0.4f) * (1f - Mathf.Clamp01((t - 5.6f) / 0.5f));
        PonerAlfaGuia(completa ? alfa : alfa * 0.75f);

        float pinza = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.95f, t)) * (1f - Mathf.InverseLerp(5.1f, 5.4f, t));
        PoseMano.Calcular(puntosIzq, true, L(-0.36f, -0.12f, -0.1f), Dir(0.35f, 0.35f, 1f), Dir(1f, 0.1f, -0.1f), pinza, 0f, 0f);
        manoIzq.Poner(alfa > 0.01f ? puntosIzq : null);
        if (t < 0.9f)
            chispaPinza = false;
        else if (!chispaPinza)
        {
            chispaPinza = true;
            Chispas((puntosIzq[4] + puntosIzq[8]) * 0.5f, 8, 0.15f);
        }

        Vector3 descanso = L(0.32f, -0.14f, -0.12f);
        Vector3 punta;
        float u = 0f;
        if (t < 1.6f)
            punta = descanso;
        else if (t < 2.2f)
            punta = Vector3.Lerp(descanso, A, Suave((t - 1.6f) / 0.6f));
        else if (t < 4.6f)
        {
            u = Suave((t - 2.2f) / 2.4f);
            punta = raiz.TransformPoint(Curva(u));
        }
        else
        {
            u = 1f;
            punta = B;
        }
        PoseMano.ConIndiceEn(puntosDer, false, punta, Dir(-0.1f, 0.25f, 1f), Dir(0f, -1f, 0.2f), 0f, 0f, 1f);
        manoDer.Poner(alfa > 0.01f ? puntosDer : null);

        if (completa)
        {
            if (t >= 0.5f)
            {
                MostrarNumero(1, new Vector3(-0.36f, 0.05f, -0.06f));
                if (t < 1.6f)
                    PonerBurbuja(Tx("1. Pinch with your LEFT hand: that turns on the pencil", "1. Pellizca con la mano IZQUIERDA: así se activa el lápiz"));
            }
            if (t >= 1.6f)
            {
                MostrarNumero(2, new Vector3(0.36f, 0.09f, -0.02f));
                PonerBurbuja(Tx("2. Keep pinching and draw with your RIGHT index finger from the dot to the flag", "2. Sin soltar, dibuja con el índice DERECHO desde el punto hasta la bandera"));
            }
            if (t >= 2.2f)
                PonerLinea(lineaDemo, curvaDemo, Mathf.Clamp(Mathf.RoundToInt(u * (curvaDemo.Count - 1)) + 1, 2, curvaDemo.Count));
        }
        return t >= 6.1f;
    }

    void EntrarPaso1()
    {
        OcultarGuia();
        if (lineaDemo != null)
            lineaDemo.positionCount = 0;
        if (lineaGuia != null)
            lineaGuia.gameObject.SetActive(true);
        PonerBurbuja(Tx("1. Pinch with your LEFT hand: that turns on the pencil", "1. Pellizca con la mano IZQUIERDA: así se activa el lápiz"));
        Cambiar(Estado.Paso1);
    }

    void Paso1()
    {
        if (control.GestoIzq == ControlManos.Gesto.Dibujar)
        {
            PararPista();
            Exito(1, control.Izq.PuntoPellizco);
            PonerBurbuja(Tx("2. Keep pinching and draw with your RIGHT index finger from the dot to the flag", "2. Sin soltar, dibuja con el índice DERECHO desde el punto hasta la bandera"));
            Cambiar(Estado.Paso2);
            return;
        }
        Pista12();
    }

    void Paso2()
    {
        var tr = control.TrazoActual;
        if (tr != null && tr.Dibujando)
        {
            PararPista();
            ultimaActividad = Time.time;
            if (trazoUsuario != tr)
            {
                trazoUsuario = tr;
                figuraEnLinea = false;
                figuraTamObjetivo = 0f;
            }
            LeerCamino(tr);
            float largo = LargoCamino;
            float ab = MitadLinea * 2f;
            // Sorpresa: a mitad de tu línea aparece un muñeco que corre sobre ella.
            if (!figuraEnLinea && largo >= ab * 0.45f)
            {
                figuraEnLinea = true;
                figuraS = 0f;
                figuraMov = 1f;
                figuraTamObjetivo = 1f;
                Chispas(PuntoCamino(0f), 6, 0.2f);
            }
            if (figuraEnLinea)
                MoverFigura(largo - 0.05f, 0.4f);
            // ¡Llegaste a la bandera!
            if (Vector3.Distance(control.Der.indice, B) < 0.07f && largo >= ab * 0.6f)
            {
                Exito(2, B);
                control.TerminarLineaActual();
                lineaUsuario = tr;
                if (lineaUsuario == null || !dibujo.trazos.Contains(lineaUsuario))
                    lineaUsuario = UltimoTrazo();
                if (lineaUsuario != null)
                {
                    Aplanar(lineaUsuario);
                    LeerCamino(lineaUsuario);
                }
                trazoUsuario = null;
                if (lineaGuia != null)
                    lineaGuia.gameObject.SetActive(false);
                Cambiar(Estado.Corre);
            }
            return;
        }
        if (trazoUsuario != null)
        {
            // Soltó antes de llegar: se quita esa línea y se intenta otra vez.
            var t = trazoUsuario;
            trazoUsuario = null;
            if (t != null && dibujo.trazos.Contains(t))
                dibujo.EliminarDelTodo(t);
            figuraTamObjetivo = 0f;
            figuraEnLinea = false;
            PonerBurbuja(Tx("Almost! Keep pinching with your left hand until you reach the flag", "¡Casi! No sueltes el pellizco izquierdo hasta llegar a la bandera"));
            return;
        }
        if (control.GestoIzq != ControlManos.Gesto.Ninguno)
        {
            ultimaActividad = Time.time;
            PararPista();
        }
        Pista12();
        ActualizarFigura();
    }

    // El muñeco termina de correr hasta cerca del final de tu línea; ahí se vuelve un títere de verdad.
    void Corre()
    {
        if (lineaUsuario == null || camino.Count < 2)
        {
            EmpezarDemo45();
            return;
        }
        float meta = Mathf.Max(0f, LargoCamino - 0.07f);
        if (!figuraEnLinea)
        {
            figuraEnLinea = true;
            figuraS = Mathf.Max(0f, meta - 0.25f);
            figuraTamObjetivo = 1f;
        }
        MoverFigura(meta, 0.45f);
        bool llego = Mathf.Abs(figuraS - meta) < 0.005f;
        if ((llego && T > 0.6f) || T > 3f)
        {
            Vector3 pies = PuntoCamino(figuraS);
            personaje = titere != null ? titere.CrearPalitoEn(pies, raiz.right, lineaUsuario.id) : null;
            if (personaje == null)
            {
                EmpezarDemo45();
                return;
            }
            figuraTam = 0f;
            figuraTamObjetivo = 0f;
            figura.Poner(Vector3.zero, Vector3.right, Vector3.up, 0f, 0f);
            Chispas(pies + Vector3.up * 0.2f, 10, 0.3f);
            MostrarNumero(3, raiz.InverseTransformPoint(pies + Vector3.up * 0.47f));
            PonerBurbuja(Tx("3. Move your hand and the stick figure will follow you. Make it jump!", "3. Mueve tu mano y el muñeco te seguirá. ¡Hazlo saltar!"));
            movido = 0f;
            teniaDedo = false;
            exito3 = false;
            Cambiar(Estado.Paso3);
        }
    }

    // ==================== Paso 3: el títere ====================

    void Paso3()
    {
        var der = control.Der;
        if (der.valida)
        {
            Vector3 p = der.indice;
            if (teniaDedo)
            {
                Vector3 d = p - dedoPrevio;
                d.y = 0f;
                movido += d.magnitude;
            }
            dedoPrevio = p;
            teniaDedo = true;
        }
        else
        {
            teniaDedo = false;
        }
        if (!exito3 && ((T > 2.5f && movido > 0.4f) || T > 14f))
        {
            exito3 = true;
            exito3En = Time.time;
            Numero n;
            Vector3 donde = numeros.TryGetValue(3, out n) && n.raiz != null ? n.raiz.position : B + Vector3.up * 0.3f;
            Exito(3, donde);
        }
        if (exito3 && Time.time - exito3En > 3f)
            EmpezarDemo45();
    }

    // ==================== Pasos 4 y 5: borrar ====================

    void EmpezarDemo45()
    {
        if (titere != null)
        {
            if (titere.Encendido)
                titere.Parar();
            if (personaje != null)
                titere.BorrarPersonaje(personaje, true);
            if (lineaUsuario != null)
                titere.pisos.Remove(lineaUsuario.id);
        }
        personaje = null;
        QuitarNumeros();
        if (lineaUsuario == null || !dibujo.trazos.Contains(lineaUsuario))
            lineaUsuario = UltimoTrazo();
        if (lineaUsuario == null)
        {
            // No quedó ninguna línea (raro): se dibuja una de ejemplo para poder borrarla.
            lineaUsuario = LineaDeEjemplo();
        }
        if (lineaUsuario != null)
            LeerCamino(lineaUsuario);
        // El muñeco vuelve a estar parado cerca del final de la línea.
        figuraS = Mathf.Max(0f, LargoCamino - 0.07f);
        figuraMov = 0f;
        dirFigura = 1f;
        huyendo = false;
        figuraEnLinea = true;
        figuraTam = 1f;
        figuraTamObjetivo = 1f;
        // Durante la demostración tu línea se esconde y se ve una copia que se va borrando.
        OcultarTrazosTutorial(true);
        if (lineaDemo != null)
        {
            lineaDemo.widthMultiplier = lineaUsuario != null ? Mathf.Max(0.003f, lineaUsuario.ancho * dibujo.EscalaMundo) : 0.005f;
            PonerLinea(lineaDemo, camino, camino.Count);
        }
        Cambiar(Estado.Demo45);
    }

    // Las manos guía enseñan el puño (4) y frotar la línea (5). "completa" = con la línea que se borra y el muñeco que huye.
    bool Demo45(float t, bool completa)
    {
        float alfa = Mathf.Clamp01(t / 0.4f) * (1f - Mathf.Clamp01((t - 5.4f) / 0.5f));
        PonerAlfaGuia(completa ? alfa : alfa * 0.75f);
        float puno = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.9f, t)) * (1f - Mathf.InverseLerp(5.0f, 5.3f, t));
        // Puño izquierdo con el DORSO hacia ti (la palma mira hacia adelante).
        PoseMano.Calcular(puntosIzq, true, L(-0.34f, -0.16f, -0.08f), Dir(0.1f, 1f, 0.25f), Dir(0f, 0.1f, 1f), 0f, puno, 0f);
        manoIzq.Poner(alfa > 0.01f ? puntosIzq : null);

        float largo = LargoCamino;
        Vector3 descanso = L(0.33f, -0.14f, -0.12f);
        Vector3 fin = camino.Count > 0 ? PuntoCamino(largo) : B;
        Vector3 inicio = camino.Count > 0 ? PuntoCamino(0f) : A;
        Vector3 punta;
        float sDedo = largo;
        bool frotando = false;
        if (t < 1.4f)
            punta = descanso;
        else if (t < 2.0f)
            punta = Vector3.Lerp(descanso, fin, Suave((t - 1.4f) / 0.6f));
        else if (t < 4.6f)
        {
            sDedo = largo * (1f - Suave((t - 2.0f) / 2.6f));
            punta = camino.Count > 0 ? PuntoCamino(sDedo) : Vector3.Lerp(fin, inicio, (t - 2f) / 2.6f);
            // un poquito de "frote" (ida y vuelta)
            punta += raiz.right * Mathf.Sin(t * 22f) * 0.006f;
            frotando = true;
        }
        else
        {
            sDedo = 0f;
            punta = inicio;
        }
        PoseMano.ConIndiceEn(puntosDer, false, punta, Dir(-0.1f, 0.2f, 1f), Dir(0f, -1f, 0.2f), 0f, 0f, 1f);
        manoDer.Poner(alfa > 0.01f ? puntosDer : null);
        bool punto = alfa > 0.01f && t >= 1.0f && t < 5.0f;
        if (puntoBorrador != null)
        {
            if (puntoBorrador.gameObject.activeSelf != punto)
                puntoBorrador.gameObject.SetActive(punto);
            if (punto)
                puntoBorrador.position = puntosDer[8];
        }

        if (completa)
        {
            if (t >= 0.5f)
            {
                MostrarNumero(4, new Vector3(-0.34f, 0.04f, -0.06f));
                if (t < 1.4f)
                    PonerBurbuja(Tx("4. Make a fist with your LEFT hand, back of the hand toward you: that's the eraser", "4. Cierra el puño IZQUIERDO con el dorso hacia ti: ese es el borrador"));
            }
            if (t >= 1.4f)
            {
                MostrarNumero(5, new Vector3(0.36f, 0.1f, -0.02f));
                PonerBurbuja(Tx("5. Rub the line with your RIGHT index finger to erase it", "5. Frota la línea con el índice DERECHO para borrarla"));
            }
            if (frotando)
            {
                // La copia de la línea se va borrando detrás del dedo.
                int n = 0;
                while (n < caminoLargo.Count && caminoLargo[n] <= sDedo)
                    n++;
                PonerLinea(lineaDemo, camino, n);
                if (Time.time - ultimaChispa > 0.12f)
                {
                    ultimaChispa = Time.time;
                    Chispas(punta, 2, 0.12f);
                }
            }
            else if (t >= 4.6f && lineaDemo != null)
            {
                lineaDemo.positionCount = 0;
            }
            // El muñeco huye del borrador (corre hacia el inicio y se va).
            if (!huyendo && t > 2f && sDedo - figuraS < 0.12f)
            {
                huyendo = true;
                dirFigura = -1f;
            }
            if (huyendo)
                MoverFigura(-0.2f, 0.6f);
            else
                ActualizarFigura();
        }
        return t >= 5.9f;
    }

    void EntrarPaso4()
    {
        OcultarGuia();
        OcultarTrazosTutorial(false);
        if (lineaDemo != null)
            lineaDemo.positionCount = 0;
        // El muñeco vuelve a aparecer parado cerca del final.
        figuraS = Mathf.Max(0f, LargoCamino - 0.07f);
        figuraMov = 0f;
        dirFigura = -1f;
        huyendo = false;
        figuraTam = 0f;
        figuraTamObjetivo = 1f;
        PonerBurbuja(Tx("4. Make a fist with your LEFT hand, back of the hand toward you: that's the eraser", "4. Cierra el puño IZQUIERDO con el dorso hacia ti: ese es el borrador"));
        Cambiar(Estado.Paso4);
    }

    void Paso4()
    {
        ActualizarFigura();
        if (control.GestoIzq == ControlManos.Gesto.Borrar)
        {
            PararPista();
            Exito(4, control.Izq.medio);
            PonerBurbuja(Tx("5. Rub the line with your RIGHT index finger to erase it", "5. Frota la línea con el índice DERECHO para borrarla"));
            nodosInicio = NodosTutorial();
            borrando = false;
            teniaDedo = false;
            velDedo = 0f;
            Cambiar(Estado.Paso5);
            return;
        }
        if (control.GestoIzq != ControlManos.Gesto.Ninguno)
            ultimaActividad = Time.time;
        Pista45();
    }

    void Paso5()
    {
        var der = control.Der;
        float dt = Mathf.Max(1e-4f, Time.deltaTime);
        if (der.valida)
        {
            if (teniaDedo)
                velDedo = Mathf.Lerp(velDedo, Vector3.Distance(der.indice, dedoPrevio) / dt, 1f - Mathf.Exp(-6f * dt));
            dedoPrevio = der.indice;
            teniaDedo = true;
        }
        else
        {
            teniaDedo = false;
        }
        int nodos = NodosTutorial();
        float distancia;
        float sDedo = ProyectarEnCamino(der.indice, out distancia);
        bool goma = control.GestoIzq == ControlManos.Gesto.Borrar;
        if (goma)
        {
            ultimaActividad = Time.time;
            PararPista();
        }
        bool cerca = goma && der.valida && distancia < 0.08f && Mathf.Abs(sDedo - figuraS) < 0.15f;
        if (!huyendo && (nodos < nodosInicio || cerca))
        {
            huyendo = true;
            dirFigura = sDedo > figuraS ? -1f : 1f;
        }
        if (!borrando && nodos < nodosInicio)
        {
            borrando = true;
            borrandoDesde = Time.time;
        }
        if (huyendo)
        {
            // Corre si borras rápido; camina si borras despacio.
            float vel = velDedo > 0.2f ? 0.6f : 0.22f;
            MoverFigura(dirFigura < 0f ? -0.2f : LargoCamino + 0.2f, vel);
        }
        else
        {
            ActualizarFigura();
        }
        bool listo = nodos == 0 || (nodosInicio > 0 && nodos <= nodosInicio / 2) || (borrando && Time.time - borrandoDesde > 2.5f);
        if (listo)
        {
            Exito(5, der.valida ? der.indice : PuntoCamino(figuraS));
            EntrarFinal();
            return;
        }
        Pista45();
    }

    void EntrarFinal()
    {
        OcultarGuia();
        QuitarNumeros();
        if (fuente != null)
            fuente.PlayOneShot(Sonidos.Tada, 0.9f);
        if (encabezado != null)
        {
            encabezado.text = Tx("You did it!", "¡Lo lograste!");
            Confeti(encabezado.transform.position, 90, 0.9f);
            Chispas(encabezado.transform.position, 20, 0.35f);
        }
        PonerBurbuja(Tx("Now draw whatever you want! See it again: My files > Tutorial", "¡Ahora dibuja lo que quieras! Para verlo otra vez: Mis archivos > Tutorial"));
        figuraTamObjetivo = 0f;
        PlayerPrefs.SetInt(ClaveVisto, 1);
        PlayerPrefs.Save();
        Cambiar(Estado.Final);
    }

    // ==================== Pistas (repetir la demostración si no haces nada) ====================

    void Pista12()
    {
        if (pistaDesde < 0f && Time.time - ultimaActividad > 8f)
            pistaDesde = Time.time;
        if (pistaDesde >= 0f && Demo12(Time.time - pistaDesde, false))
            PararPista();
    }

    void Pista45()
    {
        if (pistaDesde < 0f && Time.time - ultimaActividad > 8f)
            pistaDesde = Time.time;
        if (pistaDesde >= 0f)
        {
            if (Demo45(Time.time - pistaDesde, false))
                PararPista();
        }
    }

    void PararPista()
    {
        if (pistaDesde >= 0f)
            OcultarGuia();
        pistaDesde = -1f;
        ultimaActividad = Time.time;
    }

    void OcultarGuia()
    {
        if (manoIzq != null) manoIzq.Poner(null);
        if (manoDer != null) manoDer.Poner(null);
        if (puntoBorrador != null && puntoBorrador.gameObject.activeSelf)
            puntoBorrador.gameObject.SetActive(false);
    }

    void PonerAlfaGuia(float a)
    {
        if (matGuia == null)
            return;
        matGuia.SetColor("_BaseColor", new Color(0.45f, 0.75f, 1f, 0.6f * Mathf.Clamp01(a)));
    }

    // ==================== El camino del muñeco (tu línea, en el mundo) ====================

    void LeerCamino(Trazo t)
    {
        camino.Clear();
        caminoLargo.Clear();
        if (t == null)
            return;
        var curva = t.curva;
        float acum = 0f;
        for (int i = 0; i < curva.Count; i++)
        {
            Vector3 w = dibujo.transform.TransformPoint(curva[i]);
            if (camino.Count > 0)
            {
                float d = Vector3.Distance(camino[camino.Count - 1], w);
                if (d < 1e-4f)
                    continue;
                acum += d;
            }
            camino.Add(w);
            caminoLargo.Add(acum);
        }
    }

    float LargoCamino => caminoLargo.Count > 0 ? caminoLargo[caminoLargo.Count - 1] : 0f;

    // Punto a "s" metros del inicio. Fuera de la línea sigue derecho (para salir corriendo).
    Vector3 PuntoCamino(float s)
    {
        if (camino.Count == 0)
            return raiz != null ? raiz.position : Vector3.zero;
        if (camino.Count == 1)
            return camino[0];
        if (s <= 0f)
            return camino[0] - Horizontal(camino[1] - camino[0]) * (-s);
        float largo = LargoCamino;
        if (s >= largo)
        {
            int n = camino.Count;
            return camino[n - 1] + Horizontal(camino[n - 1] - camino[n - 2]) * (s - largo);
        }
        for (int i = 1; i < camino.Count; i++)
        {
            if (caminoLargo[i] >= s)
            {
                float tramo = caminoLargo[i] - caminoLargo[i - 1];
                float u = tramo > 1e-6f ? (s - caminoLargo[i - 1]) / tramo : 0f;
                return Vector3.Lerp(camino[i - 1], camino[i], u);
            }
        }
        return camino[camino.Count - 1];
    }

    Vector3 TangenteCamino(float s)
    {
        if (camino.Count < 2)
            return raiz != null ? raiz.right : Vector3.right;
        Vector3 a = PuntoCamino(Mathf.Clamp(s - 0.02f, 0f, LargoCamino));
        Vector3 b = PuntoCamino(Mathf.Clamp(s + 0.02f, 0f, LargoCamino));
        Vector3 d = Horizontal(b - a);
        return d.sqrMagnitude > 1e-8f ? d : Horizontal(camino[camino.Count - 1] - camino[0]);
    }

    // Dónde cae un punto sobre la línea (metros desde el inicio) y a qué distancia está.
    float ProyectarEnCamino(Vector3 p, out float distancia)
    {
        distancia = float.MaxValue;
        float mejor = 0f;
        for (int i = 1; i < camino.Count; i++)
        {
            Vector3 a = camino[i - 1], b = camino[i];
            Vector3 ab = b - a;
            float l2 = ab.sqrMagnitude;
            float u = l2 > 1e-10f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2) : 0f;
            float d = Vector3.Distance(p, a + ab * u);
            if (d < distancia)
            {
                distancia = d;
                mejor = Mathf.Lerp(caminoLargo[i - 1], caminoLargo[i], u);
            }
        }
        return mejor;
    }

    void MoverFigura(float meta, float velocidad)
    {
        float dt = Time.deltaTime;
        float paso = Mathf.Clamp(meta - figuraS, -velocidad * dt, velocidad * dt);
        figuraS += paso;
        float vel = Mathf.Abs(paso) / Mathf.Max(1e-4f, dt);
        float objetivo = vel < 0.03f ? 0f : vel < 0.3f ? 1f : 2f;
        figuraMov = Mathf.Lerp(figuraMov, objetivo, 1f - Mathf.Exp(-8f * dt));
        if (paso > 1e-5f) dirFigura = 1f;
        else if (paso < -1e-5f) dirFigura = -1f;
        if (figura != null)
            figura.Avanzar(Mathf.Abs(paso), figuraMov);
        ActualizarFigura();
    }

    void ActualizarFigura()
    {
        if (figura == null)
            return;
        figuraTam = Mathf.MoveTowards(figuraTam, figuraTamObjetivo, Time.deltaTime * 4f);
        // Fuera de la línea (saliendo corriendo) se va achicando hasta desaparecer.
        float afuera = Mathf.Max(-figuraS, figuraS - LargoCamino, 0f);
        float tam = figuraTam * Mathf.Clamp01(1f - afuera / 0.15f);
        if (tam <= 0.01f || camino.Count < 2)
        {
            figura.Poner(Vector3.zero, Vector3.right, Vector3.up, 0f, 0f);
            return;
        }
        Vector3 suelo = PuntoCamino(figuraS) + Vector3.up * 0.003f;
        figura.Poner(suelo, TangenteCamino(figuraS) * dirFigura, Vector3.up, figuraMov, tam);
    }

    // Tu línea queda en un plano frente a ti (así el títere la pisa bien).
    void Aplanar(Trazo t)
    {
        if (t == null || t.nodos.Count < 2 || raiz == null)
            return;
        Vector3 n = raiz.forward;
        Vector3 fin = dibujo.transform.TransformPoint(t.nodos[t.nodos.Count - 1]);
        for (int i = 0; i < t.nodos.Count; i++)
        {
            Vector3 w = dibujo.transform.TransformPoint(t.nodos[i]);
            w -= n * Vector3.Dot(w - fin, n);
            t.nodos[i] = dibujo.transform.InverseTransformPoint(w);
        }
        t.Reconstruir();
    }

    Trazo UltimoTrazo()
    {
        for (int i = dibujo.trazos.Count - 1; i >= 0; i--)
        {
            var t = dibujo.trazos[i];
            if (t != null && Dibujo.Editable(t) && t.visibleAnim && t.nodos.Count >= 2)
                return t;
        }
        return null;
    }

    // Una línea de ejemplo (la ola de la demostración), por si no quedó ninguna.
    Trazo LineaDeEjemplo()
    {
        var d = new DatosTrazo { ancho = dibujo.AnchoNuevoLocal() };
        for (int i = 0; i <= 8; i++)
            d.nodos.Add(dibujo.transform.InverseTransformPoint(raiz.TransformPoint(Curva(i / 8f))));
        dibujo.GuardarParaDeshacer();
        return dibujo.AgregarTrazo(d);
    }

    // Todos los nodos que quedan de las líneas del tutorial (al borrar, bajan).
    int NodosTutorial()
    {
        int n = 0;
        foreach (var t in dibujo.trazos)
            if (t != null && Dibujo.Editable(t) && t.visibleAnim)
                n += t.nodos.Count;
        return n;
    }

    void OcultarTrazosTutorial(bool ocultar)
    {
        foreach (var t in dibujo.trazos)
        {
            if (t == null)
                continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                r.forceRenderingOff = ocultar;
        }
    }

    // ==================== Números, viñeta y decorado ====================

    void MostrarNumero(int n, Vector3 local)
    {
        if (numeros.ContainsKey(n) || raiz == null)
            return;
        var num = new Numero { desde = Time.time };
        num.raiz = new GameObject("Numero" + n).transform;
        num.raiz.SetParent(raiz, false);
        num.raiz.localPosition = local;
        Disco(num.raiz, materialNegro, 0.052f, new Vector3(0f, 0f, 0.001f));
        num.relleno = Disco(num.raiz, materialAzul, 0.046f, Vector3.zero).GetComponent<Renderer>();
        var texto = CrearTexto(num.raiz, n.ToString(), new Vector3(0f, 0.001f, -0.002f), new Vector2(0.04f, 0.04f), 0.5f, Color.white);
        texto.outlineWidth = 0.15f;
        texto.outlineColor = new Color32(20, 20, 40, 255);
        num.raiz.localScale = Vector3.zero;
        numeros[n] = num;
        Pasito(0.15f);
    }

    void MarcarNumero(int n)
    {
        Numero num;
        if (numeros.TryGetValue(n, out num) && num.relleno != null && materialVerde != null)
            num.relleno.sharedMaterial = materialVerde;
    }

    void QuitarNumeros()
    {
        foreach (var par in numeros)
            if (par.Value.raiz != null)
                Destroy(par.Value.raiz.gameObject);
        numeros.Clear();
    }

    void ActualizarNumeros()
    {
        foreach (var par in numeros)
        {
            var num = par.Value;
            if (num.raiz == null)
                continue;
            float k = Mathf.Clamp01((Time.time - num.desde) / 0.35f);
            float esc = k >= 1f ? 1f : 1f - Mathf.Exp(-5f * k) * Mathf.Cos(9f * k);
            num.raiz.localScale = Vector3.one * esc;
        }
    }

    void PonerBurbuja(string texto)
    {
        if (textoBurbuja != null && textoBurbuja.text != texto)
            textoBurbuja.text = texto;
    }

    void ActualizarDecorado()
    {
        float t = Time.time;
        if (puntoA != null)
        {
            bool ver = estado == Estado.Demo12 || estado == Estado.Paso1 || estado == Estado.Paso2;
            if (puntoA.gameObject.activeSelf != ver)
                puntoA.gameObject.SetActive(ver);
            puntoA.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(t * 5f));
        }
        if (bandera != null)
        {
            bool ver = estado == Estado.Demo12 || estado == Estado.Paso1 || estado == Estado.Paso2;
            if (bandera.gameObject.activeSelf != ver)
                bandera.gameObject.SetActive(ver);
            if (tela != null)
                tela.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 3.2f) * 22f, Mathf.Sin(t * 2.1f) * 4f);
        }
        if (encabezado != null)
            encabezado.transform.localPosition = new Vector3(0f, 0.3f + Mathf.Sin(t * 1.6f) * 0.005f, 0.12f);
    }

    // ==================== Celebración ====================

    // Chulito verde + papelitos + "¡ding!" (y el número se pone verde).
    void Exito(int paso, Vector3 donde)
    {
        MarcarNumero(paso);
        if (fuente != null)
            fuente.PlayOneShot(Sonidos.Ding, 0.8f);
        Confeti(donde, 45, 0.6f);
        Chispas(donde, 12, 0.25f);
        if (control == null || control.Cabeza == null)
            return;
        var raizCheck = new GameObject("Chulito").transform;
        raizCheck.SetParent(transform, false);
        Vector3 haciaTi = (control.Cabeza.position - donde).normalized;
        raizCheck.position = donde + haciaTi * 0.04f + Vector3.up * 0.05f;
        raizCheck.rotation = Quaternion.LookRotation(raizCheck.position - control.Cabeza.position, Vector3.up);
        Disco(raizCheck, materialNegro, 0.078f, new Vector3(0f, 0f, 0.001f));
        Disco(raizCheck, materialVerde, 0.07f, Vector3.zero);
        Palo(raizCheck, new Vector2(-0.017f, 0.001f), new Vector2(-0.005f, -0.012f));
        Palo(raizCheck, new Vector2(-0.005f, -0.012f), new Vector2(0.019f, 0.014f));
        raizCheck.localScale = Vector3.zero;
        efectos.Add(new Efecto { t = raizCheck, desde = Time.time });
    }

    void Palo(Transform padre, Vector2 a, Vector2 b)
    {
        var p = Pieza(PrimitiveType.Cube, padre, materialBlanco);
        Vector2 d = b - a;
        const float grueso = 0.007f;
        p.transform.localPosition = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, -0.003f);
        p.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        p.transform.localScale = new Vector3(d.magnitude + grueso, grueso, 0.003f);
    }

    void ActualizarEfectos()
    {
        for (int i = efectos.Count - 1; i >= 0; i--)
        {
            var e = efectos[i];
            float t = Time.time - e.desde;
            if (e.t == null || t > 1.7f)
            {
                if (e.t != null)
                    Destroy(e.t.gameObject);
                efectos.RemoveAt(i);
                continue;
            }
            float k = Mathf.Clamp01(t / 0.35f);
            float esc = k >= 1f ? 1f : 1f - Mathf.Exp(-5f * k) * Mathf.Cos(9f * k);
            esc *= 1f - Mathf.Clamp01((t - 1.35f) / 0.35f);
            e.t.localScale = Vector3.one * esc;
            e.t.position += Vector3.up * 0.02f * Time.deltaTime;
        }
    }

    void Confeti(Vector3 donde, int cantidad, float fuerza)
    {
        if (confeti == null)
            return;
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < cantidad; i++)
        {
            ep.position = donde + Random.insideUnitSphere * 0.02f;
            ep.velocity = Random.insideUnitSphere * fuerza + Vector3.up * fuerza * 0.9f;
            ep.startSize = Random.Range(0.006f, 0.013f);
            ep.startLifetime = Random.Range(1.2f, 1.9f);
            ep.startColor = ColoresConfeti[Random.Range(0, ColoresConfeti.Length)];
            ep.rotation = Random.Range(0f, 360f);
            ep.angularVelocity = Random.Range(-400f, 400f);
            confeti.Emit(ep, 1);
        }
    }

    void Chispas(Vector3 donde, int cantidad, float fuerza)
    {
        if (chispas == null)
            return;
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < cantidad; i++)
        {
            ep.position = donde + Random.insideUnitSphere * 0.015f;
            ep.velocity = Random.insideUnitSphere * fuerza;
            ep.startSize = Random.Range(0.012f, 0.026f);
            ep.startLifetime = Random.Range(0.5f, 1f);
            ep.startColor = Random.value < 0.5f ? new Color(1f, 0.82f, 0.25f) : ColoresConfeti[Random.Range(0, ColoresConfeti.Length)];
            ep.rotation = Random.Range(0f, 90f);
            ep.angularVelocity = Random.Range(-90f, 90f);
            chispas.Emit(ep, 1);
        }
    }

    void Pasito(float volumen)
    {
        if (fuentePasos == null)
            return;
        fuentePasos.pitch = Random.Range(0.85f, 1.15f);
        fuentePasos.PlayOneShot(Sonidos.Paso, volumen);
    }

    ParticleSystem CrearParticulas(string nombre, Material material, float gravedad, float frenado)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = gravedad;
        main.maxParticles = 800;
        main.startSpeed = 0f;
        var emision = ps.emission;
        emision.enabled = false;
        var forma = ps.shape;
        forma.enabled = false;
        var colores = ps.colorOverLifetime;
        colores.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
        colores.color = new ParticleSystem.MinMaxGradient(g);
        var limite = ps.limitVelocityOverLifetime;
        limite.enabled = true;
        limite.limit = 20f;
        limite.drag = frenado;
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (material != null)
            r.sharedMaterial = material;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        ps.Play();
        return ps;
    }

    // ==================== Ayudas ====================

    Vector3 L(float x, float y, float z)
    {
        return raiz.TransformPoint(new Vector3(x, y, z));
    }

    Vector3 Dir(float x, float y, float z)
    {
        return raiz.TransformDirection(new Vector3(x, y, z)).normalized;
    }

    static float Suave(float u)
    {
        u = Mathf.Clamp01(u);
        return u * u * (3f - 2f * u);
    }

    static Vector3 Horizontal(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-8f ? v.normalized : Vector3.forward;
    }

    static TextMeshPro CrearTexto(Transform padre, string texto, Vector3 local, Vector2 tam, float maximo, Color color)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        go.transform.localPosition = local;
        var t = go.AddComponent<TextMeshPro>();
        t.text = texto;
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = maximo;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.rectTransform.sizeDelta = tam;
        return t;
    }

    static void Pintar(Renderer r, Material m)
    {
        if (r == null)
            return;
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static GameObject Pieza(PrimitiveType tipo, Transform padre, Material m)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        Pintar(go.GetComponent<Renderer>(), m);
        return go;
    }

    static GameObject Disco(Transform padre, Material m, float diametro, Vector3 local)
    {
        return Malla(padre, "Disco", ControlManos.MallaDisco(), m, local, diametro);
    }

    static GameObject Malla(Transform padre, string nombre, Mesh malla, Material m, Vector3 local, float escala = 1f)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = local;
        go.transform.localScale = Vector3.one * escala;
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        Pintar(go.AddComponent<MeshRenderer>(), m);
        return go;
    }

    LineRenderer CrearLinea(string nombre, Material m, float ancho)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        var l = go.AddComponent<LineRenderer>();
        l.useWorldSpace = true;
        l.widthMultiplier = ancho;
        l.numCapVertices = 4;
        l.numCornerVertices = 2;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        l.receiveShadows = false;
        if (m != null)
            l.sharedMaterial = m;
        return l;
    }

    static void PonerLinea(LineRenderer l, List<Vector3> puntos, int cuantos)
    {
        if (l == null)
            return;
        cuantos = Mathf.Clamp(cuantos, 0, puntos.Count);
        if (cuantos < 2)
        {
            l.positionCount = 0;
            return;
        }
        l.positionCount = cuantos;
        for (int i = 0; i < cuantos; i++)
            l.SetPosition(i, puntos[i]);
    }

    // Viñeta de cómic: rectángulo redondeado con una colita abajo (hacia la escena).
    // "extra" agranda la colita (para el borde negro).
    static Mesh MallaVineta(float w, float h, float r, float extra)
    {
        var contorno = new List<Vector3>();
        float x0 = -w * 0.5f, x1 = w * 0.5f, y0 = -h * 0.5f, y1 = h * 0.5f;
        const int pasos = 6;
        // Abajo, de izquierda a derecha, con la colita.
        contorno.Add(new Vector3(x0 + r, y0, 0f));
        float colaX = 0.06f;
        float colaMedia = 0.02f + extra;
        contorno.Add(new Vector3(colaX - colaMedia, y0, 0f));
        contorno.Add(new Vector3(colaX + 0.05f + extra * 0.6f, y0 - 0.045f - extra * 1.4f, 0f));
        contorno.Add(new Vector3(colaX + colaMedia, y0, 0f));
        contorno.Add(new Vector3(x1 - r, y0, 0f));
        Esquina(contorno, new Vector2(x1 - r, y0 + r), r, -90f, 0f, pasos);
        Esquina(contorno, new Vector2(x1 - r, y1 - r), r, 0f, 90f, pasos);
        Esquina(contorno, new Vector2(x0 + r, y1 - r), r, 90f, 180f, pasos);
        Esquina(contorno, new Vector2(x0 + r, y0 + r), r, 180f, 270f, pasos);

        var v = new List<Vector3> { Vector3.zero };
        v.AddRange(contorno);
        var tri = new List<int>();
        for (int i = 0; i < contorno.Count; i++)
        {
            tri.Add(0);
            tri.Add(1 + i);
            tri.Add(1 + (i + 1) % contorno.Count);
        }
        var m = new Mesh { name = "Vineta" };
        m.SetVertices(v);
        m.SetTriangles(tri, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static void Esquina(List<Vector3> lista, Vector2 centro, float r, float desdeGrados, float hastaGrados, int pasos)
    {
        for (int i = 0; i <= pasos; i++)
        {
            float a = Mathf.Lerp(desdeGrados, hastaGrados, i / (float)pasos) * Mathf.Deg2Rad;
            lista.Add(new Vector3(centro.x + Mathf.Cos(a) * r, centro.y + Mathf.Sin(a) * r, 0f));
        }
    }
}

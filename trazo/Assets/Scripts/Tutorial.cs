using System.Collections.Generic;
using TMPro;
using UnityEngine;

// JCartoons: pantalla de título + tutorial con MANOS GUÍA.
//  - Al abrir la app: "JCartoons" (letras con volumen 3D) aparece letra por letra con música de arpa mágica,
//    un muñequito corre saltando sobre las letras y todo se desvanece despacio sobre la hoja en blanco.
//  - La primera vez sigue el tutorial ("First time?"). Unas manos guía enseñan cada paso (tus manos se
//    esconden mientras) y luego es tu turno; cada paso bien hecho se celebra:
//    1. OK con la izquierda y mantenerlo (lápiz). Cae una gota de tinta que se vuelve el punto A.
//    2. Índice derecho: desde el punto A (antes no dibuja) hasta la bandera. A mitad aparece un muñeco que corre.
//    3. Abrir los dedos = dejar de dibujar.
//    4. El muñeco sigue tu mano, siempre sobre TU línea (como un tren en sus rieles). Next para seguir.
//    5. Puño izquierdo (con un giro de muñeca de lado y de vuelta) = borrador.
//    6. Frotar la línea: los nodos estallan y el muñeco huye en pánico, siempre a 2 cm de tu dedo.
//    7. Pulgar + medio izquierdos (nodos) y arrastrar un nodo con la derecha.
//    8. Deshacer: puño izquierdo con el pulgar a la izquierda y tocar la diana roja.
//  - Cada OK (cerrar o abrir) toca una nota del arpa del título. Si pasan 8 segundos sin que hagas nada,
//    las manos guía lo repiten. "Skip" lo termina. Mis archivos > Tutorial lo repite.
//  - Tu dibujo se aparta antes y vuelve igual al terminar.
public class Tutorial : MonoBehaviour
{
    public static Tutorial Instancia { get; private set; }
    // El tutorial está en marcha (no cuenta el título).
    public static bool EnCurso => Instancia != null && Instancia.estado != Estado.Nada && Instancia.estado != Estado.Titulo;

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
    [Tooltip("Relleno de las viñetas (shader TrazoVR/Trama: amarillo con puntitos)")]
    public Material materialTrama;
    public Material materialNegro, materialBlanco, materialVerde, materialAzul, materialRojo;
    [Tooltip("La línea guía (gris transparente)")]
    public Material materialGuiaLinea;
    public Material materialBoton, materialBotonMarcado;
    [Tooltip("Letra de cómic para las viñetas (Bangers). Si falta, usa la normal")]
    public TMP_FontAsset fuenteComic;

    const string ClaveVisto = "jcartoons_tutorial_visto";
    const string Nombre = "JCartoons";
    const float MitadLinea = 0.32f;     // de la bandera al centro (metros)
    const float CorrerDerecha = 0.08f;  // todo el tutorial un poco a la derecha
    const float AnchoMuneco = 0.0035f;  // grosor de cada hebra del muñeco
    const float EscalaMuneco = 0.45f;   // el muñeco del tutorial (unos 18 cm)
    const float AnchoVineta = 0.24f, AltoVineta = 0.075f;

    // Lugares (en el marco del tutorial: x derecha, y arriba, z adelante)
    static readonly Vector3 ManoIzqPecho = new Vector3(-0.11f, -0.16f, -0.14f);   // a la altura del pecho izquierdo
    static readonly Vector3 ManoCentro = new Vector3(0f, -0.15f, -0.14f);          // centro del pecho
    static readonly Vector3 ManoDerDescanso = new Vector3(0.3f, -0.18f, -0.12f);
    static readonly Vector3 VinetaIzq = new Vector3(-0.12f, -0.255f, -0.1f);       // debajo de la mano izquierda
    static readonly Vector3 VinetaCentro = new Vector3(0f, -0.245f, -0.1f);        // debajo de la mano del centro
    static readonly Vector3 VinetaLinea = new Vector3(0f, 0.135f, 0.03f);          // encima de la línea
    static readonly Vector3 VinetaMuneco = new Vector3(0.2f, 0.2f, 0.02f);         // encima del muñeco

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

    enum Estado
    {
        Nada, Titulo, Demo1, Paso1, Gota, Demo2, Paso2, Paso3, Corre, Paso4,
        Demo56, Paso5, Paso6, Regresa, Demo7, Paso7, Demo8, Paso8, Demo9, Paso9, Pausa, Final
    }
    Estado estado = Estado.Nada;
    float desde;
    float T => Time.time - desde;
    bool tituloHecho;
    bool primeraVez;

    // ---------- Textos (English first) ----------
    static string Tx(string en, string es) { return Idioma.Ingles ? en : es; }
    static string Texto1 => Tx("1. Make the OK sign with your LEFT hand (thumb + INDEX, the yellow finger) and HOLD it: the pencil turns on",
                               "1. Haz el signo de OK con la mano IZQUIERDA (pulgar + ÍNDICE, el dedo amarillo) y MANTENLO: se activa el lápiz");
    static string Texto2 => Tx("2. Keep the OK, touch the blue dot with your RIGHT index finger and draw to the flag",
                               "2. Sin soltar el OK, toca el punto azul con el índice DERECHO y dibuja hasta la bandera");
    static string Texto3 => Tx("3. You made it! Now OPEN your thumb and index finger: that stops the drawing",
                               "3. ¡Llegaste! Ahora ABRE el pulgar y el índice: así dejas de dibujar");
    static string TextoCasi => Tx("Almost! Keep the OK sign until you reach the flag", "¡Casi! Mantén el OK hasta llegar a la bandera");
    static string Texto4 => Tx("4. Move your hand: the stick figure runs along YOUR line. Flick up to jump! Tap NEXT when you're ready",
                               "4. Mueve tu mano: el muñeco corre por TU línea. ¡Golpe hacia arriba = saltar! Toca SIGUIENTE cuando quieras");
    static string Texto5 => Tx("5. Make a FIST with your LEFT hand and twist your wrist sideways and back: that's the eraser",
                               "5. Cierra el PUÑO IZQUIERDO y gira la muñeca de lado y de vuelta: ese es el borrador");
    static string Texto6 => Tx("6. Rub the line with your RIGHT index finger: the nodes pop!",
                               "6. Frota la línea con el índice DERECHO: ¡los nodos estallan!");
    static string Texto7 => Tx("7. Join your thumb with the MIDDLE finger (the yellow one, next to the index) on your left hand and hold: the nodes appear",
                               "7. Junta el pulgar con el dedo MEDIO (el amarillo, al lado del índice) de la mano izquierda y mantenlo: aparecen los nodos");
    static string Texto7b => Tx("Now pinch a node with your RIGHT hand and drag it", "Ahora pellizca un nodo con la mano DERECHA y arrástralo");
    static string Texto9 => Tx("9. Make the OK with BOTH hands: move them apart or together = size; turn them like a steering wheel = rotate",
                               "9. Haz el OK con las DOS manos: sepáralas o júntalas = tamaño; gíralas como un volante = rotar");
    static string Texto8 => Tx("8. Changed your mind? LEFT fist with the THUMB pointing left, then touch the red target: undo!",
                               "8. ¿Cambiaste de idea? Puño IZQUIERDO con el PULGAR hacia la izquierda y toca la diana roja: ¡deshacer!");

    // ---------- Título ----------
    Transform titulo;
    Quaternion tituloGiro;
    readonly List<TextMeshPro> letras = new List<TextMeshPro>();
    readonly List<List<TextMeshPro>> letrasFondo = new List<List<TextMeshPro>>();
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
    Material matGuia, matResaltado;
    TextMeshPro encabezado;
    Transform puntoA, bandera, tela, puntoBorrador, gota;
    LineRenderer lineaGuia, lineaDemo;
    readonly List<Vector3> curvaDemo = new List<Vector3>();
    MunecoGuia figura;
    ParticleSystem confeti, chispas;
    AudioSource fuente, fuentePasos;
    BotonTocable btnSaltar, btnSiguiente;
    bool siguientePedido;

    // Viñeta de cómic (relleno con trama, borde que tiembla y sombra de verdad, más atrás en 3D).
    Transform vineta;
    VinetaComic vinetaComic;
    Vector3 vinetaObjetivo;
    bool vinetaLista;

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

    // Nodos de la demostración (circulitos que estallan o se arrastran).
    class NodoDemo
    {
        public Transform t;
        public Vector3 pos;
        public float s;
        public bool estallo;
    }
    readonly List<NodoDemo> nodosDemo = new List<NodoDemo>();

    // La línea del usuario (y el camino del muñeco sobre ella), en el mundo.
    Trazo trazoUsuario, lineaUsuario;
    DatosTrazo lineaGuardada;   // la línea antes de borrarla (vuelve sola después del paso 6)
    readonly List<Vector3> camino = new List<Vector3>();
    readonly List<float> caminoLargo = new List<float>();
    readonly List<Vector3> caminoAntes = new List<Vector3>(); // la línea antes de mover el nodo (para deshacer)
    float figuraS, figuraMov, figuraTam, figuraTamObjetivo, dirFigura = 1f;
    float figuraAltura, figuraAgachar, figuraPanico;
    bool figuraEnLinea;

    // Pistas, pasos y celebraciones
    float pistaDesde = -1f, ultimaActividad;
    bool pinzaGuiaCerrada, exito4, borrando, gestoNodos, metaVisible, demoAcerto, calibrado;
    float movido, borrandoDesde, velDedo, ultimaChispa, metaDesde, saltoDesde = -1f, xCal, yCal, sCal, yPrevio, velY;
    Vector3 dedoPrevio, gotaDesde, demoDiana;
    bool teniaDedo;
    int nodosInicio, deshechoInicio, notaArpa;
    float dirHuida;
    int notaDemo = -1;
    float recorridoMelodia;
    bool huidaFija;
    readonly Dictionary<int, List<Vector3>> nodosPaso7 = new Dictionary<int, List<Vector3>>();

    void Awake()
    {
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
        if (control != null && control.permitirEmpezarLinea != null)
            control.permitirEmpezarLinea = null;
        if (raizManos != null)
            Destroy(raizManos);
        if (matGuia != null)
            Destroy(matGuia);
        if (matResaltado != null)
            Destroy(matResaltado);
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
        // Cada OK (cerrar o abrir) toca la siguiente nota del arpa del título.
        if (estado != Estado.Titulo && control.Izq.valida && (control.Izq.empezoPellizco || control.Izq.soltoPellizco))
            NotaArpa(0.45f);

        switch (estado)
        {
            case Estado.Titulo: ActualizarTitulo(); break;
            case Estado.Demo1:
                if (Demo1(T, true))
                    EntrarPaso1();
                break;
            case Estado.Paso1: Paso1(); break;
            case Estado.Gota: Gota(); break;
            case Estado.Demo2:
                if (Demo2(T, true))
                    EntrarPaso2();
                break;
            case Estado.Paso2: Paso2(); break;
            case Estado.Paso3: Paso3(); break;
            case Estado.Corre: Corre(); break;
            case Estado.Paso4: Paso4(); break;
            case Estado.Demo56:
                if (Demo56(T, true))
                    EntrarPaso5();
                break;
            case Estado.Paso5: Paso5(); break;
            case Estado.Paso6: Paso6(); break;
            case Estado.Regresa: Regresa(); break;
            case Estado.Demo7:
                if (Demo7(T, true))
                    EntrarPaso7();
                break;
            case Estado.Paso7: Paso7(); break;
            case Estado.Demo8:
                if (Demo8(T, true))
                    EntrarPaso8();
                break;
            case Estado.Paso8: Paso8(); break;
            case Estado.Demo9:
                if (Demo9(T, true))
                    EntrarPaso9();
                break;
            case Estado.Paso9: Paso9(); break;
            case Estado.Pausa:
                // Un respiro entre pasos: se ve que algo terminó antes de que empiece lo siguiente.
                ActualizarFigura();
                if (T >= pausaDura)
                {
                    var siguiente = trasPausa;
                    trasPausa = null;
                    if (siguiente != null)
                        siguiente();
                }
                break;
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
            ActualizarVineta();
        }
        ActualizarEfectos();
    }

    bool Bloquea(Estado e)
    {
        return e == Estado.Demo1 || e == Estado.Demo2 || e == Estado.Corre || e == Estado.Demo56 || e == Estado.Regresa
               || e == Estado.Demo7 || e == Estado.Demo8 || e == Estado.Demo9 || e == Estado.Pausa || (e == Estado.Titulo && primeraVez);
    }

    static bool Oculta(Estado e)
    {
        return e == Estado.Demo1 || e == Estado.Demo2 || e == Estado.Demo56 || e == Estado.Demo7 || e == Estado.Demo8 || e == Estado.Demo9;
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

    System.Action trasPausa;
    float pausaDura;

    // Entre un paso y el siguiente: se van la viñeta y las manos guía, un respiro, y luego sigue.
    void Pausa(System.Action siguiente, float duracion = 1.5f)
    {
        OcultarGuia();
        OcultarVineta();
        trasPausa = siguiente;
        pausaDura = duracion;
        Cambiar(Estado.Pausa);
    }

    void OcultarVineta()
    {
        if (vineta != null && vineta.gameObject.activeSelf)
            vineta.gameObject.SetActive(false);
    }

    void NotaArpa(float volumen)
    {
        if (fuente == null)
            return;
        fuente.PlayOneShot(Sonidos.Nota(notaArpa), volumen);
        notaArpa++;
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
        tituloGiro = Quaternion.LookRotation(adelante, Vector3.up);
        titulo.SetPositionAndRotation(pos, tituloGiro);

        const float tam = 1.15f;
        const int capasFondo = 7;
        float total = 0f;
        var anchos = new float[Nombre.Length];
        for (int i = 0; i < Nombre.Length; i++)
        {
            Color color = ColoresLetras[i % ColoresLetras.Length];
            var t = CrearTexto(titulo, Nombre[i].ToString(), Vector3.zero, new Vector2(0.3f, 0.3f), tam, color);
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
            // Volumen 3D: copias más oscuras hacia atrás (y un poquito hacia abajo), como letras de bloque.
            var fondo = new List<TextMeshPro>();
            for (int k = 1; k <= capasFondo; k++)
            {
                Color oscuro = Color.Lerp(color, new Color(0.12f, 0.08f, 0.2f), 0.45f + 0.06f * k);
                var c = CrearTexto(t.transform, Nombre[i].ToString(), new Vector3(0.0006f * k, -0.0011f * k, 0.0045f * k), new Vector2(0.3f, 0.3f), tam, oscuro);
                c.enableAutoSizing = false;
                c.fontSize = tam;
                c.outlineWidth = 0.22f;
                c.outlineColor = new Color32(30, 20, 50, 255);
                fondo.Add(c);
            }
            letrasFondo.Add(fondo);
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

        munecoTitulo = new MunecoGuia(titulo, materialNegro, 0.0048f);
        munecoTitulo.escala = 0.22f;
        munecoTitulo.temblor = 0.003f;
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
        // Se mece un poquito de lado a lado: así se nota la profundidad de las letras.
        titulo.rotation = tituloGiro * Quaternion.Euler(Mathf.Sin(t * 0.7f) * 3f, Mathf.Sin(t * 0.9f) * 11f, 0f);
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
            float esc = Rebote(k);
            float caer = (1f - k) * (1f - k) * 0.08f;
            float bailar = k >= 1f ? Mathf.Sin(Time.time * 2.2f + i * 0.7f) * 0.004f : 0f;
            l.transform.localPosition = letrasPos[i] + new Vector3(0f, caer + bailar, 0f);
            l.transform.localRotation = Quaternion.Euler(0f, 0f, (1f - k) * -30f);
            l.transform.localScale = Vector3.one * esc * (1f + (1f - f) * 0.15f);
            float alfa = Mathf.Clamp01(k * 3f) * f;
            l.alpha = alfa;
            foreach (var c in letrasFondo[i])
                c.alpha = alfa;
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

    void QuitarTitulo()
    {
        if (munecoTitulo != null)
            munecoTitulo.Destruir();
        munecoTitulo = null;
        if (titulo != null)
            Destroy(titulo.gameObject);
        titulo = null;
        letras.Clear();
        letrasFondo.Clear();
        letrasPos.Clear();
        letrasArriba.Clear();
        letrasChispa.Clear();
        eslogan = null;
    }

    void TerminarTitulo()
    {
        QuitarTitulo();
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
            QuitarTitulo();   // si lo piden durante el título, el título se va de una vez
        else if (estado != Estado.Nada)
            Terminar();
        if (titere != null && titere.Encendido)
            titere.Parar();
        dibujo.ApartarParaTutorial();
        Armar();
        // En el tutorial, una línea solo empieza en el punto A (y solo en el paso 2).
        control.permitirEmpezarLinea = p => estado == Estado.Paso2 && raiz != null && Vector3.Distance(p, A) < 0.045f;
        Cambiar(Estado.Demo1);
    }

    void Terminar()
    {
        if (control != null)
            control.soloNodosAlBorrar = false;
        PlayerPrefs.SetInt(ClaveVisto, 1);
        PlayerPrefs.Save();
        primeraVez = false;
        if (control != null)
            control.permitirEmpezarLinea = null;
        trazoUsuario = null;
        lineaUsuario = null;
        lineaGuardada = null;
        OcultarFlecha();
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
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized;
        raiz = new GameObject("Tutorial").transform;
        raiz.SetParent(transform, false);
        raiz.SetPositionAndRotation(cab.position + adelante * 0.45f - Vector3.up * 0.12f + derecha * CorrerDerecha,
                                    Quaternion.LookRotation(adelante, Vector3.up));

        // "First time?" arriba, con letras de colores.
        encabezado = CrearTexto(raiz, Tx("First time?", "¿Primera vez?"), new Vector3(0f, 0.27f, 0.12f), new Vector2(0.42f, 0.09f), 1.2f, Color.white);
        encabezado.enableVertexGradient = true;
        encabezado.colorGradient = new VertexGradient(new Color(1f, 0.6f, 0.1f), new Color(1f, 0.3f, 0.6f), new Color(0.55f, 0.35f, 1f), new Color(0.2f, 0.65f, 1f));
        encabezado.outlineWidth = 0.2f;
        encabezado.outlineColor = new Color32(45, 30, 80, 255);

        // Viñeta de cómic (se mueve junto a cada acción).
        vineta = new GameObject("Vineta").transform;
        vineta.SetParent(raiz, false);
        vinetaComic = vineta.gameObject.AddComponent<VinetaComic>();
        vinetaComic.materialTrama = materialTrama;
        vinetaComic.materialNegro = materialNegro;
        vinetaComic.fuente = fuenteComic;
        vinetaComic.Armar(AnchoVineta, AltoVineta);
        vineta.gameObject.SetActive(false);
        vinetaLista = false;

        // Botones "Skip" (abajo a la derecha, bien lejos) y "Next" (para seguir después de jugar con el muñeco).
        btnSaltar = CrearBoton(Tx("Skip", "Saltar"), new Vector3(0.52f, -0.24f, 0.04f), new Vector2(0.07f, 0.026f));
        btnSaltar.alTocar.AddListener(Terminar);
        btnSiguiente = CrearBoton(Tx("Next >", "Siguiente >"), new Vector3(0.42f, 0.13f, 0f), new Vector2(0.09f, 0.03f));
        btnSiguiente.alTocar.AddListener(() => siguientePedido = true);
        MostrarBoton(btnSiguiente, false);

        // Punto A (azul) y bandera en B (aparecen después del paso 1).
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
        metaVisible = false;
        puntoA.gameObject.SetActive(false);
        bandera.gameObject.SetActive(false);

        // La gota de tinta (cae del OK y se vuelve el punto A).
        var g = Pieza(PrimitiveType.Sphere, raiz, materialNegro);
        g.name = "GotaTinta";
        g.transform.localScale = Vector3.one * 0.014f;
        gota = g.transform;
        gota.gameObject.SetActive(false);

        lineaGuia = CrearLinea("LineaGuia", materialGuiaLinea, 0.007f);
        PonerLinea(lineaGuia, curvaDemo, curvaDemo.Count);
        lineaGuia.gameObject.SetActive(false);
        lineaDemo = CrearLinea("LineaDemo", materialNegro, Mathf.Max(0.003f, dibujo.AnchoNuevoMundo));
        lineaDemo.positionCount = 0;

        // Manos guía (sus puntos ya están en el mundo, por eso cuelgan de un objeto sin mover).
        raizManos = new GameObject("ManosGuia");
        matGuia = materialGuia != null ? new Material(materialGuia) : null;
        matResaltado = materialGuia != null ? new Material(materialGuia) : null; // el dedo que importa (amarillo)
        manoIzq = new ManoVideo(raizManos.transform, matGuia, 0, true);
        manoDer = new ManoVideo(raizManos.transform, matGuia, 0, true);
        manoIzq.Poner(null);
        manoDer.Poner(null);
        var bola = Pieza(PrimitiveType.Sphere, raiz, materialRojo);
        bola.name = "PuntoBorrador";
        bola.transform.localScale = Vector3.one * 0.016f;
        puntoBorrador = bola.transform;
        puntoBorrador.gameObject.SetActive(false);

        // El muñeco: pequeño, con 3 hebras finas que tiemblan.
        figura = new MunecoGuia(transform, materialNegro, AnchoMuneco, 3);
        figura.escala = EscalaMuneco;
        figura.temblor = 0.004f;
        figura.alPisar = () => Pasito(0.22f);
        figura.Poner(Vector3.zero, Vector3.right, Vector3.up, 0f, 0f);
        figuraTam = 0f;
        figuraTamObjetivo = 0f;
        figuraEnLinea = false;
        figuraAltura = figuraAgachar = figuraPanico = 0f;
        exito4 = false;
        borrando = false;
        gestoNodos = false;
        movido = 0f;
        teniaDedo = false;
        pinzaGuiaCerrada = false;
        caminoAntes.Clear();
        siguientePedido = false;
        notaArpa = 0;
    }

    void Desarmar()
    {
        QuitarNodosDemo();
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
        if (matResaltado != null)
            Destroy(matResaltado);
        matResaltado = null;
        if (figura != null)
            figura.Destruir();
        figura = null;
        if (lineaGuia != null)
            Destroy(lineaGuia.gameObject);
        if (lineaDemo != null)
            Destroy(lineaDemo.gameObject);
        lineaGuia = lineaDemo = null;
        vineta = null;
        vinetaComic = null;
        vinetaLista = false;
        numeros.Clear();
        encabezado = null;
        btnSaltar = btnSiguiente = null;
    }

    // ==================== Paso 1: el OK ====================

    // La curva de ejemplo (en el marco del tutorial): una ola suave de A (izquierda) a B (derecha).
    static Vector3 Curva(float u)
    {
        float y = 0.032f * (Mathf.Sin(u * Mathf.PI * 2f - 0.4f) + Mathf.Sin(0.4f));
        return new Vector3(Mathf.Lerp(-MitadLinea, MitadLinea, u), y, 0f);
    }

    Vector3 A => raiz.TransformPoint(Curva(0f));
    Vector3 B => raiz.TransformPoint(Curva(1f));

    // OK de la demostración: dos veces (pellizca, sostiene, suelta, vuelve a pellizcar y se queda).
    static float PinzaDosVeces(float t)
    {
        float subir1 = Mathf.InverseLerp(0.7f, 1.0f, t);
        float bajar1 = Mathf.InverseLerp(1.8f, 2.1f, t);
        float subir2 = Mathf.InverseLerp(2.5f, 2.8f, t);
        float bajar2 = Mathf.InverseLerp(3.9f, 4.2f, t);
        float p = t < 2.1f ? subir1 * (1f - bajar1) : subir2 * (1f - bajar2);
        return Mathf.SmoothStep(0f, 1f, p);
    }

    // La mano izquierda guía con el OK (como el dibujo: dorso hacia ti, el anillo a la derecha).
    void PonerOkGuia(float pinza, float alfa)
    {
        ResaltarDedo(1); // el índice (con el pulgar hace el OK)
        PoseMano.Calcular(puntosIzq, true, raiz.TransformPoint(ManoIzqPecho), Dir(-0.3f, 1f, 0.1f), Dir(0.7f, 0f, 0.7f), pinza, 0f, 0f);
        manoIzq.Poner(alfa > 0.01f ? puntosIzq : null);
        // Escarcha y una nota del arpa cada vez que la mano guía cierra el OK.
        bool cerrada = pinza > 0.95f;
        if (cerrada && !pinzaGuiaCerrada)
        {
            Chispas((puntosIzq[4] + puntosIzq[8]) * 0.5f, 8, 0.15f);
            if (estado == Estado.Demo1)
                NotaArpa(0.3f);
        }
        if (pinza < 0.5f || cerrada)
            pinzaGuiaCerrada = cerrada;
    }

    // Solo el paso 1: la mano del OK (dos veces), el número y su viñeta. Nada del paso 2 todavía.
    bool Demo1(float t, bool completa)
    {
        float alfa = Mathf.Clamp01(t / 0.4f) * (1f - Mathf.Clamp01((t - 4.3f) / 0.5f));
        PonerAlfaGuia(completa ? alfa : alfa * 0.75f);
        PonerOkGuia(PinzaDosVeces(t), alfa);
        manoDer.Poner(null);
        if (completa && t >= 0.4f)
        {
            PonerVineta(Texto1, VinetaIzq, 1);
            MostrarNumero(1, JuntoAVineta(VinetaIzq));
        }
        return t >= 4.9f;
    }

    void EntrarPaso1()
    {
        OcultarGuia();
        PonerVineta(Texto1, VinetaIzq, 1);
        Cambiar(Estado.Paso1);
    }

    void Paso1()
    {
        if (control.GestoIzq == ControlManos.Gesto.Dibujar)
        {
            PararPista();
            Exito(1, control.Izq.PuntoPellizco);
            // Del OK cae una gota de tinta que se vuelve el punto A.
            gotaDesde = control.Izq.PuntoPellizco;
            if (gota != null)
            {
                gota.gameObject.SetActive(true);
                gota.position = gotaDesde;
            }
            Cambiar(Estado.Gota);
            return;
        }
        if (control.GestoIzq != ControlManos.Gesto.Ninguno)
            ultimaActividad = Time.time;
        if (TocaPista() && Demo1(Time.time - pistaDesde, false))
            PararPista();
    }

    void Gota()
    {
        float u = Mathf.Clamp01(T / 0.9f);
        if (gota != null && gota.gameObject.activeSelf)
        {
            // Sube un poquito, cae en el punto A y se estira al caer.
            gota.position = Vector3.Lerp(gotaDesde, A, u) + Vector3.up * 0.12f * Mathf.Sin(Mathf.PI * u);
            gota.localScale = new Vector3(0.012f, 0.012f * (1f + 0.6f * u), 0.012f);
            if (u >= 1f)
            {
                gota.gameObject.SetActive(false);
                if (fuentePasos != null)
                    fuentePasos.PlayOneShot(Sonidos.Burbuja, 0.8f);
                Chispas(A, 10, 0.2f);
                metaVisible = true;
                metaDesde = Time.time;
                if (lineaGuia != null)
                    lineaGuia.gameObject.SetActive(true);
            }
        }
        if (T > 1.7f)
            Cambiar(Estado.Demo2);
    }

    // ==================== Paso 2: la línea (desde el punto A hasta la bandera) ====================

    // La mano izquierda guía mantiene el OK y la derecha dibuja del punto A a la bandera.
    bool Demo2(float t, bool completa)
    {
        float alfa = Mathf.Clamp01(t / 0.4f) * (1f - Mathf.Clamp01((t - 4.6f) / 0.5f));
        PonerAlfaGuia(completa ? alfa : alfa * 0.75f);
        PonerOkGuia(1f, alfa);
        Vector3 descanso = raiz.TransformPoint(ManoDerDescanso);
        Vector3 punta;
        float u = 0f;
        if (t < 0.5f)
            punta = descanso;
        else if (t < 1.1f)
            punta = Vector3.Lerp(descanso, A, Suave((t - 0.5f) / 0.6f));
        else if (t < 3.9f)
        {
            u = Suave((t - 1.1f) / 2.8f);
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
            if (t >= 0.3f)
            {
                PonerVineta(Texto2, VinetaLinea, -1);
                MostrarNumero(2, JuntoAVineta(VinetaLinea));
            }
            if (t >= 1.1f)
                PonerLinea(lineaDemo, curvaDemo, Mathf.Clamp(Mathf.RoundToInt(u * (curvaDemo.Count - 1)) + 1, 2, curvaDemo.Count));
            // La línea de la mano guía también suena (sube = agudo, baja = grave) y termina con un acorde.
            if (t >= 1.1f && t < 3.9f)
            {
                float recorrido = u * MitadLinea * 2f;
                if (recorrido - recorridoMelodia >= 0.035f || notaDemo < 0)
                {
                    recorridoMelodia = recorrido;
                    notaDemo = ControlManos.IndicePorAltura(Curva(u).y - Curva(0f).y);
                    control.NotaMusical(notaDemo);
                }
            }
            else if (t >= 3.9f && notaDemo >= 0)
            {
                notaDemo = -1;
                recorridoMelodia = 0f;
                control.AcordeMusical();
            }
            else if (t < 1.1f)
            {
                notaDemo = -1;
                recorridoMelodia = 0f;
            }
        }
        return t >= 5.2f;
    }

    void EntrarPaso2()
    {
        OcultarGuia();
        if (lineaDemo != null)
            lineaDemo.positionCount = 0;
        PonerVineta(Texto2, VinetaLinea, -1);
        Cambiar(Estado.Paso2);
    }

    void Paso2()
    {
        var tr = control.TrazoActual;
        if (tr != null && tr.Dibujando)
        {
            PararPista();
            ultimaActividad = Time.time;
            SeguirLineaNueva(tr);
            // ¡Llegaste a la bandera! (paso 2). Ahora hay que abrir los dedos (paso 3).
            if (Vector3.Distance(control.Der.indice, B) < 0.07f && LargoCamino >= MitadLinea * 2f * 0.6f)
            {
                Exito(2, B);
                OcultarVineta(); // un respiro antes del paso 3
                Cambiar(Estado.Paso3);
            }
            return;
        }
        if (trazoUsuario != null)
        {
            // Soltó antes de llegar: se quita esa línea y se intenta otra vez (desde el punto A).
            var t = trazoUsuario;
            trazoUsuario = null;
            if (t != null && dibujo.trazos.Contains(t))
                dibujo.EliminarDelTodo(t);
            figuraTamObjetivo = 0f;
            figuraEnLinea = false;
            PonerVineta(TextoCasi, VinetaLinea, -1);
            return;
        }
        if (control.GestoIzq != ControlManos.Gesto.Ninguno)
        {
            ultimaActividad = Time.time;
            PararPista();
        }
        if (TocaPista() && Demo2(Time.time - pistaDesde, false))
            PararPista();
        ActualizarFigura();
    }

    // Mientras dibujas: lee tu línea y, a mitad de camino, aparece el muñeco corriendo sobre ella (sorpresa).
    void SeguirLineaNueva(Trazo tr)
    {
        if (trazoUsuario != tr)
        {
            trazoUsuario = tr;
            figuraEnLinea = false;
            figuraTamObjetivo = 0f;
        }
        LeerCamino(tr);
        float largo = LargoCamino;
        if (!figuraEnLinea && largo >= MitadLinea * 2f * 0.45f)
        {
            figuraEnLinea = true;
            figuraS = 0f;
            figuraMov = 1f;
            figuraTamObjetivo = 1f;
            Chispas(PuntoCamino(0f), 6, 0.2f);
        }
        if (figuraEnLinea)
            MoverFigura(largo - 0.04f, 0.4f);
    }

    // ==================== Paso 3: abrir los dedos (dejar de dibujar) ====================

    void Paso3()
    {
        var tr = control.TrazoActual;
        if (tr != null && tr.Dibujando)
        {
            SeguirLineaNueva(tr);
            // Primero un respiro (se celebra la bandera); luego aparece el 3 con su viñeta.
            const float respiro = 1.4f;
            if (T < respiro)
                return;
            PonerVineta(Texto3, VinetaIzq, 1);
            MostrarNumero(3, JuntoAVineta(VinetaIzq));
            // La mano guía (suave) enseña a abrir los dedos, una y otra vez.
            float ciclo = Mathf.Repeat(T - respiro, 2.2f);
            float pinza = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.0f, 1.3f, ciclo)) * (1f - Mathf.InverseLerp(1.9f, 2.2f, ciclo));
            PonerAlfaGuia(0.55f * Mathf.Clamp01((T - respiro) / 0.4f));
            PonerOkGuia(pinza, 1f);
            // Si no suelta en un buen rato, la línea se termina sola.
            if (T > 12f)
                control.TerminarLineaActual();
            return;
        }
        // ¡Abrió los dedos! La línea queda terminada.
        OcultarGuia();
        var t = trazoUsuario;
        trazoUsuario = null;
        lineaUsuario = t != null && dibujo.trazos.Contains(t) ? t : UltimoTrazo();
        Exito(3, control.Izq.valida ? control.Izq.PuntoPellizco : raiz.TransformPoint(ManoIzqPecho));
        if (lineaUsuario != null)
        {
            Aplanar(lineaUsuario);
            LeerCamino(lineaUsuario);
        }
        if (lineaGuia != null)
            lineaGuia.gameObject.SetActive(false);
        Cambiar(Estado.Corre);
    }

    // El muñeco termina de correr hasta cerca del final de tu línea; ahí empieza a seguir tu mano.
    void Corre()
    {
        if (lineaUsuario == null || camino.Count < 2)
        {
            EmpezarDemo56();
            return;
        }
        float meta = Mathf.Max(0f, LargoCamino - 0.04f);
        if (!figuraEnLinea)
        {
            figuraEnLinea = true;
            figuraS = Mathf.Max(0f, meta - 0.25f);
            figuraTamObjetivo = 1f;
        }
        MoverFigura(meta, 0.4f);
        bool llego = Mathf.Abs(figuraS - meta) < 0.005f;
        if ((llego && T > 0.6f) || T > 3.5f)
        {
            metaVisible = false;
            PonerVineta(Texto4, VinetaMuneco, -1);
            MostrarNumero(4, JuntoAVineta(VinetaMuneco));
            movido = 0f;
            exito4 = false;
            calibrado = false;
            saltoDesde = -1f;
            siguientePedido = false;
            Chispas(PuntoCamino(figuraS) + Vector3.up * 0.08f, 10, 0.3f);
            Cambiar(Estado.Paso4);
        }
    }

    // ==================== Paso 4: el muñeco sigue tu mano, sobre TU línea (como un tren en sus rieles) ====================

    void Paso4()
    {
        var der = control.Der;
        float dt = Mathf.Max(1e-4f, Time.deltaTime);
        float largo = LargoCamino;
        if (der.valida)
        {
            float x = Vector3.Dot(der.indice - raiz.position, raiz.right);
            if (!calibrado)
            {
                // Donde está tu mano ahora = donde está el muñeco.
                calibrado = true;
                xCal = x;
                yCal = der.indice.y;
                sCal = figuraS;
                yPrevio = der.indice.y;
                velY = 0f;
            }
            float objetivo = Mathf.Clamp(sCal + (x - xCal) * 1.2f, 0.02f, Mathf.Max(0.02f, largo - 0.02f));
            float antes = figuraS;
            MoverFigura(objetivo, 1.5f);
            movido += Mathf.Abs(figuraS - antes);
            // Golpe rápido hacia arriba = saltar.
            velY = Mathf.Lerp(velY, (der.indice.y - yPrevio) / dt, 1f - Mathf.Exp(-25f * dt));
            yPrevio = der.indice.y;
            if (saltoDesde < 0f && velY > 0.8f)
                saltoDesde = Time.time;
            // Mano más de 4 cm abajo = agacharse.
            float agacharMano = Mathf.Clamp01((yCal - der.indice.y - 0.04f) / 0.06f);
            ActualizarSalto(agacharMano);
        }
        else
        {
            ActualizarSalto(0f);
            ActualizarFigura();
        }
        if (T > 2.5f)
            MostrarBoton(btnSiguiente, true);
        if (!exito4 && T > 2.5f && movido > 0.3f)
        {
            exito4 = true;
            Numero n;
            Vector3 donde = numeros.TryGetValue(4, out n) && n.raiz != null ? n.raiz.position : B + Vector3.up * 0.2f;
            Exito(4, donde);
        }
        // Sigue cuando tocas "Next" (o solo, después de un rato).
        if (siguientePedido || T > 22f)
        {
            MostrarBoton(btnSiguiente, false);
            if (!exito4)
                MarcarNumero(4);
            figuraAltura = 0f;
            figuraAgachar = 0f;
            Pausa(EmpezarDemo56);
        }
    }

    // Salto: se agacha (anticipación), sube, cae y se aplasta un poquito al caer.
    void ActualizarSalto(float agacharMano)
    {
        figuraAltura = 0f;
        figuraAgachar = agacharMano;
        if (saltoDesde < 0f)
            return;
        float t = Time.time - saltoDesde;
        const float anticipa = 0.1f, aire = 0.45f, cae = 0.15f;
        if (t < anticipa)
            figuraAgachar = Mathf.Max(figuraAgachar, 0.6f * (t / anticipa));
        else if (t < anticipa + aire)
        {
            float u = (t - anticipa) / aire;
            figuraAltura = 4f * 0.07f * u * (1f - u);
            figuraAgachar = 0f;
        }
        else if (t < anticipa + aire + cae)
        {
            float u = (t - anticipa - aire) / cae;
            figuraAgachar = Mathf.Max(figuraAgachar, 0.5f * Mathf.Sin(Mathf.PI * u));
            if (u < 0.2f && t - Time.deltaTime < anticipa + aire)
                Pasito(0.3f);
        }
        else
        {
            saltoDesde = -1f;
        }
    }

    // ==================== Pasos 5 y 6: borrar ====================

    void EmpezarDemo56()
    {
        QuitarNumeros();
        MostrarBoton(btnSiguiente, false);
        AsegurarLinea();
        // La línea como está ahora (después vuelve sola, para los pasos 7 y 8).
        lineaGuardada = lineaUsuario != null ? lineaUsuario.CrearDatos() : null;
        // El muñeco está parado cerca del final de la línea (donde va a empezar a borrar el dedo guía).
        figuraS = Mathf.Max(0f, LargoCamino - 0.04f);
        figuraMov = 0f;
        dirFigura = -1f;
        figuraPanico = 0f;
        figuraEnLinea = true;
        figuraTam = 1f;
        figuraTamObjetivo = 1f;
        // Durante la demostración tu línea se esconde y se ve una copia que se va borrando (con sus nodos).
        OcultarTrazosTutorial(true);
        MostrarCopia();
        CrearNodosDemo();
        Cambiar(Estado.Demo56);
    }

    // Puño de la demostración: cierra, gira la muñeca de lado y vuelve; abre; otra vez igual y se queda.
    static void PunoDemo(float t, float fin, out float puno, out float giro)
    {
        float c1 = Mathf.InverseLerp(0.6f, 0.9f, t), a1 = Mathf.InverseLerp(2.3f, 2.6f, t);
        float c2 = Mathf.InverseLerp(2.9f, 3.2f, t), a2 = Mathf.InverseLerp(fin, fin + 0.3f, t);
        float p = t < 2.6f ? c1 * (1f - a1) : c2 * (1f - a2);
        puno = Mathf.SmoothStep(0f, 1f, p);
        float g1 = Suave((t - 1.1f) / 0.5f) * (1f - Suave((t - 1.6f) / 0.5f));
        float g2 = Suave((t - 3.3f) / 0.5f) * (1f - Suave((t - 3.8f) / 0.5f));
        giro = Mathf.Max(g1, g2);
    }

    // Las manos guía enseñan el puño (5) y frotar la línea (6). "completa" = con la línea que se borra y el muñeco que huye.
    bool Demo56(float t, bool completa)
    {
        float alfa = Mathf.Clamp01(t / 0.4f) * (1f - Mathf.Clamp01((t - 8.4f) / 0.5f));
        PonerAlfaGuia(completa ? alfa : alfa * 0.75f);
        ResaltarDedo(-1);
        // Puño izquierdo con el DORSO hacia ti; la muñeca gira de lado (palma hacia la derecha) y vuelve.
        float puno, giro;
        PunoDemo(t, 8.0f, out puno, out giro);
        Vector3 palma = Vector3.Slerp(Dir(0.45f, 0.1f, 1f), Dir(1f, 0.1f, -0.1f), giro);
        PoseMano.Calcular(puntosIzq, true, raiz.TransformPoint(ManoIzqPecho), Dir(0.05f, 1f, 0.2f), palma, 0f, puno, 0f);
        manoIzq.Poner(alfa > 0.01f ? puntosIzq : null);

        float largo = LargoCamino;
        Vector3 descanso = raiz.TransformPoint(ManoDerDescanso);
        Vector3 fin = camino.Count > 0 ? PuntoCamino(largo) : B;
        Vector3 inicio = camino.Count > 0 ? PuntoCamino(0f) : A;
        Vector3 punta;
        float sDedo = largo + 1f;
        bool frotando = false;
        if (t < 4.6f)
            punta = descanso;
        else if (t < 5.2f)
            punta = Vector3.Lerp(descanso, fin, Suave((t - 4.6f) / 0.6f));
        else if (t < 7.8f)
        {
            sDedo = largo * (1f - Suave((t - 5.2f) / 2.6f));
            punta = camino.Count > 0 ? PuntoCamino(sDedo) : Vector3.Lerp(fin, inicio, (t - 5.2f) / 2.6f);
            punta += raiz.right * Mathf.Sin(t * 22f) * 0.006f; // un poquito de "frote" (ida y vuelta)
            frotando = true;
        }
        else
        {
            sDedo = -1f;
            punta = inicio;
        }
        PoseMano.ConIndiceEn(puntosDer, false, punta, Dir(-0.1f, 0.2f, 1f), Dir(0f, -1f, 0.2f), 0f, 0f, 1f);
        manoDer.Poner(alfa > 0.01f && t > 4.3f ? puntosDer : null);
        bool punto = alfa > 0.01f && t >= 3.2f && t < 8.0f;
        if (puntoBorrador != null)
        {
            if (puntoBorrador.gameObject.activeSelf != punto)
                puntoBorrador.gameObject.SetActive(punto);
            if (punto)
                puntoBorrador.position = puntosDer[8];
        }

        if (completa)
        {
            if (t >= 0.4f && t < 4.0f)
            {
                PonerVineta(Texto5, VinetaIzq, 1);
                MostrarNumero(5, JuntoAVineta(VinetaIzq));
            }
            else if (t >= 4.0f && t < 4.6f)
            {
                OcultarVineta(); // un respiro entre el 5 y el 6
            }
            if (t >= 4.6f)
            {
                PonerVineta(Texto6, VinetaLinea, -1);
                MostrarNumero(6, JuntoAVineta(VinetaLinea));
            }
            if (frotando || t >= 7.8f)
            {
                // La copia de la línea se va borrando detrás del dedo y sus nodos estallan.
                int n = 0;
                while (n < caminoLargo.Count && caminoLargo[n] <= sDedo)
                    n++;
                PonerLinea(lineaDemo, camino, n);
                foreach (var nd in nodosDemo)
                    if (!nd.estallo && nd.s >= sDedo - 0.004f)
                        Estallar(nd);
                if (frotando && Time.time - ultimaChispa > 0.12f)
                {
                    ultimaChispa = Time.time;
                    Chispas(punta, 2, 0.12f);
                }
            }
            // El muñeco huye en pánico, siempre 2 cm delante del dedo (y se va al final).
            if (frotando)
                Huir(sDedo, -1f, true);
            else if (t >= 7.8f)
                Escapar();
            else
                ActualizarFigura();
        }
        return t >= 9.0f;
    }

    // El muñeco corre en pánico (brazos arriba) justo delante del dedo que borra, sin salirse de la línea.
    void Huir(float sDedo, float direccion, bool rapido)
    {
        float largo = LargoCamino;
        float objetivo = Mathf.Clamp(sDedo + direccion * 0.02f, 0.005f, Mathf.Max(0.005f, largo - 0.005f));
        float paso = Mathf.Clamp(objetivo - figuraS, -1.5f * Time.deltaTime, 1.5f * Time.deltaTime);
        figuraS += paso;
        dirFigura = direccion;
        figuraMov = 2f; // siempre corriendo (aunque borres despacio)
        figuraPanico = Mathf.MoveTowards(figuraPanico, 1f, Time.deltaTime * 6f);
        if (figura != null)
            figura.Avanzar(Time.deltaTime * (rapido ? 0.45f : 0.35f) * EscalaMuneco, 2f);
        ActualizarFigura();
    }

    // Al terminar de borrar: salta fuera de la línea y desaparece.
    void Escapar()
    {
        figuraTamObjetivo = 0f;
        figuraAltura = 0.03f;
        ActualizarFigura();
    }

    void EntrarPaso5()
    {
        OcultarGuia();
        QuitarNodosDemo();
        OcultarTrazosTutorial(false);
        if (lineaDemo != null)
            lineaDemo.positionCount = 0;
        // El muñeco vuelve a aparecer parado cerca del final.
        figuraS = Mathf.Max(0f, LargoCamino - 0.04f);
        figuraMov = 0f;
        dirFigura = -1f;
        figuraPanico = 0f;
        figuraAltura = 0f;
        figuraTam = 0f;
        figuraTamObjetivo = 1f;
        PonerVineta(Texto5, VinetaIzq, 1);
        Cambiar(Estado.Paso5);
    }

    void Paso5()
    {
        ActualizarFigura();
        if (control.GestoIzq == ControlManos.Gesto.Borrar)
        {
            PararPista();
            Exito(5, control.Izq.medio);
            PonerVineta(Texto6, VinetaLinea, -1);
            nodosInicio = NodosTutorial();
            borrando = false;
            huidaFija = false;
            control.soloNodosAlBorrar = true; // aquí se borra nodo por nodo
            teniaDedo = false;
            velDedo = 0f;
            Cambiar(Estado.Paso6);
            return;
        }
        if (control.GestoIzq != ControlManos.Gesto.Ninguno)
            ultimaActividad = Time.time;
        if (TocaPista() && Demo56(Time.time - pistaDesde, false))
            PararPista();
    }

    void Paso6()
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
        if (!borrando && nodos < nodosInicio)
        {
            borrando = true;
            borrandoDesde = Time.time;
        }
        // Hacia dónde huye: hacia el lado más lejano del dedo (se decide al empezar a borrar y ya no cambia).
        if (!huidaFija)
            dirHuida = sDedo > LargoCamino * 0.5f ? -1f : 1f;
        if (borrando)
            huidaFija = true;
        bool cerca = der.valida && distancia < 0.12f && (goma || borrando);
        if (cerca || borrando)
            Huir(sDedo, dirHuida, velDedo > 0.2f);
        else
            ActualizarFigura();
        // Termina cuando el muñeco llegó hasta el final de la línea (o ya no queda línea).
        float extremo = dirHuida < 0f ? 0.005f : Mathf.Max(0.005f, LargoCamino - 0.005f);
        bool alFinal = Mathf.Abs(figuraS - extremo) < 0.015f;
        bool listo = nodos == 0 || (alFinal && nodosInicio > 0 && nodos <= nodosInicio / 2) || (alFinal && borrando && Time.time - borrandoDesde > 4f);
        if (listo)
        {
            control.soloNodosAlBorrar = false;
            Exito(6, der.valida ? der.indice : PuntoCamino(figuraS));
            Escapar();
            Pausa(() => Cambiar(Estado.Regresa));
            return;
        }
        if (TocaPista() && Demo56(Time.time - pistaDesde, false))
            PararPista();
    }

    // La línea vuelve sola (tinta mágica que se redibuja de A a B), para los pasos 7 y 8.
    void Regresa()
    {
        ActualizarFigura();
        float u = Mathf.Clamp01((T - 0.6f) / 0.9f);
        if (lineaDemo != null && caminoAntes.Count == 0 && camino.Count > 0)
            caminoAntes.AddRange(camino);
        if (T >= 0.6f && caminoAntes.Count > 1)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(u * caminoAntes.Count), 2, caminoAntes.Count);
            PonerLinea(lineaDemo, caminoAntes, n);
            if (Time.time - ultimaChispa > 0.08f)
            {
                ultimaChispa = Time.time;
                Chispas(caminoAntes[n - 1], 2, 0.1f);
            }
        }
        if (T < 1.6f)
            return;
        // Quita lo que quedó y vuelve a poner la línea tal como era.
        var viejas = new List<Trazo>(dibujo.trazos);
        foreach (var t in viejas)
            if (t != null)
                dibujo.EliminarDelTodo(t);
        if (lineaGuardada != null)
        {
            dibujo.GuardarParaDeshacer();
            lineaUsuario = dibujo.AgregarTrazo(JsonUtility.FromJson<DatosTrazo>(JsonUtility.ToJson(lineaGuardada)));
        }
        else
        {
            lineaUsuario = null;
        }
        if (lineaDemo != null)
            lineaDemo.positionCount = 0;
        caminoAntes.Clear();
        Pausa(EmpezarDemo7, 1f);
    }

    // ==================== Paso 7: mover nodos ====================

    void EmpezarDemo7()
    {
        OcultarGuia();
        QuitarNumeros();
        AsegurarLinea();
        // El muñeco vuelve, tranquilo, al final de la línea.
        figuraS = Mathf.Max(0f, LargoCamino - 0.04f);
        figuraMov = 0f;
        dirFigura = -1f;
        figuraPanico = 0f;
        figuraAltura = 0f;
        figuraTam = 0f;
        figuraTamObjetivo = 1f;
        OcultarTrazosTutorial(true);
        MostrarCopia();
        CrearNodosDemo();
        foreach (var nd in nodosDemo)
            nd.t.localScale = Vector3.zero;
        Cambiar(Estado.Demo7);
    }

    // Pulgar + medio izquierdos (aparecen los nodos) y la derecha pellizca un nodo y lo arrastra.
    bool Demo7(float t, bool completa)
    {
        float alfa = Mathf.Clamp01(t / 0.4f) * (1f - Mathf.Clamp01((t - 7.4f) / 0.5f));
        PonerAlfaGuia(completa ? alfa : alfa * 0.75f);
        ResaltarDedo(2); // el dedo MEDIO (el que sigue al índice)
        float medio = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.9f, t)) * (1f - Mathf.InverseLerp(6.9f, 7.2f, t));
        PoseMano.Calcular(puntosIzq, true, raiz.TransformPoint(ManoIzqPecho), Dir(-0.3f, 1f, 0.1f), Dir(0.7f, 0f, 0.7f), 0f, 0f, 0f, medio, 0f);
        manoIzq.Poner(alfa > 0.01f ? puntosIzq : null);

        // El nodo del medio de la línea.
        int k = nodosDemo.Count / 2;
        Vector3 nodo = k < nodosDemo.Count ? nodosDemo[k].pos : (camino.Count > 0 ? PuntoCamino(LargoCamino * 0.5f) : raiz.position);
        float sNodo = k < nodosDemo.Count ? nodosDemo[k].s : LargoCamino * 0.5f;
        Vector3 arrastre = Vector3.up * 0.07f + (control.Cabeza != null ? (control.Cabeza.position - nodo).normalized * 0.02f : Vector3.zero);
        float tirar = Suave((t - 2.8f) / 1.6f);
        float pinza = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.5f, 2.8f, t)) * (1f - Mathf.InverseLerp(4.4f, 4.7f, t));
        Vector3 descanso = raiz.TransformPoint(ManoDerDescanso);
        Vector3 punta;
        if (t < 1.8f)
            punta = descanso;
        else if (t < 2.5f)
            punta = Vector3.Lerp(descanso, nodo, Suave((t - 1.8f) / 0.7f));
        else if (t < 4.7f)
            punta = nodo + arrastre * tirar;
        else
            punta = Vector3.Lerp(nodo + arrastre, descanso, Suave((t - 4.7f) / 0.6f));
        PoseMano.ConIndiceEn(puntosDer, false, punta, Dir(-0.2f, 0.35f, 1f), Dir(0.2f, -1f, 0.1f), pinza, 0f, 0f);
        manoDer.Poner(alfa > 0.01f && t > 0.2f ? puntosDer : null);

        if (completa)
        {
            if (t >= 0.4f && t < 1.5f)
            {
                PonerVineta(Texto7, VinetaIzq, 1);
                MostrarNumero(7, JuntoAVineta(VinetaIzq));
            }
            else if (t >= 1.5f && t < 1.9f)
            {
                OcultarVineta();
            }
            if (t >= 1.9f)
                PonerVineta(Texto7b, VinetaLinea, -1);
            // Los nodos aparecen con el gesto y el del medio se arrastra (la línea lo sigue, suave).
            float ver = Rebote(Mathf.Clamp01((t - 0.9f) / 0.35f)) * (1f - Mathf.Clamp01((t - 7.0f) / 0.3f));
            Vector3 mover = arrastre * tirar;
            for (int i = 0; i < nodosDemo.Count; i++)
            {
                var nd = nodosDemo[i];
                nd.t.position = nd.pos + mover * Peso(nd.s, sNodo);
                nd.t.localScale = Vector3.one * 0.016f * ver * (i == k && pinza > 0.5f ? 1.3f : 1f);
                if (control.Cabeza != null)
                    nd.t.rotation = Quaternion.LookRotation(nd.t.position - control.Cabeza.position, Vector3.up);
            }
            if (lineaDemo != null && camino.Count >= 2)
            {
                lineaDemo.positionCount = camino.Count;
                for (int i = 0; i < camino.Count; i++)
                    lineaDemo.SetPosition(i, camino[i] + mover * Peso(caminoLargo[i], sNodo));
            }
            if (t >= 2.8f && t - Time.deltaTime < 2.8f)
                Pasito(0.2f);
            ActualizarFigura();
        }
        return t >= 8.0f;
    }

    static float Peso(float s, float centro)
    {
        float d = (s - centro) / 0.09f;
        return Mathf.Exp(-d * d);
    }

    void EntrarPaso7()
    {
        OcultarGuia();
        QuitarNodosDemo();
        OcultarTrazosTutorial(false);
        if (lineaDemo != null)
            lineaDemo.positionCount = 0;
        // Se recuerda dónde está cada nodo (para saber cuándo moviste uno) y cómo es la línea (para deshacer).
        nodosPaso7.Clear();
        foreach (var t in dibujo.trazos)
        {
            if (t == null || !Dibujo.Editable(t))
                continue;
            var lista = new List<Vector3>();
            foreach (var n in t.nodos)
                lista.Add(dibujo.transform.TransformPoint(n));
            nodosPaso7[t.id] = lista;
        }
        caminoAntes.Clear();
        caminoAntes.AddRange(camino);
        gestoNodos = false;
        PonerVineta(Texto7, VinetaIzq, 1);
        Cambiar(Estado.Paso7);
    }

    void Paso7()
    {
        ActualizarFigura();
        // Primera celebración: juntaste pulgar + medio (aparecen los nodos).
        if (control.GestoIzq == ControlManos.Gesto.Nodos)
        {
            ultimaActividad = Time.time;
            PararPista();
            if (!gestoNodos)
            {
                gestoNodos = true;
                Exito(7, control.Izq.valida ? control.Izq.medio : raiz.TransformPoint(ManoIzqPecho));
                PonerVineta(Texto7b, VinetaLinea, -1);
            }
        }
        else if (control.GestoIzq != ControlManos.Gesto.Ninguno)
        {
            ultimaActividad = Time.time;
        }
        // Segunda celebración: moviste un nodo más de 2.5 cm.
        foreach (var t in dibujo.trazos)
        {
            List<Vector3> antes;
            if (t == null || !nodosPaso7.TryGetValue(t.id, out antes))
                continue;
            int n = Mathf.Min(antes.Count, t.nodos.Count);
            for (int i = 0; i < n; i++)
            {
                Vector3 ahora = dibujo.transform.TransformPoint(t.nodos[i]);
                if (Vector3.Distance(ahora, antes[i]) > 0.025f)
                {
                    Exito(7, ahora);
                    Pausa(EmpezarDemo8);
                    return;
                }
            }
        }
        if (TocaPista() && Demo7(Time.time - pistaDesde, false))
            PararPista();
    }

    // ==================== Paso 8: deshacer (el último) ====================

    void EmpezarDemo8()
    {
        OcultarGuia();
        QuitarNumeros();
        demoAcerto = false;
        AsegurarLinea(); // la línea con el nodo movido
        OcultarTrazosTutorial(true);
        MostrarCopia();
        Cambiar(Estado.Demo8);
    }

    // Puño izquierdo con el PULGAR hacia la izquierda, centrado en el pecho (la palma mira hacia ti):
    // aparece la flecha, va hacia la diana roja y la toca = deshacer (el nodo vuelve a su lugar).
    bool Demo8(float t, bool completa)
    {
        float alfa = Mathf.Clamp01(t / 0.4f) * (1f - Mathf.Clamp01((t - 5.2f) / 0.5f));
        PonerAlfaGuia(completa ? alfa : alfa * 0.75f);
        ResaltarDedo(0); // el pulgar (la punta de la flecha)
        float puno = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1.0f, t)) * (1f - Mathf.InverseLerp(4.8f, 5.1f, t));
        float mover = Suave((t - 1.8f) / 0.8f) * (1f - Suave((t - 3.6f) / 0.8f));
        Vector3 muneca = raiz.TransformPoint(ManoCentro + new Vector3(-0.045f * mover, 0f, 0f));
        PoseMano.Calcular(puntosIzq, true, muneca, Dir(0f, 1f, 0.15f), Dir(0f, 0f, -1f), 0f, puno, 0f, 0f, puno);
        manoIzq.Poner(alfa > 0.01f ? puntosIzq : null);
        manoDer.Poner(null);

        if (completa)
        {
            if (t >= 0.4f)
            {
                PonerVineta(Texto8, VinetaCentro, 1);
                MostrarNumero(8, JuntoAVineta(VinetaCentro));
            }
            // La flecha (la punta en el pulgar) y la diana delante de ella.
            bool flecha = t >= 1.2f && t < 4.6f && control.simbolos != null;
            Vector3 dir = -raiz.right;
            Vector3 puntaFlecha = puntosIzq[4] - Vector3.up * 0.008f;
            if (t < 1.2f)
                demoDiana = puntaFlecha + dir * 0.045f;
            if (flecha)
            {
                control.simbolos.Flecha(true, puntaFlecha - dir * 0.12f, dir, 0.12f);
                control.simbolos.Diana(false, true, demoDiana, demoDiana - dir, 0.06f);
            }
            else
            {
                OcultarFlecha();
            }
            if (!demoAcerto && t >= 2.6f)
            {
                // ¡Tocó la diana! El nodo vuelve a su lugar.
                demoAcerto = true;
                if (control.simbolos != null)
                    control.simbolos.Acertar(false);
                if (fuente != null)
                    fuente.PlayOneShot(Sonidos.Ding, 0.4f);
                PonerLinea(lineaDemo, caminoAntes, caminoAntes.Count);
                Chispas(demoDiana, 10, 0.2f);
            }
            ActualizarFigura();
        }
        return t >= 5.8f;
    }

    void EntrarPaso8()
    {
        OcultarGuia();
        OcultarFlecha();
        OcultarTrazosTutorial(false);
        if (lineaDemo != null)
            lineaDemo.positionCount = 0;
        deshechoInicio = dibujo.VecesDeshecho;
        PonerVineta(Texto8, VinetaCentro, 1);
        Cambiar(Estado.Paso8);
    }

    void Paso8()
    {
        ActualizarFigura();
        if (dibujo.VecesDeshecho > deshechoInicio)
        {
            PararPista();
            Exito(8, control.Izq.valida ? control.Izq.pulgar : raiz.TransformPoint(ManoCentro));
            Pausa(EmpezarDemo9);
            return;
        }
        if (control.GestoIzq != ControlManos.Gesto.Ninguno)
            ultimaActividad = Time.time;
        if (TocaPista() && Demo8(Time.time - pistaDesde, false))
            PararPista();
    }

    // ==================== Paso 9: tamaño y giro con las dos manos ====================

    int idPaso9 = -1;
    float largoPaso9;
    Vector3 dirPaso9;

    void EmpezarDemo9()
    {
        OcultarGuia();
        QuitarNumeros();
        AsegurarLinea();
        figuraTamObjetivo = 0f; // el muñeco descansa
        dibujo.Seleccionar(null);
        OcultarTrazosTutorial(true);
        MostrarCopia();
        Cambiar(Estado.Demo9);
    }

    // Las dos manos guía hacen el OK, se separan (más grande) y giran como un volante (rotar).
    bool Demo9(float t, bool completa)
    {
        float alfa = Mathf.Clamp01(t / 0.4f) * (1f - Mathf.Clamp01((t - 4.8f) / 0.5f));
        PonerAlfaGuia(completa ? alfa : alfa * 0.75f);
        ResaltarDedo(1);
        if (manoDer != null)
            manoDer.Resaltar(1, matResaltado);
        float pinza = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.9f, t)) * (1f - Mathf.InverseLerp(4.2f, 4.5f, t));
        float separar = Suave((t - 1.2f) / 1.2f);
        float giro = Suave((t - 2.6f) / 1.2f) * 35f;
        float d = Mathf.Lerp(0.22f, 0.34f, separar);
        Vector3 centro = raiz.TransformPoint(new Vector3(0f, -0.1f, -0.14f));
        Quaternion q = Quaternion.AngleAxis(giro, raiz.forward);
        Vector3 pIzq = centro + q * (-raiz.right * d * 0.5f);
        Vector3 pDer = centro + q * (raiz.right * d * 0.5f);
        Vector3 abajo = q * (-raiz.up * 0.07f);
        PoseMano.Calcular(puntosIzq, true, pIzq + abajo, q * Dir(-0.3f, 1f, 0.1f), q * Dir(0.7f, 0f, 0.7f), pinza, 0f, 0f);
        PoseMano.Calcular(puntosDer, false, pDer + abajo, q * Dir(0.3f, 1f, 0.1f), q * Dir(-0.7f, 0f, 0.7f), pinza, 0f, 0f);
        manoIzq.Poner(alfa > 0.01f ? puntosIzq : null);
        manoDer.Poner(alfa > 0.01f ? puntosDer : null);
        if (completa)
        {
            if (t >= 0.4f)
            {
                PonerVineta(Texto9, VinetaCentro, 1);
                MostrarNumero(9, JuntoAVineta(VinetaCentro));
            }
            // La copia de la línea crece y gira con las manos.
            if (lineaDemo != null && camino.Count >= 2)
            {
                Vector3 c = (camino[0] + camino[camino.Count - 1]) * 0.5f;
                float escala = d / 0.22f;
                lineaDemo.positionCount = camino.Count;
                for (int i = 0; i < camino.Count; i++)
                    lineaDemo.SetPosition(i, c + q * ((camino[i] - c) * escala));
            }
        }
        return t >= 5.4f;
    }

    void EntrarPaso9()
    {
        OcultarGuia();
        if (manoDer != null)
            manoDer.Resaltar(-1, null);
        OcultarTrazosTutorial(false);
        if (lineaDemo != null)
            lineaDemo.positionCount = 0;
        // Se recuerda el tamaño y la dirección de tu línea, para saber cuándo la agrandaste o giraste.
        idPaso9 = lineaUsuario != null ? lineaUsuario.id : -1;
        largoPaso9 = 0f;
        var t = LineaPaso9();
        if (t != null)
        {
            Vector3 a = dibujo.transform.TransformPoint(t.nodos[0]);
            Vector3 b = dibujo.transform.TransformPoint(t.nodos[t.nodos.Count - 1]);
            largoPaso9 = (b - a).magnitude;
            dirPaso9 = b - a;
        }
        PonerVineta(Texto9, VinetaCentro, 1);
        Cambiar(Estado.Paso9);
    }

    Trazo LineaPaso9()
    {
        var t = dibujo.BuscarPorId(idPaso9);
        if (t == null || t.nodos.Count < 2)
            t = UltimoTrazo();
        return t != null && t.nodos.Count >= 2 ? t : null;
    }

    void Paso9()
    {
        if (control.GestoIzq == ControlManos.Gesto.Transformar)
        {
            ultimaActividad = Time.time;
            PararPista();
        }
        else if (control.GestoIzq != ControlManos.Gesto.Ninguno)
        {
            ultimaActividad = Time.time;
        }
        var t = LineaPaso9();
        if (t != null && largoPaso9 > 1e-4f)
        {
            Vector3 a = dibujo.transform.TransformPoint(t.nodos[0]);
            Vector3 b = dibujo.transform.TransformPoint(t.nodos[t.nodos.Count - 1]);
            float escala = (b - a).magnitude / largoPaso9;
            float angulo = Vector3.Angle(b - a, dirPaso9);
            if (Mathf.Abs(escala - 1f) > 0.15f || angulo > 15f)
            {
                Exito(9, (a + b) * 0.5f);
                Pausa(EntrarFinal);
                return;
            }
        }
        if (TocaPista() && Demo9(Time.time - pistaDesde, false))
            PararPista();
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
        PonerVineta(Tx("Now draw whatever you want! See it again: My files > Tutorial", "¡Ahora dibuja lo que quieras! Para verlo otra vez: Mis archivos > Tutorial"), VinetaLinea, -1);
        figuraTamObjetivo = 0f;
        PlayerPrefs.SetInt(ClaveVisto, 1);
        PlayerPrefs.Save();
        Cambiar(Estado.Final);
    }

    // ==================== Pistas (repetir la demostración si no haces nada) ====================

    bool TocaPista()
    {
        if (pistaDesde < 0f && Time.time - ultimaActividad > 8f)
            pistaDesde = Time.time;
        return pistaDesde >= 0f;
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

    void OcultarFlecha()
    {
        if (control == null || control.simbolos == null)
            return;
        control.simbolos.Flecha(false, Vector3.zero, Vector3.forward, 0f);
        control.simbolos.Diana(false, false, Vector3.zero, Vector3.zero, 0f);
    }

    void PonerAlfaGuia(float a)
    {
        if (matGuia != null)
            matGuia.SetColor("_BaseColor", new Color(0.45f, 0.75f, 1f, 0.6f * Mathf.Clamp01(a)));
        if (matResaltado != null)
            matResaltado.SetColor("_BaseColor", new Color(1f, 0.85f, 0.15f, 0.85f * Mathf.Clamp01(a)));
    }

    // Pinta de amarillo un dedo de la mano guía izquierda (-1 = ninguno).
    void ResaltarDedo(int dedo)
    {
        if (manoIzq != null)
            manoIzq.Resaltar(dedo, dedo >= 0 ? matResaltado : null);
    }

    // ==================== La línea del tutorial ====================

    // Que haya una línea del tutorial (la tuya; si no quedó ninguna, una de ejemplo) y su camino.
    void AsegurarLinea()
    {
        if (lineaUsuario == null || !dibujo.trazos.Contains(lineaUsuario) || !Dibujo.Editable(lineaUsuario) || !lineaUsuario.visibleAnim)
            lineaUsuario = UltimoTrazo();
        if (lineaUsuario == null)
            lineaUsuario = LineaDeEjemplo();
        if (lineaUsuario != null)
            LeerCamino(lineaUsuario);
    }

    // Una copia de tu línea (para las demostraciones).
    void MostrarCopia()
    {
        if (lineaDemo == null)
            return;
        lineaDemo.widthMultiplier = lineaUsuario != null ? Mathf.Max(0.003f, lineaUsuario.ancho * dibujo.EscalaMundo) : 0.005f;
        PonerLinea(lineaDemo, camino, camino.Count);
    }

    // Circulitos azules en los nodos de tu línea (para las demostraciones).
    void CrearNodosDemo()
    {
        QuitarNodosDemo();
        if (lineaUsuario == null || raiz == null)
            return;
        foreach (var n in lineaUsuario.nodos)
        {
            Vector3 w = dibujo.transform.TransformPoint(n);
            float d;
            var nd = new NodoDemo { pos = w, s = ProyectarEnCamino(w, out d) };
            var go = new GameObject("NodoDemo");
            go.transform.SetParent(raiz, false);
            go.transform.position = w;
            if (control.Cabeza != null)
                go.transform.rotation = Quaternion.LookRotation(w - control.Cabeza.position, Vector3.up);
            go.transform.localScale = Vector3.one * 0.016f;
            Disco(go.transform, materialNegro, 1.25f, new Vector3(0f, 0f, 0.05f));
            Disco(go.transform, materialAzul, 1f, Vector3.zero);
            nd.t = go.transform;
            nodosDemo.Add(nd);
        }
    }

    void QuitarNodosDemo()
    {
        foreach (var nd in nodosDemo)
            if (nd.t != null)
                Destroy(nd.t.gameObject);
        nodosDemo.Clear();
    }

    // Un nodo de la demostración estalla (destello, escarcha y el "plop" de burbuja).
    void Estallar(NodoDemo nd)
    {
        nd.estallo = true;
        if (nd.t != null)
            nd.t.gameObject.SetActive(false);
        dibujo.Destello(nd.pos, 0.02f);
        Chispas(nd.pos, 5, 0.18f);
        if (fuentePasos != null)
        {
            fuentePasos.pitch = Random.Range(0.9f, 1.15f);
            fuentePasos.PlayOneShot(Sonidos.Burbuja, 0.6f);
        }
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
        // (el muñeco es pequeño: corre desde menos velocidad)
        float objetivo = vel < 0.02f ? 0f : vel < 0.16f ? 1f : 2f;
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
        // Fuera de la línea se va achicando hasta desaparecer.
        float afuera = Mathf.Max(-figuraS, figuraS - LargoCamino, 0f);
        float tam = figuraTam * Mathf.Clamp01(1f - afuera / 0.12f);
        if (tam <= 0.01f || camino.Count < 2)
        {
            figura.Poner(Vector3.zero, Vector3.right, Vector3.up, 0f, 0f);
            return;
        }
        if (figuraTamObjetivo > 0.5f && figuraPanico > 0f && estado != Estado.Paso6 && estado != Estado.Demo56)
            figuraPanico = Mathf.MoveTowards(figuraPanico, 0f, Time.deltaTime * 3f);
        figura.panico = figuraPanico;
        figura.agachar = figuraAgachar;
        Vector3 suelo = PuntoCamino(figuraS) + Vector3.up * (0.003f + figuraAltura);
        figura.Poner(suelo, TangenteCamino(figuraS) * dirFigura, Vector3.up, figuraMov, tam);
    }

    // Tu línea queda en un plano frente a ti (así el muñeco la sigue bien).
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
        if (raiz == null)
            return null;
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

    // ==================== Viñeta de cómic ====================

    // cola: -1 hacia abajo, 1 hacia arriba (hacia donde está la acción).
    void PonerVineta(string texto, Vector3 lugar, int cola)
    {
        if (vineta == null || vinetaComic == null)
            return;
        if (!vineta.gameObject.activeSelf)
            vineta.gameObject.SetActive(true);
        var t = vinetaComic.Texto;
        if (t != null && t.text != texto)
            t.text = texto;
        vinetaComic.cola = cola;
        vinetaObjetivo = lugar;
        if (!vinetaLista)
        {
            vineta.localPosition = lugar;
            vinetaLista = true;
        }
    }

    // Se mueve suave a su lugar y mira hacia ti (el temblor del borde lo hace la viñeta).
    void ActualizarVineta()
    {
        if (vineta == null || !vinetaLista)
            return;
        float a = 1f - Mathf.Exp(-8f * Time.deltaTime);
        vineta.localPosition = Vector3.Lerp(vineta.localPosition, vinetaObjetivo, a);
        if (control.Cabeza != null)
        {
            Vector3 mirar = vineta.position - control.Cabeza.position;
            if (mirar.sqrMagnitude > 1e-6f)
                vineta.rotation = Quaternion.Slerp(vineta.rotation, Quaternion.LookRotation(mirar, Vector3.up), a);
        }
    }

    // El número va a la izquierda de su viñeta.
    static Vector3 JuntoAVineta(Vector3 lugar)
    {
        return lugar + new Vector3(-AnchoVineta * 0.5f - 0.034f, 0f, -0.005f);
    }

    // ==================== Números y decorado ====================

    static int GrupoNumero(int n)
    {
        return n == 1 || n == 2 ? 1 : n == 5 || n == 6 ? 5 : n;
    }

    void MostrarNumero(int n, Vector3 local)
    {
        if (numeros.ContainsKey(n) || raiz == null)
            return;
        // Los números de pasos anteriores se van (menos los que se usan juntos: 1 y 2, 5 y 6).
        var quitar = new List<int>();
        foreach (var par in numeros)
            if (GrupoNumero(par.Key) != GrupoNumero(n))
                quitar.Add(par.Key);
        foreach (int k in quitar)
        {
            if (numeros[k].raiz != null)
                Destroy(numeros[k].raiz.gameObject);
            numeros.Remove(k);
        }
        var num = new Numero { desde = Time.time };
        num.raiz = new GameObject("Numero" + n).transform;
        num.raiz.SetParent(raiz, false);
        num.raiz.localPosition = local;
        Disco(num.raiz, materialNegro, 0.052f, new Vector3(0f, 0f, 0.001f));
        num.relleno = Disco(num.raiz, materialAzul, 0.046f, Vector3.zero).GetComponent<Renderer>();
        var texto = CrearTexto(num.raiz, n.ToString(), new Vector3(0f, 0.001f, -0.002f), new Vector2(0.04f, 0.04f), 0.5f, Color.white);
        if (fuenteComic != null)
            texto.font = fuenteComic;
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
            num.raiz.localScale = Vector3.one * Rebote(Mathf.Clamp01((Time.time - num.desde) / 0.35f));
            if (control.Cabeza != null)
            {
                Vector3 mirar = num.raiz.position - control.Cabeza.position;
                if (mirar.sqrMagnitude > 1e-6f)
                    num.raiz.rotation = Quaternion.LookRotation(mirar, Vector3.up);
            }
        }
    }

    void ActualizarDecorado()
    {
        float t = Time.time;
        // El punto A y la bandera aparecen (con rebote) cuando cae la gota de tinta, y se van al terminar la línea.
        float aparecer = metaVisible ? Rebote(Mathf.Clamp01((t - metaDesde) / 0.35f)) : 0f;
        float aparecerBandera = metaVisible ? Rebote(Mathf.Clamp01((t - metaDesde - 0.3f) / 0.35f)) : 0f;
        if (puntoA != null)
        {
            bool ver = aparecer > 0.01f;
            if (puntoA.gameObject.activeSelf != ver)
                puntoA.gameObject.SetActive(ver);
            puntoA.localScale = Vector3.one * aparecer * (1f + 0.15f * Mathf.Sin(t * 5f));
        }
        if (bandera != null)
        {
            bool ver = aparecerBandera > 0.01f;
            if (bandera.gameObject.activeSelf != ver)
                bandera.gameObject.SetActive(ver);
            bandera.localScale = Vector3.one * aparecerBandera;
            if (tela != null)
                tela.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 3.2f) * 22f, Mathf.Sin(t * 2.1f) * 4f);
        }
        if (encabezado != null)
            encabezado.transform.localPosition = new Vector3(0f, 0.27f + Mathf.Sin(t * 1.6f) * 0.005f, 0.12f);
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
            float esc = Rebote(Mathf.Clamp01(t / 0.35f)) * (1f - Mathf.Clamp01((t - 1.35f) / 0.35f));
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

    Vector3 Dir(float x, float y, float z)
    {
        return raiz.TransformDirection(new Vector3(x, y, z)).normalized;
    }

    static float Suave(float u)
    {
        u = Mathf.Clamp01(u);
        return u * u * (3f - 2f * u);
    }

    // Aparece con rebote (se pasa un poquito y vuelve): 0 → 1.
    static float Rebote(float k)
    {
        return k >= 1f ? 1f : 1f - Mathf.Exp(-5f * k) * Mathf.Cos(9f * k);
    }

    static Vector3 Horizontal(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-8f ? v.normalized : Vector3.forward;
    }

    BotonTocable CrearBoton(string texto, Vector3 local, Vector2 tam)
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Boton_" + texto;
        cubo.transform.SetParent(raiz, false);
        cubo.transform.localPosition = local;
        cubo.transform.localScale = new Vector3(tam.x, tam.y, 0.008f);
        Pintar(cubo.GetComponent<Renderer>(), materialBoton);
        var b = cubo.AddComponent<BotonTocable>();
        b.materialNormal = materialBoton;
        b.materialMarcado = materialBotonMarcado;
        b.etiqueta = CrearTexto(raiz, texto, local + new Vector3(0f, 0f, -0.0046f), new Vector2(tam.x * 0.9f, tam.y * 0.8f), 0.22f, Color.black);
        return b;
    }

    static void MostrarBoton(BotonTocable b, bool ver)
    {
        if (b == null || b.gameObject.activeSelf == ver)
            return;
        b.gameObject.SetActive(ver);
        if (b.etiqueta != null)
            b.etiqueta.gameObject.SetActive(ver);
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
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Los gestos de TrazoVR (todo con las manos):
//  Izquierda pulgar + ÍNDICE (sostener)  -> dibujar con la punta del índice derecho.
//  Izquierda pulgar + ÍNDICE + MEDIO juntos (o pulgar + meñique) -> línea recta (del punto donde empiezas hasta tu dedo).
//  En modo Plano (2D), si alejas el dedo del plano más de ~2.5 cm la línea se corta (como levantar el lápiz).
//  Izquierda pulgar + MEDIO  (sostener)  -> modo nodos: pellizca con la derecha para mover nodos y asas.
//       Arrastra la punta de una línea sobre otra punta: se unen como imán (o se cierra la figura).
//  Izquierda pulgar + ANULAR (sostener)  -> grosor: sube/baja la mano izquierda = todo más grueso/delgado;
//       pellizca un nodo con la derecha y súbela/bájala = grosor solo de ese nodo.
//  Izquierda PUÑO (pulgar sobre los dedos o al lado) -> borrador: tocar un nodo lo borra;
//       la línea entera solo se borra si la FROTAS (ida y vuelta) lejos de sus nodos.
//  Izquierda puño con el PULGAR hacia tu izquierda -> la mano se vuelve una FLECHA: toca la diana roja = deshacer.
//  DERECHA puño con el PULGAR hacia tu derecha     -> flecha hacia la derecha: toca la diana verde = rehacer.
//  Izquierda: DOBLE TOQUE rápido de pulgar + índice -> bloquear / desbloquear el dibujo (candado arriba a la derecha).
//  Izquierda pulgar + ANULAR y el índice derecho girando en círculos pequeños -> grosor de las líneas nuevas
//       (a la derecha = más grueso, a la izquierda = más delgado).
//  Izquierda abierta con el pulgar tocando la base de los dedos -> menú.
//  LAS DOS manos pellizcando (índice + pulgar) -> escalar, girar (como volante) y mover todo.
//  Tocar un relleno con el índice derecho -> cambiar su color.
//  Pellizcar una línea con la derecha (sin gesto izquierdo) -> seleccionarla y moverla.
//  Pellizcar en el aire -> quitar la selección (los cambios vuelven a afectar a todo el dibujo).
[DefaultExecutionOrder(-50)]
public class ControlManos : MonoBehaviour
{
    public static ControlManos Instancia { get; private set; }

    public Dibujo dibujo;
    public CajaTransformar caja;
    public Referencias referencias;
    public PanelArriba panelArriba;
    public SimbolosMano simbolos;
    [Tooltip("Material que no dibuja nada (para esconder la mano)")]
    public Material materialInvisible;
    public Material materialNodo;
    public Material materialNodoActivo;
    public Material materialAsa;
    public Material materialIman;
    public Material materialCursor;
    public Material materialCursorBorrar;
    [Tooltip("Color con el que destella la mano al deshacer")]
    public Material materialDestelloMano;
    [Tooltip("Color de la mano izquierda mientras está en modo borrador")]
    public Material materialBorrarMano;

    [Header("Gestos (en metros)")]
    public float pellizcoEntra = 0.02f;
    public float pellizcoSale = 0.035f;
    [Tooltip("Segundos que hay que sostener un gesto antes de que cuente")]
    public float confirmarGesto = 0.08f;
    [Tooltip("Más alto = sigue más rápido al dedo; más bajo = más suave")]
    public float suavizado = 16f;
    public float radioAgarreNodo = 0.025f;
    [Tooltip("Cerca de un nodo (esta distancia), el borrador borra el nodo y nunca la línea")]
    public float radioBorrarNodo = 0.03f;
    [Tooltip("Cuánto hay que frotar una línea (metros de ida y vuelta) para borrarla entera")]
    public float distanciaFrote = 0.06f;
    public float tamanoNodo = 0.008f;
    [Tooltip("Cuánto cambia el grosor al subir/bajar la mano")]
    public float sensibilidadGrosor = 4f;

    public enum Gesto { Ninguno, Dibujar, Nodos, Grosor, Transformar, Borrar, Recta }
    public Gesto GestoIzq { get; private set; }

    [Header("Ayudas")]
    [Tooltip("Texto que flota sobre la mano con el nombre del gesto")]
    public TMP_Text textoGesto;
    public bool mostrarAyudas = true;

    // true mientras otro objeto usa las manos (por ejemplo, al mover el panel de arriba).
    public bool Ocupado { get; set; }

    // Dibujo bloqueado (doble toque izquierdo): no se dibujan líneas por accidente.
    public bool DibujoBloqueado { get; private set; }

    public ManoSeguida Izq { get; } = new ManoSeguida(true);
    public ManoSeguida Der { get; } = new ManoSeguida(false);
    public Transform Cabeza { get; private set; }

    enum Objetivo { Nada, Nodo, Asa, Linea, Relleno }

    OVRCameraRig rig;
    Gesto candidato;
    float candidatoDesde;
    bool esperarSoltarIzq;
    Trazo trazoActual;
    float inicioTrazo;

    // Pose de la mano izquierda (se calcula una vez por cuadro)
    ManosUtil.Palma palmaIzq;
    bool poseValida;
    float curvaIndice, curvaMedio, curvaAnular;  // ~0.5-1 doblado, ~1.5+ estirado
    float pulgarANudillo;                         // punta del pulgar al nudillo del índice
    Vector3 basePulgar;
    float distanciaMenu;                          // del pulgar a la base de los dedos (pose del menú)

    // Nodos y asas visibles
    readonly List<Transform> nodosVisibles = new List<Transform>();
    readonly List<Renderer> nodosRender = new List<Renderer>();
    readonly List<MeshFilter> nodosFiltro = new List<MeshFilter>();
    readonly Transform[] asasVisibles = new Transform[2];
    GameObject lineasAsas;
    Mesh mallaLineasAsas;
    readonly List<Vector3> puntosAsas = new List<Vector3>();
    readonly List<int> indicesAsas = new List<int>();
    static Mesh mallaDisco, mallaAnillo;

    // Arrastre en modo nodos
    Objetivo arrastre = Objetivo.Nada;
    Trazo arrTrazo;
    int arrIndice = -1;
    bool arrSalida;
    Vector3 desfase;
    bool deshacerPendiente;
    Trazo selTrazo;
    int selIndice = -1;
    Objetivo hoverTipo;
    Trazo hoverTrazo;
    int hoverIndice = -1;
    bool hoverSalida;
    Trazo imanTrazo;
    int imanExtremo;

    // Grosor
    float alturaInicialGrosor;
    float factorGlobal = 1f;
    Trazo grosorTrazo;
    int grosorIndice = -1;
    float grosorNodoInicio = 1f;
    float alturaDerGrosor;

    // Borrador, deshacer, rellenos
    bool tocandoRelleno;
    float proximoToque;

    // Menú (se queda abierto aunque la mano derecha tape un momento a la izquierda)
    bool menuAbierto;
    float menuFueraDesde = -1f;
    Vector3 posMenu;
    Quaternion rotMenu = Quaternion.identity;

    // Borrador
    bool armadoBorrar;
    Vector3 posUltimoBorrado;
    Trazo froteTrazo;
    float froteRecorrido;
    Vector3 froteAncla;

    // Línea recta
    Vector3 inicioRecta;

    // Plano 2D: el dedo está lejos del plano (lápiz levantado)
    bool lejosDelPlano;
    float gestoDesde;

    // Mano izquierda: dirección de los dedos (para orientar el borrador)
    Vector3 dirDedosIzq = Vector3.forward;

    // Flecha y diana: mano izquierda = deshacer, mano derecha = rehacer
    class EstadoFlecha
    {
        public bool activa;
        public float candidatoDesde = -1f;
        public float fueraDesde = -1f;
        public Vector3 ultimaPos;
        public Vector3 punta;
        public Vector3 dir;
        public Vector3 posDiana;
        public Vector3 dirDiana;
        public bool armada;
    }
    readonly EstadoFlecha flechaIzq = new EstadoFlecha();
    readonly EstadoFlecha flechaDer = new EstadoFlecha();
    float finGestoIzq = -10f;
    float finGestoDer = -10f;
    const float largoFlecha = 0.12f;
    const float distanciaDiana = 0.09f;
    const float diametroDiana = 0.06f;

    // Manos escondidas (cuando se vuelven borrador o flecha)
    bool ocultarIzq, ocultarDer;
    readonly List<Renderer> rendsIzq = new List<Renderer>();
    readonly List<Renderer> rendsDer = new List<Renderer>();
    float proximaBusquedaManos;

    // Doble toque para el candado
    float toqueInicio = -1f;
    float toqueUltimoFin = -10f;
    int toques;

    // Dial de grosor (índice derecho girando)
    bool dialUsado;
    bool dialPrevioValido;
    Vector3 dialCentro;
    Vector3 dialPrevio;
    float dialAngulo;
    float anchoDialInicio;
    float dialRadio;

    // Mover una imagen de referencia con el pellizco derecho
    Transform imagenMovida;
    Vector3 desfaseImagen;

    // Etiqueta sobre la mano
    string etiquetaTemporal;
    float etiquetaHasta;

    // Mover una línea con el pellizco derecho
    Trazo lineaMovida;
    readonly List<Vector3> baseLinea = new List<Vector3>();
    Vector3 inicioLinea;
    bool deshacerLineaPendiente;


    Transform cursor;
    Renderer cursorRender;

    void Awake()
    {
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }

    void Start()
    {
        rig = FindFirstObjectByType<OVRCameraRig>();
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
        if (caja == null)
            caja = GetComponent<CajaTransformar>();
        if (referencias == null)
            referencias = FindFirstObjectByType<Referencias>();
        if (panelArriba == null)
            panelArriba = FindFirstObjectByType<PanelArriba>();

        var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "Cursor";
        Destroy(esfera.GetComponent<Collider>());
        esfera.transform.SetParent(transform, false);
        cursorRender = esfera.GetComponent<Renderer>();
        if (materialCursor != null)
            cursorRender.sharedMaterial = materialCursor;
        cursorRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        cursorRender.receiveShadows = false;
        cursor = esfera.transform;
        cursor.gameObject.SetActive(false);

        for (int i = 0; i < 2; i++)
        {
            asasVisibles[i] = CrearCirculo("Asa", materialAsa).transform;
            asasVisibles[i].gameObject.SetActive(false);
        }
        lineasAsas = new GameObject("LineasAsas");
        lineasAsas.transform.SetParent(transform, false);
        mallaLineasAsas = new Mesh { name = "LineasAsas" };
        mallaLineasAsas.MarkDynamic();
        lineasAsas.AddComponent<MeshFilter>().sharedMesh = mallaLineasAsas;
        var mr = lineasAsas.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materialAsa;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        lineasAsas.SetActive(false);
    }

    void Update()
    {
        if (rig == null)
        {
            rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig == null)
                return;
        }
        if (dibujo == null)
            return;

        Cabeza = rig.centerEyeAnchor;
        Izq.Actualizar(rig.leftHandAnchor, suavizado, pellizcoEntra, pellizcoSale);
        Der.Actualizar(rig.rightHandAnchor, suavizado, pellizcoEntra, pellizcoSale);
        LeerPoseIzquierda();

        if (Ocupado || ExportadorVideo.Exportando || Titere.Activo)
        {
            SalirFlecha(flechaIzq, true);
            SalirFlecha(flechaDer, false);
            // Otro objeto está usando las manos: no se dibuja ni se edita nada.
            if (GestoIzq != Gesto.Ninguno)
                SalirDeGesto();
            SoltarImagen();
            lineaMovida = null;
            menuAbierto = false;
            esperarSoltarIzq = true;
            OcultarModoNodos();
            ActualizarCursor();
            ActualizarEtiqueta();
            ActualizarSimbolos();
            return;
        }

        RevisarCandado();
        ActualizarGestoIzquierdo();
        ActualizarMenu();

        switch (GestoIzq)
        {
            case Gesto.Dibujar: Dibujar(); break;
            case Gesto.Recta: Recta(); break;
            case Gesto.Nodos: EditarNodos(); break;
            case Gesto.Grosor: CambiarGrosor(); break;
            case Gesto.Borrar: Borrar(); break;
            case Gesto.Transformar: if (caja != null) caja.Actualizar(Izq, Der); break;
            default:
                if (!menuAbierto)
                {
                    RevisarAgarreLinea();
                    RevisarToqueRelleno();
                }
                break;
        }

        if (GestoIzq != Gesto.Nodos && GestoIzq != Gesto.Grosor && GestoIzq != Gesto.Borrar)
            OcultarModoNodos();
        ActualizarFlechas();
        if (Der.soltoPellizco)
            finGestoDer = Time.time;
        ActualizarCursor();
        ActualizarEtiqueta();
        ActualizarSimbolos();
    }

    // Borrador, candado y dial (la flecha y la diana las maneja ActualizarFlecha).
    void ActualizarSimbolos()
    {
        if (simbolos == null)
            return;
        bool borrador = GestoIzq == Gesto.Borrar && poseValida && !Ocupado;
        Quaternion giroBorrador = Quaternion.identity;
        if (borrador && dirDedosIzq.sqrMagnitude > 1e-6f && palmaIzq.normal.sqrMagnitude > 1e-6f)
            giroBorrador = Quaternion.LookRotation(dirDedosIzq, -palmaIzq.normal);
        simbolos.Borrador(borrador, borrador ? palmaIzq.centro + dirDedosIzq * 0.02f : Vector3.zero, giroBorrador);
        simbolos.Candado(DibujoBloqueado, Cabeza);
        bool dial = GestoIzq == Gesto.Grosor && dialUsado && Cabeza != null;
        simbolos.Dial(dial, dialCentro, Cabeza != null ? Cabeza.position : Vector3.zero, Mathf.Clamp(dialRadio * 2f, 0.02f, 0.08f));
    }

    // ---------- Pose de la mano izquierda ----------

    void LeerPoseIzquierda()
    {
        poseValida = false;
        if (!Izq.valida)
            return;
        var esq = Izq.esqueleto;
        palmaIzq = ManosUtil.LeerPalma(esq, true);
        Transform nudilloIndice = ManosUtil.Hueso(esq, "Index1", "IndexProximal");
        Transform nudilloMedio = ManosUtil.Hueso(esq, "Middle1", "MiddleProximal");
        Transform muneca = ManosUtil.Hueso(esq, "WristRoot", "Wrist");
        Transform pulgar = ManosUtil.Hueso(esq, "Thumb1", "ThumbMetacarpal");
        Transform nudilloMenique = ManosUtil.Hueso(esq, "Pinky1", "LittleProximal");
        if (!palmaIzq.valida || nudilloIndice == null || nudilloMedio == null || muneca == null || pulgar == null || nudilloMenique == null)
            return;
        float tam = palmaIzq.tamano;
        curvaIndice = Vector3.Distance(Izq.indice, palmaIzq.centro) / tam;
        curvaMedio = Vector3.Distance(Izq.medio, palmaIzq.centro) / tam;
        curvaAnular = Vector3.Distance(Izq.anular, palmaIzq.centro) / tam;
        pulgarANudillo = Vector3.Distance(Izq.pulgar, nudilloIndice.position);
        basePulgar = pulgar.position;
        Vector3 dedos = nudilloMedio.position - muneca.position;
        if (dedos.sqrMagnitude > 1e-6f)
            dirDedosIzq = dedos.normalized;
        // La "base de los dedos": una franja desde el nudillo del índice hasta el del meñique,
        // un poco hacia la muñeca. Sirve aunque el pulgar toque un poco más abajo.
        Vector3 a = Vector3.Lerp(nudilloIndice.position, muneca.position, 0.2f);
        Vector3 b = Vector3.Lerp(nudilloMenique.position, muneca.position, 0.2f);
        distanciaMenu = DistanciaASegmento(Izq.pulgar, a, b);
        poseValida = true;
    }

    static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float l2 = ab.sqrMagnitude;
        if (l2 < 1e-10f)
            return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
        return Vector3.Distance(p, a + ab * t);
    }

    // Puño (de frente o de lado, sin importar la muñeca): dedos doblados y el pulgar
    // sobre los dedos o pegado al índice. Si el pulgar se estira lejos, ya es "deshacer".
    bool EsPuno(bool yaEnPuno)
    {
        if (!poseValida)
            return false;
        float promedio = (curvaIndice + curvaMedio + curvaAnular) / 3f;
        float maximo = Mathf.Max(curvaIndice, Mathf.Max(curvaMedio, curvaAnular));
        if (yaEnPuno)
            return promedio < 1.3f && maximo < 1.45f && pulgarANudillo < 0.075f;
        return promedio < 1.15f && maximo < 1.3f && pulgarANudillo < 0.065f;
    }

    bool DedosAbiertos()
    {
        return poseValida && curvaIndice > 1.1f && curvaMedio > 1.1f;
    }

    // ---------- Mano izquierda: qué gesto hace ----------

    void ActualizarGestoIzquierdo()
    {
        // Las dos manos pellizcando = girar/escalar. Tiene prioridad sobre dibujar.
        if (GestoIzq == Gesto.Transformar)
        {
            if (!Izq.pellizco || !Der.pellizco)
            {
                SalirDeGesto();
                esperarSoltarIzq = true;
            }
            return;
        }
        if (GestoIzq == Gesto.Dibujar && Der.pellizco && Der.TiempoPellizco > 0.1f && Izq.pellizco)
        {
            PasarATransformar();
            return;
        }

        if (!Izq.valida)
        {
            // Si la mano se pierde un instante, seguimos; si se pierde más, soltamos.
            if (GestoIzq != Gesto.Ninguno && Izq.sinSenal > 0.25f)
                SalirDeGesto();
            candidato = Gesto.Ninguno;
            return;
        }

        float dIndice = Vector3.Distance(Izq.pulgar, Izq.indice);
        float dMedio = Vector3.Distance(Izq.pulgar, Izq.medio);
        float dAnular = Vector3.Distance(Izq.pulgar, Izq.anular);
        float dMenique = Vector3.Distance(Izq.pulgar, Izq.menique);

        if (GestoIzq == Gesto.Borrar)
        {
            // Se sale del borrador al abrir la mano o sacar el pulgar.
            if (poseValida && !EsPuno(true))
                SalirDeGesto();
            return;
        }
        // Recta con tres dedos: pulgar + índice + medio juntos (más fácil de ver que el meñique).
        bool tresDedos = dIndice < pellizcoEntra * 1.3f && dMedio < pellizcoEntra * 1.25f;
        if (GestoIzq == Gesto.Dibujar && tresDedos && Time.time - gestoDesde < 0.4f)
        {
            // Apenas empezaba a dibujar y juntó también el medio: era una recta.
            if (trazoActual != null)
                dibujo.CancelarTrazo(trazoActual);
            trazoActual = null;
            EntrarEnGesto(Gesto.Recta);
            return;
        }
        if (GestoIzq != Gesto.Ninguno)
        {
            float d = GestoIzq == Gesto.Dibujar ? dIndice
                    : GestoIzq == Gesto.Nodos ? dMedio
                    : GestoIzq == Gesto.Recta ? Mathf.Min(dIndice, dMenique)
                    : dAnular;
            if (d > pellizcoSale)
                SalirDeGesto();
            return;
        }

        if (esperarSoltarIzq)
        {
            if (dIndice > pellizcoSale && dMedio > pellizcoSale && dAnular > pellizcoSale && dMenique > pellizcoSale)
                esperarSoltarIzq = false;
            return;
        }

        // Prioridad: pellizco de índice (dibujar) > puño (borrar) > pellizco de medio/anular.
        Gesto nuevo = Gesto.Ninguno;
        if (dIndice < pellizcoEntra * 1.3f && dMedio < pellizcoEntra * 1.25f)
        {
            nuevo = Gesto.Recta;
        }
        else if (dIndice < pellizcoEntra && dIndice <= dMedio && dIndice <= dAnular)
        {
            nuevo = Gesto.Dibujar;
        }
        else if (EsPuno(false))
        {
            nuevo = Gesto.Borrar;
        }
        else if (!poseValida || curvaIndice > 1.15f)
        {
            // Medio, anular o meñique con el índice estirado (así no se confunde con el puño).
            if (dMedio < pellizcoEntra && dMedio <= dAnular && dMedio <= dMenique)
                nuevo = Gesto.Nodos;
            else if (dAnular < pellizcoEntra && dAnular <= dMenique)
                nuevo = Gesto.Grosor;
            else if (dMenique < pellizcoEntra)
                nuevo = Gesto.Recta;
        }

        if (nuevo != candidato)
        {
            candidato = nuevo;
            candidatoDesde = Time.time;
        }
        // Con el menú abierto se pide sostener más, para no cerrarlo por un salto del seguimiento.
        float confirmar = menuAbierto ? 0.25f : confirmarGesto;
        if (nuevo == Gesto.Ninguno || Time.time - candidatoDesde < confirmar)
            return;

        if ((nuevo == Gesto.Dibujar || nuevo == Gesto.Recta) && Der.pellizco && Izq.pellizco)
            EntrarEnGesto(Gesto.Transformar);
        else
            EntrarEnGesto(nuevo);
    }

    void PasarATransformar()
    {
        if (trazoActual != null)
        {
            // Si apenas empezaba la línea, se descarta; si ya era larga, se conserva.
            if (trazoActual.LargoCrudo * dibujo.EscalaMundo < 0.05f || Time.time - inicioTrazo < 0.4f)
                dibujo.CancelarTrazo(trazoActual);
            else
                dibujo.TerminarTrazo(trazoActual);
            trazoActual = null;
        }
        GestoIzq = Gesto.Ninguno;
        EntrarEnGesto(Gesto.Transformar);
    }

    void EntrarEnGesto(Gesto g)
    {
        GestoIzq = g;
        gestoDesde = Time.time;
        lineaMovida = null;
        lejosDelPlano = false;
        if (g != Gesto.Transformar)
            SoltarImagen();
        if (dibujo.animacion != null)
            dibujo.animacion.Pausar();
        if (g == Gesto.Grosor)
        {
            alturaInicialGrosor = Izq.pulgar.y;
            factorGlobal = 1f;
            grosorTrazo = null;
            dibujo.EmpezarGrosor();
            dialUsado = false;
            dialPrevioValido = false;
            dialAngulo = 0f;
            dialCentro = Der.indice;
            anchoDialInicio = dibujo.AnchoNuevoMundo;
        }
        else if (g == Gesto.Transformar)
        {
            Transform imagen = referencias != null ? referencias.Seleccionada : null;
            SoltarImagen();
            if (caja != null)
                caja.Empezar(Izq, Der, dibujo.Seleccion, imagen);
        }
        else if (g == Gesto.Borrar)
        {
            armadoBorrar = false; // no borra lo que ya estaba tocando al cerrar el puño
            posUltimoBorrado = Der.indice;
            froteTrazo = null;
            // La mano se esconde y en su lugar aparece un borrador (solo es un símbolo).
            OcultarMano(true, true);
        }
    }

    void SalirDeGesto()
    {
        if (trazoActual != null)
        {
            dibujo.TerminarTrazo(trazoActual);
            trazoActual = null;
        }
        if (GestoIzq == Gesto.Grosor)
        {
            dibujo.TerminarGrosor();
            if (dialUsado)
                dibujo.Mensaje("Líneas nuevas: " + Mathf.RoundToInt(dibujo.AnchoNuevoMundo * 1000f) + " mm");
            dialUsado = false;
        }
        if (GestoIzq == Gesto.Transformar && caja != null)
        {
            caja.Terminar();
            if (referencias != null && referencias.Seleccionada != null)
                referencias.AlSoltar(referencias.Seleccionada);
        }
        if (GestoIzq == Gesto.Borrar)
        {
            OcultarMano(true, false);
            CancelarFrote();
        }
        if (GestoIzq != Gesto.Ninguno)
            finGestoIzq = Time.time;
        if (arrastre != Objetivo.Nada)
            TerminarArrastre();
        selTrazo = null;
        selIndice = -1;
        grosorTrazo = null;
        hoverTipo = Objetivo.Nada;
        GestoIzq = Gesto.Ninguno;
        candidato = Gesto.Ninguno;
    }

    // ---------- Dibujar ----------

    void Dibujar()
    {
        if (!Der.valida)
            return;
        if (DibujoBloqueado)
            return;
        Vector3 local = dibujo.transform.InverseTransformPoint(Der.indice);
        if (LapizLevantado(local))
            return;
        if (trazoActual == null)
        {
            if (dibujo.plano && !dibujo.HayPlano && Cabeza != null)
                dibujo.DefinirPlano(local, Cabeza.forward);
            dibujo.Seleccionar(null);
            trazoActual = dibujo.NuevoTrazo();
            inicioTrazo = Time.time;
        }
        trazoActual.AgregarPuntoCrudo(dibujo.ProyectarEnPlano(local));
    }

    // En Plano (2D): el dedo tiene que estar sobre el plano para dibujar, como un lápiz en el papel.
    // Empieza a menos de 1.5 cm; si se aleja más de 2.5 cm, la línea termina ahí.
    // El gesto de la mano izquierda sigue activo: al volver al plano empieza otra línea.
    bool LapizLevantado(Vector3 local)
    {
        if (!dibujo.PlanoActivo)
        {
            lejosDelPlano = false;
            return false;
        }
        float d = dibujo.DistanciaAlPlanoMundo(local);
        if (trazoActual == null)
        {
            lejosDelPlano = d > 0.015f;
            return lejosDelPlano;
        }
        if (d <= 0.025f)
        {
            lejosDelPlano = false;
            return false;
        }
        dibujo.TerminarTrazo(trazoActual);
        trazoActual = null;
        lejosDelPlano = true;
        return true;
    }

    // ---------- Línea recta (pulgar + índice + medio, o pulgar + meñique) ----------

    void Recta()
    {
        if (!Der.valida)
            return;
        if (DibujoBloqueado)
            return;
        Vector3 local = dibujo.transform.InverseTransformPoint(Der.indice);
        if (LapizLevantado(local))
            return;
        if (trazoActual == null)
        {
            if (dibujo.plano && !dibujo.HayPlano && Cabeza != null)
                dibujo.DefinirPlano(local, Cabeza.forward);
            dibujo.Seleccionar(null);
            inicioRecta = Imantar(dibujo.ProyectarEnPlano(local), null);
            trazoActual = dibujo.NuevoTrazo();
            inicioTrazo = Time.time;
        }
        trazoActual.PonerRecta(inicioRecta, Imantar(dibujo.ProyectarEnPlano(local), trazoActual));
    }

    // Si el punto está cerca de la punta de otra línea, se pega a ella (para hacer polígonos).
    Vector3 Imantar(Vector3 local, Trazo excluir)
    {
        Trazo o;
        int e;
        if (dibujo.BuscarExtremo(local, excluir, out o, out e))
            return e == 0 ? o.nodos[0] : o.nodos[o.nodos.Count - 1];
        return local;
    }

    // ---------- Modo nodos (mover nodos y asas, imán) ----------

    void EditarNodos()
    {
        if (!Der.valida)
        {
            if (arrastre != Objetivo.Nada)
                TerminarArrastre();
            MostrarModoNodos(true, dibujo.Seleccion);
            return;
        }
        Vector3 pinza = Der.PuntoPellizco;

        if (arrastre != Objetivo.Nada && !Der.pellizco)
            TerminarArrastre();

        Trazo solo = dibujo.Seleccion;
        if (arrastre != Objetivo.Nada)
        {
            ContinuarArrastre(pinza);
            MostrarModoNodos(true, solo);
            return;
        }

        Objetivo tipo = Objetivo.Nada;
        Trazo t;
        int i;
        bool salida = false;
        if (BuscarNodoCercano(Der.indice, pinza, radioAgarreNodo, solo, out t, out i))
            tipo = Objetivo.Nodo;
        else if (BuscarAsaCercana(Der.indice, pinza, out salida))
        {
            tipo = Objetivo.Asa;
            t = selTrazo;
            i = selIndice;
        }
        hoverTipo = tipo;
        hoverTrazo = t;
        hoverIndice = i;
        hoverSalida = salida;

        if (Der.empezoPellizco)
        {
            if (tipo == Objetivo.Nodo || tipo == Objetivo.Asa)
            {
                dibujo.Seleccionar(t);
                EmpezarArrastre(tipo, t, i, salida, pinza);
            }
            else
            {
                // Pellizcar la línea seleccionada (lejos de sus nodos) = agregar un nodo ahí.
                // Pellizcar otra línea = seleccionarla (y ver solo sus nodos).
                var otra = LineaBajo(pinza, Der.indice);
                if (otra != null && otra == dibujo.Seleccion)
                    AgregarNodoEn(otra, pinza);
                else if (otra != null)
                    dibujo.Seleccionar(otra);
            }
        }
        MostrarModoNodos(true, dibujo.Seleccion);
    }

    // Agrega un nodo en el punto de la línea más cercano a la pinza y empieza a arrastrarlo.
    bool AgregarNodoEn(Trazo linea, Vector3 pinza)
    {
        Transform raiz = dibujo.transform;
        int seg1, seg2;
        float t1, t2, d1, d2;
        bool ok1 = linea.PuntoEnCurva(raiz.InverseTransformPoint(pinza), out seg1, out t1, out d1);
        bool ok2 = linea.PuntoEnCurva(raiz.InverseTransformPoint(Der.indice), out seg2, out t2, out d2);
        if (!ok1 && !ok2)
            return false;
        bool usarPinza = ok1 && (!ok2 || d1 <= d2);
        int segmento = usarPinza ? seg1 : seg2;
        float posicion = usarPinza ? t1 : t2;
        if (segmento < 0 || posicion < 0.03f || posicion > 0.97f)
            return false;
        dibujo.GuardarParaDeshacer();
        dibujo.InsertarNodo(linea, segmento, posicion);
        EmpezarArrastre(Objetivo.Nodo, linea, segmento + 1, false, pinza);
        deshacerPendiente = false;
        dibujo.Mensaje("Nodo agregado");
        return true;
    }

    bool BuscarNodoCercano(Vector3 a, Vector3 b, float radio, Trazo solo, out Trazo trazo, out int indice)
    {
        trazo = null;
        indice = -1;
        float mejor = radio;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || (solo != null && t != solo))
                continue;
            for (int i = 0; i < t.nodos.Count; i++)
            {
                Vector3 p = dibujo.transform.TransformPoint(t.nodos[i]);
                float d = Mathf.Min(Vector3.Distance(p, a), Vector3.Distance(p, b));
                if (d < mejor)
                {
                    mejor = d;
                    trazo = t;
                    indice = i;
                }
            }
        }
        return trazo != null;
    }

    bool SeleccionValida => Dibujo.Editable(selTrazo) && selIndice >= 0 && selIndice < selTrazo.nodos.Count;

    Vector3 PuntaAsaMundo(Trazo t, int i, bool salida)
    {
        t.AsegurarAsas();
        Vector3 local = t.nodos[i] + (salida ? t.asaSalida[i] : t.asaEntrada[i]);
        return dibujo.transform.TransformPoint(local);
    }

    bool BuscarAsaCercana(Vector3 a, Vector3 b, out bool salida)
    {
        salida = false;
        if (!SeleccionValida)
            return false;
        float mejor = radioAgarreNodo * 0.8f;
        bool hay = false;
        for (int k = 0; k < 2; k++)
        {
            bool esSalida = k == 1;
            if (!selTrazo.AsaUsada(selIndice, esSalida))
                continue;
            Vector3 p = PuntaAsaMundo(selTrazo, selIndice, esSalida);
            float d = Mathf.Min(Vector3.Distance(p, a), Vector3.Distance(p, b));
            if (d < mejor)
            {
                mejor = d;
                salida = esSalida;
                hay = true;
            }
        }
        return hay;
    }

    void EmpezarArrastre(Objetivo tipo, Trazo t, int i, bool salida, Vector3 pinza)
    {
        arrastre = tipo;
        arrTrazo = t;
        arrIndice = i;
        arrSalida = salida;
        deshacerPendiente = true;
        imanTrazo = null;
        selTrazo = t;
        selIndice = i;
        Vector3 objetivo = tipo == Objetivo.Nodo
            ? dibujo.transform.TransformPoint(t.nodos[i])
            : PuntaAsaMundo(t, i, salida);
        desfase = objetivo - pinza;
    }

    void ContinuarArrastre(Vector3 pinza)
    {
        if (arrTrazo == null || arrIndice < 0 || arrIndice >= arrTrazo.nodos.Count)
        {
            arrastre = Objetivo.Nada;
            return;
        }
        Vector3 local = dibujo.ProyectarEnPlano(dibujo.transform.InverseTransformPoint(pinza + desfase));

        if (deshacerPendiente)
        {
            // Guardamos para "deshacer" solo cuando de verdad se mueve algo.
            Vector3 actual = arrastre == Objetivo.Nodo
                ? arrTrazo.nodos[arrIndice]
                : arrTrazo.nodos[arrIndice] + (arrSalida ? arrTrazo.asaSalida[arrIndice] : arrTrazo.asaEntrada[arrIndice]);
            if (Vector3.Distance(local, actual) * dibujo.EscalaMundo < 0.003f)
                return;
            dibujo.GuardarParaDeshacer();
            deshacerPendiente = false;
        }

        if (arrastre == Objetivo.Asa)
        {
            arrTrazo.MoverAsa(arrIndice, arrSalida, local - arrTrazo.nodos[arrIndice]);
            return;
        }

        // Nodo: si es una punta y llega cerca de otra punta, se pega como imán.
        imanTrazo = null;
        int ultimo = arrTrazo.nodos.Count - 1;
        bool esPunta = !arrTrazo.cerrado && (arrIndice == 0 || arrIndice == ultimo);
        if (esPunta)
        {
            int otro = arrIndice == 0 ? ultimo : 0;
            if (arrTrazo.nodos.Count >= 4 && Vector3.Distance(local, arrTrazo.nodos[otro]) < dibujo.RadioImanLocal)
            {
                local = arrTrazo.nodos[otro];
                imanTrazo = arrTrazo;
                imanExtremo = otro == 0 ? 0 : 1;
            }
            else
            {
                Trazo o;
                int e;
                if (dibujo.BuscarExtremo(local, arrTrazo, out o, out e))
                {
                    local = e == 0 ? o.nodos[0] : o.nodos[o.nodos.Count - 1];
                    imanTrazo = o;
                    imanExtremo = e;
                }
            }
        }
        dibujo.MoverNodo(arrTrazo, arrIndice, local);
    }

    void TerminarArrastre()
    {
        if (arrastre == Objetivo.Nodo && imanTrazo != null && arrTrazo != null)
        {
            int extremoArrastrado = arrIndice == 0 ? 0 : 1;
            if (imanTrazo == arrTrazo)
                dibujo.CerrarTrazo(arrTrazo, extremoArrastrado == 1);
            else
                dibujo.Unir(arrTrazo, extremoArrastrado, imanTrazo, imanExtremo);
            selTrazo = null;
            selIndice = -1;
        }
        arrastre = Objetivo.Nada;
        arrTrazo = null;
        arrIndice = -1;
        imanTrazo = null;
    }

    // ---------- Grosor (todo, o un nodo) ----------

    void CambiarGrosor()
    {
        if (grosorTrazo != null)
        {
            if (!Der.valida || !Der.pellizco || grosorIndice >= grosorTrazo.nodos.Count)
            {
                // Al soltar el nodo, el grosor general sigue desde donde quedó.
                grosorTrazo = null;
                alturaInicialGrosor = Izq.pulgar.y - Mathf.Log(Mathf.Max(0.01f, factorGlobal)) / sensibilidadGrosor;
            }
            else
            {
                float m = grosorNodoInicio * Mathf.Exp((Der.PuntoPellizco.y - alturaDerGrosor) * sensibilidadGrosor);
                grosorTrazo.PonerGrosorNodo(grosorIndice, m);
            }
        }
        else
        {
            Trazo t = null;
            int i = -1;
            bool cerca = Der.valida && BuscarNodoCercano(Der.indice, Der.PuntoPellizco, radioAgarreNodo, dibujo.Seleccion, out t, out i);
            hoverTipo = cerca ? Objetivo.Nodo : Objetivo.Nada;
            hoverTrazo = t;
            hoverIndice = i;
            if (cerca && Der.empezoPellizco)
            {
                grosorTrazo = t;
                grosorIndice = i;
                grosorNodoInicio = t.GrosorDeNodo(i);
                alturaDerGrosor = Der.PuntoPellizco.y;
            }
            else
            {
                bool antes = dialUsado;
                ActualizarDial();
                if (dialUsado && !antes && !Mathf.Approximately(factorGlobal, 1f))
                {
                    // Empezaste a usar el dial: lo que se movió la mano izquierda no cuenta.
                    factorGlobal = 1f;
                    dibujo.AplicarFactorGrosor(1f);
                }
                if (!dialUsado && Izq.valida)
                {
                    factorGlobal = Mathf.Exp((Izq.pulgar.y - alturaInicialGrosor) * sensibilidadGrosor);
                    dibujo.AplicarFactorGrosor(factorGlobal);
                }
            }
        }
        MostrarModoNodos(false, dibujo.Seleccion);
    }

    // Dial de grosor: el índice derecho gira en un círculo pequeño (como un teléfono de disco).
    // Hacia la derecha (como el reloj) = más grueso; hacia la izquierda = más delgado. Una vuelta = el doble.
    void ActualizarDial()
    {
        if (!Der.valida || Der.pellizco || Cabeza == null)
        {
            dialPrevioValido = false;
            return;
        }
        Vector3 p = Der.indice;
        dialCentro = Vector3.Lerp(dialCentro, p, 1f - Mathf.Exp(-1f * Time.deltaTime));
        Vector3 eje = Cabeza.forward;
        Vector3 r = Vector3.ProjectOnPlane(p - dialCentro, eje);
        dialRadio = r.magnitude;
        if (dialRadio < 0.005f || dialRadio > 0.06f)
        {
            dialPrevioValido = false;
            return;
        }
        if (dialPrevioValido)
        {
            // SignedAngle es negativo cuando gira como el reloj (visto desde tus ojos).
            float paso = -Vector3.SignedAngle(dialPrevio, r, eje);
            if (Mathf.Abs(paso) < 45f)
                dialAngulo += paso;
            if (!dialUsado && Mathf.Abs(dialAngulo) > 40f)
                dialUsado = true;
            if (dialUsado)
                dibujo.ElegirAnchoPincel(anchoDialInicio * Mathf.Pow(2f, dialAngulo / 360f));
        }
        dialPrevio = r;
        dialPrevioValido = true;
    }

    // ---------- Borrador: puño izquierdo + tocar con el índice derecho ----------
    // Nodo y relleno: se borran al tocarlos. Cerca de un nodo, nunca se borra la línea.
    // Línea entera: hay que frotarla (ida y vuelta) lejos de sus nodos; se pone roja mientras.

    void Borrar()
    {
        hoverTipo = Objetivo.Nada;
        if (!Der.valida)
        {
            CancelarFrote();
            MostrarModoNodos(false, null);
            return;
        }
        Vector3 punta = Der.indice;
        Vector3 local = dibujo.transform.InverseTransformPoint(punta);
        float escala = dibujo.EscalaMundo;

        Objetivo tipo = Objetivo.Nada;
        Trazo t = null;
        int i = -1;
        bool frotando = froteTrazo != null && froteRecorrido > 0.012f && Dibujo.Editable(froteTrazo)
                        && froteTrazo.DistanciaACurva(local) * escala < froteTrazo.ancho * escala * 0.5f + 0.015f;
        if (frotando)
        {
            // Si ya empezaste a frotar, sigues frotando esa línea aunque pases cerca de un nodo.
            t = froteTrazo;
            tipo = Objetivo.Linea;
        }
        else if (BuscarNodoCercano(punta, punta, radioBorrarNodo, null, out t, out i))
        {
            tipo = Objetivo.Nodo;
        }
        else
        {
            float mejor = float.MaxValue;
            foreach (var o in dibujo.trazos)
            {
                if (!Dibujo.Editable(o))
                    continue;
                float d = o.DistanciaACurva(local) * escala;
                float limite = o.ancho * escala * 0.5f + 0.008f;
                if (d < limite && d < mejor)
                {
                    mejor = d;
                    t = o;
                    tipo = Objetivo.Linea;
                }
            }
            if (tipo == Objetivo.Nada)
            {
                foreach (var o in dibujo.trazos)
                {
                    if (Dibujo.Editable(o) && o.relleno && o.DentroDeRelleno(local, 0.012f / escala))
                    {
                        t = o;
                        tipo = Objetivo.Relleno;
                        break;
                    }
                }
            }
        }
        hoverTipo = tipo;
        hoverTrazo = t;
        hoverIndice = i;

        if (tipo == Objetivo.Nada || t == null)
        {
            armadoBorrar = true; // el dedo salió de todo: ya puede borrar otra vez
            CancelarFrote();
        }
        else if (tipo == Objetivo.Linea)
        {
            Frotar(t, punta);
        }
        else
        {
            CancelarFrote();
            // Para borrar otro nodo sin sacar el dedo, hay que moverlo un poquito.
            bool movido = Vector3.Distance(punta, posUltimoBorrado) > 0.012f;
            if (armadoBorrar || movido)
            {
                dibujo.GuardarParaDeshacer();
                if (tipo == Objetivo.Nodo)
                {
                    dibujo.Destello(dibujo.transform.TransformPoint(t.nodos[i]), 0.02f);
                    dibujo.QuitarNodo(t, i);
                }
                else
                {
                    dibujo.Destello(punta, 0.04f);
                    dibujo.QuitarRelleno(t);
                }
                armadoBorrar = false;
                posUltimoBorrado = punta;
                hoverTipo = Objetivo.Nada;
            }
        }
        MostrarModoNodos(false, null);
    }

    void Frotar(Trazo t, Vector3 punta)
    {
        if (froteTrazo != t)
        {
            CancelarFrote();
            froteTrazo = t;
            froteRecorrido = 0f;
            froteAncla = punta;
            return;
        }
        // Solo cuentan movimientos de más de 1 cm (así el temblor de la mano no borra nada).
        float paso = Vector3.Distance(punta, froteAncla);
        if (paso > 0.01f)
        {
            froteRecorrido += paso;
            froteAncla = punta;
        }
        if (froteRecorrido > 0.012f && dibujo.materialBorrado != null)
            t.PonerMaterialLinea(dibujo.materialBorrado);
        if (froteRecorrido < distanciaFrote)
            return;
        dibujo.RestaurarMaterial(t);
        dibujo.GuardarParaDeshacer();
        dibujo.BorrarTrazo(t, true);
        froteTrazo = null;
        froteRecorrido = 0f;
        armadoBorrar = false;
        posUltimoBorrado = punta;
    }

    void CancelarFrote()
    {
        if (froteTrazo != null)
            dibujo.RestaurarMaterial(froteTrazo);
        froteTrazo = null;
        froteRecorrido = 0f;
    }

    // ---------- Ver nodos y asas ----------

    void MostrarModoNodos(bool conAsas, Trazo solo)
    {
        if (!SeleccionValida)
        {
            selTrazo = null;
            selIndice = -1;
        }

        int n = 0;
        int imanIndice = imanTrazo != null ? (imanExtremo == 0 ? 0 : imanTrazo.nodos.Count - 1) : -1;
        Vector3 cabeza = Cabeza != null ? Cabeza.position : Vector3.zero;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || (solo != null && t != solo))
                continue;
            for (int i = 0; i < t.nodos.Count; i++)
            {
                if (n >= nodosVisibles.Count)
                {
                    var go = CrearCirculo("Nodo", materialNodo);
                    nodosVisibles.Add(go.transform);
                    nodosRender.Add(go.GetComponent<Renderer>());
                    nodosFiltro.Add(go.GetComponent<MeshFilter>());
                }
                var nodo = nodosVisibles[n];
                if (!nodo.gameObject.activeSelf)
                    nodo.gameObject.SetActive(true);
                Vector3 pos = dibujo.transform.TransformPoint(t.nodos[i]);
                nodo.position = pos;
                Vector3 mirar = pos - cabeza;
                if (mirar.sqrMagnitude > 1e-8f)
                    nodo.rotation = Quaternion.LookRotation(mirar);

                bool activo = (t == arrTrazo && i == arrIndice)
                              || (t == grosorTrazo && i == grosorIndice)
                              || (hoverTipo == Objetivo.Nodo && t == hoverTrazo && i == hoverIndice)
                              || (conAsas && t == selTrazo && i == selIndice);
                bool iman = t == imanTrazo && i == imanIndice;
                bool punta = !t.cerrado && (i == 0 || i == t.nodos.Count - 1);
                var malla = punta ? MallaAnillo() : MallaDisco();
                if (nodosFiltro[n].sharedMesh != malla)
                    nodosFiltro[n].sharedMesh = malla;
                float tam = tamanoNodo * (punta ? 1.3f : 1f) * (activo || iman ? 1.5f : 1f);
                nodo.localScale = Vector3.one * tam;
                var mat = iman ? materialIman : activo ? materialNodoActivo : materialNodo;
                if (mat != null && nodosRender[n].sharedMaterial != mat)
                    nodosRender[n].sharedMaterial = mat;
                n++;
            }
        }
        OcultarNodosDesde(n);
        MostrarAsas(conAsas);
    }

    // Las asas (como en Illustrator) se ven solo en el nodo seleccionado.
    void MostrarAsas(bool permitir)
    {
        puntosAsas.Clear();
        indicesAsas.Clear();
        bool hay = false;
        Vector3 cabeza = Cabeza != null ? Cabeza.position : Vector3.zero;
        for (int k = 0; k < 2; k++)
        {
            bool esSalida = k == 1;
            bool ver = permitir && SeleccionValida && selTrazo.AsaUsada(selIndice, esSalida);
            var asa = asasVisibles[k];
            if (asa == null)
                continue;
            if (asa.gameObject.activeSelf != ver)
                asa.gameObject.SetActive(ver);
            if (!ver)
                continue;
            Vector3 punta = PuntaAsaMundo(selTrazo, selIndice, esSalida);
            asa.position = punta;
            Vector3 mirar = punta - cabeza;
            if (mirar.sqrMagnitude > 1e-8f)
                asa.rotation = Quaternion.LookRotation(mirar);
            bool activa = (arrastre == Objetivo.Asa && arrSalida == esSalida)
                          || (arrastre == Objetivo.Nada && hoverTipo == Objetivo.Asa && hoverSalida == esSalida);
            asa.localScale = Vector3.one * tamanoNodo * (activa ? 1.2f : 0.8f);
            indicesAsas.Add(puntosAsas.Count);
            puntosAsas.Add(dibujo.transform.TransformPoint(selTrazo.nodos[selIndice]));
            indicesAsas.Add(puntosAsas.Count);
            puntosAsas.Add(punta);
            hay = true;
        }
        if (lineasAsas == null)
            return;
        if (lineasAsas.activeSelf != hay)
            lineasAsas.SetActive(hay);
        if (!hay)
            return;
        mallaLineasAsas.Clear();
        mallaLineasAsas.SetVertices(puntosAsas);
        mallaLineasAsas.SetIndices(indicesAsas, MeshTopology.Lines, 0);
        mallaLineasAsas.RecalculateBounds();
    }

    void OcultarModoNodos()
    {
        OcultarNodosDesde(0);
        foreach (var asa in asasVisibles)
            if (asa != null && asa.gameObject.activeSelf)
                asa.gameObject.SetActive(false);
        if (lineasAsas != null && lineasAsas.activeSelf)
            lineasAsas.SetActive(false);
    }

    void OcultarNodosDesde(int desde)
    {
        for (int i = desde; i < nodosVisibles.Count; i++)
            if (nodosVisibles[i].gameObject.activeSelf)
                nodosVisibles[i].gameObject.SetActive(false);
    }

    GameObject CrearCirculo(string nombre, Material mat)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = MallaDisco();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    // Círculo plano (siempre mira hacia ti). Diámetro 1.
    public static Mesh MallaDisco()
    {
        if (mallaDisco != null)
            return mallaDisco;
        const int lados = 20;
        var v = new Vector3[lados + 1];
        var tri = new int[lados * 3];
        v[0] = Vector3.zero;
        for (int i = 0; i < lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            v[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.5f;
        }
        for (int i = 0; i < lados; i++)
        {
            tri[i * 3] = 0;
            tri[i * 3 + 1] = 1 + (i + 1) % lados;
            tri[i * 3 + 2] = 1 + i;
        }
        mallaDisco = new Mesh { name = "Disco" };
        mallaDisco.vertices = v;
        mallaDisco.triangles = tri;
        mallaDisco.RecalculateNormals();
        mallaDisco.RecalculateBounds();
        return mallaDisco;
    }

    // Aro (círculo hueco), para las puntas de las líneas.
    public static Mesh MallaAnillo()
    {
        if (mallaAnillo != null)
            return mallaAnillo;
        const int lados = 20;
        var v = new Vector3[lados * 2];
        var tri = new int[lados * 6];
        for (int i = 0; i < lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            v[i * 2] = dir * 0.5f;      // borde de afuera
            v[i * 2 + 1] = dir * 0.3f;  // borde de adentro
        }
        for (int i = 0; i < lados; i++)
        {
            int o0 = i * 2, i0 = i * 2 + 1;
            int o1 = ((i + 1) % lados) * 2, i1 = ((i + 1) % lados) * 2 + 1;
            tri[i * 6] = o0; tri[i * 6 + 1] = i0; tri[i * 6 + 2] = o1;
            tri[i * 6 + 3] = i0; tri[i * 6 + 4] = i1; tri[i * 6 + 5] = o1;
        }
        mallaAnillo = new Mesh { name = "Anillo" };
        mallaAnillo.vertices = v;
        mallaAnillo.triangles = tri;
        mallaAnillo.RecalculateNormals();
        mallaAnillo.RecalculateBounds();
        return mallaAnillo;
    }

    // ---------- Deshacer y rehacer: la mano se vuelve una flecha y hay que tocar la diana ----------
    // Mano IZQUIERDA en puño con el pulgar hacia tu izquierda = deshacer (diana roja).
    // Mano DERECHA en puño con el pulgar hacia tu derecha = rehacer (diana verde).
    // La punta de la flecha va pegada a la punta del pulgar. Para repetir, retira la flecha y vuelve a tocar.
    // La flecha solo aparece si sostienes la pose 0.3 s con la mano quieta (y no justo después de otro gesto).

    void ActualizarFlechas()
    {
        bool libre = GestoIzq == Gesto.Ninguno && !menuAbierto && lineaMovida == null && imagenMovida == null;
        ActualizarFlecha(flechaIzq, Izq, true, libre && !flechaDer.activa);
        ActualizarFlecha(flechaDer, Der, false, libre && !flechaIzq.activa && !Der.pellizco);
    }

    void ActualizarFlecha(EstadoFlecha e, ManoSeguida mano, bool izquierda, bool permitido)
    {
        float dt = Mathf.Max(1e-4f, Time.deltaTime);
        Vector3 dirPulgar = Vector3.zero;
        bool pose = permitido && PoseFlecha(mano, izquierda, e.activa, out dirPulgar);
        float velocidad = mano.valida ? (mano.pulgar - e.ultimaPos).magnitude / dt : 0f;
        if (mano.valida)
            e.ultimaPos = mano.pulgar;

        if (!e.activa)
        {
            float finGesto = izquierda ? finGestoIzq : finGestoDer;
            if (!pose || Time.time - finGesto < 0.4f || velocidad > 0.35f)
            {
                e.candidatoDesde = -1f;
                return;
            }
            if (e.candidatoDesde < 0f)
                e.candidatoDesde = Time.time;
            if (Time.time - e.candidatoDesde < 0.3f)
                return;
            // Aparece la flecha y la diana queda fija delante de su punta.
            e.activa = true;
            e.armada = true;
            e.fueraDesde = -1f;
            e.dir = DirFlecha(dirPulgar, izquierda);
            e.punta = PuntaFlecha(mano);
            e.dirDiana = e.dir;
            e.posDiana = e.punta + e.dirDiana * distanciaDiana;
            OcultarMano(izquierda, true);
        }

        if (!permitido)
        {
            SalirFlecha(e, izquierda);
            return;
        }
        if (!pose)
        {
            // Si la pose se pierde un momento, la flecha espera un poquito antes de irse.
            if (e.fueraDesde < 0f)
                e.fueraDesde = Time.time;
            if (Time.time - e.fueraDesde > 0.35f)
                SalirFlecha(e, izquierda);
            return;
        }
        e.fueraDesde = -1f;

        // Suave, para que no tiemble.
        float a = 1f - Mathf.Exp(-14f * dt);
        e.dir = Vector3.Slerp(e.dir, DirFlecha(dirPulgar, izquierda), a).normalized;
        e.punta = Vector3.Lerp(e.punta, PuntaFlecha(mano), a);

        // ¿La punta tocó (o atravesó) la diana?
        Vector3 rel = e.punta - e.posDiana;
        float adelanteDiana = Vector3.Dot(rel, e.dirDiana);
        float aLado = (rel - e.dirDiana * adelanteDiana).magnitude;
        float radio = diametroDiana * 0.5f;
        if (e.armada && adelanteDiana > -0.01f && aLado < radio + 0.012f)
        {
            e.armada = false;
            bool rehacer = !izquierda;
            bool hecho = rehacer ? dibujo.Rehacer() : dibujo.Deshacer();
            if (hecho)
                MostrarEtiqueta(rehacer ? "Rehacer" : "Deshacer");
            if (simbolos != null)
                simbolos.Acertar(rehacer);
        }
        else if (!e.armada && adelanteDiana < -0.04f)
        {
            e.armada = true;
        }
        if (simbolos != null)
        {
            simbolos.Flecha(true, e.punta - e.dir * largoFlecha, e.dir, largoFlecha);
            simbolos.Diana(!izquierda, true, e.posDiana, e.posDiana - e.dirDiana, diametroDiana);
        }
    }

    void SalirFlecha(EstadoFlecha e, bool izquierda)
    {
        e.candidatoDesde = -1f;
        if (!e.activa)
            return;
        e.activa = false;
        e.fueraDesde = -1f;
        OcultarMano(izquierda, false);
        if (simbolos != null)
        {
            simbolos.Flecha(false, Vector3.zero, Vector3.forward, 0f);
            simbolos.Diana(!izquierda, false, Vector3.zero, Vector3.zero, 0f);
        }
    }

    // La punta de la flecha: en la punta del pulgar, un poquito más abajo.
    static Vector3 PuntaFlecha(ManoSeguida mano)
    {
        return mano.pulgar - Vector3.up * 0.008f;
    }

    // Dirección de la flecha: como el pulgar, pero nunca más arriba de lo horizontal (y como mucho 35° abajo).
    Vector3 DirFlecha(Vector3 dirPulgar, bool izquierda)
    {
        Vector3 h = new Vector3(dirPulgar.x, 0f, dirPulgar.z);
        if (h.sqrMagnitude < 1e-4f)
            h = LadoHorizontal(izquierda ? -1 : 1);
        h.Normalize();
        float inclinacion = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(dirPulgar.normalized.y, -1f, 1f)), -35f * Mathf.Deg2Rad, 0f);
        return (h * Mathf.Cos(inclinacion) + Vector3.up * Mathf.Sin(inclinacion)).normalized;
    }

    // Izquierda (lado = -1) o derecha (+1) de tu cabeza, en horizontal.
    Vector3 LadoHorizontal(int lado)
    {
        Vector3 derecha = Cabeza != null ? Cabeza.right : Vector3.right;
        derecha.y = 0f;
        if (derecha.sqrMagnitude < 1e-4f)
            derecha = Vector3.right;
        return derecha.normalized * lado;
    }

    // Puño (índice, medio y anular doblados) con el pulgar estirado hacia afuera:
    // izquierda → pulgar hacia tu izquierda; derecha → pulgar hacia tu derecha. Ya en la pose, es más tolerante.
    bool PoseFlecha(ManoSeguida mano, bool izquierda, bool yaActiva, out Vector3 dirPulgar)
    {
        dirPulgar = Vector3.zero;
        if (!mano.valida || Cabeza == null)
            return false;
        var esq = mano.esqueleto;
        var palma = ManosUtil.LeerPalma(esq, izquierda);
        Transform nudillo = ManosUtil.Hueso(esq, "Index1", "IndexProximal");
        Transform basePul = ManosUtil.Hueso(esq, "Thumb1", "ThumbMetacarpal");
        if (!palma.valida || nudillo == null || basePul == null)
            return false;
        float tam = palma.tamano;
        float curva = yaActiva ? 1.45f : 1.3f;
        if (Vector3.Distance(mano.indice, palma.centro) / tam > curva
            || Vector3.Distance(mano.medio, palma.centro) / tam > curva
            || Vector3.Distance(mano.anular, palma.centro) / tam > curva)
            return false;
        Vector3 d = mano.pulgar - basePul.position;
        if (d.magnitude < 0.045f || Vector3.Distance(mano.pulgar, nudillo.position) < (yaActiva ? 0.06f : 0.07f))
            return false;
        float lado = Vector3.Dot(d.normalized, LadoHorizontal(izquierda ? -1 : 1));
        if (lado < (yaActiva ? 0.35f : 0.55f))
            return false;
        dirPulgar = d.normalized;
        return true;
    }

    // ---------- Candado: doble toque rápido de pulgar + índice izquierdos ----------

    void RevisarCandado()
    {
        if (!Izq.valida)
            return;
        if (Izq.empezoPellizco)
        {
            toqueInicio = Time.time;
            if (Time.time - toqueUltimoFin > 0.4f)
                toques = 0;
        }
        if (Izq.soltoPellizco && toqueInicio >= 0f)
        {
            float duracion = Time.time - toqueInicio;
            toqueInicio = -1f;
            if (duracion < 0.3f)
            {
                toques++;
                toqueUltimoFin = Time.time;
                if (toques >= 2)
                {
                    toques = 0;
                    AlternarBloqueo();
                }
            }
            else
            {
                toques = 0;
            }
        }
    }

    public void AlternarBloqueo()
    {
        DibujoBloqueado = !DibujoBloqueado;
        if (DibujoBloqueado && trazoActual != null)
        {
            dibujo.CancelarTrazo(trazoActual);
            trazoActual = null;
        }
        dibujo.Mensaje(DibujoBloqueado ? "Dibujo bloqueado (doble toque para desbloquear)" : "Dibujo desbloqueado");
    }

    // ---------- Esconder una mano (cuando se vuelve borrador o flecha) ----------
    // Se esconden TODAS las partes visibles de esa mano (aunque estén en otro lugar del rig).

    void OcultarMano(bool izquierda, bool ocultar)
    {
        if (izquierda)
            ocultarIzq = ocultar;
        else
            ocultarDer = ocultar;
        if (ocultar)
        {
            BuscarRenderersMano();
            Forzar(izquierda ? rendsIzq : rendsDer, true);
        }
        else
        {
            Forzar(izquierda ? rendsIzq : rendsDer, false);
        }
    }

    static void Forzar(List<Renderer> lista, bool apagar)
    {
        foreach (var r in lista)
            if (r != null)
                r.forceRenderingOff = apagar;
    }

    void LateUpdate()
    {
        if (!ocultarIzq && !ocultarDer)
            return;
        if (Time.time >= proximaBusquedaManos)
            BuscarRenderersMano();
        if (ocultarIzq)
            Forzar(rendsIzq, true);
        if (ocultarDer)
            Forzar(rendsDer, true);
    }

    void BuscarRenderersMano()
    {
        proximaBusquedaManos = Time.time + 1f;
        Juntar(rendsIzq, Izq, rig != null ? rig.leftHandAnchor : null, true);
        Juntar(rendsDer, Der, rig != null ? rig.rightHandAnchor : null, false);
    }

    void Juntar(List<Renderer> lista, ManoSeguida mano, Transform ancla, bool izquierda)
    {
        // Las que estaban escondidas y ya no son de la mano, se vuelven a mostrar.
        bool escondida = izquierda ? ocultarIzq : ocultarDer;
        var nuevas = new List<Renderer>();
        if (mano.hand != null)
            nuevas.AddRange(mano.hand.GetComponentsInChildren<Renderer>(true));
        if (mano.esqueleto != null)
            nuevas.AddRange(mano.esqueleto.GetComponentsInChildren<Renderer>(true));
        if (ancla != null)
            nuevas.AddRange(ancla.GetComponentsInChildren<Renderer>(true));
        // Manos dibujadas en otro lugar (por ejemplo "HandVisualLeft"): se reconocen por el nombre.
        foreach (var r in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (EsDeLaMano(r.transform, izquierda))
                nuevas.Add(r);
        foreach (var r in lista)
            if (r != null && !nuevas.Contains(r) && escondida)
                r.forceRenderingOff = false;
        lista.Clear();
        foreach (var r in nuevas)
            if (r != null && !r.transform.IsChildOf(transform) && !lista.Contains(r))
                lista.Add(r);
    }

    static bool EsDeLaMano(Transform t, bool izquierda)
    {
        bool mano = false, lado = false;
        for (int k = 0; t != null && k < 8; k++, t = t.parent)
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains("hand") || n.Contains("mano"))
                mano = true;
            if (izquierda ? (n.Contains("left") || n.StartsWith("l_") || n.Contains("_l_"))
                          : (n.Contains("right") || n.StartsWith("r_") || n.Contains("_r_")))
                lado = true;
        }
        return mano && lado;
    }

    // ---------- Pellizcar una línea: seleccionar y mover ----------

    void RevisarAgarreLinea()
    {
        if (lineaMovida != null)
        {
            if (!Der.valida || !Der.pellizco || !Dibujo.Editable(lineaMovida))
            {
                lineaMovida = null;
                return;
            }
            Transform raiz = dibujo.transform;
            Vector3 delta = raiz.InverseTransformPoint(Der.PuntoPellizco) - raiz.InverseTransformPoint(inicioLinea);
            delta = dibujo.ProyectarVectorEnPlano(delta);
            if (deshacerLineaPendiente)
            {
                if (delta.magnitude * dibujo.EscalaMundo < 0.003f)
                    return;
                dibujo.GuardarParaDeshacer();
                deshacerLineaPendiente = false;
            }
            lineaMovida.Desplazar(baseLinea, delta);
            return;
        }
        if (imagenMovida != null)
        {
            if (!Der.valida || !Der.pellizco)
            {
                SoltarImagen();
                return;
            }
            if (referencias != null)
                referencias.MoverA(imagenMovida, Der.PuntoPellizco + desfaseImagen);
            return;
        }
        if (!Der.valida || !Der.empezoPellizco)
            return;
        // Pellizcos sobre el panel de arriba son del panel.
        if (panelArriba != null && panelArriba.Contiene(Der.PuntoPellizco))
            return;
        var t = LineaBajo(Der.PuntoPellizco, Der.indice);
        dibujo.Seleccionar(t);
        if (t == null)
        {
            // ¿Una imagen de referencia? Se selecciona y se mueve. En el aire: nada seleccionado.
            Transform imagen = referencias != null ? referencias.BuscarBajo(Der.PuntoPellizco) : null;
            if (referencias != null)
                referencias.Seleccionar(imagen);
            if (imagen != null)
            {
                imagenMovida = imagen;
                desfaseImagen = imagen.position - Der.PuntoPellizco;
            }
            return;
        }
        if (referencias != null)
            referencias.Seleccionar(null);
        lineaMovida = t;
        baseLinea.Clear();
        baseLinea.AddRange(t.nodos);
        inicioLinea = Der.PuntoPellizco;
        deshacerLineaPendiente = true;
    }

    void SoltarImagen()
    {
        if (imagenMovida == null)
            return;
        var soltada = imagenMovida;
        imagenMovida = null;
        if (referencias != null)
            referencias.AlSoltar(soltada);
    }

    // La línea (o figura rellena) más cercana a la pinza, si está lo bastante cerca.
    Trazo LineaBajo(Vector3 pinza, Vector3 punta)
    {
        Transform raiz = dibujo.transform;
        Vector3 localPinza = raiz.InverseTransformPoint(pinza);
        Vector3 localPunta = raiz.InverseTransformPoint(punta);
        float escala = dibujo.EscalaMundo;
        Trazo mejor = null;
        float mejorDistancia = float.MaxValue;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t))
                continue;
            float d = Mathf.Min(t.DistanciaACurva(localPinza), t.DistanciaACurva(localPunta)) * escala;
            float limite = Mathf.Max(0.02f, t.ancho * escala * 0.5f + 0.012f);
            if (d < limite && d < mejorDistancia)
            {
                mejorDistancia = d;
                mejor = t;
            }
        }
        if (mejor != null)
            return mejor;
        foreach (var t in dibujo.trazos)
            if (Dibujo.Editable(t) && t.relleno && t.DentroDeRelleno(localPinza, 0.03f / escala))
                return t;
        return null;
    }

    // ---------- Tocar un relleno para cambiar su color ----------

    void RevisarToqueRelleno()
    {
        if (!Der.valida)
        {
            tocandoRelleno = false;
            return;
        }
        Vector3 local = dibujo.transform.InverseTransformPoint(Der.indice);
        float grosor = 0.01f / dibujo.EscalaMundo;
        Trazo tocado = null;
        foreach (var t in dibujo.trazos)
        {
            if (Dibujo.Editable(t) && t.cerrado && t.DentroDeRelleno(local, grosor))
            {
                tocado = t;
                break;
            }
        }
        if (tocado != null && !tocandoRelleno && Time.time >= proximoToque)
        {
            dibujo.CambiarColorRelleno(tocado);
            proximoToque = Time.time + 0.5f;
        }
        tocandoRelleno = tocado != null;
    }

    // ---------- Cursor y menú ----------

    void ActualizarCursor()
    {
        if (cursor == null)
            return;
        bool ver = Der.valida && GestoIzq != Gesto.Transformar && !flechaDer.activa;
        if (cursor.gameObject.activeSelf != ver)
            cursor.gameObject.SetActive(ver);
        if (!ver)
            return;
        cursor.position = Der.indice;
        bool borrando = GestoIzq == Gesto.Borrar;
        var mat = borrando && materialCursorBorrar != null ? materialCursorBorrar : materialCursor;
        if (mat != null && cursorRender.sharedMaterial != mat)
            cursorRender.sharedMaterial = mat;
        cursor.localScale = Vector3.one * (borrando ? 0.012f : Mathf.Max(0.003f, dibujo.AnchoNuevoMundo));
    }

    // Menú: mano izquierda abierta con la punta del pulgar en la base de los dedos.
    // Una vez abierto, se queda aunque la mano derecha tape un momento a la izquierda
    // (eso pasa al tocar los botones). Se cierra al hacer otro gesto o al quitar el pulgar un rato.
    void ActualizarMenu()
    {
        if (GestoIzq != Gesto.Ninguno || Cabeza == null)
        {
            menuAbierto = false;
            return;
        }
        bool pose = poseValida
                    && distanciaMenu < (menuAbierto ? 0.09f : 0.045f)
                    && (menuAbierto || DedosAbiertos());
        if (pose)
        {
            menuAbierto = true;
            menuFueraDesde = -1f;
            PosicionarMenu();
            return;
        }
        if (!menuAbierto)
            return;
        if (menuFueraDesde < 0f)
            menuFueraDesde = Time.time;
        float espera = poseValida ? 0.7f : 1.5f;
        if (Time.time - menuFueraDesde > espera)
            menuAbierto = false;
        else if (poseValida)
            PosicionarMenu();
    }

    void PosicionarMenu()
    {
        Vector3 haciaCabeza = Cabeza.position - palmaIzq.centro;
        if (haciaCabeza.sqrMagnitude < 1e-6f)
            return;
        // Unos centímetros hacia ti y un poco arriba, para que la mano derecha no tape a la izquierda.
        Vector3 pos = palmaIzq.centro + haciaCabeza.normalized * 0.1f + Vector3.up * 0.06f;
        Vector3 mirar = pos - Cabeza.position;
        if (mirar.sqrMagnitude < 1e-6f)
            return;
        posMenu = pos;
        rotMenu = Quaternion.LookRotation(mirar, Vector3.up);
    }

    public bool PuedeVerPanel(bool yaVisible, out Vector3 posicion, out Quaternion rotacion)
    {
        posicion = posMenu;
        rotacion = rotMenu;
        return menuAbierto;
    }

    // ---------- Etiqueta sobre la mano (para aprender los gestos) ----------

    void MostrarEtiqueta(string texto)
    {
        etiquetaTemporal = texto;
        etiquetaHasta = Time.time + 0.9f;
    }

    void ActualizarEtiqueta()
    {
        if (textoGesto == null)
            return;
        string texto = null;
        ManoSeguida sobre = Izq;
        bool entreManos = false;
        bool hayLinea = dibujo.Seleccion != null;
        switch (GestoIzq)
        {
            case Gesto.Dibujar: texto = DibujoBloqueado ? "Bloqueado (doble toque)" : lejosDelPlano ? "Dibujar (acerca el dedo al plano)" : "Dibujar"; break;
            case Gesto.Recta: texto = DibujoBloqueado ? "Bloqueado (doble toque)" : lejosDelPlano ? "Línea recta (acerca el dedo al plano)" : "Línea recta"; break;
            case Gesto.Nodos: texto = hayLinea ? "Editar nodos (esta línea)" : "Editar nodos"; break;
            case Gesto.Grosor:
                texto = dialUsado ? "Líneas nuevas: " + Mathf.RoundToInt(dibujo.AnchoNuevoMundo * 1000f) + " mm"
                      : hayLinea ? "Grosor (esta línea)\ngira el índice = líneas nuevas"
                      : "Grosor (todo)\ngira el índice = líneas nuevas";
                if (dialUsado)
                    sobre = Der;
                break;
            case Gesto.Borrar: texto = "Borrar\n(frota la línea para borrarla entera)"; sobre = Der; break;
            case Gesto.Transformar:
                texto = hayLinea ? "Girar / escalar línea"
                      : referencias != null && referencias.Seleccionada != null ? "Girar / escalar imagen"
                      : "Girar / escalar todo";
                entreManos = true;
                break;
        }
        if (texto == null)
        {
            if (Time.time < etiquetaHasta)
            {
                texto = etiquetaTemporal;
            }
            else if (lineaMovida != null)
            {
                texto = "Mover línea";
                sobre = Der;
            }
            else if (imagenMovida != null)
            {
                texto = "Mover imagen";
                sobre = Der;
            }
        }
        bool ver = mostrarAyudas && texto != null && Cabeza != null
                   && (entreManos ? Izq.valida && Der.valida : sobre.valida);
        if (textoGesto.gameObject.activeSelf != ver)
            textoGesto.gameObject.SetActive(ver);
        if (!ver)
            return;
        if (textoGesto.text != texto)
            textoGesto.text = texto;
        Vector3 pos = entreManos
            ? (Izq.PuntoPellizco + Der.PuntoPellizco) * 0.5f
            : (sobre.indice + sobre.pulgar + sobre.medio) / 3f;
        pos += Vector3.up * 0.09f;
        Vector3 mirar = pos - Cabeza.position;
        if (mirar.sqrMagnitude > 1e-6f)
            textoGesto.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mirar, Vector3.up));
    }
}

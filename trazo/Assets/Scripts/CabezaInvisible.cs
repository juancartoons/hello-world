using System.Collections.Generic;
using TMPro;
using UnityEngine;

// LA CABEZA INVISIBLE (v51, v52, v53): dibujas la cabeza de tu personaje UNA vez (DE FRENTE o DE PERFIL) en una capa:
// el contorno y los rasgos (ojos, nariz, boca, orejas; sin cabello por ahora). La app arma con eso un HUEVO 3D invisible,
// pega cada rasgo en su superficie y te deja GIRARLA (frente, 3/4 y perfil para los dos lados, y un poco arriba/abajo).
//  - v53: la vista que DIBUJASTE se ve IDÉNTICA (son tus líneas de verdad) y TU ORIGINAL NUNCA CAMBIA: queda como clave
//    en su fotograma (y como "Original" en la biblioteca). "Usar esta vista" la pone en OTRO fotograma (6 más adelante
//    si estás en el del original) con LOS MISMOS NODOS: entre las vistas hay morph.
//  - v53: la vista previa al girar usa líneas iguales a las tuyas (mismo grosor, color, textura y vida), no un dibujo
//    de prueba. Las otras vistas son una propuesta: retócalas con Nodos y toca "Guardar vista": la biblioteca guarda TU
//    versión retocada (como un símbolo de Flash).
//  - v53: ETIQUETAS con botones: cada parte que la app reconoce tiene su cartelito ("boca?") con un visto verde
//    (confirmar), una X roja (quitar) y un cuadrito azul (cambiar el nombre: con el teclado o diciéndolo con la voz).
//  - v53: VOCABULARIO a la derecha: todas las palabras que la app entiende. Pellizca una y arrástrala a tu dibujo:
//    nombra el nodo donde la sueltas (o los nodos que elegiste con el lazo).
//  - v53: un MINITUTORIAL paso a paso (se puede quitar con "No mostrar más"; "Ayuda" lo vuelve a abrir).
//  - Con la cabeza abierta, la voz entiende "boca", "nariz", "ojo", "ceja", "oreja", "contorno" (nombran los nodos
//    elegidos con el lazo, aunque ya soltaste la izquierda) y los puntos clave ("punta de nariz", "barbilla"...).
//  - Trampas del 2D: dibujada de PERFIL, el ojo (y la oreja) se copian al otro lado para la vista de frente; la nariz
//    y la boca van en la línea del centro. Dibujada DE FRENTE, la nariz sobresale y las orejas son discos a los lados.
public class CabezaInvisible : MonoBehaviour
{
    // ---------- Puntos clave y palabras ----------
    public static readonly string[] PuntosEs =
    {
        "punta de nariz", "puente de la nariz", "ojo izquierdo", "ojo derecho", "párpado", "ceja", "entrecejo",
        "comisura izquierda", "comisura derecha", "labio superior", "labio inferior", "barbilla", "mandíbula", "pómulo",
        "frente", "coronilla", "nuca", "sien", "oreja", "lóbulo",
        "ceja izquierda", "ceja derecha", "oreja izquierda", "oreja derecha"   // v54
    };
    public static readonly string[] PuntosEn =
    {
        "nose tip", "nose bridge", "left eye", "right eye", "eyelid", "eyebrow", "between the eyebrows",
        "left mouth corner", "right mouth corner", "upper lip", "lower lip", "chin", "jaw", "cheekbone",
        "forehead", "crown", "nape", "temple", "ear", "earlobe",
        "left eyebrow", "right eyebrow", "left ear", "right ear"
    };
    // 0 nariz, 1 ojo, 2 boca, 3 contorno, 4 oreja
    static readonly int[] CategoriaPunto = { 0, 0, 1, 1, 1, 1, 0, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 4, 4, 1, 1, 4, 4 };
    static readonly string[] NombresCategoria = { "nariz", "ojo", "boca", "contorno", "oreja" };
    // Las palabras CORTAS (lo que se guarda) y cómo se pueden decir.
    static readonly string[] Cortas = { "boca", "nariz", "ojo", "ceja", "oreja", "contorno", "mejilla" };
    static readonly string[][] CortasDichas =
    {
        new[] { "boca", "labios", "mouth", "lips" }, new[] { "nariz", "nose" }, new[] { "ojo", "ojos", "eye", "eyes" },
        new[] { "ceja", "cejas", "eyebrow", "eyebrows" }, new[] { "oreja", "orejas", "ear", "ears" },
        new[] { "contorno", "outline" }, new[] { "mejilla", "cheek" }
    };
    static readonly string[] CortasEn = { "mouth", "nose", "eye", "eyebrow", "ear", "outline", "cheek" };
    const string ClaveTutorial = "jcartoons_tuto_cabeza";

    public static string[] FrasesEs(int i)
    {
        string p = PuntosEs[i];
        if (p == "punta de nariz")
            return new[] { p, "punta de la nariz" };
        if (p.StartsWith("ojo "))
            return new[] { p, "centro " + p };
        return new[] { p };
    }

    // v53: una palabra corta dicha (ya sin acentos ni mayúsculas) → lo que se guarda ("boca"...). null si no es.
    public static string PalabraCorta(string dicho)
    {
        dicho = (dicho ?? "").Trim();
        for (int g = 0; g < CortasDichas.Length; g++)
            foreach (var p in CortasDichas[g])
                if (dicho == p)
                    return Cortas[g];
        return null;
    }

    // La categoría de un nombre (también los escritos con el teclado: busca palabras conocidas).
    static int CategoriaDeNombre(string n)
    {
        if (string.IsNullOrEmpty(n))
            return -1;
        n = n.ToLowerInvariant();
        for (int i = 0; i < PuntosEs.Length; i++)
            if (n == PuntosEs[i] || n == PuntosEn[i])
                return CategoriaPunto[i];
        if (n.Contains("nariz") || n.Contains("nose") || n.Contains("entrecejo"))
            return 0;
        if (n.Contains("ojo") || n.Contains("eye") || n.Contains("ceja") || n.Contains("párpado") || n.Contains("parpado"))
            return 1;
        if (n.Contains("boca") || n.Contains("mouth") || n.Contains("labio") || n.Contains("lip") || n.Contains("comisura"))
            return 2;
        if (n.Contains("oreja") || n.Contains("ear") || n.Contains("lóbulo") || n.Contains("lobulo"))
            return 4;
        if (n.Contains("barbilla") || n.Contains("chin") || n.Contains("mandíbula") || n.Contains("jaw") || n.Contains("frente")
            || n.Contains("forehead") || n.Contains("nuca") || n.Contains("coronilla") || n.Contains("pómulo") || n.Contains("sien")
            || n.Contains("contorno") || n.Contains("outline") || n.Contains("mejilla") || n.Contains("cheek"))
            return 3;
        return -1;
    }

    static CabezaInvisible instancia;
    public static bool Activa => instancia != null && instancia.estado != Estado.Nada;

    // v53: el cuadrito de una etiqueta espera un nombre: el que digas con la voz (o escribas) va a esa línea.
    public static bool NombrarPendiente(string nombre)
    {
        if (!Activa || instancia.pendiente == 0 || string.IsNullOrEmpty(nombre))
            return false;
        int id = instancia.pendiente;
        instancia.pendiente = 0;
        TecladoVR.Cerrar();
        instancia.NombrarLinea(id, nombre);
        return true;
    }

    // v53: alguien nombró nodos (voz o teclado): la cabeza vuelve a mirar qué es cada línea.
    public static void AlNombrar()
    {
        if (Activa)
            instancia.AlCambiarNombres();
    }

    enum Estado { Nada, Preguntando, Girando }
    enum Tipo { Rasgo, Centro, Boca, Oreja, OjoPerfil, NarizFrente }

    // Una línea del modelo (en el espacio de tu dibujo original, respecto al centro de la cabeza).
    class Linea
    {
        public int idOrigen;               // la línea tuya
        public bool espejo;                // la copia del otro lado (dibujada de perfil)
        public Tipo tipo;
        public bool nombrada;              // su tipo viene de un nombre (no adivinado)
        public Vector2 c;                  // su centro (en tu dibujo)
        public float x0, x1;
        public readonly List<Vector3> nodos = new List<Vector3>();      // sus nodos sobre el huevo
        public readonly List<Vector3> asaE = new List<Vector3>(), asaS = new List<Vector3>();  // punta de cada tirador
        public readonly List<bool> manual = new List<bool>();
        public readonly List<float> prof = new List<float>();           // cuánto se salía de tu plano (dibujos en 3D)
    }

    class VistaGuardada
    {
        public string nombre;
        public float giro, inclina;
        public bool esOriginal;
        public List<DatosTrazo> pose;      // TU versión (retocada); null = solo el ángulo
    }

    Estado estado;
    ControlManos control;
    Dibujo dibujo;
    int capaCabeza = -1;
    readonly List<int> idsOriginales = new List<int>();
    // Tu dibujo tal como estaba al empezar (el modelo SIEMPRE se arma con esto, estés en el fotograma que estés).
    readonly Dictionary<int, DatosTrazo> datosOriginales = new Dictionary<int, DatosTrazo>();
    readonly Dictionary<int, int> espejos = new Dictionary<int, int>();   // línea tuya → su copia del otro lado
    readonly List<Linea> lineas = new List<Linea>();
    int idContorno;
    readonly List<float> angulosContorno = new List<float>();
    readonly List<Vector2> contornoOrig = new List<Vector2>();
    readonly List<float> contornoProf = new List<float>();
    Vector3 centro, derecha, arriba, haciaTi;
    Vector2 centro2D;
    float semiX, semiY, semiZ;
    readonly float[] proporcion = new float[72];
    bool perfil, perfilAdivinado, hayModelo;
    float mira = 1f;
    float giro, inclina;
    bool agarrada;
    Vector3 agarreInicio;
    float giroInicio, inclinaInicio;
    bool verEtiquetas = true;
    readonly List<VistaGuardada> vistas = new List<VistaGuardada>();
    int fotogramaOriginal;
    bool mostrandoReal = true;        // se ven tus líneas de verdad (no la vista previa)
    int fotogramaVisto = -1;
    int pendiente;                    // la línea cuyo nombre se espera (el cuadrito de su etiqueta)
    readonly HashSet<int> sinEtiqueta = new HashSet<int>();
    bool sucio = true;

    // La vista previa: líneas sueltas iguales a las tuyas.
    readonly List<Trazo> clones = new List<Trazo>();
    Trazo cloneContorno;
    readonly List<Renderer> apagados = new List<Renderer>();
    readonly List<Vector3> silueta = new List<Vector3>();

    // El panel, las etiquetas, el vocabulario y el tutorial.
    Transform raiz, panel;
    Material normal, marcado, matPanel, matSi, matNo, matCambiar;
    readonly List<GameObject> filaPregunta = new List<GameObject>(), filaGiro = new List<GameObject>(), filaVistas = new List<GameObject>();

    class Chip
    {
        public Transform t;
        public TextMeshPro texto;
        public GameObject botones;
        public int id;
        public string palabra;   // lo que se confirma con "Sí"
    }
    readonly List<Chip> chips = new List<Chip>();

    Transform vocabulario;
    readonly List<TextMeshPro> palabrasVista = new List<TextMeshPro>();
    readonly List<string> palabras = new List<string>();
    int arrastrando = -1;
    TextMeshPro palabraFlotante;
    Transform marcaDestino;

    Transform tarjeta;
    TextMeshPro tarjetaTitulo, tarjetaTexto;
    int paso;
    static readonly string[] PasosTitulo = { "Dibuja la cabeza", "¿De frente o de perfil?", "Revisa las etiquetas", "Gira y usa la vista", "Retoca y guarda" };
    static readonly string[] PasosTexto =
    {
        "En una capa normal, dibuja el CONTORNO de la cabeza y sus rasgos: ojos, nariz, boca y orejas (sin cabello). Luego vuelve a tocar Cabeza invisible.",
        "Toca \"De frente\" o \"De perfil\" según como la dibujaste. Esa vista queda IDÉNTICA a tu dibujo y tu original nunca cambia.",
        "Cada parte tiene su cartelito: visto verde = confirmar, X roja = quitar, cuadrito azul = las 3 palabras más probables o el teclado. EL SELLO: toca una palabra de la lista de la derecha, toca los nodos (o las líneas) que son eso y haz un pellizco con la izquierda para confirmar. También: mira una línea y di su nombre.",
        "Gírala con los botones (Perfil, 3/4, Frente) o pellizcando la cabeza con la derecha. Toca \"Usar esta vista\": se pone en OTRO fotograma, con tus mismos nodos (morph). Tu original sigue en el suyo.",
        "Las otras vistas son una propuesta: retócalas con Nodos y toca \"Guardar vista\". La biblioteca guarda TU versión; tócala en cualquier fotograma. \"Original\" siempre vuelve a tu dibujo."
    };

    // ---------- Entrar / salir ----------

    public static void Alternar()
    {
        if (Activa)
        {
            instancia.Cerrar();
            return;
        }
        if (instancia == null)
            instancia = new GameObject("CabezaInvisible").AddComponent<CabezaInvisible>();
        instancia.Abrir();
    }

    void Abrir()
    {
        control = ControlManos.Instancia;
        dibujo = control != null ? control.dibujo : FindFirstObjectByType<Dibujo>();
        if (control == null || dibujo == null || control.Cabeza == null)
            return;
        // Si ya hay una cabeza armada (y sus líneas siguen ahí), vuelve a ella con su biblioteca.
        if (hayModelo && ModeloVigente())
        {
            if (dibujo.capaActual != capaCabeza)
                dibujo.SeleccionarCapa(capaCabeza);
            ArmarPanel();
            Mostrar(filaPregunta, false);
            Mostrar(filaGiro, true);
            RefrescarVistas();
            estado = Estado.Girando;
            mostrandoReal = true;
            sucio = true;
            TutorialAlAbrir(3);
            dibujo.Mensaje("Cabeza invisible: tu cabeza y sus vistas guardadas");
            return;
        }
        Empezar();
    }

    bool ModeloVigente()
    {
        foreach (int id in idsOriginales)
            if (dibujo.BuscarPorId(id) == null)
                return false;
        return idsOriginales.Count > 0;
    }

    List<Trazo> Originales()
    {
        var l = new List<Trazo>();
        foreach (int id in idsOriginales)
        {
            var t = dibujo.BuscarPorId(id);
            if (t != null)
                l.Add(t);
        }
        return l;
    }

    // Todas las líneas de la cabeza: las tuyas y las copias del otro lado.
    List<Trazo> LineasCabeza()
    {
        var l = Originales();
        foreach (var kv in espejos)
        {
            var t = dibujo.BuscarPorId(kv.Value);
            if (t != null && !l.Contains(t))
                l.Add(t);
        }
        return l;
    }

    void Empezar()
    {
        hayModelo = false;
        vistas.Clear();
        espejos.Clear();
        sinEtiqueta.Clear();
        idsOriginales.Clear();
        datosOriginales.Clear();
        BorrarClones();
        if (dibujo.EsCuadroACuadro(dibujo.capaActual))
        {
            dibujo.Mensaje("La cabeza invisible usa una capa normal (con morph), no una cuadro a cuadro");
            return;
        }
        var originales = new List<Trazo>();
        foreach (var t in dibujo.trazos)
            if (Dibujo.Editable(t) && !t.oculto && t.capa == dibujo.capaActual && t.curva.Count >= 2 && t.nodos.Count >= 2)
            {
                originales.Add(t);
                idsOriginales.Add(t.id);
                datosOriginales[t.id] = t.CrearDatos();
            }
        if (originales.Count < 2)
        {
            dibujo.Mensaje("Cabeza invisible: dibuja en esta capa el contorno de la cabeza y sus rasgos (ojos, nariz, boca, orejas)");
            AvanzarTutorial(0);
            return;
        }
        capaCabeza = dibujo.capaActual;
        fotogramaOriginal = dibujo.animacion != null ? dibujo.animacion.Fotograma : 0;
        Vector3 suma = Vector3.zero;
        int n = 0;
        foreach (var t in originales)
            foreach (var p in t.curva)
            {
                suma += t.transform.TransformPoint(p);
                n++;
            }
        centro = suma / Mathf.Max(1, n);
        Transform cab = control.Cabeza;
        Vector3 adelante = (centro - cab.position).normalized;
        if (adelante.sqrMagnitude < 1e-6f)
            adelante = cab.forward;
        derecha = Vector3.Cross(Vector3.up, adelante).normalized;
        if (derecha.sqrMagnitude < 1e-6f)
            derecha = cab.right;
        arriba = Vector3.Cross(adelante, derecha).normalized;
        haciaTi = -adelante;

        // El contorno: el que nombraste como contorno (barbilla, frente...); si no, la línea más grande.
        Trazo contorno = null;
        foreach (var t in originales)
            if (CategoriaDeLinea(t) == 3)
                contorno = t;
        if (contorno == null)
        {
            float mayor = -1f;
            foreach (var t in originales)
            {
                Vector2 mn, mx;
                Caja(t, out mn, out mx);
                float area = (mx.x - mn.x) * (mx.y - mn.y);
                if (area > mayor)
                {
                    mayor = area;
                    contorno = t;
                }
            }
        }
        idContorno = contorno.id;
        Vector2 cmn, cmx;
        Caja(contorno, out cmn, out cmx);
        centro2D = (cmn + cmx) * 0.5f;
        semiX = Mathf.Max(0.01f, (cmx.x - cmn.x) * 0.5f);
        semiY = Mathf.Max(0.01f, (cmx.y - cmn.y) * 0.5f);
        // ¿De perfil? (la adivinanza para las etiquetas): algo sobresale mucho del contorno por un solo lado (la nariz).
        int fueraIzq = 0, fueraDer = 0;
        foreach (var t in originales)
            if (t != contorno)
                foreach (var p in t.curva)
                {
                    Vector2 q = Plano(t.transform.TransformPoint(p));
                    if (q.x > semiX * 1.02f) fueraDer++;
                    if (q.x < -semiX * 1.02f) fueraIzq++;
                }
        perfilAdivinado = (fueraDer > 3 && fueraIzq == 0) || (fueraIzq > 3 && fueraDer == 0);
        miraAdivinada = fueraIzq > fueraDer ? -1f : 1f;
        CalcularSugerencias(originales, perfilAdivinado, miraAdivinada);
        ArmarPanel();
        Mostrar(filaPregunta, true);
        Mostrar(filaGiro, false);
        Mostrar(filaVistas, false);
        estado = Estado.Preguntando;
        mostrandoReal = true;
        sucio = true;
        TutorialAlAbrir(1);
        dibujo.Mensaje("Cabeza invisible: ¿la dibujaste de frente o de perfil?");
    }

    void Cerrar()
    {
        SoltarPalabra(false);
        Desesconder();
        BorrarClones();
        estado = Estado.Nada;
        agarrada = false;
        pendiente = 0;
        if (raiz != null)
            raiz.gameObject.SetActive(false);
        if (tarjeta != null)
            tarjeta.gameObject.SetActive(false);
        if (control != null && control.Ocupado)
            control.Ocupado = false;
    }

    // ---------- Esconder tus líneas mientras se ve la vista previa ----------

    // Cada cuadro (una línea que se redibuja se vuelve a prender sola: por eso antes se veían dos caras).
    void EsconderOriginales()
    {
        foreach (var t in LineasCabeza())
            foreach (var r in t.GetComponentsInChildren<Renderer>())
                if (!r.forceRenderingOff)
                {
                    r.forceRenderingOff = true;
                    if (!apagados.Contains(r))
                        apagados.Add(r);
                }
    }

    void Desesconder()
    {
        if (apagados.Count == 0)
            return;
        foreach (var r in apagados)
            if (r != null)
                r.forceRenderingOff = false;
        apagados.Clear();
        // Cada línea vuelve a decidir qué se ve (las líneas "hueso" u ocultas siguen ocultas).
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        foreach (var t in LineasCabeza())
            t.Reconstruir(true);
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
    }

    void BorrarClones()
    {
        foreach (var c in clones)
            if (c != null)
                Destroy(c.gameObject);
        clones.Clear();
        if (cloneContorno != null)
            Destroy(cloneContorno.gameObject);
        cloneContorno = null;
    }

    void OcultarClones()
    {
        foreach (var c in clones)
            if (c != null && c.gameObject.activeSelf)
                c.gameObject.SetActive(false);
        if (cloneContorno != null && cloneContorno.gameObject.activeSelf)
            cloneContorno.gameObject.SetActive(false);
    }

    // ---------- Armar el modelo ----------

    int CategoriaDeLinea(Trazo t)
    {
        foreach (var n in t.nombresNodo)
        {
            if (string.IsNullOrEmpty(n))
                continue;
            int dos = n.IndexOf(':');
            int c = CategoriaDeNombre(dos >= 0 ? n.Substring(dos + 1) : n);
            if (c >= 0)
                return c;
        }
        return -1;
    }

    static string PrimerNombre(Trazo t)
    {
        foreach (var n in t.nombresNodo)
        {
            if (string.IsNullOrEmpty(n))
                continue;
            int dos = n.IndexOf(':');
            string s = dos >= 0 ? n.Substring(dos + 1) : n;
            if (s.Length > 0)
                return s;
        }
        return "";
    }

    void Elegir(bool dePerfil)
    {
        if (!ArmarModelo(dePerfil))
            return;
        // Tu original queda guardado como clave en SU fotograma (y como "Original" en la biblioteca): nunca se toca.
        var anim = dibujo.animacion;
        if (anim != null && anim.Fotograma == fotogramaOriginal)
            anim.GuardarClaveEn(fotogramaOriginal, capaCabeza);
        vistas.Clear();
        var original = new VistaGuardada { nombre = "Original", esOriginal = true, pose = new List<DatosTrazo>() };
        foreach (var d in datosOriginales.Values)
            original.pose.Add(d);
        vistas.Add(original);
        giro = 0f;
        inclina = 0f;
        mostrandoReal = true;
        Mostrar(filaPregunta, false);
        Mostrar(filaGiro, true);
        RefrescarVistas();
        estado = Estado.Girando;
        sucio = true;
        AvanzarTutorial(2);
        dibujo.Mensaje("Gírala: Perfil / 3/4 / Frente, o pellízcala con la derecha y arrastra");
    }

    bool ArmarModelo(bool dePerfil)
    {
        // Líneas temporales con la forma de tu ORIGINAL (y los nombres de ahora): con ellas se arma el modelo.
        var temporales = new List<Trazo>();
        Trazo contorno = null;
        bool antesSilencio = Trazo.silenciar;
        Trazo.silenciar = true;
        foreach (int id in idsOriginales)
        {
            DatosTrazo d;
            var viva = dibujo.BuscarPorId(id);
            if (viva == null || !datosOriginales.TryGetValue(id, out d))
                continue;
            d.nombresNodo = new List<string>(viva.nombresNodo);
            var tmp = dibujo.CrearTrazoSuelto(d, capaCabeza);
            tmp.id = id;
            tmp.gameObject.SetActive(false);
            temporales.Add(tmp);
            if (id == idContorno)
                contorno = tmp;
        }
        Trazo.silenciar = antesSilencio;
        Trazo.huboCambio = false;
        bool ok = contorno != null && ArmarModeloCon(dePerfil, temporales, contorno);
        foreach (var t in temporales)
            Destroy(t.gameObject);
        return ok;
    }

    bool ArmarModeloCon(bool dePerfil, List<Trazo> originales, Trazo contorno)
    {
        perfil = dePerfil;
        lineas.Clear();
        if (perfil)
        {
            float sx = 0f;
            int cuantos = 0;
            foreach (var t in originales)
                if (t != contorno)
                    foreach (var p in t.curva)
                    {
                        sx += Plano(t.transform.TransformPoint(p)).x;
                        cuantos++;
                    }
            mira = cuantos > 0 && sx < 0f ? -1f : 1f;
            semiZ = semiX * 0.8f;
        }
        else
        {
            semiZ = semiX * 1.1f;
        }
        ArmarProporcion(contorno);
        CalcularSugerencias(originales, perfil, mira);
        // El contorno: el ángulo de cada uno de sus nodos (en cada vista va a la silueta, al mismo ángulo).
        angulosContorno.Clear();
        contornoOrig.Clear();
        contornoProf.Clear();
        foreach (var p in contorno.nodos)
        {
            Vector3 m = contorno.transform.TransformPoint(p);
            Vector2 q = Plano(m);
            angulosContorno.Add(Mathf.Atan2(q.y, q.x));
            contornoOrig.Add(q);
            contornoProf.Add(Vector3.Dot(m - centro, haciaTi));
        }
        foreach (var t in originales)
        {
            if (t == contorno)
                continue;
            var plano = new List<Vector2>();
            foreach (var p in t.curva)
                plano.Add(Plano(t.transform.TransformPoint(p)));
            if (plano.Count < 2)
                continue;
            var l = new Linea { idOrigen = t.id };
            Clasificar(t, plano, l);
            Llenar(l, t);
            lineas.Add(l);
            if (perfil && (l.tipo == Tipo.OjoPerfil || l.tipo == Tipo.Oreja))
            {
                var e = new Linea { idOrigen = t.id, espejo = true, tipo = l.tipo, nombrada = l.nombrada, c = l.c, x0 = l.x0, x1 = l.x1 };
                Llenar(e, t);
                lineas.Add(e);
            }
        }
        hayModelo = true;
        BorrarClones();
        return true;
    }

    // Volver a mirar qué es cada línea (después de nombrar algo), sin perder el giro.
    void AlCambiarNombres()
    {
        sucio = true;
        if (estado == Estado.Girando && hayModelo)
        {
            float g = giro, inc = inclina;
            ArmarModelo(perfil);
            giro = g;
            inclina = inc;
        }
    }

    // Qué es cada línea: por su nombre (si nombraste un nodo) o por lo que sugieren las zonas (v54).
    void Clasificar(Trazo t, List<Vector2> plano, Linea l)
    {
        l.c = Medio(plano);
        l.x0 = float.MaxValue;
        l.x1 = float.MinValue;
        foreach (var q in plano)
        {
            l.x0 = Mathf.Min(l.x0, q.x);
            l.x1 = Mathf.Max(l.x1, q.x);
        }
        int cat = CategoriaDeLinea(t);
        l.nombrada = cat >= 0;
        if (cat < 0)
            cat = CategoriaDeNombre(MejorSugerencia(t.id));
        switch (cat)
        {
            case 0: l.tipo = perfil ? Tipo.Centro : Tipo.NarizFrente; return;
            case 1: l.tipo = perfil ? Tipo.OjoPerfil : Tipo.Rasgo; return;
            case 2: l.tipo = perfil ? Tipo.Boca : Tipo.Rasgo; return;
            case 4: l.tipo = Tipo.Oreja; return;
            default: l.tipo = Tipo.Rasgo; return;
        }
    }

    // ---------- v54: QUÉ ES CADA LÍNEA: puntos por zonas (pura lógica, sin IA) ----------
    // Cada línea recibe puntos para cada parte según DÓNDE está en la cabeza (alto y de lado), su FORMA (ancha,
    // delgada, cerrada, vertical) y si tiene PAREJA en espejo (ojos, cejas, orejas). Gana la de más puntos.
    // De frente: cejas arriba de los ojos, ojos a media cabeza y a los lados, nariz al centro bajo los ojos, boca abajo
    // al centro, orejas en los extremos. De perfil: la nariz sobresale adelante, el ojo en el tercio delantero, la boca
    // adelante abajo y la oreja detrás del centro.
    class Rasgo
    {
        public int id;
        public float u, v;      // centro: u de -1 (izquierda) a 1 (derecha), v de -1 (abajo) a 1 (arriba)
        public float w, h;      // ancho y alto (fracción de la cabeza)
        public float aspecto;   // alto ÷ ancho
        public float fuera;     // qué parte sale del contorno (0 a 1)
        public bool cerrado;
    }

    readonly Dictionary<int, List<KeyValuePair<string, float>>> sugerencias = new Dictionary<int, List<KeyValuePair<string, float>>>();
    float miraAdivinada = 1f;

    Rasgo Medir(Trazo t)
    {
        var r = new Rasgo { id = t.id, cerrado = t.cerrado };
        float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
        Vector2 suma = Vector2.zero;
        int n = 0, fuera = 0;
        foreach (var p in t.curva)
        {
            Vector2 q = Plano(t.transform.TransformPoint(p));
            x0 = Mathf.Min(x0, q.x);
            x1 = Mathf.Max(x1, q.x);
            y0 = Mathf.Min(y0, q.y);
            y1 = Mathf.Max(y1, q.y);
            suma += q;
            n++;
            if (K(q) <= 0f)
                fuera++;
        }
        if (n == 0)
            return r;
        Vector2 c = suma / n;
        r.u = c.x / semiX;
        r.v = c.y / semiY;
        r.w = (x1 - x0) / (2f * semiX);
        r.h = (y1 - y0) / (2f * semiY);
        r.aspecto = (y1 - y0) / Mathf.Max(1e-5f, x1 - x0);
        r.fuera = fuera / (float)n;
        return r;
    }

    static float G(float x, float centro, float ancho)
    {
        float d = (x - centro) / ancho;
        return Mathf.Exp(-0.5f * d * d);
    }

    // Las sugerencias de todas las líneas (de la más probable a la menos).
    void CalcularSugerencias(List<Trazo> ts, bool dePerfil, float m)
    {
        sugerencias.Clear();
        var rasgos = new List<Rasgo>();
        foreach (var t in ts)
            if (t != null && t.id != idContorno && t.curva.Count >= 2)
                rasgos.Add(Medir(t));
        foreach (var r in rasgos)
            sugerencias[r.id] = Puntuar(r, rasgos, dePerfil, m);
    }

    static List<KeyValuePair<string, float>> Puntuar(Rasgo r, List<Rasgo> todos, bool dePerfil, float m)
    {
        bool pareja = false, algoDebajo = false;
        foreach (var o in todos)
        {
            if (o == r)
                continue;
            if (Mathf.Abs(r.u) > 0.12f && Mathf.Abs(r.u + o.u) < 0.25f && Mathf.Abs(r.v - o.v) < 0.15f
                && Mathf.Abs(r.w - o.w) < Mathf.Max(0.08f, 0.5f * r.w))
                pareja = true;
            float dv = r.v - o.v;
            if (Mathf.Abs(o.u - r.u) < 0.22f && dv > 0.06f && dv < 0.4f)
                algoDebajo = true;
        }
        bool delgada = r.aspecto < 0.35f;
        bool vertical = r.aspecto > 0.9f;
        var p = new List<KeyValuePair<string, float>>();
        if (!dePerfil)
        {
            float au = Mathf.Abs(r.u);
            bool izq = r.u < 0f;
            p.Add(new KeyValuePair<string, float>(izq ? "ojo izquierdo" : "ojo derecho",
                G(r.v, 0.08f, 0.18f) * G(au, 0.38f, 0.2f) * (r.aspecto > 0.25f && r.aspecto < 1.4f ? 1.2f : 0.6f)
                * (r.cerrado ? 1.2f : 1f) * (pareja ? 1.3f : 1f) * (r.w < 0.4f ? 1f : 0.5f)));
            p.Add(new KeyValuePair<string, float>(izq ? "ceja izquierda" : "ceja derecha",
                G(r.v, 0.3f, 0.15f) * G(au, 0.38f, 0.25f) * (delgada ? 1.3f : 0.5f) * (r.cerrado ? 0.5f : 1.1f)
                * (pareja ? 1.3f : 1f) * (algoDebajo ? 1.4f : 1f)));
            p.Add(new KeyValuePair<string, float>("nariz",
                G(r.v, -0.18f, 0.2f) * G(r.u, 0f, 0.15f) * (r.w < 0.25f ? 1.1f : 0.6f)));
            p.Add(new KeyValuePair<string, float>("boca",
                G(r.v, -0.5f, 0.2f) * G(r.u, 0f, 0.22f) * (r.w > 0.12f && r.aspecto < 0.8f ? 1.3f : 0.6f)));
            p.Add(new KeyValuePair<string, float>(izq ? "oreja izquierda" : "oreja derecha",
                G(au, 0.98f, 0.15f) * G(r.v, 0f, 0.3f) * (vertical ? 1.2f : 0.8f) * (r.fuera > 0.3f ? 1.5f : 1f)));
            p.Add(new KeyValuePair<string, float>("mejilla", G(au, 0.55f, 0.2f) * G(r.v, -0.28f, 0.15f) * 0.45f));
        }
        else
        {
            float s = r.u * m;   // hacia donde mira la cara = positivo
            p.Add(new KeyValuePair<string, float>("nariz", G(s, 0.95f, 0.18f) * G(r.v, -0.1f, 0.22f) * (r.fuera > 0.2f ? 1.5f : 1f)));
            p.Add(new KeyValuePair<string, float>("ojo",
                G(s, 0.55f, 0.2f) * G(r.v, 0.08f, 0.18f) * (r.cerrado ? 1.2f : 1f) * (r.w < 0.3f ? 1f : 0.5f)));
            p.Add(new KeyValuePair<string, float>("ceja",
                G(s, 0.55f, 0.25f) * G(r.v, 0.3f, 0.15f) * (delgada ? 1.3f : 0.5f) * (algoDebajo ? 1.3f : 1f)));
            p.Add(new KeyValuePair<string, float>("boca", G(s, 0.8f, 0.2f) * G(r.v, -0.45f, 0.18f) * (r.fuera > 0.1f ? 1.2f : 1f)));
            p.Add(new KeyValuePair<string, float>("oreja", G(s, -0.15f, 0.25f) * G(r.v, 0f, 0.3f) * (vertical ? 1.2f : 0.8f)));
            p.Add(new KeyValuePair<string, float>("mejilla", G(s, 0.45f, 0.2f) * G(r.v, -0.25f, 0.15f) * 0.45f));
        }
        p.Sort((a, b) => b.Value.CompareTo(a.Value));
        return p;
    }

    string MejorSugerencia(int id)
    {
        List<KeyValuePair<string, float>> l;
        return sugerencias.TryGetValue(id, out l) && l.Count > 0 ? l[0].Key : "";
    }

    // Los nodos de la línea sobre el huevo (y la punta de sus tiradores).
    void Llenar(Linea l, Trazo t)
    {
        int n = t.nodos.Count;
        t.AsegurarAsas();
        for (int j = 0; j < n; j++)
        {
            float f = j / (float)Mathf.Max(1, n - 1);
            Vector3 nodoLocal = t.nodos[j];
            Vector3 m = t.transform.TransformPoint(nodoLocal);
            l.nodos.Add(Espejar(l, Mapear(l, Plano(m), f)));
            l.prof.Add(Vector3.Dot(m - centro, haciaTi));
            bool man = j < t.asaManual.Count && t.asaManual[j];
            l.manual.Add(man);
            Vector3 e = j < t.asaEntrada.Count ? t.asaEntrada[j] : Vector3.zero;
            Vector3 s = j < t.asaSalida.Count ? t.asaSalida[j] : Vector3.zero;
            l.asaE.Add(Espejar(l, Mapear(l, Plano(t.transform.TransformPoint(nodoLocal + e)), f)));
            l.asaS.Add(Espejar(l, Mapear(l, Plano(t.transform.TransformPoint(nodoLocal + s)), f)));
        }
    }

    static Vector3 Espejar(Linea l, Vector3 p)
    {
        return l.espejo ? new Vector3(p.x, p.y, -p.z) : p;
    }

    // Un punto de tu dibujo sobre el huevo, según qué es su línea. "f" = dónde va en la línea (0 a 1).
    // v53: sin girar, cada punto queda EXACTAMENTE donde lo dibujaste (x e y no cambian; solo se inventa la profundidad).
    Vector3 Mapear(Linea l, Vector2 q, float f)
    {
        float k = K(q);
        float u = q.x / semiX, v = q.y / semiY;
        if (!perfil)
        {
            if (l.tipo == Tipo.Oreja)
            {
                // La oreja: un disco al lado, inclinado hacia atrás (de perfil se ve como oreja, no como una raya).
                float lado = l.c.x > 0f ? 1f : -1f;
                float dx = q.x - l.c.x;
                return new Vector3(q.x, q.y, -0.1f * semiZ - dx * lado);
            }
            float z = k > 0f ? semiZ * Mathf.Sqrt(k) : -0.05f * semiZ;
            if (l.tipo == Tipo.NarizFrente)
                z += 0.25f * semiZ * Mathf.Clamp01(1f - Mathf.Abs(q.x - l.c.x) / Mathf.Max(0.005f, (l.x1 - l.x0) * 0.5f + 0.005f));
            else if (Mathf.Abs(u) < 0.22f && v > -0.45f && v < 0.15f)
                z += 0.22f * semiZ * (1f - Mathf.Abs(u) / 0.22f);
            return new Vector3(q.x, q.y, z);
        }
        switch (l.tipo)
        {
            case Tipo.Centro:
                return new Vector3(q.x, q.y, 0f);
            case Tipo.Boca:
                return new Vector3(q.x, q.y, (f - 0.5f) * 0.7f * semiZ);
            case Tipo.Oreja:
            {
                float dx = q.x - l.c.x;
                return new Vector3(q.x, q.y, semiZ * 0.9f - dx * mira);
            }
            default:
            {
                float z = semiZ * Mathf.Clamp(1f - u * mira, 0.2f, 1f) * Mathf.Sqrt(Mathf.Max(1f - v * v, 0.05f));
                z += (q.x - (l.x0 + l.x1) * 0.5f) * mira * 0.6f;
                return new Vector3(q.x, q.y, z);
            }
        }
    }

    Vector2 Plano(Vector3 mundo)
    {
        Vector3 d = mundo - centro;
        return new Vector2(Vector3.Dot(d, derecha), Vector3.Dot(d, arriba)) - centro2D;
    }

    void Caja(Trazo t, out Vector2 mn, out Vector2 mx)
    {
        mn = new Vector2(float.MaxValue, float.MaxValue);
        mx = new Vector2(float.MinValue, float.MinValue);
        foreach (var p in t.curva)
        {
            Vector3 d = t.transform.TransformPoint(p) - centro;
            var q = new Vector2(Vector3.Dot(d, derecha), Vector3.Dot(d, arriba));
            mn = Vector2.Min(mn, q);
            mx = Vector2.Max(mx, q);
        }
    }

    static Vector2 Medio(List<Vector2> pts)
    {
        Vector2 c = Vector2.zero;
        foreach (var p in pts)
            c += p;
        return c / Mathf.Max(1, pts.Count);
    }

    float K(Vector2 q)
    {
        float u = q.x / semiX, v = q.y / semiY;
        return 1f - u * u - v * v;
    }

    void ArmarProporcion(Trazo contorno)
    {
        var suma = new float[proporcion.Length];
        var cuenta = new int[proporcion.Length];
        foreach (var p in contorno.curva)
        {
            Vector2 q = Plano(contorno.transform.TransformPoint(p));
            float r = q.magnitude;
            if (r < 1e-5f)
                continue;
            float a = Mathf.Atan2(q.y, q.x);
            int i = Mathf.Clamp(Mathf.FloorToInt((a + Mathf.PI) / (2f * Mathf.PI) * proporcion.Length), 0, proporcion.Length - 1);
            suma[i] += r / RadioElipse(a, semiX, semiY);
            cuenta[i]++;
        }
        for (int i = 0; i < proporcion.Length; i++)
        {
            if (cuenta[i] > 0)
            {
                proporcion[i] = suma[i] / cuenta[i];
                continue;
            }
            float v = 1f;
            for (int d = 1; d < proporcion.Length; d++)
            {
                int a = (i + d) % proporcion.Length, b = (i - d + proporcion.Length) % proporcion.Length;
                if (cuenta[a] > 0) { v = suma[a] / cuenta[a]; break; }
                if (cuenta[b] > 0) { v = suma[b] / cuenta[b]; break; }
            }
            proporcion[i] = v;
        }
    }

    static float RadioElipse(float angulo, float a, float b)
    {
        float c = Mathf.Cos(angulo) / a, s = Mathf.Sin(angulo) / b;
        return 1f / Mathf.Sqrt(Mathf.Max(1e-8f, c * c + s * s));
    }

    float Proporcion(float angulo)
    {
        float f = (angulo + Mathf.PI) / (2f * Mathf.PI) * proporcion.Length;
        int i0 = ((Mathf.FloorToInt(f) % proporcion.Length) + proporcion.Length) % proporcion.Length;
        int i1 = (i0 + 1) % proporcion.Length;
        return Mathf.Lerp(proporcion[i0], proporcion[i1], f - Mathf.Floor(f));
    }

    // ---------- Cada cuadro ----------

    void LateUpdate()
    {
        if (estado == Estado.Nada || control == null || dibujo == null)
            return;
        bool palabra = RevisarPalabras();
        if (estado == Estado.Girando)
        {
            if (!palabra)
                RevisarAgarre();
            // Si cambias de fotograma, se ven tus líneas de verdad (las de ese fotograma).
            var anim = dibujo.animacion;
            int f = anim != null ? anim.Fotograma : 0;
            if (f != fotogramaVisto)
            {
                fotogramaVisto = f;
                if (!agarrada)
                    mostrandoReal = true;
                sucio = true;
            }
            bool real = mostrandoReal || Peso() < 0.001f;
            if (real)
            {
                Desesconder();
                OcultarClones();
            }
            else
            {
                EsconderOriginales();
                if (sucio)
                    ActualizarClones();
            }
        }
        // Las etiquetas siguen a tus líneas aunque las muevas (cada cuarto de segundo).
        if (Time.time >= proximaEtiqueta)
        {
            proximaEtiqueta = Time.time + 0.25f;
            sucio = true;
        }
        if (sucio)
        {
            sucio = false;
            PonerEtiquetas();
        }
    }

    float proximaEtiqueta;

    void RevisarAgarre()
    {
        var der = control.Der;
        if (agarrada)
        {
            if (!der.valida || !der.pellizco)
            {
                agarrada = false;
                control.Ocupado = false;
                return;
            }
            Vector3 d = der.PuntoPellizco - agarreInicio;
            giro = giroInicio - Vector3.Dot(d, derecha) * 300f;
            inclina = Mathf.Clamp(inclinaInicio + Vector3.Dot(d, arriba) * 200f, -40f, 40f);
            mostrandoReal = false;
            sucio = true;
            return;
        }
        if (der.valida && der.empezoPellizco && !BotonTocable.DedoCerca && !control.Ocupado
            && Vector3.Distance(der.PuntoPellizco, centro) < Mathf.Max(semiX, semiY) + 0.06f)
        {
            agarrada = true;
            agarreInicio = der.PuntoPellizco;
            giroInicio = giro;
            inclinaInicio = inclina;
            control.Ocupado = true;
        }
    }

    Quaternion Giro => Quaternion.Euler(inclina, giro, 0f);

    // Qué tan lejos estás de la vista que dibujaste (0 = esa misma: tus líneas tal cual; 1 = ya es otra vista).
    float Peso()
    {
        return Mathf.Clamp01(Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(giro, 0f)) / 40f, Mathf.Abs(inclina) / 25f));
    }

    // La silueta del huevo girado, con la forma de tu contorno (en el plano de tu dibujo).
    void Silueta(Quaternion q)
    {
        silueta.Clear();
        Matrix4x4 m = Matrix4x4.Rotate(q) * Matrix4x4.Scale(new Vector3(semiX, semiY, semiZ));
        Vector3 w = m.transpose.MultiplyVector(Vector3.forward).normalized;
        Vector3 e1 = Vector3.Cross(w, Mathf.Abs(w.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 e2 = Vector3.Cross(w, e1).normalized;
        const int lados = 72;
        for (int i = 0; i <= lados; i++)
        {
            float a = i / (float)lados * Mathf.PI * 2f;
            Vector3 p = m.MultiplyVector(Mathf.Cos(a) * e1 + Mathf.Sin(a) * e2);
            Vector2 p2 = new Vector2(p.x, p.y);
            p2 *= Proporcion(Mathf.Atan2(p2.y, p2.x));
            silueta.Add(new Vector3(p2.x, p2.y, 0f));
        }
    }

    // El punto de la silueta en ese ángulo.
    Vector2 EnSilueta(float angulo)
    {
        Vector2 dir = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
        float mejor = -2f;
        Vector2 res = dir * semiX;
        foreach (var p in silueta)
        {
            Vector2 p2 = new Vector2(p.x, p.y);
            float r = p2.magnitude;
            if (r < 1e-6f)
                continue;
            float d = Vector2.Dot(p2 / r, dir);
            if (d > mejor)
            {
                mejor = d;
                res = dir * r;
            }
        }
        return res;
    }

    bool Visible(Quaternion q, Vector3 p)
    {
        Vector3 n = q * new Vector3(p.x / (semiX * semiX), p.y / (semiY * semiY), p.z / (semiZ * semiZ));
        return n.sqrMagnitude < 1e-12f || n.normalized.z > -0.12f;
    }

    Vector3 Mundo(Vector3 p, float prof)
    {
        Vector2 q = new Vector2(p.x, p.y) + centro2D;
        return centro + derecha * q.x + arriba * q.y + haciaTi * prof;
    }

    static Vector3 Plano3(Vector3 p)
    {
        return new Vector3(p.x, p.y, 0f);
    }

    // La forma del contorno en este giro: cada nodo a la silueta (en su mismo ángulo), mezclado con tu contorno.
    DatosTrazo PoseContorno(Trazo contorno, float w)
    {
        var d = contorno.CrearDatos();
        if (angulosContorno.Count != d.nodos.Count || contornoOrig.Count != d.nodos.Count)
            return null;
        var raizD = dibujo.transform;
        for (int j = 0; j < d.nodos.Count; j++)
        {
            Vector2 p = Vector2.Lerp(contornoOrig[j], EnSilueta(angulosContorno[j]), w);
            d.nodos[j] = raizD.InverseTransformPoint(Mundo(new Vector3(p.x, p.y, 0f), contornoProf[j] * (1f - w)));
        }
        if (w > 0.001f)
            for (int j = 0; j < d.asaManual.Count; j++)
                d.asaManual[j] = false;   // sus tiradores se recalculan solos (curva suave)
        return d;
    }

    // La forma de una línea en este giro, con LOS MISMOS nodos. Los que quedan atrás se pegan al vecino que sí se ve.
    DatosTrazo PoseLinea(Linea l, Trazo baseT, Quaternion q, float w)
    {
        DatosTrazo d = baseT.CrearDatos();
        int n = l.nodos.Count;
        if (d.nodos.Count != n)
            return null;   // cambiaste los nodos de la línea: esta ya no se gira
        var raizD = dibujo.transform;
        var vis = new bool[n];
        var pos = new Vector3[n];
        int cuantosVis = 0;
        for (int j = 0; j < n; j++)
        {
            vis[j] = Visible(q, l.nodos[j]);
            pos[j] = Plano3(q * l.nodos[j]);
            if (vis[j])
                cuantosVis++;
        }
        for (int j = 0; j < n; j++)
        {
            if (vis[j] || cuantosVis == 0)
                continue;
            for (int dd = 1; dd < n; dd++)
            {
                if (j - dd >= 0 && vis[j - dd]) { pos[j] = pos[j - dd]; break; }
                if (j + dd < n && vis[j + dd]) { pos[j] = pos[j + dd]; break; }
            }
        }
        float conservar = l.espejo ? 0f : 1f - w;
        for (int j = 0; j < n; j++)
        {
            d.nodos[j] = raizD.InverseTransformPoint(Mundo(pos[j], l.prof[j] * conservar));
            if (j < d.asaManual.Count && l.manual[j] && vis[j])
            {
                d.asaEntrada[j] = raizD.InverseTransformPoint(Mundo(Plano3(q * l.asaE[j]), l.prof[j] * conservar)) - d.nodos[j];
                d.asaSalida[j] = raizD.InverseTransformPoint(Mundo(Plano3(q * l.asaS[j]), l.prof[j] * conservar)) - d.nodos[j];
            }
            else if (j < d.asaManual.Count)
            {
                d.asaManual[j] = false;
            }
        }
        d.oculto = cuantosVis == 0;
        return d;
    }

    // La vista previa: líneas sueltas IGUALES a las tuyas, en la forma de este giro.
    void ActualizarClones()
    {
        float w = Peso();
        Quaternion q = Giro;
        Silueta(q);
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        Trazo contorno = dibujo.BuscarPorId(idContorno);
        if (contorno != null)
        {
            var dc = PoseContorno(contorno, w);
            if (dc != null)
            {
                if (cloneContorno == null)
                    cloneContorno = dibujo.CrearTrazoSuelto(dc, capaCabeza);
                else
                    cloneContorno.AplicarPose(dc, null, 0f);
                if (!cloneContorno.gameObject.activeSelf)
                    cloneContorno.gameObject.SetActive(true);
            }
        }
        for (int i = 0; i < lineas.Count; i++)
        {
            while (clones.Count <= i)
                clones.Add(null);
            var l = lineas[i];
            Trazo origen = dibujo.BuscarPorId(l.idOrigen);
            DatosTrazo d = origen != null ? PoseLinea(l, origen, q, w) : null;
            if (d == null)
            {
                if (clones[i] != null)
                    clones[i].gameObject.SetActive(false);
                continue;
            }
            if (clones[i] == null)
                clones[i] = dibujo.CrearTrazoSuelto(d, capaCabeza);
            else
                clones[i].AplicarPose(d, null, 0f);
            if (!clones[i].gameObject.activeSelf)
                clones[i].gameObject.SetActive(true);
        }
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
    }

    // ---------- Usar una vista: tus líneas (los MISMOS nodos) toman esa forma en OTRO fotograma ----------

    // Dónde va la vista: donde estás, salvo en el fotograma de tu original (ese nunca se toca): 6 más adelante.
    int Destino(Animacion anim)
    {
        int f = anim.Fotograma;
        if (f != fotogramaOriginal)
            return f;
        f = fotogramaOriginal + 6;
        if (f > Animacion.TotalFotogramas - 1)
            f = Mathf.Max(0, fotogramaOriginal - 6);
        return f;
    }

    void Usar()
    {
        if (estado != Estado.Girando || !hayModelo)
            return;
        float w = Peso();
        if (w < 0.001f)
        {
            dibujo.Mensaje("Esa es la vista que dibujaste: gírala primero (tu original no se toca)");
            return;
        }
        var anim = dibujo.animacion;
        if (dibujo.capaActual != capaCabeza)
            dibujo.SeleccionarCapa(capaCabeza);
        Desesconder();
        dibujo.GuardarParaDeshacer();
        if (anim != null)
        {
            int destino = Destino(anim);
            if (destino != anim.Fotograma)
                anim.IrA(destino);
        }
        Quaternion q = Giro;
        Silueta(q);
        var nuevos = new List<int>();
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        Trazo contorno = dibujo.BuscarPorId(idContorno);
        if (contorno != null)
        {
            var dc = PoseContorno(contorno, w);
            if (dc != null)
                contorno.AplicarPose(dc, null, 0f);
        }
        foreach (var l in lineas)
        {
            Trazo origen = dibujo.BuscarPorId(l.idOrigen);
            if (origen == null)
                continue;
            Trazo t = origen;
            if (l.espejo)
            {
                int idE;
                t = espejos.TryGetValue(l.idOrigen, out idE) ? dibujo.BuscarPorId(idE) : null;
            }
            DatosTrazo d = PoseLinea(l, t != null ? t : origen, q, w);
            if (d == null)
                continue;
            if (t == null)
            {
                d.id = 0;
                // La copia del otro lado no lleva los nombres de los nodos (no confunde a las etiquetas).
                d.nombresNodo = new List<string>();
                t = dibujo.AgregarTrazo(d);
                espejos[l.idOrigen] = t.id;
                nuevos.Add(t.id);
            }
            else
            {
                t.AplicarPose(d, null, 0f);
            }
        }
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
        GuardarClave(nuevos);
        mostrandoReal = true;
        OcultarClones();
        int f = anim != null ? anim.Fotograma + 1 : 1;
        dibujo.Mensaje("Vista puesta en el fotograma " + f + " (tu original sigue en el " + (fotogramaOriginal + 1) + "). Retócala y toca Guardar vista");
        AvanzarTutorial(4);
        sucio = true;
    }

    // La clave en este fotograma (así entre vistas hay morph). Las copias nuevas no existían en las otras claves: allá ocultas.
    void GuardarClave(List<int> nuevos)
    {
        var anim = dibujo.animacion;
        if (anim != null)
        {
            anim.GuardarClaveEn(anim.Fotograma, capaCabeza);
            if (nuevos.Count > 0)
                foreach (var c in anim.claves)
                {
                    if (c == null || c.capa != capaCabeza || c.fotograma == anim.Fotograma)
                        continue;
                    foreach (var p in c.trazos)
                        if (p != null && nuevos.Contains(p.id))
                            p.oculto = true;
                    c.cache = null;
                }
        }
        dibujo.HayCambios = true;
        dibujo.Avisar();
    }

    // ---------- La biblioteca de vistas ----------

    string NombreAngulo()
    {
        float rel = giro - (perfil ? (mira > 0f ? -90f : 90f) : 0f);
        rel = Mathf.Repeat(rel + 180f, 360f) - 180f;
        return Mathf.Abs(rel) < 15f ? "Frente" : Mathf.Abs(rel) > 70f ? (rel > 0f ? "Perfil der" : "Perfil izq") : (rel > 0f ? "3/4 der" : "3/4 izq");
    }

    // Guarda TU versión de este fotograma (con tus retoques). Si aún no la usaste, guarda solo el ángulo.
    void GuardarVista()
    {
        var anim = dibujo.animacion;
        var v = new VistaGuardada { giro = giro, inclina = inclina };
        bool tuya = anim != null && anim.Fotograma != fotogramaOriginal && anim.EsClave(anim.Fotograma, capaCabeza) && mostrandoReal;
        if (tuya)
        {
            v.pose = new List<DatosTrazo>();
            foreach (var t in LineasCabeza())
                v.pose.Add(t.CrearDatos());
        }
        int propias = vistas.Count - 1;
        if (propias >= 6)
            vistas.RemoveAt(1);
        v.nombre = vistas.Count + " · " + NombreAngulo();   // en español: el botón lo muestra en tu idioma
        vistas.Add(v);
        RefrescarVistas();
        dibujo.Mensaje(tuya ? "Tu versión quedó en la biblioteca: tócala en cualquier fotograma"
                            : "Ángulo guardado en la biblioteca (usa la vista y retócala para guardar TU versión)");
    }

    void UsarVista(int i)
    {
        if (i < 0 || i >= vistas.Count)
            return;
        var v = vistas[i];
        giro = v.giro;
        inclina = v.inclina;
        if (v.pose == null)
        {
            mostrandoReal = false;
            sucio = true;
            Usar();
            return;
        }
        var anim = dibujo.animacion;
        if (dibujo.capaActual != capaCabeza)
            dibujo.SeleccionarCapa(capaCabeza);
        if (v.esOriginal && anim != null && anim.Fotograma == fotogramaOriginal)
        {
            mostrandoReal = true;
            sucio = true;
            dibujo.Mensaje("Aquí está tu original (este fotograma nunca cambia)");
            return;
        }
        Desesconder();
        dibujo.GuardarParaDeshacer();
        if (anim != null && !v.esOriginal)
        {
            int destino = Destino(anim);
            if (destino != anim.Fotograma)
                anim.IrA(destino);
        }
        var porId = new Dictionary<int, DatosTrazo>();
        foreach (var d in v.pose)
            if (d != null)
                porId[d.id] = d;
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        foreach (var t in LineasCabeza())
        {
            DatosTrazo d;
            if (porId.TryGetValue(t.id, out d))
            {
                if (d.nodos.Count == t.nodos.Count)
                    t.AplicarPose(d, null, 0f);
            }
            else
            {
                // Esta línea no estaba en esa vista (la copia del otro ojo en tu original, por ejemplo): oculta.
                var o = t.CrearDatos();
                o.oculto = true;
                t.AplicarPose(o, null, 0f);
            }
        }
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
        GuardarClave(new List<int>());
        mostrandoReal = true;
        sucio = true;
        int f = anim != null ? anim.Fotograma + 1 : 1;
        dibujo.Mensaje(Idioma.T(v.esOriginal ? "Original" : "Tu vista") + " · " + Idioma.T("fotograma") + " " + f);
    }

    void RefrescarVistas()
    {
        foreach (var g in filaVistas)
            if (g != null)
                Destroy(g);
        filaVistas.Clear();
        if (panel == null)
            return;
        for (int i = 0; i < vistas.Count; i++)
        {
            int k = i;
            string nombre = vistas[i].esOriginal ? "Original" : vistas[i].nombre + (vistas[i].pose != null ? " *" : "");
            Boton(filaVistas, nombre, new Vector3(-0.171f + i * 0.057f, -0.052f, 0f), 0.054f, vistas[i].esOriginal, () => UsarVista(k));
        }
    }

    // ---------- Etiquetas con botones (visto / X / cuadrito) ----------

    // v54: una palabra guardada (en español) en el idioma de la app.
    static string Mostrar(string es)
    {
        if (string.IsNullOrEmpty(es) || !Idioma.Ingles)
            return es;
        for (int i = 0; i < PuntosEs.Length; i++)
            if (es == PuntosEs[i])
                return PuntosEn[i];
        for (int i = 0; i < Cortas.Length; i++)
            if (es == Cortas[i])
                return CortasEn[i];
        if (es == "contorno")
            return "outline";
        return Idioma.T(es);
    }

    // Lo que la app cree que es una línea: (texto, palabra para confirmar, ¿ya es seguro?).
    void QueEs(Trazo t, out string texto, out string palabra, out bool segura)
    {
        string nombre = PrimerNombre(t);
        if (nombre.Length > 0)
        {
            texto = Mostrar(nombre);
            palabra = nombre;
            segura = true;
            return;
        }
        palabra = t.id == idContorno ? "contorno" : MejorSugerencia(t.id);
        texto = palabra.Length > 0 ? Mostrar(palabra) + "?" : "?";
        segura = false;
    }

    // Dónde se ve una línea ahora (tu línea de verdad o su vista previa): el centro de sus nodos.
    static Vector3 CentroDe(Trazo t)
    {
        Vector3 s = Vector3.zero;
        foreach (var p in t.nodos)
            s += t.transform.TransformPoint(p);
        return s / Mathf.Max(1, t.nodos.Count);
    }

    // La línea que se ve en pantalla para una línea tuya (la vista previa si está girada).
    Trazo Mostrada(Trazo original)
    {
        if (estado != Estado.Girando || mostrandoReal || Peso() < 0.001f)
            return original;
        if (original.id == idContorno)
            return cloneContorno != null && cloneContorno.gameObject.activeSelf ? cloneContorno : original;
        for (int i = 0; i < lineas.Count && i < clones.Count; i++)
            if (!lineas[i].espejo && lineas[i].idOrigen == original.id && clones[i] != null)
                return clones[i].gameObject.activeSelf ? clones[i] : null;
        return original;
    }

    void PonerEtiquetas()
    {
        int k = 0;
        // Con el sello en la mano, las etiquetas se esconden (así el índice no toca sus botones sin querer).
        if (verEtiquetas && raiz != null && modoPalabra != ModoPalabra.Sello)
        {
            foreach (var t in Originales())
            {
                if (sinEtiqueta.Contains(t.id))
                    continue;
                var visto = Mostrada(t);
                if (visto == null || visto.oculto)
                    continue;
                string texto, palabra;
                bool segura;
                QueEs(t, out texto, out palabra, out segura);
                Vector3 pos = t.id == idContorno
                    ? Mundo(new Vector3(0f, semiY * 1.08f, 0f), 0f)
                    : CentroDe(visto) + arriba * 0.016f;
                PonerChip(k++, t.id, texto, palabra, segura, pos + haciaTi * 0.012f);
            }
        }
        for (int i = k; i < chips.Count; i++)
            if (chips[i].t.gameObject.activeSelf)
                chips[i].t.gameObject.SetActive(false);
        if (opciones != null && opciones.gameObject.activeSelf && (k == 0 || !verEtiquetas))
            opciones.gameObject.SetActive(false);
        RefrescarVocabulario();
    }

    void PonerChip(int i, int id, string texto, string palabra, bool segura, Vector3 pos)
    {
        while (chips.Count <= i)
            chips.Add(CrearChip(chips.Count));
        var c = chips[i];
        c.id = id;
        c.palabra = palabra;
        c.t.gameObject.SetActive(true);
        c.t.SetPositionAndRotation(pos, Quaternion.LookRotation(-haciaTi, arriba));
        c.texto.text = texto;
        // Lo que adivinó: clarito. Lo confirmado (o nombrado por ti): firme.
        c.texto.color = segura ? new Color(0.1f, 0.35f, 0.75f, 1f) : new Color(0.1f, 0.35f, 0.75f, 0.55f);
    }

    Chip CrearChip(int k)
    {
        var c = new Chip();
        c.t = new GameObject("Etiqueta" + k).transform;
        c.t.SetParent(raiz, false);
        c.texto = PanelTutoriales.Texto(c.t, "", new Vector3(0f, 0.004f, 0f), new Vector2(0.07f, 0.011f), 0.09f, null);
        c.texto.fontStyle = FontStyles.Bold;
        c.texto.outlineWidth = 0.2f;
        c.texto.outlineColor = new Color32(255, 255, 255, 200);
        c.botones = new GameObject("Botones");
        c.botones.transform.SetParent(c.t, false);
        // Visto verde (confirmar)
        var si = BotonChip(c.botones.transform, new Vector3(-0.013f, -0.008f, 0f), () => ConfirmarChip(c));
        Barra(c.botones.transform, si + new Vector3(-0.0028f, 0.0002f, -0.0046f), si + new Vector3(-0.0008f, -0.0018f, -0.0046f), 0.0012f, matSi);
        Barra(c.botones.transform, si + new Vector3(-0.0008f, -0.0018f, -0.0046f), si + new Vector3(0.0028f, 0.0024f, -0.0046f), 0.0012f, matSi);
        // X roja (quitar)
        var no = BotonChip(c.botones.transform, new Vector3(0f, -0.008f, 0f), () => QuitarChip(c));
        Barra(c.botones.transform, no + new Vector3(-0.0024f, -0.0024f, -0.0046f), no + new Vector3(0.0024f, 0.0024f, -0.0046f), 0.0012f, matNo);
        Barra(c.botones.transform, no + new Vector3(-0.0024f, 0.0024f, -0.0046f), no + new Vector3(0.0024f, -0.0024f, -0.0046f), 0.0012f, matNo);
        // Cuadrito azul (cambiar el nombre)
        var cam = BotonChip(c.botones.transform, new Vector3(0.013f, -0.008f, 0f), () => CambiarChip(c));
        Barra(c.botones.transform, cam + new Vector3(-0.0024f, 0.0024f, -0.0046f), cam + new Vector3(0.0024f, 0.0024f, -0.0046f), 0.001f, matCambiar);
        Barra(c.botones.transform, cam + new Vector3(-0.0024f, -0.0024f, -0.0046f), cam + new Vector3(0.0024f, -0.0024f, -0.0046f), 0.001f, matCambiar);
        Barra(c.botones.transform, cam + new Vector3(-0.0024f, -0.0024f, -0.0046f), cam + new Vector3(-0.0024f, 0.0024f, -0.0046f), 0.001f, matCambiar);
        Barra(c.botones.transform, cam + new Vector3(0.0024f, -0.0024f, -0.0046f), cam + new Vector3(0.0024f, 0.0024f, -0.0046f), 0.001f, matCambiar);
        return c;
    }

    Vector3 BotonChip(Transform padre, Vector3 pos, UnityEngine.Events.UnityAction accion)
    {
        var b = PanelTutoriales.Boton(padre, "", pos, new Vector2(0.0095f, 0.0095f), normal, marcado);
        b.pausa = 0.4f;
        b.alTocar.AddListener(accion);
        return pos;
    }

    static void Barra(Transform padre, Vector3 a, Vector3 b, float grosor, Material m)
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cubo.GetComponent<Collider>());
        cubo.name = "Icono";
        cubo.transform.SetParent(padre, false);
        Vector3 d = b - a;
        cubo.transform.localPosition = (a + b) * 0.5f;
        cubo.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        cubo.transform.localScale = new Vector3(d.magnitude + grosor * 0.5f, grosor, 0.001f);
        var r = cubo.GetComponent<Renderer>();
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    void ConfirmarChip(Chip c)
    {
        var t = dibujo.BuscarPorId(c.id);
        if (t == null)
            return;
        if (PrimerNombre(t).Length == 0 && c.palabra.Length > 0)
            NombrarLinea(c.id, c.palabra);
        else
            dibujo.Mensaje("Ya está confirmado: " + Mostrar(PrimerNombre(t)));
    }

    void QuitarChip(Chip c)
    {
        var t = dibujo.BuscarPorId(c.id);
        if (t == null)
            return;
        if (PrimerNombre(t).Length > 0)
        {
            for (int j = 0; j < t.nodos.Count; j++)
                t.NombrarNodo(j, null);
            dibujo.HayCambios = true;
            dibujo.Mensaje("Nombre quitado (la app vuelve a adivinar)");
        }
        else
        {
            sinEtiqueta.Add(c.id);
            dibujo.Mensaje("Etiqueta quitada");
        }
        AlCambiarNombres();
    }

    // ---------- v54: el cuadrito = las 3 palabras más probables (un toque), el teclado o la voz ----------
    Transform opciones;
    readonly List<BotonTocable> botonesOpcion = new List<BotonTocable>();
    readonly List<string> palabrasOpcion = new List<string>();
    int idOpciones;

    void CambiarChip(Chip c)
    {
        var t = dibujo.BuscarPorId(c.id);
        if (t == null)
            return;
        pendiente = c.id;
        idOpciones = c.id;
        if (opciones == null)
        {
            opciones = new GameObject("Opciones").transform;
            opciones.SetParent(raiz, false);
            for (int k = 0; k < 4; k++)
            {
                int kk = k;
                var b = PanelTutoriales.Boton(opciones, "", new Vector3(-0.054f + k * 0.036f, 0f, 0f), new Vector2(0.034f, 0.012f), normal, marcado);
                b.pausa = 0.4f;
                b.alTocar.AddListener(() => ElegirOpcion(kk));
                botonesOpcion.Add(b);
            }
        }
        palabrasOpcion.Clear();
        List<KeyValuePair<string, float>> l;
        if (c.id == idContorno)
            palabrasOpcion.Add("contorno");
        else if (sugerencias.TryGetValue(c.id, out l))
            for (int k = 0; k < l.Count && palabrasOpcion.Count < 3; k++)
                palabrasOpcion.Add(l[k].Key);
        for (int k = 0; k < botonesOpcion.Count; k++)
        {
            bool teclado = k == botonesOpcion.Count - 1;
            bool ver = teclado || k < palabrasOpcion.Count;
            botonesOpcion[k].gameObject.SetActive(ver);
            botonesOpcion[k].etiqueta.gameObject.SetActive(ver);
            if (ver)
                botonesOpcion[k].PonerTexto(teclado ? Idioma.T("Teclado") : Mostrar(palabrasOpcion[k]));
        }
        opciones.SetPositionAndRotation(c.t.position - arriba * 0.022f + haciaTi * 0.004f, c.t.rotation);
        opciones.gameObject.SetActive(true);
        dibujo.Mensaje("Toca una opción, abre el teclado o DILO con la voz (\"boca\", \"punta de nariz\"...)");
    }

    void ElegirOpcion(int k)
    {
        int id = idOpciones;
        opciones.gameObject.SetActive(false);
        if (k == botonesOpcion.Count - 1)
        {
            var t = dibujo.BuscarPorId(id);
            TecladoVR.Abrir("Nombre de esta parte (o dilo)", t != null ? PrimerNombre(t) : "", nombre =>
            {
                if (pendiente == id)
                    pendiente = 0;
                if (!string.IsNullOrEmpty(nombre))
                    NombrarLinea(id, nombre.ToLowerInvariant());
            });
            return;
        }
        if (pendiente == id)
            pendiente = 0;
        if (k < palabrasOpcion.Count)
            NombrarLinea(id, palabrasOpcion[k]);
    }

    // Nombra una línea entera: su nodo del medio lleva el nombre (los otros nombres se quitan).
    void NombrarLinea(int id, string nombre)
    {
        var t = dibujo.BuscarPorId(id);
        if (t == null || t.nodos.Count == 0)
            return;
        for (int j = 0; j < t.nodos.Count; j++)
            t.NombrarNodo(j, null);
        t.NombrarNodo(t.nodos.Count / 2, nombre);
        sinEtiqueta.Remove(id);
        if (opciones != null)
            opciones.gameObject.SetActive(false);
        dibujo.HayCambios = true;
        dibujo.Mensaje(Idioma.T("Es: ") + Mostrar(nombre));
        AlCambiarNombres();
    }

    // v54: "mirar y decir": la línea que miras (con la mirada) toma ese nombre entero.
    public static bool NombrarMirada(string nombre)
    {
        if (!Activa || Mirada.Objetivo == null || string.IsNullOrEmpty(nombre))
            return false;
        int id = Mirada.Objetivo.id;
        foreach (var kv in instancia.espejos)
            if (kv.Value == id)
                id = kv.Key;
        if (!instancia.idsOriginales.Contains(id))
            return false;
        instancia.NombrarLinea(id, nombre);
        return true;
    }

    void AlternarEtiquetas()
    {
        verEtiquetas = !verEtiquetas;
        if (vocabulario != null)
            vocabulario.gameObject.SetActive(verEtiquetas);
        sucio = true;
        dibujo.Mensaje(verEtiquetas ? "Etiquetas: Sí" : "Etiquetas: No");
    }

    // ---------- El vocabulario (a la derecha) ----------
    // v54: las palabras que ya usaste se GASTAN (desaparecen de la lista; vuelven si quitas esa etiqueta). De frente
    // vienen con izquierda/derecha (a TU izquierda); de perfil, solas ("ojo", "oreja"...).
    // Tres maneras de usar una palabra:
    //  1. EL SELLO: TÓCALA con el índice (sin gesto en la izquierda): se queda pegada a tu dedo. Toca los NODOS (o una
    //     LÍNEA lejos de sus nodos = la línea entera) que son eso: se marcan en verde. Haz un PELLIZCO con la izquierda y
    //     suéltalo = confirmar. Tocar la palabra otra vez = cancelar.
    //  2. Con la izquierda en NODOS (medio + pulgar): toca la palabra, llévala pegada al índice al nodo y SUELTA la izquierda.
    //  3. Pellízcala con la derecha, llévala y suelta el pellizco sobre el nodo.
    static readonly string[] PalabrasFrente =
    {
        "ojo izquierdo", "ojo derecho", "ceja izquierda", "ceja derecha", "oreja izquierda", "oreja derecha", "nariz", "boca",
        "punta de nariz", "puente de la nariz", "entrecejo", "párpado", "labio superior", "labio inferior",
        "comisura izquierda", "comisura derecha", "barbilla", "mandíbula", "pómulo", "mejilla", "frente", "sien", "coronilla"
    };
    static readonly string[] PalabrasPerfil =
    {
        "ojo", "ceja", "oreja", "nariz", "boca", "punta de nariz", "puente de la nariz", "párpado", "labio superior",
        "labio inferior", "barbilla", "mandíbula", "pómulo", "mejilla", "frente", "sien", "coronilla", "nuca", "lóbulo"
    };

    enum ModoPalabra { Nada, Pellizco, Nodos, Sello }
    ModoPalabra modoPalabra;
    Transform fondoVocab;
    TextMeshPro tituloVocab;
    bool tocandoLista, izqNodos, izqPellizcoVisto;
    readonly HashSet<long> selNodos = new HashSet<long>();
    readonly HashSet<int> selLineas = new HashSet<int>();
    string ultimoToque = "";
    readonly List<Transform> puntos = new List<Transform>();
    Material matPunto;

    void ArmarVocabulario()
    {
        if (vocabulario != null)
            return;
        vocabulario = new GameObject("Vocabulario").transform;
        vocabulario.SetParent(raiz, false);
        var fondo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(vocabulario, false);
        var rf = fondo.GetComponent<Renderer>();
        if (matPanel != null)
            rf.sharedMaterial = matPanel;
        rf.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fondoVocab = fondo.transform;
        tituloVocab = PanelTutoriales.Texto(vocabulario, "", new Vector3(0f, 0.004f, -0.001f), new Vector2(0.16f, 0.01f), 0.08f, null);
        tituloVocab.fontStyle = FontStyles.Bold;
        Idioma.Poner(tituloVocab, "Toca una palabra (o arrástrala) a tu dibujo");
        palabraFlotante = PanelTutoriales.Texto(raiz, "", Vector3.zero, new Vector2(0.09f, 0.013f), 0.1f, null);
        palabraFlotante.fontStyle = FontStyles.Bold;
        palabraFlotante.color = new Color(0.1f, 0.35f, 0.75f);
        palabraFlotante.outlineWidth = 0.25f;
        palabraFlotante.outlineColor = new Color32(255, 255, 255, 230);
        palabraFlotante.gameObject.SetActive(false);
        var marca = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(marca.GetComponent<Collider>());
        marca.transform.SetParent(raiz, false);
        marca.transform.localScale = Vector3.one * 0.009f;
        var rm = marca.GetComponent<Renderer>();
        if (matSi != null)
            rm.sharedMaterial = matSi;
        rm.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        marcaDestino = marca.transform;
        marcaDestino.gameObject.SetActive(false);
        matPunto = MaterialColor(new Color(0.55f, 0.55f, 0.6f));
    }

    // Los nombres que ya pusiste (en tus líneas).
    HashSet<string> Usadas()
    {
        var u = new HashSet<string>();
        foreach (var t in Originales())
            foreach (var n in t.nombresNodo)
            {
                if (string.IsNullOrEmpty(n))
                    continue;
                int dos = n.IndexOf(':');
                u.Add(dos >= 0 ? n.Substring(dos + 1) : n);
            }
        return u;
    }

    // La lista de ahora: las palabras de esta vista (frente o perfil) que aún no usaste.
    void RefrescarVocabulario()
    {
        if (vocabulario == null)
            return;
        bool dePerfil = estado == Estado.Girando ? perfil : perfilAdivinado;
        var usadas = Usadas();
        palabras.Clear();
        foreach (var w in dePerfil ? PalabrasPerfil : PalabrasFrente)
            if (!usadas.Contains(w))
                palabras.Add(w);
        int porColumna = Mathf.Max(1, Mathf.CeilToInt(palabras.Count / 2f));
        float alto = porColumna * 0.0125f + 0.03f;
        fondoVocab.localPosition = new Vector3(0f, -alto * 0.5f + 0.012f, 0.004f);
        fondoVocab.localScale = new Vector3(0.17f, alto, 0.003f);
        while (palabrasVista.Count < palabras.Count)
        {
            var t = PanelTutoriales.Texto(vocabulario, "", Vector3.zero, new Vector2(0.08f, 0.0105f), 0.07f, null);
            palabrasVista.Add(t);
        }
        for (int i = 0; i < palabrasVista.Count; i++)
        {
            var t = palabrasVista[i];
            bool ver = i < palabras.Count;
            if (t.gameObject.activeSelf != ver)
                t.gameObject.SetActive(ver);
            if (!ver)
                continue;
            int col = i / porColumna, fila = i % porColumna;
            t.transform.localPosition = new Vector3(-0.042f + col * 0.084f, -0.01f - fila * 0.0125f, -0.001f);
            t.text = Mostrar(palabras[i]);
            bool principal = palabras[i].IndexOf(' ') < 0 || palabras[i].StartsWith("ojo ") || palabras[i].StartsWith("ceja ")
                             || palabras[i].StartsWith("oreja ");
            t.fontStyle = principal ? FontStyles.Bold : FontStyles.Normal;
            bool elegida = modoPalabra != ModoPalabra.Nada && palabraEnMano == palabras[i];
            t.color = elegida ? new Color(0.15f, 0.65f, 0.3f) : principal ? new Color(0.1f, 0.3f, 0.7f) : new Color(0.2f, 0.2f, 0.28f);
        }
    }

    void ColocarVocabulario()
    {
        if (vocabulario == null)
            return;
        Vector3 pos = Mundo(new Vector3(semiX + 0.14f, semiY, 0f), 0f) + haciaTi * 0.02f;
        vocabulario.SetPositionAndRotation(pos, Quaternion.LookRotation(-haciaTi, arriba));
        vocabulario.gameObject.SetActive(verEtiquetas);
    }

    string palabraEnMano = "";

    // La palabra que está bajo ese punto (o -1).
    int PalabraEn(Vector3 p, float radio)
    {
        int mejor = -1;
        for (int i = 0; i < palabras.Count && i < palabrasVista.Count; i++)
        {
            float d = Vector3.Distance(p, palabrasVista[i].transform.position);
            if (d < radio)
            {
                radio = d;
                mejor = i;
            }
        }
        return mejor;
    }

    // ¿La izquierda está en NODOS (medio + pulgar)? (Mientras usas una palabra, las manos son de la cabeza.)
    bool IzqEnNodos()
    {
        var izq = control.Izq;
        if (!izq.valida)
            return izqNodos = false;
        float dM = Vector3.Distance(izq.pulgar, izq.medio), dI = Vector3.Distance(izq.pulgar, izq.indice);
        izqNodos = dM < (izqNodos ? 0.04f : 0.025f) && dI > 0.03f;
        return izqNodos;
    }

    // Devuelve true si las manos están usando una palabra (así no gira la cabeza).
    bool RevisarPalabras()
    {
        var der = control.Der;
        bool nodosAhora = IzqEnNodos();
        switch (modoPalabra)
        {
            case ModoPalabra.Pellizco:
                if (!der.valida || !der.pellizco)
                {
                    SoltarPalabra(true);
                    return true;
                }
                MoverPalabra(der.PuntoPellizco);
                return true;
            case ModoPalabra.Nodos:
                if (!nodosAhora)
                {
                    SoltarPalabra(true);
                    return true;
                }
                if (der.valida)
                    MoverPalabra(der.indice);
                return true;
            case ModoPalabra.Sello:
                RevisarSello();
                return true;
        }
        if (vocabulario == null || !vocabulario.gameObject.activeInHierarchy || !der.valida || control.Ocupado)
        {
            tocandoLista = false;
            return false;
        }
        // Pellizco sobre una palabra = llevarla con el pellizco.
        if (der.empezoPellizco)
        {
            int k = PalabraEn(der.PuntoPellizco, 0.016f);
            if (k >= 0)
            {
                TomarPalabra(k, ModoPalabra.Pellizco, der.PuntoPellizco);
                return true;
            }
        }
        // Tocar una palabra con el índice: con la izquierda en Nodos = llevarla pegada al dedo; si no = el sello.
        int toca = der.pellizco ? -1 : PalabraEn(der.indice, 0.013f);
        if (toca >= 0 && !tocandoLista)
        {
            tocandoLista = true;
            TomarPalabra(toca, nodosAhora ? ModoPalabra.Nodos : ModoPalabra.Sello, der.indice);
            return true;
        }
        if (toca < 0)
            tocandoLista = false;
        return false;
    }

    void TomarPalabra(int k, ModoPalabra modo, Vector3 p)
    {
        modoPalabra = modo;
        palabraEnMano = palabras[k];
        palabraFlotante.text = Mostrar(palabraEnMano);
        palabraFlotante.gameObject.SetActive(true);
        selNodos.Clear();
        selLineas.Clear();
        ultimoToque = "";
        izqPellizcoVisto = false;
        control.Ocupado = true;
        control.SonidoBurbuja(p);
        if (opciones != null)
            opciones.gameObject.SetActive(false);
        if (modo == ModoPalabra.Sello)
            dibujo.Mensaje("Toca los nodos o las líneas de \"" + Mostrar(palabraEnMano) + "\" y PELLIZCA con la izquierda para confirmar");
        else if (modo == ModoPalabra.Nodos)
            dibujo.Mensaje("Llévala al nodo y SUELTA la izquierda");
        sucio = true;
        MoverPalabra(p);
    }

    // La palabra sigue a tu dedo; la bolita verde marca el nodo donde caería.
    void MoverPalabra(Vector3 p)
    {
        palabraFlotante.transform.SetPositionAndRotation(p + haciaTi * 0.012f + arriba * 0.014f, Quaternion.LookRotation(-haciaTi, arriba));
        if (modoPalabra == ModoPalabra.Sello)
        {
            marcaDestino.gameObject.SetActive(false);
            return;
        }
        Trazo t;
        int j;
        if (modoPalabra == ModoPalabra.Pellizco && control.HayNodosElegidos)
        {
            marcaDestino.gameObject.SetActive(false);
        }
        else if (NodoCercano(p, 0.045f, out t, out j))
        {
            var visto = Mostrada(t);
            if (visto == null || j >= visto.nodos.Count)
                visto = t;
            marcaDestino.gameObject.SetActive(true);
            marcaDestino.position = visto.transform.TransformPoint(visto.nodos[j]) + haciaTi * 0.003f;
        }
        else
        {
            marcaDestino.gameObject.SetActive(false);
        }
    }

    // EL SELLO: tocar nodos / líneas los marca; pellizco izquierdo (y soltar) = confirmar; tocar la palabra = cancelar.
    void RevisarSello()
    {
        var der = control.Der;
        var izq = control.Izq;
        if (der.valida)
            MoverPalabra(der.indice);
        // Confirmar: un pellizco con la izquierda y soltarlo.
        if (izq.valida && izq.empezoPellizco)
            izqPellizcoVisto = true;
        if (izqPellizcoVisto && (izq.soltoPellizco || !izq.valida))
        {
            ConfirmarSello();
            return;
        }
        MostrarPuntos();
        if (!der.valida)
            return;
        // Tocar la palabra otra vez (u otra palabra) en la lista.
        int toca = PalabraEn(der.indice, 0.013f);
        if (toca >= 0)
        {
            if (!tocandoLista)
            {
                tocandoLista = true;
                if (palabras[toca] == palabraEnMano)
                {
                    SoltarPalabra(false);
                    dibujo.Mensaje("Sello cancelado");
                }
                else
                {
                    palabraEnMano = palabras[toca];
                    palabraFlotante.text = Mostrar(palabraEnMano);
                    sucio = true;
                }
            }
            return;
        }
        tocandoLista = false;
        // ¿Qué toca el índice? Un nodo (ese nodo) o una línea lejos de sus nodos (la línea entera).
        Trazo t;
        int j;
        string toque = "";
        if (NodoCercano(der.indice, 0.013f, out t, out j))
        {
            toque = "n" + t.id + ":" + j;
            if (toque != ultimoToque)
            {
                long clave = (long)t.id * 10000 + j;
                if (!selNodos.Remove(clave))
                    selNodos.Add(clave);
                control.SonidoBurbuja(der.indice);
            }
        }
        else if (LineaCercana(der.indice, 0.009f, out t))
        {
            toque = "l" + t.id;
            if (toque != ultimoToque)
            {
                if (!selLineas.Remove(t.id))
                    selLineas.Add(t.id);
                control.SonidoBurbuja(der.indice);
            }
        }
        else
        {
            // Hay que alejar el dedo un poquito para volver a tocar lo mismo.
            if (ultimoToque.Length > 0 && LejosDeTodo(der.indice, 0.02f))
                ultimoToque = "";
            return;
        }
        ultimoToque = toque;
    }

    void ConfirmarSello()
    {
        string palabra = palabraEnMano;
        int n = 0;
        foreach (int id in selLineas)
        {
            var t = dibujo.BuscarPorId(id);
            if (t == null || t.nodos.Count == 0)
                continue;
            for (int j = 0; j < t.nodos.Count; j++)
                t.NombrarNodo(j, null);
            t.NombrarNodo(t.nodos.Count / 2, palabra);
            sinEtiqueta.Remove(id);
            n++;
        }
        foreach (long clave in selNodos)
        {
            var t = dibujo.BuscarPorId((int)(clave / 10000));
            int j = (int)(clave % 10000);
            if (t == null || j >= t.nodos.Count || selLineas.Contains(t.id))
                continue;
            t.NombrarNodo(j, palabra);
            sinEtiqueta.Remove(t.id);
            n++;
        }
        SoltarPalabra(false);
        if (n > 0)
        {
            dibujo.HayCambios = true;
            dibujo.Mensaje(Idioma.T("Es: ") + Mostrar(palabra));
            AlCambiarNombres();
        }
        else
        {
            dibujo.Mensaje("No tocaste ningún nodo ni línea: sello cancelado");
        }
    }

    // Puntitos sobre los nodos (verdes = elegidos para esta palabra).
    void MostrarPuntos()
    {
        int k = 0;
        foreach (var t in Originales())
        {
            var visto = Mostrada(t);
            if (visto == null)
                continue;
            bool lineaElegida = selLineas.Contains(t.id);
            for (int j = 0; j < visto.nodos.Count && j < t.nodos.Count; j++)
            {
                while (puntos.Count <= k)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(go.GetComponent<Collider>());
                    go.transform.SetParent(raiz, false);
                    var r = go.GetComponent<Renderer>();
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    puntos.Add(go.transform);
                }
                bool elegido = lineaElegida || selNodos.Contains((long)t.id * 10000 + j);
                var pt = puntos[k++];
                if (!pt.gameObject.activeSelf)
                    pt.gameObject.SetActive(true);
                pt.position = visto.transform.TransformPoint(visto.nodos[j]) + haciaTi * 0.002f;
                pt.localScale = Vector3.one * (elegido ? 0.009f : 0.005f);
                var rr = pt.GetComponent<Renderer>();
                var m = elegido ? matSi : matPunto;
                if (rr.sharedMaterial != m)
                    rr.sharedMaterial = m;
            }
        }
        for (int i = k; i < puntos.Count; i++)
            if (puntos[i].gameObject.activeSelf)
                puntos[i].gameObject.SetActive(false);
    }

    void OcultarPuntos()
    {
        foreach (var p in puntos)
            if (p != null && p.gameObject.activeSelf)
                p.gameObject.SetActive(false);
    }

    // Terminar con la palabra: "nombrar" = soltarla en el nodo (pellizco o nodos). El sello confirma aparte.
    void SoltarPalabra(bool nombrar)
    {
        if (modoPalabra == ModoPalabra.Nada)
            return;
        var modo = modoPalabra;
        string palabra = palabraEnMano;
        Vector3 donde = modo == ModoPalabra.Pellizco ? control.Der.PuntoPellizco : control.Der.indice;
        modoPalabra = ModoPalabra.Nada;
        palabraEnMano = "";
        selNodos.Clear();
        selLineas.Clear();
        OcultarPuntos();
        if (palabraFlotante != null)
            palabraFlotante.gameObject.SetActive(false);
        if (marcaDestino != null)
            marcaDestino.gameObject.SetActive(false);
        if (control != null)
            control.Ocupado = false;
        sucio = true;
        if (!nombrar || control == null || modo == ModoPalabra.Sello)
            return;
        int n = modo == ModoPalabra.Pellizco ? control.NombrarNodosElegidos(palabra) : 0;
        if (n == 0)
        {
            Trazo t;
            int j;
            if (NodoCercano(donde, 0.045f, out t, out j))
            {
                t.NombrarNodo(j, palabra);
                sinEtiqueta.Remove(t.id);
                dibujo.HayCambios = true;
                n = 1;
            }
        }
        dibujo.Mensaje(n > 0 ? Idioma.T("Es: ") + Mostrar(palabra) : "Suéltala sobre un nodo de tu dibujo (o elige nodos con el lazo)");
        if (n > 0)
            AlCambiarNombres();
    }

    // El nodo de tu dibujo más cercano a ese punto (en la línea que se ve; el nombre va a TU línea, mismo número).
    bool NodoCercano(Vector3 p, float radio, out Trazo trazo, out int indice)
    {
        trazo = null;
        indice = -1;
        float mejor = radio;
        foreach (var t in Originales())
        {
            var visto = Mostrada(t);
            if (visto == null)
                continue;
            for (int j = 0; j < visto.nodos.Count && j < t.nodos.Count; j++)
            {
                float d = Vector3.Distance(p, visto.transform.TransformPoint(visto.nodos[j]));
                if (d < mejor)
                {
                    mejor = d;
                    trazo = t;
                    indice = j;
                }
            }
        }
        return trazo != null;
    }

    // La línea (tuya) cuyo trazo pasa por ese punto.
    bool LineaCercana(Vector3 p, float radio, out Trazo trazo)
    {
        trazo = null;
        float mejor = radio;
        foreach (var t in Originales())
        {
            var visto = Mostrada(t);
            if (visto == null)
                continue;
            foreach (var c in visto.curva)
            {
                float d = Vector3.Distance(p, visto.transform.TransformPoint(c));
                if (d < mejor)
                {
                    mejor = d;
                    trazo = t;
                }
            }
        }
        return trazo != null;
    }

    bool LejosDeTodo(Vector3 p, float radio)
    {
        Trazo t;
        int j;
        return !NodoCercano(p, radio, out t, out j) && !LineaCercana(p, radio, out t);
    }

    // ---------- El minitutorial (paso a paso, se puede quitar) ----------

    // El tutorial sigue solo al paso que toca (si no lo quitaste con "No mostrar más").
    void AvanzarTutorial(int p)
    {
        if (PlayerPrefs.GetInt(ClaveTutorial, 0) != 0 || p < paso)
            return;
        ArmarTarjeta();
        MostrarTarjeta(p);
    }

    void TutorialAlAbrir(int pasoInicial)
    {
        if (PlayerPrefs.GetInt(ClaveTutorial, 0) != 0)
            return;
        ArmarTarjeta();
        MostrarTarjeta(Mathf.Max(paso, pasoInicial));
    }

    void ArmarTarjeta()
    {
        if (tarjeta != null)
            return;
        if (normal == null)
        {
            var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
            normal = nav != null ? nav.materialBoton : null;
            marcado = nav != null ? nav.materialBotonMarcado : null;
            matPanel = nav != null ? nav.materialPanel : null;
        }
        tarjeta = new GameObject("TutorialCabeza").transform;
        var fondo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(tarjeta, false);
        fondo.transform.localPosition = new Vector3(0f, 0f, 0.004f);
        fondo.transform.localScale = new Vector3(0.22f, 0.15f, 0.003f);
        var rf = fondo.GetComponent<Renderer>();
        if (matPanel != null)
            rf.sharedMaterial = matPanel;
        rf.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tarjetaTitulo = PanelTutoriales.Texto(tarjeta, "", new Vector3(0f, 0.058f, -0.001f), new Vector2(0.2f, 0.016f), 0.13f, null);
        tarjetaTitulo.fontStyle = FontStyles.Bold;
        tarjetaTexto = PanelTutoriales.Texto(tarjeta, "", new Vector3(0f, 0.008f, -0.001f), new Vector2(0.2f, 0.075f), 0.09f, null);
        tarjetaTexto.alignment = TextAlignmentOptions.TopLeft;
        BotonTarjeta("< Atrás", new Vector3(-0.075f, -0.056f, 0f), 0.05f, () => MostrarTarjeta(paso - 1));
        BotonTarjeta("Siguiente >", new Vector3(-0.02f, -0.056f, 0f), 0.055f, () => MostrarTarjeta(paso + 1));
        BotonTarjeta("No mostrar más", new Vector3(0.045f, -0.056f, 0f), 0.068f, () =>
        {
            PlayerPrefs.SetInt(ClaveTutorial, 1);
            PlayerPrefs.Save();
            tarjeta.gameObject.SetActive(false);
            if (dibujo != null)
                dibujo.Mensaje("Listo: \"Ayuda\" lo vuelve a mostrar");
        });
        BotonTarjeta("X", new Vector3(0.095f, -0.056f, 0f), 0.02f, () => tarjeta.gameObject.SetActive(false));
        // v54: se mueve como los demás paneles (pellizco en una parte vacía, o nodos izquierda + índice derecho).
        var mover = tarjeta.gameObject.AddComponent<ArrastrePanel>();
        mover.centro = new Vector2(0f, 0.01f);
        mover.medio = new Vector2(0.11f, 0.06f);
        tarjeta.gameObject.SetActive(false);
    }

    void BotonTarjeta(string texto, Vector3 pos, float ancho, UnityEngine.Events.UnityAction accion)
    {
        var b = PanelTutoriales.Boton(tarjeta, "", pos, new Vector2(ancho, 0.022f), normal, marcado);
        Idioma.Poner(b.etiqueta, texto);
        b.alTocar.AddListener(accion);
    }

    void MostrarTarjeta(int p)
    {
        if (tarjeta == null)
            ArmarTarjeta();
        paso = Mathf.Clamp(p, 0, PasosTitulo.Length - 1);
        EscribirTarjeta();
        // v54: se coloca SOLO al aparecer (antes saltaba de lugar con cada flecha). Arriba a la izquierda de tu vista.
        if (!tarjeta.gameObject.activeSelf)
        {
            var cab = control != null ? control.Cabeza : null;
            if (cab != null)
            {
                Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
                if (adelante.sqrMagnitude < 1e-4f)
                    adelante = Vector3.forward;
                adelante.Normalize();
                Vector3 lado = Vector3.Cross(Vector3.up, adelante).normalized;
                Vector3 pos = cab.position + adelante * 0.5f - lado * 0.3f + Vector3.up * 0.08f;
                tarjeta.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cab.position, Vector3.up));
            }
            tarjeta.gameObject.SetActive(true);
        }
    }

    void EscribirTarjeta()
    {
        if (tarjetaTitulo == null)
            return;
        tarjetaTitulo.text = Idioma.T("Paso") + " " + (paso + 1) + " / " + PasosTitulo.Length + " · " + Idioma.T(PasosTitulo[paso]);
        tarjetaTexto.text = Idioma.T(PasosTexto[paso]);
    }

    void Ayuda()
    {
        ArmarTarjeta();
        MostrarTarjeta(estado == Estado.Preguntando ? 1 : Mathf.Max(2, paso));
    }

    // v54: al cambiar el idioma, TODO lo de la cabeza se reescribe (antes quedaban partes en el otro idioma).
    void AlCambiarIdioma()
    {
        EscribirTarjeta();
        RefrescarVistas();
        sucio = true;
    }

    // ---------- El panel: debajo de la cabeza ----------

    void ArmarPanel()
    {
        if (raiz == null)
        {
            raiz = new GameObject("VistaCabeza").transform;
            Idioma.alCambiar += AlCambiarIdioma;
            var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
            normal = nav != null ? nav.materialBoton : null;
            marcado = nav != null ? nav.materialBotonMarcado : null;
            matPanel = nav != null ? nav.materialPanel : null;
            matSi = MaterialColor(new Color(0.15f, 0.65f, 0.3f));
            matNo = MaterialColor(new Color(0.85f, 0.2f, 0.2f));
            matCambiar = MaterialColor(new Color(0.15f, 0.35f, 0.8f));
            panel = new GameObject("PanelCabeza").transform;
            panel.SetParent(raiz, false);
            Boton(filaPregunta, "De frente", new Vector3(-0.15f, 0f, 0f), 0.055f, false, () => Elegir(false));
            Boton(filaPregunta, "De perfil", new Vector3(-0.092f, 0f, 0f), 0.055f, false, () => Elegir(true));
            Boton(filaPregunta, "Nombrar nodo", new Vector3(-0.028f, 0f, 0f), 0.068f, false, NombrarConTeclado);
            Boton(filaPregunta, "Etiquetas", new Vector3(0.035f, 0f, 0f), 0.052f, false, AlternarEtiquetas);
            Boton(filaPregunta, "Ayuda", new Vector3(0.09f, 0f, 0f), 0.05f, false, Ayuda);
            Boton(filaPregunta, "Cerrar", new Vector3(0.145f, 0f, 0f), 0.05f, false, Cerrar);
            string[] nombresVista = { "Perfil izq", "3/4 izq", "Frente", "3/4 der", "Perfil der" };
            float[] angulos = { -90f, -45f, 0f, 45f, 90f };
            for (int i = 0; i < nombresVista.Length; i++)
            {
                float a = angulos[i];
                Boton(filaGiro, nombresVista[i], new Vector3(-0.124f + i * 0.062f, 0.02f, 0f), 0.058f, false, () => Vista(a));
            }
            Boton(filaGiro, "Usar esta vista", new Vector3(-0.163f, -0.016f, 0f), 0.068f, true, Usar);
            Boton(filaGiro, "Guardar vista", new Vector3(-0.095f, -0.016f, 0f), 0.062f, false, GuardarVista);
            Boton(filaGiro, "Etiquetas", new Vector3(-0.035f, -0.016f, 0f), 0.052f, false, AlternarEtiquetas);
            Boton(filaGiro, "Nombrar nodo", new Vector3(0.027f, -0.016f, 0f), 0.066f, false, NombrarConTeclado);
            Boton(filaGiro, "Ayuda", new Vector3(0.082f, -0.016f, 0f), 0.04f, false, Ayuda);
            Boton(filaGiro, "Nueva cabeza", new Vector3(0.133f, -0.016f, 0f), 0.056f, false, () => { Desesconder(); Empezar(); });
            Boton(filaGiro, "Cerrar", new Vector3(0.183f, -0.016f, 0f), 0.04f, false, Cerrar);
        }
        raiz.gameObject.SetActive(true);
        Vector3 pos = centro + arriba * (centro2D.y - semiY - 0.06f) + derecha * centro2D.x + haciaTi * 0.02f;
        panel.SetPositionAndRotation(pos, Quaternion.LookRotation(-haciaTi, arriba));
        ArmarVocabulario();
        ColocarVocabulario();
        sucio = true;
    }

    void Boton(List<GameObject> fila, string texto, Vector3 pos, float ancho, bool oscuro, UnityEngine.Events.UnityAction accion)
    {
        var b = PanelTutoriales.Boton(panel, "", pos, new Vector2(ancho, 0.028f), oscuro ? marcado : normal, oscuro ? normal : marcado);
        Idioma.Poner(b.etiqueta, texto);   // v54: sigue al idioma de la app aunque lo cambies
        b.alTocar.AddListener(accion);
        if (oscuro)
        {
            b.etiqueta.color = Color.white;
            b.textoNormal = Color.white;
        }
        fila.Add(b.gameObject);
        fila.Add(b.etiqueta.gameObject);
    }

    static void Mostrar(List<GameObject> fila, bool ver)
    {
        foreach (var g in fila)
            if (g != null)
                g.SetActive(ver);
    }

    readonly List<Material> materialesPropios = new List<Material>();

    Material MaterialColor(Color c)
    {
        var baseM = control != null ? control.materialNodo : null;
        var m = baseM != null ? new Material(baseM) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        materialesPropios.Add(m);
        return m;
    }

    // "Nombrar nodo": el teclado de la app, para los nodos del lazo (aunque ya soltaste la izquierda) o el que miras.
    void NombrarConTeclado()
    {
        TecladoVR.Abrir("Nombre del nodo (ej. punta de nariz)", "", nombre =>
        {
            if (string.IsNullOrEmpty(nombre) || control == null)
                return;
            int n = control.NombrarNodos(nombre.ToLowerInvariant());
            dibujo.Mensaje(n > 0 ? "Nodo: " + nombre : "Primero elige un nodo (en Nodos) o míralo");
            if (n > 0)
                AlCambiarNombres();
        });
    }

    void Vista(float angulo)
    {
        float frente = perfil ? (mira > 0f ? -90f : 90f) : 0f;
        giro = frente + angulo;
        inclina = 0f;
        mostrandoReal = false;
        sucio = true;
    }

    void OnDestroy()
    {
        Idioma.alCambiar -= AlCambiarIdioma;
        foreach (var m in materialesPropios)
            if (m != null)
                Destroy(m);
        if (instancia == this)
            instancia = null;
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;

// LA CABEZA INVISIBLE (v51 a v55): dibujas la cabeza de tu personaje UNA vez (DE FRENTE o DE PERFIL) en una capa y la
// app arma con eso un HUEVO 3D invisible, pega cada parte en su superficie y te deja GIRARLA (frente, 3/4 y perfil para
// los dos lados, y un poco arriba/abajo). Pura geometría y reglas: no usa IA.
//  - v55: EL DICTADO. Antes de dibujar di "cara de perfil" (o "cara de frente") y, ANTES de cada parte, su nombre:
//    "nariz", "boca exterior", "mandíbula", "oreja", "cabeza arriba", "nuca", "ojo", "ceja"... Todo lo que dibujes
//    después queda con ese nombre (todos sus nodos), aunque se una con otra línea. Así la app NO adivina nada: sabe la
//    vista, el lado (de perfil, la nariz dice hacia dónde mira) y qué es cada nodo. "fin del dictado" lo termina.
//  - v55: SILUETA POR PIEZAS: cabeza arriba, nuca, mandíbula (y de frente también la frente y la barbilla) forman el
//    BORDE: al girar, todas esas piezas van juntas a la silueta, cada una en su zona. De perfil, la nariz, la frente,
//    la boca exterior y la barbilla van en la línea del centro de la cara.
//  - v55: a la derecha, DOS DIBUJITOS (de frente y de perfil) con cada zona de un color y su nombre; debajo, solo las
//    palabras que sobran. Toca una palabra (de los dibujitos o de la lista) para usarla como SELLO.
//  - v55: "Gira" la hace girar sola (para probar la vista sin agarrarla).
//  - La vista que DIBUJASTE se ve IDÉNTICA (son tus líneas) y TU ORIGINAL NUNCA CAMBIA: queda como clave en su
//    fotograma y como "Original" en la biblioteca. "Usar esta vista" la pone en OTRO fotograma con LOS MISMOS NODOS.
//  - Etiquetas con botones: visto verde (confirmar), X roja (quitar), cuadrito azul (3 palabras probables o teclado).
//  - El sello: toca una palabra, toca nodos o líneas (verdes) y pellizca con la izquierda para confirmar.
public class CabezaInvisible : MonoBehaviour
{
    // ---------- Puntos clave y palabras ----------
    public static readonly string[] PuntosEs =
    {
        "punta de nariz", "puente de la nariz", "ojo izquierdo", "ojo derecho", "párpado", "ceja", "entrecejo",
        "comisura izquierda", "comisura derecha", "labio superior", "labio inferior", "barbilla", "mandíbula", "pómulo",
        "frente", "coronilla", "nuca", "sien", "oreja", "lóbulo",
        "ceja izquierda", "ceja derecha", "oreja izquierda", "oreja derecha"
    };
    public static readonly string[] PuntosEn =
    {
        "nose tip", "nose bridge", "left eye", "right eye", "eyelid", "eyebrow", "between the eyebrows",
        "left mouth corner", "right mouth corner", "upper lip", "lower lip", "chin", "jaw", "cheekbone",
        "forehead", "crown", "nape", "temple", "ear", "earlobe",
        "left eyebrow", "right eyebrow", "left ear", "right ear"
    };
    // Las palabras CORTAS (lo que se guarda) y cómo se pueden decir.
    static readonly string[] Cortas = { "boca", "nariz", "ojo", "ceja", "oreja", "contorno", "mejilla" };
    static readonly string[][] CortasDichas =
    {
        new[] { "boca", "labios", "mouth", "lips" }, new[] { "nariz", "nose" }, new[] { "ojo", "ojos", "eye", "eyes" },
        new[] { "ceja", "cejas", "eyebrow", "eyebrows" }, new[] { "oreja", "orejas", "ear", "ears" },
        new[] { "contorno", "outline" }, new[] { "mejilla", "cheek" }
    };
    static readonly string[] CortasEn = { "mouth", "nose", "eye", "eyebrow", "ear", "outline", "cheek" };
    // Otras partes con nombre propio (para mostrarlas en inglés).
    static readonly string[] OtrasEs = { "boca exterior", "cabeza arriba", "cuello", "cabello", "ojo 1", "ojo 2", "ceja 1", "ceja 2", "oreja 1", "oreja 2" };
    static readonly string[] OtrasEn = { "outer mouth", "top of head", "neck", "hair", "eye 1", "eye 2", "eyebrow 1", "eyebrow 2", "ear 1", "ear 2" };
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

    static string Normalizar(string s)
    {
        s = (s ?? "").ToLowerInvariant().Trim();
        s = s.Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u').Replace('ü', 'u');
        while (s.Contains("  "))
            s = s.Replace("  ", " ");
        return s;
    }

    // ---------- v55: EL DICTADO ----------
    class Parte
    {
        public string guardar;
        public string[] es, en;
    }

    static Parte P(string guardar, string[] es, string[] en)
    {
        return new Parte { guardar = guardar, es = es, en = en };
    }

    static readonly Parte[] Partes =
    {
        P("nariz", new[] { "nariz" }, new[] { "nose" }),
        P("boca", new[] { "boca" }, new[] { "mouth" }),
        P("boca exterior", new[] { "boca exterior", "boca de lado" }, new[] { "outer mouth" }),
        P("mandíbula", new[] { "mandíbula", "quijada" }, new[] { "jaw" }),
        P("barbilla", new[] { "barbilla", "mentón" }, new[] { "chin" }),
        P("oreja", new[] { "oreja" }, new[] { "ear" }),
        P("cabeza arriba", new[] { "cabeza arriba", "cráneo", "parte de arriba" }, new[] { "top of head", "skull" }),
        P("nuca", new[] { "nuca" }, new[] { "nape", "back of head" }),
        P("frente", new[] { "frente" }, new[] { "forehead" }),
        P("ojo", new[] { "ojo" }, new[] { "eye" }),
        P("ceja", new[] { "ceja" }, new[] { "eyebrow" }),
        P("mejilla", new[] { "mejilla" }, new[] { "cheek" }),
        P("cuello", new[] { "cuello" }, new[] { "neck" }),
        P("cabello", new[] { "cabello", "pelo" }, new[] { "hair" }),
        P("contorno", new[] { "contorno" }, new[] { "outline" }),
        P("ojo 1", new[] { "ojo uno" }, new[] { "eye one" }),
        P("ojo 2", new[] { "ojo dos" }, new[] { "eye two" }),
        P("ceja 1", new[] { "ceja uno" }, new[] { "eyebrow one" }),
        P("ceja 2", new[] { "ceja dos" }, new[] { "eyebrow two" }),
        P("oreja 1", new[] { "oreja uno" }, new[] { "ear one" }),
        P("oreja 2", new[] { "oreja dos" }, new[] { "ear two" }),
    };

    // Las frases del dictado (para que la voz las entienda).
    public static string[] FrasesDictado(bool ingles)
    {
        var l = new List<string>();
        foreach (var p in Partes)
            foreach (var f in ingles ? p.en : p.es)
                if (!l.Contains(f))
                    l.Add(f);
        for (int i = 0; i < PuntosEs.Length; i++)
        {
            string f = ingles ? PuntosEn[i] : PuntosEs[i];
            if (!l.Contains(f))
                l.Add(f);
        }
        return l.ToArray();
    }

    static int capaDictado = -1;
    static string parteDictada = "";
    public static bool Dictando => capaDictado >= 0;

    static void Asegurar()
    {
        if (instancia == null)
            instancia = new GameObject("CabezaInvisible").AddComponent<CabezaInvisible>();
    }

    static Dibujo ElDibujo()
    {
        return ControlManos.Instancia != null && ControlManos.Instancia.dibujo != null ? ControlManos.Instancia.dibujo : FindFirstObjectByType<Dibujo>();
    }

    // "cara de perfil" / "cara de frente": la capa recuerda la vista y empieza el dictado.
    public static void EmpezarDictado(bool dePerfil)
    {
        var d = ElDibujo();
        if (d == null)
            return;
        d.CapaActual.vistaCabeza = dePerfil ? 2 : 1;
        d.HayCambios = true;
        capaDictado = d.capaActual;
        parteDictada = "";
        Asegurar();
        d.Mensaje(dePerfil
            ? "Cara de perfil: di qué vas a dibujar (\"nariz\", \"boca exterior\", \"mandíbula\", \"oreja\", \"cabeza arriba\"...) y dibújalo"
            : "Cara de frente: di qué vas a dibujar (\"ojo 1\", \"nariz\", \"boca\", \"oreja 1\", \"cabeza arriba\"...) y dibújalo");
    }

    public static void TerminarDictado()
    {
        bool estaba = Dictando;
        capaDictado = -1;
        parteDictada = "";
        var d = ElDibujo();
        if (d != null)
            d.Mensaje(estaba ? "Dictado terminado: toca Cabeza invisible para girarla" : "No estabas dictando una cara");
    }

    // Una parte dicha ("nariz"...): lo que dibujes desde ahora es eso. false si no es una parte.
    public static bool DictarParte(string dicho)
    {
        dicho = Normalizar(dicho);
        string guardar = null;
        foreach (var p in Partes)
        {
            foreach (var f in p.es)
                if (Normalizar(f) == dicho)
                    guardar = p.guardar;
            foreach (var f in p.en)
                if (Normalizar(f) == dicho)
                    guardar = p.guardar;
        }
        for (int i = 0; i < PuntosEs.Length && guardar == null; i++)
            if (Normalizar(PuntosEs[i]) == dicho || Normalizar(PuntosEn[i]) == dicho)
                guardar = PuntosEs[i];
        if (guardar == null)
            return false;
        parteDictada = guardar;
        var d = ElDibujo();
        if (d != null)
            d.Mensaje(Idioma.T("Ahora dibuja: ") + Mostrar(guardar));
        return true;
    }

    // Dibujo.TerminarTrazo la llama: la línea nueva queda nombrada (todos sus nodos) con la parte dictada.
    public static void AlTerminarLinea(Trazo t)
    {
        if (!Dictando || t == null || t.capa != capaDictado || parteDictada.Length == 0)
            return;
        for (int j = 0; j < t.nodos.Count; j++)
            t.NombrarNodo(j, parteDictada);
        if (Activa)
            instancia.AlCambiarNombres();
    }

    // ---------- v55: QUÉ ES CADA NODO ----------
    enum Clase { Ninguna, Borde, PerfilCara, Nariz, Ojo, Boca, Oreja, Otro }

    static Clase ClaseDe(string n)
    {
        if (string.IsNullOrEmpty(n))
            return Clase.Ninguna;
        n = n.ToLowerInvariant();
        if (n.StartsWith("boca exterior") || n == "outer mouth" || n.StartsWith("frente") || n == "forehead"
            || n.StartsWith("barbilla") || n == "chin")
            return Clase.PerfilCara;
        if (n.Contains("cabeza arriba") || n.Contains("cráneo") || n.Contains("craneo") || n.Contains("nuca") || n.Contains("mandíbula")
            || n.Contains("mandibula") || n.Contains("contorno") || n.Contains("coronilla") || n.Contains("cabello") || n.Contains("pelo")
            || n.Contains("skull") || n.Contains("top of head") || n.Contains("jaw") || n.Contains("nape") || n.Contains("outline")
            || n.Contains("crown") || n.Contains("hair"))
            return Clase.Borde;
        if (n.Contains("nariz") || n.Contains("nose") || n.Contains("entrecejo"))
            return Clase.Nariz;
        if (n.Contains("ojo") || n.Contains("eye") || n.Contains("ceja") || n.Contains("párpado") || n.Contains("parpado"))
            return Clase.Ojo;
        if (n.Contains("boca") || n.Contains("mouth") || n.Contains("labio") || n.Contains("lip") || n.Contains("comisura"))
            return Clase.Boca;
        if (n.Contains("oreja") || n.Contains("lóbulo") || n.Contains("lobulo") || n == "ear" || n.StartsWith("ear ") || n.Contains("earlobe")
            || n.EndsWith(" ear"))
            return Clase.Oreja;
        return Clase.Otro;
    }

    // Cómo va un nodo en el huevo: en el BORDE (la silueta) o pegado con su tipo.
    static void QueTipo(string nombre, bool dePerfil, out bool borde, out Tipo tipo)
    {
        borde = false;
        tipo = Tipo.Rasgo;
        switch (ClaseDe(nombre))
        {
            case Clase.Borde:
                borde = true;
                return;
            case Clase.PerfilCara:
                if (dePerfil)
                {
                    tipo = Tipo.Centro;   // de perfil: la línea del centro de la cara
                    return;
                }
                if (nombre.StartsWith("boca") || nombre == "outer mouth")
                    return;
                borde = true;             // de frente: la frente y la barbilla son parte del borde
                return;
            case Clase.Nariz: tipo = dePerfil ? Tipo.Centro : Tipo.NarizFrente; return;
            case Clase.Ojo: tipo = dePerfil ? Tipo.OjoPerfil : Tipo.Rasgo; return;
            case Clase.Boca: tipo = dePerfil ? Tipo.Boca : Tipo.Rasgo; return;
            case Clase.Oreja: tipo = Tipo.Oreja; return;
        }
    }

    // El nombre de cada nodo; los nodos sin nombre toman el del nodo con nombre más cercano en la línea.
    static string[] NombresPropagados(Trazo t)
    {
        int n = t.nodos.Count;
        var r = new string[n];
        bool alguno = false;
        for (int j = 0; j < n; j++)
        {
            r[j] = t.NombreDeNodo(j);
            if (r[j].Length > 0)
                alguno = true;
        }
        if (!alguno)
            return r;
        var copia = (string[])r.Clone();
        for (int j = 0; j < n; j++)
        {
            if (copia[j].Length > 0)
                continue;
            for (int d = 1; d < n; d++)
            {
                if (j - d >= 0 && copia[j - d].Length > 0) { r[j] = copia[j - d]; break; }
                if (j + d < n && copia[j + d].Length > 0) { r[j] = copia[j + d]; break; }
            }
        }
        return r;
    }

    static CabezaInvisible instancia;
    public static bool Activa => instancia != null && instancia.estado != Estado.Nada;

    // v53: el cuadrito de una etiqueta espera un nombre: el que digas con la voz (o escribas) va a esa parte.
    public static bool NombrarPendiente(string nombre)
    {
        if (!Activa || instancia.pendiente == 0 || string.IsNullOrEmpty(nombre))
            return false;
        int id = instancia.pendiente;
        instancia.pendiente = 0;
        TecladoVR.Cerrar();
        instancia.NombrarRango(id, instancia.pendienteDesde, instancia.pendienteHasta, nombre);
        return true;
    }

    // v53: alguien nombró nodos (voz o teclado): la cabeza vuelve a mirar qué es cada línea.
    public static void AlNombrar()
    {
        if (Activa)
            instancia.AlCambiarNombres();
    }

    // v54: "mirar y decir": la línea que miras (con la mirada) toma ese nombre entera.
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

    enum Estado { Nada, Preguntando, Girando }
    enum Tipo { Rasgo, Centro, Boca, Oreja, OjoPerfil, NarizFrente }

    // Lo que se sabe de cada nodo para pegarlo en el huevo.
    struct NodoInfo
    {
        public bool borde;
        public Tipo tipo;
        public Vector2 c;       // centro de su parte (en tu dibujo)
        public float x0, x1;    // de dónde a dónde va su parte (de lado)
        public float f;         // dónde va en su parte (0 a 1)
    }

    // Una línea del modelo (en el espacio de tu dibujo original, respecto al centro de la cabeza).
    class Linea
    {
        public int idOrigen;               // la línea tuya
        public bool espejo;                // la copia del otro lado (dibujada de perfil)
        public readonly List<bool> borde = new List<bool>();             // nodos de la silueta
        public readonly List<Vector2> orig = new List<Vector2>();        // cada nodo en tu dibujo
        public readonly List<float> ang = new List<float>();             // su ángulo alrededor del centro
        public readonly List<Vector3> nodos = new List<Vector3>();       // sus nodos sobre el huevo
        public readonly List<Vector3> asaE = new List<Vector3>(), asaS = new List<Vector3>();  // punta de cada tirador
        public readonly List<bool> manual = new List<bool>();
        public readonly List<float> prof = new List<float>();            // cuánto se salía de tu plano (dibujos en 3D)
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
    int idContorno = -1;                   // si no nombraste el borde: la línea más grande hace de contorno
    Vector3 centro, derecha, arriba, haciaTi;
    Vector2 centro2D;
    float semiX, semiY, semiZ;
    readonly float[] proporcion = new float[72];
    bool perfil, perfilAdivinado, hayModelo, vistaDictada;
    float mira = 1f, miraAdivinada = 1f;
    float giro, inclina;
    bool agarrada;
    Vector3 agarreInicio;
    float giroInicio, inclinaInicio;
    bool verEtiquetas = true;
    readonly List<VistaGuardada> vistas = new List<VistaGuardada>();
    int fotogramaOriginal;
    bool mostrandoReal = true;        // se ven tus líneas de verdad (no la vista previa)
    int fotogramaVisto = -1;
    int pendiente, pendienteDesde, pendienteHasta;   // la parte cuyo nombre se espera (el cuadrito de su etiqueta)
    readonly HashSet<int> sinEtiqueta = new HashSet<int>();
    bool sucio = true;
    float proximaEtiqueta;
    bool animando;                    // v55: "Gira": gira sola
    float animandoDesde;

    // La vista previa: líneas sueltas iguales a las tuyas.
    readonly List<Trazo> clones = new List<Trazo>();
    readonly List<Renderer> apagados = new List<Renderer>();
    readonly List<Vector3> silueta = new List<Vector3>();

    // El panel, las etiquetas, el vocabulario y el tutorial.
    Transform raiz, panel;
    Material normal, marcado, matPanel, matSi, matNo, matCambiar, matPunto;
    readonly List<GameObject> filaPregunta = new List<GameObject>(), filaGiro = new List<GameObject>(), filaVistas = new List<GameObject>();
    BotonTocable btnGira;
    TextMeshPro cartelDictado;

    class Chip
    {
        public Transform t;
        public TextMeshPro texto;
        public GameObject botones;
        public int id, desde, hasta;
        public string palabra;
        public bool segura;
    }
    readonly List<Chip> chips = new List<Chip>();

    Transform tarjeta;
    TextMeshPro tarjetaTitulo, tarjetaTexto;
    int paso;
    static readonly string[] PasosTitulo = { "Dicta la cara", "¿De frente o de perfil?", "Revisa las etiquetas", "Gira y usa la vista", "Retoca y guarda" };
    static readonly string[] PasosTexto =
    {
        "Antes de dibujar, di \"cara de perfil\" (o \"cara de frente\"). Luego, ANTES de cada parte, di su nombre y dibújala: \"nariz\", \"boca exterior\", \"mandíbula\", \"oreja\", \"cabeza arriba\", \"ojo\"... Los dibujitos de la derecha muestran cada zona con su color. Al terminar di \"fin del dictado\" y toca Cabeza invisible.",
        "Si la dictaste, la app ya sabe la vista. Si no, toca \"De frente\" o \"De perfil\" según como la dibujaste. Esa vista queda IDÉNTICA a tu dibujo y tu original nunca cambia.",
        "Cada parte tiene su cartelito: visto verde = confirmar, X roja = quitar, cuadrito azul = las 3 palabras más probables o el teclado. EL SELLO: toca una palabra (de los dibujitos o de la lista), toca los nodos (o las líneas) que son eso y haz un pellizco con la izquierda para confirmar.",
        "Toca \"Gira\" para verla girar sola (o Perfil, 3/4, Frente, o pellízcala con la derecha). Toca \"Usar esta vista\": se pone en OTRO fotograma, con tus mismos nodos (morph). Tu original sigue en el suyo.",
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
        Asegurar();
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
        animando = false;
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
        if (originales.Count < 1 || (originales.Count < 2 && !HayNombres(originales)))
        {
            dibujo.Mensaje("Cabeza invisible: dibuja en esta capa la cabeza (o díctala: di \"cara de perfil\" y el nombre de cada parte)");
            AvanzarTutorial(0);
            return;
        }
        capaCabeza = dibujo.capaActual;
        fotogramaOriginal = dibujo.animacion != null ? dibujo.animacion.Fotograma : 0;
        int vista = dibujo.CapaActual.vistaCabeza;
        vistaDictada = vista > 0;
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
        centro2D = Vector2.zero;

        // La línea más grande (por si no nombraste el borde: hace de contorno).
        float mayor = -1f;
        foreach (var t in originales)
        {
            Vector2 mn, mx;
            Caja(t, out mn, out mx);
            float area = (mx.x - mn.x) * (mx.y - mn.y);
            if (area > mayor)
            {
                mayor = area;
                idContorno = t.id;
            }
        }
        // El tamaño de la cabeza: las piezas del BORDE (las que nombraste o dictaste); si no hay, la línea más grande.
        bool dePerfil = vista == 2;
        var pts = PuntosBorde(originales, dePerfil, true);
        Vector2 cmn = new Vector2(float.MaxValue, float.MaxValue), cmx = new Vector2(float.MinValue, float.MinValue);
        foreach (var q in pts)
        {
            cmn = Vector2.Min(cmn, q);
            cmx = Vector2.Max(cmx, q);
        }
        centro2D = (cmn + cmx) * 0.5f;
        semiX = Mathf.Max(0.01f, (cmx.x - cmn.x) * 0.5f);
        semiY = Mathf.Max(0.01f, (cmx.y - cmn.y) * 0.5f);
        if (vistaDictada)
        {
            perfilAdivinado = dePerfil;
        }
        else
        {
            // ¿De perfil? (solo para las sugerencias): algo sobresale mucho del borde por un solo lado (la nariz).
            int fueraIzq = 0, fueraDer = 0;
            foreach (var t in originales)
                if (t.id != idContorno)
                    foreach (var p in t.curva)
                    {
                        Vector2 q = Plano(t.transform.TransformPoint(p));
                        if (q.x > semiX * 1.02f) fueraDer++;
                        if (q.x < -semiX * 1.02f) fueraIzq++;
                    }
            perfilAdivinado = (fueraDer > 3 && fueraIzq == 0) || (fueraIzq > 3 && fueraDer == 0);
        }
        miraAdivinada = perfilAdivinado ? Mira(originales) : 1f;
        CalcularSugerencias(originales, perfilAdivinado, miraAdivinada);
        ArmarPanel();
        Mostrar(filaPregunta, true);
        Mostrar(filaGiro, false);
        Mostrar(filaVistas, false);
        estado = Estado.Preguntando;
        mostrandoReal = true;
        sucio = true;
        if (vistaDictada)
        {
            // Ya sabes la vista (la dictaste): directo a girar.
            Elegir(dePerfil);
            TutorialAlAbrir(2);
            return;
        }
        TutorialAlAbrir(1);
        dibujo.Mensaje("Cabeza invisible: ¿la dibujaste de frente o de perfil?");
    }

    static bool HayNombres(List<Trazo> ts)
    {
        foreach (var t in ts)
            if (t.nombresNodo.Count > 0)
                return true;
        return false;
    }

    void Cerrar()
    {
        SoltarPalabra(false);
        Desesconder();
        BorrarClones();
        estado = Estado.Nada;
        agarrada = false;
        animando = false;
        pendiente = 0;
        if (raiz != null)
            raiz.gameObject.SetActive(false);
        if (tarjeta != null)
            tarjeta.gameObject.SetActive(false);
        if (control != null && control.Ocupado)
            control.Ocupado = false;
    }

    // ---------- Esconder tus líneas mientras se ve la vista previa ----------

    // Cada cuadro (una línea que se redibuja se vuelve a prender sola).
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
    }

    void OcultarClones()
    {
        foreach (var c in clones)
            if (c != null && c.gameObject.activeSelf)
                c.gameObject.SetActive(false);
    }

    // ---------- Armar el modelo ----------

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

    // ¿Algún nodo está nombrado como parte del BORDE (cabeza arriba, nuca, mandíbula...)?
    static bool HayBordeNombrado(List<Trazo> ts, bool dePerfil)
    {
        foreach (var t in ts)
            foreach (var n in NombresPropagados(t))
            {
                bool borde;
                Tipo tipo;
                QueTipo(n, dePerfil, out borde, out tipo);
                if (borde)
                    return true;
            }
        return false;
    }

    // Los puntos del borde (en tu dibujo): para el tamaño de la cabeza y la forma de su silueta.
    List<Vector2> PuntosBorde(List<Trazo> ts, bool dePerfil, bool conPerfilCara)
    {
        var pts = new List<Vector2>();
        bool hay = HayBordeNombrado(ts, dePerfil);
        foreach (var t in ts)
        {
            if (!hay)
            {
                if (t.id == idContorno)
                    foreach (var p in t.curva)
                        pts.Add(Plano(t.transform.TransformPoint(p)));
                continue;
            }
            var nombres = NombresPropagados(t);
            int enBorde = 0;
            for (int j = 0; j < nombres.Length; j++)
            {
                bool borde;
                Tipo tipo;
                QueTipo(nombres[j], dePerfil, out borde, out tipo);
                bool cuenta = borde || (conPerfilCara && dePerfil && ClaseDe(nombres[j]) == Clase.PerfilCara);
                if (cuenta)
                {
                    pts.Add(Plano(t.transform.TransformPoint(t.nodos[j])));
                    enBorde++;
                }
            }
            if (enBorde == nombres.Length && nombres.Length > 0)
                foreach (var p in t.curva)
                    pts.Add(Plano(t.transform.TransformPoint(p)));
        }
        if (pts.Count < 3)
        {
            var c = dibujo.BuscarPorId(idContorno);
            if (c != null)
                foreach (var p in c.curva)
                    pts.Add(Plano(c.transform.TransformPoint(p)));
        }
        return pts;
    }

    // De perfil: hacia dónde mira la cara (1 = a tu derecha). La nariz dice adelante; la oreja, atrás.
    float Mira(List<Trazo> ts)
    {
        Vector2 sumaNariz = Vector2.zero, sumaOreja = Vector2.zero, sumaTodo = Vector2.zero;
        int nN = 0, nO = 0, nT = 0;
        float sx = 0f;
        foreach (var t in ts)
        {
            var nombres = NombresPropagados(t);
            for (int j = 0; j < t.nodos.Count; j++)
            {
                Vector2 q = Plano(t.transform.TransformPoint(t.nodos[j]));
                sumaTodo += q;
                nT++;
                var c = ClaseDe(nombres[j]);
                if (c == Clase.Nariz) { sumaNariz += q; nN++; }
                if (c == Clase.Oreja) { sumaOreja += q; nO++; }
                if (t.id != idContorno)
                    sx += q.x;
            }
        }
        Vector2 medio = sumaTodo / Mathf.Max(1, nT);
        if (nN > 0)
            return sumaNariz.x / nN >= medio.x ? 1f : -1f;
        if (nO > 0)
            return sumaOreja.x / nO >= medio.x ? -1f : 1f;
        return sx < 0f ? -1f : 1f;
    }

    void Elegir(bool dePerfil)
    {
        var capa = dibujo.DatosDeCapa(capaCabeza);
        if (capa != null && capa.vistaCabeza != (dePerfil ? 2 : 1))
        {
            capa.vistaCabeza = dePerfil ? 2 : 1;
            dibujo.HayCambios = true;
        }
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
        dibujo.Mensaje(dePerfil ? "De perfil: toca Gira para verla girar" : "De frente: toca Gira para verla girar");
    }

    bool ArmarModelo(bool dePerfil)
    {
        // Líneas temporales con la forma de tu ORIGINAL (y los nombres de ahora): con ellas se arma el modelo.
        var temporales = new List<Trazo>();
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
        }
        Trazo.silenciar = antesSilencio;
        Trazo.huboCambio = false;
        bool ok = temporales.Count > 0 && ArmarModeloCon(dePerfil, temporales);
        foreach (var t in temporales)
            Destroy(t.gameObject);
        return ok;
    }

    bool ArmarModeloCon(bool dePerfil, List<Trazo> originales)
    {
        perfil = dePerfil;
        lineas.Clear();
        mira = perfil ? Mira(originales) : 1f;
        semiZ = perfil ? semiX * 0.8f : semiX * 1.1f;
        CalcularSugerencias(originales, perfil, mira);
        ArmarProporcion(PuntosBorde(originales, perfil, false));
        bool hay = HayBordeNombrado(originales, perfil);
        foreach (var t in originales)
        {
            var info = InfoNodos(t, perfil, hay);
            var l = new Linea { idOrigen = t.id };
            Llenar(l, t, info);
            lineas.Add(l);
            // De perfil, el ojo, la ceja y la oreja se copian al otro lado (para cuando la ves de frente).
            bool algunBorde = false;
            int ojoOreja = 0;
            foreach (var i in info)
            {
                if (i.borde)
                    algunBorde = true;
                if (i.tipo == Tipo.OjoPerfil || i.tipo == Tipo.Oreja)
                    ojoOreja++;
            }
            if (perfil && !algunBorde && ojoOreja * 2 > info.Length)
            {
                var e = new Linea { idOrigen = t.id, espejo = true };
                Llenar(e, t, info);
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

    // Qué es cada nodo de una línea: su nombre (o el de la sugerencia, si la línea no tiene nombres).
    NodoInfo[] InfoNodos(Trazo t, bool dePerfil, bool hayBorde)
    {
        int n = t.nodos.Count;
        var nombres = NombresPropagados(t);
        bool sinNombres = true;
        foreach (var s in nombres)
            if (s.Length > 0)
                sinNombres = false;
        string deLinea = "";
        if (sinNombres)
            deLinea = !hayBorde && t.id == idContorno ? "contorno" : MejorSugerencia(t.id);
        var q = new Vector2[n];
        for (int j = 0; j < n; j++)
            q[j] = Plano(t.transform.TransformPoint(t.nodos[j]));
        var r = new NodoInfo[n];
        int a = 0;
        while (a < n)
        {
            string nombre = sinNombres ? deLinea : nombres[a];
            int b = a;
            while (b + 1 < n && (sinNombres || nombres[b + 1] == nombre))
                b++;
            Vector2 c = Vector2.zero;
            float x0 = float.MaxValue, x1 = float.MinValue;
            for (int j = a; j <= b; j++)
            {
                c += q[j];
                x0 = Mathf.Min(x0, q[j].x);
                x1 = Mathf.Max(x1, q[j].x);
            }
            c /= (b - a + 1);
            bool borde;
            Tipo tipo;
            QueTipo(nombre, dePerfil, out borde, out tipo);
            for (int j = a; j <= b; j++)
                r[j] = new NodoInfo { borde = borde, tipo = tipo, c = c, x0 = x0, x1 = x1, f = b > a ? (j - a) / (float)(b - a) : 0.5f };
            a = b + 1;
        }
        return r;
    }

    // Los nodos de la línea sobre el huevo (y la punta de sus tiradores).
    void Llenar(Linea l, Trazo t, NodoInfo[] info)
    {
        int n = t.nodos.Count;
        t.AsegurarAsas();
        for (int j = 0; j < n; j++)
        {
            Vector3 nodoLocal = t.nodos[j];
            Vector3 m = t.transform.TransformPoint(nodoLocal);
            Vector2 q = Plano(m);
            l.borde.Add(info[j].borde);
            l.orig.Add(q);
            l.ang.Add(Mathf.Atan2(q.y, q.x));
            l.nodos.Add(Espejar(l, Mapear(info[j], q)));
            l.prof.Add(Vector3.Dot(m - centro, haciaTi));
            bool man = j < t.asaManual.Count && t.asaManual[j];
            l.manual.Add(man);
            Vector3 e = j < t.asaEntrada.Count ? t.asaEntrada[j] : Vector3.zero;
            Vector3 s = j < t.asaSalida.Count ? t.asaSalida[j] : Vector3.zero;
            l.asaE.Add(Espejar(l, Mapear(info[j], Plano(t.transform.TransformPoint(nodoLocal + e)))));
            l.asaS.Add(Espejar(l, Mapear(info[j], Plano(t.transform.TransformPoint(nodoLocal + s)))));
        }
    }

    static Vector3 Espejar(Linea l, Vector3 p)
    {
        return l.espejo ? new Vector3(p.x, p.y, -p.z) : p;
    }

    // Un punto de tu dibujo sobre el huevo, según qué es. Sin girar, cada punto queda EXACTAMENTE donde lo dibujaste
    // (x e y no cambian; solo se inventa la profundidad).
    Vector3 Mapear(NodoInfo i, Vector2 q)
    {
        float k = K(q);
        float u = q.x / semiX, v = q.y / semiY;
        if (!perfil)
        {
            if (i.tipo == Tipo.Oreja)
            {
                // La oreja: un disco al lado, inclinado hacia atrás (de perfil se ve como oreja, no como una raya).
                float lado = i.c.x > 0f ? 1f : -1f;
                float dx = q.x - i.c.x;
                return new Vector3(q.x, q.y, -0.1f * semiZ - dx * lado);
            }
            float z = k > 0f ? semiZ * Mathf.Sqrt(k) : -0.05f * semiZ;
            if (i.tipo == Tipo.NarizFrente)
                z += 0.25f * semiZ * Mathf.Clamp01(1f - Mathf.Abs(q.x - i.c.x) / Mathf.Max(0.005f, (i.x1 - i.x0) * 0.5f + 0.005f));
            else if (Mathf.Abs(u) < 0.22f && v > -0.45f && v < 0.15f)
                z += 0.22f * semiZ * (1f - Mathf.Abs(u) / 0.22f);
            return new Vector3(q.x, q.y, z);
        }
        switch (i.tipo)
        {
            case Tipo.Centro:
                return new Vector3(q.x, q.y, 0f);
            case Tipo.Boca:
                return new Vector3(q.x, q.y, (i.f - 0.5f) * 0.7f * semiZ);
            case Tipo.Oreja:
            {
                float dx = q.x - i.c.x;
                return new Vector3(q.x, q.y, semiZ * 0.9f - dx * mira);
            }
            default:
            {
                float z = semiZ * Mathf.Clamp(1f - u * mira, 0.2f, 1f) * Mathf.Sqrt(Mathf.Max(1f - v * v, 0.05f));
                z += (q.x - (i.x0 + i.x1) * 0.5f) * mira * 0.6f;
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

    float K(Vector2 q)
    {
        float u = q.x / semiX, v = q.y / semiY;
        return 1f - u * u - v * v;
    }

    void ArmarProporcion(List<Vector2> pts)
    {
        var suma = new float[proporcion.Length];
        var cuenta = new int[proporcion.Length];
        foreach (var q in pts)
        {
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

    // ---------- v54: SUGERENCIAS por zonas (para las líneas que no nombraste ni dictaste) ----------
    class Rasgo
    {
        public int id;
        public float u, v;      // centro: u de -1 (izquierda) a 1 (derecha), v de -1 (abajo) a 1 (arriba)
        public float w, h;      // ancho y alto (fracción de la cabeza)
        public float aspecto;   // alto ÷ ancho
        public float fuera;     // qué parte sale del borde (0 a 1)
        public bool cerrado;
    }

    readonly Dictionary<int, List<KeyValuePair<string, float>>> sugerencias = new Dictionary<int, List<KeyValuePair<string, float>>>();

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

    void CalcularSugerencias(List<Trazo> ts, bool dePerfil, float m)
    {
        sugerencias.Clear();
        var rasgos = new List<Rasgo>();
        foreach (var t in ts)
            if (t != null && t.curva.Count >= 2)
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
            p.Add(new KeyValuePair<string, float>("cabeza arriba", G(r.v, 0.75f, 0.2f) * (r.w > 0.6f ? 1.2f : 0.3f)));
            p.Add(new KeyValuePair<string, float>("mandíbula", G(r.v, -0.7f, 0.2f) * (r.w > 0.6f ? 1.2f : 0.3f)));
        }
        else
        {
            float s = r.u * m;   // hacia donde mira la cara = positivo
            p.Add(new KeyValuePair<string, float>("nariz", G(s, 0.95f, 0.18f) * G(r.v, -0.1f, 0.22f) * (r.fuera > 0.2f ? 1.5f : 1f)));
            p.Add(new KeyValuePair<string, float>("ojo",
                G(s, 0.55f, 0.2f) * G(r.v, 0.08f, 0.18f) * (r.cerrado ? 1.2f : 1f) * (r.w < 0.3f ? 1f : 0.5f)));
            p.Add(new KeyValuePair<string, float>("ceja",
                G(s, 0.55f, 0.25f) * G(r.v, 0.3f, 0.15f) * (delgada ? 1.3f : 0.5f) * (algoDebajo ? 1.3f : 1f)));
            p.Add(new KeyValuePair<string, float>("boca exterior", G(s, 0.85f, 0.2f) * G(r.v, -0.45f, 0.18f) * (r.fuera > 0.1f ? 1.2f : 1f)));
            p.Add(new KeyValuePair<string, float>("oreja", G(s, -0.15f, 0.25f) * G(r.v, 0f, 0.3f) * (vertical ? 1.2f : 0.8f)));
            p.Add(new KeyValuePair<string, float>("mejilla", G(s, 0.45f, 0.2f) * G(r.v, -0.25f, 0.15f) * 0.45f));
            p.Add(new KeyValuePair<string, float>("cabeza arriba", G(r.v, 0.7f, 0.22f) * (r.w > 0.5f ? 1.2f : 0.3f)));
            p.Add(new KeyValuePair<string, float>("nuca", G(s, -0.85f, 0.2f) * G(r.v, -0.1f, 0.35f) * (vertical ? 1.2f : 0.6f)));
            p.Add(new KeyValuePair<string, float>("mandíbula", G(r.v, -0.7f, 0.2f) * G(s, 0.2f, 0.35f) * (r.w > 0.3f ? 1.1f : 0.5f)));
        }
        p.Sort((a, b) => b.Value.CompareTo(a.Value));
        return p;
    }

    string MejorSugerencia(int id)
    {
        List<KeyValuePair<string, float>> l;
        return sugerencias.TryGetValue(id, out l) && l.Count > 0 ? l[0].Key : "";
    }

    // ---------- Cada cuadro ----------

    void LateUpdate()
    {
        if (control == null)
            control = ControlManos.Instancia;
        if (dibujo == null && control != null)
            dibujo = control.dibujo;
        ActualizarCartelDictado();
        if (estado == Estado.Nada || control == null || dibujo == null)
            return;
        bool palabra = RevisarPalabras();
        if (estado == Estado.Girando)
        {
            if (animando)
            {
                // v55: "Gira": va y viene de un perfil al otro, sola.
                float frente = perfil ? (mira > 0f ? -90f : 90f) : 0f;
                giro = frente + 85f * Mathf.Sin((Time.time - animandoDesde) * Mathf.PI * 2f / 6f);
                inclina = 0f;
                mostrandoReal = false;
                sucio = true;
            }
            else if (!palabra)
            {
                RevisarAgarre();
            }
            // Si cambias de fotograma, se ven tus líneas de verdad (las de ese fotograma).
            var anim = dibujo.animacion;
            int f = anim != null ? anim.Fotograma : 0;
            if (f != fotogramaVisto)
            {
                fotogramaVisto = f;
                if (!agarrada && !animando)
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
                foreach (var c in clones)
                    if (c != null && c.gameObject.activeSelf)
                        dibujo.HaloLapiz(c);
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

    // v55: dictando, junto a tu mano dice qué estás dibujando.
    void ActualizarCartelDictado()
    {
        bool ver = Dictando && control != null && control.Der.valida && control.Cabeza != null;
        if (!ver)
        {
            if (cartelDictado != null && cartelDictado.gameObject.activeSelf)
                cartelDictado.gameObject.SetActive(false);
            return;
        }
        if (cartelDictado == null)
        {
            cartelDictado = PanelTutoriales.Texto(transform, "", Vector3.zero, new Vector2(0.14f, 0.016f), 0.1f, null);
            cartelDictado.fontStyle = FontStyles.Bold;
            cartelDictado.color = new Color(0.1f, 0.35f, 0.75f);
            cartelDictado.outlineWidth = 0.25f;
            cartelDictado.outlineColor = new Color32(255, 255, 255, 230);
        }
        if (!cartelDictado.gameObject.activeSelf)
            cartelDictado.gameObject.SetActive(true);
        cartelDictado.text = parteDictada.Length > 0 ? Idioma.T("Dibujando: ") + Mostrar(parteDictada) : Idioma.T("Di qué vas a dibujar");
        Vector3 pos = control.Der.indice + Vector3.up * 0.04f;
        Vector3 mirar = pos - control.Cabeza.position;
        cartelDictado.transform.SetPositionAndRotation(pos, mirar.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(mirar, Vector3.up) : Quaternion.identity);
    }

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

    // La silueta del huevo girado, con la forma de tu borde (en el plano de tu dibujo).
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

    // La forma de una línea en este giro, con LOS MISMOS nodos. Los del BORDE van a la silueta (en su mismo ángulo);
    // los demás se pegan al huevo, y los que quedan atrás se pegan al vecino que sí se ve.
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
            if (l.borde[j] && !l.espejo)
            {
                Vector2 p = Vector2.Lerp(l.orig[j], EnSilueta(l.ang[j]), w);
                pos[j] = new Vector3(p.x, p.y, 0f);
                vis[j] = true;
            }
            else
            {
                vis[j] = Visible(q, l.nodos[j]);
                pos[j] = Plano3(q * l.nodos[j]);
            }
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
            if (j >= d.asaManual.Count)
                continue;
            if (l.borde[j])
            {
                if (w > 0.001f)
                    d.asaManual[j] = false;   // el borde girado: sus tiradores se recalculan solos (curva suave)
            }
            else if (l.manual[j] && vis[j])
            {
                d.asaEntrada[j] = raizD.InverseTransformPoint(Mundo(Plano3(q * l.asaE[j]), l.prof[j] * conservar)) - d.nodos[j];
                d.asaSalida[j] = raizD.InverseTransformPoint(Mundo(Plano3(q * l.asaS[j]), l.prof[j] * conservar)) - d.nodos[j];
            }
            else
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
        animando = false;
        RefrescarBotonGira();
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
        animando = false;
        RefrescarBotonGira();
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

    // Una palabra guardada (en español) en el idioma de la app.
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
        for (int i = 0; i < OtrasEs.Length; i++)
            if (es == OtrasEs[i])
                return OtrasEn[i];
        return Idioma.T(es);
    }

    bool ModoPerfil => estado == Estado.Girando ? perfil : perfilAdivinado;
    float MiraAhora => estado == Estado.Girando ? mira : miraAdivinada;

    // De perfil, "ojo", "ceja" y "oreja" dicen su lado: si la cara mira a tu derecha, ves su lado DERECHO.
    string ConLado(string palabra)
    {
        if (!ModoPerfil)
            return palabra;
        bool derecho = MiraAhora > 0f;
        if (palabra == "ojo")
            return derecho ? "ojo derecho" : "ojo izquierdo";
        if (palabra == "ceja")
            return derecho ? "ceja derecha" : "ceja izquierda";
        if (palabra == "oreja")
            return derecho ? "oreja derecha" : "oreja izquierda";
        return palabra;
    }

    // Dónde se ve una línea ahora (tu línea de verdad o su vista previa).
    Trazo Mostrada(Trazo original)
    {
        if (estado != Estado.Girando || mostrandoReal || Peso() < 0.001f)
            return original;
        for (int i = 0; i < lineas.Count && i < clones.Count; i++)
            if (!lineas[i].espejo && lineas[i].idOrigen == original.id && clones[i] != null)
                return clones[i].gameObject.activeSelf ? clones[i] : null;
        return original;
    }

    // Los tramos de una línea: cada parte nombrada es un tramo; una línea sin nombres es un solo tramo (sugerido).
    struct Tramo
    {
        public int desde, hasta;
        public string palabra;
        public bool segura;
    }

    List<Tramo> Tramos(Trazo t, bool hayBorde)
    {
        var r = new List<Tramo>();
        var nombres = NombresPropagados(t);
        int n = nombres.Length;
        if (n == 0)
            return r;
        if (nombres[0].Length == 0)
        {
            string s = !hayBorde && t.id == idContorno ? "contorno" : MejorSugerencia(t.id);
            r.Add(new Tramo { desde = 0, hasta = n - 1, palabra = s, segura = false });
            return r;
        }
        int a = 0;
        while (a < n)
        {
            int b = a;
            while (b + 1 < n && nombres[b + 1] == nombres[a])
                b++;
            r.Add(new Tramo { desde = a, hasta = b, palabra = nombres[a], segura = true });
            a = b + 1;
        }
        return r;
    }

    void PonerEtiquetas()
    {
        int k = 0;
        // Con el sello en la mano, las etiquetas se esconden (así el índice no toca sus botones sin querer).
        if (verEtiquetas && raiz != null && modoPalabra != ModoPalabra.Sello)
        {
            var originales = Originales();
            bool hayBorde = HayBordeNombrado(originales, ModoPerfil);
            foreach (var t in originales)
            {
                if (sinEtiqueta.Contains(t.id))
                    continue;
                var visto = Mostrada(t);
                if (visto == null || visto.oculto)
                    continue;
                foreach (var tr in Tramos(t, hayBorde))
                {
                    Vector3 suma = Vector3.zero;
                    int cuantos = 0;
                    for (int j = tr.desde; j <= tr.hasta && j < visto.nodos.Count; j++)
                    {
                        suma += visto.transform.TransformPoint(visto.nodos[j]);
                        cuantos++;
                    }
                    if (cuantos == 0)
                        continue;
                    string palabra = ConLado(tr.palabra);
                    string texto = palabra.Length == 0 ? "?" : Mostrar(palabra) + (tr.segura ? "" : "?");
                    Vector3 pos = suma / cuantos + arriba * 0.016f + haciaTi * 0.012f;
                    PonerChip(k++, t.id, tr.desde, tr.hasta, texto, tr.palabra, tr.segura, pos);
                }
            }
        }
        for (int i = k; i < chips.Count; i++)
            if (chips[i].t.gameObject.activeSelf)
                chips[i].t.gameObject.SetActive(false);
        if (opciones != null && opciones.gameObject.activeSelf && (k == 0 || !verEtiquetas))
            opciones.gameObject.SetActive(false);
        RefrescarVocabulario();
    }

    void PonerChip(int i, int id, int desde, int hasta, string texto, string palabra, bool segura, Vector3 pos)
    {
        while (chips.Count <= i)
            chips.Add(CrearChip(chips.Count));
        var c = chips[i];
        c.id = id;
        c.desde = desde;
        c.hasta = hasta;
        c.palabra = palabra;
        c.segura = segura;
        c.t.gameObject.SetActive(true);
        c.t.SetPositionAndRotation(pos, Quaternion.LookRotation(-haciaTi, arriba));
        c.texto.text = texto;
        // Lo que adivinó: clarito. Lo confirmado (o nombrado/dictado por ti): firme.
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
        if (c.segura)
        {
            dibujo.Mensaje(Idioma.T("Ya está confirmado: ") + Mostrar(ConLado(c.palabra)));
            return;
        }
        if (c.palabra.Length > 0)
            NombrarRango(c.id, c.desde, c.hasta, c.palabra);
    }

    void QuitarChip(Chip c)
    {
        var t = dibujo.BuscarPorId(c.id);
        if (t == null)
            return;
        if (c.segura)
        {
            for (int j = c.desde; j <= c.hasta && j < t.nodos.Count; j++)
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

    // ---------- El cuadrito = las 3 palabras más probables (un toque), el teclado o la voz ----------
    Transform opciones;
    readonly List<BotonTocable> botonesOpcion = new List<BotonTocable>();
    readonly List<string> palabrasOpcion = new List<string>();
    int idOpciones, desdeOpciones, hastaOpciones;

    void CambiarChip(Chip c)
    {
        var t = dibujo.BuscarPorId(c.id);
        if (t == null)
            return;
        pendiente = c.id;
        pendienteDesde = c.desde;
        pendienteHasta = c.hasta;
        idOpciones = c.id;
        desdeOpciones = c.desde;
        hastaOpciones = c.hasta;
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
        if (sugerencias.TryGetValue(c.id, out l))
            for (int k = 0; k < l.Count && palabrasOpcion.Count < 3; k++)
                palabrasOpcion.Add(l[k].Key);
        for (int k = 0; k < botonesOpcion.Count; k++)
        {
            bool teclado = k == botonesOpcion.Count - 1;
            bool ver = teclado || k < palabrasOpcion.Count;
            botonesOpcion[k].gameObject.SetActive(ver);
            botonesOpcion[k].etiqueta.gameObject.SetActive(ver);
            if (ver)
                botonesOpcion[k].PonerTexto(teclado ? "Teclado" : Mostrar(ConLado(palabrasOpcion[k])));
        }
        opciones.SetPositionAndRotation(c.t.position - arriba * 0.022f + haciaTi * 0.004f, c.t.rotation);
        opciones.gameObject.SetActive(true);
        dibujo.Mensaje("Toca una opción, abre el teclado o DILO con la voz (\"boca\", \"punta de nariz\"...)");
    }

    void ElegirOpcion(int k)
    {
        int id = idOpciones, a = desdeOpciones, b = hastaOpciones;
        opciones.gameObject.SetActive(false);
        if (k == botonesOpcion.Count - 1)
        {
            var t = dibujo.BuscarPorId(id);
            TecladoVR.Abrir("Nombre de esta parte (o dilo)", t != null ? t.NombreDeNodo(a) : "", nombre =>
            {
                if (pendiente == id)
                    pendiente = 0;
                if (!string.IsNullOrEmpty(nombre))
                    NombrarRango(id, a, b, nombre.ToLowerInvariant());
            });
            return;
        }
        if (pendiente == id)
            pendiente = 0;
        if (k < palabrasOpcion.Count)
            NombrarRango(id, a, b, palabrasOpcion[k]);
    }

    // Nombra los nodos desde..hasta de una línea (todos con ese nombre).
    void NombrarRango(int id, int desde, int hasta, string nombre)
    {
        var t = dibujo.BuscarPorId(id);
        if (t == null || t.nodos.Count == 0)
            return;
        desde = Mathf.Clamp(desde, 0, t.nodos.Count - 1);
        hasta = Mathf.Clamp(hasta, desde, t.nodos.Count - 1);
        for (int j = desde; j <= hasta; j++)
            t.NombrarNodo(j, nombre);
        sinEtiqueta.Remove(id);
        if (opciones != null)
            opciones.gameObject.SetActive(false);
        dibujo.HayCambios = true;
        dibujo.Mensaje(Idioma.T("Es: ") + Mostrar(ConLado(nombre)));
        AlCambiarNombres();
    }

    // Nombra una línea entera (los otros nombres se quitan).
    void NombrarLinea(int id, string nombre)
    {
        var t = dibujo.BuscarPorId(id);
        if (t == null)
            return;
        for (int j = 0; j < t.nodos.Count; j++)
            t.NombrarNodo(j, null);
        NombrarRango(id, 0, t.nodos.Count - 1, nombre);
    }

    void AlternarEtiquetas()
    {
        verEtiquetas = !verEtiquetas;
        if (vocabulario != null)
            vocabulario.gameObject.SetActive(verEtiquetas);
        sucio = true;
        dibujo.Mensaje(verEtiquetas ? "Etiquetas: Sí" : "Etiquetas: No");
    }

    // ---------- El vocabulario (a la derecha): dos dibujitos con sus zonas de colores y las palabras que sobran ----------
    // Tres maneras de usar una palabra:
    //  1. EL SELLO: TÓCALA con el índice (sin gesto en la izquierda): se queda pegada a tu dedo. Toca los NODOS (o una
    //     LÍNEA lejos de sus nodos = la línea entera) que son eso: se marcan en verde. Haz un PELLIZCO con la izquierda y
    //     suéltalo = confirmar. Tocar la palabra otra vez = cancelar.
    //  2. Con la izquierda en NODOS (medio + pulgar): toca la palabra, llévala pegada al índice al nodo y SUELTA la izquierda.
    //  3. Pellízcala con la derecha, llévala y suelta el pellizco sobre el nodo.
    struct ParteDibujo
    {
        public string palabra;
        public Color color;
        public Vector2[][] trazos;
    }

    static Vector2[] Arco(float cx, float cy, float rx, float ry, float desde, float hasta, int n)
    {
        var r = new Vector2[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float a = Mathf.Lerp(desde, hasta, i / (float)n) * Mathf.Deg2Rad;
            r[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
        }
        return r;
    }

    static Vector2[] Espejo(Vector2[] p)
    {
        var r = new Vector2[p.Length];
        for (int i = 0; i < p.Length; i++)
            r[i] = new Vector2(-p[i].x, p[i].y);
        return r;
    }

    static ParteDibujo PD(string palabra, Color color, params Vector2[][] trazos)
    {
        return new ParteDibujo { palabra = palabra, color = color, trazos = trazos };
    }

    static readonly Color ColAzul = new Color(0.2f, 0.45f, 0.95f), ColMorado = new Color(0.55f, 0.3f, 0.85f),
        ColLila = new Color(0.78f, 0.45f, 0.95f), ColVerde = new Color(0.2f, 0.65f, 0.3f), ColGris = new Color(0.3f, 0.3f, 0.36f),
        ColCafe = new Color(0.55f, 0.32f, 0.15f), ColNaranja = new Color(1f, 0.55f, 0.1f), ColRojo = new Color(0.9f, 0.2f, 0.2f),
        ColRosa = new Color(1f, 0.4f, 0.7f), ColCeleste = new Color(0.25f, 0.72f, 1f), ColTurquesa = new Color(0.1f, 0.62f, 0.66f);

    // La maqueta de frente (x de -1 a 1, y de -1 a 1).
    static List<ParteDibujo> DibujoFrente()
    {
        var mandibula = new[] { new Vector2(-0.8f, 0.05f), new Vector2(-0.76f, -0.35f), new Vector2(-0.58f, -0.68f), new Vector2(-0.3f, -0.88f) };
        var oreja = new[] { new Vector2(-0.8f, 0.02f), new Vector2(-0.96f, 0f), new Vector2(-0.98f, -0.18f), new Vector2(-0.9f, -0.32f), new Vector2(-0.79f, -0.33f) };
        var ceja = new[] { new Vector2(-0.55f, 0.32f), new Vector2(-0.35f, 0.4f), new Vector2(-0.15f, 0.33f) };
        return new List<ParteDibujo>
        {
            PD("cabeza arriba", ColAzul, Arco(0f, 0.05f, 0.8f, 0.95f, 0f, 180f, 16)),
            PD("mandíbula", ColMorado, mandibula, Espejo(mandibula)),
            PD("barbilla", ColLila, new[] { new Vector2(-0.3f, -0.88f), new Vector2(0f, -0.96f), new Vector2(0.3f, -0.88f) }),
            PD("oreja izquierda", ColVerde, oreja),
            PD("oreja derecha", new Color(0.45f, 0.8f, 0.3f), Espejo(oreja)),
            PD("ceja izquierda", ColGris, ceja),
            PD("ceja derecha", new Color(0.5f, 0.5f, 0.56f), Espejo(ceja)),
            PD("ojo izquierdo", ColCafe, Arco(-0.35f, 0.12f, 0.15f, 0.07f, 0f, 360f, 12)),
            PD("ojo derecho", new Color(0.75f, 0.5f, 0.25f), Arco(0.35f, 0.12f, 0.15f, 0.07f, 0f, 360f, 12)),
            PD("nariz", ColNaranja, new[] { new Vector2(0.02f, 0.08f), new Vector2(0.07f, -0.25f), new Vector2(-0.06f, -0.3f) }),
            PD("boca", ColRojo, new[] { new Vector2(-0.26f, -0.52f), new Vector2(0f, -0.58f), new Vector2(0.26f, -0.52f) }),
        };
    }

    // La maqueta de perfil (mirando a la derecha).
    static List<ParteDibujo> DibujoPerfil()
    {
        return new List<ParteDibujo>
        {
            PD("cabeza arriba", ColAzul, new[] { new Vector2(-0.72f, 0.15f), new Vector2(-0.62f, 0.62f), new Vector2(-0.25f, 0.92f),
                                                 new Vector2(0.25f, 0.92f), new Vector2(0.58f, 0.66f), new Vector2(0.68f, 0.38f) }),
            PD("frente", ColCeleste, new[] { new Vector2(0.68f, 0.38f), new Vector2(0.72f, 0.12f) }),
            PD("nariz", ColNaranja, new[] { new Vector2(0.72f, 0.12f), new Vector2(0.95f, -0.14f), new Vector2(0.72f, -0.22f) }),
            PD("boca exterior", ColRosa, new[] { new Vector2(0.72f, -0.22f), new Vector2(0.78f, -0.32f), new Vector2(0.7f, -0.4f),
                                                 new Vector2(0.76f, -0.48f), new Vector2(0.68f, -0.58f) }),
            PD("barbilla", ColLila, new[] { new Vector2(0.68f, -0.58f), new Vector2(0.66f, -0.76f), new Vector2(0.45f, -0.86f) }),
            PD("mandíbula", ColMorado, new[] { new Vector2(0.45f, -0.86f), new Vector2(0.05f, -0.76f), new Vector2(-0.22f, -0.42f) }),
            PD("nuca", ColTurquesa, new[] { new Vector2(-0.72f, 0.15f), new Vector2(-0.7f, -0.22f), new Vector2(-0.56f, -0.52f) }),
            PD("oreja", ColVerde, new[] { new Vector2(-0.08f, 0.06f), new Vector2(-0.24f, 0.12f), new Vector2(-0.32f, -0.08f),
                                          new Vector2(-0.25f, -0.3f), new Vector2(-0.1f, -0.3f) }),
            PD("ojo", ColCafe, Arco(0.48f, 0.04f, 0.08f, 0.05f, 0f, 360f, 10)),
            PD("ceja", ColGris, new[] { new Vector2(0.36f, 0.2f), new Vector2(0.5f, 0.25f), new Vector2(0.62f, 0.2f) }),
        };
    }

    // Todas las palabras (las de los dibujitos van arriba; aquí abajo, solo las que sobran).
    static readonly string[] TodasLasPalabras =
    {
        "punta de nariz", "puente de la nariz", "entrecejo", "párpado", "labio superior", "labio inferior", "comisura izquierda",
        "comisura derecha", "pómulo", "mejilla", "sien", "coronilla", "lóbulo", "cuello", "cabello", "frente", "nuca", "boca exterior",
        "ojo 1", "ojo 2", "ceja 1", "ceja 2", "oreja 1", "oreja 2"
    };

    class Entrada
    {
        public TextMeshPro t;
        public string palabra;
    }

    enum ModoPalabra { Nada, Pellizco, Nodos, Sello }
    ModoPalabra modoPalabra;
    Transform vocabulario, fondoVocab;
    TextMeshPro tituloVocab, tituloOtras, palabraFlotante;
    Transform marcaDestino;
    readonly List<Entrada> entradasDibujo = new List<Entrada>();
    readonly List<Entrada> entradasLista = new List<Entrada>();
    readonly HashSet<string> enDibujos = new HashSet<string>();
    bool tocandoLista, izqNodos, izqPellizcoVisto;
    readonly HashSet<long> selNodos = new HashSet<long>();
    readonly HashSet<int> selLineas = new HashSet<int>();
    string ultimoToque = "";
    string palabraEnMano = "";
    readonly List<Transform> puntos = new List<Transform>();
    const float AnchoVocab = 0.2f;
    float altoDibujos;

    Material MaterialLinea(Color c)
    {
        var baseM = control != null && control.materialAsa != null ? control.materialAsa : control != null ? control.materialNodo : null;
        var m = baseM != null ? new Material(baseM) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        materialesPropios.Add(m);
        return m;
    }

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
        tituloVocab = PanelTutoriales.Texto(vocabulario, "", new Vector3(0f, 0.004f, -0.001f), new Vector2(AnchoVocab - 0.01f, 0.01f), 0.08f, null);
        tituloVocab.fontStyle = FontStyles.Bold;
        Idioma.Poner(tituloVocab, "Toca una palabra (o arrástrala) a tu dibujo");
        // Los dos dibujitos: de frente (izquierda) y de perfil (derecha), cada zona de un color, con su leyenda debajo.
        float yLeyenda = ArmarDibujo(DibujoFrente(), -0.05f, "De frente");
        float yLeyenda2 = ArmarDibujo(DibujoPerfil(), 0.05f, "De perfil");
        altoDibujos = Mathf.Min(yLeyenda, yLeyenda2);
        tituloOtras = PanelTutoriales.Texto(vocabulario, "", new Vector3(0f, altoDibujos - 0.004f, -0.001f), new Vector2(AnchoVocab - 0.01f, 0.009f), 0.07f, null);
        tituloOtras.fontStyle = FontStyles.Bold;
        Idioma.Poner(tituloOtras, "Otras palabras");
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
    }

    // Un dibujito con sus líneas de colores y la leyenda (cada palabra de su color). Devuelve dónde termina (y).
    float ArmarDibujo(List<ParteDibujo> partes, float x, string titulo)
    {
        const float escala = 0.026f;
        float yCentro = -0.045f;
        var t = PanelTutoriales.Texto(vocabulario, "", new Vector3(x, -0.012f, -0.001f), new Vector2(0.09f, 0.008f), 0.06f, null);
        Idioma.Poner(t, titulo);
        t.color = new Color(0.4f, 0.4f, 0.46f);
        float y = yCentro - escala - 0.012f;
        foreach (var p in partes)
        {
            var mat = MaterialLinea(p.color);
            foreach (var tr in p.trazos)
            {
                var go = new GameObject("Maqueta");
                go.transform.SetParent(vocabulario, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.positionCount = tr.Length;
                for (int i = 0; i < tr.Length; i++)
                    lr.SetPosition(i, new Vector3(x + tr[i].x * escala, yCentro + tr[i].y * escala, -0.002f));
                lr.widthMultiplier = 0.0012f;
                lr.numCapVertices = 2;
                lr.sharedMaterial = mat;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
            }
            var e = PanelTutoriales.Texto(vocabulario, "", new Vector3(x, y, -0.001f), new Vector2(0.09f, 0.0085f), 0.06f, null);
            e.fontStyle = FontStyles.Bold;
            e.color = p.color;
            entradasDibujo.Add(new Entrada { t = e, palabra = p.palabra });
            enDibujos.Add(p.palabra);
            y -= 0.0105f;
        }
        return y;
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

    void RefrescarVocabulario()
    {
        if (vocabulario == null)
            return;
        var usadas = Usadas();
        // Las palabras de los dibujitos: las usadas quedan clarito; la que tienes en la mano, más grande.
        foreach (var e in entradasDibujo)
        {
            e.t.text = Mostrar(e.palabra);
            Color c = e.t.color;
            bool enMano = modoPalabra != ModoPalabra.Nada && palabraEnMano == e.palabra;
            c.a = enMano ? 1f : usadas.Contains(e.palabra) ? 0.35f : 1f;
            e.t.color = c;
            e.t.fontSizeMax = enMano ? 0.08f : 0.06f;
        }
        // Debajo, las que sobran (ni en los dibujitos ni usadas), en 3 columnas.
        var sobran = new List<string>();
        foreach (var w in TodasLasPalabras)
            if (!enDibujos.Contains(w) && !usadas.Contains(w))
                sobran.Add(w);
        while (entradasLista.Count < sobran.Count)
        {
            var t = PanelTutoriales.Texto(vocabulario, "", Vector3.zero, new Vector2(0.062f, 0.0095f), 0.06f, null);
            entradasLista.Add(new Entrada { t = t });
        }
        float y0 = altoDibujos - 0.016f;
        const float ancho = 0.064f;
        for (int i = 0; i < entradasLista.Count; i++)
        {
            var e = entradasLista[i];
            bool ver = i < sobran.Count;
            if (e.t.gameObject.activeSelf != ver)
                e.t.gameObject.SetActive(ver);
            if (!ver)
            {
                e.palabra = null;
                continue;
            }
            e.palabra = sobran[i];
            int col = i % 3, fila = i / 3;
            e.t.transform.localPosition = new Vector3(-ancho + col * ancho, y0 - fila * 0.011f, -0.001f);
            e.t.text = Mostrar(sobran[i]);
            bool enMano = modoPalabra != ModoPalabra.Nada && palabraEnMano == sobran[i];
            e.t.color = enMano ? new Color(0.15f, 0.65f, 0.3f) : new Color(0.2f, 0.2f, 0.28f);
        }
        int filas = Mathf.CeilToInt(sobran.Count / 3f);
        float abajo = y0 - filas * 0.011f - 0.006f;
        float alto = 0.012f - abajo;
        fondoVocab.localPosition = new Vector3(0f, 0.012f - alto * 0.5f, 0.004f);
        fondoVocab.localScale = new Vector3(AnchoVocab, alto, 0.003f);
    }

    void ColocarVocabulario()
    {
        if (vocabulario == null)
            return;
        Vector3 pos = Mundo(new Vector3(semiX + 0.15f, semiY, 0f), 0f) + haciaTi * 0.02f;
        vocabulario.SetPositionAndRotation(pos, Quaternion.LookRotation(-haciaTi, arriba));
        vocabulario.gameObject.SetActive(verEtiquetas);
    }

    // La palabra que está bajo ese punto (o null).
    string PalabraEn(Vector3 p, float radio)
    {
        string mejor = null;
        foreach (var e in entradasDibujo)
            if (e.t.gameObject.activeInHierarchy)
            {
                float d = Vector3.Distance(p, e.t.transform.position);
                if (d < radio)
                {
                    radio = d;
                    mejor = e.palabra;
                }
            }
        foreach (var e in entradasLista)
            if (e.palabra != null && e.t.gameObject.activeInHierarchy)
            {
                float d = Vector3.Distance(p, e.t.transform.position);
                if (d < radio)
                {
                    radio = d;
                    mejor = e.palabra;
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
            string w = PalabraEn(der.PuntoPellizco, 0.016f);
            if (w != null)
            {
                TomarPalabra(w, ModoPalabra.Pellizco, der.PuntoPellizco);
                return true;
            }
        }
        // Tocar una palabra con el índice: con la izquierda en Nodos = llevarla pegada al dedo; si no = el sello.
        string toca = der.pellizco ? null : PalabraEn(der.indice, 0.013f);
        if (toca != null && !tocandoLista)
        {
            tocandoLista = true;
            TomarPalabra(toca, nodosAhora ? ModoPalabra.Nodos : ModoPalabra.Sello, der.indice);
            return true;
        }
        if (toca == null)
            tocandoLista = false;
        return false;
    }

    void TomarPalabra(string palabra, ModoPalabra modo, Vector3 p)
    {
        modoPalabra = modo;
        palabraEnMano = palabra;
        palabraFlotante.text = Mostrar(ConLado(palabra));
        palabraFlotante.gameObject.SetActive(true);
        selNodos.Clear();
        selLineas.Clear();
        ultimoToque = "";
        izqPellizcoVisto = false;
        animando = false;
        RefrescarBotonGira();
        control.Ocupado = true;
        control.SonidoBurbuja(p);
        if (opciones != null)
            opciones.gameObject.SetActive(false);
        if (modo == ModoPalabra.Sello)
            dibujo.Mensaje(Idioma.T("Toca los nodos o las líneas y PELLIZCA con la izquierda para confirmar: ") + Mostrar(ConLado(palabra)));
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
        // Tocar la palabra otra vez (u otra palabra) en el vocabulario.
        string toca = PalabraEn(der.indice, 0.013f);
        if (toca != null)
        {
            if (!tocandoLista)
            {
                tocandoLista = true;
                if (toca == palabraEnMano)
                {
                    SoltarPalabra(false);
                    dibujo.Mensaje("Sello cancelado");
                }
                else
                {
                    palabraEnMano = toca;
                    palabraFlotante.text = Mostrar(ConLado(toca));
                    sucio = true;
                }
            }
            return;
        }
        tocandoLista = false;
        // ¿Qué toca el índice? Un nodo (ese nodo) o una línea lejos de sus nodos (la línea entera).
        Trazo t;
        int j;
        string toque;
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
                t.NombrarNodo(j, palabra);
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
            dibujo.Mensaje(Idioma.T("Es: ") + Mostrar(ConLado(palabra)));
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
        if (matPunto == null)
            matPunto = MaterialColor(new Color(0.55f, 0.55f, 0.6f));
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
        Vector3 donde = control != null ? (modo == ModoPalabra.Pellizco ? control.Der.PuntoPellizco : control.Der.indice) : Vector3.zero;
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
        dibujo.Mensaje(n > 0 ? Idioma.T("Es: ") + Mostrar(ConLado(palabra)) : Idioma.T("Suéltala sobre un nodo de tu dibujo (o elige nodos con el lazo)"));
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
        // Se mueve como los demás paneles (pellizco en una parte vacía, o nodos izquierda + índice derecho).
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
        // Se coloca SOLO al aparecer (no salta con cada flecha). Arriba a la izquierda de tu vista.
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

    // Al cambiar el idioma, TODO lo de la cabeza se reescribe.
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
            // v55: menos botones (nombrar = dictado, sello o etiquetas).
            Boton(filaPregunta, "De frente", new Vector3(-0.124f, 0f, 0f), 0.058f, false, () => Elegir(false));
            Boton(filaPregunta, "De perfil", new Vector3(-0.062f, 0f, 0f), 0.058f, false, () => Elegir(true));
            Boton(filaPregunta, "Etiquetas", new Vector3(0f, 0f, 0f), 0.058f, false, AlternarEtiquetas);
            Boton(filaPregunta, "Ayuda", new Vector3(0.062f, 0f, 0f), 0.058f, false, Ayuda);
            Boton(filaPregunta, "Cerrar", new Vector3(0.124f, 0f, 0f), 0.058f, false, Cerrar);
            string[] nombresVista = { "Perfil izq", "3/4 izq", "Frente", "3/4 der", "Perfil der" };
            float[] angulos = { -90f, -45f, 0f, 45f, 90f };
            for (int i = 0; i < nombresVista.Length; i++)
            {
                float a = angulos[i];
                Boton(filaGiro, nombresVista[i], new Vector3(-0.155f + i * 0.062f, 0.02f, 0f), 0.058f, false, () => Vista(a));
            }
            btnGira = Boton(filaGiro, "Gira", new Vector3(0.155f, 0.02f, 0f), 0.058f, true, AlternarGira);
            Boton(filaGiro, "Usar esta vista", new Vector3(-0.15f, -0.016f, 0f), 0.068f, true, Usar);
            Boton(filaGiro, "Guardar vista", new Vector3(-0.08f, -0.016f, 0f), 0.066f, false, GuardarVista);
            Boton(filaGiro, "Etiquetas", new Vector3(-0.014f, -0.016f, 0f), 0.058f, false, AlternarEtiquetas);
            Boton(filaGiro, "Ayuda", new Vector3(0.042f, -0.016f, 0f), 0.048f, false, Ayuda);
            Boton(filaGiro, "Nueva cabeza", new Vector3(0.1f, -0.016f, 0f), 0.06f, false, () => { Desesconder(); Empezar(); });
            Boton(filaGiro, "Cerrar", new Vector3(0.155f, -0.016f, 0f), 0.044f, false, Cerrar);
        }
        raiz.gameObject.SetActive(true);
        Vector3 pos = centro + arriba * (centro2D.y - semiY - 0.06f) + derecha * centro2D.x + haciaTi * 0.02f;
        panel.SetPositionAndRotation(pos, Quaternion.LookRotation(-haciaTi, arriba));
        ArmarVocabulario();
        ColocarVocabulario();
        RefrescarBotonGira();
        sucio = true;
    }

    BotonTocable Boton(List<GameObject> fila, string texto, Vector3 pos, float ancho, bool oscuro, UnityEngine.Events.UnityAction accion)
    {
        var b = PanelTutoriales.Boton(panel, "", pos, new Vector2(ancho, 0.028f), oscuro ? marcado : normal, oscuro ? normal : marcado);
        Idioma.Poner(b.etiqueta, texto);   // sigue al idioma de la app aunque lo cambies
        b.alTocar.AddListener(accion);
        if (oscuro)
        {
            b.etiqueta.color = Color.white;
            b.textoNormal = Color.white;
        }
        fila.Add(b.gameObject);
        fila.Add(b.etiqueta.gameObject);
        return b;
    }

    static void Mostrar(List<GameObject> fila, bool ver)
    {
        foreach (var g in fila)
            if (g != null)
                g.SetActive(ver);
    }

    // v55: "Gira": la cabeza gira sola de un perfil al otro (para probar sin agarrarla). Otra vez = parar.
    void AlternarGira()
    {
        if (estado != Estado.Girando)
            return;
        animando = !animando;
        animandoDesde = Time.time;
        if (!animando)
        {
            giro = 0f;
            mostrandoReal = true;
        }
        sucio = true;
        RefrescarBotonGira();
    }

    void RefrescarBotonGira()
    {
        if (btnGira != null)
            btnGira.PonerTexto(animando ? "Parar" : "Gira");
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

    void Vista(float angulo)
    {
        animando = false;
        RefrescarBotonGira();
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

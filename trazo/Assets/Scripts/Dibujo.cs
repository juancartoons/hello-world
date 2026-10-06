using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class DatosTrazo
{
    public int id;
    public int capa;
    public List<Vector3> nodos = new List<Vector3>();
    public List<Vector3> asaEntrada = new List<Vector3>();
    public List<Vector3> asaSalida = new List<Vector3>();
    public List<bool> asaManual = new List<bool>();
    public List<float> grosorNodo = new List<float>();
    public bool cerrado;
    public bool relleno;
    public int colorRelleno;
    public float ancho = 0.008f;
    public int estilo;
    public bool crudo; // solo para la repetición: la línea aún se estaba dibujando
    public bool oculto; // línea "hueso": existe (mueve a otras partes) pero no se ve
    public Color color = Color.black; // color de la línea (los dibujos de antes no lo tienen: negro)
    public bool rellenoAbierto;       // rellena aunque las puntas no se unan (la cubeta)
    public Color colorFondo;          // color del relleno elegido en la paleta (transparente = el de siempre)
}

[System.Serializable]
public class DatosCapa
{
    public string nombre = "Capa";
    public bool visible = true;
    // Líneas vivas de esta capa
    public int temblor;              // 0 No, 1 Suave, 2 Medio, 3 Fuerte
    public int hebras = 1;           // 1, 3 o 5
    public float grosorHebra = 0.5f; // grosor de cada hebra (veces el de la línea)
    public bool grosorVivo;
    public bool ciclo3 = true;
    public int suavidad = 1;         // 0 Suave, 1 Normal, 2 Nervioso
    public int velocidad = 1;        // índice en Dibujo.Velocidades
    public int boceto;               // 0 no, 1 lápiz gris, 2 azul (no sale en fotos ni videos)
    public bool iman = true;         // las puntas se pegan a otras líneas y las figuras se cierran solas
    // Plano 2D de esta capa
    public bool hayPlano;
    public Vector3 planoPunto;
    public Vector3 planoNormal = Vector3.forward;
    public bool unido = true;        // unido con los planos de las otras capas (como acetato sobre papel)

    public DatosCapa Copia()
    {
        return (DatosCapa)MemberwiseClone();
    }
}

// Una forma de boca de la biblioteca (para el lipsync): la forma de las líneas de la boca.
[System.Serializable]
public class PoseBoca
{
    public string nombre;
    public List<DatosTrazo> trazos = new List<DatosTrazo>();
}

// Una clave de animación: la forma de todas las líneas en un fotograma.
[System.Serializable]
public class Clave
{
    public int fotograma;
    public List<DatosTrazo> trazos = new List<DatosTrazo>();
    public int capa = -1; // la capa de esta clave (-1 = animación de antes: una clave para todo el dibujo)
    // Copia en texto (para deshacer rápido). Se borra cada vez que la clave cambia.
    [System.NonSerialized] public string cache;
}

// Una "foto" para deshacer: el dibujo (sin la animación) y cada clave por separado.
// Las claves que no cambiaron se comparten entre fotos (no se copian otra vez).
public class FotoDeshacer
{
    public string datos;
    public List<string> claves;
}

// Todo lo que se guarda de un dibujo (archivo .json). También sirve para "Deshacer".
[System.Serializable]
public class ArchivoIncluido
{
    public string nombre;  // nombre del archivo (imagen o audio)
    public string tipo;    // "imagen" o "audio"
    public string datos;   // el archivo completo, en texto (base64)
}

// Todo lo que se guarda de un dibujo. El proyecto (.jc) lleva además adentro las imágenes de referencia y el audio.
[System.Serializable]
public class DatosDibujo
{
    public int version = 6;
    public bool conReferencias;                                   // true = el proyecto trae sus imágenes
    public List<DatosImagen> referencias = new List<DatosImagen>();
    public List<ArchivoIncluido> incluidos = new List<ArchivoIncluido>();
    public string nombre = "";   // nombre del archivo ("Dibujo 3"); vacío = dibujo nuevo sin guardar
    public List<DatosTrazo> trazos = new List<DatosTrazo>();
    public int fondo;
    public float anchoPincel = 0.008f;
    public Vector3 posicion;
    public Quaternion rotacion = Quaternion.identity;
    public float escala = 1f;
    public bool plano;
    public bool hayPlano;
    public Vector3 planoPunto;
    public Vector3 planoNormal = Vector3.forward;
    public List<DatosCapa> capas = new List<DatosCapa>();
    public int capaActual;
    public int siguienteId = 1;
    public List<Clave> claves = new List<Clave>();
    public int fotograma;
    public float fps = 12f;
    public List<PoseBoca> bocas = new List<PoseBoca>();
    public string audio = "";
    public List<DatosFigura> figuras = new List<DatosFigura>();
    public List<TrazoLapiz> lapiz = new List<TrazoLapiz>();
    public int temblor;
    public bool pincelElegido;
    public int titerePierna1;
    public int titerePierna2;
    public List<int> titereCuerpo = new List<int>();
    public bool titereVoltear;
    public int titereBrazo1;
    public int titereBrazo2;
    public int titereCabeza;
    public int titereCiclo;
    public List<int> titerePisos = new List<int>();
    public List<DatosPersonaje> personajes = new List<DatosPersonaje>();
    public int titereElegido;
    public int temblorHebras = 1;
    public bool temblorGrosor;
    public bool temblorCiclo = true;
}

// El dibujo completo: crea las líneas, une, cierra, borra, deshace, guarda y carga.
// Las líneas son hijas de este objeto, así se puede mover, girar y escalar todo junto.
public class Dibujo : MonoBehaviour
{
    public const int NumeroDeCapas = 4;

    public Material materialLinea;
    public Material materialRelleno;
    public Material materialGuia;
    [Tooltip("Color de la línea seleccionada")]
    public Material materialSeleccion;
    [Tooltip("Rojo del borrador")]
    public Material materialBorrado;
    [Tooltip("Líneas de una capa de boceto: gris lápiz")]
    public Material materialBocetoGris;
    [Tooltip("Líneas de una capa de boceto: azul")]
    public Material materialBocetoAzul;
    public Figuras figuras;
    public HojasLapiz hojas;
    public Animacion animacion;
    public Lipsync lipsync;
    public Temblor temblor;
    public Titere titere;
    [Tooltip("Grosor máximo (en el centro) de las líneas nuevas, en metros")]
    public float anchoPincel = 0.008f;
    [Tooltip("true = las líneas nuevas usan el grosor elegido con el dial; false = el promedio de las que hay")]
    public bool pincelElegido;
    [Tooltip("Dibujar sobre un plano (2D) en vez de libre en 3D")]
    public bool plano;
    [Tooltip("Distancia (metros) a la que las puntas se pegan como imán")]
    public float radioIman = 0.025f;
    public Escenario escenario;
    [Tooltip("Al abrir la app, recupera el último dibujo (autoguardado)")]
    public bool cargarAlIniciar = true;

    public readonly List<Trazo> trazos = new List<Trazo>();
    public readonly List<DatosCapa> capas = new List<DatosCapa>();
    public int capaActual;
    public event System.Action alCambiar;
    public event System.Action<string> alMensaje;

    public bool HayPlano { get; private set; }
    Vector3 planoPunto;
    Vector3 planoNormal = Vector3.forward;
    GameObject guia;
    int siguienteId = 1;
    Trazo seleccion;
    Trazo grosorSolo;
    float anchoSoloInicio;

    const int maxHistorial = 40;
    readonly List<FotoDeshacer> historial = new List<FotoDeshacer>();
    readonly List<FotoDeshacer> rehacer = new List<FotoDeshacer>();
    readonly List<FotoDeshacer> rehacerRespaldo = new List<FotoDeshacer>();
    readonly List<float> anchosInicio = new List<float>();
    float pincelInicio;

    // Destellos rojos del borrador
    readonly List<Transform> destellos = new List<Transform>();
    readonly List<float> destellosFin = new List<float>();
    readonly List<float> destellosTam = new List<float>();

    string Carpeta => Path.Combine(Application.persistentDataPath, "Dibujos");
    string RutaAuto => Path.Combine(Carpeta, "autoguardado.json");
    string RutaGuardado => Path.Combine(Carpeta, "guardado.json");
    // Cada dibujo guardado con su propio nombre (Dibujo 1, Dibujo 2...).
    string CarpetaArchivos => Path.Combine(Carpeta, "Archivos");
    public string NombreArchivo { get; private set; } = "";

    public float EscalaMundo => Mathf.Max(0.0001f, transform.lossyScale.x);
    public float RadioImanLocal => radioIman / EscalaMundo;
    public bool PlanoActivo => plano && HayPlano;

    // Opciones de las líneas vivas (por capa).
    public static readonly float[] AmplitudesTemblor = { 0f, 0.0015f, 0.003f, 0.006f };
    public static readonly float[] Frecuencias = { 5f, 9f, 16f };
    public static readonly string[] NombresSuavidad = { "Suave", "Normal", "Nervioso" };
    public static readonly float[] Velocidades = { 4f, 8f, 12f, 24f };
    public const float SeparacionHebras = 1.2f;
    public const float SeparacionPlanos = 0.0005f; // metros entre planos unidos (medio milímetro: como acetato)
    bool AnimacionActiva => animacion != null && animacion.Activa;

    void Awake()
    {
        AsegurarCapas();
        if (animacion == null)
            animacion = GetComponent<Animacion>();
        Trazo.estiloCapa = EstiloDe;
        if (temblor == null)
            temblor = GetComponent<Temblor>();
        if (temblor == null)
            temblor = gameObject.AddComponent<Temblor>();
        if (titere == null)
            titere = GetComponent<Titere>();
        if (figuras == null)
            figuras = GetComponent<Figuras>();
        if (hojas == null)
            hojas = GetComponent<HojasLapiz>();
    }

    void Start()
    {
        if (cargarAlIniciar)
        {
            var d = Leer(RutaAuto);
            if (d != null)
                Aplicar(d, true);
        }
        ActualizarGuia();
        Avisar();
    }

    void Update()
    {
        // Autoguardado del proyecto actual cada 3 minutos (si tiene nombre y hubo cambios).
        if (Time.time > proximoAutoguardado)
        {
            proximoAutoguardado = Time.time + 180f;
            if (HayCambios && !string.IsNullOrEmpty(NombreArchivo) && !Titere.Activo && !ExportadorVideo.Exportando
                && (animacion == null || !animacion.Reproduciendo))
                GuardarProyecto(true);
        }
        // Los destellos rojos se encogen y desaparecen.
        for (int i = 0; i < destellos.Count; i++)
        {
            var d = destellos[i];
            if (d == null || !d.gameObject.activeSelf)
                continue;
            float resto = destellosFin[i] - Time.time;
            if (resto <= 0f)
            {
                d.gameObject.SetActive(false);
                continue;
            }
            d.localScale = Vector3.one * destellosTam[i] * Mathf.Clamp01(resto / 0.25f);
        }
    }

    void OnApplicationPause(bool pausa)
    {
        if (pausa)
            Escribir(RutaAuto, JsonUtility.ToJson(DatosParaGuardar()));
    }

    void OnApplicationQuit()
    {
        Escribir(RutaAuto, JsonUtility.ToJson(DatosParaGuardar()));
    }

    // Lo que se guarda: el dibujo de verdad (durante el tutorial, el que quedó apartado, no las líneas del tutorial).
    DatosDibujo DatosParaGuardar()
    {
        if (apartado == null)
            return CrearDatos();
        var d = JsonUtility.FromJson<DatosDibujo>(apartado.datos);
        if (d == null)
            return CrearDatos();
        if (apartado.claves != null)
            foreach (var texto in apartado.claves)
            {
                var c = JsonUtility.FromJson<Clave>(texto);
                if (c != null)
                    d.claves.Add(c);
            }
        return d;
    }

    // ---------- Capas ----------

    void AsegurarCapas()
    {
        while (capas.Count < NumeroDeCapas)
            capas.Add(new DatosCapa { nombre = "Capa " + (capas.Count + 1), visible = true });
        capaActual = Mathf.Clamp(capaActual, 0, capas.Count - 1);
    }

    public DatosCapa DatosDeCapa(int capa)
    {
        AsegurarCapas();
        return capas[Mathf.Clamp(capa, 0, capas.Count - 1)];
    }

    public DatosCapa CapaActual => DatosDeCapa(capaActual);

    // La capa de boceto en Plano (2D) dibuja con lápiz sobre una hoja (como papel).
    public bool UsaHoja => hojas != null && plano && EsBoceto(capaActual);

    public bool EsBoceto(int capa)
    {
        return capa >= 0 && capa < capas.Count && capas[capa].boceto > 0;
    }

    // Cómo se ven las líneas vivas de una capa (lo usa Trazo al armar su malla).
    public Trazo.EstiloVivo EstiloDe(int capa)
    {
        var c = DatosDeCapa(capa);
        var e = new Trazo.EstiloVivo();
        e.hebras = Mathf.Clamp(c.hebras, 1, 5);
        e.grosorHebra = e.hebras > 1 ? Mathf.Clamp(c.grosorHebra, 0.15f, 1f) : 1f;
        e.a = new Vector4(
            AmplitudesTemblor[Mathf.Clamp(c.temblor, 0, AmplitudesTemblor.Length - 1)],
            e.hebras > 1 ? SeparacionHebras : 0f,
            c.grosorVivo ? 1f : 0f,
            Velocidades[Mathf.Clamp(c.velocidad, 0, Velocidades.Length - 1)]);
        e.b = new Vector4(c.ciclo3 ? 1f : 0f, Frecuencias[Mathf.Clamp(c.suavidad, 0, Frecuencias.Length - 1)], 0f, 0f);
        return e;
    }

    // Material de la línea según su capa (boceto gris o azul, o tinta normal).
    public Material MaterialDe(Trazo t)
    {
        if (t == null)
            return materialLinea;
        var m = MaterialCapa(t.capa, materialLinea);
        // Boceto (gris o azul) manda; si no, la línea lleva su color.
        if (m != materialLinea || EsNegro(t.color))
            return m;
        // Tinta invisible: no se ve; mientras editas se muestra gris clarito.
        if (t.Invisible)
            return MaterialColor(ColorInvisibleEditando);
        return MaterialColor(t.color);
    }

    // ---------- Colores de las líneas ----------

    // El color de las líneas nuevas (lo elige la paleta de la mano izquierda). Transparente = tinta invisible.
    public Color ColorNuevo { get; set; } = Color.black;
    public static readonly Color TintaInvisible = new Color(0f, 0f, 0f, 0f);
    static readonly Color ColorInvisibleEditando = new Color(0.72f, 0.74f, 0.8f, 1f);

    // Con la paleta abierta o editando (nodos, borrador...), las líneas de tinta invisible se ven gris clarito.
    public void VerInvisibles(bool ver)
    {
        if (Trazo.verInvisibles == ver)
            return;
        Trazo.verInvisibles = ver;
        foreach (var t in trazos)
            if (t != null && t.Invisible)
                t.Reconstruir(true);
    }

    // La cubeta: rellena una forma (aunque esté abierta) con un color. Con tinta invisible = quita el relleno.
    public void RellenarConColor(Trazo t, Color c)
    {
        if (t == null)
            return;
        GuardarParaDeshacer();
        if (c.a < 0.01f)
        {
            t.relleno = false;
            t.rellenoAbierto = false;
        }
        else
        {
            if (!t.cerrado)
                t.rellenoAbierto = true;
            t.relleno = true;
            t.colorFondo = new Color(c.r, c.g, c.b, 1f);
        }
        t.Reconstruir();
        Trazo.huboCambio = false;
        HayCambios = true;
        Avisar();
    }

    readonly Dictionary<int, Material> materialesColor = new Dictionary<int, Material>();

    static bool EsNegro(Color c)
    {
        return c.r < 0.02f && c.g < 0.02f && c.b < 0.02f;
    }

    // Un material de línea de ese color (se hace una sola vez por color).
    public Material MaterialColor(Color c)
    {
        if (materialLinea == null)
            return null;
        Color32 c32 = c;
        int clave = (c32.r << 24) | (c32.g << 16) | (c32.b << 8) | c32.a;
        Material m;
        if (materialesColor.TryGetValue(clave, out m) && m != null)
            return m;
        m = new Material(materialLinea);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_ColorLuz"))
            m.SetColor("_ColorLuz", Color.Lerp(c, Color.white, 0.45f));
        materialesColor[clave] = m;
        return m;
    }

    // Pinta una línea (se puede deshacer si antes se guardó "deshacer").
    public void PonerColor(Trazo t, Color c)
    {
        if (t == null)
            return;
        t.color = c;
        RestaurarMaterial(t);
        HayCambios = true;
        Avisar();
    }

    public Material MaterialCapa(int capa, Material normal)
    {
        if (capa >= 0 && capa < capas.Count)
        {
            int b = capas[capa].boceto;
            if (b == 1 && materialBocetoGris != null) return materialBocetoGris;
            if (b == 2 && materialBocetoAzul != null) return materialBocetoAzul;
        }
        return normal;
    }

    // Capa de Unity (lo que ven las fotos y videos) y color de la línea según su capa.
    public void AplicarCapaVisual(Trazo t)
    {
        if (t == null)
            return;
        int capaUnity = EsBoceto(t.capa) ? 0 : gameObject.layer;
        if (t.gameObject.layer != capaUnity)
            foreach (var hijo in t.GetComponentsInChildren<Transform>(true))
                hijo.gameObject.layer = capaUnity;
        t.PonerMaterialLinea(EstaSeleccionada(t) && materialSeleccion != null ? materialSeleccion : MaterialDe(t));
    }

    // Vuelve a armar las líneas (y figuras) de una capa después de cambiar su estilo.
    public void RefrescarCapa(int capa)
    {
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        foreach (var t in trazos)
        {
            if (t == null || t.capa != capa)
                continue;
            AplicarCapaVisual(t);
            t.Reconstruir(false);
        }
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
        if (figuras != null)
            figuras.RefrescarCapa(capa);
        Avisar();
    }

    public bool CapaVisible(int capa)
    {
        return capa < 0 || capa >= capas.Count || capas[capa].visible;
    }

    public void SeleccionarCapa(int capa)
    {
        AsegurarCapas();
        capaActual = Mathf.Clamp(capa, 0, capas.Count - 1);
        if (!capas[capaActual].visible)
        {
            capas[capaActual].visible = true;
            ActualizarVisibilidad();
        }
        RefrescarPlano();
        ActualizarGuia();
        Avisar();
        Mensaje("Dibujas en " + capas[capaActual].nombre);
    }

    public void AlternarVerCapa(int capa)
    {
        AsegurarCapas();
        if (capa < 0 || capa >= capas.Count)
            return;
        capas[capa].visible = !capas[capa].visible;
        ActualizarVisibilidad();
        Avisar();
        Mensaje(capas[capa].nombre + (capas[capa].visible ? " visible" : " oculta"));
    }

    // Una línea se ve si su capa está visible y si existe en el fotograma actual.
    public void ActualizarVisibilidad()
    {
        foreach (var t in trazos)
        {
            if (t == null)
                continue;
            bool ver = t.visibleAnim && CapaVisible(t.capa);
            if (t.gameObject.activeSelf != ver)
                t.gameObject.SetActive(ver);
        }
    }

    // Solo se pueden tocar/editar las líneas que se ven.
    public static bool Editable(Trazo t)
    {
        return t != null && t.gameObject.activeSelf;
    }

    // ---------- Selección ----------

    // La línea seleccionada (null = ninguna: los cambios afectan a todo el dibujo).
    public Trazo Seleccion => Editable(seleccion) ? seleccion : null;

    // Selección múltiple: además de "seleccion", más líneas elegidas con toques cortos.
    readonly List<Trazo> grupo = new List<Trazo>();

    public bool EstaSeleccionada(Trazo t)
    {
        return t != null && (t == seleccion || grupo.Contains(t));
    }

    // Todas las líneas seleccionadas (la principal primero).
    public List<Trazo> Seleccionadas()
    {
        var l = new List<Trazo>();
        if (Editable(seleccion))
            l.Add(seleccion);
        foreach (var g in grupo)
            if (Editable(g) && g != seleccion && !l.Contains(g))
                l.Add(g);
        return l;
    }

    // Suma una línea a la selección (o la quita si ya estaba).
    public void AlternarEnGrupo(Trazo t)
    {
        if (t == null)
            return;
        if (seleccion == null)
        {
            Seleccionar(t);
            return;
        }
        if (t == seleccion)
        {
            t.PonerMaterialLinea(MaterialDe(t));
            seleccion = null;
            if (grupo.Count > 0)
            {
                seleccion = grupo[0];
                grupo.RemoveAt(0);
            }
        }
        else if (grupo.Remove(t))
        {
            t.PonerMaterialLinea(MaterialDe(t));
        }
        else
        {
            grupo.Add(t);
            t.PonerMaterialLinea(materialSeleccion != null ? materialSeleccion : materialLinea);
        }
    }

    void LimpiarGrupo()
    {
        foreach (var g in grupo)
            if (g != null && g != seleccion)
                g.PonerMaterialLinea(MaterialDe(g));
        grupo.Clear();
    }

    public void Seleccionar(Trazo t)
    {
        LimpiarGrupo();
        if (seleccion == t)
            return;
        var anterior = seleccion;
        seleccion = t;
        if (anterior != null)
            anterior.PonerMaterialLinea(MaterialDe(anterior));
        if (seleccion != null)
            seleccion.PonerMaterialLinea(materialSeleccion != null ? materialSeleccion : materialLinea);
    }

    // ---------- Dibujar ----------

    public Trazo NuevoTrazo()
    {
        GuardarParaDeshacer();
        AsegurarCapas();
        if (!capas[capaActual].visible)
        {
            capas[capaActual].visible = true;
            ActualizarVisibilidad();
        }
        var t = CrearTrazo(AnchoNuevoLocal());
        t.id = siguienteId++;
        t.capa = capaActual;
        t.color = ColorNuevo;
        AplicarCapaVisual(t);
        trazos.Add(t);
        return t;
    }

    // Grosor de las líneas nuevas: el promedio de las líneas que se ven (así siempre combinan,
    // aunque hayas agrandado o achicado todo). Si no hay líneas, el del pincel.
    public float AnchoNuevoLocal()
    {
        if (pincelElegido)
            return anchoPincel / EscalaMundo;
        float suma = 0f;
        int cuenta = 0;
        foreach (var t in trazos)
        {
            if (!Editable(t) || t.Dibujando)
                continue;
            suma += t.ancho;
            cuenta++;
        }
        return cuenta > 0 ? suma / cuenta : anchoPincel / EscalaMundo;
    }

    public float AnchoNuevoMundo => AnchoNuevoLocal() * EscalaMundo;

    // El dial de grosor: las líneas nuevas salen con este grosor (en metros).
    public void ElegirAnchoPincel(float mundo)
    {
        anchoPincel = Mathf.Clamp(mundo, 0.001f, 0.06f);
        pincelElegido = true;
    }

    // Agrega una línea ya hecha (por ejemplo, el muñeco de prueba). Guarda "deshacer" antes de llamarla.
    public Trazo AgregarTrazo(DatosTrazo d)
    {
        AsegurarCapas();
        if (d.ancho <= 0f)
            d.ancho = AnchoNuevoLocal();
        var t = CrearTrazo(d.ancho);
        t.id = siguienteId++;
        t.capa = capaActual;
        t.color = d.color;
        AplicarCapaVisual(t);
        t.AplicarPose(d, null, 0f);
        trazos.Add(t);
        ActualizarVisibilidad();
        Avisar();
        return t;
    }

    public Trazo BuscarPorId(int id)
    {
        if (id <= 0)
            return null;
        foreach (var t in trazos)
            if (t != null && t.id == id)
                return t;
        return null;
    }
    // Termina la línea; si su final toca su inicio se cierra, y si toca otra línea se une a ella.
    public void TerminarTrazo(Trazo t)
    {
        if (t == null)
            return;
        if (!t.Terminar())
        {
            QuitarDeLaLista(t);
            DescartarUltimoDeshacer();
            return;
        }
        if (!CapaActual.iman && !t.cerrado)
        {
            // Imán apagado: la línea queda tal como la dibujaste.
            Avisar();
            return;
        }
        int n = t.nodos.Count;
        float iman = RadioImanLocal;
        if (n >= 4 && Vector3.Distance(t.nodos[0], t.nodos[n - 1]) < iman && t.Largo > iman * 4f)
        {
            if (t.Cerrar(true))
                Mensaje("Figura cerrada");
        }
        else
        {
            Trazo otro;
            int extremo;
            if (BuscarExtremo(t.nodos[t.nodos.Count - 1], t, out otro, out extremo))
                Unir(t, 1, otro, extremo);
            if (BuscarExtremo(t.nodos[0], t, out otro, out extremo))
                Unir(t, 0, otro, extremo);
            // Si al unirse las puntas quedaron juntas (por ejemplo, un triángulo de rectas), se cierra.
            int m = t.nodos.Count;
            if (!t.cerrado && m >= 4 && Vector3.Distance(t.nodos[0], t.nodos[m - 1]) < iman)
            {
                if (t.Cerrar(true))
                    Mensaje("Figura cerrada");
            }
        }
        Avisar();
    }

    // Quita una línea que se estaba dibujando (por ejemplo, al pasar a girar/escalar).
    public void CancelarTrazo(Trazo t)
    {
        if (t == null)
            return;
        QuitarDeLaLista(t);
        DescartarUltimoDeshacer();
        Trazo.huboCambio = false;
    }

    Trazo CrearTrazo(float ancho)
    {
        var go = new GameObject("Trazo");
        go.layer = gameObject.layer; // la capa del dibujo: es lo que ven las fotos y los videos
        go.transform.SetParent(transform, false);
        var t = go.AddComponent<Trazo>();
        t.Configurar(materialLinea, materialRelleno, ancho, EstiloLinea.Cinta);
        return t;
    }

    void QuitarDeLaLista(Trazo t)
    {
        trazos.Remove(t);
        if (t != null)
            Destroy(t.gameObject);
    }

    // ---------- Editar ----------

    public void MoverNodo(Trazo t, int indice, Vector3 posicionLocal)
    {
        if (t == null)
            return;
        t.MoverNodo(indice, posicionLocal);
    }

    public void QuitarNodo(Trazo t, int indice)
    {
        if (t == null)
            return;
        int antes = t.nodos.Count;
        t.QuitarNodo(indice);
        if (animacion != null)
            animacion.QuitarNodoEnClaves(t.id, indice, antes);
        if (t.nodos.Count < 2)
            Desaparecer(t, false);
        Avisar();
    }

    // Agrega un nodo en la línea (y en todas las claves de la animación).
    public void InsertarNodo(Trazo t, int segmento, float posicion)
    {
        if (t == null)
            return;
        int antes = t.nodos.Count;
        t.InsertarNodo(segmento, posicion);
        if (animacion != null)
            animacion.InsertarNodoEnClaves(t.id, segmento, posicion, antes);
        Avisar();
    }

    // Vuelve a poner el color normal de la línea (azul si está seleccionada).
    public void RestaurarMaterial(Trazo t)
    {
        if (t != null)
            t.PonerMaterialLinea(EstaSeleccionada(t) && materialSeleccion != null ? materialSeleccion : MaterialDe(t));
    }

    // Quita la línea por completo (también de la animación). Lo usa la X de los personajes.
    public void EliminarDelTodo(Trazo t)
    {
        if (t == null)
            return;
        if (seleccion == t)
            Seleccionar(null);
        grupo.Remove(t);
        QuitarDeLaLista(t);
        Avisar();
    }

    public void BorrarTrazo(Trazo t, bool conDestello)
    {
        if (t == null)
            return;
        Desaparecer(t, conDestello);
        Avisar();
    }

    // Con animación, la línea solo se esconde desde este fotograma; sin animación, se elimina.
    void Desaparecer(Trazo t, bool conDestello)
    {
        bool esconder = animacion != null && animacion.CapaAnimada(t.capa);
        if (esconder)
        {
            t.visibleAnim = false;
            Trazo.huboCambio = true;
            Trazo.cambiadosAnim.Add(t);
        }
        else
        {
            trazos.Remove(t);
        }
        if (conDestello && materialBorrado != null && t.gameObject.activeSelf)
        {
            StartCoroutine(DestelloRojo(t, !esconder));
            return;
        }
        if (esconder)
            ActualizarVisibilidad();
        else
            Destroy(t.gameObject);
    }

    IEnumerator DestelloRojo(Trazo t, bool destruir)
    {
        var renderers = t.GetComponentsInChildren<MeshRenderer>();
        var originales = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originales[i] = renderers[i].sharedMaterial;
            renderers[i].sharedMaterial = materialBorrado;
        }
        yield return new WaitForSeconds(0.12f);
        if (t == null)
            yield break;
        if (destruir)
        {
            Destroy(t.gameObject);
            yield break;
        }
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].sharedMaterial = originales[i];
        ActualizarVisibilidad();
    }

    // Pequeño destello rojo (al borrar un nodo o un relleno).
    public void Destello(Vector3 mundo, float tamano)
    {
        if (materialBorrado == null)
            return;
        int libre = -1;
        for (int i = 0; i < destellos.Count; i++)
            if (destellos[i] != null && !destellos[i].gameObject.activeSelf)
            {
                libre = i;
                break;
            }
        if (libre < 0)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Destello";
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = materialBorrado;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            destellos.Add(go.transform);
            destellosFin.Add(0f);
            destellosTam.Add(0f);
            libre = destellos.Count - 1;
        }
        var d = destellos[libre];
        d.gameObject.SetActive(true);
        d.position = mundo;
        d.localScale = Vector3.one * tamano;
        destellosFin[libre] = Time.time + 0.25f;
        destellosTam[libre] = tamano;
    }

    public void CerrarTrazo(Trazo t, bool quitarUltimo)
    {
        if (t != null && t.Cerrar(quitarUltimo))
            Mensaje("Figura cerrada");
        Avisar();
    }

    // Busca la punta de otra línea abierta (y visible) cerca de un punto (local).
    public bool BuscarExtremo(Vector3 local, Trazo excluir, out Trazo encontrado, out int extremo)
    {
        encontrado = null;
        extremo = -1;
        float mejor = RadioImanLocal;
        foreach (var o in trazos)
        {
            if (!Editable(o) || o == excluir || o.cerrado || o.nodos.Count < 2)
                continue;
            float d0 = Vector3.Distance(local, o.nodos[0]);
            if (d0 < mejor)
            {
                mejor = d0;
                encontrado = o;
                extremo = 0;
            }
            float d1 = Vector3.Distance(local, o.nodos[o.nodos.Count - 1]);
            if (d1 < mejor)
            {
                mejor = d1;
                encontrado = o;
                extremo = 1;
            }
        }
        return encontrado != null;
    }

    // Une la punta "extremoA" de A (0 inicio, 1 final) con la punta "extremoB" de B. B desaparece.
    public void Unir(Trazo a, int extremoA, Trazo b, int extremoB)
    {
        if (a == null || b == null || a == b || a.cerrado || b.cerrado)
            return;
        a.AsegurarAsas();
        b.AsegurarAsas();
        if (extremoA == 0)
            a.Invertir();
        if (extremoB == 1)
            b.Invertir();
        int u = a.nodos.Count - 1;
        a.nodos[u] = b.nodos[0];
        a.asaSalida[u] = b.asaSalida[0];
        a.asaManual[u] = false;
        a.grosorNodo[u] = Mathf.Max(a.grosorNodo[u], b.grosorNodo[0]);
        for (int i = 1; i < b.nodos.Count; i++)
        {
            a.nodos.Add(b.nodos[i]);
            a.asaEntrada.Add(b.asaEntrada[i]);
            a.asaSalida.Add(b.asaSalida[i]);
            a.asaManual.Add(b.asaManual[i]);
            a.grosorNodo.Add(b.grosorNodo[i]);
        }
        a.ancho = Mathf.Max(a.ancho, b.ancho);
        QuitarDeLaLista(b);
        a.Reconstruir();
        Mensaje("Líneas unidas");
        Avisar();
    }

    // Tocar un relleno: si no tiene color lo pinta; si ya tiene, pasa al siguiente color.
    public void CambiarColorRelleno(Trazo t)
    {
        if (t == null || (!t.cerrado && !t.rellenoAbierto))
            return;
        GuardarParaDeshacer();
        if (!t.relleno)
            t.relleno = true;
        else if (t.colorFondo.a > 0.01f)
            t.colorFondo = new Color(0f, 0f, 0f, 0f); // tenía un color de la paleta: vuelve a los de siempre
        else
            t.colorRelleno = (t.colorRelleno + 1) % Trazo.Paleta.Length;
        t.Reconstruir();
        Avisar();
    }

    public void QuitarRelleno(Trazo t)
    {
        if (t == null)
            return;
        t.relleno = false;
        t.rellenoAbierto = false;
        t.Reconstruir();
        Avisar();
    }

    // ---------- Plano (dibujo 2D) ----------

    public void AlternarPlano()
    {
        plano = !plano;
        // El plano de esta capa se vuelve a definir con la próxima línea.
        CapaActual.hayPlano = false;
        RefrescarPlano();
        ActualizarGuia();
        Avisar();
        Mensaje(plano ? "Plano: tu próxima línea define el plano" : "Dibujo libre en 3D");
    }

    // Crea el plano en el punto donde empieza la línea, mirando hacia ti.
    public void DefinirPlano(Vector3 local, Vector3 adelanteMundo)
    {
        adelanteMundo.y = 0f;
        if (adelanteMundo.sqrMagnitude < 1e-4f)
            adelanteMundo = Vector3.forward;
        var c = CapaActual;
        c.planoPunto = local;
        c.planoNormal = transform.InverseTransformDirection(adelanteMundo.normalized).normalized;
        c.hayPlano = true;
        RefrescarPlano();
        ActualizarGuia();
    }

    // El plano de una capa (en coordenadas del dibujo). Si la capa está "unida", usa el plano del grupo:
    // cada capa unida queda medio milímetro más cerca de ti que la anterior (boceto atrás, tinta adelante).
    public bool PlanoDeCapa(int capa, out Vector3 punto, out Vector3 normal)
    {
        AsegurarCapas();
        capa = Mathf.Clamp(capa, 0, capas.Count - 1);
        var c = capas[capa];
        if (c.unido)
        {
            for (int i = 0; i < capas.Count; i++)
            {
                var b = capas[i];
                if (!b.unido || !b.hayPlano || b.planoNormal.sqrMagnitude < 1e-6f)
                    continue;
                normal = b.planoNormal.normalized;
                punto = b.planoPunto - normal * (SeparacionPlanos / EscalaMundo) * (capa - i);
                return true;
            }
        }
        bool hay = c.hayPlano && c.planoNormal.sqrMagnitude > 1e-6f;
        punto = c.planoPunto;
        normal = hay ? c.planoNormal.normalized : Vector3.forward;
        return hay;
    }

    void RefrescarPlano()
    {
        Vector3 p, n;
        HayPlano = PlanoDeCapa(capaActual, out p, out n);
        planoPunto = p;
        planoNormal = n;
    }

    public void AlternarIman()
    {
        var c = CapaActual;
        c.iman = !c.iman;
        Avisar();
        Mensaje(c.nombre + (c.iman ? ": imán encendido (las puntas se unen)" : ": imán apagado (las líneas quedan como las dibujas)"));
    }

    // Une (o separa) el plano de la capa actual con los de las otras capas unidas.
    public void AlternarUnirPlano()
    {
        var c = CapaActual;
        c.unido = !c.unido;
        RefrescarPlano();
        if (c.unido && HayPlano)
        {
            // Las líneas que ya tenía la capa se pegan al plano unido.
            GuardarParaDeshacer();
            bool antes = Trazo.silenciar;
            Trazo.silenciar = true;
            foreach (var t in trazos)
            {
                if (t == null || t.capa != capaActual || t.nodos.Count < 2)
                    continue;
                var d = t.CrearDatos();
                for (int k = 0; k < d.nodos.Count; k++)
                {
                    d.nodos[k] = ProyectarEnPlano(d.nodos[k]);
                    if (k < d.asaEntrada.Count) d.asaEntrada[k] = ProyectarVectorEnPlano(d.asaEntrada[k]);
                    if (k < d.asaSalida.Count) d.asaSalida[k] = ProyectarVectorEnPlano(d.asaSalida[k]);
                }
                t.AplicarPose(d, null, 0f);
            }
            Trazo.silenciar = antes;
        }
        if (hojas != null)
            hojas.RedibujarTodo();
        ActualizarGuia();
        Avisar();
        Mensaje(c.unido ? c.nombre + ": plano unido (pegado a las otras capas)" : c.nombre + ": plano propio");
    }

    // El plano 2D en el mundo (punto y normal, la normal apunta lejos de ti). false si no hay plano.
    public bool PlanoMundo(out Vector3 punto, out Vector3 normal)
    {
        punto = transform.TransformPoint(planoPunto);
        normal = transform.TransformDirection(planoNormal).normalized;
        return PlanoActivo;
    }

    public Vector3 ProyectarVectorEnPlano(Vector3 vectorLocal)
    {
        if (!PlanoActivo)
            return vectorLocal;
        return vectorLocal - planoNormal * Vector3.Dot(vectorLocal, planoNormal);
    }

    // Qué tan lejos (en metros) está un punto del plano de dibujo 2D.
    public float DistanciaAlPlanoMundo(Vector3 local)
    {
        if (!PlanoActivo)
            return 0f;
        return Mathf.Abs(Vector3.Dot(local - planoPunto, planoNormal)) * EscalaMundo;
    }

    public Vector3 ProyectarEnPlano(Vector3 local)
    {
        if (!PlanoActivo)
            return local;
        return local - planoNormal * Vector3.Dot(local - planoPunto, planoNormal);
    }

    void ActualizarGuia()
    {
        if (guia == null)
        {
            guia = new GameObject("GuiaPlano");
            guia.transform.SetParent(transform, false);
            guia.AddComponent<MeshFilter>().sharedMesh = MallaGuia();
            var mr = guia.AddComponent<MeshRenderer>();
            mr.sharedMaterial = materialGuia;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
        bool ver = PlanoActivo;
        guia.SetActive(ver);
        if (!ver)
            return;
        guia.transform.localPosition = planoPunto;
        Vector3 arriba = Vector3.ProjectOnPlane(transform.InverseTransformDirection(Vector3.up), planoNormal);
        if (arriba.sqrMagnitude < 1e-6f)
            arriba = Vector3.Cross(planoNormal, Vector3.right);
        guia.transform.localRotation = Quaternion.LookRotation(planoNormal, arriba);
        guia.transform.localScale = Vector3.one / Mathf.Max(0.0001f, transform.localScale.x);
    }

    // Cuadro de 1 m con una cruz y marcas cada 10 cm.
    static Mesh MallaGuia()
    {
        var v = new List<Vector3>();
        var idx = new List<int>();
        float m = 0.5f;
        Vector3[] esquinas = { new Vector3(-m, -m, 0f), new Vector3(m, -m, 0f), new Vector3(m, m, 0f), new Vector3(-m, m, 0f) };
        for (int i = 0; i < 4; i++)
        {
            idx.Add(v.Count); v.Add(esquinas[i]);
            idx.Add(v.Count); v.Add(esquinas[(i + 1) % 4]);
        }
        for (int i = -4; i <= 4; i++)
        {
            float c = i * 0.1f;
            float largo = i == 0 ? m : 0.02f;
            idx.Add(v.Count); v.Add(new Vector3(c, -largo, 0f));
            idx.Add(v.Count); v.Add(new Vector3(c, largo, 0f));
            idx.Add(v.Count); v.Add(new Vector3(-largo, c, 0f));
            idx.Add(v.Count); v.Add(new Vector3(largo, c, 0f));
        }
        var malla = new Mesh { name = "GuiaPlano" };
        malla.SetVertices(v);
        malla.SetIndices(idx, MeshTopology.Lines, 0);
        malla.RecalculateBounds();
        return malla;
    }

    // ---------- Grosor proporcional ----------

    // Con una línea seleccionada, el grosor cambia solo en ella; sin selección, en todo el dibujo.
    public void EmpezarGrosor()
    {
        GuardarParaDeshacer();
        grosorSolo = Seleccion;
        if (grosorSolo != null)
            anchoSoloInicio = grosorSolo.ancho;
        anchosInicio.Clear();
        foreach (var t in trazos)
            anchosInicio.Add(t != null ? t.ancho : 0f);
        pincelInicio = anchoPincel;
    }

    public void AplicarFactorGrosor(float factor)
    {
        factor = Mathf.Clamp(factor, 0.1f, 10f);
        if (grosorSolo != null)
        {
            float solo = Mathf.Clamp(anchoSoloInicio * factor, 0.0005f, 0.5f);
            if (Mathf.Abs(solo - grosorSolo.ancho) > 1e-6f)
            {
                grosorSolo.ancho = solo;
                grosorSolo.Reconstruir(true);
            }
            return;
        }
        anchoPincel = Mathf.Clamp(pincelInicio * factor, 0.001f, 0.06f);
        for (int i = 0; i < trazos.Count && i < anchosInicio.Count; i++)
        {
            if (trazos[i] == null)
                continue;
            float nuevo = Mathf.Clamp(anchosInicio[i] * factor, 0.0005f, 0.5f);
            if (Mathf.Abs(nuevo - trazos[i].ancho) > 1e-6f)
            {
                trazos[i].ancho = nuevo;
                trazos[i].Reconstruir(true);
            }
        }
    }

    public void TerminarGrosor()
    {
        grosorSolo = null;
        Avisar();
    }

    // ---------- Deshacer y borrar ----------

    public void GuardarParaDeshacer()
    {
        // Un cambio nuevo borra lo que se podía rehacer (se guarda por si el cambio se descarta).
        rehacerRespaldo.Clear();
        rehacerRespaldo.AddRange(rehacer);
        rehacer.Clear();
        historial.Add(Foto());
        if (historial.Count > maxHistorial)
            historial.RemoveAt(0);
        HayCambios = true;
        if (animacion != null)
            animacion.AntesDeEditar();
    }

    public void DescartarUltimoDeshacer()
    {
        if (historial.Count > 0)
            historial.RemoveAt(historial.Count - 1);
        if (rehacer.Count == 0 && rehacerRespaldo.Count > 0)
            rehacer.AddRange(rehacerRespaldo);
        rehacerRespaldo.Clear();
    }

    // Cuántas veces se ha deshecho algo (el tutorial lo usa para saber que lo lograste).
    public int VecesDeshecho { get; private set; }

    public bool Deshacer()
    {
        if (historial.Count == 0)
        {
            Mensaje("Nada que deshacer");
            return false;
        }
        var foto = historial[historial.Count - 1];
        historial.RemoveAt(historial.Count - 1);
        rehacer.Add(Foto());
        if (rehacer.Count > maxHistorial)
            rehacer.RemoveAt(0);
        rehacerRespaldo.Clear();
        AplicarFoto(foto);
        VecesDeshecho++;
        Mensaje("Deshecho");
        return true;
    }

    public bool Rehacer()
    {
        if (rehacer.Count == 0)
        {
            Mensaje("Nada que rehacer");
            return false;
        }
        var foto = rehacer[rehacer.Count - 1];
        rehacer.RemoveAt(rehacer.Count - 1);
        historial.Add(Foto());
        if (historial.Count > maxHistorial)
            historial.RemoveAt(0);
        AplicarFoto(foto);
        Mensaje("Rehecho");
        return true;
    }

    // ¿La línea ya está exactamente así? (Entonces no hace falta volver a armarla.)
    static bool MismoTrazo(Trazo t, DatosTrazo d)
    {
        return JsonUtility.ToJson(t.CrearDatos()) == JsonUtility.ToJson(d);
    }

    // Foto para deshacer: el dibujo sin animación + las claves (cada una guardada una sola vez).
    FotoDeshacer Foto()
    {
        return new FotoDeshacer
        {
            datos = JsonUtility.ToJson(CrearDatos(false)),
            claves = animacion != null ? animacion.Instantanea() : null,
        };
    }

    void AplicarFoto(FotoDeshacer foto)
    {
        if (foto == null)
            return;
        var d = JsonUtility.FromJson<DatosDibujo>(foto.datos);
        if (d != null)
            Aplicar(d, false, foto.claves ?? new List<string>());
    }

    public void BorrarTodo()
    {
        if (Titere.Activo)
        {
            Mensaje("Primero suelta los personajes");
            return;
        }
        if (trazos.Count == 0 && (hojas == null || !hojas.HayAlgo) && (figuras == null || !figuras.HayFiguras))
        {
            Mensaje("No hay nada que borrar");
            return;
        }
        GuardarParaDeshacer();
        LimpiarTrazos();
        if (animacion != null)
            animacion.Restaurar(null, 0);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        foreach (var c in capas)
            c.hayPlano = false;
        if (figuras != null)
            figuras.QuitarTodas();
        if (hojas != null)
            hojas.Limpiar();
        RefrescarPlano();
        ActualizarGuia();
        Trazo.huboCambio = false;
        NombreArchivo = ""; // al guardar, será un dibujo nuevo (con otro nombre)
        Avisar();
        Mensaje("Borrado (el pulgar a la izquierda lo recupera)");
    }

    // ---------- Tutorial ----------
    // Antes del tutorial se aparta TODO el dibujo (queda la hoja en blanco) y al terminar vuelve tal cual,
    // sin las líneas del tutorial y sin que "deshacer" las traiga de vuelta.
    FotoDeshacer apartado;
    int historialApartado;
    bool cambiosApartado;

    public void ApartarParaTutorial()
    {
        if (apartado != null)
            return;
        apartado = Foto();
        historialApartado = historial.Count;
        cambiosApartado = HayCambios;
        Seleccionar(null);
        grupo.Clear();
        LimpiarTrazos();
        if (animacion != null)
            animacion.Restaurar(null, 0);
        if (figuras != null)
            figuras.QuitarTodas();
        if (hojas != null)
            hojas.Limpiar();
        plano = false; // en el tutorial se dibuja libre (en 3D), sin plano ni hoja de lápiz
        ActualizarVisibilidad();
        ActualizarGuia();
    }

    public void RecuperarDeTutorial()
    {
        if (apartado == null)
            return;
        var foto = apartado;
        apartado = null;
        AplicarFoto(foto);
        if (historial.Count > historialApartado)
            historial.RemoveRange(historialApartado, historial.Count - historialApartado);
        rehacer.Clear();
        rehacerRespaldo.Clear();
        HayCambios = cambiosApartado;
    }

    void LimpiarTrazos()
    {
        foreach (var t in trazos)
            if (t != null)
                Destroy(t.gameObject);
        trazos.Clear();
    }

    // ---------- Guardar y cargar ----------

    // ---------- El proyecto (.jc) ----------
    // Un archivo .jc es TODO el proyecto: capas, líneas, animación, lápiz, figuras, personajes, bocas
    // y además, adentro, las imágenes de referencia y el audio (así se puede copiar a otro visor o al PC).
    public const string Extension = ".jc";
    public bool HayCambios { get; private set; }
    float proximoAutoguardado = 180f;
    Referencias refs;
    Referencias Refs => refs != null ? refs : (refs = FindFirstObjectByType<Referencias>());

    string RutaProyecto(string nombre)
    {
        return Path.Combine(CarpetaArchivos, nombre + Extension);
    }

    // Guarda el dibujo con SU nombre. Un dibujo nuevo recibe el siguiente nombre libre (Dibujo 1, 2, 3...).
    public void Guardar()
    {
        GuardarProyecto(false);
    }

    void GuardarProyecto(bool silencioso)
    {
        if (string.IsNullOrEmpty(NombreArchivo))
            NombreArchivo = NombreLibre();
        var d = DatosParaGuardar();
        IncluirArchivos(d);
        string json = JsonUtility.ToJson(d);
        bool ok = false;
        try
        {
            Directory.CreateDirectory(CarpetaArchivos);
            File.WriteAllText(RutaProyecto(NombreArchivo), json);
            ok = true;
            // Si venía de un .json de antes con el mismo nombre, ya quedó convertido a .jc.
            string viejo = Path.Combine(CarpetaArchivos, NombreArchivo + ".json");
            if (File.Exists(viejo))
            {
                File.Delete(viejo);
                string miniVieja = RutaMiniatura(viejo);
                if (File.Exists(miniVieja))
                    File.Delete(miniVieja);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo guardar: " + e.Message);
        }
        if (ok)
        {
            Escribir(RutaGuardado, json); // el último guardado (para el menú de la mano)
            GuardarMiniatura(RutaProyecto(NombreArchivo));
            HayCambios = false;
        }
        if (!silencioso || !ok)
            Mensaje(ok ? "Guardado: " + NombreArchivo : "No se pudo guardar");
        else
            Mensaje("Autoguardado: " + NombreArchivo);
    }

    // Mete adentro del proyecto las imágenes de referencia y el audio que usa.
    void IncluirArchivos(DatosDibujo d)
    {
        d.incluidos.Clear();
        d.referencias.Clear();
        var r = Refs;
        if (r != null)
        {
            d.conReferencias = true;
            d.referencias.AddRange(r.EstadoActual());
            var hechos = new HashSet<string>();
            foreach (var img in d.referencias)
                if (img != null && !string.IsNullOrEmpty(img.archivo) && hechos.Add(img.archivo))
                    Incluir(d, Path.Combine(r.CarpetaImagenes, img.archivo), "imagen");
        }
        if (lipsync != null && !string.IsNullOrEmpty(lipsync.ArchivoAudio))
            Incluir(d, Path.Combine(lipsync.CarpetaAudio, lipsync.ArchivoAudio), "audio");
    }

    static void Incluir(DatosDibujo d, string ruta, string tipo)
    {
        try
        {
            if (File.Exists(ruta))
                d.incluidos.Add(new ArchivoIncluido
                {
                    nombre = Path.GetFileName(ruta),
                    tipo = tipo,
                    datos = System.Convert.ToBase64String(File.ReadAllBytes(ruta)),
                });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo incluir " + ruta + ": " + e.Message);
        }
    }

    // Al abrir un proyecto: saca sus imágenes y su audio a sus carpetas (si aún no están).
    void SacarArchivos(DatosDibujo d)
    {
        if (d.incluidos == null)
            return;
        foreach (var a in d.incluidos)
        {
            if (a == null || string.IsNullOrEmpty(a.nombre) || string.IsNullOrEmpty(a.datos))
                continue;
            string carpeta = a.tipo == "audio"
                ? (lipsync != null ? lipsync.CarpetaAudio : null)
                : (Refs != null ? Refs.CarpetaImagenes : null);
            if (carpeta == null)
                continue;
            try
            {
                string ruta = Path.Combine(carpeta, Path.GetFileName(a.nombre));
                if (File.Exists(ruta))
                    continue;
                Directory.CreateDirectory(carpeta);
                File.WriteAllBytes(ruta, System.Convert.FromBase64String(a.datos));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TrazoVR: no se pudo sacar " + a.nombre + ": " + e.Message);
            }
        }
    }

    // Cambia el nombre del proyecto actual (y de su archivo, si ya estaba guardado).
    public void Renombrar(string nuevo)
    {
        nuevo = LimpiarNombre(nuevo);
        if (string.IsNullOrEmpty(nuevo) || nuevo == NombreArchivo)
            return;
        if (File.Exists(RutaProyecto(nuevo)))
        {
            Mensaje("Ya existe un dibujo llamado " + nuevo);
            return;
        }
        try
        {
            if (!string.IsNullOrEmpty(NombreArchivo) && File.Exists(RutaProyecto(NombreArchivo)))
            {
                File.Move(RutaProyecto(NombreArchivo), RutaProyecto(nuevo));
                string mini = RutaMiniatura(RutaProyecto(NombreArchivo));
                if (File.Exists(mini))
                    File.Move(mini, RutaMiniatura(RutaProyecto(nuevo)));
            }
            NombreArchivo = nuevo;
            Mensaje("Ahora se llama: " + nuevo);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo renombrar: " + e.Message);
            Mensaje("No se pudo cambiar el nombre");
        }
    }

    // Guarda una copia con otro nombre ("Dibujo 3 (copia)") y sigues trabajando en la copia.
    public void GuardarCopia()
    {
        string baseNombre = string.IsNullOrEmpty(NombreArchivo) ? NombreLibre() : NombreArchivo;
        string nombre = baseNombre + " (copia)";
        for (int k = 2; File.Exists(RutaProyecto(nombre)); k++)
            nombre = baseNombre + " (copia " + k + ")";
        NombreArchivo = nombre;
        Guardar();
    }

    static string LimpiarNombre(string nombre)
    {
        if (nombre == null)
            return "";
        foreach (char c in Path.GetInvalidFileNameChars())
            nombre = nombre.Replace(c.ToString(), "");
        nombre = nombre.Trim();
        return nombre.Length > 40 ? nombre.Substring(0, 40) : nombre;
    }

    // ---------- Miniaturas (para el explorador de archivos) ----------

    public string CarpetaMiniaturas => Path.Combine(Carpeta, "Miniaturas");
    public string CarpetaArchivosDibujo => CarpetaArchivos;
    public string RutaGuardadoAnterior => RutaGuardado;

    public string RutaMiniatura(string ruta)
    {
        return Path.Combine(CarpetaMiniaturas, Path.GetFileName(ruta) + ".png");
    }

    // Una foto pequeña del dibujo, para verla en el explorador de archivos.
    public void GuardarMiniatura(string rutaArchivo)
    {
        try
        {
            Vector3 posicion;
            Quaternion rotacion;
            float campoVision;
            EncuadreExportar(1.15f, out posicion, out rotacion, out campoVision);
            if (figuras != null)
                figuras.PonerVista(posicion);
            byte[] png = Exportar.Foto(posicion, rotacion, campoVision, 320, 180, Color.white, MascaraExportar);
            if (figuras != null)
                figuras.PonerVista(null);
            if (png == null)
                return;
            Directory.CreateDirectory(CarpetaMiniaturas);
            File.WriteAllBytes(RutaMiniatura(rutaArchivo), png);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo guardar la miniatura: " + e.Message);
        }
    }

    string NombreLibre()
    {
        int mayor = 0;
        foreach (var ruta in ListaArchivos(1000))
        {
            string n = Path.GetFileNameWithoutExtension(ruta);
            if (ruta == RutaGuardado)
                continue;
            int k;
            if (n.StartsWith("Dibujo ") && int.TryParse(n.Substring(7), out k))
                mayor = Mathf.Max(mayor, k);
        }
        return "Dibujo " + (mayor + 1);
    }

    // Los dibujos guardados, el más reciente primero (rutas completas).
    public List<string> ListaArchivos(int maximo)
    {
        var lista = new List<string>();
        try
        {
            if (Directory.Exists(CarpetaArchivos))
            {
                lista.AddRange(Directory.GetFiles(CarpetaArchivos, "*" + Extension));
                lista.AddRange(Directory.GetFiles(CarpetaArchivos, "*.json")); // dibujos de antes
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo leer la carpeta: " + e.Message);
        }
        lista.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
        if (lista.Count == 0 && File.Exists(RutaGuardado))
            lista.Add(RutaGuardado); // dibujos de antes (un solo archivo)
        if (lista.Count > maximo)
            lista.RemoveRange(maximo, lista.Count - maximo);
        return lista;
    }

    public void AbrirArchivo(string ruta)
    {
        if (Titere.Activo)
        {
            Mensaje("Primero suelta los personajes");
            return;
        }
        var d = Leer(ruta);
        if (d == null)
        {
            Mensaje("No se pudo abrir");
            return;
        }
        GuardarParaDeshacer();
        SacarArchivos(d);
        Aplicar(d, true);
        if (d.conReferencias && Refs != null)
            Refs.Reemplazar(d.referencias);
        NombreArchivo = ruta == RutaGuardado ? "" : Path.GetFileNameWithoutExtension(ruta);
        HayCambios = false;
        Mensaje("Abierto: " + (string.IsNullOrEmpty(NombreArchivo) ? "guardado anterior" : NombreArchivo));
    }

    // Menú de la mano: abre el dibujo guardado más reciente.
    public void Cargar()
    {
        var lista = ListaArchivos(1);
        if (lista.Count == 0)
        {
            Mensaje("No hay nada guardado");
            return;
        }
        AbrirArchivo(lista[0]);
    }

    DatosDibujo CrearDatos(bool conClaves = true)
    {
        AsegurarCapas();
        var d = new DatosDibujo
        {
            fondo = escenario != null ? escenario.modo : 0,
            anchoPincel = anchoPincel,
            posicion = transform.localPosition,
            rotacion = transform.localRotation,
            escala = transform.localScale.x,
            plano = plano,
            hayPlano = HayPlano,
            planoPunto = planoPunto,
            planoNormal = planoNormal,
            capaActual = capaActual,
            siguienteId = siguienteId,
            fotograma = animacion != null ? animacion.Fotograma : 0,
            fps = animacion != null ? animacion.fotogramasPorSegundo : 12f,
            pincelElegido = pincelElegido,
            nombre = NombreArchivo ?? ""
        };
        if (titere != null)
            titere.GuardarEn(d);
        foreach (var c in capas)
            d.capas.Add(c.Copia());
        if (figuras != null)
            figuras.GuardarEn(d);
        if (hojas != null)
            hojas.GuardarEn(d);
        foreach (var t in trazos)
            if (t != null && t.nodos.Count >= 2)
                d.trazos.Add(t.CrearDatos());
        if (animacion != null && conClaves)
            d.claves.AddRange(animacion.claves);
        if (lipsync != null)
        {
            d.bocas.AddRange(lipsync.CopiarPoses());
            d.audio = lipsync.ArchivoAudio;
        }
        return d;
    }

    // claves = null: usa las claves de "d" (al cargar). Si no, son las claves de una foto de deshacer.
    void Aplicar(DatosDibujo d, bool incluirFondo, List<string> clavesFoto = null)
    {
        Trazo.silenciar = true;
        // Las líneas que no cambiaron se quedan como están (no se vuelven a armar): deshacer es rápido.
        var viejos = new Dictionary<int, Trazo>();
        foreach (var t in trazos)
        {
            if (t == null)
                continue;
            if (t.id > 0 && !viejos.ContainsKey(t.id))
                viejos[t.id] = t;
            else
                Destroy(t.gameObject);
        }
        trazos.Clear();
        seleccion = null;
        grupo.Clear();
        anchoPincel = d.anchoPincel > 0f ? d.anchoPincel : 0.008f;
        transform.localPosition = d.posicion;
        var q = d.rotacion;
        bool rotacionValida = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 0.5f;
        transform.localRotation = rotacionValida ? q : Quaternion.identity;
        transform.localScale = Vector3.one * (d.escala > 0f ? d.escala : 1f);
        plano = d.plano;

        capas.Clear();
        if (d.capas != null)
            foreach (var c in d.capas)
                if (c != null)
                    capas.Add(c.Copia());
        AsegurarCapas();
        capaActual = Mathf.Clamp(d.capaActual, 0, capas.Count - 1);
        if (d.version < 4)
        {
            // Dibujos viejos: el plano y las líneas vivas eran de todo el dibujo.
            foreach (var c in capas)
            {
                c.temblor = d.temblor;
                c.hebras = Mathf.Max(1, d.temblorHebras);
                c.grosorVivo = d.temblorGrosor;
                c.ciclo3 = d.temblorCiclo;
            }
            if (d.hayPlano && d.planoNormal.sqrMagnitude > 1e-6f)
            {
                capas[capaActual].hayPlano = true;
                capas[capaActual].planoPunto = d.planoPunto;
                capas[capaActual].planoNormal = d.planoNormal.normalized;
            }
        }
        if (d.version < 5)
        {
            // Antes las capas no estaban unidas por defecto.
            foreach (var c in capas)
                c.unido = true;
        }
        RefrescarPlano();

        siguienteId = Mathf.Max(1, d.siguienteId);
        if (d.trazos != null)
        {
            foreach (var dt in d.trazos)
            {
                if (dt == null || dt.nodos == null || dt.nodos.Count < 2)
                    continue;
                Trazo viejo;
                if (dt.id > 0 && viejos.TryGetValue(dt.id, out viejo) && viejo != null && MismoTrazo(viejo, dt))
                {
                    viejos.Remove(dt.id);
                    AplicarCapaVisual(viejo);
                    siguienteId = Mathf.Max(siguienteId, viejo.id + 1);
                    trazos.Add(viejo);
                    continue;
                }
                var t = CrearTrazo(dt.ancho > 0f ? dt.ancho : 0.008f);
                t.id = dt.id > 0 ? dt.id : siguienteId++;
                t.capa = Mathf.Clamp(dt.capa, 0, capas.Count - 1);
                t.color = dt.color;
                AplicarCapaVisual(t);
                siguienteId = Mathf.Max(siguienteId, t.id + 1);
                t.AplicarPose(dt, null, 0f);
                trazos.Add(t);
            }
        }
        foreach (var t in viejos.Values)
            if (t != null)
                Destroy(t.gameObject);
        seleccion = null;
        grupo.Clear();
        if (animacion != null)
        {
            if (d.fps > 0f)
                animacion.fotogramasPorSegundo = d.fps;
            if (clavesFoto != null)
                animacion.RestaurarInstantanea(clavesFoto, d.fotograma);
            else
                animacion.Restaurar(d.claves, d.fotograma);
        }
        Trazo.silenciar = false;
        Trazo.huboCambio = false;
        if (lipsync != null)
            lipsync.Restaurar(d.bocas, d.audio, incluirFondo);
        if (figuras != null)
            figuras.Restaurar(d);
        if (hojas != null)
            hojas.Restaurar(d);
        pincelElegido = d.pincelElegido;
        if (titere != null)
            titere.Restaurar(d);
        if (incluirFondo && escenario != null)
            escenario.PonerModo(d.fondo);
        if (incluirFondo)
            NombreArchivo = d.nombre ?? "";
        ActualizarVisibilidad();
        ActualizarGuia();
        Avisar();
    }

    bool Escribir(string ruta, string json)
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(ruta, json);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo guardar: " + e.Message);
            return false;
        }
    }

    DatosDibujo Leer(string ruta)
    {
        try
        {
            if (!File.Exists(ruta))
                return null;
            return JsonUtility.FromJson<DatosDibujo>(File.ReadAllText(ruta));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo leer: " + e.Message);
            return null;
        }
    }

    // ---------- Exportar (SVG y foto) ----------

    // Dirección desde la que se exporta: en Plano, de frente al plano; en 3D, desde tu cabeza hacia el dibujo.
    void VistaExportar(out Vector3 adelante, out Vector3 posicion)
    {
        var control = ControlManos.Instancia;
        Transform cabeza = control != null ? control.Cabeza : null;
        posicion = cabeza != null ? cabeza.position : transform.position - Vector3.forward;
        adelante = cabeza != null ? cabeza.forward : Vector3.forward;
        Bounds caja;
        if (Caja(out caja))
        {
            Vector3 centro = transform.TransformPoint(caja.center);
            Vector3 dir = centro - posicion;
            if (dir.sqrMagnitude > 1e-4f)
                adelante = dir.normalized;
        }
        if (PlanoActivo)
            adelante = transform.TransformDirection(planoNormal).normalized;
    }

    public void ExportarSVG()
    {
        Vector3 adelante, posicion;
        VistaExportar(out adelante, out posicion);
        string svg = Exportar.Svg(this, adelante);
        if (svg == null)
        {
            Mensaje("No hay líneas para exportar");
            return;
        }
        string nombre = "dibujo_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".svg";
        Mensaje(Escribir(Path.Combine(Carpeta, nombre), svg) ? "SVG guardado: " + nombre : "No se pudo guardar el SVG");
    }

    // Capas que ven las fotos y los videos: solo la del dibujo (sin paneles, nodos ni imágenes de referencia).
    public int MascaraExportar => gameObject.layer != 0 ? (1 << gameObject.layer) : ~0;

    // Desde dónde se toma la foto o el video: tu cabeza mirando al dibujo, con el ángulo justo para que quepa.
    public void EncuadreExportar(float margen, out Vector3 posicion, out Quaternion rotacion, out float campoVision)
    {
        Vector3 adelante;
        VistaExportar(out adelante, out posicion);
        campoVision = 60f;
        Bounds caja;
        if (Caja(out caja))
        {
            float distancia = Vector3.Distance(transform.TransformPoint(caja.center), posicion);
            float radio = caja.extents.magnitude * EscalaMundo;
            if (distancia > 0.05f)
                campoVision = Mathf.Clamp(2f * Mathf.Atan(radio / distancia) * Mathf.Rad2Deg * margen, 20f, 100f);
        }
        Vector3 arriba = Mathf.Abs(Vector3.Dot(adelante, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        rotacion = Quaternion.LookRotation(adelante, arriba);
    }

    public string CarpetaDibujos => Carpeta;

    public void TomarFoto()
    {
        Vector3 posicion;
        Quaternion rotacion;
        float campoVision;
        EncuadreExportar(1.15f, out posicion, out rotacion, out campoVision);
        if (figuras != null)
            figuras.PonerVista(posicion);
        byte[] png = Exportar.Foto(posicion, rotacion, campoVision, 2560, 1440, Color.white, MascaraExportar);
        if (figuras != null)
            figuras.PonerVista(null);
        if (png == null)
        {
            Mensaje("No se pudo tomar la foto");
            return;
        }
        string nombre = "foto_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        try
        {
            Directory.CreateDirectory(Carpeta);
            string ruta = Path.Combine(Carpeta, nombre);
            File.WriteAllBytes(ruta, png);
            GuardarMiniatura(ruta);
            string publico = Galeria.Publicar(ruta, "image/png", "Pictures/JCartoons");
            Galeria.UltimoGuardado = "Foto " + nombre + ": " + Galeria.Donde(publico, ruta);
            Mensaje("Foto guardada. Búscala en la " + Galeria.Donde(publico, ruta));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo guardar la foto: " + e.Message);
            Mensaje("No se pudo guardar la foto");
        }
    }

    // ---------- Utilidades ----------

    // Caja que envuelve todos los nodos visibles, en coordenadas locales del Dibujo.
    public bool Caja(out Bounds caja)
    {
        caja = new Bounds();
        bool hay = false;
        float margen = 0f;
        foreach (var t in trazos)
        {
            if (!Editable(t))
                continue;
            foreach (var p in t.nodos)
            {
                if (!hay)
                {
                    caja = new Bounds(p, Vector3.zero);
                    hay = true;
                }
                else
                {
                    caja.Encapsulate(p);
                }
            }
            margen = Mathf.Max(margen, t.ancho);
        }
        if (hay)
            caja.Expand(margen + 0.02f / EscalaMundo);
        return hay;
    }

    public void NotificarCambio()
    {
        ActualizarGuia();
        Avisar();
    }

    void Avisar()
    {
        alCambiar?.Invoke();
    }

    public void Mensaje(string texto)
    {
        alMensaje?.Invoke(Idioma.T(texto));
    }
}

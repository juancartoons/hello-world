using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Una pose de un ciclo de caminado guardado. Coordenadas "normalizadas":
// x = hacia adelante, y = hacia arriba, z = de lado, divididas por el largo de la pierna.
[System.Serializable]
public class PoseCiclo
{
    public float tiempo; // 0 a 1 dentro del ciclo
    public float bob;    // sube y baja de la cadera
    public List<Vector3> pierna1 = new List<Vector3>();
    public List<Vector3> pierna2 = new List<Vector3>();
    public List<Vector3> brazo1 = new List<Vector3>();
    public List<Vector3> brazo2 = new List<Vector3>();
}

[System.Serializable]
public class CicloCaminado
{
    public string nombre;
    public List<PoseCiclo> poses = new List<PoseCiclo>();
}

[System.Serializable]
public class BibliotecaCiclos
{
    public List<CicloCaminado> ciclos = new List<CicloCaminado>();
}

// Una parte del dibujo pegada a un hueso (por ejemplo, el pantalón al muslo).
// miembro: 0 pierna 1, 1 pierna 2, 2 brazo 1, 3 brazo 2, 4 cuerpo, 5 cabeza; tramo: qué hueso del miembro.
[System.Serializable]
public class PegadoHueso
{
    public int id;
    public int miembro;
    public int tramo;
    public float profundidad; // metros hacia ti (lo de adelante tapa a lo de atrás)
}

[System.Serializable]
public class DatosPersonaje
{
    public string nombre = "Personaje";
    public int pierna1, pierna2, brazo1, brazo2, cabeza;
    public List<int> cuerpo = new List<int>();
    public List<PegadoHueso> pegados = new List<PegadoHueso>();
    public bool voltear;
    public int ciclo; // 0 Normal, 1 Con estilo, 2 Sigiloso (Ken Harris), 3.. ciclos guardados
    public int piso;  // la línea-piso que se creó con él (0 = ninguna)
    public Vector3 arriba; // "arriba" del personaje (en el dibujo) al crearlo; cero = la gravedad actual
}

// Títeres que caminan, corren y saltan. Hasta DOS a la vez: uno en cada mano.
//  - "Tipo" + "Crear": pone frente a ti un personaje listo (Palito, Musculoso, Gordito, Flaco o Niño) sobre un piso.
//  - ENCENDER: "choca esos cinco" con el personaje (mano abierta, palma hacia él, empujón rápido cerca).
//    Con la mano DERECHA lo controlas con la derecha; con la IZQUIERDA, otro personaje con la izquierda.
//  - APAGAR: choca esos cinco con la OTRA mano (la libre). Si las dos manos tienen personaje:
//    pon la palma hacia ARRIBA medio segundo (el muñeco se queda quieto mientras). O el botón "Parar".
//  - Mano a los lados = caminar (lento) o correr (rápido). Hacia atrás, se voltea.
//    GOLPE RÁPIDO DE LA MANO HACIA ARRIBA = SALTAR (se agacha solo, despega, cae, se aplasta y rebota).
//    La altura de la mano no lo mueve (así no flota ni se agacha solo).
//  - Al crear un personaje se enciende solo con la mano derecha.
//  - Solo pisa las líneas-piso que están a su misma profundidad (no las de otros personajes).
//  - "Arriba" es siempre la gravedad del mundo real (aunque el dibujo esté girado).
//  - Mientras un personaje se mueve, el dibujo queda bloqueado (candado) para no editarlo sin querer.
//  - "Piso +/-": la línea elegida es piso para los personajes.
//  - "Grabar": cuenta 3 segundos y guarda una clave por fotograma (todos los personajes encendidos).
//    "Choca esos cinco" con tu personaje (o "Parar") = terminar.
//  - "Ciclo": Normal, Con estilo (Richard Williams), Sigiloso (Ken Harris) o tus ciclos guardados.
//    "Guardar ciclo" toma tus claves.
//  - Los personajes nuevos van en la ÚLTIMA capa (Capa 4): "Ver/Oculta" de esa capa los esconde.
//  - Acerca la mano derecha a un personaje apagado: aparece su marco con una X arriba a la derecha.
//    Toca la X = borrar ese personaje (se puede deshacer).
//  - Mano ABAJO (más de 4 cm) = agacharse. Golpe rápido hacia arriba = saltar.
//  - "Posar dedos": el índice y el medio derechos acomodan las piernas; pellizco izquierdo = guardar clave.
//  - Para tus dibujos: pellizca una línea y toca Pierna 1/2, Brazo 1/2 o Cuerpo +/- ("Voltear" = otro lado).
public class Titere : MonoBehaviour
{
    public static bool Activo { get; private set; }
    public static readonly string[] Tipos = { "Palito", "Musculoso", "Gordito", "Flaco", "Niño" };

    public Dibujo dibujo;
    public Animacion animacion;
    public ControlManos control;
    public readonly List<DatosPersonaje> personajes = new List<DatosPersonaje>();
    public List<int> pisos = new List<int>();
    public int elegido;   // el personaje de los botones (Pierna, Brazo, Ciclo...)
    public int tipo;      // el tipo que crea "Crear"

    enum Fase { Libre, CuentaGrabar, Grabando, Posando }
    Fase fase = Fase.Libre;
    float faseHasta;
    int ultimoAviso;
    int fotogramaGrabado;
    int inicioGrabacion;
    float acumulado;
    bool grabarAlPreparar;

    // Marionetas encendidas: [0] mano derecha, [1] mano izquierda.
    readonly Marioneta[] manos = new Marioneta[2];
    Marioneta posando;

    // "Choca esos cinco" (por mano)
    readonly Vector3[] palmaPrevia = new Vector3[2];
    readonly bool[] teniaPalma = new bool[2];
    readonly float[] choqueHasta = new float[2];

    // Mientras una mano abierta va hacia un personaje (para chocar los cinco), no se edita nada
    // con las manos: así no se borran nodos ni se cambia un relleno sin querer.
    static readonly float[] protegidaHasta = new float[2];
    public static bool ManoCerca => Time.time < protegidaHasta[0] || Time.time < protegidaHasta[1];

    // Palma hacia arriba (apagar con la misma mano)
    readonly float[] palmaArribaDesde = { -1f, -1f };
    const float TiempoPalmaArriba = 0.5f;

    // Aro sobre el personaje: se llena al apagar con la palma arriba.
    [Tooltip("Material del aro indicador (apagar con la palma arriba)")]
    public Material materialIndicador;
    readonly Transform[] aros = new Transform[2];
    readonly Transform[] rellenos = new Transform[2];

    // Dedos (para posar)
    readonly Vector3[,] dedos = new Vector3[2, 4];
    Vector3 normalPalma = Vector3.down;
    Vector3 munecaDedos;
    bool dedosValidos, teniaDedos;

    BibliotecaCiclos biblioteca = new BibliotecaCiclos();

    static readonly string[][][] Dedos =
    {
        new[] { new[] { "Index1", "IndexProximal" }, new[] { "Index2", "IndexIntermediate" }, new[] { "Index3", "IndexDistal" }, new[] { "IndexTip" } },
        new[] { new[] { "Middle1", "MiddleProximal" }, new[] { "Middle2", "MiddleIntermediate" }, new[] { "Middle3", "MiddleDistal" }, new[] { "MiddleTip" } },
    };
    public static readonly string[] Muneca = { "WristRoot", "Wrist" };

    public bool Grabando => fase == Fase.Grabando;
    public bool Posando => fase == Fase.Posando;
    public bool Encendido => manos[0] != null || manos[1] != null;
    public DatosPersonaje Elegido => personajes.Count > 0 ? personajes[Mathf.Clamp(elegido, 0, personajes.Count - 1)] : null;
    public string NombreTipo => Tipos[Mathf.Clamp(tipo, 0, Tipos.Length - 1)];
    public string NombreCiclo => NombreDeCiclo(Elegido != null ? Elegido.ciclo : 0);
    public BibliotecaCiclos Biblioteca => biblioteca;

    string RutaBiblioteca => Path.Combine(Application.persistentDataPath, "Dibujos", "ciclos.json");

    void Awake()
    {
        if (dibujo == null) dibujo = GetComponent<Dibujo>();
        if (animacion == null && dibujo != null) animacion = dibujo.animacion;
    }

    void Start()
    {
        if (control == null) control = FindFirstObjectByType<ControlManos>();
        CargarBiblioteca();
    }

    void OnDisable()
    {
        Activo = false;
    }

    public void Mensaje(string texto)
    {
        if (dibujo != null)
            dibujo.Mensaje(texto);
    }

    public const int CiclosFijos = 3; // Normal, Con estilo, Sigiloso

    public string NombreDeCiclo(int c)
    {
        if (c <= 0) return "Normal";
        if (c == 1) return "Con estilo";
        if (c == 2) return "Sigiloso";
        int i = c - CiclosFijos;
        return i < biblioteca.ciclos.Count ? biblioteca.ciclos[i].nombre : "Normal";
    }

    public CicloCaminado CicloGuardado(int c)
    {
        int i = c - CiclosFijos;
        return c >= CiclosFijos && i < biblioteca.ciclos.Count ? biblioteca.ciclos[i] : null;
    }

    bool PersonajeValido(DatosPersonaje p)
    {
        return p != null && dibujo.BuscarPorId(p.pierna1) != null && dibujo.BuscarPorId(p.pierna2) != null;
    }

    // Se ve (su capa no está oculta): solo así se puede encender o borrar con la X.
    bool PersonajeVisible(DatosPersonaje p)
    {
        return PersonajeValido(p) && Dibujo.Editable(dibujo.BuscarPorId(p.pierna1));
    }

    // Todas las líneas del personaje (huesos, partes y su piso).
    static List<int> IdsDe(DatosPersonaje p, bool conPiso)
    {
        var l = new List<int> { p.pierna1, p.pierna2, p.brazo1, p.brazo2, p.cabeza };
        l.AddRange(p.cuerpo);
        foreach (var pg in p.pegados)
            l.Add(pg.id);
        if (conPiso && p.piso > 0)
            l.Add(p.piso);
        l.RemoveAll(id => id <= 0);
        return l;
    }

    // ---------- Marco con X (borrar un personaje) ----------

    class Marco
    {
        public LineRenderer linea;
        public Transform equis;
    }
    readonly Dictionary<DatosPersonaje, Marco> marcos = new Dictionary<DatosPersonaje, Marco>();
    readonly Vector3[] esquinas = new Vector3[4];
    float equisBloqueo;

    void ActualizarMarcos()
    {
        var der = control.Der;
        bool puede = !Activo && fase == Fase.Libre && !control.Ocupado && der.valida && control.Cabeza != null;
        DatosPersonaje borrar = null;
        foreach (var p in personajes)
        {
            Marco m;
            marcos.TryGetValue(p, out m);
            bool ver = puede && PersonajeVisible(p) && Vector3.Distance(der.indice, Centro(p)) < 0.4f;
            if (!ver)
            {
                if (m != null && m.linea.gameObject.activeSelf)
                    m.linea.gameObject.SetActive(false);
                continue;
            }
            if (m == null)
            {
                m = CrearMarco();
                marcos[p] = m;
            }
            if (!m.linea.gameObject.activeSelf)
                m.linea.gameObject.SetActive(true);
            // Caja del personaje vista de frente (hacia tu cabeza).
            Vector3 c = Centro(p);
            Vector3 derecha = control.Cabeza.right;
            derecha.y = 0f;
            derecha = derecha.sqrMagnitude > 1e-6f ? derecha.normalized : Vector3.right;
            Vector3 arriba = Vector3.up;
            float x0 = 0f, x1 = 0f, y0 = 0f, y1 = 0f;
            bool hay = false;
            foreach (int id in IdsDe(p, false))
            {
                var t = dibujo.BuscarPorId(id);
                if (t == null)
                    continue;
                foreach (var n in t.nodos)
                {
                    Vector3 w = dibujo.transform.TransformPoint(n) - c;
                    float x = Vector3.Dot(w, derecha), y = Vector3.Dot(w, arriba);
                    if (!hay) { x0 = x1 = x; y0 = y1 = y; hay = true; }
                    x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
                    y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
                }
            }
            const float margen = 0.015f;
            x0 -= margen; x1 += margen; y0 -= margen; y1 += margen;
            Vector3 hacia = (control.Cabeza.position - c).normalized * 0.01f;
            esquinas[0] = c + hacia + derecha * x0 + arriba * y0;
            esquinas[1] = c + hacia + derecha * x1 + arriba * y0;
            esquinas[2] = c + hacia + derecha * x1 + arriba * y1;
            esquinas[3] = c + hacia + derecha * x0 + arriba * y1;
            m.linea.SetPositions(esquinas);
            Vector3 posX = esquinas[2] + derecha * 0.012f + arriba * 0.012f;
            m.equis.position = posX;
            m.equis.rotation = Quaternion.LookRotation(posX - control.Cabeza.position, Vector3.up);
            if (Time.time > equisBloqueo && Vector3.Distance(der.indice, posX) < 0.016f)
            {
                equisBloqueo = Time.time + 1f;
                borrar = p;
            }
        }
        // Marcos de personajes que ya no existen.
        if (marcos.Count > personajes.Count)
        {
            var sobran = new List<DatosPersonaje>();
            foreach (var par in marcos)
                if (!personajes.Contains(par.Key))
                    sobran.Add(par.Key);
            foreach (var p in sobran)
            {
                if (marcos[p].linea != null)
                    Destroy(marcos[p].linea.gameObject);
                marcos.Remove(p);
            }
        }
        if (borrar != null)
            BorrarPersonaje(borrar);
    }

    Marco CrearMarco()
    {
        var go = new GameObject("MarcoPersonaje");
        var linea = go.AddComponent<LineRenderer>();
        linea.positionCount = 4;
        linea.loop = true;
        linea.useWorldSpace = true;
        linea.widthMultiplier = 0.0015f;
        linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        linea.receiveShadows = false;
        if (materialIndicador != null)
            linea.sharedMaterial = materialIndicador;
        var equis = new GameObject("X").transform;
        equis.SetParent(go.transform, false);
        for (int k = 0; k < 2; k++)
        {
            var palo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(palo.GetComponent<Collider>());
            palo.transform.SetParent(equis, false);
            palo.transform.localRotation = Quaternion.Euler(0f, 0f, k == 0 ? 45f : -45f);
            palo.transform.localScale = new Vector3(0.02f, 0.0035f, 0.002f);
            var r = palo.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (materialIndicador != null)
                r.sharedMaterial = materialIndicador;
        }
        return new Marco { linea = linea, equis = equis };
    }

    // Borra el personaje con todas sus líneas (y su piso). Se puede deshacer.
    public void BorrarPersonaje(DatosPersonaje p, bool silencioso = false)
    {
        if (p == null || Encendido)
            return;
        dibujo.GuardarParaDeshacer();
        foreach (int id in IdsDe(p, true))
            dibujo.EliminarDelTodo(dibujo.BuscarPorId(id));
        if (p.piso > 0)
            pisos.Remove(p.piso);
        personajes.Remove(p);
        elegido = Mathf.Clamp(elegido, 0, Mathf.Max(0, personajes.Count - 1));
        Trazo.huboCambio = false;
        if (!silencioso)
            Mensaje(p.nombre + " borrado (deshacer lo devuelve)");
    }

    // Los personajes nuevos van en su propia capa (la última): "Ver/Oculta" de esa capa los esconde.
    void MoverACapaPersonajes(DatosPersonaje p)
    {
        int capa = Dibujo.NumeroDeCapas - 1;
        var datos = dibujo.DatosDeCapa(capa);
        datos.visible = true;
        foreach (int id in IdsDe(p, true))
        {
            var t = dibujo.BuscarPorId(id);
            if (t == null)
                continue;
            t.capa = capa;
            dibujo.AplicarCapaVisual(t);
        }
        dibujo.ActualizarVisibilidad();
    }

    void ActualizarActivo()
    {
        Activo = manos[0] != null || manos[1] != null || fase == Fase.Posando;
        ActualizarAros();
    }

    // ---------- Guardar y cargar (con el dibujo) ----------

    public void GuardarEn(DatosDibujo d)
    {
        foreach (var p in personajes)
            d.personajes.Add(JsonUtility.FromJson<DatosPersonaje>(JsonUtility.ToJson(p)));
        d.titerePisos.AddRange(pisos);
        d.titereElegido = elegido;
    }

    public void Restaurar(DatosDibujo d)
    {
        ApagarTodo(false);
        personajes.Clear();
        if (d.personajes != null)
            foreach (var p in d.personajes)
                if (p != null)
                {
                    if (p.cuerpo == null) p.cuerpo = new List<int>();
                    if (p.pegados == null) p.pegados = new List<PegadoHueso>();
                    // Antes los ciclos guardados empezaban en 2 (ahora el 2 es "Sigiloso").
                    if (d.version < 6 && p.ciclo >= 2)
                        p.ciclo++;
                    personajes.Add(p);
                }
        // Dibujos de antes: un solo títere.
        if (personajes.Count == 0 && d.titerePierna1 > 0)
        {
            var p = new DatosPersonaje
            {
                nombre = "Palito",
                pierna1 = d.titerePierna1, pierna2 = d.titerePierna2,
                brazo1 = d.titereBrazo1, brazo2 = d.titereBrazo2, cabeza = d.titereCabeza,
                voltear = d.titereVoltear, ciclo = d.titereCiclo > 0 ? d.titereCiclo + 2 : 0,
            };
            if (d.titereCuerpo != null)
                p.cuerpo.AddRange(d.titereCuerpo);
            personajes.Add(p);
        }
        pisos.Clear();
        if (d.titerePisos != null)
            pisos.AddRange(d.titerePisos);
        elegido = Mathf.Clamp(d.titereElegido, 0, Mathf.Max(0, personajes.Count - 1));
    }

    // ---------- Botones ----------

    // Botón "Parar": apaga todos los personajes (o termina de grabar / posar).
    public void Parar()
    {
        if (fase == Fase.Grabando)
        {
            TerminarGrabacion();
            return;
        }
        if (fase == Fase.Posando)
        {
            AlternarPosar();
            return;
        }
        grabarAlPreparar = false;
        if (!Encendido && fase != Fase.CuentaGrabar)
        {
            Mensaje("No hay ningún personaje moviéndose");
            return;
        }
        ApagarTodo(true);
        Mensaje("Personaje apagado");
    }

    // El botón "Crear" se vuelve "Parar" mientras hay un personaje moviéndose.
    public void CrearOParar()
    {
        if (Encendido || fase != Fase.Libre)
            Parar();
        else
            CargarMuneco();
    }

    public void CambiarTipo()
    {
        tipo = (tipo + 1) % Tipos.Length;
        Mensaje("Personaje: " + NombreTipo);
    }

    public void Grabar()
    {
        if (fase == Fase.Grabando)
        {
            TerminarGrabacion();
            return;
        }
        if (fase == Fase.CuentaGrabar || fase == Fase.Posando)
            return;
        if (!Encendido)
        {
            // Nada encendido: enciende el personaje elegido con la derecha y luego graba.
            var p = Elegido;
            if (!PersonajeValido(p))
            {
                Mensaje("Primero crea un personaje");
                return;
            }
            Encender(0, p);
            grabarAlPreparar = true;
            return;
        }
        EmpezarCuenta();
    }

    void EmpezarCuenta()
    {
        fase = Fase.CuentaGrabar;
        faseHasta = Time.time + 3f;
        ultimoAviso = -1;
    }

    public void AlternarPosar()
    {
        if (fase == Fase.Posando)
        {
            if (posando != null)
                posando.Restaurar();
            posando = null;
            fase = Fase.Libre;
            ActualizarActivo();
            Mensaje("Posar terminado");
            return;
        }
        if (fase != Fase.Libre || Encendido)
            return;
        var p = Elegido;
        if (!PersonajeValido(p))
        {
            Mensaje("Primero crea un personaje");
            return;
        }
        if (animacion != null)
            animacion.Pausar();
        posando = new Marioneta(this, p, false);
        if (!posando.Preparar())
        {
            posando = null;
            return;
        }
        fase = Fase.Posando;
        ActualizarActivo();
        Mensaje("Acomoda las piernas con los dedos · pellizco izquierdo = guardar clave");
    }

    public void CambiarCiclo()
    {
        var p = Elegido;
        if (p == null)
            return;
        p.ciclo = (p.ciclo + 1) % (CiclosFijos + biblioteca.ciclos.Count);
        Mensaje("Caminado: " + NombreCiclo);
    }

    public void Voltear()
    {
        var p = Elegido;
        if (p == null)
            return;
        p.voltear = !p.voltear;
        Mensaje(p.voltear ? "Mira hacia el otro lado" : "Mira hacia el lado normal");
    }

    DatosPersonaje ElegidoOCrear()
    {
        if (personajes.Count == 0)
        {
            personajes.Add(new DatosPersonaje { nombre = "Mío" });
            elegido = 0;
        }
        return Elegido;
    }

    public void AsignarPierna(int cual) { Asignar(cual == 1 ? 1 : 2, "Pierna " + cual); }
    public void AsignarBrazo(int cual) { Asignar(cual == 1 ? 3 : 4, "Brazo " + cual); }

    void Asignar(int rol, string nombre)
    {
        if (dibujo == null || Encendido)
            return;
        var t = dibujo.Seleccion;
        if (t == null)
        {
            Mensaje("Pellizca una línea para elegirla");
            return;
        }
        var p = ElegidoOCrear();
        QuitarDeRoles(p, t.id);
        if (rol == 1) p.pierna1 = t.id;
        else if (rol == 2) p.pierna2 = t.id;
        else if (rol == 3) p.brazo1 = t.id;
        else p.brazo2 = t.id;
        Mensaje(nombre + " elegida");
    }

    static void QuitarDeRoles(DatosPersonaje p, int id)
    {
        if (p.pierna1 == id) p.pierna1 = 0;
        if (p.pierna2 == id) p.pierna2 = 0;
        if (p.brazo1 == id) p.brazo1 = 0;
        if (p.brazo2 == id) p.brazo2 = 0;
        if (p.cabeza == id) p.cabeza = 0;
        p.cuerpo.Remove(id);
        p.pegados.RemoveAll(x => x.id == id);
    }

    public void AlternarCuerpo()
    {
        if (dibujo == null || Encendido)
            return;
        var t = dibujo.Seleccion;
        if (t == null)
        {
            Mensaje("Pellizca una línea para elegirla");
            return;
        }
        var p = ElegidoOCrear();
        if (p.cuerpo.Contains(t.id))
        {
            p.cuerpo.Remove(t.id);
            Mensaje("La línea ya no es parte del cuerpo");
            return;
        }
        QuitarDeRoles(p, t.id);
        p.cuerpo.Add(t.id);
        Mensaje("Línea agregada al cuerpo (" + p.cuerpo.Count + ")");
    }

    public void AlternarPiso()
    {
        if (dibujo == null || Encendido)
            return;
        var t = dibujo.Seleccion;
        if (t == null)
        {
            Mensaje("Pellizca una línea para elegirla");
            return;
        }
        if (pisos.Remove(t.id))
        {
            Mensaje("La línea ya no es piso");
            return;
        }
        foreach (var p in personajes)
            QuitarDeRoles(p, t.id);
        pisos.Add(t.id);
        Mensaje("Línea marcada como piso (" + pisos.Count + ")");
    }

    // ---------- Encender y apagar ----------

    void Encender(int mano, DatosPersonaje p)
    {
        if (manos[mano] != null)
            Apagar(mano);
        if (animacion != null)
            animacion.Pausar();
        dibujo.Seleccionar(null);
        var m = new Marioneta(this, p, mano == 1);
        m.preparadoHasta = Time.time + 1f;
        manos[mano] = m;
        elegido = personajes.IndexOf(p);
        ActualizarActivo();
        Mensaje(mano == 0 ? "Pon la mano derecha frente a ti..." : "Pon la mano izquierda frente a ti...");
    }

    void Apagar(int mano)
    {
        var m = manos[mano];
        if (m == null)
            return;
        manos[mano] = null;
        if (fase != Fase.Grabando)
            m.Restaurar();
        if (!Encendido && (fase == Fase.CuentaGrabar || fase == Fase.Grabando))
        {
            if (fase == Fase.Grabando)
                TerminarGrabacion();
            else
                fase = Fase.Libre;
        }
        ActualizarActivo();
        Mensaje("Personaje apagado");
    }

    void ApagarTodo(bool restaurar)
    {
        for (int i = 0; i < 2; i++)
        {
            if (manos[i] != null && restaurar)
                manos[i].Restaurar();
            manos[i] = null;
        }
        posando = null;
        fase = Fase.Libre;
        ActualizarActivo();
    }

    // ---------- Cada cuadro ----------

    void Update()
    {
        if (dibujo == null || control == null)
            return;
        ActualizarMarcos();

        // "Choca esos cinco" con cada mano.
        if (fase != Fase.Posando && fase != Fase.CuentaGrabar)
        {
            for (int h = 0; h < 2; h++)
            {
                var p = DetectarChoque(h);
                // La mano que controla un personaje no apaga nada con choca esos cinco (así no lo persigues).
                if (p == null || manos[h] != null)
                    continue;
                if (manos[1 - h] != null && manos[1 - h].datos == p)
                {
                    // La mano libre choca con el personaje de la otra mano: se apaga.
                    if (fase == Fase.Grabando)
                    {
                        TerminarGrabacion();
                        return;
                    }
                    Apagar(1 - h);
                }
                else if (fase == Fase.Libre)
                {
                    Encender(h, p);
                }
            }
        }

        if (fase == Fase.Posando)
        {
            LeerDedos(6f);
            if (posando != null && dedosValidos)
                posando.PoseDedos(dedos, normalPalma, munecaDedos);
            if (control.Izq.valida && control.Izq.empezoPellizco && animacion != null && posando != null)
            {
                int f = animacion.Fotograma;
                dibujo.GuardarParaDeshacer();
                posando.PoseDedos(dedos, normalPalma, munecaDedos);
                animacion.GuardarClaveEn(f, CapaDe(posando.datos));
                Trazo.huboCambio = false;
                Mensaje("Clave guardada en el fotograma " + (f + 1));
            }
            return;
        }

        // Preparar las marionetas nuevas (1 segundo con la mano frente a ti).
        for (int h = 0; h < 2; h++)
        {
            var m = manos[h];
            if (m == null || m.listo)
                continue;
            m.LeerMuneca(true);
            if (Time.time < m.preparadoHasta)
                continue;
            if (!m.Preparar() || !m.ManoValida)
            {
                m.preparadoHasta = Time.time + 1f;
                Mensaje("No veo bien tu mano " + (h == 0 ? "derecha" : "izquierda"));
                continue;
            }
            m.Calibrar();
            if (grabarAlPreparar)
            {
                grabarAlPreparar = false;
                EmpezarCuenta();
            }
            else
            {
                Mensaje("¡Muévelo! A los lados = caminar · golpe hacia arriba = saltar · palma arriba = apagar");
            }
        }

        // Palma hacia arriba medio segundo con la mano que controla = apagar ese personaje.
        for (int h = 0; h < 2; h++)
        {
            var m = manos[h];
            if (m == null || !m.listo || fase == Fase.CuentaGrabar)
            {
                palmaArribaDesde[h] = -1f;
                if (m != null) m.congelado = false;
                continue;
            }
            if (PalmaArriba(h))
            {
                if (palmaArribaDesde[h] < 0f)
                    palmaArribaDesde[h] = Time.time;
                m.congelado = true;
                if (Time.time - palmaArribaDesde[h] >= TiempoPalmaArriba)
                {
                    palmaArribaDesde[h] = -1f;
                    if (fase == Fase.Grabando)
                    {
                        TerminarGrabacion();
                        ActualizarAros();
                        return;
                    }
                    Apagar(h);
                }
            }
            else
            {
                palmaArribaDesde[h] = -1f;
                m.congelado = false;
            }
        }
        ActualizarAros();

        float dt = Mathf.Max(1e-4f, Time.deltaTime);
        switch (fase)
        {
            case Fase.Libre:
                ActualizarTodas(dt);
                PonerTodas();
                break;

            case Fase.CuentaGrabar:
                ActualizarTodas(dt);
                PonerTodas();
                Avisar("Grabando en ");
                if (Time.time < faseHasta)
                    return;
                dibujo.GuardarParaDeshacer();
                fase = Fase.Grabando;
                inicioGrabacion = animacion != null ? animacion.Fotograma : 0;
                fotogramaGrabado = inicioGrabacion;
                acumulado = 0f;
                GrabarCuadro(fotogramaGrabado);
                Mensaje("Grabando... choca esos cinco con tu personaje (o Parar) para terminar");
                break;

            case Fase.Grabando:
                if (animacion == null)
                {
                    TerminarGrabacion();
                    return;
                }
                ActualizarTodas(dt);
                acumulado += dt * animacion.fotogramasPorSegundo;
                bool nuevo = false;
                while (acumulado >= 1f)
                {
                    acumulado -= 1f;
                    if (fotogramaGrabado >= Animacion.TotalFotogramas - 1)
                    {
                        TerminarGrabacion();
                        return;
                    }
                    fotogramaGrabado++;
                    nuevo = true;
                }
                if (nuevo)
                    GrabarCuadro(fotogramaGrabado);
                else
                    PonerTodas();
                break;
        }
    }

    void ActualizarTodas(float dt)
    {
        foreach (var m in manos)
            if (m != null && m.listo && !m.congelado)
                m.Actualizar(dt);
    }

    void PonerTodas()
    {
        foreach (var m in manos)
            if (m != null && m.listo)
                m.PonerPose();
    }

    void Avisar(string texto)
    {
        int resto = Mathf.CeilToInt(faseHasta - Time.time);
        if (resto == ultimoAviso || resto <= 0)
            return;
        ultimoAviso = resto;
        Mensaje(texto + resto);
    }

    void GrabarCuadro(int f)
    {
        animacion.IrA(f);
        animacion.MostrarFotograma();
        PonerTodas();
        // Cada personaje guarda su clave en SU capa.
        var capas = new HashSet<int>();
        foreach (var m in manos)
            if (m != null)
                capas.Add(CapaDe(m.datos));
        foreach (int capa in capas)
            animacion.GuardarClaveEn(f, capa);
        Trazo.huboCambio = false;
    }

    int CapaDe(DatosPersonaje p)
    {
        var t = p != null ? dibujo.BuscarPorId(p.pierna1) : null;
        return t != null ? t.capa : dibujo.capaActual;
    }

    void TerminarGrabacion()
    {
        int cuadros = fotogramaGrabado - inicioGrabacion + 1;
        for (int i = 0; i < 2; i++)
            manos[i] = null;
        fase = Fase.Libre;
        ActualizarActivo();
        Trazo.huboCambio = false;
        if (animacion != null)
        {
            animacion.IrA(inicioGrabacion);
            animacion.MostrarFotograma();
        }
        Mensaje("Grabados " + cuadros + " fotogramas. Toca Play para verlo");
    }

    // Mano abierta, palma hacia un personaje, empujón rápido cerca de él. Devuelve ese personaje.
    DatosPersonaje DetectarChoque(int h)
    {
        var m = h == 0 ? control.Der : control.Izq;
        if (m == null || !m.valida || m.esqueleto == null)
        {
            teniaPalma[h] = false;
            return null;
        }
        var palma = ManosUtil.LeerPalma(m.esqueleto, h == 1);
        if (!palma.valida)
        {
            teniaPalma[h] = false;
            return null;
        }
        float dt = Mathf.Max(1e-4f, Time.deltaTime);
        Vector3 vel = teniaPalma[h] ? (palma.centro - palmaPrevia[h]) / dt : Vector3.zero;
        palmaPrevia[h] = palma.centro;
        teniaPalma[h] = true;
        float tam = palma.tamano;
        bool abierta = Vector3.Distance(m.indice, palma.centro) / tam > 1.45f
                       && Vector3.Distance(m.medio, palma.centro) / tam > 1.45f
                       && Vector3.Distance(m.anular, palma.centro) / tam > 1.4f;
        if (!abierta)
            return null;
        // Mano abierta acercándose a un personaje: protege el dibujo un momento.
        foreach (var p in personajes)
        {
            if (!PersonajeVisible(p))
                continue;
            Vector3 hacia = Centro(p) - palma.centro;
            float dist = hacia.magnitude;
            if (dist < 0.32f && (dist < 0.12f || Vector3.Dot(vel, hacia / Mathf.Max(dist, 1e-4f)) > 0.25f))
            {
                protegidaHasta[h] = Time.time + 0.6f;
                break;
            }
        }
        if (Time.time < choqueHasta[h])
            return null;
        DatosPersonaje mejor = null;
        float mejorDist = 0.22f;
        foreach (var p in personajes)
        {
            if (!PersonajeVisible(p))
                continue;
            Vector3 hacia = Centro(p) - palma.centro;
            float dist = hacia.magnitude;
            if (dist >= mejorDist || dist < 1e-4f)
                continue;
            hacia /= dist;
            if (Vector3.Dot(palma.normal, hacia) < 0.4f || Vector3.Dot(vel, hacia) < 0.6f)
                continue;
            mejorDist = dist;
            mejor = p;
        }
        if (mejor != null)
            choqueHasta[h] = Time.time + 1f;
        return mejor;
    }

    // Mano abierta con la palma mirando al techo.
    bool PalmaArriba(int h)
    {
        var m = h == 0 ? control.Der : control.Izq;
        if (m == null || !m.valida || m.esqueleto == null)
            return false;
        var palma = ManosUtil.LeerPalma(m.esqueleto, h == 1);
        return palma.valida && palma.cierre > 1.45f && Vector3.Dot(palma.normal, Vector3.up) > 0.7f;
    }

    // Aro sobre la cabeza del personaje: se llena al apagar con la palma arriba.
    void ActualizarAros()
    {
        for (int h = 0; h < 2; h++)
        {
            var m = manos[h];
            float lleno = 0f;
            if (m != null && m.listo)
            {
                if (palmaArribaDesde[h] >= 0f)
                    lleno = Mathf.Clamp01((Time.time - palmaArribaDesde[h]) / TiempoPalmaArriba);
            }
            bool ver = lleno > 0.01f && materialIndicador != null;
            if (!ver)
            {
                if (aros[h] != null && aros[h].gameObject.activeSelf)
                    aros[h].gameObject.SetActive(false);
                continue;
            }
            if (aros[h] == null)
                CrearAro(h);
            var aro = aros[h];
            if (!aro.gameObject.activeSelf)
                aro.gameObject.SetActive(true);
            float tam = Mathf.Clamp(m.LargoPiernaMundo * 0.35f, 0.02f, 0.06f);
            aro.position = Centro(m.datos) + Vector3.up * (m.LargoPiernaMundo * 1.9f);
            if (control != null && control.Cabeza != null)
            {
                Vector3 mirar = aro.position - control.Cabeza.position;
                if (mirar.sqrMagnitude > 1e-6f)
                    aro.rotation = Quaternion.LookRotation(mirar, Vector3.up);
            }
            aro.localScale = Vector3.one * tam;
            rellenos[h].localScale = Vector3.one * (0.6f * lleno);
        }
    }

    void CrearAro(int h)
    {
        var go = new GameObject("AroTitere" + h);
        go.AddComponent<MeshFilter>().sharedMesh = ControlManos.MallaAnillo();
        PintarAro(go.AddComponent<MeshRenderer>());
        var relleno = new GameObject("Relleno");
        relleno.transform.SetParent(go.transform, false);
        relleno.AddComponent<MeshFilter>().sharedMesh = ControlManos.MallaDisco();
        PintarAro(relleno.AddComponent<MeshRenderer>());
        aros[h] = go.transform;
        rellenos[h] = relleno.transform;
    }

    void PintarAro(MeshRenderer r)
    {
        r.sharedMaterial = materialIndicador;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // La cadera del personaje, en el mundo.
    Vector3 Centro(DatosPersonaje p)
    {
        var a = dibujo.BuscarPorId(p.pierna1);
        var b = dibujo.BuscarPorId(p.pierna2);
        Vector3 suma = Vector3.zero;
        int n = 0;
        if (a != null && a.nodos.Count > 0) { suma += a.nodos[0]; n++; }
        if (b != null && b.nodos.Count > 0) { suma += b.nodos[0]; n++; }
        return n > 0 ? dibujo.transform.TransformPoint(suma / n) : new Vector3(0f, -100f, 0f);
    }

    // ---------- Pisos ----------

    // La línea-piso más alta que se pueda pisar en "x" (a lo largo del caminar), o "reposo" si no hay.
    public float AlturaPiso(Vector3 origen, Vector3 adelante, Vector3 arriba, Vector3 lado, float profundidad, float x, float desde, float subir, float reposo)
    {
        float mejor = float.NegativeInfinity;
        bool hay = false;
        Vector3 o = EnPlano(origen);
        foreach (int id in pisos)
        {
            var t = dibujo.BuscarPorId(id);
            if (t == null || !Dibujo.Editable(t))
                continue;
            var c = t.curva;
            for (int i = 1; i < c.Count; i++)
            {
                // En Plano (2D) el personaje se ve sobre el plano: el piso se compara igual, sobre el plano.
                Vector3 a = EnPlano(c[i - 1]) - o;
                Vector3 b = EnPlano(c[i]) - o;
                float xa = Vector3.Dot(a, adelante);
                float xb = Vector3.Dot(b, adelante);
                if ((x < xa && x < xb) || (x > xa && x > xb))
                    continue;
                float u = Mathf.Abs(xb - xa) > 1e-6f ? (x - xa) / (xb - xa) : 0f;
                // Solo cuenta un piso que esté a la misma profundidad que el personaje
                // (los pisos de otros personajes, más adelante o más atrás, no lo afectan).
                if (lado.sqrMagnitude > 0.5f && Mathf.Abs(Mathf.Lerp(Vector3.Dot(a, lado), Vector3.Dot(b, lado), u)) > profundidad)
                    continue;
                float y = Mathf.Lerp(Vector3.Dot(a, arriba), Vector3.Dot(b, arriba), u);
                if (y <= desde + subir && y > mejor)
                {
                    mejor = y;
                    hay = true;
                }
            }
        }
        return hay ? mejor : reposo;
    }

    Vector3 EnPlano(Vector3 local)
    {
        return dibujo.PlanoActivo ? dibujo.ProyectarEnPlano(local) : local;
    }

    // ---------- Biblioteca de ciclos ----------

    void CargarBiblioteca()
    {
        try
        {
            if (File.Exists(RutaBiblioteca))
                biblioteca = JsonUtility.FromJson<BibliotecaCiclos>(File.ReadAllText(RutaBiblioteca)) ?? new BibliotecaCiclos();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudieron leer los ciclos: " + e.Message);
        }
        if (biblioteca.ciclos == null)
            biblioteca.ciclos = new List<CicloCaminado>();
    }

    void GuardarBiblioteca()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaBiblioteca));
            File.WriteAllText(RutaBiblioteca, JsonUtility.ToJson(biblioteca, true));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudieron guardar los ciclos: " + e.Message);
        }
    }

    // Toma las claves de la animación donde están las piernas del personaje elegido y las guarda como ciclo.
    public void GuardarCiclo()
    {
        var p = Elegido;
        if (dibujo == null || animacion == null || Encendido || !PersonajeValido(p))
        {
            Mensaje("Primero crea o elige un personaje");
            return;
        }
        var m = new Marioneta(this, p, false);
        if (!m.Preparar())
            return;
        var claves = new List<Clave>();
        foreach (var c in animacion.claves)
            if (Pose(c, p.pierna1) != null && Pose(c, p.pierna2) != null)
                claves.Add(c);
        if (claves.Count < 3)
        {
            Mensaje("Anima al menos 3 claves de un paso (la última igual a la primera)");
            return;
        }
        int f0 = claves[0].fotograma;
        int f1 = claves[claves.Count - 1].fotograma;
        var nuevo = new CicloCaminado { nombre = "Mío " + (biblioteca.ciclos.Count + 1) };
        Vector3 cadera0 = Vector3.zero;
        for (int k = 0; k < claves.Count; k++)
        {
            var a = Pose(claves[k], p.pierna1);
            var b = Pose(claves[k], p.pierna2);
            Vector3 cadera = (a.nodos[0] + b.nodos[0]) * 0.5f;
            if (k == 0)
                cadera0 = cadera;
            var pose = new PoseCiclo
            {
                tiempo = f1 > f0 ? (claves[k].fotograma - f0) / (float)(f1 - f0) : 0f,
                bob = m.Normalizar(cadera - cadera0).y,
            };
            pose.pierna1 = m.NormalizarLista(a.nodos, cadera);
            pose.pierna2 = m.NormalizarLista(b.nodos, cadera);
            var c1 = Pose(claves[k], p.brazo1);
            var c2 = Pose(claves[k], p.brazo2);
            if (c1 != null) pose.brazo1 = m.NormalizarLista(c1.nodos, cadera);
            if (c2 != null) pose.brazo2 = m.NormalizarLista(c2.nodos, cadera);
            nuevo.poses.Add(pose);
        }
        biblioteca.ciclos.Add(nuevo);
        GuardarBiblioteca();
        p.ciclo = biblioteca.ciclos.Count + CiclosFijos - 1;
        Mensaje("Ciclo guardado: " + nuevo.nombre + " (" + claves.Count + " poses)");
    }

    static DatosTrazo Pose(Clave c, int id)
    {
        if (id <= 0)
            return null;
        foreach (var p in c.trazos)
            if (p != null && p.id == id && p.nodos != null && p.nodos.Count >= 2)
                return p;
        return null;
    }

    // ---------- Dedos (para posar) ----------

    void LeerDedos(float suave)
    {
        dedosValidos = false;
        var mano = control.Der;
        if (mano == null || !mano.valida || mano.esqueleto == null)
            return;
        var raiz = dibujo.transform;
        var nuevos = new Vector3[2, 4];
        for (int f = 0; f < 2; f++)
        {
            for (int j = 0; j < 4; j++)
            {
                var h = ManosUtil.Hueso(mano.esqueleto, Dedos[f][j]);
                if (h == null)
                    return;
                nuevos[f, j] = raiz.InverseTransformPoint(h.position);
            }
        }
        var palma = ManosUtil.LeerPalma(mano.esqueleto, false);
        var m = ManosUtil.Hueso(mano.esqueleto, Muneca);
        if (!palma.valida || m == null)
            return;
        float a = teniaDedos ? 1f - Mathf.Exp(-suave * Time.deltaTime) : 1f;
        for (int f = 0; f < 2; f++)
            for (int j = 0; j < 4; j++)
                dedos[f, j] = Vector3.Lerp(dedos[f, j], nuevos[f, j], a);
        normalPalma = Vector3.Slerp(normalPalma, raiz.InverseTransformDirection(palma.normal).normalized, a);
        munecaDedos = Vector3.Lerp(munecaDedos, raiz.InverseTransformPoint(m.position), a);
        teniaDedos = true;
        dedosValidos = true;
    }

    // ---------- Crear personajes ----------

    public void CargarMuneco()
    {
        if (dibujo == null || Encendido || fase != Fase.Libre)
            return;
        Transform cab = control != null ? control.Cabeza : null;
        Vector3 ojos = cab != null ? cab.position : new Vector3(0f, 1.5f, 0f);
        Vector3 frente = cab != null ? cab.forward : Vector3.forward;
        frente.y = 0f;
        if (frente.sqrMagnitude < 1e-4f)
            frente = Vector3.forward;
        frente.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, frente).normalized;
        // Cada personaje nuevo, un poco más a la derecha del anterior.
        Vector3 cadera = ojos + frente * 0.55f - Vector3.up * 0.15f + derecha * (0.28f * (personajes.Count % 3) - 0.1f);

        dibujo.GuardarParaDeshacer();
        var cons = new ConstructorPersonaje(dibujo, cadera, derecha, dibujo.AnchoNuevoLocal());
        DatosPersonaje p;
        float pies;
        if (tipo == 0)
            p = cons.Palito(out pies);
        else
            p = cons.Dibujado(tipo, out pies);
        // Un piso bajo sus pies.
        p.piso = cons.Piso(pies);
        pisos.Add(p.piso);
        p.arriba = dibujo.transform.InverseTransformDirection(Vector3.up).normalized;
        MoverACapaPersonajes(p);
        personajes.Add(p);
        elegido = personajes.Count - 1;
        // Se enciende solo con la mano derecha (sin tener que chocar los cinco).
        Encender(0, p);
    }

    // Tutorial: un Palito parado con los pies en "pies", mirando hacia "derecha", sobre la línea "piso"
    // (tu línea: no se borra con el personaje). Se enciende solo con la mano derecha.
    public DatosPersonaje CrearPalitoEn(Vector3 pies, Vector3 derecha, int piso)
    {
        if (dibujo == null || Encendido || fase != Fase.Libre)
            return null;
        derecha.y = 0f;
        derecha = derecha.sqrMagnitude > 1e-6f ? derecha.normalized : Vector3.right;
        Vector3 cadera = pies + Vector3.up * 0.172f;
        dibujo.GuardarParaDeshacer();
        var cons = new ConstructorPersonaje(dibujo, cadera, derecha, dibujo.AnchoNuevoLocal());
        float alturaPies;
        var p = cons.Palito(out alturaPies);
        p.arriba = dibujo.transform.InverseTransformDirection(Vector3.up).normalized;
        if (piso > 0 && !pisos.Contains(piso))
            pisos.Add(piso);
        MoverACapaPersonajes(p);
        personajes.Add(p);
        elegido = personajes.Count - 1;
        Encender(0, p);
        return p;
    }

    // ==================== Una marioneta encendida ====================

    class Rol
    {
        public Trazo trazo;
        public DatosTrazo reposo;
        public List<Vector3> rel;   // nodos de reposo respecto a la cadera
        public float[] fraccion;    // dónde cae cada nodo a lo largo de la línea
        public float largo;
        public float profundidad;   // metros hacia ti
        public int miembro = -1;    // si es una parte pegada: a qué miembro y tramo
        public int tramo;
    }

    class Marioneta
    {
        // Caminado "de manual": fase, muslo (grados, + adelante), rodilla doblada, pie (+ punta arriba).
        static readonly float[,] Tabla =
        {
            { 0.000f,  25f,  3f,  20f }, { 0.125f,  18f, 20f,   0f }, { 0.250f,   0f,  8f,   0f }, { 0.375f, -15f,  8f, -20f },
            { 0.500f, -22f, 12f, -35f }, { 0.625f, -12f, 45f, -55f }, { 0.750f,  10f, 70f, -35f }, { 0.875f,  30f, 35f,  -5f },
        };
        // Caminado con estilo (Richard Williams): pasos más grandes y la posición de paso alta.
        static readonly float[,] TablaEstilo =
        {
            { 0.000f,  30f,  4f,  25f }, { 0.125f,  22f, 30f,   0f }, { 0.250f,  -3f, 10f,   0f }, { 0.375f, -20f, 10f, -25f },
            { 0.500f, -27f, 14f, -40f }, { 0.625f, -14f, 55f, -60f }, { 0.750f,  12f, 85f, -40f }, { 0.875f,  36f, 40f,  -5f },
        };
        // Caminado sigiloso (Ken Harris, "sneak"): agachado, pasos largos, el pie pasa RÁPIDO por el medio
        // (rodilla alta) y se apoya con cuidado, de puntitas.
        static readonly float[,] TablaSigilo =
        {
            { 0.000f,  32f,  30f, -10f }, { 0.125f,  24f,  45f,   0f }, { 0.250f,   8f,  50f,   0f }, { 0.375f, -10f,  45f,  -5f },
            { 0.500f, -24f,  38f, -30f }, { 0.625f, -14f,  85f, -55f }, { 0.750f,  22f, 115f, -35f }, { 0.875f,  42f,  60f, -25f },
        };
        static readonly float[,] TablaCorrer =
        {
            { 0.000f,  35f,  10f,  15f }, { 0.125f,  20f,  35f,   0f }, { 0.250f,  -5f,  25f, -10f }, { 0.375f, -30f,  30f, -40f },
            { 0.500f, -35f,  60f, -50f }, { 0.625f, -10f, 110f, -40f }, { 0.750f,  25f, 100f, -20f }, { 0.875f,  45f,  45f,   5f },
        };

        const int P1 = 0, P2 = 1, B1 = 2, B2 = 3, CAB = 4;

        readonly Titere t;
        public readonly DatosPersonaje datos;
        readonly bool izquierda;
        public bool listo;
        public float preparadoHasta;

        readonly Rol[] roles = new Rol[5];
        readonly List<Rol> cuerpoRoles = new List<Rol>();
        readonly List<Rol> partes = new List<Rol>();
        readonly List<Trazo> respaldoTrazos = new List<Trazo>();
        readonly List<DatosTrazo> respaldoDatos = new List<DatosTrazo>();

        Vector3 caderaReposo, adelante = Vector3.right, arriba = Vector3.up, lado = Vector3.forward, haciaTi = Vector3.back;
        float largoPierna = 0.2f;
        readonly float[] muslo = new float[2], canilla = new float[2], pie = new float[2];
        readonly float[] brazoSup = new float[2], antebrazo = new float[2];
        float pieReposo;

        // Mano
        Vector3 munecaLocal, munecaCal;
        bool munecaValida, teniaMuneca;
        float ultimaMunecaValida = -10f;
        float recorridoPrevio, velocidad, velocidadPrevia, faseCiclo, peso, correr, velocidadVertical;
        int mirando = 1;
        Vector3 raizOffset;
        float finSalto = -10f;       // cuándo aterrizó (para no saltar dos veces seguidas)
        float agacharMano;           // mano abajo (más de 4 cm) = agacharse (suave)
        const float elevar = 0f;     // subir la mano no lo eleva (así nunca flota)
        bool sigilo;                 // caminado sigiloso (Ken Harris)

        // Salto, aterrizaje e inercia
        enum Salto { Suelo, Sostenido, Anticipa, Aire }
        Salto salto = Salto.Suelo;
        float saltoDesde, pieY, velSalto, velInicial = 1f;
        float escalaY = 1f, velEscala;
        float golpe, velGolpe;       // agacharse al caer (resorte con rebote)
        float agacharAuto;           // anticipación automática
        float inercia, velInercia;   // cabeza y tronco siguen al frenar (solo después de correr)
        float corrioHasta = -10f;    // última vez que iba corriendo rápido
        bool frenoDado;              // el empujón de la inercia ya se dio en este frenón
        public bool congelado;       // quieto mientras pones la palma hacia arriba

        public float LargoPiernaMundo => largoPierna * Dibujo.EscalaMundo;

        // Últimas poses de los miembros (para las partes pegadas)
        readonly List<Vector3>[] miembros = new List<Vector3>[6];

        public Marioneta(Titere titere, DatosPersonaje p, bool manoIzquierda)
        {
            t = titere;
            datos = p;
            izquierda = manoIzquierda;
        }

        Dibujo Dibujo => t.dibujo;
        public bool ManoValida => munecaValida;

        ManoSeguida Mano => izquierda ? t.control.Izq : t.control.Der;

        // ---------- Preparar ----------

        Rol CrearRol(int id)
        {
            var tr = Dibujo.BuscarPorId(id);
            if (tr == null || tr.nodos.Count < 2)
                return null;
            tr.visibleAnim = true;
            var r = new Rol { trazo = tr, reposo = tr.CrearDatos() };
            var nodos = r.reposo.nodos;
            r.largo = 0f;
            for (int k = 1; k < nodos.Count; k++)
                r.largo += Vector3.Distance(nodos[k - 1], nodos[k]);
            r.fraccion = new float[nodos.Count];
            float acum = 0f;
            for (int k = 0; k < nodos.Count; k++)
            {
                if (k > 0)
                    acum += Vector3.Distance(nodos[k - 1], nodos[k]);
                r.fraccion[k] = r.largo > 1e-6f ? acum / r.largo : 0f;
            }
            respaldoTrazos.Add(tr);
            respaldoDatos.Add(tr.CrearDatos());
            return r;
        }

        // Lee la forma actual ("de pie") y calcula hacia dónde mira y el tamaño.
        public bool Preparar()
        {
            respaldoTrazos.Clear();
            respaldoDatos.Clear();
            roles[P1] = CrearRol(datos.pierna1);
            roles[P2] = CrearRol(datos.pierna2);
            roles[B1] = CrearRol(datos.brazo1);
            roles[B2] = CrearRol(datos.brazo2);
            roles[CAB] = CrearRol(datos.cabeza);
            if (roles[P1] == null || roles[P2] == null)
                return false;

            var a = roles[P1].reposo.nodos;
            var b = roles[P2].reposo.nodos;
            caderaReposo = (a[0] + b[0]) * 0.5f;
            Vector3 abajo = (a[a.Count - 1] - a[0]) + (b[b.Count - 1] - b[0]);
            if (abajo.sqrMagnitude < 1e-10f)
                return false;
            Vector3 arribaPiernas = -abajo.normalized;
            Vector3 frente = (a[a.Count - 1] - a[Mathf.Max(0, a.Count - 2)]) + (b[b.Count - 1] - b[Mathf.Max(0, b.Count - 2)]);
            frente += (a[a.Count / 2] - (a[0] + a[a.Count - 1]) * 0.5f) + (b[b.Count / 2] - (b[0] + b[b.Count - 1]) * 0.5f);
            frente = Vector3.ProjectOnPlane(frente, arribaPiernas);
            if (frente.sqrMagnitude < 1e-10f)
                frente = Vector3.ProjectOnPlane(Dibujo.transform.InverseTransformDirection(Vector3.right), arribaPiernas);
            if (frente.sqrMagnitude < 1e-10f)
                frente = Vector3.Cross(arribaPiernas, Vector3.forward);
            // "Arriba" = la gravedad del mundo real (no lo que digan las piernas dibujadas),
            // dentro del plano donde está dibujado el personaje.
            Vector3 normalPersonaje = Vector3.Cross(arribaPiernas, frente.normalized);
            // El "arriba" de cuando se creó (gira junto con el dibujo, igual que su piso).
            Vector3 mundoArriba = datos.arriba.sqrMagnitude > 0.5f ? datos.arriba.normalized
                : Dibujo.transform.InverseTransformDirection(Vector3.up).normalized;
            Vector3 gravedad = normalPersonaje.sqrMagnitude > 1e-8f
                ? Vector3.ProjectOnPlane(mundoArriba, normalPersonaje.normalized) : mundoArriba;
            arriba = gravedad.sqrMagnitude > 0.25f ? gravedad.normalized : arribaPiernas;
            frente = Vector3.ProjectOnPlane(frente, arriba);
            if (frente.sqrMagnitude < 1e-10f && normalPersonaje.sqrMagnitude > 1e-8f)
                frente = Vector3.Cross(normalPersonaje, arriba);
            if (frente.sqrMagnitude < 1e-10f)
                frente = Vector3.Cross(arriba, Vector3.forward);
            adelante = frente.normalized * (datos.voltear ? -1f : 1f);
            lado = Vector3.Cross(arriba, adelante).normalized;
            // "Hacia ti": el lado que mira a tu cabeza (lo de adelante tapa a lo de atrás).
            haciaTi = lado;
            var control = t.control;
            if (control != null && control.Cabeza != null)
            {
                Vector3 aCabeza = Dibujo.transform.InverseTransformPoint(control.Cabeza.position) - caderaReposo;
                if (Vector3.Dot(aCabeza, lado) < 0f)
                    haciaTi = -lado;
            }
            largoPierna = Mathf.Max(1e-4f, (roles[P1].largo + roles[P2].largo) * 0.5f);

            for (int i = 0; i < 2; i++)
            {
                var r = roles[i];
                var n = r.reposo.nodos;
                if (n.Count == 4)
                {
                    muslo[i] = Vector3.Distance(n[0], n[1]);
                    canilla[i] = Vector3.Distance(n[1], n[2]);
                    pie[i] = Vector3.Distance(n[2], n[3]);
                }
                else
                {
                    muslo[i] = r.largo * 0.47f;
                    canilla[i] = r.largo * 0.43f;
                    pie[i] = r.largo * 0.10f;
                }
            }
            for (int j = 0; j < 2; j++)
            {
                var r = roles[B1 + j];
                if (r == null)
                    continue;
                var n = r.reposo.nodos;
                if (n.Count == 3)
                {
                    brazoSup[j] = Vector3.Distance(n[0], n[1]);
                    antebrazo[j] = Vector3.Distance(n[1], n[2]);
                }
                else
                {
                    brazoSup[j] = r.largo * 0.5f;
                    antebrazo[j] = r.largo * 0.5f;
                }
            }
            foreach (var r in roles)
                if (r != null)
                    r.rel = Relativos(r.reposo.nodos);

            cuerpoRoles.Clear();
            foreach (int id in datos.cuerpo)
            {
                if (id == datos.pierna1 || id == datos.pierna2 || id == datos.brazo1 || id == datos.brazo2 || id == datos.cabeza)
                    continue;
                var r = CrearRol(id);
                if (r == null)
                    continue;
                r.rel = Relativos(r.reposo.nodos);
                cuerpoRoles.Add(r);
            }
            partes.Clear();
            foreach (var pg in datos.pegados)
            {
                var r = CrearRol(pg.id);
                if (r == null)
                    continue;
                r.rel = Relativos(r.reposo.nodos);
                r.miembro = pg.miembro;
                r.tramo = pg.tramo;
                // Brazos y piernas un poquito más separados del tronco (el de atrás más atrás,
                // el de adelante más adelante): así al caminar no lo traspasan.
                r.profundidad = pg.miembro >= 0 && pg.miembro < 4 ? pg.profundidad * 1.8f : pg.profundidad;
                partes.Add(r);
            }
            // Palito (o tus dibujos): pierna 2 y brazo 2 van detrás; pierna 1 y brazo 1, delante.
            if (roles[P1] != null) roles[P1].profundidad = 0.004f;
            if (roles[P2] != null) roles[P2].profundidad = -0.006f;
            if (roles[B1] != null) roles[B1].profundidad = 0.007f;
            if (roles[B2] != null) roles[B2].profundidad = -0.009f;

            pieReposo = 0f;
            for (int i = 0; i < 2; i++)
                foreach (var v in roles[i].rel)
                    pieReposo = Mathf.Min(pieReposo, Vector3.Dot(v, arriba));
            Dibujo.ActualizarVisibilidad();
            return true;
        }

        List<Vector3> Relativos(List<Vector3> nodos)
        {
            var l = new List<Vector3>(nodos.Count);
            foreach (var n in nodos)
                l.Add(n - caderaReposo);
            return l;
        }

        public Vector3 Normalizar(Vector3 r)
        {
            return new Vector3(Vector3.Dot(r, adelante), Vector3.Dot(r, arriba), Vector3.Dot(r, lado)) / largoPierna;
        }

        public List<Vector3> NormalizarLista(List<Vector3> nodos, Vector3 cadera)
        {
            var l = new List<Vector3>();
            foreach (var n in nodos)
                l.Add(Normalizar(n - cadera));
            return l;
        }

        Vector3 Desnormalizar(Vector3 v)
        {
            return (adelante * v.x + arriba * v.y + lado * v.z) * largoPierna;
        }

        public void Calibrar()
        {
            munecaCal = munecaLocal;
            recorridoPrevio = 0f;
            velocidad = velocidadPrevia = 0f;
            peso = correr = 0f;
            faseCiclo = 0f;
            mirando = 1;
            raizOffset = Vector3.zero;
            finSalto = -10f;
            velocidadVertical = 0f;
            agacharMano = 0f;
            salto = Salto.Suelo;
            pieY = pieReposo;
            velSalto = 0f;
            escalaY = 1f;
            velEscala = golpe = velGolpe = agacharAuto = inercia = velInercia = 0f;
            frenoDado = false;
            corrioHasta = -10f;
            congelado = false;
            listo = true;
        }

        public void Restaurar()
        {
            bool antes = Trazo.silenciar;
            Trazo.silenciar = true;
            var anim = t.animacion;
            if (anim != null && anim.Activa)
            {
                anim.MostrarFotograma();
            }
            else
            {
                for (int i = 0; i < respaldoTrazos.Count; i++)
                    if (respaldoTrazos[i] != null)
                        respaldoTrazos[i].AplicarPose(respaldoDatos[i], null, 0f);
            }
            Trazo.silenciar = antes;
            Trazo.huboCambio = false;
        }

        // ---------- La mano ----------

        public void LeerMuneca(bool primera)
        {
            bool ok = false;
            var mano = Mano;
            if (mano != null && mano.valida && mano.esqueleto != null)
            {
                var m = ManosUtil.Hueso(mano.esqueleto, Muneca);
                if (m != null)
                {
                    Vector3 nueva = Dibujo.transform.InverseTransformPoint(m.position);
                    float a = teniaMuneca && !primera ? 1f - Mathf.Exp(-12f * Time.deltaTime) : 1f;
                    munecaLocal = Vector3.Lerp(munecaLocal, nueva, a);
                    teniaMuneca = true;
                    ultimaMunecaValida = Time.time;
                    ok = true;
                }
            }
            // Si Meta pierde la mano un instante (medio segundo), seguimos con la última posición.
            munecaValida = ok || (teniaMuneca && Time.time - ultimaMunecaValida < 0.5f);
        }

        public void Actualizar(float dt)
        {
            Vector3 antes = munecaLocal;
            LeerMuneca(false);
            if (!munecaValida)
            {
                velocidad = Mathf.Lerp(velocidad, 0f, 1f - Mathf.Exp(-6f * dt));
                peso = Mathf.Lerp(peso, 0f, 1f - Mathf.Exp(-4f * dt));
                agacharMano = Mathf.Lerp(agacharMano, 0f, 1f - Mathf.Exp(-4f * dt));
            }
            else
            {
                Vector3 d = munecaLocal - munecaCal;
                float recorrido = Vector3.Dot(d, adelante);
                raizOffset = adelante * recorrido;
                float paso = recorrido - recorridoPrevio;
                recorridoPrevio = recorrido;
                velocidad = Mathf.Lerp(velocidad, paso / dt, 1f - Mathf.Exp(-6f * dt));
                // Arrancar: pasa por caminar antes de correr (se nota la transición).
                float rapidez = Mathf.Abs(velocidad) / largoPierna;
                float objetivoCorrer = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.3f, 2.4f, rapidez));
                float tasa = objetivoCorrer > correr ? 2.2f : 4f;
                correr = Mathf.Lerp(correr, objetivoCorrer, 1f - Mathf.Exp(-tasa * dt));
                sigilo = datos.ciclo == 2;
                faseCiclo = Mathf.Repeat(faseCiclo + Mathf.Abs(paso) / (largoPierna * Mathf.Lerp(sigilo ? 2.1f : 1.5f, 2.6f, correr)), 1f);
                if (velocidad > largoPierna * 0.2f)
                    mirando = 1;
                else if (velocidad < -largoPierna * 0.2f)
                    mirando = -1;
                float objetivo = Mathf.Clamp01(Mathf.Abs(velocidad) / (largoPierna * 0.8f));
                peso = Mathf.Lerp(peso, objetivo, 1f - Mathf.Exp(-4f * dt));

                // Frenar DESPUÉS DE CORRER: la cabeza y el tronco siguen un poquito (inercia) y vuelven
                // con un rebote. Al caminar o moverlo despacio no pasa nada.
                if (rapidez > 1.8f)
                {
                    corrioHasta = Time.time;
                    frenoDado = false;
                }
                else if (!frenoDado && rapidez < 0.4f && Time.time - corrioHasta < 0.5f)
                {
                    frenoDado = true;
                    velInercia += 3.2f;
                }
                velInercia += (-inercia * 150f - velInercia * 11f) * dt;
                inercia = Mathf.Clamp(inercia + velInercia * dt, -0.25f, 0.35f);
                velocidadPrevia = velocidad;

                // SALTO (como antes): golpe rápido de la mano hacia arriba. Se agacha solo un instante
                // (anticipación), despega, cae por la gravedad, se aplasta al caer y rebota.
                // AGACHARSE: baja la mano más de 4 cm (los temblores normales no hacen nada). Suave.
                float bajada = -Vector3.Dot(d, arriba) * Dibujo.EscalaMundo - 0.04f;
                float objetivoAgachar = Mathf.Clamp(bajada / Dibujo.EscalaMundo, 0f, largoPierna * 0.5f);
                agacharMano = Mathf.Lerp(agacharMano, objetivoAgachar, 1f - Mathf.Exp(-8f * dt));
                float vy = Vector3.Dot(munecaLocal - antes, arriba) / dt * Dibujo.EscalaMundo;
                velocidadVertical = Mathf.Lerp(velocidadVertical, vy, 1f - Mathf.Exp(-25f * dt));
                if (salto == Salto.Suelo && velocidadVertical > 0.8f && Time.time - finSalto > 0.3f)
                {
                    salto = Salto.Anticipa;
                    saltoDesde = Time.time;
                    velInicial = 5.1f * largoPierna;
                }
            }
            ActualizarSalto(dt);
        }

        void Despegar()
        {
            salto = Salto.Aire;
            velSalto = velInicial;
            agacharAuto = 0f;
        }

        void ActualizarSalto(float dt)
        {
            float x = Vector3.Dot(raizOffset, adelante);
            float gravedad = 14.7f * largoPierna;
            float suelo = t.AlturaPiso(caderaReposo, adelante, arriba, lado, Mathf.Max(largoPierna * 0.6f, 0.06f / Dibujo.EscalaMundo),
                                       x, pieY, largoPierna * 0.35f, pieReposo);
            switch (salto)
            {
                case Salto.Suelo:
                    if (suelo < pieY - largoPierna * 0.05f)
                    {
                        salto = Salto.Aire; // se acabó el piso: cae
                        velSalto = 0f;
                        velInicial = largoPierna * 4f;
                    }
                    else
                    {
                        pieY = Mathf.Lerp(pieY, suelo, 1f - Mathf.Exp(-20f * dt));
                        if (elevar > largoPierna * 0.04f)
                            salto = Salto.Sostenido;
                    }
                    break;
                case Salto.Sostenido:
                    // Lo llevas en el aire con la mano (tan alto como quieras).
                    pieY = suelo + elevar;
                    if (elevar < largoPierna * 0.02f)
                    {
                        salto = Salto.Suelo;
                        escalaY = 0.92f;
                        velEscala = 0f;
                    }
                    break;
                case Salto.Anticipa:
                    agacharAuto = Mathf.Lerp(agacharAuto, largoPierna * 0.25f, 1f - Mathf.Exp(-30f * dt));
                    if (Time.time - saltoDesde > 0.12f)
                        Despegar();
                    break;
                case Salto.Aire:
                    velSalto -= gravedad * dt;
                    pieY += velSalto * dt;
                    if (velSalto < 0f && pieY <= suelo)
                    {
                        // Aterriza: se agacha más mientras más fuerte cayó, y rebota.
                        float impacto = Mathf.Clamp01(-velSalto / (6f * largoPierna));
                        pieY = suelo;
                        salto = Salto.Suelo;
                        finSalto = Time.time;
                        velGolpe = Mathf.Lerp(1.2f, 4.2f, impacto) * largoPierna;
                        escalaY = 1f - 0.3f * Mathf.Max(0.3f, impacto);
                        velEscala = 0f;
                    }
                    break;
            }
            if (salto != Salto.Anticipa)
                agacharAuto = Mathf.Lerp(agacharAuto, 0f, 1f - Mathf.Exp(-15f * dt));
            // Resorte del golpe al caer (se agacha y vuelve con un pequeño rebote).
            velGolpe += (-golpe * 180f - velGolpe * 12f) * dt;
            golpe += velGolpe * dt;
            golpe = Mathf.Clamp(golpe, -largoPierna * 0.12f, largoPierna * 0.5f);
            if (salto == Salto.Suelo)
            {
                velEscala += (-(escalaY - 1f) * 260f - velEscala * 14f) * dt;
                escalaY += velEscala * dt;
            }
            else
            {
                float objetivo = salto == Salto.Anticipa ? 0.85f
                               : salto == Salto.Sostenido ? 1.05f
                               : velSalto > 0.3f * velInicial ? 1.15f : velSalto < -0.3f * velInicial ? 1.08f : 0.95f;
                escalaY = Mathf.Lerp(escalaY, objetivo, 1f - Mathf.Exp(-18f * dt));
            }
            escalaY = Mathf.Clamp(escalaY, 0.6f, 1.3f);
        }

        float Agachado => salto == Salto.Aire || salto == Salto.Sostenido ? 0f
            : agacharMano + agacharAuto + golpe;

        // ---------- Poner la pose ----------

        public void PonerPose()
        {
            bool antes = Trazo.silenciar;
            Trazo.silenciar = true;
            var cicloGuardado = t.CicloGuardado(datos.ciclo);
            bool estilo = datos.ciclo == 1;
            float aire = salto == Salto.Aire ? 1f : salto == Salto.Sostenido ? Mathf.Clamp01(elevar / (largoPierna * 0.3f)) : 0f;
            float bobCiclo = cicloGuardado != null ? BobGuardado(cicloGuardado, faseCiclo) : BobManual(faseCiclo, estilo);
            sigilo = datos.ciclo == 2;
            float bob = bobCiclo * peso * (1f - aire);
            // Sigiloso: el cuerpo va más bajo (rodillas dobladas) y casi sin rebotar.
            float bajo = sigilo ? 0.1f * largoPierna * peso * (1f - aire) * (1f - correr) : 0f;
            if (sigilo)
                bob = bob * 0.4f - bajo;
            float agachado = Agachado;
            float altura = (pieY - pieReposo) - agachado;
            Vector3 desplazar = raizOffset + arriba * (altura + bob);

            for (int i = 0; i < 4; i++)
            {
                var r = roles[i];
                miembros[i] = null;
                if (r == null)
                    continue;
                List<Vector3> pose = null;
                if (cicloGuardado != null && aire <= 0f)
                    pose = PoseGuardada(cicloGuardado, i, faseCiclo, r);
                if (pose == null)
                    pose = i < 2 ? PiernaManual(i, faseCiclo + (i == 1 ? 0.5f : 0f), r, estilo) : BrazoManual(i - 2, faseCiclo, r, estilo);
                for (int k = 0; k < pose.Count; k++)
                    pose[k] = Vector3.Lerp(r.rel[k], pose[k], peso);
                if (aire > 0f)
                {
                    var poseAire = i < 2 ? PiernaAire(i, r) : BrazoAire(i - 2, r);
                    for (int k = 0; k < pose.Count; k++)
                        pose[k] = Vector3.Lerp(pose[k], poseAire[k], aire);
                }
                if (i < 2 && agachado > 1e-4f)
                    Agacharse(pose, agachado, i);
                if (i >= 2)
                    Inclinar(pose, 1f);
                miembros[i] = pose;
                Aplicar(r, pose, desplazar);
            }

            // Cabeza: rebota un poquito después que el cuerpo; con estilo, se inclina en el paso.
            float bobCabeza = aire > 0f ? 0f : (cicloGuardado != null ? BobGuardado(cicloGuardado, faseCiclo - 0.06f)
                                              : BobManual(faseCiclo - (estilo ? 0.1f : 0.06f), estilo)) * peso;
            Vector3 extraCabeza = estilo ? adelante * (0.04f * largoPierna * Mathf.Max(0f, Mathf.Sin(faseCiclo * Mathf.PI * 4f)) * peso) : Vector3.zero;
            if (sigilo)
            {
                bobCabeza = bobCabeza * 0.4f - bajo;
                extraCabeza = adelante * (0.06f * largoPierna * peso);
            }
            Vector3 desplazarCabeza = raizOffset + arriba * (altura + bobCabeza) + extraCabeza;
            if (roles[CAB] != null)
            {
                var rel = new List<Vector3>(roles[CAB].rel);
                Inclinar(rel, 1.3f);
                miembros[5] = rel;
                Aplicar(roles[CAB], rel, desplazarCabeza);
            }
            foreach (var r in cuerpoRoles)
            {
                var rel = new List<Vector3>(r.rel);
                Inclinar(rel, 1f);
                Aplicar(r, rel, desplazar);
            }
            // Partes pegadas a los huesos (pantalón, zapatos, brazos, cabeza...).
            foreach (var r in partes)
            {
                var rel = new List<Vector3>(r.rel);
                if (r.miembro == 4)
                {
                    Inclinar(rel, 1f);
                    Aplicar(r, rel, desplazar);
                }
                else if (r.miembro == 5)
                {
                    Inclinar(rel, 1.3f);
                    Aplicar(r, rel, desplazarCabeza);
                }
                else if (r.miembro >= 0 && r.miembro < 4 && miembros[r.miembro] != null && roles[r.miembro] != null)
                {
                    Pegar(rel, roles[r.miembro].rel, miembros[r.miembro], r.tramo);
                    Aplicar(r, rel, desplazar);
                }
                else
                {
                    Aplicar(r, rel, desplazar);
                }
            }
            Trazo.silenciar = antes;
            Trazo.huboCambio = false;
        }

        // Una parte sigue a su hueso: se gira con él (en el plano del personaje) desde su punto de inicio.
        void Pegar(List<Vector3> rel, List<Vector3> reposoMiembro, List<Vector3> poseMiembro, int tramo)
        {
            tramo = Mathf.Clamp(tramo, 0, Mathf.Min(reposoMiembro.Count, poseMiembro.Count) - 2);
            Vector3 s0 = reposoMiembro[tramo], e0 = reposoMiembro[tramo + 1];
            Vector3 s1 = poseMiembro[tramo], e1 = poseMiembro[tramo + 1];
            float delta = Angulo(e1 - s1) - Angulo(e0 - s0);
            float c = Mathf.Cos(delta), sn = Mathf.Sin(delta);
            for (int k = 0; k < rel.Count; k++)
            {
                Vector3 q = rel[k] - s0;
                float u = Vector3.Dot(q, adelante);
                float w = -Vector3.Dot(q, arriba);
                float z = Vector3.Dot(q, lado);
                float w2 = w * c - u * sn;
                float u2 = w * sn + u * c;
                rel[k] = s1 + adelante * u2 - arriba * w2 + lado * z;
            }
        }

        // Ángulo de un hueso (0 = hacia abajo, + = hacia adelante), en radianes.
        float Angulo(Vector3 v)
        {
            return Mathf.Atan2(Vector3.Dot(v, adelante), -Vector3.Dot(v, arriba));
        }

        // Al correr se inclina hacia adelante; al frenar, la inercia lo inclina un momento.
        void Inclinar(List<Vector3> rel, float factor)
        {
            float inclinacion = (0.14f * correr * peso + inercia + (sigilo ? 0.25f * peso * (1f - correr) : 0f)) * factor;
            if (Mathf.Abs(inclinacion) < 1e-5f)
                return;
            for (int k = 0; k < rel.Count; k++)
                rel[k] += adelante * Mathf.Max(0f, Vector3.Dot(rel[k], arriba)) * inclinacion;
        }

        // Agacharse de verdad: el cuerpo baja y las rodillas se doblan (los pies se quedan en el piso).
        void Agacharse(List<Vector3> pierna, float cuanto, int i)
        {
            if (pierna.Count != 4)
            {
                for (int k = 1; k < pierna.Count; k++)
                    pierna[k] += arriba * cuanto * (pierna.Count > 1 ? k / (float)(pierna.Count - 1) : 1f);
                return;
            }
            Vector3 cadera = pierna[0];
            Vector3 tobillo = pierna[2] + arriba * cuanto;
            Vector3 punta = pierna[3] + arriba * cuanto;
            float l1 = muslo[i], l2 = canilla[i];
            Vector3 d = tobillo - cadera;
            float dist = Mathf.Clamp(d.magnitude, Mathf.Abs(l1 - l2) + 1e-4f, l1 + l2 - 1e-4f);
            float fi = Angulo(d);
            float beta = Mathf.Acos(Mathf.Clamp((l1 * l1 + dist * dist - l2 * l2) / (2f * l1 * dist), -1f, 1f));
            Vector3 rodilla = cadera + Direccion((fi + beta) * Mathf.Rad2Deg) * l1;
            pierna[1] = rodilla;
            pierna[2] = tobillo;
            pierna[3] = punta;
        }

        void Aplicar(Rol r, List<Vector3> rel, Vector3 desplazar)
        {
            if (r == null || r.trazo == null)
                return;
            var d = Copiar(r.reposo);
            float sy = escalaY;
            float sx = 1f / Mathf.Sqrt(Mathf.Max(0.1f, sy));
            Vector3 fondo = haciaTi * (r.profundidad / Dibujo.EscalaMundo);
            for (int k = 0; k < d.nodos.Count && k < rel.Count; k++)
            {
                Vector3 v = rel[k];
                // Aplastar y estirar, desde los pies.
                float alto = Vector3.Dot(v, arriba);
                Vector3 resto = v - arriba * alto;
                v = resto * sx + arriba * (pieReposo + (alto - pieReposo) * sy);
                if (mirando < 0)
                    v -= 2f * Vector3.Dot(v, adelante) * adelante; // espejo: mira hacia el otro lado
                d.nodos[k] = Dibujo.ProyectarEnPlano(caderaReposo + desplazar + v) + fondo;
            }
            r.trazo.AplicarPose(d, null, 0f);
        }

        // ---------- Caminado, carrera y aire ----------

        static float Interpolar(float[,] tabla, int columna, float fase)
        {
            fase = Mathf.Repeat(fase, 1f);
            int n = tabla.GetLength(0);
            for (int k = 0; k < n; k++)
            {
                float t0 = tabla[k, 0];
                float t1 = k + 1 < n ? tabla[k + 1, 0] : 1f;
                if (fase >= t0 && fase <= t1)
                {
                    float u = t1 > t0 ? (fase - t0) / (t1 - t0) : 0f;
                    u = u * u * (3f - 2f * u);
                    float v1 = k + 1 < n ? tabla[k + 1, columna] : tabla[0, columna];
                    return Mathf.Lerp(tabla[k, columna], v1, u);
                }
            }
            return tabla[0, columna];
        }

        float AnguloTabla(int columna, float fase, bool estilo)
        {
            float camina = Interpolar(sigilo ? TablaSigilo : estilo ? TablaEstilo : Tabla, columna, fase);
            return Mathf.Lerp(camina, Interpolar(TablaCorrer, columna, fase), correr);
        }

        float BobManual(float fase, bool estilo)
        {
            float q = Mathf.Repeat(fase * 2f, 1f);
            float[] camina = estilo ? new[] { -0.02f, -0.07f, 0.05f, 0.03f, -0.02f } : new[] { -0.01f, -0.05f, 0f, 0.03f, -0.01f };
            float[] corre = { -0.03f, -0.07f, 0f, 0.06f, -0.03f };
            float x = q * 4f;
            int i = Mathf.Min(3, Mathf.FloorToInt(x));
            float u = x - i;
            u = u * u * (3f - 2f * u);
            return Mathf.Lerp(Mathf.Lerp(camina[i], camina[i + 1], u), Mathf.Lerp(corre[i], corre[i + 1], u), correr) * largoPierna;
        }

        Vector3 Direccion(float grados)
        {
            float r = grados * Mathf.Deg2Rad;
            return -arriba * Mathf.Cos(r) + adelante * Mathf.Sin(r);
        }

        List<Vector3> Pierna(int i, Rol r, float aMuslo, float rodilla, float aPieGrados)
        {
            float aPie = aPieGrados * Mathf.Deg2Rad;
            Vector3 cadera = r.rel[0];
            Vector3 rod = cadera + Direccion(aMuslo) * muslo[i];
            Vector3 tobillo = rod + Direccion(aMuslo - rodilla) * canilla[i];
            Vector3 punta = tobillo + (adelante * Mathf.Cos(aPie) + arriba * Mathf.Sin(aPie)) * pie[i];
            return SobreCadena(new[] { cadera, rod, tobillo, punta }, r);
        }

        List<Vector3> PiernaManual(int i, float fase, Rol r, bool estilo)
        {
            return Pierna(i, r, AnguloTabla(1, fase, estilo), AnguloTabla(2, fase, estilo), AnguloTabla(3, fase, estilo));
        }

        // En el aire: estirado al subir, recogido arriba, estirado hacia el piso al bajar; colgando si lo sostienes.
        void PesosAire(out float subir, out float arribaPico, out float bajar)
        {
            if (salto == Salto.Sostenido)
            {
                subir = 0f; arribaPico = 0f; bajar = 0f;
                return;
            }
            float tt = velInicial > 1e-5f ? Mathf.Clamp(velSalto / velInicial, -1f, 1f) : 0f;
            arribaPico = Mathf.Clamp01(1f - Mathf.Abs(tt) * 1.6f);
            subir = tt > 0f ? 1f - arribaPico : 0f;
            bajar = tt <= 0f ? 1f - arribaPico : 0f;
        }

        List<Vector3> PiernaAire(int i, Rol r)
        {
            float s, p, b;
            PesosAire(out s, out p, out b);
            float colgar = 1f - s - p - b;
            float otra = i == 0 ? 0f : 1f;
            float aM = s * (-8f - 6f * otra) + p * (50f - 20f * otra) + b * (15f - 10f * otra) + colgar * (2f - 6f * otra);
            float rod = s * 5f + p * 95f + b * 15f + colgar * 12f;
            float aP = s * -45f + p * -10f + b * 15f + colgar * -35f;
            return Pierna(i, r, aM, rod, aP);
        }

        List<Vector3> Brazo(int j, Rol r, float hombroGrados, float codoGrados, float antebrazoAntes)
        {
            Vector3 hombro = r.rel[0];
            Vector3 c = hombro + Direccion(hombroGrados) * brazoSup[j];
            Vector3 mano = c + Direccion(antebrazoAntes + codoGrados) * antebrazo[j];
            return SobreCadena(new[] { hombro, c, mano }, r);
        }

        // El brazo se mueve al revés que la pierna de su lado. Con estilo: balanceo grande, mano arrastrada y codo "quebrado".
        List<Vector3> BrazoManual(int j, float fase, Rol r, bool estilo)
        {
            float f = fase + (j == 1 ? 0.5f : 0f);
            float aPierna = AnguloTabla(1, f, estilo);
            float s = -Mathf.Lerp(estilo ? 1.35f : 0.8f, 1.1f, correr) * aPierna;
            if (sigilo && correr < 0.5f)
            {
                // Sigiloso: brazos doblados al frente, manos arriba, con un balanceo pequeño.
                float hombro = Mathf.Lerp(35f - 0.4f * aPierna, s, correr * 2f);
                return Brazo(j, r, hombro, Mathf.Lerp(100f, 85f, correr * 2f), hombro);
            }
            if (estilo && correr < 0.5f)
            {
                float retraso = -1.35f * AnguloTabla(1, f - 0.08f, true);
                float codo = s > 0f ? 25f + s * 0.5f : -12f; // codo quebrado al ir hacia atrás
                return Brazo(j, r, s, Mathf.Lerp(codo, 85f, correr), Mathf.Lerp(retraso, s, correr));
            }
            float codoNormal = Mathf.Lerp(15f + Mathf.Max(0f, s) * 0.7f, 85f, correr);
            return Brazo(j, r, s, codoNormal, s);
        }

        List<Vector3> BrazoAire(int j, Rol r)
        {
            float s, p, b;
            PesosAire(out s, out p, out b);
            float colgar = 1f - s - p - b;
            float otra = j == 0 ? 0f : 1f;
            float h = s * (160f - 15f * otra) + p * (60f - 20f * otra) + b * (130f - 20f * otra) + colgar * (20f + 15f * otra);
            float c = s * 10f + p * 100f + b * 20f + colgar * 25f;
            return Brazo(j, r, h, c, h);
        }

        static List<Vector3> SobreCadena(Vector3[] cadena, Rol r)
        {
            var l = new List<Vector3>(r.fraccion.Length);
            if (r.fraccion.Length == cadena.Length)
            {
                l.AddRange(cadena);
                return l;
            }
            foreach (float f in r.fraccion)
                l.Add(PuntoEnPolilinea(cadena, f));
            return l;
        }

        static Vector3 PuntoEnPolilinea(IList<Vector3> puntos, float fraccion)
        {
            float total = 0f;
            for (int k = 1; k < puntos.Count; k++)
                total += Vector3.Distance(puntos[k - 1], puntos[k]);
            if (total < 1e-8f)
                return puntos[0];
            float objetivo = Mathf.Clamp01(fraccion) * total;
            for (int k = 1; k < puntos.Count; k++)
            {
                float tramo = Vector3.Distance(puntos[k - 1], puntos[k]);
                if (objetivo <= tramo || k == puntos.Count - 1)
                    return Vector3.Lerp(puntos[k - 1], puntos[k], tramo > 1e-8f ? Mathf.Clamp01(objetivo / tramo) : 0f);
                objetivo -= tramo;
            }
            return puntos[puntos.Count - 1];
        }

        // ---------- Ciclos guardados ----------

        static void Buscar(CicloCaminado c, float fase, out int a, out int b, out float u)
        {
            fase = Mathf.Repeat(fase, 1f);
            int n = c.poses.Count;
            a = n - 1;
            b = 0;
            u = 0f;
            for (int k = 0; k < n - 1; k++)
            {
                if (fase >= c.poses[k].tiempo && fase <= c.poses[k + 1].tiempo)
                {
                    a = k;
                    b = k + 1;
                    float dt = c.poses[b].tiempo - c.poses[a].tiempo;
                    u = dt > 1e-5f ? (fase - c.poses[a].tiempo) / dt : 0f;
                    return;
                }
            }
        }

        float BobGuardado(CicloCaminado c, float fase)
        {
            if (c.poses.Count == 0)
                return 0f;
            int a, b;
            float u;
            Buscar(c, fase, out a, out b, out u);
            return Mathf.Lerp(c.poses[a].bob, c.poses[b].bob, u) * largoPierna;
        }

        static List<Vector3> Lista(PoseCiclo p, int rol)
        {
            return rol == 0 ? p.pierna1 : rol == 1 ? p.pierna2 : rol == 2 ? p.brazo1 : p.brazo2;
        }

        List<Vector3> PoseGuardada(CicloCaminado c, int rol, float fase, Rol r)
        {
            if (c.poses.Count < 2)
                return null;
            int a, b;
            float u;
            Buscar(c, fase, out a, out b, out u);
            var la = Lista(c.poses[a], rol);
            var lb = Lista(c.poses[b], rol);
            if (la == null || lb == null || la.Count < 2 || lb.Count < 2)
                return null;
            var pa = new List<Vector3>();
            var pb = new List<Vector3>();
            foreach (var v in la) pa.Add(Desnormalizar(v));
            foreach (var v in lb) pb.Add(Desnormalizar(v));
            var resultado = new List<Vector3>(r.fraccion.Length);
            for (int k = 0; k < r.fraccion.Length; k++)
            {
                Vector3 va = pa.Count == r.fraccion.Length ? pa[k] : PuntoEnPolilinea(pa, r.fraccion[k]);
                Vector3 vb = pb.Count == r.fraccion.Length ? pb[k] : PuntoEnPolilinea(pb, r.fraccion[k]);
                resultado.Add(Vector3.Lerp(va, vb, u));
            }
            return resultado;
        }

        // ---------- Posar con los dedos ----------

        static float AnguloDedo(Vector3 tramo, Vector3 abajoMano, Vector3 adelanteMano)
        {
            return Mathf.Atan2(Vector3.Dot(tramo, adelanteMano), Vector3.Dot(tramo, abajoMano)) * Mathf.Rad2Deg;
        }

        // Los dedos ponen los ÁNGULOS de las piernas (las piernas nunca se estiran ni se encogen).
        public void PoseDedos(Vector3[,] dedos, Vector3 normalPalma, Vector3 muneca)
        {
            if (roles[P1] == null || roles[P2] == null)
                return;
            Vector3 abajoMano = normalPalma.normalized;
            Vector3 nudillos = (dedos[0, 0] + dedos[1, 0]) * 0.5f;
            Vector3 adelanteMano = Vector3.ProjectOnPlane(nudillos - muneca, abajoMano);
            if (adelanteMano.sqrMagnitude < 1e-10f)
                return;
            adelanteMano.Normalize();
            bool antes = Trazo.silenciar;
            Trazo.silenciar = true;
            for (int i = 0; i < 2; i++)
            {
                float a1 = AnguloDedo(dedos[i, 1] - dedos[i, 0], abajoMano, adelanteMano);
                float a2 = AnguloDedo(dedos[i, 2] - dedos[i, 1], abajoMano, adelanteMano);
                float a3 = AnguloDedo(dedos[i, 3] - dedos[i, 2], abajoMano, adelanteMano);
                var pose = Pierna(i, roles[i], a1, a1 - a2, Mathf.Clamp(a3 - a2, -60f, 40f));
                miembros[i] = pose;
                Aplicar(roles[i], pose, Vector3.zero);
            }
            foreach (var r in partes)
            {
                if (r.miembro < 0 || r.miembro > 1 || miembros[r.miembro] == null)
                    continue;
                var rel = new List<Vector3>(r.rel);
                Pegar(rel, roles[r.miembro].rel, miembros[r.miembro], r.tramo);
                Aplicar(r, rel, Vector3.zero);
            }
            Trazo.silenciar = antes;
            Trazo.huboCambio = false;
        }

        static DatosTrazo Copiar(DatosTrazo a)
        {
            return new DatosTrazo
            {
                id = a.id,
                capa = a.capa,
                nodos = new List<Vector3>(a.nodos),
                asaEntrada = new List<Vector3>(a.asaEntrada),
                asaSalida = new List<Vector3>(a.asaSalida),
                asaManual = new List<bool>(a.asaManual),
                grosorNodo = new List<float>(a.grosorNodo),
                cerrado = a.cerrado,
                relleno = a.relleno,
                colorRelleno = a.colorRelleno,
                ancho = a.ancho,
                estilo = a.estilo,
                oculto = a.oculto
            };
        }
    }
}

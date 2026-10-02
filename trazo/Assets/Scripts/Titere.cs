using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Una pose de un ciclo de caminado guardado. Las coordenadas están "normalizadas":
// x = hacia adelante, y = hacia arriba, z = de lado, divididas por el largo de la pierna
// (así el ciclo sirve para personajes de cualquier tamaño).
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

// Títere que camina solo.
//  - "Muñeco prueba": pone frente a ti un muñeco listo (piernas, brazos, torso y cabeza).
//  - "Títere": pon la mano DERECHA frente a ti 1 segundo. Luego muévela: el muñeco va donde la llevas
//    y camina solo (ciclo de caminado con piernas, brazos y cabeza). Si mueves la mano hacia atrás, se voltea.
//    Pellizco IZQUIERDO = apagar.
//  - "Grabar": cuenta 3 segundos y guarda una clave por fotograma. Pellizco IZQUIERDO = parar.
//  - "Ciclo": elige el caminado: "Manual" (hecho por la app) o los tuyos.
//  - "Guardar ciclo": anima un paso con claves (la última pose igual a la primera) y guárdalo como ciclo propio.
//  - "Posar dedos": el índice y el medio derechos acomodan las piernas (sin prisa); pellizco IZQUIERDO = guardar
//    una clave en el fotograma actual. Toca "Posar dedos" otra vez para terminar.
//  - Para tus dibujos: pellizca una línea y toca Pierna 1/2, Brazo 1/2 o Cuerpo +/-. "Voltear" = el otro lado.
public class Titere : MonoBehaviour
{
    public static bool Activo { get; private set; }

    public Dibujo dibujo;
    public Animacion animacion;
    public ControlManos control;
    public int pierna1;
    public int pierna2;
    public int brazo1;
    public int brazo2;
    public int cabeza;
    public List<int> cuerpo = new List<int>();
    public bool voltear;
    public int ciclo; // 0 = Manual; 1.. = ciclos guardados

    enum Fase { Apagado, Preparando, Vivo, CuentaGrabar, Grabando, Posando }
    Fase fase = Fase.Apagado;
    Fase despues;
    float faseHasta;
    int ultimoAviso;

    // Tabla del caminado "de manual" para UNA pierna (la otra va medio ciclo después).
    // fase, muslo (grados, + = adelante), rodilla doblada, pie (+ = punta arriba)
    static readonly float[,] Tabla =
    {
        { 0.000f,  25f,  3f,  20f }, // contacto (talón)
        { 0.125f,  18f, 20f,   0f }, // bajada (recibe el peso)
        { 0.250f,   0f,  8f,   0f }, // paso (la pierna sostiene)
        { 0.375f, -15f,  8f, -20f }, // subida (empuja)
        { 0.500f, -22f, 12f, -35f }, // contacto de la otra (esta queda atrás)
        { 0.625f, -12f, 45f, -55f }, // se levanta
        { 0.750f,  10f, 70f, -35f }, // pasa por debajo
        { 0.875f,  30f, 35f,  -5f }, // va hacia adelante
    };

    static readonly string[][][] Dedos =
    {
        new[] { new[] { "Index1", "IndexProximal" }, new[] { "Index2", "IndexIntermediate" }, new[] { "Index3", "IndexDistal" }, new[] { "IndexTip" } },
        new[] { new[] { "Middle1", "MiddleProximal" }, new[] { "Middle2", "MiddleIntermediate" }, new[] { "Middle3", "MiddleDistal" }, new[] { "MiddleTip" } },
    };
    static readonly string[] Muneca = { "WristRoot", "Wrist" };

    class Rol
    {
        public Trazo trazo;
        public DatosTrazo reposo;
        public List<Vector3> rel;   // nodos de reposo respecto a la cadera
        public float[] fraccion;    // dónde cae cada nodo a lo largo de la línea
        public float largo;
    }

    const int P1 = 0, P2 = 1, B1 = 2, B2 = 3, CAB = 4;
    readonly Rol[] roles = new Rol[5];
    readonly List<Rol> cuerpoRoles = new List<Rol>();
    readonly List<Trazo> respaldoTrazos = new List<Trazo>();
    readonly List<DatosTrazo> respaldoDatos = new List<DatosTrazo>();

    Vector3 caderaReposo, adelante = Vector3.right, arriba = Vector3.up, lado = Vector3.forward;
    float largoPierna = 0.2f;
    readonly float[] muslo = new float[2], canilla = new float[2], pie = new float[2];
    readonly float[] brazoSup = new float[2], antebrazo = new float[2];

    // Control con la mano
    Vector3 munecaLocal, munecaCal;
    bool munecaValida, teniaMuneca;
    float recorridoPrevio;
    float velocidad;
    float faseCiclo;
    float peso;
    int mirando = 1;
    Vector3 raizOffset;

    // Dedos (para posar)
    readonly Vector3[,] dedos = new Vector3[2, 4];
    Vector3 normalPalma = Vector3.down;
    bool dedosValidos, teniaDedos;

    int fotogramaGrabado;
    int inicioGrabacion;
    float acumulado;

    BibliotecaCiclos biblioteca = new BibliotecaCiclos();

    public bool Grabando => fase == Fase.Grabando;
    public bool Posando => fase == Fase.Posando;
    public bool Encendido => fase != Fase.Apagado;
    public string NombreCiclo => ciclo <= 0 || ciclo > biblioteca.ciclos.Count ? "Manual" : biblioteca.ciclos[ciclo - 1].nombre;

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
        fase = Fase.Apagado;
    }

    void Mensaje(string texto)
    {
        if (dibujo != null)
            dibujo.Mensaje(texto);
    }

    bool RigValido => dibujo != null && dibujo.BuscarPorId(pierna1) != null && dibujo.BuscarPorId(pierna2) != null;

    // ---------- Guardar y cargar (con el dibujo) ----------

    public void GuardarEn(DatosDibujo d)
    {
        d.titerePierna1 = pierna1;
        d.titerePierna2 = pierna2;
        d.titereBrazo1 = brazo1;
        d.titereBrazo2 = brazo2;
        d.titereCabeza = cabeza;
        d.titereCuerpo.AddRange(cuerpo);
        d.titereVoltear = voltear;
        d.titereCiclo = ciclo;
    }

    public void Restaurar(DatosDibujo d)
    {
        if (Encendido)
        {
            fase = Fase.Apagado;
            Activo = false;
        }
        pierna1 = d.titerePierna1;
        pierna2 = d.titerePierna2;
        brazo1 = d.titereBrazo1;
        brazo2 = d.titereBrazo2;
        cabeza = d.titereCabeza;
        cuerpo.Clear();
        if (d.titereCuerpo != null)
            cuerpo.AddRange(d.titereCuerpo);
        voltear = d.titereVoltear;
        ciclo = Mathf.Max(0, d.titereCiclo);
    }

    // ---------- Botones ----------

    public void AlternarVivo()
    {
        if (fase == Fase.Vivo || fase == Fase.Preparando)
        {
            Apagar();
            return;
        }
        if (fase == Fase.Apagado)
            Encender(Fase.Vivo);
    }

    public void Grabar()
    {
        if (fase == Fase.Grabando)
        {
            TerminarGrabacion();
            return;
        }
        if (fase == Fase.Vivo)
        {
            EmpezarCuenta();
            return;
        }
        if (fase == Fase.Apagado)
            Encender(Fase.CuentaGrabar);
    }

    public void AlternarPosar()
    {
        if (fase == Fase.Posando)
        {
            Apagar();
            return;
        }
        if (fase == Fase.Apagado)
            Encender(Fase.Posando);
    }

    public void CambiarCiclo()
    {
        ciclo = (ciclo + 1) % (1 + biblioteca.ciclos.Count);
        Mensaje("Caminado: " + NombreCiclo);
    }

    public void Voltear()
    {
        voltear = !voltear;
        Mensaje(voltear ? "Mira hacia el otro lado" : "Mira hacia el lado normal");
    }

    void Encender(Fase siguiente)
    {
        if (!RigValido)
        {
            Mensaje("Primero toca Muñeco prueba, o elige Pierna 1 y Pierna 2");
            return;
        }
        if (animacion != null)
            animacion.Pausar();
        dibujo.Seleccionar(null);
        respaldoTrazos.Clear();
        respaldoDatos.Clear();
        despues = siguiente;
        fase = Fase.Preparando;
        faseHasta = Time.time + 1.5f;
        ultimoAviso = -1;
        Activo = true;
    }

    void EmpezarCuenta()
    {
        fase = Fase.CuentaGrabar;
        faseHasta = Time.time + 3f;
        ultimoAviso = -1;
    }

    public void AsignarPierna(int cual) { Asignar(cual == 1 ? 1 : 2, "Pierna " + cual); }
    public void AsignarBrazo(int cual) { Asignar(cual == 1 ? 3 : 4, "Brazo " + cual); }

    // 1 pierna1, 2 pierna2, 3 brazo1, 4 brazo2
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
        QuitarDeRoles(t.id);
        if (rol == 1) pierna1 = t.id;
        else if (rol == 2) pierna2 = t.id;
        else if (rol == 3) brazo1 = t.id;
        else brazo2 = t.id;
        Mensaje(nombre + " elegida");
    }

    void QuitarDeRoles(int id)
    {
        if (pierna1 == id) pierna1 = 0;
        if (pierna2 == id) pierna2 = 0;
        if (brazo1 == id) brazo1 = 0;
        if (brazo2 == id) brazo2 = 0;
        if (cabeza == id) cabeza = 0;
        cuerpo.Remove(id);
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
        if (cuerpo.Contains(t.id))
        {
            cuerpo.Remove(t.id);
            Mensaje("La línea ya no es parte del cuerpo");
            return;
        }
        QuitarDeRoles(t.id);
        cuerpo.Add(t.id);
        Mensaje("Línea agregada al cuerpo (" + cuerpo.Count + ")");
    }

    // ---------- Muñeco de prueba ----------

    public void CargarMuneco()
    {
        if (dibujo == null || Encendido)
            return;
        Transform cab = control != null ? control.Cabeza : null;
        Vector3 ojos = cab != null ? cab.position : new Vector3(0f, 1.5f, 0f);
        Vector3 frente = cab != null ? cab.forward : Vector3.forward;
        frente.y = 0f;
        if (frente.sqrMagnitude < 1e-4f)
            frente = Vector3.forward;
        frente.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, frente).normalized;
        Vector3 cadera = ojos + frente * 0.55f - Vector3.up * 0.15f;

        dibujo.GuardarParaDeshacer();
        float ancho = dibujo.AnchoNuevoLocal();

        // Medidas en metros: x = hacia la derecha (el muñeco mira a la derecha), y = arriba.
        var p1 = Linea(cadera, derecha, ancho, false, new Vector2(0f, 0f), new Vector2(0.02f, -0.085f), new Vector2(0f, -0.17f), new Vector2(0.035f, -0.17f));
        var p2 = Linea(cadera, derecha, ancho, false, new Vector2(0.008f, 0f), new Vector2(0.028f, -0.085f), new Vector2(0.008f, -0.17f), new Vector2(0.043f, -0.17f));
        var torso = Linea(cadera, derecha, ancho, false, new Vector2(0.004f, 0f), new Vector2(0.012f, 0.07f), new Vector2(0.006f, 0.14f));
        var b1 = Linea(cadera, derecha, ancho, false, new Vector2(0.006f, 0.125f), new Vector2(0.006f, 0.065f), new Vector2(0.01f, 0.01f));
        var b2 = Linea(cadera, derecha, ancho, false, new Vector2(0.008f, 0.125f), new Vector2(0.008f, 0.065f), new Vector2(0.012f, 0.01f));
        var puntos = new Vector2[10];
        for (int i = 0; i < puntos.Length; i++)
        {
            float a = i * Mathf.PI * 2f / puntos.Length;
            puntos[i] = new Vector2(0.012f + Mathf.Cos(a) * 0.035f, 0.185f + Mathf.Sin(a) * 0.038f);
        }
        var cabezaMuneco = Linea(cadera, derecha, ancho, true, puntos);
        // Nariz: así se nota hacia dónde mira.
        var nariz = Linea(cadera, derecha, ancho, false, new Vector2(0.046f, 0.19f), new Vector2(0.06f, 0.18f));

        pierna2 = dibujo.AgregarTrazo(p2).id;  // la de atrás primero (queda detrás)
        brazo2 = dibujo.AgregarTrazo(b2).id;
        cuerpo.Clear();
        cuerpo.Add(dibujo.AgregarTrazo(torso).id);
        pierna1 = dibujo.AgregarTrazo(p1).id;
        brazo1 = dibujo.AgregarTrazo(b1).id;
        cabeza = dibujo.AgregarTrazo(cabezaMuneco).id;
        cuerpo.Add(dibujo.AgregarTrazo(nariz).id);
        voltear = false;
        Mensaje("Muñeco listo. Toca Títere y mueve la mano derecha");
    }

    DatosTrazo Linea(Vector3 cadera, Vector3 derecha, float ancho, bool cerrada, params Vector2[] puntos)
    {
        var d = new DatosTrazo { ancho = ancho, cerrado = cerrada, relleno = cerrada, colorRelleno = 0 };
        foreach (var p in puntos)
        {
            Vector3 mundo = cadera + derecha * p.x + Vector3.up * p.y;
            d.nodos.Add(dibujo.ProyectarEnPlano(dibujo.transform.InverseTransformPoint(mundo)));
        }
        return d;
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

    // Toma las claves de la animación donde están las piernas y las guarda como un ciclo propio.
    public void GuardarCiclo()
    {
        if (dibujo == null || animacion == null || Encendido)
            return;
        if (!RigValido || !PrepararRig())
        {
            Mensaje("Primero elige Pierna 1 y Pierna 2");
            return;
        }
        var claves = new List<Clave>();
        foreach (var c in animacion.claves)
            if (Pose(c, pierna1) != null && Pose(c, pierna2) != null)
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
            var a = Pose(claves[k], pierna1);
            var b = Pose(claves[k], pierna2);
            Vector3 cadera = (a.nodos[0] + b.nodos[0]) * 0.5f;
            if (k == 0)
                cadera0 = cadera;
            var p = new PoseCiclo
            {
                tiempo = f1 > f0 ? (claves[k].fotograma - f0) / (float)(f1 - f0) : 0f,
                bob = Vector3.Dot(cadera - cadera0, arriba) / largoPierna,
            };
            p.pierna1 = Normalizar(a.nodos, cadera);
            p.pierna2 = Normalizar(b.nodos, cadera);
            var c1 = Pose(claves[k], brazo1);
            var c2 = Pose(claves[k], brazo2);
            if (c1 != null) p.brazo1 = Normalizar(c1.nodos, cadera);
            if (c2 != null) p.brazo2 = Normalizar(c2.nodos, cadera);
            nuevo.poses.Add(p);
        }
        biblioteca.ciclos.Add(nuevo);
        GuardarBiblioteca();
        ciclo = biblioteca.ciclos.Count;
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

    List<Vector3> Normalizar(List<Vector3> nodos, Vector3 cadera)
    {
        var lista = new List<Vector3>();
        foreach (var n in nodos)
        {
            Vector3 r = n - cadera;
            lista.Add(new Vector3(Vector3.Dot(r, adelante), Vector3.Dot(r, arriba), Vector3.Dot(r, lado)) / largoPierna);
        }
        return lista;
    }

    Vector3 Desnormalizar(Vector3 v)
    {
        return (adelante * v.x + arriba * v.y + lado * v.z) * largoPierna;
    }

    // ---------- Cada cuadro ----------

    void Update()
    {
        if (fase == Fase.Apagado)
            return;
        if (dibujo == null || control == null)
        {
            Apagar();
            return;
        }
        bool parar = control.Izq.valida && control.Izq.empezoPellizco;

        switch (fase)
        {
            case Fase.Preparando:
                Avisar(despues == Fase.Posando ? "Pon el índice y el medio derechos parados... " : "Pon la mano derecha frente a ti... ");
                LeerMuneca(true);
                LeerDedos(6f);
                if (parar)
                {
                    Apagar();
                    return;
                }
                if (Time.time < faseHasta)
                    return;
                if (!PrepararRig() || (despues == Fase.Posando ? !dedosValidos : !munecaValida))
                {
                    faseHasta = Time.time + 1f;
                    Mensaje("No veo bien tu mano derecha");
                    return;
                }
                munecaCal = munecaLocal;
                recorridoPrevio = 0f;
                velocidad = 0f;
                peso = 0f;
                faseCiclo = 0f;
                mirando = 1;
                raizOffset = Vector3.zero;
                if (despues == Fase.CuentaGrabar)
                {
                    EmpezarCuenta();
                }
                else if (despues == Fase.Posando)
                {
                    fase = Fase.Posando;
                    Mensaje("Acomoda las piernas con los dedos · pellizco izquierdo = guardar clave");
                }
                else
                {
                    fase = Fase.Vivo;
                    Mensaje("¡Mueve la mano y camina! Pellizco izquierdo = apagar");
                }
                break;

            case Fase.Vivo:
                if (parar)
                {
                    Apagar();
                    return;
                }
                ActualizarControl();
                PonerPose();
                break;

            case Fase.CuentaGrabar:
                if (parar)
                {
                    Apagar();
                    return;
                }
                ActualizarControl();
                PonerPose();
                Avisar("Grabando en ");
                if (Time.time < faseHasta)
                    return;
                dibujo.GuardarParaDeshacer();
                fase = Fase.Grabando;
                inicioGrabacion = animacion != null ? animacion.Fotograma : 0;
                fotogramaGrabado = inicioGrabacion;
                acumulado = 0f;
                GrabarCuadro(fotogramaGrabado);
                Mensaje("Grabando... pellizco izquierdo = parar");
                break;

            case Fase.Grabando:
                if (parar || animacion == null)
                {
                    TerminarGrabacion();
                    return;
                }
                ActualizarControl();
                acumulado += Time.deltaTime * animacion.fotogramasPorSegundo;
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
                    PonerPose();
                break;

            case Fase.Posando:
                LeerDedos(6f);
                PonerPoseDedos();
                if (parar && animacion != null)
                {
                    int f = animacion.Fotograma;
                    dibujo.GuardarParaDeshacer();
                    PonerPoseDedos();
                    animacion.GuardarClaveEn(f);
                    Trazo.huboCambio = false;
                    Mensaje("Clave guardada en el fotograma " + (f + 1));
                }
                break;
        }
    }

    // Cuenta regresiva en los avisos.
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
        PonerPose();
        animacion.GuardarClaveEn(f);
        Trazo.huboCambio = false;
    }

    void TerminarGrabacion()
    {
        int cuadros = fotogramaGrabado - inicioGrabacion + 1;
        fase = Fase.Apagado;
        Activo = false;
        Trazo.huboCambio = false;
        if (animacion != null)
        {
            animacion.IrA(inicioGrabacion);
            animacion.MostrarFotograma();
        }
        Mensaje("Grabados " + cuadros + " fotogramas. Toca Play para verlo");
    }

    void Apagar()
    {
        if (fase == Fase.Grabando)
        {
            TerminarGrabacion();
            return;
        }
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        if (animacion != null && animacion.Activa)
        {
            animacion.MostrarFotograma();
        }
        else
        {
            for (int i = 0; i < respaldoTrazos.Count; i++)
                if (respaldoTrazos[i] != null)
                    respaldoTrazos[i].AplicarPose(respaldoDatos[i], null, 0f);
        }
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
        fase = Fase.Apagado;
        Activo = false;
        Mensaje("Títere apagado");
    }

    // ---------- El muñeco ----------

    Rol CrearRol(int id)
    {
        var t = dibujo.BuscarPorId(id);
        if (t == null || t.nodos.Count < 2)
            return null;
        t.visibleAnim = true;
        var r = new Rol { trazo = t, reposo = t.CrearDatos() };
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
        respaldoTrazos.Add(t);
        respaldoDatos.Add(t.CrearDatos());
        return r;
    }

    // Lee la forma actual del muñeco ("de pie") y calcula hacia dónde mira.
    bool PrepararRig()
    {
        respaldoTrazos.Clear();
        respaldoDatos.Clear();
        roles[P1] = CrearRol(pierna1);
        roles[P2] = CrearRol(pierna2);
        roles[B1] = CrearRol(brazo1);
        roles[B2] = CrearRol(brazo2);
        roles[CAB] = CrearRol(cabeza);
        if (roles[P1] == null || roles[P2] == null)
            return false;

        var a = roles[P1].reposo.nodos;
        var b = roles[P2].reposo.nodos;
        caderaReposo = (a[0] + b[0]) * 0.5f;
        Vector3 abajo = (a[a.Count - 1] - a[0]) + (b[b.Count - 1] - b[0]);
        if (abajo.sqrMagnitude < 1e-10f)
            return false;
        arriba = -abajo.normalized;
        // Hacia adelante: hacia donde apuntan los pies (o doblan las rodillas).
        Vector3 frente = (a[a.Count - 1] - a[Mathf.Max(0, a.Count - 2)]) + (b[b.Count - 1] - b[Mathf.Max(0, b.Count - 2)]);
        frente += (a[a.Count / 2] - (a[0] + a[a.Count - 1]) * 0.5f) + (b[b.Count / 2] - (b[0] + b[b.Count - 1]) * 0.5f);
        frente = Vector3.ProjectOnPlane(frente, arriba);
        if (frente.sqrMagnitude < 1e-10f)
            frente = Vector3.ProjectOnPlane(dibujo.transform.InverseTransformDirection(Vector3.right), arriba);
        if (frente.sqrMagnitude < 1e-10f)
            frente = Vector3.Cross(arriba, Vector3.forward);
        adelante = frente.normalized * (voltear ? -1f : 1f);
        lado = Vector3.Cross(arriba, adelante).normalized;
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
        foreach (int id in cuerpo)
        {
            if (id == pierna1 || id == pierna2 || id == brazo1 || id == brazo2 || id == cabeza)
                continue;
            var r = CrearRol(id);
            if (r == null)
                continue;
            r.rel = Relativos(r.reposo.nodos);
            cuerpoRoles.Add(r);
        }
        dibujo.ActualizarVisibilidad();
        return true;
    }

    List<Vector3> Relativos(List<Vector3> nodos)
    {
        var l = new List<Vector3>(nodos.Count);
        foreach (var n in nodos)
            l.Add(n - caderaReposo);
        return l;
    }

    // La mano derecha lleva al muñeco: adelante/atrás = caminar; arriba/abajo = subir o agacharse.
    void ActualizarControl()
    {
        LeerMuneca(false);
        float dt = Mathf.Max(1e-4f, Time.deltaTime);
        if (!munecaValida)
        {
            velocidad = Mathf.Lerp(velocidad, 0f, 1f - Mathf.Exp(-6f * dt));
            peso = Mathf.Lerp(peso, 0f, 1f - Mathf.Exp(-4f * dt));
            return;
        }
        Vector3 d = munecaLocal - munecaCal;
        float recorrido = Vector3.Dot(d, adelante);
        raizOffset = adelante * recorrido + arriba * Vector3.Dot(d, arriba);
        float paso = recorrido - recorridoPrevio;
        recorridoPrevio = recorrido;
        velocidad = Mathf.Lerp(velocidad, paso / dt, 1f - Mathf.Exp(-6f * dt));
        // Un ciclo = dos pasos (más o menos 1.5 veces el largo de la pierna).
        faseCiclo = Mathf.Repeat(faseCiclo + Mathf.Abs(paso) / (largoPierna * 1.5f), 1f);
        if (velocidad > largoPierna * 0.2f)
            mirando = 1;
        else if (velocidad < -largoPierna * 0.2f)
            mirando = -1;
        float objetivo = Mathf.Clamp01(Mathf.Abs(velocidad) / (largoPierna * 0.8f));
        peso = Mathf.Lerp(peso, objetivo, 1f - Mathf.Exp(-4f * dt));
    }

    void LeerMuneca(bool primera)
    {
        munecaValida = false;
        var mano = control.Der;
        if (mano == null || !mano.valida || mano.esqueleto == null)
            return;
        var m = ManosUtil.Hueso(mano.esqueleto, Muneca);
        if (m == null)
            return;
        Vector3 nueva = dibujo.transform.InverseTransformPoint(m.position);
        float a = teniaMuneca && !primera ? 1f - Mathf.Exp(-12f * Time.deltaTime) : 1f;
        munecaLocal = Vector3.Lerp(munecaLocal, nueva, a);
        teniaMuneca = true;
        munecaValida = true;
    }

    // Pone el muñeco en la pose del ciclo (mezclada con "de pie" cuando no te mueves).
    void PonerPose()
    {
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        var cicloGuardado = ciclo > 0 && ciclo <= biblioteca.ciclos.Count ? biblioteca.ciclos[ciclo - 1] : null;
        float bob = (cicloGuardado != null ? BobGuardado(cicloGuardado, faseCiclo) : BobManual(faseCiclo)) * peso;
        Vector3 desplazar = raizOffset + arriba * bob;

        for (int i = 0; i < 4; i++)
        {
            var r = roles[i];
            if (r == null)
                continue;
            List<Vector3> pose = null;
            if (cicloGuardado != null)
                pose = PoseGuardada(cicloGuardado, i, faseCiclo, r);
            if (pose == null)
                pose = i < 2 ? PiernaManual(i, faseCiclo + (i == 1 ? 0.5f : 0f), r) : BrazoManual(i - 2, faseCiclo, r);
            for (int k = 0; k < pose.Count; k++)
                pose[k] = Vector3.Lerp(r.rel[k], pose[k], peso);
            Aplicar(r, pose, desplazar);
        }
        // Cabeza: rebota un poquito después que el cuerpo (se ve más vivo).
        if (roles[CAB] != null)
        {
            float bobCabeza = (cicloGuardado != null ? BobGuardado(cicloGuardado, faseCiclo - 0.06f) : BobManual(faseCiclo - 0.06f)) * peso;
            Aplicar(roles[CAB], new List<Vector3>(roles[CAB].rel), raizOffset + arriba * bobCabeza);
        }
        foreach (var r in cuerpoRoles)
            Aplicar(r, new List<Vector3>(r.rel), desplazar);
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
    }

    void Aplicar(Rol r, List<Vector3> rel, Vector3 desplazar)
    {
        if (r == null || r.trazo == null)
            return;
        var d = Copiar(r.reposo);
        for (int k = 0; k < d.nodos.Count && k < rel.Count; k++)
        {
            Vector3 v = rel[k];
            if (mirando < 0)
                v -= 2f * Vector3.Dot(v, adelante) * adelante; // espejo: mira hacia el otro lado
            d.nodos[k] = dibujo.ProyectarEnPlano(caderaReposo + desplazar + v);
        }
        r.trazo.AplicarPose(d, null, 0f);
    }

    // ---------- Caminado "de manual" ----------

    static float Interpolar(int columna, float fase)
    {
        fase = Mathf.Repeat(fase, 1f);
        int n = Tabla.GetLength(0);
        for (int k = 0; k < n; k++)
        {
            float t0 = Tabla[k, 0];
            float t1 = k + 1 < n ? Tabla[k + 1, 0] : 1f;
            if (fase >= t0 && fase <= t1)
            {
                float u = t1 > t0 ? (fase - t0) / (t1 - t0) : 0f;
                u = u * u * (3f - 2f * u); // suave
                float v1 = k + 1 < n ? Tabla[k + 1, columna] : Tabla[0, columna];
                return Mathf.Lerp(Tabla[k, columna], v1, u);
            }
        }
        return Tabla[0, columna];
    }

    // Sube y baja de la cadera (dos veces por ciclo): más abajo al recibir el peso, más arriba al empujar.
    float BobManual(float fase)
    {
        float q = Mathf.Repeat(fase * 2f, 1f);
        float[] valores = { -0.01f, -0.05f, 0f, 0.03f, -0.01f };
        float x = q * 4f;
        int i = Mathf.Min(3, Mathf.FloorToInt(x));
        float u = x - i;
        u = u * u * (3f - 2f * u);
        return Mathf.Lerp(valores[i], valores[i + 1], u) * largoPierna;
    }

    Vector3 Direccion(float grados)
    {
        float r = grados * Mathf.Deg2Rad;
        return -arriba * Mathf.Cos(r) + adelante * Mathf.Sin(r);
    }

    List<Vector3> PiernaManual(int i, float fase, Rol r)
    {
        float aMuslo = Interpolar(1, fase);
        float rodilla = Interpolar(2, fase);
        float aPie = Interpolar(3, fase) * Mathf.Deg2Rad;
        Vector3 cadera = r.rel[0];
        Vector3 rod = cadera + Direccion(aMuslo) * muslo[i];
        Vector3 tobillo = rod + Direccion(aMuslo - rodilla) * canilla[i];
        Vector3 punta = tobillo + (adelante * Mathf.Cos(aPie) + arriba * Mathf.Sin(aPie)) * pie[i];
        return SobreCadena(new[] { cadera, rod, tobillo, punta }, r);
    }

    // El brazo se mueve al revés que la pierna de su mismo lado.
    List<Vector3> BrazoManual(int j, float fase, Rol r)
    {
        float aPierna = Interpolar(1, fase + (j == 1 ? 0.5f : 0f));
        float s = -0.8f * aPierna;
        float codo = 15f + Mathf.Max(0f, s) * 0.7f;
        Vector3 hombro = r.rel[0];
        Vector3 c = hombro + Direccion(s) * brazoSup[j];
        Vector3 mano = c + Direccion(s + codo) * antebrazo[j];
        return SobreCadena(new[] { hombro, c, mano }, r);
    }

    // Reparte los nodos de la línea a lo largo de la cadena (igual que estaban en la línea de reposo).
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

    // null si el ciclo no tiene esa parte (entonces se usa el caminado de manual).
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
        munecaLocal = Vector3.Lerp(munecaLocal, raiz.InverseTransformPoint(m.position), a);
        teniaDedos = true;
        dedosValidos = true;
    }

    // Ángulo de un tramo del dedo: 0 = apuntando "abajo" (fuera de la palma), + = hacia adelante.
    float AnguloDedo(Vector3 tramo, Vector3 abajoMano, Vector3 adelanteMano)
    {
        return Mathf.Atan2(Vector3.Dot(tramo, adelanteMano), Vector3.Dot(tramo, abajoMano)) * Mathf.Rad2Deg;
    }

    // Los dedos ponen los ÁNGULOS de las piernas (las piernas nunca se estiran ni se encogen).
    void PonerPoseDedos()
    {
        if (!dedosValidos || roles[P1] == null || roles[P2] == null)
            return;
        Vector3 abajoMano = normalPalma.normalized;
        Vector3 nudillos = (dedos[0, 0] + dedos[1, 0]) * 0.5f;
        Vector3 adelanteMano = Vector3.ProjectOnPlane(nudillos - munecaLocal, abajoMano);
        if (adelanteMano.sqrMagnitude < 1e-10f)
            return;
        adelanteMano.Normalize();
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        for (int i = 0; i < 2; i++)
        {
            var r = roles[i];
            float a1 = AnguloDedo(dedos[i, 1] - dedos[i, 0], abajoMano, adelanteMano);
            float a2 = AnguloDedo(dedos[i, 2] - dedos[i, 1], abajoMano, adelanteMano);
            float a3 = AnguloDedo(dedos[i, 3] - dedos[i, 2], abajoMano, adelanteMano);
            float pieAng = Mathf.Clamp(a3 - a2, -60f, 40f) * Mathf.Deg2Rad;
            Vector3 cadera = r.rel[0];
            Vector3 rod = cadera + Direccion(a1) * muslo[i];
            Vector3 tobillo = rod + Direccion(a2) * canilla[i];
            Vector3 punta = tobillo + (adelante * Mathf.Cos(pieAng) + arriba * Mathf.Sin(pieAng)) * pie[i];
            Aplicar(r, SobreCadena(new[] { cadera, rod, tobillo, punta }, r), Vector3.zero);
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
            estilo = a.estilo
        };
    }
}

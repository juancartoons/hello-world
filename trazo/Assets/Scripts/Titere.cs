using System.Collections.Generic;
using UnityEngine;

// Títere con los dedos: el ÍNDICE y el MEDIO de la mano DERECHA son las dos piernas del personaje
// (como "caminar con los dedos" sobre una mesa). El nudillo es la cadera, la articulación del medio
// es la rodilla y la punta es el pie. El cuerpo (torso, cabeza, brazos) sigue a la cadera.
//
// Página "Títere" del panel de arriba:
//  - "Muñeco prueba": pone frente a ti un muñeco ya listo (2 piernas + cuerpo) para probar de una vez.
//  - "Títere": encenderlo. Pon el índice y el medio derechos "parados" (como piernas) 2 segundos.
//    Luego camina con los dedos y mira cómo se mueve. Pellizco IZQUIERDO = apagar.
//  - "Grabar": cuenta 3 segundos y graba una clave por fotograma desde el fotograma actual.
//    Pellizco IZQUIERDO = parar. Puedes grabar varias pasadas (cada una reemplaza esas claves).
//  - Para tus propios dibujos: pellizca una línea y toca "Pierna 1", "Pierna 2" o "Cuerpo +/-".
//    "Voltear" cambia hacia dónde dobla la rodilla.
public class Titere : MonoBehaviour
{
    public static bool Activo { get; private set; }

    public Dibujo dibujo;
    public Animacion animacion;
    public ControlManos control;
    public int pierna1;
    public int pierna2;
    public List<int> cuerpo = new List<int>();
    public bool voltear;
    public float suavizado = 18f;

    enum Fase { Apagado, Preparando, Vivo, CuentaGrabar, Grabando }
    Fase fase = Fase.Apagado;
    float faseHasta;
    bool grabarDespues;
    int ultimoAviso;

    static readonly string[][][] Huesos =
    {
        new[] { new[] { "Index1", "IndexProximal" }, new[] { "Index2", "IndexIntermediate" }, new[] { "Index3", "IndexDistal" }, new[] { "IndexTip" } },
        new[] { new[] { "Middle1", "MiddleProximal" }, new[] { "Middle2", "MiddleIntermediate" }, new[] { "Middle3", "MiddleDistal" }, new[] { "MiddleTip" } },
    };
    static readonly string[] Muneca = { "WristRoot", "Wrist" };

    class Pierna
    {
        public Trazo trazo;
        public DatosTrazo reposo;
        public float[] fraccion;   // dónde cae cada nodo a lo largo de la pierna (0 cadera, 1 pie)
        public Vector3[] dedoCal;  // el punto del dedo que le toca a cada nodo, al calibrar
    }

    readonly Pierna[] piernas = new Pierna[2];
    readonly List<Trazo> cuerpoTrazos = new List<Trazo>();
    readonly List<List<Vector3>> cuerpoBase = new List<List<Vector3>>();
    readonly List<Trazo> respaldoTrazos = new List<Trazo>();
    readonly List<DatosTrazo> respaldoDatos = new List<DatosTrazo>();
    Quaternion giro = Quaternion.identity;
    float escala = 1f;

    // Dedos (en coordenadas locales del dibujo), suavizados.
    readonly Vector3[,] dedos = new Vector3[2, 4];
    Vector3 munecaLocal;
    bool dedosValidos;
    bool teniaDedos;

    int fotogramaGrabado;
    int inicioGrabacion;
    float acumulado;

    public bool Grabando => fase == Fase.Grabando;
    public bool Encendido => fase != Fase.Apagado;

    void Awake()
    {
        if (dibujo == null) dibujo = GetComponent<Dibujo>();
        if (animacion == null && dibujo != null) animacion = dibujo.animacion;
    }

    void Start()
    {
        if (control == null) control = FindFirstObjectByType<ControlManos>();
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

    // ---------- Botones ----------

    public void AlternarVivo()
    {
        if (fase != Fase.Apagado)
        {
            Apagar();
            return;
        }
        Encender(false);
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
            Encender(true);
    }

    void Encender(bool luegoGrabar)
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
        grabarDespues = luegoGrabar;
        fase = Fase.Preparando;
        faseHasta = Time.time + 2f;
        ultimoAviso = -1;
        Activo = true;
    }

    void EmpezarCuenta()
    {
        fase = Fase.CuentaGrabar;
        faseHasta = Time.time + 3f;
        ultimoAviso = -1;
    }

    public void AsignarPierna(int cual)
    {
        if (dibujo == null || Encendido)
            return;
        var t = dibujo.Seleccion;
        if (t == null)
        {
            Mensaje("Pellizca una línea para elegirla");
            return;
        }
        if (cual == 1) pierna1 = t.id; else pierna2 = t.id;
        cuerpo.Remove(t.id);
        if (cual == 1 && pierna2 == t.id) pierna2 = 0;
        if (cual == 2 && pierna1 == t.id) pierna1 = 0;
        Mensaje("Pierna " + cual + " elegida");
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
        if (t.id == pierna1 || t.id == pierna2)
        {
            Mensaje("Esa línea es una pierna");
            return;
        }
        if (cuerpo.Remove(t.id))
        {
            Mensaje("La línea ya no es parte del cuerpo");
            return;
        }
        cuerpo.Add(t.id);
        Mensaje("Línea agregada al cuerpo (" + cuerpo.Count + ")");
    }

    public void Voltear()
    {
        voltear = !voltear;
        Mensaje(voltear ? "Rodillas hacia el otro lado" : "Rodillas normales");
    }

    public void Restaurar(int p1, int p2, List<int> c, bool v)
    {
        if (Encendido)
        {
            fase = Fase.Apagado;
            Activo = false;
        }
        pierna1 = p1;
        pierna2 = p2;
        cuerpo.Clear();
        if (c != null)
            cuerpo.AddRange(c);
        voltear = v;
    }

    // ---------- Muñeco de prueba ----------

    public void CargarMuneco()
    {
        if (dibujo == null || Encendido)
            return;
        Transform cabeza = control != null ? control.Cabeza : null;
        Vector3 ojos = cabeza != null ? cabeza.position : new Vector3(0f, 1.5f, 0f);
        Vector3 adelante = cabeza != null ? cabeza.forward : Vector3.forward;
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized;
        Vector3 cadera = ojos + adelante * 0.55f - Vector3.up * 0.15f;

        dibujo.GuardarParaDeshacer();
        float ancho = dibujo.AnchoNuevoLocal();

        // Medidas en metros: x = hacia la derecha (el muñeco mira a la derecha), y = arriba.
        var p1 = Linea(cadera, derecha, ancho, false, new Vector2(0f, 0f), new Vector2(0.03f, -0.09f), new Vector2(0f, -0.18f), new Vector2(0.04f, -0.185f));
        var p2 = Linea(cadera, derecha, ancho, false, new Vector2(0.01f, 0f), new Vector2(0.045f, -0.088f), new Vector2(0.02f, -0.178f), new Vector2(0.06f, -0.183f));
        var torso = Linea(cadera, derecha, ancho, false, new Vector2(0.005f, 0f), new Vector2(0.012f, 0.07f), new Vector2(0.005f, 0.14f));
        var brazo1 = Linea(cadera, derecha, ancho, false, new Vector2(0.006f, 0.125f), new Vector2(0.035f, 0.07f), new Vector2(0.06f, 0.025f));
        var brazo2 = Linea(cadera, derecha, ancho, false, new Vector2(0.006f, 0.125f), new Vector2(-0.025f, 0.07f), new Vector2(-0.035f, 0.02f));
        var puntos = new Vector2[10];
        for (int i = 0; i < puntos.Length; i++)
        {
            float a = i * Mathf.PI * 2f / puntos.Length;
            puntos[i] = new Vector2(0.008f + Mathf.Cos(a) * 0.035f, 0.185f + Mathf.Sin(a) * 0.038f);
        }
        var cabezaMuneco = Linea(cadera, derecha, ancho, true, puntos);

        var tP1 = dibujo.AgregarTrazo(p1);
        var tP2 = dibujo.AgregarTrazo(p2);
        var tTorso = dibujo.AgregarTrazo(torso);
        var tB1 = dibujo.AgregarTrazo(brazo1);
        var tB2 = dibujo.AgregarTrazo(brazo2);
        var tCabeza = dibujo.AgregarTrazo(cabezaMuneco);
        pierna1 = tP1.id;
        pierna2 = tP2.id;
        cuerpo.Clear();
        cuerpo.Add(tTorso.id);
        cuerpo.Add(tB1.id);
        cuerpo.Add(tB2.id);
        cuerpo.Add(tCabeza.id);
        voltear = false;
        Mensaje("Muñeco listo. Toca Títere y camina con el índice y el medio derechos");
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
        LeerDedos();
        bool parar = control.Izq.valida && control.Izq.empezoPellizco;

        switch (fase)
        {
            case Fase.Preparando:
                Avisar("Pon el índice y el medio derechos parados, como piernas... ");
                if (parar)
                {
                    Apagar();
                    return;
                }
                if (Time.time < faseHasta)
                    return;
                if (!Calibrar())
                {
                    faseHasta = Time.time + 1f;
                    Mensaje("No veo bien tu mano derecha");
                    return;
                }
                if (grabarDespues)
                {
                    EmpezarCuenta();
                }
                else
                {
                    fase = Fase.Vivo;
                    Mensaje("¡Camina con los dedos! Pellizco izquierdo = apagar");
                }
                break;

            case Fase.Vivo:
                if (parar)
                {
                    Apagar();
                    return;
                }
                PonerPose();
                break;

            case Fase.CuentaGrabar:
                if (parar)
                {
                    Apagar();
                    return;
                }
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
        }
    }

    // Cuenta regresiva en los avisos (3, 2, 1).
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

    // ---------- Dedos ----------

    void LeerDedos()
    {
        var mano = control.Der;
        dedosValidos = false;
        if (mano == null || !mano.valida || mano.esqueleto == null)
            return;
        var raiz = dibujo.transform;
        var nuevos = new Vector3[2, 4];
        for (int f = 0; f < 2; f++)
        {
            for (int j = 0; j < 4; j++)
            {
                var h = ManosUtil.Hueso(mano.esqueleto, Huesos[f][j]);
                if (h == null)
                    return;
                nuevos[f, j] = raiz.InverseTransformPoint(h.position);
            }
        }
        var m = ManosUtil.Hueso(mano.esqueleto, Muneca);
        if (m == null)
            return;
        Vector3 munecaNueva = raiz.InverseTransformPoint(m.position);
        float a = teniaDedos ? 1f - Mathf.Exp(-suavizado * Time.deltaTime) : 1f;
        for (int f = 0; f < 2; f++)
            for (int j = 0; j < 4; j++)
                dedos[f, j] = Vector3.Lerp(dedos[f, j], nuevos[f, j], a);
        munecaLocal = Vector3.Lerp(munecaLocal, munecaNueva, a);
        teniaDedos = true;
        dedosValidos = true;
    }

    // El punto a lo largo del dedo (0 = nudillo, 1 = punta).
    Vector3 PuntoDedo(int f, float fraccion)
    {
        float total = 0f;
        for (int j = 1; j < 4; j++)
            total += Vector3.Distance(dedos[f, j - 1], dedos[f, j]);
        if (total < 1e-6f)
            return dedos[f, 0];
        float objetivo = Mathf.Clamp01(fraccion) * total;
        for (int j = 1; j < 4; j++)
        {
            float tramo = Vector3.Distance(dedos[f, j - 1], dedos[f, j]);
            if (objetivo <= tramo || j == 3)
                return Vector3.Lerp(dedos[f, j - 1], dedos[f, j], tramo > 1e-6f ? Mathf.Clamp01(objetivo / tramo) : 0f);
            objetivo -= tramo;
        }
        return dedos[f, 3];
    }

    float LargoDedo(int f)
    {
        float total = 0f;
        for (int j = 1; j < 4; j++)
            total += Vector3.Distance(dedos[f, j - 1], dedos[f, j]);
        return total;
    }

    static float Largo(List<Vector3> puntos)
    {
        float total = 0f;
        for (int i = 1; i < puntos.Count; i++)
            total += Vector3.Distance(puntos[i - 1], puntos[i]);
        return total;
    }

    // Toma la forma actual del dibujo como "de pie" y la pose actual de los dedos como su equivalente.
    bool Calibrar()
    {
        if (!dedosValidos)
            return false;
        var t1 = dibujo.BuscarPorId(pierna1);
        var t2 = dibujo.BuscarPorId(pierna2);
        if (t1 == null || t2 == null || t1.nodos.Count < 2 || t2.nodos.Count < 2)
            return false;

        respaldoTrazos.Clear();
        respaldoDatos.Clear();
        var lista = new[] { t1, t2 };
        Vector3 abajoDibujo = Vector3.zero;
        Vector3 rodillas = Vector3.zero;
        float largoPiernas = 0f;
        for (int i = 0; i < 2; i++)
        {
            var t = lista[i];
            t.visibleAnim = true;
            var p = new Pierna { trazo = t, reposo = t.CrearDatos() };
            var nodos = p.reposo.nodos;
            int n = nodos.Count;
            float total = Mathf.Max(1e-6f, Largo(nodos));
            p.fraccion = new float[n];
            float acum = 0f;
            for (int k = 0; k < n; k++)
            {
                if (k > 0)
                    acum += Vector3.Distance(nodos[k - 1], nodos[k]);
                p.fraccion[k] = acum / total;
            }
            piernas[i] = p;
            respaldoTrazos.Add(t);
            respaldoDatos.Add(t.CrearDatos());
            Vector3 abajo = nodos[n - 1] - nodos[0];
            abajoDibujo += abajo;
            // Hacia dónde dobla la rodilla: el nodo del medio, respecto a la recta cadera-pie.
            Vector3 medio = nodos[n / 2] - (nodos[0] + nodos[n - 1]) * 0.5f;
            rodillas += Vector3.ProjectOnPlane(medio, abajo.normalized);
            largoPiernas += total;
        }
        if (abajoDibujo.sqrMagnitude < 1e-10f)
            return false;
        abajoDibujo.Normalize();
        Vector3 adelanteDibujo = Vector3.ProjectOnPlane(rodillas, abajoDibujo);
        if (adelanteDibujo.sqrMagnitude < 1e-8f)
            adelanteDibujo = Vector3.ProjectOnPlane(dibujo.transform.InverseTransformDirection(Vector3.right), abajoDibujo);
        if (adelanteDibujo.sqrMagnitude < 1e-8f)
            adelanteDibujo = Vector3.Cross(abajoDibujo, Vector3.forward);
        adelanteDibujo.Normalize();
        if (voltear)
            adelanteDibujo = -adelanteDibujo;

        // Los dedos: hacia abajo = del nudillo a la punta; hacia adelante = de la muñeca a los nudillos.
        Vector3 abajoDedos = (dedos[0, 3] - dedos[0, 0]) + (dedos[1, 3] - dedos[1, 0]);
        if (abajoDedos.sqrMagnitude < 1e-10f)
            return false;
        abajoDedos.Normalize();
        Vector3 nudillos = (dedos[0, 0] + dedos[1, 0]) * 0.5f;
        Vector3 adelanteDedos = Vector3.ProjectOnPlane(nudillos - munecaLocal, abajoDedos);
        if (adelanteDedos.sqrMagnitude < 1e-8f)
            adelanteDedos = Vector3.Cross(abajoDedos, Vector3.right);
        adelanteDedos.Normalize();

        giro = Quaternion.LookRotation(adelanteDibujo, -abajoDibujo) * Quaternion.Inverse(Quaternion.LookRotation(adelanteDedos, -abajoDedos));
        float largoDedos = LargoDedo(0) + LargoDedo(1);
        escala = largoDedos > 1e-5f ? largoPiernas / largoDedos : 1f;

        for (int i = 0; i < 2; i++)
        {
            var p = piernas[i];
            p.dedoCal = new Vector3[p.fraccion.Length];
            for (int k = 0; k < p.fraccion.Length; k++)
                p.dedoCal[k] = PuntoDedo(i, p.fraccion[k]);
        }

        cuerpoTrazos.Clear();
        cuerpoBase.Clear();
        foreach (int id in cuerpo)
        {
            var t = dibujo.BuscarPorId(id);
            if (t == null || t == t1 || t == t2)
                continue;
            t.visibleAnim = true;
            cuerpoTrazos.Add(t);
            cuerpoBase.Add(new List<Vector3>(t.nodos));
            respaldoTrazos.Add(t);
            respaldoDatos.Add(t.CrearDatos());
        }
        dibujo.ActualizarVisibilidad();
        return true;
    }

    // Mueve las piernas según los dedos y el cuerpo según la cadera.
    void PonerPose()
    {
        if (piernas[0] == null || piernas[1] == null || piernas[0].trazo == null || piernas[1].trazo == null)
            return;
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        Vector3 cadera = Vector3.zero;
        for (int i = 0; i < 2; i++)
        {
            var p = piernas[i];
            var d = Copiar(p.reposo);
            for (int k = 0; k < d.nodos.Count && k < p.dedoCal.Length; k++)
            {
                Vector3 delta = giro * ((PuntoDedo(i, p.fraccion[k]) - p.dedoCal[k]) * escala);
                d.nodos[k] = dibujo.ProyectarEnPlano(p.reposo.nodos[k] + delta);
            }
            cadera += d.nodos[0] - p.reposo.nodos[0];
            p.trazo.AplicarPose(d, null, 0f);
        }
        cadera = dibujo.ProyectarVectorEnPlano(cadera * 0.5f);
        for (int i = 0; i < cuerpoTrazos.Count; i++)
            if (cuerpoTrazos[i] != null)
                cuerpoTrazos[i].Desplazar(cuerpoBase[i], cadera);
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

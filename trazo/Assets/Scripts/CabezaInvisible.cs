using System.Collections.Generic;
using TMPro;
using UnityEngine;

// LA CABEZA INVISIBLE (v51, v52): dibujas la cabeza de tu personaje UNA vez (DE FRENTE o DE PERFIL) en una capa: el
// contorno (la línea más grande) y los rasgos (ojos, nariz, boca, orejas; sin cabello por ahora). La app arma con eso
// un HUEVO 3D invisible, pega cada rasgo en su superficie y te deja GIRARLA: de frente, 3/4 y perfil para los dos
// lados (y un poco arriba/abajo). Se redibuja en 2D con tus mismas líneas (grosor y color).
//  - Los botones Perfil / 3/4 / Frente la ponen en esa vista; o PELLIZCA con la derecha cerca de la cabeza y arrastra
//    (de lado = girar, arriba/abajo = inclinar).
//  - v52: "Usar esta vista" pone esa vista en el FOTOGRAMA actual con LOS MISMOS NODOS de tus líneas (se mueven, no se
//    reemplazan): así queda una clave y entre dos vistas hay MORPH. Lo que queda atrás se pega a lo que sí se ve.
//  - v52: BIBLIOTECA de vistas (como Ctrl+L en Flash): "Guardar vista" la agrega; tocar una vista guardada la pone en
//    el fotograma donde estés. El modelo se queda mientras existan tus líneas ("Nueva cabeza" lo vuelve a armar).
//  - v52: PUNTOS CLAVE (opcional): nombra nodos con la voz ("punta de nariz", "barbilla"...) o con el teclado
//    ("Nombrar nodo"): así la app sabe qué es cada línea aunque tu estilo sea muy libre. "Etiquetas" muestra lo que la
//    app reconoce (con "?" si lo adivinó) encima de cada parte, semitransparente.
//  - Trampas del 2D: dibujada de PERFIL, el ojo (y la oreja) se copian al otro lado para la vista de frente; la nariz
//    y la boca van en la línea del centro (la nariz sobresale; la boca se abre a lo ancho). Dibujada DE FRENTE, la
//    nariz sobresale un poquito y las orejas (fuera del contorno) son discos a los lados.
public class CabezaInvisible : MonoBehaviour
{
    // ---------- Puntos clave ----------
    public static readonly string[] PuntosEs =
    {
        "punta de nariz", "puente de la nariz", "ojo izquierdo", "ojo derecho", "párpado", "ceja", "entrecejo",
        "comisura izquierda", "comisura derecha", "labio superior", "labio inferior", "barbilla", "mandíbula", "pómulo",
        "frente", "coronilla", "nuca", "sien", "oreja", "lóbulo"
    };
    public static readonly string[] PuntosEn =
    {
        "nose tip", "nose bridge", "left eye", "right eye", "eyelid", "eyebrow", "between the eyebrows",
        "left mouth corner", "right mouth corner", "upper lip", "lower lip", "chin", "jaw", "cheekbone",
        "forehead", "crown", "nape", "temple", "ear", "earlobe"
    };
    // 0 nariz, 1 ojo, 2 boca, 3 contorno, 4 oreja
    static readonly int[] CategoriaPunto = { 0, 0, 1, 1, 1, 1, 0, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 4, 4 };
    static readonly string[] NombresCategoria = { "nariz", "ojo", "boca", "contorno", "oreja" };

    public static string[] FrasesEs(int i)
    {
        string p = PuntosEs[i];
        if (p == "punta de nariz")
            return new[] { p, "punta de la nariz" };
        if (p.StartsWith("ojo "))
            return new[] { p, "centro " + p };
        return new[] { p };
    }

    // La categoría de un nombre (también para los escritos con el teclado: busca palabras conocidas).
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
            || n.Contains("forehead") || n.Contains("nuca") || n.Contains("coronilla") || n.Contains("pómulo") || n.Contains("sien"))
            return 3;
        return -1;
    }

    static CabezaInvisible instancia;
    public static bool Activa => instancia != null && instancia.estado != Estado.Nada;

    enum Estado { Nada, Preguntando, Girando }
    enum Tipo { Rasgo, Centro, Boca, Oreja, OjoPerfil, NarizFrente }

    // Una línea del modelo (en el espacio de tu dibujo original, respecto al centro de la cabeza).
    class Linea
    {
        public int idOrigen;               // la línea tuya
        public bool espejo;                // la copia del otro lado (dibujada de perfil)
        public int idEspejo;               // la línea creada para la copia (0 = aún no existe)
        public Tipo tipo;
        public bool nombrada;              // su tipo viene de un nombre (no adivinado)
        public Vector2 c;                  // su centro (en tu dibujo)
        public float x0, x1;
        public readonly List<Vector3> pts = new List<Vector3>();        // la curva (para la vista previa)
        public readonly List<Vector3> nodos = new List<Vector3>();      // sus nodos (para usar la vista)
        public readonly List<Vector3> asaE = new List<Vector3>(), asaS = new List<Vector3>();  // punta de cada tirador
        public readonly List<bool> manual = new List<bool>();
    }

    class VistaGuardada
    {
        public string nombre;
        public float giro, inclina;
    }

    Estado estado;
    ControlManos control;
    Dibujo dibujo;
    int capaCabeza = -1;
    readonly List<int> idsOriginales = new List<int>();
    readonly List<Renderer> apagados = new List<Renderer>();
    readonly List<Linea> lineas = new List<Linea>();
    int idContorno;
    readonly List<float> angulosContorno = new List<float>();   // el ángulo de cada nodo del contorno (en tu dibujo)
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

    // La vista previa y el panel.
    Transform raiz, panel;
    Material normal, marcado;
    readonly List<LineRenderer> trazosVista = new List<LineRenderer>();
    readonly List<TextMeshPro> etiquetas = new List<TextMeshPro>();
    readonly Dictionary<Color, Material> materiales = new Dictionary<Color, Material>();
    readonly List<GameObject> filaPregunta = new List<GameObject>(), filaGiro = new List<GameObject>(), filaVistas = new List<GameObject>();
    readonly List<Vector3> silueta = new List<Vector3>();
    bool sucio = true;

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
            Esconder();
            Mostrar(filaPregunta, false);
            Mostrar(filaGiro, true);
            RefrescarVistas();
            estado = Estado.Girando;
            sucio = true;
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

    void Empezar()
    {
        hayModelo = false;
        vistas.Clear();
        idsOriginales.Clear();
        var originales = new List<Trazo>();
        foreach (var t in dibujo.trazos)
            if (Dibujo.Editable(t) && !t.oculto && t.capa == dibujo.capaActual && t.curva.Count >= 2 && t.nodos.Count >= 2)
            {
                originales.Add(t);
                idsOriginales.Add(t.id);
            }
        if (originales.Count < 2)
        {
            dibujo.Mensaje("Cabeza invisible: dibuja en esta capa el contorno de la cabeza y sus rasgos (ojos, nariz, boca, orejas)");
            return;
        }
        capaCabeza = dibujo.capaActual;
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
        ArmarPanel();
        Mostrar(filaPregunta, true);
        Mostrar(filaGiro, false);
        Mostrar(filaVistas, false);
        estado = Estado.Preguntando;
        dibujo.Mensaje("Cabeza invisible: ¿la dibujaste de frente o de perfil?");
    }

    void Cerrar()
    {
        Desesconder();
        estado = Estado.Nada;
        agarrada = false;
        if (raiz != null)
            raiz.gameObject.SetActive(false);
        if (control != null && control.Ocupado)
            control.Ocupado = false;
    }

    void Esconder()
    {
        foreach (var t in Originales())
            EsconderTrazo(t);
        foreach (var l in lineas)
            if (l.espejo && l.idEspejo != 0)
                EsconderTrazo(dibujo.BuscarPorId(l.idEspejo));
    }

    void EsconderTrazo(Trazo t)
    {
        if (t == null)
            return;
        foreach (var r in t.GetComponentsInChildren<Renderer>())
            if (!r.forceRenderingOff)
            {
                r.forceRenderingOff = true;
                apagados.Add(r);
            }
    }

    void Desesconder()
    {
        foreach (var r in apagados)
            if (r != null)
                r.forceRenderingOff = false;
        apagados.Clear();
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

    void Elegir(bool dePerfil)
    {
        perfil = dePerfil;
        lineas.Clear();
        var originales = Originales();
        Trazo contorno = dibujo.BuscarPorId(idContorno);
        if (contorno == null)
            return;
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
        // El contorno: el ángulo de cada uno de sus nodos (en cada vista va a la silueta, al mismo ángulo).
        angulosContorno.Clear();
        foreach (var p in contorno.nodos)
        {
            Vector2 q = Plano(contorno.transform.TransformPoint(p));
            angulosContorno.Add(Mathf.Atan2(q.y, q.x));
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
            Llenar(l, t, plano);
            lineas.Add(l);
            if (perfil && (l.tipo == Tipo.OjoPerfil || l.tipo == Tipo.Oreja))
            {
                var e = new Linea { idOrigen = t.id, espejo = true, tipo = l.tipo, nombrada = l.nombrada, c = l.c, x0 = l.x0, x1 = l.x1 };
                Llenar(e, t, plano);
                lineas.Add(e);
            }
        }
        hayModelo = true;
        Esconder();
        giro = 0f;
        inclina = 0f;
        Mostrar(filaPregunta, false);
        Mostrar(filaGiro, true);
        RefrescarVistas();
        estado = Estado.Girando;
        sucio = true;
        dibujo.Mensaje("Gírala: Perfil / 3/4 / Frente, o pellízcala con la derecha y arrastra");
    }

    // Qué es cada línea: por su nombre (si nombraste un nodo) o adivinando por dónde está.
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
        if (cat >= 0)
        {
            l.nombrada = true;
            switch (cat)
            {
                case 0: l.tipo = perfil ? Tipo.Centro : Tipo.NarizFrente; return;
                case 1: l.tipo = perfil ? Tipo.OjoPerfil : Tipo.Rasgo; return;
                case 2: l.tipo = perfil ? Tipo.Boca : Tipo.Rasgo; return;
                case 4: l.tipo = Tipo.Oreja; return;
                default: l.tipo = Tipo.Rasgo; return;
            }
        }
        l.tipo = Adivinar(plano, l.c, perfil);
    }

    Tipo Adivinar(List<Vector2> plano, Vector2 c, bool dePerfil)
    {
        if (!dePerfil)
        {
            int fuera = 0;
            foreach (var q in plano)
                if (K(q) <= 0f)
                    fuera++;
            return fuera > plano.Count / 2 ? Tipo.Oreja : Tipo.Rasgo;
        }
        float uc = c.x / semiX, vc = c.y / semiY;
        int borde = 0;
        float m = dePerfil ? mira : 1f;
        foreach (var q in plano)
            if (K(q) <= 0f || q.x / semiX * m > 0.7f)
                borde++;
        if (borde > plano.Count / 2)
            return vc < -0.2f ? Tipo.Boca : Tipo.Centro;
        if (Mathf.Abs(uc) < 0.35f && Mathf.Abs(vc) < 0.4f)
            return Tipo.Oreja;
        return Tipo.OjoPerfil;
    }

    // Los puntos 3D de la línea: su curva (vista previa) y sus nodos y tiradores (para usar la vista).
    void Llenar(Linea l, Trazo t, List<Vector2> plano)
    {
        for (int i = 0; i < plano.Count; i++)
            l.pts.Add(Espejar(l, Mapear(l, plano[i], i / (float)Mathf.Max(1, plano.Count - 1))));
        int n = t.nodos.Count;
        t.AsegurarAsas();
        for (int j = 0; j < n; j++)
        {
            float f = j / (float)Mathf.Max(1, n - 1);
            Vector3 nodoLocal = t.nodos[j];
            Vector2 q = Plano(t.transform.TransformPoint(nodoLocal));
            l.nodos.Add(Espejar(l, Mapear(l, q, f)));
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
                return new Vector3(l.c.x + dx * 0.71f, q.y, -0.1f * semiZ - dx * lado * 0.71f);
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
            {
                float z = (f - 0.5f) * 0.7f * semiZ;
                return new Vector3(q.x - Mathf.Abs(z) * 0.3f * mira, q.y, z);
            }
            case Tipo.Oreja:
            {
                float dx = q.x - l.c.x;
                return new Vector3(l.c.x + dx * 0.71f, q.y, semiZ * 0.9f - dx * mira * 0.71f);
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
        if (estado == Estado.Girando)
        {
            RevisarAgarre();
            if (sucio)
                Dibujar();
        }
        else if (estado == Estado.Preguntando && sucio)
        {
            EtiquetasPregunta();
        }
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
            sucio = true;
            return;
        }
        if (der.valida && der.empezoPellizco && !BotonTocable.DedoCerca
            && Vector3.Distance(der.PuntoPellizco, centro) < Mathf.Max(semiX, semiY) + 0.12f)
        {
            agarrada = true;
            agarreInicio = der.PuntoPellizco;
            giroInicio = giro;
            inclinaInicio = inclina;
            control.Ocupado = true;
        }
    }

    Quaternion Giro => Quaternion.Euler(inclina, giro, 0f);

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

    void Dibujar()
    {
        sucio = false;
        Quaternion q = Giro;
        Silueta(q);
        int k = 0;
        Trazo contorno = dibujo.BuscarPorId(idContorno);
        PonerTrazoVista(k++, silueta, contorno);
        int et = 0;
        foreach (var l in lineas)
        {
            Trazo origen = dibujo.BuscarPorId(l.idOrigen);
            var corrida = new List<Vector3>();
            Vector3 suma = Vector3.zero;
            int visibles = 0;
            foreach (var p in l.pts)
            {
                Vector3 pr = q * p;
                if (Visible(q, p))
                {
                    corrida.Add(new Vector3(pr.x, pr.y, 0f));
                    suma += pr;
                    visibles++;
                }
                else
                {
                    if (corrida.Count >= 2)
                        PonerTrazoVista(k++, corrida, origen);
                    corrida = new List<Vector3>();
                }
            }
            if (corrida.Count >= 2)
                PonerTrazoVista(k++, corrida, origen);
            // Su etiqueta (lo que la app reconoce), encima de la línea.
            if (verEtiquetas && visibles > 0 && !l.espejo)
                PonerEtiqueta(et++, Mundo(suma / visibles) + arriba * 0.012f, NombreTipo(l), l.nombrada);
        }
        // Los nodos con nombre (sólidos).
        if (verEtiquetas)
            foreach (var l in lineas)
            {
                if (l.espejo)
                    continue;
                var t = dibujo.BuscarPorId(l.idOrigen);
                if (t == null)
                    continue;
                for (int j = 0; j < l.nodos.Count; j++)
                {
                    string nombre = t.NombreDeNodo(j);
                    if (nombre.Length > 0 && Visible(q, l.nodos[j]))
                        PonerEtiqueta(et++, Mundo(q * l.nodos[j]) + haciaTi * 0.003f, "• " + nombre, true);
                }
            }
        for (int i = k; i < trazosVista.Count; i++)
            trazosVista[i].positionCount = 0;
        for (int i = et; i < etiquetas.Count; i++)
            etiquetas[i].gameObject.SetActive(false);
    }

    string NombreTipo(Linea l)
    {
        string n;
        switch (l.tipo)
        {
            case Tipo.Centro: n = l.c.y / semiY < -0.2f ? "boca" : "nariz"; break;
            case Tipo.NarizFrente: n = "nariz"; break;
            case Tipo.Boca: n = "boca"; break;
            case Tipo.Oreja: n = "oreja"; break;
            case Tipo.OjoPerfil: n = "ojo"; break;
            default:
            {
                float v = l.c.y / semiY;
                n = v > 0.05f ? "ojo" : v < -0.3f ? "boca" : "nariz";
                break;
            }
        }
        return Idioma.T(n) + (l.nombrada ? "" : "?");
    }

    // Antes de elegir frente/perfil: lo que la app cree que es cada línea (con "?"), y los nodos con nombre.
    void EtiquetasPregunta()
    {
        sucio = false;
        int et = 0;
        if (verEtiquetas)
        {
            foreach (var t in Originales())
            {
                var plano = new List<Vector2>();
                foreach (var p in t.curva)
                    plano.Add(Plano(t.transform.TransformPoint(p)));
                if (plano.Count < 2)
                    continue;
                Vector2 c = Medio(plano);
                string texto;
                bool nombrada = CategoriaDeLinea(t) >= 0;
                if (t.id == idContorno)
                    texto = Idioma.T("contorno");
                else if (nombrada)
                    texto = Idioma.T(NombresCategoria[CategoriaDeLinea(t)]);
                else
                {
                    var l = new Linea { c = c, tipo = Adivinar(plano, c, perfilAdivinado) };
                    texto = NombreTipo(l);
                }
                if (t.id == idContorno)
                    c = new Vector2(0f, semiY);
                PonerEtiqueta(et++, Mundo(new Vector3(c.x, c.y, 0f)) + arriba * 0.012f, texto, nombrada);
                for (int j = 0; j < t.nodos.Count; j++)
                {
                    string nombre = t.NombreDeNodo(j);
                    if (nombre.Length > 0)
                        PonerEtiqueta(et++, t.transform.TransformPoint(t.nodos[j]) + haciaTi * 0.003f, "• " + nombre, true);
                }
            }
        }
        for (int i = et; i < etiquetas.Count; i++)
            etiquetas[i].gameObject.SetActive(false);
    }

    void PonerEtiqueta(int i, Vector3 pos, string texto, bool segura)
    {
        while (etiquetas.Count <= i)
        {
            var t = PanelTutoriales.Texto(raiz, "", Vector3.zero, new Vector2(0.08f, 0.012f), 0.09f, null);
            t.fontStyle = FontStyles.Bold;
            t.outlineWidth = 0.2f;
            t.outlineColor = new Color32(255, 255, 255, 200);
            etiquetas.Add(t);
        }
        var e = etiquetas[i];
        e.gameObject.SetActive(true);
        e.text = texto;
        e.color = segura ? new Color(0.1f, 0.35f, 0.75f, 1f) : new Color(0.1f, 0.35f, 0.75f, 0.45f);
        e.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-haciaTi, arriba));
    }

    void PonerTrazoVista(int i, List<Vector3> pts, Trazo origen)
    {
        var lr = TrazoVista(i);
        lr.positionCount = pts.Count;
        for (int j = 0; j < pts.Count; j++)
            lr.SetPosition(j, Mundo(pts[j]));
        float ancho = origen != null ? origen.ancho * dibujo.EscalaMundo : 0.004f;
        lr.widthMultiplier = Mathf.Max(0.0015f, ancho);
        lr.sharedMaterial = MaterialColor(origen != null ? origen.color : Color.black);
    }

    Vector3 Mundo(Vector3 p)
    {
        Vector2 q = new Vector2(p.x, p.y) + centro2D;
        return centro + derecha * q.x + arriba * q.y + haciaTi * 0.001f;
    }

    LineRenderer TrazoVista(int i)
    {
        while (trazosVista.Count <= i)
        {
            var go = new GameObject("CabezaLinea");
            go.transform.SetParent(raiz, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 3;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            trazosVista.Add(lr);
        }
        return trazosVista[i];
    }

    Material MaterialColor(Color c)
    {
        Material m;
        if (materiales.TryGetValue(c, out m) && m != null)
            return m;
        m = control.materialNodo != null ? new Material(control.materialNodo) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        materiales[c] = m;
        return m;
    }

    // ---------- Usar una vista: tus líneas (los MISMOS nodos) toman esa forma en el fotograma actual ----------

    void Usar()
    {
        if (estado != Estado.Girando || !hayModelo)
            return;
        var anim = dibujo.animacion;
        if (dibujo.capaActual != capaCabeza)
            dibujo.SeleccionarCapa(capaCabeza);
        Desesconder();
        dibujo.GuardarParaDeshacer();
        Quaternion q = Giro;
        Silueta(q);
        var raizDibujo = dibujo.transform;
        var nuevos = new List<int>();
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        // El contorno: cada nodo a la silueta, en su mismo ángulo.
        Trazo contorno = dibujo.BuscarPorId(idContorno);
        if (contorno != null && angulosContorno.Count == contorno.nodos.Count)
        {
            var d = contorno.CrearDatos();
            for (int j = 0; j < d.nodos.Count; j++)
            {
                Vector2 p = EnSilueta(angulosContorno[j]);
                d.nodos[j] = raizDibujo.InverseTransformPoint(Mundo(new Vector3(p.x, p.y, 0f)));
            }
            // Sus tiradores se recalculan solos (curva suave).
            for (int j = 0; j < d.asaManual.Count; j++)
                d.asaManual[j] = false;
            contorno.AplicarPose(d, null, 0f);
        }
        foreach (var l in lineas)
        {
            Trazo origen = dibujo.BuscarPorId(l.idOrigen);
            if (origen == null)
                continue;
            Trazo t = l.espejo ? (l.idEspejo != 0 ? dibujo.BuscarPorId(l.idEspejo) : null) : origen;
            DatosTrazo d = (t != null ? t : origen).CrearDatos();
            int n = l.nodos.Count;
            if (d.nodos.Count != n)
                continue;   // cambiaste los nodos de la línea: esta ya no se gira
            // Dónde queda cada nodo; los de atrás se pegan al vecino que sí se ve (así no cambia el número de nodos).
            var vis = new bool[n];
            var pos = new Vector3[n];
            int cuantosVis = 0;
            for (int j = 0; j < n; j++)
            {
                vis[j] = Visible(q, l.nodos[j]);
                Vector3 pr = q * l.nodos[j];
                pos[j] = new Vector3(pr.x, pr.y, 0f);
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
            for (int j = 0; j < n; j++)
            {
                Vector3 nodoMundo = Mundo(pos[j]);
                d.nodos[j] = raizDibujo.InverseTransformPoint(nodoMundo);
                if (j < d.asaManual.Count && l.manual[j] && vis[j])
                {
                    d.asaEntrada[j] = raizDibujo.InverseTransformPoint(Mundo(Plano3(q * l.asaE[j]))) - d.nodos[j];
                    d.asaSalida[j] = raizDibujo.InverseTransformPoint(Mundo(Plano3(q * l.asaS[j]))) - d.nodos[j];
                }
                else if (j < d.asaManual.Count)
                {
                    d.asaManual[j] = false;
                }
            }
            d.oculto = cuantosVis == 0;
            if (t == null)
            {
                d.id = 0;
                t = dibujo.AgregarTrazo(d);
                l.idEspejo = t.id;
                nuevos.Add(t.id);
            }
            else
            {
                t.AplicarPose(d, null, 0f);
            }
        }
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
        // La clave en este fotograma (así entre dos vistas hay morph).
        if (anim != null)
        {
            anim.GuardarClaveEn(anim.Fotograma, capaCabeza);
            // Las copias nuevas (el otro ojo...) no existían en las otras claves: allá quedan ocultas.
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
        int f = anim != null ? anim.Fotograma + 1 : 1;
        dibujo.Mensaje("Vista puesta en el fotograma " + f + " (mismos nodos: hace morph con las otras claves)");
        Esconder();
        sucio = true;
    }

    static Vector3 Plano3(Vector3 p)
    {
        return new Vector3(p.x, p.y, 0f);
    }

    // ---------- La biblioteca de vistas ----------

    void GuardarVista()
    {
        if (vistas.Count >= 6)
            vistas.RemoveAt(0);
        float rel = giro - (perfil ? (mira > 0f ? -90f : 90f) : 0f);
        rel = Mathf.Repeat(rel + 180f, 360f) - 180f;
        string nombre = Mathf.Abs(rel) < 15f ? "Frente" : Mathf.Abs(rel) > 70f ? (rel > 0f ? "Perfil der" : "Perfil izq") : (rel > 0f ? "3/4 der" : "3/4 izq");
        vistas.Add(new VistaGuardada { nombre = (vistas.Count + 1) + " · " + Idioma.T(nombre), giro = giro, inclina = inclina });
        RefrescarVistas();
        dibujo.Mensaje("Vista guardada en la biblioteca: tócala en cualquier fotograma");
    }

    void UsarVista(int i)
    {
        if (i < 0 || i >= vistas.Count)
            return;
        giro = vistas[i].giro;
        inclina = vistas[i].inclina;
        sucio = true;
        Usar();
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
            Boton(filaVistas, vistas[i].nombre, new Vector3(-0.155f + i * 0.062f, -0.052f, 0f), 0.058f, false, () => UsarVista(k));
        }
    }

    // ---------- El panel: debajo de la cabeza ----------

    void ArmarPanel()
    {
        if (raiz == null)
        {
            raiz = new GameObject("VistaCabeza").transform;
            var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
            normal = nav != null ? nav.materialBoton : null;
            marcado = nav != null ? nav.materialBotonMarcado : null;
            panel = new GameObject("PanelCabeza").transform;
            panel.SetParent(raiz, false);
            Boton(filaPregunta, "De frente", new Vector3(-0.1f, 0f, 0f), 0.06f, false, () => Elegir(false));
            Boton(filaPregunta, "De perfil", new Vector3(-0.035f, 0f, 0f), 0.06f, false, () => Elegir(true));
            Boton(filaPregunta, "Nombrar nodo", new Vector3(0.035f, 0f, 0f), 0.07f, false, NombrarConTeclado);
            Boton(filaPregunta, "Etiquetas", new Vector3(0.1f, 0f, 0f), 0.05f, false, AlternarEtiquetas);
            Boton(filaPregunta, "Cerrar", new Vector3(0.155f, 0f, 0f), 0.05f, false, Cerrar);
            string[] nombresVista = { "Perfil izq", "3/4 izq", "Frente", "3/4 der", "Perfil der" };
            float[] angulos = { -90f, -45f, 0f, 45f, 90f };
            for (int i = 0; i < nombresVista.Length; i++)
            {
                float a = angulos[i];
                Boton(filaGiro, nombresVista[i], new Vector3(-0.124f + i * 0.062f, 0.02f, 0f), 0.058f, false, () => Vista(a));
            }
            Boton(filaGiro, "Usar esta vista", new Vector3(-0.13f, -0.016f, 0f), 0.075f, true, Usar);
            Boton(filaGiro, "Guardar vista", new Vector3(-0.053f, -0.016f, 0f), 0.07f, false, GuardarVista);
            Boton(filaGiro, "Etiquetas", new Vector3(0.014f, -0.016f, 0f), 0.055f, false, AlternarEtiquetas);
            Boton(filaGiro, "Nueva cabeza", new Vector3(0.08f, -0.016f, 0f), 0.068f, false, () => { Desesconder(); Empezar(); });
            Boton(filaGiro, "Cerrar", new Vector3(0.145f, -0.016f, 0f), 0.05f, false, Cerrar);
        }
        raiz.gameObject.SetActive(true);
        Vector3 pos = centro + arriba * (centro2D.y - semiY - 0.06f) + derecha * centro2D.x + haciaTi * 0.02f;
        panel.SetPositionAndRotation(pos, Quaternion.LookRotation(-haciaTi, arriba));
        sucio = true;
    }

    void Boton(List<GameObject> fila, string texto, Vector3 pos, float ancho, bool oscuro, UnityEngine.Events.UnityAction accion)
    {
        var b = PanelTutoriales.Boton(panel, Idioma.T(texto), pos, new Vector2(ancho, 0.028f), oscuro ? marcado : normal, oscuro ? normal : marcado);
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

    void AlternarEtiquetas()
    {
        verEtiquetas = !verEtiquetas;
        sucio = true;
        dibujo.Mensaje(verEtiquetas ? "Etiquetas: Sí" : "Etiquetas: No");
    }

    // "Nombrar nodo": el teclado de la app, para el nodo elegido en Nodos (o el que miras).
    void NombrarConTeclado()
    {
        TecladoVR.Abrir("Nombre del nodo (ej. punta de nariz)", "", nombre =>
        {
            if (string.IsNullOrEmpty(nombre) || control == null)
                return;
            int n = control.NombrarNodos(nombre.ToLowerInvariant());
            dibujo.Mensaje(n > 0 ? "Nodo: " + nombre : "Primero elige un nodo (en Nodos) o míralo");
            sucio = true;
        });
    }

    void Vista(float angulo)
    {
        float frente = perfil ? (mira > 0f ? -90f : 90f) : 0f;
        giro = frente + angulo;
        inclina = 0f;
        sucio = true;
    }

    void OnDestroy()
    {
        foreach (var m in materiales.Values)
            if (m != null)
                Destroy(m);
        if (instancia == this)
            instancia = null;
    }
}

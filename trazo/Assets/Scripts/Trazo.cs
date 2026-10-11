using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum EstiloLinea { Cinta = 0, Tubo = 1 }

// Una línea del dibujo. Mientras se dibuja usa los puntos "crudos" del dedo; al terminar,
// los simplifica a pocos nodos (los "vectores" editables) unidos por curvas Bézier con asas.
// Grosor con "valor de línea": grueso en el centro y en punta en los extremos.
// Si la línea se cierra (sus puntas se unen), puede tener relleno de color.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Trazo : MonoBehaviour
{
    // Colores del relleno de siempre (cuando la figura no tiene un color de la paleta).
    public static readonly Color[] Paleta =
    {
        new Color(1f, 0.85f, 0.35f),   // amarillo
        new Color(0.55f, 0.78f, 1f),   // celeste
        new Color(1f, 0.6f, 0.7f),     // rosado
        new Color(0.55f, 0.9f, 0.7f),  // menta
        new Color(1f, 0.65f, 0.35f),   // naranja
        new Color(0.75f, 0.65f, 1f),   // lavanda
        new Color(0.85f, 0.85f, 0.85f),// gris claro
        new Color(0.12f, 0.12f, 0.14f) // negro
    };

    public List<Vector3> nodos = new List<Vector3>();       // en coordenadas locales del Dibujo
    public List<Vector3> asaEntrada = new List<Vector3>();  // asas Bézier (desplazamiento desde el nodo)
    public List<Vector3> asaSalida = new List<Vector3>();
    public List<bool> asaManual = new List<bool>();         // false: el asa se calcula sola (curva suave)
    public List<float> grosorNodo = new List<float>();      // grosor propio de cada nodo (1 = normal)
    public int id;
    public int capa;
    public bool visibleAnim = true;                         // false: no existe en este fotograma de la animación
    public bool oculto;                                     // línea "hueso": existe pero no se ve
    public bool cerrado;
    public bool relleno;
    public int colorRelleno;
    [Tooltip("Grosor máximo (en el centro), en unidades locales del Dibujo")]
    public float ancho = 0.008f;
    public Color color = Color.black;                       // color de la línea (la paleta de la mano izquierda)
    // Relleno ABIERTO (la cubeta): se rellena aunque las puntas no se unan, como si una línea invisible las uniera.
    public bool rellenoAbierto;
    // Color del relleno elegido en la paleta (si es transparente, se usa Paleta[colorRelleno]).
    public Color colorFondo;
    // Tinta invisible: la línea no se ve (pero su relleno sí). Se ve gris clarito mientras editas.
    public bool Invisible => color.a < 0.01f;
    public static bool verInvisibles;
    public Color ColorDelRelleno => colorFondo.a > 0.01f ? colorFondo : Paleta[Mathf.Abs(colorRelleno) % Paleta.Length];
    public EstiloLinea estilo = EstiloLinea.Cinta;

    // ---------- Encantamiento 2D: la línea vive en su hoja ----------
    // Una línea dibujada en Plano 2D queda "encantada" en su hoja: sus nodos, tiradores, lazo, plastilina,
    // moverla y girarla se quedan siempre dentro de esa hoja (nunca hacia el fondo ni hacia ti), aunque
    // cambies a 3D. "Liberar" (en la fila de su capa) le quita el encantamiento: vuelve a ser una línea 3D.
    public bool enHoja;
    public Vector3 hojaPunto;                    // la hoja (en coordenadas locales del Dibujo)
    public Vector3 hojaNormal = Vector3.forward;
    public bool EnHoja => enHoja && hojaNormal.sqrMagnitude > 0.25f;

    // ---------- Frente / fondo dentro de su capa ----------
    public int orden;                            // más grande = más adelante (tapa a las otras de su capa)
    [System.NonSerialized] public int nivel;     // escalón visual (lo calcula el Dibujo según con quién se encima)
    public const int NivelesPorCapa = 6;
    float NivelVisual => Mathf.Max(0, capa) * NivelesPorCapa + Mathf.Clamp(nivel, 0, NivelesPorCapa - 1);

    // ---------- Relleno vivo (texturas que se mueven, estilo Quill) ----------
    public int texturaRelleno;                   // 0 liso, 1 facetas, 2 manchas, 3 pinceladas
    public int velocidadTextura = 2;             // índice en VelocidadesTextura
    public float escalaTextura = 1f;             // tamaño de las manchas (x1 normal; se cambia abriendo y cerrando 2 dedos)
    public const float EscalaTexturaMin = 0.25f, EscalaTexturaMax = 8f;
    // Los archivos de antes guardaban el tamaño como uno de estos 4 escalones (Chico, Normal, Grande, Enorme).
    public static readonly float[] TamanosTextura = { 0.5f, 1f, 2f, 4f };
    public static float LimitarEscala(float e) => Mathf.Clamp(e > 0f ? e : 1f, EscalaTexturaMin, EscalaTexturaMax);
    public static readonly float[] VelocidadesTextura = { 0f, 2f, 4f, 8f }; // cambios por segundo (0 = quieto)
    public static readonly string[] NombresTextura = { "Liso", "Facetas", "Manchas", "Pinceladas" };
    public static readonly string[] NombresVelocidadTextura = { "Quieto", "Lento", "Medio", "Rápido" };

    // ---------- Puntos de TELA (relieve suave dentro del relleno) ----------
    // Puntos sueltos dentro del relleno (no tienen que ver con la línea): al moverlos, la "tela" de alrededor los
    // sigue en forma de campana suave (nunca en punta) y el borde se queda pegado a la línea.
    public List<Vector3> telaBase = new List<Vector3>();    // dónde está cada punto sobre el relleno (local)
    public List<Vector3> telaMovida = new List<Vector3>();  // cuánto se movió (local)
    public bool TieneTela => telaBase.Count > 0 && telaBase.Count == telaMovida.Count;

    // La curva ya calculada (local), para tocarla y medirla.
    public readonly List<Vector3> curva = new List<Vector3>();
    readonly List<int> curvaSegmento = new List<int>();   // en qué tramo (entre dos nodos) cae cada punto
    readonly List<float> curvaT = new List<float>();      // y en qué parte del tramo (0 a 1)
    public bool PoligonoValido { get; private set; }

    // La animación aplica poses sin que cuenten como "cambios del usuario".
    public static bool silenciar;
    public static bool huboCambio;
    // Alguna línea cambió de forma (para recalcular frente/fondo), también al animar.
    public static bool formaCambio;

    // Líneas vivas (temblor, hebras, grosor vivo): cada capa tiene las suyas. Las da el Dibujo.
    public struct EstiloVivo
    {
        public int hebras;         // 1, 3 o 5 hebras finas por línea
        public float grosorHebra;  // grosor de cada hebra (veces el de la línea)
        public Vector4 a;          // amplitud del temblor, separación de hebras, grosor vivo, cambios por segundo
        public Vector4 b;          // ciclo de 3 (1/0), frecuencia del ruido (suavidad)
    }
    public static System.Func<int, EstiloVivo> estiloCapa;

    EstiloVivo Estilo()
    {
        EstiloVivo e;
        if (estiloCapa != null)
        {
            e = estiloCapa(capa);
        }
        else
        {
            e = new EstiloVivo { a = new Vector4(0f, 0f, 0f, 8f), b = new Vector4(1f, 9f, 0f, 0f) };
        }
        if (e.hebras < 1) e.hebras = 1;
        if (e.grosorHebra <= 0f) e.grosorHebra = 1f;
        return e;
    }

    // Para el SVG "como se ve": el estilo vivo de su capa (hebras, grosor vivo...).
    public EstiloVivo EstiloActual => Estilo();

    // Para el SVG "como se ve": los puntos de la curva (locales) y el medio grosor en cada uno (local),
    // igual que los usa la malla de la línea (con las puntas finitas y el grosor de cada nodo).
    public void PuntosConGrosor(List<Vector3> puntos, List<float> medios)
    {
        puntos.Clear();
        medios.Clear();
        if (nodos.Count < 2 || crudos.Count > 0)
            return;
        muestras.Clear();
        multiplicadores.Clear();
        AsegurarAsas();
        MuestrearBezier(muestras);
        largos.Clear();
        float total = 0f;
        largos.Add(0f);
        for (int i = 1; i < muestras.Count; i++)
        {
            total += Vector3.Distance(muestras[i - 1], muestras[i]);
            largos.Add(total);
        }
        if (total < 1e-5f)
            return;
        for (int i = 0; i < muestras.Count; i++)
        {
            puntos.Add(muestras[i]);
            medios.Add(MedioGrosor(i, total));
        }
    }

    // Para grabar el proceso: qué líneas cambiaron desde la última muestra.
    public static bool registrarCambios;
    public static readonly HashSet<Trazo> modificados = new HashSet<Trazo>();
    // Líneas que cambiaron (para saber en qué capa poner la clave automática).
    public static readonly HashSet<Trazo> cambiadosAnim = new HashSet<Trazo>();

    const float separacionCrudos = 0.003f;       // metros entre puntos al dibujar
    const float toleranciaSimplificar = 0.003f;  // cuánto puede alejarse la curva al simplificar
    const float pasoMuestras = 0.004f;           // detalle de la curva final
    const int ladosTubo = 8;
    const float puntaMinima = 0.06f;             // grosor de las puntas (fracción del centro)
    const int maxPuntosRelleno = 200;            // el relleno sigue la curva de cerca (así no se sale de la línea)
    const int maxPuntosRellenoDibujando = 64;    // mientras dibujas con la cubeta, menos (va más rápido)

    readonly List<Vector3> crudos = new List<Vector3>();
    Mesh malla;
    Mesh mallaRelleno;
    MeshRenderer rellenoRenderer;
    Material materialRelleno;

    Vector3 poliCentro, poliU, poliV, poliN;
    readonly List<Vector3> poli3D = new List<Vector3>();
    readonly List<Vector2> poli2D = new List<Vector2>();

    static readonly List<Vector3> muestras = new List<Vector3>();
    static readonly List<float> largos = new List<float>();
    static readonly List<float> multiplicadores = new List<float>();
    static readonly List<Vector3> vertices = new List<Vector3>();
    static readonly List<Vector3> normales = new List<Vector3>();
    static readonly List<Vector3> uvs = new List<Vector3>();   // x: medio grosor (cinta), y: 0 cinta / 1 tubo, z: nivel (frente/fondo)
    static readonly List<Vector4> uvsRelleno = new List<Vector4>();  // relleno: x, y en su plano; z textura; w cambios por segundo
    static readonly List<Vector2> uvsRelleno2 = new List<Vector2>(); // relleno: x semilla, y nivel (frente/fondo)
    static readonly List<Vector2> uvs2 = new List<Vector2>(); // x: número de hebra, y: lugar a lo largo (0 a 1)
    static readonly List<Vector4> uvs3 = new List<Vector4>(); // estilo vivo (a)
    static readonly List<Vector4> uvs4 = new List<Vector4>(); // estilo vivo (b)
    static readonly List<int> indices = new List<int>();
    static readonly List<int> restantes = new List<int>();
    static readonly List<Color> colores = new List<Color>();

    public bool Dibujando => crudos.Count > 0;

    float Escala => Mathf.Max(0.0001f, transform.lossyScale.x);

    public float Largo
    {
        get
        {
            float total = 0f;
            for (int i = 1; i < curva.Count; i++)
                total += Vector3.Distance(curva[i - 1], curva[i]);
            return total;
        }
    }

    // Para el halo (Dibujo lo dibuja con la misma malla de la línea).
    public Mesh Malla => malla;
    MeshRenderer rendererLinea;
    public MeshRenderer RendererLinea => rendererLinea != null ? rendererLinea : (rendererLinea = GetComponent<MeshRenderer>());

    public void Encantar(Vector3 punto, Vector3 normal)
    {
        if (normal.sqrMagnitude < 1e-8f)
            return;
        enHoja = true;
        hojaPunto = punto;
        hojaNormal = normal.normalized;
    }

    public Vector3 ProyectarEnHoja(Vector3 local)
    {
        return EnHoja ? local - hojaNormal * Vector3.Dot(local - hojaPunto, hojaNormal) : local;
    }

    public Vector3 ProyectarVectorEnHoja(Vector3 v)
    {
        return EnHoja ? v - hojaNormal * Vector3.Dot(v, hojaNormal) : v;
    }

    // ¿Todos sus nodos están sobre este plano? (tolerancia en unidades locales)
    public bool SobrePlano(Vector3 punto, Vector3 normal, float tolerancia)
    {
        if (nodos.Count < 2 || normal.sqrMagnitude < 1e-8f)
            return false;
        Vector3 n = normal.normalized;
        foreach (var p in nodos)
            if (Mathf.Abs(Vector3.Dot(p - punto, n)) > tolerancia)
                return false;
        return true;
    }

    // Pone todos sus nodos y tiradores dentro de su hoja (sin reconstruir).
    void PegarAHoja()
    {
        if (!EnHoja)
            return;
        for (int i = 0; i < nodos.Count; i++)
            nodos[i] = ProyectarEnHoja(nodos[i]);
        for (int i = 0; i < asaEntrada.Count; i++)
            asaEntrada[i] = ProyectarVectorEnHoja(asaEntrada[i]);
        for (int i = 0; i < asaSalida.Count; i++)
            asaSalida[i] = ProyectarVectorEnHoja(asaSalida[i]);
    }

    // Lo que no es "pose" (no cambia entre claves): la hoja, el orden y la textura del relleno.
    public void AplicarPropiedades(DatosTrazo d)
    {
        if (d == null)
            return;
        enHoja = d.enHoja && d.hojaNormal.sqrMagnitude > 0.25f;
        hojaPunto = d.hojaPunto;
        hojaNormal = d.hojaNormal.sqrMagnitude > 1e-8f ? d.hojaNormal.normalized : Vector3.forward;
        orden = d.orden;
        texturaRelleno = Mathf.Clamp(d.texturaRelleno, 0, NombresTextura.Length - 1);
        velocidadTextura = Mathf.Clamp(d.velocidadTextura, 0, VelocidadesTextura.Length - 1);
        // escalaTextura 0 = archivo de antes: usa su escalón.
        escalaTextura = d.escalaTextura > 0f ? LimitarEscala(d.escalaTextura)
                                             : TamanosTextura[Mathf.Clamp(d.tamanoTextura, 0, TamanosTextura.Length - 1)];
    }

    // El escalón visual de frente/fondo (lo pone el Dibujo). Rehace la malla solo si cambió.
    public void PonerNivel(int n)
    {
        n = Mathf.Clamp(n, 0, NivelesPorCapa - 1);
        if (n == nivel)
            return;
        nivel = n;
        bool antes = silenciar;
        silenciar = true;
        Reconstruir();
        silenciar = antes;
    }

    // La textura del relleno vivo (se ve al instante).
    public void PonerTexturaRelleno(int textura, int velocidad, float escala)
    {
        textura = Mathf.Clamp(textura, 0, NombresTextura.Length - 1);
        velocidad = Mathf.Clamp(velocidad, 0, VelocidadesTextura.Length - 1);
        escala = LimitarEscala(escala);
        if (textura == texturaRelleno && velocidad == velocidadTextura && Mathf.Abs(escala - escalaTextura) < 1e-4f)
            return;
        texturaRelleno = textura;
        velocidadTextura = velocidad;
        escalaTextura = escala;
        Reconstruir();
    }

    // Solo el tamaño de la textura (mientras abres y cierras los dedos). Se ve al instante.
    public void PonerEscalaTextura(float escala)
    {
        escala = LimitarEscala(escala);
        if (Mathf.Abs(escala - escalaTextura) < 1e-4f)
            return;
        escalaTextura = escala;
        Reconstruir();
    }

    // Los límites de la línea y su relleno (locales del Dibujo), para saber con quién se encima.
    public bool Limites(out Bounds b)
    {
        b = new Bounds();
        bool hay = false;
        if (malla != null && malla.vertexCount > 0)
        {
            b = malla.bounds;
            hay = true;
        }
        if (mallaRelleno != null && rellenoRenderer != null && rellenoRenderer.gameObject.activeSelf && mallaRelleno.vertexCount > 0)
        {
            if (hay)
                b.Encapsulate(mallaRelleno.bounds);
            else
                b = mallaRelleno.bounds;
            hay = true;
        }
        return hay;
    }

    public void Configurar(Material material, Material rellenoMat, float anchoInicial, EstiloLinea estiloInicial)
    {
        ancho = anchoInicial;
        estilo = estiloInicial;
        materialRelleno = rellenoMat;
        var mr = GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        AsegurarMalla();
    }

    void AsegurarMalla()
    {
        if (malla != null)
            return;
        malla = new Mesh { name = "Trazo" };
        malla.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = malla;
    }

    void OnDestroy()
    {
        if (malla != null)
            Destroy(malla);
        if (mallaRelleno != null)
            Destroy(mallaRelleno);
    }

    // ---------- Dibujar ----------

    // Agrega un punto mientras se dibuja (en coordenadas locales del Dibujo).
    public void AgregarPuntoCrudo(Vector3 p)
    {
        float sep = separacionCrudos / Escala;
        if (crudos.Count > 0 && (p - crudos[crudos.Count - 1]).sqrMagnitude < sep * sep)
            return;
        crudos.Add(p);
        Reconstruir();
    }

    public float LargoCrudo => LargoDe(crudos);

    // Pone una línea "a mano alzada" con estos puntos (para la repetición y las manos fantasma).
    public void PonerCrudos(List<Vector3> puntos)
    {
        crudos.Clear();
        if (puntos != null)
            crudos.AddRange(puntos);
        Reconstruir();
    }

    // Como CrearDatos, pero si la línea se está dibujando guarda sus puntos crudos.
    public DatosTrazo CrearDatosRepeticion()
    {
        var d = CrearDatos();
        if (crudos.Count > 0)
        {
            d.nodos = new List<Vector3>(crudos);
            d.asaEntrada.Clear();
            d.asaSalida.Clear();
            d.asaManual.Clear();
            d.grosorNodo.Clear();
            d.crudo = true;
        }
        return d;
    }

    // Línea recta mientras se dibuja: solo dos puntos, inicio y fin.
    public void PonerRecta(Vector3 a, Vector3 b)
    {
        crudos.Clear();
        crudos.Add(a);
        crudos.Add(b);
        Reconstruir();
    }

    // Convierte el trazo crudo en nodos. Devuelve false si quedó demasiado corto.
    public bool Terminar()
    {
        if (crudos.Count < 2 || LargoDe(crudos) < 0.01f / Escala)
        {
            crudos.Clear();
            return false;
        }
        nodos.Clear();
        asaEntrada.Clear();
        asaSalida.Clear();
        asaManual.Clear();
        grosorNodo.Clear();
        cerrado = false;
        relleno = rellenoAbierto; // con la cubeta, el relleno se queda aunque no se cierre
        Simplificar(crudos, toleranciaSimplificar / Escala, nodos);
        crudos.Clear();
        Reconstruir();
        return nodos.Count >= 2;
    }

    // ---------- Editar ----------

    public void AsegurarAsas()
    {
        int n = nodos.Count;
        if (asaEntrada == null) asaEntrada = new List<Vector3>();
        if (asaSalida == null) asaSalida = new List<Vector3>();
        if (asaManual == null) asaManual = new List<bool>();
        if (grosorNodo == null) grosorNodo = new List<float>();
        Ajustar(grosorNodo, n, 1f);
        Ajustar(asaEntrada, n, Vector3.zero);
        Ajustar(asaSalida, n, Vector3.zero);
        Ajustar(asaManual, n, false);
        for (int i = 0; i < n; i++)
            if (!asaManual[i])
                AsaAutomatica(i);
    }

    static void Ajustar<T>(List<T> lista, int n, T valor)
    {
        while (lista.Count < n)
            lista.Add(valor);
        if (lista.Count > n)
            lista.RemoveRange(n, lista.Count - n);
    }

    // Asas automáticas: la curva pasa suave por el nodo, siguiendo a sus vecinos.
    void AsaAutomatica(int i)
    {
        int n = nodos.Count;
        Vector3 p = nodos[i];
        bool hayAnterior = i > 0 || cerrado;
        bool haySiguiente = i < n - 1 || cerrado;
        Vector3 anterior = hayAnterior ? nodos[(i - 1 + n) % n] : p;
        Vector3 siguiente = haySiguiente ? nodos[(i + 1) % n] : p;
        Vector3 dir = siguiente - anterior;
        if (dir.sqrMagnitude < 1e-12f)
        {
            asaEntrada[i] = Vector3.zero;
            asaSalida[i] = Vector3.zero;
            return;
        }
        dir.Normalize();
        asaEntrada[i] = -dir * (Vector3.Distance(p, anterior) / 3f);
        asaSalida[i] = dir * (Vector3.Distance(siguiente, p) / 3f);
    }

    // ¿Esta asa se usa? (las puntas de una línea abierta solo tienen un asa)
    public bool AsaUsada(int i, bool salida)
    {
        if (cerrado)
            return true;
        return salida ? i < nodos.Count - 1 : i > 0;
    }

    public void MoverNodo(int i, Vector3 posicionLocal)
    {
        if (i < 0 || i >= nodos.Count)
            return;
        nodos[i] = posicionLocal;
        Reconstruir();
    }

    // Para mover varios nodos juntos: se cambian todos y al final se llama Reconstruir una sola vez.
    public void MoverNodoSinReconstruir(int i, Vector3 posicionLocal)
    {
        if (i >= 0 && i < nodos.Count)
            nodos[i] = posicionLocal;
    }

    // Mueve un asa; la otra asa del mismo nodo gira en espejo (nodo suave, como en Illustrator).
    public void MoverAsa(int i, bool salida, Vector3 desplazamiento)
    {
        AsegurarAsas();
        if (i < 0 || i >= nodos.Count)
            return;
        asaManual[i] = true;
        float largoOtra = (salida ? asaEntrada[i] : asaSalida[i]).magnitude;
        if (largoOtra < 1e-6f)
            largoOtra = desplazamiento.magnitude;
        Vector3 espejo = desplazamiento.sqrMagnitude > 1e-12f ? -desplazamiento.normalized * largoOtra : Vector3.zero;
        if (salida)
        {
            asaSalida[i] = desplazamiento;
            asaEntrada[i] = espejo;
        }
        else
        {
            asaEntrada[i] = desplazamiento;
            asaSalida[i] = espejo;
        }
        Reconstruir();
    }

    // Mueve SOLO un asa (la otra se queda como estaba): el nodo queda como esquina (como Alt en Illustrator).
    // Un asa en cero = ese lado sale recto del nodo.
    public void MoverAsaSola(int i, bool salida, Vector3 desplazamiento)
    {
        AsegurarAsas();
        if (i < 0 || i >= nodos.Count)
            return;
        asaManual[i] = true;
        if (salida)
            asaSalida[i] = desplazamiento;
        else
            asaEntrada[i] = desplazamiento;
        Reconstruir();
    }

    public void ReiniciarAsa(int i)
    {
        AsegurarAsas();
        if (i < 0 || i >= nodos.Count)
            return;
        asaManual[i] = false;
        Reconstruir();
    }

    public void QuitarNodo(int i)
    {
        AsegurarAsas();
        if (i < 0 || i >= nodos.Count)
            return;
        nodos.RemoveAt(i);
        asaEntrada.RemoveAt(i);
        asaSalida.RemoveAt(i);
        asaManual.RemoveAt(i);
        grosorNodo.RemoveAt(i);
        if (cerrado && nodos.Count < 3)
        {
            cerrado = false;
            relleno = false;
        }
        Reconstruir();
    }

    // Da la vuelta a la línea (el inicio pasa a ser el final).
    public void Invertir()
    {
        AsegurarAsas();
        nodos.Reverse();
        asaManual.Reverse();
        grosorNodo.Reverse();
        asaEntrada.Reverse();
        asaSalida.Reverse();
        var temporal = asaEntrada;
        asaEntrada = asaSalida;
        asaSalida = temporal;
    }

    // Une las dos puntas en un solo nodo y activa el relleno.
    public bool Cerrar(bool quitarUltimo)
    {
        AsegurarAsas();
        if (cerrado || nodos.Count < 4)
            return false;
        int q = quitarUltimo ? nodos.Count - 1 : 0;
        nodos.RemoveAt(q);
        asaEntrada.RemoveAt(q);
        asaSalida.RemoveAt(q);
        asaManual.RemoveAt(q);
        grosorNodo.RemoveAt(q);
        asaManual[0] = false;
        asaManual[nodos.Count - 1] = false;
        cerrado = true;
        relleno = true;
        Reconstruir();
        return true;
    }

    // Mueve toda la línea (los nodos parten de "baseNodos" y se suman "delta").
    public void Desplazar(List<Vector3> baseNodos, Vector3 delta)
    {
        int n = Mathf.Min(nodos.Count, baseNodos.Count);
        for (int i = 0; i < n; i++)
            nodos[i] = baseNodos[i] + delta;
        Reconstruir();
    }

    // Gira/escala/mueve solo esta línea, partiendo de su forma "origen" (m: de local a local).
    public void TransformarDesde(DatosTrazo origen, Matrix4x4 m, float escala)
    {
        if (origen == null || origen.nodos == null || origen.nodos.Count != nodos.Count)
            return;
        AsegurarAsas();
        int n = nodos.Count;
        bool asas = Completa(origen.asaEntrada, n) && Completa(origen.asaSalida, n);
        for (int i = 0; i < n; i++)
        {
            nodos[i] = m.MultiplyPoint3x4(origen.nodos[i]);
            if (asas)
            {
                asaEntrada[i] = m.MultiplyVector(origen.asaEntrada[i]);
                asaSalida[i] = m.MultiplyVector(origen.asaSalida[i]);
            }
        }
        ancho = Mathf.Clamp(origen.ancho * escala, 0.0005f, 0.5f);
        // Los puntos de tela giran y se mueven con la figura.
        if (origen.telaBase != null && origen.telaMovida != null && origen.telaBase.Count == telaBase.Count && origen.telaMovida.Count == telaMovida.Count)
            for (int k = 0; k < telaBase.Count; k++)
            {
                telaBase[k] = m.MultiplyPoint3x4(origen.telaBase[k]);
                telaMovida[k] = m.MultiplyVector(origen.telaMovida[k]);
            }
        PegarAHoja(); // encantada: se queda en su hoja
        Reconstruir();
    }

    // Cambia el material de la línea (para mostrarla seleccionada).
    public void PonerMaterialLinea(Material material)
    {
        var mr = GetComponent<MeshRenderer>();
        if (material != null && mr.sharedMaterial != material)
            mr.sharedMaterial = material;
    }

    public void PonerGrosorNodo(int i, float multiplicador)
    {
        AsegurarAsas();
        if (i < 0 || i >= nodos.Count)
            return;
        grosorNodo[i] = Mathf.Clamp(multiplicador, 0.1f, 6f);
        Reconstruir(true);
    }

    public float GrosorDeNodo(int i)
    {
        AsegurarAsas();
        return i >= 0 && i < grosorNodo.Count ? grosorNodo[i] : 1f;
    }

    // ---------- Poses (para guardar y para la animación) ----------

    public DatosTrazo CrearDatos()
    {
        AsegurarAsas();
        return new DatosTrazo
        {
            id = id,
            capa = capa,
            nodos = new List<Vector3>(nodos),
            asaEntrada = new List<Vector3>(asaEntrada),
            asaSalida = new List<Vector3>(asaSalida),
            asaManual = new List<bool>(asaManual),
            grosorNodo = new List<float>(grosorNodo),
            cerrado = cerrado,
            relleno = relleno,
            colorRelleno = colorRelleno,
            ancho = ancho,
            estilo = (int)estilo,
            oculto = oculto,
            color = color,
            rellenoAbierto = rellenoAbierto && !cerrado,
            colorFondo = colorFondo,
            enHoja = EnHoja,
            hojaPunto = hojaPunto,
            hojaNormal = hojaNormal,
            orden = orden,
            texturaRelleno = texturaRelleno,
            velocidadTextura = velocidadTextura,
            escalaTextura = escalaTextura,
            tamanoTextura = EscalonMasCercano(escalaTextura),
            telaBase = new List<Vector3>(telaBase),
            telaMovida = new List<Vector3>(telaMovida)
        };
    }

    // Para los archivos: el escalón de antes más parecido (así una versión vieja del app también lo abre).
    static int EscalonMasCercano(float escala)
    {
        int mejor = 1;
        float d = float.MaxValue;
        for (int i = 0; i < TamanosTextura.Length; i++)
        {
            float e = Mathf.Abs(Mathf.Log(TamanosTextura[i]) - Mathf.Log(LimitarEscala(escala)));
            if (e < d)
            {
                d = e;
                mejor = i;
            }
        }
        return mejor;
    }

    static bool Completa<T>(List<T> lista, int n)
    {
        return lista != null && lista.Count == n;
    }

    // Pone la forma "a", o una mezcla entre "a" y "b" (u = 0..1) si tienen los mismos nodos (morph).
    public void AplicarPose(DatosTrazo a, DatosTrazo b, float u)
    {
        if (a == null || a.nodos == null || a.nodos.Count < 2)
            return;
        crudos.Clear();
        int n = a.nodos.Count;
        bool mezclar = b != null && u > 0f && Completa(b.nodos, n) && b.cerrado == a.cerrado;
        bool asasA = Completa(a.asaEntrada, n) && Completa(a.asaSalida, n) && Completa(a.asaManual, n);
        bool asasB = mezclar && Completa(b.asaEntrada, n) && Completa(b.asaSalida, n);
        bool grosA = Completa(a.grosorNodo, n);
        bool grosB = mezclar && Completa(b.grosorNodo, n);

        nodos.Clear();
        asaEntrada.Clear();
        asaSalida.Clear();
        asaManual.Clear();
        grosorNodo.Clear();
        for (int i = 0; i < n; i++)
        {
            nodos.Add(mezclar ? Vector3.Lerp(a.nodos[i], b.nodos[i], u) : a.nodos[i]);
            if (asasA)
            {
                asaEntrada.Add(asasB ? Vector3.Lerp(a.asaEntrada[i], b.asaEntrada[i], u) : a.asaEntrada[i]);
                asaSalida.Add(asasB ? Vector3.Lerp(a.asaSalida[i], b.asaSalida[i], u) : a.asaSalida[i]);
                asaManual.Add(a.asaManual[i]);
            }
            float g = grosA ? a.grosorNodo[i] : 1f;
            if (grosB)
                g = Mathf.Lerp(g, b.grosorNodo[i], u);
            grosorNodo.Add(g);
        }
        cerrado = a.cerrado && n >= 3;
        rellenoAbierto = a.rellenoAbierto && !cerrado;
        relleno = a.relleno && (cerrado || rellenoAbierto);
        colorFondo = a.colorFondo;
        oculto = a.oculto;
        colorRelleno = a.colorRelleno;
        ancho = mezclar ? Mathf.Lerp(a.ancho, b.ancho, u) : a.ancho;
        if (ancho <= 0f)
            ancho = 0.008f;
        // Los puntos de tela también son parte de la pose (se mezclan si las dos poses tienen los mismos).
        telaBase.Clear();
        telaMovida.Clear();
        int nt = a.telaBase != null && a.telaMovida != null && a.telaBase.Count == a.telaMovida.Count ? a.telaBase.Count : 0;
        bool telaB = mezclar && nt > 0 && b.telaBase != null && b.telaMovida != null && b.telaBase.Count == nt && b.telaMovida.Count == nt;
        for (int k = 0; k < nt; k++)
        {
            telaBase.Add(telaB ? Vector3.Lerp(a.telaBase[k], b.telaBase[k], u) : a.telaBase[k]);
            telaMovida.Add(telaB ? Vector3.Lerp(a.telaMovida[k], b.telaMovida[k], u) : a.telaMovida[k]);
        }
        Reconstruir();
    }

    // ---------- Tocar ----------

    // Agrega un nodo en el tramo "segmento" (en la parte t, de 0 a 1) sin cambiar la forma.
    public void InsertarNodo(int segmento, float t)
    {
        AsegurarAsas();
        NodosUtil.Insertar(nodos, asaEntrada, asaSalida, asaManual, grosorNodo, cerrado, segmento, t);
        Reconstruir();
    }

    // Busca el punto de la curva más cercano: en qué tramo está y a qué distancia (local).
    public bool PuntoEnCurva(Vector3 local, out int segmento, out float t, out float distancia)
    {
        segmento = -1;
        t = 0f;
        distancia = float.MaxValue;
        if (crudos.Count > 0 || curvaSegmento.Count != curva.Count)
            return false;
        for (int i = 0; i < curva.Count; i++)
        {
            float d = Vector3.Distance(local, curva[i]);
            if (d < distancia)
            {
                distancia = d;
                segmento = curvaSegmento[i];
                t = curvaT[i];
            }
        }
        return segmento >= 0;
    }

    public float DistanciaACurva(Vector3 local)
    {
        float mejor = float.MaxValue;
        for (int i = 1; i < curva.Count; i++)
            mejor = Mathf.Min(mejor, DistanciaASegmento(local, curva[i - 1], curva[i]));
        return mejor;
    }

    // ¿El punto está dentro de la figura cerrada (y cerca de su superficie)?
    public bool DentroDeRelleno(Vector3 local, float grosorLocal)
    {
        if (!PoligonoValido)
            return false;
        Vector3 d = local - poliCentro;
        if (Mathf.Abs(Vector3.Dot(d, poliN)) > grosorLocal)
            return false;
        var q = new Vector2(Vector3.Dot(d, poliU), Vector3.Dot(d, poliV));
        return PuntoEnPoligono(q, poli2D);
    }

    // ---------- Construir la malla ----------

    public void Reconstruir()
    {
        Reconstruir(false);
    }

    // soloLinea = true cuando solo cambia el grosor o el estilo (el relleno no cambia).
    public void Reconstruir(bool soloLinea)
    {
        AsegurarMalla();
        var mr = GetComponent<MeshRenderer>();
        bool sinLinea = oculto || (Invisible && !verInvisibles);
        if (mr != null && mr.forceRenderingOff != sinLinea)
            mr.forceRenderingOff = sinLinea;
        if (rellenoRenderer != null && rellenoRenderer.forceRenderingOff != oculto)
            rellenoRenderer.forceRenderingOff = oculto;
        formaCambio = true;
        if (!silenciar)
        {
            huboCambio = true;
            cambiadosAnim.Add(this);
        }
        if (registrarCambios)
            modificados.Add(this);
        muestras.Clear();
        multiplicadores.Clear();
        if (crudos.Count > 0)
        {
            muestras.AddRange(crudos);
            for (int i = 0; i < crudos.Count; i++)
                multiplicadores.Add(1f);
        }
        else
        {
            AsegurarAsas();
            MuestrearBezier(muestras);
        }
        curva.Clear();
        curva.AddRange(muestras);
        if (crudos.Count > 0)
        {
            curvaSegmento.Clear();
            curvaT.Clear();
        }

        if (!soloLinea)
            ConstruirRelleno();

        malla.Clear();
        if (muestras.Count < 2)
            return;

        largos.Clear();
        float total = 0f;
        largos.Add(0f);
        for (int i = 1; i < muestras.Count; i++)
        {
            total += Vector3.Distance(muestras[i - 1], muestras[i]);
            largos.Add(total);
        }
        if (total < 1e-5f)
            return;

        vertices.Clear();
        normales.Clear();
        uvs.Clear();
        uvs2.Clear();
        uvs3.Clear();
        uvs4.Clear();
        indices.Clear();
        if (estilo == EstiloLinea.Tubo)
            ConstruirTubo(total);
        else
            ConstruirCinta(total);

        malla.indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        malla.SetVertices(vertices);
        malla.SetNormals(normales);
        malla.SetUVs(0, uvs);
        if (uvs2.Count == vertices.Count)
            malla.SetUVs(1, uvs2);
        if (uvs3.Count == vertices.Count)
        {
            malla.SetUVs(2, uvs3);
            malla.SetUVs(3, uvs4);
        }
        malla.SetTriangles(indices, 0);
        malla.RecalculateBounds();
        var caja = malla.bounds;
        caja.Expand(ancho * 2f);
        malla.bounds = caja;
    }

    bool CerradoAhora => cerrado && crudos.Count == 0;

    // Medio grosor en un punto del trazo: grueso en el centro, en punta en los extremos.
    float MedioGrosor(int i, float total)
    {
        float recorrido = largos[i];
        float m = i < multiplicadores.Count ? multiplicadores[i] : 1f;
        if (CerradoAhora)
            return ancho * 0.5f * 0.85f * m;
        float t = Mathf.Clamp01(recorrido / total);
        float seno = Mathf.Max(0f, Mathf.Sin(Mathf.PI * t));
        float perfil = Mathf.Max(puntaMinima, Mathf.Pow(seno, 0.55f));
        return ancho * 0.5f * perfil * m;
    }

    Vector3 Tangente(int i)
    {
        int n = muestras.Count;
        int a = i - 1;
        int b = i + 1;
        if (CerradoAhora && n > 2)
        {
            if (a < 0) a = n - 2;
            if (b > n - 1) b = 1;
        }
        a = Mathf.Clamp(a, 0, n - 1);
        b = Mathf.Clamp(b, 0, n - 1);
        Vector3 d = muestras[b] - muestras[a];
        return d.sqrMagnitude > 1e-12f ? d.normalized : Vector3.forward;
    }

    // Cinta: dos vértices por punto en el mismo lugar; el shader los abre mirando a la cámara.
    // Con varias hebras, la cinta se repite (más delgada); el shader mueve cada hebra por su lado.
    void ConstruirCinta(float total)
    {
        var e = Estilo();
        int n = e.hebras;
        float delgada = n > 1 ? e.grosorHebra : 1f;
        float nv = NivelVisual;
        for (int hebra = 0; hebra < n; hebra++)
        {
            int inicio = vertices.Count;
            for (int i = 0; i < muestras.Count; i++)
            {
                Vector3 p = muestras[i];
                Vector3 t = Tangente(i);
                float h = MedioGrosor(i, total) * delgada;
                float a = total > 0f ? largos[i] / total : 0f;
                vertices.Add(p); normales.Add(t); uvs.Add(new Vector3(-h, 0f, nv)); uvs2.Add(new Vector2(hebra, a)); uvs3.Add(e.a); uvs4.Add(e.b);
                vertices.Add(p); normales.Add(t); uvs.Add(new Vector3(h, 0f, nv)); uvs2.Add(new Vector2(hebra, a)); uvs3.Add(e.a); uvs4.Add(e.b);
                if (i > 0)
                {
                    int b = inicio + (i - 1) * 2;
                    indices.Add(b); indices.Add(b + 2); indices.Add(b + 1);
                    indices.Add(b + 1); indices.Add(b + 2); indices.Add(b + 3);
                }
            }
        }
    }

    // Tubo 3D real (anillos alrededor de la curva).
    void ConstruirTubo(float total)
    {
        Vector3 n = Vector3.zero;
        for (int i = 0; i < muestras.Count; i++)
        {
            Vector3 p = muestras[i];
            Vector3 t = Tangente(i);
            if (i == 0)
            {
                n = Perpendicular(t);
            }
            else
            {
                n = n - t * Vector3.Dot(n, t);
                if (n.sqrMagnitude < 1e-8f)
                    n = Perpendicular(t);
                n.Normalize();
            }
            Vector3 b = Vector3.Cross(t, n);
            float h = MedioGrosor(i, total);
            for (int k = 0; k < ladosTubo; k++)
            {
                float ang = k * Mathf.PI * 2f / ladosTubo;
                Vector3 dir = n * Mathf.Cos(ang) + b * Mathf.Sin(ang);
                vertices.Add(p + dir * h);
                normales.Add(dir);
                uvs.Add(new Vector3(0f, 1f, NivelVisual));
            }
            if (i > 0)
            {
                int a = (i - 1) * ladosTubo;
                int c = i * ladosTubo;
                for (int k = 0; k < ladosTubo; k++)
                {
                    int k2 = (k + 1) % ladosTubo;
                    indices.Add(a + k); indices.Add(c + k); indices.Add(a + k2);
                    indices.Add(a + k2); indices.Add(c + k); indices.Add(c + k2);
                }
            }
        }
    }

    // ---------- Relleno ----------

    void ConstruirRelleno()
    {
        PoligonoValido = false;
        // Cerrada; o abierta ya terminada (para poder rellenarla tocándola); o con la cubeta mientras la dibujas.
        if ((CerradoAhora || (!cerrado && (crudos.Count == 0 || rellenoAbierto))) && curva.Count >= 4)
            PrepararPoligono();

        bool mostrar = PoligonoValido && relleno;
        if (!mostrar)
        {
            if (rellenoRenderer != null && rellenoRenderer.gameObject.activeSelf)
                rellenoRenderer.gameObject.SetActive(false);
            return;
        }

        if (rellenoRenderer == null)
        {
            var go = new GameObject("Relleno");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            mallaRelleno = new Mesh { name = "Relleno" };
            mallaRelleno.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mallaRelleno;
            rellenoRenderer = go.AddComponent<MeshRenderer>();
            rellenoRenderer.sharedMaterial = materialRelleno;
            rellenoRenderer.shadowCastingMode = ShadowCastingMode.Off;
            rellenoRenderer.receiveShadows = false;
        }
        if (!rellenoRenderer.gameObject.activeSelf)
            rellenoRenderer.gameObject.SetActive(true);

        vertices.Clear();
        indices.Clear();
        colores.Clear();
        // Con puntos de tela: una malla más fina que se dobla. Si no, la de siempre (pocos triángulos).
        if (!(TieneTela && !Dibujando && ArmarMallaTela(vertices, indices)))
        {
            vertices.Clear();
            indices.Clear();
            vertices.AddRange(poli3D);
            Triangular(poli2D, indices, vertices);
            MeterBordeBajoLinea(vertices, poli2D, poli3D.Count);
        }
        Color c = ColorDelRelleno;
        var estiloVivo = Estilo();
        uvs3.Clear();
        uvs4.Clear();
        uvsRelleno.Clear();
        uvsRelleno2.Clear();
        // Relleno vivo: cada punto sabe dónde está en el plano de la figura (para las texturas).
        float textura = Mathf.Clamp(texturaRelleno, 0, NombresTextura.Length - 1);
        float cambios = VelocidadesTextura[Mathf.Clamp(velocidadTextura, 0, VelocidadesTextura.Length - 1)];
        float semilla = (id % 97) * 0.731f;
        float porEscala = 1f / LimitarEscala(escalaTextura);
        float nv = NivelVisual;
        for (int i = 0; i < vertices.Count; i++)
        {
            colores.Add(c);
            uvs3.Add(estiloVivo.a);
            uvs4.Add(estiloVivo.b);
            Vector3 d = vertices[i] - poliCentro;
            uvsRelleno.Add(new Vector4(Vector3.Dot(d, poliU) * porEscala, Vector3.Dot(d, poliV) * porEscala, textura, cambios));
            uvsRelleno2.Add(new Vector2(semilla, nv));
        }

        mallaRelleno.Clear();
        mallaRelleno.SetVertices(vertices);
        mallaRelleno.SetColors(colores);
        mallaRelleno.SetUVs(0, uvsRelleno);
        mallaRelleno.SetUVs(1, uvsRelleno2);
        mallaRelleno.SetUVs(2, uvs3);
        mallaRelleno.SetUVs(3, uvs4);
        mallaRelleno.SetTriangles(indices, 0);
        mallaRelleno.RecalculateBounds();
    }

    // ---------- El borde del relleno, escondido debajo de la línea ----------
    // El borde se mete un poquito hacia adentro (menos de medio grosor de la línea): así el relleno nunca asoma
    // por fuera de la línea ni la "muerde", y la línea se ve completa de frente y de espalda.
    float HundimientoBorde(List<Vector2> p)
    {
        float area = Mathf.Abs(AreaDoble(p)) * 0.5f;
        return Mathf.Min(ancho * 0.3f, Mathf.Sqrt(area) * 0.08f);
    }

    static float AreaDoble(List<Vector2> p)
    {
        float a = 0f;
        for (int i = 0; i < p.Count; i++)
        {
            Vector2 u = p[i], v = p[(i + 1) % p.Count];
            a += u.x * v.y - v.x * u.y;
        }
        return a;
    }

    // Hacia adentro del polígono en el punto i, con el largo justo para que el borde se corra "dist".
    // signo: 1 si el polígono va contra el reloj, -1 si va como el reloj.
    static Vector2 HaciaAdentro(List<Vector2> p, int i, float signo, float dist)
    {
        int n = p.Count;
        Vector2 a = p[(i - 1 + n) % n], b = p[i], c = p[(i + 1) % n];
        Vector2 e1 = b - a, e2 = c - b;
        if (e1.sqrMagnitude < 1e-14f)
            e1 = e2;
        if (e2.sqrMagnitude < 1e-14f)
            e2 = e1;
        if (e1.sqrMagnitude < 1e-14f)
            return Vector2.zero;
        e1.Normalize();
        e2.Normalize();
        Vector2 n1 = new Vector2(-e1.y, e1.x) * signo, n2 = new Vector2(-e2.y, e2.x) * signo;
        Vector2 m = n1 + n2;
        if (m.sqrMagnitude < 1e-8f)
            m = n1;
        m.Normalize();
        float coseno = Mathf.Max(0.5f, Vector2.Dot(m, n1));
        return m * (dist / coseno);
    }

    void MeterBordeBajoLinea(List<Vector3> verts, List<Vector2> p, int n)
    {
        if (n < 3 || p.Count != n)
            return;
        float dist = HundimientoBorde(p);
        if (dist <= 0f)
            return;
        float signo = AreaDoble(p) >= 0f ? 1f : -1f;
        for (int i = 0; i < n && i < verts.Count; i++)
        {
            Vector2 d = HaciaAdentro(p, i, signo, dist);
            verts[i] += poliU * d.x + poliV * d.y;
        }
    }

    // ---------- Puntos de tela ----------
    readonly List<Vector2> telaBorde = new List<Vector2>();     // el borde (hundido), en el plano de la figura
    readonly List<Vector2> telaContorno = new List<Vector2>();  // el contorno con que se armó (para reusarla)
    readonly List<Vector2> telaPlano = new List<Vector2>();     // la malla fina en el plano (el borde primero)
    readonly List<float> telaAlto = new List<float>();          // cuánto se sale del plano (contornos en 3D)
    readonly List<int> telaTris = new List<int>();
    float telaHundido = -1f;
    bool telaMallaLista;
    static readonly List<Vector3> telaTemp = new List<Vector3>();
    static readonly List<float> altosBorde = new List<float>();
    static readonly List<Vector2> telaCentros = new List<Vector2>();
    static readonly List<float> telaRadios = new List<float>();

    bool MismoContorno()
    {
        if (telaContorno.Count != poli2D.Count)
            return false;
        for (int i = 0; i < poli2D.Count; i++)
            if ((telaContorno[i] - poli2D[i]).sqrMagnitude > 1e-14f)
                return false;
        return true;
    }

    // El borde hundido del relleno (se rehace solo si la figura cambió).
    List<Vector2> ContornoTela()
    {
        float hundido = HundimientoBorde(poli2D);
        if (MismoContorno() && Mathf.Abs(hundido - telaHundido) < 1e-7f && telaBorde.Count == poli2D.Count)
            return telaBorde;
        telaBorde.Clear();
        float signo = AreaDoble(poli2D) >= 0f ? 1f : -1f;
        for (int i = 0; i < poli2D.Count; i++)
            telaBorde.Add(poli2D[i] + HaciaAdentro(poli2D, i, signo, hundido));
        telaContorno.Clear();
        telaContorno.AddRange(poli2D);
        telaHundido = hundido;
        telaMallaLista = false;
        return telaBorde;
    }

    Vector2 EnPlano(Vector3 local)
    {
        Vector3 d = local - poliCentro;
        return new Vector2(Vector3.Dot(d, poliU), Vector3.Dot(d, poliV));
    }

    static float DistanciaAlContorno(Vector2 q, List<Vector2> borde)
    {
        float mejor = float.MaxValue;
        int n = borde.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = borde[i], b = borde[(i + 1) % n];
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 1e-14f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
            mejor = Mathf.Min(mejor, (q - (a + ab * t)).sqrMagnitude);
        }
        return Mathf.Sqrt(mejor);
    }

    // La campana: 1 en el centro, 0 en el radio (con la orilla suave, sin punta ni escalón).
    static float Campana(Vector2 q, Vector2 centro, float radio)
    {
        float x2 = (q - centro).sqrMagnitude / Mathf.Max(1e-12f, radio * radio);
        if (x2 >= 1f)
            return 0f;
        float t = 1f - x2;
        return t * t;
    }

    // Centro y radio de cada punto de tela: el radio llega justo hasta el borde más cercano (ahí ya no se mueve).
    void PrepararCampanas(List<Vector2> borde)
    {
        telaCentros.Clear();
        telaRadios.Clear();
        for (int i = 0; i < telaBase.Count; i++)
        {
            Vector2 q = EnPlano(telaBase[i]);
            telaCentros.Add(q);
            telaRadios.Add(Mathf.Max(1e-4f, DistanciaAlContorno(q, borde)));
        }
    }

    Vector3 Desplazamiento(Vector2 q, int sinEste)
    {
        Vector3 d = Vector3.zero;
        for (int i = 0; i < telaCentros.Count && i < telaMovida.Count; i++)
        {
            if (i == sinEste)
                continue;
            float w = Campana(q, telaCentros[i], telaRadios[i]);
            if (w > 0f)
                d += telaMovida[i] * w;
        }
        return d;
    }

    // La altura (fuera del plano) de un punto de adentro: un promedio de las del borde, más pesadas las cercanas.
    float AltoInterpolado(Vector2 q, List<Vector2> borde, List<float> altos)
    {
        float suma = 0f, pesos = 0f;
        for (int i = 0; i < borde.Count && i < altos.Count; i++)
        {
            float d2 = (borde[i] - q).sqrMagnitude;
            if (d2 < 1e-14f)
                return altos[i];
            float w = 1f / d2;
            suma += altos[i] * w;
            pesos += w;
        }
        return pesos > 0f ? suma / pesos : 0f;
    }

    void AltosDelBorde()
    {
        altosBorde.Clear();
        foreach (var p in poli3D)
            altosBorde.Add(Vector3.Dot(p - poliCentro, poliN));
    }

    // La malla fina (en el plano): el borde por orejas y una cuadrícula de puntos adentro.
    bool ArmarBaseTela(List<Vector2> borde)
    {
        telaPlano.Clear();
        telaAlto.Clear();
        telaTris.Clear();
        int n = borde.Count;
        if (n < 3 || poli3D.Count != n)
            return false;
        telaTemp.Clear();
        for (int i = 0; i < n; i++)
            telaTemp.Add(Vector3.zero);
        Triangular(borde, telaTris, telaTemp);
        if (telaTris.Count < 3)
        {
            telaTris.Clear();
            return false;
        }
        AltosDelBorde();
        telaPlano.AddRange(borde);
        telaAlto.AddRange(altosBorde);
        if (telaTemp.Count > n && restantes.Count > 0)
        {
            // Se trabó (la figura se cruza a sí misma): el centro del abanico también va en la malla fina.
            Vector2 centro = Vector2.zero;
            float alto = 0f;
            foreach (int r in restantes)
            {
                centro += borde[r];
                alto += r < altosBorde.Count ? altosBorde[r] : 0f;
            }
            telaPlano.Add(centro / restantes.Count);
            telaAlto.Add(alto / restantes.Count);
        }
        Vector2 min = borde[0], max = borde[0];
        foreach (var q in borde)
        {
            min = Vector2.Min(min, q);
            max = Vector2.Max(max, q);
        }
        float paso = Mathf.Max(max.x - min.x, max.y - min.y) / 16f;
        if (paso <= 1e-6f)
            return false;
        for (float y = min.y + paso * 0.5f; y < max.y; y += paso)
            for (float x = min.x + paso * 0.5f; x < max.x; x += paso)
            {
                var q = new Vector2(x, y);
                if (!PuntoEnPoligono(q, borde) || DistanciaAlContorno(q, borde) < paso * 0.45f)
                    continue;
                if (InsertarEnMalla(q))
                    telaAlto.Add(AltoInterpolado(q, borde, altosBorde));
            }
        telaMallaLista = true;
        return true;
    }

    // Mete un punto en el triángulo que lo contiene (ese triángulo se parte en 3).
    bool InsertarEnMalla(Vector2 q)
    {
        for (int t = 0; t + 2 < telaTris.Count; t += 3)
        {
            int a = telaTris[t], b = telaTris[t + 1], c = telaTris[t + 2];
            if (!PuntoEnTriangulo(q, telaPlano[a], telaPlano[b], telaPlano[c]))
                continue;
            int k = telaPlano.Count;
            telaPlano.Add(q);
            telaTris[t + 2] = k;
            telaTris.Add(b); telaTris.Add(c); telaTris.Add(k);
            telaTris.Add(c); telaTris.Add(a); telaTris.Add(k);
            return true;
        }
        return false;
    }

    // La malla del relleno con la tela movida. Devuelve false si no se pudo (y se usa la de siempre).
    bool ArmarMallaTela(List<Vector3> verts, List<int> tris)
    {
        var borde = ContornoTela();
        if (!telaMallaLista && !ArmarBaseTela(borde))
            return false;
        PrepararCampanas(borde);
        for (int k = 0; k < telaPlano.Count; k++)
        {
            Vector2 q = telaPlano[k];
            Vector3 p = poliCentro + poliU * q.x + poliV * q.y + poliN * (k < telaAlto.Count ? telaAlto[k] : 0f);
            verts.Add(p + Desplazamiento(q, -1));
        }
        tris.AddRange(telaTris);
        return true;
    }

    bool PuedeTela => PoligonoValido && relleno && !Dibujando;

    // Dónde se ve el punto de tela i (local del Dibujo).
    public Vector3 PosicionTela(int i)
    {
        if (i < 0 || i >= telaBase.Count || i >= telaMovida.Count)
            return Vector3.zero;
        if (!PuedeTela)
            return telaBase[i] + telaMovida[i];
        PrepararCampanas(ContornoTela());
        return telaBase[i] + Desplazamiento(telaCentros[i], -1);
    }

    // Un punto de tela nuevo donde toca el dedo (local). Devuelve su número, o -1 si no se pudo.
    public int AgregarTela(Vector3 local)
    {
        if (!PuedeTela)
            return -1;
        var borde = ContornoTela();
        Vector2 q = EnPlano(local);
        AltosDelBorde();
        Vector3 b = poliCentro + poliU * q.x + poliV * q.y + poliN * AltoInterpolado(q, poli2D, altosBorde);
        telaBase.Add(b);
        telaMovida.Add(Vector3.zero);
        int k = telaBase.Count - 1;
        // Si ya hay relieve de otros puntos en ese lugar, el nuevo empieza justo ahí (sin saltos): su propio
        // movimiento empieza en cero y se suma al de los otros.
        PrepararCampanas(borde);
        Reconstruir();
        return k;
    }

    // Lleva el punto de tela i a "objetivo" (local): la tela de alrededor lo sigue suave.
    public void MoverTela(int i, Vector3 objetivo)
    {
        if (i < 0 || i >= telaBase.Count || i >= telaMovida.Count || !PuedeTela)
            return;
        PrepararCampanas(ContornoTela());
        telaMovida[i] = objetivo - telaBase[i] - Desplazamiento(telaCentros[i], i);
        Reconstruir();
    }

    public void QuitarTela(int i)
    {
        if (i < 0 || i >= telaBase.Count)
            return;
        telaBase.RemoveAt(i);
        if (i < telaMovida.Count)
            telaMovida.RemoveAt(i);
        Reconstruir();
    }

    // Toma la curva cerrada, la reduce a pocos puntos y calcula su plano promedio.
    void PrepararPoligono()
    {
        poli3D.Clear();
        poli2D.Clear();
        bool abierta = !CerradoAhora;
        int total = abierta ? curva.Count : curva.Count - 1; // cerrada: el último punto repite el primero
        int maximo = Dibujando ? maxPuntosRellenoDibujando : maxPuntosRelleno;
        int paso = Mathf.Max(1, Mathf.CeilToInt(total / (float)maximo));
        for (int i = 0; i < total; i += paso)
            poli3D.Add(curva[i]);
        int m = poli3D.Count;
        if (m < 3)
            return;

        Vector3 c = Vector3.zero;
        foreach (var p in poli3D)
            c += p;
        c /= m;
        Vector3 suma = Vector3.zero;
        for (int i = 0; i < m; i++)
            suma += Vector3.Cross(poli3D[i] - c, poli3D[(i + 1) % m] - c);
        if (suma.sqrMagnitude < 1e-14f)
            return;
        if (abierta)
        {
            // Abierta: solo si encierra algo (una línea casi recta no se rellena).
            float perimetro = 0f;
            for (int i = 0; i < m; i++)
                perimetro += Vector3.Distance(poli3D[i], poli3D[(i + 1) % m]);
            float area = suma.magnitude * 0.5f;
            if (area < 0.01f * perimetro * perimetro)
                return;
        }

        poliCentro = c;
        poliN = suma.normalized;
        poliU = Perpendicular(poliN);
        poliV = Vector3.Cross(poliN, poliU);
        foreach (var p in poli3D)
        {
            Vector3 d = p - c;
            poli2D.Add(new Vector2(Vector3.Dot(d, poliU), Vector3.Dot(d, poliV)));
        }
        PoligonoValido = true;
    }

    // Triangulación por "orejas". Los puntos sobre un lado recto (o repetidos) se saltan sin triángulo: ahí no hay
    // oreja que cortar y antes trababan todo (figuras hechas con rectas). Si aun así se traba (la figura se cruza a
    // sí misma), termina en abanico desde el centro.
    static void Triangular(List<Vector2> p, List<int> tris, List<Vector3> verts3D)
    {
        int m = p.Count;
        float area = 0f;
        for (int i = 0; i < m; i++)
        {
            Vector2 a = p[i];
            Vector2 b = p[(i + 1) % m];
            area += a.x * b.y - b.x * a.y;
        }
        restantes.Clear();
        if (area >= 0f)
            for (int i = 0; i < m; i++) restantes.Add(i);
        else
            for (int i = m - 1; i >= 0; i--) restantes.Add(i);

        int vueltas = 0;
        while (restantes.Count > 3 && vueltas < m * 2)
        {
            vueltas++;
            bool corte = false;
            int cuenta = restantes.Count;
            for (int k = 0; k < cuenta; k++)
            {
                int i0 = restantes[(k - 1 + cuenta) % cuenta];
                int i1 = restantes[k];
                int i2 = restantes[(k + 1) % cuenta];
                Vector2 a = p[i0], b = p[i1], c = p[i2];
                Vector2 ab = b - a, bc = c - b;
                float cruz = Cruz(ab, bc);
                if (Mathf.Abs(cruz) <= 1e-4f * Mathf.Sqrt(ab.sqrMagnitude * bc.sqrMagnitude))
                {
                    restantes.RemoveAt(k);
                    corte = true;
                    break;
                }
                if (cruz <= 0f)
                    continue;
                bool contiene = false;
                for (int j = 0; j < cuenta; j++)
                {
                    int o = restantes[j];
                    if (o == i0 || o == i1 || o == i2)
                        continue;
                    if (PuntoEnTriangulo(p[o], a, b, c))
                    {
                        contiene = true;
                        break;
                    }
                }
                if (contiene)
                    continue;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                restantes.RemoveAt(k);
                corte = true;
                break;
            }
            if (!corte)
                break;
        }

        if (restantes.Count == 3)
        {
            tris.Add(restantes[0]); tris.Add(restantes[1]); tris.Add(restantes[2]);
        }
        else if (restantes.Count > 3)
        {
            Vector3 centro = Vector3.zero;
            foreach (int r in restantes)
                centro += verts3D[r];
            centro /= restantes.Count;
            int ic = verts3D.Count;
            verts3D.Add(centro);
            for (int k = 0; k < restantes.Count; k++)
            {
                tris.Add(ic);
                tris.Add(restantes[k]);
                tris.Add(restantes[(k + 1) % restantes.Count]);
            }
        }
    }

    static float Cruz(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    static bool PuntoEnTriangulo(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cruz(b - a, p - a);
        float d2 = Cruz(c - b, p - b);
        float d3 = Cruz(a - c, p - c);
        return d1 >= 0f && d2 >= 0f && d3 >= 0f;
    }

    static bool PuntoEnPoligono(Vector2 q, List<Vector2> poli)
    {
        bool dentro = false;
        int n = poli.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = poli[i];
            Vector2 b = poli[j];
            if ((a.y > q.y) != (b.y > q.y))
            {
                float x = (b.x - a.x) * (q.y - a.y) / (b.y - a.y) + a.x;
                if (q.x < x)
                    dentro = !dentro;
            }
        }
        return dentro;
    }

    // ---------- Curvas ----------

    void MuestrearBezier(List<Vector3> salida)
    {
        int n = nodos.Count;
        if (n == 0)
            return;
        curvaSegmento.Clear();
        curvaT.Clear();
        if (n == 1)
        {
            salida.Add(nodos[0]);
            multiplicadores.Add(1f);
            curvaSegmento.Add(-1);
            curvaT.Add(0f);
            return;
        }
        float paso = pasoMuestras / Escala;
        int segmentos = cerrado ? n : n - 1;
        for (int s = 0; s < segmentos; s++)
        {
            int a = s;
            int b = (s + 1) % n;
            Vector3 p0 = nodos[a];
            Vector3 p1 = p0 + asaSalida[a];
            Vector3 p3 = nodos[b];
            Vector3 p2 = p3 + asaEntrada[b];
            float largo = Vector3.Distance(p0, p1) + Vector3.Distance(p1, p2) + Vector3.Distance(p2, p3);
            int pasos = Mathf.Clamp(Mathf.CeilToInt(largo / paso), 1, 48);
            for (int k = 0; k < pasos; k++)
            {
                salida.Add(Bezier(p0, p1, p2, p3, k / (float)pasos));
                multiplicadores.Add(Mathf.Lerp(grosorNodo[a], grosorNodo[b], k / (float)pasos));
                curvaSegmento.Add(s);
                curvaT.Add(k / (float)pasos);
            }
        }
        salida.Add(cerrado ? nodos[0] : nodos[n - 1]);
        multiplicadores.Add(cerrado ? grosorNodo[0] : grosorNodo[n - 1]);
        curvaSegmento.Add(segmentos - 1);
        curvaT.Add(1f);
    }

    static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }

    static Vector3 Perpendicular(Vector3 t)
    {
        Vector3 eje = Mathf.Abs(t.y) < 0.9f ? Vector3.up : Vector3.right;
        return Vector3.Cross(t, eje).normalized;
    }

    static float LargoDe(List<Vector3> puntos)
    {
        float total = 0f;
        for (int i = 1; i < puntos.Count; i++)
            total += Vector3.Distance(puntos[i - 1], puntos[i]);
        return total;
    }

    // Ramer-Douglas-Peucker: deja solo los puntos necesarios para conservar la forma.
    static void Simplificar(List<Vector3> pts, float tolerancia, List<Vector3> salida)
    {
        int n = pts.Count;
        var conservar = new bool[n];
        conservar[0] = true;
        conservar[n - 1] = true;
        var pila = new Stack<Vector2Int>();
        pila.Push(new Vector2Int(0, n - 1));
        while (pila.Count > 0)
        {
            var tramo = pila.Pop();
            int a = tramo.x;
            int b = tramo.y;
            if (b <= a + 1)
                continue;
            float mayor = -1f;
            int indice = -1;
            for (int i = a + 1; i < b; i++)
            {
                float d = DistanciaASegmento(pts[i], pts[a], pts[b]);
                if (d > mayor)
                {
                    mayor = d;
                    indice = i;
                }
            }
            if (indice >= 0 && mayor > tolerancia)
            {
                conservar[indice] = true;
                pila.Push(new Vector2Int(a, indice));
                pila.Push(new Vector2Int(indice, b));
            }
        }
        for (int i = 0; i < n; i++)
            if (conservar[i])
                salida.Add(pts[i]);
    }

    static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float l2 = ab.sqrMagnitude;
        if (l2 < 1e-12f)
            return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
        return Vector3.Distance(p, a + ab * t);
    }
}

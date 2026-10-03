using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NodoFigura
{
    public Vector3 punto;          // dónde está en la figura sin deformar (unidades de la figura)
    public Vector3 desplazamiento; // cuánto se movió
}

[System.Serializable]
public class DatosFigura
{
    public int id;
    public int tipo;               // 0 esfera, 1 cubo, 2 cilindro
    public int capa;
    public Vector3 posicion;       // respecto al dibujo
    public Quaternion rotacion = Quaternion.identity;
    public float escala = 0.2f;
    public float suavizado;        // 0 = esquinas duras, 1 = muy redondeado
    public float ancho = 0.008f;   // grosor de la línea (unidades del dibujo)
    public int color = 6;
    public List<NodoFigura> nodos = new List<NodoFigura>();
}

// Figuras 3D que se ven como DIBUJO: relleno plano y la línea de contorno (la silueta y los bordes que se ven),
// calculada desde donde las mires. La línea usa el estilo de su capa (normal, hebras, temblor, boceto).
//  - Menú de la mano: Esfera, Cubo, Cilindro, "A líneas" (la convierte en líneas normales) y "Quitar figura".
//  - Pellizca una figura con la derecha para elegirla y moverla; con las dos manos: tamaño, giro y posición.
//  - Gesto de grosor (pulgar + anular) con una figura elegida: subir/bajar la izquierda = suavizar las esquinas.
//  - Modo nodos (pulgar + medio): arrastra los puntos para deformarla como plastilina;
//    pellizca cerca de la superficie (lejos de los puntos) = agregar un punto nuevo.
public class Figuras : MonoBehaviour
{
    public static readonly string[] Nombres = { "Esfera", "Cubo", "Cilindro" };

    public Dibujo dibujo;
    public Material materialLinea;
    public Material materialSeleccion;
    public Material materialRelleno;
    public Material materialNodo;
    public Material materialNodoActivo;
    [Tooltip("Ángulo (grados) a partir del cual un borde se dibuja como línea")]
    public float anguloBorde = 35f;

    class Figura
    {
        public DatosFigura d;
        public Transform raiz;
        public Mesh relleno;
        public Mesh linea;
        public MeshRenderer rLinea;
        public MeshRenderer rRelleno;
        public Vector3[] baseV;      // sin deformar
        public Vector3[] defV;       // deformada
        public int[] tris;
        public Vector3[] normalCara;
        public Vector3[] centroCara;
        public List<Vector4> aristas = new List<Vector4>(); // (a, b, cara1, cara2)
        public bool sucia = true;
        public float suavizadoArmado = -1f;
        public readonly List<List<Vector3>> cadenas = new List<List<Vector3>>();
        public readonly List<bool> cerradas = new List<bool>();
    }

    readonly List<Figura> figuras = new List<Figura>();
    Figura seleccionada;
    int siguienteId = 1;
    Vector3? vistaFija;

    // Nodos visibles y arrastre
    readonly List<Transform> marcas = new List<Transform>();
    int arrastre = -1;
    int hover = -1;
    Vector3 desfase;

    static readonly List<Vector3> v3 = new List<Vector3>();
    static readonly List<Vector3> nor = new List<Vector3>();
    static readonly List<Vector2> uv = new List<Vector2>();
    static readonly List<Vector2> uv2 = new List<Vector2>();
    static readonly List<Vector4> uv3 = new List<Vector4>();
    static readonly List<Vector4> uv4 = new List<Vector4>();
    static readonly List<int> idx = new List<int>();
    static readonly List<Color> col = new List<Color>();

    public Transform Seleccionada => seleccionada != null && seleccionada.raiz != null ? seleccionada.raiz : null;

    void Awake()
    {
        if (dibujo == null) dibujo = GetComponent<Dibujo>();
    }

    void Mensaje(string texto)
    {
        if (dibujo != null)
            dibujo.Mensaje(texto);
    }

    Figura Buscar(Transform raiz)
    {
        if (raiz == null)
            return null;
        foreach (var f in figuras)
            if (f.raiz == raiz)
                return f;
        return null;
    }

    // ---------- Crear, quitar, guardar ----------

    public void Agregar(int tipo)
    {
        if (dibujo == null)
            return;
        var control = ControlManos.Instancia;
        Transform cabeza = control != null ? control.Cabeza : null;
        Vector3 ojos = cabeza != null ? cabeza.position : new Vector3(0f, 1.5f, 0f);
        Vector3 frente = cabeza != null ? cabeza.forward : Vector3.forward;
        frente.y = 0f;
        if (frente.sqrMagnitude < 1e-4f)
            frente = Vector3.forward;
        frente.Normalize();
        Vector3 mundo = ojos + frente * 0.5f - Vector3.up * 0.1f;

        dibujo.GuardarParaDeshacer();
        var t = dibujo.transform;
        var d = new DatosFigura
        {
            id = siguienteId++,
            tipo = Mathf.Clamp(tipo, 0, 2),
            capa = dibujo.capaActual,
            posicion = dibujo.ProyectarEnPlano(t.InverseTransformPoint(mundo)),
            rotacion = Quaternion.Inverse(t.rotation) * Quaternion.LookRotation(frente, Vector3.up),
            escala = 0.18f / dibujo.EscalaMundo,
            suavizado = 0f,
            ancho = dibujo.AnchoNuevoLocal(),
            color = 6,
        };
        NodosIniciales(d);
        var f = Crear(d);
        Seleccionar(f.raiz);
        Mensaje(Nombres[d.tipo] + " lista. Pellízcala para moverla; con las dos manos cambia su tamaño");
    }

    static void NodosIniciales(DatosFigura d)
    {
        d.nodos.Clear();
        var puntos = new List<Vector3>();
        if (d.tipo == 0)
        {
            puntos.AddRange(new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back });
            for (int i = 0; i < puntos.Count; i++)
                puntos[i] *= 0.5f;
        }
        else if (d.tipo == 1)
        {
            for (int i = 0; i < 8; i++)
                puntos.Add(new Vector3((i & 1) != 0 ? 0.5f : -0.5f, (i & 2) != 0 ? 0.5f : -0.5f, (i & 4) != 0 ? 0.5f : -0.5f));
        }
        else
        {
            puntos.Add(new Vector3(0f, 0.5f, 0f));
            puntos.Add(new Vector3(0f, -0.5f, 0f));
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI * 0.5f;
                puntos.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f));
            }
        }
        foreach (var p in puntos)
            d.nodos.Add(new NodoFigura { punto = p });
    }

    Figura Crear(DatosFigura d)
    {
        var f = new Figura { d = d };
        var go = new GameObject("Figura_" + Nombres[Mathf.Clamp(d.tipo, 0, 2)]);
        go.transform.SetParent(dibujo.transform, false);
        f.raiz = go.transform;

        var goR = new GameObject("Relleno");
        goR.transform.SetParent(f.raiz, false);
        f.relleno = new Mesh { name = "FiguraRelleno" };
        f.relleno.MarkDynamic();
        goR.AddComponent<MeshFilter>().sharedMesh = f.relleno;
        f.rRelleno = goR.AddComponent<MeshRenderer>();
        f.rRelleno.sharedMaterial = materialRelleno;
        Preparar(f.rRelleno);

        var goL = new GameObject("Contorno");
        goL.transform.SetParent(f.raiz, false);
        f.linea = new Mesh { name = "FiguraContorno" };
        f.linea.MarkDynamic();
        goL.AddComponent<MeshFilter>().sharedMesh = f.linea;
        f.rLinea = goL.AddComponent<MeshRenderer>();
        Preparar(f.rLinea);

        figuras.Add(f);
        siguienteId = Mathf.Max(siguienteId, d.id + 1);
        Colocar(f);
        AplicarCapa(f);
        return f;
    }

    static void Preparar(MeshRenderer r)
    {
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    void Colocar(Figura f)
    {
        var q = f.d.rotacion;
        f.raiz.localPosition = f.d.posicion;
        f.raiz.localRotation = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 0.5f ? q : Quaternion.identity;
        f.raiz.localScale = Vector3.one * Mathf.Max(0.0001f, f.d.escala);
    }

    // Capa de Unity (boceto = no sale en fotos/videos), color de la línea y visibilidad de la capa.
    void AplicarCapa(Figura f)
    {
        int capaUnity = dibujo.EsBoceto(f.d.capa) ? 0 : dibujo.gameObject.layer;
        foreach (var t in f.raiz.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = capaUnity;
        f.rLinea.sharedMaterial = f == seleccionada && materialSeleccion != null ? materialSeleccion : dibujo.MaterialCapa(f.d.capa, materialLinea);
        f.sucia = true;
    }

    void Destruir(Figura f)
    {
        if (f.raiz != null)
            Destroy(f.raiz.gameObject);
        if (f.relleno != null)
            Destroy(f.relleno);
        if (f.linea != null)
            Destroy(f.linea);
    }

    public void QuitarSeleccionada()
    {
        if (seleccionada == null)
        {
            Mensaje("Pellizca una figura para elegirla");
            return;
        }
        dibujo.GuardarParaDeshacer();
        var f = seleccionada;
        seleccionada = null;
        figuras.Remove(f);
        Destruir(f);
        OcultarNodos();
        Mensaje("Figura quitada");
    }

    public void QuitarTodas()
    {
        foreach (var f in figuras)
            Destruir(f);
        figuras.Clear();
        seleccionada = null;
        OcultarNodos();
    }

    public void GuardarEn(DatosDibujo d)
    {
        foreach (var f in figuras)
        {
            if (f.raiz == null)
                continue;
            f.d.posicion = f.raiz.localPosition;
            f.d.rotacion = f.raiz.localRotation;
            f.d.escala = f.raiz.localScale.x;
            d.figuras.Add(Copiar(f.d));
        }
    }

    static DatosFigura Copiar(DatosFigura a)
    {
        var b = (DatosFigura)JsonUtility.FromJson(JsonUtility.ToJson(a), typeof(DatosFigura));
        return b;
    }

    public void Restaurar(DatosDibujo d)
    {
        QuitarTodas();
        if (d.figuras == null)
            return;
        foreach (var fd in d.figuras)
        {
            if (fd == null)
                continue;
            if (fd.nodos == null)
                fd.nodos = new List<NodoFigura>();
            Crear(Copiar(fd));
        }
    }

    public void RefrescarCapa(int capa)
    {
        foreach (var f in figuras)
            if (f.d.capa == capa)
                AplicarCapa(f);
    }

    // ---------- Elegir, mover, transformar ----------

    public Transform BuscarBajo(Vector3 mundo)
    {
        Figura mejor = null;
        float mejorDist = float.MaxValue;
        foreach (var f in figuras)
        {
            if (f.raiz == null || !f.raiz.gameObject.activeInHierarchy || f.defV == null)
                continue;
            Vector3 l = f.raiz.InverseTransformPoint(mundo);
            float margen = 0.03f / Mathf.Max(1e-5f, f.raiz.lossyScale.x);
            var caja = new Bounds(f.defV[0], Vector3.zero);
            foreach (var v in f.defV)
                caja.Encapsulate(v);
            caja.Expand(margen * 2f);
            if (!caja.Contains(l))
                continue;
            float dist = Vector3.Distance(l, caja.center);
            if (dist < mejorDist)
            {
                mejorDist = dist;
                mejor = f;
            }
        }
        return mejor != null ? mejor.raiz : null;
    }

    public void Seleccionar(Transform raiz)
    {
        var nueva = Buscar(raiz);
        if (nueva == seleccionada)
            return;
        var anterior = seleccionada;
        seleccionada = nueva;
        arrastre = -1;
        if (anterior != null)
            AplicarCapa(anterior);
        if (seleccionada != null)
            AplicarCapa(seleccionada);
        if (seleccionada == null)
            OcultarNodos();
    }

    public float SuavizadoSeleccionada => seleccionada != null ? seleccionada.d.suavizado : 0f;

    public void PonerSuavizado(float valor)
    {
        if (seleccionada == null)
            return;
        valor = Mathf.Clamp01(valor);
        if (Mathf.Abs(valor - seleccionada.d.suavizado) < 0.01f)
            return;
        seleccionada.d.suavizado = valor;
        seleccionada.sucia = true;
    }

    // ---------- Convertir a líneas ----------

    public void ConvertirSeleccionada()
    {
        var f = seleccionada;
        if (f == null)
        {
            Mensaje("Pellizca una figura para elegirla");
            return;
        }
        Silueta(f, VistaActual());
        if (f.cadenas.Count == 0)
            return;
        dibujo.GuardarParaDeshacer();
        int capaAntes = dibujo.capaActual;
        dibujo.capaActual = f.d.capa;
        // La cadena cerrada más larga es la silueta: lleva el relleno.
        int masLarga = -1;
        float largoMax = 0f;
        for (int i = 0; i < f.cadenas.Count; i++)
        {
            if (!f.cerradas[i])
                continue;
            float l = Largo(f.cadenas[i]);
            if (l > largoMax)
            {
                largoMax = l;
                masLarga = i;
            }
        }
        var raizDibujo = dibujo.transform;
        for (int i = 0; i < f.cadenas.Count; i++)
        {
            var c = f.cadenas[i];
            bool cerrada = f.cerradas[i];
            var d = new DatosTrazo { ancho = f.d.ancho, cerrado = cerrada && c.Count >= 3, relleno = i == masLarga, colorRelleno = f.d.color };
            int paso = Mathf.Max(1, c.Count / 24);
            for (int k = 0; k < c.Count; k += paso)
                d.nodos.Add(raizDibujo.InverseTransformPoint(f.raiz.TransformPoint(c[k])));
            if (!cerrada && (c.Count - 1) % paso != 0)
                d.nodos.Add(raizDibujo.InverseTransformPoint(f.raiz.TransformPoint(c[c.Count - 1])));
            if (d.nodos.Count >= 2)
                dibujo.AgregarTrazo(d);
        }
        dibujo.capaActual = capaAntes;
        seleccionada = null;
        figuras.Remove(f);
        Destruir(f);
        OcultarNodos();
        Mensaje("Figura convertida en líneas");
    }

    static float Largo(List<Vector3> c)
    {
        float l = 0f;
        for (int i = 1; i < c.Count; i++)
            l += Vector3.Distance(c[i - 1], c[i]);
        return l;
    }

    // ---------- Nodos (deformar como plastilina) ----------

    Vector3 Deformar(Figura f, Vector3 p)
    {
        Vector3 r = p;
        const float sigma2 = 0.35f * 0.35f;
        foreach (var n in f.d.nodos)
        {
            float d2 = (p - n.punto).sqrMagnitude;
            r += n.desplazamiento * Mathf.Exp(-d2 / sigma2);
        }
        return r;
    }

    // Lo llama ControlManos en modo nodos cuando hay una figura elegida.
    public void EditarNodos(ManoSeguida der)
    {
        var f = seleccionada;
        if (f == null || f.raiz == null)
        {
            OcultarNodos();
            return;
        }
        if (!der.valida)
        {
            arrastre = -1;
            MostrarNodos(f);
            return;
        }
        Vector3 pinza = der.PuntoPellizco;
        if (arrastre >= 0)
        {
            if (!der.pellizco || arrastre >= f.d.nodos.Count)
            {
                arrastre = -1;
            }
            else
            {
                var n = f.d.nodos[arrastre];
                Vector3 objetivo = f.raiz.InverseTransformPoint(pinza + desfase);
                n.desplazamiento += objetivo - Deformar(f, n.punto);
                f.sucia = true;
            }
            MostrarNodos(f);
            return;
        }

        // ¿Qué nodo está cerca?
        hover = -1;
        float mejor = 0.025f;
        for (int i = 0; i < f.d.nodos.Count; i++)
        {
            Vector3 m = f.raiz.TransformPoint(Deformar(f, f.d.nodos[i].punto));
            float dist = Mathf.Min(Vector3.Distance(m, pinza), Vector3.Distance(m, der.indice));
            if (dist < mejor)
            {
                mejor = dist;
                hover = i;
            }
        }
        if (der.empezoPellizco)
        {
            if (hover >= 0)
            {
                dibujo.GuardarParaDeshacer();
                arrastre = hover;
                desfase = f.raiz.TransformPoint(Deformar(f, f.d.nodos[hover].punto)) - pinza;
            }
            else if (f.defV != null)
            {
                // Cerca de la superficie: un nodo nuevo en el punto más cercano.
                int cercano = -1;
                float dMin = 0.03f;
                for (int i = 0; i < f.defV.Length; i++)
                {
                    float dist = Vector3.Distance(f.raiz.TransformPoint(f.defV[i]), pinza);
                    if (dist < dMin)
                    {
                        dMin = dist;
                        cercano = i;
                    }
                }
                if (cercano >= 0)
                {
                    dibujo.GuardarParaDeshacer();
                    f.d.nodos.Add(new NodoFigura { punto = f.baseV[cercano] });
                    arrastre = f.d.nodos.Count - 1;
                    desfase = f.raiz.TransformPoint(f.defV[cercano]) - pinza;
                    Mensaje("Nodo agregado a la figura");
                }
            }
        }
        MostrarNodos(f);
    }

    void MostrarNodos(Figura f)
    {
        var cabeza = ControlManos.Instancia != null ? ControlManos.Instancia.Cabeza : null;
        int n = 0;
        for (int i = 0; i < f.d.nodos.Count; i++)
        {
            if (n >= marcas.Count)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "NodoFigura";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(transform, false);
                var r = go.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.layer = 0;
                marcas.Add(go.transform);
            }
            var m = marcas[n++];
            if (!m.gameObject.activeSelf)
                m.gameObject.SetActive(true);
            m.position = f.raiz.TransformPoint(Deformar(f, f.d.nodos[i].punto));
            bool activo = i == arrastre || i == hover;
            m.localScale = Vector3.one * (activo ? 0.016f : 0.011f);
            var mat = activo ? materialNodoActivo : materialNodo;
            var rr = m.GetComponent<Renderer>();
            if (mat != null && rr.sharedMaterial != mat)
                rr.sharedMaterial = mat;
        }
        for (int i = n; i < marcas.Count; i++)
            if (marcas[i].gameObject.activeSelf)
                marcas[i].gameObject.SetActive(false);
        if (cabeza == null)
            return;
    }

    public void OcultarNodos()
    {
        arrastre = -1;
        hover = -1;
        foreach (var m in marcas)
            if (m != null && m.gameObject.activeSelf)
                m.gameObject.SetActive(false);
    }

    // ---------- Cada cuadro: forma y contorno ----------

    // Para fotos y videos: el contorno se calcula desde la cámara de la captura.
    public void PonerVista(Vector3? mundo)
    {
        vistaFija = mundo;
        ActualizarTodas();
    }

    Vector3 VistaActual()
    {
        if (vistaFija.HasValue)
            return vistaFija.Value;
        var control = ControlManos.Instancia;
        if (control != null && control.Cabeza != null)
            return control.Cabeza.position;
        return Vector3.zero;
    }

    void LateUpdate()
    {
        ActualizarTodas();
        ActualizarEquis();
    }

    // ---------- Borrar figuras (borrador y X) ----------

    public bool HayFiguras => figuras.Count > 0;

    // El nodo de figura más cercano a un punto (a menos de "radio" metros). Devuelve false si no hay.
    public bool NodoCerca(Vector3 mundo, float radio, out Vector3 lugar)
    {
        Figura f;
        int i;
        return BuscarNodo(mundo, radio, out f, out i, out lugar);
    }

    bool BuscarNodo(Vector3 mundo, float radio, out Figura mejor, out int indice, out Vector3 lugar)
    {
        mejor = null;
        indice = -1;
        lugar = Vector3.zero;
        float mejorDist = radio;
        foreach (var f in figuras)
        {
            if (f.raiz == null || !f.raiz.gameObject.activeInHierarchy)
                continue;
            for (int i = 0; i < f.d.nodos.Count; i++)
            {
                Vector3 m = f.raiz.TransformPoint(Deformar(f, f.d.nodos[i].punto));
                float d = Vector3.Distance(m, mundo);
                if (d < mejorDist)
                {
                    mejorDist = d;
                    mejor = f;
                    indice = i;
                    lugar = m;
                }
            }
        }
        return mejor != null;
    }

    // Borrador sobre un nodo de figura: si estaba deformado, vuelve a su lugar; si no, el nodo se quita.
    public bool BorrarNodoCerca(Vector3 mundo, float radio)
    {
        Figura f;
        int i;
        Vector3 lugar;
        if (!BuscarNodo(mundo, radio, out f, out i, out lugar))
            return false;
        dibujo.GuardarParaDeshacer();
        var n = f.d.nodos[i];
        if (n.desplazamiento.sqrMagnitude > 1e-10f)
            n.desplazamiento = Vector3.zero;
        else if (f.d.nodos.Count > 1)
            f.d.nodos.RemoveAt(i);
        f.sucia = true;
        arrastre = -1;
        return true;
    }

    // Quita una figura entera (con el borrador frotando, o con su X). Se puede deshacer.
    public void Quitar(Transform raiz)
    {
        var f = Buscar(raiz);
        if (f == null)
            return;
        dibujo.GuardarParaDeshacer();
        if (seleccionada == f)
            seleccionada = null;
        figuras.Remove(f);
        Destruir(f);
        OcultarNodos();
        Mensaje("Figura borrada (deshacer la devuelve)");
    }

    // Mientras la frotas con el borrador, su contorno se pone rojo.
    public void MarcarBorrando(Transform raiz, bool borrando)
    {
        var f = Buscar(raiz);
        if (f == null || f.rLinea == null)
            return;
        if (borrando && dibujo.materialBorrado != null)
            f.rLinea.sharedMaterial = dibujo.materialBorrado;
        else
            AplicarCapa(f);
    }

    // La X arriba a la derecha de la figura elegida: tocarla con el índice derecho = borrarla.
    Transform equis;
    float equisBloqueo;

    void ActualizarEquis()
    {
        var control = ControlManos.Instancia;
        var f = seleccionada;
        bool ver = f != null && f.raiz != null && f.raiz.gameObject.activeInHierarchy && f.rRelleno != null
                   && control != null && control.Cabeza != null && !Titere.Activo;
        if (!ver)
        {
            if (equis != null && equis.gameObject.activeSelf)
                equis.gameObject.SetActive(false);
            return;
        }
        if (equis == null)
        {
            equis = new GameObject("XFigura").transform;
            for (int k = 0; k < 2; k++)
            {
                var palo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(palo.GetComponent<Collider>());
                palo.transform.SetParent(equis, false);
                palo.transform.localRotation = Quaternion.Euler(0f, 0f, k == 0 ? 45f : -45f);
                palo.transform.localScale = new Vector3(0.022f, 0.004f, 0.002f);
                var r = palo.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (materialNodoActivo != null)
                    r.sharedMaterial = materialNodoActivo;
            }
        }
        if (!equis.gameObject.activeSelf)
            equis.gameObject.SetActive(true);
        var caja = f.rRelleno.bounds;
        Vector3 derecha = control.Cabeza.right;
        derecha.y = 0f;
        derecha = derecha.sqrMagnitude > 1e-6f ? derecha.normalized : Vector3.right;
        float lado = Mathf.Abs(Vector3.Dot(caja.extents, new Vector3(Mathf.Abs(derecha.x), 0f, Mathf.Abs(derecha.z))));
        Vector3 pos = caja.center + derecha * (lado + 0.02f) + Vector3.up * (caja.extents.y + 0.02f);
        equis.position = pos;
        equis.rotation = Quaternion.LookRotation(pos - control.Cabeza.position, Vector3.up);
        if (control.Der.valida && Time.time > equisBloqueo && Vector3.Distance(control.Der.indice, pos) < 0.018f)
        {
            equisBloqueo = Time.time + 1f;
            Quitar(f.raiz);
        }
    }

    void ActualizarTodas()
    {
        if (dibujo == null)
            return;
        Vector3 vista = VistaActual();
        foreach (var f in figuras)
        {
            if (f.raiz == null)
                continue;
            bool ver = dibujo.CapaVisible(f.d.capa);
            if (f.raiz.gameObject.activeSelf != ver)
                f.raiz.gameObject.SetActive(ver);
            if (!ver)
                continue;
            if (f.sucia || f.baseV == null || !Mathf.Approximately(f.suavizadoArmado, f.d.suavizado))
                Armar(f);
            Silueta(f, vista);
            ConstruirLinea(f);
        }
    }

    // ---------- La malla de cada figura ----------

    class Constructor
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<int> t = new List<int>();
        readonly Dictionary<Vector3Int, int> mapa = new Dictionary<Vector3Int, int>();

        public int Indice(Vector3 p)
        {
            var k = new Vector3Int(Mathf.RoundToInt(p.x * 20000f), Mathf.RoundToInt(p.y * 20000f), Mathf.RoundToInt(p.z * 20000f));
            int i;
            if (!mapa.TryGetValue(k, out i))
            {
                i = v.Count;
                v.Add(p);
                mapa[k] = i;
            }
            return i;
        }

        public void Tri(int a, int b, int c)
        {
            if (a == b || b == c || a == c)
                return;
            t.Add(a); t.Add(b); t.Add(c);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int ia = Indice(a), ib = Indice(b), ic = Indice(c), id = Indice(d);
            Tri(ia, ib, ic);
            Tri(ia, ic, id);
        }
    }

    delegate Vector3 Superficie(float u, float v);

    static void Rejilla(Constructor c, int nu, int nv, Superficie s)
    {
        for (int i = 0; i < nu; i++)
            for (int j = 0; j < nv; j++)
            {
                float u0 = i / (float)nu, u1 = (i + 1) / (float)nu;
                float v0 = j / (float)nv, v1 = (j + 1) / (float)nv;
                c.Quad(s(u0, v0), s(u1, v0), s(u1, v1), s(u0, v1));
            }
    }

    // Cubo con esquinas redondeadas (r = 0: esquinas duras).
    static Vector3 Redondear(Vector3 p, float r)
    {
        float m = 0.5f - r;
        var q = new Vector3(Mathf.Clamp(p.x, -m, m), Mathf.Clamp(p.y, -m, m), Mathf.Clamp(p.z, -m, m));
        Vector3 d = p - q;
        return d.sqrMagnitude > 1e-12f ? q + d.normalized * r : q;
    }

    static Vector3 Cilindro(float rho, float y, float ang, float r)
    {
        float m = 0.5f - r;
        float rc = Mathf.Min(rho, m);
        float yc = Mathf.Clamp(y, -m, m);
        var d = new Vector2(rho - rc, y - yc);
        if (d.sqrMagnitude > 1e-12f)
        {
            d = d.normalized * r;
            rho = rc + d.x;
            y = yc + d.y;
        }
        return new Vector3(Mathf.Cos(ang) * rho, y, Mathf.Sin(ang) * rho);
    }

    void Armar(Figura f)
    {
        var c = new Constructor();
        float r = Mathf.Clamp01(f.d.suavizado) * 0.45f;
        const float dosPi = Mathf.PI * 2f;
        if (f.d.tipo == 0)
        {
            Rejilla(c, 28, 16, (u, v) =>
            {
                float th = u * dosPi, ph = v * Mathf.PI;
                return new Vector3(Mathf.Sin(ph) * Mathf.Cos(th), Mathf.Cos(ph), Mathf.Sin(ph) * Mathf.Sin(th)) * 0.5f;
            });
        }
        else if (f.d.tipo == 1)
        {
            Vector3[] normales = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var n in normales)
            {
                Vector3 a = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 ejeU = Vector3.Cross(n, a).normalized;
                Vector3 ejeV = Vector3.Cross(n, ejeU).normalized;
                Vector3 nn = n;
                Rejilla(c, 8, 8, (u, v) => Redondear(nn * 0.5f + ejeU * (u - 0.5f) + ejeV * (v - 0.5f), r));
            }
        }
        else
        {
            Rejilla(c, 28, 8, (u, v) => Cilindro(0.5f, v - 0.5f, u * dosPi, r));
            Rejilla(c, 28, 5, (u, v) => Cilindro(v * 0.5f, 0.5f, u * dosPi, r));
            Rejilla(c, 28, 5, (u, v) => Cilindro(v * 0.5f, -0.5f, u * dosPi, r));
        }
        f.baseV = c.v.ToArray();
        f.tris = c.t.ToArray();
        // Todas las caras mirando hacia afuera (la figura base es convexa y está centrada).
        for (int i = 0; i < f.tris.Length; i += 3)
        {
            Vector3 a = f.baseV[f.tris[i]], b = f.baseV[f.tris[i + 1]], d = f.baseV[f.tris[i + 2]];
            Vector3 n = Vector3.Cross(b - a, d - a);
            if (Vector3.Dot(n, (a + b + d) / 3f) < 0f)
            {
                int tmp = f.tris[i + 1];
                f.tris[i + 1] = f.tris[i + 2];
                f.tris[i + 2] = tmp;
            }
        }
        // Aristas con sus dos caras.
        var mapa = new Dictionary<long, int>();
        f.aristas.Clear();
        int caras = f.tris.Length / 3;
        for (int k = 0; k < caras; k++)
        {
            for (int e = 0; e < 3; e++)
            {
                int a = f.tris[k * 3 + e];
                int b = f.tris[k * 3 + (e + 1) % 3];
                long clave = (long)Mathf.Min(a, b) * 1000000L + Mathf.Max(a, b);
                int pos;
                if (mapa.TryGetValue(clave, out pos))
                {
                    var ar = f.aristas[pos];
                    ar.w = k;
                    f.aristas[pos] = ar;
                }
                else
                {
                    mapa[clave] = f.aristas.Count;
                    f.aristas.Add(new Vector4(a, b, k, -1));
                }
            }
        }
        f.suavizadoArmado = f.d.suavizado;
        Deformada(f);
    }

    void Deformada(Figura f)
    {
        int n = f.baseV.Length;
        if (f.defV == null || f.defV.Length != n)
            f.defV = new Vector3[n];
        for (int i = 0; i < n; i++)
            f.defV[i] = Deformar(f, f.baseV[i]);
        int caras = f.tris.Length / 3;
        if (f.normalCara == null || f.normalCara.Length != caras)
        {
            f.normalCara = new Vector3[caras];
            f.centroCara = new Vector3[caras];
        }
        for (int k = 0; k < caras; k++)
        {
            Vector3 a = f.defV[f.tris[k * 3]], b = f.defV[f.tris[k * 3 + 1]], c = f.defV[f.tris[k * 3 + 2]];
            f.normalCara[k] = Vector3.Cross(b - a, c - a).normalized;
            f.centroCara[k] = (a + b + c) / 3f;
        }

        // Relleno plano (con el estilo vivo de la capa, para que tiemble junto con la línea).
        var e = dibujo.EstiloDe(f.d.capa);
        Color color = Trazo.Paleta[Mathf.Abs(f.d.color) % Trazo.Paleta.Length];
        col.Clear();
        uv3.Clear();
        uv4.Clear();
        for (int i = 0; i < n; i++)
        {
            col.Add(color);
            uv3.Add(e.a);
            uv4.Add(e.b);
        }
        f.relleno.Clear();
        f.relleno.vertices = f.defV;
        f.relleno.SetColors(col);
        f.relleno.SetUVs(2, uv3);
        f.relleno.SetUVs(3, uv4);
        f.relleno.triangles = f.tris;
        f.relleno.RecalculateBounds();
        f.sucia = false;
    }

    // La silueta y los bordes marcados que se ven desde "vista" (en el mundo).
    void Silueta(Figura f, Vector3 vista)
    {
        f.cadenas.Clear();
        f.cerradas.Clear();
        if (f.defV == null)
            return;
        Vector3 V = f.raiz.InverseTransformPoint(vista);
        int caras = f.normalCara.Length;
        var frente = new bool[caras];
        for (int k = 0; k < caras; k++)
            frente[k] = Vector3.Dot(f.normalCara[k], V - f.centroCara[k]) > 0f;
        float cosBorde = Mathf.Cos(anguloBorde * Mathf.Deg2Rad);

        var vecinos = new Dictionary<int, List<int>>();
        foreach (var ar in f.aristas)
        {
            int c1 = (int)ar.z, c2 = (int)ar.w;
            if (c2 < 0)
                continue;
            bool silueta = frente[c1] != frente[c2];
            bool borde = frente[c1] && frente[c2] && Vector3.Dot(f.normalCara[c1], f.normalCara[c2]) < cosBorde;
            if (!silueta && !borde)
                continue;
            int a = (int)ar.x, b = (int)ar.y;
            Agregar(vecinos, a, b);
            Agregar(vecinos, b, a);
        }

        // Se juntan las aristas en cadenas (líneas largas).
        var usadas = new HashSet<long>();
        foreach (var par in vecinos)
        {
            foreach (int otro in par.Value)
            {
                long clave = Clave(par.Key, otro);
                if (usadas.Contains(clave))
                    continue;
                var cadena = new List<int> { par.Key, otro };
                usadas.Add(clave);
                Extender(cadena, vecinos, usadas);
                cadena.Reverse();
                Extender(cadena, vecinos, usadas);
                bool cerrada = cadena.Count > 3 && cadena[0] == cadena[cadena.Count - 1];
                var puntos = new List<Vector3>(cadena.Count);
                foreach (int i in cadena)
                    puntos.Add(f.defV[i]);
                if (cerrada)
                    puntos.RemoveAt(puntos.Count - 1);
                Suavizar(puntos, cerrada);
                f.cadenas.Add(puntos);
                f.cerradas.Add(cerrada);
            }
        }
    }

    static long Clave(int a, int b)
    {
        return (long)Mathf.Min(a, b) * 1000000L + Mathf.Max(a, b);
    }

    static void Agregar(Dictionary<int, List<int>> vecinos, int a, int b)
    {
        List<int> l;
        if (!vecinos.TryGetValue(a, out l))
        {
            l = new List<int>();
            vecinos[a] = l;
        }
        if (!l.Contains(b))
            l.Add(b);
    }

    static void Extender(List<int> cadena, Dictionary<int, List<int>> vecinos, HashSet<long> usadas)
    {
        while (true)
        {
            int ultimo = cadena[cadena.Count - 1];
            List<int> l;
            if (!vecinos.TryGetValue(ultimo, out l))
                return;
            int siguiente = -1;
            foreach (int o in l)
            {
                if (!usadas.Contains(Clave(ultimo, o)))
                {
                    siguiente = o;
                    break;
                }
            }
            if (siguiente < 0)
                return;
            usadas.Add(Clave(ultimo, siguiente));
            cadena.Add(siguiente);
            if (siguiente == cadena[0])
                return;
        }
    }

    // Quita el "zigzag" de la silueta (promedia cada punto con sus vecinos).
    static void Suavizar(List<Vector3> p, bool cerrada)
    {
        int n = p.Count;
        if (n < 3)
            return;
        var copia = new Vector3[n];
        for (int vuelta = 0; vuelta < 2; vuelta++)
        {
            p.CopyTo(copia);
            for (int i = 0; i < n; i++)
            {
                if (!cerrada && (i == 0 || i == n - 1))
                    continue;
                Vector3 a = copia[(i - 1 + n) % n];
                Vector3 b = copia[(i + 1) % n];
                p[i] = copia[i] * 0.5f + (a + b) * 0.25f;
            }
        }
    }

    // La línea del contorno: igual que las líneas del dibujo (cinta que mira a la cámara, con hebras y temblor).
    void ConstruirLinea(Figura f)
    {
        v3.Clear(); nor.Clear(); uv.Clear(); uv2.Clear(); uv3.Clear(); uv4.Clear(); idx.Clear();
        var e = dibujo.EstiloDe(f.d.capa);
        int hebras = Mathf.Max(1, e.hebras);
        float escala = Mathf.Max(0.0001f, f.d.escala);
        float medio = f.d.ancho * 0.5f / escala * (hebras > 1 ? e.grosorHebra : 1f);
        for (int k = 0; k < f.cadenas.Count; k++)
        {
            var p = f.cadenas[k];
            bool cerrada = f.cerradas[k];
            int n = p.Count;
            if (n < 2)
                continue;
            float total = 0f;
            for (int i = 1; i < n; i++)
                total += Vector3.Distance(p[i - 1], p[i]);
            if (cerrada)
                total += Vector3.Distance(p[n - 1], p[0]);
            if (total < 1e-6f)
                continue;
            int puntos = cerrada ? n + 1 : n;
            for (int hebra = 0; hebra < hebras; hebra++)
            {
                int inicio = v3.Count;
                float recorrido = 0f;
                for (int i = 0; i < puntos; i++)
                {
                    Vector3 q = p[i % n];
                    if (i > 0)
                        recorrido += Vector3.Distance(p[(i - 1) % n], q);
                    Vector3 antes = p[cerrada ? (i - 1 + n) % n : Mathf.Max(0, i - 1)];
                    Vector3 despues = p[cerrada ? (i + 1) % n : Mathf.Min(n - 1, i + 1)];
                    Vector3 t = despues - antes;
                    t = t.sqrMagnitude > 1e-12f ? t.normalized : Vector3.forward;
                    float a = recorrido / total;
                    float h = medio;
                    if (!cerrada)
                        h *= Mathf.Max(0.15f, Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * a)), 0.4f));
                    v3.Add(q); nor.Add(t); uv.Add(new Vector2(-h, 0f)); uv2.Add(new Vector2(hebra, a)); uv3.Add(e.a); uv4.Add(e.b);
                    v3.Add(q); nor.Add(t); uv.Add(new Vector2(h, 0f)); uv2.Add(new Vector2(hebra, a)); uv3.Add(e.a); uv4.Add(e.b);
                    if (i > 0)
                    {
                        int b = inicio + (i - 1) * 2;
                        idx.Add(b); idx.Add(b + 2); idx.Add(b + 1);
                        idx.Add(b + 1); idx.Add(b + 2); idx.Add(b + 3);
                    }
                }
            }
        }
        f.linea.Clear();
        f.linea.indexFormat = v3.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        f.linea.SetVertices(v3);
        f.linea.SetNormals(nor);
        f.linea.SetUVs(0, uv);
        f.linea.SetUVs(1, uv2);
        f.linea.SetUVs(2, uv3);
        f.linea.SetUVs(3, uv4);
        f.linea.SetTriangles(idx, 0);
        f.linea.RecalculateBounds();
        var caja = f.linea.bounds;
        caja.Expand(medio * 4f);
        f.linea.bounds = caja;
    }
}

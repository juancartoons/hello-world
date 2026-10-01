using System.Collections.Generic;
using UnityEngine;

// Los gestos de TrazoVR (todo con las manos):
//  Mano izquierda, pulgar + ÍNDICE (sostener)  -> dibujar con la punta del índice derecho.
//  Mano izquierda, pulgar + MEDIO  (sostener)  -> ver los nodos; pellizca un nodo con la derecha y arrástralo.
//  Mano izquierda, pulgar + ANULAR (sostener)  -> sube o baja la mano izquierda: líneas más gruesas o delgadas.
//  Rombo con las dos manos (índices juntos y pulgares juntos) -> caja para escalar/mover todo el dibujo.
//  Palma izquierda mirándote -> aparece el panel de botones.
[DefaultExecutionOrder(-50)]
public class ControlManos : MonoBehaviour
{
    public static ControlManos Instancia { get; private set; }

    public Dibujo dibujo;
    public CajaTransformar caja;
    public Material materialNodo;
    public Material materialNodoActivo;
    public Material materialCursor;

    [Header("Gestos (en metros)")]
    public float pellizcoEntra = 0.02f;
    public float pellizcoSale = 0.035f;
    [Tooltip("Segundos que hay que sostener un pellizco antes de que cuente")]
    public float confirmarGesto = 0.08f;
    [Tooltip("Más alto = sigue más rápido al dedo; más bajo = más suave")]
    public float suavizado = 16f;
    public float radioAgarreNodo = 0.025f;
    public float tamanoNodo = 0.012f;
    [Tooltip("Cuánto cambia el grosor al subir/bajar la mano izquierda")]
    public float sensibilidadGrosor = 4f;
    public float distanciaRombo = 0.035f;

    public enum Gesto { Ninguno, Dibujar, Nodos, Grosor }
    public Gesto GestoIzq { get; private set; }

    public ManoSeguida Izq { get; } = new ManoSeguida(true);
    public ManoSeguida Der { get; } = new ManoSeguida(false);
    public Transform Cabeza { get; private set; }

    OVRCameraRig rig;
    Gesto candidato;
    float candidatoDesde;
    Trazo trazoActual;

    readonly List<Transform> nodosVisibles = new List<Transform>();
    readonly List<Renderer> nodosRender = new List<Renderer>();
    Trazo nodoTrazo;
    int nodoIndice = -1;
    bool arrastrando;
    Vector3 desfaseNodo;

    float alturaInicialGrosor;
    float tiempoRombo;
    bool esperarSoltarRombo;

    Transform cursor;
    static Mesh mallaNodo;

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

        var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "Cursor";
        Destroy(esfera.GetComponent<Collider>());
        esfera.transform.SetParent(transform, false);
        var r = esfera.GetComponent<Renderer>();
        if (materialCursor != null)
            r.sharedMaterial = materialCursor;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        cursor = esfera.transform;
        cursor.gameObject.SetActive(false);
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

        RevisarRombo();

        if (caja != null && caja.Activa)
        {
            if (GestoIzq != Gesto.Ninguno)
                SalirDeGesto();
            caja.Actualizar(Izq, Der);
        }
        else
        {
            ActualizarGestoIzquierdo();
        }

        if (GestoIzq == Gesto.Dibujar)
            Dibujar();
        else if (GestoIzq == Gesto.Nodos)
            EditarNodos();
        else if (GestoIzq == Gesto.Grosor)
            CambiarGrosor();

        if (GestoIzq != Gesto.Nodos)
            OcultarNodosDesde(0);
        ActualizarCursor();
    }

    // ---------- Mano izquierda: qué dedo toca el pulgar ----------

    void ActualizarGestoIzquierdo()
    {
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

        if (GestoIzq != Gesto.Ninguno)
        {
            float d = GestoIzq == Gesto.Dibujar ? dIndice : GestoIzq == Gesto.Nodos ? dMedio : dAnular;
            if (d > pellizcoSale)
                SalirDeGesto();
            return;
        }

        Gesto nuevo = Gesto.Ninguno;
        float menor = pellizcoEntra;
        if (dIndice < menor) { menor = dIndice; nuevo = Gesto.Dibujar; }
        if (dMedio < menor) { menor = dMedio; nuevo = Gesto.Nodos; }
        if (dAnular < menor) { nuevo = Gesto.Grosor; }

        if (nuevo != candidato)
        {
            candidato = nuevo;
            candidatoDesde = Time.time;
        }
        if (nuevo != Gesto.Ninguno && Time.time - candidatoDesde >= confirmarGesto)
            EntrarEnGesto(nuevo);
    }

    void EntrarEnGesto(Gesto g)
    {
        GestoIzq = g;
        if (g == Gesto.Grosor)
        {
            alturaInicialGrosor = Izq.pulgar.y;
            dibujo.EmpezarGrosor();
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
            dibujo.TerminarGrosor();
        arrastrando = false;
        nodoTrazo = null;
        nodoIndice = -1;
        GestoIzq = Gesto.Ninguno;
        candidato = Gesto.Ninguno;
    }

    // ---------- Dibujar ----------

    void Dibujar()
    {
        if (!Der.valida)
            return;
        if (trazoActual == null)
            trazoActual = dibujo.NuevoTrazo();
        trazoActual.AgregarPuntoCrudo(dibujo.transform.InverseTransformPoint(Der.indice));
    }

    // ---------- Editar nodos ----------

    void EditarNodos()
    {
        if (!Der.valida)
        {
            arrastrando = false;
        }
        else if (arrastrando)
        {
            if (!Der.pellizco || nodoTrazo == null)
            {
                arrastrando = false;
            }
            else
            {
                Vector3 mundo = Der.PuntoPellizco + desfaseNodo;
                dibujo.MoverNodo(nodoTrazo, nodoIndice, dibujo.transform.InverseTransformPoint(mundo));
            }
        }
        else
        {
            Trazo t;
            int i;
            if (BuscarNodoCercano(Der.indice, Der.PuntoPellizco, out t, out i))
            {
                nodoTrazo = t;
                nodoIndice = i;
                if (Der.empezoPellizco)
                {
                    dibujo.GuardarParaDeshacer();
                    desfaseNodo = dibujo.transform.TransformPoint(t.nodos[i]) - Der.PuntoPellizco;
                    arrastrando = true;
                }
            }
            else
            {
                nodoTrazo = null;
                nodoIndice = -1;
            }
        }
        MostrarNodos();
    }

    bool BuscarNodoCercano(Vector3 a, Vector3 b, out Trazo trazo, out int indice)
    {
        trazo = null;
        indice = -1;
        float mejor = radioAgarreNodo;
        foreach (var t in dibujo.trazos)
        {
            if (t == null)
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

    void MostrarNodos()
    {
        int n = 0;
        foreach (var t in dibujo.trazos)
        {
            if (t == null)
                continue;
            for (int i = 0; i < t.nodos.Count; i++)
            {
                if (n >= nodosVisibles.Count)
                    CrearNodoVisible();
                var nodo = nodosVisibles[n];
                if (!nodo.gameObject.activeSelf)
                    nodo.gameObject.SetActive(true);
                nodo.position = dibujo.transform.TransformPoint(t.nodos[i]);
                nodo.rotation = Quaternion.identity;
                bool activo = t == nodoTrazo && i == nodoIndice;
                nodo.localScale = Vector3.one * (activo ? tamanoNodo * 1.4f : tamanoNodo);
                var mat = activo ? materialNodoActivo : materialNodo;
                if (mat != null && nodosRender[n].sharedMaterial != mat)
                    nodosRender[n].sharedMaterial = mat;
                n++;
            }
        }
        OcultarNodosDesde(n);
    }

    void OcultarNodosDesde(int desde)
    {
        for (int i = desde; i < nodosVisibles.Count; i++)
            if (nodosVisibles[i].gameObject.activeSelf)
                nodosVisibles[i].gameObject.SetActive(false);
    }

    void CrearNodoVisible()
    {
        var go = new GameObject("Nodo");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = MallaNodo();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = materialNodo;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        nodosVisibles.Add(go.transform);
        nodosRender.Add(r);
    }

    // Rombo 3D (octaedro): pocos triángulos y se ve limpio, tipo vector.
    static Mesh MallaNodo()
    {
        if (mallaNodo != null)
            return mallaNodo;
        mallaNodo = new Mesh { name = "Nodo" };
        mallaNodo.vertices = new[]
        {
            new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 0f, 0f),
            new Vector3(0f, 0.5f, 0f), new Vector3(0f, -0.5f, 0f),
            new Vector3(0f, 0f, 0.5f), new Vector3(0f, 0f, -0.5f)
        };
        mallaNodo.triangles = new[] { 2, 4, 0, 2, 0, 5, 2, 5, 1, 2, 1, 4, 3, 0, 4, 3, 5, 0, 3, 1, 5, 3, 4, 1 };
        mallaNodo.RecalculateNormals();
        mallaNodo.RecalculateBounds();
        return mallaNodo;
    }

    // ---------- Grosor ----------

    void CambiarGrosor()
    {
        if (!Izq.valida)
            return;
        float factor = Mathf.Exp((Izq.pulgar.y - alturaInicialGrosor) * sensibilidadGrosor);
        dibujo.AplicarFactorGrosor(factor);
    }

    // ---------- Rombo con las dos manos: caja de transformar ----------

    void RevisarRombo()
    {
        if (caja == null || !Izq.valida || !Der.valida)
        {
            tiempoRombo = 0f;
            return;
        }
        float dIndices = Vector3.Distance(Izq.indice, Der.indice);
        float dPulgares = Vector3.Distance(Izq.pulgar, Der.pulgar);
        if (esperarSoltarRombo)
        {
            if (dIndices > distanciaRombo * 2f || dPulgares > distanciaRombo * 2f)
                esperarSoltarRombo = false;
            tiempoRombo = 0f;
            return;
        }
        bool manosAbiertas = Vector3.Distance(Izq.indice, Izq.pulgar) > 0.05f && Vector3.Distance(Der.indice, Der.pulgar) > 0.05f;
        if (!manosAbiertas || dIndices > distanciaRombo || dPulgares > distanciaRombo)
        {
            tiempoRombo = 0f;
            return;
        }
        tiempoRombo += Time.deltaTime;
        if (tiempoRombo < 0.35f)
            return;
        tiempoRombo = 0f;
        esperarSoltarRombo = true;
        if (GestoIzq != Gesto.Ninguno)
            SalirDeGesto();
        caja.Alternar();
    }

    // ---------- Cursor y panel ----------

    void ActualizarCursor()
    {
        if (cursor == null)
            return;
        bool ver = Der.valida && !(caja != null && caja.Activa);
        if (cursor.gameObject.activeSelf != ver)
            cursor.gameObject.SetActive(ver);
        if (!ver)
            return;
        cursor.position = Der.indice;
        cursor.localScale = Vector3.one * Mathf.Max(0.003f, dibujo.anchoPincel);
    }

    // El panel se ve cuando la palma izquierda te mira y no estás haciendo un gesto.
    public bool PuedeVerPanel(float umbral, out Vector3 posicion, out Quaternion rotacion)
    {
        posicion = Vector3.zero;
        rotacion = Quaternion.identity;
        if (GestoIzq != Gesto.Ninguno || !Izq.valida || Cabeza == null)
            return false;
        if (caja != null && caja.Agarrando)
            return false;
        var palma = ManosUtil.LeerPalma(Izq.esqueleto, true);
        if (!palma.valida)
            return false;
        Vector3 haciaCabeza = (Cabeza.position - palma.centro).normalized;
        if (Vector3.Dot(palma.normal, haciaCabeza) < umbral)
            return false;
        posicion = palma.centro + palma.normal * 0.09f + Vector3.up * 0.03f;
        Vector3 mirar = posicion - Cabeza.position;
        if (mirar.sqrMagnitude < 1e-6f)
            return false;
        rotacion = Quaternion.LookRotation(mirar, Vector3.up);
        return true;
    }
}

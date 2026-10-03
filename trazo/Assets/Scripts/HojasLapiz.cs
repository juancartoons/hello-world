using System.Collections.Generic;
using UnityEngine;

// Un trazo de lápiz sobre la hoja de una capa de boceto. Se guarda como puntos (no como imagen),
// así deshacer, guardar y cargar funcionan igual que con las líneas; la imagen se vuelve a pintar.
[System.Serializable]
public class TrazoLapiz
{
    public int capa;
    public bool borra;              // true = goma de borrar
    public float radio;             // radio máximo (unidades del dibujo)
    public List<Vector4> puntos = new List<Vector4>(); // xyz = punto (dibujo), w = presión (0 a 1)
}

// Lápiz de boceto: en una capa de Boceto con Plano (2D), dibujar pinta sobre una HOJA (imagen),
// como grafito sobre papel: con grano, más oscuro al repasar y "presión" según qué tan cerca está el dedo.
// La goma (puño) aclara la hoja. La hoja no sale en fotos ni videos (como todo el boceto).
public class HojasLapiz : MonoBehaviour
{
    public Dibujo dibujo;
    [Tooltip("Sello del lápiz (shader TrazoVR/SelloLapiz)")]
    public Material materialSello;
    [Tooltip("Cómo se ve la hoja (shader TrazoVR/HojaLapiz)")]
    public Material materialHoja;
    [Tooltip("Pixeles por lado de la hoja")]
    public int resolucion = 4096;
    [Tooltip("Tamaño de la hoja (unidades del dibujo). Se ve su borde con una línea delgada")]
    public float tamano = 3f;
    [Tooltip("Radio del lápiz (metros)")]
    public float radioLapiz = 0.0018f;
    [Tooltip("Radio de la goma (metros)")]
    public float radioGoma = 0.012f;
    [Tooltip("A qué distancia del plano (metros) el dedo deja de pintar")]
    public float alcance = 0.02f;

    static readonly Color Gris = new Color(0.32f, 0.32f, 0.36f, 1f);
    static readonly Color Azul = new Color(0.25f, 0.42f, 0.9f, 1f);

    class Hoja
    {
        public RenderTexture rt;
        public GameObject go;
        public Material material;
        public Vector3 origen, ejeU, ejeV, normal;
        public bool colocada;
    }

    float avisoBorde = -10f;
    bool redibujando;

    readonly Dictionary<int, Hoja> hojas = new Dictionary<int, Hoja>();
    readonly List<TrazoLapiz> trazos = new List<TrazoLapiz>();
    TrazoLapiz actual;
    static Mesh mallaHoja;

    public bool HayAlgo => trazos.Count > 0;

    void Awake()
    {
        if (dibujo == null) dibujo = GetComponent<Dibujo>();
    }

    void OnDestroy()
    {
        foreach (var h in hojas.Values)
        {
            if (h.rt != null) h.rt.Release();
            if (h.material != null) Destroy(h.material);
        }
    }

    // ---------- Pintar ----------

    // Pinta (o borra) en la hoja de la capa actual. local = punto del dedo (coordenadas del dibujo).
    public void Pintar(Vector3 local, float distanciaMundo, bool borra)
    {
        if (dibujo == null || materialSello == null)
            return;
        int capa = dibujo.capaActual;
        var h = HojaDe(capa);
        if (h == null)
            return;
        float presion = Mathf.Clamp01(1f - distanciaMundo / alcance);
        if (actual == null || actual.capa != capa || actual.borra != borra)
        {
            Terminar();
            dibujo.GuardarParaDeshacer();
            actual = new TrazoLapiz
            {
                capa = capa,
                borra = borra,
                radio = (borra ? radioGoma : radioLapiz) / dibujo.EscalaMundo,
            };
            trazos.Add(actual);
        }
        Vector3 p = dibujo.ProyectarEnPlano(local);
        // ¿Se sale de la hoja? Avisa (la hoja tiene un borde delgado que siempre se ve).
        Vector3 rel = p - h.origen;
        if ((Mathf.Abs(Vector3.Dot(rel, h.ejeU)) > tamano * 0.5f || Mathf.Abs(Vector3.Dot(rel, h.ejeV)) > tamano * 0.5f)
            && Time.time - avisoBorde > 2f)
        {
            avisoBorde = Time.time;
            dibujo.Mensaje("Borde de la hoja del lápiz");
        }
        var nuevo = new Vector4(p.x, p.y, p.z, presion);
        var lista = actual.puntos;
        if (lista.Count > 0)
        {
            Vector4 ultimo = lista[lista.Count - 1];
            if (Vector3.Distance(ultimo, p) < actual.radio * 0.3f)
                return;
            Sellar(h, actual, ultimo, nuevo);
        }
        else
        {
            Sellar(h, actual, nuevo, nuevo);
        }
        lista.Add(nuevo);
    }

    public void Terminar()
    {
        if (actual == null)
            return;
        if (actual.puntos.Count == 0)
        {
            trazos.Remove(actual);
            dibujo.DescartarUltimoDeshacer();
        }
        actual = null;
    }

    // Sellos del lápiz entre dos puntos (bien juntitos, para que la línea salga continua).
    void Sellar(Hoja h, TrazoLapiz t, Vector4 a, Vector4 b)
    {
        var prev = EmpezarSellos(h, t);
        SellosEntre(h, t, a, b);
        TerminarSellos(prev);
    }

    RenderTexture EmpezarSellos(Hoja h, TrazoLapiz t)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = h.rt;
        GL.PushMatrix();
        GL.LoadOrtho();
        materialSello.SetPass(t.borra ? 1 : 0);
        GL.Begin(GL.QUADS);
        return prev;
    }

    static void TerminarSellos(RenderTexture prev)
    {
        GL.End();
        GL.PopMatrix();
        RenderTexture.active = prev;
    }

    void SellosEntre(Hoja h, TrazoLapiz t, Vector4 a, Vector4 b)
    {
        float largo = Vector3.Distance(a, b);
        float paso = Mathf.Max(1e-5f, t.radio * 0.25f);
        int n = Mathf.Max(1, Mathf.CeilToInt(largo / paso));
        for (int i = 0; i <= n; i++)
        {
            if (largo < 1e-6f && i > 0)
                break;
            float u = n > 0 ? i / (float)n : 0f;
            Vector4 q = Vector4.Lerp(a, b, u);
            Sello(h, new Vector3(q.x, q.y, q.z), q.w, t);
        }
    }

    void Sello(Hoja h, Vector3 p, float presion, TrazoLapiz t)
    {
        Vector3 d = p - h.origen;
        float x = Vector3.Dot(d, h.ejeU) / tamano + 0.5f;
        float y = Vector3.Dot(d, h.ejeV) / tamano + 0.5f;
        float r = t.radio * (t.borra ? 1f : Mathf.Lerp(0.45f, 1f, presion)) / tamano;
        float intensidad = t.borra ? 0.5f : Mathf.Lerp(0.08f, 0.32f, presion);
        GL.Color(new Color(1f, 1f, 1f, intensidad));
        Esquina(x, y, r, -1f, -1f);
        Esquina(x, y, r, 1f, -1f);
        Esquina(x, y, r, 1f, 1f);
        Esquina(x, y, r, -1f, 1f);
    }

    static void Esquina(float x, float y, float r, float sx, float sy)
    {
        GL.MultiTexCoord2(0, sx, sy);
        GL.MultiTexCoord2(1, x + sx * r, y + sy * r);
        GL.Vertex3(x + sx * r, y + sy * r, 0f);
    }

    // ---------- Hojas ----------

    Hoja HojaDe(int capa)
    {
        Vector3 punto, normal;
        if (!dibujo.PlanoDeCapa(capa, out punto, out normal))
            return null;
        Hoja h;
        if (!hojas.TryGetValue(capa, out h))
        {
            h = new Hoja();
            // Un solo canal (gris) gasta 4 veces menos memoria; si el visor no lo soporta, color normal más chico.
            bool r8 = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);
            int lado = r8 ? resolucion : Mathf.Min(resolucion, 2560);
            h.rt = new RenderTexture(lado, lado, 0, r8 ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32);
            h.rt.wrapMode = TextureWrapMode.Clamp;
            h.rt.useMipMap = false;
            h.rt.Create();
            Limpiar(h);
            h.go = new GameObject("HojaLapiz_" + (capa + 1));
            h.go.layer = 0; // el boceto no sale en fotos ni videos
            h.go.transform.SetParent(dibujo.transform, false);
            h.go.AddComponent<MeshFilter>().sharedMesh = MallaHoja();
            var mr = h.go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            if (materialHoja != null)
            {
                h.material = new Material(materialHoja);
                h.material.SetTexture("_MainTex", h.rt);
                mr.sharedMaterial = h.material;
            }
            hojas[capa] = h;
        }
        // Si la hoja cambió de lugar (otro plano), se vuelve a pintar desde los puntos: así nunca se "corre".
        if (Colocar(h, capa, punto, normal) && !redibujando)
            RedibujarTodo();
        return h;
    }

    // Acomoda la hoja en su plano. Su giro queda FIJO (no depende de cómo esté girado el dibujo después).
    // Devuelve true si se movió (entonces hay que volver a pintarla).
    bool Colocar(Hoja h, int capa, Vector3 punto, Vector3 normal)
    {
        if (h.material != null)
            h.material.SetColor("_BaseColor", dibujo.DatosDeCapa(capa).boceto == 2 ? Azul : Gris);
        bool igual = h.colocada && Vector3.Dot(h.normal, normal) > 0.99999f && (h.origen - punto).sqrMagnitude < 1e-10f;
        if (igual)
            return false;
        Vector3 arriba = h.colocada ? Vector3.ProjectOnPlane(h.ejeV, normal)
                                    : Vector3.ProjectOnPlane(dibujo.transform.InverseTransformDirection(Vector3.up), normal);
        if (arriba.sqrMagnitude < 1e-6f)
            arriba = Vector3.ProjectOnPlane(Vector3.up, normal);
        if (arriba.sqrMagnitude < 1e-6f)
            arriba = Vector3.Cross(normal, Vector3.right);
        bool antes = h.colocada;
        h.colocada = true;
        var rot = Quaternion.LookRotation(normal, arriba.normalized);
        h.origen = punto;
        h.normal = normal;
        h.ejeU = rot * Vector3.right;
        h.ejeV = rot * Vector3.up;
        h.go.transform.localPosition = punto;
        h.go.transform.localRotation = rot;
        h.go.transform.localScale = Vector3.one * tamano;
        return antes;
    }

    static void Limpiar(Hoja h)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = h.rt;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = prev;
    }

    static Mesh MallaHoja()
    {
        if (mallaHoja != null)
            return mallaHoja;
        mallaHoja = new Mesh { name = "Hoja" };
        mallaHoja.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
        mallaHoja.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        mallaHoja.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mallaHoja.RecalculateBounds();
        return mallaHoja;
    }

    void LateUpdate()
    {
        if (dibujo == null)
            return;
        // La hoja de la capa activa aparece apenas hay plano (así ves su borde desde el principio).
        if (dibujo.UsaHoja && materialSello != null)
            HojaDe(dibujo.capaActual);
        foreach (var par in hojas)
        {
            var h = par.Value;
            if (h.go == null)
                continue;
            bool ver = dibujo.CapaVisible(par.Key) && dibujo.EsBoceto(par.Key);
            if (h.go.activeSelf != ver)
                h.go.SetActive(ver);
        }
    }

    // ---------- Guardar, cargar, deshacer ----------

    // Vuelve a pintar todas las hojas desde los trazos guardados (al cargar, deshacer o mover un plano).
    public void RedibujarTodo()
    {
        redibujando = true;
        try
        {
            RedibujarTodoInterno();
        }
        finally
        {
            redibujando = false;
        }
    }

    void RedibujarTodoInterno()
    {
        foreach (var h in hojas.Values)
            Limpiar(h);
        if (materialSello == null)
            return;
        // Un solo "pase" por trazo (mucho más rápido que sello por sello).
        foreach (var t in trazos)
        {
            var h = HojaDe(t.capa);
            if (h == null || t.puntos.Count == 0)
                continue;
            var prev = EmpezarSellos(h, t);
            for (int i = 0; i < t.puntos.Count; i++)
                SellosEntre(h, t, t.puntos[Mathf.Max(0, i - 1)], t.puntos[i]);
            TerminarSellos(prev);
        }
    }

    public void Limpiar()
    {
        actual = null;
        trazos.Clear();
        foreach (var h in hojas.Values)
            Limpiar(h);
    }

    public void GuardarEn(DatosDibujo d)
    {
        foreach (var t in trazos)
            if (t != actual || t.puntos.Count > 0)
                d.lapiz.Add(t);
    }

    public void Restaurar(DatosDibujo d)
    {
        actual = null;
        var nuevos = new List<TrazoLapiz>();
        if (d.lapiz != null)
            foreach (var t in d.lapiz)
                if (t != null && t.puntos != null)
                    nuevos.Add(t);
        // Si el lápiz no cambió (deshiciste otra cosa), no hace falta volver a pintar las hojas.
        bool igual = nuevos.Count == trazos.Count;
        for (int i = 0; igual && i < nuevos.Count; i++)
            igual = Iguales(nuevos[i], trazos[i]);
        trazos.Clear();
        trazos.AddRange(nuevos);
        if (!igual)
            RedibujarTodo();
        else
            foreach (var capa in new List<int>(hojas.Keys))
                HojaDe(capa); // solo acomoda las hojas (por si cambió el plano)
    }

    static bool Iguales(TrazoLapiz a, TrazoLapiz b)
    {
        if (a.capa != b.capa || a.borra != b.borra || a.puntos.Count != b.puntos.Count)
            return false;
        int n = a.puntos.Count;
        return n == 0 || (a.puntos[0] == b.puntos[0] && a.puntos[n - 1] == b.puntos[n - 1] && a.puntos[n / 2] == b.puntos[n / 2]);
    }
}

using UnityEngine;

// Halo brillante y suave alrededor del pájaro cuando lo encuentras.
// Es un anillo dorado con bordes difuminados que siempre mira hacia ti y "respira" (crece y se achica un poco).
public class HaloPajaro : MonoBehaviour
{
    [Tooltip("Material FarmaciaVR/Plano (lo pone el menú de FarmaciaVR)")]
    public Material material;
    public Color color = new Color(1f, 0.82f, 0.25f, 1f);
    [Tooltip("Tamaño del halo comparado con el pájaro")]
    public float tamano = 2.6f;
    public float velocidadPulso = 2.5f;

    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");

    Transform cabeza;
    GameObject halo;
    MeshRenderer render;
    MaterialPropertyBlock bloque;
    Renderer[] rendersPajaro;
    float radioPajaro = 0.07f;
    float aparicion;
    bool visible;

    void Start()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        cabeza = rig != null ? rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : null;
        rendersPajaro = GetComponentsInChildren<Renderer>();

        // El halo va suelto (no hijo del pájaro): así no cambia el tamaño ni el color del pájaro.
        halo = new GameObject("Halo_" + name);
        halo.AddComponent<MeshFilter>().sharedMesh = CrearMalla();
        render = halo.AddComponent<MeshRenderer>();
        render.sharedMaterial = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        bloque = new MaterialPropertyBlock();
        halo.SetActive(false);
    }

    public void Mostrar(bool mostrar)
    {
        visible = mostrar;
        if (halo == null)
            return;
        if (mostrar)
        {
            aparicion = 0f;
            radioPajaro = Mathf.Max(0.04f, Limites().extents.magnitude * 0.75f);
        }
        halo.SetActive(mostrar);
    }

    void LateUpdate()
    {
        if (!visible || halo == null || cabeza == null)
            return;
        aparicion = Mathf.MoveTowards(aparicion, 1f, Time.deltaTime * 2.5f);
        float pulso = 1f + 0.08f * Mathf.Sin(Time.time * velocidadPulso);
        float escala = radioPajaro * tamano * pulso * Mathf.SmoothStep(0.5f, 1f, aparicion);

        Vector3 centro = Limites().center;
        Vector3 haciaCabeza = cabeza.position - centro;
        halo.transform.SetPositionAndRotation(centro, Quaternion.LookRotation(-haciaCabeza, Vector3.up));
        halo.transform.localScale = Vector3.one * escala;

        render.GetPropertyBlock(bloque);
        bloque.SetColor(idBaseColor, new Color(color.r, color.g, color.b, color.a * aparicion * (0.85f + 0.15f * Mathf.Sin(Time.time * velocidadPulso * 1.3f))));
        render.SetPropertyBlock(bloque);
    }

    Bounds Limites()
    {
        if (rendersPajaro == null || rendersPajaro.Length == 0 || rendersPajaro[0] == null)
            return new Bounds(transform.position, Vector3.one * 0.1f);
        Bounds b = rendersPajaro[0].bounds;
        for (int i = 1; i < rendersPajaro.Length; i++)
            if (rendersPajaro[i] != null)
                b.Encapsulate(rendersPajaro[i].bounds);
        return b;
    }

    // Disco de radio 1 hecho de anillos: transparente en el centro, brillante en el anillo y difuminado hacia afuera.
    static Mesh CrearMalla()
    {
        float[] radios = { 0f, 0.42f, 0.6f, 1f };
        float[] alfas = { 0.12f, 0.18f, 0.85f, 0f };
        const int lados = 48;
        var vertices = new Vector3[radios.Length * lados];
        var colores = new Color[vertices.Length];
        for (int r = 0; r < radios.Length; r++)
            for (int i = 0; i < lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                vertices[r * lados + i] = new Vector3(Mathf.Cos(a) * radios[r], Mathf.Sin(a) * radios[r], 0f);
                colores[r * lados + i] = new Color(1f, 1f, 1f, alfas[r]);
            }
        var triangulos = new int[(radios.Length - 1) * lados * 6];
        int t = 0;
        for (int r = 0; r < radios.Length - 1; r++)
            for (int i = 0; i < lados; i++)
            {
                int a = r * lados + i, b = r * lados + (i + 1) % lados;
                int c = a + lados, d = b + lados;
                triangulos[t++] = a; triangulos[t++] = c; triangulos[t++] = b;
                triangulos[t++] = b; triangulos[t++] = c; triangulos[t++] = d;
            }
        var malla = new Mesh { name = "Halo" };
        malla.vertices = vertices;
        malla.colors = colores;
        malla.triangles = triangulos;
        malla.RecalculateBounds();
        return malla;
    }

    void OnDestroy()
    {
        if (halo != null)
            Destroy(halo);
    }
}

using UnityEngine;

// Cruz verde de farmacia hecha de "pixeles" LED (como las de Bogotá), con animaciones que cambian:
// late suave, ondas desde el centro, barrido, parpadeo y una luz que da vueltas por el borde.
// Se ve por los dos lados.
public class CruzLED : MonoBehaviour
{
    [Tooltip("Material FarmaciaVR/Plano con colores de la malla (lo pone el menú ★)")]
    public Material material;
    [Tooltip("Tamaño de cada pixel (metros)")]
    public float paso = 0.08f;
    [Tooltip("Separación de cada cara desde el centro de la caja (metros)")]
    public float separacion = 0.045f;
    public Color encendido = new Color(0.2f, 1f, 0.45f);
    public Color apagado = new Color(0.03f, 0.16f, 0.07f);
    public float segundosPorAnimacion = 4f;

    const int tam = 9;
    Mesh malla;
    Color[] colores;
    Vector2Int[] pixeles;

    void Start()
    {
        var lista = new System.Collections.Generic.List<Vector2Int>();
        for (int i = 0; i < tam; i++)
            for (int j = 0; j < tam; j++)
                if (Mathf.Abs(i - 4) <= 1 || Mathf.Abs(j - 4) <= 1)
                    lista.Add(new Vector2Int(i, j));
        pixeles = lista.ToArray();

        // Cada pixel es un cuadrito en cada cara (la cruz está en el plano YZ, mirando hacia ±X).
        var vertices = new Vector3[pixeles.Length * 8];
        var triangulos = new int[pixeles.Length * 12];
        float m = paso * 0.42f;
        for (int p = 0; p < pixeles.Length; p++)
        {
            float y = (pixeles[p].y - 4) * paso, z = (pixeles[p].x - 4) * paso;
            for (int cara = 0; cara < 2; cara++)
            {
                float x = cara == 0 ? separacion : -separacion;
                int v = p * 8 + cara * 4;
                vertices[v] = new Vector3(x, y - m, z - m);
                vertices[v + 1] = new Vector3(x, y + m, z - m);
                vertices[v + 2] = new Vector3(x, y + m, z + m);
                vertices[v + 3] = new Vector3(x, y - m, z + m);
                int t = p * 12 + cara * 6;
                triangulos[t] = v; triangulos[t + 1] = v + 1; triangulos[t + 2] = v + 2;
                triangulos[t + 3] = v; triangulos[t + 4] = v + 2; triangulos[t + 5] = v + 3;
            }
        }
        colores = new Color[vertices.Length];
        malla = new Mesh { name = "CruzLED" };
        malla.vertices = vertices;
        malla.triangles = triangulos;
        malla.colors = colores;
        malla.RecalculateBounds();
        gameObject.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = gameObject.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void Update()
    {
        if (malla == null)
            return;
        float t = Time.time;
        int animacion = (int)(t / segundosPorAnimacion) % 5;
        float local = t % segundosPorAnimacion;
        for (int p = 0; p < pixeles.Length; p++)
        {
            float b = Brillo(animacion, local, pixeles[p].x, pixeles[p].y);
            Color c = Color.Lerp(apagado, encendido, b);
            for (int v = 0; v < 8; v++)
                colores[p * 8 + v] = c;
        }
        malla.colors = colores;
    }

    float Brillo(int animacion, float t, int i, int j)
    {
        float dx = i - 4, dy = j - 4;
        switch (animacion)
        {
            case 0: // late suave
                return 0.55f + 0.45f * Mathf.Sin(t * 3f);
            case 1: // ondas desde el centro
                return Mathf.Clamp01(Mathf.Sin(Mathf.Sqrt(dx * dx + dy * dy) * 1.2f - t * 6f) * 0.5f + 0.5f);
            case 2: // barrido de abajo hacia arriba
                return Mathf.Clamp01(1f - Mathf.Abs((j - (t * 4f % 12f - 2f))) * 0.5f) * 0.8f + 0.2f;
            case 3: // parpadeo
                return (int)(t * 4f) % 2 == 0 ? 1f : 0.15f;
            default: // luz que da vueltas por el borde
            {
                float angulo = Mathf.Atan2(dy, dx);
                float giro = Mathf.Repeat(t * 3f, Mathf.PI * 2f) - Mathf.PI;
                float diferencia = Mathf.Abs(Mathf.DeltaAngle(angulo * Mathf.Rad2Deg, giro * Mathf.Rad2Deg));
                return Mathf.Clamp01(1f - diferencia / 70f) * 0.85f + 0.15f;
            }
        }
    }
}

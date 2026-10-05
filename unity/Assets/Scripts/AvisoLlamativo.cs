using UnityEngine;

// Llama la atención hacia el aviso del premio cuando aparece arriba (por encima de las góndolas):
// destellos dorados que suben desde frente al jugador hasta el aviso, y un resplandor suave que late detrás.
public class AvisoLlamativo : MonoBehaviour
{
    [Tooltip("Material FarmaciaVR/Luz (luz sumada); lo pone el menú ★")]
    public Material material;
    public Color color = new Color(1f, 0.82f, 0.35f);
    public int cantidadDestellos = 18;
    [Tooltip("Segundos que tarda un destello en subir hasta el aviso")]
    public float duracionSubida = 1.4f;

    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");

    Transform cabeza, resplandor;
    Transform[] destellos;
    MeshRenderer[] rendersDestellos;
    MeshRenderer renderResplandor;
    MaterialPropertyBlock bloque;
    float[] desfase;
    Vector3 posicionAviso, haciaAviso;
    bool activo;
    float inicio;

    void Start()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        cabeza = rig != null ? rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : null;
        bloque = new MaterialPropertyBlock();
        var malla = CrearDisco();

        resplandor = Crear("Aviso_Resplandor", malla, out renderResplandor);
        destellos = new Transform[cantidadDestellos];
        rendersDestellos = new MeshRenderer[cantidadDestellos];
        desfase = new float[cantidadDestellos];
        for (int i = 0; i < cantidadDestellos; i++)
        {
            destellos[i] = Crear("Aviso_Destello", malla, out rendersDestellos[i]);
            desfase[i] = Random.value;
        }
        Ocultar();
    }

    Transform Crear(string nombre, Mesh malla, out MeshRenderer render)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        render = go.AddComponent<MeshRenderer>();
        render.sharedMaterial = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    // "posicion" y "rotacion" son las del aviso (el panel del premio).
    public void Mostrar(Vector3 posicion, Quaternion rotacion)
    {
        if (resplandor == null)
            return;
        posicionAviso = posicion;
        haciaAviso = rotacion * Vector3.forward;
        resplandor.SetPositionAndRotation(posicion + haciaAviso * 0.03f, rotacion);
        resplandor.gameObject.SetActive(true);
        foreach (var d in destellos)
            d.gameObject.SetActive(true);
        activo = true;
        inicio = Time.time;
    }

    // Mueve el aviso (cuando se reacomoda frente al jugador) sin reiniciar los destellos.
    public void Mover(Vector3 posicion, Quaternion rotacion)
    {
        if (!activo || resplandor == null)
            return;
        posicionAviso = posicion;
        haciaAviso = rotacion * Vector3.forward;
        resplandor.SetPositionAndRotation(posicion + haciaAviso * 0.03f, rotacion);
    }

    public void Ocultar()
    {
        activo = false;
        if (resplandor == null)
            return;
        resplandor.gameObject.SetActive(false);
        foreach (var d in destellos)
            d.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!activo || cabeza == null)
            return;
        float t = Time.time - inicio;

        // Resplandor que late detrás del aviso
        float pulso = 0.75f + 0.25f * Mathf.Sin(t * 3f);
        resplandor.localScale = new Vector3(0.7f, 0.46f, 1f) * (1f + 0.04f * Mathf.Sin(t * 3f));
        Pintar(renderResplandor, 0.55f * pulso * Mathf.Clamp01(t * 2f));

        // Destellos: salen frente al jugador (a la altura del pecho) y suben en curva hasta el aviso.
        // Los primeros segundos son muchos; después quedan unos pocos, suaves.
        Vector3 frente = posicionAviso - cabeza.position;
        frente.y = 0f;
        frente = frente.sqrMagnitude > 0.001f ? frente.normalized : Vector3.forward;
        Vector3 lado = Vector3.Cross(Vector3.up, frente);
        Vector3 salida = cabeza.position + frente * 0.6f + Vector3.down * 0.45f;
        float intensidad = t < 5f ? 1f : 0.35f;
        for (int i = 0; i < destellos.Length; i++)
        {
            float u = Mathf.Repeat(t / duracionSubida + desfase[i], 1f);
            float abierto = (desfase[i] - 0.5f) * 0.9f;
            Vector3 p = Vector3.Lerp(salida, posicionAviso, u) + lado * (abierto * Mathf.Sin(u * Mathf.PI))
                        + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 0.15f);
            destellos[i].SetPositionAndRotation(p, Quaternion.LookRotation(p - cabeza.position, Vector3.up));
            destellos[i].localScale = Vector3.one * Mathf.Lerp(0.035f, 0.06f, Mathf.Sin(u * Mathf.PI));
            Pintar(rendersDestellos[i], Mathf.Sin(u * Mathf.PI) * intensidad);
        }
    }

    void Pintar(MeshRenderer r, float brillo)
    {
        r.GetPropertyBlock(bloque);
        bloque.SetColor(idBaseColor, color * brillo);
        r.SetPropertyBlock(bloque);
    }

    // Disco de radio 1 con el centro brillante y el borde difuminado (los colores van en los vértices).
    static Mesh CrearDisco()
    {
        const int lados = 24;
        var vertices = new Vector3[lados + 1];
        var colores = new Color[lados + 1];
        var triangulos = new int[lados * 3];
        vertices[0] = Vector3.zero;
        colores[0] = Color.white;
        for (int i = 0; i < lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            vertices[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            colores[i + 1] = new Color(1f, 1f, 1f, 0f);
            triangulos[i * 3] = 0;
            triangulos[i * 3 + 1] = 1 + (i + 1) % lados;
            triangulos[i * 3 + 2] = 1 + i;
        }
        var malla = new Mesh { name = "DiscoSuave" };
        malla.vertices = vertices;
        malla.colors = colores;
        malla.triangles = triangulos;
        malla.RecalculateBounds();
        return malla;
    }
}

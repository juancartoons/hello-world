using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Fondo de TrazoVR:
//  0 = blanco con piso de cuadrícula, 1 = todo blanco, 2 = realidad (passthrough: ves tu cuarto),
//  3 = cuarto 360 (una foto 360 de un cuarto real, viene con la app: Plugins/Fondos),
//  4 = mi foto 360 (la que elegiste en "Imagen +" con el botón "Fondo 360"; solo aparece si hay una).
// La PRIMERA vez la app empieza en Realidad (passthrough: dibujas sobre tu cuarto); después recuerda el último.
public class Escenario : MonoBehaviour
{
    const string ClaveFondo = "jcartoons_fondo";
    public Material materialCuadricula;
    [Tooltip("0 cuadrícula, 1 blanco, 2 realidad (passthrough)")]
    public int modo;
    public Color fondo = Color.white;
    [Tooltip("Tamaño del piso en metros")]
    public float tamano = 10f;
    [Tooltip("Distancia entre líneas de la cuadrícula en metros")]
    public float paso = 0.5f;
    [Tooltip("Capa de passthrough (la crea el menú TrazoVR)")]
    public OVRPassthroughLayer passthrough;

    [Tooltip("Fondo 360 (Skybox/Panoramic) con el cuarto de prueba (lo crea el menú TrazoVR)")]
    public Material materialFondo360;

    public static readonly string[] Nombres = { "Cuadrícula", "Blanco", "Realidad", "Cuarto 360", "Mi foto 360" };

    GameObject cuadricula;
    Camera camara;
    Material cieloOriginal, material360Propio;
    Texture2D textura360Propia;

    // Tu foto 360 (se copia aquí al elegirla).
    public static string RutaFoto360 => Path.Combine(Application.persistentDataPath, "Dibujos", "Fondo360.jpg");

    void Awake()
    {
        cieloOriginal = RenderSettings.skybox;
        cuadricula = new GameObject("Cuadricula");
        cuadricula.transform.SetParent(transform, false);
        var mf = cuadricula.AddComponent<MeshFilter>();
        var mr = cuadricula.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materialCuadricula;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mf.sharedMesh = CrearMallaCuadricula();
    }

    void Start()
    {
        // El piso de la cuadrícula está en y = 0: pedimos que el origen sea el piso real.
        if (OVRManager.instance != null)
            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;

        var rig = FindFirstObjectByType<OVRCameraRig>();
        camara = rig != null && rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : Camera.main;
        if (passthrough == null)
            passthrough = FindFirstObjectByType<OVRPassthroughLayer>(FindObjectsInactive.Include);
        PonerModo(PlayerPrefs.GetInt(ClaveFondo, 2));
    }

    // ¿Se puede usar ese fondo? (los 360 necesitan el material del menú TrazoVR; "Mi foto 360", una foto elegida)
    bool Disponible(int m)
    {
        if (m == 3)
            return materialFondo360 != null;
        if (m == 4)
            return materialFondo360 != null && File.Exists(RutaFoto360);
        return m >= 0 && m < 3;
    }

    public void SiguienteModo()
    {
        int n = modo;
        for (int i = 0; i < Nombres.Length; i++)
        {
            n = (n + 1) % Nombres.Length;
            if (Disponible(n))
                break;
        }
        PonerModo(n);
    }

    public void PonerModo(int nuevo)
    {
        modo = Mathf.Clamp(nuevo, 0, Nombres.Length - 1);
        Material cielo = null;
        if (modo == 4)
        {
            cielo = Material360Propio();
            if (cielo == null)
                modo = 3; // la foto no se pudo abrir: el cuarto de prueba
        }
        if (modo == 3)
        {
            cielo = materialFondo360;
            if (cielo == null)
                modo = 2;
        }
        PlayerPrefs.SetInt(ClaveFondo, modo);
        bool realidad = modo == 2;
        if (cuadricula != null)
            cuadricula.SetActive(modo == 0);
        if (passthrough != null)
            passthrough.enabled = realidad;
        // La foto 360 se pone como "cielo": queda muy lejos, a tu alrededor, y te sigue al caminar.
        RenderSettings.skybox = cielo != null ? cielo : cieloOriginal;
        if (camara != null)
        {
            camara.clearFlags = cielo != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            camara.backgroundColor = realidad ? new Color(0f, 0f, 0f, 0f) : fondo;
        }
    }

    // Usa como fondo la foto 360 que ya se copió a RutaFoto360 (la llama el buscador de imágenes).
    public void UsarFoto360()
    {
        LiberarFoto360();
        PonerModo(4);
    }

    Material Material360Propio()
    {
        if (material360Propio != null)
            return material360Propio;
        if (materialFondo360 == null || !File.Exists(RutaFoto360))
            return null;
        var tex = Leer360(RutaFoto360);
        if (tex == null)
            return null;
        textura360Propia = tex;
        material360Propio = new Material(materialFondo360);
        material360Propio.SetTexture("_MainTex", tex);
        return material360Propio;
    }

    // Lee la foto. Si es más grande que 4096 de ancho, se achica (más liviana para el visor).
    static Texture2D Leer360(string ruta)
    {
        Texture2D tex = null;
        try
        {
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(ruta)))
            {
                Destroy(tex);
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo abrir la foto 360: " + e.Message);
            if (tex != null)
                Destroy(tex);
            return null;
        }
        if (tex.width > 4096)
        {
            int ancho = 4096, alto = Mathf.Max(1, Mathf.RoundToInt(4096f * tex.height / tex.width));
            var rt = RenderTexture.GetTemporary(ancho, alto, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, rt);
            var anterior = RenderTexture.active;
            RenderTexture.active = rt;
            var chica = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            chica.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            chica.Apply(false, true);
            RenderTexture.active = anterior;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(tex);
            tex = chica;
        }
        else
        {
            tex.Apply(false, true); // ya no hace falta la copia en memoria normal
        }
        tex.wrapModeU = TextureWrapMode.Repeat;
        tex.wrapModeV = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    void LiberarFoto360()
    {
        if (RenderSettings.skybox == material360Propio && material360Propio != null)
            RenderSettings.skybox = cieloOriginal;
        if (material360Propio != null)
            Destroy(material360Propio);
        if (textura360Propia != null)
            Destroy(textura360Propia);
        material360Propio = null;
        textura360Propia = null;
    }

    void OnDestroy()
    {
        LiberarFoto360();
    }

    Mesh CrearMallaCuadricula()
    {
        var v = new List<Vector3>();
        var idx = new List<int>();
        int n = Mathf.Max(1, Mathf.RoundToInt(tamano / paso));
        float mitad = n * paso * 0.5f;
        for (int i = 0; i <= n; i++)
        {
            float c = -mitad + i * paso;
            idx.Add(v.Count); v.Add(new Vector3(c, 0.001f, -mitad));
            idx.Add(v.Count); v.Add(new Vector3(c, 0.001f, mitad));
            idx.Add(v.Count); v.Add(new Vector3(-mitad, 0.001f, c));
            idx.Add(v.Count); v.Add(new Vector3(mitad, 0.001f, c));
        }
        var m = new Mesh { name = "Cuadricula" };
        m.SetVertices(v);
        m.SetIndices(idx, MeshTopology.Lines, 0);
        m.RecalculateBounds();
        return m;
    }
}

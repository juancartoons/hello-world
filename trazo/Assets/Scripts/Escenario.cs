using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Fondo de TrazoVR (botón Fondo):
//  0 = blanco con piso de cuadrícula, 1 = todo blanco, 2 = realidad (passthrough: ves tu cuarto),
//  3, 4, 5... = FONDOS 360 EN 3D: primero los que trae la app (Plugins/Fondos: un cuarto, Roma de noche y
//  un amanecer) y después tus fotos 360 (las que elegiste en "Imagen +" con el botón "Fondo 360").
// Un fondo 360 es una "cáscara" a tu alrededor con PROFUNDIDAD: cada cosa de la foto queda a su distancia,
// así se ve en 3D con los dos ojos y cambia un poquito al mover la cabeza (como una foto espacial).
//  - Los de la app traen sus distancias calculadas con IA (archivos *_profundidad.bytes).
//  - Tus fotos usan "piso real": el piso de la foto queda en tu piso de verdad; lo demás, a unos metros.
// La cáscara queda fija donde estabas al elegir el fondo (con la foto mirando hacia donde mirabas).
// La PRIMERA vez la app empieza en Realidad (passthrough: dibujas sobre tu cuarto); después recuerda el último.
public class Escenario : MonoBehaviour
{
    [System.Serializable]
    public class Foto360
    {
        public string nombre;
        public Texture2D foto;
        [Tooltip("Distancias calculadas con IA (o vacío = piso real)")]
        public TextAsset profundidad;
    }

    const string ClaveFondo = "jcartoons_fondo";
    const int MaxFotosPropias = 8;
    const float AlturaCamara = 1.6f;   // a esta altura (más o menos) se toman las fotos 360
    const float RadioPisoReal = 5f;    // tus fotos: a qué distancia queda todo lo que no es piso (metros)
    const int Columnas = 256, Filas = 128;
    const float DistMin = 0.05f, DistMax = 60f; // en alturas de cámara (como el archivo de profundidad)

    public Material materialCuadricula;
    [Tooltip("0 cuadrícula, 1 blanco, 2 realidad (passthrough), 3 en adelante: fondos 360")]
    public int modo;
    public Color fondo = Color.white;
    [Tooltip("Tamaño del piso en metros")]
    public float tamano = 10f;
    [Tooltip("Distancia entre líneas de la cuadrícula en metros")]
    public float paso = 0.5f;
    [Tooltip("Capa de passthrough (la crea el menú TrazoVR)")]
    public OVRPassthroughLayer passthrough;
    [Tooltip("Material sin luz para los fondos 360 (lo crea el menú TrazoVR)")]
    public Material materialFondo360;
    [Tooltip("Los fondos 360 que vienen con la app (los llena el menú TrazoVR)")]
    public Foto360[] fondos360 = new Foto360[0];

    static readonly string[] NombresFijos = { "Cuadrícula", "Blanco", "Realidad" };
    const string NombreFotoPropia = "Mi foto 360";

    GameObject cuadricula, cascara;
    MeshFilter cascaraFiltro;
    Mesh mallaCascara;
    Material materialCascara;
    Texture2D texturaPropia;
    int texturaPropiaDe = -1;
    readonly List<string> fotosPropias = new List<string>();
    Camera camara;

    // Tus fotos 360 se copian aquí al elegirlas.
    public static string CarpetaFotos360 => Path.Combine(Application.persistentDataPath, "Dibujos", "Fondos360");
    static string RutaAntigua => Path.Combine(Application.persistentDataPath, "Dibujos", "Fondo360.jpg"); // v27

    int CantidadFijos => fondos360 != null ? fondos360.Length : 0;
    public int CantidadModos => 3 + CantidadFijos + fotosPropias.Count;
    public bool Hay360 => materialFondo360 != null;
    public string NombreModo => Nombre(modo);

    public string Nombre(int m)
    {
        if (m < 3)
            return NombresFijos[Mathf.Clamp(m, 0, 2)];
        m -= 3;
        if (m < CantidadFijos)
            return fondos360[m] != null && !string.IsNullOrEmpty(fondos360[m].nombre) ? fondos360[m].nombre : "Fondo 360";
        m -= CantidadFijos;
        return fotosPropias.Count > 1 ? NombreFotoPropia + " " + (m + 1) : NombreFotoPropia;
    }

    void Awake()
    {
        cuadricula = new GameObject("Cuadricula");
        cuadricula.transform.SetParent(transform, false);
        var mf = cuadricula.AddComponent<MeshFilter>();
        var mr = cuadricula.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materialCuadricula;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mf.sharedMesh = CrearMallaCuadricula();
        ListarFotosPropias();
    }

    void Start()
    {
        // El piso de la cuadrícula está en y = 0: pedimos que el origen sea el piso real.
        if (OVRManager.instance != null)
            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;

        var rig = FindFirstObjectByType<OVRCameraRig>();
        camara = rig != null && rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : Camera.main;
        if (camara != null && camara.farClipPlane < 150f)
            camara.farClipPlane = 150f; // los fondos 360 llegan hasta ~100 m
        if (passthrough == null)
            passthrough = FindFirstObjectByType<OVRPassthroughLayer>(FindObjectsInactive.Include);
        PonerModo(PlayerPrefs.GetInt(ClaveFondo, 2));
    }

    // ¿Se puede usar ese fondo?
    bool Disponible(int m)
    {
        if (m < 3)
            return m >= 0;
        if (materialFondo360 == null)
            return false;
        int k = m - 3;
        if (k < CantidadFijos)
            return fondos360[k] != null && fondos360[k].foto != null;
        return k - CantidadFijos < fotosPropias.Count;
    }

    public void SiguienteModo()
    {
        int n = modo;
        int total = CantidadModos;
        for (int i = 0; i < total; i++)
        {
            n = (n + 1) % total;
            if (Disponible(n))
                break;
        }
        PonerModo(n);
    }

    public void PonerModo(int nuevo)
    {
        modo = Mathf.Clamp(nuevo, 0, CantidadModos - 1);
        if (modo >= 3 && !(Disponible(modo) && ArmarCascara(modo)))
            modo = 2; // ese fondo 360 no se pudo abrir: realidad
        if (modo < 3 + CantidadFijos)
            LiberarPropia();
        PlayerPrefs.SetInt(ClaveFondo, modo);
        bool realidad = modo == 2;
        if (cuadricula != null)
            cuadricula.SetActive(modo == 0);
        if (cascara != null)
            cascara.SetActive(modo >= 3);
        if (passthrough != null)
            passthrough.enabled = realidad;
        if (camara != null)
        {
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = realidad ? new Color(0f, 0f, 0f, 0f) : fondo;
        }
    }

    // ---------- Tus fotos 360 ----------

    void ListarFotosPropias()
    {
        fotosPropias.Clear();
        try
        {
            // La foto de la v27 (una sola) pasa a la carpeta nueva.
            if (File.Exists(RutaAntigua))
            {
                Directory.CreateDirectory(CarpetaFotos360);
                string nueva = Path.Combine(CarpetaFotos360, "foto_00000000_000000.jpg");
                if (!File.Exists(nueva))
                    File.Move(RutaAntigua, nueva);
            }
            if (Directory.Exists(CarpetaFotos360))
                foreach (var f in Directory.GetFiles(CarpetaFotos360))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                        fotosPropias.Add(f);
                }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo leer la carpeta de fotos 360: " + e.Message);
        }
        fotosPropias.Sort(System.StringComparer.Ordinal);
    }

    // Dónde copiar una foto 360 nueva (la llama el buscador de imágenes).
    public static string NuevaRutaFoto360()
    {
        Directory.CreateDirectory(CarpetaFotos360);
        return Path.Combine(CarpetaFotos360, "foto_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg");
    }

    // La foto ya se copió a "ruta": se agrega a tus fondos y se pone. Se guardan las 8 más nuevas.
    public void UsarFotoNueva(string ruta)
    {
        LiberarPropia();
        ListarFotosPropias();
        while (fotosPropias.Count > MaxFotosPropias)
        {
            try
            {
                File.Delete(fotosPropias[0]);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TrazoVR: no se pudo borrar una foto 360 vieja: " + e.Message);
            }
            fotosPropias.RemoveAt(0);
        }
        int i = fotosPropias.IndexOf(ruta);
        PonerModo(i >= 0 ? 3 + CantidadFijos + i : 3 + CantidadFijos + fotosPropias.Count - 1);
    }

    void LiberarPropia()
    {
        if (texturaPropia != null)
        {
            if (materialCascara != null && materialCascara.mainTexture == texturaPropia)
                PonerTextura(materialCascara, null);
            Destroy(texturaPropia);
        }
        texturaPropia = null;
        texturaPropiaDe = -1;
    }

    // ---------- La cáscara 3D ----------

    bool ArmarCascara(int m)
    {
        if (materialFondo360 == null)
            return false;
        int k = m - 3;
        Texture2D foto;
        float[] dist = null;
        int dw = 0, dh = 0;
        if (k < CantidadFijos)
        {
            var f = fondos360[k];
            if (f == null || f.foto == null)
                return false;
            foto = f.foto;
            if (f.profundidad != null)
                dist = LeerProfundidad(f.profundidad.bytes, out dw, out dh);
        }
        else
        {
            int j = k - CantidadFijos;
            if (j < 0 || j >= fotosPropias.Count)
                return false;
            if (texturaPropia == null || texturaPropiaDe != j)
            {
                LiberarPropia();
                texturaPropia = Leer360(fotosPropias[j]);
                texturaPropiaDe = texturaPropia != null ? j : -1;
            }
            foto = texturaPropia;
            if (foto == null)
                return false;
        }

        if (cascara == null)
        {
            cascara = new GameObject("Fondo360");
            cascaraFiltro = cascara.AddComponent<MeshFilter>();
            var mr = cascara.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            materialCascara = new Material(materialFondo360);
            if (materialCascara.HasProperty("_Cull"))
                materialCascara.SetFloat("_Cull", 0f); // se ve desde adentro
            materialCascara.renderQueue = 2450;     // después de lo demás (lo que está delante lo tapa)
            mr.sharedMaterial = materialCascara;
            mallaCascara = new Mesh { name = "Fondo360" };
            cascaraFiltro.sharedMesh = mallaCascara;
        }
        PonerTextura(materialCascara, foto);
        ConstruirMalla(dist, dw, dh);
        Centrar();
        return true;
    }

    static void PonerTextura(Material m, Texture t)
    {
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
    }

    // La cáscara queda alrededor de tu cabeza (a la altura de la cámara de la foto, con su piso en tu piso)
    // y la foto mira hacia donde estás mirando.
    void Centrar()
    {
        Transform cab = camara != null ? camara.transform : (Camera.main != null ? Camera.main.transform : null);
        Vector3 pos = cab != null ? cab.position : Vector3.zero;
        Vector3 adelante = cab != null ? cab.forward : Vector3.forward;
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        cascara.transform.SetPositionAndRotation(new Vector3(pos.x, AlturaCamara, pos.z),
                                                 Quaternion.LookRotation(adelante.normalized, Vector3.up));
    }

    // Una esfera de Columnas x Filas: cada punto a la distancia de lo que se ve en esa dirección.
    void ConstruirMalla(float[] dist, int dw, int dh)
    {
        int ancho = Columnas + 1;
        var verts = new Vector3[ancho * (Filas + 1)];
        var uvs = new Vector2[verts.Length];
        for (int f = 0; f <= Filas; f++)
        {
            float v = f / (float)Filas;              // 0 abajo, 1 arriba
            float lat = (v - 0.5f) * Mathf.PI;
            float cl = Mathf.Cos(lat), sl = Mathf.Sin(lat);
            for (int c = 0; c <= Columnas; c++)
            {
                float u = c / (float)Columnas;
                float lon = (u - 0.5f) * 2f * Mathf.PI;
                var dir = new Vector3(cl * Mathf.Sin(lon), sl, cl * Mathf.Cos(lon));
                float d = dist != null ? Muestra(dist, dw, dh, u, v) * AlturaCamara : PisoReal(dir);
                int i = f * ancho + c;
                verts[i] = dir * d;
                uvs[i] = new Vector2(u, v);
            }
        }
        var tri = new int[Columnas * Filas * 6];
        int t = 0;
        for (int f = 0; f < Filas; f++)
            for (int c = 0; c < Columnas; c++)
            {
                int a = f * ancho + c, b = a + 1, arriba = a + ancho, arribaB = arriba + 1;
                tri[t++] = a; tri[t++] = arriba; tri[t++] = b;
                tri[t++] = b; tri[t++] = arriba; tri[t++] = arribaB;
            }
        mallaCascara.Clear();
        mallaCascara.vertices = verts;
        mallaCascara.uv = uvs;
        mallaCascara.triangles = tri;
        mallaCascara.RecalculateBounds();
    }

    // Tus fotos (sin profundidad): el piso de la foto en tu piso; lo demás a RadioPisoReal metros.
    static float PisoReal(Vector3 dir)
    {
        if (dir.y < -0.01f)
            return Mathf.Min(RadioPisoReal, AlturaCamara / -dir.y);
        return RadioPisoReal;
    }

    // Distancia (en alturas de cámara) en el punto u, v de la foto (bilineal).
    static float Muestra(float[] dist, int w, int h, float u, float v)
    {
        float x = u * w - 0.5f, y = (1f - v) * h - 0.5f;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        int x1 = ((x0 + 1) % w + w) % w;
        x0 = (x0 % w + w) % w;
        int y1 = Mathf.Clamp(y0 + 1, 0, h - 1);
        y0 = Mathf.Clamp(y0, 0, h - 1);
        float a = Mathf.Lerp(dist[y0 * w + x0], dist[y0 * w + x1], fx);
        float b = Mathf.Lerp(dist[y1 * w + x0], dist[y1 * w + x1], fx);
        return Mathf.Lerp(a, b, fy);
    }

    // Archivo de profundidad: "JCP1", ancho y alto (uint16), y un uint16 por pixel = log(distancia).
    static float[] LeerProfundidad(byte[] datos, out int w, out int h)
    {
        w = h = 0;
        if (datos == null || datos.Length < 8 || datos[0] != 'J' || datos[1] != 'C' || datos[2] != 'P' || datos[3] != '1')
            return null;
        w = datos[4] | (datos[5] << 8);
        h = datos[6] | (datos[7] << 8);
        if (w <= 0 || h <= 0 || datos.Length < 8 + w * h * 2)
            return null;
        var dist = new float[w * h];
        float lnMin = Mathf.Log(DistMin), lnMax = Mathf.Log(DistMax);
        for (int i = 0; i < dist.Length; i++)
        {
            int codigo = datos[8 + i * 2] | (datos[9 + i * 2] << 8);
            dist[i] = Mathf.Exp(lnMin + codigo / 65535f * (lnMax - lnMin));
        }
        return dist;
    }

    // Lee una foto tuya. Si es más grande que 4096 de ancho, se achica (más liviana para el visor).
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

    void OnDestroy()
    {
        LiberarPropia();
        if (cascara != null)
            Destroy(cascara);
        if (materialCascara != null)
            Destroy(materialCascara);
        if (mallaCascara != null)
            Destroy(mallaCascara);
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

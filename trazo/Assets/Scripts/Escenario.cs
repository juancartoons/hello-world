using System.Collections.Generic;
using UnityEngine;

// Fondo de TrazoVR. Tres modos:
//  0 = blanco con piso de cuadrícula, 1 = todo blanco, 2 = realidad (passthrough: ves tu cuarto).
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

    public static readonly string[] Nombres = { "Cuadrícula", "Blanco", "Realidad" };

    GameObject cuadricula;
    Camera camara;

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

    public void SiguienteModo()
    {
        PonerModo((modo + 1) % 3);
    }

    public void PonerModo(int nuevo)
    {
        modo = Mathf.Clamp(nuevo, 0, 2);
        PlayerPrefs.SetInt(ClaveFondo, modo);
        bool realidad = modo == 2;
        if (cuadricula != null)
            cuadricula.SetActive(modo == 0);
        if (passthrough != null)
            passthrough.enabled = realidad;
        if (camara != null)
        {
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = realidad ? new Color(0f, 0f, 0f, 0f) : fondo;
        }
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

using System.Collections.Generic;
using UnityEngine;

// Fondo blanco con un piso de cuadrícula suave (o todo blanco, opcional).
public class Escenario : MonoBehaviour
{
    public Material materialCuadricula;
    [Tooltip("Sí: todo blanco, sin cuadrícula")]
    public bool soloBlanco;
    public Color fondo = Color.white;
    [Tooltip("Tamaño del piso en metros")]
    public float tamano = 10f;
    [Tooltip("Distancia entre líneas de la cuadrícula en metros")]
    public float paso = 0.5f;

    GameObject cuadricula;

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
        PonerSoloBlanco(soloBlanco);
    }

    void Start()
    {
        // El piso de la cuadrícula está en y = 0: pedimos que el origen sea el piso real.
        if (OVRManager.instance != null)
            OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;

        var rig = FindFirstObjectByType<OVRCameraRig>();
        Camera cam = rig != null && rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = fondo;
        }
    }

    public void PonerSoloBlanco(bool valor)
    {
        soloBlanco = valor;
        if (cuadricula != null)
            cuadricula.SetActive(!valor);
    }

    public void AlternarFondo()
    {
        PonerSoloBlanco(!soloBlanco);
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

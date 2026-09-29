using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Menú de ayuda en el editor: FarmaciaVR > Crear puntos de teletransporte.
// Crea los discos del piso, sus materiales y el TeletransportePorPuntos,
// y apaga el teletransporte de Meta (ISDK) para que no haya dos sistemas a la vez.
public static class CrearPuntosTeletransporte
{
    const string carpetaMateriales = "Assets/Materials/FarmaciaVR";

    static readonly (string nombre, Vector3 posicion)[] puntos =
    {
        ("P1_Entrada",          new Vector3(0f,    0.01f, -3.5f)),
        ("P2_FrenteIzquierda",  new Vector3(-3.6f, 0.01f, -3.0f)),
        ("P3_FrenteDerecha",    new Vector3(3.6f,  0.01f, -3.0f)),
        ("P4_PasilloIzquierdo", new Vector3(-1.25f,0.01f, 0.5f)),
        ("P5_PasilloDerecho",   new Vector3(1.25f, 0.01f, 0.5f)),
        ("P6_ParedIzquierda",   new Vector3(-3.6f, 0.01f, 1.5f)),
        ("P7_ParedDerecha",     new Vector3(3.6f,  0.01f, 1.5f)),
        ("P8_FondoIzquierda",   new Vector3(-3.0f, 0.01f, 4.2f)),
        ("P9_FondoDerecha",     new Vector3(3.0f,  0.01f, 4.2f)),
    };

    [MenuItem("FarmaciaVR/Crear puntos de teletransporte")]
    static void Crear()
    {
        var existente = GameObject.Find("PuntosTeletransporte");
        if (existente != null &&
            !EditorUtility.DisplayDialog("Puntos de teletransporte", "Ya existen. ¿Reemplazarlos?", "Sí", "No"))
            return;
        if (existente != null)
            Undo.DestroyObjectImmediate(existente);

        var matDisco = ObtenerMaterial("Mat_PuntoTeleport", "Universal Render Pipeline/Lit", new Color(0.2f, 0.8f, 1f));
        var matLinea = ObtenerMaterial("Mat_LineaTeleport", "Sprites/Default", Color.white);

        var padre = new GameObject("PuntosTeletransporte");
        Undo.RegisterCreatedObjectUndo(padre, "Crear puntos de teletransporte");
        var tele = padre.AddComponent<TeletransportePorPuntos>();
        tele.materialLinea = matLinea;

        foreach (var (nombre, posicion) in puntos)
        {
            var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(disco, "Crear puntos de teletransporte");
            disco.name = nombre;
            disco.transform.SetParent(padre.transform, false);
            disco.transform.localPosition = posicion;
            disco.transform.localScale = new Vector3(0.6f, 0.01f, 0.6f);

            // El cilindro trae un collider de cápsula que sobresale; lo cambiamos por una caja plana.
            Object.DestroyImmediate(disco.GetComponent<Collider>());
            var caja = disco.AddComponent<BoxCollider>();
            caja.size = new Vector3(1f, 10f, 1f);
            caja.center = new Vector3(0f, 4f, 0f);

            disco.GetComponent<Renderer>().sharedMaterial = matDisco;
            disco.AddComponent<PuntoTeletransporte>();
        }

        // Apaga el teletransporte de Meta para que no compita con los puntos.
        var teleportMeta = GameObject.Find("ISDK_TeleportInteraction");
        if (teleportMeta != null)
        {
            Undo.RecordObject(teleportMeta, "Apagar teletransporte de Meta");
            teleportMeta.SetActive(false);
            Debug.Log("FarmaciaVR: apagué ISDK_TeleportInteraction (el teletransporte de Meta).");
        }

        EditorSceneManager.MarkSceneDirty(padre.scene);
        Selection.activeGameObject = padre;
        Debug.Log($"FarmaciaVR: creé {puntos.Length} puntos de teletransporte. Guarda con Ctrl + S.");
    }

    static Material ObtenerMaterial(string nombre, string shader, Color color)
    {
        string ruta = $"{carpetaMateriales}/{nombre}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat != null)
            return mat;

        Directory.CreateDirectory(carpetaMateriales);
        var s = Shader.Find(shader) ?? Shader.Find("Standard");
        mat = new Material(s);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        AssetDatabase.CreateAsset(mat, ruta);
        AssetDatabase.SaveAssets();
        return mat;
    }
}

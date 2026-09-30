#if UNITY_EDITOR
using UnityEditor;

// Reemplazado: ahora los discos de teletransporte los crea
// FarmaciaVR > ★ Aplicar estilo toon y escenografía.
public static class CrearPuntosTeletransporte
{
    [MenuItem("FarmaciaVR/Crear puntos de teletransporte (usa ★)")]
    static void Crear()
    {
        EditorUtility.DisplayDialog("Puntos de teletransporte",
            "Ahora los discos se crean solos con:\n\nFarmaciaVR > ★ Aplicar estilo toon y escenografía",
            "OK");
    }
}
#endif

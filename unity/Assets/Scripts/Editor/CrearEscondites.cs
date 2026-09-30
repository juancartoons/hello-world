#if UNITY_EDITOR
using UnityEditor;

// Reemplazado: ahora los escondites (en el piso y encima de productos) los crea
// FarmaciaVR > ★ Aplicar estilo toon y escenografía.
public static class CrearEscondites
{
    [MenuItem("FarmaciaVR/Crear escondites (usa ★)")]
    static void Crear()
    {
        EditorUtility.DisplayDialog("Escondites",
            "Ahora los escondites se crean solos con:\n\nFarmaciaVR > ★ Aplicar estilo toon y escenografía",
            "OK");
    }
}
#endif

using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menú de ayuda en el editor: FarmaciaVR > Arreglar teletransporte.
// Busca los TeleportInteractable de la escena y:
// - vacía "Target Point" (si está lleno, siempre te manda al mismo punto),
// - desactiva "Face Target Direction",
// - escribe en la Console un reporte con su configuración, para diagnosticar.
public static class ArreglarTeletransporte
{
    [MenuItem("FarmaciaVR/Arreglar teletransporte")]
    static void Arreglar()
    {
        var teleports = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(m => m != null && m.GetType().Name == "TeleportInteractable")
            .ToArray();

        var reporte = new StringBuilder();
        reporte.AppendLine($"FarmaciaVR: encontré {teleports.Length} TeleportInteractable.");

        foreach (var t in teleports)
        {
            reporte.AppendLine();
            reporte.AppendLine($"• {Ruta(t.transform)}  (activo: {t.isActiveAndEnabled})");
            reporte.AppendLine($"   Componentes: {string.Join(", ", t.GetComponents<Component>().Select(c => c.GetType().Name))}");

            var so = new SerializedObject(t);
            var it = so.GetIterator();
            bool entrar = true;
            while (it.NextVisible(entrar))
            {
                entrar = false;
                if (it.name != "m_Script")
                    reporte.AppendLine($"   {it.name} = {Valor(it)}");
            }

            var destino = so.FindProperty("_targetPoint");
            if (destino != null && destino.objectReferenceValue != null)
            {
                destino.objectReferenceValue = null;
                reporte.AppendLine("   >> ARREGLADO: _targetPoint vaciado");
            }

            var mirarDestino = so.FindProperty("_faceTargetDirection");
            if (mirarDestino != null && mirarDestino.boolValue)
            {
                mirarDestino.boolValue = false;
                reporte.AppendLine("   >> ARREGLADO: _faceTargetDirection desactivado");
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
        }

        Debug.Log(reporte.ToString());
        EditorUtility.DisplayDialog("Teletransporte",
            teleports.Length == 0
                ? "No encontré ningún TeleportInteractable en la escena."
                : "Listo. Revisa la Console (abajo) y mándale una captura del reporte a Claude.\n\nLuego guarda con Ctrl + S.",
            "OK");
    }

    static string Ruta(Transform t)
    {
        return t.parent == null ? t.name : Ruta(t.parent) + "/" + t.name;
    }

    static string Valor(SerializedProperty p)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                return p.objectReferenceValue == null
                    ? "None"
                    : $"{p.objectReferenceValue.name} ({p.objectReferenceValue.GetType().Name})";
            case SerializedPropertyType.Boolean: return p.boolValue.ToString();
            case SerializedPropertyType.Float: return p.floatValue.ToString("0.###");
            case SerializedPropertyType.Integer: return p.intValue.ToString();
            case SerializedPropertyType.String: return p.stringValue;
            case SerializedPropertyType.Enum:
                return p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length
                    ? p.enumDisplayNames[p.enumValueIndex]
                    : p.intValue.ToString();
            default: return p.propertyType.ToString();
        }
    }
}

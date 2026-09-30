#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Menú de ayuda en el editor: FarmaciaVR > Crear escondites.
// Crea el objeto "Escondites" con sus 6 puntos en la escena abierta.
public static class CrearEscondites
{
    static readonly (string nombre, Vector3 posicion)[] puntos =
    {
        ("E1", new Vector3(-2.5f, 2.0f, 2.5f)),   // encima de la góndola 1
        ("E2", new Vector3(1.25f, 0.2f, 2.8f)),   // piso, fondo del pasillo derecho
        ("E3", new Vector3(4.6f, 2.2f, -2.2f)),   // encima del estante derecho
        ("E4", new Vector3(1.0f, 0.2f, 4.55f)),   // detrás del mostrador
        ("E5", new Vector3(-4.6f, 2.2f, 3.0f)),   // encima del estante izquierdo
        ("E6", new Vector3(-1.25f, 0.2f, 1.8f)),  // piso, pasillo izquierdo
    };

    [MenuItem("FarmaciaVR/Crear escondites")]
    static void Crear()
    {
        var existente = GameObject.Find("Escondites");
        if (existente != null &&
            !EditorUtility.DisplayDialog("Escondites", "Ya existe un objeto 'Escondites'. ¿Reemplazarlo?", "Sí", "No"))
            return;
        if (existente != null)
            Undo.DestroyObjectImmediate(existente);

        var padre = new GameObject("Escondites");
        Undo.RegisterCreatedObjectUndo(padre, "Crear escondites");

        foreach (var (nombre, posicion) in puntos)
        {
            var punto = new GameObject(nombre);
            Undo.RegisterCreatedObjectUndo(punto, "Crear escondites");
            punto.transform.SetParent(padre.transform, false);
            punto.transform.localPosition = posicion;
        }

        Selection.activeGameObject = padre;
        EditorGUIUtility.PingObject(padre);
    }
}
#endif

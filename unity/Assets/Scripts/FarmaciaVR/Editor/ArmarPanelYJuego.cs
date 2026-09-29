using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menú de ayuda en el editor: FarmaciaVR > Armar panel del premio y JuegoManager.
// - Usa el Canvas "PanelPremio" (o lo crea) y le pone fondo, título y código.
// - Crea/conecta el "JuegoManager" con el personaje, los escondites y el panel.
public static class ArmarPanelYJuego
{
    const string nombrePanel = "PanelPremio";
    static readonly Color azul = new Color(0.08f, 0.36f, 0.75f, 0.95f);

    [MenuItem("FarmaciaVR/Armar panel del premio y JuegoManager")]
    static void Armar()
    {
        // TextMeshPro necesita sus recursos básicos para mostrar texto.
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            EditorUtility.DisplayDialog("Falta TextMeshPro",
                "Primero importa los recursos de TextMeshPro:\n\nWindow > TextMeshPro > Import TMP Essential Resources\n\nLuego vuelve a usar este menú.",
                "Entendido");
            return;
        }

        var panel = ObtenerOCrearPanel();
        var rt = panel.GetComponent<RectTransform>();

        BorrarHijo(rt, "Fondo");
        BorrarHijo(rt, "Titulo");
        BorrarHijo(rt, "Codigo");

        // Fondo azul que cubre todo el panel
        var fondo = CrearHijoUI("Fondo", rt);
        fondo.anchorMin = Vector2.zero;
        fondo.anchorMax = Vector2.one;
        fondo.offsetMin = Vector2.zero;
        fondo.offsetMax = Vector2.zero;
        Undo.AddComponent<Image>(fondo.gameObject).color = azul;

        var titulo = CrearTexto("Titulo", rt, new Vector2(0, 70), new Vector2(560, 150), 40, "¡Me encontraste!", FontStyles.Normal);
        var codigo = CrearTexto("Codigo", rt, new Vector2(0, -80), new Vector2(560, 120), 64, "FARMA-XXXX", FontStyles.Bold);

        ConectarJuegoManager(panel, titulo, codigo);

        EditorSceneManager.MarkSceneDirty(panel.scene);
        Selection.activeGameObject = panel;
        Debug.Log("FarmaciaVR: panel y JuegoManager listos. Guarda la escena con Ctrl + S.");
    }

    static GameObject ObtenerOCrearPanel()
    {
        var panel = GameObject.Find(nombrePanel);
        if (panel == null)
        {
            panel = new GameObject(nombrePanel, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(panel, "Crear panel");
        }

        var canvas = panel.GetComponent<Canvas>();
        if (canvas == null)
            canvas = Undo.AddComponent<Canvas>(panel);
        Undo.RecordObject(canvas, "Configurar panel");
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = panel.GetComponent<RectTransform>();
        Undo.RecordObject(rt, "Configurar panel");
        rt.sizeDelta = new Vector2(600, 350);
        rt.localScale = Vector3.one * 0.002f;
        rt.position = new Vector3(0f, 1.5f, -2.5f); // solo para verlo en el editor; en el juego se mueve solo
        return panel;
    }

    static RectTransform CrearHijoUI(string nombre, RectTransform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        return rt;
    }

    static TextMeshProUGUI CrearTexto(string nombre, RectTransform padre, Vector2 posicion, Vector2 tamano,
        float tamanoLetra, string texto, FontStyles estilo)
    {
        var rt = CrearHijoUI(nombre, padre);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        var tmp = Undo.AddComponent<TextMeshProUGUI>(rt.gameObject);
        tmp.text = texto;
        tmp.fontSize = tamanoLetra;
        tmp.fontStyle = estilo;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    static void BorrarHijo(Transform padre, string nombre)
    {
        var hijo = padre.Find(nombre);
        if (hijo != null)
            Undo.DestroyObjectImmediate(hijo.gameObject);
    }

    static void ConectarJuegoManager(GameObject panel, TMP_Text titulo, TMP_Text codigo)
    {
        var manager = Object.FindFirstObjectByType<JuegoManager>();
        if (manager == null)
        {
            var go = new GameObject("JuegoManager");
            Undo.RegisterCreatedObjectUndo(go, "Crear JuegoManager");
            manager = Undo.AddComponent<JuegoManager>(go);
        }

        Undo.RecordObject(manager, "Conectar JuegoManager");
        manager.personaje = Object.FindFirstObjectByType<PersonajeEncontrable>();
        var escondites = GameObject.Find("Escondites");
        manager.escondites = escondites != null ? escondites.transform : null;
        manager.panel = panel;
        manager.textoTitulo = titulo;
        manager.textoCodigo = codigo;
        EditorUtility.SetDirty(manager);

        if (manager.personaje == null)
            Debug.LogWarning("FarmaciaVR: no encontré ningún objeto con PersonajeEncontrable. Agrégalo a la esfera 'Personaje' y vuelve a usar el menú.");
        if (manager.escondites == null)
            Debug.LogWarning("FarmaciaVR: no encontré 'Escondites'. Usa primero FarmaciaVR > Crear escondites.");
    }
}

#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Menú de ayuda: TrazoVR > ★ Armar escena.
// Con un clic arma todo: materiales, fondo blanco con cuadrícula, dibujo, gestos de manos,
// caja de transformar y panel de la muñeca. Se puede usar varias veces sin duplicar nada.
public static class ArmarEscenaTrazo
{
    const string carpetaBase = "Assets/TrazoVR";
    const string carpeta = "Assets/TrazoVR/Materiales";
    const string nombreRaiz = "_TrazoVR";

    [MenuItem("TrazoVR/★ Armar escena")]
    static void Armar()
    {
        var rig = Object.FindFirstObjectByType<OVRCameraRig>();
        if (rig == null)
        {
            EditorUtility.DisplayDialog("TrazoVR",
                "No encontré el OVRCameraRig.\n\nAgrégalo primero: Meta > Tools > Building Blocks > Camera Rig y Hand Tracking.\n\nLuego vuelve a usar este menú.", "OK");
            return;
        }
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            EditorUtility.DisplayDialog("TrazoVR",
                "Falta TextMeshPro (para los textos del panel).\n\nWindow > TextMeshPro > Import TMP Essential Resources\n\nLuego vuelve a usar este menú.", "OK");
            return;
        }
        var shaderLinea = Shader.Find("TrazoVR/Linea");
        if (shaderLinea == null)
        {
            EditorUtility.DisplayDialog("TrazoVR",
                "No encontré el shader TrazoVR/Linea.\n\nCopia la carpeta Shaders dentro de Assets y espera a que Unity termine de cargar.", "OK");
            return;
        }
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null)
            unlit = Shader.Find("Unlit/Color");

        // ---------- Materiales ----------
        if (!AssetDatabase.IsValidFolder(carpetaBase))
            AssetDatabase.CreateFolder("Assets", "TrazoVR");
        if (!AssetDatabase.IsValidFolder(carpeta))
            AssetDatabase.CreateFolder(carpetaBase, "Materiales");

        var matLinea = Mat("Linea", shaderLinea, Color.black);
        if (matLinea.HasProperty("_ColorLuz"))
            matLinea.SetColor("_ColorLuz", new Color(0.45f, 0.45f, 0.45f));
        var matCuadricula = Mat("Cuadricula", unlit, new Color(0.84f, 0.85f, 0.87f));
        var matNodo = Mat("Nodo", unlit, new Color(0.15f, 0.45f, 1f));
        var matNodoActivo = Mat("NodoActivo", unlit, new Color(1f, 0.5f, 0.1f));
        var matCursor = Mat("Cursor", unlit, new Color(0.3f, 0.3f, 0.32f));
        var matCaja = Mat("Caja", unlit, new Color(0.15f, 0.45f, 1f));
        var matEsquina = Mat("Esquina", unlit, new Color(0.15f, 0.45f, 1f));
        var matEsquinaActiva = Mat("EsquinaActiva", unlit, new Color(1f, 0.5f, 0.1f));
        var matPanel = Mat("Panel", unlit, new Color(0.97f, 0.97f, 0.98f));
        var matBoton = Mat("Boton", unlit, new Color(0.86f, 0.87f, 0.9f));
        var matBotonMarcado = Mat("BotonMarcado", unlit, new Color(0.08f, 0.08f, 0.1f));
        AssetDatabase.SaveAssets();

        // ---------- Objetos ----------
        var viejo = GameObject.Find(nombreRaiz);
        if (viejo != null)
            Object.DestroyImmediate(viejo);
        var raiz = new GameObject(nombreRaiz);

        var goEscenario = new GameObject("Escenario");
        goEscenario.transform.SetParent(raiz.transform, false);
        var escenario = goEscenario.AddComponent<Escenario>();
        escenario.materialCuadricula = matCuadricula;

        var goDibujo = new GameObject("Dibujo");
        goDibujo.transform.SetParent(raiz.transform, false);
        var dibujo = goDibujo.AddComponent<Dibujo>();
        dibujo.materialLinea = matLinea;
        dibujo.escenario = escenario;

        var goControl = new GameObject("ControlManos");
        goControl.transform.SetParent(raiz.transform, false);
        var control = goControl.AddComponent<ControlManos>();
        var caja = goControl.AddComponent<CajaTransformar>();
        control.dibujo = dibujo;
        control.caja = caja;
        control.materialNodo = matNodo;
        control.materialNodoActivo = matNodoActivo;
        control.materialCursor = matCursor;
        caja.dibujo = dibujo;
        caja.materialCaja = matCaja;
        caja.materialEsquina = matEsquina;
        caja.materialEsquinaActiva = matEsquinaActiva;

        CrearPanel(raiz.transform, control, dibujo, escenario, matPanel, matBoton, matBotonMarcado);

        // ---------- Cámara: fondo blanco ----------
        var camara = rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : null;
        if (camara != null)
        {
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.white;
            camara.nearClipPlane = 0.03f;
            EditorUtility.SetDirty(camara);
        }
        // Apaga otras cámaras (por ejemplo la "Main Camera" de la plantilla).
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (c.transform.IsChildOf(rig.transform))
                continue;
            c.gameObject.SetActive(false);
            Debug.Log("TrazoVR: apagué la cámara extra '" + c.name + "'.");
        }

        // ---------- El origen es el piso (para que la cuadrícula quede en el suelo) ----------
        var manager = Object.FindFirstObjectByType<OVRManager>();
        if (manager != null)
        {
            var so = new SerializedObject(manager);
            var prop = so.FindProperty("_trackingOriginType");
            if (prop != null && prop.propertyType == SerializedPropertyType.Enum)
            {
                int idx = System.Array.IndexOf(prop.enumNames, "FloorLevel");
                if (idx >= 0)
                {
                    prop.enumValueIndex = idx;
                    so.ApplyModifiedProperties();
                }
            }
        }

        // ---------- Antialiasing 4x (líneas más limpias) ----------
        var assets = new List<UnityEngine.Rendering.RenderPipelineAsset>();
        if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
            assets.Add(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline);
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            var a = QualitySettings.GetRenderPipelineAssetAt(i);
            if (a != null)
                assets.Add(a);
        }
        foreach (var a in assets)
        {
            var urp = a as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (urp == null)
                continue;
            urp.msaaSampleCount = 4;
            EditorUtility.SetDirty(urp);
        }
        AssetDatabase.SaveAssets();

        // ---------- Escena en la lista de Build ----------
        var escena = raiz.scene;
        bool escenaGuardada = !string.IsNullOrEmpty(escena.path);
        if (escenaGuardada)
        {
            var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!lista.Exists(s => s.path == escena.path))
            {
                lista.Insert(0, new EditorBuildSettingsScene(escena.path, true));
                EditorBuildSettings.scenes = lista.ToArray();
            }
        }

        bool hayManos = Object.FindObjectsByType<OVRHand>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;

        EditorSceneManager.MarkSceneDirty(escena);
        Selection.activeGameObject = raiz;

        string aviso = "¡Listo! Guarda la escena con Ctrl + S y luego haz Build And Run.";
        if (!hayManos)
            aviso += "\n\nOJO: no encontré manos (OVRHand). Agrega el Building Block 'Hand Tracking'.";
        if (!escenaGuardada)
            aviso += "\n\nOJO: la escena aún no tiene nombre. Guárdala con Ctrl + S y vuelve a usar este menú para agregarla al Build.";
        Debug.Log("TrazoVR: " + aviso);
        EditorUtility.DisplayDialog("TrazoVR", aviso, "OK");
    }

    // ---------- Panel de la muñeca ----------

    static void CrearPanel(Transform raiz, ControlManos control, Dibujo dibujo, Escenario escenario,
                           Material matPanel, Material matBoton, Material matBotonMarcado)
    {
        var goPanel = new GameObject("PanelMuneca");
        goPanel.transform.SetParent(raiz, false);
        goPanel.transform.position = new Vector3(0f, 1.2f, 0.4f);
        var panel = goPanel.AddComponent<PanelMuneca>();
        panel.control = control;
        panel.dibujo = dibujo;
        panel.escenario = escenario;

        var contenido = new GameObject("Contenido");
        contenido.transform.SetParent(goPanel.transform, false);
        panel.contenido = contenido;

        var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "Fondo";
        Object.DestroyImmediate(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(contenido.transform, false);
        fondo.transform.localPosition = new Vector3(0f, 0f, 0.004f);
        fondo.transform.localScale = new Vector3(0.105f, 0.15f, 1f);
        SinSombras(fondo.GetComponent<Renderer>(), matPanel);

        Texto(contenido.transform, "TrazoVR", new Vector3(0f, 0.062f, -0.001f), new Vector2(0.09f, 0.014f), Color.black);
        panel.textoEstado = Texto(contenido.transform, "", new Vector3(0f, -0.062f, -0.001f), new Vector2(0.098f, 0.02f), new Color(0.2f, 0.2f, 0.25f));

        string[] nombres = { "Deshacer", "Borrar", "Guardar", "Cargar", "Cinta", "Tubo", "Por línea: No", "Fondo: cuadrícula" };
        var botones = new BotonTocable[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            float x = i % 2 == 0 ? -0.025f : 0.025f;
            float y = 0.036f - (i / 2) * 0.026f;
            botones[i] = Boton(contenido.transform, nombres[i], new Vector3(x, y, 0f), matBoton, matBotonMarcado);
        }
        panel.btnDeshacer = botones[0];
        panel.btnBorrar = botones[1];
        panel.btnGuardar = botones[2];
        panel.btnCargar = botones[3];
        panel.btnCinta = botones[4];
        panel.btnTubo = botones[5];
        panel.btnPorLinea = botones[6];
        panel.btnFondo = botones[7];
    }

    static BotonTocable Boton(Transform padre, string texto, Vector3 posicion, Material normal, Material marcado)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Boton_" + texto;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localScale = new Vector3(0.046f, 0.022f, 0.008f);
        SinSombras(go.GetComponent<Renderer>(), normal);
        var b = go.AddComponent<BotonTocable>();
        b.materialNormal = normal;
        b.materialMarcado = marcado;
        b.etiqueta = Texto(padre, texto, posicion + new Vector3(0f, 0f, -0.0046f), new Vector2(0.042f, 0.018f), Color.black);
        return b;
    }

    static TextMeshPro Texto(Transform padre, string texto, Vector3 posicion, Vector2 tamano, Color color)
    {
        var go = new GameObject("Texto_" + texto, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = texto;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.01f;
        tmp.fontSizeMax = 0.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = tamano;
        go.transform.localPosition = posicion;
        go.transform.localRotation = Quaternion.identity;
        return tmp;
    }

    static void SinSombras(Renderer r, Material m)
    {
        if (r == null)
            return;
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static Material Mat(string nombre, Shader shader, Color color)
    {
        string ruta = carpeta + "/" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, ruta);
        }
        else
        {
            m.shader = shader;
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        EditorUtility.SetDirty(m);
        return m;
    }
}
#endif

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
        var shaderRelleno = Shader.Find("TrazoVR/Relleno");
        if (shaderRelleno == null)
        {
            EditorUtility.DisplayDialog("TrazoVR",
                "No encontré el shader TrazoVR/Relleno.\n\nCopia otra vez la carpeta Shaders dentro de Assets (elige Reemplazar) y espera a que Unity cargue.", "OK");
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
        var matRelleno = Mat("Relleno", shaderRelleno, Color.white);
        var matAsa = Mat("Asa", unlit, new Color(0.2f, 0.8f, 0.3f));
        var matIman = Mat("Iman", unlit, new Color(0.1f, 0.95f, 0.35f));
        var matCaja = Mat("Caja", unlit, new Color(0.65f, 0.78f, 1f));
        var matGuia = Mat("Guia", unlit, new Color(0.7f, 0.8f, 0.95f));
        var matPanel = Mat("Panel", unlit, new Color(0.97f, 0.97f, 0.98f));
        var matBoton = Mat("Boton", unlit, new Color(0.86f, 0.87f, 0.9f));
        var matBotonMarcado = Mat("BotonMarcado", unlit, new Color(0.08f, 0.08f, 0.1f));
        var matBorrado = Mat("Borrado", unlit, new Color(0.95f, 0.15f, 0.15f));
        var matCursorBorrar = Mat("CursorBorrar", unlit, new Color(0.95f, 0.2f, 0.2f));
        var matDestelloMano = Mat("DestelloMano", unlit, new Color(0.35f, 0.7f, 1f));
        var matBorrarMano = Mat("BorrarMano", unlit, new Color(1f, 0.35f, 0.35f));
        var matSeleccion = Mat("LineaSeleccion", shaderLinea, new Color(0.1f, 0.35f, 0.95f));
        if (matSeleccion.HasProperty("_ColorLuz"))
            matSeleccion.SetColor("_ColorLuz", new Color(0.5f, 0.7f, 1f));
        var matClave = Mat("Clave", unlit, new Color(1f, 0.55f, 0.1f));
        var matCabezal = Mat("Cabezal", unlit, new Color(0.9f, 0.15f, 0.15f));
        var matBarra = Mat("Barra", unlit, new Color(0.55f, 0.57f, 0.62f));
        // Los círculos de los nodos se ven por ambos lados.
        foreach (var m in new[] { matNodo, matNodoActivo, matAsa, matIman })
            if (m.HasProperty("_Cull"))
                m.SetFloat("_Cull", 0f);
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
        var animacion = goDibujo.AddComponent<Animacion>();
        animacion.dibujo = dibujo;
        dibujo.animacion = animacion;
        dibujo.materialBorrado = matBorrado;
        dibujo.materialSeleccion = matSeleccion;
        dibujo.materialLinea = matLinea;
        dibujo.materialRelleno = matRelleno;
        dibujo.materialGuia = matGuia;
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
        control.materialAsa = matAsa;
        control.materialIman = matIman;
        control.materialCursorBorrar = matCursorBorrar;
        control.materialDestelloMano = matDestelloMano;
        control.materialBorrarMano = matBorrarMano;
        caja.dibujo = dibujo;
        caja.materialCaja = matCaja;

        var panel = CrearPanel(raiz.transform, control, dibujo, escenario, matPanel, matBoton, matBotonMarcado);
        CrearPanelArriba(raiz.transform, control, dibujo, animacion, matPanel, matBoton, matBotonMarcado, matClave, matCabezal, matBarra);
        var aviso = Texto(raiz.transform, "", new Vector3(0f, 1.4f, 0.6f), new Vector2(0.4f, 0.05f), new Color(0.1f, 0.1f, 0.12f));
        aviso.gameObject.name = "Aviso";
        aviso.fontSizeMax = 0.3f;
        panel.textoAviso = aviso;

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
                    prop.enumValueIndex = idx;
            }
            // Passthrough (ver tu cuarto real) disponible en el modo de fondo "Realidad".
            var pt = so.FindProperty("isInsightPassthroughEnabled");
            if (pt != null && pt.propertyType == SerializedPropertyType.Boolean)
                pt.boolValue = true;
            so.ApplyModifiedProperties();
        }

        // ---------- Capa de passthrough (apagada; el botón "Fondo" la enciende) ----------
        var capa = Object.FindFirstObjectByType<OVRPassthroughLayer>(FindObjectsInactive.Include);
        if (capa == null)
            capa = rig.gameObject.AddComponent<OVRPassthroughLayer>();
        var soCapa = new SerializedObject(capa);
        PonerEnum(soCapa.FindProperty("overlayType"), "Underlay");
        soCapa.ApplyModifiedProperties();
        capa.enabled = false;
        EditorUtility.SetDirty(capa);
        escenario.passthrough = capa;

        bool passthroughConfigurado = false;
        foreach (var guid in AssetDatabase.FindAssets("t:OVRProjectConfig"))
        {
            var cfg = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (cfg == null)
                continue;
            var soCfg = new SerializedObject(cfg);
            var soporte = soCfg.FindProperty("insightPassthroughSupport");
            if (soporte == null)
                soporte = soCfg.FindProperty("_insightPassthroughSupport");
            var actual = soporte != null && soporte.propertyType == SerializedPropertyType.Enum && soporte.enumValueIndex >= 0
                ? soporte.enumNames[soporte.enumValueIndex] : "";
            if (actual == "Required" || PonerEnum(soporte, "Supported"))
            {
                soCfg.ApplyModifiedProperties();
                EditorUtility.SetDirty(cfg);
                passthroughConfigurado = true;
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

        string avisoFinal = "¡Listo! Guarda la escena con Ctrl + S y luego haz Build And Run.";
        if (!hayManos)
            avisoFinal += "\n\nOJO: no encontré manos (OVRHand). Agrega el Building Block 'Hand Tracking'.";
        if (!passthroughConfigurado)
            avisoFinal += "\n\nPara el fondo 'Realidad': Edit > Project Settings > Meta XR > Passthrough Support = Supported (o agrega el Building Block 'Passthrough').";
        if (!escenaGuardada)
            avisoFinal += "\n\nOJO: la escena aún no tiene nombre. Guárdala con Ctrl + S y vuelve a usar este menú para agregarla al Build.";
        Debug.Log("TrazoVR: " + avisoFinal);
        EditorUtility.DisplayDialog("TrazoVR", avisoFinal, "OK");
    }

    // ---------- Panel de la muñeca ----------

    static PanelMuneca CrearPanel(Transform raiz, ControlManos control, Dibujo dibujo, Escenario escenario,
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

        // Fila 1: Línea / Por línea · Fila 2: Plano / Fondo · Fila 3: Guardar / Cargar · Fila 4: Borrar todo.
        // Fila 1: Plano / Fondo · Fila 2: Guardar / Cargar · Fila 3: Borrar todo.
        string[] nombres = { "Libre (3D)", "Fondo: Cuadrícula", "Guardar", "Cargar", "Borrar todo" };
        var botones = new BotonTocable[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            float x = i == nombres.Length - 1 ? 0f : (i % 2 == 0 ? -0.025f : 0.025f);
            float y = 0.036f - (i / 2) * 0.026f;
            botones[i] = Boton(contenido.transform, nombres[i], new Vector3(x, y, 0f), matBoton, matBotonMarcado);
        }
        panel.btnPlano = botones[0];
        panel.btnFondo = botones[1];
        panel.btnGuardar = botones[2];
        panel.btnCargar = botones[3];
        panel.btnBorrar = botones[4];
        return panel;
    }

    // ---------- Panel de arriba: animación y capas ----------

    static void CrearPanelArriba(Transform raiz, ControlManos control, Dibujo dibujo, Animacion animacion,
                                 Material matPanel, Material matBoton, Material matBotonMarcado,
                                 Material matClave, Material matCabezal, Material matBarra)
    {
        var go = new GameObject("PanelArriba");
        go.transform.SetParent(raiz, false);
        go.transform.position = new Vector3(0f, 1.9f, 0.5f);
        var panel = go.AddComponent<PanelArriba>();
        panel.control = control;
        panel.dibujo = dibujo;
        panel.animacion = animacion;
        panel.materialClave = matClave;

        var contenido = new GameObject("Contenido");
        contenido.transform.SetParent(go.transform, false);
        panel.contenido = contenido;

        var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "Fondo";
        Object.DestroyImmediate(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(contenido.transform, false);
        fondo.transform.localPosition = new Vector3(0f, 0f, 0.006f);
        fondo.transform.localScale = new Vector3(0.5f, 0.24f, 1f);
        SinSombras(fondo.GetComponent<Renderer>(), matPanel);

        panel.textoFotograma = Texto(contenido.transform, "Fotograma 1 / 200", new Vector3(0f, 0.097f, -0.001f), new Vector2(0.3f, 0.022f), Color.black);

        // Línea de tiempo
        var barra = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barra.name = "BarraTiempo";
        Object.DestroyImmediate(barra.GetComponent<Collider>());
        barra.transform.SetParent(contenido.transform, false);
        barra.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        barra.transform.localScale = new Vector3(0.44f, 0.02f, 0.008f);
        SinSombras(barra.GetComponent<Renderer>(), matBarra);
        panel.barra = barra.transform;

        var cabezal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabezal.name = "Cabezal";
        Object.DestroyImmediate(cabezal.GetComponent<Collider>());
        cabezal.transform.SetParent(contenido.transform, false);
        cabezal.transform.localPosition = new Vector3(-0.22f, 0.06f, -0.006f);
        cabezal.transform.localScale = new Vector3(0.004f, 0.036f, 0.004f);
        SinSombras(cabezal.GetComponent<Renderer>(), matCabezal);
        panel.cabezal = cabezal.transform;

        // Controles de la animación
        string[] controles = { "Inicio", "<", "Play", ">", "+ Clave", "- Clave", "12 fps" };
        var b = new BotonTocable[controles.Length];
        for (int i = 0; i < controles.Length; i++)
        {
            float x = -0.192f + i * 0.064f;
            b[i] = Boton(contenido.transform, controles[i], new Vector3(x, 0.02f, 0f), matBoton, matBotonMarcado);
        }
        panel.btnInicio = b[0];
        panel.btnAnterior = b[1];
        panel.btnPlay = b[2];
        panel.btnSiguiente = b[3];
        panel.btnClave = b[4];
        panel.btnQuitarClave = b[5];
        panel.btnFps = b[6];

        // Capas
        panel.btnCapas = new BotonTocable[Dibujo.NumeroDeCapas];
        panel.btnVer = new BotonTocable[Dibujo.NumeroDeCapas];
        for (int i = 0; i < Dibujo.NumeroDeCapas; i++)
        {
            float x = -0.165f + i * 0.11f;
            panel.btnCapas[i] = Boton(contenido.transform, "Capa " + (i + 1), new Vector3(x, -0.03f, 0f), matBoton, matBotonMarcado);
            panel.btnVer[i] = Boton(contenido.transform, "Ver", new Vector3(x, -0.058f, 0f), matBoton, matBotonMarcado);
        }
        Texto(contenido.transform, "Toca la barra = ir a un fotograma · pellizca una clave = moverla · si editas, se guarda una clave", new Vector3(0f, -0.095f, -0.001f), new Vector2(0.46f, 0.018f), new Color(0.3f, 0.3f, 0.35f));
    }

    static bool PonerEnum(SerializedProperty prop, string nombre)
    {
        if (prop == null || prop.propertyType != SerializedPropertyType.Enum)
            return false;
        int idx = System.Array.IndexOf(prop.enumNames, nombre);
        if (idx < 0)
            return false;
        prop.enumValueIndex = idx;
        return true;
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

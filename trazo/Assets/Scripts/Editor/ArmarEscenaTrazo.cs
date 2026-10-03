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
    const int CapaDibujo = 29;

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
        var shaderInvisible = Shader.Find("TrazoVR/Invisible");
        if (shaderInvisible == null)
        {
            EditorUtility.DisplayDialog("TrazoVR",
                "No encontré el shader TrazoVR/Invisible.\n\nCopia otra vez la carpeta Shaders dentro de Assets (elige Reemplazar) y espera a que Unity cargue.", "OK");
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
        var matAsaPanel = Mat("AsaPanel", unlit, new Color(0.3f, 0.55f, 0.95f));
        var matImagen = Mat("Imagen", unlit, Color.white);
        var matImagenTransparente = Mat("ImagenTransparente", unlit, new Color(1f, 1f, 1f, 0.5f));
        Transparente(matImagenTransparente);
        var matManoFantasma = Mat("ManoFantasma", shaderLinea, new Color(0.6f, 0.6f, 0.63f));
        if (matManoFantasma.HasProperty("_ColorLuz"))
            matManoFantasma.SetColor("_ColorLuz", new Color(0.8f, 0.8f, 0.82f));
        var matInvisible = Mat("Invisible", shaderInvisible, Color.clear);
        var matBocetoGris = Mat("LineaBocetoGris", shaderLinea, new Color(0.62f, 0.62f, 0.65f));
        var matBocetoAzul = Mat("LineaBocetoAzul", shaderLinea, new Color(0.45f, 0.62f, 1f));
        var matGoma = Mat("BorradorGoma", unlit, new Color(1f, 0.62f, 0.7f));
        var matFunda = Mat("BorradorFunda", unlit, new Color(0.2f, 0.4f, 0.85f));
        var matFlecha = Mat("Flecha", unlit, new Color(0.15f, 0.2f, 0.35f));
        var matDianaRoja = Mat("DianaDeshacer", unlit, new Color(0.9f, 0.15f, 0.15f));
        var matDianaVerde = Mat("DianaRehacer", unlit, new Color(0.15f, 0.7f, 0.3f));
        var matBlanco = Mat("Blanco", unlit, Color.white);
        var matCandado = Mat("Candado", unlit, new Color(0.95f, 0.6f, 0.1f));
        var matDial = Mat("Dial", unlit, new Color(0.2f, 0.5f, 1f));
        var matIndicador = Mat("IndicadorTitere", unlit, new Color(1f, 0.55f, 0.1f));
        // Lápiz de boceto sobre hoja (si faltan los shaders, el lápiz no se activa pero todo lo demás sí).
        var shaderSello = Shader.Find("TrazoVR/SelloLapiz");
        var shaderHoja = Shader.Find("TrazoVR/HojaLapiz");
        Material matSello = shaderSello != null ? Mat("SelloLapiz", shaderSello, Color.white) : null;
        Material matHoja = shaderHoja != null ? Mat("HojaLapiz", shaderHoja, new Color(0.32f, 0.32f, 0.36f)) : null;
        if (matSello == null || matHoja == null)
            Debug.LogWarning("TrazoVR: no encontré los shaders del lápiz de boceto (copia otra vez la carpeta Shaders).");
        // Los círculos de los nodos (y las imágenes, dianas, flecha y dial) se ven por ambos lados.
        foreach (var m in new[] { matNodo, matNodoActivo, matAsa, matIman, matImagen, matImagenTransparente, matFlecha, matDianaRoja, matDianaVerde, matBlanco, matDial, matIndicador })
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
        // Capa propia del dibujo: las fotos y los videos solo ven esta capa (sin paneles ni imágenes).
        goDibujo.layer = CapaDibujo;
        var dibujo = goDibujo.AddComponent<Dibujo>();
        var animacion = goDibujo.AddComponent<Animacion>();
        dibujo.temblor = goDibujo.AddComponent<Temblor>();
        dibujo.materialBocetoGris = matBocetoGris;
        dibujo.materialBocetoAzul = matBocetoAzul;
        var figuras = goDibujo.AddComponent<Figuras>();
        figuras.dibujo = dibujo;
        figuras.materialLinea = matLinea;
        figuras.materialSeleccion = matSeleccion;
        figuras.materialRelleno = matRelleno;
        figuras.materialNodo = matNodo;
        figuras.materialNodoActivo = matNodoActivo;
        dibujo.figuras = figuras;
        var titere = goDibujo.AddComponent<Titere>();
        titere.dibujo = dibujo;
        titere.animacion = animacion;
        dibujo.titere = titere;
        titere.materialIndicador = matIndicador;
        if (matSello != null && matHoja != null)
        {
            var hojas = goDibujo.AddComponent<HojasLapiz>();
            hojas.dibujo = dibujo;
            hojas.materialSello = matSello;
            hojas.materialHoja = matHoja;
            dibujo.hojas = hojas;
        }
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
        titere.control = control;
        control.materialInvisible = matInvisible;
        var simbolos = goControl.AddComponent<SimbolosMano>();
        simbolos.materialGoma = matGoma;
        simbolos.materialFunda = matFunda;
        simbolos.materialFlecha = matFlecha;
        simbolos.materialDianaDeshacer = matDianaRoja;
        simbolos.materialDianaRehacer = matDianaVerde;
        simbolos.materialBlanco = matBlanco;
        simbolos.materialCandado = matCandado;
        simbolos.materialDial = matDial;
        control.simbolos = simbolos;
        control.figuras = figuras;

        // Imágenes de referencia
        var goRef = new GameObject("Referencias");
        goRef.transform.SetParent(raiz.transform, false);
        var referencias = goRef.AddComponent<Referencias>();
        referencias.dibujo = dibujo;
        referencias.materialImagen = matImagen;
        referencias.materialBoton = matBoton;
        referencias.materialImagenTransparente = matImagenTransparente;
        control.referencias = referencias;

        // Bocas automáticas (lipsync) + el audio que suena con la animación
        var goLip = new GameObject("Lipsync");
        goLip.transform.SetParent(raiz.transform, false);
        var fuente = goLip.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        var lipsync = goLip.AddComponent<Lipsync>();
        lipsync.dibujo = dibujo;
        lipsync.animacion = animacion;
        lipsync.fuente = fuente;
        animacion.fuenteAudio = fuente;
        dibujo.lipsync = lipsync;

        // Videos (animación y proceso)
        var goExp = new GameObject("Exportador");
        goExp.transform.SetParent(raiz.transform, false);
        var grabador = goExp.AddComponent<GrabadorProceso>();
        grabador.dibujo = dibujo;
        grabador.control = control;
        var exportador = goExp.AddComponent<ExportadorVideo>();
        exportador.dibujo = dibujo;
        exportador.animacion = animacion;
        exportador.lipsync = lipsync;
        exportador.grabador = grabador;
        exportador.control = control;
        exportador.materialManoFantasma = matManoFantasma;
        // Video: manos con volumen y conversión rápida en la tarjeta gráfica (si faltan los shaders, modo de antes).
        var shaderManoVideo = Shader.Find("TrazoVR/ManoVideo");
        if (shaderManoVideo != null)
            exportador.materialManoVideo = Mat("ManoVideo", shaderManoVideo, new Color(0.82f, 0.78f, 0.75f));
        var shaderNv12 = Shader.Find("TrazoVR/Nv12");
        if (shaderNv12 != null)
            exportador.materialNv12 = Mat("Nv12", shaderNv12, Color.white);

        var panel = CrearPanel(raiz.transform, control, dibujo, escenario, matPanel, matBoton, matBotonMarcado);
        panel.figuras = figuras;
        var arriba = CrearPanelArriba(raiz.transform, control, dibujo, animacion, escenario, matPanel, matBoton, matBotonMarcado, matClave, matCabezal, matBarra, matAsaPanel);
        arriba.lipsync = lipsync;
        arriba.referencias = referencias;
        arriba.grabador = grabador;
        arriba.exportador = exportador;
        arriba.titere = titere;
        control.panelArriba = arriba;
        var aviso = Texto(raiz.transform, "", new Vector3(0f, 1.4f, 0.6f), new Vector2(0.4f, 0.05f), new Color(0.1f, 0.1f, 0.12f));
        aviso.gameObject.name = "Aviso";
        aviso.fontSizeMax = 0.3f;
        panel.textoAviso = aviso;

        // Etiqueta que flota sobre la mano con el nombre del gesto (para aprender).
        var ayuda = Texto(raiz.transform, "", new Vector3(0f, 1.3f, 0.5f), new Vector2(0.22f, 0.05f), new Color(0.1f, 0.25f, 0.6f));
        ayuda.gameObject.name = "AyudaGesto";
        ayuda.fontSizeMax = 0.25f;
        control.textoGesto = ayuda;

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
            // Seguimiento de manos más frecuente (el lápiz va más pegado al dedo).
            if (PonerEnum(soCfg.FindProperty("handTrackingFrequency"), "HIGH")
                || PonerEnum(soCfg.FindProperty("_handTrackingFrequency"), "HIGH"))
            {
                soCfg.ApplyModifiedProperties();
                EditorUtility.SetDirty(cfg);
            }
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
        fondo.transform.localPosition = new Vector3(0f, -0.01f, 0.004f);
        fondo.transform.localScale = new Vector3(0.105f, 0.23f, 1f);
        SinSombras(fondo.GetComponent<Renderer>(), matPanel);

        Texto(contenido.transform, "TrazoVR", new Vector3(0f, 0.09f, -0.001f), new Vector2(0.09f, 0.014f), Color.black);
        panel.textoEstado = Texto(contenido.transform, "", new Vector3(0f, -0.105f, -0.001f), new Vector2(0.098f, 0.02f), new Color(0.2f, 0.2f, 0.25f));

        // Dos columnas: Plano / Fondo · Guardar / Cargar · Borrar todo / Imán · Esfera / Cubo · Cilindro / A líneas · Quitar figura
        string[] nombres = { "Libre (3D)", "Fondo: Cuadrícula", "Guardar", "Cargar", "Borrar todo", "Esfera", "Cubo", "Cilindro", "A líneas", "Quitar figura", "Imán: Sí" };
        Vector2[] lugares =
        {
            new Vector2(-0.025f, 0.064f), new Vector2(0.025f, 0.064f),
            new Vector2(-0.025f, 0.038f), new Vector2(0.025f, 0.038f),
            new Vector2(-0.025f, 0.012f),
            new Vector2(-0.025f, -0.022f), new Vector2(0.025f, -0.022f),
            new Vector2(-0.025f, -0.048f), new Vector2(0.025f, -0.048f),
            new Vector2(0f, -0.074f),
            new Vector2(0.025f, 0.012f),
        };
        var botones = new BotonTocable[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
            botones[i] = Boton(contenido.transform, nombres[i], new Vector3(lugares[i].x, lugares[i].y, 0f), matBoton, matBotonMarcado);
        panel.btnPlano = botones[0];
        panel.btnFondo = botones[1];
        panel.btnGuardar = botones[2];
        panel.btnCargar = botones[3];
        panel.btnBorrar = botones[4];
        panel.btnEsfera = botones[5];
        panel.btnCubo = botones[6];
        panel.btnCilindro = botones[7];
        panel.btnALineas = botones[8];
        panel.btnQuitarFigura = botones[9];
        panel.btnIman = botones[10];
        Texto(contenido.transform, "Figuras 3D", new Vector3(0f, -0.004f, -0.001f), new Vector2(0.09f, 0.01f), new Color(0.3f, 0.3f, 0.35f));
        return panel;
    }

    // ---------- Panel de arriba: animación, capas, medios y bocas ----------

    static PanelArriba CrearPanelArriba(Transform raiz, ControlManos control, Dibujo dibujo, Animacion animacion, Escenario escenario,
                                 Material matPanel, Material matBoton, Material matBotonMarcado,
                                 Material matClave, Material matCabezal, Material matBarra, Material matAsaPanel)
    {
        var go = new GameObject("PanelArriba");
        go.transform.SetParent(raiz, false);
        go.transform.position = new Vector3(0f, 1.9f, 0.5f);
        var panel = go.AddComponent<PanelArriba>();
        panel.control = control;
        panel.dibujo = dibujo;
        panel.animacion = animacion;
        panel.escenario = escenario;
        panel.materialClave = matClave;

        var contenido = new GameObject("Contenido");
        contenido.transform.SetParent(go.transform, false);
        panel.contenido = contenido;
        var c = contenido.transform;

        var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "Fondo";
        Object.DestroyImmediate(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(c, false);
        fondo.transform.localPosition = new Vector3(0f, 0.035f, 0.006f);
        fondo.transform.localScale = new Vector3(0.5f, 0.34f, 1f);
        SinSombras(fondo.GetComponent<Renderer>(), matPanel);

        // Asa para mover el panel (y cambiar su tamaño con la otra mano)
        var asa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        asa.name = "Asa";
        Object.DestroyImmediate(asa.GetComponent<Collider>());
        asa.transform.SetParent(c, false);
        asa.transform.localPosition = new Vector3(0f, 0.195f, 0f);
        asa.transform.localScale = new Vector3(0.14f, 0.016f, 0.01f);
        SinSombras(asa.GetComponent<Renderer>(), matAsaPanel);
        panel.asa = asa.transform;
        // Otra asa abajo del panel (también lo mueve).
        var asaAbajo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        asaAbajo.name = "AsaAbajo";
        Object.DestroyImmediate(asaAbajo.GetComponent<Collider>());
        asaAbajo.transform.SetParent(c, false);
        asaAbajo.transform.localPosition = new Vector3(0f, -0.15f, 0f);
        asaAbajo.transform.localScale = new Vector3(0.14f, 0.016f, 0.01f);
        SinSombras(asaAbajo.GetComponent<Renderer>(), matAsaPanel);
        panel.asaAbajo = asaAbajo.transform;
        Texto(c, "pellizca aquí para mover · + otra mano = tamaño", new Vector3(0f, 0.195f, -0.006f), new Vector2(0.13f, 0.012f), Color.white);

        panel.textoFotograma = Texto(c, "Fotograma 1 / 2000", new Vector3(0f, 0.168f, -0.001f), new Vector2(0.4f, 0.018f), Color.black);

        // Línea de tiempo
        var barra = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barra.name = "BarraTiempo";
        Object.DestroyImmediate(barra.GetComponent<Collider>());
        barra.transform.SetParent(c, false);
        barra.transform.localPosition = new Vector3(0f, 0.142f, 0f);
        barra.transform.localScale = new Vector3(0.44f, 0.02f, 0.008f);
        SinSombras(barra.GetComponent<Renderer>(), matBarra);
        panel.barra = barra.transform;

        var cabezal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabezal.name = "Cabezal";
        Object.DestroyImmediate(cabezal.GetComponent<Collider>());
        cabezal.transform.SetParent(c, false);
        cabezal.transform.localPosition = new Vector3(-0.22f, 0.142f, -0.006f);
        cabezal.transform.localScale = new Vector3(0.004f, 0.036f, 0.004f);
        SinSombras(cabezal.GetComponent<Renderer>(), matCabezal);
        panel.cabezal = cabezal.transform;

        // Pestañas
        string[] pestanas = { "Animar", "Medios", "Bocas", "Títere", "Zoom: 100", "Fijar aquí" };
        var p = new BotonTocable[pestanas.Length];
        for (int i = 0; i < pestanas.Length; i++)
            p[i] = Boton(c, pestanas[i], new Vector3(-0.2f + i * 0.08f, 0.079f, 0f), matBoton, matBotonMarcado);
        panel.btnPaginaAnimar = p[0];
        panel.btnPaginaMedios = p[1];
        panel.btnPaginaBocas = p[2];
        panel.btnPaginaTitere = p[3];
        panel.btnZoom = p[4];
        panel.btnSeguir = p[5];
        // Ayuda: "?" en la esquina de arriba a la derecha (abre abajo una copia azul que explica cada botón).
        panel.btnAyuda = Boton(c, "?", new Vector3(0.215f, 0.195f, 0f), matBoton, matBotonMarcado);
        panel.btnAyuda.transform.localScale = new Vector3(0.03f, 0.022f, 0.008f);

        // ----- Página Animar -----
        var animar = Pagina(c, "PaginaAnimar");
        panel.paginaAnimar = animar.gameObject;
        string[] controles = { "Inicio", "<", "Play", ">", "+ Clave", "- Clave", "12 fps" };
        var b = new BotonTocable[controles.Length];
        // Reproductor: siempre visible justo debajo de la línea de tiempo (no está dentro de ninguna página).
        for (int i = 0; i < controles.Length; i++)
            b[i] = Boton(c, controles[i], new Vector3(-0.192f + i * 0.064f, 0.11f, 0f), matBoton, matBotonMarcado);
        panel.btnInicio = b[0];
        panel.btnAnterior = b[1];
        panel.btnPlay = b[2];
        panel.btnSiguiente = b[3];
        panel.btnClave = b[4];
        panel.btnQuitarClave = b[5];
        panel.btnFps = b[6];

        panel.btnCapas = new BotonTocable[Dibujo.NumeroDeCapas];
        panel.btnVer = new BotonTocable[Dibujo.NumeroDeCapas];
        for (int i = 0; i < Dibujo.NumeroDeCapas; i++)
        {
            float x = -0.165f + i * 0.11f;
            panel.btnCapas[i] = Boton(animar, "Capa " + (i + 1), new Vector3(x, 0.018f, 0f), matBoton, matBotonMarcado);
            panel.btnVer[i] = Boton(animar, "Ver", new Vector3(x, -0.01f, 0f), matBoton, matBotonMarcado);
        }
        string[] menu = { "Libre (3D)", "Fondo", "Guardar", "Cargar", "Borrar todo", "SVG", "Foto" };
        var m = new BotonTocable[menu.Length];
        for (int i = 0; i < menu.Length; i++)
            m[i] = Boton(animar, menu[i], new Vector3(-0.192f + i * 0.064f, -0.05f, 0f), matBoton, matBotonMarcado);
        panel.btnPlano = m[0];
        panel.btnFondo = m[1];
        panel.btnGuardar = m[2];
        panel.btnCargar = m[3];
        panel.btnBorrarTodo = m[4];
        panel.btnSvg = m[5];
        panel.btnFoto = m[6];
        Texto(animar, "Toca la barra = ir a un fotograma · pellizca una clave = moverla · si editas, se guarda una clave", new Vector3(0f, -0.09f, -0.001f), new Vector2(0.46f, 0.018f), new Color(0.3f, 0.3f, 0.35f));

        // ----- Página Medios -----
        var medios = Pagina(c, "PaginaMedios");
        panel.paginaMedios = medios.gameObject;
        string[] fila1 = { "Grabar", "Video proceso", "Vel x1" };
        string[] fila2 = { "Video anim", "Imagen +", "Imagen -" };
        var f1 = new BotonTocable[3];
        var f2 = new BotonTocable[3];
        for (int i = 0; i < 3; i++)
        {
            f1[i] = BotonAncho(medios, fila1[i], new Vector3(-0.13f + i * 0.13f, 0.056f, 0f), matBoton, matBotonMarcado);
            f2[i] = BotonAncho(medios, fila2[i], new Vector3(-0.13f + i * 0.13f, 0.024f, 0f), matBoton, matBotonMarcado);
        }
        panel.btnGrabar = f1[0];
        panel.btnVideoProceso = f1[1];
        panel.btnVelocidad = f1[2];
        panel.btnVideoAnim = f2[0];
        panel.btnImagenMas = f2[1];
        panel.btnImagenMenos = f2[2];
        string[] fila3 = { "Temblor: No", "Hebras: 1", "Grosor vivo", "Ciclo de 3", "Imágenes: ver" };
        var f3 = new BotonTocable[fila3.Length];
        for (int i = 0; i < fila3.Length; i++)
            f3[i] = Boton(medios, fila3[i], new Vector3(-0.2f + i * 0.1f, -0.008f, 0f), matBoton, matBotonMarcado);
        panel.btnTemblor = f3[0];
        panel.btnHebras = f3[1];
        panel.btnGrosorVivo = f3[2];
        panel.btnCicloTemblor = f3[3];
        panel.btnImagenesVer = f3[4];
        string[] fila4 = { "Suavidad: Normal", "Velocidad: 8", "Boceto: No", "Plano: propio" };
        var f4 = new BotonTocable[fila4.Length];
        for (int i = 0; i < fila4.Length; i++)
            f4[i] = Boton(medios, fila4[i], new Vector3(-0.2f + i * 0.1f, -0.038f, 0f), matBoton, matBotonMarcado);
        panel.btnSuavidad = f4[0];
        panel.btnVelocidadTemblor = f4[1];
        panel.btnBoceto = f4[2];
        panel.btnUnirPlano = f4[3];
        panel.textoMedios = Texto(medios, "", new Vector3(0f, -0.068f, -0.001f), new Vector2(0.46f, 0.018f), new Color(0.2f, 0.2f, 0.25f));
        Texto(medios, "Videos y fotos en Dibujos · las imágenes y las capas de boceto no salen en ellos", new Vector3(0f, -0.095f, -0.001f), new Vector2(0.46f, 0.016f), new Color(0.3f, 0.3f, 0.35f));

        // ----- Página Bocas -----
        var bocas = Pagina(c, "PaginaBocas");
        panel.paginaBocas = bocas.gameObject;
        panel.btnBocas = new BotonTocable[Lipsync.Nombres.Length];
        for (int i = 0; i < Lipsync.Nombres.Length; i++)
            panel.btnBocas[i] = Boton(bocas, Lipsync.Nombres[i], new Vector3(-0.192f + i * 0.064f, 0.055f, 0f), matBoton, matBotonMarcado);
        string[] acciones = { "Modo: Guardar", "Voz", "Audio", "Lipsync", "Quitar audio" };
        var a = new BotonTocable[acciones.Length];
        for (int i = 0; i < acciones.Length; i++)
            a[i] = Boton(bocas, acciones[i], new Vector3(-0.192f + i * 0.096f, 0.018f, 0f), matBoton, matBotonMarcado);
        panel.btnModoBoca = a[0];
        panel.btnVoz = a[1];
        panel.btnAudio = a[2];
        panel.btnLipsync = a[3];
        panel.btnQuitarAudio = a[4];
        panel.textoBocas = Texto(bocas, "", new Vector3(0f, -0.04f, -0.001f), new Vector2(0.46f, 0.04f), new Color(0.2f, 0.2f, 0.25f));
        Texto(bocas, "Dibuja la boca en su propia capa y elígela · audios propios en Dibujos/Audio", new Vector3(0f, -0.09f, -0.001f), new Vector2(0.46f, 0.018f), new Color(0.3f, 0.3f, 0.35f));

        // ----- Página Títere -----
        var tit = Pagina(c, "PaginaTitere");
        panel.paginaTitere = tit.gameObject;
        string[] filaT = { "Tipo: Palito", "Crear Palito", "Grabar" };
        var t1 = new BotonTocable[3];
        for (int i = 0; i < 3; i++)
            t1[i] = BotonAncho(tit, filaT[i], new Vector3(-0.13f + i * 0.13f, 0.05f, 0f), matBoton, matBotonMarcado);
        panel.btnTitere = t1[0];
        panel.btnMuneco = t1[1];
        panel.btnGrabarTitere = t1[2];
        string[] filaT2 = { "Posar dedos", "Ciclo: Manual", "Guardar ciclo", "Voltear", "Piso +/-" };
        var t2 = new BotonTocable[filaT2.Length];
        for (int i = 0; i < filaT2.Length; i++)
            t2[i] = Boton(tit, filaT2[i], new Vector3(-0.2f + i * 0.1f, 0.016f, 0f), matBoton, matBotonMarcado);
        panel.btnPiso = t2[4];
        panel.btnPosar = t2[0];
        panel.btnCiclo = t2[1];
        panel.btnGuardarCiclo = t2[2];
        panel.btnVoltear = t2[3];
        string[] filaT3 = { "Pierna 1", "Pierna 2", "Brazo 1", "Brazo 2", "Cuerpo +/-" };
        var t3 = new BotonTocable[filaT3.Length];
        for (int i = 0; i < filaT3.Length; i++)
            t3[i] = Boton(tit, filaT3[i], new Vector3(-0.2f + i * 0.1f, -0.016f, 0f), matBoton, matBotonMarcado);
        panel.btnPierna1 = t3[0];
        panel.btnPierna2 = t3[1];
        panel.btnBrazo1 = t3[2];
        panel.btnBrazo2 = t3[3];
        panel.btnCuerpo = t3[4];
        panel.textoTitere = Texto(tit, "", new Vector3(0f, -0.052f, -0.001f), new Vector2(0.46f, 0.02f), new Color(0.2f, 0.2f, 0.25f));
        Texto(tit, "Crear = se enciende solo · mano a los lados = caminar/correr · golpe arriba = saltar · palma arriba = apagar", new Vector3(0f, -0.09f, -0.001f), new Vector2(0.46f, 0.018f), new Color(0.3f, 0.3f, 0.35f));
        return panel;
    }

    static Transform Pagina(Transform padre, string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        return go.transform;
    }

    // Botón más ancho (para textos largos).
    static BotonTocable BotonAncho(Transform padre, string texto, Vector3 posicion, Material normal, Material marcado)
    {
        var b = Boton(padre, texto, posicion, normal, marcado);
        b.transform.localScale = new Vector3(0.11f, 0.026f, 0.008f);
        if (b.etiqueta != null)
            b.etiqueta.rectTransform.sizeDelta = new Vector2(0.1f, 0.02f);
        return b;
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

    // Material URP Unlit transparente (para ver las imágenes al 50% o 20%).
    static void Transparente(Material m)
    {
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
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

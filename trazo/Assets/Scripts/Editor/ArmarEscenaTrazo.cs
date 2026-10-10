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
        // Los tiradores Bézier se ven siempre, aunque queden detrás de una línea (shader TrazoVR/Encima).
        var shaderEncima = Shader.Find("TrazoVR/Encima");
        var matAsa = Mat("Asa", shaderEncima != null ? shaderEncima : unlit, new Color(0.2f, 0.8f, 0.3f));
        var matIman = Mat("Iman", unlit, new Color(0.1f, 0.95f, 0.35f));
        // Halo suave detrás de las líneas (con un fondo 360 o la realidad). Sin el shader, no hay halo.
        var shaderHalo = Shader.Find("TrazoVR/Halo");
        var matHalo = shaderHalo != null ? Mat("Halo", shaderHalo, new Color(1f, 1f, 1f, 0.85f)) : null;
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
        // Íconos de instrumentos (carrusel sobre el parlante): transparentes, los de los lados al 70 %.
        var matIconos = Mat("IconosSonido", unlit, Color.white);
        Transparente(matIconos);
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
        // Título y tutorial (JCartoons): manos guía, escarcha, papelitos, viñeta, números y bandera.
        var shaderGuia = Shader.Find("TrazoVR/Guia");
        var shaderBrillo = Shader.Find("TrazoVR/Brillo");
        if (shaderGuia == null || shaderBrillo == null)
            Debug.LogWarning("TrazoVR: no encontré los shaders del tutorial (copia otra vez la carpeta Shaders).");
        Material matManoGuia = shaderGuia != null ? Mat("ManoGuia", shaderGuia, new Color(0.45f, 0.75f, 1f, 0.6f)) : null;
        Material matConfeti = shaderBrillo != null ? Mat("Confeti", shaderBrillo, Color.white) : null;
        Material matEscarcha = shaderBrillo != null ? Mat("Escarcha", shaderBrillo, Color.white) : null;
        if (matConfeti != null && matConfeti.HasProperty("_Suave"))
            matConfeti.SetFloat("_Suave", 0f);
        if (matEscarcha != null && matEscarcha.HasProperty("_Suave"))
            matEscarcha.SetFloat("_Suave", 1f);
        var matTutNegro = Mat("TutorialNegro", unlit, new Color(0.08f, 0.08f, 0.1f));
        var matTutVerde = Mat("TutorialVerde", unlit, new Color(0.2f, 0.78f, 0.35f));
        var matTutAzul = Mat("TutorialAzul", unlit, new Color(0.2f, 0.5f, 1f));
        var matTutRojo = Mat("TutorialRojo", unlit, new Color(1f, 0.3f, 0.25f));
        var matTutLineaGuia = Mat("TutorialLineaGuia", unlit, new Color(0.45f, 0.5f, 0.62f, 0.35f));
        Transparente(matTutLineaGuia);
        // Viñetas estilo cómic: amarillo apagado con puntitos de trama.
        var shaderTrama = Shader.Find("TrazoVR/Trama");
        Material matTrama = shaderTrama != null ? Mat("VinetaTrama", shaderTrama, new Color(0.97f, 0.82f, 0.3f)) : null;
        if (matTrama != null && matTrama.HasProperty("_ColorPuntos"))
            matTrama.SetColor("_ColorPuntos", new Color(0.93f, 0.55f, 0.15f));
        var fuenteComic = FuenteComic();
        // Guante de caricatura (blanco con contorno) para la mano del OK.
        Material matGuante = null;
        if (shaderGuia != null)
        {
            matGuante = Mat("GuanteCaricatura", shaderGuia, new Color(1f, 1f, 1f, 1f));
            if (matGuante.HasProperty("_Opaco"))
                matGuante.SetFloat("_Opaco", 1f);
            if (matGuante.HasProperty("_ColorBorde"))
                matGuante.SetColor("_ColorBorde", Color.black);
            if (matGuante.HasProperty("_Borde"))
                matGuante.SetFloat("_Borde", 0.36f); // contorno un poquito más grueso
        }
        // Los círculos de los nodos (y las imágenes, dianas, flecha y dial) se ven por ambos lados.
        foreach (var m in new[] { matNodo, matNodoActivo, matAsa, matIman, matImagen, matImagenTransparente, matFlecha, matDianaRoja, matDianaVerde, matBlanco, matDial, matIndicador,
                                  matTutNegro, matTutVerde, matTutAzul, matTutRojo, matTutLineaGuia })
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
        PrepararFondos360(escenario, unlit);

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
        dibujo.materialHalo = matHalo;
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
        control.materialIconos = matIconos;
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
        control.materialGuante = matGuante;
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

        // "Mis archivos": dibujos, videos, fotos y SVG con miniatura (abrir, ver y borrar).
        var goNav = new GameObject("NavegadorArchivos");
        goNav.transform.SetParent(raiz.transform, false);
        var navegador = goNav.AddComponent<NavegadorArchivos>();
        navegador.dibujo = dibujo;
        navegador.control = control;
        navegador.materialPanel = matPanel;
        navegador.materialBoton = matBoton;
        navegador.materialBotonMarcado = matBotonMarcado;
        navegador.materialImagen = matImagen;

        // Título "JCartoons" (cada vez que abres la app) y tutorial con manos guía (la primera vez).
        var goTutorial = new GameObject("Tutorial");
        goTutorial.transform.SetParent(raiz.transform, false);
        var tutorial = goTutorial.AddComponent<Tutorial>();
        tutorial.dibujo = dibujo;
        tutorial.control = control;
        tutorial.titere = titere;
        tutorial.materialGuia = matManoGuia;
        tutorial.materialConfeti = matConfeti;
        tutorial.materialChispa = matEscarcha;
        tutorial.materialNegro = matTutNegro;
        tutorial.materialBlanco = matBlanco;
        tutorial.materialVerde = matTutVerde;
        tutorial.materialAzul = matTutAzul;
        tutorial.materialRojo = matTutRojo;
        tutorial.materialGuiaLinea = matTutLineaGuia;
        tutorial.materialBoton = matBoton;
        tutorial.materialBotonMarcado = matBotonMarcado;
        tutorial.materialTrama = matTrama;
        tutorial.fuenteComic = fuenteComic;

        var panel = CrearPanel(raiz.transform, control, dibujo, escenario, matPanel, matBoton, matBotonMarcado);
        panel.figuras = figuras;
        var arriba = CrearPanelArriba(raiz.transform, control, dibujo, animacion, escenario, matPanel, matBoton, matBotonMarcado, matClave, matCabezal, matBarra, matAsaPanel);
        arriba.lipsync = lipsync;
        arriba.referencias = referencias;
        arriba.grabador = grabador;
        arriba.exportador = exportador;
        arriba.titere = titere;
        control.panelArriba = arriba;
        // Avisos y nombre del gesto: el mismo cartel de cómic, arriba al centro de tu vista (CartelArriba).
        var aviso = Texto(raiz.transform, "", new Vector3(0f, 1.4f, 0.6f), new Vector2(0.36f, 0.05f), new Color(0.1f, 0.1f, 0.12f));
        aviso.gameObject.name = "Aviso";
        aviso.fontSizeMax = 0.3f;
        panel.textoAviso = aviso;
        // Los avisos salen en una viñeta de cómic (como el tutorial).
        var vinetaAviso = aviso.gameObject.AddComponent<VinetaComic>();
        vinetaAviso.texto = aviso;
        vinetaAviso.ajustarAlTexto = true;
        vinetaAviso.materialTrama = matTrama;
        vinetaAviso.materialNegro = matTutNegro;
        vinetaAviso.fuente = fuenteComic;
        vinetaAviso.profundidadSombra = 0.02f;

        // El nombre del gesto (para aprender).
        var ayuda = Texto(raiz.transform, "", new Vector3(0f, 1.3f, 0.5f), new Vector2(0.26f, 0.06f), new Color(0.1f, 0.25f, 0.6f));
        ayuda.gameObject.name = "AyudaGesto";
        ayuda.fontSizeMax = 0.25f;
        control.textoGesto = ayuda;
        // El nombre del gesto, en una viñeta pequeña.
        var vinetaGesto = ayuda.gameObject.AddComponent<VinetaComic>();
        vinetaGesto.texto = ayuda;
        vinetaGesto.ajustarAlTexto = true;
        vinetaGesto.cola = 0; // sin colita
        ayuda.fontSizeMax = 0.2f;
        vinetaGesto.materialTrama = matTrama;
        vinetaGesto.materialNegro = matTutNegro;
        vinetaGesto.fuente = fuenteComic;
        vinetaGesto.profundidadSombra = 0.015f;

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
            // Teclado del visor (para ponerle nombre a los dibujos).
            var teclado = soCfg.FindProperty("requiresSystemKeyboard");
            if (teclado != null && teclado.propertyType == SerializedPropertyType.Boolean && !teclado.boolValue)
            {
                teclado.boolValue = true;
                soCfg.ApplyModifiedProperties();
                EditorUtility.SetDirty(cfg);
            }
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

        // ---------- Nombre de la app y sin el logo de Unity al abrir ----------
        PlayerSettings.productName = "JCartoons";
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;

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

        Texto(contenido.transform, "JCartoons", new Vector3(0f, 0.09f, -0.001f), new Vector2(0.09f, 0.014f), Color.black);
        panel.textoEstado = Texto(contenido.transform, "", new Vector3(0f, -0.105f, -0.001f), new Vector2(0.098f, 0.02f), new Color(0.2f, 0.2f, 0.25f));

        // Dos columnas: Plano / Fondo · Guardar / Cargar · Borrar todo / Imán · Esfera / Cubo · Cilindro / A líneas · Quitar figura
        string[] nombres = { "Libre (3D)", "Fondo: Cuadrícula", "Guardar", "Archivos", "Borrar todo", "Esfera", "Cubo", "Cilindro", "A líneas", "Imán: Sí" };
        Vector2[] lugares =
        {
            new Vector2(-0.025f, 0.064f), new Vector2(0.025f, 0.064f),
            new Vector2(-0.025f, 0.038f), new Vector2(0.025f, 0.038f),
            new Vector2(-0.025f, 0.012f),
            new Vector2(-0.025f, -0.022f), new Vector2(0.025f, -0.022f),
            new Vector2(-0.025f, -0.048f), new Vector2(0.025f, -0.048f),
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
        // "Quitar figura" ya no hace falta (la figura elegida tiene su X). La música se elige en el carrusel
        // de íconos que está sobre el parlante (arriba a la derecha).
        panel.btnQuitarFigura = null;
        panel.btnIman = botones[9];
        // ? arriba a la izquierda: las secciones del tutorial.
        panel.btnAyuda = Boton(contenido.transform, "?", new Vector3(-0.04f, 0.09f, 0f), matBoton, matBotonMarcado);
        panel.btnAyuda.transform.localScale = new Vector3(0.016f, 0.016f, 0.008f);
        if (panel.btnAyuda.etiqueta != null)
            panel.btnAyuda.etiqueta.rectTransform.sizeDelta = new Vector2(0.014f, 0.014f);
        // X arriba a la derecha: cerrar el menú a mano.
        panel.btnCerrar = Boton(contenido.transform, "X", new Vector3(0.04f, 0.09f, 0f), matBoton, matBotonMarcado);
        panel.btnCerrar.transform.localScale = new Vector3(0.016f, 0.016f, 0.008f);
        if (panel.btnCerrar.etiqueta != null)
            panel.btnCerrar.etiqueta.rectTransform.sizeDelta = new Vector2(0.014f, 0.014f);
        Texto(contenido.transform, "Figuras 3D", new Vector3(0f, -0.004f, -0.001f), new Vector2(0.09f, 0.01f), new Color(0.3f, 0.3f, 0.35f));
        // Botón pequeño debajo del menú: abre la paleta de colores (por si el gesto de la palma no la abre).
        panel.btnColores = Boton(contenido.transform, "Colores", new Vector3(0f, -0.14f, 0f), matBoton, matBotonMarcado);
        panel.btnColores.transform.localScale = new Vector3(0.06f, 0.02f, 0.008f);
        if (panel.btnColores.etiqueta != null)
            panel.btnColores.etiqueta.rectTransform.sizeDelta = new Vector2(0.055f, 0.016f);
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
        // Arriba (siempre): asa, capas, línea de tiempo y reproductor. Las páginas van replegadas, como en un
        // archivador: solo se asoman sus pestañas (ver "Paginas" más abajo).
        fondo.transform.localPosition = new Vector3(0f, 0.152f, 0.006f);
        fondo.transform.localScale = new Vector3(0.5f, 0.116f, 1f);
        SinSombras(fondo.GetComponent<Renderer>(), matPanel);
        // Las páginas (Animar, Bocas, Títere) con su fondo y el asa de abajo: se despliegan al tocar su pestaña.
        var paginas = Pagina(c, "Paginas");
        panel.paginas = paginas.gameObject;
        var fondoPaginas = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondoPaginas.name = "FondoPaginas";
        Object.DestroyImmediate(fondoPaginas.GetComponent<Collider>());
        fondoPaginas.transform.SetParent(paginas, false);
        fondoPaginas.transform.localPosition = new Vector3(0f, -0.03f, 0.006f);
        fondoPaginas.transform.localScale = new Vector3(0.5f, 0.245f, 1f);
        SinSombras(fondoPaginas.GetComponent<Renderer>(), matPanel);

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
        asaAbajo.transform.SetParent(paginas, false);
        asaAbajo.transform.localPosition = new Vector3(0f, -0.15f, 0f);
        asaAbajo.transform.localScale = new Vector3(0.14f, 0.016f, 0.01f);
        SinSombras(asaAbajo.GetComponent<Renderer>(), matAsaPanel);
        panel.asaAbajo = asaAbajo.transform;
        Texto(c, "pellizca aquí para mover · + otra mano = tamaño", new Vector3(0f, 0.195f, -0.006f), new Vector2(0.13f, 0.012f), Color.white);

        panel.textoFotograma = Texto(c, "Fotograma 1 / 2000", new Vector3(0.045f, 0.168f, -0.001f), new Vector2(0.34f, 0.018f), Color.black);

        // Capas: un botón plegable ("+ Capa 1") y, al abrirlo, una fila por capa encima del panel
        // (elegir la capa, Ver/Oculta y las claves de esa capa alineadas con la línea de tiempo).
        panel.btnCapasPlegar = Boton(c, "+ Capa 1", new Vector3(-0.19f, 0.168f, 0f), matBoton, matBotonMarcado);
        panel.btnCapasPlegar.transform.localScale = new Vector3(0.075f, 0.02f, 0.008f);
        if (panel.btnCapasPlegar.etiqueta != null)
            panel.btnCapasPlegar.etiqueta.rectTransform.sizeDelta = new Vector2(0.07f, 0.016f);
        var capasGo = new GameObject("CapasDesplegable");
        capasGo.transform.SetParent(c, false);
        panel.capasDesplegable = capasGo;
        var fondoCapas = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondoCapas.name = "FondoCapas";
        Object.DestroyImmediate(fondoCapas.GetComponent<Collider>());
        fondoCapas.transform.SetParent(capasGo.transform, false);
        float filasAlto = Dibujo.NumeroDeCapas * panel.pasoCapasY;
        fondoCapas.transform.localPosition = new Vector3(0.04f, panel.filaCapasY + filasAlto * 0.5f - panel.pasoCapasY * 0.5f, 0.006f);
        fondoCapas.transform.localScale = new Vector3(0.77f, filasAlto + 0.01f, 1f);
        SinSombras(fondoCapas.GetComponent<Renderer>(), matPanel);
        panel.btnCapas = new BotonTocable[Dibujo.NumeroDeCapas];
        panel.btnVer = new BotonTocable[Dibujo.NumeroDeCapas];
        // El estilo de cada capa (antes en la página Animar): Boceto y Plano, a la derecha de sus claves.
        panel.btnBocetoCapa = new BotonTocable[Dibujo.NumeroDeCapas];
        panel.btnPlanoCapa = new BotonTocable[Dibujo.NumeroDeCapas];
        // Liberar: quita el encantamiento 2D de esa capa (solo se ve si tiene líneas encantadas en su hoja).
        panel.btnLiberarCapa = new BotonTocable[Dibujo.NumeroDeCapas];
        for (int i = 0; i < Dibujo.NumeroDeCapas; i++)
        {
            float y = panel.filaCapasY + i * panel.pasoCapasY;
            panel.btnCapas[i] = Boton(capasGo.transform, "Capa " + (i + 1), new Vector3(-0.3f, y, 0f), matBoton, matBotonMarcado);
            panel.btnVer[i] = Boton(capasGo.transform, "Ver", new Vector3(-0.25f, y, 0f), matBoton, matBotonMarcado);
            panel.btnVer[i].transform.localScale = new Vector3(0.042f, 0.022f, 0.008f);
            panel.btnBocetoCapa[i] = Boton(capasGo.transform, "Boceto: No", new Vector3(0.275f, y, 0f), matBoton, matBotonMarcado);
            panel.btnPlanoCapa[i] = Boton(capasGo.transform, "Plano: propio", new Vector3(0.337f, y, 0f), matBoton, matBotonMarcado);
            foreach (var bc in new[] { panel.btnBocetoCapa[i], panel.btnPlanoCapa[i] })
            {
                bc.transform.localScale = new Vector3(0.058f, 0.022f, 0.008f);
                if (bc.etiqueta != null)
                    bc.etiqueta.rectTransform.sizeDelta = new Vector2(0.054f, 0.018f);
            }
            panel.btnLiberarCapa[i] = Boton(capasGo.transform, "Liberar", new Vector3(0.394f, y, 0f), matBoton, matBotonMarcado);
        }

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

        // Pestañas asomadas bajo el reproductor (como un archivador): Animar, Bocas y Títere.
        // Tocar una = se despliega su página; tocarla otra vez = se repliega.
        string[] pestanas = { "Animar", "Bocas", "Títere" };
        var p = new BotonTocable[pestanas.Length];
        for (int i = 0; i < pestanas.Length; i++)
        {
            p[i] = Boton(c, pestanas[i], new Vector3(-0.085f + i * 0.085f, 0.08f, 0f), matBoton, matBotonMarcado);
            p[i].transform.localScale = new Vector3(0.078f, 0.022f, 0.008f);
            if (p[i].etiqueta != null)
                p[i].etiqueta.rectTransform.sizeDelta = new Vector2(0.07f, 0.018f);
        }
        panel.btnPaginaAnimar = p[0];
        panel.btnPaginaMedios = null;
        panel.btnPaginaBocas = p[1];
        panel.btnPaginaTitere = p[2];
        // Fijar aquí / Seguirme: un alfiler junto al asa (sin texto).
        panel.btnSeguir = Boton(c, "", new Vector3(0.18f, 0.195f, 0f), matBoton, matBotonMarcado);
        panel.btnSeguir.transform.localScale = new Vector3(0.026f, 0.022f, 0.008f);
        var cabezaAlfiler = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cabezaAlfiler.name = "AlfilerCabeza";
        Object.DestroyImmediate(cabezaAlfiler.GetComponent<Collider>());
        cabezaAlfiler.transform.SetParent(c, false);
        cabezaAlfiler.transform.localPosition = new Vector3(0.1835f, 0.1985f, -0.0065f);
        cabezaAlfiler.transform.localScale = Vector3.one * 0.0085f;
        SinSombras(cabezaAlfiler.GetComponent<Renderer>(), matClave);
        var agujaAlfiler = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        agujaAlfiler.name = "AlfilerAguja";
        Object.DestroyImmediate(agujaAlfiler.GetComponent<Collider>());
        agujaAlfiler.transform.SetParent(c, false);
        agujaAlfiler.transform.localPosition = new Vector3(0.1785f, 0.1925f, -0.0062f);
        agujaAlfiler.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        agujaAlfiler.transform.localScale = new Vector3(0.0016f, 0.0045f, 0.0016f);
        SinSombras(agujaAlfiler.GetComponent<Renderer>(), matCabezal);
        // Ayuda: "?" en la esquina de arriba a la derecha (abre abajo una copia azul que explica cada botón).
        panel.btnAyuda = Boton(c, "?", new Vector3(0.215f, 0.195f, 0f), matBoton, matBotonMarcado);
        panel.btnAyuda.transform.localScale = new Vector3(0.03f, 0.022f, 0.008f);

        // Reproductor: siempre visible justo debajo de la línea de tiempo (no está dentro de ninguna página).
        // (Zoom va aquí: es de la línea de tiempo.)
        string[] controles = { "Inicio", "<", "Play", ">", "+ Clave", "- Clave", "12 fps", "Zoom: 100" };
        var b = new BotonTocable[controles.Length];
        for (int i = 0; i < controles.Length; i++)
            b[i] = Boton(c, controles[i], new Vector3(-0.21f + i * 0.06f, 0.11f, 0f), matBoton, matBotonMarcado);
        panel.btnZoom = b[7];
        panel.btnInicio = b[0];
        panel.btnAnterior = b[1];
        panel.btnPlay = b[2];
        panel.btnSiguiente = b[3];
        panel.btnClave = b[4];
        panel.btnQuitarClave = b[5];
        panel.btnFps = b[6];

        // ----- Página Animar: solo animación. Grabar proceso, Foto, SVG e imágenes están en Mis archivos;
        //       Boceto y Plano, en cada fila de Capas. -----
        var animar = Pagina(paginas, "PaginaAnimar");
        panel.paginaAnimar = animar.gameObject;
        panel.paginaMedios = null;
        panel.btnVideoAnim = BotonAncho(animar, "Video anim", new Vector3(-0.17f, 0.04f, 0f), matBoton, matBotonMarcado);
        panel.btnGrabar = null;
        panel.btnPausaGrabar = null;
        panel.btnVideoProceso = null;
        panel.btnVelocidad = null;
        panel.btnFoto = null;
        panel.btnSvg = null;
        panel.btnImagenMas = null;
        panel.btnImagenMenos = null;
        panel.btnImagenesVer = null;
        panel.btnBoceto = null;
        panel.btnUnirPlano = null;
        // El temblor y sus opciones ahora están en la paleta de colores (palma izquierda).
        panel.btnTemblor = null;
        panel.btnHebras = null;
        panel.btnGrosorVivo = null;
        panel.btnCicloTemblor = null;
        panel.btnSuavidad = null;
        panel.btnVelocidadTemblor = null;
        panel.btnPlano = null;
        panel.btnFondo = null;
        panel.btnGuardar = null;
        panel.btnCargar = null;
        panel.btnBorrarTodo = null;
        panel.textoMedios = Texto(animar, "", new Vector3(0f, -0.048f, -0.001f), new Vector2(0.46f, 0.018f), new Color(0.2f, 0.2f, 0.25f));
        Texto(animar, "Toca la barra = ir a un fotograma · pellizca una clave = moverla · Foto, SVG y Grabar proceso: Mis archivos → Compartir", new Vector3(0f, -0.085f, -0.001f), new Vector2(0.46f, 0.016f), new Color(0.3f, 0.3f, 0.35f));
        Texto(animar, "Próximamente aquí: papel cebolla y cuadro a cuadro", new Vector3(0f, -0.115f, -0.001f), new Vector2(0.46f, 0.016f), new Color(0.45f, 0.45f, 0.5f));

        // ----- Página Bocas -----
        var bocas = Pagina(paginas, "PaginaBocas");
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
        var tit = Pagina(paginas, "PaginaTitere");
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

    // Letra de cómic (Bangers, licencia libre OFL) para las viñetas del tutorial.
    // Busca el archivo .ttf (viene en Plugins/Fuentes) y crea una sola vez su versión para TextMeshPro.
    static TMP_FontAsset FuenteComic()
    {
        const string ruta = carpetaBase + "/BangersSDF.asset";
        var hecha = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta);
        if (hecha != null)
            return hecha;
        Font fuente = null;
        foreach (var guid in AssetDatabase.FindAssets("Bangers-Regular t:Font"))
        {
            fuente = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
            if (fuente != null)
                break;
        }
        if (fuente == null)
        {
            Debug.LogWarning("TrazoVR: no encontré la letra Bangers (copia otra vez la carpeta Plugins). Las viñetas usan la letra normal.");
            return null;
        }
        try
        {
            var fa = TMP_FontAsset.CreateFontAsset(fuente);
            if (fa == null)
                return null;
            fa.name = "Bangers SDF";
            AssetDatabase.CreateAsset(fa, ruta);
            if (fa.material != null)
            {
                fa.material.name = "Bangers SDF Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            if (fa.atlasTextures != null)
                foreach (var tx in fa.atlasTextures)
                    if (tx != null)
                    {
                        tx.name = "Bangers SDF Atlas";
                        AssetDatabase.AddObjectToAsset(tx, fa);
                    }
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no pude preparar la letra de cómic: " + e.Message);
            return null;
        }
    }

    // Fondos 360 en 3D: las fotos de Plugins/Fondos (con sus distancias calculadas con IA).
    static readonly string[,] Fondos360 =
    {
        { "cuarto360", "Cuarto 360" },
        { "roma360", "Roma de noche" },
        { "amanecer360", "Amanecer" },
        { "venecia_canal360", "Canal de Venecia" },
        { "venecia_calle360", "Callejón de Venecia" },
        { "atardecer_mar360", "Atardecer en el mar" },
        { "crucero360", "Crucero de lujo" },
        { "columnas360", "Pórtico de columnas" },
        { "mirador360", "Mirador" },
        { "parque360", "Parque" },
        { "luna360", "La Luna" },
    };

    static void PrepararFondos360(Escenario escenario, Shader unlit)
    {
        var lista = new List<Escenario.Foto360>();
        var usadas = new HashSet<string>();
        for (int k = 0; k < Fondos360.GetLength(0); k++)
        {
            string archivo = Fondos360[k, 0];
            foreach (var guid in AssetDatabase.FindAssets(archivo + " t:Texture2D"))
            {
                string rutaTex = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(rutaTex) != archivo)
                    continue;
                var f = PrepararFoto360(rutaTex, Fondos360[k, 1]);
                if (f != null)
                {
                    lista.Add(f);
                    usadas.Add(rutaTex);
                    break;
                }
            }
        }
        // Cualquier otra foto 360 (el doble de ancha que de alta) que pongas en Plugins/Fondos también entra.
        const string carpetaFondos = "Assets/Plugins/Fondos";
        if (AssetDatabase.IsValidFolder(carpetaFondos))
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { carpetaFondos }))
            {
                string rutaTex = AssetDatabase.GUIDToAssetPath(guid);
                if (usadas.Contains(rutaTex))
                    continue;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);
                if (tex == null || Mathf.Abs(tex.width - tex.height * 2) > tex.height / 20)
                    continue;
                var f = PrepararFoto360(rutaTex, System.IO.Path.GetFileNameWithoutExtension(rutaTex));
                if (f != null)
                    lista.Add(f);
            }
        if (lista.Count == 0)
            Debug.LogWarning("TrazoVR: no encontré las fotos 360 (copia otra vez la carpeta Plugins).");
        escenario.fondos360 = lista.ToArray();
        // Material sin luz (se ve igual desde cualquier lado); la foto se pone al elegir el fondo.
        var m = Mat("Fondo360", unlit, Color.white);
        if (m.HasProperty("_Cull"))
            m.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        escenario.materialFondo360 = m;
    }

    // Una foto 360: tamaño máximo 4096, sin mipmaps, y sus distancias (archivo <nombre>_profundidad) si las tiene.
    static Escenario.Foto360 PrepararFoto360(string rutaTex, string nombre)
    {
        var imp = AssetImporter.GetAtPath(rutaTex) as TextureImporter;
        if (imp != null && (imp.maxTextureSize != 4096 || imp.mipmapEnabled || imp.wrapModeV != TextureWrapMode.Clamp))
        {
            imp.maxTextureSize = 4096;
            imp.mipmapEnabled = false;
            imp.wrapModeU = TextureWrapMode.Repeat;
            imp.wrapModeV = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);
        if (tex == null)
            return null;
        string archivo = System.IO.Path.GetFileNameWithoutExtension(rutaTex);
        TextAsset profundidad = null;
        foreach (var guid in AssetDatabase.FindAssets(archivo + "_profundidad t:TextAsset"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(ruta) != archivo + "_profundidad")
                continue;
            profundidad = AssetDatabase.LoadAssetAtPath<TextAsset>(ruta);
            if (profundidad != null)
                break;
        }
        return new Escenario.Foto360 { nombre = nombre, foto = tex, profundidad = profundidad };
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

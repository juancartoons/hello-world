#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Menú de ayuda: FarmaciaVR > ★ Aplicar estilo toon y escenografía.
// Con un clic arma todo: materiales toon, muebles con productos, mostrador, techo, letreros,
// paredes con ventanas y sol, exterior (Bogotá), discos de teletransporte, escondites y personaje.
// Se puede usar varias veces: reconstruye todo sin duplicar.
public static class AplicarEstiloYEscenografia
{
    const string carpetaMateriales = "Assets/Materials/FarmaciaVR/Toon";
    const string carpetaMallas = "Assets/Generated/FarmaciaVR";
    const string nombreRaiz = "_Escenografia";

    static readonly Color azul = new Color(0.09f, 0.36f, 0.72f);
    static readonly Color azulClaro = new Color(0.55f, 0.78f, 0.95f);
    static readonly Color blanco = new Color(0.96f, 0.96f, 0.95f);
    static readonly Color grisOscuro = new Color(0.22f, 0.24f, 0.30f);
    static readonly Color verde = new Color(0.12f, 0.68f, 0.38f);
    static readonly Color colorContorno = new Color(0.10f, 0.12f, 0.20f);
    static readonly Color sombraSuave = new Color(0.78f, 0.82f, 0.90f);

    static readonly Color[] coloresProductos =
    {
        new Color(0.91f, 0.26f, 0.24f), new Color(0.98f, 0.60f, 0.20f), new Color(0.99f, 0.83f, 0.25f),
        new Color(0.36f, 0.75f, 0.40f), new Color(0.20f, 0.70f, 0.70f), new Color(0.25f, 0.50f, 0.90f),
        new Color(0.60f, 0.40f, 0.85f), new Color(0.95f, 0.50f, 0.70f), new Color(0.95f, 0.95f, 0.95f),
    };

    // Sol: viene del este (entra solo por la ventana de la derecha, cerca de la entrada).
    static readonly Vector3 rotacionSol = new Vector3(40f, -80f, 0f);

    // Discos de teletransporte (40 cm): pasillos, frente y fondo.
    static readonly Vector3[] puntosTeletransporte = CrearPuntos();

    static Vector3[] CrearPuntos()
    {
        var p = new List<Vector3>();
        foreach (float x in new[] { -3.6f, -1.25f, 1.25f, 3.6f })
            foreach (float z in new[] { -1.0f, 1.0f, 2.7f })
                p.Add(new Vector3(x, 0.01f, z));
        foreach (float x in new[] { -3.6f, -1.8f, 0f, 1.8f, 3.6f })
            p.Add(new Vector3(x, 0.01f, -3.2f));
        // Zona nueva del fondo (la farmacia se amplió 3 m): frente al mostrador y a sus lados.
        foreach (float x in new[] { -3.6f, -1.8f, 0f, 1.8f, 3.6f })
            p.Add(new Vector3(x, 0.01f, 4.4f));
        foreach (float x in new[] { -3.6f, -2.4f, 2.4f, 3.6f })
            p.Add(new Vector3(x, 0.01f, 6.0f));
        return p.ToArray();
    }

    // Escondites en el piso (los de encima de productos se calculan al armar los muebles).
    static readonly Vector3[] escondidesPiso =
    {
        new Vector3(-3.05f, 0.05f, -0.2f), new Vector3(-1.7f, 0.05f, 2.2f), new Vector3(-0.8f, 0.05f, -1.6f),
        new Vector3(0.8f, 0.05f, 0.3f), new Vector3(1.9f, 0.05f, -0.4f), new Vector3(3.05f, 0.05f, 2.1f),
        new Vector3(2.0f, 0.05f, 7.5f), new Vector3(-4.2f, 0.05f, 7.5f), new Vector3(4.2f, 0.05f, -4.4f),
        new Vector3(-4.2f, 0.05f, -4.4f), new Vector3(3.0f, 0.05f, 5.3f), new Vector3(-2.6f, 0.05f, 6.9f),
    };

    [MenuItem("FarmaciaVR/★ Aplicar estilo toon y escenografía")]
    static void Aplicar()
    {
        foreach (var nombre in new[] { "FarmaciaVR/Toon", "FarmaciaVR/Toon Sin Borde", "FarmaciaVR/Vidrio", "FarmaciaVR/Luz", "FarmaciaVR/Realista", "FarmaciaVR/Plano" })
        {
            if (Shader.Find(nombre) != null)
                continue;
            EditorUtility.DisplayDialog("Falta un shader",
                $"No encuentro el shader {nombre}.\n\nCopia la carpeta 'Shaders' dentro de Assets (reemplazando), espera a que Unity compile y vuelve a usar este menú.",
                "Entendido");
            return;
        }

        AsegurarCarpeta(carpetaMateriales);
        AsegurarCarpeta(carpetaMallas);

        // ---------- Materiales ----------
        var matPared = CrearMaterial("Toon_Pared", "FarmaciaVR/Toon", new Color(0.97f, 0.96f, 0.93f), sombraSuave, 1, false);
        var matPiso = CrearMaterial("Toon_Piso", "FarmaciaVR/Toon Sin Borde", new Color(0.86f, 0.88f, 0.90f), sombraSuave, 0, false);
        var matTecho = CrearMaterial("Toon_Techo", "FarmaciaVR/Toon Sin Borde", blanco, new Color(0.93f, 0.94f, 0.96f), 0, false);
        var matLampara = CrearMaterial("Toon_Lampara", "FarmaciaVR/Toon Sin Borde", Color.white, Color.white, 0, false);
        var matKit = CrearMaterial("Toon_Kit", "FarmaciaVR/Toon", Color.white, sombraSuave, 2, true);
        var matPunto = CrearMaterial("Toon_Punto", "FarmaciaVR/Toon Sin Borde", new Color(0.2f, 0.8f, 1f), Color.white, 0, false);
        var matExterior = CrearMaterialSimple("Exterior_Realista", "FarmaciaVR/Realista", Color.white);
        var matBoton = CrearMaterial("Toon_Boton", "FarmaciaVR/Toon Sin Borde", Color.white, new Color(0.85f, 0.87f, 0.9f), 0, false);
        var matPluma = CrearMaterial("Toon_Pluma", "FarmaciaVR/Toon Sin Borde", Color.white, new Color(0.85f, 0.80f, 0.85f), 0, true);
        var matVidrio = CrearMaterialSimple("Toon_Vidrio", "FarmaciaVR/Vidrio", new Color(0.75f, 0.9f, 1f, 0.18f));
        var matLuz = CrearMaterialSimple("Toon_Luz", "FarmaciaVR/Luz", new Color(1f, 0.93f, 0.72f, 1f));
        var matLinea = CrearMaterialSimple("Mat_LineaTeleport", "Sprites/Default", Color.white);
        var matBombillo = CrearMaterialPlano("Plano_Bombillo", Color.white, false, true, 2000);
        var matHalo = CrearMaterialPlano("Plano_Halo", Color.white, true, false, 3000);
        AssetDatabase.SaveAssets();

        // El cubo de prueba del principio ya no se necesita: se mide (para que el pájaro tenga su tamaño) y se borra.
        float tamPersonaje = MedirYBorrarCubo();
        Vector3 dirLuz = Quaternion.Euler(rotacionSol) * Vector3.forward;

        // ---------- Raíz de la escenografía (se reconstruye cada vez) ----------
        var viejaRaiz = GameObject.Find(nombreRaiz);
        if (viejaRaiz != null)
            Undo.DestroyObjectImmediate(viejaRaiz);
        var raiz = new GameObject(nombreRaiz);
        Undo.RegisterCreatedObjectUndo(raiz, "Aplicar escenografía");

        // ---------- Farmacia más grande hacia el fondo (para caminar alrededor del mostrador) ----------
        AmpliarFarmacia();

        // ---------- Paredes y piso ----------
        PonerMaterial(GameObject.Find("Pared_Norte"), matPared);
        PonerMaterial(GameObject.Find("Plane"), matPiso);
        foreach (var nombre in new[] { "Pared_Sur", "Pared_Este", "Pared_Oeste" })
            OcultarRender(GameObject.Find(nombre)); // se reemplazan por paredes con ventanas; el collider se queda (vidrio invisible)
        FachadaYExterior.ConstruirFachada(raiz.transform, matKit, matVidrio, matLuz, dirLuz, carpetaMallas);

        // ---------- Góndolas y estantes ----------
        var escondites = new List<Vector3>(escondidesPiso);
        int semilla = 1;
        foreach (var nombre in new[] { "Gondola_1", "Gondola_2", "Gondola_3" })
            ConstruirMueble(nombre, raiz.transform, matKit, true, 4, semilla++, tamPersonaje, escondites);
        foreach (var nombre in new[] { "Estante_Este", "Estante_Oeste" })
            ConstruirMueble(nombre, raiz.transform, matKit, false, 5, semilla++, tamPersonaje, escondites);

        // ---------- Mostrador, techo, decoración ----------
        ConstruirMostrador(raiz.transform, matKit);
        ConstruirTecho(raiz.transform, matTecho, matLampara);
        bool conTextos = ConstruirDecoracion(raiz.transform, matKit);

        // ---------- Exterior ----------
        FachadaYExterior.ConstruirExterior(raiz.transform, matExterior, carpetaMallas);
        FachadaYExterior.ConstruirTrafico(raiz.transform, matExterior, matBombillo, carpetaMallas);

        // ---------- Teletransporte, escondites y personaje ----------
        ConstruirPuntosTeletransporte(matPunto, matLinea);
        ConstruirEscondites(escondites);
        var mallaPluma = CrearMallaPluma();
        ConstruirPajaro(matKit, matPluma, mallaPluma, matHalo, tamPersonaje);
        ConstruirCronometro();
        ConstruirOpcionesNavegacion(raiz.transform, matBoton);

        // ---------- Luz y cámara ----------
        var luz = GameObject.Find("Directional Light");
        if (luz != null)
        {
            Undo.RecordObject(luz.transform, "Luz");
            luz.transform.rotation = Quaternion.Euler(rotacionSol);
            var componenteLuz = luz.GetComponent<Light>();
            if (componenteLuz != null)
            {
                Undo.RecordObject(componenteLuz, "Luz");
                componenteLuz.color = new Color(1f, 0.96f, 0.88f); // sol de la mañana
                componenteLuz.intensity = 1.15f;
                RenderSettings.sun = componenteLuz;
            }
        }
        // Cielo realista de Unity (el de la primera versión), con el sol en la misma dirección de la luz.
        var cieloUnity = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        if (cieloUnity != null)
            RenderSettings.skybox = cieloUnity;
        var rig = Object.FindFirstObjectByType<OVRCameraRig>();
        var camara = rig != null && rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : null;
        if (camara != null)
        {
            Undo.RecordObject(camara, "Cámara");
            camara.farClipPlane = Mathf.Max(camara.farClipPlane, 1000f); // para ver los cerros
            camara.clearFlags = CameraClearFlags.Skybox;
        }

        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log($"FarmaciaVR: listo ({escondites.Count} escondites, {puntosTeletransporte.Length} discos)" +
                  (conTextos ? "" : " — sin textos: falta TextMeshPro") + ". Guarda con Ctrl + S.");
        EditorUtility.DisplayDialog("FarmaciaVR", "¡Listo! Revisa la escena y guarda con Ctrl + S.", "OK");
    }

    // Mueve y estira los bloques originales de la maqueta para que la farmacia llegue hasta z = Fondo.
    // Usa valores fijos, así se puede aplicar varias veces sin que siga creciendo.
    static void AmpliarFarmacia()
    {
        float fondo = FachadaYExterior.Fondo;
        float largo = fondo + 5f, centro = (fondo - 5f) / 2f;
        void Poner(string nombre, Vector3 posicion, Vector3? escala = null)
        {
            var go = GameObject.Find(nombre);
            if (go == null)
                return;
            Undo.RecordObject(go.transform, "Ampliar farmacia");
            go.transform.position = posicion;
            if (escala.HasValue)
                go.transform.localScale = escala.Value;
        }
        Poner("Pared_Norte", new Vector3(0f, 1.5f, fondo), new Vector3(10f, 3f, 0.2f));
        Poner("Pared_Este", new Vector3(5f, 1.5f, centro), new Vector3(0.2f, 3f, largo));
        Poner("Pared_Oeste", new Vector3(-5f, 1.5f, centro), new Vector3(0.2f, 3f, largo));
        Poner("Plane", new Vector3(0f, 0f, centro), new Vector3(1f, 1f, largo / 10f));
        Poner("Mostrador", new Vector3(0f, 0.5f, fondo - 0.75f)); // pegado a la pared del fondo (queda un espacio de 25 cm)
    }

    // ================= Muebles =================

    static void ConstruirMueble(string nombre, Transform raiz, Material mat, bool dobleCara, int niveles, int semilla,
        float tamPersonaje, List<Vector3> escondites)
    {
        var original = GameObject.Find(nombre);
        if (original == null)
        {
            Debug.LogWarning($"FarmaciaVR: no encontré '{nombre}', lo salto.");
            return;
        }

        Bounds b = LimitesDeBloque(original.transform);
        if (!dobleCara)
        {
            // Los estantes de pared se separan 1 cm de la pared para que nada "pelee" con ella.
            float haciaPared = Mathf.Sign(b.center.x);
            b = new Bounds(b.center - new Vector3(haciaPared * 0.005f, 0f, 0f), b.size - new Vector3(0.01f, 0f, 0f));
        }
        OcultarRender(original);
        // El bloque ya no bloquea nada: ahora cada repisa tiene su propio collider (así se ven los productos).
        var colliderBloque = original.GetComponent<Collider>();
        if (colliderBloque != null)
        {
            Undo.RecordObject(colliderBloque, "Collider");
            colliderBloque.enabled = false;
        }

        var rnd = new System.Random(semilla * 7919);
        var kit = new KitMalla();
        var colisiones = new List<Bounds>();
        void Estructura(Vector3 c, Vector3 s, Color color)
        {
            kit.Caja(c, s, color);
            colisiones.Add(new Bounds(c, s));
        }

        float baseAlto = 0.12f;
        float yBase = b.min.y + baseAlto;

        // Zócalo del mueble un poco metido (como en los muebles reales): así ninguna cara coincide
        // con las tapas laterales y no hay parpadeo. Las tapas van del piso hasta debajo del techo del mueble.
        Estructura(new Vector3(b.center.x, b.min.y + baseAlto / 2f, b.center.z), new Vector3(b.size.x - 0.04f, baseAlto, b.size.z - 0.12f), azul);
        Estructura(new Vector3(b.center.x, b.max.y - 0.015f, b.center.z), new Vector3(b.size.x, 0.03f, b.size.z), blanco);
        float altoTapa = b.size.y - 0.03f;
        Estructura(new Vector3(b.center.x, b.min.y + altoTapa / 2f, b.min.z + 0.02f), new Vector3(b.size.x, altoTapa, 0.04f), blanco);
        Estructura(new Vector3(b.center.x, b.min.y + altoTapa / 2f, b.max.z - 0.02f), new Vector3(b.size.x, altoTapa, 0.04f), blanco);

        // Panel trasero
        var lados = new List<int>();
        float xPanel;
        if (dobleCara)
        {
            xPanel = b.center.x;
            lados.Add(-1);
            lados.Add(1);
        }
        else
        {
            int mira = b.center.x > 0 ? -1 : 1; // los estantes de pared miran hacia el centro
            xPanel = mira > 0 ? b.min.x + 0.025f : b.max.x - 0.025f;
            lados.Add(mira);
        }
        float altoPanel = b.max.y - 0.03f - yBase;
        Estructura(new Vector3(xPanel, yBase + altoPanel / 2f, b.center.z), new Vector3(0.05f, altoPanel, b.size.z - 0.08f), azul);

        float paso = altoPanel / niveles;
        foreach (int lado in lados)
        {
            float xFrente = lado > 0 ? b.max.x : b.min.x;
            float xFondo = xPanel + lado * 0.025f;
            float profundidad = Mathf.Abs(xFrente - xFondo);
            var candidatos = new List<(Vector3 punto, Bounds producto)>();

            for (int nivel = 0; nivel < niveles; nivel++)
            {
                float yRepisa = yBase + nivel * paso;
                if (nivel > 0)
                    Estructura(new Vector3((xFrente + xFondo) / 2f, yRepisa, b.center.z), new Vector3(profundidad, 0.025f, b.size.z - 0.08f), blanco);

                float ySuelo = yRepisa + (nivel > 0 ? 0.0125f : 0f);
                float altoMax = Mathf.Min(0.32f, paso - 0.07f);
                float altoParaEscondite = paso - 0.025f - (tamPersonaje + 0.06f);
                float z = b.min.z + 0.07f;
                while (true)
                {
                    float ancho = Rango(rnd, 0.06f, 0.14f);
                    if (z + ancho > b.max.z - 0.07f)
                        break;
                    if (rnd.NextDouble() < 0.08) // algún hueco para que se vea natural
                    {
                        z += ancho + 0.01f;
                        continue;
                    }
                    float alto = Rango(rnd, 0.08f, altoMax);
                    float fondo = Rango(rnd, 0.08f, Mathf.Min(0.22f, profundidad - 0.03f));
                    Color color = coloresProductos[rnd.Next(coloresProductos.Length)];

                    float xCentro = xFrente - lado * (0.015f + fondo / 2f);
                    Vector3 centro = new Vector3(xCentro, ySuelo + alto / 2f, z + ancho / 2f);
                    Vector3 tam = new Vector3(fondo, alto, ancho);
                    kit.grosorContorno = 0.5f; // contorno más delgado en los productos
                    kit.Caja(centro, tam, color);
                    kit.grosorContorno = 1f;

                    Color etiqueta = color.r > 0.9f && color.g > 0.9f && color.b > 0.9f ? azul : blanco;
                    Vector3 cara = new Vector3(xCentro + lado * (fondo / 2f + 0.002f), ySuelo + alto * 0.55f, centro.z);
                    kit.Etiqueta(cara, new Vector3(lado, 0f, 0f), alto * 0.18f, ancho * 0.38f, etiqueta);

                    // Escondite posible: encima de un producto, a una altura cómoda (nunca encima del mueble).
                    bool alturaComoda = nivel <= 2 && ySuelo + alto < 1.35f;
                    bool lejosDeLasPuntas = z > b.min.z + 0.4f && z < b.max.z - 0.4f;
                    if (alturaComoda && lejosDeLasPuntas && alto <= altoParaEscondite && ancho >= 0.08f)
                        candidatos.Add((new Vector3(xCentro, ySuelo + alto + 0.02f, centro.z), new Bounds(centro, tam)));

                    z += ancho + 0.008f;
                }
            }

            // 3 escondites por cara, al azar; el producto de abajo recibe un collider para apoyarse.
            foreach (var c in candidatos.OrderBy(_ => rnd.Next()).Take(3))
            {
                escondites.Add(c.punto);
                colisiones.Add(c.producto);
            }
        }

        var go = kit.CrearObjeto("Kit_" + nombre, raiz, mat, carpetaMallas);
        var contenedor = new GameObject("Colisiones");
        contenedor.transform.SetParent(go.transform, false);
        foreach (var c in colisiones)
        {
            var caja = contenedor.AddComponent<BoxCollider>();
            caja.center = c.center;
            caja.size = c.size;
        }
    }

    // Mostrador de la caja, pegado a la pared del fondo, con caja registradora, datáfono, dulces, gel,
    // una matera, el letrero "CAJA" colgando y una repisa con medicamentos en la pared de atrás.
    static void ConstruirMostrador(Transform raiz, Material mat)
    {
        var original = GameObject.Find("Mostrador");
        if (original == null)
            return;
        Bounds b = LimitesDeBloque(original.transform);
        OcultarRender(original); // el collider del mostrador se queda

        Color grisMedio = new Color(0.45f, 0.47f, 0.52f);
        Color negro = new Color(0.1f, 0.1f, 0.12f);
        Color terracota = new Color(0.78f, 0.42f, 0.28f);
        Color hojas = new Color(0.25f, 0.6f, 0.32f);

        var kit = new KitMalla();
        kit.Caja(b.center, b.size - new Vector3(0.04f, 0.04f, 0.06f), azul);
        kit.Caja(new Vector3(b.center.x, b.max.y + 0.025f, b.center.z), new Vector3(b.size.x + 0.08f, 0.05f, b.size.z + 0.08f), blanco);
        float zFrente = b.min.z + 0.03f;
        kit.Caja(new Vector3(b.center.x, b.min.y + b.size.y * 0.72f, zFrente - 0.005f), new Vector3(b.size.x - 0.04f, 0.08f, 0.01f), blanco);
        kit.Caja(new Vector3(b.center.x, b.center.y - 0.08f, zFrente - 0.01f), new Vector3(0.30f, 0.10f, 0.02f), verde);
        kit.Caja(new Vector3(b.center.x, b.center.y - 0.08f, zFrente - 0.01f), new Vector3(0.10f, 0.30f, 0.02f), verde);
        float yTope = b.max.y + 0.05f;
        float zc = b.center.z;

        // Caja registradora: cajón, teclado inclinado (mirando al cajero), pantalla para el cliente e impresora.
        float xc = b.center.x + 0.75f;
        kit.Caja(new Vector3(xc, yTope + 0.06f, zc), new Vector3(0.44f, 0.12f, 0.40f), grisOscuro);
        kit.Caja(new Vector3(xc, yTope + 0.03f, zc - 0.205f), new Vector3(0.38f, 0.02f, 0.01f), grisMedio, false); // ranura del cajón
        kit.Hexaedro(new[]
        {
            new Vector3(xc - 0.17f, yTope + 0.12f, zc - 0.02f), new Vector3(xc + 0.17f, yTope + 0.12f, zc - 0.02f),
            new Vector3(xc + 0.17f, yTope + 0.12f, zc + 0.18f), new Vector3(xc - 0.17f, yTope + 0.12f, zc + 0.18f),
            new Vector3(xc - 0.17f, yTope + 0.20f, zc - 0.02f), new Vector3(xc + 0.17f, yTope + 0.20f, zc - 0.02f),
            new Vector3(xc + 0.17f, yTope + 0.15f, zc + 0.18f), new Vector3(xc - 0.17f, yTope + 0.15f, zc + 0.18f),
        }, grisMedio);
        kit.grosorContorno = 0.4f;
        for (int fila = 0; fila < 3; fila++)
            for (int col = 0; col < 4; col++)
            {
                Color tecla = col == 3 ? (fila == 0 ? new Color(0.9f, 0.25f, 0.2f) : fila == 1 ? new Color(0.95f, 0.8f, 0.2f) : verde) : blanco;
                float zt = zc + 0.03f + fila * 0.05f;
                float yt = yTope + 0.20f - (zt - (zc - 0.02f)) / 0.2f * 0.05f + 0.008f;
                kit.Caja(new Vector3(xc - 0.11f + col * 0.07f, yt, zt), new Vector3(0.05f, 0.012f, 0.035f), tecla);
            }
        kit.grosorContorno = 1f;
        kit.Caja(new Vector3(xc - 0.1f, yTope + 0.23f, zc - 0.12f), new Vector3(0.03f, 0.22f, 0.03f), grisOscuro);
        kit.Caja(new Vector3(xc - 0.1f, yTope + 0.37f, zc - 0.12f), new Vector3(0.24f, 0.13f, 0.03f), grisOscuro);
        kit.Etiqueta(new Vector3(xc - 0.1f, yTope + 0.37f, zc - 0.136f), Vector3.back, 0.05f, 0.1f, azulClaro);
        kit.Caja(new Vector3(xc + 0.33f, yTope + 0.06f, zc + 0.05f), new Vector3(0.16f, 0.12f, 0.2f), blanco);
        kit.Caja(new Vector3(xc + 0.33f, yTope + 0.125f, zc - 0.02f), new Vector3(0.08f, 0.01f, 0.06f), new Color(0.97f, 0.97f, 0.95f), false); // papel
        // Datáfono
        kit.Caja(new Vector3(xc - 0.45f, yTope + 0.02f, zc - 0.22f), new Vector3(0.09f, 0.04f, 0.16f), negro);
        kit.Piso(new Vector3(xc - 0.45f, yTope + 0.041f, zc - 0.25f), 0.03f, 0.025f, azulClaro);

        // Exhibidor de dulces de dos pisos, con cajitas de colores.
        float xd = b.center.x - 0.85f;
        kit.Caja(new Vector3(xd, yTope + 0.04f, zc - 0.15f), new Vector3(0.5f, 0.08f, 0.22f), blanco);
        kit.Caja(new Vector3(xd, yTope + 0.14f, zc - 0.06f), new Vector3(0.5f, 0.12f, 0.12f), blanco);
        kit.grosorContorno = 0.5f;
        for (int i = 0; i < 6; i++)
        {
            kit.Caja(new Vector3(xd - 0.2f + i * 0.08f, yTope + 0.11f, zc - 0.19f), new Vector3(0.06f, 0.06f, 0.08f), coloresProductos[i % coloresProductos.Length]);
            kit.Caja(new Vector3(xd - 0.2f + i * 0.08f, yTope + 0.23f, zc - 0.07f), new Vector3(0.06f, 0.06f, 0.07f), coloresProductos[(i + 3) % coloresProductos.Length]);
        }
        kit.grosorContorno = 1f;

        // Gel antibacterial y matera con planta.
        float xg = b.center.x - 0.3f;
        kit.Cilindro(new Vector3(xg, yTope + 0.08f, zc - 0.2f), 0.04f, 0.16f, Vector3.up, 12, blanco, true, true);
        kit.Cilindro(new Vector3(xg, yTope + 0.18f, zc - 0.2f), 0.012f, 0.04f, Vector3.up, 8, azul);
        kit.Caja(new Vector3(xg, yTope + 0.2f, zc - 0.22f), new Vector3(0.015f, 0.012f, 0.05f), azul);
        float xm = b.min.x + 0.2f;
        kit.Cilindro(new Vector3(xm, yTope + 0.08f, zc + 0.1f), 0.08f, 0.16f, Vector3.up, 14, terracota, true, true);
        kit.Esfera(new Vector3(xm, yTope + 0.26f, zc + 0.1f), new Vector3(0.14f, 0.16f, 0.14f), 2, hojas, true, false, true);

        // Repisa con medicamentos en la pared de atrás (sobre el mostrador, debajo del letrero).
        float zPared = FachadaYExterior.Fondo - 0.1f;
        var rnd = new System.Random(77);
        foreach (float y in new[] { 1.3f, 1.68f })
        {
            kit.Caja(new Vector3(0f, y, zPared - 0.1f), new Vector3(3.0f, 0.025f, 0.2f), blanco);
            kit.grosorContorno = 0.5f;
            for (float x = -1.42f; x < 1.38f;)
            {
                float ancho = Rango(rnd, 0.06f, 0.12f), alto = Rango(rnd, 0.1f, 0.24f);
                kit.Caja(new Vector3(x + ancho / 2f, y + 0.0125f + alto / 2f, zPared - 0.1f), new Vector3(ancho, alto, 0.14f),
                    coloresProductos[rnd.Next(coloresProductos.Length)]);
                x += ancho + 0.01f;
            }
            kit.grosorContorno = 1f;
        }
        foreach (float x in new[] { -1.45f, 0f, 1.45f })
            kit.Caja(new Vector3(x, 1.49f, zPared - 0.015f), new Vector3(0.03f, 0.42f, 0.03f), grisOscuro);

        // Letrero "CAJA" colgando del techo, sobre la registradora.
        kit.Caja(new Vector3(xc, 2.5f, zc - 0.2f), new Vector3(0.7f, 0.24f, 0.04f), verde);
        kit.Caja(new Vector3(xc - 0.3f, 2.81f, zc - 0.2f), new Vector3(0.01f, 0.38f, 0.01f), grisOscuro, false);
        kit.Caja(new Vector3(xc + 0.3f, 2.81f, zc - 0.2f), new Vector3(0.01f, 0.38f, 0.01f), grisOscuro, false);
        kit.CrearObjeto("Kit_Mostrador", raiz, mat, carpetaMallas);

        if (Resources.Load<TMP_Settings>("TMP Settings") != null)
            Texto(raiz, "CAJA", new Vector3(xc, 2.5f, zc - 0.225f), new Vector2(0.6f, 0.2f), Color.white);
    }

    static void ConstruirTecho(Transform raiz, Material matTecho, Material matLampara)
    {
        var techo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        techo.name = "Techo";
        techo.transform.SetParent(raiz, false);
        float fondo = FachadaYExterior.Fondo;
        techo.transform.position = new Vector3(0f, 3.05f, (fondo - 5f) / 2f);
        techo.transform.localScale = new Vector3(10f, 0.1f, fondo + 5f);
        techo.GetComponent<Renderer>().sharedMaterial = matTecho; // conserva su collider: el personaje no se escapa por arriba

        var lamparas = new KitMalla();
        foreach (float x in new[] { -3.6f, -1.25f, 1.25f, 3.6f })
            foreach (float z in new[] { -1.2f, 2.0f, 5.2f })
                lamparas.Caja(new Vector3(x, 2.985f, z), new Vector3(0.35f, 0.03f, 1.4f), Color.white, false);
        lamparas.CrearObjeto("Lamparas", raiz, matLampara, carpetaMallas);
    }

    static bool ConstruirDecoracion(Transform raiz, Material mat)
    {
        var kit = new KitMalla();

        // Zócalo azul solo en la pared del fondo (en los lados peleaba con los estantes y parpadeaba).
        float zFondo = FachadaYExterior.Fondo - 0.11f;           // cara interior de la pared del fondo
        float largoLados = FachadaYExterior.Fondo + 5f - 0.4f;    // largo de las paredes laterales por dentro
        float centroLados = (FachadaYExterior.Fondo - 5f) / 2f;
        kit.Caja(new Vector3(0f, 0.07f, zFondo), new Vector3(9.6f, 0.14f, 0.02f), azul);
        // Franja azul alta en las cuatro paredes.
        kit.Caja(new Vector3(0f, 2.8f, zFondo), new Vector3(9.6f, 0.12f, 0.02f), azul);
        kit.Caja(new Vector3(0f, 2.8f, -4.89f), new Vector3(9.6f, 0.12f, 0.02f), azul);
        kit.Caja(new Vector3(4.89f, 2.8f, centroLados), new Vector3(0.02f, 0.12f, largoLados), azul);
        kit.Caja(new Vector3(-4.89f, 2.8f, centroLados), new Vector3(0.02f, 0.12f, largoLados), azul);

        // Tapete de entrada
        kit.Caja(new Vector3(0f, 0.006f, -4.3f), new Vector3(1.6f, 0.012f, 0.9f), azul);

        // Letrero principal sobre el mostrador
        kit.Caja(new Vector3(0f, 2.35f, zFondo - 0.02f), new Vector3(3.4f, 0.7f, 0.06f), blanco);
        kit.Caja(new Vector3(-1.35f, 2.35f, zFondo - 0.06f), new Vector3(0.36f, 0.12f, 0.03f), verde);
        kit.Caja(new Vector3(-1.35f, 2.35f, zFondo - 0.06f), new Vector3(0.12f, 0.36f, 0.03f), verde);

        // Letrero exterior sobre la vitrina (con cruz verde) y tablero de instrucciones en el andén
        kit.CajaMinMax(new Vector3(-3.2f, 2.55f, -5.23f), new Vector3(3.2f, 3.05f, -5.13f), azul);
        kit.Caja(new Vector3(-2.75f, 2.8f, -5.245f), new Vector3(0.34f, 0.11f, 0.03f), verde);
        kit.Caja(new Vector3(-2.75f, 2.8f, -5.245f), new Vector3(0.11f, 0.34f, 0.03f), verde);
        kit.Caja(new Vector3(1.8f, 1.35f, -6.1f), new Vector3(1.0f, 0.72f, 0.04f), blanco);
        kit.Caja(new Vector3(1.8f, 1.6f, -6.123f), new Vector3(0.98f, 0.18f, 0.006f), azul, false);
        kit.Caja(new Vector3(1.4f, 0.5f, -6.1f), new Vector3(0.05f, 1.0f, 0.05f), grisOscuro);
        kit.Caja(new Vector3(2.2f, 0.5f, -6.1f), new Vector3(0.05f, 1.0f, 0.05f), grisOscuro);

        // Letreros colgantes de pasillo
        float[] xs = { -3.6f, -1.25f, 1.25f, 3.6f };
        string[] textos = { "Cuidado personal", "Medicamentos", "Bebé y mamá", "Vitaminas" };
        foreach (float x in xs)
        {
            kit.Caja(new Vector3(x, 2.55f, -2.4f), new Vector3(1.2f, 0.32f, 0.04f), azul);
            kit.Caja(new Vector3(x - 0.5f, 2.855f, -2.4f), new Vector3(0.01f, 0.29f, 0.01f), grisOscuro, false);
            kit.Caja(new Vector3(x + 0.5f, 2.855f, -2.4f), new Vector3(0.01f, 0.29f, 0.01f), grisOscuro, false);
        }
        kit.CrearObjeto("Kit_Decoracion", raiz, mat, carpetaMallas);

        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            Debug.LogWarning("FarmaciaVR: falta TextMeshPro (Window > TextMeshPro > Import TMP Essential Resources). Creé los letreros sin texto.");
            return false;
        }
        var contenedor = new GameObject("Letreros");
        contenedor.transform.SetParent(raiz, false);
        Texto(contenedor.transform, "Farma-CIA Agencia", new Vector3(0.25f, 2.35f, FachadaYExterior.Fondo - 0.165f), new Vector2(2.6f, 0.5f), azul);

        // Afuera: letrero grande sobre la entrada y tablero de instrucciones junto a la puerta.
        Texto(contenedor.transform, "Farma-CIA Agencia", new Vector3(0.3f, 2.8f, -5.235f), new Vector2(5.0f, 0.4f), Color.white);
        Texto(contenedor.transform, "¡Encuentra al personaje escondido!", new Vector3(1.8f, 1.6f, -6.13f), new Vector2(0.9f, 0.12f), Color.white);
        Texto(contenedor.transform, "Da una palmada para empezar.\nAdentro, búscalo con la mirada.", new Vector3(1.8f, 1.28f, -6.128f), new Vector2(0.88f, 0.36f), azul);
        for (int i = 0; i < xs.Length; i++)
            Texto(contenedor.transform, textos[i], new Vector3(xs[i], 2.55f, -2.425f), new Vector2(1.1f, 0.24f), Color.white);
        return true;
    }

    static void Texto(Transform padre, string texto, Vector3 posicion, Vector2 tamano, Color color)
    {
        var go = new GameObject("Texto_" + texto, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = texto;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.1f;
        tmp.fontSizeMax = 30f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = tamano;
        go.transform.position = posicion;
        go.transform.rotation = Quaternion.identity; // se lee mirando hacia el norte (+Z), como el jugador
    }

    // ================= Teletransporte y escondites =================

    static void ConstruirPuntosTeletransporte(Material matPunto, Material matLinea)
    {
        var padre = GameObject.Find("PuntosTeletransporte");
        if (padre == null)
        {
            padre = new GameObject("PuntosTeletransporte");
            Undo.RegisterCreatedObjectUndo(padre, "Puntos de teletransporte");
        }
        var tele = padre.GetComponent<TeletransportePorPuntos>();
        if (tele == null)
            tele = Undo.AddComponent<TeletransportePorPuntos>(padre);
        Undo.RecordObject(tele, "Teletransporte");
        tele.materialLinea = matLinea;

        for (int i = padre.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(padre.transform.GetChild(i).gameObject);

        for (int i = 0; i < puntosTeletransporte.Length; i++)
        {
            var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(disco, "Puntos de teletransporte");
            disco.name = $"P{i + 1:00}";
            disco.transform.SetParent(padre.transform, false);
            disco.transform.position = puntosTeletransporte[i];
            disco.transform.localScale = new Vector3(0.4f, 0.01f, 0.4f);
            // El cilindro trae un collider de cápsula que sobresale; lo cambiamos por una caja plana.
            Object.DestroyImmediate(disco.GetComponent<Collider>());
            var caja = disco.AddComponent<BoxCollider>();
            caja.size = new Vector3(1f, 10f, 1f);
            caja.center = new Vector3(0f, 4f, 0f);
            disco.GetComponent<Renderer>().sharedMaterial = matPunto;
            disco.AddComponent<PuntoTeletransporte>();
        }

        // Apaga el teletransporte de Meta para que no compita con los discos.
        var teleportMeta = GameObject.Find("ISDK_TeleportInteraction");
        if (teleportMeta != null)
        {
            Undo.RecordObject(teleportMeta, "Apagar teletransporte de Meta");
            teleportMeta.SetActive(false);
        }
    }

    static void ConstruirEscondites(List<Vector3> puntos)
    {
        var padre = GameObject.Find("Escondites");
        if (padre == null)
        {
            padre = new GameObject("Escondites");
            Undo.RegisterCreatedObjectUndo(padre, "Escondites");
        }
        for (int i = padre.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(padre.transform.GetChild(i).gameObject);

        for (int i = 0; i < puntos.Count; i++)
        {
            var e = new GameObject(puntos[i].y < 0.1f ? $"Piso_{i + 1:00}" : $"Producto_{i + 1:00}");
            Undo.RegisterCreatedObjectUndo(e, "Escondites");
            e.transform.SetParent(padre.transform, false);
            e.transform.position = puntos[i];
            e.transform.rotation = Quaternion.LookRotation(DireccionDeEscondite(puntos[i]), Vector3.up);
        }

        var manager = Object.FindFirstObjectByType<JuegoManager>();
        if (manager != null && manager.escondites != padre.transform)
        {
            Undo.RecordObject(manager, "Escondites");
            manager.escondites = padre.transform;
            EditorUtility.SetDirty(manager);
        }
    }

    // El personaje mira hacia afuera del mueble (al pasillo) o, si está en el piso, hacia el centro.
    static Vector3 DireccionDeEscondite(Vector3 punto)
    {
        if (punto.y >= 0.1f)
        {
            float[] centrosMuebles = { -4.6f, -2.5f, 0f, 2.5f, 4.6f };
            float cercano = centrosMuebles.OrderBy(c => Mathf.Abs(c - punto.x)).First();
            return new Vector3(Mathf.Sign(punto.x - cercano + 0.0001f), 0f, 0f);
        }
        Vector3 haciaCentro = new Vector3(-punto.x, 0f, -punto.z);
        return haciaCentro.sqrMagnitude > 0.01f ? haciaCentro.normalized : Vector3.forward;
    }

    // ================= Personaje =================

    // Mide el cubo agarrable de Meta (el de la primera prueba) para que el personaje quede del mismo tamaño,
    // y luego lo borra porque ya no se necesita. Si ya no existe, usa el tamaño del pájaro actual.
    static float MedirYBorrarCubo()
    {
        var cubo = GameObject.Find("[BuildingBlock] Cube");
        if (cubo == null)
        {
            var actual = Object.FindFirstObjectByType<PersonajeEncontrable>();
            return actual != null ? Mathf.Clamp(actual.transform.localScale.x / 1.3f, 0.06f, 0.3f) : 0.1f;
        }
        float tam = 0.1f;
        // Solo mallas normales (no las manos fantasma de Meta que pueden venir dentro del bloque).
        var renders = cubo.GetComponentsInChildren<MeshRenderer>();
        if (renders.Length > 0)
        {
            Bounds b = renders[0].bounds;
            foreach (var r in renders)
                b.Encapsulate(r.bounds);
            tam = Mathf.Clamp(Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)), 0.06f, 0.3f);
        }
        Undo.DestroyObjectImmediate(cubo);
        Debug.Log("FarmaciaVR: borré el cubo de prueba ([BuildingBlock] Cube).");
        return tam;
    }

    // Crea el personaje: un pájaro rojo low poly (inspirado en la referencia), del tamaño del cubo agarrable.
    // Está hecho en una "unidad" de 1 de alto mirando hacia +Z y luego se escala.
    static void ConstruirPajaro(Material mat, Material matPluma, Mesh mallaPluma, Material matHalo, float tam)
    {
        var manager = Object.FindFirstObjectByType<JuegoManager>();
        var actual = Object.FindFirstObjectByType<PersonajeEncontrable>();
        Vector3 posicion = actual != null ? actual.transform.position : new Vector3(0.5f, 1.2f, 3.8f);
        float escala = tam * 1.3f; // un poquito más grande que el cubo, para que se vea la cara

        Color rojo = new Color(0.86f, 0.11f, 0.11f);
        Color barriga = new Color(0.98f, 0.74f, 0.68f);
        Color blancoOjo = new Color(0.98f, 0.98f, 0.97f);
        Color negro = new Color(0.08f, 0.07f, 0.07f);
        Color cafeIris = new Color(0.5f, 0.28f, 0.12f);
        Color naranja = new Color(1f, 0.62f, 0.12f);
        Color naranjaOscuro = new Color(0.92f, 0.48f, 0.08f);
        Color boca = new Color(0.55f, 0.12f, 0.12f);
        Color overol = new Color(0.80f, 0.63f, 0.42f);
        Color overolOscuro = new Color(0.64f, 0.47f, 0.30f);
        Color azulPollito = new Color(0.35f, 0.65f, 0.95f);
        Color azulClaro = new Color(0.58f, 0.82f, 1f);
        Color amarilloGafas = new Color(1f, 0.85f, 0.1f);

        var cuerpo = new KitMalla();
        // Cuerpo en forma de huevo y barriga clara
        cuerpo.Esfera(new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.55f, 0.47f), 3, rojo);
        cuerpo.Esfera(new Vector3(0f, 0.42f, 0.22f), new Vector3(0.36f, 0.33f, 0.27f), 3, barriga);
        cuerpo.Esfera(new Vector3(0f, 0.36f, -0.45f), new Vector3(0.1f, 0.08f, 0.1f), 1, rojo); // colita

        // Overol café: peto, tirantes con botones, estrella y bolsillo
        cuerpo.Esfera(new Vector3(0f, 0.27f, 0.27f), new Vector3(0.38f, 0.22f, 0.24f), 2, overol);
        foreach (float s in new[] { -1f, 1f })
        {
            cuerpo.Caja(new Vector3(s * 0.2f, 0.6f, 0.43f), new Vector3(0.07f, 0.24f, 0.04f), overol);
            cuerpo.grosorContorno = 0.5f;
            cuerpo.Esfera(new Vector3(s * 0.2f, 0.49f, 0.45f), new Vector3(0.025f, 0.025f, 0.015f), 1, overolOscuro);
            cuerpo.grosorContorno = 1f;
        }
        Estrella(cuerpo, new Vector3(0.2f, 0.31f, 0.478f), 0.05f, Color.white);
        cuerpo.Caja(new Vector3(0f, 0.2f, 0.51f), new Vector3(0.26f, 0.12f, 0.04f), overolOscuro);

        // Ojos grandes con iris café, pupila y brillo
        foreach (float s in new[] { -1f, 1f })
        {
            cuerpo.Esfera(new Vector3(s * 0.15f, 0.64f, 0.37f), new Vector3(0.14f, 0.16f, 0.1f), 2, blancoOjo);
            cuerpo.grosorContorno = 0.5f;
            cuerpo.Esfera(new Vector3(s * 0.125f, 0.63f, 0.455f), new Vector3(0.065f, 0.075f, 0.03f), 1, cafeIris);
            cuerpo.Esfera(new Vector3(s * 0.12f, 0.63f, 0.475f), new Vector3(0.035f, 0.04f, 0.02f), 1, negro);
            cuerpo.Esfera(new Vector3(s * 0.1f, 0.665f, 0.49f), new Vector3(0.013f, 0.013f, 0.008f), 1, Color.white, false);
            cuerpo.grosorContorno = 1f;
            // Cejas gruesas, más bajas hacia el centro
            float xi = s * 0.03f, xe = s * 0.32f;
            cuerpo.Hexaedro(new[]
            {
                new Vector3(xi, 0.76f, 0.48f), new Vector3(xe, 0.82f, 0.40f), new Vector3(xe, 0.82f, 0.32f), new Vector3(xi, 0.76f, 0.41f),
                new Vector3(xi, 0.86f, 0.48f), new Vector3(xe, 0.91f, 0.40f), new Vector3(xe, 0.91f, 0.32f), new Vector3(xi, 0.86f, 0.41f),
            }, negro);
            cuerpo.Esfera(new Vector3(s * 0.16f, 0.03f, 0.13f), new Vector3(0.1f, 0.04f, 0.15f), 1, naranja); // patas
        }

        // Pico abierto (de arriba y de abajo) con la boca adentro
        Vector3 punta = new Vector3(0f, 0.5f, 0.73f);
        cuerpo.Hexaedro(new[]
        {
            new Vector3(-0.11f, 0.45f, 0.43f), new Vector3(0.11f, 0.45f, 0.43f), new Vector3(0.11f, 0.45f, 0.5f), new Vector3(-0.11f, 0.45f, 0.5f),
            new Vector3(-0.1f, 0.58f, 0.43f), new Vector3(0.1f, 0.58f, 0.43f), punta, punta,
        }, naranja);
        Vector3 puntaAbajo = new Vector3(0f, 0.4f, 0.62f);
        cuerpo.Hexaedro(new[]
        {
            new Vector3(-0.08f, 0.35f, 0.43f), new Vector3(0.08f, 0.35f, 0.43f), puntaAbajo, puntaAbajo,
            new Vector3(-0.09f, 0.42f, 0.43f), new Vector3(0.09f, 0.42f, 0.43f), new Vector3(0.09f, 0.42f, 0.5f), new Vector3(-0.09f, 0.42f, 0.5f),
        }, naranjaOscuro);
        cuerpo.Esfera(new Vector3(0f, 0.435f, 0.47f), new Vector3(0.08f, 0.03f, 0.04f), 1, boca, false);

        // Copete de plumas
        cuerpo.Esfera(new Vector3(0f, 1.06f, 0.02f), new Vector3(0.05f, 0.13f, 0.05f), 1, rojo);
        cuerpo.Esfera(new Vector3(0.07f, 1.02f, -0.03f), new Vector3(0.04f, 0.1f, 0.04f), 1, rojo);
        cuerpo.Esfera(new Vector3(-0.06f, 1.0f, -0.05f), new Vector3(0.035f, 0.08f, 0.035f), 1, rojo);

        // El hijo: pollito azul con gafas amarillas, asomado en el bolsillo
        cuerpo.grosorContorno = 0.4f;
        cuerpo.Esfera(new Vector3(0f, 0.31f, 0.5f), new Vector3(0.085f, 0.085f, 0.08f), 2, azulPollito);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            cuerpo.Esfera(new Vector3(x, 0.4f + (x == 0f ? 0.01f : 0f), 0.5f), new Vector3(0.015f, 0.04f, 0.015f), 1, azulClaro);
        foreach (float s in new[] { -1f, 1f })
        {
            cuerpo.Cilindro(new Vector3(s * 0.033f, 0.33f, 0.572f), 0.036f, 0.01f, Vector3.forward, 12, amarilloGafas);
            cuerpo.Esfera(new Vector3(s * 0.033f, 0.33f, 0.578f), new Vector3(0.028f, 0.03f, 0.014f), 1, blancoOjo, false);
            cuerpo.Esfera(new Vector3(s * 0.03f, 0.33f, 0.59f), new Vector3(0.012f, 0.013f, 0.006f), 1, negro, false);
        }
        cuerpo.Caja(new Vector3(0f, 0.335f, 0.578f), new Vector3(0.02f, 0.008f, 0.006f), amarilloGafas, false);
        cuerpo.Esfera(new Vector3(0f, 0.302f, 0.585f), new Vector3(0.013f, 0.01f, 0.016f), 1, naranja, false);
        cuerpo.grosorContorno = 1f;

        var ala = new KitMalla();
        ala.Esfera(new Vector3(0f, -0.17f, 0f), new Vector3(0.07f, 0.2f, 0.15f), 2, rojo);

        var nuevo = new GameObject("Personaje");
        Undo.RegisterCreatedObjectUndo(nuevo, "Personaje");
        nuevo.transform.position = posicion;
        nuevo.transform.localScale = Vector3.one * escala;
        var colision = nuevo.AddComponent<SphereCollider>();
        colision.center = new Vector3(0f, 0.5f, 0f);
        colision.radius = 0.5f;

        var goCuerpo = cuerpo.CrearObjeto("PajaroCuerpo", nuevo.transform, mat, carpetaMallas);
        goCuerpo.name = "Cuerpo";
        var mallaAla = ala.GuardarComo($"{carpetaMallas}/PajaroAla.asset");
        Transform CrearAla(string nombre, float lado)
        {
            var hombro = new GameObject(nombre).transform;
            hombro.SetParent(nuevo.transform, false);
            hombro.localPosition = new Vector3(lado * 0.44f, 0.62f, -0.02f);
            var malla = new GameObject("Malla");
            malla.transform.SetParent(hombro, false);
            malla.AddComponent<MeshFilter>().sharedMesh = mallaAla;
            malla.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return hombro;
        }

        var cuerpoFisico = nuevo.AddComponent<Rigidbody>();
        cuerpoFisico.isKinematic = true;
        cuerpoFisico.useGravity = false;
        var animacion = nuevo.AddComponent<AnimacionPajaro>();
        animacion.alaIzquierda = CrearAla("AlaIzquierda", -1f);
        animacion.alaDerecha = CrearAla("AlaDerecha", 1f);
        var encontrable = nuevo.AddComponent<PersonajeEncontrable>();
        nuevo.AddComponent<AgarreAntigravedad>();
        var plumas = nuevo.AddComponent<PistaPlumas>();
        plumas.materialPluma = matPluma;
        plumas.mallaPluma = mallaPluma;
        var halo = nuevo.AddComponent<HaloPajaro>();
        halo.material = matHalo;

        if (manager != null)
        {
            Undo.RecordObject(manager, "Conectar personaje");
            manager.personaje = encontrable;
            EditorUtility.SetDirty(manager);
        }
        if (actual != null)
            Undo.DestroyObjectImmediate(actual.gameObject);
    }

    // Estrella plana de 5 puntas (mirando hacia +Z), para el parche del overol.
    static void Estrella(KitMalla k, Vector3 centro, float radio, Color color)
    {
        var puntas = new Vector3[10];
        for (int i = 0; i < 10; i++)
        {
            float r = i % 2 == 0 ? radio : radio * 0.45f;
            float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
            puntas[i] = centro + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        }
        for (int i = 0; i < 10; i++)
            k.Triangulo(centro, puntas[i], puntas[(i + 1) % 10], Vector3.forward, color, centro, false);
    }

    // ================= Cronómetro y opciones =================

    // Cronómetro pegado a la vista (como unas gafas XR), a la derecha. Se dibuja encima de todo,
    // con un fondo sólido (no transparente) y un borde claro para que se lea bien.
    static void ConstruirCronometro()
    {
        var manager = Object.FindFirstObjectByType<JuegoManager>();
        var rig = Object.FindFirstObjectByType<OVRCameraRig>();
        if (manager == null || rig == null || rig.centerEyeAnchor == null || Resources.Load<TMP_Settings>("TMP Settings") == null)
            return;

        var viejo = rig.centerEyeAnchor.Find("Cronometro");
        if (viejo != null)
            Undo.DestroyObjectImmediate(viejo.gameObject);

        var go = new GameObject("Cronometro", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Cronómetro");
        go.transform.SetParent(rig.centerEyeAnchor, false);
        go.transform.localPosition = new Vector3(0.2f, -0.1f, 0.7f);
        go.transform.localRotation = Quaternion.Euler(0f, 12f, 0f); // un poco girado hacia el centro de la vista
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "90";
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.05f;
        tmp.fontSizeMax = 20f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(0.075f, 0.042f);

        // Fondo azul oscuro sólido con borde blanco (dos rectángulos detrás del número).
        var matBorde = CrearMaterialPlano("Plano_CronometroBorde", Color.white, false, false, 3998, true);
        var matFondo = CrearMaterialPlano("Plano_CronometroFondo", new Color(0.09f, 0.12f, 0.22f, 1f), false, false, 3999, true);
        void Rectangulo(string nombre, Vector2 tam, float atras, Material m)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = nombre;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(go.transform, false);
            q.transform.localPosition = new Vector3(0f, 0f, atras);
            q.transform.localScale = new Vector3(tam.x, tam.y, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Rectangulo("Borde", new Vector2(0.098f, 0.058f), 0.002f, matBorde);
        Rectangulo("Fondo", new Vector2(0.09f, 0.05f), 0.001f, matFondo);

        // Material que se dibuja por encima de paredes y muebles.
        var overlay = Shader.Find("TextMeshPro/Distance Field Overlay");
        if (overlay != null && tmp.fontSharedMaterial != null)
        {
            string ruta = $"{carpetaMateriales}/TMP_Cronometro.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                mat = new Material(tmp.fontSharedMaterial) { shader = overlay };
                AssetDatabase.CreateAsset(mat, ruta);
            }
            tmp.fontSharedMaterial = mat;
        }

        Undo.RecordObject(manager, "Cronómetro");
        manager.textoTiempo = tmp;
        EditorUtility.SetDirty(manager);
        go.SetActive(false);
    }

    // Tres botones (se tocan con el dedo) para elegir la navegación antes de entrar.
    static void ConstruirOpcionesNavegacion(Transform raiz, Material matBoton)
    {
        var manager = Object.FindFirstObjectByType<JuegoManager>();
        if (manager == null)
            return;

        var opciones = new GameObject("OpcionesNavegacion");
        opciones.transform.SetParent(raiz, false);
        opciones.transform.position = new Vector3(0f, 1.2f, -6.8f);

        Renderer CrearBoton(string nombre, string texto, float x)
        {
            var boton = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boton.name = nombre;
            boton.transform.SetParent(opciones.transform, false);
            boton.transform.localPosition = new Vector3(x, 0f, 0f);
            boton.transform.localScale = new Vector3(0.17f, 0.07f, 0.025f);
            boton.GetComponent<Renderer>().sharedMaterial = matBoton;
            boton.AddComponent<BotonTocable>();
            Texto(opciones.transform, texto, Vector3.zero, new Vector2(0.15f, 0.05f), new Color(0.1f, 0.12f, 0.2f));
            var etiqueta = opciones.transform.GetChild(opciones.transform.childCount - 1);
            etiqueta.localPosition = new Vector3(x, 0f, -0.014f);
            etiqueta.localRotation = Quaternion.identity;
            return boton.GetComponent<Renderer>();
        }

        if (Resources.Load<TMP_Settings>("TMP Settings") != null)
        {
            Texto(opciones.transform, "¿Cómo te quieres mover?", Vector3.zero, new Vector2(0.5f, 0.05f), Color.white);
            var titulo = opciones.transform.GetChild(opciones.transform.childCount - 1);
            titulo.localPosition = new Vector3(0f, 0.075f, 0f);
            titulo.localRotation = Quaternion.identity;
        }
        var botonPuntos = CrearBoton("BotonDedo", "Con el dedo", -0.19f);
        var botonMeta = CrearBoton("BotonMeta", "Modo Meta", 0f);
        var botonCaminar = CrearBoton("BotonCaminar", "Caminar", 0.19f);

        // El que decide qué navegación está activa vive en el JuegoManager.
        var modo = manager.GetComponent<ModoNavegacion>();
        if (modo == null)
            modo = Undo.AddComponent<ModoNavegacion>(manager.gameObject);
        Undo.RecordObject(modo, "Navegación");
        var puntos = GameObject.Find("PuntosTeletransporte");
        modo.discos = puntos;
        modo.teletransportePuntos = puntos != null ? puntos.GetComponent<TeletransportePorPuntos>() : null;
        modo.teletransporteMeta = BuscarAunqueEsteApagado("ISDK_TeleportInteraction");
        // Nuestro teletransporte con arco (estilo Meta)
        var arcoGo = new GameObject("TeletransporteArco");
        arcoGo.transform.SetParent(raiz, false);
        var arco = arcoGo.AddComponent<TeletransporteArco>();
        arco.materialLinea = AssetDatabase.LoadAssetAtPath<Material>($"{carpetaMateriales}/Mat_LineaTeleport.mat");
        arco.zonaPermitida = new Vector4(-4.8f, 4.8f, -4.8f, FachadaYExterior.Fondo - 0.2f);
        arco.enabled = false;
        modo.teletransporteArco = arco;
        // Caminar de forma continua hacia donde señala el dedo
        var caminarGo = new GameObject("CaminarConDedo");
        caminarGo.transform.SetParent(raiz, false);
        var caminar = caminarGo.AddComponent<CaminarConDedo>();
        caminar.zonaPermitida = arco.zonaPermitida;
        caminar.enabled = false;
        modo.caminar = caminar;
        modo.botonPuntos = botonPuntos;
        modo.botonMeta = botonMeta;
        modo.botonCaminar = botonCaminar;
        EditorUtility.SetDirty(modo);

        // Un componente recién agregado desde el editor puede traer su evento vacío (null): se crea antes de conectarlo.
        var tocarPuntos = botonPuntos.GetComponent<BotonTocable>();
        var tocarMeta = botonMeta.GetComponent<BotonTocable>();
        var tocarCaminar = botonCaminar.GetComponent<BotonTocable>();
        if (tocarPuntos.alTocar == null) tocarPuntos.alTocar = new UnityEngine.Events.UnityEvent();
        if (tocarMeta.alTocar == null) tocarMeta.alTocar = new UnityEngine.Events.UnityEvent();
        if (tocarCaminar.alTocar == null) tocarCaminar.alTocar = new UnityEngine.Events.UnityEvent();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(tocarPuntos.alTocar, modo.ElegirPuntos);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(tocarMeta.alTocar, modo.ElegirMeta);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(tocarCaminar.alTocar, modo.ElegirCaminar);

        ArreglarSuperficieTeletransporteMeta(modo.teletransporteMeta);

        Undo.RecordObject(manager, "Opciones");
        manager.opcionesNavegacion = opciones;
        EditorUtility.SetDirty(manager);
        opciones.SetActive(false);
    }

    static GameObject BuscarAunqueEsteApagado(string nombre)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == nombre)
                return t.gameObject;
        return null;
    }

    // El teletransporte de Meta apuntaba siempre al centro: su "superficie" no tenía collider asignado.
    // Aquí se le asigna el collider del piso.
    static void ArreglarSuperficieTeletransporteMeta(GameObject teleport)
    {
        var piso = GameObject.Find("Plane");
        var colliderPiso = piso != null ? piso.GetComponent<Collider>() : null;
        if (teleport == null || colliderPiso == null)
            return;
        foreach (var comp in teleport.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (comp == null || comp.GetType().Name != "ColliderSurface")
                continue;
            var so = new SerializedObject(comp);
            var prop = so.FindProperty("_collider");
            if (prop == null)
                continue;
            if (prop.objectReferenceValue != colliderPiso)
                Debug.Log($"FarmaciaVR: superficie del teletransporte de Meta: '{(prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "None")}' → piso.");
            prop.objectReferenceValue = colliderPiso;
            so.ApplyModifiedProperties();
        }
    }

    static Mesh CrearMallaPluma()
    {
        var kit = new KitMalla();
        kit.Caja(Vector3.zero, new Vector3(1f, 0.05f, 0.3f), Color.white, false);
        return kit.GuardarComo($"{carpetaMallas}/Pluma.asset");
    }

    // ================= Utilidades =================

    static Material CrearMaterial(string nombre, string shader, Color color, Color sombra, int modoContorno, bool coloresMalla)
    {
        var m = ObtenerMaterial(nombre, shader);
        m.SetColor("_BaseColor", color);
        m.SetColor("_ShadowColor", sombra);
        m.SetFloat("_UseVertexColor", coloresMalla ? 1f : 0f);
        m.SetColor("_OutlineColor", colorContorno);
        m.SetFloat("_OutlineWidth", shader == "FarmaciaVR/Toon" ? 2f : 0f);
        m.SetFloat("_OutlineMode", modoContorno);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // Material de color plano (FarmaciaVR/Plano). "encima" = se dibuja por encima de todo (para el cronómetro).
    static Material CrearMaterialPlano(string nombre, Color color, bool coloresMalla, bool opaco, int cola, bool encima = false)
    {
        var m = ObtenerMaterial(nombre, "FarmaciaVR/Plano");
        m.SetColor("_BaseColor", color);
        m.SetFloat("_UseVertexColor", coloresMalla ? 1f : 0f);
        m.SetFloat("_ZWrite", opaco ? 1f : 0f);
        m.SetFloat("_ZTest", encima ? (float)UnityEngine.Rendering.CompareFunction.Always : (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        m.renderQueue = cola;
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material CrearMaterialSimple(string nombre, string shader, Color color)
    {
        var m = ObtenerMaterial(nombre, shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material ObtenerMaterial(string nombre, string shader)
    {
        string ruta = $"{carpetaMateriales}/{nombre}.mat";
        var s = Shader.Find(shader);
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(s);
            AssetDatabase.CreateAsset(m, ruta);
        }
        else
        {
            m.shader = s;
        }
        return m;
    }

    static void PonerMaterial(GameObject go, Material mat)
    {
        if (go == null)
            return;
        var r = go.GetComponent<Renderer>();
        if (r == null)
            return;
        Undo.RecordObject(r, "Material toon");
        r.sharedMaterial = mat;
        r.enabled = true;
    }

    static void OcultarRender(GameObject go)
    {
        if (go == null)
            return;
        var r = go.GetComponent<MeshRenderer>();
        if (r == null)
            return;
        Undo.RecordObject(r, "Ocultar bloque");
        r.enabled = false;
    }

    // Los bloques de la maqueta son cubos de Unity (1x1x1) sin rotar: sus límites salen del transform.
    static Bounds LimitesDeBloque(Transform t)
    {
        return new Bounds(t.position, t.lossyScale);
    }

    static float Rango(System.Random rnd, float min, float max)
    {
        return min + (float)rnd.NextDouble() * Mathf.Max(0f, max - min);
    }

    static void AsegurarCarpeta(string ruta)
    {
        var partes = ruta.Split('/');
        string actual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string siguiente = actual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(siguiente))
                AssetDatabase.CreateFolder(actual, partes[i]);
            actual = siguiente;
        }
    }
}
#endif

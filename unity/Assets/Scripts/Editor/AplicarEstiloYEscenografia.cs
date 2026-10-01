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
        p.Add(new Vector3(-3.8f, 0.01f, 4.2f));
        p.Add(new Vector3(-2.3f, 0.01f, 4.1f));
        p.Add(new Vector3(2.3f, 0.01f, 4.1f));
        p.Add(new Vector3(3.8f, 0.01f, 4.2f));
        return p.ToArray();
    }

    // Escondites en el piso (los de encima de productos se calculan al armar los muebles).
    static readonly Vector3[] escondidesPiso =
    {
        new Vector3(-3.05f, 0.05f, -0.2f), new Vector3(-1.7f, 0.05f, 2.2f), new Vector3(-0.8f, 0.05f, -1.6f),
        new Vector3(0.8f, 0.05f, 0.3f), new Vector3(1.9f, 0.05f, -0.4f), new Vector3(3.05f, 0.05f, 2.1f),
        new Vector3(1.0f, 0.05f, 4.55f), new Vector3(-4.2f, 0.05f, 4.5f), new Vector3(4.2f, 0.05f, -4.4f),
        new Vector3(-4.2f, 0.05f, -4.4f),
    };

    [MenuItem("FarmaciaVR/★ Aplicar estilo toon y escenografía")]
    static void Aplicar()
    {
        foreach (var nombre in new[] { "FarmaciaVR/Toon", "FarmaciaVR/Toon Sin Borde", "FarmaciaVR/Vidrio", "FarmaciaVR/Luz" })
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
        var matPersonaje = CrearMaterial("Toon_Personaje", "FarmaciaVR/Toon", new Color(0.90f, 0.13f, 0.13f), new Color(0.75f, 0.70f, 0.80f), 1, false);
        var matPunto = CrearMaterial("Toon_Punto", "FarmaciaVR/Toon Sin Borde", new Color(0.2f, 0.8f, 1f), Color.white, 0, false);
        var matCielo = CrearMaterial("Toon_Cielo", "FarmaciaVR/Toon Sin Borde", Color.white, Color.white, 0, true);
        var matPluma = CrearMaterial("Toon_Pluma", "FarmaciaVR/Toon Sin Borde", Color.white, new Color(0.85f, 0.80f, 0.85f), 0, true);
        var matVidrio = CrearMaterialSimple("Toon_Vidrio", "FarmaciaVR/Vidrio", new Color(0.75f, 0.9f, 1f, 0.18f));
        var matLuz = CrearMaterialSimple("Toon_Luz", "FarmaciaVR/Luz", new Color(1f, 0.93f, 0.72f, 1f));
        var matLinea = CrearMaterialSimple("Mat_LineaTeleport", "Sprites/Default", Color.white);
        AssetDatabase.SaveAssets();

        float tamPersonaje = MedirCuboAgarrable();
        Vector3 dirLuz = Quaternion.Euler(rotacionSol) * Vector3.forward;

        // ---------- Raíz de la escenografía (se reconstruye cada vez) ----------
        var viejaRaiz = GameObject.Find(nombreRaiz);
        if (viejaRaiz != null)
            Undo.DestroyObjectImmediate(viejaRaiz);
        var raiz = new GameObject(nombreRaiz);
        Undo.RegisterCreatedObjectUndo(raiz, "Aplicar escenografía");

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
        FachadaYExterior.ConstruirExterior(raiz.transform, matKit, matCielo, dirLuz, carpetaMallas);

        // ---------- Teletransporte, escondites y personaje ----------
        ConstruirPuntosTeletransporte(matPunto, matLinea);
        ConstruirEscondites(escondites);
        var mallaPluma = CrearMallaPluma();
        ConstruirPersonaje(matPersonaje, matPluma, mallaPluma, tamPersonaje);

        // ---------- Luz y cámara ----------
        var luz = GameObject.Find("Directional Light");
        if (luz != null)
        {
            Undo.RecordObject(luz.transform, "Luz");
            luz.transform.rotation = Quaternion.Euler(rotacionSol);
        }
        var rig = Object.FindFirstObjectByType<OVRCameraRig>();
        var camara = rig != null && rig.centerEyeAnchor != null ? rig.centerEyeAnchor.GetComponent<Camera>() : null;
        if (camara != null && camara.farClipPlane < 1000f)
        {
            Undo.RecordObject(camara, "Cámara");
            camara.farClipPlane = 1000f; // para ver los cerros y el cielo
        }

        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log($"FarmaciaVR: listo ({escondites.Count} escondites, {puntosTeletransporte.Length} discos)" +
                  (conTextos ? "" : " — sin textos: falta TextMeshPro") + ". Guarda con Ctrl + S.");
        EditorUtility.DisplayDialog("FarmaciaVR", "¡Listo! Revisa la escena y guarda con Ctrl + S.", "OK");
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

    static void ConstruirMostrador(Transform raiz, Material mat)
    {
        var original = GameObject.Find("Mostrador");
        if (original == null)
            return;
        Bounds b = LimitesDeBloque(original.transform);
        OcultarRender(original); // el collider del mostrador se queda

        var kit = new KitMalla();
        kit.Caja(b.center, b.size - new Vector3(0.04f, 0.04f, 0.06f), azul);
        kit.Caja(new Vector3(b.center.x, b.max.y + 0.025f, b.center.z), new Vector3(b.size.x + 0.08f, 0.05f, b.size.z + 0.08f), blanco);
        float zFrente = b.min.z + 0.03f;
        kit.Caja(new Vector3(b.center.x, b.min.y + b.size.y * 0.72f, zFrente - 0.005f), new Vector3(b.size.x - 0.04f, 0.08f, 0.01f), blanco);
        kit.Caja(new Vector3(b.center.x, b.center.y - 0.08f, zFrente - 0.01f), new Vector3(0.30f, 0.10f, 0.02f), verde);
        kit.Caja(new Vector3(b.center.x, b.center.y - 0.08f, zFrente - 0.01f), new Vector3(0.10f, 0.30f, 0.02f), verde);
        float yTope = b.max.y + 0.05f;
        kit.Caja(new Vector3(b.center.x + 0.8f, yTope + 0.06f, b.center.z), new Vector3(0.35f, 0.12f, 0.30f), grisOscuro);
        kit.Caja(new Vector3(b.center.x + 0.8f, yTope + 0.24f, b.center.z + 0.08f), new Vector3(0.30f, 0.20f, 0.03f), grisOscuro);
        kit.Etiqueta(new Vector3(b.center.x + 0.8f, yTope + 0.24f, b.center.z + 0.08f - 0.016f), Vector3.back, 0.08f, 0.13f, azulClaro);
        kit.CrearObjeto("Kit_Mostrador", raiz, mat, carpetaMallas);
    }

    static void ConstruirTecho(Transform raiz, Material matTecho, Material matLampara)
    {
        var techo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        techo.name = "Techo";
        techo.transform.SetParent(raiz, false);
        techo.transform.position = new Vector3(0f, 3.05f, 0f);
        techo.transform.localScale = new Vector3(10f, 0.1f, 10f);
        techo.GetComponent<Renderer>().sharedMaterial = matTecho; // conserva su collider: el personaje no se escapa por arriba

        var lamparas = new KitMalla();
        foreach (float x in new[] { -3.6f, -1.25f, 1.25f, 3.6f })
            foreach (float z in new[] { -1.2f, 2.0f })
                lamparas.Caja(new Vector3(x, 2.985f, z), new Vector3(0.35f, 0.03f, 1.4f), Color.white, false);
        lamparas.CrearObjeto("Lamparas", raiz, matLampara, carpetaMallas);
    }

    static bool ConstruirDecoracion(Transform raiz, Material mat)
    {
        var kit = new KitMalla();

        // Zócalo azul solo en la pared del fondo (en los lados peleaba con los estantes y parpadeaba).
        kit.Caja(new Vector3(0f, 0.07f, 4.89f), new Vector3(9.6f, 0.14f, 0.02f), azul);
        // Franja azul alta en las cuatro paredes.
        kit.Caja(new Vector3(0f, 2.8f, 4.89f), new Vector3(9.6f, 0.12f, 0.02f), azul);
        kit.Caja(new Vector3(0f, 2.8f, -4.89f), new Vector3(9.6f, 0.12f, 0.02f), azul);
        kit.Caja(new Vector3(4.89f, 2.8f, 0f), new Vector3(0.02f, 0.12f, 9.6f), azul);
        kit.Caja(new Vector3(-4.89f, 2.8f, 0f), new Vector3(0.02f, 0.12f, 9.6f), azul);

        // Tapete de entrada
        kit.Caja(new Vector3(0f, 0.006f, -4.3f), new Vector3(1.6f, 0.012f, 0.9f), azul);

        // Letrero principal sobre el mostrador
        kit.Caja(new Vector3(0f, 2.35f, 4.87f), new Vector3(3.4f, 0.7f, 0.06f), blanco);
        kit.Caja(new Vector3(-1.35f, 2.35f, 4.83f), new Vector3(0.36f, 0.12f, 0.03f), verde);
        kit.Caja(new Vector3(-1.35f, 2.35f, 4.83f), new Vector3(0.12f, 0.36f, 0.03f), verde);

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
        Texto(contenedor.transform, "Farma-CIA Agencia", new Vector3(0.25f, 2.35f, 4.835f), new Vector2(2.6f, 0.5f), azul);

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
            e.transform.rotation = Quaternion.Euler(0f, (i * 47) % 360, 0f);
        }

        var manager = Object.FindFirstObjectByType<JuegoManager>();
        if (manager != null && manager.escondites != padre.transform)
        {
            Undo.RecordObject(manager, "Escondites");
            manager.escondites = padre.transform;
            EditorUtility.SetDirty(manager);
        }
    }

    // ================= Personaje =================

    // Mide el cubo agarrable de Meta para que el personaje quede del mismo tamaño.
    static float MedirCuboAgarrable()
    {
        var cubo = GameObject.Find("[BuildingBlock] Cube");
        if (cubo == null)
            return 0.1f;
        // Solo mallas normales (no las manos fantasma de Meta que pueden venir dentro del bloque).
        var renders = cubo.GetComponentsInChildren<MeshRenderer>();
        if (renders.Length == 0)
            return 0.1f;
        Bounds b = renders[0].bounds;
        foreach (var r in renders)
            b.Encapsulate(r.bounds);
        return Mathf.Clamp(Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)), 0.06f, 0.3f);
    }

    // Crea el personaje como un cubo propio (sin los scripts de agarre de Meta, porque usa el suyo).
    static void ConstruirPersonaje(Material mat, Material matPluma, Mesh mallaPluma, float tam)
    {
        var manager = Object.FindFirstObjectByType<JuegoManager>();
        var actual = Object.FindFirstObjectByType<PersonajeEncontrable>();
        Vector3 posicion = actual != null ? actual.transform.position : new Vector3(0.5f, 1.2f, 3.8f);

        var nuevo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(nuevo, "Personaje");
        nuevo.name = "Personaje";
        nuevo.transform.position = posicion;
        nuevo.transform.localScale = Vector3.one * tam;
        nuevo.GetComponent<Renderer>().sharedMaterial = mat;

        var cuerpo = nuevo.AddComponent<Rigidbody>();
        cuerpo.isKinematic = true;
        cuerpo.useGravity = false;
        var encontrable = nuevo.AddComponent<PersonajeEncontrable>();
        nuevo.AddComponent<AgarreAntigravedad>();
        var plumas = nuevo.AddComponent<PistaPlumas>();
        plumas.materialPluma = matPluma;
        plumas.mallaPluma = mallaPluma;

        if (manager != null)
        {
            Undo.RecordObject(manager, "Conectar personaje");
            manager.personaje = encontrable;
            EditorUtility.SetDirty(manager);
        }
        if (actual != null)
            Undo.DestroyObjectImmediate(actual.gameObject);
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

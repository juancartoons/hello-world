#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Menú de ayuda: FarmaciaVR > ★ Aplicar estilo toon y escenografía.
// Con un clic:
//  1. Crea los materiales toon (shader FarmaciaVR/Toon).
//  2. Construye góndolas y estantes con repisas y cientos de productos (fundidos en una sola malla por mueble).
//  3. Arma el mostrador, techo con lámparas, zócalos, franjas, tapete de entrada y letreros.
//  4. Convierte al personaje en una copia del cubo agarrable (mismo tamaño) con color rojo toon.
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

    [MenuItem("FarmaciaVR/★ Aplicar estilo toon y escenografía")]
    static void Aplicar()
    {
        if (Shader.Find("FarmaciaVR/Toon") == null || Shader.Find("FarmaciaVR/Toon Sin Borde") == null)
        {
            EditorUtility.DisplayDialog("Falta el shader",
                "No encuentro los shaders FarmaciaVR/Toon.\n\nCopia la carpeta 'Shaders' dentro de Assets, espera a que Unity compile y vuelve a usar este menú.",
                "Entendido");
            return;
        }

        AsegurarCarpeta(carpetaMateriales);
        AsegurarCarpeta(carpetaMallas);

        // ---------- Materiales ----------
        var matPared = CrearMaterial("Toon_Pared", true, new Color(0.97f, 0.96f, 0.93f), sombraSuave, 1, false);
        var matPiso = CrearMaterial("Toon_Piso", false, new Color(0.86f, 0.88f, 0.90f), sombraSuave, 0, false);
        var matTecho = CrearMaterial("Toon_Techo", false, blanco, new Color(0.93f, 0.94f, 0.96f), 0, false);
        var matLampara = CrearMaterial("Toon_Lampara", false, Color.white, Color.white, 0, false);
        var matKit = CrearMaterial("Toon_Kit", true, Color.white, sombraSuave, 2, true);
        var matPersonaje = CrearMaterial("Toon_Personaje", true, new Color(0.90f, 0.13f, 0.13f), new Color(0.75f, 0.70f, 0.80f), 1, false);
        var matPunto = CrearMaterial("Toon_Punto", false, new Color(0.2f, 0.8f, 1f), Color.white, 0, false);
        AssetDatabase.SaveAssets();

        // ---------- Raíz de la escenografía (se reconstruye cada vez) ----------
        var viejaRaiz = GameObject.Find(nombreRaiz);
        if (viejaRaiz != null)
            Undo.DestroyObjectImmediate(viejaRaiz);
        var raiz = new GameObject(nombreRaiz);
        Undo.RegisterCreatedObjectUndo(raiz, "Aplicar escenografía");

        // ---------- Paredes y piso ----------
        foreach (var nombre in new[] { "Pared_Norte", "Pared_Sur", "Pared_Este", "Pared_Oeste" })
            PonerMaterial(GameObject.Find(nombre), matPared);
        PonerMaterial(GameObject.Find("Plane"), matPiso);

        // ---------- Góndolas y estantes ----------
        int semilla = 1;
        foreach (var nombre in new[] { "Gondola_1", "Gondola_2", "Gondola_3" })
            ConstruirMueble(nombre, raiz.transform, matKit, true, 4, semilla++);
        foreach (var nombre in new[] { "Estante_Este", "Estante_Oeste" })
            ConstruirMueble(nombre, raiz.transform, matKit, false, 5, semilla++);

        // ---------- Mostrador ----------
        ConstruirMostrador(raiz.transform, matKit);

        // ---------- Techo, lámparas y decoración ----------
        ConstruirTecho(raiz.transform, matTecho, matLampara);
        var letreros = ConstruirDecoracion(raiz.transform, matKit);

        // ---------- Discos de teletransporte ----------
        var puntos = GameObject.Find("PuntosTeletransporte");
        if (puntos != null)
            foreach (var r in puntos.GetComponentsInChildren<MeshRenderer>())
                PonerMaterial(r.gameObject, matPunto);

        // ---------- Luz: dirección bonita para el sombreado toon ----------
        var luz = GameObject.Find("Directional Light");
        if (luz != null)
        {
            Undo.RecordObject(luz.transform, "Luz");
            luz.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        // ---------- Personaje ----------
        ConvertirPersonaje(matPersonaje);

        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log("FarmaciaVR: estilo toon y escenografía listos" + (letreros ? "" : " (sin textos: falta TextMeshPro)") + ". Guarda con Ctrl + S.");
        EditorUtility.DisplayDialog("FarmaciaVR", "¡Listo! Revisa la escena y guarda con Ctrl + S.", "OK");
    }

    // ================= Muebles =================

    static void ConstruirMueble(string nombre, Transform raiz, Material mat, bool dobleCara, int niveles, int semilla)
    {
        var original = GameObject.Find(nombre);
        if (original == null)
        {
            Debug.LogWarning($"FarmaciaVR: no encontré '{nombre}', lo salto.");
            return;
        }

        var rend = original.GetComponent<MeshRenderer>();
        Bounds b = LimitesDeBloque(original.transform);
        if (rend != null)
        {
            Undo.RecordObject(rend, "Ocultar bloque");
            rend.enabled = false; // el bloque queda invisible pero su collider sigue bloqueando la mirada
        }

        var rnd = new System.Random(semilla * 7919);
        var kit = new KitMalla();
        float baseAlto = 0.12f;
        float yBase = b.min.y + baseAlto;

        // Zócalo, techo del mueble y tapas laterales
        kit.Caja(new Vector3(b.center.x, b.min.y + baseAlto / 2f, b.center.z), new Vector3(b.size.x, baseAlto, b.size.z), azul);
        kit.Caja(new Vector3(b.center.x, b.max.y - 0.015f, b.center.z), new Vector3(b.size.x, 0.03f, b.size.z), blanco);
        kit.Caja(new Vector3(b.center.x, b.center.y, b.min.z + 0.02f), new Vector3(b.size.x, b.size.y, 0.04f), blanco);
        kit.Caja(new Vector3(b.center.x, b.center.y, b.max.z - 0.02f), new Vector3(b.size.x, b.size.y, 0.04f), blanco);

        // Panel trasero y lados con productos
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
        kit.Caja(new Vector3(xPanel, yBase + altoPanel / 2f, b.center.z), new Vector3(0.05f, altoPanel, b.size.z - 0.08f), azul);

        float paso = altoPanel / niveles;
        foreach (int lado in lados)
        {
            float xFrente = lado > 0 ? b.max.x : b.min.x;
            float xFondo = xPanel + lado * 0.025f;
            float profundidad = Mathf.Abs(xFrente - xFondo);

            for (int nivel = 0; nivel < niveles; nivel++)
            {
                float yRepisa = yBase + nivel * paso;
                if (nivel > 0)
                    kit.Caja(new Vector3((xFrente + xFondo) / 2f, yRepisa, b.center.z), new Vector3(profundidad, 0.025f, b.size.z - 0.08f), blanco);

                float ySuelo = yRepisa + (nivel > 0 ? 0.0125f : 0f);
                float altoMax = Mathf.Min(0.32f, paso - 0.07f);
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
                    kit.Caja(centro, new Vector3(fondo, alto, ancho), color);

                    // Etiqueta en la cara del frente
                    Color etiqueta = color.r > 0.9f && color.g > 0.9f && color.b > 0.9f ? azul : blanco;
                    Vector3 cara = new Vector3(xCentro + lado * (fondo / 2f + 0.002f), ySuelo + alto * 0.55f, centro.z);
                    kit.Etiqueta(cara, new Vector3(lado, 0f, 0f), alto * 0.18f, ancho * 0.38f, etiqueta);

                    z += ancho + 0.008f;
                }
            }
        }

        CrearObjetoKit("Kit_" + nombre, raiz, kit, mat);
    }

    static void ConstruirMostrador(Transform raiz, Material mat)
    {
        var original = GameObject.Find("Mostrador");
        if (original == null)
            return;
        var rend = original.GetComponent<MeshRenderer>();
        Bounds b = LimitesDeBloque(original.transform);
        if (rend != null)
        {
            Undo.RecordObject(rend, "Ocultar bloque");
            rend.enabled = false;
        }

        var kit = new KitMalla();
        kit.Caja(b.center, b.size - new Vector3(0.04f, 0.04f, 0.06f), azul);
        kit.Caja(new Vector3(b.center.x, b.max.y + 0.025f, b.center.z), new Vector3(b.size.x + 0.08f, 0.05f, b.size.z + 0.08f), blanco);
        // Franja blanca y cruz verde al frente (lado del jugador)
        float zFrente = b.min.z + 0.03f;
        kit.Caja(new Vector3(b.center.x, b.min.y + b.size.y * 0.72f, zFrente - 0.005f), new Vector3(b.size.x - 0.04f, 0.08f, 0.01f), blanco);
        kit.Caja(new Vector3(b.center.x, b.center.y - 0.08f, zFrente - 0.01f), new Vector3(0.30f, 0.10f, 0.02f), verde);
        kit.Caja(new Vector3(b.center.x, b.center.y - 0.08f, zFrente - 0.01f), new Vector3(0.10f, 0.30f, 0.02f), verde);
        // Caja registradora
        float yTope = b.max.y + 0.05f;
        kit.Caja(new Vector3(b.center.x + 0.8f, yTope + 0.06f, b.center.z), new Vector3(0.35f, 0.12f, 0.30f), grisOscuro);
        kit.Caja(new Vector3(b.center.x + 0.8f, yTope + 0.24f, b.center.z + 0.08f), new Vector3(0.30f, 0.20f, 0.03f), grisOscuro);
        kit.Etiqueta(new Vector3(b.center.x + 0.8f, yTope + 0.24f, b.center.z + 0.08f - 0.016f), Vector3.back, 0.08f, 0.13f, azulClaro);

        CrearObjetoKit("Kit_Mostrador", raiz, kit, mat);
    }

    static void ConstruirTecho(Transform raiz, Material matTecho, Material matLampara)
    {
        var techo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        techo.name = "Techo";
        techo.transform.SetParent(raiz, false);
        techo.transform.position = new Vector3(0f, 3.05f, 0f);
        techo.transform.localScale = new Vector3(10f, 0.1f, 10f);
        Object.DestroyImmediate(techo.GetComponent<Collider>());
        techo.GetComponent<Renderer>().sharedMaterial = matTecho;

        var lamparas = new KitMalla();
        foreach (float x in new[] { -3.6f, -1.25f, 1.25f, 3.6f })
            foreach (float z in new[] { -1.2f, 2.0f })
                lamparas.Caja(new Vector3(x, 2.985f, z), new Vector3(0.35f, 0.03f, 1.4f), Color.white, false);
        CrearObjetoKit("Lamparas", raiz, lamparas, matLampara);
    }

    static bool ConstruirDecoracion(Transform raiz, Material mat)
    {
        var kit = new KitMalla();

        // Zócalos azules y franja superior en las paredes (caras interiores)
        foreach (float y in new[] { 0.07f, 2.8f })
        {
            float alto = y < 1f ? 0.14f : 0.12f;
            kit.Caja(new Vector3(0f, y, 4.89f), new Vector3(9.6f, alto, 0.02f), azul);
            kit.Caja(new Vector3(0f, y, -4.89f), new Vector3(9.6f, alto, 0.02f), azul);
            kit.Caja(new Vector3(4.89f, y, 0f), new Vector3(0.02f, alto, 9.6f), azul);
            kit.Caja(new Vector3(-4.89f, y, 0f), new Vector3(0.02f, alto, 9.6f), azul);
        }

        // Tapete de entrada
        kit.Caja(new Vector3(0f, 0.006f, -4.3f), new Vector3(1.6f, 0.012f, 0.9f), azul);

        // Letrero principal sobre el mostrador
        kit.Caja(new Vector3(0f, 2.35f, 4.87f), new Vector3(3.4f, 0.7f, 0.06f), blanco);
        kit.Caja(new Vector3(-1.35f, 2.35f, 4.83f), new Vector3(0.36f, 0.12f, 0.03f), verde);
        kit.Caja(new Vector3(-1.35f, 2.35f, 4.83f), new Vector3(0.12f, 0.36f, 0.03f), verde);

        // Letreros colgantes de pasillo
        float[] xs = { -3.6f, -1.25f, 1.25f, 3.6f };
        string[] textos = { "Cuidado personal", "Medicamentos", "Bebé y mamá", "Vitaminas" };
        foreach (float x in xs)
        {
            kit.Caja(new Vector3(x, 2.55f, -2.4f), new Vector3(1.2f, 0.32f, 0.04f), azul);
            kit.Caja(new Vector3(x - 0.5f, 2.855f, -2.4f), new Vector3(0.01f, 0.29f, 0.01f), grisOscuro, false);
            kit.Caja(new Vector3(x + 0.5f, 2.855f, -2.4f), new Vector3(0.01f, 0.29f, 0.01f), grisOscuro, false);
        }
        CrearObjetoKit("Kit_Decoracion", raiz, kit, mat);

        // Textos (TextMeshPro 3D)
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            Debug.LogWarning("FarmaciaVR: falta TextMeshPro (Window > TextMeshPro > Import TMP Essential Resources). Creé los letreros sin texto.");
            return false;
        }
        var contenedor = new GameObject("Letreros");
        contenedor.transform.SetParent(raiz, false);
        Texto(contenedor.transform, "FARMACIA", new Vector3(0.25f, 2.35f, 4.835f), new Vector2(2.6f, 0.5f), azul);
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

    // ================= Personaje =================

    static void ConvertirPersonaje(Material mat)
    {
        var manager = Object.FindFirstObjectByType<JuegoManager>();
        var actual = Object.FindFirstObjectByType<PersonajeEncontrable>();

        // Si ya es una copia del cubo agarrable, solo actualiza el material.
        if (actual != null && actual.GetComponentInChildren<Rigidbody>() != null)
        {
            PonerMaterialATodo(actual.gameObject, mat);
            return;
        }

        var cubo = GameObject.Find("[BuildingBlock] Cube");
        if (cubo == null)
        {
            Debug.LogWarning("FarmaciaVR: no encontré '[BuildingBlock] Cube' (el cubo agarrable). Dejo el personaje como está, solo con color toon.");
            if (actual != null)
                PonerMaterialATodo(actual.gameObject, mat);
            return;
        }

        var nuevo = Object.Instantiate(cubo);
        Undo.RegisterCreatedObjectUndo(nuevo, "Convertir personaje");
        nuevo.name = "Personaje";
        foreach (var comp in nuevo.GetComponentsInChildren<MonoBehaviour>(true))
            if (comp != null && comp.GetType().Name == "BuildingBlock")
                Object.DestroyImmediate(comp); // evita que Meta lo confunda con el bloque original

        nuevo.transform.position = actual != null ? actual.transform.position : cubo.transform.position + Vector3.right * 0.5f;
        PonerMaterialATodo(nuevo, mat);
        var encontrable = Undo.AddComponent<PersonajeEncontrable>(nuevo);

        if (manager != null)
        {
            Undo.RecordObject(manager, "Conectar personaje");
            manager.personaje = encontrable;
            EditorUtility.SetDirty(manager);
        }

        if (actual != null)
            Undo.DestroyObjectImmediate(actual.gameObject);
    }

    // ================= Utilidades =================

    static Material CrearMaterial(string nombre, bool conContorno, Color color, Color sombra, int modoContorno, bool coloresMalla)
    {
        string ruta = $"{carpetaMateriales}/{nombre}.mat";
        var shader = Shader.Find(conContorno ? "FarmaciaVR/Toon" : "FarmaciaVR/Toon Sin Borde");
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
        m.SetColor("_BaseColor", color);
        m.SetColor("_ShadowColor", sombra);
        m.SetFloat("_UseVertexColor", coloresMalla ? 1f : 0f);
        m.SetColor("_OutlineColor", colorContorno);
        m.SetFloat("_OutlineWidth", conContorno ? 2f : 0f);
        m.SetFloat("_OutlineMode", modoContorno);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
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
    }

    static void PonerMaterialATodo(GameObject go, Material mat)
    {
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
        {
            Undo.RecordObject(r, "Material toon");
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    static void CrearObjetoKit(string nombre, Transform padre, KitMalla kit, Material mat)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = kit.GuardarComo($"{carpetaMallas}/{nombre}.asset");
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
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

    // Arma una malla con muchas cajas de colores (colores en los vértices)
    // y guarda en UV3 la dirección del contorno de cada caja.
    class KitMalla
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normales = new List<Vector3>();
        readonly List<Color> colores = new List<Color>();
        readonly List<Vector3> direcciones = new List<Vector3>();
        readonly List<int> triangulos = new List<int>();

        public void Caja(Vector3 centro, Vector3 tamano, Color color, bool contorno = true)
        {
            Vector3 m = tamano * 0.5f;
            Cara(centro, m, Vector3.right, Vector3.up, Vector3.forward, color, contorno);
            Cara(centro, m, Vector3.left, Vector3.forward, Vector3.up, color, contorno);
            Cara(centro, m, Vector3.up, Vector3.forward, Vector3.right, color, contorno);
            Cara(centro, m, Vector3.down, Vector3.right, Vector3.forward, color, contorno);
            Cara(centro, m, Vector3.forward, Vector3.right, Vector3.up, color, contorno);
            Cara(centro, m, Vector3.back, Vector3.up, Vector3.right, color, contorno);
        }

        // u × v = normal, así el frente queda hacia afuera.
        void Cara(Vector3 centro, Vector3 m, Vector3 normal, Vector3 u, Vector3 v, Color color, bool contorno)
        {
            float en = Mathf.Abs(Vector3.Dot(m, normal));
            float eu = Mathf.Abs(Vector3.Dot(m, u));
            float ev = Mathf.Abs(Vector3.Dot(m, v));
            Vector3 c = centro + normal * en;
            Agregar(c - u * eu - v * ev, c - u * eu + v * ev, c + u * eu + v * ev, c + u * eu - v * ev,
                normal, color, centro, contorno);
        }

        // Rectángulo plano (sin contorno) pegado a una cara, para etiquetas y pantallas.
        public void Etiqueta(Vector3 centro, Vector3 normal, float mitadAlto, float mitadAncho, Color color)
        {
            Vector3 u, v;
            float hu, hv;
            if (Mathf.Abs(normal.x) > 0.5f)
            {
                if (normal.x > 0) { u = Vector3.up; v = Vector3.forward; hu = mitadAlto; hv = mitadAncho; }
                else { u = Vector3.forward; v = Vector3.up; hu = mitadAncho; hv = mitadAlto; }
            }
            else
            {
                if (normal.z > 0) { u = Vector3.right; v = Vector3.up; hu = mitadAncho; hv = mitadAlto; }
                else { u = Vector3.up; v = Vector3.right; hu = mitadAlto; hv = mitadAncho; }
            }
            Agregar(centro - u * hu - v * hv, centro - u * hu + v * hv, centro + u * hu + v * hv, centro + u * hu - v * hv,
                normal.normalized, color, centro, false);
        }

        void Agregar(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, Color color, Vector3 centroCaja, bool contorno)
        {
            int i = vertices.Count;
            foreach (var p in new[] { p0, p1, p2, p3 })
            {
                vertices.Add(p);
                normales.Add(normal);
                colores.Add(color);
                direcciones.Add(contorno ? p - centroCaja : Vector3.zero);
            }
            // Orden horario visto desde afuera (frente en Unity).
            triangulos.Add(i); triangulos.Add(i + 2); triangulos.Add(i + 1);
            triangulos.Add(i); triangulos.Add(i + 3); triangulos.Add(i + 2);
        }

        public Mesh GuardarComo(string ruta)
        {
            var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            bool nueva = malla == null;
            if (nueva)
                malla = new Mesh();
            malla.Clear();
            malla.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            malla.SetVertices(vertices);
            malla.SetNormals(normales);
            malla.SetColors(colores);
            malla.SetUVs(3, direcciones);
            malla.SetTriangles(triangulos, 0);
            malla.RecalculateBounds();
            if (nueva)
                AssetDatabase.CreateAsset(malla, ruta);
            else
                EditorUtility.SetDirty(malla);
            return malla;
        }
    }
}
#endif

using System.Collections.Generic;
using UnityEngine;

// FONDOS VIVOS (v42, idea de Juan Carlos): fondos que NO son fotos: los dibuja la app (son 100 % nuestros y gratis).
//  - UNIVERSO: miles de estrellas alrededor de ti (las cercanas, de verdad en 3D: cada ojo las ve distinto), la Vía
//    Láctea y nebulosas de colores. Sin piso: flotas en medio del espacio. "Avanzar": las estrellas pasan despacio a
//    tu lado (siempre a la misma velocidad suave, para no marear).
//  - SOBRE LAS NUBES: un mar de nubes debajo de ti (dos capas: la de arriba pasa más rápido), el cielo en degradado y
//    un sol cálido a lo lejos. "Volar": las nubes pasan despacio debajo, como desde la ventana de un avión.
// Salen en la ventanita de fondos 360 (después de los de la app). Las flechas de girar también los giran.
// Shaders: TrazoVR/CieloVivo, TrazoVR/Estrellas y TrazoVR/Nubes (sus materiales los pone ★ Armar escena).
public class FondosVivos : MonoBehaviour
{
    public const int Cantidad = 2;
    public const int Universo = 0, Nubes = 1;
    static readonly string[] Nombres = { "Universo", "Sobre las nubes" };
    const float RadioCielo = 110f, RadioLejanas = 100f;
    const float Caja = 80f;                                  // las estrellas cercanas llenan una caja así (metros)
    const int CuantasLejanas = 5000, CuantasCercanas = 1600;
    const float BajoNubes = 28f, BajoNubesArriba = 18f, RadioNubes = 140f;
    const float EscalaNubes = 90f, EscalaNubesArriba = 65f;
    const float VelocidadUniverso = 1.2f, VelocidadNubes = 4f;   // metros por segundo (despacio: no marea)
    static readonly Vector3 SolDir = new Vector3(0.35f, 0.18f, 0.92f);
    static readonly Vector3 NormalBanda = new Vector3(0.25f, 0.92f, 0.3f);  // la misma de TrazoCieloVivo
    static readonly int idCentro = Shader.PropertyToID("_JCCentroVivo");
    static readonly int idAvance = Shader.PropertyToID("_Avance");

    static Texture2D ruido;
    static readonly Texture2D[] miniaturas = new Texture2D[Cantidad];

    Material baseCielo, baseEstrellas, baseNubes;
    Transform raizUniverso, raizNubes, cercanas;
    Material matCercanas, matNubesAbajo, matNubesArriba;
    readonly List<Material> materiales = new List<Material>();
    readonly List<Mesh> mallas = new List<Mesh>();
    int puesto = -1;
    float avanceEstrellas, avanceAbajo, avanceArriba;

    public bool Avanzando { get; private set; }
    public int Puesto => puesto;
    public bool Listo => baseCielo != null && baseEstrellas != null && baseNubes != null;

    public static string Nombre(int cual) => Nombres[Mathf.Clamp(cual, 0, Cantidad - 1)];
    public static string NombreAvanzar(int cual) => cual == Nubes ? "Volar" : "Avanzar";
    // El color de detrás (la cámara y el vidrio de los menús).
    public static Color ColorFondo(int cual) => cual == Nubes ? new Color(0.7f, 0.8f, 0.93f) : new Color(0.01f, 0.012f, 0.03f);

    public void Preparar(Material cielo, Material estrellas, Material nubes)
    {
        baseCielo = cielo;
        baseEstrellas = estrellas;
        baseNubes = nubes;
    }

    // cual = Universo, Nubes o -1 (ninguno).
    public void Poner(int cual, Transform cabeza)
    {
        if (cual >= 0 && !Listo)
            cual = -1;
        if (cual >= 0)
            Armar(cual);
        if (cual != puesto)
            Avanzando = false;
        puesto = cual;
        if (raizUniverso != null)
            raizUniverso.gameObject.SetActive(cual == Universo);
        if (cercanas != null)
            cercanas.gameObject.SetActive(cual == Universo);
        if (raizNubes != null)
            raizNubes.gameObject.SetActive(cual == Nubes);
        if (cual >= 0)
            Centrar(cabeza);
    }

    public void AlternarAvanzar()
    {
        if (puesto >= 0)
            Avanzando = !Avanzando;
    }

    // Mirando hacia donde miras al elegirlo (de lado).
    void Centrar(Transform cabeza)
    {
        Vector3 pos = cabeza != null ? cabeza.position : Vector3.up * 1.6f;
        Vector3 adelante = cabeza != null ? cabeza.forward : Vector3.forward;
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        Quaternion giro = Quaternion.LookRotation(adelante.normalized, Vector3.up);
        foreach (var t in new[] { raizUniverso, raizNubes, cercanas })
            if (t != null)
                t.SetPositionAndRotation(pos, giro);
        Shader.SetGlobalVector(idCentro, pos);
    }

    // Cada cuadro (y justo antes de dibujar): el cielo y las nubes van contigo; las estrellas cercanas se quedan
    // (así, al mover la cabeza, se ven en 3D) y siempre te rodean.
    public void Seguir(Transform cabeza)
    {
        if (puesto < 0 || cabeza == null)
            return;
        Vector3 pos = cabeza.position;
        if (puesto == Universo && raizUniverso != null)
            raizUniverso.position = pos;
        if (puesto == Nubes && raizNubes != null)
            raizNubes.position = pos;
        Shader.SetGlobalVector(idCentro, pos);
    }

    // Como si TÚ giraras (las flechas, el reloj o la voz): todo gira alrededor de tu cabeza.
    public bool Girar(float grados, Vector3 pivote)
    {
        if (puesto < 0)
            return false;
        foreach (var t in new[] { raizUniverso, raizNubes, cercanas })
            if (t != null)
                t.RotateAround(pivote, Vector3.up, -grados);
        return true;
    }

    void Update()
    {
        if (puesto < 0 || !Avanzando)
            return;
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        if (puesto == Universo && matCercanas != null)
        {
            // (La caja se repite cada "Caja" metros: así el número nunca crece.)
            avanceEstrellas = Mathf.Repeat(avanceEstrellas + VelocidadUniverso * dt, Caja);
            matCercanas.SetFloat(idAvance, avanceEstrellas);
        }
        else if (puesto == Nubes && matNubesAbajo != null && matNubesArriba != null)
        {
            // La textura de las nubes se repite: se vuelve a empezar justo donde todo coincide.
            avanceAbajo = Mathf.Repeat(avanceAbajo + VelocidadNubes * dt, EscalaNubes * 10f);
            avanceArriba = Mathf.Repeat(avanceArriba + VelocidadNubes * 1.35f * dt, EscalaNubesArriba * 10f);
            matNubesAbajo.SetFloat(idAvance, avanceAbajo);
            matNubesArriba.SetFloat(idAvance, avanceArriba);
        }
    }

    // ---------- Armar ----------

    void Armar(int cual)
    {
        if (cual == Universo && raizUniverso == null)
        {
            raizUniverso = new GameObject("Universo").transform;
            raizUniverso.SetParent(transform, false);
            var cielo = NuevoMaterial(baseCielo);
            cielo.SetFloat("_Modo", 0f);
            cielo.SetTexture("_Ruido", Ruido());
            Pieza(raizUniverso, "Cielo", Esfera(RadioCielo), cielo, Vector3.zero);
            var lejanas = NuevoMaterial(baseEstrellas);
            lejanas.SetFloat("_Envolver", 0f);
            Pieza(raizUniverso, "EstrellasLejanas", Lejanas(), lejanas, Vector3.zero);
            cercanas = new GameObject("EstrellasCercanas").transform;
            cercanas.SetParent(transform, false);
            matCercanas = NuevoMaterial(baseEstrellas);
            matCercanas.SetFloat("_Envolver", 1f);
            matCercanas.SetFloat("_Caja", Caja);
            matCercanas.SetFloat(idAvance, 0f);
            Pieza(cercanas, "Estrellas", Cercanas(), matCercanas, Vector3.zero);
        }
        if (cual == Nubes && raizNubes == null)
        {
            raizNubes = new GameObject("SobreLasNubes").transform;
            raizNubes.SetParent(transform, false);
            var cielo = NuevoMaterial(baseCielo);
            cielo.SetFloat("_Modo", 1f);
            cielo.SetVector("_SolDir", SolDir.normalized);
            cielo.SetTexture("_Ruido", Ruido());
            Pieza(raizNubes, "Cielo", Esfera(RadioCielo), cielo, Vector3.zero);
            matNubesAbajo = Nube(0f, EscalaNubes, 2455);
            matNubesArriba = Nube(1f, EscalaNubesArriba, 2456);
            var disco = Disco(RadioNubes);
            Pieza(raizNubes, "NubesAbajo", disco, matNubesAbajo, new Vector3(0f, -BajoNubes, 0f));
            Pieza(raizNubes, "NubesArriba", disco, matNubesArriba, new Vector3(0f, -BajoNubesArriba, 0f));
        }
    }

    Material Nube(float capa, float escala, int cola)
    {
        var m = NuevoMaterial(baseNubes);
        m.SetFloat("_Capa", capa);
        m.SetFloat("_Escala", escala);
        m.SetFloat("_Radio", RadioNubes);
        m.SetFloat(idAvance, 0f);
        m.SetVector("_SolDir", SolDir.normalized);
        m.SetTexture("_Ruido", Ruido());
        m.renderQueue = cola;   // la de abajo primero, la de arriba encima
        return m;
    }

    Material NuevoMaterial(Material baseMat)
    {
        var m = new Material(baseMat);
        materiales.Add(m);
        return m;
    }

    void Pieza(Transform padre, string nombre, Mesh malla, Material m, Vector3 pos)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // Una esfera para verla desde adentro (el shader calcula el color con la dirección).
    Mesh Esfera(float radio)
    {
        const int lados = 48, pisos = 24;
        var v = new List<Vector3>();
        var t = new List<int>();
        for (int f = 0; f <= pisos; f++)
        {
            float lat = (f / (float)pisos - 0.5f) * Mathf.PI;
            for (int c = 0; c <= lados; c++)
            {
                float lon = c / (float)lados * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon)) * radio);
            }
        }
        int ancho = lados + 1;
        for (int f = 0; f < pisos; f++)
            for (int c = 0; c < lados; c++)
            {
                int a = f * ancho + c, b = a + 1, arriba = a + ancho, arribaB = arriba + 1;
                t.Add(a); t.Add(arriba); t.Add(b);
                t.Add(b); t.Add(arriba); t.Add(arribaB);
            }
        var m = new Mesh { name = "CieloVivo" };
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateBounds();
        mallas.Add(m);
        return m;
    }

    // Un disco plano (las nubes), con anillos para que la distancia se calcule bien.
    Mesh Disco(float radio)
    {
        const int lados = 64, anillos = 16;
        var v = new List<Vector3> { Vector3.zero };
        var t = new List<int>();
        for (int a = 1; a <= anillos; a++)
        {
            float r = radio * a / anillos;
            for (int c = 0; c < lados; c++)
            {
                float ang = c / (float)lados * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r));
            }
        }
        for (int c = 0; c < lados; c++)
        {
            t.Add(0);
            t.Add(1 + (c + 1) % lados);
            t.Add(1 + c);
        }
        for (int a = 1; a < anillos; a++)
        {
            int adentro = 1 + (a - 1) * lados, afuera = 1 + a * lados;
            for (int c = 0; c < lados; c++)
            {
                int c1 = (c + 1) % lados;
                t.Add(adentro + c); t.Add(adentro + c1); t.Add(afuera + c);
                t.Add(adentro + c1); t.Add(afuera + c1); t.Add(afuera + c);
            }
        }
        var m = new Mesh { name = "Nubes" };
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateBounds();
        mallas.Add(m);
        return m;
    }

    // Las estrellas lejanas: en la esfera del cielo, más juntas en la banda de la Vía Láctea.
    Mesh Lejanas()
    {
        var r = new System.Random(42);
        var centros = new List<Vector3>();
        var tamanos = new List<float>();
        var colores = new List<Color>();
        Vector3 n = NormalBanda.normalized;
        for (int i = 0; i < CuantasLejanas; i++)
        {
            Vector3 d = AlAzarEnEsfera(r);
            if (r.NextDouble() < 0.45)
                d = (d - n * Vector3.Dot(d, n) * 0.88f).normalized;
            float u = (float)r.NextDouble();
            centros.Add(d * RadioLejanas);
            tamanos.Add(Mathf.Lerp(0.16f, 0.45f, u * u * u));
            colores.Add(ColorEstrella(r, 0.18f + 0.82f * Mathf.Pow(u, 3f)));
        }
        return Estrellas(centros, tamanos, colores, r, "EstrellasLejanas");
    }

    // Las cercanas: dentro de una caja alrededor de ti (el shader las mantiene siempre a tu alrededor).
    Mesh Cercanas()
    {
        var r = new System.Random(7);
        var centros = new List<Vector3>();
        var tamanos = new List<float>();
        var colores = new List<Color>();
        while (centros.Count < CuantasCercanas)
        {
            var p = new Vector3((float)r.NextDouble() - 0.5f, (float)r.NextDouble() - 0.5f, (float)r.NextDouble() - 0.5f) * Caja;
            if (p.magnitude < 2f)
                continue;
            float u = (float)r.NextDouble();
            centros.Add(p);
            tamanos.Add(0.025f + 0.06f * u * u * u);
            colores.Add(ColorEstrella(r, 0.35f + 0.65f * u * u));
        }
        return Estrellas(centros, tamanos, colores, r, "EstrellasCercanas");
    }

    Mesh Estrellas(List<Vector3> centros, List<float> tamanos, List<Color> colores, System.Random r, string nombre)
    {
        int n = centros.Count;
        var v = new Vector3[n * 4];
        var uv = new Vector2[n * 4];
        var uv2 = new Vector2[n * 4];
        var col = new Color[n * 4];
        var tri = new int[n * 6];
        var esquinas = new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f) };
        for (int i = 0; i < n; i++)
        {
            float fase = (float)r.NextDouble();
            for (int k = 0; k < 4; k++)
            {
                v[i * 4 + k] = centros[i];
                uv[i * 4 + k] = esquinas[k];
                uv2[i * 4 + k] = new Vector2(tamanos[i], fase);
                col[i * 4 + k] = colores[i];
            }
            int b = i * 4;
            tri[i * 6] = b; tri[i * 6 + 1] = b + 2; tri[i * 6 + 2] = b + 1;
            tri[i * 6 + 3] = b; tri[i * 6 + 4] = b + 3; tri[i * 6 + 5] = b + 2;
        }
        var m = new Mesh { name = nombre };
        m.vertices = v;
        m.uv = uv;
        m.uv2 = uv2;
        m.colors = col;
        m.triangles = tri;
        // El shader las mueve (te miran y te rodean): que Unity nunca las deje de dibujar por estar "fuera".
        m.bounds = new Bounds(Vector3.zero, Vector3.one * 400f);
        mallas.Add(m);
        return m;
    }

    static Vector3 AlAzarEnEsfera(System.Random r)
    {
        float z = (float)r.NextDouble() * 2f - 1f;
        float ang = (float)r.NextDouble() * Mathf.PI * 2f;
        float s = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
        return new Vector3(s * Mathf.Cos(ang), z, s * Mathf.Sin(ang));
    }

    // Colores de estrellas de verdad: azuladas, blancas, amarillas, naranjas y alguna rojiza. a = brillo.
    static Color ColorEstrella(System.Random r, float brillo)
    {
        double t = r.NextDouble();
        Color c;
        if (t < 0.12) c = new Color(0.7f, 0.8f, 1f);
        else if (t < 0.55) c = new Color(0.93f, 0.95f, 1f);
        else if (t < 0.8) c = new Color(1f, 0.95f, 0.85f);
        else if (t < 0.95) c = new Color(1f, 0.85f, 0.65f);
        else c = new Color(1f, 0.7f, 0.6f);
        c.a = Mathf.Clamp01(brillo);
        return c;
    }

    // ---------- El ruido (una textura que se repite; cada canal, un tamaño distinto) ----------

    static Texture2D Ruido()
    {
        if (ruido != null)
            return ruido;
        const int n = 256;
        int[] inicio = { 4, 8, 16, 32 };
        int[] octavas = { 4, 4, 3, 2 };
        var canales = new float[4][];
        for (int c = 0; c < 4; c++)
            canales[c] = Fbm(n, inicio[c], octavas[c], 17 + c * 31);
        var pix = new Color32[n * n];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = new Color32(A8(canales[0][i]), A8(canales[1][i]), A8(canales[2][i]), A8(canales[3][i]));
        ruido = new Texture2D(n, n, TextureFormat.RGBA32, false, true) { name = "RuidoVivo" };
        ruido.wrapMode = TextureWrapMode.Repeat;
        ruido.filterMode = FilterMode.Bilinear;
        ruido.SetPixels32(pix);
        ruido.Apply(false, true);
        return ruido;
    }

    static byte A8(float x)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(x * 255f), 0, 255);
    }

    // Ruido "fbm" que se repite: varias capas de cuadritos al azar, suavizados; cada capa, el doble de fina y la
    // mitad de fuerte. Al final se estira para que vaya de 0 a 1.
    static float[] Fbm(int n, int celdas, int octavas, int semilla)
    {
        var r = new System.Random(semilla);
        var res = new float[n * n];
        float amp = 1f;
        for (int o = 0; o < octavas; o++)
        {
            int f = celdas << o;
            var red = new float[f * f];
            for (int i = 0; i < red.Length; i++)
                red[i] = (float)r.NextDouble();
            for (int y = 0; y < n; y++)
            {
                float fy = y * f / (float)n;
                int y0 = (int)fy;
                float ty = fy - y0;
                ty = ty * ty * (3f - 2f * ty);
                int y1 = (y0 + 1) % f;
                for (int x = 0; x < n; x++)
                {
                    float fx = x * f / (float)n;
                    int x0 = (int)fx;
                    float tx = fx - x0;
                    tx = tx * tx * (3f - 2f * tx);
                    int x1 = (x0 + 1) % f;
                    float a = Mathf.Lerp(red[y0 * f + x0], red[y0 * f + x1], tx);
                    float b = Mathf.Lerp(red[y1 * f + x0], red[y1 * f + x1], tx);
                    res[y * n + x] += Mathf.Lerp(a, b, ty) * amp;
                }
            }
            amp *= 0.5f;
        }
        float min = float.MaxValue, max = float.MinValue;
        foreach (var x in res)
        {
            if (x < min) min = x;
            if (x > max) max = x;
        }
        float rango = Mathf.Max(1e-5f, max - min);
        for (int i = 0; i < res.Length; i++)
            res[i] = (res[i] - min) / rango;
        return res;
    }

    // ---------- Miniaturas (para la ventanita de fondos) ----------

    public static Texture2D Miniatura(int cual)
    {
        cual = Mathf.Clamp(cual, 0, Cantidad - 1);
        if (miniaturas[cual] != null)
            return miniaturas[cual];
        const int w = 256, h = 128;
        var pix = new Color32[w * h];
        var r = new System.Random(3 + cual);
        Vector3 sol = SolDir.normalized;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float lon = (x / (float)w - 0.5f) * Mathf.PI * 2f;
                float lat = (y / (float)h - 0.5f) * Mathf.PI;
                var d = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                Color c;
                if (cual == Universo)
                {
                    c = new Color(0.01f, 0.012f, 0.035f);
                    if (r.NextDouble() < 0.012)
                        c += Color.white * (0.4f + 0.6f * (float)r.NextDouble());
                }
                else
                {
                    float s = Mathf.Clamp01(Vector3.Dot(d, sol));
                    if (d.y >= 0f)
                    {
                        float k = Mathf.Pow(1f - d.y, 4f);
                        c = Color.Lerp(new Color(0.16f, 0.36f, 0.74f), new Color(0.76f, 0.85f, 0.95f), k);
                    }
                    else
                    {
                        float nube = Mathf.PerlinNoise(x * 0.06f, y * 0.12f);
                        c = Color.Lerp(new Color(0.55f, 0.62f, 0.78f), new Color(1f, 0.97f, 0.93f), Mathf.SmoothStep(0.3f, 0.65f, nube));
                        c = Color.Lerp(c, new Color(0.84f, 0.88f, 0.94f), Mathf.Clamp01(1f + d.y * 4f));
                    }
                    c += new Color(1f, 0.9f, 0.7f) * (Mathf.Pow(s, 60f) * 0.8f + Mathf.Pow(s, 10f) * 0.15f);
                }
                c.a = 1f;
                pix[y * w + x] = c;
            }
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Miniatura" + Nombre(cual) };
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(pix);
        tex.Apply(false, true);
        miniaturas[cual] = tex;
        return tex;
    }

    void OnDestroy()
    {
        foreach (var m in materiales)
            if (m != null)
                Destroy(m);
        foreach (var m in mallas)
            if (m != null)
                Destroy(m);
    }
}

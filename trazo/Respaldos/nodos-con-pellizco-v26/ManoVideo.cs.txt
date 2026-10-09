using System.Collections.Generic;
using UnityEngine;

// Una mano "normal" para el video del proceso (en vez de las líneas de los huesos):
// dedos redondeados que se afinan hacia la punta y una palma con grosor.
// Lleva un GUANTE NEGRO SIN DEDOS (los dedos quedan descubiertos desde su base) y una MANGA de ropa
// larga y cerrada (así el brazo no se ve hueco).
// Se arma con las 21 articulaciones que guardó GrabadorProceso (en coordenadas del mundo).
public sealed class ManoVideo
{
    // Grosor (radio en metros) de cada articulación: muñeca, pulgar (4), índice (4), medio (4), anular (4), meñique (4).
    static readonly float[] Radio =
    {
        0.021f,
        0.0135f, 0.012f, 0.0105f, 0.0092f,
        0.0108f, 0.0097f, 0.0087f, 0.0077f,
        0.0111f, 0.0100f, 0.0089f, 0.0079f,
        0.0105f, 0.0095f, 0.0085f, 0.0075f,
        0.0094f, 0.0084f, 0.0076f, 0.0068f,
    };
    static readonly int[] Segmentos =
    {
        0, 1, 1, 2, 2, 3, 3, 4,
        5, 6, 6, 7, 7, 8,
        9, 10, 10, 11, 11, 12,
        13, 14, 14, 15, 15, 16,
        17, 18, 18, 19, 19, 20,
        0, 5, 0, 17,
    };
    static readonly int[] Palma = { 0, 1, 5, 9, 13, 17 };
    const int Lados = 10;
    const int Paralelos = 6;

    // Partes: 0 = piel (dedos), 1 = guante (palma, muñeca, base de los dedos), 2 = manga.
    const int Piel = 0, Guante = 1, Manga = 2;
    static readonly Color ColorGuante = new Color(0.07f, 0.07f, 0.08f);
    static readonly Color ColorManga = new Color(0.22f, 0.25f, 0.32f);
    const float LargoManga = 0.26f;

    readonly GameObject go;
    readonly Mesh malla;
    readonly Material materialGuante, materialManga;
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Vector3> normales = new List<Vector3>();
    readonly List<int>[] partes = { new List<int>(), new List<int>(), new List<int>() };
    List<int> triangulos;
    static Vector3[] esfera;
    static int[] esferaTri;

    readonly bool guia;
    MeshRenderer render;
    Material materialBase;
    int resaltado = -1;

    // guia = true: mano "fantasma" del tutorial (toda del mismo material, sin guante ni manga).
    public ManoVideo(Transform padre, Material material, int capa, bool guia = false)
    {
        this.guia = guia;
        go = new GameObject(guia ? "ManoGuia" : "ManoVideo");
        go.layer = capa;
        go.transform.SetParent(padre, false);
        malla = new Mesh { name = "ManoVideo" };
        malla.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var mr = go.AddComponent<MeshRenderer>();
        render = mr;
        materialBase = material;
        if (guia)
        {
            mr.sharedMaterials = new[] { material, material, material };
        }
        else
        {
            materialGuante = Copia(material, ColorGuante);
            materialManga = Copia(material, ColorManga);
            mr.sharedMaterials = new[] { material, materialGuante, materialManga };
        }
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        ArmarEsfera();
    }

    public void Poner(Vector3[] a)
    {
        bool ver = a != null && a.Length >= Radio.Length;
        if (go.activeSelf != ver)
            go.SetActive(ver);
        if (!ver)
            return;
        vertices.Clear();
        normales.Clear();
        foreach (var l in partes)
            l.Clear();
        for (int i = 0; i < Radio.Length; i++)
        {
            triangulos = partes[EnResaltado(i) ? Manga : DeGuante(i) ? Guante : Piel];
            Esfera(a[i], Radio[i]);
        }
        for (int k = 0; k + 1 < Segmentos.Length; k += 2)
        {
            int i0 = Segmentos[k], i1 = Segmentos[k + 1];
            // El tubo va con guante si sus dos puntas están dentro del guante (palma y base del pulgar).
            triangulos = partes[EnResaltado(i0) && EnResaltado(i1) ? Manga : DeGuante(i0) && DeGuante(i1) ? Guante : Piel];
            Tubo(a[i0], Radio[i0], a[i1], Radio[i1]);
        }
        triangulos = partes[Guante];
        PalmaGruesa(a);
        // Relleno del dorso y la palma (redondeado, ni flaco ni gordo).
        Esfera(Vector3.Lerp(a[0], a[9], 0.5f), 0.019f);
        Esfera(Vector3.Lerp(a[0], a[5], 0.55f), 0.0165f);
        Esfera(Vector3.Lerp(a[0], a[13], 0.55f), 0.0165f);
        Esfera(Vector3.Lerp(a[0], a[17], 0.5f), 0.0145f);
        // Manga: desde la muñeca hacia atrás, larga y cerrada al final.
        Vector3 atras = a[0] - a[9];
        if (!guia && atras.sqrMagnitude > 1e-8f)
        {
            atras.Normalize();
            triangulos = partes[Manga];
            Vector3 inicio = a[0] + atras * 0.012f;
            Vector3 fin = a[0] + atras * LargoManga;
            // Manga ajustada al brazo (un poquito más ancha que la muñeca).
            Tubo(inicio, Radio[0] * 1.12f, fin, Radio[0] * 1.35f);
            Tapa(inicio, -atras, Radio[0] * 1.12f);
            Tapa(fin, atras, Radio[0] * 1.35f);
        }
        malla.Clear();
        malla.subMeshCount = 3;
        malla.SetVertices(vertices);
        malla.SetNormals(normales);
        for (int k = 0; k < 3; k++)
            malla.SetTriangles(partes[k], k);
        malla.RecalculateBounds();
    }

    // Mano guía: un dedo de otro color (0 pulgar, 1 índice, 2 medio, 3 anular, 4 meñique; -1 = ninguno).
    // Ese dedo usa la tercera parte de la malla (en la mano guía no hay manga).
    public void Resaltar(int dedo, Material m)
    {
        if (!guia || render == null)
            return;
        int nuevo = m != null ? dedo : -1;
        if (nuevo == resaltado)
            return;
        resaltado = nuevo;
        render.sharedMaterials = new[] { materialBase, materialBase, nuevo >= 0 ? m : materialBase };
    }

    bool EnResaltado(int i)
    {
        return guia && resaltado >= 0 && i >= 1 + resaltado * 4 && i <= 4 + resaltado * 4;
    }

    // Con guante: la muñeca, la base del pulgar y los nudillos (la base de cada dedo). Lo demás es piel.
    static bool DeGuante(int i)
    {
        return i == 0 || i == 1 || i == 2 || i == 5 || i == 9 || i == 13 || i == 17;
    }

    static Material Copia(Material m, Color c)
    {
        var copia = new Material(m);
        if (copia.HasProperty("_BaseColor")) copia.SetColor("_BaseColor", c);
        if (copia.HasProperty("_Color")) copia.SetColor("_Color", c);
        return copia;
    }

    // Un círculo que cierra el tubo (así la manga no se ve hueca).
    void Tapa(Vector3 centro, Vector3 normal, float radio)
    {
        Vector3 u = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 w = Vector3.Cross(normal, u);
        int c = vertices.Count;
        vertices.Add(centro);
        normales.Add(normal);
        for (int k = 0; k < Lados; k++)
        {
            float ang = 2f * Mathf.PI * k / Lados;
            vertices.Add(centro + (u * Mathf.Cos(ang) + w * Mathf.Sin(ang)) * radio);
            normales.Add(normal);
        }
        for (int k = 0; k < Lados; k++)
        {
            triangulos.Add(c);
            triangulos.Add(c + 1 + k);
            triangulos.Add(c + 1 + (k + 1) % Lados);
        }
    }

    // ---------- Guante de caricatura (como el de Mickey) ----------
    // Dedos gorditos y redondos, dorso ovalado y relleno con 3 rayitas, puño en la muñeca y 4 dedos
    // (el meñique se esconde, como en los cartoons). Parte 0 = guante, 1 = rayitas, 2 = puño.

    static Vector3[] esferaFina;
    static int[] esferaFinaTri;

    public void UsarGuante(Material rayas)
    {
        if (render == null)
            return;
        render.sharedMaterials = new[] { materialBase, rayas != null ? rayas : materialBase, materialBase };
    }

    const float EscalaGuante = 0.93f;
    readonly Vector3[] guanteA = new Vector3[21];

    public void PonerGuante(Vector3[] entrada, bool izquierda)
    {
        bool ver = entrada != null && entrada.Length >= 21;
        if (go.activeSelf != ver)
            go.SetActive(ver);
        if (!ver)
            return;
        ArmarEsferaFina();
        // Un poquito más pequeño que tu mano (7%), alrededor del centro del dorso.
        Vector3 c0 = Vector3.Lerp(entrada[0], entrada[9], 0.5f);
        for (int i = 0; i < 21; i++)
            guanteA[i] = c0 + (entrada[i] - c0) * EscalaGuante;
        var a = guanteA;
        vertices.Clear();
        normales.Clear();
        foreach (var l in partes)
            l.Clear();

        // Dedos gorditos y curvos (pulgar, índice, medio y anular): una "salchichita" suave que pasa por
        // las articulaciones (curva Catmull-Rom), sin tramos rectos.
        triangulos = partes[0];
        for (int d = 0; d < 4; d++)
        {
            int b = 1 + d * 4;
            float r = (d == 0 ? 0.0175f : 0.0158f) * EscalaGuante;
            Vector3 previo = a[b];
            const int muestras = 10;
            for (int k = 0; k <= muestras; k++)
            {
                float u = k / (float)muestras * 3f;      // 0..3 (tres tramos entre 4 articulaciones)
                int tramo = Mathf.Min(2, Mathf.FloorToInt(u));
                float t = u - tramo;
                Vector3 p0 = a[b + Mathf.Max(0, tramo - 1)];
                Vector3 p1 = a[b + tramo];
                Vector3 p2 = a[b + tramo + 1];
                Vector3 p3 = a[b + Mathf.Min(3, tramo + 2)];
                Vector3 p = CatmullRom(p0, p1, p2, p3, t);
                EsferaFina(p, r);
                if (k > 0)
                    TuboSuave(previo, r, p, r);
                previo = p;
            }
        }

        // Dorso: un óvalo relleno (sin formas raras).
        Vector3 F = a[9] - a[0];
        float largo = F.magnitude;
        Vector3 S = a[5] - a[13];
        if (largo > 1e-4f)
        {
            F /= largo;
            S -= F * Vector3.Dot(S, F);
            float ancho = S.magnitude;
            S = ancho > 1e-5f ? S / ancho : Vector3.Cross(F, Vector3.up).normalized;
            Vector3 N = Vector3.Cross(F, S).normalized;
            Vector3 n = Vector3.Cross(a[5] - a[0], a[17] - a[0]);
            Vector3 palma = n.sqrMagnitude > 1e-10f ? (izquierda ? n.normalized : -n.normalized) : N;
            if (Vector3.Dot(N, palma) < 0f)
                N = -N;
            Vector3 c = Vector3.Lerp(a[0], a[9], 0.5f);
            float ex = largo * 0.6f, ey = (ancho * 0.5f + 0.022f) * EscalaGuante, ez = 0.026f * EscalaGuante;
            Elipsoide(c, F * ex, S * ey, N * ez);

            // Las 3 rayitas negras del dorso (cortas).
            triangulos = partes[1];
            for (int j = -1; j <= 1; j++)
            {
                float v = j * 0.3f;
                Vector3 previo = Vector3.zero;
                for (int k = 0; k < 4; k++)
                {
                    float u = Mathf.Lerp(-0.3f, 0.13f, k / 3f);
                    float alto = ez * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u - v * v)) + 0.0015f;
                    Vector3 p = c + F * (u * ex) + S * (v * ey) - N * alto;
                    EsferaFina(p, 0.0026f);
                    if (k > 0)
                        TuboSuave(previo, 0.0026f, p, 0.0026f);
                    previo = p;
                }
            }
        }

        malla.Clear();
        malla.subMeshCount = 3;
        malla.SetVertices(vertices);
        malla.SetNormals(normales);
        for (int k = 0; k < 3; k++)
            malla.SetTriangles(partes[k], k);
        malla.RecalculateBounds();
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    void EsferaFina(Vector3 centro, float radio)
    {
        int b = vertices.Count;
        foreach (var n in esferaFina)
        {
            vertices.Add(centro + n * radio);
            normales.Add(n);
        }
        foreach (int i in esferaFinaTri)
            triangulos.Add(b + i);
    }

    // Como Tubo, pero con más lados (el contorno se ve redondo, no a pedazos).
    void TuboSuave(Vector3 a, float ra, Vector3 b, float rb)
    {
        Vector3 d = b - a;
        if (d.sqrMagnitude < 1e-10f)
            return;
        const int lados = 18;
        Vector3 eje = d.normalized;
        Vector3 u = Vector3.Cross(eje, Mathf.Abs(eje.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 w = Vector3.Cross(eje, u);
        int baseIndice = vertices.Count;
        for (int k = 0; k < lados; k++)
        {
            float ang = 2f * Mathf.PI * k / lados;
            Vector3 radial = u * Mathf.Cos(ang) + w * Mathf.Sin(ang);
            vertices.Add(a + radial * ra);
            normales.Add(radial);
            vertices.Add(b + radial * rb);
            normales.Add(radial);
        }
        for (int k = 0; k < lados; k++)
        {
            int i0 = baseIndice + k * 2, i1 = i0 + 1;
            int j0 = baseIndice + ((k + 1) % lados) * 2, j1 = j0 + 1;
            triangulos.Add(i0); triangulos.Add(i1); triangulos.Add(j0);
            triangulos.Add(j0); triangulos.Add(i1); triangulos.Add(j1);
        }
    }

    // Una esfera estirada (el dorso del guante), con más detalle que las de los dedos.
    void Elipsoide(Vector3 c, Vector3 ex, Vector3 ey, Vector3 ez)
    {
        float lx = Mathf.Max(1e-5f, ex.magnitude), ly = Mathf.Max(1e-5f, ey.magnitude), lz = Mathf.Max(1e-5f, ez.magnitude);
        Vector3 ux = ex / lx, uy = ey / ly, uz = ez / lz;
        int b = vertices.Count;
        foreach (var n in esferaFina)
        {
            vertices.Add(c + ex * n.x + ey * n.y + ez * n.z);
            normales.Add((ux * (n.x / lx) + uy * (n.y / ly) + uz * (n.z / lz)).normalized);
        }
        foreach (int i in esferaFinaTri)
            triangulos.Add(b + i);
    }

    static void ArmarEsferaFina()
    {
        if (esferaFina != null)
            return;
        const int lados = 18, paralelos = 12;
        var v = new List<Vector3>();
        var t = new List<int>();
        for (int p = 0; p <= paralelos; p++)
        {
            float lat = Mathf.PI * p / paralelos;
            for (int m = 0; m <= lados; m++)
            {
                float lon = 2f * Mathf.PI * m / lados;
                v.Add(new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon), Mathf.Sin(lat) * Mathf.Sin(lon), Mathf.Cos(lat)));
            }
        }
        for (int p = 0; p < paralelos; p++)
            for (int m = 0; m < lados; m++)
            {
                int i0 = p * (lados + 1) + m;
                int i1 = i0 + lados + 1;
                t.Add(i0); t.Add(i1); t.Add(i0 + 1);
                t.Add(i0 + 1); t.Add(i1); t.Add(i1 + 1);
            }
        esferaFina = v.ToArray();
        esferaFinaTri = t.ToArray();
    }

    public void Destruir()
    {
        if (materialGuante != null)
            Object.Destroy(materialGuante);
        if (materialManga != null)
            Object.Destroy(materialManga);
        if (malla != null)
            Object.Destroy(malla);
        if (go != null)
            Object.Destroy(go);
    }

    static void ArmarEsfera()
    {
        if (esfera != null)
            return;
        var v = new List<Vector3>();
        var t = new List<int>();
        for (int p = 0; p <= Paralelos; p++)
        {
            float lat = Mathf.PI * p / Paralelos;
            for (int m = 0; m <= Lados; m++)
            {
                float lon = 2f * Mathf.PI * m / Lados;
                v.Add(new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(lon)));
            }
        }
        for (int p = 0; p < Paralelos; p++)
            for (int m = 0; m < Lados; m++)
            {
                int a = p * (Lados + 1) + m;
                int b = a + Lados + 1;
                t.Add(a); t.Add(b); t.Add(a + 1);
                t.Add(a + 1); t.Add(b); t.Add(b + 1);
            }
        esfera = v.ToArray();
        esferaTri = t.ToArray();
    }

    void Esfera(Vector3 centro, float radio)
    {
        int b = vertices.Count;
        foreach (var n in esfera)
        {
            vertices.Add(centro + n * radio);
            normales.Add(n);
        }
        foreach (int i in esferaTri)
            triangulos.Add(b + i);
    }

    void Tubo(Vector3 a, float ra, Vector3 b, float rb)
    {
        Vector3 d = b - a;
        if (d.sqrMagnitude < 1e-8f)
            return;
        Vector3 eje = d.normalized;
        Vector3 u = Vector3.Cross(eje, Mathf.Abs(eje.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 w = Vector3.Cross(eje, u);
        int baseIndice = vertices.Count;
        for (int k = 0; k < Lados; k++)
        {
            float ang = 2f * Mathf.PI * k / Lados;
            Vector3 radial = u * Mathf.Cos(ang) + w * Mathf.Sin(ang);
            vertices.Add(a + radial * ra);
            normales.Add(radial);
            vertices.Add(b + radial * rb);
            normales.Add(radial);
        }
        for (int k = 0; k < Lados; k++)
        {
            int i0 = baseIndice + k * 2, i1 = i0 + 1;
            int j0 = baseIndice + ((k + 1) % Lados) * 2, j1 = j0 + 1;
            triangulos.Add(i0); triangulos.Add(i1); triangulos.Add(j0);
            triangulos.Add(j0); triangulos.Add(i1); triangulos.Add(j1);
        }
    }

    // La palma: una "tabla" con grosor entre la muñeca, el pulgar y los nudillos.
    void PalmaGruesa(Vector3[] a)
    {
        Vector3 centro = Vector3.zero;
        foreach (int i in Palma)
            centro += a[i];
        centro /= Palma.Length;
        Vector3 normal = Vector3.Cross(a[5] - a[0], a[17] - a[0]);
        if (normal.sqrMagnitude < 1e-10f)
            return;
        normal.Normalize();
        const float grosor = 0.0145f;
        for (int cara = 0; cara < 2; cara++)
        {
            Vector3 lado = normal * (cara == 0 ? grosor : -grosor);
            int c = vertices.Count;
            vertices.Add(centro + lado);
            normales.Add(cara == 0 ? normal : -normal);
            foreach (int i in Palma)
            {
                vertices.Add(a[i] + lado);
                normales.Add(cara == 0 ? normal : -normal);
            }
            for (int k = 0; k < Palma.Length; k++)
            {
                int p0 = c + 1 + k, p1 = c + 1 + (k + 1) % Palma.Length;
                triangulos.Add(c); triangulos.Add(p0); triangulos.Add(p1);
            }
        }
        // Los costados de la palma.
        for (int k = 0; k < Palma.Length; k++)
        {
            Vector3 p0 = a[Palma[k]], p1 = a[Palma[(k + 1) % Palma.Length]];
            Vector3 fuera = ((p0 + p1) * 0.5f - centro);
            fuera = fuera.sqrMagnitude > 1e-10f ? fuera.normalized : normal;
            int b = vertices.Count;
            vertices.Add(p0 + normal * grosor); vertices.Add(p0 - normal * grosor);
            vertices.Add(p1 + normal * grosor); vertices.Add(p1 - normal * grosor);
            for (int r = 0; r < 4; r++)
                normales.Add(fuera);
            triangulos.Add(b); triangulos.Add(b + 1); triangulos.Add(b + 2);
            triangulos.Add(b + 2); triangulos.Add(b + 1); triangulos.Add(b + 3);
        }
    }
}

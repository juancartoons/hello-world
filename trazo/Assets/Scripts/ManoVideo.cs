using System.Collections.Generic;
using UnityEngine;

// Una mano "humana" suave (las manos guía del tutorial y las del video del proceso):
//  - PALMA: un "cojín" de piel (sección casi rectangular con los bordes bien redondeados) que va de la
//    muñeca a los nudillos y sigue un poquito más (la piel entre los dedos). Dorso con carne, sin aristas.
//  - DEDOS: "salchichas" suaves que siguen una curva por sus articulaciones, más gruesas en la base y con
//    la punta redondeada. El pulgar nace carnoso cerca de la muñeca (como la base real del pulgar).
//  - Todo con normales suaves: el contorno de la mano guía se ve limpio (sin líneas de triángulos).
// En el video lleva un GUANTE NEGRO SIN DEDOS (palma y base del pulgar) y una MANGA larga y cerrada.
// Se arma con las 21 articulaciones (en coordenadas del mundo).
public sealed class ManoVideo
{
    // Radio de cada dedo en sus 4 puntos (base, nudillo, nudillo, punta), en metros:
    // pulgar, índice, medio, anular y meñique.
    static readonly float[,] RadioDedo =
    {
        { 0.0185f, 0.0124f, 0.0104f, 0.0092f },
        { 0.0100f, 0.0091f, 0.0082f, 0.0074f },
        { 0.0103f, 0.0094f, 0.0085f, 0.0076f },
        { 0.0098f, 0.0089f, 0.0080f, 0.0072f },
        { 0.0088f, 0.0079f, 0.0071f, 0.0064f },
    };
    const int LadosDedo = 16;        // vueltas redondas (contorno suave)
    const int MuestrasTramo = 6;     // puntos de la curva entre dos articulaciones
    const int LadosPalma = 24;
    const int FilasPalma = 18;
    const float ExpPalma = 2.6f;     // 2 = óvalo; más = más "rectangular" (con bordes redondeados)

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
    readonly Vector3[] curva = new Vector3[3 * MuestrasTramo + 1];
    readonly float[] radios = new float[3 * MuestrasTramo + 1];
    readonly Vector3[] tangentes = new Vector3[3 * MuestrasTramo + 1];
    readonly Vector3[] palmaGrilla = new Vector3[(FilasPalma + 1) * LadosPalma];
    readonly Vector3[] palmaCentros = new Vector3[FilasPalma + 1];
    readonly Vector3[] puntosDedo = new Vector3[4];

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
    }

    public void Poner(Vector3[] a)
    {
        bool ver = a != null && a.Length >= 21;
        if (go.activeSelf != ver)
            go.SetActive(ver);
        if (!ver)
            return;
        vertices.Clear();
        normales.Clear();
        foreach (var l in partes)
            l.Clear();
        triangulos = partes[Guante];
        PalmaSuave(a);
        for (int d = 0; d < 5; d++)
            DedoSuave(a, d);
        // Manga (solo en el video): desde la muñeca hacia atrás, larga y cerrada al final.
        Vector3 atras = a[0] - a[9];
        if (!guia && atras.sqrMagnitude > 1e-8f)
        {
            atras.Normalize();
            triangulos = partes[Manga];
            Vector3 inicio = a[0] + atras * 0.012f;
            Vector3 fin = a[0] + atras * LargoManga;
            TuboSuave(inicio, 0.0235f, fin, 0.0285f);
            Tapa(inicio, -atras, 0.0235f);
            Tapa(fin, atras, 0.0285f);
        }
        malla.Clear();
        malla.subMeshCount = 3;
        malla.SetVertices(vertices);
        malla.SetNormals(normales);
        for (int k = 0; k < 3; k++)
            malla.SetTriangles(partes[k], k);
        malla.RecalculateBounds();
    }

    // La palma: un "cojín" de la muñeca a los nudillos. En cada fila, la sección es una "superelipse"
    // (casi rectangular con los bordes muy redondeados): más ancha en los nudillos, con carne en el dorso.
    // Las dos puntas se redondean como la punta de un huevo.
    void PalmaSuave(Vector3[] a)
    {
        Vector3 nudillos = (a[5] + a[9] + a[13] + a[17]) * 0.25f;
        Vector3 F = nudillos - a[0];
        float largo = F.magnitude;
        if (largo < 1e-4f)
            return;
        F /= largo;
        Vector3 S = a[5] - a[17];
        S -= F * Vector3.Dot(S, F);
        float anchoNudillos = S.magnitude;
        if (anchoNudillos < 1e-5f)
            return;
        S /= anchoNudillos;
        Vector3 N = Vector3.Cross(F, S).normalized;
        // Mitad del ancho y mitad del grosor (metros): en la muñeca, en el medio y en los nudillos.
        float anchoMuneca = 0.0245f, anchoFin = anchoNudillos * 0.5f + 0.0095f;
        const float grosorMuneca = 0.0155f, grosorMedio = 0.016f, grosorFin = 0.0125f;
        // v: 0 = muñeca, 1 = nudillos. Empieza un poco detrás de la muñeca y termina un poco después
        // de los nudillos (la piel entre los dedos).
        const float vAtras = -0.16f, vAdelante = 1.12f;
        for (int f = 0; f <= FilasPalma; f++)
        {
            float v = Mathf.Lerp(vAtras, vAdelante, f / (float)FilasPalma);
            float vc = Mathf.Clamp01(v);
            float tapa = 1f;
            if (v < 0f)
            {
                float e = v / vAtras;
                tapa = Mathf.Sqrt(Mathf.Max(0f, 1f - e * e));
            }
            else if (v > 1f)
            {
                float e = (v - 1f) / (vAdelante - 1f);
                tapa = Mathf.Sqrt(Mathf.Max(0f, 1f - e * e));
            }
            float w = Mathf.Lerp(anchoMuneca, anchoFin, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(vc / 0.65f))) * tapa;
            float t = (vc < 0.5f ? Mathf.Lerp(grosorMuneca, grosorMedio, vc * 2f) : Mathf.Lerp(grosorMedio, grosorFin, (vc - 0.5f) * 2f)) * tapa;
            Vector3 c = a[0] + F * (v * largo);
            palmaCentros[f] = c;
            for (int k = 0; k < LadosPalma; k++)
            {
                float ang = 2f * Mathf.PI * k / LadosPalma;
                float cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                float x = Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), 2f / ExpPalma);
                float y = Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 2f / ExpPalma);
                palmaGrilla[f * LadosPalma + k] = c + S * (x * w) + N * (y * t);
            }
        }
        // Normales suaves (de la forma de la superficie, mirando hacia afuera).
        int b = vertices.Count;
        for (int f = 0; f <= FilasPalma; f++)
            for (int k = 0; k < LadosPalma; k++)
            {
                Vector3 p = palmaGrilla[f * LadosPalma + k];
                Vector3 n;
                if (f == 0)
                {
                    n = -F;
                }
                else if (f == FilasPalma)
                {
                    n = F;
                }
                else
                {
                    Vector3 dk = palmaGrilla[f * LadosPalma + (k + 1) % LadosPalma] - palmaGrilla[f * LadosPalma + (k + LadosPalma - 1) % LadosPalma];
                    Vector3 dv = palmaGrilla[(f + 1) * LadosPalma + k] - palmaGrilla[(f - 1) * LadosPalma + k];
                    n = Vector3.Cross(dk, dv);
                    Vector3 fuera = p - palmaCentros[f];
                    if (n.sqrMagnitude < 1e-16f)
                        n = fuera;
                    if (Vector3.Dot(n, fuera) < 0f)
                        n = -n;
                    n = n.sqrMagnitude > 1e-16f ? n.normalized : F;
                }
                vertices.Add(p);
                normales.Add(n);
            }
        for (int f = 0; f < FilasPalma; f++)
            for (int k = 0; k < LadosPalma; k++)
            {
                int i0 = b + f * LadosPalma + k, i1 = b + (f + 1) * LadosPalma + k;
                int j0 = b + f * LadosPalma + (k + 1) % LadosPalma, j1 = b + (f + 1) * LadosPalma + (k + 1) % LadosPalma;
                triangulos.Add(i0); triangulos.Add(i1); triangulos.Add(j0);
                triangulos.Add(j0); triangulos.Add(i1); triangulos.Add(j1);
            }
    }

    // Un dedo: una "salchicha" suave por la curva de sus 4 puntos (Catmull-Rom), del grosor de la base al
    // de la punta, con las dos puntas redondeadas (la de la punta, un poco achatada, como una yema).
    void DedoSuave(Vector3[] a, int d)
    {
        int b = 1 + d * 4;
        for (int k = 0; k < 4; k++)
            puntosDedo[k] = a[b + k];
        // La base del pulgar se hunde un poco en la palma: así se ve carnosa (no un tubo pegado).
        if (d == 0)
            puntosDedo[0] = Vector3.Lerp(a[1], Vector3.Lerp(a[0], a[9], 0.45f), 0.35f);
        int n = curva.Length;
        for (int s = 0; s < n; s++)
        {
            float u = s / (float)MuestrasTramo;           // 0..3
            int tramo = Mathf.Min(2, Mathf.FloorToInt(u));
            float t = u - tramo;
            Vector3 p0 = puntosDedo[Mathf.Max(0, tramo - 1)];
            Vector3 p1 = puntosDedo[tramo];
            Vector3 p2 = puntosDedo[tramo + 1];
            Vector3 p3 = puntosDedo[Mathf.Min(3, tramo + 2)];
            curva[s] = CatmullRom(p0, p1, p2, p3, t);
            radios[s] = Mathf.Lerp(RadioDedo[d, tramo], RadioDedo[d, tramo + 1], t);
        }
        for (int s = 0; s < n; s++)
        {
            Vector3 dir = curva[Mathf.Min(n - 1, s + 1)] - curva[Mathf.Max(0, s - 1)];
            tangentes[s] = dir.sqrMagnitude > 1e-12f ? dir.normalized : (s > 0 ? tangentes[s - 1] : Vector3.forward);
        }
        // Los dedos de la mano guía pueden ir resaltados (otro color); en el video, la base del pulgar va con guante.
        List<int> lista = guia && resaltado == d ? partes[Manga] : partes[Piel];
        List<int> listaBase = !guia && d == 0 ? partes[Guante] : lista;
        // Marco que no se tuerce a lo largo del dedo (transporte paralelo).
        Vector3 eje0 = tangentes[0];
        Vector3 nn = Vector3.Cross(eje0, Mathf.Abs(eje0.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 nnBase = nn, wwBase = Vector3.Cross(eje0, nn), nnPunta = nn, wwPunta = wwBase;
        int inicio = vertices.Count;
        for (int s = 0; s < n; s++)
        {
            Vector3 eje = tangentes[s];
            nn -= eje * Vector3.Dot(nn, eje);
            if (nn.sqrMagnitude < 1e-10f)
                nn = Vector3.Cross(eje, Mathf.Abs(eje.y) < 0.9f ? Vector3.up : Vector3.right);
            nn.Normalize();
            Vector3 ww = Vector3.Cross(eje, nn);
            if (s == 0)
            {
                nnBase = nn;
                wwBase = ww;
            }
            nnPunta = nn;
            wwPunta = ww;
            for (int k = 0; k < LadosDedo; k++)
            {
                float ang = 2f * Mathf.PI * k / LadosDedo;
                Vector3 radial = nn * Mathf.Cos(ang) + ww * Mathf.Sin(ang);
                vertices.Add(curva[s] + radial * radios[s]);
                normales.Add(radial);
            }
            if (s > 0)
            {
                var tri = s <= MuestrasTramo ? listaBase : lista;
                int r0 = inicio + (s - 1) * LadosDedo, r1 = inicio + s * LadosDedo;
                for (int k = 0; k < LadosDedo; k++)
                {
                    int i0 = r0 + k, i1 = r1 + k, j0 = r0 + (k + 1) % LadosDedo, j1 = r1 + (k + 1) % LadosDedo;
                    tri.Add(i0); tri.Add(i1); tri.Add(j0);
                    tri.Add(j0); tri.Add(i1); tri.Add(j1);
                }
            }
        }
        Cupula(inicio, curva[0], -tangentes[0], nnBase, wwBase, radios[0], 1f, listaBase);
        Cupula(inicio + (n - 1) * LadosDedo, curva[n - 1], tangentes[n - 1], nnPunta, wwPunta, radios[n - 1], 0.6f, lista);
    }

    // Tapa redonda en la punta de un dedo, cosida al último anillo. largo menor que 1 = más achatada.
    void Cupula(int anillo, Vector3 centro, Vector3 eje, Vector3 nn, Vector3 ww, float radio, float largo, List<int> tri)
    {
        const int filas = 4;
        int previo = anillo;
        for (int j = 1; j < filas; j++)
        {
            float fi = 0.5f * Mathf.PI * j / filas;
            float cf = Mathf.Cos(fi), sf = Mathf.Sin(fi);
            int fila = vertices.Count;
            for (int k = 0; k < LadosDedo; k++)
            {
                float ang = 2f * Mathf.PI * k / LadosDedo;
                Vector3 radial = nn * Mathf.Cos(ang) + ww * Mathf.Sin(ang);
                vertices.Add(centro + radial * (radio * cf) + eje * (radio * largo * sf));
                normales.Add((radial * cf + eje * (sf / largo)).normalized);
            }
            for (int k = 0; k < LadosDedo; k++)
            {
                int i0 = previo + k, i1 = fila + k, j0 = previo + (k + 1) % LadosDedo, j1 = fila + (k + 1) % LadosDedo;
                tri.Add(i0); tri.Add(i1); tri.Add(j0);
                tri.Add(j0); tri.Add(i1); tri.Add(j1);
            }
            previo = fila;
        }
        int polo = vertices.Count;
        vertices.Add(centro + eje * (radio * largo));
        normales.Add(eje);
        for (int k = 0; k < LadosDedo; k++)
        {
            tri.Add(previo + k);
            tri.Add(polo);
            tri.Add(previo + (k + 1) % LadosDedo);
        }
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
        for (int k = 0; k < LadosDedo; k++)
        {
            float ang = 2f * Mathf.PI * k / LadosDedo;
            vertices.Add(centro + (u * Mathf.Cos(ang) + w * Mathf.Sin(ang)) * radio);
            normales.Add(normal);
        }
        for (int k = 0; k < LadosDedo; k++)
        {
            triangulos.Add(c);
            triangulos.Add(c + 1 + k);
            triangulos.Add(c + 1 + (k + 1) % LadosDedo);
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

    public const float EscalaGuante = 0.84f; // v38: 10 % más pequeño (antes 0.93)
    readonly Vector3[] guanteA = new Vector3[21];

    public void PonerGuante(Vector3[] entrada, bool izquierda)
    {
        bool ver = entrada != null && entrada.Length >= 21;
        if (go.activeSelf != ver)
            go.SetActive(ver);
        if (!ver)
            return;
        ArmarEsferaFina();
        // Más pequeño que tu mano (v38: 16 %), alrededor del centro del dorso.
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
}

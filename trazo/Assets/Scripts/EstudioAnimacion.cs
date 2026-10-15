using System.Collections.Generic;
using UnityEngine;

// ESTUDIO DE ANIMACIÓN (v44): un fondo vivo DIBUJADO A LÁPIZ por la app (no es una foto): un cuarto de animador
// alrededor de ti, como un boceto en papel: primero las líneas de construcción en AZUL (el lápiz azul de los
// animadores) y encima el GRAFITO, con trazos que se pasan un poquito de las esquinas. Algunos acentos de color.
//  - Adelante: la mesa de luz con su disco, hojas con dibujos, la lámpara de brazo y, en la pared, un corcho con
//    dibujos clavados y un tendedero con hojas colgadas.
//  - A la izquierda: repisas con libros y frascos de lápices. A la derecha: la ventana y una planta.
//  - Atrás: la puerta, un póster y un banco. Arriba: una lámpara colgante.
// Se dibuja una sola vez (una textura 360 de 2048 x 1024) y se pone sobre las paredes de un cuarto de su tamaño real
// (así se ve con profundidad, como si estuvieras adentro).
public static class EstudioAnimacion
{
    const int W = 2048, H = 1024;
    // El cuarto, con tus ojos en (0, 0, 0): piso, techo y paredes.
    const float Piso = -1.55f, Techo = 1.45f, Izq = -2.8f, Der = 2.8f, Frente = 3.0f, Atras = -3.2f;
    static readonly Color Papel = new Color(0.965f, 0.945f, 0.9f);
    static readonly Color Azul = new Color(0.42f, 0.62f, 0.92f);
    static readonly Color Grafito = new Color(0.22f, 0.22f, 0.26f);
    static readonly Color Rojo = new Color(0.85f, 0.3f, 0.28f);
    static readonly Color Naranja = new Color(0.95f, 0.6f, 0.22f);
    static readonly Color Verde = new Color(0.35f, 0.65f, 0.38f);

    static Texture2D textura;
    static Color[] pix;
    static System.Random azar;

    // La dirección (desde tus ojos) en la imagen: u (puede pasar de 1 en la costura de atrás) y v.
    public static Vector2 UV(Vector3 d)
    {
        d.Normalize();
        return new Vector2(Mathf.Atan2(d.x, d.z) / (Mathf.PI * 2f) + 0.5f, Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) / Mathf.PI + 0.5f);
    }

    // Las paredes, el piso y el techo del cuarto (del tamaño de verdad: se ven en 3D, con profundidad), con la
    // imagen puesta según la dirección. Cada triángulo con sus propios vértices (así la costura de atrás no se nota).
    public static Mesh Cuarto()
    {
        const int N = 24;
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var tri = new List<int>();
        Vector3 min = new Vector3(Izq, Piso, Atras), max = new Vector3(Der, Techo, Frente);
        // Cada cara: un origen y dos lados.
        var caras = new[]
        {
            new[] { new Vector3(min.x, min.y, min.z), new Vector3(max.x - min.x, 0, 0), new Vector3(0, 0, max.z - min.z) },   // piso
            new[] { new Vector3(min.x, max.y, min.z), new Vector3(max.x - min.x, 0, 0), new Vector3(0, 0, max.z - min.z) },   // techo
            new[] { new Vector3(min.x, min.y, min.z), new Vector3(0, max.y - min.y, 0), new Vector3(0, 0, max.z - min.z) },   // izquierda
            new[] { new Vector3(max.x, min.y, min.z), new Vector3(0, max.y - min.y, 0), new Vector3(0, 0, max.z - min.z) },   // derecha
            new[] { new Vector3(min.x, min.y, max.z), new Vector3(max.x - min.x, 0, 0), new Vector3(0, max.y - min.y, 0) },   // adelante
            new[] { new Vector3(min.x, min.y, min.z), new Vector3(max.x - min.x, 0, 0), new Vector3(0, max.y - min.y, 0) },   // atrás
        };
        var p = new Vector3[4];
        var t = new Vector2[4];
        foreach (var c in caras)
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                {
                    p[0] = c[0] + c[1] * (i / (float)N) + c[2] * (j / (float)N);
                    p[1] = c[0] + c[1] * ((i + 1) / (float)N) + c[2] * (j / (float)N);
                    p[2] = c[0] + c[1] * ((i + 1) / (float)N) + c[2] * ((j + 1) / (float)N);
                    p[3] = c[0] + c[1] * (i / (float)N) + c[2] * ((j + 1) / (float)N);
                    for (int k = 0; k < 4; k++)
                        t[k] = UV(p[k]);
                    // La costura (atrás): si el cuadrito la cruza, los de un lado pasan a u + 1.
                    float umin = Mathf.Min(Mathf.Min(t[0].x, t[1].x), Mathf.Min(t[2].x, t[3].x));
                    float umax = Mathf.Max(Mathf.Max(t[0].x, t[1].x), Mathf.Max(t[2].x, t[3].x));
                    if (umax - umin > 0.5f)
                        for (int k = 0; k < 4; k++)
                            if (t[k].x < 0.5f)
                                t[k].x += 1f;
                    int b = v.Count;
                    for (int k = 0; k < 4; k++)
                    {
                        v.Add(p[k]);
                        uv.Add(t[k]);
                    }
                    tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
                    tri.Add(b); tri.Add(b + 2); tri.Add(b + 3);
                }
        var m = new Mesh { name = "EstudioAnimacion" };
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(v);
        m.SetUVs(0, uv);
        m.SetTriangles(tri, 0);
        m.RecalculateBounds();
        return m;
    }

    public static Texture2D Textura()
    {
        if (textura != null)
            return textura;
        pix = new Color[W * H];
        azar = new System.Random(44);
        Fondo();
        Dibujar();
        textura = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "EstudioAnimacion" };
        textura.wrapModeU = TextureWrapMode.Repeat;   // la costura queda atrás de ti (u = 0 = 1)
        textura.wrapModeV = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Trilinear;
        textura.anisoLevel = 4;
        textura.SetPixels(pix);
        textura.Apply(true, true);
        pix = null;
        return textura;
    }

    static float R(float a, float b) => a + (float)azar.NextDouble() * (b - a);

    // ---------- El papel: cada superficie con su tono, y grano ----------

    // De pixel a dirección (y al revés en Punto): u = lon / 2π + 0.5 (adelante, +z, queda en el centro de la imagen).
    static void Fondo()
    {
        var senos = new float[W];
        var cosenos = new float[W];
        for (int x = 0; x < W; x++)
        {
            float lon = ((x + 0.5f) / W - 0.5f) * Mathf.PI * 2f;
            senos[x] = Mathf.Sin(lon);
            cosenos[x] = Mathf.Cos(lon);
        }
        for (int y = 0; y < H; y++)
        {
            float lat = ((y + 0.5f) / H - 0.5f) * Mathf.PI;
            float cl = Mathf.Cos(lat), sl = Mathf.Sin(lat);
            for (int x = 0; x < W; x++)
            {
                var d = new Vector3(cl * senos[x], sl, cl * cosenos[x]);
                // ¿Qué superficie del cuarto ve este pixel?
                float t = float.MaxValue;
                int cara = 0;
                Prueba(d.y, Piso, 1, ref t, ref cara);
                Prueba(d.y, Techo, 2, ref t, ref cara);
                Prueba(d.x, Izq, 3, ref t, ref cara);
                Prueba(d.x, Der, 4, ref t, ref cara);
                Prueba(d.z, Frente, 5, ref t, ref cara);
                Prueba(d.z, Atras, 6, ref t, ref cara);
                float tono = cara == 1 ? 0.88f : cara == 2 ? 1.01f : cara == 5 ? 0.985f : cara == 6 ? 0.965f : 0.975f;
                Color c = Papel * tono;
                if (cara == 1)
                    c = Color.Lerp(c, new Color(0.86f, 0.78f, 0.66f), 0.35f);   // el piso de madera clarita
                // Un poquito más oscuro lejos (las esquinas del cuarto), y el grano del papel.
                c *= Mathf.Lerp(1.02f, 0.93f, Mathf.Clamp01((t - 1.5f) / 3f));
                float grano = (Hash(x, y) - 0.5f) * 0.035f;
                c.r += grano;
                c.g += grano;
                c.b += grano;
                c.a = 1f;
                pix[y * W + x] = c;
            }
        }
    }

    static void Prueba(float dEje, float plano, int id, ref float t, ref int cara)
    {
        if (Mathf.Abs(dEje) < 1e-5f)
            return;
        float k = plano / dEje;
        if (k > 0f && k < t)
        {
            t = k;
            cara = id;
        }
    }

    static float Hash(int x, int y)
    {
        uint h = (uint)(x * 374761393 + y * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h & 0xFFFF) / 65535f;
    }

    // ---------- Trazos ----------

    // Una línea a lápiz: azul de construcción (más suelta) o grafito. Se pasa un poquito de sus puntas.
    static void Linea(Vector3 a, Vector3 b, Color c, float grosor, float suelta)
    {
        Vector3 dir = b - a;
        float largo = dir.magnitude;
        if (largo < 1e-4f)
            return;
        dir /= largo;
        float pasa = R(0.01f, 0.04f) * (suelta > 0.01f ? 1.5f : 1f);
        Vector3 p0 = a - dir * R(0f, pasa) + Random3(suelta);
        Vector3 p1 = b + dir * R(0f, pasa) + Random3(suelta);
        Vector3 curva = Random3(suelta * 0.6f);
        float angulo = Vector3.Angle(p0, p1) * Mathf.Deg2Rad;
        int n = Mathf.Clamp(Mathf.CeilToInt(angulo * W / (2f * Mathf.PI) * 2.2f) + 2, 2, 4000);
        float fuerza = R(0.65f, 0.95f);
        for (int i = 0; i <= n; i++)
        {
            float u = i / (float)n;
            Vector3 p = Vector3.Lerp(p0, p1, u) + curva * Mathf.Sin(u * Mathf.PI);
            // Más claro al principio y al final (como el lápiz al apoyarse y al levantarse).
            float presion = fuerza * Mathf.Lerp(0.55f, 1f, Mathf.Sin(u * Mathf.PI));
            Punto(p, c, grosor, presion);
        }
    }

    static Vector3 Random3(float r) => new Vector3(R(-r, r), R(-r, r), R(-r, r));

    // Primero en azul (suelta, a veces dos veces) y después en grafito (más firme).
    static void Boceto(Vector3 a, Vector3 b)
    {
        Linea(a, b, Azul, 1.3f, 0.025f);
        if (azar.NextDouble() < 0.4)
            Linea(a, b, Azul, 1.1f, 0.03f);
        Linea(a, b, Grafito, 1.6f, 0.006f);
    }

    static void Punto(Vector3 p, Color c, float radio, float fuerza)
    {
        float dist = p.magnitude;
        if (dist < 1e-4f)
            return;
        Vector3 d = p / dist;
        float lon = Mathf.Atan2(d.x, d.z);
        float lat = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f));
        float fx = (lon / (Mathf.PI * 2f) + 0.5f) * W - 0.5f;
        float fy = (lat / Mathf.PI + 0.5f) * H - 0.5f;
        // Cerca de arriba y abajo la imagen se estira de lado: el punto también, para que se vea igual de grueso.
        float estira = 1f / Mathf.Max(0.08f, Mathf.Cos(lat));
        float rx = radio * Mathf.Min(estira, 12f), ry = radio;
        int x0 = Mathf.FloorToInt(fx - rx - 1), x1 = Mathf.CeilToInt(fx + rx + 1);
        int y0 = Mathf.Max(0, Mathf.FloorToInt(fy - ry - 1)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(fy + ry + 1));
        for (int y = y0; y <= y1; y++)
            for (int xx = x0; xx <= x1; xx++)
            {
                float dx = (xx - fx) / rx, dy = (y - fy) / ry;
                float k = 1f - Mathf.Sqrt(dx * dx + dy * dy);
                if (k <= 0f)
                    continue;
                int x = ((xx % W) + W) % W;
                int i = y * W + x;
                float a = Mathf.Clamp01(k * 1.6f) * fuerza;
                // Como el lápiz sobre papel: oscurece (no tapa).
                Color o = pix[i];
                pix[i] = new Color(Mathf.Lerp(o.r, o.r * c.r * 1.1f, a), Mathf.Lerp(o.g, o.g * c.g * 1.1f, a), Mathf.Lerp(o.b, o.b * c.b * 1.1f, a), 1f);
            }
    }

    static void Caja(Vector3 min, Vector3 max)
    {
        Vector3[] v =
        {
            new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z),
        };
        for (int i = 0; i < 4; i++)
        {
            Boceto(v[i], v[(i + 1) % 4]);
            Boceto(v[i + 4], v[(i + 1) % 4 + 4]);
            Boceto(v[i], v[i + 4]);
        }
    }

    // Un rectángulo en un plano (centro, eje "a lo ancho", eje "a lo alto").
    static void Rect(Vector3 c, Vector3 ancho, Vector3 alto, Color color, bool azulAntes = true)
    {
        Vector3 a = c - ancho - alto, b = c + ancho - alto, d = c + ancho + alto, e = c - ancho + alto;
        if (azulAntes)
        {
            Boceto(a, b);
            Boceto(b, d);
            Boceto(d, e);
            Boceto(e, a);
        }
        else
        {
            Linea(a, b, color, 1.4f, 0.004f);
            Linea(b, d, color, 1.4f, 0.004f);
            Linea(d, e, color, 1.4f, 0.004f);
            Linea(e, a, color, 1.4f, 0.004f);
        }
    }

    static void Circulo(Vector3 c, Vector3 ejeA, Vector3 ejeB, float r, Color color, float grosor, int lados = 28)
    {
        Vector3 antes = c + ejeA * r;
        for (int i = 1; i <= lados; i++)
        {
            float t = i / (float)lados * Mathf.PI * 2f;
            Vector3 p = c + (ejeA * Mathf.Cos(t) + ejeB * Mathf.Sin(t)) * r;
            Linea(antes, p, color, grosor, 0.002f);
            antes = p;
        }
    }

    // Una carita de caricatura en una hoja (en el plano ejeA/ejeB, de tamaño s).
    static void Carita(Vector3 c, Vector3 ejeA, Vector3 ejeB, float s, Color color)
    {
        Circulo(c, ejeA, ejeB, s, color, 1.2f, 18);
        Circulo(c + ejeA * -s * 0.35f + ejeB * s * 0.25f, ejeA, ejeB, s * 0.12f, color, 1f, 8);
        Circulo(c + ejeA * s * 0.35f + ejeB * s * 0.25f, ejeA, ejeB, s * 0.12f, color, 1f, 8);
        Vector3 antes = c + ejeA * -s * 0.45f - ejeB * s * 0.25f;
        for (int i = 1; i <= 8; i++)
        {
            float t = i / 8f;
            Vector3 p = c + ejeA * Mathf.Lerp(-s * 0.45f, s * 0.45f, t) - ejeB * (s * 0.25f + Mathf.Sin(t * Mathf.PI) * s * 0.25f);
            Linea(antes, p, color, 1f, 0.001f);
            antes = p;
        }
    }

    // ---------- El cuarto ----------

    static void Dibujar()
    {
        Vector3 X = Vector3.right, Y = Vector3.up, Z = Vector3.forward;
        // Las orillas del cuarto.
        Caja(new Vector3(Izq, Piso, Atras), new Vector3(Der, Techo, Frente));
        // Las tablas del piso (suaves).
        for (float x = Izq + 0.35f; x < Der; x += 0.35f)
            Linea(new Vector3(x, Piso, Atras), new Vector3(x, Piso, Frente), Grafito, 0.9f, 0.01f);
        // El zócalo.
        Linea(new Vector3(Izq, Piso + 0.1f, Frente), new Vector3(Der, Piso + 0.1f, Frente), Grafito, 1f, 0.005f);
        Linea(new Vector3(Izq, Piso + 0.1f, Atras), new Vector3(Izq, Piso + 0.1f, Frente), Grafito, 1f, 0.005f);
        Linea(new Vector3(Der, Piso + 0.1f, Atras), new Vector3(Der, Piso + 0.1f, Frente), Grafito, 1f, 0.005f);

        // ADELANTE: la mesa de luz.
        float mesa = Piso + 0.76f;
        Caja(new Vector3(-0.85f, mesa - 0.04f, 1.15f), new Vector3(0.85f, mesa, 1.85f));
        foreach (var pata in new[] { new Vector2(-0.8f, 1.2f), new Vector2(0.8f, 1.2f), new Vector2(-0.8f, 1.8f), new Vector2(0.8f, 1.8f) })
            Boceto(new Vector3(pata.x, mesa - 0.04f, pata.y), new Vector3(pata.x, Piso, pata.y));
        // El tablero inclinado con el disco de animar (y su luz).
        Vector3 c0 = new Vector3(0f, mesa + 0.17f, 1.55f);
        Vector3 inclinado = (Y * 0.8f + Z * 0.6f).normalized;
        Rect(c0, X * 0.42f, inclinado * 0.26f, Grafito);
        Circulo(c0, X, inclinado, 0.2f, Azul, 1.2f);
        Circulo(c0, X, inclinado, 0.2f, Grafito, 1.5f);
        for (int i = 0; i < 6; i++)
            Circulo(c0, X, inclinado, 0.03f + i * 0.025f, new Color(1f, 0.85f, 0.45f), 1.4f, 24);   // la luz de abajo
        Boceto(new Vector3(-0.42f, mesa, 1.35f), c0 - inclinado * 0.26f - X * 0.42f);
        Boceto(new Vector3(0.42f, mesa, 1.35f), c0 - inclinado * 0.26f + X * 0.42f);
        // Hojas sobre el disco, con una carita y la barra de registro.
        Rect(c0 + inclinado * 0.02f, X * 0.13f, inclinado * 0.1f, Grafito, false);
        Carita(c0 + inclinado * 0.03f, X, inclinado, 0.05f, Grafito);
        Linea(c0 - inclinado * 0.14f - X * 0.09f, c0 - inclinado * 0.14f + X * 0.09f, Rojo, 2.2f, 0.002f);
        // La lámpara de brazo (a la derecha de la mesa).
        Vector3 baseL = new Vector3(0.65f, mesa, 1.6f), codo = new Vector3(0.75f, mesa + 0.45f, 1.45f), cabeza = new Vector3(0.4f, mesa + 0.55f, 1.35f);
        Boceto(baseL, codo);
        Boceto(codo, cabeza);
        Circulo(baseL, X, Z, 0.07f, Grafito, 1.3f, 16);
        Circulo(cabeza - Y * 0.05f, X, Z, 0.07f, Grafito, 1.4f, 16);
        Boceto(cabeza, cabeza - Y * 0.05f + X * 0.07f);
        Boceto(cabeza, cabeza - Y * 0.05f - X * 0.07f);
        // Frasco de lápices (izquierda de la mesa).
        Vector3 frasco = new Vector3(-0.65f, mesa, 1.5f);
        Circulo(frasco, X, Z, 0.05f, Grafito, 1.3f, 14);
        Circulo(frasco + Y * 0.12f, X, Z, 0.05f, Grafito, 1.3f, 14);
        Boceto(frasco + X * 0.05f, frasco + X * 0.05f + Y * 0.12f);
        Boceto(frasco - X * 0.05f, frasco - X * 0.05f + Y * 0.12f);
        Color[] lapices = { Rojo, Azul, Naranja, Verde, Grafito };
        for (int i = 0; i < lapices.Length; i++)
        {
            Vector3 p = frasco + Y * 0.1f + X * (-0.03f + i * 0.015f);
            Linea(p, p + Y * 0.14f + X * (i - 2) * 0.012f, lapices[i], 2f, 0.002f);
        }
        // Una silla delante de la mesa (detrás de ti al sentarte: queda baja, adelante).
        Caja(new Vector3(-0.25f, Piso + 0.45f, 0.55f), new Vector3(0.25f, Piso + 0.5f, 0.95f));

        // La pared de adelante: un corcho con dibujos y un tendedero con hojas.
        Vector3 corcho = new Vector3(-1.4f, 0.15f, Frente - 0.01f);
        Rect(corcho, X * 0.55f, Y * 0.4f, Grafito);
        for (int i = 0; i < 5; i++)
        {
            Vector3 h = corcho + X * R(-0.38f, 0.38f) + Y * R(-0.25f, 0.25f);
            Rect(h, X * 0.12f, Y * 0.09f, Grafito, false);
            if (i % 2 == 0)
                Carita(h, X, Y, 0.05f, i == 0 ? Rojo : Grafito);
            else
                for (int k = 0; k < 4; k++)
                    Linea(h + X * R(-0.08f, 0.08f) + Y * R(-0.06f, 0.06f), h + X * R(-0.08f, 0.08f) + Y * R(-0.06f, 0.06f), Azul, 1f, 0.004f);
            Circulo(h + Y * 0.085f, X, Y, 0.008f, Rojo, 2f, 8);   // la tachuela
        }
        // El tendedero (una cuerda que cuelga) con hojas: los cuadros de una animación.
        Vector3 desde = new Vector3(-0.5f, 0.85f, Frente - 0.02f), hasta = new Vector3(2.4f, 0.85f, Frente - 0.02f);
        Vector3 antes = desde;
        for (int i = 1; i <= 20; i++)
        {
            float t = i / 20f;
            Vector3 p = Vector3.Lerp(desde, hasta, t) - Y * Mathf.Sin(t * Mathf.PI) * 0.12f;
            Linea(antes, p, Grafito, 1.2f, 0.002f);
            antes = p;
        }
        for (int i = 0; i < 6; i++)
        {
            float t = 0.12f + i * 0.15f;
            Vector3 p = Vector3.Lerp(desde, hasta, t) - Y * Mathf.Sin(t * Mathf.PI) * 0.12f;
            Rect(p - Y * 0.13f, X * 0.1f, Y * 0.13f, Grafito, false);
            // Un circulito que rebota de hoja en hoja (la pelota que todo animador dibuja).
            float salto = Mathf.Abs(Mathf.Cos(i * 0.9f));
            Circulo(p - Y * (0.22f - salto * 0.14f), X, Y, 0.025f, i == 3 ? Rojo : Grafito, 1.2f, 12);
            Linea(p + X * 0.02f, p - X * 0.02f + Y * 0.01f, Naranja, 2f, 0.001f);   // la pinza
        }

        // IZQUIERDA: repisas con libros y frascos.
        for (int r = 0; r < 3; r++)
        {
            float y = -0.6f + r * 0.55f;
            Caja(new Vector3(Izq, y - 0.03f, 0.2f), new Vector3(Izq + 0.3f, y, 1.9f));
            float z = 0.3f;
            while (z < 1.75f)
            {
                float grueso = R(0.03f, 0.07f), alto = R(0.18f, 0.32f);
                if (azar.NextDouble() < 0.15)
                {
                    z += 0.12f;
                    continue;
                }
                Rect(new Vector3(Izq + 0.15f, y + alto * 0.5f, z + grueso * 0.5f), Z * grueso * 0.5f, Y * alto * 0.5f, Grafito);
                if (azar.NextDouble() < 0.3)
                    Linea(new Vector3(Izq + 0.3f, y + alto * 0.3f, z), new Vector3(Izq + 0.3f, y + alto * 0.3f, z + grueso), lapices[(int)(z * 10) % 4], 2f, 0.002f);
                z += grueso + 0.01f;
            }
        }

        // DERECHA: la ventana (con su cruz y cortina) y una planta.
        Vector3 ventana = new Vector3(Der - 0.01f, 0.25f, 0.6f);
        Rect(ventana, Z * 0.7f, Y * 0.55f, Grafito);
        Boceto(ventana - Z * 0.7f, ventana + Z * 0.7f);
        Boceto(ventana - Y * 0.55f, ventana + Y * 0.55f);
        for (int i = 0; i < 4; i++)
            Linea(ventana + Z * (0.4f + i * 0.08f) + Y * 0.6f, ventana + Z * (0.45f + i * 0.06f) - Y * 0.6f, Azul, 1f, 0.02f);
        // Unas nubecitas afuera.
        Circulo(ventana - Z * 0.3f + Y * 0.25f, Z, Y, 0.08f, Azul, 1.1f, 14);
        Circulo(ventana - Z * 0.18f + Y * 0.28f, Z, Y, 0.1f, Azul, 1.1f, 14);
        Vector3 maceta = new Vector3(Der - 0.35f, Piso, 1.6f);
        Rect(maceta + Y * 0.15f, Z * 0.12f, Y * 0.15f, Naranja, false);
        for (int i = 0; i < 7; i++)
        {
            Vector3 tallo = maceta + Y * 0.3f;
            Vector3 hoja = tallo + Y * R(0.25f, 0.6f) + Z * R(-0.25f, 0.25f) + X * R(-0.15f, 0.1f);
            Linea(tallo, hoja, Verde, 1.8f, 0.03f);
            Circulo(hoja, Z, Y, 0.04f, Verde, 1.3f, 10);
        }

        // ATRÁS: la puerta, un póster con un personaje y un banco.
        Vector3 puerta = new Vector3(1.4f, Piso + 1.0f, Atras + 0.01f);
        Rect(puerta, X * 0.45f, Y * 1.0f, Grafito);
        Circulo(puerta + X * 0.33f, X, Y, 0.03f, Grafito, 1.5f, 10);
        Vector3 poster = new Vector3(-1.2f, 0.2f, Atras + 0.01f);
        Rect(poster, X * 0.45f, Y * 0.6f, Grafito);
        Carita(poster + Y * 0.2f, -X, Y, 0.2f, Grafito);
        Boceto(poster + Y * 0.0f, poster - Y * 0.35f);
        Boceto(poster - Y * 0.35f, poster - Y * 0.5f - X * 0.15f);
        Boceto(poster - Y * 0.35f, poster - Y * 0.5f + X * 0.15f);
        Boceto(poster - Y * 0.12f, poster - Y * 0.05f - X * 0.22f);
        Boceto(poster - Y * 0.12f, poster + Y * 0.02f + X * 0.22f);
        Caja(new Vector3(-0.3f, Piso + 0.4f, Atras + 0.4f), new Vector3(0.3f, Piso + 0.45f, Atras + 0.8f));

        // ARRIBA: una lámpara colgante.
        Vector3 cuelga = new Vector3(0f, Techo, 1.2f), foco = new Vector3(0f, Techo - 0.6f, 1.2f);
        Boceto(cuelga, foco);
        Circulo(foco - Y * 0.15f, X, Z, 0.22f, Grafito, 1.5f, 24);
        Boceto(foco, foco - Y * 0.15f + X * 0.22f);
        Boceto(foco, foco - Y * 0.15f - X * 0.22f);
        Boceto(foco, foco - Y * 0.15f + Z * 0.22f);
        Boceto(foco, foco - Y * 0.15f - Z * 0.22f);
    }
}

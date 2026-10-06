using System.Collections.Generic;
using UnityEngine;

// Los íconos de los instrumentos (carrusel sobre el parlante), dibujados como imágenes pequeñas dentro
// de la app: un círculo con borde negro (blanco, o amarillo si es el que suena) y el dibujo encima.
// Los dibujos son los mismos del simulador (trazo/Herramientas/simulador-sonidos.html): rectángulos,
// círculos, rayas y polígonos sobre una cuadrícula de 48 x 48 (y hacia abajo, como en una página web).
public static class IconosSonido
{
    public const int Tamano = 112; // píxeles de cada imagen

    static readonly Color Tinta = new Color(0.086f, 0.094f, 0.106f);
    static readonly Color Blanco = Color.white;
    static readonly Color Amarillo = new Color(1f, 0.847f, 0.29f);
    static readonly Color Turquesa = new Color(0.235f, 0.76f, 0.72f);

    // Una figura: 0 rectángulo, 1 rectángulo redondeado, 2 círculo, 3 raya (con puntas redondas), 4 polígono convexo.
    struct Forma
    {
        public int tipo;
        public Color color;
        public float a, b, c, d, r;
        public Vector2[] puntos;
    }

    static readonly List<Forma> formas = new List<Forma>();

    static void Rect(Color col, float x, float y, float w, float h) => formas.Add(new Forma { tipo = 0, color = col, a = x, b = y, c = w, d = h });
    static void RectRedondo(Color col, float x, float y, float w, float h, float r) => formas.Add(new Forma { tipo = 1, color = col, a = x, b = y, c = w, d = h, r = r });
    static void Circulo(Color col, float x, float y, float r) => formas.Add(new Forma { tipo = 2, color = col, a = x, b = y, r = r });
    static void Raya(Color col, float x1, float y1, float x2, float y2, float grosor) => formas.Add(new Forma { tipo = 3, color = col, a = x1, b = y1, c = x2, d = y2, r = grosor * 0.5f });
    static void Poligono(Color col, params float[] xy)
    {
        var p = new Vector2[xy.Length / 2];
        for (int i = 0; i < p.Length; i++)
            p[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
        formas.Add(new Forma { tipo = 4, color = col, puntos = p });
    }

    // Los dibujos (en el orden de Sonidos: Piano, Arpa, Orquesta, Cajita, Japan, Post-punk, Drum & Bass, Dubstep, Marimba).
    static void Dibujo(int instrumento)
    {
        formas.Clear();
        switch (instrumento)
        {
            case Sonidos.Piano: // teclas
                RectRedondo(Tinta, 3.75f, 9.75f, 40.5f, 28.5f, 4.25f);
                RectRedondo(Blanco, 6.25f, 12.25f, 35.5f, 23.5f, 1.75f);
                foreach (float x in new[] { 12.6f, 20.2f, 27.8f, 35.4f })
                    Rect(Tinta, x - 1f, 11f, 2f, 26f);
                Rect(Tinta, 10.4f, 11f, 4.4f, 15f);
                Rect(Tinta, 18f, 11f, 4.4f, 15f);
                Rect(Tinta, 33.2f, 11f, 4.4f, 15f);
                break;
            case Sonidos.Arpa:
                Raya(Tinta, 12f, 7f, 12f, 41f, 3f);
                Raya(Tinta, 12f, 7f, 22f, 5f, 3f);
                Raya(Tinta, 22f, 5f, 30f, 8f, 3f);
                Raya(Tinta, 30f, 8f, 38f, 14f, 3f);
                Raya(Tinta, 38f, 14f, 14f, 41f, 3f);
                Raya(Tinta, 11f, 41f, 18f, 41f, 3f);
                Raya(Tinta, 17f, 6.5f, 17f, 36.5f, 1.5f);
                Raya(Tinta, 22f, 5.5f, 22f, 30.5f, 1.5f);
                Raya(Tinta, 27f, 7f, 27f, 26f, 1.5f);
                Raya(Tinta, 32f, 9.5f, 32f, 20.5f, 1.5f);
                break;
            case Sonidos.Orquesta: // violín con su arco
                Circulo(Tinta, 24f, 31f, 9f);
                Circulo(Tinta, 24f, 19f, 6.5f);
                Rect(Tinta, 22.5f, 3f, 3f, 14f);
                Circulo(Tinta, 24f, 4f, 2.5f);
                Raya(Blanco, 21f, 27f, 21f, 35f, 1.5f);
                Raya(Blanco, 27f, 27f, 27f, 35f, 1.5f);
                Raya(Tinta, 6f, 42f, 40f, 10f, 2.2f);
                break;
            case Sonidos.Cajita: // cajita de música con manivela y una notita
                Rect(Tinta, 5.75f, 19.75f, 30.5f, 20.5f);
                Rect(Blanco, 8.25f, 22.25f, 25.5f, 15.5f);
                Rect(Tinta, 5f, 15f, 32f, 6f);
                Raya(Tinta, 35f, 30f, 42f, 30f, 2.5f);
                Raya(Tinta, 42f, 30f, 42f, 37f, 2.5f);
                Circulo(Tinta, 42f, 38f, 2.5f);
                Circulo(Tinta, 25f, 9f, 3f);
                Raya(Tinta, 28f, 9f, 28f, 2f, 2f);
                Raya(Tinta, 28f, 2f, 33f, 4f, 2f);
                break;
            case Sonidos.Japan: // puerta torii
                Poligono(Tinta, 3f, 9f, 45f, 9f, 42f, 14f, 6f, 14f);
                Rect(Tinta, 8f, 18f, 32f, 3.5f);
                Rect(Tinta, 12f, 14f, 4.5f, 30f);
                Rect(Tinta, 31.5f, 14f, 4.5f, 30f);
                Rect(Tinta, 22f, 14f, 4f, 4f);
                break;
            case Sonidos.PostPunk: // guitarra eléctrica
                Circulo(Tinta, 15f, 34f, 8f);
                Circulo(Tinta, 21f, 27f, 6.5f);
                Raya(Tinta, 21f, 27f, 38f, 9f, 3.5f);
                Poligono(Tinta, 36f, 5f, 42f, 8f, 39f, 14f, 34f, 11f);
                Raya(Blanco, 14f, 31f, 19f, 36f, 1.5f);
                Raya(Blanco, 17f, 28f, 22f, 33f, 1.5f);
                break;
            case Sonidos.DrumAndBass: // rayo (velocidad y energía)
                Poligono(Tinta, 28f, 4f, 10f, 27f, 22f, 27f, 26f, 19f);
                Poligono(Tinta, 26f, 19f, 22f, 27f, 18f, 44f, 38f, 19f);
                break;
            case Sonidos.Dubstep: // cara de androide
                Raya(Tinta, 24f, 11f, 24f, 5f, 2.5f);
                Circulo(Tinta, 24f, 4f, 2.5f);
                RectRedondo(Tinta, 8.75f, 9.75f, 30.5f, 30.5f, 8.25f);
                RectRedondo(Blanco, 11.25f, 12.25f, 25.5f, 25.5f, 5.75f);
                Rect(Tinta, 5f, 20f, 5f, 10f);
                Rect(Tinta, 38f, 20f, 5f, 10f);
                RectRedondo(Tinta, 14f, 18f, 20f, 8f, 2f);
                Circulo(Turquesa, 19f, 22f, 1.8f);
                Circulo(Turquesa, 29f, 22f, 1.8f);
                foreach (float x in new[] { 18f, 22f, 26f, 30f })
                    Raya(Tinta, x, 32f, x, 35f, 2f);
                break;
            default: // marimba: teclas de madera y dos baquetas
                Rect(Tinta, 7f, 8f, 5f, 22f);
                Rect(Tinta, 14.5f, 10f, 5f, 18f);
                Rect(Tinta, 22f, 12f, 5f, 14f);
                Rect(Tinta, 29.5f, 14f, 5f, 10f);
                Rect(Tinta, 37f, 16f, 5f, 6f);
                Raya(Tinta, 10f, 44f, 20f, 33f, 2.2f);
                Raya(Tinta, 38f, 44f, 28f, 33f, 2.2f);
                Circulo(Tinta, 20.5f, 32.5f, 3f);
                Circulo(Tinta, 27.5f, 32.5f, 3f);
                break;
        }
    }

    static bool Dentro(ref Forma f, float x, float y)
    {
        switch (f.tipo)
        {
            case 0:
                return x >= f.a && x <= f.a + f.c && y >= f.b && y <= f.b + f.d;
            case 1:
            {
                float cx = Mathf.Clamp(x, f.a + f.r, f.a + f.c - f.r);
                float cy = Mathf.Clamp(y, f.b + f.r, f.b + f.d - f.r);
                float dx = x - cx, dy = y - cy;
                return dx * dx + dy * dy <= f.r * f.r;
            }
            case 2:
            {
                float dx = x - f.a, dy = y - f.b;
                return dx * dx + dy * dy <= f.r * f.r;
            }
            case 3:
            {
                float vx = f.c - f.a, vy = f.d - f.b;
                float largo2 = vx * vx + vy * vy;
                float u = largo2 > 1e-6f ? Mathf.Clamp01(((x - f.a) * vx + (y - f.b) * vy) / largo2) : 0f;
                float dx = x - (f.a + vx * u), dy = y - (f.b + vy * u);
                return dx * dx + dy * dy <= f.r * f.r;
            }
            default:
            {
                // Convexo: el punto queda del mismo lado de todos los bordes.
                var p = f.puntos;
                int signo = 0;
                for (int i = 0; i < p.Length; i++)
                {
                    Vector2 a = p[i], b = p[(i + 1) % p.Length];
                    float cruz = (b.x - a.x) * (y - a.y) - (b.y - a.y) * (x - a.x);
                    int s = cruz > 0f ? 1 : cruz < 0f ? -1 : 0;
                    if (s == 0)
                        continue;
                    if (signo == 0)
                        signo = s;
                    else if (s != signo)
                        return false;
                }
                return true;
            }
        }
    }

    // La imagen del ícono. centro = fondo amarillo (el que suena); si no, blanco.
    public static Texture2D Crear(int instrumento, bool centro)
    {
        Dibujo(instrumento);
        var tex = new Texture2D(Tamano, Tamano, TextureFormat.RGBA32, true);
        tex.name = "Icono" + instrumento + (centro ? "Centro" : "");
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        var px = new Color[Tamano * Tamano];
        float radio = Tamano * 0.5f - 1f;
        float borde = Tamano * 3f / 64f;                 // borde negro (como en el simulador: 3 de 64)
        float escala = Tamano * 44f / 64f / 48f;         // la cuadrícula de 48 ocupa 44 de 64
        float margen = (Tamano - 48f * escala) * 0.5f;
        Color fondo = centro ? Amarillo : Blanco;
        float c0 = Tamano * 0.5f;
        var arr = formas.ToArray();
        for (int py = 0; py < Tamano; py++)
            for (int pxx = 0; pxx < Tamano; pxx++)
            {
                // 4 muestras por píxel (bordes suaves).
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                    {
                        float x = pxx + 0.25f + sx * 0.5f, y = py + 0.25f + sy * 0.5f;
                        float dx = x - c0, dy = y - c0;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist > radio)
                            continue;
                        Color col = dist > radio - borde ? Tinta : fondo;
                        if (dist <= radio - borde)
                        {
                            // A la cuadrícula del dibujo (y hacia abajo).
                            float u = (x - margen) / escala, v = (Tamano - y - margen) / escala;
                            for (int k = 0; k < arr.Length; k++)
                                if (Dentro(ref arr[k], u, v))
                                    col = arr[k].color;
                        }
                        r += col.r; g += col.g; b += col.b; a += 1f;
                    }
                px[py * Tamano + pxx] = a > 0f ? new Color(r / a, g / a, b / a, a / 4f) : new Color(0f, 0f, 0f, 0f);
            }
        tex.SetPixels(px);
        tex.Apply(true, false);
        return tex;
    }
}

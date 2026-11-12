using System.Collections.Generic;
using UnityEngine;

// v62: EL PAPEL DE ANIMACIÓN (para la hoja 2D de adentro de los archivos): una imagen transparente con las
// PERFORACIONES abajo (redonda al centro y dos ranuras, como el papel Acme de los estudios) y el NÚMERO del
// fotograma escrito a mano arriba a la derecha (en un círculo si es clave). Igual que las hojas de la mesa.
public static class PapelAnimacion
{
    static readonly Color32 Grafito = new Color32(56, 56, 64, 255);
    static readonly Color32 Hoyo = new Color32(150, 150, 146, 255);

    // px: w x h. Lo deja transparente y pinta encima.
    public static void Pintar(Color32[] px, int w, int h, int numero, bool clave)
    {
        var vacio = new Color32(255, 255, 255, 0);
        for (int i = 0; i < px.Length; i++)
            px[i] = vacio;
        float k = w / 800f;   // las medidas de la mesa (hoja de 800 de ancho)
        float yHoyos = h * 0.06f;
        Sello(px, w, h, w * 0.5f, yHoyos, 8f * k, Hoyo, 1f, false);
        foreach (float hx in new[] { 0.22f, 0.78f })
            for (float d = -22f; d <= 22f; d += 1f)
                Sello(px, w, h, w * hx + d * k, yHoyos, 5f * k, Hoyo, 1f, false);
        Numero(px, w, h, w - 58f * k, h - 44f * k, k, numero, clave);
    }

    static void Numero(Color32[] px, int w, int h, float cx, float cy, float k, int numero, bool clave)
    {
        string s = Mathf.Max(1, numero).ToString();
        var azar = new System.Random(numero * 7919 + 17);
        float alto = 19f * k, ancho = 10.5f * k, paso = 12.5f * k;
        float x0 = cx - (s.Length - 1) * paso * 0.5f;
        for (int j = 0; j < s.Length; j++)
        {
            int dig = s[j] - '0';
            if (dig < 0 || dig > 9)
                continue;
            var pts = new List<Vector2>();
            foreach (var p in MesaEntrada.Digitos[dig])
                pts.Add(new Vector2(x0 + j * paso + (p.x - 0.5f) * ancho + Temblor(azar, 0.5f * k),
                                    cy + (p.y - 0.5f) * alto + Temblor(azar, 0.5f * k)));
            Linea(px, w, h, MesaEntrada.Chaikin(MesaEntrada.Chaikin(pts)), 1.05f * k, 0.62f);
        }
        if (!clave)
            return;
        float rx = (15f + (s.Length - 1) * 12.5f * 0.5f) * k, ry = 15.5f * k;
        float inicio = (float)azar.NextDouble() * Mathf.PI * 2f, fase = (float)azar.NextDouble() * 6f;
        var c = new List<Vector2>();
        for (int i = 0; i <= 72; i++)
        {
            float t = i / 72f;
            float a = inicio + t * Mathf.PI * 2f * 1.09f;
            float r = 1f + 0.05f * Mathf.Sin(2f * a + fase) - 0.06f * t;
            c.Add(new Vector2(cx + 0.5f * k + Mathf.Cos(a) * rx * r, cy + Mathf.Sin(a) * ry * r));
        }
        Linea(px, w, h, c, 0.95f * k, 0.5f);
    }

    static float Temblor(System.Random azar, float cuanto)
    {
        return ((float)azar.NextDouble() * 2f - 1f) * cuanto;
    }

    static void Linea(Color32[] px, int w, int h, List<Vector2> p, float radio, float fuerza)
    {
        for (int i = 1; i < p.Count; i++)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p[i - 1], p[i]) / 0.6f));
            for (int j = 0; j <= n; j++)
            {
                Vector2 q = Vector2.Lerp(p[i - 1], p[i], j / (float)n);
                Sello(px, w, h, q.x, q.y, Mathf.Max(0.8f, radio), Grafito, fuerza, true);
            }
        }
    }

    // Un sello redondo y suave sobre una imagen transparente (sube la opacidad, con grano si se pide).
    static void Sello(Color32[] px, int w, int h, float cx, float cy, float radio, Color32 color, float fuerza, bool grano)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radio)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + radio));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radio)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + radio));
        float r2 = radio * radio;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy, d2 = dx * dx + dy * dy;
                if (d2 > r2)
                    continue;
                float a = fuerza * (1f - d2 / r2);
                if (grano)
                {
                    uint hh = (uint)(x * 374761393 + y * 668265263);
                    hh = (hh ^ (hh >> 13)) * 1274126177u;
                    a *= 0.55f + 0.45f * ((hh & 0xFFFF) / 65535f);
                }
                int i = y * w + x;
                Color32 o = px[i];
                byte na = (byte)Mathf.Max(o.a, Mathf.Clamp(a * 255f * 1.4f, 0f, 255f));
                px[i] = new Color32(color.r, color.g, color.b, na);
            }
    }
}

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Arma una malla con muchas piezas de colores (colores en los vértices) y guarda en UV3
// la dirección del contorno de cada pieza, para el shader FarmaciaVR/Toon (modo "Malla generada").
// Así cientos de objetos se dibujan como uno solo.
internal class KitMalla
{
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Vector3> normales = new List<Vector3>();
    readonly List<Color> colores = new List<Color>();
    readonly List<Vector4> direcciones = new List<Vector4>();
    readonly List<Vector4> emisiones = new List<Vector4>();

    // Luz propia de noche de las piezas que se agreguen (ventanas encendidas, faros, lámparas).
    // Se guarda en UV2; el shader realista la prende cuando es de noche. Negro = no se prende.
    public Color luzNoche = Color.black;

    Vector4 Emision() => new Vector4(luzNoche.r, luzNoche.g, luzNoche.b, 0f);

    // Grosor relativo del contorno para las piezas que se agreguen (1 = normal, 0.5 = la mitad).
    public float grosorContorno = 1f;

    // Qué tanto brillan (reflejo del sol) las piezas que se agreguen, para el shader realista (0 a 1).
    // Se guarda en el alfa del color; el shader toon no lo usa.
    public float brillo = 1f;

    Color ConBrillo(Color c) => new Color(c.r, c.g, c.b, c.a * brillo);

    Vector4 Dir(Vector3 d, bool contorno) => contorno ? new Vector4(d.x, d.y, d.z, grosorContorno) : new Vector4(0f, 0f, 0f, grosorContorno);
    readonly List<int> triangulos = new List<int>();

    public int CantidadVertices => vertices.Count;

    // ---------- Cajas ----------

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

    // Caja definida por sus esquinas mínima y máxima.
    public void CajaMinMax(Vector3 min, Vector3 max, Color color, bool contorno = true)
    {
        Caja((min + max) * 0.5f, max - min, color, contorno);
    }

    // u × v = normal, así el frente queda hacia afuera.
    void Cara(Vector3 centro, Vector3 m, Vector3 normal, Vector3 u, Vector3 v, Color color, bool contorno)
    {
        float en = Mathf.Abs(Vector3.Dot(m, normal));
        float eu = Mathf.Abs(Vector3.Dot(m, u));
        float ev = Mathf.Abs(Vector3.Dot(m, v));
        Vector3 c = centro + normal * en;
        Cuadro(c - u * eu - v * ev, c - u * eu + v * ev, c + u * eu + v * ev, c + u * eu - v * ev,
            normal, color, centro, contorno);
    }

    // Rectángulo plano (sin contorno) pegado a una cara vertical, para etiquetas y ventanas.
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
        Cuadro(centro - u * hu - v * hv, centro - u * hu + v * hv, centro + u * hu + v * hv, centro + u * hu - v * hv,
            normal.normalized, color, centro, false);
    }

    // Rectángulo horizontal en el piso (mirando hacia arriba), para líneas de calle y parqueadero.
    public void Piso(Vector3 centro, float mitadX, float mitadZ, Color color)
    {
        Vector3 u = Vector3.forward, v = Vector3.right; // forward × right = up
        Cuadro(centro - u * mitadZ - v * mitadX, centro - u * mitadZ + v * mitadX, centro + u * mitadZ + v * mitadX,
            centro + u * mitadZ - v * mitadX, Vector3.up, color, centro, false);
    }

    void Cuadro(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, Color color, Vector3 centroPieza, bool contorno)
    {
        int i = vertices.Count;
        foreach (var p in new[] { p0, p1, p2, p3 })
        {
            vertices.Add(p);
            normales.Add(normal);
            colores.Add(ConBrillo(color));
            direcciones.Add(Dir(p - centroPieza, contorno));
            emisiones.Add(Emision());
        }
        // Orden horario visto desde afuera (frente en Unity).
        triangulos.Add(i); triangulos.Add(i + 2); triangulos.Add(i + 1);
        triangulos.Add(i); triangulos.Add(i + 3); triangulos.Add(i + 2);
    }

    // ---------- Triángulos libres (esferas, cilindros) ----------

    // Agrega un triángulo con sombreado plano; lo voltea si hace falta para que mire hacia "haciaAfuera".
    public void Triangulo(Vector3 a, Vector3 b, Vector3 c, Vector3 haciaAfuera, Color color, Vector3 centroPieza, bool contorno)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        if (Vector3.Dot(n, haciaAfuera) < 0f)
        {
            var t = b; b = c; c = t;
            n = -n;
        }
        n.Normalize();
        int i = vertices.Count;
        foreach (var p in new[] { a, b, c })
        {
            vertices.Add(p);
            normales.Add(n);
            colores.Add(ConBrillo(color));
            direcciones.Add(Dir(p - centroPieza, contorno));
            emisiones.Add(Emision());
        }
        triangulos.Add(i); triangulos.Add(i + 1); triangulos.Add(i + 2);
    }

    // Triángulo con sombreado suave: cada esquina tiene su propia normal (así no se notan los polígonos).
    public void TrianguloSuave(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc,
        Vector3 haciaAfuera, Color color, Vector3 centroPieza, bool contorno)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), haciaAfuera) < 0f)
        {
            var t = b; b = c; c = t;
            var tn = nb; nb = nc; nc = tn;
        }
        int i = vertices.Count;
        var ps = new[] { a, b, c };
        var ns = new[] { na, nb, nc };
        for (int k = 0; k < 3; k++)
        {
            vertices.Add(ps[k]);
            normales.Add(ns[k].normalized);
            colores.Add(ConBrillo(color));
            direcciones.Add(Dir(ps[k] - centroPieza, contorno));
            emisiones.Add(Emision());
        }
        triangulos.Add(i); triangulos.Add(i + 1); triangulos.Add(i + 2);
    }

    // Bloque de 8 esquinas libres (para formas inclinadas, como la cabina de un carro).
    // c[0..3] = esquinas de abajo en orden alrededor; c[4..7] = las de arriba en el mismo orden.
    public void Hexaedro(Vector3[] c, Color color, bool contorno = true)
    {
        Vector3 centro = Vector3.zero;
        foreach (var p in c)
            centro += p;
        centro /= 8f;
        int[][] caras =
        {
            new[] { 0, 1, 2, 3 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 },
            new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 },
        };
        foreach (var f in caras)
        {
            Vector3 a = c[f[0]], b = c[f[1]], d = c[f[2]], e = c[f[3]];
            Vector3 afuera = (a + b + d + e) / 4f - centro;
            Triangulo(a, b, d, afuera, color, centro, contorno);
            Triangulo(a, d, e, afuera, color, centro, contorno);
        }
    }

    // Cuadrilátero plano en cualquier orientación (ventanas, faros), sin contorno.
    public void CuadroLibre(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 haciaAfuera, Color color)
    {
        Triangulo(a, b, c, haciaAfuera, color, a, false);
        Triangulo(a, c, d, haciaAfuera, color, a, false);
    }

    // Cuadrilátero con un color por esquina (con transparencia), para rayos de luz. Sin contorno.
    public void CuadroColores(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Color c0, Color c1, Color c2, Color c3)
    {
        int i = vertices.Count;
        Vector3 n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
        var ps = new[] { p0, p1, p2, p3 };
        var cs = new[] { c0, c1, c2, c3 };
        for (int k = 0; k < 4; k++)
        {
            vertices.Add(ps[k]);
            normales.Add(n);
            colores.Add(cs[k]);
            direcciones.Add(Dir(Vector3.zero, false));
            emisiones.Add(Emision());
        }
        triangulos.Add(i); triangulos.Add(i + 1); triangulos.Add(i + 2);
        triangulos.Add(i); triangulos.Add(i + 2); triangulos.Add(i + 3);
    }

    // Esfera low-poly (icosaedro subdividido). "radios" permite aplastarla o estirarla.
    // Si "haciaAdentro" es true, se ve desde adentro (para el cielo).
    // Si "suave" es true, se sombrea liso (como el "smooth shading" de los programas 3D): no se notan los polígonos.
    public void Esfera(Vector3 centro, Vector3 radios, int subdivisiones, Color color, bool contorno = true, bool haciaAdentro = false, bool suave = false)
    {
        Vector3 inverso = new Vector3(1f / radios.x, 1f / radios.y, 1f / radios.z);
        foreach (var tri in Icosfera(subdivisiones))
        {
            Vector3 a = centro + Vector3.Scale(tri[0], radios);
            Vector3 b = centro + Vector3.Scale(tri[1], radios);
            Vector3 c = centro + Vector3.Scale(tri[2], radios);
            Vector3 afuera = (a + b + c) / 3f - centro;
            if (haciaAdentro)
                afuera = -afuera;
            if (suave)
            {
                float s = haciaAdentro ? -1f : 1f;
                TrianguloSuave(a, b, c, Vector3.Scale(tri[0], inverso) * s, Vector3.Scale(tri[1], inverso) * s,
                    Vector3.Scale(tri[2], inverso) * s, afuera, color, centro, contorno);
            }
            else
            {
                Triangulo(a, b, c, afuera, color, centro, contorno);
            }
        }
    }

    // Esfera lisa con un color distinto en cada punto (por ejemplo, barriga más clara u overol en la parte de abajo).
    // "colorEn" recibe la dirección del punto en una esfera de radio 1 (x, y, z entre -1 y 1).
    public void EsferaColor(Vector3 centro, Vector3 radios, int subdivisiones, System.Func<Vector3, Color> colorEn, bool contorno = true)
    {
        Vector3 inverso = new Vector3(1f / radios.x, 1f / radios.y, 1f / radios.z);
        foreach (var tri in Icosfera(subdivisiones))
        {
            Vector3 a = centro + Vector3.Scale(tri[0], radios);
            Vector3 b = centro + Vector3.Scale(tri[1], radios);
            Vector3 c = centro + Vector3.Scale(tri[2], radios);
            Vector3 afuera = (a + b + c) / 3f - centro;
            TrianguloSuaveColores(a, b, c, Vector3.Scale(tri[0], inverso), Vector3.Scale(tri[1], inverso), Vector3.Scale(tri[2], inverso),
                colorEn(tri[0]), colorEn(tri[1]), colorEn(tri[2]), afuera, centro, contorno);
        }
    }

    void TrianguloSuaveColores(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc,
        Color ca, Color cb, Color cc, Vector3 haciaAfuera, Vector3 centroPieza, bool contorno)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), haciaAfuera) < 0f)
        {
            var t = b; b = c; c = t;
            var tn = nb; nb = nc; nc = tn;
            var tc = cb; cb = cc; cc = tc;
        }
        int i = vertices.Count;
        var ps = new[] { a, b, c };
        var ns = new[] { na, nb, nc };
        var cs = new[] { ca, cb, cc };
        for (int k = 0; k < 3; k++)
        {
            vertices.Add(ps[k]);
            normales.Add(ns[k].normalized);
            colores.Add(ConBrillo(cs[k]));
            direcciones.Add(Dir(ps[k] - centroPieza, contorno));
            emisiones.Add(Emision());
        }
        triangulos.Add(i); triangulos.Add(i + 1); triangulos.Add(i + 2);
    }

    // Esfera del cielo con degradado (horizonte claro, cenit más azul). Se ve desde adentro.
    public void Cielo(float radio, Color horizonte, Color cenit, Color suelo)
    {
        foreach (var tri in Icosfera(3))
        {
            Vector3 a = tri[0] * radio, b = tri[1] * radio, c = tri[2] * radio;
            float y = (tri[0].y + tri[1].y + tri[2].y) / 3f;
            Color color = y >= 0 ? Color.Lerp(horizonte, cenit, Mathf.Pow(y, 0.6f)) : suelo;
            Vector3 afuera = (a + b + c) / 3f;
            Triangulo(a, b, c, -afuera, color, Vector3.zero, false);
        }
    }

    // Cilindro (ruedas, postes). "eje" es la dirección del largo.
    // Si "suave" es true, el costado se sombrea liso (las llantas se ven redondas, sin polígonos).
    public void Cilindro(Vector3 centro, float radio, float largo, Vector3 eje, int lados, Color color, bool contorno = true, bool suave = false)
    {
        eje.Normalize();
        Vector3 u = Vector3.Cross(eje, Mathf.Abs(eje.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        Vector3 v = Vector3.Cross(eje, u);
        Vector3 arriba = centro + eje * (largo / 2f);
        Vector3 abajo = centro - eje * (largo / 2f);
        for (int i = 0; i < lados; i++)
        {
            float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
            Vector3 d0 = (u * Mathf.Cos(a0) + v * Mathf.Sin(a0)) * radio;
            Vector3 d1 = (u * Mathf.Cos(a1) + v * Mathf.Sin(a1)) * radio;
            Vector3 afuera = (d0 + d1) * 0.5f;
            if (suave)
            {
                TrianguloSuave(abajo + d0, arriba + d0, arriba + d1, d0, d0, d1, afuera, color, centro, contorno);
                TrianguloSuave(abajo + d0, arriba + d1, abajo + d1, d0, d1, d1, afuera, color, centro, contorno);
            }
            else
            {
                Triangulo(abajo + d0, arriba + d0, arriba + d1, afuera, color, centro, contorno);
                Triangulo(abajo + d0, arriba + d1, abajo + d1, afuera, color, centro, contorno);
            }
            Triangulo(arriba, arriba + d0, arriba + d1, eje, color, centro, contorno);
            Triangulo(abajo, abajo + d1, abajo + d0, -eje, color, centro, contorno);
        }
    }

    // ---------- Formas curvas (mostrador, techo, LEDs) ----------

    // Normales hacia afuera de una forma cerrada (puntos en el plano XZ), una por punto.
    public static List<Vector2> NormalesDeContorno(List<Vector2> forma)
    {
        int n = forma.Count;
        Vector2 centro = Vector2.zero;
        foreach (var p in forma)
            centro += p;
        centro /= n;
        var normales = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            Vector2 t = forma[(i + 1) % n] - forma[(i - 1 + n) % n];
            Vector2 normal = new Vector2(t.y, -t.x).normalized;
            if (Vector2.Dot(normal, forma[i] - centro) < 0f)
                normal = -normal;
            normales.Add(normal);
        }
        return normales;
    }

    // La misma forma, más grande (d > 0) o más pequeña (d < 0).
    public static List<Vector2> Desplazar(List<Vector2> forma, float d)
    {
        var normales = NormalesDeContorno(forma);
        var nueva = new List<Vector2>(forma.Count);
        for (int i = 0; i < forma.Count; i++)
            nueva.Add(forma[i] + normales[i] * d);
        return nueva;
    }

    // Levanta una forma cerrada y convexa (vista desde arriba, en XZ) entre las alturas y0 y y1.
    // Con "suave" los costados se ven redondeados (sin aristas entre los pedacitos).
    public void Extruir(List<Vector2> forma, float y0, float y1, Color color, bool contorno = true, bool suave = true)
    {
        int n = forma.Count;
        Vector2 c2 = Vector2.zero;
        foreach (var p in forma)
            c2 += p;
        c2 /= n;
        Vector3 centro = new Vector3(c2.x, (y0 + y1) / 2f, c2.y);
        Vector3 centroArriba = new Vector3(c2.x, y1, c2.y), centroAbajo = new Vector3(c2.x, y0, c2.y);
        var normales = NormalesDeContorno(forma);
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Vector3 a0 = new Vector3(forma[i].x, y0, forma[i].y), b0 = new Vector3(forma[j].x, y0, forma[j].y);
            Vector3 a1 = new Vector3(forma[i].x, y1, forma[i].y), b1 = new Vector3(forma[j].x, y1, forma[j].y);
            Vector3 na = new Vector3(normales[i].x, 0f, normales[i].y), nb = new Vector3(normales[j].x, 0f, normales[j].y);
            Vector3 afuera = na + nb;
            if (suave)
            {
                TrianguloSuave(a0, a1, b1, na, na, nb, afuera, color, centro, contorno);
                TrianguloSuave(a0, b1, b0, na, nb, nb, afuera, color, centro, contorno);
            }
            else
            {
                Triangulo(a0, a1, b1, afuera, color, centro, contorno);
                Triangulo(a0, b1, b0, afuera, color, centro, contorno);
            }
            Triangulo(centroArriba, a1, b1, Vector3.up, color, centro, contorno);
            Triangulo(centroAbajo, a0, b0, Vector3.down, color, centro, contorno);
        }
    }

    // Cinta vertical (sin contorno) que sigue una línea de puntos en XZ, entre y0 y y1,
    // mirando hacia "afuera" (una dirección por punto). Para LEDs, bordes y franjas.
    public void CintaVertical(List<Vector2> puntos, List<Vector2> afuera, float y0, float y1, Color color, bool cerrada = false)
    {
        int n = puntos.Count;
        int tramos = cerrada ? n : n - 1;
        for (int i = 0; i < tramos; i++)
        {
            int j = (i + 1) % n;
            Vector3 a0 = new Vector3(puntos[i].x, y0, puntos[i].y), b0 = new Vector3(puntos[j].x, y0, puntos[j].y);
            Vector3 a1 = new Vector3(puntos[i].x, y1, puntos[i].y), b1 = new Vector3(puntos[j].x, y1, puntos[j].y);
            Vector3 na = new Vector3(afuera[i].x, 0f, afuera[i].y), nb = new Vector3(afuera[j].x, 0f, afuera[j].y);
            TrianguloSuave(a0, a1, b1, na, na, nb, na + nb, color, a0, false);
            TrianguloSuave(a0, b1, b0, na, nb, nb, na + nb, color, a0, false);
        }
    }

    // Franja plana entre dos líneas de puntos (misma cantidad), mirando hacia "normal". Sin contorno.
    public void Franja(List<Vector3> a, List<Vector3> b, Vector3 normal, Color color, bool cerrada = false)
    {
        int n = a.Count;
        int tramos = cerrada ? n : n - 1;
        for (int i = 0; i < tramos; i++)
        {
            int j = (i + 1) % n;
            Triangulo(a[i], a[j], b[j], normal, color, a[i], false);
            Triangulo(a[i], b[j], b[i], normal, color, a[i], false);
        }
    }

    // Franja con degradado de color (para resplandores de luz): color "ca" en la línea a y "cb" en la línea b.
    public void FranjaDegradada(List<Vector3> a, List<Vector3> b, Color ca, Color cb, bool cerrada = false)
    {
        int n = a.Count;
        int tramos = cerrada ? n : n - 1;
        for (int i = 0; i < tramos; i++)
        {
            int j = (i + 1) % n;
            CuadroColores(a[i], a[j], b[j], b[i], ca, ca, cb, cb);
        }
    }

    // Disco con degradado: "cc" en el centro y "cb" en el borde (para focos y manchas de luz).
    public void DiscoDegradado(Vector3 centro, Vector3 eje, float radio, Color cc, Color cb, int lados = 16)
    {
        eje.Normalize();
        Vector3 u = Vector3.Cross(eje, Mathf.Abs(eje.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        Vector3 v = Vector3.Cross(eje, u);
        for (int i = 0; i < lados; i++)
        {
            float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
            Vector3 p0 = centro + (u * Mathf.Cos(a0) + v * Mathf.Sin(a0)) * radio;
            Vector3 p1 = centro + (u * Mathf.Cos(a1) + v * Mathf.Sin(a1)) * radio;
            CuadroColores(centro, p0, p1, centro, cc, cb, cb, cc);
        }
    }

    static List<Vector3[]> Icosfera(int subdivisiones)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        var v = new[]
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
        };
        int[] f =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };
        var tris = new List<Vector3[]>();
        for (int i = 0; i < f.Length; i += 3)
            tris.Add(new[] { v[f[i]].normalized, v[f[i + 1]].normalized, v[f[i + 2]].normalized });

        for (int s = 0; s < subdivisiones; s++)
        {
            var nuevos = new List<Vector3[]>();
            foreach (var tri in tris)
            {
                Vector3 ab = ((tri[0] + tri[1]) * 0.5f).normalized;
                Vector3 bc = ((tri[1] + tri[2]) * 0.5f).normalized;
                Vector3 ca = ((tri[2] + tri[0]) * 0.5f).normalized;
                nuevos.Add(new[] { tri[0], ab, ca });
                nuevos.Add(new[] { tri[1], bc, ab });
                nuevos.Add(new[] { tri[2], ca, bc });
                nuevos.Add(new[] { ab, bc, ca });
            }
            tris = nuevos;
        }
        return tris;
    }

    // ---------- Guardar ----------

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
        malla.SetUVs(2, emisiones);
        malla.SetUVs(3, direcciones);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateBounds();
        if (nueva)
            AssetDatabase.CreateAsset(malla, ruta);
        else
            EditorUtility.SetDirty(malla);
        return malla;
    }

    // Crea en la escena un objeto que dibuja esta malla.
    public GameObject CrearObjeto(string nombre, Transform padre, Material material, string carpetaMallas)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = GuardarComo($"{carpetaMallas}/{nombre}.asset");
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }
}
#endif

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
    readonly List<Vector3> direcciones = new List<Vector3>();
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
            colores.Add(color);
            direcciones.Add(contorno ? p - centroPieza : Vector3.zero);
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
            colores.Add(color);
            direcciones.Add(contorno ? p - centroPieza : Vector3.zero);
        }
        triangulos.Add(i); triangulos.Add(i + 1); triangulos.Add(i + 2);
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
            direcciones.Add(Vector3.zero);
        }
        triangulos.Add(i); triangulos.Add(i + 1); triangulos.Add(i + 2);
        triangulos.Add(i); triangulos.Add(i + 2); triangulos.Add(i + 3);
    }

    // Esfera low-poly (icosaedro subdividido) con caras planas. "radios" permite aplastarla o estirarla.
    // Si "hacia Adentro" es true, se ve desde adentro (para el cielo).
    public void Esfera(Vector3 centro, Vector3 radios, int subdivisiones, Color color, bool contorno = true, bool haciaAdentro = false)
    {
        foreach (var tri in Icosfera(subdivisiones))
        {
            Vector3 a = centro + Vector3.Scale(tri[0], radios);
            Vector3 b = centro + Vector3.Scale(tri[1], radios);
            Vector3 c = centro + Vector3.Scale(tri[2], radios);
            Vector3 afuera = (a + b + c) / 3f - centro;
            Triangulo(a, b, c, haciaAdentro ? -afuera : afuera, color, centro, contorno);
        }
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
    public void Cilindro(Vector3 centro, float radio, float largo, Vector3 eje, int lados, Color color, bool contorno = true)
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
            Triangulo(abajo + d0, arriba + d0, arriba + d1, afuera, color, centro, contorno);
            Triangulo(abajo + d0, arriba + d1, abajo + d1, afuera, color, centro, contorno);
            Triangulo(arriba, arriba + d0, arriba + d1, eje, color, centro, contorno);
            Triangulo(abajo, abajo + d1, abajo + d0, -eje, color, centro, contorno);
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

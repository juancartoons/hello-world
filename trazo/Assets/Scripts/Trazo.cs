using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum EstiloLinea { Cinta = 0, Tubo = 1 }

// Una línea del dibujo. Mientras se dibuja usa los puntos "crudos" del dedo; al terminar,
// los simplifica a pocos nodos (los "vectores" editables) y dibuja una curva suave entre ellos.
// Grosor con "valor de línea": grueso en el centro y en punta en los extremos.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Trazo : MonoBehaviour
{
    public List<Vector3> nodos = new List<Vector3>(); // en coordenadas locales del Dibujo
    [Tooltip("Grosor máximo (en el centro), en unidades locales del Dibujo")]
    public float ancho = 0.008f;
    public EstiloLinea estilo = EstiloLinea.Cinta;

    const float separacionCrudos = 0.003f;       // metros entre puntos al dibujar
    const float toleranciaSimplificar = 0.003f;  // cuánto puede alejarse la curva al simplificar
    const float pasoMuestras = 0.004f;           // detalle de la curva final
    const int ladosTubo = 8;
    const float puntaMinima = 0.06f;             // grosor de las puntas (fracción del centro)

    readonly List<Vector3> crudos = new List<Vector3>();
    Mesh malla;

    static readonly List<Vector3> muestras = new List<Vector3>();
    static readonly List<float> largos = new List<float>();
    static readonly List<Vector3> vertices = new List<Vector3>();
    static readonly List<Vector3> normales = new List<Vector3>();
    static readonly List<Vector2> uvs = new List<Vector2>();
    static readonly List<int> indices = new List<int>();

    public bool Dibujando => crudos.Count > 0;

    float Escala => Mathf.Max(0.0001f, transform.lossyScale.x);

    public void Configurar(Material material, float anchoInicial, EstiloLinea estiloInicial)
    {
        ancho = anchoInicial;
        estilo = estiloInicial;
        var mr = GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        AsegurarMalla();
    }

    void AsegurarMalla()
    {
        if (malla != null)
            return;
        malla = new Mesh { name = "Trazo" };
        malla.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = malla;
    }

    void OnDestroy()
    {
        if (malla != null)
            Destroy(malla);
    }

    // Agrega un punto mientras se dibuja (en coordenadas locales del Dibujo).
    public void AgregarPuntoCrudo(Vector3 p)
    {
        float sep = separacionCrudos / Escala;
        if (crudos.Count > 0 && (p - crudos[crudos.Count - 1]).sqrMagnitude < sep * sep)
            return;
        crudos.Add(p);
        Reconstruir();
    }

    // Convierte el trazo crudo en nodos. Devuelve false si quedó demasiado corto.
    public bool Terminar()
    {
        if (crudos.Count < 2 || Largo(crudos) < 0.01f / Escala)
        {
            crudos.Clear();
            return false;
        }
        nodos.Clear();
        Simplificar(crudos, toleranciaSimplificar / Escala, nodos);
        crudos.Clear();
        Reconstruir();
        return nodos.Count >= 2;
    }

    public void Reconstruir()
    {
        AsegurarMalla();
        muestras.Clear();
        if (crudos.Count > 0)
            muestras.AddRange(crudos);
        else
            MuestrearCurva(nodos, pasoMuestras / Escala, muestras);

        malla.Clear();
        if (muestras.Count < 2)
            return;

        largos.Clear();
        float total = 0f;
        largos.Add(0f);
        for (int i = 1; i < muestras.Count; i++)
        {
            total += Vector3.Distance(muestras[i - 1], muestras[i]);
            largos.Add(total);
        }
        if (total < 1e-5f)
            return;

        vertices.Clear();
        normales.Clear();
        uvs.Clear();
        indices.Clear();
        if (estilo == EstiloLinea.Tubo)
            ConstruirTubo(total);
        else
            ConstruirCinta(total);

        malla.indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        malla.SetVertices(vertices);
        malla.SetNormals(normales);
        malla.SetUVs(0, uvs);
        malla.SetTriangles(indices, 0);
        malla.RecalculateBounds();
        var caja = malla.bounds;
        caja.Expand(ancho * 2f);
        malla.bounds = caja;
    }

    // Medio grosor en un punto del trazo: grueso en el centro, en punta en los extremos.
    float MedioGrosor(float recorrido, float total)
    {
        float t = Mathf.Clamp01(recorrido / total);
        float seno = Mathf.Max(0f, Mathf.Sin(Mathf.PI * t));
        float perfil = Mathf.Max(puntaMinima, Mathf.Pow(seno, 0.55f));
        return ancho * 0.5f * perfil;
    }

    Vector3 Tangente(int i)
    {
        int a = Mathf.Max(0, i - 1);
        int b = Mathf.Min(muestras.Count - 1, i + 1);
        Vector3 d = muestras[b] - muestras[a];
        return d.sqrMagnitude > 1e-12f ? d.normalized : Vector3.forward;
    }

    // Cinta: dos vértices por punto en el mismo lugar; el shader los abre mirando a la cámara.
    void ConstruirCinta(float total)
    {
        for (int i = 0; i < muestras.Count; i++)
        {
            Vector3 p = muestras[i];
            Vector3 t = Tangente(i);
            float h = MedioGrosor(largos[i], total);
            vertices.Add(p); normales.Add(t); uvs.Add(new Vector2(-h, 0f));
            vertices.Add(p); normales.Add(t); uvs.Add(new Vector2(h, 0f));
            if (i > 0)
            {
                int a = (i - 1) * 2;
                indices.Add(a); indices.Add(a + 2); indices.Add(a + 1);
                indices.Add(a + 1); indices.Add(a + 2); indices.Add(a + 3);
            }
        }
    }

    // Tubo 3D real (anillos alrededor de la curva).
    void ConstruirTubo(float total)
    {
        Vector3 n = Vector3.zero;
        for (int i = 0; i < muestras.Count; i++)
        {
            Vector3 p = muestras[i];
            Vector3 t = Tangente(i);
            if (i == 0)
            {
                n = Perpendicular(t);
            }
            else
            {
                n = n - t * Vector3.Dot(n, t);
                if (n.sqrMagnitude < 1e-8f)
                    n = Perpendicular(t);
                n.Normalize();
            }
            Vector3 b = Vector3.Cross(t, n);
            float h = MedioGrosor(largos[i], total);
            for (int k = 0; k < ladosTubo; k++)
            {
                float ang = k * Mathf.PI * 2f / ladosTubo;
                Vector3 dir = n * Mathf.Cos(ang) + b * Mathf.Sin(ang);
                vertices.Add(p + dir * h);
                normales.Add(dir);
                uvs.Add(new Vector2(0f, 1f));
            }
            if (i > 0)
            {
                int a = (i - 1) * ladosTubo;
                int c = i * ladosTubo;
                for (int k = 0; k < ladosTubo; k++)
                {
                    int k2 = (k + 1) % ladosTubo;
                    indices.Add(a + k); indices.Add(c + k); indices.Add(a + k2);
                    indices.Add(a + k2); indices.Add(c + k); indices.Add(c + k2);
                }
            }
        }
    }

    static Vector3 Perpendicular(Vector3 t)
    {
        Vector3 eje = Mathf.Abs(t.y) < 0.9f ? Vector3.up : Vector3.right;
        return Vector3.Cross(t, eje).normalized;
    }

    static float Largo(List<Vector3> puntos)
    {
        float total = 0f;
        for (int i = 1; i < puntos.Count; i++)
            total += Vector3.Distance(puntos[i - 1], puntos[i]);
        return total;
    }

    // Curva suave (Catmull-Rom centrípeta) que pasa exactamente por todos los nodos.
    static void MuestrearCurva(List<Vector3> pts, float paso, List<Vector3> salida)
    {
        int n = pts.Count;
        if (n == 0)
            return;
        if (n == 1)
        {
            salida.Add(pts[0]);
            return;
        }
        for (int i = 0; i < n - 1; i++)
        {
            Vector3 p0 = i > 0 ? pts[i - 1] : pts[0] * 2f - pts[1];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = i + 2 < n ? pts[i + 2] : pts[n - 1] * 2f - pts[n - 2];
            float largo = Vector3.Distance(p1, p2);
            int pasos = Mathf.Clamp(Mathf.CeilToInt(largo / paso), 1, 40);
            for (int s = 0; s < pasos; s++)
                salida.Add(CatmullRom(p0, p1, p2, p3, s / (float)pasos));
        }
        salida.Add(pts[n - 1]);
    }

    static float Nudo(Vector3 a, Vector3 b)
    {
        return Mathf.Max(1e-4f, Mathf.Sqrt(Vector3.Distance(a, b)));
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t0 = 0f;
        float t1 = t0 + Nudo(p0, p1);
        float t2 = t1 + Nudo(p1, p2);
        float t3 = t2 + Nudo(p2, p3);
        float u = Mathf.Lerp(t1, t2, t);
        Vector3 a1 = (t1 - u) / (t1 - t0) * p0 + (u - t0) / (t1 - t0) * p1;
        Vector3 a2 = (t2 - u) / (t2 - t1) * p1 + (u - t1) / (t2 - t1) * p2;
        Vector3 a3 = (t3 - u) / (t3 - t2) * p2 + (u - t2) / (t3 - t2) * p3;
        Vector3 b1 = (t2 - u) / (t2 - t0) * a1 + (u - t0) / (t2 - t0) * a2;
        Vector3 b2 = (t3 - u) / (t3 - t1) * a2 + (u - t1) / (t3 - t1) * a3;
        return (t2 - u) / (t2 - t1) * b1 + (u - t1) / (t2 - t1) * b2;
    }

    // Ramer-Douglas-Peucker: deja solo los puntos necesarios para conservar la forma.
    static void Simplificar(List<Vector3> pts, float tolerancia, List<Vector3> salida)
    {
        int n = pts.Count;
        var conservar = new bool[n];
        conservar[0] = true;
        conservar[n - 1] = true;
        var pila = new Stack<Vector2Int>();
        pila.Push(new Vector2Int(0, n - 1));
        while (pila.Count > 0)
        {
            var tramo = pila.Pop();
            int a = tramo.x;
            int b = tramo.y;
            if (b <= a + 1)
                continue;
            float mayor = -1f;
            int indice = -1;
            for (int i = a + 1; i < b; i++)
            {
                float d = DistanciaASegmento(pts[i], pts[a], pts[b]);
                if (d > mayor)
                {
                    mayor = d;
                    indice = i;
                }
            }
            if (indice >= 0 && mayor > tolerancia)
            {
                conservar[indice] = true;
                pila.Push(new Vector2Int(a, indice));
                pila.Push(new Vector2Int(indice, b));
            }
        }
        for (int i = 0; i < n; i++)
            if (conservar[i])
                salida.Add(pts[i]);
    }

    static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float l2 = ab.sqrMagnitude;
        if (l2 < 1e-12f)
            return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
        return Vector3.Distance(p, a + ab * t);
    }
}

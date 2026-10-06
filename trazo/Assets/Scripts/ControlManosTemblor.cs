using System.Collections.Generic;
using UnityEngine;

// Temblor dentro de la paleta de colores (parte de ControlManos). Sin textos: todo se ve.
//  - En el centro de la paleta, una línea corta que tiembla DE VERDAD con el estilo de la capa activa
//    (y del color elegido). Tócala = cambia el temblor: No, Suave, Medio, Fuerte (y otra vez No).
//  - Debajo de la paleta, 5 botoncitos con dibujos:
//      hebras (3 rayitas), grosor vivo (línea gorda en medio), ciclo de 3 / libre (3 puntitos),
//      suavidad (zigzag) y velocidad (>>). Cada toque cambia esa opción y la línea del centro lo muestra.
//    El fondo del botoncito se pone amarillo cuando la opción no es la normal.
public partial class ControlManos
{
    static readonly float[] AmplitudMuestra = { 0f, 0.0008f, 0.0016f, 0.0028f };
    static readonly float[] OndasMuestra = { 1.2f, 2.5f, 5f };
    static readonly Color AmarilloOpcion = new Color(1f, 0.9f, 0.45f);
    const float LargoMuestra = 0.023f;  // media línea de muestra (metros)
    const float RadioCentro = 0.017f;   // dónde se toca la línea del centro
    const float RadioOpcion = 0.011f;   // dónde se toca cada botoncito

    Transform centroTemblor;
    Mesh mallaMuestra;
    readonly List<Transform> botonesTemblor = new List<Transform>();
    readonly List<Material> fondosTemblor = new List<Material>();
    readonly List<Mesh> mallasTemblor = new List<Mesh>();
    readonly List<Vector3> vertsMuestra = new List<Vector3>();
    readonly List<int> trisMuestra = new List<int>();
    readonly List<Vector2> centrosMuestra = new List<Vector2>();
    readonly List<float> anchosMuestra = new List<float>();
    int claveMuestra = int.MinValue;
    bool dedoEnTemblor;

    // Se llama al armar la paleta: la línea del centro y los 5 botoncitos de abajo.
    void ArmarTemblorPaleta(Material negro)
    {
        centroTemblor = new GameObject("TemblorMuestra").transform;
        centroTemblor.SetParent(paleta, false);
        // Un aro fino alrededor y fondo gris clarito (para que se vea que es un botón, y la línea blanca también).
        DiscoPaleta(centroTemblor, negro, 0.056f, new Vector3(0f, 0f, 0.0005f));
        DiscoPaleta(centroTemblor, ColorMaterial(new Color(0.85f, 0.85f, 0.87f)), 0.052f, new Vector3(0f, 0f, 0.0003f));
        mallaMuestra = new Mesh { name = "TemblorMuestra" };
        mallaMuestra.MarkDynamic();
        mallasTemblor.Add(mallaMuestra);
        var linea = new GameObject("Linea");
        linea.transform.SetParent(centroTemblor, false);
        linea.transform.localPosition = new Vector3(0f, 0f, -0.0005f);
        linea.AddComponent<MeshFilter>().sharedMesh = mallaMuestra;
        var r = linea.AddComponent<MeshRenderer>();
        r.sharedMaterial = materialCentroPaleta;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        // Los 5 botoncitos, en un arco debajo de la paleta.
        var iconos = new List<Vector2[]>[5];
        for (int i = 0; i < iconos.Length; i++)
            iconos[i] = new List<Vector2[]>();
        for (int k = -1; k <= 1; k++)
            BarraIcono(iconos[0], new Vector2(-0.27f, k * 0.17f), new Vector2(0.27f, k * 0.17f), 0.07f);
        iconos[1].Add(new[] { new Vector2(-0.33f, 0f), new Vector2(-0.15f, 0.11f), new Vector2(0.15f, 0.11f), new Vector2(0.33f, 0f), new Vector2(0.15f, -0.11f), new Vector2(-0.15f, -0.11f) });
        for (int k = 0; k < 3; k++)
        {
            float a = (90f + k * 120f) * Mathf.Deg2Rad;
            iconos[2].Add(Circulo(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.17f, 0.075f));
        }
        LineaIcono(iconos[3], new[] { new Vector2(-0.3f, 0f), new Vector2(-0.15f, 0.15f), new Vector2(0f, -0.15f), new Vector2(0.15f, 0.15f), new Vector2(0.3f, 0f) }, 0.07f);
        LineaIcono(iconos[4], new[] { new Vector2(-0.25f, 0.17f), new Vector2(-0.06f, 0f), new Vector2(-0.25f, -0.17f) }, 0.07f);
        LineaIcono(iconos[4], new[] { new Vector2(0.04f, 0.17f), new Vector2(0.23f, 0f), new Vector2(0.04f, -0.17f) }, 0.07f);
        for (int i = 0; i < iconos.Length; i++)
        {
            float ang = (-90f + (i - 2) * 19f) * Mathf.Deg2Rad;
            var b = new GameObject("OpcionTemblor" + i).transform;
            b.SetParent(paleta, false);
            b.localPosition = new Vector3(Mathf.Cos(ang) * 0.073f, Mathf.Sin(ang) * 0.073f, -0.001f);
            b.localScale = Vector3.one * 0.02f;
            var fondo = ColorMaterial(Color.white);
            fondosTemblor.Add(fondo);
            DiscoPaleta(b, negro, 1.12f, new Vector3(0f, 0f, 0.05f));
            DiscoPaleta(b, fondo, 1f, new Vector3(0f, 0f, 0.025f));
            var icono = new GameObject("Icono");
            icono.transform.SetParent(b, false);
            icono.AddComponent<MeshFilter>().sharedMesh = MallaIcono(iconos[i]);
            var ri = icono.AddComponent<MeshRenderer>();
            ri.sharedMaterial = negro;
            ri.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ri.receiveShadows = false;
            botonesTemblor.Add(b);
        }
    }

    // Cada cuadro con la paleta abierta: la línea de muestra y los botoncitos. Devuelve true si el dedo
    // está tocando algo del temblor (así no se toman colores ni líneas en ese momento).
    bool ActualizarTemblorPaleta(Vector3 punta, bool dedoValido)
    {
        var capa = dibujo != null ? dibujo.CapaActual : null;
        if (capa == null || centroTemblor == null)
            return false;
        DibujarMuestra(capa);
        for (int i = 0; i < fondosTemblor.Count; i++)
        {
            Color c = OpcionDistinta(capa, i) ? AmarilloOpcion : Color.white;
            if (fondosTemblor[i].HasProperty("_BaseColor"))
                fondosTemblor[i].SetColor("_BaseColor", c);
        }
        if (!dedoValido)
        {
            dedoEnTemblor = false;
            return false;
        }
        // ¿Qué toca? -1 = la línea del centro; 0..4 = un botoncito; -2 = nada.
        int tocado = -2;
        if (Vector3.Distance(punta, centroTemblor.position) < RadioCentro)
        {
            tocado = -1;
        }
        else
        {
            float mejor = RadioOpcion;
            for (int i = 0; i < botonesTemblor.Count; i++)
            {
                float d = Vector3.Distance(punta, botonesTemblor[i].position);
                if (d < mejor)
                {
                    mejor = d;
                    tocado = i;
                }
            }
        }
        if (tocado == -2)
        {
            dedoEnTemblor = false;
            return false;
        }
        if (!dedoEnTemblor && dibujo.temblor != null)
        {
            var tb = dibujo.temblor;
            switch (tocado)
            {
                case -1: tb.Siguiente(); break;
                case 0: tb.SiguienteHebras(); break;
                case 1: tb.AlternarGrosor(); break;
                case 2: tb.AlternarCiclo(); break;
                case 3: tb.SiguienteSuavidad(); break;
                default: tb.SiguienteVelocidad(); break;
            }
            Vector3 donde = tocado < 0 ? centroTemblor.position : botonesTemblor[tocado].position;
            Burbuja(donde, tocado < 0 ? 1.1f : 0.8f);
            claveMuestra = int.MinValue;
            // Abierta con el botón: mientras ajustas el temblor no se cierra.
            if (paletaFija)
                paletaFijaHasta = Mathf.Max(paletaFijaHasta, Time.time + 12f);
        }
        dedoEnTemblor = true;
        return true;
    }

    // ¿La opción está cambiada respecto a lo normal? (para pintar su botoncito de amarillo)
    static bool OpcionDistinta(DatosCapa c, int i)
    {
        switch (i)
        {
            case 0: return c.hebras > 1;
            case 1: return c.grosorVivo;
            case 2: return !c.ciclo3;
            case 3: return c.suavidad != 1;
            default: return c.velocidad != 1;
        }
    }

    // La línea de muestra: una curvita que tiembla igual que las líneas de la capa activa.
    void DibujarMuestra(DatosCapa c)
    {
        int nivel = Mathf.Clamp(c.temblor, 0, AmplitudMuestra.Length - 1);
        int hebras = Mathf.Clamp(c.hebras, 1, 5);
        int suavidad = Mathf.Clamp(c.suavidad, 0, 2);
        float cps = Dibujo.Velocidades[Mathf.Clamp(c.velocidad, 0, Dibujo.Velocidades.Length - 1)];
        int cuadro = nivel == 0 ? 0 : Mathf.FloorToInt(Time.time * cps);
        if (c.ciclo3)
            cuadro %= 3;
        int clave = cuadro * 1000 + nivel * 200 + hebras * 20 + suavidad * 4 + (c.grosorVivo ? 1 : 0) + (c.ciclo3 ? 2 : 0);
        if (clave == claveMuestra)
            return;
        claveMuestra = clave;

        const int puntos = 22;
        float amp = AmplitudMuestra[nivel];
        float ondas = OndasMuestra[suavidad];
        float anchoBase = hebras > 1 ? 0.0009f : 0.0018f;
        vertsMuestra.Clear();
        trisMuestra.Clear();
        for (int h = 0; h < hebras; h++)
        {
            // Cada hebra (y cada cuadro) tiembla distinto; en las puntas se juntan.
            float semilla = cuadro * 7.31f + h * 3.17f;
            float f1 = Mathf.Repeat(Mathf.Sin(semilla * 12.9898f) * 43758.55f, 1f) * Mathf.PI * 2f;
            float f2 = Mathf.Repeat(Mathf.Sin(semilla * 78.233f) * 12543.21f, 1f) * Mathf.PI * 2f;
            float separacion = hebras > 1 ? (h - (hebras - 1) * 0.5f) * 0.0011f : 0f;
            centrosMuestra.Clear();
            anchosMuestra.Clear();
            for (int i = 0; i < puntos; i++)
            {
                float u = i / (float)(puntos - 1);
                float x = Mathf.Lerp(-LargoMuestra, LargoMuestra, u);
                float baseY = 0.004f * Mathf.Sin(u * Mathf.PI * 2f);
                float panza = Mathf.Sin(u * Mathf.PI);
                float ruido = Mathf.Sin(u * ondas * Mathf.PI * 2f + f1) * 0.65f + Mathf.Sin(u * ondas * 2.3f * Mathf.PI * 2f + f2) * 0.35f;
                float y = baseY + ruido * amp * (0.35f + 0.65f * panza) + separacion * panza;
                float ancho = anchoBase;
                if (c.grosorVivo)
                    ancho *= 0.35f + 1.3f * panza * (0.85f + 0.15f * Mathf.Sin(f1 + u * 5f));
                else
                    ancho *= 0.6f + 0.4f * Mathf.Sqrt(panza); // puntas un poco más finas
                centrosMuestra.Add(new Vector2(x, y));
                anchosMuestra.Add(ancho);
            }
            // Una cinta: cada punto se abre hacia los dos lados de la línea (se ve por delante y por detrás).
            int inicio = vertsMuestra.Count;
            for (int i = 0; i < puntos; i++)
            {
                Vector2 dir = centrosMuestra[Mathf.Min(puntos - 1, i + 1)] - centrosMuestra[Mathf.Max(0, i - 1)];
                if (dir.sqrMagnitude < 1e-12f)
                    dir = Vector2.right;
                dir.Normalize();
                Vector2 n = new Vector2(-dir.y, dir.x) * anchosMuestra[i] * 0.5f;
                Vector2 p = centrosMuestra[i];
                vertsMuestra.Add(new Vector3(p.x + n.x, p.y + n.y, 0f));
                vertsMuestra.Add(new Vector3(p.x - n.x, p.y - n.y, 0f));
                if (i > 0)
                {
                    int a = inicio + (i - 1) * 2, b = inicio + i * 2;
                    trisMuestra.Add(a); trisMuestra.Add(a + 1); trisMuestra.Add(b + 1);
                    trisMuestra.Add(a); trisMuestra.Add(b + 1); trisMuestra.Add(b);
                    trisMuestra.Add(a); trisMuestra.Add(b + 1); trisMuestra.Add(a + 1);
                    trisMuestra.Add(a); trisMuestra.Add(b); trisMuestra.Add(b + 1);
                }
            }
        }
        mallaMuestra.Clear();
        mallaMuestra.SetVertices(vertsMuestra);
        mallaMuestra.SetTriangles(trisMuestra, 0);
        mallaMuestra.RecalculateBounds();
    }

    // ---------- Iconos (polígonos planos, se ven por los dos lados) ----------

    static Vector2[] Circulo(Vector2 c, float r)
    {
        var p = new Vector2[12];
        for (int i = 0; i < p.Length; i++)
        {
            float a = i * Mathf.PI * 2f / p.Length;
            p[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
        return p;
    }

    static void BarraIcono(List<Vector2[]> partes, Vector2 a, Vector2 b, float grosor)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x) * grosor * 0.5f;
        partes.Add(new[] { a - n, a + n, b + n, b - n });
        partes.Add(Circulo(a, grosor * 0.5f)); // puntas redondas
        partes.Add(Circulo(b, grosor * 0.5f));
    }

    static void LineaIcono(List<Vector2[]> partes, Vector2[] puntos, float grosor)
    {
        for (int i = 0; i + 1 < puntos.Length; i++)
            BarraIcono(partes, puntos[i], puntos[i + 1], grosor);
    }

    // El icono queda un poquito delante del fondo del botoncito (z negativo = hacia ti).
    Mesh MallaIcono(List<Vector2[]> partes)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        foreach (var p in partes)
        {
            int b = v.Count;
            foreach (var q in p)
                v.Add(new Vector3(q.x, q.y, -0.03f));
            for (int i = 1; i < p.Length - 1; i++)
            {
                t.Add(b); t.Add(b + i); t.Add(b + i + 1);
                t.Add(b); t.Add(b + i + 1); t.Add(b + i);
            }
        }
        var m = new Mesh { name = "IconoTemblor" };
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateBounds();
        mallasTemblor.Add(m);
        return m;
    }

    void LiberarTemblorPaleta()
    {
        foreach (var m in mallasTemblor)
            if (m != null)
                Destroy(m);
        mallasTemblor.Clear();
    }
}

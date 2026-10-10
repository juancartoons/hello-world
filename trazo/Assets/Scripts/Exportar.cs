using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

// Exportar el dibujo:
//  - SVG (vector), en dos archivos: "líneas" (cada línea es una curva Bézier con LOS MISMOS nodos y asas que
//    en la app, del mismo grueso) y "como se ve" (formas rellenas con el grosor que cambia y las hebras).
//    Se ve "de frente" (proyección sin perspectiva), así las curvas se conservan exactas.
//  - Foto (PNG): una imagen del dibujo desde donde estás.
public static class Exportar
{
    static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    // adelante: hacia dónde se mira el dibujo. Devuelve null si no hay líneas visibles.
    public static string Svg(Dibujo dibujo, Vector3 adelante)
    {
        Vector3 arriba = Mathf.Abs(Vector3.Dot(adelante.normalized, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        Vector3 derecha = Vector3.Cross(arriba, adelante).normalized;
        Vector3 arribaVista = Vector3.Cross(adelante, derecha).normalized;
        Transform raiz = dibujo.transform;

        // Primero medimos todo para saber el tamaño de la hoja.
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        bool hay = false;
        foreach (var t in dibujo.TrazosEnOrden())
        {
            if (!Dibujo.Editable(t) || t.nodos.Count < 2)
                continue;
            t.AsegurarAsas();
            for (int i = 0; i < t.nodos.Count; i++)
            {
                Medir(Proyectar(raiz.TransformPoint(t.nodos[i]), derecha, arribaVista), ref minX, ref minY, ref maxX, ref maxY);
                Medir(Proyectar(raiz.TransformPoint(t.nodos[i] + t.asaEntrada[i]), derecha, arribaVista), ref minX, ref minY, ref maxX, ref maxY);
                Medir(Proyectar(raiz.TransformPoint(t.nodos[i] + t.asaSalida[i]), derecha, arribaVista), ref minX, ref minY, ref maxX, ref maxY);
            }
            hay = true;
        }
        if (!hay)
            return null;

        float margen = 10f; // mm
        minX -= margen; minY -= margen; maxX += margen; maxY += margen;
        float ancho = maxX - minX;
        float alto = maxY - minY;

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<!-- Hecho con JCartoons -->");
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(N(ancho)).Append("mm\" height=\"").Append(N(alto))
          .Append("mm\" viewBox=\"").Append(N(minX)).Append(' ').Append(N(minY)).Append(' ').Append(N(ancho)).Append(' ').Append(N(alto)).AppendLine("\">");

        // Rellenos primero (debajo), luego las líneas (encima), como en la app.
        sb.AppendLine("<g id=\"rellenos\" stroke=\"none\">");
        foreach (var t in dibujo.TrazosEnOrden())
        {
            if (!Dibujo.Editable(t) || t.nodos.Count < 3 || (!t.cerrado && !t.rellenoAbierto) || !t.relleno)
                continue;
            Color c = t.ColorDelRelleno;
            sb.Append("<path fill=\"#").Append(ColorUtility.ToHtmlStringRGB(c)).Append("\" d=\"");
            Camino(sb, t, raiz, derecha, arribaVista);
            sb.AppendLine("\"/>");
        }
        sb.AppendLine("</g>");

        sb.AppendLine("<g id=\"lineas\" fill=\"none\" stroke=\"#000000\" stroke-linecap=\"round\" stroke-linejoin=\"round\">");
        float escala = dibujo.EscalaMundo;
        foreach (var t in dibujo.TrazosEnOrden())
        {
            if (!Dibujo.Editable(t) || t.nodos.Count < 2 || t.Invisible)
                continue;
            float grosorMm = t.ancho * escala * 1000f * 0.75f;
            sb.Append("<path stroke=\"#").Append(ColorUtility.ToHtmlStringRGB(t.color)).Append("\" stroke-width=\"").Append(N(grosorMm)).Append("\" d=\"");
            Camino(sb, t, raiz, derecha, arribaVista);
            sb.AppendLine("\"/>");
        }
        sb.AppendLine("</g>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    // SVG "COMO SE VE": cada línea es una forma rellena con su contorno, con el grosor que cambia a lo largo
    // (puntas finitas, grosor de cada nodo, grosor vivo) y cada hebra por separado, como en el visor
    // (sin el temblor, que es movimiento). Los rellenos van debajo. Es como "Expandir trazo" en Illustrator.
    public static string SvgComoSeVe(Dibujo dibujo, Vector3 adelante)
    {
        Vector3 arriba = Mathf.Abs(Vector3.Dot(adelante.normalized, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        Vector3 derecha = Vector3.Cross(arriba, adelante).normalized;
        Vector3 arribaVista = Vector3.Cross(adelante, derecha).normalized;
        Transform raiz = dibujo.transform;
        float escala = dibujo.EscalaMundo;

        var formas = new System.Collections.Generic.List<System.Collections.Generic.List<Vector2>>();
        var colores = new System.Collections.Generic.List<Color>();
        var puntos = new System.Collections.Generic.List<Vector3>();
        var medios = new System.Collections.Generic.List<float>();
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var t in dibujo.TrazosEnOrden())
        {
            if (!Dibujo.Editable(t) || t.nodos.Count < 2)
                continue;
            // Los nodos de los rellenos también cuentan para el tamaño de la hoja.
            t.AsegurarAsas();
            for (int i = 0; i < t.nodos.Count; i++)
                Medir(Proyectar(raiz.TransformPoint(t.nodos[i]), derecha, arribaVista), ref minX, ref minY, ref maxX, ref maxY);
            if (t.Invisible || t.oculto)
                continue;
            t.PuntosConGrosor(puntos, medios);
            int n = puntos.Count;
            if (n < 2)
                continue;
            var e = t.EstiloActual;
            int hebras = Mathf.Max(1, e.hebras);
            float delgada = hebras > 1 ? e.grosorHebra : 1f;
            // Largo acumulado (para saber el lugar a lo largo de la línea, de 0 a 1).
            var largo = new float[n];
            for (int i = 1; i < n; i++)
                largo[i] = largo[i - 1] + Vector3.Distance(puntos[i - 1], puntos[i]);
            float total = Mathf.Max(1e-6f, largo[n - 1]);
            for (int hebra = 0; hebra < hebras; hebra++)
            {
                var centro = new Vector2[n];
                var ancho = new float[n];
                for (int i = 0; i < n; i++)
                {
                    Vector3 mundo = raiz.TransformPoint(puntos[i]);
                    float medio = medios[i] * delgada * escala;
                    Vector3 corrido = mundo + Hebra(mundo, hebra, largo[i] / total, medio, e.a, e.b);
                    centro[i] = Proyectar(corrido, derecha, arribaVista);
                    ancho[i] = medio * GrosorVivo(mundo, hebra, e.a, e.b) * 1000f; // mm
                }
                var forma = new System.Collections.Generic.List<Vector2>(n * 2);
                var lado = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    Vector2 d = centro[Mathf.Min(n - 1, i + 1)] - centro[Mathf.Max(0, i - 1)];
                    lado[i] = d.sqrMagnitude > 1e-12f ? new Vector2(-d.y, d.x).normalized : Vector2.up;
                }
                for (int i = 0; i < n; i++)
                    forma.Add(centro[i] + lado[i] * ancho[i]);
                for (int i = n - 1; i >= 0; i--)
                    forma.Add(centro[i] - lado[i] * ancho[i]);
                foreach (var p in forma)
                    Medir(p, ref minX, ref minY, ref maxX, ref maxY);
                formas.Add(forma);
                colores.Add(t.color);
            }
        }
        if (formas.Count == 0)
            return null;

        float margen = 10f; // mm
        minX -= margen; minY -= margen; maxX += margen; maxY += margen;
        float anchoHoja = maxX - minX, altoHoja = maxY - minY;
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<!-- Hecho con JCartoons: líneas como se ven (formas rellenas) -->");
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(N(anchoHoja)).Append("mm\" height=\"").Append(N(altoHoja))
          .Append("mm\" viewBox=\"").Append(N(minX)).Append(' ').Append(N(minY)).Append(' ').Append(N(anchoHoja)).Append(' ').Append(N(altoHoja)).AppendLine("\">");
        sb.AppendLine("<g id=\"rellenos\" stroke=\"none\">");
        foreach (var t in dibujo.TrazosEnOrden())
        {
            if (!Dibujo.Editable(t) || t.nodos.Count < 3 || (!t.cerrado && !t.rellenoAbierto) || !t.relleno)
                continue;
            sb.Append("<path fill=\"#").Append(ColorUtility.ToHtmlStringRGB(t.ColorDelRelleno)).Append("\" d=\"");
            Camino(sb, t, raiz, derecha, arribaVista);
            sb.AppendLine("\"/>");
        }
        sb.AppendLine("</g>");
        sb.AppendLine("<g id=\"lineas\" stroke=\"none\">");
        for (int k = 0; k < formas.Count; k++)
        {
            var f = formas[k];
            sb.Append("<path fill=\"#").Append(ColorUtility.ToHtmlStringRGB(colores[k])).Append("\" d=\"M ")
              .Append(N(f[0].x)).Append(' ').Append(N(f[0].y));
            for (int i = 1; i < f.Count; i++)
                sb.Append(" L ").Append(N(f[i].x)).Append(' ').Append(N(f[i].y));
            sb.AppendLine(" Z\"/>");
        }
        sb.AppendLine("</g>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    // ---- Lo mismo que hace el shader de la línea (TrazoTemblor.hlsl), sin el tiempo ----

    static float Frac(float x)
    {
        return x - Mathf.Floor(x);
    }

    static float Hash(Vector3 p)
    {
        p = new Vector3(Frac(p.x * 0.3183099f + 0.1f), Frac(p.y * 0.3183099f + 0.1f), Frac(p.z * 0.3183099f + 0.1f)) * 17f;
        return Frac(p.x * p.y * p.z * (p.x + p.y + p.z));
    }

    static float Ruido(Vector3 x)
    {
        Vector3 i = new Vector3(Mathf.Floor(x.x), Mathf.Floor(x.y), Mathf.Floor(x.z));
        Vector3 f = x - i;
        Vector3 u = new Vector3(f.x * f.x * (3f - 2f * f.x), f.y * f.y * (3f - 2f * f.y), f.z * f.z * (3f - 2f * f.z));
        float a = Hash(i);
        float b = Hash(i + new Vector3(1, 0, 0));
        float c = Hash(i + new Vector3(0, 1, 0));
        float d = Hash(i + new Vector3(1, 1, 0));
        float e = Hash(i + new Vector3(0, 0, 1));
        float g = Hash(i + new Vector3(1, 0, 1));
        float h = Hash(i + new Vector3(0, 1, 1));
        float k = Hash(i + new Vector3(1, 1, 1));
        return Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(a, b, u.x), Mathf.Lerp(c, d, u.x), u.y),
                          Mathf.Lerp(Mathf.Lerp(e, g, u.x), Mathf.Lerp(h, k, u.x), u.y), u.z);
    }

    static Vector3 Ruido3(Vector3 p)
    {
        Vector3 suma = Vector3.one;
        return new Vector3(Ruido(p), Ruido(p + suma * 31.4f), Ruido(p + suma * 67.2f)) * 2f - Vector3.one;
    }

    static float Frecuencia(Vector4 b)
    {
        return b.y > 0f ? b.y : 9f;
    }

    static Vector3 Hebra(Vector3 pos, int hebra, float t, float medio, Vector4 a, Vector4 b)
    {
        if (hebra < 1 || a.y <= 0f)
            return Vector3.zero;
        float juntas = Mathf.Clamp01(Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) * 1.6f);
        Vector3 p = pos * (Frecuencia(b) * 1.55f) + hebra * new Vector3(13.7f, 7.1f, 3.3f);
        return Ruido3(p) * a.y * Mathf.Max(medio, 0.0015f) * juntas;
    }

    static float GrosorVivo(Vector3 pos, int hebra, Vector4 a, Vector4 b)
    {
        if (a.z <= 0f)
            return 1f;
        float n = Ruido(pos * (Frecuencia(b) * 1.8f) + Vector3.one * (hebra * 5.1f));
        return Mathf.Lerp(1f, 0.35f + 1.3f * n, a.z);
    }

    // "M x y C ..." con un tramo Bézier por cada par de nodos.
    static void Camino(StringBuilder sb, Trazo t, Transform raiz, Vector3 derecha, Vector3 arriba)
    {
        int n = t.nodos.Count;
        Vector2 inicio = Proyectar(raiz.TransformPoint(t.nodos[0]), derecha, arriba);
        sb.Append("M ").Append(N(inicio.x)).Append(' ').Append(N(inicio.y));
        int segmentos = t.cerrado ? n : n - 1;
        for (int s = 0; s < segmentos; s++)
        {
            int a = s;
            int b = (s + 1) % n;
            Vector2 c1 = Proyectar(raiz.TransformPoint(t.nodos[a] + t.asaSalida[a]), derecha, arriba);
            Vector2 c2 = Proyectar(raiz.TransformPoint(t.nodos[b] + t.asaEntrada[b]), derecha, arriba);
            Vector2 p = Proyectar(raiz.TransformPoint(t.nodos[b]), derecha, arriba);
            sb.Append(" C ").Append(N(c1.x)).Append(' ').Append(N(c1.y))
              .Append(' ').Append(N(c2.x)).Append(' ').Append(N(c2.y))
              .Append(' ').Append(N(p.x)).Append(' ').Append(N(p.y));
        }
        if (t.cerrado)
            sb.Append(" Z");
    }

    // De metros en el mundo a milímetros en la hoja (en SVG la "y" va hacia abajo).
    static Vector2 Proyectar(Vector3 mundo, Vector3 derecha, Vector3 arriba)
    {
        return new Vector2(Vector3.Dot(mundo, derecha) * 1000f, -Vector3.Dot(mundo, arriba) * 1000f);
    }

    static void Medir(Vector2 p, ref float minX, ref float minY, ref float maxX, ref float maxY)
    {
        minX = Mathf.Min(minX, p.x);
        minY = Mathf.Min(minY, p.y);
        maxX = Mathf.Max(maxX, p.x);
        maxY = Mathf.Max(maxY, p.y);
    }

    static string N(float v)
    {
        return v.ToString("0.###", Cultura);
    }

    // Foto (PNG): una cámara temporal (no estéreo) dibuja la escena en una imagen.
    public static byte[] Foto(Vector3 posicion, Quaternion rotacion, float campoVision,
                              int ancho, int alto, Color fondo, int mascara)
    {
        byte[] png = null;
        var captura = new Captura(ancho, alto, mascara, fondo);
        try
        {
            captura.Poner(posicion, rotacion, campoVision);
            png = captura.CapturarPng();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning("TrazoVR: no se pudo tomar la foto: " + e.Message);
        }
        captura.Liberar();
        return png;
    }

    // Cámara para capturar muchos cuadros seguidos (fotos y videos).
    // Solo ve las capas de "mascara" (el dibujo), así no salen paneles, nodos ni imágenes de referencia.
    public sealed class Captura
    {
        readonly GameObject go;
        readonly Camera cam;
        readonly RenderTexture rt;
        readonly Texture2D tex;
        public readonly int ancho, alto;

        public Captura(int ancho, int alto, int mascara, Color fondo)
        {
            this.ancho = ancho;
            this.alto = alto;
            go = new GameObject("CamaraCaptura");
            cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.cullingMask = mascara;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = fondo;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 100f;
            cam.aspect = ancho / (float)alto;
            rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        }

        public void Poner(Vector3 posicion, Quaternion rotacion, float campoVision)
        {
            go.transform.SetPositionAndRotation(posicion, rotacion);
            cam.fieldOfView = Mathf.Clamp(campoVision, 10f, 120f);
        }

        // ---------- Modo rápido: NV12 en la tarjeta gráfica + lectura sin esperar ----------
        Material materialNv12;
        RenderTexture rtNv12;
        static readonly int idTam = Shader.PropertyToID("_Tam");

        // true si este visor puede usar el modo rápido (si no, se usa el modo normal).
        public bool PrepararNv12(Material material)
        {
            if (material == null || !SystemInfo.supportsAsyncGPUReadback
                || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) || ancho % 2 != 0 || alto % 2 != 0)
                return false;
            materialNv12 = material;
            rtNv12 = new RenderTexture(ancho, alto * 3 / 2, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            rtNv12.filterMode = FilterMode.Point;
            rtNv12.Create();
            return true;
        }

        public int TamanoNv12 => ancho * alto * 3 / 2;

        // Dibuja el cuadro, lo convierte a NV12 y pide leerlo (la respuesta llega después).
        public AsyncGPUReadbackRequest PedirNv12()
        {
            Renderizar();
            materialNv12.SetVector(idTam, new Vector4(ancho, alto, 0f, 0f));
            Graphics.Blit(rt, rtNv12, materialNv12);
            return AsyncGPUReadback.Request(rtNv12, 0, TextureFormat.R8);
        }

        void Renderizar()
        {
            var pedido = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, pedido))
            {
                RenderPipeline.SubmitRenderRequest(cam, pedido);
            }
            else
            {
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
            }
        }

        void Dibujar()
        {
            Renderizar();
            var anterior = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            tex.Apply(false);
            RenderTexture.active = anterior;
        }

        // Píxeles RGBA (de abajo hacia arriba), ancho * alto * 4 bytes.
        public byte[] CapturarRgba()
        {
            Dibujar();
            return tex.GetRawTextureData();
        }

        public byte[] CapturarPng()
        {
            Dibujar();
            return tex.EncodeToPNG();
        }

        public void Liberar()
        {
            if (rtNv12 != null)
            {
                rtNv12.Release();
                Object.Destroy(rtNv12);
            }
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
            Object.Destroy(go);
        }
    }
}

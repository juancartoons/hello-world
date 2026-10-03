using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

// Exportar el dibujo:
//  - SVG (vector): cada línea es una curva Bézier con LOS MISMOS nodos y asas que en la app.
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
        foreach (var t in dibujo.trazos)
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
        sb.AppendLine("<!-- Hecho con TrazoVR -->");
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(N(ancho)).Append("mm\" height=\"").Append(N(alto))
          .Append("mm\" viewBox=\"").Append(N(minX)).Append(' ').Append(N(minY)).Append(' ').Append(N(ancho)).Append(' ').Append(N(alto)).AppendLine("\">");

        // Rellenos primero (debajo), luego las líneas (encima), como en la app.
        sb.AppendLine("<g id=\"rellenos\" stroke=\"none\">");
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || t.nodos.Count < 3 || !t.cerrado || !t.relleno)
                continue;
            Color c = Trazo.Paleta[Mathf.Abs(t.colorRelleno) % Trazo.Paleta.Length];
            sb.Append("<path fill=\"#").Append(ColorUtility.ToHtmlStringRGB(c)).Append("\" d=\"");
            Camino(sb, t, raiz, derecha, arribaVista);
            sb.AppendLine("\"/>");
        }
        sb.AppendLine("</g>");

        sb.AppendLine("<g id=\"lineas\" fill=\"none\" stroke=\"#000000\" stroke-linecap=\"round\" stroke-linejoin=\"round\">");
        float escala = dibujo.EscalaMundo;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || t.nodos.Count < 2)
                continue;
            float grosorMm = t.ancho * escala * 1000f * 0.75f;
            sb.Append("<path stroke-width=\"").Append(N(grosorMm)).Append("\" d=\"");
            Camino(sb, t, raiz, derecha, arribaVista);
            sb.AppendLine("\"/>");
        }
        sb.AppendLine("</g>");
        sb.AppendLine("</svg>");
        return sb.ToString();
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

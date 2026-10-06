using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Videos MP4 (se guardan en Dibujos, junto a las fotos):
//  - "Video anim": la animación (con el audio del lipsync, si hay).
//  - "Video proceso": repite lo que grabaste (líneas apareciendo + tus manos, con forma de mano de verdad).
// Modo rápido: el color se convierte al formato del video en la tarjeta gráfica y se hacen varios cuadros
// por paso (sin esperar a que la tarjeta termine cada uno).
//    "Vel" cambia la velocidad de la repetición: x1, x2, x4 u x8.
// Ni los paneles, ni los nodos, ni las imágenes de referencia salen en el video.
public class ExportadorVideo : MonoBehaviour
{
    public const int CapaRepeticion = 30;
    public static readonly int[] Velocidades = { 1, 2, 4, 8 };
    public static bool Exportando { get; private set; }

    public Dibujo dibujo;
    public Animacion animacion;
    public Lipsync lipsync;
    public GrabadorProceso grabador;
    public ControlManos control;
    [Tooltip("Material gris para las manos en el video del proceso")]
    public Material materialManoFantasma;
    [Tooltip("Material de las manos con volumen del video del proceso (shader TrazoVR/ManoVideo)")]
    public Material materialManoVideo;
    [Tooltip("Conversión rápida a NV12 en la tarjeta gráfica (shader TrazoVR/Nv12)")]
    public Material materialNv12;
    [Tooltip("Segundos de trabajo por cuadro de la app (más = video más rápido, pero la vista se congela más)")]
    public float tiempoPorPaso = 0.06f;
    public int ancho = 1280;
    public int alto = 720;
    public int fpsProceso = 30;
    public int velocidad = 1;

    // Dedos de la mano (índices de GrabadorProceso.Huesos) dibujados como líneas.
    static readonly int[][] Cadenas =
    {
        new[] { 0, 1, 2, 3, 4 },
        new[] { 0, 5, 6, 7, 8 },
        new[] { 0, 9, 10, 11, 12 },
        new[] { 0, 13, 14, 15, 16 },
        new[] { 0, 17, 18, 19, 20 },
        new[] { 5, 9, 13, 17 },
    };

    void Start()
    {
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (animacion == null && dibujo != null) animacion = dibujo.animacion;
        if (lipsync == null) lipsync = FindFirstObjectByType<Lipsync>();
        if (grabador == null) grabador = FindFirstObjectByType<GrabadorProceso>();
        if (control == null) control = FindFirstObjectByType<ControlManos>();
    }

    void OnDisable()
    {
        Exportando = false;
    }

    void Mensaje(string texto)
    {
        if (dibujo != null)
            dibujo.Mensaje(texto);
    }

    public void CambiarVelocidad()
    {
        int i = System.Array.IndexOf(Velocidades, velocidad);
        velocidad = Velocidades[(i + 1) % Velocidades.Length];
        Mensaje("Video del proceso: x" + velocidad);
    }

    string NombreArchivo(string tipo)
    {
        return tipo + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }

    // ---------- Video de la animación ----------

    public void ExportarAnimacion()
    {
        if (Exportando || dibujo == null || animacion == null)
            return;
        if (animacion.claves.Count < 2 || animacion.UltimaClave <= 0)
        {
            Mensaje("Necesitas al menos 2 claves para el video");
            return;
        }
        StartCoroutine(VideoAnimacion());
    }

    IEnumerator VideoAnimacion()
    {
        Exportando = true;
        int fps = Mathf.RoundToInt(animacion.fotogramasPorSegundo);
        AudioClip audio = lipsync != null ? lipsync.Clip : null;
        int fin = animacion.UltimaClave;
        if (audio != null)
            fin = Mathf.Max(fin, Mathf.CeilToInt(audio.length * fps));
        fin = Mathf.Clamp(fin, 1, Animacion.TotalFotogramas - 1);

        Vector3 posicion;
        Quaternion rotacion;
        float campoVision;
        dibujo.EncuadreExportar(1.15f, out posicion, out rotacion, out campoVision);
        int volver = animacion.Fotograma;
        animacion.Pausar();
        dibujo.Seleccionar(null); // la línea seleccionada (azul) sale negra en el video

        var codificador = new CodificadorVideo();
        var captura = new Exportar.Captura(ancho, alto, dibujo.MascaraExportar, Color.white);
        captura.Poner(posicion, rotacion, campoVision);
        bool ok = codificador.Iniciar(dibujo.CarpetaDibujos, NombreArchivo("animacion"), ancho, alto, fps, audio);
        var envio = new Envio(codificador, captura, materialNv12);
        Mensaje("Haciendo el video... no te muevas mucho");
        yield return null;

        float avisoEn = Time.time + 1.2f;
        float lote = Time.realtimeSinceStartup;
        for (int f = 0; f <= fin && ok; f++)
        {
            animacion.IrA(f);
            animacion.MostrarFotograma();
            if (dibujo.temblor != null)
                dibujo.temblor.PonerTiempo(f / (float)fps);
            if (dibujo.figuras != null)
                dibujo.figuras.PonerVista(posicion);
            ok = envio.Cuadro();
            if (Time.time > avisoEn)
            {
                avisoEn = Time.time + 1.2f;
                Mensaje("Video: " + Mathf.RoundToInt(100f * f / fin) + "%");
            }
            // Varios cuadros por paso (así tarda mucho menos).
            if (Time.realtimeSinceStartup - lote > tiempoPorPaso)
            {
                yield return null;
                lote = Time.realtimeSinceStartup;
            }
        }
        ok = envio.Terminar() && ok;
        if (ok)
            GuardarMiniatura(captura, codificador);
        captura.Liberar();
        if (dibujo.temblor != null)
            dibujo.temblor.PonerTiempo(-1f);
        if (dibujo.figuras != null)
            dibujo.figuras.PonerVista(null);
        bool listo = codificador.Terminar() && ok;
        animacion.IrA(volver);
        animacion.MostrarFotograma();
        Exportando = false;
        Avisar(listo, codificador);
    }

    // El último cuadro, en pequeño, para el explorador de archivos.
    void GuardarMiniatura(Exportar.Captura captura, CodificadorVideo codificador)
    {
        if (codificador.UsaPng || string.IsNullOrEmpty(codificador.Ruta))
            return;
        try
        {
            byte[] png = captura.CapturarPng();
            Directory.CreateDirectory(dibujo.CarpetaMiniaturas);
            File.WriteAllBytes(dibujo.RutaMiniatura(codificador.Ruta), png);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo guardar la miniatura del video: " + e.Message);
        }
    }

    void Avisar(bool listo, CodificadorVideo codificador)
    {
        if (!listo)
        {
            Debug.LogWarning("TrazoVR: falló el video: " + codificador.Error);
            Mensaje("No se pudo hacer el video");
            return;
        }
        string nombre = Path.GetFileName(codificador.Ruta);
        if (codificador.UsaPng)
        {
            Galeria.UltimoGuardado = "Cuadros PNG: " + Galeria.Donde("", codificador.Ruta) + " (usa hacer_video.bat)";
            Mensaje("Cuadros PNG guardados en " + nombre + " (usa hacer_video.bat)");
            return;
        }
        Galeria.UltimoGuardado = "Video " + nombre + ": " + Galeria.Donde(codificador.RutaPublica, codificador.Ruta);
        Mensaje("Video listo. Búscalo en la " + Galeria.Donde(codificador.RutaPublica, codificador.Ruta));
    }

    // ---------- Video del proceso ----------

    public void ExportarProceso()
    {
        if (Exportando || dibujo == null)
            return;
        if (grabador == null || grabador.muestras.Count < 2)
        {
            Mensaje("Primero graba el proceso (botón Grabar)");
            return;
        }
        if (grabador.Grabando)
        {
            Mensaje("Primero detén la grabación");
            return;
        }
        StartCoroutine(VideoProceso());
    }

    IEnumerator VideoProceso()
    {
        Exportando = true;
        var muestras = grabador.muestras;
        Vector3 posicion;
        Quaternion rotacion;
        float campoVision;
        dibujo.EncuadreExportar(1.4f, out posicion, out rotacion, out campoVision);

        // Lo que se dibuja para el video vive en su propia capa (y la cámara de las gafas no la ve).
        Camera camaraGafas = control != null && control.Cabeza != null ? control.Cabeza.GetComponent<Camera>() : null;
        int mascaraGafas = camaraGafas != null ? camaraGafas.cullingMask : 0;
        if (camaraGafas != null)
            camaraGafas.cullingMask &= ~(1 << CapaRepeticion);

        var raiz = new GameObject("Repeticion");
        raiz.layer = CapaRepeticion;
        var manos = new GameObject("ManosRepeticion");
        manos.layer = CapaRepeticion;
        var lineas = new Dictionary<int, Trazo>();
        // Manos con volumen (como manos de verdad). Si falta el material, las de antes (líneas).
        ManoVideo[] manosVideo = null;
        Trazo[,] dedos = null;
        if (materialManoVideo != null)
        {
            manosVideo = new[] { new ManoVideo(manos.transform, materialManoVideo, CapaRepeticion),
                                 new ManoVideo(manos.transform, materialManoVideo, CapaRepeticion) };
        }
        else
        {
            dedos = new Trazo[2, Cadenas.Length];
            for (int m = 0; m < 2; m++)
                for (int c = 0; c < Cadenas.Length; c++)
                    dedos[m, c] = CrearTrazo(manos.transform, materialManoFantasma != null ? materialManoFantasma : dibujo.materialLinea, 0.012f);
        }

        var codificador = new CodificadorVideo();
        var captura = new Exportar.Captura(ancho, alto, 1 << CapaRepeticion, Color.white);
        captura.Poner(posicion, rotacion, campoVision);
        bool ok = codificador.Iniciar(dibujo.CarpetaDibujos, NombreArchivo("proceso"), ancho, alto, fpsProceso, null);
        var envio = new Envio(codificador, captura, materialNv12);
        Mensaje("Haciendo el video del proceso (x" + velocidad + ")...");
        yield return null;
        float lote = Time.realtimeSinceStartup;

        bool silencio = Trazo.silenciar;
        float duracion = muestras[muestras.Count - 1].tiempo;
        int totalCuadros = Mathf.Max(1, Mathf.CeilToInt(duracion / velocidad * fpsProceso));
        int siguiente = 0;
        var puntos = new List<Vector3>();
        float avisoEn = Time.time + 1.2f;
        for (int cuadro = 0; cuadro <= totalCuadros && ok; cuadro++)
        {
            float t = cuadro * velocidad / (float)fpsProceso;
            MuestraProceso actual = null;
            Trazo.silenciar = true;
            // Se aplican, en orden, todas las muestras hasta este momento.
            while (siguiente < muestras.Count && muestras[siguiente].tiempo <= t)
            {
                actual = muestras[siguiente++];
                foreach (var d in actual.cambios)
                    AplicarLinea(lineas, raiz.transform, d, puntos);
            }
            if (actual != null)
            {
                raiz.transform.SetPositionAndRotation(actual.raizPosicion, actual.raizRotacion);
                raiz.transform.localScale = Vector3.one * Mathf.Max(0.0001f, actual.raizEscala);
                var ver = new HashSet<int>(actual.visibles);
                foreach (var par in lineas)
                {
                    bool visible = ver.Contains(par.Key);
                    if (par.Value.gameObject.activeSelf != visible)
                        par.Value.gameObject.SetActive(visible);
                }
                if (manosVideo != null)
                {
                    manosVideo[0].Poner(actual.manoIzq);
                    manosVideo[1].Poner(actual.manoDer);
                }
                else
                {
                    PonerMano(dedos, 0, actual.manoIzq, puntos);
                    PonerMano(dedos, 1, actual.manoDer, puntos);
                }
            }
            Trazo.silenciar = silencio;
            if (dibujo.temblor != null)
                dibujo.temblor.PonerTiempo(t);
            ok = envio.Cuadro();
            if (Time.time > avisoEn)
            {
                avisoEn = Time.time + 1.2f;
                Mensaje("Video del proceso: " + Mathf.RoundToInt(100f * cuadro / totalCuadros) + "%");
            }
            if (Time.realtimeSinceStartup - lote > tiempoPorPaso)
            {
                yield return null;
                lote = Time.realtimeSinceStartup;
            }
        }
        ok = envio.Terminar() && ok;
        if (ok)
            GuardarMiniatura(captura, codificador);
        Trazo.silenciar = silencio;
        Trazo.huboCambio = false;
        captura.Liberar();
        if (manosVideo != null)
            foreach (var mv in manosVideo)
                mv.Destruir();
        if (dibujo.temblor != null)
            dibujo.temblor.PonerTiempo(-1f);
        bool listo = codificador.Terminar() && ok;
        Destroy(raiz);
        Destroy(manos);
        if (camaraGafas != null)
            camaraGafas.cullingMask = mascaraGafas;
        Exportando = false;
        Avisar(listo, codificador);
    }

    Trazo CrearTrazo(Transform padre, Material material, float ancho)
    {
        var go = new GameObject("TrazoRepeticion");
        go.layer = CapaRepeticion;
        go.transform.SetParent(padre, false);
        var t = go.AddComponent<Trazo>();
        t.Configurar(material, dibujo.materialRelleno, ancho, EstiloLinea.Cinta);
        return t;
    }

    void AplicarLinea(Dictionary<int, Trazo> lineas, Transform raiz, DatosTrazo d, List<Vector3> puntos)
    {
        if (d == null || d.nodos == null)
            return;
        Trazo t;
        if (!lineas.TryGetValue(d.id, out t) || t == null)
        {
            t = CrearTrazo(raiz, dibujo.materialLinea, d.ancho > 0f ? d.ancho : 0.008f);
            t.id = d.id;
            t.capa = d.capa;
            lineas[d.id] = t;
        }
        t.color = d.color; // así la tinta invisible tampoco sale en el video
        if (d.crudo)
        {
            t.ancho = d.ancho > 0f ? d.ancho : 0.008f;
            puntos.Clear();
            puntos.AddRange(d.nodos);
            t.PonerCrudos(puntos);
        }
        else if (d.nodos.Count >= 2)
        {
            t.AplicarPose(d, null, 0f);
        }
    }

    static void PonerMano(Trazo[,] dedos, int m, Vector3[] articulaciones, List<Vector3> puntos)
    {
        bool ver = articulaciones != null && articulaciones.Length >= GrabadorProceso.Huesos.Length;
        for (int c = 0; c < Cadenas.Length; c++)
        {
            var t = dedos[m, c];
            if (t.gameObject.activeSelf != ver)
                t.gameObject.SetActive(ver);
            if (!ver)
                continue;
            puntos.Clear();
            foreach (int i in Cadenas[c])
                puntos.Add(articulaciones[i]);
            t.PonerCrudos(puntos);
        }
    }

    // Manda cada cuadro al video. Modo rápido: convierte a NV12 en la tarjeta gráfica y deja hasta 3 cuadros
    // "en camino" (no espera a la tarjeta en cada uno). Si el visor no puede, usa el modo normal.
    sealed class Envio
    {
        readonly CodificadorVideo codificador;
        readonly Exportar.Captura captura;
        readonly bool rapido;
        readonly Queue<AsyncGPUReadbackRequest> pendientes = new Queue<AsyncGPUReadbackRequest>();
        readonly byte[] buffer;
        readonly byte[] fila;
        readonly int filas;
        int volteado = -1; // -1 = aún no se sabe; 1 = las filas llegan al revés

        public Envio(CodificadorVideo codificador, Exportar.Captura captura, Material materialNv12)
        {
            this.codificador = codificador;
            this.captura = captura;
            rapido = codificador.AceptaNv12 && captura.PrepararNv12(materialNv12);
            if (rapido)
            {
                buffer = new byte[captura.TamanoNv12];
                fila = new byte[captura.ancho];
                filas = captura.alto * 3 / 2;
            }
        }

        public bool Cuadro()
        {
            if (!rapido)
                return codificador.AgregarCuadro(captura.CapturarRgba());
            pendientes.Enqueue(captura.PedirNv12());
            return Vaciar(pendientes.Count > 3, false);
        }

        public bool Terminar()
        {
            return !rapido || Vaciar(true, true);
        }

        bool Vaciar(bool esperarUno, bool todo)
        {
            while (pendientes.Count > 0)
            {
                var pedido = pendientes.Peek();
                if (!pedido.done)
                {
                    if (!esperarUno && !todo)
                        return true;
                    pedido.WaitForCompletion();
                }
                pendientes.Dequeue();
                esperarUno = false;
                if (pedido.hasError)
                    return false;
                var datos = pedido.GetData<byte>();
                if (datos.Length < buffer.Length)
                    return false;
                NativeArray<byte>.Copy(datos, buffer, buffer.Length);
                Orientar();
                if (!codificador.AgregarCuadroNv12(buffer))
                    return false;
            }
            return true;
        }

        // El fondo es blanco: el brillo (Y, arriba en NV12) es alto y el color (UV, abajo) vale ~128.
        // Si llega al revés (depende del visor), se dan vuelta las filas.
        void Orientar()
        {
            if (volteado < 0)
            {
                int n = captura.ancho * 8;
                long inicio = 0, final = 0;
                for (int i = 0; i < n; i++)
                {
                    inicio += buffer[i];
                    final += buffer[buffer.Length - 1 - i];
                }
                volteado = inicio < final - 20L * n ? 1 : 0;
            }
            if (volteado == 0)
                return;
            int w = captura.ancho;
            for (int r = 0; r < filas / 2; r++)
            {
                int a = r * w, b = (filas - 1 - r) * w;
                System.Buffer.BlockCopy(buffer, a, fila, 0, w);
                System.Buffer.BlockCopy(buffer, b, buffer, a, w);
                System.Buffer.BlockCopy(fila, 0, buffer, b, w);
            }
        }
    }
}

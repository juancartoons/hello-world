using System.IO;
using UnityEngine;

// Convierte cuadros (RGBA) en un video MP4 (H.264) con audio opcional.
// En las gafas usa el codificador de Android (plugin TrazoCodificador.java).
// Si eso falla (o en el editor), guarda una secuencia de PNG + un "hacer_video.bat" para ffmpeg.
public class CodificadorVideo
{
    public string Ruta { get; private set; }
    public bool UsaPng { get; private set; }
    public string Error { get; private set; } = "";
    public int Cuadros { get; private set; }
    public string RutaPublica { get; private set; } = ""; // copia en Movies/TrazoVR (app Archivos del Quest)

    int ancho, alto, fps;
#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaObject java;
#endif
    Texture2D texPng;

    public bool Iniciar(string carpeta, string nombreBase, int ancho, int alto, int fps, AudioClip audio)
    {
        this.ancho = ancho;
        this.alto = alto;
        this.fps = Mathf.Max(1, fps);
        Cuadros = 0;
        Directory.CreateDirectory(carpeta);
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Ruta = Path.Combine(carpeta, nombreBase + ".mp4");
            java = new AndroidJavaObject("com.trazovr.TrazoCodificador");
            int bitrate = Mathf.Clamp(ancho * alto * this.fps / 8, 2000000, 12000000);
            if (java.Call<bool>("iniciar", Ruta, ancho, alto, this.fps, bitrate))
            {
                if (audio != null && !java.Call<bool>("agregarAudio", AudioUtil.Pcm16(audio), audio.frequency, Mathf.Clamp(audio.channels, 1, 2)))
                    Debug.LogWarning("TrazoVR: el video irá sin audio: " + java.Call<string>("obtenerError"));
                return true;
            }
            Error = java.Call<string>("obtenerError");
            Debug.LogWarning("TrazoVR: no se pudo iniciar el MP4 (" + Error + "); uso PNG.");
            java.Dispose();
            java = null;
        }
        catch (System.Exception e)
        {
            Error = e.Message;
            Debug.LogWarning("TrazoVR: no se pudo iniciar el MP4 (" + e.Message + "); uso PNG.");
            java = null;
        }
#endif
        return IniciarPng(carpeta, nombreBase, audio);
    }

    bool IniciarPng(string carpeta, string nombreBase, AudioClip audio)
    {
        UsaPng = true;
        Ruta = Path.Combine(carpeta, nombreBase);
        try
        {
            Directory.CreateDirectory(Ruta);
            bool conAudio = false;
            if (audio != null)
            {
                AudioUtil.GuardarWav(Path.Combine(Ruta, "audio.wav"), audio);
                conAudio = true;
            }
            string bat = "ffmpeg -y -framerate " + fps + " -i cuadro_%05d.png"
                         + (conAudio ? " -i audio.wav -c:a aac -shortest" : "")
                         + " -c:v libx264 -pix_fmt yuv420p video.mp4\r\npause\r\n";
            File.WriteAllText(Path.Combine(Ruta, "hacer_video.bat"), bat);
        }
        catch (System.Exception e)
        {
            Error = e.Message;
            return false;
        }
        texPng = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        return true;
    }

    // rgba: ancho * alto * 4 bytes, de abajo hacia arriba (como Texture2D).
    public bool AgregarCuadro(byte[] rgba)
    {
        if (rgba == null || rgba.Length < ancho * alto * 4)
            return false;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (java != null)
        {
            bool ok = java.Call<bool>("agregarCuadro", rgba);
            if (!ok)
                Error = java.Call<string>("obtenerError");
            else
                Cuadros++;
            return ok;
        }
#endif
        if (texPng == null)
            return false;
        try
        {
            texPng.LoadRawTextureData(rgba);
            texPng.Apply(false);
            File.WriteAllBytes(Path.Combine(Ruta, "cuadro_" + Cuadros.ToString("00000") + ".png"), texPng.EncodeToPNG());
            Cuadros++;
            return true;
        }
        catch (System.Exception e)
        {
            Error = e.Message;
            return false;
        }
    }

    public bool Terminar()
    {
        bool ok = true;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (java != null)
        {
            ok = java.Call<bool>("terminar");
            if (!ok)
                Error = java.Call<string>("obtenerError");
            java.Dispose();
            java = null;
        }
#endif
        if (texPng != null)
        {
            Object.Destroy(texPng);
            texPng = null;
        }
        if (ok && Cuadros > 0 && !UsaPng)
            RutaPublica = Galeria.Publicar(Ruta, "video/mp4", "Movies/TrazoVR");
        return ok && Cuadros > 0;
    }
}

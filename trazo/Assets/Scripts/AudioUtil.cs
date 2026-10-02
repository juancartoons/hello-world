using System.IO;
using UnityEngine;

// Ayudas para el audio: mezclar a mono, convertir a PCM de 16 bits y guardar WAV.
public static class AudioUtil
{
    // Todas las muestras en un solo canal (promedio de los canales).
    public static float[] Mono(AudioClip clip)
    {
        int canales = Mathf.Max(1, clip.channels);
        var datos = new float[clip.samples * canales];
        clip.GetData(datos, 0);
        if (canales == 1)
            return datos;
        var mono = new float[clip.samples];
        for (int i = 0; i < clip.samples; i++)
        {
            float suma = 0f;
            for (int c = 0; c < canales; c++)
                suma += datos[i * canales + c];
            mono[i] = suma / canales;
        }
        return mono;
    }

    // PCM de 16 bits, little-endian, con los canales intercalados (lo que usan WAV y el codificador AAC).
    public static byte[] Pcm16(AudioClip clip)
    {
        int canales = Mathf.Max(1, clip.channels);
        var datos = new float[clip.samples * canales];
        clip.GetData(datos, 0);
        var bytes = new byte[datos.Length * 2];
        for (int i = 0; i < datos.Length; i++)
        {
            int v = Mathf.RoundToInt(Mathf.Clamp(datos[i], -1f, 1f) * 32767f);
            bytes[i * 2] = (byte)(v & 0xff);
            bytes[i * 2 + 1] = (byte)((v >> 8) & 0xff);
        }
        return bytes;
    }

    public static void GuardarWav(string ruta, AudioClip clip)
    {
        byte[] pcm = Pcm16(clip);
        int canales = Mathf.Max(1, clip.channels);
        int frecuencia = clip.frequency;
        using (var archivo = new FileStream(ruta, FileMode.Create))
        using (var w = new BinaryWriter(archivo))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + pcm.Length);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            w.Write(16);
            w.Write((short)1);
            w.Write((short)canales);
            w.Write(frecuencia);
            w.Write(frecuencia * canales * 2);
            w.Write((short)(canales * 2));
            w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            w.Write(pcm.Length);
            w.Write(pcm);
        }
    }
}

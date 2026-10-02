using UnityEngine;

// Sonidos hechos por la app (sin archivos de audio).
public static class Sonidos
{
    static AudioClip burbuja;

    // "Plop" de una burbujita que revienta: un tono corto que sube rápido y se apaga, con un pequeño chasquido.
    public static AudioClip Burbuja
    {
        get
        {
            if (burbuja != null)
                return burbuja;
            const int frecuencia = 44100;
            int muestras = Mathf.RoundToInt(frecuencia * 0.11f);
            var datos = new float[muestras];
            float fase = 0f;
            var azar = new System.Random(7);
            for (int i = 0; i < muestras; i++)
            {
                float t = i / (float)frecuencia;
                float hz = Mathf.Lerp(450f, 1500f, Mathf.Clamp01(t / 0.05f));
                fase += 2f * Mathf.PI * hz / frecuencia;
                float sobre = Mathf.Exp(-t * 45f) * Mathf.Clamp01(t / 0.003f);
                float chasquido = t < 0.004f ? (float)(azar.NextDouble() * 2.0 - 1.0) * (1f - t / 0.004f) * 0.5f : 0f;
                datos[i] = (Mathf.Sin(fase) * sobre + chasquido) * 0.8f;
            }
            burbuja = AudioClip.Create("Burbuja", muestras, 1, frecuencia, false);
            burbuja.SetData(datos, 0);
            return burbuja;
        }
    }
}

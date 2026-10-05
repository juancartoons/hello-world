using UnityEngine;
using Random = UnityEngine.Random;

// Sonidos creados con matemáticas (no hay que descargar nada): música suave de tienda, ruido de la ciudad,
// pitos de carros, grillos de noche, campanita de la puerta, zumbido del robot, el "pío" y la celebración.
public static class SonidosProcedurales
{
    const int frecuencia = 22050;
    const float dosPi = Mathf.PI * 2f;

    static AudioClip Crear(string nombre, float[] datos)
    {
        var clip = AudioClip.Create(nombre, datos.Length, 1, frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }

    static float[] Muestras(float segundos) => new float[Mathf.CeilToInt(segundos * frecuencia)];

    // Suma una nota tipo "campana" (con armónicos que se apagan) en el tiempo "inicio".
    static void Campana(float[] d, float inicio, float f, float volumen, float apagado, bool repetir = false)
    {
        int i0 = Mathf.RoundToInt(inicio * frecuencia);
        int largo = Mathf.RoundToInt(apagado * 5f * frecuencia);
        for (int n = 0; n < largo; n++)
        {
            int i = i0 + n;
            if (i >= d.Length)
            {
                if (!repetir) break;
                i %= d.Length;
            }
            float t = n / (float)frecuencia;
            float ataque = Mathf.Clamp01(t / 0.004f);
            float onda = Mathf.Sin(dosPi * f * t) * Mathf.Exp(-t / apagado)
                         + 0.4f * Mathf.Sin(dosPi * f * 2.76f * t) * Mathf.Exp(-t / (apagado * 0.5f))
                         + 0.22f * Mathf.Sin(dosPi * f * 5.4f * t) * Mathf.Exp(-t / (apagado * 0.25f));
            d[i] += onda * ataque * volumen;
        }
    }

    static void Normalizar(float[] d, float pico)
    {
        float max = 0.0001f;
        foreach (float x in d)
            max = Mathf.Max(max, Mathf.Abs(x));
        float k = pico / max;
        for (int i = 0; i < d.Length; i++)
            d[i] *= k;
    }

    // "Ding-dong" de la puerta automática.
    public static AudioClip Campanita()
    {
        var d = Muestras(2.4f);
        Campana(d, 0f, 659.25f, 0.6f, 0.45f);
        Campana(d, 0.55f, 523.25f, 0.6f, 0.55f);
        Normalizar(d, 0.7f);
        return Crear("Campanita", d);
    }

    // El "pío" del pájaro: dos silbidos rápidos que suben.
    public static AudioClip Pio()
    {
        var d = Muestras(0.45f);
        foreach (float inicio in new[] { 0f, 0.2f })
        {
            float fase = 0f;
            int i0 = Mathf.RoundToInt(inicio * frecuencia), largo = Mathf.RoundToInt(0.12f * frecuencia);
            for (int n = 0; n < largo && i0 + n < d.Length; n++)
            {
                float u = n / (float)largo;
                float f = Mathf.Lerp(2800f, 4300f, Mathf.Sin(u * Mathf.PI * 0.6f));
                fase += dosPi * f / frecuencia;
                d[i0 + n] += Mathf.Sin(fase) * Mathf.Sin(u * Mathf.PI) * 0.5f;
            }
        }
        return Crear("Pio", d);
    }

    // Celebración al encontrarlo: arpegio alegre de campanitas.
    public static AudioClip Celebracion()
    {
        var d = Muestras(1.9f);
        float[] notas = { 523.25f, 659.25f, 783.99f, 1046.5f };
        for (int i = 0; i < notas.Length; i++)
            Campana(d, i * 0.12f, notas[i], 0.5f, 0.35f);
        Campana(d, 0.5f, 1318.5f, 0.35f, 0.4f);
        Normalizar(d, 0.7f);
        return Crear("Celebracion", d);
    }

    // Música de fondo suave (16 s que se repiten sin que se note el corte): acordes y un arpegio tranquilo.
    public static AudioClip Musica()
    {
        const float duracion = 16f;
        var d = Muestras(duracion);
        float[][] acordes =
        {
            new[] { 130.81f, 329.63f, 392.00f, 493.88f },   // Do maj7
            new[] { 110.00f, 261.63f, 329.63f, 392.00f },   // La m7
            new[] { 87.31f, 220.00f, 261.63f, 329.63f },    // Fa maj7
            new[] { 98.00f, 246.94f, 293.66f, 329.63f },    // Sol 6
        };
        for (int a = 0; a < acordes.Length; a++)
        {
            float inicio = a * 4f;
            // Colchón suave (con un poquito de coro) y bajo
            for (int nota = 0; nota < 4; nota++)
            {
                float f = acordes[a][nota];
                float vol = nota == 0 ? 0.1f : 0.05f;
                int i0 = Mathf.RoundToInt(inicio * frecuencia), largo = Mathf.RoundToInt(5.2f * frecuencia);
                for (int n = 0; n < largo; n++)
                {
                    float t = n / (float)frecuencia;
                    float env = Mathf.Clamp01(t / 0.6f) * (t > 4f ? Mathf.Clamp01(1f - (t - 4f) / 1.2f) : 1f);
                    float onda = Mathf.Sin(dosPi * f * t) + 0.5f * Mathf.Sin(dosPi * f * 1.003f * t) + 0.2f * Mathf.Sin(dosPi * f * 2f * t);
                    d[(i0 + n) % d.Length] += onda * env * vol;
                }
            }
            // Arpegio (una nota cada medio segundo)
            for (int paso = 0; paso < 8; paso++)
            {
                float f = acordes[a][1 + (paso % 3)] * (paso % 6 < 3 ? 2f : 1f);
                float t0 = inicio + paso * 0.5f;
                int i0 = Mathf.RoundToInt(t0 * frecuencia), largo = Mathf.RoundToInt(1.2f * frecuencia);
                for (int n = 0; n < largo; n++)
                {
                    float t = n / (float)frecuencia;
                    float env = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * 3.5f);
                    float onda = Mathf.Sin(dosPi * f * t) + 0.25f * Mathf.Sin(dosPi * f * 2f * t);
                    d[(i0 + n) % d.Length] += onda * env * 0.06f;
                }
            }
        }
        Normalizar(d, 0.6f);
        return Crear("Musica", d);
    }

    // Ruido de la ciudad (motores lejanos y carros que pasan), 10 s que se repiten.
    public static AudioClip Ciudad()
    {
        const float duracion = 10f, cruce = 1f;
        var d = Muestras(duracion + cruce);
        float marron = 0f, siseo = 0f;
        var pasos = new float[6];
        for (int p = 0; p < pasos.Length; p++)
            pasos[p] = Random.Range(0f, duracion);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)frecuencia;
            marron = (marron + Random.Range(-1f, 1f) * 0.02f) * 0.997f;
            siseo += (Random.Range(-1f, 1f) - siseo) * 0.12f;
            float paso = 0f;
            foreach (float tp in pasos)
            {
                float x = (t - tp) / 1.2f;
                paso += Mathf.Exp(-x * x);
            }
            d[i] = marron * 2.5f + siseo * 0.25f * paso + Mathf.Sin(dosPi * 55f * t) * 0.02f;
        }
        // Une el final con el principio para que el ciclo no se note
        int n = Mathf.RoundToInt(duracion * frecuencia), c = Mathf.RoundToInt(cruce * frecuencia);
        var final = new float[n];
        for (int i = 0; i < n; i++)
            final[i] = d[i];
        for (int i = 0; i < c; i++)
        {
            float u = i / (float)c;
            final[i] = d[i] * u + d[n + i] * (1f - u);
        }
        Normalizar(final, 0.5f);
        return Crear("Ciudad", final);
    }

    // Pito de carro: "pi-pi".
    public static AudioClip Pito()
    {
        var d = Muestras(0.5f);
        foreach (float inicio in new[] { 0f, 0.24f })
        {
            int i0 = Mathf.RoundToInt(inicio * frecuencia), largo = Mathf.RoundToInt(0.17f * frecuencia);
            for (int n = 0; n < largo && i0 + n < d.Length; n++)
            {
                float t = n / (float)frecuencia;
                float env = Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((largo - n) / (0.03f * frecuencia));
                float onda = (float)System.Math.Tanh(3f * Mathf.Sin(dosPi * 405f * t)) + 0.8f * (float)System.Math.Tanh(3f * Mathf.Sin(dosPi * 508f * t));
                d[i0 + n] += onda * env * 0.35f;
            }
        }
        return Crear("Pito", d);
    }

    // Grillos de noche, 4 s que se repiten.
    public static AudioClip Grillos()
    {
        const float duracion = 4f;
        var d = Muestras(duracion);
        foreach (float f in new[] { 4500f, 5100f })
        {
            for (float t0 = Random.Range(0f, 0.3f); t0 < duracion; t0 += Random.Range(0.45f, 0.75f))
                for (int pulso = 0; pulso < 3; pulso++)
                {
                    int i0 = Mathf.RoundToInt((t0 + pulso * 0.04f) * frecuencia), largo = Mathf.RoundToInt(0.025f * frecuencia);
                    for (int n = 0; n < largo; n++)
                    {
                        float u = n / (float)largo;
                        d[(i0 + n) % d.Length] += Mathf.Sin(dosPi * f * n / frecuencia) * Mathf.Sin(u * Mathf.PI) * 0.25f;
                    }
                }
        }
        return Crear("Grillos", d);
    }

    // Zumbido suave del robot aspiradora, 2 s que se repiten.
    public static AudioClip Zumbido()
    {
        const float duracion = 2f;
        var d = Muestras(duracion);
        float siseo = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)frecuencia;
            siseo += (Random.Range(-1f, 1f) - siseo) * 0.3f;
            d[i] = Mathf.Sin(dosPi * 120f * t) * 0.3f + Mathf.Sin(dosPi * 240f * t) * 0.15f + Mathf.Sin(dosPi * 360f * t) * 0.06f + siseo * 0.25f;
        }
        Normalizar(d, 0.5f);
        return Crear("Zumbido", d);
    }
}

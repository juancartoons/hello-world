using UnityEngine;

// Sonidos hechos por la app (sin archivos de audio): todo se calcula con matemáticas al empezar.
public static class Sonidos
{
    static AudioClip burbuja, magia, paso, ding, tada;
    const int Frecuencia = 44100;

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

    // Calcula los sonidos de una vez (al empezar), para que luego suenen sin esperar.
    public static void Preparar()
    {
        var lista = new[] { Magia, Paso, Ding, Tada };
        foreach (var c in lista)
            if (c == null)
                Debug.LogWarning("JCartoons: no se pudo crear un sonido");
    }

    // ---------- Música del título: arpa mágica + brillo "tecnológico" ----------
    // Las notas del arpa suben (Re mayor con novena), una por cada letra del título "JCartoons"
    // (empiezan en "inicioNotas" y van cada "pasoNotas" segundos, igual que las letras).
    public const float InicioNotas = 0.25f;
    public const float PasoNotas = 0.12f;

    public static AudioClip Magia
    {
        get
        {
            if (magia != null)
                return magia;
            var datos = new float[Mathf.RoundToInt(Frecuencia * 5.5f)];
            var azar = new System.Random(11);
            // Colchón suave de fondo (entra y sale despacio).
            Pad(datos, 0f, 5.2f, new[] { 146.83f, 220f, 293.66f }, 0.05f);
            // Un "whoosh" que sube (como cuando algo mágico se abre).
            Soplo(datos, 0.0f, 1.4f, 0.05f, azar);
            // El arpa: 9 notas, una por letra.
            float[] arpa = { 293.66f, 369.99f, 440f, 554.37f, 659.25f, 739.99f, 880f, 1108.73f, 1318.51f };
            for (int i = 0; i < arpa.Length; i++)
                Pulsar(datos, InicioNotas + i * PasoNotas, arpa[i], 0.28f, azar);
            // Final: campanitas brillantes (el toque "tecnológico") y un acorde de arpa.
            float final = InicioNotas + arpa.Length * PasoNotas + 0.05f;
            Campana(datos, final, 1760f, 0.16f, 2.6f, 3.5f);
            Campana(datos, final + 0.07f, 2637.02f, 0.10f, 2.2f, 3.5f);
            Campana(datos, final + 0.14f, 2217.46f, 0.08f, 2.0f, 3.5f);
            foreach (float hz in new[] { 293.66f, 440f, 554.37f, 739.99f })
                Pulsar(datos, final, hz, 0.16f, azar);
            Eco(datos, 0.32f);
            magia = Clip("Magia", datos, 0.85f);
            return magia;
        }
    }

    // Un pasito suave (para el muñeco que corre).
    public static AudioClip Paso
    {
        get
        {
            if (paso != null)
                return paso;
            var datos = new float[Mathf.RoundToInt(Frecuencia * 0.07f)];
            var azar = new System.Random(3);
            float bajo = 0f;
            for (int i = 0; i < datos.Length; i++)
            {
                float t = i / (float)Frecuencia;
                float ruido = (float)(azar.NextDouble() * 2.0 - 1.0);
                bajo += (ruido - bajo) * 0.18f;
                datos[i] = bajo * Mathf.Exp(-t * 75f) * 1.6f + Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 55f) * 0.5f;
            }
            paso = Clip("Paso", datos, 0.6f);
            return paso;
        }
    }

    // "¡Ding!" de un paso bien hecho (dos campanitas que suben).
    public static AudioClip Ding
    {
        get
        {
            if (ding != null)
                return ding;
            var datos = new float[Mathf.RoundToInt(Frecuencia * 1.1f)];
            Campana(datos, 0f, 1318.51f, 0.5f, 0.9f, 2f);
            Campana(datos, 0.09f, 1975.53f, 0.45f, 1f, 2f);
            Eco(datos, 0.18f);
            ding = Clip("Ding", datos, 0.8f);
            return ding;
        }
    }

    // "¡Ta-dá!" del final del tutorial (arpa rápida hacia arriba + campanas).
    public static AudioClip Tada
    {
        get
        {
            if (tada != null)
                return tada;
            var datos = new float[Mathf.RoundToInt(Frecuencia * 2.6f)];
            var azar = new System.Random(5);
            float[] notas = { 587.33f, 739.99f, 880f, 1174.66f, 1479.98f, 1760f };
            for (int i = 0; i < notas.Length; i++)
                Pulsar(datos, i * 0.06f, notas[i], 0.3f, azar);
            Campana(datos, 0.38f, 1760f, 0.3f, 1.6f, 3.5f);
            Campana(datos, 0.45f, 2349.32f, 0.2f, 1.4f, 3.5f);
            Eco(datos, 0.3f);
            tada = Clip("Tada", datos, 0.85f);
            return tada;
        }
    }

    // ---------- Instrumentos ----------

    // Cuerda pulsada (arpa) con el método Karplus-Strong: ruido suave que se repite y se apaga solo.
    static void Pulsar(float[] salida, float inicio, float hz, float volumen, System.Random azar)
    {
        int n = Mathf.Max(2, Mathf.RoundToInt(Frecuencia / hz));
        var cuerda = new float[n];
        float previo = 0f;
        for (int i = 0; i < n; i++)
        {
            float r = (float)(azar.NextDouble() * 2.0 - 1.0);
            previo = previo * 0.55f + r * 0.45f; // ruido suavecito = sonido más dulce
            cuerda[i] = previo;
        }
        int desde = Mathf.RoundToInt(inicio * Frecuencia);
        if (desde >= salida.Length)
            return;
        int largo = Mathf.Min(salida.Length - desde, Mathf.RoundToInt(Frecuencia * 3.2f));
        int k = 0;
        for (int i = 0; i < largo; i++)
        {
            int j = (k + 1) % n;
            float v = cuerda[k];
            cuerda[k] = (cuerda[k] + cuerda[j]) * 0.5f * 0.9996f;
            k = j;
            float ataque = Mathf.Clamp01(i / (Frecuencia * 0.003f));
            salida[desde + i] += v * volumen * ataque;
        }
    }

    // Campanita metálica (síntesis FM): brillo que se apaga despacio.
    static void Campana(float[] salida, float inicio, float hz, float volumen, float duracion, float razon)
    {
        int desde = Mathf.RoundToInt(inicio * Frecuencia);
        int largo = Mathf.Min(salida.Length - desde, Mathf.RoundToInt(Frecuencia * duracion));
        for (int i = 0; i < largo; i++)
        {
            float t = i / (float)Frecuencia;
            float indice = 1.6f * Mathf.Exp(-t * 4f);
            float vibrato = 1f + 0.003f * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
            float fase = 2f * Mathf.PI * hz * vibrato * t;
            float v = Mathf.Sin(fase + indice * Mathf.Sin(fase * razon));
            float sobre = Mathf.Exp(-t * 4.5f / duracion) * Mathf.Clamp01(t / 0.002f);
            salida[desde + i] += v * sobre * volumen;
        }
    }

    // Colchón de notas largas que entra y sale suave.
    static void Pad(float[] salida, float inicio, float duracion, float[] notas, float volumen)
    {
        int desde = Mathf.RoundToInt(inicio * Frecuencia);
        int largo = Mathf.Min(salida.Length - desde, Mathf.RoundToInt(Frecuencia * duracion));
        for (int i = 0; i < largo; i++)
        {
            float t = i / (float)Frecuencia;
            float sobre = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duracion));
            float v = 0f;
            foreach (float hz in notas)
                v += Mathf.Sin(2f * Mathf.PI * hz * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * hz * 2.003f * t);
            salida[desde + i] += v * sobre * volumen / notas.Length;
        }
    }

    // Ruido que sube de tono (un "fiuuu" suave).
    static void Soplo(float[] salida, float inicio, float duracion, float volumen, System.Random azar)
    {
        int desde = Mathf.RoundToInt(inicio * Frecuencia);
        int largo = Mathf.Min(salida.Length - desde, Mathf.RoundToInt(Frecuencia * duracion));
        float bajo = 0f;
        for (int i = 0; i < largo; i++)
        {
            float u = i / (float)largo;
            float r = (float)(azar.NextDouble() * 2.0 - 1.0);
            bajo += (r - bajo) * Mathf.Lerp(0.02f, 0.35f, u);
            salida[desde + i] += bajo * Mathf.Sin(Mathf.PI * u) * volumen * 3f;
        }
    }

    // Eco de sala (cuatro "peines" de retardo): da la sensación de un lugar amplio y mágico.
    static void Eco(float[] datos, float mezcla)
    {
        float[] tiempos = { 0.0297f, 0.0371f, 0.0411f, 0.0437f };
        var suma = new float[datos.Length];
        foreach (float tiempo in tiempos)
        {
            int d = Mathf.RoundToInt(tiempo * Frecuencia);
            var y = new float[datos.Length];
            for (int i = 0; i < datos.Length; i++)
            {
                y[i] = datos[i] + (i >= d ? y[i - d] * 0.78f : 0f);
                suma[i] += y[i];
            }
        }
        for (int i = 0; i < datos.Length; i++)
            datos[i] += suma[i] * mezcla / tiempos.Length;
    }

    // Normaliza (el punto más alto queda en "pico") y crea el AudioClip.
    static AudioClip Clip(string nombre, float[] datos, float pico)
    {
        float max = 1e-6f;
        foreach (float v in datos)
            max = Mathf.Max(max, Mathf.Abs(v));
        float g = pico / max;
        int fin = datos.Length;
        for (int i = 0; i < datos.Length; i++)
            datos[i] *= g;
        // Final suavecito (sin "clic").
        int cola = Mathf.Min(fin, Frecuencia / 50);
        for (int i = 0; i < cola; i++)
            datos[fin - 1 - i] *= i / (float)cola;
        var clip = AudioClip.Create(nombre, datos.Length, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }
}

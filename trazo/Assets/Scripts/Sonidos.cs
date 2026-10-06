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
        for (int i = 0; i < Arpa.Length; i++)
            Nota(i);
    }

    // ---------- Música del trazo: escala pentatónica (siempre suena bonita) ----------
    // 13 notas de grave a agudo (Do, Re, Mi, Sol, La en 2 octavas y media). Instrumentos: 0 arpa, 1 piano,
    // 2 marimba, 3 cajita de música, 4 rock (guitarra grunge), 5 punk, 6 drum and bass (percusión electrónica).
    // Los 3 últimos traen batería: suenan al compás (ver Ritmo).
    public const int NotasEscala = 13;
    public static readonly string[] Instrumentos = { "Arpa", "Piano", "Marimba", "Cajita", "Rock", "Punk", "Drum & Bass" };
    public const int PrimerConRitmo = 4;
    public static bool TieneRitmo(int instrumento) => instrumento >= PrimerConRitmo && instrumento < Instrumentos.Length;
    static readonly int[] Pentatonica = { 0, 2, 4, 7, 9 };
    static AudioClip[,] notasInstrumento;

    public static float FrecuenciaEscala(int indice)
    {
        indice = Mathf.Clamp(indice, 0, NotasEscala - 1);
        int semitonos = 12 * (indice / 5) + Pentatonica[indice % 5];
        return 261.63f * Mathf.Pow(2f, semitonos / 12f);
    }

    public static AudioClip NotaInstrumento(int instrumento, int indice)
    {
        if (notasInstrumento == null)
            notasInstrumento = new AudioClip[Instrumentos.Length, NotasEscala];
        instrumento = Mathf.Clamp(instrumento, 0, Instrumentos.Length - 1);
        indice = Mathf.Clamp(indice, 0, NotasEscala - 1);
        if (notasInstrumento[instrumento, indice] != null)
            return notasInstrumento[instrumento, indice];
        float hz = FrecuenciaEscala(indice);
        var datos = new float[Mathf.RoundToInt(Frecuencia * 1.3f)];
        switch (instrumento)
        {
            case 0: // arpa
                Pulsar(datos, 0f, hz, 0.5f, new System.Random(40 + indice));
                break;
            case 1: // piano suave: armónicos que se apagan (los agudos más rápido)
                for (int i = 0; i < datos.Length; i++)
                {
                    float t = i / (float)Frecuencia;
                    float v = 0f;
                    for (int k = 1; k <= 6; k++)
                        v += Mathf.Sin(2f * Mathf.PI * hz * k * (1f + 0.0004f * k * k) * t) * Mathf.Exp(-t * (1.6f + k * 1.1f)) / (k * k * 0.6f + 0.4f);
                    datos[i] = v * Mathf.Clamp01(t / 0.003f);
                }
                break;
            case 2: // marimba: madera (fundamental + parcial alto) y un golpecito
                for (int i = 0; i < datos.Length; i++)
                {
                    float t = i / (float)Frecuencia;
                    float v = Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t * 5f)
                              + 0.35f * Mathf.Sin(2f * Mathf.PI * hz * 3.93f * t) * Mathf.Exp(-t * 18f)
                              + 0.15f * Mathf.Sin(2f * Mathf.PI * hz * 9.2f * t) * Mathf.Exp(-t * 60f);
                    datos[i] = v * Mathf.Clamp01(t / 0.002f);
                }
                break;
            case 4: // rock grunge: acorde de quinta (power chord) grave, guitarras dobladas y muy distorsionadas
                PowerChord(datos, hz * 0.5f, 7f, 0.9997f, 1.2f, 0.22f, new System.Random(60 + indice));
                break;
            case 5: // punk: acorde de quinta corto y apagado con la palma (rasgueo rápido), más brillante
                PowerChord(datos, hz * 0.5f, 5f, 0.999f, 7f, 0.5f, new System.Random(80 + indice));
                break;
            case 6: // drum and bass: percusión electrónica afinada (tom de caja de ritmos + golpecito metálico)
                PercusionElectronica(datos, hz * 0.5f, new System.Random(70 + indice));
                break;
            case 3: // cajita de música: brillante, una octava arriba, con brillo metálico
                for (int i = 0; i < datos.Length; i++)
                {
                    float t = i / (float)Frecuencia;
                    float v = Mathf.Sin(2f * Mathf.PI * hz * 2f * t) * Mathf.Exp(-t * 3.2f)
                              + 0.3f * Mathf.Sin(2f * Mathf.PI * hz * 2f * 2.756f * t) * Mathf.Exp(-t * 7f)
                              + 0.12f * Mathf.Sin(2f * Mathf.PI * hz * 2f * 5.4f * t) * Mathf.Exp(-t * 14f);
                    datos[i] = v * Mathf.Clamp01(t / 0.0015f);
                }
                break;
        }
        notasInstrumento[instrumento, indice] = Clip("Nota" + instrumento + "_" + indice, datos, 0.6f);
        return notasInstrumento[instrumento, indice];
    }

    // Calcula todas las notas de un instrumento de una vez (al elegirlo), para que luego no se trabe.
    public static void PrepararInstrumento(int instrumento)
    {
        if (instrumento < 0 || instrumento >= Instrumentos.Length)
            return;
        for (int i = 0; i < NotasEscala; i++)
            NotaInstrumento(instrumento, i);
        if (TieneRitmo(instrumento))
            Ritmo(instrumento);
    }

    // Guitarra eléctrica: 2 cuerdas (nota y su quinta) + la octava, tocadas 2 veces un poquito desafinadas
    // (como 2 guitarras), pasadas por un "amplificador" saturado y un filtro que quita lo chillón.
    // ganancia = distorsión; apagado = qué tan rápido se apaga (palma sobre las cuerdas).
    static void PowerChord(float[] datos, float hz, float ganancia, float sostener, float apagado, float brillo, System.Random azar)
    {
        var limpio = new float[datos.Length];
        float[] razones = { 1f, 1.4983f, 2f };
        foreach (float desafino in new[] { 1f, 1.004f })
            foreach (float r in razones)
                Cuerda(limpio, hz * r * desafino, sostener, r == 2f ? 0.5f : 1f, azar);
        float bajo = 0f;
        for (int i = 0; i < datos.Length; i++)
        {
            float t = i / (float)Frecuencia;
            float v = (float)System.Math.Tanh(limpio[i] * ganancia);
            bajo += (v - bajo) * brillo; // filtro: menos brillo = más "grunge" (sucio y grave)
            datos[i] = bajo * Mathf.Exp(-t * apagado) * Mathf.Clamp01(t / 0.002f);
        }
    }

    // Una cuerda Karplus-Strong (como Pulsar, pero con el ruido más brillante: púa de guitarra).
    static void Cuerda(float[] salida, float hz, float sostener, float volumen, System.Random azar)
    {
        int n = Mathf.Max(2, Mathf.RoundToInt(Frecuencia / hz));
        var cuerda = new float[n];
        for (int i = 0; i < n; i++)
            cuerda[i] = (float)(azar.NextDouble() * 2.0 - 1.0);
        int k = 0;
        for (int i = 0; i < salida.Length; i++)
        {
            int j = (k + 1) % n;
            float v = cuerda[k];
            cuerda[k] = (cuerda[k] + cuerda[j]) * 0.5f * sostener;
            k = j;
            salida[i] += v * volumen;
        }
    }

    // Percusión electrónica afinada (drum and bass): un golpe cuyo tono cae rápido ("pew", como un tom
    // de caja de ritmos), un brillo metálico cortito y un clic de ruido al principio.
    static void PercusionElectronica(float[] datos, float hz, System.Random azar)
    {
        float fase = 0f, bajo = 0f;
        for (int i = 0; i < datos.Length; i++)
        {
            float t = i / (float)Frecuencia;
            float f = hz * (1f + 1.5f * Mathf.Exp(-t * 35f));
            fase += 2f * Mathf.PI * f / Frecuencia;
            float cuerpo = Mathf.Sin(fase) * Mathf.Exp(-t * 11f);
            float metal = Mathf.Sin(2f * Mathf.PI * hz * 3.5f * t + 2f * Mathf.Sin(2f * Mathf.PI * hz * 5.1f * t)) * Mathf.Exp(-t * 40f) * 0.25f;
            float r = (float)(azar.NextDouble() * 2.0 - 1.0);
            bajo += (r - bajo) * 0.4f;
            float clic = (r - bajo) * Mathf.Exp(-t * 250f) * 0.4f;
            datos[i] = (float)System.Math.Tanh((cuerpo + metal + clic) * 1.3f) * Mathf.Clamp01(t / 0.001f);
        }
    }

    // ---------- Batería (para Rock, Punk y Drum & Bass) ----------
    // Cada estilo tiene un compás de batería que se repite mientras dibujas. "Pasos" = en cuántas partes
    // se divide el compás: las notas de tu línea caen justo en esos pasos (así siempre suena a tiempo).
    static AudioClip[] ritmos;
    static AudioClip crash;

    public static float PulsosPorMinuto(int instrumento) => instrumento == 5 ? 180f : instrumento == 6 ? 174f : 120f;

    // Notas por compás: rock y punk en corcheas (8), drum and bass en semicorcheas (16), pero el bajo va cada 2.
    public static int PasosPorCompas(int instrumento) => instrumento == 6 ? 16 : 8;
    public static int PasosPorNota(int instrumento) => instrumento == 6 ? 2 : 1;

    public static AudioClip Ritmo(int instrumento)
    {
        if (!TieneRitmo(instrumento))
            return null;
        if (ritmos == null)
            ritmos = new AudioClip[Instrumentos.Length];
        if (ritmos[instrumento] != null)
            return ritmos[instrumento];
        float compas = 4f * 60f / PulsosPorMinuto(instrumento);
        int pasos = PasosPorCompas(instrumento);
        float paso = compas / pasos;
        int n = Mathf.RoundToInt(compas * Frecuencia);
        var largo = new float[n + Frecuencia / 2]; // con espacio para lo que suena después del último golpe
        var azar = new System.Random(90 + instrumento);
        // x = bombo, o = caja, h = platillo cerrado (hi-hat), H = abierto, g = caja suavecita (fantasma).
        string bombo, caja, hat;
        switch (instrumento)
        {
            case 4: // rock grunge (pesado, 120)
                bombo = "x.x..xx.";
                caja = "..o...o.";
                hat = "hhhhhhhH";
                break;
            case 5: // punk (rápido, 180)
                bombo = "x.x.x.x.";
                caja = "..o...oo";
                hat = "hhhhhhhh";
                break;
            default: // drum and bass (2-step, 174)
                bombo = "x.........x.....";
                caja = "....o..g....o.g.";
                hat = "h.h.h.hHh.h.h.hH";
                break;
        }
        for (int i = 0; i < pasos; i++)
        {
            float t = i * paso;
            if (bombo[i] == 'x') Bombo(largo, t, instrumento == 6 ? 0.9f : 1f);
            if (caja[i] == 'o') Caja(largo, t, 0.75f, azar);
            if (caja[i] == 'g') Caja(largo, t, 0.25f, azar);
            if (hat[i] == 'h') Platillo(largo, t, 0.06f, 0.2f, azar);
            if (hat[i] == 'H') Platillo(largo, t, 0.25f, 0.22f, azar);
        }
        // Lo que sobra al final se suma al principio: así el compás se repite sin cortes.
        var datos = new float[n];
        for (int i = 0; i < largo.Length; i++)
            datos[i % n] += largo[i];
        ritmos[instrumento] = Clip("Ritmo" + instrumento, datos, 0.8f, false);
        return ritmos[instrumento];
    }

    // ¡Crash! Platillo grande del final de la frase.
    public static AudioClip Crash
    {
        get
        {
            if (crash != null)
                return crash;
            var datos = new float[Mathf.RoundToInt(Frecuencia * 1.8f)];
            Platillo(datos, 0f, 1.6f, 1f, new System.Random(33));
            Bombo(datos, 0f, 0.8f);
            crash = Clip("Crash", datos, 0.75f);
            return crash;
        }
    }

    // Bombo: un tono que cae rápido (de 160 a 50 Hz) más un "clic" del golpe.
    static void Bombo(float[] salida, float inicio, float volumen)
    {
        int desde = Mathf.RoundToInt(inicio * Frecuencia);
        int largo = Mathf.Min(salida.Length - desde, Mathf.RoundToInt(Frecuencia * 0.35f));
        float fase = 0f;
        for (int i = 0; i < largo; i++)
        {
            float t = i / (float)Frecuencia;
            float hz = 50f + 110f * Mathf.Exp(-t * 30f);
            fase += 2f * Mathf.PI * hz / Frecuencia;
            float v = Mathf.Sin(fase) * Mathf.Exp(-t * 9f) + Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 300f) * 0.3f;
            salida[desde + i] += (float)System.Math.Tanh(v * 1.5f) * volumen;
        }
    }

    // Caja: un tono corto (el parche) más ruido (los alambres de abajo).
    static void Caja(float[] salida, float inicio, float volumen, System.Random azar)
    {
        int desde = Mathf.RoundToInt(inicio * Frecuencia);
        int largo = Mathf.Min(salida.Length - desde, Mathf.RoundToInt(Frecuencia * 0.25f));
        float bajo = 0f;
        for (int i = 0; i < largo; i++)
        {
            float t = i / (float)Frecuencia;
            float r = (float)(azar.NextDouble() * 2.0 - 1.0);
            bajo += (r - bajo) * 0.3f;
            float ruido = (r - bajo) * Mathf.Exp(-t * 16f);
            float tono = Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 25f);
            salida[desde + i] += (ruido * 0.9f + tono * 0.6f) * volumen;
        }
    }

    // Platillo: ruido agudo (sin graves). duracion corta = hi-hat cerrado; larga = abierto o crash.
    static void Platillo(float[] salida, float inicio, float duracion, float volumen, System.Random azar)
    {
        int desde = Mathf.RoundToInt(inicio * Frecuencia);
        int largo = Mathf.Min(salida.Length - desde, Mathf.RoundToInt(Frecuencia * duracion * 1.5f));
        float bajo = 0f;
        for (int i = 0; i < largo; i++)
        {
            float t = i / (float)Frecuencia;
            float r = (float)(azar.NextDouble() * 2.0 - 1.0);
            bajo += (r - bajo) * 0.45f;
            float metal = Mathf.Sin(2f * Mathf.PI * 5400f * t) * Mathf.Sin(2f * Mathf.PI * 7900f * t) * 0.3f;
            salida[desde + i] += (r - bajo + metal) * Mathf.Exp(-t * 4.5f / duracion) * volumen;
        }
    }

    // Las notas del arpa del título (Re mayor con novena, de grave a agudo).
    static readonly float[] Arpa = { 293.66f, 369.99f, 440f, 554.37f, 659.25f, 739.99f, 880f, 1108.73f, 1318.51f };
    static AudioClip[] notas;

    // Una nota del arpa (con un brillito de campana): el tutorial toca la melodía del título con tus OK.
    public static AudioClip Nota(int i)
    {
        if (notas == null)
            notas = new AudioClip[Arpa.Length];
        i = ((i % Arpa.Length) + Arpa.Length) % Arpa.Length;
        if (notas[i] != null)
            return notas[i];
        var datos = new float[Mathf.RoundToInt(Frecuencia * 1.6f)];
        Pulsar(datos, 0f, Arpa[i], 0.5f, new System.Random(20 + i));
        Campana(datos, 0.01f, Arpa[i] * 2f, 0.07f, 1f, 3.5f);
        Eco(datos, 0.25f);
        notas[i] = Clip("Nota" + i, datos, 0.7f);
        return notas[i];
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
            float[] arpa = Arpa;
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
    static AudioClip Clip(string nombre, float[] datos, float pico, bool colaSuave = true)
    {
        float max = 1e-6f;
        foreach (float v in datos)
            max = Mathf.Max(max, Mathf.Abs(v));
        float g = pico / max;
        int fin = datos.Length;
        for (int i = 0; i < datos.Length; i++)
            datos[i] *= g;
        // Final suavecito (sin "clic"). Los ritmos que se repiten no lo llevan (el compás debe quedar entero).
        int cola = colaSuave ? Mathf.Min(fin, Frecuencia / 50) : 0;
        for (int i = 0; i < cola; i++)
            datos[fin - 1 - i] *= i / (float)cola;
        var clip = AudioClip.Create(nombre, datos.Length, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }
}

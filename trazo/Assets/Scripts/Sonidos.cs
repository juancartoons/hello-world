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
        for (int i = 0; i < NotasTitulo.Length; i++)
            Nota(i);
    }

    // ---------- Música del trazo: escala pentatónica (siempre suena bonita) ----------
    // 13 notas de grave a agudo (Do, Re, Mi, Sol, La en 2 octavas y media).
    // Los instrumentos van en el orden del selector (el carrusel sobre el parlante):
    // 0 Piano, 1 Arpa, 2 Orquesta, 3 Cajita, 4 Japan, 5 Post-punk, 6 Drum & Bass, 7 Dubstep, 8 Marimba.
    // Post-punk, Drum & Bass y Dubstep traen batería: suenan al compás (ver Ritmo).
    // (Son las mismas fórmulas que trazo/Herramientas/simulador-sonidos.html: si cambias una, cambia la otra.)
    public const int NotasEscala = 13;
    public const int Piano = 0, Arpa = 1, Orquesta = 2, Cajita = 3, Japan = 4, PostPunk = 5, DrumAndBass = 6, Dubstep = 7, Marimba = 8;
    public static readonly string[] Instrumentos = { "Piano", "Arpa", "Orquesta", "Cajita", "Japan", "Post-punk", "Drum & Bass", "Dubstep", "Marimba" };
    public static bool TieneRitmo(int instrumento) => instrumento >= PostPunk && instrumento <= Dubstep;
    static readonly int[] Pentatonica = { 0, 2, 4, 7, 9 };
    static AudioClip[,] notasInstrumento;

    // Volumen de las notas de cada instrumento (veces el normal) y de su batería.
    public static float VolumenNota(int instrumento)
    {
        switch (instrumento)
        {
            case Orquesta: return 0.7f;
            case Japan: return 1.3f;
            case PostPunk: return 0.6f;
            case DrumAndBass: return 0.45f;
            case Dubstep: return 0.65f;
            default: return 1f;
        }
    }

    public static float VolumenRitmo(int instrumento) => instrumento == DrumAndBass ? 0.45f : instrumento == Dubstep ? 0.4f : 0.3f;

    // Cuántos metros hay que subir la mano para la siguiente nota (el Dubstep cambia más fácil: 2 cm).
    public static float AlturaNota(int instrumento) => instrumento == Dubstep ? 0.02f : 0.03f;

    public static float FrecuenciaEscala(int indice)
    {
        indice = Mathf.Clamp(indice, 0, NotasEscala - 1);
        int semitonos = 12 * (indice / 5) + Pentatonica[indice % 5];
        return 261.63f * Mathf.Pow(2f, semitonos / 12f);
    }

    // Las notas se calculan EN SEGUNDO PLANO (otro hilo del procesador), así la imagen nunca se traba
    // aunque el piano o la orquesta tarden en hacerse. Mientras una nota no está lista, no suena (null).
    static readonly object cerrojo = new object();
    static float[,][] datosListos;     // notas ya calculadas que esperan volverse AudioClip
    static bool[,] pedidas;

    public static AudioClip NotaInstrumento(int instrumento, int indice)
    {
        if (notasInstrumento == null)
            notasInstrumento = new AudioClip[Instrumentos.Length, NotasEscala];
        instrumento = Mathf.Clamp(instrumento, 0, Instrumentos.Length - 1);
        indice = Mathf.Clamp(indice, 0, NotasEscala - 1);
        if (notasInstrumento[instrumento, indice] != null)
            return notasInstrumento[instrumento, indice];
        float[] datos;
        lock (cerrojo)
        {
            datos = datosListos != null ? datosListos[instrumento, indice] : null;
            if (datos != null)
                datosListos[instrumento, indice] = null;
        }
        if (datos == null)
        {
            Pedir(instrumento, indice);
            return null;
        }
        var clip = AudioClip.Create("Nota" + instrumento + "_" + indice, datos.Length, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        notasInstrumento[instrumento, indice] = clip;
        return clip;
    }

    static void Pedir(int instrumento, int indice)
    {
        lock (cerrojo)
        {
            if (pedidas == null)
            {
                pedidas = new bool[Instrumentos.Length, NotasEscala];
                datosListos = new float[Instrumentos.Length, NotasEscala][];
            }
            if (pedidas[instrumento, indice])
                return;
            pedidas[instrumento, indice] = true;
        }
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            float[] datos = null;
            try
            {
                datos = CalcularNota(instrumento, indice);
            }
            catch (System.Exception)
            {
                datos = null;
            }
            lock (cerrojo)
            {
                if (datos != null)
                    datosListos[instrumento, indice] = datos;
                else
                    pedidas[instrumento, indice] = false; // falló: se puede pedir otra vez
            }
        });
    }

    // Calcula una nota (en el hilo de segundo plano: solo matemáticas, nada de Unity).
    static float[] CalcularNota(int instrumento, int indice)
    {
        float hz = FrecuenciaEscala(indice);
        float segundos = instrumento == Orquesta ? 2.2f : instrumento == Piano ? 2.8f : 1.3f;
        var datos = new float[Mathf.RoundToInt(Frecuencia * segundos)];
        switch (instrumento)
        {
            case Piano:
                PianoMagico(datos, hz, new System.Random(140 + indice));
                break;
            case Arpa:
                Pulsar(datos, 0f, hz, 0.5f, new System.Random(40 + indice));
                break;
            case Orquesta:
                OrquestaConCoro(datos, hz, new System.Random(100 + indice));
                break;
            case Cajita: // cajita de música: brillante, una octava arriba, con brillo metálico
                for (int i = 0; i < datos.Length; i++)
                {
                    float t = i / (float)Frecuencia;
                    float v = Mathf.Sin(2f * Mathf.PI * hz * 2f * t) * Mathf.Exp(-t * 3.2f)
                              + 0.3f * Mathf.Sin(2f * Mathf.PI * hz * 2f * 2.756f * t) * Mathf.Exp(-t * 7f)
                              + 0.12f * Mathf.Sin(2f * Mathf.PI * hz * 2f * 5.4f * t) * Mathf.Exp(-t * 14f);
                    datos[i] = v * Mathf.Clamp01(t / 0.0015f);
                }
                break;
            case Japan:
                GuitarraJapan(datos, hz * 0.5f, new System.Random(60 + indice));
                break;
            case PostPunk:
                FuzzMoto(datos, hz * 0.25f, new System.Random(80 + indice));
                break;
            case DrumAndBass:
                Pzhht(datos, hz, new System.Random(70 + indice));
                break;
            case Dubstep: // del muy grave (38 Hz) al muy agudo (1760 Hz): cada nota sube más que la anterior
                Grunido(datos, hz * 0.25f * Mathf.Pow(2f, (indice - 3) * 0.25f), new System.Random(120 + indice));
                break;
            case Marimba: // madera (fundamental + parcial alto) y un golpecito
                for (int i = 0; i < datos.Length; i++)
                {
                    float t = i / (float)Frecuencia;
                    float v = Mathf.Sin(2f * Mathf.PI * hz * t) * Mathf.Exp(-t * 5f)
                              + 0.35f * Mathf.Sin(2f * Mathf.PI * hz * 3.93f * t) * Mathf.Exp(-t * 18f)
                              + 0.15f * Mathf.Sin(2f * Mathf.PI * hz * 9.2f * t) * Mathf.Exp(-t * 60f);
                    datos[i] = v * Mathf.Clamp01(t / 0.002f);
                }
                break;
        }
        Normalizar(datos, 0.6f, true);
        return datos;
    }

    // Empieza a calcular (en segundo plano) todas las notas de un instrumento, para que estén listas al usarlo.
    public static void PrepararInstrumento(int instrumento)
    {
        if (instrumento < 0 || instrumento >= Instrumentos.Length)
            return;
        for (int i = 0; i < NotasEscala; i++)
            NotaInstrumento(instrumento, i);
        if (TieneRitmo(instrumento))
            Ritmo(instrumento);
    }

    static float Tanh(float x) => (float)System.Math.Tanh(x);
    static float Azar(System.Random azar) => (float)(azar.NextDouble() * 2.0 - 1.0);

    // Una cuerda Karplus-Strong (como Pulsar, pero con el ruido más brillante: púa de guitarra).
    static void Cuerda(float[] salida, float hz, float sostener, float volumen, System.Random azar)
    {
        int n = Mathf.Max(2, Mathf.RoundToInt(Frecuencia / hz));
        var cuerda = new float[n];
        for (int i = 0; i < n; i++)
            cuerda[i] = Azar(azar);
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

    // Piano "mágico" estilo películas de Ghibli: martillos de fieltro suaves (pocos agudos), 3 cuerdas
    // por nota un poquito desafinadas (el piano "respira"), sonido que primero baja rápido y luego se queda
    // mucho tiempo (como con el pedal), un brillito de celesta muy suave encima y una sala de conciertos.
    static void PianoMagico(float[] datos, float hz, System.Random azar)
    {
        // Rápido: cada armónico es un "punto que gira" (un paso de giro por muestra, sin calcular senos),
        // y cada caída se multiplica por un número fijo en cada muestra (sin calcular exponenciales).
        float[] cuerdas = { 0.9996f, 1f, 1.0005f };
        const int armonicos = 8;
        int osc = armonicos * cuerdas.Length;
        var re = new double[osc];
        var im = new double[osc];
        var cs = new double[osc];
        var sn = new double[osc];
        var amp = new double[armonicos];
        var e1 = new double[armonicos];
        var e2 = new double[armonicos];
        var d1 = new double[armonicos];
        var d2 = new double[armonicos];
        for (int k = 1; k <= armonicos; k++)
        {
            double fk = hz * k * System.Math.Sqrt(1.0 + 0.0004 * k * k);
            amp[k - 1] = System.Math.Exp(-k * 0.35) / System.Math.Pow(k, 0.8);
            double caida = 0.8 + k * 0.5;
            e1[k - 1] = 0.55;
            e2[k - 1] = 0.45;
            d1[k - 1] = System.Math.Exp(-caida * 2.5 / Frecuencia);
            d2[k - 1] = System.Math.Exp(-caida * 0.3 / Frecuencia);
            for (int c = 0; c < cuerdas.Length; c++)
            {
                int j = (k - 1) * cuerdas.Length + c;
                double w = 2.0 * System.Math.PI * fk * cuerdas[c] / Frecuencia;
                re[j] = 1.0;
                im[j] = 0.0;
                cs[j] = System.Math.Cos(w);
                sn[j] = System.Math.Sin(w);
            }
        }
        float golpe = 0f;
        for (int i = 0; i < datos.Length; i++)
        {
            float t = i / (float)Frecuencia;
            double v = 0.0;
            for (int k = 0; k < armonicos; k++)
            {
                double env = (e1[k] + e2[k]) * amp[k];
                e1[k] *= d1[k];
                e2[k] *= d2[k];
                for (int c = 0; c < cuerdas.Length; c++)
                {
                    int j = k * cuerdas.Length + c;
                    v += im[j] * env;
                    double r2 = re[j] * cs[j] - im[j] * sn[j];
                    im[j] = re[j] * sn[j] + im[j] * cs[j];
                    re[j] = r2;
                }
            }
            float r = Azar(azar);
            golpe += (r - golpe) * 0.08f;
            float brillo = Mathf.Sin(2f * Mathf.PI * hz * 4f * t) * Mathf.Exp(-t * 1.5f) * 0.05f;
            datos[i] = ((float)(v / 3.0) + golpe * Mathf.Exp(-t * 60f) * 0.3f + brillo) * Mathf.Clamp01(t / 0.002f);
        }
        Sala(datos, 0.4f);
    }

    // Japan: guitarra eléctrica casi limpia, brillante y con un poquito de "crunch": la nota y su octava,
    // dos guitarras apenas desafinadas, rasgueo corto.
    static void GuitarraJapan(float[] datos, float hz, System.Random azar)
    {
        var limpio = new float[datos.Length];
        foreach (float desafino in new[] { 1f, 1.003f })
        {
            Cuerda(limpio, hz * desafino, 0.9993f, 1f, azar);
            Cuerda(limpio, hz * 2f * desafino, 0.9993f, 0.45f, azar);
        }
        float bajo = 0f, muyBajo = 0f;
        for (int i = 0; i < datos.Length; i++)
        {
            float t = i / (float)Frecuencia;
            float v = Tanh(limpio[i] * 0.3f * 3f);
            bajo += (v - bajo) * 0.55f;
            muyBajo += (bajo - muyBajo) * 0.04f;
            float brillo = bajo + 0.3f * (bajo - muyBajo); // un poco más de agudos ("presencia")
            datos[i] = brillo * Mathf.Exp(-t * 3.5f) * Mathf.Clamp01(t / 0.0015f);
        }
    }

    // Post-punk "moto" desgarrador (wrang wrang): acorde de quinta grave con ondas ásperas que entra un poco
    // desafinado y sube (como acelerar), un motor que tartamudea, dos etapas de fuzz (la segunda "rasga"
    // y suma la octava), un toque de sonido "roto" y un parlante de guitarra.
    static void FuzzMoto(float[] datos, float hz, System.Random azar)
    {
        var fases = new float[6];
        float[] razones = { 1f, 1.4983f, 2f };
        float[] desafinos = { 1f, 1.007f };
        float ruidoLento = 0f, pre = 0f, c1 = 0f, c2 = 0f, hp = 0f, retenido = 0f;
        float sesgo = Tanh(0.3f);
        for (int i = 0; i < datos.Length; i++)
        {
            float t = i / (float)Frecuencia;
            float ruido = Azar(azar);
            ruidoLento += (ruido - ruidoLento) * 0.006f;
            float acelera = 1f - 0.06f * Mathf.Exp(-t * 9f);
            float x = 0f;
            int k = 0;
            foreach (float d in desafinos)
                foreach (float r in razones)
                {
                    fases[k] = (fases[k] + hz * r * d * acelera * (1f + 0.006f * ruidoLento) / Frecuencia) % 1f;
                    x += (fases[k] * 2f - 1f) * (r == 2f ? 0.6f : 1f);
                    k++;
                }
            x += ruido * Mathf.Exp(-t * 30f);                                        // golpe de púa
            x *= (1f + 0.35f * Mathf.Sin(2f * Mathf.PI * 11f * t)) * (1f + 8f * ruidoLento); // motor que tartamudea
            pre += (x - pre) * 0.05f;
            float xh = x - pre * 0.7f;
            float y = Tanh(xh * 20f + 0.3f) - sesgo;                                 // fuzz 1
            y = 0.55f * y + 0.45f * (Mathf.Abs(y) * 2f - 1f);                        // fuzz 2: rasgado
            y = Mathf.Clamp(y * 2f, -0.7f, 0.7f);
            if (i % 3 == 0)
                retenido = y;                                                        // un poco "roto"
            y = 0.75f * y + 0.25f * retenido;
            c1 += (y - c1) * 0.3f;
            c2 += (c1 - c2) * 0.3f;
            hp += (c2 - hp) * 0.008f;
            datos[i] = (c2 - hp) * Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t * 0.6f);
        }
    }

    // Drum & Bass "pzhht": un clic ("p"), ruido que zumba afinado con tu nota ("zh") y se corta seco ("t").
    static void Pzhht(float[] datos, float hz, System.Random azar)
    {
        int n = datos.Length;
        var ruido = new float[n];
        float fase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Frecuencia;
            float r = Azar(azar);
            fase = (fase + hz * 0.5f / Frecuencia) % 1f;
            float zumbido = fase < 0.5f ? 1f : -1f;
            float env = Mathf.Clamp01(t / 0.001f) * Mathf.Exp(-t * 30f) * (t < 0.15f ? 1f : Mathf.Max(0f, 1f - (t - 0.15f) / 0.02f));
            ruido[i] = r * (0.6f + 0.4f * zumbido) * env;
        }
        float centro = Mathf.Min(6000f, hz * 4f);
        PasaBanda(ruido, datos, centro, centro * 0.8f, 1f);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Frecuencia;
            float clic = Mathf.Sin(2f * Mathf.PI * 2000f * t) * Mathf.Exp(-t * 400f) * 0.5f;
            datos[i] = Tanh((datos[i] * 2.5f + clic) * 1.5f);
        }
    }

    // Dubstep "grrthh": bajo de FM (una onda que hace temblar a otra) cuyo "gruñido" cambia a cada
    // semicorchea y vibra 23 veces por segundo, con mucha distorsión (saturación + "doblado" de la onda)
    // y una reverberación SECA: rebotes cortísimos sin cola de eco (como un cuarto pequeño de metal).
    static void Grunido(float[] datos, float f0, System.Random azar)
    {
        float dieciseisavo = 140f / 60f * 4f;
        float[] patron = { 1f, 0.3f, 0.8f, 0.5f };
        int n = datos.Length;
        var seco = new float[n];
        float fc = 0f, fm = 0f, fs = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Frecuencia;
            float indice = 2f + 4f * patron[Mathf.FloorToInt(t * dieciseisavo) % 4] + 1.5f * Mathf.Sin(2f * Mathf.PI * 23f * t);
            fc = (fc + f0 / Frecuencia) % 1f;
            fm = (fm + f0 * 1.5f / Frecuencia) % 1f;
            float x = Mathf.Sin(2f * Mathf.PI * fc + indice * Mathf.Sin(2f * Mathf.PI * fm)) + (fc * 2f - 1f) * 0.5f + Azar(azar) * 0.03f;
            x = Tanh(x * 4f);
            x = Mathf.Sin(x * 2.5f); // doblado de la onda (más armónicos ásperos)
            fs = (fs + f0 * 0.5f / Frecuencia) % 1f;
            float env = Mathf.Clamp01(t / 0.004f) * (t < 0.42f ? 1f : Mathf.Max(0f, 1f - (t - 0.42f) / 0.04f));
            seco[i] = (x * 0.8f + Mathf.Sin(2f * Mathf.PI * fs) * 0.4f) * env;
        }
        // Reverberación seca: 4 rebotes cortísimos (1 a 3 ms) que se apagan enseguida.
        var y = new float[n];
        foreach (float tiempo in new[] { 0.0011f, 0.0017f, 0.0023f, 0.0031f })
        {
            int d = Mathf.RoundToInt(tiempo * Frecuencia);
            for (int i = 0; i < n; i++)
            {
                y[i] = seco[i] + (i >= d ? y[i - d] * 0.5f : 0f);
                datos[i] += y[i] * 0.25f;
            }
        }
        for (int i = 0; i < n; i++)
            datos[i] = Tanh(datos[i] * 1.5f + seco[i] * 0.5f);
    }

    // Filtro "pasa banda" (deja pasar solo una zona de tonos): forma las vocales del coro.
    static void PasaBanda(float[] entrada, float[] salida, float f, float ancho, float ganancia)
    {
        double w = 2.0 * System.Math.PI * f / Frecuencia, q = f / ancho, alfa = System.Math.Sin(w) / (2.0 * q);
        double a0 = 1.0 + alfa, b0 = alfa / a0, b2 = -alfa / a0, a1 = -2.0 * System.Math.Cos(w) / a0, a2 = (1.0 - alfa) / a0;
        double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
        for (int i = 0; i < entrada.Length; i++)
        {
            double x = entrada[i];
            double y = b0 * x + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x; y2 = y1; y1 = y;
            salida[i] += (float)(y * ganancia);
        }
    }

    // ---------- Coro (síntesis de formantes: la "forma" de cada vocal) ----------
    // Frecuencias, volúmenes y anchos de las 5 resonancias de cada vocal.
    static readonly float[][] VocalA = { new[] { 800f, 1150f, 2900f, 3900f, 4950f }, new[] { 1f, 0.5f, 0.025f, 0.1f, 0.003f }, new[] { 80f, 90f, 120f, 130f, 140f } };
    static readonly float[][] VocalO = { new[] { 450f, 800f, 2830f, 3800f, 4950f }, new[] { 1f, 0.28f, 0.08f, 0.1f, 0.003f }, new[] { 70f, 80f, 100f, 130f, 135f } };
    static readonly float[][] VocalAHombre = { new[] { 600f, 1040f, 2250f, 2450f, 2750f }, new[] { 1f, 0.45f, 0.35f, 0.35f, 0.1f }, new[] { 60f, 70f, 110f, 120f, 130f } };

    // Una cuerda vocal: pulsos suaves con vibrato que aparece poco a poco, pequeñas imperfecciones
    // (como una persona real) y aire de la respiración.
    static void CuerdaVocal(float[] salida, float hz, System.Random azar, float ataque, float fin, float vibrato)
    {
        float ritmoVib = 4.6f + (float)azar.NextDouble() * 1.2f;
        float faseVib = (float)azar.NextDouble() * 2f * Mathf.PI;
        float fase = (float)azar.NextDouble(), g1 = 0f, g2 = 0f, imperfecto = 0f, aire = 0f;
        for (int i = 0; i < salida.Length; i++)
        {
            float t = i / (float)Frecuencia;
            float r = Azar(azar);
            imperfecto += (r - imperfecto) * 0.0008f;
            float vib = vibrato * Mathf.Clamp01((t - 0.15f) / 0.4f) * Mathf.Sin(2f * Mathf.PI * ritmoVib * t + faseVib);
            float f = hz * (1f + vib + 0.15f * imperfecto);
            fase = (fase + f / Frecuencia) % 1f;
            g1 += ((fase * 2f - 1f) - g1) * 0.2f;
            g2 += (g1 - g2) * 0.2f;
            aire += (r - aire) * 0.5f;
            float env = Mathf.Clamp01(t / ataque) * Mathf.Clamp01((fin - t) / 0.2f);
            salida[i] += (g2 + aire * 0.06f) * env;
        }
    }

    static void Formantes(float[] fuente, float[] salida, float[][] vocal, float volumen)
    {
        for (int k = 0; k < 5; k++)
            PasaBanda(fuente, salida, vocal[0][k], vocal[2][k], vocal[1][k] * volumen);
    }

    static void Voces(float[] buf, float f, int cuantas, float ataque, float fin, System.Random azar)
    {
        for (int v = 0; v < cuantas; v++)
            CuerdaVocal(buf, f * (1f + ((float)azar.NextDouble() - 0.5f) * 0.01f), azar, ataque, fin, 0.006f);
    }

    // Coro grande: 5 sopranos ("Aaa"), 4 altos ("Ooo"), 4 tenores y 3 bajos (con la "A" de hombre),
    // todos entrando despacio (sin golpe, para que no suene a piano).
    static void Coro(float[] salida, float hz, System.Random azar, float fin)
    {
        int n = salida.Length;
        var sopranos = new float[n];
        var altos = new float[n];
        var tenores = new float[n];
        var bajos = new float[n];
        Voces(sopranos, hz, 5, 0.3f, fin, azar);
        Voces(altos, hz, 4, 0.35f, fin, azar);
        Voces(tenores, hz * 0.5f, 4, 0.35f, fin, azar);
        Voces(bajos, hz * 0.25f, 3, 0.4f, fin, azar);
        Formantes(sopranos, salida, VocalA, 1f);
        Formantes(altos, salida, VocalO, 0.6f);
        Formantes(tenores, salida, VocalAHombre, 0.8f);
        Formantes(bajos, salida, VocalAHombre, 0.6f);
    }

    // Orquesta sinfónica con coro. Cada sección por separado y luego juntas en una sala de conciertos:
    // violines (6), chelos (3, una octava abajo), cornos (2, una octava abajo; el sonido se abre al soplar),
    // flauta (dos octavas arriba, con aire) y el coro grande, que es lo que más se oye.
    // Todo entra despacio (sin golpe), para que no suene a piano.
    static void OrquestaConCoro(float[] datos, float hz, System.Random azar)
    {
        int n = datos.Length;
        float fin = n / (float)Frecuencia;
        var violines = new float[n];
        var chelos = new float[n];
        var cornos = new float[n];
        var flauta = new float[n];
        var voces = new float[n];
        Seccion(violines, 6, hz, 0.25f, 0.25f, 0.006f, fin, azar);
        Seccion(chelos, 3, hz * 0.5f, 0.3f, 0.12f, 0.005f, fin, azar);
        // Cornos: el filtro se abre al soplar y luego se calma.
        for (int v = 0; v < 2; v++)
        {
            float fase = (float)azar.NextDouble(), a1 = 0f, a2 = 0f;
            float det = v == 0 ? 0.997f : 1.003f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Frecuencia;
                fase = (fase + hz * 0.5f * det / Frecuencia) % 1f;
                float brillo = 0.03f + 0.12f * Mathf.Clamp01(t / 0.05f) * Mathf.Exp(-t * 2f);
                a1 += ((fase * 2f - 1f) - a1) * brillo;
                a2 += (a1 - a2) * brillo;
                cornos[i] += a2 * Mathf.Clamp01(t / 0.2f) * Mathf.Clamp01((fin - t) / 0.3f);
            }
        }
        // Flauta: tono puro con vibrato y aire.
        float fF = 0f, aire = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Frecuencia;
            fF = (fF + hz * 2f * (1f + 0.008f * Mathf.Clamp01((t - 0.2f) / 0.3f) * Mathf.Sin(2f * Mathf.PI * 5.2f * t)) / Frecuencia) % 1f;
            float r = Azar(azar);
            aire += (r - aire) * 0.1f;
            flauta[i] = (Mathf.Sin(2f * Mathf.PI * fF) + aire * 0.15f) * Mathf.Clamp01(t / 0.08f) * Mathf.Clamp01((fin - t) / 0.3f);
        }
        Coro(voces, hz, azar, fin);
        SumarSeccion(datos, voces, 0.6f); // el coro manda
        SumarSeccion(datos, violines, 0.18f);
        SumarSeccion(datos, chelos, 0.12f);
        SumarSeccion(datos, cornos, 0.12f);
        SumarSeccion(datos, flauta, 0.06f);
        Sala(datos, 0.45f);
    }

    // Una sección de cuerdas: varios instrumentos, cada uno con su afinación, su vibrato y el roce del arco.
    static void Seccion(float[] salida, int cuantos, float f, float ataque, float brillo, float vibrato, float fin, System.Random azar)
    {
        int n = salida.Length;
        for (int v = 0; v < cuantos; v++)
        {
            float det = 1f + ((float)azar.NextDouble() - 0.5f) * 0.008f;
            float rv = 4.8f + (float)azar.NextDouble() * 1.2f;
            float pv = (float)azar.NextDouble() * 2f * Mathf.PI;
            float fase = (float)azar.NextDouble(), a1 = 0f, a2 = 0f, roce = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Frecuencia;
                float vib = vibrato * Mathf.Clamp01((t - 0.2f) / 0.4f) * Mathf.Sin(2f * Mathf.PI * rv * t + pv);
                fase = (fase + f * det * (1f + vib) / Frecuencia) % 1f;
                float r = Azar(azar);
                roce += (r - roce) * 0.3f;
                float x = fase * 2f - 1f + roce * 0.08f;
                a1 += (x - a1) * brillo;
                a2 += (a1 - a2) * brillo;
                salida[i] += a2 * Mathf.Clamp01(t / ataque) * Mathf.Clamp01((fin - t) / 0.3f);
            }
        }
    }

    static float Pico(float[] a)
    {
        float m = 1e-6f;
        foreach (float v in a)
            m = Mathf.Max(m, Mathf.Abs(v));
        return m;
    }

    static void SumarSeccion(float[] salida, float[] seccion, float nivel)
    {
        float g = nivel / Pico(seccion);
        for (int i = 0; i < salida.Length; i++)
            salida[i] += seccion[i] * g;
    }

    // Sala de conciertos (reverberación tipo "Freeverb": 6 ecos con los agudos apagándose + 4 difusores).
    static void Sala(float[] datos, float mezcla)
    {
        int n = datos.Length;
        var cola = new float[n];
        foreach (int d in new[] { 1116, 1188, 1277, 1356, 1422, 1491 })
        {
            var buf = new float[d];
            int k = 0;
            float filtro = 0f;
            for (int i = 0; i < n; i++)
            {
                float y = buf[k];
                filtro = y * 0.7f + filtro * 0.3f;
                buf[k] = datos[i] * 0.015f + filtro * 0.84f;
                k = (k + 1) % d;
                cola[i] += y;
            }
        }
        foreach (int d in new[] { 556, 441, 341, 225 })
        {
            var buf = new float[d];
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                float b = buf[k];
                float o = -cola[i] + b;
                buf[k] = cola[i] + b * 0.5f;
                k = (k + 1) % d;
                cola[i] = o;
            }
        }
        float g = mezcla * Pico(datos) / Pico(cola);
        for (int i = 0; i < n; i++)
            datos[i] += cola[i] * g;
    }

    // ---------- Batería (para Post-punk, Drum & Bass y Dubstep) ----------
    // Cada estilo tiene un compás de batería que se repite mientras dibujas. "Pasos" = en cuántas partes
    // se divide el compás: las notas de tu línea caen justo en esos pasos (así siempre suena a tiempo).
    static AudioClip[] ritmos;
    static AudioClip crash;

    public static float PulsosPorMinuto(int instrumento) => instrumento == PostPunk ? 180f : instrumento == DrumAndBass ? 174f : 140f;

    // Pasos por compás: post-punk en corcheas (8); drum and bass y dubstep en semicorcheas (16).
    // Las notas: post-punk en cada paso, drum and bass cada 2 y dubstep cada 4 (una por pulso).
    public static int PasosPorCompas(int instrumento) => instrumento == PostPunk ? 8 : 16;
    public static int PasosPorNota(int instrumento) => instrumento == DrumAndBass ? 2 : instrumento == Dubstep ? 4 : 1;
    static int SemillaRitmo(int instrumento) => instrumento == PostPunk ? 95 : instrumento == DrumAndBass ? 96 : 98;

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
        var azar = new System.Random(SemillaRitmo(instrumento));
        // x = bombo, o = caja, O = caja fuerte, g = caja suavecita (fantasma), h = platillo cerrado, H = abierto.
        string bombo, caja, hat;
        switch (instrumento)
        {
            case PostPunk: // rápido, 180
                bombo = "x.x.x.x.";
                caja = "..o...oo";
                hat = "hhhhhhhh";
                break;
            case DrumAndBass: // 2-step, 174
                bombo = "x.........x.....";
                caja = "....o..g....o.g.";
                hat = "h.h.h.hHh.h.h.hH";
                break;
            default: // dubstep, medio tiempo, 140
                bombo = "x.....x...x.....";
                caja = "........O.......";
                hat = "h.h.h.h.h.h.h.hH";
                break;
        }
        for (int i = 0; i < pasos; i++)
        {
            float t = i * paso;
            if (bombo[i] == 'x') Bombo(largo, t, instrumento == DrumAndBass ? 0.9f : 1f);
            if (caja[i] == 'O') Caja(largo, t, 1f, azar);
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
    static readonly float[] NotasTitulo = { 293.66f, 369.99f, 440f, 554.37f, 659.25f, 739.99f, 880f, 1108.73f, 1318.51f };
    static AudioClip[] notas;

    // Una nota del arpa (con un brillito de campana): el tutorial toca la melodía del título con tus OK.
    public static AudioClip Nota(int i)
    {
        if (notas == null)
            notas = new AudioClip[NotasTitulo.Length];
        i = ((i % NotasTitulo.Length) + NotasTitulo.Length) % NotasTitulo.Length;
        if (notas[i] != null)
            return notas[i];
        var datos = new float[Mathf.RoundToInt(Frecuencia * 1.6f)];
        Pulsar(datos, 0f, NotasTitulo[i], 0.5f, new System.Random(20 + i));
        Campana(datos, 0.01f, NotasTitulo[i] * 2f, 0.07f, 1f, 3.5f);
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
            float[] arpa = NotasTitulo;
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
        Normalizar(datos, pico, colaSuave);
        var clip = AudioClip.Create(nombre, datos.Length, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }

    static void Normalizar(float[] datos, float pico, bool colaSuave)
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
    }
}

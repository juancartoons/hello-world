using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// Bocas automáticas (lipsync).
// 1. Dibuja una boca en una capa propia (por ejemplo, Capa 2) y elige esa capa.
// 2. Modo "Guardar": mueve los nodos de la boca y toca Reposo, A, E, I, O, U o M para guardar cada forma.
// 3. Graba tu voz (botón Voz) o elige un audio (botón Audio: archivos en Dibujos/Audio).
// 4. Toca "Lipsync": se crean las claves de la boca en la línea de tiempo según el audio.
// En modo "Probar", tocar una boca la pone en el fotograma actual (y se guarda como clave).
public class Lipsync : MonoBehaviour
{
    public static readonly string[] Nombres = { "Reposo", "A", "E", "I", "O", "U", "M" };
    const int Reposo = 0, A = 1, E = 2, I = 3, O = 4, U = 5, M = 6;

    // Si falta una boca, se usa la primera que exista de su lista.
    static readonly int[][] Alternativas =
    {
        new[] { Reposo, M },
        new[] { A, O, E, Reposo },
        new[] { E, I, A, Reposo },
        new[] { I, E, A, Reposo },
        new[] { O, U, A, Reposo },
        new[] { U, O, Reposo },
        new[] { M, Reposo },
    };

    // Formantes aproximados de las vocales en español (F1, F2 en Hz).
    static readonly float[,] Formantes =
    {
        { 750f, 1300f }, // A
        { 470f, 1900f }, // E
        { 300f, 2300f }, // I
        { 480f, 900f },  // O
        { 320f, 750f },  // U
    };
    static readonly int[] VocalDeFormante = { A, E, I, O, U };

    public Dibujo dibujo;
    public Animacion animacion;
    public AudioSource fuente;
    [Tooltip("true = tocar una boca guarda la forma; false = la pone en el fotograma actual")]
    public bool modoGuardar = true;
    [Tooltip("Por debajo de este volumen (0 a 1) la boca descansa")]
    public float umbralSilencio = 0.12f;

    public readonly PoseBoca[] poses = new PoseBoca[7];
    public string ArchivoAudio { get; private set; } = "";
    public AudioClip Clip => fuente != null ? fuente.clip : null;
    public bool Grabando { get; private set; }
    public float TiempoGrabando => Grabando ? Time.time - inicioGrabacion : 0f;
    public event System.Action alCambiar;

    int indiceAudio = -1;
    AudioClip clipMicrofono;
    float inicioGrabacion;
    int frecuenciaMicrofono = 44100;

    string Carpeta => Path.Combine(Application.persistentDataPath, "Dibujos", "Audio");

    void Awake()
    {
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (animacion == null && dibujo != null) animacion = dibujo.animacion;
        if (fuente == null) fuente = GetComponent<AudioSource>();
        if (animacion != null && fuente != null)
            animacion.fuenteAudio = fuente;
    }

    void Avisar()
    {
        alCambiar?.Invoke();
    }

    void Mensaje(string texto)
    {
        if (dibujo != null)
            dibujo.Mensaje(texto);
    }

    // ---------- Muestra lista para probar ----------
    // Una cara con TODAS las bocas ya guardadas (Reposo, A, E, I, O, U, M), en la Capa 3.
    // Aparece sola al abrir la página Bocas si aún no hay bocas. Solo falta: Voz → hablar → Voz → Lipsync → Play.

    // Media apertura (ancho, alto) en metros y cuánto suben las comisuras (sonrisa) para cada boca.
    static readonly Vector3[] FormaMuestra =
    {
        new Vector3(0.034f, 0.004f, 0.000f),  // Reposo
        new Vector3(0.032f, 0.034f, 0.000f),  // A
        new Vector3(0.040f, 0.016f, 0.004f),  // E
        new Vector3(0.044f, 0.008f, 0.006f),  // I
        new Vector3(0.021f, 0.027f, 0.000f),  // O
        new Vector3(0.012f, 0.015f, 0.000f),  // U
        new Vector3(0.033f, 0.0015f, -0.001f), // M
    };

    public bool HayBocas
    {
        get
        {
            for (int i = 0; i < poses.Length; i++)
                if (TieneBoca(i))
                    return true;
            return false;
        }
    }

    public void CrearMuestra(Transform cabeza)
    {
        if (dibujo == null || cabeza == null)
            return;
        Vector3 frente = cabeza.forward;
        frente.y = 0f;
        frente = frente.sqrMagnitude > 1e-4f ? frente.normalized : Vector3.forward;
        Vector3 derecha = Vector3.Cross(Vector3.up, frente).normalized;
        Vector3 centro = cabeza.position + frente * 0.5f - Vector3.up * 0.05f;

        dibujo.GuardarParaDeshacer();
        dibujo.SeleccionarCapa(2);
        // Cara, ojos y boca.
        var cara = Linea(Ovalo(centro, derecha, 0.11f, 0.14f, 12, 0f), true, 0, 1f);
        Linea(Ovalo(centro + derecha * -0.04f + Vector3.up * 0.035f, derecha, 0.009f, 0.013f, 8, 0f), true, 7, 0.7f);
        Linea(Ovalo(centro + derecha * 0.04f + Vector3.up * 0.035f, derecha, 0.009f, 0.013f, 8, 0f), true, 7, 0.7f);
        Vector3 centroBoca = centro - Vector3.up * 0.06f;
        var boca = Linea(Boca(centroBoca, derecha, 0), true, 7, 0.8f);
        if (cara == null || boca == null)
            return;
        // Guarda las 7 bocas (misma línea, mismos nodos, distinta forma: así el morph sale suave).
        var baseDatos = boca.CrearDatos();
        for (int i = 0; i < poses.Length; i++)
        {
            var d = JsonUtility.FromJson<DatosTrazo>(JsonUtility.ToJson(baseDatos));
            d.nodos.Clear();
            foreach (var w in Boca(centroBoca, derecha, i))
                d.nodos.Add(w);
            d.asaEntrada.Clear();
            d.asaSalida.Clear();
            d.asaManual.Clear();
            poses[i] = new PoseBoca { nombre = Nombres[i] };
            poses[i].trazos.Add(d);
        }
        PonerBoca(0);
        modoGuardar = false;
        Avisar();
        Mensaje("Muestra de bocas lista (Capa 3). Toca Voz, habla, Voz otra vez, luego Lipsync y Play");
    }

    Trazo Linea(List<Vector3> local, bool cerrada, int color, float grosor)
    {
        var d = new DatosTrazo
        {
            ancho = dibujo.AnchoNuevoLocal() * grosor,
            cerrado = cerrada,
            relleno = cerrada,
            colorRelleno = color,
        };
        d.nodos.AddRange(local);
        return dibujo.AgregarTrazo(d);
    }

    List<Vector3> Ovalo(Vector3 centro, Vector3 derecha, float ancho, float alto, int n, float sonrisa)
    {
        var l = new List<Vector3>(n);
        for (int k = 0; k < n; k++)
        {
            float a = k * Mathf.PI * 2f / n;
            float x = Mathf.Cos(a) * ancho * 0.5f;
            float y = Mathf.Sin(a) * alto * 0.5f;
            y += sonrisa * Mathf.Abs(Mathf.Cos(a)); // las comisuras suben
            Vector3 mundo = centro + derecha * x + Vector3.up * y;
            l.Add(dibujo.ProyectarEnPlano(dibujo.transform.InverseTransformPoint(mundo)));
        }
        return l;
    }

    // La boca i: 8 nodos (siempre los mismos), el labio de arriba un poco más plano.
    List<Vector3> Boca(Vector3 centro, Vector3 derecha, int i)
    {
        var f = FormaMuestra[Mathf.Clamp(i, 0, FormaMuestra.Length - 1)];
        var l = new List<Vector3>(8);
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI * 2f / 8f;
            float x = Mathf.Cos(a) * f.x;
            float y = Mathf.Sin(a) * f.y * (Mathf.Sin(a) > 0f ? 0.7f : 1f);
            y += f.z * Mathf.Abs(Mathf.Cos(a));
            Vector3 mundo = centro + derecha * x + Vector3.up * y;
            l.Add(dibujo.ProyectarEnPlano(dibujo.transform.InverseTransformPoint(mundo)));
        }
        return l;
    }

    // ---------- Biblioteca de bocas ----------

    public bool TieneBoca(int i)
    {
        return i >= 0 && i < poses.Length && poses[i] != null && poses[i].trazos.Count > 0;
    }

    public void AlternarModo()
    {
        modoGuardar = !modoGuardar;
        Avisar();
        Mensaje(modoGuardar ? "Tocar una boca = guardar su forma" : "Tocar una boca = ponerla en este fotograma");
    }

    public void TocarBoca(int i)
    {
        if (dibujo == null || i < 0 || i >= poses.Length)
            return;
        if (modoGuardar)
        {
            var pose = new PoseBoca { nombre = Nombres[i] };
            foreach (var t in dibujo.trazos)
                if (Dibujo.Editable(t) && t.capa == dibujo.capaActual && t.nodos.Count >= 2 && !t.Dibujando)
                    pose.trazos.Add(t.CrearDatos());
            if (pose.trazos.Count == 0)
            {
                Mensaje("Dibuja la boca en la capa " + (dibujo.capaActual + 1) + " primero");
                return;
            }
            poses[i] = pose;
            Avisar();
            Mensaje("Boca " + Nombres[i] + " guardada (" + pose.trazos.Count + " líneas)");
            return;
        }
        int real = Elegir(i);
        if (real < 0)
        {
            Mensaje("Aún no hay bocas guardadas");
            return;
        }
        dibujo.GuardarParaDeshacer();
        PonerBoca(real);
        Mensaje("Boca " + Nombres[real]);
    }

    // La boca que se usará para "i" (o una parecida si esa no existe). -1 si no hay ninguna.
    int Elegir(int i)
    {
        foreach (int k in Alternativas[i])
            if (TieneBoca(k))
                return k;
        for (int k = 0; k < poses.Length; k++)
            if (TieneBoca(k))
                return k;
        return -1;
    }

    // Pone la forma de la boca en las líneas (con los mismos ids).
    void PonerBoca(int i)
    {
        if (!TieneBoca(i))
            return;
        foreach (var d in poses[i].trazos)
        {
            if (d == null)
                continue;
            foreach (var t in dibujo.trazos)
            {
                if (t != null && t.id == d.id && t.visibleAnim)
                {
                    t.AplicarPose(d, null, 0f);
                    break;
                }
            }
        }
    }

    public List<PoseBoca> CopiarPoses()
    {
        var lista = new List<PoseBoca>();
        foreach (var p in poses)
            if (p != null)
                lista.Add(p);
        return lista;
    }

    public void Restaurar(List<PoseBoca> lista, string audio, bool cargarAudio)
    {
        for (int i = 0; i < poses.Length; i++)
            poses[i] = null;
        if (lista != null)
        {
            foreach (var p in lista)
            {
                if (p == null || p.trazos == null)
                    continue;
                int i = System.Array.IndexOf(Nombres, p.nombre);
                if (i >= 0)
                    poses[i] = p;
            }
        }
        if (cargarAudio)
        {
            if (string.IsNullOrEmpty(audio))
                QuitarAudioSinAviso();
            else if (audio != ArchivoAudio)
                StartCoroutine(CargarAudio(Path.Combine(Carpeta, audio), false));
        }
        Avisar();
    }

    // ---------- Audio ----------

    public void AlternarVoz()
    {
        if (Grabando)
        {
            TerminarVoz();
            return;
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            Mensaje("Acepta el permiso del micrófono y vuelve a tocar Voz");
            return;
        }
#endif
        if (Microphone.devices.Length == 0)
        {
            Mensaje("No encontré micrófono");
            return;
        }
        int minimo = 0;
        int maximo = 0;
        Microphone.GetDeviceCaps(null, out minimo, out maximo);
        frecuenciaMicrofono = maximo > 0 ? Mathf.Clamp(44100, minimo, maximo) : 44100;
        if (animacion != null)
            animacion.Pausar();
        clipMicrofono = Microphone.Start(null, false, 180, frecuenciaMicrofono);
        if (clipMicrofono == null)
        {
            Mensaje("No se pudo usar el micrófono");
            return;
        }
        Grabando = true;
        inicioGrabacion = Time.time;
        Avisar();
        Mensaje("Grabando voz... toca Voz otra vez para parar");
    }

    void TerminarVoz()
    {
        int muestras = Microphone.GetPosition(null);
        Microphone.End(null);
        Grabando = false;
        if (clipMicrofono == null)
            return;
        if (muestras <= 0)
            muestras = Mathf.Min(clipMicrofono.samples, Mathf.RoundToInt((Time.time - inicioGrabacion) * clipMicrofono.frequency));
        if (muestras < clipMicrofono.frequency / 10)
        {
            Mensaje("Grabación muy corta");
            Avisar();
            return;
        }
        int canales = Mathf.Max(1, clipMicrofono.channels);
        var datos = new float[muestras * canales];
        clipMicrofono.GetData(datos, 0);
        var clip = AudioClip.Create("Voz", muestras, canales, clipMicrofono.frequency, false);
        clip.SetData(datos, 0);
        Destroy(clipMicrofono);
        clipMicrofono = null;

        string nombre = "voz_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
        try
        {
            Directory.CreateDirectory(Carpeta);
            AudioUtil.GuardarWav(Path.Combine(Carpeta, nombre), clip);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo guardar la voz: " + e.Message);
            nombre = "";
        }
        PonerClip(clip, nombre);
        Mensaje("Voz grabada (" + clip.length.ToString("0.0") + " s). Toca Lipsync");
    }

    // Pasa al siguiente audio de la carpeta Dibujos/Audio (wav, mp3 u ogg).
    public void SiguienteAudio()
    {
        if (Grabando)
            return;
        var archivos = new List<string>();
        try
        {
            Directory.CreateDirectory(Carpeta);
            foreach (var f in Directory.GetFiles(Carpeta))
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                if (ext == ".wav" || ext == ".mp3" || ext == ".ogg")
                    archivos.Add(f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo leer la carpeta de audio: " + e.Message);
        }
        if (archivos.Count == 0)
        {
            Mensaje("Pon audios (wav, mp3) en Dibujos/Audio");
            return;
        }
        archivos.Sort();
        indiceAudio = (indiceAudio + 1) % archivos.Count;
        StartCoroutine(CargarAudio(archivos[indiceAudio], true));
    }

    IEnumerator CargarAudio(string ruta, bool avisar)
    {
        if (!File.Exists(ruta))
        {
            if (avisar)
                Mensaje("No encontré el audio");
            yield break;
        }
        string ext = Path.GetExtension(ruta).ToLowerInvariant();
        AudioType tipo = ext == ".mp3" ? AudioType.MPEG : ext == ".ogg" ? AudioType.OGGVORBIS : AudioType.WAV;
        string url = new System.Uri(ruta).AbsoluteUri;
        using (var pedido = UnityWebRequestMultimedia.GetAudioClip(url, tipo))
        {
            var descarga = pedido.downloadHandler as DownloadHandlerAudioClip;
            if (descarga != null)
            {
                descarga.streamAudio = false;
                descarga.compressed = false;
            }
            yield return pedido.SendWebRequest();
            if (pedido.result != UnityWebRequest.Result.Success)
            {
                Mensaje("No se pudo abrir el audio");
                yield break;
            }
            var clip = DownloadHandlerAudioClip.GetContent(pedido);
            if (clip == null || clip.samples == 0)
            {
                Mensaje("No se pudo abrir el audio");
                yield break;
            }
            clip.name = Path.GetFileNameWithoutExtension(ruta);
            PonerClip(clip, Path.GetFileName(ruta));
            if (avisar)
                Mensaje("Audio: " + clip.name + " (" + clip.length.ToString("0.0") + " s)");
        }
    }

    void PonerClip(AudioClip clip, string archivo)
    {
        if (fuente != null)
        {
            fuente.Stop();
            fuente.clip = clip;
        }
        ArchivoAudio = archivo ?? "";
        Avisar();
    }

    public void QuitarAudio()
    {
        if (Grabando)
            return;
        QuitarAudioSinAviso();
        Mensaje("Sin audio");
    }

    void QuitarAudioSinAviso()
    {
        if (fuente != null)
        {
            fuente.Stop();
            fuente.clip = null;
        }
        ArchivoAudio = "";
        Avisar();
    }

    // ---------- Lipsync automático ----------

    public void Generar()
    {
        if (dibujo == null || animacion == null)
            return;
        var clip = Clip;
        if (clip == null)
        {
            Mensaje("Primero graba tu voz o elige un audio");
            return;
        }
        if (Elegir(Reposo) < 0)
        {
            Mensaje("Primero guarda las bocas (modo Guardar)");
            return;
        }
        float fps = Mathf.Max(1f, animacion.fotogramasPorSegundo);
        int[] visemas = Analizar(clip, fps);
        if (visemas.Length == 0)
        {
            Mensaje("El audio está vacío");
            return;
        }

        dibujo.GuardarParaDeshacer();
        int volver = animacion.Fotograma;
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        int claves = 0;
        int anterior = -1;
        int ultimoFotograma = -1;
        for (int f = 0; f < visemas.Length; f++)
        {
            int v = Elegir(visemas[f]);
            if (v == anterior)
                continue;
            // La boca anterior se sostiene hasta justo antes del cambio (así no se "derrite" lentamente).
            if (anterior >= 0 && f - 1 > ultimoFotograma)
            {
                Clavar(f - 1, anterior);
                claves++;
            }
            Clavar(f, v);
            claves++;
            ultimoFotograma = f;
            anterior = v;
        }
        // Al final la boca descansa.
        int fin = Mathf.Min(visemas.Length, Animacion.TotalFotogramas - 1);
        int reposo = Elegir(Reposo);
        if (anterior != reposo && fin > ultimoFotograma)
        {
            Clavar(fin, reposo);
            claves++;
        }
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
        animacion.IrA(volver);
        animacion.MostrarFotograma();
        Mensaje("Lipsync listo: " + claves + " claves. Toca Play");
    }

    void Clavar(int f, int boca)
    {
        animacion.IrA(f);
        animacion.MostrarFotograma();
        PonerBoca(boca);
        animacion.GuardarClaveEn(f);
    }

    // Una boca por fotograma, según el volumen y las frecuencias de la voz.
    int[] Analizar(AudioClip clip, float fps)
    {
        float[] muestras = AudioUtil.Mono(clip);
        int frecuencia = clip.frequency;
        int total = Mathf.Min(Animacion.TotalFotogramas - 1, Mathf.CeilToInt(clip.length * fps));
        if (total <= 0 || muestras.Length == 0)
            return new int[0];

        const int N = 1024;
        var re = new float[N];
        var im = new float[N];
        var volumen = new float[total];
        var f1 = new float[total];
        var f2 = new float[total];
        float maximo = 1e-6f;
        int mitadVentana = Mathf.Max(N / 2, Mathf.RoundToInt(frecuencia / fps / 2f));

        for (int f = 0; f < total; f++)
        {
            int centro = Mathf.RoundToInt((f + 0.5f) / fps * frecuencia);
            // Volumen (RMS) de todo el fotograma.
            double suma = 0;
            int cuenta = 0;
            for (int k = centro - mitadVentana; k < centro + mitadVentana; k++)
            {
                if (k < 0 || k >= muestras.Length)
                    continue;
                suma += muestras[k] * muestras[k];
                cuenta++;
            }
            volumen[f] = cuenta > 0 ? Mathf.Sqrt((float)(suma / cuenta)) : 0f;
            maximo = Mathf.Max(maximo, volumen[f]);

            // Espectro (FFT de 1024 muestras con ventana de Hann).
            for (int k = 0; k < N; k++)
            {
                int idx = centro - N / 2 + k;
                float s = idx >= 0 && idx < muestras.Length ? muestras[idx] : 0f;
                re[k] = s * (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * k / (N - 1)));
                im[k] = 0f;
            }
            Fft(re, im);
            float hz = frecuencia / (float)N;
            f1[f] = Pico(re, im, hz, 250f, 950f);
            f2[f] = Pico(re, im, hz, Mathf.Max(f1[f] + 250f, 650f), 2900f);
        }

        // Volumen suavizado y normalizado.
        var resultado = new int[total];
        float suave = 0f;
        for (int f = 0; f < total; f++)
        {
            float v = volumen[f] / maximo;
            suave = v > suave ? v : Mathf.Lerp(suave, v, 0.6f);
            if (suave < umbralSilencio)
            {
                resultado[f] = Reposo;
                continue;
            }
            resultado[f] = Vocal(f1[f], f2[f]);
            if (suave < umbralSilencio * 2f && (resultado[f] == U || resultado[f] == O))
                resultado[f] = M; // sonidos suaves y graves: boca casi cerrada
        }

        // Pausas muy cortas en medio de la voz = labios cerrados (m, p, b).
        for (int f = 1; f < total - 1; f++)
        {
            if (resultado[f] != Reposo)
                continue;
            int fin = f;
            while (fin < total && resultado[fin] == Reposo)
                fin++;
            if (fin < total && fin - f <= 2 && resultado[f - 1] != Reposo)
                for (int k = f; k < fin; k++)
                    resultado[k] = M;
            f = fin;
        }

        // Quita cambios de un solo fotograma (se ven como parpadeos).
        for (int f = 1; f < total - 1; f++)
            if (resultado[f] != resultado[f - 1] && resultado[f - 1] == resultado[f + 1])
                resultado[f] = resultado[f - 1];
        return resultado;
    }

    // La vocal más parecida según los dos primeros formantes.
    static int Vocal(float f1, float f2)
    {
        int mejor = 0;
        float distancia = float.MaxValue;
        for (int i = 0; i < 5; i++)
        {
            float d1 = Mathf.Log(f1 / Formantes[i, 0]);
            float d2 = Mathf.Log(f2 / Formantes[i, 1]);
            float d = d1 * d1 * 1.5f + d2 * d2;
            if (d < distancia)
            {
                distancia = d;
                mejor = i;
            }
        }
        return VocalDeFormante[mejor];
    }

    // Frecuencia con más energía (suavizada) entre desde y hasta.
    static float Pico(float[] re, float[] im, float hz, float desde, float hasta)
    {
        int a = Mathf.Max(1, Mathf.FloorToInt(desde / hz));
        int b = Mathf.Min(re.Length / 2 - 2, Mathf.CeilToInt(hasta / hz));
        float mejor = -1f;
        int indice = a;
        for (int k = a; k <= b; k++)
        {
            float e = Magnitud(re, im, k - 1) + 2f * Magnitud(re, im, k) + Magnitud(re, im, k + 1);
            if (e > mejor)
            {
                mejor = e;
                indice = k;
            }
        }
        return Mathf.Max(hz, indice * hz);
    }

    static float Magnitud(float[] re, float[] im, int k)
    {
        return re[k] * re[k] + im[k] * im[k];
    }

    // FFT clásica (Cooley-Tukey), en el mismo arreglo. El tamaño debe ser potencia de 2.
    static void Fft(float[] re, float[] im)
    {
        int n = re.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;
            if (i < j)
            {
                float tr = re[i]; re[i] = re[j]; re[j] = tr;
                float ti = im[i]; im[i] = im[j]; im[j] = ti;
            }
        }
        for (int largo = 2; largo <= n; largo <<= 1)
        {
            float angulo = -2f * Mathf.PI / largo;
            float wr = Mathf.Cos(angulo);
            float wi = Mathf.Sin(angulo);
            for (int i = 0; i < n; i += largo)
            {
                float cr = 1f, ci = 0f;
                for (int k = 0; k < largo / 2; k++)
                {
                    int p = i + k;
                    int q = p + largo / 2;
                    float xr = re[q] * cr - im[q] * ci;
                    float xi = re[q] * ci + im[q] * cr;
                    re[q] = re[p] - xr;
                    im[q] = im[p] - xi;
                    re[p] += xr;
                    im[p] += xi;
                    float nr = cr * wr - ci * wi;
                    ci = cr * wi + ci * wr;
                    cr = nr;
                }
            }
        }
    }
}

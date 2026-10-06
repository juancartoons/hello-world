using UnityEngine;

// Música del trazo (parte de ControlManos): mientras dibujas suena una melodía que sigue tu línea.
//  - Escala pentatónica (siempre suena bonita, aunque el trazo sea caótico).
//  - Arriba = agudo, abajo = grave (respecto a donde empezó la línea; cada 3 cm una nota, en Dubstep 2 cm).
//  - El ritmo lo pone tu mano: una nota cada pocos centímetros que recorres.
//  - Al terminar la línea (abrir los dedos) suena un acorde corto que "cierra" la frase.
//  - Post-punk, Drum & Bass y Dubstep traen batería: arranca al empezar a dibujar y tus notas caen AL COMPÁS.
//    Al soltar (o si dejas la mano quieta), la batería termina en el siguiente golpe con un ¡crash!
//    Si te vuelves a mover, arranca otra vez.
//  - Agrandar con las dos manos = notas que suben; achicar = notas que bajan.
//  - Instrumento: el carrusel de íconos sobre el parlante (BotonSonido). Silencio: el parlante.
public partial class ControlManos
{
    const string ClaveMusica = "jcartoons_sonido";
    const float PasoNota = 0.035f;   // metros recorridos entre una nota y la siguiente
    const float VolumenMusica = 0.35f;
    const float PasoTamano = 0.07f;  // cuánto hay que agrandar o achicar (7 %) para la siguiente nota

    AudioSource fuenteMusica, fuenteRitmo;
    Trazo trazoMusica;
    Vector3 musicaInicio, musicaPrevio;
    float musicaRecorrido;
    int ultimaNota = 5;
    int instrumento = -1; // -1 = aún no leído
    int pasoRitmo = -1, pasoFin = -1;
    float ritmoSoltado = -100f, ultimoMovimiento;
    float distanciaTamano = -1f;
    int notaTamano = 5;

    // El tutorial puede tocar con otro instrumento un momento (la orquesta en su demostración, el piano
    // en tu primera línea) sin cambiar el que elegiste. -1 = el tuyo.
    [System.NonSerialized] public int instrumentoTutorial = -1;

    // El instrumento que elegiste (se recuerda).
    public int Instrumento
    {
        get
        {
            if (instrumento < 0)
                instrumento = Mathf.Clamp(PlayerPrefs.GetInt(ClaveMusica, Sonidos.Piano), 0, Sonidos.Instrumentos.Length - 1);
            return instrumento;
        }
    }

    // El que suena ahora (el del tutorial, si lo hay; si no, el tuyo). El carrusel muestra este.
    public int InstrumentoActivo => instrumentoTutorial >= 0 ? instrumentoTutorial : Instrumento;

    // Encendida = sin silencio (el parlante).
    public bool MusicaEncendida => !BotonSonido.Silenciado;

    bool ConRitmo => Sonidos.TieneRitmo(InstrumentoActivo);

    // Lo llama el carrusel al elegir un ícono.
    public void ElegirInstrumento(int nuevo)
    {
        nuevo = Mathf.Clamp(nuevo, 0, Sonidos.Instrumentos.Length - 1);
        instrumentoTutorial = -1;
        instrumento = nuevo;
        PlayerPrefs.SetInt(ClaveMusica, instrumento);
        PlayerPrefs.Save();
        Sonidos.PrepararInstrumento(instrumento);
        PararRitmo(false);
        // Una notita de muestra con el instrumento nuevo (si aún se está preparando, suena apenas esté lista).
        muestraPendiente = instrumento;
        muestraHasta = Time.time + 5f;
        TocarMuestra();
    }

    int muestraPendiente = -1;
    float muestraHasta;

    void TocarMuestra()
    {
        if (muestraPendiente < 0)
            return;
        if (muestraPendiente != InstrumentoActivo || Time.time > muestraHasta || !MusicaEncendida)
        {
            muestraPendiente = -1;
            return;
        }
        if (Sonidos.NotaInstrumento(muestraPendiente, 7) == null)
            return;
        muestraPendiente = -1;
        NotaMusical(7);
    }

    // Para el tutorial: cambia el instrumento que suena (sin guardarlo). -1 = vuelve al tuyo.
    public void InstrumentoDelTutorial(int i)
    {
        if (instrumentoTutorial == i)
            return;
        instrumentoTutorial = i;
        if (i >= 0)
            Sonidos.PrepararInstrumento(i);
        PararRitmo(false);
    }

    // La nota (0 a 12) según qué tan arriba o abajo está algo respecto al inicio (en metros, de 3 en 3 cm).
    public static int IndicePorAltura(float altura)
    {
        return IndicePorAltura(altura, 0.03f);
    }

    static int IndicePorAltura(float altura, float paso)
    {
        return Mathf.Clamp(5 + Mathf.RoundToInt(altura / paso), 0, Sonidos.NotasEscala - 1);
    }

    int IndiceNota(float altura) => IndicePorAltura(altura, Sonidos.AlturaNota(InstrumentoActivo));

    public void NotaMusical(int indice)
    {
        if (!MusicaEncendida)
            return;
        AsegurarFuenteMusica();
        ultimaNota = indice;
        int i = InstrumentoActivo;
        var clip = Sonidos.NotaInstrumento(i, indice);
        if (clip != null) // si aún se está preparando (en segundo plano), esa nota no suena
            fuenteMusica.PlayOneShot(clip, VolumenMusica * Sonidos.VolumenNota(i));
    }

    // Acorde corto (Do-Mi-Sol de la octava de la última nota): "fin de la frase".
    // Con batería: una sola nota grave (el post-punk ya toca acordes).
    public void AcordeMusical()
    {
        if (!MusicaEncendida)
            return;
        AsegurarFuenteMusica();
        int i = InstrumentoActivo;
        float volumen = VolumenMusica * Sonidos.VolumenNota(i);
        int baseNota = Mathf.Clamp((ultimaNota / 5) * 5, 0, Sonidos.NotasEscala - 4);
        if (ConRitmo)
        {
            var grave = Sonidos.NotaInstrumento(i, baseNota);
            if (grave != null)
                fuenteMusica.PlayOneShot(grave, volumen);
            return;
        }
        foreach (int n in new[] { baseNota, baseNota + 2, baseNota + 3 })
        {
            var clip = Sonidos.NotaInstrumento(i, n);
            if (clip != null)
                fuenteMusica.PlayOneShot(clip, volumen * 0.7f);
        }
    }

    // Al empezar la app: se preparan (en segundo plano) tu instrumento y los del tutorial.
    void PrepararMusica()
    {
        Sonidos.PrepararInstrumento(Instrumento);
        Sonidos.PrepararInstrumento(Sonidos.Piano);
        Sonidos.PrepararInstrumento(Sonidos.Orquesta);
    }

    void AsegurarFuenteMusica()
    {
        if (fuenteMusica != null)
            return;
        var go = new GameObject("MusicaTrazo");
        go.transform.SetParent(transform, false);
        fuenteMusica = go.AddComponent<AudioSource>();
        fuenteMusica.playOnAwake = false;
        fuenteMusica.spatialBlend = 0f;
        fuenteRitmo = go.AddComponent<AudioSource>();
        fuenteRitmo.playOnAwake = false;
        fuenteRitmo.spatialBlend = 0f;
        fuenteRitmo.loop = true;
        Sonidos.PrepararInstrumento(InstrumentoActivo);
    }

    bool RitmoSonando => fuenteRitmo != null && fuenteRitmo.isPlaying;

    // Arranca la batería (si no estaba sonando). Devuelve true si empezó ahora (estamos en el primer golpe).
    bool EmpezarRitmo()
    {
        AsegurarFuenteMusica();
        var clip = Sonidos.Ritmo(InstrumentoActivo);
        if (clip == null)
            return false;
        if (fuenteRitmo.clip != clip)
        {
            fuenteRitmo.Stop();
            fuenteRitmo.clip = clip;
        }
        if (fuenteRitmo.isPlaying)
            return false;
        fuenteRitmo.volume = Sonidos.VolumenRitmo(InstrumentoActivo);
        fuenteRitmo.timeSamples = 0;
        fuenteRitmo.Play();
        pasoRitmo = 0;
        pasoFin = 0;
        return true;
    }

    void PararRitmo(bool conCrash)
    {
        if (!RitmoSonando)
            return;
        fuenteRitmo.Stop();
        if (conCrash && MusicaEncendida)
            fuenteMusica.PlayOneShot(Sonidos.Crash, VolumenMusica);
    }

    // En qué parte del compás va la batería (0 .. pasos-1).
    int PasoActualRitmo()
    {
        var clip = fuenteRitmo.clip;
        if (clip == null || clip.samples <= 0)
            return 0;
        int pasos = Sonidos.PasosPorCompas(InstrumentoActivo);
        return Mathf.Clamp((int)((long)fuenteRitmo.timeSamples * pasos / clip.samples), 0, pasos - 1);
    }

    // Cada cuadro: si estás dibujando, suena la melodía de tu línea; al terminarla, el acorde.
    void ActualizarMusica(bool puedeSonar)
    {
        TocarMuestra();
        ActualizarMusicaTamano(puedeSonar);
        bool dibujando = puedeSonar && GestoIzq == Gesto.Dibujar && trazoActual != null && trazoActual.Dibujando && Der.valida;
        if (RitmoSonando && (!puedeSonar || !ConRitmo || !MusicaEncendida))
            PararRitmo(false);
        Vector3 p = Der.indice;
        if (dibujando && trazoMusica == trazoActual && Vector3.Distance(p, musicaPrevio) > 0.0015f)
            ultimoMovimiento = Time.time;
        // La batería termina rápido y a tiempo: si sueltas (0.3 s) o dejas la mano quieta (0.6 s),
        // para en el siguiente golpe (pulso) con un crash.
        if (RitmoSonando)
        {
            bool terminar = dibujando ? Time.time - ultimoMovimiento > 0.6f : Time.time - ritmoSoltado > 0.3f;
            int paso = PasoActualRitmo();
            int porPulso = Mathf.Max(1, Sonidos.PasosPorCompas(InstrumentoActivo) / 4);
            if (terminar && paso != pasoFin && paso % porPulso == 0)
                PararRitmo(true);
            pasoFin = paso;
        }
        if (!dibujando)
        {
            if (trazoMusica != null)
            {
                if (puedeSonar)
                    AcordeMusical();
                trazoMusica = null;
                ritmoSoltado = Time.time;
            }
            return;
        }
        if (!MusicaEncendida)
            return;
        if (trazoMusica != trazoActual)
        {
            trazoMusica = trazoActual;
            musicaInicio = p;
            musicaPrevio = p;
            musicaRecorrido = 0f;
            ultimoMovimiento = Time.time;
            if (!ConRitmo || EmpezarRitmo())
            {
                NotaMusical(IndiceNota(0f));
            }
            else
            {
                // La batería ya sonaba (otra línea seguida): la primera nota espera al compás.
                musicaRecorrido = 0.01f;
                pasoRitmo = PasoActualRitmo();
            }
            return;
        }
        musicaRecorrido += Vector3.Distance(p, musicaPrevio);
        musicaPrevio = p;
        if (ConRitmo)
        {
            if (!RitmoSonando)
            {
                // Se había parado por la mano quieta: vuelve a arrancar en cuanto te mueves.
                if (Time.time - ultimoMovimiento < 0.1f && EmpezarRitmo())
                {
                    musicaRecorrido = 0f;
                    NotaMusical(IndiceNota(p.y - musicaInicio.y));
                }
                return;
            }
            // Las notas caen justo en los pasos del compás (si tu mano se movió desde la nota anterior).
            int paso = PasoActualRitmo();
            if (paso == pasoRitmo)
                return;
            pasoRitmo = paso;
            if (paso % Sonidos.PasosPorNota(InstrumentoActivo) != 0 || musicaRecorrido < 0.01f)
                return;
            musicaRecorrido = 0f;
            NotaMusical(IndiceNota(p.y - musicaInicio.y));
            return;
        }
        if (musicaRecorrido >= PasoNota)
        {
            musicaRecorrido = 0f;
            NotaMusical(IndiceNota(p.y - musicaInicio.y));
        }
    }

    // Agrandar o achicar con las dos manos (OK + OK): cada 7 % más grande, una nota más aguda;
    // cada 7 % más chico, una más grave (como una escalera de notas).
    void ActualizarMusicaTamano(bool puedeSonar)
    {
        if (!puedeSonar || GestoIzq != Gesto.Transformar || DibujoBloqueado || !Izq.valida || !Der.valida)
        {
            distanciaTamano = -1f;
            return;
        }
        float d = Vector3.Distance(Izq.PuntoPellizco, Der.PuntoPellizco);
        if (d < 0.02f)
            return;
        if (distanciaTamano < 0f)
        {
            distanciaTamano = d;
            notaTamano = 5;
            return;
        }
        float cambio = Mathf.Log(d / distanciaTamano);
        if (Mathf.Abs(cambio) < PasoTamano)
            return;
        distanciaTamano = d;
        notaTamano = Mathf.Clamp(notaTamano + (cambio > 0f ? 1 : -1), 0, Sonidos.NotasEscala - 1);
        NotaMusical(notaTamano);
    }
}

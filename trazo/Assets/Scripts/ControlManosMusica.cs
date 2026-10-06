using UnityEngine;

// Música del trazo (parte de ControlManos): mientras dibujas suena una melodía que sigue tu línea.
//  - Escala pentatónica (siempre suena bonita, aunque el trazo sea caótico).
//  - Arriba = agudo, abajo = grave (respecto a donde empezó la línea; cada 3 cm, una nota).
//  - El ritmo lo pone tu mano: una nota cada pocos centímetros que recorres.
//  - Al terminar la línea (abrir los dedos) suena un acorde corto que "cierra" la frase.
//  - Rock, Punk y Drum & Bass traen batería: arranca al empezar a dibujar y tus notas caen AL COMPÁS.
//    Al soltar, la batería sigue un ratito (si empiezas otra línea, la canción sigue); si no, termina
//    al final del compás con un ¡crash!
//  - Agrandar con las dos manos = notas que suben; achicar = notas que bajan.
//  - Instrumento: botón "Música" del menú del reloj. Silencio: el parlante de arriba a la derecha.
public partial class ControlManos
{
    const string ClaveMusica = "jcartoons_instrumento";
    const float PasoNota = 0.035f;   // metros recorridos entre una nota y la siguiente
    const float AlturaNota = 0.03f;  // metros que hay que subir o bajar para cambiar de nota
    const float VolumenMusica = 0.35f;
    const float VolumenRitmo = 0.3f;
    const float PasoTamano = 0.07f;  // cuánto hay que agrandar o achicar (7 %) para la siguiente nota

    AudioSource fuenteMusica, fuenteRitmo;
    Trazo trazoMusica;
    Vector3 musicaInicio, musicaPrevio;
    float musicaRecorrido;
    int ultimaNota = 5;
    int instrumento = -1; // -1 = aún no leído
    int pasoRitmo = -1, muestrasRitmo;
    float ritmoSoltado = -100f;
    float distanciaTamano = -1f;
    int notaTamano = 5;

    public int Instrumento
    {
        get
        {
            if (instrumento < 0)
                instrumento = Mathf.Clamp(PlayerPrefs.GetInt(ClaveMusica, 0), 0, Sonidos.Instrumentos.Length - 1);
            return instrumento;
        }
    }

    // Encendida = sin silencio (el parlante).
    public bool MusicaEncendida => !BotonSonido.Silenciado;

    public string NombreMusica => "Música: " + Sonidos.Instrumentos[Instrumento];

    bool ConRitmo => Sonidos.TieneRitmo(Instrumento);

    public void SiguienteInstrumento()
    {
        instrumento = (Instrumento + 1) % Sonidos.Instrumentos.Length;
        PlayerPrefs.SetInt(ClaveMusica, instrumento);
        PlayerPrefs.Save();
        Sonidos.PrepararInstrumento(instrumento);
        PararRitmo(false);
        // Una notita de muestra con el instrumento nuevo.
        NotaMusical(7);
        MostrarEtiqueta(NombreMusica);
    }

    // La nota (0 a 12) según qué tan arriba o abajo está algo respecto al inicio (en metros).
    public static int IndicePorAltura(float altura)
    {
        return Mathf.Clamp(5 + Mathf.RoundToInt(altura / AlturaNota), 0, Sonidos.NotasEscala - 1);
    }

    public void NotaMusical(int indice)
    {
        if (!MusicaEncendida)
            return;
        AsegurarFuenteMusica();
        ultimaNota = indice;
        fuenteMusica.PlayOneShot(Sonidos.NotaInstrumento(Instrumento, indice), VolumenMusica);
    }

    // Acorde corto (Do-Mi-Sol de la octava de la última nota): "fin de la frase".
    // Rock, Punk y Drum & Bass: una nota grave (la guitarra ya toca acordes de quinta).
    public void AcordeMusical()
    {
        if (!MusicaEncendida)
            return;
        AsegurarFuenteMusica();
        int baseNota = Mathf.Clamp((ultimaNota / 5) * 5, 0, Sonidos.NotasEscala - 4);
        if (ConRitmo)
        {
            fuenteMusica.PlayOneShot(Sonidos.NotaInstrumento(Instrumento, baseNota), VolumenMusica);
            return;
        }
        foreach (int n in new[] { baseNota, baseNota + 2, baseNota + 3 })
            fuenteMusica.PlayOneShot(Sonidos.NotaInstrumento(Instrumento, n), VolumenMusica * 0.7f);
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
        fuenteRitmo.volume = VolumenRitmo;
        Sonidos.PrepararInstrumento(Instrumento);
    }

    // Arranca la batería (si no estaba sonando). Devuelve true si empezó ahora (estamos en el primer golpe).
    bool EmpezarRitmo()
    {
        AsegurarFuenteMusica();
        var clip = Sonidos.Ritmo(Instrumento);
        if (clip == null)
            return false;
        if (fuenteRitmo.clip != clip)
        {
            fuenteRitmo.Stop();
            fuenteRitmo.clip = clip;
        }
        if (fuenteRitmo.isPlaying)
            return false;
        fuenteRitmo.timeSamples = 0;
        fuenteRitmo.Play();
        muestrasRitmo = 0;
        pasoRitmo = 0;
        return true;
    }

    void PararRitmo(bool conCrash)
    {
        if (fuenteRitmo == null || !fuenteRitmo.isPlaying)
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
        int pasos = Sonidos.PasosPorCompas(Instrumento);
        return Mathf.Clamp((int)((long)fuenteRitmo.timeSamples * pasos / clip.samples), 0, pasos - 1);
    }

    // Cada cuadro: si estás dibujando, suena la melodía de tu línea; al terminarla, el acorde.
    void ActualizarMusica(bool puedeSonar)
    {
        ActualizarMusicaTamano(puedeSonar);
        bool dibujando = puedeSonar && GestoIzq == Gesto.Dibujar && trazoActual != null && trazoActual.Dibujando && Der.valida;
        // ¿La batería dio la vuelta al compás?
        bool vuelta = false;
        if (fuenteRitmo != null && fuenteRitmo.isPlaying)
        {
            int m = fuenteRitmo.timeSamples;
            vuelta = m < muestrasRitmo;
            muestrasRitmo = m;
            if (!puedeSonar || !ConRitmo || !MusicaEncendida)
                PararRitmo(false);
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
            // Sin dibujar: la batería termina al final del compás (con su crash).
            if (vuelta && Time.time - ritmoSoltado > 0.9f)
                PararRitmo(true);
            return;
        }
        if (!MusicaEncendida)
            return;
        Vector3 p = Der.indice;
        if (trazoMusica != trazoActual)
        {
            trazoMusica = trazoActual;
            musicaInicio = p;
            musicaPrevio = p;
            musicaRecorrido = 0f;
            if (!ConRitmo || EmpezarRitmo())
            {
                NotaMusical(IndicePorAltura(0f));
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
            if (fuenteRitmo == null || !fuenteRitmo.isPlaying)
            {
                EmpezarRitmo();
                return;
            }
            // Las notas caen justo en los pasos del compás (si tu mano se movió desde la nota anterior).
            int paso = PasoActualRitmo();
            if (paso == pasoRitmo)
                return;
            pasoRitmo = paso;
            if (paso % Sonidos.PasosPorNota(Instrumento) != 0 || musicaRecorrido < 0.01f)
                return;
            musicaRecorrido = 0f;
            NotaMusical(IndicePorAltura(p.y - musicaInicio.y));
            return;
        }
        if (musicaRecorrido >= PasoNota)
        {
            musicaRecorrido = 0f;
            NotaMusical(IndicePorAltura(p.y - musicaInicio.y));
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

using UnityEngine;

// Música del trazo (parte de ControlManos): mientras dibujas suena una melodía que sigue tu línea.
//  - Escala pentatónica (siempre suena bonita, aunque el trazo sea caótico).
//  - Arriba = agudo, abajo = grave (respecto a donde empezó la línea; cada 3 cm, una nota).
//  - El ritmo lo pone tu mano: una nota cada pocos centímetros que recorres.
//  - Al terminar la línea (abrir los dedos) suena un acorde corto que "cierra" la frase.
//  - Instrumento: Arpa, Piano, Marimba, Cajita de música o apagado (botón "Música" del menú del reloj).
public partial class ControlManos
{
    const string ClaveMusica = "jcartoons_musica";
    const float PasoNota = 0.035f;   // metros recorridos entre una nota y la siguiente
    const float AlturaNota = 0.03f;  // metros que hay que subir o bajar para cambiar de nota
    const float VolumenMusica = 0.35f;

    AudioSource fuenteMusica;
    Trazo trazoMusica;
    Vector3 musicaInicio, musicaPrevio;
    float musicaRecorrido;
    int ultimaNota = 5;
    int instrumento = -2; // -2 = aún no leído

    // 0..3 = instrumento; 4 = apagado.
    public int Instrumento
    {
        get
        {
            if (instrumento == -2)
                instrumento = Mathf.Clamp(PlayerPrefs.GetInt(ClaveMusica, 0), 0, Sonidos.Instrumentos.Length);
            return instrumento;
        }
    }

    public bool MusicaEncendida => Instrumento < Sonidos.Instrumentos.Length;

    public string NombreMusica => MusicaEncendida ? "Música: " + Sonidos.Instrumentos[Instrumento] : "Música: No";

    public void SiguienteInstrumento()
    {
        instrumento = (Instrumento + 1) % (Sonidos.Instrumentos.Length + 1);
        PlayerPrefs.SetInt(ClaveMusica, instrumento);
        PlayerPrefs.Save();
        // Una notita de muestra con el instrumento nuevo.
        if (MusicaEncendida)
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
    public void AcordeMusical()
    {
        if (!MusicaEncendida)
            return;
        AsegurarFuenteMusica();
        int baseNota = Mathf.Clamp((ultimaNota / 5) * 5, 0, Sonidos.NotasEscala - 4);
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
    }

    // Cada cuadro: si estás dibujando, suena la melodía de tu línea; al terminarla, el acorde.
    void ActualizarMusica(bool puedeSonar)
    {
        bool dibujando = puedeSonar && GestoIzq == Gesto.Dibujar && trazoActual != null && trazoActual.Dibujando && Der.valida;
        if (!dibujando)
        {
            if (trazoMusica != null)
            {
                if (puedeSonar)
                    AcordeMusical();
                trazoMusica = null;
            }
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
            NotaMusical(IndicePorAltura(0f));
            return;
        }
        musicaRecorrido += Vector3.Distance(p, musicaPrevio);
        musicaPrevio = p;
        if (musicaRecorrido >= PasoNota)
        {
            musicaRecorrido = 0f;
            NotaMusical(IndicePorAltura(p.y - musicaInicio.y));
        }
    }
}

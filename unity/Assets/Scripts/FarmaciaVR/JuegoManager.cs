using System.Collections;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

// El "director" del juego: esconde al personaje, cuenta el tiempo,
// muestra el premio y reinicia solo para el siguiente jugador.
public class JuegoManager : MonoBehaviour
{
    [Header("Referencias")]
    public PersonajeEncontrable personaje;
    [Tooltip("Objeto vacío cuyos hijos son los escondites posibles")]
    public Transform escondites;

    [Header("Panel (Canvas World Space)")]
    public GameObject panel;
    public TMP_Text textoTitulo;
    public TMP_Text textoCodigo;
    [Tooltip("Opcional: texto con el tiempo restante (por ejemplo, como 'reloj' en la muñeca)")]
    public TMP_Text textoTiempo;
    [Tooltip("Distancia (metros) a la que aparece el panel frente al jugador")]
    public float distanciaPanel = 1.2f;

    [Header("Reglas")]
    public float segundosIntro = 4f;
    public float segundosParaBuscar = 90f;
    public float segundosAntesDeReiniciar = 20f;
    public string[] codigos = { "FARMA-7K2Q", "FARMA-3M8P", "FARMA-9T4X", "FARMA-5B1R", "FARMA-2H6W" };

    [Header("Sonidos y efectos (opcionales)")]
    [Tooltip("Arrastra aquí el sonido del 'pío' (mp3/wav). Suena en 3D desde el personaje")]
    public AudioClip sonidoPio;
    public float segundosEntrePios = 4f;
    [Tooltip("Arrastra aquí el sonido de celebración al encontrarlo")]
    public AudioClip sonidoCelebracion;
    public ParticleSystem confeti;

    Transform cabeza;
    AudioSource fuentePersonaje;
    int ultimoEscondite = -1;
    float tiempoRestante;
    bool buscando;

    void Start()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        cabeza = rig != null ? rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : null;

        // El "pío" sale del personaje en 3D: se oye más fuerte al acercarse.
        fuentePersonaje = personaje.GetComponent<AudioSource>();
        if (fuentePersonaje == null)
            fuentePersonaje = personaje.gameObject.AddComponent<AudioSource>();
        fuentePersonaje.playOnAwake = false;
        fuentePersonaje.loop = false;
        fuentePersonaje.spatialBlend = 1f;
        fuentePersonaje.rolloffMode = AudioRolloffMode.Linear;
        fuentePersonaje.maxDistance = 15f;

        personaje.alSerEncontrado.AddListener(AlEncontrarlo);
        StartCoroutine(NuevaPartida());
    }

    IEnumerator NuevaPartida()
    {
        buscando = false;
        personaje.Activo = false;
        personaje.Reiniciar();
        EsconderPersonaje();
        if (textoTiempo != null) textoTiempo.text = "";

        MostrarPanel("¡Encuentra al personaje escondido!", $"Tienes {Mathf.RoundToInt(segundosParaBuscar)} segundos");
        yield return new WaitForSeconds(segundosIntro);
        panel.SetActive(false);

        tiempoRestante = segundosParaBuscar;
        buscando = true;
        personaje.Activo = true;
        StartCoroutine(Pios());
    }

    void Update()
    {
        if (!buscando)
            return;

        tiempoRestante -= Time.deltaTime;
        if (textoTiempo != null)
            textoTiempo.text = $"{Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante))} s";

        if (tiempoRestante <= 0f)
            SeAcaboElTiempo();
    }

    void EsconderPersonaje()
    {
        int cantidad = escondites.childCount;
        if (cantidad == 0)
        {
            Debug.LogWarning("JuegoManager: 'escondites' no tiene hijos.", this);
            return;
        }

        int indice = Random.Range(0, cantidad);
        if (cantidad > 1 && indice == ultimoEscondite)
            indice = (indice + 1) % cantidad; // nunca repite el escondite anterior
        ultimoEscondite = indice;

        Transform punto = escondites.GetChild(indice);
        personaje.transform.SetPositionAndRotation(punto.position, punto.rotation);
    }

    IEnumerator Pios()
    {
        while (buscando)
        {
            if (sonidoPio != null)
                fuentePersonaje.PlayOneShot(sonidoPio);
            yield return new WaitForSeconds(segundosEntrePios);
        }
    }

    void AlEncontrarlo()
    {
        buscando = false;
        string codigo = codigos.Length > 0 ? codigos[Random.Range(0, codigos.Length)] : "";

        MostrarPanel("¡Me encontraste!\nTu bono de descuento:", codigo);
        if (confeti != null)
        {
            confeti.transform.position = personaje.transform.position;
            confeti.Play();
        }
        if (sonidoCelebracion != null && cabeza != null)
            AudioSource.PlayClipAtPoint(sonidoCelebracion, cabeza.position);

        StartCoroutine(ReiniciarDespues());
    }

    void SeAcaboElTiempo()
    {
        buscando = false;
        personaje.Activo = false;
        MostrarPanel("¡Se acabó el tiempo!", "Inténtalo de nuevo");
        StartCoroutine(ReiniciarDespues());
    }

    IEnumerator ReiniciarDespues()
    {
        yield return new WaitForSeconds(segundosAntesDeReiniciar);
        StartCoroutine(NuevaPartida());
    }

    // Pone el panel frente al jugador, a la altura de sus ojos, mirándolo.
    void MostrarPanel(string titulo, string codigo)
    {
        if (textoTitulo != null) textoTitulo.text = titulo;
        if (textoCodigo != null) textoCodigo.text = codigo;

        if (cabeza != null)
        {
            Vector3 frente = cabeza.forward;
            frente.y = 0f;
            if (frente.sqrMagnitude < 0.001f) frente = Vector3.forward;
            frente.Normalize();

            panel.transform.position = cabeza.position + frente * distanciaPanel;
            panel.transform.rotation = Quaternion.LookRotation(frente, Vector3.up);
        }
        panel.SetActive(true);
    }
}

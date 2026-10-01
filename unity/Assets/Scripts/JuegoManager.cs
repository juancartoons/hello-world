using System.Collections;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

// El "director" del juego:
// 1. El jugador empieza AFUERA, frente a la entrada (ve el letrero y las instrucciones).
// 2. Con una palmada entra a la farmacia y empieza a buscar.
// 3. Al encontrar al personaje (o acabarse el tiempo) muestra el resultado.
// 4. Con una palmada (en cualquier momento) vuelve a empezar desde afuera.
// También ajusta la altura sola si el jugador está sentado.
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
    [Tooltip("Cronómetro que se ve siempre a un lado mientras buscas (lo crea el menú ★)")]
    public TMP_Text textoTiempo;
    [Tooltip("Botones para elegir la navegación (aparecen afuera, antes de entrar)")]
    public GameObject opcionesNavegacion;
    [Tooltip("Distancia (metros) a la que aparece el panel frente al jugador")]
    public float distanciaPanel = 1.2f;

    [Header("Lugares")]
    [Tooltip("Donde empieza el jugador: afuera, en el andén frente a la puerta")]
    public Vector3 puntoAfuera = new Vector3(0f, 0f, -7.2f);
    [Tooltip("Donde aparece al entrar a la farmacia")]
    public Vector3 puntoAdentro = new Vector3(0f, 0f, -4f);

    [Header("Reglas")]
    public float segundosIntro = 2.5f;
    public float segundosParaBuscar = 90f;
    [Tooltip("Si nadie da la palmada al terminar, vuelve solo a la entrada después de estos segundos")]
    public float segundosParaVolverSolo = 60f;
    public string[] codigos = { "FARMA-7K2Q", "FARMA-3M8P", "FARMA-9T4X", "FARMA-5B1R", "FARMA-2H6W" };

    [Header("Altura automática (sentado / de pie)")]
    [Tooltip("Si los ojos están más abajo de esto (metros), se asume que la persona está sentada")]
    public float alturaSentado = 1.25f;
    [Tooltip("Altura de los ojos (metros) a la que se sube a quien está sentado")]
    public float alturaObjetivo = 1.55f;

    [Header("Sonidos y efectos (opcionales)")]
    [Tooltip("Arrastra aquí el sonido del 'pío' (mp3/wav). Suena en 3D desde el personaje")]
    public AudioClip sonidoPio;
    public float segundosEntrePios = 4f;
    [Tooltip("Arrastra aquí el sonido de celebración al encontrarlo")]
    public AudioClip sonidoCelebracion;
    public ParticleSystem confeti;

    enum Estado { Afuera, Entrando, Buscando, Terminado }

    const string textoPalmada = "<size=45%>Da una palmada para empezar</size>";
    const string textoOtraVez = "<size=40%>\nDa una palmada para jugar otra vez</size>";

    OVRCameraRig rig;
    Transform cabeza;
    AudioSource fuentePersonaje;
    int ultimoEscondite = -1;
    float tiempoRestante;
    Estado estado;
    Coroutine rutina;

    void Start()
    {
        rig = FindFirstObjectByType<OVRCameraRig>();
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

        var palmadas = GetComponent<DetectorPalmadas>();
        if (palmadas == null)
            palmadas = gameObject.AddComponent<DetectorPalmadas>();
        palmadas.AlAplaudir += AlAplaudir;

        // Espera un momento a que el visor tenga la posición de la cabeza antes de ubicar al jugador.
        Cambiar(EsperarYEmpezar());
    }

    IEnumerator EsperarYEmpezar()
    {
        yield return new WaitForSeconds(0.5f);
        EmpezarAfuera();
    }

    // ---------- Flujo del juego ----------

    float ultimaPalmada = -10f;
    float ignorarPalmadasHasta;

    void AlAplaudir()
    {
        // Justo después de cambiar de etapa no se escuchan palmadas (para que una sola no cuente dos veces).
        if (Time.time < ignorarPalmadasHasta)
            return;
        if (estado == Estado.Afuera)
            Cambiar(Entrar());
        else if (estado == Estado.Terminado)
            EmpezarAfuera();
        else if (estado == Estado.Buscando)
        {
            // Mientras buscas hay que dar DOS palmadas seguidas para reiniciar (evita reinicios por accidente).
            if (Time.time - ultimaPalmada < 1.5f)
                EmpezarAfuera();
            ultimaPalmada = Time.time;
        }
    }

    void EmpezarAfuera()
    {
        Detener();
        estado = Estado.Afuera;
        ignorarPalmadasHasta = Time.time + 1f;
        personaje.Activo = false;
        personaje.Reiniciar();
        EsconderPersonaje();
        MostrarCronometro(false);

        MoverJugador(puntoAfuera);
        MostrarPanel("¡Encuentra al personaje escondido!", textoPalmada);
        MostrarOpciones(true);
    }

    IEnumerator Entrar()
    {
        estado = Estado.Entrando;
        MostrarOpciones(false);
        MoverJugador(puntoAdentro);
        MostrarPanel("¡A buscar!", $"Tienes {Mathf.RoundToInt(segundosParaBuscar)} segundos");
        yield return new WaitForSeconds(segundosIntro);
        panel.SetActive(false);

        tiempoRestante = segundosParaBuscar;
        estado = Estado.Buscando;
        personaje.Activo = true;
        MostrarCronometro(true);
        StartCoroutine(Pios());
    }

    void Update()
    {
        if (estado != Estado.Buscando)
            return;

        tiempoRestante -= Time.deltaTime;
        ActualizarCronometro();

        if (tiempoRestante <= 0f)
            Terminar("GAME OVER", "<size=40%>Da una palmada para jugar otra vez</size>");
    }

    // ---------- Cronómetro (como unas gafas XR: siempre visible a la derecha) ----------

    void MostrarCronometro(bool visible)
    {
        if (textoTiempo == null)
            return;
        textoTiempo.gameObject.SetActive(visible);
        if (visible)
            ActualizarCronometro();
    }

    void ActualizarCronometro()
    {
        if (textoTiempo == null)
            return;
        int segundos = Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante));
        string color = segundos <= 10 ? "#FF5A4E" : "#FFFFFF";
        textoTiempo.text = $"<mark=#00000099><color={color}> {segundos / 60}:{segundos % 60:00} </color></mark>";
    }

    void MostrarOpciones(bool visible)
    {
        if (opcionesNavegacion == null)
            return;
        if (visible && cabeza != null)
        {
            // Al alcance de la mano: un poco abajo y adelante.
            Vector3 frente = cabeza.forward;
            frente.y = 0f;
            if (frente.sqrMagnitude < 0.001f) frente = Vector3.forward;
            frente.Normalize();
            opcionesNavegacion.transform.position = cabeza.position + frente * 0.45f + Vector3.down * 0.35f;
            opcionesNavegacion.transform.rotation = Quaternion.LookRotation(frente, Vector3.up);
        }
        opcionesNavegacion.SetActive(visible);
    }

    void AlEncontrarlo()
    {
        string codigo = codigos.Length > 0 ? codigos[Random.Range(0, codigos.Length)] : "";
        Terminar("¡Me encontraste!\nTu bono de descuento:", codigo + textoOtraVez);

        if (confeti != null)
        {
            confeti.transform.position = personaje.transform.position;
            confeti.Play();
        }
        if (sonidoCelebracion != null && cabeza != null)
            AudioSource.PlayClipAtPoint(sonidoCelebracion, cabeza.position);
    }

    void Terminar(string titulo, string texto)
    {
        estado = Estado.Terminado;
        ignorarPalmadasHasta = Time.time + 1f;
        personaje.Activo = false;
        MostrarCronometro(false);
        MostrarPanel(titulo, texto);
        Cambiar(VolverSolo());
    }

    IEnumerator VolverSolo()
    {
        yield return new WaitForSeconds(segundosParaVolverSolo);
        EmpezarAfuera();
    }

    void Cambiar(IEnumerator nueva)
    {
        Detener();
        rutina = StartCoroutine(nueva);
    }

    void Detener()
    {
        if (rutina != null)
            StopCoroutine(rutina);
        rutina = null;
    }

    IEnumerator Pios()
    {
        while (estado == Estado.Buscando)
        {
            if (sonidoPio != null)
                fuentePersonaje.PlayOneShot(sonidoPio);
            yield return new WaitForSeconds(segundosEntrePios);
        }
    }

    // ---------- Jugador ----------

    // Mueve el rig para que la cabeza quede sobre "destino" y ajusta la altura (sentado / de pie).
    void MoverJugador(Vector3 destino)
    {
        if (rig == null || cabeza == null)
            return;
        Vector3 delta = destino - cabeza.position;
        delta.y = 0f;
        rig.transform.position += delta;
        AjustarAltura();
    }

    // Sube la vista (no el cuerpo) si la persona está sentada. Se mueve el "tracking space" del rig
    // y no el rig completo, porque el sistema de movimiento de Meta mantiene el rig pegado al piso.
    void AjustarAltura()
    {
        var espacio = rig.trackingSpace;
        if (espacio == null)
            return;
        float alturaOjos = cabeza.localPosition.y; // altura de los ojos sobre el piso real
        float extra = alturaOjos < alturaSentado ? alturaObjetivo - alturaOjos : 0f;
        espacio.localPosition = new Vector3(espacio.localPosition.x, extra, espacio.localPosition.z);
    }

    // ---------- Escondites ----------

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
        ApoyarEnSuperficie();
    }

    // Baja (o sube) al personaje para que quede apoyado sobre lo que tenga debajo
    // (piso o producto), sea del tamaño que sea.
    void ApoyarEnSuperficie()
    {
        var colliders = personaje.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
            return;

        Physics.SyncTransforms();
        Bounds limites = colliders[0].bounds;
        foreach (var c in colliders)
            limites.Encapsulate(c.bounds);

        Vector3 origen = new Vector3(limites.center.x, limites.max.y + 0.05f, limites.center.z);
        float superficie = float.NegativeInfinity;
        foreach (var hit in Physics.RaycastAll(origen, Vector3.down, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(personaje.transform))
                continue;
            if (hit.point.y > superficie)
                superficie = hit.point.y;
        }

        if (!float.IsNegativeInfinity(superficie))
            personaje.transform.position += Vector3.up * (superficie - limites.min.y);
    }

    // ---------- Panel ----------

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

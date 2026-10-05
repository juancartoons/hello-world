using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Un carro (o el bus) que anda por su carril, de "inicio" a "fin" (o por una "ruta" con curvas, para voltear
// en una esquina), y vuelve a empezar.
// - Frena detrás del carro de adelante (no se chocan).
// - Respeta el semáforo del cruce (SemaforoCruce).
// - Para que no se note que se repite: cada vez que da la vuelta lejos de la vista, espera un rato
//   al azar, cambia un poco su velocidad y, si tiene variantes, cambia de color.
public class CarroEnRuta : MonoBehaviour
{
    [Header("Carril")]
    public Vector3 inicio;
    public Vector3 fin;
    [Tooltip("Ruta con curvas (opcional). Si tiene 2 o más puntos, se usa en vez de inicio y fin")]
    public Vector3[] ruta;
    [Tooltip("Carros con el mismo número comparten carril (se respetan la distancia)")]
    public int carril;
    [Tooltip("Dónde arranca el carro al empezar (metros desde el inicio del carril)")]
    public float avanceInicial;

    [Header("Manejo")]
    [Tooltip("Velocidad normal (m/s). 8 m/s ≈ 30 km/h")]
    public float velocidad = 8f;
    [Tooltip("Largo del carro (metros)")]
    public float largo = 4.5f;
    public float aceleracion = 2.5f;
    public float frenado = 3.5f;
    [Tooltip("Distancia (metros) que deja con el carro de adelante")]
    public float distanciaSegura = 2.5f;

    [Header("Semáforo")]
    [Tooltip("0 = semáforo de la calle, 1 = semáforo de la carrera")]
    public int grupoSemaforo;
    [Tooltip("Línea de pare (metros desde el inicio del carril). Negativo = no hay semáforo")]
    public float lineaDePare = -1f;

    [Header("Para que no se note la repetición")]
    [Tooltip("Otras mallas (otros colores) que puede usar al dar la vuelta")]
    public Mesh[] variantes;
    public float esperaMinima = 0f;
    public float esperaMaxima = 6f;

    static readonly List<CarroEnRuta> todos = new List<CarroEnRuta>();

    float largoCarril, avance, rapidez, factor = 1f, esperaHasta;
    bool oculto;
    Vector3[] puntos;
    float[] acumulado;
    MeshFilter filtro;
    Renderer[] renders;

    void OnEnable() => todos.Add(this);
    void OnDisable() => todos.Remove(this);

    void Start()
    {
        puntos = ruta != null && ruta.Length >= 2 ? ruta : new[] { inicio, fin };
        acumulado = new float[puntos.Length];
        for (int i = 1; i < puntos.Length; i++)
            acumulado[i] = acumulado[i - 1] + Vector3.Distance(puntos[i - 1], puntos[i]);
        largoCarril = Mathf.Max(0.01f, acumulado[puntos.Length - 1]);
        filtro = GetComponent<MeshFilter>();
        renders = GetComponentsInChildren<Renderer>(true);
        avance = Mathf.Clamp(avanceInicial, 0f, largoCarril);
        factor = Random.Range(0.85f, 1.1f);
        rapidez = velocidad * factor;
        Ubicar();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (oculto)
        {
            // Espera fuera de la vista hasta que pase su tiempo y el inicio del carril esté libre.
            if (Time.time < esperaHasta || InicioOcupado())
                return;
            Mostrar(true);
        }

        float objetivo = velocidad * factor;

        // No chocar con el de adelante.
        float hueco = HuecoAdelante();
        if (hueco < float.MaxValue)
            objetivo = Mathf.Min(objetivo, VelocidadParaParar(hueco - distanciaSegura));

        // Semáforo.
        if (lineaDePare >= 0f)
        {
            float hastaLinea = lineaDePare - (avance + largo / 2f);
            if (hastaLinea > -0.2f)
            {
                var luz = SemaforoCruce.Estado(grupoSemaforo);
                bool parar = luz == SemaforoCruce.Luz.Rojo
                             || (luz == SemaforoCruce.Luz.Amarillo && hastaLinea > rapidez * rapidez / (2f * 4f) + 1f);
                if (parar)
                    objetivo = Mathf.Min(objetivo, VelocidadParaParar(hastaLinea - 0.3f));
            }
        }

        rapidez = Mathf.MoveTowards(rapidez, objetivo, (objetivo > rapidez ? aceleracion : frenado * 1.7f) * dt);
        avance += rapidez * dt;

        if (avance - largo / 2f > largoCarril)
        {
            // Salió por el final (lejos): vuelve a empezar desde el inicio, un rato después.
            Mostrar(false);
            avance = 0f;
            esperaHasta = Time.time + Random.Range(esperaMinima, esperaMaxima);
            factor = Random.Range(0.85f, 1.1f);
            rapidez = velocidad * factor;
            if (variantes != null && variantes.Length > 0 && filtro != null)
                filtro.sharedMesh = variantes[Random.Range(0, variantes.Length)];
        }
        Ubicar();
    }

    // La velocidad máxima con la que todavía se alcanza a parar en "distancia" metros, frenando suave.
    float VelocidadParaParar(float distancia)
    {
        return Mathf.Sqrt(2f * frenado * Mathf.Max(0f, distancia));
    }

    float HuecoAdelante()
    {
        float minimo = float.MaxValue;
        foreach (var otro in todos)
        {
            if (otro == this || otro.oculto || otro.carril != carril || otro.avance <= avance)
                continue;
            float hueco = (otro.avance - otro.largo / 2f) - (avance + largo / 2f);
            if (hueco < minimo)
                minimo = hueco;
        }
        return minimo;
    }

    bool InicioOcupado()
    {
        foreach (var otro in todos)
            if (otro != this && !otro.oculto && otro.carril == carril && otro.avance - otro.largo / 2f < largo + distanciaSegura + 4f)
                return true;
        return false;
    }

    void Mostrar(bool visible)
    {
        oculto = !visible;
        foreach (var r in renders)
            if (r != null)
                r.enabled = visible;
    }

    // Punto de la ruta a "s" metros del inicio.
    Vector3 PuntoEn(float s)
    {
        s = Mathf.Clamp(s, 0f, largoCarril);
        for (int i = 1; i < puntos.Length; i++)
        {
            if (s <= acumulado[i] || i == puntos.Length - 1)
            {
                float tramo = acumulado[i] - acumulado[i - 1];
                float t = tramo > 0.0001f ? (s - acumulado[i - 1]) / tramo : 0f;
                return Vector3.Lerp(puntos[i - 1], puntos[i], t);
            }
        }
        return puntos[0];
    }

    void Ubicar()
    {
        Vector3 posicion = PuntoEn(avance);
        // La dirección se toma un poco atrás y un poco adelante: así gira suave en las curvas.
        Vector3 direccion = PuntoEn(avance + 1.2f) - PuntoEn(avance - 1.2f);
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f)
            direccion = puntos[puntos.Length - 1] - puntos[0];
        // Las mallas de los carros miran hacia +X; LookRotation hace mirar +Z, por eso se gira -90°.
        var rotacion = Quaternion.LookRotation(direccion.normalized, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
        transform.SetPositionAndRotation(posicion, rotacion);
    }
}

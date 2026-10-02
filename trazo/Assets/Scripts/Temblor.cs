using UnityEngine;

// Temblor de líneas ("line boil"): las líneas vibran un poquito, como dibujadas a mano cuadro por cuadro.
// Botón "Temblor" en la página Medios del panel de arriba: No → Suave → Medio → Fuerte.
// No cambia tus nodos: solo cambia cómo se ven (y sale en los videos).
public class Temblor : MonoBehaviour
{
    public static readonly string[] Nombres = { "No", "Suave", "Medio", "Fuerte" };
    static readonly float[] Cantidades = { 0f, 0.0015f, 0.003f, 0.006f }; // metros

    static readonly int idCantidad = Shader.PropertyToID("_TrazoTemblor");
    static readonly int idFase = Shader.PropertyToID("_TrazoTemblorFase");

    // Los videos ponen aquí el "tiempo" de cada cuadro (así el temblor va al ritmo del video). -1 = tiempo real.
    public static float tiempoFijo = -1f;

    [Tooltip("Cuántas veces por segundo cambia el temblor")]
    public float cambiosPorSegundo = 10f;
    public int nivel;

    public string NombreNivel => Nombres[Mathf.Clamp(nivel, 0, Nombres.Length - 1)];

    public void Siguiente()
    {
        nivel = (nivel + 1) % Nombres.Length;
        var dibujo = GetComponent<Dibujo>();
        if (dibujo != null)
            dibujo.Mensaje("Temblor de líneas: " + NombreNivel);
    }

    // Para los videos: fija el "tiempo" del temblor en este cuadro (y lo aplica ya). Negativo = tiempo real.
    public void PonerTiempo(float t)
    {
        tiempoFijo = t;
        Actualizar();
    }

    void LateUpdate()
    {
        Actualizar();
    }

    void Actualizar()
    {
        nivel = Mathf.Clamp(nivel, 0, Cantidades.Length - 1);
        float t = tiempoFijo >= 0f ? tiempoFijo : Time.time;
        float fase = Mathf.Floor(t * cambiosPorSegundo) % 1000f; // se repite cada rato (así no pierde precisión)
        Shader.SetGlobalFloat(idCantidad, Cantidades[nivel]);
        Shader.SetGlobalFloat(idFase, fase);
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat(idCantidad, 0f);
    }
}

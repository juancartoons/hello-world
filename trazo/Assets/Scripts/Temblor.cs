using UnityEngine;

// Líneas vivas ("line boil"), como en la animación dibujada a mano:
//  - Temblor: las líneas vibran un poquito (No, Suave, Medio, Fuerte).
//  - Hebras: cada línea se dibuja con 1, 3 o 5 hebras finas que se mueven por su cuenta.
//  - Grosor vivo: el grosor sube y baja a lo largo de la línea, como la presión de un pincel.
//  - Ciclo de 3: el temblor repite 3 "dibujos" (1, 2, 3, 1, 2, 3...) como en la animación tradicional.
// No cambia tus nodos: solo cambia cómo se ven (y sale en los videos).
public class Temblor : MonoBehaviour
{
    public static readonly string[] Nombres = { "No", "Suave", "Medio", "Fuerte" };
    static readonly float[] Cantidades = { 0f, 0.0015f, 0.003f, 0.006f }; // metros
    static readonly int[] OpcionesHebras = { 1, 3, 5 };

    static readonly int idCantidad = Shader.PropertyToID("_TrazoTemblor");
    static readonly int idFase = Shader.PropertyToID("_TrazoTemblorFase");
    static readonly int idHebras = Shader.PropertyToID("_TrazoHebras");
    static readonly int idGrosor = Shader.PropertyToID("_TrazoGrosorVivo");

    // Los videos ponen aquí el "tiempo" de cada cuadro (así el temblor va al ritmo del video). -1 = tiempo real.
    public static float tiempoFijo = -1f;

    [Tooltip("Cuántas veces por segundo cambia el temblor")]
    public float cambiosPorSegundo = 8f;
    public int nivel;
    public int hebras = 1;
    public bool grosorVivo;
    public bool ciclo3 = true;
    [Tooltip("Cuánto se separan las hebras (veces el grosor de la línea)")]
    public float separacionHebras = 1.2f;

    public string NombreNivel => Nombres[Mathf.Clamp(nivel, 0, Nombres.Length - 1)];

    Dibujo dibujo;

    void Awake()
    {
        dibujo = GetComponent<Dibujo>();
        Trazo.hebras = Mathf.Max(1, hebras);
    }

    void Mensaje(string texto)
    {
        if (dibujo != null)
            dibujo.Mensaje(texto);
    }

    public void Siguiente()
    {
        nivel = (nivel + 1) % Nombres.Length;
        Mensaje("Temblor de líneas: " + NombreNivel);
    }

    public void SiguienteHebras()
    {
        int i = System.Array.IndexOf(OpcionesHebras, hebras);
        PonerHebras(OpcionesHebras[(i + 1) % OpcionesHebras.Length]);
        Mensaje(hebras == 1 ? "Una sola línea" : hebras + " hebras por línea");
    }

    public void AlternarGrosor()
    {
        grosorVivo = !grosorVivo;
        Mensaje(grosorVivo ? "Grosor vivo" : "Grosor normal");
    }

    public void AlternarCiclo()
    {
        ciclo3 = !ciclo3;
        Mensaje(ciclo3 ? "Temblor en ciclo de 3 dibujos" : "Temblor libre");
    }

    // Cambia el número de hebras y vuelve a armar todas las líneas.
    public void PonerHebras(int n)
    {
        n = Mathf.Clamp(n, 1, 5);
        hebras = n;
        if (Trazo.hebras == n)
            return;
        Trazo.hebras = n;
        if (dibujo == null)
            return;
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        foreach (var t in dibujo.trazos)
            if (t != null)
                t.Reconstruir(true);
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
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
        if (Trazo.hebras != hebras)
            PonerHebras(hebras);
        float t = tiempoFijo >= 0f ? tiempoFijo : Time.time;
        float paso = Mathf.Floor(t * cambiosPorSegundo);
        // Ciclo de 3 dibujos que se repiten, o "libre" (siempre distinto).
        float fase = ciclo3 ? paso % 3f : paso % 1000f;
        bool vivo = nivel > 0;
        Shader.SetGlobalFloat(idCantidad, Cantidades[nivel]);
        Shader.SetGlobalFloat(idFase, vivo ? fase : 0f);
        Shader.SetGlobalFloat(idHebras, hebras > 1 ? separacionHebras : 0f);
        Shader.SetGlobalFloat(idGrosor, grosorVivo ? 1f : 0f);
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat(idCantidad, 0f);
        Shader.SetGlobalFloat(idHebras, 0f);
        Shader.SetGlobalFloat(idGrosor, 0f);
    }
}

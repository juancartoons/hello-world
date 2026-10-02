using UnityEngine;

// Líneas vivas ("line boil"), como en la animación dibujada a mano. Cada CAPA tiene las suyas
// (los botones de la página Medios cambian la capa activa):
//  - Temblor: No, Suave, Medio, Fuerte.
//  - Hebras: 1, 3 o 5 hebras finas por línea (en las puntas se juntan en una sola).
//  - Grosor vivo: el grosor sube y baja a lo largo de la línea, como la presión de un pincel.
//  - Ciclo de 3 / Libre: el temblor repite 3 "dibujos" o es siempre distinto.
//  - Suavidad: Suave, Normal, Nervioso (qué tan ondulado es el temblor).
//  - Velocidad: 4, 8, 12 o 24 cambios por segundo.
//  - Boceto: la capa se ve como lápiz gris o azul y NO sale en fotos ni videos.
// Nada de esto cambia tus nodos: solo cambia cómo se ven.
public class Temblor : MonoBehaviour
{
    public static readonly string[] Nombres = { "No", "Suave", "Medio", "Fuerte" };
    static readonly int[] OpcionesHebras = { 1, 3, 5 };
    static readonly string[] NombresBoceto = { "No", "Gris", "Azul" };

    static readonly int idTiempo = Shader.PropertyToID("_TrazoTiempo");

    // Los videos ponen aquí el "tiempo" de cada cuadro (así el temblor va al ritmo del video). -1 = tiempo real.
    public static float tiempoFijo = -1f;

    Dibujo dibujo;

    void Awake()
    {
        dibujo = GetComponent<Dibujo>();
    }

    DatosCapa Capa => dibujo != null ? dibujo.CapaActual : null;

    public string NombreNivel => Capa != null ? Nombres[Mathf.Clamp(Capa.temblor, 0, Nombres.Length - 1)] : Nombres[0];
    public int Hebras => Capa != null ? Mathf.Clamp(Capa.hebras, 1, 5) : 1;
    public bool GrosorVivo => Capa != null && Capa.grosorVivo;
    public bool Ciclo3 => Capa == null || Capa.ciclo3;
    public int Nivel => Capa != null ? Capa.temblor : 0;
    public string NombreSuavidad => Capa != null ? Dibujo.NombresSuavidad[Mathf.Clamp(Capa.suavidad, 0, 2)] : "Normal";
    public float CambiosPorSegundo => Capa != null ? Dibujo.Velocidades[Mathf.Clamp(Capa.velocidad, 0, Dibujo.Velocidades.Length - 1)] : 8f;
    public int Boceto => Capa != null ? Capa.boceto : 0;
    public string NombreBoceto => NombresBoceto[Mathf.Clamp(Boceto, 0, 2)];

    void Cambio(string mensaje)
    {
        if (dibujo == null)
            return;
        dibujo.RefrescarCapa(dibujo.capaActual);
        dibujo.Mensaje(dibujo.CapaActual.nombre + ": " + mensaje);
    }

    public void Siguiente()
    {
        if (Capa == null) return;
        Capa.temblor = (Capa.temblor + 1) % Nombres.Length;
        Cambio("temblor " + NombreNivel);
    }

    public void SiguienteHebras()
    {
        if (Capa == null) return;
        int i = System.Array.IndexOf(OpcionesHebras, Hebras);
        Capa.hebras = OpcionesHebras[(i + 1) % OpcionesHebras.Length];
        Cambio(Capa.hebras == 1 ? "una sola línea" : Capa.hebras + " hebras por línea");
    }

    public void AlternarGrosor()
    {
        if (Capa == null) return;
        Capa.grosorVivo = !Capa.grosorVivo;
        Cambio(Capa.grosorVivo ? "grosor vivo" : "grosor normal");
    }

    public void AlternarCiclo()
    {
        if (Capa == null) return;
        Capa.ciclo3 = !Capa.ciclo3;
        Cambio(Capa.ciclo3 ? "temblor en ciclo de 3 dibujos" : "temblor libre");
    }

    public void SiguienteSuavidad()
    {
        if (Capa == null) return;
        Capa.suavidad = (Mathf.Clamp(Capa.suavidad, 0, 2) + 1) % 3;
        Cambio("temblor " + NombreSuavidad);
    }

    public void SiguienteVelocidad()
    {
        if (Capa == null) return;
        Capa.velocidad = (Mathf.Clamp(Capa.velocidad, 0, Dibujo.Velocidades.Length - 1) + 1) % Dibujo.Velocidades.Length;
        Cambio(CambiosPorSegundo + " cambios por segundo");
    }

    public void SiguienteBoceto()
    {
        if (Capa == null) return;
        Capa.boceto = (Mathf.Clamp(Capa.boceto, 0, 2) + 1) % 3;
        // Al bocetar el imán estorba: se apaga solo (y se enciende al volver a tinta).
        Capa.iman = Capa.boceto == 0;
        Cambio(Capa.boceto == 0 ? "tinta (sale en fotos y videos)" : "boceto " + NombreBoceto.ToLower() + " (no sale en fotos ni videos)");
    }

    // Grosor de cada hebra (con el gesto de grosor y un pellizco derecho en el aire).
    public void PonerGrosorHebra(float valor)
    {
        if (Capa == null) return;
        valor = Mathf.Clamp(valor, 0.15f, 1f);
        if (Mathf.Abs(valor - Capa.grosorHebra) < 0.01f)
            return;
        Capa.grosorHebra = valor;
        dibujo.RefrescarCapa(dibujo.capaActual);
    }

    public float GrosorHebra => Capa != null ? Capa.grosorHebra : 0.5f;

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
        // Se repite cada 10 minutos (así no pierde precisión).
        float t = tiempoFijo >= 0f ? tiempoFijo : Time.time;
        Shader.SetGlobalFloat(idTiempo, Mathf.Repeat(t, 600f));
    }
}

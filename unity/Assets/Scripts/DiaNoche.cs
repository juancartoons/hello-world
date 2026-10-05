using UnityEngine;

// Cambia entre DÍA y NOCHE (con los botones del sol y la luna que aparecen afuera, antes de entrar).
// De noche: cielo con estrellas y luna, la calle se oscurece, se prenden las ventanas de los edificios,
// los postes de luz y los faros de los carros, y ya no entra sol por las ventanas.
// Adentro de la farmacia todo sigue igual de iluminado (las luces de la tienda están prendidas).
public class DiaNoche : MonoBehaviour
{
    [Tooltip("Empieza de noche")]
    public bool noche;

    [Header("Luz y cielo")]
    public Light sol;
    public Material cieloDia;
    public Material cieloNoche;
    public Color colorDia = new Color(1f, 0.96f, 0.88f);
    public float intensidadDia = 1.15f;
    [Tooltip("De noche la luz principal es la de la luna: azulada y suave")]
    public Color colorNoche = new Color(0.55f, 0.65f, 1f);
    public float intensidadNoche = 0.22f;

    [Header("Botones")]
    public Renderer botonDia;
    public Renderer botonNoche;
    public Color colorElegido = new Color(0.15f, 0.7f, 0.4f);
    public Color colorNormal = new Color(0.75f, 0.78f, 0.82f);

    static readonly int idNoche = Shader.PropertyToID("_FarmaciaNoche");
    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int idColor = Shader.PropertyToID("_Color");
    MaterialPropertyBlock bloque;

    void Start()
    {
        Aplicar(noche);
    }

    public void Dia() => Aplicar(false);
    public void Noche() => Aplicar(true);

    public void Aplicar(bool deNoche)
    {
        noche = deNoche;
        Shader.SetGlobalFloat(idNoche, deNoche ? 1f : 0f);
        if (sol != null)
        {
            sol.color = deNoche ? colorNoche : colorDia;
            sol.intensity = deNoche ? intensidadNoche : intensidadDia;
        }
        var cielo = deNoche ? cieloNoche : cieloDia;
        if (cielo != null)
            RenderSettings.skybox = cielo;

        foreach (var o in FindObjectsByType<SoloDeNoche>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            o.gameObject.SetActive(deNoche);
        foreach (var o in FindObjectsByType<SoloDeDia>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            o.gameObject.SetActive(!deNoche);

        Pintar(botonDia, !deNoche);
        Pintar(botonNoche, deNoche);
    }

    void Pintar(Renderer r, bool elegido)
    {
        if (r == null)
            return;
        if (bloque == null)
            bloque = new MaterialPropertyBlock();
        Color c = elegido ? colorElegido : colorNormal;
        r.GetPropertyBlock(bloque);
        bloque.SetColor(idBaseColor, c);
        bloque.SetColor(idColor, c);
        r.SetPropertyBlock(bloque);
    }

    void OnDestroy()
    {
        // Para que el editor no se quede "de noche" al salir del modo Play.
        Shader.SetGlobalFloat(idNoche, 0f);
    }
}

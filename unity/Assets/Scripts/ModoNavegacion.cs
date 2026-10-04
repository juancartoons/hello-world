using UnityEngine;

// Elige cómo se mueve el jugador adentro de la farmacia:
// - Puntos: señalar con el dedo los discos del piso.
// - Meta: arco curvo como el de Meta Quest (palma abajo + pellizco, soltar para ir), a cualquier parte del piso.
// - Caminar: movimiento continuo hacia donde señala el dedo índice, a paso normal.
// El arco original de Meta se apaga siempre (apuntaba mal y se veía encima del nuestro).
// Se elige con los botones que aparecen afuera, antes de entrar.
public class ModoNavegacion : MonoBehaviour
{
    public enum Modo { Puntos, Meta, Caminar }

    [Tooltip("Modo con el que arranca la app")]
    public Modo modo = Modo.Puntos;
    public TeletransportePorPuntos teletransportePuntos;
    [Tooltip("Contenedor de los discos del piso")]
    public GameObject discos;
    [Tooltip("Nuestro teletransporte con arco, estilo Meta")]
    public TeletransporteArco teletransporteArco;
    [Tooltip("Caminar de forma continua señalando con el dedo")]
    public CaminarConDedo caminar;
    [Tooltip("El teletransporte original de Meta (ISDK_TeleportInteraction): se mantiene apagado")]
    public GameObject teletransporteMeta;
    [Tooltip("Botones para resaltar el modo elegido")]
    public Renderer botonPuntos, botonMeta, botonCaminar;
    public Color colorElegido = new Color(0.15f, 0.7f, 0.4f);
    public Color colorNormal = new Color(0.75f, 0.78f, 0.82f);

    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int idColor = Shader.PropertyToID("_Color");
    MaterialPropertyBlock bloque;

    void Start()
    {
        ApagarArcoOriginalDeMeta();
        Aplicar(modo);
    }

    // Apaga los "lanzadores" del arco de teletransporte del kit de manos de Meta (el giro con el pulgar sigue funcionando).
    void ApagarArcoOriginalDeMeta()
    {
        foreach (var comp in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (comp != null && comp.GetType().Name == "TeleportInteractor")
                comp.gameObject.SetActive(false);
    }

    public void ElegirPuntos() => Aplicar(Modo.Puntos);
    public void ElegirMeta() => Aplicar(Modo.Meta);
    public void ElegirCaminar() => Aplicar(Modo.Caminar);

    public void Aplicar(Modo nuevo)
    {
        modo = nuevo;
        bool puntos = modo == Modo.Puntos;
        if (teletransportePuntos != null)
            teletransportePuntos.enabled = puntos;
        if (discos != null)
            foreach (Transform disco in discos.transform)
                disco.gameObject.SetActive(puntos);
        if (teletransporteArco != null)
            teletransporteArco.enabled = modo == Modo.Meta;
        if (caminar != null)
            caminar.enabled = modo == Modo.Caminar;
        if (teletransporteMeta != null)
            teletransporteMeta.SetActive(false);
        Pintar(botonPuntos, puntos);
        Pintar(botonMeta, modo == Modo.Meta);
        Pintar(botonCaminar, modo == Modo.Caminar);
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
}

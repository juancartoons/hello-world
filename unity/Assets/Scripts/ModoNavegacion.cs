using UnityEngine;

// Elige cómo se mueve el jugador adentro de la farmacia:
// - Puntos: señalar con el dedo los discos del piso (el sistema nuestro).
// - Meta: el teletransporte normal de Meta Quest (arco con la mano).
// Se elige con los botones que aparecen afuera, antes de entrar.
public class ModoNavegacion : MonoBehaviour
{
    public enum Modo { Puntos, Meta }

    [Tooltip("Modo con el que arranca la app")]
    public Modo modo = Modo.Puntos;
    public TeletransportePorPuntos teletransportePuntos;
    [Tooltip("Contenedor de los discos del piso")]
    public GameObject discos;
    [Tooltip("El teletransporte de Meta (ISDK_TeleportInteraction)")]
    public GameObject teletransporteMeta;
    [Tooltip("Botones para resaltar el modo elegido")]
    public Renderer botonPuntos, botonMeta;
    public Color colorElegido = new Color(0.15f, 0.7f, 0.4f);
    public Color colorNormal = new Color(0.75f, 0.78f, 0.82f);

    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int idColor = Shader.PropertyToID("_Color");
    MaterialPropertyBlock bloque;

    void Start()
    {
        Aplicar(modo);
    }

    public void ElegirPuntos() => Aplicar(Modo.Puntos);
    public void ElegirMeta() => Aplicar(Modo.Meta);

    public void Aplicar(Modo nuevo)
    {
        modo = nuevo;
        bool puntos = modo == Modo.Puntos;
        if (teletransportePuntos != null)
            teletransportePuntos.enabled = puntos;
        if (discos != null)
            foreach (Transform disco in discos.transform)
                disco.gameObject.SetActive(puntos);
        if (teletransporteMeta != null)
            teletransporteMeta.SetActive(!puntos);
        Pintar(botonPuntos, puntos);
        Pintar(botonMeta, !puntos);
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

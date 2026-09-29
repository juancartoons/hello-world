using UnityEngine;

// Va en cada disco del piso al que el jugador se puede teletransportar.
// Solo se encarga de cambiar de color cuando lo están apuntando.
public class PuntoTeletransporte : MonoBehaviour
{
    public Color colorNormal = new Color(0.2f, 0.8f, 1f);
    public Color colorResaltado = new Color(1f, 0.85f, 0.1f);

    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor"); // URP
    static readonly int idColor = Shader.PropertyToID("_Color");         // Built-in

    Renderer miRenderer;
    MaterialPropertyBlock bloque;

    void Awake()
    {
        miRenderer = GetComponentInChildren<Renderer>();
        bloque = new MaterialPropertyBlock();
        Resaltar(false);
    }

    public void Resaltar(bool resaltado)
    {
        if (miRenderer == null)
            return;
        Color c = resaltado ? colorResaltado : colorNormal;
        miRenderer.GetPropertyBlock(bloque);
        bloque.SetColor(idBaseColor, c);
        bloque.SetColor(idColor, c);
        miRenderer.SetPropertyBlock(bloque);
    }
}

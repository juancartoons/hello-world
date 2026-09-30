using UnityEngine;

// Va en cada disco del piso al que el jugador se puede teletransportar.
// Cambia de color y crece un poco mientras lo están apuntando (muestra el progreso).
public class PuntoTeletransporte : MonoBehaviour
{
    public Color colorNormal = new Color(0.2f, 0.8f, 1f);
    public Color colorResaltado = new Color(1f, 0.85f, 0.1f);
    [Tooltip("Cuánto crece al completar el apuntado (0.35 = 35%)")]
    public float crecimiento = 0.35f;

    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor"); // URP y shader toon
    static readonly int idColor = Shader.PropertyToID("_Color");         // Built-in

    Renderer miRenderer;
    MaterialPropertyBlock bloque;
    Vector3 escalaOriginal;

    void Awake()
    {
        miRenderer = GetComponentInChildren<Renderer>();
        bloque = new MaterialPropertyBlock();
        escalaOriginal = transform.localScale;
        Resaltar(0f);
    }

    public void Resaltar(bool resaltado)
    {
        Resaltar(resaltado ? 1f : 0f);
    }

    // progreso: 0 = normal, 1 = listo para teletransportar.
    public void Resaltar(float progreso)
    {
        progreso = Mathf.Clamp01(progreso);
        if (miRenderer != null)
        {
            Color c = progreso <= 0f ? colorNormal : Color.Lerp(colorNormal, colorResaltado, 0.4f + 0.6f * progreso);
            miRenderer.GetPropertyBlock(bloque);
            bloque.SetColor(idBaseColor, c);
            bloque.SetColor(idColor, c);
            miRenderer.SetPropertyBlock(bloque);
        }
        float s = 1f + crecimiento * progreso;
        transform.localScale = new Vector3(escalaOriginal.x * s, escalaOriginal.y, escalaOriginal.z * s);
    }
}

using System.Collections.Generic;
using UnityEngine;

// Al encontrar al pájaro, el aviso del premio tapa todo... menos al pájaro (y su halo): se dibujan después del aviso.
// Nada más cambia: el pájaro se sigue tapando con lo que de verdad esté delante de él (por ejemplo, tu otra mano).
// Con el botón secreto (círculo pequeño en la esquina izquierda de abajo del menú de afuera) se alterna:
// gris = versión nueva (pájaro encima del aviso), blanco = como antes. La opción se recuerda entre sesiones.
public class PajaroEncima : MonoBehaviour
{
    public Renderer boton;
    public Color colorApagado = Color.white;
    public Color colorEncendido = new Color(0.55f, 0.55f, 0.58f);
    [Tooltip("Orden de dibujo del pájaro: mayor que el del aviso del premio (el texto encima va en 4000)")]
    public int colaPajaro = 4100;

    const string clave = "FarmaciaVR_PajaroEncima";
    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int idColor = Shader.PropertyToID("_Color");

    public bool Activado { get; private set; }

    readonly List<Renderer> renders = new List<Renderer>();
    readonly List<Material[]> originales = new List<Material[]>();
    readonly Dictionary<Material, Material> copias = new Dictionary<Material, Material>();
    PersonajeEncontrable personaje;
    MaterialPropertyBlock bloque;

    void Start()
    {
        Activado = PlayerPrefs.GetInt(clave, 1) == 1;
        Pintar();
    }

    // Lo llama el botón secreto.
    public void Alternar()
    {
        Activado = !Activado;
        PlayerPrefs.SetInt(clave, Activado ? 1 : 0);
        PlayerPrefs.Save();
        if (!Activado)
            Subir(personaje, false);
        Pintar();
    }

    // El JuegoManager lo llama al encontrar al pájaro (encima = true) y al volver a empezar (encima = false).
    public void Subir(PersonajeEncontrable p, bool encima)
    {
        Restaurar();
        personaje = p;
        if (!encima || !Activado || p == null)
            return;

        foreach (var r in p.GetComponentsInChildren<Renderer>())
            Cambiar(r, colaPajaro);
        // El halo va después del pájaro (como antes: luz transparente sobre el pájaro ya dibujado).
        var halo = p.GetComponent<HaloPajaro>();
        if (halo != null && halo.Render != null)
            Cambiar(halo.Render, colaPajaro + 1);
    }

    void Cambiar(Renderer r, int cola)
    {
        var mats = r.sharedMaterials;
        renders.Add(r);
        originales.Add(mats);
        var nuevos = new Material[mats.Length];
        for (int i = 0; i < mats.Length; i++)
            nuevos[i] = Copia(mats[i], cola);
        r.sharedMaterials = nuevos;
    }

    // Una copia del material (el original lo comparten los peluches) que se dibuja después del aviso.
    Material Copia(Material m, int cola)
    {
        if (m == null)
            return null;
        if (!copias.TryGetValue(m, out var copia))
        {
            copia = new Material(m) { name = m.name + "_Encima" };
            copias[m] = copia;
        }
        copia.renderQueue = cola;
        return copia;
    }

    void Restaurar()
    {
        for (int i = 0; i < renders.Count; i++)
            if (renders[i] != null)
                renders[i].sharedMaterials = originales[i];
        renders.Clear();
        originales.Clear();
    }

    void Pintar()
    {
        if (boton == null)
            return;
        if (bloque == null)
            bloque = new MaterialPropertyBlock();
        Color c = Activado ? colorEncendido : colorApagado;
        boton.GetPropertyBlock(bloque);
        bloque.SetColor(idBaseColor, c);
        bloque.SetColor(idColor, c);
        boton.SetPropertyBlock(bloque);
    }

    void OnDestroy()
    {
        Restaurar();
        foreach (var copia in copias.Values)
            if (copia != null)
                Destroy(copia);
    }
}

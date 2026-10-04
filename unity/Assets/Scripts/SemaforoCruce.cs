using UnityEngine;

// Semáforos del cruce entre la calle y la carrera: verde → amarillo → rojo, por turnos.
// Los carros (CarroEnRuta) le preguntan si pueden pasar.
public class SemaforoCruce : MonoBehaviour
{
    public enum Luz { Verde, Amarillo, Rojo }

    [Header("Tiempos (segundos)")]
    public float verdeCalle = 14f;
    public float verdeCarrera = 10f;
    public float amarillo = 3f;
    [Tooltip("Tiempo con los dos en rojo, para que el cruce quede vacío")]
    public float todoRojo = 2.5f;

    [Header("Bombillos (de a 3 por semáforo: rojo, amarillo, verde)")]
    public Renderer[] bombillosCalle;
    public Renderer[] bombillosCarrera;

    static SemaforoCruce actual;
    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly Color[] encendido = { new Color(1f, 0.15f, 0.1f), new Color(1f, 0.75f, 0.1f), new Color(0.2f, 1f, 0.4f) };
    static readonly Color[] apagado = { new Color(0.25f, 0.06f, 0.05f), new Color(0.25f, 0.2f, 0.05f), new Color(0.05f, 0.22f, 0.1f) };

    float tiempo;
    Luz pintadaCalle = (Luz)(-1), pintadaCarrera = (Luz)(-1);
    MaterialPropertyBlock bloque;

    void OnEnable() => actual = this;

    void OnDisable()
    {
        if (actual == this)
            actual = null;
    }

    // Si no hay semáforo en la escena, siempre está en verde.
    public static Luz Estado(int grupo) => actual == null ? Luz.Verde : actual.Calcular(grupo);

    void Update()
    {
        tiempo += Time.deltaTime;
        var calle = Calcular(0);
        var carrera = Calcular(1);
        if (calle != pintadaCalle) { Pintar(bombillosCalle, calle); pintadaCalle = calle; }
        if (carrera != pintadaCarrera) { Pintar(bombillosCarrera, carrera); pintadaCarrera = carrera; }
    }

    Luz Calcular(int grupo)
    {
        float ciclo = verdeCalle + verdeCarrera + 2f * (amarillo + todoRojo);
        float t = tiempo % ciclo;
        float inicio = grupo == 0 ? 0f : verdeCalle + amarillo + todoRojo;
        float verde = grupo == 0 ? verdeCalle : verdeCarrera;
        float x = t - inicio;
        if (x >= 0f && x < verde) return Luz.Verde;
        if (x >= verde && x < verde + amarillo) return Luz.Amarillo;
        return Luz.Rojo;
    }

    void Pintar(Renderer[] bombillos, Luz luz)
    {
        if (bombillos == null)
            return;
        if (bloque == null)
            bloque = new MaterialPropertyBlock();
        int prendido = luz == Luz.Rojo ? 0 : luz == Luz.Amarillo ? 1 : 2;
        for (int i = 0; i < bombillos.Length; i++)
        {
            if (bombillos[i] == null)
                continue;
            int cual = i % 3;
            bombillos[i].GetPropertyBlock(bloque);
            bloque.SetColor(idBaseColor, cual == prendido ? encendido[cual] : apagado[cual]);
            bombillos[i].SetPropertyBlock(bloque);
        }
    }
}

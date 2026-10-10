using System.Reflection;
using UnityEngine;

// MODO RENDIMIENTO automático (sin perder lo bonito): mide los cuadros por segundo y, si bajan (el visor
// empieza a marear), primero le pide al Quest más potencia y después ahorra solo donde no se nota:
//  1) Procesador y gráficos en "alto sostenido" (gasta un poco más de batería, no cambia nada de lo que ves).
//  2) Foveación dinámica: el borde de la vista (donde casi no se ve) con menos detalle; el centro queda igual.
// Nunca baja la resolución, ni apaga el halo, el temblor o el relleno vivo.
// Se crea solo al abrir la app. Los ajustes del Meta XR SDK se ponen "por nombre": si una versión del SDK
// no los tiene, simplemente no hace nada (no rompe el Build).
public class Rendimiento : MonoBehaviour
{
    public static Rendimiento Instancia { get; private set; }

    const float Objetivo = 72f;        // cuadros por segundo del Quest 2 (mínimo cómodo)
    const float Margen = 4f;           // por debajo de 68: hay que ayudar
    const float Ventana = 2f;          // segundos que se promedian
    const int NivelMaximo = 2;

    public float Fps { get; private set; } = Objetivo;
    public int Nivel { get; private set; }

    float suma;
    int cuadros;
    float desde;
    int ventanasLentas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (Instancia != null)
            return;
        var go = new GameObject("Rendimiento");
        DontDestroyOnLoad(go);
        Instancia = go.AddComponent<Rendimiento>();
    }

    void Update()
    {
        suma += Time.unscaledDeltaTime;
        cuadros++;
        if (Time.unscaledTime - desde < Ventana)
            return;
        Fps = suma > 0f ? cuadros / suma : Objetivo;
        suma = 0f;
        cuadros = 0;
        desde = Time.unscaledTime;
        // Al exportar un video los cuadros van lentos a propósito: no cuenta.
        if (ExportadorVideo.Exportando)
        {
            ventanasLentas = 0;
            return;
        }
        ventanasLentas = Fps < Objetivo - Margen ? ventanasLentas + 1 : 0;
        // Dos ventanas seguidas lentas (unos 4 segundos): un paso más de ayuda.
        if (ventanasLentas >= 2 && Nivel < NivelMaximo)
        {
            ventanasLentas = 0;
            Subir();
        }
    }

    void Subir()
    {
        Nivel++;
        bool hecho = false;
        if (Nivel == 1)
        {
            hecho |= PonerEnum("suggestedCpuPerfLevel", "SustainedHigh");
            hecho |= PonerEnum("suggestedGpuPerfLevel", "SustainedHigh");
        }
        else if (Nivel == 2)
        {
            hecho |= PonerBool("useDynamicFoveatedRendering", true);
            hecho |= PonerEnum("foveatedRenderingLevel", "High");
        }
        Debug.Log("TrazoVR: modo rendimiento nivel " + Nivel + " (" + Mathf.RoundToInt(Fps) + " fps)" + (hecho ? "" : " — el SDK no lo tiene"));
        var dibujo = FindFirstObjectByType<Dibujo>();
        if (dibujo != null && hecho && Nivel == 1)
            dibujo.Mensaje("Modo rendimiento: más potencia para que todo vaya fluido");
    }

    // Una propiedad estática de OVRManager (del Meta XR SDK) con un valor de su enum, por nombre.
    static bool PonerEnum(string propiedad, string valor)
    {
        try
        {
            var p = typeof(OVRManager).GetProperty(propiedad, BindingFlags.Public | BindingFlags.Static);
            if (p == null || !p.CanWrite || !p.PropertyType.IsEnum)
                return false;
            p.SetValue(null, System.Enum.Parse(p.PropertyType, valor));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: modo rendimiento (" + propiedad + "): " + e.Message);
            return false;
        }
    }

    static bool PonerBool(string propiedad, bool valor)
    {
        try
        {
            var p = typeof(OVRManager).GetProperty(propiedad, BindingFlags.Public | BindingFlags.Static);
            if (p == null || !p.CanWrite || p.PropertyType != typeof(bool))
                return false;
            p.SetValue(null, valor);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: modo rendimiento (" + propiedad + "): " + e.Message);
            return false;
        }
    }
}

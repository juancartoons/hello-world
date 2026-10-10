using UnityEngine;

// Copia videos, fotos y SVG a una carpeta pública del Quest (plugin TrazoGaleria.java):
//   todo → Download/JCartoons (en la app "Archivos" del visor: Descargas → JCartoons).
// Devuelve la ruta pública, o "" si no se pudo.
public static class Galeria
{
    public const string Carpeta = "Download/JCartoons";

    // Lo último que se guardó (se muestra en la página Medios para que no se pierda el aviso).
    public static string UltimoGuardado = "";

    public static string Publicar(string ruta, string mime, string carpeta)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var actividad = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var galeria = new AndroidJavaClass("com.trazovr.TrazoGaleria"))
            {
                string r = galeria.CallStatic<string>("publicar", actividad, ruta, mime, carpeta);
                return r ?? "";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo copiar a la galería: " + e.Message);
        }
#endif
        return "";
    }

    // Texto para el usuario: dónde encontrar el archivo.
    public static string Donde(string publico, string privado)
    {
        if (!string.IsNullOrEmpty(publico))
            return "app Archivos del Quest → " + (publico.StartsWith("Download/") ? "Descargas/" + publico.Substring(9) : publico).Replace("/", " → ");
        return "carpeta Dibujos de la app (con el cable: Android/data/<la app>/files/Dibujos/" + System.IO.Path.GetFileName(privado) + ")";
    }
}

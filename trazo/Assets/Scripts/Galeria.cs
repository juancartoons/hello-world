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

    // La carpeta pública de verdad (Descargas/JCartoons), para listar y abrir lo que la app ya publicó.
    // En el editor de Unity no hay: "".
    static string carpetaPublica;
    public static string CarpetaPublica
    {
        get
        {
            if (carpetaPublica != null)
                return carpetaPublica;
            carpetaPublica = "";
#if UNITY_ANDROID && !UNITY_EDITOR
            string descargas = Descargas();
            if (!string.IsNullOrEmpty(descargas))
                carpetaPublica = System.IO.Path.Combine(descargas, "JCartoons");
#endif
            return carpetaPublica;
        }
    }

    // La carpeta Descargas del Quest (ruta completa), o "" si no se pudo saber.
    public static string Descargas()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var entorno = new AndroidJavaClass("android.os.Environment"))
            using (var dir = entorno.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", "Download"))
                return dir != null ? dir.Call<string>("getAbsolutePath") ?? "" : "";
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no encontré la carpeta Descargas: " + e.Message);
        }
#endif
        return "";
    }

    // Si la copia pública quedó bien, se borra la de la app (para no gastar espacio dos veces).
    public static void BorrarPrivadaSiPublicada(string privado, string publico)
    {
        if (string.IsNullOrEmpty(privado) || string.IsNullOrEmpty(publico) || string.IsNullOrEmpty(CarpetaPublica))
            return;
        try
        {
            string real = System.IO.Path.Combine(CarpetaPublica, System.IO.Path.GetFileName(publico));
            if (System.IO.File.Exists(real) && System.IO.File.Exists(privado))
                System.IO.File.Delete(privado);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo borrar la copia de la app: " + e.Message);
        }
    }

    // Texto para el usuario: dónde encontrar el archivo.
    public static string Donde(string publico, string privado)
    {
        if (!string.IsNullOrEmpty(publico))
            return "app Archivos del Quest → " + (publico.StartsWith("Download/") ? "Descargas/" + publico.Substring(9) : publico).Replace("/", " → ");
        return "carpeta Dibujos de la app (con el cable: Android/data/<la app>/files/Dibujos/" + System.IO.Path.GetFileName(privado) + ")";
    }
}

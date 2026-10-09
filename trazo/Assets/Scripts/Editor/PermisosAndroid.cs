#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;
using Debug = UnityEngine.Debug;

// Al hacer el Build para el Quest, agrega solo al manifiesto el permiso de LEER FOTOS del visor
// (para "Imagen +": buscar imágenes en Descargas, Cámara, WhatsApp, Facebook...). No hay que hacer nada a mano.
public class PermisosAndroid : IPostGenerateGradleAndroidProject
{
    const string Android = "http://schemas.android.com/apk/res/android";

    static readonly string[] Permisos =
    {
        "android.permission.READ_MEDIA_IMAGES",     // Android 13 o más nuevo
        "android.permission.READ_EXTERNAL_STORAGE", // Android 12 o más viejo
    };

    public int callbackOrder => 100;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string ruta = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(ruta))
        {
            Debug.LogWarning("TrazoVR: no encontré el AndroidManifest.xml para agregar el permiso de fotos: " + ruta);
            return;
        }
        try
        {
            var doc = new XmlDocument();
            doc.Load(ruta);
            var manifiesto = doc.DocumentElement;
            if (manifiesto == null)
                return;
            bool cambio = false;
            foreach (var permiso in Permisos)
            {
                bool ya = false;
                foreach (XmlNode n in manifiesto.ChildNodes)
                {
                    var e = n as XmlElement;
                    if (e != null && e.Name == "uses-permission" && e.GetAttribute("name", Android) == permiso)
                    {
                        ya = true;
                        break;
                    }
                }
                if (ya)
                    continue;
                var nuevo = doc.CreateElement("uses-permission");
                var nombre = doc.CreateAttribute("android", "name", Android);
                nombre.Value = permiso;
                nuevo.Attributes.Append(nombre);
                manifiesto.PrependChild(nuevo);
                cambio = true;
            }
            if (cambio)
                doc.Save(ruta);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no pude agregar el permiso de fotos al manifiesto: " + e.Message);
        }
    }
}
#endif

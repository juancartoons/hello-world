#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

// Menú: TrazoVR > Voz: incluir los idiomas en la app.
// Baja UNA SOLA VEZ los dos modelos de la voz (español e inglés, unos 80 MB) a Assets/StreamingAssets.
// Así quedan DENTRO del .apk: la primera vez que alguien prende la voz, el visor los saca de la app (sin internet,
// en unos segundos). Si no están, la app los baja de internet como antes (TrazoVoz.java).
// El menú "Meta Store: armar APK para subir" también lo ofrece si faltan.
public static class IncluirVoz
{
    // Los mismos que usa Voz.cs (el nombre del archivo es lo que busca TrazoVoz.java dentro de la app).
    static readonly string[] Urls =
    {
        "https://alphacephei.com/vosk/models/vosk-model-small-es-0.42.zip",
        "https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip",
    };

    static string Carpeta => Path.Combine(Application.dataPath, "StreamingAssets");

    static string Ruta(string url) => Path.Combine(Carpeta, url.Substring(url.LastIndexOf('/') + 1));

    public static bool Listos()
    {
        foreach (var u in Urls)
        {
            var f = new FileInfo(Ruta(u));
            if (!f.Exists || f.Length < 1000000)
                return false;
        }
        return true;
    }

    [MenuItem("TrazoVR/Voz: incluir los idiomas en la app")]
    static void Menu()
    {
        if (Listos())
        {
            EditorUtility.DisplayDialog("Voz", "Los dos idiomas ya están dentro de la app (Assets/StreamingAssets).", "OK");
            return;
        }
        if (Bajar())
            EditorUtility.DisplayDialog("Voz", "¡Listo! Los dos idiomas de la voz van dentro de la app.\n\nArma el .apk otra vez (Meta Store o Build And Run).", "OK");
    }

    // Baja los que falten. false = no se pudo (o lo cancelaste).
    public static bool Bajar()
    {
        Directory.CreateDirectory(Carpeta);
        try
        {
            for (int i = 0; i < Urls.Length; i++)
            {
                string url = Urls[i];
                string ruta = Ruta(url);
                var f = new FileInfo(ruta);
                if (f.Exists && f.Length >= 1000000)
                    continue;
                string temporal = ruta + ".bajando";
                if (File.Exists(temporal))
                    File.Delete(temporal);
                using (var pedido = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
                {
                    pedido.downloadHandler = new DownloadHandlerFile(temporal);
                    var op = pedido.SendWebRequest();
                    while (!op.isDone)
                    {
                        string texto = "Idioma " + (i + 1) + " de " + Urls.Length + ": " + Mathf.RoundToInt(pedido.downloadProgress * 100f) + " %";
                        if (EditorUtility.DisplayCancelableProgressBar("Voz: bajando los idiomas (solo esta vez)", texto, pedido.downloadProgress))
                        {
                            pedido.Abort();
                            EditorUtility.ClearProgressBar();
                            if (File.Exists(temporal))
                                File.Delete(temporal);
                            return false;
                        }
                        System.Threading.Thread.Sleep(50);
                    }
                    if (pedido.result != UnityWebRequest.Result.Success)
                    {
                        EditorUtility.ClearProgressBar();
                        if (File.Exists(temporal))
                            File.Delete(temporal);
                        EditorUtility.DisplayDialog("Voz", "No se pudo bajar el idioma:\n" + url + "\n\n" + pedido.error + "\n\nRevisa tu internet e inténtalo otra vez.", "OK");
                        return false;
                    }
                }
                var bajado = new FileInfo(temporal);
                if (!bajado.Exists || bajado.Length < 1000000)
                {
                    EditorUtility.ClearProgressBar();
                    if (bajado.Exists)
                        bajado.Delete();
                    EditorUtility.DisplayDialog("Voz", "El archivo bajado está incompleto:\n" + url + "\n\nInténtalo otra vez.", "OK");
                    return false;
                }
                if (File.Exists(ruta))
                    File.Delete(ruta);
                File.Move(temporal, ruta);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        AssetDatabase.Refresh();
        Debug.Log("JCartoons: los idiomas de la voz están en Assets/StreamingAssets (van dentro del .apk).");
        return true;
    }
}
#endif

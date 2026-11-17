#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Menú: TrazoVR > Meta Store: armar APK para subir.
// Con un clic deja el .apk listo para subirlo al Developer Dashboard de Meta (canal ALPHA):
//  - Nombre del paquete propio (com.juancartoons.jcartoons, si aún tenía el de Unity por defecto).
//  - Número de versión que sube solo en cada armado (Meta no acepta dos veces el mismo).
//  - ARM64 + IL2CPP, sin "Development Build", .apk (no .aab).
//  - Tu propia FIRMA (keystore): la crea la primera vez en la carpeta "FirmaMeta" (al lado de Assets) junto con
//    su clave en un .txt. ¡GUÁRDALA! Sin ese archivo no se pueden subir actualizaciones de la app.
//  - Arma el .apk en la carpeta "Builds" y la abre.
public static class PrepararMetaStore
{
    const string PaquetePorDefecto = "com.juancartoons.jcartoons";
    const string Alias = "jcartoons";

    [MenuItem("TrazoVR/Meta Store: armar APK para subir")]
    static void Armar()
    {
        if (!EditorUtility.DisplayDialog("Meta Store",
                "Se va a armar el .apk para subirlo a Meta (tarda como un Build normal).\n\n" +
                "La primera vez se crea tu FIRMA en la carpeta FirmaMeta. Guárdala bien: sin ella no podrás actualizar la app.",
                "Armar", "Cancelar"))
            return;

        // 1) Android.
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        // 2) Nombre del paquete (solo si era el de Unity por defecto).
        string paquete = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        if (string.IsNullOrEmpty(paquete) || paquete.Contains("DefaultCompany") || paquete.StartsWith("com.unity"))
        {
            paquete = PaquetePorDefecto;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, paquete);
        }

        // 3) Versión (sube 1 cada vez).
        int codigo = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode + 1);
        PlayerSettings.Android.bundleVersionCode = codigo;
        PlayerSettings.bundleVersion = "1.0." + codigo;

        // 4) Lo que pide la tienda de Quest.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        if ((int)PlayerSettings.Android.minSdkVersion < 29)
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.development = false;

        // 5) La firma.
        string raiz = Directory.GetParent(Application.dataPath).FullName;
        string carpetaFirma = Path.Combine(raiz, "FirmaMeta");
        string rutaFirma = Path.Combine(carpetaFirma, "jcartoons.keystore");
        string rutaClave = Path.Combine(carpetaFirma, "CLAVE-no-compartir.txt");
        string clave;
        if (!File.Exists(rutaFirma))
        {
            Directory.CreateDirectory(carpetaFirma);
            clave = ClaveNueva();
            if (!CrearFirma(rutaFirma, clave))
                return;
            File.WriteAllText(rutaClave,
                clave + "\n\nEsta es la clave de jcartoons.keystore (alias: " + Alias + ").\n" +
                "Guarda ESTA CARPETA en un lugar seguro (por ejemplo, una USB o tu nube). No la compartas.\n" +
                "Sin ella no se pueden subir actualizaciones de JCartoons a Meta.\n");
        }
        else
        {
            if (!File.Exists(rutaClave))
            {
                EditorUtility.DisplayDialog("Meta Store", "Falta el archivo de la clave:\n" + rutaClave, "OK");
                return;
            }
            clave = File.ReadAllLines(rutaClave)[0].Trim();
        }
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = rutaFirma;
        PlayerSettings.Android.keystorePass = clave;
        PlayerSettings.Android.keyaliasName = Alias;
        PlayerSettings.Android.keyaliasPass = clave;
        AssetDatabase.SaveAssets();

        // 6) Las escenas (las de Build Settings; si no hay, la abierta).
        EditorSceneManager.SaveOpenScenes();
        var escenas = new List<string>();
        foreach (var e in EditorBuildSettings.scenes)
            if (e.enabled && !string.IsNullOrEmpty(e.path))
                escenas.Add(e.path);
        if (escenas.Count == 0)
        {
            string activa = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(activa))
            {
                EditorUtility.DisplayDialog("Meta Store", "Guarda la escena primero (Ctrl+S) y vuelve a intentarlo.", "OK");
                return;
            }
            escenas.Add(activa);
        }

        // 7) ¡A armar!
        string carpetaBuilds = Path.Combine(raiz, "Builds");
        Directory.CreateDirectory(carpetaBuilds);
        string rutaApk = Path.Combine(carpetaBuilds, "JCartoons-v" + codigo + ".apk");
        var opciones = new BuildPlayerOptions
        {
            scenes = escenas.ToArray(),
            locationPathName = rutaApk,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        if (reporte.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("JCartoons: APK listo para Meta: " + rutaApk + " (paquete " + paquete + ", versión " + codigo + ")");
            EditorUtility.RevealInFinder(rutaApk);
            EditorUtility.DisplayDialog("Meta Store",
                "¡Listo!\n\n" + Path.GetFileName(rutaApk) + "\nPaquete: " + paquete + "\nVersión: " + codigo +
                "\n\nSúbelo al canal ALPHA (Meta Quest Developer Hub o el Developer Dashboard).", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Meta Store",
                "El armado no terminó (" + reporte.summary.result + "). Mira la Console (en rojo) y mándale una captura a Claude.", "OK");
        }
    }

    static string ClaveNueva()
    {
        const string letras = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var r = new System.Random();
        var c = new char[20];
        for (int i = 0; i < c.Length; i++)
            c[i] = letras[r.Next(letras.Length)];
        return new string(c);
    }

    // Crea la firma con el keytool que trae Unity (el Java de Android).
    static bool CrearFirma(string ruta, string clave)
    {
        string keytool = BuscarKeytool();
        if (keytool == null)
        {
            EditorUtility.DisplayDialog("Meta Store",
                "No encontré el keytool de Unity (Java de Android). Revisa que Unity tenga instalado el módulo Android con OpenJDK.", "OK");
            return false;
        }
        var info = new System.Diagnostics.ProcessStartInfo
        {
            FileName = keytool,
            Arguments = "-genkeypair -keystore \"" + ruta + "\" -alias " + Alias + " -keyalg RSA -keysize 2048 -validity 10000" +
                        " -storepass " + clave + " -keypass " + clave + " -dname \"CN=JCartoons, O=JCartoons\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        try
        {
            using (var p = System.Diagnostics.Process.Start(info))
            {
                string error = p.StandardError.ReadToEnd();
                p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0 || !File.Exists(ruta))
                {
                    EditorUtility.DisplayDialog("Meta Store", "No se pudo crear la firma:\n" + error, "OK");
                    return false;
                }
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Meta Store", "No se pudo crear la firma:\n" + e.Message, "OK");
            return false;
        }
        return true;
    }

    static string BuscarKeytool()
    {
        string exe = Application.platform == RuntimePlatform.WindowsEditor ? "keytool.exe" : "keytool";
        var candidatas = new List<string>();
        // El Java que viene con el módulo Android de Unity.
        candidatas.Add(Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "OpenJDK", "bin", exe));
        string editor = Path.GetDirectoryName(EditorApplication.applicationPath);
        if (!string.IsNullOrEmpty(editor))
            candidatas.Add(Path.Combine(editor, "Data", "PlaybackEngines", "AndroidPlayer", "OpenJDK", "bin", exe));
        // El que esté configurado en Preferences > External Tools (si se puede leer).
        try
        {
            var tipo = System.Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions");
            var prop = tipo != null ? tipo.GetProperty("jdkRootPath") : null;
            string jdk = prop != null ? prop.GetValue(null, null) as string : null;
            if (!string.IsNullOrEmpty(jdk))
                candidatas.Insert(0, Path.Combine(jdk, "bin", exe));
        }
        catch (System.Exception)
        {
        }
        foreach (var c in candidatas)
            if (File.Exists(c))
                return c;
        return null;
    }
}
#endif

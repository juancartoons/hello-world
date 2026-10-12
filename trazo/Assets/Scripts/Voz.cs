using System.IO;
using System.Text;
#pragma warning disable 0414 // en el editor algunas cosas solo se usan en el visor
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

// LA VOZ de JCartoons (v40). Se prende y se apaga con el botón del micrófono (a la izquierda, en espejo del
// parlante). Funciona AL MISMO TIEMPO que los gestos de las manos: no se excluyen.
//  - Usa Vosk (gratis, de código abierto): el visor entiende tus palabras SIN internet. Solo la primera vez baja
//    el "modelo" del idioma de la app (español o inglés, unos 40 MB) y lo guarda en el visor.
//  - Solo escucha las frases de los comandos (ver ControlManosVoz.cs): así es rápido y casi no se equivoca.
//  - El puente con el visor es TrazoVoz.java (Plugins/Android) con las librerías vosk-android y jna (.aar).
// En el editor de Unity no hay micrófono de Vosk: el botón avisa y no hace nada más.
public class Voz : MonoBehaviour
{
    const string ClaveActiva = "jcartoons_voz";
    const string UrlEspanol = "https://alphacephei.com/vosk/models/vosk-model-small-es-0.42.zip";
    const string UrlIngles = "https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip";

    public static Voz Instancia { get; private set; }

    // ¿El botón del micrófono está prendido? (Aunque todavía se esté preparando.)
    public static bool Activa => Instancia != null && Instancia.quiere;
    // ¿Ya está escuchando de verdad?
    public static bool Escuchando => Instancia != null && Instancia.estado == "escuchando";
    // Para el panel de la izquierda: "", o lo que está pasando ("Bajando la voz 45 %", "Preparando la voz…").
    public static string Situacion => Instancia != null ? Instancia.situacion : "";
    // Cuándo entendió algo por última vez (para que el botón dé un saltito).
    public static float UltimoOido { get; private set; } = -10f;

    bool quiere;
    string estado = "apagada";
    string situacion = "";
    float proximaConsulta;
    bool esperandoPermiso, preparando, idiomaIngles;
    volatile bool permisoSi, permisoNo;
#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaClass java;
#endif

    public static void Crear()
    {
        if (Instancia != null)
            return;
        var go = new GameObject("Voz");
        Instancia = go.AddComponent<Voz>();
    }

    void Start()
    {
        idiomaIngles = Idioma.Ingles;
        // Si la dejaste prendida, se vuelve a prender sola.
        if (PlayerPrefs.GetInt(ClaveActiva, 0) == 1)
            Prender();
    }

    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
        Parar();
    }

    public void Alternar()
    {
        if (quiere)
            Apagar();
        else
            Prender();
        PlayerPrefs.SetInt(ClaveActiva, quiere ? 1 : 0);
        PlayerPrefs.Save();
    }

    void Prender()
    {
        quiere = true;
        situacion = "Preparando la voz…";
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            esperandoPermiso = true;
            permisoSi = permisoNo = false;
            var respuestas = new PermissionCallbacks();
            respuestas.PermissionGranted += _ => permisoSi = true;
            respuestas.PermissionDenied += _ => permisoNo = true;
            respuestas.PermissionDeniedAndDontAskAgain += _ => permisoNo = true;
            Permission.RequestUserPermission(Permission.Microphone, respuestas);
            situacion = "Permite el micrófono";
            return;
        }
        Preparar();
#else
        Avisar("La voz funciona en el visor (no en el editor)");
        situacion = "La voz funciona en el visor";
#endif
    }

    void Apagar()
    {
        quiere = false;
        esperandoPermiso = false;
        situacion = "";
        Parar();
        Avisar("Voz apagada");
    }

    void Parar()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (java != null)
                java.CallStatic("parar");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR voz: " + e.Message);
        }
#endif
        if (estado == "escuchando")
            estado = "lista";
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void Preparar()
    {
        try
        {
            if (java == null)
                java = new AndroidJavaClass("com.trazovr.TrazoVoz");
            // (Microphone se nombra aquí para que Unity ponga el permiso del micrófono en el visor.)
            if (Microphone.devices == null)
                Debug.Log("TrazoVR voz: sin lista de micrófonos");
            idiomaIngles = Idioma.Ingles;
            string carpeta = Path.Combine(Application.persistentDataPath, "voz", idiomaIngles ? "en" : "es");
            string url = idiomaIngles ? UrlIngles : UrlEspanol;
            java.CallStatic("preparar", url, carpeta, Gramatica());
            preparando = true;
            proximaConsulta = 0f;
        }
        catch (System.Exception e)
        {
            Fallo(e.Message);
        }
    }
#endif

    // Las frases que se escuchan, en el formato de Vosk: ["dibuja", "gira izquierda", ..., "[unk]"].
    static string Gramatica()
    {
        var sb = new StringBuilder("[");
        foreach (var f in ControlManos.FrasesDeVoz(Idioma.Ingles))
        {
            sb.Append('"');
            sb.Append(f.Replace("\"", ""));
            sb.Append("\", ");
        }
        sb.Append("\"[unk]\"]");
        return sb.ToString();
    }

    void Update()
    {
        if (!quiere)
            return;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (esperandoPermiso)
        {
            if (permisoNo)
            {
                esperandoPermiso = false;
                quiere = false;
                situacion = "";
                PlayerPrefs.SetInt(ClaveActiva, 0);
                Avisar("Sin permiso del micrófono no hay voz");
            }
            else if (permisoSi || Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                esperandoPermiso = false;
                Preparar();
            }
            return;
        }
        // Si cambias el idioma de la app, la voz cambia de idioma.
        if (idiomaIngles != Idioma.Ingles && !preparando)
        {
            Parar();
            Preparar();
            return;
        }
        if (java == null || Time.unscaledTime < proximaConsulta)
        {
            LeerLoOido();
            return;
        }
        proximaConsulta = Time.unscaledTime + 0.2f;
        string e;
        try
        {
            e = java.CallStatic<string>("estado");
        }
        catch (System.Exception ex)
        {
            Fallo(ex.Message);
            return;
        }
        estado = e ?? "";
        if (estado.StartsWith("bajando"))
        {
            int p = java.CallStatic<int>("progreso");
            situacion = "Bajando la voz (solo la primera vez) " + p + " %";
        }
        else if (estado.StartsWith("descomprimiendo") || estado.StartsWith("cargando"))
        {
            situacion = "Preparando la voz…";
        }
        else if (estado == "lista")
        {
            preparando = false;
            bool ok = java.CallStatic<bool>("escuchar");
            if (!ok)
            {
                estado = java.CallStatic<string>("estado") ?? "";
                if (estado.StartsWith("error"))
                    Fallo(estado.Substring(Mathf.Min(estado.Length, 7)));
                return;
            }
            estado = "escuchando";
            situacion = "";
            Avisar("Te escucho");
        }
        else if (estado.StartsWith("error"))
        {
            Fallo(estado.Length > 7 ? estado.Substring(7) : "");
            return;
        }
        LeerLoOido();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Lo que entendió desde el último cuadro (como mucho 3 cosas por cuadro).
    void LeerLoOido()
    {
        if (java == null || estado != "escuchando")
            return;
        for (int i = 0; i < 3; i++)
        {
            string s;
            try
            {
                s = java.CallStatic<string>("siguiente");
            }
            catch (System.Exception e)
            {
                Fallo(e.Message);
                return;
            }
            if (string.IsNullOrEmpty(s))
                return;
            UltimoOido = Time.time;
            if (ControlManos.Instancia != null)
                ControlManos.Instancia.ComandoDeVoz(s);
        }
    }
#endif

    void OnApplicationPause(bool pausa)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (pausa)
        {
            Parar();
        }
        else if (quiere && java != null && !esperandoPermiso && !preparando)
        {
            try
            {
                if (java.CallStatic<bool>("escuchar"))
                    estado = "escuchando";
            }
            catch (System.Exception e)
            {
                Fallo(e.Message);
            }
        }
#endif
    }

    void Fallo(string detalle)
    {
        Debug.LogWarning("TrazoVR voz: " + detalle);
        quiere = false;
        preparando = false;
        situacion = "";
        PlayerPrefs.SetInt(ClaveActiva, 0);
        Avisar("La voz no pudo arrancar (¿hay internet la primera vez?)");
    }

    static void Avisar(string texto)
    {
        var control = ControlManos.Instancia;
        if (control != null && control.dibujo != null)
            control.dibujo.Mensaje(texto);
    }
}

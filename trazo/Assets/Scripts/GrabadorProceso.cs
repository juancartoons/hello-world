using System.Collections.Generic;
using UnityEngine;

// Una muestra de la grabación del proceso (20 por segundo).
public class MuestraProceso
{
    public float tiempo;
    public Vector3[] manoIzq;   // 21 articulaciones (null = no se veía)
    public Vector3[] manoDer;
    public Vector3 raizPosicion;
    public Quaternion raizRotacion;
    public float raizEscala;
    public List<DatosTrazo> cambios = new List<DatosTrazo>(); // líneas que cambiaron
    public int[] visibles;                                     // ids de las líneas que se ven
}

// Graba cómo dibujas (tus manos y las líneas) para luego hacer un video del proceso.
// Botón "Grabar" en la página Medios del panel de arriba; "Video proceso" lo convierte en MP4.
public class GrabadorProceso : MonoBehaviour
{
    // Las 21 articulaciones de la mano (nombres viejos y nuevos de Meta).
    public static readonly string[][] Huesos =
    {
        new[] { "WristRoot", "Wrist" },
        new[] { "Thumb1", "ThumbMetacarpal" },
        new[] { "Thumb2", "ThumbProximal" },
        new[] { "Thumb3", "ThumbDistal" },
        new[] { "ThumbTip" },
        new[] { "Index1", "IndexProximal" },
        new[] { "Index2", "IndexIntermediate" },
        new[] { "Index3", "IndexDistal" },
        new[] { "IndexTip" },
        new[] { "Middle1", "MiddleProximal" },
        new[] { "Middle2", "MiddleIntermediate" },
        new[] { "Middle3", "MiddleDistal" },
        new[] { "MiddleTip" },
        new[] { "Ring1", "RingProximal" },
        new[] { "Ring2", "RingIntermediate" },
        new[] { "Ring3", "RingDistal" },
        new[] { "RingTip" },
        new[] { "Pinky1", "LittleProximal" },
        new[] { "Pinky2", "LittleIntermediate" },
        new[] { "Pinky3", "LittleDistal" },
        new[] { "PinkyTip", "LittleTip" },
    };

    public Dibujo dibujo;
    public ControlManos control;
    public float muestrasPorSegundo = 20f;
    public float maximoMinutos = 30f;

    public readonly List<MuestraProceso> muestras = new List<MuestraProceso>();
    public bool Grabando { get; private set; }
    public float Duracion => muestras.Count > 0 ? muestras[muestras.Count - 1].tiempo : 0f;

    float inicio;
    float proxima;
    float pausaDesde;

    // Pausa: deja de grabar un momento (lo que hagas mientras tanto aparece de una vez al reanudar).
    public bool Pausado { get; private set; }
    public float TiempoGrabado => Grabando ? (Pausado ? pausaDesde : Time.time) - inicio : Duracion;

    public void AlternarPausa()
    {
        if (!Grabando)
        {
            Mensaje("Primero toca Grabar");
            return;
        }
        if (!Pausado)
        {
            Pausado = true;
            pausaDesde = Time.time;
            Mensaje("Grabación en pausa (toca Reanudar)");
            return;
        }
        inicio += Time.time - pausaDesde; // el tiempo en pausa no cuenta
        Pausado = false;
        proxima = 0f;
        Mensaje("Grabando otra vez");
    }

    void Start()
    {
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (control == null) control = FindFirstObjectByType<ControlManos>();
    }

    void OnDisable()
    {
        if (Grabando)
            Detener();
    }

    public void Alternar()
    {
        if (Grabando)
        {
            Detener();
            Mensaje("Grabación lista (" + Formato(Duracion) + "). Toca Video proceso");
            return;
        }
        if (dibujo == null || control == null)
            return;
        muestras.Clear();
        Trazo.modificados.Clear();
        Trazo.registrarCambios = true;
        Grabando = true;
        inicio = Time.time;
        proxima = 0f;
        Tomar(true);
        Mensaje("Grabando el proceso... toca Detener para parar");
    }

    void Detener()
    {
        Grabando = false;
        Pausado = false;
        Trazo.registrarCambios = false;
        Trazo.modificados.Clear();
    }

    void Mensaje(string texto)
    {
        if (dibujo != null)
            dibujo.Mensaje(texto);
    }

    public static string Formato(float segundos)
    {
        int s = Mathf.FloorToInt(segundos);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    void LateUpdate()
    {
        if (!Grabando || Pausado)
            return;
        float t = Time.time - inicio;
        if (t > maximoMinutos * 60f)
        {
            Detener();
            Mensaje("Grabación detenida: llegó a " + maximoMinutos + " minutos");
            return;
        }
        if (t < proxima)
            return;
        proxima = t + 1f / Mathf.Max(1f, muestrasPorSegundo);
        Tomar(false);
    }

    void Tomar(bool todo)
    {
        var m = new MuestraProceso { tiempo = Time.time - inicio };
        m.manoIzq = Articulaciones(control.Izq);
        m.manoDer = Articulaciones(control.Der);
        Transform raiz = dibujo.transform;
        m.raizPosicion = raiz.position;
        m.raizRotacion = raiz.rotation;
        m.raizEscala = raiz.lossyScale.x;

        if (todo)
        {
            foreach (var t in dibujo.trazos)
                if (t != null)
                    m.cambios.Add(t.CrearDatosRepeticion());
        }
        else
        {
            foreach (var t in Trazo.modificados)
                if (t != null && dibujo.trazos.Contains(t))
                    m.cambios.Add(t.CrearDatosRepeticion());
        }
        Trazo.modificados.Clear();

        var ids = new List<int>();
        foreach (var t in dibujo.trazos)
            if (Dibujo.Editable(t))
                ids.Add(t.id);
        m.visibles = ids.ToArray();
        muestras.Add(m);
    }

    static Vector3[] Articulaciones(ManoSeguida mano)
    {
        if (mano == null || !mano.valida || mano.esqueleto == null)
            return null;
        var puntos = new Vector3[Huesos.Length];
        for (int i = 0; i < Huesos.Length; i++)
        {
            var h = ManosUtil.Hueso(mano.esqueleto, Huesos[i]);
            if (h == null)
                return null;
            puntos[i] = h.position;
        }
        return puntos;
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class DatosTrazo
{
    public List<Vector3> nodos = new List<Vector3>();
    public float ancho = 0.008f;
    public int estilo;
}

// Todo lo que se guarda de un dibujo (archivo .json). También sirve para "Deshacer".
[System.Serializable]
public class DatosDibujo
{
    public int version = 1;
    public List<DatosTrazo> trazos = new List<DatosTrazo>();
    public int estilo;
    public bool porLinea;
    public bool soloBlanco;
    public float anchoPincel = 0.008f;
    public Vector3 posicion;
    public Quaternion rotacion = Quaternion.identity;
    public float escala = 1f;
}

// El dibujo completo: crea las líneas, deshace, guarda y carga.
// Las líneas son hijas de este objeto, así la "caja" puede moverlo y escalarlo todo junto.
public class Dibujo : MonoBehaviour
{
    public Material materialLinea;
    [Tooltip("Grosor máximo (en el centro) de las líneas nuevas, en metros")]
    public float anchoPincel = 0.008f;
    public EstiloLinea estilo = EstiloLinea.Cinta;
    [Tooltip("Sí: cada línea conserva su estilo. No: Cinta/Tubo cambia todas las líneas")]
    public bool porLinea;
    public Escenario escenario;
    [Tooltip("Al abrir la app, recupera el último dibujo (autoguardado)")]
    public bool cargarAlIniciar = true;

    public readonly List<Trazo> trazos = new List<Trazo>();
    public event System.Action alCambiar;
    public event System.Action<string> alMensaje;

    const int maxHistorial = 40;
    readonly List<string> historial = new List<string>();
    readonly List<float> anchosInicio = new List<float>();
    float pincelInicio;

    string Carpeta => Path.Combine(Application.persistentDataPath, "Dibujos");
    string RutaAuto => Path.Combine(Carpeta, "autoguardado.json");
    string RutaGuardado => Path.Combine(Carpeta, "guardado.json");

    void Start()
    {
        if (cargarAlIniciar)
        {
            var d = Leer(RutaAuto);
            if (d != null)
                Aplicar(d, true);
        }
        Avisar();
    }

    void OnApplicationPause(bool pausa)
    {
        if (pausa)
            Escribir(RutaAuto, JsonUtility.ToJson(CrearDatos()));
    }

    void OnApplicationQuit()
    {
        Escribir(RutaAuto, JsonUtility.ToJson(CrearDatos()));
    }

    // ---------- Dibujar ----------

    public Trazo NuevoTrazo()
    {
        GuardarParaDeshacer();
        var t = CrearTrazo(anchoPincel / Mathf.Max(0.0001f, transform.lossyScale.x), estilo);
        trazos.Add(t);
        return t;
    }

    public void TerminarTrazo(Trazo t)
    {
        if (t == null)
            return;
        if (!t.Terminar())
        {
            trazos.Remove(t);
            Destroy(t.gameObject);
            DescartarUltimoDeshacer();
            return;
        }
        Avisar();
    }

    Trazo CrearTrazo(float ancho, EstiloLinea estiloTrazo)
    {
        var go = new GameObject("Trazo");
        go.transform.SetParent(transform, false);
        var t = go.AddComponent<Trazo>();
        t.Configurar(materialLinea, ancho, estiloTrazo);
        return t;
    }

    public void MoverNodo(Trazo t, int indice, Vector3 posicionLocal)
    {
        if (t == null || indice < 0 || indice >= t.nodos.Count)
            return;
        t.nodos[indice] = posicionLocal;
        t.Reconstruir();
    }

    // ---------- Grosor proporcional ----------

    public void EmpezarGrosor()
    {
        GuardarParaDeshacer();
        anchosInicio.Clear();
        foreach (var t in trazos)
            anchosInicio.Add(t.ancho);
        pincelInicio = anchoPincel;
    }

    public void AplicarFactorGrosor(float factor)
    {
        factor = Mathf.Clamp(factor, 0.1f, 10f);
        anchoPincel = Mathf.Clamp(pincelInicio * factor, 0.001f, 0.06f);
        for (int i = 0; i < trazos.Count && i < anchosInicio.Count; i++)
        {
            float nuevo = Mathf.Clamp(anchosInicio[i] * factor, 0.0005f, 0.5f);
            if (Mathf.Abs(nuevo - trazos[i].ancho) > 1e-6f)
            {
                trazos[i].ancho = nuevo;
                trazos[i].Reconstruir();
            }
        }
    }

    public void TerminarGrosor()
    {
        Avisar();
    }

    // ---------- Estilo ----------

    public void PonerCinta()
    {
        CambiarEstilo(EstiloLinea.Cinta);
    }

    public void PonerTubo()
    {
        CambiarEstilo(EstiloLinea.Tubo);
    }

    public void CambiarEstilo(EstiloLinea nuevo)
    {
        if (!porLinea)
        {
            GuardarParaDeshacer();
            foreach (var t in trazos)
            {
                if (t.estilo == nuevo)
                    continue;
                t.estilo = nuevo;
                t.Reconstruir();
            }
        }
        estilo = nuevo;
        Avisar();
        string nombre = nuevo == EstiloLinea.Tubo ? "Tubo" : "Cinta";
        Mensaje(porLinea ? "Próximas líneas: " + nombre : "Todo en " + nombre);
    }

    public void AlternarPorLinea()
    {
        porLinea = !porLinea;
        Avisar();
        Mensaje(porLinea ? "Cada línea con su estilo" : "Un estilo para todo");
    }

    // ---------- Deshacer y borrar ----------

    public void GuardarParaDeshacer()
    {
        historial.Add(JsonUtility.ToJson(CrearDatos()));
        if (historial.Count > maxHistorial)
            historial.RemoveAt(0);
    }

    public void DescartarUltimoDeshacer()
    {
        if (historial.Count > 0)
            historial.RemoveAt(historial.Count - 1);
    }

    public void Deshacer()
    {
        if (historial.Count == 0)
        {
            Mensaje("Nada que deshacer");
            return;
        }
        string json = historial[historial.Count - 1];
        historial.RemoveAt(historial.Count - 1);
        var d = JsonUtility.FromJson<DatosDibujo>(json);
        if (d != null)
            Aplicar(d, false);
        Mensaje("Deshecho");
    }

    public void BorrarTodo()
    {
        if (trazos.Count == 0)
        {
            Mensaje("No hay nada que borrar");
            return;
        }
        GuardarParaDeshacer();
        LimpiarTrazos();
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        Avisar();
        Mensaje("Borrado (Deshacer lo recupera)");
    }

    void LimpiarTrazos()
    {
        foreach (var t in trazos)
            if (t != null)
                Destroy(t.gameObject);
        trazos.Clear();
    }

    // ---------- Guardar y cargar ----------

    public void Guardar()
    {
        string json = JsonUtility.ToJson(CrearDatos(), true);
        if (Escribir(RutaGuardado, json))
        {
            string copia = "dibujo_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
            Escribir(Path.Combine(Carpeta, copia), json);
            Mensaje("Guardado");
        }
        else
        {
            Mensaje("No se pudo guardar");
        }
    }

    public void Cargar()
    {
        var d = Leer(RutaGuardado);
        if (d == null)
        {
            Mensaje("No hay nada guardado");
            return;
        }
        GuardarParaDeshacer();
        Aplicar(d, true);
        Mensaje("Cargado");
    }

    DatosDibujo CrearDatos()
    {
        var d = new DatosDibujo
        {
            estilo = (int)estilo,
            porLinea = porLinea,
            soloBlanco = escenario != null && escenario.soloBlanco,
            anchoPincel = anchoPincel,
            posicion = transform.localPosition,
            rotacion = transform.localRotation,
            escala = transform.localScale.x
        };
        foreach (var t in trazos)
        {
            if (t == null || t.nodos.Count < 2)
                continue;
            d.trazos.Add(new DatosTrazo { nodos = new List<Vector3>(t.nodos), ancho = t.ancho, estilo = (int)t.estilo });
        }
        return d;
    }

    void Aplicar(DatosDibujo d, bool incluirFondo)
    {
        LimpiarTrazos();
        estilo = (EstiloLinea)Mathf.Clamp(d.estilo, 0, 1);
        porLinea = d.porLinea;
        anchoPincel = d.anchoPincel > 0f ? d.anchoPincel : 0.008f;
        transform.localPosition = d.posicion;
        var q = d.rotacion;
        bool rotacionValida = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 0.5f;
        transform.localRotation = rotacionValida ? q : Quaternion.identity;
        transform.localScale = Vector3.one * (d.escala > 0f ? d.escala : 1f);
        if (d.trazos != null)
        {
            foreach (var dt in d.trazos)
            {
                if (dt == null || dt.nodos == null || dt.nodos.Count < 2)
                    continue;
                var t = CrearTrazo(dt.ancho > 0f ? dt.ancho : 0.008f, (EstiloLinea)Mathf.Clamp(dt.estilo, 0, 1));
                t.nodos.AddRange(dt.nodos);
                t.Reconstruir();
                trazos.Add(t);
            }
        }
        if (incluirFondo && escenario != null)
            escenario.PonerSoloBlanco(d.soloBlanco);
        Avisar();
    }

    bool Escribir(string ruta, string json)
    {
        try
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(ruta, json);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo guardar: " + e.Message);
            return false;
        }
    }

    DatosDibujo Leer(string ruta)
    {
        try
        {
            if (!File.Exists(ruta))
                return null;
            return JsonUtility.FromJson<DatosDibujo>(File.ReadAllText(ruta));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo leer: " + e.Message);
            return null;
        }
    }

    // ---------- Utilidades ----------

    // Caja que envuelve todos los nodos, en coordenadas locales del Dibujo.
    public bool Caja(out Bounds caja)
    {
        caja = new Bounds();
        bool hay = false;
        float margen = 0f;
        foreach (var t in trazos)
        {
            if (t == null)
                continue;
            foreach (var p in t.nodos)
            {
                if (!hay)
                {
                    caja = new Bounds(p, Vector3.zero);
                    hay = true;
                }
                else
                {
                    caja.Encapsulate(p);
                }
            }
            margen = Mathf.Max(margen, t.ancho);
        }
        if (hay)
            caja.Expand(margen + 0.02f / Mathf.Max(0.0001f, transform.lossyScale.x));
        return hay;
    }

    void Avisar()
    {
        alCambiar?.Invoke();
    }

    public void Mensaje(string texto)
    {
        alMensaje?.Invoke(texto);
    }
}

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class DatosTrazo
{
    public int id;
    public int capa;
    public List<Vector3> nodos = new List<Vector3>();
    public List<Vector3> asaEntrada = new List<Vector3>();
    public List<Vector3> asaSalida = new List<Vector3>();
    public List<bool> asaManual = new List<bool>();
    public List<float> grosorNodo = new List<float>();
    public bool cerrado;
    public bool relleno;
    public int colorRelleno;
    public float ancho = 0.008f;
    public int estilo;
    public bool crudo; // solo para la repetición: la línea aún se estaba dibujando
}

[System.Serializable]
public class DatosCapa
{
    public string nombre = "Capa";
    public bool visible = true;
}

// Una forma de boca de la biblioteca (para el lipsync): la forma de las líneas de la boca.
[System.Serializable]
public class PoseBoca
{
    public string nombre;
    public List<DatosTrazo> trazos = new List<DatosTrazo>();
}

// Una clave de animación: la forma de todas las líneas en un fotograma.
[System.Serializable]
public class Clave
{
    public int fotograma;
    public List<DatosTrazo> trazos = new List<DatosTrazo>();
}

// Todo lo que se guarda de un dibujo (archivo .json). También sirve para "Deshacer".
[System.Serializable]
public class DatosDibujo
{
    public int version = 3;
    public List<DatosTrazo> trazos = new List<DatosTrazo>();
    public int fondo;
    public float anchoPincel = 0.008f;
    public Vector3 posicion;
    public Quaternion rotacion = Quaternion.identity;
    public float escala = 1f;
    public bool plano;
    public bool hayPlano;
    public Vector3 planoPunto;
    public Vector3 planoNormal = Vector3.forward;
    public List<DatosCapa> capas = new List<DatosCapa>();
    public int capaActual;
    public int siguienteId = 1;
    public List<Clave> claves = new List<Clave>();
    public int fotograma;
    public float fps = 12f;
    public List<PoseBoca> bocas = new List<PoseBoca>();
    public string audio = "";
    public int temblor;
}

// El dibujo completo: crea las líneas, une, cierra, borra, deshace, guarda y carga.
// Las líneas son hijas de este objeto, así se puede mover, girar y escalar todo junto.
public class Dibujo : MonoBehaviour
{
    public const int NumeroDeCapas = 4;

    public Material materialLinea;
    public Material materialRelleno;
    public Material materialGuia;
    [Tooltip("Color de la línea seleccionada")]
    public Material materialSeleccion;
    [Tooltip("Rojo del borrador")]
    public Material materialBorrado;
    public Animacion animacion;
    public Lipsync lipsync;
    public Temblor temblor;
    [Tooltip("Grosor máximo (en el centro) de las líneas nuevas, en metros")]
    public float anchoPincel = 0.008f;
    [Tooltip("Dibujar sobre un plano (2D) en vez de libre en 3D")]
    public bool plano;
    [Tooltip("Distancia (metros) a la que las puntas se pegan como imán")]
    public float radioIman = 0.025f;
    public Escenario escenario;
    [Tooltip("Al abrir la app, recupera el último dibujo (autoguardado)")]
    public bool cargarAlIniciar = true;

    public readonly List<Trazo> trazos = new List<Trazo>();
    public readonly List<DatosCapa> capas = new List<DatosCapa>();
    public int capaActual;
    public event System.Action alCambiar;
    public event System.Action<string> alMensaje;

    public bool HayPlano { get; private set; }
    Vector3 planoPunto;
    Vector3 planoNormal = Vector3.forward;
    GameObject guia;
    int siguienteId = 1;
    Trazo seleccion;
    Trazo grosorSolo;
    float anchoSoloInicio;

    const int maxHistorial = 40;
    readonly List<string> historial = new List<string>();
    readonly List<float> anchosInicio = new List<float>();
    float pincelInicio;

    // Destellos rojos del borrador
    readonly List<Transform> destellos = new List<Transform>();
    readonly List<float> destellosFin = new List<float>();
    readonly List<float> destellosTam = new List<float>();

    string Carpeta => Path.Combine(Application.persistentDataPath, "Dibujos");
    string RutaAuto => Path.Combine(Carpeta, "autoguardado.json");
    string RutaGuardado => Path.Combine(Carpeta, "guardado.json");

    public float EscalaMundo => Mathf.Max(0.0001f, transform.lossyScale.x);
    public float RadioImanLocal => radioIman / EscalaMundo;
    public bool PlanoActivo => plano && HayPlano;
    bool AnimacionActiva => animacion != null && animacion.Activa;

    void Awake()
    {
        AsegurarCapas();
        if (animacion == null)
            animacion = GetComponent<Animacion>();
        if (temblor == null)
            temblor = GetComponent<Temblor>();
        if (temblor == null)
            temblor = gameObject.AddComponent<Temblor>();
    }

    void Start()
    {
        if (cargarAlIniciar)
        {
            var d = Leer(RutaAuto);
            if (d != null)
                Aplicar(d, true);
        }
        ActualizarGuia();
        Avisar();
    }

    void Update()
    {
        // Los destellos rojos se encogen y desaparecen.
        for (int i = 0; i < destellos.Count; i++)
        {
            var d = destellos[i];
            if (d == null || !d.gameObject.activeSelf)
                continue;
            float resto = destellosFin[i] - Time.time;
            if (resto <= 0f)
            {
                d.gameObject.SetActive(false);
                continue;
            }
            d.localScale = Vector3.one * destellosTam[i] * Mathf.Clamp01(resto / 0.25f);
        }
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

    // ---------- Capas ----------

    void AsegurarCapas()
    {
        while (capas.Count < NumeroDeCapas)
            capas.Add(new DatosCapa { nombre = "Capa " + (capas.Count + 1), visible = true });
        capaActual = Mathf.Clamp(capaActual, 0, capas.Count - 1);
    }

    public bool CapaVisible(int capa)
    {
        return capa < 0 || capa >= capas.Count || capas[capa].visible;
    }

    public void SeleccionarCapa(int capa)
    {
        AsegurarCapas();
        capaActual = Mathf.Clamp(capa, 0, capas.Count - 1);
        if (!capas[capaActual].visible)
        {
            capas[capaActual].visible = true;
            ActualizarVisibilidad();
        }
        Avisar();
        Mensaje("Dibujas en " + capas[capaActual].nombre);
    }

    public void AlternarVerCapa(int capa)
    {
        AsegurarCapas();
        if (capa < 0 || capa >= capas.Count)
            return;
        capas[capa].visible = !capas[capa].visible;
        ActualizarVisibilidad();
        Avisar();
        Mensaje(capas[capa].nombre + (capas[capa].visible ? " visible" : " oculta"));
    }

    // Una línea se ve si su capa está visible y si existe en el fotograma actual.
    public void ActualizarVisibilidad()
    {
        foreach (var t in trazos)
        {
            if (t == null)
                continue;
            bool ver = t.visibleAnim && CapaVisible(t.capa);
            if (t.gameObject.activeSelf != ver)
                t.gameObject.SetActive(ver);
        }
    }

    // Solo se pueden tocar/editar las líneas que se ven.
    public static bool Editable(Trazo t)
    {
        return t != null && t.gameObject.activeSelf;
    }

    // ---------- Selección ----------

    // La línea seleccionada (null = ninguna: los cambios afectan a todo el dibujo).
    public Trazo Seleccion => Editable(seleccion) ? seleccion : null;

    public void Seleccionar(Trazo t)
    {
        if (seleccion == t)
            return;
        if (seleccion != null)
            seleccion.PonerMaterialLinea(materialLinea);
        seleccion = t;
        if (seleccion != null)
            seleccion.PonerMaterialLinea(materialSeleccion != null ? materialSeleccion : materialLinea);
    }

    // ---------- Dibujar ----------

    public Trazo NuevoTrazo()
    {
        GuardarParaDeshacer();
        AsegurarCapas();
        if (!capas[capaActual].visible)
        {
            capas[capaActual].visible = true;
            ActualizarVisibilidad();
        }
        var t = CrearTrazo(AnchoNuevoLocal());
        t.id = siguienteId++;
        t.capa = capaActual;
        trazos.Add(t);
        return t;
    }

    // Grosor de las líneas nuevas: el promedio de las líneas que se ven (así siempre combinan,
    // aunque hayas agrandado o achicado todo). Si no hay líneas, el del pincel.
    public float AnchoNuevoLocal()
    {
        float suma = 0f;
        int cuenta = 0;
        foreach (var t in trazos)
        {
            if (!Editable(t) || t.Dibujando)
                continue;
            suma += t.ancho;
            cuenta++;
        }
        return cuenta > 0 ? suma / cuenta : anchoPincel / EscalaMundo;
    }

    public float AnchoNuevoMundo => AnchoNuevoLocal() * EscalaMundo;

    // Termina la línea; si su final toca su inicio se cierra, y si toca otra línea se une a ella.
    public void TerminarTrazo(Trazo t)
    {
        if (t == null)
            return;
        if (!t.Terminar())
        {
            QuitarDeLaLista(t);
            DescartarUltimoDeshacer();
            return;
        }
        int n = t.nodos.Count;
        float iman = RadioImanLocal;
        if (n >= 4 && Vector3.Distance(t.nodos[0], t.nodos[n - 1]) < iman && t.Largo > iman * 4f)
        {
            if (t.Cerrar(true))
                Mensaje("Figura cerrada");
        }
        else
        {
            Trazo otro;
            int extremo;
            if (BuscarExtremo(t.nodos[t.nodos.Count - 1], t, out otro, out extremo))
                Unir(t, 1, otro, extremo);
            if (BuscarExtremo(t.nodos[0], t, out otro, out extremo))
                Unir(t, 0, otro, extremo);
            // Si al unirse las puntas quedaron juntas (por ejemplo, un triángulo de rectas), se cierra.
            int m = t.nodos.Count;
            if (!t.cerrado && m >= 4 && Vector3.Distance(t.nodos[0], t.nodos[m - 1]) < iman)
            {
                if (t.Cerrar(true))
                    Mensaje("Figura cerrada");
            }
        }
        Avisar();
    }

    // Quita una línea que se estaba dibujando (por ejemplo, al pasar a girar/escalar).
    public void CancelarTrazo(Trazo t)
    {
        if (t == null)
            return;
        QuitarDeLaLista(t);
        DescartarUltimoDeshacer();
        Trazo.huboCambio = false;
    }

    Trazo CrearTrazo(float ancho)
    {
        var go = new GameObject("Trazo");
        go.layer = gameObject.layer; // la capa del dibujo: es lo que ven las fotos y los videos
        go.transform.SetParent(transform, false);
        var t = go.AddComponent<Trazo>();
        t.Configurar(materialLinea, materialRelleno, ancho, EstiloLinea.Cinta);
        return t;
    }

    void QuitarDeLaLista(Trazo t)
    {
        trazos.Remove(t);
        if (t != null)
            Destroy(t.gameObject);
    }

    // ---------- Editar ----------

    public void MoverNodo(Trazo t, int indice, Vector3 posicionLocal)
    {
        if (t == null)
            return;
        t.MoverNodo(indice, posicionLocal);
    }

    public void QuitarNodo(Trazo t, int indice)
    {
        if (t == null)
            return;
        int antes = t.nodos.Count;
        t.QuitarNodo(indice);
        if (animacion != null)
            animacion.QuitarNodoEnClaves(t.id, indice, antes);
        if (t.nodos.Count < 2)
            Desaparecer(t, false);
        Avisar();
    }

    // Agrega un nodo en la línea (y en todas las claves de la animación).
    public void InsertarNodo(Trazo t, int segmento, float posicion)
    {
        if (t == null)
            return;
        int antes = t.nodos.Count;
        t.InsertarNodo(segmento, posicion);
        if (animacion != null)
            animacion.InsertarNodoEnClaves(t.id, segmento, posicion, antes);
        Avisar();
    }

    // Vuelve a poner el color normal de la línea (azul si está seleccionada).
    public void RestaurarMaterial(Trazo t)
    {
        if (t != null)
            t.PonerMaterialLinea(t == seleccion && materialSeleccion != null ? materialSeleccion : materialLinea);
    }

    public void BorrarTrazo(Trazo t, bool conDestello)
    {
        if (t == null)
            return;
        Desaparecer(t, conDestello);
        Avisar();
    }

    // Con animación, la línea solo se esconde desde este fotograma; sin animación, se elimina.
    void Desaparecer(Trazo t, bool conDestello)
    {
        bool esconder = AnimacionActiva;
        if (esconder)
        {
            t.visibleAnim = false;
            Trazo.huboCambio = true;
        }
        else
        {
            trazos.Remove(t);
        }
        if (conDestello && materialBorrado != null && t.gameObject.activeSelf)
        {
            StartCoroutine(DestelloRojo(t, !esconder));
            return;
        }
        if (esconder)
            ActualizarVisibilidad();
        else
            Destroy(t.gameObject);
    }

    IEnumerator DestelloRojo(Trazo t, bool destruir)
    {
        var renderers = t.GetComponentsInChildren<MeshRenderer>();
        var originales = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originales[i] = renderers[i].sharedMaterial;
            renderers[i].sharedMaterial = materialBorrado;
        }
        yield return new WaitForSeconds(0.12f);
        if (t == null)
            yield break;
        if (destruir)
        {
            Destroy(t.gameObject);
            yield break;
        }
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].sharedMaterial = originales[i];
        ActualizarVisibilidad();
    }

    // Pequeño destello rojo (al borrar un nodo o un relleno).
    public void Destello(Vector3 mundo, float tamano)
    {
        if (materialBorrado == null)
            return;
        int libre = -1;
        for (int i = 0; i < destellos.Count; i++)
            if (destellos[i] != null && !destellos[i].gameObject.activeSelf)
            {
                libre = i;
                break;
            }
        if (libre < 0)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Destello";
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = materialBorrado;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            destellos.Add(go.transform);
            destellosFin.Add(0f);
            destellosTam.Add(0f);
            libre = destellos.Count - 1;
        }
        var d = destellos[libre];
        d.gameObject.SetActive(true);
        d.position = mundo;
        d.localScale = Vector3.one * tamano;
        destellosFin[libre] = Time.time + 0.25f;
        destellosTam[libre] = tamano;
    }

    public void CerrarTrazo(Trazo t, bool quitarUltimo)
    {
        if (t != null && t.Cerrar(quitarUltimo))
            Mensaje("Figura cerrada");
        Avisar();
    }

    // Busca la punta de otra línea abierta (y visible) cerca de un punto (local).
    public bool BuscarExtremo(Vector3 local, Trazo excluir, out Trazo encontrado, out int extremo)
    {
        encontrado = null;
        extremo = -1;
        float mejor = RadioImanLocal;
        foreach (var o in trazos)
        {
            if (!Editable(o) || o == excluir || o.cerrado || o.nodos.Count < 2)
                continue;
            float d0 = Vector3.Distance(local, o.nodos[0]);
            if (d0 < mejor)
            {
                mejor = d0;
                encontrado = o;
                extremo = 0;
            }
            float d1 = Vector3.Distance(local, o.nodos[o.nodos.Count - 1]);
            if (d1 < mejor)
            {
                mejor = d1;
                encontrado = o;
                extremo = 1;
            }
        }
        return encontrado != null;
    }

    // Une la punta "extremoA" de A (0 inicio, 1 final) con la punta "extremoB" de B. B desaparece.
    public void Unir(Trazo a, int extremoA, Trazo b, int extremoB)
    {
        if (a == null || b == null || a == b || a.cerrado || b.cerrado)
            return;
        a.AsegurarAsas();
        b.AsegurarAsas();
        if (extremoA == 0)
            a.Invertir();
        if (extremoB == 1)
            b.Invertir();
        int u = a.nodos.Count - 1;
        a.nodos[u] = b.nodos[0];
        a.asaSalida[u] = b.asaSalida[0];
        a.asaManual[u] = false;
        a.grosorNodo[u] = Mathf.Max(a.grosorNodo[u], b.grosorNodo[0]);
        for (int i = 1; i < b.nodos.Count; i++)
        {
            a.nodos.Add(b.nodos[i]);
            a.asaEntrada.Add(b.asaEntrada[i]);
            a.asaSalida.Add(b.asaSalida[i]);
            a.asaManual.Add(b.asaManual[i]);
            a.grosorNodo.Add(b.grosorNodo[i]);
        }
        a.ancho = Mathf.Max(a.ancho, b.ancho);
        QuitarDeLaLista(b);
        a.Reconstruir();
        Mensaje("Líneas unidas");
        Avisar();
    }

    // Tocar un relleno: si no tiene color lo pinta; si ya tiene, pasa al siguiente color.
    public void CambiarColorRelleno(Trazo t)
    {
        if (t == null || !t.cerrado)
            return;
        GuardarParaDeshacer();
        if (!t.relleno)
            t.relleno = true;
        else
            t.colorRelleno = (t.colorRelleno + 1) % Trazo.Paleta.Length;
        t.Reconstruir();
        Avisar();
    }

    public void QuitarRelleno(Trazo t)
    {
        if (t == null)
            return;
        t.relleno = false;
        t.Reconstruir();
        Avisar();
    }

    // ---------- Plano (dibujo 2D) ----------

    public void AlternarPlano()
    {
        plano = !plano;
        HayPlano = false;
        ActualizarGuia();
        Avisar();
        Mensaje(plano ? "Plano: tu próxima línea define el plano" : "Dibujo libre en 3D");
    }

    // Crea el plano en el punto donde empieza la línea, mirando hacia ti.
    public void DefinirPlano(Vector3 local, Vector3 adelanteMundo)
    {
        adelanteMundo.y = 0f;
        if (adelanteMundo.sqrMagnitude < 1e-4f)
            adelanteMundo = Vector3.forward;
        planoPunto = local;
        planoNormal = transform.InverseTransformDirection(adelanteMundo.normalized).normalized;
        HayPlano = true;
        ActualizarGuia();
    }

    public Vector3 ProyectarVectorEnPlano(Vector3 vectorLocal)
    {
        if (!PlanoActivo)
            return vectorLocal;
        return vectorLocal - planoNormal * Vector3.Dot(vectorLocal, planoNormal);
    }

    // Qué tan lejos (en metros) está un punto del plano de dibujo 2D.
    public float DistanciaAlPlanoMundo(Vector3 local)
    {
        if (!PlanoActivo)
            return 0f;
        return Mathf.Abs(Vector3.Dot(local - planoPunto, planoNormal)) * EscalaMundo;
    }

    public Vector3 ProyectarEnPlano(Vector3 local)
    {
        if (!PlanoActivo)
            return local;
        return local - planoNormal * Vector3.Dot(local - planoPunto, planoNormal);
    }

    void ActualizarGuia()
    {
        if (guia == null)
        {
            guia = new GameObject("GuiaPlano");
            guia.transform.SetParent(transform, false);
            guia.AddComponent<MeshFilter>().sharedMesh = MallaGuia();
            var mr = guia.AddComponent<MeshRenderer>();
            mr.sharedMaterial = materialGuia;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
        bool ver = PlanoActivo;
        guia.SetActive(ver);
        if (!ver)
            return;
        guia.transform.localPosition = planoPunto;
        Vector3 arriba = Vector3.ProjectOnPlane(transform.InverseTransformDirection(Vector3.up), planoNormal);
        if (arriba.sqrMagnitude < 1e-6f)
            arriba = Vector3.Cross(planoNormal, Vector3.right);
        guia.transform.localRotation = Quaternion.LookRotation(planoNormal, arriba);
        guia.transform.localScale = Vector3.one / Mathf.Max(0.0001f, transform.localScale.x);
    }

    // Cuadro de 1 m con una cruz y marcas cada 10 cm.
    static Mesh MallaGuia()
    {
        var v = new List<Vector3>();
        var idx = new List<int>();
        float m = 0.5f;
        Vector3[] esquinas = { new Vector3(-m, -m, 0f), new Vector3(m, -m, 0f), new Vector3(m, m, 0f), new Vector3(-m, m, 0f) };
        for (int i = 0; i < 4; i++)
        {
            idx.Add(v.Count); v.Add(esquinas[i]);
            idx.Add(v.Count); v.Add(esquinas[(i + 1) % 4]);
        }
        for (int i = -4; i <= 4; i++)
        {
            float c = i * 0.1f;
            float largo = i == 0 ? m : 0.02f;
            idx.Add(v.Count); v.Add(new Vector3(c, -largo, 0f));
            idx.Add(v.Count); v.Add(new Vector3(c, largo, 0f));
            idx.Add(v.Count); v.Add(new Vector3(-largo, c, 0f));
            idx.Add(v.Count); v.Add(new Vector3(largo, c, 0f));
        }
        var malla = new Mesh { name = "GuiaPlano" };
        malla.SetVertices(v);
        malla.SetIndices(idx, MeshTopology.Lines, 0);
        malla.RecalculateBounds();
        return malla;
    }

    // ---------- Grosor proporcional ----------

    // Con una línea seleccionada, el grosor cambia solo en ella; sin selección, en todo el dibujo.
    public void EmpezarGrosor()
    {
        GuardarParaDeshacer();
        grosorSolo = Seleccion;
        if (grosorSolo != null)
            anchoSoloInicio = grosorSolo.ancho;
        anchosInicio.Clear();
        foreach (var t in trazos)
            anchosInicio.Add(t != null ? t.ancho : 0f);
        pincelInicio = anchoPincel;
    }

    public void AplicarFactorGrosor(float factor)
    {
        factor = Mathf.Clamp(factor, 0.1f, 10f);
        if (grosorSolo != null)
        {
            float solo = Mathf.Clamp(anchoSoloInicio * factor, 0.0005f, 0.5f);
            if (Mathf.Abs(solo - grosorSolo.ancho) > 1e-6f)
            {
                grosorSolo.ancho = solo;
                grosorSolo.Reconstruir(true);
            }
            return;
        }
        anchoPincel = Mathf.Clamp(pincelInicio * factor, 0.001f, 0.06f);
        for (int i = 0; i < trazos.Count && i < anchosInicio.Count; i++)
        {
            if (trazos[i] == null)
                continue;
            float nuevo = Mathf.Clamp(anchosInicio[i] * factor, 0.0005f, 0.5f);
            if (Mathf.Abs(nuevo - trazos[i].ancho) > 1e-6f)
            {
                trazos[i].ancho = nuevo;
                trazos[i].Reconstruir(true);
            }
        }
    }

    public void TerminarGrosor()
    {
        grosorSolo = null;
        Avisar();
    }

    // ---------- Deshacer y borrar ----------

    public void GuardarParaDeshacer()
    {
        historial.Add(JsonUtility.ToJson(CrearDatos()));
        if (historial.Count > maxHistorial)
            historial.RemoveAt(0);
        if (animacion != null)
            animacion.AntesDeEditar();
    }

    public void DescartarUltimoDeshacer()
    {
        if (historial.Count > 0)
            historial.RemoveAt(historial.Count - 1);
    }

    public bool Deshacer()
    {
        if (historial.Count == 0)
        {
            Mensaje("Nada que deshacer");
            return false;
        }
        string json = historial[historial.Count - 1];
        historial.RemoveAt(historial.Count - 1);
        var d = JsonUtility.FromJson<DatosDibujo>(json);
        if (d != null)
            Aplicar(d, false);
        Mensaje("Deshecho");
        return true;
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
        if (animacion != null)
            animacion.Restaurar(null, 0);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        HayPlano = false;
        ActualizarGuia();
        Trazo.huboCambio = false;
        Avisar();
        Mensaje("Borrado (el pulgar a la izquierda lo recupera)");
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
        AsegurarCapas();
        var d = new DatosDibujo
        {
            fondo = escenario != null ? escenario.modo : 0,
            anchoPincel = anchoPincel,
            posicion = transform.localPosition,
            rotacion = transform.localRotation,
            escala = transform.localScale.x,
            plano = plano,
            hayPlano = HayPlano,
            planoPunto = planoPunto,
            planoNormal = planoNormal,
            capaActual = capaActual,
            siguienteId = siguienteId,
            fotograma = animacion != null ? animacion.Fotograma : 0,
            fps = animacion != null ? animacion.fotogramasPorSegundo : 12f,
            temblor = temblor != null ? temblor.nivel : 0
        };
        foreach (var c in capas)
            d.capas.Add(new DatosCapa { nombre = c.nombre, visible = c.visible });
        foreach (var t in trazos)
            if (t != null && t.nodos.Count >= 2)
                d.trazos.Add(t.CrearDatos());
        if (animacion != null)
            d.claves.AddRange(animacion.claves);
        if (lipsync != null)
        {
            d.bocas.AddRange(lipsync.CopiarPoses());
            d.audio = lipsync.ArchivoAudio;
        }
        return d;
    }

    void Aplicar(DatosDibujo d, bool incluirFondo)
    {
        Trazo.silenciar = true;
        LimpiarTrazos();
        anchoPincel = d.anchoPincel > 0f ? d.anchoPincel : 0.008f;
        transform.localPosition = d.posicion;
        var q = d.rotacion;
        bool rotacionValida = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 0.5f;
        transform.localRotation = rotacionValida ? q : Quaternion.identity;
        transform.localScale = Vector3.one * (d.escala > 0f ? d.escala : 1f);
        plano = d.plano;
        HayPlano = d.hayPlano && d.planoNormal.sqrMagnitude > 1e-6f;
        planoPunto = d.planoPunto;
        planoNormal = HayPlano ? d.planoNormal.normalized : Vector3.forward;

        capas.Clear();
        if (d.capas != null)
            foreach (var c in d.capas)
                if (c != null)
                    capas.Add(new DatosCapa { nombre = c.nombre, visible = c.visible });
        AsegurarCapas();
        capaActual = Mathf.Clamp(d.capaActual, 0, capas.Count - 1);

        siguienteId = Mathf.Max(1, d.siguienteId);
        if (d.trazos != null)
        {
            foreach (var dt in d.trazos)
            {
                if (dt == null || dt.nodos == null || dt.nodos.Count < 2)
                    continue;
                var t = CrearTrazo(dt.ancho > 0f ? dt.ancho : 0.008f);
                t.id = dt.id > 0 ? dt.id : siguienteId++;
                t.capa = Mathf.Clamp(dt.capa, 0, capas.Count - 1);
                siguienteId = Mathf.Max(siguienteId, t.id + 1);
                t.AplicarPose(dt, null, 0f);
                trazos.Add(t);
            }
        }
        seleccion = null;
        if (animacion != null)
        {
            if (d.fps > 0f)
                animacion.fotogramasPorSegundo = d.fps;
            animacion.Restaurar(d.claves, d.fotograma);
        }
        Trazo.silenciar = false;
        Trazo.huboCambio = false;
        if (lipsync != null)
            lipsync.Restaurar(d.bocas, d.audio, incluirFondo);
        if (temblor != null)
            temblor.nivel = Mathf.Clamp(d.temblor, 0, Temblor.Nombres.Length - 1);
        if (incluirFondo && escenario != null)
            escenario.PonerModo(d.fondo);
        ActualizarVisibilidad();
        ActualizarGuia();
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

    // ---------- Exportar (SVG y foto) ----------

    // Dirección desde la que se exporta: en Plano, de frente al plano; en 3D, desde tu cabeza hacia el dibujo.
    void VistaExportar(out Vector3 adelante, out Vector3 posicion)
    {
        var control = ControlManos.Instancia;
        Transform cabeza = control != null ? control.Cabeza : null;
        posicion = cabeza != null ? cabeza.position : transform.position - Vector3.forward;
        adelante = cabeza != null ? cabeza.forward : Vector3.forward;
        Bounds caja;
        if (Caja(out caja))
        {
            Vector3 centro = transform.TransformPoint(caja.center);
            Vector3 dir = centro - posicion;
            if (dir.sqrMagnitude > 1e-4f)
                adelante = dir.normalized;
        }
        if (PlanoActivo)
            adelante = transform.TransformDirection(planoNormal).normalized;
    }

    public void ExportarSVG()
    {
        Vector3 adelante, posicion;
        VistaExportar(out adelante, out posicion);
        string svg = Exportar.Svg(this, adelante);
        if (svg == null)
        {
            Mensaje("No hay líneas para exportar");
            return;
        }
        string nombre = "dibujo_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".svg";
        Mensaje(Escribir(Path.Combine(Carpeta, nombre), svg) ? "SVG guardado: " + nombre : "No se pudo guardar el SVG");
    }

    // Capas que ven las fotos y los videos: solo la del dibujo (sin paneles, nodos ni imágenes de referencia).
    public int MascaraExportar => gameObject.layer != 0 ? (1 << gameObject.layer) : ~0;

    // Desde dónde se toma la foto o el video: tu cabeza mirando al dibujo, con el ángulo justo para que quepa.
    public void EncuadreExportar(float margen, out Vector3 posicion, out Quaternion rotacion, out float campoVision)
    {
        Vector3 adelante;
        VistaExportar(out adelante, out posicion);
        campoVision = 60f;
        Bounds caja;
        if (Caja(out caja))
        {
            float distancia = Vector3.Distance(transform.TransformPoint(caja.center), posicion);
            float radio = caja.extents.magnitude * EscalaMundo;
            if (distancia > 0.05f)
                campoVision = Mathf.Clamp(2f * Mathf.Atan(radio / distancia) * Mathf.Rad2Deg * margen, 20f, 100f);
        }
        Vector3 arriba = Mathf.Abs(Vector3.Dot(adelante, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        rotacion = Quaternion.LookRotation(adelante, arriba);
    }

    public string CarpetaDibujos => Carpeta;

    public void TomarFoto()
    {
        Vector3 posicion;
        Quaternion rotacion;
        float campoVision;
        EncuadreExportar(1.15f, out posicion, out rotacion, out campoVision);
        byte[] png = Exportar.Foto(posicion, rotacion, campoVision, 2560, 1440, Color.white, MascaraExportar);
        if (png == null)
        {
            Mensaje("No se pudo tomar la foto");
            return;
        }
        string nombre = "foto_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        try
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllBytes(Path.Combine(Carpeta, nombre), png);
            Mensaje("Foto guardada: " + nombre);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo guardar la foto: " + e.Message);
            Mensaje("No se pudo guardar la foto");
        }
    }

    // ---------- Utilidades ----------

    // Caja que envuelve todos los nodos visibles, en coordenadas locales del Dibujo.
    public bool Caja(out Bounds caja)
    {
        caja = new Bounds();
        bool hay = false;
        float margen = 0f;
        foreach (var t in trazos)
        {
            if (!Editable(t))
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
            caja.Expand(margen + 0.02f / EscalaMundo);
        return hay;
    }

    public void NotificarCambio()
    {
        ActualizarGuia();
        Avisar();
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

using System.Collections.Generic;
using UnityEngine;

// Los gestos de TrazoVR (todo con las manos):
//  Izquierda pulgar + ÍNDICE (sostener)  -> dibujar con la punta del índice derecho.
//  Izquierda pulgar + MEDIO  (sostener)  -> modo nodos: pellizca con la derecha para mover nodos y asas.
//       Doble pellizco sobre un nodo = borrar nodo; sobre la línea = borrar línea;
//       sobre el relleno = quitar color; sobre un asa = asa automática.
//       Arrastra la punta de una línea sobre otra punta: se unen como imán (o se cierra la figura).
//  Izquierda pulgar + ANULAR (sostener)  -> sube/baja la mano izquierda: más grueso o más delgado.
//  LAS DOS manos pellizcando (índice + pulgar) -> escalar, girar (como volante) y mover todo.
//  Izquierda: puño con el pulgar apuntando a tu izquierda -> deshacer.
//  Tocar un relleno con el índice derecho -> cambiar su color.
//  Palma izquierda mirándote -> panel de botones.
[DefaultExecutionOrder(-50)]
public class ControlManos : MonoBehaviour
{
    public static ControlManos Instancia { get; private set; }

    public Dibujo dibujo;
    public CajaTransformar caja;
    public Material materialNodo;
    public Material materialNodoActivo;
    public Material materialAsa;
    public Material materialIman;
    public Material materialCursor;

    [Header("Gestos (en metros)")]
    public float pellizcoEntra = 0.02f;
    public float pellizcoSale = 0.035f;
    [Tooltip("Segundos que hay que sostener un pellizco antes de que cuente")]
    public float confirmarGesto = 0.08f;
    [Tooltip("Más alto = sigue más rápido al dedo; más bajo = más suave")]
    public float suavizado = 16f;
    public float radioAgarreNodo = 0.025f;
    public float radioAgarreLinea = 0.015f;
    public float tamanoNodo = 0.012f;
    [Tooltip("Cuánto cambia el grosor al subir/bajar la mano izquierda")]
    public float sensibilidadGrosor = 4f;
    [Tooltip("Segundos máximos entre los dos pellizcos de un doble pellizco")]
    public float tiempoDoblePellizco = 0.45f;

    public enum Gesto { Ninguno, Dibujar, Nodos, Grosor, Transformar }
    public Gesto GestoIzq { get; private set; }

    public ManoSeguida Izq { get; } = new ManoSeguida(true);
    public ManoSeguida Der { get; } = new ManoSeguida(false);
    public Transform Cabeza { get; private set; }

    enum Objetivo { Nada, Nodo, Asa, Linea, Relleno }

    OVRCameraRig rig;
    Gesto candidato;
    float candidatoDesde;
    bool esperarSoltarIzq;
    Trazo trazoActual;
    float inicioTrazo;

    // Modo nodos
    readonly List<Transform> nodosVisibles = new List<Transform>();
    readonly List<Renderer> nodosRender = new List<Renderer>();
    readonly Transform[] asasVisibles = new Transform[2];
    GameObject lineasAsas;
    Mesh mallaLineasAsas;
    readonly List<Vector3> puntosAsas = new List<Vector3>();
    readonly List<int> indicesAsas = new List<int>();

    Objetivo arrastre = Objetivo.Nada;
    Trazo arrTrazo;
    int arrIndice = -1;
    bool arrSalida;
    Vector3 desfase;
    bool deshacerPendiente;

    Trazo selTrazo;
    int selIndice = -1;
    Objetivo hoverTipo;
    Trazo hoverTrazo;
    int hoverIndice = -1;
    bool hoverSalida;

    Trazo imanTrazo;
    int imanExtremo;

    Vector3 pellizcoInicio;
    Objetivo pellizcoTipo;
    Trazo pellizcoTrazo;
    int pellizcoIndice;
    float finUltimoPellizco = -10f;
    bool ultimoSinMover;
    Objetivo tipoUltimo;
    Trazo trazoUltimo;
    int indiceUltimo;

    // Grosor, deshacer y rellenos
    float alturaInicialGrosor;
    float tiempoDeshacer;
    bool esperarSoltarDeshacer;
    bool tocandoRelleno;
    float proximoToque;

    Transform cursor;
    static Mesh mallaNodo;

    void Awake()
    {
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }

    void Start()
    {
        rig = FindFirstObjectByType<OVRCameraRig>();
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
        if (caja == null)
            caja = GetComponent<CajaTransformar>();

        var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "Cursor";
        Destroy(esfera.GetComponent<Collider>());
        esfera.transform.SetParent(transform, false);
        var r = esfera.GetComponent<Renderer>();
        if (materialCursor != null)
            r.sharedMaterial = materialCursor;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        cursor = esfera.transform;
        cursor.gameObject.SetActive(false);

        for (int i = 0; i < 2; i++)
        {
            asasVisibles[i] = CrearRombo("Asa", materialAsa).transform;
            asasVisibles[i].gameObject.SetActive(false);
        }
        lineasAsas = new GameObject("LineasAsas");
        lineasAsas.transform.SetParent(transform, false);
        mallaLineasAsas = new Mesh { name = "LineasAsas" };
        mallaLineasAsas.MarkDynamic();
        lineasAsas.AddComponent<MeshFilter>().sharedMesh = mallaLineasAsas;
        var mr = lineasAsas.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materialAsa;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        lineasAsas.SetActive(false);
    }

    void Update()
    {
        if (rig == null)
        {
            rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig == null)
                return;
        }
        if (dibujo == null)
            return;

        Cabeza = rig.centerEyeAnchor;
        Izq.Actualizar(rig.leftHandAnchor, suavizado, pellizcoEntra, pellizcoSale);
        Der.Actualizar(rig.rightHandAnchor, suavizado, pellizcoEntra, pellizcoSale);

        ActualizarGestoIzquierdo();

        switch (GestoIzq)
        {
            case Gesto.Dibujar: Dibujar(); break;
            case Gesto.Nodos: EditarNodos(); break;
            case Gesto.Grosor: CambiarGrosor(); break;
            case Gesto.Transformar: if (caja != null) caja.Actualizar(Izq, Der); break;
            default:
                RevisarDeshacer();
                RevisarToqueRelleno();
                break;
        }

        if (GestoIzq != Gesto.Nodos)
            OcultarModoNodos();
        ActualizarCursor();
    }

    // ---------- Mano izquierda: qué dedo toca el pulgar ----------

    void ActualizarGestoIzquierdo()
    {
        // Las dos manos pellizcando = girar/escalar. Tiene prioridad sobre dibujar.
        if (GestoIzq == Gesto.Transformar)
        {
            if (!Izq.pellizco || !Der.pellizco)
            {
                SalirDeGesto();
                esperarSoltarIzq = true;
            }
            return;
        }
        if (GestoIzq == Gesto.Dibujar && Der.pellizco && Der.TiempoPellizco > 0.1f && Izq.pellizco)
        {
            PasarATransformar();
            return;
        }

        if (!Izq.valida)
        {
            // Si la mano se pierde un instante, seguimos; si se pierde más, soltamos.
            if (GestoIzq != Gesto.Ninguno && Izq.sinSenal > 0.25f)
                SalirDeGesto();
            candidato = Gesto.Ninguno;
            return;
        }

        float dIndice = Vector3.Distance(Izq.pulgar, Izq.indice);
        float dMedio = Vector3.Distance(Izq.pulgar, Izq.medio);
        float dAnular = Vector3.Distance(Izq.pulgar, Izq.anular);

        if (GestoIzq != Gesto.Ninguno)
        {
            float d = GestoIzq == Gesto.Dibujar ? dIndice : GestoIzq == Gesto.Nodos ? dMedio : dAnular;
            if (d > pellizcoSale)
                SalirDeGesto();
            return;
        }

        if (esperarSoltarIzq)
        {
            if (dIndice > pellizcoSale && dMedio > pellizcoSale && dAnular > pellizcoSale)
                esperarSoltarIzq = false;
            return;
        }

        Gesto nuevo = Gesto.Ninguno;
        float menor = pellizcoEntra;
        if (dIndice < menor) { menor = dIndice; nuevo = Gesto.Dibujar; }
        if (dMedio < menor) { menor = dMedio; nuevo = Gesto.Nodos; }
        if (dAnular < menor) { nuevo = Gesto.Grosor; }

        if (nuevo != candidato)
        {
            candidato = nuevo;
            candidatoDesde = Time.time;
        }
        if (nuevo == Gesto.Ninguno || Time.time - candidatoDesde < confirmarGesto)
            return;

        if (nuevo == Gesto.Dibujar && Der.pellizco && Izq.pellizco)
            EntrarEnGesto(Gesto.Transformar);
        else
            EntrarEnGesto(nuevo);
    }

    void PasarATransformar()
    {
        if (trazoActual != null)
        {
            // Si apenas empezaba la línea, se descarta; si ya era larga, se conserva.
            if (trazoActual.LargoCrudo * dibujo.EscalaMundo < 0.05f || Time.time - inicioTrazo < 0.4f)
                dibujo.CancelarTrazo(trazoActual);
            else
                dibujo.TerminarTrazo(trazoActual);
            trazoActual = null;
        }
        GestoIzq = Gesto.Ninguno;
        EntrarEnGesto(Gesto.Transformar);
    }

    void EntrarEnGesto(Gesto g)
    {
        GestoIzq = g;
        if (g == Gesto.Grosor)
        {
            alturaInicialGrosor = Izq.pulgar.y;
            dibujo.EmpezarGrosor();
        }
        else if (g == Gesto.Transformar)
        {
            if (caja != null)
                caja.Empezar(Izq, Der);
        }
    }

    void SalirDeGesto()
    {
        if (trazoActual != null)
        {
            dibujo.TerminarTrazo(trazoActual);
            trazoActual = null;
        }
        if (GestoIzq == Gesto.Grosor)
            dibujo.TerminarGrosor();
        if (GestoIzq == Gesto.Transformar && caja != null)
            caja.Terminar();
        if (arrastre != Objetivo.Nada)
            TerminarArrastre();
        selTrazo = null;
        selIndice = -1;
        GestoIzq = Gesto.Ninguno;
        candidato = Gesto.Ninguno;
    }

    // ---------- Dibujar ----------

    void Dibujar()
    {
        if (!Der.valida)
            return;
        Vector3 local = dibujo.transform.InverseTransformPoint(Der.indice);
        if (trazoActual == null)
        {
            if (dibujo.plano && !dibujo.HayPlano && Cabeza != null)
                dibujo.DefinirPlano(local, Cabeza.forward);
            trazoActual = dibujo.NuevoTrazo();
            inicioTrazo = Time.time;
        }
        trazoActual.AgregarPuntoCrudo(dibujo.ProyectarEnPlano(local));
    }

    // ---------- Modo nodos ----------

    void EditarNodos()
    {
        if (!Der.valida)
        {
            if (arrastre != Objetivo.Nada)
                TerminarArrastre();
            MostrarModoNodos();
            return;
        }
        Vector3 pinza = Der.PuntoPellizco;

        if (arrastre != Objetivo.Nada && !Der.pellizco)
            TerminarArrastre();
        if (Der.soltoPellizco)
        {
            finUltimoPellizco = Time.time;
            ultimoSinMover = Vector3.Distance(pinza, pellizcoInicio) < 0.02f;
            tipoUltimo = pellizcoTipo;
            trazoUltimo = pellizcoTrazo;
            indiceUltimo = pellizcoIndice;
        }

        if (arrastre != Objetivo.Nada)
        {
            ContinuarArrastre(pinza);
            MostrarModoNodos();
            return;
        }

        Objetivo tipo;
        Trazo t;
        int i;
        bool salida;
        DetectarObjetivo(pinza, Der.indice, out tipo, out t, out i, out salida);
        hoverTipo = tipo;
        hoverTrazo = t;
        hoverIndice = i;
        hoverSalida = salida;

        if (Der.empezoPellizco)
        {
            bool doble = tipo != Objetivo.Nada
                         && Time.time - finUltimoPellizco < tiempoDoblePellizco
                         && ultimoSinMover
                         && tipo == tipoUltimo
                         && t == trazoUltimo
                         && (tipo != Objetivo.Nodo || i == indiceUltimo);
            pellizcoInicio = pinza;
            pellizcoTipo = tipo;
            pellizcoTrazo = t;
            pellizcoIndice = i;
            if (doble)
            {
                finUltimoPellizco = -10f;
                pellizcoTipo = Objetivo.Nada;
                BorrarObjetivo(tipo, t, i);
            }
            else if (tipo == Objetivo.Nodo || tipo == Objetivo.Asa)
            {
                EmpezarArrastre(tipo, t, i, salida, pinza);
            }
        }
        MostrarModoNodos();
    }

    void DetectarObjetivo(Vector3 pinza, Vector3 punta, out Objetivo tipo, out Trazo trazo, out int indice, out bool salida)
    {
        tipo = Objetivo.Nada;
        trazo = null;
        indice = -1;
        salida = false;

        Trazo t;
        int i;
        if (BuscarNodoCercano(punta, pinza, out t, out i))
        {
            tipo = Objetivo.Nodo;
            trazo = t;
            indice = i;
            return;
        }
        bool s;
        if (BuscarAsaCercana(punta, pinza, out s))
        {
            tipo = Objetivo.Asa;
            trazo = selTrazo;
            indice = selIndice;
            salida = s;
            return;
        }

        Vector3 local = dibujo.transform.InverseTransformPoint(pinza);
        float escala = dibujo.EscalaMundo;
        float mejor = radioAgarreLinea / escala;
        foreach (var o in dibujo.trazos)
        {
            if (o == null)
                continue;
            float d = o.DistanciaACurva(local);
            if (d < mejor)
            {
                mejor = d;
                trazo = o;
                tipo = Objetivo.Linea;
            }
        }
        if (tipo == Objetivo.Linea)
            return;
        foreach (var o in dibujo.trazos)
        {
            if (o != null && o.relleno && o.DentroDeRelleno(local, 0.03f / escala))
            {
                tipo = Objetivo.Relleno;
                trazo = o;
                return;
            }
        }
    }

    bool BuscarNodoCercano(Vector3 a, Vector3 b, out Trazo trazo, out int indice)
    {
        trazo = null;
        indice = -1;
        float mejor = radioAgarreNodo;
        foreach (var t in dibujo.trazos)
        {
            if (t == null)
                continue;
            for (int i = 0; i < t.nodos.Count; i++)
            {
                Vector3 p = dibujo.transform.TransformPoint(t.nodos[i]);
                float d = Mathf.Min(Vector3.Distance(p, a), Vector3.Distance(p, b));
                if (d < mejor)
                {
                    mejor = d;
                    trazo = t;
                    indice = i;
                }
            }
        }
        return trazo != null;
    }

    bool SeleccionValida => selTrazo != null && selIndice >= 0 && selIndice < selTrazo.nodos.Count;

    Vector3 PuntaAsaMundo(Trazo t, int i, bool salida)
    {
        t.AsegurarAsas();
        Vector3 local = t.nodos[i] + (salida ? t.asaSalida[i] : t.asaEntrada[i]);
        return dibujo.transform.TransformPoint(local);
    }

    bool BuscarAsaCercana(Vector3 a, Vector3 b, out bool salida)
    {
        salida = false;
        if (!SeleccionValida)
            return false;
        float mejor = radioAgarreNodo * 0.8f;
        bool hay = false;
        for (int k = 0; k < 2; k++)
        {
            bool esSalida = k == 1;
            if (!selTrazo.AsaUsada(selIndice, esSalida))
                continue;
            Vector3 p = PuntaAsaMundo(selTrazo, selIndice, esSalida);
            float d = Mathf.Min(Vector3.Distance(p, a), Vector3.Distance(p, b));
            if (d < mejor)
            {
                mejor = d;
                salida = esSalida;
                hay = true;
            }
        }
        return hay;
    }

    void EmpezarArrastre(Objetivo tipo, Trazo t, int i, bool salida, Vector3 pinza)
    {
        arrastre = tipo;
        arrTrazo = t;
        arrIndice = i;
        arrSalida = salida;
        deshacerPendiente = true;
        imanTrazo = null;
        selTrazo = t;
        selIndice = i;
        Vector3 objetivo = tipo == Objetivo.Nodo
            ? dibujo.transform.TransformPoint(t.nodos[i])
            : PuntaAsaMundo(t, i, salida);
        desfase = objetivo - pinza;
    }

    void ContinuarArrastre(Vector3 pinza)
    {
        if (arrTrazo == null || arrIndice < 0 || arrIndice >= arrTrazo.nodos.Count)
        {
            arrastre = Objetivo.Nada;
            return;
        }
        Vector3 local = dibujo.ProyectarEnPlano(dibujo.transform.InverseTransformPoint(pinza + desfase));

        if (deshacerPendiente)
        {
            // Guardamos para "deshacer" solo cuando de verdad se mueve algo.
            Vector3 actual = arrastre == Objetivo.Nodo
                ? arrTrazo.nodos[arrIndice]
                : arrTrazo.nodos[arrIndice] + (arrSalida ? arrTrazo.asaSalida[arrIndice] : arrTrazo.asaEntrada[arrIndice]);
            if (Vector3.Distance(local, actual) * dibujo.EscalaMundo < 0.003f)
                return;
            dibujo.GuardarParaDeshacer();
            deshacerPendiente = false;
        }

        if (arrastre == Objetivo.Asa)
        {
            arrTrazo.MoverAsa(arrIndice, arrSalida, local - arrTrazo.nodos[arrIndice]);
            return;
        }

        // Nodo: si es una punta y llega cerca de otra punta, se pega como imán.
        imanTrazo = null;
        int ultimo = arrTrazo.nodos.Count - 1;
        bool esPunta = !arrTrazo.cerrado && (arrIndice == 0 || arrIndice == ultimo);
        if (esPunta)
        {
            int otro = arrIndice == 0 ? ultimo : 0;
            if (arrTrazo.nodos.Count >= 4 && Vector3.Distance(local, arrTrazo.nodos[otro]) < dibujo.RadioImanLocal)
            {
                local = arrTrazo.nodos[otro];
                imanTrazo = arrTrazo;
                imanExtremo = otro == 0 ? 0 : 1;
            }
            else
            {
                Trazo o;
                int e;
                if (dibujo.BuscarExtremo(local, arrTrazo, out o, out e))
                {
                    local = e == 0 ? o.nodos[0] : o.nodos[o.nodos.Count - 1];
                    imanTrazo = o;
                    imanExtremo = e;
                }
            }
        }
        dibujo.MoverNodo(arrTrazo, arrIndice, local);
    }

    void TerminarArrastre()
    {
        if (arrastre == Objetivo.Nodo && imanTrazo != null && arrTrazo != null)
        {
            int extremoArrastrado = arrIndice == 0 ? 0 : 1;
            if (imanTrazo == arrTrazo)
                dibujo.CerrarTrazo(arrTrazo, extremoArrastrado == 1);
            else
                dibujo.Unir(arrTrazo, extremoArrastrado, imanTrazo, imanExtremo);
            selTrazo = null;
            selIndice = -1;
        }
        arrastre = Objetivo.Nada;
        arrTrazo = null;
        arrIndice = -1;
        imanTrazo = null;
    }

    void BorrarObjetivo(Objetivo tipo, Trazo t, int i)
    {
        if (t == null)
            return;
        dibujo.GuardarParaDeshacer();
        switch (tipo)
        {
            case Objetivo.Nodo:
                dibujo.QuitarNodo(t, i);
                dibujo.Mensaje("Nodo borrado");
                break;
            case Objetivo.Asa:
                t.ReiniciarAsa(i);
                dibujo.Mensaje("Asa automática");
                break;
            case Objetivo.Linea:
                dibujo.BorrarTrazo(t);
                dibujo.Mensaje("Línea borrada");
                break;
            case Objetivo.Relleno:
                dibujo.QuitarRelleno(t);
                dibujo.Mensaje("Color quitado");
                break;
        }
        selTrazo = null;
        selIndice = -1;
    }

    void MostrarModoNodos()
    {
        if (!SeleccionValida)
        {
            selTrazo = null;
            selIndice = -1;
        }

        int n = 0;
        int imanIndice = imanTrazo != null ? (imanExtremo == 0 ? 0 : imanTrazo.nodos.Count - 1) : -1;
        foreach (var t in dibujo.trazos)
        {
            if (t == null)
                continue;
            for (int i = 0; i < t.nodos.Count; i++)
            {
                if (n >= nodosVisibles.Count)
                {
                    var go = CrearRombo("Nodo", materialNodo);
                    nodosVisibles.Add(go.transform);
                    nodosRender.Add(go.GetComponent<Renderer>());
                }
                var nodo = nodosVisibles[n];
                if (!nodo.gameObject.activeSelf)
                    nodo.gameObject.SetActive(true);
                nodo.position = dibujo.transform.TransformPoint(t.nodos[i]);
                nodo.rotation = Quaternion.identity;

                bool activo = (t == arrTrazo && i == arrIndice)
                              || (arrastre == Objetivo.Nada && hoverTipo == Objetivo.Nodo && t == hoverTrazo && i == hoverIndice)
                              || (t == selTrazo && i == selIndice);
                bool iman = t == imanTrazo && i == imanIndice;
                bool punta = !t.cerrado && (i == 0 || i == t.nodos.Count - 1);
                float tam = tamanoNodo * (punta ? 1.25f : 1f) * (activo || iman ? 1.4f : 1f);
                nodo.localScale = Vector3.one * tam;
                var mat = iman ? materialIman : activo ? materialNodoActivo : materialNodo;
                if (mat != null && nodosRender[n].sharedMaterial != mat)
                    nodosRender[n].sharedMaterial = mat;
                n++;
            }
        }
        OcultarNodosDesde(n);
        MostrarAsas();
    }

    // Las asas (como en Illustrator) se ven solo en el nodo seleccionado.
    void MostrarAsas()
    {
        puntosAsas.Clear();
        indicesAsas.Clear();
        bool hay = false;
        for (int k = 0; k < 2; k++)
        {
            bool esSalida = k == 1;
            bool ver = SeleccionValida && selTrazo.AsaUsada(selIndice, esSalida);
            var asa = asasVisibles[k];
            if (asa == null)
                continue;
            if (asa.gameObject.activeSelf != ver)
                asa.gameObject.SetActive(ver);
            if (!ver)
                continue;
            Vector3 punta = PuntaAsaMundo(selTrazo, selIndice, esSalida);
            asa.position = punta;
            asa.rotation = Quaternion.identity;
            bool activa = (arrastre == Objetivo.Asa && arrSalida == esSalida)
                          || (arrastre == Objetivo.Nada && hoverTipo == Objetivo.Asa && hoverSalida == esSalida);
            asa.localScale = Vector3.one * tamanoNodo * (activa ? 1.1f : 0.75f);
            indicesAsas.Add(puntosAsas.Count);
            puntosAsas.Add(dibujo.transform.TransformPoint(selTrazo.nodos[selIndice]));
            indicesAsas.Add(puntosAsas.Count);
            puntosAsas.Add(punta);
            hay = true;
        }
        if (lineasAsas == null)
            return;
        if (lineasAsas.activeSelf != hay)
            lineasAsas.SetActive(hay);
        if (!hay)
            return;
        mallaLineasAsas.Clear();
        mallaLineasAsas.SetVertices(puntosAsas);
        mallaLineasAsas.SetIndices(indicesAsas, MeshTopology.Lines, 0);
        mallaLineasAsas.RecalculateBounds();
    }

    void OcultarModoNodos()
    {
        OcultarNodosDesde(0);
        foreach (var asa in asasVisibles)
            if (asa != null && asa.gameObject.activeSelf)
                asa.gameObject.SetActive(false);
        if (lineasAsas != null && lineasAsas.activeSelf)
            lineasAsas.SetActive(false);
    }

    void OcultarNodosDesde(int desde)
    {
        for (int i = desde; i < nodosVisibles.Count; i++)
            if (nodosVisibles[i].gameObject.activeSelf)
                nodosVisibles[i].gameObject.SetActive(false);
    }

    GameObject CrearRombo(string nombre, Material mat)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = MallaNodo();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    // Rombo 3D (octaedro): pocos triángulos y se ve limpio, tipo vector.
    static Mesh MallaNodo()
    {
        if (mallaNodo != null)
            return mallaNodo;
        mallaNodo = new Mesh { name = "Nodo" };
        mallaNodo.vertices = new[]
        {
            new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 0f, 0f),
            new Vector3(0f, 0.5f, 0f), new Vector3(0f, -0.5f, 0f),
            new Vector3(0f, 0f, 0.5f), new Vector3(0f, 0f, -0.5f)
        };
        mallaNodo.triangles = new[] { 2, 4, 0, 2, 0, 5, 2, 5, 1, 2, 1, 4, 3, 0, 4, 3, 5, 0, 3, 1, 5, 3, 4, 1 };
        mallaNodo.RecalculateNormals();
        mallaNodo.RecalculateBounds();
        return mallaNodo;
    }

    // ---------- Grosor ----------

    void CambiarGrosor()
    {
        if (!Izq.valida)
            return;
        float factor = Mathf.Exp((Izq.pulgar.y - alturaInicialGrosor) * sensibilidadGrosor);
        dibujo.AplicarFactorGrosor(factor);
    }

    // ---------- Deshacer: puño izquierdo con el pulgar hacia tu izquierda ----------

    void RevisarDeshacer()
    {
        if (!PoseDeshacer())
        {
            tiempoDeshacer = 0f;
            esperarSoltarDeshacer = false;
            return;
        }
        if (esperarSoltarDeshacer)
            return;
        tiempoDeshacer += Time.deltaTime;
        if (tiempoDeshacer < 0.25f)
            return;
        esperarSoltarDeshacer = true;
        dibujo.Deshacer();
    }

    bool PoseDeshacer()
    {
        if (!Izq.valida || Cabeza == null)
            return false;
        var esqueleto = Izq.esqueleto;
        var palma = ManosUtil.LeerPalma(esqueleto, true);
        if (!palma.valida || palma.cierre > 1.2f)
            return false; // los dedos deben estar cerrados (puño)
        Transform basePulgar = ManosUtil.Hueso(esqueleto, "Thumb1", "ThumbMetacarpal");
        Transform nudillo = ManosUtil.Hueso(esqueleto, "Index1", "IndexProximal");
        if (basePulgar == null || nudillo == null)
            return false;
        Vector3 dir = Izq.pulgar - basePulgar.position;
        if (dir.magnitude < 0.05f || Vector3.Distance(Izq.pulgar, nudillo.position) < 0.055f)
            return false; // el pulgar debe estar estirado
        Vector3 izquierda = -Cabeza.right;
        izquierda.y = 0f;
        if (izquierda.sqrMagnitude < 1e-4f)
            return false;
        izquierda.Normalize();
        return Vector3.Dot(dir.normalized, izquierda) > 0.6f;
    }

    // ---------- Tocar un relleno para cambiar su color ----------

    void RevisarToqueRelleno()
    {
        if (!Der.valida)
        {
            tocandoRelleno = false;
            return;
        }
        Vector3 local = dibujo.transform.InverseTransformPoint(Der.indice);
        float grosor = 0.01f / dibujo.EscalaMundo;
        Trazo tocado = null;
        foreach (var t in dibujo.trazos)
        {
            if (t != null && t.cerrado && t.DentroDeRelleno(local, grosor))
            {
                tocado = t;
                break;
            }
        }
        if (tocado != null && !tocandoRelleno && Time.time >= proximoToque)
        {
            dibujo.CambiarColorRelleno(tocado);
            proximoToque = Time.time + 0.5f;
        }
        tocandoRelleno = tocado != null;
    }

    // ---------- Cursor y panel ----------

    void ActualizarCursor()
    {
        if (cursor == null)
            return;
        bool ver = Der.valida && GestoIzq != Gesto.Transformar;
        if (cursor.gameObject.activeSelf != ver)
            cursor.gameObject.SetActive(ver);
        if (!ver)
            return;
        cursor.position = Der.indice;
        cursor.localScale = Vector3.one * Mathf.Max(0.003f, dibujo.anchoPincel);
    }

    // El panel se ve cuando la palma izquierda te mira y no estás haciendo un gesto.
    public bool PuedeVerPanel(float umbral, out Vector3 posicion, out Quaternion rotacion)
    {
        posicion = Vector3.zero;
        rotacion = Quaternion.identity;
        if (GestoIzq != Gesto.Ninguno || !Izq.valida || Cabeza == null)
            return false;
        var palma = ManosUtil.LeerPalma(Izq.esqueleto, true);
        if (!palma.valida)
            return false;
        Vector3 haciaCabeza = (Cabeza.position - palma.centro).normalized;
        if (Vector3.Dot(palma.normal, haciaCabeza) < umbral)
            return false;
        posicion = palma.centro + palma.normal * 0.09f + Vector3.up * 0.03f;
        Vector3 mirar = posicion - Cabeza.position;
        if (mirar.sqrMagnitude < 1e-6f)
            return false;
        rotacion = Quaternion.LookRotation(mirar, Vector3.up);
        return true;
    }
}

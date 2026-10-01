using System.Collections.Generic;
using UnityEngine;

// Los gestos de TrazoVR (todo con las manos):
//  Izquierda pulgar + ÍNDICE (sostener)  -> dibujar con la punta del índice derecho.
//  Izquierda pulgar + MEDIO  (sostener)  -> modo nodos: pellizca con la derecha para mover nodos y asas.
//       Arrastra la punta de una línea sobre otra punta: se unen como imán (o se cierra la figura).
//  Izquierda pulgar + ANULAR (sostener)  -> grosor: sube/baja la mano izquierda = todo más grueso/delgado;
//       pellizca un nodo con la derecha y súbela/bájala = grosor solo de ese nodo.
//  Izquierda PUÑO (pulgar escondido)      -> borrador: lo que toques con el índice derecho se borra.
//  Izquierda puño con el PULGAR hacia tu izquierda -> deshacer (la mano destella).
//  Izquierda abierta con el pulgar tocando la base de los dedos -> menú.
//  LAS DOS manos pellizcando (índice + pulgar) -> escalar, girar (como volante) y mover todo.
//  Tocar un relleno con el índice derecho -> cambiar su color.
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
    public Material materialCursorBorrar;
    [Tooltip("Color con el que destella la mano al deshacer")]
    public Material materialDestelloMano;

    [Header("Gestos (en metros)")]
    public float pellizcoEntra = 0.02f;
    public float pellizcoSale = 0.035f;
    [Tooltip("Segundos que hay que sostener un gesto antes de que cuente")]
    public float confirmarGesto = 0.08f;
    [Tooltip("Más alto = sigue más rápido al dedo; más bajo = más suave")]
    public float suavizado = 16f;
    public float radioAgarreNodo = 0.025f;
    public float radioBorrarNodo = 0.015f;
    public float tamanoNodo = 0.008f;
    [Tooltip("Cuánto cambia el grosor al subir/bajar la mano")]
    public float sensibilidadGrosor = 4f;

    public enum Gesto { Ninguno, Dibujar, Nodos, Grosor, Transformar, Borrar }
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

    // Pose de la mano izquierda (se calcula una vez por cuadro)
    ManosUtil.Palma palmaIzq;
    bool poseValida;
    float curvaIndice, curvaMedio, curvaAnular;  // ~0.5-1 doblado, ~1.5+ estirado
    float pulgarANudillo;                         // punta del pulgar al nudillo del índice
    Vector3 basePulgar;
    Vector3 baseDedos;                            // donde toca el pulgar para abrir el menú

    // Nodos y asas visibles
    readonly List<Transform> nodosVisibles = new List<Transform>();
    readonly List<Renderer> nodosRender = new List<Renderer>();
    readonly List<MeshFilter> nodosFiltro = new List<MeshFilter>();
    readonly Transform[] asasVisibles = new Transform[2];
    GameObject lineasAsas;
    Mesh mallaLineasAsas;
    readonly List<Vector3> puntosAsas = new List<Vector3>();
    readonly List<int> indicesAsas = new List<int>();
    static Mesh mallaDisco, mallaAnillo;

    // Arrastre en modo nodos
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

    // Grosor
    float alturaInicialGrosor;
    float factorGlobal = 1f;
    Trazo grosorTrazo;
    int grosorIndice = -1;
    float grosorNodoInicio = 1f;
    float alturaDerGrosor;

    // Borrador, deshacer, rellenos
    bool tocandoBorrar;
    float tiempoDeshacer;
    bool esperarSoltarDeshacer;
    bool tocandoRelleno;
    float proximoToque;

    // Destello de la mano
    Renderer[] rendsMano;
    Material[][] materialesMano;
    float finDestelloMano;

    Transform cursor;
    Renderer cursorRender;

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
        cursorRender = esfera.GetComponent<Renderer>();
        if (materialCursor != null)
            cursorRender.sharedMaterial = materialCursor;
        cursorRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        cursorRender.receiveShadows = false;
        cursor = esfera.transform;
        cursor.gameObject.SetActive(false);

        for (int i = 0; i < 2; i++)
        {
            asasVisibles[i] = CrearCirculo("Asa", materialAsa).transform;
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
        LeerPoseIzquierda();

        ActualizarGestoIzquierdo();

        switch (GestoIzq)
        {
            case Gesto.Dibujar: Dibujar(); break;
            case Gesto.Nodos: EditarNodos(); break;
            case Gesto.Grosor: CambiarGrosor(); break;
            case Gesto.Borrar: Borrar(); break;
            case Gesto.Transformar: if (caja != null) caja.Actualizar(Izq, Der); break;
            default:
                RevisarDeshacer();
                RevisarToqueRelleno();
                break;
        }

        if (GestoIzq != Gesto.Nodos && GestoIzq != Gesto.Grosor && GestoIzq != Gesto.Borrar)
            OcultarModoNodos();
        ActualizarCursor();
        if (finDestelloMano > 0f && Time.time > finDestelloMano)
            RestaurarMano();
    }

    // ---------- Pose de la mano izquierda ----------

    void LeerPoseIzquierda()
    {
        poseValida = false;
        if (!Izq.valida)
            return;
        var esq = Izq.esqueleto;
        palmaIzq = ManosUtil.LeerPalma(esq, true);
        Transform nudilloIndice = ManosUtil.Hueso(esq, "Index1", "IndexProximal");
        Transform nudilloMedio = ManosUtil.Hueso(esq, "Middle1", "MiddleProximal");
        Transform muneca = ManosUtil.Hueso(esq, "WristRoot", "Wrist");
        Transform pulgar = ManosUtil.Hueso(esq, "Thumb1", "ThumbMetacarpal");
        if (!palmaIzq.valida || nudilloIndice == null || nudilloMedio == null || muneca == null || pulgar == null)
            return;
        float tam = palmaIzq.tamano;
        curvaIndice = Vector3.Distance(Izq.indice, palmaIzq.centro) / tam;
        curvaMedio = Vector3.Distance(Izq.medio, palmaIzq.centro) / tam;
        curvaAnular = Vector3.Distance(Izq.anular, palmaIzq.centro) / tam;
        pulgarANudillo = Vector3.Distance(Izq.pulgar, nudilloIndice.position);
        basePulgar = pulgar.position;
        baseDedos = Vector3.Lerp(Vector3.Lerp(nudilloIndice.position, nudilloMedio.position, 0.35f), muneca.position, 0.2f);
        poseValida = true;
    }

    // Puño: los tres dedos doblados y el pulgar escondido cerca del índice.
    bool EsPuno()
    {
        return poseValida && curvaIndice < 1.05f && curvaMedio < 1.05f && curvaAnular < 1.05f && pulgarANudillo < 0.05f;
    }

    bool DedosAbiertos()
    {
        return poseValida && curvaIndice > 1.25f && curvaMedio > 1.25f && curvaAnular > 1.2f;
    }

    // ---------- Mano izquierda: qué gesto hace ----------

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

        if (GestoIzq == Gesto.Borrar)
        {
            // Se sale del borrador al abrir la mano o sacar el pulgar.
            bool sigue = poseValida && curvaIndice < 1.2f && curvaMedio < 1.2f && curvaAnular < 1.2f && pulgarANudillo < 0.06f;
            if (!sigue && poseValida)
                SalirDeGesto();
            return;
        }
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
        if (EsPuno())
        {
            nuevo = Gesto.Borrar;
        }
        else
        {
            float menor = pellizcoEntra;
            if (dIndice < menor) { menor = dIndice; nuevo = Gesto.Dibujar; }
            if (dMedio < menor) { menor = dMedio; nuevo = Gesto.Nodos; }
            if (dAnular < menor) { nuevo = Gesto.Grosor; }
        }

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
        if (dibujo.animacion != null)
            dibujo.animacion.Pausar();
        if (g == Gesto.Grosor)
        {
            alturaInicialGrosor = Izq.pulgar.y;
            factorGlobal = 1f;
            grosorTrazo = null;
            dibujo.EmpezarGrosor();
        }
        else if (g == Gesto.Transformar)
        {
            if (caja != null)
                caja.Empezar(Izq, Der);
        }
        else if (g == Gesto.Borrar)
        {
            tocandoBorrar = true; // no borra lo que ya estaba tocando al cerrar el puño
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
        grosorTrazo = null;
        hoverTipo = Objetivo.Nada;
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

    // ---------- Modo nodos (mover nodos y asas, imán) ----------

    void EditarNodos()
    {
        if (!Der.valida)
        {
            if (arrastre != Objetivo.Nada)
                TerminarArrastre();
            MostrarModoNodos(true);
            return;
        }
        Vector3 pinza = Der.PuntoPellizco;

        if (arrastre != Objetivo.Nada && !Der.pellizco)
            TerminarArrastre();

        if (arrastre != Objetivo.Nada)
        {
            ContinuarArrastre(pinza);
            MostrarModoNodos(true);
            return;
        }

        Objetivo tipo = Objetivo.Nada;
        Trazo t;
        int i;
        bool salida = false;
        if (BuscarNodoCercano(Der.indice, pinza, radioAgarreNodo, out t, out i))
            tipo = Objetivo.Nodo;
        else if (BuscarAsaCercana(Der.indice, pinza, out salida))
        {
            tipo = Objetivo.Asa;
            t = selTrazo;
            i = selIndice;
        }
        hoverTipo = tipo;
        hoverTrazo = t;
        hoverIndice = i;
        hoverSalida = salida;

        if (Der.empezoPellizco && (tipo == Objetivo.Nodo || tipo == Objetivo.Asa))
            EmpezarArrastre(tipo, t, i, salida, pinza);
        MostrarModoNodos(true);
    }

    bool BuscarNodoCercano(Vector3 a, Vector3 b, float radio, out Trazo trazo, out int indice)
    {
        trazo = null;
        indice = -1;
        float mejor = radio;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t))
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

    bool SeleccionValida => Dibujo.Editable(selTrazo) && selIndice >= 0 && selIndice < selTrazo.nodos.Count;

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

    // ---------- Grosor (todo, o un nodo) ----------

    void CambiarGrosor()
    {
        if (grosorTrazo != null)
        {
            if (!Der.valida || !Der.pellizco || grosorIndice >= grosorTrazo.nodos.Count)
            {
                // Al soltar el nodo, el grosor general sigue desde donde quedó.
                grosorTrazo = null;
                alturaInicialGrosor = Izq.pulgar.y - Mathf.Log(Mathf.Max(0.01f, factorGlobal)) / sensibilidadGrosor;
            }
            else
            {
                float m = grosorNodoInicio * Mathf.Exp((Der.PuntoPellizco.y - alturaDerGrosor) * sensibilidadGrosor);
                grosorTrazo.PonerGrosorNodo(grosorIndice, m);
            }
        }
        else
        {
            Trazo t = null;
            int i = -1;
            bool cerca = Der.valida && BuscarNodoCercano(Der.indice, Der.PuntoPellizco, radioAgarreNodo, out t, out i);
            hoverTipo = cerca ? Objetivo.Nodo : Objetivo.Nada;
            hoverTrazo = t;
            hoverIndice = i;
            if (cerca && Der.empezoPellizco)
            {
                grosorTrazo = t;
                grosorIndice = i;
                grosorNodoInicio = t.GrosorDeNodo(i);
                alturaDerGrosor = Der.PuntoPellizco.y;
            }
            else if (Izq.valida)
            {
                factorGlobal = Mathf.Exp((Izq.pulgar.y - alturaInicialGrosor) * sensibilidadGrosor);
                dibujo.AplicarFactorGrosor(factorGlobal);
            }
        }
        MostrarModoNodos(false);
    }

    // ---------- Borrador: puño izquierdo + tocar con el índice derecho ----------

    void Borrar()
    {
        hoverTipo = Objetivo.Nada;
        if (!Der.valida)
        {
            tocandoBorrar = false;
            MostrarModoNodos(false);
            return;
        }
        Vector3 punta = Der.indice;
        Vector3 local = dibujo.transform.InverseTransformPoint(punta);
        float escala = dibujo.EscalaMundo;

        Objetivo tipo = Objetivo.Nada;
        Trazo t;
        int i;
        if (BuscarNodoCercano(punta, punta, radioBorrarNodo, out t, out i))
        {
            tipo = Objetivo.Nodo;
        }
        else
        {
            float mejor = float.MaxValue;
            foreach (var o in dibujo.trazos)
            {
                if (!Dibujo.Editable(o))
                    continue;
                float d = o.DistanciaACurva(local) * escala;
                float limite = o.ancho * escala * 0.5f + 0.008f;
                if (d < limite && d < mejor)
                {
                    mejor = d;
                    t = o;
                    tipo = Objetivo.Linea;
                }
            }
            if (tipo == Objetivo.Nada)
            {
                foreach (var o in dibujo.trazos)
                {
                    if (Dibujo.Editable(o) && o.relleno && o.DentroDeRelleno(local, 0.012f / escala))
                    {
                        t = o;
                        tipo = Objetivo.Relleno;
                        break;
                    }
                }
            }
        }
        hoverTipo = tipo;
        hoverTrazo = t;
        hoverIndice = i;

        bool toca = tipo != Objetivo.Nada;
        if (toca && !tocandoBorrar && t != null)
        {
            // Solo borra al "entrar" en algo: así no se borra la línea entera después de un nodo.
            dibujo.GuardarParaDeshacer();
            if (tipo == Objetivo.Nodo)
            {
                dibujo.Destello(dibujo.transform.TransformPoint(t.nodos[i]), 0.02f);
                dibujo.QuitarNodo(t, i);
            }
            else if (tipo == Objetivo.Linea)
            {
                dibujo.BorrarTrazo(t, true);
            }
            else
            {
                dibujo.Destello(punta, 0.04f);
                dibujo.QuitarRelleno(t);
            }
            hoverTipo = Objetivo.Nada;
        }
        tocandoBorrar = toca;
        MostrarModoNodos(false);
    }

    // ---------- Ver nodos y asas ----------

    void MostrarModoNodos(bool conAsas)
    {
        if (!SeleccionValida)
        {
            selTrazo = null;
            selIndice = -1;
        }

        int n = 0;
        int imanIndice = imanTrazo != null ? (imanExtremo == 0 ? 0 : imanTrazo.nodos.Count - 1) : -1;
        Vector3 cabeza = Cabeza != null ? Cabeza.position : Vector3.zero;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t))
                continue;
            for (int i = 0; i < t.nodos.Count; i++)
            {
                if (n >= nodosVisibles.Count)
                {
                    var go = CrearCirculo("Nodo", materialNodo);
                    nodosVisibles.Add(go.transform);
                    nodosRender.Add(go.GetComponent<Renderer>());
                    nodosFiltro.Add(go.GetComponent<MeshFilter>());
                }
                var nodo = nodosVisibles[n];
                if (!nodo.gameObject.activeSelf)
                    nodo.gameObject.SetActive(true);
                Vector3 pos = dibujo.transform.TransformPoint(t.nodos[i]);
                nodo.position = pos;
                Vector3 mirar = pos - cabeza;
                if (mirar.sqrMagnitude > 1e-8f)
                    nodo.rotation = Quaternion.LookRotation(mirar);

                bool activo = (t == arrTrazo && i == arrIndice)
                              || (t == grosorTrazo && i == grosorIndice)
                              || (hoverTipo == Objetivo.Nodo && t == hoverTrazo && i == hoverIndice)
                              || (conAsas && t == selTrazo && i == selIndice);
                bool iman = t == imanTrazo && i == imanIndice;
                bool punta = !t.cerrado && (i == 0 || i == t.nodos.Count - 1);
                var malla = punta ? MallaAnillo() : MallaDisco();
                if (nodosFiltro[n].sharedMesh != malla)
                    nodosFiltro[n].sharedMesh = malla;
                float tam = tamanoNodo * (punta ? 1.3f : 1f) * (activo || iman ? 1.5f : 1f);
                nodo.localScale = Vector3.one * tam;
                var mat = iman ? materialIman : activo ? materialNodoActivo : materialNodo;
                if (mat != null && nodosRender[n].sharedMaterial != mat)
                    nodosRender[n].sharedMaterial = mat;
                n++;
            }
        }
        OcultarNodosDesde(n);
        MostrarAsas(conAsas);
    }

    // Las asas (como en Illustrator) se ven solo en el nodo seleccionado.
    void MostrarAsas(bool permitir)
    {
        puntosAsas.Clear();
        indicesAsas.Clear();
        bool hay = false;
        Vector3 cabeza = Cabeza != null ? Cabeza.position : Vector3.zero;
        for (int k = 0; k < 2; k++)
        {
            bool esSalida = k == 1;
            bool ver = permitir && SeleccionValida && selTrazo.AsaUsada(selIndice, esSalida);
            var asa = asasVisibles[k];
            if (asa == null)
                continue;
            if (asa.gameObject.activeSelf != ver)
                asa.gameObject.SetActive(ver);
            if (!ver)
                continue;
            Vector3 punta = PuntaAsaMundo(selTrazo, selIndice, esSalida);
            asa.position = punta;
            Vector3 mirar = punta - cabeza;
            if (mirar.sqrMagnitude > 1e-8f)
                asa.rotation = Quaternion.LookRotation(mirar);
            bool activa = (arrastre == Objetivo.Asa && arrSalida == esSalida)
                          || (arrastre == Objetivo.Nada && hoverTipo == Objetivo.Asa && hoverSalida == esSalida);
            asa.localScale = Vector3.one * tamanoNodo * (activa ? 1.2f : 0.8f);
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

    GameObject CrearCirculo(string nombre, Material mat)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = MallaDisco();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    // Círculo plano (siempre mira hacia ti). Diámetro 1.
    static Mesh MallaDisco()
    {
        if (mallaDisco != null)
            return mallaDisco;
        const int lados = 20;
        var v = new Vector3[lados + 1];
        var tri = new int[lados * 3];
        v[0] = Vector3.zero;
        for (int i = 0; i < lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            v[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.5f;
        }
        for (int i = 0; i < lados; i++)
        {
            tri[i * 3] = 0;
            tri[i * 3 + 1] = 1 + (i + 1) % lados;
            tri[i * 3 + 2] = 1 + i;
        }
        mallaDisco = new Mesh { name = "Disco" };
        mallaDisco.vertices = v;
        mallaDisco.triangles = tri;
        mallaDisco.RecalculateNormals();
        mallaDisco.RecalculateBounds();
        return mallaDisco;
    }

    // Aro (círculo hueco), para las puntas de las líneas.
    static Mesh MallaAnillo()
    {
        if (mallaAnillo != null)
            return mallaAnillo;
        const int lados = 20;
        var v = new Vector3[lados * 2];
        var tri = new int[lados * 6];
        for (int i = 0; i < lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            v[i * 2] = dir * 0.5f;      // borde de afuera
            v[i * 2 + 1] = dir * 0.3f;  // borde de adentro
        }
        for (int i = 0; i < lados; i++)
        {
            int o0 = i * 2, i0 = i * 2 + 1;
            int o1 = ((i + 1) % lados) * 2, i1 = ((i + 1) % lados) * 2 + 1;
            tri[i * 6] = o0; tri[i * 6 + 1] = i0; tri[i * 6 + 2] = o1;
            tri[i * 6 + 3] = i0; tri[i * 6 + 4] = i1; tri[i * 6 + 5] = o1;
        }
        mallaAnillo = new Mesh { name = "Anillo" };
        mallaAnillo.vertices = v;
        mallaAnillo.triangles = tri;
        mallaAnillo.RecalculateNormals();
        mallaAnillo.RecalculateBounds();
        return mallaAnillo;
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
        if (tiempoDeshacer < 0.12f)
            return;
        esperarSoltarDeshacer = true;
        if (dibujo.Deshacer())
            DestellarMano(Izq);
    }

    bool PoseDeshacer()
    {
        if (!poseValida || Cabeza == null)
            return false;
        // Dedos doblados (no hace falta un puño perfecto).
        if (curvaIndice > 1.3f || curvaMedio > 1.3f || curvaAnular > 1.3f)
            return false;
        // Pulgar estirado y lejos del índice.
        Vector3 dir = Izq.pulgar - basePulgar;
        if (dir.magnitude < 0.04f || pulgarANudillo < 0.05f)
            return false;
        Vector3 izquierda = -Cabeza.right;
        izquierda.y = 0f;
        if (izquierda.sqrMagnitude < 1e-4f)
            return false;
        izquierda.Normalize();
        return Vector3.Dot(dir.normalized, izquierda) > 0.5f;
    }

    // La mano cambia de color un instante para confirmar la acción.
    void DestellarMano(ManoSeguida mano)
    {
        if (mano == null || mano.hand == null || materialDestelloMano == null)
            return;
        RestaurarMano();
        rendsMano = mano.hand.GetComponentsInChildren<Renderer>(true);
        materialesMano = new Material[rendsMano.Length][];
        for (int i = 0; i < rendsMano.Length; i++)
        {
            materialesMano[i] = rendsMano[i].sharedMaterials;
            var nuevos = new Material[materialesMano[i].Length];
            for (int k = 0; k < nuevos.Length; k++)
                nuevos[k] = materialDestelloMano;
            rendsMano[i].sharedMaterials = nuevos;
        }
        finDestelloMano = Time.time + 0.3f;
    }

    void RestaurarMano()
    {
        if (rendsMano != null && materialesMano != null)
            for (int i = 0; i < rendsMano.Length; i++)
                if (rendsMano[i] != null)
                    rendsMano[i].sharedMaterials = materialesMano[i];
        rendsMano = null;
        materialesMano = null;
        finDestelloMano = 0f;
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
            if (Dibujo.Editable(t) && t.cerrado && t.DentroDeRelleno(local, grosor))
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

    // ---------- Cursor y menú ----------

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
        bool borrando = GestoIzq == Gesto.Borrar;
        var mat = borrando && materialCursorBorrar != null ? materialCursorBorrar : materialCursor;
        if (mat != null && cursorRender.sharedMaterial != mat)
            cursorRender.sharedMaterial = mat;
        cursor.localScale = Vector3.one * (borrando ? 0.012f : Mathf.Max(0.003f, dibujo.anchoPincel));
    }

    // Menú: mano izquierda abierta con la punta del pulgar en la base de los dedos.
    // Los botones aparecen unos centímetros hacia ti y siguen a la mano.
    public bool PuedeVerPanel(bool yaVisible, out Vector3 posicion, out Quaternion rotacion)
    {
        posicion = Vector3.zero;
        rotacion = Quaternion.identity;
        if (GestoIzq != Gesto.Ninguno || !poseValida || Cabeza == null)
            return false;
        if (!DedosAbiertos())
            return false;
        float d = Vector3.Distance(Izq.pulgar, baseDedos);
        if (d > (yaVisible ? 0.05f : 0.035f))
            return false;
        Vector3 haciaCabeza = Cabeza.position - palmaIzq.centro;
        if (haciaCabeza.sqrMagnitude < 1e-6f)
            return false;
        posicion = palmaIzq.centro + haciaCabeza.normalized * 0.08f + Vector3.up * 0.02f;
        Vector3 mirar = posicion - Cabeza.position;
        if (mirar.sqrMagnitude < 1e-6f)
            return false;
        rotacion = Quaternion.LookRotation(mirar, Vector3.up);
        return true;
    }
}

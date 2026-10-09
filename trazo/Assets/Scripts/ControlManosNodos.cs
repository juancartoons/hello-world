using System.Collections.Generic;
using UnityEngine;

// Nodos con el DEDO (parte de ControlManos). Mano izquierda: pulgar + medio = modo nodos.
//  - TOCAR un nodo (o un asa) con el índice derecho = se pega al dedo y lo sigue.
//    Para SOLTARLO: abre los dedos de la mano izquierda.
//  - ÍNDICE + MEDIO juntos (como diciendo "dos") al tocar un nodo = PLASTILINA: los vecinos de la misma
//    línea lo siguen suave (más cerca, más se mueven) y brillan en naranja clarito.
//  - LAZO: empieza en un espacio vacío y dibuja un círculo alrededor de varios nodos: quedan naranjas.
//    Toca uno de ellos y se mueven todos juntos (exactos, sin plastilina).
//  - Tocar la línea elegida (lejos de sus nodos) y quedarte quieto medio segundo = nodo nuevo, pegado al dedo.
//  - Tocar otra línea = elegirla (y ver solo sus nodos).
//  - Al entrar al modo, si el dedo ya estaba sobre algo, no agarra nada hasta que salga y vuelva a tocar.
// Al abrir la mano izquierda todo se suelta y se deseleccionan los del lazo.
// (Respaldo de la versión con pellizco: trazo/Respaldos/nodos-con-pellizco-v26.)
public partial class ControlManos
{
    const float RadioToqueNodo = 0.018f;     // metros: qué tan cerca hay que tocar un nodo
    const float AlcancePlastilina = 0.08f;   // metros: hasta dónde llegan los vecinos de la plastilina
    const float EsperaNodoNuevo = 0.5f;      // segundos quieto sobre la línea para crear un nodo
    const float LargoMinimoLazo = 0.12f;     // metros que hay que recorrer antes de poder cerrar el lazo
    const float CierreLazo = 0.03f;          // metros del inicio para que el lazo se cierre

    // Un nodo de un grupo (lazo o plastilina): su línea, cuál es, dónde estaba y cuánto se mueve (0..1).
    struct NodoGrupo
    {
        public Trazo t;
        public int i;
        public Vector3 inicio;
        public float peso;
    }

    readonly List<NodoGrupo> grupo = new List<NodoGrupo>();          // los que se están moviendo juntos
    readonly List<NodoGrupo> elegidosLazo = new List<NodoGrupo>();   // los elegidos con el lazo
    readonly HashSet<Trazo> trazosGrupo = new HashSet<Trazo>();
    bool moviendoGrupo, deshacerGrupoPendiente, grupoPlastilina, dedoListoNodos, esperarSalirLinea;
    Vector3 grupoInicioLocal, grupoDesfase;
    readonly List<Vector3> lazo = new List<Vector3>();
    readonly List<Vector2> lazo2D = new List<Vector2>();
    float largoLazo, lazoDesde;
    LineRenderer lineaLazo;
    Trazo lineaEspera, lineaBajoDedo;
    Vector3 puntoEspera;
    float lineaBajoDesde;
    float esperaDesde = -1f;
    Material materialNodoPlastilina;

    // Al entrar al modo nodos.
    void EmpezarModoNodos()
    {
        dedoListoNodos = false;
        esperarSalirLinea = false;
        moviendoGrupo = false;
        grupo.Clear();
        elegidosLazo.Clear();
        CancelarLazo();
        esperaDesde = -1f;
    }

    // Al salir del modo nodos (abrir la mano izquierda): se suelta todo.
    void TerminarModoNodos()
    {
        moviendoGrupo = false;
        grupoPlastilina = false;
        grupo.Clear();
        elegidosLazo.Clear();
        CancelarLazo();
        esperaDesde = -1f;
    }

    void EditarNodosConDedo()
    {
        Trazo solo = dibujo.Seleccion;
        if (!Der.valida)
        {
            // Si la mano derecha se pierde un momento, lo agarrado se queda quieto (no se suelta).
            MostrarModoNodos(true, solo);
            return;
        }
        Vector3 punta = Der.indice;

        // 1. Algo agarrado: sigue al dedo (se suelta al abrir la mano izquierda).
        if (moviendoGrupo)
        {
            MoverGrupo(punta);
            MostrarModoNodos(true, solo);
            return;
        }
        if (arrastre != Objetivo.Nada)
        {
            ContinuarArrastre(punta);
            MostrarModoNodos(true, dibujo.Seleccion);
            return;
        }

        // 2. ¿Qué toca el dedo?
        Trazo t;
        int i;
        bool salida = false;
        Objetivo tipo = Objetivo.Nada;
        if (BuscarNodoCercano(punta, punta, RadioToqueNodo, solo, out t, out i))
        {
            tipo = Objetivo.Nodo;
        }
        else if (BuscarAsaCercana(punta, punta, out salida))
        {
            tipo = Objetivo.Asa;
            t = selTrazo;
            i = selIndice;
        }
        Trazo linea = tipo == Objetivo.Nada ? LineaBajo(punta, punta) : null;
        if (linea != lineaBajoDedo)
        {
            lineaBajoDedo = linea;
            lineaBajoDesde = Time.time;
        }
        // Durante un lazo, pasar rápido por encima de una línea no la elige: hay que detenerse un momento en ella.
        bool lineaFirme = linea != null && (largoLazo < 0.03f || Time.time - lineaBajoDesde > 0.25f);
        hoverTipo = tipo;
        hoverTrazo = t;
        hoverIndice = i;
        hoverSalida = salida;
        bool tocaAlgo = tipo != Objetivo.Nada || linea != null;
        if (!dedoListoNodos)
        {
            // Recién entraste al modo: espera a que el dedo salga de lo que estaba tocando.
            if (!tocaAlgo)
                dedoListoNodos = true;
            MostrarModoNodos(true, solo);
            return;
        }

        // 3. Tocó un nodo: se agarra (con su grupo del lazo, con plastilina o él solo).
        if (tipo == Objetivo.Nodo)
        {
            CancelarLazo();
            esperaDesde = -1f;
            if (EnLazo(t, i))
            {
                EmpezarGrupo(t, i, punta, false);
            }
            else if (Der.DosDedos)
            {
                ArmarPlastilina(t, i);
                EmpezarGrupo(t, i, punta, true);
            }
            else
            {
                dibujo.Seleccionar(t);
                EmpezarArrastre(Objetivo.Nodo, t, i, false, punta);
            }
            MostrarModoNodos(true, dibujo.Seleccion);
            return;
        }
        if (tipo == Objetivo.Asa)
        {
            CancelarLazo();
            esperaDesde = -1f;
            EmpezarArrastre(Objetivo.Asa, t, i, salida, punta);
            MostrarModoNodos(true, dibujo.Seleccion);
            return;
        }

        // 4. Tocó una línea (lejos de sus nodos): elegirla, o (si ya es la elegida) quedarse quieto = nodo nuevo.
        if (lineaFirme)
        {
            CancelarLazo();
            if (linea != dibujo.Seleccion)
            {
                dibujo.Seleccionar(linea);
                esperarSalirLinea = true; // para el nodo nuevo, primero hay que salir y volver a tocar
                esperaDesde = -1f;
            }
            else if (!esperarSalirLinea)
            {
                if (esperaDesde < 0f || lineaEspera != linea || Vector3.Distance(punta, puntoEspera) > 0.006f)
                {
                    lineaEspera = linea;
                    puntoEspera = punta;
                    esperaDesde = Time.time;
                }
                else if (Time.time - esperaDesde > EsperaNodoNuevo)
                {
                    esperaDesde = -1f;
                    AgregarNodoEn(linea, punta);
                }
            }
            MostrarModoNodos(true, dibujo.Seleccion);
            return;
        }
        esperarSalirLinea = false;
        esperaDesde = -1f;

        // 5. Espacio vacío: el lazo.
        ActualizarLazo(punta);
        MostrarModoNodos(true, solo);
    }

    // ---------- Grupo (lazo o plastilina) ----------

    bool EnLazo(Trazo t, int i)
    {
        foreach (var g in elegidosLazo)
            if (g.t == t && g.i == i)
                return true;
        return false;
    }

    // Cuánto se mueve este nodo con el grupo que se está moviendo (-1 = no está).
    float PesoEnGrupo(Trazo t, int i)
    {
        if (!moviendoGrupo)
            return -1f;
        foreach (var g in grupo)
            if (g.t == t && g.i == i)
                return g.peso;
        return -1f;
    }

    // Plastilina: los nodos de la misma línea cerca del tocado, con su peso (1 el tocado, 0 en el borde).
    void ArmarPlastilina(Trazo t, int tocado)
    {
        grupo.Clear();
        Vector3 centro = dibujo.transform.TransformPoint(t.nodos[tocado]);
        for (int j = 0; j < t.nodos.Count; j++)
        {
            float d = Vector3.Distance(dibujo.transform.TransformPoint(t.nodos[j]), centro);
            if (d >= AlcancePlastilina)
                continue;
            float x = d / AlcancePlastilina;
            float peso = (1f - x * x) * (1f - x * x);
            grupo.Add(new NodoGrupo { t = t, i = j, inicio = t.nodos[j], peso = j == tocado ? 1f : peso });
        }
    }

    void EmpezarGrupo(Trazo t, int tocado, Vector3 punta, bool plastilina)
    {
        if (!plastilina)
        {
            grupo.Clear();
            foreach (var g in elegidosLazo)
                if (Dibujo.Editable(g.t) && g.i < g.t.nodos.Count)
                    grupo.Add(new NodoGrupo { t = g.t, i = g.i, inicio = g.t.nodos[g.i], peso = 1f });
        }
        if (grupo.Count == 0)
            return;
        grupoPlastilina = plastilina;
        grupoInicioLocal = t.nodos[tocado];
        grupoDesfase = dibujo.transform.TransformPoint(grupoInicioLocal) - punta;
        moviendoGrupo = true;
        deshacerGrupoPendiente = true;
        selTrazo = t;
        selIndice = tocado;
        Burbuja(punta, 0.8f);
    }

    void MoverGrupo(Vector3 punta)
    {
        Vector3 objetivo = dibujo.ProyectarEnPlano(dibujo.transform.InverseTransformPoint(punta + grupoDesfase));
        Vector3 delta = objetivo - grupoInicioLocal;
        if (deshacerGrupoPendiente)
        {
            // Se guarda "deshacer" solo cuando de verdad se mueve algo.
            if (delta.magnitude * dibujo.EscalaMundo < 0.003f)
                return;
            dibujo.GuardarParaDeshacer();
            deshacerGrupoPendiente = false;
        }
        trazosGrupo.Clear();
        foreach (var g in grupo)
        {
            if (g.t == null || g.i >= g.t.nodos.Count)
                continue;
            g.t.MoverNodoSinReconstruir(g.i, dibujo.ProyectarEnPlano(g.inicio + delta * g.peso));
            trazosGrupo.Add(g.t);
        }
        foreach (var tr in trazosGrupo)
            tr.Reconstruir();
    }

    // ---------- Lazo ----------

    void ActualizarLazo(Vector3 punta)
    {
        if (lazo.Count == 0 || Time.time - lazoDesde > 8f || largoLazo > 1.5f)
        {
            // Empieza (o vuelve a empezar) aquí.
            lazo.Clear();
            lazo.Add(punta);
            largoLazo = 0f;
            lazoDesde = Time.time;
            DibujarLazo();
            return;
        }
        float paso = Vector3.Distance(lazo[lazo.Count - 1], punta);
        if (paso > 0.005f)
        {
            lazo.Add(punta);
            largoLazo += paso;
        }
        // Se cierra al volver cerca del inicio (después de dar una vuelta).
        if (largoLazo > LargoMinimoLazo && Vector3.Distance(punta, lazo[0]) < CierreLazo)
        {
            CerrarLazo();
            return;
        }
        DibujarLazo();
    }

    void DibujarLazo()
    {
        // La raya del lazo aparece cuando ya se nota que estás dando una vuelta (no al mover el dedo cualquiera).
        bool ver = largoLazo > 0.05f && lazo.Count >= 2;
        if (!ver)
        {
            if (lineaLazo != null && lineaLazo.positionCount > 0)
                lineaLazo.positionCount = 0;
            return;
        }
        if (lineaLazo == null)
        {
            var go = new GameObject("Lazo");
            go.transform.SetParent(transform, false);
            lineaLazo = go.AddComponent<LineRenderer>();
            lineaLazo.useWorldSpace = true;
            lineaLazo.widthMultiplier = 0.0025f;
            lineaLazo.numCapVertices = 3;
            lineaLazo.numCornerVertices = 2;
            lineaLazo.sharedMaterial = materialNodoActivo != null ? materialNodoActivo : materialNodo;
            lineaLazo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineaLazo.receiveShadows = false;
        }
        lineaLazo.positionCount = lazo.Count;
        for (int k = 0; k < lazo.Count; k++)
            lineaLazo.SetPosition(k, lazo[k]);
    }

    void CancelarLazo()
    {
        lazo.Clear();
        largoLazo = 0f;
        if (lineaLazo != null)
            lineaLazo.positionCount = 0;
    }

    // Los nodos que quedaron dentro del lazo (visto desde tus ojos) quedan elegidos.
    void CerrarLazo()
    {
        Vector3 der = Cabeza != null ? Cabeza.right : Vector3.right;
        Vector3 arriba = Cabeza != null ? Cabeza.up : Vector3.up;
        Vector3 ojo = Cabeza != null ? Cabeza.position : Vector3.zero;
        Vector3 adelante = Cabeza != null ? Cabeza.forward : Vector3.forward;
        lazo2D.Clear();
        foreach (var p in lazo)
            lazo2D.Add(Proyectar(p, ojo, der, arriba, adelante));
        elegidosLazo.Clear();
        Trazo solo = dibujo.Seleccion;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || (solo != null && t != solo))
                continue;
            for (int j = 0; j < t.nodos.Count; j++)
            {
                Vector3 w = dibujo.transform.TransformPoint(t.nodos[j]);
                if (DentroDelPoligono(Proyectar(w, ojo, der, arriba, adelante), lazo2D))
                    elegidosLazo.Add(new NodoGrupo { t = t, i = j, inicio = t.nodos[j], peso = 1f });
            }
        }
        if (lazo.Count > 0)
            Burbuja(lazo[0], elegidosLazo.Count > 0 ? 1.2f : 0.7f);
        if (elegidosLazo.Count > 0)
            MostrarEtiqueta("Nodos elegidos: " + elegidosLazo.Count);
        CancelarLazo();
    }

    // Lo que ves: el punto en tu "pantalla" (perspectiva desde tus ojos).
    static Vector2 Proyectar(Vector3 p, Vector3 ojo, Vector3 der, Vector3 arriba, Vector3 adelante)
    {
        Vector3 d = p - ojo;
        float z = Mathf.Max(0.05f, Vector3.Dot(d, adelante));
        return new Vector2(Vector3.Dot(d, der) / z, Vector3.Dot(d, arriba) / z);
    }

    static bool DentroDelPoligono(Vector2 q, List<Vector2> poli)
    {
        bool dentro = false;
        for (int a = 0, b = poli.Count - 1; a < poli.Count; b = a++)
        {
            Vector2 pa = poli[a], pb = poli[b];
            if ((pa.y > q.y) != (pb.y > q.y) && q.x < (pb.x - pa.x) * (q.y - pa.y) / (pb.y - pa.y) + pa.x)
                dentro = !dentro;
        }
        return dentro;
    }

    // Los vecinos de la plastilina: naranja clarito.
    Material MaterialNodoPlastilina()
    {
        if (materialNodoPlastilina == null && materialNodoActivo != null)
        {
            materialNodoPlastilina = new Material(materialNodoActivo);
            var c = new Color(1f, 0.78f, 0.5f);
            if (materialNodoPlastilina.HasProperty("_BaseColor"))
                materialNodoPlastilina.SetColor("_BaseColor", c);
            if (materialNodoPlastilina.HasProperty("_Color"))
                materialNodoPlastilina.SetColor("_Color", c);
            materialesPaleta.Add(materialNodoPlastilina); // se libera junto con los de la paleta
        }
        return materialNodoPlastilina;
    }
}

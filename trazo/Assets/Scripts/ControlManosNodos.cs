using System.Collections.Generic;
using UnityEngine;

// Nodos con el DEDO (parte de ControlManos). Mano izquierda: pulgar + medio = modo nodos.
//  - TOCAR un nodo con el índice derecho = se pega al dedo y lo sigue.
//    Para SOLTARLO: abre los dedos de la mano izquierda.
//  - TIRADORES (Bézier): al ACERCAR el dedo a un nodo (sin tocarlo) aparecen sus tiradores; toca la punta
//    de uno y se pega al dedo (para curvar la línea). Se suelta igual: abriendo la mano izquierda.
//    Un dedo = los dos lados (el otro gira en espejo, curva suave). Con la V (índice y medio estirados) =
//    solo ese lado (esquina, como Alt en Illustrator). Llevar la bolita hasta su nodo = tirador en cero (recto).
//  - ÍNDICE + MEDIO estirados (juntos o en V) y los otros dedos doblados, al tocar un nodo = PLASTILINA:
//    los vecinos de la misma línea lo siguen suave (más cerca, más se mueven) y brillan en naranja clarito.
//  - LAZO: empieza en un espacio vacío y dibuja un círculo alrededor de varios nodos: quedan naranjas.
//    Toca uno de ellos y se mueven todos juntos (exactos, sin plastilina). Si lo tocas con la V (índice y
//    medio estirados), además GIRAN con tu muñeca, alrededor del centro del grupo.
//  - Lo que vas a agarrar (nodo o tirador) se ilumina un poquito antes de tocarlo.
//  - Tocar la línea elegida (lejos de sus nodos) y quedarte quieto medio segundo = nodo nuevo, pegado al dedo.
//  - Tocar otra línea = elegirla (y ver solo sus nodos).
//  - PUNTOS DE TELA: PELLIZCA un relleno con la derecha (pulgar + índice) y jálalo hacia ti o empújalo al fondo;
//    al abrir el pellizco se queda así (rombito morado). Los rombitos se mueven con el índice, como los nodos.
//    (Ver ControlManosTela.cs.)
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
    const float RadioVerAsas = 0.06f;        // metros: al acercar el dedo a un nodo se ven sus tiradores
    const float RadioPrevio = 0.035f;        // metros: lo que vas a agarrar se ilumina desde esta distancia

    // Un nodo de un grupo (lazo o plastilina): su línea, cuál es, dónde estaba y cuánto se mueve (0..1).
    struct NodoGrupo
    {
        public Trazo t;
        public int i;
        public Vector3 inicio;
        public float peso;
        public Vector3 entrada, salida;  // sus tiradores al empezar (para girarlos junto con el grupo)
    }

    readonly List<NodoGrupo> grupo = new List<NodoGrupo>();          // los que se están moviendo juntos
    readonly List<NodoGrupo> elegidosLazo = new List<NodoGrupo>();   // los elegidos con el lazo
    readonly HashSet<Trazo> trazosGrupo = new HashSet<Trazo>();
    bool moviendoGrupo, deshacerGrupoPendiente, grupoPlastilina, grupoGira, dedoListoNodos, esperarSalirLinea;
    Vector3 grupoInicioLocal, grupoDesfase, grupoCentro;
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
        SoltarTela();
        moviendoGrupo = false;
        grupoPlastilina = false;
        grupoGira = false;
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

        // 1. Algo agarrado: sigue al dedo (se suelta al abrir la mano izquierda; la tela pellizcada, al abrir el pellizco).
        if (telaTrazo != null)
        {
            SeguirTela(punta);
            MostrarModoNodos(true, dibujo.Seleccion);
            return;
        }
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

        // 1b. El pellizco derecho dentro de un relleno: agarrar la tela ahí (ver ControlManosTela.cs).
        if (Der.empezoPellizco && EmpezarTelaConPellizco())
        {
            CancelarLazo();
            esperaDesde = -1f;
            MostrarModoNodos(true, dibujo.Seleccion);
            return;
        }

        // 2. Los tiradores (Bézier) del nodo al que acercas el dedo, aunque no lo toques.
        ActualizarNodoConAsas(punta, solo);

        // 3. ¿Qué vas a agarrar? Lo más cercano (un nodo o la bolita de un tirador) se ilumina desde un poco
        //    antes de tocarlo, y se agarra al tocarlo: así siempre sabes cuál vas a tomar.
        Trazo t;
        int i;
        bool salida = false;
        Objetivo tipo = Objetivo.Nada;
        Objetivo candidato = Objetivo.Nada;
        float distancia = float.MaxValue;
        if (BuscarNodoCercano(punta, punta, RadioPrevio, solo, out t, out i))
        {
            candidato = Objetivo.Nodo;
            distancia = Vector3.Distance(punta, dibujo.transform.TransformPoint(t.nodos[i]));
        }
        if (BuscarAsaCercana(punta, punta, RadioPrevio, out salida))
        {
            float dAsa = Vector3.Distance(punta, PuntaAsaVisible(selTrazo, selIndice, salida));
            if (dAsa < distancia)
            {
                candidato = Objetivo.Asa;
                distancia = dAsa;
                t = selTrazo;
                i = selIndice;
            }
        }
        Trazo tTela;
        int iTela;
        if (BuscarTelaCercana(punta, RadioPrevio, null, out tTela, out iTela))
        {
            float dTela = Vector3.Distance(punta, dibujo.transform.TransformPoint(tTela.PosicionTela(iTela)));
            if (dTela < distancia)
            {
                candidato = Objetivo.Tela;
                distancia = dTela;
                t = tTela;
                i = iTela;
            }
        }
        if (candidato != Objetivo.Nada && distancia < RadioToqueNodo)
            tipo = candidato;
        Trazo linea = tipo == Objetivo.Nada ? LineaBajo(punta, punta) : null;
        if (linea != lineaBajoDedo)
        {
            lineaBajoDedo = linea;
            lineaBajoDesde = Time.time;
        }
        // Durante un lazo, pasar rápido por encima de una línea no la elige: hay que detenerse un momento en ella.
        bool lineaFirme = linea != null && (largoLazo < 0.03f || Time.time - lineaBajoDesde > 0.25f);
        hoverTipo = candidato;
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

        // 4. Tocó un nodo: se agarra (con su grupo del lazo, con plastilina o él solo).
        if (tipo == Objetivo.Nodo)
        {
            CancelarLazo();
            esperaDesde = -1f;
            if (EnLazo(t, i))
            {
                // Con la V (índice y medio estirados) el grupo además gira con tu muñeca.
                EmpezarGrupo(t, i, punta, false, Der.DosDedos);
            }
            else if (Der.DosDedos)
            {
                ArmarPlastilina(t, i);
                EmpezarGrupo(t, i, punta, true, false);
            }
            else
            {
                dibujo.Seleccionar(t);
                EmpezarArrastre(Objetivo.Nodo, t, i, false, punta);
            }
            MostrarModoNodos(true, dibujo.Seleccion);
            return;
        }
        if (tipo == Objetivo.Tela)
        {
            CancelarLazo();
            esperaDesde = -1f;
            EmpezarArrastreTela(t, i, punta);
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

        // 5. Tocó una línea (lejos de sus nodos): elegirla, o (si ya es la elegida) quedarse quieto = nodo nuevo.
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
                    // Sobre la línea = nodo nuevo. Dentro del relleno (lejos de la línea): la tela es con el pellizco.
                    if (!TocaSoloRelleno(linea, punta))
                        AgregarNodoEn(linea, punta);
                    else if (Time.time - avisoTela > 4f)
                    {
                        avisoTela = Time.time;
                        dibujo.Mensaje("Tela: pellizca el relleno y jálalo");
                    }
                }
            }
            MostrarModoNodos(true, dibujo.Seleccion);
            return;
        }
        esperarSalirLinea = false;
        esperaDesde = -1f;

        // 6. Espacio vacío: el lazo.
        ActualizarLazo(punta);
        MostrarModoNodos(true, solo);
    }

    // El nodo cuyos tiradores se ven: el más cercano al dedo. Se mantiene mientras el dedo siga cerca de él
    // o de las puntas de sus tiradores (así puedes ir hasta la punta de un tirador sin que desaparezca).
    void ActualizarNodoConAsas(Vector3 punta, Trazo solo)
    {
        if (SeleccionValida && (solo == null || selTrazo == solo))
        {
            float cerca = Vector3.Distance(punta, dibujo.transform.TransformPoint(selTrazo.nodos[selIndice]));
            for (int k = 0; k < 2; k++)
                if (selTrazo.AsaUsada(selIndice, k == 1))
                    cerca = Mathf.Min(cerca, Vector3.Distance(punta, PuntaAsaVisible(selTrazo, selIndice, k == 1)));
            if (cerca < RadioVerAsas)
                return;
        }
        Trazo t;
        int i;
        if (BuscarNodoCercano(punta, punta, RadioVerAsas * 0.75f, solo, out t, out i))
        {
            selTrazo = t;
            selIndice = i;
        }
        else
        {
            selTrazo = null;
            selIndice = -1;
        }
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

    void EmpezarGrupo(Trazo t, int tocado, Vector3 punta, bool plastilina, bool girar)
    {
        if (!plastilina)
        {
            grupo.Clear();
            foreach (var g in elegidosLazo)
                if (Dibujo.Editable(g.t) && g.i < g.t.nodos.Count)
                {
                    g.t.AsegurarAsas();
                    grupo.Add(new NodoGrupo
                    {
                        t = g.t, i = g.i, inicio = g.t.nodos[g.i], peso = 1f,
                        entrada = g.t.asaEntrada[g.i], salida = g.t.asaSalida[g.i]
                    });
                }
        }
        if (grupo.Count == 0)
            return;
        grupoPlastilina = plastilina;
        // Girar: alrededor del centro del grupo, con el giro de tu muñeca derecha (en Plano 2D, solo dentro del plano).
        grupoGira = girar && !plastilina;
        if (grupoGira)
        {
            Vector3 c = Vector3.zero;
            foreach (var g in grupo)
                c += g.inicio;
            grupoCentro = c / grupo.Count;
            var muneca = ManosUtil.Hueso(Der.esqueleto, Titere.Muneca);
            tieneRotMano = muneca != null;
            if (tieneRotMano)
                rotManoInicio = rotManoSuave = muneca.rotation;
            anguloGiro = 0f;
        }
        grupoInicioLocal = t.nodos[tocado];
        grupoDesfase = dibujo.transform.TransformPoint(grupoInicioLocal) - punta;
        moviendoGrupo = true;
        EmpezarEmpujon(t);
        deshacerGrupoPendiente = true;
        selTrazo = t;
        selIndice = tocado;
        Burbuja(punta, 0.8f);
    }

    void MoverGrupo(Vector3 punta)
    {
        // Encantadas en su hoja 2D: cada nodo (y sus tiradores) se queda en la hoja de su línea.
        Trazo baseHoja = selTrazo;
        Vector3 objetivo = dibujo.transform.InverseTransformPoint(punta + grupoDesfase);
        if (baseHoja != null)
            objetivo = baseHoja.ProyectarEnHoja(objetivo);
        Vector3 delta = objetivo - grupoInicioLocal;
        if (baseHoja != null)
            delta = baseHoja.ProyectarVectorEnHoja(delta);
        Quaternion giro = grupoGira ? GiroManoLocal(baseHoja) : Quaternion.identity;
        if (!grupoPlastilina)
            OrdenarConEmpujonGrupo(punta);
        float angulo = grupoGira ? anguloGiro : 0f;
        if (deshacerGrupoPendiente)
        {
            // Se guarda "deshacer" solo cuando de verdad se mueve (o gira) algo.
            if (delta.magnitude * dibujo.EscalaMundo < 0.003f && angulo < 0.5f)
                return;
            dibujo.GuardarParaDeshacer();
            deshacerGrupoPendiente = false;
        }
        trazosGrupo.Clear();
        foreach (var g in grupo)
        {
            if (g.t == null || g.i >= g.t.nodos.Count)
                continue;
            Vector3 p = grupoGira ? grupoCentro + delta + giro * (g.inicio - grupoCentro) : g.inicio + delta * g.peso;
            g.t.MoverNodoSinReconstruir(g.i, g.t.ProyectarEnHoja(p));
            // Los tiradores hechos a mano giran con su nodo (los automáticos se recalculan solos).
            if (grupoGira && g.i < g.t.asaManual.Count && g.t.asaManual[g.i])
            {
                g.t.asaEntrada[g.i] = g.t.ProyectarVectorEnHoja(giro * g.entrada);
                g.t.asaSalida[g.i] = g.t.ProyectarVectorEnHoja(giro * g.salida);
            }
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

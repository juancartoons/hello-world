using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Paleta de colores (parte de ControlManos):
//  - Palma IZQUIERDA abierta mirando hacia ti (dedos separados) = aparece una paleta de pintor con 12 colores.
//  - BOTONES CON HIJOS: alrededor hay dos botones "raíz". Al tocar uno se pone amarillo y le salen sus botones
//    hijos, unidos por una rayita (un arco). Al tocarlo otra vez se esconden. Solo una raíz abierta a la vez.
//      · LÍNEA (a las 10): temblor, hebras, grosor vivo, ciclo de 3, suavidad, velocidad, imán y halo
//        (el halo solo con un fondo 360 o la realidad).
//      · RELLENO (a las 2): textura (relleno vivo), cubeta y velocidad de la textura.
//  - Los 12 colores pintan lo que esté abierto: con RELLENO abierto, el color del relleno; si no, el de la línea.
//    Con líneas elegidas (azules), se les pone a ellas (se puede deshacer).
//  - TINTA INVISIBLE (a las 12): un color que no se ve (en el relleno: sin relleno).
//  - EL CENTRO es la vista previa: la línea que tiembla (o, con RELLENO abierto, un circulito con el relleno y su
//    textura moviéndose). Abajo, dentro del círculo, un textito con lo último que tocaste ("Hebras 3", "Temblor 2").
//  - Cuentagotas: con la paleta abierta, toca una línea con el índice derecho = tomas su color.
//  - Con RELLENO abierto, toca DENTRO de una figura = la rellenas (ver ControlManosRelleno.cs).
//  - Pulgar e índice derechos sobre un relleno, abriendo y cerrando = tamaño de su textura.
//  - PALETA ESTABLE: no copia los temblores chiquitos de la mano y, cuando tu índice derecho está a menos de 12 cm,
//    se queda quieta en el aire (solo se mueve si tu mano izquierda se va más de 3 cm).
//  - Debajo de la paleta: un cartelito con los datos de la línea y del relleno. Lo último que cambiaste sale resaltado.
//  - Cierra la mano (o bájala) y la paleta se va.
public partial class ControlManos
{
    static readonly Color[] ColoresPaleta =
    {
        Color.black,
        new Color(0.97f, 0.97f, 0.97f),
        new Color(0.55f, 0.55f, 0.58f),
        new Color(1f, 0.82f, 0.1f),
        new Color(1f, 0.5f, 0.1f),
        new Color(0.9f, 0.15f, 0.15f),
        new Color(1f, 0.45f, 0.7f),
        new Color(0.55f, 0.3f, 0.85f),
        new Color(0.15f, 0.45f, 0.95f),
        new Color(0.25f, 0.75f, 0.3f),
        new Color(0.5f, 0.3f, 0.15f),
        new Color(0.96f, 0.75f, 0.6f),
    };
    const float RadioPaleta = 0.04f;    // dónde van los colores (metros)
    const float TamColor = 0.02f;       // tamaño de cada color
    const float RadioBotones = 0.073f;  // el arco de botones alrededor de la paleta
    const float AnguloLinea = 150f, AnguloRelleno = 30f, AnguloInvisible = 90f; // a las 10, a las 2 y a las 12
    const float PasoHijos = 22f;        // grados entre un hijo y el siguiente (deja ver el arco que los une)
    const float PrimerHijo = 24f;       // grados de la raíz a su primer hijo
    const float CorteMinPaleta = 0.8f;  // paleta estable: filtro de la posición (Hz quieta)
    const float BetaPaleta = 12f;       // y cuánto más rápido sigue cuando la mano se mueve de verdad
    const float DedoCercaPaleta = 0.12f, SoltarAncla = 0.03f;

    // Un botón redondo de la paleta: aro negro, fondo blanco (o amarillo) e icono.
    class BotonPaleta
    {
        public Transform t;
        public Material fondo;
        public float tamano;
        public float angulo;                 // grados, en el arco de la paleta
        public float radioToque = 0.011f;
        public float tono = 0.9f;            // del sonido de burbuja
        public RamaPaleta rama;              // de qué raíz es hijo (null = siempre a la vista)
        public System.Func<bool> visible;    // null = siempre
        public System.Func<bool> amarillo;   // null = nunca
        public System.Action alTocar;
        public System.Func<string> texto;    // lo que dice el centro después de tocarlo
        public bool dedoEn;
    }

    // Una raíz con sus hijos y el arco que los une.
    class RamaPaleta
    {
        public BotonPaleta raiz;
        public readonly List<BotonPaleta> hijos = new List<BotonPaleta>();
        public Transform arco;
        public Mesh mallaArco;
        public float hastaArco = float.NaN;
        public float sentido = 1f;           // 1: los hijos van contra el reloj; -1: como el reloj
    }

    Transform paleta;
    readonly List<Transform> botonesColor = new List<Transform>();
    readonly List<Material> materialesPaleta = new List<Material>();
    readonly List<BotonPaleta> botonesPaleta = new List<BotonPaleta>();
    RamaPaleta ramaLinea, ramaRelleno, ramaAbierta;
    BotonPaleta botonInvisible;
    Material materialCentroPaleta, materialCursorColor, gotaCubeta;
    TMP_Text textoCentro;
    Transform centroRelleno;
    Mesh mallaCentroRelleno;
    string claveCentroRelleno;
    bool paletaAbierta, dedoEnColor, dedoEnLinea;
    float paletaDesde = -1f, paletaFueraDesde = -1f;

    public bool PaletaAbierta => paletaAbierta;
    bool RellenoAbierto => paletaAbierta && ramaAbierta != null && ramaAbierta == ramaRelleno;

    // Palma izquierda abierta, más o menos mirando hacia tu cara (pocas condiciones, para que salga fácil).
    bool PosePaleta(bool yaAbierta)
    {
        if (!palmaIzq.valida || Cabeza == null)
            return false;
        Vector3 aCabeza = Cabeza.position - palmaIzq.centro;
        float dist = aCabeza.magnitude;
        if (dist < 0.12f || dist > 0.95f)
            return false;
        aCabeza /= dist;
        float palma = Vector3.Dot(palmaIzq.normal, aCabeza);
        float mirar = Vector3.Dot(Cabeza.forward, -aCabeza);
        bool abierta = palmaIzq.cierre > (yaAbierta ? 1.15f : 1.3f);
        return abierta && palma > (yaAbierta ? 0.3f : 0.5f) && mirar > 0.3f;
    }

    // Botón "Colores" (debajo del menú del reloj): abre o cierra la paleta sin el gesto.
    // Así abierta se queda junto al menú; se cierra al elegir un color de línea, con el botón otra vez o sola en 25 s.
    bool paletaFija;
    float paletaFijaHasta;

    public void AlternarPaleta()
    {
        if (paletaAbierta)
        {
            CerrarPaleta();
            return;
        }
        AbrirPaleta();
        if (paleta == null || Cabeza == null)
            return;
        paletaFija = true;
        paletaFijaHasta = Time.time + 25f;
        Vector3 pos = posMenu + Cabeza.right * 0.13f;
        paleta.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - Cabeza.position, Vector3.up));
    }

    void ActualizarPaleta()
    {
        bool pose = GestoIzq == Gesto.Ninguno && !menuAbierto && PosePaleta(paletaAbierta);
        if (paletaFija)
        {
            // Abierta con el botón: no depende del gesto.
            if (Time.time > paletaFijaHasta)
                CerrarPaleta();
        }
        else if (pose)
        {
            paletaFueraDesde = -1f;
            if (paletaDesde < 0f)
                paletaDesde = Time.time;
            if (!paletaAbierta && Time.time - paletaDesde > 0.25f)
                AbrirPaleta();
        }
        else
        {
            paletaDesde = -1f;
            if (paletaAbierta)
            {
                if (paletaFueraDesde < 0f)
                    paletaFueraDesde = Time.time;
                // Si la mano derecha está tocando la paleta, la izquierda puede perderse un momento.
                bool tocando = Der.valida && paleta != null && Vector3.Distance(Der.indice, paleta.position) < 0.11f;
                float espera = GestoIzq != Gesto.Ninguno ? 0f : tocando ? 0.8f : 0.35f;
                if (Time.time - paletaFueraDesde > espera)
                    CerrarPaleta();
            }
        }
        if (!paletaAbierta || paleta == null)
            return;

        ColocarPaleta();
        RefrescarBotonesPaleta();
        ActualizarCentroPaleta();
        ActualizarDatosPaleta();

        if (!Der.valida)
        {
            foreach (var b in botonesPaleta)
                b.dedoEn = false;
            TerminarPellizcoTextura();
            dedoEnRelleno = false;
            rellenoCandidato = null;
            dedoEnColor = false;
            dedoEnLinea = false;
            return;
        }
        Vector3 punta = Der.indice;
        // ¿Tocó un botón (raíz, hijo o tinta invisible)?
        if (pellizcoTrazo == null && TocarBotonesPaleta(punta))
        {
            dedoEnColor = false;
            dedoEnLinea = false;
            return;
        }
        // ¿Tocó un color?
        int tocado = -1;
        for (int i = 0; i < botonesColor.Count; i++)
            if (Vector3.Distance(punta, botonesColor[i].position) < 0.014f)
                tocado = i;
        if (tocado >= 0 && pellizcoTrazo == null)
        {
            if (!dedoEnColor)
                ElegirColor(ColoresPaleta[tocado], botonesColor[tocado].position);
            dedoEnColor = true;
            return;
        }
        dedoEnColor = false;
        // Cerca de la paleta no se toman colores de las líneas ni se rellena.
        if (Vector3.Distance(punta, paleta.position) < 0.1f)
        {
            TerminarPellizcoTextura();
            dedoEnLinea = false;
            dedoEnRelleno = false;
            rellenoCandidato = null;
            return;
        }
        // Pulgar e índice sobre un relleno: tamaño de la textura.
        if (RevisarPellizcoTextura())
        {
            dedoEnLinea = false;
            return;
        }
        // Cuentagotas: tocar una línea (lejos de la paleta) = tomar su color.
        Trazo cerca = null;
        float mejor = float.MaxValue;
        Vector3 local = dibujo.transform.InverseTransformPoint(punta);
        float escala = dibujo.EscalaMundo;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || t.oculto)
                continue;
            float d = t.DistanciaACurva(local) * escala;
            if (d < t.ancho * escala * 0.5f + 0.01f && d < mejor)
            {
                mejor = d;
                cerca = t;
            }
        }
        if (cerca != null)
        {
            if (!dedoEnLinea)
            {
                if (RellenoAbierto)
                {
                    PonerColorRellenoNuevo(cerca.relleno ? cerca.ColorDelRelleno : cerca.color);
                    PonerTextoCentro("Relleno: " + NombreColor(colorRellenoNuevo));
                }
                else
                {
                    dibujo.ColorNuevo = cerca.color;
                    PonerTextoCentro("Línea: " + NombreColor(cerca.color));
                }
                Burbuja(punta, 1.3f);
                MostrarEtiqueta("Cuentagotas: color tomado");
            }
            dedoEnLinea = true;
            dedoEnRelleno = false;
            rellenoCandidato = null;
            return;
        }
        dedoEnLinea = false;
        // Con RELLENO abierto: ¿está DENTRO de una forma? = rellenarla. Si no, tocar las figuras no hace nada.
        if (RellenoAbierto)
        {
            RellenarAlTocar(local, punta);
        }
        else
        {
            dedoEnRelleno = false;
            rellenoCandidato = null;
        }
    }

    // Un color de la paleta (o la tinta invisible): para el relleno si RELLENO está abierto; si no, para la línea.
    void ElegirColor(Color c, Vector3 donde)
    {
        Burbuja(donde, 1.1f);
        var elegidas = dibujo.Seleccionadas();
        if (RellenoAbierto)
        {
            PonerColorRellenoNuevo(c);
            int n = elegidas.Count > 0 ? dibujo.PonerColorRelleno(elegidas, c) : 0;
            if (n > 0)
            {
                dibujo.Seleccionar(null); // sin el azul de "elegida", así se ve el color nuevo
                MostrarEtiqueta(n > 1 ? "Rellenos pintados" : "Relleno pintado");
            }
            else
            {
                MostrarEtiqueta(c.a < 0.01f ? "Sin relleno" : "Color del relleno");
            }
            PonerTextoCentro(c.a < 0.01f ? "Sin relleno" : "Relleno: " + NombreColor(c));
            // Abierta con el botón: se queda, para que puedas tocar dentro de una figura.
            if (paletaFija)
                paletaFijaHasta = Mathf.Max(paletaFijaHasta, Time.time + 12f);
            return;
        }
        dibujo.ColorNuevo = c;
        // Si hay líneas elegidas, se pintan de ese color.
        if (elegidas.Count > 0)
        {
            dibujo.GuardarParaDeshacer();
            dibujo.Seleccionar(null);
            foreach (var t in elegidas)
                dibujo.PonerColor(t, c);
            Trazo.huboCambio = false;
            MostrarEtiqueta(elegidas.Count > 1 ? "Líneas pintadas" : "Línea pintada");
        }
        else
        {
            MostrarEtiqueta(c.a < 0.01f ? "Tinta invisible" : "Color");
        }
        PonerTextoCentro(c.a < 0.01f ? "Línea invisible" : "Línea: " + NombreColor(c));
        // Abierta con el botón: después de elegir, se cierra sola.
        if (paletaFija)
            paletaFijaHasta = Mathf.Min(paletaFijaHasta, Time.time + 0.8f);
    }

    static int IndiceColor(Color c)
    {
        if (c.a < 0.01f)
            return -1; // tinta invisible
        int mejor = 0;
        float distancia = float.MaxValue;
        for (int i = 0; i < ColoresPaleta.Length; i++)
        {
            Color p = ColoresPaleta[i];
            float d = (p.r - c.r) * (p.r - c.r) + (p.g - c.g) * (p.g - c.g) + (p.b - c.b) * (p.b - c.b);
            if (d < distancia)
            {
                distancia = d;
                mejor = i;
            }
        }
        return mejor;
    }

    static string NombreColor(Color c)
    {
        int i = IndiceColor(c);
        return i < 0 ? "Tinta invisible" : NombresColorPaleta[Mathf.Clamp(i, 0, NombresColorPaleta.Length - 1)];
    }

    void AbrirPaleta()
    {
        if (paleta == null)
            ArmarPaleta();
        if (paleta == null)
            return;
        paletaAbierta = true;
        // No se toca nada hasta que el dedo salga y vuelva a entrar.
        dedoEnColor = true;
        dedoEnLinea = true;
        dedoEnRelleno = true;
        rellenoCandidato = null;
        foreach (var b in botonesPaleta)
            b.dedoEn = true;
        claveMuestra = int.MinValue;
        claveCentroRelleno = null;
        ramaAbierta = null; // las raíces empiezan cerradas
        PonerTextoCentro("");
        paletaFiltroListo = false;
        paletaAnclada = false;
        if (palmaIzq.valida && Cabeza != null)
        {
            Vector3 pos = palmaIzq.centro + palmaIzq.normal * 0.05f + Vector3.up * 0.01f;
            paleta.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - Cabeza.position, Vector3.up));
        }
        paleta.gameObject.SetActive(true);
        RefrescarBotonesPaleta();
        Burbuja(paleta.position, 0.9f);
    }

    void CerrarPaleta()
    {
        TerminarPellizcoTextura();
        paletaFija = false;
        paletaAbierta = false;
        paletaDesde = -1f;
        paletaFueraDesde = -1f;
        ramaAbierta = null;
        if (paleta != null && paleta.gameObject.activeSelf)
            paleta.gameObject.SetActive(false);
    }

    // ---------- Paleta estable (como el estabilizador de una cámara) ----------
    Vector3 paletaFiltrada, paletaVelocidad, paletaCrudaPrevia, anclaPaletaPos;
    Quaternion anclaPaletaRot;
    bool paletaFiltroListo, paletaAnclada, paletaSiguiendo;

    // La paleta flota sobre tu palma, mirando hacia ti. Los temblores chiquitos no la mueven (filtro "One Euro",
    // como el del lápiz) y, con tu índice derecho cerca, se queda quieta: solo se mueve si la mano se va más de 3 cm.
    void ColocarPaleta()
    {
        if (paletaFija || !palmaIzq.valida || !poseValida || Cabeza == null)
            return;
        Vector3 cruda = palmaIzq.centro + palmaIzq.normal * 0.05f + Vector3.up * 0.01f;
        float dt = Mathf.Max(1e-4f, Time.deltaTime);
        if (!paletaFiltroListo)
        {
            paletaFiltroListo = true;
            paletaFiltrada = cruda;
            paletaCrudaPrevia = cruda;
            paletaVelocidad = Vector3.zero;
        }
        Vector3 v = (cruda - paletaCrudaPrevia) / dt;
        paletaCrudaPrevia = cruda;
        paletaVelocidad = Vector3.Lerp(paletaVelocidad, v, AlfaFiltro(dt, 1f));
        float corte = CorteMinPaleta + BetaPaleta * paletaVelocidad.magnitude;
        paletaFiltrada = Vector3.Lerp(paletaFiltrada, cruda, AlfaFiltro(dt, corte));
        Quaternion rot = Quaternion.LookRotation(paletaFiltrada - Cabeza.position, Vector3.up);

        bool dedoCerca = Der.valida && Vector3.Distance(Der.indice, paleta.position) < DedoCercaPaleta;
        if (dedoCerca)
        {
            if (!paletaAnclada)
            {
                paletaAnclada = true;
                paletaSiguiendo = false;
                anclaPaletaPos = paleta.position;
                anclaPaletaRot = paleta.rotation;
            }
            float lejos = Vector3.Distance(paletaFiltrada, anclaPaletaPos);
            if (lejos > SoltarAncla)
                paletaSiguiendo = true;
            else if (lejos < 0.006f)
                paletaSiguiendo = false;
            if (paletaSiguiendo)
            {
                float a = 1f - Mathf.Exp(-6f * dt);
                anclaPaletaPos = Vector3.Lerp(anclaPaletaPos, paletaFiltrada, a);
                anclaPaletaRot = Quaternion.Slerp(anclaPaletaRot, rot, a);
            }
            paleta.SetPositionAndRotation(anclaPaletaPos, anclaPaletaRot);
        }
        else
        {
            paletaAnclada = false;
            float a = 1f - Mathf.Exp(-12f * dt);
            paleta.SetPositionAndRotation(Vector3.Lerp(paleta.position, paletaFiltrada, a), Quaternion.Slerp(paleta.rotation, rot, a));
        }
    }

    // ---------- Armar la paleta ----------
    // Un disco blanco con borde negro, 12 circulitos de color alrededor y la vista previa en el centro;
    // afuera, la tinta invisible y las dos raíces con sus hijos.
    void ArmarPaleta()
    {
        if (materialNodo == null)
            return;
        var go = new GameObject("PaletaColores");
        go.transform.SetParent(transform, false);
        paleta = go.transform;
        var negro = ColorMaterial(Color.black);
        var blanco = ColorMaterial(Color.white);
        DiscoPaleta(paleta, negro, 0.118f, new Vector3(0f, 0f, 0.002f));
        DiscoPaleta(paleta, blanco, 0.11f, new Vector3(0f, 0f, 0.001f));
        // El centro: aro fino y fondo gris clarito (así se ve también una línea blanca).
        DiscoPaleta(paleta, negro, 0.056f, new Vector3(0f, 0f, 0.0005f));
        DiscoPaleta(paleta, ColorMaterial(new Color(0.85f, 0.85f, 0.87f)), 0.052f, new Vector3(0f, 0f, 0.0003f));
        materialCentroPaleta = ColorMaterial(dibujo != null ? dibujo.ColorNuevo : Color.black);
        ArmarTemblorPaleta(negro);
        ArmarCentroRelleno();
        ArmarTextoCentro();
        ArmarBotonesPaleta(negro);
        ArmarDatosPaleta();
        for (int i = 0; i < ColoresPaleta.Length; i++)
        {
            float ang = Mathf.PI * 0.5f - i * Mathf.PI * 2f / ColoresPaleta.Length;
            var b = new GameObject("Color" + i).transform;
            b.SetParent(paleta, false);
            b.localPosition = new Vector3(Mathf.Cos(ang) * RadioPaleta, Mathf.Sin(ang) * RadioPaleta, -0.001f);
            b.localScale = Vector3.one * TamColor;
            DiscoPaleta(b, negro, 1.18f, new Vector3(0f, 0f, 0.02f));
            DiscoPaleta(b, ColorMaterial(ColoresPaleta[i]), 1f, Vector3.zero);
            botonesColor.Add(b);
        }
        go.SetActive(false);
    }

    BotonPaleta CrearBotonPaleta(string nombre, float angulo, float tamano, Material negro, RamaPaleta rama)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(paleta, false);
        float a = angulo * Mathf.Deg2Rad;
        t.localPosition = new Vector3(Mathf.Cos(a) * RadioBotones, Mathf.Sin(a) * RadioBotones, -0.001f);
        t.localScale = Vector3.one * tamano;
        var fondo = ColorMaterial(Color.white);
        DiscoPaleta(t, negro, 1.12f, new Vector3(0f, 0f, 0.05f));
        DiscoPaleta(t, fondo, 1f, new Vector3(0f, 0f, 0.025f));
        var b = new BotonPaleta { t = t, fondo = fondo, tamano = tamano, angulo = angulo, rama = rama };
        botonesPaleta.Add(b);
        if (rama != null)
            rama.hijos.Add(b);
        return b;
    }

    RamaPaleta CrearRama(string nombre, float angulo, Material negro)
    {
        var r = new RamaPaleta();
        r.raiz = CrearBotonPaleta(nombre, angulo, 0.022f, negro, null);
        r.raiz.radioToque = 0.012f;
        r.raiz.tono = 1f;
        r.raiz.amarillo = () => ramaAbierta == r;
        // El arco que une la raíz con sus hijos (detrás de los botones).
        var arco = new GameObject("Arco" + nombre);
        arco.transform.SetParent(paleta, false);
        arco.transform.localPosition = new Vector3(0f, 0f, 0.0004f);
        r.mallaArco = new Mesh { name = "Arco" + nombre };
        mallasTemblor.Add(r.mallaArco);
        arco.AddComponent<MeshFilter>().sharedMesh = r.mallaArco;
        var mr = arco.AddComponent<MeshRenderer>();
        mr.sharedMaterial = negro;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        r.arco = arco.transform;
        arco.SetActive(false);
        return r;
    }

    void ArmarBotonesPaleta(Material negro)
    {
        // ---- Tinta invisible (a las 12): un círculo a cuadritos, como lo "transparente" de los programas de dibujo.
        botonInvisible = CrearBotonPaleta("TintaInvisible", AnguloInvisible, 0.021f, negro, null);
        botonInvisible.radioToque = 0.012f;
        botonInvisible.tono = 1.1f;
        var cuadros = new List<Vector2[]>();
        for (int fy = 0; fy < 4; fy++)
            for (int fx = 0; fx < 4; fx++)
            {
                if ((fx + fy) % 2 == 1)
                    continue;
                float x0 = -0.3f + fx * 0.15f, y0 = -0.3f + fy * 0.15f;
                cuadros.Add(new[] { new Vector2(x0, y0), new Vector2(x0, y0 + 0.15f), new Vector2(x0 + 0.15f, y0 + 0.15f), new Vector2(x0 + 0.15f, y0) });
            }
        IconoPaleta(botonInvisible.t, cuadros, ColorMaterial(new Color(0.7f, 0.72f, 0.76f)));
        botonInvisible.alTocar = () => ElegirColor(Dibujo.TintaInvisible, botonInvisible.t.position);

        // ---- LÍNEA (a las 10): una pincelada en S.
        ramaLinea = CrearRama("Linea", AnguloLinea, negro);
        var pincelada = new Vector2[9];
        for (int k = 0; k < pincelada.Length; k++)
        {
            float u = k / (float)(pincelada.Length - 1);
            pincelada[k] = new Vector2(-0.26f + 0.52f * u, -0.15f + 0.3f * u + 0.09f * Mathf.Sin(u * Mathf.PI * 2f));
        }
        var s = new List<Vector2[]>();
        LineaIcono(s, pincelada, 0.09f);
        IconoPaleta(ramaLinea.raiz.t, s, negro);
        ramaLinea.raiz.alTocar = () => AbrirRama(ramaLinea);
        ramaLinea.raiz.texto = () => ramaAbierta == ramaLinea ? "Línea" : "";

        var iconos = IconosTemblor();
        var hijo = HijoLinea(0, iconos[0], negro);
        hijo.amarillo = () => CapaPaleta() != null && CapaPaleta().temblor > 0;
        hijo.alTocar = () => { if (dibujo.temblor != null) dibujo.temblor.Siguiente(); };
        hijo.texto = () => dibujo.temblor != null && CapaPaleta() != null ? "Temblor " + CapaPaleta().temblor + " · " + dibujo.temblor.NombreNivel : "";
        hijo = HijoLinea(1, iconos[1], negro);
        hijo.amarillo = () => CapaPaleta() != null && CapaPaleta().hebras > 1;
        hijo.alTocar = () => { if (dibujo.temblor != null) dibujo.temblor.SiguienteHebras(); };
        hijo.texto = () => dibujo.temblor != null ? "Hebras " + dibujo.temblor.Hebras : "";
        hijo = HijoLinea(2, iconos[2], negro);
        hijo.amarillo = () => CapaPaleta() != null && CapaPaleta().grosorVivo;
        hijo.alTocar = () => { if (dibujo.temblor != null) dibujo.temblor.AlternarGrosor(); };
        hijo.texto = () => dibujo.temblor != null && dibujo.temblor.GrosorVivo ? "Grosor vivo: Sí" : "Grosor vivo: No";
        hijo = HijoLinea(3, iconos[3], negro);
        hijo.amarillo = () => CapaPaleta() != null && !CapaPaleta().ciclo3;
        hijo.alTocar = () => { if (dibujo.temblor != null) dibujo.temblor.AlternarCiclo(); };
        hijo.texto = () => dibujo.temblor == null || dibujo.temblor.Ciclo3 ? "Ciclo de 3" : "Libre";
        hijo = HijoLinea(4, iconos[4], negro);
        hijo.amarillo = () => CapaPaleta() != null && CapaPaleta().suavidad != 1;
        hijo.alTocar = () => { if (dibujo.temblor != null) dibujo.temblor.SiguienteSuavidad(); };
        hijo.texto = () => dibujo.temblor != null && CapaPaleta() != null ? "Suavidad " + (Mathf.Clamp(CapaPaleta().suavidad, 0, 2) + 1) + " · " + dibujo.temblor.NombreSuavidad : "";
        hijo = HijoLinea(5, iconos[5], negro);
        hijo.amarillo = () => CapaPaleta() != null && CapaPaleta().velocidad != 1;
        hijo.alTocar = () => { if (dibujo.temblor != null) dibujo.temblor.SiguienteVelocidad(); };
        hijo.texto = () => dibujo.temblor != null ? Mathf.RoundToInt(dibujo.temblor.CambiosPorSegundo) + " cambios/s" : "";
        // Imán: una herradura roja con las puntas grises.
        hijo = HijoLinea(6, null, negro);
        var u2 = new List<Vector2[]>();
        LineaIcono(u2, new[] { new Vector2(-0.17f, 0.12f), new Vector2(-0.17f, -0.06f), new Vector2(-0.1f, -0.2f),
                               new Vector2(0f, -0.24f), new Vector2(0.1f, -0.2f), new Vector2(0.17f, -0.06f), new Vector2(0.17f, 0.12f) }, 0.11f);
        IconoPaleta(hijo.t, u2, ColorMaterial(new Color(0.88f, 0.15f, 0.15f)));
        var puntas = new List<Vector2[]>();
        BarraIcono(puntas, new Vector2(-0.17f, 0.13f), new Vector2(-0.17f, 0.25f), 0.11f);
        BarraIcono(puntas, new Vector2(0.17f, 0.13f), new Vector2(0.17f, 0.25f), 0.11f);
        IconoPaleta(hijo.t, puntas, ColorMaterial(new Color(0.75f, 0.76f, 0.8f)));
        hijo.amarillo = () => CapaPaleta() != null && CapaPaleta().iman;
        hijo.alTocar = () => dibujo.AlternarIman();
        hijo.texto = () => CapaPaleta() != null && CapaPaleta().iman ? "Imán: Sí" : "Imán: No";
        hijo.tono = 1.2f;
        // Halo (solo con un fondo 360 o la realidad): una raya negra con su brillo gris detrás.
        hijo = HijoLinea(7, null, negro);
        var brillo = new List<Vector2[]>();
        BarraIcono(brillo, new Vector2(-0.2f, -0.2f), new Vector2(0.2f, 0.2f), 0.3f);
        IconoPaleta(hijo.t, brillo, ColorMaterial(new Color(0.68f, 0.7f, 0.75f)));
        hijo.t.GetChild(hijo.t.childCount - 1).localPosition = new Vector3(0f, 0f, 0.012f);
        var raya = new List<Vector2[]>();
        BarraIcono(raya, new Vector2(-0.2f, -0.2f), new Vector2(0.2f, 0.2f), 0.08f);
        IconoPaleta(hijo.t, raya, negro);
        hijo.visible = () => dibujo.HaloDisponible;
        hijo.amarillo = () => dibujo.HaloEncendido;
        hijo.alTocar = () => dibujo.AlternarHalo();
        hijo.texto = () => dibujo.HaloEncendido ? "Halo: Sí" : "Halo: No";
        hijo.tono = 1.1f;
        // Lápiz (solo en una capa de boceto 2D): cómo sigue a tu dedo. Un lápiz amarillo con su punta.
        hijo = HijoLinea(8, null, negro);
        Vector2 dirLapiz = new Vector2(0.7071f, -0.7071f), ladoLapiz = new Vector2(0.7071f, 0.7071f);
        Vector2 finCuerpo = new Vector2(0.05f, -0.05f), puntaLapiz = finCuerpo + dirLapiz * 0.2f;
        var cuerpo = new List<Vector2[]>();
        BarraIcono(cuerpo, new Vector2(-0.24f, 0.24f), finCuerpo, 0.15f);
        IconoPaleta(hijo.t, cuerpo, ColorMaterial(new Color(0.96f, 0.76f, 0.2f)));
        IconoPaleta(hijo.t, new List<Vector2[]> { new[] { finCuerpo + ladoLapiz * 0.075f, puntaLapiz, finCuerpo - ladoLapiz * 0.075f } },
                    ColorMaterial(new Color(0.93f, 0.8f, 0.62f)));
        Vector2 baseGrafito = puntaLapiz - dirLapiz * 0.07f;
        IconoPaleta(hijo.t, new List<Vector2[]> { new[] { baseGrafito + ladoLapiz * 0.026f, puntaLapiz, baseGrafito - ladoLapiz * 0.026f } }, negro);
        hijo.visible = () => dibujo.UsaHoja;
        hijo.amarillo = () => ModoLapiz != 0;
        hijo.alTocar = SiguienteModoLapiz;
        hijo.texto = () => "Lápiz: " + NombresModoLapiz[ModoLapiz];
        hijo.tono = 1f;

        // ---- RELLENO (a las 2): 3 facetas verdes.
        ramaRelleno = CrearRama("Relleno", AnguloRelleno, negro);
        ramaRelleno.sentido = -1f;
        IconoPaleta(ramaRelleno.raiz.t, new List<Vector2[]>
        {
            new[] { new Vector2(-0.3f, -0.3f), new Vector2(0.05f, -0.3f), new Vector2(-0.1f, 0.1f), new Vector2(-0.3f, 0.15f) },
        }, ColorMaterial(new Color(0.3f, 0.5f, 0.32f)));
        IconoPaleta(ramaRelleno.raiz.t, new List<Vector2[]>
        {
            new[] { new Vector2(0.05f, -0.3f), new Vector2(0.3f, -0.3f), new Vector2(0.3f, 0.05f), new Vector2(-0.1f, 0.1f) },
        }, ColorMaterial(new Color(0.45f, 0.68f, 0.42f)));
        IconoPaleta(ramaRelleno.raiz.t, new List<Vector2[]>
        {
            new[] { new Vector2(-0.3f, 0.15f), new Vector2(-0.1f, 0.1f), new Vector2(0.3f, 0.05f), new Vector2(0.3f, 0.3f), new Vector2(-0.3f, 0.3f) },
        }, ColorMaterial(new Color(0.62f, 0.82f, 0.55f)));
        ramaRelleno.raiz.alTocar = () => AbrirRama(ramaRelleno);
        ramaRelleno.raiz.texto = () => ramaAbierta == ramaRelleno ? "Relleno: " + NombreColor(colorRellenoNuevo) : "";

        // Textura (relleno vivo): tres manchas verdes de distinto tono.
        hijo = HijoRelleno(0, negro);
        IconoPaleta(hijo.t, new List<Vector2[]> { Circulo(new Vector2(-0.13f, 0.1f), 0.13f) }, ColorMaterial(new Color(0.3f, 0.5f, 0.32f)));
        IconoPaleta(hijo.t, new List<Vector2[]> { Circulo(new Vector2(0.14f, 0.08f), 0.11f) }, ColorMaterial(new Color(0.45f, 0.68f, 0.42f)));
        IconoPaleta(hijo.t, new List<Vector2[]> { Circulo(new Vector2(0f, -0.15f), 0.12f) }, ColorMaterial(new Color(0.62f, 0.82f, 0.55f)));
        hijo.amarillo = () => TexturaElegida > 0;
        hijo.alTocar = () => ElegirTextura((TexturaElegida + 1) % Trazo.NombresTextura.Length, VelocidadTexturaElegida);
        hijo.texto = () => "Textura " + TexturaElegida + " · " + Trazo.NombresTextura[TexturaElegida];
        hijo.tono = 1.15f;
        // Cubeta: un balde con su asa y una gota del color de relleno.
        hijo = HijoRelleno(1, negro);
        var balde = new List<Vector2[]>
        {
            new[] { new Vector2(-0.24f, 0.08f), new Vector2(0.16f, 0.08f), new Vector2(0.11f, -0.28f), new Vector2(-0.19f, -0.28f) },
        };
        LineaIcono(balde, new[] { new Vector2(-0.22f, 0.1f), new Vector2(-0.16f, 0.27f), new Vector2(0.08f, 0.27f), new Vector2(0.14f, 0.1f) }, 0.05f);
        IconoPaleta(hijo.t, balde, negro);
        gotaCubeta = ColorMaterial(ColorDeRelleno);
        IconoPaleta(hijo.t, new List<Vector2[]>
        {
            Circulo(new Vector2(0.27f, -0.14f), 0.09f),
            new[] { new Vector2(0.27f, 0.03f), new Vector2(0.19f, -0.11f), new Vector2(0.35f, -0.11f) },
        }, gotaCubeta);
        hijo.amarillo = () => CubetaActiva;
        hijo.alTocar = () =>
        {
            CubetaActiva = !CubetaActiva;
            MostrarEtiqueta(CubetaActiva ? "Cubeta: Sí" : "Cubeta: No");
        };
        hijo.texto = () => CubetaActiva ? "Cubeta: Sí" : "Cubeta: No";
        hijo.tono = 1.1f;
        // Velocidad de la textura (solo con textura): ">>".
        hijo = HijoRelleno(2, negro);
        var flechas = new List<Vector2[]>();
        IconoFlechas(flechas);
        IconoPaleta(hijo.t, flechas, negro);
        hijo.visible = () => TexturaElegida > 0;
        hijo.alTocar = () => ElegirTextura(TexturaElegida, (VelocidadTexturaElegida + 1) % Trazo.VelocidadesTextura.Length);
        hijo.texto = () => "Velocidad: " + Trazo.NombresVelocidadTextura[VelocidadTexturaElegida];
        hijo.tono = 1.3f;
    }

    // Los hijos de LÍNEA bajan por la izquierda de la paleta; los de RELLENO, por la derecha.
    BotonPaleta HijoLinea(int k, List<Vector2[]> icono, Material negro)
    {
        var b = CrearBotonPaleta("Linea" + k, AnguloLinea + PrimerHijo + k * PasoHijos, 0.02f, negro, ramaLinea);
        if (icono != null)
            IconoPaleta(b.t, icono, negro);
        return b;
    }

    BotonPaleta HijoRelleno(int k, Material negro)
    {
        return CrearBotonPaleta("Relleno" + k, AnguloRelleno - PrimerHijo - k * PasoHijos, 0.02f, negro, ramaRelleno);
    }

    DatosCapa CapaPaleta()
    {
        return dibujo != null ? dibujo.CapaActual : null;
    }

    // Tocar una raíz: se abre (y se cierra la otra); tocarla otra vez la cierra.
    void AbrirRama(RamaPaleta r)
    {
        ramaAbierta = ramaAbierta == r ? null : r;
        claveCentroRelleno = null;
    }

    // Cada cuadro: qué botones se ven, cuáles van en amarillo, los arcos y el tamaño de los colores.
    void RefrescarBotonesPaleta()
    {
        AcomodarHijos(ramaLinea);
        AcomodarHijos(ramaRelleno);
        foreach (var b in botonesPaleta)
        {
            bool ver = (b.rama == null || b.rama == ramaAbierta) && (b.visible == null || b.visible());
            if (b.t.gameObject.activeSelf != ver)
                b.t.gameObject.SetActive(ver);
            if (!ver)
                b.dedoEn = false;
            else if (b.fondo != null)
                PonerColorMaterial(b.fondo, b.amarillo != null && b.amarillo() ? AmarilloOpcion : Color.white);
        }
        ActualizarArco(ramaLinea);
        ActualizarArco(ramaRelleno);
        // El color marcado (más grande) es el de lo que está abierto: relleno o línea.
        Color objetivo = RellenoAbierto ? colorRellenoNuevo : dibujo.ColorNuevo;
        int marcado = IndiceColor(objetivo);
        for (int i = 0; i < botonesColor.Count; i++)
            botonesColor[i].localScale = Vector3.one * TamColor * (i == marcado ? 1.3f : 1f);
        if (botonInvisible != null)
            botonInvisible.t.localScale = Vector3.one * botonInvisible.tamano * (marcado < 0 ? 1.3f : 1f);
        PonerColorMaterial(gotaCubeta, ColorDeRelleno);
    }

    // Los hijos que se ven quedan seguidos, sin huecos (por ejemplo, el halo o el lápiz solo a veces).
    void AcomodarHijos(RamaPaleta r)
    {
        if (r == null)
            return;
        int k = 0;
        foreach (var h in r.hijos)
        {
            if (h.visible != null && !h.visible())
                continue;
            float angulo = r.raiz.angulo + r.sentido * (PrimerHijo + k * PasoHijos);
            k++;
            if (Mathf.Abs(angulo - h.angulo) < 0.01f)
                continue;
            h.angulo = angulo;
            float a = angulo * Mathf.Deg2Rad;
            h.t.localPosition = new Vector3(Mathf.Cos(a) * RadioBotones, Mathf.Sin(a) * RadioBotones, -0.001f);
        }
    }

    // El arco va de la raíz hasta su último hijo a la vista (se rehace solo si eso cambia).
    void ActualizarArco(RamaPaleta r)
    {
        if (r == null || r.arco == null)
            return;
        bool ver = ramaAbierta == r;
        if (r.arco.gameObject.activeSelf != ver)
            r.arco.gameObject.SetActive(ver);
        if (!ver)
            return;
        float hasta = r.raiz.angulo;
        foreach (var h in r.hijos)
            if (h.t.gameObject.activeSelf)
                hasta = h.angulo;
        if (hasta == r.hastaArco)
            return;
        r.hastaArco = hasta;
        const float ancho = 0.0015f;
        float desde = r.raiz.angulo;
        int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(hasta - desde) / 3f) + 1);
        var v = new Vector3[n * 2];
        var tri = new int[(n - 1) * 12];
        for (int i = 0; i < n; i++)
        {
            float a = Mathf.Lerp(desde, hasta, i / (float)(n - 1)) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            v[i * 2] = dir * (RadioBotones - ancho * 0.5f);
            v[i * 2 + 1] = dir * (RadioBotones + ancho * 0.5f);
            if (i == 0)
                continue;
            int p = (i - 1) * 2, q = i * 2, k = (i - 1) * 12;
            tri[k] = p; tri[k + 1] = p + 1; tri[k + 2] = q + 1;
            tri[k + 3] = p; tri[k + 4] = q + 1; tri[k + 5] = q;
            tri[k + 6] = p; tri[k + 7] = q + 1; tri[k + 8] = p + 1;
            tri[k + 9] = p; tri[k + 10] = q; tri[k + 11] = q + 1;
        }
        r.mallaArco.Clear();
        r.mallaArco.vertices = v;
        r.mallaArco.triangles = tri;
        r.mallaArco.RecalculateBounds();
    }

    // Toca un botón (el más cercano dentro de su radio). Devuelve true si el dedo está en uno.
    bool TocarBotonesPaleta(Vector3 punta)
    {
        BotonPaleta tocado = null;
        float mejor = float.MaxValue;
        foreach (var b in botonesPaleta)
        {
            if (!b.t.gameObject.activeSelf)
                continue;
            float d = Vector3.Distance(punta, b.t.position);
            if (d < b.radioToque && d < mejor)
            {
                mejor = d;
                tocado = b;
            }
        }
        foreach (var b in botonesPaleta)
            if (b != tocado)
                b.dedoEn = false;
        if (tocado == null)
            return false;
        if (!tocado.dedoEn)
        {
            tocado.dedoEn = true;
            if (tocado.alTocar != null)
                tocado.alTocar();
            Burbuja(tocado.t.position, tocado.tono);
            claveMuestra = int.MinValue;
            claveCentroRelleno = null;
            if (tocado.texto != null)
                PonerTextoCentro(tocado.texto());
            // Abierta con el botón: mientras ajustas, no se cierra.
            if (paletaFija)
                paletaFijaHasta = Mathf.Max(paletaFijaHasta, Time.time + 12f);
        }
        return true;
    }

    // ---------- El centro: la vista previa y su textito ----------

    void ArmarTextoCentro()
    {
        var go = new GameObject("TextoCentro", typeof(RectTransform));
        go.transform.SetParent(paleta, false);
        go.transform.localPosition = new Vector3(0f, -0.0185f, -0.0012f);
        var t = go.AddComponent<TextMeshPro>();
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.005f;
        t.fontSizeMax = 0.05f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.12f, 0.12f, 0.15f);
        t.rectTransform.sizeDelta = new Vector2(0.042f, 0.008f);
        t.text = "";
        textoCentro = t;
    }

    void PonerTextoCentro(string texto)
    {
        if (textoCentro != null)
            textoCentro.text = Idioma.T(texto);
    }

    // Un circulito con el relleno (color, textura, velocidad y tamaño): se ve con RELLENO abierto.
    void ArmarCentroRelleno()
    {
        var go = new GameObject("RellenoMuestra");
        go.transform.SetParent(paleta, false);
        go.transform.localPosition = new Vector3(0f, 0.004f, -0.0004f);
        mallaCentroRelleno = new Mesh { name = "RellenoMuestra" };
        mallasTemblor.Add(mallaCentroRelleno);
        go.AddComponent<MeshFilter>().sharedMesh = mallaCentroRelleno;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = dibujo != null ? dibujo.materialRelleno : null;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        centroRelleno = go.transform;
        go.SetActive(false);
    }

    void ActualizarCentroPaleta()
    {
        bool verRelleno = RellenoAbierto;
        ActualizarMuestraPaleta(!verRelleno);
        // La línea del centro lleva el color de las líneas nuevas (con tinta invisible, gris clarito).
        if (materialCentroPaleta != null && materialCentroPaleta.HasProperty("_BaseColor"))
            materialCentroPaleta.SetColor("_BaseColor", dibujo.ColorNuevo.a > 0.01f ? dibujo.ColorNuevo : new Color(0.72f, 0.74f, 0.8f));
        if (centroRelleno == null)
            return;
        bool hayColor = colorRellenoNuevo.a > 0.01f;
        bool ver = verRelleno && hayColor && dibujo.materialRelleno != null;
        if (centroRelleno.gameObject.activeSelf != ver)
            centroRelleno.gameObject.SetActive(ver);
        if (ver)
            LlenarCentroRelleno();
    }

    void LlenarCentroRelleno()
    {
        Color c = colorRellenoNuevo;
        int tex = TexturaElegida, vel = VelocidadTexturaElegida;
        float escala = EscalaTexturaElegida;
        string clave = c.ToString() + tex + "_" + vel + "_" + escala.ToString("0.000");
        if (clave == claveCentroRelleno)
            return;
        claveCentroRelleno = clave;
        const int lados = 28;
        const float radio = 0.017f;
        var v = new List<Vector3>(lados + 1);
        var colores = new List<Color>(lados + 1);
        var uv0 = new List<Vector4>(lados + 1);
        var uv1 = new List<Vector2>(lados + 1);
        var cero = new List<Vector4>(lados + 1);
        var tri = new List<int>(lados * 3);
        float porEscala = 1f / Trazo.LimitarEscala(escala);
        float cambios = Trazo.VelocidadesTextura[Mathf.Clamp(vel, 0, Trazo.VelocidadesTextura.Length - 1)];
        Color opaco = new Color(c.r, c.g, c.b, 1f);
        for (int i = 0; i <= lados; i++)
        {
            Vector3 p = Vector3.zero;
            if (i > 0)
            {
                float a = (i - 1) * Mathf.PI * 2f / lados;
                p = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radio;
            }
            v.Add(p);
            colores.Add(opaco);
            uv0.Add(new Vector4(p.x * porEscala, p.y * porEscala, tex, cambios));
            uv1.Add(new Vector2(5.117f, 0f));
            cero.Add(Vector4.zero);
        }
        for (int i = 0; i < lados; i++)
        {
            tri.Add(0);
            tri.Add(1 + (i + 1) % lados);
            tri.Add(1 + i);
        }
        mallaCentroRelleno.Clear();
        mallaCentroRelleno.SetVertices(v);
        mallaCentroRelleno.SetColors(colores);
        mallaCentroRelleno.SetUVs(0, uv0);
        mallaCentroRelleno.SetUVs(1, uv1);
        mallaCentroRelleno.SetUVs(2, cero);
        mallaCentroRelleno.SetUVs(3, cero);
        mallaCentroRelleno.SetTriangles(tri, 0);
        mallaCentroRelleno.RecalculateBounds();
    }

    // ---------- Datos de la línea y del relleno (cartelito debajo de la paleta) ----------
    // Cómo está la línea ahora (la elegida, o las nuevas): grosor, color, temblor, relleno... Lo último que
    // cambiaste sale resaltado un momento, así ves qué hace cada botón.
    static readonly string[] NombresColorPaleta =
    {
        "Negro", "Blanco", "Gris", "Amarillo", "Naranja", "Rojo", "Rosado", "Morado", "Azul", "Verde", "Café", "Piel"
    };
    TMP_Text textoDatosPaleta;
    readonly string[] datosAntes = new string[12];
    readonly float[] resaltarHasta = new float[12];
    readonly string[] datosAhora = new string[12];
    float proximosDatos;

    void ArmarDatosPaleta()
    {
        var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "FondoDatos";
        Destroy(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(paleta, false);
        fondo.transform.localPosition = new Vector3(0f, -0.128f, 0.001f);
        fondo.transform.localScale = new Vector3(0.2f, 0.064f, 1f);
        var r = fondo.GetComponent<Renderer>();
        r.sharedMaterial = ColorMaterial(new Color(0.97f, 0.97f, 0.95f));
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        var go = new GameObject("DatosLinea", typeof(RectTransform));
        go.transform.SetParent(paleta, false);
        go.transform.localPosition = new Vector3(0f, -0.128f, -0.001f);
        var t = go.AddComponent<TextMeshPro>();
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = 0.09f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.12f, 0.12f, 0.15f);
        t.rectTransform.sizeDelta = new Vector2(0.19f, 0.058f);
        textoDatosPaleta = t;
    }

    void ActualizarDatosPaleta()
    {
        if (textoDatosPaleta == null || dibujo == null || Time.time < proximosDatos)
            return;
        proximosDatos = Time.time + 0.1f;
        var elegida = dibujo.Seleccion;
        var capa = dibujo.CapaActual;
        var tb = dibujo.temblor;
        Color color = elegida != null ? elegida.color : dibujo.ColorNuevo;
        float mm = (elegida != null ? elegida.ancho * dibujo.EscalaMundo : dibujo.AnchoNuevoMundo) * 1000f;
        datosAhora[0] = Mathf.RoundToInt(mm) + " mm";
        datosAhora[1] = NombreColor(color);
        datosAhora[2] = capa != null ? capa.nombre : "";
        datosAhora[3] = tb != null ? "Temblor: " + tb.NombreNivel : "";
        datosAhora[4] = tb != null ? "Hebras: " + tb.Hebras + (tb.Hebras > 1 && capa != null ? " (" + Mathf.RoundToInt(capa.grosorHebra * 100f) + " %)" : "") : "";
        datosAhora[5] = tb != null ? (tb.GrosorVivo ? "Grosor vivo: Sí" : "Grosor vivo: No") : "";
        datosAhora[6] = tb != null ? (tb.Ciclo3 ? "Ciclo de 3" : "Libre") : "";
        datosAhora[7] = tb != null ? "Suavidad: " + tb.NombreSuavidad : "";
        datosAhora[8] = tb != null ? Mathf.RoundToInt(tb.CambiosPorSegundo) + " cambios/s" : "";
        bool deElegida = elegida != null && elegida.relleno;
        Color cr = deElegida ? elegida.ColorDelRelleno : colorRellenoNuevo;
        int tex = deElegida ? elegida.texturaRelleno : TexturaElegida;
        int vel = deElegida ? elegida.velocidadTextura : VelocidadTexturaElegida;
        float esc = deElegida ? elegida.escalaTextura : EscalaTexturaElegida;
        tex = Mathf.Clamp(tex, 0, Trazo.NombresTextura.Length - 1);
        datosAhora[9] = "Relleno: " + (cr.a < 0.01f ? "Sin relleno" : NombreColor(cr)) + " · " + Trazo.NombresTextura[tex]
                        + (tex > 0 ? " · " + Trazo.NombresVelocidadTextura[Mathf.Clamp(vel, 0, Trazo.VelocidadesTextura.Length - 1)]
                                   + " · x" + Trazo.LimitarEscala(esc).ToString("0.0") : "");
        datosAhora[10] = (CubetaActiva ? "Cubeta: Sí" : "Cubeta: No") + (capa != null ? (capa.iman ? " · Imán: Sí" : " · Imán: No") : "");
        datosAhora[11] = dibujo.HaloDisponible ? (dibujo.HaloEncendido ? "Halo: Sí" : "Halo: No") : "";
        for (int i = 0; i < datosAhora.Length; i++)
        {
            if (datosAntes[i] != null && datosAntes[i] != datosAhora[i])
                resaltarHasta[i] = Time.time + 2.5f;
            datosAntes[i] = datosAhora[i];
        }
        var sb = new System.Text.StringBuilder();
        sb.Append(elegida != null ? Idioma.T("Línea elegida: ") : Idioma.T("Líneas nuevas: "));
        Dato(sb, 0, ""); Dato(sb, 1, " · "); Dato(sb, 2, " · ");
        sb.Append('\n');
        Dato(sb, 3, ""); Dato(sb, 4, " · "); Dato(sb, 5, " · ");
        sb.Append('\n');
        Dato(sb, 6, ""); Dato(sb, 7, " · "); Dato(sb, 8, " · ");
        sb.Append('\n');
        Dato(sb, 9, "");
        sb.Append('\n');
        Dato(sb, 10, ""); Dato(sb, 11, " · ");
        textoDatosPaleta.text = sb.ToString();
    }

    // Un dato; si acaba de cambiar, resaltado (negrita y naranja).
    void Dato(System.Text.StringBuilder sb, int i, string antes)
    {
        string d = datosAhora[i];
        if (string.IsNullOrEmpty(d))
            return;
        sb.Append(antes);
        bool resaltar = Time.time < resaltarHasta[i];
        if (resaltar)
            sb.Append("<b><color=#D2560A>");
        sb.Append(Idioma.T(d));
        if (resaltar)
            sb.Append("</color></b>");
    }

    Material ColorMaterial(Color c)
    {
        var m = new Material(materialNodo);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        materialesPaleta.Add(m);
        return m;
    }

    static void DiscoPaleta(Transform padre, Material m, float diametro, Vector3 local)
    {
        var go = new GameObject("Disco");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = local;
        go.transform.localScale = Vector3.one * diametro;
        go.AddComponent<MeshFilter>().sharedMesh = MallaDisco();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // La puntita del índice derecho lleva el color elegido (como una gotita de pintura): el del relleno si
    // RELLENO está abierto; si no, el de la línea.
    Material MaterialCursorColor()
    {
        if (materialCursor == null)
            return null;
        if (materialCursorColor == null)
        {
            materialCursorColor = new Material(materialCursor);
            materialesPaleta.Add(materialCursorColor);
        }
        if (materialCursorColor.HasProperty("_BaseColor"))
            materialCursorColor.SetColor("_BaseColor", RellenoAbierto ? colorRellenoNuevo : dibujo.ColorNuevo);
        return materialCursorColor;
    }

    void LiberarPaleta()
    {
        foreach (var m in materialesPaleta)
            if (m != null)
                Destroy(m);
        materialesPaleta.Clear();
        LiberarTemblorPaleta();
    }
}

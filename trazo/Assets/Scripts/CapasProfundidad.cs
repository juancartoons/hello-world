using System.Collections.Generic;
using TMPro;
using UnityEngine;

// CAPAS EN PROFUNDIDAD (botón "Capas" del menú de la mano). En vez de una lista, las capas cuelgan como papeles,
// una detrás de otra, arriba al centro de tu vista (hacia donde sale el cartel de cómic, que se esconde mientras tanto).
//  - La capa ACTUAL: siempre en el mismo lugar, cerca (a unos 35 cm de tus ojos, al alcance del dedo), sólida, con
//    su nombre y sus botones (Ver, Boceto, Plano, Liberar y "+ Capa").
//  - Las de ADELANTE (más cerca de ti): fantasmas transparentes, sin letras, solo con los bordes. Mientras más cerca
//    de tus ojos, más arriba y más transparentes (la que queda en tus ojos ya no se ve; la siguiente, un 10 %...).
//  - Las de ATRÁS: más al fondo y un poco más abajo, también transparentes.
//    Capa 1 queda al fondo y la última más cerca de ti, igual que se apilan los planos 2D. Entre una y otra, 7 cm.
//  - EMPUJA la capa actual con el índice derecho (unos 3 cm hacia el fondo, tocando la parte del nombre): TODO el
//    montón se va hacia el fondo y la capa de adelante llega al lugar de la actual. ATRÁELA hacia ti: todo viene
//    hacia ti y la de atrás llega a ese lugar. Como papeles colgados.
//  - "+ Capa": una capa nueva adelante de todas (hasta 12); el montón se mueve hasta ella.
//  - Se cierra sola si no la tocas en 20 segundos (o con su X). Cerca de ella, el dedo queda libre: sin la bolita
//    de la tinta ni del borrador, y no dibuja.
//  - v36: una PASTILLITA siempre visible, chiquita y de vidrio, un poco más arriba ("Capa 2 · de 5"). Abierta con
//    el botón Capas, funciona como antes (se cierra a los 20 s o con su X).
//  - v38: la pastillita es una PERSIANA (ver Persiana.cs), más cerca, al alcance del dedo: tócala y baja la mano =
//    se despliegan las capas (y se quedan hasta que subas el tirador de abajo o toques la X).
//  - v39: SOLO persiana: mirar arriba ya no abre nada (salía sin querer mientras dibujabas o mirabas el fondo).
//    En la "vista limpia" la pastillita no se ve (las capas se abren con el botón Capas del reloj).
//  - v41: PANELES FLEXIBLES (ver PanelesFlexibles.cs). Pellizca la pastillita y llévala ABAJO: ahí se jala hacia
//    arriba y las capas salen DEBAJO de tu mirada, inclinadas hacia ti (las ves desde arriba). En espejo: las de
//    adelante bajan y las de atrás suben; el tirador para guardarlas queda encima de la tarjeta (jálalo hacia abajo).
//  - v43: una tercera fila: dos botones REDONDOS "2D" / "3D" (cada capa recuerda el suyo; ya no está en el reloj),
//    "Nombre" (el teclado del Quest: "3 · Fondo", sin perder el número) y "Borrar" (toca dos veces; deshacer la
//    devuelve). Mientras empujas o atraes las capas, los botones se esconden y salen 1 segundo después de que todo
//    queda quieto. Y se tocan A PROPÓSITO: el dedo llega de frente y se hunde 1 cm detrás del botón.
//    "Cámara": la capa de la cámara (ver CamaraEscena.cs).
public class CapasProfundidad : MonoBehaviour
{
    const float Ancho = 0.26f, Alto = 0.085f;   // v43: más alta (tercera fila)
    const float DistanciaActual = 0.35f;                    // metros de tus ojos a la capa actual
    const float PasoFondo = 0.07f;                          // metros entre una capa y la siguiente
    const float SubeAdelante = 0.024f, SubeMas = 0.005f;    // las de adelante suben (más mientras más cerca de ti)
    const float BajaAtras = 0.02f;                          // las de atrás bajan un poco
    const float CercaOjos = 0.07f;                          // a esta distancia de tus ojos (o menos) ya no se ven
    const float Empujon = 0.03f;                            // cuánto hay que empujar o atraer
    const float CerrarSola = 20f;
    const float Borde = 0.0022f;
    const float FilaBotones = -0.006f;                      // la fila de botones (abajo de la tarjeta)
    const float FilaBotones2 = -0.03f;                      // v43: la tercera fila (2D, 3D, Nombre, Borrar)
    const float HundirBoton = 0.01f;                        // v43: el toque se activa 1 cm detrás del botón
    const float EsperaBotones = 1f;                         // v43: segundos quietas antes de que salgan los botones
    const float FinZonaNombre = 0.04f;                      // la zona para empujar llega hasta aquí (a la derecha: "+" y X)
    const string AyudaNormal = "Empuja: capa de adelante · Atrae: la de atrás · Deja el dedo quieto en el nombre: cambiarlo";
    const float MantenerNombre = 1f;                        // v44: segundos con el dedo quieto en el nombre = teclado
    // La pastillita (la persiana).
    const float AnchoPastilla = 0.085f, AltoPastilla = 0.022f;
    const float BajoCapas = 18f;            // v41: abajo, grados debajo de hacia donde miras
    const string AvisoMaximo = "Ya no caben más capas (máximo 12)";

    static CapasProfundidad instancia;
    static int cercaCuadro = -10;

    public static bool Abierta => instancia != null && instancia.raiz != null && instancia.raiz.gameObject.activeSelf;
    // v41: dónde están abiertas: 1 = arriba, -1 = abajo, 0 = cerradas.
    public static int OrillaAbierta => Abierta ? (instancia.abajoAhora ? -1 : 1) : 0;
    // El dedo índice derecho está cerca de las capas (ControlManos esconde su bolita y no dibuja).
    public static bool DedoCerca => Time.frameCount - cercaCuadro <= 1;

    class Fantasma
    {
        public Transform t;
        public Material relleno, borde;
        public float s;
    }

    Dibujo dibujo;
    ControlManos control;
    Transform raiz, tarjeta;
    TMP_Text nombre, ayuda;
    BotonTocable btnVer, btnBoceto, btnPlano, btnLiberar, btnCerrar, btnMas;
    BotonTocable btn2D, btn3D, btnBorrar;                // v43 (v44: Borrar es un basurerito)
    BotonTocable btnMasCamara, btnSugiere;               // v44: en la tarjeta de una capa de cámara
    BotonTocable btnCuadros;                             // v46: "Cuadro a cuadro"
    BotonTocable btnNombre;                              // v50: cambiar el nombre (teclado de la app)
    Transform iconoCamara;                               // v44
    Material matMini;
    RenderTexture rtMini;
    Camera lenteMini;
    float proximaMini, quietoNombreDesde;
    bool miniSucia = true;
    int miniDe = -1;
    readonly List<Renderer> apagados = new List<Renderer>();
    bool liberarVisible;
    float quietoDesde = -10f, borrarHasta;
    TouchScreenKeyboard teclado;
    int capaTeclado = -1;
    readonly List<BotonTocable> botones = new List<BotonTocable>();
    Material materialPanel, materialBoton, materialBotonMarcado, materialTransparente;
    readonly List<Material> materiales = new List<Material>();
    readonly List<Fantasma> fantasmas = new List<Fantasma>();
    float ultimoToque, aparece = 1f, proximoRefresco, avisoHasta;
    int capaMostrada = -1;
    bool empujando, esperarSalir;
    float zInicio, desdeEmpuje, sacudida;
    bool porPersiana;                     // v38: abiertas con la persiana (se quedan hasta que las recojas)
    bool abajoAhora;                      // v41: abiertas ABAJO de tu mirada (la pastilla estaba abajo)
    Persiana persianaArriba, tiradorArriba;

    // La pastillita tiene que existir desde el principio (ControlManos lo llama cada cuadro).
    public static void Asegurar()
    {
        if (instancia != null)
            return;
        var go = new GameObject("CapasProfundidad");
        instancia = go.AddComponent<CapasProfundidad>();
    }

    public static void Alternar()
    {
        if (Abierta)
        {
            instancia.Cerrar();
            return;
        }
        if (instancia == null)
        {
            var go = new GameObject("CapasProfundidad");
            instancia = go.AddComponent<CapasProfundidad>();
        }
        instancia.Abrir(false);
    }

    // v40 (voz: "capa nueva"): abre las capas si hace falta y agrega una nueva adelante, como el botón "+ Capa".
    public static void CapaNueva()
    {
        if (!Abierta)
            Alternar();
        if (Abierta)
            instancia.AgregarCapa();
    }

    public static void CerrarSiAbierta()
    {
        if (Abierta)
            instancia.Cerrar();
    }

    void Abrir(bool conPersiana)
    {
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
        control = ControlManos.Instancia;
        if (dibujo == null || control == null)
            return;
        if (raiz == null)
        {
            BuscarMateriales();
            Armar();
        }
        // v41: salen en la orilla donde está su pastilla (abajo si la llevaste abajo, o si la mesa ya ocupa arriba).
        abajoAhora = !PanelesFlexibles.CapasPastillaArriba;
        raiz.gameObject.SetActive(true);
        porPersiana = conPersiana;
        aparece = 0f; // sale con un pequeño crecimiento
        if (control.Cabeza != null)
            control.SonidoBurbuja(control.Cabeza.position + control.Cabeza.forward * 0.4f);
        ultimoToque = Time.time;
        capaMostrada = -1;
        empujando = false;
        quietoDesde = -10f;
        borrarHasta = 0f;
        esperarSalir = true; // si el dedo ya estaba ahí, no empuja hasta que salga
        int cur = Actual;
        AsegurarFantasmas(cur);
        for (int k = 0; k < fantasmas.Count; k++)
            fantasmas[k].s = k - cur;
        Ayuda(AyudaNormal);
    }

    void Cerrar()
    {
        empujando = false;
        porPersiana = false;
        if (raiz != null)
            raiz.gameObject.SetActive(false);
    }

    void BuscarMateriales()
    {
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (nav != null)
        {
            materialPanel = nav.materialPanel;
            materialBoton = nav.materialBoton;
            materialBotonMarcado = nav.materialBotonMarcado;
        }
        var referencias = FindFirstObjectByType<Referencias>(FindObjectsInactive.Include);
        if (referencias != null)
            materialTransparente = referencias.materialImagenTransparente;
    }

    // ---------- Armar ----------

    void Armar()
    {
        raiz = new GameObject("Capas").transform;
        raiz.SetParent(transform, false);

        // La tarjeta de la capa actual: blanca con borde negro, el nombre arriba y los botones abajo.
        tarjeta = new GameObject("CapaActual").transform;
        tarjeta.SetParent(raiz, false);
        // v45: la tarjeta es más ancha a la izquierda: ahí va la miniatura, DENTRO de la tarjeta.
        Quad(tarjeta, "Fondo", new Vector3(-ExtraIzq * 0.5f, 0f, 0f), new Vector2(Ancho + ExtraIzq, Alto), materialPanel);
        Marco(tarjeta, materialBotonMarcado, 0.0018f, -0.0004f);
        nombre = Texto(tarjeta, "Capa 1", new Vector3(-0.06f, 0.024f, -0.002f), new Vector2(0.11f, 0.02f), 0.26f, Color.black);
        // v50: "Nombre" es un BOTÓN (tocar el nombre empujaba la capa): abre el teclado de la app.
        btnNombre = Boton(tarjeta, "Nombre", new Vector3(Ancho * 0.5f - 0.103f, 0.026f, 0f), new Vector2(0.04f, 0.018f));
        btnNombre.alTocar.AddListener(AbrirTeclado);
        BotonTocable.PonerNombre(btnNombre, "Cambiar el nombre de la capa");
        nombre.fontStyle = FontStyles.Bold;
        btnMas = Boton(tarjeta, "+ Capa", new Vector3(Ancho * 0.5f - 0.055f, 0.026f, 0f), new Vector2(0.05f, 0.018f));
        btnCerrar = Boton(tarjeta, "X", new Vector3(Ancho * 0.5f - 0.015f, 0.028f, 0f), new Vector2(0.022f, 0.022f)); // v37: más grande
        btnVer = Boton(tarjeta, "Ver", new Vector3(-0.0965f, FilaBotones, 0f), new Vector2(0.045f, 0.017f));
        btnBoceto = Boton(tarjeta, "Lápiz: No", new Vector3(-0.035f, FilaBotones, 0f), new Vector2(0.062f, 0.017f));
        btnPlano = Boton(tarjeta, "Plano: unido", new Vector3(0.035f, FilaBotones, 0f), new Vector2(0.062f, 0.017f));
        btnLiberar = Boton(tarjeta, "Liberar", new Vector3(0.0965f, FilaBotones, 0f), new Vector2(0.045f, 0.017f));
        // v43: la tercera fila.
        btn2D = BotonRedondo(tarjeta, "2D", new Vector3(-0.109f, FilaBotones2, 0f), 0.02f);
        btn3D = BotonRedondo(tarjeta, "3D", new Vector3(-0.082f, FilaBotones2, 0f), 0.02f);
        // v44: borrar = un basurerito en la esquina de abajo a la derecha (toca dos veces).
        btnBorrar = Boton(tarjeta, "", new Vector3(Ancho * 0.5f - 0.016f, FilaBotones2, 0f), new Vector2(0.02f, 0.02f));
        IconoBasura(btnBorrar.etiqueta.transform);
        btn2D.alTocar.AddListener(() => { dibujo.PonerPlano(true); Tocado(); });
        btn3D.alTocar.AddListener(() => { dibujo.PonerPlano(false); Tocado(); });
        btnBorrar.alTocar.AddListener(TocarBorrar);
        // v46: cuadro a cuadro (cada cuadro empieza vacío): convierte esta capa si está vacía, o crea una nueva.
        btnCuadros = Boton(tarjeta, "Cuadro a cuadro", new Vector3(0.012f, FilaBotones2, 0f), new Vector2(0.085f, 0.017f));
        btnCuadros.alTocar.AddListener(() => { dibujo.AlternarCuadroACuadro(); AsegurarFantasmas(Actual); RefrescarTarjeta(Actual); Tocado(); });
        BotonTocable.PonerVoz(btnCuadros, ControlManos.ComandoVoz.CuadroACuadro);
        BotonTocable.PonerVoz(btn2D, ControlManos.ComandoVoz.En2D);
        BotonTocable.PonerVoz(btn3D, ControlManos.ComandoVoz.En3D);
        BotonTocable.PonerNombre(btnBorrar, "Borrar esta capa (toca dos veces)");
        // v44: la tarjeta de una capa de CÁMARA: "+ Cámara" (otra cámara) y "Sugiere" (3 encuadres).
        btnMasCamara = Boton(tarjeta, "+ Cámara", new Vector3(-0.08f, FilaBotones, 0f), new Vector2(0.06f, 0.017f));
        btnSugiere = Boton(tarjeta, "Sugiere", new Vector3(-0.012f, FilaBotones, 0f), new Vector2(0.06f, 0.017f));
        btnMasCamara.alTocar.AddListener(() => { dibujo.AgregarCamara(); AsegurarFantasmas(Actual); Tocado(); });
        btnSugiere.alTocar.AddListener(() => { CamaraEscena.SugerirVoz(); Tocado(); });
        BotonTocable.PonerNombre(btnMasCamara, "Otra cámara (otro ángulo o recorrido)");
        BotonTocable.PonerVoz(btnSugiere, ControlManos.ComandoVoz.SugiereCamara);
        iconoCamara = IconoCamara(tarjeta, new Vector3(-Ancho * 0.5f + 0.016f, 0.024f, -0.002f));
        ArmarMiniatura();
        btnVer.alTocar.AddListener(() => { dibujo.AlternarVerCapa(Actual); Tocado(); });
        // v43: al poner el lápiz, la capa pasa a 2D de una vez (el lápiz es sobre una hoja).
        btnBoceto.alTocar.AddListener(() =>
        {
            if (dibujo.temblor != null)
            {
                dibujo.temblor.SiguienteBoceto();
                if (dibujo.CapaActual.boceto > 0 && !dibujo.plano)
                    dibujo.PonerPlano(true);
            }
            Tocado();
        });
        btnPlano.alTocar.AddListener(() => { dibujo.AlternarUnirPlano(); Tocado(); });
        btnLiberar.alTocar.AddListener(() => { dibujo.LiberarCapa(Actual); Tocado(); });
        btnMas.alTocar.AddListener(AgregarCapa);
        btnCerrar.alTocar.AddListener(Cerrar);
        // v42: la etiqueta de cada botón es la palabra que se dice ("Capa nueva", "Lápiz", "Liberar"...).
        btnMas.PonerTexto("Capa nueva");
        BotonTocable.PonerVoz(btnMas, ControlManos.ComandoVoz.CapaNueva);
        BotonTocable.PonerVoz(btnBoceto, ControlManos.ComandoVoz.Lapiz);
        BotonTocable.PonerVoz(btnLiberar, ControlManos.ComandoVoz.Liberar);
        BotonTocable.PonerNombre(btnPlano, "Plano de la capa: propio o unido");
        BotonTocable.PonerNombre(btnCerrar, "Guardar las capas");

        // Una ayudita debajo (blanca con contorno, para que se lea sobre cualquier fondo).
        ayuda = Texto(raiz, "", new Vector3(0f, -Alto * 0.5f - 0.015f, -0.002f), new Vector2(Ancho, 0.02f), 0.11f, Color.white);
        ayuda.outlineWidth = 0.25f;
        ayuda.outlineColor = new Color32(30, 30, 40, 255);
        AsegurarFantasmas(Actual);
    }

    bool ManoAbiertaCerca(ManoSeguida m)
    {
        if (m == null || !m.valida || tarjeta == null)
            return false;
        bool abierta = m.estiradoIndice > 1.5f && m.estiradoMedio > 1.5f && m.estiradoAnular > 1.5f && m.estiradoMenique > 1.5f;
        return abierta && Vector3.Distance(m.indice, tarjeta.position) < 0.25f;
    }

    int Actual => Mathf.Clamp(dibujo.capaActual, 0, Mathf.Max(0, dibujo.capas.Count - 1));

    // Un fantasma por capa. Los nuevos empiezan en su lugar (contando desde la capa "cur").
    void AsegurarFantasmas(int cur)
    {
        if (raiz == null || dibujo == null)
            return;
        while (fantasmas.Count < dibujo.capas.Count)
        {
            var f = new Fantasma();
            f.s = fantasmas.Count - cur;
            f.t = new GameObject("Fantasma" + (fantasmas.Count + 1)).transform;
            f.t.SetParent(raiz, false);
            f.relleno = Copia(materialTransparente, new Color(0.9f, 0.93f, 1f, 0.15f));
            f.borde = Copia(materialTransparente, new Color(0.85f, 0.92f, 1f, 0.6f));
            if (f.relleno != null)
                Quad(f.t, "Relleno", new Vector3(-ExtraIzq * 0.5f, 0f, 0f), new Vector2(Ancho + ExtraIzq, Alto), f.relleno);
            Marco(f.t, f.borde != null ? f.borde : materialBoton, Borde, -0.0003f);
            fantasmas.Add(f);
        }
    }

    Material Copia(Material m, Color c)
    {
        if (m == null)
            return null;
        var copia = new Material(m);
        if (copia.HasProperty("_BaseColor"))
            copia.SetColor("_BaseColor", c);
        if (copia.HasProperty("_Color"))
            copia.SetColor("_Color", c);
        materiales.Add(copia);
        return copia;
    }

    static void PonerAlfa(Material m, Color c)
    {
        if (m == null)
            return;
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }

    static Transform Quad(Transform padre, string nombre, Vector3 pos, Vector2 tam, Material m)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = nombre;
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(padre, false);
        q.transform.localPosition = pos;
        q.transform.localScale = new Vector3(tam.x, tam.y, 1f);
        var r = q.GetComponent<Renderer>();
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return q.transform;
    }

    // El borde de una tarjeta: 4 tiritas.
    const float ExtraIzq = 0.068f;   // v45: lo que crece la tarjeta a la izquierda (la miniatura)

    static void Marco(Transform padre, Material m, float grosor, float z)
    {
        float ancho = Ancho + ExtraIzq, cx = -ExtraIzq * 0.5f;
        Quad(padre, "BordeArriba", new Vector3(cx, Alto * 0.5f, z), new Vector2(ancho + grosor, grosor), m);
        Quad(padre, "BordeAbajo", new Vector3(cx, -Alto * 0.5f, z), new Vector2(ancho + grosor, grosor), m);
        Quad(padre, "BordeIzq", new Vector3(-Ancho * 0.5f - ExtraIzq, 0f, z), new Vector2(grosor, Alto + grosor), m);
        Quad(padre, "BordeDer", new Vector3(Ancho * 0.5f, 0f, z), new Vector2(grosor, Alto + grosor), m);
    }

    BotonTocable Boton(Transform padre, string texto, Vector3 pos, Vector2 tam)
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Boton_" + texto;
        cubo.transform.SetParent(padre, false);
        cubo.transform.localPosition = pos;
        cubo.transform.localScale = new Vector3(tam.x, tam.y, 0.006f);
        var r = cubo.GetComponent<Renderer>();
        if (materialBoton != null)
            r.sharedMaterial = materialBoton;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        var b = cubo.AddComponent<BotonTocable>();
        b.materialNormal = materialBoton;
        b.materialMarcado = materialBotonMarcado;
        b.etiqueta = Texto(padre, texto, pos + new Vector3(0f, 0f, -0.0036f), new Vector2(tam.x * 0.92f, tam.y * 0.8f), 0.16f, Color.black);
        b.etiqueta.fontStyle = FontStyles.Bold;
        b.fondoToque = HundirBoton;   // v43: a propósito (de frente y 1 cm adentro)
        botones.Add(b);
        return b;
    }

    // v43: un botón REDONDO (2D / 3D): el que toca es un cuadrito invisible del tamaño del círculo; el círculo
    // (un cilindro de frente) va de hijo.
    BotonTocable BotonRedondo(Transform padre, string texto, Vector3 pos, float diametro)
    {
        var go = new GameObject("Boton_" + texto);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(diametro, diametro, 0.006f);
        go.AddComponent<BoxCollider>();
        var cil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cil.name = "Circulo";
        Destroy(cil.GetComponent<Collider>());
        cil.transform.SetParent(go.transform, false);
        cil.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        cil.transform.localScale = new Vector3(1f, 0.5f, 1f);
        var r = cil.GetComponent<Renderer>();
        if (materialBoton != null)
            r.sharedMaterial = materialBoton;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        var b = go.AddComponent<BotonTocable>();
        b.materialNormal = materialBoton;
        b.materialMarcado = materialBotonMarcado;
        b.etiqueta = Texto(padre, texto, pos + new Vector3(0f, 0f, -0.0036f), new Vector2(diametro * 0.8f, diametro * 0.6f), 0.16f, Color.black);
        b.etiqueta.fontStyle = FontStyles.Bold;
        b.fondoToque = HundirBoton;
        botones.Add(b);
        return b;
    }

    // v43: los botones (menos la X) se esconden mientras empujas o atraes las capas, y salen un momento después
    // de que todo queda quieto (así no se toca uno sin querer al pasar de capa).
    void MostrarBotones(bool ver)
    {
        foreach (var b in botones)
        {
            if (b == null || b == btnCerrar)
                continue;
            bool v = ver && Aplica(b);
            if (b.gameObject.activeSelf == v)
                continue;
            b.gameObject.SetActive(v);
            if (b.etiqueta != null)
                b.etiqueta.gameObject.SetActive(v);
        }
    }

    // v44: qué botones tiene la tarjeta según su capa (de dibujo o de cámara).
    bool Aplica(BotonTocable b)
    {
        bool cam = dibujo != null && dibujo.EsCamara(Actual);
        if (b == btnMasCamara || b == btnSugiere)
            return cam;
        if (b == btnVer || b == btnBoceto || b == btnPlano || b == btn2D || b == btn3D || b == btnCuadros)
            return !cam;
        if (b == btnLiberar)
            return !cam && liberarVisible;
        return true;
    }

    // v44: el basurerito (rojo, para que se vea sobre el botón claro y sobre el oscuro de "¿borrar?").
    void IconoBasura(Transform padre)
    {
        var rojo = Copia(materialTransparente != null ? materialTransparente : materialBoton, new Color(0.78f, 0.16f, 0.16f, 1f));
        Quad(padre, "Tapa", new Vector3(0f, 0.0052f, 0f), new Vector2(0.012f, 0.0016f), rojo);
        Quad(padre, "Asa", new Vector3(0f, 0.0066f, 0f), new Vector2(0.004f, 0.0012f), rojo);
        Quad(padre, "Cuerpo", new Vector3(0f, -0.0012f, 0f), new Vector2(0.0095f, 0.0105f), rojo);
    }

    // v44: una camarita (cuerpo y lente) junto al nombre de las capas de cámara.
    Transform IconoCamara(Transform padre, Vector3 pos)
    {
        var raizIcono = new GameObject("IconoCamara").transform;
        raizIcono.SetParent(padre, false);
        raizIcono.localPosition = pos;
        var oscuro = materialBotonMarcado;
        Quad(raizIcono, "Cuerpo", new Vector3(-0.002f, 0f, 0f), new Vector2(0.014f, 0.01f), oscuro);
        Quad(raizIcono, "Lente", new Vector3(0.0075f, 0f, 0f), new Vector2(0.005f, 0.007f), oscuro);
        Quad(raizIcono, "Visor", new Vector3(-0.005f, 0.0062f, 0f), new Vector2(0.005f, 0.0024f), oscuro);
        raizIcono.gameObject.SetActive(false);
        return raizIcono;
    }

    // ---------- v44: la miniatura de la capa (a la izquierda de la tarjeta) ----------

    const float LadoMini = 0.058f;

    void ArmarMiniatura()
    {
        var marco = Quad(tarjeta, "MarcoMiniatura", new Vector3(-Ancho * 0.5f - ExtraIzq * 0.5f + 0.002f, 0f, -0.0005f),
                         new Vector2(LadoMini + 0.004f, LadoMini + 0.004f), Copia(materialBoton, new Color(0.55f, 0.56f, 0.6f, 1f)));
        rtMini = new RenderTexture(112, 112, 24, RenderTextureFormat.ARGB32);
        rtMini.Create();
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        matMini = nav != null && nav.materialImagen != null ? new Material(nav.materialImagen) : null;
        if (matMini != null)
        {
            materiales.Add(matMini);
            if (matMini.HasProperty("_BaseMap"))
                matMini.SetTexture("_BaseMap", rtMini);
            if (matMini.HasProperty("_MainTex"))
                matMini.SetTexture("_MainTex", rtMini);
            PonerAlfa(matMini, Color.white);
        }
        Quad(tarjeta, "Miniatura", marco.localPosition + new Vector3(0f, 0f, -0.001f), new Vector2(LadoMini, LadoMini), matMini);
        var go = new GameObject("LenteMiniatura");
        go.transform.SetParent(transform, false);
        lenteMini = go.AddComponent<Camera>();
        lenteMini.enabled = false;
        lenteMini.stereoTargetEye = StereoTargetEyeMask.None;
        lenteMini.clearFlags = CameraClearFlags.SolidColor;
        lenteMini.backgroundColor = Color.white;
        lenteMini.nearClipPlane = 0.01f;
        lenteMini.farClipPlane = 50f;
        lenteMini.fieldOfView = 40f;
        lenteMini.aspect = 1f;
        dibujo.alCambiar += () => miniSucia = true;
    }

    // Solo cuando cambia algo (no cada cuadro): se esconden un momento las otras capas y se toma la foto.
    void ActualizarMiniatura(int cur)
    {
        if (rtMini == null || lenteMini == null)
            return;
        bool cam = dibujo.EsCamara(cur);
        if (cur != miniDe)
        {
            miniDe = cur;
            miniSucia = true;
        }
        if (cam)
        {
            // La de la cámara: lo que ve su monitor (si ya hay cámara puesta).
            var mon = CamaraEscena.Monitor;
            if (Time.time >= proximaMini && mon != null && CamaraEscena.Hay)
            {
                proximaMini = Time.time + 0.5f;
                Graphics.Blit(mon, rtMini);
            }
            return;
        }
        if (!miniSucia || Time.time < proximaMini)
            return;
        miniSucia = false;
        proximaMini = Time.time + 0.6f;
        // La caja de las líneas de esta capa.
        bool hay = false;
        Bounds caja = new Bounds();
        foreach (var t in dibujo.trazos)
        {
            if (t == null || t.capa != cur || !t.gameObject.activeInHierarchy)
                continue;
            var r = t.GetComponent<Renderer>();
            if (r == null)
                continue;
            if (!hay)
            {
                caja = r.bounds;
                hay = true;
            }
            else
            {
                caja.Encapsulate(r.bounds);
            }
        }
        apagados.Clear();
        if (hay)
            foreach (var t in dibujo.trazos)
            {
                if (t == null || t.capa == cur || !t.gameObject.activeInHierarchy)
                    continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>())
                    if (r.enabled)
                    {
                        r.enabled = false;
                        apagados.Add(r);
                    }
            }
        Vector3 centro = hay ? caja.center : dibujo.transform.position;
        Vector3 desde = control != null && control.Cabeza != null ? control.Cabeza.position : centro - Vector3.forward;
        Vector3 dir = centro - desde;
        if (dir.sqrMagnitude < 1e-4f)
            dir = Vector3.forward;
        dir.Normalize();
        float radio = hay ? Mathf.Max(0.02f, caja.extents.magnitude) : 0.1f;
        float dist = radio / Mathf.Tan(20f * Mathf.Deg2Rad) * 1.1f;
        lenteMini.transform.SetPositionAndRotation(centro - dir * dist, Quaternion.LookRotation(dir, Vector3.up));
        lenteMini.cullingMask = hay ? dibujo.MascaraExportar : 0;
        Exportar.Captura.RenderizarEn(lenteMini, rtMini);
        foreach (var r in apagados)
            if (r != null)
                r.enabled = true;
        apagados.Clear();
    }

    // v43: "Nombre": el teclado del Quest. Lo que escribas queda junto al número ("3 · Fondo").
    void AbrirTeclado()
    {
        Tocado();
        // v50: el teclado propio de la app (el del sistema del Quest no salía).
        int capa = Actual;
        var c = dibujo.capas[capa];
        string actual = c.nombre != null && c.nombre.StartsWith("Capa ") ? "" : c.nombre;
        TecladoVR.Abrir("Nombre de la capa", actual ?? "", nombre =>
        {
            if (dibujo != null && capa >= 0 && capa < dibujo.capas.Count)
            {
                dibujo.RenombrarCapa(capa, nombre);
                proximoRefresco = 0f;
            }
        });
    }

    void RevisarTeclado()
    {
        if (teclado == null)
            return;
        ultimoToque = Time.time;   // mientras escribes, las capas no se cierran
        var estado = teclado.status;
        if (estado == TouchScreenKeyboard.Status.Visible)
            return;
        if (estado == TouchScreenKeyboard.Status.Done && capaTeclado >= 0 && capaTeclado < dibujo.capas.Count)
        {
            dibujo.RenombrarCapa(capaTeclado, teclado.text);
            proximoRefresco = 0f;
        }
        teclado = null;
        capaTeclado = -1;
    }

    // v43: "Borrar": el primer toque pregunta ("¿Borrar?"); el segundo (en 3 s) la borra. Deshacer la devuelve.
    void TocarBorrar()
    {
        Tocado();
        if (Time.time > borrarHasta)
        {
            borrarHasta = Time.time + 3f;
            btnBorrar.Marcar(true);
            Ayuda("Toca otra vez para borrar " + dibujo.NombreCapa(Actual) + " (deshacer la devuelve)");
            avisoHasta = Time.time + 3f;
            return;
        }
        borrarHasta = 0f;
        if (dibujo.BorrarCapa(Actual))
        {
            control.SonidoBurbuja(tarjeta.position);
            capaMostrada = -1;
        }
        RefrescarTarjeta(Actual);
    }

    static TMP_Text Texto(Transform padre, string texto, Vector3 pos, Vector2 tam, float maximo, Color color)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        Idioma.Poner(t, texto);
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = maximo;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.rectTransform.sizeDelta = tam;
        return t;
    }

    void Ayuda(string texto)
    {
        if (ayuda != null)
            Idioma.Poner(ayuda, texto);
    }

    void Tocado()
    {
        ultimoToque = Time.time;
        proximoRefresco = 0f;
    }

    // ---------- Cada cuadro ----------

    void Update()
    {
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
        if (control == null)
            control = ControlManos.Instancia;
        if (dibujo == null || control == null || control.Cabeza == null)
        {
            if (Abierta)
                Cerrar();
            return;
        }
        AsegurarPersianas();
        if (!Abierta)
            return;
        float dt = Time.deltaTime;
        // El lugar de la capa actual: hacia el cartel de cómic, pero cerca de ti (al alcance del dedo).
        // v41: abajo, otro tanto DEBAJO de hacia donde miras, inclinada para mirarte.
        Vector3 pos;
        Quaternion rot;
        CartelArriba.Pose(control.Cabeza, out pos, out rot);
        Vector3 ojos = control.Cabeza.position;
        Vector3 rayo = pos - ojos;
        if (abajoAhora)
            rayo = Quaternion.AngleAxis(PanelesFlexibles.ArribaCartel + BajoCapas, rot * Vector3.right) * rayo;
        if (rayo.sqrMagnitude > 1e-6f)
        {
            pos = ojos + rayo.normalized * DistanciaActual;
            if (abajoAhora)
                rot = Quaternion.LookRotation(rayo, Vector3.up);
        }
        raiz.SetPositionAndRotation(pos, rot);
        Vector3 adelante = rot * Vector3.forward;

        AsegurarFantasmas(Actual);
        RevisarTeclado();
        if (borrarHasta > 0f && Time.time > borrarHasta)
        {
            borrarHasta = 0f;
            proximoRefresco = 0f;
        }
        aparece = Mathf.MoveTowards(aparece, 1f, dt * 5f);
        sacudida = Mathf.MoveTowards(sacudida, 0f, dt * 3f);
        // El dedo (puede cambiar de capa). z: cuánto empujas (+) o atraes (-) todo el montón.
        float z = 0f;
        RevisarDedo(ref z);

        int cur = Actual;
        if (cur != capaMostrada)
        {
            if (capaMostrada >= 0 && cur < fantasmas.Count)
            {
                aparece = 0f;
                // La tarjeta viaja con su capa; si el salto es largo (una capa nueva), llega desde cerquita.
                fantasmas[cur].s = Mathf.Clamp(fantasmas[cur].s, -1.3f, 1.3f);
            }
            capaMostrada = cur;
            proximoRefresco = 0f;
        }
        if (Time.time >= proximoRefresco)
        {
            proximoRefresco = Time.time + 0.25f;
            RefrescarTarjeta(cur);
        }
        ActualizarMiniatura(cur);

        // Todas las capas se acomodan (suave) en su lugar: adelante las más altas, atrás las más bajas.
        // Así, al cambiar de capa, se mueve todo el montón junto.
        float a = 1f - Mathf.Exp(-8f * dt);
        float desliz = z / PasoFondo;
        for (int k = 0; k < fantasmas.Count; k++)
        {
            var f = fantasmas[k];
            bool existe = k < dibujo.capas.Count;
            if (existe)
                f.s = Mathf.Lerp(f.s, k - cur, a);
            float s = f.s - desliz;
            Vector3 lugar = Lugar(s);
            float relleno, borde;
            if (s > 0f)
            {
                // Adelante: mientras más cerca de tus ojos, más transparente (en tus ojos, invisible; la siguiente
                // un 10 %, la otra un 20 %...).
                float frente = Vector3.Dot(raiz.TransformPoint(lugar) - ojos, adelante);
                borde = Mathf.Clamp((frente - CercaOjos) / PasoFondo * 0.1f, 0f, 0.4f);
                relleno = borde * 0.2f;
            }
            else
            {
                // Atrás: un poco más visible. Más lejos = más suave.
                float lejos = 1f / (1f + 0.35f * Mathf.Max(0f, -s - 1f));
                borde = 0.7f * lejos;
                relleno = 0.2f * lejos;
            }
            // La actual es la tarjeta sólida (no su fantasma).
            bool ver = existe && k != cur && borde > 0.004f;
            if (f.t.gameObject.activeSelf != ver)
                f.t.gameObject.SetActive(ver);
            if (!ver)
                continue;
            f.t.localPosition = lugar;
            float oculta = dibujo.capas[k].visible ? 1f : 0.45f;
            PonerAlfa(f.relleno, new Color(0.9f, 0.93f, 1f, relleno * oculta));
            PonerAlfa(f.borde, new Color(0.85f, 0.92f, 1f, borde * oculta));
        }

        // La tarjeta: va donde está su capa (llega con todo el montón), crece un poquito al cambiar de capa
        // y se sacude si ya no hay más capas hacia ese lado.
        float sActual = (cur < fantasmas.Count ? fantasmas[cur].s : 0f) - desliz;
        float x = Mathf.Sin(sacudida * 40f) * 0.006f * sacudida;
        tarjeta.localPosition = Lugar(sActual) + new Vector3(x, 0f, 0f);
        tarjeta.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, Suave(aparece));
        // v43: los botones, solo con todo quieto (y un momento después).
        if (empujando || Mathf.Abs(sActual) > 0.03f || Mathf.Abs(z) > 0.002f)
            quietoDesde = Time.time;
        // v49: "mano de empujar" vs "dedo de tocar": con una mano ABIERTA (los cuatro dedos estirados) cerca de las
        // capas, los botones se apagan; vuelven cuando cierras la mano y señalas con el índice solo.
        if (ManoAbiertaCerca(control.Der) || ManoAbiertaCerca(control.Izq))
            quietoDesde = Mathf.Max(quietoDesde, Time.time - EsperaBotones + 0.35f);
        MostrarBotones(Time.time - quietoDesde > EsperaBotones);

        if (Time.time > avisoHasta && avisoHasta > 0f)
        {
            avisoHasta = 0f;
            Ayuda(AyudaNormal);
        }
        // Abiertas con el botón: se cierran solas a los 20 s. Con la persiana: hasta que las recojas.
        if (!porPersiana && Time.time - ultimoToque > CerrarSola)
            Cerrar();
    }

    // ---------- v38: las persianas ----------
    // Arriba, la pastillita "Capa 2 · de 5". Tócala y baja la mano: se despliegan las capas. Abiertas, un tirador
    // debajo de la tarjeta las recoge (súbelo), o su X.

    void AsegurarPersianas()
    {
        if (persianaArriba != null)
            return;
        var go = new GameObject("PersianaCapas");
        go.transform.SetParent(transform, false);
        persianaArriba = go.AddComponent<Persiana>();
        persianaArriba.seEsconde = true;    // en la vista limpia no se ve
        persianaArriba.sentido = -1;
        persianaArriba.ancho = AnchoPastilla;
        persianaArriba.alto = AltoPastilla;
        // v41: arriba o abajo (se mueve con un pellizco; ver PanelesFlexibles.cs).
        persianaArriba.pose = PanelesFlexibles.PoseCapas;
        persianaArriba.arriba = () => PanelesFlexibles.CapasPastillaArriba;
        persianaArriba.alMoverA = MoverPastilla;
        persianaArriba.comandoVoz = ControlManos.ComandoVoz.Capas;
        persianaArriba.visible = () => !Abierta && !Tutorial.EnCurso && !ExportadorVideo.Exportando;
        persianaArriba.texto = () => dibujo != null ? "Capa " + (Actual + 1) + " · de " + dibujo.capas.Count : "";
        persianaArriba.alAbrir = () => Abrir(true);
        persianaArriba.puedeJalar = () => control != null && !control.TrazandoAhora;   // v44: sin depender de la otra mano

        var go2 = new GameObject("TiradorCapas");
        go2.transform.SetParent(transform, false);
        tiradorArriba = go2.AddComponent<Persiana>();
        tiradorArriba.sentido = 1;
        // Arriba se recogen subiéndolo; abajo (v41), bajándolo. ("arriba" = se jala hacia abajo.)
        tiradorArriba.arriba = () => abajoAhora;
        tiradorArriba.comandoVoz = ControlManos.ComandoVoz.Capas;
        tiradorArriba.ancho = 0.05f;
        tiradorArriba.alto = 0.012f;
        tiradorArriba.pose = PoseTirador;
        tiradorArriba.visible = () => Abierta;
        tiradorArriba.alAbrir = Cerrar;
    }

    // v41: llevaste la pastillita arriba o abajo con un pellizco.
    void MoverPastilla(bool arriba)
    {
        PanelesFlexibles.PonerCapasArriba(arriba);
        if (dibujo == null)
            return;
        if (PanelesFlexibles.CapasPastillaArriba != arriba)
            dibujo.Mensaje("La mesa ocupa esa orilla: mientras esté abierta, las capas salen en la otra");
        else
            dibujo.Mensaje(arriba ? "Capas arriba: jala la pastillita hacia abajo" : "Capas abajo: jala la pastillita hacia arriba");
    }

    // El tirador: debajo de la tarjeta (y de su ayudita). v41: con las capas abajo, encima de la tarjeta.
    void PoseTirador(Transform cabeza, out Vector3 pos, out Quaternion rot)
    {
        if (raiz == null)
        {
            pos = cabeza.position + cabeza.forward * 0.35f;
            rot = cabeza.rotation;
            return;
        }
        pos = raiz.TransformPoint(new Vector3(0f, abajoAhora ? Alto * 0.5f + 0.025f : -Alto * 0.5f - 0.04f, 0f));
        rot = raiz.rotation;
    }

    // Dónde cuelga una capa que está "s" pasos adelante (+) o atrás (-) de la actual (local de la raíz).
    // v41: abajo, en espejo (las de adelante bajan, para no taparte la vista; las de atrás suben un poco).
    Vector3 Lugar(float s)
    {
        float y = s > 0f ? s * SubeAdelante + s * s * SubeMas : s * BajaAtras;
        if (abajoAhora)
            y = -y;
        return new Vector3(0f, y, -s * PasoFondo);
    }

    static float Suave(float u)
    {
        u = Mathf.Clamp01(u);
        return u * u * (3f - 2f * u);
    }

    // El dedo: cerca = queda libre (sin bolita); en la parte del nombre se empuja o se atrae todo el montón.
    void RevisarDedo(ref float z)
    {
        var der = control.Der;
        if (!der.valida)
        {
            empujando = false;
            return;
        }
        Vector3 p = raiz.InverseTransformPoint(der.indice);
        bool cerca = Mathf.Abs(p.x) < Ancho * 0.5f + ExtraIzq + 0.03f && p.y > -Alto * 0.5f - 0.06f && p.y < Alto * 0.5f + 0.05f
                     && p.z > -0.09f && p.z < 0.09f;
        if (cerca)
        {
            cercaCuadro = Time.frameCount;
            ultimoToque = Time.time;
        }
        // La parte de arriba de la tarjeta (la del nombre), lejos de "+ Capa" y de la X.
        bool enZona = p.x > -Ancho * 0.5f + 0.004f && p.x < FinZonaNombre && p.y > FilaBotones + 0.013f && p.y < Alto * 0.5f;
        if (esperarSalir)
        {
            if (!(enZona && Mathf.Abs(p.z) < 0.04f))
                esperarSalir = false;
            empujando = false;
            return;
        }
        if (!empujando)
        {
            if (enZona && Mathf.Abs(p.z) < 0.012f)
            {
                empujando = true;
                zInicio = p.z;
                desdeEmpuje = Time.time;
                quietoNombreDesde = Time.time;
            }
            return;
        }
        float dz = p.z - zInicio;
        // v44: el dedo QUIETO en el nombre un segundo = cambiarle el nombre (sale el teclado). El nombre se pone
        // verde azulado mientras tanto.
        if (Mathf.Abs(dz) > 0.008f)
            quietoNombreDesde = Time.time;
        float mantener = Mathf.Clamp01((Time.time - quietoNombreDesde) / MantenerNombre);
        nombre.color = Color.Lerp(Color.black, new Color(0.16f, 0.62f, 0.58f), mantener);
        if (mantener >= 1f)
        {
            empujando = false;
            esperarSalir = true;
            nombre.color = Color.black;
            AbrirTeclado();
            return;
        }
        bool fuera = Mathf.Abs(p.x) > Ancho * 0.5f + 0.03f || p.y < -Alto * 0.5f - 0.03f || p.y > Alto * 0.5f + 0.04f;
        if (fuera || Time.time - desdeEmpuje > 2.5f)
        {
            empujando = false;
            return;
        }
        z = Mathf.Clamp(dz, -Empujon, Empujon);
        if (dz > Empujon)
        {
            Cambiar(1, z);
            z = 0f;
        }
        else if (dz < -Empujon)
        {
            Cambiar(-1, z);
            z = 0f;
        }
    }

    // +1 = empujaste: la de adelante pasa a ser la actual. -1 = atrajiste: la de atrás pasa a ser la actual.
    // z: lo que ya habías empujado o atraído (el montón sigue desde ahí, sin saltos).
    void Cambiar(int direccion, float z)
    {
        empujando = false;
        esperarSalir = true;
        ultimoToque = Time.time;
        int cur = Actual;
        int nuevo = cur + direccion;
        if (nuevo < 0 || nuevo >= dibujo.capas.Count)
        {
            sacudida = 1f;
            Ayuda(direccion > 0 ? "Esta ya es la capa de más adelante (+ Capa: una nueva)" : "Esta ya es la capa del fondo");
            avisoHasta = Time.time + 2f;
            control.SonidoBurbuja(tarjeta.position);
            return;
        }
        float pasos = z / PasoFondo;
        foreach (var f in fantasmas)
            f.s -= pasos;
        dibujo.SeleccionarCapa(nuevo);
        control.SonidoBurbuja(tarjeta.position);
        RefrescarTarjeta(nuevo);
    }

    // "+ Capa": una capa nueva adelante de todas; el montón se mueve hasta ella.
    void AgregarCapa()
    {
        Tocado();
        int antes = Actual;
        int nueva = dibujo.AgregarCapa();
        if (nueva < 0)
        {
            sacudida = 1f;
            Ayuda(AvisoMaximo);
            avisoHasta = Time.time + 2f;
            return;
        }
        AsegurarFantasmas(antes); // la nueva aparece adelante, en su lugar, y desde ahí llega
        control.SonidoBurbuja(tarjeta.position);
        RefrescarTarjeta(nueva);
    }

    void RefrescarTarjeta(int cur)
    {
        if (cur < 0 || cur >= dibujo.capas.Count)
            return;
        var c = dibujo.capas[cur];
        Idioma.Poner(nombre, dibujo.NombreCapa(cur));
        // v42: el botón dice lo que hace (y es su palabra de voz): "Oculta" la capa, o "Muestra" la que está oculta.
        btnVer.PonerTexto(c.visible ? "Oculta" : "Muestra");
        btnVer.comandoVoz = c.visible ? ControlManos.ComandoVoz.Oculta : ControlManos.ComandoVoz.Muestra;
        btnVer.Marcar(!c.visible);
        // v42: "Lápiz: Sí / No" (antes Boceto No / Gris / Azul).
        btnBoceto.PonerTexto(c.boceto > 0 ? "Lápiz: Sí" : "Lápiz: No");
        btnBoceto.Marcar(c.boceto > 0);
        btnPlano.PonerTexto(c.unido ? "Plano: unido" : "Plano: propio");
        btnPlano.Marcar(c.unido);
        liberarVisible = dibujo.EncantadasEnCapa(cur) > 0 || (!c.unido && c.hayPlano);
        // v43: 2D / 3D (el de esta capa, marcado) y Borrar (si no está preguntando).
        btn2D.Marcar(c.en2D);
        if (!empujando)
            nombre.color = Color.black;
        if (iconoCamara.gameObject.activeSelf != c.camara)
            iconoCamara.gameObject.SetActive(c.camara);
        btn3D.Marcar(!c.en2D);
        btnCuadros.PonerTexto(c.cuadroACuadro ? "Cuadro a cuadro: Sí" : "Cuadro a cuadro");
        btnCuadros.Marcar(c.cuadroACuadro);
        if (Time.time > borrarHasta)
            btnBorrar.Marcar(false);
    }

    void OnDestroy()
    {
        foreach (var m in materiales)
            if (m != null)
                Destroy(m);
        if (instancia == this)
            instancia = null;
    }
}

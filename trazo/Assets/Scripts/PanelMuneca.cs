using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Menú: aparece como si miraras la hora: el dorso de la muñeca izquierda hacia tu cara (y la miras).
// Flota unos centímetros hacia ti y sigue a la mano. Los botones se tocan con el índice derecho.
// También muestra avisos cortos frente a tus ojos ("Deshecho", "Líneas unidas"...).
public class PanelMuneca : MonoBehaviour
{
    public ControlManos control;
    public Dibujo dibujo;
    public Escenario escenario;
    public GameObject contenido;
    public BotonTocable btnPlano, btnFondo, btnGuardar, btnCargar, btnBorrar;
    [Tooltip("Imán de la capa activa (con su ícono de imán encendido o apagado)")]
    public BotonTocable btnIman;
    [Tooltip("Abre la paleta de colores (por si el gesto de la palma no la abre)")]
    public BotonTocable btnColores;
    [Tooltip("Abre las capas en profundidad (arriba al centro de tu vista)")]
    public BotonTocable btnCapas;
    [Tooltip("? = las secciones del tutorial")]
    public BotonTocable btnAyuda;
    [Tooltip("X para cerrar el menú")]
    public BotonTocable btnCerrar;
    [Tooltip("v38: girar el fondo 360 a la izquierda / a la derecha (30°)")]
    public BotonTocable btnGirarIzq, btnGirarDer;
    [Tooltip("v39: vista limpia (sin las pastillitas, el parlante ni las letras de cómic)")]
    public BotonTocable btnVistaLimpia;
    [Tooltip("v39: saca la mesa de trabajo (para la vista limpia, donde no está su pastillita)")]
    public BotonTocable btnMesa;
    Renderer[] iconoIman;
    Material imanEncendido, imanApagado;
    [Header("Figuras 3D")]
    public Figuras figuras;
    public BotonTocable btnEsfera, btnCubo, btnCilindro, btnALineas, btnQuitarFigura;
    public TMP_Text textoEstado;
    public TMP_Text textoAviso;
    public float suavizado = 20f;

    const string textoAyuda = "Izq + pulgar: índice dibuja · índice+medio recta · medio nodos · anular grosor\nPuño: borrar · Pulgar izq/der: deshacer/rehacer · Doble toque: candado · Pastillitas: capas (arriba) y mesa (abajo)\nGirar: mira a un lado, toca la flecha y desliza · Doble puño der.: vista limpia · Micrófono: voz";

    bool visible;
    float ultimaVezVisto;
    float ocultarMensajeEn;
    float ocultarAvisoEn;
    float avisoDesde;

    // Grabando el proceso: abajo del menú sale "● 01:23" con Pausa y Parar (como mirar la hora).
    GrabadorProceso grabador;
    GameObject filaRec;
    TMP_Text textoRec;
    BotonTocable btnPausaRec, btnPararRec;
    float proximoRec;
    TMP_Text textoFps; // los cuadros por segundo (modo rendimiento), pequeñito arriba

    void Start()
    {
        if (control == null) control = FindFirstObjectByType<ControlManos>();
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (escenario == null) escenario = FindFirstObjectByType<Escenario>();

        if (dibujo != null)
        {
            // v43: el 2D / 3D ya no va en el reloj (va en cada capa). Si la escena es de antes, se esconde.
            if (btnPlano != null)
            {
                btnPlano.gameObject.SetActive(false);
                if (btnPlano.etiqueta != null)
                    btnPlano.etiqueta.gameObject.SetActive(false);
                btnPlano = null;
            }
            Conectar(btnGuardar, dibujo.Guardar);
            Conectar(btnCargar, AbrirMisArchivos);
            Conectar(btnBorrar, dibujo.BorrarTodo);
            Conectar(btnIman, dibujo.AlternarIman);
            dibujo.alCambiar += Refrescar;
            dibujo.alMensaje += Mensaje;
        }
        Conectar(btnFondo, SiguienteFondo);
        Conectar(btnCapas, () =>
        {
            CapasProfundidad.Alternar();
            Refrescar();
        });
        if (control != null)
        {
            Conectar(btnGirarIzq, () => control.GirarFondoBoton(-1));
            Conectar(btnVistaLimpia, () =>
            {
                VistaLimpia.Alternar();
                Refrescar();
            });
            Conectar(btnMesa, () =>
            {
                control.CerrarMenu();
                if (control.panelArriba != null)
                    control.panelArriba.AbrirMesa();
            });
            Conectar(btnGirarDer, () => control.GirarFondoBoton(1));
            Conectar(btnCerrar, control.CerrarMenu);
            Conectar(btnColores, control.AlternarPaleta);
            Conectar(btnAyuda, () =>
            {
                control.CerrarMenu();
                if (Tutorial.Instancia != null)
                    Tutorial.Instancia.AbrirSecciones();
            });
        }
        if (figuras == null && dibujo != null)
            figuras = dibujo.figuras;
        if (figuras != null)
        {
            Conectar(btnEsfera, () => figuras.Agregar(0));
            Conectar(btnCubo, () => figuras.Agregar(1));
            Conectar(btnCilindro, () => figuras.Agregar(2));
            Conectar(btnALineas, figuras.ConvertirSeleccionada);
            Conectar(btnQuitarFigura, figuras.QuitarSeleccionada);
        }

        // v42: la etiqueta de cada botón es la palabra que se dice (ver EtiquetasBotones y ControlManosVoz).
        BotonTocable.PonerVoz(btnPlano, ControlManos.ComandoVoz.Plano);
        BotonTocable.PonerVoz(btnFondo, ControlManos.ComandoVoz.Fondo);
        BotonTocable.PonerVoz(btnGuardar, ControlManos.ComandoVoz.Guarda);
        BotonTocable.PonerVoz(btnCargar, ControlManos.ComandoVoz.Archivos);
        BotonTocable.PonerVoz(btnEsfera, ControlManos.ComandoVoz.Esfera);
        BotonTocable.PonerVoz(btnCubo, ControlManos.ComandoVoz.Cubo);
        BotonTocable.PonerVoz(btnCilindro, ControlManos.ComandoVoz.Cilindro);
        BotonTocable.PonerVoz(btnALineas, ControlManos.ComandoVoz.ALineas);
        BotonTocable.PonerVoz(btnCapas, ControlManos.ComandoVoz.Capas);
        BotonTocable.PonerVoz(btnGirarIzq, ControlManos.ComandoVoz.GiraIzquierda);
        BotonTocable.PonerVoz(btnGirarDer, ControlManos.ComandoVoz.GiraDerecha);
        BotonTocable.PonerVoz(btnVistaLimpia, ControlManos.ComandoVoz.VistaLimpia);
        BotonTocable.PonerVoz(btnMesa, ControlManos.ComandoVoz.Mesa);
        BotonTocable.PonerVoz(btnAyuda, ControlManos.ComandoVoz.Ayuda);
        BotonTocable.PonerVoz(btnColores, ControlManos.ComandoVoz.Paleta);
        BotonTocable.PonerNombre(btnBorrar, "Borrar todo");      // (por seguridad, no va por voz)
        BotonTocable.PonerNombre(btnCerrar, "Cerrar");
        if (btnColores != null)
            btnColores.PonerTexto("Paleta");                        // igual que su palabra de voz

        CrearIconoIman();
        CrearBotonesV52();
        grabador = FindFirstObjectByType<GrabadorProceso>();
        CrearFilaRec();
        if (contenido != null)
        {
            textoFps = TextoRec(contenido.transform, "", new Vector3(0f, 0.0795f, -0.001f), new Vector2(0.06f, 0.006f));
            textoFps.color = new Color(0.45f, 0.45f, 0.5f);
        }
        if (textoEstado != null)
            textoEstado.text = Idioma.T(textoAyuda);
        if (textoAviso != null)
            textoAviso.gameObject.SetActive(false);
        Refrescar();
        if (contenido != null)
            contenido.SetActive(false);
    }

    void OnDestroy()
    {
        if (imanEncendido != null) Destroy(imanEncendido);
        if (imanApagado != null) Destroy(imanApagado);
        if (dibujo != null)
        {
            dibujo.alCambiar -= Refrescar;
            dibujo.alMensaje -= Mensaje;
        }
    }

    void AbrirMisArchivos()
    {
        var nav = FindFirstObjectByType<NavegadorArchivos>();
        if (nav != null)
            nav.Abrir(control != null ? control.Cabeza : null);
        else if (dibujo != null)
            dibujo.Cargar();
    }

    static void Conectar(BotonTocable boton, UnityAction accion)
    {
        if (boton != null)
            boton.alTocar.AddListener(accion);
    }

    public void SiguienteFondo()   // v42: también la voz ("fondo")
    {
        if (escenario == null)
            return;
        escenario.SiguienteModo();
        Refrescar();
        // Al llegar a 360 se abre la ventanita con las miniaturas (al lado de este menú).
        if (escenario.modo >= 3)
        {
            SelectorFondos.Abrir(escenario, transform, control != null ? control.Cabeza : null);
            Mensaje(Idioma.T("Fondos 360: elige uno en la ventanita de al lado"));
        }
        else
        {
            SelectorFondos.Cerrar();
            Mensaje(escenario.MensajeFondo);
        }
    }

    // v36: ¿se está viendo el menú de la muñeca? (Mirar la muñeca es mirar hacia abajo: la mesa de trabajo espera.)
    public static float VistoHasta { get; private set; } = -10f;
    public static bool Visible => Time.time - VistoHasta < 0.8f;

    void Update()
    {
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        bool pose = control != null && control.PuedeVerPanel(visible, out pos, out rot);
        if (pose)
        {
            ultimaVezVisto = Time.time;
            VistoHasta = Time.time;
        }
        // Pequeña espera antes de esconderlo, para que no parpadee.
        bool ver = pose || (visible && Time.time - ultimaVezVisto < 0.2f);

        if (ver != visible)
        {
            visible = ver;
            if (contenido != null)
                contenido.SetActive(ver);
            if (ver)
            {
                transform.SetPositionAndRotation(pos, rot);
                Refrescar(); // por si cambió algo con un gesto (la vista limpia, por ejemplo)
            }
        }
        if (pose)
        {
            float a = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, pos, a);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, a);
        }

        if (ocultarMensajeEn > 0f && Time.time > ocultarMensajeEn)
        {
            ocultarMensajeEn = 0f;
            if (textoEstado != null)
                textoEstado.text = Idioma.T(textoAyuda);
        }
        ActualizarAviso();
        ActualizarRec();
    }

    void CrearFilaRec()
    {
        if (contenido == null || btnColores == null)
            return;
        filaRec = new GameObject("Grabando");
        filaRec.transform.SetParent(contenido.transform, false);
        var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "FondoGrabando";
        Destroy(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(filaRec.transform, false);
        fondo.transform.localPosition = new Vector3(0f, -0.198f, 0.004f);
        fondo.transform.localScale = new Vector3(0.105f, 0.028f, 1f);
        var rf = fondo.GetComponent<Renderer>();
        var fondoMenu = contenido.transform.Find("Fondo");
        if (fondoMenu != null && fondoMenu.GetComponent<Renderer>() != null)
            rf.sharedMaterial = fondoMenu.GetComponent<Renderer>().sharedMaterial;
        rf.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rf.receiveShadows = false;
        textoRec = TextoRec(filaRec.transform, "● 00:00", new Vector3(-0.027f, -0.198f, -0.001f), new Vector2(0.046f, 0.016f));
        textoRec.color = new Color(0.85f, 0.1f, 0.1f);
        btnPausaRec = BotonRec(filaRec.transform, "Pausa", new Vector3(0.017f, -0.198f, 0f), 0.03f);
        btnPausaRec.alTocar.AddListener(() => { if (grabador != null) grabador.AlternarPausa(); });
        btnPararRec = BotonRec(filaRec.transform, "Parar", new Vector3(0.042f, -0.198f, 0f), 0.022f);
        btnPararRec.alTocar.AddListener(() =>
        {
            if (grabador != null && grabador.Grabando)
                grabador.Alternar();
        });
        filaRec.SetActive(false);
    }

    BotonTocable BotonRec(Transform padre, string texto, Vector3 pos, float ancho)
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Boton_" + texto;
        cubo.transform.SetParent(padre, false);
        cubo.transform.localPosition = pos;
        cubo.transform.localScale = new Vector3(ancho, 0.02f, 0.008f);
        var r = cubo.GetComponent<Renderer>();
        r.sharedMaterial = btnColores.materialNormal;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        var b = cubo.AddComponent<BotonTocable>();
        b.materialNormal = btnColores.materialNormal;
        b.materialMarcado = btnColores.materialMarcado;
        b.etiqueta = TextoRec(padre, texto, pos + new Vector3(0f, 0f, -0.0046f), new Vector2(ancho * 0.9f, 0.014f));
        b.etiqueta.fontStyle = FontStyles.Bold;
        return b;
    }

    static TMP_Text TextoRec(Transform padre, string texto, Vector3 pos, Vector2 tam)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        Idioma.Poner(t, texto);
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = 0.12f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.black;
        t.rectTransform.sizeDelta = tam;
        return t;
    }

    void ActualizarRec()
    {
        if (textoFps != null && Rendimiento.Instancia != null && Time.time >= proximoRec)
        {
            var r = Rendimiento.Instancia;
            textoFps.text = Mathf.RoundToInt(r.Fps) + " fps" + (r.Nivel > 0 ? " · " + Idioma.T("modo rendimiento") + " " + r.Nivel : "");
        }
        if (filaRec == null || Time.time < proximoRec)
            return;
        proximoRec = Time.time + 0.25f;
        bool grabando = grabador != null && grabador.Grabando;
        if (filaRec.activeSelf != grabando)
            filaRec.SetActive(grabando);
        if (!grabando)
            return;
        textoRec.text = (grabador.Pausado ? "II " : "● ") + GrabadorProceso.Formato(grabador.TiempoGrabado);
        btnPausaRec.PonerTexto(grabador.Pausado ? "Seguir" : "Pausa");
        btnPausaRec.Marcar(grabador.Pausado);
    }

    // El aviso sale en el cartel de cómic de arriba al centro de tu vista (el mismo del nombre del gesto:
    // si los dos quieren verse, gana el más nuevo).
    void ActualizarAviso()
    {
        if (textoAviso == null)
            return;
        var cabeza = control != null ? control.Cabeza : null;
        bool ver = Time.time <= ocultarAvisoEn && cabeza != null && CartelArriba.PuedeVerAviso(avisoDesde);
        if (textoAviso.gameObject.activeSelf != ver)
            textoAviso.gameObject.SetActive(ver);
        if (!ver)
            return;
        Vector3 pos;
        Quaternion rot;
        CartelArriba.Pose(cabeza, out pos, out rot);
        textoAviso.transform.SetPositionAndRotation(pos, rot);
    }

    void Refrescar()
    {
        if (dibujo != null)
        {
            if (btnPlano != null)
            {
                btnPlano.PonerTexto(dibujo.plano ? "Plano (2D)" : "Libre (3D)");
                btnPlano.Marcar(dibujo.plano);
            }
        }
        if (btnFondo != null && escenario != null)
            btnFondo.PonerTexto("Fondo: " + escenario.NombreModo);
        if (btnCapas != null && dibujo != null)
        {
            btnCapas.PonerTexto("Capas: " + (dibujo.capaActual + 1));
            btnCapas.Marcar(CapasProfundidad.Abierta);
        }
        if (btnVistaLimpia != null)
            btnVistaLimpia.Marcar(VistaLimpia.Activa);
        if (btnIman != null && dibujo != null)
        {
            bool iman = dibujo.CapaActual.iman;
            btnIman.PonerTexto(iman ? "     Imán: Sí" : "     Imán: No");
            btnIman.Marcar(iman);
            if (iconoIman != null)
                foreach (var r in iconoIman)
                    if (r != null)
                        r.sharedMaterial = iman ? imanEncendido : imanApagado;
        }
    }

    // Ícono de imán (herradura): rojo = encendido, gris = apagado.
    void CrearIconoIman()
    {
        if (btnIman == null || btnIman.materialNormal == null)
            return;
        imanEncendido = new Material(btnIman.materialNormal);
        imanApagado = new Material(btnIman.materialNormal);
        Pintar(imanEncendido, new Color(0.9f, 0.15f, 0.15f));
        Pintar(imanApagado, new Color(0.55f, 0.55f, 0.58f));
        var icono = new GameObject("IconoIman").transform;
        icono.SetParent(btnIman.transform.parent, false);
        icono.localPosition = btnIman.transform.localPosition + new Vector3(-0.0145f, 0f, -0.006f);
        icono.localRotation = Quaternion.identity;
        // Dos patas y la curva de abajo (una U).
        var piezas = new[]
        {
            new[] { -0.003f, 0.001f, 0.0022f, 0.009f },
            new[] { 0.003f, 0.001f, 0.0022f, 0.009f },
            new[] { 0f, -0.0035f, 0.0082f, 0.0022f },
        };
        iconoIman = new Renderer[piezas.Length];
        for (int k = 0; k < piezas.Length; k++)
        {
            var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cubo.GetComponent<Collider>());
            cubo.transform.SetParent(icono, false);
            cubo.transform.localPosition = new Vector3(piezas[k][0], piezas[k][1], 0f);
            cubo.transform.localScale = new Vector3(piezas[k][2], piezas[k][3], 0.002f);
            var r = cubo.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            iconoIman[k] = r;
        }
    }

    static void Pintar(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    public void Mensaje(string texto)
    {
        if (textoEstado != null)
            textoEstado.text = texto;
        ocultarMensajeEn = Time.time + 2.5f;
        if (textoAviso != null)
        {
            textoAviso.text = texto;
            avisoDesde = Time.time;
            ocultarAvisoEn = Time.time + 1.6f;
            ActualizarAviso();
        }
    }

    // ---------- v52: Salir y los iconos de copiar / pegar ----------
    BotonTocable btnSalir;

    void CrearBotonesV52()
    {
        if (contenido == null || btnColores == null)
            return;
        // SALIR: guarda y cierra el dibujo (queda una hoja nueva). Con la hoja en blanco, cierra la app.
        btnSalir = BotonRec(contenido.transform, "Salir", new Vector3(0f, -0.139f, 0f), 0.06f);
        btnSalir.alTocar.AddListener(Salir);
        BotonTocable.PonerNombre(btnSalir, "Salir");
        // Copiar y pegar: dos iconos pequeñitos arriba, entre el ? y la X.
        var oscuro = new Material(btnColores.materialNormal);
        Pintar(oscuro, new Color(0.22f, 0.22f, 0.26f));
        var copiar = BotonIcono(new Vector3(-0.013f, 0.09f, 0f));
        copiar.alTocar.AddListener(() => { if (control != null) control.CopiarBoton(); });
        BotonTocable.PonerVoz(copiar, ControlManos.ComandoVoz.Copiar);
        // Copiar: dos hojitas, una encima de la otra.
        Marco(copiar.transform.parent, copiar.transform.localPosition + new Vector3(-0.0012f, 0.0012f, -0.0046f), 0.0062f, 0.0074f, oscuro);
        Marco(copiar.transform.parent, copiar.transform.localPosition + new Vector3(0.0014f, -0.0014f, -0.0050f), 0.0062f, 0.0074f, oscuro);
        var pegar = BotonIcono(new Vector3(0.013f, 0.09f, 0f));
        pegar.alTocar.AddListener(() => { if (control != null) control.PegarBoton(); });
        BotonTocable.PonerVoz(pegar, ControlManos.ComandoVoz.Pegar);
        // Pegar: una tablita con su pinza arriba.
        Marco(pegar.transform.parent, pegar.transform.localPosition + new Vector3(0f, -0.0006f, -0.0046f), 0.0074f, 0.0088f, oscuro);
        Barra(pegar.transform.parent, pegar.transform.localPosition + new Vector3(0f, 0.0042f, -0.0048f), new Vector2(0.0036f, 0.0016f), oscuro);
    }

    BotonTocable BotonIcono(Vector3 pos)
    {
        var b = BotonRec(contenido.transform, "", pos, 0.016f);
        b.transform.localScale = new Vector3(0.016f, 0.016f, 0.008f);
        return b;
    }

    // Un cuadrito hueco (4 barritas).
    static void Marco(Transform padre, Vector3 centro, float ancho, float alto, Material m)
    {
        const float g = 0.0009f;
        Barra(padre, centro + new Vector3(0f, alto * 0.5f, 0f), new Vector2(ancho, g), m);
        Barra(padre, centro - new Vector3(0f, alto * 0.5f, 0f), new Vector2(ancho, g), m);
        Barra(padre, centro + new Vector3(ancho * 0.5f, 0f, 0f), new Vector2(g, alto), m);
        Barra(padre, centro - new Vector3(ancho * 0.5f, 0f, 0f), new Vector2(g, alto), m);
    }

    static void Barra(Transform padre, Vector3 pos, Vector2 tam, Material m)
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cubo.GetComponent<Collider>());
        cubo.name = "Icono";
        cubo.transform.SetParent(padre, false);
        cubo.transform.localPosition = pos;
        cubo.transform.localScale = new Vector3(tam.x, tam.y, 0.0012f);
        var r = cubo.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    void Salir()
    {
        if (dibujo == null)
            return;
        if (dibujo.HayArchivoAbierto)
        {
            dibujo.ArchivoNuevo();
            Refrescar();
            return;
        }
        // Ya no hay nada abierto: cierra la app.
        Mensaje(Idioma.T("Hasta pronto"));
        Application.Quit();
    }
}

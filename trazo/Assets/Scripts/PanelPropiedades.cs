using System.Collections.Generic;
using TMPro;
using UnityEngine;

// PROPIEDADES (v59, v60): un panel con los ajustes de LA HERRAMIENTA QUE ESTÁS USANDO (solo esa), sin botones
// "ciegos": cada ajuste muestra TODAS sus opciones a la vista, la actual iluminada, y una frase corta de qué hace.
//  - Lápiz (capa de lápiz en 2D): cómo te sigue el lápiz (Pegado · Suave · Cuerda) y el papel.
//  - Tinta: textura de la línea, vida (temblor), suavidad, grosor vivo, uniones, imán y halo.
//  - Relleno (si lo último que abriste en la paleta fue RELLENO): textura, velocidad y degradado.
//  - Cámara (ahí no se dibuja): su nota y el FONDO.
// v60: la paleta ya no repite estos ajustes (solo colores). Se abre con su PASTILLITA (abajo a la derecha de tu vista,
// jálala hacia arriba), con el botón del reloj o con la voz ("propiedades"). Se mueve con un pellizco en su
// AGARRADERA (la barrita de arriba) o en cualquier parte vacía, como los demás paneles, y se queda donde lo dejes.
public class PanelPropiedades : MonoBehaviour
{
    static PanelPropiedades instancia;
    public static bool Abierto => instancia != null && instancia.gameObject.activeSelf;

    const float Ancho = 0.4f;
    const float AltoFila = 0.05f;

    ControlManos control;
    Dibujo dibujo;
    Material normal, marcado, matPanel;
    Transform fondo, contenido;
    TextMeshPro titulo;
    string firma = "";
    float proximo;
    bool colocado;
    Escenario esc;
    readonly HashSet<TextMeshPro> textosEncima = new HashSet<TextMeshPro>();

    // v62: SIEMPRE VISIBLE: el panel se dibuja ENCIMA de todo (aunque un dibujo u objeto esté más cerca de ti).
    // Sus piezas usan el shader TrazoVR/Encima (sin esconderse detrás de nada) y sus letras no miran la profundidad.
    static Shader shaderEncima;
    static Material Encima(Material origen, int cola)
    {
        if (shaderEncima == null)
            shaderEncima = Shader.Find("TrazoVR/Encima");
        if (shaderEncima == null || origen == null)
            return origen;
        var m = new Material(shaderEncima);
        Color c = origen.HasProperty("_BaseColor") ? origen.GetColor("_BaseColor") : origen.HasProperty("_Color") ? origen.GetColor("_Color") : Color.white;
        c.a = 1f;
        m.SetColor("_BaseColor", c);
        m.renderQueue = cola;
        return m;
    }

    void TextosEncima()
    {
        foreach (var t in GetComponentsInChildren<TextMeshPro>(true))
        {
            if (t == null || textosEncima.Contains(t))
                continue;
            var m = t.fontMaterial;
            if (m != null)
            {
                m.renderQueue = 4002;
                m.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.Always);
            }
            textosEncima.Add(t);
        }
    }

    // Una fila: sus botones, cuál está elegido y la frase de abajo.
    class Fila
    {
        public List<BotonTocable> botones = new List<BotonTocable>();
        public System.Func<int> actual;
        public System.Func<string> frase;
        public System.Func<bool> visible;
        public TextMeshPro texto, nombre;
        public List<GameObject> partes = new List<GameObject>();
    }
    readonly List<Fila> filas = new List<Fila>();

    public static void Alternar()
    {
        if (Abierto)
            Cerrar();
        else
            Abrir();
    }

    public static void Abrir()
    {
        if (instancia == null)
        {
            instancia = new GameObject("PanelPropiedades").AddComponent<PanelPropiedades>();
            if (!instancia.Armar())
            {
                Destroy(instancia.gameObject);
                instancia = null;
                return;
            }
        }
        bool yaAbierto = instancia.gameObject.activeSelf;
        instancia.gameObject.SetActive(true);
        // v65: cada vez que se abre sale A UN LADO (a tu derecha), para no tapar lo que dibujas. Abierto, se queda
        // donde lo dejes (y si quedó muy lejos de ti, se trae).
        var cab = instancia.control != null ? instancia.control.Cabeza : null;
        if (!yaAbierto || !instancia.colocado || (cab != null && Vector3.Distance(cab.position, instancia.transform.position) > 1.6f))
            instancia.Colocar();
        instancia.firma = "";
        instancia.Refrescar();
    }

    public static void Cerrar()
    {
        if (instancia != null)
            instancia.gameObject.SetActive(false);
    }

    bool Armar()
    {
        control = ControlManos.Instancia;
        dibujo = control != null ? control.dibujo : null;
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (dibujo == null || nav == null)
            return false;
        esc = FindFirstObjectByType<Escenario>(FindObjectsInactive.Include);
        normal = Encima(nav.materialBoton, 4001);
        marcado = Encima(nav.materialBotonMarcado, 4001);
        matPanel = Encima(nav.materialPanel, 4000);
        var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(f.GetComponent<Collider>());
        f.transform.SetParent(transform, false);
        var r = f.GetComponent<Renderer>();
        r.sharedMaterial = matPanel;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fondo = f.transform;
        titulo = PanelTutoriales.Texto(transform, "", new Vector3(-0.02f, 0f, -0.002f), new Vector2(Ancho - 0.08f, 0.02f), 0.18f, null);
        // v60: la AGARRADERA: una barrita arriba, sin botones cerca (pellízcala para llevar el panel).
        var asa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(asa.GetComponent<Collider>());
        asa.transform.SetParent(transform, false);
        asa.transform.localPosition = new Vector3(0f, 0.032f, 0f);
        asa.transform.localScale = new Vector3(0.12f, 0.008f, 0.004f);
        var ra = asa.GetComponent<Renderer>();
        ra.sharedMaterial = marcado;
        ra.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        titulo.fontStyle = FontStyles.Bold;
        var x = PanelTutoriales.Boton(transform, "X", new Vector3(Ancho * 0.5f - 0.02f, 0f, 0f), new Vector2(0.024f, 0.024f), normal, marcado);
        x.alTocar.AddListener(Cerrar);
        contenido = new GameObject("Filas").transform;
        contenido.SetParent(transform, false);
        var mover = gameObject.AddComponent<ArrastrePanel>();
        mover.centro = new Vector2(0f, -0.1f);
        mover.medio = new Vector2(Ancho * 0.5f, 0.12f);
        TextosEncima();
        return true;
    }

    // Arriba, delante de ti (como la mesa de trabajo).
    void Colocar()
    {
        var cab = control.Cabeza;
        if (cab == null)
            return;
        Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        // v67: a la DERECHA DE LO QUE ESTÁS DIBUJANDO (tus últimos movimientos), aunque mires a otro lado.
        Vector3 pos = ZonaTrabajo.PosicionAlLado(cab, true, Ancho * 0.5f);
        posAuto = pos;
        movidoAMano = false;
        deslizando = false;
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cab.position, Vector3.up));
        colocado = true;
    }

    // v62: como el panel va encima de todo, tapa la punta de tu dedo: un PUNTITO azul (también encima) te muestra
    // dónde está tu índice cuando está cerca del panel.
    Transform puntaDedo;

    void ActualizarPuntaDedo()
    {
        var der = control.Der;
        bool ver = false;
        if (der.valida && fondo != null)
        {
            Vector3 l = transform.InverseTransformPoint(der.indice);
            Vector3 c = fondo.localPosition, m = fondo.localScale * 0.5f;
            ver = Mathf.Abs(l.x - c.x) < m.x + 0.02f && Mathf.Abs(l.y - c.y) < m.y + 0.02f && l.z > -0.1f && l.z < 0.03f;
        }
        if (puntaDedo == null)
        {
            if (!ver)
                return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PuntaDedo";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Encima(marcado, 4003);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            puntaDedo = go.transform;
        }
        if (puntaDedo.gameObject.activeSelf != ver)
            puntaDedo.gameObject.SetActive(ver);
        if (!ver)
            return;
        puntaDedo.position = der.indice;
        float s = Mathf.Max(0.01f, transform.lossyScale.x);
        puntaDedo.localScale = Vector3.one * (0.009f / s);
    }

    // v67: si tu zona de trabajo se va lejos, el panel se desliza suave a su nuevo lado (salvo que lo hayas movido tú).
    Vector3 posAuto, objetivo;
    bool movidoAMano, deslizando;
    float proximaZona;

    void SeguirZona()
    {
        var cab = control.Cabeza;
        if (cab == null || !colocado)
            return;
        if (!movidoAMano && !deslizando && (transform.position - posAuto).sqrMagnitude > 0.0004f)
            movidoAMano = true;   // lo moviste tú: se queda donde lo pusiste
        if (movidoAMano)
            return;
        if (Time.time >= proximaZona)
        {
            proximaZona = Time.time + 1f;
            bool dedoLejos = !control.Der.valida || Vector3.Distance(control.Der.indice, transform.position) > 0.3f;
            Vector3 nueva = ZonaTrabajo.PosicionAlLado(cab, true, Ancho * 0.5f);
            if (dedoLejos && Vector3.Distance(nueva, posAuto) > 0.25f)
            {
                objetivo = nueva;
                deslizando = true;
            }
        }
        if (!deslizando)
            return;
        posAuto = Vector3.Lerp(posAuto, objetivo, 1f - Mathf.Exp(-3f * Time.deltaTime));
        if ((posAuto - objetivo).sqrMagnitude < 0.0001f)
        {
            posAuto = objetivo;
            deslizando = false;
        }
        transform.SetPositionAndRotation(posAuto, Quaternion.LookRotation(posAuto - cab.position, Vector3.up));
    }

    void Update()
    {
        if (control == null || dibujo == null)
            return;
        ActualizarPuntaDedo();
        SeguirZona();
        if (Time.time < proximo)
            return;
        proximo = Time.time + 0.25f;
        Refrescar();
    }

    // ---------- Qué se muestra ----------

    // v62: + "+semi" (con el semipassthrough puesto sale su subopción) y "+plano" (capa en 2D: "Ver en la mesa").
    string FirmaAhora()
    {
        bool plano = dibujo.plano && !dibujo.EsCamara(dibujo.capaActual);
        return FirmaBase() + (esc != null && esc.Semi ? "+semi" : "") + (plano ? "+plano" : "");
    }

    string FirmaBase()
    {
        if (dibujo.EsCamara(dibujo.capaActual))
            return "camara";
        if (dibujo.UsaHoja)
            return "lapiz";
        if (control.ContextoRelleno)
            return control.TexturaElegida == Trazo.TexturaPlastilina ? "relleno+plast" : "relleno";
        return "tinta" + (dibujo.temblor != null && dibujo.temblor.Textura == 1 ? "+lapiz" : "") + (dibujo.HaloDisponible ? "+halo" : "")
               + (dibujo.ColorNuevo.a < 0.01f ? "+inv" : "");
    }

    void Refrescar()
    {
        string f = FirmaAhora();
        if (f != firma)
        {
            firma = f;
            Construir();
        }
        foreach (var fila in filas)
        {
            bool ver = fila.visible == null || fila.visible();
            foreach (var p in fila.partes)
                if (p != null && p.activeSelf != ver)
                    p.SetActive(ver);
            if (!ver)
                continue;
            int a = fila.actual != null ? fila.actual() : -1;
            for (int i = 0; i < fila.botones.Count; i++)
                fila.botones[i].Marcar(i == a);
            if (fila.texto != null && fila.frase != null)
                fila.texto.text = Idioma.T(fila.frase());
        }
    }

    void Construir()
    {
        foreach (Transform h in contenido)
            Destroy(h.gameObject);
        filas.Clear();
        float y = -0.035f;
        if (firma.StartsWith("lapiz"))
        {
            titulo.text = Idioma.T("Propiedades · Lápiz");
            y = Opciones(y, "Sigue al dedo", ControlManos.NombresModoLapiz, () => control.ModoLapiz, control.ElegirModoLapiz,
                         () => ControlManos.ExplicacionModoLapiz[control.ModoLapiz]);
            y = Opciones(y, "Papel", new[] { "Blanco", "50 %", "Transparente" }, () => Mathf.Clamp(dibujo.CapaActual.papel, 0, 2),
                         dibujo.PonerPapel, () => "Lo que hay detrás del lápiz (cada capa recuerda el suyo)");
        }
        else if (firma.StartsWith("camara"))
        {
            titulo.text = Idioma.T("Propiedades · Cámara y fondo");
            y = Nota(y, "Capa de cámara: pellízcala para moverla. El zoom y \"Sugerir\" están en su panelito.");
            var muneca = FindFirstObjectByType<PanelMuneca>(FindObjectsInactive.Include);
            var escenario = FindFirstObjectByType<Escenario>(FindObjectsInactive.Include);
            if (muneca != null && escenario != null)
            {
                y = Opciones(y, "Fondo", new[] { "Siguiente" }, () => -1, i => muneca.SiguienteFondo(), () => escenario.MensajeFondo);
                y = Opciones(y, "Girar", new[] { "← Girar", "Girar →" }, () => -1, i => control.GirarFondoBoton(i == 0 ? -1 : 1),
                             () => "Gira el fondo 360", () => escenario.modo >= 3);
            }
        }
        else if (firma.StartsWith("relleno"))
        {
            titulo.text = Idioma.T("Propiedades · Relleno");
            // v63: cómo rellena (Normal por defecto, Cubeta para líneas abiertas, o rellenar al dibujar).
            y = Opciones(y, "Cómo rellena", ControlManos.NombresModoRelleno, () => control.ModoRelleno, control.ElegirModoRelleno,
                         () => ControlManos.ExplicacionModoRelleno[control.ModoRelleno]);
            y = Opciones(y, "Textura", Trazo.NombresTextura, () => control.TexturaElegida, control.ElegirTexturaRelleno,
                         () => "El relleno de las próximas figuras (y de las seleccionadas)");
            y = Opciones(y, "Velocidad", Trazo.NombresVelocidadTextura, () => control.VelocidadTexturaElegida, control.ElegirVelocidadRelleno,
                         () => "Qué tan rápido se mueve la textura", () => control.TexturaElegida > 0);
            y = Opciones(y, "Degradado", ControlManos.NombresDegradado, () => control.ModoDegradado, i => control.ModoDegradado = i,
                         () => "Del color de antes al de ahora");
            if (firma.Contains("+plast"))
                y = Accion(y, "Palancas de plastilina", PanelPlastilina.Abrir);
        }
        else
        {
            titulo.text = Idioma.T("Propiedades · Tinta");
            // v61: con la tinta invisible las líneas nuevas no se ven: se avisa aquí (para que no parezca un error).
            if (firma.Contains("+inv"))
                y = Nota(y, "Tinta invisible: tus líneas nuevas no se ven (el relleno sí). Toca un color de la paleta para volver.");
            var tb = dibujo.temblor;
            if (tb != null)
            {
                y = Opciones(y, "Textura", Temblor.NombresTextura, () => tb.Textura, tb.PonerTextura,
                             () => tb.Textura == 1 ? "Lápiz: sus palancas se abren solas" : "Cómo se ve la línea (las nuevas y las seleccionadas)");
                y = Opciones(y, "Vida", Temblor.NombresVida, () => tb.Vida, tb.PonerVida, () => "Cuánto tiembla la línea, como dibujo animado a mano");
                y = Opciones(y, "Suavidad", Dibujo.NombresSuavidad, () => tb.Suavidad, tb.PonerSuavidad,
                             () => "Qué tan ondulado es el temblor", () => tb.Vida > 0);
                y = Opciones(y, "Grosor vivo", new[] { "Sí", "No" }, () => tb.GrosorVivo ? 0 : 1,
                             i => { if ((i == 0) != tb.GrosorVivo) tb.AlternarGrosor(); }, () => "El grosor de la línea respira un poquito");
            }
            y = Opciones(y, "Líneas invisibles", new[] { "Ocultas", "Previsualizar" }, () => Trazo.PrevisualizarInvisibles ? 1 : 0,
                         i => dibujo.PonerPrevisualizarInvisibles(i == 1),
                         () => Trazo.PrevisualizarInvisibles ? "Una raya intermitente muestra dónde están (no sale en fotos)" : "No se ven (solo su relleno)");
            y = Opciones(y, "Al borrar un nodo", new[] { "Unir", "Cortar" }, () => Trazo.BorrarCortando ? 1 : 0,
                         i => Trazo.BorrarCortando = i == 1,
                         () => Trazo.BorrarCortando ? "La línea queda partida donde borras" : "Se unen los nodos de al lado: la línea sigue");
            y = Opciones(y, "Uniones", new[] { "Rectas", "Suaves" }, () => Trazo.UnionesSuaves ? 1 : 0,
                         i => { if ((i == 1) != Trazo.UnionesSuaves) Trazo.AlternarUniones(); }, () => "Cómo se juntan dos líneas que se unen");
            y = Opciones(y, "Imán", new[] { "Sí", "No" }, () => dibujo.CapaActual.iman ? 0 : 1,
                         i => { if ((i == 0) != dibujo.CapaActual.iman) dibujo.AlternarIman(); }, () => "Las puntas cercanas se unen solas");
            if (firma.Contains("+halo"))
                y = Opciones(y, "Halo", new[] { "Sí", "No" }, () => dibujo.HaloEncendido ? 0 : 1,
                             i => { if ((i == 0) != dibujo.HaloEncendido) dibujo.AlternarHalo(); }, () => "Un brillo detrás de las líneas (con fondo 360 o realidad)");
            if (firma.Contains("+lapiz"))
                y = Accion(y, "Ajustes del lápiz", PanelLapiz.Abrir);
        }
        // v62: en 2D, "Ver en la mesa" (tu hoja sobre el disco de la mesa de animación, sin salir del archivo).
        if (firma.Contains("+plano"))
            y = Accion(y, "Ver en la mesa", MesaEntrada.VerEnLaMesa);
        // v62: la subopción del semipassthrough.
        if (firma.Contains("+semi") && esc != null)
            y = Opciones(y, "Semipassthrough", new[] { "Ventana fija", "Sigue tu mirada" }, () => esc.SemiMirada ? 1 : 0,
                         i => esc.PonerSemiMirada(i == 1), () => "Dónde queda la ventana a tu cuarto (blanco a los lados)");
        TextosEncima();
        float alto = -y + 0.01f;
        fondo.localPosition = new Vector3(0f, 0.02f - alto * 0.5f, 0.005f);
        fondo.localScale = new Vector3(Ancho, alto + 0.03f, 0.004f);
        var mover = GetComponent<ArrastrePanel>();
        if (mover != null)
        {
            mover.centro = new Vector2(0f, fondo.localPosition.y);
            mover.medio = new Vector2(Ancho * 0.5f, (alto + 0.03f) * 0.5f);
        }
    }

    float Seccion(float y, string nombre)
    {
        var t = PanelTutoriales.Texto(contenido, "", new Vector3(-Ancho * 0.5f + 0.06f, y, -0.002f), new Vector2(0.1f, 0.014f), 0.11f, null);
        Idioma.Poner(t, nombre);
        t.fontStyle = FontStyles.Bold | FontStyles.Underline;
        t.alignment = TextAlignmentOptions.Left;
        t.color = new Color(0.15f, 0.3f, 0.7f);
        return y - 0.022f;
    }

    float Nota(float y, string texto)
    {
        var t = PanelTutoriales.Texto(contenido, "", new Vector3(0f, y - 0.01f, -0.002f), new Vector2(Ancho - 0.04f, 0.03f), 0.09f, null);
        Idioma.Poner(t, texto);
        return y - 0.045f;
    }

    float Accion(float y, string texto, UnityEngine.Events.UnityAction accion)
    {
        var b = PanelTutoriales.Boton(contenido, "", new Vector3(0f, y, 0f), new Vector2(0.16f, 0.024f), marcado, normal);
        Idioma.Poner(b.etiqueta, texto);
        b.etiqueta.color = Color.white;
        b.textoNormal = Color.white;
        b.alTocar.AddListener(accion);
        return y - 0.035f;
    }

    // Una fila: su nombre a la izquierda, TODAS sus opciones (la actual iluminada) y una frase debajo.
    float Opciones(float y, string nombre, string[] opciones, System.Func<int> actual, System.Action<int> elegir,
                   System.Func<string> frase, System.Func<bool> visible = null)
    {
        var fila = new Fila { actual = actual, frase = frase, visible = visible };
        fila.nombre = PanelTutoriales.Texto(contenido, "", new Vector3(-Ancho * 0.5f + 0.045f, y, -0.002f), new Vector2(0.075f, 0.016f), 0.1f, null);
        Idioma.Poner(fila.nombre, nombre);
        fila.nombre.alignment = TextAlignmentOptions.Left;
        fila.partes.Add(fila.nombre.gameObject);
        float x0 = -Ancho * 0.5f + 0.09f, ancho = Ancho - 0.105f;
        float w = ancho / opciones.Length;
        for (int i = 0; i < opciones.Length; i++)
        {
            int k = i;
            var b = PanelTutoriales.Boton(contenido, "", new Vector3(x0 + w * (i + 0.5f), y, 0f), new Vector2(w - 0.004f, 0.022f), normal, marcado);
            Idioma.Poner(b.etiqueta, opciones[i]);
            b.alTocar.AddListener(() =>
            {
                elegir(k);
                if (control != null)
                    control.SonidoBurbuja(b.transform.position);
                proximo = 0f;
            });
            fila.botones.Add(b);
            fila.partes.Add(b.gameObject);
            fila.partes.Add(b.etiqueta.gameObject);
        }
        fila.texto = PanelTutoriales.Texto(contenido, "", new Vector3(x0 + ancho * 0.5f, y - 0.018f, -0.002f), new Vector2(ancho, 0.01f), 0.07f, null);
        fila.texto.color = new Color(0.4f, 0.4f, 0.46f);
        fila.partes.Add(fila.texto.gameObject);
        filas.Add(fila);
        return y - AltoFila;
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}

// v60: LA PASTILLITA de Propiedades (como las de Capas y Línea de tiempo): abajo, a la derecha de tu vista.
// Jálala hacia arriba y sale el panel. No se ve en la vista limpia, ni en la mesa de animación, ni con el panel abierto.
public class PastillaPropiedades : MonoBehaviour
{
    const float Lado = 0.1f;    // metros a la derecha del centro de la orilla de abajo (v62: más al centro)
    const float Sube = 0.03f;   // v62: un poco más arriba que las otras pastillitas
    Persiana persiana;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        new GameObject("PastillaPropiedades").AddComponent<PastillaPropiedades>();
    }

    void Update()
    {
        if (persiana != null)
            return;
        var control = ControlManos.Instancia;
        if (control == null || control.Cabeza == null)
            return;
        persiana = new GameObject("PersianaPropiedades").AddComponent<Persiana>();
        persiana.seEsconde = true;
        persiana.sentido = 1;   // se jala hacia arriba
        persiana.pose = (Transform cab, out Vector3 pos, out Quaternion rot) =>
        {
            PanelesFlexibles.PoseOrilla(cab, false, Lado, out pos, out rot);
            pos += rot * Vector3.up * Sube;
        };
        persiana.visible = () => !PanelPropiedades.Abierto && !Tutorial.EnCurso && !MesaEntrada.Activa && !ExportadorVideo.Exportando;
        persiana.puedeJalar = () => !control.TrazandoAhora;
        persiana.texto = () => Idioma.T("Propiedades");
        persiana.comandoVoz = ControlManos.ComandoVoz.Propiedades;
        persiana.alAbrir = PanelPropiedades.Abrir;
    }
}

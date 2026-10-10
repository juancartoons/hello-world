using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Paleta de colores (parte de ControlManos):
//  - Palma IZQUIERDA abierta mirando hacia ti (dedos separados) = aparece una paleta de pintor con 12 colores.
//  - Toca un color con el índice derecho: las líneas nuevas salen de ese color (la puntita del dedo lo muestra).
//  - Si hay líneas elegidas (azules), se pintan de ese color (se puede deshacer).
//  - Cuentagotas: con la paleta abierta, toca una línea con el índice derecho = tomas su color.
//  - En el centro, una línea que tiembla: tócala para cambiar el temblor; debajo, sus opciones
//    (ver ControlManosTemblor.cs).
//  - Arriba a la derecha (solo con un fondo 360 o la realidad): el botón HALO, un brillo suave detrás de
//    las líneas para que no se pierdan en el fondo (amarillo = encendido).
//  - Arriba a la izquierda: el RELLENO VIVO (texturas que se mueven, estilo Quill), su velocidad y su tamaño.
//  - A la derecha: el IMÁN de la capa activa (amarillo = encendido: las puntas se pegan a otras líneas).
//  - Debajo de la paleta: un cartelito con los datos de la línea (grosor, color, temblor, relleno...).
//    Lo último que cambiaste sale resaltado.
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

    Transform paleta;
    readonly List<Transform> botonesColor = new List<Transform>();
    readonly List<Material> materialesPaleta = new List<Material>();
    Material materialCentroPaleta, materialCursorColor;
    bool paletaAbierta, dedoEnColor, dedoEnLinea;
    float paletaDesde = -1f, paletaFueraDesde = -1f;
    int colorElegido;

    public bool PaletaAbierta => paletaAbierta;

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
    // Así abierta se queda junto al menú; se cierra al elegir un color, con el botón otra vez o sola en 25 s.
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

        // La paleta flota sobre tu palma, mirando hacia ti.
        if (!paletaFija && palmaIzq.valida && poseValida)
        {
            Vector3 pos = palmaIzq.centro + palmaIzq.normal * 0.05f + Vector3.up * 0.01f;
            Quaternion rot = Quaternion.LookRotation(pos - Cabeza.position, Vector3.up);
            float a = 1f - Mathf.Exp(-18f * Time.deltaTime);
            paleta.position = Vector3.Lerp(paleta.position, pos, a);
            paleta.rotation = Quaternion.Slerp(paleta.rotation, rot, a);
        }
        for (int i = 0; i < botonesColor.Count; i++)
            botonesColor[i].localScale = Vector3.one * TamColor * (i == colorElegido ? 1.3f : 1f);
        // La línea del centro lleva el color elegido (con tinta invisible, gris clarito).
        if (materialCentroPaleta != null && materialCentroPaleta.HasProperty("_BaseColor"))
            materialCentroPaleta.SetColor("_BaseColor", dibujo.ColorNuevo.a > 0.01f ? dibujo.ColorNuevo : new Color(0.72f, 0.74f, 0.8f));
        ActualizarDatosPaleta();

        if (!Der.valida)
        {
            ActualizarTemblorPaleta(Vector3.zero, false);
            ActualizarRellenoPaleta(Vector3.zero, false);
            ActualizarHaloPaleta(Vector3.zero, false);
            ActualizarTexturaPaleta(Vector3.zero, false);
            ActualizarImanPaleta(Vector3.zero, false);
            dedoEnRelleno = false;
            dedoEnColor = false;
            dedoEnLinea = false;
            return;
        }
        Vector3 punta = Der.indice;
        // ¿Tocó la línea que tiembla o una de sus opciones?
        if (ActualizarTemblorPaleta(punta, true))
        {
            dedoEnColor = false;
            dedoEnLinea = false;
            return;
        }
        // ¿Tocó la tinta invisible o la cubeta?
        if (ActualizarRellenoPaleta(punta, true))
        {
            dedoEnColor = false;
            dedoEnLinea = false;
            return;
        }
        // ¿Tocó el botón del halo?
        if (ActualizarHaloPaleta(punta, true))
        {
            dedoEnColor = false;
            dedoEnLinea = false;
            return;
        }
        // ¿Tocó el imán?
        if (ActualizarImanPaleta(punta, true))
        {
            dedoEnColor = false;
            dedoEnLinea = false;
            return;
        }
        // ¿Tocó el relleno vivo o su velocidad?
        if (ActualizarTexturaPaleta(punta, true))
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
        if (tocado >= 0)
        {
            if (!dedoEnColor)
                ElegirColor(ColoresPaleta[tocado], tocado, botonesColor[tocado].position);
            dedoEnColor = true;
            return;
        }
        dedoEnColor = false;
        // Cuentagotas: tocar una línea (lejos de la paleta) = tomar su color.
        if (Vector3.Distance(punta, paleta.position) < 0.1f)
        {
            dedoEnLinea = false;
            dedoEnRelleno = false;
            return;
        }
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
                dibujo.ColorNuevo = cerca.color;
                colorElegido = IndiceColor(cerca.color);
                Burbuja(punta, 1.3f);
                MostrarEtiqueta("Cuentagotas: color tomado");
            }
            dedoEnLinea = true;
            dedoEnRelleno = false;
        }
        else
        {
            dedoEnLinea = false;
            // No hay línea cerca: ¿está DENTRO de una forma? = rellenarla.
            RellenarAlTocar(local, punta);
        }
    }

    // i = cuál de los 12 colores (-1 = tinta invisible).
    void ElegirColor(Color c, int i, Vector3 donde)
    {
        colorElegido = i;
        dibujo.ColorNuevo = c;
        Burbuja(donde, 1.1f);
        // Si hay líneas elegidas, se pintan de ese color.
        var elegidas = dibujo.Seleccionadas();
        if (elegidas.Count > 0)
        {
            dibujo.GuardarParaDeshacer();
            dibujo.Seleccionar(null); // sin el azul de "elegida", así se ve el color nuevo
            foreach (var t in elegidas)
                dibujo.PonerColor(t, c);
            Trazo.huboCambio = false;
            MostrarEtiqueta(elegidas.Count > 1 ? "Líneas pintadas" : "Línea pintada");
        }
        else
        {
            MostrarEtiqueta(c.a < 0.01f ? "Tinta invisible" : "Color");
        }
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

    void AbrirPaleta()
    {
        if (paleta == null)
            ArmarPaleta();
        if (paleta == null)
            return;
        paletaAbierta = true;
        dedoEnColor = true;  // no elige nada hasta que el dedo salga y vuelva a entrar
        dedoEnLinea = true;
        dedoEnTemblor = true;
        dedoEnBotonRelleno = true;
        dedoEnRelleno = true;
        dedoEnHalo = true;
        dedoEnTextura = true;
        dedoEnIman = true;
        claveMuestra = int.MinValue;
        colorElegido = IndiceColor(dibujo.ColorNuevo);
        if (palmaIzq.valida && Cabeza != null)
        {
            Vector3 pos = palmaIzq.centro + palmaIzq.normal * 0.05f + Vector3.up * 0.01f;
            paleta.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - Cabeza.position, Vector3.up));
        }
        paleta.gameObject.SetActive(true);
        Burbuja(paleta.position, 0.9f);
    }

    void CerrarPaleta()
    {
        paletaFija = false;
        paletaAbierta = false;
        paletaDesde = -1f;
        paletaFueraDesde = -1f;
        if (paleta != null && paleta.gameObject.activeSelf)
            paleta.gameObject.SetActive(false);
    }

    // La paleta: un disco blanco con borde negro y 12 circulitos de color alrededor; en el centro, el color actual.
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
        // En el centro: una línea que tiembla (del color elegido) y debajo las opciones del temblor.
        materialCentroPaleta = ColorMaterial(dibujo != null ? dibujo.ColorNuevo : Color.black);
        ArmarTemblorPaleta(negro);
        ArmarRellenoPaleta(negro);
        ArmarHaloPaleta(negro);
        ArmarTexturaPaleta(negro);
        ArmarImanPaleta(negro);
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

    // ---------- Datos de la línea (cartelito debajo de la paleta) ----------
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
        int indice = IndiceColor(color);
        datosAhora[0] = Mathf.RoundToInt(mm) + " mm";
        datosAhora[1] = indice < 0 ? "Tinta invisible" : NombresColorPaleta[Mathf.Clamp(indice, 0, NombresColorPaleta.Length - 1)];
        datosAhora[2] = capa != null ? capa.nombre : "";
        datosAhora[3] = tb != null ? "Temblor: " + tb.NombreNivel : "";
        datosAhora[4] = tb != null ? "Hebras: " + tb.Hebras + (tb.Hebras > 1 && capa != null ? " (" + Mathf.RoundToInt(capa.grosorHebra * 100f) + " %)" : "") : "";
        datosAhora[5] = tb != null ? (tb.GrosorVivo ? "Grosor vivo: Sí" : "Grosor vivo: No") : "";
        datosAhora[6] = tb != null ? (tb.Ciclo3 ? "Ciclo de 3" : "Libre") : "";
        datosAhora[7] = tb != null ? "Suavidad: " + tb.NombreSuavidad : "";
        datosAhora[8] = tb != null ? Mathf.RoundToInt(tb.CambiosPorSegundo) + " cambios/s" : "";
        int tex = elegida != null && elegida.relleno ? elegida.texturaRelleno : TexturaElegida;
        int vel = elegida != null && elegida.relleno ? elegida.velocidadTextura : VelocidadTexturaElegida;
        int tam = elegida != null && elegida.relleno ? elegida.tamanoTextura : TamanoTexturaElegido;
        datosAhora[9] = "Relleno: " + Trazo.NombresTextura[Mathf.Clamp(tex, 0, Trazo.NombresTextura.Length - 1)]
                        + (tex > 0 ? " · " + Trazo.NombresVelocidadTextura[Mathf.Clamp(vel, 0, Trazo.VelocidadesTextura.Length - 1)]
                                   + " · " + Trazo.NombresTamanoTextura[Mathf.Clamp(tam, 0, Trazo.TamanosTextura.Length - 1)] : "");
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
        Dato(sb, 9, ""); Dato(sb, 10, " · "); Dato(sb, 11, " · ");
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

    // ---------- Imán (de la capa activa): las puntas de las líneas se pegan a otras ----------
    Transform botonIman;
    Material fondoIman;
    bool dedoEnIman;

    void ArmarImanPaleta(Material negro)
    {
        botonIman = new GameObject("Iman").transform;
        botonIman.SetParent(paleta, false);
        botonIman.localPosition = new Vector3(0.0719f, 0.0127f, -0.001f);
        botonIman.localScale = Vector3.one * 0.02f;
        fondoIman = ColorMaterial(Color.white);
        DiscoPaleta(botonIman, negro, 1.12f, new Vector3(0f, 0f, 0.05f));
        DiscoPaleta(botonIman, fondoIman, 1f, new Vector3(0f, 0f, 0.025f));
        // Una herradura roja (una U) con las puntas grises.
        var u = new List<Vector2[]>();
        LineaIcono(u, new[] { new Vector2(-0.17f, 0.12f), new Vector2(-0.17f, -0.06f), new Vector2(-0.1f, -0.2f),
                              new Vector2(0f, -0.24f), new Vector2(0.1f, -0.2f), new Vector2(0.17f, -0.06f), new Vector2(0.17f, 0.12f) }, 0.11f);
        IconoPaleta(botonIman, u, ColorMaterial(new Color(0.88f, 0.15f, 0.15f)));
        var puntas = new List<Vector2[]>();
        BarraIcono(puntas, new Vector2(-0.17f, 0.13f), new Vector2(-0.17f, 0.25f), 0.11f);
        BarraIcono(puntas, new Vector2(0.17f, 0.13f), new Vector2(0.17f, 0.25f), 0.11f);
        IconoPaleta(botonIman, puntas, ColorMaterial(new Color(0.75f, 0.76f, 0.8f)));
    }

    // Cada cuadro con la paleta abierta. Devuelve true si el dedo está en el botón.
    bool ActualizarImanPaleta(Vector3 punta, bool dedoValido)
    {
        if (botonIman == null || dibujo == null)
            return false;
        PonerColorMaterial(fondoIman, dibujo.CapaActual.iman ? AmarilloOpcion : Color.white);
        if (!dedoValido || Vector3.Distance(punta, botonIman.position) > 0.012f)
        {
            dedoEnIman = false;
            return false;
        }
        if (!dedoEnIman)
        {
            dibujo.AlternarIman();
            Burbuja(botonIman.position, 1.2f);
            if (paletaFija)
                paletaFijaHasta = Mathf.Max(paletaFijaHasta, Time.time + 12f);
        }
        dedoEnIman = true;
        return true;
    }

    // ---------- Botón Halo (solo con un fondo 360 o la realidad) ----------
    Transform botonHalo;
    Material fondoHalo;
    bool dedoEnHalo;

    // Un botoncito con una raya negra y su brillo gris alrededor.
    void ArmarHaloPaleta(Material negro)
    {
        botonHalo = new GameObject("Halo").transform;
        botonHalo.SetParent(paleta, false);
        botonHalo.localPosition = new Vector3(0.056f, 0.047f, -0.001f);
        botonHalo.localScale = Vector3.one * 0.021f;
        fondoHalo = ColorMaterial(Color.white);
        DiscoPaleta(botonHalo, negro, 1.12f, new Vector3(0f, 0f, 0.05f));
        DiscoPaleta(botonHalo, fondoHalo, 1f, new Vector3(0f, 0f, 0.025f));
        var brillo = new List<Vector2[]>();
        BarraIcono(brillo, new Vector2(-0.2f, -0.2f), new Vector2(0.2f, 0.2f), 0.3f);
        IconoPaleta(botonHalo, brillo, ColorMaterial(new Color(0.68f, 0.7f, 0.75f)));
        // El brillo queda detrás de la raya.
        botonHalo.GetChild(botonHalo.childCount - 1).localPosition = new Vector3(0f, 0f, 0.012f);
        var raya = new List<Vector2[]>();
        BarraIcono(raya, new Vector2(-0.2f, -0.2f), new Vector2(0.2f, 0.2f), 0.08f);
        IconoPaleta(botonHalo, raya, negro);
    }

    // Cada cuadro con la paleta abierta. Devuelve true si el dedo está en el botón.
    bool ActualizarHaloPaleta(Vector3 punta, bool dedoValido)
    {
        if (botonHalo == null)
            return false;
        bool ver = dibujo.HaloDisponible;
        if (botonHalo.gameObject.activeSelf != ver)
            botonHalo.gameObject.SetActive(ver);
        if (!ver)
        {
            dedoEnHalo = false;
            return false;
        }
        PonerColorMaterial(fondoHalo, dibujo.HaloEncendido ? AmarilloOpcion : Color.white);
        if (!dedoValido || Vector3.Distance(punta, botonHalo.position) > 0.012f)
        {
            dedoEnHalo = false;
            return false;
        }
        if (!dedoEnHalo)
        {
            dibujo.AlternarHalo();
            Burbuja(botonHalo.position, 1.1f);
            if (paletaFija)
                paletaFijaHasta = Mathf.Max(paletaFijaHasta, Time.time + 12f);
        }
        dedoEnHalo = true;
        return true;
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

    // La puntita del índice derecho lleva el color elegido (como una gotita de pintura).
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
            materialCursorColor.SetColor("_BaseColor", dibujo.ColorNuevo);
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

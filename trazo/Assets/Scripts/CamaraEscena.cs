using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

// Una clave de la cámara: dónde está, hacia dónde mira y cuánto abarca (en coordenadas del dibujo).
[System.Serializable]
public class ClaveCamara
{
    public int fotograma;
    public Vector3 posicion;
    public Quaternion rotacion = Quaternion.identity;
    public float campo = 40f;
}

// La cámara del dibujo (se guarda con el proyecto). Sin "hay" = encuadre automático de siempre.
[System.Serializable]
public class DatosCamara
{
    public bool hay;
    public Vector3 posicion;
    public Quaternion rotacion = Quaternion.identity;
    public float campo = 40f;          // grados (de arriba a abajo del cuadro)
    public float distancia = 0.5f;     // hasta lo que mira (para girar alrededor de eso)
    public List<ClaveCamara> claves = new List<ClaveCamara>();
}

// LA CÁMARA (v43): la "capa" de la cámara. v44: es una CAPA de verdad (adelante de todas, con ícono de cámara);
// elígela en las capas (o di "cámara") y aparece. "+ Cámara" en su tarjeta = otra cámara (otro ángulo u otro
// recorrido); la que elegiste es la que se usa al exportar.
//  - Se ve una camarita con su CUADRO (16:9 horizontal o 9:16 vertical, como el formato de Exportar) y, encima, un
//    MONITOR que muestra en vivo lo que ve, con sus botones: Clave, Quita, Sugiere, + (acerca), − (aleja) y Quitar.
//  - PELLIZCA la camarita con la DERECHA y muévela: gira alrededor de lo que está mirando (siempre lo mira).
//    Con la IZQUIERDA: se mueve sin cambiar hacia dónde mira (para correrla de lado, subirla o acercarla).
//  - v48: botón "Libre" (en el monitor): con él prendido, la DERECHA la sostiene como una cámara de verdad: gira con
//    tu mano y apunta a donde quieras. Apagado (lo normal), siempre mira a lo que dibujaste.
//  - v48: al entrar a la capa de cámara, apareces justo DETRÁS de ella (mirando hacia donde mira).
//  - En 2D, al ponerla, apunta de frente al plano de tu capa.
//  - SUGIERE: la app prueba muchos ángulos (de frente, de tres cuartos, desde arriba, desde abajo) y elige los 3 en los
//    que el dibujo se ve mejor (más grande, menos líneas encimadas, bien acomodado en el cuadro y con aire arriba).
//    Salen 3 miniaturas: toca una para usarla.
//  - ANIMADA: "Clave" guarda la cámara en el fotograma actual (como las claves del dibujo); entre dos claves la
//    cámara viaja suave. Con claves, mover la cámara en un fotograma guarda ahí su clave sola.
//  - Exportar (foto y video) sale desde esta cámara. "Quitar" = vuelve el encuadre automático.
// No sale en fotos ni videos (solo el dibujo).
public class CamaraEscena : MonoBehaviour
{
    const float RadioAgarre = 0.06f;
    const float DistanciaCuadro = 0.12f;
    const float CampoMin = 12f, CampoMax = 90f;
    const int CadaCuantos = 3;           // el monitor se actualiza 1 de cada 3 cuadros (rendimiento)

    static CamaraEscena instancia;
    static readonly DatosCamara vacia = new DatosCamara();
    static Dibujo dib;

    // v44: la cámara vive en su CAPA (ver Dibujo: capas de cámara). Se usa la capa de cámara elegida.
    static DatosCamara datos
    {
        get
        {
            var d = ElDibujo;
            var c = d != null ? d.DatosCamaraActiva : null;
            if (c == null)
                return vacia;
            if (c.claves == null)
                c.claves = new List<ClaveCamara>();
            return c;
        }
    }

    public static bool Hay => datos.hay;
    // v44: se ve cuando estás en una capa de cámara.
    public static bool Visible
    {
        get
        {
            var d = ElDibujo;
            return d != null && d.capas.Count > 0 && d.EsCamara(d.capaActual);
        }
    }
    public static List<ClaveCamara> Claves => datos.claves;

    static Dibujo ElDibujo
    {
        get
        {
            if (dib == null)
                dib = FindFirstObjectByType<Dibujo>();
            return dib;
        }
    }

    static int FotogramaActual
    {
        get
        {
            var d = ElDibujo;
            return d != null && d.animacion != null ? d.animacion.Fotograma : 0;
        }
    }

    static Quaternion Q(Quaternion q)
    {
        return q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 0.5f ? q : Quaternion.identity;
    }

    // La pose en un fotograma (en el dibujo): con claves, viaja suave entre ellas.
    static void PoseLocal(int f, out Vector3 p, out Quaternion r, out float c)
    {
        var k = datos.claves;
        if (k.Count == 0)
        {
            p = datos.posicion;
            r = Q(datos.rotacion);
            c = datos.campo;
            return;
        }
        if (f <= k[0].fotograma || k.Count == 1)
        {
            p = k[0].posicion;
            r = Q(k[0].rotacion);
            c = k[0].campo;
            return;
        }
        var ultima = k[k.Count - 1];
        if (f >= ultima.fotograma)
        {
            p = ultima.posicion;
            r = Q(ultima.rotacion);
            c = ultima.campo;
            return;
        }
        int i = 0;
        while (i < k.Count - 2 && k[i + 1].fotograma <= f)
            i++;
        var a = k[i];
        var b = k[i + 1];
        float u = Mathf.Clamp01((f - a.fotograma) / (float)Mathf.Max(1, b.fotograma - a.fotograma));
        u = u * u * (3f - 2f * u);   // arranca y frena suave
        p = Vector3.Lerp(a.posicion, b.posicion, u);
        r = Quaternion.Slerp(Q(a.rotacion), Q(b.rotacion), u);
        c = Mathf.Lerp(a.campo, b.campo, u);
    }

    // Para exportar: la cámara en el fotograma f (en el mundo). false si no hay cámara.
    public static bool PoseMundo(int f, out Vector3 pos, out Quaternion rot, out float campo)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;
        campo = 40f;
        var d = ElDibujo;
        if (!datos.hay || d == null)
            return false;
        Vector3 p;
        Quaternion r;
        PoseLocal(f, out p, out r, out campo);
        pos = d.transform.TransformPoint(p);
        rot = d.transform.rotation * r;
        return true;
    }

    // Una pose nueva (en el mundo): con claves, se guarda como clave en el fotograma actual; sin claves, es LA pose.
    static void PonerPose(Vector3 pos, Quaternion rot, float campo, float distanciaMundo)
    {
        var d = ElDibujo;
        if (d == null)
            return;
        d.GuardarParaDeshacer();
        Vector3 p = d.transform.InverseTransformPoint(pos);
        Quaternion r = Quaternion.Inverse(d.transform.rotation) * rot;
        campo = Mathf.Clamp(campo, CampoMin, CampoMax);
        float escala = Mathf.Max(1e-4f, d.transform.lossyScale.x);
        datos.hay = true;
        datos.distancia = Mathf.Max(0.05f, distanciaMundo / escala);
        if (datos.claves.Count > 0)
        {
            PonerClave(FotogramaActual, p, r, campo);
        }
        else
        {
            datos.posicion = p;
            datos.rotacion = r;
            datos.campo = campo;
        }
        d.Avisar();
    }

    static void PonerClave(int f, Vector3 p, Quaternion r, float c)
    {
        foreach (var k in datos.claves)
            if (k.fotograma == f)
            {
                k.posicion = p;
                k.rotacion = r;
                k.campo = c;
                return;
            }
        datos.claves.Add(new ClaveCamara { fotograma = f, posicion = p, rotacion = r, campo = c });
        datos.claves.Sort((a, b) => a.fotograma.CompareTo(b.fotograma));
    }

    public static bool EsClave(int f)
    {
        foreach (var k in datos.claves)
            if (k.fotograma == f)
                return true;
        return false;
    }

    // ---------- Lo que se puede pedir (botones y voz) ----------

    public static void Asegurar()
    {
        if (instancia == null)
            instancia = new GameObject("CamaraEscena").AddComponent<CamaraEscena>();
    }

    // La voz ("cámara"): ve a la capa de la cámara (o vuelve a tu capa de dibujo). La primera vez se pone sola.
    public static void Alternar()
    {
        Asegurar();
        var d = ElDibujo;
        if (d == null)
            return;
        if (Visible)
        {
            d.SeleccionarCapa(d.UltimaCapaDeDibujo);
            return;
        }
        d.SeleccionarCapa(d.CamaraActiva);
    }

    public static void SugerirVoz()
    {
        Asegurar();
        var d = ElDibujo;
        if (d == null)
            return;
        if (!Visible)
            d.SeleccionarCapa(d.CamaraActiva);
        instancia.Sugerir(!datos.hay);
    }

    public static void AgregarClave()
    {
        var d = ElDibujo;
        if (d == null)
            return;
        Asegurar();
        if (!datos.hay)
            instancia.Sugerir(true);
        d.GuardarParaDeshacer();
        int f = FotogramaActual;
        Vector3 p;
        Quaternion r;
        float c;
        PoseLocal(f, out p, out r, out c);
        PonerClave(f, p, r, c);
        d.Avisar();
        Mensaje("Clave de cámara en el fotograma " + (f + 1) + " (" + datos.claves.Count + " en total)");
    }

    public static void QuitarClave()
    {
        var d = ElDibujo;
        if (d == null)
            return;
        int f = FotogramaActual;
        int i = datos.claves.FindIndex(k => k.fotograma == f);
        if (i < 0)
        {
            Mensaje("Aquí no hay clave de cámara");
            return;
        }
        d.GuardarParaDeshacer();
        // La última que se quita deja la cámara donde estaba.
        if (datos.claves.Count == 1)
        {
            datos.posicion = datos.claves[0].posicion;
            datos.rotacion = datos.claves[0].rotacion;
            datos.campo = datos.claves[0].campo;
        }
        datos.claves.RemoveAt(i);
        d.Avisar();
        Mensaje("Clave de cámara quitada");
    }

    public static void Quitar()
    {
        var d = ElDibujo;
        if (d == null)
            return;
        d.GuardarParaDeshacer();
        var c = datos;
        c.hay = false;
        c.claves.Clear();
        d.Avisar();
        Mensaje("Cámara vacía: el encuadre vuelve a ser automático (deshacer la devuelve) · Sugiere: ponerla otra vez");
    }

    static void Mensaje(string t)
    {
        var d = ElDibujo;
        if (d != null)
            d.Mensaje(t);
    }

    // Un pellizco ahí es de la cámara (no elige líneas).
    public static bool Contiene(Vector3 punto)
    {
        return instancia != null && instancia.cuerpo != null && instancia.cuerpo.gameObject.activeInHierarchy
               && Vector3.Distance(punto, instancia.cuerpo.position) < RadioAgarre;
    }

    // ---------- La camarita, el cuadro y el monitor ----------

    class Sugerencia
    {
        public Vector3 pos;
        public Quaternion rot;
        public float campo, distancia, puntos;
    }

    ControlManos control;
    Transform raiz, cuerpo, panel, monitor, fondoMonitor;
    readonly Transform[] cuadro = new Transform[4];
    Camera lente;
    RenderTexture rtMonitor;
    Material matMonitor, matPanel, matBoton, matMarcado, matImagen;
    readonly List<Material> materiales = new List<Material>();
    readonly List<BotonTocable> botones = new List<BotonTocable>();
    BotonTocable btnClave;
    readonly BotonTocable[] miniaturas = new BotonTocable[3];
    readonly RenderTexture[] rtMinis = new RenderTexture[3];
    readonly List<Sugerencia> sugerencias = new List<Sugerencia>();
    float minisHasta;
    bool armada, verticalPuesto, agarrada, conIzquierda, intentoAuto;
    // v48: apuntar libre (se recuerda) y ponerte detrás de la cámara al entrar a su capa.
    const string ClaveLibre = "jcartoons_camara_libre";
    static bool Libre
    {
        get => PlayerPrefs.GetInt(ClaveLibre, 0) == 1;
        set { PlayerPrefs.SetInt(ClaveLibre, value ? 1 : 0); PlayerPrefs.Save(); }
    }
    Quaternion manoInicio = Quaternion.identity;
    bool estabaEnCapa, ponerDetras;
    Vector3 desfase, objetivo, poseViva;
    Quaternion rotViva, rotInicio;
    float distanciaViva;

    void Armar()
    {
        armada = true;
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (nav != null)
        {
            matPanel = nav.materialPanel;
            matBoton = nav.materialBoton;
            matMarcado = nav.materialBotonMarcado;
            matImagen = nav.materialImagen;
        }
        raiz = new GameObject("Camara").transform;
        raiz.SetParent(transform, false);

        // La camarita: el cuerpo, el lente (adelante) y el cuadro de lo que ve.
        cuerpo = new GameObject("Camarita").transform;
        cuerpo.SetParent(raiz, false);
        Pieza(PrimitiveType.Cube, cuerpo, new Vector3(0f, 0f, -0.012f), Quaternion.identity, new Vector3(0.05f, 0.034f, 0.045f), matMarcado);
        Pieza(PrimitiveType.Cylinder, cuerpo, new Vector3(0f, 0f, 0.018f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.026f, 0.012f, 0.026f), matMarcado);
        Pieza(PrimitiveType.Cylinder, cuerpo, new Vector3(-0.012f, 0.026f, -0.012f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.022f, 0.004f, 0.022f), matMarcado);
        Pieza(PrimitiveType.Cylinder, cuerpo, new Vector3(0.013f, 0.026f, -0.012f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.022f, 0.004f, 0.022f), matMarcado);
        for (int i = 0; i < 4; i++)
            cuadro[i] = Pieza(PrimitiveType.Cube, cuerpo, Vector3.zero, Quaternion.identity, Vector3.one * 0.002f, matMarcado);

        // El lente de verdad (no se ve): lo que ve el monitor y las miniaturas.
        var goLente = new GameObject("Lente");
        goLente.transform.SetParent(raiz, false);
        lente = goLente.AddComponent<Camera>();
        lente.enabled = false;
        lente.stereoTargetEye = StereoTargetEyeMask.None;
        lente.clearFlags = CameraClearFlags.SolidColor;
        lente.nearClipPlane = 0.02f;
        lente.farClipPlane = 100f;

        // El monitor, con sus botones debajo y las miniaturas (de Sugiere) encima.
        panel = new GameObject("Monitor").transform;
        panel.SetParent(raiz, false);
        // v44: con borde gris (con el fondo blanco no se veía dónde empieza).
        Material matBorde = matPanel != null ? new Material(matPanel) : null;
        if (matBorde != null)
        {
            materiales.Add(matBorde);
            PintarColor(matBorde, new Color(0.55f, 0.56f, 0.6f));
        }
        fondoMonitor = Pieza(PrimitiveType.Quad, panel, new Vector3(0f, 0f, 0.002f), Quaternion.identity, Vector3.one, matBorde);
        matMonitor = matImagen != null ? new Material(matImagen) : null;
        if (matMonitor != null)
            materiales.Add(matMonitor);
        monitor = Pieza(PrimitiveType.Quad, panel, Vector3.zero, Quaternion.identity, Vector3.one, matMonitor);
        string[] nombres = { "Clave", "Quita", "Sugiere", "+", "−", "Quitar", "Libre" };
        for (int i = 0; i < nombres.Length; i++)
        {
            var b = Boton(panel, nombres[i], Vector3.zero, new Vector2(i >= 3 && i <= 4 ? 0.02f : 0.03f, 0.016f));
            botones.Add(b);
        }
        btnClave = botones[0];
        botones[0].alTocar.AddListener(AgregarClave);
        botones[1].alTocar.AddListener(QuitarClave);
        botones[2].alTocar.AddListener(() => Sugerir(false));
        botones[3].alTocar.AddListener(() => Zoom(1f / 1.2f));
        botones[4].alTocar.AddListener(() => Zoom(1.2f));
        botones[5].alTocar.AddListener(Quitar);
        botones[6].alTocar.AddListener(() =>
        {
            Libre = !Libre;
            botones[6].Marcar(Libre);
            Mensaje(Libre ? "Cámara libre: pellízcala con la derecha y apúntala a donde quieras (gira con tu mano)"
                          : "Cámara: siempre mira a tu dibujo");
        });
        BotonTocable.PonerNombre(botones[6], "Apuntar libre (gira con tu mano)");
        BotonTocable.PonerVoz(botones[0], ControlManos.ComandoVoz.ClaveCamara);
        BotonTocable.PonerNombre(botones[1], "Quitar la clave de cámara");
        BotonTocable.PonerVoz(botones[2], ControlManos.ComandoVoz.SugiereCamara);
        BotonTocable.PonerNombre(botones[3], "Acercar (zoom)");
        BotonTocable.PonerNombre(botones[4], "Alejar (zoom)");
        BotonTocable.PonerNombre(botones[5], "Quitar la cámara (encuadre automático)");
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Sugerencia" + (i + 1);
            go.transform.SetParent(panel, false);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            var m = matImagen != null ? new Material(matImagen) : null;
            if (m != null)
            {
                materiales.Add(m);
                r.sharedMaterial = m;
            }
            var b = go.AddComponent<BotonTocable>();
            b.materialNormal = m;
            b.materialMarcado = m;
            b.alTocar.AddListener(() => ElegirSugerencia(k));
            BotonTocable.PonerNombre(b, "Usar este encuadre");
            miniaturas[i] = b;
            go.SetActive(false);
        }
        Acomodar(ExportadorVideo.Vertical);
    }

    Transform Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Quaternion rot, Vector3 escala, Material m)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = escala;
        var r = go.GetComponent<Renderer>();
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    BotonTocable Boton(Transform padre, string texto, Vector3 pos, Vector2 tam)
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Boton_" + texto;
        cubo.transform.SetParent(padre, false);
        cubo.transform.localPosition = pos;
        cubo.transform.localScale = new Vector3(tam.x, tam.y, 0.006f);
        var r = cubo.GetComponent<Renderer>();
        if (matBoton != null)
            r.sharedMaterial = matBoton;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        var b = cubo.AddComponent<BotonTocable>();
        b.materialNormal = matBoton;
        b.materialMarcado = matMarcado;
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var t = go.AddComponent<TextMeshPro>();
        Idioma.Poner(t, texto);
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.005f;
        t.fontSizeMax = 0.12f;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.black;
        t.rectTransform.sizeDelta = new Vector2(tam.x * 0.92f, tam.y * 0.8f);
        b.etiqueta = t;
        return b;
    }

    // El monitor horizontal (16:9) o vertical (9:16), como el formato de Exportar.
    void Acomodar(bool vertical)
    {
        verticalPuesto = vertical;
        float w = vertical ? 0.072f : 0.16f, h = vertical ? 0.128f : 0.09f;
        monitor.localScale = new Vector3(w, h, 1f);
        fondoMonitor.localScale = new Vector3(w + 0.007f, h + 0.007f, 1f);
        if (rtMonitor != null)
            rtMonitor.Release();
        rtMonitor = new RenderTexture(vertical ? 144 : 256, vertical ? 256 : 144, 24, RenderTextureFormat.ARGB32);
        rtMonitor.Create();
        PonerTextura(matMonitor, rtMonitor);
        float yBotones = -h * 0.5f - 0.016f;
        float[] xs = { -0.069f, -0.036f, -0.003f, 0.025f, 0.048f, 0.077f, 0f };
        for (int i = 0; i < botones.Count; i++)
        {
            var b = botones[i];
            // v48: "Libre" va en una segunda fila, al centro.
            Vector3 p = new Vector3(xs[i], i == 6 ? yBotones - 0.02f : yBotones, 0f);
            b.transform.localPosition = p;
            b.etiqueta.transform.localPosition = p + new Vector3(0f, 0f, -0.0036f);
        }
        float mw = vertical ? 0.027f : 0.048f, mh = vertical ? 0.048f : 0.027f;
        for (int i = 0; i < 3; i++)
        {
            miniaturas[i].transform.localPosition = new Vector3((i - 1) * (mw + 0.006f), h * 0.5f + mh * 0.5f + 0.012f, 0f);
            miniaturas[i].transform.localScale = new Vector3(mw, mh, 0.004f);
            if (rtMinis[i] != null)
                rtMinis[i].Release();
            rtMinis[i] = new RenderTexture(vertical ? 72 : 128, vertical ? 128 : 72, 24, RenderTextureFormat.ARGB32);
            rtMinis[i].Create();
            PonerTextura(miniaturas[i].materialNormal, rtMinis[i]);
        }
    }

    static void PintarColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }

    // v44: la miniatura de la capa de cámara (lo que ve), para la tarjeta de capas.
    public static RenderTexture Monitor => instancia != null ? instancia.rtMonitor : null;

    static void PonerTextura(Material m, Texture t)
    {
        if (m == null)
            return;
        if (m.HasProperty("_BaseMap"))
            m.SetTexture("_BaseMap", t);
        if (m.HasProperty("_MainTex"))
            m.SetTexture("_MainTex", t);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", Color.white);
    }

    // ---------- Cada cuadro ----------

    void LateUpdate()
    {
        if (control == null)
            control = ControlManos.Instancia;
        var d = ElDibujo;
        var cab = control != null ? control.Cabeza : null;
        bool enCapa = Visible && d != null && cab != null && !ExportadorVideo.Exportando && !Tutorial.EnCurso;
        // v44: al llegar a una capa de cámara vacía, se pone sola en el mejor ángulo (una vez).
        if (enCapa && !datos.hay && !intentoAuto)
        {
            intentoAuto = true;
            Sugerir(true);
        }
        if (!enCapa)
            intentoAuto = false;
        // v48: al entrar a la capa de cámara, apareces detrás de ella (cuando ya sabe dónde está).
        if (enCapa && !estabaEnCapa)
            ponerDetras = true;
        estabaEnCapa = enCapa;
        bool ver = enCapa && datos.hay && !VistaLimpia.Activa;
        if (!ver)
        {
            if (agarrada)
                Soltar(false);
            if (raiz != null && raiz.gameObject.activeSelf)
                raiz.gameObject.SetActive(false);
            return;
        }
        if (!armada)
            Armar();
        if (!raiz.gameObject.activeSelf)
            raiz.gameObject.SetActive(true);
        bool vertical = ExportadorVideo.Vertical;
        if (vertical != verticalPuesto)
            Acomodar(vertical);
        float aspecto = vertical ? 9f / 16f : 16f / 9f;

        RevisarAgarre(d);
        float campo;
        if (!agarrada)
        {
            PoseMundo(FotogramaActual, out poseViva, out rotViva, out campo);
            distanciaViva = datos.distancia * d.transform.lossyScale.x;
        }
        else
        {
            Vector3 p0;
            Quaternion r0;
            PoseMundo(FotogramaActual, out p0, out r0, out campo);
        }
        cuerpo.SetPositionAndRotation(poseViva, rotViva);
        if (ponerDetras && !agarrada)
        {
            ponerDetras = false;
            PonermeDetras(cab);
        }
        if (botones.Count > 6)
            botones[6].Marcar(Libre);

        // El cuadro: un rectángulo adelante del lente, del tamaño de lo que ve.
        float altoMedio = Mathf.Tan(campo * 0.5f * Mathf.Deg2Rad) * DistanciaCuadro;
        float anchoMedio = altoMedio * aspecto;
        float g = 0.0015f;
        cuadro[0].localPosition = new Vector3(0f, altoMedio, DistanciaCuadro);
        cuadro[0].localScale = new Vector3(anchoMedio * 2f + g, g, g);
        cuadro[1].localPosition = new Vector3(0f, -altoMedio, DistanciaCuadro);
        cuadro[1].localScale = new Vector3(anchoMedio * 2f + g, g, g);
        cuadro[2].localPosition = new Vector3(-anchoMedio, 0f, DistanciaCuadro);
        cuadro[2].localScale = new Vector3(g, altoMedio * 2f + g, g);
        cuadro[3].localPosition = new Vector3(anchoMedio, 0f, DistanciaCuadro);
        cuadro[3].localScale = new Vector3(g, altoMedio * 2f + g, g);

        // El monitor: encima de la camarita, mirándote.
        Vector3 lugar = poseViva + Vector3.up * (vertical ? 0.13f : 0.1f);
        Vector3 mira = lugar - cab.position;
        panel.SetPositionAndRotation(lugar, mira.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(mira, Vector3.up) : cab.rotation);
        bool esClave = EsClave(FotogramaActual);
        btnClave.Marcar(esClave);

        if (Time.frameCount % CadaCuantos == 0)
            Fotografiar(rtMonitor, poseViva, rotViva, campo, d);

        if (minisHasta > 0f && Time.time > minisHasta)
            MostrarMiniaturas(false);
    }

    void Fotografiar(RenderTexture rt, Vector3 pos, Quaternion rot, float campo, Dibujo d)
    {
        if (rt == null || lente == null)
            return;
        lente.transform.SetPositionAndRotation(pos, rot);
        lente.fieldOfView = Mathf.Clamp(campo, 5f, 120f);
        lente.aspect = rt.width / (float)rt.height;
        lente.cullingMask = d.MascaraExportar;
        lente.backgroundColor = ExportadorVideo.FondoVerde ? new Color(0f, 0.694f, 0.251f) : Color.white;
        Exportar.Captura.RenderizarEn(lente, rt);
    }

    // Pellizco DERECHO: gira alrededor de lo que mira. IZQUIERDO: se corre sin cambiar hacia dónde mira.
    void RevisarAgarre(Dibujo d)
    {
        var der = control.Der;
        var izq = control.Izq;
        if (agarrada)
        {
            var m = conIzquierda ? izq : der;
            if (!m.valida || !m.pellizco)
            {
                Soltar(true);
                return;
            }
            poseViva = m.PuntoPellizco + desfase;
            if (!conIzquierda && Libre && der.hand != null)
            {
                // v48: libre: la cámara gira con tu mano, como si la sostuvieras.
                Quaternion giro = der.hand.transform.rotation * Quaternion.Inverse(manoInicio);
                poseViva = m.PuntoPellizco + giro * desfase;
                rotViva = giro * rotInicio;
            }
            else if (conIzquierda)
            {
                rotViva = rotInicio;
            }
            else
            {
                Vector3 hacia = objetivo - poseViva;
                if (hacia.sqrMagnitude > 1e-4f)
                {
                    rotViva = Quaternion.LookRotation(hacia, Vector3.up);
                    distanciaViva = hacia.magnitude;
                }
            }
            return;
        }
        if (control.Ocupado)
            return;
        bool derecha = der.valida && der.empezoPellizco && Vector3.Distance(der.PuntoPellizco, cuerpo.position) < RadioAgarre;
        bool izquierda = !derecha && izq.valida && izq.empezoPellizco && Vector3.Distance(izq.PuntoPellizco, cuerpo.position) < RadioAgarre;
        if (!derecha && !izquierda)
            return;
        agarrada = true;
        conIzquierda = izquierda;
        var mano = izquierda ? izq : der;
        desfase = cuerpo.position - mano.PuntoPellizco;
        rotInicio = cuerpo.rotation;
        objetivo = cuerpo.position + cuerpo.forward * Mathf.Max(0.05f, distanciaViva);
        manoInicio = mano.hand != null ? mano.hand.transform.rotation : Quaternion.identity;
        control.Ocupado = true;
        control.SonidoBurbuja(cuerpo.position);
    }

    // v48: te pone justo detrás de la cámara, mirando hacia donde ella mira (mueve tu lugar en el mundo, no la cámara).
    void PonermeDetras(Transform cab)
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null || cab == null)
            return;
        Vector3 adelante = Vector3.ProjectOnPlane(rotViva * Vector3.forward, Vector3.up);
        Vector3 miAdelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f || miAdelante.sqrMagnitude < 1e-4f)
            return;
        adelante.Normalize();
        float angulo = Vector3.SignedAngle(miAdelante, adelante, Vector3.up);
        Transform r = rig.transform;
        r.RotateAround(cab.position, Vector3.up, angulo);
        // 40 cm detrás y un poquito arriba (para ver la camarita y su monitor).
        Vector3 deseado = poseViva - adelante * 0.4f + Vector3.up * 0.05f;
        r.position += deseado - cab.position;
    }

    void Soltar(bool guardar)
    {
        agarrada = false;
        if (control != null)
            control.Ocupado = false;
        if (!guardar)
            return;
        float campo;
        Vector3 p;
        Quaternion r;
        PoseMundo(FotogramaActual, out p, out r, out campo);
        PonerPose(poseViva, rotViva, campo, distanciaViva);
        if (control != null)
            control.SonidoBurbuja(poseViva);
        if (datos.claves.Count > 0)
            Mensaje("Clave de cámara en el fotograma " + (FotogramaActual + 1));
    }

    void Zoom(float factor)
    {
        Vector3 p;
        Quaternion r;
        float campo;
        if (!PoseMundo(FotogramaActual, out p, out r, out campo))
            return;
        var d = ElDibujo;
        PonerPose(p, r, campo * factor, datos.distancia * d.transform.lossyScale.x);
        Mensaje("Cámara: " + Mathf.RoundToInt(Mathf.Clamp(campo * factor, CampoMin, CampoMax)) + "°");
    }

    // ---------- Sugiere: los 3 mejores encuadres ----------

    void Sugerir(bool aplicarMejor)
    {
        var d = ElDibujo;
        if (control == null)
            control = ControlManos.Instancia;
        var cab = control != null ? control.Cabeza : null;
        if (d == null || cab == null)
            return;
        var puntos = Puntos(d);
        if (puntos.Count == 0)
        {
            Mensaje("Dibuja algo primero: la cámara busca el mejor ángulo de tu dibujo");
            return;
        }
        Vector3 c = Vector3.zero;
        foreach (var p in puntos)
            c += p;
        c /= puntos.Count;
        Vector3 frente = c - cab.position;
        frente.y = 0f;
        if (frente.sqrMagnitude < 1e-4f)
            frente = cab.forward;
        frente.y = 0f;
        frente = frente.sqrMagnitude > 1e-6f ? frente.normalized : Vector3.forward;
        float aspecto = ExportadorVideo.Vertical ? 9f / 16f : 16f / 9f;

        var candidatas = new List<Sugerencia>();
        // En 2D: de frente al plano de la capa (la mejor, casi siempre).
        Vector3 pp, n;
        if (d.plano && d.PlanoDeCapa(d.capaActual, out pp, out n))
        {
            Vector3 nm = d.transform.TransformDirection(n).normalized;
            if (Vector3.Dot(nm, frente) < 0f)
                nm = -nm;
            var s = Encuadrar(puntos, c, nm, aspecto);
            s.puntos += 1f;
            candidatas.Add(s);
        }
        float[] giros = { 0f, -30f, 30f, -55f, 55f, -80f, 80f };
        float[] alturas = { 0f, 20f, -12f, 40f };
        Vector3 lado = Vector3.Cross(Vector3.up, frente).normalized;
        foreach (float giro in giros)
            foreach (float alto in alturas)
            {
                Vector3 dir = Quaternion.AngleAxis(giro, Vector3.up) * frente;
                Vector3 ladoGiro = Quaternion.AngleAxis(giro, Vector3.up) * lado;
                dir = Quaternion.AngleAxis(alto, ladoGiro) * dir;   // + = desde arriba
                var s = Encuadrar(puntos, c, dir.normalized, aspecto);
                // Un poquito mejor de frente y un poco desde arriba (como se suele mirar un dibujo).
                s.puntos -= Mathf.Abs(giro) * 0.0012f + Mathf.Abs(alto - 10f) * 0.0015f;
                candidatas.Add(s);
            }
        candidatas.Sort((a, b) => b.puntos.CompareTo(a.puntos));
        sugerencias.Clear();
        foreach (var s in candidatas)
        {
            bool lejos = true;
            foreach (var y in sugerencias)
                if (Quaternion.Angle(y.rot, s.rot) < 20f)
                    lejos = false;
            if (lejos)
                sugerencias.Add(s);
            if (sugerencias.Count == 3)
                break;
        }
        if (aplicarMejor && sugerencias.Count > 0)
        {
            var m = sugerencias[0];
            PonerPose(m.pos, m.rot, m.campo, m.distancia);
            Mensaje(d.plano ? "Cámara de frente a tu plano 2D · Sugiere: otros encuadres" : "Cámara puesta en el mejor ángulo · pellízcala para moverla");
            return;
        }
        if (!armada)
            Armar();
        for (int i = 0; i < 3; i++)
            if (i < sugerencias.Count)
                Fotografiar(rtMinis[i], sugerencias[i].pos, sugerencias[i].rot, sugerencias[i].campo, d);
        MostrarMiniaturas(true);
        Mensaje("3 encuadres sugeridos: toca uno (encima del monitor)");
    }

    void ElegirSugerencia(int i)
    {
        if (i < 0 || i >= sugerencias.Count)
            return;
        var s = sugerencias[i];
        PonerPose(s.pos, s.rot, s.campo, s.distancia);
        MostrarMiniaturas(false);
        Mensaje(datos.claves.Count > 0 ? "Encuadre puesto (clave en este fotograma)" : "Encuadre puesto");
    }

    void MostrarMiniaturas(bool ver)
    {
        minisHasta = ver ? Time.time + 25f : 0f;
        for (int i = 0; i < 3; i++)
        {
            bool v = ver && i < sugerencias.Count;
            if (miniaturas[i] != null && miniaturas[i].gameObject.activeSelf != v)
                miniaturas[i].gameObject.SetActive(v);
        }
    }

    // Los puntos del dibujo que se ven (cada línea, con puntitos entre sus nodos).
    static List<Vector3> Puntos(Dibujo d)
    {
        var lista = new List<Vector3>();
        foreach (var t in d.trazos)
        {
            if (t == null || !t.gameObject.activeInHierarchy || t.oculto || t.nodos.Count == 0)
                continue;
            var tr = t.transform;
            Vector3 antes = tr.TransformPoint(t.nodos[0]);
            lista.Add(antes);
            for (int i = 1; i < t.nodos.Count; i++)
            {
                Vector3 p = tr.TransformPoint(t.nodos[i]);
                lista.Add(Vector3.Lerp(antes, p, 0.33f));
                lista.Add(Vector3.Lerp(antes, p, 0.66f));
                lista.Add(p);
                antes = p;
            }
            if (lista.Count > 6000)
                break;
        }
        return lista;
    }

    // Cómo se ve el dibujo mirando hacia "dir": qué tanto llena el cuadro sin encimarse (puntos) y dónde poner la
    // cámara para que quepa todo, con un poco de aire arriba.
    static Sugerencia Encuadrar(List<Vector3> puntos, Vector3 c, Vector3 dir, float aspecto)
    {
        const float Campo = 40f;
        const int Celdas = 24;
        var rot = Quaternion.LookRotation(dir, Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up);
        Vector3 der = rot * Vector3.right, arr = rot * Vector3.up;
        float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue, z0 = float.MaxValue;
        foreach (var p in puntos)
        {
            Vector3 v = p - c;
            float x = Vector3.Dot(v, der), y = Vector3.Dot(v, arr), z = Vector3.Dot(v, dir);
            if (x < x0) x0 = x;
            if (x > x1) x1 = x;
            if (y < y0) y0 = y;
            if (y > y1) y1 = y;
            if (z < z0) z0 = z;
        }
        float w = Mathf.Max(1e-3f, x1 - x0), h = Mathf.Max(1e-3f, y1 - y0);
        // Qué tanto del cuadro llenan las líneas sin encimarse (celdas ocupadas).
        var ocupadas = new bool[Celdas * Celdas];
        int cuenta = 0;
        foreach (var p in puntos)
        {
            Vector3 v = p - c;
            int cx = Mathf.Clamp((int)((Vector3.Dot(v, der) - x0) / w * Celdas), 0, Celdas - 1);
            int cy = Mathf.Clamp((int)((Vector3.Dot(v, arr) - y0) / h * Celdas), 0, Celdas - 1);
            int k = cy * Celdas + cx;
            if (!ocupadas[k])
            {
                ocupadas[k] = true;
                cuenta++;
            }
        }
        float lleno = cuenta / (float)(Celdas * Celdas);
        float forma = w / h;
        float cabe = Mathf.Min(forma / aspecto, aspecto / forma);   // 1 = la forma del dibujo es la del cuadro
        var s = new Sugerencia();
        s.puntos = lleno * Mathf.Pow(cabe, 0.35f);
        s.campo = Campo;
        float medio = Mathf.Max(h * 0.5f, w * 0.5f / aspecto) * 1.18f;
        float distancia = medio / Mathf.Tan(Campo * 0.5f * Mathf.Deg2Rad) + Mathf.Max(0f, -z0);
        distancia = Mathf.Max(0.15f, distancia);
        // El centro del cuadro un poquito más arriba que el del dibujo: le deja aire arriba.
        Vector3 centro = c + der * ((x0 + x1) * 0.5f) + arr * ((y0 + y1) * 0.5f + medio * 0.05f);
        s.pos = centro - dir * distancia;
        s.rot = rot;
        s.distancia = distancia;
        return s;
    }

    void OnDestroy()
    {
        foreach (var m in materiales)
            if (m != null)
                Destroy(m);
        if (rtMonitor != null)
            rtMonitor.Release();
        foreach (var r in rtMinis)
            if (r != null)
                r.Release();
        if (instancia == this)
            instancia = null;
        if (agarrada && control != null)
            control.Ocupado = false;
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;

// CAPAS EN PROFUNDIDAD (botón "Capas" del menú de la mano). En vez de una lista, las capas cuelgan como papeles,
// una detrás de otra, arriba al centro de tu vista (donde sale el cartel de cómic, que se esconde mientras tanto).
//  - La capa ACTUAL: siempre en el mismo lugar, sólida, con su nombre y sus botones (Ver, Boceto, Plano, Liberar).
//  - Las de ADELANTE (más cerca de ti): fantasmas transparentes, sin letras, solo con los bordes iluminados.
//  - Las de ATRÁS: físicamente más al fondo, también transparentes.
//    Capa 1 queda al fondo y la 4 más cerca de ti, igual que se apilan los planos 2D.
//  - EMPUJA la capa actual con el índice derecho (unos 3 cm hacia el fondo, tocando la parte de arriba, la del
//    nombre): se va atrás y la de adelante pasa a ser la actual. ATRÁELA hacia ti: se vuelve fantasma y la de
//    atrás pasa a ser la actual. Como papeles colgados.
//  - Se cierra sola si no la tocas en 8 segundos (o con su X). Cerca de ella, el dedo queda libre: sin la bolita de
//    la tinta ni del borrador, y no dibuja.
public class CapasProfundidad : MonoBehaviour
{
    const float Ancho = 0.3f, Alto = 0.07f;
    const float PasoFondo = 0.07f;                          // metros entre una capa y la siguiente
    const float SubeAdelante = 0.024f, BajaAtras = 0.02f;   // las de adelante un poco más arriba; las de atrás, abajo
    const float Empujon = 0.03f;                            // cuánto hay que empujar o atraer
    const float CerrarSola = 8f;
    const float Borde = 0.0022f;
    const float FilaBotones = -0.019f;                      // la fila de botones (abajo de la tarjeta)

    static CapasProfundidad instancia;
    static int cercaCuadro = -10;

    public static bool Abierta => instancia != null && instancia.raiz != null && instancia.raiz.gameObject.activeSelf;
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
    BotonTocable btnVer, btnBoceto, btnPlano, btnLiberar, btnCerrar;
    readonly List<BotonTocable> botones = new List<BotonTocable>();
    Material materialPanel, materialBoton, materialBotonMarcado, materialTransparente;
    readonly List<Material> materiales = new List<Material>();
    readonly List<Fantasma> fantasmas = new List<Fantasma>();
    float ultimoToque, aparece = 1f, proximoRefresco, avisoHasta;
    int capaMostrada = -1;
    bool empujando, esperarSalir;
    float zInicio, desdeEmpuje, sacudida;

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
        instancia.Abrir();
    }

    public static void CerrarSiAbierta()
    {
        if (Abierta)
            instancia.Cerrar();
    }

    void Abrir()
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
        raiz.gameObject.SetActive(true);
        ultimoToque = Time.time;
        capaMostrada = -1;
        empujando = false;
        esperarSalir = true; // si el dedo ya estaba ahí, no empuja hasta que salga
        int cur = Mathf.Clamp(dibujo.capaActual, 0, Mathf.Max(0, dibujo.capas.Count - 1));
        AsegurarFantasmas();
        for (int k = 0; k < fantasmas.Count; k++)
            fantasmas[k].s = k - cur;
        Ayuda("Empuja: capa de adelante · Atrae hacia ti: capa de atrás");
    }

    void Cerrar()
    {
        empujando = false;
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
        Quad(tarjeta, "Fondo", Vector3.zero, new Vector2(Ancho, Alto), materialPanel);
        Marco(tarjeta, materialBotonMarcado, 0.0018f, -0.0004f);
        nombre = Texto(tarjeta, "Capa 1", new Vector3(-0.02f, 0.017f, -0.002f), new Vector2(0.2f, 0.022f), 0.28f, Color.black);
        nombre.fontStyle = FontStyles.Bold;
        btnVer = Boton(tarjeta, "Ver", new Vector3(-0.105f, FilaBotones, 0f), new Vector2(0.05f, 0.018f));
        btnBoceto = Boton(tarjeta, "Boceto: No", new Vector3(-0.04f, FilaBotones, 0f), new Vector2(0.07f, 0.018f));
        btnPlano = Boton(tarjeta, "Plano: unido", new Vector3(0.037f, FilaBotones, 0f), new Vector2(0.07f, 0.018f));
        btnLiberar = Boton(tarjeta, "Liberar", new Vector3(0.105f, FilaBotones, 0f), new Vector2(0.05f, 0.018f));
        btnCerrar = Boton(tarjeta, "X", new Vector3(Ancho * 0.5f - 0.014f, 0.022f, 0f), new Vector2(0.018f, 0.018f));
        btnVer.alTocar.AddListener(() => { dibujo.AlternarVerCapa(Actual); Tocado(); });
        btnBoceto.alTocar.AddListener(() => { if (dibujo.temblor != null) dibujo.temblor.SiguienteBoceto(); Tocado(); });
        btnPlano.alTocar.AddListener(() => { dibujo.AlternarUnirPlano(); Tocado(); });
        btnLiberar.alTocar.AddListener(() => { dibujo.LiberarCapa(Actual); Tocado(); });
        btnCerrar.alTocar.AddListener(Cerrar);

        // Una ayudita debajo (blanca con contorno, para que se lea sobre cualquier fondo).
        ayuda = Texto(raiz, "", new Vector3(0f, -Alto * 0.5f - 0.012f, -0.002f), new Vector2(Ancho, 0.012f), 0.11f, Color.white);
        ayuda.outlineWidth = 0.25f;
        ayuda.outlineColor = new Color32(30, 30, 40, 255);
        AsegurarFantasmas();
    }

    int Actual => Mathf.Clamp(dibujo.capaActual, 0, Mathf.Max(0, dibujo.capas.Count - 1));

    void AsegurarFantasmas()
    {
        if (raiz == null || dibujo == null)
            return;
        while (fantasmas.Count < dibujo.capas.Count)
        {
            var f = new Fantasma();
            f.t = new GameObject("Fantasma" + (fantasmas.Count + 1)).transform;
            f.t.SetParent(raiz, false);
            f.relleno = Copia(materialTransparente, new Color(0.9f, 0.93f, 1f, 0.15f));
            f.borde = Copia(materialTransparente, new Color(0.85f, 0.92f, 1f, 0.6f));
            if (f.relleno != null)
                Quad(f.t, "Relleno", Vector3.zero, new Vector2(Ancho, Alto), f.relleno);
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
    static void Marco(Transform padre, Material m, float grosor, float z)
    {
        Quad(padre, "BordeArriba", new Vector3(0f, Alto * 0.5f, z), new Vector2(Ancho + grosor, grosor), m);
        Quad(padre, "BordeAbajo", new Vector3(0f, -Alto * 0.5f, z), new Vector2(Ancho + grosor, grosor), m);
        Quad(padre, "BordeIzq", new Vector3(-Ancho * 0.5f, 0f, z), new Vector2(grosor, Alto + grosor), m);
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
        botones.Add(b);
        return b;
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
        if (!Abierta)
            return;
        if (dibujo == null || control == null || control.Cabeza == null)
        {
            Cerrar();
            return;
        }
        float dt = Time.deltaTime;
        Vector3 pos;
        Quaternion rot;
        CartelArriba.Pose(control.Cabeza, out pos, out rot);
        raiz.SetPositionAndRotation(pos, rot);

        AsegurarFantasmas();
        int cur = Actual;
        if (cur != capaMostrada)
        {
            if (capaMostrada >= 0)
                aparece = 0f;
            capaMostrada = cur;
            proximoRefresco = 0f;
        }
        if (Time.time >= proximoRefresco)
        {
            proximoRefresco = Time.time + 0.25f;
            RefrescarTarjeta(cur);
        }

        // Los fantasmas se acomodan (suave) en su lugar: adelante las capas más altas, atrás las más bajas.
        float a = 1f - Mathf.Exp(-10f * dt);
        for (int k = 0; k < fantasmas.Count; k++)
        {
            var f = fantasmas[k];
            bool existe = k < dibujo.capas.Count;
            f.s = Mathf.Lerp(f.s, k - cur, a);
            bool ver = existe && !(k == cur && Mathf.Abs(f.s) < 0.08f);
            if (f.t.gameObject.activeSelf != ver)
                f.t.gameObject.SetActive(ver);
            if (!ver)
                continue;
            float s = f.s;
            f.t.localPosition = new Vector3(0f, s > 0f ? s * SubeAdelante : s * BajaAtras, -s * PasoFondo);
            // Adelante: casi invisible, solo el borde. Atrás: un poco más visible. Más lejos = más suave.
            float lejos = 1f / (1f + 0.35f * Mathf.Max(0f, Mathf.Abs(s) - 1f));
            float oculta = dibujo.capas[k].visible ? 1f : 0.45f;
            PonerAlfa(f.relleno, new Color(0.9f, 0.93f, 1f, (s > 0f ? 0.08f : 0.2f) * lejos * oculta));
            PonerAlfa(f.borde, new Color(0.85f, 0.92f, 1f, (s > 0f ? 0.5f : 0.7f) * lejos * oculta));
        }

        // La tarjeta: crece un poquito al cambiar de capa; se mueve con el dedo mientras la empujas o atraes.
        aparece = Mathf.MoveTowards(aparece, 1f, dt * 5f);
        sacudida = Mathf.MoveTowards(sacudida, 0f, dt * 3f);
        float z = 0f;
        RevisarDedo(ref z);
        float x = Mathf.Sin(sacudida * 40f) * 0.006f * sacudida;
        tarjeta.localPosition = new Vector3(x, 0f, z);
        tarjeta.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, Suave(aparece));

        if (Time.time > avisoHasta && avisoHasta > 0f)
        {
            avisoHasta = 0f;
            Ayuda("Empuja: capa de adelante · Atrae hacia ti: capa de atrás");
        }
        if (Time.time - ultimoToque > CerrarSola)
            Cerrar();
    }

    static float Suave(float u)
    {
        u = Mathf.Clamp01(u);
        return u * u * (3f - 2f * u);
    }

    // El dedo: cerca = queda libre (sin bolita); en la parte del nombre se empuja o se atrae la tarjeta.
    void RevisarDedo(ref float z)
    {
        var der = control.Der;
        if (!der.valida)
        {
            empujando = false;
            return;
        }
        Vector3 p = raiz.InverseTransformPoint(der.indice);
        bool cerca = Mathf.Abs(p.x) < Ancho * 0.5f + 0.05f && p.y > -Alto * 0.5f - 0.06f && p.y < Alto * 0.5f + 0.05f
                     && p.z > -0.09f && p.z < 0.09f;
        if (cerca)
        {
            cercaCuadro = Time.frameCount;
            ultimoToque = Time.time;
        }
        // La parte de arriba de la tarjeta (la del nombre), lejos de la X.
        bool enZona = Mathf.Abs(p.x) < Ancho * 0.5f - 0.004f && p.y > FilaBotones + 0.013f && p.y < Alto * 0.5f
                      && Vector3.Distance(der.indice, btnCerrar.transform.position) > 0.02f;
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
            }
            return;
        }
        float dz = p.z - zInicio;
        bool fuera = Mathf.Abs(p.x) > Ancho * 0.5f + 0.03f || p.y < -Alto * 0.5f - 0.03f || p.y > Alto * 0.5f + 0.04f;
        if (fuera || Time.time - desdeEmpuje > 2.5f)
        {
            empujando = false;
            return;
        }
        z = Mathf.Clamp(dz, -Empujon, Empujon);
        if (dz > Empujon)
            Cambiar(1);
        else if (dz < -Empujon)
            Cambiar(-1);
    }

    // +1 = empujaste: la de adelante pasa a ser la actual. -1 = atrajiste: la de atrás pasa a ser la actual.
    void Cambiar(int direccion)
    {
        empujando = false;
        esperarSalir = true;
        ultimoToque = Time.time;
        int cur = Actual;
        int nuevo = cur + direccion;
        if (nuevo < 0 || nuevo >= dibujo.capas.Count)
        {
            sacudida = 1f;
            Ayuda(direccion > 0 ? "Esta ya es la capa de más adelante" : "Esta ya es la capa del fondo");
            avisoHasta = Time.time + 2f;
            control.SonidoBurbuja(tarjeta.position);
            return;
        }
        dibujo.SeleccionarCapa(nuevo);
        control.SonidoBurbuja(tarjeta.position);
        RefrescarTarjeta(nuevo);
    }

    void RefrescarTarjeta(int cur)
    {
        if (cur < 0 || cur >= dibujo.capas.Count)
            return;
        var c = dibujo.capas[cur];
        Idioma.Poner(nombre, c.nombre);
        btnVer.PonerTexto(c.visible ? "Ver" : "Oculta");
        btnVer.Marcar(!c.visible);
        int boceto = Mathf.Clamp(c.boceto, 0, 2);
        btnBoceto.PonerTexto("Boceto: " + (boceto == 0 ? "No" : boceto == 1 ? "Gris" : "Azul"));
        btnBoceto.Marcar(boceto > 0);
        btnPlano.PonerTexto(c.unido ? "Plano: unido" : "Plano: propio");
        btnPlano.Marcar(c.unido);
        bool liberar = dibujo.EncantadasEnCapa(cur) > 0 || (!c.unido && c.hayPlano);
        if (btnLiberar.gameObject.activeSelf != liberar)
        {
            btnLiberar.gameObject.SetActive(liberar);
            if (btnLiberar.etiqueta != null)
                btnLiberar.etiqueta.gameObject.SetActive(liberar);
        }
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

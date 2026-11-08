using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

// LA MESA DE ANIMACIÓN (v59): el "modo entrada". Al abrir la app (después del título) y al tocar Salir en el reloj,
// desaparece TODO lo de trabajar (reloj, paleta, capas, línea de tiempo, parlante, flechas...) y frente a ti queda
// solo una MESA DE ANIMACIÓN antigua: el tablero de madera inclinado, el DISCO con la luz por debajo, las dos reglas
// con pivotes, una HOJA con un boceto antiguo, los lápices y, arriba, tus 7 DIBUJOS RECIENTES como hojas.
//  - Toca una hoja de arriba: ENTRAS a ese dibujo (la hoja crece hacia ti hasta que estás adentro).
//  - Dibuja en la hoja del disco con el ÍNDICE DERECHO (como un lápiz: más pegado = más oscuro). El lápiz va
//    "suave" (un poco arrastrado, más liso). Pellizca el borde del disco y gíralo, como en la mesa de verdad.
//  - "Hoja de tinta": pone una hoja encima (como papel de calcar) y lo que dibujas es TINTA.
//  - "✦ Entrar": entras a tu boceto: se crea un dibujo nuevo con tu lápiz en la capa 1 (lápiz, 2D) y tu tinta
//    en la capa 2 (líneas de verdad, editables). Ahí sigues entintando con todo lo de siempre.
//  - "Hoja nueva": una hoja en blanco. Si sales sin entrar, tu boceto se queda en la mesa (también al cerrar la app).
//  - "Cerrar app" (abajo a la derecha, dos toques).
public class MesaEntrada : MonoBehaviour
{
    static MesaEntrada instancia;
    public static bool Activa => instancia != null && instancia.activa;

    const int Recientes = 7;
    const int AnchoTex = 800, AltoTex = 600;
    const float AnchoHoja = 0.30f, AltoHoja = 0.225f;
    const float RadioDisco = 0.2f;
    const float Inclinacion = 40f;        // grados desde lo horizontal (como un tablero de dibujo)
    const float AlcanceLapiz = 0.012f;    // metros: el dedo dibuja a menos de esto del papel
    static readonly Color32 ColorPapel = new Color32(246, 242, 230, 255);
    static readonly Color ColorGrafito = new Color(0.22f, 0.22f, 0.25f);
    static readonly Color ColorAzul = new Color(0.35f, 0.55f, 0.95f);
    static readonly Color ColorRojo = new Color(0.85f, 0.3f, 0.22f);

    bool activa, armada, animando, arrancado, tutorialVisto;
    ControlManos control;
    Dibujo dibujo;
    Transform raiz, giratorio, hoja, hojaTinta;
    Texture2D texHoja, texTinta;
    Color32[] pxHoja, pxBase, pxTinta;
    bool sucioHoja, sucioTinta, tintaPuesta, conBocetoAntiguo = true;
    readonly List<BotonTocable> botonesRecientes = new List<BotonTocable>();
    readonly List<Renderer> minis = new List<Renderer>();
    readonly List<TextMeshPro> nombresRecientes = new List<TextMeshPro>();
    readonly List<string> rutasRecientes = new List<string>();
    readonly List<Texture2D> texturasMini = new List<Texture2D>();
    BotonTocable btnTinta, btnCerrarApp;
    TextMeshPro textoAyuda;
    float cerrarAppHasta;
    Material matImagen, matTransparente, matColor;

    // Lo que dibujaste (en la hoja: u, v de 0 a 1 y la presión), para pasarlo al dibujo al entrar.
    [System.Serializable]
    class Linea { public List<Vector3> p = new List<Vector3>(); }
    [System.Serializable]
    class Guardado
    {
        public bool antiguo = true;
        public List<Linea> lapiz = new List<Linea>();
        public List<Linea> tinta = new List<Linea>();
    }
    Guardado boceto = new Guardado();
    Linea actual;
    bool actualTinta;

    // El filtro del lápiz ("Suave": un poco arrastrado, más liso).
    Vector3 filtrado, velocidad, crudoPrevio;
    bool tieneFiltro;

    // Girar el disco.
    int manoGiro = -1;   // 0 izquierda, 1 derecha
    float anguloAntes;

    void Start()
    {
        // Se mira AL ABRIR: si es la primera vez, va el tutorial (y al terminarlo te quedas trabajando).
        tutorialVisto = PlayerPrefs.GetInt("jcartoons_tutorial_visto", 0) == 1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia != null)
            return;
        instancia = new GameObject("MesaEntrada").AddComponent<MesaEntrada>();
    }

    // ---------- Entrar y salir del modo ----------

    // El botón Salir del reloj: guarda el dibujo y vuelve a la mesa.
    public static void Salir()
    {
        if (instancia != null)
            instancia.Mostrar();
    }

    // La voz ("entrar"): entra a tu boceto de la mesa.
    public static void EntrarVoz()
    {
        if (Activa && !instancia.animando)
            instancia.EntrarABoceto();
    }

    void Update()
    {
        if (control == null)
            control = ControlManos.Instancia;
        if (dibujo == null && control != null)
            dibujo = control.dibujo;
        if (control == null || dibujo == null || control.Cabeza == null)
            return;
        // Al abrir la app: después del título (y nunca la primera vez: ahí va el tutorial).
        if (!arrancado)
        {
            if (!Tutorial.ListoParaMesa)
                return;
            arrancado = true;
            if (tutorialVisto && !Tutorial.EnCurso)
                Mostrar();
            return;
        }
        if (!activa)
            return;
        control.Ocupado = true;
        if (animando)
            return;
        Dibujar();
        GirarDisco();
        if (sucioHoja)
        {
            sucioHoja = false;
            texHoja.SetPixels32(pxHoja);
            texHoja.Apply(false);
        }
        if (sucioTinta)
        {
            sucioTinta = false;
            texTinta.SetPixels32(pxTinta);
            texTinta.Apply(false);
        }
        if (btnCerrarApp != null && cerrarAppHasta > 0f && Time.time > cerrarAppHasta)
        {
            cerrarAppHasta = 0f;
            btnCerrarApp.etiqueta.text = Idioma.T("Cerrar app");
        }
    }

    void LateUpdate()
    {
        if (activa && control != null)
            control.Ocupado = true;
    }

    void Mostrar()
    {
        if (activa || animando)
            return;
        if (Titere.Activo)
        {
            dibujo.Mensaje("Primero suelta los personajes");
            return;
        }
        if (Funcion.Activa)
            Funcion.Alternar();
        // Guarda lo que estabas haciendo y deja la hoja en blanco (como Salir de siempre).
        if (dibujo.HayArchivoAbierto)
            dibujo.ArchivoNuevo();
        CerrarPaneles();
        if (!Armar())
            return;
        activa = true;
        control.Ocupado = true;
        Colocar();
        LlenarRecientes();
        StartCoroutine(Aparecer());
    }

    // Todo lo de trabajar se cierra (paneles, ventanas, selección).
    void CerrarPaneles()
    {
        control.CerrarMenu();
        if (control.panelArriba != null)
            control.panelArriba.GuardarMesa();
        CapasProfundidad.CerrarSiAbierta();
        PanelLapiz.Cerrar();
        PanelPlastilina.Cerrar();
        PanelPropiedades.Cerrar();
        ImportadorSvg.Cerrar();
        SelectorFondos.Cerrar();
        CabezaInvisible.CerrarSiAbierta();
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (nav != null && nav.Abierto)
            nav.Cerrar();
        var buscador = FindFirstObjectByType<BuscadorImagenes>(FindObjectsInactive.Include);
        if (buscador != null && buscador.Abierto)
            buscador.Cerrar();
        dibujo.Seleccionar(null);
    }

    // Frente a ti, un poco abajo, inclinada hacia ti (como un tablero de dibujo).
    void Colocar()
    {
        var cab = control.Cabeza;
        Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        float a = Inclinacion * Mathf.Deg2Rad;
        Vector3 normal = Vector3.up * Mathf.Cos(a) - adelante * Mathf.Sin(a);       // hacia arriba y hacia ti
        Vector3 subeTablero = adelante * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a);
        raiz.SetPositionAndRotation(cab.position + adelante * 0.5f - Vector3.up * 0.3f, Quaternion.LookRotation(-normal, subeTablero));
    }

    IEnumerator Aparecer()
    {
        animando = true;
        raiz.gameObject.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            raiz.localScale = Vector3.one * Mathf.Lerp(0.05f, 1f, e);
            yield return null;
        }
        raiz.localScale = Vector3.one;
        animando = false;
    }

    // ---------- ENTRAR (la magia: la hoja crece hacia ti y quedas adentro) ----------

    void EntrarABoceto()
    {
        StartCoroutine(Entrar(-1));
    }

    void AbrirReciente(int i)
    {
        if (animando || i < 0 || i >= rutasRecientes.Count)
            return;
        StartCoroutine(Entrar(i));
    }

    IEnumerator Entrar(int reciente)
    {
        animando = true;
        control.SonidoBurbuja(hoja.position);
        // La hoja que vuela: la del disco (tu boceto) o la del dibujo reciente.
        Transform vuela;
        if (reciente < 0)
        {
            vuela = Instantiate(hoja.gameObject).transform;
            vuela.SetPositionAndRotation(hoja.position, hoja.rotation);
            vuela.localScale = hoja.lossyScale;
        }
        else
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = minis[reciente].sharedMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            vuela = go.transform;
            var m = minis[reciente].transform;
            vuela.SetPositionAndRotation(m.position, m.rotation);
            vuela.localScale = m.lossyScale;
        }
        var cab = control.Cabeza;
        Vector3 p0 = vuela.position, s0 = vuela.localScale;
        Quaternion r0 = vuela.rotation;
        bool hecho = false;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            // Primero sube a tu vista; después crece hasta que lo atraviesas.
            Vector3 frente = cab.position + cab.forward * Mathf.Lerp(0.45f, 0.05f, Mathf.Clamp01((e - 0.45f) / 0.55f));
            Quaternion mira = Quaternion.LookRotation(cab.forward, Vector3.up);
            float k = Mathf.Clamp01(e / 0.45f);
            vuela.position = Vector3.Lerp(p0, frente, k);
            vuela.rotation = Quaternion.Slerp(r0, mira, k);
            float crece = Mathf.Lerp(1f, 1.6f, k) * Mathf.Lerp(1f, 9f, Mathf.Clamp01((e - 0.45f) / 0.55f));
            vuela.localScale = new Vector3(s0.x * crece, s0.y * crece, 1f);
            raiz.localScale = Vector3.one * Mathf.Lerp(1f, 0.02f, Mathf.Clamp01(e / 0.6f));
            if (!hecho && e > 0.6f)
            {
                hecho = true;
                raiz.gameObject.SetActive(false);
                if (reciente < 0)
                    PasarBocetoAlDibujo();
                else
                    dibujo.AbrirArchivo(rutasRecientes[reciente]);
            }
            yield return null;
        }
        if (!hecho)
        {
            raiz.gameObject.SetActive(false);
            if (reciente < 0)
                PasarBocetoAlDibujo();
            else
                dibujo.AbrirArchivo(rutasRecientes[reciente]);
        }
        Destroy(vuela.gameObject);
        raiz.localScale = Vector3.one;
        activa = false;
        animando = false;
        control.Ocupado = false;
    }

    // Tu boceto pasa a un dibujo nuevo: el lápiz en la capa 1 (lápiz 2D) y la tinta en la capa 2 (líneas de verdad).
    void PasarBocetoAlDibujo()
    {
        bool hayLapiz = boceto.lapiz.Count > 0, hayTinta = boceto.tinta.Count > 0;
        var cab = control.Cabeza;
        Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized;
        Vector3 centro = cab.position + adelante * 0.7f;
        const float ancho = 0.9f, alto = ancho * AltoHoja / AnchoHoja;
        var raizD = dibujo.transform;
        System.Func<Vector3, Vector3> mundo = q => centro + derecha * ((q.x - 0.5f) * ancho) + Vector3.up * ((q.y - 0.5f) * alto);
        if (hayLapiz)
        {
            dibujo.SeleccionarCapa(0);
            dibujo.CapaActual.nombre = "Lápiz";
            if (dibujo.temblor != null)
                dibujo.temblor.PonerBoceto(true);
            dibujo.PonerPlano(true);
            dibujo.DefinirPlano(raizD.InverseTransformPoint(centro), adelante);
            var hojas = dibujo.GetComponent<HojasLapiz>();
            if (hojas == null)
                hojas = FindFirstObjectByType<HojasLapiz>();
            if (hojas != null)
            {
                float radio = hojas.radioLapiz * 1.6f / dibujo.EscalaMundo;
                foreach (var l in boceto.lapiz)
                {
                    var t = new TrazoLapiz { capa = dibujo.capaActual, borra = false, radio = radio };
                    foreach (var q in l.p)
                    {
                        Vector3 p = dibujo.ProyectarEnPlano(raizD.InverseTransformPoint(mundo(q)));
                        t.puntos.Add(new Vector4(p.x, p.y, p.z, Mathf.Clamp01(q.z)));
                    }
                    if (t.puntos.Count > 0)
                        hojas.AgregarHecho(t);
                }
            }
        }
        if (hayLapiz || hayTinta)
        {
            // La tinta va en su propia capa, encima del lápiz (en un dibujo nuevo la otra capa es la de la cámara).
            int capaTinta = hayLapiz ? dibujo.AgregarCapa("Tinta", false) : -1;
            if (capaTinta < 0)
            {
                dibujo.SeleccionarCapa(0);
                dibujo.CapaActual.nombre = "Tinta";
            }
            if (dibujo.temblor != null)
                dibujo.temblor.PonerBoceto(false);
            dibujo.PonerPlano(true);
            Vector3 centroTinta = centro - adelante * 0.002f;
            Vector3 planoLocal = raizD.InverseTransformPoint(centroTinta);
            dibujo.DefinirPlano(planoLocal, adelante);
            Vector3 normalLocal = raizD.InverseTransformDirection(adelante).normalized;
            foreach (var l in boceto.tinta)
            {
                var puntos = new List<Vector3>();
                foreach (var q in l.p)
                    puntos.Add(raizD.InverseTransformPoint(mundo(q) - adelante * 0.002f));
                var simples = Simplificar(puntos, 0.0015f / dibujo.EscalaMundo);
                if (simples.Count < 2)
                    continue;
                var d = new DatosTrazo { color = Color.black, enHoja = true, hojaPunto = planoLocal, hojaNormal = normalLocal };
                foreach (var p in simples)
                {
                    d.nodos.Add(p);
                    d.asaEntrada.Add(Vector3.zero);
                    d.asaSalida.Add(Vector3.zero);
                    d.asaManual.Add(false);
                    d.grosorNodo.Add(1f);
                }
                dibujo.AgregarTrazo(d);
            }
            dibujo.HayCambios = true;
            dibujo.Mensaje(hayLapiz ? "Tu boceto de la mesa: el lápiz en su capa; entinta en la capa Tinta" : "Tu tinta de la mesa ya está aquí");
        }
        else
        {
            dibujo.Mensaje("Hoja nueva");
        }
        // La hoja de la mesa queda lista para otro boceto.
        boceto = new Guardado();
        tintaPuesta = false;
        RehacerHoja();
        GuardarBoceto();
    }

    // Douglas-Peucker: menos puntos (los nodos de la tinta), la misma forma.
    static List<Vector3> Simplificar(List<Vector3> p, float tolerancia)
    {
        var r = new List<Vector3>();
        if (p.Count < 3)
        {
            r.AddRange(p);
            return r;
        }
        var marca = new bool[p.Count];
        marca[0] = marca[p.Count - 1] = true;
        var pila = new Stack<Vector2Int>();
        pila.Push(new Vector2Int(0, p.Count - 1));
        while (pila.Count > 0)
        {
            var s = pila.Pop();
            float mejor = 0f;
            int idx = -1;
            for (int i = s.x + 1; i < s.y; i++)
            {
                float d = DistanciaASegmento(p[i], p[s.x], p[s.y]);
                if (d > mejor)
                {
                    mejor = d;
                    idx = i;
                }
            }
            if (idx >= 0 && mejor > tolerancia)
            {
                marca[idx] = true;
                pila.Push(new Vector2Int(s.x, idx));
                pila.Push(new Vector2Int(idx, s.y));
            }
        }
        for (int i = 0; i < p.Count; i++)
            if (marca[i])
                r.Add(p[i]);
        return r;
    }

    static float DistanciaASegmento(Vector3 q, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = ab.sqrMagnitude > 1e-12f ? Mathf.Clamp01(Vector3.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector3.Distance(q, a + ab * t);
    }

    // ---------- Dibujar en la hoja (índice derecho) ----------

    void Dibujar()
    {
        var der = control.Der;
        if (!der.valida || der.pellizco)
        {
            TerminarLinea();
            return;
        }
        Vector3 dedo = Filtrar(der.indice);
        Transform plano = tintaPuesta ? hojaTinta : hoja;
        Vector3 l = plano.InverseTransformPoint(dedo);
        float distancia = -l.z;   // la hoja mira hacia ti (su -Z)
        bool dentro = Mathf.Abs(l.x) < 0.5f && Mathf.Abs(l.y) < 0.5f;
        bool toca = dentro && distancia < (actual != null ? AlcanceLapiz * 1.5f : AlcanceLapiz) && distancia > -0.03f;
        if (!toca)
        {
            TerminarLinea();
            return;
        }
        float presion = Mathf.Clamp01(1f - Mathf.Max(0f, distancia) / AlcanceLapiz);
        var q = new Vector3(l.x + 0.5f, l.y + 0.5f, Mathf.Max(0.15f, presion));
        if (actual == null || actualTinta != tintaPuesta)
        {
            TerminarLinea();
            actual = new Linea();
            actualTinta = tintaPuesta;
            (tintaPuesta ? boceto.tinta : boceto.lapiz).Add(actual);
        }
        var lista = actual.p;
        if (lista.Count > 0)
        {
            Vector3 ultimo = lista[lista.Count - 1];
            if (Vector2.Distance(ultimo, q) < 0.0015f)
                return;
            Trazar(ultimo, q, actualTinta);
        }
        else
        {
            Trazar(q, q, actualTinta);
        }
        lista.Add(q);
    }

    void TerminarLinea()
    {
        if (actual == null)
            return;
        if (actual.p.Count == 0)
            (actualTinta ? boceto.tinta : boceto.lapiz).Remove(actual);
        actual = null;
        GuardarBoceto();
    }

    static float Alfa(float dt, float corte)
    {
        float tau = 1f / (2f * Mathf.PI * Mathf.Max(0.01f, corte));
        return 1f / (1f + tau / dt);
    }

    Vector3 Filtrar(Vector3 crudo)
    {
        float dt = Mathf.Max(1e-4f, Time.deltaTime);
        if (!tieneFiltro)
        {
            tieneFiltro = true;
            filtrado = crudo;
            crudoPrevio = crudo;
            velocidad = Vector3.zero;
            return crudo;
        }
        Vector3 v = (crudo - crudoPrevio) / dt;
        crudoPrevio = crudo;
        velocidad = Vector3.Lerp(velocidad, v, Alfa(dt, 1f));
        float corte = 1.2f + 12f * velocidad.magnitude;
        filtrado = Vector3.Lerp(filtrado, crudo, Alfa(dt, corte));
        return filtrado;
    }

    // Sellos entre dos puntos de la hoja (u, v, presión).
    void Trazar(Vector3 a, Vector3 b, bool tinta)
    {
        float pxA = a.x * AnchoTex, pyA = a.y * AltoTex, pxB = b.x * AnchoTex, pyB = b.y * AltoTex;
        float largo = Mathf.Sqrt((pxB - pxA) * (pxB - pxA) + (pyB - pyA) * (pyB - pyA));
        int pasos = Mathf.Max(1, Mathf.CeilToInt(largo / 0.8f));
        for (int i = 0; i <= pasos; i++)
        {
            float t = i / (float)pasos;
            float pr = Mathf.Lerp(a.z, b.z, t);
            if (tinta)
                Sello(pxTinta, Mathf.Lerp(pxA, pxB, t), Mathf.Lerp(pyA, pyB, t), 1.9f, new Color(0.05f, 0.05f, 0.07f), 0.9f, false);
            else
                Sello(pxHoja, Mathf.Lerp(pxA, pxB, t), Mathf.Lerp(pyA, pyB, t), 1.1f + 1.4f * pr, ColorGrafito, 0.12f + 0.3f * pr, true);
        }
        if (tinta)
            sucioTinta = true;
        else
            sucioHoja = true;
    }

    // Un sello redondo y suave. "grano": como grafito sobre papel (con huequitos).
    static void Sello(Color32[] px, float cx, float cy, float radio, Color color, float fuerza, bool grano)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radio)), x1 = Mathf.Min(AnchoTex - 1, Mathf.CeilToInt(cx + radio));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radio)), y1 = Mathf.Min(AltoTex - 1, Mathf.CeilToInt(cy + radio));
        float r2 = radio * radio;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy, d2 = dx * dx + dy * dy;
                if (d2 > r2)
                    continue;
                float f = 1f - d2 / r2;
                float a = fuerza * f;
                if (grano)
                    a *= 0.55f + 0.45f * Hash(x, y);
                int i = y * AnchoTex + x;
                Color32 c = px[i];
                px[i] = new Color32((byte)Mathf.Lerp(c.r, color.r * 255f, a), (byte)Mathf.Lerp(c.g, color.g * 255f, a),
                                    (byte)Mathf.Lerp(c.b, color.b * 255f, a), (byte)Mathf.Max(c.a, Mathf.Clamp(a * 255f * 1.4f, 0f, 255f)));
            }
    }

    static float Hash(int x, int y)
    {
        uint h = (uint)(x * 374761393 + y * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h & 0xFFFF) / 65535f;
    }

    // ---------- Girar el disco (pellizco en el disco, fuera de la hoja) ----------

    void GirarDisco()
    {
        if (manoGiro >= 0)
        {
            var m = manoGiro == 0 ? control.Izq : control.Der;
            if (!m.valida || !m.pellizco)
            {
                manoGiro = -1;
                return;
            }
            float ang = AnguloEnDisco(m.PuntoPellizco);
            giratorio.localRotation *= Quaternion.Euler(0f, 0f, Mathf.DeltaAngle(anguloAntes, ang));
            anguloAntes = ang;
            return;
        }
        for (int k = 0; k < 2; k++)
        {
            var m = k == 0 ? control.Izq : control.Der;
            if (!m.valida || !m.empezoPellizco || BotonTocable.DedoCerca)
                continue;
            Vector3 l = raiz.InverseTransformPoint(m.PuntoPellizco) - giratorio.localPosition;
            if (new Vector2(l.x, l.y).magnitude < RadioDisco + 0.03f && Mathf.Abs(l.z) < 0.06f)
            {
                manoGiro = k;
                anguloAntes = AnguloEnDisco(m.PuntoPellizco);
                return;
            }
        }
    }

    float AnguloEnDisco(Vector3 mundo)
    {
        Vector3 l = raiz.InverseTransformPoint(mundo) - giratorio.localPosition;
        return Mathf.Atan2(l.y, l.x) * Mathf.Rad2Deg;
    }

    // ---------- Botones ----------

    void HojaNueva()
    {
        boceto = new Guardado { antiguo = false };
        tintaPuesta = false;
        RehacerHoja();
        GuardarBoceto();
        control.SonidoBurbuja(hoja.position);
    }

    void AlternarTinta()
    {
        tintaPuesta = !tintaPuesta;
        TerminarLinea();
        hojaTinta.gameObject.SetActive(tintaPuesta);
        btnTinta.etiqueta.text = Idioma.T(tintaPuesta ? "Quitar hoja de tinta" : "Hoja de tinta");
        btnTinta.Marcar(tintaPuesta);
        control.SonidoBurbuja(hoja.position);
    }

    void CerrarApp()
    {
        if (cerrarAppHasta <= 0f)
        {
            cerrarAppHasta = Time.time + 3f;
            btnCerrarApp.etiqueta.text = Idioma.T("¿Cerrar? Toca otra vez");
            return;
        }
        GuardarBoceto();
        Application.Quit();
    }

    // ---------- Las hojas recientes ----------

    void LlenarRecientes()
    {
        rutasRecientes.Clear();
        foreach (var t in texturasMini)
            if (t != null)
                Destroy(t);
        texturasMini.Clear();
        rutasRecientes.AddRange(dibujo.ListaArchivos(Recientes));
        for (int i = 0; i < Recientes; i++)
        {
            bool hay = i < rutasRecientes.Count;
            botonesRecientes[i].gameObject.SetActive(hay);
            minis[i].gameObject.SetActive(hay);
            nombresRecientes[i].gameObject.SetActive(hay);
            if (!hay)
                continue;
            nombresRecientes[i].text = Path.GetFileNameWithoutExtension(rutasRecientes[i]);
            Texture2D tex = null;
            try
            {
                string mini = dibujo.RutaMiniatura(rutasRecientes[i]);
                if (File.Exists(mini))
                {
                    tex = new Texture2D(2, 2);
                    if (!tex.LoadImage(File.ReadAllBytes(mini)))
                    {
                        Destroy(tex);
                        tex = null;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TrazoVR: miniatura: " + e.Message);
            }
            if (tex != null)
                texturasMini.Add(tex);
            PonerTextura(minis[i].sharedMaterial, tex != null ? tex : Texture2D.whiteTexture);
        }
        textoAyuda.text = Idioma.T(rutasRecientes.Count > 0
            ? "Toca una hoja de arriba para abrirla · dibuja con el índice en la hoja · pellizca el disco para girarlo · ✦ Entrar = a tu boceto"
            : "Dibuja con el índice en la hoja · pellizca el disco para girarlo · ✦ Entrar = a tu boceto");
    }

    static void PonerTextura(Material m, Texture t)
    {
        if (m == null)
            return;
        if (m.HasProperty("_BaseMap"))
            m.SetTexture("_BaseMap", t);
        if (m.HasProperty("_MainTex"))
            m.SetTexture("_MainTex", t);
    }

    // ---------- Armar la mesa (una vez) ----------

    bool Armar()
    {
        if (armada)
            return true;
        var refs = FindFirstObjectByType<Referencias>(FindObjectsInactive.Include);
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (refs == null || refs.materialImagen == null || nav == null)
            return false;
        matImagen = refs.materialImagen;
        matTransparente = refs.materialImagenTransparente != null ? refs.materialImagenTransparente : refs.materialImagen;
        matColor = nav.materialBoton;
        Material normal = nav.materialBoton, marcado = nav.materialBotonMarcado;
        raiz = new GameObject("Mesa").transform;
        raiz.SetParent(transform, false);

        // El tablero de madera (su cara de arriba en z = 0; lo de encima va hacia -z).
        Pieza(PrimitiveType.Cube, new Vector3(0f, 0f, 0.012f), new Vector3(0.84f, 0.6f, 0.024f), Quaternion.identity, Textura(Madera()));
        // La orilla de aluminio de abajo (para que no se caigan los lápices).
        Pieza(PrimitiveType.Cube, new Vector3(0f, -0.302f, -0.006f), new Vector3(0.84f, 0.012f, 0.03f), Quaternion.identity, Pintura(new Color(0.75f, 0.76f, 0.78f)));

        // El disco (gira) con la luz por debajo de la hoja.
        giratorio = new GameObject("Disco").transform;
        giratorio.SetParent(raiz, false);
        giratorio.localPosition = new Vector3(0f, -0.065f, 0f);
        var giroDisco = Quaternion.Euler(90f, 0f, 0f);
        Pieza(PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.002f), new Vector3(RadioDisco * 2f, 0.002f, RadioDisco * 2f), giroDisco,
              Pintura(new Color(0.16f, 0.16f, 0.18f)), giratorio);
        Pieza(PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.0042f), new Vector3(RadioDisco * 1.62f, 0.0003f, RadioDisco * 1.62f), giroDisco,
              Pintura(new Color(1f, 0.99f, 0.94f)), giratorio);
        // La hoja (lápiz) y la hoja de tinta encima.
        texHoja = new Texture2D(AnchoTex, AltoTex, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        texTinta = new Texture2D(AnchoTex, AltoTex, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        pxHoja = new Color32[AnchoTex * AltoTex];
        pxBase = new Color32[AnchoTex * AltoTex];
        pxTinta = new Color32[AnchoTex * AltoTex];
        var mHoja = new Material(matImagen);
        PonerTextura(mHoja, texHoja);
        hoja = Pieza(PrimitiveType.Quad, new Vector3(0f, 0f, -0.0055f), new Vector3(AnchoHoja, AltoHoja, 1f), Quaternion.identity, mHoja, giratorio);
        var mTinta = new Material(matTransparente);
        if (mTinta.HasProperty("_BaseColor"))
            mTinta.SetColor("_BaseColor", UnityEngine.Color.white);
        PonerTextura(mTinta, texTinta);
        hojaTinta = Pieza(PrimitiveType.Quad, new Vector3(0f, 0f, -0.0068f), new Vector3(AnchoHoja, AltoHoja, 1f), Quaternion.identity, mTinta, giratorio);
        hojaTinta.gameObject.SetActive(false);
        // Las reglas negras con pivotes (sujetan la hoja).
        var negro = Pintura(new Color(0.08f, 0.08f, 0.09f));
        var metal = Pintura(new Color(0.55f, 0.56f, 0.6f));
        for (int s = -1; s <= 1; s += 2)
        {
            float y = s * (AltoHoja * 0.5f + 0.008f);
            Pieza(PrimitiveType.Cube, new Vector3(0f, y, -0.008f), new Vector3(AnchoHoja + 0.05f, 0.022f, 0.006f), Quaternion.identity, negro, giratorio);
            for (int k = -1; k <= 1; k++)
                Pieza(PrimitiveType.Cylinder, new Vector3(k * 0.11f, y, -0.0115f), new Vector3(0.007f, 0.0012f, 0.007f), giroDisco, metal, giratorio);
        }

        // Los lápices y el clip de resorte (arriba a la derecha).
        Pieza(PrimitiveType.Cube, new Vector3(0.33f, 0.07f, -0.006f), new Vector3(0.1f, 0.016f, 0.01f), Quaternion.identity, metal);
        for (int k = 0; k < 2; k++)
        {
            float x = 0.31f + k * 0.04f;
            Pieza(PrimitiveType.Cylinder, new Vector3(x, 0.0f, -0.012f), new Vector3(0.007f, 0.07f, 0.007f), Quaternion.identity, negro);
            Pieza(PrimitiveType.Cylinder, new Vector3(x, -0.075f, -0.012f), new Vector3(0.006f, 0.006f, 0.006f), Quaternion.identity,
                  Pintura(new Color(0.85f, 0.72f, 0.52f)));
        }

        // Arriba: tus dibujos recientes (hojas con su miniatura y su nombre).
        for (int i = 0; i < Recientes; i++)
        {
            float x = -0.345f + i * 0.115f;
            int k = i;
            var b = PanelTutoriales.Boton(raiz, "", new Vector3(x, 0.235f, -0.004f), new Vector2(0.1f, 0.075f), normal, marcado);
            b.alTocar.AddListener(() => AbrirReciente(k));
            botonesRecientes.Add(b);
            var mini = Pieza(PrimitiveType.Quad, new Vector3(x, 0.237f, -0.0085f), new Vector3(0.09f, 0.064f, 1f), Quaternion.identity, new Material(matImagen));
            minis.Add(mini.GetComponent<Renderer>());
            var n = PanelTutoriales.Texto(raiz, "", new Vector3(x, 0.188f, -0.002f), new Vector2(0.105f, 0.012f), 0.09f, null);
            n.color = new Color(0.22f, 0.13f, 0.06f);
            nombresRecientes.Add(n);
        }

        // Botones: entrar, hoja nueva, hoja de tinta, cerrar app.
        var entrar = PanelTutoriales.Boton(raiz, "", new Vector3(0f, -0.272f, -0.004f), new Vector2(0.14f, 0.026f), marcado, normal);
        Idioma.Poner(entrar.etiqueta, "✦ Entrar");
        entrar.etiqueta.color = UnityEngine.Color.white;
        entrar.textoNormal = UnityEngine.Color.white;
        entrar.alTocar.AddListener(() => { if (!animando) EntrarABoceto(); });
        var nueva = PanelTutoriales.Boton(raiz, "", new Vector3(0.33f, -0.16f, -0.004f), new Vector2(0.11f, 0.03f), normal, marcado);
        Idioma.Poner(nueva.etiqueta, "Hoja nueva");
        nueva.alTocar.AddListener(HojaNueva);
        btnTinta = PanelTutoriales.Boton(raiz, "", new Vector3(-0.33f, -0.16f, -0.004f), new Vector2(0.12f, 0.03f), normal, marcado);
        btnTinta.etiqueta.text = Idioma.T("Hoja de tinta");
        btnTinta.alTocar.AddListener(AlternarTinta);
        btnCerrarApp = PanelTutoriales.Boton(raiz, "", new Vector3(0.355f, -0.272f, -0.004f), new Vector2(0.09f, 0.02f), normal, marcado);
        btnCerrarApp.etiqueta.text = Idioma.T("Cerrar app");
        btnCerrarApp.alTocar.AddListener(CerrarApp);
        textoAyuda = PanelTutoriales.Texto(raiz, "", new Vector3(-0.25f, -0.272f, -0.002f), new Vector2(0.28f, 0.02f), 0.07f, null);
        textoAyuda.color = new Color(0.25f, 0.15f, 0.07f);
        var titulo = PanelTutoriales.Texto(raiz, "JCartoons", new Vector3(-0.33f, 0.15f, -0.002f), new Vector2(0.14f, 0.02f), 0.16f, null);
        titulo.color = new Color(0.35f, 0.2f, 0.08f);
        titulo.fontStyle = FontStyles.Italic | FontStyles.Bold;

        CargarBoceto();
        RehacerHoja();
        raiz.gameObject.SetActive(false);
        armada = true;
        return true;
    }

    Transform Pieza(PrimitiveType tipo, Vector3 pos, Vector3 escala, Quaternion giro, Material m, Transform padre = null)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(padre != null ? padre : raiz, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = giro;
        go.transform.localScale = escala;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    Material Pintura(Color c)
    {
        var m = new Material(matColor);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        return m;
    }

    Material Textura(Texture2D t)
    {
        var m = new Material(matImagen);
        PonerTextura(m, t);
        return m;
    }

    // Vetas de madera clara (como el tablero de la foto).
    static Texture2D Madera()
    {
        const int w = 256, h = 192;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        var claro = new Color(0.86f, 0.6f, 0.34f);
        var oscuro = new Color(0.74f, 0.47f, 0.24f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float veta = Mathf.PerlinNoise(x * 0.012f, y * 0.11f) * 0.7f + Mathf.PerlinNoise(x * 0.05f, y * 0.4f) * 0.3f;
                float anillo = 0.5f + 0.5f * Mathf.Sin(veta * 18f);
                px[y * w + x] = UnityEngine.Color.Lerp(oscuro, claro, 0.35f + 0.65f * anillo * anillo);
            }
        t.SetPixels32(px);
        t.Apply(true);
        return t;
    }

    // ---------- La hoja: papel, perforaciones, boceto antiguo y lo tuyo ----------

    void RehacerHoja()
    {
        // El papel con sus tres perforaciones arriba.
        for (int i = 0; i < pxBase.Length; i++)
            pxBase[i] = ColorPapel;
        foreach (float hx in new[] { 0.3f, 0.5f, 0.7f })
            Sello(pxBase, hx * AnchoTex, 0.93f * AltoTex, 9f, new Color(0.62f, 0.62f, 0.6f), 1f, false);
        if (boceto.antiguo)
            BocetoAntiguo(pxBase);
        System.Array.Copy(pxBase, pxHoja, pxBase.Length);
        for (int i = 0; i < pxTinta.Length; i++)
            pxTinta[i] = new Color32(255, 255, 255, 38);   // papel de calcar: deja ver el lápiz, un poco más claro
        foreach (var l in boceto.lapiz)
            RepintarLinea(l, false);
        foreach (var l in boceto.tinta)
            RepintarLinea(l, true);
        hojaTinta.gameObject.SetActive(tintaPuesta);
        if (btnTinta != null)
        {
            btnTinta.etiqueta.text = Idioma.T(tintaPuesta ? "Quitar hoja de tinta" : "Hoja de tinta");
            btnTinta.Marcar(tintaPuesta);
        }
        sucioHoja = sucioTinta = true;
    }

    void RepintarLinea(Linea l, bool tinta)
    {
        if (l.p.Count == 0)
            return;
        Trazar(l.p[0], l.p[0], tinta);
        for (int i = 1; i < l.p.Count; i++)
            Trazar(l.p[i - 1], l.p[i], tinta);
    }

    // Un personaje original, al estilo de los bocetos de animación antiguos: construcción en azul y línea en rojo.
    static void BocetoAntiguo(Color32[] px)
    {
        // Azul: la línea de acción, las formas de construcción y el esqueleto.
        Curva(px, ColorAzul, 0.35f, new Vector2(0.43f, 0.12f), new Vector2(0.53f, 0.5f), new Vector2(0.48f, 0.88f));
        Ovalo(px, ColorAzul, 0.3f, new Vector2(0.5f, 0.66f), 0.075f, 0.1f);
        Ovalo(px, ColorAzul, 0.3f, new Vector2(0.5f, 0.42f), 0.065f, 0.12f);
        Poli(px, ColorAzul, 0.3f, new Vector2(0.45f, 0.5f), new Vector2(0.35f, 0.58f), new Vector2(0.26f, 0.71f));
        Poli(px, ColorAzul, 0.3f, new Vector2(0.55f, 0.5f), new Vector2(0.65f, 0.58f), new Vector2(0.74f, 0.71f));
        Poli(px, ColorAzul, 0.3f, new Vector2(0.47f, 0.32f), new Vector2(0.4f, 0.22f), new Vector2(0.44f, 0.12f));
        Poli(px, ColorAzul, 0.3f, new Vector2(0.53f, 0.32f), new Vector2(0.61f, 0.24f), new Vector2(0.59f, 0.12f));
        // Rojo: la línea "limpia".
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.5f, 0.66f), 0.07f, 0.095f);
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.42f, 0.73f), 0.025f, 0.035f);
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.58f, 0.73f), 0.025f, 0.035f);
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.475f, 0.68f), 0.013f, 0.028f);
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.525f, 0.68f), 0.013f, 0.028f);
        Ovalo(px, ColorRojo, 0.7f, new Vector2(0.48f, 0.675f), 0.004f, 0.009f);
        Ovalo(px, ColorRojo, 0.7f, new Vector2(0.53f, 0.675f), 0.004f, 0.009f);
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.5f, 0.645f), 0.012f, 0.01f);
        Curva(px, ColorRojo, 0.55f, new Vector2(0.455f, 0.615f), new Vector2(0.5f, 0.565f), new Vector2(0.545f, 0.615f));
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.5f, 0.42f), 0.06f, 0.115f);
        Ovalo(px, ColorRojo, 0.7f, new Vector2(0.5f, 0.46f), 0.006f, 0.008f);
        Ovalo(px, ColorRojo, 0.7f, new Vector2(0.5f, 0.4f), 0.006f, 0.008f);
        Curva(px, ColorRojo, 0.55f, new Vector2(0.45f, 0.51f), new Vector2(0.36f, 0.6f), new Vector2(0.28f, 0.69f));
        Curva(px, ColorRojo, 0.55f, new Vector2(0.55f, 0.51f), new Vector2(0.64f, 0.6f), new Vector2(0.72f, 0.69f));
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.255f, 0.715f), 0.025f, 0.03f);
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.745f, 0.715f), 0.025f, 0.03f);
        Curva(px, ColorRojo, 0.55f, new Vector2(0.47f, 0.31f), new Vector2(0.39f, 0.23f), new Vector2(0.445f, 0.135f));
        Curva(px, ColorRojo, 0.55f, new Vector2(0.53f, 0.31f), new Vector2(0.62f, 0.24f), new Vector2(0.585f, 0.135f));
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.43f, 0.115f), 0.045f, 0.022f);
        Ovalo(px, ColorRojo, 0.55f, new Vector2(0.6f, 0.115f), 0.045f, 0.022f);
        // El número de la toma, en la esquina (como en los papeles de los estudios).
        Poli(px, ColorGrafito, 0.45f, new Vector2(0.88f, 0.08f), new Vector2(0.89f, 0.13f), new Vector2(0.89f, 0.06f));
        Ovalo(px, ColorGrafito, 0.45f, new Vector2(0.92f, 0.095f), 0.012f, 0.03f);
    }

    static void Punto(Color32[] px, Color c, float fuerza, Vector2 uv)
    {
        Sello(px, uv.x * AnchoTex, uv.y * AltoTex, 1.5f, c, fuerza, true);
    }

    static void Poli(Color32[] px, Color c, float fuerza, params Vector2[] p)
    {
        for (int i = 1; i < p.Length; i++)
        {
            float largo = Vector2.Distance(Vector2.Scale(p[i - 1], new Vector2(AnchoTex, AltoTex)), Vector2.Scale(p[i], new Vector2(AnchoTex, AltoTex)));
            int n = Mathf.Max(2, Mathf.CeilToInt(largo / 0.8f));
            for (int k = 0; k <= n; k++)
                Punto(px, c, fuerza, Vector2.Lerp(p[i - 1], p[i], k / (float)n));
        }
    }

    static void Curva(Color32[] px, Color c, float fuerza, Vector2 a, Vector2 control, Vector2 b)
    {
        const int n = 400;
        for (int k = 0; k <= n; k++)
        {
            float t = k / (float)n, u = 1f - t;
            Punto(px, c, fuerza, u * u * a + 2f * u * t * control + t * t * b);
        }
    }

    // rx en ancho de la hoja; ry en alto de la hoja.
    static void Ovalo(Color32[] px, Color c, float fuerza, Vector2 centro, float rx, float ry)
    {
        int n = Mathf.Clamp(Mathf.CeilToInt((rx * AnchoTex + ry * AltoTex) * 4f), 40, 900);
        for (int k = 0; k <= n; k++)
        {
            float a = k / (float)n * Mathf.PI * 2f;
            Punto(px, c, fuerza, centro + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry));
        }
    }

    // ---------- El boceto se guarda (si sales sin entrar, sigue ahí) ----------

    static string RutaBoceto => Path.Combine(Application.persistentDataPath, "mesa_boceto.json");

    void GuardarBoceto()
    {
        try
        {
            File.WriteAllText(RutaBoceto, JsonUtility.ToJson(boceto));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: boceto de la mesa: " + e.Message);
        }
    }

    void CargarBoceto()
    {
        try
        {
            if (File.Exists(RutaBoceto))
            {
                var g = JsonUtility.FromJson<Guardado>(File.ReadAllText(RutaBoceto));
                if (g != null)
                    boceto = g;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: boceto de la mesa: " + e.Message);
        }
        if (boceto.lapiz == null) boceto.lapiz = new List<Linea>();
        if (boceto.tinta == null) boceto.tinta = new List<Linea>();
    }

    // v59: "Reset" en Archivos: la próxima vez la mesa empieza limpia (con el boceto antiguo).
    public static void BorrarBocetoGuardado()
    {
        try
        {
            if (File.Exists(RutaBoceto))
                File.Delete(RutaBoceto);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: boceto de la mesa: " + e.Message);
        }
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}

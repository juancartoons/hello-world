using UnityEngine;

// EL DEDO JERINGA (v44, parte de ControlManos): toca un color de la paleta y tu índice derecho se va LLENANDO de ese
// color, como el cuerpo de una jeringa. v45: se pinta EL MISMO DEDO de la mano (el shader de la mano, TrazoVR/ManoVidrio),
// desde la yema hacia el nudillo (antes era una cápsula aparte que se salía del dedo).
//  - Con el dedo cargado, TOCA una línea = la pintas; toca DENTRO de una forma = la rellenas. No hay que elegir antes
//    entre línea y relleno. Al pintar, el dedo se vacía.
//  - Mientras llevas el dedo cargado, a veces cae una gotita (o dos) de tinta, como si salieras de un tintero.
//    Caen y desaparecen antes de llegar al piso.
//  - El cuentagotas (tocar una línea con la paleta abierta) y la voz ("copiar") también cargan el dedo.
// (La cubeta sigue por ahora; si la jeringa funciona bien, se quita en la siguiente versión.)
// v46: el color sigue las ARTICULACIONES del dedo (yema → falanges → nudillo), así se llena aunque el dedo esté
// doblado. Tocar líneas es más fácil (más margen) y funciona cerca de la paleta.
// v47: es la PIEL de la punta del dedo la que se mancha, como si lo metieras en tinta: el shader TrazoVR/TintaDedo va
// como una pasada más sobre la malla de tu mano derecha (sea la de vidrio o la normal). Ya no hay "alma" por dentro.
// Al pintar, la tinta se seca en un momento (baja hacia la yema y desaparece).
public partial class ControlManos
{
    const float LlenadoSegundos = 0.7f;
    const float EsperaJeringa = 0.12f;     // segundos tocando antes de pintar (así no pinta al pasar de largo)
    const int Gotas = 12;   // v48: más gotas (el efecto es bonito)

    Color colorJeringa = Color.black;
    bool hayJeringa, jeringaEsperaSalir;
    float nivelJeringa, jeringaDesde, proximaGota;
    Trazo jeringaCandidato;
    bool jeringaCandidatoLinea;
    Transform tuboJeringa, nudilloIndiceDer, yemaIndiceDer, distalIndiceDer, medioIndiceDer;
    readonly Vector3[] cadenaDedo = new Vector3[4];
    const float TintaHasta = 0.5f;          // v47: la tinta llega hasta la mitad del dedo (la punta y un poco más)
    const float SecadoSegundos = 0.45f;
    static readonly int idJeringaPunta = Shader.PropertyToID("_JCJeringaPunta");
    static readonly int idJeringaNudillo = Shader.PropertyToID("_JCJeringaNudillo");
    static readonly int idJeringaColor = Shader.PropertyToID("_JCJeringaColor");
    static readonly int idJeringaA = Shader.PropertyToID("_JCJeringaA");
    static readonly int idJeringaB = Shader.PropertyToID("_JCJeringaB");
    readonly Transform[] gotas = new Transform[Gotas];
    readonly Vector3[] velGota = new Vector3[Gotas];
    readonly float[] vidaGota = new float[Gotas];
    readonly float[] tamGota = new float[Gotas];
    Vector3 puntaAntes;

    public bool HayJeringa => hayJeringa;
    public Color ColorJeringa => colorJeringa;

    // Carga el dedo con un color (se va llenando desde la punta).
    void CargarJeringa(Color c)
    {
        if (c.a < 0.01f)
            return;   // la tinta invisible no se carga
        bool nuevo = !hayJeringa || colorJeringa != c;
        colorJeringa = new Color(c.r, c.g, c.b, 1f);
        hayJeringa = true;
        jeringaEsperaSalir = true;
        if (nuevo)
            nivelJeringa = 0.05f;
        Shader.SetGlobalColor(idJeringaColor, colorJeringa);
        proximaGota = Time.time + 0.8f;
    }

    void VaciarJeringa()
    {
        hayJeringa = false;
        jeringaCandidato = null;
    }

    static void PintarMaterial(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }

    // Con el dedo cargado: ¿toca una línea o está dentro de una forma? Pinta (después de un instante) y se vacía.
    // Devuelve true mientras el dedo está sobre algo que pintar (así nada más usa ese toque).
    bool RevisarJeringa(Vector3 punta)
    {
        if (!hayJeringa || dibujo == null || !Der.valida)
            return false;
        Vector3 local = dibujo.transform.InverseTransformPoint(punta);
        float escala = dibujo.EscalaMundo;
        Trazo linea = null;
        float mejor = float.MaxValue;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || t.oculto)
                continue;
            float d = t.DistanciaACurva(local) * escala;
            if (d < t.ancho * escala * 0.5f + 0.015f && d < mejor)   // v46: más margen (antes 8 mm)
            {
                mejor = d;
                linea = t;
            }
        }
        Trazo objetivo = linea;
        bool esLinea = linea != null;
        if (objetivo == null)
            objetivo = FiguraBajo(local, 0.02f, false);
        if (objetivo == null)
        {
            jeringaEsperaSalir = false;
            jeringaCandidato = null;
            return false;
        }
        if (jeringaEsperaSalir)
            return true;
        if (objetivo != jeringaCandidato || esLinea != jeringaCandidatoLinea)
        {
            jeringaCandidato = objetivo;
            jeringaCandidatoLinea = esLinea;
            jeringaDesde = Time.time;
            return true;
        }
        if (Time.time - jeringaDesde < EsperaJeringa)
            return true;
        if (esLinea)
        {
            dibujo.GuardarParaDeshacer();
            dibujo.PonerColor(objetivo, colorJeringa);
            MostrarEtiqueta("Línea pintada");
        }
        else
        {
            dibujo.RellenarConColor(objetivo, colorJeringa);
            MostrarEtiqueta("Relleno pintado");
        }
        Burbuja(punta, 1.2f);
        SoltarGotas(punta, 4);
        VaciarJeringa();
        return true;
    }

    // Cada cuadro: el color dentro del dedo (lo dibuja el shader de la mano) y las gotas.
    void ActualizarJeringa()
    {
        float dt = Time.deltaTime;
        // v47: sin tinta, la que quedaba se seca (baja hacia la yema) y desaparece.
        if (!hayJeringa)
            nivelJeringa = Mathf.MoveTowards(nivelJeringa, 0f, dt / SecadoSegundos);
        bool ver = (hayJeringa || nivelJeringa > 0.01f) && Der.valida && !ocultarTodas && !ocultarDer && !ocultarDerPlano;
        if (ver && tuboJeringa == null)
            ArmarJeringa();
        if (ver)
        {
            if (nudilloIndiceDer == null)
                nudilloIndiceDer = ManosUtil.Hueso(Der.esqueleto, "Index1", "IndexProximal");
            if (medioIndiceDer == null)
                medioIndiceDer = ManosUtil.Hueso(Der.esqueleto, "Index2", "IndexIntermediate");
            if (distalIndiceDer == null)
                distalIndiceDer = ManosUtil.Hueso(Der.esqueleto, "Index3", "IndexDistal");
            if (yemaIndiceDer == null)
                yemaIndiceDer = ManosUtil.Hueso(Der.esqueleto, "IndexTip");
            if (hayJeringa)
                nivelJeringa = Mathf.MoveTowards(nivelJeringa, 1f, dt / LlenadoSegundos);
            Vector3 punta = yemaIndiceDer != null ? yemaIndiceDer.position : Der.indice;
            Vector3 nudillo = nudilloIndiceDer != null ? nudilloIndiceDer.position : punta + Vector3.down * 0.07f;
            // La cadena del dedo: yema, falange de la punta, falange del medio, nudillo (si falta un hueso, en línea recta).
            cadenaDedo[0] = punta;
            cadenaDedo[1] = distalIndiceDer != null ? distalIndiceDer.position : Vector3.Lerp(punta, nudillo, 0.3f);
            cadenaDedo[2] = medioIndiceDer != null ? medioIndiceDer.position : Vector3.Lerp(punta, nudillo, 0.62f);
            cadenaDedo[3] = nudillo;
            // Se mancha desde la yema hasta la mitad del dedo.
            float nivel = Mathf.SmoothStep(0f, 1f, nivelJeringa) * TintaHasta;
            Shader.SetGlobalVector(idJeringaPunta, new Vector4(punta.x, punta.y, punta.z, nivel));
            Shader.SetGlobalVector(idJeringaA, new Vector4(cadenaDedo[1].x, cadenaDedo[1].y, cadenaDedo[1].z, 0f));
            Shader.SetGlobalVector(idJeringaB, new Vector4(cadenaDedo[2].x, cadenaDedo[2].y, cadenaDedo[2].z, 0f));
            Shader.SetGlobalVector(idJeringaNudillo, new Vector4(nudillo.x, nudillo.y, nudillo.z, 1f));
            // Llevándolo cargado: de vez en cuando cae una gotita (o dos).
            float vel = dt > 1e-5f ? Vector3.Distance(Der.indice, puntaAntes) / dt : 0f;
            // v48: caen más seguido (moviéndolo más; quieto, de vez en cuando).
            if (hayJeringa && nivelJeringa > 0.6f && Time.time > proximaGota)
            {
                SoltarGotas(punta, vel > 0.12f ? Random.Range(1, 4) : 1);
                proximaGota = Time.time + (vel > 0.12f ? Random.Range(0.25f, 0.7f) : Random.Range(0.9f, 1.8f));
            }
        }
        else
        {
            Shader.SetGlobalVector(idJeringaNudillo, Vector4.zero);
        }
        puntaAntes = Der.indice;
        // Las gotas caen, se achican y desaparecen (nunca se ve dónde caen).
        for (int i = 0; i < Gotas; i++)
        {
            if (gotas[i] == null || !gotas[i].gameObject.activeSelf)
                continue;
            vidaGota[i] -= dt;
            if (vidaGota[i] <= 0f)
            {
                gotas[i].gameObject.SetActive(false);
                continue;
            }
            velGota[i] += Vector3.down * 9.8f * dt * 0.6f;
            gotas[i].position += velGota[i] * dt;
            float k = Mathf.Clamp01(vidaGota[i] / 0.25f);
            float s = tamGota[i] * k;
            gotas[i].localScale = new Vector3(s * 0.85f, s * 1.25f, s * 0.85f);   // alargadita al caer
        }
    }

    void SoltarGotas(Vector3 punta, int cuantas)
    {
        if (tuboJeringa == null)
            ArmarJeringa();
        for (int n = 0; n < cuantas; n++)
            for (int i = 0; i < Gotas; i++)
            {
                if (gotas[i].gameObject.activeSelf)
                    continue;
                var r = gotas[i].GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null)
                    PintarMaterial(r.sharedMaterial, colorJeringa);
                gotas[i].position = punta + Random.insideUnitSphere * 0.003f;
                velGota[i] = new Vector3(Random.Range(-0.05f, 0.05f), -0.05f - n * 0.08f, Random.Range(-0.05f, 0.05f));
                vidaGota[i] = Random.Range(0.45f, 0.65f);
                tamGota[i] = n == 0 ? Random.Range(0.005f, 0.007f) : Random.Range(0.0028f, 0.004f);
                gotas[i].gameObject.SetActive(true);
                break;
            }
    }

    void ArmarJeringa()
    {
        tuboJeringa = new GameObject("Jeringa").transform;
        var baseMat = materialNodo;
        for (int i = 0; i < Gotas; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Gota";
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(tuboJeringa, false);
            var rg = g.GetComponent<Renderer>();
            rg.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rg.receiveShadows = false;
            if (baseMat != null)
                rg.sharedMaterial = new Material(baseMat);
            g.SetActive(false);
            gotas[i] = g.transform;
        }
    }
}

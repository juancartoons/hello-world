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
// doblado; y por dentro va un "alma" de color (una línea fina por el centro del dedo) que se ve siempre, aunque
// la mano sea otra (sin el shader de vidrio). Tocar líneas es más fácil (más margen) y funciona cerca de la paleta.
public partial class ControlManos
{
    const float LlenadoSegundos = 0.7f;
    const float EsperaJeringa = 0.12f;     // segundos tocando antes de pintar (así no pinta al pasar de largo)
    const int Gotas = 4;

    Color colorJeringa = Color.black;
    bool hayJeringa, jeringaEsperaSalir;
    float nivelJeringa, jeringaDesde, proximaGota;
    Trazo jeringaCandidato;
    bool jeringaCandidatoLinea;
    Transform tuboJeringa, nudilloIndiceDer, yemaIndiceDer, distalIndiceDer, medioIndiceDer;
    LineRenderer almaJeringa;
    Material matAlma;
    readonly Vector3[] cadenaDedo = new Vector3[4];
    readonly Vector3[] puntosAlma = new Vector3[4];
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
        if (matAlma != null)
            PintarMaterial(matAlma, colorJeringa);
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
        SoltarGotas(punta, 2);
        VaciarJeringa();
        return true;
    }

    // Cada cuadro: el color dentro del dedo (lo dibuja el shader de la mano) y las gotas.
    void ActualizarJeringa()
    {
        float dt = Time.deltaTime;
        bool ver = hayJeringa && Der.valida && !ocultarTodas && !ocultarDer && !ocultarDerPlano;
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
            nivelJeringa = Mathf.MoveTowards(nivelJeringa, 1f, dt / LlenadoSegundos);
            Vector3 punta = yemaIndiceDer != null ? yemaIndiceDer.position : Der.indice;
            Vector3 nudillo = nudilloIndiceDer != null ? nudilloIndiceDer.position : punta + Vector3.down * 0.07f;
            // La cadena del dedo: yema, falange de la punta, falange del medio, nudillo (si falta un hueso, en línea recta).
            cadenaDedo[0] = punta;
            cadenaDedo[1] = distalIndiceDer != null ? distalIndiceDer.position : Vector3.Lerp(punta, nudillo, 0.3f);
            cadenaDedo[2] = medioIndiceDer != null ? medioIndiceDer.position : Vector3.Lerp(punta, nudillo, 0.62f);
            cadenaDedo[3] = nudillo;
            // Se llena hasta el 92 % (sin llegar a la palma).
            float nivel = Mathf.SmoothStep(0f, 1f, nivelJeringa) * 0.92f;
            Shader.SetGlobalVector(idJeringaPunta, new Vector4(punta.x, punta.y, punta.z, nivel));
            Shader.SetGlobalVector(idJeringaA, new Vector4(cadenaDedo[1].x, cadenaDedo[1].y, cadenaDedo[1].z, 0f));
            Shader.SetGlobalVector(idJeringaB, new Vector4(cadenaDedo[2].x, cadenaDedo[2].y, cadenaDedo[2].z, 0f));
            Shader.SetGlobalVector(idJeringaNudillo, new Vector4(nudillo.x, nudillo.y, nudillo.z, 1f));
            PonerAlma(nivel);
            // Llevándolo cargado: de vez en cuando cae una gotita (o dos).
            float vel = dt > 1e-5f ? Vector3.Distance(Der.indice, puntaAntes) / dt : 0f;
            if (nivelJeringa > 0.6f && vel > 0.12f && Time.time > proximaGota)
            {
                SoltarGotas(punta, Random.value < 0.5f ? 1 : 2);
                proximaGota = Time.time + Random.Range(1.2f, 2.6f);
            }
        }
        else
        {
            Shader.SetGlobalVector(idJeringaNudillo, Vector4.zero);
            if (almaJeringa != null && almaJeringa.enabled)
                almaJeringa.enabled = false;
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

    // v46: el alma de color, por el centro del dedo, desde la yema hasta donde llega el líquido (sigue las falanges).
    void PonerAlma(float nivel)
    {
        if (almaJeringa == null)
            return;
        float total = 0f;
        for (int i = 0; i < 3; i++)
            total += Vector3.Distance(cadenaDedo[i], cadenaDedo[i + 1]);
        float falta = total * nivel;
        int n = 1;
        // La yema del hueso queda en la orilla del dedo: el alma empieza 4 mm más adentro.
        puntosAlma[0] = Vector3.MoveTowards(cadenaDedo[0], cadenaDedo[1], 0.004f);
        for (int i = 0; i < 3 && falta > 1e-4f; i++)
        {
            float largo = Vector3.Distance(cadenaDedo[i], cadenaDedo[i + 1]);
            if (largo >= falta)
            {
                puntosAlma[n++] = Vector3.MoveTowards(cadenaDedo[i], cadenaDedo[i + 1], falta);
                falta = 0f;
            }
            else
            {
                puntosAlma[n++] = cadenaDedo[i + 1];
                falta -= largo;
            }
        }
        bool ver = n >= 2 && nivel > 0.02f;
        if (almaJeringa.enabled != ver)
            almaJeringa.enabled = ver;
        if (!ver)
            return;
        almaJeringa.positionCount = n;
        for (int i = 0; i < n; i++)
            almaJeringa.SetPosition(i, puntosAlma[i]);
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
        // v46: el alma de color (fina, por dentro del dedo de vidrio).
        var ga = new GameObject("AlmaJeringa");
        ga.transform.SetParent(tuboJeringa, false);
        almaJeringa = ga.AddComponent<LineRenderer>();
        almaJeringa.useWorldSpace = true;
        almaJeringa.numCapVertices = 4;
        almaJeringa.numCornerVertices = 3;
        almaJeringa.widthCurve = new AnimationCurve(new Keyframe(0f, 0.0062f), new Keyframe(1f, 0.0085f));
        almaJeringa.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        almaJeringa.receiveShadows = false;
        almaJeringa.positionCount = 0;
        almaJeringa.enabled = false;
        if (baseMat != null)
        {
            matAlma = new Material(baseMat);
            PintarMaterial(matAlma, colorJeringa);
            almaJeringa.sharedMaterial = matAlma;
        }
    }
}

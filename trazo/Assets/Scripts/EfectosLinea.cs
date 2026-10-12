using System.Collections.Generic;
using UnityEngine;

// EFECTOS DE LAS LÍNEAS (v36, "como de 2030"). Todo es visual: no cambia los datos del dibujo.
//  - SE MATERIALIZAN: al terminar una línea, un destello de luz la recorre de punta a punta (lo pinta el shader
//    TrazoLinea con _JCDestello).
//  - SE DISUELVEN: al borrarla, se deshace en polvo brillante que cae despacio (un sistema de partículas). Al borrar
//    un nodo (v37), se deshacen los dos tramos que lo sostenían.
//  - SE REBOBINAN: al deshacer, la línea que desaparece se recoge desde su final hasta su inicio (una copia de su
//    malla que el shader va cortando con _JCCorte).
public class EfectosLinea : MonoBehaviour
{
    const float DuracionDestello = 0.8f;    // un poco más que el recorrido del destello (0.55 s), para que salga entero
    const float DuracionRebobinar = 0.38f;
    const int MaxPolvoPorLinea = 700;

    static EfectosLinea instancia;
    static readonly int idDestello = Shader.PropertyToID("_JCDestello");
    static readonly int idCorte = Shader.PropertyToID("_JCCorte");

    class Destello
    {
        public Renderer r;
        public float fin;
    }

    class Fantasma
    {
        public GameObject go;
        public Renderer r;
        public Mesh malla;
        public float inicio;
    }

    readonly List<Destello> destellos = new List<Destello>();
    readonly List<Fantasma> fantasmas = new List<Fantasma>();
    MaterialPropertyBlock bloque;
    ParticleSystem polvo, polvoOscuro;
    Material materialOscuro;

    static EfectosLinea Instancia
    {
        get
        {
            if (instancia == null)
            {
                var go = new GameObject("EfectosLinea");
                instancia = go.AddComponent<EfectosLinea>();
            }
            return instancia;
        }
    }

    // ---------- Se materializa ----------

    public static void Materializar(Trazo t)
    {
        if (t == null || !t.gameObject.activeInHierarchy)
            return;
        var r = t.GetComponent<MeshRenderer>();
        if (r == null || r.forceRenderingOff)
            return;
        var e = Instancia;
        e.Bloque(r);
        e.bloque.SetFloat(idDestello, Time.timeSinceLevelLoad);
        r.SetPropertyBlock(e.bloque);
        float fin = Time.time + DuracionDestello;
        foreach (var d in e.destellos)
            if (d.r == r)
            {
                d.fin = fin;
                return;
            }
        e.destellos.Add(new Destello { r = r, fin = fin });
    }

    // ---------- Se disuelve en polvo ----------

    // Devuelve true si salió el polvo (entonces no hace falta el destello rojo de antes).
    public static bool Disolver(Trazo t)
    {
        if (!SePuedeVer(t) || t.curva.Count < 2)
            return false;
        return Instancia.Emitir(t, t.curva) > 0;
    }

    // v37: al borrar un nodo, se deshacen en polvo los dos tramos de línea que lo sostenían.
    public static bool DisolverNodo(Trazo t, int nodo)
    {
        if (!SePuedeVer(t) || !t.CurvaAlrededorDeNodo(nodo, tramo))
            return false;
        return Instancia.Emitir(t, tramo) > 0;
    }

    static readonly List<Vector3> tramo = new List<Vector3>();
    Escenario escenario;

    static bool SePuedeVer(Trazo t)
    {
        if (t == null || !t.gameObject.activeInHierarchy)
            return false;
        var r = t.GetComponent<MeshRenderer>();
        return r != null && !r.forceRenderingOff; // tinta invisible u oculta: sin polvo
    }

    // v38: con el fondo Blanco o la Cuadrícula, el polvo brillante casi no se ve: ahí sale del color de la línea
    // (oscuro con las líneas negras), mezclado normal en vez de sumar luz.
    bool FondoClaro()
    {
        if (escenario == null)
            escenario = FindFirstObjectByType<Escenario>();
        return escenario != null && escenario.modo < 2;
    }

    // Puntitos de polvo a lo largo de unos puntos de la línea (en sus coordenadas): devuelve cuántos salieron.
    int Emitir(Trazo t, List<Vector3> puntos)
    {
        bool claro = FondoClaro();
        var ps = claro ? PrepararPolvo(ref polvoOscuro, "PolvoOscuro", true) : PrepararPolvo(ref polvo, "Polvo", false);
        if (ps == null)
            return 0;
        Color c = claro ? Color.Lerp(t.color, Color.black, 0.15f) : Color.Lerp(t.color, Color.white, 0.55f);
        c.a = 1f;
        var tr = t.transform;
        float escala = Mathf.Max(1e-4f, tr.lossyScale.x);
        float paso = 0.006f / escala;   // un puntito cada 6 mm (en el dibujo)
        float hecho = 0f, siguiente = 0f;
        int emitidos = 0;
        var ep = new ParticleSystem.EmitParams();
        for (int i = 1; i < puntos.Count && emitidos < MaxPolvoPorLinea; i++)
        {
            Vector3 a = puntos[i - 1], b = puntos[i];
            float largo = Vector3.Distance(a, b);
            if (largo < 1e-7f)
                continue;
            while (siguiente <= hecho + largo && emitidos < MaxPolvoPorLinea)
            {
                float u = Mathf.Clamp01((siguiente - hecho) / largo);
                Vector3 p = tr.TransformPoint(Vector3.Lerp(a, b, u));
                for (int k = 0; k < 2; k++)
                {
                    ep.position = p + Random.insideUnitSphere * 0.002f;
                    ep.velocity = Random.insideUnitSphere * 0.025f + Vector3.down * 0.03f;
                    ep.startColor = c;
                    ep.startSize = Random.Range(0.003f, 0.007f);
                    ep.startLifetime = Random.Range(0.8f, 1.5f);
                    ps.Emit(ep, 1);
                    emitidos++;
                }
                siguiente += paso;
            }
            hecho += largo;
        }
        return emitidos;
    }

    // El sistema de partículas del polvo (se crea la primera vez). "normal" = mezcla normal (para fondos claros).
    ParticleSystem PrepararPolvo(ref ParticleSystem ps, string nombre, bool normal)
    {
        if (ps != null)
            return ps;
        var control = ControlManos.Instancia;
        var mat = control != null ? control.materialPolvo : null;
        if (mat == null)
            return null;
        if (normal)
        {
            mat = new Material(mat) { name = "PolvoOscuro" };
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            materialOscuro = mat;
        }
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 5000;
        main.startSpeed = 0f;
        main.gravityModifier = 0.02f;
        var emision = ps.emission;
        emision.enabled = false;
        var forma = ps.shape;
        forma.enabled = false;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
        color.color = new ParticleSystem.MinMaxGradient(grad);
        var tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.35f));
        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.015f;
        ruido.frequency = 2f;
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = mat;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        ps.Play();
        return ps;
    }

    // ---------- Se rebobina ----------

    // Lo llama el Dibujo al deshacer, justo antes de quitar una línea: una copia de la línea se recoge sola.
    public static void Rebobinar(Trazo t)
    {
        if (t == null || !t.gameObject.activeInHierarchy)
            return;
        var mf = t.GetComponent<MeshFilter>();
        var r = t.GetComponent<MeshRenderer>();
        if (mf == null || r == null || r.forceRenderingOff || r.sharedMaterial == null || mf.sharedMesh == null
            || mf.sharedMesh.vertexCount == 0)
            return;
        var e = Instancia;
        var go = new GameObject("Rebobinar");
        go.layer = t.gameObject.layer;
        var tr = t.transform;
        go.transform.SetPositionAndRotation(tr.position, tr.rotation);
        go.transform.localScale = tr.lossyScale;
        var malla = Instantiate(mf.sharedMesh);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var gr = go.AddComponent<MeshRenderer>();
        gr.sharedMaterial = r.sharedMaterial;
        gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        gr.receiveShadows = false;
        e.fantasmas.Add(new Fantasma { go = go, r = gr, malla = malla, inicio = Time.time });
        e.PonerCorte(gr, 1f);
    }

    void PonerCorte(Renderer r, float corte)
    {
        Bloque(r);
        bloque.SetFloat(idCorte, corte);
        r.SetPropertyBlock(bloque);
    }

    void Bloque(Renderer r)
    {
        if (bloque == null)
            bloque = new MaterialPropertyBlock();
        r.GetPropertyBlock(bloque);
    }

    // ---------- Cada cuadro ----------

    void Update()
    {
        float ahora = Time.time;
        for (int i = destellos.Count - 1; i >= 0; i--)
        {
            var d = destellos[i];
            if (d.r == null)
            {
                destellos.RemoveAt(i);
                continue;
            }
            if (ahora >= d.fin)
            {
                d.r.SetPropertyBlock(null); // vuelve a ser una línea normal
                destellos.RemoveAt(i);
            }
        }
        for (int i = fantasmas.Count - 1; i >= 0; i--)
        {
            var f = fantasmas[i];
            float u = (ahora - f.inicio) / DuracionRebobinar;
            if (f.r == null || u >= 1f)
            {
                if (f.go != null)
                    Destroy(f.go);
                if (f.malla != null)
                    Destroy(f.malla);
                fantasmas.RemoveAt(i);
                continue;
            }
            u = Mathf.Clamp01(u);
            PonerCorte(f.r, 1f - u * u * (3f - 2f * u));
        }
    }

    void OnDestroy()
    {
        foreach (var f in fantasmas)
        {
            if (f.go != null)
                Destroy(f.go);
            if (f.malla != null)
                Destroy(f.malla);
        }
        fantasmas.Clear();
        if (materialOscuro != null)
            Destroy(materialOscuro);
        if (instancia == this)
            instancia = null;
    }
}

using System.Collections.Generic;
using UnityEngine;

// Plano 2D: el dedo puede pasar DETRÁS del plano transparente y la línea sigue (pegada al plano).
// Mientras está detrás, tu mano derecha real se esconde y aparece una COPIA DE TU MISMA MANO (la misma
// forma y el mismo material) con la punta del índice justo sobre el plano, de tu lado, que imita todos tus
// movimientos. Un anillito naranja marca en el plano dónde está la punta. Alejar el dedo hacia ti levanta el lápiz.
// También sirve para el tutorial: al llegar a la bandera, la mano queda "atrapada" ahí (una copia quieta)
// y la línea ya no crece hasta que abres la mano izquierda.
public partial class ControlManos
{
    const float MaxDetrasPlano = 0.3f;     // más de 30 cm detrás del plano ya no cuenta
    const float SeparacionCopia = 0.004f;  // la punta de la copia queda 4 mm delante del plano (de tu lado)
    const float RadioAnilloPlano = 0.007f;
    const int LadosAnilloPlano = 24;

    // Una parte visible de tu mano derecha real, para dibujar su copia.
    class PiezaCopia
    {
        public Renderer origen;
        public Mesh malla;              // la forma de la mano en ese momento (horneada)
        public bool propia;             // la malla es nuestra (se destruye al final)
        public Matrix4x4 matriz;        // dónde estaba la pieza real
        public Material[] materiales;
        public MaterialPropertyBlock bloque;
        public int capa;
        public int escala = -1;         // -1 sin decidir, 0 la malla horneada ya trae la escala, 1 hay que ponérsela
    }

    readonly List<PiezaCopia> piezasCopia = new List<PiezaCopia>();
    int piezasActivas;
    Vector3 moverCopia;
    bool conCopia, ocultarDerPlano, copiaCongelada;
    LineRenderer anilloPlano;

    // Tutorial: la mano quedó atrapada en la bandera (la línea no crece; hay que abrir la mano izquierda).
    public bool LapizAtrapado { get; private set; }

    // Cuánto está el punto DETRÁS del plano, visto desde tu cabeza (en metros): + = detrás, - = de tu lado.
    float ProfundidadTrasPlano(Vector3 local)
    {
        float d = dibujo.DistanciaConSignoMundo(local);
        // La normal del plano apunta lejos de donde estabas al crearlo. Si le diste la vuelta, tu lado es el otro.
        if (Cabeza != null && dibujo.DistanciaConSignoMundo(dibujo.transform.InverseTransformPoint(Cabeza.position)) > 0f)
            d = -d;
        return d;
    }

    bool DibujandoEnPlano => dibujo != null && dibujo.PlanoActivo && !DibujoBloqueado
                             && (GestoIzq == Gesto.Dibujar || GestoIzq == Gesto.Recta);

    // ¿La punta del índice derecho está detrás del plano (con dibujar activo)? Da el punto sobre el plano
    // y la dirección del plano hacia ti.
    bool DedoTrasPlano(out Vector3 enPlanoMundo, out Vector3 haciaTi)
    {
        enPlanoMundo = Vector3.zero;
        haciaTi = Vector3.back;
        if (!DibujandoEnPlano || !Der.valida)
            return false;
        Vector3 local = dibujo.transform.InverseTransformPoint(Der.indice);
        float p = ProfundidadTrasPlano(local);
        if (p <= 0.001f || p > MaxDetrasPlano)
            return false;
        enPlanoMundo = dibujo.transform.TransformPoint(dibujo.ProyectarEnPlano(local));
        Vector3 puntoPlano, normal;
        dibujo.PlanoMundo(out puntoPlano, out normal);
        // La normal apunta lejos de ti (salvo que le hayas dado la vuelta al plano).
        haciaTi = Cabeza != null && Vector3.Dot(Cabeza.position - puntoPlano, normal) > 0f ? normal : -normal;
        return true;
    }

    // Se llama al final de cada cuadro (LateUpdate).
    void ActualizarManoCopia()
    {
        Vector3 enPlano = Vector3.zero, haciaTi = Vector3.back;
        bool detras = false, ver;
        if (LapizAtrapado)
        {
            // La copia se queda quieta donde llegaste (no se vuelve a tomar la forma de la mano).
            ver = copiaCongelada;
        }
        else
        {
            detras = DedoTrasPlano(out enPlano, out haciaTi);
            ver = false;
            var punta = detras ? ManosUtil.Hueso(Der.esqueleto, "IndexTip") : null;
            if (punta != null && TomarFormaManoDer())
            {
                // Toda la mano se mueve junta para que la punta del índice quede justo sobre el anillito.
                moverCopia = enPlano + haciaTi * SeparacionCopia - punta.position;
                ver = true;
            }
        }
        if (ver)
            DibujarCopia();
        if (ver != conCopia)
        {
            conCopia = ver;
            OcultarDerPorPlano(ver);
        }
        PonerAnilloPlano(detras, enPlano, haciaTi);
    }

    // Tutorial: al llegar a la bandera. La mano derecha queda quieta ahí (una copia) y la línea ya no crece.
    public void AtraparLapiz()
    {
        if (LapizAtrapado)
            return;
        LapizAtrapado = true;
        moverCopia = Vector3.zero;
        copiaCongelada = TomarFormaManoDer();
    }

    // Al abrir la mano izquierda (se llama al salir del gesto).
    void SoltarLapizAtrapado()
    {
        LapizAtrapado = false;
        copiaCongelada = false;
    }

    // Toma la forma de este momento de tu mano derecha real (todas sus partes visibles).
    bool TomarFormaManoDer()
    {
        if (rendsDer.Count == 0)
            BuscarRenderersMano();
        int n = 0;
        foreach (var r in rendsDer)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                continue;
            var smr = r as SkinnedMeshRenderer;
            Mesh fuente = null;
            if (smr == null)
            {
                var mf = r.GetComponent<MeshFilter>();
                fuente = mf != null ? mf.sharedMesh : null;
                if (fuente == null)
                    continue;
            }
            else if (smr.sharedMesh == null)
            {
                continue;
            }
            if (n >= piezasCopia.Count)
                piezasCopia.Add(new PiezaCopia { bloque = new MaterialPropertyBlock() });
            var p = piezasCopia[n++];
            if (p.origen != r)
            {
                p.origen = r;
                p.escala = -1;
            }
            Transform t = r.transform;
            if (smr != null)
            {
                if (p.malla == null || !p.propia)
                {
                    p.malla = new Mesh { name = "CopiaMano" };
                    p.malla.MarkDynamic();
                    p.propia = true;
                }
                smr.BakeMesh(p.malla);
                if (p.escala < 0)
                {
                    p.malla.RecalculateBounds();
                    p.escala = UsarEscala(smr, p.malla) ? 1 : 0;
                }
                p.matriz = p.escala == 1 ? t.localToWorldMatrix : Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
            }
            else
            {
                if (p.propia && p.malla != null)
                    Destroy(p.malla);
                p.malla = fuente;
                p.propia = false;
                p.matriz = t.localToWorldMatrix;
            }
            p.materiales = r.sharedMaterials;
            p.capa = r.gameObject.layer;
            r.GetPropertyBlock(p.bloque);
        }
        piezasActivas = n;
        return n > 0;
    }

    // ¿La malla horneada viene sin la escala de la mano? (se compara con el tamaño que Unity le da a la mano real)
    static bool UsarEscala(SkinnedMeshRenderer smr, Mesh horneada)
    {
        float s = Mathf.Abs(smr.transform.lossyScale.x);
        if (Mathf.Abs(s - 1f) < 0.02f)
            return false;
        float real = smr.bounds.size.magnitude;
        float sinEscala = horneada.bounds.size.magnitude;
        return Mathf.Abs(sinEscala * s - real) < Mathf.Abs(sinEscala - real);
    }

    void DibujarCopia()
    {
        var mover = Matrix4x4.Translate(moverCopia);
        for (int i = 0; i < piezasActivas; i++)
        {
            var p = piezasCopia[i];
            if (p.malla == null || p.materiales == null || p.materiales.Length == 0)
                continue;
            Matrix4x4 m = mover * p.matriz;
            int partes = Mathf.Max(1, p.malla.subMeshCount);
            int cuantos = Mathf.Max(partes, p.materiales.Length);
            for (int k = 0; k < cuantos; k++)
            {
                var mat = p.materiales[Mathf.Min(k, p.materiales.Length - 1)];
                if (mat != null)
                    Graphics.DrawMesh(p.malla, m, mat, p.capa, null, Mathf.Min(k, partes - 1), p.bloque, false, false);
            }
        }
    }

    void OcultarDerPorPlano(bool ocultar)
    {
        ocultarDerPlano = ocultar;
        BuscarRenderersMano();
        Forzar(rendsDer, ocultar || ocultarDer || ocultarTodas);
    }

    // Anillito naranja sobre el plano, alrededor de la punta del dedo.
    void PonerAnilloPlano(bool ver, Vector3 centro, Vector3 haciaTi)
    {
        if (!ver)
        {
            if (anilloPlano != null && anilloPlano.gameObject.activeSelf)
                anilloPlano.gameObject.SetActive(false);
            return;
        }
        if (anilloPlano == null)
        {
            var go = new GameObject("AnilloPlano");
            go.transform.SetParent(transform, false);
            anilloPlano = go.AddComponent<LineRenderer>();
            anilloPlano.useWorldSpace = true;
            anilloPlano.loop = true;
            anilloPlano.positionCount = LadosAnilloPlano;
            anilloPlano.widthMultiplier = 0.0016f;
            anilloPlano.numCornerVertices = 2;
            anilloPlano.sharedMaterial = materialNodoActivo != null ? materialNodoActivo : materialCursor;
            anilloPlano.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            anilloPlano.receiveShadows = false;
        }
        if (!anilloPlano.gameObject.activeSelf)
            anilloPlano.gameObject.SetActive(true);
        Vector3 u = Vector3.Cross(haciaTi, Vector3.up);
        if (u.sqrMagnitude < 1e-4f)
            u = Vector3.Cross(haciaTi, Vector3.right);
        u.Normalize();
        Vector3 v = Vector3.Cross(haciaTi, u).normalized;
        centro += haciaTi * 0.001f;
        for (int i = 0; i < LadosAnilloPlano; i++)
        {
            float a = i * (Mathf.PI * 2f / LadosAnilloPlano);
            anilloPlano.SetPosition(i, centro + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * RadioAnilloPlano);
        }
    }
}

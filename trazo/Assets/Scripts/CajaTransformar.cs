using System.Collections.Generic;
using UnityEngine;

// Caja con 8 esquinas alrededor de todo el dibujo.
//  Pellizca una ESQUINA (con cualquier mano) y aléjala/acércala del centro: escala todo.
//  Pellizca DENTRO de la caja y mueve la mano: mueve todo el dibujo.
public class CajaTransformar : MonoBehaviour
{
    public Dibujo dibujo;
    public Material materialCaja;
    public Material materialEsquina;
    public Material materialEsquinaActiva;
    public float radioEsquina = 0.05f;
    public float tamanoEsquina = 0.02f;

    public bool Activa { get; private set; }
    public bool Agarrando => modo != Modo.Nada;

    enum Modo { Nada, Escalar, Mover }
    Modo modo;
    ManoSeguida manoAgarre;
    int esquinaAgarrada = -1;
    int esquinaCercana = -1;

    Bounds cajaLocal;
    GameObject lineas;
    Mesh mallaLineas;
    readonly Transform[] esquinas = new Transform[8];
    readonly Renderer[] esquinasRender = new Renderer[8];

    Vector3 centroInicio, puntoInicio, posicionInicio;
    float escalaInicio, distanciaInicio;

    void Start()
    {
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
        if (dibujo != null)
            dibujo.alCambiar += Recalcular;
    }

    void OnDestroy()
    {
        if (dibujo != null)
            dibujo.alCambiar -= Recalcular;
    }

    public void Alternar()
    {
        if (Activa)
        {
            Activa = false;
            modo = Modo.Nada;
            Mostrar(false);
            return;
        }
        if (dibujo == null || !dibujo.Caja(out cajaLocal))
        {
            if (dibujo != null)
                dibujo.Mensaje("Dibuja algo primero");
            return;
        }
        Activa = true;
        CrearPiezas();
        ConstruirLineas();
        Mostrar(true);
    }

    void Recalcular()
    {
        if (!Activa || Agarrando)
            return;
        if (!dibujo.Caja(out cajaLocal))
        {
            Activa = false;
            Mostrar(false);
            return;
        }
        ConstruirLineas();
    }

    public void Actualizar(ManoSeguida izq, ManoSeguida der)
    {
        if (!Activa || dibujo == null)
            return;
        Transform raiz = dibujo.transform;

        if (modo == Modo.Nada)
        {
            esquinaCercana = -1;
            if (!Probar(der))
                Probar(izq);
        }
        else if (manoAgarre == null || !manoAgarre.pellizco)
        {
            modo = Modo.Nada;
            manoAgarre = null;
            esquinaAgarrada = -1;
            dibujo.Mensaje("Listo");
        }
        else
        {
            Vector3 p = manoAgarre.PuntoPellizco;
            if (modo == Modo.Escalar)
            {
                float s = Vector3.Distance(p, centroInicio) / distanciaInicio;
                float nueva = Mathf.Clamp(escalaInicio * s, 0.02f, 50f);
                float real = nueva / escalaInicio;
                raiz.localScale = Vector3.one * nueva;
                raiz.position = centroInicio + (posicionInicio - centroInicio) * real;
            }
            else
            {
                raiz.position = posicionInicio + (p - puntoInicio);
            }
        }

        int resaltada = modo == Modo.Escalar ? esquinaAgarrada : esquinaCercana;
        for (int i = 0; i < 8; i++)
        {
            if (esquinas[i] == null)
                continue;
            esquinas[i].position = raiz.TransformPoint(Esquina(i));
            esquinas[i].rotation = raiz.rotation;
            esquinas[i].localScale = Vector3.one * (i == resaltada ? tamanoEsquina * 1.5f : tamanoEsquina);
            var mat = i == resaltada ? materialEsquinaActiva : materialEsquina;
            if (mat != null && esquinasRender[i].sharedMaterial != mat)
                esquinasRender[i].sharedMaterial = mat;
        }
    }

    // Revisa si esta mano empieza a agarrar una esquina o el interior de la caja.
    bool Probar(ManoSeguida mano)
    {
        if (mano == null || !mano.valida)
            return false;
        Vector3 p = mano.PuntoPellizco;
        int cerca = EsquinaMasCercana(p);
        if (cerca >= 0)
            esquinaCercana = cerca;
        if (!mano.empezoPellizco)
            return cerca >= 0;

        Transform raiz = dibujo.transform;
        if (cerca >= 0)
        {
            modo = Modo.Escalar;
            esquinaAgarrada = cerca;
            centroInicio = raiz.TransformPoint(cajaLocal.center);
            distanciaInicio = Mathf.Max(0.01f, Vector3.Distance(p, centroInicio));
        }
        else if (DentroDeCaja(p))
        {
            modo = Modo.Mover;
        }
        else
        {
            return false;
        }
        dibujo.GuardarParaDeshacer();
        manoAgarre = mano;
        puntoInicio = p;
        posicionInicio = raiz.position;
        escalaInicio = Mathf.Max(0.0001f, raiz.localScale.x);
        return true;
    }

    int EsquinaMasCercana(Vector3 p)
    {
        int mejor = -1;
        float menor = radioEsquina;
        for (int i = 0; i < 8; i++)
        {
            float d = Vector3.Distance(p, dibujo.transform.TransformPoint(Esquina(i)));
            if (d < menor)
            {
                menor = d;
                mejor = i;
            }
        }
        return mejor;
    }

    bool DentroDeCaja(Vector3 p)
    {
        Vector3 local = dibujo.transform.InverseTransformPoint(p);
        var b = cajaLocal;
        b.Expand(0.03f / Mathf.Max(0.0001f, dibujo.transform.lossyScale.x));
        return b.Contains(local);
    }

    Vector3 Esquina(int i)
    {
        Vector3 min = cajaLocal.min;
        Vector3 max = cajaLocal.max;
        return new Vector3((i & 1) != 0 ? max.x : min.x, (i & 2) != 0 ? max.y : min.y, (i & 4) != 0 ? max.z : min.z);
    }

    void CrearPiezas()
    {
        if (lineas != null)
            return;
        lineas = new GameObject("CajaLineas");
        lineas.transform.SetParent(dibujo.transform, false);
        mallaLineas = new Mesh { name = "Caja" };
        lineas.AddComponent<MeshFilter>().sharedMesh = mallaLineas;
        var mr = lineas.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materialCaja;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        for (int i = 0; i < 8; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Esquina" + i;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<Renderer>();
            if (materialEsquina != null)
                r.sharedMaterial = materialEsquina;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            esquinas[i] = go.transform;
            esquinasRender[i] = r;
        }
    }

    void ConstruirLineas()
    {
        if (mallaLineas == null)
            return;
        var v = new Vector3[8];
        for (int i = 0; i < 8; i++)
            v[i] = Esquina(i);
        var idx = new List<int>();
        int[] bits = { 1, 2, 4 };
        for (int i = 0; i < 8; i++)
            foreach (int bit in bits)
                if ((i & bit) == 0)
                {
                    idx.Add(i);
                    idx.Add(i | bit);
                }
        mallaLineas.Clear();
        mallaLineas.vertices = v;
        mallaLineas.SetIndices(idx, MeshTopology.Lines, 0);
        mallaLineas.RecalculateBounds();
    }

    void Mostrar(bool ver)
    {
        if (lineas != null)
            lineas.SetActive(ver);
        foreach (var e in esquinas)
            if (e != null)
                e.gameObject.SetActive(ver);
    }
}

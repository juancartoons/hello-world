using UnityEngine;

// Girar, escalar y mover todo el dibujo con las DOS manos pellizcando (índice + pulgar) a la vez.
//  - Separar o juntar las manos: más grande o más pequeño.
//  - Mover las manos como un volante (en cualquier dirección): gira el dibujo.
//  - Mover las dos manos juntas: lo traslada.
// Si hay líneas seleccionadas (una o varias), solo se transforman esas líneas.
// Mientras dura, se ve una caja suave alrededor del dibujo y una línea entre las manos.
public class CajaTransformar : MonoBehaviour
{
    public Dibujo dibujo;
    public Material materialCaja;

    public bool Activa { get; private set; }

    Vector3 posicionInicio;
    Quaternion rotacionInicio;
    float escalaInicio;
    Vector3 medioInicio;
    Vector3 vectorInicio;
    readonly System.Collections.Generic.List<Trazo> solos = new System.Collections.Generic.List<Trazo>();
    readonly System.Collections.Generic.List<DatosTrazo> origenes = new System.Collections.Generic.List<DatosTrazo>();
    Transform objeto;          // una imagen de referencia (en vez del dibujo)
    Vector3 objetoPosicion;
    Quaternion objetoRotacion;
    float objetoEscala;

    GameObject lineas;
    Mesh mallaLineas;
    GameObject volante;
    Mesh mallaVolante;
    readonly Vector3[] puntosVolante = new Vector3[2];
    static readonly int[] indicesVolante = { 0, 1 };

    void Start()
    {
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
    }

    public void Empezar(ManoSeguida izq, ManoSeguida der, Trazo seleccion)
    {
        Empezar(izq, der, seleccion, null);
    }

    public void Empezar(ManoSeguida izq, ManoSeguida der, Trazo seleccion, Transform imagen)
    {
        var lista = new System.Collections.Generic.List<Trazo>();
        if (seleccion != null)
            lista.Add(seleccion);
        Empezar(izq, der, lista, imagen);
    }

    // Con "imagen" (y sin líneas seleccionadas) se transforma esa imagen de referencia (o figura).
    public void Empezar(ManoSeguida izq, ManoSeguida der, System.Collections.Generic.List<Trazo> seleccion, Transform imagen)
    {
        if (dibujo == null)
            return;
        bool hayLineas = seleccion != null && seleccion.Count > 0;
        objeto = hayLineas ? null : imagen;
        if (objeto != null)
        {
            objetoPosicion = objeto.position;
            objetoRotacion = objeto.rotation;
            objetoEscala = Mathf.Max(0.0001f, objeto.localScale.x);
        }
        else
        {
            dibujo.GuardarParaDeshacer();
        }
        solos.Clear();
        origenes.Clear();
        if (hayLineas)
            foreach (var t in seleccion)
            {
                if (t == null)
                    continue;
                solos.Add(t);
                origenes.Add(t.CrearDatos());
            }
        Transform raiz = dibujo.transform;
        posicionInicio = raiz.position;
        rotacionInicio = raiz.rotation;
        escalaInicio = Mathf.Max(0.0001f, raiz.localScale.x);
        medioInicio = (izq.PuntoPellizco + der.PuntoPellizco) * 0.5f;
        vectorInicio = der.PuntoPellizco - izq.PuntoPellizco;
        Activa = true;

        CrearPiezas();
        Bounds caja = new Bounds();
        bool hay = solos.Count == 0 && objeto == null && dibujo.Caja(out caja);
        if (hay)
            ConstruirLineas(caja);
        lineas.SetActive(hay);
        volante.SetActive(true);
        ActualizarVolante(izq.PuntoPellizco, der.PuntoPellizco);
    }

    public void Actualizar(ManoSeguida izq, ManoSeguida der)
    {
        if (!Activa || dibujo == null)
            return;
        if (!izq.valida || !der.valida)
            return; // si una mano se pierde un instante, el dibujo se queda quieto

        Vector3 a = izq.PuntoPellizco;
        Vector3 b = der.PuntoPellizco;
        ActualizarVolante(a, b);

        Vector3 v = b - a;
        if (vectorInicio.magnitude < 0.02f || v.magnitude < 0.02f)
            return;
        float s = v.magnitude / vectorInicio.magnitude;
        Quaternion giroSolo = Quaternion.FromToRotation(vectorInicio, v);
        Vector3 medioSolo = (a + b) * 0.5f;
        if (objeto != null)
        {
            float escalaObjeto = Mathf.Clamp(objetoEscala * s, objetoEscala * 0.05f, objetoEscala * 20f);
            float so = escalaObjeto / objetoEscala;
            objeto.position = medioSolo + giroSolo * ((objetoPosicion - medioInicio) * so);
            objeto.rotation = giroSolo * objetoRotacion;
            objeto.localScale = Vector3.one * escalaObjeto;
            return;
        }
        if (solos.Count > 0)
        {
            s = Mathf.Clamp(s, 0.05f, 20f);
            Transform r = dibujo.transform;
            Matrix4x4 mundo = Matrix4x4.TRS(medioSolo, giroSolo, Vector3.one * s) * Matrix4x4.Translate(-medioInicio);
            Matrix4x4 m = r.worldToLocalMatrix * mundo * r.localToWorldMatrix;
            for (int k = 0; k < solos.Count; k++)
                if (Dibujo.Editable(solos[k]))
                    solos[k].TransformarDesde(origenes[k], m, s);
            return;
        }
        float nueva = Mathf.Clamp(escalaInicio * s, 0.02f, 50f);
        s = nueva / escalaInicio;
        Quaternion giro = Quaternion.FromToRotation(vectorInicio, v);
        Vector3 medio = (a + b) * 0.5f;

        Transform raiz = dibujo.transform;
        raiz.position = medio + giro * ((posicionInicio - medioInicio) * s);
        raiz.rotation = giro * rotacionInicio;
        raiz.localScale = Vector3.one * nueva;
    }

    public void Terminar()
    {
        if (!Activa)
            return;
        Activa = false;
        solos.Clear();
        objeto = null;
        origenes.Clear();
        if (lineas != null)
            lineas.SetActive(false);
        if (volante != null)
            volante.SetActive(false);
        if (dibujo != null)
            dibujo.NotificarCambio();
    }

    void ActualizarVolante(Vector3 a, Vector3 b)
    {
        if (mallaVolante == null)
            return;
        puntosVolante[0] = a;
        puntosVolante[1] = b;
        mallaVolante.vertices = puntosVolante;
        mallaVolante.SetIndices(indicesVolante, MeshTopology.Lines, 0);
        mallaVolante.RecalculateBounds();
    }

    void CrearPiezas()
    {
        if (lineas == null)
        {
            lineas = new GameObject("CajaLineas");
            lineas.transform.SetParent(dibujo.transform, false);
            mallaLineas = new Mesh { name = "Caja" };
            lineas.AddComponent<MeshFilter>().sharedMesh = mallaLineas;
            Preparar(lineas.AddComponent<MeshRenderer>());
        }
        if (volante == null)
        {
            volante = new GameObject("Volante");
            volante.transform.SetParent(transform, false);
            mallaVolante = new Mesh { name = "Volante" };
            mallaVolante.MarkDynamic();
            volante.AddComponent<MeshFilter>().sharedMesh = mallaVolante;
            Preparar(volante.AddComponent<MeshRenderer>());
        }
    }

    void Preparar(MeshRenderer mr)
    {
        mr.sharedMaterial = materialCaja;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void ConstruirLineas(Bounds caja)
    {
        Vector3 min = caja.min;
        Vector3 max = caja.max;
        var v = new Vector3[8];
        for (int i = 0; i < 8; i++)
            v[i] = new Vector3((i & 1) != 0 ? max.x : min.x, (i & 2) != 0 ? max.y : min.y, (i & 4) != 0 ? max.z : min.z);
        var idx = new System.Collections.Generic.List<int>();
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
}

using UnityEngine;

// Los símbolos 3D que reemplazan a la mano o ayudan a ver lo que pasa:
//  - Borrador (goma rosada con funda) cuando la mano izquierda hace el puño de borrar.
//  - Flecha + diana: deshacer (diana roja, a tu izquierda) y rehacer (diana verde, a tu derecha).
//  - Candado pequeño arriba a la derecha de tu vista cuando el dibujo está bloqueado.
//  - Dial (círculo pequeño) mientras eliges el grosor girando el índice.
// Todo se arma con formas simples, sin modelos externos.
public class SimbolosMano : MonoBehaviour
{
    public Material materialGoma;
    public Material materialFunda;
    public Material materialFlecha;
    public Material materialDianaDeshacer;
    public Material materialDianaRehacer;
    public Material materialBlanco;
    public Material materialCandado;
    public Material materialDial;

    Transform borrador, flecha, candado, dial;
    Transform cabezaFlecha, astaFlecha;
    readonly Transform[] dianas = new Transform[2];
    readonly float[] destelloHasta = new float[2];
    static Mesh mallaCono;

    void Awake()
    {
        Armar();
    }

    void Armar()
    {
        if (borrador != null)
            return;

        // Borrador: goma rosada y funda azul en el centro.
        borrador = new GameObject("Borrador").transform;
        borrador.SetParent(transform, false);
        Caja(borrador, materialGoma, Vector3.zero, new Vector3(0.05f, 0.028f, 0.085f));
        Caja(borrador, materialFunda, new Vector3(0f, 0f, -0.012f), new Vector3(0.053f, 0.031f, 0.05f));
        borrador.gameObject.SetActive(false);

        // Flecha: asta, punta (cono) y plumas. Mira hacia +z local.
        flecha = new GameObject("Flecha").transform;
        flecha.SetParent(transform, false);
        var asta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        asta.name = "Asta";
        Destroy(asta.GetComponent<Collider>());
        asta.transform.SetParent(flecha, false);
        asta.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Pintar(asta.GetComponent<Renderer>(), materialFlecha);
        astaFlecha = asta.transform;
        var punta = new GameObject("Punta");
        punta.transform.SetParent(flecha, false);
        punta.AddComponent<MeshFilter>().sharedMesh = MallaCono();
        Pintar(punta.AddComponent<MeshRenderer>(), materialFlecha);
        cabezaFlecha = punta.transform;
        Caja(flecha, materialFlecha, new Vector3(0f, 0f, 0.012f), new Vector3(0.022f, 0.002f, 0.024f));
        Caja(flecha, materialFlecha, new Vector3(0f, 0f, 0.012f), new Vector3(0.002f, 0.022f, 0.024f));
        flecha.gameObject.SetActive(false);

        // Dianas: tres círculos (color, blanco, color).
        for (int k = 0; k < 2; k++)
        {
            var color = k == 0 ? materialDianaDeshacer : materialDianaRehacer;
            var d = new GameObject(k == 0 ? "DianaDeshacer" : "DianaRehacer").transform;
            d.SetParent(transform, false);
            Disco(d, color, 0f, 1f);
            Disco(d, materialBlanco, -0.02f, 0.66f);
            Disco(d, color, -0.04f, 0.33f);
            d.gameObject.SetActive(false);
            dianas[k] = d;
        }

        // Candado: cuerpo y arco.
        candado = new GameObject("Candado").transform;
        candado.SetParent(transform, false);
        Caja(candado, materialCandado, new Vector3(0f, -0.004f, 0f), new Vector3(0.02f, 0.016f, 0.006f));
        Caja(candado, materialCandado, new Vector3(-0.0065f, 0.0075f, 0f), new Vector3(0.003f, 0.01f, 0.003f));
        Caja(candado, materialCandado, new Vector3(0.0065f, 0.0075f, 0f), new Vector3(0.003f, 0.01f, 0.003f));
        Caja(candado, materialCandado, new Vector3(0f, 0.0125f, 0f), new Vector3(0.016f, 0.003f, 0.003f));
        candado.gameObject.SetActive(false);

        // Dial: un aro delgado.
        var aro = new GameObject("Dial");
        aro.transform.SetParent(transform, false);
        aro.AddComponent<MeshFilter>().sharedMesh = ControlManos.MallaAnillo();
        Pintar(aro.AddComponent<MeshRenderer>(), materialDial);
        dial = aro.transform;
        dial.gameObject.SetActive(false);
    }

    static void Pintar(Renderer r, Material m)
    {
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static void Caja(Transform padre, Material m, Vector3 pos, Vector3 escala)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        Pintar(go.GetComponent<Renderer>(), m);
    }

    // Círculo de diámetro "tam" (en unidades de la diana); z negativo = más cerca de ti.
    static void Disco(Transform padre, Material m, float z, float tam)
    {
        var go = new GameObject("Circulo");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        go.transform.localScale = new Vector3(tam, tam, 1f);
        go.AddComponent<MeshFilter>().sharedMesh = ControlManos.MallaDisco();
        Pintar(go.AddComponent<MeshRenderer>(), m);
    }

    static Mesh MallaCono()
    {
        if (mallaCono != null)
            return mallaCono;
        const int lados = 12;
        var v = new Vector3[lados + 2];
        var tri = new int[lados * 6];
        v[0] = new Vector3(0f, 0f, 1f); // punta
        v[1] = Vector3.zero;            // centro de la base
        for (int i = 0; i < lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            v[i + 2] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
        }
        for (int i = 0; i < lados; i++)
        {
            int a = i + 2;
            int b = (i + 1) % lados + 2;
            tri[i * 6] = 0; tri[i * 6 + 1] = a; tri[i * 6 + 2] = b;
            tri[i * 6 + 3] = 1; tri[i * 6 + 4] = b; tri[i * 6 + 5] = a;
        }
        mallaCono = new Mesh { name = "Cono" };
        mallaCono.vertices = v;
        mallaCono.triangles = tri;
        mallaCono.RecalculateNormals();
        mallaCono.RecalculateBounds();
        return mallaCono;
    }

    // ---------- Para ControlManos ----------

    public void Borrador(bool ver, Vector3 posicion, Quaternion rotacion)
    {
        Armar();
        if (borrador.gameObject.activeSelf != ver)
            borrador.gameObject.SetActive(ver);
        if (ver)
            borrador.SetPositionAndRotation(posicion, rotacion);
    }

    // La flecha empieza en "cola" y su punta queda en cola + dir * largo.
    public void Flecha(bool ver, Vector3 cola, Vector3 dir, float largo)
    {
        Armar();
        if (flecha.gameObject.activeSelf != ver)
            flecha.gameObject.SetActive(ver);
        if (!ver || dir.sqrMagnitude < 1e-6f)
            return;
        flecha.SetPositionAndRotation(cola, Quaternion.LookRotation(dir.normalized));
        float largoPunta = 0.03f;
        float largoAsta = Mathf.Max(0.01f, largo - largoPunta);
        astaFlecha.localPosition = new Vector3(0f, 0f, largoAsta * 0.5f);
        astaFlecha.localScale = new Vector3(0.007f, largoAsta * 0.5f, 0.007f);
        cabezaFlecha.localPosition = new Vector3(0f, 0f, largoAsta);
        cabezaFlecha.localScale = new Vector3(0.012f, 0.012f, largoPunta);
    }

    // rehacer: false = diana de deshacer, true = de rehacer.
    public void Diana(bool rehacer, bool ver, Vector3 posicion, Vector3 cabeza, float diametro)
    {
        Armar();
        int k = rehacer ? 1 : 0;
        var d = dianas[k];
        if (d.gameObject.activeSelf != ver)
            d.gameObject.SetActive(ver);
        if (!ver)
            return;
        Vector3 mirar = posicion - cabeza;
        if (mirar.sqrMagnitude > 1e-6f)
            d.SetPositionAndRotation(posicion, Quaternion.LookRotation(mirar, Vector3.up));
        float pulso = Time.time < destelloHasta[k] ? 1.35f : 1f;
        d.localScale = new Vector3(diametro * pulso, diametro * pulso, 0.05f);
    }

    // Pequeño "salto" de la diana al acertar.
    public void Acertar(bool rehacer)
    {
        destelloHasta[rehacer ? 1 : 0] = Time.time + 0.15f;
    }

    // Candado arriba a la derecha de tu vista (sigue a tu cabeza, sin tapar el centro).
    public void Candado(bool ver, Transform cabeza)
    {
        Armar();
        ver = ver && cabeza != null;
        if (candado.gameObject.activeSelf != ver)
            candado.gameObject.SetActive(ver);
        if (!ver)
            return;
        Vector3 pos = cabeza.position + cabeza.forward * 0.45f + cabeza.right * 0.16f + cabeza.up * 0.11f;
        candado.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cabeza.position, cabeza.up));
    }

    public void Dial(bool ver, Vector3 centro, Vector3 cabeza, float diametro)
    {
        Armar();
        if (dial.gameObject.activeSelf != ver)
            dial.gameObject.SetActive(ver);
        if (!ver)
            return;
        Vector3 mirar = centro - cabeza;
        if (mirar.sqrMagnitude > 1e-6f)
            dial.SetPositionAndRotation(centro, Quaternion.LookRotation(mirar, Vector3.up));
        dial.localScale = Vector3.one * diametro;
    }
}

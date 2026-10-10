using TMPro;
using UnityEngine;

// Panel "?" (se abre con el botón ? del menú de la mano izquierda): las secciones del tutorial.
//  1 · Primeros pasos: el tutorial de presentación (el de las manos guía).
//  2 · Rellenos: la cubeta, tocar dentro para rellenar y la tinta invisible (TutorialRellenos).
//  3 · Nodos: tocar y soltar, nodo nuevo, lazo y plastilina (TutorialNodos).
//  4 · Novedades: lo nuevo de las últimas versiones (TutorialNovedades).
public class PanelTutoriales : MonoBehaviour
{
    Transform raiz;
    Tutorial tutorial;

    static string Tx(string en, string es) { return Idioma.Ingles ? en : es; }

    public static void Abrir(Tutorial t)
    {
        if (t == null)
            return;
        var p = t.GetComponent<PanelTutoriales>();
        if (p == null)
            p = t.gameObject.AddComponent<PanelTutoriales>();
        p.tutorial = t;
        p.Mostrar();
    }

    public static void CerrarSiAbierto(Tutorial t)
    {
        var p = t != null ? t.GetComponent<PanelTutoriales>() : null;
        if (p != null)
            p.Cerrar();
    }

    void Mostrar()
    {
        Cerrar();
        var control = ControlManos.Instancia;
        if (control == null || control.Cabeza == null)
            return;
        Transform cab = control.Cabeza;
        Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        raiz = new GameObject("PanelTutoriales").transform;
        raiz.SetPositionAndRotation(cab.position + adelante * 0.45f - Vector3.up * 0.06f, Quaternion.LookRotation(adelante, Vector3.up));

        Fondo(raiz, new Vector2(0.3f, 0.26f), tutorial.materialNegro, tutorial.materialBlanco);
        var titulo = Texto(raiz, Tx("? Tutorials", "? Tutoriales"), new Vector3(0f, 0.084f, -0.004f), new Vector2(0.22f, 0.03f), 0.3f, tutorial.fuenteComic);
        titulo.color = Color.black;
        var b1 = Boton(raiz, Tx("1 · First steps", "1 · Primeros pasos"), new Vector3(0f, 0.04f, 0f), new Vector2(0.25f, 0.034f),
                       tutorial.materialBoton, tutorial.materialBotonMarcado);
        b1.alTocar.AddListener(() =>
        {
            Cerrar();
            tutorial.Empezar();
        });
        var b2 = Boton(raiz, Tx("2 · Fills (bucket and invisible ink)", "2 · Rellenos (cubeta y tinta invisible)"), new Vector3(0f, -0.006f, 0f),
                       new Vector2(0.25f, 0.034f), tutorial.materialBoton, tutorial.materialBotonMarcado);
        b2.alTocar.AddListener(() =>
        {
            Cerrar();
            tutorial.EmpezarRellenos();
        });
        var b3 = Boton(raiz, Tx("3 · Nodes (lasso and clay)", "3 · Nodos (lazo y plastilina)"), new Vector3(0f, -0.052f, 0f),
                       new Vector2(0.25f, 0.034f), tutorial.materialBoton, tutorial.materialBotonMarcado);
        b3.alTocar.AddListener(() =>
        {
            Cerrar();
            tutorial.EmpezarNodos();
        });
        var b4 = Boton(raiz, Tx("4 · What's new", "4 · Novedades"), new Vector3(0f, -0.098f, 0f),
                       new Vector2(0.25f, 0.034f), tutorial.materialBoton, tutorial.materialBotonMarcado);
        b4.alTocar.AddListener(() =>
        {
            Cerrar();
            tutorial.EmpezarNovedades();
        });
        var x = Boton(raiz, "X", new Vector3(0.13f, 0.087f, 0f), new Vector2(0.024f, 0.024f), tutorial.materialBoton, tutorial.materialBotonMarcado);
        x.alTocar.AddListener(Cerrar);
    }

    public void Cerrar()
    {
        if (raiz != null)
            Destroy(raiz.gameObject);
        raiz = null;
    }

    void OnDestroy()
    {
        Cerrar();
    }

    // ---------- Piezas (las usa también TutorialRellenos) ----------

    // Tarjeta blanca con borde negro, un poco detrás (z +).
    public static void Fondo(Transform padre, Vector2 tam, Material borde, Material relleno)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(b.GetComponent<Collider>());
        b.name = "Borde";
        b.transform.SetParent(padre, false);
        b.transform.localPosition = new Vector3(0f, 0f, 0.006f);
        b.transform.localScale = new Vector3(tam.x + 0.006f, tam.y + 0.006f, 0.002f);
        Pintar(b.GetComponent<Renderer>(), borde);
        var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(f.GetComponent<Collider>());
        f.name = "Fondo";
        f.transform.SetParent(padre, false);
        f.transform.localPosition = new Vector3(0f, 0f, 0.004f);
        f.transform.localScale = new Vector3(tam.x, tam.y, 0.002f);
        Pintar(f.GetComponent<Renderer>(), relleno);
    }

    public static BotonTocable Boton(Transform padre, string texto, Vector3 pos, Vector2 tam, Material normal, Material marcado)
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Boton_" + texto;
        cubo.transform.SetParent(padre, false);
        cubo.transform.localPosition = pos;
        cubo.transform.localScale = new Vector3(tam.x, tam.y, 0.008f);
        Pintar(cubo.GetComponent<Renderer>(), normal);
        var b = cubo.AddComponent<BotonTocable>();
        b.materialNormal = normal;
        b.materialMarcado = marcado;
        b.etiqueta = Texto(padre, texto, pos + new Vector3(0f, 0f, -0.0046f), new Vector2(tam.x * 0.92f, tam.y * 0.75f), 0.2f, null);
        b.etiqueta.color = Color.black;
        b.etiqueta.fontStyle = FontStyles.Bold;
        return b;
    }

    public static TextMeshPro Texto(Transform padre, string texto, Vector3 pos, Vector2 tam, float maximo, TMP_FontAsset fuente)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        if (fuente != null)
            t.font = fuente;
        t.text = texto;
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = maximo;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.black;
        t.rectTransform.sizeDelta = tam;
        return t;
    }

    static void Pintar(Renderer r, Material m)
    {
        if (r == null)
            return;
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }
}

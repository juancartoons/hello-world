using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Ventanita de FONDOS 360: se abre al llegar a "360" con el botón Fondo (al lado del menú de la mano).
//  - Miniaturas de todos los fondos 360 (los de la app y tus fotos): toca uno y queda puesto. Puedes probar
//    varios; la ventanita sigue abierta hasta que tocas la X.
//  - Arriba: Cuadrícula, Blanco y Realidad (ponen ese fondo y cierran la ventanita).
//  - < y > si hay más de 16.
public class SelectorFondos : MonoBehaviour
{
    const int Columnas = 4, Filas = 4, PorPagina = Columnas * Filas;
    const float AnchoMini = 0.074f, AltoMini = 0.037f, PasoX = 0.08f, PasoY = 0.056f, ArribaGrilla = 0.072f;

    static SelectorFondos instancia;

    Escenario escenario;
    Dibujo dibujo;
    Material materialPanel, materialBoton, materialBotonMarcado, materialImagen;
    Transform raiz;
    readonly BotonTocable[] celdas = new BotonTocable[PorPagina];
    readonly Renderer[] vistas = new Renderer[PorPagina];
    readonly Material[] materiales = new Material[PorPagina];
    readonly TMP_Text[] nombres = new TMP_Text[PorPagina];
    BotonTocable btnAnterior, btnSiguiente;
    TMP_Text textoPagina;
    int pagina;

    public static bool Abierto => instancia != null && instancia.raiz != null && instancia.raiz.gameObject.activeSelf;

    // junto: el menú de la mano (la ventanita sale a su derecha).
    public static void Abrir(Escenario e, Transform junto, Transform cabeza)
    {
        if (e == null)
            return;
        if (instancia == null)
        {
            instancia = e.GetComponent<SelectorFondos>();
            if (instancia == null)
                instancia = e.gameObject.AddComponent<SelectorFondos>();
        }
        instancia.escenario = e;
        instancia.Mostrar(junto, cabeza);
    }

    public static void Cerrar()
    {
        if (instancia != null && instancia.raiz != null)
            instancia.raiz.gameObject.SetActive(false);
    }

    void BuscarPiezas()
    {
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (nav != null)
        {
            materialPanel = nav.materialPanel;
            materialBoton = nav.materialBoton;
            materialBotonMarcado = nav.materialBotonMarcado;
            materialImagen = nav.materialImagen;
        }
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
    }

    void Mostrar(Transform junto, Transform cabeza)
    {
        if (raiz == null)
        {
            BuscarPiezas();
            Armar();
        }
        raiz.gameObject.SetActive(true);
        if (cabeza != null)
        {
            Vector3 base_ = junto != null ? junto.position : cabeza.position + cabeza.forward * 0.45f;
            Vector3 hacia = base_ - cabeza.position;
            hacia.y = 0f;
            if (hacia.sqrMagnitude < 1e-4f)
                hacia = cabeza.forward;
            Vector3 derecha = Vector3.Cross(Vector3.up, hacia.normalized).normalized;
            Vector3 pos = base_ + derecha * (junto != null ? 0.25f : 0f) + Vector3.up * 0.03f;
            raiz.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cabeza.position, Vector3.up));
        }
        // Se abre en la página del fondo que está puesto.
        int indice = escenario.modo - 3;
        pagina = indice >= 0 ? indice / PorPagina : 0;
        Refrescar();
    }

    int Total => Mathf.Max(0, escenario.CantidadModos - 3);
    int Paginas => Mathf.Max(1, (Total + PorPagina - 1) / PorPagina);

    void Refrescar()
    {
        pagina = Mathf.Clamp(pagina, 0, Paginas - 1);
        for (int i = 0; i < PorPagina; i++)
        {
            int m = 3 + pagina * PorPagina + i;
            bool hay = m < escenario.CantidadModos && escenario.Disponible(m);
            Ver(celdas[i], hay);
            nombres[i].gameObject.SetActive(hay);
            vistas[i].gameObject.SetActive(false);
            if (!hay)
                continue;
            Idioma.Poner(nombres[i], escenario.Nombre(m));
            var mini = escenario.Miniatura(m);
            if (mini != null && materiales[i] != null)
            {
                if (materiales[i].HasProperty("_BaseMap")) materiales[i].SetTexture("_BaseMap", mini);
                if (materiales[i].HasProperty("_MainTex")) materiales[i].SetTexture("_MainTex", mini);
                if (materiales[i].HasProperty("_BaseColor")) materiales[i].SetColor("_BaseColor", Color.white);
                vistas[i].gameObject.SetActive(true);
            }
            celdas[i].Marcar(m == escenario.modo);
        }
        bool varias = Paginas > 1;
        Ver(btnAnterior, varias);
        Ver(btnSiguiente, varias);
        textoPagina.gameObject.SetActive(varias);
        textoPagina.text = (pagina + 1) + " / " + Paginas;
    }

    static void Ver(BotonTocable b, bool ver)
    {
        if (b == null || b.gameObject.activeSelf == ver)
            return;
        b.gameObject.SetActive(ver);
        if (b.etiqueta != null)
            b.etiqueta.gameObject.SetActive(ver);
    }

    void Elegir(int i)
    {
        int m = 3 + pagina * PorPagina + i;
        if (m >= escenario.CantidadModos)
            return;
        escenario.PonerModo(m);
        if (dibujo != null)
            dibujo.Mensaje(escenario.MensajeFondo);
        Refrescar();
    }

    void PonerFijo(int m)
    {
        escenario.PonerModo(m);
        if (dibujo != null)
            dibujo.Mensaje(escenario.MensajeFondo);
        Cerrar();
    }

    void Armar()
    {
        raiz = new GameObject("FondosVentana").transform;
        raiz.SetParent(transform, false);

        var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "Fondo";
        Destroy(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(raiz, false);
        fondo.transform.localPosition = new Vector3(0f, -0.005f, 0.006f);
        fondo.transform.localScale = new Vector3(0.34f, 0.285f, 1f);
        Pintar(fondo.GetComponent<Renderer>(), materialPanel);

        Texto(raiz, "Fondos 360", new Vector3(-0.125f, 0.118f, -0.002f), new Vector2(0.08f, 0.016f), 0.15f).fontStyle = FontStyles.Bold;
        Boton(raiz, "Cuadrícula", new Vector3(-0.035f, 0.118f, 0f), new Vector2(0.07f, 0.022f)).alTocar.AddListener(() => PonerFijo(0));
        Boton(raiz, "Blanco", new Vector3(0.04f, 0.118f, 0f), new Vector2(0.06f, 0.022f)).alTocar.AddListener(() => PonerFijo(1));
        Boton(raiz, "Realidad", new Vector3(0.105f, 0.118f, 0f), new Vector2(0.06f, 0.022f)).alTocar.AddListener(() => PonerFijo(2));
        Boton(raiz, "X", new Vector3(0.155f, 0.118f, 0f), new Vector2(0.022f, 0.022f)).alTocar.AddListener(Cerrar);

        for (int i = 0; i < PorPagina; i++)
        {
            int col = i % Columnas, fila = i / Columnas;
            float x = -0.12f + col * PasoX;
            float y = ArribaGrilla - fila * PasoY;
            int indice = i;
            celdas[i] = Boton(raiz, "", new Vector3(x, y, 0f), new Vector2(AnchoMini + 0.004f, 0.051f));
            celdas[i].alTocar.AddListener(() => Elegir(indice));
            var vista = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vista.name = "Miniatura";
            Destroy(vista.GetComponent<Collider>());
            vista.transform.SetParent(raiz, false);
            vista.transform.localPosition = new Vector3(x, y + 0.005f, -0.005f);
            vista.transform.localScale = new Vector3(AnchoMini, AltoMini, 1f);
            materiales[i] = materialImagen != null ? new Material(materialImagen) : null;
            vistas[i] = vista.GetComponent<Renderer>();
            Pintar(vistas[i], materiales[i]);
            nombres[i] = Texto(raiz, "", new Vector3(x, y - 0.0185f, -0.005f), new Vector2(AnchoMini, 0.009f), 0.08f);
        }

        btnAnterior = Boton(raiz, "<", new Vector3(-0.035f, -0.13f, 0f), new Vector2(0.024f, 0.018f));
        btnAnterior.alTocar.AddListener(() => { pagina--; Refrescar(); });
        textoPagina = Texto(raiz, "1 / 1", new Vector3(0f, -0.13f, -0.002f), new Vector2(0.04f, 0.014f), 0.12f);
        btnSiguiente = Boton(raiz, ">", new Vector3(0.035f, -0.13f, 0f), new Vector2(0.024f, 0.018f));
        btnSiguiente.alTocar.AddListener(() => { pagina++; Refrescar(); });
    }

    BotonTocable Boton(Transform padre, string texto, Vector3 pos, Vector2 tam)
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Boton_" + texto;
        cubo.transform.SetParent(padre, false);
        cubo.transform.localPosition = pos;
        cubo.transform.localScale = new Vector3(tam.x, tam.y, 0.006f);
        Pintar(cubo.GetComponent<Renderer>(), materialBoton);
        var b = cubo.AddComponent<BotonTocable>();
        b.materialNormal = materialBoton;
        b.materialMarcado = materialBotonMarcado;
        b.etiqueta = Texto(padre, texto, pos + new Vector3(0f, 0f, -0.0036f), new Vector2(tam.x * 0.92f, Mathf.Min(tam.y * 0.8f, 0.016f)), 0.16f);
        b.etiqueta.fontStyle = FontStyles.Bold;
        return b;
    }

    static TMP_Text Texto(Transform padre, string texto, Vector3 pos, Vector2 tam, float maximo)
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
        t.color = Color.black;
        t.rectTransform.sizeDelta = tam;
        return t;
    }

    static void Pintar(Renderer r, Material m)
    {
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
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

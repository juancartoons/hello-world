using System.Collections.Generic;
using TMPro;
using UnityEngine;

// PERSIANAS (v38): una pastillita de vidrio que tocas con el índice derecho y jalas para abrir algo, como una persiana.
//  - ARRIBA: "Capa 2 · de 5". Tócala y BAJA la mano: se despliegan las capas.
//  - ABAJO: "Fotograma 12 · 0:01" (y una rayita de cuánto va de la animación). Tócala y SUBE la mano: sale la mesa de
//    trabajo (reproductor y línea de tiempo, con sus 3 pestañas guardadas).
//  - Al lado, un circulito: LLENO = persiana (con la mano); HUECO = solo con la mirada (mirar arriba / abajo).
//  - Sin texto ni circulito también sirve de "tirador" para cerrar: debajo de las capas (súbelo) y debajo de la mesa
//    (bájalo).
// Mientras la jalas, una lámina de vidrio sale de la pastillita siguiendo tu dedo. Si la sueltas a más de la mitad,
// se abre; si no, vuelve.
public class Persiana : MonoBehaviour
{
    public delegate void FuncionPose(Transform cabeza, out Vector3 pos, out Quaternion rot);

    public FuncionPose pose;               // dónde va (se pregunta cada cuadro)
    public System.Func<bool> visible;      // si se ve
    public System.Func<string> texto;      // su texto (null = tirador, sin texto)
    public System.Func<float> progreso;    // la rayita de abajo, de 0 a 1 (null = sin rayita)
    public System.Action alAbrir;          // al jalarla lo suficiente
    public System.Func<bool> puedeJalar;   // si ahora se puede agarrar (null = siempre; por ejemplo, no mientras dibujas)
    public int sentido = -1;               // -1: se jala hacia abajo; +1: hacia arriba
    public string clave;                   // dónde se guarda el circulito (null = sin circulito)
    public float ancho = 0.085f, alto = 0.022f;

    const float Recorrido = 0.09f;         // cuánto hay que jalar (metros)

    static readonly Dictionary<string, bool> modos = new Dictionary<string, bool>();

    // ¿Esa persiana está en modo "con la mano"? (Si no, se usa la mirada.) Al principio: con la mano.
    public static bool Modo(string clave)
    {
        if (string.IsNullOrEmpty(clave))
            return true;
        bool m;
        if (!modos.TryGetValue(clave, out m))
        {
            m = PlayerPrefs.GetInt(clave, 1) == 1;
            modos[clave] = m;
        }
        return m;
    }

    static void PonerModo(string clave, bool m)
    {
        modos[clave] = m;
        PlayerPrefs.SetInt(clave, m ? 1 : 0);
        PlayerPrefs.Save();
    }

    Transform lamina, circulo, rellenoCirculo, barra;
    Renderer rFondo, rLamina;
    TMP_Text etiqueta;
    MaterialPropertyBlock bloque;
    readonly List<Material> materiales = new List<Material>();
    readonly List<Mesh> mallas = new List<Mesh>();
    bool armada, agarrada, esperarSalir, esperarCirculo;
    Vector3 inicio, eje;
    float avance, ver, atencion = 0.5f;
    string textoPuesto = "";

    void Armar(ControlManos control)
    {
        armada = true;
        var malla = CrearPastilla(ancho, alto);
        mallas.Add(malla);
        Material vidrio = null;
        if (control.materialVidrio != null)
        {
            vidrio = new Material(control.materialVidrio);
            if (vidrio.HasProperty("_Esmerilado"))
                vidrio.SetFloat("_Esmerilado", 0.7f);
            if (vidrio.HasProperty("_Opacidad"))
                vidrio.SetFloat("_Opacidad", 0.8f);
            materiales.Add(vidrio);
        }
        else
        {
            vidrio = MaterialColor(control, new Color(0.95f, 0.95f, 0.97f));
        }
        Pieza("Fondo", malla, vidrio, Vector3.zero, Vector3.one, out rFondo);
        lamina = Pieza("Lamina", malla, vidrio, Vector3.zero, Vector3.one, out rLamina);
        lamina.gameObject.SetActive(false);
        if (texto != null)
        {
            var go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, progreso != null ? alto * 0.08f : 0f, -0.001f);
            var t = go.AddComponent<TextMeshPro>();
            t.enableAutoSizing = true;
            t.fontSizeMin = 0.01f;
            t.fontSizeMax = 0.2f;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            t.color = new Color(0.12f, 0.12f, 0.16f);
            t.rectTransform.sizeDelta = new Vector2(ancho * 0.86f, alto * (progreso != null ? 0.55f : 0.7f));
            etiqueta = t;
        }
        Renderer r;
        if (progreso != null)
            barra = Pieza("Barra", ControlManos.MallaDisco(), MaterialColor(control, new Color(0.35f, 0.55f, 0.85f)),
                          new Vector3(0f, -alto * 0.3f, -0.0008f), new Vector3(0.001f, 0.0022f, 1f), out r);
        if (!string.IsNullOrEmpty(clave))
        {
            Vector3 c = new Vector3(ancho * 0.5f + 0.013f, 0f, -0.0005f);
            circulo = Pieza("Circulo", ControlManos.MallaAnillo(), MaterialColor(control, new Color(0.35f, 0.55f, 0.85f)), c,
                            Vector3.one * 0.016f, out r);
            rellenoCirculo = Pieza("Lleno", ControlManos.MallaDisco(), MaterialColor(control, new Color(0.62f, 0.8f, 1f)),
                                   c + new Vector3(0f, 0f, 0.0003f), Vector3.one * 0.0105f, out r);
        }
    }

    Material MaterialColor(ControlManos control, Color c)
    {
        if (control.materialNodo == null)
            return null;
        var m = new Material(control.materialNodo);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        materiales.Add(m);
        return m;
    }

    Transform Pieza(string nombre, Mesh malla, Material m, Vector3 pos, Vector3 escala, out Renderer r)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        r = go.AddComponent<MeshRenderer>();
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    void LateUpdate()
    {
        var control = ControlManos.Instancia;
        var cab = control != null ? control.Cabeza : null;
        bool quiere = cab != null && pose != null && (visible == null || visible());
        ver = Mathf.MoveTowards(ver, quiere ? 1f : 0f, Time.deltaTime * 5f);
        if (!quiere && ver <= 0.01f)
        {
            agarrada = false;
            avance = 0f;
            Mostrar(false);
            return;
        }
        if (!armada)
            Armar(control);
        Mostrar(true);
        if (!agarrada)
        {
            Vector3 p;
            Quaternion q;
            pose(cab, out p, out q);
            transform.SetPositionAndRotation(p, q);
        }
        transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, ver);

        if (etiqueta != null)
        {
            string s = texto();
            if (s != textoPuesto)
            {
                textoPuesto = s;
                Idioma.Poner(etiqueta, s);
            }
        }
        if (barra != null)
        {
            float k = Mathf.Clamp01(progreso());
            float largo = ancho * 0.72f;
            barra.localScale = new Vector3(Mathf.Max(0.001f, largo * k), 0.0022f, 1f);
            barra.localPosition = new Vector3(-largo * 0.5f + largo * k * 0.5f, -alto * 0.3f, -0.0008f);
        }
        bool modo = Modo(clave);
        if (rellenoCirculo != null && rellenoCirculo.gameObject.activeSelf != modo)
            rellenoCirculo.gameObject.SetActive(modo);

        // Discreta si no la miras; firme si la miras o acercas el dedo.
        bool dedoOk = control.Der.valida && quiere;
        Vector3 dedo = control.Der.indice;
        bool mira = Vector3.Angle(cab.forward, transform.position - cab.position) < 14f;
        bool cerca = dedoOk && Vector3.Distance(dedo, transform.position) < 0.12f;
        atencion = Mathf.MoveTowards(atencion, mira || cerca || agarrada ? 1f : 0f, Time.deltaTime * 3f);
        PonerAtencion(atencion * ver);

        // El circulito: tocarlo cambia entre persiana (lleno) y mirada (hueco).
        if (circulo != null && dedoOk)
        {
            float d = Vector3.Distance(dedo, circulo.position);
            if (!esperarCirculo && d < 0.011f)
            {
                esperarCirculo = true;
                PonerModo(clave, !modo);
                control.SonidoBurbuja(circulo.position);
                if (control.dibujo != null)
                    control.dibujo.Mensaje(!modo ? "Persiana: tócala y jálala con el dedo" : "Con la mirada: mira arriba o abajo");
            }
            else if (d > 0.022f)
            {
                esperarCirculo = false;
            }
        }

        // Jalar.
        if (!agarrada)
        {
            avance = Mathf.MoveTowards(avance, 0f, Time.deltaTime * 4f);
            if (esperarSalir && (!dedoOk || !Cerca(dedo, 0.04f)))
                esperarSalir = false;
            if (modo && dedoOk && !esperarSalir && (puedeJalar == null || puedeJalar()) && Sobre(dedo))
            {
                agarrada = true;
                inicio = dedo;
                eje = transform.up * sentido;
            }
        }
        else if (!dedoOk)
        {
            Soltar();
        }
        else
        {
            float d = Vector3.Dot(dedo - inicio, eje);
            avance = Mathf.Clamp01(d / Recorrido);
            Vector3 deLado = dedo - inicio - eje * d;
            if (avance >= 1f)
                Abrir(control);
            else if (deLado.magnitude > 0.07f || d < -0.03f)
                Soltar();
        }
        // La lámina que sale siguiendo tu dedo.
        bool verLamina = avance > 0.01f;
        if (lamina.gameObject.activeSelf != verLamina)
            lamina.gameObject.SetActive(verLamina);
        if (verLamina)
        {
            float extra = avance * Recorrido;
            lamina.localScale = new Vector3(1f, 1f + extra / alto, 1f);
            lamina.localPosition = new Vector3(0f, sentido * extra * 0.5f, 0.0004f);
        }
    }

    void Soltar()
    {
        if (agarrada && avance > 0.5f)
        {
            var control = ControlManos.Instancia;
            if (control != null)
            {
                Abrir(control);
                return;
            }
        }
        agarrada = false;
    }

    void Abrir(ControlManos control)
    {
        agarrada = false;
        avance = 0f;
        esperarSalir = true;
        control.SonidoBurbuja(transform.position);
        if (alAbrir != null)
            alAbrir();
    }

    // ¿El dedo está sobre la pastillita? (en sus coordenadas, con un poquito de margen)
    bool Sobre(Vector3 dedo)
    {
        Vector3 l = transform.InverseTransformPoint(dedo);
        return Mathf.Abs(l.x) < ancho * 0.5f + 0.008f && Mathf.Abs(l.y) < alto * 0.5f + 0.01f && Mathf.Abs(l.z) < 0.025f;
    }

    bool Cerca(Vector3 dedo, float margen)
    {
        Vector3 l = transform.InverseTransformPoint(dedo);
        return Mathf.Abs(l.x) < ancho * 0.5f + margen && Mathf.Abs(l.y) < alto * 0.5f + margen && Mathf.Abs(l.z) < margen + 0.02f;
    }

    void Mostrar(bool si)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var c = transform.GetChild(i).gameObject;
            if (c == (lamina != null ? lamina.gameObject : null))
                continue;
            if (c == (rellenoCirculo != null ? rellenoCirculo.gameObject : null))
                continue;
            if (c.activeSelf != si)
                c.SetActive(si);
        }
        if (!si)
        {
            if (lamina != null && lamina.gameObject.activeSelf)
                lamina.gameObject.SetActive(false);
            if (rellenoCirculo != null && rellenoCirculo.gameObject.activeSelf)
                rellenoCirculo.gameObject.SetActive(false);
        }
    }

    void PonerAtencion(float a)
    {
        if (bloque == null)
            bloque = new MaterialPropertyBlock();
        foreach (var r in new[] { rFondo, rLamina })
        {
            if (r == null || r.sharedMaterial == null || !r.sharedMaterial.HasProperty("_Atencion"))
                continue;
            r.GetPropertyBlock(bloque);
            bloque.SetFloat("_Atencion", a);
            r.SetPropertyBlock(bloque);
        }
        if (etiqueta != null)
        {
            float ta = Mathf.Lerp(0.5f, 1f, a) * ver;
            if (Mathf.Abs(etiqueta.alpha - ta) > 0.01f)
                etiqueta.alpha = ta;
        }
    }

    // Una pastilla (rectángulo con las puntas redondas), con UV de 0 a 1 (para el borde del vidrio).
    public static Mesh CrearPastilla(float ancho, float alto)
    {
        const int pasos = 10;
        float r = alto * 0.5f, medio = Mathf.Max(0f, ancho * 0.5f - r);
        var puntos = new List<Vector3>();
        for (int k = 0; k <= pasos; k++)            // punta derecha (de abajo hacia arriba)
        {
            float a = -Mathf.PI * 0.5f + Mathf.PI * k / pasos;
            puntos.Add(new Vector3(medio + Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
        for (int k = 0; k <= pasos; k++)            // punta izquierda (de arriba hacia abajo)
        {
            float a = Mathf.PI * 0.5f + Mathf.PI * k / pasos;
            puntos.Add(new Vector3(-medio + Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
        var v = new Vector3[puntos.Count + 1];
        var uv = new Vector2[v.Length];
        var n = new Vector3[v.Length];
        v[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < puntos.Count; i++)
        {
            v[i + 1] = puntos[i];
            uv[i + 1] = new Vector2(puntos[i].x / ancho + 0.5f, puntos[i].y / alto + 0.5f);
        }
        for (int i = 0; i < n.Length; i++)
            n[i] = new Vector3(0f, 0f, -1f);
        var tri = new int[puntos.Count * 3];
        for (int i = 0; i < puntos.Count; i++)
        {
            tri[i * 3] = 0;
            tri[i * 3 + 1] = 1 + (i + 1) % puntos.Count;
            tri[i * 3 + 2] = 1 + i;
        }
        var m = new Mesh { name = "Pastilla" };
        m.vertices = v;
        m.uv = uv;
        m.normals = n;
        m.triangles = tri;
        m.RecalculateBounds();
        return m;
    }

    void OnDestroy()
    {
        foreach (var m in materiales)
            if (m != null)
                Destroy(m);
        foreach (var m in mallas)
            if (m != null)
                Destroy(m);
    }
}

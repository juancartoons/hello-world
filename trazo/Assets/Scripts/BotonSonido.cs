using System.Collections.Generic;
using UnityEngine;

// Arriba a la derecha de lo que miras (te sigue con calma), siempre visible:
//  - El PARLANTE (abajo): silencia TODO el sonido de la app (y se recuerda).
//      Con sonido: verde azulado con sus ondas. En silencio: gris oscuro con una X.
//  - Encima, el CARRUSEL de instrumentos: en el centro (grande, amarillo) el que suena; a los lados,
//    más pequeños, más al fondo y un poco transparentes, el anterior y el siguiente.
//    Arrastra con la punta del índice derecho a la izquierda o a la derecha para cambiar (como en la Mac),
//    o toca uno de los de al lado. Al soltar, se acomoda y suena una notita del instrumento.
// Mientras el dedo está cerca, la bolita de la tinta o del borrador se esconde (y no dibuja ni borra),
// para que se vea bien lo que tocas.
public class BotonSonido : MonoBehaviour
{
    const string ClaveSilencio = "jcartoons_silencio";
    static readonly Color ColorActivo = new Color(0.16f, 0.68f, 0.64f);
    static readonly Color ColorApagado = new Color(0.22f, 0.22f, 0.24f);
    // Dónde queda respecto a tu cabeza: a la derecha, arriba y adelante (metros).
    static readonly Vector3 Lugar = new Vector3(0.16f, 0.13f, 0.45f);
    // El parlante: un poco más abajo, para que el carrusel se vea cómodo encima.
    static readonly Vector3 LugarParlante = new Vector3(0f, -0.042f, 0f);
    const float TamanoParlante = 0.04f;
    const float RadioToque = 0.026f;
    // El carrusel.
    const float AlturaCarrusel = 0.006f;
    const float DiametroCentro = 0.032f;
    const float EscalaLados = 0.5f;     // los de al lado, a la mitad
    const float Espacio = 0.033f;       // distancia entre el centro y los de al lado
    const float FondoLados = 0.012f;    // cuánto más atrás están los de al lado (3D)
    const float AlfaLados = 0.7f;

    static int silencio = -1; // -1 = aún no leído

    // El dedo índice derecho está cerca del parlante o del carrusel (ControlManos esconde su bolita).
    public static bool DedoCerca { get; private set; }

    ControlManos control;
    Transform parlante, ondas, equis;
    Material matIcono, matBaseIconos;
    readonly List<Material> materiales = new List<Material>();
    readonly List<Mesh> mallas = new List<Mesh>();
    readonly List<Texture2D> texturas = new List<Texture2D>();
    Quaternion giroSuave = Quaternion.identity;
    bool colocado, dedoEnParlante;
    float bloqueadoHasta;

    // Carrusel
    Transform[] items;
    Material[] matItems;
    Texture2D[] texNormal, texCentro;
    int seleccion;
    float vista;                 // dónde está "mirando" el carrusel (0 = primer ícono); se mueve suave
    bool arrastrando, movio;
    float xInicio, vistaInicio;
    int tocadoAlEntrar = -1;
    int ultimoCentro = -1;

    public static bool Silenciado
    {
        get
        {
            if (silencio < 0)
                silencio = PlayerPrefs.GetInt(ClaveSilencio, 0) != 0 ? 1 : 0;
            return silencio == 1;
        }
    }

    // Pone el volumen general según lo guardado (se llama al empezar la app).
    public static void AplicarSilencio()
    {
        AudioListener.volume = Silenciado ? 0f : 1f;
    }

    public static void Alternar()
    {
        silencio = Silenciado ? 0 : 1;
        PlayerPrefs.SetInt(ClaveSilencio, silencio);
        PlayerPrefs.Save();
        AplicarSilencio();
    }

    public static BotonSonido Crear(ControlManos control, Material baseMaterial, Material materialIconos)
    {
        var go = new GameObject("BotonSonido");
        var b = go.AddComponent<BotonSonido>();
        b.control = control;
        b.Armar(baseMaterial, materialIconos);
        return b;
    }

    void Armar(Material baseMaterial, Material materialIconos)
    {
        if (baseMaterial == null)
            return;
        parlante = new GameObject("Parlante").transform;
        parlante.SetParent(transform, false);
        parlante.localPosition = LugarParlante;
        parlante.localScale = Vector3.one * TamanoParlante;
        var blanco = NuevoMaterial(baseMaterial, Color.white);
        var negro = NuevoMaterial(baseMaterial, Color.black);
        matIcono = NuevoMaterial(baseMaterial, ColorActivo);
        // Fondo redondo blanco con borde negro (se ve sobre cualquier fondo).
        Parte(parlante, "Borde", Disco(0.62f), negro, 0.1f);
        Parte(parlante, "Fondo", Disco(0.56f), blanco, 0.05f);
        // El parlante: una cajita y la bocina (trapecio).
        var forma = new List<Vector2[]>
        {
            new[] { new Vector2(-0.3f, -0.11f), new Vector2(-0.3f, 0.11f), new Vector2(-0.15f, 0.11f), new Vector2(-0.15f, -0.11f) },
            new[] { new Vector2(-0.15f, -0.11f), new Vector2(-0.15f, 0.11f), new Vector2(0.03f, 0.28f), new Vector2(0.03f, -0.28f) },
        };
        Parte(parlante, "Bocina", Malla(forma), matIcono, 0f);
        // Ondas del sonido (dos arcos).
        var arcos = new List<Vector2[]>();
        Arco(arcos, 0.13f, 0.05f);
        Arco(arcos, 0.27f, 0.05f);
        ondas = Parte(parlante, "Ondas", Malla(arcos), matIcono, 0f);
        ondas.localPosition = new Vector3(0.04f, 0f, 0f);
        // X del silencio.
        var x = new List<Vector2[]>();
        Barra(x, new Vector2(-0.1f, -0.1f), new Vector2(0.1f, 0.1f), 0.05f);
        Barra(x, new Vector2(-0.1f, 0.1f), new Vector2(0.1f, -0.1f), 0.05f);
        equis = Parte(parlante, "Equis", Malla(x), matIcono, 0f);
        equis.localPosition = new Vector3(0.2f, 0f, 0f);
        Refrescar();
        ArmarCarrusel(materialIconos);
    }

    // ---------- Carrusel de instrumentos ----------

    void ArmarCarrusel(Material materialIconos)
    {
        matBaseIconos = materialIconos != null ? new Material(materialIconos) : MaterialTransparente();
        if (matBaseIconos == null)
            return;
        materiales.Add(matBaseIconos);
        int n = Sonidos.Instrumentos.Length;
        items = new Transform[n];
        matItems = new Material[n];
        texNormal = new Texture2D[n];
        texCentro = new Texture2D[n];
        var cuadro = Cuadro();
        for (int k = 0; k < n; k++)
        {
            var m = new Material(matBaseIconos);
            materiales.Add(m);
            matItems[k] = m;
            items[k] = Parte(transform, "Instrumento" + k, cuadro, m, 0f);
            items[k].gameObject.SetActive(false);
        }
        seleccion = control != null ? control.InstrumentoActivo : 0;
        vista = seleccion;
    }

    // Si la escena no trae el material de los íconos, se arma uno transparente aquí.
    static Material MaterialTransparente()
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null)
            sh = Shader.Find("Unlit/Transparent");
        if (sh == null)
            return null;
        var m = new Material(sh);
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    }

    Texture2D Textura(int k, bool centro)
    {
        var lista = centro ? texCentro : texNormal;
        if (lista[k] == null)
        {
            lista[k] = IconosSonido.Crear(k, centro);
            texturas.Add(lista[k]);
        }
        return lista[k];
    }

    // Cada ícono según su distancia al centro: el del centro grande y amarillo; los de al lado más chicos,
    // más atrás y un poco transparentes; los demás no se ven.
    void PintarCarrusel()
    {
        if (items == null)
            return;
        for (int k = 0; k < items.Length; k++)
        {
            float pos = k - vista;
            float a = Mathf.Abs(pos);
            float alfa = a <= 1f ? Mathf.Lerp(1f, AlfaLados, a) : AlfaLados * Mathf.Clamp01(1f - (a - 1f) * 2f);
            bool ver = alfa > 0.01f;
            if (items[k].gameObject.activeSelf != ver)
                items[k].gameObject.SetActive(ver);
            if (!ver)
                continue;
            float cerca = Mathf.Min(1f, a);
            items[k].localPosition = new Vector3(pos * Espacio, AlturaCarrusel, FondoLados * cerca);
            items[k].localScale = Vector3.one * DiametroCentro * Mathf.Lerp(1f, EscalaLados, cerca);
            var m = matItems[k];
            var tex = Textura(k, a < 0.5f);
            if (m.mainTexture != tex)
            {
                m.mainTexture = tex;
                if (m.HasProperty("_BaseMap"))
                    m.SetTexture("_BaseMap", tex);
            }
            var c = new Color(1f, 1f, 1f, alfa);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color"))
                m.SetColor("_Color", c);
        }
    }

    // El dedo en el carrusel: arrastrar = mover los íconos; soltar = se acomoda en el más cercano.
    void ActualizarCarrusel(bool dedoValido, Vector3 local, float dt)
    {
        if (items == null)
            return;
        int n = items.Length;
        bool dentro = dedoValido && Mathf.Abs(local.x) < 0.065f && Mathf.Abs(local.y - AlturaCarrusel) < 0.02f
                      && local.z > -0.03f && local.z < 0.03f;
        if (dentro && !arrastrando)
        {
            arrastrando = true;
            movio = false;
            xInicio = local.x;
            vistaInicio = vista;
            // ¿Entró justo sobre uno de los de al lado? (para elegirlo con un toque)
            tocadoAlEntrar = -1;
            for (int k = 0; k < n; k++)
            {
                float px = (k - vista) * Espacio;
                if (Mathf.Abs(k - vista) <= 1.2f && Mathf.Abs(local.x - px) < 0.011f)
                    tocadoAlEntrar = k;
            }
        }
        if (arrastrando && dentro)
        {
            float dx = local.x - xInicio;
            if (Mathf.Abs(dx) > 0.006f)
                movio = true;
            // Arrastrar a la izquierda = ir al siguiente. En las puntas cuesta más (como un resorte).
            float v = vistaInicio - dx / Espacio;
            if (v < 0f) v *= 0.3f;
            if (v > n - 1) v = n - 1 + (v - (n - 1)) * 0.3f;
            vista = v;
        }
        else if (arrastrando)
        {
            // Soltó (el dedo salió).
            arrastrando = false;
            int nuevo = !movio && tocadoAlEntrar >= 0 ? tocadoAlEntrar : Mathf.RoundToInt(vista);
            nuevo = Mathf.Clamp(nuevo, 0, n - 1);
            if (nuevo != seleccion || (control != null && nuevo != control.InstrumentoActivo))
            {
                seleccion = nuevo;
                if (control != null)
                    control.ElegirInstrumento(nuevo);
            }
        }
        // El que queda en el centro (aunque sea mientras arrastras) se empieza a preparar en segundo plano,
        // así al soltarlo ya suena.
        int centro = Mathf.Clamp(Mathf.RoundToInt(vista), 0, n - 1);
        if (centro != ultimoCentro)
        {
            ultimoCentro = centro;
            Sonidos.PrepararInstrumento(centro);
        }
        if (!arrastrando)
        {
            // Si el instrumento cambió por otro lado (el tutorial), el carrusel lo sigue.
            if (control != null)
                seleccion = control.InstrumentoActivo;
            vista = Mathf.Lerp(vista, seleccion, 1f - Mathf.Exp(-14f * dt));
            if (Mathf.Abs(vista - seleccion) < 0.001f)
                vista = seleccion;
        }
        PintarCarrusel();
    }

    void Refrescar()
    {
        bool sil = Silenciado;
        if (ondas != null)
            ondas.gameObject.SetActive(!sil);
        if (equis != null)
            equis.gameObject.SetActive(sil);
        if (matIcono != null)
        {
            Color c = sil ? ColorApagado : ColorActivo;
            if (matIcono.HasProperty("_BaseColor"))
                matIcono.SetColor("_BaseColor", c);
            if (matIcono.HasProperty("_Color"))
                matIcono.SetColor("_Color", c);
        }
    }

    void LateUpdate()
    {
        if (control == null)
            control = ControlManos.Instancia;
        if (control == null || control.Cabeza == null || matIcono == null)
        {
            DedoCerca = false;
            return;
        }
        Transform cabeza = control.Cabeza;
        // Sigue tu mirada (solo hacia los lados y arriba/abajo, sin inclinarse), con calma.
        Vector3 adelante = cabeza.forward;
        if (Mathf.Abs(Vector3.Dot(adelante, Vector3.up)) > 0.97f)
            adelante = Vector3.ProjectOnPlane(cabeza.up, Vector3.up);
        Quaternion giro = Quaternion.LookRotation(adelante, Vector3.up);
        if (!colocado)
        {
            giroSuave = giro;
            colocado = true;
        }
        // Mientras arrastras el carrusel se queda quieto (para que no se te escape).
        if (!arrastrando)
            giroSuave = Quaternion.Slerp(giroSuave, giro, 1f - Mathf.Exp(-4f * Time.deltaTime));
        Vector3 pos = cabeza.position + giroSuave * Lugar;
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cabeza.position, Vector3.up));

        bool dedo = control.Der.valida && control.GestoIzq != ControlManos.Gesto.Transformar;
        Vector3 punta = control.Der.indice;
        Vector3 local = transform.InverseTransformPoint(punta);
        DedoCerca = dedo && Mathf.Abs(local.x) < 0.085f && local.y > -0.075f && local.y < 0.05f && Mathf.Abs(local.z) < 0.07f;
        ActualizarCarrusel(dedo, local, Time.deltaTime);

        // Tocar el parlante con la punta del índice derecho.
        bool toca = dedo && Vector3.Distance(punta, parlante.position) < RadioToque;
        if (toca && !dedoEnParlante && Time.time >= bloqueadoHasta)
        {
            bloqueadoHasta = Time.time + 0.6f;
            Alternar();
            Refrescar();
            if (!Silenciado)
            {
                var fuente = GetComponent<AudioSource>();
                if (fuente == null)
                {
                    fuente = gameObject.AddComponent<AudioSource>();
                    fuente.playOnAwake = false;
                    fuente.spatialBlend = 0f;
                }
                fuente.PlayOneShot(Sonidos.Burbuja, 0.6f);
            }
        }
        dedoEnParlante = toca;
    }

    // ---------- Mallas sencillas (planas, se ven por los dos lados) ----------

    Material NuevoMaterial(Material baseMaterial, Color c)
    {
        var m = new Material(baseMaterial);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        materiales.Add(m);
        return m;
    }

    // "atras" = cuánto más lejos de ti (en el tamaño del ícono): así las capas no se pelean.
    Transform Parte(Transform padre, string nombre, Mesh malla, Material m, float atras)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3(0f, 0f, atras);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    Mesh Disco(float radio)
    {
        const int lados = 28;
        var pts = new Vector2[lados];
        for (int i = 0; i < lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio;
        }
        return Malla(new List<Vector2[]> { pts });
    }

    // Arco de -50° a 50° (una "onda" de sonido), hecho con cuadritos.
    static void Arco(List<Vector2[]> partes, float radio, float grosor)
    {
        const int pasos = 8;
        for (int i = 0; i < pasos; i++)
        {
            float a0 = Mathf.Lerp(-50f, 50f, i / (float)pasos) * Mathf.Deg2Rad;
            float a1 = Mathf.Lerp(-50f, 50f, (i + 1) / (float)pasos) * Mathf.Deg2Rad;
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
            Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            partes.Add(new[] { d0 * (radio - grosor * 0.5f), d0 * (radio + grosor * 0.5f), d1 * (radio + grosor * 0.5f), d1 * (radio - grosor * 0.5f) });
        }
    }

    static void Barra(List<Vector2[]> partes, Vector2 a, Vector2 b, float grosor)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x) * grosor * 0.5f;
        partes.Add(new[] { a - n, a + n, b + n, b - n });
    }

    // Un cuadrado de 1 x 1 (para los íconos), por los dos lados.
    Mesh Cuadro()
    {
        var m = new Mesh { name = "CuadroIcono" };
        m.SetVertices(new List<Vector3> { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) });
        m.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
        m.SetTriangles(new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 }, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        mallas.Add(m);
        return m;
    }

    // Cada parte es un polígono convexo (se rellena en abanico), por los dos lados.
    Mesh Malla(List<Vector2[]> partes)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        foreach (var p in partes)
        {
            int b = v.Count;
            foreach (var q in p)
                v.Add(new Vector3(q.x, q.y, 0f));
            for (int i = 1; i < p.Length - 1; i++)
            {
                t.Add(b); t.Add(b + i); t.Add(b + i + 1);
                t.Add(b); t.Add(b + i + 1); t.Add(b + i);
            }
        }
        var m = new Mesh { name = "IconoSonido" };
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        mallas.Add(m);
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
        foreach (var t in texturas)
            if (t != null)
                Destroy(t);
        DedoCerca = false;
    }
}

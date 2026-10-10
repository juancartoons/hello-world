using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Viñeta de cómic estilo Spider-Verse: relleno amarillo con trama de puntitos, borde negro que tiembla
// (3 dibujos, 8 veces por segundo) y una sombra negra DE VERDAD más atrás (se siente la profundidad en 3D).
// Dos usos:
//  - Viñeta fija (tutorial): tamaño "ancho" x "alto", con su texto adentro (Armar crea todo).
//  - Fondo de un texto que ya existe (avisos y etiquetas de los gestos): se pone en el mismo objeto del texto,
//    se ajusta sola al tamaño de las letras y se esconde cuando no hay texto.
public class VinetaComic : MonoBehaviour
{
    public Material materialTrama, materialNegro;
    [Tooltip("Letra de cómic (Bangers). Si falta, usa la normal")]
    public TMP_FontAsset fuente;
    [Tooltip("Si está: la viñeta es el fondo de este texto y se ajusta a su tamaño")]
    public TMP_Text texto;
    public bool ajustarAlTexto;
    public float ancho = 0.24f, alto = 0.075f;
    [Tooltip("Colita: 0 ninguna, -1 hacia abajo, 1 hacia arriba")]
    public int cola;
    public float profundidadSombra = 0.03f;
    public float bordeGrosor = 0.006f;

    Mesh mallaRelleno, mallaBorde, mallaSombra;
    Transform relleno, borde, sombra;
    int variante = -1;
    float anchoHecho = -1f, altoHecho = -1f;
    int colaHecha = 99;
    Vector3 centro;
    readonly List<Vector3> contorno = new List<Vector3>();
    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<int> tris = new List<int>();

    public TMP_Text Texto => texto;

    // Viñeta fija con su propio texto (tutorial).
    public void Armar(float anchoBase, float altoBase)
    {
        ancho = anchoBase;
        alto = altoBase;
        ajustarAlTexto = false;
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.002f);
        var t = go.AddComponent<TextMeshPro>();
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.05f;
        t.fontSizeMax = 0.3f;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = new Vector2(ancho - 0.026f, alto - 0.016f);
        texto = t;
        PrepararTexto();
        AsegurarMallas();
    }

    void Start()
    {
        if (texto != null && ajustarAlTexto)
            PrepararTexto();
        AsegurarMallas();
    }

    void PrepararTexto()
    {
        if (texto == null)
            return;
        texto.color = Color.black;
        if (fuente != null)
        {
            texto.font = fuente;
            texto.fontStyle = FontStyles.UpperCase;
        }
        else
        {
            texto.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        }
    }

    void AsegurarMallas()
    {
        if (mallaRelleno != null)
            return;
        mallaRelleno = new Mesh { name = "VinetaRelleno" };
        mallaBorde = new Mesh { name = "VinetaBorde" };
        mallaSombra = new Mesh { name = "VinetaSombra" };
        mallaRelleno.MarkDynamic();
        mallaBorde.MarkDynamic();
        mallaSombra.MarkDynamic();
        // Detrás del texto: relleno, luego el borde y, mucho más atrás, la sombra.
        sombra = Parte("Sombra", mallaSombra, materialNegro);
        borde = Parte("Borde", mallaBorde, materialNegro);
        relleno = Parte("Relleno", mallaRelleno, materialTrama != null ? materialTrama : materialNegro);
    }

    Transform Parte(string nombre, Mesh malla, Material m)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    void LateUpdate()
    {
        if (mallaRelleno == null)
            return;
        float w = ancho, h = alto;
        Vector3 c = Vector3.zero;
        if (ajustarAlTexto && texto != null)
        {
            // Del tamaño de las letras (más un margen). Sin texto, no se ve.
            bool hay = !string.IsNullOrEmpty(texto.text);
            if (relleno.gameObject.activeSelf != hay)
            {
                relleno.gameObject.SetActive(hay);
                borde.gameObject.SetActive(hay);
                sombra.gameObject.SetActive(hay);
            }
            if (!hay)
                return;
            Bounds b = texto.textBounds;
            w = Mathf.Max(0.04f, b.size.x + 0.026f);
            h = Mathf.Max(0.025f, b.size.y + 0.018f);
            c = new Vector3(b.center.x, b.center.y, 0f);
        }
        bool cambio = Mathf.Abs(w - anchoHecho) > 0.002f || Mathf.Abs(h - altoHecho) > 0.002f || cola != colaHecha || (c - centro).sqrMagnitude > 1e-6f;
        if (cambio)
        {
            anchoHecho = w;
            altoHecho = h;
            colaHecha = cola;
            centro = c;
            ArmarMalla(mallaRelleno, w, h, 0f, 0f, 0);
            variante = -1;
            relleno.localPosition = c + new Vector3(0f, 0f, 0.002f);
            borde.localPosition = c + new Vector3(0f, 0f, 0.0035f);
            sombra.localPosition = c + new Vector3(0.012f, -0.012f, profundidadSombra);
        }
        int v = Mathf.FloorToInt(Time.time * 8f) % 3;
        if (v != variante)
        {
            variante = v;
            ArmarMalla(mallaBorde, w, h, bordeGrosor, 0.0018f, v + 1);
            ArmarMalla(mallaSombra, w, h, bordeGrosor, 0.0018f, v + 4);
        }
    }

    // Rectángulo redondeado con una colita (hacia abajo o hacia arriba, hacia donde está la acción).
    // extra = cuánto más grande (el borde); temblor = cuánto se mueve cada punto; variante = qué "dibujo".
    void ArmarMalla(Mesh m, float anchoBase, float altoBase, float extra, float temblor, int vari)
    {
        float w = anchoBase + extra * 2f, h = altoBase + extra * 2f;
        float r = Mathf.Min(0.02f, Mathf.Min(anchoBase, altoBase) * 0.4f) + extra;
        contorno.Clear();
        float x0 = -w * 0.5f, x1 = w * 0.5f, y0 = -h * 0.5f, y1 = h * 0.5f;
        const int pasos = 6;
        float colaX = Mathf.Clamp(0.04f, x0 + r + 0.03f, x1 - r - 0.05f);
        float media = Mathf.Min(0.018f, anchoBase * 0.12f) + extra;
        float largoCola = Mathf.Min(0.04f, altoBase * 0.6f);
        float puntaX = colaX + largoCola * 0.75f + extra * 0.6f;
        contorno.Add(new Vector3(x0 + r, y0, 0f));
        if (cola < 0)
        {
            contorno.Add(new Vector3(colaX - media, y0, 0f));
            contorno.Add(new Vector3(puntaX, y0 - largoCola - extra * 1.4f, 0f));
            contorno.Add(new Vector3(colaX + media, y0, 0f));
        }
        contorno.Add(new Vector3(x1 - r, y0, 0f));
        Esquina(new Vector2(x1 - r, y0 + r), r, -90f, 0f, pasos);
        Esquina(new Vector2(x1 - r, y1 - r), r, 0f, 90f, pasos);
        if (cola > 0)
        {
            contorno.Add(new Vector3(colaX + media, y1, 0f));
            contorno.Add(new Vector3(puntaX, y1 + largoCola + extra * 1.4f, 0f));
            contorno.Add(new Vector3(colaX - media, y1, 0f));
        }
        Esquina(new Vector2(x0 + r, y1 - r), r, 90f, 180f, pasos);
        Esquina(new Vector2(x0 + r, y0 + r), r, 180f, 270f, pasos);
        if (temblor > 0f)
        {
            for (int i = 0; i < contorno.Count; i++)
            {
                Vector3 p = contorno[i];
                Vector3 fuera = p.sqrMagnitude > 1e-8f ? p.normalized : Vector3.up;
                float semilla = i * 2.37f + vari * 5.11f;
                contorno[i] = p + fuera * (Mathf.Sin(semilla) * 0.6f + Mathf.Sin(semilla * 2.3f + 1.3f) * 0.4f) * temblor;
            }
        }
        verts.Clear();
        tris.Clear();
        verts.Add(Vector3.zero);
        verts.AddRange(contorno);
        for (int i = 0; i < contorno.Count; i++)
        {
            tris.Add(0);
            tris.Add(1 + i);
            tris.Add(1 + (i + 1) % contorno.Count);
        }
        m.Clear();
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
    }

    void Esquina(Vector2 c, float r, float desdeGrados, float hastaGrados, int pasos)
    {
        for (int i = 0; i <= pasos; i++)
        {
            float a = Mathf.Lerp(desdeGrados, hastaGrados, i / (float)pasos) * Mathf.Deg2Rad;
            contorno.Add(new Vector3(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r, 0f));
        }
    }

    void OnDestroy()
    {
        foreach (var m in new[] { mallaRelleno, mallaBorde, mallaSombra })
            if (m != null)
                Destroy(m);
    }
}

// El cartel de cómic de arriba al centro de tu vista: ahí salen el nombre del gesto y los avisos cortos
// (uno solo a la vez: el más nuevo gana). Sigue tu mirada con calma, como el parlante.
public static class CartelArriba
{
    // Dónde queda respecto a tu cabeza: al centro, arriba y adelante (metros).
    static readonly Vector3 Lugar = new Vector3(0f, 0.165f, 0.6f);

    static Quaternion giroSuave;
    static bool colocado;
    static int cuadroHecho = -1;
    static Vector3 posHecha;
    static Quaternion rotHecha;

    // Desde cuándo se ve cada uno (para saber cuál es el más nuevo) y si sigue visible.
    static float desdeAviso = -1f, desdeGesto = -1f;
    static int cuadroAviso = -10, cuadroGesto = -10;

    public static void Pose(Transform cabeza, out Vector3 pos, out Quaternion rot)
    {
        if (cuadroHecho != Time.frameCount)
        {
            cuadroHecho = Time.frameCount;
            Vector3 adelante = cabeza.forward;
            if (Mathf.Abs(Vector3.Dot(adelante, Vector3.up)) > 0.97f)
                adelante = Vector3.ProjectOnPlane(cabeza.up, Vector3.up);
            Quaternion giro = Quaternion.LookRotation(adelante, Vector3.up);
            if (!colocado || Time.deltaTime > 0.5f)
            {
                giroSuave = giro;
                colocado = true;
            }
            giroSuave = Quaternion.Slerp(giroSuave, giro, 1f - Mathf.Exp(-4f * Time.deltaTime));
            posHecha = cabeza.position + giroSuave * Lugar;
            rotHecha = Quaternion.LookRotation(posHecha - cabeza.position, Vector3.up);
        }
        pos = posHecha;
        rot = rotHecha;
    }

    // Se llaman cada cuadro mientras cada uno quiere verse. Devuelven si le toca verse (el más nuevo).
    public static bool PuedeVerAviso(float desde)
    {
        desdeAviso = desde;
        cuadroAviso = Time.frameCount;
        return !(Vivo(cuadroGesto) && desdeGesto > desde);
    }

    public static bool PuedeVerGesto(float desde)
    {
        desdeGesto = desde;
        cuadroGesto = Time.frameCount;
        return !(Vivo(cuadroAviso) && desdeAviso >= desde);
    }

    // "Vivo": quiso verse en este cuadro o en el anterior.
    static bool Vivo(int cuadro) => Time.frameCount - cuadro <= 1;
}

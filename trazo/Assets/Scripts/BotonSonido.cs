using System.Collections.Generic;
using UnityEngine;

// Parlante de silencio: siempre visible, pequeño, arriba a la derecha de lo que miras (te sigue suave).
//  - Con sonido: parlante de color (verde azulado) con sus ondas.
//  - En silencio: parlante gris oscuro con una X.
// Se toca con la punta del índice derecho. Silencia TODO el sonido de la app (y se recuerda).
public class BotonSonido : MonoBehaviour
{
    const string ClaveSilencio = "jcartoons_silencio";
    static readonly Color ColorActivo = new Color(0.16f, 0.68f, 0.64f);
    static readonly Color ColorApagado = new Color(0.22f, 0.22f, 0.24f);
    // Dónde queda respecto a tu cabeza: a la derecha, arriba y adelante (metros).
    static readonly Vector3 Lugar = new Vector3(0.16f, 0.12f, 0.45f);
    const float Tamano = 0.028f;
    const float RadioToque = 0.022f;

    static int silencio = -1; // -1 = aún no leído

    ControlManos control;
    Transform ondas, equis;
    Material matIcono;
    readonly List<Material> materiales = new List<Material>();
    readonly List<Mesh> mallas = new List<Mesh>();
    Quaternion giroSuave = Quaternion.identity;
    bool colocado, dedoDentro;
    float bloqueadoHasta;

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

    public static BotonSonido Crear(ControlManos control, Material baseMaterial)
    {
        var go = new GameObject("BotonSonido");
        var b = go.AddComponent<BotonSonido>();
        b.control = control;
        b.Armar(baseMaterial);
        return b;
    }

    void Armar(Material baseMaterial)
    {
        if (baseMaterial == null)
            return;
        transform.localScale = Vector3.one * Tamano;
        var blanco = NuevoMaterial(baseMaterial, Color.white);
        var negro = NuevoMaterial(baseMaterial, Color.black);
        matIcono = NuevoMaterial(baseMaterial, ColorActivo);
        // Fondo redondo blanco con borde negro (se ve sobre cualquier fondo).
        Parte(transform, "Borde", Disco(0.62f), negro, 0.1f);
        Parte(transform, "Fondo", Disco(0.56f), blanco, 0.05f);
        // El parlante: una cajita y la bocina (trapecio).
        var parlante = new List<Vector2[]>
        {
            new[] { new Vector2(-0.3f, -0.11f), new Vector2(-0.3f, 0.11f), new Vector2(-0.15f, 0.11f), new Vector2(-0.15f, -0.11f) },
            new[] { new Vector2(-0.15f, -0.11f), new Vector2(-0.15f, 0.11f), new Vector2(0.03f, 0.28f), new Vector2(0.03f, -0.28f) },
        };
        Parte(transform, "Parlante", Malla(parlante), matIcono, 0f);
        // Ondas del sonido (dos arcos).
        var arcos = new List<Vector2[]>();
        Arco(arcos, 0.13f, 0.05f);
        Arco(arcos, 0.27f, 0.05f);
        ondas = Parte(transform, "Ondas", Malla(arcos), matIcono, 0f);
        ondas.localPosition = new Vector3(0.04f, 0f, 0f);
        // X del silencio.
        var x = new List<Vector2[]>();
        Barra(x, new Vector2(-0.1f, -0.1f), new Vector2(0.1f, 0.1f), 0.05f);
        Barra(x, new Vector2(-0.1f, 0.1f), new Vector2(0.1f, -0.1f), 0.05f);
        equis = Parte(transform, "Equis", Malla(x), matIcono, 0f);
        equis.localPosition = new Vector3(0.2f, 0f, 0f);
        Refrescar();
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
            return;
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
        giroSuave = Quaternion.Slerp(giroSuave, giro, 1f - Mathf.Exp(-4f * Time.deltaTime));
        Vector3 pos = cabeza.position + giroSuave * Lugar;
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cabeza.position, Vector3.up));

        // Tocar con la punta del índice derecho.
        bool toca = control.Der.valida && Vector3.Distance(control.Der.indice, pos) < RadioToque;
        if (toca && !dedoDentro && Time.time >= bloqueadoHasta && control.GestoIzq == ControlManos.Gesto.Ninguno)
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
            control.MostrarEtiqueta(Silenciado ? "Sonido: No" : "Sonido: Sí");
        }
        dedoDentro = toca;
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
    }
}

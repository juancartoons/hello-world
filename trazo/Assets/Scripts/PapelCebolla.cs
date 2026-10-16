using System.Collections.Generic;
using UnityEngine;

// PAPEL CEBOLLA (v43), para animar cuadro a cuadro: con la animación en pausa, se ven clarito las líneas de la
// CLAVE ANTERIOR (rojizas) y de la SIGUIENTE (verdosas) de la capa en la que dibujas. Así calcas el movimiento.
//  - Solo se ven: no se tocan, no se borran y no salen en fotos ni videos.
//  - Se prende y se apaga con el botón "Cebolla" de la página Animar (la mesa) o la voz ("cebolla"). Se recuerda.
//  - v46: en una capa CUADRO A CUADRO se ve SIEMPRE (ahí es donde más se usa: cada cuadro empieza vacío y calcas
//    el anterior). El botón "Cebolla" es para las capas normales.
public class PapelCebolla : MonoBehaviour
{
    const string ClaveGuardada = "jcartoons_cebolla";
    static readonly Color ColorAntes = new Color(0.95f, 0.5f, 0.5f);
    static readonly Color ColorDespues = new Color(0.45f, 0.82f, 0.55f);

    static PapelCebolla instancia;
    static int activo = -1;   // -1 = aún no leído

    public static bool Activo
    {
        get
        {
            if (activo < 0)
                activo = PlayerPrefs.GetInt(ClaveGuardada, 0);
            return activo == 1;
        }
    }

    public static void Alternar()
    {
        activo = Activo ? 0 : 1;
        PlayerPrefs.SetInt(ClaveGuardada, activo);
        PlayerPrefs.Save();
        Asegurar();
        var d = instancia != null ? instancia.dibujo : null;
        if (d != null)
            d.Mensaje(activo == 1 ? "Papel cebolla: Sí (clave anterior en rojo, siguiente en verde)" : "Papel cebolla: No");
    }

    public static void Asegurar()
    {
        if (instancia != null)
            return;
        instancia = new GameObject("PapelCebolla").AddComponent<PapelCebolla>();
    }

    Dibujo dibujo;
    Transform raiz;
    readonly List<Trazo> lineas = new List<Trazo>();
    Clave antes, despues;
    int capaPuesta = -1, fotogramaPuesto = -1, cuantasAntes = -1, cuantasDespues = -1;

    void LateUpdate()
    {
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
        var anim = dibujo != null ? dibujo.animacion : null;
        bool cuadros = dibujo != null && dibujo.EsCuadroACuadro(dibujo.capaActual);
        bool ver = (Activo || cuadros) && anim != null && anim.claves.Count > 0 && !anim.Reproduciendo && !ExportadorVideo.Exportando
                   && !Tutorial.EnCurso;
        Clave a = null, b = null;
        int capa = ver ? dibujo.capaActual : -1;
        int f = ver ? anim.Fotograma : -1;
        if (ver)
            Buscar(anim, capa, f, out a, out b);
        int na = a != null ? a.trazos.Count : -1, nb = b != null ? b.trazos.Count : -1;
        if (a == antes && b == despues && capa == capaPuesta && f == fotogramaPuesto && na == cuantasAntes && nb == cuantasDespues)
            return;
        antes = a;
        despues = b;
        capaPuesta = capa;
        fotogramaPuesto = f;
        cuantasAntes = na;
        cuantasDespues = nb;
        Armar();
    }

    // La clave anterior (antes de este fotograma) y la siguiente (después) de esa capa.
    static void Buscar(Animacion anim, int capa, int f, out Clave a, out Clave b)
    {
        a = null;
        b = null;
        foreach (var c in anim.claves)
        {
            if (c == null || c.capa != capa)
                continue;
            if (c.fotograma < f)
                a = c;
            else if (c.fotograma > f && b == null)
                b = c;
        }
    }

    void Armar()
    {
        foreach (var t in lineas)
            if (t != null)
                Destroy(t.gameObject);
        lineas.Clear();
        if (antes == null && despues == null)
            return;
        if (raiz == null)
        {
            raiz = new GameObject("Cebolla").transform;
            raiz.gameObject.layer = 0;   // la capa que no sale en fotos ni videos
        }
        // Va con el dibujo (si lo mueves o lo escalas, el papel cebolla también).
        raiz.SetParent(dibujo.transform, false);
        raiz.localPosition = Vector3.zero;
        raiz.localRotation = Quaternion.identity;
        raiz.localScale = Vector3.one;
        bool silencio = Trazo.silenciar;
        Trazo.silenciar = true;
        Poner(antes, ColorAntes);
        Poner(despues, ColorDespues);
        Trazo.silenciar = silencio;
    }

    void Poner(Clave c, Color color)
    {
        if (c == null || c.trazos == null)
            return;
        foreach (var p in c.trazos)
        {
            if (p == null || p.oculto || p.nodos == null || p.nodos.Count < 2)
                continue;
            var go = new GameObject("LineaCebolla");
            go.layer = 0;
            go.transform.SetParent(raiz, false);
            var t = go.AddComponent<Trazo>();
            t.Configurar(dibujo.materialLinea, dibujo.materialRelleno, p.ancho > 0f ? p.ancho : 0.008f, EstiloLinea.Cinta);
            t.color = color;
            // Solo la línea (sin el relleno, que taparía lo que dibujas).
            bool relleno = p.relleno, abierto = p.rellenoAbierto;
            p.relleno = false;
            p.rellenoAbierto = false;
            t.AplicarPose(p, null, 0f);
            p.relleno = relleno;
            p.rellenoAbierto = abierto;
            lineas.Add(t);
        }
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}

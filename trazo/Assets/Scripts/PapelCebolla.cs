using System.Collections.Generic;
using UnityEngine;

// PAPEL CEBOLLA (v43), para animar cuadro a cuadro: con la animación en pausa, se ven clarito las líneas de la
// CLAVE ANTERIOR (rojizas) y de la SIGUIENTE (verdosas) de la capa en la que dibujas. Así calcas el movimiento.
//  - Solo se ven: no se tocan, no se borran y no salen en fotos ni videos.
//  - Se prende y se apaga con el botón "Cebolla" de la página Animar (la mesa) o la voz ("cebolla"). Se recuerda.
//  - v53: el botón "Cebolla" MANDA SIEMPRE (también en cuadro a cuadro: antes ahí se veía siempre y el botón parecía
//    no hacer nada). Al volver una capa cuadro a cuadro, la cebolla se prende sola. Prender/apagar se ve al instante.
public class PapelCebolla : MonoBehaviour
{
    const string ClaveGuardada = "jcartoons_cebolla";
    // v50: un COLOR POR CUADRO (como los programas de animación): 2 antes y 2 después, cada uno distinto, y los más
    // lejanos más clarito. Así se sabe cuál es cuál.
    static readonly Color[] ColoresAntes = { new Color(0.92f, 0.36f, 0.36f), new Color(0.97f, 0.66f, 0.3f) };    // rojo, naranja
    static readonly Color[] ColoresDespues = { new Color(0.28f, 0.72f, 0.42f), new Color(0.3f, 0.55f, 0.95f) }; // verde, azul
    const int Cuantos = 2;

    static PapelCebolla instancia;
    static int activo = 0;    // v52: siempre empieza APAGADO (no se recuerda de una sesión a otra)

    public static bool Activo
    {
        get
        {
            return activo == 1;
        }
    }

    // v53: para las capas cuadro a cuadro (ahí casi siempre se quiere ver).
    public static void Encender()
    {
        Asegurar();
        activo = 1;
        if (instancia != null)
            instancia.firmaPuesta = int.MinValue;
    }

    public static void Alternar()
    {
        activo = Activo ? 0 : 1;
        Asegurar();
        if (instancia != null)
            instancia.firmaPuesta = int.MinValue;   // se rehace YA (no espera a que cambie otra cosa)
        var d = instancia != null ? instancia.dibujo : null;
        if (d != null)
            d.Mensaje(activo == 1 ? "Papel cebolla: Sí (antes: rojo y naranja · después: verde y azul)" : "Papel cebolla: No");
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
    readonly List<Clave> antes = new List<Clave>(), despues = new List<Clave>();
    readonly List<Clave> nuevasAntes = new List<Clave>(), nuevasDespues = new List<Clave>();
    int capaPuesta = -1, fotogramaPuesto = -1, firmaPuesta = -1;

    void LateUpdate()
    {
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
        var anim = dibujo != null ? dibujo.animacion : null;
        bool ver = Activo && dibujo != null && anim != null && anim.claves.Count > 0 && !anim.Reproduciendo && !ExportadorVideo.Exportando
                   && !Tutorial.EnCurso;
        int capa = ver ? dibujo.capaActual : -1;
        int f = ver ? anim.Fotograma : -1;
        nuevasAntes.Clear();
        nuevasDespues.Clear();
        if (ver)
            Buscar(anim, capa, f, nuevasAntes, nuevasDespues);
        int firma = 17;
        unchecked
        {
            foreach (var k in nuevasAntes)
                firma = firma * 31 + Firma(k);
            foreach (var k in nuevasDespues)
                firma = firma * 37 + Firma(k);
        }
        if (firma == firmaPuesta && capa == capaPuesta && f == fotogramaPuesto)
            return;
        antes.Clear();
        antes.AddRange(nuevasAntes);
        despues.Clear();
        despues.AddRange(nuevasDespues);
        firmaPuesta = firma;
        capaPuesta = capa;
        fotogramaPuesto = f;
        Armar();
    }

    // Lo que tiene una clave (si cambias su dibujo, la cebolla se rehace aunque tenga las mismas líneas).
    static int Firma(Clave k)
    {
        unchecked
        {
            int h = k.GetHashCode() * 7 + k.trazos.Count;
            foreach (var p in k.trazos)
            {
                if (p == null || p.nodos == null)
                    continue;
                h = h * 31 + p.nodos.Count + (p.oculto ? 1 : 0);
                if (p.nodos.Count > 0)
                    h = h * 31 + p.nodos[0].GetHashCode() + p.nodos[p.nodos.Count - 1].GetHashCode();
            }
            return h;
        }
    }

    // Las claves de antes (la más cercana primero) y las de después, de esa capa (hasta 2 de cada lado).
    static void Buscar(Animacion anim, int capa, int f, List<Clave> a, List<Clave> b)
    {
        foreach (var c in anim.claves)
        {
            if (c == null || c.capa != capa)
                continue;
            if (c.fotograma < f)
                a.Insert(0, c);
            else if (c.fotograma > f && b.Count < Cuantos)
                b.Add(c);
        }
        while (a.Count > Cuantos)
            a.RemoveAt(a.Count - 1);
    }

    void Armar()
    {
        foreach (var t in lineas)
            if (t != null)
                Destroy(t.gameObject);
        lineas.Clear();
        if (antes.Count == 0 && despues.Count == 0)
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
        // Los más lejanos primero y más clarito.
        for (int i = antes.Count - 1; i >= 0; i--)
            Poner(antes[i], Color.Lerp(ColoresAntes[i], Color.white, i * 0.3f));
        for (int i = despues.Count - 1; i >= 0; i--)
            Poner(despues[i], Color.Lerp(ColoresDespues[i], Color.white, i * 0.3f));
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
            // v52: el color va en su MATERIAL (antes solo se guardaba en la línea y todas salían negras).
            t.PonerMaterialLinea(dibujo.MaterialColor(color));
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

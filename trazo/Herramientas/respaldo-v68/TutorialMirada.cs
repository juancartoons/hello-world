using System.Collections.Generic;
using UnityEngine;

// Tutorial "MIRA Y DI" (v44, sección 5 del panel "?"): la mirada + la voz, para aprender las palabras de una vez.
//  1 · El ojo y la mira (lo señalado brilla).    3 · Copiar y pegar un color.
//  2 · Di un color (relleno o línea).            4 · Esquina y suave (en nodos).
public class TutorialMirada : TarjetaTutorial
{
    static readonly Color Amarillo = new Color(1f, 0.85f, 0.35f);
    static readonly Color Rojo = new Color(0.9f, 0.25f, 0.2f);
    static readonly Color Azul = new Color(0.25f, 0.5f, 0.95f);
    static readonly Color Brillo = new Color(0.72f, 0.9f, 1f);   // v46: el titileo (prudente)

    LineRenderer mira, brillo, otra;
    Material matOtra, matBrillo;
    readonly List<Vector3> aro = new List<Vector3>();
    readonly List<Vector3> segunda = new List<Vector3>();

    protected override int Paginas => 4;

    protected override void ArmarExtra()
    {
        mira = Linea("Mira", Copia(tutorial.materialNegro, new Color(0.16f, 0.62f, 0.58f)), 0.0012f);
        brillo = Linea("Brillo", matBrillo = Copia(tutorial.materialNegro, Brillo), 0.0035f);   // v46: mismo grosor que la línea
        matOtra = Copia(tutorial.materialNegro, Color.black);
        otra = Linea("Otra", matOtra, 0.0025f);
    }

    protected override string Titulo(int p)
    {
        switch (p)
        {
            case 0: return Tx("1 · The eye and the sight", "1 · El ojo y la mira");
            case 1: return Tx("2 · Say a color", "2 · Di un color");
            case 2: return Tx("3 · Copy and paste", "3 · Copiar y pegar");
            default: return Tx("4 · Corner and smooth", "4 · Esquina y suave");
        }
    }

    protected override string Explicacion(int p)
    {
        switch (p)
        {
            case 0:
                return Tx(
                    "Touch the EYE (under the microphone) or say \"gaze\": a small SIGHT appears in the middle of your view. Look at a line or a fill: it doesn't need to be exact (it has an invisible magnet); what you point at BLINKS softly (it keeps its shape and thickness). Looking never does anything by itself: you say what happens. The voice must be on (the microphone).",
                    "Toca el OJO (debajo del micrófono) o di \"mirada\": sale una MIRA chiquita en el centro de tu vista. Mira una línea o un relleno: no hace falta ser exacto (tiene un imán invisible); lo señalado TITILA suave (no cambia de forma ni de grosor). Mirar nunca hace nada solo: tú dices qué pasa. La voz tiene que estar prendida (el micrófono).");
            case 1:
                return Tx(
                    "Look at a fill and say \"red\": it turns red. Say \"red line\" or \"red fill\" to choose exactly the outline or the inside. If NOTHING glows, the color is for what you draw next (as always). Also: \"delete this\", \"hide this\", \"this layer\", \"select\".",
                    "Mira un relleno y di \"rojo\": se vuelve rojo. Di \"línea roja\" o \"relleno rojo\" para elegir justo el contorno o el interior. Si NADA brilla, el color es para lo que dibujes después (como siempre). También: \"borra esto\", \"oculta esto\", \"esta capa\", \"selecciona\".");
            case 2:
                return Tx(
                    "Look at something and say \"copy color\": its color goes into your index finger (like the syringe). Look at something else and say \"paste color\": it gets that color. (\"Copy\" and \"paste\" alone copy the LINES themselves.) Touching a color of the palette also fills your finger: then touch a line or the inside of a shape and it gets painted.",
                    "Mira algo y di \"copiar color\": su color se carga en tu índice (como la jeringa). Mira otra cosa y di \"pegar color\": se pinta de ese color. (\"Copiar\" y \"pegar\" solos copian las LÍNEAS mismas.) Tocar un color de la paleta también llena tu dedo: luego toca una línea o el interior de una forma y se pinta.");
            default:
                return Tx(
                    "In Nodes, look at a node and say \"corner\" (or \"wings\", \"handles\"...): now when you move one of its handles with your index, only that side moves. Say \"smooth\" and both handles line up again (a smooth curve).",
                    "En Nodos, mira un nodo y di \"esquina\" (o \"alas\", \"manijas\", \"tiradores\"...): ahora al mover uno de sus tiradores con el índice, solo se mueve ese lado. Di \"suave\" y los dos tiradores vuelven a quedar alineados (curva lisa).");
        }
    }

    protected override void Animar(int p, float t)
    {
        dedo.gameObject.SetActive(false);
        lineaGris.positionCount = 0;
        brillo.positionCount = 0;
        otra.positionCount = 0;
        SinRelleno();
        switch (p)
        {
            case 0: AnimarMira(t); break;
            case 1: AnimarColor(t); break;
            case 2: AnimarCopiar(t); break;
            default: AnimarEsquina(t); break;
        }
    }

    static void Circulo(List<Vector3> l, Vector3 c, float r, int n = 32)
    {
        l.Clear();
        for (int i = 0; i <= n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            l.Add(c + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
    }

    void PonerMira(Vector3 c, bool encima)
    {
        Circulo(aro, c + new Vector3(0f, 0f, -0.006f), encima ? 0.009f : 0.007f, 20);
        PonerLinea(mira, aro);
    }

    // La mira llega a la forma y la forma brilla.
    void AnimarMira(float t)
    {
        Circulo(puntos, Vector3.zero, 0.04f);
        PonerLinea(linea, puntos);
        Vector3 desde = new Vector3(-0.1f, 0.04f, 0f), hasta = new Vector3(0.01f, 0.005f, 0f);
        float u = Suave(Mathf.InverseLerp(0.3f, 1.6f, t));
        bool encima = u > 0.95f;
        PonerMira(Vector3.Lerp(desde, hasta, u), encima);
        if (encima)
        {
            Titila(t, Color.black);
            PonerLinea(brillo, puntos);
        }
    }

    // Mira el relleno y "rojo": se vuelve rojo.
    void AnimarColor(float t)
    {
        Circulo(puntos, Vector3.zero, 0.04f);
        PonerLinea(linea, puntos);
        PonerMira(new Vector3(0.01f, 0.005f, 0f), true);
        Titila(t, Color.black);
        PonerLinea(brillo, puntos);
        Rellenar(puntos, t < 1.6f ? Amarillo : Rojo, 1f);
    }

    // "Copiar" en la línea azul (izquierda) y "pegar" en la negra (derecha): se vuelve azul.
    void AnimarCopiar(float t)
    {
        Circulo(puntos, new Vector3(-0.05f, 0f, 0f), 0.03f);
        PonerColor(matOtra, Azul);
        PonerLinea(otra, puntos);
        Circulo(segunda, new Vector3(0.05f, 0f, 0f), 0.03f);
        PonerLinea(linea, segunda);
        bool pegado = t > 2.8f;
        if (pegado)
            PonerLinea(otra, segunda);
        Vector3 izq = new Vector3(-0.05f, 0.03f, 0f), der = new Vector3(0.05f, 0.03f, 0f);
        float u = Suave(Mathf.InverseLerp(1.3f, 2.3f, t));
        Vector3 m = Vector3.Lerp(izq, der, u);
        PonerMira(m, u < 0.05f || u > 0.95f);
        Titila(t, u < 0.5f ? Azul : Color.black);
        if (!pegado)
            PonerLinea(brillo, u < 0.5f ? puntos : segunda);
        // El dedo, cargado del color copiado.
        dedo.gameObject.SetActive(t > 1f && !pegado);
        dedo.localPosition = new Vector3(0f, -0.055f, -0.004f);
    }

    // Un nodo de esquina: el tirador de la derecha se mueve solo (el de la izquierda se queda).
    void AnimarEsquina(float t)
    {
        Vector3 nodo = new Vector3(0f, 0.01f, 0f);
        Vector3 izq = nodo + new Vector3(-0.04f, -0.01f, 0f);
        float u = Suave(Mathf.InverseLerp(1.4f, 2.6f, t));
        Vector3 der = nodo + Vector3.Lerp(new Vector3(0.04f, 0.01f, 0f), new Vector3(0.025f, -0.035f, 0f), u);
        // La curva: de la izquierda al nodo y del nodo hacia abajo a la derecha.
        puntos.Clear();
        Vector3 a = new Vector3(-0.08f, -0.03f, 0f), b = new Vector3(0.08f, -0.03f, 0f);
        for (int i = 0; i <= 12; i++)
        {
            float k = i / 12f;
            puntos.Add(Bezier(a, a + (izq - a) * 0.5f, izq, nodo, k));
        }
        for (int i = 1; i <= 12; i++)
        {
            float k = i / 12f;
            puntos.Add(Bezier(nodo, der, b + (der - b) * 0.5f, b, k));
        }
        PonerLinea(linea, puntos);
        segunda.Clear();
        segunda.Add(izq);
        segunda.Add(nodo);
        segunda.Add(der);
        PonerLinea(lineaGris, segunda);
        PonerMira(nodo, true);
        dedo.gameObject.SetActive(t > 1.2f && t < 3f);
        dedo.localPosition = der + new Vector3(0f, 0f, -0.004f);
    }

    // v46: la línea señalada titila: su color va y vuelve suave hacia un celeste (encima, con el mismo grosor).
    void Titila(float t, Color desde)
    {
        float k = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 1.8f);
        PonerColor(matBrillo, Color.Lerp(desde, Brillo, k * 0.6f));
    }

    static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float s = 1f - t;
        return s * s * s * p0 + 3f * s * s * t * p1 + 3f * s * t * t * p2 + t * t * t * p3;
    }
}

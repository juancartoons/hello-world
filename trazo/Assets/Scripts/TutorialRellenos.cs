using System.Collections.Generic;
using UnityEngine;

// Tutorial de RELLENOS (sección 2 del panel "?"). No es parte del tutorial de presentación.
//  1 · La cubeta: rellena mientras dibujas (en vivo), aunque no cierres la forma.
//  2 · Tocar dentro para rellenar (contorno negro + relleno de otro color).
//  3 · Tinta invisible (cierra y rellena sin que se vea la línea).
//  4 · Ideas (nariz de lado, manchas, caminos del títere, mejillas).
public class TutorialRellenos : TarjetaTutorial
{
    static readonly Color Amarillo = new Color(1f, 0.85f, 0.35f);
    static readonly Color Piel = new Color(0.96f, 0.75f, 0.6f);
    static readonly Color Celeste = new Color(0.55f, 0.78f, 1f);

    protected override int Paginas => 4;

    protected override string Titulo(int p)
    {
        switch (p)
        {
            case 0: return Tx("1 · The bucket: fill while you draw", "1 · La cubeta: rellena mientras dibujas");
            case 1: return Tx("2 · Tap inside to fill", "2 · Toca dentro para rellenar");
            case 2: return Tx("3 · Invisible ink", "3 · Tinta invisible");
            default: return Tx("4 · Ideas", "4 · Ideas");
        }
    }

    protected override string Explicacion(int p)
    {
        switch (p)
        {
            case 0:
                return Tx(
                    "Open the palette (left palm), tap FILL (at 2 o'clock, 3 green facets) and then the BUCKET: it turns yellow. Now draw a C: it fills LIVE while you draw, and stays filled when you let go, even if it isn't closed. Tap the bucket again = off.",
                    "Abre la paleta (palma izquierda), toca RELLENO (a las 2, 3 facetas verdes) y luego la CUBETA (el balde): se pone amarilla. Ahora dibuja una C: se va rellenando EN VIVO y al soltar queda rellena, aunque no la cierres. Otra vez la cubeta = apagada.");
            case 1:
                return Tx(
                    "Open the palette and tap FILL (at 2 o'clock): now the colors are for the fill (the line keeps its own). Pick one and tap INSIDE a shape (open or closed) for a moment: it fills. Keep a black outline and another fill color, like a nose in profile. You can undo it.",
                    "Abre la paleta y toca RELLENO (a las 2): ahora los colores son del relleno (la línea guarda el suyo). Elige uno y toca DENTRO de una forma (abierta o cerrada) un instante: se rellena. Así el contorno queda negro y el relleno de otro color, como una nariz de lado. Se puede deshacer.");
            case 2:
                return Tx(
                    "Tap the CHECKERED circle (at 12 o'clock on the palette): your line won't show, but it closes and fills. While editing (palette, nodes or eraser) it shows light gray so you can find it. With FILL open, invisible ink + tap inside = remove the fill.",
                    "Toca el círculo a CUADRITOS (a las 12 de la paleta): tu línea no se verá, pero sí cierra y rellena. Mientras editas (paleta, nodos o borrador) se ve gris clarito para encontrarla. Con RELLENO abierto, tinta invisible + tocar dentro = quitar el relleno.");
            default:
                return Tx(
                    "• Nose in profile: black outline + tap inside with skin color.\n• Spots and shadows with no outline: bucket + invisible ink.\n• Puppet path: draw it with invisible ink (it won't show in videos).\n• Cheeks: an oval with the bucket and pink.",
                    "• Nariz de lado: contorno negro + toca dentro con color piel.\n• Manchas y sombras sin borde: cubeta + tinta invisible.\n• Camino del títere: dibújalo con tinta invisible (no sale en el video).\n• Mejillas: un óvalo con la cubeta y color rosado.");
        }
    }

    protected override void Animar(int p, float t)
    {
        switch (p)
        {
            case 0: AnimarCubeta(t); break;
            case 1: AnimarTocarDentro(t); break;
            case 2: AnimarInvisible(t); break;
            default: AnimarIdeas(); break;
        }
    }

    // Una "C" abierta hacia la derecha (de 50° a 310°).
    static void FormaC(List<Vector3> salida, float hasta)
    {
        salida.Clear();
        const int n = 40;
        int cuantos = Mathf.Clamp(Mathf.RoundToInt(n * hasta), 0, n);
        for (int i = 0; i <= cuantos; i++)
        {
            float a = Mathf.Lerp(50f, 310f, i / (float)n) * Mathf.Deg2Rad;
            salida.Add(new Vector3(-0.02f + Mathf.Cos(a) * 0.04f, Mathf.Sin(a) * 0.04f, 0f));
        }
    }

    // Nariz de lado (abierta: sin línea del lado de la cara).
    static void FormaNariz(List<Vector3> salida)
    {
        salida.Clear();
        Vector3[] c = { new Vector3(-0.03f, 0.045f, 0f), new Vector3(0.0f, 0.01f, 0f), new Vector3(0.035f, -0.02f, 0f),
                        new Vector3(0.025f, -0.035f, 0f), new Vector3(0.0f, -0.035f, 0f), new Vector3(-0.012f, -0.028f, 0f),
                        new Vector3(-0.03f, -0.035f, 0f) };
        for (int i = 0; i < c.Length - 1; i++)
            for (int k = 0; k < 6; k++)
            {
                // Catmull-Rom (curva suave que pasa por los puntos).
                Vector3 p0 = c[Mathf.Max(0, i - 1)], p1 = c[i], p2 = c[i + 1], p3 = c[Mathf.Min(c.Length - 1, i + 2)];
                float u = k / 6f;
                salida.Add(0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u));
            }
        salida.Add(c[c.Length - 1]);
    }

    void AnimarCubeta(float t)
    {
        FormaC(puntos, Mathf.Clamp01(t / 2.4f));
        PonerLinea(linea, puntos);
        Rellenar(puntos, Amarillo, 1f);
        lineaGris.positionCount = 0;
        dedo.gameObject.SetActive(t < 2.8f);
        if (puntos.Count > 0)
            dedo.localPosition = puntos[puntos.Count - 1] + new Vector3(0f, 0f, -0.004f);
    }

    void AnimarTocarDentro(float t)
    {
        FormaNariz(puntos);
        PonerLinea(linea, puntos);
        lineaGris.positionCount = 0;
        // El dedo entra desde fuera y toca adentro: ¡se rellena!
        Vector3 fuera = new Vector3(0.11f, 0.03f, -0.004f), dentro = new Vector3(0.008f, -0.012f, -0.004f);
        dedo.gameObject.SetActive(t < 3.2f);
        dedo.localPosition = Vector3.Lerp(fuera, dentro, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1.4f, t)));
        Rellenar(puntos, Piel, t < 1.5f ? 0f : Mathf.Clamp01((t - 1.5f) / 0.25f));
    }

    void AnimarInvisible(float t)
    {
        FormaC(puntos, 1f);
        PonerLinea(linea, puntos);
        // La tinta invisible cierra la C (gris clarito mientras "editas")...
        float cierre = Mathf.Clamp01((t - 0.6f) / 1.0f);
        forma.Clear();
        if (cierre > 0f && puntos.Count > 1)
        {
            Vector3 a = puntos[puntos.Count - 1], b = puntos[0];
            forma.Add(a);
            forma.Add(Vector3.Lerp(a, b, cierre));
        }
        // ...y al terminar de editar desaparece: queda el relleno sin línea de cierre.
        if (t < 3.0f)
            PonerLinea(lineaGris, forma);
        else
            lineaGris.positionCount = 0;
        dedo.gameObject.SetActive(t > 0.5f && t < 1.7f);
        if (forma.Count > 1)
            dedo.localPosition = forma[1] + new Vector3(0f, 0f, -0.004f);
        Rellenar(puntos, Celeste, Mathf.Clamp01((t - 1.8f) / 0.3f));
    }

    void AnimarIdeas()
    {
        FormaNariz(puntos);
        PonerLinea(linea, puntos);
        lineaGris.positionCount = 0;
        dedo.gameObject.SetActive(false);
        Rellenar(puntos, Piel, 1f);
    }
}

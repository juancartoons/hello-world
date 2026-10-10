using System.Collections.Generic;
using UnityEngine;

// Tutorial de NOVEDADES (sección 4 del panel "?"): lo nuevo de las últimas versiones, con una animación corta
// de cada cosa. Se actualiza en cada versión.
//  1 · Encantamiento 2D y Liberar.          4 · Menú de arriba (archivador) y Mis archivos.
//  2 · Frente / fondo con un empujón.        5 · Fondos 360 (ventanita) y papel blanco del boceto.
//  3 · Relleno vivo (estilo Quill).          6 · Paleta: imán, datos de la línea y modo rendimiento.
public class TutorialNovedades : TarjetaTutorial
{
    static readonly Color Verde = new Color(0.45f, 0.72f, 0.42f);
    static readonly Color Amarillo = new Color(1f, 0.85f, 0.35f);
    static readonly Color Celeste = new Color(0.55f, 0.78f, 1f);

    LineRenderer extra;   // una segunda línea (para frente/fondo y los rectángulos)
    readonly List<Vector3> otra = new List<Vector3>();

    protected override int Paginas => 6;

    protected override void ArmarExtra()
    {
        extra = Linea("Extra", tutorial.materialNegro, 0.0025f);
    }

    protected override string Titulo(int p)
    {
        switch (p)
        {
            case 0: return Tx("1 · 2D lines stay on their sheet", "1 · Las líneas 2D se quedan en su hoja");
            case 1: return Tx("2 · Front and back", "2 · Frente y fondo");
            case 2: return Tx("3 · Live fill (like Quill)", "3 · Relleno vivo (como Quill)");
            case 3: return Tx("4 · Top menu and My files", "4 · Menú de arriba y Mis archivos");
            case 4: return Tx("5 · 360 backgrounds and white paper", "5 · Fondos 360 y papel blanco");
            default: return Tx("6 · Palette: magnet and line data", "6 · Paleta: imán y datos de la línea");
        }
    }

    protected override string Explicacion(int p)
    {
        switch (p)
        {
            case 0:
                return Tx(
                    "What you draw in Plane 2D is ENCHANTED on its layer's sheet: handles, nodes, lasso and moving it stay inside that sheet, even in 3D. To take lines out: top menu > + Layer > RELEASE (selected lines, or the whole layer).",
                    "Lo que dibujas en Plano 2D queda ENCANTADO en la hoja de su capa: tiradores, nodos, lazo y moverla se quedan dentro de esa hoja, aunque pases a 3D. Para sacarlas: menú de arriba > + Capa > LIBERAR (las elegidas o toda la capa).");
            case 1:
                return Tx(
                    "Grab lines of a 2D sheet (lasso or pinch) and PUSH your hand about 3 cm into the sheet: they go BEHIND the next shape they touch. PULL toward you: they come to the FRONT. New lines always start in front.",
                    "Agarra líneas de una hoja 2D (lazo o pellizco) y EMPUJA la mano unos 3 cm hacia adentro: pasan DETRÁS de la siguiente figura que tocan. TIRA hacia ti: pasan ADELANTE. Las líneas nuevas siempre salen al frente.");
            case 2:
                return Tx(
                    "Palette, top left (3 green facets): Plain → Facets → Patches → Brushstrokes. Next to it: speed (arrows) and size (squares). With selected lines it goes on them; if not, on your next fills (tap inside or use the bucket).",
                    "Paleta, arriba a la izquierda (3 facetas verdes): Liso → Facetas → Manchas → Pinceladas. Al lado: la velocidad (flechas) y el tamaño (cuadritos). Con líneas elegidas se les pone a ellas; si no, a tus próximos rellenos (toca dentro o usa la cubeta).");
            case 3:
                return Tx(
                    "Top menu: only the timeline and player; the pages peek out as tabs (tap = open, again = close). The pin fixes the panel. My files: tap a thumbnail for its actions, swipe sideways to change page; Share, Record and Versions are there.",
                    "Menú de arriba: solo la línea de tiempo y el reproductor; las páginas se asoman como pestañas (tocar = abrir, otra vez = guardar). El alfiler fija el panel. Mis archivos: toca una miniatura para ver sus acciones, desliza de lado para cambiar de página; ahí están Compartir, Grabar y las Versiones.");
            case 4:
                return Tx(
                    "Background button: Grid → White → Reality → 360. At 360 a small window with all the photos opens: tap one to try it. In a Sketch layer in 2D, behind the pencil there is white paper; its corner button: White → 50% → Clear.",
                    "Botón Fondo: Cuadrícula → Blanco → Realidad → 360. Al llegar a 360 se abre una ventanita con todas las fotos: toca una para probarla. En una capa de Boceto en 2D, detrás del lápiz hay papel blanco; su botón de la esquina: Blanco → 50 % → Transparente.");
            default:
                return Tx(
                    "The MAGNET (line ends stick together) is now in the palette, on the right. Under the palette you see the data of the line (width, color, wobble, fill...): what you just changed glows orange. If the headset slows down, performance mode helps by itself.",
                    "El IMÁN (las puntas se pegan) ahora está en la paleta, a la derecha. Debajo de la paleta ves los datos de la línea (grosor, color, temblor, relleno...): lo que acabas de cambiar se ve en naranja. Si el visor se pone lento, el modo rendimiento ayuda solo.");
        }
    }

    protected override void Animar(int p, float t)
    {
        extra.positionCount = 0;
        lineaGris.positionCount = 0;
        SinRelleno();
        dedo.gameObject.SetActive(false);
        dedo.localScale = Vector3.one * 0.009f;
        switch (p)
        {
            case 0: AnimarHoja(t); break;
            case 1: AnimarOrden(t); break;
            case 2: AnimarRellenoVivo(t); break;
            case 3: AnimarMenu(t); break;
            case 4: AnimarPapel(t); break;
            default: AnimarPaleta(t); break;
        }
    }

    // ---------- Formas ----------

    static void Rectangulo(List<Vector3> s, float cx, float cy, float w, float h, float z = 0f)
    {
        s.Clear();
        s.Add(new Vector3(cx - w * 0.5f, cy - h * 0.5f, z));
        s.Add(new Vector3(cx + w * 0.5f, cy - h * 0.5f, z));
        s.Add(new Vector3(cx + w * 0.5f, cy + h * 0.5f, z));
        s.Add(new Vector3(cx - w * 0.5f, cy + h * 0.5f, z));
        s.Add(new Vector3(cx - w * 0.5f, cy - h * 0.5f, z));
    }

    static void Circulo(List<Vector3> s, float cx, float cy, float r, float z = 0f)
    {
        s.Clear();
        const int n = 28;
        for (int i = 0; i <= n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            s.Add(new Vector3(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, z));
        }
    }

    // ---------- Las animaciones ----------

    // La hoja (rectángulo gris) y una curva; el tirador se mueve, pero siempre dentro de la hoja.
    void AnimarHoja(float t)
    {
        Rectangulo(otra, 0f, 0f, 0.24f, 0.09f);
        PonerLinea(lineaGris, otra);
        float u = Mathf.Sin(t / Ciclo * Mathf.PI * 2f);
        Vector3 a = new Vector3(-0.07f, -0.02f, 0f), b = new Vector3(0.07f, -0.02f, 0f);
        Vector3 asa = new Vector3(u * 0.04f, 0.03f + u * 0.008f, 0f);
        puntos.Clear();
        for (int i = 0; i <= 24; i++)
        {
            float k = i / 24f;
            // Curva de un tirador (cuadrática): del punto a al b, tirada hacia el asa.
            puntos.Add((1 - k) * (1 - k) * a + 2 * (1 - k) * k * asa + k * k * b);
        }
        PonerLinea(linea, puntos);
        otra.Clear();
        otra.Add(new Vector3(0f, 0.005f, 0f));
        otra.Add(asa);
        PonerLinea(extra, otra);
        dedo.gameObject.SetActive(true);
        dedo.localPosition = asa + new Vector3(0f, 0f, -0.004f);
    }

    // Un círculo relleno y otro solo línea: la línea pasa detrás y delante del relleno.
    void AnimarOrden(float t)
    {
        Circulo(puntos, -0.02f, 0f, 0.035f);
        Rellenar(puntos, Celeste, 1f);
        PonerLinea(linea, puntos);
        bool atras = t > Ciclo * 0.5f;
        Circulo(otra, 0.025f, 0.005f, 0.03f, atras ? 0.002f : -0.002f);
        PonerLinea(extra, otra);
        // El dedo "empuja" (se achica: va hacia adentro) y luego "tira" (crece: hacia ti).
        dedo.gameObject.SetActive(true);
        float empuje = Suave(Mathf.InverseLerp(Ciclo * 0.3f, Ciclo * 0.5f, t)) - Suave(Mathf.InverseLerp(Ciclo * 0.8f, Ciclo, t));
        dedo.localPosition = new Vector3(0.09f, 0.02f, -0.004f);
        dedo.localScale = Vector3.one * Mathf.Lerp(0.011f, 0.006f, empuje);
    }

    // Un relleno que cambia de tono a saltitos (así se ve el "respirar" de las manchas).
    void AnimarRellenoVivo(float t)
    {
        Circulo(puntos, 0f, 0f, 0.045f);
        int paso = Mathf.FloorToInt(t * 4f) % 4;
        float[] tonos = { 0f, 0.08f, -0.06f, 0.04f };
        Color c = Verde * (1f + tonos[paso]);
        c.a = 1f;
        Rellenar(puntos, c, 1f);
        PonerLinea(linea, puntos);
    }

    // Un panel con su reproductor y una página que se despliega y se guarda.
    void AnimarMenu(float t)
    {
        Rectangulo(puntos, 0f, 0.03f, 0.2f, 0.035f);
        PonerLinea(linea, puntos);
        float abre = Suave(Mathf.InverseLerp(0.6f, 1.2f, t)) - Suave(Mathf.InverseLerp(3.2f, 3.8f, t));
        float alto = Mathf.Lerp(0.004f, 0.05f, abre);
        Rectangulo(otra, 0f, 0.0125f - alto * 0.5f, 0.2f, alto);
        PonerLinea(extra, otra);
        // La pestaña que se toca.
        Rectangulo(forma, -0.05f, 0.0055f, 0.04f, 0.012f);
        PonerLinea(lineaGris, forma);
        dedo.gameObject.SetActive(t < 1.4f || (t > 2.8f && t < 3.6f));
        dedo.localPosition = new Vector3(-0.05f, 0.0055f, -0.004f);
    }

    // La hoja con su papel blanco: aparece, queda a la mitad y luego transparente.
    void AnimarPapel(float t)
    {
        Rectangulo(puntos, 0f, 0f, 0.2f, 0.08f);
        int estado = Mathf.FloorToInt(t / Ciclo * 3f) % 3;
        if (estado < 2)
        {
            puntos.RemoveAt(puntos.Count - 1);
            Rellenar(puntos, estado == 0 ? Color.white : Color.Lerp(FondoDemo, Color.white, 0.5f), 1f);
        }
        Rectangulo(otra, 0f, 0f, 0.2f, 0.08f);
        PonerLinea(lineaGris, otra);
        // Un trazo de lápiz.
        forma.Clear();
        for (int i = 0; i <= 20; i++)
        {
            float k = i / 20f;
            forma.Add(new Vector3(-0.07f + k * 0.12f, Mathf.Sin(k * 6f) * 0.015f, -0.001f));
        }
        PonerLinea(linea, forma);
        // El botón de la esquina.
        Rectangulo(otra, 0.085f, 0.033f, 0.03f, 0.01f, -0.001f);
        PonerLinea(extra, otra);
        dedo.gameObject.SetActive(Mathf.Repeat(t, Ciclo / 3f) < 0.5f);
        dedo.localPosition = new Vector3(0.085f, 0.033f, -0.004f);
    }

    // La paleta con el imán (a la derecha) y el cartelito de datos abajo.
    void AnimarPaleta(float t)
    {
        Circulo(puntos, -0.04f, 0.005f, 0.04f);
        Rellenar(puntos, Color.white, 1f);
        PonerLinea(linea, puntos);
        Circulo(otra, 0.01f, 0.012f, 0.008f);
        PonerLinea(extra, otra);
        Rectangulo(forma, 0.06f, -0.01f, 0.08f, 0.03f);
        PonerLinea(lineaGris, forma);
        bool toca = Mathf.Repeat(t, 1.5f) < 0.6f;
        dedo.gameObject.SetActive(true);
        dedo.localPosition = new Vector3(0.01f, 0.012f, toca ? -0.003f : -0.012f);
        if (toca)
            Rellenar(puntos, Amarillo, 0.25f);
    }
}

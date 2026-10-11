using System.Collections.Generic;
using UnityEngine;

// Tutorial de NOVEDADES (sección 4 del panel "?"): lo nuevo de las últimas versiones, con una animación corta
// de cada cosa. Se actualiza en cada versión.
//  1 · Capas en profundidad (empujar / atraer).   5 · Paleta con botones hijos (LÍNEA y RELLENO), estable.
//  2 · Puntos de tela (relieve en el relleno).    6 · Relleno vivo y su tamaño (2 dedos).
//  3 · Girar con el pulgar (fondos 360).          7 · Fondos 360 (ventanita) y papel blanco del boceto.
//  4 · El lápiz: Pegado / Suave / Cuerda y recta.  8 · Las líneas 2D se quedan en su hoja.
public class TutorialNovedades : TarjetaTutorial
{
    static readonly Color Verde = new Color(0.45f, 0.72f, 0.42f);
    static readonly Color Amarillo = new Color(1f, 0.85f, 0.35f);
    static readonly Color Celeste = new Color(0.55f, 0.78f, 1f);

    LineRenderer extra;   // una segunda línea (para frente/fondo y los rectángulos)
    LineRenderer arco;    // el arco que une una raíz de la paleta con sus hijos
    readonly LineRenderer[] hijos = new LineRenderer[4];
    readonly List<Vector3> otra = new List<Vector3>();

    protected override int Paginas => 8;

    protected override void ArmarExtra()
    {
        extra = Linea("Extra", tutorial.materialNegro, 0.0025f);
        arco = Linea("Arco", tutorial.materialNegro, 0.0012f);
        for (int i = 0; i < hijos.Length; i++)
            hijos[i] = Linea("Hijo" + i, tutorial.materialNegro, 0.0016f);
    }

    protected override string Titulo(int p)
    {
        switch (p)
        {
            case 0: return Tx("1 · Layers in depth", "1 · Capas en profundidad");
            case 1: return Tx("2 · Cloth points", "2 · Puntos de tela");
            case 2: return Tx("3 · Turn with your thumb", "3 · Girar con el pulgar");
            case 3: return Tx("4 · The pencil: how it follows you", "4 · El lápiz: cómo te sigue");
            case 4: return Tx("5 · Palette with child buttons", "5 · Paleta con botones hijos");
            case 5: return Tx("6 · Live fill and its size", "6 · Relleno vivo y su tamaño");
            case 6: return Tx("7 · 360 backgrounds and white paper", "7 · Fondos 360 y papel blanco");
            default: return Tx("8 · 2D lines stay on their sheet", "8 · Las líneas 2D se quedan en su hoja");
        }
    }

    protected override string Explicacion(int p)
    {
        switch (p)
        {
            case 0:
                return Tx(
                    "Hand menu > LAYERS: the layers hang like sheets of paper at the top center of your view. The current one is solid; the ones in front and behind are see-through. PUSH the current one (about 3 cm) = the layer in front; PULL it toward you = the one behind. They close by themselves after 8 seconds.",
                    "Menú de la mano > CAPAS: las capas cuelgan como papeles arriba al centro de tu vista. La actual se ve sólida; las de adelante y atrás, transparentes. EMPUJA la actual (unos 3 cm) = la capa de adelante; ATRÁELA hacia ti = la de atrás. Se cierran solas a los 8 segundos.");
            case 1:
                return Tx(
                    "In node mode, touch INSIDE the fill of the selected shape and hold still for half a second: a purple diamond appears. Move it toward you or away: the cloth bends smoothly, like a bell, and the edge stays on the line. Eraser on the diamond = it's removed.",
                    "En el modo nodos, toca DENTRO del relleno de la figura elegida y quédate quieto medio segundo: sale un rombito morado. Llévalo hacia ti o hacia el fondo: la tela se dobla suave, como una campana, y el borde se queda en la línea. Borrador sobre el rombito = se quita.");
            case 2:
                return Tx(
                    "With either hand in a loose fist, slide your thumb along the side of your index finger to the left or right: you turn 30° to see the back of the 360 background. Your drawings stay in front of you.",
                    "Con cualquier mano en puño suelto, desliza el pulgar sobre el costado del índice hacia la izquierda o la derecha: giras 30° para ver la parte de atrás del fondo 360. Tus dibujos se quedan delante de ti.");
            case 3:
                return Tx(
                    "In a Sketch layer in 2D: palette > LINE > Pencil: Stuck (right on your finger), Smooth (a little dragged) or String (it follows you on a short string, very clean lines). The straight-line gesture also draws with the pencil.",
                    "En una capa de Boceto en 2D: paleta > LÍNEA > Lápiz: Pegado (justo en tu dedo), Suave (un poco arrastrado) o Cuerda (te sigue con un hilo, líneas muy limpias). El gesto de la recta también dibuja con el lápiz.");
            case 4:
                return Tx(
                    "Around the palette there are two buttons: LINE (at 10 o'clock) and FILL (at 2 o'clock). Tap one and its children come out joined by an arc (wobble, strands, magnet, halo... or texture, bucket, speed). The colors paint whatever is open. The center shows how it looks, with a small label (\"Strands 3\"). The palette no longer shakes: with your finger close, it stays still.",
                    "Alrededor de la paleta hay dos botones: LÍNEA (a las 10) y RELLENO (a las 2). Toca uno y salen sus hijos unidos por un arco (temblor, hebras, imán, halo... o textura, cubeta, velocidad). Los colores pintan lo que esté abierto. El centro te muestra cómo queda, con un textito (\"Hebras 3\"). La paleta ya no tiembla: con tu dedo cerca se queda quieta.");
            case 5:
                return Tx(
                    "Palette > FILL (at 2 o'clock) > Texture: Plain → Facets → Patches → Brushstrokes, and its speed (>>). SIZE: put your right thumb and index on the fill and open or close them, like zooming on a phone. That size stays for your next fills. The patches now show much more, and the fill no longer peeks out of the line.",
                    "Paleta > RELLENO (a las 2) > Textura: Liso → Facetas → Manchas → Pinceladas, y su velocidad (>>). El TAMAÑO: pon el pulgar y el índice derechos sobre el relleno y ábrelos o ciérralos, como el zoom del teléfono. Ese tamaño queda para tus próximos rellenos. Las manchas ahora se notan mucho más y el relleno ya no se sale de la línea.");
            case 6:
                return Tx(
                    "Background button: Grid → White → Reality → 360. At 360 a small window with all the photos opens: tap one to try it. In a Sketch layer in 2D, behind the pencil there is white paper; its corner button: White → 50% → Clear.",
                    "Botón Fondo: Cuadrícula → Blanco → Realidad → 360. Al llegar a 360 se abre una ventanita con todas las fotos: toca una para probarla. En una capa de Boceto en 2D, detrás del lápiz hay papel blanco; su botón de la esquina: Blanco → 50 % → Transparente.");
            default:
                return Tx(
                    "What you draw in Plane 2D is ENCHANTED on its layer's sheet: handles, nodes, lasso and moving it stay inside that sheet, even in 3D. To take lines out: top menu > + Layer > RELEASE (selected lines, or the whole layer).",
                    "Lo que dibujas en Plano 2D queda ENCANTADO en la hoja de su capa: tiradores, nodos, lazo y moverla se quedan dentro de esa hoja, aunque pases a 3D. Para sacarlas: menú de arriba > + Capa > LIBERAR (las elegidas o toda la capa).");
        }
    }

    protected override void Animar(int p, float t)
    {
        extra.positionCount = 0;
        lineaGris.positionCount = 0;
        arco.positionCount = 0;
        foreach (var h in hijos)
            h.positionCount = 0;
        SinRelleno();
        dedo.gameObject.SetActive(false);
        dedo.localScale = Vector3.one * 0.009f;
        switch (p)
        {
            case 0: AnimarCapas(t); break;
            case 1: AnimarTela(t); break;
            case 2: AnimarGiro(t); break;
            case 3: AnimarLapiz(t); break;
            case 4: AnimarPaleta(t); break;
            case 5: AnimarRellenoVivo(t); break;
            case 6: AnimarPapel(t); break;
            default: AnimarHoja(t); break;
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
        // Pulgar e índice abriéndose y cerrándose: la mancha (circulito gris) crece y se achica con ellos.
        float abre = 0.5f + 0.5f * Mathf.Sin(t / Ciclo * Mathf.PI * 2f);
        Circulo(forma, -0.012f, 0.008f, Mathf.Lerp(0.006f, 0.022f, abre), -0.001f);
        PonerLinea(lineaGris, forma);
        Vector3 baseMano = new Vector3(0.03f, -0.03f, -0.003f);
        float aIndice = (115f - 25f * abre) * Mathf.Deg2Rad, aPulgar = (170f + 10f * abre) * Mathf.Deg2Rad;
        Vector3 indice = baseMano + new Vector3(Mathf.Cos(aIndice), Mathf.Sin(aIndice), 0f) * 0.035f;
        Vector3 pulgar = baseMano + new Vector3(Mathf.Cos(aPulgar), Mathf.Sin(aPulgar), 0f) * 0.028f;
        otra.Clear();
        otra.Add(indice);
        otra.Add(baseMano);
        otra.Add(pulgar);
        PonerLinea(extra, otra);
        dedo.gameObject.SetActive(true);
        dedo.localPosition = indice + new Vector3(0f, 0f, -0.001f);
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

    // La paleta: se toca la raíz LÍNEA (a las 10), se pone amarilla y le salen sus hijos unidos por un arco.
    void AnimarPaleta(float t)
    {
        const float cx = -0.01f, cy = -0.005f, radio = 0.032f, fuera = 0.046f;
        Circulo(puntos, cx, cy, radio);
        PonerLinea(linea, puntos);
        // En el centro, la vista previa: una línea ondulada.
        forma.Clear();
        for (int i = 0; i <= 12; i++)
        {
            float k = i / 12f;
            forma.Add(new Vector3(cx - 0.015f + k * 0.03f, cy + Mathf.Sin(k * Mathf.PI * 2f) * 0.004f, -0.001f));
        }
        PonerLinea(lineaGris, forma);
        float aRaiz = 150f * Mathf.Deg2Rad;
        Vector3 raiz = new Vector3(cx + Mathf.Cos(aRaiz) * fuera, cy + Mathf.Sin(aRaiz) * fuera, -0.001f);
        Circulo(otra, raiz.x, raiz.y, 0.007f, -0.001f);
        PonerLinea(extra, otra);
        bool abierta = t > 1.0f && t < 3.6f;
        if (abierta)
        {
            Rellenar(otra, Amarillo, 1f);
            // Los hijos salen uno tras otro, por el borde, unidos por el arco.
            int cuantos = Mathf.Clamp(Mathf.FloorToInt((t - 1.0f) / 0.25f) + 1, 1, hijos.Length);
            float hasta = 150f + 24f + (cuantos - 1) * 22f;
            otra.Clear();
            for (float g = 150f; g <= hasta + 0.01f; g += 3f)
                otra.Add(new Vector3(cx + Mathf.Cos(g * Mathf.Deg2Rad) * fuera, cy + Mathf.Sin(g * Mathf.Deg2Rad) * fuera, 0f));
            PonerLinea(arco, otra);
            for (int i = 0; i < cuantos; i++)
            {
                float a = (174f + i * 22f) * Mathf.Deg2Rad;
                Circulo(otra, cx + Mathf.Cos(a) * fuera, cy + Mathf.Sin(a) * fuera, 0.006f, -0.001f);
                PonerLinea(hijos[i], otra);
            }
        }
        bool toca = (t > 0.5f && t < 1.0f) || (t > 3.2f && t < 3.6f);
        dedo.gameObject.SetActive(toca);
        dedo.localPosition = raiz + new Vector3(0f, 0f, -0.003f);
    }

    // Capas en profundidad: la actual sólida, las de adelante más arriba y grandes, las de atrás más abajo.
    // El dedo empuja la actual y todo el montón se corre un paso hacia el fondo.
    void AnimarCapas(float t)
    {
        float paso = Suave(Mathf.InverseLerp(1.9f, 2.7f, t));
        bool empuja = t > 1.0f && t < 2.0f;
        int h = 0;
        for (int k = 0; k < 4; k++)
        {
            float s = k - 1 - paso; // antes: la capa 2 (k = 1) es la actual
            float ancho = 0.15f * (1f + 0.14f * s), alto = 0.03f * (1f + 0.14f * s);
            float y = 0.012f + (s > 0f ? s * 0.02f : s * 0.016f);
            if (Mathf.Abs(s) < 0.5f)
            {
                Rectangulo(puntos, 0f, y, ancho, alto, -0.001f);
                PonerLinea(linea, puntos);
                var lleno = new List<Vector3>(puntos);
                lleno.RemoveAt(lleno.Count - 1);
                Rellenar(lleno, Color.white, 1f);
                dedo.gameObject.SetActive(t < 2.0f);
                dedo.localPosition = new Vector3(-0.03f, y, -0.006f);
                dedo.localScale = Vector3.one * (empuja ? Mathf.Lerp(0.011f, 0.006f, Mathf.InverseLerp(1.0f, 1.9f, t)) : 0.011f);
            }
            else if (h < hijos.Length)
            {
                Rectangulo(otra, 0f, y, ancho, alto, s > 0f ? -0.002f : 0.002f);
                PonerLinea(hijos[h++], otra);
            }
        }
        lineaGris.positionCount = 0;
    }

    // Tela: tres hilos que cruzan el relleno se abomban suave alrededor del punto morado (el dedo) que sube y baja.
    void AnimarTela(float t)
    {
        Circulo(puntos, 0f, 0f, 0.045f);
        var lleno = new List<Vector3>(puntos);
        lleno.RemoveAt(lleno.Count - 1);
        Rellenar(lleno, Celeste, 1f);
        PonerLinea(linea, puntos);
        float alto = Mathf.Sin(t / Ciclo * Mathf.PI * 2f) * 0.014f;
        var hilos = new[] { extra, arco, hijos[0] };
        for (int h = 0; h < hilos.Length; h++)
        {
            float y0 = (h - 1) * 0.018f;
            otra.Clear();
            float medio = Mathf.Sqrt(Mathf.Max(0f, 0.045f * 0.045f - y0 * y0));
            for (int k = 0; k <= 20; k++)
            {
                float x = Mathf.Lerp(-medio, medio, k / 20f);
                float r2 = (x * x + y0 * y0) / (0.04f * 0.04f);
                float campana = r2 < 1f ? (1f - r2) * (1f - r2) : 0f;
                otra.Add(new Vector3(x, y0 + alto * campana, -0.001f));
            }
            PonerLinea(hilos[h], otra);
        }
        dedo.gameObject.SetActive(true);
        dedo.localPosition = new Vector3(0f, alto, -0.004f);
    }

    // Girar: el fondo 360 visto desde arriba (un círculo con marcas) gira de a 30° cuando el pulgar se desliza.
    void AnimarGiro(float t)
    {
        Circulo(puntos, 0f, 0f, 0.045f);
        PonerLinea(linea, puntos);
        int giros = t > 2.6f ? 2 : t > 1.1f ? 1 : 0;
        float extraGiro = giros * 30f + 30f * Suave(Mathf.InverseLerp(0f, 0.25f, t - (giros == 2 ? 2.6f : 1.1f))) - (giros > 0 ? 30f : 0f);
        for (int h = 0; h < hijos.Length; h++)
        {
            float a = (90f + h * 90f + extraGiro) * Mathf.Deg2Rad;
            otra.Clear();
            otra.Add(new Vector3(Mathf.Cos(a) * 0.038f, Mathf.Sin(a) * 0.038f, -0.001f));
            otra.Add(new Vector3(Mathf.Cos(a) * 0.052f, Mathf.Sin(a) * 0.052f, -0.001f));
            PonerLinea(hijos[h], otra);
        }
        // Tú, en el centro, mirando hacia arriba (siempre igual: lo que gira es el fondo).
        otra.Clear();
        otra.Add(new Vector3(-0.008f, -0.006f, -0.001f));
        otra.Add(new Vector3(0f, 0.012f, -0.001f));
        otra.Add(new Vector3(0.008f, -0.006f, -0.001f));
        otra.Add(new Vector3(-0.008f, -0.006f, -0.001f));
        PonerLinea(extra, otra);
        // El pulgar que se desliza (abajo a la derecha).
        float u = Mathf.Repeat(t, 1.5f);
        dedo.gameObject.SetActive(u > 0.6f && u < 1.2f);
        dedo.localPosition = new Vector3(Mathf.Lerp(0.065f, 0.09f, Mathf.InverseLerp(0.6f, 1.1f, u)), -0.04f, -0.004f);
    }

    // El lápiz con "cuerda": el dedo tiembla (gris) y el lápiz lo sigue liso, un poquito atrás (negro).
    void AnimarLapiz(float t)
    {
        float hasta = Mathf.Clamp01(t / (Ciclo * 0.8f));
        int n = Mathf.Max(2, Mathf.RoundToInt(40 * hasta));
        forma.Clear();
        otra.Clear();
        for (int k = 0; k < n; k++)
        {
            float u = k / 39f;
            float x = -0.07f + u * 0.14f;
            float y = Mathf.Sin(u * 5f) * 0.02f;
            float temblor = Mathf.Sin(u * 90f) * 0.003f + Mathf.Sin(u * 53f + 1f) * 0.002f;
            otra.Add(new Vector3(x, y + temblor + 0.012f, -0.001f));
            forma.Add(new Vector3(x - 0.004f, Mathf.Sin((u - 0.03f) * 5f) * 0.02f - 0.012f, -0.001f));
        }
        PonerLinea(lineaGris, otra);
        PonerLinea(linea, forma);
        dedo.gameObject.SetActive(true);
        dedo.localPosition = otra[otra.Count - 1] + new Vector3(0f, 0f, -0.003f);
    }
}

using System.Collections.Generic;
using UnityEngine;

// Tutorial de NOVEDADES (sección 4 del panel "?"): lo nuevo de las últimas versiones, con una animación corta
// de cada cosa. Se actualiza en cada versión (v36 "como de 2030", con los retoques de la v37).
//  1 · Capas: mira arriba (la pastillita).           5 · Menús que respiran.
//  2 · Mesa de trabajo: mira abajo.                  6 · Tela con pellizco.
//  3 · Líneas que cobran vida (destello, polvo...).  7 · Capas: empujar y atraer.
//  4 · Vidrio vivo y luz en tu dedo.                 8 · Fondos nuevos y retocados.
public class TutorialNovedades : TarjetaTutorial
{
    static readonly Color Amarillo = new Color(1f, 0.85f, 0.35f);
    static readonly Color Celeste = new Color(0.55f, 0.78f, 1f);
    static readonly Color Vidrio = new Color(0.84f, 0.89f, 0.96f);

    // La pastillita "Capa 2 · de 5" (en el recuadro de la animación).
    const float PastillaX = 0.02f, PastillaY = 0.042f, PastillaW = 0.05f, PastillaH = 0.011f;

    LineRenderer extra;   // una segunda línea (frente/fondo, la línea de tiempo...)
    LineRenderer arco;    // una línea fina
    LineRenderer mirada;  // el ojo y hacia dónde miras (azul)
    readonly LineRenderer[] hijos = new LineRenderer[4];
    readonly LineRenderer[] luces = new LineRenderer[3];   // líneas de luz celeste (destellos, la luz del dedo)
    readonly Transform[] polvo = new Transform[14];        // puntitos de polvo brillante
    readonly List<Vector3> otra = new List<Vector3>();
    readonly List<Vector3> tramo = new List<Vector3>();

    protected override int Paginas => 8;

    protected override void ArmarExtra()
    {
        extra = Linea("Extra", tutorial.materialNegro, 0.0025f);
        arco = Linea("Arco", tutorial.materialNegro, 0.0012f);
        mirada = Linea("Mirada", tutorial.materialAzul, 0.0018f);
        for (int i = 0; i < hijos.Length; i++)
            hijos[i] = Linea("Hijo" + i, tutorial.materialNegro, 0.0016f);
        var matLuz = Copia(tutorial.materialBlanco, new Color(0.45f, 0.72f, 1f));
        for (int i = 0; i < luces.Length; i++)
            luces[i] = Linea("Luz" + i, matLuz, 0.0045f);
        for (int i = 0; i < polvo.Length; i++)
        {
            polvo[i] = Bolita("Polvo" + i, matLuz, 0.004f);
            polvo[i].gameObject.SetActive(false);
        }
    }

    protected override string Titulo(int p)
    {
        switch (p)
        {
            case 0: return Tx("1 · Layers: look up", "1 · Capas: mira arriba");
            case 1: return Tx("2 · Work table: look down", "2 · Mesa de trabajo: mira abajo");
            case 2: return Tx("3 · Lines that come alive", "3 · Líneas que cobran vida");
            case 3: return Tx("4 · Living glass, light on your finger", "4 · Vidrio vivo y luz en tu dedo");
            case 4: return Tx("5 · Menus that breathe", "5 · Menús que respiran");
            case 5: return Tx("6 · Cloth points", "6 · Puntos de tela");
            case 6: return Tx("7 · Layers: push and pull", "7 · Capas: empujar y atraer");
            default: return Tx("8 · New and retouched backgrounds", "8 · Fondos nuevos y retocados");
        }
    }

    protected override string Explicacion(int p)
    {
        switch (p)
        {
            case 0:
                return Tx(
                    "At the top of your view there is a tiny pill, \"Layer 2 · of 5\": the layer you are on and how many there are. LOOK UP for half a second and the layers unfold, in the same spot as always. LOOK DOWN and they fold away by themselves. They don't appear while you draw or right after, or with the palette open. Hand menu > LAYERS also opens them (those close by themselves after 20 seconds).",
                    "Arriba de tu vista hay una pastillita chiquita, \"Capa 2 · de 5\": en qué capa estás y cuántas hay. MIRA ARRIBA medio segundo y las capas se despliegan, en el mismo lugar de siempre. BAJA la mirada y se recogen solas. No salen mientras dibujas ni justo después, ni con la paleta abierta. También se abren con el menú de la mano > CAPAS (esas se cierran solas a los 20 segundos).");
            case 1:
                return Tx(
                    "The top menu is now a WORK TABLE: LOOK DOWN for half a second (while not drawing) and it appears at the bottom of your view, like a mirror of the layers. The player and the timeline STAY and follow you calmly; with your finger on it, it stays still. The Animate, Mouths and Puppet tabs are on top and their pages open upward. The X (top right) puts it away.",
                    "El menú de arriba ahora es una MESA DE TRABAJO: MIRA ABAJO medio segundo (sin estar dibujando) y aparece abajo en tu vista, como un espejo de las capas. El reproductor y la línea de tiempo se QUEDAN y te siguen con calma; con tu dedo encima se queda quieta. Las pestañas Animar, Bocas y Títere están arriba y sus páginas se abren hacia arriba. La X (arriba a la derecha) la guarda.");
            case 2:
                return Tx(
                    "When you FINISH a line, a flash of light runs along it from end to end. When you ERASE it, it dissolves into glowing dust that falls slowly (and when you erase a node, so do the pieces of line that held it). When you UNDO, the line rolls itself back, from its end to its start, as if you rewound time.",
                    "Al TERMINAR una línea, un destello de luz la recorre de punta a punta. Al BORRARLA, se deshace en polvo brillante que cae despacio (y al borrar un nodo, los tramos de línea que lo sostenían). Al DESHACER, la línea se recoge sola, desde su final hasta su inicio, como si rebobinaras el tiempo.");
            case 3:
                return Tx(
                    "Menus and the palette are made of GLASS: your 360 background shows through, blurred, with a fine glint on the edge. The palette has no black rings anymore: its colors are pearls. The tip of your index finger carries a LITTLE LIGHT that makes fills and glass glow a little when your finger comes close. And your hands look like glass, with a glowing outline.",
                    "Los menús y la paleta son de VIDRIO: a través se ve borroso tu fondo 360, con un brillo fino en la orilla. La paleta ya no tiene aros negros: sus colores son perlas. La punta de tu índice lleva una LUCECITA que ilumina un poquito los rellenos y el vidrio cuando acercas el dedo. Y tus manos se ven de vidrio, con el contorno iluminado.");
            case 4:
                return Tx(
                    "The work table breathes with your attention: when you don't look at it, it becomes discreet (more transparent, softer letters) so it doesn't get in the way of your drawing. Look at it or bring your finger close and it wakes up. When it appears it comes out with a small bounce, like a spring, and a soft little sound.",
                    "La mesa de trabajo respira con tu atención: si no la miras, se vuelve discreta (más transparente y con letras suaves) para no estorbar tu dibujo. Al mirarla o acercar el dedo, despierta. Al aparecer sale con un pequeño rebote, como un resorte, y un sonidito fino.");
            case 5:
                return Tx(
                    "In node mode (left thumb + middle), PINCH a fill with your right hand (thumb + index) and pull it toward you or push it away; open the pinch and it stays. The cloth bends smoothly, like a bell, and the edge stays on the line. A purple diamond marks it: touch it with your index, like a node, to move it again. Leave it flat (or use the eraser) = it's removed.",
                    "En el modo nodos (izquierda pulgar + medio), PELLIZCA un relleno con la derecha (pulgar + índice) y jálalo hacia ti o empújalo al fondo; abre el pellizco y se queda así. La tela se dobla suave, como una campana, y el borde se queda en la línea. Un rombito morado lo marca: tócalo con el índice, como un nodo, para moverlo otra vez. Si lo dejas plano (o con el borrador), se quita.");
            case 6:
                return Tx(
                    "With the layers open, they hang like sheets of paper close to you (about 35 cm), 7 cm apart. The current one is solid; the ones in front fade away as they get near your eyes. PUSH the current one (about 3 cm) = the whole stack moves back and the layer in front arrives; PULL it toward you = the one behind arrives. \"+ Layer\" = a new layer in front (up to 12).",
                    "Con las capas abiertas, cuelgan como papeles cerca de ti (a unos 35 cm), a 7 cm una de otra. La actual se ve sólida; las de adelante se desvanecen al acercarse a tus ojos. EMPUJA la actual (unos 3 cm) = todo el montón se va al fondo y llega la de adelante; ATRÁELA hacia ti = llega la de atrás. \"+ Capa\" = una capa nueva adelante (hasta 12).");
            default:
                return Tx(
                    "Background button > 360, in the little window. NEW: New York rooftop and Luxury living room. RETOUCHED: Shanghai at night (no people, no haze and sharper), Sunset by the sea without the railing and Venice canal without the fence and with the water lower down (you feel you are on the bridge). The graffiti shelter and the Shanghai riverside by day are gone.",
                    "Botón Fondo > 360, en la ventanita. NUEVOS: Azotea en Nueva York y Sala de lujo. RETOCADOS: Shanghai de noche (sin gente, sin neblina y más nítido), Atardecer en el mar sin la baranda y Canal de Venecia sin la reja y con el agua más abajo (te sientes sobre el puente). Se fueron el refugio de grafitis y la orilla de Shanghai de día.");
        }
    }

    protected override void Animar(int p, float t)
    {
        linea.positionCount = 0;
        extra.positionCount = 0;
        lineaGris.positionCount = 0;
        arco.positionCount = 0;
        mirada.positionCount = 0;
        foreach (var h in hijos)
        {
            h.positionCount = 0;
            h.widthMultiplier = 0.0016f;
        }
        foreach (var l in luces)
            l.positionCount = 0;
        foreach (var b in polvo)
            if (b.gameObject.activeSelf)
                b.gameObject.SetActive(false);
        SinRelleno();
        dedo.gameObject.SetActive(false);
        dedo.localScale = Vector3.one * 0.009f;
        switch (p)
        {
            case 0: AnimarMirarArriba(t); break;
            case 1: AnimarMesa(t); break;
            case 2: AnimarLineas(t); break;
            case 3: AnimarVidrio(t); break;
            case 4: AnimarRespira(t); break;
            case 5: AnimarTela(t); break;
            case 6: AnimarCapas(t); break;
            default: AnimarFondos(t); break;
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

    // Rellena un rectángulo (sin repetir la primera esquina al final).
    void RellenarRect(float cx, float cy, float w, float h, Color c, float aparece)
    {
        Rectangulo(forma, cx, cy, w, h);
        forma.RemoveAt(forma.Count - 1);
        Rellenar(forma, c, aparece);
    }

    // Un ojo (circulito azul) y una flechita que sale de él hacia donde mira.
    void Mirada(Vector3 ojo, Vector3 hacia)
    {
        Vector3 dir = hacia - ojo;
        dir.z = 0f;
        if (dir.sqrMagnitude < 1e-8f)
            dir = Vector3.right;
        dir.Normalize();
        float a0 = Mathf.Atan2(dir.y, dir.x);
        const float r = 0.006f;
        otra.Clear();
        for (int i = 0; i <= 16; i++)
        {
            float a = a0 + i * Mathf.PI * 2f / 16f;
            otra.Add(new Vector3(ojo.x + Mathf.Cos(a) * r, ojo.y + Mathf.Sin(a) * r, -0.001f));
        }
        Vector3 punta = new Vector3(hacia.x, hacia.y, -0.001f);
        Vector3 lado = new Vector3(-dir.y, dir.x, 0f);
        otra.Add(punta);
        otra.Add(punta - dir * 0.007f + lado * 0.0045f);
        otra.Add(punta);
        otra.Add(punta - dir * 0.007f - lado * 0.0045f);
        PonerLinea(mirada, otra);
    }

    // Un resorte: empieza un poco chico, se pasa un poquito y se acomoda (como los menús al aparecer).
    static float Resorte(float t)
    {
        return 1f - 0.25f * Mathf.Exp(-6f * t) * Mathf.Cos(14f * t);
    }

    static Vector3 Escalar(Vector3 centro, Vector3 p, float s)
    {
        return centro + (p - centro) * s;
    }

    // Un número "al azar" entre 0 y 1, pero siempre el mismo para el mismo i (así el polvo no salta).
    static float Azar(int i)
    {
        float x = Mathf.Sin(i * 12.9898f) * 43758.5453f;
        return x - Mathf.Floor(x);
    }

    // ---------- Las animaciones ----------

    // Capas al mirar arriba: la pastillita arriba; el ojo mira arriba y las capas se despliegan desde ella;
    // baja la mirada y se recogen otra vez en la pastillita.
    void AnimarMirarArriba(float t)
    {
        Vector3 ojo = new Vector3(-0.105f, -0.018f, 0f);
        bool arriba = t > 0.4f && t < 1.3f;
        bool abajo = t > 2.8f && t < 3.5f;
        Mirada(ojo, arriba ? new Vector3(-0.092f, 0.02f, 0f) : abajo ? new Vector3(-0.092f, -0.05f, 0f) : new Vector3(-0.07f, -0.018f, 0f));
        float abre = Suave(Mathf.InverseLerp(1.0f, 1.4f, t)) * (1f - Suave(Mathf.InverseLerp(3.3f, 3.7f, t)));
        if (abre < 0.02f)
        {
            // La pastillita, con su "texto" (una rayita).
            Rectangulo(puntos, PastillaX, PastillaY, PastillaW, PastillaH, -0.001f);
            PonerLinea(lineaGris, puntos);
            RellenarRect(PastillaX, PastillaY, PastillaW, PastillaH, Color.white, 1f);
            otra.Clear();
            otra.Add(new Vector3(PastillaX - 0.015f, PastillaY, -0.0015f));
            otra.Add(new Vector3(PastillaX + 0.015f, PastillaY, -0.0015f));
            PonerLinea(hijos[3], otra);
            return;
        }
        // Las capas salen de la pastillita: la de atrás (abajo, chica), la actual (sólida) y la de adelante (arriba).
        CapaQueSale(hijos[1], PastillaX, -0.018f, 0.095f, 0.021f, abre, 0.002f, false);
        CapaQueSale(linea, PastillaX, 0.002f, 0.11f, 0.024f, abre, -0.001f, true);
        CapaQueSale(hijos[0], PastillaX, 0.024f, 0.125f, 0.027f, abre, -0.002f, false);
    }

    // Un rectángulo que va de la pastillita (abre = 0) a su lugar (abre = 1).
    void CapaQueSale(LineRenderer l, float x, float y, float w, float h, float abre, float z, bool lleno)
    {
        float cx = Mathf.Lerp(PastillaX, x, abre), cy = Mathf.Lerp(PastillaY, y, abre);
        float cw = Mathf.Lerp(PastillaW, w, abre), ch = Mathf.Lerp(PastillaH, h, abre);
        Rectangulo(otra, cx, cy, cw, ch, z);
        PonerLinea(l, otra);
        if (lleno)
            RellenarRect(cx, cy, cw, ch, Color.white, 1f);
    }

    // La mesa de trabajo: el ojo mira abajo y la mesa aparece con un rebote; al volver a mirar al frente se
    // QUEDA (con su reproductor y la línea de tiempo andando).
    void AnimarMesa(float t)
    {
        Vector3 ojo = new Vector3(-0.105f, 0.02f, 0f);
        bool abajo = t > 0.3f && t < 2.6f;
        Mirada(ojo, abajo ? new Vector3(-0.088f, -0.012f, 0f) : new Vector3(-0.07f, 0.02f, 0f));
        if (t < 0.9f)
            return;
        float s = Resorte(t - 0.9f);
        Vector3 c = new Vector3(0.02f, -0.03f, 0f);
        // La mesa, inclinada: un trapecio de vidrio.
        forma.Clear();
        forma.Add(Escalar(c, new Vector3(-0.05f, -0.048f, 0f), s));
        forma.Add(Escalar(c, new Vector3(0.09f, -0.048f, 0f), s));
        forma.Add(Escalar(c, new Vector3(0.075f, -0.012f, 0f), s));
        forma.Add(Escalar(c, new Vector3(-0.035f, -0.012f, 0f), s));
        Rellenar(forma, Vidrio, 1f);
        otra.Clear();
        foreach (var q in forma)
            otra.Add(q + new Vector3(0f, 0f, -0.001f));
        otra.Add(otra[0]);
        PonerLinea(linea, otra);
        // La línea de tiempo y su cabezal, que avanza.
        otra.Clear();
        otra.Add(Escalar(c, new Vector3(-0.035f, -0.04f, -0.0015f), s));
        otra.Add(Escalar(c, new Vector3(0.075f, -0.04f, -0.0015f), s));
        PonerLinea(extra, otra);
        float x = Mathf.Lerp(-0.035f, 0.075f, Mathf.Repeat((t - 0.9f) / 2.4f, 1f));
        otra.Clear();
        otra.Add(Escalar(c, new Vector3(x, -0.045f, -0.002f), s));
        otra.Add(Escalar(c, new Vector3(x, -0.035f, -0.002f), s));
        PonerLinea(hijos[3], otra);
        // El botón de reproducir (un triangulito) y dos botones más.
        otra.Clear();
        otra.Add(Escalar(c, new Vector3(-0.028f, -0.031f, -0.0015f), s));
        otra.Add(Escalar(c, new Vector3(-0.028f, -0.019f, -0.0015f), s));
        otra.Add(Escalar(c, new Vector3(-0.018f, -0.025f, -0.0015f), s));
        otra.Add(otra[0]);
        PonerLinea(hijos[0], otra);
        for (int i = 0; i < 2; i++)
        {
            Rectangulo(otra, i * 0.022f, -0.025f, 0.014f, 0.009f, -0.0015f);
            for (int k = 0; k < otra.Count; k++)
                otra[k] = Escalar(c, otra[k], s);
            PonerLinea(hijos[1 + i], otra);
        }
    }

    // Las dos ondas de la animación de las líneas (k: 0 = inicio, 1 = final).
    static Vector3 OndaArriba(float k)
    {
        return new Vector3(-0.08f + k * 0.16f, 0.024f + Mathf.Sin(k * Mathf.PI * 2f) * 0.012f, -0.001f);
    }

    static Vector3 OndaAbajo(float k)
    {
        return new Vector3(-0.08f + k * 0.16f, -0.028f + Mathf.Sin(k * 5f + 1f) * 0.008f, -0.001f);
    }

    // Puntos de una onda, de "desde" a "hasta" (corridos un poquito hacia ti con "z").
    static void Onda(List<Vector3> s, bool deArriba, float desde, float hasta, float z)
    {
        s.Clear();
        if (hasta - desde < 0.005f)
            return;
        int m = Mathf.Max(2, Mathf.CeilToInt(30f * (hasta - desde)));
        for (int i = 0; i <= m; i++)
        {
            float k = Mathf.Lerp(desde, hasta, i / (float)m);
            s.Add((deArriba ? OndaArriba(k) : OndaAbajo(k)) + new Vector3(0f, 0f, z));
        }
    }

    // Líneas que cobran vida: la de arriba se dibuja, un destello la recorre y luego se deshace en polvo que cae;
    // la de abajo se rebobina (se recoge desde su final hasta su inicio).
    void AnimarLineas(float t)
    {
        float dibuja = Mathf.Clamp01(t / 0.9f);
        if (t < 2.35f)
        {
            Onda(puntos, true, 0f, dibuja, 0f);
            PonerLinea(t < 2.2f ? linea : lineaGris, puntos);
        }
        if (t < 0.9f)
        {
            dedo.gameObject.SetActive(true);
            dedo.localPosition = OndaArriba(dibuja) + new Vector3(0f, 0f, -0.003f);
        }
        // El destello: una franja de luz que va de punta a punta.
        float g = (t - 0.9f) / 0.55f;
        if (g > -0.1f && g < 1.1f)
        {
            Onda(tramo, true, Mathf.Clamp01(g - 0.09f), Mathf.Clamp01(g + 0.09f), -0.001f);
            PonerLinea(luces[0], tramo);
        }
        // El polvo brillante: sale de la línea, se abre un poquito y cae mientras se apaga.
        float disuelve = Mathf.InverseLerp(2.2f, 3.2f, t);
        if (disuelve > 0f && disuelve < 1f)
            for (int i = 0; i < polvo.Length; i++)
            {
                Vector3 p = OndaArriba((i + 0.5f) / polvo.Length);
                p.x += (Azar(i) - 0.5f) * 0.012f * disuelve;
                p.y -= (0.006f + 0.02f * Azar(i + 50)) * disuelve + 0.012f * disuelve * disuelve;
                p.z = -0.002f;
                polvo[i].gameObject.SetActive(true);
                polvo[i].localPosition = p;
                polvo[i].localScale = Vector3.one * 0.0042f * (1f - disuelve);
            }
        // La de abajo se rebobina.
        float queda = 1f - Suave(Mathf.InverseLerp(3.0f, 3.45f, t));
        if (queda > 0.02f)
        {
            Onda(otra, false, 0f, queda, 0f);
            PonerLinea(extra, otra);
        }
    }

    // Un punto del borde de un rectángulo centrado (u de 0 a 1 = una vuelta, desde la esquina de abajo a la izquierda).
    static Vector3 Perimetro(float w, float h, float u, float z)
    {
        float d = Mathf.Repeat(u, 1f) * 2f * (w + h);
        if (d < w)
            return new Vector3(-w * 0.5f + d, -h * 0.5f, z);
        d -= w;
        if (d < h)
            return new Vector3(w * 0.5f, -h * 0.5f + d, z);
        d -= h;
        if (d < w)
            return new Vector3(w * 0.5f - d, h * 0.5f, z);
        d -= w;
        return new Vector3(-w * 0.5f, h * 0.5f - d, z);
    }

    // Vidrio vivo y luz en el dedo: a la izquierda, un panel de vidrio con un brillo que da la vuelta por su orilla; a la
    // derecha, un relleno que se ilumina cuando el dedo (con su lucecita) se le acerca.
    void AnimarVidrio(float t)
    {
        const float w = 0.1f, h = 0.07f, px = -0.065f;
        Rectangulo(puntos, px, 0f, w, h, -0.001f);
        PonerLinea(lineaGris, puntos);
        float u = Mathf.Repeat(t / Ciclo, 1f);
        tramo.Clear();
        for (int i = 0; i <= 6; i++)
            tramo.Add(Perimetro(w, h, u + i * 0.012f, -0.002f) + new Vector3(px, 0f, 0f));
        PonerLinea(luces[0], tramo);
        const float cx = 0.065f, r = 0.03f;
        Circulo(otra, cx, 0f, r, -0.001f);
        PonerLinea(linea, otra);
        float fx = cx + Mathf.Sin(t / Ciclo * Mathf.PI * 2f) * 0.05f, fy = 0.012f;
        float cerca = 1f - Mathf.Clamp01(Mathf.Abs(fx - cx) / 0.05f);
        otra.RemoveAt(otra.Count - 1);
        Rellenar(otra, Color.Lerp(Celeste, Color.white, 0.6f * cerca), 1f);
        dedo.gameObject.SetActive(true);
        dedo.localScale = Vector3.one * 0.007f;
        dedo.localPosition = new Vector3(fx, fy, -0.004f);
        Circulo(forma, fx, fy, 0.011f + 0.0015f * Mathf.Sin(t * 6f), -0.0025f);
        PonerLinea(luces[1], forma);
    }

    // Un menú que respira: aparece con un rebote; si lo miras está despierto (sólido); si miras a otro lado se
    // vuelve discreto (transparente, letras finitas) y al volver a mirarlo despierta.
    void AnimarRespira(float t)
    {
        Vector3 ojo = new Vector3(-0.105f, -0.01f, 0f);
        bool mira = t < 1.9f || t > 3.6f;
        Mirada(ojo, mira ? new Vector3(-0.07f, -0.006f, 0f) : new Vector3(-0.1f, 0.035f, 0f));
        if (t < 0.2f)
            return;
        float s = Resorte(t - 0.2f);
        float despierto = 1f - 0.7f * Suave(Mathf.InverseLerp(1.9f, 2.5f, t)) * (1f - Suave(Mathf.InverseLerp(3.6f, 3.9f, t)));
        const float cx = 0.03f, cy = 0f;
        Rectangulo(otra, cx, cy, 0.12f * s, 0.07f * s, -0.001f);
        PonerLinea(despierto > 0.65f ? linea : lineaGris, otra);
        RellenarRect(cx, cy, 0.12f * s, 0.07f * s, Vidrio, despierto);
        // Tres renglones de "texto": más finitos cuando está discreto.
        for (int i = 0; i < 3; i++)
        {
            float y = cy + (0.018f - i * 0.016f) * s;
            float x0 = cx - 0.04f * s;
            otra.Clear();
            otra.Add(new Vector3(x0, y, -0.0015f));
            otra.Add(new Vector3(x0 + (i == 2 ? 0.05f : 0.08f) * s, y, -0.0015f));
            hijos[i].widthMultiplier = Mathf.Lerp(0.0006f, 0.0016f, despierto);
            PonerLinea(hijos[i], otra);
        }
    }

    // La ventanita de fondos 360: seis fotos; las dos nuevas tienen marco de luz. El dedo toca una y luego la otra.
    void AnimarFondos(float t)
    {
        const float w = 0.042f, h = 0.026f;
        for (int i = 0; i < 6; i++)
        {
            float cx = (i % 3 - 1) * 0.05f, cy = i < 3 ? 0.017f : -0.017f;
            Rectangulo(otra, cx, cy, w, h, -0.001f);
            PonerLinea(i < 4 ? hijos[i] : luces[i - 4], otra);
        }
        bool primera = t < 2.7f;
        float desde = primera ? 0.9f : 2.7f;
        float cxElegida = primera ? 0f : 0.05f;
        if (t >= 0.9f)
            RellenarRect(cxElegida, -0.017f, w, h, Amarillo, Suave((t - desde) / 0.3f));
        bool toca = (t > 0.6f && t < 1.1f) || (t > 2.4f && t < 2.9f);
        dedo.gameObject.SetActive(toca);
        dedo.localPosition = new Vector3(t < 2f ? 0f : 0.05f, -0.017f, -0.004f);
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
}

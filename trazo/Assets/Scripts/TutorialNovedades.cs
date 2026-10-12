using System.Collections.Generic;
using UnityEngine;

// Tutorial de NOVEDADES (sección 4 del panel "?"): lo nuevo de las últimas versiones, con una animación corta
// de cada cosa. Se actualiza en cada versión (v36 "como de 2030", con los retoques de la v37 y la v38).
//  1 · Capas: la persiana de arriba.                 5 · Menús que respiran y se mueven.
//  2 · Mesa de trabajo: la persiana de abajo.        6 · Tela con pellizco.
//  3 · Líneas que cobran vida (destello, polvo...).  7 · Capas: empujar y atraer.
//  4 · Vidrio, luz en tu dedo y etiquetas.           8 · Fondos retocados y girar.
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
            case 0: return Tx("1 · Layers: the top blind", "1 · Capas: la persiana de arriba");
            case 1: return Tx("2 · Work table: the bottom blind", "2 · Mesa de trabajo: la persiana de abajo");
            case 2: return Tx("3 · Lines that come alive", "3 · Líneas que cobran vida");
            case 3: return Tx("4 · Glass, finger light and labels", "4 · Vidrio, luz en tu dedo y etiquetas");
            case 4: return Tx("5 · Menus that breathe and move", "5 · Menús que respiran y se mueven");
            case 5: return Tx("6 · Cloth points", "6 · Puntos de tela");
            case 6: return Tx("7 · Layers: push and pull", "7 · Capas: empujar y atraer");
            default: return Tx("8 · Retouched backgrounds and turning", "8 · Fondos retocados y girar");
        }
    }

    protected override string Explicacion(int p)
    {
        switch (p)
        {
            case 0:
                return Tx(
                    "At the top of your view there is a tiny pill, \"Layer 2 · of 5\". It works like a BLIND: put your finger on it and PULL IT DOWN, and the layers unfold. To put them away, pull UP the little pill under the layers. The circle to the right of the pill switches modes: blind (pull) or look (look up and they come out by themselves). Hand menu > LAYERS also opens them.",
                    "Arriba de tu vista hay una pastillita, \"Capa 2 · de 5\". Funciona como una PERSIANA: pon el dedo encima y JÁLALA HACIA ABAJO, y las capas se despliegan. Para guardarlas, jala HACIA ARRIBA la pastillita que queda debajo de las capas. El circulito a la derecha de la pastilla cambia el modo: persiana (jalar) o mirada (mirar arriba y salen solas). También se abren con el menú de la mano > CAPAS.");
            case 1:
                return Tx(
                    "Down below there is another pill with the frame and the time, and a little bar that shows how far along the animation is. PULL IT UP and the WORK TABLE comes out: player and timeline, with the Animate, Mouths and Puppet tabs folded. To put it away, pull DOWN the little pill under it. Its circle switches between blind and look (look down). PINCH an empty part of the table to move it.",
                    "Abajo hay otra pastillita con el fotograma y el tiempo, y una barrita que dice cuánto va de la animación. JÁLALA HACIA ARRIBA y sale la MESA DE TRABAJO: el reproductor y la línea de tiempo, con las pestañas Animar, Bocas y Títere replegadas. Para guardarla, jala HACIA ABAJO la pastillita de debajo. Su circulito cambia entre persiana y mirada (mirar abajo). PELLIZCA una parte vacía de la mesa para moverla.");
            case 2:
                return Tx(
                    "When you FINISH a line, a flash of light runs along it. When you ERASE it, it dissolves into glowing dust (dark dust on white backgrounds, so you can see it). The eraser is a glass orb: ROSE GOLD for lines and CHAMPAGNE for fills. Line tips are now round. When you UNDO, the line rolls itself back, as if you rewound time.",
                    "Al TERMINAR una línea, un destello de luz la recorre. Al BORRARLA, se deshace en polvo brillante (polvo oscuro en los fondos blancos, para que se note). El borrador es una esfera de vidrio: ORO ROSA para las líneas y CHAMPAÑA para los rellenos. Las puntas de las líneas ahora son redondas. Al DESHACER, la línea se recoge sola, como si rebobinaras el tiempo.");
            case 3:
                return Tx(
                    "Menus and the palette are made of GLASS: your 360 background shows through, blurred. The palette's colors are pearls. LABELS: in the palette, leave your index finger close to a button or a color for a moment and a little glass label with its name appears. The tip of your index finger carries a LITTLE LIGHT that makes fills and glass glow a little.",
                    "Los menús y la paleta son de VIDRIO: a través se ve borroso tu fondo 360. Los colores de la paleta son perlas. ETIQUETAS: en la paleta, deja el índice cerquita de un botón o de un color un momento y sale una etiquetita de vidrio con su nombre. La punta de tu índice lleva una LUCECITA que ilumina un poquito los rellenos y el vidrio.");
            case 4:
                return Tx(
                    "The work table breathes with your attention: when you don't look at it, it becomes discreet so it doesn't get in the way. MOVE ANY PANEL: pinch an empty part of it (not a button) and carry it wherever you want: My files, the 360 window, the image search, the table. While carrying it, pinch with your left hand too and spread or bring your hands together = size.",
                    "La mesa de trabajo respira con tu atención: si no la miras, se vuelve discreta para no estorbar. MUEVE CUALQUIER PANEL: pellizca una parte vacía (no un botón) y llévalo a donde quieras: Mis archivos, la ventanita de fondos 360, el buscador de imágenes, la mesa. Mientras lo llevas, pellizca también con la izquierda y separa o junta las manos = tamaño.");
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
                    "RETOUCHED: Venice at sunset (orange sky reflected in the water, no chimneys or bent rail), New York rooftop without the fence and sharper, and the Luxury living room tidy (no near sofa, only books on the table). Your own 360 photos have an X to delete them (touch it twice). TURN the background: slide your right index across your left forearm, or the arrows in the watch menu.",
                    "RETOCADOS: Venecia al atardecer (cielo naranja reflejado en el agua, sin chimeneas ni barandal torcido), la Azotea en Nueva York sin la reja y más nítida, y la Sala de lujo ordenada (sin el sofá cercano y con solo libros en la mesa). Tus fotos 360 tienen una X para borrarlas (tócala dos veces). GIRA el fondo: desliza el índice derecho sobre tu antebrazo izquierdo, o las flechas del menú del reloj.");
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
            case 0: AnimarPersianaArriba(t); break;
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
    // Capas: el dedo jala hacia abajo la pastillita (como una persiana) y las capas se despliegan; al final se
    // recogen. A la derecha de la pastilla, su circulito (persiana o mirada).
    void AnimarPersianaArriba(float t)
    {
        float abre = Suave(Mathf.InverseLerp(1.3f, 1.7f, t)) * (1f - Suave(Mathf.InverseLerp(3.5f, 3.9f, t)));
        if (abre < 0.02f)
        {
            float tiron = t < 2f ? Suave(Mathf.InverseLerp(0.5f, 1.3f, t)) : 0f;
            float y = PastillaY - 0.035f * tiron;
            // La lámina que se estira detrás, de donde estaba la pastilla hasta donde va.
            if (tiron > 0.01f)
            {
                Rectangulo(otra, PastillaX, (PastillaY + y) * 0.5f, PastillaW * 0.9f, PastillaY - y + PastillaH, 0.001f);
                PonerLinea(arco, otra);
            }
            Rectangulo(puntos, PastillaX, y, PastillaW, PastillaH, -0.001f);
            PonerLinea(lineaGris, puntos);
            RellenarRect(PastillaX, y, PastillaW, PastillaH, Color.white, 1f);
            otra.Clear();
            otra.Add(new Vector3(PastillaX - 0.015f, y, -0.0015f));
            otra.Add(new Vector3(PastillaX + 0.012f, y, -0.0015f));
            PonerLinea(hijos[3], otra);
            Circulo(otra, PastillaX + PastillaW * 0.5f + 0.008f, y, 0.0045f, -0.0015f);
            PonerLinea(hijos[2], otra);
            if (t < 1.4f)
            {
                // El dedo llega a la pastilla y la jala hacia abajo.
                float llega = Suave(Mathf.InverseLerp(0f, 0.5f, t));
                dedo.gameObject.SetActive(true);
                dedo.localPosition = Vector3.Lerp(new Vector3(PastillaX + 0.04f, PastillaY - 0.04f, -0.004f),
                                                  new Vector3(PastillaX, y, -0.004f), llega);
            }
            return;
        }
        // Las capas salen de la pastillita: la de atrás (abajo, chica), la actual (sólida) y la de adelante (arriba).
        CapaQueSale(hijos[1], PastillaX, -0.018f, 0.095f, 0.021f, abre, 0.002f, false);
        CapaQueSale(linea, PastillaX, 0.002f, 0.11f, 0.024f, abre, -0.001f, true);
        CapaQueSale(hijos[0], PastillaX, 0.024f, 0.125f, 0.027f, abre, -0.002f, false);
        // La pastillita de abajo (para guardarlas), que el dedo jala hacia arriba al final.
        float sube = Suave(Mathf.InverseLerp(2.9f, 3.5f, t));
        float yb = -0.042f + 0.012f * sube;
        Rectangulo(otra, PastillaX, yb, 0.03f, 0.007f, -0.0015f);
        PonerLinea(hijos[2], otra);
        if (t > 2.5f && t < 3.6f)
        {
            dedo.gameObject.SetActive(true);
            dedo.localPosition = new Vector3(PastillaX, yb, -0.004f);
        }
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

    // La mesa de trabajo: el dedo jala hacia arriba la pastillita de abajo (con su barrita de avance) y la mesa
    // aparece con un rebote, y se QUEDA (con su reproductor y la línea de tiempo andando).
    void AnimarMesa(float t)
    {
        if (t < 0.9f)
        {
            float tiron = Suave(Mathf.InverseLerp(0.3f, 0.85f, t));
            float y = -0.062f + 0.03f * tiron;
            const float px = 0.02f, pw = 0.05f, ph = 0.011f;
            Rectangulo(puntos, px, y, pw, ph, -0.001f);
            PonerLinea(lineaGris, puntos);
            RellenarRect(px, y, pw, ph, Color.white, 1f);
            otra.Clear();
            otra.Add(new Vector3(px - pw * 0.42f, y - ph * 0.3f, -0.0015f));
            otra.Add(new Vector3(px, y - ph * 0.3f, -0.0015f));
            PonerLinea(hijos[3], otra);
            Circulo(otra, px + pw * 0.5f + 0.008f, y, 0.0045f, -0.0015f);
            PonerLinea(hijos[2], otra);
            float llega = Suave(Mathf.InverseLerp(0f, 0.3f, t));
            dedo.gameObject.SetActive(true);
            dedo.localPosition = Vector3.Lerp(new Vector3(px + 0.04f, -0.02f, -0.004f), new Vector3(px, y, -0.004f), llega);
            return;
        }
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

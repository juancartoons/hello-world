using System.Collections.Generic;
using UnityEngine;

// Tutorial de NOVEDADES (sección 4 del panel "?"): lo nuevo de las últimas versiones, con una animación corta
// de cada cosa. Se actualiza en cada versión (v36 "como de 2030", con los retoques de la v37 a la v42).
//  1 · Capas: la persiana de arriba.                 5 · Mover paneles y vista limpia.
//  2 · Mesa de trabajo: la persiana de abajo.        6 · La voz (v40).
//  3 · Líneas que cobran vida (destello, polvo...).  7 · Línea de tiempo con gestos (v40).
//  4 · Vidrio, luz en tu dedo y etiquetas.           8 · Girar con las flechitas de abajo (v42).
//                                                    9 · Paneles flexibles (v41).
//                                                   10 · Fondos vivos: universo y nubes (v42).
//                                                   11 · Capas: 2D/3D, nombre, borrar, cebolla (v43).
//                                                   12 · La cámara (v43; v44: es una capa).
//                                                   13 · La mirada y el dedo jeringa (v44).
//                                                   14 · Atardecer en el desierto y capas (v44-v45).
public class TutorialNovedades : TarjetaTutorial
{
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

    protected override int Paginas => 20;

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
            case 4: return Tx("5 · Move panels and clean view", "5 · Mover paneles y vista limpia");
            case 5: return Tx("6 · Voice", "6 · La voz");
            case 6: return Tx("7 · Timeline with gestures", "7 · Línea de tiempo con gestos");
            case 7: return Tx("8 · Turning with the arrows", "8 · Girar con las flechas");
            case 8: return Tx("9 · Flexible panels", "9 · Paneles flexibles");
            case 9: return Tx("10 · Living backgrounds", "10 · Fondos vivos");
            case 10: return Tx("11 · Layers 2D/3D, onion skin and export", "11 · Capas 2D/3D, cebolla y exportar");
            case 11: return Tx("12 · The camera", "12 · La cámara");
            case 12: return Tx("13 · Gaze and the syringe finger", "13 · La mirada y el dedo jeringa");
            case 13: return Tx("14 · Desert sunset and layers", "14 · Atardecer en el desierto y capas");
            case 14: return Tx("15 · Frame by frame and your voice", "15 · Cuadro a cuadro y tu voz");
            case 15: return Tx("16 · Gradient, pencil and two hands", "16 · Degradado, lápiz y dos manos");
            case 16: return Tx("17 · Invisible head, Exit and timeline zoom", "17 · Cabeza invisible, Salir y zoom del timeline");
            case 17: return Tx("18 · Your head, untouched; pencil grains", "18 · Tu cabeza intacta y granos de lápiz");
            case 18: return Tx("19 · The stamp, smarter labels and pencil halo", "19 · El sello, etiquetas más listas y halo del lápiz");
            default: return Tx("20 · Dictate the face", "20 · Dicta la cara");
        }
    }

    protected override string Explicacion(int p)
    {
        switch (p)
        {
            case 0:
                return Tx(
                    "At the top of your view there is a tiny pill, \"Layer 2 · of 5\". It works like a BLIND: put your finger on it and PULL IT DOWN, and the layers unfold. To put them away, pull UP the little pill under the layers (or tap their X). Looking up no longer opens anything, so nothing pops up by accident. Watch menu > LAYERS also opens them.",
                    "Arriba de tu vista hay una pastillita, \"Capa 2 · de 5\". Funciona como una PERSIANA: pon el dedo encima y JÁLALA HACIA ABAJO, y las capas se despliegan. Para guardarlas, jala HACIA ARRIBA la pastillita que queda debajo de las capas (o toca su X). Mirar arriba ya no abre nada, así nada sale sin querer. También se abren con el menú del reloj > CAPAS.");
            case 1:
                return Tx(
                    "Down below there is another pill with the frame and the time, and a little bar that shows how far along the animation is. PULL IT UP and the WORK TABLE comes out: player and timeline, with the Animate, Mouths and Puppet tabs folded. To put it away, pull DOWN the little pill under it (or tap its X). Watch menu > DESK also brings it out. PINCH an empty part of the table to move it.",
                    "Abajo hay otra pastillita con el fotograma y el tiempo, y una barrita que dice cuánto va de la animación. JÁLALA HACIA ARRIBA y sale la MESA DE TRABAJO: el reproductor y la línea de tiempo, con las pestañas Animar, Bocas y Títere replegadas. Para guardarla, jala HACIA ABAJO la pastillita de debajo (o toca su X). También la saca el menú del reloj > MESA. PELLIZCA una parte vacía de la mesa para moverla.");
            case 2:
                return Tx(
                    "When you FINISH a line, a flash of light runs along it. When you ERASE it, it dissolves into glowing dust (dark dust on white backgrounds, so you can see it). The eraser is a glass orb: ROSE GOLD for lines and CHAMPAGNE for fills. With a 360 background, the halo now wraps the line's tip (it no longer looks cut). When you UNDO, the line rolls itself back, as if you rewound time.",
                    "Al TERMINAR una línea, un destello de luz la recorre. Al BORRARLA, se deshace en polvo brillante (polvo oscuro en los fondos blancos, para que se note). El borrador es una esfera de vidrio: ORO ROSA para las líneas y CHAMPAÑA para los rellenos. Con fondo 360, el halo ahora envuelve la punta de la línea (ya no se ve cortada). Al DESHACER, la línea se recoge sola, como si rebobinaras el tiempo.");
            case 3:
                return Tx(
                    "Menus and the palette are made of GLASS: your 360 background shows through, blurred. The palette's colors are pearls. LABELS: in the palette, leave your index finger close to a button or a color for a moment and a little glass label with its name appears. The tip of your index finger carries a LITTLE LIGHT that makes fills and glass glow a little.",
                    "Los menús y la paleta son de VIDRIO: a través se ve borroso tu fondo 360. Los colores de la paleta son perlas. ETIQUETAS: en la paleta, deja el índice cerquita de un botón o de un color un momento y sale una etiquetita de vidrio con su nombre. La punta de tu índice lleva una LUCECITA que ilumina un poquito los rellenos y el vidrio.");
            case 4:
                return Tx(
                    "MOVE ANY PANEL: pinch an empty part of it (not a button) and carry it wherever you want (My files, the 360 window, the image search, the table); pinching with your left hand too, spread your hands = size. CLEAN VIEW: make a DOUBLE FIST with your right hand (close, open, close quickly) or tap Clean view in the watch menu: the pills, the speaker and the comic letters disappear. Same again = they come back.",
                    "MUEVE CUALQUIER PANEL: pellizca una parte vacía (no un botón) y llévalo a donde quieras (Mis archivos, la ventanita de fondos 360, el buscador, la mesa); pellizcando también con la izquierda, separa las manos = tamaño. VISTA LIMPIA: haz un DOBLE PUÑO con la mano derecha (cerrar, abrir y cerrar rápido) o toca Vista limpia en el menú del reloj: se van las pastillas, el parlante y las letras de cómic. Lo mismo otra vez = vuelven.");
            case 5:
                return Tx(
                    "Touch the MICROPHONE (top left) and talk; your gestures keep working too. Say \"draw\" and your right index draws until you say \"stop\"; \"erase\", \"nodes\" and \"straight\" work the same. EVERY BUTTON shows its word when you bring your finger close (with a little microphone): that is exactly what you say. Also colors (\"red\"), \"thicker\", \"universe\"... and \"spanish\" changes the language. The first time it downloads the language (about 40 MB); after that it works without internet.",
                    "Toca el MICRÓFONO (arriba a la izquierda) y habla; tus gestos siguen funcionando. Di \"dibuja\" y tu índice derecho dibuja hasta que digas \"ya\"; igual \"borra\", \"nodos\" y \"recta\". CADA BOTÓN muestra su palabra al acercar el dedo (con un microfonito): esa es justo la que dices. También los colores (\"rojo\"), \"más grueso\", \"universo\"... y \"inglés\" cambia el idioma. La primera vez baja el idioma (unos 40 MB); después funciona sin internet.");
            case 6:
                return Tx(
                    "On the timeline bar: with the ERASE gesture (left fist), touch a key (orange mark) = it's removed. With the DRAW gesture (left thumb + index), touch the bar = a new key there. LOCKED BAR: while you drag the playhead (or a key), double tap with your left thumb + index and the bar follows your finger only sideways, even if your finger drifts far away: you can just watch your animation. Double tap again = back to normal.",
                    "En la barra de la línea de tiempo: con el gesto de BORRAR (puño izquierdo), toca una clave (marca naranja) = se quita. Con el gesto de DIBUJAR (pulgar + índice izquierdos), toca la barra = una clave nueva ahí. BARRA SUJETA: mientras arrastras el cabezal (o una clave), haz doble toque con pulgar + índice izquierdos y la barra sigue tu dedo solo de lado, aunque el dedo se aleje: puedes mirar solo tu animación. Otro doble toque = vuelve a la normalidad.");
            case 7:
                return Tx(
                    "TURN a 360 background: at the bottom of your view there are two little ARROWS, under the microphone and under the speaker. TAP one = the background turns 30°. TOUCH and SLIDE sideways = the background follows your finger (about 10 cm = 30°). They only show up with a 360 background. Also the watch arrows or the voice (\"turn right\").",
                    "GIRA un fondo 360: abajo en tu vista hay dos FLECHITAS, debajo del micrófono y debajo del parlante. TOCA una = el fondo gira 30°. TOCA y DESLIZA de lado = el fondo sigue a tu dedo (unos 10 cm = 30°). Solo salen con un fondo 360. También las flechas del reloj o la voz (\"gira derecha\").");
            case 8:
                return Tx(
                    "FLEXIBLE PANELS: PINCH a pill (without pulling it) and carry it to the TOP or the BOTTOM of your view. At the top you pull it DOWN and its panel unfolds downward: the desk at the top puts its tabs and pages UNDER the player; the layers at the bottom are seen from above. Both pills on the same edge sit side by side. If you open both from the same edge, they arrange themselves one at the top and one at the bottom. It's remembered.",
                    "PANELES FLEXIBLES: PELLIZCA una pastillita (sin jalarla) y llévala ARRIBA o ABAJO de tu vista. Arriba se jala HACIA ABAJO y su panel se despliega hacia abajo: la mesa arriba deja sus pestañas y páginas DEBAJO del reproductor; las capas abajo se ven desde arriba. Las dos pastillas en la misma orilla se ponen lado a lado. Si abres las dos desde la misma orilla, se acomodan una arriba y otra abajo. Se recuerda.");
            case 10:
                return Tx(
                    "Each LAYER has two ROUND buttons, 2D and 3D: each layer remembers its own (it's no longer on the watch). \"Pencil\" by voice = pencil + 2D at once. \"Name\" opens the keyboard (\"3 · Background\": the number stays). \"Delete\": tap twice (undo brings it back). The mannequin and the mouths make their OWN new layer. Buttons hide while you push the layers and come back a second later; press them on purpose (1 cm in). ONION SKIN (Animate page): the previous key in red and the next one in green. EXPORT: 1920x1080 or vertical 1080x1920, and an optional green background only in the MP4.",
                    "Cada CAPA tiene dos botones REDONDOS, 2D y 3D: cada capa recuerda el suyo (ya no está en el reloj). \"Lápiz\" por voz = lápiz y 2D de una vez. \"Nombre\" abre el teclado (\"3 · Fondo\": el número se queda). \"Borrar\": toca dos veces (deshacer la devuelve). El maniquí y las bocas crean SU capa nueva. Los botones se esconden mientras empujas las capas y salen un segundo después; se tocan a propósito (1 cm adentro). PAPEL CEBOLLA (página Animar): la clave anterior en rojo y la siguiente en verde. EXPORTAR: 1920x1080 o vertical 1080x1920, y fondo verde opcional solo en el MP4.");
            case 12:
                return Tx(
                    "THE EYE (under the microphone): a small sight appears in the middle of your view and what you look at glows. Say \"red\" and it turns red (\"red line\" / \"red fill\" to be exact), \"copy color\" and \"paste color\", \"delete this\", \"hide this\", \"this layer\", \"select\". In Nodes: \"corner\" / \"smooth\". Next to the sight it tells you what to say. THE SYRINGE: touch a color of the palette and your index fills up with it; touch a line or the inside of a shape and it gets painted (a drop or two may fall).",
                    "EL OJO (debajo del micrófono): sale una mira chiquita en el centro de tu vista y lo que miras brilla. Di \"rojo\" y se vuelve rojo (\"línea roja\" / \"relleno rojo\" para ser exacto), \"copiar color\" y \"pegar color\", \"borra esto\", \"oculta esto\", \"esta capa\", \"selecciona\". En Nodos: \"esquina\" / \"suave\". Junto a la mira te dice qué decir. LA JERINGA: toca un color de la paleta y tu índice se llena; toca una línea o el interior de una forma y se pinta (a veces cae una gotita).");
            case 13:
                return Tx(
                    "DESERT SUNSET: a new living background: dunes all around you lit by a low golden sun, violet shadows, a pink and purple sky with thin clouds, the first stars and the wind lifting dust far away. Say \"desert\". LAYERS: each one shows a thumbnail on its left; say \"layer 2\" to jump to it; leave your finger still on the name to rename it; delete with the little trash can (tap twice). The camera is a layer of its own (\"+ Camera\" = another one). The panels open with either hand, whatever the other one is doing.",
                    "ATARDECER EN EL DESIERTO: un fondo vivo nuevo: dunas a tu alrededor con la luz dorada del sol bajito, sombras violeta, un cielo rosado y morado con nubes finas, las primeras estrellas y el viento levantando polvo a lo lejos. Di \"desierto\". CAPAS: cada una muestra su miniatura a la izquierda; di \"capa 2\" para ir a ella; deja el dedo quieto en el nombre para cambiarlo; borra con el basurerito (toca dos veces). La cámara es su propia capa (\"+ Cámara\" = otra). Las persianas se abren con cualquier mano, haga lo que haga la otra.");
            default:
                return Tx(
                    "DICTATE THE FACE: before drawing, say \"profile face\" (or \"front face\"). Then, BEFORE each part, say its name and draw it: \"nose\", \"outer mouth\", \"jaw\", \"ear\", \"top of head\", \"nape\", \"eye\"... A little sign next to your hand says what you are drawing. Every node keeps that name, even when lines join. In profile, the nose tells the app where the face looks, so it knows which eye and ear you drew. From the front, say \"eye one\" / \"eye two\" (or left / right). Say \"end dictation\" and tap Invisible head: it goes straight to turning. The outline can be several pieces (top of head, nape, jaw...): they turn together as one silhouette. On the right: two little drawings (front and profile) with each zone in its color; below, only the leftover words. \"Turn\" makes it turn by itself.",
                    "DICTA LA CARA: antes de dibujar, di \"cara de perfil\" (o \"cara de frente\"). Luego, ANTES de cada parte, di su nombre y dibújala: \"nariz\", \"boca exterior\", \"mandíbula\", \"oreja\", \"cabeza arriba\", \"nuca\", \"ojo\"... Un cartelito junto a tu mano dice qué estás dibujando. Cada nodo guarda ese nombre, aunque las líneas se unan. De perfil, la nariz le dice a la app hacia dónde mira la cara: así sabe qué ojo y qué oreja dibujaste. De frente, di \"ojo uno\" / \"ojo dos\" (o izquierdo / derecho). Di \"fin del dictado\" y toca Cabeza invisible: va directo a girar. El borde puede ser de varias piezas (cabeza arriba, nuca, mandíbula...): giran juntas como una sola silueta. A la derecha: dos dibujitos (de frente y de perfil) con cada zona de un color; debajo, solo las palabras que sobran. \"Gira\" la hace girar sola.");
            case 19:
                return Tx(
                    "THE STAMP (invisible head): tap a word in the list on the right; it sticks to your index finger. Tap the nodes (or a line, away from its nodes = the whole line) that are that word: they turn green. PINCH with your left hand and let go = confirmed. Used words disappear from the list (front: left/right eye, eyebrow and ear; profile: just \"eye\"). Other ways: left hand in Nodes + touch the word (it follows your finger; let go of the left = it drops), pinch it, look at a line and say its name, or the blue square of a label (3 likely words). Labels now use zones (eyebrows above the eyes, nose in the center, mouth below, ears at the sides) and everything follows the app's language. The tutorial stays put and can be moved. PENCIL: no more grains; a soft HALO in the line's color (0 = none). Leaving the camera layer brings you back to where you were.",
                    "EL SELLO (cabeza invisible): toca una palabra de la lista de la derecha; se pega a tu índice. Toca los nodos (o una línea, lejos de sus nodos = la línea entera) que son esa palabra: se ponen verdes. PELLIZCA con la izquierda y suelta = confirmado. Las palabras usadas desaparecen de la lista (de frente: ojo, ceja y oreja izquierda/derecha; de perfil: solo \"ojo\"). Otras formas: izquierda en Nodos + toca la palabra (sigue tu dedo; sueltas la izquierda = cae), pellizcarla, mirar una línea y decir su nombre, o el cuadrito azul de una etiqueta (3 palabras probables). Las etiquetas ahora usan zonas (cejas sobre los ojos, nariz al centro, boca abajo, orejas a los lados) y todo sigue el idioma de la app. El tutorial se queda quieto y se puede mover. LÁPIZ: sin granos; un HALO suave del color de la línea (0 = sin halo). Al dejar la capa de cámara vuelves a donde estabas.");
            case 17:
                return Tx(
                    "INVISIBLE HEAD: the view you drew now looks IDENTICAL and your original NEVER changes: \"Use this view\" puts the new view on another frame (6 frames ahead if you are on the original's). While you turn it, the preview uses lines exactly like yours. Labels have 3 buttons: green check = confirm, red X = remove, blue square = rename (keyboard or voice). On the right, the VOCABULARY: pinch a word and drop it on a node. With the head open, say \"mouth\", \"nose\", \"eye\"... (the loop nodes stay chosen even after you let go). \"Save view\" keeps YOUR touched-up version. A step-by-step mini tutorial (\"Help\"). PENCIL: grain shapes (squares, round, asteroids, blurry, hairs), live changes and the panel opens by itself. The \"#\" is now one of the main colors. Onion skin turns on and off instantly.",
                    "CABEZA INVISIBLE: la vista que dibujaste ahora se ve IDÉNTICA y tu original NUNCA cambia: \"Usar esta vista\" pone la vista nueva en otro fotograma (6 más adelante si estás en el del original). Mientras la giras, la vista previa usa líneas iguales a las tuyas. Las etiquetas tienen 3 botones: visto verde = confirmar, X roja = quitar, cuadrito azul = cambiar el nombre (teclado o voz). A la derecha, el VOCABULARIO: pellizca una palabra y suéltala sobre un nodo. Con la cabeza abierta, di \"boca\", \"nariz\", \"ojo\"... (los nodos del lazo siguen elegidos aunque sueltes). \"Guardar vista\" guarda TU versión retocada. Un minitutorial paso a paso (\"Ayuda\"). LÁPIZ: formas de grano (cuadritos, redondos, asteroides, borrosos, pelos), cambios en vivo y el panel se abre solo. El \"#\" ahora es uno más de los colores principales. El papel cebolla se prende y se apaga al instante.");
            case 16:
                return Tx(
                    "INVISIBLE HEAD (Animate page): draw a head (front or profile) and turn it; \"Use this view\" moves YOUR SAME NODES into that view on the current frame, so two views MORPH. \"Save view\" keeps it in a library (like Flash's Ctrl+L). Name nodes by voice (\"nose tip\", \"chin\"...) or with \"Name node\"; \"Labels\" shows what the app recognizes (faint with \"?\" = guessed). EXIT (watch): saves and closes your drawing; with a blank sheet, it closes the app. \"+ New\" in My files. TIMELINE: pinch the two side handles with both hands and spread = bigger frames; cells shaded every 5. THICKNESS without circles: pinch with the right and drag up/down. Move any panel: left nodes gesture + touch it with your right index. Voice: \"frame 12\", \"delete frame 12\", \"delete layer 2\", \"back\", \"control z\", \"select all\", \"deselect\", \"turn off voice\", \"copy style\", \"paste style\".",
                    "CABEZA INVISIBLE (página Animar): dibuja una cabeza (de frente o de perfil) y gírala; \"Usar esta vista\" lleva TUS MISMOS NODOS a esa vista en el fotograma actual, así entre dos vistas hay MORPH. \"Guardar vista\" la deja en una biblioteca (como Ctrl+L en Flash). Nombra nodos con la voz (\"punta de nariz\", \"barbilla\"...) o con \"Nombrar nodo\"; \"Etiquetas\" muestra lo que la app reconoce (clarito con \"?\" = adivinado). SALIR (reloj): guarda y cierra tu dibujo; con la hoja en blanco, cierra la app. \"+ Nuevo\" en Mis archivos. TIMELINE: pellizca las dos agarraderas de los lados con las dos manos y sepáralas = cuadros más grandes; casillas sombreadas cada 5. GROSOR sin círculos: pellizca con la derecha y arrastra arriba/abajo. Mover cualquier panel: gesto de nodos izquierdo + tócalo con el índice derecho. Voz: \"fotograma 12\", \"borra fotograma 12\", \"borra capa 2\", \"atrás\", \"control z\", \"selecciona todo\", \"quita la selección\", \"apaga la voz\", \"copiar estilo\", \"pegar estilo\".");
            case 15:
                return Tx(
                    "GRADIENT (replaces the bucket in FILL): pick a color, then another, and touch inside a shape: it goes from the first (top) to the second (bottom); tap again for center-to-edge. Line TEXTURE: Smooth, PENCIL (paper grain, rough edge, boiling like hand-drawn frames) or strands; LIFE: Still, Soft, Jittery, Strong. NODES: keep the left nodes gesture, choose nodes with a loop and PINCH with the right in the air: spread = size, turn = rotate, move both = move. In THICKNESS the right index also loops several nodes. \"Paste\" appears where you look. Camera keys (diamonds) can be dragged; twist your wrist while holding the camera to change where it aims.",
                    "DEGRADADO (reemplaza la cubeta en RELLENO): elige un color, luego otro, y toca dentro de una forma: va del primero (arriba) al segundo (abajo); tócalo otra vez para del centro hacia afuera. TEXTURA de la línea: Lisa, LÁPIZ (grano de papel, orilla irregular, hervida como cuadros a mano) o hebras; VIDA: Quieta, Suave, Nerviosa, Fuerte. NODOS: sin soltar la izquierda, elige nodos con un lazo y PELLIZCA con la derecha en el aire: separar = tamaño, girar = rotar, mover las dos = mover. En GROSOR el índice derecho también hace lazo de varios nodos. \"Pegar\" sale donde miras. Las claves de la cámara (rombitos) se arrastran; gira la muñeca al sostener la cámara para cambiar a dónde apunta.");
            case 14:
                return Tx(
                    "FRAME BY FRAME: the one exception of the app. Tap \"Frame by frame\" (Animate page of the desk, or on the layer card) or say \"frame by frame\": you get a layer where each new frame starts EMPTY, like classic animation programs. You see the previous drawing in reddish and the next one in greenish (onion skin, always on here). Each drawing lasts until the next one. YOUR VOICE: say \"hello, how are you?\" and the head (or your mouths) repeats it with YOUR voice while its mouth moves. Also: the skin of your fingertip gets stained with ink (like dipping it in an inkwell) and it paints lines more easily; videos are much sharper (and there is vertical 2K); what you look at blinks softly; golden desert sand; the timeline feels a bit heavier when you drag it.",
                    "CUADRO A CUADRO: la única excepción de la app. Toca \"Cuadro a cuadro\" (página Animar de la mesa, o en la tarjeta de la capa) o di \"cuadro a cuadro\": tienes una capa donde cada cuadro nuevo empieza VACÍO, como los programas de animación de siempre. Ves el dibujo anterior rojizo y el siguiente verdoso (papel cebolla, aquí siempre prendido). Cada dibujo dura hasta el siguiente. TU VOZ: di \"hola, ¿cómo estás?\" y la cabeza (o tus bocas) lo repite con TU voz mientras mueve la boca. Además: la piel de la punta de tu dedo se mancha de tinta (como si lo metieras en un tintero) y pinta líneas más fácil; los videos salen mucho más nítidos (y hay vertical 2K); lo que miras titila suave; arena dorada en el desierto; el timeline se siente un poquito más pesado al arrastrarlo.");
            case 11:
                return Tx(
                    "THE CAMERA: it is a LAYER of its own, in front of all the others (or say \"camera\"). In 2D it points straight at your plane; in 3D it finds the best angle by itself. PINCH it with your RIGHT hand and move it: it orbits around what it's looking at. With your LEFT hand: it slides without changing where it looks. The monitor above it shows what it sees. \"Suggest\" = 3 framings to choose from. \"Key\" saves it on that frame: between keys the camera travels smoothly (diamonds on the timeline). Photos and videos are exported from it. \"Remove\" = automatic framing again.",
                    "LA CÁMARA: es una CAPA propia, adelante de todas (o di \"cámara\"). En 2D apunta de frente a tu plano; en 3D busca sola el mejor ángulo. PELLÍZCALA con la DERECHA y muévela: gira alrededor de lo que mira. Con la IZQUIERDA: se corre sin cambiar hacia dónde mira. El monitor de encima muestra lo que ve. \"Sugiere\" = 3 encuadres para elegir. \"Clave\" la guarda en ese fotograma: entre claves la cámara viaja suave (rombitos en la línea de tiempo). Fotos y videos salen desde ella. \"Quitar\" = encuadre automático otra vez.");
            case 9:
                return Tx(
                    "LIVING BACKGROUNDS (not photos: the app draws them). UNIVERSE: thousands of stars around you, the closest ones truly in 3D, and small colorful nebulae far away; there is no floor, you float in space. ABOVE THE CLOUDS: a sea of clouds below you and a warm sun far away. In the 360 window, \"Fly\" makes the stars or the clouds pass slowly by (always gently, so it doesn't make you dizzy). Also by voice: \"universe\", \"clouds\", \"fly\".",
                    "FONDOS VIVOS (no son fotos: los dibuja la app). UNIVERSO: miles de estrellas a tu alrededor, las cercanas de verdad en 3D, y pequeñas nebulosas de colores a lo lejos; no hay piso, flotas en el espacio. SOBRE LAS NUBES: un mar de nubes debajo de ti y un sol cálido a lo lejos. En la ventanita de fondos, \"Avanzar\" hace que las estrellas o las nubes pasen despacio (siempre suave, para no marear). También con la voz: \"universo\", \"nubes\", \"avanzar\".");
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
        PonerColor(matGris, p == 14 ? new Color(0.95f, 0.55f, 0.55f) : new Color(0.72f, 0.74f, 0.8f));   // v46: cebolla rojiza
        switch (p)
        {
            case 0: AnimarPersianaArriba(t); break;
            case 1: AnimarMesa(t); break;
            case 2: AnimarLineas(t); break;
            case 3: AnimarVidrio(t); break;
            case 4: AnimarRespira(t); break;
            case 5: AnimarVoz(t); break;
            case 6: AnimarBarra(t); break;
            case 7: AnimarGiro(t); break;
            case 8: AnimarFlexibles(t); break;
            case 9: AnimarVivos(t); break;
            case 10: AnimarPersianaArriba(t); break;
            case 11: AnimarRespira(t); break;
            case 12: AnimarVidrio(t); break;
            case 13: AnimarVivos(t); break;
            case 14: AnimarCuadros(t); break;
            case 15: AnimarVidrio(t); break;
            case 16: AnimarBarra(t); break;
            case 17: AnimarVidrio(t); break;
            case 18: AnimarLineas(t); break;
            default: AnimarVoz(t); break;
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
    // recogen con la pastillita de abajo.
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

    // Girar (v42): dos flechitas fijas abajo, a los lados. El dedo toca la de la derecha (el fondo, una tira de
    // fotos, salta un paso) y luego toca y arrastra (la tira sigue al dedo).
    void AnimarGiro(float t)
    {
        float salto = Suave(Mathf.InverseLerp(0.9f, 1.2f, t)) * 0.04f;
        float arrastre = t > 2.0f ? Mathf.Sin((t - 2.0f) / 2.2f * Mathf.PI * 2f) * 0.03f : 0f;
        float corre = -salto + arrastre;
        for (int i = 0; i < 4; i++)
        {
            float x = Mathf.Repeat(-0.07f + i * 0.04f + corre + 0.08f, 0.16f) - 0.08f;
            Rectangulo(otra, x, 0.025f, 0.036f, 0.024f, 0.002f);
            PonerLinea(hijos[i], otra);
        }
        // Las dos flechitas, abajo a los lados.
        Rectangulo(puntos, -0.075f, -0.045f, 0.022f, 0.014f, -0.001f);
        PonerLinea(lineaGris, puntos);
        Rectangulo(otra, 0.075f, -0.045f, 0.022f, 0.014f, -0.001f);
        PonerLinea(extra, otra);
        bool activa = (t > 0.8f && t < 1.3f) || t > 1.9f;
        RellenarRect(0.075f, -0.045f, 0.022f, 0.014f, activa ? Celeste : Color.white, 1f);
        otra.Clear();
        otra.Add(new Vector3(0.072f, -0.04f, -0.0015f));
        otra.Add(new Vector3(0.079f, -0.045f, -0.0015f));
        otra.Add(new Vector3(0.072f, -0.05f, -0.0015f));
        PonerLinea(arco, otra);
        otra.Clear();
        otra.Add(new Vector3(-0.072f, -0.04f, -0.0015f));
        otra.Add(new Vector3(-0.079f, -0.045f, -0.0015f));
        otra.Add(new Vector3(-0.072f, -0.05f, -0.0015f));
        PonerLinea(mirada, otra);
        dedo.gameObject.SetActive(true);
        Vector3 sobre = new Vector3(0.075f, -0.045f, -0.004f);
        if (t < 0.8f)
            dedo.localPosition = Vector3.Lerp(new Vector3(0.05f, -0.01f, -0.004f), sobre, Suave(t / 0.8f));
        else if (t < 1.3f)
            dedo.localPosition = sobre;
        else if (t < 1.9f)
            dedo.localPosition = sobre + new Vector3(-0.01f, 0.015f, 0f) * Mathf.Sin(Mathf.PI * Mathf.InverseLerp(1.3f, 1.9f, t));
        else
            dedo.localPosition = sobre + new Vector3(arrastre, 0f, 0f);
    }

    // Paneles flexibles: el dedo PELLIZCA la pastillita de la mesa (abajo) y la lleva arriba, junto a la de las capas
    // (las dos se ponen lado a lado); luego la jala hacia abajo y la mesa sale ARRIBA, con sus pestañas DEBAJO del
    // reproductor, y su página se despliega hacia abajo. La pastillita de las capas hace espacio: se va abajo.
    void AnimarFlexibles(float t)
    {
        const float pw = 0.05f, ph = 0.011f, arribaY = 0.046f, abajoY = -0.056f;
        float lleva = Suave(Mathf.InverseLerp(0.4f, 1.4f, t));
        float aparta = Suave(Mathf.InverseLerp(1.1f, 1.5f, t));
        float tiron = Suave(Mathf.InverseLerp(2.0f, 2.45f, t));
        float bajaCapas = Suave(Mathf.InverseLerp(2.7f, 3.2f, t));

        // La pastillita de las capas: arriba; se aparta a la izquierda y, con la mesa abierta arriba, se va abajo.
        float cx = Mathf.Lerp(Mathf.Lerp(0.02f, -0.012f, aparta), 0.02f, bajaCapas);
        float cy = Mathf.Lerp(arribaY, abajoY, bajaCapas);
        Rectangulo(otra, cx, cy, pw, ph, -0.001f);
        PonerLinea(hijos[0], otra);

        float mx = Mathf.Lerp(0.02f, 0.052f, aparta);
        if (t < 2.5f)
        {
            // La pastillita de la mesa: sube con el pellizco y luego se jala hacia abajo.
            float my = Mathf.Lerp(abajoY, arribaY, lleva) - 0.03f * tiron;
            if (tiron > 0.01f)
            {
                Rectangulo(otra, mx, (arribaY + my) * 0.5f, pw * 0.9f, arribaY - my + ph, 0.001f);
                PonerLinea(arco, otra);
            }
            bool llevada = t > 0.4f && t < 1.5f;
            float g = llevada ? 1.12f : 1f;   // agarrada: un poquito más grande
            Rectangulo(puntos, mx, my, pw * g, ph * g, -0.001f);
            PonerLinea(lineaGris, puntos);
            RellenarRect(mx, my, pw * g, ph * g, Color.white, 1f);
            otra.Clear();
            otra.Add(new Vector3(mx - pw * 0.3f, my, -0.0015f));
            otra.Add(new Vector3(mx + pw * 0.2f, my, -0.0015f));
            PonerLinea(hijos[3], otra);
            dedo.gameObject.SetActive(true);
            Vector3 sobre = new Vector3(mx, my, -0.004f);
            if (t < 0.4f)
                dedo.localPosition = Vector3.Lerp(new Vector3(mx + 0.04f, abajoY + 0.03f, -0.004f), sobre, Suave(t / 0.4f));
            else if (t < 1.5f || t >= 2f)
                dedo.localPosition = sobre;
            else
                dedo.localPosition = sobre + new Vector3(0.015f, -0.015f, 0f) * Mathf.Sin(Mathf.PI * Mathf.InverseLerp(1.5f, 2f, t));
            if (t > 0.3f && t < 1.5f)
            {
                // El pulgar junto al índice: es un pellizco (no un jalón).
                Circulo(otra, mx - 0.006f, my - 0.008f, 0.0045f, -0.004f);
                PonerLinea(arco, otra);
            }
            return;
        }

        // La mesa ARRIBA: reproductor y línea de tiempo...
        float s = Resorte(t - 2.5f);
        const float x = 0.025f, y = 0.036f;
        float w = 0.13f * s, h = 0.026f * s;
        Rectangulo(otra, x, y, w, h, 0f);
        PonerLinea(linea, otra);
        RellenarRect(x, y, w, h, Vidrio, 1f);
        otra.Clear();
        otra.Add(new Vector3(x - w * 0.42f, y + h * 0.15f, -0.0015f));
        otra.Add(new Vector3(x + w * 0.42f, y + h * 0.15f, -0.0015f));
        PonerLinea(extra, otra);
        // ... y DEBAJO del reproductor, sus tres pestañas (Animar, Bocas, Títere).
        float tw = 0.03f * s, th = 0.009f * s;
        float ty = y - h * 0.5f - 0.007f * s;
        float x0 = x - tw * 1.5f, x1 = x0 + tw, x2 = x1 + tw, x3 = x2 + tw;
        float abajo = ty - th * 0.5f, encima = ty + th * 0.5f;
        otra.Clear();
        otra.Add(new Vector3(x0, abajo, 0f));
        otra.Add(new Vector3(x3, abajo, 0f));
        otra.Add(new Vector3(x3, encima, 0f));
        otra.Add(new Vector3(x0, encima, 0f));
        otra.Add(new Vector3(x0, abajo, 0f));
        otra.Add(new Vector3(x1, abajo, 0f));
        otra.Add(new Vector3(x1, encima, 0f));
        otra.Add(new Vector3(x2, encima, 0f));
        otra.Add(new Vector3(x2, abajo, 0f));
        PonerLinea(hijos[1], otra);
        // Tocas una pestaña y su página se despliega HACIA ABAJO.
        float despliega = Suave(Mathf.InverseLerp(3.4f, 3.9f, t));
        if (despliega > 0.01f)
        {
            float alto = 0.05f * despliega;
            Rectangulo(otra, x, abajo - alto * 0.5f, 0.12f, alto, 0.001f);
            PonerLinea(hijos[2], otra);
        }
        if (t > 3.0f && t < 3.6f)
        {
            dedo.gameObject.SetActive(true);
            dedo.localPosition = Vector3.Lerp(new Vector3(x0 + tw * 0.5f + 0.03f, ty - 0.03f, -0.004f),
                                              new Vector3(x0 + tw * 0.5f, ty, -0.004f), Suave(Mathf.InverseLerp(3.0f, 3.35f, t)));
        }
    }

    // Fondos vivos: primero el universo (las estrellas salen del centro y pasan a tu lado: avanzas); luego, sobre las
    // nubes (un sol y nubes que pasan debajo).
    // v46: cuadro a cuadro: una pelota que rebota, un dibujo por cuadro; el anterior se ve rojizo (papel cebolla).
    static float Bote(int i)
    {
        return 0.035f - 0.07f * Mathf.Abs(Mathf.Sin(i / 4f * Mathf.PI));
    }

    void AnimarCuadros(float t)
    {
        int k = Mathf.FloorToInt(t * 2.5f) % 5;
        Circulo(otra, -0.08f + k * 0.04f, Bote(k), 0.014f, -0.001f);
        PonerLinea(linea, otra);
        if (k > 0)
        {
            Circulo(forma, -0.08f + (k - 1) * 0.04f, Bote(k - 1), 0.014f, -0.0005f);
            PonerLinea(lineaGris, forma);
        }
        else
        {
            lineaGris.positionCount = 0;
        }
    }

    void AnimarVivos(float t)
    {
        bool universo = t < 2.3f;
        if (universo)
        {
            RellenarRect(0.02f, 0f, 0.2f, 0.11f, new Color(0.05f, 0.06f, 0.14f), 1f);
            for (int i = 0; i < polvo.Length; i++)
            {
                float ang = Azar(i) * Mathf.PI * 2f;
                float fase = Mathf.Repeat(t * 0.45f + Azar(i + 40), 1f);
                float r = fase * fase * 0.07f;
                var p = new Vector3(0.02f + Mathf.Cos(ang) * r * 1.4f, Mathf.Sin(ang) * r, -0.002f);
                polvo[i].gameObject.SetActive(true);
                polvo[i].localPosition = p;
                polvo[i].localScale = Vector3.one * Mathf.Lerp(0.0015f, 0.0045f, fase);
            }
            return;
        }
        // Sobre las nubes: el cielo, el sol y tres nubes que pasan (vuelas).
        RellenarRect(0.02f, 0.015f, 0.2f, 0.08f, new Color(0.62f, 0.78f, 0.95f), 1f);
        Circulo(otra, 0.075f, 0.03f, 0.009f, -0.002f);
        PonerLinea(luces[0], otra);
        for (int i = 0; i < 3; i++)
        {
            float x = Mathf.Repeat(0.1f - (t - 2.3f) * 0.05f + i * 0.07f, 0.21f) - 0.085f;
            otra.Clear();
            for (int k = 0; k <= 12; k++)
            {
                float a = k / 12f * Mathf.PI;
                otra.Add(new Vector3(x - 0.02f + k * (0.04f / 12f), -0.03f + Mathf.Abs(Mathf.Sin(a * 2f)) * 0.008f, -0.002f));
            }
            PonerLinea(hijos[i], otra);
        }
    }

    // La voz: el micrófono "habla" (ondas) y, después de "dibuja", una línea se dibuja sola con el dedo hasta "ya".
    void AnimarVoz(float t)
    {
        Vector3 micro = new Vector3(-0.1f, 0.04f, 0f);
        Circulo(otra, micro.x, micro.y, 0.011f, -0.001f);
        PonerLinea(arco, otra);
        otra.Clear();
        otra.Add(new Vector3(micro.x, micro.y - 0.011f, -0.001f));
        otra.Add(new Vector3(micro.x, micro.y - 0.02f, -0.001f));
        PonerLinea(hijos[2], otra);
        bool habla = (t > 0.2f && t < 0.9f) || (t > 2.6f && t < 3.1f);
        if (habla)
        {
            float f = Mathf.Repeat(t * 2.5f, 1f);
            for (int i = 0; i < 2; i++)
                ArcoVoz(hijos[i], micro, 0.016f + Mathf.Repeat(f + i * 0.5f, 1f) * 0.02f);
        }
        if (t > 1.0f)
        {
            float dibuja = Mathf.InverseLerp(1.0f, 2.6f, t);
            Onda(otra, false, 0f, dibuja, -0.001f);
            PonerLinea(linea, otra);
            if (t < 2.75f)
            {
                dedo.gameObject.SetActive(true);
                dedo.localPosition = OndaAbajo(dibuja) + new Vector3(0f, 0f, -0.003f);
            }
        }
    }

    void ArcoVoz(LineRenderer l, Vector3 c, float r)
    {
        otra.Clear();
        for (int k = 0; k <= 10; k++)
        {
            float a = Mathf.Lerp(-50f, 50f, k / 10f) * Mathf.Deg2Rad;
            otra.Add(new Vector3(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r, -0.001f));
        }
        PonerLinea(l, otra);
    }

    // La línea de tiempo: el dedo arrastra el cabezal; luego (barra sujeta) se aleja hacia arriba y el cabezal lo sigue
    // de lado igual; al final, con el gesto de dibujar, tocar la barra pone una clave nueva.
    void AnimarBarra(float t)
    {
        const float y = -0.02f, x0 = -0.09f, x1 = 0.09f;
        otra.Clear();
        otra.Add(new Vector3(x0, y, -0.001f));
        otra.Add(new Vector3(x1, y, -0.001f));
        PonerLinea(linea, otra);
        float[] claves = { -0.06f, 0.0f, 0.05f };
        for (int i = 0; i < claves.Length; i++)
            Marca(hijos[i], claves[i], y, 0.006f);
        float x;
        float dy = 0f;
        if (t < 1.4f)
        {
            x = Mathf.Lerp(-0.07f, 0.03f, Suave(Mathf.InverseLerp(0.1f, 1.3f, t)));
        }
        else if (t < 3.0f)
        {
            x = Mathf.Lerp(0.03f, -0.04f, Suave(Mathf.InverseLerp(1.6f, 2.9f, t)));
            dy = Mathf.Lerp(0f, 0.045f, Suave(Mathf.InverseLerp(1.4f, 1.8f, t)));
        }
        else
        {
            x = Mathf.Lerp(-0.04f, 0.075f, Suave(Mathf.InverseLerp(3.0f, 3.5f, t)));
            dy = Mathf.Lerp(0.045f, 0f, Suave(Mathf.InverseLerp(3.0f, 3.5f, t)));
            if (t > 3.6f)
                Marca(hijos[3], 0.075f, y, 0.006f);   // la clave nueva
        }
        Marca(extra, x, y, 0.012f);                   // el cabezal
        dedo.gameObject.SetActive(true);
        dedo.localPosition = new Vector3(x, y + dy, -0.004f);
    }

    // Una rayita vertical en la barra (las claves y el cabezal).
    void Marca(LineRenderer l, float x, float y, float alto)
    {
        otra.Clear();
        otra.Add(new Vector3(x, y - alto, -0.0015f));
        otra.Add(new Vector3(x, y + alto, -0.0015f));
        PonerLinea(l, otra);
    }
}

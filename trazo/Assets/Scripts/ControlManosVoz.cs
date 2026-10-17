using System.Collections.Generic;
using UnityEngine;

// LOS COMANDOS DE VOZ (parte de ControlManos, v40). La voz (Voz.cs) le pasa lo que entendió y aquí se hace.
// En el idioma de la app (español o inglés), cortos y con sus variantes:
//  - MODOS, como si la mano izquierda los sostuviera (la izquierda queda libre), hasta decir "YA":
//      "dibuja" (dibujas con la punta del índice derecho), "borra" (el índice derecho borra), "nodos", "recta".
//  - "deshaz", "rehaz" · "gira izquierda / derecha / atrás" (fondos 360) · "capas", "capa nueva" · "mesa",
//    "play", "pausa", "siguiente", "anterior", "clave", "quita clave" · "paleta", "guarda", "vista limpia",
//    "silencio" (o "música"), "halo".
//  - v42: cada botón dice su palabra en su etiqueta (al acercar el dedo, con un microfonito): la MISMA que se dice.
//    Nuevas: "plano", "fondo", "archivos", "esfera", "cubo", "cilindro", "a líneas", "ayuda", "inicio", "zoom",
//    "animar", "bocas", "títere", "video", "muestra" / "oculta" (la capa), "lápiz", "liberar", "avanzar" (fondos
//    vivos), "universo", "nubes", los COLORES ("rojo", "azul"...), "más grueso" / "más fino" y el IDIOMA (en
//    inglés di "spanish"; en español di "inglés").
//  - v43: "siguiente fondo" (sin abrir la ventanita) y el NOMBRE de cada fondo ("roma", "parque", "nubes"...);
//    "dos de" / "tres de" (2D o 3D en la capa), "lápiz" (lápiz Y 2D de una vez), "tinta"; el INSTRUMENTO ("piano",
//    "arpa", "drum and bass"...); "exportar" (el video).
//  - "Borrar todo" NO va por voz (por seguridad).
// Los gestos siguen funcionando al mismo tiempo.
public partial class ControlManos
{
    public enum ComandoVoz
    {
        Nada, Dibuja, Borra, Nodos, Recta, Ya, Deshaz, Rehaz, GiraIzquierda, GiraDerecha, GiraAtras, Capas, CapaNueva,
        Mesa, Play, Pausa, Siguiente, Anterior, Clave, QuitaClave, Paleta, Guarda, VistaLimpia, Silencio, Halo,
        // v42
        Plano, Fondo, Archivos, Esfera, Cubo, Cilindro, ALineas, Ayuda, Inicio, Zoom, Animar, Bocas, Titere, Video,
        Muestra, Oculta, Lapiz, Liberar, Avanza, Universo, Nubes, Color, Grueso, Fino, Espanol, Ingles,
        // v43
        En2D, En3D, Tinta, SiguienteFondo, FondoNombre, Instrumento, Camara, SugiereCamara, ClaveCamara, Cebolla,
        // v44: la mirada y las capas por número
        Mirada, ColorLinea, ColorRelleno, Copiar, Pegar, Esquina, Suave, BorraEsto, OcultaEsto, EstaCapa, Selecciona, IrACapa, Hola,
        // v45
        CopiarColor, PegarColor,
        // v46
        CuadroACuadro
    }

    // v43: los fondos que se pueden pedir por su nombre (el nombre exacto que tienen en Escenario).
    static readonly string[] FondosPorVoz =
    {
        "Roma de noche", "Shanghai de noche", "Canal de Venecia", "Callejón de Venecia", "Amanecer", "Atardecer en el mar",
        "Mirador", "Parque", "Camino entre flores", "Campo seco", "Golf de noche", "Azotea en Nueva York", "Sala de lujo",
        "Cuadrícula", "Realidad", "Atardecer en el desierto",
    };

    struct FrasesComando
    {
        public ComandoVoz comando;
        public int dato;                    // (los colores: cuál de la paleta)
        public string nombreEs, nombreEn;   // como se muestra ("Gira derecha"): en la etiqueta del botón y en la voz
        public string[] es, en;             // lo que se puede decir
    }

    static readonly string[] Nada0 = new string[0];

    static readonly FrasesComando[] Comandos = ConExtras(new[]
    {
        C(ComandoVoz.Dibuja, "Dibuja", "Draw", new[] { "dibuja", "dibujar", "dibujo", "pinta", "pintar" }, new[] { "draw", "paint" }),
        C(ComandoVoz.Borra, "Borra", "Erase", new[] { "borra", "borrar", "borrador" }, new[] { "erase", "eraser", "delete" }),
        C(ComandoVoz.Nodos, "Nodos", "Nodes", new[] { "nodos", "nodo", "edita", "editar" }, new[] { "nodes", "node", "edit" }),
        C(ComandoVoz.Recta, "Recta", "Straight", new[] { "recta", "línea recta", "raya" }, new[] { "straight", "straight line", "line" }),
        C(ComandoVoz.Ya, "Ya", "Stop", new[] { "ya", "listo", "para", "parar", "alto", "basta", "termina", "terminar" }, new[] { "stop", "done", "enough", "finish", "okay" }),
        C(ComandoVoz.Deshaz, "Deshaz", "Undo", new[] { "deshaz", "deshacer", "deshace" }, new[] { "undo" }),
        C(ComandoVoz.Rehaz, "Rehaz", "Redo", new[] { "rehaz", "rehacer", "rehace" }, new[] { "redo" }),
        C(ComandoVoz.GiraIzquierda, "Gira izquierda", "Turn left",
          new[] { "gira izquierda", "gira a la izquierda", "girar izquierda", "girar a la izquierda", "gira izq", "izquierda" },
          new[] { "turn left", "rotate left", "left" }),
        C(ComandoVoz.GiraDerecha, "Gira derecha", "Turn right",
          new[] { "gira derecha", "gira a la derecha", "girar derecha", "girar a la derecha", "gira der", "derecha" },
          new[] { "turn right", "rotate right", "right" }),
        C(ComandoVoz.GiraAtras, "Gira atrás", "Turn around",
          new[] { "gira atrás", "girar atrás", "da la vuelta", "media vuelta" }, new[] { "turn around", "around" }),
        C(ComandoVoz.Capas, "Capas", "Layers", new[] { "capas", "las capas" }, new[] { "layers" }),
        C(ComandoVoz.CapaNueva, "Capa nueva", "New layer", new[] { "capa nueva", "nueva capa", "otra capa" }, new[] { "new layer", "add layer" }),
        C(ComandoVoz.Mesa, "Mesa", "Desk", new[] { "mesa", "la mesa", "línea de tiempo" }, new[] { "desk", "timeline" }),
        C(ComandoVoz.Play, "Play", "Play", new[] { "play", "reproduce", "reproducir", "dale" }, new[] { "play" }),
        C(ComandoVoz.Pausa, "Pausa", "Pause", new[] { "pausa", "pausar" }, new[] { "pause" }),
        C(ComandoVoz.Siguiente, "Siguiente", "Next", new[] { "siguiente", "adelante" }, new[] { "next", "forward" }),
        C(ComandoVoz.Anterior, "Anterior", "Back", new[] { "anterior" }, new[] { "back", "previous" }),
        C(ComandoVoz.Clave, "Clave", "Key", new[] { "clave", "pon clave", "nueva clave" }, new[] { "key", "keyframe", "add key" }),
        C(ComandoVoz.QuitaClave, "Quita clave", "Remove key", new[] { "quita clave", "quitar clave", "borra clave", "borrar clave" },
          new[] { "remove key", "delete key" }),
        C(ComandoVoz.Paleta, "Paleta", "Palette", new[] { "paleta", "colores", "color" }, new[] { "palette", "colors", "color" }),
        C(ComandoVoz.Guarda, "Guardar", "Save", new[] { "guarda", "guardar" }, new[] { "save" }),
        C(ComandoVoz.VistaLimpia, "Vista limpia", "Clean view", new[] { "vista limpia", "limpia" }, new[] { "clean view", "clean" }),
        C(ComandoVoz.Silencio, "Silencio", "Mute", new[] { "silencio", "música", "sonido" }, new[] { "mute", "music", "sound" }),
        C(ComandoVoz.Halo, "Halo", "Halo", new[] { "halo" }, new[] { "halo" }),
        // v42: los botones que no tenían voz.
        C(ComandoVoz.Plano, "Plano", "Plane", new[] { "plano" }, new[] { "plane" }),
        C(ComandoVoz.Fondo, "Fondo", "Background", new[] { "fondo" }, new[] { "background" }),
        C(ComandoVoz.Archivos, "Archivos", "Files", new[] { "archivos", "mis archivos" }, new[] { "files", "my files" }),
        C(ComandoVoz.Esfera, "Esfera", "Sphere", new[] { "esfera" }, new[] { "sphere", "ball" }),
        C(ComandoVoz.Cubo, "Cubo", "Cube", new[] { "cubo" }, new[] { "cube", "box" }),
        C(ComandoVoz.Cilindro, "Cilindro", "Cylinder", new[] { "cilindro" }, new[] { "cylinder" }),
        C(ComandoVoz.ALineas, "A líneas", "To lines", new[] { "a líneas", "en líneas", "convertir a líneas" }, new[] { "to lines", "into lines" }),
        C(ComandoVoz.Ayuda, "Ayuda", "Help", new[] { "ayuda", "tutorial" }, new[] { "help", "tutorial" }),
        C(ComandoVoz.Inicio, "Inicio", "Start", new[] { "inicio", "al inicio", "al principio" }, new[] { "start", "beginning" }),
        C(ComandoVoz.Zoom, "Zoom", "Zoom", new[] { "zoom" }, new[] { "zoom" }),
        C(ComandoVoz.Animar, "Animar", "Animate", new[] { "animar", "anima" }, new[] { "animate" }),
        C(ComandoVoz.Bocas, "Bocas", "Mouths", new[] { "bocas", "boca" }, new[] { "mouths", "mouth" }),
        C(ComandoVoz.Titere, "Títere", "Puppet", new[] { "títere", "títeres" }, new[] { "puppet" }),
        C(ComandoVoz.Video, "Exportar", "Export", new[] { "exportar", "exporta", "video", "haz video", "haz un video" },
          new[] { "export", "video", "make video" }),
        C(ComandoVoz.Muestra, "Muestra", "Show", new[] { "muestra", "mostrar", "muestra capa", "ver capa" }, new[] { "show", "show layer" }),
        C(ComandoVoz.Oculta, "Oculta", "Hide", new[] { "oculta", "ocultar", "oculta capa", "esconde" }, new[] { "hide", "hide layer" }),
        C(ComandoVoz.Lapiz, "Lápiz", "Pencil", new[] { "lápiz" }, new[] { "pencil" }),
        C(ComandoVoz.Liberar, "Liberar", "Release", new[] { "liberar", "libera" }, new[] { "release" }),
        // Los fondos vivos.
        C(ComandoVoz.Avanza, "Avanzar", "Fly", new[] { "avanza", "avanzar", "vuela", "volar", "viaja" }, new[] { "fly", "travel" }),
        C(ComandoVoz.Universo, "Universo", "Universe", new[] { "universo", "espacio", "estrellas" }, new[] { "universe", "space", "stars" }),
        C(ComandoVoz.Nubes, "Nubes", "Clouds", new[] { "nubes", "cielo" }, new[] { "clouds", "sky" }),
        // Lo que no está en ningún botón: los colores, el grosor y el idioma.
        C(ComandoVoz.Color, "Negro", "Black", new[] { "negro" }, new[] { "black" }, 0),
        C(ComandoVoz.Color, "Blanco", "White", new[] { "blanco" }, new[] { "white" }, 1),
        C(ComandoVoz.Color, "Gris", "Gray", new[] { "gris" }, new[] { "gray", "grey" }, 2),
        C(ComandoVoz.Color, "Amarillo", "Yellow", new[] { "amarillo" }, new[] { "yellow" }, 3),
        C(ComandoVoz.Color, "Naranja", "Orange", new[] { "naranja" }, new[] { "orange" }, 4),
        C(ComandoVoz.Color, "Rojo", "Red", new[] { "rojo" }, new[] { "red" }, 5),
        C(ComandoVoz.Color, "Rosado", "Pink", new[] { "rosado", "rosa" }, new[] { "pink" }, 6),
        C(ComandoVoz.Color, "Morado", "Purple", new[] { "morado", "violeta" }, new[] { "purple", "violet" }, 7),
        C(ComandoVoz.Color, "Azul", "Blue", new[] { "azul" }, new[] { "blue" }, 8),
        C(ComandoVoz.Color, "Verde", "Green", new[] { "verde" }, new[] { "green" }, 9),
        C(ComandoVoz.Color, "Café", "Brown", new[] { "café", "marrón" }, new[] { "brown" }, 10),
        C(ComandoVoz.Color, "Piel", "Skin", new[] { "piel" }, new[] { "skin" }, 11),
        C(ComandoVoz.Grueso, "Más grueso", "Thicker", new[] { "más grueso", "más gruesa", "grueso", "gruesa" }, new[] { "thicker", "thick" }),
        C(ComandoVoz.Fino, "Más fino", "Thinner", new[] { "más fino", "más fina", "fino", "fina", "delgado" }, new[] { "thinner", "thin" }),
        C(ComandoVoz.Espanol, "Español", "Spanish", Nada0, new[] { "spanish", "español", "espanol" }),
        // v43: 2D / 3D en la capa, tinta, el siguiente fondo y cada fondo por su nombre, el instrumento.
        C(ComandoVoz.En2D, "2D", "2D", new[] { "dos de", "dos d", "en dos de", "plano dos de" }, new[] { "two d", "two dee", "flat" }),
        C(ComandoVoz.En3D, "3D", "3D", new[] { "tres de", "tres d", "en tres de", "libre" }, new[] { "three d", "three dee", "free" }),
        C(ComandoVoz.Tinta, "Tinta", "Ink", new[] { "tinta" }, new[] { "ink" }),
        C(ComandoVoz.Camara, "Cámara", "Camera", new[] { "cámara", "la cámara" }, new[] { "camera" }),
        C(ComandoVoz.SugiereCamara, "Sugiere", "Suggest", new[] { "sugiere", "sugiere cámara", "encuadre", "encuadra" },
          new[] { "suggest", "suggest camera", "frame it" }),
        C(ComandoVoz.ClaveCamara, "Clave de cámara", "Camera key", new[] { "clave de cámara", "clave cámara" }, new[] { "camera key" }),
        C(ComandoVoz.Cebolla, "Cebolla", "Onion skin", new[] { "cebolla", "papel cebolla" }, new[] { "onion", "onion skin" }),
        C(ComandoVoz.CuadroACuadro, "Cuadro a cuadro", "Frame by frame", new[] { "cuadro a cuadro", "cuadro por cuadro" },
          new[] { "frame by frame" }),
        C(ComandoVoz.SiguienteFondo, "Siguiente fondo", "Next background",
          new[] { "siguiente fondo", "otro fondo", "cambia fondo", "cambia el fondo" }, new[] { "next background", "change background" }),
        C(ComandoVoz.FondoNombre, "Roma", "Rome", new[] { "roma" }, new[] { "rome" }, 0),
        C(ComandoVoz.FondoNombre, "Shanghai", "Shanghai", new[] { "shanghai", "shangai" }, new[] { "shanghai" }, 1),
        C(ComandoVoz.FondoNombre, "Venecia", "Venice", new[] { "venecia", "canal" }, new[] { "venice", "canal" }, 2),
        C(ComandoVoz.FondoNombre, "Callejón", "Alley", new[] { "callejón" }, new[] { "alley" }, 3),
        C(ComandoVoz.FondoNombre, "Amanecer", "Sunrise", new[] { "amanecer" }, new[] { "sunrise" }, 4),
        C(ComandoVoz.FondoNombre, "Atardecer", "Sunset", new[] { "atardecer", "el mar" }, new[] { "sunset", "sea" }, 5),
        C(ComandoVoz.FondoNombre, "Mirador", "Lookout", new[] { "mirador" }, new[] { "lookout" }, 6),
        C(ComandoVoz.FondoNombre, "Parque", "Park", new[] { "parque" }, new[] { "park" }, 7),
        C(ComandoVoz.FondoNombre, "Flores", "Flowers", new[] { "flores", "camino" }, new[] { "flowers" }, 8),
        C(ComandoVoz.FondoNombre, "Campo", "Field", new[] { "campo", "campo seco" }, new[] { "field" }, 9),
        C(ComandoVoz.FondoNombre, "Golf", "Golf", new[] { "golf" }, new[] { "golf" }, 10),
        C(ComandoVoz.FondoNombre, "Nueva York", "New York", new[] { "nueva york", "azotea" }, new[] { "new york", "rooftop" }, 11),
        C(ComandoVoz.FondoNombre, "Sala de lujo", "Living room", new[] { "sala", "sala de lujo" }, new[] { "living room", "luxury" }, 12),
        C(ComandoVoz.FondoNombre, "Cuadrícula", "Grid", new[] { "cuadrícula" }, new[] { "grid" }, 13),
        C(ComandoVoz.FondoNombre, "Realidad", "Reality", new[] { "realidad", "mi cuarto" }, new[] { "reality", "passthrough" }, 14),
        C(ComandoVoz.FondoNombre, "Desierto", "Desert", new[] { "desierto", "dunas" }, new[] { "desert", "dunes" }, 15),
        C(ComandoVoz.Instrumento, "Piano", "Piano", new[] { "piano" }, new[] { "piano" }, Sonidos.Piano),
        C(ComandoVoz.Instrumento, "Arpa", "Harp", new[] { "arpa" }, new[] { "harp" }, Sonidos.Arpa),
        C(ComandoVoz.Instrumento, "Orquesta", "Orchestra", new[] { "orquesta" }, new[] { "orchestra" }, Sonidos.Orquesta),
        C(ComandoVoz.Instrumento, "Cajita", "Music box", new[] { "cajita", "cajita de música" }, new[] { "music box" }, Sonidos.Cajita),
        C(ComandoVoz.Instrumento, "Japón", "Japan", new[] { "japón", "japonés" }, new[] { "japan" }, Sonidos.Japan),
        C(ComandoVoz.Instrumento, "Post-punk", "Post-punk", new[] { "post punk", "punk" }, new[] { "post punk", "punk" }, Sonidos.PostPunk),
        C(ComandoVoz.Instrumento, "Drum and bass", "Drum and bass", new[] { "drum and bass", "drum", "batería" },
          new[] { "drum and bass", "drums" }, Sonidos.DrumAndBass),
        C(ComandoVoz.Instrumento, "Dubstep", "Dubstep", new[] { "dubstep" }, new[] { "dubstep" }, Sonidos.Dubstep),
        C(ComandoVoz.Instrumento, "Marimba", "Marimba", new[] { "marimba" }, new[] { "marimba" }, Sonidos.Marimba),
        C(ComandoVoz.Ingles, "Inglés", "English", new[] { "inglés", "english", "en inglés" }, Nada0),
    });

    // v44: lo que se arma solo: "línea roja" / "relleno rojo" (los 12 colores), "capa uno" ... "capa doce", y la mirada.
    // (Las listas van DENTRO: "Comandos" se arma antes que cualquier campo escrito más abajo en el archivo.)
    static FrasesComando[] ConExtras(FrasesComando[] base0)
    {
        string[] ColorMasc = { "negro", "blanco", "gris", "amarillo", "naranja", "rojo", "rosado", "morado", "azul", "verde", "café", "piel" };
        string[] ColorFem = { "negra", "blanca", "gris", "amarilla", "naranja", "roja", "rosada", "morada", "azul", "verde", "café", "piel" };
        string[] ColorIngles = { "black", "white", "gray", "yellow", "orange", "red", "pink", "purple", "blue", "green", "brown", "skin" };
        string[] NumerosEs = { "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve", "diez", "once", "doce" };
        string[] NumerosEn = { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve" };
        var l = new List<FrasesComando>(base0);
        l.Add(C(ComandoVoz.Hola, "Hola", "Hello", new[] { "hola", "hola cómo estás", "cómo estás" }, new[] { "hello", "hi", "hello how are you", "how are you" }));
        l.Add(C(ComandoVoz.Mirada, "Mirada", "Gaze", new[] { "mirada", "la mirada", "ojo" }, new[] { "gaze", "eye" }));
        // v45: "copiar" / "pegar" = las LÍNEAS (con sus rellenos); "copiar color" / "pegar color" = el color.
        l.Add(C(ComandoVoz.Copiar, "Copiar", "Copy", new[] { "copiar", "copia", "cópialo", "cópiala" }, new[] { "copy", "copy it", "copy this" }));
        l.Add(C(ComandoVoz.Pegar, "Pegar", "Paste", new[] { "pegar", "pega", "pégalo", "pégala" }, new[] { "paste", "paste it" }));
        l.Add(C(ComandoVoz.CopiarColor, "Copiar color", "Copy color", new[] { "copiar color", "copia color", "copia el color" },
                new[] { "copy color", "copy the color" }));
        l.Add(C(ComandoVoz.PegarColor, "Pegar color", "Paste color", new[] { "pegar color", "pega color", "pega el color" },
                new[] { "paste color", "paste the color" }));
        l.Add(C(ComandoVoz.Esquina, "Esquina", "Corner",
                new[] { "esquina", "esquinas", "alas", "alitas", "manijas", "handles", "antenas", "palancas", "tiradores", "alt" },
                new[] { "corner", "corners", "wings", "handles", "alt" }));
        l.Add(C(ComandoVoz.Suave, "Suave", "Smooth", new[] { "suave", "lisa", "alisa" }, new[] { "smooth" }));
        l.Add(C(ComandoVoz.BorraEsto, "Borra esto", "Delete this", new[] { "borra esto", "bórralo", "borrar esto" },
                new[] { "delete this", "erase this", "delete it" }));
        l.Add(C(ComandoVoz.OcultaEsto, "Oculta esto", "Hide this", new[] { "oculta esto", "ocúltalo", "esconde esto", "escóndelo" },
                new[] { "hide this", "hide it" }));
        l.Add(C(ComandoVoz.EstaCapa, "Esta capa", "This layer", new[] { "esta capa", "a esta capa" }, new[] { "this layer" }));
        l.Add(C(ComandoVoz.Selecciona, "Selecciona", "Select", new[] { "selecciona", "selecciónalo", "elige esto" },
                new[] { "select", "select this", "select it" }));
        for (int i = 0; i < ColorMasc.Length; i++)
        {
            string fem = ColorFem[i], masc = ColorMasc[i], en = ColorIngles[i];
            var esLinea = fem == masc ? new[] { "línea " + fem, "contorno " + masc } : new[] { "línea " + fem, "línea " + masc, "contorno " + masc };
            l.Add(C(ComandoVoz.ColorLinea, "Línea " + fem, en + " line", esLinea, new[] { en + " line", "line " + en }, i));
            l.Add(C(ComandoVoz.ColorRelleno, "Relleno " + masc, en + " fill", new[] { "relleno " + masc }, new[] { en + " fill", "fill " + en }, i));
        }
        for (int i = 0; i < NumerosEs.Length; i++)
            l.Add(C(ComandoVoz.IrACapa, "Capa " + (i + 1), "Layer " + (i + 1),
                    new[] { "capa " + NumerosEs[i], "layer " + NumerosEs[i] }, new[] { "layer " + NumerosEn[i] }, i));
        return l.ToArray();
    }

    static FrasesComando C(ComandoVoz c, string nombreEs, string nombreEn, string[] es, string[] en, int dato = 0)
    {
        return new FrasesComando { comando = c, dato = dato, nombreEs = nombreEs, nombreEn = nombreEn, es = es, en = en };
    }

    // v42: la palabra de un comando, como se muestra en la etiqueta del botón (en el idioma de la app). "" si no hay.
    public static string NombreComando(ComandoVoz c)
    {
        foreach (var f in Comandos)
            if (f.comando == c)
                return Idioma.Ingles ? f.nombreEn : f.nombreEs;
        return "";
    }

    // v42: ¿este nombre (en español, como en la paleta: "Rojo", "Halo") también se puede decir? Devuelve la palabra
    // en el idioma de la app.
    public static bool EsPalabraDeVoz(string nombreEs, out string palabra)
    {
        palabra = null;
        if (string.IsNullOrEmpty(nombreEs))
            return false;
        foreach (var f in Comandos)
            if (string.Equals(f.nombreEs, nombreEs, System.StringComparison.OrdinalIgnoreCase))
            {
                palabra = Idioma.Ingles ? f.nombreEn : f.nombreEs;
                return true;
            }
        return false;
    }

    Gesto gestoVoz = Gesto.Ninguno;   // el modo puesto con la voz (hasta decir "ya")

    // Cuánto lleva el gesto de ahora (la línea de tiempo pide que ya estuviera hecho antes de tocar la barra).
    public float SegundosEnGesto => Time.time - gestoDesde;

    // Lo último que se entendió (para el panel de la izquierda).
    public static string UltimoComandoVoz { get; private set; } = "";
    public static float UltimoComandoHora { get; private set; } = -10f;

    // Todas las frases que se escuchan (para Vosk).
    public static List<string> FrasesDeVoz(bool ingles)
    {
        var lista = new List<string>();
        foreach (var c in Comandos)
            foreach (var f in ingles ? c.en : c.es)
                if (!lista.Contains(f))
                    lista.Add(f);
        return lista;
    }

    static string Normalizar(string s)
    {
        s = (s ?? "").ToLowerInvariant().Trim();
        s = s.Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u').Replace('ü', 'u');
        while (s.Contains("  "))
            s = s.Replace("  ", " ");
        return s;
    }

    // Lo que entendió la voz: se busca la frase más larga que esté dicha (con palabras completas).
    public void ComandoDeVoz(string texto)
    {
        string n = " " + Normalizar(texto) + " ";
        bool ingles = Idioma.Ingles;
        int mejor = -1, largo = 0;
        for (int i = 0; i < Comandos.Length; i++)
            foreach (var f in ingles ? Comandos[i].en : Comandos[i].es)
            {
                string p = Normalizar(f);
                if (p.Length > largo && n.Contains(" " + p + " "))
                {
                    largo = p.Length;
                    mejor = i;
                }
            }
        if (mejor < 0)
            return;
        var c = Comandos[mejor];
        // v42: durante el tutorial solo valen los de la mano izquierda (y el silencio): nada que lo desordene.
        if (Tutorial.EnCurso && c.comando != ComandoVoz.Dibuja && c.comando != ComandoVoz.Borra && c.comando != ComandoVoz.Nodos
            && c.comando != ComandoVoz.Ya && c.comando != ComandoVoz.Deshaz && c.comando != ComandoVoz.Silencio
            && c.comando != ComandoVoz.Hola)
            return;
        UltimoComandoVoz = ingles ? c.nombreEn : c.nombreEs;
        UltimoComandoHora = Time.time;
        if (Cabeza != null)
            Burbuja(Cabeza.position + Cabeza.forward * 0.4f - Cabeza.right * 0.15f, 1.05f);
        HacerComando(c.comando, c.dato);
    }

    void HacerComando(ComandoVoz c, int dato)
    {
        var anim = dibujo != null ? dibujo.animacion : null;
        switch (c)
        {
            case ComandoVoz.Dibuja: PonerGestoVoz(Gesto.Dibujar); break;
            case ComandoVoz.Borra: PonerGestoVoz(Gesto.Borrar); break;
            case ComandoVoz.Nodos: PonerGestoVoz(Gesto.Nodos); break;
            case ComandoVoz.Recta: PonerGestoVoz(Gesto.Recta); break;
            case ComandoVoz.Ya: QuitarGestoVoz(); break;
            case ComandoVoz.Deshaz:
                QuitarGestoVoz();
                dibujo.Deshacer();
                break;
            case ComandoVoz.Rehaz:
                QuitarGestoVoz();
                dibujo.Rehacer();
                break;
            case ComandoVoz.GiraIzquierda: GirarFondoBoton(-1); break;
            case ComandoVoz.GiraDerecha: GirarFondoBoton(1); break;
            case ComandoVoz.GiraAtras:
                if (escenarioGiro == null)
                    escenarioGiro = FindFirstObjectByType<Escenario>();
                if (escenarioGiro == null || !escenarioGiro.Girar(180f))
                    dibujo.Mensaje("Girar es para los fondos 360");
                break;
            case ComandoVoz.Capas: CapasProfundidad.Alternar(); break;
            case ComandoVoz.CapaNueva: CapasProfundidad.CapaNueva(); break;
            case ComandoVoz.Mesa:
                if (panelArriba != null)
                    panelArriba.AlternarMesa();
                break;
            case ComandoVoz.Play:
                if (anim != null && !anim.Reproduciendo)
                    anim.AlternarReproducir();
                break;
            case ComandoVoz.Pausa:
                if (anim != null)
                    anim.Pausar();
                break;
            case ComandoVoz.Siguiente:
                if (anim != null)
                    anim.Siguiente();
                break;
            case ComandoVoz.Anterior:
                if (anim != null)
                    anim.Anterior();
                break;
            case ComandoVoz.Clave:
                if (anim != null)
                    anim.AgregarClave();
                break;
            case ComandoVoz.QuitaClave:
                if (anim != null)
                    anim.QuitarClave();
                break;
            case ComandoVoz.Paleta: AlternarPaleta(); break;
            case ComandoVoz.Guarda: dibujo.Guardar(); break;
            case ComandoVoz.VistaLimpia: VistaLimpia.Alternar(); break;
            case ComandoVoz.Silencio:
                BotonSonido.Alternar();
                dibujo.Mensaje(BotonSonido.Silenciado ? "Silencio" : "Con sonido");
                break;
            case ComandoVoz.Halo: dibujo.AlternarHalo(); break;
            // v42
            case ComandoVoz.Plano: dibujo.AlternarPlano(); break;
            case ComandoVoz.Fondo:
            {
                var reloj = FindFirstObjectByType<PanelMuneca>();
                if (reloj != null)
                    reloj.SiguienteFondo();
                break;
            }
            case ComandoVoz.Archivos:
            {
                var nav = FindFirstObjectByType<NavegadorArchivos>();
                if (nav != null)
                    nav.Abrir(Cabeza);
                break;
            }
            case ComandoVoz.Esfera: if (figuras != null) figuras.Agregar(0); break;
            case ComandoVoz.Cubo: if (figuras != null) figuras.Agregar(1); break;
            case ComandoVoz.Cilindro: if (figuras != null) figuras.Agregar(2); break;
            case ComandoVoz.ALineas: if (figuras != null) figuras.ConvertirSeleccionada(); break;
            case ComandoVoz.Ayuda:
                if (Tutorial.Instancia != null)
                    Tutorial.Instancia.AbrirSecciones();
                break;
            case ComandoVoz.Inicio: if (anim != null) anim.Inicio(); break;
            case ComandoVoz.Zoom: if (panelArriba != null) panelArriba.ComandoZoom(); break;
            case ComandoVoz.Animar: if (panelArriba != null) panelArriba.ComandoPestana(0); break;
            case ComandoVoz.Bocas: if (panelArriba != null) panelArriba.ComandoPestana(2); break;
            case ComandoVoz.Titere: if (panelArriba != null) panelArriba.ComandoPestana(3); break;
            case ComandoVoz.Video:
            {
                var exportador = FindFirstObjectByType<ExportadorVideo>();
                if (exportador != null)
                    exportador.ExportarAnimacion();
                break;
            }
            case ComandoVoz.Muestra: VerCapaVoz(true); break;
            case ComandoVoz.Oculta: VerCapaVoz(false); break;
            case ComandoVoz.Lapiz:
                // v43: lápiz Y 2D de una vez, en la capa en la que dibujas.
                if (dibujo.temblor != null)
                    dibujo.temblor.PonerBoceto(true);
                dibujo.PonerPlano(true);
                dibujo.Mensaje("Lápiz en 2D: " + dibujo.NombreCapa(dibujo.capaActual));
                break;
            case ComandoVoz.Tinta:
                if (dibujo.temblor != null)
                    dibujo.temblor.PonerBoceto(false);
                break;
            case ComandoVoz.Camara: CamaraEscena.Alternar(); break;
            case ComandoVoz.SugiereCamara: CamaraEscena.SugerirVoz(); break;
            case ComandoVoz.ClaveCamara: CamaraEscena.AgregarClave(); break;
            case ComandoVoz.Cebolla: PapelCebolla.Alternar(); break;
            case ComandoVoz.CuadroACuadro: dibujo.AlternarCuadroACuadro(); break;
            case ComandoVoz.En2D: dibujo.PonerPlano(true); break;
            case ComandoVoz.En3D: dibujo.PonerPlano(false); break;
            case ComandoVoz.SiguienteFondo: SiguienteFondoVoz(); break;
            case ComandoVoz.FondoNombre: FondoPorNombreVoz(dato); break;
            case ComandoVoz.Instrumento:
                if (dato >= 0 && dato < Sonidos.Instrumentos.Length)
                {
                    ElegirInstrumento(dato);
                    if (!MusicaEncendida)
                        BotonSonido.Alternar();
                    dibujo.Mensaje("Instrumento: " + Sonidos.Instrumentos[dato]);
                }
                break;
            case ComandoVoz.Liberar: dibujo.LiberarCapa(dibujo.capaActual); break;
            case ComandoVoz.Avanza: AvanzarVoz(); break;
            case ComandoVoz.Universo: FondoVivoVoz(FondosVivos.Universo); break;
            case ComandoVoz.Nubes: FondoVivoVoz(FondosVivos.Nubes); break;
            case ComandoVoz.Color:
                // v44: con la mirada señalando algo, el color va a eso (el relleno o la línea que brilla).
                if (dato >= 0 && dato < ColoresPaleta.Length && !ColorAMirada(ColoresPaleta[dato], 0))
                    ElegirColor(ColoresPaleta[dato], Cabeza != null ? Cabeza.position + Cabeza.forward * 0.4f : transform.position);
                break;
            case ComandoVoz.ColorLinea:
                if (dato >= 0 && dato < ColoresPaleta.Length && !ColorAMirada(ColoresPaleta[dato], 1))
                {
                    dibujo.ColorNuevo = ColoresPaleta[dato];
                    dibujo.Mensaje("Líneas nuevas: " + NombreComando(ComandoVoz.ColorLinea, dato));
                }
                break;
            case ComandoVoz.ColorRelleno:
                if (dato >= 0 && dato < ColoresPaleta.Length && !ColorAMirada(ColoresPaleta[dato], 2))
                {
                    PonerColorRellenoNuevo(ColoresPaleta[dato]);
                    dibujo.Mensaje("Rellenos nuevos: " + NombreComando(ComandoVoz.ColorRelleno, dato));
                }
                break;
            case ComandoVoz.Mirada: Mirada.Alternar(); break;
            case ComandoVoz.Hola:
                // v44: en el tutorial, la cabeza dice "hola"; si no, la boca de tus bocas dice "hola".
                // v46: lo repite CON TU VOZ (la frase entera que dijiste). Mientras habla no se escucha a sí misma.
                if (Time.time < Lipsync.HablandoHasta)
                    break;
                if (Tutorial.Instancia != null && Tutorial.Instancia.EsperaHola)
                    Tutorial.Instancia.DecirHolaConVoz();
                else
                {
                    var lip = FindFirstObjectByType<Lipsync>();
                    if (lip != null)
                        lip.RepetirMiVoz(Idioma.Ingles ? "hello, how are you?" : "hola, ¿cómo estás?", null);
                }
                break;
            case ComandoVoz.Copiar: CopiarLineasVoz(); break;
            case ComandoVoz.Pegar: PegarLineasVoz(); break;
            case ComandoVoz.CopiarColor: CopiarColorVoz(); break;
            case ComandoVoz.PegarColor: PegarColorVoz(); break;
            case ComandoVoz.Esquina: EsquinaVoz(true); break;
            case ComandoVoz.Suave: EsquinaVoz(false); break;
            case ComandoVoz.BorraEsto: BorraEstoVoz(); break;
            case ComandoVoz.OcultaEsto:
            {
                var t = ObjetivoMirada();
                if (t != null && dibujo.capas[t.capa].visible)
                    dibujo.AlternarVerCapa(t.capa);
                break;
            }
            case ComandoVoz.EstaCapa:
            {
                var t = ObjetivoMirada();
                if (t != null)
                    dibujo.SeleccionarCapa(t.capa);
                break;
            }
            case ComandoVoz.Selecciona:
            {
                var t = ObjetivoMirada();
                if (t != null)
                {
                    dibujo.Seleccionar(t);
                    dibujo.Mensaje("Seleccionada");
                }
                break;
            }
            case ComandoVoz.IrACapa:
                if (dato >= 0 && dato < dibujo.capas.Count)
                    dibujo.SeleccionarCapa(dato);
                else
                    dibujo.Mensaje("Esa capa no existe (hay " + dibujo.capas.Count + ")");
                break;
            case ComandoVoz.Grueso: GrosorVoz(1.4f); break;
            case ComandoVoz.Fino: GrosorVoz(1f / 1.4f); break;
            case ComandoVoz.Espanol: IdiomaVoz(false); break;
            case ComandoVoz.Ingles: IdiomaVoz(true); break;
        }
    }

    // ---------- v44: la mirada ----------

    // v44: el nombre que se muestra de un comando con dato ("Línea roja", "Capa 3").
    static string NombreComando(ComandoVoz c, int dato)
    {
        foreach (var f in Comandos)
            if (f.comando == c && f.dato == dato)
                return Idioma.Ingles ? f.nombreEn : f.nombreEs;
        return "";
    }

    // Lo que señala la mirada (o un aviso si no hay nada).
    Trazo ObjetivoMirada()
    {
        if (!Mirada.Activa)
        {
            dibujo.Mensaje("Prende la mirada (el ojo, debajo del micrófono) y mira algo");
            return null;
        }
        if (Mirada.Objetivo == null)
        {
            dibujo.Mensaje("Mira una línea o un relleno (que brille) y vuelve a decirlo");
            return null;
        }
        return Mirada.Objetivo;
    }

    // Un color a lo que señala la mirada. cual: 0 = lo que brilla (relleno o línea), 1 = la línea, 2 = el relleno.
    // false si la mirada no señala nada (entonces el color es para lo que dibujes después).
    bool ColorAMirada(Color c, int cual)
    {
        if (!Mirada.Activa || Mirada.Objetivo == null)
            return false;
        var t = Mirada.Objetivo;
        bool relleno = cual == 2 || (cual == 0 && Mirada.EsRelleno);
        if (relleno)
        {
            dibujo.RellenarConColor(t, c);
            dibujo.Mensaje("Relleno pintado");
        }
        else
        {
            dibujo.GuardarParaDeshacer();
            dibujo.PonerColor(t, c);
            dibujo.Mensaje("Línea pintada");
        }
        if (Cabeza != null)
            Burbuja(Cabeza.position + Cabeza.forward * 0.5f, 1f);
        return true;
    }

    // "Copiar": el color de lo que miras (sin mirada: el de tus líneas). Se carga también en el dedo (la jeringa).
    void CopiarColorVoz()
    {
        Color c = dibujo.ColorNuevo;
        if (Mirada.Activa && Mirada.Objetivo != null)
        {
            var t = Mirada.Objetivo;
            c = Mirada.EsRelleno && t.colorFondo.a > 0.01f ? t.colorFondo : t.color;
        }
        c.a = 1f;
        CargarJeringa(c);
        dibujo.Mensaje("Color copiado: " + NombreColor(c) + " · mira otra cosa y di \"pegar color\"");
    }

    // "Pegar": ese color a lo que miras (sin mirada: a tus líneas nuevas).
    void PegarColorVoz()
    {
        if (!HayJeringa)
        {
            dibujo.Mensaje("Primero di \"copiar color\" (o toca un color de la paleta)");
            return;
        }
        if (!ColorAMirada(ColorJeringa, 0))
        {
            dibujo.ColorNuevo = ColorJeringa;
            dibujo.Mensaje("Líneas nuevas: " + NombreColor(ColorJeringa));
        }
    }

    // v45: "copiar" / "pegar" LÍNEAS: lo que está seleccionado (o lo que señala la mirada), con sus rellenos.
    readonly List<DatosTrazo> portapapeles = new List<DatosTrazo>();

    void CopiarLineasVoz()
    {
        var lista = dibujo.Seleccionadas();
        if (lista.Count == 0 && Mirada.Activa && Mirada.Objetivo != null)
            lista.Add(Mirada.Objetivo);
        if (lista.Count == 0)
        {
            dibujo.Mensaje("Selecciona líneas (o míralas con la mirada) y di \"copiar\"");
            return;
        }
        portapapeles.Clear();
        foreach (var t in lista)
            portapapeles.Add(JsonUtility.FromJson<DatosTrazo>(JsonUtility.ToJson(t.CrearDatos())));
        dibujo.Mensaje(portapapeles.Count == 1 ? "Línea copiada · di \"pegar\"" : portapapeles.Count + " líneas copiadas · di \"pegar\"");
    }

    void PegarLineasVoz()
    {
        if (portapapeles.Count == 0)
        {
            dibujo.Mensaje("Primero di \"copiar\" (con líneas seleccionadas o con la mirada)");
            return;
        }
        if (dibujo.EsCamara(dibujo.capaActual))
        {
            dibujo.Mensaje("Estás en la capa de la cámara: elige otra capa para pegar");
            return;
        }
        // v49: sale JUSTO DONDE MIRAS (el centro de tu vista), a la misma distancia de ti que lo copiado (entre 35 cm
        // y 1.2 m). Voltea a la derecha, arriba o abajo y di "pegar": aparece allá. Con o sin la mirada prendida.
        var raiz = dibujo.transform;
        Vector3 centro = Vector3.zero;
        int n = 0;
        foreach (var d in portapapeles)
            foreach (var p in d.nodos)
            {
                centro += p;
                n++;
            }
        centro = n > 0 ? raiz.TransformPoint(centro / n) : raiz.position;
        Vector3 mundo = Vector3.right * 0.05f;
        if (Cabeza != null)
        {
            float distancia = Mathf.Clamp(Vector3.Distance(centro, Cabeza.position), 0.35f, 1.2f);
            mundo = Cabeza.position + Cabeza.forward * distancia - centro;
        }
        Vector3 local = raiz.InverseTransformVector(mundo);
        dibujo.GuardarParaDeshacer();
        Trazo ultima = null;
        foreach (var original in portapapeles)
        {
            var d = JsonUtility.FromJson<DatosTrazo>(JsonUtility.ToJson(original));
            d.id = 0;
            for (int i = 0; i < d.nodos.Count; i++)
                d.nodos[i] += local;
            if (d.telaBase != null)
                for (int i = 0; i < d.telaBase.Count; i++)
                    d.telaBase[i] += local;
            d.hojaPunto += local;
            ultima = dibujo.AgregarTrazo(d);
        }
        if (ultima != null)
            dibujo.Seleccionar(ultima);
        dibujo.Mensaje(portapapeles.Count == 1 ? "Línea pegada" : portapapeles.Count + " líneas pegadas");
    }

    void BorraEstoVoz()
    {
        var t = ObjetivoMirada();
        if (t == null)
            return;
        dibujo.GuardarParaDeshacer();
        if (Mirada.EsRelleno)
        {
            dibujo.QuitarRelleno(t);
            dibujo.Mensaje("Relleno quitado");
        }
        else
        {
            dibujo.BorrarTrazo(t, true);
        }
    }

    // "Esquina" (o alas, manijas, tiradores...): el nodo que miras (o el elegido en nodos) queda como esquina: cada
    // tirador se mueve por su lado. "Suave": vuelven a quedar alineados.
    readonly HashSet<long> esquinasVoz = new HashSet<long>();

    static long ClaveEsquina(Trazo t, int i) => (long)t.GetInstanceID() * 4096L + i;

    bool EsquinaPorVoz(Trazo t, int i) => t != null && esquinasVoz.Contains(ClaveEsquina(t, i));

    void EsquinaVoz(bool esquina)
    {
        Trazo t = null;
        int i = -1;
        if (Mirada.Activa && Mirada.NodoTrazo != null && Mirada.Nodo >= 0)
        {
            t = Mirada.NodoTrazo;
            i = Mirada.Nodo;
        }
        else if (SeleccionValida)
        {
            t = selTrazo;
            i = selIndice;
        }
        if (t == null || i < 0 || i >= t.nodos.Count)
        {
            dibujo.Mensaje("Mira un nodo (o tócalo en Nodos) y vuelve a decirlo");
            return;
        }
        if (esquina)
        {
            esquinasVoz.Add(ClaveEsquina(t, i));
            t.AsegurarAsas();
            t.asaManual[i] = true;
            dibujo.Mensaje("Esquina: mueve un tirador con el índice y solo se mueve ese lado");
        }
        else
        {
            esquinasVoz.Remove(ClaveEsquina(t, i));
            dibujo.GuardarParaDeshacer();
            t.Alisar(i);
            dibujo.Mensaje("Suave: los tiradores vuelven a quedar alineados");
        }
        selTrazo = t;
        selIndice = i;
    }

    // v42: "muestra" / "oculta": la capa en la que dibujas.
    void VerCapaVoz(bool ver)
    {
        int capa = dibujo.capaActual;
        if (capa < 0 || capa >= dibujo.capas.Count)
            return;
        if (dibujo.capas[capa].visible != ver)
            dibujo.AlternarVerCapa(capa);
    }

    // v43: "siguiente fondo": el que sigue de todos, de una vez (sin la ventanita de las fotos).
    void SiguienteFondoVoz()
    {
        if (escenarioGiro == null)
            escenarioGiro = FindFirstObjectByType<Escenario>();
        if (escenarioGiro == null)
            return;
        escenarioGiro.SiguienteDeTodos();
        SelectorFondos.RefrescarSiAbierto();
        dibujo.Mensaje(escenarioGiro.MensajeFondo);
    }

    // v43: "roma", "parque"...: ese fondo.
    void FondoPorNombreVoz(int cual)
    {
        if (escenarioGiro == null)
            escenarioGiro = FindFirstObjectByType<Escenario>();
        if (escenarioGiro == null || cual < 0 || cual >= FondosPorVoz.Length)
            return;
        if (!escenarioGiro.PonerPorNombre(FondosPorVoz[cual]))
        {
            dibujo.Mensaje("Ese fondo sale después de TrazoVR > ★ Armar escena");
            return;
        }
        SelectorFondos.RefrescarSiAbierto();
        dibujo.Mensaje(escenarioGiro.MensajeFondo);
    }

    // v42: "avanzar" / "volar" en los fondos vivos.
    void AvanzarVoz()
    {
        if (escenarioGiro == null)
            escenarioGiro = FindFirstObjectByType<Escenario>();
        if (escenarioGiro == null || escenarioGiro.VivoPuesto < 0)
        {
            dibujo.Mensaje("Avanzar es para los fondos vivos (Universo y Sobre las nubes)");
            return;
        }
        escenarioGiro.AlternarAvanzar();
        SelectorFondos.RefrescarSiAbierto();
        dibujo.Mensaje(FondosVivos.NombreAvanzar(escenarioGiro.VivoPuesto) + (escenarioGiro.Avanzando ? ": Sí" : ": No"));
    }

    // v42: "universo" / "nubes": ese fondo vivo.
    void FondoVivoVoz(int cual)
    {
        if (escenarioGiro == null)
            escenarioGiro = FindFirstObjectByType<Escenario>();
        if (escenarioGiro == null)
            return;
        if (!escenarioGiro.PonerVivo(cual))
        {
            dibujo.Mensaje("Los fondos vivos salen después de TrazoVR > ★ Armar escena");
            return;
        }
        SelectorFondos.RefrescarSiAbierto();
        dibujo.Mensaje(escenarioGiro.MensajeFondo);
    }

    // v42: "más grueso" / "más fino": las líneas nuevas.
    void GrosorVoz(float factor)
    {
        dibujo.ElegirAnchoPincel(dibujo.AnchoNuevoMundo * factor);
        dibujo.Mensaje("Líneas nuevas: " + Mathf.RoundToInt(dibujo.AnchoNuevoMundo * 1000f) + " mm");
    }

    // v42: en inglés di "spanish"; en español di "inglés". La voz se vuelve a preparar en el otro idioma (la primera
    // vez lo baja, con internet).
    void IdiomaVoz(bool ingles)
    {
        if (Idioma.Ingles == ingles)
            return;
        Idioma.Alternar();
        dibujo.Mensaje(ingles ? "English" : "Español");
    }

    void PonerGestoVoz(Gesto g)
    {
        gestoVoz = g;
        // Al entrar se vuelve a empezar (si ya dibujabas por voz, esa línea se termina y empieza otra).
        if (GestoIzq != Gesto.Ninguno && GestoIzq != Gesto.Transformar)
            SalirDeGesto();
    }

    void QuitarGestoVoz()
    {
        if (gestoVoz == Gesto.Ninguno)
            return;
        bool eraEse = GestoIzq == gestoVoz;
        gestoVoz = Gesto.Ninguno;
        if (eraEse)
            SalirDeGesto();
    }

    // ¿Qué conviene decir ahora? (el panel de la izquierda: "Sugerencia: di en voz alta").
    public string SugerenciaVoz()
    {
        bool en = Idioma.Ingles;
        if (gestoVoz != Gesto.Ninguno)
            return en ? "Stop" : "Ya";
        if (paletaAbierta)
            return en ? "Draw" : "Dibuja";
        if (CapasProfundidad.Abierta)
            return en ? "New layer" : "Capa nueva";
        if (panelArriba != null && panelArriba.MesaVisible)
        {
            var anim = dibujo != null ? dibujo.animacion : null;
            return anim != null && anim.Reproduciendo ? (en ? "Pause" : "Pausa") : "Play";
        }
        switch (GestoIzq)
        {
            case Gesto.Borrar: return en ? "Draw" : "Dibuja";
            case Gesto.Dibujar:
            case Gesto.Recta:
            case Gesto.Nodos: return en ? "Undo" : "Deshaz";
        }
        if (SegundosSinEditar < 4f)
            return en ? "Nodes" : "Nodos";
        return en ? "Draw" : "Dibuja";
    }
}

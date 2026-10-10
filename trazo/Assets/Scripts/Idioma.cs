using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// Idioma de la app: English (al empezar) o Español (botón en "Mis archivos"). Solo cambia los textos; nada más.
//  - T("texto en español") devuelve el texto en el idioma elegido.
//  - Las frases conocidas se traducen completas; las que llevan números o nombres se traducen por partes.
//  - Los textos fijos de la escena (botones, títulos) se registran al empezar y se cambian al tocar el botón.
public static class Idioma
{
    // JCartoons empieza en inglés ("English first"); el botón de Mis archivos lo cambia y se recuerda.
    const string Clave = "jcartoons_idioma";
    static bool cargado, ingles;
    static readonly Dictionary<TMP_Text, string> registrados = new Dictionary<TMP_Text, string>();
    static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
    static Dictionary<string, string> exactas;
    static List<KeyValuePair<string, string>> partes;

    public static event System.Action alCambiar;

    public static bool Ingles
    {
        get
        {
            Cargar();
            return ingles;
        }
    }

    static void Cargar()
    {
        if (cargado)
            return;
        cargado = true;
        ingles = PlayerPrefs.GetInt(Clave, 1) == 1;
    }

    public static void Alternar()
    {
        Cargar();
        ingles = !ingles;
        PlayerPrefs.SetInt(Clave, ingles ? 1 : 0);
        PlayerPrefs.Save();
        cache.Clear();
        // Los textos fijos vuelven a escribirse en el idioma nuevo.
        var muertos = new List<TMP_Text>();
        foreach (var par in registrados)
        {
            if (par.Key == null)
            {
                muertos.Add(par.Key);
                continue;
            }
            par.Key.text = T(par.Value);
        }
        foreach (var m in muertos)
            registrados.Remove(m);
        alCambiar?.Invoke();
    }

    // Escribe un texto (en español) en el idioma elegido y lo recuerda para cuando cambie el idioma.
    public static void Poner(TMP_Text t, string es)
    {
        if (t == null)
            return;
        registrados[t] = es ?? "";
        t.text = T(es);
    }

    // Una copia de un texto ya registrado (por ejemplo, el panel azul de ayuda).
    public static void Copiar(TMP_Text original, TMP_Text copia)
    {
        if (original == null || copia == null)
            return;
        string es;
        if (registrados.TryGetValue(original, out es))
            registrados[copia] = es;
    }

    // Al empezar: todos los textos de la escena (están en español) se registran y se traducen si hace falta.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegistrarEscena()
    {
        foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || registrados.ContainsKey(t))
                continue;
            registrados[t] = t.text;
            if (Ingles)
                t.text = T(t.text);
        }
    }

    public static string T(string es)
    {
        if (string.IsNullOrEmpty(es) || !Ingles)
            return es;
        string hecho;
        if (cache.TryGetValue(es, out hecho))
            return hecho;
        Armar();
        if (!exactas.TryGetValue(es, out hecho))
            hecho = PorPartes(es);
        if (cache.Count > 3000)
            cache.Clear();
        cache[es] = hecho;
        return hecho;
    }

    // Reemplaza cada frase conocida (las más largas primero), sin cortar palabras a la mitad.
    static string PorPartes(string texto)
    {
        var sb = new StringBuilder(texto);
        foreach (var par in partes)
        {
            string es = par.Key;
            int desde = 0;
            while (desde < sb.Length)
            {
                int i = sb.ToString().IndexOf(es, desde, System.StringComparison.Ordinal);
                if (i < 0)
                    break;
                bool inicioOk = i == 0 || !char.IsLetter(es[0]) || !char.IsLetter(sb[i - 1]);
                int fin = i + es.Length;
                bool finOk = fin >= sb.Length || !char.IsLetter(es[es.Length - 1]) || !char.IsLetter(sb[fin]);
                if (inicioOk && finOk)
                {
                    sb.Remove(i, es.Length);
                    sb.Insert(i, par.Value);
                    desde = i + par.Value.Length;
                }
                else
                {
                    desde = i + 1;
                }
            }
        }
        return sb.ToString();
    }

    static void Armar()
    {
        if (exactas != null)
            return;
        exactas = new Dictionary<string, string>();
        for (int i = 0; i + 1 < Tabla.Length; i += 2)
            exactas[Tabla[i]] = Tabla[i + 1];
        partes = new List<KeyValuePair<string, string>>(exactas);
        partes.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
    }

    // Español, English (en parejas).
    static readonly string[] Tabla =
    {
        // ----- Pestañas, panel y botones -----
        "Animar", "Animate",
        "Medios", "Media",
        "Bocas", "Mouths",
        "Títere", "Puppet",
        "Zoom: todo", "Zoom: all",
        "Zoom: ", "Zoom: ",
        "Fijar aquí", "Pin here",
        "Seguirme", "Follow me",
        "Inicio", "Start",
        "Play", "Play",
        "Pausa", "Pause",
        "+ Clave", "+ Key",
        "- Clave", "- Key",
        "Grabar", "Record",
        "Detener", "Stop",
        "Reanudar", "Resume",
        "Video proceso", "Process video",
        "Video anim", "Anim video",
        "Vel x", "Speed x",
        "Foto", "Photo",
        "Crear SVG", "Create SVG",
        "Ver SVG", "View SVG",
        "Imagen +", "Image +",
        "Imagen -", "Image -",
        "Imágenes: ver", "Images: show",
        "Imágenes: ocultas", "Images: hidden",
        "Boceto: No", "Sketch: No",
        "Boceto: Gris", "Sketch: Gray",
        "Boceto: Azul", "Sketch: Blue",
        "Boceto: ", "Sketch: ",
        "Plano: unido", "Plane: joined",
        "Plano: propio", "Plane: own",
        "Temblor: No", "Wobble: No",
        "Temblor: Suave", "Wobble: Soft",
        "Temblor: Medio", "Wobble: Medium",
        "Temblor: Fuerte", "Wobble: Strong",
        "Temblor: ", "Wobble: ",
        "Hebras: ", "Strands: ",
        "Grosor vivo", "Live width",
        "Ciclo de 3", "Cycle of 3",
        "Libre", "Free",
        "Suavidad: Suave", "Smoothness: Soft",
        "Suavidad: Normal", "Smoothness: Normal",
        "Suavidad: Nervioso", "Smoothness: Nervous",
        "Suavidad: ", "Smoothness: ",
        "Velocidad: ", "Speed: ",
        "Capa ", "Layer ",
        "Capa", "Layer",
        "Ver", "Show",
        "Oculta", "Hidden",
        "Modo: Guardar", "Mode: Save",
        "Modo: Probar", "Mode: Test",
        "Voz", "Voice",
        "Audio", "Audio",
        "Lipsync", "Lipsync",
        "Quitar audio", "Remove audio",
        "Reposo", "Rest",
        "Tipo: ", "Type: ",
        "Crear ", "Create ",
        "Parar", "Stop",
        "Posar dedos", "Pose fingers",
        "Terminar", "Finish",
        "Ciclo: ", "Cycle: ",
        "Guardar ciclo", "Save cycle",
        "Voltear", "Flip",
        "Piso +/-", "Floor +/-",
        "Pierna 1", "Leg 1",
        "Pierna 2", "Leg 2",
        "Brazo 1", "Arm 1",
        "Brazo 2", "Arm 2",
        "Cuerpo +/-", "Body +/-",
        "Palito", "Stick",
        "Musculoso", "Muscular",
        "Gordito", "Chubby",
        "Flaco", "Skinny",
        "Niño", "Kid",
        "Normal", "Normal",
        "Con estilo", "With style",
        "Sigiloso", "Sneaky",
        "Libre (3D)", "Free (3D)",
        "Plano (2D)", "Plane (2D)",
        "Fondo: Cuadrícula", "Background: Grid",
        "Fondo: Blanco", "Background: White",
        "Fondo: Realidad", "Background: Reality",
        "Fondo: ", "Background: ",
        "Cuadrícula", "Grid",
        "Blanco", "White",
        "Realidad", "Reality",
        "Guardar", "Save",
        "Archivos", "Files",
        "Borrar todo", "Clear all",
        "Esfera", "Sphere",
        "Cubo", "Cube",
        "Cilindro", "Cylinder",
        "A líneas", "To lines",
        "Imán: Sí", "Magnet: On",
        "Imán: No", "Magnet: Off",
        "Figuras 3D", "3D shapes",
        "Despegar", "Detach",
        "Ver 100%", "Show 100%",
        "Ver 50%", "Show 50%",
        "Ver 20%", "Show 20%",
        "pellizca aquí para mover · + otra mano = tamaño", "pinch here to move · + other hand = size",
        "Fotograma ", "Frame ",
        "  (clave de ", "  (key of ",
        "   ·   barra ", "   ·   bar ",

        // ----- Mis archivos -----
        "Mis archivos", "My files",
        "Todos", "All",
        "Dibujos", "Drawings",
        "Videos", "Videos",
        "Fotos", "Photos",
        "Dibujo", "Drawing",
        "Video", "Video",
        "SVG", "SVG",
        "Abrir dibujo", "Open drawing",
        "Abrir", "Open",
        "Borrar", "Delete",
        "Volver", "Back",
        "Dónde está", "Where is it",
        "¿Seguro? Toca otra vez", "Sure? Tap again",
        "Guardar copia", "Save copy",
        "Renombrar", "Rename",
        "Actual: ", "Current: ",
        " · sin guardar", " · unsaved",
        "(sin nombre)", "(no name)",
        "Vacío", "Empty",
        " archivos", " files",
        "Nombre del dibujo", "Drawing name",
        "Mis archivos: toca uno para elegirlo", "My files: tap one to select it",

        // ----- Gestos (sobre la mano) -----
        "Dibujar (acerca el dedo al plano)", "Draw (bring your finger to the plane)",
        "Dibujar", "Draw",
        "Línea recta (acerca el dedo al plano)", "Straight line (bring your finger to the plane)",
        "Línea recta", "Straight line",
        "Bloqueado (doble toque)", "Locked (double tap)",
        "Borrar\n(frota la línea para borrarla entera)", "Erase\n(rub the line to erase it all)",
        "Grosor (esta línea)\ngira el índice = líneas nuevas", "Width (this line)\nturn your index = new lines",
        "Grosor (todo)\ngira el índice = líneas nuevas", "Width (all)\nturn your index = new lines",
        "Nodos de la figura\n(pellizca la superficie = nodo nuevo)", "Shape nodes\n(pinch the surface = new node)",
        "Suavizar figura\n(sube o baja la izquierda)", "Smooth shape\n(raise or lower your left hand)",
        "Editar nodos (esta línea)", "Edit nodes (this line)",
        "Editar nodos", "Edit nodes",
        "Girar / escalar figura", "Rotate / scale shape",
        "Girar / escalar imagen", "Rotate / scale image",
        "Girar / escalar línea", "Rotate / scale line",
        "Girar / escalar todo", "Rotate / scale all",
        "Girar / mirar todo", "Rotate / look around",
        "Mover línea", "Move line",
        "Mover imagen", "Move image",
        "Deshacer", "Undo",
        "Rehacer", "Redo",
        "Deshecho", "Undone",
        "Cuentagotas: color tomado", "Eyedropper: color picked",
        "Música: Arpa", "Music: Harp",
        "Colores", "Colors",
        "Música: Piano", "Music: Piano",
        "Música: Marimba", "Music: Marimba",
        "Música: Cajita", "Music: Music box",
        "Música: No", "Music: Off",
        "Música: Rock", "Music: Rock",
        "Música: Punk", "Music: Punk",
        "Música: Drum & Bass", "Music: Drum & Bass",
        "Sonido: Sí", "Sound: On",
        "Sonido: No", "Sound: Off",
        "Cubeta: Sí", "Bucket: On",
        "Cubeta: No", "Bucket: Off",
        "Tinta invisible", "Invisible ink",
        "Relleno", "Fill",
        "Relleno quitado", "Fill removed",
        "Nodos elegidos: ", "Nodes selected: ",
        "Cuarto 360", "360 room",
        "Roma de noche", "Rome at night",
        "Canal de Venecia", "Venice canal",
        "Callejón de Venecia", "Venice alley",
        "Atardecer en el mar", "Sunset by the sea",
        "Crucero de lujo", "Luxury cruise ship",
        "Pórtico de columnas", "Columned porch",
        "Mirador", "Lookout",
        "Parque", "Park",
        "La Luna", "The Moon",
        "No hay fondos 360: en Unity toca TrazoVR > ★ Armar escena (y luego Ctrl+S y Build)", "No 360 backgrounds: in Unity run TrazoVR > ★ Armar escena (then Ctrl+S and Build)",
        "Amanecer", "Sunrise",
        "Toca una imagen para verla en grande, o pellízcala y sácala del panel", "Tap an image to see it big, or pinch it and pull it out of the panel",
        "Mi foto 360", "My 360 photo",
        "Fondo: Cuarto 360", "Background: 360 room",
        "Fondo: Mi foto 360", "Background: My 360 photo",
        "Imágenes del Quest", "Quest images",
        "Toca una imagen para verla en grande", "Tap an image to see it big",
        "Todas", "All",
        "Descargas", "Downloads",
        "Cámara", "Camera",
        "Capturas", "Screenshots",
        "Otras", "Other",
        "En la app", "In the app",
        " imágenes", " images",
        "< Volver", "< Back",
        "Importar", "Import",
        "Fondo 360", "360 background",
        "¿Importar como imagen, o usarla de fondo 360 a tu alrededor?", "Import it as an image, or use it as a 360 background around you?",
        "¿Importar esta imagen? (para calcar o de referencia)", "Import this image? (to trace or as a reference)",
        "Permite que JCartoons vea tus fotos (ventana del Quest)", "Let JCartoons see your photos (Quest window)",
        "Sin permiso solo ves las imágenes de la app. Para dárselo: Ajustes del Quest > Apps > JCartoons > Permisos", "Without permission you only see the app's images. To allow it: Quest Settings > Apps > JCartoons > Permissions",
        "No se pudo abrir esa imagen", "Couldn't open that image",
        "No se pudo importar esa imagen", "Couldn't import that image",
        "No se pudo usar esa foto de fondo", "Couldn't use that photo as background",
        "Falta el fondo 360: en Unity, vuelve a tocar TrazoVR > ★ Armar escena", "The 360 background is missing: in Unity, run TrazoVR > ★ Armar escena again",
        "Primero activa las hebras (en la paleta de colores)", "First turn on the strands (in the color palette)",
        "Líneas pintadas", "Lines painted",
        "Línea pintada", "Line painted",
        "Color", "Color",
        "Rehecho", "Redone",
        "Nada que deshacer", "Nothing to undo",
        "Nada que rehacer", "Nothing to redo",
        "Borrador x", "Eraser x",
        " líneas elegidas", " lines selected",
        "1 línea elegida", "1 line selected",
        "Izq + pulgar: índice dibuja · índice+medio recta · medio nodos · anular grosor\nPuño: borrar · Pulgar izq/der: deshacer/rehacer · Doble toque: candado · Mira arriba: panel",
        "Left + thumb: index draws · index+middle straight · middle nodes · ring width\nFist: erase · Thumb left/right: undo/redo · Double tap: lock · Look up: panel",

        // ----- Mensajes -----
        "Guardado: ", "Saved: ",
        "Autoguardado: ", "Autosaved: ",
        "No se pudo guardar", "Could not save",
        "Abierto: ", "Opened: ",
        "guardado anterior", "previous save",
        "No se pudo abrir", "Could not open",
        "No hay nada guardado", "Nothing saved yet",
        "Ahora se llama: ", "Now it's called: ",
        "Ya existe un dibujo llamado ", "There's already a drawing called ",
        "No se pudo cambiar el nombre", "Could not rename",
        "Borrado (el pulgar a la izquierda lo recupera)", "Cleared (thumb left brings it back)",
        "No hay nada que borrar", "Nothing to clear",
        "Primero suelta los personajes", "First let go of the characters",
        "Borrado: ", "Deleted: ",
        "No se pudo borrar", "Could not delete",
        "Dibujo bloqueado (doble toque para desbloquear)", "Drawing locked (double tap to unlock)",
        "Dibujo desbloqueado", "Drawing unlocked",
        "Líneas nuevas: ", "New lines: ",
        "Figura borrada (deshacer la devuelve)", "Shape deleted (undo brings it back)",
        "Figura quitada", "Shape removed",
        "Figura convertida en líneas", "Shape turned into lines",
        "Pellizca una figura para elegirla", "Pinch a shape to select it",
        "Pellizca una línea para elegirla", "Pinch a line to select it",
        "Pellizca una imagen para elegirla (o toca la X de su esquina)", "Pinch an image to select it (or tap the X on its corner)",
        "Imagen quitada", "Image removed",
        "Imagen pegada al plano", "Image stuck to the plane",
        "Imagen despegada", "Image detached",
        "Imágenes ocultas", "Images hidden",
        "Imágenes visibles", "Images visible",
        "Imagen: ", "Image: ",
        "Borde de la hoja del lápiz", "Edge of the pencil sheet",
        "Panel fijo. Pellizca el asa (arriba) para moverlo", "Panel pinned. Pinch the handle (top) to move it",
        "El panel aparece al mirar arriba", "The panel appears when you look up",
        "Barra: todos los fotogramas", "Bar: all frames",
        "Barra: ", "Bar: ",
        " fotogramas", " frames",
        "Clave en el fotograma ", "Key at frame ",
        "Clave guardada en el fotograma ", "Key saved at frame ",
        "Clave movida al fotograma ", "Key moved to frame ",
        "Clave quitada (", "Key removed (",
        "Aquí no hay clave de ", "No key here for ",
        "Necesitas al menos 2 claves para el video", "You need at least 2 keys for the video",
        "Necesitas al menos 2 claves", "You need at least 2 keys",
        " cuadros por segundo", " frames per second",
        " cambios por segundo", " changes per second",
        "Ayuda: toca cualquier botón del panel azul (abajo)", "Help: tap any button on the blue panel (below)",
        "Ayuda cerrada", "Help closed",
        "Toca un dibujo para abrirlo (Cargar otra vez = cerrar)", "Tap a drawing to open it (Load again = close)",
        "Aún no has guardado ningún dibujo", "You haven't saved any drawing yet",
        "Grabando el proceso... toca Detener para parar", "Recording the process... tap Stop to finish",
        "Grabando el proceso: ", "Recording the process: ",
        "En pausa: ", "Paused: ",
        "Grabación en pausa (toca Reanudar)", "Recording paused (tap Resume)",
        "Grabando otra vez", "Recording again",
        "Primero toca Grabar", "Tap Record first",
        "Grabación lista (", "Recording ready (",
        "). Toca Video proceso", "). Tap Process video",
        "Grabación lista: ", "Recording ready: ",
        " · toca Video proceso", " · tap Process video",
        "Grabación detenida: llegó a ", "Recording stopped: it reached ",
        " minutos", " minutes",
        "Primero graba el proceso (botón Grabar)", "First record the process (Record button)",
        "Primero detén la grabación", "First stop the recording",
        "Haciendo el video del proceso (x", "Making the process video (x",
        "Haciendo el video... no te muevas mucho", "Making the video... try not to move much",
        "Haciendo el video... (mira los avisos)", "Making the video... (watch the messages)",
        "Video del proceso: ", "Process video: ",
        "Video: ", "Video: ",
        "No se pudo hacer el video", "Could not make the video",
        "Video listo. Búscalo en la ", "Video ready. Find it in the ",
        "Foto guardada. Búscala en la ", "Photo saved. Find it in the ",
        "app Archivos del Quest → ", "Quest Files app → ",
        "carpeta Dibujos de la app (con el cable: Android/data/<la app>/files/Dibujos/", "app's Dibujos folder (with the cable: Android/data/<the app>/files/Dibujos/",
        "Cuadros PNG guardados en ", "PNG frames saved in ",
        "No se pudo tomar la foto", "Could not take the photo",
        "No se pudo guardar la foto", "Could not save the photo",
        "SVG guardado: ", "SVG saved: ",
        "No se pudo guardar el SVG", "Could not save the SVG",
        "No hay líneas para exportar", "There are no lines to export",
        "Los SVG se abren en el PC (Inkscape, Illustrator): Android/data/<la app>/files/Dibujos/", "SVG files open on the PC (Inkscape, Illustrator): Android/data/<the app>/files/Dibujos/",
        "Este visor no tiene teclado aquí (vuelve a tocar ★ Armar escena en Unity)", "This headset has no keyboard here (run ★ Armar escena again in Unity)",
        "No se pudo abrir la foto", "Could not open the photo",
        "Las líneas vivas y el boceto son de la capa activa: ", "Live lines and sketch belong to the active layer: ",
        "Toca la barra = ir a un fotograma · pellizca una clave = moverla · las imágenes y el boceto no salen en videos ni fotos",
        "Tap the bar = go to a frame · pinch a key = move it · images and sketch don't appear in videos or photos",
        "Primero activa las hebras (página Medios)", "First turn on strands (Animate page)",
        "temblor en ciclo de 3 dibujos", "wobble in a cycle of 3 drawings",
        "temblor libre", "free wobble",
        "temblor ", "wobble ",
        "grosor vivo", "live width",
        "grosor normal", "normal width",
        "una sola línea", "a single line",
        " hebras por línea", " strands per line",
        "tinta (sale en fotos y videos)", "ink (appears in photos and videos)",
        "boceto ", "sketch ",
        " (no sale en fotos ni videos)", " (doesn't appear in photos or videos)",
        ": imán encendido (las puntas se unen)", ": magnet on (ends join)",
        ": imán apagado (las líneas quedan como las dibujas)", ": magnet off (lines stay as you draw them)",
        ": plano unido (pegado a las otras capas)", ": joined plane (stuck to the other layers)",
        ": plano propio", ": own plane",
        "Plano: tu próxima línea define el plano", "Plane: your next line sets the plane",
        "Dibujo libre en 3D", "Free 3D drawing",
        "Nodo agregado", "Node added",
        "Nodo agregado a la figura", "Node added to the shape",
        "Líneas unidas", "Lines joined",
        "Figura cerrada", "Shape closed",
        " lista. Pellízcala para moverla; con las dos manos cambia su tamaño", " ready. Pinch it to move it; with both hands change its size",

        // ----- Bocas -----
        "Capa de la boca: ", "Mouth layer: ",
        " · Audio: ", " · Audio: ",
        "sin audio", "no audio",
        "\nGuardar: mueve los nodos de la boca y toca una forma · Lipsync crea las claves", "\nSave: move the mouth nodes and tap a shape · Lipsync makes the keys",
        "Dibuja la boca en su propia capa y elígela · audios propios en Dibujos/Audio", "Draw the mouth on its own layer and pick it · your audios go in Dibujos/Audio",
        "Tocar una boca = guardar su forma", "Tap a mouth = save its shape",
        "Tocar una boca = ponerla en este fotograma", "Tap a mouth = put it on this frame",
        "Aún no hay bocas guardadas", "No mouths saved yet",
        "Dibuja la boca en la capa ", "Draw the mouth on layer ",
        " primero", " first",
        "Boca ", "Mouth ",
        " guardada (", " saved (",
        " líneas)", " lines)",
        "Grabando voz... toca Voz otra vez para parar", "Recording voice... tap Voice again to stop",
        "Voz grabada (", "Voice recorded (",
        " s). Toca Lipsync", " s). Tap Lipsync",
        "Grabación muy corta", "Recording too short",
        "No encontré micrófono", "No microphone found",
        "No se pudo usar el micrófono", "Could not use the microphone",
        "Acepta el permiso del micrófono y vuelve a tocar Voz", "Accept the microphone permission and tap Voice again",
        "Pon audios (wav, mp3) en Dibujos/Audio", "Put audio files (wav, mp3) in Dibujos/Audio",
        "No encontré el audio", "Audio not found",
        "No se pudo abrir el audio", "Could not open the audio",
        "El audio está vacío", "The audio is empty",
        "Sin audio", "No audio",
        "Primero graba tu voz o elige un audio", "First record your voice or choose an audio",
        "Primero guarda las bocas (modo Guardar)", "First save the mouths (Save mode)",
        "Lipsync listo: ", "Lipsync ready: ",
        " claves. Toca Play", " keys. Tap Play",
        "Muestra de bocas lista (Capa 3). Toca Voz, habla, Voz otra vez, luego Lipsync y Play", "Sample mouths ready (Layer 3). Tap Voice, speak, Voice again, then Lipsync and Play",

        // ----- Títere -----
        "Crear = aparece y se enciende solo · choca esos cinco para encender otro · elegido: ", "Create = it appears and turns on by itself · high five to turn on another · selected: ",
        "Crear = se enciende solo · mano a los lados = caminar/correr · golpe arriba = saltar · palma arriba = apagar", "Create = turns on by itself · hand sideways = walk/run · flick up = jump · palm up = turn off",
        "Mano a los lados = caminar/correr · golpe rápido hacia arriba = saltar · palma arriba (o Parar) = apagar", "Hand sideways = walk/run · quick flick up = jump · palm up (or Stop) = turn off",
        "Acomoda las piernas con el índice y el medio · pellizco IZQUIERDO = guardar clave", "Pose the legs with index and middle fingers · LEFT pinch = save key",
        " · pisos: ", " · floors: ",
        "ninguno", "none",
        "Personaje: ", "Character: ",
        "Personaje apagado", "Character turned off",
        "No hay ningún personaje moviéndose", "No character is moving",
        "Pon la mano derecha frente a ti...", "Put your right hand in front of you...",
        "Pon la mano izquierda frente a ti...", "Put your left hand in front of you...",
        "No veo bien tu mano ", "I can't see your hand well: ",
        "derecha", "right",
        "izquierda", "left",
        "¡Muévelo! A los lados = caminar · golpe hacia arriba = saltar · palma arriba = apagar", "Move it! Sideways = walk · flick up = jump · palm up = turn off",
        "Grabando en ", "Recording in ",
        "Grabando... choca esos cinco con tu personaje (o Parar) para terminar", "Recording... high five your character (or Stop) to finish",
        "Grabados ", "Recorded ",
        " fotogramas. Toca Play para verlo", " frames. Tap Play to watch",
        "Caminado: ", "Walk: ",
        "Mira hacia el otro lado", "Facing the other way",
        "Mira hacia el lado normal", "Facing the normal way",
        "Primero crea un personaje", "First create a character",
        "Primero crea o elige un personaje", "First create or pick a character",
        "La línea ya no es parte del cuerpo", "The line is no longer part of the body",
        "Línea agregada al cuerpo (", "Line added to the body (",
        "La línea ya no es piso", "The line is no longer a floor",
        "Línea marcada como piso (", "Line marked as floor (",
        "Anima al menos 3 claves de un paso (la última igual a la primera)", "Animate at least 3 keys of a step (the last equal to the first)",
        "Ciclo guardado: ", "Cycle saved: ",
        " poses)", " poses)",
        "Posar terminado", "Posing finished",
        " borrado (deshacer lo devuelve)", " deleted (undo brings it back)",

        // ----- Mis archivos: pestañas Crear, Grabar proceso e Imágenes -----
        "Crear", "Create",
        "Grabar proceso", "Record process",
        "Imágenes", "Images",
        "Foto (PNG)", "Photo (PNG)",
        "Crear SVG (2 archivos)", "Create SVG (2 files)",
        "Traer imagen", "Bring image",
        "Lo último: ", "Latest: ",
        "Aún no hay grabación", "No recording yet",
        "Una imagen del dibujo desde donde estás.\nSin paneles, nodos ni capas de boceto.", "An image of the drawing from where you are.\nNo panels, nodes or sketch layers.",
        "Las líneas como curvas (Illustrator, Inkscape):\n_lineas = el trazo limpio · _como_se_ve = con temblor y hebras, como en la app.", "The lines as curves (Illustrator, Inkscape):\n_lineas = the clean stroke · _como_se_ve = with wobble and strands, as in the app.",
        "Video anim (tu animación en MP4): en la página Animar del menú de arriba.", "Anim video (your animation as MP4): on the Animate page of the top menu.",
        "Todo se guarda en: app Archivos del Quest → Descargas → JCartoons", "Everything is saved in: Quest Files app → Downloads → JCartoons",
        "1) Grabar: graba cómo dibujas (tus manos y las líneas que aparecen). Puedes cerrar este panel y dibujar.\n2) Vuelve aquí y toca Detener (Pausa = un descanso que no sale en el video).\n3) Elige la velocidad (Vel) y toca Video proceso: un MP4 en Descargas → JCartoons.",
        "1) Record: records how you draw (your hands and the lines appearing). You can close this panel and draw.\n2) Come back here and tap Stop (Pause = a break that is not in the video).\n3) Choose the speed and tap Process video: an MP4 in Downloads → JCartoons.",
        "Traer imagen: las imágenes de tu Quest (Descargas, Cámara, capturas, WhatsApp...). Toca una para verla en grande: Importar o, si es una foto 360, Fondo 360. O pellízcala y sácala del panel: queda donde la sueltes.\nPara calcar: en Plano 2D, suelta la imagen cerca del plano y se pega detrás.\nPara quitar una imagen: toca la X de su esquina.",
        "Bring image: the images on your Quest (Downloads, Camera, screenshots, WhatsApp...). Tap one to see it big: Import or, if it is a 360 photo, 360 background. Or pinch it and pull it out of the panel: it stays where you drop it.\nTo trace: in Plane 2D, drop the image near the plane and it sticks behind it.\nTo remove an image: tap the X on its corner.",
        "Video anim: la animación en MP4, en Descargas → JCartoons", "Anim video: the animation as MP4, in Downloads → JCartoons",
        "No encuentro el grabador (vuelve a tocar ★ Armar escena en Unity)", "Recorder not found (run ★ Armar escena in Unity again)",
        "No encuentro las imágenes de referencia (vuelve a tocar ★ Armar escena en Unity)", "Reference images not found (run ★ Armar escena in Unity again)",
        "Los SVG se abren en el PC (Inkscape, Illustrator). Están en la app Archivos del Quest → Descargas → JCartoons → ", "SVG files open on a PC (Inkscape, Illustrator). They are in the Quest Files app → Downloads → JCartoons → ",
        "Halo: sí (las líneas se separan del fondo)", "Halo: on (lines stand out from the background)",
        "Halo: no", "Halo: off",
        "V: solo este tirador (esquina)", "V: only this handle (corner)",
        "Tirador en cero: ese lado sale recto", "Handle at zero: that side goes straight",
        "Tiradores en cero: esquina recta", "Handles at zero: straight corner",
        "Toca la barra = ir a un fotograma · pellizca una clave = moverla · Foto, SVG, Grabar proceso e imágenes: en Mis archivos", "Touch the bar = go to a frame · pinch a key = move it · Photo, SVG, Record process and images: in My files",
    };
}

using System.Collections.Generic;
using UnityEngine;

// LOS COMANDOS DE VOZ (parte de ControlManos, v40). La voz (Voz.cs) le pasa lo que entendió y aquí se hace.
// En el idioma de la app (español o inglés), cortos y con sus variantes:
//  - MODOS, como si la mano izquierda los sostuviera (la izquierda queda libre), hasta decir "YA":
//      "dibuja" (dibujas con la punta del índice derecho), "borra" (el índice derecho borra), "nodos", "recta".
//  - "deshaz", "rehaz" · "gira izquierda / derecha / atrás" (fondos 360) · "capas", "capa nueva" · "mesa",
//    "play", "pausa", "siguiente", "anterior", "clave", "quita clave" · "paleta", "guarda", "vista limpia",
//    "silencio" (o "música"), "halo".
//  - "Borrar todo" NO va por voz (por seguridad).
// Los gestos siguen funcionando al mismo tiempo.
public partial class ControlManos
{
    enum ComandoVoz
    {
        Nada, Dibuja, Borra, Nodos, Recta, Ya, Deshaz, Rehaz, GiraIzquierda, GiraDerecha, GiraAtras, Capas, CapaNueva,
        Mesa, Play, Pausa, Siguiente, Anterior, Clave, QuitaClave, Paleta, Guarda, VistaLimpia, Silencio, Halo
    }

    struct FrasesComando
    {
        public ComandoVoz comando;
        public string nombreEs, nombreEn;   // como se muestra ("Gira derecha")
        public string[] es, en;             // lo que se puede decir
    }

    static readonly FrasesComando[] Comandos =
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
          new[] { "gira atrás", "girar atrás", "da la vuelta", "media vuelta", "vuelta" }, new[] { "turn around", "around" }),
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
        C(ComandoVoz.Guarda, "Guarda", "Save", new[] { "guarda", "guardar" }, new[] { "save" }),
        C(ComandoVoz.VistaLimpia, "Vista limpia", "Clean view", new[] { "vista limpia", "limpia" }, new[] { "clean view", "clean" }),
        C(ComandoVoz.Silencio, "Silencio", "Mute", new[] { "silencio", "música", "sonido" }, new[] { "mute", "music", "sound" }),
        C(ComandoVoz.Halo, "Halo", "Halo", new[] { "halo" }, new[] { "halo" }),
    };

    static FrasesComando C(ComandoVoz c, string nombreEs, string nombreEn, string[] es, string[] en)
    {
        return new FrasesComando { comando = c, nombreEs = nombreEs, nombreEn = nombreEn, es = es, en = en };
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
        UltimoComandoVoz = ingles ? c.nombreEn : c.nombreEs;
        UltimoComandoHora = Time.time;
        if (Cabeza != null)
            Burbuja(Cabeza.position + Cabeza.forward * 0.4f - Cabeza.right * 0.15f, 1.05f);
        HacerComando(c.comando);
    }

    void HacerComando(ComandoVoz c)
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
        }
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

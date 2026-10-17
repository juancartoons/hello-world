using TMPro;
using UnityEngine;

// EL TECLADO DE JCARTOONS (v50): un teclado propio que flota delante de ti y se toca con el índice (el del sistema
// del Quest no salía). Lo usan el nombre de las capas y el de los dibujos en Archivos.
//  - Arriba se ve lo que escribes. Mayús cambia entre minúsculas y MAYÚSCULAS. "Listo" guarda; "Cancelar" lo cierra.
public class TecladoVR : MonoBehaviour
{
    static TecladoVR instancia;
    public static bool Abierto => instancia != null && instancia.gameObject.activeSelf;

    static readonly string[] Filas = { "1234567890", "qwertyuiop", "asdfghjklñ", "zxcvbnm-_." };
    const float Tecla = 0.03f, Paso = 0.034f;
    const int Maximo = 32;

    System.Action<string> alTerminar;
    string texto = "";
    bool mayus;
    TextMeshPro pantalla, titulo;
    readonly System.Collections.Generic.List<BotonTocable> letras = new System.Collections.Generic.List<BotonTocable>();
    readonly System.Collections.Generic.List<char> letrasChar = new System.Collections.Generic.List<char>();
    BotonTocable btnMayus;

    // Abre el teclado delante de ti con un texto inicial. "listo" recibe lo escrito (no se llama si cancelas).
    public static void Abrir(string titulo, string inicial, System.Action<string> listo)
    {
        if (instancia == null)
        {
            var go = new GameObject("TecladoVR");
            instancia = go.AddComponent<TecladoVR>();
            if (!instancia.Armar())
            {
                Destroy(go);
                instancia = null;
                return;
            }
        }
        instancia.gameObject.SetActive(true);
        instancia.alTerminar = listo;
        instancia.texto = inicial ?? "";
        instancia.mayus = false;
        if (instancia.titulo != null)
            instancia.titulo.text = Idioma.T(titulo ?? "");
        instancia.Colocar();
        instancia.Refrescar();
    }

    public static void Cerrar()
    {
        if (instancia != null)
            instancia.gameObject.SetActive(false);
    }

    bool Armar()
    {
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (nav == null)
            return false;
        Material panel = nav.materialPanel, normal = nav.materialBoton, marcado = nav.materialBotonMarcado;
        float ancho = Paso * 10f + 0.02f, alto = Paso * 6f + 0.05f;
        var fondo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(fondo.GetComponent<Collider>());
        fondo.name = "Fondo";
        fondo.transform.SetParent(transform, false);
        fondo.transform.localPosition = new Vector3(0f, -0.01f, 0.006f);
        fondo.transform.localScale = new Vector3(ancho, alto, 0.004f);
        var rf = fondo.GetComponent<Renderer>();
        if (panel != null)
            rf.sharedMaterial = panel;
        rf.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        titulo = PanelTutoriales.Texto(transform, "", new Vector3(0f, alto * 0.5f - 0.016f, -0.002f), new Vector2(ancho - 0.02f, 0.014f), 0.12f, null);
        titulo.color = new Color(0.35f, 0.35f, 0.42f);
        pantalla = PanelTutoriales.Texto(transform, "", new Vector3(0f, alto * 0.5f - 0.036f, -0.002f), new Vector2(ancho - 0.02f, 0.022f), 0.2f, null);
        pantalla.fontStyle = FontStyles.Bold;

        float arriba = alto * 0.5f - 0.068f;
        for (int f = 0; f < Filas.Length; f++)
        {
            string fila = Filas[f];
            float x0 = -(fila.Length - 1) * Paso * 0.5f;
            for (int i = 0; i < fila.Length; i++)
            {
                char ch = fila[i];
                var b = PanelTutoriales.Boton(transform, ch.ToString(), new Vector3(x0 + i * Paso, arriba - f * Paso, 0f), new Vector2(Tecla, Tecla), normal, marcado);
                b.pausa = 0.25f;
                b.alTocar.AddListener(() => Escribir(ch));
                letras.Add(b);
                letrasChar.Add(ch);
            }
        }
        float yAbajo = arriba - Filas.Length * Paso;
        btnMayus = PanelTutoriales.Boton(transform, "Mayús", new Vector3(-0.13f, yAbajo, 0f), new Vector2(0.05f, Tecla), normal, marcado);
        btnMayus.alTocar.AddListener(() => { mayus = !mayus; Refrescar(); });
        var espacio = PanelTutoriales.Boton(transform, Idioma.T("Espacio"), new Vector3(-0.055f, yAbajo, 0f), new Vector2(0.09f, Tecla), normal, marcado);
        espacio.pausa = 0.25f;
        espacio.alTocar.AddListener(() => Escribir(' '));
        var borrar = PanelTutoriales.Boton(transform, Idioma.T("Borrar"), new Vector3(0.022f, yAbajo, 0f), new Vector2(0.05f, Tecla), normal, marcado);
        borrar.pausa = 0.25f;
        borrar.alTocar.AddListener(() =>
        {
            if (texto.Length > 0)
                texto = texto.Substring(0, texto.Length - 1);
            Refrescar();
        });
        var cancelar = PanelTutoriales.Boton(transform, Idioma.T("Cancelar"), new Vector3(0.082f, yAbajo, 0f), new Vector2(0.055f, Tecla), normal, marcado);
        cancelar.alTocar.AddListener(Cerrar);
        // "Listo": oscuro, el último paso.
        var listo = PanelTutoriales.Boton(transform, Idioma.T("Listo"), new Vector3(0.143f, yAbajo, 0f), new Vector2(0.055f, Tecla), marcado, normal);
        listo.etiqueta.color = Color.white;
        listo.textoNormal = Color.white;
        listo.alTocar.AddListener(Terminar);
        return true;
    }

    void Escribir(char c)
    {
        if (texto.Length >= Maximo)
            return;
        texto += mayus ? char.ToUpperInvariant(c) : c;
        Refrescar();
    }

    void Terminar()
    {
        var accion = alTerminar;
        alTerminar = null;
        Cerrar();
        if (accion != null)
            accion(texto.Trim());
    }

    void Refrescar()
    {
        if (pantalla != null)
            pantalla.text = texto + "|";
        for (int i = 0; i < letras.Count; i++)
            if (letras[i] != null && letras[i].etiqueta != null)
            {
                char c = letrasChar[i];
                letras[i].etiqueta.text = (mayus ? char.ToUpperInvariant(c) : c).ToString();
            }
        if (btnMayus != null)
            btnMayus.Marcar(mayus);
    }

    // Delante de ti, un poco abajo, mirándote.
    void Colocar()
    {
        var c = ControlManos.Instancia != null ? ControlManos.Instancia.Cabeza : null;
        if (c == null)
            return;
        Vector3 adelante = Vector3.ProjectOnPlane(c.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        Vector3 pos = c.position + adelante * 0.42f + Vector3.down * 0.16f;
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - c.position, Vector3.up));
    }
}

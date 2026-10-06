using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Menú: aparece como si miraras la hora: el dorso de la muñeca izquierda hacia tu cara (y la miras).
// Flota unos centímetros hacia ti y sigue a la mano. Los botones se tocan con el índice derecho.
// También muestra avisos cortos frente a tus ojos ("Deshecho", "Líneas unidas"...).
public class PanelMuneca : MonoBehaviour
{
    public ControlManos control;
    public Dibujo dibujo;
    public Escenario escenario;
    public GameObject contenido;
    public BotonTocable btnPlano, btnFondo, btnGuardar, btnCargar, btnBorrar;
    [Tooltip("Imán de la capa activa (con su ícono de imán encendido o apagado)")]
    public BotonTocable btnIman;
    [Tooltip("Abre la paleta de colores (por si el gesto de la palma no la abre)")]
    public BotonTocable btnColores;
    [Tooltip("? = las secciones del tutorial")]
    public BotonTocable btnAyuda;
    [Tooltip("X para cerrar el menú")]
    public BotonTocable btnCerrar;
    Renderer[] iconoIman;
    Material imanEncendido, imanApagado;
    [Header("Figuras 3D")]
    public Figuras figuras;
    public BotonTocable btnEsfera, btnCubo, btnCilindro, btnALineas, btnQuitarFigura;
    public TMP_Text textoEstado;
    public TMP_Text textoAviso;
    public float suavizado = 20f;

    const string textoAyuda = "Izq + pulgar: índice dibuja · índice+medio recta · medio nodos · anular grosor\nPuño: borrar · Pulgar izq/der: deshacer/rehacer · Doble toque: candado · Mira arriba: panel";

    bool visible;
    float ultimaVezVisto;
    float ocultarMensajeEn;
    float ocultarAvisoEn;

    void Start()
    {
        if (control == null) control = FindFirstObjectByType<ControlManos>();
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (escenario == null) escenario = FindFirstObjectByType<Escenario>();

        if (dibujo != null)
        {
            Conectar(btnPlano, dibujo.AlternarPlano);
            Conectar(btnGuardar, dibujo.Guardar);
            Conectar(btnCargar, AbrirMisArchivos);
            Conectar(btnBorrar, dibujo.BorrarTodo);
            Conectar(btnIman, dibujo.AlternarIman);
            dibujo.alCambiar += Refrescar;
            dibujo.alMensaje += Mensaje;
        }
        Conectar(btnFondo, SiguienteFondo);
        if (control != null)
        {
            Conectar(btnCerrar, control.CerrarMenu);
            Conectar(btnColores, control.AlternarPaleta);
            Conectar(btnAyuda, () =>
            {
                control.CerrarMenu();
                if (Tutorial.Instancia != null)
                    Tutorial.Instancia.AbrirSecciones();
            });
        }
        if (figuras == null && dibujo != null)
            figuras = dibujo.figuras;
        if (figuras != null)
        {
            Conectar(btnEsfera, () => figuras.Agregar(0));
            Conectar(btnCubo, () => figuras.Agregar(1));
            Conectar(btnCilindro, () => figuras.Agregar(2));
            Conectar(btnALineas, figuras.ConvertirSeleccionada);
            Conectar(btnQuitarFigura, figuras.QuitarSeleccionada);
        }

        CrearIconoIman();
        if (textoEstado != null)
            textoEstado.text = Idioma.T(textoAyuda);
        if (textoAviso != null)
            textoAviso.gameObject.SetActive(false);
        Refrescar();
        if (contenido != null)
            contenido.SetActive(false);
    }

    void OnDestroy()
    {
        if (imanEncendido != null) Destroy(imanEncendido);
        if (imanApagado != null) Destroy(imanApagado);
        if (dibujo != null)
        {
            dibujo.alCambiar -= Refrescar;
            dibujo.alMensaje -= Mensaje;
        }
    }

    void AbrirMisArchivos()
    {
        var nav = FindFirstObjectByType<NavegadorArchivos>();
        if (nav != null)
            nav.Abrir(control != null ? control.Cabeza : null);
        else if (dibujo != null)
            dibujo.Cargar();
    }

    static void Conectar(BotonTocable boton, UnityAction accion)
    {
        if (boton != null)
            boton.alTocar.AddListener(accion);
    }

    void SiguienteFondo()
    {
        if (escenario == null)
            return;
        escenario.SiguienteModo();
        Refrescar();
        Mensaje("Fondo: " + Escenario.Nombres[escenario.modo]);
    }

    void Update()
    {
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        bool pose = control != null && control.PuedeVerPanel(visible, out pos, out rot);
        if (pose)
            ultimaVezVisto = Time.time;
        // Pequeña espera antes de esconderlo, para que no parpadee.
        bool ver = pose || (visible && Time.time - ultimaVezVisto < 0.2f);

        if (ver != visible)
        {
            visible = ver;
            if (contenido != null)
                contenido.SetActive(ver);
            if (ver)
                transform.SetPositionAndRotation(pos, rot);
        }
        if (pose)
        {
            float a = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, pos, a);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, a);
        }

        if (ocultarMensajeEn > 0f && Time.time > ocultarMensajeEn)
        {
            ocultarMensajeEn = 0f;
            if (textoEstado != null)
                textoEstado.text = Idioma.T(textoAyuda);
        }
        ActualizarAviso();
    }

    // El aviso flota un poco abajo y al frente de tu vista.
    void ActualizarAviso()
    {
        if (textoAviso == null || !textoAviso.gameObject.activeSelf)
            return;
        if (Time.time > ocultarAvisoEn)
        {
            textoAviso.gameObject.SetActive(false);
            return;
        }
        var cabeza = control != null ? control.Cabeza : null;
        if (cabeza == null)
            return;
        Vector3 pos = cabeza.position + cabeza.forward * 0.6f - cabeza.up * 0.15f;
        textoAviso.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cabeza.position, cabeza.up));
    }

    void Refrescar()
    {
        if (dibujo != null)
        {
            if (btnPlano != null)
            {
                btnPlano.PonerTexto(dibujo.plano ? "Plano (2D)" : "Libre (3D)");
                btnPlano.Marcar(dibujo.plano);
            }
        }
        if (btnFondo != null && escenario != null)
            btnFondo.PonerTexto("Fondo: " + Escenario.Nombres[escenario.modo]);
        if (btnIman != null && dibujo != null)
        {
            bool iman = dibujo.CapaActual.iman;
            btnIman.PonerTexto(iman ? "     Imán: Sí" : "     Imán: No");
            btnIman.Marcar(iman);
            if (iconoIman != null)
                foreach (var r in iconoIman)
                    if (r != null)
                        r.sharedMaterial = iman ? imanEncendido : imanApagado;
        }
    }

    // Ícono de imán (herradura): rojo = encendido, gris = apagado.
    void CrearIconoIman()
    {
        if (btnIman == null || btnIman.materialNormal == null)
            return;
        imanEncendido = new Material(btnIman.materialNormal);
        imanApagado = new Material(btnIman.materialNormal);
        Pintar(imanEncendido, new Color(0.9f, 0.15f, 0.15f));
        Pintar(imanApagado, new Color(0.55f, 0.55f, 0.58f));
        var icono = new GameObject("IconoIman").transform;
        icono.SetParent(btnIman.transform.parent, false);
        icono.localPosition = btnIman.transform.localPosition + new Vector3(-0.0145f, 0f, -0.006f);
        icono.localRotation = Quaternion.identity;
        // Dos patas y la curva de abajo (una U).
        var piezas = new[]
        {
            new[] { -0.003f, 0.001f, 0.0022f, 0.009f },
            new[] { 0.003f, 0.001f, 0.0022f, 0.009f },
            new[] { 0f, -0.0035f, 0.0082f, 0.0022f },
        };
        iconoIman = new Renderer[piezas.Length];
        for (int k = 0; k < piezas.Length; k++)
        {
            var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cubo.GetComponent<Collider>());
            cubo.transform.SetParent(icono, false);
            cubo.transform.localPosition = new Vector3(piezas[k][0], piezas[k][1], 0f);
            cubo.transform.localScale = new Vector3(piezas[k][2], piezas[k][3], 0.002f);
            var r = cubo.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            iconoIman[k] = r;
        }
    }

    static void Pintar(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    public void Mensaje(string texto)
    {
        if (textoEstado != null)
            textoEstado.text = texto;
        ocultarMensajeEn = Time.time + 2.5f;
        if (textoAviso != null)
        {
            textoAviso.text = texto;
            textoAviso.gameObject.SetActive(true);
            ocultarAvisoEn = Time.time + 1.6f;
            ActualizarAviso();
        }
    }
}

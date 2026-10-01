using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Panel de botones que flota frente a la palma izquierda cuando la giras hacia ti.
// Los botones se tocan con la punta del índice derecho.
public class PanelMuneca : MonoBehaviour
{
    public ControlManos control;
    public Dibujo dibujo;
    public Escenario escenario;
    public GameObject contenido;
    public BotonTocable btnDeshacer, btnBorrar, btnGuardar, btnCargar, btnCinta, btnTubo, btnPorLinea, btnFondo;
    public TMP_Text textoEstado;
    public float suavizado = 20f;

    const string textoAyuda = "Izq + pulgar: índice dibuja · medio nodos · anular grosor\nRombo con 2 manos: caja";

    bool visible;
    float ocultarMensajeEn;

    void Start()
    {
        if (control == null) control = FindFirstObjectByType<ControlManos>();
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (escenario == null) escenario = FindFirstObjectByType<Escenario>();

        if (dibujo != null)
        {
            Conectar(btnDeshacer, dibujo.Deshacer);
            Conectar(btnBorrar, dibujo.BorrarTodo);
            Conectar(btnGuardar, dibujo.Guardar);
            Conectar(btnCargar, dibujo.Cargar);
            Conectar(btnCinta, dibujo.PonerCinta);
            Conectar(btnTubo, dibujo.PonerTubo);
            Conectar(btnPorLinea, dibujo.AlternarPorLinea);
            dibujo.alCambiar += Refrescar;
            dibujo.alMensaje += Mensaje;
        }
        Conectar(btnFondo, AlternarFondo);

        if (textoEstado != null)
            textoEstado.text = textoAyuda;
        Refrescar();
        if (contenido != null)
            contenido.SetActive(false);
    }

    void OnDestroy()
    {
        if (dibujo != null)
        {
            dibujo.alCambiar -= Refrescar;
            dibujo.alMensaje -= Mensaje;
        }
    }

    static void Conectar(BotonTocable boton, UnityAction accion)
    {
        if (boton != null)
            boton.alTocar.AddListener(accion);
    }

    void AlternarFondo()
    {
        if (escenario != null)
            escenario.AlternarFondo();
        Refrescar();
    }

    void Update()
    {
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        bool ver = control != null && control.PuedeVerPanel(visible ? 0.35f : 0.6f, out pos, out rot);

        if (ver != visible)
        {
            visible = ver;
            if (contenido != null)
                contenido.SetActive(ver);
            if (ver)
                transform.SetPositionAndRotation(pos, rot);
        }
        if (ver)
        {
            float a = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, pos, a);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, a);
        }

        if (ocultarMensajeEn > 0f && Time.time > ocultarMensajeEn)
        {
            ocultarMensajeEn = 0f;
            if (textoEstado != null)
                textoEstado.text = textoAyuda;
        }
    }

    void Refrescar()
    {
        if (dibujo != null)
        {
            if (btnCinta != null) btnCinta.Marcar(dibujo.estilo == EstiloLinea.Cinta);
            if (btnTubo != null) btnTubo.Marcar(dibujo.estilo == EstiloLinea.Tubo);
            if (btnPorLinea != null)
            {
                btnPorLinea.PonerTexto(dibujo.porLinea ? "Por línea: Sí" : "Por línea: No");
                btnPorLinea.Marcar(dibujo.porLinea);
            }
        }
        if (btnFondo != null && escenario != null)
            btnFondo.PonerTexto(escenario.soloBlanco ? "Fondo: blanco" : "Fondo: cuadrícula");
    }

    public void Mensaje(string texto)
    {
        if (textoEstado != null)
            textoEstado.text = texto;
        ocultarMensajeEn = Time.time + 2.5f;
    }
}

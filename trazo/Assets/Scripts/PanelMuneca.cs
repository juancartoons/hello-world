using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Menú: aparece con la mano izquierda abierta y la punta del pulgar en la base de los dedos.
// Flota unos centímetros hacia ti y sigue a la mano. Los botones se tocan con el índice derecho.
// También muestra avisos cortos frente a tus ojos ("Deshecho", "Líneas unidas"...).
public class PanelMuneca : MonoBehaviour
{
    public ControlManos control;
    public Dibujo dibujo;
    public Escenario escenario;
    public GameObject contenido;
    public BotonTocable btnPlano, btnFondo, btnGuardar, btnCargar, btnBorrar;
    public TMP_Text textoEstado;
    public TMP_Text textoAviso;
    public float suavizado = 20f;

    const string textoAyuda = "Izq + pulgar: índice dibuja · índice+medio recta · medio nodos · anular grosor\nPuño: borrar · Pulgar a la izq: deshacer · Mira arriba: animar";

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
            Conectar(btnCargar, dibujo.Cargar);
            Conectar(btnBorrar, dibujo.BorrarTodo);
            dibujo.alCambiar += Refrescar;
            dibujo.alMensaje += Mensaje;
        }
        Conectar(btnFondo, SiguienteFondo);

        if (textoEstado != null)
            textoEstado.text = textoAyuda;
        if (textoAviso != null)
            textoAviso.gameObject.SetActive(false);
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
                textoEstado.text = textoAyuda;
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

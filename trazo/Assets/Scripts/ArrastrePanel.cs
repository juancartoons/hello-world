using System.Collections.Generic;
using UnityEngine;

// MOVER UN PANEL CON EL PELLIZCO (v38): pellizca con la derecha una parte vacía del panel (no un botón) y llévalo a
// donde quieras; se queda mirándote. Mientras lo llevas, pellizca también con la izquierda y separa o junta las
// manos = tamaño (como con el asa de la mesa de trabajo).
// Lo usan Mis archivos, la ventanita de fondos 360 y el buscador de imágenes (la mesa de trabajo tiene lo suyo).
public class ArrastrePanel : MonoBehaviour
{
    [Tooltip("Centro del área que se puede agarrar (coordenadas del panel)")]
    public Vector2 centro;
    [Tooltip("Medio ancho y medio alto del área que se puede agarrar")]
    public Vector2 medio = new Vector2(0.3f, 0.2f);
    public System.Func<bool> puede;          // si ahora se puede agarrar (null = sí)

    static readonly List<ArrastrePanel> activos = new List<ArrastrePanel>();
    bool agarrado, escalando;
    Vector3 desfase;
    float escalaInicio = 1f, distanciaInicio;

    void OnEnable()
    {
        if (!activos.Contains(this))
            activos.Add(this);
    }

    void OnDisable()
    {
        activos.Remove(this);
        Soltar();
    }

    // ¿Ese punto está sobre algún panel que se puede mover? (Así ese pellizco es del panel y no elige líneas.)
    public static bool Contiene(Vector3 mundo)
    {
        foreach (var a in activos)
            if (a != null && a.isActiveAndEnabled && a.Dentro(mundo))
                return true;
        return false;
    }

    bool Dentro(Vector3 mundo)
    {
        Vector3 l = transform.InverseTransformPoint(mundo);
        return Mathf.Abs(l.x - centro.x) < medio.x && Mathf.Abs(l.y - centro.y) < medio.y && l.z > -0.06f && l.z < 0.04f;
    }

    void LateUpdate()
    {
        var control = ControlManos.Instancia;
        if (control == null)
            return;
        var der = control.Der;
        var izq = control.Izq;
        if (agarrado)
        {
            if (!der.valida || !der.pellizco)
            {
                Soltar();
                return;
            }
            transform.position = der.PuntoPellizco + desfase;
            var cab = control.Cabeza;
            if (cab != null)
            {
                Vector3 mirar = transform.position - cab.position;
                if (mirar.sqrMagnitude > 1e-6f)
                    transform.rotation = Quaternion.LookRotation(mirar, Vector3.up);
            }
            // Con la izquierda también pellizcando: separar/juntar las manos = tamaño.
            if (izq.valida && izq.pellizco)
            {
                float d = Vector3.Distance(izq.PuntoPellizco, der.PuntoPellizco);
                if (!escalando)
                {
                    escalando = true;
                    distanciaInicio = Mathf.Max(0.02f, d);
                    escalaInicio = transform.localScale.x;
                }
                else
                {
                    transform.localScale = Vector3.one * Mathf.Clamp(escalaInicio * d / distanciaInicio, 0.4f, 3f);
                }
            }
            else
            {
                escalando = false;
            }
            return;
        }
        if (!der.valida || !der.empezoPellizco || control.Ocupado || control.GestoIzq != ControlManos.Gesto.Ninguno)
            return;
        if (BotonTocable.DedoCerca || (puede != null && !puede()))
            return;
        if (!Dentro(der.PuntoPellizco))
            return;
        agarrado = true;
        escalando = false;
        desfase = transform.position - der.PuntoPellizco;
        control.Ocupado = true;
    }

    void Soltar()
    {
        if (!agarrado)
            return;
        agarrado = false;
        escalando = false;
        var control = ControlManos.Instancia;
        if (control != null)
            control.Ocupado = false;
    }
}

using UnityEngine;

// GIRAR EL FONDO 360 (parte de ControlManos).
//  - v42: dos flechitas FIJAS en la fila de la pastillita de abajo (ver FlechasGiro.cs): toca una = 30° de una vez;
//    toca y arrastra de lado = el fondo sigue a tu dedo.
//  - Los botones "← Girar" / "Girar →" del menú del reloj y la voz ("gira derecha", "gira izquierda", "gira atrás").
// (Antes: v38 el antebrazo, v39 la mano plana de lado, v40-v41 una flecha que salía al girar la cabeza: costaba.)
// Tus dibujos se quedan contigo, delante de ti: lo que gira es el fondo (ver Escenario.Girar).
public partial class ControlManos
{
    const float GiroPaso = 30f;               // los botones, las flechas (un toque) y la voz

    Escenario escenarioGiro;
    float avisoGiro = -10f;

    // Los botones del menú del reloj (lado = 1: a la derecha; -1: a la izquierda) y la voz.
    public void GirarFondoBoton(int lado)
    {
        Vector3 donde = Cabeza != null ? Cabeza.position + Cabeza.forward * 0.4f : transform.position;
        GirarFondo(lado, donde);
    }

    // lado = 1: giras a la derecha; -1: a la izquierda. "donde": de dónde sale la burbujita.
    public void GirarFondo(int lado, Vector3 donde)
    {
        if (escenarioGiro == null)
            escenarioGiro = FindFirstObjectByType<Escenario>();
        if (escenarioGiro != null && escenarioGiro.Girar(lado * GiroPaso))
        {
            Burbuja(donde, lado > 0 ? 1.15f : 0.95f);
            MostrarEtiqueta(lado > 0 ? "Girar →" : "← Girar");
        }
        else if (Time.time - avisoGiro > 3f)
        {
            avisoGiro = Time.time;
            dibujo.Mensaje("Girar es para los fondos 360");
        }
    }
}

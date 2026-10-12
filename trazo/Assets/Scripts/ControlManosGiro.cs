using UnityEngine;

// GIRAR (parte de ControlManos). v38: como un PANEL TÁCTIL EN TU ANTEBRAZO IZQUIERDO: desliza la punta del índice
// derecho sobre el antebrazo (la "manga") hacia la izquierda o la derecha, unos 5 cm = giras 30° hacia ese lado.
// (Antes era el pulgar sobre el índice y se disparaba solo mientras dibujabas. La palma hacia arriba no sirve: ahí
// se abre la paleta.) También están los botones "← Girar" y "Girar →" del menú del reloj.
// Sirve para ver la parte de atrás de los fondos 360 sin darte la vuelta. Tus dibujos se quedan contigo, delante
// de ti: lo que gira es el fondo (ver Escenario.Girar).
public partial class ControlManos
{
    const float GiroPaso = 30f;           // grados por cada deslizada
    const float DeslizarMinimo = 0.05f;   // metros que debe recorrer el dedo sobre el antebrazo
    const float DeslizarTiempo = 0.7f;    // en menos de estos segundos (si es más lento no cuenta)
    const float LargoAntebrazo = 0.24f;   // de la muñeca hacia el codo (metros)

    bool dedoEnAntebrazo;
    float inicioGiro, desdeGiro, bloqueadoGiro;
    Escenario escenarioGiro;
    float avisoGiro = -10f;

    // Cada cuadro. Solo con la mano izquierda libre (sin gesto), sin la paleta ni el menú abiertos.
    void RevisarGiros()
    {
        bool permitido = GestoIzq == Gesto.Ninguno && !flechaIzq.activa && !flechaDer.activa && !paletaAbierta
                         && !menuAbierto && !Der.pellizco && !Ocupado;
        float lado;
        if (!permitido || !DedoEnAntebrazo(out lado))
        {
            dedoEnAntebrazo = false;
            return;
        }
        float ahora = Time.time;
        if (!dedoEnAntebrazo || ahora - desdeGiro > DeslizarTiempo)
        {
            // Empieza (o fue muy lento): desde aquí se mide.
            dedoEnAntebrazo = true;
            inicioGiro = lado;
            desdeGiro = ahora;
            return;
        }
        float recorrido = lado - inicioGiro;
        if (Mathf.Abs(recorrido) < DeslizarMinimo || ahora < bloqueadoGiro)
            return;
        bloqueadoGiro = ahora + 0.5f;
        inicioGiro = lado;
        desdeGiro = ahora;
        GirarFondo(recorrido > 0f ? 1 : -1, Der.indice);
    }

    // ¿La punta del índice derecho está sobre el antebrazo izquierdo? lado: dónde está a lo ancho de tu vista.
    bool DedoEnAntebrazo(out float lado)
    {
        lado = 0f;
        if (!Izq.valida || !Der.valida || !poseValida || Cabeza == null)
            return false;
        Transform muneca = ManosUtil.Hueso(Izq.esqueleto, "WristRoot", "Wrist");
        if (muneca == null || dirDedosIzq.sqrMagnitude < 1e-6f)
            return false;
        // El antebrazo sigue la línea de la mano, de la muñeca hacia el codo.
        Vector3 eje = dirDedosIzq.normalized;
        Vector3 codo = muneca.position - eje * LargoAntebrazo;
        Vector3 p = Der.indice;
        float t = Vector3.Dot(p - codo, eje);
        if (t < 0.03f || t > LargoAntebrazo - 0.02f)
            return false;                                  // ni sobre la mano ni más allá del codo
        if (Vector3.Distance(p, codo + eje * t) > 0.07f)
            return false;                                  // sobre la "manga" (el brazo mide unos 3-4 cm de radio)
        lado = Vector3.Dot(p, LadoHorizontal(1));
        return true;
    }

    // Los botones del menú del reloj (lado = 1: a la derecha; -1: a la izquierda).
    public void GirarFondoBoton(int lado)
    {
        Vector3 donde = Cabeza != null ? Cabeza.position + Cabeza.forward * 0.4f : transform.position;
        GirarFondo(lado, donde);
    }

    // lado = 1: giras a la derecha; -1: a la izquierda.
    void GirarFondo(int lado, Vector3 donde)
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

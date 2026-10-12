using System.Collections.Generic;
using UnityEngine;

// GIRAR (parte de ControlManos). v39: "PASAR PÁGINA" con la mano DERECHA: la mano abierta y plana, de lado (como
// un cuchillo: la palma mira a tu izquierda o a tu derecha), y un barrido rápido de unos 20 cm hacia un lado = el
// fondo gira 30°. Arrastras el fondo hacia donde va tu mano: barrer a la izquierda trae al frente lo que estaba a
// tu derecha (como pasar la página de una tablet).
// No usa la mano izquierda, así no se abren la paleta ni el menú del reloj. Para que no se dispare solo: tiene que
// ser rápido, con la mano bien abierta y de lado, delante de ti a la altura del pecho o más arriba, y lejos de los
// menús. (v38 era sobre el antebrazo izquierdo y se confundía con la paleta; v34, el pulgar sobre el índice.)
// También están los botones "← Girar" y "Girar →" del menú del reloj.
// Sirve para ver la parte de atrás de los fondos 360 sin darte la vuelta. Tus dibujos se quedan contigo, delante
// de ti: lo que gira es el fondo (ver Escenario.Girar).
public partial class ControlManos
{
    const float GiroPaso = 30f;            // grados por cada barrido
    const float BarridoMinimo = 0.2f;      // metros que debe recorrer la mano de lado
    const float BarridoTiempo = 0.4f;      // en menos de estos segundos
    const float BarridoVertical = 0.1f;    // sin subir ni bajar más que esto mientras tanto

    struct MuestraGiro
    {
        public float t, x, y;
    }

    readonly List<MuestraGiro> muestrasGiro = new List<MuestraGiro>();
    float bloqueadoGiro;
    Escenario escenarioGiro;
    float avisoGiro = -10f;

    // Cada cuadro. Solo con un fondo 360 puesto, sin gesto en la mano izquierda, sin pellizco y sin la paleta ni el
    // menú abiertos (con los otros fondos no hay nada que girar: así tampoco sale el aviso sin querer).
    void RevisarGiros()
    {
        if (escenarioGiro == null)
            escenarioGiro = FindFirstObjectByType<Escenario>();
        bool permitido = escenarioGiro != null && escenarioGiro.modo >= 3 && GestoIzq == Gesto.Ninguno
                         && !flechaIzq.activa && !flechaDer.activa && !paletaAbierta && !menuAbierto && !Der.pellizco
                         && !Ocupado && !Titere.ManoCerca && Cabeza != null;
        Vector3 centro;
        if (!permitido || !ManoPlanaDeLado(out centro))
        {
            muestrasGiro.Clear();
            return;
        }
        float ahora = Time.time;
        Vector3 rel = centro - Cabeza.position;
        var actual = new MuestraGiro { t = ahora, x = Vector3.Dot(rel, LadoHorizontal(1)), y = rel.y };
        muestrasGiro.Add(actual);
        while (muestrasGiro.Count > 0 && ahora - muestrasGiro[0].t > BarridoTiempo)
            muestrasGiro.RemoveAt(0);
        if (ahora < bloqueadoGiro)
            return;
        // ¿Desde alguna muestra de hace menos de 0.4 s la mano recorrió 20 cm de lado (sin subir ni bajar)?
        foreach (var antes in muestrasGiro)
        {
            float dx = actual.x - antes.x;
            if (Mathf.Abs(dx) >= BarridoMinimo && Mathf.Abs(actual.y - antes.y) < BarridoVertical)
            {
                bloqueadoGiro = ahora + 0.6f;
                muestrasGiro.Clear();
                // La mano va a la izquierda = lo que estaba a tu derecha viene al frente (giras a la derecha).
                GirarFondo(dx < 0f ? 1 : -1, centro);
                return;
            }
        }
    }

    // La derecha abierta y plana (índice, medio y anular estirados), de lado (la palma mira a un lado, no a ti ni
    // al piso), delante de ti a la altura del pecho o más arriba, y no sobre un botón ni un panel.
    bool ManoPlanaDeLado(out Vector3 centro)
    {
        centro = Vector3.zero;
        if (!Der.valida)
            return false;
        if (Der.estiradoIndice < 1.6f || Der.estiradoMedio < 1.6f || Der.estiradoAnular < 1.5f)
            return false;
        var palma = ManosUtil.LeerPalma(Der.esqueleto, false);
        if (!palma.valida || Mathf.Abs(Vector3.Dot(palma.normal, LadoHorizontal(1))) < 0.6f)
            return false;
        centro = palma.centro;
        Vector3 rel = centro - Cabeza.position;
        if (rel.y < -0.45f || Vector3.Dot(rel, Cabeza.forward) < 0.15f)
            return false;
        if (BotonTocable.DedoCerca || BotonSonido.DedoCerca || CapasProfundidad.DedoCerca || ArrastrePanel.Contiene(Der.indice))
            return false;
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

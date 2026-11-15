using UnityEngine;

// v68: DESLIZAR EL FONDO (parte de ControlManos), como pasar de video en Instagram: pon el ÍNDICE (de cualquier mano)
// un poquito arriba de lo que ves y bájalo de un jalón hasta un poquito abajo. Cada vez pasa al SIGUIENTE FONDO
// (en el orden de siempre, también los 360), igual que decir "siguiente fondo".
//  - Tiene que empezar arriba de verdad (en la orilla de arriba de tu vista) y terminar abajo en poco tiempo, casi en
//    línea recta hacia abajo, con el índice estirado. Así no se confunde con bajar la mano para descansar.
//  - No cuenta mientras dibujas o haces un gesto, ni con la paleta abierta.
public partial class ControlManos
{
    const float DeslizaArriba = 28f;      // grados sobre el centro de tu vista para empezar
    const float DeslizaAbajo = -28f;      // grados bajo el centro para terminar
    const float DeslizaTiempo = 1.1f;     // segundos máximos de arriba a abajo
    const float DeslizaLado = 22f;        // cuánto se puede ir de lado (grados)

    class Deslizar
    {
        public bool armado;
        public float desde;
        public float ladoInicio;
    }
    readonly Deslizar deslizaIzq = new Deslizar(), deslizaDer = new Deslizar();
    float ultimoDeslizar = -10f;

    // Cada cuadro (desde LateUpdate).
    void ActualizarDeslizarFondo()
    {
        bool permitido = Cabeza != null && (!Ocupado || MesaEntrada.Activa) && !ExportadorVideo.Exportando && !Titere.Activo
                         && !Tutorial.EnCurso && GestoIzq == Gesto.Ninguno && trazoActual == null && !paletaAbierta
                         && Time.time - ultimoDeslizar > 0.8f;
        RevisarDeslizar(Izq, deslizaIzq, permitido);
        RevisarDeslizar(Der, deslizaDer, permitido);
    }

    void RevisarDeslizar(ManoSeguida m, Deslizar d, bool permitido)
    {
        if (!permitido || !m.valida || m.pellizco || m.estiradoIndice < 1.5f)
        {
            d.armado = false;
            return;
        }
        Vector3 rel = m.indice - Cabeza.position;
        float adelante = Vector3.Dot(rel, Cabeza.forward);
        if (adelante < 0.12f)
        {
            d.armado = false;
            return;
        }
        float alto = Mathf.Atan2(Vector3.Dot(rel, Cabeza.up), adelante) * Mathf.Rad2Deg;
        float lado = Mathf.Atan2(Vector3.Dot(rel, Cabeza.right), adelante) * Mathf.Rad2Deg;
        if (alto > DeslizaArriba)
        {
            // Arriba: se arma (y se vuelve a armar mientras siga arriba).
            d.armado = true;
            d.desde = Time.time;
            d.ladoInicio = lado;
            return;
        }
        if (!d.armado)
            return;
        if (Time.time - d.desde > DeslizaTiempo || Mathf.Abs(lado - d.ladoInicio) > DeslizaLado)
        {
            d.armado = false;
            return;
        }
        if (alto < DeslizaAbajo)
        {
            d.armado = false;
            deslizaIzq.armado = false;
            deslizaDer.armado = false;
            ultimoDeslizar = Time.time;
            SiguienteFondoVoz();
            TicGesto(1.35f, m.indice);
        }
    }
}

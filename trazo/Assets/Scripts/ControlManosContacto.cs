using UnityEngine;

// v71: DIBUJAR CON SOLO TOCAR EL PAPEL (parte de ControlManos). En la mesa de animación (modo 2D) no hace falta el OK
// de la mano izquierda: mientras tu índice derecho está sobre el papel, vale como si la izquierda sostuviera el gesto
// de DIBUJAR (igual que el modo "dibuja" de la voz). La línea empieza al tocar la hoja y termina al levantar el dedo
// (eso ya lo hace el plano 2D de siempre).
//  - Si la mano izquierda hace un gesto de verdad (OK, nodos, grosor, puño), la mano manda, como siempre.
//  - Con la paleta o el reloj abiertos, o con un modo puesto con la voz, no aplica.
//  - Quién decide si el dedo está "sobre el papel" es la mesa (MesaEntrada pone "dibujarAlTocar").
public partial class ControlManos
{
    // null = apagado. Devuelve true si ese punto (la punta del índice derecho) está sobre el papel de la mesa.
    public System.Func<Vector3, bool> dibujarAlTocar;
    bool gestoContacto;          // el gesto de dibujar lo puso el contacto con el papel (no la mano izquierda)
    float contactoFuera = -1f;
    bool transformarMesa;        // el pellizco de dos manos es de la mesa (no mueve el dibujo)

    // v71: MesaEntrada pregunta esto para mover y agrandar la mesa con las dos manos.
    public bool TransformarEsDeLaMesa => GestoIzq == Gesto.Transformar && transformarMesa;
    // ¿El gesto de dibujar lo sostiene el contacto con el papel (y no la mano izquierda)?
    public bool GestoPorContacto => gestoContacto && GestoIzq == Gesto.Dibujar;

    // Desde ActualizarGestoIzquierdo. Devuelve true si el contacto sostiene el gesto (y no hay más que revisar).
    bool ActualizarGestoContacto()
    {
        bool permitido = dibujarAlTocar != null && Der.valida && !Der.pellizco && !paletaAbierta && !menuAbierto;
        // ¿La mano izquierda hace un gesto de verdad? Entonces ella manda.
        bool manoManda = Izq.valida
            && (Vector3.Distance(Izq.pulgar, Izq.indice) < pellizcoEntra || Vector3.Distance(Izq.pulgar, Izq.medio) < pellizcoEntra
                || Vector3.Distance(Izq.pulgar, Izq.anular) < pellizcoEntra || PunoApretado());
        if (gestoContacto)
        {
            if (GestoIzq != Gesto.Dibujar)
            {
                gestoContacto = false;   // algo más lo terminó (un panel, la voz...)
                return false;
            }
            if (!permitido || manoManda)
            {
                gestoContacto = false;
                SalirDeGesto();
                if (manoManda)
                    esperarSoltarIzq = false;
                return false;
            }
            if (dibujarAlTocar(Der.indice))
            {
                contactoFuera = -1f;
            }
            else if (contactoFuera < 0f)
            {
                contactoFuera = Time.time;
            }
            else if (Time.time - contactoFuera > 0.3f && trazoActual == null)
            {
                // Levantaste el dedo: el gesto se suelta (así el reloj y la paleta vuelven a estar libres).
                gestoContacto = false;
                SalirDeGesto();
                return false;
            }
            return true;
        }
        if (permitido && !manoManda && GestoIzq == Gesto.Ninguno && dibujarAlTocar(Der.indice))
        {
            gestoContacto = true;   // (antes de entrar: así no suena el "tic" ni brillan los dedos de la izquierda)
            contactoFuera = -1f;
            EntrarEnGesto(Gesto.Dibujar);
            return true;
        }
        return false;
    }
}

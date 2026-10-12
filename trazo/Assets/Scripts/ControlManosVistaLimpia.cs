using UnityEngine;

// VISTA LIMPIA con la mano derecha (parte de ControlManos, v39): DOBLE PUÑO derecho = cerrar la mano, abrirla y
// cerrarla otra vez en menos de un segundo. Esconde o vuelve a mostrar lo fijo (ver VistaLimpia.cs).
//  - Un solo puño no hace nada: así descansar la mano (medio cerrada) nunca la cambia.
//  - Cada puño tiene que empezar con la mano bien abierta (todos los dedos estirados), y el puño tiene que ser
//    claro (los tres dedos bien doblados): lo de en medio no cuenta, así el visor no se confunde.
//  - Con el pulgar estirado hacia la derecha es la flecha de rehacer: no cuenta.
//  - Solo sin gesto en la mano izquierda, sin pellizco y sin la paleta ni el menú abiertos.
// (No se usa el meñique: el visor lo reconoce mal.)
public partial class ControlManos
{
    const float PunoDobla = 1.28f;          // los tres dedos más doblados que esto (punta a muñeca ÷ nudillo a muñeca)
    const float AbiertaEstira = 1.6f;       // y en promedio más estirados que esto = mano abierta
    const float DoblePunoSegundos = 1.0f;   // del primer puño al segundo

    bool derAbierta;                // la última pose clara de la derecha fue "abierta"
    float primerPunoDer = -10f;

    // Cada cuadro (desde Update).
    void RevisarVistaLimpia()
    {
        bool permitido = Der.valida && !Der.pellizco && GestoIzq == Gesto.Ninguno && !paletaAbierta && !menuAbierto
                         && !flechaDer.activa && !Ocupado;
        if (!permitido)
        {
            derAbierta = false;
            primerPunoDer = -10f;
            return;
        }
        int pose = PoseDerecha();
        if (pose == 1)
        {
            derAbierta = true;
            return;
        }
        if (pose != 0 || !derAbierta)
            return;
        // Un cierre: de mano abierta a puño.
        derAbierta = false;
        float ahora = Time.time;
        if (ahora - primerPunoDer < DoblePunoSegundos)
        {
            primerPunoDer = -10f;
            VistaLimpia.Alternar();
            Burbuja(Der.indice, VistaLimpia.Activa ? 0.9f : 1.1f);
        }
        else
        {
            primerPunoDer = ahora;
        }
    }

    // 0 = puño claro (con el pulgar recogido), 1 = mano abierta, -1 = en medio (no cuenta).
    int PoseDerecha()
    {
        float i = Der.estiradoIndice, m = Der.estiradoMedio, a = Der.estiradoAnular;
        if ((i + m + a) / 3f > AbiertaEstira)
            return 1;
        if (Mathf.Max(i, Mathf.Max(m, a)) > PunoDobla)
            return -1;
        // El pulgar recogido (sobre los dedos o pegado al índice), no estirado hacia el lado.
        Transform nudillo = ManosUtil.Hueso(Der.esqueleto, "Index1", "IndexProximal");
        if (nudillo == null || Vector3.Distance(Der.pulgar, nudillo.position) > 0.07f)
            return -1;
        return 0;
    }
}

using UnityEngine;

// GIRAR (parte de ControlManos), como el gesto de Meta para girar: con cualquiera de las dos manos en puño suelto,
// desliza el PULGAR sobre el costado del índice hacia la izquierda o la derecha = giras 30° hacia ese lado.
// Sirve para ver la parte de atrás de los fondos 360 sin darte la vuelta. Tus dibujos se quedan contigo, delante
// de ti: lo que gira es el fondo (ver Escenario.Girar).
// No choca con deshacer / rehacer: esos son el pulgar ESTIRADO y quieto; este es el pulgar apoyado, deslizándose.
public partial class ControlManos
{
    const float GiroPaso = 30f;           // grados por cada deslizada
    const float DeslizarMinimo = 0.02f;   // metros que debe recorrer el pulgar
    const float DeslizarTiempo = 0.45f;   // en menos de estos segundos (si es más lento no cuenta)

    class EstadoGiro
    {
        public bool enPose;
        public float inicio, desde, bloqueadoHasta;
    }

    readonly EstadoGiro giroIzq = new EstadoGiro(), giroDer = new EstadoGiro();
    Escenario escenarioGiro;
    float avisoGiro = -10f;

    // Cada cuadro (sin gesto izquierdo, o con el borrador: el puño izquierdo).
    void RevisarGiros()
    {
        bool permitido = (GestoIzq == Gesto.Ninguno || GestoIzq == Gesto.Borrar) && !flechaIzq.activa && !flechaDer.activa;
        RevisarGiroPulgar(Izq, true, giroIzq, permitido);
        RevisarGiroPulgar(Der, false, giroDer, permitido);
    }

    void RevisarGiroPulgar(ManoSeguida mano, bool izquierda, EstadoGiro e, bool permitido)
    {
        float lado;
        if (!permitido || !PosePulgarSobreIndice(mano, izquierda, out lado))
        {
            e.enPose = false;
            return;
        }
        float ahora = Time.time;
        if (!e.enPose || ahora - e.desde > DeslizarTiempo)
        {
            // Empieza (o fue muy lento): desde aquí se mide.
            e.enPose = true;
            e.inicio = lado;
            e.desde = ahora;
            return;
        }
        float recorrido = lado - e.inicio;
        if (Mathf.Abs(recorrido) < DeslizarMinimo || ahora < e.bloqueadoHasta)
            return;
        e.bloqueadoHasta = ahora + 0.5f;
        e.inicio = lado;
        e.desde = ahora;
        GirarFondo(recorrido > 0f ? 1 : -1, mano);
    }

    // Puño suelto (índice, medio y anular doblados) con la yema del pulgar apoyada en el costado del índice.
    // lado: dónde está el pulgar a lo ancho de tu vista (para saber si se desliza a la izquierda o a la derecha).
    bool PosePulgarSobreIndice(ManoSeguida mano, bool izquierda, out float lado)
    {
        lado = 0f;
        if (!mano.valida || Cabeza == null)
            return false;
        var esq = mano.esqueleto;
        var palma = ManosUtil.LeerPalma(esq, izquierda);
        Transform i1 = ManosUtil.Hueso(esq, "Index1", "IndexProximal");
        Transform i2 = ManosUtil.Hueso(esq, "Index2", "IndexIntermediate");
        Transform i3 = ManosUtil.Hueso(esq, "Index3", "IndexDistal");
        if (!palma.valida || i1 == null || i2 == null || palma.tamano < 1e-4f)
            return false;
        float tam = palma.tamano;
        if (Vector3.Distance(mano.indice, palma.centro) / tam > 1.45f
            || Vector3.Distance(mano.medio, palma.centro) / tam > 1.45f
            || Vector3.Distance(mano.anular, palma.centro) / tam > 1.5f)
            return false;
        float d = DistanciaASegmento(mano.pulgar, i1.position, i2.position);
        if (i3 != null)
            d = Mathf.Min(d, DistanciaASegmento(mano.pulgar, i2.position, i3.position));
        if (d > 0.03f)
            return false;
        lado = Vector3.Dot(mano.pulgar - palma.centro, LadoHorizontal(1));
        return true;
    }

    // lado = 1: giras a la derecha; -1: a la izquierda.
    void GirarFondo(int lado, ManoSeguida mano)
    {
        if (escenarioGiro == null)
            escenarioGiro = FindFirstObjectByType<Escenario>();
        if (escenarioGiro != null && escenarioGiro.Girar(lado * GiroPaso))
        {
            Burbuja(mano.pulgar, lado > 0 ? 1.15f : 0.95f);
            MostrarEtiqueta(lado > 0 ? "Girar →" : "← Girar");
        }
        else if (Time.time - avisoGiro > 3f)
        {
            avisoGiro = Time.time;
            dibujo.Mensaje("El giro con el pulgar es para los fondos 360");
        }
    }
}

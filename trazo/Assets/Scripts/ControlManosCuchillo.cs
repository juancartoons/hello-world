using UnityEngine;

// EL CUCHILLO (v58, parte de ControlManos): borrar la línea entre DOS NODOS para separarlos.
//  - Con la mano izquierda en NODOS (pulgar + medio), pasa el índice derecho RÁPIDO cruzando la línea entre dos
//    nodos, y luego otra vez DE VUELTA (ida y vuelta, como cortando con un cuchillo). Ese tramo desaparece.
//  - Una figura cerrada se ABRE ahí; una línea abierta queda en DOS líneas. Cada parte conserva su color, grosor,
//    estilo, relleno y los nombres de sus nodos. Se puede deshacer.
//  - Una sola pasada no corta (así no hay accidentes): la primera avisa "otra vez, de vuelta".
//  - Por voz: pon el índice derecho sobre el tramo y di "cortar aquí".
public partial class ControlManos
{
    const float VelocidadCuchillo = 0.45f;   // metros por segundo: más lento no es un corte (es tocar o arrastrar)
    const float RadioCuchillo = 0.015f;      // metros: qué tan cerca de la línea tiene que pasar el dedo
    const float TiempoVuelta = 0.9f;         // segundos para la pasada de vuelta

    Vector3 cuchilloAntes;
    bool cuchilloHayAntes;
    Trazo cuchilloTrazo;
    int cuchilloTramo = -1;
    Vector3 cuchilloDireccion;
    float cuchilloHora = -10f, cuchilloPausa;

    void ReiniciarCuchillo()
    {
        cuchilloHayAntes = false;
        cuchilloTrazo = null;
        cuchilloTramo = -1;
    }

    // Se llama cada cuadro en modo nodos, con la punta del índice derecho (mundo). true = cortó (este cuadro no hace más).
    bool RevisarCuchillo(Vector3 punta, bool ocupadoConAlgo)
    {
        Vector3 antes = cuchilloAntes;
        bool hayAntes = cuchilloHayAntes;
        cuchilloAntes = punta;
        cuchilloHayAntes = true;
        if (ocupadoConAlgo || !hayAntes || Time.deltaTime <= 0f)
            return false;
        if (cuchilloTrazo != null && Time.time - cuchilloHora > TiempoVuelta)
        {
            cuchilloTrazo = null;
            cuchilloTramo = -1;
        }
        Vector3 mov = punta - antes;
        if (mov.magnitude / Time.deltaTime < VelocidadCuchillo || Time.time < cuchilloPausa)
            return false;
        // ¿El dedo cruzó una línea en este cuadro? (se revisan unos puntos del camino del dedo)
        var raiz = dibujo.transform;
        float radio = RadioCuchillo / dibujo.EscalaMundo;
        Trazo t = null;
        int tramo = -1;
        for (int k = 0; k <= 3 && t == null; k++)
        {
            Vector3 p = raiz.InverseTransformPoint(Vector3.Lerp(antes, punta, k / 3f));
            Trazo encontrado;
            int seg;
            if (dibujo.TramoCercano(p, radio, out encontrado, out seg))
            {
                t = encontrado;
                tramo = seg;
            }
        }
        if (t == null)
            return false;
        cuchilloPausa = Time.time + 0.15f;   // una pasada cuenta una sola vez
        // La pasada de VUELTA: la misma línea, el mismo tramo (o el de al lado) y en sentido contrario.
        if (cuchilloTrazo == t && Mathf.Abs(cuchilloTramo - tramo) <= 1 && Vector3.Dot(mov, cuchilloDireccion) < 0f)
        {
            int cortar = cuchilloTramo;
            cuchilloTrazo = null;
            cuchilloTramo = -1;
            if (dibujo.CortarTramo(t, cortar))
            {
                SonidoBurbuja(punta);
                return true;
            }
            return false;
        }
        cuchilloTrazo = t;
        cuchilloTramo = tramo;
        cuchilloDireccion = mov;
        cuchilloHora = Time.time;
        SonidoBurbuja(punta);
        dibujo.Mensaje("Cuchillo: pasa otra vez, de vuelta, para cortar");
        return false;
    }

    // "Cortar aquí" (voz): el tramo bajo el índice derecho (o el que miras).
    void CortarAquiVoz()
    {
        Trazo t;
        int seg;
        if (Der.valida && dibujo.TramoCercano(dibujo.transform.InverseTransformPoint(Der.indice), 0.03f / dibujo.EscalaMundo, out t, out seg))
        {
            dibujo.CortarTramo(t, seg);
            return;
        }
        dibujo.Mensaje("Pon el índice derecho sobre la línea (entre dos nodos) y di \"cortar aquí\"");
    }
}

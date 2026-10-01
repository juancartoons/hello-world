using UnityEngine;

// Lee una mano (OVRHand + OVRSkeleton) y suaviza las puntas de los dedos para que el trazo no tiemble.
public class ManoSeguida
{
    public readonly bool izquierda;
    public OVRHand hand;
    public OVRSkeleton esqueleto;

    public bool valida;          // la mano se ve bien en este momento
    public float sinSenal;       // segundos desde que se perdió la mano
    public Vector3 indice, pulgar, medio, anular; // puntas de los dedos (suavizadas)

    // Pellizco índice + pulgar (con histéresis: entra con poca distancia, sale con más).
    public bool pellizco, empezoPellizco, soltoPellizco;

    bool teniaDatos;
    float proximaBusqueda;

    public ManoSeguida(bool esIzquierda)
    {
        izquierda = esIzquierda;
    }

    public Vector3 PuntoPellizco => (indice + pulgar) * 0.5f;

    public void Actualizar(Transform ancla, float suavizado, float entra, float sale)
    {
        empezoPellizco = false;
        soltoPellizco = false;

        if ((hand == null || esqueleto == null) && Time.time >= proximaBusqueda)
        {
            proximaBusqueda = Time.time + 1f;
            if (hand == null)
                hand = ManosUtil.BuscarEnAncla<OVRHand>(ancla);
            if (esqueleto == null)
                esqueleto = ManosUtil.BuscarEnAncla<OVRSkeleton>(ancla);
        }

        Transform tIndice = ManosUtil.Hueso(esqueleto, "IndexTip");
        Transform tPulgar = ManosUtil.Hueso(esqueleto, "ThumbTip");
        Transform tMedio = ManosUtil.Hueso(esqueleto, "MiddleTip");
        Transform tAnular = ManosUtil.Hueso(esqueleto, "RingTip");
        bool ok = hand != null && hand.IsTracked && tIndice != null && tPulgar != null && tMedio != null && tAnular != null;
        if (!ok)
        {
            valida = false;
            teniaDatos = false;
            sinSenal += Time.deltaTime;
            if (pellizco && sinSenal > 0.25f)
            {
                pellizco = false;
                soltoPellizco = true;
            }
            return;
        }

        sinSenal = 0f;
        float a = teniaDatos ? 1f - Mathf.Exp(-suavizado * Time.deltaTime) : 1f;
        indice = Vector3.Lerp(indice, tIndice.position, a);
        pulgar = Vector3.Lerp(pulgar, tPulgar.position, a);
        medio = Vector3.Lerp(medio, tMedio.position, a);
        anular = Vector3.Lerp(anular, tAnular.position, a);
        teniaDatos = true;
        valida = true;

        float d = Vector3.Distance(indice, pulgar);
        if (!pellizco && d < entra)
        {
            pellizco = true;
            empezoPellizco = true;
        }
        else if (pellizco && d > sale)
        {
            pellizco = false;
            soltoPellizco = true;
        }
    }
}

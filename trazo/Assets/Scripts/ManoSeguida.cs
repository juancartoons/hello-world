using UnityEngine;

// Lee una mano (OVRHand + OVRSkeleton) y suaviza las puntas de los dedos para que el trazo no tiemble.
public class ManoSeguida
{
    public readonly bool izquierda;
    public OVRHand hand;
    public OVRSkeleton esqueleto;

    public bool valida;          // la mano se ve bien en este momento
    public float sinSenal;       // segundos desde que se perdió la mano
    public Vector3 indice, pulgar, medio, anular, menique; // puntas de los dedos (suavizadas)
    public Vector3 indiceCrudo;  // punta del índice SIN suavizar (para el lápiz: va pegado al dedo)
    // Qué tan estirado está cada dedo: (punta a muñeca) ÷ (nudillo a muñeca). ~1.8 estirado, ~1 doblado.
    public float estiradoIndice, estiradoMedio, estiradoAnular, estiradoMenique = 1f;

    // Pellizco índice + pulgar (con histéresis: entra con poca distancia, sale con más).
    public bool pellizco, empezoPellizco, soltoPellizco;
    public float pellizcoDesde;  // Time.time cuando empezó el pellizco

    bool teniaDatos;
    float proximaBusqueda;

    public ManoSeguida(bool esIzquierda)
    {
        izquierda = esIzquierda;
    }

    public Vector3 PuntoPellizco => (indice + pulgar) * 0.5f;

    // Índice y medio estirados (juntos o en V, da igual) y el anular y el meñique doblados: "dos dedos".
    // Así no importa que el Quest no vea bien los dos dedos pegados: basta con que sean los únicos estirados.
    public bool DosDedos => valida && estiradoIndice > 1.5f && estiradoMedio > 1.5f
                            && estiradoAnular < 1.32f && estiradoMenique < 1.38f;

    public float TiempoPellizco => pellizco ? Time.time - pellizcoDesde : 0f;

    static float Estirado(Transform muneca, Transform nudillo, Transform punta)
    {
        if (muneca == null || nudillo == null || punta == null)
            return 1f;
        float baseDedo = Vector3.Distance(nudillo.position, muneca.position);
        return baseDedo > 1e-4f ? Vector3.Distance(punta.position, muneca.position) / baseDedo : 1f;
    }

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
        Transform tMenique = ManosUtil.Hueso(esqueleto, "PinkyTip", "LittleTip");
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
        indiceCrudo = tIndice.position;
        indice = Vector3.Lerp(indice, tIndice.position, a);
        pulgar = Vector3.Lerp(pulgar, tPulgar.position, a);
        medio = Vector3.Lerp(medio, tMedio.position, a);
        anular = Vector3.Lerp(anular, tAnular.position, a);
        // Si no encuentra el meñique, lo deja lejos del pulgar (así nunca cuenta como pellizco).
        Vector3 objetivoMenique = tMenique != null ? tMenique.position : pulgar + Vector3.up;
        menique = teniaDatos ? Vector3.Lerp(menique, objetivoMenique, a) : objetivoMenique;
        teniaDatos = true;
        valida = true;

        Transform tMuneca = ManosUtil.Hueso(esqueleto, "WristRoot", "Wrist");
        estiradoIndice = Estirado(tMuneca, ManosUtil.Hueso(esqueleto, "Index1", "IndexProximal"), tIndice);
        estiradoMedio = Estirado(tMuneca, ManosUtil.Hueso(esqueleto, "Middle1", "MiddleProximal"), tMedio);
        estiradoAnular = Estirado(tMuneca, ManosUtil.Hueso(esqueleto, "Ring1", "RingProximal"), tAnular);
        // Sin meñique, cuenta como doblado.
        estiradoMenique = tMenique != null ? Estirado(tMuneca, ManosUtil.Hueso(esqueleto, "Pinky1", "LittleProximal"), tMenique) : 1f;

        float d = Vector3.Distance(indice, pulgar);
        if (!pellizco && d < entra)
        {
            pellizco = true;
            empezoPellizco = true;
            pellizcoDesde = Time.time;
        }
        else if (pellizco && d > sale)
        {
            pellizco = false;
            soltoPellizco = true;
        }
    }
}

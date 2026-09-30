using UnityEngine;

// Ayudas para leer las manos de Meta Quest (OVRHand / OVRSkeleton).
public static class ManosUtil
{
    // Busca un componente (OVRHand, OVRSkeleton...) que esté dentro de un ancla de mano del OVRCameraRig.
    public static T BuscarEnAncla<T>(Transform ancla) where T : Component
    {
        if (ancla == null)
            return null;
        foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.transform.IsChildOf(ancla))
                return c;
        return null;
    }

    // Busca un hueso del esqueleto de la mano por el final de su nombre.
    // Funciona con los nombres viejos ("Hand_IndexTip") y los nuevos ("XRHand_IndexTip").
    public static Transform Hueso(OVRSkeleton esqueleto, params string[] finalesDeNombre)
    {
        if (esqueleto == null || !esqueleto.IsInitialized || esqueleto.Bones == null)
            return null;
        foreach (var hueso in esqueleto.Bones)
        {
            if (hueso == null || hueso.Transform == null)
                continue;
            string nombre = hueso.Transform.name;
            foreach (var final in finalesDeNombre)
                if (nombre.EndsWith(final))
                    return hueso.Transform;
        }
        return null;
    }

    // Punto entre la punta del pulgar y la del índice (donde "pellizcas"). Si no hay esqueleto, usa el ancla.
    public static Vector3 PuntoDePellizco(OVRSkeleton esqueleto, Transform anclaMano)
    {
        var indice = Hueso(esqueleto, "IndexTip");
        var pulgar = Hueso(esqueleto, "ThumbTip");
        if (indice != null && pulgar != null)
            return (indice.position + pulgar.position) * 0.5f;
        return anclaMano != null ? anclaMano.position : Vector3.zero;
    }
}

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

    // Lo que sabemos de una palma en este momento.
    public struct Palma
    {
        public bool valida;
        public Vector3 centro;   // centro de la palma (sobre la piel)
        public Vector3 normal;   // hacia dónde mira la palma
        public float tamano;     // de la muñeca a los nudillos (≈ 9 cm)
        public float cierre;     // qué tan cerrada: ≈ 0.9 puño, ≈ 1.8 mano abierta
    }

    // Lee la palma desde el esqueleto. Si falta algún hueso, la palma no es válida.
    public static Palma LeerPalma(OVRSkeleton esqueleto, bool esIzquierda)
    {
        var p = new Palma();
        var muneca = Hueso(esqueleto, "WristRoot", "Wrist");
        var indice = Hueso(esqueleto, "Index1", "IndexProximal");
        var medio = Hueso(esqueleto, "Middle1", "MiddleProximal");
        var menique = Hueso(esqueleto, "Pinky1", "LittleProximal");
        if (muneca == null || indice == null || medio == null || menique == null)
            return p;

        // La palma mira hacia el lado contrario del dorso; el signo depende de la mano.
        Vector3 n = Vector3.Cross(indice.position - muneca.position, menique.position - muneca.position).normalized;
        p.normal = esIzquierda ? n : -n;
        p.tamano = Mathf.Max(0.05f, Vector3.Distance(muneca.position, medio.position));
        p.centro = Vector3.Lerp(muneca.position, medio.position, 0.55f) + p.normal * 0.015f;

        float suma = 0f;
        int cuenta = 0;
        foreach (var nombre in new[] { "IndexTip", "MiddleTip", "RingTip" })
        {
            var punta = Hueso(esqueleto, nombre);
            if (punta == null)
                continue;
            suma += Vector3.Distance(punta.position, p.centro);
            cuenta++;
        }
        p.cierre = cuenta > 0 ? suma / cuenta / p.tamano : 2f;
        p.valida = true;
        return p;
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

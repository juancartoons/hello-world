using UnityEngine;

// Hace que un objeto gire siempre hacia el jugador (solo de lado a lado).
// Útil para el personaje o para una "card" 2D, así nunca se ve de perfil.
public class MirarAlJugador : MonoBehaviour
{
    [Tooltip("Velocidad de giro. 0 = instantáneo")]
    public float suavidad = 5f;

    Transform cabeza;

    void Start()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        cabeza = rig != null ? rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : null;
    }

    void LateUpdate()
    {
        if (cabeza == null)
            return;

        Vector3 haciaJugador = cabeza.position - transform.position;
        haciaJugador.y = 0f;
        if (haciaJugador.sqrMagnitude < 0.001f)
            return;

        Quaternion objetivo = Quaternion.LookRotation(haciaJugador, Vector3.up);
        transform.rotation = suavidad <= 0f
            ? objetivo
            : Quaternion.Slerp(transform.rotation, objetivo, Time.deltaTime * suavidad);
    }
}

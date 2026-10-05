using UnityEngine;

// Puerta de vidrio automática: se abre sola (con un "ding-dong") cuando el jugador está cerca
// y se cierra cuando se aleja. Las dos hojas se deslizan hacia los lados.
public class PuertaAutomatica : MonoBehaviour
{
    public Transform hojaIzquierda, hojaDerecha;
    [Tooltip("Cuánto se corre cada hoja al abrirse (metros)")]
    public float apertura = 0.68f;
    [Tooltip("Distancia (metros) a la que se abre")]
    public float distanciaSensor = 1.7f;
    public float velocidad = 1.4f;
    [Range(0f, 1f)] public float volumenCampanita = 0.6f;

    Transform cabeza;
    Vector3 cerradaIzq, cerradaDer;
    float abierta;
    bool abriendo;
    AudioSource fuente;

    void Start()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        cabeza = rig != null ? rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : null;
        if (hojaIzquierda != null) cerradaIzq = hojaIzquierda.localPosition;
        if (hojaDerecha != null) cerradaDer = hojaDerecha.localPosition;
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.clip = SonidosProcedurales.Campanita();
        fuente.spatialBlend = 0.7f;
        fuente.playOnAwake = false;
        fuente.volume = volumenCampanita;
    }

    void Update()
    {
        if (cabeza == null)
            return;
        Vector3 d = cabeza.position - transform.position;
        d.y = 0f;
        bool cerca = d.magnitude < distanciaSensor;
        if (cerca && !abriendo && abierta < 0.5f)
            fuente.Play(); // ding-dong al empezar a abrir
        abriendo = cerca;
        abierta = Mathf.MoveTowards(abierta, cerca ? 1f : 0f, Time.deltaTime * velocidad);
        float k = Mathf.SmoothStep(0f, 1f, abierta) * apertura;
        if (hojaIzquierda != null) hojaIzquierda.localPosition = cerradaIzq + Vector3.left * k;
        if (hojaDerecha != null) hojaDerecha.localPosition = cerradaDer + Vector3.right * k;
    }
}

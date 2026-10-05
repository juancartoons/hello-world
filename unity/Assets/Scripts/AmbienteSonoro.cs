using UnityEngine;
using Random = UnityEngine.Random;

// Sonido de ambiente (todo generado por código):
// - Adentro: música suave de tienda (afuera se oye bajito, como saliendo por la puerta).
// - Afuera: la ciudad (motores y carros que pasan) y de vez en cuando un pito. Adentro se oye apagada.
// - De noche: grillos.
public class AmbienteSonoro : MonoBehaviour
{
    [Range(0f, 1f)] public float volumenMusica = 0.22f;
    [Range(0f, 1f)] public float volumenCiudad = 0.45f;
    [Range(0f, 1f)] public float volumenGrillos = 0.3f;
    [Range(0f, 1f)] public float volumenPitos = 0.5f;
    [Tooltip("Zona de la farmacia por dentro (x mínimo, x máximo, z mínimo, z máximo)")]
    public Vector4 zonaAdentro = new Vector4(-5f, 5f, -5f, 8f);

    AudioSource musica, ciudad, grillos;
    AudioLowPassFilter filtroCiudad;
    AudioClip pito;
    Transform cabeza;
    DiaNoche diaNoche;
    float proximoPito;

    void Start()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        cabeza = rig != null ? rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : null;
        diaNoche = FindFirstObjectByType<DiaNoche>();

        musica = Fuente("Musica", SonidosProcedurales.Musica(), Vector3.zero, 0f);
        ciudad = Fuente("Ciudad", SonidosProcedurales.Ciudad(), new Vector3(0f, 1f, -12f), 0.6f);
        ciudad.minDistance = 6f;
        ciudad.maxDistance = 60f;
        filtroCiudad = ciudad.gameObject.AddComponent<AudioLowPassFilter>();
        filtroCiudad.cutoffFrequency = 6000f;
        grillos = Fuente("Grillos", SonidosProcedurales.Grillos(), Vector3.zero, 0f);
        pito = SonidosProcedurales.Pito();
        proximoPito = Time.time + Random.Range(5f, 12f);
    }

    AudioSource Fuente(string nombre, AudioClip clip, Vector3 posicion, float espacial)
    {
        var go = new GameObject("Sonido_" + nombre);
        go.transform.SetParent(transform, false);
        go.transform.position = posicion;
        var fuente = go.AddComponent<AudioSource>();
        fuente.clip = clip;
        fuente.loop = true;
        fuente.spatialBlend = espacial;
        fuente.volume = 0f;
        fuente.Play();
        return fuente;
    }

    void Update()
    {
        if (cabeza == null || musica == null)
            return;
        float dt = Time.deltaTime;
        Vector3 p = cabeza.position;
        bool adentro = p.x > zonaAdentro.x && p.x < zonaAdentro.y && p.z > zonaAdentro.z && p.z < zonaAdentro.w;
        bool noche = diaNoche != null && diaNoche.noche;

        musica.volume = Mathf.MoveTowards(musica.volume, adentro ? volumenMusica : volumenMusica * 0.25f, dt * 0.5f);
        ciudad.volume = Mathf.MoveTowards(ciudad.volume, adentro ? volumenCiudad * 0.35f : volumenCiudad, dt * 0.5f);
        filtroCiudad.cutoffFrequency = Mathf.MoveTowards(filtroCiudad.cutoffFrequency, adentro ? 900f : 6000f, dt * 8000f);
        float grillosObjetivo = noche ? (adentro ? volumenGrillos * 0.15f : volumenGrillos) : 0f;
        grillos.volume = Mathf.MoveTowards(grillos.volume, grillosObjetivo, dt * 0.5f);

        if (Time.time >= proximoPito)
        {
            proximoPito = Time.time + Random.Range(7f, 20f);
            Vector3 donde = new Vector3(Random.Range(-40f, 40f), 0.8f, Random.value < 0.5f ? -10f : -14f);
            AudioSource.PlayClipAtPoint(pito, donde, adentro ? volumenPitos * 0.3f : volumenPitos);
        }
    }
}

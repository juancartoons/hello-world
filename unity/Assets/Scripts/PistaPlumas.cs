using UnityEngine;

// Pista para encontrar al personaje: plumitas rojas que caen despacio cerca de donde está escondido.
// Mientras más cerca estás, más plumas caen. Solo mientras se está buscando.
[RequireComponent(typeof(PersonajeEncontrable))]
public class PistaPlumas : MonoBehaviour
{
    [Tooltip("Material para las plumas (lo pone el menú de FarmaciaVR)")]
    public Material materialPluma;
    [Tooltip("Forma de la pluma (la pone el menú de FarmaciaVR)")]
    public Mesh mallaPluma;
    public float plumasPorSegundoLejos = 0.4f;
    public float plumasPorSegundoCerca = 3f;
    public float distanciaCerca = 1.5f;
    public float distanciaLejos = 8f;
    [Tooltip("Altura (metros) sobre el personaje desde donde caen las plumas")]
    public float alturaPlumas = 0.6f;

    PersonajeEncontrable personaje;
    Transform cabeza;
    ParticleSystem sistema;
    ParticleSystem.EmissionModule emision;

    void Start()
    {
        personaje = GetComponent<PersonajeEncontrable>();
        var rig = FindFirstObjectByType<OVRCameraRig>();
        cabeza = rig != null ? rig.centerEyeAnchor : Camera.main != null ? Camera.main.transform : null;

        // El sistema de partículas va suelto (no hijo del personaje) para que no herede su escala.
        var go = new GameObject("Plumas_" + name);
        sistema = go.AddComponent<ParticleSystem>();
        sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = sistema.main;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.065f);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.012f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.13f, 0.13f), new Color(1f, 0.45f, 0.25f));
        main.maxParticles = 60;

        emision = sistema.emission;
        emision.rateOverTime = 0f;

        var forma = sistema.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.3f;

        var ruido = sistema.noise;
        ruido.enabled = true;
        ruido.strength = 0.12f;
        ruido.frequency = 0.4f;
        ruido.scrollSpeed = 0.2f;

        var giro = sistema.rotationOverLifetime;
        giro.enabled = true;
        giro.separateAxes = true;
        giro.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        giro.y = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        giro.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

        var tamano = sistema.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0.8f, 1f, 1f, 0f));

        var render = go.GetComponent<ParticleSystemRenderer>();
        if (materialPluma != null)
            render.sharedMaterial = materialPluma;
        if (mallaPluma != null)
        {
            render.renderMode = ParticleSystemRenderMode.Mesh;
            render.mesh = mallaPluma;
            render.alignment = ParticleSystemRenderSpace.World;
        }

        sistema.Play();
    }

    void LateUpdate()
    {
        if (sistema == null)
            return;
        sistema.transform.position = transform.position + Vector3.up * alturaPlumas;

        float tasa = 0f;
        if (personaje.Activo && cabeza != null)
        {
            float d = Vector3.Distance(cabeza.position, transform.position);
            float cercania = 1f - Mathf.InverseLerp(distanciaCerca, distanciaLejos, d);
            tasa = Mathf.Lerp(plumasPorSegundoLejos, plumasPorSegundoCerca, cercania);
        }
        emision.rateOverTime = tasa;
    }

    void OnDestroy()
    {
        if (sistema != null)
            Destroy(sistema.gameObject);
    }
}

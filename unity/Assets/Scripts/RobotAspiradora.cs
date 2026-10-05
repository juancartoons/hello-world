using UnityEngine;
using Random = UnityEngine.Random;

// Robot aspiradora que recorre solo el piso brillante: avanza, y cuando encuentra una góndola, una pared,
// el mostrador, al jugador o un peluche, gira y sigue por otro lado. Sus cepillos giran y hace un zumbido suave.
// También se ve reflejado en el piso.
public class RobotAspiradora : MonoBehaviour
{
    public float velocidad = 0.22f;
    [Tooltip("Grados por segundo al girar")]
    public float velocidadGiro = 110f;
    [Tooltip("Zona donde puede andar (x mínimo, x máximo, z mínimo, z máximo)")]
    public Vector4 zona = new Vector4(-4.6f, 4.6f, -4.6f, 7.5f);
    public Mesh mallaCepillo;
    public Material materialCepillo;
    [Tooltip("Material del reflejo en el piso (FarmaciaVR/Reflejo)")]
    public Material materialReflejo;
    [Range(0f, 1f)] public float volumen = 0.25f;

    const float radio = 0.17f;
    Transform cabeza, cepilloIzq, cepilloDer, reflejo;
    Transform[] peluches = new Transform[0];
    bool girando;
    float rumbo, proximoCambio;

    void Start()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        cabeza = rig != null ? rig.centerEyeAnchor : null;
        rumbo = transform.eulerAngles.y;

        cepilloIzq = CrearCepillo(-1f);
        cepilloDer = CrearCepillo(1f);

        // Copia "de cabeza" para que se vea reflejado en el piso brillante
        var filtro = GetComponent<MeshFilter>();
        if (materialReflejo != null && filtro != null)
        {
            var go = new GameObject("Reflejo_" + name);
            go.AddComponent<MeshFilter>().sharedMesh = filtro.sharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = materialReflejo;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            reflejo = go.transform;
        }

        var manager = FindFirstObjectByType<JuegoManager>();
        if (manager != null && manager.senuelos != null)
        {
            peluches = new Transform[manager.senuelos.childCount];
            for (int i = 0; i < peluches.Length; i++)
                peluches[i] = manager.senuelos.GetChild(i);
        }

        var fuente = gameObject.AddComponent<AudioSource>();
        fuente.clip = SonidosProcedurales.Zumbido();
        fuente.loop = true;
        fuente.spatialBlend = 1f;
        fuente.minDistance = 0.5f;
        fuente.maxDistance = 6f;
        fuente.rolloffMode = AudioRolloffMode.Linear;
        fuente.volume = volumen;
        fuente.Play();
        proximoCambio = Time.time + Random.Range(6f, 12f);
    }

    Transform CrearCepillo(float lado)
    {
        if (mallaCepillo == null)
            return null;
        var go = new GameObject(lado < 0 ? "CepilloIzquierdo" : "CepilloDerecho");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(lado * 0.11f, 0.006f, 0.1f);
        go.AddComponent<MeshFilter>().sharedMesh = mallaCepillo;
        go.AddComponent<MeshRenderer>().sharedMaterial = materialCepillo;
        return go.transform;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (cepilloIzq != null) cepilloIzq.Rotate(0f, 600f * dt, 0f, Space.Self);
        if (cepilloDer != null) cepilloDer.Rotate(0f, -600f * dt, 0f, Space.Self);

        if (girando)
        {
            float actual = Mathf.MoveTowardsAngle(transform.eulerAngles.y, rumbo, velocidadGiro * dt);
            transform.rotation = Quaternion.Euler(0f, actual, 0f);
            if (Mathf.Abs(Mathf.DeltaAngle(actual, rumbo)) < 0.5f)
                girando = false;
        }
        else
        {
            Vector3 adelante = transform.forward;
            float paso = velocidad * dt;
            if (HayObstaculo(adelante, paso + 0.12f))
            {
                Girar(Random.Range(110f, 250f));
            }
            else
            {
                transform.position += adelante * paso;
                if (Time.time >= proximoCambio)
                {
                    proximoCambio = Time.time + Random.Range(6f, 12f);
                    Girar(Random.Range(-35f, 35f)); // de vez en cuando cambia un poco el rumbo
                }
            }
        }
    }

    void LateUpdate()
    {
        if (reflejo == null)
            return;
        Vector3 p = transform.position;
        reflejo.SetPositionAndRotation(new Vector3(p.x, -p.y, p.z), transform.rotation);
        reflejo.localScale = new Vector3(1f, -1f, 1f);
    }

    void Girar(float grados)
    {
        rumbo = transform.eulerAngles.y + grados;
        girando = true;
    }

    bool HayObstaculo(Vector3 dir, float distancia)
    {
        Vector3 p = transform.position;
        Vector3 siguiente = p + dir * (distancia + radio);
        if (siguiente.x < zona.x || siguiente.x > zona.y || siguiente.z < zona.z || siguiente.z > zona.w)
            return true;

        // El jugador (sus pies, debajo de la cabeza) y los peluches del piso
        if (cabeza != null)
        {
            Vector3 pies = new Vector3(cabeza.position.x, p.y, cabeza.position.z);
            if (Vector3.Distance(siguiente, pies) < 0.45f)
                return true;
        }
        foreach (var peluche in peluches)
            if (peluche != null && peluche.gameObject.activeInHierarchy && peluche.position.y < 0.2f
                && Vector3.Distance(siguiente, new Vector3(peluche.position.x, p.y, peluche.position.z)) < 0.25f)
                return true;

        // Muebles, paredes y el pájaro
        Vector3 origen = p + Vector3.up * 0.2f;
        foreach (var hit in Physics.SphereCastAll(origen, 0.15f, dir, distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance <= 0f || hit.collider.transform.IsChildOf(transform))
                continue;
            if (hit.collider.GetComponentInParent<PuntoTeletransporte>() != null)
                continue;
            return true;
        }
        return false;
    }

    void OnDestroy()
    {
        if (reflejo != null)
            Destroy(reflejo.gameObject);
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Botón 3D que se presiona tocándolo con la punta del dedo índice (o con el control).
// Necesita un Collider (por ejemplo, el BoxCollider de un cubo).
[RequireComponent(typeof(Collider))]
public class BotonTocable : MonoBehaviour
{
    public UnityEvent alTocar = new UnityEvent();
    [Tooltip("Distancia (metros) entre la punta del dedo y el botón para presionarlo")]
    public float distancia = 0.025f;
    [Tooltip("Segundos de espera entre toques")]
    public float pausa = 0.8f;

    OVRCameraRig rig;
    OVRSkeleton esqIzq, esqDer;
    Collider miCollider;
    Vector3 escalaOriginal;
    float bloqueadoHasta;

    void Start()
    {
        rig = FindFirstObjectByType<OVRCameraRig>();
        miCollider = GetComponent<Collider>();
        escalaOriginal = transform.localScale;
    }

    void Update()
    {
        if (rig == null || Time.time < bloqueadoHasta)
            return;
        if (esqIzq == null) esqIzq = ManosUtil.BuscarEnAncla<OVRSkeleton>(rig.leftHandAnchor);
        if (esqDer == null) esqDer = ManosUtil.BuscarEnAncla<OVRSkeleton>(rig.rightHandAnchor);

        bool tocado = Toca(ManosUtil.Hueso(esqIzq, "IndexTip"), distancia)
                      || Toca(ManosUtil.Hueso(esqDer, "IndexTip"), distancia)
                      || TocaControl(rig.leftControllerAnchor, OVRInput.Controller.LTouch)
                      || TocaControl(rig.rightControllerAnchor, OVRInput.Controller.RTouch);
        if (!tocado)
            return;

        bloqueadoHasta = Time.time + pausa;
        StartCoroutine(Hundir());
        alTocar.Invoke();
    }

    bool Toca(Transform punta, float d)
    {
        return punta != null && Vector3.Distance(miCollider.ClosestPoint(punta.position), punta.position) < d;
    }

    bool TocaControl(Transform ancla, OVRInput.Controller control)
    {
        return ancla != null && (OVRInput.GetActiveController() & control) != 0 && Toca(ancla, 0.05f);
    }

    // Pequeña animación de "presionado".
    IEnumerator Hundir()
    {
        transform.localScale = new Vector3(escalaOriginal.x * 0.95f, escalaOriginal.y * 0.95f, escalaOriginal.z * 0.5f);
        yield return new WaitForSeconds(0.15f);
        transform.localScale = escalaOriginal;
    }
}

using System;
using UnityEngine;

// Detecta una palmada (aplauso): las dos palmas se juntan rápido.
// Con controles, el botón A o X también cuenta como palmada.
public class DetectorPalmadas : MonoBehaviour
{
    [Tooltip("Distancia (metros) entre las palmas para contar el choque")]
    public float distanciaContacto = 0.13f;
    [Tooltip("Velocidad mínima (m/s) con la que se juntan las manos")]
    public float velocidadMinima = 0.4f;
    [Tooltip("Segundos de espera entre palmadas")]
    public float pausa = 1f;

    public event Action AlAplaudir;

    OVRCameraRig rig;
    OVRHand manoIzq, manoDer;
    OVRSkeleton esqIzq, esqDer;
    float distanciaAnterior;
    bool hayAnterior;
    float bloqueadoHasta;

    void Start()
    {
        rig = FindFirstObjectByType<OVRCameraRig>();
    }

    void Update()
    {
        if (rig == null)
            return;

        if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
        {
            Disparar();
            return;
        }

        if (manoIzq == null) manoIzq = ManosUtil.BuscarEnAncla<OVRHand>(rig.leftHandAnchor);
        if (manoDer == null) manoDer = ManosUtil.BuscarEnAncla<OVRHand>(rig.rightHandAnchor);
        if (esqIzq == null) esqIzq = ManosUtil.BuscarEnAncla<OVRSkeleton>(rig.leftHandAnchor);
        if (esqDer == null) esqDer = ManosUtil.BuscarEnAncla<OVRSkeleton>(rig.rightHandAnchor);

        if (manoIzq == null || manoDer == null || !manoIzq.IsTracked || !manoDer.IsTracked)
        {
            hayAnterior = false;
            return;
        }

        var pi = ManosUtil.LeerPalma(esqIzq, true);
        var pd = ManosUtil.LeerPalma(esqDer, false);
        Vector3 a = pi.valida ? pi.centro : rig.leftHandAnchor.position;
        Vector3 b = pd.valida ? pd.centro : rig.rightHandAnchor.position;
        float distancia = Vector3.Distance(a, b);

        if (hayAnterior && Time.deltaTime > 0f)
        {
            float velocidadCierre = (distanciaAnterior - distancia) / Time.deltaTime;
            if (distancia < distanciaContacto && velocidadCierre > velocidadMinima)
                Disparar();
        }
        distanciaAnterior = distancia;
        hayAnterior = true;
    }

    void Disparar()
    {
        if (Time.time < bloqueadoHasta)
            return;
        bloqueadoHasta = Time.time + pausa;
        AlAplaudir?.Invoke();
    }
}

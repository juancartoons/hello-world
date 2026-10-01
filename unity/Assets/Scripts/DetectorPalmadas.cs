using System;
using UnityEngine;

// Detecta una palmada (aplauso): las dos palmas, mirándose entre sí, se juntan rápido.
// Ignora los "saltos" del seguimiento de manos (cuando una mano tapa a la otra), que causaban palmadas falsas.
// Con controles, el botón A o X también cuenta como palmada.
public class DetectorPalmadas : MonoBehaviour
{
    [Tooltip("Distancia (metros) entre las palmas para contar el choque")]
    public float distanciaContacto = 0.13f;
    [Tooltip("Velocidad mínima (m/s) con la que se juntan las manos")]
    public float velocidadMinima = 0.7f;
    [Tooltip("Si la distancia entre manos cambia más que esto en un solo cuadro, es un error del seguimiento y no cuenta")]
    public float saltoMaximo = 0.2f;
    [Tooltip("Segundos de espera entre palmadas")]
    public float pausa = 0.35f;

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
        if (!pi.valida || !pd.valida)
        {
            hayAnterior = false;
            return;
        }
        float distancia = Vector3.Distance(pi.centro, pd.centro);
        bool palmasEnfrentadas = Vector3.Dot(pi.normal, pd.normal) < -0.3f;

        if (hayAnterior && Time.deltaTime > 0f)
        {
            float cambio = distanciaAnterior - distancia;
            float velocidadCierre = cambio / Time.deltaTime;
            if (Mathf.Abs(cambio) < saltoMaximo && palmasEnfrentadas
                && distancia < distanciaContacto && velocidadCierre > velocidadMinima)
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

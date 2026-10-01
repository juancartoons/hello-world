using System.Collections;
using UnityEngine;

// Animación sencilla del pájaro: al ser encontrado aletea y levanta las alas de alegría.
public class AnimacionPajaro : MonoBehaviour
{
    public Transform alaIzquierda, alaDerecha;
    [Tooltip("Ángulo (grados) de las alas levantadas")]
    public float anguloArriba = 140f;
    [Tooltip("Ángulo de las alas en reposo")]
    public float anguloReposo = 10f;

    Quaternion rotacionIzq, rotacionDer;
    Coroutine rutina;

    void Awake()
    {
        if (alaIzquierda != null) rotacionIzq = alaIzquierda.localRotation;
        if (alaDerecha != null) rotacionDer = alaDerecha.localRotation;
        PonerAngulo(anguloReposo);
    }

    public void Celebrar()
    {
        if (rutina != null)
            StopCoroutine(rutina);
        rutina = StartCoroutine(Alegria());
    }

    public void Reposo()
    {
        if (rutina != null)
            StopCoroutine(rutina);
        rutina = null;
        PonerAngulo(anguloReposo);
    }

    IEnumerator Alegria()
    {
        // Aletea rápido...
        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            PonerAngulo(Mathf.Lerp(anguloReposo, anguloArriba, 0.55f + 0.45f * Mathf.Sin(t * 22f)));
            yield return null;
        }
        // ...deja las alas arriba un momento...
        PonerAngulo(anguloArriba);
        yield return new WaitForSeconds(1.6f);
        // ...y las baja despacio.
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            PonerAngulo(Mathf.Lerp(anguloArriba, anguloReposo + 25f, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }
        rutina = null;
    }

    // Las alas cuelgan hacia abajo desde el hombro; girarlas en Z las abre hacia los lados y arriba.
    void PonerAngulo(float angulo)
    {
        if (alaIzquierda != null) alaIzquierda.localRotation = rotacionIzq * Quaternion.Euler(0f, 0f, -angulo);
        if (alaDerecha != null) alaDerecha.localRotation = rotacionDer * Quaternion.Euler(0f, 0f, angulo);
    }
}

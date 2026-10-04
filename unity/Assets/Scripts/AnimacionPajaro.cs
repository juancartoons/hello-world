using System.Collections;
using UnityEngine;

// Animación sencilla del pájaro: al ser encontrado aletea de alegría y sigue aleteando sin parar
// mientras lo tienes en las manos (hasta que empieza otra partida).
public class AnimacionPajaro : MonoBehaviour
{
    public Transform alaIzquierda, alaDerecha;
    [Tooltip("Ángulo (grados) de las alas levantadas")]
    public float anguloArriba = 140f;
    [Tooltip("Ángulo de las alas en reposo")]
    public float anguloReposo = 10f;
    [Tooltip("Qué tan rápido aletea en la mano (más alto = más rápido)")]
    public float velocidadAleteo = 13f;

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
        // Primero aletea rápido y grande, de alegría...
        float t = 0f;
        for (; t < 1.2f; t += Time.deltaTime)
        {
            PonerAngulo(Mathf.Lerp(anguloReposo, anguloArriba, 0.55f + 0.45f * Mathf.Sin(t * 22f)));
            yield return null;
        }
        // ...y luego sigue aleteando sin parar (más tranquilo) mientras está contigo.
        for (float f = 0f; ; f += Time.deltaTime)
        {
            float ola = 0.5f + 0.5f * Mathf.Sin(f * velocidadAleteo);
            PonerAngulo(Mathf.Lerp(anguloReposo + 20f, anguloArriba - 15f, ola));
            yield return null;
        }
    }

    // Las alas cuelgan hacia abajo desde el hombro; girarlas en Z las abre hacia los lados y arriba.
    void PonerAngulo(float angulo)
    {
        if (alaIzquierda != null) alaIzquierda.localRotation = rotacionIzq * Quaternion.Euler(0f, 0f, -angulo);
        if (alaDerecha != null) alaDerecha.localRotation = rotacionDer * Quaternion.Euler(0f, 0f, angulo);
    }
}

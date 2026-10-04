using UnityEngine;

// Poses de una mano "de mentira" para las manos guía del tutorial.
// Devuelve las 21 articulaciones en el mismo orden que GrabadorProceso / ManoVideo:
// 0 muñeca, 1-4 pulgar, 5-8 índice, 9-12 medio, 13-16 anular, 17-20 meñique.
public static class PoseMano
{
    // Nudillos (base de cada dedo) respecto a la muñeca: (hacia el pulgar, hacia los dedos, hacia la palma).
    static readonly Vector3[] Nudillo =
    {
        new Vector3(0.022f, 0.088f, 0f),
        new Vector3(0.002f, 0.090f, 0f),
        new Vector3(-0.016f, 0.084f, 0f),
        new Vector3(-0.031f, 0.075f, 0f),
    };
    // Largo de las 3 falanges de cada dedo (índice, medio, anular, meñique).
    static readonly float[,] Falange =
    {
        { 0.040f, 0.024f, 0.021f },
        { 0.045f, 0.028f, 0.022f },
        { 0.042f, 0.027f, 0.021f },
        { 0.033f, 0.020f, 0.019f },
    };
    // Qué tanto se abren los dedos hacia los lados (radianes, con la mano abierta).
    static readonly float[] Abanico = { 0.07f, 0f, -0.06f, -0.13f };
    // Ángulo máximo de cada articulación al cerrar del todo (grados).
    static readonly float[] Doblez = { 78f, 98f, 68f };

    // pinza: 0 a 1 (pulgar con índice: el "OK" 👌, los otros dedos estirados) · puno: 0 a 1 (puño)
    // apuntar: 0 a 1 (índice estirado, los demás doblados) · pinzaMedio: pulgar con el dedo medio
    // pulgarFuera: con el puño, el pulgar estirado hacia afuera (la flecha de deshacer).
    // muneca = dónde va la muñeca; dedos = hacia dónde apuntan los dedos; palma = hacia dónde mira la palma.
    public static void Calcular(Vector3[] a, bool izquierda, Vector3 muneca, Vector3 dedos, Vector3 palma,
                                float pinza, float puno, float apuntar, float pinzaMedio = 0f, float pulgarFuera = 0f)
    {
        if (a == null || a.Length < 21)
            return;
        Vector3 F = dedos.sqrMagnitude > 1e-8f ? dedos.normalized : Vector3.forward;
        Vector3 N = palma - F * Vector3.Dot(palma, F);
        N = N.sqrMagnitude > 1e-8f ? N.normalized : Vector3.Cross(F, Vector3.right).normalized;
        // Lado del pulgar (depende de la mano).
        Vector3 S = izquierda ? Vector3.Cross(F, N) : Vector3.Cross(N, F);

        pinza = Mathf.Clamp01(pinza);
        puno = Mathf.Clamp01(puno);
        apuntar = Mathf.Clamp01(apuntar);
        pinzaMedio = Mathf.Clamp01(pinzaMedio);
        pulgarFuera = Mathf.Clamp01(pulgarFuera);

        // Qué tan doblado está cada dedo (0 = estirado, 1 = cerrado).
        var curva = new float[4];
        for (int d = 0; d < 4; d++)
        {
            float c = 0.12f;                                                  // mano relajada
            c = Mathf.Lerp(c, d == 0 ? 0.6f : 0.08f + d * 0.04f, pinza);       // OK: los otros tres bien estirados
            c = Mathf.Lerp(c, d == 1 ? 0.64f : d == 0 ? 0.08f : 0.2f, pinzaMedio);
            c = Mathf.Lerp(c, 1f, puno);
            c = Mathf.Lerp(c, d == 0 ? 0.04f : 0.95f, apuntar);
            curva[d] = c;
        }

        a[0] = muneca;
        // Dedos: cada articulación dobla el dedo hacia la palma.
        for (int d = 0; d < 4; d++)
        {
            Vector3 n = Nudillo[d];
            Vector3 p = muneca + S * n.x + F * n.y + N * n.z;
            float abre = Abanico[d] * (1f - curva[d]);
            Vector3 dir0 = F * Mathf.Cos(abre) + S * Mathf.Sin(abre);
            int baseIndice = 5 + d * 4;
            a[baseIndice] = p;
            float angulo = 0f;
            for (int k = 0; k < 3; k++)
            {
                angulo += Doblez[k] * curva[d] * Mathf.Deg2Rad;
                Vector3 dir = dir0 * Mathf.Cos(angulo) + N * Mathf.Sin(angulo);
                p += dir * Falange[d, k];
                a[baseIndice + 1 + k] = p;
            }
        }

        // Pulgar: base fija y la punta va hacia donde le toca (abierto, tocando un dedo, sobre el puño o afuera).
        Vector3 base1 = muneca + S * 0.022f + F * 0.026f + N * 0.012f;
        Vector3 base2 = base1 + (S * 0.75f + F * 0.55f + N * 0.35f).normalized * 0.038f;
        Vector3 abierto = base2 + (S * 0.6f + F * 0.75f + N * 0.2f).normalized * 0.06f;
        Vector3 conIndice = a[8] + S * 0.004f;
        Vector3 conMedio = a[12] + S * 0.004f;
        Vector3 sobrePuno = Vector3.Lerp(a[6], a[10], 0.45f) + N * 0.012f;
        Vector3 afuera = base2 + (S * 0.9f + F * 0.3f - N * 0.15f).normalized * 0.062f;
        Vector3 punta = Vector3.Lerp(abierto, conIndice, pinza);
        punta = Vector3.Lerp(punta, conMedio, pinzaMedio);
        punta = Vector3.Lerp(punta, sobrePuno, Mathf.Max(puno, apuntar) * (1f - pulgarFuera));
        punta = Vector3.Lerp(punta, afuera, pulgarFuera);
        Vector3 hacia = punta - base2;
        Vector3 fuera = (S * 0.6f - N * 0.8f).normalized; // el nudillo del pulgar se curva hacia afuera
        float largo = hacia.magnitude;
        a[1] = base1;
        a[2] = base2;
        a[3] = base2 + hacia * 0.55f + fuera * Mathf.Clamp(0.06f - largo, 0f, 0.03f) * 0.4f;
        a[4] = punta;
    }

    // Igual, pero mueve la mano para que la punta del índice quede justo en "punta".
    public static void ConIndiceEn(Vector3[] a, bool izquierda, Vector3 punta, Vector3 dedos, Vector3 palma,
                                   float pinza, float puno, float apuntar)
    {
        Calcular(a, izquierda, Vector3.zero, dedos, palma, pinza, puno, apuntar, 0f, 0f);
        Vector3 mover = punta - a[8];
        for (int i = 0; i < 21; i++)
            a[i] += mover;
    }
}

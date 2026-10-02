using System.Collections.Generic;
using UnityEngine;

// Quitar y agregar nodos en las listas de una línea. Se usa igual para la línea que ves
// y para su forma guardada en cada clave de la animación, así el morph sigue funcionando.
public static class NodosUtil
{
    static bool Tiene<T>(List<T> lista, int n)
    {
        return lista != null && lista.Count == n;
    }

    public static void Quitar(List<Vector3> nodos, List<Vector3> entrada, List<Vector3> salida,
                              List<bool> manual, List<float> grosor, int i)
    {
        if (nodos == null || i < 0 || i >= nodos.Count)
            return;
        int n = nodos.Count;
        nodos.RemoveAt(i);
        if (Tiene(entrada, n)) entrada.RemoveAt(i);
        if (Tiene(salida, n)) salida.RemoveAt(i);
        if (Tiene(manual, n)) manual.RemoveAt(i);
        if (Tiene(grosor, n)) grosor.RemoveAt(i);
    }

    // Parte la curva del tramo "segmento" en el punto t (0..1) sin cambiar su forma (de Casteljau).
    public static void Insertar(List<Vector3> nodos, List<Vector3> entrada, List<Vector3> salida,
                                List<bool> manual, List<float> grosor, bool cerrado, int segmento, float t)
    {
        if (nodos == null)
            return;
        int n = nodos.Count;
        if (n < 2)
            return;
        int segmentos = cerrado ? n : n - 1;
        if (segmento < 0 || segmento >= segmentos)
            return;
        t = Mathf.Clamp(t, 0.02f, 0.98f);
        bool asas = Tiene(entrada, n) && Tiene(salida, n);
        int a = segmento;
        int b = (segmento + 1) % n;

        Vector3 p0 = nodos[a];
        Vector3 p3 = nodos[b];
        Vector3 p1 = p0 + (asas ? salida[a] : (p3 - p0) / 3f);
        Vector3 p2 = p3 + (asas ? entrada[b] : (p0 - p3) / 3f);
        Vector3 q0 = Vector3.Lerp(p0, p1, t);
        Vector3 q1 = Vector3.Lerp(p1, p2, t);
        Vector3 q2 = Vector3.Lerp(p2, p3, t);
        Vector3 r0 = Vector3.Lerp(q0, q1, t);
        Vector3 r1 = Vector3.Lerp(q1, q2, t);
        Vector3 nuevo = Vector3.Lerp(r0, r1, t);

        // Primero se ajustan los vecinos (antes de insertar, para no correr los índices).
        if (asas)
        {
            salida[a] = q0 - p0;
            entrada[b] = q2 - p3;
        }
        bool hayManual = Tiene(manual, n);
        if (hayManual && asas)
        {
            manual[a] = true;
            manual[b] = true;
        }
        float g = Tiene(grosor, n) ? Mathf.Lerp(grosor[a], grosor[b], t) : 1f;

        int pos = a + 1;
        nodos.Insert(pos, nuevo);
        if (asas)
        {
            entrada.Insert(pos, r0 - nuevo);
            salida.Insert(pos, r1 - nuevo);
        }
        if (hayManual)
            manual.Insert(pos, asas);
        if (Tiene(grosor, n))
            grosor.Insert(pos, g);
    }
}

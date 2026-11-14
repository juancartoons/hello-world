using UnityEngine;

// v67: LA ZONA DE TRABAJO: dónde has estado dibujando o editando (las últimas muestras de tu dedo mientras dibujas,
// mueves nodos, cambias grosores o borras). Los paneles (Propiedades, Ajustes del lápiz, Plastilina) se abren AL LADO
// de esa zona (no según hacia dónde miras), así nunca tapan lo que estás haciendo.
public static class ZonaTrabajo
{
    const int Capacidad = 60;          // ~10 movimientos (una muestra cada 0,15 s)
    const float Vigencia = 300f;       // segundos: después de esto ya no cuenta
    static readonly Vector3[] puntos = new Vector3[Capacidad];
    static readonly float[] tiempos = new float[Capacidad];
    static int siguiente, cuantas;
    static float ultima = -10f;

    public static void Anotar(Vector3 mundo)
    {
        if (Time.time - ultima < 0.15f)
            return;
        ultima = Time.time;
        puntos[siguiente] = mundo;
        tiempos[siguiente] = Time.time;
        siguiente = (siguiente + 1) % Capacidad;
        cuantas = Mathf.Min(Capacidad, cuantas + 1);
    }

    // El centro de la zona y su "radio" (qué tan grande es). false si no has trabajado hace poco.
    public static bool Zona(out Vector3 centro, out float radio)
    {
        centro = Vector3.zero;
        radio = 0f;
        int n = 0;
        for (int i = 0; i < cuantas; i++)
            if (Time.time - tiempos[i] < Vigencia)
            {
                centro += puntos[i];
                n++;
            }
        if (n < 3)
            return false;
        centro /= n;
        for (int i = 0; i < cuantas; i++)
            if (Time.time - tiempos[i] < Vigencia)
                radio = Mathf.Max(radio, Vector3.Distance(centro, puntos[i]));
        radio = Mathf.Clamp(radio, 0.05f, 0.45f);
        return true;
    }

    // Dónde va un panel de "medioAncho" metros: a la derecha (o a la izquierda) de tu zona de trabajo, tal como tú la
    // ves, mirándote. Sin zona reciente: a un lado de hacia donde miras.
    public static Vector3 PosicionAlLado(Transform cab, bool aLaDerecha, float medioAncho)
    {
        Vector3 centro;
        float radio;
        float signo = aLaDerecha ? 1f : -1f;
        if (Zona(out centro, out radio))
        {
            Vector3 hacia = Vector3.ProjectOnPlane(centro - cab.position, Vector3.up);
            float dist = hacia.magnitude;
            if (dist > 0.1f)
            {
                Vector3 dir = hacia / dist;
                Vector3 lado = Vector3.Cross(Vector3.up, dir).normalized * signo;
                float d = Mathf.Clamp(dist, 0.35f, 0.9f);
                Vector3 pos = cab.position + dir * d + lado * (radio + medioAncho + 0.05f);
                pos.y = Mathf.Clamp(centro.y, cab.position.y - 0.45f, cab.position.y + 0.1f);
                return pos;
            }
        }
        Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized * signo;
        return cab.position + adelante * 0.36f + derecha * 0.44f + Vector3.up * 0.06f;
    }
}

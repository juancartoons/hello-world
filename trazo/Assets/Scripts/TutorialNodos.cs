using UnityEngine;

// Tutorial de NODOS (sección 3 del panel "?"). No es parte del tutorial de presentación.
//  1 · Tocar y soltar: el nodo se pega al dedo; se suelta al abrir la mano izquierda.
//  2 · Nodo nuevo: tocar la línea y quedarse quieto medio segundo.
//  3 · Lazo: un círculo alrededor de varios nodos = se mueven juntos.
//  4 · Plastilina: índice + medio juntos = los vecinos siguen suave.
public class TutorialNodos : TarjetaTutorial
{
    const int CantidadNodos = 7;
    static readonly Color Azul = new Color(0.15f, 0.45f, 1f);
    static readonly Color Naranja = new Color(1f, 0.5f, 0.1f);
    static readonly Color NaranjaClaro = new Color(1f, 0.78f, 0.5f);
    static readonly Vector3 Subir = new Vector3(0f, 0.032f, 0f);   // cuánto se lleva el nodo en las animaciones
    static readonly Vector3 Descanso = new Vector3(0.125f, -0.045f, -0.004f); // dónde descansa el dedo

    readonly Vector3[] baseNodos = new Vector3[CantidadNodos];
    readonly Vector3[] nodos = new Vector3[CantidadNodos + 1];     // el último es el "nodo nuevo"
    readonly Transform[] bolas = new Transform[CantidadNodos + 1];
    readonly int[] estado = new int[CantidadNodos + 1];            // 0 azul, 1 naranja, 2 naranja clarito
    Material matAzul, matNaranja, matNaranjaClaro;
    Transform dedo2;
    LineRenderer lineaLazo;

    protected override int Paginas => 4;

    protected override string Titulo(int p)
    {
        switch (p)
        {
            case 0: return Tx("1 · Touch and let go", "1 · Tocar y soltar");
            case 1: return Tx("2 · A new node", "2 · Nodo nuevo");
            case 2: return Tx("3 · Lasso: several nodes", "3 · Lazo: varios nodos");
            default: return Tx("4 · Clay: two fingers", "4 · Plastilina: dos dedos");
        }
    }

    protected override string Explicacion(int p)
    {
        switch (p)
        {
            case 0:
                return Tx(
                    "LEFT hand: thumb + MIDDLE finger = the nodes appear. TOUCH a node with your RIGHT index: it sticks to your finger and follows it. To let it go, OPEN your left hand.\nCurves: bring your finger CLOSE to a node (without touching it) and its handles appear; touch the tip of one to bend the line (with a V: only that side). Take the tip into its node = straight corner. What you are about to grab lights up first.",
                    "Mano IZQUIERDA: pulgar + dedo MEDIO = aparecen los nodos. TOCA un nodo con el índice DERECHO: se pega a tu dedo y lo sigue. Para soltarlo, ABRE la mano izquierda.\nCurvas: ACERCA el dedo a un nodo (sin tocarlo) y aparecen sus tiradores; toca la punta de uno para curvar la línea (con la V: solo ese lado). Lleva la punta hasta su nodo = esquina recta. Lo que vas a agarrar se ilumina antes.");
            case 1:
                return Tx(
                    "Touch the line (away from its nodes) and stay STILL for half a second: a new node is born right there, stuck to your finger. Touching another line selects it.",
                    "Toca la línea (lejos de sus nodos) y quédate QUIETO medio segundo: nace un nodo nuevo justo ahí, pegado a tu dedo. Tocar otra línea = elegirla.");
            case 2:
                return Tx(
                    "Start in an EMPTY space and draw a circle around several nodes: they turn orange. Touch one of them and they all move together. Touch it with a V (index + middle stretched) and twist your wrist: they ROTATE. Opening your left hand lets them go.",
                    "Empieza en un espacio VACÍO y dibuja un círculo alrededor de varios nodos: quedan naranjas. Toca uno de ellos y se mueven todos juntos. Tócalo con la V (índice y medio estirados) y gira la muñeca: GIRAN. Al abrir la mano izquierda se sueltan.");
            default:
                return Tx(
                    "Stretch out your INDEX and MIDDLE fingers (together or in a V) with the other fingers folded, and touch a node: its neighbors follow softly, like clay (the light orange ones). One finger = only that node.",
                    "Estira el ÍNDICE y el MEDIO (juntos o en V) con los demás dedos doblados, y toca un nodo: sus vecinos lo siguen suave, como plastilina (los naranja clarito). Un dedo = solo ese nodo.");
        }
    }

    protected override void ArmarExtra()
    {
        matAzul = Copia(tutorial.materialBlanco, Azul);
        matNaranja = Copia(tutorial.materialBlanco, Naranja);
        matNaranjaClaro = Copia(tutorial.materialBlanco, NaranjaClaro);
        // El dedo: gris oscuro (los nodos son azules).
        var matDedo = Copia(tutorial.materialBlanco, new Color(0.25f, 0.25f, 0.3f));
        Pintar(dedo.GetComponent<Renderer>(), matDedo);
        dedo2 = Bolita("Dedo2", matDedo, 0.009f);
        lineaLazo = Linea("Lazo", matNaranja, 0.0022f);
        for (int i = 0; i < CantidadNodos; i++)
        {
            float u = i / (float)(CantidadNodos - 1);
            baseNodos[i] = new Vector3(Mathf.Lerp(-0.11f, 0.11f, u), 0.012f * Mathf.Sin(u * Mathf.PI * 2f) - 0.01f, 0f);
        }
        for (int i = 0; i <= CantidadNodos; i++)
            bolas[i] = Bolita("Nodo" + i, matAzul, 0.009f);
    }

    protected override void Animar(int p, float t)
    {
        SinRelleno();
        lineaGris.positionCount = 0;
        for (int i = 0; i < CantidadNodos; i++)
        {
            nodos[i] = baseNodos[i];
            estado[i] = 0;
        }
        estado[CantidadNodos] = -1; // el nodo nuevo, escondido
        dedo2.gameObject.SetActive(false);
        lineaLazo.positionCount = 0;
        switch (p)
        {
            case 0: AnimarTocar(t); break;
            case 1: AnimarNuevo(t); break;
            case 2: AnimarLazo(t); break;
            default: AnimarPlastilina(t); break;
        }
        PonerNodos();
    }

    // El dedo va de donde descansa al nodo, lo lleva y (al abrir la izquierda) lo suelta y se va.
    Vector3 DedoHacia(Vector3 objetivo, float t, float llega, float sale)
    {
        if (t < llega - 0.6f)
            return Descanso;
        if (t < llega)
            return Vector3.Lerp(Descanso, objetivo, Suave((t - (llega - 0.6f)) / 0.6f));
        if (t < sale)
            return objetivo;
        return Vector3.Lerp(objetivo, Descanso, Suave((t - sale) / 0.6f));
    }

    void AnimarTocar(float t)
    {
        const int k = 3;
        float lleva = Suave((t - 1.0f) / 1.4f);
        nodos[k] = baseNodos[k] + Subir * lleva;
        bool pegado = t >= 1.0f && t < 2.8f;
        estado[k] = pegado ? 1 : 0;
        dedo.gameObject.SetActive(true);
        dedo.localPosition = t < 1.0f ? DedoHacia(baseNodos[k] + new Vector3(0f, 0f, -0.004f), t, 1.0f, 99f)
                                      : DedoHacia(nodos[k] + new Vector3(0f, 0f, -0.004f), t, 1.0f, 2.9f);
    }

    void AnimarNuevo(float t)
    {
        // El punto de la línea entre el nodo 3 y el 4.
        Vector3 enLinea = Vector3.Lerp(baseNodos[3], baseNodos[4], 0.5f) + new Vector3(0f, 0.006f, 0f);
        float lleva = Suave((t - 1.7f) / 1.1f);
        Vector3 nuevo = enLinea + Subir * lleva;
        if (t >= 1.0f && t < 1.5f)
        {
            // Quieto medio segundo: el nodo nuevo va naciendo (crece).
            estado[CantidadNodos] = 2;
            bolas[CantidadNodos].localScale = Vector3.one * 0.009f * Suave((t - 1.0f) / 0.5f);
        }
        else if (t >= 1.5f)
        {
            estado[CantidadNodos] = t < 3.0f ? 1 : 0;
            bolas[CantidadNodos].localScale = Vector3.one * 0.009f;
        }
        nodos[CantidadNodos] = nuevo;
        dedo.gameObject.SetActive(true);
        dedo.localPosition = t < 1.7f ? DedoHacia(enLinea + new Vector3(0f, 0f, -0.004f), t, 1.0f, 99f)
                                      : DedoHacia(nuevo + new Vector3(0f, 0f, -0.004f), t, 1.0f, 3.1f);
    }

    void AnimarLazo(float t)
    {
        // El dedo da una vuelta alrededor de los nodos 2, 3 y 4 (dejando una raya naranja)...
        Vector3 centro = baseNodos[3];
        float vuelta = Mathf.Clamp01((t - 0.3f) / 1.6f);
        forma.Clear();
        for (int s = 0; s <= 40 && s / 40f <= vuelta; s++)
        {
            float a = (-90f + 360f * (s / 40f)) * Mathf.Deg2Rad;
            forma.Add(centro + new Vector3(Mathf.Cos(a) * 0.052f, Mathf.Sin(a) * 0.024f, -0.003f));
        }
        bool elegidos = t >= 1.95f && t < 3.9f;
        if (t < 1.95f)
            PonerLinea(lineaLazo, forma);
        // ...quedan naranjas; toca uno y se mueven los tres juntos; al soltar vuelven a azul.
        float lleva = Suave((t - 2.6f) / 1.0f);
        for (int i = 2; i <= 4; i++)
        {
            nodos[i] = baseNodos[i] + Subir * lleva;
            estado[i] = elegidos ? 1 : 0;
        }
        dedo.gameObject.SetActive(true);
        if (t < 0.3f)
            dedo.localPosition = Vector3.Lerp(Descanso, forma.Count > 0 ? forma[0] : centro, Suave(t / 0.3f));
        else if (t < 1.95f)
            dedo.localPosition = forma.Count > 0 ? forma[forma.Count - 1] : centro;
        else if (t < 2.6f)
            dedo.localPosition = Vector3.Lerp(centro + new Vector3(0f, -0.024f, -0.003f), baseNodos[3] + new Vector3(0f, 0f, -0.004f), Suave((t - 1.95f) / 0.5f));
        else if (t < 3.9f)
            dedo.localPosition = nodos[3] + new Vector3(0f, 0f, -0.004f);
        else
            dedo.localPosition = Vector3.Lerp(nodos[3] + new Vector3(0f, 0f, -0.004f), Descanso, Suave((t - 3.9f) / 0.5f));
    }

    void AnimarPlastilina(float t)
    {
        const int k = 3;
        const float alcance = 0.08f;
        float lleva = Suave((t - 1.0f) / 1.4f);
        bool pegado = t >= 1.0f && t < 2.8f;
        for (int i = 0; i < CantidadNodos; i++)
        {
            float x = Mathf.Abs(baseNodos[i].x - baseNodos[k].x) / alcance;
            float peso = x < 1f ? (1f - x * x) * (1f - x * x) : 0f;
            nodos[i] = baseNodos[i] + Subir * (lleva * peso);
            estado[i] = pegado && peso > 0f ? (i == k ? 1 : 2) : 0;
        }
        // Dos dedos juntos (índice y medio).
        Vector3 objetivo = (t < 1.0f ? baseNodos[k] : nodos[k]) + new Vector3(0f, 0f, -0.004f);
        Vector3 p = DedoHacia(objetivo, t, 1.0f, 2.9f);
        dedo.gameObject.SetActive(true);
        dedo2.gameObject.SetActive(true);
        dedo.localPosition = p + new Vector3(-0.0055f, 0f, 0f);
        dedo2.localPosition = p + new Vector3(0.0055f, 0.002f, 0f);
    }

    // Las bolitas de los nodos (con su color) y la línea que pasa por ellos (curva suave).
    void PonerNodos()
    {
        for (int i = 0; i <= CantidadNodos; i++)
        {
            bool ver = estado[i] >= 0;
            if (bolas[i].gameObject.activeSelf != ver)
                bolas[i].gameObject.SetActive(ver);
            if (!ver)
                continue;
            bolas[i].localPosition = nodos[i] + new Vector3(0f, 0f, -0.002f);
            if (i < CantidadNodos)
                bolas[i].localScale = Vector3.one * (estado[i] == 1 ? 0.012f : 0.009f);
            var m = estado[i] == 1 ? matNaranja : estado[i] == 2 ? matNaranjaClaro : matAzul;
            var r = bolas[i].GetComponent<Renderer>();
            if (r.sharedMaterial != m)
                r.sharedMaterial = m;
        }
        // La línea: por los nodos en orden (el nodo nuevo va entre el 3 y el 4).
        forma.Clear();
        for (int i = 0; i < CantidadNodos; i++)
        {
            forma.Add(nodos[i]);
            if (i == 3 && estado[CantidadNodos] >= 0 && bolas[CantidadNodos].localScale.x > 0.0085f)
                forma.Add(nodos[CantidadNodos]);
        }
        puntos.Clear();
        for (int i = 0; i + 1 < forma.Count; i++)
            for (int s = 0; s < 6; s++)
            {
                Vector3 p0 = forma[Mathf.Max(0, i - 1)], p1 = forma[i], p2 = forma[i + 1], p3 = forma[Mathf.Min(forma.Count - 1, i + 2)];
                float u = s / 6f;
                puntos.Add(0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u));
            }
        puntos.Add(forma[forma.Count - 1]);
        PonerLinea(linea, puntos);
    }
}

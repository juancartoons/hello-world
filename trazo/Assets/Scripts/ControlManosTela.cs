using System.Collections.Generic;
using UnityEngine;

// Puntos de TELA (parte de ControlManos): relieve suave dentro de un relleno.
//  - Modo nodos (izquierda pulgar + medio): toca DENTRO del relleno de la figura elegida (lejos de su línea) y
//    quédate quieto medio segundo = punto de tela nuevo (un rombito morado), pegado al dedo.
//  - Toca un punto de tela = se pega al dedo. Llévalo hacia ti o hacia el fondo (o de lado): la tela de alrededor
//    lo sigue en forma de campana suave (nunca en punta) y el borde se queda pegado a la línea.
//    Se suelta abriendo la mano izquierda, igual que los nodos.
//  - Borrador (puño izquierdo) sobre un punto de tela = se quita (la tela vuelve a su lugar).
// La forma de la tela la calcula Trazo (ArmarMallaTela).
public partial class ControlManos
{
    Trazo telaTrazo;
    int telaIndice = -1;
    Vector3 telaDesfase;
    float proximaTela;
    readonly List<Transform> telasVisibles = new List<Transform>();
    readonly List<Renderer> telasRender = new List<Renderer>();
    Material materialTela;
    static Mesh mallaRombo;

    // El punto de tela más cercano a la punta (a menos de "radio" metros). solo: solo de esa figura.
    bool BuscarTelaCercana(Vector3 punta, float radio, Trazo solo, out Trazo trazo, out int indice)
    {
        trazo = null;
        indice = -1;
        float mejor = radio;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || !t.TieneTela || (solo != null && t != solo))
                continue;
            for (int i = 0; i < t.telaBase.Count; i++)
            {
                float d = Vector3.Distance(punta, dibujo.transform.TransformPoint(t.PosicionTela(i)));
                if (d < mejor)
                {
                    mejor = d;
                    trazo = t;
                    indice = i;
                }
            }
        }
        return trazo != null;
    }

    // ¿El dedo está DENTRO del relleno de "t", lejos de su línea? (ahí se crean los puntos de tela)
    bool TocaSoloRelleno(Trazo t, Vector3 punta)
    {
        if (t == null || !t.relleno)
            return false;
        float escala = dibujo.EscalaMundo;
        float d = t.DistanciaACurva(dibujo.transform.InverseTransformPoint(punta)) * escala;
        return d >= Mathf.Max(0.02f, t.ancho * escala * 0.5f + 0.012f);
    }

    void EmpezarArrastreTela(Trazo t, int i, Vector3 punta)
    {
        dibujo.GuardarParaDeshacer();
        dibujo.Seleccionar(t);
        telaTrazo = t;
        telaIndice = i;
        telaDesfase = t.PosicionTela(i) - dibujo.transform.InverseTransformPoint(punta);
        Burbuja(punta, 1.2f);
    }

    // Un punto de tela nuevo en la figura "t" donde toca el dedo; queda pegado al dedo.
    bool AgregarTelaEn(Trazo t, Vector3 punta)
    {
        if (t == null || !t.relleno)
            return false;
        dibujo.GuardarParaDeshacer();
        int k = t.AgregarTela(dibujo.transform.InverseTransformPoint(punta));
        if (k < 0)
        {
            dibujo.DescartarUltimoDeshacer();
            return false;
        }
        telaTrazo = t;
        telaIndice = k;
        telaDesfase = t.PosicionTela(k) - dibujo.transform.InverseTransformPoint(punta);
        Burbuja(punta, 1.3f);
        dibujo.Mensaje("Punto de tela: llévalo hacia ti o hacia el fondo");
        return true;
    }

    // Cada cuadro mientras está agarrado: la tela sigue al dedo (unas 45 veces por segundo).
    void MoverTelaConDedo(Vector3 punta)
    {
        if (telaTrazo == null || !Dibujo.Editable(telaTrazo) || telaIndice < 0 || telaIndice >= telaTrazo.telaBase.Count)
        {
            SoltarTela();
            return;
        }
        if (Time.time < proximaTela)
            return;
        proximaTela = Time.time + 1f / 45f;
        telaTrazo.MoverTela(telaIndice, dibujo.transform.InverseTransformPoint(punta) + telaDesfase);
    }

    void SoltarTela()
    {
        telaTrazo = null;
        telaIndice = -1;
    }

    // Los rombitos morados de los puntos de tela (en el modo nodos y con el borrador).
    void MostrarTelas(Trazo solo)
    {
        int n = 0;
        if (materialTela == null && materialNodo != null)
            materialTela = ColorMaterial(new Color(0.62f, 0.32f, 0.95f));
        Vector3 cabeza = Cabeza != null ? Cabeza.position : Vector3.zero;
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || !t.TieneTela || (solo != null && t != solo))
                continue;
            for (int i = 0; i < t.telaBase.Count; i++)
            {
                if (n >= telasVisibles.Count)
                {
                    var go = new GameObject("Tela");
                    go.transform.SetParent(transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = MallaRombo();
                    var r = go.AddComponent<MeshRenderer>();
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    telasVisibles.Add(go.transform);
                    telasRender.Add(r);
                }
                var marca = telasVisibles[n];
                if (!marca.gameObject.activeSelf)
                    marca.gameObject.SetActive(true);
                Vector3 pos = dibujo.transform.TransformPoint(t.PosicionTela(i));
                marca.position = pos;
                Vector3 mirar = pos - cabeza;
                if (mirar.sqrMagnitude > 1e-8f)
                    marca.rotation = Quaternion.LookRotation(mirar);
                bool activo = (t == telaTrazo && i == telaIndice) || (hoverTipo == Objetivo.Tela && t == hoverTrazo && i == hoverIndice);
                marca.localScale = Vector3.one * tamanoNodo * (activo ? 1.7f : 1.25f);
                var mat = activo ? materialNodoActivo : materialTela;
                if (mat != null && telasRender[n].sharedMaterial != mat)
                    telasRender[n].sharedMaterial = mat;
                n++;
            }
        }
        OcultarTelasDesde(n);
    }

    void OcultarTelasDesde(int desde)
    {
        for (int i = desde; i < telasVisibles.Count; i++)
            if (telasVisibles[i].gameObject.activeSelf)
                telasVisibles[i].gameObject.SetActive(false);
    }

    // Un rombo plano (se ve por los dos lados). Alto 1.
    static Mesh MallaRombo()
    {
        if (mallaRombo != null)
            return mallaRombo;
        mallaRombo = new Mesh { name = "Rombo" };
        mallaRombo.vertices = new[] { new Vector3(0f, 0.5f, 0f), new Vector3(0.36f, 0f, 0f), new Vector3(0f, -0.5f, 0f), new Vector3(-0.36f, 0f, 0f) };
        mallaRombo.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 };
        mallaRombo.RecalculateBounds();
        return mallaRombo;
    }
}

using System.Collections.Generic;
using UnityEngine;

// Puntos de TELA (parte de ControlManos): relieve suave dentro de un relleno.
//  - Modo nodos (izquierda pulgar + medio) y PELLIZCA el relleno con la derecha (pulgar + índice), en cualquier
//    figura (no hace falta elegirla antes): la tela se agarra justo ahí. Jálala hacia ti o empújala al fondo y
//    abre el pellizco: se queda así. Cada pellizco en otro lugar = otro punto (un rombito morado).
//    Pellizcar cerca de un rombito (a menos de 2 cm) agarra ese mismo.
//  - Para cambiarlo de lugar después: tócalo con el índice, igual que un nodo (se pega al dedo y se suelta
//    abriendo la mano izquierda).
//  - Si lo sueltas casi plano (sin relieve), el punto se quita solo. El borrador (puño izquierdo) también lo quita.
// La tela de alrededor sigue al punto en forma de campana suave (nunca en punta) y el borde se queda pegado a la
// línea. La forma de la tela la calcula Trazo (ArmarMallaTela).
public partial class ControlManos
{
    const float RadioPellizcoTela = 0.02f;   // metros: pellizcar a esta distancia de un rombito agarra ese mismo
    const float TelaPlana = 0.004f;          // metros: soltado con menos relieve que esto, el punto se quita

    Trazo telaTrazo;
    int telaIndice = -1;
    Vector3 telaDesfase;
    float proximaTela, avisoTela = -10f;
    bool telaConPellizco, telaNueva;
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
        telaConPellizco = false;
        telaNueva = false;
        telaDesfase = t.PosicionTela(i) - dibujo.transform.InverseTransformPoint(punta);
        Burbuja(punta, 1.2f);
    }

    // Un punto de tela nuevo en la figura "t" donde está la pinza; queda agarrado.
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
        dibujo.Seleccionar(t);
        telaTrazo = t;
        telaIndice = k;
        telaConPellizco = false;
        telaNueva = true;
        telaDesfase = t.PosicionTela(k) - dibujo.transform.InverseTransformPoint(punta);
        Burbuja(punta, 1.3f);
        dibujo.Mensaje("Tela: jálala hacia ti o empújala al fondo, y abre el pellizco");
        return true;
    }

    // Modo nodos: el pellizco derecho (pulgar + índice) acaba de cerrarse. Dentro de un relleno = agarrar la tela
    // ahí (un punto nuevo, o el rombito que ya estaba a menos de 2 cm). Devuelve true si agarró algo.
    bool EmpezarTelaConPellizco()
    {
        Vector3 pinza = Der.PuntoPellizco;
        Trazo t;
        int i;
        if (BuscarTelaCercana(pinza, RadioPellizcoTela, null, out t, out i))
        {
            EmpezarArrastreTela(t, i, pinza);
            telaConPellizco = true;
            return true;
        }
        t = FiguraBajo(dibujo.transform.InverseTransformPoint(pinza), 0.06f, true);
        if (t == null)
            return false;
        // Pegado a la línea la campana sería diminuta: un poquito más adentro.
        float escala = dibujo.EscalaMundo;
        float d = t.DistanciaACurva(dibujo.transform.InverseTransformPoint(pinza)) * escala;
        if (d < Mathf.Max(0.008f, t.ancho * escala * 0.5f + 0.004f))
        {
            if (Time.time - avisoTela > 3f)
            {
                avisoTela = Time.time;
                dibujo.Mensaje("Tela: pellizca un poco más adentro del relleno");
            }
            return false;
        }
        if (!AgregarTelaEn(t, pinza))
            return false;
        telaConPellizco = true;
        return true;
    }

    // Cada cuadro mientras hay un punto de tela agarrado (paso 1 del modo nodos).
    void SeguirTela(Vector3 punta)
    {
        if (telaConPellizco)
        {
            // Agarrado con el pellizco: abrirlo = soltarlo.
            if (!Der.pellizco)
            {
                SoltarTela();
                return;
            }
            punta = Der.PuntoPellizco;
        }
        MoverTelaConDedo(punta);
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

    // Se suelta (al abrir el pellizco, o la mano izquierda). Si quedó casi plano, el punto se quita solo.
    void SoltarTela()
    {
        if (telaTrazo != null && dibujo != null && Dibujo.Editable(telaTrazo) && telaIndice >= 0
            && telaIndice < telaTrazo.telaMovida.Count)
        {
            if (telaTrazo.telaMovida[telaIndice].magnitude * dibujo.EscalaMundo < TelaPlana)
            {
                telaTrazo.QuitarTela(telaIndice);
                if (telaNueva)
                    dibujo.DescartarUltimoDeshacer(); // no cambió nada: sin paso de deshacer
                else
                    dibujo.Mensaje("Tela plana: punto quitado");
            }
        }
        telaTrazo = null;
        telaIndice = -1;
        telaConPellizco = false;
        telaNueva = false;
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

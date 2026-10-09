using UnityEngine;

// Plano 2D: el dedo puede pasar DETRÁS del plano transparente y la línea sigue (pegada al plano).
// Mientras está detrás, tu mano derecha real se esconde y aparece una copia (un guante de caricatura)
// con la punta del índice justo sobre el plano, de tu lado, que imita todos tus movimientos.
// Un anillito naranja marca en el plano dónde está la punta. Alejar el dedo hacia ti sigue levantando el lápiz.
public partial class ControlManos
{
    const float MaxDetrasPlano = 0.3f;     // más de 30 cm detrás del plano ya no cuenta
    const float SeparacionCopia = 0.005f;  // la punta de la copia queda 5 mm delante del plano (de tu lado)
    const float RadioAnilloPlano = 0.007f;
    const int LadosAnilloPlano = 24;

    GameObject raizCopia;
    ManoVideo manoCopia;
    LineRenderer anilloPlano;
    readonly Vector3[] puntosCopia = new Vector3[21];
    bool conCopia, ocultarDerPlano;

    // Cuánto está el punto DETRÁS del plano, visto desde tu cabeza (en metros): + = detrás, - = de tu lado.
    float ProfundidadTrasPlano(Vector3 local)
    {
        float d = dibujo.DistanciaConSignoMundo(local);
        // La normal del plano apunta lejos de donde estabas al crearlo. Si le diste la vuelta, tu lado es el otro.
        if (Cabeza != null && dibujo.DistanciaConSignoMundo(dibujo.transform.InverseTransformPoint(Cabeza.position)) > 0f)
            d = -d;
        return d;
    }

    bool DibujandoEnPlano => dibujo != null && dibujo.PlanoActivo && !DibujoBloqueado
                             && (GestoIzq == Gesto.Dibujar || GestoIzq == Gesto.Recta);

    // ¿La punta del índice derecho está detrás del plano (con dibujar activo)? Da el punto sobre el plano
    // y la dirección del plano hacia ti.
    bool DedoTrasPlano(out Vector3 enPlanoMundo, out Vector3 haciaTi)
    {
        enPlanoMundo = Vector3.zero;
        haciaTi = Vector3.back;
        if (!DibujandoEnPlano || !Der.valida)
            return false;
        Vector3 local = dibujo.transform.InverseTransformPoint(Der.indice);
        float p = ProfundidadTrasPlano(local);
        if (p <= 0.001f || p > MaxDetrasPlano)
            return false;
        enPlanoMundo = dibujo.transform.TransformPoint(dibujo.ProyectarEnPlano(local));
        Vector3 puntoPlano, normal;
        dibujo.PlanoMundo(out puntoPlano, out normal);
        // La normal apunta lejos de ti (salvo que le hayas dado la vuelta al plano).
        haciaTi = Cabeza != null && Vector3.Dot(Cabeza.position - puntoPlano, normal) > 0f ? normal : -normal;
        return true;
    }

    // Se llama al final de cada cuadro (LateUpdate).
    void ActualizarManoCopia()
    {
        Vector3 enPlano, haciaTi;
        bool detras = DedoTrasPlano(out enPlano, out haciaTi);
        bool ver = detras && materialGuante != null && Der.esqueleto != null;
        if (ver)
        {
            for (int i = 0; i < puntosCopia.Length; i++)
            {
                var hueso = ManosUtil.Hueso(Der.esqueleto, GrabadorProceso.Huesos[i]);
                if (hueso == null)
                {
                    ver = false;
                    break;
                }
                puntosCopia[i] = hueso.position;
            }
        }
        if (ver)
        {
            // Toda la mano se mueve junta para que la punta del índice del guante (que es un poquito más
            // chico que tu mano) quede justo sobre el anillito, de tu lado del plano.
            Vector3 c0 = Vector3.Lerp(puntosCopia[0], puntosCopia[9], 0.5f);
            Vector3 punta = c0 + (puntosCopia[8] - c0) * ManoVideo.EscalaGuante;
            Vector3 mover = enPlano + haciaTi * SeparacionCopia - punta;
            for (int i = 0; i < puntosCopia.Length; i++)
                puntosCopia[i] += mover;
            if (manoCopia == null)
            {
                raizCopia = new GameObject("ManoCopiaPlano");
                manoCopia = new ManoVideo(raizCopia.transform, materialGuante, 0, true);
                manoCopia.UsarGuante(RayasGuante());
            }
        }
        if (manoCopia != null)
            manoCopia.PonerGuante(ver ? puntosCopia : null, false);
        if (ver != conCopia)
        {
            conCopia = ver;
            OcultarDerPorPlano(ver);
        }
        PonerAnilloPlano(detras, enPlano, haciaTi);
    }

    void OcultarDerPorPlano(bool ocultar)
    {
        ocultarDerPlano = ocultar;
        BuscarRenderersMano();
        Forzar(rendsDer, ocultar || ocultarDer || ocultarTodas);
    }

    // Anillito naranja sobre el plano, alrededor de la punta del dedo.
    void PonerAnilloPlano(bool ver, Vector3 centro, Vector3 haciaTi)
    {
        if (!ver)
        {
            if (anilloPlano != null && anilloPlano.gameObject.activeSelf)
                anilloPlano.gameObject.SetActive(false);
            return;
        }
        if (anilloPlano == null)
        {
            var go = new GameObject("AnilloPlano");
            go.transform.SetParent(transform, false);
            anilloPlano = go.AddComponent<LineRenderer>();
            anilloPlano.useWorldSpace = true;
            anilloPlano.loop = true;
            anilloPlano.positionCount = LadosAnilloPlano;
            anilloPlano.widthMultiplier = 0.0016f;
            anilloPlano.numCornerVertices = 2;
            anilloPlano.sharedMaterial = materialNodoActivo != null ? materialNodoActivo : materialCursor;
            anilloPlano.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            anilloPlano.receiveShadows = false;
        }
        if (!anilloPlano.gameObject.activeSelf)
            anilloPlano.gameObject.SetActive(true);
        Vector3 u = Vector3.Cross(haciaTi, Vector3.up);
        if (u.sqrMagnitude < 1e-4f)
            u = Vector3.Cross(haciaTi, Vector3.right);
        u.Normalize();
        Vector3 v = Vector3.Cross(haciaTi, u).normalized;
        centro += haciaTi * 0.001f;
        for (int i = 0; i < LadosAnilloPlano; i++)
        {
            float a = i * (Mathf.PI * 2f / LadosAnilloPlano);
            anilloPlano.SetPosition(i, centro + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * RadioAnilloPlano);
        }
    }
}

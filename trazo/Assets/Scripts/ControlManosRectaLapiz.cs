using UnityEngine;

// Línea RECTA con el lápiz de boceto (parte de ControlManos). En una capa de Boceto con Plano (2D), el gesto de la
// recta (izquierda pulgar + índice + medio) ya no hace una línea de tinta: mientras la estiras ves una GUÍA gris
// clarita y, al soltar (o al levantar el dedo del papel), se vuelve GRAFITO de verdad, con grano y con presión
// (más oscura donde el dedo está más pegado al papel). Se puede deshacer y se guarda como cualquier trazo de lápiz.
public partial class ControlManos
{
    const float AnchoGuiaRecta = 0.0012f; // metros

    bool rectaLapiz;
    Vector3 rectaLapizA, rectaLapizB;      // locales del Dibujo (sobre el plano)
    float presionRectaA, presionRectaB;
    Transform guiaRecta;
    Mesh mallaGuiaRecta;

    void RectaLapiz()
    {
        var hojas = dibujo.hojas;
        if (!Der.valida || hojas == null)
            return;
        // Como el lápiz normal: la punta del dedo de ESTE momento, con el filtro del lápiz.
        var punta = ManosUtil.Hueso(Der.esqueleto, "IndexTip");
        Vector3 crudo = punta != null ? punta.position : Der.indiceCrudo;
        Vector3 local = dibujo.transform.InverseTransformPoint(FiltrarLapiz(crudo));
        if (dibujo.plano && !dibujo.HayPlano && Cabeza != null)
            dibujo.DefinirPlano(local, Cabeza.forward);
        float p = ProfundidadTrasPlano(local);
        bool sobreElPapel = p >= -hojas.alcance && p <= MaxDetrasPlano;
        float presion = Mathf.Clamp01(1f - Mathf.Max(0f, -p) / Mathf.Max(1e-4f, hojas.alcance));
        if (!rectaLapiz)
        {
            lejosDelPlano = !sobreElPapel;
            if (!sobreElPapel)
                return;
            // Empieza donde el dedo toca el papel.
            rectaLapiz = true;
            dibujo.Seleccionar(null);
            rectaLapizA = dibujo.ProyectarEnPlano(local);
            rectaLapizB = rectaLapizA;
            presionRectaA = presion;
            presionRectaB = presion;
        }
        else if (p < -0.025f || p > MaxDetrasPlano)
        {
            // Levantó el dedo del papel: la recta queda hasta donde llegó (al volver, empieza otra).
            TerminarRectaLapiz(true);
            lejosDelPlano = true;
            return;
        }
        lejosDelPlano = false;
        rectaLapizB = dibujo.ProyectarEnPlano(local);
        if (sobreElPapel)
            presionRectaB = presion;
        DibujarGuiaRecta();
    }

    // Termina la recta del lápiz: con sellar = true queda pintada en la hoja (si mide algo).
    void TerminarRectaLapiz(bool sellar)
    {
        if (!rectaLapiz)
            return;
        rectaLapiz = false;
        if (guiaRecta != null && guiaRecta.gameObject.activeSelf)
            guiaRecta.gameObject.SetActive(false);
        lapizTiene = false;
        if (!sellar || dibujo == null || dibujo.hojas == null)
            return;
        if (Vector3.Distance(rectaLapizA, rectaLapizB) * dibujo.EscalaMundo < 0.002f)
            return;
        dibujo.hojas.PintarRecta(rectaLapizA, presionRectaA, rectaLapizB, presionRectaB);
        dibujo.Mensaje("Línea recta de lápiz");
    }

    // La guía: una cinta fina gris sobre el papel, del inicio a tu dedo.
    void DibujarGuiaRecta()
    {
        if (guiaRecta == null)
        {
            if (materialNodo == null)
                return;
            var go = new GameObject("GuiaRectaLapiz");
            go.transform.SetParent(dibujo.transform, false);
            mallaGuiaRecta = new Mesh { name = "GuiaRectaLapiz" };
            mallaGuiaRecta.MarkDynamic();
            mallasTemblor.Add(mallaGuiaRecta);
            go.AddComponent<MeshFilter>().sharedMesh = mallaGuiaRecta;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ColorMaterial(new Color(0.55f, 0.56f, 0.6f));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            guiaRecta = go.transform;
        }
        if (!guiaRecta.gameObject.activeSelf)
            guiaRecta.gameObject.SetActive(true);
        Vector3 punto, normal;
        if (!dibujo.PlanoDeCapa(dibujo.capaActual, out punto, out normal))
            normal = Vector3.forward;
        float escala = dibujo.EscalaMundo;
        Vector3 dir = rectaLapizB - rectaLapizA;
        if (dir.sqrMagnitude < 1e-12f)
            dir = Vector3.Cross(normal, Vector3.up);
        Vector3 lado = Vector3.Cross(dir.normalized, normal);
        if (lado.sqrMagnitude < 1e-8f)
            lado = Vector3.right;
        lado = lado.normalized * (AnchoGuiaRecta * 0.5f / escala);
        // Un poquito hacia tus ojos, para que no se esconda detrás del papel.
        Vector3 cabeza = Cabeza != null ? dibujo.transform.InverseTransformPoint(Cabeza.position) : rectaLapizA - normal;
        Vector3 arriba = normal * (Vector3.Dot(cabeza - rectaLapizA, normal) >= 0f ? 1f : -1f) * (0.0008f / escala);
        Vector3 a = rectaLapizA + arriba, b = rectaLapizB + arriba;
        mallaGuiaRecta.Clear();
        mallaGuiaRecta.vertices = new[] { a - lado, a + lado, b + lado, b - lado };
        mallaGuiaRecta.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 };
        mallaGuiaRecta.RecalculateBounds();
    }
}

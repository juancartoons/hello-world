using System.Collections.Generic;
using UnityEngine;

// "COMO DE 2030" (parte de ControlManos, v36):
//  - LUZ EN LA PUNTA DEL DEDO: el índice derecho lleva una lucecita suave (y el izquierdo una más chiquita). Las
//    líneas, los rellenos, el vidrio de los menús y las manos se iluminan un poquito cuando el dedo se acerca
//    (los shaders leen _JCDedo y _JCDedo2, ver Shaders/TrazoLuz.hlsl).
//  - MANOS DE VIDRIO: las manos de Meta se ven como vidrio, con el contorno iluminado y las yemas brillando.
//    (Si no te gusta, deja vacío "materialManoVidrio" y vuelven las de siempre.)
//  - Cuándo editaste por última vez: las capas (al mirar arriba) y la mesa de trabajo (al mirar abajo) no salen
//    mientras dibujas ni justo después.
//  - La BURBUJITA SECRETA de la paleta (arriba a la derecha): con burbujas (el "plop" de siempre) o sin burbujas
//    (un "tin" de cristal). Llena = con burbujas.
public partial class ControlManos
{
    [Header("v36: como de 2030")]
    [Tooltip("Puntitos de luz (el polvo al borrar y la lucecita de los índices): shader TrazoVR/Polvo")]
    public Material materialPolvo;
    [Tooltip("Manos de vidrio: shader TrazoVR/ManoVidrio (vacío = las manos de siempre)")]
    public Material materialManoVidrio;
    [Tooltip("Vidrio de los menús y la paleta: shader TrazoVR/Vidrio")]
    public Material materialVidrio;

    static readonly int idDedo = Shader.PropertyToID("_JCDedo");
    static readonly int idDedo2 = Shader.PropertyToID("_JCDedo2");
    static Mesh mallaBrillo;
    float luzDer, luzIzq;
    Transform brilloDer, brilloIzq;
    float proximaManoVidrio;
    float ultimaEdicion = -10f;

    // Segundos desde que dibujaste o editaste por última vez (cualquier gesto de la mano izquierda).
    public float SegundosSinEditar => Time.time - ultimaEdicion;

    // Cada cuadro (desde LateUpdate).
    void Actualizar2030()
    {
        if (GestoIzq != Gesto.Ninguno || LapizAtrapado)
            ultimaEdicion = Time.time;
        CapasProfundidad.Asegurar();

        // La luz de la punta de los dedos (se apaga suave si la mano se pierde o se esconde).
        float dt = Time.deltaTime;
        bool derOk = Der.valida && !ocultarDer && !ocultarDerPlano && !ocultarTodas;
        bool izqOk = Izq.valida && !ocultarIzq && !ocultarTodas;
        luzDer = Mathf.MoveTowards(luzDer, derOk ? 1f : 0f, dt * 4f);
        luzIzq = Mathf.MoveTowards(luzIzq, izqOk ? 1f : 0f, dt * 4f);
        Vector3 pd = Der.indice, pi = Izq.indice;
        Shader.SetGlobalVector(idDedo, new Vector4(pd.x, pd.y, pd.z, luzDer));
        Shader.SetGlobalVector(idDedo2, new Vector4(pi.x, pi.y, pi.z, luzIzq));
        ActualizarBrillo(ref brilloDer, pd, luzDer, 0.022f);
        ActualizarBrillo(ref brilloIzq, pi, luzIzq * 0.7f, 0.016f);

        // Las manos de vidrio (se revisa cada 2 segundos: las manos de Meta a veces se vuelven a crear).
        if (materialManoVidrio != null && Time.time >= proximaManoVidrio)
        {
            proximaManoVidrio = Time.time + 2f;
            BuscarRenderersMano();
            PonerManosVidrio(rendsIzq);
            PonerManosVidrio(rendsDer);
        }
    }

    // La lucecita de la punta de un índice: un puntito de luz suave, siempre mirando hacia ti.
    void ActualizarBrillo(ref Transform brillo, Vector3 punta, float fuerza, float tam)
    {
        if (materialPolvo == null)
            return;
        if (brillo == null)
        {
            var go = new GameObject("BrilloDedo");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = MallaBrillo();
            var r = go.AddComponent<MeshRenderer>();
            var m = new Material(materialPolvo);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", new Color(0.62f, 0.8f, 1f, 0.55f));
            materialesPaleta.Add(m); // se libera junto con los de la paleta
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            brillo = go.transform;
        }
        bool ver = fuerza > 0.01f && Cabeza != null;
        if (brillo.gameObject.activeSelf != ver)
            brillo.gameObject.SetActive(ver);
        if (!ver)
            return;
        Vector3 haciaOjos = Cabeza.position - punta;
        if (haciaOjos.sqrMagnitude < 1e-8f)
            return;
        brillo.position = punta + haciaOjos.normalized * 0.004f; // un poquito hacia ti (que no se esconda en el dedo)
        brillo.rotation = Quaternion.LookRotation(-haciaOjos, Cabeza.up);
        brillo.localScale = Vector3.one * tam * Mathf.Lerp(0.6f, 1f, fuerza);
    }

    // Un cuadradito (con color blanco por vértice): el shader del polvo dibuja adentro un círculo suave.
    static Mesh MallaBrillo()
    {
        if (mallaBrillo != null)
            return mallaBrillo;
        mallaBrillo = new Mesh { name = "BrilloDedo" };
        mallaBrillo.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
        mallaBrillo.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        mallaBrillo.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        mallaBrillo.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mallaBrillo.RecalculateBounds();
        return mallaBrillo;
    }

    // Solo las mallas de las manos (SkinnedMeshRenderer), no los menús que cuelgan de la muñeca.
    void PonerManosVidrio(List<Renderer> lista)
    {
        foreach (var r in lista)
        {
            if (r == null || !(r is SkinnedMeshRenderer))
                continue;
            var mats = r.sharedMaterials;
            bool ya = mats.Length > 0;
            foreach (var m in mats)
                if (m != materialManoVidrio)
                    ya = false;
            if (ya)
                continue;
            var nuevos = new Material[Mathf.Max(1, mats.Length)];
            for (int i = 0; i < nuevos.Length; i++)
                nuevos[i] = materialManoVidrio;
            r.sharedMaterials = nuevos;
        }
    }

    // ---------- Vidrio de la paleta ----------

    // El disco blanco de la paleta, de vidrio (si hay material de vidrio).
    Material MaterialVidrioPaleta()
    {
        if (materialVidrio == null)
            return null;
        var m = new Material(materialVidrio);
        if (m.HasProperty("_Esmerilado"))
            m.SetFloat("_Esmerilado", 0.78f);
        materialesPaleta.Add(m);
        return m;
    }

    // ---------- La burbujita secreta ----------

    BotonPaleta botonBurbuja;
    Transform rellenoBurbuja;

    // Arriba a la derecha de la paleta, por fuera de los botones: un aro (la burbuja), su brillito y, si está
    // "con burbujas", llena de celeste. Tocarla cambia entre burbujas y cristal (y suena el nuevo sonido).
    void ArmarBurbujaSecreta()
    {
        var t = new GameObject("BurbujaSecreta").transform;
        t.SetParent(paleta, false);
        float a = 60f * Mathf.Deg2Rad;
        t.localPosition = new Vector3(Mathf.Cos(a) * 0.097f, Mathf.Sin(a) * 0.097f, -0.001f);
        t.localScale = Vector3.one * 0.011f;
        var aro = new GameObject("Aro");
        aro.transform.SetParent(t, false);
        aro.AddComponent<MeshFilter>().sharedMesh = MallaAnillo();
        var ra = aro.AddComponent<MeshRenderer>();
        ra.sharedMaterial = ColorMaterial(new Color(0.35f, 0.6f, 0.85f));
        ra.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ra.receiveShadows = false;
        DiscoPaleta(t, ColorMaterial(new Color(0.72f, 0.88f, 1f)), 0.62f, new Vector3(0f, 0f, 0.02f));
        rellenoBurbuja = t.GetChild(t.childCount - 1);
        DiscoPaleta(t, ColorMaterial(Color.white), 0.18f, new Vector3(-0.16f, 0.16f, -0.02f));
        botonBurbuja = new BotonPaleta { t = t, fondo = null, tamano = 0.011f, angulo = 60f, rama = null, radioToque = 0.008f, tono = 1.2f };
        botonBurbuja.alTocar = () => Sonidos.ConBurbujas = !Sonidos.ConBurbujas;
        botonBurbuja.texto = () => Sonidos.ConBurbujas ? "°o°" : "· · ·";
        botonesPaleta.Add(botonBurbuja);
    }

    void RefrescarBurbujaSecreta()
    {
        if (rellenoBurbuja == null)
            return;
        bool con = Sonidos.ConBurbujas;
        if (rellenoBurbuja.gameObject.activeSelf != con)
            rellenoBurbuja.gameObject.SetActive(con);
    }
}

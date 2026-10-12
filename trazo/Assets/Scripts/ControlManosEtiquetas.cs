using TMPro;
using UnityEngine;

// ETIQUETAS AL PASAR EL DEDO (parte de ControlManos, v38): en la paleta, si dejas el índice derecho cerquita de un
// botón o de un color un cuarto de segundo, sale encima una etiquetita de vidrio con su nombre (como el "hover" de
// los botones en la web). La burbujita secreta no tiene etiqueta.
public partial class ControlManos
{
    const float EsperaEtiqueta = 0.25f;    // segundos con el dedo cerca antes de que salga
    const float CercaBoton = 0.022f;       // más allá del radio de toque de cada botón
    const float AnchoEtiqueta = 0.07f, AltoEtiqueta = 0.016f;

    Transform etiquetaHover, objetivoHover;
    TMP_Text textoHover;
    string nombreHover = "";
    float hoverDesde;

    // El nombre de cada botón de la paleta (por el nombre de su objeto). Null = sin etiqueta.
    static string NombreBotonPaleta(string objeto)
    {
        switch (objeto)
        {
            case "TintaInvisible": return "Tinta invisible";
            case "Linea": return "Línea";
            case "Linea0": return "Temblor";
            case "Linea1": return "Hebras";
            case "Linea2": return "Grosor vivo";
            case "Linea3": return "Ciclo del temblor";
            case "Linea4": return "Suavidad";
            case "Linea5": return "Velocidad del temblor";
            case "Linea6": return "Imán";
            case "Linea7": return "Halo";
            case "Linea8": return "Lápiz";
            case "Linea9": return "Uniones";
            case "Relleno": return "Relleno";
            case "Relleno0": return "Textura";
            case "Relleno1": return "Cubeta";
            case "Relleno2": return "Velocidad de la textura";
            default: return null;
        }
    }

    // Cada cuadro con la paleta abierta (y una vez al cerrarla, para esconder la etiqueta).
    void ActualizarEtiquetaHover(bool abierta)
    {
        Transform mejor = null;
        string nombre = null;
        if (abierta && paleta != null && Der.valida)
        {
            Vector3 punta = Der.indice;
            float mejorD = float.MaxValue;
            foreach (var b in botonesPaleta)
            {
                if (b.t == null || !b.t.gameObject.activeInHierarchy)
                    continue;
                string n = NombreBotonPaleta(b.t.name);
                if (n == null)
                    continue;
                float d = Vector3.Distance(punta, b.t.position);
                if (d < b.radioToque + CercaBoton && d < mejorD)
                {
                    mejorD = d;
                    mejor = b.t;
                    nombre = n;
                }
            }
            for (int i = 0; i < botonesColor.Count && i < ColoresPaleta.Length; i++)
            {
                float d = Vector3.Distance(punta, botonesColor[i].position);
                if (d < 0.014f + CercaBoton * 0.6f && d < mejorD)
                {
                    mejorD = d;
                    mejor = botonesColor[i];
                    nombre = NombreColor(ColoresPaleta[i]);
                }
            }
        }
        if (mejor != objetivoHover)
        {
            objetivoHover = mejor;
            hoverDesde = Time.time;
        }
        bool ver = mejor != null && Time.time - hoverDesde > EsperaEtiqueta;
        if (!ver)
        {
            if (etiquetaHover != null && etiquetaHover.gameObject.activeSelf)
                etiquetaHover.gameObject.SetActive(false);
            return;
        }
        if (etiquetaHover == null)
            ArmarEtiquetaHover();
        if (!etiquetaHover.gameObject.activeSelf)
            etiquetaHover.gameObject.SetActive(true);
        if (nombre != nombreHover)
        {
            nombreHover = nombre;
            Idioma.Poner(textoHover, nombre);
        }
        // Encima del botón (en el plano de la paleta), un poquito hacia ti.
        etiquetaHover.SetPositionAndRotation(mejor.position + paleta.up * 0.021f - paleta.forward * 0.004f, paleta.rotation);
    }

    void ArmarEtiquetaHover()
    {
        etiquetaHover = new GameObject("EtiquetaPaleta").transform;
        etiquetaHover.SetParent(transform, false);
        var fondo = new GameObject("Fondo");
        fondo.transform.SetParent(etiquetaHover, false);
        fondo.AddComponent<MeshFilter>().sharedMesh = Persiana.CrearPastilla(AnchoEtiqueta, AltoEtiqueta);
        var r = fondo.AddComponent<MeshRenderer>();
        Material m;
        if (materialVidrio != null)
        {
            m = new Material(materialVidrio);
            if (m.HasProperty("_Esmerilado"))
                m.SetFloat("_Esmerilado", 0.85f);
            if (m.HasProperty("_Opacidad"))
                m.SetFloat("_Opacidad", 0.92f);
        }
        else
        {
            m = ColorMaterial(new Color(0.96f, 0.96f, 0.97f));
        }
        if (materialVidrio != null)
            materialesPaleta.Add(m);
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(etiquetaHover, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.001f);
        var t = go.AddComponent<TextMeshPro>();
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.005f;
        t.fontSizeMax = 0.1f;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.color = new Color(0.12f, 0.12f, 0.16f);
        t.rectTransform.sizeDelta = new Vector2(AnchoEtiqueta * 0.88f, AltoEtiqueta * 0.7f);
        textoHover = t;
        etiquetaHover.gameObject.SetActive(false);
    }
}

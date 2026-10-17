using System.Collections.Generic;
using TMPro;
using UnityEngine;

// AJUSTES DEL LÁPIZ (v52): un panel que sale arriba (como la mesa) con 5 PALANCAS y una línea de muestra en vivo:
//  Grano (fino → grueso), Densidad (suave → cargado), Orilla (nítida → borrosa), Ancho y Hervido (quieto → nervioso).
//  Desliza el índice derecho sobre una palanca para moverla. "OK" lo guarda en la capa: así salen las líneas NUEVAS
//  (y las seleccionadas cambian también).
public class PanelLapiz : MonoBehaviour
{
    static PanelLapiz instancia;
    public static bool Abierto => instancia != null && instancia.gameObject.activeSelf;

    static readonly string[] Nombres = { "Grano", "Densidad", "Orilla", "Ancho", "Hervido" };
    static readonly string[] Extremos = { "fino · grueso", "suave · cargado", "nítida · borrosa", "delgado · ancho", "quieto · nervioso" };
    const float LargoPalanca = 0.16f;

    ControlManos control;
    Dibujo dibujo;
    readonly int[] valores = new int[5];
    readonly Transform[] perillas = new Transform[5];
    readonly Transform[] rieles = new Transform[5];
    readonly TextMeshPro[] numeros = new TextMeshPro[5];
    Trazo muestra;
    EstiloTrazo estilo;

    public static void Abrir()
    {
        if (instancia == null)
        {
            instancia = new GameObject("PanelLapiz").AddComponent<PanelLapiz>();
            if (!instancia.Armar())
            {
                Destroy(instancia.gameObject);
                instancia = null;
                return;
            }
        }
        instancia.gameObject.SetActive(true);
        instancia.Empezar();
    }

    public static void Cerrar()
    {
        if (instancia != null)
            instancia.gameObject.SetActive(false);
    }

    bool Armar()
    {
        control = ControlManos.Instancia;
        dibujo = control != null ? control.dibujo : null;
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (dibujo == null || nav == null)
            return false;
        Material panel = nav.materialPanel, normal = nav.materialBoton, marcado = nav.materialBotonMarcado;
        Pieza(PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.006f), new Vector3(0.34f, 0.25f, 0.004f), panel);
        var t = PanelTutoriales.Texto(transform, Idioma.T("Ajustes del lápiz"), new Vector3(0f, 0.09f, -0.002f), new Vector2(0.3f, 0.02f), 0.2f, null);
        t.fontStyle = FontStyles.Bold;
        for (int i = 0; i < 5; i++)
        {
            float y = 0.035f - i * 0.03f;
            var n = PanelTutoriales.Texto(transform, Idioma.T(Nombres[i]), new Vector3(-0.125f, y, -0.002f), new Vector2(0.07f, 0.016f), 0.12f, null);
            n.alignment = TextAlignmentOptions.Left;
            var e = PanelTutoriales.Texto(transform, Idioma.T(Extremos[i]), new Vector3(0.02f, y - 0.011f, -0.002f), new Vector2(LargoPalanca, 0.01f), 0.07f, null);
            e.color = new Color(0.45f, 0.45f, 0.5f);
            rieles[i] = Pieza(PrimitiveType.Cube, new Vector3(0.02f, y, 0f), new Vector3(LargoPalanca, 0.004f, 0.004f), marcado);
            perillas[i] = Pieza(PrimitiveType.Sphere, new Vector3(0.02f, y, -0.003f), Vector3.one * 0.012f, marcado);
            numeros[i] = PanelTutoriales.Texto(transform, "5", new Vector3(0.125f, y, -0.002f), new Vector2(0.02f, 0.016f), 0.12f, null);
        }
        var ok = PanelTutoriales.Boton(transform, "OK", new Vector3(0.06f, -0.122f, 0f), new Vector2(0.06f, 0.026f), marcado, normal);
        ok.etiqueta.color = Color.white;
        ok.textoNormal = Color.white;
        ok.alTocar.AddListener(Guardar);
        var cancelar = PanelTutoriales.Boton(transform, Idioma.T("Cancelar"), new Vector3(-0.02f, -0.122f, 0f), new Vector2(0.07f, 0.026f), normal, marcado);
        cancelar.alTocar.AddListener(Cerrar);
        // La línea de muestra (con el mismo shader que tus líneas).
        var go = new GameObject("MuestraLapiz");
        go.layer = 0;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(-0.1f, -0.122f, -0.004f);
        muestra = go.AddComponent<Trazo>();
        muestra.Configurar(dibujo.materialLinea, dibujo.materialRelleno, 0.004f, EstiloLinea.Cinta);
        // Se mueve como los demás paneles (pellizco en una parte vacía, o nodos izquierdo + índice derecho).
        var mover = gameObject.AddComponent<ArrastrePanel>();
        mover.centro = new Vector2(0f, -0.02f);
        mover.medio = new Vector2(0.17f, 0.125f);
        return true;
    }

    Transform Pieza(PrimitiveType tipo, Vector3 pos, Vector3 escala, Material m)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        var r = go.GetComponent<Renderer>();
        if (m != null)
            r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    void Empezar()
    {
        var c = dibujo.CapaActual;
        estilo = EstiloTrazo.DesdeCapa(c);
        estilo.texturaLinea = 1;
        valores[0] = estilo.lapizGrano;
        valores[1] = estilo.lapizDensidad;
        valores[2] = estilo.lapizBorde;
        valores[3] = estilo.lapizAncho;
        valores[4] = estilo.lapizHervido;
        // Arriba, delante de ti (como la mesa).
        var cab = control.Cabeza;
        if (cab != null)
        {
            Vector3 adelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
            if (adelante.sqrMagnitude < 1e-4f)
                adelante = Vector3.forward;
            adelante.Normalize();
            Vector3 pos = cab.position + adelante * 0.45f + Vector3.up * 0.12f;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cab.position, Vector3.up));
        }
        Refrescar();
    }

    void Update()
    {
        if (control == null || !control.Der.valida)
            return;
        Vector3 dedo = control.Der.indice;
        for (int i = 0; i < 5; i++)
        {
            Vector3 l = transform.InverseTransformPoint(dedo) - rieles[i].localPosition;
            if (Mathf.Abs(l.y) < 0.012f && Mathf.Abs(l.z) < 0.02f && Mathf.Abs(l.x) < LargoPalanca * 0.5f + 0.01f)
            {
                int v = Mathf.Clamp(Mathf.RoundToInt((l.x / LargoPalanca + 0.5f) * 9f), 0, 9);
                if (v != valores[i])
                {
                    valores[i] = v;
                    control.SonidoBurbuja(perillas[i].position);
                    Refrescar();
                }
            }
        }
    }

    void Refrescar()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = rieles[i].localPosition;
            perillas[i].localPosition = new Vector3(p.x + (valores[i] / 9f - 0.5f) * LargoPalanca, p.y, -0.003f);
            numeros[i].text = valores[i].ToString();
        }
        estilo.lapizGrano = valores[0];
        estilo.lapizDensidad = valores[1];
        estilo.lapizBorde = valores[2];
        estilo.lapizAncho = valores[3];
        estilo.lapizHervido = valores[4];
        // La muestra: una S con este lápiz.
        muestra.tieneEstilo = true;
        muestra.estiloTrazo = estilo.Copia();
        muestra.color = dibujo.ColorNuevo.a > 0.01f ? dibujo.ColorNuevo : Color.black;
        var d = new DatosTrazo { ancho = 0.004f / Mathf.Max(0.0001f, transform.lossyScale.x) };
        for (int k = 0; k <= 8; k++)
        {
            float u = k / 8f;
            d.nodos.Add(new Vector3(-0.04f + 0.08f * u, 0.008f * Mathf.Sin(u * Mathf.PI * 2f), 0f));
        }
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        muestra.AplicarPose(d, null, 0f);
        muestra.PonerMaterialLinea(dibujo.MaterialColor(muestra.color));
        Trazo.silenciar = antes;
    }

    void Guardar()
    {
        var c = dibujo.CapaActual;
        if (c != null)
        {
            c.texturaLinea = 1;
            c.lapizGrano = valores[0];
            c.lapizDensidad = valores[1];
            c.lapizBorde = valores[2];
            c.lapizAncho = valores[3];
            c.lapizHervido = valores[4];
            int n = dibujo.AplicarEstiloASeleccion();
            dibujo.Mensaje(n > 0 ? "Lápiz guardado (" + n + " seleccionadas y las nuevas)" : "Lápiz guardado: tus líneas nuevas salen así");
        }
        Cerrar();
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}

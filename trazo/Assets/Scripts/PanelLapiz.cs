using System.Collections.Generic;
using TMPro;
using UnityEngine;

// AJUSTES DEL LÁPIZ (v52, v53, v54): un panel que sale arriba (como la mesa) con 6 PALANCAS y una línea de muestra grande:
//  Halo (v54: un degradé suave del color de la línea alrededor; 0 = sin halo), Grano (fibras finas → gruesas),
//  Densidad (suave → cargado), Orilla (nítida → borrosa), Ancho y Hervido (quieto → nervioso). (v54: sin granitos.)
//  - Desliza el índice derecho sobre una palanca para moverla.
//  - v53: EN VIVO: la muestra y las líneas SELECCIONADAS cambian mientras mueves la palanca (sin esperar al OK).
//    "OK" lo deja así (y las líneas nuevas salen así); "Cancelar" vuelve todo a como estaba.
//  - v53: se abre solo al elegir la textura Lápiz en la paleta.
public class PanelLapiz : MonoBehaviour
{
    static PanelLapiz instancia;
    public static bool Abierto => instancia != null && instancia.gameObject.activeSelf;

    const int Filas = 6;
    static readonly string[] Nombres = { "Halo", "Grano", "Densidad", "Orilla", "Ancho", "Hervido" };
    static readonly string[] Extremos =
    {
        "sin halo · fuerte", "fibras finas · gruesas", "suave · cargado", "nítida · borrosa",
        "delgado · ancho", "quieto · nervioso"
    };
    const float LargoPalanca = 0.16f;

    ControlManos control;
    Dibujo dibujo;
    readonly int[] valores = new int[Filas];
    readonly Transform[] perillas = new Transform[Filas];
    readonly Transform[] rieles = new Transform[Filas];
    readonly TextMeshPro[] numeros = new TextMeshPro[Filas];
    Trazo muestra;
    EstiloTrazo estilo;

    // Para "Cancelar": cómo estaban la capa y las líneas seleccionadas al abrir.
    DatosCapa capaAntes;
    readonly List<Trazo> selAntes = new List<Trazo>();
    readonly List<EstiloTrazo> estilosAntes = new List<EstiloTrazo>();
    readonly List<bool> teniaEstilo = new List<bool>();
    bool cambio, deshacerGuardado;

    static int Maximo(int fila) => 9;

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
        if (instancia.gameObject.activeSelf)
            return;
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
        Pieza(PrimitiveType.Cube, new Vector3(0f, -0.03f, 0.006f), new Vector3(0.36f, 0.31f, 0.004f), panel);
        var t = PanelTutoriales.Texto(transform, Idioma.T("Ajustes del lápiz"), new Vector3(0f, 0.11f, -0.002f), new Vector2(0.3f, 0.02f), 0.2f, null);
        t.fontStyle = FontStyles.Bold;
        var ayuda = PanelTutoriales.Texto(transform, Idioma.T("Cambia en vivo: la muestra y las líneas seleccionadas"),
                                          new Vector3(0f, 0.094f, -0.002f), new Vector2(0.32f, 0.01f), 0.07f, null);
        ayuda.color = new Color(0.45f, 0.45f, 0.5f);
        for (int i = 0; i < Filas; i++)
        {
            float y = 0.02f - i * 0.03f;
            var n = PanelTutoriales.Texto(transform, Idioma.T(Nombres[i]), new Vector3(-0.13f, y, -0.002f), new Vector2(0.07f, 0.016f), 0.12f, null);
            n.alignment = TextAlignmentOptions.Left;
            var e = PanelTutoriales.Texto(transform, Idioma.T(Extremos[i]), new Vector3(0.01f, y - 0.011f, -0.002f), new Vector2(LargoPalanca + 0.02f, 0.01f), 0.07f, null);
            e.color = new Color(0.45f, 0.45f, 0.5f);
            rieles[i] = Pieza(PrimitiveType.Cube, new Vector3(0.01f, y, 0f), new Vector3(LargoPalanca, 0.004f, 0.004f), marcado);
            perillas[i] = Pieza(PrimitiveType.Sphere, new Vector3(0.01f, y, -0.003f), Vector3.one * 0.012f, marcado);
            numeros[i] = PanelTutoriales.Texto(transform, "5", new Vector3(0.135f, y, -0.002f), new Vector2(0.05f, 0.016f), 0.1f, null);
        }
        var ok = PanelTutoriales.Boton(transform, "OK", new Vector3(0.09f, -0.165f, 0f), new Vector2(0.06f, 0.026f), marcado, normal);
        ok.etiqueta.color = Color.white;
        ok.textoNormal = Color.white;
        ok.alTocar.AddListener(Guardar);
        var cancelar = PanelTutoriales.Boton(transform, Idioma.T("Cancelar"), new Vector3(0.01f, -0.165f, 0f), new Vector2(0.08f, 0.026f), normal, marcado);
        cancelar.alTocar.AddListener(Cancelar);
        // La línea de muestra, grande, arriba (con el mismo shader que tus líneas).
        var go = new GameObject("MuestraLapiz");
        go.layer = 0;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.06f, -0.004f);
        muestra = go.AddComponent<Trazo>();
        muestra.Configurar(dibujo.materialLinea, dibujo.materialRelleno, 0.006f, EstiloLinea.Cinta);
        // Se mueve como los demás paneles (pellizco en una parte vacía, o nodos izquierdo + índice derecho).
        var mover = gameObject.AddComponent<ArrastrePanel>();
        mover.centro = new Vector2(0f, -0.03f);
        mover.medio = new Vector2(0.18f, 0.155f);
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
        capaAntes = c != null ? c.Copia() : null;
        selAntes.Clear();
        estilosAntes.Clear();
        teniaEstilo.Clear();
        foreach (var t in dibujo.Seleccionadas())
        {
            selAntes.Add(t);
            teniaEstilo.Add(t.tieneEstilo);
            estilosAntes.Add(t.estiloTrazo != null ? t.estiloTrazo.Copia() : null);
        }
        cambio = false;
        deshacerGuardado = false;
        estilo = EstiloTrazo.DesdeCapa(c);
        estilo.texturaLinea = 1;
        valores[0] = estilo.lapizHalo;
        valores[1] = estilo.lapizGrano;
        valores[2] = estilo.lapizDensidad;
        valores[3] = estilo.lapizBorde;
        valores[4] = estilo.lapizAncho;
        valores[5] = estilo.lapizHervido;
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
        Refrescar(false);
    }

    void Update()
    {
        if (dibujo != null && muestra != null)
            dibujo.HaloLapiz(muestra);   // v54: la muestra también lleva su halo
        if (control == null || !control.Der.valida)
            return;
        Vector3 dedo = control.Der.indice;
        for (int i = 0; i < Filas; i++)
        {
            Vector3 l = transform.InverseTransformPoint(dedo) - rieles[i].localPosition;
            if (Mathf.Abs(l.y) < 0.012f && Mathf.Abs(l.z) < 0.02f && Mathf.Abs(l.x) < LargoPalanca * 0.5f + 0.01f)
            {
                int v = Mathf.Clamp(Mathf.RoundToInt((l.x / LargoPalanca + 0.5f) * Maximo(i)), 0, Maximo(i));
                if (v != valores[i])
                {
                    valores[i] = v;
                    control.SonidoBurbuja(perillas[i].position);
                    Refrescar(true);
                }
            }
        }
    }

    void Refrescar(bool enVivo)
    {
        for (int i = 0; i < Filas; i++)
        {
            Vector3 p = rieles[i].localPosition;
            perillas[i].localPosition = new Vector3(p.x + (valores[i] / (float)Maximo(i) - 0.5f) * LargoPalanca, p.y, -0.003f);
            numeros[i].text = i == 0 && valores[0] == 0 ? Idioma.T("No") : valores[i].ToString();
        }
        estilo.lapizHalo = valores[0];
        estilo.lapizGrano = valores[1];
        estilo.lapizDensidad = valores[2];
        estilo.lapizBorde = valores[3];
        estilo.lapizAncho = valores[4];
        estilo.lapizHervido = valores[5];
        // La muestra: una onda larga con este lápiz.
        muestra.tieneEstilo = true;
        muestra.estiloTrazo = estilo.Copia();
        muestra.color = dibujo.ColorNuevo.a > 0.01f ? dibujo.ColorNuevo : Color.black;
        var d = new DatosTrazo { ancho = 0.006f / Mathf.Max(0.0001f, transform.lossyScale.x) };
        for (int k = 0; k <= 12; k++)
        {
            float u = k / 12f;
            d.nodos.Add(new Vector3(-0.15f + 0.3f * u, 0.012f * Mathf.Sin(u * Mathf.PI * 3f), 0f));
        }
        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        muestra.AplicarPose(d, null, 0f);
        muestra.PonerMaterialLinea(dibujo.MaterialColor(muestra.color));
        Trazo.silenciar = antes;
        if (enVivo)
            AplicarEnVivo();
    }

    // v53: la capa (líneas nuevas) y las seleccionadas cambian YA.
    void AplicarEnVivo()
    {
        var c = dibujo.CapaActual;
        if (c == null)
            return;
        if (!deshacerGuardado && selAntes.Count > 0)
        {
            dibujo.GuardarParaDeshacer();
            deshacerGuardado = true;
        }
        cambio = true;
        c.texturaLinea = 1;
        if (c.hebras > 1)
            c.hebras = 1;
        c.lapizHalo = valores[0];
        c.lapizGrano = valores[1];
        c.lapizDensidad = valores[2];
        c.lapizBorde = valores[3];
        c.lapizAncho = valores[4];
        c.lapizHervido = valores[5];
        dibujo.AplicarEstiloASeleccion();
    }

    void Guardar()
    {
        if (!cambio)
            AplicarEnVivo();
        int n = selAntes.Count;
        dibujo.Mensaje(n > 0 ? "Lápiz guardado (" + n + " seleccionadas y las nuevas)" : "Lápiz guardado: tus líneas nuevas salen así");
        Cerrar();
    }

    // Todo vuelve a como estaba al abrir el panel.
    void Cancelar()
    {
        if (cambio)
        {
            var c = dibujo.CapaActual;
            if (c != null && capaAntes != null)
            {
                c.texturaLinea = capaAntes.texturaLinea;
                c.hebras = capaAntes.hebras;
                c.lapizHalo = capaAntes.lapizHalo;
                c.lapizGrano = capaAntes.lapizGrano;
                c.lapizDensidad = capaAntes.lapizDensidad;
                c.lapizBorde = capaAntes.lapizBorde;
                c.lapizAncho = capaAntes.lapizAncho;
                c.lapizHervido = capaAntes.lapizHervido;
            }
            bool antes = Trazo.silenciar;
            Trazo.silenciar = true;
            for (int i = 0; i < selAntes.Count; i++)
            {
                var t = selAntes[i];
                if (t == null)
                    continue;
                t.tieneEstilo = teniaEstilo[i];
                t.estiloTrazo = estilosAntes[i];
                t.Reconstruir(false);
            }
            Trazo.silenciar = antes;
            Trazo.huboCambio = false;
        }
        Cerrar();
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}

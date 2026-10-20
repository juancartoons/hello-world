using System.Collections.Generic;
using TMPro;
using UnityEngine;

// PLASTILINA (v57): una textura más del RELLENO (Relleno → Textura → Plastilina, o di "plastilina").
//  - Con la plastilina elegida, dibuja una figura cerrada: se rellena e INFLA sola, como una almohadita de plastilina
//    (por delante y por detrás). La línea del borde desaparece (se puede dejar con el botón "Línea del borde").
//  - Luz mate y suave, bordes un poco irregulares (hechos a mano), huellas de dedos aquí y allá, y "hervido"
//    opcional (stop-motion: la plastilina se acomoda sola unas veces por segundo).
//  - El panel de PALANCAS (sale solo al elegirla): Inflado, Huellas, Bordes y Hervido. Cambia en vivo la última
//    figura de plastilina (o las seleccionadas) y las próximas.
//  - PRESUPUESTO: la plastilina pesa más que un relleno plano. Un medidor dice cuánto del "presupuesto seguro" del
//    Quest 2 llevas (verde, amarillo, rojo) y la app avisa con consejos antes de que se ponga lento.
public static class Plastilina
{
    const string ClaveInflado = "jcartoons_pl_inflado", ClaveHuellas = "jcartoons_pl_huellas";
    const string ClaveIrregular = "jcartoons_pl_irregular", ClaveBorde = "jcartoons_pl_borde";
    // Puntos (vértices) de plastilina que el Quest 2 mueve cómodo, también al animar. Un muñeco mediano usa ~800.
    public const int PresupuestoSeguro = 40000;

    public static float Inflado
    {
        get => PlayerPrefs.GetFloat(ClaveInflado, 0.55f);
        set => PlayerPrefs.SetFloat(ClaveInflado, Mathf.Clamp(value, 0.05f, 1.5f));
    }

    public static float Huellas
    {
        get => PlayerPrefs.GetFloat(ClaveHuellas, 0.5f);
        set => PlayerPrefs.SetFloat(ClaveHuellas, Mathf.Clamp01(value));
    }

    public static float Irregular
    {
        get => PlayerPrefs.GetFloat(ClaveIrregular, 0.5f);
        set => PlayerPrefs.SetFloat(ClaveIrregular, Mathf.Clamp01(value));
    }

    public static bool ConBorde
    {
        get => PlayerPrefs.GetInt(ClaveBorde, 0) != 0;
        set => PlayerPrefs.SetInt(ClaveBorde, value ? 1 : 0);
    }

    // La última figura que se volvió plastilina (el panel la cambia en vivo si no hay seleccionadas).
    public static Trazo Ultima;

    public static void PorDefecto(Trazo t)
    {
        if (t == null)
            return;
        t.plastiInflado = Inflado;
        t.plastiHuellas = Huellas;
        t.plastiIrregular = Irregular;
        t.sinBorde = !ConBorde;
        Ultima = t;
    }

    // Cuánto del presupuesto se usa (1 = 100%) y cuántas figuras de plastilina hay.
    public static float Uso(Dibujo d, out int figuras, out int puntos)
    {
        figuras = 0;
        puntos = 0;
        if (d == null)
            return 0f;
        foreach (var t in d.trazos)
        {
            if (t == null || !t.EsPlastilina)
                continue;
            figuras++;
            puntos += t.VerticesRelleno;
        }
        float uso = puntos / (float)PresupuestoSeguro;
        // Si el visor YA va lento y hay plastilina, el medidor no puede decir "verde".
        var r = Rendimiento.Instancia;
        if (figuras > 0 && r != null && r.Fps < 66f)
            uso = Mathf.Max(uso, 0.95f);
        return uso;
    }

    static int nivelAvisado;

    // Después de rellenar con plastilina: avisa (una vez por escalón) si se acerca al límite.
    public static void Revisar(Dibujo d)
    {
        int figuras, puntos;
        float uso = Uso(d, out figuras, out puntos);
        int nivel = uso >= 1f ? 2 : uso >= 0.7f ? 1 : 0;
        if (nivel <= nivelAvisado)
        {
            nivelAvisado = nivel;
            return;
        }
        nivelAvisado = nivel;
        if (nivel == 1)
            d.Mensaje(Idioma.T("Plastilina: vas en") + " " + Mathf.RoundToInt(uso * 100f) + "% · "
                      + Idioma.T("Consejo: usa plastilina en los personajes y haz el fondo plano, en un fondo 360 o en otro archivo"));
        else if (nivel == 2)
            d.Mensaje(Idioma.T("Plastilina: llegaste al límite seguro del Quest 2. Puede ponerse lento: acorta la escena, aplana lo lejano (textura Liso) o haz los fondos aparte"));
    }

    public static Color ColorUso(float uso)
    {
        return uso >= 1f ? new Color(0.9f, 0.25f, 0.2f) : uso >= 0.7f ? new Color(0.95f, 0.75f, 0.15f) : new Color(0.3f, 0.75f, 0.35f);
    }
}

// El panel de palancas de la plastilina (como el del lápiz: sale arriba, delante de ti).
public class PanelPlastilina : MonoBehaviour
{
    static PanelPlastilina instancia;
    public static bool Abierto => instancia != null && instancia.gameObject.activeSelf;

    const int Filas = 4;
    static readonly string[] Nombres = { "Inflado", "Huellas", "Bordes", "Hervido" };
    static readonly string[] Extremos = { "plano · globo", "lisa · con huellas", "parejos · hechos a mano", "quieto · stop-motion" };
    const float LargoPalanca = 0.16f;

    ControlManos control;
    Dibujo dibujo;
    readonly int[] valores = new int[Filas];
    readonly Transform[] perillas = new Transform[Filas];
    readonly Transform[] rieles = new Transform[Filas];
    readonly TextMeshPro[] numeros = new TextMeshPro[Filas];
    BotonTocable btnBorde;
    Transform barra;
    Renderer barraRenderer;
    TextMeshPro textoUso;
    MaterialPropertyBlock bloque;
    float proximoUso;
    bool deshacerGuardado;

    static int Maximo(int fila) => fila == 3 ? Trazo.VelocidadesTextura.Length - 1 : 9;

    public static void Abrir()
    {
        if (instancia == null)
        {
            instancia = new GameObject("PanelPlastilina").AddComponent<PanelPlastilina>();
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
        Pieza(PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.006f), new Vector3(0.36f, 0.29f, 0.004f), panel);
        var t = PanelTutoriales.Texto(transform, "", new Vector3(0f, 0.105f, -0.002f), new Vector2(0.3f, 0.02f), 0.2f, null);
        Idioma.Poner(t, "Plastilina");
        t.fontStyle = FontStyles.Bold;
        var ayuda = PanelTutoriales.Texto(transform, "", new Vector3(0f, 0.088f, -0.002f), new Vector2(0.32f, 0.01f), 0.07f, null);
        Idioma.Poner(ayuda, "Cambia en vivo: la última figura de plastilina (o las seleccionadas) y las próximas");
        ayuda.color = new Color(0.45f, 0.45f, 0.5f);
        for (int i = 0; i < Filas; i++)
        {
            float y = 0.06f - i * 0.03f;
            var n = PanelTutoriales.Texto(transform, "", new Vector3(-0.13f, y, -0.002f), new Vector2(0.07f, 0.016f), 0.12f, null);
            Idioma.Poner(n, Nombres[i]);
            n.alignment = TextAlignmentOptions.Left;
            var e = PanelTutoriales.Texto(transform, "", new Vector3(0.01f, y - 0.011f, -0.002f), new Vector2(LargoPalanca + 0.02f, 0.01f), 0.07f, null);
            Idioma.Poner(e, Extremos[i]);
            e.color = new Color(0.45f, 0.45f, 0.5f);
            rieles[i] = Pieza(PrimitiveType.Cube, new Vector3(0.01f, y, 0f), new Vector3(LargoPalanca, 0.004f, 0.004f), marcado);
            perillas[i] = Pieza(PrimitiveType.Sphere, new Vector3(0.01f, y, -0.003f), Vector3.one * 0.012f, marcado);
            numeros[i] = PanelTutoriales.Texto(transform, "5", new Vector3(0.135f, y, -0.002f), new Vector2(0.05f, 0.016f), 0.1f, null);
        }
        // La línea del borde: se ve o no.
        btnBorde = PanelTutoriales.Boton(transform, "", new Vector3(-0.07f, -0.07f, 0f), new Vector2(0.15f, 0.024f), normal, marcado);
        btnBorde.alTocar.AddListener(AlternarBorde);
        // El medidor del presupuesto.
        Pieza(PrimitiveType.Cube, new Vector3(0f, -0.1f, 0.001f), new Vector3(0.3f, 0.01f, 0.003f), normal);
        barra = Pieza(PrimitiveType.Cube, new Vector3(-0.15f, -0.1f, -0.001f), new Vector3(0.001f, 0.01f, 0.004f), marcado);
        barraRenderer = barra.GetComponent<Renderer>();
        textoUso = PanelTutoriales.Texto(transform, "", new Vector3(0f, -0.117f, -0.002f), new Vector2(0.32f, 0.012f), 0.08f, null);
        var listo = PanelTutoriales.Boton(transform, "OK", new Vector3(0.11f, -0.07f, 0f), new Vector2(0.06f, 0.024f), marcado, normal);
        listo.etiqueta.color = Color.white;
        listo.textoNormal = Color.white;
        listo.alTocar.AddListener(Cerrar);
        var mover = gameObject.AddComponent<ArrastrePanel>();
        mover.centro = new Vector2(0f, -0.02f);
        mover.medio = new Vector2(0.18f, 0.145f);
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
        deshacerGuardado = false;
        valores[0] = Mathf.Clamp(Mathf.RoundToInt((Plastilina.Inflado - 0.05f) / 0.16f), 0, 9);
        valores[1] = Mathf.Clamp(Mathf.RoundToInt(Plastilina.Huellas * 9f), 0, 9);
        valores[2] = Mathf.Clamp(Mathf.RoundToInt(Plastilina.Irregular * 9f), 0, 9);
        valores[3] = control.VelocidadTexturaElegida;
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
        proximoUso = 0f;
        Refrescar(false);
    }

    void Update()
    {
        if (Time.unscaledTime >= proximoUso)
        {
            proximoUso = Time.unscaledTime + 0.5f;
            Medidor();
        }
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

    void Medidor()
    {
        int figuras, puntos;
        float uso = Plastilina.Uso(dibujo, out figuras, out puntos);
        float largo = 0.3f * Mathf.Clamp01(uso);
        barra.localScale = new Vector3(Mathf.Max(0.001f, largo), 0.01f, 0.004f);
        barra.localPosition = new Vector3(-0.15f + largo * 0.5f, -0.1f, -0.001f);
        if (bloque == null)
            bloque = new MaterialPropertyBlock();
        Color c = Plastilina.ColorUso(uso);
        bloque.SetColor("_BaseColor", c);
        bloque.SetColor("_Color", c);
        barraRenderer.SetPropertyBlock(bloque);
        var r = Rendimiento.Instancia;
        textoUso.text = Idioma.T("Presupuesto del Quest 2") + ": " + Mathf.RoundToInt(uso * 100f) + "% · " + figuras + " "
                        + Idioma.T("figuras") + (r != null ? " · " + Mathf.RoundToInt(r.Fps) + " fps" : "")
                        + (uso >= 1f ? " · " + Idioma.T("¡para aquí!") : uso >= 0.7f ? " · " + Idioma.T("con cuidado") : " · " + Idioma.T("seguro"));
    }

    void Refrescar(bool enVivo)
    {
        for (int i = 0; i < Filas; i++)
        {
            Vector3 p = rieles[i].localPosition;
            perillas[i].localPosition = new Vector3(p.x + (valores[i] / (float)Maximo(i) - 0.5f) * LargoPalanca, p.y, -0.003f);
            numeros[i].text = i == 3 ? Idioma.T(Trazo.NombresVelocidadTextura[valores[3]]) : valores[i].ToString();
        }
        btnBorde.etiqueta.text = Idioma.T(Plastilina.ConBorde ? "Línea del borde: sí" : "Línea del borde: no");
        if (enVivo)
            Aplicar();
    }

    void AlternarBorde()
    {
        Plastilina.ConBorde = !Plastilina.ConBorde;
        Refrescar(true);
    }

    // Las que cambian en vivo: las seleccionadas de plastilina; si no hay, la última que se hizo.
    List<Trazo> Destinos()
    {
        var lista = new List<Trazo>();
        foreach (var t in dibujo.Seleccionadas())
            if (t != null && t.EsPlastilina)
                lista.Add(t);
        if (lista.Count == 0 && Plastilina.Ultima != null && Plastilina.Ultima.EsPlastilina && dibujo.trazos.Contains(Plastilina.Ultima))
            lista.Add(Plastilina.Ultima);
        return lista;
    }

    void Aplicar()
    {
        Plastilina.Inflado = 0.05f + valores[0] * 0.16f;
        Plastilina.Huellas = valores[1] / 9f;
        Plastilina.Irregular = valores[2] / 9f;
        control.ElegirVelocidadPlastilina(valores[3]);
        PlayerPrefs.Save();
        var lista = Destinos();
        if (lista.Count == 0)
            return;
        if (!deshacerGuardado)
        {
            dibujo.GuardarParaDeshacer();
            deshacerGuardado = true;
        }
        foreach (var t in lista)
        {
            t.plastiInflado = Plastilina.Inflado;
            t.plastiHuellas = Plastilina.Huellas;
            t.plastiIrregular = Plastilina.Irregular;
            t.sinBorde = !Plastilina.ConBorde;
            t.velocidadTextura = valores[3];
            t.Reconstruir();
        }
        dibujo.TexturaCambiada();
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}

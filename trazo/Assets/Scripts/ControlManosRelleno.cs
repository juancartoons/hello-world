using UnityEngine;

// Rellenos (parte de ControlManos), con la paleta de colores:
//  - Botón RELLENO (a las 2 de la paleta, 3 facetas verdes): ábrelo (se pone amarillo) y los 12 colores pasan a
//    ser los del RELLENO, que va aparte del color de la línea. En el centro de la paleta ves cómo queda.
//  - Con RELLENO abierto, TOCAR DENTRO de una forma (abierta o cerrada) = rellenarla con ese color y su relleno
//    vivo (con tinta invisible = quitarle el relleno). Espera un instante: si también llega el pulgar, es el gesto
//    del tamaño. Con RELLENO cerrado, tocar las figuras no les hace nada (así no cambias un color sin querer).
//  - Hijos de RELLENO: TEXTURA (Liso → Facetas → Manchas → Pinceladas, estilo Quill), CUBETA (lo que dibujas se va
//    rellenando en vivo, aunque no cierres la forma) y VELOCIDAD de la textura (solo con textura).
//  - TAMAÑO de la textura: con la paleta abierta, pon el pulgar y el índice derechos sobre un relleno con textura
//    y ábrelos o ciérralos (como el zoom del teléfono). Ese tamaño se usa también en los próximos rellenos.
//  - TINTA INVISIBLE (a las 12): un "color" que no se ve. Para la línea: formas sin borde, caminos del títere, guías.
//    Para el relleno: quitarlo.
public partial class ControlManos
{
    const string ClaveCubeta = "jcartoons_cubeta";
    const string ClaveTextura = "jcartoons_textura", ClaveVelocidadTextura = "jcartoons_vel_textura";
    const string ClaveEscalaTextura = "jcartoons_escala_textura";
    const float EsperaRelleno = 0.15f;      // segundos con el dedo dentro antes de rellenar
    const float ZonaMuertaPellizco = 0.008f; // hay que abrir o cerrar 8 mm antes de que cambie el tamaño

    int cubeta = -1; // -1 = aún no leído
    // El color del RELLENO (aparte del de la línea). Empieza amarillo.
    Color colorRellenoNuevo = new Color(1f, 0.82f, 0.1f);
    Color ultimoRellenoVisible = new Color(1f, 0.82f, 0.1f);

    public bool CubetaActiva
    {
        get
        {
            if (cubeta < 0)
                cubeta = PlayerPrefs.GetInt(ClaveCubeta, 0) != 0 ? 1 : 0;
            return cubeta == 1;
        }
        set
        {
            cubeta = value ? 1 : 0;
            PlayerPrefs.SetInt(ClaveCubeta, cubeta);
            PlayerPrefs.Save();
        }
    }

    // El color con el que se rellena: el elegido para el relleno; si es tinta invisible, el último que se veía.
    Color ColorDeRelleno => colorRellenoNuevo.a > 0.01f ? colorRellenoNuevo : ultimoRellenoVisible;

    void PonerColorRellenoNuevo(Color c)
    {
        colorRellenoNuevo = c;
        if (c.a > 0.01f)
            ultimoRellenoVisible = c;
    }

    // Una línea nueva con la cubeta encendida: se va rellenando mientras la dibujas.
    void AplicarCubeta(Trazo t)
    {
        if (t == null || !CubetaActiva)
            return;
        t.rellenoAbierto = true;
        t.relleno = true;
        Color c = ColorDeRelleno;
        t.colorFondo = new Color(c.r, c.g, c.b, 1f);
        t.texturaRelleno = TexturaElegida;
        t.velocidadTextura = VelocidadTexturaElegida;
        t.escalaTextura = EscalaTexturaElegida;
    }

    // ---------- Relleno vivo (texturas que se mueven, estilo Quill) ----------
    int texturaElegida = -1, velocidadTexturaElegida = -1; // -1 = aún no leído
    float escalaTexturaElegida = -1f;

    public int TexturaElegida
    {
        get
        {
            if (texturaElegida < 0)
                texturaElegida = Mathf.Clamp(PlayerPrefs.GetInt(ClaveTextura, 0), 0, Trazo.NombresTextura.Length - 1);
            return texturaElegida;
        }
    }

    public int VelocidadTexturaElegida
    {
        get
        {
            if (velocidadTexturaElegida < 0)
                velocidadTexturaElegida = Mathf.Clamp(PlayerPrefs.GetInt(ClaveVelocidadTextura, 2), 0, Trazo.VelocidadesTextura.Length - 1);
            return velocidadTexturaElegida;
        }
    }

    // El tamaño de la textura para los próximos rellenos (el último que pusiste con los dedos).
    public float EscalaTexturaElegida
    {
        get
        {
            if (escalaTexturaElegida <= 0f)
                escalaTexturaElegida = Trazo.LimitarEscala(PlayerPrefs.GetFloat(ClaveEscalaTextura, 1f));
            return escalaTexturaElegida;
        }
    }

    void GuardarEscalaTextura(float escala)
    {
        escalaTexturaElegida = Trazo.LimitarEscala(escala);
        PlayerPrefs.SetFloat(ClaveEscalaTextura, escalaTexturaElegida);
        PlayerPrefs.Save();
    }

    void ElegirTextura(int textura, int velocidad)
    {
        texturaElegida = Mathf.Clamp(textura, 0, Trazo.NombresTextura.Length - 1);
        velocidadTexturaElegida = Mathf.Clamp(velocidad, 0, Trazo.VelocidadesTextura.Length - 1);
        PlayerPrefs.SetInt(ClaveTextura, texturaElegida);
        PlayerPrefs.SetInt(ClaveVelocidadTextura, velocidadTexturaElegida);
        PlayerPrefs.Save();
        string nombre = NombreTexturaElegida();
        int n = dibujo.TexturaEnElegidas(texturaElegida, velocidadTexturaElegida, EscalaTexturaElegida);
        if (n > 0)
            dibujo.Mensaje("Relleno vivo: " + nombre + (n > 1 ? " (" + n + " figuras)" : ""));
        else
            dibujo.Mensaje("Relleno vivo: " + nombre + ". Toca dentro de una figura (o usa la cubeta)");
    }

    string NombreTexturaElegida()
    {
        return Trazo.NombresTextura[TexturaElegida] + (TexturaElegida > 0
            ? " · " + Trazo.NombresVelocidadTextura[VelocidadTexturaElegida] + " · x" + EscalaTexturaElegida.ToString("0.0") : "");
    }

    void IconoPaleta(Transform padre, System.Collections.Generic.List<Vector2[]> partes, Material m)
    {
        var go = new GameObject("Icono");
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = MallaIcono(partes);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static void PonerColorMaterial(Material m, Color c)
    {
        if (m == null)
            return;
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }

    // La figura rellenable bajo un punto (local del Dibujo). Si hay varias (una dentro de otra), la más pequeña.
    // toleranciaMundo: a qué distancia del plano de la figura todavía cuenta (metros).
    Trazo FiguraBajo(Vector3 local, float toleranciaMundo, bool soloConRelleno)
    {
        Trazo dentro = null;
        float mejor = float.MaxValue;
        float tolerancia = toleranciaMundo / Mathf.Max(1e-4f, dibujo.EscalaMundo);
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || t.oculto || t.Dibujando || (soloConRelleno && !t.relleno))
                continue;
            if (!t.DentroDeRelleno(local, tolerancia))
                continue;
            var b = t.GetComponent<MeshFilter>();
            float tam = b != null && b.sharedMesh != null ? b.sharedMesh.bounds.size.sqrMagnitude : 0f;
            if (tam < mejor)
            {
                mejor = tam;
                dentro = t;
            }
        }
        return dentro;
    }

    // ---------- Tocar DENTRO de una forma (solo con RELLENO abierto) ----------
    bool dedoEnRelleno;
    Trazo rellenoCandidato;
    float rellenoDesde;

    void RellenarAlTocar(Vector3 local, Vector3 punta)
    {
        Trazo dentro = FiguraBajo(local, 0.02f, false);
        if (dentro == null)
        {
            dedoEnRelleno = false;
            rellenoCandidato = null;
            return;
        }
        if (dedoEnRelleno)
            return; // ya se rellenó: hay que sacar el dedo para volver a rellenar
        if (dentro != rellenoCandidato)
        {
            rellenoCandidato = dentro;
            rellenoDesde = Time.time;
            return;
        }
        if (Time.time - rellenoDesde < EsperaRelleno)
            return;
        dedoEnRelleno = true;
        bool quitar = colorRellenoNuevo.a < 0.01f;
        dibujo.RellenarConColor(dentro, colorRellenoNuevo, TexturaElegida, VelocidadTexturaElegida, EscalaTexturaElegida);
        Burbuja(punta, 1.2f);
        MostrarEtiqueta(quitar ? "Relleno quitado" : "Relleno");
    }

    // ---------- Tamaño de la textura con dos dedos ----------
    Trazo pellizcoTrazo;
    float pellizcoDistancia, pellizcoEscala, pellizcoUltima, proximoPellizco;
    bool pellizcoMovio, pellizcoAvisado;

    // Con la paleta abierta: el pulgar y el índice derechos sobre el mismo relleno. Abrirlos o cerrarlos cambia el
    // tamaño de su textura (sin escalones). Devuelve true mientras el gesto está activo (no se rellena ni se toma color).
    bool RevisarPellizcoTextura()
    {
        if (!Der.valida || dibujo == null)
        {
            TerminarPellizcoTextura();
            return false;
        }
        Transform raiz = dibujo.transform;
        float escalaMundo = dibujo.EscalaMundo;
        Vector3 li = raiz.InverseTransformPoint(Der.indice);
        Vector3 lp = raiz.InverseTransformPoint(Der.pulgar);
        float abertura = Vector3.Distance(Der.indice, Der.pulgar);
        if (pellizcoTrazo == null)
        {
            // Empieza: los dos dedos tocando el mismo relleno, un poco abiertos (cerrados del todo es "agarrar").
            if (abertura < 0.012f)
                return false;
            Trazo bajoIndice = FiguraBajo(li, 0.025f, true);
            if (bajoIndice == null || FiguraBajo(lp, 0.025f, true) != bajoIndice)
                return false;
            pellizcoTrazo = bajoIndice;
            pellizcoDistancia = abertura;
            pellizcoEscala = bajoIndice.escalaTextura;
            pellizcoUltima = pellizcoEscala;
            pellizcoMovio = false;
            pellizcoAvisado = false;
            rellenoCandidato = null; // el dedo índice no rellena mientras tanto
            return true;
        }
        // Sigue mientras el punto entre los dos dedos esté sobre el relleno (a menos de 5 cm de su plano).
        Vector3 medio = (li + lp) * 0.5f;
        if (!Dibujo.Editable(pellizcoTrazo) || !pellizcoTrazo.relleno || !pellizcoTrazo.DentroDeRelleno(medio, 0.05f / escalaMundo))
        {
            TerminarPellizcoTextura();
            return false;
        }
        if (!pellizcoMovio)
        {
            if (Mathf.Abs(abertura - pellizcoDistancia) < ZonaMuertaPellizco)
                return true;
            if (pellizcoTrazo.texturaRelleno == 0)
            {
                if (!pellizcoAvisado)
                {
                    pellizcoAvisado = true;
                    dibujo.Mensaje("Este relleno es liso: elige una textura en RELLENO (las 2 de la paleta)");
                }
                return true;
            }
            pellizcoMovio = true;
            pellizcoDistancia = abertura; // desde aquí, sin saltos
            dibujo.GuardarParaDeshacer();
        }
        pellizcoUltima = Trazo.LimitarEscala(pellizcoEscala * abertura / Mathf.Max(0.004f, pellizcoDistancia));
        if (Time.time >= proximoPellizco)
        {
            proximoPellizco = Time.time + 0.05f;
            pellizcoTrazo.PonerEscalaTextura(pellizcoUltima);
        }
        PonerTextoCentro("Textura x" + pellizcoUltima.ToString("0.0"));
        return true;
    }

    void TerminarPellizcoTextura()
    {
        if (pellizcoTrazo == null)
            return;
        if (pellizcoMovio && dibujo != null)
        {
            pellizcoTrazo.PonerEscalaTextura(pellizcoUltima);
            GuardarEscalaTextura(pellizcoUltima);
            dibujo.TexturaCambiada();
            dibujo.Mensaje("Textura x" + pellizcoUltima.ToString("0.0"));
        }
        pellizcoTrazo = null;
        pellizcoMovio = false;
        dedoEnRelleno = true; // el índice sigue dentro: no rellena hasta que salga
    }
}

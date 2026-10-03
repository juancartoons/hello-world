using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Panel de arriba. Aparece cuando miras hacia arriba.
//  - ASA (barra de arriba): pellízcala con la derecha y arrastra el panel a donde quieras.
//    Mientras lo arrastras, pellizca también con la izquierda y separa/junta las manos = tamaño.
//    Después de moverlo (o con "Fijar aquí") se queda fijo y siempre visible. "Seguirme" lo devuelve a "mirar arriba".
//  - Línea de tiempo (hasta 2000 fotogramas): toca la barra con el índice derecho para ir a un fotograma.
//    "Zoom" cambia cuántos fotogramas caben en la barra (Todo, 400, 100, 25).
//    Pellizca una clave (marca naranja) y arrástrala para moverla a otro fotograma.
//  - Páginas: Animar (controles, capas, menú, exportar), Medios (videos, grabar el proceso, imágenes)
//    Bocas (lipsync) y Títere (caminar con los dedos).
public class PanelArriba : MonoBehaviour
{
    public static readonly int[] Ventanas = { 0, 400, 100, 25 }; // 0 = todo

    public ControlManos control;
    public Dibujo dibujo;
    public Animacion animacion;
    public Escenario escenario;
    public Lipsync lipsync;
    public Referencias referencias;
    public GrabadorProceso grabador;
    public ExportadorVideo exportador;
    public Titere titere;
    public GameObject contenido;
    [Tooltip("Barra de la línea de tiempo (un cubo hijo de Contenido)")]
    public Transform barra;
    public Transform cabezal;
    [Tooltip("Barra para pellizcar y mover el panel")]
    public Transform asa;
    [Tooltip("Otra barra igual, abajo del panel")]
    public Transform asaAbajo;
    public Material materialClave;
    public TMP_Text textoFotograma;

    [Header("Pestañas")]
    public GameObject paginaAnimar, paginaMedios, paginaBocas, paginaTitere;
    public BotonTocable btnPaginaAnimar, btnPaginaMedios, btnPaginaBocas, btnPaginaTitere, btnZoom, btnSeguir;
    [Tooltip("Botón \"?\" de la esquina: abre abajo una copia azul del panel que explica cada botón")]
    public BotonTocable btnAyuda;

    [Header("Animar")]
    public BotonTocable btnInicio, btnAnterior, btnPlay, btnSiguiente, btnClave, btnQuitarClave, btnFps;
    public BotonTocable btnPlano, btnFondo, btnGuardar, btnCargar, btnBorrarTodo, btnSvg, btnFoto;
    public BotonTocable[] btnCapas = new BotonTocable[0];
    public BotonTocable[] btnVer = new BotonTocable[0];

    [Header("Medios")]
    public BotonTocable btnGrabar, btnVideoProceso, btnVelocidad, btnVideoAnim, btnImagenMas, btnImagenMenos, btnTemblor;
    public BotonTocable btnHebras, btnGrosorVivo, btnCicloTemblor, btnImagenesVer;
    public BotonTocable btnSuavidad, btnVelocidadTemblor, btnBoceto, btnUnirPlano, btnIman;
    public TMP_Text textoMedios;

    [Header("Bocas")]
    public BotonTocable[] btnBocas = new BotonTocable[0];
    public BotonTocable btnModoBoca, btnVoz, btnAudio, btnLipsync, btnQuitarAudio;
    public TMP_Text textoBocas;

    [Header("Títere")]
    public BotonTocable btnMuneco, btnTitere, btnGrabarTitere, btnPierna1, btnPierna2, btnCuerpo, btnVoltear;
    public BotonTocable btnPosar, btnCiclo, btnGuardarCiclo, btnBrazo1, btnBrazo2, btnPiso;
    public TMP_Text textoTitere;

    [Tooltip("Qué tanto hay que mirar hacia arriba para que aparezca (0 a 1)")]
    public float mirarArribaEntra = 0.35f;
    public float mirarArribaSale = 0.15f;
    public float distancia = 0.32f;
    public float altura = 0.2f;
    public float suavizado = 12f;

    bool visible;
    bool anclado;          // lo moviste: se queda fijo y siempre visible
    bool arrastrando;
    bool escalando;
    Vector3 desfaseAsa;
    float distanciaEscala;
    float escalaInicio = 1f;
    float escala = 1f;
    Quaternion giro = Quaternion.identity;
    readonly List<Transform> marcas = new List<Transform>();
    int claveArrastrada = -1;
    int destinoClave;
    int zoom = 2;          // índice en Ventanas
    int inicioVentana;
    int pagina;

    int Ventana => Ventanas[zoom] <= 0 ? Animacion.TotalFotogramas : Mathf.Min(Ventanas[zoom], Animacion.TotalFotogramas);

    // ---------- Ayuda ----------
    // El botón "?" crea una copia exacta de este panel, abajo y en azul. En la copia, cada botón
    // NO hace nada: solo explica para qué sirve (con los pasos, si se combina con otros botones o gestos).
    static bool creandoAyuda;
    bool esAyuda;            // este panel es la copia azul
    PanelArriba original;    // (en la copia) el panel de verdad
    PanelArriba ayuda;       // (en el de verdad) la copia azul
    bool ayudaAbierta;
    TMP_Text textoAyuda;
    const float BajarAyuda = 0.42f;

    void Awake()
    {
        if (creandoAyuda)
            esAyuda = true;
    }

    void Start()
    {
        if (esAyuda)
        {
            PrepararAyuda();
            return;
        }
        if (control == null) control = FindFirstObjectByType<ControlManos>();
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (animacion == null) animacion = FindFirstObjectByType<Animacion>();
        if (escenario == null) escenario = FindFirstObjectByType<Escenario>();
        if (lipsync == null) lipsync = FindFirstObjectByType<Lipsync>();
        if (referencias == null) referencias = FindFirstObjectByType<Referencias>();
        if (grabador == null) grabador = FindFirstObjectByType<GrabadorProceso>();
        if (exportador == null) exportador = FindFirstObjectByType<ExportadorVideo>();
        if (titere == null) titere = FindFirstObjectByType<Titere>();

        Conectar(btnPaginaAnimar, () => PonerPagina(0));
        Conectar(btnPaginaMedios, () => PonerPagina(1));
        Conectar(btnPaginaBocas, () => PonerPagina(2));
        Conectar(btnPaginaTitere, () => PonerPagina(3));
        Conectar(btnZoom, CambiarZoom);
        Conectar(btnSeguir, AlternarFijo);
        Conectar(btnAyuda, AlternarAyuda);

        if (animacion != null)
        {
            Conectar(btnInicio, animacion.Inicio);
            Conectar(btnAnterior, animacion.Anterior);
            Conectar(btnPlay, animacion.AlternarReproducir);
            Conectar(btnSiguiente, animacion.Siguiente);
            Conectar(btnClave, animacion.AgregarClave);
            Conectar(btnQuitarClave, animacion.QuitarClave);
            Conectar(btnFps, animacion.CambiarFps);
        }
        Conectar(btnFondo, SiguienteFondo);
        Conectar(btnFoto, TomarFoto);
        if (dibujo != null)
        {
            Conectar(btnPlano, dibujo.AlternarPlano);
            Conectar(btnGuardar, dibujo.Guardar);
            Conectar(btnCargar, AlternarArchivos);
            Conectar(btnBorrarTodo, dibujo.BorrarTodo);
            Conectar(btnSvg, dibujo.ExportarSVG);
            Conectar(btnUnirPlano, dibujo.AlternarUnirPlano);
            Conectar(btnIman, dibujo.AlternarIman);
            for (int i = 0; i < btnCapas.Length; i++)
            {
                int capa = i;
                Conectar(btnCapas[i], () => dibujo.SeleccionarCapa(capa));
            }
            for (int i = 0; i < btnVer.Length; i++)
            {
                int capa = i;
                Conectar(btnVer[i], () => dibujo.AlternarVerCapa(capa));
            }
        }

        if (grabador != null)
            Conectar(btnGrabar, grabador.Alternar);
        if (exportador != null)
        {
            Conectar(btnVideoProceso, exportador.ExportarProceso);
            Conectar(btnVelocidad, exportador.CambiarVelocidad);
            Conectar(btnVideoAnim, exportador.ExportarAnimacion);
        }
        if (dibujo != null && dibujo.temblor != null)
        {
            Conectar(btnTemblor, dibujo.temblor.Siguiente);
            Conectar(btnHebras, dibujo.temblor.SiguienteHebras);
            Conectar(btnGrosorVivo, dibujo.temblor.AlternarGrosor);
            Conectar(btnCicloTemblor, dibujo.temblor.AlternarCiclo);
            Conectar(btnSuavidad, dibujo.temblor.SiguienteSuavidad);
            Conectar(btnVelocidadTemblor, dibujo.temblor.SiguienteVelocidad);
            Conectar(btnBoceto, dibujo.temblor.SiguienteBoceto);
        }
        if (referencias != null)
        {
            Conectar(btnImagenMas, () => referencias.AgregarSiguiente(control != null ? control.Cabeza : null));
            Conectar(btnImagenMenos, referencias.QuitarSeleccionada);
            Conectar(btnImagenesVer, referencias.AlternarTodas);
        }

        if (lipsync != null)
        {
            for (int i = 0; i < btnBocas.Length; i++)
            {
                int boca = i;
                Conectar(btnBocas[i], () => lipsync.TocarBoca(boca));
            }
            Conectar(btnModoBoca, lipsync.AlternarModo);
            Conectar(btnVoz, lipsync.AlternarVoz);
            Conectar(btnAudio, lipsync.SiguienteAudio);
            Conectar(btnLipsync, lipsync.Generar);
            Conectar(btnQuitarAudio, lipsync.QuitarAudio);
        }

        if (titere != null)
        {
            Conectar(btnMuneco, titere.CrearOParar);
            Conectar(btnTitere, titere.CambiarTipo);
            Conectar(btnGrabarTitere, titere.Grabar);
            Conectar(btnPierna1, () => titere.AsignarPierna(1));
            Conectar(btnPierna2, () => titere.AsignarPierna(2));
            Conectar(btnCuerpo, titere.AlternarCuerpo);
            Conectar(btnVoltear, titere.Voltear);
            Conectar(btnPosar, titere.AlternarPosar);
            Conectar(btnCiclo, titere.CambiarCiclo);
            Conectar(btnGuardarCiclo, titere.GuardarCiclo);
            Conectar(btnBrazo1, () => titere.AsignarBrazo(1));
            Conectar(btnBrazo2, () => titere.AsignarBrazo(2));
            Conectar(btnPiso, titere.AlternarPiso);
        }

        PonerPagina(0);
        if (contenido != null)
            contenido.SetActive(false);
    }

    void PonerPagina(int p)
    {
        pagina = p;
        if (paginaAnimar != null) paginaAnimar.SetActive(p == 0);
        if (paginaMedios != null) paginaMedios.SetActive(p == 1);
        if (paginaBocas != null) paginaBocas.SetActive(p == 2);
        if (paginaTitere != null) paginaTitere.SetActive(p == 3);
        if (btnPaginaAnimar != null) btnPaginaAnimar.Marcar(p == 0);
        if (btnPaginaMedios != null) btnPaginaMedios.Marcar(p == 1);
        if (btnPaginaBocas != null) btnPaginaBocas.Marcar(p == 2);
        if (btnPaginaTitere != null) btnPaginaTitere.Marcar(p == 3);
    }

    void CambiarZoom()
    {
        zoom = (zoom + 1) % Ventanas.Length;
        inicioVentana = 0;
        AjustarVentana(true);
        if (dibujo != null)
            dibujo.Mensaje(Ventanas[zoom] <= 0 ? "Barra: todos los fotogramas" : "Barra: " + Ventanas[zoom] + " fotogramas");
    }

    // "Fijar aquí": el panel se queda donde está y siempre visible. "Seguirme": vuelve a aparecer al mirar arriba.
    void AlternarFijo()
    {
        if (anclado)
        {
            Seguirme();
            return;
        }
        anclado = true;
        if (dibujo != null)
            dibujo.Mensaje("Panel fijo. Pellizca el asa (arriba) para moverlo");
    }

    void Seguirme()
    {
        anclado = false;
        visible = false; // al volver a mirar arriba aparece frente a ti
        if (contenido != null)
            contenido.SetActive(false);
        if (dibujo != null)
            dibujo.Mensaje("El panel aparece al mirar arriba");
    }

    void SiguienteFondo()
    {
        if (escenario == null)
            return;
        escenario.SiguienteModo();
        if (dibujo != null)
            dibujo.Mensaje("Fondo: " + Escenario.Nombres[escenario.modo]);
    }

    // La foto se toma sin este panel en medio.
    void TomarFoto()
    {
        if (dibujo == null)
            return;
        bool estaba = contenido != null && contenido.activeSelf;
        if (estaba)
            contenido.SetActive(false);
        dibujo.TomarFoto();
        if (estaba)
            contenido.SetActive(true);
    }

    static void Conectar(BotonTocable boton, UnityAction accion)
    {
        if (boton != null)
            boton.alTocar.AddListener(accion);
    }

    // ¿Este punto está sobre el panel? (Así los pellizcos ahí no seleccionan líneas.)
    public bool Contiene(Vector3 mundo)
    {
        if (!esAyuda && ayuda != null && ayuda.Contiene(mundo))
            return true;
        if (contenido == null || !contenido.activeInHierarchy)
            return false;
        Vector3 l = transform.InverseTransformPoint(mundo);
        float abajo = esAyuda || (listaArchivos != null && listaArchivos.activeSelf) ? -0.33f : -0.175f;
        return Mathf.Abs(l.x) < 0.27f && l.y > abajo && l.y < 0.225f && Mathf.Abs(l.z) < 0.06f;
    }

    void Update()
    {
        if (esAyuda)
        {
            SeguirOriginal();
            return;
        }
        var cabeza = control != null ? control.Cabeza : null;
        if (cabeza == null || contenido == null)
            return;

        float arriba = cabeza.forward.y;
        bool ver = anclado || (visible ? arriba > mirarArribaSale : arriba > mirarArribaEntra);
        if (ver != visible)
        {
            visible = ver;
            contenido.SetActive(ver);
            if (ver && !anclado)
            {
                // Se queda en la dirección hacia donde mirabas al levantar la vista.
                Vector3 adelante = cabeza.forward;
                adelante.y = 0f;
                if (adelante.sqrMagnitude < 1e-4f)
                    adelante = cabeza.up;
                adelante.y = 0f;
                giro = Quaternion.LookRotation(adelante.sqrMagnitude > 1e-6f ? adelante.normalized : Vector3.forward, Vector3.up);
                Colocar(cabeza, 1f);
            }
        }
        if (!ver)
        {
            if (arrastrando)
                SoltarAsa();
            return;
        }

        RevisarAsa(cabeza);
        if (!anclado)
            Colocar(cabeza, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
        if (!arrastrando)
            RevisarBarra();
        Refrescar();
    }

    void Colocar(Transform cabeza, float a)
    {
        Vector3 pos = cabeza.position + giro * new Vector3(0f, altura, distancia);
        Quaternion rot = Quaternion.LookRotation(pos - cabeza.position, Vector3.up);
        transform.position = Vector3.Lerp(transform.position, pos, a);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, a);
    }

    // ---------- Mover y cambiar el tamaño del panel ----------

    void RevisarAsa(Transform cabeza)
    {
        if (control == null || asa == null)
            return;
        var der = control.Der;
        var izq = control.Izq;
        if (arrastrando)
        {
            if (!der.valida || !der.pellizco)
            {
                SoltarAsa();
                return;
            }
            transform.position = der.PuntoPellizco + desfaseAsa;
            Vector3 mirar = transform.position - cabeza.position;
            if (mirar.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(mirar, Vector3.up);

            // Con la izquierda también pellizcando: separar/juntar las manos = tamaño.
            if (izq.valida && izq.pellizco)
            {
                float d = Vector3.Distance(izq.PuntoPellizco, der.PuntoPellizco);
                if (!escalando)
                {
                    escalando = true;
                    distanciaEscala = Mathf.Max(0.02f, d);
                    escalaInicio = escala;
                }
                else
                {
                    escala = Mathf.Clamp(escalaInicio * d / distanciaEscala, 0.4f, 3f);
                    transform.localScale = Vector3.one * escala;
                }
            }
            else
            {
                escalando = false;
            }
            return;
        }
        if (!der.valida || !der.empezoPellizco)
            return;
        Vector3 l = transform.InverseTransformPoint(der.PuntoPellizco);
        if (!EnAsa(asa, l) && !EnAsa(asaAbajo, l))
            return;
        arrastrando = true;
        escalando = false;
        anclado = true;
        desfaseAsa = transform.position - der.PuntoPellizco;
        control.Ocupado = true;
    }

    static bool EnAsa(Transform barra, Vector3 l)
    {
        if (barra == null)
            return false;
        Vector3 a = barra.localPosition;
        return Mathf.Abs(l.x - a.x) < barra.localScale.x * 0.5f + 0.02f
               && Mathf.Abs(l.y - a.y) < 0.025f && Mathf.Abs(l.z) < 0.05f;
    }

    void SoltarAsa()
    {
        arrastrando = false;
        escalando = false;
        if (control != null)
            control.Ocupado = false;
    }

    void OnDisable()
    {
        if (arrastrando)
            SoltarAsa();
    }

    // ---------- Línea de tiempo ----------

    // Tocar la barra con el índice derecho = ir a ese fotograma (se puede arrastrar).
    // Pellizcar una clave = arrastrarla a otro fotograma.
    void RevisarBarra()
    {
        if (barra == null || animacion == null || control == null)
            return;
        var der = control.Der;

        if (claveArrastrada >= 0)
        {
            if (!der.valida || !der.pellizco)
            {
                animacion.MoverClave(claveArrastrada, destinoClave);
                claveArrastrada = -1;
                return;
            }
            destinoClave = FotogramaEn(barra.InverseTransformPoint(der.PuntoPellizco).x);
            return;
        }
        if (!der.valida)
            return;

        if (der.empezoPellizco)
        {
            Vector3 lp = barra.InverseTransformPoint(der.PuntoPellizco);
            bool cerca = Mathf.Abs(lp.y) < 3f && Mathf.Abs(lp.z) < 4f && lp.x > -0.56f && lp.x < 0.56f;
            if (cerca)
            {
                int f = FotogramaEn(lp.x);
                int mejor = -1;
                // Tolerancia: unos 1.3 cm de barra, en fotogramas.
                int tolerancia = Mathf.Max(1, Mathf.RoundToInt(0.03f * Ventana));
                foreach (var c in animacion.claves)
                {
                    if (!EnVentana(c.fotograma))
                        continue;
                    int d = Mathf.Abs(c.fotograma - f);
                    if (d <= tolerancia)
                    {
                        tolerancia = d;
                        mejor = c.fotograma;
                    }
                }
                if (mejor >= 0)
                {
                    claveArrastrada = mejor;
                    destinoClave = mejor;
                    return;
                }
            }
        }
        if (der.pellizco)
            return; // mientras pellizcas no se mueve el cabezal

        Vector3 local = barra.InverseTransformPoint(der.indice);
        bool encima = Mathf.Abs(local.y) < 1.2f && Mathf.Abs(local.z) < 2.5f && local.x > -0.55f && local.x < 0.55f;
        if (!encima)
            return;
        int fotograma = FotogramaEn(local.x);
        if (fotograma != animacion.Fotograma)
            animacion.IrA(fotograma);
    }

    // La ventana sigue al cabezal: si se sale, se vuelve a centrar.
    void AjustarVentana(bool forzar)
    {
        int v = Ventana;
        int maxInicio = Mathf.Max(0, Animacion.TotalFotogramas - v);
        int f = animacion != null ? animacion.Fotograma : 0;
        if (forzar || f < inicioVentana || f > inicioVentana + v - 1)
            inicioVentana = f - v / 2;
        inicioVentana = Mathf.Clamp(inicioVentana, 0, maxInicio);
    }

    bool EnVentana(int f)
    {
        return f >= inicioVentana && f <= inicioVentana + Ventana - 1;
    }

    int FotogramaEn(float xLocal)
    {
        int f = inicioVentana + Mathf.RoundToInt(Mathf.Clamp01(xLocal + 0.5f) * (Ventana - 1));
        return Mathf.Clamp(f, 0, Animacion.TotalFotogramas - 1);
    }

    Vector3 PosicionEnBarra(int f, float z)
    {
        float x = (f - inicioVentana) / (float)Mathf.Max(1, Ventana - 1) - 0.5f;
        Vector3 mundo = barra.TransformPoint(new Vector3(x, 0f, 0f));
        Vector3 local = contenido.transform.InverseTransformPoint(mundo);
        local.z = z;
        return local;
    }

    void Refrescar()
    {
        if (animacion != null && barra != null)
        {
            if (claveArrastrada < 0)
                AjustarVentana(false);

            if (cabezal != null)
            {
                bool verCabezal = EnVentana(animacion.Fotograma);
                if (cabezal.gameObject.activeSelf != verCabezal)
                    cabezal.gameObject.SetActive(verCabezal);
                if (verCabezal)
                    cabezal.localPosition = PosicionEnBarra(animacion.Fotograma, -0.006f);
            }

            int n = 0;
            foreach (var c in animacion.claves)
            {
                int fm = c.fotograma == claveArrastrada ? destinoClave : c.fotograma;
                if (!EnVentana(fm))
                    continue;
                if (n >= marcas.Count)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Marca";
                    Destroy(go.GetComponent<Collider>());
                    go.transform.SetParent(contenido.transform, false);
                    go.transform.localScale = new Vector3(0.004f, 0.03f, 0.004f);
                    var r = go.GetComponent<Renderer>();
                    if (materialClave != null)
                        r.sharedMaterial = materialClave;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    marcas.Add(go.transform);
                }
                var m = marcas[n++];
                if (!m.gameObject.activeSelf)
                    m.gameObject.SetActive(true);
                m.localPosition = PosicionEnBarra(fm, -0.005f);
                float ancho = Ventana > 400 ? 0.002f : 0.004f;
                m.localScale = new Vector3(ancho, c.fotograma == claveArrastrada ? 0.045f : 0.03f, 0.004f);
            }
            for (int i = n; i < marcas.Count; i++)
                if (marcas[i].gameObject.activeSelf)
                    marcas[i].gameObject.SetActive(false);

            if (textoFotograma != null)
            {
                string texto = "Fotograma " + (animacion.Fotograma + 1) + " / " + Animacion.TotalFotogramas;
                if (animacion.EsClave(animacion.Fotograma))
                    texto += "  (clave)";
                if (Ventanas[zoom] > 0)
                    texto += "   ·   barra " + (inicioVentana + 1) + "-" + (inicioVentana + Ventana);
                textoFotograma.text = texto;
            }
            if (btnPlay != null)
                btnPlay.PonerTexto(animacion.Reproduciendo ? "Pausa" : "Play");
            if (btnFps != null)
                btnFps.PonerTexto(animacion.fotogramasPorSegundo + " fps");
        }
        if (btnZoom != null)
            btnZoom.PonerTexto(Ventanas[zoom] <= 0 ? "Zoom: todo" : "Zoom: " + Ventanas[zoom]);
        if (btnAyuda != null)
            btnAyuda.Marcar(ayuda != null && ayudaAbierta);
        if (btnSeguir != null)
        {
            btnSeguir.PonerTexto(anclado ? "Seguirme" : "Fijar aquí");
            btnSeguir.Marcar(anclado);
        }

        if (pagina == 0)
            RefrescarAnimar();
        else if (pagina == 1)
            RefrescarMedios();
        else if (pagina == 2)
            RefrescarBocas();
        else
            RefrescarTitere();
    }

    void RefrescarTitere()
    {
        if (titere == null)
            return;
        var p = titere.Elegido;
        if (btnTitere != null)
            btnTitere.PonerTexto("Tipo: " + titere.NombreTipo);
        if (btnMuneco != null)
        {
            bool mueve = titere.Encendido || titere.Grabando;
            btnMuneco.PonerTexto(mueve ? "Parar" : "Crear " + titere.NombreTipo);
            btnMuneco.Marcar(mueve);
        }
        if (btnPosar != null)
        {
            btnPosar.PonerTexto(titere.Posando ? "Terminar" : "Posar dedos");
            btnPosar.Marcar(titere.Posando);
        }
        if (btnCiclo != null)
            btnCiclo.PonerTexto("Ciclo: " + titere.NombreCiclo);
        if (btnGrabarTitere != null)
        {
            btnGrabarTitere.PonerTexto(titere.Grabando ? "Parar" : "Grabar");
            btnGrabarTitere.Marcar(titere.Grabando);
        }
        if (btnVoltear != null)
            btnVoltear.Marcar(p != null && p.voltear);
        if (btnPierna1 != null)
            btnPierna1.Marcar(p != null && dibujo != null && dibujo.BuscarPorId(p.pierna1) != null);
        if (btnPierna2 != null)
            btnPierna2.Marcar(p != null && dibujo != null && dibujo.BuscarPorId(p.pierna2) != null);
        if (btnBrazo1 != null)
            btnBrazo1.Marcar(p != null && dibujo != null && dibujo.BuscarPorId(p.brazo1) != null);
        if (btnBrazo2 != null)
            btnBrazo2.Marcar(p != null && dibujo != null && dibujo.BuscarPorId(p.brazo2) != null);
        if (textoTitere != null)
            textoTitere.text = titere.Posando
                ? "Acomoda las piernas con el índice y el medio · pellizco IZQUIERDO = guardar clave"
                : titere.Encendido
                ? "Mano a los lados = caminar/correr · golpe rápido hacia arriba = saltar · palma arriba (o Parar) = apagar"
                : "Crear = aparece y se enciende solo · choca esos cinco para encender otro · elegido: " + (p != null ? p.nombre : "ninguno") + " · pisos: " + titere.pisos.Count;
    }

    void RefrescarAnimar()
    {
        if (btnPlano != null && dibujo != null)
        {
            btnPlano.PonerTexto(dibujo.plano ? "Plano (2D)" : "Libre (3D)");
            btnPlano.Marcar(dibujo.plano);
        }
        if (btnFondo != null && escenario != null)
            btnFondo.PonerTexto("Fondo: " + Escenario.Nombres[escenario.modo]);

        if (dibujo == null)
            return;
        for (int i = 0; i < btnCapas.Length && i < dibujo.capas.Count; i++)
            if (btnCapas[i] != null)
                btnCapas[i].Marcar(i == dibujo.capaActual);
        for (int i = 0; i < btnVer.Length && i < dibujo.capas.Count; i++)
        {
            if (btnVer[i] == null)
                continue;
            bool capaVisible = dibujo.capas[i].visible;
            btnVer[i].PonerTexto(capaVisible ? "Ver" : "Oculta");
            btnVer[i].Marcar(!capaVisible);
        }
    }

    void RefrescarMedios()
    {
        bool grabando = grabador != null && grabador.Grabando;
        if (btnGrabar != null)
        {
            btnGrabar.PonerTexto(grabando ? "Detener" : "Grabar");
            btnGrabar.Marcar(grabando);
        }
        if (btnVelocidad != null && exportador != null)
            btnVelocidad.PonerTexto("Vel x" + exportador.velocidad);
        if (dibujo != null && dibujo.temblor != null)
        {
            var tb = dibujo.temblor;
            if (btnTemblor != null)
            {
                btnTemblor.PonerTexto("Temblor: " + tb.NombreNivel);
                btnTemblor.Marcar(tb.Nivel > 0);
            }
            if (btnHebras != null)
            {
                btnHebras.PonerTexto("Hebras: " + tb.Hebras);
                btnHebras.Marcar(tb.Hebras > 1);
            }
            if (btnGrosorVivo != null)
                btnGrosorVivo.Marcar(tb.GrosorVivo);
            if (btnCicloTemblor != null)
            {
                btnCicloTemblor.PonerTexto(tb.Ciclo3 ? "Ciclo de 3" : "Libre");
                btnCicloTemblor.Marcar(tb.Ciclo3);
            }
            if (btnSuavidad != null)
                btnSuavidad.PonerTexto("Suavidad: " + tb.NombreSuavidad);
            if (btnVelocidadTemblor != null)
                btnVelocidadTemblor.PonerTexto("Velocidad: " + tb.CambiosPorSegundo);
            if (btnBoceto != null)
            {
                btnBoceto.PonerTexto("Boceto: " + tb.NombreBoceto);
                btnBoceto.Marcar(tb.Boceto > 0);
            }
        }
        if (btnUnirPlano != null && dibujo != null)
        {
            btnUnirPlano.PonerTexto(dibujo.CapaActual.unido ? "Plano: unido" : "Plano: propio");
            btnUnirPlano.Marcar(dibujo.CapaActual.unido);
            if (btnIman != null)
            {
                btnIman.PonerTexto(dibujo.CapaActual.iman ? "Imán: Sí" : "Imán: No");
                btnIman.Marcar(dibujo.CapaActual.iman);
            }
        }
        if (btnImagenesVer != null && referencias != null)
        {
            btnImagenesVer.PonerTexto(referencias.Ocultas ? "Imágenes: ocultas" : "Imágenes: ver");
            btnImagenesVer.Marcar(referencias.Ocultas);
        }
        if (textoMedios == null)
            return;
        string texto;
        if (ExportadorVideo.Exportando)
            texto = "Haciendo el video... (mira los avisos)";
        else if (grabando)
            texto = "Grabando el proceso: " + GrabadorProceso.Formato(grabador.Duracion);
        else if (grabador != null && grabador.muestras.Count > 1)
            texto = "Grabación lista: " + GrabadorProceso.Formato(grabador.Duracion) + " · toca Video proceso";
        else
            texto = dibujo != null ? "Las líneas vivas y el boceto son de la capa activa: " + dibujo.CapaActual.nombre : "";
        textoMedios.text = texto;
    }

    void RefrescarBocas()
    {
        if (lipsync == null)
            return;
        for (int i = 0; i < btnBocas.Length; i++)
            if (btnBocas[i] != null)
                btnBocas[i].Marcar(lipsync.TieneBoca(i));
        if (btnModoBoca != null)
            btnModoBoca.PonerTexto(lipsync.modoGuardar ? "Modo: Guardar" : "Modo: Probar");
        if (btnVoz != null)
        {
            btnVoz.PonerTexto(lipsync.Grabando ? "Parar " + GrabadorProceso.Formato(lipsync.TiempoGrabando) : "Voz");
            btnVoz.Marcar(lipsync.Grabando);
        }
        if (textoBocas == null)
            return;
        string audio = lipsync.Clip != null ? lipsync.Clip.name + " (" + lipsync.Clip.length.ToString("0.0") + " s)" : "sin audio";
        textoBocas.text = "Capa de la boca: " + (dibujo != null ? dibujo.capaActual + 1 : 1) + " · Audio: " + audio
                          + "\nGuardar: mueve los nodos de la boca y toca una forma · Lipsync crea las claves";
    }

    // ==================== Abrir dibujos guardados ====================
    // "Cargar" muestra abajo la lista de tus dibujos (el más reciente primero). Toca uno para abrirlo.

    GameObject listaArchivos;

    void AlternarArchivos()
    {
        if (listaArchivos != null && listaArchivos.activeSelf)
        {
            CerrarArchivos();
            return;
        }
        if (dibujo == null || btnCargar == null || btnCargar.etiqueta == null || contenido == null)
            return;
        var rutas = dibujo.ListaArchivos(12);
        if (rutas.Count == 0)
        {
            dibujo.Mensaje("Aún no has guardado ningún dibujo");
            return;
        }
        ayudaAbierta = false;
        if (listaArchivos != null)
            Destroy(listaArchivos);
        listaArchivos = new GameObject("ListaArchivos");
        listaArchivos.SetActive(false); // así los botones se arman antes de despertar
        listaArchivos.transform.SetParent(contenido.transform, false);

        var fondo = contenido.transform.Find("Fondo");
        if (fondo != null)
        {
            var hoja = Instantiate(fondo.gameObject, listaArchivos.transform);
            hoja.transform.localPosition = new Vector3(0f, -0.25f, 0.006f);
            hoja.transform.localScale = new Vector3(0.5f, 0.15f, 1f);
        }
        for (int i = 0; i < rutas.Count; i++)
        {
            string ruta = rutas[i];
            int fila = i / 3, columna = i % 3;
            var pos = new Vector3(-0.155f + columna * 0.155f, -0.195f - fila * 0.034f, 0f);
            var go = Instantiate(btnCargar.gameObject, listaArchivos.transform);
            go.name = "Archivo";
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(0.145f, 0.028f, 0.008f);
            var b = go.GetComponent<BotonTocable>();
            var texto = Instantiate(btnCargar.etiqueta.gameObject, listaArchivos.transform).GetComponent<TMP_Text>();
            texto.rectTransform.localPosition = pos + new Vector3(0f, 0f, -0.0046f);
            texto.rectTransform.localRotation = Quaternion.identity;
            texto.rectTransform.sizeDelta = new Vector2(0.135f, 0.022f);
            string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
            string fecha = System.IO.File.GetLastWriteTime(ruta).ToString("dd/MM HH:mm");
            texto.text = nombre + "  ·  " + fecha;
            b.etiqueta = texto;
            b.alTocar.RemoveAllListeners();
            b.alTocar.AddListener(() =>
            {
                CerrarArchivos();
                dibujo.AbrirArchivo(ruta);
            });
            b.Marcar(dibujo.NombreArchivo == nombre);
        }
        listaArchivos.SetActive(true);
        dibujo.Mensaje("Toca un dibujo para abrirlo (Cargar otra vez = cerrar)");
    }

    void CerrarArchivos()
    {
        if (listaArchivos != null)
            listaArchivos.SetActive(false);
    }

    // ==================== Ayuda (copia azul) ====================

    void AlternarAyuda()
    {
        CerrarArchivos();
        if (ayuda == null)
        {
            GameObject copia;
            creandoAyuda = true;
            try
            {
                copia = Instantiate(gameObject, transform.parent);
            }
            finally
            {
                creandoAyuda = false;
            }
            copia.name = "PanelAyuda";
            ayuda = copia.GetComponent<PanelArriba>();
            ayuda.original = this;
            ayudaAbierta = true;
        }
        else
        {
            ayudaAbierta = !ayudaAbierta;
        }
        if (dibujo != null)
            dibujo.Mensaje(ayudaAbierta ? "Ayuda: toca cualquier botón del panel azul (abajo)" : "Ayuda cerrada");
    }

    void CerrarAyuda()
    {
        ayudaAbierta = false;
    }

    // La copia azul va justo debajo del panel de verdad (y se esconde con él).
    void SeguirOriginal()
    {
        if (original == null)
        {
            Destroy(gameObject);
            return;
        }
        bool ver = original.ayudaAbierta && original.contenido != null && original.contenido.activeSelf;
        if (contenido != null && contenido.activeSelf != ver)
            contenido.SetActive(ver);
        if (!ver)
            return;
        var o = original.transform;
        transform.localScale = o.localScale;
        transform.SetPositionAndRotation(o.position - o.up * (BajarAyuda * o.lossyScale.y), o.rotation);
    }

    void PrepararAyuda()
    {
        var copiaLista = contenido != null ? contenido.transform.Find("ListaArchivos") : null;
        if (copiaLista != null)
        {
            copiaLista.SetParent(null, false); // fuera de la copia (así sus botones no cuentan)
            Destroy(copiaLista.gameObject);
        }
        // 1) Todo en azul.
        var mapa = new Dictionary<Material, Material>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.GetComponent<TMP_Text>() != null || r.sharedMaterial == null)
                continue;
            r.sharedMaterial = Azul(r.sharedMaterial, mapa);
        }
        var botones = GetComponentsInChildren<BotonTocable>(true);
        foreach (var b in botones)
        {
            b.materialNormal = Azul(b.materialNormal, mapa);
            b.materialMarcado = Azul(b.materialMarcado, mapa);
            b.alTocar.RemoveAllListeners();
        }

        // 2) El cuadro donde sale la explicación (debajo de la copia).
        if (contenido != null && textoFotograma != null)
        {
            var hoja = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hoja.name = "FondoAyuda";
            Destroy(hoja.GetComponent<Collider>());
            hoja.transform.SetParent(contenido.transform, false);
            hoja.transform.localPosition = new Vector3(0f, -0.237f, 0.006f);
            hoja.transform.localScale = new Vector3(0.5f, 0.14f, 1f);
            var rh = hoja.GetComponent<Renderer>();
            var fondo = contenido.transform.Find("Fondo");
            if (fondo != null && fondo.GetComponent<Renderer>() != null)
                rh.sharedMaterial = fondo.GetComponent<Renderer>().sharedMaterial;
            rh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rh.receiveShadows = false;

            var go = Instantiate(textoFotograma.gameObject, contenido.transform);
            go.name = "TextoAyuda";
            textoAyuda = go.GetComponent<TMP_Text>();
            textoAyuda.rectTransform.localPosition = new Vector3(0f, -0.237f, -0.002f);
            textoAyuda.rectTransform.localRotation = Quaternion.identity;
            textoAyuda.rectTransform.sizeDelta = new Vector2(0.47f, 0.128f);
            textoAyuda.alignment = TextAlignmentOptions.TopLeft;
            textoAyuda.fontStyle = FontStyles.Normal;
            textoAyuda.enableAutoSizing = true;
            textoAyuda.fontSizeMin = 0.04f;
            textoAyuda.fontSizeMax = 0.13f;
            textoAyuda.color = new Color(0.05f, 0.1f, 0.3f);
            MostrarAyuda("AYUDA\nEste panel azul es una copia del de arriba. Toca cualquier botón aquí y te explico para qué sirve " +
                         "y cómo usarlo. Aquí los botones no cambian nada de tu dibujo.\nToca \"?\" para cerrar la ayuda.");
        }

        // 3) Cada botón explica lo suyo (las pestañas, además, cambian de página en la copia).
        var textos = TextosAyuda();
        Conectar(btnPaginaAnimar, () => PonerPagina(0));
        Conectar(btnPaginaMedios, () => PonerPagina(1));
        Conectar(btnPaginaBocas, () => PonerPagina(2));
        Conectar(btnPaginaTitere, () => PonerPagina(3));
        foreach (var b in botones)
        {
            if (b == btnAyuda)
                continue;
            string texto;
            if (!textos.TryGetValue(b, out texto))
                texto = "Este botón: " + (b.etiqueta != null ? b.etiqueta.text : b.name);
            Conectar(b, () => MostrarAyuda(texto));
        }
        Conectar(btnAyuda, () => { if (original != null) original.CerrarAyuda(); });
        PonerPagina(original != null ? original.pagina : 0);
        if (contenido != null)
            contenido.SetActive(true);
    }

    void MostrarAyuda(string texto)
    {
        if (textoAyuda != null)
            textoAyuda.text = texto;
    }

    // Un color azul con la misma claridad que el original (blanco → celeste, negro → azul oscuro).
    static Material Azul(Material m, Dictionary<Material, Material> mapa)
    {
        if (m == null)
            return null;
        Material copia;
        if (mapa.TryGetValue(m, out copia))
            return copia;
        Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
        float l = c.grayscale;
        var azul = new Color(l * 0.72f, l * 0.84f + 0.04f, Mathf.Min(1f, l * 0.55f + 0.42f), c.a);
        copia = new Material(m);
        if (copia.HasProperty("_BaseColor")) copia.SetColor("_BaseColor", azul);
        if (copia.HasProperty("_Color")) copia.SetColor("_Color", azul);
        mapa[m] = copia;
        mapa[copia] = copia;
        return copia;
    }

    static void Poner(Dictionary<BotonTocable, string> d, BotonTocable b, string texto)
    {
        if (b != null)
            d[b] = texto;
    }

    Dictionary<BotonTocable, string> TextosAyuda()
    {
        var d = new Dictionary<BotonTocable, string>();
        // Pestañas y barra de arriba
        Poner(d, btnPaginaAnimar, "ANIMAR (página)\nControles de la animación, capas y menú (Plano, Fondo, Guardar, Cargar, Borrar todo, SVG, Foto).");
        Poner(d, btnPaginaMedios, "MEDIOS (página)\nVideos, grabar tu proceso, imágenes de referencia y el estilo de la capa activa: líneas vivas, boceto, plano e imán.");
        Poner(d, btnPaginaBocas, "BOCAS (página)\nLipsync: guarda formas de boca (A, E, I, O, U, M...) y la app crea las claves según tu voz o un audio.");
        Poner(d, btnPaginaTitere, "TÍTERE (página)\nPersonajes que caminan, corren y saltan con tu mano. Crear, grabar, ciclos de caminado y armar tus propios personajes.");
        Poner(d, btnZoom, "ZOOM\nCuántos fotogramas caben en la barra de tiempo: Todo, 400, 100 o 25. Con menos fotogramas ves las claves (marcas naranjas) más separadas y es más fácil tocarlas.");
        Poner(d, btnSeguir, "FIJAR AQUÍ / SEGUIRME\nFijar aquí: el panel se queda en ese lugar y siempre visible. Seguirme: vuelve a aparecer solo cuando miras hacia arriba.\nTambién: pellizca el asa azul para moverlo; pellizcando además con la izquierda, separa las manos = más grande.");

        // Animar
        Poner(d, btnInicio, "INICIO\nVa al fotograma 1.");
        Poner(d, btnAnterior, "< (ANTERIOR)\nRetrocede un fotograma. También puedes tocar la barra de tiempo con el índice derecho.");
        Poner(d, btnSiguiente, "> (SIGUIENTE)\nAvanza un fotograma.");
        Poner(d, btnPlay, "PLAY / PAUSA\nReproduce la animación: las líneas se transforman entre las claves.\nPara animar: 1) ve a otro fotograma (toca la barra), 2) mueve los nodos (izquierda pulgar + medio), 3) se guarda una clave sola, 4) Play.");
        Poner(d, btnClave, "+ CLAVE\nGuarda la forma actual de todas las líneas en este fotograma (marca naranja). Normalmente no hace falta: al editar en un fotograma, la clave se guarda sola.");
        Poner(d, btnQuitarClave, "- CLAVE\nQuita la clave de este fotograma.\nPara MOVER una clave: pellízcala en la barra y arrástrala.");
        Poner(d, btnFps, "FPS\nVelocidad de la animación: 12, 24, 30 o 60 cuadros por segundo.");
        if (btnCapas != null)
            foreach (var b in btnCapas)
                Poner(d, b, "CAPA 1-4\nElige en qué capa dibujas. Cada capa tiene su estilo (temblor, hebras, boceto, plano, imán) en la página Medios.\nIdea: boceto en la capa 1 (Medios → Boceto: Gris) y tinta encima en la capa 2.");
        if (btnVer != null)
            foreach (var b in btnVer)
                Poner(d, b, "VER / OCULTA\nMuestra o esconde esa capa. Una capa oculta no se puede editar.");
        Poner(d, btnPlano, "LIBRE (3D) / PLANO (2D)\nPlano: dibujas sobre una hoja invisible frente a ti (si alejas el dedo más de ~2.5 cm, la línea se corta, como levantar el lápiz). Libre: dibujas en el aire, en 3D.\nCon una capa de Boceto en Plano, dibujas con lápiz sobre papel.");
        Poner(d, btnFondo, "FONDO\nCambia el fondo: blanco, cuadrícula o tu cuarto real (passthrough).");
        Poner(d, btnGuardar, "GUARDAR\nGuarda el dibujo con su propio nombre (Dibujo 1, Dibujo 2...). Si ya tiene nombre, lo actualiza.\nPara empezar uno NUEVO: Borrar todo y luego Guardar (recibe otro nombre).");
        Poner(d, btnCargar, "CARGAR\nMuestra abajo la lista de tus dibujos (el más reciente primero, con la fecha). Toca uno para abrirlo. Cargar otra vez = cerrar la lista.\nGuarda antes lo que tienes (Guardar). Si te arrepientes: deshacer.");
        Poner(d, btnBorrarTodo, "BORRAR TODO\nBorra todo el dibujo y empieza uno nuevo (al guardar recibe otro nombre). Se puede deshacer.");
        Poner(d, btnSvg, "SVG\nExporta las líneas como curvas vectoriales (para Illustrator, Inkscape...). En Plano se ve de frente al plano; en 3D, desde donde estás.");
        Poner(d, btnFoto, "FOTO\nGuarda una imagen PNG del dibujo desde donde estás. Los paneles, nodos, imágenes y capas de boceto no salen.");

        // Medios
        Poner(d, btnGrabar, "GRABAR (proceso)\nGraba cómo dibujas: tus manos y cómo aparecen las líneas. Toca otra vez (Detener) para parar.\nDespués: Video proceso lo convierte en MP4.");
        Poner(d, btnVideoProceso, "VIDEO PROCESO\nConvierte tu grabación (botón Grabar) en un video MP4: líneas + manos en gris. Elige antes la velocidad con Vel.");
        Poner(d, btnVelocidad, "VEL x1 / x2 / x4 / x8\nQué tan rápido se ve el video del proceso.");
        Poner(d, btnVideoAnim, "VIDEO ANIM\nExporta la animación como MP4 (1280x720), con el audio de las bocas si hay. Necesitas al menos 2 claves.");
        Poner(d, btnImagenMas, "IMAGEN +\nPone frente a ti la siguiente imagen de la carpeta Dibujos/Imagenes (cópialas con el cable o Meta Quest Developer Hub).\nPara calcar: en Plano, suelta la imagen cerca del plano y se pega detrás.");
        Poner(d, btnImagenMenos, "IMAGEN -\nQuita la imagen seleccionada (la que tiene tono azul). Pellizca una imagen para elegirla.");
        Poner(d, btnImagenesVer, "IMÁGENES: VER / OCULTAS\nEsconde o muestra todas las imágenes de referencia a la vez.");
        Poner(d, btnTemblor, "TEMBLOR (capa activa)\nLíneas vivas, como en la animación dibujada a mano: No, Suave, Medio, Fuerte. No cambia tus nodos, solo cómo se ven.");
        Poner(d, btnHebras, "HEBRAS (capa activa)\n1, 3 o 5 hebras finas por línea (en las puntas se juntan).\nGrosor de las hebras: gesto de grosor (izquierda pulgar + anular) + pellizco derecho en el aire, sube o baja.");
        Poner(d, btnGrosorVivo, "GROSOR VIVO (capa activa)\nEl grosor sube y baja a lo largo de la línea, como la presión de un pincel.");
        Poner(d, btnCicloTemblor, "CICLO DE 3 / LIBRE (capa activa)\nCiclo de 3: el temblor repite 3 dibujos (estilo clásico). Libre: siempre distinto.");
        Poner(d, btnSuavidad, "SUAVIDAD (capa activa)\nQué tan ondulado es el temblor: Suave, Normal o Nervioso.");
        Poner(d, btnVelocidadTemblor, "VELOCIDAD (capa activa)\nCuántas veces por segundo cambia el temblor: 4, 8, 12 o 24.");
        Poner(d, btnBoceto, "BOCETO (capa activa)\nLa capa se ve como lápiz gris o azul y NO sale en fotos ni videos.\nEn Plano (2D) dibujas con lápiz de verdad sobre una hoja: más cerca del plano = más oscuro. El puño izquierdo (de lado) es la goma.");
        Poner(d, btnUnirPlano, "PLANO: UNIDO / PROPIO (capa activa)\nUnido: la capa comparte la hoja con las otras, medio milímetro delante (como acetatos sobre papel). Propio: la capa tiene su propio plano.\nAl unir, sus líneas se pegan al plano.");
        Poner(d, btnIman, "IMÁN: SÍ / NO (capa activa)\nSí: las puntas de las líneas se pegan a otras y las figuras se cierran solas. No: las líneas quedan como las dibujas. En boceto se apaga solo.");

        // Bocas
        if (btnBocas != null)
            foreach (var b in btnBocas)
                Poner(d, b, "FORMA DE BOCA\nModo Guardar: 1) dibuja la boca en su propia capa y elígela, 2) mueve sus nodos para esta forma, 3) toca este botón: queda guardada (se ve oscuro).\nModo Probar: tocarlo pone esa boca en el fotograma actual.");
        Poner(d, btnModoBoca, "MODO: GUARDAR / PROBAR\nGuardar: tocar una forma la guarda. Probar: tocar una forma la pone en el fotograma actual (para corregir a mano).");
        Poner(d, btnVoz, "VOZ\nGraba tu voz con el micrófono de las gafas. Toca otra vez para parar. Después toca Lipsync.");
        Poner(d, btnAudio, "AUDIO\nElige un audio de la carpeta Dibujos/Audio (wav o mp3) en vez de grabar tu voz.");
        Poner(d, btnLipsync, "LIPSYNC\nCrea las claves de la boca según el audio. Antes: guarda las formas (Reposo, A, E, I, O, U, M) y graba Voz o elige Audio. Luego Play.");
        Poner(d, btnQuitarAudio, "QUITAR AUDIO\nQuita el audio de la animación.");

        // Títere
        Poner(d, btnTitere, "TIPO\nElige qué personaje crea el botón Crear: Palito, Musculoso, Gordito, Flaco o Niño.");
        Poner(d, btnMuneco, "CREAR / PARAR\nCrear: aparece el personaje (en la Capa 4) y se enciende con tu mano DERECHA (ponla frente a ti un segundo). Lados = caminar/correr · mano abajo = agacharse · golpe arriba = saltar.\nApagar: Parar, palma arriba o choca los cinco con la otra mano. Esconderlos: Ver/Oculta de la Capa 4. Borrar uno: acerca la mano y toca la X de su marco.");
        Poner(d, btnGrabarTitere, "GRABAR (títere)\nCuenta 3 segundos y guarda una clave por fotograma de los personajes encendidos. Parar = terminar. Después: Play, corrige claves y exporta con Video anim (Medios).");
        Poner(d, btnPosar, "POSAR DEDOS\nEl índice y el medio derechos acomodan las piernas del personaje; pellizco IZQUIERDO = guardar una clave. Toca otra vez para terminar.");
        Poner(d, btnCiclo, "CICLO\nEl caminado: Normal, Con estilo (Richard Williams: paso alto, brazos grandes), Sigiloso (Ken Harris: agachado, el pie pasa rápido por el medio y se apoya con cuidado) o tus ciclos guardados.");
        Poner(d, btnGuardarCiclo, "GUARDAR CICLO\nGuarda tu propio caminado: 1) anima al menos 3 claves de un paso con las piernas del personaje (la última igual a la primera), 2) toca Guardar ciclo, 3) elígelo con Ciclo.");
        Poner(d, btnVoltear, "VOLTEAR\nEl personaje elegido mira hacia el otro lado.");
        Poner(d, btnPiso, "PISO +/-\nPellizca una línea (queda azul) y toca Piso: esa línea será piso o plataforma (sube rampas, cae si se acaba). Otra vez = deja de ser piso.\nSolo pisa los pisos que están a su misma profundidad.");
        Poner(d, btnPierna1, "PIERNA 1 / 2\nPara armar tu propio personaje: 1) dibuja una pierna (de la cadera al pie), 2) pellízcala, 3) toca Pierna 1. Haz lo mismo con la otra (Pierna 2). Con las dos piernas ya camina.");
        Poner(d, btnPierna2, "PIERNA 2\nLa segunda pierna de tu personaje: pellízcala y toca este botón. (Mira también Pierna 1.)");
        Poner(d, btnBrazo1, "BRAZO 1 / 2\nPellizca una línea de brazo (del hombro a la mano) y toca Brazo 1 o Brazo 2. Los brazos se balancean al caminar.");
        Poner(d, btnBrazo2, "BRAZO 2\nEl segundo brazo: pellízcalo y toca este botón.");
        Poner(d, btnCuerpo, "CUERPO +/-\nPellizca una línea (tronco, cabeza, sombrero...) y toca Cuerpo: se mueve junto con la cadera. Otra vez = la quita del cuerpo.");
        return d;
    }
}

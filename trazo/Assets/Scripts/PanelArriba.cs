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
//    y Bocas (lipsync).
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
    public GameObject contenido;
    [Tooltip("Barra de la línea de tiempo (un cubo hijo de Contenido)")]
    public Transform barra;
    public Transform cabezal;
    [Tooltip("Barra para pellizcar y mover el panel")]
    public Transform asa;
    public Material materialClave;
    public TMP_Text textoFotograma;

    [Header("Pestañas")]
    public GameObject paginaAnimar, paginaMedios, paginaBocas;
    public BotonTocable btnPaginaAnimar, btnPaginaMedios, btnPaginaBocas, btnZoom, btnSeguir;

    [Header("Animar")]
    public BotonTocable btnInicio, btnAnterior, btnPlay, btnSiguiente, btnClave, btnQuitarClave, btnFps;
    public BotonTocable btnPlano, btnFondo, btnGuardar, btnCargar, btnBorrarTodo, btnSvg, btnFoto;
    public BotonTocable[] btnCapas = new BotonTocable[0];
    public BotonTocable[] btnVer = new BotonTocable[0];

    [Header("Medios")]
    public BotonTocable btnGrabar, btnVideoProceso, btnVelocidad, btnVideoAnim, btnImagenMas, btnImagenMenos, btnTemblor;
    public TMP_Text textoMedios;

    [Header("Bocas")]
    public BotonTocable[] btnBocas = new BotonTocable[0];
    public BotonTocable btnModoBoca, btnVoz, btnAudio, btnLipsync, btnQuitarAudio;
    public TMP_Text textoBocas;

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

    void Start()
    {
        if (control == null) control = FindFirstObjectByType<ControlManos>();
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (animacion == null) animacion = FindFirstObjectByType<Animacion>();
        if (escenario == null) escenario = FindFirstObjectByType<Escenario>();
        if (lipsync == null) lipsync = FindFirstObjectByType<Lipsync>();
        if (referencias == null) referencias = FindFirstObjectByType<Referencias>();
        if (grabador == null) grabador = FindFirstObjectByType<GrabadorProceso>();
        if (exportador == null) exportador = FindFirstObjectByType<ExportadorVideo>();

        Conectar(btnPaginaAnimar, () => PonerPagina(0));
        Conectar(btnPaginaMedios, () => PonerPagina(1));
        Conectar(btnPaginaBocas, () => PonerPagina(2));
        Conectar(btnZoom, CambiarZoom);
        Conectar(btnSeguir, AlternarFijo);

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
            Conectar(btnCargar, dibujo.Cargar);
            Conectar(btnBorrarTodo, dibujo.BorrarTodo);
            Conectar(btnSvg, dibujo.ExportarSVG);
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
            Conectar(btnTemblor, dibujo.temblor.Siguiente);
        if (referencias != null)
        {
            Conectar(btnImagenMas, () => referencias.AgregarSiguiente(control != null ? control.Cabeza : null));
            Conectar(btnImagenMenos, referencias.QuitarSeleccionada);
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
        if (btnPaginaAnimar != null) btnPaginaAnimar.Marcar(p == 0);
        if (btnPaginaMedios != null) btnPaginaMedios.Marcar(p == 1);
        if (btnPaginaBocas != null) btnPaginaBocas.Marcar(p == 2);
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
        if (contenido == null || !contenido.activeInHierarchy)
            return false;
        Vector3 l = transform.InverseTransformPoint(mundo);
        return Mathf.Abs(l.x) < 0.27f && l.y > -0.15f && l.y < 0.225f && Mathf.Abs(l.z) < 0.06f;
    }

    void Update()
    {
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
        Vector3 a = asa.localPosition;
        bool enAsa = Mathf.Abs(l.x - a.x) < asa.localScale.x * 0.5f + 0.02f
                     && Mathf.Abs(l.y - a.y) < 0.025f && Mathf.Abs(l.z) < 0.05f;
        if (!enAsa)
            return;
        arrastrando = true;
        escalando = false;
        anclado = true;
        desfaseAsa = transform.position - der.PuntoPellizco;
        control.Ocupado = true;
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
        if (btnSeguir != null)
        {
            btnSeguir.PonerTexto(anclado ? "Seguirme" : "Fijar aquí");
            btnSeguir.Marcar(anclado);
        }

        if (pagina == 0)
            RefrescarAnimar();
        else if (pagina == 1)
            RefrescarMedios();
        else
            RefrescarBocas();
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
        if (btnTemblor != null && dibujo != null && dibujo.temblor != null)
        {
            btnTemblor.PonerTexto("Temblor: " + dibujo.temblor.NombreNivel);
            btnTemblor.Marcar(dibujo.temblor.nivel > 0);
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
            texto = "Grabar = guardar tu proceso con las manos · Imagen + = referencias de Dibujos/Imagenes";
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
}

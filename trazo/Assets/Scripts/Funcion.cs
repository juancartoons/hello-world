using UnityEngine;

// LA FUNCIÓN (v56): un reproductor INMERSIVO, como en Quill. Tocas "Función" (reloj) o dices "función" y:
//  - desaparece todo lo de editar (vista limpia, mesa, capas, paleta) y apareces DONDE ESTÁ LA CÁMARA, dentro de tu
//    escena, mirando hacia donde ella mira;
//  - la animación corre desde el principio, fluida (el morph calcula los pasos intermedios) y se repite;
//  - te puedes mover y mirar libremente: la cámara solo dice dónde estás parado. Si la cámara viaja, tú viajas con
//    ella (suave); si salta a otro lugar (un corte), apareces allá de una vez, como un corte de cine.
//  - Para salir: el botón "Salir" junto a tu mano izquierda, o di "función" otra vez. Vuelves a donde estabas.
public class Funcion : MonoBehaviour
{
    static Funcion instancia;
    public static bool Activa => instancia != null && instancia.activa;

    bool activa, vistaLimpiaAntes;
    ControlManos control;
    Dibujo dibujo;
    Animacion anim;
    Transform rig;
    Vector3 posAntes;
    Quaternion rotAntes;
    Vector3 camaraAntes;
    bool hayCamaraAntes;
    int tramoAntes = -1;
    BotonTocable btnSalir;

    public static void Alternar()
    {
        if (instancia == null)
            instancia = new GameObject("Funcion").AddComponent<Funcion>();
        if (instancia.activa)
            instancia.Salir();
        else
            instancia.Entrar();
    }

    void Entrar()
    {
        control = ControlManos.Instancia;
        dibujo = control != null ? control.dibujo : FindFirstObjectByType<Dibujo>();
        anim = dibujo != null ? dibujo.animacion : null;
        var r = FindFirstObjectByType<OVRCameraRig>();
        if (control == null || anim == null || r == null || control.Cabeza == null)
            return;
        if (!anim.HayAnimacion)
        {
            dibujo.Mensaje("Para la función necesitas una animación (al menos 2 claves, del dibujo o de la cámara)");
            return;
        }
        rig = r.transform;
        posAntes = rig.position;
        rotAntes = rig.rotation;
        // Nada de editar a la vista.
        vistaLimpiaAntes = VistaLimpia.Activa;
        if (!vistaLimpiaAntes)
            VistaLimpia.Alternar();
        if (control.panelArriba != null)
            control.panelArriba.GuardarMesa();
        CapasProfundidad.CerrarSiAbierta();
        control.CerrarMenu();
        control.Ocupado = true;
        activa = true;
        hayCamaraAntes = false;
        tramoAntes = -1;
        anim.IrA(0);
        if (!anim.Reproduciendo)
            anim.AlternarReproducir();
        Seguir(true);
        dibujo.Mensaje("Función: mira y muévete libremente. Para salir: Salir (junto a tu mano izquierda) o di \"función\"");
    }

    void Salir()
    {
        activa = false;
        if (anim != null && anim.Reproduciendo)
            anim.Pausar();
        if (rig != null)
            rig.SetPositionAndRotation(posAntes, rotAntes);
        if (!vistaLimpiaAntes && VistaLimpia.Activa)
            VistaLimpia.Alternar();
        if (control != null)
            control.Ocupado = false;
        if (btnSalir != null)
        {
            btnSalir.gameObject.SetActive(false);
            btnSalir.etiqueta.gameObject.SetActive(false);
        }
        if (dibujo != null)
            dibujo.Mensaje("Fin de la función");
    }

    void LateUpdate()
    {
        if (!activa)
            return;
        if (control == null || anim == null || rig == null)
        {
            activa = false;
            return;
        }
        control.Ocupado = true;
        // Se repite: al terminar (o si se pausó), vuelve a empezar.
        if (!anim.Reproduciendo)
        {
            anim.IrA(0);
            anim.AlternarReproducir();
            hayCamaraAntes = false;
        }
        Seguir(false);
        BotonSalir();
    }

    // El tramo de la cámara (entre qué claves va): al cambiar a uno que empieza lejos, es un CORTE.
    static int Tramo(int f)
    {
        int t = -1, mejor = int.MinValue;
        var claves = CamaraEscena.Claves;
        for (int i = 0; i < claves.Count; i++)
            if (claves[i] != null && claves[i].fotograma <= f && claves[i].fotograma > mejor)
            {
                mejor = claves[i].fotograma;
                t = i;
            }
        return t;
    }

    void Seguir(bool primeraVez)
    {
        Vector3 p;
        Quaternion r;
        float campo;
        var cab = control.Cabeza;
        if (!CamaraEscena.PoseMundo(anim.Fotograma, out p, out r, out campo))
        {
            // Sin cámara: te quedas donde estás (solo corre la animación).
            return;
        }
        int tramo = Tramo(anim.Fotograma);
        bool corte = primeraVez || !hayCamaraAntes || (tramo != tramoAntes && Vector3.Distance(p, camaraAntes) > 0.3f);
        tramoAntes = tramo;
        if (corte)
        {
            // Apareces en la cámara, mirando hacia donde ella mira (solo de lado: el piso sigue siendo el piso).
            Vector3 adelante = Vector3.ProjectOnPlane(r * Vector3.forward, Vector3.up);
            Vector3 miAdelante = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
            if (adelante.sqrMagnitude > 1e-4f && miAdelante.sqrMagnitude > 1e-4f)
                rig.RotateAround(cab.position, Vector3.up, Vector3.SignedAngle(miAdelante, adelante, Vector3.up));
            rig.position += p - cab.position;
        }
        else
        {
            // La cámara viaja: tú con ella (tus propios movimientos se respetan).
            rig.position += p - camaraAntes;
        }
        camaraAntes = p;
        hayCamaraAntes = true;
    }

    // "Salir": un botoncito junto a tu mano izquierda (solo si la miras de cerca no estorba).
    void BotonSalir()
    {
        var izq = control.Izq;
        if (btnSalir == null)
        {
            var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
            if (nav == null)
                return;
            btnSalir = PanelTutoriales.Boton(transform, "", Vector3.zero, new Vector2(0.05f, 0.022f), nav.materialBoton, nav.materialBotonMarcado);
            Idioma.Poner(btnSalir.etiqueta, "Salir");
            btnSalir.alTocar.AddListener(Salir);
        }
        bool ver = izq.valida;
        if (btnSalir.gameObject.activeSelf != ver)
        {
            btnSalir.gameObject.SetActive(ver);
            btnSalir.etiqueta.gameObject.SetActive(ver);
        }
        if (!ver)
            return;
        Vector3 pos = (izq.pulgar + izq.indice) * 0.5f + Vector3.up * 0.06f;
        Vector3 mirar = pos - control.Cabeza.position;
        Quaternion rot = mirar.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(mirar, Vector3.up) : Quaternion.identity;
        btnSalir.transform.SetPositionAndRotation(pos, rot);
        btnSalir.etiqueta.transform.SetPositionAndRotation(pos + rot * new Vector3(0f, 0f, -0.0046f), rot);
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}

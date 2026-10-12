using TMPro;
using UnityEngine;

// GIRAR EL FONDO 360 (parte de ControlManos). v40: FLECHAS A LOS LADOS + DESLIZAR EL ÍNDICE.
//  - Con un fondo 360 puesto, si giras la cabeza bastante hacia un lado (unos 45° de hacia donde venías mirando),
//    aparece una flechita en la orilla de ese lado, al alcance del dedo.
//  - Tócala con el índice derecho y se prende el MODO GIRAR (la flecha se vuelve "<  >" y se pone azul). Desde ahí
//    desliza el índice hacia cualquier lado (no importa qué flecha tocaste): el fondo sigue a tu dedo, suave y
//    continuo (unos 10 cm = 30°).
//  - Se apaga solo si dejas el dedo quieto 1.5 segundos, o si vuelves a tocar la flecha.
//  - Sin flecha no gira nada: así no hay accidentes. También están los botones "← Girar" / "Girar →" del menú del
//    reloj y la voz ("gira derecha", "gira izquierda", "gira atrás").
// (Antes: v38 el antebrazo, v39 la mano plana de lado; se confundían con otros gestos o el visor no las veía bien.)
// Tus dibujos se quedan contigo, delante de ti: lo que gira es el fondo (ver Escenario.Girar).
public partial class ControlManos
{
    const float GiroPaso = 30f;               // los botones del reloj y la voz
    const float GradosPorMetro = 300f;        // deslizando: 10 cm = 30°
    const float AnguloFlecha = 45f;           // cuánto girar la cabeza para que salga la flecha
    const float AnguloQuitaFlecha = 25f;      // y cuándo se va (al volver a mirar al frente)
    const float FrenteSigue = 0.12f;          // qué tan rápido "tu frente" alcanza a tu cabeza (por segundo)
    const float FlechaDura = 6f;              // si no la tocas, se va sola
    const float QuietoApaga = 1.5f;           // el modo girar se apaga con el dedo quieto este tiempo

    Escenario escenarioGiro;
    float avisoGiro = -10f;
    float frenteYaw;
    bool frenteListo;
    int ladoFlecha;                            // -1 a la izquierda, 1 a la derecha, 0 = no hay flecha
    float flechaDesde;
    bool modoGirar, dedoEnFlecha, salioDeFlecha;
    float quietoDesde;
    Vector3 dedoAntes;
    Transform flechaGiro;
    TMP_Text textoFlecha;
    Material materialFlecha;

    // Cada cuadro.
    void RevisarGiros()
    {
        if (escenarioGiro == null)
            escenarioGiro = FindFirstObjectByType<Escenario>();
        bool hay360 = escenarioGiro != null && escenarioGiro.modo >= 3;
        bool libre = hay360 && Cabeza != null && GestoIzq == Gesto.Ninguno && !paletaAbierta && !menuAbierto && !Ocupado
                     && !flechaIzq.activa && !flechaDer.activa && !Titere.Activo && !Tutorial.EnCurso;
        if (!libre)
        {
            ApagarModoGirar(false);
            QuitarFlecha();
            frenteListo = false;
            return;
        }

        // "Tu frente": sigue a tu cabeza despacio. Mientras hay flecha, se queda quieto (para que no se escape).
        float yaw = Cabeza.eulerAngles.y;
        if (!frenteListo)
        {
            frenteYaw = yaw;
            frenteListo = true;
        }
        if (ladoFlecha == 0)
            frenteYaw = Mathf.LerpAngle(frenteYaw, yaw, 1f - Mathf.Exp(-FrenteSigue * Time.deltaTime));
        float diferencia = Mathf.DeltaAngle(frenteYaw, yaw);

        if (ladoFlecha == 0)
        {
            if (Mathf.Abs(diferencia) > AnguloFlecha)
                PonerFlecha(diferencia > 0f ? 1 : -1);
        }
        else if (!modoGirar && (Mathf.Abs(diferencia) < AnguloQuitaFlecha || Time.time - flechaDesde > FlechaDura))
        {
            QuitarFlecha();
            frenteYaw = yaw; // ya te diste la vuelta: este es tu nuevo frente
            return;
        }
        if (ladoFlecha == 0 || flechaGiro == null)
            return;

        // Tocar la flecha: prende (o apaga) el modo girar.
        bool toca = Der.valida && Vector3.Distance(Der.indice, flechaGiro.position) < 0.03f;
        if (toca && !dedoEnFlecha)
        {
            if (!modoGirar)
                PrenderModoGirar();
            else if (salioDeFlecha)
                ApagarModoGirar(true);
        }
        dedoEnFlecha = toca;
        if (!modoGirar)
            return;
        if (!Der.valida)
        {
            ApagarModoGirar(true);
            return;
        }

        // El fondo sigue al dedo, de lado.
        Vector3 mov = Der.indice - dedoAntes;
        dedoAntes = Der.indice;
        if (Vector3.Distance(Der.indice, flechaGiro.position) > 0.05f)
            salioDeFlecha = true;
        float dx = Vector3.Dot(mov, LadoHorizontal(1));
        if (Mathf.Abs(dx) > 0.0004f)
            escenarioGiro.Girar(-dx * GradosPorMetro); // el dedo a la derecha = el fondo va a la derecha
        if (mov.magnitude > 0.0025f)
            quietoDesde = Time.time;
        else if (Time.time - quietoDesde > QuietoApaga)
            ApagarModoGirar(true);
        flechaDesde = Time.time; // mientras giras, la flecha no se va
    }

    void PonerFlecha(int lado)
    {
        ladoFlecha = lado;
        flechaDesde = Time.time;
        if (flechaGiro == null)
            ArmarFlecha();
        // En la orilla de ese lado de tu vista, un poco abajo, al alcance del dedo. Se queda fija ahí.
        Vector3 adelante = Cabeza.forward;
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        Vector3 dir = Quaternion.AngleAxis(lado * 25f, Vector3.up) * adelante.normalized;
        dir = (dir - Vector3.up * 0.12f).normalized;
        Vector3 pos = Cabeza.position + dir * 0.42f;
        flechaGiro.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - Cabeza.position, Vector3.up));
        flechaGiro.gameObject.SetActive(true);
        PintarFlecha();
    }

    void QuitarFlecha()
    {
        ladoFlecha = 0;
        dedoEnFlecha = false;
        if (flechaGiro != null && flechaGiro.gameObject.activeSelf)
            flechaGiro.gameObject.SetActive(false);
    }

    void PrenderModoGirar()
    {
        modoGirar = true;
        salioDeFlecha = false;
        dedoAntes = Der.indice;
        quietoDesde = Time.time;
        Burbuja(flechaGiro.position, 1.15f);
        dibujo.Mensaje("Modo girar: desliza el índice a un lado");
        PintarFlecha();
    }

    void ApagarModoGirar(bool avisar)
    {
        if (!modoGirar)
            return;
        modoGirar = false;
        if (avisar && flechaGiro != null)
            Burbuja(flechaGiro.position, 0.95f);
        PintarFlecha();
    }

    void ArmarFlecha()
    {
        flechaGiro = new GameObject("FlechaGirar").transform;
        flechaGiro.SetParent(transform, false);
        var fondo = new GameObject("Fondo");
        fondo.transform.SetParent(flechaGiro, false);
        fondo.AddComponent<MeshFilter>().sharedMesh = Persiana.CrearPastilla(0.05f, 0.034f);
        var r = fondo.AddComponent<MeshRenderer>();
        if (materialVidrio != null)
        {
            materialFlecha = new Material(materialVidrio);
            if (materialFlecha.HasProperty("_Esmerilado"))
                materialFlecha.SetFloat("_Esmerilado", 0.75f);
            if (materialFlecha.HasProperty("_Opacidad"))
                materialFlecha.SetFloat("_Opacidad", 0.9f);
        }
        else
        {
            materialFlecha = ColorMaterial(new Color(0.96f, 0.96f, 0.97f));
        }
        r.sharedMaterial = materialFlecha;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(flechaGiro, false);
        go.transform.localPosition = new Vector3(0f, 0.001f, -0.001f);
        var t = go.AddComponent<TextMeshPro>();
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.01f;
        t.fontSizeMax = 0.3f;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.rectTransform.sizeDelta = new Vector2(0.044f, 0.028f);
        textoFlecha = t;
        flechaGiro.gameObject.SetActive(false);
    }

    void PintarFlecha()
    {
        if (textoFlecha == null)
            return;
        textoFlecha.text = modoGirar ? "<  >" : ladoFlecha < 0 ? "<" : ">";
        textoFlecha.color = modoGirar ? new Color(0.15f, 0.45f, 0.9f) : new Color(0.12f, 0.12f, 0.16f);
    }

    // Los botones del menú del reloj (lado = 1: a la derecha; -1: a la izquierda) y la voz.
    public void GirarFondoBoton(int lado)
    {
        Vector3 donde = Cabeza != null ? Cabeza.position + Cabeza.forward * 0.4f : transform.position;
        GirarFondo(lado, donde);
    }

    // lado = 1: giras a la derecha; -1: a la izquierda.
    void GirarFondo(int lado, Vector3 donde)
    {
        if (escenarioGiro == null)
            escenarioGiro = FindFirstObjectByType<Escenario>();
        if (escenarioGiro != null && escenarioGiro.Girar(lado * GiroPaso))
        {
            Burbuja(donde, lado > 0 ? 1.15f : 0.95f);
            MostrarEtiqueta(lado > 0 ? "Girar →" : "← Girar");
        }
        else if (Time.time - avisoGiro > 3f)
        {
            avisoGiro = Time.time;
            dibujo.Mensaje("Girar es para los fondos 360");
        }
    }
}

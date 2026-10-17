using System.Collections.Generic;
using TMPro;
using UnityEngine;

// LAS FLECHAS DE GIRAR (v42): dos flechitas FIJAS y chiquitas en la fila de la pastillita de abajo: la de la
// izquierda en la columna del micrófono y la de la derecha en la del parlante. Solo se ven con un fondo 360.
//  - TOCA una y retira el dedo: el fondo gira de una vez 30° hacia ese lado.
//  - TOCA y ARRASTRA de lado: el fondo sigue a tu dedo mientras lo mueves (10 cm = 30°). Se suelta al dejar el dedo
//    quieto un momento o al alejarlo.
//  - En la vista limpia no se ven (como las pastillas, el parlante y el micrófono).
// (Antes, v40-v41: una flecha salía al girar la cabeza bastante hacia un lado; costaba que apareciera.)
public class FlechasGiro : MonoBehaviour
{
    const float GradosPorMetro = 300f;     // arrastrando: 10 cm = 30°
    const float Ancho = 0.034f, Alto = 0.024f;
    const float Arrastre = 0.012f;         // metros de lado para que un toque se vuelva arrastre
    const float QuietoSuelta = 1.2f;       // arrastrando: el dedo quieto este tiempo = se suelta
    const float LejosSuelta = 0.2f;        // o el dedo así de lejos de la flecha
    static readonly Color Normal = new Color(0.12f, 0.12f, 0.16f);
    static readonly Color Activa = new Color(0.15f, 0.45f, 0.9f);

    public static FlechasGiro Instancia { get; private set; }
    // El índice derecho está cerca de una flecha (no se dibuja ni se borra).
    public static bool DedoCerca { get; private set; }

    class Flecha
    {
        public Transform t;
        public TMP_Text texto;
        public int lado;          // -1 izquierda, 1 derecha
    }

    readonly Flecha[] flechas = new Flecha[2];
    readonly List<Material> materiales = new List<Material>();
    readonly List<Mesh> mallas = new List<Mesh>();
    ControlManos control;
    Escenario escenario;
    int activa = -1;              // la flecha que tocas (0 izquierda, 1 derecha)
    bool arrastrando, esperarSalir, armada;
    Vector3 inicio, dedoAntes;
    float quietoDesde, ver;
    ManoSeguida mano;          // v43: la mano que está tocando (derecha o izquierda)

    public static void Crear(ControlManos control)
    {
        if (Instancia != null)
            return;
        var go = new GameObject("FlechasGiro");
        Instancia = go.AddComponent<FlechasGiro>();
        Instancia.control = control;
    }

    void Armar()
    {
        armada = true;
        var malla = Persiana.CrearPastilla(Ancho, Alto);
        mallas.Add(malla);
        Material vidrio;
        if (control.materialVidrio != null)
        {
            vidrio = new Material(control.materialVidrio);
            if (vidrio.HasProperty("_Esmerilado"))
                vidrio.SetFloat("_Esmerilado", 0.75f);
            if (vidrio.HasProperty("_Opacidad"))
                vidrio.SetFloat("_Opacidad", 0.88f);
            materiales.Add(vidrio);
        }
        else
        {
            vidrio = control.materialNodo != null ? new Material(control.materialNodo) : null;
            if (vidrio != null)
            {
                if (vidrio.HasProperty("_BaseColor"))
                    vidrio.SetColor("_BaseColor", new Color(0.96f, 0.96f, 0.97f));
                materiales.Add(vidrio);
            }
        }
        for (int i = 0; i < 2; i++)
        {
            var f = new Flecha { lado = i == 0 ? -1 : 1 };
            f.t = new GameObject(i == 0 ? "GiraIzquierda" : "GiraDerecha").transform;
            f.t.SetParent(transform, false);
            var fondo = new GameObject("Fondo");
            fondo.transform.SetParent(f.t, false);
            fondo.AddComponent<MeshFilter>().sharedMesh = malla;
            var r = fondo.AddComponent<MeshRenderer>();
            if (vidrio != null)
                r.sharedMaterial = vidrio;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(f.t, false);
            go.transform.localPosition = new Vector3(0f, 0.001f, -0.001f);
            var t = go.AddComponent<TextMeshPro>();
            t.enableAutoSizing = true;
            t.fontSizeMin = 0.01f;
            t.fontSizeMax = 0.3f;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            t.color = Normal;
            t.text = f.lado < 0 ? "<" : ">";
            t.rectTransform.sizeDelta = new Vector2(Ancho * 0.8f, Alto * 0.85f);
            f.texto = t;
            flechas[i] = f;
        }
    }

    void LateUpdate()
    {
        DedoCerca = false;
        if (control == null)
            control = ControlManos.Instancia;
        var cab = control != null ? control.Cabeza : null;
        if (escenario == null)
            escenario = FindFirstObjectByType<Escenario>();
        bool quiere = cab != null && escenario != null && escenario.modo >= 3 && !VistaLimpia.Activa
                      && !Tutorial.EnCurso && !Titere.Activo && !ExportadorVideo.Exportando;
        ver = Mathf.MoveTowards(ver, quiere ? 1f : 0f, Time.deltaTime * 5f);
        if (!armada)
        {
            if (!quiere || control == null)
                return;
            Armar();
        }
        bool mostrar = ver > 0.01f;
        if (transform.childCount > 0)
            for (int i = 0; i < 2; i++)
                if (flechas[i].t.gameObject.activeSelf != mostrar)
                    flechas[i].t.gameObject.SetActive(mostrar);
        if (!mostrar)
        {
            activa = -1;
            arrastrando = false;
            return;
        }

        // En la fila de la pastillita de abajo, en la columna del micrófono (izquierda) y del parlante (derecha).
        for (int i = 0; i < 2; i++)
        {
            Vector3 p;
            Quaternion q;
            // v45: más al centro (antes, en las columnas del micrófono y del parlante, quedaban muy al borde).
            // v49: aún más al centro y un poquito más abajo.
            PanelesFlexibles.PoseOrilla(cab, false, (i == 0 ? BotonVoz.Lugar.x : BotonSonido.Lugar.x) * 0.4f, out p, out q);
            p -= q * Vector3.up * 0.018f;
            flechas[i].t.SetPositionAndRotation(p, q);
            flechas[i].t.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, ver) * (activa == i ? 1.1f : 1f);
            Color c = activa == i ? Activa : Normal;
            if (flechas[i].texto.color != c)
                flechas[i].texto.color = c;
        }

        var der = control.Der;
        bool dedoOk = der.valida && quiere && ver > 0.9f && !control.Ocupado;
        if (der.valida)
            for (int i = 0; i < 2; i++)
                if (Cerca(i, der.indice, 0.03f))
                {
                    DedoCerca = true;
                    // v42: su etiqueta, encima: "Gira izquierda" / "Gira derecha" (la palabra que se dice).
                    if (activa < 0 && ver > 0.9f)
                        EtiquetasBotones.ProponerComando(flechas[i].t, Vector3.Distance(der.indice, flechas[i].t.position),
                            flechas[i].lado < 0 ? ControlManos.ComandoVoz.GiraIzquierda : ControlManos.ComandoVoz.GiraDerecha,
                            flechas[i].t.position + flechas[i].t.up * (Alto * 0.5f + 0.013f) - flechas[i].t.forward * 0.004f,
                            flechas[i].t.rotation);
                }

        // v43: también con el índice IZQUIERDO: la mano que toca primero es la que manda hasta soltar.
        var izq = control.Izq;
        bool izqOk = izq.valida && quiere && ver > 0.9f && !control.Ocupado;
        if (activa < 0)
        {
            if (esperarSalir)
            {
                bool derLejos = !dedoOk || (!Cerca(0, der.indice, 0.02f) && !Cerca(1, der.indice, 0.02f));
                bool izqLejos = !izqOk || (!Cerca(0, izq.indice, 0.02f) && !Cerca(1, izq.indice, 0.02f));
                if (derLejos && izqLejos)
                    esperarSalir = false;
                return;
            }
            for (int h = 0; h < 2; h++)
            {
                var m = h == 0 ? der : izq;
                if (!(h == 0 ? dedoOk : izqOk))
                    continue;
                for (int i = 0; i < 2; i++)
                {
                    if (!Cerca(i, m.indice, 0.006f))
                        continue;
                    activa = i;
                    mano = m;
                    arrastrando = false;
                    inicio = dedoAntes = m.indice;
                    quietoDesde = Time.time;
                    return;
                }
            }
            return;
        }
        der = mano ?? der;
        dedoOk = der.valida && quiere && ver > 0.9f && !control.Ocupado;

        if (!arrastrando)
        {
            if (!dedoOk)
            {
                activa = -1;
                return;
            }
            float dx = Vector3.Dot(der.indice - inicio, LadoHorizontal(cab));
            if (Mathf.Abs(dx) > Arrastre)
            {
                // Se volvió arrastre: el fondo sigue al dedo (también lo que ya movió).
                arrastrando = true;
                escenario.Girar(-dx * GradosPorMetro);
                dedoAntes = der.indice;
                quietoDesde = Time.time;
            }
            else if (!Cerca(activa, der.indice, 0.02f))
            {
                // Retiraste el dedo sin arrastrar: un toque = 30° de una vez.
                control.GirarFondo(flechas[activa].lado, flechas[activa].t.position);
                activa = -1;
                esperarSalir = true;
            }
            return;
        }

        // Arrastrando: el dedo a la derecha = el fondo va a la derecha.
        if (!der.valida || control.Ocupado)
        {
            Terminar();
            return;
        }
        Vector3 mov = der.indice - dedoAntes;
        dedoAntes = der.indice;
        float lado = Vector3.Dot(mov, LadoHorizontal(cab));
        if (Mathf.Abs(lado) > 0.0004f)
            escenario.Girar(-lado * GradosPorMetro);
        if (mov.magnitude > 0.0025f)
            quietoDesde = Time.time;
        else if (Time.time - quietoDesde > QuietoSuelta)
            Terminar();
        if (activa >= 0 && Vector3.Distance(der.indice, flechas[activa].t.position) > LejosSuelta)
            Terminar();
    }

    void Terminar()
    {
        if (activa >= 0 && control != null)
            control.SonidoBurbuja(flechas[activa].t.position);
        activa = -1;
        arrastrando = false;
        esperarSalir = true;
    }

    // ¿El dedo está sobre la flecha i? (en sus coordenadas, con este margen)
    bool Cerca(int i, Vector3 dedo, float margen)
    {
        var f = flechas[i];
        if (f == null || f.t == null)
            return false;
        Vector3 l = f.t.InverseTransformPoint(dedo);
        return Mathf.Abs(l.x) < Ancho * 0.5f + margen && Mathf.Abs(l.y) < Alto * 0.5f + margen && Mathf.Abs(l.z) < margen + 0.015f;
    }

    // Tu derecha, de lado (sin subir ni bajar).
    static Vector3 LadoHorizontal(Transform cabeza)
    {
        Vector3 derecha = cabeza.right;
        derecha.y = 0f;
        if (derecha.sqrMagnitude < 1e-4f)
            derecha = Vector3.right;
        return derecha.normalized;
    }

    // Para las etiquetas (tooltips): la flecha más cerca del dedo, o -1.
    public Transform FlechaCerca(Vector3 dedo, out int lado)
    {
        lado = 0;
        if (!armada || ver < 0.9f)
            return null;
        for (int i = 0; i < 2; i++)
            if (Cerca(i, dedo, 0.02f))
            {
                lado = flechas[i].lado;
                return flechas[i].t;
            }
        return null;
    }

    void OnDestroy()
    {
        foreach (var m in materiales)
            if (m != null)
                Destroy(m);
        foreach (var m in mallas)
            if (m != null)
                Destroy(m);
        if (Instancia == this)
            Instancia = null;
        DedoCerca = false;
    }
}

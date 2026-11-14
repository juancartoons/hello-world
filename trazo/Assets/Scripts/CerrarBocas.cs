using TMPro;
using UnityEngine;

// v67: la CARA DE MUESTRA DE LAS BOCAS (capa "Bocas") tiene una X arriba a la derecha (como las figuras 3D). Tocarla
// pregunta "¿Seguro que quieres borrar estas bocas de la escena?"; "Sí, borrar" quita la capa con todo (sus bocas y
// claves). Deshacer la devuelve.
public class CerrarBocas : MonoBehaviour
{
    Transform equis, pregunta;
    BotonTocable btnEquis;
    ControlManos control;
    float proximo;
    int capaBocas = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        new GameObject("CerrarBocas").AddComponent<CerrarBocas>();
    }

    void Update()
    {
        if (control == null)
            control = ControlManos.Instancia;
        if (control == null || control.dibujo == null || control.Cabeza == null)
            return;
        if (Time.time < proximo)
            return;
        proximo = Time.time + 0.1f;
        var dibujo = control.dibujo;
        Bounds caja;
        capaBocas = BuscarCapa(dibujo, out caja);
        bool ver = capaBocas >= 0 && !MesaEntrada.Activa && !Tutorial.EnCurso && !ExportadorVideo.Exportando && !Funcion.Activa;
        if (!ver)
        {
            if (equis != null && equis.gameObject.activeSelf)
                equis.gameObject.SetActive(false);
            if (pregunta != null && pregunta.gameObject.activeSelf)
                pregunta.gameObject.SetActive(false);
            return;
        }
        if (equis == null && !Armar())
            return;
        if (!equis.gameObject.activeSelf)
            equis.gameObject.SetActive(true);
        // Arriba a la derecha de la cara (como la ves tú), mirándote.
        var cab = control.Cabeza;
        Vector3 mira = caja.center - cab.position;
        if (mira.sqrMagnitude < 1e-6f)
            return;
        var giro = Quaternion.LookRotation(mira, Vector3.up);
        Vector3 der = giro * Vector3.right, arriba = giro * Vector3.up;
        float ancho = Mathf.Abs(Vector3.Dot(caja.extents, new Vector3(Mathf.Abs(der.x), Mathf.Abs(der.y), Mathf.Abs(der.z))));
        float alto = Mathf.Abs(Vector3.Dot(caja.extents, new Vector3(Mathf.Abs(arriba.x), Mathf.Abs(arriba.y), Mathf.Abs(arriba.z))));
        Vector3 pos = caja.center + der * (ancho + 0.02f) + arriba * (alto + 0.02f) - mira.normalized * 0.02f;
        equis.SetPositionAndRotation(pos, giro);
        if (pregunta.gameObject.activeSelf)
            pregunta.SetPositionAndRotation(pos - der * 0.12f - arriba * 0.05f - mira.normalized * 0.01f, giro);
    }

    // La capa "Bocas" (la de la cara de muestra) con líneas, y la caja que ocupan.
    static int BuscarCapa(Dibujo dibujo, out Bounds caja)
    {
        caja = new Bounds();
        int capa = -1;
        for (int i = 0; i < dibujo.capas.Count; i++)
            if (dibujo.capas[i] != null && dibujo.capas[i].nombre == "Bocas" && dibujo.capas[i].visible)
                capa = i;
        if (capa < 0)
            return -1;
        bool hay = false;
        foreach (var t in dibujo.trazos)
        {
            if (t == null || t.capa != capa)
                continue;
            var r = t.GetComponent<Renderer>();
            if (r == null)
                continue;
            if (!hay)
                caja = r.bounds;
            else
                caja.Encapsulate(r.bounds);
            hay = true;
        }
        return hay ? capa : -1;
    }

    bool Armar()
    {
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (nav == null)
            return false;
        equis = new GameObject("XBocas").transform;
        equis.SetParent(transform, false);
        btnEquis = PanelTutoriales.Boton(equis, "X", Vector3.zero, new Vector2(0.024f, 0.024f), nav.materialBoton, nav.materialBotonMarcado);
        BotonTocable.PonerNombre(btnEquis, "Borrar las bocas");
        btnEquis.alTocar.AddListener(() => { if (pregunta != null) pregunta.gameObject.SetActive(true); });

        pregunta = new GameObject("PreguntaBocas").transform;
        pregunta.SetParent(transform, false);
        var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(f.GetComponent<Collider>());
        f.transform.SetParent(pregunta, false);
        f.transform.localPosition = new Vector3(0f, 0f, 0.005f);
        f.transform.localScale = new Vector3(0.22f, 0.08f, 0.004f);
        var r = f.GetComponent<Renderer>();
        r.sharedMaterial = nav.materialPanel;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var t = PanelTutoriales.Texto(pregunta, "", new Vector3(0f, 0.018f, -0.002f), new Vector2(0.2f, 0.03f), 0.12f, null);
        Idioma.Poner(t, "¿Seguro que quieres borrar estas bocas de la escena?");
        var si = PanelTutoriales.Boton(pregunta, "", new Vector3(-0.05f, -0.018f, 0f), new Vector2(0.09f, 0.024f), nav.materialBotonMarcado, nav.materialBoton);
        Idioma.Poner(si.etiqueta, "Sí, borrar");
        si.etiqueta.color = Color.white;
        si.textoNormal = Color.white;
        si.alTocar.AddListener(Borrar);
        var no = PanelTutoriales.Boton(pregunta, "", new Vector3(0.05f, -0.018f, 0f), new Vector2(0.09f, 0.024f), nav.materialBoton, nav.materialBotonMarcado);
        Idioma.Poner(no.etiqueta, "No");
        no.alTocar.AddListener(() => pregunta.gameObject.SetActive(false));
        pregunta.gameObject.SetActive(false);
        return true;
    }

    void Borrar()
    {
        pregunta.gameObject.SetActive(false);
        var dibujo = control != null ? control.dibujo : null;
        if (dibujo == null || capaBocas < 0)
            return;
        if (!dibujo.BorrarCapa(capaBocas))
        {
            // Era la única capa: se borran sus líneas.
            dibujo.GuardarParaDeshacer();
            foreach (var tr in dibujo.trazos.ToArray())
                if (tr != null && tr.capa == capaBocas)
                    dibujo.BorrarTrazo(tr, false);
        }
        if (dibujo.lipsync != null)
            dibujo.lipsync.Restaurar(null, "", false);
        dibujo.Mensaje("Bocas borradas (Deshacer las devuelve)");
        control.SonidoBurbuja(equis.position);
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Panel de animación y capas. Aparece cuando miras hacia arriba, siempre en el mismo lugar respecto a ti.
//  - Línea de tiempo (200 fotogramas): toca la barra con el índice derecho para ir a un fotograma.
//  - Botones: Inicio, <, Play/Pausa, >, + Clave, - Clave.
//  - Capas 1 a 4: tocar el nombre = dibujar en esa capa; "Ver/Oculta" = mostrar u ocultar.
public class PanelArriba : MonoBehaviour
{
    public ControlManos control;
    public Dibujo dibujo;
    public Animacion animacion;
    public GameObject contenido;
    [Tooltip("Barra de la línea de tiempo (un cubo hijo de Contenido)")]
    public Transform barra;
    public Transform cabezal;
    public Material materialClave;
    public TMP_Text textoFotograma;
    public BotonTocable btnInicio, btnAnterior, btnPlay, btnSiguiente, btnClave, btnQuitarClave;
    public BotonTocable[] btnCapas = new BotonTocable[0];
    public BotonTocable[] btnVer = new BotonTocable[0];
    [Tooltip("Qué tanto hay que mirar hacia arriba para que aparezca (0 a 1)")]
    public float mirarArribaEntra = 0.35f;
    public float mirarArribaSale = 0.15f;
    public float distancia = 0.45f;
    public float altura = 0.32f;
    public float suavizado = 12f;

    bool visible;
    Quaternion giro = Quaternion.identity;
    readonly List<Transform> marcas = new List<Transform>();

    void Start()
    {
        if (control == null) control = FindFirstObjectByType<ControlManos>();
        if (dibujo == null) dibujo = FindFirstObjectByType<Dibujo>();
        if (animacion == null) animacion = FindFirstObjectByType<Animacion>();

        if (animacion != null)
        {
            Conectar(btnInicio, animacion.Inicio);
            Conectar(btnAnterior, animacion.Anterior);
            Conectar(btnPlay, animacion.AlternarReproducir);
            Conectar(btnSiguiente, animacion.Siguiente);
            Conectar(btnClave, animacion.AgregarClave);
            Conectar(btnQuitarClave, animacion.QuitarClave);
        }
        if (dibujo != null)
        {
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
        if (contenido != null)
            contenido.SetActive(false);
    }

    static void Conectar(BotonTocable boton, UnityAction accion)
    {
        if (boton != null)
            boton.alTocar.AddListener(accion);
    }

    void Update()
    {
        var cabeza = control != null ? control.Cabeza : null;
        if (cabeza == null || contenido == null)
            return;

        float arriba = cabeza.forward.y;
        bool ver = visible ? arriba > mirarArribaSale : arriba > mirarArribaEntra;
        if (ver != visible)
        {
            visible = ver;
            contenido.SetActive(ver);
            if (ver)
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
            return;

        Colocar(cabeza, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
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

    // Tocar la barra con el índice derecho = ir a ese fotograma (se puede arrastrar).
    void RevisarBarra()
    {
        if (barra == null || animacion == null || control == null || !control.Der.valida)
            return;
        Vector3 local = barra.InverseTransformPoint(control.Der.indice);
        bool encima = Mathf.Abs(local.y) < 1.2f && Mathf.Abs(local.z) < 2.5f && local.x > -0.55f && local.x < 0.55f;
        if (!encima)
            return;
        int f = Mathf.RoundToInt(Mathf.Clamp01(local.x + 0.5f) * (Animacion.TotalFotogramas - 1));
        if (f != animacion.Fotograma)
            animacion.IrA(f);
    }

    Vector3 PosicionEnBarra(int f, float z)
    {
        float x = f / (float)(Animacion.TotalFotogramas - 1) - 0.5f;
        Vector3 mundo = barra.TransformPoint(new Vector3(x, 0f, 0f));
        Vector3 local = contenido.transform.InverseTransformPoint(mundo);
        local.z = z;
        return local;
    }

    void Refrescar()
    {
        if (animacion == null || barra == null)
            return;

        if (cabezal != null)
            cabezal.localPosition = PosicionEnBarra(animacion.Fotograma, -0.006f);

        int n = 0;
        foreach (var c in animacion.claves)
        {
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
            m.localPosition = PosicionEnBarra(c.fotograma, -0.005f);
        }
        for (int i = n; i < marcas.Count; i++)
            if (marcas[i].gameObject.activeSelf)
                marcas[i].gameObject.SetActive(false);

        if (textoFotograma != null)
        {
            string texto = "Fotograma " + (animacion.Fotograma + 1) + " / " + Animacion.TotalFotogramas;
            if (animacion.EsClave(animacion.Fotograma))
                texto += "  (clave)";
            textoFotograma.text = texto;
        }
        if (btnPlay != null)
            btnPlay.PonerTexto(animacion.Reproduciendo ? "Pausa" : "Play");

        if (dibujo != null)
        {
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
    }
}

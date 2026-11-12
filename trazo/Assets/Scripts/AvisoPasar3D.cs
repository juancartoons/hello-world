using System.Collections.Generic;
using TMPro;
using UnityEngine;

// v62: "¿PASAR A 3D?": si estás dibujando en 2D (capa con plano) y empiezas a dibujar LEJOS de la hoja (más de unos
// 20 cm, dos manos), en el centro de tu vista sale este aviso con dos botones:
//  - "Pasar a 3D": la capa pasa a dibujo libre en 3D (lo que ya estaba en la hoja se queda en su hoja).
//  - "Seguir en 2D": no vuelve a preguntar en esa capa (hasta que abras otro archivo).
// Se esconde solo a los pocos segundos si no tocas nada.
public class AvisoPasar3D : MonoBehaviour
{
    const float Distancia = 0.2f;   // metros desde la hoja
    public static float DistanciaAviso => Distancia;

    static AvisoPasar3D instancia;
    Transform raiz;
    Dibujo dibujo;
    ControlManos control;
    float hasta;
    string archivo = "";
    readonly HashSet<int> capasEn2D = new HashSet<int>();

    public static void Pedir(ControlManos control, Dibujo dibujo)
    {
        if (control == null || dibujo == null || control.Cabeza == null || Tutorial.EnCurso || MesaEntrada.Activa)
            return;
        if (instancia == null)
            instancia = new GameObject("AvisoPasar3D").AddComponent<AvisoPasar3D>();
        instancia.Mostrar(control, dibujo);
    }

    void Mostrar(ControlManos c, Dibujo d)
    {
        control = c;
        dibujo = d;
        string nombre = d.NombreArchivo ?? "";
        if (nombre != archivo)
        {
            archivo = nombre;
            capasEn2D.Clear();
        }
        if (capasEn2D.Contains(d.capaActual))
            return;
        if (raiz != null && raiz.gameObject.activeSelf)
        {
            hasta = Time.time + 6f;
            return;
        }
        if (raiz == null && !Armar())
            return;
        var cab = c.Cabeza;
        Vector3 adelante = cab.forward;
        Vector3 pos = cab.position + adelante * 0.45f;
        raiz.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cab.position, Vector3.up));
        raiz.gameObject.SetActive(true);
        hasta = Time.time + 6f;
        c.SonidoBurbuja(pos);
    }

    bool Armar()
    {
        var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
        if (nav == null)
            return false;
        raiz = new GameObject("Panel").transform;
        raiz.SetParent(transform, false);
        var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(f.GetComponent<Collider>());
        f.transform.SetParent(raiz, false);
        f.transform.localPosition = new Vector3(0f, 0f, 0.005f);
        f.transform.localScale = new Vector3(0.21f, 0.075f, 0.004f);
        var r = f.GetComponent<Renderer>();
        r.sharedMaterial = nav.materialPanel;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var t = PanelTutoriales.Texto(raiz, "", new Vector3(0f, 0.018f, -0.002f), new Vector2(0.19f, 0.022f), 0.16f, null);
        Idioma.Poner(t, "¿Pasar a 3D?");
        t.fontStyle = FontStyles.Bold;
        var si = PanelTutoriales.Boton(raiz, "", new Vector3(-0.05f, -0.014f, 0f), new Vector2(0.09f, 0.026f), nav.materialBotonMarcado, nav.materialBoton);
        Idioma.Poner(si.etiqueta, "Pasar a 3D");
        si.etiqueta.color = Color.white;
        si.textoNormal = Color.white;
        si.alTocar.AddListener(PasarA3D);
        var no = PanelTutoriales.Boton(raiz, "", new Vector3(0.05f, -0.014f, 0f), new Vector2(0.09f, 0.026f), nav.materialBoton, nav.materialBotonMarcado);
        Idioma.Poner(no.etiqueta, "Seguir en 2D");
        no.alTocar.AddListener(SeguirEn2D);
        raiz.gameObject.SetActive(false);
        return true;
    }

    void PasarA3D()
    {
        if (dibujo != null)
            dibujo.PonerPlano(false);
        Esconder();
    }

    void SeguirEn2D()
    {
        if (dibujo != null)
        {
            capasEn2D.Add(dibujo.capaActual);
            dibujo.Mensaje("Sigues en 2D: acerca el dedo a la hoja para dibujar");
        }
        Esconder();
    }

    void Esconder()
    {
        if (raiz != null)
        {
            if (control != null)
                control.SonidoBurbuja(raiz.position);
            raiz.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (raiz == null || !raiz.gameObject.activeSelf)
            return;
        if (Time.time > hasta || MesaEntrada.Activa || (dibujo != null && !dibujo.PlanoActivo))
            raiz.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }
}

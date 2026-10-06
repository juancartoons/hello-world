using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Optimización para que la app corra más fluida (y maree menos):
// - Siempre: foveated rendering fijo en nivel BAJO y "dinámico" (solo baja el detalle de los bordes de la vista
//   cuando la gafa lo necesita; si va sobrada, se ve con calidad completa).
// - Con el botón secreto (círculo blanco pequeño en la esquina del menú de afuera; gris = activado):
//   ajustes de URP para Quest (sin sombras en tiempo real, sin HDR, antialiasing 2x, sin texturas internas que no usamos)
//   y fundido a negro corto al teletransportarse.
// La opción se recuerda entre sesiones.
public class ModoOptimizado : MonoBehaviour
{
    public Renderer boton;
    public Color colorApagado = Color.white;
    public Color colorEncendido = new Color(0.55f, 0.55f, 0.58f);
    [Tooltip("Segundos que tarda en oscurecerse (y en aclararse) al teletransportarse")]
    public float duracionFundido = 0.12f;

    const string clave = "FarmaciaVR_Optimizado";
    static ModoOptimizado actual;

    public bool Activado { get; private set; }

    UniversalRenderPipelineAsset urp;
    bool guardado;
    float sombrasOriginal;
    int msaaOriginal;
    bool hdrOriginal, profundidadOriginal, opacaOriginal;
    LightShadows sombrasLuzOriginal;
    Light sol;
    Renderer cortina;
    MaterialPropertyBlock bloque;
    Coroutine rutinaFundido;
    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int idColor = Shader.PropertyToID("_Color");

    void OnEnable() => actual = this;

    void OnDisable()
    {
        if (actual == this)
            actual = null;
    }

    void Start()
    {
        ActivarFoveatedRendering();
        urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        var luz = GameObject.Find("Directional Light");
        sol = luz != null ? luz.GetComponent<Light>() : null;
        CrearCortina();
        Aplicar(PlayerPrefs.GetInt(clave, 0) == 1);
    }

    static void ActivarFoveatedRendering()
    {
        try
        {
            OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.Low;
            OVRManager.useDynamicFoveatedRendering = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("ModoOptimizado: no se pudo activar el foveated rendering: " + e.Message);
        }
    }

    // Lo llama el botón secreto.
    public void Alternar()
    {
        Aplicar(!Activado);
        PlayerPrefs.SetInt(clave, Activado ? 1 : 0);
        PlayerPrefs.Save();
    }

    void Aplicar(bool activar)
    {
        Activado = activar;
        if (urp != null)
        {
            if (!guardado)
            {
                sombrasOriginal = urp.shadowDistance;
                msaaOriginal = urp.msaaSampleCount;
                hdrOriginal = urp.supportsHDR;
                profundidadOriginal = urp.supportsCameraDepthTexture;
                opacaOriginal = urp.supportsCameraOpaqueTexture;
                if (sol != null) sombrasLuzOriginal = sol.shadows;
                guardado = true;
            }
            urp.shadowDistance = activar ? 0f : sombrasOriginal;
            urp.msaaSampleCount = activar ? Mathf.Min(2, msaaOriginal) : msaaOriginal;
            urp.supportsHDR = !activar && hdrOriginal;
            urp.supportsCameraDepthTexture = !activar && profundidadOriginal;
            urp.supportsCameraOpaqueTexture = !activar && opacaOriginal;
        }
        if (sol != null && guardado)
            sol.shadows = activar ? LightShadows.None : sombrasLuzOriginal;
        Pintar();
    }

    void Pintar()
    {
        if (boton == null)
            return;
        if (bloque == null)
            bloque = new MaterialPropertyBlock();
        Color c = Activado ? colorEncendido : colorApagado;
        boton.GetPropertyBlock(bloque);
        bloque.SetColor(idBaseColor, c);
        bloque.SetColor(idColor, c);
        boton.SetPropertyBlock(bloque);
    }

    // ---------- Fundido a negro al teletransportarse ----------

    // Los teletransportes llaman esto: si el modo está activado, oscurece, mueve al jugador y aclara;
    // si no, mueve al jugador de una vez (como siempre).
    public static void Teletransportar(Action mover)
    {
        if (actual == null || !actual.Activado || actual.cortina == null || !actual.isActiveAndEnabled)
        {
            mover();
            return;
        }
        if (actual.rutinaFundido != null)
            actual.StopCoroutine(actual.rutinaFundido);
        actual.rutinaFundido = actual.StartCoroutine(actual.Fundido(mover));
    }

    IEnumerator Fundido(Action mover)
    {
        cortina.enabled = true;
        for (float t = 0f; t < duracionFundido; t += Time.deltaTime)
        {
            Oscuridad(t / duracionFundido);
            yield return null;
        }
        Oscuridad(1f);
        mover();
        yield return null;
        for (float t = 0f; t < duracionFundido; t += Time.deltaTime)
        {
            Oscuridad(1f - t / duracionFundido);
            yield return null;
        }
        Oscuridad(0f);
        cortina.enabled = false;
        rutinaFundido = null;
    }

    void Oscuridad(float a)
    {
        if (bloque == null)
            bloque = new MaterialPropertyBlock();
        cortina.GetPropertyBlock(bloque);
        bloque.SetColor(idBaseColor, new Color(0f, 0f, 0f, Mathf.Clamp01(a)));
        cortina.SetPropertyBlock(bloque);
    }

    // Una "cortina" negra pegada a la vista que se dibuja por encima de todo.
    void CrearCortina()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        var plano = Shader.Find("FarmaciaVR/Plano");
        if (rig == null || rig.centerEyeAnchor == null || plano == null)
            return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "CortinaFundido";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(rig.centerEyeAnchor, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.3f);
        go.transform.localScale = new Vector3(3f, 3f, 1f);
        var m = new Material(plano);
        m.SetColor("_BaseColor", Color.black);
        m.SetFloat("_UseVertexColor", 0f);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_ZTest", (float)CompareFunction.Always);
        m.renderQueue = 4500;
        cortina = go.GetComponent<Renderer>();
        cortina.sharedMaterial = m;
        cortina.shadowCastingMode = ShadowCastingMode.Off;
        cortina.enabled = false;
    }
}

using UnityEngine;

// v68: AVISO DE GESTO (parte de ControlManos). Cuando un gesto de la mano se activa, las puntas de los dedos que lo
// forman se encienden con una lucecita celeste (como en los ejemplos de Meta) y suena un "tic" muy suave.
// Así se sabe al instante que el visor lo reconoció (sobre todo el de GROSOR: pulgar + anular).
//  - Dibujar: pulgar + índice.   Recta: pulgar + índice + medio.   Nodos: pulgar + medio.   Grosor: pulgar + anular.
//  - Transformar: pulgar + índice de las dos manos.   El puño (borrar) no lleva luz: la mano se vuelve borrador.
//  - En la mesa de animación: grosor (pulgar + anular) y nodos (pulgar + medio).
// La luz se queda encendida mientras dure el gesto; al activarse da un destello un poco más grande.
public partial class ControlManos
{
    const int MaxBrillos = 6;
    const float TamBrillo = 0.026f;       // metros (el destello lo agranda un poco)
    readonly Transform[] brillos = new Transform[MaxBrillos];
    readonly Vector3[] puntosBrillo = new Vector3[MaxBrillos];
    Material matBrillo;
    Texture2D texBrillo;
    Mesh mallaLuzDedo;
    bool brillosHechos;
    float destelloDesde = -10f;
    float ultimoTic = -10f;
    bool mesaGrosorAntes, mesaNodosAntes;
    AudioSource fuenteGesto;

    // Desde EntrarEnGesto: destello + "tic" (cada gesto con su tono).
    void AvisarGesto(Gesto g)
    {
        if (g == Gesto.Ninguno || g == Gesto.Borrar)
            return;
        destelloDesde = Time.time;
        float tono = g == Gesto.Grosor ? 0.85f
                   : g == Gesto.Nodos ? 1.12f
                   : g == Gesto.Recta ? 1.25f
                   : g == Gesto.Transformar ? 0.75f
                   : 1f;
        TicGesto(tono, Izq.valida ? Izq.pulgar : transform.position);
    }

    void TicGesto(float tono, Vector3 donde)
    {
        if (Time.time - ultimoTic < 0.25f)
            return;
        ultimoTic = Time.time;
        if (fuenteGesto == null)
        {
            var go = new GameObject("SonidoGesto");
            go.transform.SetParent(transform, false);
            fuenteGesto = go.AddComponent<AudioSource>();
            fuenteGesto.playOnAwake = false;
            fuenteGesto.spatialBlend = 0.6f;
            fuenteGesto.minDistance = 0.3f;
        }
        fuenteGesto.transform.position = donde;
        fuenteGesto.pitch = tono;
        fuenteGesto.PlayOneShot(Sonidos.Gesto, 0.45f);
    }

    // Cada cuadro (desde LateUpdate): qué puntas brillan y dónde.
    void ActualizarBrilloDedos()
    {
        int n = 0;
        if (MesaEntrada.Activa)
        {
            bool grosor = GrosorIzquierdo;
            bool nodos = !grosor && NodosIzquierdo;
            if ((grosor && !mesaGrosorAntes) || (nodos && !mesaNodosAntes))
            {
                destelloDesde = Time.time;
                TicGesto(grosor ? 0.85f : 1.12f, Izq.pulgar);
            }
            mesaGrosorAntes = grosor;
            mesaNodosAntes = nodos;
            if (grosor)
            {
                puntosBrillo[n++] = Izq.pulgar;
                puntosBrillo[n++] = Izq.anular;
            }
            else if (nodos)
            {
                puntosBrillo[n++] = Izq.pulgar;
                puntosBrillo[n++] = Izq.medio;
            }
        }
        else
        {
            mesaGrosorAntes = false;
            mesaNodosAntes = false;
            if (Izq.valida)
            {
                switch (GestoIzq)
                {
                    case Gesto.Dibujar:
                        puntosBrillo[n++] = Izq.pulgar;
                        puntosBrillo[n++] = Izq.indice;
                        break;
                    case Gesto.Recta:
                        puntosBrillo[n++] = Izq.pulgar;
                        puntosBrillo[n++] = Izq.indice;
                        puntosBrillo[n++] = Izq.medio;
                        break;
                    case Gesto.Nodos:
                        puntosBrillo[n++] = Izq.pulgar;
                        puntosBrillo[n++] = Izq.medio;
                        break;
                    case Gesto.Grosor:
                        puntosBrillo[n++] = Izq.pulgar;
                        puntosBrillo[n++] = Izq.anular;
                        break;
                    case Gesto.Transformar:
                        puntosBrillo[n++] = Izq.pulgar;
                        puntosBrillo[n++] = Izq.indice;
                        if (Der.valida)
                        {
                            puntosBrillo[n++] = Der.pulgar;
                            puntosBrillo[n++] = Der.indice;
                        }
                        break;
                }
            }
        }

        if (n == 0 && !brillosHechos)
            return;
        if (!brillosHechos)
            CrearBrillos();

        Vector3 ojo = Cabeza != null ? Cabeza.position : Vector3.zero;
        float k = Mathf.Clamp01((Time.time - destelloDesde) / 0.35f);
        float destello = 1f - k;
        float tam = TamBrillo * (1f + destello * 0.7f + Mathf.Sin(Time.time * 6f) * 0.06f);
        if (matBrillo != null && matBrillo.HasProperty("_Alfa"))
            matBrillo.SetFloat("_Alfa", 0.8f + 0.2f * destello);
        for (int i = 0; i < MaxBrillos; i++)
        {
            var b = brillos[i];
            if (b == null)
                continue;
            bool ver = i < n;
            if (b.gameObject.activeSelf != ver)
                b.gameObject.SetActive(ver);
            if (!ver)
                continue;
            Vector3 p = puntosBrillo[i];
            Vector3 haciaOjo = ojo - p;
            if (haciaOjo.sqrMagnitude < 1e-6f)
                haciaOjo = Vector3.back;
            haciaOjo.Normalize();
            // Un poquito hacia tus ojos: así la luz queda delante del guante y no se esconde dentro del dedo.
            b.position = p + haciaOjo * 0.012f;
            b.rotation = Quaternion.LookRotation(-haciaOjo, Vector3.up);
            b.localScale = Vector3.one * tam;
        }
    }

    void CrearBrillos()
    {
        brillosHechos = true;
        var sh = Shader.Find("TrazoVR/Logo");
        if (sh != null)
        {
            texBrillo = TexturaBrillo(64);
            matBrillo = new Material(sh);
            matBrillo.SetTexture("_BaseMap", texBrillo);
            matBrillo.SetFloat("_Revela", 1f);
            matBrillo.SetFloat("_Alfa", 1f);
            matBrillo.SetFloat("_Ola", 0f);
            matBrillo.SetColor("_Tinte", Color.white);
            mallaLuzDedo = CuadroBrillo();
        }
        else
        {
            // Sin ese shader: una bolita celeste (el color del destello de la mano).
            matBrillo = materialDestelloMano;
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mallaLuzDedo = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
        }
        for (int i = 0; i < MaxBrillos; i++)
        {
            var go = new GameObject("BrilloDedo" + i);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mallaLuzDedo;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = matBrillo;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.SetActive(false);
            brillos[i] = go.transform;
        }
    }

    // Un cuadrito de 1 x 1 centrado (se ve de los dos lados: el shader no recorta caras).
    static Mesh CuadroBrillo()
    {
        var m = new Mesh { name = "BrilloDedo" };
        m.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
        };
        m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        m.RecalculateBounds();
        return m;
    }

    // Una lucecita redonda: blanca en el centro, celeste alrededor y que se desvanece hacia la orilla.
    static Texture2D TexturaBrillo(int lado)
    {
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var celeste = new Color(0.5f, 0.85f, 1f);
        var px = new Color[lado * lado];
        float c = (lado - 1) * 0.5f;
        for (int y = 0; y < lado; y++)
            for (int x = 0; x < lado; x++)
            {
                float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - r);
                a = a * a * (3f - 2f * a);
                float centro = Mathf.Clamp01(1f - r / 0.35f);
                var col = Color.Lerp(celeste, Color.white, centro);
                col.a = Mathf.Clamp01(a * 1.25f);
                px[y * lado + x] = col;
            }
        tex.SetPixels(px);
        tex.Apply(false, true);
        return tex;
    }
}

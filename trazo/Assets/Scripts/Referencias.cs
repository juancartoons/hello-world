using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

[System.Serializable]
public class DatosImagen
{
    public string archivo;
    public Vector3 posicion;
    public Quaternion rotacion = Quaternion.identity;
    public float escala = 0.4f;
    public bool pegada;            // pegada al plano 2D (sigue al dibujo)
    public Vector3 posicionLocal;  // respecto al dibujo, si está pegada
    public Quaternion rotacionLocal = Quaternion.identity;
    public float escalaLocal = 0.4f;
    public int vista;              // 0 = 100%, 1 = 50%, 2 = 20%, 3 = oculta
}

[System.Serializable]
public class DatosReferencias
{
    public List<DatosImagen> imagenes = new List<DatosImagen>();
    public bool ocultas;           // "Imágenes: ocultar" (todas)
}

// Imágenes de referencia: se ven en las gafas pero NO salen en las fotos ni en los videos exportados.
//  - Copia tus imágenes (png o jpg) a la carpeta Dibujos/Imagenes de la app.
//  - "Imagen +" pone la siguiente imagen frente a ti.
//  - Pellizca una imagen con la derecha para moverla (queda seleccionada, con un tono azul).
//  - Con una imagen seleccionada, pellizca con las dos manos: escalar, girar y mover.
//  - "Imagen -" quita la imagen seleccionada. Todo queda guardado donde lo dejes.
//  - En modo Plano (2D): si sueltas una imagen cerca del plano, se PEGA detrás de él como imán
//    (para calcar). Si mueves, giras o escalas el dibujo, la imagen lo sigue.
//    Arriba a la derecha de una imagen pegada aparecen los botones "Ver" (100%, 50%, 20%, oculta) y "Despegar".
//  - "Imágenes: ver / ocultar" (página Medios) las muestra o esconde todas a la vez.
public class Referencias : MonoBehaviour
{
    public Dibujo dibujo;
    [Tooltip("Material base (URP Unlit) para las imágenes")]
    public Material materialImagen;
    public Color colorSeleccion = new Color(0.7f, 0.82f, 1f);
    [Tooltip("Material base transparente (para ver las imágenes al 50% o 20%)")]
    public Material materialImagenTransparente;
    [Tooltip("Material de los botones Despegar y Ver")]
    public Material materialBoton;
    [Tooltip("Qué tan cerca del plano (metros) hay que soltar la imagen para que se pegue")]
    public float distanciaIman = 0.08f;
    [Tooltip("Qué tan detrás del plano queda la imagen pegada (metros)")]
    public float detrasDelPlano = 0.004f;

    class Imagen
    {
        public DatosImagen datos;
        public Transform raiz;
        public Material material;
        public Texture2D textura;
        public float aspecto = 1f;
        public Transform boton;   // botones "Ver" y "Despegar" (solo si está pegada)
        public BotonTocable botonVer;
        public Material transparente;
        public Renderer render;
        public Color tinte = Color.white;
    }

    static readonly float[] Opacidades = { 1f, 0.5f, 0.2f, 0f };
    static readonly string[] NombresVista = { "Ver 100%", "Ver 50%", "Ver 20%", "Oculta" };

    readonly List<Imagen> imagenes = new List<Imagen>();
    bool ocultas;
    public bool Ocultas => ocultas;
    Imagen seleccionada;
    int siguienteArchivo;

    string Carpeta => Path.Combine(Application.persistentDataPath, "Dibujos", "Imagenes");
    string RutaEstado => Path.Combine(Application.persistentDataPath, "Dibujos", "referencias.json");

    public Transform Seleccionada => seleccionada != null ? seleccionada.raiz : null;

    void Start()
    {
        if (dibujo == null)
            dibujo = FindFirstObjectByType<Dibujo>();
        CargarEstado();
    }

    void Update()
    {
        // El botón "Despegar" sigue la esquina de arriba a la derecha de cada imagen pegada.
        foreach (var img in imagenes)
        {
            if (img.boton == null || img.raiz == null)
                continue;
            bool ver = img.datos.pegada && !ocultas;
            if (img.boton.gameObject.activeSelf != ver)
                img.boton.gameObject.SetActive(ver);
            if (!ver)
                continue;
            Vector3 esquina = img.raiz.TransformPoint(new Vector3(img.aspecto * 0.5f, 0.5f, 0f));
            Vector3 hacia = -img.raiz.forward; // hacia ti
            img.boton.SetPositionAndRotation(esquina + hacia * 0.01f - img.raiz.right * 0.03f + img.raiz.up * 0.015f, img.raiz.rotation);
        }
    }

    Imagen Buscar(Transform raiz)
    {
        if (raiz == null)
            return null;
        foreach (var img in imagenes)
            if (img.raiz == raiz)
                return img;
        return null;
    }

    public bool EstaPegada(Transform raiz)
    {
        var img = Buscar(raiz);
        return img != null && img.datos.pegada;
    }

    // Mover una imagen (con el pellizco). Si está pegada y hay plano, se desliza sobre el plano.
    public void MoverA(Transform raiz, Vector3 posicion)
    {
        var img = Buscar(raiz);
        if (img == null)
            return;
        Vector3 punto, normal;
        if (img.datos.pegada && dibujo != null && dibujo.PlanoMundo(out punto, out normal))
        {
            normal = NormalLejos(normal, posicion);
            posicion = posicion - normal * Vector3.Dot(posicion - punto, normal) + normal * detrasDelPlano * dibujo.EscalaMundo;
        }
        raiz.position = posicion;
    }

    // Al soltar una imagen (o terminar de girarla/escalarla): si está cerca del plano, se pega.
    public void AlSoltar(Transform raiz)
    {
        var img = Buscar(raiz);
        if (img == null || dibujo == null)
            return;
        Vector3 punto, normal;
        if (dibujo.PlanoMundo(out punto, out normal))
        {
            normal = NormalLejos(normal, raiz.position);
            float distancia = Mathf.Abs(Vector3.Dot(raiz.position - punto, normal));
            if (img.datos.pegada || distancia < distanciaIman)
            {
                bool nueva = !img.datos.pegada;
                Pegar(img, punto, normal);
                if (nueva)
                    Mensaje("Imagen pegada al plano");
            }
        }
        Guardar();
    }

    // La normal del plano apuntando lejos de ti (la imagen va detrás del plano).
    Vector3 NormalLejos(Vector3 normal, Vector3 cerca)
    {
        var control = ControlManos.Instancia;
        if (control != null && control.Cabeza != null && Vector3.Dot(normal, cerca - control.Cabeza.position) < 0f)
            return -normal;
        return normal;
    }

    void Pegar(Imagen img, Vector3 punto, Vector3 normal)
    {
        var raiz = img.raiz;
        Vector3 pos = raiz.position;
        pos = pos - normal * Vector3.Dot(pos - punto, normal) + normal * detrasDelPlano * dibujo.EscalaMundo;
        // Mira igual que el plano, conservando su giro dentro del plano.
        Vector3 arriba = Vector3.ProjectOnPlane(raiz.up, normal);
        if (arriba.sqrMagnitude < 1e-4f)
            arriba = Vector3.ProjectOnPlane(Vector3.up, normal);
        if (arriba.sqrMagnitude < 1e-4f)
            arriba = Vector3.ProjectOnPlane(Vector3.forward, normal);
        raiz.SetPositionAndRotation(pos, Quaternion.LookRotation(normal, arriba.normalized));
        raiz.SetParent(dibujo.transform, true);
        img.datos.pegada = true;
        AsegurarBoton(img);
    }

    void Despegar(Imagen img)
    {
        if (img == null || img.raiz == null || !img.datos.pegada)
            return;
        img.raiz.SetParent(transform, true);
        img.datos.pegada = false;
        if (img.boton != null)
            img.boton.gameObject.SetActive(false);
        Guardar();
        Mensaje("Imagen despegada");
    }

    // Botones "Ver" y "Despegar" (se tocan con el índice derecho).
    void AsegurarBoton(Imagen img)
    {
        if (img.boton != null)
            return;
        var contenedor = new GameObject("BotonesImagen").transform;
        contenedor.SetParent(transform, false);
        var esta = img;
        var despegar = CrearBoton(contenedor, "Despegar", 0f);
        despegar.alTocar.AddListener(() => Despegar(esta));
        img.botonVer = CrearBoton(contenedor, NombresVista[Mathf.Clamp(img.datos.vista, 0, 3)], -0.06f);
        img.botonVer.alTocar.AddListener(() => SiguienteVista(esta));
        img.boton = contenedor;
    }

    BotonTocable CrearBoton(Transform contenedor, string nombre, float x)
    {
        var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Boton_" + nombre;
        cubo.transform.SetParent(contenedor, false);
        cubo.transform.localPosition = new Vector3(x, 0f, 0f);
        cubo.transform.localScale = new Vector3(0.055f, 0.02f, 0.006f);
        var r = cubo.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (materialBoton != null)
            r.sharedMaterial = materialBoton;
        var boton = cubo.AddComponent<BotonTocable>();
        boton.materialNormal = materialBoton;
        boton.materialMarcado = materialBoton;
        var texto = new GameObject("Texto", typeof(RectTransform));
        texto.transform.SetParent(contenedor, false);
        texto.transform.localPosition = new Vector3(x, 0f, -0.0035f);
        var tmp = texto.AddComponent<TextMeshPro>();
        tmp.text = nombre;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.01f;
        tmp.fontSizeMax = 0.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.black;
        tmp.rectTransform.sizeDelta = new Vector2(0.05f, 0.016f);
        boton.etiqueta = tmp;
        return boton;
    }

    // 100% → 50% → 20% → oculta → 100%
    void SiguienteVista(Imagen img)
    {
        if (img == null)
            return;
        img.datos.vista = (Mathf.Clamp(img.datos.vista, 0, 3) + 1) % 4;
        AplicarVista(img);
        Guardar();
    }

    // Todas las imágenes: ver u ocultar.
    public void AlternarTodas()
    {
        ocultas = !ocultas;
        foreach (var img in imagenes)
            AplicarVista(img);
        Guardar();
        Mensaje(ocultas ? "Imágenes ocultas" : "Imágenes visibles");
    }

    void AplicarVista(Imagen img)
    {
        if (img == null)
            return;
        int vista = Mathf.Clamp(img.datos.vista, 0, 3);
        float alfa = Opacidades[vista];
        if (img.render != null)
        {
            img.render.enabled = !ocultas && alfa > 0f;
            Material m = img.material;
            if (alfa < 1f && materialImagenTransparente != null)
            {
                if (img.transparente == null)
                {
                    img.transparente = new Material(materialImagenTransparente);
                    if (img.transparente.HasProperty("_BaseMap")) img.transparente.SetTexture("_BaseMap", img.textura);
                    if (img.transparente.HasProperty("_MainTex")) img.transparente.SetTexture("_MainTex", img.textura);
                }
                m = img.transparente;
            }
            if (m != null)
            {
                Color c = img.tinte;
                c.a = alfa;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                if (img.render.sharedMaterial != m)
                    img.render.sharedMaterial = m;
            }
        }
        if (img.botonVer != null)
            img.botonVer.PonerTexto(NombresVista[vista]);
    }

    void Mensaje(string texto)
    {
        if (dibujo != null)
            dibujo.Mensaje(texto);
    }

    List<string> Archivos()
    {
        var lista = new List<string>();
        try
        {
            Directory.CreateDirectory(Carpeta);
            foreach (var f in Directory.GetFiles(Carpeta))
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                    lista.Add(f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo leer la carpeta de imágenes: " + e.Message);
        }
        lista.Sort();
        return lista;
    }

    // Pone la siguiente imagen de la carpeta frente a tus ojos.
    public void AgregarSiguiente(Transform cabeza)
    {
        var archivos = Archivos();
        if (archivos.Count == 0)
        {
            Mensaje("Copia imágenes (png o jpg) a Dibujos/Imagenes");
            return;
        }
        string ruta = archivos[siguienteArchivo % archivos.Count];
        siguienteArchivo++;
        if (ocultas)
        {
            ocultas = false;
            foreach (var otra in imagenes)
                AplicarVista(otra);
        }
        Vector3 adelante = cabeza != null ? cabeza.forward : Vector3.forward;
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 1e-4f)
            adelante = Vector3.forward;
        adelante.Normalize();
        Vector3 ojos = cabeza != null ? cabeza.position : new Vector3(0f, 1.5f, 0f);
        var d = new DatosImagen
        {
            archivo = Path.GetFileName(ruta),
            posicion = ojos + adelante * 0.7f,
            rotacion = Quaternion.LookRotation(adelante, Vector3.up),
            escala = 0.4f
        };
        var img = Crear(d);
        if (img == null)
        {
            Mensaje("No se pudo abrir " + d.archivo);
            return;
        }
        Seleccionar(img.raiz);
        Guardar();
        Mensaje("Imagen: " + d.archivo);
    }

    public void QuitarSeleccionada()
    {
        if (seleccionada == null)
        {
            Mensaje("Pellizca una imagen para elegirla");
            return;
        }
        var img = seleccionada;
        seleccionada = null;
        imagenes.Remove(img);
        Destruir(img);
        Guardar();
        Mensaje("Imagen quitada");
    }

    // La imagen que está bajo un punto (o muy cerca de su superficie).
    public Transform BuscarBajo(Vector3 mundo)
    {
        Imagen mejor = null;
        float mejorZ = float.MaxValue;
        foreach (var img in imagenes)
        {
            if (img.raiz == null || img.render == null || !img.render.enabled)
                continue;
            Vector3 l = img.raiz.InverseTransformPoint(mundo);
            float z = Mathf.Abs(l.z) * img.raiz.lossyScale.x;
            if (Mathf.Abs(l.x) <= img.aspecto * 0.55f && Mathf.Abs(l.y) <= 0.55f && z < 0.03f && z < mejorZ)
            {
                mejorZ = z;
                mejor = img;
            }
        }
        return mejor != null ? mejor.raiz : null;
    }

    public void Seleccionar(Transform raiz)
    {
        Imagen nueva = null;
        foreach (var img in imagenes)
            if (img.raiz == raiz && raiz != null)
                nueva = img;
        if (nueva == seleccionada)
            return;
        if (seleccionada != null)
            Tenir(seleccionada, Color.white);
        seleccionada = nueva;
        if (seleccionada != null)
            Tenir(seleccionada, colorSeleccion);
    }

    void Tenir(Imagen img, Color c)
    {
        img.tinte = c;
        AplicarVista(img);
    }

    Imagen Crear(DatosImagen d)
    {
        string ruta = Path.Combine(Carpeta, d.archivo ?? "");
        if (!File.Exists(ruta))
            return null;
        Texture2D tex = null;
        try
        {
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (!tex.LoadImage(File.ReadAllBytes(ruta)))
            {
                Destroy(tex);
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudo abrir la imagen: " + e.Message);
            return null;
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.anisoLevel = 4;

        var img = new Imagen { datos = d, textura = tex, aspecto = tex.width / (float)Mathf.Max(1, tex.height) };
        var raiz = new GameObject("Imagen_" + d.archivo);
        if (d.pegada && dibujo != null)
        {
            // Pegada al plano: su lugar se guarda respecto al dibujo.
            raiz.transform.SetParent(dibujo.transform, false);
            var q = d.rotacionLocal;
            raiz.transform.localPosition = d.posicionLocal;
            raiz.transform.localRotation = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 0.5f ? q : Quaternion.identity;
            raiz.transform.localScale = Vector3.one * Mathf.Clamp(d.escalaLocal > 0f ? d.escalaLocal : d.escala, 0.001f, 100f);
        }
        else
        {
            d.pegada = false;
            raiz.transform.SetParent(transform, false);
            raiz.transform.SetPositionAndRotation(d.posicion, d.rotacion);
            raiz.transform.localScale = Vector3.one * Mathf.Clamp(d.escala, 0.05f, 10f);
        }
        img.raiz = raiz.transform;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Lamina";
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(raiz.transform, false);
        quad.transform.localScale = new Vector3(img.aspecto, 1f, 1f);
        var r = quad.GetComponent<Renderer>();
        img.render = r;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (materialImagen != null)
        {
            img.material = new Material(materialImagen);
            if (img.material.HasProperty("_BaseMap")) img.material.SetTexture("_BaseMap", tex);
            if (img.material.HasProperty("_MainTex")) img.material.SetTexture("_MainTex", tex);
            r.sharedMaterial = img.material;
        }
        imagenes.Add(img);
        if (d.pegada)
            AsegurarBoton(img);
        Tenir(img, Color.white);
        return img;
    }

    void Destruir(Imagen img)
    {
        if (img.raiz != null)
            Destroy(img.raiz.gameObject);
        if (img.boton != null)
            Destroy(img.boton.gameObject);
        if (img.material != null)
            Destroy(img.material);
        if (img.transparente != null)
            Destroy(img.transparente);
        if (img.textura != null)
            Destroy(img.textura);
    }

    // Guarda dónde quedó cada imagen (se llama al soltarlas).
    public void Guardar()
    {
        var estado = new DatosReferencias { ocultas = ocultas };
        foreach (var img in imagenes)
        {
            if (img.raiz == null)
                continue;
            img.datos.posicion = img.raiz.position;
            img.datos.rotacion = img.raiz.rotation;
            img.datos.escala = img.raiz.lossyScale.x;
            img.datos.posicionLocal = img.raiz.localPosition;
            img.datos.rotacionLocal = img.raiz.localRotation;
            img.datos.escalaLocal = img.raiz.localScale.x;
            estado.imagenes.Add(img.datos);
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaEstado));
            File.WriteAllText(RutaEstado, JsonUtility.ToJson(estado, true));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudieron guardar las imágenes: " + e.Message);
        }
    }

    void CargarEstado()
    {
        DatosReferencias estado = null;
        try
        {
            if (File.Exists(RutaEstado))
                estado = JsonUtility.FromJson<DatosReferencias>(File.ReadAllText(RutaEstado));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TrazoVR: no se pudieron leer las imágenes: " + e.Message);
        }
        if (estado == null || estado.imagenes == null)
            return;
        ocultas = estado.ocultas;
        foreach (var d in estado.imagenes)
        {
            if (d == null)
                continue;
            var q = d.rotacion;
            if (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w < 0.5f)
                d.rotacion = Quaternion.identity;
            Crear(d);
        }
    }
}

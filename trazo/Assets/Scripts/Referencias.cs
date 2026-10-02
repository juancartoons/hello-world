using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class DatosImagen
{
    public string archivo;
    public Vector3 posicion;
    public Quaternion rotacion = Quaternion.identity;
    public float escala = 0.4f;
}

[System.Serializable]
public class DatosReferencias
{
    public List<DatosImagen> imagenes = new List<DatosImagen>();
}

// Imágenes de referencia: se ven en las gafas pero NO salen en las fotos ni en los videos exportados.
//  - Copia tus imágenes (png o jpg) a la carpeta Dibujos/Imagenes de la app.
//  - "Imagen +" pone la siguiente imagen frente a ti.
//  - Pellizca una imagen con la derecha para moverla (queda seleccionada, con un tono azul).
//  - Con una imagen seleccionada, pellizca con las dos manos: escalar, girar y mover.
//  - "Imagen -" quita la imagen seleccionada. Todo queda guardado donde lo dejes.
public class Referencias : MonoBehaviour
{
    public Dibujo dibujo;
    [Tooltip("Material base (URP Unlit) para las imágenes")]
    public Material materialImagen;
    public Color colorSeleccion = new Color(0.7f, 0.82f, 1f);

    class Imagen
    {
        public DatosImagen datos;
        public Transform raiz;
        public Material material;
        public Texture2D textura;
        public float aspecto = 1f;
    }

    readonly List<Imagen> imagenes = new List<Imagen>();
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
            if (img.raiz == null)
                continue;
            Vector3 l = img.raiz.InverseTransformPoint(mundo);
            float z = Mathf.Abs(l.z) * img.raiz.localScale.x;
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

    static void Tenir(Imagen img, Color c)
    {
        if (img.material == null)
            return;
        if (img.material.HasProperty("_BaseColor")) img.material.SetColor("_BaseColor", c);
        if (img.material.HasProperty("_Color")) img.material.SetColor("_Color", c);
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
        raiz.transform.SetParent(transform, false);
        raiz.transform.SetPositionAndRotation(d.posicion, d.rotacion);
        raiz.transform.localScale = Vector3.one * Mathf.Clamp(d.escala, 0.05f, 10f);
        img.raiz = raiz.transform;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Lamina";
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(raiz.transform, false);
        quad.transform.localScale = new Vector3(img.aspecto, 1f, 1f);
        var r = quad.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (materialImagen != null)
        {
            img.material = new Material(materialImagen);
            if (img.material.HasProperty("_BaseMap")) img.material.SetTexture("_BaseMap", tex);
            if (img.material.HasProperty("_MainTex")) img.material.SetTexture("_MainTex", tex);
            r.sharedMaterial = img.material;
            Tenir(img, Color.white);
        }
        imagenes.Add(img);
        return img;
    }

    void Destruir(Imagen img)
    {
        if (img.raiz != null)
            Destroy(img.raiz.gameObject);
        if (img.material != null)
            Destroy(img.material);
        if (img.textura != null)
            Destroy(img.textura);
    }

    // Guarda dónde quedó cada imagen (se llama al soltarlas).
    public void Guardar()
    {
        var estado = new DatosReferencias();
        foreach (var img in imagenes)
        {
            if (img.raiz == null)
                continue;
            img.datos.posicion = img.raiz.position;
            img.datos.rotacion = img.raiz.rotation;
            img.datos.escala = img.raiz.localScale.x;
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

using System.Collections.Generic;
using UnityEngine;

// Animación por "morph" (como las interpolaciones de forma de Flash), hasta 2000 fotogramas.
// - Una CLAVE guarda la forma de todas las líneas en un fotograma.
// - Entre dos claves, cada línea con los mismos nodos se transforma suavemente de una forma a otra.
// - Si estás en un fotograma y editas algo, se crea/actualiza la clave de ese fotograma (automático).
// - Si nunca tocas la línea de tiempo, el dibujo es normal (sin animación).
public class Animacion : MonoBehaviour
{
    public const int TotalFotogramas = 2000;
    public static readonly float[] OpcionesFps = { 12f, 24f, 30f, 60f };

    public Dibujo dibujo;
    public float fotogramasPorSegundo = 12f;
    [Tooltip("Audio que suena junto con la animación (voz para el lipsync)")]
    public AudioSource fuenteAudio;

    public readonly List<Clave> claves = new List<Clave>(); // ordenadas por fotograma
    public int Fotograma { get; private set; }
    public bool Reproduciendo { get; private set; }
    public bool Activa => claves.Count > 0;
    public event System.Action alCambiar;

    float acumulado;

    void Awake()
    {
        if (dibujo == null)
            dibujo = GetComponent<Dibujo>();
    }

    void LateUpdate()
    {
        if (Reproduciendo)
        {
            acumulado += Time.deltaTime * fotogramasPorSegundo;
            if (acumulado >= 1f)
            {
                int pasos = Mathf.FloorToInt(acumulado);
                acumulado -= pasos;
                int fin = UltimoFotograma();
                int nuevo = fin > 0 ? (Fotograma + pasos) % (fin + 1) : 0;
                if (nuevo < Fotograma)
                    SincronizarAudio(nuevo); // dio la vuelta: el audio vuelve a empezar
                Fotograma = nuevo;
                MostrarFotograma();
                Avisar();
            }
            Trazo.huboCambio = false;
            return;
        }

        // Clave automática: lo que edites queda guardado en el fotograma actual.
        if (Trazo.huboCambio)
        {
            Trazo.huboCambio = false;
            if (claves.Count > 0)
            {
                GuardarClave(Fotograma);
                Avisar();
            }
        }
    }

    // Pone a sonar el audio desde el fotograma indicado (si hay audio).
    void SincronizarAudio(int f)
    {
        if (fuenteAudio == null || fuenteAudio.clip == null)
            return;
        float t = f / Mathf.Max(1f, fotogramasPorSegundo);
        if (t >= fuenteAudio.clip.length)
        {
            fuenteAudio.Stop();
            return;
        }
        fuenteAudio.time = t;
        fuenteAudio.Play();
    }

    public int UltimoFotograma()
    {
        int fin = claves.Count > 0 ? claves[claves.Count - 1].fotograma : 0;
        return fin > 0 ? fin : TotalFotogramas - 1;
    }

    // Lo llama el Dibujo justo antes de cualquier cambio.
    // Si es la primera edición fuera del fotograma 1, guarda el dibujo original como clave en el fotograma 1.
    public void AntesDeEditar()
    {
        Pausar();
        if (claves.Count == 0 && Fotograma != 0)
            GuardarClave(0);
    }

    public bool EsClave(int f)
    {
        return BuscarClave(f) != null;
    }

    Clave BuscarClave(int f)
    {
        foreach (var c in claves)
            if (c.fotograma == f)
                return c;
        return null;
    }

    // Guarda (o reemplaza) la clave del fotograma f con la forma actual de todas las líneas.
    public void GuardarClaveEn(int f)
    {
        GuardarClave(Mathf.Clamp(f, 0, TotalFotogramas - 1));
        Avisar();
    }

    void GuardarClave(int f)
    {
        var c = BuscarClave(f);
        if (c == null)
        {
            c = new Clave { fotograma = f };
            int i = 0;
            while (i < claves.Count && claves[i].fotograma < f)
                i++;
            claves.Insert(i, c);
        }
        c.trazos.Clear();
        foreach (var t in dibujo.trazos)
            if (t != null && t.visibleAnim && t.nodos.Count >= 2)
                c.trazos.Add(t.CrearDatos());
    }

    // ---------- Botones de la línea de tiempo ----------

    public void AgregarClave()
    {
        dibujo.GuardarParaDeshacer();
        GuardarClave(Fotograma);
        Trazo.huboCambio = false;
        Avisar();
        dibujo.Mensaje("Clave en el fotograma " + (Fotograma + 1));
    }

    public void QuitarClave()
    {
        var c = BuscarClave(Fotograma);
        if (c == null)
        {
            dibujo.Mensaje("Aquí no hay clave");
            return;
        }
        dibujo.GuardarParaDeshacer();
        claves.Remove(c);
        MostrarFotograma();
        Avisar();
        dibujo.Mensaje("Clave quitada");
    }

    public void AlternarReproducir()
    {
        if (Reproduciendo)
        {
            Pausar();
            return;
        }
        if (claves.Count < 2)
        {
            dibujo.Mensaje("Necesitas al menos 2 claves");
            return;
        }
        Reproduciendo = true;
        acumulado = 0f;
        SincronizarAudio(Fotograma);
        Avisar();
    }

    public void Pausar()
    {
        if (!Reproduciendo)
            return;
        Reproduciendo = false;
        if (fuenteAudio != null)
            fuenteAudio.Stop();
        Avisar();
    }

    // Cambia la velocidad: 12 → 24 → 30 → 60 cuadros por segundo.
    public void CambiarFps()
    {
        int i = 0;
        for (int k = 0; k < OpcionesFps.Length; k++)
            if (Mathf.Approximately(OpcionesFps[k], fotogramasPorSegundo))
                i = k;
        fotogramasPorSegundo = OpcionesFps[(i + 1) % OpcionesFps.Length];
        Avisar();
        dibujo.Mensaje(fotogramasPorSegundo + " cuadros por segundo");
    }

    // Arrastrar una clave a otro fotograma.
    public void MoverClave(int desde, int hasta)
    {
        hasta = Mathf.Clamp(hasta, 0, TotalFotogramas - 1);
        var c = BuscarClave(desde);
        if (c == null || desde == hasta)
            return;
        dibujo.GuardarParaDeshacer();
        var otra = BuscarClave(hasta);
        if (otra != null)
            claves.Remove(otra);
        c.fotograma = hasta;
        claves.Sort((a, b) => a.fotograma.CompareTo(b.fotograma));
        Fotograma = hasta;
        MostrarFotograma();
        Trazo.huboCambio = false;
        Avisar();
        dibujo.Mensaje("Clave movida al fotograma " + (hasta + 1));
    }

    public void Inicio() { IrA(0); }
    public void Anterior() { IrA(Fotograma - 1); }
    public void Siguiente() { IrA(Fotograma + 1); }

    public void IrA(int f)
    {
        Pausar();
        f = Mathf.Clamp(f, 0, TotalFotogramas - 1);
        if (f == Fotograma)
            return;
        Fotograma = f;
        MostrarFotograma();
        Avisar();
    }

    // Restaura las claves (al cargar o deshacer). Con lista nula, borra la animación.
    public void Restaurar(List<Clave> lista, int f)
    {
        Reproduciendo = false;
        claves.Clear();
        if (lista != null)
            foreach (var c in lista)
                if (c != null && c.trazos != null)
                    claves.Add(c);
        claves.Sort((a, b) => a.fotograma.CompareTo(b.fotograma));
        Fotograma = Mathf.Clamp(f, 0, TotalFotogramas - 1);
        if (claves.Count == 0)
        {
            foreach (var t in dibujo.trazos)
                if (t != null)
                    t.visibleAnim = true;
        }
        MostrarFotograma();
        Avisar();
    }

    // Pone cada línea en la forma que le toca en el fotograma actual.
    public void MostrarFotograma()
    {
        if (claves.Count == 0)
            return;
        Clave a = null;
        Clave b = null;
        foreach (var c in claves)
        {
            if (c.fotograma <= Fotograma)
                a = c;
            else if (b == null)
                b = c;
        }
        if (a == null)
        {
            a = claves[0];
            b = null;
        }
        float u = b != null ? (Fotograma - a.fotograma) / (float)(b.fotograma - a.fotograma) : 0f;

        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        foreach (var t in dibujo.trazos)
        {
            if (t == null)
                continue;
            var pa = BuscarPose(a, t.id);
            if (pa == null)
            {
                t.visibleAnim = false;
                continue;
            }
            t.visibleAnim = true;
            t.AplicarPose(pa, b != null ? BuscarPose(b, t.id) : null, u);
        }
        Trazo.silenciar = antes;
        Trazo.huboCambio = false;
        dibujo.ActualizarVisibilidad();
    }

    // Al borrar un nodo, se borra en TODAS las claves (así el morph sigue funcionando).
    public void QuitarNodoEnClaves(int id, int indice, int nodosAntes)
    {
        foreach (var c in claves)
        {
            var p = BuscarPose(c, id);
            if (p == null || p.nodos == null || p.nodos.Count != nodosAntes)
                continue;
            NodosUtil.Quitar(p.nodos, p.asaEntrada, p.asaSalida, p.asaManual, p.grosorNodo, indice);
            if (p.cerrado && p.nodos.Count < 3)
            {
                p.cerrado = false;
                p.relleno = false;
            }
            if (p.nodos.Count < 2)
                c.trazos.Remove(p);
        }
    }

    // Al agregar un nodo, se agrega en el mismo lugar de la curva en TODAS las claves.
    public void InsertarNodoEnClaves(int id, int segmento, float t, int nodosAntes)
    {
        foreach (var c in claves)
        {
            var p = BuscarPose(c, id);
            if (p == null || p.nodos == null || p.nodos.Count != nodosAntes)
                continue;
            NodosUtil.Insertar(p.nodos, p.asaEntrada, p.asaSalida, p.asaManual, p.grosorNodo, p.cerrado, segmento, t);
        }
    }

    static DatosTrazo BuscarPose(Clave c, int id)
    {
        foreach (var p in c.trazos)
            if (p != null && p.id == id)
                return p;
        return null;
    }

    void Avisar()
    {
        alCambiar?.Invoke();
    }
}

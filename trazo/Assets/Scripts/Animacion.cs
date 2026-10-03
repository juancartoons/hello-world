using System.Collections.Generic;
using UnityEngine;

// Animación por "morph" (como las interpolaciones de forma de Flash), hasta 2000 fotogramas.
// - CADA CAPA TIENE SUS PROPIAS CLAVES (como en Flash): una clave guarda la forma de las líneas de su capa.
// - Entre dos claves de una capa, cada línea con los mismos nodos se transforma suavemente.
// - Si estás en un fotograma y editas algo, se crea/actualiza la clave de ESA capa en ese fotograma (automático).
// - Una capa sin claves no se anima (se queda quieta). Play reproduce todas las capas juntas.
// - Una línea NUEVA (dibujada en cualquier fotograma) aparece en toda la animación de su capa.
public class Animacion : MonoBehaviour
{
    public const int TotalFotogramas = 2000;
    public static readonly float[] OpcionesFps = { 12f, 24f, 30f, 60f };

    public Dibujo dibujo;
    public float fotogramasPorSegundo = 12f;
    [Tooltip("Audio que suena junto con la animación (voz para el lipsync)")]
    public AudioSource fuenteAudio;

    public readonly List<Clave> claves = new List<Clave>(); // de todas las capas, ordenadas por fotograma
    public int Fotograma { get; private set; }
    public bool Reproduciendo { get; private set; }
    public bool Activa => claves.Count > 0;
    public event System.Action alCambiar;

    float acumulado;

    int CapaActiva => dibujo != null ? dibujo.capaActual : 0;

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
            Trazo.cambiadosAnim.Clear();
            return;
        }

        // Clave automática: lo que edites queda guardado en el fotograma actual, en la capa de esas líneas.
        if (Trazo.huboCambio)
        {
            Trazo.huboCambio = false;
            var capas = new HashSet<int>();
            foreach (var t in Trazo.cambiadosAnim)
                if (t != null)
                    capas.Add(t.capa);
            if (capas.Count == 0)
                capas.Add(CapaActiva);
            bool alguna = false;
            foreach (int capa in capas)
            {
                if (!CapaAnimada(capa))
                    continue;
                GuardarClave(Fotograma, capa);
                alguna = true;
            }
            if (alguna)
                Avisar();
        }
        Trazo.cambiadosAnim.Clear();
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

    // La última clave (de cualquier capa). 0 si no hay.
    public int UltimaClave
    {
        get
        {
            int fin = 0;
            foreach (var c in claves)
                fin = Mathf.Max(fin, c.fotograma);
            return fin;
        }
    }

    public int UltimoFotograma()
    {
        int fin = UltimaClave;
        return fin > 0 ? fin : TotalFotogramas - 1;
    }

    public bool CapaAnimada(int capa)
    {
        foreach (var c in claves)
            if (c.capa == capa)
                return true;
        return false;
    }

    // Las claves de una capa (para las marcas de la línea de tiempo).
    public List<Clave> ClavesDe(int capa)
    {
        var l = new List<Clave>();
        foreach (var c in claves)
            if (c.capa == capa)
                l.Add(c);
        return l;
    }

    bool CapaTieneLineas(int capa)
    {
        foreach (var t in dibujo.trazos)
            if (t != null && t.capa == capa && t.nodos.Count >= 2)
                return true;
        return false;
    }

    // Lo llama el Dibujo justo antes de cualquier cambio.
    // Si editas fuera del fotograma 1, cada capa que aún no estaba animada guarda su dibujo original
    // como clave en el fotograma 1 (así el cambio de ahora se vuelve un movimiento).
    public void AntesDeEditar()
    {
        Pausar();
        if (Fotograma == 0)
            return;
        for (int capa = 0; capa < Dibujo.NumeroDeCapas; capa++)
            if (!CapaAnimada(capa) && CapaTieneLineas(capa))
                GuardarClave(0, capa);
    }

    public bool EsClave(int f)
    {
        return BuscarClave(f, CapaActiva) != null;
    }

    public bool EsClave(int f, int capa)
    {
        return BuscarClave(f, capa) != null;
    }

    Clave BuscarClave(int f, int capa)
    {
        foreach (var c in claves)
            if (c.fotograma == f && c.capa == capa)
                return c;
        return null;
    }

    // Guarda (o reemplaza) la clave del fotograma f de la capa activa.
    public void GuardarClaveEn(int f)
    {
        GuardarClaveEn(f, CapaActiva);
    }

    public void GuardarClaveEn(int f, int capa)
    {
        GuardarClave(Mathf.Clamp(f, 0, TotalFotogramas - 1), capa);
        Avisar();
    }

    void GuardarClave(int f, int capa)
    {
        var c = BuscarClave(f, capa);
        if (c == null)
        {
            c = new Clave { fotograma = f, capa = capa };
            int i = 0;
            while (i < claves.Count && claves[i].fotograma <= f)
                i++;
            claves.Insert(i, c);
        }
        // Las líneas que ya existían en alguna clave de esta capa (las demás son líneas NUEVAS).
        var conocidas = new HashSet<int>();
        foreach (var k in claves)
            if (k.capa == capa)
                foreach (var p in k.trazos)
                    if (p != null)
                        conocidas.Add(p.id);
        c.cache = null;
        c.trazos.Clear();
        foreach (var t in dibujo.trazos)
            if (t != null && t.capa == capa && t.visibleAnim && t.nodos.Count >= 2)
                c.trazos.Add(t.CrearDatos());
        // Una línea nueva existe en TODA la animación de su capa (con la misma forma en todas sus claves).
        foreach (var t in dibujo.trazos)
        {
            if (t == null || t.capa != capa || !t.visibleAnim || t.Dibujando || t.nodos.Count < 2 || conocidas.Contains(t.id))
                continue;
            foreach (var k in claves)
            {
                if (k == c || k.capa != capa)
                    continue;
                k.trazos.Add(t.CrearDatos());
                k.cache = null;
            }
        }
    }

    // Para deshacer: cada clave en texto. Las que no cambiaron reutilizan su texto (no se copian otra vez).
    public List<string> Instantanea()
    {
        var lista = new List<string>(claves.Count);
        foreach (var c in claves)
        {
            if (c.cache == null)
                c.cache = JsonUtility.ToJson(c);
            lista.Add(c.cache);
        }
        return lista;
    }

    // Vuelve a las claves de una foto de deshacer. Las claves que no cambiaron se quedan como están.
    public void RestaurarInstantanea(List<string> lista, int f)
    {
        var nuevas = new List<Clave>(lista.Count);
        foreach (var texto in lista)
        {
            Clave igual = null;
            foreach (var c in claves)
            {
                if (c.cache != null && ReferenceEquals(c.cache, texto))
                {
                    igual = c;
                    break;
                }
            }
            if (igual == null)
            {
                igual = JsonUtility.FromJson<Clave>(texto);
                if (igual == null)
                    continue;
                igual.cache = texto;
            }
            nuevas.Add(igual);
        }
        Restaurar(nuevas, f);
    }

    // ---------- Botones de la línea de tiempo (trabajan en la capa activa) ----------

    public void AgregarClave()
    {
        dibujo.GuardarParaDeshacer();
        GuardarClave(Fotograma, CapaActiva);
        Trazo.huboCambio = false;
        Trazo.cambiadosAnim.Clear();
        Avisar();
        dibujo.Mensaje("Clave en el fotograma " + (Fotograma + 1) + " (" + dibujo.CapaActual.nombre + ")");
    }

    public void QuitarClave()
    {
        var c = BuscarClave(Fotograma, CapaActiva);
        if (c == null)
        {
            dibujo.Mensaje("Aquí no hay clave de " + dibujo.CapaActual.nombre);
            return;
        }
        dibujo.GuardarParaDeshacer();
        claves.Remove(c);
        MostrarFotograma();
        Avisar();
        dibujo.Mensaje("Clave quitada (" + dibujo.CapaActual.nombre + ")");
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
        var c = BuscarClave(desde, CapaActiva);
        if (c == null || desde == hasta)
            return;
        dibujo.GuardarParaDeshacer();
        var otra = BuscarClave(hasta, CapaActiva);
        if (otra != null)
            claves.Remove(otra);
        c.fotograma = hasta;
        c.cache = null;
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
        bool viejas = false;
        if (lista != null)
            foreach (var c in lista)
                if (c != null && c.trazos != null)
                {
                    claves.Add(c);
                    if (c.capa < 0)
                        viejas = true;
                }
        if (viejas)
            SepararPorCapas();
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

    // Animaciones de antes (una clave para todo el dibujo): cada clave se reparte en sus capas.
    void SepararPorCapas()
    {
        var viejas = claves.FindAll(c => c.capa < 0);
        claves.RemoveAll(c => c.capa < 0);
        var capas = new HashSet<int>();
        foreach (var v in viejas)
            foreach (var p in v.trazos)
                if (p != null)
                    capas.Add(p.capa);
        foreach (var v in viejas)
            foreach (int capa in capas)
            {
                var k = new Clave { fotograma = v.fotograma, capa = capa };
                foreach (var p in v.trazos)
                    if (p != null && p.capa == capa)
                        k.trazos.Add(p);
                claves.Add(k);
            }
    }

    // Pone cada línea en la forma que le toca en el fotograma actual (cada capa con sus claves).
    public void MostrarFotograma()
    {
        if (claves.Count == 0)
            return;
        var anterior = new Dictionary<int, Clave>();
        var siguiente = new Dictionary<int, Clave>();
        var conocidas = new Dictionary<int, HashSet<int>>();
        foreach (var c in claves)
        {
            HashSet<int> ids;
            if (!conocidas.TryGetValue(c.capa, out ids))
                conocidas[c.capa] = ids = new HashSet<int>();
            foreach (var p in c.trazos)
                if (p != null)
                    ids.Add(p.id);
            if (c.fotograma <= Fotograma)
                anterior[c.capa] = c;
            else if (!siguiente.ContainsKey(c.capa))
                siguiente[c.capa] = c;
        }
        // Antes de la primera clave de una capa, la capa se ve como en esa primera clave.
        foreach (int capa in conocidas.Keys)
        {
            if (anterior.ContainsKey(capa))
                continue;
            anterior[capa] = siguiente[capa];
            siguiente.Remove(capa);
        }

        bool antes = Trazo.silenciar;
        Trazo.silenciar = true;
        foreach (var t in dibujo.trazos)
        {
            if (t == null)
                continue;
            HashSet<int> ids;
            if (!conocidas.TryGetValue(t.capa, out ids) || !ids.Contains(t.id))
            {
                t.visibleAnim = true; // su capa no está animada (o la línea no está en sus claves): se queda quieta
                continue;
            }
            Clave a = anterior[t.capa];
            Clave b;
            siguiente.TryGetValue(t.capa, out b);
            var pa = BuscarPose(a, t.id);
            if (pa == null)
            {
                t.visibleAnim = false;
                continue;
            }
            float u = b != null && b.fotograma > a.fotograma ? (Fotograma - a.fotograma) / (float)(b.fotograma - a.fotograma) : 0f;
            t.visibleAnim = true;
            t.AplicarPose(pa, b != null ? BuscarPose(b, t.id) : null, Mathf.Clamp01(u));
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
            c.cache = null;
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
            c.cache = null;
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

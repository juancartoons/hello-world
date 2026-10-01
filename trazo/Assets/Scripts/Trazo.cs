using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum EstiloLinea { Cinta = 0, Tubo = 1 }

// Una línea del dibujo. Mientras se dibuja usa los puntos "crudos" del dedo; al terminar,
// los simplifica a pocos nodos (los "vectores" editables) unidos por curvas Bézier con asas.
// Grosor con "valor de línea": grueso en el centro y en punta en los extremos.
// Si la línea se cierra (sus puntas se unen), puede tener relleno de color.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Trazo : MonoBehaviour
{
    // Colores del relleno (se cambian tocando el relleno con el índice derecho).
    public static readonly Color[] Paleta =
    {
        new Color(1f, 0.85f, 0.35f),   // amarillo
        new Color(0.55f, 0.78f, 1f),   // celeste
        new Color(1f, 0.6f, 0.7f),     // rosado
        new Color(0.55f, 0.9f, 0.7f),  // menta
        new Color(1f, 0.65f, 0.35f),   // naranja
        new Color(0.75f, 0.65f, 1f),   // lavanda
        new Color(0.85f, 0.85f, 0.85f),// gris claro
        new Color(0.12f, 0.12f, 0.14f) // negro
    };

    public List<Vector3> nodos = new List<Vector3>();       // en coordenadas locales del Dibujo
    public List<Vector3> asaEntrada = new List<Vector3>();  // asas Bézier (desplazamiento desde el nodo)
    public List<Vector3> asaSalida = new List<Vector3>();
    public List<bool> asaManual = new List<bool>();         // false: el asa se calcula sola (curva suave)
    public List<float> grosorNodo = new List<float>();      // grosor propio de cada nodo (1 = normal)
    public int id;
    public int capa;
    public bool visibleAnim = true;                         // false: no existe en este fotograma de la animación
    public bool cerrado;
    public bool relleno;
    public int colorRelleno;
    [Tooltip("Grosor máximo (en el centro), en unidades locales del Dibujo")]
    public float ancho = 0.008f;
    public EstiloLinea estilo = EstiloLinea.Cinta;

    // La curva ya calculada (local), para tocarla y medirla.
    public readonly List<Vector3> curva = new List<Vector3>();
    public bool PoligonoValido { get; private set; }

    // La animación aplica poses sin que cuenten como "cambios del usuario".
    public static bool silenciar;
    public static bool huboCambio;

    const float separacionCrudos = 0.003f;       // metros entre puntos al dibujar
    const float toleranciaSimplificar = 0.003f;  // cuánto puede alejarse la curva al simplificar
    const float pasoMuestras = 0.004f;           // detalle de la curva final
    const int ladosTubo = 8;
    const float puntaMinima = 0.06f;             // grosor de las puntas (fracción del centro)
    const int maxPuntosRelleno = 64;

    readonly List<Vector3> crudos = new List<Vector3>();
    Mesh malla;
    Mesh mallaRelleno;
    MeshRenderer rellenoRenderer;
    Material materialRelleno;

    Vector3 poliCentro, poliU, poliV, poliN;
    readonly List<Vector3> poli3D = new List<Vector3>();
    readonly List<Vector2> poli2D = new List<Vector2>();

    static readonly List<Vector3> muestras = new List<Vector3>();
    static readonly List<float> largos = new List<float>();
    static readonly List<float> multiplicadores = new List<float>();
    static readonly List<Vector3> vertices = new List<Vector3>();
    static readonly List<Vector3> normales = new List<Vector3>();
    static readonly List<Vector2> uvs = new List<Vector2>();
    static readonly List<int> indices = new List<int>();
    static readonly List<int> restantes = new List<int>();
    static readonly List<Color> colores = new List<Color>();

    public bool Dibujando => crudos.Count > 0;

    float Escala => Mathf.Max(0.0001f, transform.lossyScale.x);

    public float Largo
    {
        get
        {
            float total = 0f;
            for (int i = 1; i < curva.Count; i++)
                total += Vector3.Distance(curva[i - 1], curva[i]);
            return total;
        }
    }

    public void Configurar(Material material, Material rellenoMat, float anchoInicial, EstiloLinea estiloInicial)
    {
        ancho = anchoInicial;
        estilo = estiloInicial;
        materialRelleno = rellenoMat;
        var mr = GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        AsegurarMalla();
    }

    void AsegurarMalla()
    {
        if (malla != null)
            return;
        malla = new Mesh { name = "Trazo" };
        malla.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = malla;
    }

    void OnDestroy()
    {
        if (malla != null)
            Destroy(malla);
        if (mallaRelleno != null)
            Destroy(mallaRelleno);
    }

    // ---------- Dibujar ----------

    // Agrega un punto mientras se dibuja (en coordenadas locales del Dibujo).
    public void AgregarPuntoCrudo(Vector3 p)
    {
        float sep = separacionCrudos / Escala;
        if (crudos.Count > 0 && (p - crudos[crudos.Count - 1]).sqrMagnitude < sep * sep)
            return;
        crudos.Add(p);
        Reconstruir();
    }

    public float LargoCrudo => LargoDe(crudos);

    // Convierte el trazo crudo en nodos. Devuelve false si quedó demasiado corto.
    public bool Terminar()
    {
        if (crudos.Count < 2 || LargoDe(crudos) < 0.01f / Escala)
        {
            crudos.Clear();
            return false;
        }
        nodos.Clear();
        asaEntrada.Clear();
        asaSalida.Clear();
        asaManual.Clear();
        grosorNodo.Clear();
        cerrado = false;
        relleno = false;
        Simplificar(crudos, toleranciaSimplificar / Escala, nodos);
        crudos.Clear();
        Reconstruir();
        return nodos.Count >= 2;
    }

    // ---------- Editar ----------

    public void AsegurarAsas()
    {
        int n = nodos.Count;
        if (asaEntrada == null) asaEntrada = new List<Vector3>();
        if (asaSalida == null) asaSalida = new List<Vector3>();
        if (asaManual == null) asaManual = new List<bool>();
        if (grosorNodo == null) grosorNodo = new List<float>();
        Ajustar(grosorNodo, n, 1f);
        Ajustar(asaEntrada, n, Vector3.zero);
        Ajustar(asaSalida, n, Vector3.zero);
        Ajustar(asaManual, n, false);
        for (int i = 0; i < n; i++)
            if (!asaManual[i])
                AsaAutomatica(i);
    }

    static void Ajustar<T>(List<T> lista, int n, T valor)
    {
        while (lista.Count < n)
            lista.Add(valor);
        if (lista.Count > n)
            lista.RemoveRange(n, lista.Count - n);
    }

    // Asas automáticas: la curva pasa suave por el nodo, siguiendo a sus vecinos.
    void AsaAutomatica(int i)
    {
        int n = nodos.Count;
        Vector3 p = nodos[i];
        bool hayAnterior = i > 0 || cerrado;
        bool haySiguiente = i < n - 1 || cerrado;
        Vector3 anterior = hayAnterior ? nodos[(i - 1 + n) % n] : p;
        Vector3 siguiente = haySiguiente ? nodos[(i + 1) % n] : p;
        Vector3 dir = siguiente - anterior;
        if (dir.sqrMagnitude < 1e-12f)
        {
            asaEntrada[i] = Vector3.zero;
            asaSalida[i] = Vector3.zero;
            return;
        }
        dir.Normalize();
        asaEntrada[i] = -dir * (Vector3.Distance(p, anterior) / 3f);
        asaSalida[i] = dir * (Vector3.Distance(siguiente, p) / 3f);
    }

    // ¿Esta asa se usa? (las puntas de una línea abierta solo tienen un asa)
    public bool AsaUsada(int i, bool salida)
    {
        if (cerrado)
            return true;
        return salida ? i < nodos.Count - 1 : i > 0;
    }

    public void MoverNodo(int i, Vector3 posicionLocal)
    {
        if (i < 0 || i >= nodos.Count)
            return;
        nodos[i] = posicionLocal;
        Reconstruir();
    }

    // Mueve un asa; la otra asa del mismo nodo gira en espejo (nodo suave, como en Illustrator).
    public void MoverAsa(int i, bool salida, Vector3 desplazamiento)
    {
        AsegurarAsas();
        if (i < 0 || i >= nodos.Count)
            return;
        asaManual[i] = true;
        float largoOtra = (salida ? asaEntrada[i] : asaSalida[i]).magnitude;
        if (largoOtra < 1e-6f)
            largoOtra = desplazamiento.magnitude;
        Vector3 espejo = desplazamiento.sqrMagnitude > 1e-12f ? -desplazamiento.normalized * largoOtra : Vector3.zero;
        if (salida)
        {
            asaSalida[i] = desplazamiento;
            asaEntrada[i] = espejo;
        }
        else
        {
            asaEntrada[i] = desplazamiento;
            asaSalida[i] = espejo;
        }
        Reconstruir();
    }

    public void ReiniciarAsa(int i)
    {
        AsegurarAsas();
        if (i < 0 || i >= nodos.Count)
            return;
        asaManual[i] = false;
        Reconstruir();
    }

    public void QuitarNodo(int i)
    {
        AsegurarAsas();
        if (i < 0 || i >= nodos.Count)
            return;
        nodos.RemoveAt(i);
        asaEntrada.RemoveAt(i);
        asaSalida.RemoveAt(i);
        asaManual.RemoveAt(i);
        grosorNodo.RemoveAt(i);
        if (cerrado && nodos.Count < 3)
        {
            cerrado = false;
            relleno = false;
        }
        Reconstruir();
    }

    // Da la vuelta a la línea (el inicio pasa a ser el final).
    public void Invertir()
    {
        AsegurarAsas();
        nodos.Reverse();
        asaManual.Reverse();
        grosorNodo.Reverse();
        asaEntrada.Reverse();
        asaSalida.Reverse();
        var temporal = asaEntrada;
        asaEntrada = asaSalida;
        asaSalida = temporal;
    }

    // Une las dos puntas en un solo nodo y activa el relleno.
    public bool Cerrar(bool quitarUltimo)
    {
        AsegurarAsas();
        if (cerrado || nodos.Count < 4)
            return false;
        int q = quitarUltimo ? nodos.Count - 1 : 0;
        nodos.RemoveAt(q);
        asaEntrada.RemoveAt(q);
        asaSalida.RemoveAt(q);
        asaManual.RemoveAt(q);
        grosorNodo.RemoveAt(q);
        asaManual[0] = false;
        asaManual[nodos.Count - 1] = false;
        cerrado = true;
        relleno = true;
        Reconstruir();
        return true;
    }

    // Mueve toda la línea (los nodos parten de "baseNodos" y se suman "delta").
    public void Desplazar(List<Vector3> baseNodos, Vector3 delta)
    {
        int n = Mathf.Min(nodos.Count, baseNodos.Count);
        for (int i = 0; i < n; i++)
            nodos[i] = baseNodos[i] + delta;
        Reconstruir();
    }

    // Gira/escala/mueve solo esta línea, partiendo de su forma "origen" (m: de local a local).
    public void TransformarDesde(DatosTrazo origen, Matrix4x4 m, float escala)
    {
        if (origen == null || origen.nodos == null || origen.nodos.Count != nodos.Count)
            return;
        AsegurarAsas();
        int n = nodos.Count;
        bool asas = Completa(origen.asaEntrada, n) && Completa(origen.asaSalida, n);
        for (int i = 0; i < n; i++)
        {
            nodos[i] = m.MultiplyPoint3x4(origen.nodos[i]);
            if (asas)
            {
                asaEntrada[i] = m.MultiplyVector(origen.asaEntrada[i]);
                asaSalida[i] = m.MultiplyVector(origen.asaSalida[i]);
            }
        }
        ancho = Mathf.Clamp(origen.ancho * escala, 0.0005f, 0.5f);
        Reconstruir();
    }

    // Cambia el material de la línea (para mostrarla seleccionada).
    public void PonerMaterialLinea(Material material)
    {
        var mr = GetComponent<MeshRenderer>();
        if (material != null && mr.sharedMaterial != material)
            mr.sharedMaterial = material;
    }

    public void PonerGrosorNodo(int i, float multiplicador)
    {
        AsegurarAsas();
        if (i < 0 || i >= nodos.Count)
            return;
        grosorNodo[i] = Mathf.Clamp(multiplicador, 0.1f, 6f);
        Reconstruir(true);
    }

    public float GrosorDeNodo(int i)
    {
        AsegurarAsas();
        return i >= 0 && i < grosorNodo.Count ? grosorNodo[i] : 1f;
    }

    // ---------- Poses (para guardar y para la animación) ----------

    public DatosTrazo CrearDatos()
    {
        AsegurarAsas();
        return new DatosTrazo
        {
            id = id,
            capa = capa,
            nodos = new List<Vector3>(nodos),
            asaEntrada = new List<Vector3>(asaEntrada),
            asaSalida = new List<Vector3>(asaSalida),
            asaManual = new List<bool>(asaManual),
            grosorNodo = new List<float>(grosorNodo),
            cerrado = cerrado,
            relleno = relleno,
            colorRelleno = colorRelleno,
            ancho = ancho,
            estilo = (int)estilo
        };
    }

    static bool Completa<T>(List<T> lista, int n)
    {
        return lista != null && lista.Count == n;
    }

    // Pone la forma "a", o una mezcla entre "a" y "b" (u = 0..1) si tienen los mismos nodos (morph).
    public void AplicarPose(DatosTrazo a, DatosTrazo b, float u)
    {
        if (a == null || a.nodos == null || a.nodos.Count < 2)
            return;
        int n = a.nodos.Count;
        bool mezclar = b != null && u > 0f && Completa(b.nodos, n) && b.cerrado == a.cerrado;
        bool asasA = Completa(a.asaEntrada, n) && Completa(a.asaSalida, n) && Completa(a.asaManual, n);
        bool asasB = mezclar && Completa(b.asaEntrada, n) && Completa(b.asaSalida, n);
        bool grosA = Completa(a.grosorNodo, n);
        bool grosB = mezclar && Completa(b.grosorNodo, n);

        nodos.Clear();
        asaEntrada.Clear();
        asaSalida.Clear();
        asaManual.Clear();
        grosorNodo.Clear();
        for (int i = 0; i < n; i++)
        {
            nodos.Add(mezclar ? Vector3.Lerp(a.nodos[i], b.nodos[i], u) : a.nodos[i]);
            if (asasA)
            {
                asaEntrada.Add(asasB ? Vector3.Lerp(a.asaEntrada[i], b.asaEntrada[i], u) : a.asaEntrada[i]);
                asaSalida.Add(asasB ? Vector3.Lerp(a.asaSalida[i], b.asaSalida[i], u) : a.asaSalida[i]);
                asaManual.Add(a.asaManual[i]);
            }
            float g = grosA ? a.grosorNodo[i] : 1f;
            if (grosB)
                g = Mathf.Lerp(g, b.grosorNodo[i], u);
            grosorNodo.Add(g);
        }
        cerrado = a.cerrado && n >= 3;
        relleno = a.relleno && cerrado;
        colorRelleno = a.colorRelleno;
        ancho = mezclar ? Mathf.Lerp(a.ancho, b.ancho, u) : a.ancho;
        if (ancho <= 0f)
            ancho = 0.008f;
        Reconstruir();
    }

    // ---------- Tocar ----------

    public float DistanciaACurva(Vector3 local)
    {
        float mejor = float.MaxValue;
        for (int i = 1; i < curva.Count; i++)
            mejor = Mathf.Min(mejor, DistanciaASegmento(local, curva[i - 1], curva[i]));
        return mejor;
    }

    // ¿El punto está dentro de la figura cerrada (y cerca de su superficie)?
    public bool DentroDeRelleno(Vector3 local, float grosorLocal)
    {
        if (!PoligonoValido)
            return false;
        Vector3 d = local - poliCentro;
        if (Mathf.Abs(Vector3.Dot(d, poliN)) > grosorLocal)
            return false;
        var q = new Vector2(Vector3.Dot(d, poliU), Vector3.Dot(d, poliV));
        return PuntoEnPoligono(q, poli2D);
    }

    // ---------- Construir la malla ----------

    public void Reconstruir()
    {
        Reconstruir(false);
    }

    // soloLinea = true cuando solo cambia el grosor o el estilo (el relleno no cambia).
    public void Reconstruir(bool soloLinea)
    {
        AsegurarMalla();
        if (!silenciar)
            huboCambio = true;
        muestras.Clear();
        multiplicadores.Clear();
        if (crudos.Count > 0)
        {
            muestras.AddRange(crudos);
            for (int i = 0; i < crudos.Count; i++)
                multiplicadores.Add(1f);
        }
        else
        {
            AsegurarAsas();
            MuestrearBezier(muestras);
        }
        curva.Clear();
        curva.AddRange(muestras);

        if (!soloLinea)
            ConstruirRelleno();

        malla.Clear();
        if (muestras.Count < 2)
            return;

        largos.Clear();
        float total = 0f;
        largos.Add(0f);
        for (int i = 1; i < muestras.Count; i++)
        {
            total += Vector3.Distance(muestras[i - 1], muestras[i]);
            largos.Add(total);
        }
        if (total < 1e-5f)
            return;

        vertices.Clear();
        normales.Clear();
        uvs.Clear();
        indices.Clear();
        if (estilo == EstiloLinea.Tubo)
            ConstruirTubo(total);
        else
            ConstruirCinta(total);

        malla.indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        malla.SetVertices(vertices);
        malla.SetNormals(normales);
        malla.SetUVs(0, uvs);
        malla.SetTriangles(indices, 0);
        malla.RecalculateBounds();
        var caja = malla.bounds;
        caja.Expand(ancho * 2f);
        malla.bounds = caja;
    }

    bool CerradoAhora => cerrado && crudos.Count == 0;

    // Medio grosor en un punto del trazo: grueso en el centro, en punta en los extremos.
    float MedioGrosor(int i, float total)
    {
        float recorrido = largos[i];
        float m = i < multiplicadores.Count ? multiplicadores[i] : 1f;
        if (CerradoAhora)
            return ancho * 0.5f * 0.85f * m;
        float t = Mathf.Clamp01(recorrido / total);
        float seno = Mathf.Max(0f, Mathf.Sin(Mathf.PI * t));
        float perfil = Mathf.Max(puntaMinima, Mathf.Pow(seno, 0.55f));
        return ancho * 0.5f * perfil * m;
    }

    Vector3 Tangente(int i)
    {
        int n = muestras.Count;
        int a = i - 1;
        int b = i + 1;
        if (CerradoAhora && n > 2)
        {
            if (a < 0) a = n - 2;
            if (b > n - 1) b = 1;
        }
        a = Mathf.Clamp(a, 0, n - 1);
        b = Mathf.Clamp(b, 0, n - 1);
        Vector3 d = muestras[b] - muestras[a];
        return d.sqrMagnitude > 1e-12f ? d.normalized : Vector3.forward;
    }

    // Cinta: dos vértices por punto en el mismo lugar; el shader los abre mirando a la cámara.
    void ConstruirCinta(float total)
    {
        for (int i = 0; i < muestras.Count; i++)
        {
            Vector3 p = muestras[i];
            Vector3 t = Tangente(i);
            float h = MedioGrosor(i, total);
            vertices.Add(p); normales.Add(t); uvs.Add(new Vector2(-h, 0f));
            vertices.Add(p); normales.Add(t); uvs.Add(new Vector2(h, 0f));
            if (i > 0)
            {
                int a = (i - 1) * 2;
                indices.Add(a); indices.Add(a + 2); indices.Add(a + 1);
                indices.Add(a + 1); indices.Add(a + 2); indices.Add(a + 3);
            }
        }
    }

    // Tubo 3D real (anillos alrededor de la curva).
    void ConstruirTubo(float total)
    {
        Vector3 n = Vector3.zero;
        for (int i = 0; i < muestras.Count; i++)
        {
            Vector3 p = muestras[i];
            Vector3 t = Tangente(i);
            if (i == 0)
            {
                n = Perpendicular(t);
            }
            else
            {
                n = n - t * Vector3.Dot(n, t);
                if (n.sqrMagnitude < 1e-8f)
                    n = Perpendicular(t);
                n.Normalize();
            }
            Vector3 b = Vector3.Cross(t, n);
            float h = MedioGrosor(i, total);
            for (int k = 0; k < ladosTubo; k++)
            {
                float ang = k * Mathf.PI * 2f / ladosTubo;
                Vector3 dir = n * Mathf.Cos(ang) + b * Mathf.Sin(ang);
                vertices.Add(p + dir * h);
                normales.Add(dir);
                uvs.Add(new Vector2(0f, 1f));
            }
            if (i > 0)
            {
                int a = (i - 1) * ladosTubo;
                int c = i * ladosTubo;
                for (int k = 0; k < ladosTubo; k++)
                {
                    int k2 = (k + 1) % ladosTubo;
                    indices.Add(a + k); indices.Add(c + k); indices.Add(a + k2);
                    indices.Add(a + k2); indices.Add(c + k); indices.Add(c + k2);
                }
            }
        }
    }

    // ---------- Relleno ----------

    void ConstruirRelleno()
    {
        PoligonoValido = false;
        if (CerradoAhora && curva.Count >= 4)
            PrepararPoligono();

        bool mostrar = PoligonoValido && relleno;
        if (!mostrar)
        {
            if (rellenoRenderer != null && rellenoRenderer.gameObject.activeSelf)
                rellenoRenderer.gameObject.SetActive(false);
            return;
        }

        if (rellenoRenderer == null)
        {
            var go = new GameObject("Relleno");
            go.transform.SetParent(transform, false);
            mallaRelleno = new Mesh { name = "Relleno" };
            mallaRelleno.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mallaRelleno;
            rellenoRenderer = go.AddComponent<MeshRenderer>();
            rellenoRenderer.sharedMaterial = materialRelleno;
            rellenoRenderer.shadowCastingMode = ShadowCastingMode.Off;
            rellenoRenderer.receiveShadows = false;
        }
        if (!rellenoRenderer.gameObject.activeSelf)
            rellenoRenderer.gameObject.SetActive(true);

        vertices.Clear();
        indices.Clear();
        colores.Clear();
        vertices.AddRange(poli3D);
        Triangular(poli2D, indices, vertices);
        Color c = Paleta[Mathf.Abs(colorRelleno) % Paleta.Length];
        for (int i = 0; i < vertices.Count; i++)
            colores.Add(c);

        mallaRelleno.Clear();
        mallaRelleno.SetVertices(vertices);
        mallaRelleno.SetColors(colores);
        mallaRelleno.SetTriangles(indices, 0);
        mallaRelleno.RecalculateBounds();
    }

    // Toma la curva cerrada, la reduce a pocos puntos y calcula su plano promedio.
    void PrepararPoligono()
    {
        poli3D.Clear();
        poli2D.Clear();
        int total = curva.Count - 1; // el último punto repite el primero
        int paso = Mathf.Max(1, Mathf.CeilToInt(total / (float)maxPuntosRelleno));
        for (int i = 0; i < total; i += paso)
            poli3D.Add(curva[i]);
        int m = poli3D.Count;
        if (m < 3)
            return;

        Vector3 c = Vector3.zero;
        foreach (var p in poli3D)
            c += p;
        c /= m;
        Vector3 suma = Vector3.zero;
        for (int i = 0; i < m; i++)
            suma += Vector3.Cross(poli3D[i] - c, poli3D[(i + 1) % m] - c);
        if (suma.sqrMagnitude < 1e-14f)
            return;

        poliCentro = c;
        poliN = suma.normalized;
        poliU = Perpendicular(poliN);
        poliV = Vector3.Cross(poliN, poliU);
        foreach (var p in poli3D)
        {
            Vector3 d = p - c;
            poli2D.Add(new Vector2(Vector3.Dot(d, poliU), Vector3.Dot(d, poliV)));
        }
        PoligonoValido = true;
    }

    // Triangulación por "orejas". Si la figura se cruza a sí misma, termina en abanico desde el centro.
    static void Triangular(List<Vector2> p, List<int> tris, List<Vector3> verts3D)
    {
        int m = p.Count;
        float area = 0f;
        for (int i = 0; i < m; i++)
        {
            Vector2 a = p[i];
            Vector2 b = p[(i + 1) % m];
            area += a.x * b.y - b.x * a.y;
        }
        restantes.Clear();
        if (area >= 0f)
            for (int i = 0; i < m; i++) restantes.Add(i);
        else
            for (int i = m - 1; i >= 0; i--) restantes.Add(i);

        int vueltas = 0;
        while (restantes.Count > 3 && vueltas < m * 2)
        {
            vueltas++;
            bool corte = false;
            int cuenta = restantes.Count;
            for (int k = 0; k < cuenta; k++)
            {
                int i0 = restantes[(k - 1 + cuenta) % cuenta];
                int i1 = restantes[k];
                int i2 = restantes[(k + 1) % cuenta];
                Vector2 a = p[i0], b = p[i1], c = p[i2];
                if (Cruz(b - a, c - b) <= 1e-12f)
                    continue;
                bool contiene = false;
                for (int j = 0; j < cuenta; j++)
                {
                    int o = restantes[j];
                    if (o == i0 || o == i1 || o == i2)
                        continue;
                    if (PuntoEnTriangulo(p[o], a, b, c))
                    {
                        contiene = true;
                        break;
                    }
                }
                if (contiene)
                    continue;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                restantes.RemoveAt(k);
                corte = true;
                break;
            }
            if (!corte)
                break;
        }

        if (restantes.Count == 3)
        {
            tris.Add(restantes[0]); tris.Add(restantes[1]); tris.Add(restantes[2]);
        }
        else if (restantes.Count > 3)
        {
            Vector3 centro = Vector3.zero;
            foreach (int r in restantes)
                centro += verts3D[r];
            centro /= restantes.Count;
            int ic = verts3D.Count;
            verts3D.Add(centro);
            for (int k = 0; k < restantes.Count; k++)
            {
                tris.Add(ic);
                tris.Add(restantes[k]);
                tris.Add(restantes[(k + 1) % restantes.Count]);
            }
        }
    }

    static float Cruz(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    static bool PuntoEnTriangulo(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cruz(b - a, p - a);
        float d2 = Cruz(c - b, p - b);
        float d3 = Cruz(a - c, p - c);
        return d1 >= 0f && d2 >= 0f && d3 >= 0f;
    }

    static bool PuntoEnPoligono(Vector2 q, List<Vector2> poli)
    {
        bool dentro = false;
        int n = poli.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = poli[i];
            Vector2 b = poli[j];
            if ((a.y > q.y) != (b.y > q.y))
            {
                float x = (b.x - a.x) * (q.y - a.y) / (b.y - a.y) + a.x;
                if (q.x < x)
                    dentro = !dentro;
            }
        }
        return dentro;
    }

    // ---------- Curvas ----------

    void MuestrearBezier(List<Vector3> salida)
    {
        int n = nodos.Count;
        if (n == 0)
            return;
        if (n == 1)
        {
            salida.Add(nodos[0]);
            multiplicadores.Add(1f);
            return;
        }
        float paso = pasoMuestras / Escala;
        int segmentos = cerrado ? n : n - 1;
        for (int s = 0; s < segmentos; s++)
        {
            int a = s;
            int b = (s + 1) % n;
            Vector3 p0 = nodos[a];
            Vector3 p1 = p0 + asaSalida[a];
            Vector3 p3 = nodos[b];
            Vector3 p2 = p3 + asaEntrada[b];
            float largo = Vector3.Distance(p0, p1) + Vector3.Distance(p1, p2) + Vector3.Distance(p2, p3);
            int pasos = Mathf.Clamp(Mathf.CeilToInt(largo / paso), 1, 48);
            for (int k = 0; k < pasos; k++)
            {
                salida.Add(Bezier(p0, p1, p2, p3, k / (float)pasos));
                multiplicadores.Add(Mathf.Lerp(grosorNodo[a], grosorNodo[b], k / (float)pasos));
            }
        }
        salida.Add(cerrado ? nodos[0] : nodos[n - 1]);
        multiplicadores.Add(cerrado ? grosorNodo[0] : grosorNodo[n - 1]);
    }

    static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }

    static Vector3 Perpendicular(Vector3 t)
    {
        Vector3 eje = Mathf.Abs(t.y) < 0.9f ? Vector3.up : Vector3.right;
        return Vector3.Cross(t, eje).normalized;
    }

    static float LargoDe(List<Vector3> puntos)
    {
        float total = 0f;
        for (int i = 1; i < puntos.Count; i++)
            total += Vector3.Distance(puntos[i - 1], puntos[i]);
        return total;
    }

    // Ramer-Douglas-Peucker: deja solo los puntos necesarios para conservar la forma.
    static void Simplificar(List<Vector3> pts, float tolerancia, List<Vector3> salida)
    {
        int n = pts.Count;
        var conservar = new bool[n];
        conservar[0] = true;
        conservar[n - 1] = true;
        var pila = new Stack<Vector2Int>();
        pila.Push(new Vector2Int(0, n - 1));
        while (pila.Count > 0)
        {
            var tramo = pila.Pop();
            int a = tramo.x;
            int b = tramo.y;
            if (b <= a + 1)
                continue;
            float mayor = -1f;
            int indice = -1;
            for (int i = a + 1; i < b; i++)
            {
                float d = DistanciaASegmento(pts[i], pts[a], pts[b]);
                if (d > mayor)
                {
                    mayor = d;
                    indice = i;
                }
            }
            if (indice >= 0 && mayor > tolerancia)
            {
                conservar[indice] = true;
                pila.Push(new Vector2Int(a, indice));
                pila.Push(new Vector2Int(indice, b));
            }
        }
        for (int i = 0; i < n; i++)
            if (conservar[i])
                salida.Add(pts[i]);
    }

    static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float l2 = ab.sqrMagnitude;
        if (l2 < 1e-12f)
            return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
        return Vector3.Distance(p, a + ab * t);
    }
}

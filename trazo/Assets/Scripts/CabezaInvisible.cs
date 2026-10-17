using System.Collections.Generic;
using UnityEngine;

// LA CABEZA INVISIBLE (v51): dibujas la cabeza de tu personaje UNA vez (DE FRENTE o DE PERFIL) en una capa: el
// contorno (la línea más grande) y los rasgos (ojos, nariz, boca, orejas; sin cabello por ahora). La app arma con eso
// un HUEVO 3D invisible, pega cada rasgo en su superficie y te deja GIRARLA: de frente, 3/4 y perfil para los dos
// lados (y un poco arriba/abajo). Se redibuja en 2D con tus mismas líneas (grosor y color).
//  - Los botones Perfil / 3/4 / Frente la ponen en esa vista; o PELLIZCA con la derecha cerca de la cabeza y arrastra
//    (de lado = girar, arriba/abajo = inclinar).
//  - "Usar esta vista" cambia tus líneas por las de esa vista (se puede deshacer). Así se hacen las claves.
//  - Trampas del 2D: dibujada de PERFIL, el ojo (y la oreja) se copian al otro lado para la vista de frente; la nariz
//    y la boca van en la línea del centro (la nariz sobresale). Dibujada DE FRENTE, la nariz sobresale un poquito y las
//    orejas (fuera del contorno) quedan a los lados.
//  - Lo que queda atrás de la cabeza no se ve. El contorno se recalcula en cada vista con la forma de tu dibujo.
public class CabezaInvisible : MonoBehaviour
{
    static CabezaInvisible instancia;
    public static bool Activa => instancia != null && instancia.estado != Estado.Nada;

    enum Estado { Nada, Preguntando, Girando }

    // Una línea del modelo: sus puntos en 3D (respecto al centro de la cabeza, en el espacio de tu dibujo original).
    class Linea
    {
        public Trazo origen;
        public List<Vector3> pts = new List<Vector3>();
    }

    Estado estado;
    ControlManos control;
    Dibujo dibujo;
    readonly List<Trazo> originales = new List<Trazo>();
    readonly List<Renderer> apagados = new List<Renderer>();
    readonly List<Linea> lineas = new List<Linea>();
    Trazo contorno;
    Vector3 centro, derecha, arriba, haciaTi;     // la cabeza y la vista en que la dibujaste
    Vector2 centro2D;
    float semiX, semiY, semiZ;                    // el huevo (x a lo ancho de la vista original, y arriba, z hacia ti)
    readonly float[] proporcion = new float[72];  // la forma de tu contorno comparada con una elipse (por ángulo)
    bool perfil;
    float mira = 1f;                              // de perfil: hacia dónde mira la cara (+1 derecha, -1 izquierda)
    float giro, inclina;                          // grados
    bool agarrada;
    Vector3 agarreInicio;
    float giroInicio, inclinaInicio;

    // La vista previa.
    Transform raiz, panel;
    readonly List<LineRenderer> trazosVista = new List<LineRenderer>();
    readonly Dictionary<Color, Material> materiales = new Dictionary<Color, Material>();
    readonly List<GameObject> filaPregunta = new List<GameObject>(), filaGiro = new List<GameObject>();
    readonly List<List<Vector3>> corridas = new List<List<Vector3>>();
    readonly List<Linea> corridasDe = new List<Linea>();
    readonly List<bool> corridasEnteras = new List<bool>();
    bool sucio = true;

    // ---------- Entrar / salir ----------

    public static void Alternar()
    {
        if (Activa)
        {
            instancia.Cerrar();
            return;
        }
        if (instancia == null)
            instancia = new GameObject("CabezaInvisible").AddComponent<CabezaInvisible>();
        instancia.Empezar();
    }

    void Empezar()
    {
        control = ControlManos.Instancia;
        dibujo = control != null ? control.dibujo : FindFirstObjectByType<Dibujo>();
        if (control == null || dibujo == null || control.Cabeza == null)
            return;
        originales.Clear();
        foreach (var t in dibujo.trazos)
            if (Dibujo.Editable(t) && !t.oculto && t.capa == dibujo.capaActual && t.curva.Count >= 2)
                originales.Add(t);
        if (originales.Count < 2)
        {
            dibujo.Mensaje("Cabeza invisible: dibuja en esta capa el contorno de la cabeza y sus rasgos (ojos, nariz, boca, orejas)");
            return;
        }
        // La vista en que la dibujaste: desde tus ojos hacia el centro del dibujo.
        Vector3 suma = Vector3.zero;
        int n = 0;
        foreach (var t in originales)
            foreach (var p in t.curva)
            {
                suma += t.transform.TransformPoint(p);
                n++;
            }
        centro = suma / Mathf.Max(1, n);
        Transform cab = control.Cabeza;
        Vector3 adelante = (centro - cab.position).normalized;
        if (adelante.sqrMagnitude < 1e-6f)
            adelante = cab.forward;
        derecha = Vector3.Cross(Vector3.up, adelante).normalized;
        if (derecha.sqrMagnitude < 1e-6f)
            derecha = cab.right;
        arriba = Vector3.Cross(adelante, derecha).normalized;
        haciaTi = -adelante;

        // El contorno: la línea más grande (vista desde ti).
        float mayor = -1f;
        contorno = null;
        foreach (var t in originales)
        {
            Vector2 mn, mx;
            Caja(t, out mn, out mx);
            float area = (mx.x - mn.x) * (mx.y - mn.y);
            if (area > mayor)
            {
                mayor = area;
                contorno = t;
            }
        }
        Vector2 cmn, cmx;
        Caja(contorno, out cmn, out cmx);
        centro2D = (cmn + cmx) * 0.5f;
        semiX = Mathf.Max(0.01f, (cmx.x - cmn.x) * 0.5f);
        semiY = Mathf.Max(0.01f, (cmx.y - cmn.y) * 0.5f);
        ArmarPanel();
        Mostrar(filaPregunta, true);
        Mostrar(filaGiro, false);
        estado = Estado.Preguntando;
        dibujo.Mensaje("Cabeza invisible: ¿la dibujaste de frente o de perfil?");
    }

    void Cerrar()
    {
        foreach (var r in apagados)
            if (r != null)
                r.forceRenderingOff = false;
        apagados.Clear();
        estado = Estado.Nada;
        agarrada = false;
        if (raiz != null)
            raiz.gameObject.SetActive(false);
        if (control != null && control.Ocupado)
            control.Ocupado = false;
    }

    // ---------- Armar el modelo ----------

    void Elegir(bool dePerfil)
    {
        perfil = dePerfil;
        lineas.Clear();
        // De perfil: la cara mira hacia donde están los rasgos (la nariz sobresale de ese lado).
        if (perfil)
        {
            float sx = 0f;
            int cuantos = 0;
            foreach (var t in originales)
                if (t != contorno)
                    foreach (var p in t.curva)
                    {
                        sx += Plano(t.transform.TransformPoint(p)).x;
                        cuantos++;
                    }
            mira = cuantos > 0 && sx < 0f ? -1f : 1f;
            semiZ = semiX * 0.8f;   // el ancho de la cabeza (de oreja a oreja), un poco menos que de frente a nuca
        }
        else
        {
            semiZ = semiX * 1.1f;   // de la nariz a la nuca, un poco más que el ancho
        }
        ArmarProporcion();
        foreach (var t in originales)
        {
            if (t == contorno)
                continue;
            var plano = new List<Vector2>();
            foreach (var p in t.curva)
                plano.Add(Plano(t.transform.TransformPoint(p)));
            if (plano.Count < 2)
                continue;
            if (perfil)
                ArmarDePerfil(t, plano);
            else
                ArmarDeFrente(t, plano);
        }
        // Tus líneas se esconden mientras giras la cabeza (vuelven al cerrar).
        foreach (var t in originales)
            foreach (var r in t.GetComponentsInChildren<Renderer>())
                if (!r.forceRenderingOff)
                {
                    r.forceRenderingOff = true;
                    apagados.Add(r);
                }
        giro = 0f;
        inclina = 0f;
        Mostrar(filaPregunta, false);
        Mostrar(filaGiro, true);
        estado = Estado.Girando;
        sucio = true;
        dibujo.Mensaje("Gírala: Perfil / 3/4 / Frente, o pellízcala con la derecha y arrastra");
    }

    // En el plano de tu dibujo original (x a tu derecha, y arriba), respecto al centro de la cabeza.
    Vector2 Plano(Vector3 mundo)
    {
        Vector3 d = mundo - centro;
        return new Vector2(Vector3.Dot(d, derecha), Vector3.Dot(d, arriba)) - centro2D;
    }

    void Caja(Trazo t, out Vector2 mn, out Vector2 mx)
    {
        mn = new Vector2(float.MaxValue, float.MaxValue);
        mx = new Vector2(float.MinValue, float.MinValue);
        foreach (var p in t.curva)
        {
            Vector3 d = t.transform.TransformPoint(p) - centro;
            var q = new Vector2(Vector3.Dot(d, derecha), Vector3.Dot(d, arriba));
            mn = Vector2.Min(mn, q);
            mx = Vector2.Max(mx, q);
        }
    }

    static Vector2 Medio(List<Vector2> pts)
    {
        Vector2 c = Vector2.zero;
        foreach (var p in pts)
            c += p;
        return c / Mathf.Max(1, pts.Count);
    }

    float K(Vector2 q)
    {
        float u = q.x / semiX, v = q.y / semiY;
        return 1f - u * u - v * v;
    }

    // DE FRENTE: los rasgos se pegan al frente del huevo (la nariz sobresale un poquito). Las OREJAS (fuera del
    // contorno) son un disco a los lados, inclinado hacia atrás (de perfil se ven como orejas, no como una raya).
    void ArmarDeFrente(Trazo t, List<Vector2> plano)
    {
        Vector2 c = Medio(plano);
        int fuera = 0;
        foreach (var q in plano)
            if (K(q) <= 0f)
                fuera++;
        var l = new Linea { origen = t };
        if (fuera > plano.Count / 2)
        {
            float lado = c.x > 0f ? 1f : -1f;
            foreach (var q in plano)
            {
                float dx = q.x - c.x;
                l.pts.Add(new Vector3(c.x + dx * 0.71f, q.y, -0.1f * semiZ - dx * lado * 0.71f));
            }
        }
        else
        {
            foreach (var q in plano)
            {
                float k = K(q);
                float u = q.x / semiX, v = q.y / semiY;
                float z = k > 0f ? semiZ * Mathf.Sqrt(k) : -0.05f * semiZ;
                if (Mathf.Abs(u) < 0.22f && v > -0.45f && v < 0.15f)
                    z += 0.22f * semiZ * (1f - Mathf.Abs(u) / 0.22f);
                l.pts.Add(new Vector3(q.x, q.y, z));
            }
        }
        lineas.Add(l);
    }

    // DE PERFIL (x = de la nuca a la cara): lo del borde de la cara va en la línea del centro (la NARIZ sobresale; la
    // BOCA se abre a lo ancho); la OREJA es un disco al lado; el OJO y la ceja van en la cara, cerca del centro. Todo lo
    // que no es del centro se copia al otro lado (de frente se ven los dos ojos y las dos orejas).
    void ArmarDePerfil(Trazo t, List<Vector2> plano)
    {
        Vector2 c = Medio(plano);
        float uc = c.x / semiX, vc = c.y / semiY;
        int borde = 0;
        foreach (var q in plano)
            if (K(q) <= 0f || q.x / semiX * mira > 0.7f)
                borde++;
        var l = new Linea { origen = t };
        if (borde > plano.Count / 2)
        {
            if (vc < -0.2f)
            {
                // La boca: de frente se abre a lo ancho (y sus puntas van un poquito hacia atrás).
                for (int i = 0; i < plano.Count; i++)
                {
                    float u = i / (float)Mathf.Max(1, plano.Count - 1);
                    float z = (u - 0.5f) * 0.7f * semiZ;
                    l.pts.Add(new Vector3(plano[i].x - Mathf.Abs(z) * 0.3f * mira, plano[i].y, z));
                }
            }
            else
            {
                foreach (var q in plano)
                    l.pts.Add(new Vector3(q.x, q.y, 0f));
            }
            lineas.Add(l);
            return;
        }
        if (Mathf.Abs(uc) < 0.35f && Mathf.Abs(vc) < 0.4f)
        {
            // La oreja: un disco al lado de la cabeza.
            foreach (var q in plano)
            {
                float dx = q.x - c.x;
                l.pts.Add(new Vector3(c.x + dx * 0.71f, q.y, semiZ * 0.9f - dx * mira * 0.71f));
            }
        }
        else
        {
            // Ojo, ceja: en la cara (más cerca del centro mientras más adelante estén), un poquito abiertos.
            float x0 = float.MaxValue, x1 = float.MinValue;
            foreach (var q in plano)
            {
                x0 = Mathf.Min(x0, q.x);
                x1 = Mathf.Max(x1, q.x);
            }
            foreach (var q in plano)
            {
                float u = q.x / semiX, v = q.y / semiY;
                float z = semiZ * Mathf.Clamp(1f - u * mira, 0.2f, 1f) * Mathf.Sqrt(Mathf.Max(1f - v * v, 0.05f));
                z += (q.x - (x0 + x1) * 0.5f) * mira * 0.6f;
                l.pts.Add(new Vector3(q.x, q.y, z));
            }
        }
        lineas.Add(l);
        var copia = new Linea { origen = t };
        foreach (var p in l.pts)
            copia.pts.Add(new Vector3(p.x, p.y, -p.z));
        lineas.Add(copia);
    }

    // La forma de tu contorno (por ángulo) comparada con la elipse del huevo: así el contorno girado conserva tu dibujo
    // (la barbilla, los cachetes...).
    void ArmarProporcion()
    {
        var suma = new float[proporcion.Length];
        var cuenta = new int[proporcion.Length];
        foreach (var p in contorno.curva)
        {
            Vector2 q = Plano(contorno.transform.TransformPoint(p));
            float r = q.magnitude;
            if (r < 1e-5f)
                continue;
            float a = Mathf.Atan2(q.y, q.x);
            int i = Mathf.Clamp(Mathf.FloorToInt((a + Mathf.PI) / (2f * Mathf.PI) * proporcion.Length), 0, proporcion.Length - 1);
            suma[i] += r / RadioElipse(a, semiX, semiY);
            cuenta[i]++;
        }
        for (int i = 0; i < proporcion.Length; i++)
            proporcion[i] = cuenta[i] > 0 ? suma[i] / cuenta[i] : -1f;
        // Los huecos: del vecino más cercano.
        for (int i = 0; i < proporcion.Length; i++)
        {
            if (proporcion[i] > 0f)
                continue;
            float v = 1f;
            for (int d = 1; d < proporcion.Length; d++)
            {
                float a = suma[(i + d) % proporcion.Length], b = suma[(i - d + proporcion.Length) % proporcion.Length];
                int ca = cuenta[(i + d) % proporcion.Length], cb = cuenta[(i - d + proporcion.Length) % proporcion.Length];
                if (ca > 0)
                {
                    v = a / ca;
                    break;
                }
                if (cb > 0)
                {
                    v = b / cb;
                    break;
                }
            }
            proporcion[i] = v;
        }
    }

    static float RadioElipse(float angulo, float a, float b)
    {
        float c = Mathf.Cos(angulo) / a, s = Mathf.Sin(angulo) / b;
        return 1f / Mathf.Sqrt(Mathf.Max(1e-8f, c * c + s * s));
    }

    float Proporcion(float angulo)
    {
        float f = (angulo + Mathf.PI) / (2f * Mathf.PI) * proporcion.Length;
        int i0 = ((Mathf.FloorToInt(f) % proporcion.Length) + proporcion.Length) % proporcion.Length;
        int i1 = (i0 + 1) % proporcion.Length;
        return Mathf.Lerp(proporcion[i0], proporcion[i1], f - Mathf.Floor(f));
    }

    // ---------- Cada cuadro ----------

    void LateUpdate()
    {
        if (estado == Estado.Nada || control == null || dibujo == null)
            return;
        if (estado == Estado.Girando)
        {
            RevisarAgarre();
            if (sucio)
                Dibujar();
        }
    }

    void RevisarAgarre()
    {
        var der = control.Der;
        if (agarrada)
        {
            if (!der.valida || !der.pellizco)
            {
                agarrada = false;
                control.Ocupado = false;
                return;
            }
            Vector3 d = der.PuntoPellizco - agarreInicio;
            giro = giroInicio - Vector3.Dot(d, derecha) * 300f;
            inclina = Mathf.Clamp(inclinaInicio + Vector3.Dot(d, arriba) * 200f, -40f, 40f);
            sucio = true;
            return;
        }
        if (der.valida && der.empezoPellizco && !BotonTocable.DedoCerca
            && Vector3.Distance(der.PuntoPellizco, centro) < Mathf.Max(semiX, semiY) + 0.12f)
        {
            agarrada = true;
            agarreInicio = der.PuntoPellizco;
            giroInicio = giro;
            inclinaInicio = inclina;
            control.Ocupado = true;
        }
    }

    // El giro de la cabeza (en el espacio de tu dibujo original). De perfil, "Frente" = la cara hacia ti.
    Quaternion Giro => Quaternion.Euler(inclina, giro, 0f);

    void Dibujar()
    {
        sucio = false;
        Quaternion q = Giro;
        corridas.Clear();
        corridasDe.Clear();
        corridasEnteras.Clear();
        // El contorno: la silueta del huevo girado, con la forma de tu dibujo.
        var sil = new List<Vector3>();
        Matrix4x4 m = Matrix4x4.Rotate(q) * Matrix4x4.Scale(new Vector3(semiX, semiY, semiZ));
        Vector3 w = m.transpose.MultiplyVector(Vector3.forward).normalized;
        Vector3 e1 = Vector3.Cross(w, Mathf.Abs(w.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 e2 = Vector3.Cross(w, e1).normalized;
        const int lados = 72;
        for (int i = 0; i <= lados; i++)
        {
            float a = i / (float)lados * Mathf.PI * 2f;
            Vector3 p = m.MultiplyVector(Mathf.Cos(a) * e1 + Mathf.Sin(a) * e2);
            Vector2 p2 = new Vector2(p.x, p.y);
            float ang = Mathf.Atan2(p2.y, p2.x);
            p2 *= Proporcion(ang);
            sil.Add(new Vector3(p2.x, p2.y, 0f));
        }
        corridas.Add(sil);
        corridasDe.Add(null);
        corridasEnteras.Add(true);
        // Los rasgos: girados; lo que queda atrás de la cabeza no se ve.
        foreach (var l in lineas)
        {
            List<Vector3> actual = null;
            bool entera = true;
            foreach (var p in l.pts)
            {
                Vector3 pr = q * p;
                Vector3 nrm = q * new Vector3(p.x / (semiX * semiX), p.y / (semiY * semiY), p.z / (semiZ * semiZ));
                bool ver = nrm.sqrMagnitude < 1e-12f || nrm.normalized.z > -0.12f;
                if (ver)
                {
                    if (actual == null)
                    {
                        actual = new List<Vector3>();
                        corridas.Add(actual);
                        corridasDe.Add(l);
                        corridasEnteras.Add(false);
                    }
                    actual.Add(new Vector3(pr.x, pr.y, 0f));
                }
                else
                {
                    entera = false;
                    actual = null;
                }
            }
            if (entera && actual != null && corridasDe[corridasDe.Count - 1] == l)
                corridasEnteras[corridasEnteras.Count - 1] = true;
        }
        // A las líneas de la vista previa.
        int k = 0;
        for (int c = 0; c < corridas.Count; c++)
        {
            var pts = corridas[c];
            if (pts.Count < 2)
                continue;
            Trazo origen = corridasDe[c] != null ? corridasDe[c].origen : contorno;
            var lr = TrazoVista(k++);
            lr.positionCount = pts.Count;
            for (int i = 0; i < pts.Count; i++)
                lr.SetPosition(i, Mundo(pts[i]));
            float ancho = origen != null ? origen.ancho * dibujo.EscalaMundo : 0.004f;
            lr.widthMultiplier = Mathf.Max(0.0015f, ancho);
            lr.sharedMaterial = MaterialColor(origen != null ? origen.color : Color.black);
        }
        for (int i = k; i < trazosVista.Count; i++)
            trazosVista[i].positionCount = 0;
    }

    Vector3 Mundo(Vector3 p)
    {
        Vector2 q = new Vector2(p.x, p.y) + centro2D;
        return centro + derecha * q.x + arriba * q.y + haciaTi * 0.001f;
    }

    LineRenderer TrazoVista(int i)
    {
        while (trazosVista.Count <= i)
        {
            var go = new GameObject("CabezaLinea");
            go.transform.SetParent(raiz, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 3;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            trazosVista.Add(lr);
        }
        return trazosVista[i];
    }

    Material MaterialColor(Color c)
    {
        Material m;
        if (materiales.TryGetValue(c, out m) && m != null)
            return m;
        m = control.materialNodo != null ? new Material(control.materialNodo) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
        materiales[c] = m;
        return m;
    }

    // ---------- Usar esta vista: tus líneas pasan a ser las de esta vista ----------

    void Usar()
    {
        if (estado != Estado.Girando)
            return;
        Dibujar();
        dibujo.GuardarParaDeshacer();
        foreach (var r in apagados)
            if (r != null)
                r.forceRenderingOff = false;
        apagados.Clear();
        var raizDibujo = dibujo.transform;
        for (int c = 0; c < corridas.Count; c++)
        {
            var pts = corridas[c];
            if (pts.Count < 2)
                continue;
            Trazo origen = corridasDe[c] != null ? corridasDe[c].origen : contorno;
            var d = new DatosTrazo();
            // Como mucho unos 40 nodos por línea (se ve igual y se edita mejor).
            int paso = Mathf.Max(1, pts.Count / 40);
            for (int i = 0; i < pts.Count; i += paso)
                d.nodos.Add(raizDibujo.InverseTransformPoint(Mundo(pts[i])));
            if ((pts.Count - 1) % paso != 0)
                d.nodos.Add(raizDibujo.InverseTransformPoint(Mundo(pts[pts.Count - 1])));
            bool cerrada = c == 0 || (corridasEnteras[c] && origen != null && origen.cerrado);
            if (cerrada && d.nodos.Count > 2 && Vector3.Distance(d.nodos[0], d.nodos[d.nodos.Count - 1]) < 1e-4f)
                d.nodos.RemoveAt(d.nodos.Count - 1);
            if (origen != null)
            {
                d.ancho = origen.ancho;
                d.color = origen.color;
                d.cerrado = cerrada && d.nodos.Count >= 3;
                d.relleno = d.cerrado && origen.relleno;
                d.colorFondo = origen.colorFondo;
                d.colorRelleno = origen.colorRelleno;
                d.texturaRelleno = origen.texturaRelleno;
            }
            dibujo.AgregarTrazo(d);
        }
        foreach (var t in originales)
            if (t != null)
                dibujo.EliminarDelTodo(t);
        Trazo.huboCambio = true;
        dibujo.Mensaje("Cabeza: vista guardada (deshacer la devuelve)");
        Cerrar();
    }

    // ---------- El panel: debajo de la cabeza ----------

    void ArmarPanel()
    {
        if (raiz == null)
        {
            raiz = new GameObject("VistaCabeza").transform;
            var nav = FindFirstObjectByType<NavegadorArchivos>(FindObjectsInactive.Include);
            Material normal = nav != null ? nav.materialBoton : null, marcado = nav != null ? nav.materialBotonMarcado : null;
            panel = new GameObject("PanelCabeza").transform;
            panel.SetParent(raiz, false);
            Boton(filaPregunta, "De frente", new Vector3(-0.07f, 0f, 0f), 0.065f, normal, marcado, false, () => Elegir(false));
            Boton(filaPregunta, "De perfil", new Vector3(0f, 0f, 0f), 0.065f, normal, marcado, false, () => Elegir(true));
            Boton(filaPregunta, "Cerrar", new Vector3(0.07f, 0f, 0f), 0.065f, normal, marcado, false, Cerrar);
            string[] vistas = { "Perfil izq", "3/4 izq", "Frente", "3/4 der", "Perfil der" };
            float[] angulos = { -90f, -45f, 0f, 45f, 90f };
            for (int i = 0; i < vistas.Length; i++)
            {
                float a = angulos[i];
                Boton(filaGiro, vistas[i], new Vector3(-0.124f + i * 0.062f, 0.02f, 0f), 0.058f, normal, marcado, false, () => Vista(a));
            }
            Boton(filaGiro, "Usar esta vista", new Vector3(-0.04f, -0.016f, 0f), 0.1f, normal, marcado, true, Usar);
            Boton(filaGiro, "Cerrar", new Vector3(0.06f, -0.016f, 0f), 0.06f, normal, marcado, false, Cerrar);
        }
        raiz.gameObject.SetActive(true);
        // Debajo de la cabeza, mirándote.
        Vector3 pos = centro + arriba * (centro2D.y - semiY - 0.06f) + derecha * centro2D.x + haciaTi * 0.02f;
        panel.SetPositionAndRotation(pos, Quaternion.LookRotation(-haciaTi, arriba));
    }

    // Un botón (y su texto) que se agrega a "fila" (para mostrar u ocultar la fila entera).
    void Boton(List<GameObject> fila, string texto, Vector3 pos, float ancho, Material normal, Material marcado, bool oscuro,
               UnityEngine.Events.UnityAction accion)
    {
        var b = PanelTutoriales.Boton(panel, Idioma.T(texto), pos, new Vector2(ancho, 0.028f), oscuro ? marcado : normal, oscuro ? normal : marcado);
        b.alTocar.AddListener(accion);
        if (oscuro)
        {
            b.etiqueta.color = Color.white;
            b.textoNormal = Color.white;
        }
        fila.Add(b.gameObject);
        fila.Add(b.etiqueta.gameObject);
    }

    static void Mostrar(List<GameObject> fila, bool ver)
    {
        foreach (var g in fila)
            if (g != null)
                g.SetActive(ver);
    }

    // Una vista: el ángulo es desde "de frente" (de perfil, de frente = la cara hacia ti).
    void Vista(float angulo)
    {
        float frente = perfil ? (mira > 0f ? -90f : 90f) : 0f;
        giro = frente + angulo;
        inclina = 0f;
        sucio = true;
    }

    void OnDestroy()
    {
        foreach (var m in materiales.Values)
            if (m != null)
                Destroy(m);
        if (instancia == this)
            instancia = null;
    }
}

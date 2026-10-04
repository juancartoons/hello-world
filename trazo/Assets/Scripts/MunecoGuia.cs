using UnityEngine;

// Un muñeco de palitos "de mentira" (solo se ve; no es parte del dibujo) que camina, corre, salta o se queda quieto.
// Lo usan el título y el tutorial. Tiene las mismas medidas que el Palito de los títeres (escala 1).
// Cada palito puede tener varias HEBRAS finas que tiemblan por separado (líneas vivas, como dibujo animado).
public sealed class MunecoGuia
{
    const float Muslo = 0.085f, Canilla = 0.085f, Pie = 0.035f;
    const float Torso = 0.14f, BrazoSup = 0.06f, Antebrazo = 0.055f;
    const int PuntosCabeza = 14;
    const int Partes = 6; // 0 pierna 1, 1 pierna 2, 2 brazo 1, 3 brazo 2, 4 cuerpo, 5 cabeza

    readonly GameObject go;
    readonly LineRenderer[,] lineas;
    readonly int hebras;
    readonly Vector3[] tmp4 = new Vector3[4];
    readonly Vector3[] tmp3 = new Vector3[3];
    readonly Vector3[] cabeza = new Vector3[PuntosCabeza];
    readonly Vector3[] copia4 = new Vector3[4];
    readonly Vector3[] copia3 = new Vector3[3];
    readonly Vector3[] copiaCabeza = new Vector3[PuntosCabeza];
    readonly float ancho;

    public float escala = 1f;   // 1 = tamaño del Palito (unos 40 cm de alto)
    public float fase;          // dónde va el ciclo de pasos (radianes)
    public float temblor;       // líneas vivas: cuánto tiemblan los palitos (metros, a escala 1). 0 = nada
    public float panico;        // 0 a 1: los brazos arriba, agitándose (huyendo del borrador)
    public float agachar;       // 0 a 1: dobla las rodillas (antes de saltar, al caer o con la mano abajo)
    int variante;               // cuál de los 3 "dibujos" del temblor (cambia 8 veces por segundo)

    // Se llama cada vez que un pie toca el suelo (para el sonido de los pasos).
    public System.Action alPisar;

    // hebras: 1 = una línea normal; 3 = tres hebras finas por palito.
    public MunecoGuia(Transform padre, Material material, float anchoLinea, int hebras = 1)
    {
        ancho = anchoLinea;
        this.hebras = Mathf.Max(1, hebras);
        lineas = new LineRenderer[Partes, this.hebras];
        go = new GameObject("MunecoGuia");
        go.transform.SetParent(padre, false);
        for (int i = 0; i < Partes; i++)
        {
            for (int h = 0; h < this.hebras; h++)
            {
                var hijo = new GameObject((i == 5 ? "Cabeza" : "Palo" + i) + "_" + h);
                hijo.transform.SetParent(go.transform, false);
                var l = hijo.AddComponent<LineRenderer>();
                l.useWorldSpace = true;
                l.positionCount = i < 2 ? 4 : i == 5 ? PuntosCabeza : 3;
                l.loop = i == 5;
                l.numCapVertices = 4;
                l.numCornerVertices = 3;
                l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                l.receiveShadows = false;
                if (material != null)
                    l.sharedMaterial = material;
                lineas[i, h] = l;
            }
        }
    }

    public bool Visible
    {
        get { return go != null && go.activeSelf; }
        set { if (go != null && go.activeSelf != value) go.SetActive(value); }
    }

    // Avanza el ciclo de pasos según lo que caminó (metros). Más largo el paso al correr.
    public void Avanzar(float distancia, float movimiento)
    {
        float correr = Mathf.Clamp01(movimiento - 1f);
        float zancada = Mathf.Lerp(0.2f, 0.32f, correr) * escala;
        float antes = fase;
        fase += Mathf.Abs(distancia) / Mathf.Max(1e-4f, zancada) * Mathf.PI * 2f;
        // Cada media vuelta del ciclo, un pie toca el suelo.
        if (Mathf.Floor(antes / Mathf.PI) != Mathf.Floor(fase / Mathf.PI))
            alPisar?.Invoke();
        if (fase > 1000f)
            fase -= Mathf.PI * 200f;
    }

    // suelo = el punto del piso bajo la cadera · adelante = hacia dónde mira · arriba = el "arriba" del muñeco.
    // movimiento: 0 quieto, 1 caminar, 2 correr · tamano: 0 a 1 (para aparecer y desaparecer).
    public void Poner(Vector3 suelo, Vector3 adelante, Vector3 arriba, float movimiento, float tamano)
    {
        if (go == null)
            return;
        bool ver = tamano > 0.01f;
        Visible = ver;
        if (!ver)
            return;
        Vector3 up = arriba.sqrMagnitude > 1e-8f ? arriba.normalized : Vector3.up;
        Vector3 fw = adelante - up * Vector3.Dot(adelante, up);
        fw = fw.sqrMagnitude > 1e-8f ? fw.normalized : Vector3.Cross(up, Vector3.forward).normalized;
        float s = escala * tamano;
        variante = Mathf.FloorToInt(Time.time * 8f) % 3;

        float andar = Mathf.Clamp01(movimiento);
        float correr = Mathf.Clamp01(movimiento - 1f);
        float amplitud = andar * Mathf.Lerp(0.05f, 0.085f, correr);
        float alzar = andar * Mathf.Lerp(0.025f, 0.06f, correr);
        float inclinar = andar * Mathf.Lerp(0.06f, 0.26f, correr);
        float rebote = andar * Mathf.Lerp(0.005f, 0.012f, correr) * Mathf.Cos(fase * 2f);
        float respirar = (1f - andar) * 0.002f * Mathf.Sin(Time.time * 2.4f);
        float altoCadera = (Muslo + Canilla) * Mathf.Lerp(1f, Mathf.Lerp(0.96f, 0.9f, correr), andar) - 0.002f + rebote + respirar;
        altoCadera *= 1f - 0.35f * Mathf.Clamp01(agachar);
        Vector2 cadera = new Vector2(0f, altoCadera);

        // Piernas (con "IK" de dos huesos: la rodilla siempre hacia adelante).
        for (int i = 0; i < 2; i++)
        {
            float f = fase + i * Mathf.PI;
            float apoyo = (1f - andar) * (i == 0 ? 0.012f : -0.008f); // quieto: pies un poquito separados
            Vector2 tobillo = new Vector2(amplitud * Mathf.Cos(f) + apoyo, Mathf.Max(0f, -Mathf.Sin(f)) * alzar);
            Vector2 d = tobillo - cadera;
            float largo = Mathf.Clamp(d.magnitude, 0.01f, Muslo + Canilla - 0.0005f);
            Vector2 dir = d.magnitude > 1e-5f ? d / d.magnitude : Vector2.down;
            tobillo = cadera + dir * largo;
            float angulo = Mathf.Acos(Mathf.Clamp(largo / (Muslo + Canilla), -1f, 1f));
            Vector2 rodilla = cadera + Girar(dir, angulo) * Muslo;
            Vector2 punta = tobillo + new Vector2(Pie, 0f);
            tmp4[0] = Mundo(suelo, fw, up, cadera, s);
            tmp4[1] = Mundo(suelo, fw, up, rodilla, s);
            tmp4[2] = Mundo(suelo, fw, up, tobillo, s);
            tmp4[3] = Mundo(suelo, fw, up, punta, s);
            PonerParte(i, tmp4, copia4, fw, up, s);
        }

        // Cuerpo inclinado hacia adelante (más al correr).
        Vector2 cuello = cadera + Girar(new Vector2(0.006f, Torso), -inclinar);
        Vector2 medio = cadera + Girar(new Vector2(0.012f, Torso * 0.5f), -inclinar);
        tmp3[0] = Mundo(suelo, fw, up, cadera, s);
        tmp3[1] = Mundo(suelo, fw, up, medio, s);
        tmp3[2] = Mundo(suelo, fw, up, cuello, s);
        PonerParte(4, tmp3, copia3, fw, up, s);

        // Brazos: se balancean al revés que las piernas; el codo se dobla más al correr.
        // Con pánico: los brazos arriba, agitándose.
        Vector2 hombro = cadera + Girar(new Vector2(0.006f, 0.125f), -inclinar);
        float balanceo = andar * Mathf.Lerp(0.5f, 0.95f, correr);
        float codo = Mathf.Lerp(0.15f, Mathf.Lerp(0.55f, 1.5f, correr), andar);
        float miedo = Mathf.Clamp01(panico);
        for (int j = 0; j < 2; j++)
        {
            float f = fase + j * Mathf.PI + Mathf.PI;
            float th = -balanceo * Mathf.Cos(f) + (1f - andar) * (j == 0 ? 0.08f : -0.05f);
            float thPanico = Mathf.PI * 0.82f + Mathf.Sin(Time.time * 19f + j * 2.1f) * 0.35f + (j == 0 ? 0.15f : -0.15f);
            th = Mathf.Lerp(th, thPanico, miedo);
            float codoAhora = Mathf.Lerp(codo, 0.35f + Mathf.Sin(Time.time * 23f + j) * 0.25f, miedo);
            Vector2 arribaBrazo = new Vector2(Mathf.Sin(th), -Mathf.Cos(th));
            Vector2 abajoBrazo = new Vector2(Mathf.Sin(th + codoAhora), -Mathf.Cos(th + codoAhora));
            Vector2 c = hombro + arribaBrazo * BrazoSup;
            Vector2 m = c + abajoBrazo * Antebrazo;
            tmp3[0] = Mundo(suelo, fw, up, hombro, s);
            tmp3[1] = Mundo(suelo, fw, up, c, s);
            tmp3[2] = Mundo(suelo, fw, up, m, s);
            PonerParte(2 + j, tmp3, copia3, fw, up, s);
        }

        // Cabeza (un óvalo).
        Vector2 centro = cadera + Girar(new Vector2(0.012f, 0.185f), -inclinar);
        for (int k = 0; k < PuntosCabeza; k++)
        {
            float a = k * Mathf.PI * 2f / PuntosCabeza;
            cabeza[k] = Mundo(suelo, fw, up, centro + new Vector2(Mathf.Cos(a) * 0.035f, Mathf.Sin(a) * 0.038f), s);
        }
        PonerParte(5, cabeza, copiaCabeza, fw, up, s);

        float grosor = ancho * Mathf.Max(0.3f, tamano) * Mathf.Max(0.35f, escala);
        for (int i = 0; i < Partes; i++)
            for (int h = 0; h < hebras; h++)
                lineas[i, h].widthMultiplier = grosor;
    }

    // Cada hebra del palito es una copia que tiembla distinto (si hay una sola, tiembla sola).
    void PonerParte(int parte, Vector3[] puntos, Vector3[] copia, Vector3 adelante, Vector3 arriba, float s)
    {
        for (int h = 0; h < hebras; h++)
        {
            System.Array.Copy(puntos, copia, puntos.Length);
            Temblar(copia, parte, h, adelante, arriba, s);
            lineas[parte, h].SetPositions(copia);
        }
    }

    // Mueve un poquito cada punto (siempre igual para cada uno de los 3 "dibujos"): parece dibujado a mano.
    void Temblar(Vector3[] puntos, int linea, int hebra, Vector3 adelante, Vector3 arriba, float s)
    {
        float a = temblor * s;
        if (a <= 0f && hebras == 1)
            return;
        // Las hebras se separan un poquito entre sí (aunque no haya temblor).
        float separar = hebras > 1 ? 0.0035f * s : 0f;
        for (int k = 0; k < puntos.Length; k++)
        {
            float semilla = linea * 7.13f + k * 3.71f + variante * 11.3f + hebra * 17.9f;
            float amp = a + separar;
            puntos[k] += adelante * (Mathf.Sin(semilla) * amp) + arriba * (Mathf.Sin(semilla * 1.7f + 2.1f) * amp);
        }
    }

    static Vector2 Girar(Vector2 v, float angulo)
    {
        float c = Mathf.Cos(angulo), s = Mathf.Sin(angulo);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    static Vector3 Mundo(Vector3 suelo, Vector3 adelante, Vector3 arriba, Vector2 p, float escala)
    {
        return suelo + (adelante * p.x + arriba * p.y) * escala;
    }

    // La cabeza (para poner cosas encima del muñeco).
    public Vector3 Cabeza(Vector3 suelo, Vector3 arriba)
    {
        return suelo + arriba.normalized * 0.4f * escala;
    }

    public void Destruir()
    {
        if (go != null)
            Object.Destroy(go);
    }
}

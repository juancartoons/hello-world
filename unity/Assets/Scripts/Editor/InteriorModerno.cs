#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

// Interior de alta gama (lo usa el menú ★):
// - Techo futurista: anillos flotantes con luz LED (al frente), una "ola" de techo con bordes de LED
//   que recorre la farmacia hacia el fondo, una moldura con luz alrededor y muchos focos redondos.
// - Pared del fondo con listones de madera, el letrero iluminado y repisas naranjas con luz por debajo.
// - Mostrador blanco curvo (como la foto 4): computadores, caja registradora, datáfono, dulces, gel,
//   plantas, línea de cobre y luz LED por debajo que ilumina el piso.
// - Reflejo del piso brillante: una copia "de cabeza" de todo lo de adentro, debajo del piso.
internal static class InteriorModerno
{
    // Medidas de la farmacia por dentro
    const float xMin = -4.9f, xMax = 4.9f, zMin = -4.9f;
    static float ZMax => FachadaYExterior.Fondo - 0.1f;
    const float yTecho = 3.0f;

    static readonly Color blanco = new Color(0.97f, 0.97f, 0.98f);
    static readonly Color blancoLed = new Color(1f, 1f, 1f);
    static readonly Color calidoLed = new Color(1f, 0.9f, 0.72f);
    static readonly Color madera = new Color(0.80f, 0.60f, 0.40f);
    static readonly Color maderaOscura = new Color(0.36f, 0.24f, 0.16f);
    static readonly Color naranja = new Color(0.95f, 0.56f, 0.24f);
    static readonly Color cobre = new Color(0.84f, 0.50f, 0.30f);
    static readonly Color verdeCruz = new Color(0.15f, 0.85f, 0.45f);
    static readonly Color grisOscuro = new Color(0.22f, 0.24f, 0.30f);
    static readonly Color negro = new Color(0.1f, 0.1f, 0.12f);
    static readonly Color pantalla = new Color(0.3f, 0.5f, 0.78f);
    static readonly Color hojas = new Color(0.22f, 0.58f, 0.30f);

    static Color Resplandor(float a) => new Color(0.88f, 0.94f, 1f, a);
    static Color ResplandorCalido(float a) => new Color(1f, 0.82f, 0.55f, a);

    // ================= Techo =================

    // Anillos del frente (centro y radios)
    static readonly Vector2 centroAnillos = new Vector2(0f, -2.35f);
    static readonly Vector2 anilloA_fuera = new Vector2(3.35f, 1.75f), anilloA_dentro = new Vector2(2.45f, 1.1f);
    static readonly Vector2 anilloB_fuera = new Vector2(1.55f, 0.78f), anilloB_dentro = new Vector2(0.95f, 0.45f);
    const float yAnilloA = 2.8f, yAnilloB = 2.9f, yMoldura = 2.82f, yOla = 2.79f;
    const float anchoMoldura = 0.55f;

    // La "ola": franja de techo más baja que recorre la farmacia hacia el fondo, con bordes ondulados.
    const float zOlaInicio = 0f, zOlaFin = 7.6f;
    static float OlaIzquierda(float z) => OlaMitad(z) + (-2.1f + 0.75f * Mathf.Sin(0.8f * z + 0.4f) - OlaMitad(z)) * OlaPunta(z);
    static float OlaDerecha(float z) => OlaMitad(z) + (1.3f + 0.75f * Mathf.Sin(0.8f * z + 1.6f) - OlaMitad(z)) * OlaPunta(z);
    static float OlaMitad(float z) => (-2.1f + 0.75f * Mathf.Sin(0.8f * z + 0.4f) + 1.3f + 0.75f * Mathf.Sin(0.8f * z + 1.6f)) / 2f;
    static float OlaPunta(float z) => Mathf.Sqrt(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((z - zOlaInicio) / 1.6f)));

    internal static void ConstruirTecho(Transform raiz, Material matTechoKit, Material matBrillo, Material matResplandor, string carpeta)
    {
        var techo = new KitMalla();  // superficies (blancas)
        var luces = new KitMalla();  // LEDs y focos (brillan siempre)
        var brillo = new KitMalla(); // resplandor suave de las luces sobre el techo
        float cz = (zMin + ZMax) / 2f, hx = (xMax - xMin) / 2f, hz = (ZMax - zMin) / 2f;

        // --- Moldura alrededor, con una línea de luz en su borde ---
        var molduraFuera = RectRedondeado(0f, cz, hx, hz, 0.25f, 6);
        var molduraDentro = RectRedondeado(0f, cz, hx - anchoMoldura, hz - anchoMoldura, 0.7f, 6);
        Losa(techo, molduraFuera, molduraDentro, yMoldura, yTecho, blanco, false, true);
        LedEnBorde(luces, molduraDentro, false, yMoldura);
        BrilloEntre(brillo, molduraDentro, RectRedondeado(0f, cz, hx - anchoMoldura - 0.6f, hz - anchoMoldura - 0.6f, 0.9f, 6), yTecho - 0.003f, 0.45f);

        // --- Anillos flotantes del frente ---
        var aFuera = Elipse(centroAnillos, anilloA_fuera, 72);
        var aDentro = Elipse(centroAnillos, anilloA_dentro, 72);
        Losa(techo, aFuera, aDentro, yAnilloA, yTecho, blanco, true, true);
        LedEnBorde(luces, aFuera, true, yAnilloA);
        LedEnBorde(luces, aDentro, false, yAnilloA);
        BrilloEntre(brillo, aFuera, Elipse(centroAnillos, anilloA_fuera + Vector2.one * 0.45f, 72), yTecho - 0.003f, 0.4f);
        BrilloEntre(brillo, aDentro, Elipse(centroAnillos, anilloA_dentro - Vector2.one * 0.4f, 72), yTecho - 0.003f, 0.5f);

        var bFuera = Elipse(centroAnillos, anilloB_fuera, 64);
        var bDentro = Elipse(centroAnillos, anilloB_dentro, 64);
        Losa(techo, bFuera, bDentro, yAnilloB, yTecho, blanco, true, true);
        LedEnBorde(luces, bFuera, true, yAnilloB);
        LedEnBorde(luces, bDentro, false, yAnilloB);
        BrilloEntre(brillo, bDentro, Elipse(centroAnillos, anilloB_dentro - Vector2.one * 0.25f, 64), yTecho - 0.003f, 0.5f);

        // Panel de luz ovalado en el centro de los anillos
        var panel = Elipse(centroAnillos, new Vector2(0.55f, 0.24f), 40);
        var centro3 = new Vector3(centroAnillos.x, yTecho - 0.004f, centroAnillos.y);
        luces.Franja(A3(panel, yTecho - 0.004f), Repetir(centro3, panel.Count), Vector3.down, blancoLed, true);

        // --- La ola ---
        int n = 64;
        var izq = new List<Vector2>();
        var der = new List<Vector2>();
        for (int i = 0; i <= n; i++)
        {
            float z = Mathf.Lerp(zOlaInicio, zOlaFin, i / (float)n);
            izq.Add(new Vector2(OlaIzquierda(z), z));
            der.Add(new Vector2(OlaDerecha(z), z));
        }
        techo.Franja(A3(izq, yOla), A3(der, yOla), Vector3.down, blanco);
        var normalIzq = NormalesDeLinea(izq, -1f);
        var normalDer = NormalesDeLinea(der, 1f);
        techo.CintaVertical(izq, normalIzq, yOla + 0.04f, yTecho, blanco);
        techo.CintaVertical(der, normalDer, yOla + 0.04f, yTecho, blanco);
        luces.CintaVertical(Mover(izq, normalIzq, 0.003f), normalIzq, yOla, yOla + 0.04f, blancoLed);
        luces.CintaVertical(Mover(der, normalDer, 0.003f), normalDer, yOla, yOla + 0.04f, blancoLed);
        brillo.FranjaDegradada(A3(izq, yTecho - 0.003f), A3(Mover(izq, normalIzq, 0.45f), yTecho - 0.003f), Resplandor(0.45f), Resplandor(0f));
        brillo.FranjaDegradada(A3(der, yTecho - 0.003f), A3(Mover(der, normalDer, 0.45f), yTecho - 0.003f), Resplandor(0.45f), Resplandor(0f));

        // Dos líneas de luz finas que acompañan la ola por fuera (como en la foto 4)
        foreach (var lado in new[] { (izq, normalIzq), (der, normalDer) })
        {
            var linea = new List<Vector2>();
            var normales = new List<Vector2>();
            for (int i = 0; i < lado.Item1.Count; i++)
            {
                if (lado.Item1[i].y < 0.9f || lado.Item1[i].y > 7.25f)
                    continue;
                linea.Add(lado.Item1[i] + lado.Item2[i] * 0.55f);
                normales.Add(lado.Item2[i]);
            }
            luces.Franja(A3(Mover(linea, normales, -0.015f), yTecho - 0.004f), A3(Mover(linea, normales, 0.015f), yTecho - 0.004f), Vector3.down, blancoLed);
            brillo.FranjaDegradada(A3(linea, yTecho - 0.005f), A3(Mover(linea, normales, 0.16f), yTecho - 0.005f), Resplandor(0.35f), Resplandor(0f));
            brillo.FranjaDegradada(A3(linea, yTecho - 0.005f), A3(Mover(linea, normales, -0.16f), yTecho - 0.005f), Resplandor(0.35f), Resplandor(0f));
        }

        // --- Focos redondos ---
        foreach (var p in PosicionesFocos())
        {
            float y = AlturaTecho(p.x, p.y);
            Vector3 c = new Vector3(p.x, y - 0.003f, p.y);
            luces.DiscoDegradado(c, Vector3.down, 0.06f, blancoLed, blancoLed, 14);
            brillo.DiscoDegradado(c + Vector3.down * 0.002f, Vector3.down, 0.24f, Resplandor(0.55f), Resplandor(0f), 18);
        }

        techo.CrearObjeto("Kit_Techo", raiz, matTechoKit, carpeta);
        luces.CrearObjeto("Kit_TechoLuces", raiz, matBrillo, carpeta);
        brillo.CrearObjeto("Kit_TechoResplandor", raiz, matResplandor, carpeta);
    }

    static List<Vector2> PosicionesFocos()
    {
        var focos = new List<Vector2>();
        // En el anillo grande, a mitad de camino entre sus bordes
        for (int i = 0; i < 12; i++)
        {
            float a = (i + 0.5f) * Mathf.PI * 2f / 12f;
            focos.Add(centroAnillos + new Vector2(Mathf.Cos(a) * (anilloA_fuera.x + anilloA_dentro.x) / 2f, Mathf.Sin(a) * (anilloA_fuera.y + anilloA_dentro.y) / 2f));
        }
        // En la moldura, a lo largo de las paredes
        float mitadMoldura = anchoMoldura / 2f;
        for (float z = -3.9f; z <= 7.0f; z += 1.1f)
        {
            focos.Add(new Vector2(xMin + mitadMoldura, z));
            focos.Add(new Vector2(xMax - mitadMoldura, z));
        }
        for (float x = -3.9f; x <= 3.95f; x += 1.3f)
        {
            focos.Add(new Vector2(x, zMin + mitadMoldura));
            focos.Add(new Vector2(x, ZMax - mitadMoldura));
        }
        // Debajo de la ola, por el centro
        for (float z = 0.9f; z <= 7.1f; z += 0.9f)
            focos.Add(new Vector2(OlaMitad(z), z));
        // En el techo, sobre los pasillos de los lados y en las esquinas del frente
        for (float z = 0.4f; z <= 6.9f; z += 1.6f)
        {
            focos.Add(new Vector2(-3.6f, z));
            focos.Add(new Vector2(3.6f, z));
        }
        foreach (float x in new[] { -3.95f, 3.95f })
            foreach (float z in new[] { -3.6f, -1.0f })
                focos.Add(new Vector2(x, z));
        return focos;
    }

    // Altura de la superficie del techo (la más baja) en un punto.
    static float AlturaTecho(float x, float z)
    {
        float cz = (zMin + ZMax) / 2f;
        if (Mathf.Abs(x) > xMax - anchoMoldura || Mathf.Abs(z - cz) > (ZMax - zMin) / 2f - anchoMoldura)
            return yMoldura;
        if (DentroElipse(x, z, anilloA_fuera) && !DentroElipse(x, z, anilloA_dentro))
            return yAnilloA;
        if (DentroElipse(x, z, anilloB_fuera) && !DentroElipse(x, z, anilloB_dentro))
            return yAnilloB;
        if (z > zOlaInicio && z < zOlaFin && x > OlaIzquierda(z) && x < OlaDerecha(z))
            return yOla;
        return yTecho;
    }

    static bool DentroElipse(float x, float z, Vector2 radios)
    {
        float dx = (x - centroAnillos.x) / radios.x, dz = (z - centroAnillos.y) / radios.y;
        return dx * dx + dz * dz <= 1f;
    }

    // Losa del techo entre dos curvas cerradas (afuera y adentro), de yAbajo hasta el techo.
    static void Losa(KitMalla k, List<Vector2> fuera, List<Vector2> dentro, float yAbajo, float yArriba, Color color, bool caraFuera, bool caraDentro)
    {
        k.Franja(A3(fuera, yAbajo), A3(dentro, yAbajo), Vector3.down, color, true);
        if (caraFuera)
            k.CintaVertical(fuera, KitMalla.NormalesDeContorno(fuera), yAbajo + 0.04f, yArriba, color, true);
        if (caraDentro)
            k.CintaVertical(dentro, Invertir(KitMalla.NormalesDeContorno(dentro)), yAbajo + 0.04f, yArriba, color, true);
    }

    // Línea de LED en el borde de abajo de una losa (en su cara de afuera o de adentro).
    static void LedEnBorde(KitMalla luces, List<Vector2> curva, bool haciaAfuera, float yAbajo)
    {
        var normales = KitMalla.NormalesDeContorno(curva);
        if (!haciaAfuera)
            normales = Invertir(normales);
        luces.CintaVertical(Mover(curva, normales, 0.003f), normales, yAbajo, yAbajo + 0.04f, blancoLed, true);
    }

    static void BrilloEntre(KitMalla brillo, List<Vector2> borde, List<Vector2> lejos, float y, float intensidad)
    {
        brillo.FranjaDegradada(A3(borde, y), A3(lejos, y), Resplandor(intensidad), Resplandor(0f), true);
    }

    // ================= Pared del fondo: madera, letrero y repisas =================

    internal static void ConstruirParedFondo(Transform raiz, Material matKit, Material matBrillo, Material matResplandor, string carpeta)
    {
        var kit = new KitMalla();
        var luces = new KitMalla();
        var brillo = new KitMalla();
        float zp = ZMax;          // cara de la pared del fondo
        float yArriba = yMoldura; // la madera llega hasta la moldura del techo

        // Listones verticales de madera sobre un fondo oscuro (como la foto 4)
        kit.CajaMinMax(new Vector3(-2.7f, 0f, zp - 0.025f), new Vector3(2.7f, yArriba, zp), maderaOscura, false);
        var rnd = new System.Random(5);
        for (int i = 0; i < 67; i++)
        {
            float x = -2.64f + i * 0.08f;
            float f = 0.92f + (float)rnd.NextDouble() * 0.14f;
            kit.CajaMinMax(new Vector3(x - 0.025f, 0f, zp - 0.065f), new Vector3(x + 0.025f, yArriba, zp - 0.025f),
                new Color(madera.r * f, madera.g * f, madera.b * f), false);
        }
        // Líneas de luz verticales a los lados de la madera
        foreach (float s in new[] { -1f, 1f })
        {
            float x = s * 2.73f;
            luces.CajaMinMax(new Vector3(x - 0.012f, 0f, zp - 0.03f), new Vector3(x + 0.012f, yArriba, zp - 0.006f), calidoLed, false);
            var cerca = new List<Vector3> { new Vector3(x + s * 0.015f, 0f, zp - 0.004f), new Vector3(x + s * 0.015f, yArriba, zp - 0.004f) };
            var lejos = new List<Vector3> { new Vector3(x + s * 0.4f, 0f, zp - 0.004f), new Vector3(x + s * 0.4f, yArriba, zp - 0.004f) };
            brillo.FranjaDegradada(cerca, lejos, ResplandorCalido(0.35f), ResplandorCalido(0f));
        }

        // Letrero: cruz verde iluminada y resplandor detrás de las letras (las letras se ponen abajo)
        float yLetrero = 2.2f, zLetrero = zp - 0.075f;
        luces.Caja(new Vector3(-1.55f, yLetrero, zLetrero), new Vector3(0.3f, 0.1f, 0.02f), verdeCruz, false);
        luces.Caja(new Vector3(-1.55f, yLetrero, zLetrero), new Vector3(0.1f, 0.3f, 0.02f), verdeCruz, false);
        RectanguloSuave(brillo, new Vector3(0.1f, yLetrero, zp - 0.067f), 1.55f, 0.24f, 0.3f, ResplandorCalido(0.4f));

        // Repisas blancas con fondo naranja y luz por debajo, a los dos lados de la madera
        kit.grosorContorno = 0.6f;
        foreach (float s in new[] { -1f, 1f })
        {
            float x0 = s > 0 ? 3.05f : -4.85f, x1 = s > 0 ? 4.85f : -3.05f;
            float zf = zp - 0.3f;
            kit.CajaMinMax(new Vector3(x0, 0.1f, zp - 0.02f), new Vector3(x1, 2.2f, zp), naranja, false);
            kit.CajaMinMax(new Vector3(x0, 0f, zf), new Vector3(x0 + 0.025f, 2.2f, zp), blanco);
            kit.CajaMinMax(new Vector3(x1 - 0.025f, 0f, zf), new Vector3(x1, 2.2f, zp), blanco);
            kit.CajaMinMax(new Vector3(x0, 0f, zf + 0.02f), new Vector3(x1, 0.1f, zp), blanco);
            float[] niveles = { 0.1f, 0.52f, 0.94f, 1.36f, 1.78f, 2.2f };
            for (int i = 0; i < niveles.Length; i++)
            {
                float y = niveles[i];
                kit.CajaMinMax(new Vector3(x0, y - 0.0125f, zf), new Vector3(x1, y + 0.0125f, zp - 0.02f), blanco);
                if (i == 0)
                    continue;
                // LED debajo de la repisa y su luz sobre el fondo naranja
                luces.CajaMinMax(new Vector3(x0 + 0.03f, y - 0.022f, zf + 0.01f), new Vector3(x1 - 0.03f, y - 0.0125f, zf + 0.03f), calidoLed, false);
                brillo.FranjaDegradada(
                    new List<Vector3> { new Vector3(x0 + 0.03f, y - 0.0125f, zp - 0.022f), new Vector3(x1 - 0.03f, y - 0.0125f, zp - 0.022f) },
                    new List<Vector3> { new Vector3(x0 + 0.03f, y - 0.32f, zp - 0.022f), new Vector3(x1 - 0.03f, y - 0.32f, zp - 0.022f) },
                    ResplandorCalido(0.4f), ResplandorCalido(0f));
            }
            // Productos (cajas blancas con franjas de colores, como en las fotos)
            kit.grosorContorno = 0.45f;
            for (int i = 0; i < niveles.Length - 1; i++)
            {
                float yBase = niveles[i] + 0.0125f;
                float alturaLibre = niveles[i + 1] - niveles[i] - 0.06f;
                for (float x = x0 + 0.05f; x < x1 - 0.12f;)
                {
                    float ancho = 0.05f + (float)rnd.NextDouble() * 0.07f;
                    float alto = Mathf.Min(alturaLibre, 0.1f + (float)rnd.NextDouble() * 0.18f);
                    float fondo = 0.1f + (float)rnd.NextDouble() * 0.1f;
                    Color franja = Productos[rnd.Next(Productos.Length)];
                    Color caja = rnd.NextDouble() < 0.6 ? new Color(0.96f, 0.96f, 0.95f) : franja;
                    Vector3 c = new Vector3(x + ancho / 2f, yBase + alto / 2f, zf + 0.02f + fondo / 2f);
                    kit.Caja(c, new Vector3(ancho, alto, fondo), caja);
                    kit.Etiqueta(new Vector3(c.x, yBase + alto * 0.6f, zf + 0.018f), Vector3.back, alto * 0.12f, ancho * 0.45f, franja == caja ? blanco : franja);
                    x += ancho + 0.01f;
                }
            }
            kit.grosorContorno = 0.6f;
        }
        kit.grosorContorno = 1f;

        var go = kit.CrearObjeto("Kit_ParedFondo", raiz, matKit, carpeta);
        luces.CrearObjeto("Kit_ParedFondoLuces", raiz, matBrillo, carpeta);
        brillo.CrearObjeto("Kit_ParedFondoResplandor", raiz, matResplandor, carpeta);

        // Las repisas son sólidas (no se puede caminar ni teletransportar a través de ellas)
        var colisiones = new GameObject("Colisiones");
        colisiones.transform.SetParent(go.transform, false);
        foreach (float s in new[] { -1f, 1f })
        {
            var caja = colisiones.AddComponent<BoxCollider>();
            caja.center = new Vector3(s * 3.95f, 1.1f, zp - 0.15f);
            caja.size = new Vector3(1.8f, 2.2f, 0.3f);
        }

        if (Resources.Load<TMP_Settings>("TMP Settings") != null)
            Texto(raiz, "Farma-CIA Agencia", new Vector3(0.15f, yLetrero, zp - 0.075f), Quaternion.identity, new Vector2(2.7f, 0.42f), Color.white);
    }

    static readonly Color[] Productos =
    {
        new Color(0.91f, 0.26f, 0.24f), new Color(0.98f, 0.60f, 0.20f), new Color(0.99f, 0.83f, 0.25f),
        new Color(0.36f, 0.75f, 0.40f), new Color(0.20f, 0.70f, 0.70f), new Color(0.25f, 0.50f, 0.90f),
        new Color(0.60f, 0.40f, 0.85f), new Color(0.95f, 0.50f, 0.70f),
    };

    // Rectángulo de luz con bordes difuminados (mirando hacia -Z).
    static void RectanguloSuave(KitMalla k, Vector3 centro, float mitadAncho, float mitadAlto, float borde, Color color)
    {
        Color nada = new Color(color.r, color.g, color.b, 0f);
        var dentro = new List<Vector3>
        {
            centro + new Vector3(-mitadAncho, -mitadAlto, 0f), centro + new Vector3(mitadAncho, -mitadAlto, 0f),
            centro + new Vector3(mitadAncho, mitadAlto, 0f), centro + new Vector3(-mitadAncho, mitadAlto, 0f),
        };
        var fuera = new List<Vector3>
        {
            centro + new Vector3(-mitadAncho - borde, -mitadAlto - borde, 0f), centro + new Vector3(mitadAncho + borde, -mitadAlto - borde, 0f),
            centro + new Vector3(mitadAncho + borde, mitadAlto + borde, 0f), centro + new Vector3(-mitadAncho - borde, mitadAlto + borde, 0f),
        };
        k.CuadroColores(dentro[0], dentro[1], dentro[2], dentro[3], color, color, color, color);
        k.FranjaDegradada(dentro, fuera, color, nada, true);
    }

    // ================= Mostrador curvo =================

    const float xPunta = 2.1f, zAtras = 7.55f, zPuntas = 7.0f, curva = 0.38f;
    static float ZFrente(float x) => zPuntas - curva * (1f - (x / xPunta) * (x / xPunta));
    static Vector2 NormalFrente(float x) => new Vector2(2f * curva * x / (xPunta * xPunta), -1f).normalized;

    static List<Vector2> FormaMostrador()
    {
        var f = new List<Vector2>();
        const int n = 24;
        for (int i = 0; i <= n; i++)
        {
            float x = Mathf.Lerp(-xPunta, xPunta, i / (float)n);
            f.Add(new Vector2(x, ZFrente(x)));
        }
        float radio = (zAtras - zPuntas) / 2f;
        var cDer = new Vector2(xPunta, zPuntas + radio);
        for (int i = 1; i < 8; i++)
        {
            float a = (-90f + 180f * i / 8f) * Mathf.Deg2Rad;
            f.Add(cDer + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio);
        }
        f.Add(new Vector2(xPunta, zAtras));
        f.Add(new Vector2(-xPunta, zAtras));
        var cIzq = new Vector2(-xPunta, zPuntas + radio);
        for (int i = 1; i < 8; i++)
        {
            float a = (90f + 180f * i / 8f) * Mathf.Deg2Rad;
            f.Add(cIzq + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio);
        }
        return f;
    }

    // Puntos del frente del mostrador (de izquierda a derecha), corridos "d" metros hacia afuera.
    static List<Vector2> Frente(float x0, float x1, int n, float d, out List<Vector2> normales)
    {
        var puntos = new List<Vector2>();
        normales = new List<Vector2>();
        for (int i = 0; i <= n; i++)
        {
            float x = Mathf.Lerp(x0, x1, i / (float)n);
            Vector2 normal = NormalFrente(x);
            puntos.Add(new Vector2(x, ZFrente(x)) + normal * d);
            normales.Add(normal);
        }
        return puntos;
    }

    internal static void ConstruirMostrador(Transform raiz, Material matKit, Material matBrillo, Material matResplandor, string carpeta)
    {
        // El bloque original de la maqueta ya no se usa (ni se ve ni estorba): este mostrador tiene sus propias colisiones.
        var original = GameObject.Find("Mostrador");
        if (original != null)
        {
            var r = original.GetComponent<MeshRenderer>();
            if (r != null) { Undo.RecordObject(r, "Mostrador"); r.enabled = false; }
            var col = original.GetComponent<Collider>();
            if (col != null) { Undo.RecordObject(col, "Mostrador"); col.enabled = false; }
        }

        var kit = new KitMalla();
        var luces = new KitMalla();
        var brillo = new KitMalla();
        var forma = FormaMostrador();
        const float yTope = 1.05f;

        // Cuerpo blanco con curvas, tapa y zócalo oscuro metido (con luz LED que ilumina el piso)
        kit.Extruir(KitMalla.Desplazar(forma, -0.07f), 0f, 0.1f, new Color(0.24f, 0.25f, 0.29f), false, true);
        kit.grosorContorno = 0.6f;
        kit.Extruir(forma, 0.1f, 1.0f, blanco, true, true);
        kit.Extruir(KitMalla.Desplazar(forma, 0.03f), 1.0f, yTope, new Color(0.995f, 0.995f, 1f), true, true);
        // "Bancas" redondeadas al frente, como olas
        kit.Esfera(new Vector3(-1.05f, 0.24f, ZFrente(-1.05f) - 0.08f), new Vector3(0.42f, 0.24f, 0.26f), 3, blanco, true, false, true);
        kit.Esfera(new Vector3(0.95f, 0.2f, ZFrente(0.95f) - 0.07f), new Vector3(0.36f, 0.2f, 0.22f), 3, blanco, true, false, true);
        kit.grosorContorno = 1f;

        var frenteLed = Frente(-xPunta, xPunta, 32, -0.065f, out var normalesLed);
        luces.CintaVertical(frenteLed, normalesLed, 0.065f, 0.095f, blancoLed);
        var frentePiso = Frente(-xPunta, xPunta, 32, 0f, out var normalesPiso);
        brillo.FranjaDegradada(A3(frentePiso, 0.004f), A3(Mover(frentePiso, normalesPiso, 0.6f), 0.004f), Resplandor(0.5f), Resplandor(0f));

        // Línea de cobre al frente (con espacio en el centro para el nombre)
        foreach (var tramo in new[] { (-xPunta, -0.6f), (0.6f, xPunta) })
        {
            var linea = Frente(tramo.Item1, tramo.Item2, 12, 0.004f, out var normales);
            kit.CintaVertical(linea, normales, 0.7f, 0.722f, cobre);
        }

        // Computadores (miran hacia el cajero; el cliente ve la parte de atrás, blanca)
        foreach (float x in new[] { -0.75f, 0.45f })
        {
            float z = 7.22f;
            kit.Caja(new Vector3(x, yTope + 0.006f, z + 0.04f), new Vector3(0.2f, 0.012f, 0.15f), blanco);
            kit.Caja(new Vector3(x, yTope + 0.13f, z + 0.03f), new Vector3(0.04f, 0.24f, 0.03f), blanco);
            kit.Caja(new Vector3(x, yTope + 0.36f, z), new Vector3(0.52f, 0.33f, 0.025f), blanco);
            luces.Etiqueta(new Vector3(x, yTope + 0.37f, z + 0.0135f), Vector3.forward, 0.14f, 0.24f, pantalla);
        }

        // Caja registradora moderna (pantalla inclinada hacia el cajero), impresora de recibos y datáfono
        float xc = 1.45f, zc = 7.25f;
        kit.Caja(new Vector3(xc, yTope + 0.035f, zc), new Vector3(0.36f, 0.07f, 0.3f), blanco);
        kit.Hexaedro(new[]
        {
            new Vector3(xc - 0.16f, yTope + 0.07f, zc - 0.05f), new Vector3(xc + 0.16f, yTope + 0.07f, zc - 0.05f),
            new Vector3(xc + 0.16f, yTope + 0.07f, zc + 0.02f), new Vector3(xc - 0.16f, yTope + 0.07f, zc + 0.02f),
            new Vector3(xc - 0.16f, yTope + 0.3f, zc - 0.12f), new Vector3(xc + 0.16f, yTope + 0.3f, zc - 0.12f),
            new Vector3(xc + 0.16f, yTope + 0.3f, zc - 0.07f), new Vector3(xc - 0.16f, yTope + 0.3f, zc - 0.07f),
        }, blanco);
        luces.CuadroLibre(new Vector3(xc - 0.13f, yTope + 0.1f, zc + 0.016f), new Vector3(xc + 0.13f, yTope + 0.1f, zc + 0.016f),
            new Vector3(xc + 0.13f, yTope + 0.28f, zc - 0.064f), new Vector3(xc - 0.13f, yTope + 0.28f, zc - 0.064f),
            new Vector3(0f, 0.4f, 1f), pantalla);
        kit.Caja(new Vector3(1.85f, yTope + 0.06f, 7.3f), new Vector3(0.15f, 0.12f, 0.18f), blanco);
        kit.Caja(new Vector3(1.85f, yTope + 0.125f, 7.24f), new Vector3(0.07f, 0.01f, 0.05f), new Color(0.98f, 0.98f, 0.96f), false);
        kit.Caja(new Vector3(1.1f, yTope + 0.02f, 6.9f), new Vector3(0.09f, 0.04f, 0.16f), negro);
        luces.Piso(new Vector3(1.1f, yTope + 0.041f, 6.87f), 0.03f, 0.025f, pantalla);

        // Exhibidor de dulces, gel antibacterial y una matera pequeña
        float xd = -1.5f, zd = 7.05f;
        kit.Caja(new Vector3(xd, yTope + 0.04f, zd - 0.06f), new Vector3(0.45f, 0.08f, 0.2f), blanco);
        kit.Caja(new Vector3(xd, yTope + 0.14f, zd + 0.06f), new Vector3(0.45f, 0.12f, 0.12f), blanco);
        kit.grosorContorno = 0.5f;
        for (int i = 0; i < 5; i++)
        {
            kit.Caja(new Vector3(xd - 0.18f + i * 0.09f, yTope + 0.11f, zd - 0.09f), new Vector3(0.07f, 0.06f, 0.08f), Productos[i % Productos.Length]);
            kit.Caja(new Vector3(xd - 0.18f + i * 0.09f, yTope + 0.23f, zd + 0.05f), new Vector3(0.07f, 0.06f, 0.07f), Productos[(i + 3) % Productos.Length]);
        }
        kit.grosorContorno = 1f;
        kit.Cilindro(new Vector3(-0.2f, yTope + 0.08f, 6.85f), 0.04f, 0.16f, Vector3.up, 14, blanco, true, true);
        kit.Cilindro(new Vector3(-0.2f, yTope + 0.18f, 6.85f), 0.012f, 0.04f, Vector3.up, 8, new Color(0.09f, 0.36f, 0.72f));
        kit.Cilindro(new Vector3(-1.95f, yTope + 0.07f, 7.3f), 0.07f, 0.14f, Vector3.up, 16, blanco, true, true);
        kit.Esfera(new Vector3(-1.95f, yTope + 0.22f, 7.3f), new Vector3(0.12f, 0.14f, 0.12f), 2, hojas, true, false, true);

        // Palma grande en matera blanca, junto al mostrador y la madera
        Palma(kit, new Vector3(-2.8f, 0f, 7.35f));

        var go = kit.CrearObjeto("Kit_Mostrador", raiz, matKit, carpeta);
        luces.CrearObjeto("Kit_MostradorLuces", raiz, matBrillo, carpeta);
        brillo.CrearObjeto("Kit_MostradorResplandor", raiz, matResplandor, carpeta);

        // Colisiones: el mostrador, las bancas y la palma son sólidos.
        var colisiones = new GameObject("Colisiones");
        colisiones.transform.SetParent(go.transform, false);
        void Caja(Vector3 centro, Vector3 tam)
        {
            var c = colisiones.AddComponent<BoxCollider>();
            c.center = centro;
            c.size = tam;
        }
        Caja(new Vector3(0f, 0.525f, 7.275f), new Vector3(4.75f, 1.05f, 0.55f));
        Caja(new Vector3(0f, 0.525f, 6.85f), new Vector3(3.6f, 1.05f, 0.3f));
        Caja(new Vector3(0f, 0.525f, 6.66f), new Vector3(1.6f, 1.05f, 0.1f));
        Caja(new Vector3(-1.05f, 0.24f, 6.6f), new Vector3(0.84f, 0.48f, 0.5f));
        Caja(new Vector3(0.95f, 0.2f, 6.62f), new Vector3(0.72f, 0.4f, 0.44f));
        Caja(new Vector3(-2.8f, 0.6f, 7.35f), new Vector3(0.45f, 1.2f, 0.45f));

        if (Resources.Load<TMP_Settings>("TMP Settings") != null)
        {
            Texto(raiz, "Farma-CIA", new Vector3(0f, 0.71f, ZFrente(0f) - 0.006f), Quaternion.identity, new Vector2(0.9f, 0.13f), cobre);
            float xCaja = 1.45f;
            float angulo = -Mathf.Atan(2f * curva * xCaja / (xPunta * xPunta)) * Mathf.Rad2Deg;
            Vector2 n = NormalFrente(xCaja);
            Texto(raiz, "CAJA", new Vector3(xCaja + n.x * 0.006f, 0.86f, ZFrente(xCaja) + n.y * 0.006f), Quaternion.Euler(0f, angulo, 0f), new Vector2(0.4f, 0.11f), new Color(0.1f, 0.62f, 0.38f));
        }
    }

    // Palma: matera blanca, tallo y hojas largas que caen hacia los lados.
    static void Palma(KitMalla k, Vector3 piso)
    {
        k.grosorContorno = 0.6f;
        k.Cilindro(piso + Vector3.up * 0.25f, 0.22f, 0.5f, Vector3.up, 20, blanco, true, true);
        k.Cilindro(piso + Vector3.up * 0.51f, 0.2f, 0.02f, Vector3.up, 20, new Color(0.35f, 0.25f, 0.18f), true, true);
        k.Cilindro(piso + Vector3.up * 0.85f, 0.03f, 0.7f, Vector3.up, 8, new Color(0.45f, 0.35f, 0.22f), true, true);
        k.grosorContorno = 1f;
        Vector3 copa = piso + Vector3.up * 1.18f;
        for (int capa = 0; capa < 2; capa++)
        {
            int hojasCapa = capa == 0 ? 9 : 6;
            for (int i = 0; i < hojasCapa; i++)
            {
                float a = (i + capa * 0.5f) * Mathf.PI * 2f / hojasCapa;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 lado = Vector3.Cross(Vector3.up, dir);
                float largo = capa == 0 ? 0.75f : 0.5f;
                Vector3 inicio = copa + Vector3.up * (capa * 0.12f);
                Vector3 medio = inicio + dir * largo * 0.5f + Vector3.up * (capa == 0 ? 0.12f : 0.22f);
                Vector3 punta = inicio + dir * largo + Vector3.down * (capa == 0 ? 0.25f : 0.05f);
                Color c = capa == 0 ? hojas : new Color(0.3f, 0.66f, 0.36f);
                Vector3 l = lado * (largo * 0.13f);
                // Dos caras (se ve por arriba y por abajo)
                k.CuadroLibre(inicio, medio + l, punta, medio - l, Vector3.up, c);
                k.CuadroLibre(inicio, medio - l, punta, medio + l, Vector3.down, c * 0.85f);
            }
        }
    }

    // ================= Reflejo en el piso brillante =================

    // Copia "de cabeza" (escala Y = -1) de lo que hay adentro; el piso semitransparente la deja ver como un reflejo.
    internal static void ConstruirReflejos(Transform raiz, Material reflejoKit, Material reflejoBrillo, Material reflejoTecho, Material reflejoPared)
    {
        var espejo = new GameObject("ReflejoDelPiso");
        espejo.transform.SetParent(raiz, false);
        espejo.transform.localScale = new Vector3(1f, -1f, 1f);

        void Copiar(string nombre, Material mat)
        {
            var go = GameObject.Find(nombre);
            if (go == null)
                return;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (!mr.enabled || mf == null || mf.sharedMesh == null)
                    continue;
                var copia = new GameObject("Reflejo_" + mr.name);
                copia.transform.SetParent(espejo.transform, false);
                copia.transform.localPosition = mr.transform.position;
                copia.transform.localRotation = mr.transform.rotation;
                copia.transform.localScale = mr.transform.lossyScale;
                copia.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var r = copia.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        foreach (var nombre in new[] { "Kit_Fachada", "Kit_Techo", "Kit_Gondola_1", "Kit_Gondola_2", "Kit_Gondola_3",
                     "Kit_Estante_Este", "Kit_Estante_Oeste", "Kit_Mostrador", "Kit_ParedFondo", "Kit_Decoracion" })
            Copiar(nombre, reflejoKit);
        foreach (var nombre in new[] { "Kit_TechoLuces", "Kit_MostradorLuces", "Kit_ParedFondoLuces", "Kit_LucesMuebles" })
            Copiar(nombre, reflejoBrillo);
        Copiar("Techo", reflejoTecho);
        Copiar("Pared_Norte", reflejoPared);
    }

    // ================= Ayudas =================

    static List<Vector2> Elipse(Vector2 centro, Vector2 radios, int n)
    {
        var puntos = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            puntos.Add(centro + new Vector2(Mathf.Cos(a) * radios.x, Mathf.Sin(a) * radios.y));
        }
        return puntos;
    }

    // Rectángulo con esquinas redondeadas (siempre la misma cantidad de puntos, para unir dos de distinto tamaño).
    static List<Vector2> RectRedondeado(float cx, float cz, float mitadX, float mitadZ, float radio, int puntosEsquina)
    {
        var puntos = new List<Vector2>();
        var esquinas = new[]
        {
            (new Vector2(cx + mitadX - radio, cz + mitadZ - radio), 0f),
            (new Vector2(cx - mitadX + radio, cz + mitadZ - radio), 90f),
            (new Vector2(cx - mitadX + radio, cz - mitadZ + radio), 180f),
            (new Vector2(cx + mitadX - radio, cz - mitadZ + radio), 270f),
        };
        foreach (var esquina in esquinas)
            for (int i = 0; i <= puntosEsquina; i++)
            {
                float a = (esquina.Item2 + 90f * i / puntosEsquina) * Mathf.Deg2Rad;
                puntos.Add(esquina.Item1 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio);
            }
        return puntos;
    }

    // Normales de una línea abierta (en XZ), hacia el lado indicado de X (-1 = izquierda, 1 = derecha).
    static List<Vector2> NormalesDeLinea(List<Vector2> linea, float ladoX)
    {
        var normales = new List<Vector2>(linea.Count);
        for (int i = 0; i < linea.Count; i++)
        {
            Vector2 t = linea[Mathf.Min(i + 1, linea.Count - 1)] - linea[Mathf.Max(i - 1, 0)];
            Vector2 normal = new Vector2(t.y, -t.x).normalized;
            if (normal.x * ladoX < 0f)
                normal = -normal;
            normales.Add(normal);
        }
        return normales;
    }

    static List<Vector2> Mover(List<Vector2> puntos, List<Vector2> normales, float d)
    {
        var nuevos = new List<Vector2>(puntos.Count);
        for (int i = 0; i < puntos.Count; i++)
            nuevos.Add(puntos[i] + normales[i] * d);
        return nuevos;
    }

    static List<Vector2> Invertir(List<Vector2> normales)
    {
        var nuevas = new List<Vector2>(normales.Count);
        foreach (var n in normales)
            nuevas.Add(-n);
        return nuevas;
    }

    static List<Vector3> A3(List<Vector2> puntos, float y)
    {
        var lista = new List<Vector3>(puntos.Count);
        foreach (var p in puntos)
            lista.Add(new Vector3(p.x, y, p.y));
        return lista;
    }

    static List<Vector3> Repetir(Vector3 p, int n)
    {
        var lista = new List<Vector3>(n);
        for (int i = 0; i < n; i++)
            lista.Add(p);
        return lista;
    }

    static void Texto(Transform padre, string texto, Vector3 posicion, Quaternion rotacion, Vector2 tamano, Color color)
    {
        var go = new GameObject("Texto_" + texto, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = texto;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.05f;
        tmp.fontSizeMax = 30f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = tamano;
        go.transform.SetPositionAndRotation(posicion, rotacion);
    }
}
#endif

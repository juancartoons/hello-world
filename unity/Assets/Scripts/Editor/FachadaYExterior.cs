#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Construye (desde el menú ★) las paredes con ventanas, los vidrios, los rayos de sol que entran
// y el exterior: calles de Bogotá, parqueadero, edificios de ladrillo, árboles, carros, bus del SITP,
// cerros orientales, nubes y cielo. Todo en pocas mallas para que la Quest 2 lo mueva sin problema.
internal static class FachadaYExterior
{
    struct Hueco
    {
        public float a0, a1, y0, y1;
        public bool puerta;
        public Hueco(float a0, float a1, float y0, float y1, bool puerta = false)
        {
            this.a0 = a0; this.a1 = a1; this.y0 = y0; this.y1 = y1; this.puerta = puerta;
        }
    }

    static readonly Color pared = new Color(0.97f, 0.96f, 0.93f);
    static readonly Color marco = new Color(0.35f, 0.38f, 0.44f);
    static readonly Color grisOscuro = new Color(0.22f, 0.24f, 0.30f);
    static readonly Color grisClaro = new Color(0.86f, 0.87f, 0.88f);
    static readonly Color acera = new Color(0.78f, 0.78f, 0.76f);
    static readonly Color asfalto = new Color(0.30f, 0.31f, 0.34f);
    static readonly Color asfaltoParqueo = new Color(0.40f, 0.41f, 0.44f);
    static readonly Color pasto = new Color(0.47f, 0.62f, 0.40f);
    static readonly Color blanco = new Color(0.95f, 0.95f, 0.92f);
    static readonly Color amarillo = new Color(0.98f, 0.80f, 0.20f);
    static readonly Color vidrioOscuro = new Color(0.22f, 0.30f, 0.40f);
    static readonly Color concreto = new Color(0.84f, 0.82f, 0.78f);
    static readonly Color tronco = new Color(0.42f, 0.30f, 0.22f);
    static readonly Color azulSitp = new Color(0.12f, 0.38f, 0.78f);
    static readonly Color[] ladrillos =
    {
        new Color(0.70f, 0.35f, 0.24f), new Color(0.62f, 0.30f, 0.22f),
        new Color(0.76f, 0.42f, 0.28f), new Color(0.66f, 0.38f, 0.30f),
    };
    static readonly Color[] verdes =
    {
        new Color(0.30f, 0.58f, 0.32f), new Color(0.24f, 0.50f, 0.28f), new Color(0.38f, 0.64f, 0.34f),
    };
    // Colores del carro familiar (el beige de la referencia primero).
    static readonly Color[] coloresFamiliar =
    {
        new Color(0.92f, 0.86f, 0.74f), new Color(0.82f, 0.20f, 0.18f), new Color(0.45f, 0.65f, 0.85f),
        new Color(0.95f, 0.95f, 0.93f), new Color(0.35f, 0.60f, 0.42f), new Color(0.65f, 0.66f, 0.70f),
        new Color(0.18f, 0.28f, 0.55f), new Color(0.50f, 0.15f, 0.20f),
    };

    // ================= Fachada: paredes con ventanas, vidrios y sol =================

    internal static void ConstruirFachada(Transform raiz, Material matKit, Material matVidrio, Material matLuz, Vector3 dirLuz, string carpeta)
    {
        var fachada = new KitMalla();
        var vidrio = new KitMalla();
        var luz = new KitMalla();

        // Sur: vitrina con puerta en el centro.
        var sur = new List<Hueco>
        {
            new Hueco(-4.5f, -1.0f, 0.5f, 2.5f),
            new Hueco(-0.7f, 0.7f, 0f, 2.3f, true),
            new Hueco(1.0f, 4.5f, 0.5f, 2.5f),
        };
        // Este y oeste: ventana grande cerca de la entrada y ventanas altas sobre los estantes.
        var lados = new List<Hueco>
        {
            new Hueco(-4.6f, -2.8f, 0.5f, 2.5f),
            new Hueco(-2.2f, -0.4f, 2.15f, 2.7f),
            new Hueco(0.2f, 2.0f, 2.15f, 2.7f),
            new Hueco(2.6f, 4.4f, 2.15f, 2.7f),
        };

        Pared(fachada, vidrio, false, -5.0f, sur);
        Pared(fachada, vidrio, true, 5.0f, lados);
        Pared(fachada, vidrio, true, -5.0f, lados);

        // Esquinas, losa del techo y umbral de la puerta.
        foreach (float x in new[] { -5.05f, 5.05f })
            foreach (float z in new[] { -5.05f, 5.05f })
                fachada.Caja(new Vector3(x, 1.625f, z), new Vector3(0.3f, 3.25f, 0.3f), pared);
        fachada.CajaMinMax(new Vector3(-5.25f, 3.1f, -5.25f), new Vector3(5.25f, 3.25f, 5.25f), grisClaro);
        fachada.CajaMinMax(new Vector3(-0.7f, -0.12f, -5.1f), new Vector3(0.7f, 0f, -4.9f), acera, false);

        // Sol entrando solo por la ventana grande de la derecha (pared este, cerca de la entrada).
        foreach (var h in lados)
            SolPorHueco(luz, true, 4.9f, h, dirLuz);

        fachada.CrearObjeto("Kit_Fachada", raiz, matKit, carpeta);
        vidrio.CrearObjeto("Vidrios", raiz, matVidrio, carpeta);
        luz.CrearObjeto("LuzDeSol", raiz, matLuz, carpeta);
    }

    // Pared de 0.2 m de grosor con huecos. Si "enX" es true, la pared va a lo largo de Z en x = fijo;
    // si no, va a lo largo de X en z = fijo.
    static void Pared(KitMalla kit, KitMalla vidrio, bool enX, float fijo, List<Hueco> huecos)
    {
        const float mitadGrosor = 0.1f, alto = 3.0f, inicio = -5.1f, fin = 5.1f;

        Vector3 P(float a, float y, float p) => enX ? new Vector3(p, y, a) : new Vector3(a, y, p);
        void Bloque(float a0, float a1, float y0, float y1, float p0, float p1, Color color, bool contorno, KitMalla destino)
        {
            Vector3 q0 = P(a0, y0, p0), q1 = P(a1, y1, p1);
            destino.CajaMinMax(Vector3.Min(q0, q1), Vector3.Max(q0, q1), color, contorno);
        }

        // Cortes a lo largo de la pared: los bordes de cada hueco.
        var cortes = new List<float> { inicio, fin };
        foreach (var h in huecos) { cortes.Add(h.a0); cortes.Add(h.a1); }
        cortes.Sort();

        for (int i = 0; i < cortes.Count - 1; i++)
        {
            float c0 = cortes[i], c1 = cortes[i + 1];
            if (c1 - c0 < 0.001f)
                continue;
            var tramos = new List<Vector2> { new Vector2(0f, alto) };
            foreach (var h in huecos)
            {
                if (h.a0 > c0 + 0.001f || h.a1 < c1 - 0.001f)
                    continue;
                var nuevos = new List<Vector2>();
                foreach (var t in tramos)
                {
                    if (h.y1 <= t.x || h.y0 >= t.y) { nuevos.Add(t); continue; }
                    if (h.y0 > t.x) nuevos.Add(new Vector2(t.x, h.y0));
                    if (h.y1 < t.y) nuevos.Add(new Vector2(h.y1, t.y));
                }
                tramos = nuevos;
            }
            // Los pedazos de pared van sin contorno para que no se vean líneas en las uniones.
            foreach (var t in tramos)
                Bloque(c0, c1, t.x, t.y, fijo - mitadGrosor, fijo + mitadGrosor, pared, false, kit);
        }

        // Marcos (con contorno) y vidrios.
        const float b = 0.03f;
        float pm0 = fijo - mitadGrosor - 0.03f, pm1 = fijo + mitadGrosor + 0.03f;
        foreach (var h in huecos)
        {
            float yBase = h.puerta ? 0f : h.y0;
            Bloque(h.a0 - b, h.a1 + b, h.y1 - b, h.y1 + b, pm0, pm1, marco, true, kit);
            if (!h.puerta)
                Bloque(h.a0 - b, h.a1 + b, h.y0 - b, h.y0 + b, pm0, pm1, marco, true, kit);
            Bloque(h.a0 - b, h.a0 + b, yBase, h.y1, pm0, pm1, marco, true, kit);
            Bloque(h.a1 - b, h.a1 + b, yBase, h.y1, pm0, pm1, marco, true, kit);

            float medio = (h.a0 + h.a1) / 2f;
            if (h.puerta || h.a1 - h.a0 > 1.2f)
                Bloque(medio - b, medio + b, yBase, h.y1, pm0, pm1, marco, true, kit);
            if (h.puerta)
            {
                // Manijas de la puerta de vidrio.
                foreach (float lado in new[] { -1f, 1f })
                    Bloque(medio + lado * 0.1f - 0.015f, medio + lado * 0.1f + 0.015f, 0.9f, 1.3f,
                        fijo - mitadGrosor - 0.08f, fijo + mitadGrosor + 0.08f, grisOscuro, true, kit);
            }

            Bloque(h.a0, h.a1, yBase, h.y1, fijo - 0.005f, fijo + 0.005f, Color.white, false, vidrio);
        }
    }

    // Mancha de sol en el piso y rayo de luz entre la ventana y la mancha.
    static void SolPorHueco(KitMalla luz, bool enX, float planoInterior, Hueco h, Vector3 dirLuz)
    {
        // Solo si el sol entra por esta pared (la luz va hacia adentro).
        Vector3 haciaAdentro = enX ? new Vector3(-Mathf.Sign(planoInterior), 0f, 0f) : new Vector3(0f, 0f, -Mathf.Sign(planoInterior));
        if (Vector3.Dot(dirLuz, haciaAdentro) <= 0.05f || dirLuz.y >= -0.05f)
            return;
        // Las ventanas altas quedan sobre los estantes: su luz pegaría en los muebles, así que no se dibuja.
        if (!h.puerta && h.y0 > 1.5f)
            return;

        float yBase = h.puerta ? 0.05f : h.y0;
        // Las ventanas anchas tienen parteluz: dos manchas separadas.
        var paneles = new List<Vector2>();
        float medio = (h.a0 + h.a1) / 2f;
        if (h.puerta || h.a1 - h.a0 > 1.2f)
        {
            paneles.Add(new Vector2(h.a0 + 0.03f, medio - 0.03f));
            paneles.Add(new Vector2(medio + 0.03f, h.a1 - 0.03f));
        }
        else
        {
            paneles.Add(new Vector2(h.a0 + 0.03f, h.a1 - 0.03f));
        }

        const float yPiso = 0.015f;
        Vector3 Punto(float a, float y) => enX ? new Vector3(planoInterior, y, a) : new Vector3(a, y, planoInterior);
        Vector3 Proyectar(Vector3 p)
        {
            float t = (p.y - yPiso) / -dirLuz.y;
            Vector3 f = p + dirLuz * t;
            f.y = yPiso;
            return f;
        }

        Color mancha = new Color(1f, 1f, 1f, 0.38f);
        Color rayoVentana = new Color(1f, 1f, 1f, 0.10f);
        Color rayoPiso = new Color(1f, 1f, 1f, 0.02f);

        foreach (var panel in paneles)
        {
            Vector3 w00 = Punto(panel.x, yBase), w10 = Punto(panel.y, yBase);
            Vector3 w11 = Punto(panel.y, h.y1 - 0.03f), w01 = Punto(panel.x, h.y1 - 0.03f);
            Vector3 f00 = Proyectar(w00), f10 = Proyectar(w10), f11 = Proyectar(w11), f01 = Proyectar(w01);

            luz.CuadroColores(f00, f10, f11, f01, mancha, mancha, mancha, mancha);
            luz.CuadroColores(w00, w10, f10, f00, rayoVentana, rayoVentana, rayoPiso, rayoPiso);
            luz.CuadroColores(w01, w11, f11, f01, rayoVentana, rayoVentana, rayoPiso, rayoPiso);
            luz.CuadroColores(w00, w01, f01, f00, rayoVentana, rayoVentana, rayoPiso, rayoPiso);
            luz.CuadroColores(w10, w11, f11, f10, rayoVentana, rayoVentana, rayoPiso, rayoPiso);
        }
    }

    // ================= Exterior =================

    // El exterior usa el shader "realista" (sin contorno, con brillo del sol). El cielo es el de Unity.
    internal static void ConstruirExterior(Transform raiz, Material matExterior, string carpeta)
    {
        var ext = new KitMalla();
        var rnd = new System.Random(2026);
        ext.brillo = 0.2f; // casi todo es mate; abajo se sube el brillo del asfalto, vidrios y carros

        // Suelo: pasto lejano, asfalto de las calles y las manzanas (andenes) 12 cm más arriba.
        ext.CajaMinMax(new Vector3(-400f, -0.5f, -400f), new Vector3(400f, -0.2f, 400f), pasto, false);
        ext.brillo = 0.6f; // el asfalto refleja el sol
        ext.CajaMinMax(new Vector3(-70f, -0.25f, -70f), new Vector3(70f, -0.13f, 70f), asfalto, false);
        ext.brillo = 0.35f;
        Manzana(ext, -7f, 70f, -8f, 70f);    // la de la farmacia
        Manzana(ext, -7f, 70f, -70f, -16f);  // al frente, cruzando la calle
        Manzana(ext, -70f, -13f, -8f, 70f);  // cruzando la carrera
        Manzana(ext, -70f, -13f, -70f, -16f);

        // Líneas de la calle (al frente) y de la carrera (al lado oeste), sin pintar en el cruce.
        for (float x = -69f; x < 69f; x += 3f)
            if (x + 1.5f < -13f || x > -7f)
                ext.Piso(new Vector3(x + 0.75f, -0.125f, -12f), 0.75f, 0.06f, amarillo);
        for (float z = -69f; z < 69f; z += 3f)
            if (z + 1.5f < -16f || z > -8f)
                ext.Piso(new Vector3(-10f, -0.125f, z + 0.75f), 0.06f, 0.75f, amarillo);
        // Cebras
        for (float z = -15.6f; z <= -8.3f; z += 0.9f)
            ext.Piso(new Vector3(-4.7f, -0.124f, z), 1.5f, 0.22f, blanco);
        for (float x = -12.6f; x <= -7.3f; x += 0.9f)
            ext.Piso(new Vector3(x, -0.124f, -5f), 0.22f, 1.5f, blanco);

        // Parqueadero al lado este de la farmacia.
        ext.brillo = 0.55f;
        ext.CajaMinMax(new Vector3(5.4f, -0.02f, -7.4f), new Vector3(24f, 0.005f, 14f), asfaltoParqueo, false);
        ext.brillo = 0.35f;
        float[] lineas = { -6f, -3.5f, -1f, 1.5f, 4f, 6.5f, 9f, 11.5f };
        foreach (float xFila in new[] { 10f, 18.5f })
        {
            foreach (float z in lineas)
                ext.Piso(new Vector3(xFila, 0.01f, z), 2.5f, 0.05f, blanco);
            for (int i = 0; i < lineas.Length - 1; i++)
            {
                if (rnd.NextDouble() > 0.65)
                    continue;
                Vector3 puesto = new Vector3(xFila, 0.005f, (lineas[i] + lineas[i + 1]) / 2f);
                Vector3 dir = rnd.NextDouble() < 0.5 ? Vector3.right : Vector3.left;
                if (rnd.NextDouble() < 0.15)
                    Taxi(ext, puesto, dir, raiz);
                else
                    Familiar(ext, puesto, dir, Elegir(rnd, coloresFamiliar));
            }
        }

        ext.brillo = 0.2f;

        // Carros y bus en la calle.
        // En Colombia se maneja por la derecha: hacia +X por el carril sur, hacia -X por el carril norte.
        Taxi(ext, new Vector3(-2f, -0.13f, -10f), Vector3.left, raiz);
        Taxi(ext, new Vector3(-25f, -0.13f, -14f), Vector3.right, raiz);
        Familiar(ext, new Vector3(16f, -0.13f, -14f), Vector3.right, coloresFamiliar[1]);
        Familiar(ext, new Vector3(-18f, -0.13f, -10f), Vector3.left, coloresFamiliar[0]);
        Familiar(ext, new Vector3(24f, -0.13f, -10f), Vector3.left, coloresFamiliar[2]);
        Familiar(ext, new Vector3(-9f, -0.13f, 6f), Vector3.forward, coloresFamiliar[3]);
        Familiar(ext, new Vector3(-11f, -0.13f, -24f), Vector3.back, coloresFamiliar[4]);
        Bus(ext, new Vector3(5f, -0.13f, -14f), raiz);
        ext.brillo = 0.2f; // edificios, árboles y cerros: mate

        // Edificios cercanos (ladrillo bogotano, con ventanas y placas de concreto).
        Edificio(ext, rnd, new Vector3(-6.5f, 0f, 5.3f), new Vector3(6.5f, 15f, 17f), Elegir(rnd, ladrillos));  // vecino de atrás
        Edificio(ext, rnd, new Vector3(8f, 0f, 15f), new Vector3(24f, 18f, 26f), Elegir(rnd, ladrillos));       // detrás del parqueadero
        Edificio(ext, rnd, new Vector3(25f, 0f, -6f), new Vector3(36f, 12f, 14f), concreto);                   // al este del parqueadero
        float[][] frente = { new[] { -6.5f, 2f, 15f }, new[] { 2.5f, 11f, 18f }, new[] { 11.5f, 20f, 12f }, new[] { 20.5f, 30f, 15f } };
        foreach (var e in frente)
        {
            Edificio(ext, rnd, new Vector3(e[0], 0f, -30f), new Vector3(e[1], e[2], -19f), Elegir(rnd, ladrillos));
            Toldo(ext, rnd, new Vector3((e[0] + e[1]) / 2f, 2.6f, -18.6f), e[1] - e[0] - 1f, false);
        }
        float[][] carrera = { new[] { -7f, 2f, 12f }, new[] { 2.5f, 10f, 15f }, new[] { 10.5f, 20f, 18f }, new[] { 20.5f, 30f, 12f } };
        foreach (var e in carrera)
        {
            Edificio(ext, rnd, new Vector3(-28f, 0f, e[0]), new Vector3(-16f, e[2], e[1]), Elegir(rnd, ladrillos));
            Toldo(ext, rnd, new Vector3(-15.6f, 2.6f, (e[0] + e[1]) / 2f), e[1] - e[0] - 1f, true);
        }
        Edificio(ext, rnd, new Vector3(-28f, 0f, -30f), new Vector3(-16f, 21f, -19f), Elegir(rnd, ladrillos));

        // Edificios de fondo (más claros, como vistos a través del aire).
        for (int i = 0; i < 34; i++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float dist = Rango(rnd, 48f, 100f);
            Vector3 c = new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
            float ancho = Rango(rnd, 8f, 18f), fondo = Rango(rnd, 8f, 18f), alto = Rango(rnd, 12f, 48f);
            Color color = Color.Lerp(rnd.NextDouble() < 0.6 ? Elegir(rnd, ladrillos) : concreto, new Color(0.75f, 0.82f, 0.9f), 0.35f);
            Vector3 min = c - new Vector3(ancho / 2f, 0f, fondo / 2f);
            Vector3 max = c + new Vector3(ancho / 2f, alto, fondo / 2f);
            ext.CajaMinMax(min, max, color);
            // Franjas de ventanas en la cara que mira hacia la farmacia.
            Vector3 haciaCentro = -c.normalized;
            Vector3 normal = Mathf.Abs(haciaCentro.x) > Mathf.Abs(haciaCentro.z)
                ? new Vector3(Mathf.Sign(haciaCentro.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(haciaCentro.z));
            float mitadCara = Mathf.Abs(normal.x) > 0 ? fondo / 2f : ancho / 2f;
            float offset = Mathf.Abs(normal.x) > 0 ? ancho / 2f : fondo / 2f;
            for (float y = 4f; y < alto - 2f; y += 3f)
                ext.Etiqueta(c + normal * (offset + 0.02f) + Vector3.up * y, normal, 0.55f, mitadCara * 0.8f,
                    Color.Lerp(vidrioOscuro, new Color(0.75f, 0.82f, 0.9f), 0.35f));
        }

        // Árboles en andenes y parqueadero, y postes de luz.
        foreach (float x in new[] { -5.8f, 7f, 13f, 19f, 25f }) Arbol(ext, rnd, new Vector3(x, 0f, -7f));
        foreach (float x in new[] { -5f, 1f, 7f, 13f, 19f, 25f }) Arbol(ext, rnd, new Vector3(x, 0f, -17.3f));
        foreach (float z in new[] { -2f, 4f, 10f, 16f }) Arbol(ext, rnd, new Vector3(-6.3f, 0f, z));
        foreach (float z in new[] { -4f, 3f, 10f, 17f }) Arbol(ext, rnd, new Vector3(-14.3f, 0f, z));
        foreach (float z in new[] { -5f, 3f, 11f }) Arbol(ext, rnd, new Vector3(14.25f, 0f, z));
        foreach (float x in new[] { -6.2f, 6.5f, 15f, 24f }) Poste(ext, new Vector3(x, 0f, -7.7f), -1f);

        // Cerros orientales al fondo (al este).
        float[][] cerros =
        {
            new[] { 330f, -260f, 170f, 150f }, new[] { 360f, -90f, 200f, 190f }, new[] { 340f, 80f, 180f, 170f },
            new[] { 370f, 250f, 210f, 160f }, new[] { 420f, 0f, 260f, 230f }, new[] { 250f, 380f, 160f, 110f },
        };
        foreach (var c in cerros)
        {
            float lejania = Mathf.InverseLerp(300f, 450f, c[0]);
            Color verde = Color.Lerp(new Color(0.34f, 0.52f, 0.36f), new Color(0.50f, 0.64f, 0.62f), lejania);
            ext.Esfera(new Vector3(c[0], -30f, c[1]), new Vector3(c[2], c[3], c[2] * 0.9f), 2, verde);
        }

        ext.CrearObjeto("Kit_Exterior", raiz, matExterior, carpeta);

        // Piso sólido afuera (el exterior es solo visual): sin esto el jugador se cae al vacío,
        // porque el sistema de movimiento de Meta le aplica gravedad.
        var pisoExterior = new GameObject("PisoExterior");
        pisoExterior.transform.SetParent(raiz, false);
        var caja = pisoExterior.AddComponent<BoxCollider>();
        caja.center = new Vector3(0f, -0.26f, 0f); // la parte de arriba queda en y = -0.01 (nivel del andén)
        caja.size = new Vector3(140f, 0.5f, 140f);
    }

    // ---------- Piezas del exterior ----------

    static void Manzana(KitMalla k, float x0, float x1, float z0, float z1)
    {
        k.CajaMinMax(new Vector3(x0, -0.25f, z0), new Vector3(x1, -0.01f, z1), acera);
    }

    static void Edificio(KitMalla k, System.Random rnd, Vector3 min, Vector3 max, Color color)
    {
        k.CajaMinMax(min, max, color);
        float alto = max.y - min.y;
        Vector3 centro = (min + max) / 2f;
        Vector3 tam = max - min;

        // Placas de concreto entre pisos (cada 3 m), un poco salidas.
        for (float y = min.y + 3f; y < max.y - 0.5f; y += 3f)
            k.CajaMinMax(new Vector3(min.x - 0.06f, y - 0.12f, min.z - 0.06f), new Vector3(max.x + 0.06f, y + 0.12f, max.z + 0.06f), concreto, false);
        // Remate del techo
        k.CajaMinMax(new Vector3(min.x - 0.1f, max.y, min.z - 0.1f), new Vector3(max.x + 0.1f, max.y + 0.4f, max.z + 0.1f), concreto);

        // Ventanas en las cuatro caras (desde el segundo piso).
        foreach (var normal in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
        {
            bool enX = Mathf.Abs(normal.x) > 0;
            float largo = enX ? tam.z : tam.x;
            float mitad = enX ? tam.x / 2f : tam.z / 2f;
            int columnas = Mathf.Max(1, Mathf.FloorToInt(largo / 2.2f));
            float paso = largo / columnas;
            for (float y = min.y + 4.5f; y < max.y - 1f; y += 3f)
            {
                for (int c = 0; c < columnas; c++)
                {
                    float a = -largo / 2f + paso * (c + 0.5f);
                    Vector3 p = centro + normal * (mitad + 0.02f) + (enX ? Vector3.forward : Vector3.right) * a;
                    p.y = y;
                    float brilloAntes = k.brillo;
                    k.brillo = 1f; // las ventanas reflejan el sol
                    k.Etiqueta(p, normal, 0.7f, Mathf.Min(0.65f, paso * 0.35f), vidrioOscuro);
                    k.brillo = brilloAntes;
                }
            }
        }
    }

    static void Toldo(KitMalla k, System.Random rnd, Vector3 centro, float ancho, bool enX)
    {
        Color[] colores = { new Color(0.85f, 0.2f, 0.2f), new Color(0.15f, 0.55f, 0.35f), new Color(0.95f, 0.65f, 0.15f), new Color(0.2f, 0.45f, 0.8f) };
        Color color = Elegir(rnd, colores);
        Vector3 tam = enX ? new Vector3(0.8f, 0.25f, ancho) : new Vector3(ancho, 0.25f, 0.8f);
        k.Caja(centro, tam, color);
        // Vitrina oscura de la tienda en el primer piso.
        Vector3 normal = enX ? Vector3.right : Vector3.forward;
        Vector3 cara = centro - normal * 0.38f + Vector3.down * 1.35f;
        k.Etiqueta(cara, normal, 1.1f, ancho * 0.45f, vidrioOscuro);
    }

    static void Arbol(KitMalla k, System.Random rnd, Vector3 base0)
    {
        bool alto = rnd.NextDouble() < 0.35; // eucalipto / urapán más alto
        float altoTronco = alto ? 3.2f : 2.0f;
        k.Caja(base0 + Vector3.up * (altoTronco / 2f), new Vector3(0.25f, altoTronco, 0.25f), tronco);
        float s = Rango(rnd, 0.85f, 1.2f);
        Vector3 radios = alto ? new Vector3(1.2f, 2.3f, 1.2f) * s : new Vector3(1.5f, 1.3f, 1.5f) * s;
        k.Esfera(base0 + Vector3.up * (altoTronco + radios.y * 0.7f), radios, 1, Elegir(rnd, verdes));
    }

    static void Poste(KitMalla k, Vector3 base0, float haciaZ)
    {
        k.Cilindro(base0 + Vector3.up * 2.5f, 0.06f, 5f, Vector3.up, 6, grisOscuro);
        k.Caja(base0 + new Vector3(0f, 4.95f, haciaZ * 0.5f), new Vector3(0.08f, 0.08f, 1.0f), grisOscuro);
        k.Caja(base0 + new Vector3(0f, 4.88f, haciaZ * 1.0f), new Vector3(0.25f, 0.1f, 0.45f), grisClaro);
    }

    // ---------- Carros (estilo de las referencias: low poly, cabina en trapecio) ----------

    static readonly Color vidrioCarro = new Color(0.27f, 0.33f, 0.42f);
    static readonly Color negroCarro = new Color(0.12f, 0.13f, 0.15f);
    static readonly Color parachoques = new Color(0.20f, 0.21f, 0.24f);
    static readonly Color amarilloTaxi = new Color(0.99f, 0.80f, 0.12f);

    // Taxi amarillo de 4 puertas (sedán) con letrero "TAXI" y antena.
    static void Taxi(KitMalla k, Vector3 piso, Vector3 dir, Transform raiz)
    {
        var a = new ArmadorCarro(k, piso, dir, 4.4f, 1.75f);
        a.Carroceria(amarilloTaxi, 0.80f, 0.86f);
        // Cabina: parabrisas inclinado adelante, vidrio trasero inclinado atrás (sedán).
        a.Cabina(amarilloTaxi, -1.05f, 1.0f, -0.75f, 0.35f, 0.84f, 1.42f, 0.82f, 0.70f);
        a.VentanasLaterales(new[] { new Vector2(0.10f, 0.48f), new Vector2(0.52f, 0.88f) });
        a.LineasDePuertas(new[] { 0.95f, 0.0f, -1.0f }, new[] { 0.55f, -0.42f });
        a.Frente();
        a.Atras();
        a.Ruedas();

        // Letrero de techo con cuadros negros, y antena.
        a.CajaLocal(-0.32f, -0.02f, 1.42f, 1.62f, -0.42f, 0.42f, amarilloTaxi);
        foreach (float lado in new[] { -1f, 1f })
            for (int fila = 0; fila < 2; fila++)
                for (int col = 0; col < 2; col++)
                {
                    if ((fila + col) % 2 == 1)
                        continue;
                    float z0 = lado * (0.29f + col * 0.05f), z1 = z0 + lado * 0.05f;
                    float y0 = 1.47f + fila * 0.05f, y1 = y0 + 0.05f;
                    a.CuadroFrontal(-0.02f + 0.003f, Mathf.Min(z0, z1), Mathf.Max(z0, z1), y0, y1, negroCarro);
                }
        a.CajaLocal(-0.66f, -0.63f, 1.40f, 1.88f, -0.62f, -0.59f, negroCarro);

        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            return;
        var textos = new GameObject("TextosTaxi");
        textos.transform.SetParent(raiz, false);
        TextoCarro(textos.transform, "TAXI", a.Mundo(-0.02f + 0.006f, 1.53f, 0f), Quaternion.LookRotation(-dir, Vector3.up), new Vector2(0.42f, 0.15f));
        TextoCarro(textos.transform, "TAXI", a.Mundo(-0.32f - 0.006f, 1.53f, 0f), Quaternion.LookRotation(dir, Vector3.up), new Vector2(0.42f, 0.15f));
    }

    // Carro familiar (station wagon): techo largo hasta atrás y portón trasero casi vertical.
    static void Familiar(KitMalla k, Vector3 piso, Vector3 dir, Color color)
    {
        var a = new ArmadorCarro(k, piso, dir, 4.5f, 1.75f);
        a.Carroceria(color, 0.80f, 0.86f);
        a.Cabina(color, -2.15f, 0.95f, -2.08f, 0.2f, 0.84f, 1.45f, 0.82f, 0.72f);
        a.VentanasLaterales(new[] { new Vector2(0.04f, 0.32f), new Vector2(0.36f, 0.62f), new Vector2(0.66f, 0.92f) });
        a.LineasDePuertas(new[] { 0.9f, -0.15f, -1.2f }, new[] { 0.5f, -0.55f });
        a.Frente();
        a.Atras();
        a.Ruedas();
    }

    static void TextoCarro(Transform padre, string texto, Vector3 posicion, Quaternion rotacion, Vector2 tamano)
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
        tmp.color = new Color(0.1f, 0.1f, 0.12f);
        tmp.rectTransform.sizeDelta = tamano;
        go.transform.SetPositionAndRotation(posicion, rotacion);
    }

    // Arma un carro en coordenadas "del carro": X = hacia adelante, Y = arriba, Z = hacia un lado.
    // Funciona en cualquier dirección (dir).
    class ArmadorCarro
    {
        readonly KitMalla k;
        readonly Vector3 piso, dir, lado;
        readonly float hx, hz;
        float xr0, xf0, xr1, xf1, yb, yt, wb, wt;
        Color color;

        public ArmadorCarro(KitMalla k, Vector3 piso, Vector3 dir, float largo, float ancho)
        {
            this.k = k;
            k.brillo = 0.8f; // pintura de carro brillante
            this.piso = piso;
            this.dir = dir.normalized;
            lado = Vector3.Cross(this.dir, Vector3.up);
            hx = largo / 2f;
            hz = ancho / 2f;
        }

        public Vector3 Mundo(float x, float y, float z) => piso + dir * x + Vector3.up * y + lado * z;

        public void CajaLocal(float x0, float x1, float y0, float y1, float z0, float z1, Color c, bool contorno = true)
        {
            k.Hexaedro(new[]
            {
                Mundo(x0, y0, z0), Mundo(x1, y0, z0), Mundo(x1, y0, z1), Mundo(x0, y0, z1),
                Mundo(x0, y1, z0), Mundo(x1, y1, z0), Mundo(x1, y1, z1), Mundo(x0, y1, z1),
            }, c, contorno);
        }

        // Rectángulo en un plano frontal (mirando hacia adelante) en x.
        public void CuadroFrontal(float x, float z0, float z1, float y0, float y1, Color c)
        {
            k.CuadroLibre(Mundo(x, y0, z0), Mundo(x, y0, z1), Mundo(x, y1, z1), Mundo(x, y1, z0), dir, c);
        }

        void CuadroTrasero(float x, float z0, float z1, float y0, float y1, Color c)
        {
            k.CuadroLibre(Mundo(x, y0, z0), Mundo(x, y0, z1), Mundo(x, y1, z1), Mundo(x, y1, z0), -dir, c);
        }

        // Rectángulo en el costado (s = -1 o 1).
        // "capa" separa un poquito las piezas que se enciman (líneas de puertas sobre los pasos de rueda).
        void CuadroLateral(float s, float x0, float x1, float y0, float y1, Color c, float capa = 0.003f)
        {
            float z = s * (hz + capa);
            k.CuadroLibre(Mundo(x0, y0, z), Mundo(x1, y0, z), Mundo(x1, y1, z), Mundo(x0, y1, z), lado * s, c);
        }

        // Parte de abajo: caja con el capó un poco más bajo adelante.
        public void Carroceria(Color c, float altoFrente, float altoAtras)
        {
            color = c;
            k.Hexaedro(new[]
            {
                Mundo(-hx, 0.30f, -hz), Mundo(hx, 0.30f, -hz), Mundo(hx, 0.30f, hz), Mundo(-hx, 0.30f, hz),
                Mundo(-hx, altoAtras, -hz), Mundo(hx, altoFrente, -hz), Mundo(hx, altoFrente, hz), Mundo(-hx, altoAtras, hz),
            }, c);
        }

        // Cabina en trapecio: más angosta y corta arriba que abajo.
        public void Cabina(Color c, float atrasAbajo, float frenteAbajo, float atrasArriba, float frenteArriba,
            float altoBase, float altoTecho, float anchoBase, float anchoTecho)
        {
            xr0 = atrasAbajo; xf0 = frenteAbajo; xr1 = atrasArriba; xf1 = frenteArriba;
            yb = altoBase; yt = altoTecho; wb = anchoBase; wt = anchoTecho;
            k.Hexaedro(new[]
            {
                Mundo(xr0, yb, -wb), Mundo(xf0, yb, -wb), Mundo(xf0, yb, wb), Mundo(xr0, yb, wb),
                Mundo(xr1, yt, -wt), Mundo(xf1, yt, -wt), Mundo(xf1, yt, wt), Mundo(xr1, yt, wt),
            }, c);

            // Parabrisas y vidrio trasero
            CaraVentana(Mundo(xf0, yb, -wb), Mundo(xf0, yb, wb), Mundo(xf1, yt, wt), Mundo(xf1, yt, -wt), 0.06f, 0.94f, 0.08f, 0.9f, dir + Vector3.up);
            CaraVentana(Mundo(xr0, yb, wb), Mundo(xr0, yb, -wb), Mundo(xr1, yt, -wt), Mundo(xr1, yt, wt), 0.08f, 0.92f, 0.12f, 0.88f, -dir + Vector3.up);
            // Espejos
            foreach (float s in new[] { -1f, 1f })
                CajaLocal(xf0 - 0.18f, xf0 - 0.04f, yb + 0.06f, yb + 0.16f, s * (wb + 0.02f), s * (wb + 0.12f), c);
        }

        // Ventanas en los costados de la cabina; cada Vector2 es (inicio, fin) de 0 (atrás) a 1 (adelante).
        public void VentanasLaterales(Vector2[] tramos)
        {
            foreach (float s in new[] { -1f, 1f })
            {
                Vector3 p0 = Mundo(xr0, yb, s * wb), p1 = Mundo(xf0, yb, s * wb);
                Vector3 p2 = Mundo(xf1, yt, s * wt), p3 = Mundo(xr1, yt, s * wt);
                foreach (var t in tramos)
                    CaraVentana(p0, p1, p2, p3, t.x, t.y, 0.12f, 0.9f, lado * s);
            }
        }

        // Ventana dentro de una cara de 4 esquinas (u = a lo ancho, v = de abajo hacia arriba).
        void CaraVentana(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u0, float u1, float v0, float v1, Vector3 afuera)
        {
            Vector3 P(float u, float v) => Vector3.Lerp(Vector3.Lerp(p0, p1, u), Vector3.Lerp(p3, p2, u), v);
            Vector3 n = afuera.normalized * 0.004f;
            float brilloAntes = k.brillo;
            k.brillo = 1f;
            k.CuadroLibre(P(u0, v0) + n, P(u1, v0) + n, P(u1, v1) + n, P(u0, v1) + n, afuera, vidrioCarro);
            k.brillo = brilloAntes;
        }

        public void LineasDePuertas(float[] xs, float[] manijas)
        {
            foreach (float s in new[] { -1f, 1f })
            {
                foreach (float x in xs)
                    CuadroLateral(s, x - 0.008f, x + 0.008f, 0.38f, 0.82f, negroCarro, 0.006f);
                foreach (float x in manijas)
                    CuadroLateral(s, x - 0.07f, x + 0.07f, 0.70f, 0.73f, negroCarro, 0.006f);
                // Direccional lateral naranja adelante
                CuadroLateral(s, hx - 0.22f, hx - 0.08f, 0.64f, 0.70f, new Color(1f, 0.55f, 0.1f));
            }
        }

        public void Frente()
        {
            float x = hx + 0.003f;
            CuadroFrontal(x, -0.36f, 0.36f, 0.56f, 0.74f, negroCarro);           // rejilla
            foreach (float s in new[] { -1f, 1f })
            {
                CuadroFrontal(x, Mathf.Min(s * 0.42f, s * 0.72f), Mathf.Max(s * 0.42f, s * 0.72f), 0.58f, 0.73f, new Color(0.95f, 0.95f, 0.9f));
                CuadroFrontal(x, Mathf.Min(s * 0.74f, s * 0.85f), Mathf.Max(s * 0.74f, s * 0.85f), 0.58f, 0.73f, new Color(1f, 0.55f, 0.1f));
            }
            CajaLocal(hx, hx + 0.12f, 0.30f, 0.48f, -hz - 0.03f, hz + 0.03f, parachoques);
            CuadroFrontal(hx + 0.123f, -0.22f, 0.22f, 0.33f, 0.45f, new Color(0.95f, 0.95f, 0.95f)); // placa
        }

        public void Atras()
        {
            float x = -hx - 0.003f;
            foreach (float s in new[] { -1f, 1f })
                CuadroTrasero(x, Mathf.Min(s * 0.52f, s * 0.84f), Mathf.Max(s * 0.52f, s * 0.84f), 0.6f, 0.76f, new Color(0.85f, 0.22f, 0.15f));
            CajaLocal(-hx - 0.12f, -hx, 0.30f, 0.48f, -hz - 0.03f, hz + 0.03f, parachoques);
            CuadroTrasero(-hx - 0.123f, -0.22f, 0.22f, 0.33f, 0.45f, new Color(0.95f, 0.95f, 0.95f));
        }

        public void Ruedas()
        {
            foreach (float x in new[] { -hx * 0.62f, hx * 0.62f })
                foreach (float s in new[] { -1f, 1f })
                {
                    CuadroLateral(s, x - 0.42f, x + 0.42f, 0.30f, 0.66f, negroCarro);   // paso de rueda
                    k.Cilindro(Mundo(x, 0.31f, s * (hz - 0.07f)), 0.31f, 0.2f, lado, 10, negroCarro);
                    k.Cilindro(Mundo(x, 0.31f, s * (hz + 0.045f)), 0.14f, 0.03f, lado, 10, new Color(0.55f, 0.56f, 0.58f));
                }
        }
    }

    // Bus del SITP (estilo de la referencia): azul, puertas dobles de vidrio, ventanas con marco negro,
    // letrero "SITP" arriba del parabrisas y en el costado, espejos, faros redondos y direccionales naranja.
    // El frente mira hacia +X.
    static void Bus(KitMalla k, Vector3 piso, Transform raiz)
    {
        k.brillo = 0.8f;
        Color azul = new Color(0.13f, 0.45f, 0.88f);
        Color negro = new Color(0.08f, 0.09f, 0.10f);
        Color vidrio = new Color(0.27f, 0.36f, 0.48f);
        Color naranja = new Color(1f, 0.55f, 0.1f);
        Color gris = new Color(0.55f, 0.56f, 0.58f);
        const float L = 11f, W = 2.5f, yBajo = 0.35f, yAlto = 3.15f;
        float hx = L / 2f, hz = W / 2f;
        Vector3 P(float x, float y, float z) => piso + new Vector3(x, y, z);

        // Carrocería, ducto del techo y parachoques
        k.CajaMinMax(P(-hx, yBajo, -hz), P(hx, yAlto, hz), azul);
        k.CajaMinMax(P(-0.6f, yAlto, -0.6f), P(1.6f, yAlto + 0.16f, 0.6f), negro);
        k.CajaMinMax(P(hx, 0.38f, -hz + 0.05f), P(hx + 0.12f, 0.72f, hz - 0.05f), azul);

        // Costados: ventanas y puertas (en los dos lados)
        float[][] ventanas = { new[] { -5.3f, -3.6f }, new[] { -2.4f, -0.4f }, new[] { 0.8f, 3.2f }, new[] { 4.7f, 5.3f } };
        float[][] puertas = { new[] { -3.5f, -2.5f }, new[] { -0.3f, 0.7f }, new[] { 3.3f, 4.6f } };
        foreach (float s in new[] { -1f, 1f })
        {
            Vector3 n = new Vector3(0f, 0f, s);
            float zMarco = s * (hz + 0.002f), zVidrio = s * (hz + 0.004f);
            foreach (var v in ventanas)
            {
                float cx = (v[0] + v[1]) / 2f, mx = (v[1] - v[0]) / 2f;
                k.Etiqueta(P(cx, 2.35f, zMarco), n, 0.58f, mx + 0.06f, negro);
                k.Etiqueta(P(cx, 2.35f, zVidrio), n, 0.5f, mx - 0.02f, vidrio);
            }
            foreach (var d in puertas)
            {
                float cx = (d[0] + d[1]) / 2f, mx = (d[1] - d[0]) / 2f;
                k.Etiqueta(P(cx, 1.68f, zMarco), n, 1.3f, mx + 0.06f, negro);
                float hoja = mx / 2f - 0.04f;
                k.Etiqueta(P(cx - mx / 2f, 1.68f, zVidrio), n, 1.22f, hoja, vidrio);
                k.Etiqueta(P(cx + mx / 2f, 1.68f, zVidrio), n, 1.22f, hoja, vidrio);
            }
            // Pasos de rueda negros, llantas y rines
            foreach (float x in new[] { -1.6f, 2.2f })
            {
                k.Etiqueta(P(x, 0.72f, zMarco), n, 0.38f, 0.72f, negro);
                k.Cilindro(P(x, 0.5f, s * (hz - 0.165f)), 0.5f, 0.35f, Vector3.forward, 10, negro);
                k.Cilindro(P(x, 0.5f, s * (hz + 0.03f)), 0.24f, 0.04f, Vector3.forward, 10, gris);
            }
            // Espejos: brazo hacia adelante, brazo hacia afuera y espejo
            k.CajaMinMax(P(hx, 2.72f, s * (hz - 0.05f) - 0.03f), P(hx + 0.35f, 2.78f, s * (hz - 0.05f) + 0.03f), negro);
            float zEsp = s * (hz + 0.28f);
            k.CajaMinMax(P(hx + 0.32f, 2.72f, Mathf.Min(s * (hz - 0.05f), zEsp)), P(hx + 0.38f, 2.78f, Mathf.Max(s * (hz - 0.05f), zEsp)), negro);
            k.CajaMinMax(P(hx + 0.28f, 2.05f, zEsp - 0.1f), P(hx + 0.42f, 2.75f, zEsp + 0.1f), negro);
        }

        // Frente: parabrisas partido, letrero, rejilla, placa, faros y direccionales
        Vector3 fr = Vector3.right;
        float xMarco = hx + 0.002f, xVidrio = hx + 0.004f;
        k.Etiqueta(P(xMarco, 2.05f, 0f), fr, 0.72f, 1.17f, negro);
        k.Etiqueta(P(xVidrio, 2.05f, -0.58f), fr, 0.66f, 0.54f, vidrio);
        k.Etiqueta(P(xVidrio, 2.05f, 0.58f), fr, 0.66f, 0.54f, vidrio);
        k.Etiqueta(P(xMarco, 2.96f, 0f), fr, 0.15f, 0.9f, negro);
        k.Etiqueta(P(xMarco, 1.08f, 0f), fr, 0.03f, 0.4f, negro);
        k.Etiqueta(P(hx + 0.122f, 0.52f, 0f), fr, 0.08f, 0.3f, blanco);
        foreach (float s in new[] { -1f, 1f })
        {
            foreach (float z in new[] { 0.62f, 0.85f })
                k.Cilindro(P(hx + 0.02f, 0.9f, s * z), 0.09f, 0.04f, Vector3.right, 10, blanco);
            k.CajaMinMax(P(hx, 0.82f, s * 1.08f - 0.06f), P(hx + 0.03f, 0.98f, s * 1.08f + 0.06f), naranja);
            k.CajaMinMax(P(hx + 0.12f, 0.5f, s * 1.0f - 0.1f), P(hx + 0.15f, 0.58f, s * 1.0f + 0.1f), naranja);
        }

        // Atrás: ventana trasera
        k.Etiqueta(P(-hx - 0.002f, 2.4f, 0f), Vector3.left, 0.45f, 1.0f, negro);

        // Textos "SITP" (frente y costados)
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            return;
        var textos = new GameObject("TextosBus");
        textos.transform.SetParent(raiz, false);
        TextoBus(textos.transform, P(hx + 0.006f, 2.96f, 0f), Quaternion.Euler(0f, -90f, 0f), new Vector2(1.6f, 0.26f));
        TextoBus(textos.transform, P(2.0f, 1.25f, hz + 0.006f), Quaternion.Euler(0f, 180f, 0f), new Vector2(1.6f, 0.4f));
        TextoBus(textos.transform, P(2.0f, 1.25f, -hz - 0.006f), Quaternion.identity, new Vector2(1.6f, 0.4f));
    }

    static void TextoBus(Transform padre, Vector3 posicion, Quaternion rotacion, Vector2 tamano)
    {
        var go = new GameObject("Texto_SITP", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "SITP";
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.1f;
        tmp.fontSizeMax = 30f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = tamano;
        go.transform.SetPositionAndRotation(posicion, rotacion);
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    static T Elegir<T>(System.Random rnd, T[] opciones) => opciones[rnd.Next(opciones.Length)];

    static float Rango(System.Random rnd, float min, float max) => min + (float)rnd.NextDouble() * (max - min);
}
#endif

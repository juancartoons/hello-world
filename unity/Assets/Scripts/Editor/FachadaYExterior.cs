#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Construye (desde el menú ★) las paredes con ventanas, los vidrios, los rayos de sol que entran
// y el exterior: calles de Bogotá, parqueadero, edificios de ladrillo, árboles, carros, bus del SITP,
// cerros orientales, nubes y cielo. Todo en pocas mallas para que la Quest 2 lo mueva sin problema.
internal static class FachadaYExterior
{
    // Fondo de la farmacia: la pared del fondo está en z = Fondo (se amplió 3 m para poder caminar alrededor del mostrador).
    internal const float Fondo = 8f;

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
        // Este y oeste: ventana grande cerca de la entrada, ventanas altas sobre los estantes
        // y otra ventana grande en la parte del fondo (la zona nueva).
        var lados = new List<Hueco>
        {
            new Hueco(-4.6f, -2.8f, 0.5f, 2.5f),
            new Hueco(-2.2f, -0.4f, 2.15f, 2.7f),
            new Hueco(0.2f, 2.0f, 2.15f, 2.7f),
            new Hueco(2.6f, 3.6f, 2.15f, 2.7f),
            new Hueco(4.4f, 7.2f, 0.5f, 2.5f),
        };

        Pared(fachada, vidrio, false, -5.0f, sur, -5.1f, 5.1f);
        Pared(fachada, vidrio, true, 5.0f, lados, -5.1f, Fondo + 0.1f);
        Pared(fachada, vidrio, true, -5.0f, lados, -5.1f, Fondo + 0.1f);

        // Esquinas, losa del techo y umbral de la puerta.
        foreach (float x in new[] { -5.05f, 5.05f })
            foreach (float z in new[] { -5.05f, Fondo + 0.05f })
                fachada.Caja(new Vector3(x, 1.625f, z), new Vector3(0.3f, 3.25f, 0.3f), pared);
        fachada.CajaMinMax(new Vector3(-5.25f, 3.1f, -5.25f), new Vector3(5.25f, 3.25f, Fondo + 0.25f), grisClaro);
        fachada.CajaMinMax(new Vector3(-0.7f, -0.12f, -5.1f), new Vector3(0.7f, 0f, -4.9f), acera, false);

        // Sol: entra por la vitrina de la entrada y por las ventanas grandes de la derecha (pared este).
        foreach (var h in sur)
            SolPorHueco(luz, false, -4.9f, h, dirLuz);
        foreach (var h in lados)
            SolPorHueco(luz, true, 4.9f, h, dirLuz);

        fachada.CrearObjeto("Kit_Fachada", raiz, matKit, carpeta);
        vidrio.CrearObjeto("Vidrios", raiz, matVidrio, carpeta);
        ConstruirPuerta(raiz, matKit, matVidrio, carpeta);
        luz.CrearObjeto("LuzDeSol", raiz, matLuz, carpeta).AddComponent<SoloDeDia>(); // de noche no entra sol
    }

    // Puerta automática de vidrio: dos hojas (marco y vidrio) que se deslizan hacia los lados, por dentro de la pared.
    static void ConstruirPuerta(Transform raiz, Material matKit, Material matVidrio, string carpeta)
    {
        const float ancho = 0.7f, alto = 2.3f, b = 0.03f;
        var marcoHoja = new KitMalla();
        var vidrioHoja = new KitMalla();
        marcoHoja.CajaMinMax(new Vector3(-ancho / 2f, 0f, -0.012f), new Vector3(ancho / 2f, b * 2f, 0.012f), marco);
        marcoHoja.CajaMinMax(new Vector3(-ancho / 2f, alto - b, -0.012f), new Vector3(ancho / 2f, alto, 0.012f), marco);
        marcoHoja.CajaMinMax(new Vector3(-ancho / 2f, 0f, -0.012f), new Vector3(-ancho / 2f + b, alto, 0.012f), marco);
        marcoHoja.CajaMinMax(new Vector3(ancho / 2f - b, 0f, -0.012f), new Vector3(ancho / 2f, alto, 0.012f), marco);
        vidrioHoja.CajaMinMax(new Vector3(-ancho / 2f + b, b * 2f, -0.004f), new Vector3(ancho / 2f - b, alto - b, 0.004f), Color.white, false);
        var mallaMarco = marcoHoja.GuardarComo($"{carpeta}/Puerta_Marco.asset");
        var mallaVidrio = vidrioHoja.GuardarComo($"{carpeta}/Puerta_Vidrio.asset");

        var puerta = new GameObject("PuertaAutomatica");
        puerta.transform.SetParent(raiz, false);
        puerta.transform.position = new Vector3(0f, 0f, -4.83f);
        Transform Hoja(string nombre, float x, float xManija)
        {
            var hoja = new GameObject(nombre);
            hoja.transform.SetParent(puerta.transform, false);
            hoja.transform.localPosition = new Vector3(x, 0f, 0f);
            var m = new GameObject("Marco");
            m.transform.SetParent(hoja.transform, false);
            m.AddComponent<MeshFilter>().sharedMesh = mallaMarco;
            m.AddComponent<MeshRenderer>().sharedMaterial = matKit;
            var v = new GameObject("Vidrio");
            v.transform.SetParent(hoja.transform, false);
            v.AddComponent<MeshFilter>().sharedMesh = mallaVidrio;
            v.AddComponent<MeshRenderer>().sharedMaterial = matVidrio;
            // Manija vertical junto al centro de la puerta
            var manija = new KitMalla();
            manija.CajaMinMax(new Vector3(xManija - 0.015f, 0.9f, -0.06f), new Vector3(xManija + 0.015f, 1.3f, 0.06f), grisOscuro);
            var mm = new GameObject("Manija");
            mm.transform.SetParent(hoja.transform, false);
            mm.AddComponent<MeshFilter>().sharedMesh = manija.GuardarComo($"{carpeta}/Puerta_{nombre}_Manija.asset");
            mm.AddComponent<MeshRenderer>().sharedMaterial = matKit;
            return hoja.transform;
        }
        var control = puerta.AddComponent<PuertaAutomatica>();
        control.hojaIzquierda = Hoja("HojaIzquierda", -ancho / 2f, ancho / 2f - 0.1f);
        control.hojaDerecha = Hoja("HojaDerecha", ancho / 2f, -ancho / 2f + 0.1f);
    }

    // Pared de 0.2 m de grosor con huecos. Si "enX" es true, la pared va a lo largo de Z en x = fijo;
    // si no, va a lo largo de X en z = fijo.
    static void Pared(KitMalla kit, KitMalla vidrio, bool enX, float fijo, List<Hueco> huecos, float inicio, float fin)
    {
        const float mitadGrosor = 0.1f, alto = 3.0f;

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

            // La puerta es automática: sus hojas de vidrio se arman aparte (ConstruirPuerta) porque se mueven.
            if (h.puerta)
                continue;
            float medio = (h.a0 + h.a1) / 2f;
            if (h.a1 - h.a0 > 1.2f)
                Bloque(medio - b, medio + b, yBase, h.y1, pm0, pm1, marco, true, kit);

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
        // Debajo de la farmacia el suelo de afuera tiene un hueco: ahí abajo está el reflejo del piso brillante.
        CajaConHueco(ext, new Vector3(-400f, -0.5f, -400f), new Vector3(400f, -0.2f, 400f), pasto);
        ext.brillo = 0.6f; // el asfalto refleja el sol
        CajaConHueco(ext, new Vector3(-70f, -0.25f, -70f), new Vector3(70f, -0.13f, 70f), asfalto);
        ext.brillo = 0.35f;
        CajaConHueco(ext, new Vector3(-7f, -0.25f, -8f), new Vector3(70f, -0.01f, 70f), acera); // la manzana de la farmacia
        Manzana(ext, -7f, 70f, -70f, -16f);  // al frente, cruzando la calle
        Manzana(ext, xCalleNueva + 4f, -13f, -8f, 70f);  // cruzando la carrera (llega hasta la calle nueva)
        Manzana(ext, xCalleNueva + 4f, -13f, -70f, -16f);
        // La calle (hacia el oriente) y la carrera siguen más allá (hasta 170 m) para que los carros
        // aparezcan y desaparezcan muy lejos. Hacia el occidente la calle termina en "T" contra la calle nueva.
        foreach (float s0 in new[] { -1f, 1f })
        {
            float a0 = s0 * 70f, a1 = s0 * LargoCalles;
            if (s0 > 0f)
            {
                ext.CajaMinMax(new Vector3(a0, -0.25f, -16f), new Vector3(a1, -0.13f, -8f), asfalto, false);
                Manzana(ext, a0, a1, -19f, -16f);
                Manzana(ext, a0, a1, -8f, -5f);
            }
            ext.CajaMinMax(new Vector3(-13f, -0.25f, Mathf.Min(a0, a1)), new Vector3(-7f, -0.13f, Mathf.Max(a0, a1)), asfalto, false);
            Manzana(ext, -16f, -13f, Mathf.Min(a0, a1), Mathf.Max(a0, a1));
            Manzana(ext, -7f, -4f, Mathf.Min(a0, a1), Mathf.Max(a0, a1));
        }

        // Calle nueva (norte-sur) donde termina la calle, y al otro lado una cuadra de edificios que cierra la vista.
        float xc0 = xCalleNueva - 4f, xc1 = xCalleNueva + 4f;
        ext.CajaMinMax(new Vector3(xc0, -0.25f, -70f), new Vector3(-70f, -0.13f, 70f), asfalto, false);
        ext.CajaMinMax(new Vector3(xc0, -0.25f, 70f), new Vector3(xc1, -0.13f, LargoCalles), asfalto, false);
        ext.CajaMinMax(new Vector3(xc0, -0.25f, -LargoCalles), new Vector3(xc1, -0.13f, -70f), asfalto, false);
        Manzana(ext, xc0 - 40f, xc0, -LargoCalles, LargoCalles);
        for (float z = -LargoCalles + 1f; z < LargoCalles - 1f; z += 3f)
            ext.Piso(new Vector3(xCalleNueva, -0.125f, z + 0.75f), 0.06f, 0.75f, amarillo);

        // Líneas de la calle (al frente) y de la carrera (al lado oeste), sin pintar en el cruce.
        for (float x = xCalleNueva + 4f; x < LargoCalles - 1f; x += 3f)
            if (x + 1.5f < -13f || x > -7f)
                ext.Piso(new Vector3(x + 0.75f, -0.125f, -12f), 0.75f, 0.06f, amarillo);
        for (float z = -LargoCalles + 1f; z < LargoCalles - 1f; z += 3f)
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

        // Los carros y el bus de la calle se mueven: los arma ConstruirTrafico.

        // Edificios cercanos (ladrillo bogotano, con ventanas y placas de concreto).
        Edificio(ext, rnd, new Vector3(-6.5f, 0f, Fondo + 0.3f), new Vector3(6.5f, 15f, Fondo + 12f), Elegir(rnd, ladrillos));  // vecino de atrás
        Edificio(ext, rnd, new Vector3(8f, 0f, 15f), new Vector3(24f, 18f, 26f), Elegir(rnd, ladrillos));       // detrás del parqueadero
        Edificio(ext, rnd, new Vector3(25f, 0f, -6f), new Vector3(36f, 12f, 14f), concreto);                   // al este del parqueadero
        float[][] frente = { new[] { -6.5f, 2f, 15f }, new[] { 2.5f, 11f, 18f }, new[] { 11.5f, 20f, 12f }, new[] { 20.5f, 30f, 15f } };
        foreach (var e in frente)
        {
            Edificio(ext, rnd, new Vector3(e[0], 0f, -30f), new Vector3(e[1], e[2], -19f), Elegir(rnd, ladrillos));
            if (e[0] == laCiaX0)
                EntradaLaCIA(ext); // el edificio justo al frente de la farmacia: entrada moderna con el letrero
            else
                Toldo(ext, rnd, new Vector3((e[0] + e[1]) / 2f, 2.6f, -18.6f), e[1] - e[0] - 1f, false);
        }
        float[][] carrera = { new[] { -7f, 2f, 12f }, new[] { 2.5f, 10f, 15f }, new[] { 10.5f, 20f, 18f }, new[] { 20.5f, 30f, 12f } };
        foreach (var e in carrera)
        {
            Edificio(ext, rnd, new Vector3(-28f, 0f, e[0]), new Vector3(-16f, e[2], e[1]), Elegir(rnd, ladrillos));
            Toldo(ext, rnd, new Vector3(-15.6f, 2.6f, (e[0] + e[1]) / 2f), e[1] - e[0] - 1f, true);
        }
        Edificio(ext, rnd, new Vector3(-28f, 0f, -30f), new Vector3(-16f, 21f, -19f), Elegir(rnd, ladrillos));

        // Atrás a la izquierda (al salir se veía un "potrero" vacío): un edificio ancho y otros detrás.
        Edificio(ext, rnd, new Vector3(-62f, 0f, -31f), new Vector3(-29f, 16f, -19f), Elegir(rnd, ladrillos));
        Toldo(ext, rnd, new Vector3(-53f, 2.6f, -18.6f), 14f, false);
        Toldo(ext, rnd, new Vector3(-37f, 2.6f, -18.6f), 12f, false);
        Edificio(ext, rnd, new Vector3(-62f, 0f, -52f), new Vector3(-30f, 24f, -32f), concreto);
        Edificio(ext, rnd, new Vector3(-28f, 0f, -48f), new Vector3(-16f, 19f, -31f), Elegir(rnd, ladrillos));
        Edificio(ext, rnd, new Vector3(-6.5f, 0f, -46f), new Vector3(10f, 13f, -31.5f), Elegir(rnd, ladrillos));
        Edificio(ext, rnd, new Vector3(10.5f, 0f, -46f), new Vector3(30f, 17f, -31.5f), Elegir(rnd, ladrillos));
        Edificio(ext, rnd, new Vector3(-62f, 0f, -4.5f), new Vector3(-29f, 14f, 8f), Elegir(rnd, ladrillos));

        // "Muro" de edificios al otro lado de la calle nueva: cierra el fondo de la calle (antes se veía un potrero).
        float xFachada = xCalleNueva - 6f;
        float[][] muro =
        {
            new[] { -62f, -37f, 22f }, new[] { -36f, -21f, 16f }, new[] { -20f, -4f, 26f },
            new[] { -3f, 12f, 18f }, new[] { 13f, 31f, 24f }, new[] { 32f, 52f, 15f },
        };
        foreach (var e in muro)
        {
            Edificio(ext, rnd, new Vector3(xFachada - 22f, 0f, e[0]), new Vector3(xFachada, e[2], e[1]),
                rnd.NextDouble() < 0.75 ? Elegir(rnd, ladrillos) : concreto);
            Toldo(ext, rnd, new Vector3(xFachada + 0.4f, 2.6f, (e[0] + e[1]) / 2f), e[1] - e[0] - 1f, true);
        }

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
            bool tapaLaCalle = min.z < -4f && max.z > -20f;
            bool tapaLaCarrera = min.x < -3f && max.x > -17f;
            bool tapaCalleNueva = min.x < xCalleNueva + 7f && max.x > xCalleNueva - 29f;
            if (tapaLaCalle || tapaLaCarrera || tapaCalleNueva)
                continue;
            ext.CajaMinMax(min, max, color);
            // Franjas de ventanas en la cara que mira hacia la farmacia.
            Vector3 haciaCentro = -c.normalized;
            Vector3 normal = Mathf.Abs(haciaCentro.x) > Mathf.Abs(haciaCentro.z)
                ? new Vector3(Mathf.Sign(haciaCentro.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(haciaCentro.z));
            float mitadCara = Mathf.Abs(normal.x) > 0 ? fondo / 2f : ancho / 2f;
            float offset = Mathf.Abs(normal.x) > 0 ? ancho / 2f : fondo / 2f;
            for (float y = 4f; y < alto - 2f; y += 3f)
            {
                ext.luzNoche = rnd.NextDouble() < 0.6 ? new Color(0.9f, 0.68f, 0.38f) * Rango(rnd, 0.35f, 0.7f) : Color.black;
                ext.Etiqueta(c + normal * (offset + 0.02f) + Vector3.up * y, normal, 0.55f, mitadCara * 0.8f,
                    Color.Lerp(vidrioOscuro, new Color(0.75f, 0.82f, 0.9f), 0.35f));
                ext.luzNoche = Color.black;
            }
        }

        // Árboles en andenes y parqueadero, y postes de luz.
        foreach (float x in new[] { 7f, 13f, 19f, 25f }) Arbol(ext, rnd, new Vector3(x, 0f, -7f));
        foreach (float x in new[] { -5f, 1f, 7f, 13f, 19f, 25f }) Arbol(ext, rnd, new Vector3(x, 0f, -17.3f));
        foreach (float z in new[] { -2f, 4f, 10f, 16f }) Arbol(ext, rnd, new Vector3(-6.3f, 0f, z));
        foreach (float z in new[] { -4f, 3f, 10f, 17f }) Arbol(ext, rnd, new Vector3(-14.3f, 0f, z));
        foreach (float z in new[] { -5f, 3f, 11f }) Arbol(ext, rnd, new Vector3(14.25f, 0f, z));
        foreach (var poste in Postes())
            Poste(ext, poste.base0, poste.hacia);
        foreach (var semaforo in semaforos)
            PosteSemaforo(ext, semaforo.poste, semaforo.mira);

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

    // ================= Tráfico: carros que se mueven y semáforos =================

    // Hasta dónde llegan la calle y la carrera (los carros dan la vuelta allá, muy lejos de la vista).
    const float LargoCalles = 170f;
    // Centro de la calle nueva (norte-sur) donde termina la calle en "T", justo después del último edificio.
    const float xCalleNueva = -68f;
    const float yCalle = -0.13f;

    // Semáforos del cruce: poste en cada esquina, mirando hacia los carros que llegan.
    // grupo 0 = calle (oriente-occidente), grupo 1 = carrera (norte-sur).
    struct DatoSemaforo
    {
        public Vector3 poste, mira;
        public int grupo;
        public DatoSemaforo(Vector3 poste, Vector3 mira, int grupo) { this.poste = poste; this.mira = mira; this.grupo = grupo; }
    }

    static readonly DatoSemaforo[] semaforos =
    {
        new DatoSemaforo(new Vector3(-6.6f, 0f, -7.6f), Vector3.right, 0),    // carros que van hacia el occidente
        new DatoSemaforo(new Vector3(-13.4f, 0f, -16.4f), Vector3.left, 0),   // carros que van hacia el oriente
        new DatoSemaforo(new Vector3(-13.4f, 0f, -7.6f), Vector3.forward, 1), // carros que bajan (hacia el sur)
        new DatoSemaforo(new Vector3(-6.6f, 0f, -16.4f), Vector3.back, 1),    // carros que suben (hacia el norte)
    };

    const float altoCabeza = 3.3f;

    static void PosteSemaforo(KitMalla k, Vector3 base0, Vector3 mira)
    {
        k.brillo = 0.4f;
        k.Cilindro(base0 + Vector3.up * (altoCabeza / 2f), 0.07f, altoCabeza, Vector3.up, 10, grisOscuro, true, true);
        // Caja negra con visera para los tres bombillos
        k.Caja(base0 + Vector3.up * altoCabeza + mira * 0.08f, Abs(new Vector3(mira.z, 0f, mira.x)) * 0.3f + new Vector3(0f, 0.95f, 0f) + Abs(mira) * 0.22f, new Color(0.1f, 0.1f, 0.12f));
        k.brillo = 0.2f;
    }

    // Crea los carros que andan por la calle y la carrera (en Colombia se maneja por la derecha),
    // el bus del SITP y los bombillos de los semáforos.
    internal static void ConstruirTrafico(Transform raiz, Material matExterior, Material matBombillo, Material matLuz, string carpeta)
    {
        var trafico = new GameObject("Trafico");
        trafico.transform.SetParent(raiz, false);

        // Mallas de los carros (con el frente hacia +X y las llantas en y = 0).
        var familiares = new Mesh[coloresFamiliar.Length];
        for (int i = 0; i < familiares.Length; i++)
        {
            var k = new KitMalla();
            Familiar(k, Vector3.zero, Vector3.right, coloresFamiliar[i]);
            familiares[i] = k.GuardarComo($"{carpeta}/Carro_Familiar_{i}.asset");
        }
        var kitTaxi = new KitMalla();
        Taxi(kitTaxi, Vector3.zero, Vector3.right, null);
        var mallaTaxi = kitTaxi.GuardarComo($"{carpeta}/Carro_Taxi.asset");
        var kitBus = new KitMalla();
        Bus(kitBus, Vector3.zero, null);
        var mallaBus = kitBus.GuardarComo($"{carpeta}/Bus_SITP.asset");
        // Luces de noche (haces de los faros y luz roja atrás), pegadas a cada carro.
        var lucesCarro = MallaLucesCarro($"{carpeta}/LucesNoche_Carro.asset", 2.25f, 0.65f, 0.57f);
        var lucesBus = MallaLucesCarro($"{carpeta}/LucesNoche_Bus.asset", 5.5f, 0.9f, 0.74f);

        // Carriles: ruta, línea de pare (antes del cruce o de la cebra) y grupo del semáforo.
        // Los de la calle voltean a la derecha en la "T" con la calle nueva (en Colombia se maneja por la derecha).
        float L = LargoCalles;
        float xNorte = xCalleNueva + 2f, xSur = xCalleNueva - 2f; // carriles de la calle nueva
        var haciaOriente = new List<Vector3> { new Vector3(xNorte, yCalle, -120f) };
        haciaOriente.AddRange(Curva(new Vector3(xNorte + 3f, yCalle, -17f), 3f, 180f, 90f, 8));
        haciaOriente.Add(new Vector3(L, yCalle, -14f));
        float pareOriente = LargoHasta(haciaOriente, haciaOriente.Count - 2) + (-13.6f - (xNorte + 3f));
        var haciaOccidente = new List<Vector3> { new Vector3(L, yCalle, -10f) };
        haciaOccidente.AddRange(Curva(new Vector3(xNorte + 3f, yCalle, -7f), 3f, -90f, -180f, 8));
        haciaOccidente.Add(new Vector3(xNorte, yCalle, 120f));
        var carriles = new[]
        {
            (ruta: haciaOriente.ToArray(), pare: pareOriente, grupo: 0),                                               // calle hacia el oriente
            (ruta: haciaOccidente.ToArray(), pare: L + 3.4f, grupo: 0),                                                // calle hacia el occidente
            (ruta: new[] { new Vector3(-8.6f, yCalle, -L), new Vector3(-8.6f, yCalle, L) }, pare: -16.6f + L, grupo: 1),  // carrera hacia el norte
            (ruta: new[] { new Vector3(-11.4f, yCalle, L), new Vector3(-11.4f, yCalle, -L) }, pare: L + 3.7f, grupo: 1),  // carrera hacia el sur
            (ruta: new[] { new Vector3(xSur, yCalle, 120f), new Vector3(xSur, yCalle, -120f) }, pare: -1f, grupo: 0),    // calle nueva hacia el sur
        };

        int numero = 0;
        void Carro(int carril, float avance, Mesh malla, Mesh[] variantes, float largo, float velocidad, float esperaMin, float esperaMax, bool esTaxi, bool esBus)
        {
            var c = carriles[carril];
            var go = new GameObject($"Carro_{++numero:00}");
            go.transform.SetParent(trafico.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            go.AddComponent<MeshRenderer>().sharedMaterial = matExterior;
            // Los letreros del taxi y del bus van pegados al carro (se crean con el carro en el origen).
            if (esTaxi) Taxi(new KitMalla(), Vector3.zero, Vector3.right, go.transform);
            if (esBus) Bus(new KitMalla(), Vector3.zero, go.transform);
            var luces = new GameObject("LucesNoche");
            luces.transform.SetParent(go.transform, false);
            luces.AddComponent<MeshFilter>().sharedMesh = esBus ? lucesBus : lucesCarro;
            var renderLuces = luces.AddComponent<MeshRenderer>();
            renderLuces.sharedMaterial = matLuz;
            renderLuces.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            luces.AddComponent<SoloDeNoche>();
            luces.SetActive(false);
            var ruta = go.AddComponent<CarroEnRuta>();
            ruta.ruta = c.ruta;
            ruta.inicio = c.ruta[0];
            ruta.fin = c.ruta[c.ruta.Length - 1];
            ruta.carril = carril;
            ruta.avanceInicial = avance;
            ruta.largo = largo;
            ruta.velocidad = velocidad;
            ruta.grupoSemaforo = c.grupo;
            ruta.lineaDePare = c.pare;
            ruta.variantes = variantes;
            ruta.esperaMinima = esperaMin;
            ruta.esperaMaxima = esperaMax;
            go.transform.position = c.ruta[0];
        }

        Mesh F(int i) => familiares[i % familiares.Length];
        // Calle hacia el oriente (el bus va por aquí, justo detrás del jugador al empezar)
        Carro(0, 176f, mallaBus, null, 11f, 6.5f, 8f, 25f, false, true);
        Carro(0, 130f, F(1), familiares, 4.5f, 8.3f, 0f, 7f, false, false);
        Carro(0, 60f, mallaTaxi, null, 4.4f, 8.3f, 0f, 7f, true, false);
        Carro(0, 250f, F(5), familiares, 4.5f, 8.3f, 0f, 7f, false, false);
        Carro(0, 300f, F(6), familiares, 4.5f, 8.3f, 0f, 7f, false, false);
        // Calle hacia el occidente
        Carro(1, 162f, mallaTaxi, null, 4.4f, 8.3f, 0f, 7f, true, false);
        Carro(1, 178f, F(0), familiares, 4.5f, 8.3f, 0f, 7f, false, false);
        Carro(1, 136f, F(2), familiares, 4.5f, 8.3f, 0f, 7f, false, false);
        Carro(1, 60f, F(7), familiares, 4.5f, 8.3f, 0f, 7f, false, false);
        Carro(1, 250f, mallaTaxi, null, 4.4f, 8.3f, 0f, 7f, true, false);
        // Carrera hacia el norte y hacia el sur
        Carro(2, 176f, F(3), familiares, 4.5f, 8.3f, 0f, 7f, false, false);
        Carro(2, 90f, mallaTaxi, null, 4.4f, 8.3f, 0f, 7f, true, false);
        Carro(2, 240f, F(4), familiares, 4.5f, 8.3f, 0f, 7f, false, false);
        Carro(3, 184f, F(4), familiares, 4.5f, 8.3f, 0f, 7f, false, false);
        Carro(3, 120f, F(1), familiares, 4.5f, 8.3f, 0f, 7f, false, false);
        Carro(3, 50f, mallaTaxi, null, 4.4f, 8.3f, 0f, 7f, true, false);
        // Calle nueva hacia el sur (pasa por el fondo de la "T")
        Carro(4, 40f, F(6), familiares, 4.5f, 8.3f, 0f, 9f, false, false);
        Carro(4, 150f, mallaTaxi, null, 4.4f, 8.3f, 0f, 9f, true, false);

        // Bombillos de los semáforos (rojo arriba, amarillo, verde abajo).
        var cruce = new GameObject("Semaforos");
        cruce.transform.SetParent(raiz, false);
        var control = cruce.AddComponent<SemaforoCruce>();
        var calle = new List<Renderer>();
        var carrera = new List<Renderer>();
        foreach (var dato in semaforos)
        {
            for (int i = 0; i < 3; i++)
            {
                var bombillo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(bombillo.GetComponent<Collider>());
                bombillo.name = $"Bombillo_{(dato.grupo == 0 ? "Calle" : "Carrera")}_{i}";
                bombillo.transform.SetParent(cruce.transform, false);
                bombillo.transform.position = dato.poste + Vector3.up * (altoCabeza + 0.29f - i * 0.29f) + dato.mira * 0.2f;
                bombillo.transform.localScale = Vector3.one * 0.2f;
                var r = bombillo.GetComponent<Renderer>();
                r.sharedMaterial = matBombillo;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                (dato.grupo == 0 ? calle : carrera).Add(r);
            }
        }
        control.bombillosCalle = calle.ToArray();
        control.bombillosCarrera = carrera.ToArray();
    }

    // Puntos de una curva (arco de círculo en el piso) para que los carros volteen suave en una esquina.
    static List<Vector3> Curva(Vector3 centro, float radio, float desdeGrados, float hastaGrados, int pasos)
    {
        var puntos = new List<Vector3>();
        for (int i = 0; i <= pasos; i++)
        {
            float a = Mathf.Lerp(desdeGrados, hastaGrados, i / (float)pasos) * Mathf.Deg2Rad;
            puntos.Add(centro + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radio);
        }
        return puntos;
    }

    // Largo de una ruta desde su inicio hasta el punto número "hasta".
    static float LargoHasta(List<Vector3> ruta, int hasta)
    {
        float largo = 0f;
        for (int i = 1; i <= hasta && i < ruta.Count; i++)
            largo += Vector3.Distance(ruta[i - 1], ruta[i]);
        return largo;
    }

    // Haces de luz de un carro (coordenadas del carro: +X adelante, la calle en y = 0).
    static Mesh MallaLucesCarro(string ruta, float hx, float alturaFaro, float ladoFaro)
    {
        var k = new KitMalla();
        Color Blanca(float a) => new Color(1f, 0.95f, 0.82f, a);
        Color Roja(float a) => new Color(1f, 0.08f, 0.04f, a);

        // Mancha de luz en la calle, adelante (más fuerte en el centro, se apaga hacia los lados y a lo lejos).
        float[] xs = { hx + 0.2f, hx + 2f, hx + 4.5f, hx + 8.5f };
        float[] alfas = { 0.36f, 0.28f, 0.13f, 0f };
        float[] anchos = { 1.0f, 1.6f, 2.4f, 3.3f };
        for (int i = 0; i < xs.Length - 1; i++)
            foreach (float s in new[] { -1f, 1f })
                k.CuadroColores(new Vector3(xs[i], 0.02f, 0f), new Vector3(xs[i + 1], 0.02f, 0f),
                    new Vector3(xs[i + 1], 0.02f, s * anchos[i + 1]), new Vector3(xs[i], 0.02f, s * anchos[i]),
                    Blanca(alfas[i]), Blanca(alfas[i + 1]), Blanca(0f), Blanca(0f));

        // Haz de cada faro (plano suave que baja hasta la calle)
        foreach (float s in new[] { -1f, 1f })
        {
            Vector3 faro = new Vector3(hx + 0.02f, alturaFaro, s * ladoFaro);
            Vector3 lejos = new Vector3(hx + 7f, 0.05f, s * ladoFaro * 1.4f);
            k.CuadroColores(faro + Vector3.back * 0.08f, faro + Vector3.forward * 0.08f,
                lejos + Vector3.forward * 0.9f, lejos + Vector3.back * 0.9f, Blanca(0.14f), Blanca(0.14f), Blanca(0f), Blanca(0f));
        }

        // Reflejo rojo de las luces de atrás
        k.CuadroColores(new Vector3(-hx - 0.1f, 0.02f, -0.9f), new Vector3(-hx - 0.1f, 0.02f, 0.9f),
            new Vector3(-hx - 1.8f, 0.02f, 1.1f), new Vector3(-hx - 1.8f, 0.02f, -1.1f), Roja(0.22f), Roja(0.22f), Roja(0f), Roja(0f));
        return k.GuardarComo(ruta);
    }

    // ---------- Letrero "LA CIA Agencia" (edificio al frente de la farmacia, cruzando la calle) ----------

    const float laCiaX0 = -6.5f, laCiaX1 = 2f, laCiaFachada = -19f;
    static readonly Color negroMate = new Color(0.1f, 0.11f, 0.13f);

    // Entrada moderna (como la foto): franja oscura para las letras, marquesina con listones, vitrina de vidrio y puerta.
    static void EntradaLaCIA(KitMalla k)
    {
        float x0 = laCiaX0 + 0.5f, x1 = laCiaX1 - 0.5f, z = laCiaFachada;
        k.brillo = 0.5f;
        // Franja oscura donde van las letras (sale un poco de la fachada)
        k.CajaMinMax(new Vector3(x0, 3.0f, z), new Vector3(x1, 4.15f, z + 0.28f), negroMate);
        // Marquesina: marco y listones metálicos
        float zm = z + 1.7f;
        k.CajaMinMax(new Vector3(x0 + 0.3f, 2.86f, z), new Vector3(x1 - 0.3f, 2.94f, z + 0.12f), negroMate);
        k.CajaMinMax(new Vector3(x0 + 0.3f, 2.86f, zm - 0.08f), new Vector3(x1 - 0.3f, 2.94f, zm), negroMate);
        foreach (float x in new[] { x0 + 0.3f, x1 - 0.38f })
            k.CajaMinMax(new Vector3(x, 2.86f, z), new Vector3(x + 0.08f, 2.94f, zm), negroMate);
        for (float x = x0 + 0.45f; x < x1 - 0.4f; x += 0.14f)
            k.CajaMinMax(new Vector3(x, 2.87f, z + 0.12f), new Vector3(x + 0.035f, 2.95f, zm - 0.08f), new Color(0.2f, 0.21f, 0.24f), false);
        // Tensores que sostienen la marquesina
        foreach (float x in new[] { x0 + 0.7f, x1 - 0.7f })
            k.Hexaedro(new[]
            {
                new Vector3(x - 0.015f, 2.94f, zm - 0.1f), new Vector3(x + 0.015f, 2.94f, zm - 0.1f),
                new Vector3(x + 0.015f, 2.94f, zm - 0.07f), new Vector3(x - 0.015f, 2.94f, zm - 0.07f),
                new Vector3(x - 0.015f, 3.6f, z + 0.03f), new Vector3(x + 0.015f, 3.6f, z + 0.03f),
                new Vector3(x + 0.015f, 3.6f, z + 0.06f), new Vector3(x - 0.015f, 3.6f, z + 0.06f),
            }, negroMate, false);
        // Vitrina de vidrio con perfiles oscuros, y puerta doble en el centro
        k.brillo = 1f;
        k.luzNoche = new Color(1.1f, 0.95f, 0.75f);
        k.Etiqueta(new Vector3((x0 + x1) / 2f, 1.4f, z + 0.02f), Vector3.forward, 1.35f, (x1 - x0) / 2f, vidrioOscuro);
        k.luzNoche = Color.black;
        k.brillo = 0.5f;
        for (float x = x0; x <= x1 + 0.01f; x += (x1 - x0) / 6f)
            k.CajaMinMax(new Vector3(x - 0.04f, 0.05f, z), new Vector3(x + 0.04f, 2.8f, z + 0.06f), negroMate, false);
        k.CajaMinMax(new Vector3(x0, 2.75f, z), new Vector3(x1, 2.82f, z + 0.06f), negroMate, false);
        k.CajaMinMax(new Vector3(x0, 0.0f, z), new Vector3(x1, 0.1f, z + 0.06f), negroMate, false);
        float xp = (x0 + x1) / 2f;
        foreach (float lado in new[] { -0.12f, 0.12f })
            k.CajaMinMax(new Vector3(xp + lado - 0.012f, 0.95f, z + 0.06f), new Vector3(xp + lado + 0.012f, 1.45f, z + 0.12f), new Color(0.75f, 0.76f, 0.78f), false);
        k.brillo = 0.2f;
    }

    // Letras blancas "LA CIA Agencia" sobre la franja oscura, y de noche un resplandor suave alrededor.
    internal static void ConstruirLetreroLaCIA(Transform raiz, Material matLuz, string carpeta)
    {
        float xc = (laCiaX0 + laCiaX1) / 2f, zFrente = laCiaFachada + 0.28f;
        var brillo = new KitMalla();
        Color calida = new Color(1f, 0.92f, 0.8f);
        var dentro = new List<Vector3>
        {
            new Vector3(xc + 3.2f, 3.2f, zFrente + 0.01f), new Vector3(xc - 3.2f, 3.2f, zFrente + 0.01f),
            new Vector3(xc - 3.2f, 3.95f, zFrente + 0.01f), new Vector3(xc + 3.2f, 3.95f, zFrente + 0.01f),
        };
        var fuera = new List<Vector3>
        {
            new Vector3(xc + 3.7f, 2.95f, zFrente + 0.01f), new Vector3(xc - 3.7f, 2.95f, zFrente + 0.01f),
            new Vector3(xc - 3.7f, 4.2f, zFrente + 0.01f), new Vector3(xc + 3.7f, 4.2f, zFrente + 0.01f),
        };
        Color c = new Color(calida.r, calida.g, calida.b, 0.22f), nada = new Color(calida.r, calida.g, calida.b, 0f);
        brillo.CuadroColores(dentro[0], dentro[1], dentro[2], dentro[3], c, c, c, c);
        brillo.FranjaDegradada(dentro, fuera, c, nada, true);
        brillo.CrearObjeto("LetreroLaCIA_Resplandor", raiz, matLuz, carpeta).AddComponent<SoloDeNoche>();

        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            return;
        var go = new GameObject("Letrero_LaCIA", typeof(RectTransform));
        go.transform.SetParent(raiz, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "<b>LA CIA</b> <size=75%>Agencia</size>";
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.1f;
        tmp.fontSizeMax = 40f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.characterSpacing = 4f;
        tmp.rectTransform.sizeDelta = new Vector2(6.2f, 0.8f);
        // El letrero mira hacia la farmacia (hacia +Z): se lee desde el andén de enfrente.
        go.transform.SetPositionAndRotation(new Vector3(xc, 3.58f, zFrente + 0.006f), Quaternion.Euler(0f, 180f, 0f));
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
                    k.luzNoche = LuzDeVentana(rnd);
                    k.Etiqueta(p, normal, 0.7f, Mathf.Min(0.65f, paso * 0.35f), vidrioOscuro);
                    k.luzNoche = Color.black;
                    k.brillo = brilloAntes;
                }
            }
        }
    }

    // De noche: la mayoría de ventanas encendidas (luz cálida), algunas con luz fría y otras apagadas.
    static Color LuzDeVentana(System.Random rnd)
    {
        double r = rnd.NextDouble();
        float f = Rango(rnd, 0.6f, 1.1f);
        if (r < 0.55) return new Color(1f, 0.76f, 0.4f) * f;
        if (r < 0.68) return new Color(0.7f, 0.82f, 1f) * (f * 0.8f);
        return Color.black;
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
        k.luzNoche = new Color(1.2f, 1.0f, 0.75f); // vitrina iluminada de noche
        k.Etiqueta(cara, normal, 1.1f, ancho * 0.45f, vidrioOscuro);
        k.luzNoche = Color.black;
    }

    static void Arbol(KitMalla k, System.Random rnd, Vector3 base0)
    {
        bool alto = rnd.NextDouble() < 0.35; // eucalipto / urapán más alto
        float altoTronco = alto ? 3.2f : 2.0f;
        k.Caja(base0 + Vector3.up * (altoTronco / 2f), new Vector3(0.25f, altoTronco, 0.25f), tronco);
        float s = Rango(rnd, 0.85f, 1.2f);
        Vector3 radios = alto ? new Vector3(1.2f, 2.3f, 1.2f) * s : new Vector3(1.5f, 1.3f, 1.5f) * s;
        k.Esfera(base0 + Vector3.up * (altoTronco + radios.y * 0.7f), radios, 2, Elegir(rnd, verdes), true, false, true); // lisa, sin polígonos
    }

    // Poste de luz con brazo hacia la calle ("hacia"). De noche la lámpara se prende.
    static void Poste(KitMalla k, Vector3 base0, Vector3 hacia)
    {
        Vector3 lado = Abs(new Vector3(hacia.z, 0f, hacia.x));
        k.Cilindro(base0 + Vector3.up * 2.5f, 0.06f, 5f, Vector3.up, 10, grisOscuro, true, true);
        k.Caja(base0 + Vector3.up * 4.95f + hacia * 0.5f, Abs(hacia) * 1.0f + lado * 0.08f + Vector3.up * 0.08f, grisOscuro);
        k.luzNoche = luzLampara;
        k.Caja(base0 + Vector3.up * 4.88f + hacia * 1.0f, Abs(hacia) * 0.45f + lado * 0.25f + Vector3.up * 0.1f, grisClaro);
        k.luzNoche = Color.black;
    }

    static readonly Color luzLampara = new Color(1.6f, 1.3f, 0.85f);

    // Postes en los andenes de la calle y de la carrera (lejos de los semáforos y de la farmacia).
    static List<(Vector3 base0, Vector3 hacia)> Postes()
    {
        var lista = new List<(Vector3 base0, Vector3 hacia)>();
        foreach (float x in new[] { 6.5f, 15f, 24f, 34f, 46f, 60f, -26f, -40f, -54f })
            lista.Add((new Vector3(x, 0f, -7.7f), Vector3.back));
        foreach (float x in new[] { -1f, 9f, 20f, 31f, 44f, 58f, -24f, -38f, -52f })
            lista.Add((new Vector3(x, 0f, -16.3f), Vector3.forward));
        foreach (float z in new[] { 1f, 13f, 24f, 36f, 50f, -27f, -40f })
            lista.Add((new Vector3(-6.7f, 0f, z), Vector3.left));
        foreach (float z in new[] { 6.5f, 13.5f, 22f, 32f, 46f, -24f, -36f })
            lista.Add((new Vector3(-13.3f, 0f, z), Vector3.right));
        return lista;
    }

    // Caja con un hueco (en X y Z) justo debajo de la farmacia.
    static void CajaConHueco(KitMalla k, Vector3 min, Vector3 max, Color color)
    {
        float hx0 = -4.95f, hx1 = 4.95f, hz0 = -4.95f, hz1 = Fondo - 0.05f;
        k.CajaMinMax(min, new Vector3(hx0, max.y, max.z), color, false);
        k.CajaMinMax(new Vector3(hx1, min.y, min.z), max, color, false);
        k.CajaMinMax(new Vector3(hx0, min.y, min.z), new Vector3(hx1, max.y, hz0), color, false);
        k.CajaMinMax(new Vector3(hx0, min.y, hz1), new Vector3(hx1, max.y, max.z), color, false);
    }

    // ================= Noche: manchas de luz de los postes =================

    // Altura del suelo de afuera en un punto (calle, andén o parqueadero).
    static float AlturaSuelo(float x, float z)
    {
        if ((z > -16f && z < -8f) || (x > -13f && x < -7f))
            return -0.125f;
        if (x > 5.4f && x < 24f && z > -7.4f && z < 14f)
            return 0.012f;
        return -0.003f;
    }

    // Luz de los postes de noche: una mancha cálida en el suelo y un cono suave de luz.
    internal static void ConstruirLucesNoche(Transform raiz, Material matLuz, string carpeta)
    {
        var k = new KitMalla();
        Color calida = new Color(1f, 0.78f, 0.45f);
        foreach (var poste in Postes())
        {
            Vector3 cabeza = poste.base0 + Vector3.up * 4.83f + poste.hacia * 1.0f;
            const float radio = 3.8f, celda = 0.6f;
            for (float x = -radio; x < radio - 0.001f; x += celda)
                for (float z = -radio; z < radio - 0.001f; z += celda)
                {
                    float cx = cabeza.x + x + celda / 2f, cz = cabeza.z + z + celda / 2f;
                    if (new Vector2(x + celda / 2f, z + celda / 2f).magnitude > radio + celda)
                        continue;
                    float y = AlturaSuelo(cx, cz);
                    Vector3 p0 = new Vector3(cabeza.x + x, y, cabeza.z + z);
                    Vector3 p1 = p0 + Vector3.right * celda, p2 = p0 + new Vector3(celda, 0f, celda), p3 = p0 + Vector3.forward * celda;
                    Color A(Vector3 p)
                    {
                        float d = new Vector2(p.x - cabeza.x, p.z - cabeza.z).magnitude;
                        float f = Mathf.Clamp01(1f - d / radio);
                        return new Color(calida.r, calida.g, calida.b, 0.5f * f * f);
                    }
                    k.CuadroColores(p0, p1, p2, p3, A(p0), A(p1), A(p2), A(p3));
                }
            // Cono de luz (dos planos cruzados, muy suaves)
            float suelo = AlturaSuelo(cabeza.x, cabeza.z);
            Color arriba = new Color(calida.r, calida.g, calida.b, 0.16f), abajo = new Color(calida.r, calida.g, calida.b, 0f);
            foreach (var eje in new[] { Vector3.right, Vector3.forward })
                k.CuadroColores(cabeza - eje * 0.12f, cabeza + eje * 0.12f,
                    new Vector3(cabeza.x, suelo, cabeza.z) + eje * 1.9f, new Vector3(cabeza.x, suelo, cabeza.z) - eje * 1.9f,
                    arriba, arriba, abajo, abajo);
        }
        k.CrearObjeto("LucesNoche_Calle", raiz, matLuz, carpeta).AddComponent<SoloDeNoche>();
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

        if (raiz == null || Resources.Load<TMP_Settings>("TMP Settings") == null)
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
        go.transform.localPosition = posicion;
        go.transform.localRotation = rotacion;
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
                k.luzNoche = new Color(1.6f, 1.6f, 1.45f); // faros prendidos de noche
                CuadroFrontal(x, Mathf.Min(s * 0.42f, s * 0.72f), Mathf.Max(s * 0.42f, s * 0.72f), 0.58f, 0.73f, new Color(0.95f, 0.95f, 0.9f));
                k.luzNoche = Color.black;
                CuadroFrontal(x, Mathf.Min(s * 0.74f, s * 0.85f), Mathf.Max(s * 0.74f, s * 0.85f), 0.58f, 0.73f, new Color(1f, 0.55f, 0.1f));
            }
            CajaLocal(hx, hx + 0.12f, 0.30f, 0.48f, -hz - 0.03f, hz + 0.03f, parachoques);
            CuadroFrontal(hx + 0.123f, -0.22f, 0.22f, 0.33f, 0.45f, new Color(0.95f, 0.95f, 0.95f)); // placa
        }

        public void Atras()
        {
            float x = -hx - 0.003f;
            k.luzNoche = new Color(1.3f, 0.12f, 0.06f); // luces de atrás prendidas de noche
            foreach (float s in new[] { -1f, 1f })
                CuadroTrasero(x, Mathf.Min(s * 0.52f, s * 0.84f), Mathf.Max(s * 0.52f, s * 0.84f), 0.6f, 0.76f, new Color(0.85f, 0.22f, 0.15f));
            k.luzNoche = Color.black;
            CajaLocal(-hx - 0.12f, -hx, 0.30f, 0.48f, -hz - 0.03f, hz + 0.03f, parachoques);
            CuadroTrasero(-hx - 0.123f, -0.22f, 0.22f, 0.33f, 0.45f, new Color(0.95f, 0.95f, 0.95f));
        }

        public void Ruedas()
        {
            foreach (float x in new[] { -hx * 0.62f, hx * 0.62f })
                foreach (float s in new[] { -1f, 1f })
                {
                    CuadroLateral(s, x - 0.42f, x + 0.42f, 0.30f, 0.66f, negroCarro);   // paso de rueda
                    k.Cilindro(Mundo(x, 0.31f, s * (hz - 0.07f)), 0.31f, 0.2f, lado, 18, negroCarro, true, true);
                    k.Cilindro(Mundo(x, 0.31f, s * (hz + 0.045f)), 0.14f, 0.03f, lado, 18, new Color(0.55f, 0.56f, 0.58f), true, true);
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
                k.luzNoche = new Color(0.5f, 0.6f, 0.7f); // de noche se ve la luz de adentro del bus
                k.Etiqueta(P(cx, 2.35f, zVidrio), n, 0.5f, mx - 0.02f, vidrio);
                k.luzNoche = Color.black;
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
                k.Cilindro(P(x, 0.5f, s * (hz - 0.165f)), 0.5f, 0.35f, Vector3.forward, 20, negro, true, true);
                k.Cilindro(P(x, 0.5f, s * (hz + 0.03f)), 0.24f, 0.04f, Vector3.forward, 20, gris, true, true);
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
            k.luzNoche = new Color(1.6f, 1.6f, 1.45f);
            foreach (float z in new[] { 0.62f, 0.85f })
                k.Cilindro(P(hx + 0.02f, 0.9f, s * z), 0.09f, 0.04f, Vector3.right, 14, blanco, true, true);
            k.luzNoche = Color.black;
            k.CajaMinMax(P(hx, 0.82f, s * 1.08f - 0.06f), P(hx + 0.03f, 0.98f, s * 1.08f + 0.06f), naranja);
            k.CajaMinMax(P(hx + 0.12f, 0.5f, s * 1.0f - 0.1f), P(hx + 0.15f, 0.58f, s * 1.0f + 0.1f), naranja);
        }

        // Atrás: ventana trasera
        k.Etiqueta(P(-hx - 0.002f, 2.4f, 0f), Vector3.left, 0.45f, 1.0f, negro);

        // Textos "SITP" (frente y costados)
        if (raiz == null || Resources.Load<TMP_Settings>("TMP Settings") == null)
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
        go.transform.localPosition = posicion;
        go.transform.localRotation = rotacion;
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    static T Elegir<T>(System.Random rnd, T[] opciones) => opciones[rnd.Next(opciones.Length)];

    static float Rango(System.Random rnd, float min, float max) => min + (float)rnd.NextDouble() * (max - min);
}
#endif

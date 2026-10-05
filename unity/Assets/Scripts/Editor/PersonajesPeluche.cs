#if UNITY_EDITOR
using UnityEngine;

// Mallas lisas (sin polígonos visibles ni línea de borde) para:
// - el pájaro rojo con overol y su pollito azul con gafas (como el dibujo),
// - los peluches rojos que sirven de "señuelo" (osito, corazón, mariquita y pulpito),
// - el robot aspiradora.
// Todo se arma en una "unidad" (1 de alto) mirando hacia +Z, excepto el robot (en metros reales).
internal static class PersonajesPeluche
{
    static readonly Color rojo = new Color(0.86f, 0.1f, 0.1f);
    static readonly Color barriga = new Color(0.97f, 0.62f, 0.58f);
    static readonly Color overol = new Color(0.82f, 0.66f, 0.46f);
    static readonly Color overolOscuro = new Color(0.62f, 0.47f, 0.31f);
    static readonly Color blancoOjo = new Color(0.98f, 0.98f, 0.97f);
    static readonly Color negro = new Color(0.07f, 0.06f, 0.06f);
    static readonly Color cafeIris = new Color(0.48f, 0.26f, 0.1f);
    static readonly Color naranja = new Color(1f, 0.6f, 0.12f);
    static readonly Color naranjaOscuro = new Color(0.94f, 0.48f, 0.08f);
    static readonly Color azulPollito = new Color(0.38f, 0.68f, 0.97f);
    static readonly Color azulPelo = new Color(0.55f, 0.82f, 1f);
    static readonly Color amarilloGafas = new Color(1f, 0.84f, 0.1f);
    static readonly Color rosado = new Color(0.98f, 0.62f, 0.66f);

    // Esfera lisa (sin contorno)
    static void E(KitMalla k, Vector3 centro, Vector3 radios, Color color, int subdivisiones = 2)
    {
        k.Esfera(centro, radios, subdivisiones, color, false, false, true);
    }

    // ================= Pájaro =================

    // Color del cuerpo en cada punto: rojo, barriga clara y overol (pantalón, peto en U y tirantes).
    static Color ColorCuerpo(Vector3 d)
    {
        float ax = Mathf.Abs(d.x);
        float pantalon = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.36f, -0.44f, d.y));
        float bordePeto = -0.12f + 0.55f * (ax / 0.62f) * (ax / 0.62f);
        float peto = ax < 0.62f ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(bordePeto + 0.03f, bordePeto - 0.03f, d.y)) : 0f;
        peto *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.28f, d.z));
        float tirante = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.085f, 0.055f, Mathf.Abs(ax - 0.5f)));
        tirante *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.0f, 0.15f, d.z)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.55f, d.y));
        float tela = Mathf.Max(pantalon, Mathf.Max(peto, tirante));
        float b = Mathf.Clamp01((d.z - 0.25f) / 0.5f) * Mathf.Clamp01(1f - Mathf.Abs(d.y + 0.02f) / 0.7f);
        Color piel = Color.Lerp(rojo, barriga, b * 0.9f);
        return Color.Lerp(piel, overol, tela);
    }

    internal static Mesh MallaPajaro(string ruta)
    {
        var k = new KitMalla();

        // Cuerpo en forma de huevo alto (con barriga y overol pintados), y colita
        k.EsferaColor(new Vector3(0f, 0.56f, 0f), new Vector3(0.4f, 0.47f, 0.37f), 4, ColorCuerpo, false);
        E(k, new Vector3(0f, 0.42f, -0.38f), new Vector3(0.09f, 0.07f, 0.08f), rojo);

        // Copete de tres plumas
        E(k, new Vector3(0f, 1.07f, -0.02f), new Vector3(0.035f, 0.1f, 0.035f), rojo);
        E(k, new Vector3(0.055f, 1.05f, -0.03f), new Vector3(0.03f, 0.08f, 0.03f), rojo);
        E(k, new Vector3(-0.05f, 1.04f, -0.01f), new Vector3(0.028f, 0.07f, 0.028f), rojo);

        // Ojos grandes y juntos, con iris café mirando un poco hacia el centro, pupila y brillo
        foreach (float s in new[] { -1f, 1f })
        {
            E(k, new Vector3(s * 0.105f, 0.8f, 0.27f), new Vector3(0.105f, 0.115f, 0.075f), blancoOjo, 3);
            E(k, new Vector3(s * 0.085f, 0.79f, 0.338f), new Vector3(0.05f, 0.055f, 0.012f), cafeIris);
            E(k, new Vector3(s * 0.083f, 0.785f, 0.348f), new Vector3(0.028f, 0.032f, 0.006f), negro);
            E(k, new Vector3(s * 0.07f, 0.805f, 0.353f), new Vector3(0.012f, 0.012f, 0.004f), Color.white, 1);
            // Cejas negras gruesas (un poco más bajas hacia el centro)
            E(k, new Vector3(s * 0.08f, 0.925f, 0.2f), new Vector3(0.08f, 0.04f, 0.06f), negro);
            E(k, new Vector3(s * 0.17f, 0.945f, 0.17f), new Vector3(0.08f, 0.04f, 0.055f), negro);
        }

        // Pico abierto: arriba con punta, boca, lengua y pico de abajo
        E(k, new Vector3(0f, 0.665f, 0.37f), new Vector3(0.1f, 0.055f, 0.095f), naranja);
        E(k, new Vector3(0f, 0.635f, 0.44f), new Vector3(0.045f, 0.04f, 0.04f), naranja);
        E(k, new Vector3(0f, 0.6f, 0.37f), new Vector3(0.075f, 0.04f, 0.06f), new Color(0.55f, 0.12f, 0.14f));
        E(k, new Vector3(0f, 0.59f, 0.405f), new Vector3(0.045f, 0.02f, 0.03f), new Color(0.95f, 0.5f, 0.55f));
        E(k, new Vector3(0f, 0.565f, 0.36f), new Vector3(0.075f, 0.03f, 0.07f), naranjaOscuro);

        // Overol: botones, estrella blanca en el tirante y bolsillo
        foreach (float s in new[] { -1f, 1f })
            E(k, new Vector3(s * 0.2f, 0.36f, 0.275f), new Vector3(0.035f, 0.035f, 0.015f), overolOscuro);
        EstrellaPlana(k, new Vector3(-0.2f, 0.62f, 0.322f), 0.045f, Color.white);
        E(k, new Vector3(0f, 0.27f, 0.27f), new Vector3(0.15f, 0.07f, 0.03f), overolOscuro);

        // Patas y pies de tres dedos
        foreach (float s in new[] { -1f, 1f })
        {
            k.Cilindro(new Vector3(s * 0.12f, 0.06f, 0.02f), 0.03f, 0.08f, Vector3.up, 12, naranja, false, true);
            foreach (float dx in new[] { -0.05f, 0f, 0.05f })
                E(k, new Vector3(s * 0.12f + dx, 0.025f, 0.08f), new Vector3(0.035f, 0.025f, 0.07f), naranja);
        }

        // El hijo: pollito azul con pelo parado y gafas amarillas, asomado en el bolsillo del overol
        E(k, new Vector3(0f, 0.5f, 0.36f), new Vector3(0.13f, 0.12f, 0.11f), azulPollito, 3);
        Vector3[] pelos =
        {
            new Vector3(0f, 0.64f, 0.36f), new Vector3(0.035f, 0.63f, 0.35f), new Vector3(-0.035f, 0.63f, 0.35f),
            new Vector3(0.062f, 0.61f, 0.36f), new Vector3(-0.062f, 0.61f, 0.36f), new Vector3(0.015f, 0.625f, 0.39f),
        };
        for (int i = 0; i < pelos.Length; i++)
        {
            float alto = i == 0 ? 0.06f : i < 3 ? 0.05f : 0.04f;
            E(k, pelos[i], new Vector3(0.017f, alto, 0.017f), azulPelo, 1);
        }
        foreach (float s in new[] { -1f, 1f })
        {
            E(k, new Vector3(s * 0.045f, 0.53f, 0.45f), new Vector3(0.04f, 0.042f, 0.02f), blancoOjo);
            E(k, new Vector3(s * 0.043f, 0.528f, 0.468f), new Vector3(0.024f, 0.026f, 0.006f), new Color(0.15f, 0.6f, 0.9f));
            E(k, new Vector3(s * 0.042f, 0.527f, 0.473f), new Vector3(0.014f, 0.015f, 0.004f), negro, 1);
            Aro(k, new Vector3(s * 0.045f, 0.53f, 0.464f), 0.043f, 0.054f, 0.012f, amarilloGafas);
            E(k, new Vector3(s * 0.065f, 0.48f, 0.475f), new Vector3(0.03f, 0.022f, 0.02f), azulPollito, 1); // manitos en el borde
        }
        k.Caja(new Vector3(0f, 0.535f, 0.472f), new Vector3(0.02f, 0.008f, 0.008f), amarilloGafas, false);
        E(k, new Vector3(0f, 0.505f, 0.47f), new Vector3(0.012f, 0.01f, 0.014f), naranja, 1);
        // Borde del bolsillo, por delante del pollito
        E(k, new Vector3(0f, 0.405f, 0.43f), new Vector3(0.2f, 0.085f, 0.065f), overol, 3);

        return k.GuardarComo(ruta);
    }

    // Ala tipo "mano" (cuelga desde el hombro), con tres plumas al final como dedos.
    internal static Mesh MallaAla(string ruta)
    {
        var k = new KitMalla();
        E(k, new Vector3(0f, -0.14f, 0.02f), new Vector3(0.075f, 0.18f, 0.13f), rojo, 3);
        E(k, new Vector3(0f, -0.3f, 0.07f), new Vector3(0.05f, 0.07f, 0.045f), rojo);
        E(k, new Vector3(0f, -0.31f, 0.0f), new Vector3(0.05f, 0.075f, 0.045f), rojo);
        E(k, new Vector3(0f, -0.29f, -0.065f), new Vector3(0.045f, 0.065f, 0.04f), rojo);
        return k.GuardarComo(ruta);
    }

    // Aro (anillo) mirando hacia +Z: para las gafas.
    static void Aro(KitMalla k, Vector3 centro, float radioDentro, float radioFuera, float fondo, Color color)
    {
        const int lados = 28;
        for (int i = 0; i < lados; i++)
        {
            float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
            Vector3 d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f), d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
            Vector3 f0 = centro + d0 * radioFuera, f1 = centro + d1 * radioFuera;
            Vector3 i0 = centro + d0 * radioDentro, i1 = centro + d1 * radioDentro;
            Vector3 atras = Vector3.back * fondo;
            k.Triangulo(f0, f1, i1, Vector3.forward, color, centro, false);
            k.Triangulo(f0, i1, i0, Vector3.forward, color, centro, false);
            k.TrianguloSuave(f0, f1, f1 + atras, d0, d1, d1, d0 + d1, color, centro, false);
            k.TrianguloSuave(f0, f1 + atras, f0 + atras, d0, d1, d0, d0 + d1, color, centro, false);
            k.TrianguloSuave(i0, i1, i1 + atras, -d0, -d1, -d1, -(d0 + d1), color, centro, false);
            k.TrianguloSuave(i0, i1 + atras, i0 + atras, -d0, -d1, -d0, -(d0 + d1), color, centro, false);
        }
    }

    // Estrella plana de 5 puntas (mirando hacia +Z).
    static void EstrellaPlana(KitMalla k, Vector3 centro, float radio, Color color)
    {
        var puntas = new Vector3[10];
        for (int i = 0; i < 10; i++)
        {
            float r = i % 2 == 0 ? radio : radio * 0.45f;
            float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
            puntas[i] = centro + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        }
        for (int i = 0; i < 10; i++)
            k.Triangulo(centro, puntas[i], puntas[(i + 1) % 10], Vector3.forward, color, centro, false);
    }

    // ================= Peluches rojos (señuelos) =================

    internal static Mesh[] MallasPeluches(string carpeta)
    {
        return new[]
        {
            Osito().GuardarComo($"{carpeta}/Peluche_Osito.asset"),
            Corazon().GuardarComo($"{carpeta}/Peluche_Corazon.asset"),
            Mariquita().GuardarComo($"{carpeta}/Peluche_Mariquita.asset"),
            Pulpito().GuardarComo($"{carpeta}/Peluche_Pulpito.asset"),
        };
    }

    static KitMalla Osito()
    {
        var k = new KitMalla();
        Color r = new Color(0.82f, 0.12f, 0.14f), claro = new Color(0.98f, 0.76f, 0.72f);
        E(k, new Vector3(0f, 0.33f, 0f), new Vector3(0.28f, 0.3f, 0.25f), r, 3);
        E(k, new Vector3(0f, 0.3f, 0.18f), new Vector3(0.17f, 0.18f, 0.08f), claro);
        E(k, new Vector3(0f, 0.76f, 0.02f), new Vector3(0.25f, 0.23f, 0.22f), r, 3);
        foreach (float s in new[] { -1f, 1f })
        {
            E(k, new Vector3(s * 0.17f, 0.95f, 0f), new Vector3(0.08f, 0.08f, 0.05f), r);
            E(k, new Vector3(s * 0.17f, 0.95f, 0.04f), new Vector3(0.045f, 0.045f, 0.02f), claro);
            E(k, new Vector3(s * 0.08f, 0.81f, 0.2f), new Vector3(0.03f, 0.035f, 0.02f), negro);
            E(k, new Vector3(s * 0.27f, 0.42f, 0.05f), new Vector3(0.09f, 0.14f, 0.09f), r);
            E(k, new Vector3(s * 0.13f, 0.08f, 0.1f), new Vector3(0.1f, 0.08f, 0.12f), r);
        }
        E(k, new Vector3(0f, 0.7f, 0.2f), new Vector3(0.11f, 0.08f, 0.07f), claro);
        E(k, new Vector3(0f, 0.73f, 0.27f), new Vector3(0.04f, 0.03f, 0.025f), negro);
        return k;
    }

    static KitMalla Corazon()
    {
        var k = new KitMalla();
        Color r = new Color(0.9f, 0.15f, 0.2f);
        foreach (float s in new[] { -1f, 1f })
            E(k, new Vector3(s * 0.15f, 0.6f, 0f), new Vector3(0.22f, 0.22f, 0.17f), r, 3);
        E(k, new Vector3(0f, 0.38f, 0f), new Vector3(0.28f, 0.32f, 0.165f), r, 3);
        foreach (float s in new[] { -1f, 1f })
        {
            E(k, new Vector3(s * 0.1f, 0.55f, 0.16f), new Vector3(0.035f, 0.045f, 0.02f), negro);
            E(k, new Vector3(s * 0.2f, 0.45f, 0.13f), new Vector3(0.05f, 0.03f, 0.02f), rosado);
        }
        for (int i = 0; i < 5; i++)
        {
            float a = Mathf.Lerp(-0.9f, 0.9f, i / 4f);
            E(k, new Vector3(Mathf.Sin(a) * 0.07f, 0.46f - Mathf.Cos(a) * 0.03f, 0.165f), new Vector3(0.014f, 0.014f, 0.01f), negro, 1);
        }
        return k;
    }

    static KitMalla Mariquita()
    {
        var k = new KitMalla();
        Color r = new Color(0.88f, 0.1f, 0.08f);
        Vector3 centro = new Vector3(0f, 0.3f, -0.03f), radios = new Vector3(0.32f, 0.28f, 0.4f);
        E(k, centro, radios, r, 3);
        E(k, centro + Vector3.up * 0.005f, new Vector3(0.012f, 0.283f, 0.395f), negro, 2);
        Vector3[] manchas =
        {
            new Vector3(0.45f, 0.75f, 0.3f), new Vector3(-0.45f, 0.75f, 0.3f), new Vector3(0.6f, 0.55f, -0.3f),
            new Vector3(-0.6f, 0.55f, -0.3f), new Vector3(0.3f, 0.8f, -0.55f), new Vector3(-0.3f, 0.8f, -0.55f),
        };
        foreach (var m in manchas)
            E(k, centro + Vector3.Scale(m.normalized, radios) * 0.97f, new Vector3(0.06f, 0.06f, 0.06f), negro);
        E(k, new Vector3(0f, 0.26f, 0.36f), new Vector3(0.17f, 0.15f, 0.13f), negro, 3);
        foreach (float s in new[] { -1f, 1f })
        {
            E(k, new Vector3(s * 0.065f, 0.29f, 0.47f), new Vector3(0.045f, 0.05f, 0.02f), blancoOjo);
            E(k, new Vector3(s * 0.06f, 0.285f, 0.487f), new Vector3(0.022f, 0.025f, 0.006f), negro, 1);
            k.Cilindro(new Vector3(s * 0.06f, 0.46f, 0.4f), 0.008f, 0.14f, Vector3.up, 8, negro, false, true);
            E(k, new Vector3(s * 0.06f, 0.54f, 0.4f), new Vector3(0.025f, 0.025f, 0.025f), negro, 1);
        }
        return k;
    }

    static KitMalla Pulpito()
    {
        var k = new KitMalla();
        Color r = new Color(0.9f, 0.22f, 0.18f);
        E(k, new Vector3(0f, 0.55f, 0f), new Vector3(0.3f, 0.33f, 0.28f), r, 3);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f + 0.3f;
            Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            E(k, d * 0.2f + Vector3.up * 0.18f, new Vector3(0.075f, 0.14f, 0.075f), r);
            E(k, d * 0.3f + Vector3.up * 0.06f, new Vector3(0.06f, 0.05f, 0.06f), r);
        }
        foreach (float s in new[] { -1f, 1f })
        {
            E(k, new Vector3(s * 0.1f, 0.58f, 0.25f), new Vector3(0.065f, 0.075f, 0.03f), blancoOjo);
            E(k, new Vector3(s * 0.095f, 0.57f, 0.275f), new Vector3(0.035f, 0.04f, 0.008f), negro, 1);
            E(k, new Vector3(s * 0.19f, 0.47f, 0.22f), new Vector3(0.05f, 0.03f, 0.02f), rosado);
        }
        E(k, new Vector3(0f, 0.45f, 0.27f), new Vector3(0.05f, 0.022f, 0.02f), new Color(0.5f, 0.08f, 0.1f));
        return k;
    }

    // ================= Robot aspiradora (en metros) =================

    internal static Mesh MallaRobot(string ruta)
    {
        var k = new KitMalla();
        Color blanco = new Color(0.95f, 0.95f, 0.96f), gris = new Color(0.32f, 0.34f, 0.38f), cian = new Color(0.2f, 0.9f, 1f);
        k.grosorContorno = 0.6f;
        k.Cilindro(new Vector3(0f, 0.045f, 0f), 0.17f, 0.07f, Vector3.up, 36, blanco, true, true);
        k.Cilindro(new Vector3(0f, 0.0815f, 0f), 0.125f, 0.003f, Vector3.up, 36, gris, false, true);
        k.Cilindro(new Vector3(0f, 0.094f, -0.035f), 0.042f, 0.025f, Vector3.up, 24, blanco, true, true);
        k.grosorContorno = 1f;
        E(k, new Vector3(0f, 0.0835f, 0.1f), new Vector3(0.01f, 0.004f, 0.01f), cian, 1);
        // Parachoques gris adelante
        var puntos = new System.Collections.Generic.List<Vector2>();
        var normales = new System.Collections.Generic.List<Vector2>();
        for (int i = 0; i <= 20; i++)
        {
            float a = Mathf.Lerp(-80f, 80f, i / 20f) * Mathf.Deg2Rad;
            Vector2 n = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            puntos.Add(n * 0.172f);
            normales.Add(n);
        }
        k.CintaVertical(puntos, normales, 0.02f, 0.06f, gris);
        return k.GuardarComo(ruta);
    }

    internal static Mesh MallaCepillo(string ruta)
    {
        var k = new KitMalla();
        Color negroCepillo = new Color(0.15f, 0.15f, 0.17f);
        k.Cilindro(Vector3.zero, 0.012f, 0.008f, Vector3.up, 12, negroCepillo, false, true);
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2f / 3f;
            Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), lado = new Vector3(-d.z, 0f, d.x) * 0.006f;
            Vector3 p0 = d * 0.01f, p1 = d * 0.06f;
            k.CuadroLibre(p0 - lado, p1 - lado * 2f, p1 + lado * 2f, p0 + lado, Vector3.up, negroCepillo);
            k.CuadroLibre(p0 - lado, p1 - lado * 2f, p1 + lado * 2f, p0 + lado, Vector3.down, negroCepillo);
        }
        return k.GuardarComo(ruta);
    }
}
#endif

using System.Collections.Generic;
using UnityEngine;

// Arma los personajes de prueba con líneas y rellenos, como un dibujo.
// Los personajes "dibujados" tienen un esqueleto invisible (piernas de 4 nodos, brazos de 3)
// y cada parte (muslo, canilla, zapato, brazo, antebrazo, mano, torso, cabeza...) está pegada a su hueso.
// Medidas en metros: x = hacia adelante (el personaje mira a la derecha), y = arriba; la cadera está en (0, 0).
public class ConstructorPersonaje
{
    readonly Dibujo dibujo;
    readonly Vector3 cadera;
    readonly Vector3 derecha;
    readonly float ancho;
    readonly float escala;

    // escala: 1 = tamaño normal (el tutorial usa uno más pequeño).
    public ConstructorPersonaje(Dibujo dibujo, Vector3 caderaMundo, Vector3 derechaMundo, float anchoLocal, float escala = 1f)
    {
        this.dibujo = dibujo;
        cadera = caderaMundo;
        derecha = derechaMundo;
        ancho = anchoLocal;
        this.escala = escala;
    }

    Vector3 Local(Vector2 p)
    {
        p *= escala;
        Vector3 mundo = cadera + derecha * p.x + Vector3.up * p.y;
        return dibujo.ProyectarEnPlano(dibujo.transform.InverseTransformPoint(mundo));
    }

    int Linea(IList<Vector2> puntos, bool cerrada, int color = 0, bool oculta = false, float grosor = 1f)
    {
        var d = new DatosTrazo
        {
            ancho = ancho * grosor,
            cerrado = cerrada && puntos.Count >= 3,
            relleno = cerrada && puntos.Count >= 3,
            colorRelleno = color,
            oculto = oculta
        };
        foreach (var p in puntos)
            d.nodos.Add(Local(p));
        return dibujo.AgregarTrazo(d).id;
    }

    // Piso bajo los pies (y = altura de los pies).
    public int Piso(float pies)
    {
        return Linea(new[] { new Vector2(-0.45f, pies - 0.009f), new Vector2(0.65f, pies - 0.009f) }, false, 0, false, 0.7f);
    }

    // ---------- Palito ----------

    public DatosPersonaje Palito(out float pies)
    {
        var p = new DatosPersonaje { nombre = "Palito" };
        p.pierna2 = Linea(new[] { new Vector2(0.008f, 0f), new Vector2(0.028f, -0.085f), new Vector2(0.008f, -0.17f), new Vector2(0.043f, -0.17f) }, false);
        p.brazo2 = Linea(new[] { new Vector2(0.008f, 0.125f), new Vector2(0.008f, 0.065f), new Vector2(0.012f, 0.01f) }, false);
        p.cuerpo.Add(Linea(new[] { new Vector2(0.004f, 0f), new Vector2(0.012f, 0.07f), new Vector2(0.006f, 0.14f) }, false));
        p.pierna1 = Linea(new[] { new Vector2(0f, 0f), new Vector2(0.02f, -0.085f), new Vector2(0f, -0.17f), new Vector2(0.035f, -0.17f) }, false);
        p.brazo1 = Linea(new[] { new Vector2(0.006f, 0.125f), new Vector2(0.006f, 0.065f), new Vector2(0.01f, 0.01f) }, false);
        p.cabeza = Linea(Elipse(new Vector2(0.012f, 0.185f), 0.035f, 0.038f, 10), true, 0);
        p.cuerpo.Add(Linea(new[] { new Vector2(0.046f, 0.19f), new Vector2(0.06f, 0.18f) }, false));
        pies = -0.17f;
        return p;
    }

    // ---------- Personajes dibujados ----------

    class Medidas
    {
        public string nombre;
        public float muslo, canilla, pie, brazoSup, antebrazo, hombroY;
        public float anchoCadera, anchoRodilla, anchoTobillo, anchoHombro, anchoCodo, anchoMuneca, mano, bulto = 1f;
        public Vector2[] torso;
        public Vector2 cabeza;
        public float cabezaX, cabezaY;
        public float nariz = 1f;
        public bool cuello;
        public float zapato = 1f;
        public int colorTorso, colorPiernas, colorPiel = 2, colorZapato = 7;
    }

    static Medidas MedidasDe(int tipo)
    {
        switch (tipo)
        {
            case 1: // Musculoso: pecho enorme, cintura fina, brazos grandes, cabeza pequeña.
                return new Medidas
                {
                    nombre = "Musculoso",
                    muslo = 0.085f, canilla = 0.08f, pie = 0.035f, brazoSup = 0.07f, antebrazo = 0.065f, hombroY = 0.17f,
                    anchoCadera = 0.032f, anchoRodilla = 0.022f, anchoTobillo = 0.015f,
                    anchoHombro = 0.042f, anchoCodo = 0.03f, anchoMuneca = 0.02f, mano = 0.014f, bulto = 1.25f,
                    torso = new[]
                    {
                        new Vector2(-0.025f, -0.005f), new Vector2(0.03f, -0.005f), new Vector2(0.045f, 0.05f), new Vector2(0.085f, 0.13f),
                        new Vector2(0.06f, 0.18f), new Vector2(0f, 0.195f), new Vector2(-0.06f, 0.18f), new Vector2(-0.055f, 0.12f), new Vector2(-0.03f, 0.05f)
                    },
                    cabeza = new Vector2(0.022f, 0.225f), cabezaX = 0.024f, cabezaY = 0.028f,
                    colorTorso = 1, colorPiernas = 5,
                };
            case 2: // Gordito: cuerpo de pera, piernas cortas.
                return new Medidas
                {
                    nombre = "Gordito",
                    muslo = 0.055f, canilla = 0.05f, pie = 0.032f, brazoSup = 0.045f, antebrazo = 0.045f, hombroY = 0.125f,
                    anchoCadera = 0.032f, anchoRodilla = 0.025f, anchoTobillo = 0.018f,
                    anchoHombro = 0.026f, anchoCodo = 0.021f, anchoMuneca = 0.016f, mano = 0.013f,
                    torso = new[]
                    {
                        new Vector2(-0.045f, -0.012f), new Vector2(0.05f, -0.012f), new Vector2(0.088f, 0.04f), new Vector2(0.075f, 0.1f),
                        new Vector2(0.035f, 0.142f), new Vector2(-0.03f, 0.142f), new Vector2(-0.062f, 0.09f), new Vector2(-0.065f, 0.03f)
                    },
                    cabeza = new Vector2(0.018f, 0.172f), cabezaX = 0.035f, cabezaY = 0.035f,
                    colorTorso = 4, colorPiernas = 1,
                };
            case 3: // Flaco alto: piernas largas, cuello largo y nariz grande.
                return new Medidas
                {
                    nombre = "Flaco",
                    muslo = 0.1f, canilla = 0.1f, pie = 0.045f, brazoSup = 0.08f, antebrazo = 0.075f, hombroY = 0.16f,
                    anchoCadera = 0.018f, anchoRodilla = 0.014f, anchoTobillo = 0.011f,
                    anchoHombro = 0.014f, anchoCodo = 0.012f, anchoMuneca = 0.01f, mano = 0.011f,
                    torso = new[]
                    {
                        new Vector2(-0.02f, -0.005f), new Vector2(0.022f, -0.005f), new Vector2(0.03f, 0.08f), new Vector2(0.028f, 0.15f),
                        new Vector2(0f, 0.168f), new Vector2(-0.026f, 0.15f), new Vector2(-0.03f, 0.08f)
                    },
                    cabeza = new Vector2(0.018f, 0.25f), cabezaX = 0.022f, cabezaY = 0.032f, nariz = 2.2f, cuello = true, zapato = 1.2f,
                    colorTorso = 3, colorPiernas = 6,
                };
            default: // Niño: cabeza grande, cuerpo pequeño y redondo.
                return new Medidas
                {
                    nombre = "Niño",
                    muslo = 0.045f, canilla = 0.04f, pie = 0.028f, brazoSup = 0.04f, antebrazo = 0.035f, hombroY = 0.085f,
                    anchoCadera = 0.022f, anchoRodilla = 0.018f, anchoTobillo = 0.015f,
                    anchoHombro = 0.016f, anchoCodo = 0.014f, anchoMuneca = 0.012f, mano = 0.012f, zapato = 0.85f,
                    torso = new[]
                    {
                        new Vector2(-0.03f, -0.005f), new Vector2(0.03f, -0.005f), new Vector2(0.04f, 0.05f), new Vector2(0.03f, 0.092f),
                        new Vector2(-0.03f, 0.092f), new Vector2(-0.04f, 0.05f)
                    },
                    cabeza = new Vector2(0.012f, 0.145f), cabezaX = 0.05f, cabezaY = 0.048f,
                    colorTorso = 0, colorPiernas = 1,
                };
        }
    }

    public DatosPersonaje Dibujado(int tipo, out float pies)
    {
        var m = MedidasDe(tipo);
        var p = new DatosPersonaje { nombre = m.nombre };
        float largo = m.muslo + m.canilla;
        pies = -largo;

        // Esqueleto invisible (los huesos que mueve el caminado).
        p.pierna2 = Linea(new[] { new Vector2(0.006f, 0f), new Vector2(0.014f, -m.muslo), new Vector2(0.006f, -largo), new Vector2(0.006f + m.pie, -largo) }, false, 0, true);
        p.pierna1 = Linea(new[] { new Vector2(0f, 0f), new Vector2(0.008f, -m.muslo), new Vector2(0f, -largo), new Vector2(m.pie, -largo) }, false, 0, true);
        float hx = 0.006f;
        p.brazo2 = Linea(new[] { new Vector2(hx - 0.006f, m.hombroY), new Vector2(hx - 0.006f, m.hombroY - m.brazoSup), new Vector2(hx, m.hombroY - m.brazoSup - m.antebrazo) }, false, 0, true);
        p.brazo1 = Linea(new[] { new Vector2(hx, m.hombroY), new Vector2(hx, m.hombroY - m.brazoSup), new Vector2(hx + 0.006f, m.hombroY - m.brazoSup - m.antebrazo) }, false, 0, true);

        // Piernas (atrás y adelante): muslo, canilla y zapato.
        for (int k = 0; k < 2; k++)
        {
            int miembro = k == 0 ? 1 : 0;      // primero la de atrás
            float x0 = k == 0 ? 0.006f : 0f;
            float prof = k == 0 ? -0.004f : 0.003f;
            var cadera = new Vector2(x0, 0f);
            var rodilla = new Vector2(x0 + (k == 0 ? 0.008f : 0.008f), -m.muslo);
            var tobillo = new Vector2(x0, -largo);
            var punta = new Vector2(x0 + m.pie, -largo);
            Pegar(p, Linea(Capsula(cadera, rodilla, m.anchoCadera, m.anchoRodilla, 1f), true, m.colorPiernas), miembro, 0, prof);
            Pegar(p, Linea(Capsula(rodilla, tobillo, m.anchoRodilla, m.anchoTobillo, 1f), true, m.colorPiernas), miembro, 1, prof);
            Pegar(p, Linea(Zapato(tobillo, punta, m.zapato), true, m.colorZapato), miembro, 2, prof + 0.0005f);
        }

        // Torso y cuello.
        Pegar(p, Linea(m.torso, true, m.colorTorso), 4, 0, -0.001f);
        if (m.cuello)
            Pegar(p, Linea(Capsula(new Vector2(0.002f, m.hombroY - 0.01f), new Vector2(m.cabeza.x - 0.004f, m.cabeza.y - m.cabezaY * 0.7f), 0.012f, 0.01f, 1f), true, m.colorPiel), 4, 0, -0.0015f);

        // Cabeza: forma con relleno, ojo, nariz y boca.
        p.cabeza = Linea(Elipse(m.cabeza, m.cabezaX, m.cabezaY, 10), true, m.colorPiel);
        Pegar(p, Linea(Elipse(m.cabeza + new Vector2(m.cabezaX * 0.45f, m.cabezaY * 0.2f), 0.004f, 0.005f, 6), true, 7, false, 0.6f), 5, 0, 0.001f);
        float largoNariz = 0.012f * m.nariz;
        Pegar(p, Linea(new[]
        {
            m.cabeza + new Vector2(m.cabezaX * 0.88f, m.cabezaY * 0.12f),
            m.cabeza + new Vector2(m.cabezaX * 0.9f + largoNariz, -m.cabezaY * 0.1f),
            m.cabeza + new Vector2(m.cabezaX * 0.85f, -m.cabezaY * 0.28f)
        }, false, 0, false, 0.8f), 5, 0, 0.001f);
        Pegar(p, Linea(new[]
        {
            m.cabeza + new Vector2(m.cabezaX * 0.35f, -m.cabezaY * 0.55f),
            m.cabeza + new Vector2(m.cabezaX * 0.65f, -m.cabezaY * 0.5f)
        }, false, 0, false, 0.6f), 5, 0, 0.001f);

        // Brazos (atrás y adelante): brazo, antebrazo y mano.
        for (int k = 0; k < 2; k++)
        {
            int miembro = k == 0 ? 3 : 2;
            float x0 = k == 0 ? hx - 0.006f : hx;
            float prof = k == 0 ? -0.007f : 0.006f;
            var hombro = new Vector2(x0, m.hombroY);
            var codo = new Vector2(x0, m.hombroY - m.brazoSup);
            var muneca = new Vector2(x0 + 0.006f, m.hombroY - m.brazoSup - m.antebrazo);
            Pegar(p, Linea(Capsula(hombro, codo, m.anchoHombro, m.anchoCodo, m.bulto), true, m.colorPiel), miembro, 0, prof);
            Pegar(p, Linea(Capsula(codo, muneca, m.anchoCodo, m.anchoMuneca, 1f), true, m.colorPiel), miembro, 1, prof);
            Pegar(p, Linea(Elipse(muneca + new Vector2(0.002f, -m.mano * 0.6f), m.mano * 0.75f, m.mano, 7), true, m.colorPiel), miembro, 1, prof + 0.0005f);
        }
        return p;
    }

    static void Pegar(DatosPersonaje p, int id, int miembro, int tramo, float profundidad)
    {
        p.pegados.Add(new PegadoHueso { id = id, miembro = miembro, tramo = tramo, profundidad = profundidad });
    }

    // ---------- Formas ----------

    static Vector2[] Elipse(Vector2 centro, float rx, float ry, int nodos)
    {
        var l = new Vector2[nodos];
        for (int i = 0; i < nodos; i++)
        {
            float a = i * Mathf.PI * 2f / nodos;
            l[i] = centro + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
        }
        return l;
    }

    // Forma de "cápsula" alrededor de un hueso (ancho al inicio y al final; bulto = músculo en el medio).
    static Vector2[] Capsula(Vector2 a, Vector2 b, float wa, float wb, float bulto)
    {
        Vector2 d = b - a;
        if (d.sqrMagnitude < 1e-10f)
            d = Vector2.down;
        d.Normalize();
        var n = new Vector2(-d.y, d.x);
        Vector2 medio = (a + b) * 0.5f;
        float wm = (wa + wb) * 0.25f * bulto;
        return new[]
        {
            a - d * wa * 0.35f,
            a + n * wa * 0.5f,
            medio + n * wm,
            b + n * wb * 0.5f,
            b + d * wb * 0.35f,
            b - n * wb * 0.5f,
            medio - n * wm,
            a - n * wa * 0.5f,
        };
    }

    // Zapato de perfil, del tobillo a la punta.
    static Vector2[] Zapato(Vector2 tobillo, Vector2 punta, float tam)
    {
        float s = tam;
        return new[]
        {
            tobillo + new Vector2(-0.012f, 0.012f) * s,
            tobillo + new Vector2(0.01f, 0.013f) * s,
            punta + new Vector2(0f, 0.006f) * s,
            punta + new Vector2(0.012f, -0.002f) * s,
            punta + new Vector2(0f, -0.008f) * s,
            tobillo + new Vector2(-0.014f, -0.008f) * s,
        };
    }
}

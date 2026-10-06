using UnityEngine;

// Rellenos (parte de ControlManos), en la paleta de colores:
//  - CUBETA (botón arriba a la derecha de la paleta): encendida, lo que dibujas se va RELLENANDO EN VIVO,
//    como si una línea invisible uniera el inicio con tu dedo. Al soltar queda rellena aunque no la cierres
//    (por ejemplo una "C" o una nariz de lado). El relleno es del color elegido.
//  - TINTA INVISIBLE (botón arriba a la izquierda, a cuadritos): un "color" que no se ve. Sirve para cerrar
//    formas sin línea, rellenos sin borde, caminos para el títere o guías. Mientras editas (paleta abierta,
//    nodos, borrador) esas líneas se ven gris clarito.
//  - TOCAR DENTRO de una forma (abierta o cerrada) con la paleta abierta = rellenarla del color elegido
//    (con tinta invisible elegida = quitarle el relleno). Se puede deshacer.
public partial class ControlManos
{
    const string ClaveCubeta = "jcartoons_cubeta";
    static readonly Color ColorRellenoInicial = new Color(1f, 0.85f, 0.35f);

    Transform botonInvisible, botonCubeta;
    Material fondoCubeta, gotaCubeta;
    bool dedoEnBotonRelleno, dedoEnRelleno;
    int cubeta = -1; // -1 = aún no leído
    Color ultimoColorVisible = ColorRellenoInicial;

    public bool CubetaActiva
    {
        get
        {
            if (cubeta < 0)
                cubeta = PlayerPrefs.GetInt(ClaveCubeta, 0) != 0 ? 1 : 0;
            return cubeta == 1;
        }
        set
        {
            cubeta = value ? 1 : 0;
            PlayerPrefs.SetInt(ClaveCubeta, cubeta);
            PlayerPrefs.Save();
        }
    }

    // El color con el que se rellena: el elegido; si es tinta invisible, el último color que se veía.
    Color ColorDeRelleno => dibujo.ColorNuevo.a > 0.01f ? dibujo.ColorNuevo : ultimoColorVisible;

    // Una línea nueva con la cubeta encendida: se va rellenando mientras la dibujas.
    void AplicarCubeta(Trazo t)
    {
        if (t == null || !CubetaActiva)
            return;
        t.rellenoAbierto = true;
        t.relleno = true;
        Color c = ColorDeRelleno;
        t.colorFondo = new Color(c.r, c.g, c.b, 1f);
    }

    // Los dos botones de arriba de la paleta: tinta invisible (izquierda) y cubeta (derecha).
    void ArmarRellenoPaleta(Material negro)
    {
        // Tinta invisible: un círculo a cuadritos (como lo "transparente" en los programas de dibujo).
        botonInvisible = new GameObject("TintaInvisible").transform;
        botonInvisible.SetParent(paleta, false);
        botonInvisible.localPosition = new Vector3(-0.019f, 0.071f, -0.001f);
        botonInvisible.localScale = Vector3.one * 0.021f;
        DiscoPaleta(botonInvisible, negro, 1.12f, new Vector3(0f, 0f, 0.05f));
        DiscoPaleta(botonInvisible, ColorMaterial(Color.white), 1f, new Vector3(0f, 0f, 0.025f));
        var cuadros = new System.Collections.Generic.List<Vector2[]>();
        for (int fy = 0; fy < 4; fy++)
            for (int fx = 0; fx < 4; fx++)
            {
                if ((fx + fy) % 2 == 1)
                    continue;
                float x0 = -0.3f + fx * 0.15f, y0 = -0.3f + fy * 0.15f;
                cuadros.Add(new[] { new Vector2(x0, y0), new Vector2(x0, y0 + 0.15f), new Vector2(x0 + 0.15f, y0 + 0.15f), new Vector2(x0 + 0.15f, y0) });
            }
        IconoPaleta(botonInvisible, cuadros, ColorMaterial(new Color(0.7f, 0.72f, 0.76f)));

        // Cubeta: un balde con su asa y una gota del color de relleno.
        botonCubeta = new GameObject("Cubeta").transform;
        botonCubeta.SetParent(paleta, false);
        botonCubeta.localPosition = new Vector3(0.019f, 0.071f, -0.001f);
        botonCubeta.localScale = Vector3.one * 0.021f;
        fondoCubeta = ColorMaterial(Color.white);
        DiscoPaleta(botonCubeta, negro, 1.12f, new Vector3(0f, 0f, 0.05f));
        DiscoPaleta(botonCubeta, fondoCubeta, 1f, new Vector3(0f, 0f, 0.025f));
        var balde = new System.Collections.Generic.List<Vector2[]>
        {
            new[] { new Vector2(-0.24f, 0.08f), new Vector2(0.16f, 0.08f), new Vector2(0.11f, -0.28f), new Vector2(-0.19f, -0.28f) },
        };
        LineaIcono(balde, new[] { new Vector2(-0.22f, 0.1f), new Vector2(-0.16f, 0.27f), new Vector2(0.08f, 0.27f), new Vector2(0.14f, 0.1f) }, 0.05f);
        IconoPaleta(botonCubeta, balde, negro);
        gotaCubeta = ColorMaterial(ColorRellenoInicial);
        var gota = new System.Collections.Generic.List<Vector2[]>
        {
            Circulo(new Vector2(0.27f, -0.14f), 0.09f),
            new[] { new Vector2(0.27f, 0.03f), new Vector2(0.19f, -0.11f), new Vector2(0.35f, -0.11f) },
        };
        IconoPaleta(botonCubeta, gota, gotaCubeta);
    }

    void IconoPaleta(Transform padre, System.Collections.Generic.List<Vector2[]> partes, Material m)
    {
        var go = new GameObject("Icono");
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = MallaIcono(partes);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static void PonerColorMaterial(Material m, Color c)
    {
        if (m == null)
            return;
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }

    // Cada cuadro con la paleta abierta: cómo se ven los botones y si el dedo los toca.
    // Devuelve true si el dedo está en uno de los dos botones.
    bool ActualizarRellenoPaleta(Vector3 punta, bool dedoValido)
    {
        if (botonCubeta == null || botonInvisible == null)
            return false;
        if (dibujo.ColorNuevo.a > 0.01f)
            ultimoColorVisible = dibujo.ColorNuevo;
        PonerColorMaterial(fondoCubeta, CubetaActiva ? AmarilloOpcion : Color.white);
        PonerColorMaterial(gotaCubeta, ColorDeRelleno);
        bool invisible = dibujo.ColorNuevo.a < 0.01f;
        botonInvisible.localScale = Vector3.one * 0.021f * (invisible ? 1.3f : 1f);
        if (!dedoValido)
        {
            dedoEnBotonRelleno = false;
            return false;
        }
        bool enInvisible = Vector3.Distance(punta, botonInvisible.position) < 0.012f;
        bool enCubeta = Vector3.Distance(punta, botonCubeta.position) < 0.012f;
        if (!enInvisible && !enCubeta)
        {
            dedoEnBotonRelleno = false;
            return false;
        }
        if (!dedoEnBotonRelleno)
        {
            if (enInvisible)
            {
                ElegirColor(Dibujo.TintaInvisible, -1, botonInvisible.position);
            }
            else
            {
                CubetaActiva = !CubetaActiva;
                Burbuja(botonCubeta.position, 1.1f);
                MostrarEtiqueta(CubetaActiva ? "Cubeta: Sí" : "Cubeta: No");
                if (paletaFija)
                    paletaFijaHasta = Mathf.Max(paletaFijaHasta, Time.time + 12f);
            }
        }
        dedoEnBotonRelleno = true;
        return true;
    }

    // Con la paleta abierta, tocar DENTRO de una forma (lejos de sus líneas) = rellenarla del color elegido.
    void RellenarAlTocar(Vector3 local, Vector3 punta)
    {
        Trazo dentro = null;
        float mejor = float.MaxValue;
        float tolerancia = 0.02f / Mathf.Max(1e-4f, dibujo.EscalaMundo);
        foreach (var t in dibujo.trazos)
        {
            if (!Dibujo.Editable(t) || t.oculto || t.Dibujando)
                continue;
            if (!t.DentroDeRelleno(local, tolerancia))
                continue;
            // Si hay varias (una dentro de otra), la más pequeña (la que tocas de verdad).
            var b = t.GetComponent<MeshFilter>();
            float tam = b != null && b.sharedMesh != null ? b.sharedMesh.bounds.size.sqrMagnitude : 0f;
            if (tam < mejor)
            {
                mejor = tam;
                dentro = t;
            }
        }
        if (dentro == null)
        {
            dedoEnRelleno = false;
            return;
        }
        if (!dedoEnRelleno)
        {
            bool quitar = dibujo.ColorNuevo.a < 0.01f;
            dibujo.RellenarConColor(dentro, dibujo.ColorNuevo);
            Burbuja(punta, 1.2f);
            MostrarEtiqueta(quitar ? "Relleno quitado" : "Relleno");
        }
        dedoEnRelleno = true;
    }
}

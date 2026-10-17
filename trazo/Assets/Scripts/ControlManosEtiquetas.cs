using UnityEngine;

// ETIQUETAS AL PASAR EL DEDO (parte de ControlManos, v38): en la paleta, si dejas el índice derecho cerquita de un
// botón o de un color un cuarto de segundo, sale encima una etiquetita de vidrio con su nombre (como el "hover" de
// los botones en la web). La burbujita secreta no tiene etiqueta.
public partial class ControlManos
{
    const float CercaBoton = 0.022f;       // más allá del radio de toque de cada botón

    // El nombre de cada botón de la paleta (por el nombre de su objeto). Null = sin etiqueta.
    static string NombreBotonPaleta(string objeto)
    {
        switch (objeto)
        {
            case "TintaInvisible": return "Tinta invisible";
            case "Linea": return "Línea";
            case "Linea0": return "Temblor";
            case "Linea1": return "Hebras";
            case "Linea2": return "Grosor vivo";
            case "Linea3": return "Ciclo del temblor";
            case "Linea4": return "Suavidad";
            case "Linea5": return "Velocidad del temblor";
            case "Linea6": return "Imán";
            case "Linea7": return "Halo";
            case "Linea8": return "Lápiz";
            case "Linea9": return "Uniones";
            case "Relleno": return "Relleno";
            case "Relleno0": return "Textura";
            case "Relleno1": return "Cubeta";
            case "Relleno2": return "Velocidad de la textura";
            default: return null;
        }
    }

    // Cada cuadro con la paleta abierta: propone la etiqueta del botón o color más cercano al dedo.
    // v42: la muestra EtiquetasBotones (igual para todos los botones); si el nombre también se puede decir ("Halo",
    // "Rojo"...), lleva el microfonito y es la palabra de la voz.
    void ActualizarEtiquetaHover(bool abierta)
    {
        if (!abierta || paleta == null || !Der.valida)
            return;
        Transform mejor = null;
        string nombre = null;
        Vector3 punta = Der.indice;
        float mejorD = float.MaxValue;
        foreach (var b in botonesPaleta)
        {
            if (b.t == null || !b.t.gameObject.activeInHierarchy)
                continue;
            string n = NombreBotonPaleta(b.t.name);
            if (n == null)
                continue;
            float d = Vector3.Distance(punta, b.t.position);
            if (d < b.radioToque + CercaBoton && d < mejorD)
            {
                mejorD = d;
                mejor = b.t;
                nombre = n;
            }
        }
        for (int i = 0; i < botonesColor.Count && i < ColoresPaleta.Length; i++)
        {
            float d = Vector3.Distance(punta, botonesColor[i].position);
            if (d < 0.014f + CercaBoton * 0.6f && d < mejorD)
            {
                mejorD = d;
                mejor = botonesColor[i];
                nombre = NombreColor(ColoresPaleta[i]);
            }
        }
        if (botonHex != null)
        {
            float d = Vector3.Distance(punta, botonHex.position);
            if (d < 0.014f + CercaBoton * 0.6f && d < mejorD)
            {
                mejorD = d;
                mejor = botonHex;
                nombre = "Color hexadecimal (#)";
            }
        }
        if (mejor == null)
            return;
        string palabra;
        bool voz = EsPalabraDeVoz(nombre, out palabra);
        // Encima del botón (en el plano de la paleta), un poquito hacia ti.
        EtiquetasBotones.Proponer(mejor, mejorD, voz ? palabra : Idioma.T(nombre), voz,
                                  mejor.position + paleta.up * 0.021f - paleta.forward * 0.004f, paleta.rotation);
    }
}

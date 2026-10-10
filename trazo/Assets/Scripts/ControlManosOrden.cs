using System.Collections.Generic;
using UnityEngine;

// Frente / fondo dentro de una capa (parte de ControlManos). Con líneas encantadas en su hoja 2D agarradas
// (el grupo del lazo, o moviéndolas con el pellizco):
//  - EMPUJA la mano unos 3 cm hacia adentro de la hoja = pasan DETRÁS de la siguiente línea que tocan.
//  - TIRA de la mano unos 3 cm hacia ti = pasan DELANTE.
// Las líneas no salen de su hoja: solo cambia quién tapa a quién. Cada paso suena y el cartel dice el puesto.
public partial class ControlManos
{
    const float PasoEmpujon = 0.03f;   // metros de mano por cada escalón
    Trazo hojaEmpujon;                 // la línea agarrada (su hoja manda); null = sin frente/fondo
    float profEmpujon;                 // profundidad de referencia de la mano (+ = hacia adentro de la hoja)
    bool hayEmpujon;
    readonly List<Trazo> lineasEmpujon = new List<Trazo>();

    void EmpezarEmpujon(Trazo t)
    {
        hayEmpujon = false;
        hojaEmpujon = t != null && t.EnHoja ? t : null;
    }

    // Qué tan adentro de la hoja de "t" está este punto (metros): + = más lejos de ti que la hoja.
    bool ProfundidadEnHoja(Trazo t, Vector3 mundo, out float prof)
    {
        prof = 0f;
        if (t == null || !t.EnHoja || Cabeza == null)
            return false;
        Transform raiz = dibujo.transform;
        Vector3 punto = raiz.TransformPoint(t.hojaPunto);
        Vector3 n = raiz.TransformDirection(t.hojaNormal).normalized;
        if (Vector3.Dot(Cabeza.position - punto, n) > 0f)
            n = -n; // que apunte lejos de ti
        prof = Vector3.Dot(mundo - punto, n);
        return true;
    }

    // Mientras mueves líneas con el pellizco.
    void OrdenarConEmpujon(List<Trazo> lineas, Trazo agarrada, ref bool deshacerPendiente)
    {
        if (hojaEmpujon == null || hojaEmpujon != agarrada)
            return;
        Empujon(Der.PuntoPellizco, lineas, ref deshacerPendiente);
    }

    // Mientras mueves el grupo del lazo.
    void OrdenarConEmpujonGrupo(Vector3 punta)
    {
        if (hojaEmpujon == null)
            return;
        lineasEmpujon.Clear();
        foreach (var g in grupo)
            if (g.t != null && !lineasEmpujon.Contains(g.t))
                lineasEmpujon.Add(g.t);
        Empujon(punta, lineasEmpujon, ref deshacerGrupoPendiente);
    }

    void Empujon(Vector3 mundo, List<Trazo> lineas, ref bool deshacerPendiente)
    {
        float prof;
        if (lineas.Count == 0 || !ProfundidadEnHoja(hojaEmpujon, mundo, out prof))
            return;
        if (!hayEmpujon)
        {
            profEmpujon = prof;
            hayEmpujon = true;
            return;
        }
        float dif = prof - profEmpujon;
        if (Mathf.Abs(dif) < PasoEmpujon)
            return;
        int paso = dif > 0f ? -1 : 1; // hacia adentro de la hoja = atrás
        profEmpujon += dif > 0f ? PasoEmpujon : -PasoEmpujon;
        if (deshacerPendiente)
        {
            dibujo.GuardarParaDeshacer();
            deshacerPendiente = false;
        }
        int puesto, total;
        if (dibujo.CambiarOrden(lineas, paso, out puesto, out total))
        {
            Burbuja(mundo, paso > 0 ? 1.25f : 0.8f);
            dibujo.Mensaje((paso > 0 ? "Adelante · " : "Atrás · ") + puesto + " de " + total);
        }
        else
        {
            dibujo.Mensaje(paso > 0 ? "Ya está al frente" : "Ya está al fondo");
        }
    }
}

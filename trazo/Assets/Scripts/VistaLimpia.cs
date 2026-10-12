using UnityEngine;

// VISTA LIMPIA (v39): esconde lo que siempre está a la vista: las dos pastillitas (capas arriba, mesa abajo), el
// parlante con los íconos de los instrumentos y las letras del cartel de cómic (nombres de los gestos y avisos).
// Se recuerda al cerrar la app.
//  - Botón "Vista limpia" del menú del reloj.
//  - Gesto rápido: DOBLE PUÑO con la mano DERECHA (cerrar, abrir y cerrar otra vez en menos de un segundo).
//    Ver ControlManosVistaLimpia.cs.
// Con la vista limpia, las capas se abren con el botón Capas del reloj y la mesa con el botón Mesa. Los avisos
// siguen saliendo en el menú del reloj (abajo, en letras pequeñas).
public static class VistaLimpia
{
    const string Clave = "jcartoons_vista_limpia";
    const float VerAvisoCambio = 2.5f;   // segundos que se ve el aviso "Vista limpia" al ponerla

    static int activa = -1;   // -1 = aún no leída
    static float cambio = -10f;

    public static bool Activa
    {
        get
        {
            if (activa < 0)
                activa = PlayerPrefs.GetInt(Clave, 0) == 1 ? 1 : 0;
            return activa == 1;
        }
    }

    // ¿El cartel de cómic se esconde ahora? (Al ponerla, su propio aviso se ve un momento.)
    public static bool EsconderCartel => Activa && Time.time - cambio > VerAvisoCambio;

    public static void Alternar()
    {
        activa = Activa ? 0 : 1;
        cambio = Time.time;
        PlayerPrefs.SetInt(Clave, activa);
        PlayerPrefs.Save();
        var control = ControlManos.Instancia;
        if (control != null && control.dibujo != null)
            control.dibujo.Mensaje(activa == 1 ? "Vista limpia (doble puño derecho para volver)" : "Menús fijos de vuelta");
    }
}

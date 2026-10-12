using UnityEngine;

// PANELES FLEXIBLES (v41, experimento de Juan Carlos). Las dos pastillitas, la de las capas ("Capa 2 · de 5") y la de
// la mesa ("Fotograma 12 · 0:01"), ya no tienen un lugar fijo:
//  - PELLIZCA una pastilla (sin jalarla) y llévala ARRIBA o ABAJO de tu vista. Al soltarla vuela a su sitio nuevo.
//  - ARRIBA se jala HACIA ABAJO y su panel se despliega hacia abajo; ABAJO se jala HACIA ARRIBA y se despliega hacia
//    arriba. Así las capas pueden ir abajo (las ves desde arriba, con otra perspectiva) y la mesa arriba (con sus
//    pestañas y páginas DEBAJO del reproductor).
//  - Las dos en la misma orilla: las pastillas se ponen lado a lado (capas a la izquierda, mesa a la derecha).
//  - Si abres una y luego la otra desde la misma orilla, se acomodan UNA ARRIBA Y OTRA ABAJO: la segunda "hace
//    espacio" y su pastilla salta a la otra orilla mientras la primera está abierta.
//  - Se recuerda al cerrar la app. (Antes, v38-v40: capas siempre arriba y mesa siempre abajo.)
public static class PanelesFlexibles
{
    const string ClaveCapas = "jcartoons_capas_abajo", ClaveMesa = "jcartoons_mesa_arriba";
    const float SubeArriba = 9f;            // la orilla de arriba: grados más arriba que el cartel de cómic
    const float BajaAbajo = 30f;            // la de abajo: grados debajo de hacia donde miras
    const float DistanciaPastilla = 0.45f;  // al alcance del dedo
    const float Separa = 0.05f;             // juntas: metros del centro a cada pastilla

    // El cartel de cómic va un poco arriba de tu vista (0.165 m arriba a 0.6 m): estos grados.
    public static readonly float ArribaCartel = Mathf.Atan2(0.165f, 0.6f) * Mathf.Rad2Deg;

    static int capasAbajo = -1, mesaArriba = -1;
    static float juntas;
    static int cuadroJuntas = -10;
    static bool juntasLista;

    // Dónde las dejaste (su "casa").
    public static bool CapasArriba
    {
        get
        {
            if (capasAbajo < 0)
                capasAbajo = PlayerPrefs.GetInt(ClaveCapas, 0) == 1 ? 1 : 0;
            return capasAbajo == 0;
        }
    }

    public static bool MesaArriba
    {
        get
        {
            if (mesaArriba < 0)
                mesaArriba = PlayerPrefs.GetInt(ClaveMesa, 0) == 1 ? 1 : 0;
            return mesaArriba == 1;
        }
    }

    public static void PonerCapasArriba(bool arriba)
    {
        capasAbajo = arriba ? 0 : 1;
        PlayerPrefs.SetInt(ClaveCapas, capasAbajo);
        PlayerPrefs.Save();
    }

    public static void PonerMesaArriba(bool arriba)
    {
        mesaArriba = arriba ? 1 : 0;
        PlayerPrefs.SetInt(ClaveMesa, mesaArriba);
        PlayerPrefs.Save();
    }

    // Dónde está abierto ahora cada panel: 1 = arriba, -1 = abajo, 0 = cerrado (o la mesa fija en el aire).
    static int OrillaCapas => CapasProfundidad.OrillaAbierta;
    static int OrillaMesa => PanelArriba.Principal != null ? PanelArriba.Principal.OrillaAbierta : 0;

    // Dónde está AHORA cada pastilla: en su casa, salvo que el otro panel esté abierto justo ahí (entonces, en la
    // otra orilla: así los dos paneles quedan uno arriba y otro abajo).
    public static bool CapasPastillaArriba => OrillaMesa == (CapasArriba ? 1 : -1) ? !CapasArriba : CapasArriba;
    public static bool MesaPastillaArriba => OrillaCapas == (MesaArriba ? 1 : -1) ? !MesaArriba : MesaArriba;

    // ¿Las dos pastillas se ven en la misma orilla? (de 0 a 1, suave: así se apartan sin saltos)
    public static float Juntas
    {
        get
        {
            if (cuadroJuntas != Time.frameCount)
            {
                cuadroJuntas = Time.frameCount;
                bool lasDos = !CapasProfundidad.Abierta && (PanelArriba.Principal == null || !PanelArriba.Principal.MesaVisible);
                float meta = lasDos && CapasArriba == MesaArriba ? 1f : 0f;
                if (!juntasLista)
                {
                    juntasLista = true;
                    juntas = meta;
                }
                juntas = Mathf.MoveTowards(juntas, meta, Time.deltaTime * 4f);
            }
            return juntas;
        }
    }

    // Las pastillas: capas a la izquierda y mesa a la derecha cuando van juntas.
    public static void PoseCapas(Transform cabeza, out Vector3 pos, out Quaternion rot)
    {
        PoseOrilla(cabeza, CapasPastillaArriba, -Separa * Juntas, out pos, out rot);
    }

    public static void PoseMesa(Transform cabeza, out Vector3 pos, out Quaternion rot)
    {
        PoseOrilla(cabeza, MesaPastillaArriba, Separa * Juntas, out pos, out rot);
    }

    // Una pastilla en la orilla de arriba o de abajo de tu vista (sigue al cartel de cómic), corrida "lado" metros.
    public static void PoseOrilla(Transform cabeza, bool arriba, float lado, out Vector3 pos, out Quaternion rot)
    {
        Vector3 p;
        Quaternion q;
        CartelArriba.Pose(cabeza, out p, out q);
        Vector3 ojos = cabeza.position;
        Vector3 dir = p - ojos;
        if (dir.sqrMagnitude < 1e-6f)
            dir = cabeza.forward;
        float grados = arriba ? -SubeArriba : ArribaCartel + BajaAbajo;
        dir = Quaternion.AngleAxis(grados, q * Vector3.right) * dir.normalized;
        pos = ojos + dir * DistanciaPastilla;
        rot = Quaternion.LookRotation(pos - ojos, Vector3.up);
        if (Mathf.Abs(lado) > 1e-5f)
        {
            pos += rot * Vector3.right * lado;
            rot = Quaternion.LookRotation(pos - ojos, Vector3.up);
        }
    }
}

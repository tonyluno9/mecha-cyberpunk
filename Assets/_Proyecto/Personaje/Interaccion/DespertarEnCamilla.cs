using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Interaccion
{
    /// <summary>
    /// T-1628: al despertar en la clínica del Doc (EventosJugador.Desperto, después de caer) el jugador juega PJ01_Camilla_Acostarse y se levanta con
    /// PJ01_Dano_Levantarse_BocaArriba (el controlador los encadena), en el punto donde el Doc lo dejó. Si no hay camilla en la escena el clip se juega igual donde apareció.
    /// Se instala solo (suscripción estática al evento) y agrega este componente al jugador la primera vez que hace falta.
    /// </summary>
    public class DespertarEnCamilla : MonoBehaviour
    {
        private const int CuadrosEspera = 2;
        private const int CuadrosReintento = 30;

        private Coroutine rutina;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Instalar()
        {
            EventosJugador.Desperto -= AlDespertar;
            EventosJugador.Desperto += AlDespertar;
        }

        private static void AlDespertar()
        {
            Despertar();
        }

        /// <summary>Lanza la secuencia en el jugador actual. False si no hay jugador a pie. Sirve también para depuración.</summary>
        public static bool Despertar()
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null)
            {
                return false;
            }
            DespertarEnCamilla d = jugador.GetComponent<DespertarEnCamilla>();
            if (d == null)
            {
                d = jugador.gameObject.AddComponent<DespertarEnCamilla>();
            }
            if (d.rutina != null)
            {
                d.StopCoroutine(d.rutina);
            }
            d.rutina = d.StartCoroutine(d.Secuencia());
            return true;
        }

        // Espera a que el Doc coloque al jugador y le restaure la vida y lanza el gesto; reintenta si el visual aún estaba caído.
        private IEnumerator Secuencia()
        {
            for (int i = 0; i < CuadrosEspera; i++)
            {
                yield return null;
            }
            VisualPersonaje visual = GetComponentInChildren<VisualPersonaje>(true);
            for (int i = 0; i < CuadrosReintento && visual != null; i++)
            {
                if (visual.AcostarseEnCamilla())
                {
                    Debug.Log("[Personaje] Despierta en la camilla: PJ01_Camilla_Acostarse y se levanta.", this);
                    rutina = null;
                    yield break;
                }
                yield return null;
            }
            Debug.LogWarning("[Personaje] No pudo jugar el clip de la camilla (sin visual o sin gesto en el controlador).", this);
            rutina = null;
        }
    }
}

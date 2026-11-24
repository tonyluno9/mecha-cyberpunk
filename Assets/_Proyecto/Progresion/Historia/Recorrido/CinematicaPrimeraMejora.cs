using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Armas;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-cinematicas-narracion-24nov: la cinemática de Tino (C05, videos 25–27: golpea la pistola, la avienta y el jugador la atrapa) sale una
    /// vez, en el momento en que se pone la primera mejora de un arma en el banco de Tino. Mira el arsenal sin tocar el director de la historia:
    /// al aparecer el arsenal anota si ya traía mejora (partida cargada: no sale) y solo dispara si la mejora aparece con el banco abierto.
    /// Solo con la historia en marcha (<see cref="DirectorRecorrido"/> iniciado). T90: al empezar la cinemática el banco se cierra solo
    /// (<see cref="MechaCyberpunk.UI.Hud.CierreMenusCinematica"/>) y al terminar el jugador queda en la calle con control, sin el banco encima.
    /// </summary>
    public class CinematicaPrimeraMejora : MonoBehaviour
    {
        public const string IdCinematica = "C05";
        private const float SegundosEntreMiradas = 0.2f;

        private static CinematicaPrimeraMejora instancia;

        /// <summary>True si ya salió en esta sesión (o la partida ya traía mejora).</summary>
        public static bool Hecha { get; private set; }

        /// <summary>Veces que se lanzó (para las pruebas).</summary>
        public static int Lanzadas { get; private set; }

        private ArsenalJugador arsenal;
        private bool habiaMejora;
        private float proxima;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            instancia = null;
            Hecha = false;
            Lanzadas = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (instancia != null)
            {
                return;
            }
            var objeto = new GameObject("CinematicaPrimeraMejora");
            DontDestroyOnLoad(objeto);
            instancia = objeto.AddComponent<CinematicaPrimeraMejora>();
        }

        private void Update()
        {
            if (Hecha || Time.unscaledTime < proxima)
            {
                return;
            }
            proxima = Time.unscaledTime + SegundosEntreMiradas;
            DirectorRecorrido director = DirectorRecorrido.Actual;
            ArsenalJugador actual = Registro<ArsenalJugador>.Actual;
            if (director == null || !director.Iniciado || actual == null)
            {
                arsenal = null;
                return;
            }
            bool tiene = TieneMejora(actual);
            if (actual != arsenal)
            {
                arsenal = actual; // arsenal nuevo (escena o partida cargada): se toma como punto de partida
                habiaMejora = tiene;
                return;
            }
            if (!tiene || habiaMejora)
            {
                habiaMejora = tiene;
                return;
            }
            habiaMejora = true;
            if (!PestanasBancoTino.Abierto)
            {
                return; // la mejora llegó por otro lado (cargar partida, depuración): no es la de Tino
            }
            Hecha = true;
            Lanzadas++;
            Lanzar();
        }

        private static void Lanzar()
        {
            DatosRecorrido datos = Resources.Load<DatosRecorrido>(DatosRecorrido.NombreRecurso);
            DatosCinematica d = datos != null ? datos.Buscar(IdCinematica) : null;
            if (d == null || d.Clips.Count == 0)
            {
                Debug.LogWarning("[Historia] La cinemática de Tino (" + IdCinematica + ") no tiene videos: no sale.");
                return;
            }
            Debug.Log("[Historia] Primera mejora de arma: cinemática de Tino (" + IdCinematica + ").");
            CinematicaPendiente.Reproducir(d);
        }

        /// <summary>True si alguna arma (sin puños ni granada) tiene nivel 1 o más: la misma cuenta que usa la historia para "ya la mejoraste".</summary>
        public static bool TieneMejora(ArsenalJugador a)
        {
            if (a == null)
            {
                return false;
            }
            foreach (ArmaAPie arma in Enum.GetValues(typeof(ArmaAPie)))
            {
                if (arma != ArmaAPie.Punos && arma != ArmaAPie.Granada && a.Nivel(arma) >= 1)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-voces-personajes-23nov: pide la voz grabada de un peatón (3D, a la altura de la boca) con su <see cref="ReglaVozPeaton"/>: una frase a la vez por
    /// peatón, enfriamiento por momento, silencio común entre las ambientales de todos y nada lejos del jugador. La usan el puente de los peatones de la
    /// ciudad (IA, AnimacionCivilMundo) y la gente de banqueta (vendedores y corros, <see cref="GenteDeBanqueta"/>).
    /// </summary>
    public static class VozPeaton
    {
        private const float AlturaBoca = 1.6f;

        private static float ultimaAmbientalDeTodos = float.NegativeInfinity;

        /// <summary>El último ID que pidió un peatón (para pruebas y depuración).</summary>
        public static string UltimoId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            ultimaAmbientalDeTodos = float.NegativeInfinity;
            UltimoId = null;
        }

        /// <summary>Distancia (m, en el plano) del jugador a <paramref name="punto"/>; infinito si no hay jugador.</summary>
        public static float DistanciaAlJugador(Vector3 punto)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return float.PositiveInfinity;
            }
            Vector3 d = jugador.Raiz.position - punto;
            d.y = 0f;
            return d.magnitude;
        }

        /// <summary>True si el jugador va a pie (los peatones no saludan a un mecha ni a un carro).</summary>
        public static bool JugadorAPie()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Modo == ModoJugador.APie;
        }

        /// <summary>El peatón con voz <paramref name="voz"/> dice <paramref name="momento"/> desde <paramref name="pies"/>. True si sonó.</summary>
        public static bool Decir(ReglaVozPeaton regla, int voz, MomentoVozPeaton momento, Vector3 pies)
        {
            if (regla == null || DistanciaAlJugador(pies) > ReglaVozPeaton.DistanciaMaxima)
            {
                return false;
            }
            float ahora = Time.time;
            bool ambiental = ReglaVozPeaton.EsAmbiental(momento);
            if (ambiental && !ReglaVozPeaton.TocaAmbientalDeTodos(ahora, ultimaAmbientalDeTodos))
            {
                return false;
            }
            if (!regla.Intentar(ahora, momento))
            {
                return false;
            }
            if (ambiental)
            {
                ultimaAmbientalDeTodos = ahora;
            }
            UltimoId = ReglaVozPeaton.Id(voz, momento);
            Sonidos.ReproducirEn(UltimoId, pies + Vector3.up * AlturaBoca);
            return true;
        }
    }
}

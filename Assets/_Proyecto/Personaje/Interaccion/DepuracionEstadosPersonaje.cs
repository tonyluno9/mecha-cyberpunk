using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Interaccion
{
    /// <summary>T-1628: métodos estáticos para probar sentarse, camilla y colgarse en Play (por eval), sin armar la situación.</summary>
    public static class DepuracionEstadosPersonaje
    {
        /// <summary>Se sienta en el asiento libre más cercano (un AsientoInteractuable). False si no hay.</summary>
        public static bool SentarseEnMasCercano()
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null)
            {
                Debug.LogWarning("[Personaje] No hay jugador a pie.");
                return false;
            }
            AsientoInteractuable mejor = null;
            float menor = float.MaxValue;
            foreach (IInteractuable i in Registro<IInteractuable>.Todos)
            {
                AsientoInteractuable a = i as AsientoInteractuable;
                if (a == null || a.EstaOcupado)
                {
                    continue;
                }
                float d = (a.Posicion - jugador.transform.position).sqrMagnitude;
                if (d < menor)
                {
                    menor = d;
                    mejor = a;
                }
            }
            if (mejor == null)
            {
                Debug.LogWarning("[Personaje] No hay asiento libre en la escena.");
                return false;
            }
            return SentadoAPie.Sentar(mejor);
        }

        /// <summary>Se levanta del asiento si está sentado.</summary>
        public static void Levantarse()
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            SentadoAPie s = jugador != null ? jugador.GetComponent<SentadoAPie>() : null;
            if (s != null)
            {
                s.Levantar();
            }
        }

        /// <summary>Juega la secuencia de despertar en camilla donde esté el jugador.</summary>
        public static bool ForzarDespertarCamilla()
        {
            return DespertarEnCamilla.Despertar();
        }

        /// <summary>Se cuelga del borde de enfrente (o de uno imaginario a la altura de las manos).</summary>
        public static bool ForzarColgarse()
        {
            TreparAPie t = Trepar();
            return t != null && t.ForzarColgarse();
        }

        /// <summary>Sube al borde desde colgado.</summary>
        public static void SubirDeColgado()
        {
            TreparAPie t = Trepar();
            if (t != null)
            {
                t.SubirDeColgado();
            }
        }

        /// <summary>Se suelta del borde.</summary>
        public static void SoltarDeColgado()
        {
            TreparAPie t = Trepar();
            if (t != null)
            {
                t.SoltarDeColgado();
            }
        }

        private static TreparAPie Trepar()
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            return jugador != null ? jugador.GetComponent<TreparAPie>() : null;
        }
    }
}

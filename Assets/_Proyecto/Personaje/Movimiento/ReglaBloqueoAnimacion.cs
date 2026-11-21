using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// T-1493: qué estados del Animator del jugador lo dejan sin moverse (ni caminar, ni girar el cuerpo, ni saltar) hasta que terminan.
    /// Bloquean las animaciones largas que se ven mal si el cuerpo se mueve: subir y bajar de la moto y del carro, abrir puerta, botón, recoger, levantarse del piso.
    /// No bloquean golpes, disparos, recargar, daño corto, sacar o guardar arma, el teléfono, burla, presumir, manejar ni rodar (rodar se mueve sola).
    /// Lógica pura: la usa ControladorAPie con el estado actual del Animator.
    /// </summary>
    public static class ReglaBloqueoAnimacion
    {
        /// <summary>Lo más que puede bloquear un solo estado (s): tope de seguridad para no quedarse trabado si un estado no termina.</summary>
        public const float SegundosMaximos = 4f;

        /// <summary>Nombres de los estados del Animator (ConstructorControladorBase) que bloquean el movimiento.</summary>
        public static readonly string[] EstadosBloqueantes =
        {
            "MotoSubir", "MotoBajar", "CarroEntrar", "CarroSalir",
            "AbrirPuerta", "Boton", "Recoger",
            "LevantarseBocaArriba", "LevantarseBocaAbajo",
            // T-1505: el aterrizaje fuerte (PJ01_Aterrizaje_Fuerte, 1.47 s: sentadilla profunda y se levanta) no deja caminar ni saltar hasta terminar.
            "AterrizajeFuerte",
            // T-1540: el derribo por explosión (1.3 s, con root motion), el empujón (0.87 s, retrocede 0.55 m) y el tambaleo de una explosión cercana (1 s) no dejan caminar mientras duran.
            "DerriboExplosion", "DanoEmpujon", "DanoExplosionCercana",
        };

        private static readonly int[] HashesBloqueantes = CalcularHashes();

        private static int[] CalcularHashes()
        {
            int[] hashes = new int[EstadosBloqueantes.Length];
            for (int i = 0; i < hashes.Length; i++)
            {
                hashes[i] = Animator.StringToHash(EstadosBloqueantes[i]);
            }
            return hashes;
        }

        /// <summary>True si el estado con ese nombre bloquea el movimiento.</summary>
        public static bool EstadoBloquea(string nombre)
        {
            return nombre != null && System.Array.IndexOf(EstadosBloqueantes, nombre) >= 0;
        }

        /// <summary>True si el estado con ese hash corto (<c>shortNameHash</c>) bloquea el movimiento.</summary>
        public static bool HashBloquea(int shortNameHash)
        {
            return System.Array.IndexOf(HashesBloqueantes, shortNameHash) >= 0;
        }

        /// <summary>El movimiento se ignora si el estado bloquea y lleva menos del tope bloqueando seguido.</summary>
        public static bool DebeBloquear(bool estadoBloquea, float segundosBloqueando)
        {
            return estadoBloquea && segundosBloqueando < SegundosMaximos;
        }
    }
}

using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-962 (A3): root motion solo en las muertes y en levantarse del jugador. Los clips de Blender (PJ01_Muerte_*, PJ01_Dano_Levantarse_*) mueven el cuerpo 0.7 a 1.2 m:
    /// con Bake Into Pose el cuerpo quedaba lejos de la cápsula y, al levantarse (en la clínica tras el fundido o al salir volando de la moto), regresaba deslizándose a ella.
    /// Aquí el avance del clip mueve el CharacterController del jugador, así el cuerpo cae y se levanta con los pies fijos y la cápsula queda donde quedó el cuerpo.
    /// En los demás estados no hace nada (como Apply Root Motion apagado). Va en el objeto del Animator; lo agrega AnimacionJugador.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class RaizCaidaJugador : MonoBehaviour
    {
        private static readonly int[] EstadosConRaiz =
        {
            Animator.StringToHash("MuerteAdelante"), Animator.StringToHash("MuerteAtras"), Animator.StringToHash("MuerteDesmayo"),
            Animator.StringToHash("LevantarseBocaArriba"), Animator.StringToHash("LevantarseBocaAbajo"),
            // T-1540: muertes por explosión (3.1 m atrás), atropello (4 m) y caída; derribo por explosión (1.8 m); empujón (0.55 m) y explosión cercana (0.25 m).
            Animator.StringToHash("MuerteExplosion"), Animator.StringToHash("MuerteAtropello"), Animator.StringToHash("MuerteCaidaAltura"),
            Animator.StringToHash("DerriboExplosion"), Animator.StringToHash("DanoEmpujon"), Animator.StringToHash("DanoExplosionCercana"),
        };

        private Animator animador;
        private CharacterController cuerpo;

        /// <summary>Lo que ha movido a la cápsula desde que se configuró (m, en el plano), para las pruebas.</summary>
        public Vector3 Recorrido { get; private set; }

        /// <summary>T-1540: con true no mueve la cápsula (otro script ya la desplaza, como el derribo por un carro en AtropelloJugador).</summary>
        public bool Suspendida { get; set; }

        /// <summary>True si en ese estado de la capa base el clip mueve la cápsula.</summary>
        public static bool MueveLaRaiz(int estadoHash) => System.Array.IndexOf(EstadosConRaiz, estadoHash) >= 0;

        public void Configurar(CharacterController controlador)
        {
            cuerpo = controlador;
            Recorrido = Vector3.zero;
        }

        private void Awake()
        {
            animador = GetComponent<Animator>();
        }

        private void OnAnimatorMove()
        {
            if (Suspendida || cuerpo == null || !cuerpo.enabled || !cuerpo.gameObject.activeInHierarchy)
            {
                return;
            }
            if (!MueveLaRaiz(animador.GetCurrentAnimatorStateInfo(0).shortNameHash))
            {
                return;
            }
            Vector3 avance = animador.deltaPosition;
            avance.y = 0f;
            if (avance.sqrMagnitude <= 0f)
            {
                return;
            }
            cuerpo.Move(avance);
            Recorrido += avance;
        }
    }
}

using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Pasa el estado del jugador al Animator del modelo (T-400): Velocidad, EnPiso, Apuntando, Arma, KO y los disparadores
    /// Golpe, Disparo y Recarga. El modelo final se cambia sin tocar código mientras conserve esos nombres.
    /// </summary>
    public class AnimacionJugador : MonoBehaviour
    {
        private static readonly int IdVelocidad = Animator.StringToHash("Velocidad");
        private static readonly int IdEnPiso = Animator.StringToHash("EnPiso");
        private static readonly int IdApuntando = Animator.StringToHash("Apuntando");
        private static readonly int IdArma = Animator.StringToHash("Arma");
        private static readonly int IdKO = Animator.StringToHash("KO");
        private static readonly int IdGolpe = Animator.StringToHash("Golpe");
        private static readonly int IdDisparo = Animator.StringToHash("Disparo");
        private static readonly int IdRecarga = Animator.StringToHash("Recarga");

        [SerializeField] private Animator animador;

        private ControladorAPie controlador;
        private GolpeAPie golpe;
        private PistolaAPie pistola;
        private ArmasAPie armas;
        private VidaAPie vida;
        private FaseGolpe faseAnterior = FaseGolpe.Listo;
        private bool recargandoAntes;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            golpe = GetComponent<GolpeAPie>();
            pistola = GetComponent<PistolaAPie>();
            armas = GetComponent<ArmasAPie>();
            vida = GetComponent<VidaAPie>();
            if (animador == null)
            {
                animador = GetComponentInChildren<Animator>(true);
            }
            if (animador == null)
            {
                Debug.LogError("[Personaje] Falta el Animator del modelo en AnimacionJugador de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (pistola != null)
            {
                pistola.Disparo += AlDisparar;
            }
        }

        private void OnDisable()
        {
            if (pistola != null)
            {
                pistola.Disparo -= AlDisparar;
            }
        }

        private void Update()
        {
            if (!animador.gameObject.activeInHierarchy)
            {
                return;
            }
            animador.SetFloat(IdVelocidad, controlador.Velocidad.magnitude);
            animador.SetBool(IdEnPiso, controlador.EnPiso);
            animador.SetBool(IdApuntando, controlador.Apuntando);
            animador.SetInteger(IdArma, armas != null ? (int)armas.ArmaActiva : 0);
            animador.SetBool(IdKO, vida != null && vida.EstaKO);

            FaseGolpe fase = golpe != null ? golpe.Fase : FaseGolpe.Listo;
            if (fase == FaseGolpe.Preparando && faseAnterior != FaseGolpe.Preparando)
            {
                animador.SetTrigger(IdGolpe);
            }
            faseAnterior = fase;

            bool recargando = pistola != null && pistola.Recargando;
            if (recargando && !recargandoAntes)
            {
                animador.SetTrigger(IdRecarga);
            }
            recargandoAntes = recargando;
        }

        private void AlDisparar()
        {
            if (animador.gameObject.activeInHierarchy)
            {
                animador.SetTrigger(IdDisparo);
            }
        }
    }
}

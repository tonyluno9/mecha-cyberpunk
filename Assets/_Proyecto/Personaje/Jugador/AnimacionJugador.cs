using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Pasa el estado del jugador al Animator del modelo (T-400): Velocidad, EnPiso, Apuntando, Arma, KO y los disparadores
    /// Golpe, Disparo y Recarga. El modelo final se cambia sin tocar código mientras conserve esos nombres.
    /// </summary>
    public class AnimacionJugador : MonoBehaviour, IModeloCambiable
    {
        private static readonly int IdVelocidad = Animator.StringToHash("Velocidad");
        private static readonly int IdEnPiso = Animator.StringToHash("EnPiso");
        private static readonly int IdApuntando = Animator.StringToHash("Apuntando");
        private static readonly int IdArma = Animator.StringToHash("Arma");
        private static readonly int IdKO = Animator.StringToHash("KO");
        private static readonly int IdGolpe = Animator.StringToHash("Golpe");
        private static readonly int IdDisparo = Animator.StringToHash("Disparo");
        private static readonly int IdRecarga = Animator.StringToHash("Recarga");

        // Un impacto cuenta como del jugador si cae a esta distancia de su centro (m); el evento no dice a quién le pegaron.
        private const float DistanciaImpacto = 2.2f;
        private const float AlturaCentro = 1f;
        // Un golpe de esta fracción de la vida máxima o más se muestra como daño fuerte.
        private const float FraccionDanoFuerte = 0.25f;

        [SerializeField] private Animator animador;
        [Tooltip("Segundos que el jugador sigue tumbado después de despertar, antes de levantarse (s).")]
        [Min(0f)] [SerializeField] private float retardoLevantarse = ReglasAnimacion.RetardoLevantarse;

        private ControladorAPie controlador;
        private GolpeAPie golpe;
        private PistolaAPie pistola;
        private ArmasAPie armas;
        private VidaAPie vida;
        private CubiertaYRodada cubierta;
        private VisualPersonaje visual;
        private FaseGolpe faseAnterior = FaseGolpe.Listo;
        private bool recargandoAntes;
        private TipoArmaVisual tipoArmaAntes;
        private bool koAntes;
        private float segundosSinKO;
        private bool apuntandoAntes;
        private bool cubiertoAntes;
        private bool agachadoAntes;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            golpe = GetComponent<GolpeAPie>();
            pistola = GetComponent<PistolaAPie>();
            armas = GetComponent<ArmasAPie>();
            vida = GetComponent<VidaAPie>();
            cubierta = GetComponent<CubiertaYRodada>();
            visual = GetComponentInChildren<VisualPersonaje>(true);
            if (visual != null) { visual.RapidezGolpes = ReglasAnimacion.RapidezGolpeJugador; }
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

        public void AlCambiarModelo(Animator nuevo)
        {
            animador = nuevo;
            visual = nuevo != null ? nuevo.GetComponentInParent<VisualPersonaje>() : null;
            if (visual != null) { visual.RapidezGolpes = ReglasAnimacion.RapidezGolpeJugador; }
            enabled = nuevo != null;
        }

        private void OnEnable()
        {
            if (pistola != null)
            {
                pistola.Disparo += AlDisparar;
            }
            EventosPelea.ImpactoRecibido += AlRecibirImpacto;
        }

        private void OnDisable()
        {
            if (pistola != null)
            {
                pistola.Disparo -= AlDisparar;
            }
            EventosPelea.ImpactoRecibido -= AlRecibirImpacto;
        }

        private void Update()
        {
            if (!animador.gameObject.activeInHierarchy)
            {
                return;
            }
            animador.SetFloat(IdVelocidad, controlador.Velocidad.magnitude);
            animador.SetBool(IdEnPiso, controlador.EnPiso);
            if (visual == null)
            {
                animador.SetBool(IdApuntando, controlador.Apuntando);
            }
            bool desarmado = armas == null || armas.ArmaActiva == ArmaAPie.Punos;
            TipoArmaVisual tipoArma = ReglasAnimacion.TipoPorArma(desarmado, !desarmado && armas.ArmaActiva == ArmaAPie.Pistola);
            bool ko = vida != null && vida.EstaKO;
            if (visual == null)
            {
                animador.SetInteger(IdArma, (int)tipoArma);
                animador.SetBool(IdKO, ko);
            }
            if (visual != null)
            {
                // Solo al cambiar: así otras pruebas o herramientas pueden manejar el visual sin que este script lo pise cada cuadro.
                bool cubierto = cubierta != null && cubierta.Cubierto;
                // T-674: agachado (CTRL) usa PoseAgachado (cadera baja y rodillas dobladas) sobre el idle o AgachadoCaminar.
                bool agachado = cubierta != null && cubierta.AgachadoLibre;
                if (agachado != agachadoAntes) { visual.Agachado = agachado; agachadoAntes = agachado; }
                bool apuntando = controlador.Apuntando;
                if (apuntando != apuntandoAntes) { visual.Apuntando = apuntando; apuntandoAntes = apuntando; }
                if (tipoArma != tipoArmaAntes) { visual.TipoArma = tipoArma; tipoArmaAntes = tipoArma; }
                ActualizarCaido(ko, Time.deltaTime);
                if (cubierto != cubiertoAntes) { visual.Cubierto = cubierto; cubiertoAntes = cubierto; }
            }

            FaseGolpe fase = golpe != null ? golpe.Fase : FaseGolpe.Listo;
            if (fase == FaseGolpe.Preparando && faseAnterior != FaseGolpe.Preparando)
            {
                if (visual != null)
                {
                    visual.Golpear(false);
                }
                else
                {
                    animador.SetTrigger(IdGolpe);
                }
            }
            faseAnterior = fase;

            bool recargando = pistola != null && pistola.Recargando;
            if (recargando && !recargandoAntes)
            {
                animador.SetTrigger(IdRecarga);
            }
            recargandoAntes = recargando;
        }

        // Cae al instante (clip de muerte); al despertar (la clínica restaura la vida con la pantalla en negro) sigue tumbado un momento
        // y luego se levanta con Dano_Levantarse (T-562), para que se vea al abrirse el fundido.
        private void ActualizarCaido(bool ko, float dt)
        {
            segundosSinKO = ko ? 0f : segundosSinKO + dt;
            if (ko && !koAntes)
            {
                koAntes = true;
                visual.Caido = true;
            }
            else if (koAntes && ReglasAnimacion.DebeLevantarse(ko, segundosSinKO, retardoLevantarse))
            {
                koAntes = false;
                visual.Caido = false;
            }
        }

        private void AlDisparar()
        {
            if (!animador.gameObject.activeInHierarchy)
            {
                return;
            }
            if (visual != null)
            {
                visual.Disparar();
            }
            else
            {
                animador.SetTrigger(IdDisparo);
            }
        }

        private void AlRecibirImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (visual == null || objetivo != null || vida == null || (info.Punto - (transform.position + Vector3.up * AlturaCentro)).sqrMagnitude > DistanciaImpacto * DistanciaImpacto)
            {
                return;
            }
            visual.Herir(info.Direccion, vida.Maxima > 0f && info.Cantidad >= vida.Maxima * FraccionDanoFuerte);
        }
    }
}

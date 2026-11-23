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
        private static readonly int IdConImplante = Animator.StringToHash(ReglasAnimacion.ConImplante);

        // Un impacto cuenta como del jugador si cae a esta distancia de su centro (m); el evento no dice a quién le pegaron.
        private const float DistanciaImpacto = 2.2f;
        private const float AlturaCentro = 1f;
        // Un golpe de esta fracción de la vida máxima o más se muestra como daño fuerte (T-735: ReglasAnimacion.FraccionDanoFuerte).
        private const float FraccionDanoFuerte = ReglasAnimacion.FraccionDanoFuerte;

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
        private bool rodandoAntes;
        private ApuntadoIK apuntadoIK;
        private DetectorEscalones detectorEscalones;
        private EscalonesVisual escalonesAntes;
        private float conImplanteAplicado = -1f;

        /// <summary>T-739: el jugador lleva un implante de piernas (IMP01) o de brazos (IMP02) y camina y corre con los clips ConImplante (ServicioImplantes, contrato IImplantes de Core).</summary>
        public static bool LlevaImplanteDeLocomocion()
        {
            IImplantes implantes = Registro<IImplantes>.Actual;
            return implantes != null && (implantes.Nivel(EspacioImplante.Piernas) > 0 || implantes.Nivel(EspacioImplante.Brazos) > 0);
        }

        // T-681: hasta dónde se busca el punto de mira para llevarle el cañón (m).
        private const float AlcanceMiraVisual = 80f;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            golpe = GetComponent<GolpeAPie>();
            pistola = GetComponent<PistolaAPie>();
            armas = GetComponent<ArmasAPie>();
            vida = GetComponent<VidaAPie>();
            detectorEscalones = GetComponent<DetectorEscalones>();
            if (detectorEscalones == null)
            {
                detectorEscalones = gameObject.AddComponent<DetectorEscalones>(); // T-1515
            }
            cubierta = GetComponent<CubiertaYRodada>();
            visual = GetComponentInChildren<VisualPersonaje>(true);
            if (visual != null)
            {
                visual.RapidezGolpes = ReglasAnimacion.RapidezGolpeJugador;
                visual.GuardiaTrasGolpe = true; // T-985: PJ01_Punos_Guardia entre golpes
                visual.DanoLateral = true; // T-1515: PJ01_Dano_Izquierda y Derecha
                visual.RapidezMuerte = ReglasAnimacion.RapidezMuerteJugador;
            }
            ClipsDeTony(visual);
            apuntadoIK = visual != null ? visual.GetComponentInChildren<ApuntadoIK>(true) : null;
            if (animador == null)
            {
                animador = GetComponentInChildren<Animator>(true);
            }
            if (animador == null)
            {
                Debug.LogError("[Personaje] Falta el Animator del modelo en AnimacionJugador de " + name, this);
                enabled = false;
                return;
            }
            PonerRaizCaida(animador);
        }

        /// <summary>T-962: las muertes y levantarse de Blender mueven la cápsula del jugador con su root motion (<see cref="RaizCaidaJugador"/>).</summary>
        private void PonerRaizCaida(Animator a)
        {
            if (a == null)
            {
                return;
            }
            RaizCaidaJugador raiz = a.GetComponent<RaizCaidaJugador>();
            if (raiz == null)
            {
                raiz = a.gameObject.AddComponent<RaizCaidaJugador>();
            }
            raiz.Configurar(GetComponent<CharacterController>());
        }

        /// <summary>
        /// T-724: caminar y correr del jugador son clips de Tony. T-1484: su rapidez sigue a la velocidad del cuerpo (0.7 a 1.6) para que los pies no patinen,
        /// y el IK de pies solo corrige desniveles del piso.
        /// </summary>
        public static void ClipsDeTony(VisualPersonaje visual)
        {
            if (visual == null)
            {
                return;
            }
            // T-53: el cuerpo vive en VisualPersonaje.ConfigurarComoJugador para que los NPC humanos usen exactamente lo mismo.
            visual.ConfigurarComoJugador();
        }

        public void AlCambiarModelo(Animator nuevo)
        {
            animador = nuevo;
            conImplanteAplicado = -1f;
            visual = nuevo != null ? nuevo.GetComponentInParent<VisualPersonaje>() : null;
            if (visual != null)
            {
                visual.RapidezGolpes = ReglasAnimacion.RapidezGolpeJugador;
                visual.GuardiaTrasGolpe = true; // T-985: PJ01_Punos_Guardia entre golpes
                visual.DanoLateral = true; // T-1515: PJ01_Dano_Izquierda y Derecha
                visual.RapidezMuerte = ReglasAnimacion.RapidezMuerteJugador;
            }
            ClipsDeTony(visual);
            PonerRaizCaida(nuevo);
            apuntadoIK = nuevo != null ? nuevo.GetComponent<ApuntadoIK>() : null;
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
            // T-56 (T56): con prótesis el jugador usa los mismos clips que sin ellas; el parámetro se queda en 0.
            float conImplante = 0f;
            if (conImplante != conImplanteAplicado)
            {
                animador.SetFloat(IdConImplante, conImplante);
                conImplanteAplicado = conImplante;
            }
            animador.SetBool(IdEnPiso, controlador.EnPiso);
            if (visual == null)
            {
                animador.SetBool(IdApuntando, controlador.Apuntando);
            }
            bool desarmado = armas == null || armas.ArmaActiva == ArmaAPie.Punos;
            TipoArmaVisual tipoArma = ReglasAnimacion.TipoPorArma(desarmado, !desarmado && ReglasArmaAPie.UsaPoseDePistola(armas.ArmaActiva));
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
                // T-683: agachado (CTRL) usa los clips de agachado (AgachadoQuieto y AgachadoCaminar) con los brazos sueltos.
                bool agachado = cubierta != null && cubierta.AgachadoLibre;
                if (agachado != agachadoAntes) { visual.Agachado = agachado; agachadoAntes = agachado; }
                bool apuntando = controlador.Apuntando;
                ActualizarMira(apuntando);
                if (apuntando != apuntandoAntes) { visual.Apuntando = apuntando; apuntandoAntes = apuntando; }
                if (tipoArma != tipoArmaAntes) { visual.TipoArma = tipoArma; tipoArmaAntes = tipoArma; }
                visual.PidiendoCorrer = controlador.PidiendoCorrer; // T-1561: Shift saca del modo pelea y guarda el arma larga
                ActualizarCaido(ko, Time.deltaTime);
                EscalonesVisual escalones = detectorEscalones.Tipo > 0 ? EscalonesVisual.Subiendo : detectorEscalones.Tipo < 0 ? EscalonesVisual.Bajando : EscalonesVisual.Ninguno;
                if (escalones != escalonesAntes) { visual.Escalones = escalones; escalonesAntes = escalones; }
                if (cubierto != cubiertoAntes) { visual.Cubierto = cubierto; cubiertoAntes = cubierto; }
            }

            // T-675: el esquive corta la reacción al golpe recibido.
            bool rodando = cubierta != null && cubierta.Rodando;
            if (rodando && !rodandoAntes && visual != null)
            {
                visual.CortarReaccion();
                visual.Rodar(); // T-733: Loco_Rodar (1 s); la cápsula avanza 2.2 m en ese segundo
            }
            rodandoAntes = rodando;

            FaseGolpe fase = golpe != null ? golpe.Fase : FaseGolpe.Listo;
            if (fase == FaseGolpe.Preparando && faseAnterior != FaseGolpe.Preparando)
            {
                if (visual != null)
                {
                    visual.Golpear(golpe.EsGolpeFinal); // T-1487: el último golpe del combo usa GolpeFuerte
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
                // T-1308: con el visual nuevo la recarga saca el arma si estaba guardada (si no, el Animator ignoraba el disparador).
                if (visual != null)
                {
                    visual.Recargar(pistola.Cargador != null ? pistola.Cargador.TiempoRecarga : 0f);
                }
                else
                {
                    animador.SetTrigger(IdRecarga);
                }
            }
            recargandoAntes = recargando;
            // T-1561: echar a correr con una recarga en curso: la pistola sigue con PJ01_Pistola_RecargarCaminar (la capa de brazos); un arma larga cancela la recarga y se guarda.
            if (visual != null && recargando && controlador.PidiendoCorrer && !ReglasTransicionesT1561.RecargaSigueAlCorrer(tipoArma))
            {
                pistola.CancelarRecargaActual();
                visual.Interrumpir();
            }
        }

        // T-681: al apuntar, el cañón del arma en la mano mira al punto que marca el centro de la cámara de juego.
        private void ActualizarMira(bool apuntando)
        {
            if (apuntadoIK == null)
            {
                return;
            }
            if (apuntando && pistola != null && controlador.Camara != null)
            {
                apuntadoIK.PonerPuntoMira(pistola.PuntoDeMira(AlcanceMiraVisual));
            }
            else
            {
                apuntadoIK.QuitarPuntoMira();
            }
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
                if (visual.GranadaEnCurso)
                {
                    return; // T-1561: la granada ya juega su propia animación de lanzar; el retroceso de la pistola no la corta
                }
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
            // T-1561: un golpe de cierta fuerza corta el puño, la recarga y el lanzamiento en curso (vuelven a su reposo); apuntar sigue (la capa de daño estremece la pose).
            if (ReglasTransicionesT1561.DanoInterrumpe(info.Cantidad, vida.Maxima))
            {
                visual.Interrumpir();
                golpe?.InterrumpirPorDano();
                pistola?.CancelarRecargaActual();
            }
            // T-1540: una explosión elige entre tambaleo, derribo (y levantarse) y muerte volando; un golpe vertical hacia abajo que mata es una caída de altura.
            // El motivo se deja antes de que ActualizarCaido ponga Caido (ahí se elige el clip de muerte).
            bool explosion = info.Tipo == TipoDanio.Explosion;
            if (explosion)
            {
                float cantidad = resultado.DanioAplicado > 0f ? resultado.DanioAplicado : info.Cantidad;
                visual.RecibirExplosion(info.Direccion, cantidad, vida.Maxima, resultado.CausoKO);
                return;
            }
            if (resultado.CausoKO)
            {
                MotivoDanioVisual motivo = ReglasAnimacion.MotivoDe(false, info.Tipo == TipoDanio.Golpe, info.Direccion);
                if (motivo != MotivoDanioVisual.Normal)
                {
                    visual.MotivoDanio = motivo;
                }
            }
            visual.Herir(info.Direccion, vida.Maxima > 0f && info.Cantidad >= vida.Maxima * FraccionDanoFuerte);
        }
    }
}

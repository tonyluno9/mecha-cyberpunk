using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1506: la caída especial de un agente de Nova cuando llega: Caida_Aire (en bucle) mientras baja, Caida_Aterrizaje al tocar el piso y Caida_Levantarse;
    /// después le devuelve el Animator al controlador. Los clips salen de <see cref="ConfigClipsNovaNpc"/>. Se juega con un PlayableGraph encima del Animator
    /// (como ReproductorClipUnico): la IA y el movimiento del soldado no se tocan. Si lo matan a la mitad, se corta y manda la animación de muerte.
    /// </summary>
    public class CaidaNova : MonoBehaviour
    {
        public enum Fase
        {
            Aire,
            Aterrizaje,
            Levantarse,
            Terminada
        }

        /// <summary>Segundos mínimos en el aire antes de aceptar que "tocó el piso" por gravedad (el primer cuadro del agente aún no se ha movido).</summary>
        public const float SegundosMinimosEnAire = 0.2f;

        private Animator animador;
        private VisualPersonaje visual;
        private CharacterController controlador;
        private ConfigClipsNovaNpc config;
        private PlayableGraph grafo;
        private AnimationPlayableOutput salida;
        private AnimationClipPlayable playable;
        private AnimationClip clipActual;
        private bool porGravedad;
        private float segundosEnAire;

        public Fase Actual { get; private set; } = Fase.Aire;

        /// <summary>El clip que se está jugando (null si ya terminó).</summary>
        public AnimationClip ClipActual => clipActual;

        /// <summary>
        /// Pone la caída en <paramref name="objeto"/>. Con <paramref name="porGravedad"/> aterriza sola cuando su CharacterController toca el piso;
        /// si no, hay que llamar a <see cref="Aterrizar"/>. Devuelve null si falta el Animator o la config.
        /// </summary>
        public static CaidaNova Iniciar(GameObject objeto, bool porGravedad)
        {
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (objeto == null || config == null || config.CaidaAire == null || config.CaidaAterrizaje == null || config.CaidaLevantarse == null)
            {
                return null;
            }
            VisualPersonaje visual = objeto.GetComponentInChildren<VisualPersonaje>(true);
            Animator animador = visual != null ? visual.Animador : objeto.GetComponentInChildren<Animator>(true);
            if (animador == null || !animador.isActiveAndEnabled)
            {
                return null;
            }
            CaidaNova caida = objeto.GetComponent<CaidaNova>();
            if (caida != null)
            {
                Destroy(caida);
            }
            caida = objeto.AddComponent<CaidaNova>();
            caida.config = config;
            caida.animador = animador;
            caida.visual = visual;
            caida.controlador = objeto.GetComponent<CharacterController>();
            caida.porGravedad = porGravedad;
            caida.Jugar(Fase.Aire, config.CaidaAire);
            return caida;
        }

        /// <summary>Toca el piso: Aterrizaje y luego Levantarse. Si ya aterrizó, no hace nada.</summary>
        public void Aterrizar()
        {
            if (Actual == Fase.Aire)
            {
                Jugar(Fase.Aterrizaje, config.CaidaAterrizaje);
            }
        }

        private void Jugar(Fase fase, AnimationClip clip)
        {
            if (!grafo.IsValid())
            {
                grafo = PlayableGraph.Create("CaidaNova");
                grafo.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                salida = AnimationPlayableOutput.Create(grafo, "Salida", animador);
                grafo.Play();
            }
            if (playable.IsValid())
            {
                playable.Destroy();
            }
            playable = AnimationClipPlayable.Create(grafo, clip);
            salida.SetSourcePlayable(playable);
            Actual = fase;
            clipActual = clip;
        }

        private void Update()
        {
            if (Actual == Fase.Terminada)
            {
                return;
            }
            if (visual != null && visual.Caido)
            {
                Terminar();
                return;
            }
            if (!playable.IsValid())
            {
                return;
            }
            double tiempo = playable.GetTime();
            switch (Actual)
            {
                case Fase.Aire:
                    segundosEnAire += Time.deltaTime;
                    if (tiempo >= clipActual.length)
                    {
                        playable.SetTime(0d);
                    }
                    if (porGravedad && segundosEnAire >= SegundosMinimosEnAire && controlador != null && controlador.isGrounded)
                    {
                        Aterrizar();
                    }
                    break;
                case Fase.Aterrizaje:
                    if (tiempo >= clipActual.length)
                    {
                        Jugar(Fase.Levantarse, config.CaidaLevantarse);
                    }
                    break;
                case Fase.Levantarse:
                    if (tiempo >= clipActual.length)
                    {
                        Terminar();
                    }
                    break;
            }
        }

        private void Terminar()
        {
            Actual = Fase.Terminada;
            clipActual = null;
            if (grafo.IsValid())
            {
                grafo.Destroy();
            }
            Destroy(this);
        }

        private void OnDestroy()
        {
            if (grafo.IsValid())
            {
                grafo.Destroy();
            }
        }
    }
}

using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// T-1204: mientras el jugador está en una escalera de mano, reproduce el clip de subir o bajar (Humanoid) con un PlayableGraph
    /// y avanza su tiempo según los metros trepados. Sin clips en ConfigAnimacionEscalera no hace nada.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class AnimacionEscalera : MonoBehaviour
    {
        private TreparAPie trepar;
        private Animator animator;
        private ConfigAnimacionEscalera config;
        private PlayableGraph grafo;
        private AnimationClipPlayable playableClip;
        private bool grafoVivo;
        private bool bajandoActual;
        private bool rootMotionPrevio;

        public bool Reproduciendo => grafoVivo;
        public AnimationClip ClipActual { get; private set; }

        /// <summary>Tiempo del clip (s) para los metros trepados: una vuelta del clip por cada metrosPorCiclo.</summary>
        public static float TiempoClip(float progreso, float metrosPorCiclo, float largoClip)
        {
            if (metrosPorCiclo <= 0f)
            {
                return 0f;
            }
            return Mathf.Repeat(progreso / metrosPorCiclo, 1f) * largoClip;
        }

        private void Awake()
        {
            trepar = GetComponent<TreparAPie>();
            VisualPersonaje visual = GetComponentInChildren<VisualPersonaje>(true);
            animator = visual != null ? visual.GetComponentInChildren<Animator>(true) : null;
            config = ConfigAnimacionEscalera.Actual;
        }

        private void LateUpdate()
        {
            if (trepar == null || animator == null || config == null || !config.HayClips)
            {
                return;
            }
            if (!trepar.EnEscaleraDeMano)
            {
                Destruir();
                return;
            }
            bool bajando = trepar.BajandoEscalera;
            AnimationClip clip = bajando && config.Bajar != null ? config.Bajar : config.Subir != null ? config.Subir : config.Bajar;
            if (clip == null)
            {
                return;
            }
            if (!grafoVivo || clip != ClipActual || bajando != bajandoActual)
            {
                Crear(clip);
                bajandoActual = bajando;
            }
            playableClip.SetTime(TiempoClip(trepar.ProgresoEscalera, config.MetrosPorCiclo, clip.length));
        }

        private void Crear(AnimationClip clip)
        {
            bool vivoAntes = grafoVivo;
            Destruir();
            if (!vivoAntes)
            {
                rootMotionPrevio = animator.applyRootMotion;
            }
            animator.applyRootMotion = false;
            playableClip = AnimationPlayableUtilities.PlayClip(animator, clip, out grafo);
            playableClip.SetSpeed(0d);
            ClipActual = clip;
            grafoVivo = true;
        }

        private void Destruir()
        {
            if (!grafoVivo)
            {
                return;
            }
            if (grafo.IsValid())
            {
                grafo.Destroy();
            }
            grafoVivo = false;
            ClipActual = null;
            if (animator != null)
            {
                animator.applyRootMotion = rootMotionPrevio;
            }
        }

        private void OnDisable()
        {
            Destruir();
        }

        private void OnDestroy()
        {
            Destruir();
        }
    }
}

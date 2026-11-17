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

        private float inicioEntrar = -1f;
        private float inicioSalirAbajo = -1f;

        private void Awake()
        {
            trepar = GetComponent<TreparAPie>();
            VisualPersonaje visual = GetComponentInChildren<VisualPersonaje>(true);
            animator = visual != null ? visual.GetComponentInChildren<Animator>(true) : null;
            config = ConfigAnimacionEscalera.Actual;
        }

        private void OnEnable()
        {
            if (trepar != null)
            {
                trepar.EntroDesdeAbajo += AlEntrar;
                trepar.SalioAbajo += AlSalirAbajo;
            }
        }

        private void AlEntrar()
        {
            inicioEntrar = Time.time;
            inicioSalirAbajo = -1f;
        }

        private void AlSalirAbajo()
        {
            inicioSalirAbajo = Time.time;
            inicioEntrar = -1f;
        }

        private void LateUpdate()
        {
            if (trepar == null || animator == null || config == null || !config.HayClips)
            {
                return;
            }
            AnimationClip clip = null;
            double tiempo = 0d;
            if (trepar.SaliendoArriba && config.SalirArriba != null)
            {
                // De la cima al techo: el clip avanza con el paso al techo.
                clip = config.SalirArriba;
                tiempo = trepar.ProgresoSalida * clip.length;
            }
            else if (trepar.EnEscaleraDeMano)
            {
                if (config.Entrar != null && inicioEntrar >= 0f && Time.time - inicioEntrar < config.Entrar.length && !trepar.BajandoEscalera)
                {
                    clip = config.Entrar;
                    tiempo = Time.time - inicioEntrar;
                }
                else
                {
                    bool bajando = trepar.BajandoEscalera;
                    clip = bajando && config.Bajar != null ? config.Bajar : config.Subir != null ? config.Subir : config.Bajar;
                    tiempo = clip != null ? TiempoClip(trepar.ProgresoEscalera, config.MetrosPorCiclo, clip.length) : 0d;
                }
            }
            else if (config.SalirAbajo != null && inicioSalirAbajo >= 0f && Time.time - inicioSalirAbajo < config.SalirAbajo.length && !trepar.Trepando)
            {
                clip = config.SalirAbajo;
                tiempo = Time.time - inicioSalirAbajo;
            }
            if (clip == null)
            {
                Destruir();
                return;
            }
            if (!grafoVivo || clip != ClipActual)
            {
                Crear(clip);
            }
            playableClip.SetTime(tiempo);
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
            if (trepar != null)
            {
                trepar.EntroDesdeAbajo -= AlEntrar;
                trepar.SalioAbajo -= AlSalirAbajo;
            }
            Destruir();
        }

        private void OnDestroy()
        {
            Destruir();
        }
    }
}

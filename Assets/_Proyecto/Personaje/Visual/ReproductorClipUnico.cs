using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1310: juega un clip Humanoid una sola vez sobre el Animator de un personaje con un PlayableGraph (como AnimacionEscalera) y, al terminar, le devuelve el control
    /// al controlador. Lo usan el parkour y la caída del jugador, y el conductor al que le roban el carro. Va en cualquier objeto del personaje.
    /// </summary>
    public class ReproductorClipUnico : MonoBehaviour
    {
        private Animator animador;
        private PlayableGraph grafo;
        private AnimationClipPlayable playable;
        private AnimationClip clip;
        private bool vivo;
        private bool bucle;
        private bool rootMotionPrevio;

        /// <summary>True mientras el clip se está jugando.</summary>
        public bool Reproduciendo => vivo;

        /// <summary>El clip que se juega (null si no hay).</summary>
        public AnimationClip ClipActual => vivo ? clip : null;

        /// <summary>T-1516: true si el clip se repite hasta que alguien lo detenga.</summary>
        public bool Repite => vivo && bucle;

        /// <summary>Segundos que lleva jugando el clip.</summary>
        public float Tiempo => vivo && playable.IsValid() ? (float)playable.GetTime() : 0f;

        /// <summary>Empieza a jugar <paramref name="nuevo"/> con rapidez <paramref name="rapidez"/> (1 = normal). Devuelve false si no hay Animator o clip.</summary>
        public bool Reproducir(Animator sobre, AnimationClip nuevo, float rapidez = 1f, bool repetir = false)
        {
            if (sobre == null || nuevo == null)
            {
                return false;
            }
            Detener();
            animador = sobre;
            rootMotionPrevio = animador.applyRootMotion;
            animador.applyRootMotion = false;
            playable = AnimationPlayableUtilities.PlayClip(animador, nuevo, out grafo);
            playable.SetSpeed(Mathf.Max(0.01f, rapidez));
            clip = nuevo;
            bucle = repetir;
            vivo = true;
            return true;
        }

        /// <summary>Corta el clip y devuelve el control al controlador del Animator.</summary>
        public void Detener()
        {
            if (!vivo)
            {
                return;
            }
            vivo = false;
            if (grafo.IsValid())
            {
                grafo.Destroy();
            }
            if (animador != null)
            {
                animador.applyRootMotion = rootMotionPrevio;
            }
            clip = null;
        }

        private void Update()
        {
            if (!vivo)
            {
                return;
            }
            if (!playable.IsValid())
            {
                Detener();
            }
            else if (playable.GetTime() >= clip.length)
            {
                if (bucle)
                {
                    playable.SetTime(playable.GetTime() % clip.length);
                }
                else
                {
                    Detener();
                }
            }
        }

        private void OnDisable()
        {
            Detener();
        }

        private void OnDestroy()
        {
            Detener();
        }
    }
}

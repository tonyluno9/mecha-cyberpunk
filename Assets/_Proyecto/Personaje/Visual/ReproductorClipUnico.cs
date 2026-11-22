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

        // T-1561: mezcla entre el clip que ya juega y el nuevo (platicar o discutir a asustarse: no de golpe).
        private AnimationMixerPlayable mezclador;
        private AnimationClipPlayable viejo;
        private AnimationClip clipViejo;
        private bool bucleViejo;
        private bool hayViejo;
        private float mezclaTranscurrida;
        private float mezclaTotal;

        /// <summary>
        /// T-1561: igual que <see cref="Reproducir(Animator, AnimationClip, float, bool)"/>, pero si ya jugaba un clip lo desvanece mientras entra el nuevo durante
        /// <paramref name="segundosMezcla"/> (0.2 s es lo normal). Sin clip anterior, o con 0 s, entra de golpe como siempre.
        /// </summary>
        public bool Reproducir(Animator sobre, AnimationClip nuevo, float rapidez, bool repetir, float segundosMezcla)
        {
            bool desdeClip = segundosMezcla > 0f && vivo && grafo.IsValid() && playable.IsValid() && sobre == animador && clip != null && nuevo != null;
            if (!desdeClip)
            {
                return Reproducir(sobre, nuevo, rapidez, repetir);
            }
            AnimationClip clipAnterior = clip;
            double tiempoAnterior = playable.GetTime();
            float rapidezAnterior = (float)playable.GetSpeed();
            bool bucleAnterior = bucle;
            Detener();
            animador = sobre;
            rootMotionPrevio = animador.applyRootMotion;
            animador.applyRootMotion = false;
            grafo = PlayableGraph.Create("ReproductorClipUnico");
            grafo.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationPlayableOutput salida = AnimationPlayableOutput.Create(grafo, "ClipUnico", animador);
            mezclador = AnimationMixerPlayable.Create(grafo, 2);
            viejo = AnimationClipPlayable.Create(grafo, clipAnterior);
            viejo.SetTime(tiempoAnterior);
            viejo.SetSpeed(rapidezAnterior);
            playable = AnimationClipPlayable.Create(grafo, nuevo);
            playable.SetSpeed(Mathf.Max(0.01f, rapidez));
            grafo.Connect(viejo, 0, mezclador, 0);
            grafo.Connect(playable, 0, mezclador, 1);
            mezclador.SetInputWeight(0, 1f);
            mezclador.SetInputWeight(1, 0f);
            salida.SetSourcePlayable(mezclador);
            grafo.Play();
            clip = nuevo;
            bucle = repetir;
            vivo = true;
            clipViejo = clipAnterior;
            bucleViejo = bucleAnterior;
            hayViejo = true;
            mezclaTranscurrida = 0f;
            mezclaTotal = segundosMezcla;
            return true;
        }

        /// <summary>T-1561: True mientras el clip nuevo todavía se está mezclando con el anterior.</summary>
        public bool Mezclando => vivo && hayViejo;

        /// <summary>Corta el clip y devuelve el control al controlador del Animator.</summary>
        public void Detener()
        {
            if (!vivo)
            {
                return;
            }
            hayViejo = false;
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
            if (hayViejo && mezclador.IsValid())
            {
                mezclaTranscurrida += Time.deltaTime;
                float peso = Mathf.Clamp01(mezclaTranscurrida / Mathf.Max(0.01f, mezclaTotal));
                mezclador.SetInputWeight(0, 1f - peso);
                mezclador.SetInputWeight(1, peso);
                if (bucleViejo && viejo.IsValid() && clipViejo != null && viejo.GetTime() >= clipViejo.length)
                {
                    viejo.SetTime(viejo.GetTime() % clipViejo.length);
                }
                if (peso >= 1f)
                {
                    hayViejo = false;
                }
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

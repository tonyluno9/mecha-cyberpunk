using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1495: puente entre el peatón de la ciudad (Trafico, que no referencia a Personaje) y su <see cref="VisualPersonaje"/>. El peatón avisa con
    /// SendMessage (AlHerirCivil, AlMorirCivil, AlRevivirCivil) y aquí se pone la reacción de daño y la animación de muerte (MuerteAdelante, MuerteAtras,
    /// MuerteDesmayo del controlador base, o el estado Caer de los controladores propios). Va en la raíz del prefab Peaton.
    /// </summary>
    public class AnimacionCivilMundo : MonoBehaviour
    {
        private VisualPersonaje visual;

        public VisualPersonaje Visual => visual != null ? visual : (visual = GetComponentInChildren<VisualPersonaje>(false));

        /// <summary>Recibe un golpe sin morir: reacción de daño según la dirección (del atacante al personaje).</summary>
        public void AlHerirCivil(Vector3 direccion)
        {
            if (Visual != null)
            {
                Visual.Herir(Plano(direccion));
            }
        }

        /// <summary>Muere: elige la caída según la dirección del golpe y la reproduce.</summary>
        public void AlMorirCivil(Vector3 direccion)
        {
            if (Visual != null)
            {
                ReproductorClipUnico bucle = Visual.GetComponent<ReproductorClipUnico>();
                if (bucle != null && bucle.Repite)
                {
                    bucle.Detener(); // T-1516: la muerte animada vuelve a mandar
                }
                Visual.RegistrarGolpe(Plano(direccion));
                Visual.Caido = true;
            }
        }

        /// <summary>T-1506: empieza a huir: juega NPC_Asustarse una vez (brinco atrás con las manos a la cara) y sigue corriendo. Sin clip configurado, no hace nada.</summary>
        public void AlAsustarCivil()
        {
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (Visual == null || Visual.Animador == null || config == null || config.Asustarse == null || Visual.Caido)
            {
                return;
            }
            ReproductorClipUnico reproductor = Reproductor();
            if (reproductor.Repite)
            {
                reproductor.Detener(); // T-1516: deja de platicar o de estar agachado para asustarse
            }
            if (!reproductor.Reproduciendo)
            {
                reproductor.Reproducir(Visual.Animador, config.Asustarse);
            }
        }

        private ReproductorClipUnico Reproductor()
        {
            ReproductorClipUnico reproductor = Visual.GetComponent<ReproductorClipUnico>();
            return reproductor != null ? reproductor : Visual.gameObject.AddComponent<ReproductorClipUnico>();
        }

        /// <summary>T-1516: juega <paramref name="clip"/> en bucle (si ya lo juega, no lo reinicia).</summary>
        private bool JugarEnBucle(AnimationClip clip)
        {
            if (Visual == null || Visual.Animador == null || clip == null || Visual.Caido)
            {
                return false;
            }
            ReproductorClipUnico reproductor = Reproductor();
            if (reproductor.ClipActual == clip)
            {
                return true;
            }
            return reproductor.Reproducir(Visual.Animador, clip, 1f, true);
        }

        private void DetenerSiJuega(params AnimationClip[] clips)
        {
            ReproductorClipUnico reproductor = Visual != null ? Visual.GetComponent<ReproductorClipUnico>() : null;
            if (reproductor == null || !reproductor.Repite)
            {
                return;
            }
            foreach (AnimationClip clip in clips)
            {
                if (clip != null && reproductor.ClipActual == clip)
                {
                    reproductor.Detener();
                    return;
                }
            }
        }

        /// <summary>T-1516: el civil se queda platicando (variante 0 o 1): clip de plática en bucle.</summary>
        public void AlPlaticarCivil(int variante)
        {
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (config != null)
            {
                JugarEnBucle(variante % 2 == 0 ? config.Platicar1 : config.Platicar2);
            }
        }

        public void AlDejarPlaticarCivil()
        {
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (config != null)
            {
                DetenerSiJuega(config.Platicar1, config.Platicar2);
            }
        }

        /// <summary>T-1516: se agacha cubriéndose la cabeza (bucle) hasta que se levanta.</summary>
        public void AlAgacharCivil()
        {
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (config != null)
            {
                JugarEnBucle(config.Agachado);
            }
        }

        public void AlLevantarseCivil()
        {
            ConfigClipsNovaNpc config = ConfigClipsNovaNpc.Actual;
            if (config != null)
            {
                DetenerSiJuega(config.Agachado);
            }
        }

        /// <summary>El peatón vuelve a la vida (el pool lo reutiliza).</summary>
        public void AlRevivirCivil()
        {
            if (Visual != null)
            {
                Reproductor().Detener();
                Visual.Caido = false;
            }
        }

        private Vector3 Plano(Vector3 direccion)
        {
            direccion.y = 0f;
            return direccion.sqrMagnitude < 1e-6f ? transform.forward : direccion.normalized;
        }
    }
}

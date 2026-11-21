using MechaCyberpunk.Core;
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
        [Header("T-1521: guarda contra el civil vivo acostado")]
        [Tooltip("Cabeza sobre la planta (m) por debajo de la cual un civil vivo se considera acostado.")]
        [Min(0.1f)] [SerializeField] private float alturaMinimaCabeza = 0.5f;
        [Tooltip("Segundos seguidos con la cabeza baja, sin estar tumbado a propósito, antes de levantarlo.")]
        [Min(0.5f)] [SerializeField] private float segundosBajo = 2.5f;
        [Tooltip("Segundos seguidos tumbado (KO) sin que se haya avisado su muerte antes de levantarlo. Sacar a un conductor del carro tarda menos de 2 s.")]
        [Min(1f)] [SerializeField] private float segundosTumbado = 8f;
        [Tooltip("Cada cuántos segundos revisa la postura (s).")]
        [Min(0.1f)] [SerializeField] private float segundosEntreRevisiones = 0.5f;
        [Tooltip("Cuánto debe verse la raíz derecha para revisarlo (1 = vertical, valor mínimo de transform.up.y).")]
        [Range(0.5f, 1f)] [SerializeField] private float minimoRaizDerecha = 0.9f;

        private VisualPersonaje visual;
        private bool muerto;
        private ReglaPosturaCivil postura;
        private Transform cabeza;
        private float proximaRevision;

        /// <summary>True desde que le avisaron su muerte hasta que vuelve a la vida.</summary>
        public bool MuerteAvisada => muerto;

        public VisualPersonaje Visual => visual != null ? visual : (visual = GetComponentInChildren<VisualPersonaje>(false));

        /// <summary>
        /// T-1533: recibe daño (de bala, golpe o atropello, herido o muerto): suena su voz de dolor, de hombre o de mujer según el visual que trae el peatón
        /// (<see cref="ReglaSonidoNpc"/>); los robots sueltan chispas.
        /// </summary>
        public void AlDolerCivil(Vector3 punto)
        {
            PeatonVariado peaton = GetComponent<PeatonVariado>();
            string nombre = peaton != null ? peaton.NombreVisual : name;
            Vector3 donde = punto != Vector3.zero ? punto : transform.position + Vector3.up;
            Sonidos.ReproducirEn(ReglaSonidoNpc.IdDolor(nombre, GetInstanceID()), donde);
        }

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
            muerto = true;
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
            muerto = false;
            postura?.Reiniciar();
            if (Visual != null)
            {
                Reproductor().Detener();
                Visual.Caido = false;
            }
        }

        private void Update()
        {
            if (Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + segundosEntreRevisiones;
            RevisarPostura(segundosEntreRevisiones);
        }

        /// <summary>
        /// T-1521: si un civil vivo lleva un rato con la cabeza a ras de piso o tumbado sin que nadie lo haya matado, suelta el clip, quita el KO y reinicia el Animator
        /// (queda de pie en Idle). Cubre un clip de caída o de agachado que no se soltó, un KO que nadie quitó y un Animator trabado.
        /// </summary>
        public void RevisarPostura(float dt)
        {
            VisualPersonaje v = Visual;
            if (v == null || v.Animador == null)
            {
                return;
            }
            if (cabeza == null || !cabeza.IsChildOf(v.Animador.transform))
            {
                cabeza = v.Animador.avatar != null && v.Animador.isHuman ? v.Animador.GetBoneTransform(HumanBodyBones.Head) : null;
            }
            if (cabeza == null)
            {
                return;
            }
            if (postura == null)
            {
                postura = new ReglaPosturaCivil(alturaMinimaCabeza, segundosBajo, segundosTumbado);
            }
            ReproductorClipUnico reproductor = v.GetComponent<ReproductorClipUnico>();
            bool enBucle = reproductor != null && reproductor.Repite;
            float alturaCabeza = cabeza.position.y - transform.position.y;
            bool derecha = transform.up.y >= minimoRaizDerecha;
            if (postura.Avanzar(dt, alturaCabeza, muerto, v.Caido, enBucle, derecha) != ReglaPosturaCivil.Accion.Levantar)
            {
                return;
            }
            Debug.LogWarning("[IA] T-1521: " + name + " estaba vivo y acostado (cabeza a " + alturaCabeza.ToString("F2") + " m, KO " + v.Caido + "): se levanta.", this);
            Reproductor().Detener();
            v.Caido = false;
            v.Animador.Rebind();
        }

        private Vector3 Plano(Vector3 direccion)
        {
            direccion.y = 0f;
            return direccion.sqrMagnitude < 1e-6f ? transform.forward : direccion.normalized;
        }
    }
}

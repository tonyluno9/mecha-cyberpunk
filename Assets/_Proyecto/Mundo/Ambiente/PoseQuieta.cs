using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T-1463: gente de decorado (el paseo del Malecón, la banqueta). Al arrancar pone al personaje en un estado de su Animator (por defecto "Idle")
    /// con una fase distinta para que no respiren todos a la vez, y deja el Animator animando aunque no se vea (AlwaysAnimate).
    /// </summary>
    public class PoseQuieta : MonoBehaviour
    {
        [Tooltip("Nombre del estado del Animator (capa 0) que se reproduce.")]
        [SerializeField] private string estado = "Idle";

        [Tooltip("Punto del ciclo donde arranca el estado (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float fase;

        [Tooltip("Si es true el Animator se detiene en esa fase (pose fija).")]
        [SerializeField] private bool congelar;

        public string Estado => estado;
        public float Fase => fase;
        public bool Congelar => congelar;

        /// <summary>Lo usan los scripts de colocación (editor).</summary>
        public void Definir(string nuevoEstado, float nuevaFase, bool fija)
        {
            estado = nuevoEstado;
            fase = Mathf.Clamp01(nuevaFase);
            congelar = fija;
        }

        private void Awake()
        {
            Aplicar();
        }

        /// <summary>Pone el estado y la fase en el Animator del personaje; no hace nada si no hay Animator con controlador. Devuelve si encontró el estado.</summary>
        public bool Aplicar()
        {
            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                return false;
            }
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (animator.runtimeAnimatorController == null)
            {
                return false;
            }
            int hash = Animator.StringToHash(estado);
            if (!animator.HasState(0, hash))
            {
                return false;
            }
            animator.Play(hash, 0, Mathf.Repeat(fase, 1f));
            if (congelar)
            {
                animator.Update(0f);
                animator.speed = 0f;
            }
            return true;
        }
    }
}

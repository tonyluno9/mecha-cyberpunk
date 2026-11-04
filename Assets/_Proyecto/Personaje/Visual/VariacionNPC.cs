using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Variación de un NPC con el clip de idle que hay (T-562): al aparecer salta el idle a un punto distinto del ciclo y lo juega a una rapidez
    /// un poco distinta, para que dos NPC juntos no respiren al unísono. Determinista: la misma semilla da la misma variación.
    /// La mirada al jugador por IK ya la hace <see cref="IKPiesYMirada"/> tras un segundo cerca.
    /// </summary>
    [RequireComponent(typeof(VisualPersonaje))]
    public class VariacionNPC : MonoBehaviour
    {
        [Tooltip("Semilla de la variación. 0 = se saca del nombre y la posición de este NPC.")]
        [SerializeField] private int semilla;

        private VisualPersonaje visual;
        private bool aplicada;

        /// <summary>Fracción del ciclo del idle con la que empezó (0 a menos de 1).</summary>
        public float Desfase { get; private set; }

        /// <summary>Rapidez del idle de este NPC.</summary>
        public float Rapidez { get; private set; } = 1f;

        public bool Aplicada => aplicada;

        private void Awake() => visual = GetComponent<VisualPersonaje>();

        private void Update()
        {
            if (aplicada || visual == null || visual.Animador == null || visual.Animador.runtimeAnimatorController == null)
            {
                return;
            }
            // Se espera a que el Animator esté en Idle; un NPC que arranca caminando no se toca.
            if (!visual.Animador.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            {
                return;
            }
            int s = semilla != 0 ? semilla : SemillaAutomatica();
            Desfase = ReglasAnimacion.DesfaseIdle(s);
            Rapidez = ReglasAnimacion.RapidezIdle(s);
            visual.Variar(Desfase, Rapidez);
            aplicada = true;
        }

        private int SemillaAutomatica()
        {
            Vector3 p = transform.position;
            unchecked
            {
                return name.Length * 7919 + Mathf.RoundToInt(p.x * 100f) * 31 + Mathf.RoundToInt(p.z * 100f) * 17 + 1;
            }
        }
    }
}

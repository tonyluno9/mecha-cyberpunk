using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Apunta con las manos cuando el modelo no tiene animación de apuntar (T-520): con <see cref="VisualPersonaje.Apuntando"/> y un arma
    /// en la mano lleva las manos al frente del pecho por IK de Animator (necesita la capa base con IK Pass). Va en el mismo objeto que el Animator.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class ApuntadoIK : MonoBehaviour
    {
        [Tooltip("Visual del que se lee Apuntando y TipoArma. Si está vacío se busca en los padres.")]
        [SerializeField] private VisualPersonaje visual;
        [Tooltip("Qué tan al frente del pecho queda la mano de la pistola (m).")]
        [Min(0f)] [SerializeField] private float alcancePistola = 0.5f;
        [Tooltip("Qué tan al frente del pecho queda la mano que sostiene el arma larga (m).")]
        [Min(0f)] [SerializeField] private float alcanceArmaLarga = 0.62f;
        [Tooltip("Separación lateral de las manos respecto al pecho (m).")]
        [Min(0f)] [SerializeField] private float separacionManos = 0.1f;
        [Tooltip("Altura de las manos sobre el pecho (m); negativa = más abajo.")]
        [SerializeField] private float alturaManos = 0.06f;
        [Tooltip("Rapidez con la que sube o baja el arma (1/s).")]
        [Min(0.1f)] [SerializeField] private float rapidez = 10f;

        [Tooltip("Punto de la mano derecha donde van las armas (hijo del hueso de la mano). Su -Y apunta por el cañón: el arma lo gira al frente al apuntar. Vacío = no se mueve.")]
        [SerializeField] private Transform socketArma;
        [Tooltip("Inclinación del arma hacia abajo cuando no se apunta (°); 0 = horizontal, 90 = colgando.")]
        [Range(0f, 90f)] [SerializeField] private float inclinacionBajada = 60f;

        [Tooltip("Llevar las manos al frente por IK. Se apaga cuando el controlador ya trae las animaciones ApuntarPistola y ApuntarLarga de verdad.")]
        [SerializeField] private bool manosPorIK = true;

        private Animator animador;
        private float peso;

        /// <summary>Qué tanto está levantada el arma, de 0 (bajada) a 1.</summary>
        public float Peso => peso;

        private void Awake()
        {
            animador = GetComponent<Animator>();
            if (visual == null)
            {
                visual = GetComponentInParent<VisualPersonaje>();
            }
        }

        /// <summary>Para los creadores: el socket del arma.</summary>
        public void ConfigurarSocket(Transform socket) => socketArma = socket;

        public Transform SocketArma => socketArma;

        public bool ManosPorIK { get => manosPorIK; set => manosPorIK = value; }

        private void Update()
        {
            if (visual == null)
            {
                return;
            }
            bool arriba = visual.Apuntando && visual.TipoArma != TipoArmaVisual.Ninguna && !visual.Caido;
            peso = Mathf.MoveTowards(peso, arriba ? 1f : 0f, rapidez * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (socketArma == null || visual == null)
            {
                return;
            }
            // Con el arma arriba el cañón apunta al frente; abajo, inclinado hacia el piso.
            Vector3 frente = visual.transform.forward;
            Vector3 bajada = Quaternion.AngleAxis(inclinacionBajada, visual.transform.right) * frente;
            Vector3 canon = Vector3.Slerp(bajada, frente, peso).normalized;
            // La -Y local del socket es la dirección del cañón (las armas del jugador cuelgan de la mano con -Y).
            socketArma.rotation = Quaternion.LookRotation(canon, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
        }

        private void OnAnimatorIK(int capa)
        {
            if (animador == null || visual == null)
            {
                return;
            }
            if (!manosPorIK)
            {
                return;
            }
            Transform pecho = animador.GetBoneTransform(HumanBodyBones.UpperChest);
            if (pecho == null)
            {
                pecho = animador.GetBoneTransform(HumanBodyBones.Chest);
            }
            if (pecho == null)
            {
                return;
            }
            Transform referencia = visual.transform;
            Vector3 frente = referencia.forward;
            Vector3 derecha = referencia.right;
            bool larga = visual.TipoArma == TipoArmaVisual.ArmaLarga;
            Vector3 centro = pecho.position + Vector3.up * alturaManos;
            Vector3 manoDer = centro + frente * alcancePistola + derecha * separacionManos;
            Vector3 manoIzq = larga
                ? centro + frente * alcanceArmaLarga - derecha * separacionManos * 0.5f
                : centro + frente * alcancePistola - derecha * (separacionManos * 0.4f);
            Quaternion giro = Quaternion.LookRotation(frente, Vector3.up);
            Poner(AvatarIKGoal.RightHand, manoDer, giro);
            Poner(AvatarIKGoal.LeftHand, manoIzq, giro);
        }

        private void Poner(AvatarIKGoal mano, Vector3 posicion, Quaternion giro)
        {
            animador.SetIKPositionWeight(mano, peso);
            animador.SetIKRotationWeight(mano, peso * 0.6f);
            animador.SetIKPosition(mano, posicion);
            animador.SetIKRotation(mano, giro);
            AvatarIKHint codo = mano == AvatarIKGoal.RightHand ? AvatarIKHint.RightElbow : AvatarIKHint.LeftElbow;
            float lado = mano == AvatarIKGoal.RightHand ? 1f : -1f;
            animador.SetIKHintPositionWeight(codo, peso);
            animador.SetIKHintPosition(codo, posicion - visual.transform.forward * 0.25f + visual.transform.right * lado * 0.35f - Vector3.up * 0.15f);
        }
    }
}

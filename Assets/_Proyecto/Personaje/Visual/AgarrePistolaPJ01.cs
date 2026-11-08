using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-738 (A6 final): compensa en el agarre de la pistola de PJ01 lo que el retargeting Humanoid mueve la mano respecto a Blender. Con el clip puro la mano llega con la pistola
    /// unos 22 grados a un lado, 19 arriba y con la corredera ladeada (medido: 17-29 grados de la mira y 65-80 de ladeo, y la mano oscila hasta 24 grados en el ciclo de caminar de lado,
    /// así que un giro fijo por clip no alcanza). NO toca huesos: solo gira la pistola, en el pivote de la empuñadura (sigue en la palma, R_Hand no se mueve), para que el cañón mire
    /// de la boca al punto de mira con la corredera arriba. El agarre de Tony (agarre.txt, AgarresArmas.CuaternionTony y PosicionPJ01) queda tal cual en el nodo de la pistola como
    /// referencia: con <see cref="Usar"/> apagado la pistola vuelve a él, para cuando llegue el export de Blender (docs/pedido-blender-09nov.md).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(30)]
    public class AgarrePistolaPJ01 : MonoBehaviour
    {
        [Tooltip("Aplicar la compensación. Apagado, la pistola queda con el agarre de agarre.txt tal cual.")]
        [SerializeField] private bool usar = true;
        [Tooltip("Distancia (m) a la que mira la pistola cuando no hay punto de mira.")]
        [Min(1f)] [SerializeField] private float distanciaSinMira = 30f;
        [Tooltip("Cuánto sube el cañón (°) en el pico del retroceso al disparar; dura unos 0.1 s. 0 = sin retroceso.")]
        [Range(0f, 15f)] [SerializeField] private float retrocesoGrados = 4f;
        [Tooltip("Duración del retroceso (s).")]
        [Min(0.02f)] [SerializeField] private float retrocesoSegundos = 0.1f;

        private Animator animador;
        private ApuntadoIK ik;
        private Transform cuerpo;
        private Transform pistola;
        private Transform boca;
        private Quaternion agarreOriginal;

        public bool Usar { get => usar; set => usar = value; }

        /// <summary>T-738: cuánto sube el cañón (°) en el pico del retroceso; 0 lo apaga (las pruebas lo apagan para medir la mira).</summary>
        public float RetrocesoGrados { get => retrocesoGrados; set => retrocesoGrados = value; }

        /// <summary>Cuánto está subido el cañón por el retroceso en este cuadro (°).</summary>
        public float RetrocesoActual { get; private set; }

        private const string ClipDisparo = "Pistola_Disparo";
        private int capaArriba = -2;

        private void Awake()
        {
            animador = GetComponent<Animator>();
            ik = GetComponent<ApuntadoIK>();
            VisualPersonaje v = GetComponentInParent<VisualPersonaje>();
            cuerpo = v != null ? v.transform : transform;
        }

        // T-738: el clip de disparo no levanta el cañón (la pistola sigue la mira): se le suma un salto corto que sube y baja en los primeros ~0.1 s del clip.
        private float Retroceso()
        {
            if (retrocesoGrados <= 0f) { return 0f; }
            Animator a = animador;
            if (capaArriba == -2) { capaArriba = a.GetLayerIndex(ReglasAnimacion.CapaArriba); }
            if (capaArriba < 0) { return 0f; }
            AnimatorClipInfo[] clips = a.GetCurrentAnimatorClipInfo(capaArriba);
            if (clips.Length == 0 || clips[0].clip.name != ClipDisparo) { return 0f; }
            float t = a.GetCurrentAnimatorStateInfo(capaArriba).normalizedTime * clips[0].clip.length;
            if (t < 0f || t >= retrocesoSegundos) { return 0f; }
            return retrocesoGrados * Mathf.Sin(Mathf.PI * t / retrocesoSegundos);
        }

        private void LateUpdate()
        {
            if (ik == null || ik.SocketArma == null)
            {
                return;
            }
            if (pistola == null)
            {
                pistola = ik.SocketArma.Find("Pistola");
                if (pistola == null) { return; }
                boca = pistola.Find("Boca");
                agarreOriginal = pistola.localRotation;
            }
            if (!usar)
            {
                pistola.localRotation = agarreOriginal;
                return;
            }
            if (!pistola.gameObject.activeInHierarchy)
            {
                RetrocesoActual = 0f;
                return;
            }
            Vector3 destino = ik.HayMira ? ik.PuntoMira : pistola.position + cuerpo.forward * distanciaSinMira;
            // La boca se mueve al girar la pistola: dos pasadas bastan (la boca está sobre el eje del cañón).
            for (int i = 0; i < 2; i++)
            {
                Vector3 origen = i == 0 || boca == null ? pistola.position : boca.position;
                Vector3 direccion = destino - origen;
                if (direccion.sqrMagnitude < 1e-4f) { return; }
                pistola.rotation = Quaternion.LookRotation(direccion, Vector3.up);
            }
            RetrocesoActual = Retroceso();
            if (RetrocesoActual > 0f)
            {
                pistola.rotation = pistola.rotation * Quaternion.Euler(-RetrocesoActual, 0f, 0f);
            }
        }
    }
}

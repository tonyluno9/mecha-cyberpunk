using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// IK de pies en el suelo y mirada de la cabeza (T-561), en OnAnimatorIK. Va en el mismo objeto que el Animator, con IK Pass en la capa base.
    /// Los pies se apoyan en el piso que hay debajo; la cabeza voltea al objetivo (o al jugador si está cerca y delante, tras un tiempo).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class IKPiesYMirada : MonoBehaviour
    {
        [Header("Pies")]
        [Tooltip("Apoyar los pies en el piso por IK.")]
        [SerializeField] private bool pies = true;
        [Tooltip("Altura desde la que se lanza el rayo hacia abajo sobre cada pie (m).")]
        [Min(0.1f)] [SerializeField] private float alturaRayo = 0.6f;
        [Tooltip("Qué tan lejos debajo del pie se busca el piso (m).")]
        [Min(0.1f)] [SerializeField] private float alcanceRayo = 1.1f;
        [Tooltip("Rapidez con la que el peso del IK de pies sube o baja (1/s).")]
        [Min(0.1f)] [SerializeField] private float rapidezPies = 10f;

        [Header("Mirada")]
        [Tooltip("Objetivo al que voltea la cabeza. Vacío = el jugador, si mirarAlJugador está activo.")]
        [SerializeField] private Transform objetivo;
        [Tooltip("Voltear la cabeza hacia el jugador cuando está cerca y delante.")]
        [SerializeField] private bool mirarAlJugador;
        [Tooltip("Distancia máxima a la que mira al jugador (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaMirada = 7f;
        [Tooltip("Ángulo máximo respecto al frente en el que mira al jugador (°).")]
        [Range(10f, 180f)] [SerializeField] private float anguloMirada = 100f;
        [Tooltip("Tiempo que el jugador debe estar en rango antes de que voltee (s).")]
        [Min(0f)] [SerializeField] private float retardoMirada = ReglasAnimacion.RetardoMirada;
        [Tooltip("Rapidez con la que el peso de la mirada sube o baja (1/s).")]
        [Min(0.1f)] [SerializeField] private float rapidezMirada = 4f;
        [Tooltip("Cuánto del cuerpo acompaña a la cabeza (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float pesoCuerpo = 0.2f;
        [Tooltip("Altura del punto al que mira sobre la raíz del jugador (m).")]
        [Min(0f)] [SerializeField] private float alturaObjetivo = 1.5f;

        private static readonly RaycastHit[] Golpes = new RaycastHit[8];

        private Animator animador;
        private CharacterController cuerpo;
        private Transform referencia;
        private float pesoPies;
        private float pesoMirada;
        private float tiempoEnRango;
        private Vector3 puntoMirada;
        private bool hayMirada;

        public bool Pies { get => pies; set => pies = value; }
        public bool MirarAlJugador { get => mirarAlJugador; set => mirarAlJugador = value; }
        public Transform Objetivo { get => objetivo; set => objetivo = value; }
        public float PesoPies => pesoPies;
        public float PesoMirada => pesoMirada;

        private void Awake()
        {
            animador = GetComponent<Animator>();
            cuerpo = GetComponentInParent<CharacterController>();
            // El Animator cuelga de un nodo girado 180°: el frente del personaje es el del VisualPersonaje.
            VisualPersonaje visual = GetComponentInParent<VisualPersonaje>();
            referencia = visual != null ? visual.transform : transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            hayMirada = BuscarPuntoDeMirada(dt);
            pesoMirada = Mathf.MoveTowards(pesoMirada, hayMirada ? 1f : 0f, rapidezMirada * dt);
            pesoPies = Mathf.MoveTowards(pesoPies, pies && EnSuelo() ? 1f : 0f, rapidezPies * dt);
        }

        private bool EnSuelo()
        {
            return cuerpo == null || !cuerpo.enabled || cuerpo.isGrounded || Mathf.Abs(cuerpo.velocity.y) < 1.5f;
        }

        private bool BuscarPuntoDeMirada(float dt)
        {
            Transform meta = objetivo;
            if (meta == null && mirarAlJugador)
            {
                IJugador jugador = Registro<IJugador>.Actual;
                meta = jugador != null ? jugador.Raiz : null;
            }
            if (meta == null || meta.root == transform.root)
            {
                tiempoEnRango = 0f;
                return false;
            }
            Vector3 destino = meta.position + Vector3.up * alturaObjetivo;
            Vector3 hacia = destino - referencia.position;
            Vector3 plano = new Vector3(hacia.x, 0f, hacia.z);
            bool enRango = plano.magnitude <= distanciaMirada
                && Vector3.Angle(referencia.forward, plano) <= anguloMirada;
            tiempoEnRango = ReglasAnimacion.AvanzarMirada(tiempoEnRango, enRango, dt);
            if (!enRango || (objetivo == null && !ReglasAnimacion.MiradaLista(tiempoEnRango, retardoMirada)))
            {
                return false;
            }
            puntoMirada = destino;
            return true;
        }

        private void OnAnimatorIK(int capa)
        {
            if (animador == null || capa != 0)
            {
                return;
            }
            PonerPie(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot);
            PonerPie(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot);
            if (pesoMirada > 0.001f)
            {
                animador.SetLookAtWeight(pesoMirada, pesoCuerpo, 1f, 0.5f, 0.6f);
                animador.SetLookAtPosition(puntoMirada);
            }
            else
            {
                animador.SetLookAtWeight(0f);
            }
        }

        private void PonerPie(AvatarIKGoal meta, HumanBodyBones hueso)
        {
            Transform pie = animador.GetBoneTransform(hueso);
            if (pie == null || pesoPies <= 0.001f)
            {
                animador.SetIKPositionWeight(meta, 0f);
                animador.SetIKRotationWeight(meta, 0f);
                return;
            }
            Vector3 origen = pie.position + Vector3.up * alturaRayo;
            if (!Piso(origen, out RaycastHit golpe))
            {
                animador.SetIKPositionWeight(meta, 0f);
                animador.SetIKRotationWeight(meta, 0f);
                return;
            }
            float altoPie = meta == AvatarIKGoal.LeftFoot ? animador.leftFeetBottomHeight : animador.rightFeetBottomHeight;
            Vector3 posicion = golpe.point + Vector3.up * Mathf.Max(altoPie, 0.05f);
            // Solo sube o baja el pie un poco: nunca lo lleva más allá del alcance del rayo.
            posicion.y = Mathf.Clamp(posicion.y, pie.position.y - alcanceRayo * 0.4f, pie.position.y + alturaRayo * 0.5f);
            Quaternion giro = Quaternion.FromToRotation(Vector3.up, golpe.normal) * animador.GetIKRotation(meta);
            animador.SetIKPositionWeight(meta, pesoPies);
            animador.SetIKRotationWeight(meta, pesoPies * 0.7f);
            animador.SetIKPosition(meta, posicion);
            animador.SetIKRotation(meta, giro);
        }

        private bool Piso(Vector3 origen, out RaycastHit mejor)
        {
            mejor = default;
            int n = Physics.RaycastNonAlloc(origen, Vector3.down, Golpes, alturaRayo + alcanceRayo, ~0, QueryTriggerInteraction.Ignore);
            float cerca = float.MaxValue;
            bool hay = false;
            for (int i = 0; i < n; i++)
            {
                if (Golpes[i].transform.root == transform.root || Golpes[i].distance >= cerca)
                {
                    continue;
                }
                cerca = Golpes[i].distance;
                mejor = Golpes[i];
                hay = true;
            }
            return hay;
        }
    }
}

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
        [Tooltip("T-1484: solo sube o baja el pie lo que el piso bajo él está más alto o más bajo que bajo el personaje; el resto lo hace el clip (no aplana la zancada).")]
        [SerializeField] private bool soloDesnivel;
        [Tooltip("T-1484: desnivel máximo que corrige (m).")]
        [Min(0.05f)] [SerializeField] private float desnivelMaximo = 0.35f;
        [Tooltip("T-1484: inclina el cuerpo hacia dentro de la curva al girar en movimiento (los pies se quedan en su lugar por IK).")]
        [SerializeField] private bool inclinarEnGiros;

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
        private float bajadaCadera;
        private float bajadaCaderaSuave;
        private float alturaBase;
        private float inclinacion;
        private VisualPersonaje visual;

        public bool Pies { get => pies; set => pies = value; }
        public bool SoloDesnivel { get => soloDesnivel; set => soloDesnivel = value; }
        public bool InclinarEnGiros { get => inclinarEnGiros; set => inclinarEnGiros = value; }
        /// <summary>T-1484: inclinación aplicada este cuadro (°, positiva a la derecha).</summary>
        public float Inclinacion => inclinacion;
        /// <summary>T-1484: cuánto bajó la cadera este cuadro para que el pie más bajo llegue al piso (m, cero o negativo).</summary>
        public float BajadaCadera => bajadaCadera;
        public bool MirarAlJugador { get => mirarAlJugador; set => mirarAlJugador = value; }
        public Transform Objetivo { get => objetivo; set => objetivo = value; }
        public float PesoPies => pesoPies;
        public float PesoMirada => pesoMirada;

        private void Awake()
        {
            animador = GetComponent<Animator>();
            cuerpo = GetComponentInParent<CharacterController>();
            // El Animator cuelga de un nodo girado 180°: el frente del personaje es el del VisualPersonaje.
            visual = GetComponentInParent<VisualPersonaje>();
            referencia = visual != null ? visual.transform : transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            hayMirada = BuscarPuntoDeMirada(dt);
            pesoMirada = Mathf.MoveTowards(pesoMirada, hayMirada ? 1f : 0f, rapidezMirada * dt);
            pesoPies = Mathf.MoveTowards(pesoPies, pies && EnSuelo() ? 1f : 0f, rapidezPies * dt);
            float meta = inclinarEnGiros && visual != null && EnSuelo() ? ReglasAnimacion.InclinacionEnGiro(visual.Velocidad, visual.GiroSuavizado) : 0f;
            inclinacion = Mathf.Lerp(inclinacion, meta, dt > 0f ? 1f - Mathf.Exp(-8f * dt) : 1f);
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
            if (Mathf.Abs(inclinacion) > 0.05f)
            {
                // Hacia dentro de la curva: girando a la derecha el torso se va a la derecha (ángulo negativo sobre el frente).
                animador.bodyRotation = Quaternion.AngleAxis(-inclinacion, referencia.forward) * animador.bodyRotation;
            }
            if (soloDesnivel)
            {
                // El piso bajo el centro del personaje es el nivel de referencia: en plano el desnivel es cero aunque la cápsula flote por su piel.
                Vector3 centro = referencia.position + Vector3.up * alturaRayo;
                alturaBase = Piso(centro, out RaycastHit bajoCentro) ? bajoCentro.point.y : referencia.position.y;
                float izq = PonerPieDesnivel(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot);
                float der = PonerPieDesnivel(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot);
                // La cadera baja con el pie que queda más abajo (escalón hacia abajo); nunca sube.
                bajadaCadera = ReglasAnimacion.BajadaCadera(izq, der) * pesoPies;
                bajadaCaderaSuave = Mathf.Lerp(bajadaCaderaSuave, bajadaCadera, 1f - Mathf.Exp(-rapidezPies * Time.deltaTime));
                if (bajadaCaderaSuave < -0.001f)
                {
                    animador.bodyPosition += Vector3.up * bajadaCaderaSuave;
                }
            }
            else
            {
                PonerPie(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot);
                PonerPie(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot);
            }
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

        // T-1484: corrige solo el desnivel: el piso bajo el pie contra el plano de los pies del personaje (la raíz del CharacterController).
        private float PonerPieDesnivel(AvatarIKGoal meta, HumanBodyBones hueso)
        {
            Transform pie = animador.GetBoneTransform(hueso);
            if (pie == null || pesoPies <= 0.001f)
            {
                animador.SetIKPositionWeight(meta, 0f);
                animador.SetIKRotationWeight(meta, 0f);
                return 0f;
            }
            Vector3 meta0 = animador.GetIKPosition(meta);
            Vector3 origen = new Vector3(meta0.x, alturaBase + alturaRayo, meta0.z);
            if (!Piso(origen, out RaycastHit golpe))
            {
                animador.SetIKPositionWeight(meta, 0f);
                animador.SetIKRotationWeight(meta, 0f);
                return 0f;
            }
            float desnivel = ReglasAnimacion.Desnivel(golpe.point.y, alturaBase, desnivelMaximo);
            Quaternion giro = Quaternion.FromToRotation(Vector3.up, golpe.normal) * animador.GetIKRotation(meta);
            animador.SetIKPositionWeight(meta, pesoPies);
            animador.SetIKRotationWeight(meta, pesoPies * 0.5f);
            animador.SetIKPosition(meta, meta0 + Vector3.up * desnivel);
            animador.SetIKRotation(meta, giro);
            return desnivel;
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

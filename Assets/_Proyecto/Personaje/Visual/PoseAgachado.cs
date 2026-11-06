using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Pose de agachado sin clip propio (T-674): en LateUpdate baja la cadera, dobla las rodillas con un cálculo de dos huesos para que los pies
    /// se queden donde la animación los puso, y endereza el torso. Se monta encima de cualquier animación (idle, caminar agachado, apuntar).
    /// Va en el mismo objeto que el Animator; VisualPersonaje la agrega sola al agacharse. Cada hueso se orienta por la línea hacia su hijo,
    /// así que no depende de los ejes locales del rig.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PoseAgachado : MonoBehaviour
    {
        [Tooltip("Cuánto baja la cadera quieto, como fracción del largo de la pierna.")]
        [Range(0f, 0.8f)] [SerializeField] private float bajadaQuieto = 0.5f;
        [Tooltip("Cuánto baja la cadera caminando agachado (el clip ya viene un poco bajo), como fracción del largo de la pierna.")]
        [Range(0f, 0.8f)] [SerializeField] private float bajadaCaminando = 0.32f;
        [Tooltip("Cuánto se echa la cadera hacia atrás al agacharse, como fracción del largo de la pierna.")]
        [Range(0f, 0.5f)] [SerializeField] private float cadereAtras = 0.12f;
        [Tooltip("Inclinación del torso hacia delante (0 = recto, 1 = el de la animación). Agachado el torso queda casi recto.")]
        [Range(0f, 1f)] [SerializeField] private float inclinacionTorso = 0.25f;
        [Tooltip("Rapidez con la que entra y sale de la pose (1/s).")]
        [Min(1f)] [SerializeField] private float rapidez = 8f;
        [Tooltip("Velocidad (m/s) desde la que cuenta como caminando agachado.")]
        [Min(0f)] [SerializeField] private float umbralCaminar = 0.3f;

        private Animator animador;
        private VisualPersonaje visual;
        private Transform raizVisual;
        private float peso;
        private float caminando;

        /// <summary>Peso actual de la pose, de 0 a 1.</summary>
        public float Peso => peso;

        private void Awake()
        {
            animador = GetComponent<Animator>();
            visual = GetComponentInParent<VisualPersonaje>();
            raizVisual = visual != null ? visual.transform : transform;
        }

        private void LateUpdate()
        {
            if (animador == null || !animador.isHuman)
            {
                return;
            }
            bool agachado = visual != null && visual.Agachado && !visual.Caido && !visual.Manejando;
            peso = Mathf.MoveTowards(peso, agachado ? 1f : 0f, rapidez * Time.deltaTime);
            float objetivoCamina = visual != null && visual.Velocidad > umbralCaminar ? 1f : 0f;
            caminando = Mathf.MoveTowards(caminando, objetivoCamina, rapidez * Time.deltaTime);
            if (peso <= 0f)
            {
                return;
            }

            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            Transform musloI = animador.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform piernaI = animador.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform pieI = animador.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform musloD = animador.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Transform piernaD = animador.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            Transform pieD = animador.GetBoneTransform(HumanBodyBones.RightFoot);
            if (cadera == null || musloI == null || piernaI == null || pieI == null || musloD == null || piernaD == null || pieD == null)
            {
                return;
            }

            Vector3 frente = Vector3.ProjectOnPlane(raizVisual.forward, Vector3.up).normalized;
            Vector3 posPieI = pieI.position;
            Vector3 posPieD = pieD.position;
            Quaternion rotPieI = pieI.rotation;
            Quaternion rotPieD = pieD.rotation;
            float largoPierna = (piernaI.position - musloI.position).magnitude + (pieI.position - piernaI.position).magnitude;
            float bajada = Mathf.Lerp(bajadaQuieto, bajadaCaminando, caminando) * largoPierna * peso;
            cadera.position += Vector3.down * bajada - frente * (cadereAtras * largoPierna * peso);

            Resolver(musloI, piernaI, pieI, posPieI, frente);
            Resolver(musloD, piernaD, pieD, posPieD, frente);
            pieI.rotation = Quaternion.Slerp(pieI.rotation, rotPieI, peso);
            pieD.rotation = Quaternion.Slerp(pieD.rotation, rotPieD, peso);

            // Torso: se endereza hacia arriba, sin tocar los brazos que ya trae la animación.
            float enderezar = (1f - inclinacionTorso) * peso * caminando;
            if (enderezar > 0f)
            {
                Transform columna = animador.GetBoneTransform(HumanBodyBones.Spine);
                Transform pecho = animador.GetBoneTransform(HumanBodyBones.Chest);
                if (columna != null && pecho != null)
                {
                    Vector3 actual = pecho.position - columna.position;
                    Vector3 recto = (Vector3.up + frente * 0.1f).normalized;
                    Vector3 mezcla = Vector3.Slerp(actual.normalized, recto, enderezar);
                    columna.rotation = Quaternion.FromToRotation(actual, mezcla) * columna.rotation;
                }
            }
        }

        // Dos huesos: lleva el pie a <paramref name="destino"/> doblando la rodilla hacia delante.
        private static void Resolver(Transform muslo, Transform pierna, Transform pie, Vector3 destino, Vector3 frente)
        {
            float l1 = (pierna.position - muslo.position).magnitude;
            float l2 = (pie.position - pierna.position).magnitude;
            Vector3 aDestino = destino - muslo.position;
            float d = Mathf.Clamp(aDestino.magnitude, Mathf.Abs(l1 - l2) + 0.01f, l1 + l2 - 0.001f);
            if (aDestino.sqrMagnitude < 1e-8f)
            {
                return;
            }
            Vector3 eje = aDestino.normalized;
            Vector3 polo = Vector3.ProjectOnPlane(frente, eje);
            if (polo.sqrMagnitude < 1e-6f)
            {
                return;
            }
            polo.Normalize();
            float a = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - a * a));
            Vector3 rodilla = muslo.position + eje * a + polo * h;

            muslo.rotation = Quaternion.FromToRotation(pierna.position - muslo.position, rodilla - muslo.position) * muslo.rotation;
            pierna.rotation = Quaternion.FromToRotation(pie.position - pierna.position, destino - pierna.position) * pierna.rotation;
        }
    }
}

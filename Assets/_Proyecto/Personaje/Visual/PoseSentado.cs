using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Pose de persona sentada escribiendo, sin clip (T-639): en LateUpdate baja la cadera a la altura de la silla, pone los muslos hacia el frente,
    /// las pantorrillas hacia abajo y los brazos hacia el teclado, con un vaivén corto de manos y cabeza al teclear.
    /// Va en el mismo objeto que el Animator (el pose se aplica encima de lo que haga). Cada hueso se orienta por la dirección hacia su hijo,
    /// así que no depende de los ejes locales del rig. El frente es el forward de este objeto.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PoseSentado : MonoBehaviour
    {
        [Tooltip("Altura de la cadera sobre el piso (m).")]
        [SerializeField] private float alturaCadera = 0.5f;

        [Tooltip("Amplitud del vaivén de las manos al teclear (m).")]
        [SerializeField] private float vaiven = 0.02f;

        private Animator animador;
        private Vector3 piso;

        /// <summary>Altura de la cadera sobre el piso en la pose (m).</summary>
        public float AlturaCadera => alturaCadera;

        private void Awake()
        {
            animador = GetComponent<Animator>();
        }

        private void Start()
        {
            piso = transform.position;
        }

        private void LateUpdate()
        {
            if (animador == null || !animador.isHuman)
            {
                return;
            }
            Vector3 frente = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float t = Time.time;
            float tecla = Mathf.Sin(t * 14f) * vaiven;
            float tecla2 = Mathf.Sin(t * 11f + 1.7f) * vaiven;

            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            if (cadera != null)
            {
                // La cadera queda sobre el punto de piso del visual, a la altura de la silla.
                cadera.position = new Vector3(piso.x, piso.y + alturaCadera, piso.z);
            }
            Orientar(HumanBodyBones.Spine, HumanBodyBones.Chest, Vector3.up + frente * 0.12f);
            Orientar(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, frente);
            Orientar(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, frente);
            Orientar(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, Vector3.down);
            Orientar(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, Vector3.down);
            Orientar(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, Vector3.down * 0.9f + frente * 0.35f);
            Orientar(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, Vector3.down * 0.9f + frente * 0.35f);
            Orientar(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, frente + Vector3.down * (0.08f + tecla * 4f));
            Orientar(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, frente + Vector3.down * (0.08f + tecla2 * 4f));
            Orientar(HumanBodyBones.Neck, HumanBodyBones.Head, Vector3.up + frente * (0.15f + tecla * 2f));
        }

        // Gira el hueso para que la línea hacia su hijo apunte a la dirección pedida.
        private void Orientar(HumanBodyBones hueso, HumanBodyBones hijo, Vector3 direccion)
        {
            Transform h = animador.GetBoneTransform(hueso);
            Transform c = animador.GetBoneTransform(hijo);
            if (h == null || c == null || direccion.sqrMagnitude < 1e-6f)
            {
                return;
            }
            Vector3 actual = c.position - h.position;
            if (actual.sqrMagnitude < 1e-8f)
            {
                return;
            }
            h.rotation = Quaternion.FromToRotation(actual, direccion.normalized) * h.rotation;
        }
    }
}

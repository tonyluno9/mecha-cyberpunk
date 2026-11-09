using UnityEngine;

namespace MechaCyberpunk.Casino.Meseras
{
    /// <summary>
    /// T-930: la mesera R01 lleva la charola fija en la mano izquierda (plana en la pose T). Con los clips de caminar el brazo baja y la charola queda vertical junto al muslo: después del
    /// Animator se dobla el brazo izquierdo, codo al costado y antebrazo al frente, y la palma se pone hacia arriba, para que la charola quede plana a la altura de la cintura.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(40)]
    public class BrazoCharola : MonoBehaviour
    {
        [Tooltip("Dirección del brazo (hombro a codo) en el espacio del cuerpo, sin unidades.")]
        [SerializeField] private Vector3 direccionBrazo = new Vector3(0.28f, -0.92f, 0.25f);
        [Tooltip("Dirección del antebrazo (codo a muñeca) en el espacio del cuerpo, sin unidades.")]
        [SerializeField] private Vector3 direccionAntebrazo = new Vector3(0.22f, 0.1f, 0.97f);

        private Transform hombro, codo, mano;
        private Vector3 arribaAntebrazo, arribaMano;

        /// <summary>Hacia dónde mira la cara de arriba de la charola, en el mundo (en la pose T apunta hacia arriba).</summary>
        public Vector3 NormalCharola => mano.TransformDirection(arribaMano);

        /// <summary>La mano izquierda.</summary>
        public Transform Mano => mano;

        private void Awake()
        {
            Animator animador = GetComponent<Animator>();
            hombro = animador.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            codo = animador.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            mano = animador.GetBoneTransform(HumanBodyBones.LeftHand);
            if (hombro == null || codo == null || mano == null)
            {
                Debug.LogError("[Casino] BrazoCharola en '" + name + "' necesita los huesos del brazo izquierdo.", this);
                enabled = false;
                return;
            }
            // En Awake el esqueleto sigue en la pose T del modelo: ahí la charola está plana.
            arribaAntebrazo = codo.InverseTransformDirection(Vector3.up);
            arribaMano = mano.InverseTransformDirection(Vector3.up);
        }

        private void LateUpdate()
        {
            Girar(hombro, codo.position - hombro.position, transform.TransformDirection(direccionBrazo));
            Girar(codo, mano.position - codo.position, transform.TransformDirection(direccionAntebrazo));
            // La charola la mueve el antebrazo o la mano según el pesado del modelo: se endereza el giro de los dos.
            Vector3 eje = (mano.position - codo.position).normalized;
            Vector3 actual = Vector3.ProjectOnPlane(codo.TransformDirection(arribaAntebrazo), eje);
            codo.rotation = Quaternion.FromToRotation(actual, Vector3.ProjectOnPlane(Vector3.up, eje)) * codo.rotation;
            mano.rotation = Quaternion.FromToRotation(NormalCharola, Vector3.up) * mano.rotation;
        }

        private static void Girar(Transform hueso, Vector3 actual, Vector3 deseada)
        {
            hueso.rotation = Quaternion.FromToRotation(actual, deseada) * hueso.rotation;
        }
    }
}

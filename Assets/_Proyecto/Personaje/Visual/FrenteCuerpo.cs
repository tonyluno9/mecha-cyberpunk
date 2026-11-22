using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1622: mide hacia dónde mira el cuerpo de un personaje Humanoid (por la línea entre sus dos muslos) y cuánto se desvía de su raíz. Los clips de plática
    /// con orientación original (+180° de desfase) pueden dejar al personaje de espaldas a su pareja aunque la raíz mire bien; con esta medida se corrige el giro.
    /// </summary>
    public static class FrenteCuerpo
    {
        /// <summary>Mínimo de desvío (°) que vale la pena corregir.</summary>
        public const float DesvioMinimo = 20f;

        /// <summary>
        /// Grados con signo (eje Y) desde <paramref name="raiz"/>.forward hasta el frente del cuerpo; false si el Animator no es Humanoid o le faltan los muslos.
        /// </summary>
        public static bool Medir(Animator animador, Transform raiz, out float grados)
        {
            grados = 0f;
            if (animador == null || raiz == null || !animador.isHuman)
            {
                return false;
            }
            Transform izquierdo = animador.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform derecho = animador.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            if (izquierdo == null || derecho == null)
            {
                return false;
            }
            Vector3 haciaLaDerecha = derecho.position - izquierdo.position;
            haciaLaDerecha.y = 0f;
            if (haciaLaDerecha.sqrMagnitude < 0.0001f)
            {
                return false;
            }
            Vector3 frente = Vector3.Cross(haciaLaDerecha.normalized, Vector3.up);
            Vector3 base0 = raiz.forward;
            base0.y = 0f;
            if (base0.sqrMagnitude < 0.0001f)
            {
                return false;
            }
            grados = Vector3.SignedAngle(base0.normalized, frente, Vector3.up);
            return true;
        }
    }
}

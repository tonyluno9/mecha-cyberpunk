using UnityEngine;

namespace MechaCyberpunk.Progresion.Apoyo
{
    /// <summary>
    /// Pose de sentado en la silla del Doc (T-813), sin clip ni tocar el Animator Controller del jugador: no hay clip de sentado en el proyecto
    /// (el "hacker sentado" de T-639 también es por huesos), así que en LateUpdate, encima de lo que haga el Animator, pone la cadera en el
    /// asiento, la espalda un poco recostada, los muslos al frente, las piernas estiradas hacia abajo y los brazos en los descansabrazos.
    /// La silla la agrega al objeto del Animator del jugador al sentarlo y la quita al levantarlo. Cada hueso se orienta por la dirección hacia
    /// su hijo, así que no depende de los ejes del rig. <see cref="Peso"/> (0 a 1) mezcla con la pose del Animator para sentarse y pararse.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PoseSillaClinica : MonoBehaviour
    {
        private const float MinimoDireccion = 1e-6f;

        private Animator animador;
        private Vector3 asiento;
        private Vector3 frente = Vector3.forward;
        private float recostado = 0.35f;

        /// <summary>Cuánto manda la pose (0 = la del Animator, 1 = sentado).</summary>
        public float Peso { get; set; } = 1f;

        /// <summary>Dónde queda la cadera (m, mundo).</summary>
        public Vector3 Asiento => asiento;

        public void Configurar(Vector3 puntoAsiento, Vector3 direccionFrente, float espalda)
        {
            asiento = puntoAsiento;
            Vector3 plano = Vector3.ProjectOnPlane(direccionFrente, Vector3.up);
            frente = plano.sqrMagnitude > MinimoDireccion ? plano.normalized : Vector3.forward;
            recostado = Mathf.Clamp01(espalda);
        }

        private void Awake()
        {
            animador = GetComponent<Animator>();
        }

        private void LateUpdate()
        {
            if (animador == null || !animador.isHuman || Peso <= 0f)
            {
                return;
            }
            Vector3 arriba = Vector3.up;
            Vector3 abajo = Vector3.down;
            Vector3 atras = -frente;
            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            if (cadera != null)
            {
                cadera.position = Vector3.Lerp(cadera.position, asiento, Peso);
            }
            Vector3 espalda = Vector3.Slerp(arriba, atras, recostado * 0.8f);
            Orientar(HumanBodyBones.Spine, HumanBodyBones.Chest, espalda);
            Orientar(HumanBodyBones.Neck, HumanBodyBones.Head, Vector3.Slerp(arriba, atras, recostado * 0.5f));
            // Silla larga de consultorio: muslos y piernas casi derechos sobre el reposapiés.
            Vector3 muslo = frente + arriba * 0.08f;
            Orientar(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, muslo);
            Orientar(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, muslo);
            Vector3 pierna = frente * 0.85f + abajo * 0.35f;
            Orientar(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, pierna);
            Orientar(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, pierna);
            Vector3 brazo = abajo * 0.85f + frente * 0.3f;
            Orientar(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, brazo);
            Orientar(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, brazo);
            Vector3 antebrazo = frente + abajo * 0.15f;
            Orientar(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, antebrazo);
            Orientar(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, antebrazo);
        }

        // Gira el hueso para que la línea hacia su hijo apunte a la dirección pedida (mezclado con Peso).
        private void Orientar(HumanBodyBones hueso, HumanBodyBones hijo, Vector3 direccion)
        {
            Transform h = animador.GetBoneTransform(hueso);
            Transform c = animador.GetBoneTransform(hijo);
            if (h == null || c == null || direccion.sqrMagnitude < MinimoDireccion)
            {
                return;
            }
            Vector3 actual = c.position - h.position;
            if (actual.sqrMagnitude < MinimoDireccion)
            {
                return;
            }
            Quaternion giro = Quaternion.FromToRotation(actual.normalized, direccion.normalized);
            h.rotation = Quaternion.Slerp(Quaternion.identity, giro, Peso) * h.rotation;
        }
    }
}

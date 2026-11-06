using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-674: agachado se ve agachado (cadera abajo, rodillas dobladas, pies en el piso, torso recto) quieto, caminando y apuntando.</summary>
    public class PoseAgachadoT674Pruebas : BaseJugadorAPie
    {
        private IEnumerator Pulsar(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private Animator Anim(out VisualPersonaje visual)
        {
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return visual.Animador;
        }

        private float Alto(Animator a, HumanBodyBones hueso) => a.GetBoneTransform(hueso).position.y - jugador.transform.position.y;

        private void Fotos(string nombre)
        {
            Transform t = jugador.transform;
            CapturaS1.Foto("s1n-T-674-" + nombre, t, new Vector3(3.0f, 1.0f, 0.3f), new Vector3(0f, 0.8f, 0.3f), 40f, 700, 700);
        }

        [UnityTest]
        public IEnumerator AgachadoQuieto_CaderaBajaYCabezaBajaDeLaCapsula_PiesEnElPiso()
        {
            yield return Calentar();
            Animator a = Anim(out VisualPersonaje visual);
            yield return Esperar(0.6f);
            float caderaDePie = Alto(a, HumanBodyBones.Hips);
            float cabezaDePie = Alto(a, HumanBodyBones.Head);
            Fotos("de-pie");
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(1.2f);
            PoseAgachado pose = a.GetComponent<PoseAgachado>();
            Assert.IsNotNull(pose, "VisualPersonaje agrega la pose al agacharse");
            Assert.AreEqual(1f, pose.Peso, 0.01f);
            float cadera = Alto(a, HumanBodyBones.Hips);
            float cabeza = Alto(a, HumanBodyBones.Head);
            float pieI = Alto(a, HumanBodyBones.LeftFoot);
            float pieD = Alto(a, HumanBodyBones.RightFoot);
            Assert.Less(cadera, caderaDePie - 0.3f, "La cadera baja (" + cadera + " contra " + caderaDePie + ")");
            Assert.Less(cabeza, cabezaDePie - 0.4f, "La cabeza baja (" + cabeza + " contra " + cabezaDePie + ")");
            Assert.Less(cabeza, jugador.GetComponent<CharacterController>().height + 0.25f, "La cabeza coincide con la cápsula baja");
            Assert.Greater(Mathf.Min(pieI, pieD), -0.05f, "Los pies no se hunden en el piso");
            Assert.Less(Mathf.Max(pieI, pieD), 0.3f, "Los pies siguen en el piso");
            // Torso derecho: de la cadera a la cabeza casi vertical.
            Vector3 torso = a.GetBoneTransform(HumanBodyBones.Head).position - a.GetBoneTransform(HumanBodyBones.Hips).position;
            Assert.Less(Vector3.Angle(torso, Vector3.up), 25f, "El torso queda derecho");
            // Rodillas dobladas.
            Vector3 muslo = a.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position - a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
            Vector3 pierna = a.GetBoneTransform(HumanBodyBones.LeftFoot).position - a.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
            Assert.Greater(Vector3.Angle(muslo, pierna), 40f, "Las rodillas están dobladas");
            Fotos("agachado-quieto");
            yield return Pulsar(teclado.leftCtrlKey);
            yield return Esperar(1.0f);
            Assert.AreEqual(0f, pose.Peso, 0.01f, "Al levantarse la pose se apaga");
            Assert.AreEqual(cabezaDePie, Alto(a, HumanBodyBones.Head), 0.1f, "Vuelve a su altura");
        }

        [UnityTest]
        public IEnumerator AgachadoCaminando_SigueBajo_YApuntando_ConArmaEnPose()
        {
            yield return Calentar();
            Animator a = Anim(out VisualPersonaje visual);
            yield return Esperar(0.6f);
            float cabezaDePie = Alto(a, HumanBodyBones.Head);
            yield return Pulsar(teclado.leftCtrlKey);
            Press(teclado.wKey);
            yield return Esperar(1.2f);
            Assert.Less(Alto(a, HumanBodyBones.Head), cabezaDePie - 0.25f, "Caminando agachado la cabeza va baja (" + Alto(a, HumanBodyBones.Head) + " contra " + cabezaDePie + ")");
            Fotos("agachado-caminando");
            Release(teclado.wKey);
            yield return Esperar(0.5f);
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            Press(raton.rightButton);
            yield return Esperar(1.0f);
            Assert.Greater(visual.PesoCapaArriba, 0.9f, "Apuntando agachado los brazos usan la pose de apuntar");
            Assert.Less(Alto(a, HumanBodyBones.Head), cabezaDePie - 0.25f, "Apuntando agachado la cabeza sigue baja (" + Alto(a, HumanBodyBones.Head) + ")");
            Fotos("agachado-apuntando");
            Release(raton.rightButton);
        }
    }
}

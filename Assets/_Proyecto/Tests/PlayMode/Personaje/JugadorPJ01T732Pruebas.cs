using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-732 (A2): el jugador es PJ01, Humanoid, mide 1.80 m animado, R_Hand sin escala extra y material URP con sus texturas.</summary>
    public class JugadorPJ01T732Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);

        [UnityTest]
        public IEnumerator Jugador_UsaElModeloPJ01_Humanoid()
        {
            yield return Calentar();
            Animator a = Visual().Animador;
            Assert.IsNotNull(a.avatar);
            Assert.IsTrue(a.avatar.isHuman, "avatar humanoide");
            Transform mano = a.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.IsNotNull(mano);
            Assert.AreEqual("R_Hand", mano.name, "la mano derecha de PJ01");
            Assert.IsNotNull(mano.Find("ManoDer"), "socket del arma en la mano");
            Assert.IsTrue(a.name == "Modelo" && a.transform.parent.parent == Visual().transform, "Visual/Giro/Modelo");
            Assert.IsNotNull(a.GetComponent<ApuntadoIK>());
            Assert.IsNotNull(a.GetComponent<IKPiesYMirada>());
        }

        [UnityTest]
        public IEnumerator Jugador_PJ01_Mide180Animado_YLaManoNoTrae_EscalaExtra()
        {
            yield return Calentar();
            Visual().Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return Esperar(1.5f);
            float altura = EscalaJugadorT670Pruebas.AlturaAnimada(Visual().gameObject);
            Debug.Log("T732 altura animada " + altura.ToString("F3"));
            Assert.AreEqual(1.8f, altura, 0.05f, "PJ01 animado mide 1.80 m (BakeMesh)");
            Transform mano = Visual().Animador.GetBoneTransform(HumanBodyBones.RightHand);
            Debug.Log("T732 R_Hand lossyScale " + mano.lossyScale.ToString("F4"));
            Assert.AreEqual(1f, mano.lossyScale.x, 0.03f, "caso A de agarre.txt: R_Hand con lossyScale 1");
        }

        [UnityTest]
        public IEnumerator Jugador_PJ01_TieneMaterialUrpConTexturas()
        {
            yield return Calentar();
            int cuantos = 0;
            foreach (SkinnedMeshRenderer r in Visual().GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Material m = r.sharedMaterial;
                Assert.IsNotNull(m, r.name);
                Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name, r.name);
                Assert.IsNotNull(m.GetTexture("_BaseMap"), "color " + r.name);
                Assert.IsNotNull(m.GetTexture("_BumpMap"), "normal " + r.name);
                cuantos++;
            }
            Assert.Greater(cuantos, 0);
        }

        [UnityTest]
        public IEnumerator Jugador_PJ01_ConservaColliderYCamara()
        {
            yield return Calentar();
            CharacterController cc = jugador.GetComponent<CharacterController>();
            Assert.IsNotNull(cc);
            Assert.AreEqual(1.8f, cc.height, 0.2f);
            Assert.IsNotNull(jugador.GetComponentInChildren<Camera>(true), "cámara del jugador");
            CapturaS1.FotoCamara("pj01-camara", jugador.GetComponentInChildren<Camera>());
            if (CapturaS1.Activa)
            {
                // Foto de cuerpo entero desde enfrente, con el idle ya animado.
                Visual().Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                yield return Esperar(1.0f);
                GameObject ojo = new GameObject("OjoPJ01");
                Camera cam = ojo.AddComponent<Camera>();
                cam.fieldOfView = 35f;
                ojo.transform.position = jugador.transform.position + jugador.transform.forward * 5f + Vector3.up * 1.0f;
                ojo.transform.rotation = Quaternion.LookRotation(jugador.transform.position + Vector3.up * 0.9f - ojo.transform.position);
                CapturaS1.FotoCamara("pj01-cuerpo-frente", cam, 720, 1080);
                ojo.transform.position = jugador.transform.position + jugador.transform.right * 5f + Vector3.up * 1.0f;
                ojo.transform.rotation = Quaternion.LookRotation(jugador.transform.position + Vector3.up * 0.9f - ojo.transform.position);
                CapturaS1.FotoCamara("pj01-cuerpo-lado", cam, 720, 1080);
                Object.Destroy(ojo);
            }
        }

        [UnityTest]
        public IEnumerator Jugador_PJ01_AlCaerSeTumbaYSeLevanta()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Transform cabeza = visual.Animador.GetBoneTransform(HumanBodyBones.Head);
            float dePie = cabeza.position.y;
            visual.Caido = true;
            yield return Esperar(4f);
            Debug.Log("T732 cabeza caido y=" + cabeza.position.y.ToString("F3") + " de pie " + dePie.ToString("F3"));
            Assert.Less(cabeza.position.y, 0.5f, "Caído: la cabeza queda cerca del piso");
            CapturaS1.FotoCamara("pj01-caido", jugador.GetComponentInChildren<Camera>());
            visual.Caido = false;
            yield return Esperar(6f);
            Assert.Greater(cabeza.position.y, dePie * 0.9f, "Se levanta otra vez");
        }
    }
}

using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-520: el jugador a pie con su visual de Tripo: la animación cambia al caminar y correr con entrada simulada, los pies siguen el piso y el hombre sustituye a la mujer.</summary>
    public class JugadorVisualPruebas : BaseJugadorAPie
    {
        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);

        private static string Estado(Animator a)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo siguiente = a.GetNextAnimatorStateInfo(0);
            foreach (string n in ReglasVisual.Estados)
            {
                if (a.IsInTransition(0) ? siguiente.IsName(n) : actual.IsName(n))
                {
                    return n;
                }
            }
            return "?";
        }

        [UnityTest]
        public IEnumerator Caminar_ConW_PasaDeIdleACaminarYAlCorrerAEsaAnimacionConLosPiesEnElPiso()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Assert.IsNotNull(visual, "El jugador trae el visual de Tripo");
            Animator animador = visual.Animador;
            yield return Esperar(1.5f);
            Assert.AreEqual("Idle", Estado(animador), "Quieto");
            Assert.AreEqual(1f, animador.speed, 0.001f, "Quieto no se acelera");

            Press(teclado.wKey);
            yield return Esperar(0.9f);
            Assert.AreNotEqual("Idle", Estado(animador), "Al caminar cambia la animación");
            Assert.Greater(visual.Velocidad, 1f, "El visual lee la velocidad del CharacterController");
            Assert.IsTrue(Estado(animador) == "Caminar" || Estado(animador) == "Correr");

            Press(teclado.leftShiftKey);
            yield return Esperar(0.9f);
            Assert.AreEqual("Correr", Estado(animador), "Al correr usa Correr");
            Assert.Greater(animador.speed, 1f, "La animación se acelera para que los pies no patinen");
            // Pies en el piso: el hueso del pie derecho a la altura del suelo del CharacterController (piso en y = 0).
            Transform pie = animador.GetBoneTransform(HumanBodyBones.RightFoot);
            Assert.IsNotNull(pie);
            Assert.Less(pie.position.y, 0.35f, "El pie no flota (m sobre el piso)");
            Assert.Greater(pie.position.y, -0.15f, "El pie no se hunde");

            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            yield return Esperar(0.8f);
            Assert.AreEqual("Idle", Estado(animador), "Al soltar vuelve a Idle");
        }

        [UnityTest]
        public IEnumerator Caminar_SinPatinar_LaRapidezDeLaAnimacionSigueLaVelocidadReal()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Press(teclado.wKey);
            yield return Esperar(1.2f);
            float real = controlador.Velocidad.magnitude;
            Assert.Greater(real, 1f);
            UnityEditor.SerializedProperty referencia = new UnityEditor.SerializedObject(visual).FindProperty(Estado(visual.Animador) == "Correr" ? "velocidadClipCorrer" : "velocidadClipCaminar");
            Assert.Greater(referencia.floatValue, 0f);
            float esperada = Mathf.Clamp(visual.Velocidad / (referencia.floatValue * visual.EscalaClips), 0.6f, 2.2f);
            Assert.AreEqual(esperada, visual.Animador.speed, esperada * 0.15f, "rapidez = velocidad / velocidad del clip");
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Apuntar_ConPistola_LevantaElArmaPorIKYElCuerpoCaidoSeTumba()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            ApuntadoIK ik = visual.GetComponentInChildren<ApuntadoIK>(true);
            Assert.IsNotNull(ik);
            // Sin ventana de juego el Animator se duerme fuera de cámara y ni la pose ni el IK del cañón se aplican.
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.AreEqual(0f, ik.Peso, 0.001f);
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola), "El arma está en la mano (si no, no se ve ni se corrige)");
            visual.LeerDelPadre = false;
            visual.EnSuelo = true;
            visual.Apuntando = true;
            visual.TipoArma = TipoArmaVisual.Pistola;
            yield return Esperar(0.6f);
            Assert.AreEqual(1f, ik.Peso, 0.05f, "Arma arriba");
            // El cañón (-Y del socket) mira al frente del jugador.
            // S1: el arma va en la palma (ya no se gira el socket): el cañón es el +Z de la pistola.
            Vector3 canon = ik.SocketArma.Find("Pistola").forward;
            Assert.Greater(Vector3.Dot(Vector3.ProjectOnPlane(canon, Vector3.up).normalized, visual.transform.forward), 0.95f, "El cañón apunta al frente (la inclinación es la de la animación)");

            visual.Apuntando = false;
            yield return Esperar(0.6f);
            Assert.AreEqual(0f, ik.Peso, 0.05f, "Arma abajo");
            Assert.Less(ik.Peso, 0.05f, "Con el arma abajo no se corrige el cañón");

            // T-561: con el controlador base el KO usa las animaciones de muerte y levantarse; el cuerpo ya no se gira a mano.
            // Sin ventana de juego el Animator se duerme fuera de cámara y los huesos no se mueven: se le pide que anime siempre.
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            visual.Caido = true;
            yield return Esperar(2.5f);
            Assert.AreEqual(0f, visual.Tumbado, 0.01f, "KO con clip de muerte: no se tumba a mano");
            Transform cabeza = visual.Animador.GetBoneTransform(HumanBodyBones.Head);
            Assert.Less(cabeza.position.y, 1.0f, "La cabeza queda cerca del piso (m)");
            visual.Caido = false;
            float fin = Time.time + 12f;
            while (cabeza.position.y <= 1.1f && Time.time < fin)
            {
                yield return null;
            }
            Assert.Greater(cabeza.position.y, 1.1f, "De pie otra vez (el hueso de la cabeza queda a ~1.24 m en este modelo)");
        }

        [UnityTest]
        public IEnumerator ModeloSegunPerfil_ConCaraUno_PoneAlHombreYLaAnimacionSigueFuncionando()
        {
            yield return Calentar();
            GameObject host = new GameObject("ModeloSegunPerfilPrueba");
            ModeloSegunPerfil modelo = host.AddComponent<ModeloSegunPerfil>();
            var perfil = new PerfilFalso();
            Registro<IPerfilJugador>.Agregar(perfil);
            modelo.Configurar(new[]
            {
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_woman.prefab"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_man.prefab"),
            });
            yield return null;
            Assert.IsTrue(Visual().GetComponentInChildren<SkinnedMeshRenderer>().name.Contains("86ec4d7a"), "Empieza la mujer");
            int armasAntes = jugador.GetComponentsInChildren<Transform>(true).Length;

            perfil.Poner(new AparienciaJugador(1, 0, 0));
            yield return null;
            yield return null;
            VisualPersonaje visual = Visual();
            Assert.IsNotNull(visual);
            Assert.IsTrue(visual.GetComponentInChildren<SkinnedMeshRenderer>().name.Contains("0f9d95ea"), "Ahora el hombre");
            Assert.AreEqual(1, jugador.GetComponentsInChildren<VisualPersonaje>(true).Length, "Solo queda un visual");
            Transform pistola = visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find("Pistola");
            Assert.IsNotNull(pistola, "La pistola pasó a la mano del hombre");

            Press(teclado.wKey);
            yield return Esperar(0.9f);
            Assert.AreNotEqual("Idle", Estado(visual.Animador), "El hombre también camina");
            Release(teclado.wKey);
            Assert.Greater(armasAntes, 0);
            Registro<IPerfilJugador>.Quitar(perfil);
            Object.Destroy(host);
        }

        private class PerfilFalso : IPerfilJugador
        {
            private AparienciaJugador apariencia = AparienciaJugador.Predeterminada;

            public string Nombre => "T";
            public string Alias => "T";
            public AparienciaJugador Apariencia => apariencia;
            public string NombrePandilla => "P";
            public int ColorPandilla => 0;
            public event System.Action Cambio;

            public void Poner(AparienciaJugador nueva)
            {
                apariencia = nueva;
                Cambio?.Invoke();
            }
        }
    }
}

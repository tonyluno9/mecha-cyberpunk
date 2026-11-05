using System.Collections;
using System.Linq;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>S1 (T-650/T-651): el arma va en la palma, el cañón al frente, y con arma en mano se usan las animaciones de arma al caminar y correr.</summary>
    public class ArmaEnManoS1Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);

        private IEnumerator Fotos(string sufijo)
        {
            Transform t = jugador.transform;
            // Lado derecho de cerca (se ve la mano y el arma) y tres cuartos de frente.
            CapturaS1.Foto("s1-" + sufijo + "-lado", t, new Vector3(2.4f, 1.0f, 0.3f), new Vector3(0f, 0.95f, 0.3f), 45f);
            CapturaS1.Foto("s1-" + sufijo + "-mano", t, new Vector3(0.95f, 1.0f, 0.75f), new Vector3(0.05f, 0.95f, 0.3f), 30f);
            yield return null;
        }


        private static bool EnEstado(Animator a, int capa, string nombre)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(capa);
            AnimatorStateInfo siguiente = a.GetNextAnimatorStateInfo(capa);
            return a.IsInTransition(capa) ? siguiente.IsName(nombre) || actual.IsName(nombre) : actual.IsName(nombre);
        }

        [UnityTest]
        public IEnumerator ConPistola_CaminarYCorrer_LasPiernasUsanPistolaCaminarYCorrer()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            int capa = a.GetLayerIndex(ReglasAnimacion.CapaPiernas);
            Assert.GreaterOrEqual(capa, 0, "El controlador tiene la capa Piernas");
            yield return Esperar(0.4f);
            Assert.AreEqual(0f, a.GetLayerWeight(capa), 0.01f, "Sin arma, las piernas son de la capa base");
            Press(teclado.wKey);
            yield return Esperar(0.8f);
            Assert.AreEqual(0f, visual.PesoPiernasArma, 0.01f, "Con los puños caminar usa la caminata normal");
            Release(teclado.wKey);
            yield return Esperar(0.6f);

            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            yield return Esperar(0.3f);
            Assert.AreEqual(0f, visual.PesoPiernasArma, 0.01f, "Quieto con pistola, piernas de la base");
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.AreEqual(1f, visual.PesoPiernasArma, 0.05f, "Caminando con pistola sin apuntar: piernas de Pistola_Caminar");
            Assert.IsTrue(EnEstado(a, capa, "PistolaPiernas"));
            Assert.AreEqual(1f, a.GetLayerWeight(capa), 0.05f);
            Assert.AreEqual("Pistola_Caminar", a.GetCurrentAnimatorClipInfo(capa).OrderByDescending(c => c.weight).First().clip.name);

            Press(teclado.leftShiftKey);
            yield return Esperar(1.0f);
            Assert.AreEqual(1f, visual.PesoPiernasArma, 0.05f);
            Assert.AreEqual("Pistola_Correr", a.GetCurrentAnimatorClipInfo(capa).OrderByDescending(c => c.weight).First().clip.name, "Con Shift, Pistola_Correr");
            float esperada = Mathf.Clamp(visual.Velocidad / ReglasAnimacion.VelocidadClipArma(TipoArmaVisual.Pistola, visual.Velocidad), 0.6f, 2.8f);
            Assert.AreEqual(esperada, a.speed, esperada * 0.15f, "Los pies siguen la velocidad real con el clip del arma");
            Release(teclado.leftShiftKey);

            // Apuntando se queda la locomoción de la base (con dirección).
            Press(raton.rightButton);
            yield return Esperar(0.6f);
            Assert.AreEqual(0f, visual.PesoPiernasArma, 0.05f, "Apuntando, la base maneja las piernas");
            Release(raton.rightButton);
            Release(teclado.wKey);

            // Arma larga: Rifle_Caminar y Rifle_Correr.
            yield return Esperar(0.6f);
            visual.TipoArma = TipoArmaVisual.ArmaLarga;
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.IsTrue(EnEstado(a, capa, "LargaPiernas"));
            Assert.AreEqual("Rifle_Caminar", a.GetCurrentAnimatorClipInfo(capa).OrderByDescending(c => c.weight).First().clip.name);
            Press(teclado.leftShiftKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("Rifle_Correr", a.GetCurrentAnimatorClipInfo(capa).OrderByDescending(c => c.weight).First().clip.name);
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
        }
        [UnityTest]
        public IEnumerator Capturas_ArmaEnMano()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola));
            Transform mano = visual.Animador.GetBoneTransform(HumanBodyBones.RightHand);
            foreach (Transform h in mano.GetComponentsInChildren<Transform>(true))
            {
                Debug.Log("[S1] " + h.name + " activo=" + h.gameObject.activeInHierarchy + " pos=" + h.position + " localPos=" + h.localPosition + " localRot=" + h.localEulerAngles);
            }
            yield return Esperar(1f);
            yield return Fotos("pistola-quieto");
            Press(teclado.wKey);
            yield return Esperar(1.2f);
            yield return Fotos("pistola-caminar");
            Press(teclado.leftShiftKey);
            yield return Esperar(1.2f);
            yield return Fotos("pistola-correr");
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            yield return Esperar(0.6f);
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            yield return Fotos("pistola-apuntar");
            Press(raton.leftButton);
            yield return Esperar(0.12f);
            yield return Fotos("pistola-disparo");
            Release(raton.leftButton);
            Release(raton.rightButton);
            yield return Esperar(0.8f);
            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return Esperar(0.6f);
            yield return Fotos("pistola-recargar");
        }
    }
}

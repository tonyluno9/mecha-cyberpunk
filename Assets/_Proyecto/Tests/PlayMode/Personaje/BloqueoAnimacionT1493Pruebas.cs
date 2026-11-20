using System.Collections;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1493: al bajar de la moto con W apretado el jugador no se mueve hasta que termina la animación, y después sí.</summary>
    public class BloqueoAnimacionT1493Pruebas : BaseJugadorAPie
    {
        private VisualPersonaje visual;

        public override void Setup()
        {
            base.Setup();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
        }

        private bool EnMotoBajar()
        {
            Animator a = visual.Animador;
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(0);
            return actual.IsName("MotoBajar") || (a.IsInTransition(0) && a.GetNextAnimatorStateInfo(0).IsName("MotoBajar"));
        }

        [UnityTest]
        public IEnumerator MotoBajar_ConWApretada_NoSeMueveHastaTerminar_YDespuesSi()
        {
            yield return Calentar();
            Assert.IsNotNull(visual, "el jugador tiene VisualPersonaje");
            Press(teclado.wKey);
            visual.Gestear(GestoVisual.MotoBajar);

            float limite = Time.time + 2f;
            while (!EnMotoBajar() && Time.time < limite) { yield return null; }
            Assert.IsTrue(EnMotoBajar(), "llegó al estado MotoBajar");

            Vector3 inicio = jugador.transform.position;
            float giroInicio = jugador.transform.eulerAngles.y;
            int cuadros = 0;
            while (EnMotoBajar() && cuadros < 600)
            {
                yield return null;
                cuadros++;
                if (!EnMotoBajar()) { break; }
                Assert.IsTrue(controlador.BloqueadoPorAnimacion, "bloqueado mientras dura MotoBajar");
                Vector3 d = jugador.transform.position - inicio;
                d.y = 0f;
                Assert.Less(d.magnitude, 0.05f, "no se movió durante MotoBajar");
            }
            Assert.Less(cuadros, 600, "MotoBajar terminó");
            Assert.AreEqual(giroInicio, jugador.transform.eulerAngles.y, 1f, "el cuerpo no giró");

            Vector3 trasTerminar = jugador.transform.position;
            yield return Esperar(1f);
            Vector3 despues = jugador.transform.position - trasTerminar;
            despues.y = 0f;
            Assert.IsFalse(controlador.BloqueadoPorAnimacion, "ya no bloquea");
            Assert.Greater(despues.magnitude, 0.5f, "después de terminar sí se mueve");
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Golpe_NoBloquea()
        {
            yield return Calentar();
            Press(teclado.wKey);
            yield return Esperar(0.5f);
            Assert.IsFalse(controlador.BloqueadoPorAnimacion, "caminar normal no bloquea");
            Release(teclado.wKey);
        }
    }
}

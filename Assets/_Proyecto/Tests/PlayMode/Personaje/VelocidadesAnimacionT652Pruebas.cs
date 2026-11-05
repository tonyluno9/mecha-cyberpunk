using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-652: las animaciones del jugador siguen el ritmo real del juego (puños y salto) y no tienen esperas de varios segundos.</summary>
    public class VelocidadesAnimacionT652Pruebas : BaseJugadorAPie
    {
        private static bool EnGolpe(Animator a)
        {
            AnimatorStateInfo s = a.GetCurrentAnimatorStateInfo(1);
            return s.IsName("GolpeIzq") || s.IsName("GolpeDer") || s.IsName("GolpeFuerte");
        }

        [UnityTest]
        public IEnumerator Golpe_ConClic_LaAnimacionDePunoTerminaEnMenosDeUnSegundo()
        {
            yield return Calentar();
            Animator a = jugador.GetComponentInChildren<VisualPersonaje>(true).Animador;
            yield return Esperar(0.5f);
            Press(raton.leftButton);
            float t0 = Time.time;
            yield return null;
            Release(raton.leftButton);
            float limite = Time.time + 2f;
            while (!EnGolpe(a) && Time.time < limite) { yield return null; }
            Assert.IsTrue(EnGolpe(a), "El clic lanza la animación de puño");
            Assert.AreEqual(ReglasAnimacion.RapidezGolpeJugador, a.GetCurrentAnimatorStateInfo(1).speed * a.GetFloat(ReglasAnimacion.RapidezGolpe) / Mathf.Max(0.0001f, a.GetCurrentAnimatorStateInfo(1).speed), 0.01f, "RapidezGolpe del jugador");
            float t1 = Time.time;
            while (EnGolpe(a) && Time.time < t1 + 3f) { yield return null; }
            Assert.Less(Time.time - t0, 1.0f, "El puño (clip de 1.8 a 2.2 s) dura menos de 1 s con la rapidez del jugador (s)");
        }

        [UnityTest]
        public IEnumerator Saltar_ConEspacio_LaAnimacionDeDespegueNoRetrasaElSalto()
        {
            yield return Calentar();
            Animator a = jugador.GetComponentInChildren<VisualPersonaje>(true).Animador;
            yield return Esperar(0.5f);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            float limite = Time.time + 1.5f;
            while (!a.GetCurrentAnimatorStateInfo(0).IsName("SaltoInicio") && Time.time < limite) { yield return null; }
            Assert.IsTrue(a.GetCurrentAnimatorStateInfo(0).IsName("SaltoInicio"), "Salta con espacio");
            Assert.AreEqual(ReglasAnimacion.VelocidadEstado("SaltoInicio"), a.GetCurrentAnimatorStateInfo(0).speed, 0.01f);
            Assert.Greater(ReglasAnimacion.VelocidadEstado("SaltoInicio"), 1.5f);
        }
    }
}

using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1487: el hitstop congela y restaura los Animator, y el golpe empuja al golpeado sin atravesar paredes.</summary>
    public class SensacionGolpeT1487Pruebas
    {
        private GameObject atacante;
        private GameObject golpeado;

        [TearDown]
        public void TearDown()
        {
            if (atacante != null) Object.Destroy(atacante);
            if (golpeado != null) Object.Destroy(golpeado);
            Time.timeScale = 1f;
        }

        private void Armar(out SensacionGolpe sensacion, out Animator animAtacante, out Animator animGolpeado)
        {
            atacante = new GameObject("Atacante");
            animAtacante = atacante.AddComponent<Animator>();
            sensacion = atacante.AddComponent<SensacionGolpe>();
            sensacion.Configurar(ConfigGolpeAPie.PorDefecto);
            golpeado = new GameObject("Golpeado");
            golpeado.transform.position = new Vector3(0f, 0f, 1.5f);
            golpeado.AddComponent<CharacterController>();
            animGolpeado = golpeado.AddComponent<Animator>();
        }

        private static void Mover(GameObject g, Vector3 posicion)
        {
            CharacterController cc = g.GetComponent<CharacterController>();
            cc.enabled = false;
            g.transform.position = posicion;
            cc.enabled = true;
        }

        [UnityTest]
        public IEnumerator Conectar_CongelaAmbosAnimatorYLosRestauraAunConTimeScaleCero()
        {
            Armar(out SensacionGolpe s, out Animator a, out Animator g);
            Time.timeScale = 0f; // una pausa no debe dejar la animación congelada para siempre
            s.Conectar(golpeado.GetComponent<CharacterController>(), Vector3.forward, false);
            Assert.AreEqual(0f, a.speed);
            Assert.AreEqual(0f, g.speed);
            Assert.IsTrue(s.EnHitstop);
            float limite = Time.realtimeSinceStartup + 1f;
            while (s.EnHitstop && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            Assert.IsFalse(s.EnHitstop);
            Assert.AreEqual(1f, a.speed);
            Assert.AreEqual(1f, g.speed);
        }

        [UnityTest]
        public IEnumerator Desactivar_EnPleno_Hitstop_RestauraLaVelocidad()
        {
            Armar(out SensacionGolpe s, out Animator a, out Animator g);
            s.Conectar(golpeado.GetComponent<CharacterController>(), Vector3.forward, true);
            s.enabled = false;
            yield return null;
            Assert.AreEqual(1f, a.speed);
            Assert.AreEqual(1f, g.speed);
        }

        [UnityTest]
        public IEnumerator Conectar_GolpeFinal_EmpujaMasQueElNormal()
        {
            Armar(out SensacionGolpe s, out _, out _);
            ConfigGolpeAPie c = ConfigGolpeAPie.PorDefecto;
            CharacterController cc = golpeado.GetComponent<CharacterController>();
            Mover(golpeado, new Vector3(0f, 0.1f, 1.5f));
            s.Conectar(cc, Vector3.forward, false);
            yield return new WaitForSecondsRealtime(c.SegundosEmpuje + 0.4f);
            float normal = golpeado.transform.position.z - 1.5f;
            Mover(golpeado, new Vector3(0f, 0.1f, 1.5f));
            s.Conectar(cc, Vector3.forward, true);
            yield return new WaitForSecondsRealtime(c.SegundosEmpuje + 0.4f);
            float final = golpeado.transform.position.z - 1.5f;
            Assert.Greater(normal, 0.05f, "el golpe normal lo empuja");
            Assert.Greater(final, normal, "el golpe final lo empuja más");
        }
    }
}

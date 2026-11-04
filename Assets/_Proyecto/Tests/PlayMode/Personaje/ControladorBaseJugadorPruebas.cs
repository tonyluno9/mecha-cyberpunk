using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-561: el jugador a pie con el controlador base: la capa base y la de arriba cambian de estado con entrada simulada (teclas y ratón).</summary>
    public class ControladorBaseJugadorPruebas : BaseJugadorAPie
    {
        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);

        private static bool EnEstado(Animator a, int capa, string nombre)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(capa);
            AnimatorStateInfo siguiente = a.GetNextAnimatorStateInfo(capa);
            return a.IsInTransition(capa) ? siguiente.IsName(nombre) || actual.IsName(nombre) : actual.IsName(nombre);
        }

        private static IEnumerator Hasta(System.Func<bool> condicion, string que, float limite = 4f)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        [UnityTest]
        public IEnumerator Jugador_UsaElOverrideDelPapelSobreElControladorBase()
        {
            yield return Calentar();
            Animator a = Visual().Animador;
            Assert.IsInstanceOf<AnimatorOverrideController>(a.runtimeAnimatorController);
            Assert.AreEqual("Override_Jugador", a.runtimeAnimatorController.name);
            Assert.AreEqual(2, a.layerCount);
            Assert.AreEqual("Base", a.GetLayerName(0));
            Assert.AreEqual("Arriba", a.GetLayerName(1));
        }

        [UnityTest]
        public IEnumerator Caminar_Correr_YSoltar_CambianLaCapaBase_ConLaDireccionLocal()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            yield return Esperar(0.4f);
            Assert.IsTrue(EnEstado(a, 0, "Idle"), "Quieto");

            Press(teclado.wKey);
            yield return Hasta(() => EnEstado(a, 0, "Caminar") || EnEstado(a, 0, "Correr"), "Caminar al pulsar W");
            yield return Hasta(() => a.GetFloat(ReglasAnimacion.VelZ) > 0.8f, "Avanzar de frente: VelZ cerca de 1");
            Assert.AreEqual(0f, a.GetFloat(ReglasAnimacion.VelX), 0.3f);

            Press(teclado.leftShiftKey);
            yield return Hasta(() => EnEstado(a, 0, "Correr"), "Correr con Shift");

            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
            yield return Esperar(0.5f);
            Press(teclado.sKey);
            yield return Hasta(() => a.GetFloat(ReglasAnimacion.VelZ) < -0.5f, "Retroceder: VelZ negativa (velocidad real " + controlador.Velocidad + ")");
            Release(teclado.sKey);
            yield return Hasta(() => EnEstado(a, 0, "Idle"), "Idle al soltar", 5f);
        }

        [UnityTest]
        public IEnumerator Pistola_ApuntarDisparaYRecargar_CambianLaCapaDeArriba()
        {
            yield return Calentar();
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola), "El jugador saca la pistola");
            yield return Hasta(() => EnEstado(a, 1, "PistolaIdle"), "PistolaIdle con la pistola en la mano");
            Assert.AreEqual(1, a.GetInteger(ReglasVisual.TipoArma));

            Press(raton.rightButton);
            yield return Hasta(() => EnEstado(a, 1, "PistolaApuntar"), "PistolaApuntar con clic derecho");

            Press(raton.leftButton);
            yield return Hasta(() => EnEstado(a, 1, "PistolaDisparo"), "PistolaDisparo con clic izquierdo");
            Release(raton.leftButton);

            Release(raton.rightButton);
            yield return Esperar(0.6f);
            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            yield return Hasta(() => armas.Recargando, "la pistola recarga con R");
            yield return Hasta(() => EnEstado(a, 1, "PistolaRecargar") || EnEstado(a, 1, "PistolaRecargarCaminar"), "animación de recargar");
        }
    }
}

using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-691: una explosión prende al jugador; el fuego hace daño por ticks y se apaga solo a los 4 s, antes si rueda (agachado + espacio) o si sale
    /// del área en llamas, y no daña sin fin. Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-691-*.png.
    /// </summary>
    public class FuegoT691Pruebas : BaseJugadorAPie
    {
        private VidaAPie Vida => jugador.GetComponent<VidaAPie>();
        private QuemaduraJugador Quemadura => Vida.Quemadura;

        private void Explosion(float cantidad)
        {
            Vida.RecibirDanio(new InfoDanio(cantidad, TipoDanio.Explosion, jugador.transform.position + Vector3.up, Vector3.forward, null));
        }

        [UnityTest]
        public IEnumerator Explosion_PrendeAlJugador_DaniaYSeApagaSolaSinDanarSinFin()
        {
            yield return Calentar();
            Assert.IsFalse(Quemadura.Ardiendo);
            float antes = Vida.Actual;
            Explosion(10f);
            Assert.IsTrue(Quemadura.Ardiendo, "la explosión lo prende");
            Assert.AreEqual(antes - 10f, Vida.Actual, 0.01f);
            yield return Esperar(1.6f);
            Assert.Less(Vida.Actual, antes - 10f - 1f, "el fuego baja la vida poco a poco");
            CapturaS1.Foto("s1t-T-691-quemandose", jugador.transform, new Vector3(1.6f, 1.4f, 2.2f), new Vector3(0f, 1.0f, 0f), 45f, 900, 900);
            yield return Esperar(3.2f);
            Assert.IsFalse(Quemadura.Ardiendo, "se apagó sola");
            Assert.AreEqual(ReglaQuemadura.Apagado.Tiempo, Quemadura.UltimoApagado);
            float despues = Vida.Actual;
            Assert.LessOrEqual(antes - despues, 10f + ReglaQuemadura.DanioTotalMaximo + 0.5f, "el daño del fuego tiene tope");
            yield return Esperar(3f);
            Assert.AreEqual(despues, Vida.Actual, 0.01f, "apagado no sigue bajando (solo puede subir por regeneración)");
        }

        [UnityTest]
        public IEnumerator Rodando_ApagaElFuego_ConEntradaSimulada()
        {
            yield return Calentar();
            Explosion(5f);
            Assert.IsTrue(Quemadura.Ardiendo);
            Press(teclado.leftCtrlKey);
            yield return null;
            Release(teclado.leftCtrlKey);
            yield return Esperar(0.3f);
            Press(teclado.wKey);
            yield return Esperar(0.3f);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            yield return Esperar(0.2f);
            Release(teclado.wKey);
            Assert.IsFalse(Quemadura.Ardiendo, "al rodar se apaga");
            Assert.AreEqual(ReglaQuemadura.Apagado.Rodar, Quemadura.UltimoApagado);
            Assert.Less(Quemadura.DanioTotal, ReglaQuemadura.DanioTotalMaximo, "se apagó antes de hacer todo el daño");
        }

        [UnityTest]
        public IEnumerator SalirDelAreaEnLlamas_ApagaElFuegoAntes()
        {
            yield return Calentar();
            Explosion(5f);
            Assert.IsTrue(Quemadura.Ardiendo);
            jugadorAPie.Colocar(new Pose(jugador.transform.position + Vector3.right * (ReglaQuemadura.DistanciaApagado + 2f), jugador.transform.rotation));
            yield return Esperar(0.3f);
            Assert.IsFalse(Quemadura.Ardiendo, "al alejarse del área en llamas se apaga");
            Assert.AreEqual(ReglaQuemadura.Apagado.SalioDelArea, Quemadura.UltimoApagado);
        }
    }
}

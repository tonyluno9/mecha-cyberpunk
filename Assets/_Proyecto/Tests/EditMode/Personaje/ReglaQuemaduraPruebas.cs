using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-691: el fuego del jugador se apaga solo (4 s), al rodar o al salir del área, y nunca daña sin fin.</summary>
    public class ReglaQuemaduraPruebas
    {
        private static float Correr(ReglaQuemadura regla, float segundos, bool rodando, Vector3 posicion)
        {
            float total = 0f;
            const float Paso = 0.05f;
            for (float t = 0f; t < segundos; t += Paso)
            {
                total += regla.Avanzar(Paso, rodando, posicion);
            }
            return total;
        }

        [Test]
        public void SinPrender_NoHaceDanio()
        {
            ReglaQuemadura regla = new ReglaQuemadura();
            Assert.IsFalse(regla.Ardiendo);
            Assert.AreEqual(0f, Correr(regla, 10f, false, Vector3.zero), 0.0001f);
        }

        [Test]
        public void Prendida_DaniaPorTicks_YSeApagaSolaALos4Segundos()
        {
            ReglaQuemadura regla = new ReglaQuemadura();
            regla.Prender(Vector3.zero);
            Assert.IsTrue(regla.Ardiendo);
            float danio = Correr(regla, 2f, false, Vector3.zero);
            Assert.AreEqual(ReglaQuemadura.DanioPorSegundo * 2f, danio, ReglaQuemadura.DanioPorSegundo * ReglaQuemadura.SegundosPorTick + 0.01f);
            danio += Correr(regla, 3f, false, Vector3.zero);
            Assert.IsFalse(regla.Ardiendo, "pasados 4 s ya no arde");
            Assert.AreEqual(ReglaQuemadura.Apagado.Tiempo, regla.UltimoApagado);
            Assert.LessOrEqual(danio, ReglaQuemadura.DanioTotalMaximo + 0.01f, "el daño total tiene tope");
            Assert.AreEqual(0f, Correr(regla, 10f, false, Vector3.zero), 0.0001f, "apagada no sigue dañando");
        }

        [Test]
        public void Rodar_ApagaAntes()
        {
            ReglaQuemadura regla = new ReglaQuemadura();
            regla.Prender(Vector3.zero);
            Correr(regla, 1f, false, Vector3.zero);
            regla.Avanzar(0.05f, true, Vector3.zero);
            Assert.IsFalse(regla.Ardiendo);
            Assert.AreEqual(ReglaQuemadura.Apagado.Rodar, regla.UltimoApagado);
            Assert.Less(regla.Restante, 0.001f);
        }

        [Test]
        public void SalirDelArea_ApagaAntes_PeroDentroSigue()
        {
            ReglaQuemadura regla = new ReglaQuemadura();
            regla.Prender(Vector3.zero);
            Correr(regla, 1f, false, new Vector3(ReglaQuemadura.DistanciaApagado * 0.5f, 0f, 0f));
            Assert.IsTrue(regla.Ardiendo, "dentro del área sigue ardiendo");
            regla.Avanzar(0.05f, false, new Vector3(ReglaQuemadura.DistanciaApagado + 1f, 0f, 0f));
            Assert.IsFalse(regla.Ardiendo);
            Assert.AreEqual(ReglaQuemadura.Apagado.SalioDelArea, regla.UltimoApagado);
        }

        [Test]
        public void VolverAPrender_RenuevaElTiempo_SinSumarDanioSinFin()
        {
            ReglaQuemadura regla = new ReglaQuemadura();
            regla.Prender(Vector3.zero);
            Correr(regla, 3f, false, Vector3.zero);
            regla.Prender(Vector3.zero);
            Assert.AreEqual(ReglaQuemadura.SegundosDuracion, regla.Restante, 0.0001f);
            Correr(regla, 10f, false, Vector3.zero);
            Assert.IsFalse(regla.Ardiendo, "aunque se renueve, termina");
        }
    }
}

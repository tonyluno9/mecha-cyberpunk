using System;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-444: la vida del carro baja, cambia de estado por fracciones y el choque hace daño solo por encima del umbral.</summary>
    public class CalculoDanioCarroPruebas
    {
        private const float Tolerancia = 0.0001f;

        private static CalculoDanioCarro Crear() => new CalculoDanioCarro(100f, 0.5f, 0.25f);

        [Test]
        public void Estado_SegunLaVidaQueQueda()
        {
            CalculoDanioCarro carro = Crear();
            Assert.AreEqual(EstadoCarro.Sano, carro.Estado);
            carro.Aplicar(49f);
            Assert.AreEqual(EstadoCarro.Sano, carro.Estado, "Con 51 % todavía sano");
            carro.Aplicar(1f);
            Assert.AreEqual(EstadoCarro.Humo, carro.Estado, "Con 50 % sale humo");
            carro.Aplicar(25f);
            Assert.AreEqual(EstadoCarro.Fuego, carro.Estado, "Con 25 % hay fuego");
            carro.Aplicar(25f);
            Assert.AreEqual(EstadoCarro.Destruido, carro.Estado);
            Assert.IsTrue(carro.Destruido);
        }

        [Test]
        public void Aplicar_NoBajaDeCero_YDevuelveLoRestado()
        {
            CalculoDanioCarro carro = Crear();
            Assert.AreEqual(60f, carro.Aplicar(60f), Tolerancia);
            Assert.AreEqual(40f, carro.Aplicar(500f), Tolerancia, "Solo resta lo que queda");
            Assert.AreEqual(0f, carro.Actual, Tolerancia);
            Assert.AreEqual(0f, carro.Aplicar(10f), Tolerancia, "Destruido no recibe más");
        }

        [Test]
        public void Aplicar_CantidadNegativaOCero_NoHaceNada()
        {
            CalculoDanioCarro carro = Crear();
            Assert.AreEqual(0f, carro.Aplicar(-5f), Tolerancia);
            Assert.AreEqual(0f, carro.Aplicar(0f), Tolerancia);
            Assert.AreEqual(100f, carro.Actual, Tolerancia);
        }

        [Test]
        public void DanioPorChoque_SoloPorEncimaDelUmbral()
        {
            Assert.AreEqual(0f, CalculoDanioCarro.DanioPorChoque(5f, 6f, 7f), Tolerancia);
            Assert.AreEqual(0f, CalculoDanioCarro.DanioPorChoque(6f, 6f, 7f), Tolerancia);
            Assert.AreEqual(14f, CalculoDanioCarro.DanioPorChoque(8f, 6f, 7f), Tolerancia);
            Assert.AreEqual(0f, CalculoDanioCarro.DanioPorChoque(20f, 6f, -3f), Tolerancia, "Un factor negativo no cura");
        }

        [Test]
        public void Constructor_ConVidaInvalida_Lanza()
        {
            Assert.Throws<ArgumentException>(() => new CalculoDanioCarro(0f, 0.5f, 0.25f));
        }

        [Test]
        public void Constructor_ConFuegoMayorQueHumo_LoRecortaAlHumo()
        {
            CalculoDanioCarro carro = new CalculoDanioCarro(100f, 0.3f, 0.9f);
            carro.Aplicar(70f);
            Assert.AreEqual(EstadoCarro.Fuego, carro.Estado, "Con 30 % ya es fuego: el fuego nunca está antes que el humo");
        }
    }
}

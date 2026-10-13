using System;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    public class CalculoTorretaPruebas
    {
        private const float Periodo = 0.35f;
        private const float Mitad = 0.5f;
        private const float PasoChico = 0.01f;
        private const int PasosTresSegundos = 300;
        private const int CambiosEnTresSegundos = 8;

        [Test]
        public void ColorActual_AlEmpezarSinFase_EsRojo()
        {
            Assert.AreEqual(ColorTorreta.Rojo, new CalculoTorreta(Periodo, 0f).ColorActual);
        }

        [Test]
        public void Avanzar_CadaPeriodo_AlternaRojoYAzul()
        {
            CalculoTorreta torreta = new CalculoTorreta(Periodo, 0f);
            torreta.Avanzar(Periodo * Mitad);
            Assert.AreEqual(ColorTorreta.Rojo, torreta.ColorActual);
            torreta.Avanzar(Periodo);
            Assert.AreEqual(ColorTorreta.Azul, torreta.ColorActual);
            torreta.Avanzar(Periodo);
            Assert.AreEqual(ColorTorreta.Rojo, torreta.ColorActual);
            torreta.Avanzar(Periodo);
            Assert.AreEqual(ColorTorreta.Azul, torreta.ColorActual);
        }

        [Test]
        public void ColorActual_DosFasesIniciales_DanColoresDistintosEnElMismoInstante()
        {
            CalculoTorreta a = new CalculoTorreta(Periodo, 0f);
            CalculoTorreta b = new CalculoTorreta(Periodo, Periodo);
            Assert.AreNotEqual(a.ColorActual, b.ColorActual);
            a.Avanzar(0.1f);
            b.Avanzar(0.1f);
            Assert.AreNotEqual(a.ColorActual, b.ColorActual);
            a.Avanzar(0.5f);
            b.Avanzar(0.5f);
            Assert.AreNotEqual(a.ColorActual, b.ColorActual);
        }

        [Test]
        public void ColorActual_FaseNegativa_AlternaSinFallar()
        {
            CalculoTorreta torreta = new CalculoTorreta(Periodo, -Periodo * Mitad);
            Assert.AreEqual(ColorTorreta.Azul, torreta.ColorActual);
            torreta.Avanzar(Periodo);
            Assert.AreEqual(ColorTorreta.Rojo, torreta.ColorActual);
        }

        [Test]
        public void Avanzar_PasosChicos_CuentaLosCambiosDelPeriodo()
        {
            CalculoTorreta torreta = new CalculoTorreta(Periodo, 0f);
            ColorTorreta anterior = torreta.ColorActual;
            int cambios = 0;
            for (int i = 0; i < PasosTresSegundos; i++)
            {
                torreta.Avanzar(PasoChico);
                if (torreta.ColorActual != anterior)
                {
                    cambios++;
                    anterior = torreta.ColorActual;
                }
            }
            Assert.AreEqual(CambiosEnTresSegundos, cambios, "3 s con periodo 0.35 s son 8 cambios");
        }

        [Test]
        public void Avanzar_DtNegativo_NoRetrocede()
        {
            CalculoTorreta torreta = new CalculoTorreta(Periodo, 0f);
            torreta.Avanzar(Periodo * 1.5f);
            ColorTorreta antes = torreta.ColorActual;
            torreta.Avanzar(-10f);
            Assert.AreEqual(antes, torreta.ColorActual);
        }

        [Test]
        public void Constructor_PeriodoCeroONegativo_Lanza()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CalculoTorreta(0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CalculoTorreta(-1f, 0f));
        }
    }
}

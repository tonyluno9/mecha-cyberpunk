using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Economia
{
    public class ModeloRentaPruebas
    {
        private const int Monto = 500;
        private const int DiasEntreCobros = 7;
        private const int PrimerDia = 8;

        private ModeloRenta renta;
        private ModeloCartera cartera;
        private MotivoDinero ultimoMotivo;

        private void Preparar(int dinero)
        {
            renta = new ModeloRenta(Monto, DiasEntreCobros, PrimerDia);
            cartera = new ModeloCartera(dinero);
            ultimoMotivo = MotivoDinero.Otro;
            cartera.Cambio += (d, m) => ultimoMotivo = m;
        }

        [Test]
        public void AlEmpezarDia_Dia7_NoCobra()
        {
            Preparar(500);
            renta.AlEmpezarDia(7, cartera);

            Assert.AreEqual(500, cartera.Dinero);
            Assert.AreEqual(0, renta.Adeudo);
            Assert.AreEqual(PrimerDia, renta.DiaProximoCobro);
        }

        [Test]
        public void AlEmpezarDia_Dia8ConDinero_CobraConMotivoRentaYElProximoEsElDia15()
        {
            Preparar(500);
            renta.AlEmpezarDia(8, cartera);

            Assert.AreEqual(0, cartera.Dinero);
            Assert.AreEqual(MotivoDinero.Renta, ultimoMotivo);
            Assert.AreEqual(0, renta.Adeudo);
            Assert.IsFalse(renta.Bloqueado);
            Assert.AreEqual(15, renta.DiaProximoCobro);
        }

        [Test]
        public void AlEmpezarDia_Dia8SinDinero_DejaAdeudoYBloqueaSinTocarElDinero()
        {
            Preparar(300);
            renta.AlEmpezarDia(8, cartera);

            Assert.AreEqual(300, cartera.Dinero);
            Assert.AreEqual(500, renta.Adeudo);
            Assert.IsTrue(renta.Bloqueado);
            Assert.AreEqual(15, renta.DiaProximoCobro);
        }

        [Test]
        public void AlEmpezarDia_SaltoDel1Al16SinDinero_AcumulaDosRentasYElProximoEsElDia22()
        {
            Preparar(0);
            renta.AlEmpezarDia(16, cartera);

            Assert.AreEqual(1000, renta.Adeudo);
            Assert.AreEqual(22, renta.DiaProximoCobro);
        }

        [Test]
        public void AlEmpezarDia_ConAdeudoYDinero_LaRentaNuevaSeSumaAlAdeudo()
        {
            Preparar(0);
            renta.AlEmpezarDia(8, cartera);
            cartera.Abonar(5000, MotivoDinero.Otro);

            renta.AlEmpezarDia(15, cartera);

            Assert.AreEqual(1000, renta.Adeudo);
            Assert.AreEqual(5000, cartera.Dinero, "No cobra sola mientras haya adeudo");
        }

        [Test]
        public void AlEmpezarDia_ConCarteraNull_TodoVaAAdeudo()
        {
            Preparar(0);
            renta.AlEmpezarDia(8, null);

            Assert.AreEqual(500, renta.Adeudo);
        }

        [Test]
        public void AlEmpezarDia_Cobro_AvisaCambioUnaVez_YSinCobroNoAvisa()
        {
            Preparar(500);
            int avisos = 0;
            renta.Cambio += () => avisos++;

            renta.AlEmpezarDia(7, cartera);
            Assert.AreEqual(0, avisos);

            renta.AlEmpezarDia(8, cartera);
            Assert.AreEqual(1, avisos);
        }

        [Test]
        public void PagarAdeudo_ConPocoDinero_DevuelveFalseYNoCambiaNada()
        {
            Preparar(0);
            renta.AlEmpezarDia(8, cartera);
            cartera.Abonar(400, MotivoDinero.Otro);

            Assert.IsFalse(renta.PagarAdeudo(cartera));
            Assert.AreEqual(400, cartera.Dinero);
            Assert.AreEqual(500, renta.Adeudo);
            Assert.IsTrue(renta.Bloqueado);
        }

        [Test]
        public void PagarAdeudo_ConSuficiente_DesbloqueaYElProximoCobroNoCambia()
        {
            Preparar(0);
            renta.AlEmpezarDia(8, cartera);
            cartera.Abonar(600, MotivoDinero.Otro);

            Assert.IsTrue(renta.PagarAdeudo(cartera));

            Assert.AreEqual(100, cartera.Dinero);
            Assert.AreEqual(MotivoDinero.Renta, ultimoMotivo);
            Assert.AreEqual(0, renta.Adeudo);
            Assert.IsFalse(renta.Bloqueado);
            Assert.AreEqual(15, renta.DiaProximoCobro);
        }

        [Test]
        public void PagarAdeudo_SinAdeudo_DevuelveFalse()
        {
            Preparar(1000);
            Assert.IsFalse(renta.PagarAdeudo(cartera));
            Assert.AreEqual(1000, cartera.Dinero);
        }

        [Test]
        public void Restablecer_FijaElEstadoSinCobrarNada()
        {
            Preparar(500);
            renta.Restablecer(22, 1000);

            Assert.AreEqual(22, renta.DiaProximoCobro);
            Assert.AreEqual(1000, renta.Adeudo);
            Assert.IsTrue(renta.Bloqueado);
            Assert.AreEqual(500, cartera.Dinero);
        }
    }
}

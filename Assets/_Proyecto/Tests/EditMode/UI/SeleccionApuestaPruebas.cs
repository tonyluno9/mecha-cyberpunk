using MechaCyberpunk.UI.Peleas;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    public class SeleccionApuestaPruebas
    {
        private static readonly int[] Montos = { 100, 250, 500 };

        [Test]
        public void Nueva_EmpiezaEnSinApuestaConMontoCero()
        {
            SeleccionApuesta seleccion = new SeleccionApuesta(Montos, monto => true);

            Assert.AreEqual(4, seleccion.Cantidad, "SIN APUESTA y tres montos");
            Assert.AreEqual(0, seleccion.Indice);
            Assert.AreEqual(0, seleccion.Monto);
        }

        [Test]
        public void Mover_ConDineroDeSobra_RecorreLosMontosYNoDaLaVuelta()
        {
            SeleccionApuesta seleccion = new SeleccionApuesta(Montos, monto => true);

            Assert.IsFalse(seleccion.Mover(-1), "A la izquierda de SIN APUESTA no hay nada");
            Assert.IsTrue(seleccion.Mover(1));
            Assert.AreEqual(100, seleccion.Monto);
            Assert.IsTrue(seleccion.Mover(1));
            Assert.AreEqual(250, seleccion.Monto);
            Assert.IsTrue(seleccion.Mover(1));
            Assert.AreEqual(500, seleccion.Monto);
            Assert.IsFalse(seleccion.Mover(1), "Al final no da la vuelta");
            Assert.AreEqual(500, seleccion.Monto);
            Assert.IsTrue(seleccion.Mover(-1));
            Assert.AreEqual(250, seleccion.Monto);
        }

        [Test]
        public void Mover_ConMontosQueNoSePagan_LosSalta()
        {
            SeleccionApuesta seleccion = new SeleccionApuesta(Montos, monto => monto != 250);

            Assert.IsTrue(seleccion.Disponible(0), "SIN APUESTA siempre está disponible");
            Assert.IsTrue(seleccion.Disponible(1));
            Assert.IsFalse(seleccion.Disponible(2), "$250 apagado");
            Assert.IsTrue(seleccion.Disponible(3));
            seleccion.Mover(1);
            Assert.AreEqual(100, seleccion.Monto);
            seleccion.Mover(1);
            Assert.AreEqual(500, seleccion.Monto, "Salta el $250");
            seleccion.Mover(-1);
            Assert.AreEqual(100, seleccion.Monto, "Y también de regreso");
        }

        [Test]
        public void Mover_ConPocoDinero_NoSaleDeSinApuesta()
        {
            SeleccionApuesta seleccion = new SeleccionApuesta(Montos, monto => monto <= 80);

            Assert.IsFalse(seleccion.Mover(1));
            Assert.IsFalse(seleccion.Mover(-1));
            Assert.AreEqual(0, seleccion.Indice);
            Assert.AreEqual(0, seleccion.Monto);
        }

        [Test]
        public void Nueva_SinMontosOSinFuncion_SoloOfreceSinApuesta()
        {
            SeleccionApuesta sinMontos = new SeleccionApuesta(null, monto => true);
            SeleccionApuesta sinFuncion = new SeleccionApuesta(Montos, null);

            Assert.AreEqual(1, sinMontos.Cantidad);
            Assert.IsFalse(sinMontos.Mover(1));
            Assert.AreEqual(4, sinFuncion.Cantidad);
            Assert.IsFalse(sinFuncion.Mover(1), "Sin saber si se pagan, ninguno se ofrece");
        }

        [Test]
        public void Mover_ConPasoCero_NoHaceNada()
        {
            SeleccionApuesta seleccion = new SeleccionApuesta(Montos, monto => true);

            Assert.IsFalse(seleccion.Mover(0));
            Assert.AreEqual(0, seleccion.Indice);
        }
    }
}

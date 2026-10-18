using System.Collections.Generic;
using MechaCyberpunk.UI.Peleas;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    public class SeleccionCarteleraPruebas
    {
        private static FilaCartelera Fila(bool elegible, bool jefe = false) => new FilaCartelera("fila", 100, elegible, jefe);

        [Test]
        public void Nueva_EmpiezaEnLaPrimeraElegible()
        {
            var seleccion = new SeleccionCartelera(new List<FilaCartelera> { Fila(false), Fila(false), Fila(true), Fila(true) });

            Assert.AreEqual(2, seleccion.Indice, "Se salta lo que no se puede elegir");
            Assert.AreEqual(4, seleccion.Cantidad);
        }

        [Test]
        public void Nueva_ConLaPrimeraElegible_EmpiezaEnCero()
        {
            var seleccion = new SeleccionCartelera(new List<FilaCartelera> { Fila(true), Fila(true), Fila(false) });

            Assert.AreEqual(0, seleccion.Indice);
        }

        [Test]
        public void Mover_ArribaEnLaPrimera_NoSeMueveNiDaLaVuelta()
        {
            var seleccion = new SeleccionCartelera(new List<FilaCartelera> { Fila(true), Fila(true), Fila(false) });

            Assert.IsFalse(seleccion.Mover(-1));
            Assert.AreEqual(0, seleccion.Indice);
        }

        [Test]
        public void Mover_AbajoEnLaUltimaElegible_NoSeMueveNiDaLaVuelta()
        {
            var seleccion = new SeleccionCartelera(new List<FilaCartelera> { Fila(true), Fila(true) });

            Assert.IsTrue(seleccion.Mover(1));
            Assert.AreEqual(1, seleccion.Indice);
            Assert.IsFalse(seleccion.Mover(1), "Abajo de la última no hay nada");
            Assert.AreEqual(1, seleccion.Indice);
        }

        [Test]
        public void Mover_ConUnaBloqueadaAlFinal_NoLaElige()
        {
            var seleccion = new SeleccionCartelera(new List<FilaCartelera> { Fila(true), Fila(true), Fila(false) });

            seleccion.Mover(1);
            Assert.IsFalse(seleccion.Mover(1), "La bloqueada no se elige");
            Assert.AreEqual(1, seleccion.Indice);
        }

        [Test]
        public void Mover_ConUnaBloqueadaEnMedio_LaSalta()
        {
            var seleccion = new SeleccionCartelera(new List<FilaCartelera> { Fila(true), Fila(false), Fila(true) });

            Assert.IsTrue(seleccion.Mover(1));
            Assert.AreEqual(2, seleccion.Indice, "Salta la bloqueada");
            Assert.IsTrue(seleccion.Mover(-1));
            Assert.AreEqual(0, seleccion.Indice, "Y vuelve saltándola");
        }

        [Test]
        public void Mover_PasoCero_NoHaceNada()
        {
            var seleccion = new SeleccionCartelera(new List<FilaCartelera> { Fila(true), Fila(true) });

            Assert.IsFalse(seleccion.Mover(0));
            Assert.AreEqual(0, seleccion.Indice);
        }

        [Test]
        public void Nueva_SinNingunaElegible_NoEligeNadaYNoSeMueve()
        {
            var seleccion = new SeleccionCartelera(new List<FilaCartelera> { Fila(false), Fila(false) });

            Assert.AreEqual(-1, seleccion.Indice);
            Assert.IsFalse(seleccion.Mover(1));
            Assert.IsFalse(seleccion.Mover(-1));
            Assert.AreEqual(-1, seleccion.Indice);
        }

        [Test]
        public void Elegible_DiceSiLaFilaSePuedeElegir()
        {
            var seleccion = new SeleccionCartelera(new List<FilaCartelera> { Fila(true), Fila(false) });

            Assert.IsTrue(seleccion.Elegible(0));
            Assert.IsFalse(seleccion.Elegible(1));
            Assert.IsFalse(seleccion.Elegible(2), "Fuera de la lista no es elegible");
            Assert.IsFalse(seleccion.Elegible(-1));
        }
    }
}

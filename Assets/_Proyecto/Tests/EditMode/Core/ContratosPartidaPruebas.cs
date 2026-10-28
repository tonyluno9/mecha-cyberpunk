using System;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    public class ContratosPartidaPruebas
    {
        private class GuardableFalso : IGuardable
        {
            public string ClaveGuardado => "falso";
            public string Datos = "";
            public string Guardar() => Datos;
            public void Cargar(string datos) { Datos = datos; }
        }

        private class InteractuableFalso : IInteractuable
        {
            public Vector3 Posicion => Vector3.zero;
            public string Texto => "Dormir";
            public bool PuedeUsar => true;
            public int Usos;
            public void Usar() { Usos++; }
        }

        private class RentaFalsa : IRenta
        {
            public int Monto => 500;
            public int DiaProximoCobro => 7;
            public int Adeudo { get; set; }
            public bool TallerBloqueado => Adeudo > 0;
            public event Action Cambio;
            public bool PagarAdeudo()
            {
                if (Adeudo <= 0) return false;
                Adeudo = 0;
                Cambio?.Invoke();
                return true;
            }
        }

        [SetUp]
        public void SetUp() => Registros.LimpiarTodos();

        [TearDown]
        public void TearDown() => Registros.LimpiarTodos();

        [Test]
        public void GuardableEntraYSaleDelRegistro()
        {
            var g = new GuardableFalso();
            Registro<IGuardable>.Agregar(g);
            Assert.AreSame(g, Registro<IGuardable>.Actual);
            g.Cargar("{}");
            Assert.AreEqual("{}", g.Guardar());
            Registro<IGuardable>.Quitar(g);
            Assert.AreEqual(0, Registro<IGuardable>.Cantidad);
        }

        [Test]
        public void InteractuableEntraYSaleDelRegistro()
        {
            var i = new InteractuableFalso();
            Registro<IInteractuable>.Agregar(i);
            Assert.AreSame(i, Registro<IInteractuable>.Actual);
            i.Usar();
            Assert.AreEqual(1, i.Usos);
            Registro<IInteractuable>.Quitar(i);
            Assert.AreEqual(0, Registro<IInteractuable>.Cantidad);
        }

        [Test]
        public void RentaEntraYSaleDelRegistro()
        {
            var r = new RentaFalsa { Adeudo = 500 };
            Registro<IRenta>.Agregar(r);
            Assert.AreSame(r, Registro<IRenta>.Actual);
            Assert.IsTrue(r.TallerBloqueado);
            Assert.IsTrue(r.PagarAdeudo());
            Assert.IsFalse(r.TallerBloqueado);
            Registro<IRenta>.Quitar(r);
            Assert.AreEqual(0, Registro<IRenta>.Cantidad);
        }

        [Test]
        public void LimpiarTodosVaciaLosTresRegistros()
        {
            Registro<IGuardable>.Agregar(new GuardableFalso());
            Registro<IInteractuable>.Agregar(new InteractuableFalso());
            Registro<IRenta>.Agregar(new RentaFalsa());
            Registros.LimpiarTodos();
            Assert.AreEqual(0, Registro<IGuardable>.Cantidad);
            Assert.AreEqual(0, Registro<IInteractuable>.Cantidad);
            Assert.AreEqual(0, Registro<IRenta>.Cantidad);
        }

        [Test]
        public void MotivoDineroCargaEsElUltimoYPremioSigueEnCero()
        {
            var valores = (MotivoDinero[])Enum.GetValues(typeof(MotivoDinero));
            Assert.AreEqual(MotivoDinero.Mision, valores[valores.Length - 1]);
            Assert.AreEqual(MotivoDinero.Carga, valores[valores.Length - 2]);
            Assert.AreEqual(0, (int)MotivoDinero.Premio);
            Assert.AreEqual(MotivoDinero.Otro, valores[valores.Length - 3]);
        }
    }
}

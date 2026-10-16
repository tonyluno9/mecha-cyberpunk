using System;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    public class ContratosHistoriaPruebas
    {
        private class HistoriaFalsa : IHistoria
        {
            public EtapaPrologo Etapa { get; private set; }
            public event Action<EtapaPrologo> EtapaCambio;

            public void Poner(EtapaPrologo etapa)
            {
                if (etapa == Etapa) return;
                Etapa = etapa;
                EtapaCambio?.Invoke(etapa);
            }
        }

        private class PuntoHistoriaFalso : IPuntoHistoria
        {
            public string Id => "Prologo_Carro";
            public Pose Pose => Pose.identity;
            public bool Activo;
            public void Activar(bool activo) => Activo = activo;
        }

        [SetUp]
        public void SetUp() => Registros.LimpiarTodos();

        [TearDown]
        public void TearDown() => Registros.LimpiarTodos();

        [Test]
        public void Historia_AgregarYQuitar_EntraYSaleYAvisaCambio()
        {
            var historia = new HistoriaFalsa();
            EtapaPrologo recibida = EtapaPrologo.Ninguna;
            int avisos = 0;
            historia.EtapaCambio += e => { recibida = e; avisos++; };
            Registro<IHistoria>.Agregar(historia);
            Assert.AreSame(historia, Registro<IHistoria>.Actual);
            historia.Poner(EtapaPrologo.Emboscada);
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(EtapaPrologo.Emboscada, recibida);
            Registro<IHistoria>.Quitar(historia);
            Assert.AreEqual(0, Registro<IHistoria>.Cantidad);
        }

        [Test]
        public void PuntoHistoria_AgregarYQuitar_EntraYSale()
        {
            var punto = new PuntoHistoriaFalso();
            Registro<IPuntoHistoria>.Agregar(punto);
            Assert.AreSame(punto, Registro<IPuntoHistoria>.Actual);
            Registro<IPuntoHistoria>.Actual.Activar(true);
            Assert.IsTrue(punto.Activo);
            Registro<IPuntoHistoria>.Quitar(punto);
            Assert.AreEqual(0, Registro<IPuntoHistoria>.Cantidad);
        }

        [Test]
        public void LimpiarTodos_DejaVaciosHistoriaYPuntos()
        {
            Registro<IHistoria>.Agregar(new HistoriaFalsa());
            Registro<IPuntoHistoria>.Agregar(new PuntoHistoriaFalso());
            Registros.LimpiarTodos();
            Assert.AreEqual(0, Registro<IHistoria>.Cantidad);
            Assert.AreEqual(0, Registro<IPuntoHistoria>.Cantidad);
        }

        [Test]
        public void EtapaPrologo_NingunaVale0YLibreEsElUltimo()
        {
            Assert.AreEqual(0, (int)EtapaPrologo.Ninguna);
            var valores = (EtapaPrologo[])Enum.GetValues(typeof(EtapaPrologo));
            Assert.AreEqual(EtapaPrologo.Libre, valores[valores.Length - 1]);
            Assert.AreEqual(12, valores.Length);
        }

        [Test]
        public void Apariencia_ComparaPorValor()
        {
            var a = new AparienciaJugador(1, 2, 3);
            var b = new AparienciaJugador(1, 2, 3);
            var c = new AparienciaJugador(1, 2, 4);
            Assert.IsTrue(a == b);
            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.IsTrue(a != c);
            Assert.IsFalse(a == c);
        }

        [Test]
        public void Apariencia_PredeterminadaEsCeroCeroCero()
        {
            var p = AparienciaJugador.Predeterminada;
            Assert.AreEqual(0, p.Cara);
            Assert.AreEqual(0, p.Ropa);
            Assert.AreEqual(0, p.Implante);
            Assert.IsTrue(p == new AparienciaJugador(0, 0, 0));
        }
    }
}

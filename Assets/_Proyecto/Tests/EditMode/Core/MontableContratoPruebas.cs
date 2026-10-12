using System;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    public class MontableContratoPruebas
    {
        private class MontableFalso : IMontable
        {
            public ModoJugador Modo => ModoJugador.Carro;
            public string Nombre => "Carro falso";
            public Vector3 PuntoSubida => Vector3.zero;
            public bool Ocupado { get; private set; }
            public bool PuedeSubir { get; set; } = true;
            public bool PuedeBajar { get; set; } = true;
            public Transform Asiento => null;
            public Camera Camara => null;
            public event Action<bool> OcupadoCambio;

            public bool IntentarSubir(IJugador jugador)
            {
                if (!PuedeSubir) return false;
                Ocupado = true;
                OcupadoCambio?.Invoke(true);
                return true;
            }

            public bool IntentarBajar(out Pose salida)
            {
                salida = Pose.identity;
                if (!Ocupado || !PuedeBajar) return false;
                Ocupado = false;
                OcupadoCambio?.Invoke(false);
                salida = new Pose(new Vector3(2f, 0f, 0f), Quaternion.identity);
                return true;
            }
        }

        [SetUp]
        public void SetUp() => Registros.LimpiarTodos();

        [TearDown]
        public void TearDown() => Registros.LimpiarTodos();

        [Test]
        public void Registro_AgregarYQuitar_EntraYSale()
        {
            IMontable m = new MontableFalso();
            Registro<IMontable>.Agregar(m);
            Assert.AreEqual(1, Registro<IMontable>.Cantidad);
            Assert.AreSame(m, Registro<IMontable>.Actual);
            Registro<IMontable>.Quitar(m);
            Assert.AreEqual(0, Registro<IMontable>.Cantidad);
        }

        [Test]
        public void IntentarSubir_PuedeSubirFalse_DevuelveFalseSinAvisar()
        {
            var m = new MontableFalso { PuedeSubir = false };
            int avisos = 0;
            m.OcupadoCambio += _ => avisos++;
            Assert.IsFalse(m.IntentarSubir(null));
            Assert.IsFalse(m.Ocupado);
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void IntentarBajar_SinEstarOcupado_DevuelveFalse()
        {
            var m = new MontableFalso();
            Assert.IsFalse(m.IntentarBajar(out _));
        }

        [Test]
        public void SubirYBajar_Normal_AvisaTrueYFalse()
        {
            var m = new MontableFalso();
            var avisos = new System.Collections.Generic.List<bool>();
            m.OcupadoCambio += avisos.Add;
            Assert.IsTrue(m.IntentarSubir(null));
            Assert.IsTrue(m.Ocupado);
            Assert.IsTrue(m.IntentarBajar(out Pose salida));
            Assert.IsFalse(m.Ocupado);
            Assert.AreEqual(new Vector3(2f, 0f, 0f), salida.position);
            CollectionAssert.AreEqual(new[] { true, false }, avisos);
        }

        [Test]
        public void TipoDanio_Golpe_SigueEnCeroYExisteDisparo()
        {
            Assert.AreEqual(0, (int)TipoDanio.Golpe);
            Assert.IsTrue(Enum.IsDefined(typeof(TipoDanio), "Disparo"));
        }
    }
}

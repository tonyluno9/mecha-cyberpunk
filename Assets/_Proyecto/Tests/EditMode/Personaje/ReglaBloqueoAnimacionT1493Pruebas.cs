using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1493: qué estados del Animator bloquean el movimiento del jugador.</summary>
    public class ReglaBloqueoAnimacionT1493Pruebas
    {
        [TestCase("MotoBajar")]
        [TestCase("MotoSubir")]
        [TestCase("CarroEntrar")]
        [TestCase("CarroSalir")]
        [TestCase("AbrirPuerta")]
        [TestCase("Boton")]
        [TestCase("Recoger")]
        [TestCase("LevantarseBocaArriba")]
        [TestCase("LevantarseBocaAbajo")]
        public void EstadoBloquea_AnimacionLarga_Bloquea(string estado)
        {
            Assert.IsTrue(ReglaBloqueoAnimacion.EstadoBloquea(estado));
            Assert.IsTrue(ReglaBloqueoAnimacion.HashBloquea(Animator.StringToHash(estado)));
        }

        [TestCase("Idle")]
        [TestCase("Caminar")]
        [TestCase("Telefono")]
        [TestCase("VerTelefono")]
        [TestCase("MotoManejar")]
        [TestCase("CarroManejar")]
        [TestCase("Burla")]
        [TestCase("Presumir")]
        [TestCase("PistolaDisparo")]
        [TestCase("PistolaRecargar")]
        [TestCase("PistolaSacar")]
        [TestCase("PunosGolpeIzq")]
        [TestCase("Rodar")]
        [TestCase("")]
        public void EstadoBloquea_AnimacionCorta_NoBloquea(string estado)
        {
            Assert.IsFalse(ReglaBloqueoAnimacion.EstadoBloquea(estado));
            Assert.IsFalse(ReglaBloqueoAnimacion.HashBloquea(Animator.StringToHash(estado)));
        }

        [Test]
        public void EstadoBloquea_Nulo_NoBloquea()
        {
            Assert.IsFalse(ReglaBloqueoAnimacion.EstadoBloquea(null));
        }

        [Test]
        public void DebeBloquear_BajoElTope_Bloquea()
        {
            Assert.IsTrue(ReglaBloqueoAnimacion.DebeBloquear(true, ReglaBloqueoAnimacion.SegundosMaximos - 0.1f));
        }

        [Test]
        public void DebeBloquear_PasadoElTope_Libera()
        {
            Assert.IsFalse(ReglaBloqueoAnimacion.DebeBloquear(true, ReglaBloqueoAnimacion.SegundosMaximos));
        }

        [Test]
        public void DebeBloquear_EstadoNoBloqueante_NuncaBloquea()
        {
            Assert.IsFalse(ReglaBloqueoAnimacion.DebeBloquear(false, 0f));
        }
    }
}

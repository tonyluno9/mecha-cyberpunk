using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1327: reglas puras del atropello, del acercamiento del golpe y del tiempo del clip de bajar la escalera.</summary>
    public class JugadorT1327EditPruebas
    {
        [Test]
        public void Derriba_CarroRapidoHaciaElJugador_Derriba()
        {
            Assert.IsTrue(ReglaAtropello.Derriba(new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 2f)));
        }

        [Test]
        public void Derriba_CarroLento_NoDerriba()
        {
            Assert.IsFalse(ReglaAtropello.Derriba(new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 2f)));
        }

        [Test]
        public void Derriba_CarroAlejandose_NoDerriba()
        {
            Assert.IsFalse(ReglaAtropello.Derriba(new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, -2f)));
        }

        [Test]
        public void DireccionEmpuje_ConVelocidad_EsLaDelCarro()
        {
            Vector3 d = ReglaAtropello.DireccionEmpuje(new Vector3(3f, 5f, 0f), Vector3.forward);
            Assert.AreEqual(1f, d.x, 1e-4f);
            Assert.AreEqual(0f, d.y, 1e-4f);
        }

        [Test]
        public void Acercamiento_Lejos_TopeEnAvanceMaximo()
        {
            Assert.AreEqual(0.8f, SelectorAutoGiro.Acercamiento(2.5f, 1f, 0.8f), 1e-4f);
        }

        [Test]
        public void Acercamiento_Cerca_NoAvanza()
        {
            Assert.AreEqual(0f, SelectorAutoGiro.Acercamiento(0.9f, 1f, 0.8f), 1e-4f);
        }

        [Test]
        public void Acercamiento_Medio_FaltaParaLaParada()
        {
            Assert.AreEqual(0.4f, SelectorAutoGiro.Acercamiento(1.4f, 1f, 0.8f), 1e-4f);
        }

        [Test]
        public void TiempoClip_AlReves_AvanzaCuandoElProgresoBaja()
        {
            const float metros = 0.9f;
            const float largo = 1.2f;
            float a = AnimacionEscalera.TiempoClip(3f, metros, largo, true);
            float b = AnimacionEscalera.TiempoClip(2.9f, metros, largo, true);
            float delta = Mathf.Repeat(b - a, largo);
            Assert.AreEqual(0.1f / metros * largo, delta, 1e-3f, "bajar 10 cm avanza el clip de bajar hacia adelante");
        }

        [Test]
        public void TiempoClip_Normal_NoCambia()
        {
            Assert.AreEqual(AnimacionEscalera.TiempoClip(0.45f, 0.9f, 1.2f), AnimacionEscalera.TiempoClip(0.45f, 0.9f, 1.2f, false), 1e-6f);
        }
    }
}

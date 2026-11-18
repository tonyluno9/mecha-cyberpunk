using MechaCyberpunk.UI.Inicio;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1330: la foto de carga conserva su proporción y cabe entera en la pantalla.</summary>
    public class FotosCargaT1330Pruebas
    {
        [Test]
        public void FotoMasAnchaQueLaPantalla_CabePorElAncho_ConBordesArribaYAbajo()
        {
            Vector2 t = ReglaFotosCarga.TamanoEncajado(2000f, 1000f, 1920f, 1080f);
            Assert.AreEqual(1920f, t.x, 1e-3f);
            Assert.AreEqual(960f, t.y, 1e-3f);
        }

        [Test]
        public void FotoMasAltaQueLaPantalla_CabePorElAlto()
        {
            Vector2 t = ReglaFotosCarga.TamanoEncajado(1000f, 1000f, 1920f, 1080f);
            Assert.AreEqual(1080f, t.y, 1e-3f);
            Assert.AreEqual(1080f, t.x, 1e-3f);
        }

        [Test]
        public void SiempreConservaLaProporcion_YNoSobrepasaLaPantalla([Values(1376f, 1024f, 2400f)] float ancho)
        {
            Vector2 t = ReglaFotosCarga.TamanoEncajado(ancho, 768f, 1920f, 1080f);
            Assert.AreEqual(ancho / 768f, t.x / t.y, 1e-3f);
            Assert.LessOrEqual(t.x, 1920f + 1e-3f);
            Assert.LessOrEqual(t.y, 1080f + 1e-3f);
        }
    }
}

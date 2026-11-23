using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1706 (T67): enemigos en el minimapa y el mapa grande. Hostil a menos de 60 m sale, lejos no; los de misión siempre; policía azul, Nova violeta, enemigo rojo.</summary>
    public class MarcasEnemigosT67Pruebas
    {
        [Test]
        public void HostilDentroDe60m_Sale_Fuera_No()
        {
            Assert.IsTrue(ReglaMarcaEnemigo.Sale(false, true, 59f));
            Assert.IsTrue(ReglaMarcaEnemigo.Sale(false, true, ReglaMarcaEnemigo.Radio));
            Assert.IsFalse(ReglaMarcaEnemigo.Sale(false, true, 61f));
        }

        [Test]
        public void SinPelea_NoSale_PeroLosDeMisionSiempre()
        {
            Assert.IsFalse(ReglaMarcaEnemigo.Sale(false, false, 5f));
            Assert.IsTrue(ReglaMarcaEnemigo.Sale(true, false, 500f));
        }

        [Test]
        public void Colores_RojoEnemigo_AzulPolicia_VioletaNova()
        {
            Color rojo = Color.red;
            Assert.AreEqual(rojo, IconosMapa.ColorPunto(TipoMarcaMapa.Enemigo, rojo));
            Assert.AreEqual(rojo, IconosMapa.ColorPunto(TipoMarcaMapa.Jefe, rojo));
            Color policia = IconosMapa.ColorPunto(TipoMarcaMapa.Policia, rojo);
            Assert.Greater(policia.b, policia.r);
            Assert.Greater(policia.b, 0.8f);
            Assert.AreEqual(FormatoTerritorio.ColorDe(IdPandilla.Nova), IconosMapa.ColorPunto(TipoMarcaMapa.Nova, rojo));
            Assert.AreNotEqual(policia, IconosMapa.ColorPunto(TipoMarcaMapa.Nova, rojo));
        }

        [TestCase(TipoMarcaMapa.Enemigo, true)]
        [TestCase(TipoMarcaMapa.Jefe, true)]
        [TestCase(TipoMarcaMapa.Policia, true)]
        [TestCase(TipoMarcaMapa.Nova, true)]
        [TestCase(TipoMarcaMapa.Mercado, false)]
        [TestCase(TipoMarcaMapa.Casa, false)]
        public void EsEnemigo_SoloLasPersonasHostiles(TipoMarcaMapa tipo, bool esperado)
        {
            Assert.AreEqual(esperado, IconosMapa.EsEnemigo(tipo));
        }
    }
}

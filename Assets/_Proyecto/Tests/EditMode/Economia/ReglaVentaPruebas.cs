using System.Collections.Generic;
using MechaCyberpunk.Economia;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public class ReglaVentaPruebas
    {
        private static readonly TipoObjeto[] PiezasYChatarra = { TipoObjeto.Pieza, TipoObjeto.Chatarra };
        private const int ValorBrazo = 300;
        private const int ValorChatarra = 50;

        private DatosObjeto brazo;
        private DatosObjeto chatarra;
        private DatosObjeto kit;

        [SetUp]
        public void Preparar()
        {
            brazo = DatosObjeto.Crear("brazo", "Brazo", TipoObjeto.Pieza, ValorBrazo, 5);
            chatarra = DatosObjeto.Crear("chatarra", "Chatarra", TipoObjeto.Chatarra, ValorChatarra, 20);
            kit = DatosObjeto.Crear("kit", "Kit", TipoObjeto.KitReparacion, 0, 3);
        }

        [Test]
        public void Kit_ConValorCero_NoSeVende()
        {
            Assert.IsFalse(ReglaVenta.PuedeVender(kit, PiezasYChatarra));
            Assert.AreEqual(0, ReglaVenta.Pago(kit, PiezasYChatarra));
        }

        [Test]
        public void Pieza_EnMostradorQueNoCompraPiezas_NoSeVende()
        {
            Assert.IsFalse(ReglaVenta.PuedeVender(brazo, new[] { TipoObjeto.Chatarra }));
            Assert.AreEqual(0, ReglaVenta.Pago(brazo, new[] { TipoObjeto.Chatarra }));
            Assert.IsFalse(ReglaVenta.PuedeVender(brazo, new TipoObjeto[0]));
            Assert.IsFalse(ReglaVenta.PuedeVender(brazo, null));
        }

        [Test]
        public void Pago_EsElValorDeVenta()
        {
            Assert.IsTrue(ReglaVenta.PuedeVender(brazo, PiezasYChatarra));
            Assert.AreEqual(ValorBrazo, ReglaVenta.Pago(brazo, PiezasYChatarra));
            Assert.AreEqual(ValorChatarra, ReglaVenta.Pago(chatarra, PiezasYChatarra));
        }

        [Test]
        public void ObjetoNulo_NoSeVende()
        {
            Assert.IsFalse(ReglaVenta.PuedeVender(null, PiezasYChatarra));
            Assert.AreEqual(0, ReglaVenta.Pago(null, PiezasYChatarra));
        }

        [Test]
        public void Vendibles_SinInventario_QuedaVacio()
        {
            var resultado = new List<DatosObjeto> { brazo };
            ReglaVenta.Vendibles(new[] { brazo }, null, PiezasYChatarra, resultado);
            Assert.IsEmpty(resultado);
        }
    }
}

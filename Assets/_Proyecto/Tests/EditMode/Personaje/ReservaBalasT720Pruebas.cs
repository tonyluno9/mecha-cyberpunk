using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-720: la reserva de balas por arma, la recarga que la gasta y los datos de El Mercado.</summary>
    public class ReservaBalasT720Pruebas
    {
        private class FuenteFalsa : IFuenteBalas
        {
            public int Resto;
            public int Disponibles => Resto;

            public int Tomar(int cantidad)
            {
                int n = System.Math.Min(cantidad, Resto);
                Resto -= n;
                return n;
            }
        }

        [Test]
        public void Modelo_SoloEscopetaFusilYSniperGastanReserva()
        {
            Assert.IsTrue(ModeloReservaBalas.EsLimitada(ArmaAPie.Escopeta));
            Assert.IsTrue(ModeloReservaBalas.EsLimitada(ArmaAPie.Fusil));
            Assert.IsTrue(ModeloReservaBalas.EsLimitada(ArmaAPie.Sniper));
            Assert.IsFalse(ModeloReservaBalas.EsLimitada(ArmaAPie.Pistola));
            Assert.IsFalse(ModeloReservaBalas.EsLimitada(ArmaAPie.Punos));
        }

        [Test]
        public void Modelo_AsegurarDaLasInicialesUnaSolaVez()
        {
            var modelo = new ModeloReservaBalas();
            Assert.IsTrue(modelo.Asegurar(ArmaAPie.Fusil, 60, 240));
            Assert.AreEqual(60, modelo.Cantidad(ArmaAPie.Fusil));
            modelo.Tomar(ArmaAPie.Fusil, 60);
            Assert.IsFalse(modelo.Asegurar(ArmaAPie.Fusil, 60, 240), "No regala otra vez");
            Assert.AreEqual(0, modelo.Cantidad(ArmaAPie.Fusil));
            Assert.IsFalse(modelo.Asegurar(ArmaAPie.Pistola, 10, 100), "La pistola no lleva reserva");
        }

        [Test]
        public void Modelo_AgregarNoPasaDelMaximo()
        {
            var modelo = new ModeloReservaBalas();
            modelo.Asegurar(ArmaAPie.Escopeta, 36, 40);
            Assert.AreEqual(4, modelo.Agregar(ArmaAPie.Escopeta, 8, 40), "Solo caben 4");
            Assert.AreEqual(40, modelo.Cantidad(ArmaAPie.Escopeta));
            Assert.AreEqual(0, modelo.Agregar(ArmaAPie.Escopeta, 8, 40));
            Assert.AreEqual(0, modelo.Espacio(ArmaAPie.Escopeta, 40));
        }

        [Test]
        public void Modelo_TomarNoSacaMasDeLoQueHay()
        {
            var modelo = new ModeloReservaBalas();
            modelo.Asegurar(ArmaAPie.Sniper, 3, 40);
            Assert.AreEqual(3, modelo.Tomar(ArmaAPie.Sniper, 10));
            Assert.AreEqual(0, modelo.Cantidad(ArmaAPie.Sniper));
        }

        [Test]
        public void Cargador_ConFuenteGastaBalasDeLaReserva()
        {
            var fuente = new FuenteFalsa { Resto = 7 };
            var c = new CargadorPistola(5, 0f, 1f) { Fuente = fuente };
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(c.IntentarDisparar());
            }
            Assert.IsTrue(c.EmpezarRecarga());
            c.Avanzar(1.1f);
            Assert.AreEqual(5, c.Balas);
            Assert.AreEqual(2, fuente.Resto, "Gastó 5 de las 7");
            for (int i = 0; i < 5; i++)
            {
                c.IntentarDisparar();
            }
            Assert.IsTrue(c.EmpezarRecarga());
            c.Avanzar(1.1f);
            Assert.AreEqual(2, c.Balas, "Solo quedaban 2");
            Assert.AreEqual(0, fuente.Resto);
        }

        [Test]
        public void Cargador_SinReservaNoRecarga()
        {
            var c = new CargadorPistola(5, 0f, 1f) { Fuente = new FuenteFalsa { Resto = 0 } };
            c.IntentarDisparar();
            Assert.IsFalse(c.EmpezarRecarga(), "Sin balas de reserva no hay recarga");
            Assert.IsFalse(c.Recargando);
            Assert.AreEqual(4, c.Balas);
        }

        [Test]
        public void Cargador_SinFuenteSigueInfinito()
        {
            var c = new CargadorPistola(8, 0f, 1f);
            c.IntentarDisparar();
            Assert.IsTrue(c.EmpezarRecarga());
            c.Avanzar(1.1f);
            Assert.AreEqual(8, c.Balas);
        }

        [TestCase("escopeta", 60)]
        [TestCase("fusil", 120)]
        [TestCase("sniper", 150)]
        public void Datos_LasBalasEstanEnElCatalogoYEnLaTienda(string clave, int precio)
        {
            var balas = AssetDatabase.LoadAssetAtPath<DatosMunicionArma>("Assets/_Proyecto/Economia/Datos/DatosMunicionArma_" + clave + ".asset");
            Assert.IsNotNull(balas, "Existe DatosMunicionArma_" + clave);
            Assert.IsNotNull(balas.Oferta);
            Assert.AreEqual(precio, balas.Oferta.Precio);
            Assert.Greater(balas.BalasPorPaquete, 0);
            Assert.GreaterOrEqual(balas.Maximo, balas.BalasPorPaquete);
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>("Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset");
            Assert.Contains(balas, new System.Collections.Generic.List<DatosObjeto>(catalogo.Objetos));
            var tienda = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Core.DatosTienda>("Assets/_Proyecto/Economia/Datos/DatosTienda_MercadoNegro.asset");
            Assert.Contains(balas.Oferta, new System.Collections.Generic.List<MechaCyberpunk.Core.DatosOferta>(tienda.Ofertas));
        }
    }
}

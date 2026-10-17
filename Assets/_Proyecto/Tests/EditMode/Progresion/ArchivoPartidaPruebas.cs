using System.Collections.Generic;
using MechaCyberpunk.Progresion.Guardado;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>Formato del archivo de partida (T-111): ida y vuelta de piezas y lectura que nunca lanza excepciones.</summary>
    public class ArchivoPartidaPruebas
    {
        private const string Fecha = "2026-10-16T12:00:00.0000000Z";

        [Test]
        public void IdaYVuelta_ConComillasAcentosYSaltos_ConservaLosDatosTalCual()
        {
            string datosA = "{\"nombre\":\"Ana \\\"La Chispa\\\"\",\"alias\":\"ÁÉÍÓÚ ñ\"}";
            string datosB = "linea 1\nlinea 2\r\n\ttab y \\ barra";
            var piezas = new List<PiezaGuardada>
            {
                new PiezaGuardada { clave = "perfil", datos = datosA },
                new PiezaGuardada { clave = "reloj", datos = datosB },
            };

            string json = ArchivoPartida.Escribir(piezas, Fecha);
            bool ok = ArchivoPartida.TryLeer(json, out List<PiezaGuardada> leidas, out string fecha);

            Assert.IsTrue(ok);
            Assert.AreEqual(Fecha, fecha);
            Assert.AreEqual(2, leidas.Count);
            Assert.AreEqual("perfil", leidas[0].clave);
            Assert.AreEqual(datosA, leidas[0].datos);
            Assert.AreEqual("reloj", leidas[1].clave);
            Assert.AreEqual(datosB, leidas[1].datos);
        }

        [Test]
        public void IdaYVuelta_ListaVacia_EsValidaYSinPiezas()
        {
            string json = ArchivoPartida.Escribir(new List<PiezaGuardada>(), Fecha);

            bool ok = ArchivoPartida.TryLeer(json, out List<PiezaGuardada> leidas, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(0, leidas.Count);
        }

        [Test]
        public void Escribir_Siempre_UsaLaVersionActual()
        {
            string json = ArchivoPartida.Escribir(new List<PiezaGuardada>(), Fecha);

            Assert.AreEqual(1, ArchivoPartida.VersionActual);
            StringAssert.Contains("\"version\":1", json);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        [TestCase("esto no es json")]
        [TestCase("{\"version\":1,\"piezas\":[{\"clave\":")]
        [TestCase("[1,2,3]")]
        public void TryLeer_JsonRotoOVacio_DevuelveFalseSinExcepcion(string json)
        {
            bool ok = true;
            List<PiezaGuardada> leidas = null;
            Assert.DoesNotThrow(() => ok = ArchivoPartida.TryLeer(json, out leidas, out _));

            Assert.IsFalse(ok);
            Assert.AreEqual(0, leidas.Count);
        }

        [TestCase(2)]
        [TestCase(99)]
        [TestCase(0)]
        public void TryLeer_VersionDistintaDeLaActual_DevuelveFalse(int version)
        {
            string json = "{\"version\":" + version + ",\"fecha\":\"x\",\"piezas\":[{\"clave\":\"cartera\",\"datos\":\"{}\"}]}";

            bool ok = ArchivoPartida.TryLeer(json, out List<PiezaGuardada> leidas, out _);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, leidas.Count);
        }

        [Test]
        public void Escribir_PiezaSinClave_SeOmite()
        {
            var piezas = new List<PiezaGuardada>
            {
                new PiezaGuardada { clave = "", datos = "x" },
                new PiezaGuardada { clave = "cartera", datos = null },
            };

            ArchivoPartida.TryLeer(ArchivoPartida.Escribir(piezas, Fecha), out List<PiezaGuardada> leidas, out _);

            Assert.AreEqual(1, leidas.Count);
            Assert.AreEqual("cartera", leidas[0].clave);
            Assert.AreEqual(string.Empty, leidas[0].datos);
        }
    }
}

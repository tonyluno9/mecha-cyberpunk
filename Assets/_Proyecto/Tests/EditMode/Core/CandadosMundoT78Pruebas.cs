using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-78: los candados del mundo y cómo los lee MisionProtegida.</summary>
    public class CandadosMundoT78Pruebas
    {
        [SetUp]
        public void Antes()
        {
            CandadosMundo.Liberar();
            MisionProtegida.Desactivar();
        }

        [TearDown]
        public void Despues()
        {
            CandadosMundo.Liberar();
            MisionProtegida.Desactivar();
        }

        [Test]
        public void Fijar_PoneYQuitaLosCandadosYAvisaUnaVezPorCambio()
        {
            int avisos = 0;
            System.Action escucha = () => avisos++;
            CandadosMundo.Cambio += escucha;
            CandadosMundo.Fijar(true, true, true, true, true, CandadosMundo.TiendaDoc);
            Assert.IsTrue(CandadosMundo.Policia && CandadosMundo.Pandillas && CandadosMundo.Nova && CandadosMundo.RoboCarros && CandadosMundo.Tiendas);
            Assert.AreEqual(CandadosMundo.TiendaDoc, CandadosMundo.TiendaAbierta);
            CandadosMundo.Fijar(true, true, true, true, true, CandadosMundo.TiendaDoc);
            Assert.AreEqual(1, avisos, "lo mismo otra vez no avisa");
            CandadosMundo.Liberar();
            CandadosMundo.Cambio -= escucha;
            Assert.IsFalse(CandadosMundo.Alguno);
            Assert.AreEqual(2, avisos);
        }

        [Test]
        public void CandadoDePolicia_HaceActivaLaMisionProtegidaSinNova()
        {
            Assert.IsFalse(MisionProtegida.Activa);
            CandadosMundo.Fijar(true, false, false, false, false);
            Assert.IsTrue(MisionProtegida.Activa, "sin calor ni patrullas");
            Assert.IsFalse(MisionProtegida.NovaPermitida);
            CandadosMundo.Liberar();
            Assert.IsFalse(MisionProtegida.Activa);
        }

        [Test]
        public void CandadoDeNova_QuitaLaNovaPermitidaDeUnaMision()
        {
            MisionProtegida.Activar("prueba", true);
            Assert.IsTrue(MisionProtegida.NovaPermitida);
            CandadosMundo.Fijar(false, false, true, false, false);
            Assert.IsFalse(MisionProtegida.NovaPermitida);
            Assert.IsTrue(MisionProtegida.Activa, "la misión sigue activa");
        }

        [Test]
        public void MisionProtegida_SigueActivandoseYDesactivandoseSinCandados()
        {
            MisionProtegida.Activar("prueba", false);
            Assert.IsTrue(MisionProtegida.Activa);
            MisionProtegida.Desactivar();
            Assert.IsFalse(MisionProtegida.Activa);
        }

        [Test]
        public void TiendaPermitida_SoloLaDelPaso()
        {
            Assert.IsTrue(CandadosMundo.TiendaPermitida("cualquiera"), "sin candado todo abre");
            CandadosMundo.Fijar(true, true, true, true, true, CandadosMundo.TiendaTino);
            Assert.IsTrue(CandadosMundo.TiendaPermitida(CandadosMundo.TiendaTino));
            Assert.IsFalse(CandadosMundo.TiendaPermitida(CandadosMundo.TiendaDoc));
            Assert.IsFalse(CandadosMundo.TiendaPermitida(string.Empty));
        }

        [Test]
        public void Negar_AvisaConElTextoSuave()
        {
            string recibido = null;
            System.Action<string> escucha = t => recibido = t;
            CandadosMundo.Negado += escucha;
            CandadosMundo.Negar();
            CandadosMundo.Negado -= escucha;
            Assert.AreEqual(CandadosMundo.AvisoAunNo, recibido);
        }
    }
}

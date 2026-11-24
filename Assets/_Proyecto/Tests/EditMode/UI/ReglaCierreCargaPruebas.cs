using MechaCyberpunk.UI.Titulo;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T96: la pantalla de carga se quita también al volver al menú principal, que no trae jugador.</summary>
    public class ReglaCierreCargaPruebas
    {
        [Test]
        public void DestinoListo_LlegaInicioSinJugador_EstaListo()
        {
            Assert.IsTrue(ReglaCierreCarga.DestinoListo(false, true, false, false), "HAS MUERTO o pausa → MENÚ PRINCIPAL: Inicio no tiene jugador");
        }

        [Test]
        public void DestinoListo_LlegaJugador_EstaListo()
        {
            Assert.IsTrue(ReglaCierreCarga.DestinoListo(true, false, false, false), "REINTENTAR o CARGAR: llega el jugador");
        }

        [Test]
        public void DestinoListo_TituloDeLaCiudadEnEditor_EstaListo()
        {
            Assert.IsTrue(ReglaCierreCarga.DestinoListo(false, false, true, false), "en el editor el título de la ciudad es el menú");
        }

        [Test]
        public void DestinoListo_TituloQueVuelveAlInicio_SigueEsperando()
        {
            Assert.IsFalse(ReglaCierreCarga.DestinoListo(false, false, true, true), "en el build el título manda a Inicio: la carga sigue hasta allá");
        }

        [Test]
        public void DestinoListo_SinJugadorNiMenu_SigueEsperando()
        {
            Assert.IsFalse(ReglaCierreCarga.DestinoListo(false, false, false, false));
        }

        [Test]
        public void PuedeTerminarAsincrona_MenuCargadoYOperacionHecha_Termina()
        {
            bool listo = ReglaCierreCarga.DestinoListo(false, true, false, false);
            Assert.IsTrue(ReglaCierreCarga.PuedeTerminarAsincrona(true, true, listo), "antes esperaba al jugador y se quedaba encima del menú");
        }

        [Test]
        public void PuedeTerminarAsincrona_SinActivarOSinTerminar_NoTermina()
        {
            Assert.IsFalse(ReglaCierreCarga.PuedeTerminarAsincrona(false, true, true));
            Assert.IsFalse(ReglaCierreCarga.PuedeTerminarAsincrona(true, false, true));
            Assert.IsFalse(ReglaCierreCarga.PuedeTerminarAsincrona(true, true, false));
        }

        [Test]
        public void PuedeTerminarSimulada_DestinoListoTiempoYAvance_Termina()
        {
            Assert.IsTrue(ReglaCierreCarga.PuedeTerminarSimulada(true, 3f, 2.2f, 1f));
            Assert.IsFalse(ReglaCierreCarga.PuedeTerminarSimulada(false, 3f, 2.2f, 1f), "sin destino no termina");
            Assert.IsFalse(ReglaCierreCarga.PuedeTerminarSimulada(true, 1f, 2.2f, 1f), "antes del tiempo mínimo no termina");
            Assert.IsFalse(ReglaCierreCarga.PuedeTerminarSimulada(true, 3f, 2.2f, 0.5f), "con el avance a medias no termina");
        }
    }
}

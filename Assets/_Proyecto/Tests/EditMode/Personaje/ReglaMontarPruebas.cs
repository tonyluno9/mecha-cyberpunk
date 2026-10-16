using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    public class ReglaMontarPruebas
    {
        [Test]
        public void Decidir_APieConMontableQuePuedeSubir_Sube()
        {
            Assert.AreEqual(AccionMontar.Subir,
                ReglaMontar.Decidir(ModoJugador.APie, false, TipoCandidatoAPie.Montable, true, false));
        }

        [Test]
        public void Decidir_APieConMontableQueNoPuedeSubir_NoHaceNada()
        {
            Assert.AreEqual(AccionMontar.Nada,
                ReglaMontar.Decidir(ModoJugador.APie, false, TipoCandidatoAPie.Montable, false, false));
        }

        [Test]
        public void Decidir_APieConPuerta_AbreLaPuerta()
        {
            Assert.AreEqual(AccionMontar.AbrirPuerta,
                ReglaMontar.Decidir(ModoJugador.APie, false, TipoCandidatoAPie.Puerta, false, false));
        }

        [Test]
        public void Decidir_APieSinCandidato_NoHaceNada()
        {
            Assert.AreEqual(AccionMontar.Nada,
                ReglaMontar.Decidir(ModoJugador.APie, false, TipoCandidatoAPie.Ninguno, true, true));
        }

        [Test]
        public void Decidir_EnCarroQuePuedeBajar_Baja()
        {
            Assert.AreEqual(AccionMontar.Bajar,
                ReglaMontar.Decidir(ModoJugador.Carro, false, TipoCandidatoAPie.Ninguno, false, true));
        }

        [Test]
        public void Decidir_EnCarroQueNoPuedeBajar_AvisaQueNoPuede()
        {
            Assert.AreEqual(AccionMontar.NoPuedeBajar,
                ReglaMontar.Decidir(ModoJugador.Carro, false, TipoCandidatoAPie.Ninguno, false, false));
        }

        [Test]
        public void Decidir_EnMechaQuePuedeBajar_Baja()
        {
            Assert.AreEqual(AccionMontar.Bajar,
                ReglaMontar.Decidir(ModoJugador.Mecha, false, TipoCandidatoAPie.Ninguno, false, true));
        }

        [Test]
        public void Decidir_EnCarroConPuertaDeFrente_NoAbreLaPuerta()
        {
            Assert.AreEqual(AccionMontar.Bajar,
                ReglaMontar.Decidir(ModoJugador.Carro, false, TipoCandidatoAPie.Puerta, false, true));
            Assert.AreEqual(AccionMontar.NoPuedeBajar,
                ReglaMontar.Decidir(ModoJugador.Carro, false, TipoCandidatoAPie.Puerta, false, false));
        }

        [Test]
        public void Decidir_EnKO_NoHaceNadaEnNingunModo()
        {
            Assert.AreEqual(AccionMontar.Nada,
                ReglaMontar.Decidir(ModoJugador.APie, true, TipoCandidatoAPie.Montable, true, true));
            Assert.AreEqual(AccionMontar.Nada,
                ReglaMontar.Decidir(ModoJugador.APie, true, TipoCandidatoAPie.Puerta, true, true));
            Assert.AreEqual(AccionMontar.Nada,
                ReglaMontar.Decidir(ModoJugador.Carro, true, TipoCandidatoAPie.Ninguno, true, true));
        }
    
        [Test]
        public void Decidir_APieConInteractuableQuePuedeUsarse_Usa()
        {
            Assert.AreEqual(AccionMontar.Usar,
                ReglaMontar.Decidir(ModoJugador.APie, false, TipoCandidatoAPie.Interactuable, false, false, true));
        }

        [Test]
        public void Decidir_APieConInteractuableQueNoPuedeUsarse_Niega()
        {
            Assert.AreEqual(AccionMontar.UsarNegado,
                ReglaMontar.Decidir(ModoJugador.APie, false, TipoCandidatoAPie.Interactuable, false, false, false));
        }

        [Test]
        public void Decidir_InteractuableEnKO_NoHaceNada()
        {
            Assert.AreEqual(AccionMontar.Nada,
                ReglaMontar.Decidir(ModoJugador.APie, true, TipoCandidatoAPie.Interactuable, false, false, true));
        }

        [Test]
        public void Decidir_EnCarroConInteractuable_BajaComoSiempre()
        {
            Assert.AreEqual(AccionMontar.Bajar,
                ReglaMontar.Decidir(ModoJugador.Carro, false, TipoCandidatoAPie.Interactuable, false, true, true));
        }
}
}

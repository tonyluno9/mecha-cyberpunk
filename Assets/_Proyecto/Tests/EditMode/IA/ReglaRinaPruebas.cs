using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1107: la lógica pura de la riña entre SNAKES y SKULLS.</summary>
    public class ReglaRinaPruebas
    {
        private static ReglaRina.Candidato C(float x, float z, IdPandilla pandilla, bool enPie = true, float vida = 1f)
        {
            return new ReglaRina.Candidato { Posicion = new Vector3(x, 0f, z), Pandilla = pandilla, EnPie = enPie, VidaFraccion = vida };
        }

        [Test]
        public void ElegirRival_VariosRivales_ElMasCercanoDeOtraPandilla()
        {
            List<ReglaRina.Candidato> lista = new List<ReglaRina.Candidato>
            {
                C(1f, 0f, IdPandilla.Viboras),   // de la suya, aunque esté más cerca
                C(8f, 0f, IdPandilla.Calaveras),
                C(5f, 0f, IdPandilla.Calaveras)
            };
            Assert.AreEqual(2, ReglaRina.ElegirRival(Vector3.zero, IdPandilla.Viboras, lista));
        }

        [Test]
        public void ElegirRival_IgnoraALosDeSuPandillaYALosKO()
        {
            List<ReglaRina.Candidato> lista = new List<ReglaRina.Candidato>
            {
                C(2f, 0f, IdPandilla.Viboras),
                C(3f, 0f, IdPandilla.Calaveras, enPie: false),
                C(10f, 0f, IdPandilla.Calaveras)
            };
            Assert.AreEqual(2, ReglaRina.ElegirRival(Vector3.zero, IdPandilla.Viboras, lista), "salta al de su pandilla y al KO");
            lista.RemoveAt(2);
            Assert.AreEqual(-1, ReglaRina.ElegirRival(Vector3.zero, IdPandilla.Viboras, lista), "sin rival válido");
        }

        [Test]
        public void ElegirRival_FueraDelRadioOEnElPiso_NoLoElige()
        {
            Assert.AreEqual(-1, ReglaRina.ElegirRival(Vector3.zero, IdPandilla.Viboras,
                new List<ReglaRina.Candidato> { C(ReglaRina.RadioBusqueda + 1f, 0f, IdPandilla.Calaveras) }), "a más de 15 m");
            Assert.AreEqual(-1, ReglaRina.ElegirRival(Vector3.zero, IdPandilla.Viboras,
                new List<ReglaRina.Candidato> { C(3f, 0f, IdPandilla.Calaveras, vida: ReglaRina.FraccionMinimaVida) }), "ya no se le pega a quien llegó al mínimo de vida");
        }

        [Test]
        public void DebeSoltar_ElJugadorSeAcercaOLePega_SueltaElControl()
        {
            Assert.IsTrue(ReglaRina.DebeSoltar(ReglaRina.RadioJugador, true, false), "entra a 6 m");
            Assert.IsTrue(ReglaRina.DebeSoltar(40f, true, true), "le pegó a alguno, aunque esté lejos");
            Assert.IsFalse(ReglaRina.DebeSoltar(ReglaRina.RadioJugador + 0.5f, true, false), "a 6.5 m sigue la riña");
            Assert.IsFalse(ReglaRina.DebeSoltar(2f, false, false), "si el jugador no va a pie no cuenta");
        }

        [Test]
        public void Danio_EsElTreintaYCincoPorCiento()
        {
            Assert.AreEqual(3.5f, ReglaRina.Danio(10f), 0.0001f);
        }

        [Test]
        public void BandoCaido_SoloCuandoNadieQuedaEnPieConVida()
        {
            Assert.IsFalse(ReglaRina.BandoCaido(new List<ReglaRina.Candidato> { C(0f, 0f, IdPandilla.Viboras, false), C(0f, 0f, IdPandilla.Viboras) }));
            Assert.IsTrue(ReglaRina.BandoCaido(new List<ReglaRina.Candidato>
            {
                C(0f, 0f, IdPandilla.Viboras, false), C(0f, 0f, IdPandilla.Viboras, true, ReglaRina.FraccionMinimaVida)
            }));
        }

        [Test]
        public void DebeRearmar_LejosYSinVerse()
        {
            Assert.IsTrue(ReglaRina.DebeRearmar(ReglaRina.DistanciaRearme + 1f, false));
            Assert.IsFalse(ReglaRina.DebeRearmar(ReglaRina.DistanciaRearme + 1f, true), "alguno se ve en cámara");
            Assert.IsFalse(ReglaRina.DebeRearmar(ReglaRina.DistanciaRearme - 1f, false), "el jugador aún está cerca");
        }

        [Test]
        public void Grito_CadaBandoGritaALaOtra()
        {
            Assert.AreEqual("¡Fuera de aquí, calaca!", ReglaRina.Grito(IdPandilla.Viboras, 0f));
            Assert.AreEqual("¡Lárgate, víbora!", ReglaRina.Grito(IdPandilla.Calaveras, 0f));
            Assert.IsNotEmpty(ReglaRina.Grito(IdPandilla.Calaveras, 1f), "el azar en 1 no se sale de la lista");
        }
    
        [Test]
        public void Gritos_UnoCadaUnoYMedioComoMinimoEnLaMismaRina()
        {
            Assert.IsFalse(ReglaRina.PuedeGritar(10f, 9f), "1 s después todavía no");
            Assert.IsTrue(ReglaRina.PuedeGritar(10.5f, 9f), "1.5 s después sí");
            Assert.IsTrue(ReglaRina.PuedeGritar(0f, -100f), "el primero de la riña siempre puede");
        }

        [Test]
        public void Gritos_ElPrimeroYElSiguienteSeDesfasanConElAzar()
        {
            Assert.AreEqual(1f, ReglaRina.PrimerGrito(0f, 0f), 0.0001f);
            Assert.AreEqual(8f, ReglaRina.PrimerGrito(0f, 1f), 0.0001f);
            Assert.AreNotEqual(ReglaRina.PrimerGrito(0f, 0.2f), ReglaRina.PrimerGrito(0f, 0.7f), "dos miembros con azar distinto no gritan a la vez");
            Assert.AreEqual(5f, ReglaRina.SiguienteGrito(0f, 0f), 0.0001f);
            Assert.AreEqual(12f, ReglaRina.SiguienteGrito(0f, 1f), 0.0001f);
        }
    }
}

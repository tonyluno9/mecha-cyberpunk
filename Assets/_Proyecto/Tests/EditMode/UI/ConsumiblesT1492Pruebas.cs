using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Rueda;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1492: las dos ranuras de consumibles, sus teclas, curarse en carro y el aviso de vida baja a 40 %.</summary>
    public class ConsumiblesT1492Pruebas
    {
        private static readonly string[] Tres = { "bebida", "papitas", "venda" };

        [SetUp]
        public void SetUp() => RanurasConsumibles.Reiniciar();

        [TearDown]
        public void TearDown() => RanurasConsumibles.Reiniciar();

        [Test]
        public void Efectivos_SinAsignar_LlenaConLosDosPrimeros()
        {
            string[] r = RanurasConsumibles.Efectivos(Tres);
            Assert.AreEqual("bebida", r[0]);
            Assert.AreEqual("papitas", r[1]);
        }

        [Test]
        public void Efectivos_ConUnSoloObjeto_LaSegundaQuedaVacia()
        {
            string[] r = RanurasConsumibles.Efectivos(new[] { "venda" });
            Assert.AreEqual("venda", r[0]);
            Assert.IsNull(r[1]);
        }

        [Test]
        public void Efectivos_SinNada_AmbasVacias()
        {
            string[] r = RanurasConsumibles.Efectivos(new string[0]);
            Assert.IsNull(r[0]);
            Assert.IsNull(r[1]);
        }

        [Test]
        public void Asignar_PoneElObjetoEnLaRanura()
        {
            RanurasConsumibles.Asignar(1, "venda");
            string[] r = RanurasConsumibles.Efectivos(Tres);
            Assert.AreEqual("venda", r[1]);
            Assert.AreEqual("bebida", r[0]);
        }

        [Test]
        public void Asignar_UnObjetoQueEstabaEnLaOtra_SeIntercambian()
        {
            RanurasConsumibles.Asignar(0, "bebida");
            RanurasConsumibles.Asignar(1, "papitas");
            RanurasConsumibles.Asignar(0, "papitas");
            string[] r = RanurasConsumibles.Efectivos(Tres);
            Assert.AreEqual("papitas", r[0]);
            Assert.AreEqual("bebida", r[1]);
        }

        [Test]
        public void Efectivos_LoAsignadoQueYaNoLleva_SeLlenaSolo()
        {
            RanurasConsumibles.Asignar(0, "cafe");
            string[] r = RanurasConsumibles.Efectivos(Tres);
            Assert.AreEqual("bebida", r[0]);
            Assert.AreEqual("papitas", r[1]);
        }

        [Test]
        public void Rotar_PasaAlSiguienteQueNoEstaEnLaOtra()
        {
            string nuevo = RanurasConsumibles.Rotar(0, Tres);
            Assert.AreEqual("venda", nuevo, "papitas ya está en la ranura 2");
            string[] r = RanurasConsumibles.Efectivos(Tres);
            Assert.AreEqual("venda", r[0]);
            Assert.AreEqual("papitas", r[1]);
        }

        [Test]
        public void Rotar_DaLaVueltaAlTerminar()
        {
            RanurasConsumibles.Rotar(1, Tres); // papitas -> venda
            string nuevo = RanurasConsumibles.Rotar(1, Tres); // venda -> bebida está en la 1, se salta -> papitas
            Assert.AreEqual("papitas", nuevo);
            Assert.AreEqual("bebida", RanurasConsumibles.Efectivos(Tres)[0]);
        }

        [Test]
        public void Rotar_ConUnSoloObjeto_NoCambia()
        {
            string[] uno = { "bebida" };
            Assert.AreEqual("bebida", RanurasConsumibles.Rotar(0, uno));
            Assert.AreEqual("bebida", RanurasConsumibles.Efectivos(uno)[0]);
        }

        [Test]
        public void Rotar_SinNada_DevuelveNull()
        {
            Assert.IsNull(RanurasConsumibles.Rotar(0, new string[0]));
        }

        [Test]
        public void Decidir_ELaPrimeraRanuraConAlgoEnfrente_NoGastaElObjeto()
        {
            Assert.AreEqual(AccionRanura.Nada, ReglaUsoConsumibles.Decidir(0, true, false, ModoJugador.APie, true), "E es de interactuar");
            Assert.AreEqual(AccionRanura.Usar, ReglaUsoConsumibles.Decidir(0, true, false, ModoJugador.APie, false));
        }

        [Test]
        public void Decidir_CNuncaChocaConInteractuar()
        {
            Assert.AreEqual(AccionRanura.Usar, ReglaUsoConsumibles.Decidir(1, true, false, ModoJugador.APie, true));
        }

        [Test]
        public void Decidir_ConAlt_Cambia()
        {
            Assert.AreEqual(AccionRanura.Cambiar, ReglaUsoConsumibles.Decidir(1, true, true, ModoJugador.APie, false));
        }

        [Test]
        public void Decidir_SinPulsar_OEnElMecha_NoHaceNada()
        {
            Assert.AreEqual(AccionRanura.Nada, ReglaUsoConsumibles.Decidir(0, false, false, ModoJugador.APie, false));
            Assert.AreEqual(AccionRanura.Nada, ReglaUsoConsumibles.Decidir(0, true, false, ModoJugador.Mecha, false));
        }

        [Test]
        public void Decidir_EnCarro_UsaLasDosRanuras()
        {
            Assert.AreEqual(AccionRanura.Usar, ReglaUsoConsumibles.Decidir(0, true, false, ModoJugador.Carro, true));
            Assert.AreEqual(AccionRanura.Usar, ReglaUsoConsumibles.Decidir(1, true, false, ModoJugador.Carro, false));
        }

        [Test]
        public void PuedeActuar_EnCarroSePuedeCurarse_PeroNoKONiEnElMecha()
        {
            Assert.IsTrue(ReglaConsumibles.PuedeActuar(ModoJugador.Carro, false, true), "Persecución en carro");
            Assert.IsTrue(ReglaConsumibles.PuedeActuar(ModoJugador.APie, true, true));
            Assert.IsFalse(ReglaConsumibles.PuedeActuar(ModoJugador.APie, false, true), "Sin control a pie (menú, cinemática)");
            Assert.IsFalse(ReglaConsumibles.PuedeActuar(ModoJugador.Carro, true, false), "KO");
            Assert.IsFalse(ReglaConsumibles.PuedeActuar(ModoJugador.Mecha, true, true));
        }

        [Test]
        public void TeclasPorOmision_SonEYCyEnCarroZYX()
        {
            Assert.AreEqual("E", ReglaTutorial.TeclaPorOmision(ReglaTutorial.TeclaConsumir));
            Assert.AreEqual("C", ReglaTutorial.TeclaPorOmision(ReglaTutorial.TeclaConsumir2));
            Assert.AreEqual("Z", ReglaTutorial.TeclaPorOmision(ReglaTutorial.TeclaConsumirCarro1));
            Assert.AreEqual("X", ReglaTutorial.TeclaPorOmision(ReglaTutorial.TeclaConsumirCarro2));
        }

        [Test]
        public void VidaBaja_AvisaA40PorCiento()
        {
            Assert.AreEqual(0.4f, VidaAPie.FraccionVidaBaja, 1e-5f);
            Assert.AreEqual(0.4f, ReglaVinietaVida.FraccionUmbral, 1e-5f);
            Assert.Greater(ReglaVinietaVida.Alfa(35f, 100f, 0f), 0f, "A 35 % ya hay viñeta");
            Assert.AreEqual(0f, ReglaVinietaVida.Alfa(41f, 100f, 0f), "A 41 % no");
        }

        [Test]
        public void Indicador_ElDanioAbsorbidoPorElEscudoTambienMarca()
        {
            // El dron dispara de frente-arriba: el escudo se come el daño (DanioAplicado 0) pero la marca sale igual.
            Assert.IsTrue(ReglaIndicadorDanio.Angulo(UnityEngine.Vector3.forward, new UnityEngine.Vector3(0.1f, -0.7f, -0.7f), out float grados));
            Assert.Less(System.Math.Abs(grados), 15f);
        }
    }
}

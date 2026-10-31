using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Prologo.Encargos;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    public class ReglaAtaquesPruebas
    {
        [Test]
        public void Intervalo_SinZonasExtraNiCalor_VaDelMinimoAlMaximo()
        {
            Assert.AreEqual(400f, ReglaAtaques.Intervalo(400f, 800f, 1, 0, 0f, 0.35f, 0.2f), 0.01f);
            Assert.AreEqual(800f, ReglaAtaques.Intervalo(400f, 800f, 1, 0, 1f, 0.35f, 0.2f), 0.01f);
        }

        [Test]
        public void Intervalo_ConMasZonasYCalor_EsMasCorto()
        {
            float basico = ReglaAtaques.Intervalo(400f, 800f, 1, 0, 0.5f, 0.35f, 0.2f);
            float zonas = ReglaAtaques.Intervalo(400f, 800f, 3, 0, 0.5f, 0.35f, 0.2f);
            float calor = ReglaAtaques.Intervalo(400f, 800f, 1, 3, 0.5f, 0.35f, 0.2f);
            Assert.Less(zonas, basico);
            Assert.Less(calor, basico);
            Assert.GreaterOrEqual(ReglaAtaques.Intervalo(400f, 800f, 20, 3, 0f, 0.35f, 0.2f), 100f, "Nunca baja de un cuarto del mínimo");
        }

        [Test]
        public void Permitido_SoloEnJuegoLibreSinEncargoNiAtaqueYConZona()
        {
            Assert.IsTrue(ReglaAtaques.Permitido(true, false, false, 1));
            Assert.IsFalse(ReglaAtaques.Permitido(false, false, false, 1), "historia");
            Assert.IsFalse(ReglaAtaques.Permitido(true, true, false, 1), "encargo");
            Assert.IsFalse(ReglaAtaques.Permitido(true, false, true, 1), "otro ataque");
            Assert.IsFalse(ReglaAtaques.Permitido(true, false, false, 0), "sin zonas conquistadas");
        }

        [Test]
        public void AtacanteYAviso_SegunLaZona()
        {
            Assert.AreEqual(IdPandilla.Viboras, ReglaAtaques.Atacante("tianguis"));
            Assert.AreEqual(IdPandilla.Calaveras, ReglaAtaques.Atacante("neon"));
            Assert.AreEqual(IdPandilla.Nova, ReglaAtaques.Atacante("refineria"));
            Assert.AreEqual("NOVA", ReglaAtaques.NombrePandilla(IdPandilla.Nova));
            Assert.AreEqual("¡LOS SNAKES ATACAN EL TIANGUIS!", ReglaAtaques.Aviso(IdPandilla.Viboras, "EL TIANGUIS"));
            Assert.AreEqual(-1, ReglaAtaques.Elegir(0, 0.5f));
            Assert.AreEqual(1, ReglaAtaques.Elegir(2, 0.999f));
        }

        [Test]
        public void PierdeZona_SoloBajoElUmbralTrasElCastigo()
        {
            Assert.IsFalse(ReglaAtaques.PierdeZona(100, 45, 40));
            Assert.IsTrue(ReglaAtaques.PierdeZona(55, 45, 40));
        }

        [Test]
        public void Guardado_ConservaEsperaYContadores()
        {
            ReglaAtaques a = new ReglaAtaques();
            a.Fijar(123f);
            a.Terminar(true);
            a.Fijar(50f);
            ReglaAtaques b = new ReglaAtaques();
            Assert.IsTrue(b.Cargar(a.Guardar()));
            Assert.AreEqual(50f, b.Espera, 0.01f);
            Assert.AreEqual(1, b.Defendidos);
            Assert.IsFalse(b.Cargar("basura{"));
            b.Avanzar(60f);
            Assert.IsTrue(b.Listo);
        }
    }
}

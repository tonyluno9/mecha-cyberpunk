using MechaCyberpunk.Combate.Golpe;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Combate
{
    public class CicloGolpePruebas
    {
        private const float Preparacion = 0.25f;
        private const float Recarga = 0.8f;
        private const float Margen = 0.01f;

        private static CicloGolpe Nuevo() => new CicloGolpe(Preparacion, Recarga);

        [Test]
        public void Nuevo_EstaListoYSinProgreso()
        {
            CicloGolpe ciclo = Nuevo();
            Assert.AreEqual(FaseGolpe.Listo, ciclo.Fase);
            Assert.AreEqual(0f, ciclo.Progreso);
        }

        [Test]
        public void Avanzar_SinGolpear_NuncaDaImpacto()
        {
            CicloGolpe ciclo = Nuevo();
            Assert.IsFalse(ciclo.Avanzar(5f));
            Assert.AreEqual(FaseGolpe.Listo, ciclo.Fase);
        }

        [Test]
        public void Impacto_OcurreJustoDespuesDeLaPreparacion()
        {
            CicloGolpe ciclo = Nuevo();
            Assert.IsTrue(ciclo.IntentarGolpear());
            Assert.IsFalse(ciclo.Avanzar(Preparacion - Margen), "Todavía preparando");
            Assert.AreEqual(FaseGolpe.Preparando, ciclo.Fase);
            Assert.IsTrue(ciclo.Avanzar(2f * Margen), "Cruza el final de la preparación");
            Assert.AreEqual(FaseGolpe.Recuperando, ciclo.Fase);
        }

        [Test]
        public void Impacto_PasaUnaSolaVezPorGolpe()
        {
            CicloGolpe ciclo = Nuevo();
            ciclo.IntentarGolpear();
            int impactos = 0;
            for (int i = 0; i < 300; i++)
            {
                if (ciclo.Avanzar(0.01f))
                {
                    impactos++;
                }
            }
            Assert.AreEqual(1, impactos);
            Assert.AreEqual(FaseGolpe.Listo, ciclo.Fase);
        }

        [Test]
        public void IntentarGolpear_AntesDeTerminarLaRecarga_NoHaceNada()
        {
            CicloGolpe ciclo = Nuevo();
            ciclo.IntentarGolpear();
            Assert.IsFalse(ciclo.IntentarGolpear(), "Preparando");
            ciclo.Avanzar(Preparacion + Margen);
            Assert.IsFalse(ciclo.IntentarGolpear(), "Recuperando");
            ciclo.Avanzar(Recarga - 2f * Margen);
            Assert.IsFalse(ciclo.IntentarGolpear(), "Falta un poco de recarga");
            Assert.AreEqual(FaseGolpe.Recuperando, ciclo.Fase);
        }

        [Test]
        public void IntentarGolpear_DespuesDeLaRecarga_VuelveAFuncionar()
        {
            CicloGolpe ciclo = Nuevo();
            ciclo.IntentarGolpear();
            ciclo.Avanzar(Preparacion + Recarga + Margen);
            Assert.AreEqual(FaseGolpe.Listo, ciclo.Fase);
            Assert.IsTrue(ciclo.IntentarGolpear());
            Assert.IsTrue(ciclo.Avanzar(Preparacion + Margen), "El segundo golpe también impacta");
        }

        [Test]
        public void Avanzar_ConDtGrande_DaUnSoloImpactoYTerminaListo()
        {
            CicloGolpe ciclo = Nuevo();
            ciclo.IntentarGolpear();
            Assert.IsTrue(ciclo.Avanzar(100f), "Un solo impacto");
            Assert.AreEqual(FaseGolpe.Listo, ciclo.Fase);
            Assert.IsFalse(ciclo.Avanzar(100f), "No hay un segundo impacto");
        }

        [Test]
        public void Progreso_SubeDeCeroAUnoEnCadaFase()
        {
            CicloGolpe ciclo = Nuevo();
            ciclo.IntentarGolpear();
            ciclo.Avanzar(Preparacion / 2f);
            Assert.AreEqual(0.5f, ciclo.Progreso, 1e-4f);
            ciclo.Avanzar(Preparacion / 2f + Recarga / 4f);
            Assert.AreEqual(FaseGolpe.Recuperando, ciclo.Fase);
            Assert.AreEqual(0.25f, ciclo.Progreso, 1e-4f);
        }

        [Test]
        public void Reiniciar_CancelaElGolpeSinImpacto()
        {
            CicloGolpe ciclo = Nuevo();
            ciclo.IntentarGolpear();
            ciclo.Avanzar(Preparacion / 2f);
            ciclo.Reiniciar();
            Assert.AreEqual(FaseGolpe.Listo, ciclo.Fase);
            Assert.IsFalse(ciclo.Avanzar(1f));
        }
    }
}

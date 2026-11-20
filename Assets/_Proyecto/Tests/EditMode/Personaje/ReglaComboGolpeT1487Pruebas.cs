using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1487: buffer, ventana y reinicio del combo del golpe a pie.</summary>
    public class ReglaComboGolpeT1487Pruebas
    {
        private static ReglaComboGolpe Nueva() => new ReglaComboGolpe(3, 0.3f, 0.5f, 0.7f);

        [Test]
        public void Avanzar_ClicEnListo_ArrancaPrimerGolpe()
        {
            ReglaComboGolpe r = Nueva();
            r.RegistrarEntrada();
            Assert.IsTrue(r.Avanzar(0.016f, FaseGolpe.Listo, 0f));
            Assert.AreEqual(0, r.Paso);
        }

        [Test]
        public void Avanzar_ClicAntesDeLaVentana_EsperaYArrancaAlAbrirse()
        {
            ReglaComboGolpe r = Nueva();
            r.RegistrarEntrada();
            r.Avanzar(0.016f, FaseGolpe.Listo, 0f);
            r.RegistrarEntrada();
            Assert.IsFalse(r.Avanzar(0.05f, FaseGolpe.Recuperando, 0.2f));
            Assert.IsTrue(r.HayBuffer);
            Assert.IsTrue(r.Avanzar(0.05f, FaseGolpe.Recuperando, 0.6f));
            Assert.AreEqual(1, r.Paso);
        }

        [Test]
        public void Avanzar_BufferCaducado_NoArranca()
        {
            ReglaComboGolpe r = Nueva();
            r.RegistrarEntrada();
            r.Avanzar(0.016f, FaseGolpe.Listo, 0f);
            r.RegistrarEntrada();
            r.Avanzar(0.31f, FaseGolpe.Recuperando, 0.2f);
            Assert.IsFalse(r.HayBuffer);
            Assert.IsFalse(r.Avanzar(0.016f, FaseGolpe.Recuperando, 0.9f));
        }

        [Test]
        public void Avanzar_TercerGolpe_EsFinalYNoEncadenaDentroDeLaRecuperacion()
        {
            ReglaComboGolpe r = Nueva();
            for (int i = 0; i < 3; i++)
            {
                r.RegistrarEntrada();
                Assert.IsTrue(r.Avanzar(0.016f, i == 0 ? FaseGolpe.Listo : FaseGolpe.Recuperando, 0.9f));
            }
            Assert.IsTrue(r.EsGolpeFinal);
            r.RegistrarEntrada();
            Assert.IsFalse(r.Avanzar(0.016f, FaseGolpe.Recuperando, 0.99f));
            Assert.IsTrue(r.Avanzar(0.016f, FaseGolpe.Listo, 0f));
            Assert.AreEqual(0, r.Paso, "tras el final el combo vuelve a empezar");
        }

        [Test]
        public void Avanzar_SinGolpearTrasElReinicio_VuelveAlPrimerGolpe()
        {
            ReglaComboGolpe r = Nueva();
            r.RegistrarEntrada();
            r.Avanzar(0.016f, FaseGolpe.Listo, 0f);
            r.Avanzar(0.016f, FaseGolpe.Listo, 0f);
            r.Avanzar(0.8f, FaseGolpe.Listo, 0f);
            Assert.AreEqual(-1, r.Paso);
        }

        [Test]
        public void Avanzar_PermitirFalso_NoArrancaYConservaElBuffer()
        {
            ReglaComboGolpe r = Nueva();
            r.RegistrarEntrada();
            Assert.IsFalse(r.Avanzar(0.05f, FaseGolpe.Listo, 0f, false));
            Assert.IsTrue(r.HayBuffer);
            Assert.IsTrue(r.Avanzar(0.05f, FaseGolpe.Listo, 0f, true));
        }

        [Test]
        public void Reiniciar_ConComboYBuffer_LoLimpia()
        {
            ReglaComboGolpe r = Nueva();
            r.RegistrarEntrada();
            r.Avanzar(0.016f, FaseGolpe.Listo, 0f);
            r.RegistrarEntrada();
            r.Reiniciar();
            Assert.AreEqual(-1, r.Paso);
            Assert.IsFalse(r.HayBuffer);
            Assert.IsFalse(r.Avanzar(0.016f, FaseGolpe.Listo, 0f));
        }
    }
}

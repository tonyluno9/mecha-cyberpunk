using MechaCyberpunk.IA.Rival;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-178: la lógica pura de la habilidad del jefe (enfriamiento, aviso, furia, primer uso).</summary>
    public class ReglaHabilidadRivalPruebas
    {
        private const float Base = 7f;
        private const float Factor = 1.5f;
        private const float FactorFuria = 0.7f;
        private const float Aviso = 0.6f;
        private const float Paso = 0.1f;
        private const float Tolerancia = 0.15f;

        private static ReglaHabilidadRival Nueva() => new ReglaHabilidadRival(Base, Factor, FactorFuria, Aviso);

        // Avanza hasta que devuelva el paso pedido; devuelve los segundos que tardó, o -1 si no llegó.
        private static float HastaPaso(ReglaHabilidadRival regla, PasoHabilidadRival esperado, bool enFuria = false,
            bool puedeEmpezar = true, bool puedeSeguir = true, float limite = 60f)
        {
            float t = 0f;
            while (t < limite)
            {
                t += Paso;
                if (regla.Avanzar(Paso, enFuria, puedeEmpezar, puedeSeguir) == esperado)
                {
                    return t;
                }
            }
            return -1f;
        }

        [Test]
        public void Enfriamiento_SinFuria_EsBaseXFactor()
        {
            Assert.AreEqual(10.5f, Nueva().Enfriamiento(false), 1e-4f);
        }

        [Test]
        public void Enfriamiento_EnFuria_SeMultiplicaEncima()
        {
            Assert.AreEqual(7.35f, Nueva().Enfriamiento(true), 1e-4f);
        }

        [Test]
        public void PrimerUso_LlegaDespuesDeUnEnfriamientoCompleto()
        {
            ReglaHabilidadRival regla = Nueva();
            float t = HastaPaso(regla, PasoHabilidadRival.EmpezoAviso);
            Assert.AreEqual(10.5f, t, Tolerancia);
            Assert.IsTrue(regla.Avisando);
        }

        [Test]
        public void Aviso_DuraLosSegundosPedidos_YLuegoLanza()
        {
            ReglaHabilidadRival regla = Nueva();
            HastaPaso(regla, PasoHabilidadRival.EmpezoAviso);
            float t = HastaPaso(regla, PasoHabilidadRival.Lanzar);
            Assert.AreEqual(Aviso, t, Tolerancia);
            Assert.IsFalse(regla.Avisando);
        }

        [Test]
        public void SiguienteUso_ContandoDesdeElLanzamiento_TardaOtroEnfriamiento()
        {
            ReglaHabilidadRival regla = Nueva();
            HastaPaso(regla, PasoHabilidadRival.EmpezoAviso);
            HastaPaso(regla, PasoHabilidadRival.Lanzar);
            float t = HastaPaso(regla, PasoHabilidadRival.EmpezoAviso);
            Assert.AreEqual(10.5f, t, Tolerancia);
        }

        [Test]
        public void EnFuria_ElEnfriamientoSeAcorta()
        {
            ReglaHabilidadRival regla = Nueva();
            float t = HastaPaso(regla, PasoHabilidadRival.EmpezoAviso, enFuria: true);
            Assert.AreEqual(7.35f, t, Tolerancia);
        }

        [Test]
        public void FuriaAMitad_ElRestoCorreMasRapido()
        {
            ReglaHabilidadRival regla = Nueva();
            for (int i = 0; i < 50; i++)
            {
                regla.Avanzar(Paso, false, true, true);
            }
            // 5.0 s de 10.5: queda 52.4 %, y en furia eso son 0.524 x 7.35 = 3.85 s.
            float t = HastaPaso(regla, PasoHabilidadRival.EmpezoAviso, enFuria: true);
            Assert.AreEqual(3.85f, t, Tolerancia);
        }

        [Test]
        public void SinPoderEmpezar_EsperaListoHastaQueSePueda()
        {
            ReglaHabilidadRival regla = Nueva();
            Assert.AreEqual(-1f, HastaPaso(regla, PasoHabilidadRival.EmpezoAviso, puedeEmpezar: false, limite: 15f));
            Assert.AreEqual(1f, regla.Progreso, 1e-4f);
            Assert.AreEqual(PasoHabilidadRival.EmpezoAviso, regla.Avanzar(Paso, false, true, true), "Empieza en cuanto se puede");
        }

        [Test]
        public void SinControlDuranteElAviso_SeCortaSinLanzar_YVuelveAEmpezar()
        {
            ReglaHabilidadRival regla = Nueva();
            HastaPaso(regla, PasoHabilidadRival.EmpezoAviso);
            Assert.AreEqual(PasoHabilidadRival.Nada, regla.Avanzar(Paso, false, true, true));

            Assert.AreEqual(PasoHabilidadRival.AvisoCortado, regla.Avanzar(Paso, false, false, false));

            Assert.IsFalse(regla.Avisando);
            Assert.AreEqual(0f, regla.Progreso, 1e-4f);
            Assert.AreEqual(-1f, HastaPaso(regla, PasoHabilidadRival.Lanzar, limite: 5f), "No lanza");
        }

        [Test]
        public void Reiniciar_VuelveAEmpezarElEnfriamientoCompleto()
        {
            ReglaHabilidadRival regla = Nueva();
            for (int i = 0; i < 80; i++)
            {
                regla.Avanzar(Paso, false, true, true);
            }
            regla.Reiniciar();
            Assert.AreEqual(0f, regla.Progreso, 1e-4f);
            Assert.AreEqual(10.5f, HastaPaso(regla, PasoHabilidadRival.EmpezoAviso), Tolerancia);
        }

        [Test]
        public void EnfriamientoCero_EmpiezaDeInmediato()
        {
            ReglaHabilidadRival regla = new ReglaHabilidadRival(0f, 1f, 1f, 0.2f);
            Assert.AreEqual(PasoHabilidadRival.EmpezoAviso, regla.Avanzar(Paso, false, true, true));
        }
    }
}

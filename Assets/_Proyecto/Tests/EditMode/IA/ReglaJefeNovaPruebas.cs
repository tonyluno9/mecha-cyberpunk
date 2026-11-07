using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-693: el patrón del jefe de Nova: telegrafía, golpea, se recupera, y cada 3 golpes carga desde lejos.</summary>
    public class ReglaJefeNovaPruebas
    {
        private const float Dt = 0.05f;

        private static ReglaJefeNova Crear() => new ReglaJefeNova(2.6f, 0.8f, 1.8f, 3, 1.1f, 0.7f, 2.6f, 4f, 14f);

        private static ReglaJefeNova.Salida Avanzar(ReglaJefeNova regla, float segundos, float distancia, bool atacable = true)
        {
            ReglaJefeNova.Salida ultima = default;
            ReglaJefeNova.Salida acumulada = default;
            for (float t = 0f; t < segundos - 0.0001f; t += Dt)
            {
                ultima = regla.Avanzar(Dt, atacable, distancia);
                acumulada.Impacto |= ultima.Impacto;
                acumulada.InicioCarga |= ultima.InicioCarga;
                acumulada.Orden = ultima.Orden;
            }
            return acumulada;
        }

        [Test]
        public void Lejos_SeAcerca()
        {
            ReglaJefeNova regla = Crear();
            ReglaJefeNova.Salida s = regla.Avanzar(Dt, true, 9f);
            Assert.AreEqual(ReglaJefeNova.Orden.Acercarse, s.Orden);
            Assert.AreEqual(ReglaJefeNova.Fase.Acecho, regla.Actual);
        }

        [Test]
        public void AlAlcance_Telegrafia_Pausa_YSoloDespuesGolpea()
        {
            ReglaJefeNova regla = Crear();
            regla.Avanzar(Dt, true, 2f);
            Assert.AreEqual(ReglaJefeNova.Fase.TelegrafiaGolpe, regla.Actual);
            Assert.IsTrue(regla.Telegrafiando);
            ReglaJefeNova.Salida durante = Avanzar(regla, 0.6f, 2f);
            Assert.IsFalse(durante.Impacto, "durante la pausa no golpea");
            Assert.AreEqual(ReglaJefeNova.Orden.Quieto, durante.Orden, "la pausa es quieto");
            Assert.Greater(regla.ProgresoAviso, 0.5f);
            ReglaJefeNova.Salida fin = Avanzar(regla, 0.3f, 2f);
            Assert.IsTrue(fin.Impacto, "al terminar la telegrafía golpea");
            Assert.AreEqual(ReglaJefeNova.Fase.Recupera, regla.Actual);
        }

        private static void HastaFase(ReglaJefeNova regla, ReglaJefeNova.Fase fase, float distancia)
        {
            for (int i = 0; i < 400 && regla.Actual != fase; i++)
            {
                regla.Avanzar(Dt, true, distancia);
            }
            Assert.AreEqual(fase, regla.Actual, "llega a " + fase);
        }

        [Test]
        public void TrasElGolpe_SeRecuperaYEsVulnerable_Luego_VuelveAAcechar()
        {
            ReglaJefeNova regla = Crear();
            HastaFase(regla, ReglaJefeNova.Fase.Recupera, 2f);
            Assert.IsTrue(regla.Vulnerable, "recuperándose se le puede castigar");
            Avanzar(regla, 1.5f, 2f);
            Assert.IsTrue(regla.Vulnerable, "la recuperación dura casi 2 s");
            HastaFase(regla, ReglaJefeNova.Fase.Acecho, 2f);
            Assert.IsFalse(regla.Vulnerable);
        }

        [Test]
        public void CadaTresGolpes_YDeLejos_TelegrafiaYEmbisteEnCarga_Y_LuegoRecupera()
        {
            ReglaJefeNova regla = Crear();
            for (int i = 0; i < 3; i++)
            {
                HastaFase(regla, ReglaJefeNova.Fase.Recupera, 2f);
                HastaFase(regla, ReglaJefeNova.Fase.Acecho, 2f);
            }
            Assert.IsTrue(regla.TocaCarga);
            Assert.AreEqual(3, regla.GolpesDesdeCarga);
            regla.Avanzar(Dt, true, 2f);
            Assert.AreEqual(ReglaJefeNova.Fase.TelegrafiaGolpe, regla.Actual, "de cerca sigue golpeando: la carga necesita distancia");
            HastaFase(regla, ReglaJefeNova.Fase.Recupera, 2f);
            HastaFase(regla, ReglaJefeNova.Fase.Acecho, 2f);
            regla.Avanzar(Dt, true, 9f);
            Assert.AreEqual(ReglaJefeNova.Fase.TelegrafiaCarga, regla.Actual, "de lejos telegrafía la carga");
            ReglaJefeNova.Salida tel = Avanzar(regla, 0.9f, 9f);
            Assert.IsFalse(tel.InicioCarga, "durante la pausa no embiste");
            bool inicio = false;
            for (int i = 0; i < 40 && regla.Actual == ReglaJefeNova.Fase.TelegrafiaCarga; i++)
            {
                inicio |= regla.Avanzar(Dt, true, 9f).InicioCarga;
            }
            Assert.IsTrue(inicio, "tras la pausa embiste");
            Assert.AreEqual(ReglaJefeNova.Fase.Carga, regla.Actual);
            HastaFase(regla, ReglaJefeNova.Fase.RecuperaCarga, 9f);
            Assert.IsTrue(regla.Vulnerable);
            Assert.AreEqual(0, regla.GolpesDesdeCarga, "la carga reinicia la cuenta");
        }

        [Test]
        public void SiElJugadorYaNoEsAtacable_VuelveAAcechar()
        {
            ReglaJefeNova regla = Crear();
            Avanzar(regla, 0.4f, 2f);
            Assert.AreEqual(ReglaJefeNova.Fase.TelegrafiaGolpe, regla.Actual);
            regla.Avanzar(Dt, false, 2f);
            Assert.AreEqual(ReglaJefeNova.Fase.Acecho, regla.Actual);
        }
    }
}

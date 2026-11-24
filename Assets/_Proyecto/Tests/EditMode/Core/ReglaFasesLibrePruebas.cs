using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-historia-libre-23nov: la escalada del modo libre sube por tiempo O por bajas (lo que pase primero), una fase a la vez.</summary>
    public class ReglaFasesLibrePruebas
    {
        private static ReglaFasesLibre ReglaDeTony() => new ReglaFasesLibre(ParametrosRespuesta.FasesLibrePorOmision());

        [Test]
        public void PorOmision_SeisFases_LaUltimaSinFin()
        {
            FaseModoLibre[] fases = ParametrosRespuesta.FasesLibrePorOmision();
            Assert.AreEqual(6, fases.Length);
            Assert.AreEqual(300f, fases[0].Segundos);
            Assert.AreEqual(20, fases[0].Muertes);
            Assert.AreEqual(0f, fases[5].Segundos);
            Assert.AreEqual(0, fases[5].Muertes);
            Assert.IsFalse(fases[2].ModoNova);
            Assert.IsTrue(fases[3].ModoNova);
        }

        [Test]
        public void Avanzar_PasaElTiempoMaximo_SubeDeFase()
        {
            ReglaFasesLibre r = ReglaDeTony();
            Assert.IsFalse(r.Avanzar(299f));
            Assert.AreEqual(1, r.Nivel);
            Assert.IsTrue(r.Avanzar(1f));
            Assert.AreEqual(2, r.Nivel);
            Assert.AreEqual(0f, r.SegundosEnFase);
        }

        [Test]
        public void Avanzar_MatasVeintePoliciasAntesDelTiempo_SubeDeFase()
        {
            ReglaFasesLibre r = ReglaDeTony();
            for (int i = 0; i < 19; i++)
            {
                r.RegistrarBaja(TipoRespuesta.PoliciaAPie);
            }
            Assert.IsFalse(r.Avanzar(10f));
            r.RegistrarBaja(TipoRespuesta.PoliciaAPie);
            Assert.IsTrue(r.Avanzar(0.1f));
            Assert.AreEqual(2, r.Nivel);
        }

        [Test]
        public void RegistrarBaja_TipoQueNoCuenta_NoSube()
        {
            ReglaFasesLibre r = ReglaDeTony();
            for (int i = 0; i < 50; i++)
            {
                r.RegistrarBaja(TipoRespuesta.Patrulla); // en la fase 1 solo cuentan los policías a pie
            }
            Assert.IsFalse(r.Avanzar(1f));
            Assert.AreEqual(1, r.Nivel);
        }

        [Test]
        public void Fase2_SeisPatrullasInutilizadas_Sube()
        {
            ReglaFasesLibre r = ReglaDeTony();
            r.Iniciar(2);
            for (int i = 0; i < 6; i++)
            {
                r.RegistrarBaja(TipoRespuesta.Patrulla);
            }
            Assert.IsTrue(r.Avanzar(0.1f));
            Assert.AreEqual(3, r.Nivel);
        }

        [Test]
        public void EventoGrande_MuchasBajasDeGolpe_SoloSubeUnaFase()
        {
            ReglaFasesLibre r = ReglaDeTony();
            for (int i = 0; i < 200; i++)
            {
                r.RegistrarBaja(TipoRespuesta.PoliciaAPie); // explotó un camión lleno
            }
            Assert.IsTrue(r.Avanzar(0.1f));
            Assert.AreEqual(2, r.Nivel);
            Assert.AreEqual(0, r.MuertesDeFase, "las bajas de sobra no pasan a la fase nueva");
            Assert.IsFalse(r.Avanzar(0.1f));
            Assert.AreEqual(2, r.Nivel);
        }

        [Test]
        public void UltimaFase_NuncaTermina()
        {
            ReglaFasesLibre r = ReglaDeTony();
            r.Iniciar(6);
            for (int i = 0; i < 500; i++)
            {
                r.RegistrarBaja(TipoRespuesta.NovaHumanoide);
            }
            Assert.IsFalse(r.Avanzar(100000f));
            Assert.AreEqual(6, r.Nivel);
        }

        [Test]
        public void SegundosHastaFase_SoloTiempo_MinutosDeTony()
        {
            ReglaFasesLibre r = ReglaDeTony();
            Assert.AreEqual(0f, r.SegundosHastaFase(1));
            Assert.AreEqual(900f, r.SegundosHastaFase(4));
            Assert.AreEqual(1440f, r.SegundosHastaFase(6));
        }

        [Test]
        public void ReglaEscalada_ModoLibre_SoloTiposDeLaFaseYTope()
        {
            ParametrosRespuesta p = ParametrosRespuesta.PorOmision();
            p.TopeEnemigosLibre = 2;
            p.SegundosEntreGenerados = 0f;
            ReglaEscaladaRespuesta regla = new ReglaEscaladaRespuesta(p);
            FaseModoLibre fase = p.FasesLibre[4]; // soldados NOVA
            regla.PonerModoLibre(fase);
            int[] vivos = new int[System.Enum.GetValues(typeof(TipoRespuesta)).Length];
            regla.Avanzar(0f, 0, true, false, vivos); // misión protegida activa: en el modo libre no aplica
            Assert.IsTrue(regla.ModoNova);
            Assert.IsTrue(regla.PuedeGenerar(TipoRespuesta.NovaHumanoide, 1f));
            Assert.IsFalse(regla.PuedeGenerar(TipoRespuesta.Patrulla, 1f), "en una fase de NOVA no llega policía");
            Assert.IsFalse(regla.PuedeGenerar(TipoRespuesta.RobotGrande, 1f), "E16 llega hasta la fase 6");
            vivos[(int)TipoRespuesta.NovaHumanoide] = 2;
            regla.Avanzar(2f, 0, false, false, vivos);
            Assert.IsFalse(regla.PuedeGenerar(TipoRespuesta.RobotMediano, 3f), "tope de enemigos simultáneos");
        }
    }
}

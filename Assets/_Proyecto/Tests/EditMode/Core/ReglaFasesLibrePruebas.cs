using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-historia-libre-23nov / T99: la escalada del modo libre sube a 1 min O a 10 bajas de la fase (lo que pase primero), una fase a la vez.</summary>
    public class ReglaFasesLibrePruebas
    {
        private static ReglaFasesLibre ReglaDeTony() => new ReglaFasesLibre(ParametrosRespuesta.FasesLibrePorOmision());

        [Test]
        public void PorOmision_SeisFases_UnMinutoODiezBajas_LaUltimaSinFin()
        {
            FaseModoLibre[] fases = ParametrosRespuesta.FasesLibrePorOmision();
            Assert.AreEqual(6, fases.Length);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(60f, fases[i].Segundos, fases[i].Nombre);
                Assert.AreEqual(10, fases[i].Muertes, fases[i].Nombre);
                Assert.AreEqual(0, fases[i].MuertesAlterna, fases[i].Nombre);
                foreach (TipoRespuesta t in fases[i].Tipos)
                {
                    Assert.IsTrue(FaseModoLibre.Contiene(fases[i].TiposMuertes, t), fases[i].Nombre + ": cuenta toda baja de lo que llega en la fase (" + t + ")");
                }
            }
            Assert.AreEqual(0f, fases[5].Segundos);
            Assert.AreEqual(0, fases[5].Muertes);
            Assert.IsFalse(fases[2].ModoNova);
            Assert.IsTrue(fases[3].ModoNova);
        }

        [Test]
        public void Avanzar_UnMinutoSinBajas_SubeDeFase()
        {
            ReglaFasesLibre r = ReglaDeTony();
            Assert.IsFalse(r.Avanzar(59f));
            Assert.AreEqual(1, r.Nivel);
            Assert.IsTrue(r.Avanzar(1f));
            Assert.AreEqual(2, r.Nivel);
            Assert.AreEqual(0f, r.SegundosEnFase);
        }

        [Test]
        public void Avanzar_DiezBajasAntesDelMinuto_SubeDeFase()
        {
            ReglaFasesLibre r = ReglaDeTony();
            for (int i = 0; i < 9; i++)
            {
                r.RegistrarBaja(i % 2 == 0 ? TipoRespuesta.PoliciaAPie : TipoRespuesta.Patrulla);
            }
            Assert.IsFalse(r.Avanzar(10f));
            r.RegistrarBaja(TipoRespuesta.PoliciaAPie);
            Assert.IsTrue(r.Avanzar(0.1f));
            Assert.AreEqual(2, r.Nivel);
        }

        [Test]
        public void RegistrarBaja_TipoQueNoEsDeLaFase_NoSube()
        {
            ReglaFasesLibre r = ReglaDeTony();
            r.Iniciar(4); // drones de NOVA
            for (int i = 0; i < 50; i++)
            {
                r.RegistrarBaja(TipoRespuesta.PoliciaAPie); // un policía rezagado no es de esta fase
            }
            Assert.IsFalse(r.Avanzar(1f));
            Assert.AreEqual(4, r.Nivel);
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
        public void Avanzar_PasoDeTiempoEnorme_SoloSubeUnaFase()
        {
            ReglaFasesLibre r = ReglaDeTony();
            Assert.IsTrue(r.Avanzar(1000f));
            Assert.AreEqual(2, r.Nivel);
            Assert.AreEqual(0f, r.SegundosEnFase);
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
        public void SegundosHastaFase_SoloTiempo_UnMinutoPorFase()
        {
            ReglaFasesLibre r = ReglaDeTony();
            Assert.AreEqual(0f, r.SegundosHastaFase(1));
            Assert.AreEqual(180f, r.SegundosHastaFase(4));
            Assert.AreEqual(300f, r.SegundosHastaFase(6));
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

using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1490: casi sin temblor de cámara, combo de 2 golpes y puños más flojos contra enemigos.</summary>
    public class TemblorComboT1490Pruebas
    {
        [Test]
        public void ConfigCamara_TemblorYBalanceoSonUnaFraccionDeLoAnterior()
        {
            ConfigCamaraHombro c = ScriptableObject.CreateInstance<ConfigCamaraHombro>();
            Assert.LessOrEqual(c.GananciaTemblor, 0.2f);
            Assert.GreaterOrEqual(c.GananciaTemblor, 0.15f);
            Assert.LessOrEqual(c.SacudidaZoom, 0.3f);
            Assert.LessOrEqual(c.BalanceoVertical, 0.0176f);
            Assert.LessOrEqual(c.BalanceoLateral, 0.0101f);
            Object.DestroyImmediate(c);
        }

        [Test]
        public void Patada_LasArmasPatean20PorCientoDeLoAnterior()
        {
            AjusteSensacion[] a = DatosSensacion.AjustesPorArma();
            Assert.LessOrEqual(a[(int)ArmaAPie.Sniper].patada, 1f);
            Assert.LessOrEqual(a[(int)ArmaAPie.Escopeta].patada, 0.7f);
            Assert.Greater(a[(int)ArmaAPie.Escopeta].patada, a[(int)ArmaAPie.Pistola].patada);
        }

        [Test]
        public void ConfigGolpe_ElComboEsDeDosGolpesComoMaximo()
        {
            Assert.AreEqual(2, ConfigGolpeAPie.PorDefecto.GolpesCombo);
        }

        [Test]
        public void Combo_DosGolpes_NingunoEsFinalYElCicloVuelveAlPrimero()
        {
            ReglaComboGolpe r = new ReglaComboGolpe(2, 0.3f, 0.5f, 0.7f);
            Assert.IsFalse(r.TieneGolpeFinal);
            r.RegistrarEntrada();
            Assert.IsTrue(r.Avanzar(0.016f, FaseGolpe.Listo, 0f));
            Assert.AreEqual(0, r.Paso);
            Assert.IsFalse(r.EsGolpeFinal);
            r.RegistrarEntrada();
            Assert.IsTrue(r.Avanzar(0.016f, FaseGolpe.Recuperando, 0.9f));
            Assert.AreEqual(1, r.Paso);
            Assert.IsFalse(r.EsGolpeFinal, "el segundo golpe no usa GolpeFuerte");
            r.RegistrarEntrada();
            Assert.IsTrue(r.Avanzar(0.016f, FaseGolpe.Recuperando, 0.9f));
            Assert.AreEqual(0, r.Paso, "tras el segundo vuelve al primero");
        }

        [Test]
        public void Punos_ContraEnemigoPegaElFactorYContraOtrosNo()
        {
            Assert.AreEqual(9f, ReglaDanioPunos.Danio(20f, 0.45f, true), 1e-4f);
            Assert.AreEqual(20f, ReglaDanioPunos.Danio(20f, 0.45f, false), 1e-4f);
        }

        [Test]
        public void Punos_UnPandilleroDe60DeVidaAguantaEntre6Y8Golpes()
        {
            float danio = ReglaDanioPunos.Danio(20f, ConfigGolpeAPie.PorDefecto.MultiplicadorPunosContraEnemigos, true);
            int golpes = ReglaDanioPunos.GolpesParaDerribar(60f, danio);
            Assert.GreaterOrEqual(golpes, 6);
            Assert.LessOrEqual(golpes, 8);
            Assert.AreEqual(3, ReglaDanioPunos.GolpesParaDerribar(60f, 20f), "antes eran 3");
        }
    }
}

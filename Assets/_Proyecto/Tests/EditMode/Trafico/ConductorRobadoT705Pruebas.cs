using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-705: tiempos y radios del conductor al que le roban el carro (sin Unity en juego).</summary>
    public class ConductorRobadoT705Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";

        [Test]
        public void Tiempos_PrimeroLoSacanLuegoCaeYAlFinalSeLevanta()
        {
            Assert.Greater(ConductorRobado.SegundosSacar, 0f);
            Assert.Less(ConductorRobado.SegundosSacar, ConductorRobado.SegundosCaido, "Cae mientras lo sacan y sigue tirado un momento");
            Assert.Greater(ConductorRobado.SegundosLevantarse, 0.5f);
        }

        [Test]
        public void LlamadaCivil_DaTiempoDeLevantarseYLlamar_YElConductorPierdeDeVistaMasCerca()
        {
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig);
            Assert.IsNotNull(config);
            float enPie = ConductorRobado.SegundosCaido + ConductorRobado.SegundosLevantarse;
            Assert.Greater(config.SegundosLlamadaCivil, enPie, "La llamada termina cuando ya se levantó");
            Assert.Less(CarroTrafico.RadioAbandonoConductor, config.RadioAbandonoCivil, "Si el jugador se aleja rápido, el conductor no alcanza a llamar");
            Assert.Less(CarroTrafico.RadioAbandonoConductor, config.RadioVistaCivil);
        }
    }
}

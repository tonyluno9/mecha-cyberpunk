using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1521: todos los humanos a la altura del jugador. La tabla es lógica pura; la altura de los prefabs lee lo ya generado con
    /// EscalaHumanosT1521.Aplicar (corre antes, con -executeMethod): mide la malla animada en Idle, no los bounds.
    /// </summary>
    public class EscalaHumanosT1521Pruebas
    {
        private static string[] Prefabs => EscalasHumanos.Prefabs;

        private static readonly string[] NoSeEscalan =
        {
            "Visual_E15_CentinelaNova_a", "Visual_E15_CentinelaNova_b", "Visual_E16_RobotPesadoNova_a", "Visual_E16_RobotPesadoNova_b",
            "Visual_E18_EliteNova_a", "Visual_E17_PerroRobotNova_a", "Visual_E17_PerroRobotNova_esc", "Visual_GuardiaNova",
            "Visual_E11_JefeSnakes", "Visual_E12_GuardiaPesadoNova", "Visual_E14_JefeFinalNova", "Visual_PJ01",
        };

        [Test]
        public void Tabla_TodosEstanEntreLosEnanosYLosGigantes()
        {
            Assert.GreaterOrEqual(Prefabs.Length, 30, "doctor, inventor, hacker, mecánico, meseras, civiles NP07-NP14, man/woman/woman2, pandilleros y policía");
            foreach (string visual in Prefabs)
            {
                Assert.That(EscalasHumanos.Objetivo(visual), Is.InRange(EscalasHumanos.AlturaMinima, EscalasHumanos.AlturaMaxima), visual);
            }
        }

        [Test]
        public void Tabla_ElDoc_NoEsMasAltoQueElJugador()
        {
            Assert.LessOrEqual(EscalasHumanos.Objetivo("Visual_Doctor"), EscalasHumanos.AlturaJugador + 0.0001f);
            Assert.AreEqual(EscalasHumanos.Objetivo("Visual_Doctor"), EscalasHumanos.Objetivo("Doctor"), 0.0001f, "acepta el nombre con o sin prefijo");
        }

        [Test]
        public void Tabla_RobotsGigantesJefesYJugador_NoEstan()
        {
            foreach (string visual in NoSeEscalan)
            {
                Assert.AreEqual(0f, EscalasHumanos.Objetivo(visual), visual + " conserva su tamaño de diseño");
            }
        }

        [Test]
        public void Tabla_LosCivilesDeLaCiudad_SeParecenEntreSi()
        {
            float menor = float.MaxValue;
            float mayor = float.MinValue;
            foreach (string visual in Prefabs)
            {
                if (!visual.StartsWith("Visual_NP"))
                {
                    continue;
                }
                menor = Mathf.Min(menor, EscalasHumanos.Objetivo(visual));
                mayor = Mathf.Max(mayor, EscalasHumanos.Objetivo(visual));
            }
            Assert.LessOrEqual(mayor - menor, 0.2f, "civiles NP07-NP14: de la abuela al estibador no pasan de 20 cm");
        }

        [TestCase(1.2f, 1.8f, 1.5f)]
        [TestCase(1.8f, 1.8f, 1f)]
        [TestCase(2.0f, 1.76f, 0.88f)]
        public void Factor_LlevaLaMedidaAlObjetivo(float medida, float objetivo, float esperado)
        {
            Assert.AreEqual(esperado, EscalasHumanos.FactorCorreccion(medida, objetivo), 0.0001f);
        }

        [TestCase(0.1f, 1.8f)]
        [TestCase(0.9f, 0f)]
        [TestCase(0.8f, 1.8f)]
        [TestCase(3.8f, 1.8f)]
        public void Factor_MedidaNoConfiable_NoTocaNada(float medida, float objetivo)
        {
            Assert.AreEqual(1f, EscalasHumanos.FactorCorreccion(medida, objetivo), 0.0001f);
        }

        [TestCaseSource(nameof(Prefabs))]
        public void Prefab_AnimadoEnIdle_MideSuAlturaObjetivo(string visual)
        {
            EscalaHumanosT1521.Medida m = EscalaHumanosT1521.MedirPrefab(EscalaHumanosT1521.CarpetaVisuales + visual + ".prefab");
            Assert.IsTrue(m.Valida, visual + ": " + m.Problema);
            Assert.AreEqual(EscalasHumanos.Objetivo(visual), m.Malla, EscalasHumanos.ToleranciaPrueba, visual + " (malla animada en Idle; corre EscalaHumanosT1521.Aplicar si falla)");
        }

        [TestCaseSource(nameof(Prefabs))]
        public void Prefab_EnIdle_LaCabezaNoEstaAcostada(string visual)
        {
            EscalaHumanosT1521.Medida m = EscalaHumanosT1521.MedirPrefab(EscalaHumanosT1521.CarpetaVisuales + visual + ".prefab");
            Assert.IsTrue(m.Valida, visual);
            Assert.Greater(m.Cabeza, EscalaHumanosT1521.AlturaCabezaIdleMinima, visual + ": en Idle la cabeza queda baja (avatar roto o idle acostado)");
        }

        [TestCaseSource(nameof(Prefabs))]
        public void Prefab_Modelo_EsUniformeYLaRaizNoSeEscala(string visual)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EscalaHumanosT1521.CarpetaVisuales + visual + ".prefab");
            Transform modelo = prefab.transform.Find("Giro/Modelo");
            Assert.IsNotNull(modelo, visual);
            Assert.AreEqual(modelo.localScale.x, modelo.localScale.y, 0.0001f, visual + " escala uniforme");
            Assert.AreEqual(modelo.localScale.x, modelo.localScale.z, 0.0001f, visual + " escala uniforme");
            Assert.AreEqual(Vector3.one, prefab.transform.localScale, visual + ": la raíz no se escala");
        }
    }
}

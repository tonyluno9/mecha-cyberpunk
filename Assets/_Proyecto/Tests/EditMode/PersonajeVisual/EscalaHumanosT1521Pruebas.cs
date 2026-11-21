using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1521 + T-1552: las alturas de los personajes (tabla de Tony del 20-nov en EscalasHumanos). La tabla es lógica pura; la altura de los prefabs lee lo ya generado con
    /// AlturasT1552.Aplicar (corre antes, con -executeMethod): mide la malla animada en Idle, no los bounds.
    /// </summary>
    public class EscalaHumanosT1521Pruebas
    {
        private static string[] Prefabs => EscalasHumanos.Prefabs;

        /// <summary>T-1552: el jugador y el perro no cambian de altura y no están en la tabla (los robots de Nova, los jefes y los pesados ya sí).</summary>
        private static readonly string[] NoSeEscalan = { "Visual_E17_PerroRobotNova_a", "Visual_E17_PerroRobotNova_esc", "Visual_PJ01" };

        /// <summary>
        /// Los que se miden animados con el clip humanoide de pie. Sin el soldado azul E18: su avatar y sus clips los rehace T-1553 (hasta entonces no hay medida animada fiable).
        /// </summary>
        private static string[] PrefabsMedibles => EscalasHumanos.Prefabs.Where(v => v != "Visual_E18_EliteNova_a").ToArray();

        /// <summary>
        /// Para la altura animada en Idle con PJ01_Loco_Idle: sin los robots de Nova (E15, E16, E18). En juego no usan ese idle (tienen sus propios clips) y retargeteado
        /// en su esqueleto da 2.1 a 4.3 m; su altura buena es la de bind, que comprueba AlturasT1552Pruebas.
        /// </summary>
        private static string[] PrefabsIdle => PrefabsMedibles.Where(v => !v.StartsWith("Visual_E15_") && !v.StartsWith("Visual_E16_")).ToArray();

        [Test]
        public void Tabla_TodosEstanEntreLaAbuelaYLosRobotsGrandes()
        {
            Assert.GreaterOrEqual(Prefabs.Length, 41, "doctor, inventor, hacker, mecánico, meseras, civiles NP07-NP14, man/woman/woman2, pandilleros, policía, AL10/AL11 y los robots de Nova");
            foreach (string visual in Prefabs)
            {
                Assert.That(EscalasHumanos.Objetivo(visual), Is.InRange(EscalasHumanos.AlturaMinima, EscalasHumanos.AlturaMaxima), visual);
            }
        }

        [Test]
        public void Tabla_ElDoc_NoPasaDelJugadorPorMasDe5cm()
        {
            Assert.LessOrEqual(EscalasHumanos.Objetivo("Visual_Doctor"), EscalasHumanos.AlturaJugador + 0.05f);
            Assert.AreEqual(EscalasHumanos.Objetivo("Visual_Doctor"), EscalasHumanos.Objetivo("Doctor"), 0.0001f, "acepta el nombre con o sin prefijo");
        }

        [Test]
        public void Tabla_JugadorYPerro_NoEstan()
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
            Assert.LessOrEqual(mayor - menor, 0.3f, "civiles NP07-NP14: de la abuela (1.64) al estibador (1.90) no pasan de 30 cm (decisión de Tony, 20-nov)");
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

        [TestCaseSource(nameof(PrefabsIdle))]
        public void Prefab_AnimadoEnIdle_MideSuAlturaObjetivo(string visual)
        {
            EscalaHumanosT1521.Medida m = EscalaHumanosT1521.MedirPrefab(EscalaHumanosT1521.CarpetaVisuales + visual + ".prefab");
            Assert.IsTrue(m.Valida, visual + ": " + m.Problema);
            float esperada = EscalasHumanos.Objetivo(visual) * EscalasHumanos.AlturaJugadorAnimada / EscalasHumanos.AlturaJugador; // la tabla es de Blender (PJ01 1.84); animado, PJ01 mide 1.80
            Assert.AreEqual(esperada, m.Malla, EscalasHumanos.TolerarPrueba(visual), visual + " (malla animada en Idle, proporcional a PJ01; corre AlturasT1552.Aplicar si falla)");
        }

        [TestCaseSource(nameof(PrefabsMedibles))]
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

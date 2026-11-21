using System.Linq;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1553: los prefabs de enemigos de Nova quedan como pidió Tony el 20-nov. Solo el E16 dispara el cañón y camina con escudo (no el E12), el E18 lleva su conducta de vuelo y
    /// puñetazos y ya no lleva la W10. Necesita haber corrido RobotsNovaT1553.Aplicar.
    /// </summary>
    public class RobotsNovaT1553PrefabsPruebas
    {
        private const string Carpeta = "Assets/_Proyecto/IA/Prefabs/";
        private const string NombreSocket = "ManoDer";
        private const string NombreArma = "W10";

        private static GameObject Prefab(string nombre)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + nombre + ".prefab");
            Assert.IsNotNull(p, nombre);
            return p;
        }

        private static Transform Arma(GameObject prefab)
        {
            Transform mano = prefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == NombreSocket);
            return mano != null ? mano.Find(NombreArma) : null;
        }

        [TestCase("GuardiaPesado_E16_RobotPesadoNova_a")]
        [TestCase("GuardiaPesado_E16_RobotPesadoNova_b")]
        public void E16_DisparaElCanon_YCaminaConEscudo(string nombre)
        {
            ConductaGuardiaPesado c = Prefab(nombre).GetComponent<ConductaGuardiaPesado>();
            Assert.IsNotNull(c, nombre + " usa ConductaGuardiaPesado");
            Assert.IsTrue(c.DisparaCanon, nombre + ": fase de disparo del cañón (corre RobotsNovaT1553.Aplicar)");
            Assert.IsTrue(c.CaminaConEscudo, nombre + ": camina con el escudo arriba (corre RobotsNovaT1553.Aplicar)");
        }

        [Test]
        public void E12_NoDisparaCanon_NiCaminaConEscudo_SoloElE16()
        {
            ConductaGuardiaPesado c = Prefab("GuardiaPesado_E12").GetComponent<ConductaGuardiaPesado>();
            Assert.IsNotNull(c);
            Assert.IsFalse(c.DisparaCanon, "el cañón es solo del E16");
            Assert.IsFalse(c.CaminaConEscudo, "caminar con escudo es solo del E16");
        }

        [Test]
        public void E18_TieneSuConductaDeVueloYPunos_YNoLlevaArma()
        {
            GameObject e18 = Prefab("GuardiaNova_E18_EliteNova_a");
            Assert.IsNotNull(e18.GetComponent<ConductaEliteNova>(), "ConductaEliteNova (corre RobotsNovaT1553.Aplicar)");
            Assert.IsNull(Arma(e18), "decisión de Tony 20-nov: el E18 no lleva arma");
            Assert.AreEqual(1, e18.GetComponents<MonoBehaviour>().Count(m => m is IConductaAPie), "una sola conducta");
        }

        [Test]
        public void E15_SigueConLaW10()
        {
            Assert.IsNotNull(Arma(Prefab("GuardiaNova_E15_CentinelaNova_a")), "el E15 conserva su rifle W10");
        }
    }
}

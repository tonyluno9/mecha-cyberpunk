using System.Collections;
using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-501 en juego: el posproceso local de Suburbios solo pesa de noche y las torres de horizonte se apagan de día.</summary>
    public class SuburbiosT501PlayPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float HoraNoche = 23f;
        private const float HoraDia = 13f;
        private const float Tolerancia = 0.01f;
        private const int LucesMaximas = 30;

        private RelojMundo reloj;
        private CicloDiaNoche ciclo;
        private VolumenSigueNoche sigue;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            reloj = Object.FindFirstObjectByType<RelojMundo>();
            ciclo = Object.FindFirstObjectByType<CicloDiaNoche>();
            sigue = Object.FindFirstObjectByType<VolumenSigueNoche>();
            Assert.IsNotNull(reloj);
            Assert.IsNotNull(ciclo);
            Assert.IsNotNull(sigue, "Ciudad_Prologo debe traer el volumen local de Suburbios");
            reloj.Pausar(true);
        }

        [UnityTest]
        public IEnumerator DeNoche_VolumenLocalPesaComoElDeNoche()
        {
            reloj.AvanzarHasta(HoraNoche);
            ciclo.Aplicar(HoraNoche);
            yield return null;
            yield return null;
            Volume local = sigue.GetComponent<Volume>();
            Assert.AreEqual(ciclo.VolumeNoche.weight, local.weight, Tolerancia);
            Assert.Greater(local.weight, 0.9f);
            Assert.Greater(Shader.GetGlobalFloat("_T501Noche"), 0.9f);
        }

        [UnityTest]
        public IEnumerator DeDia_VolumenLocalYTorresSeApagan()
        {
            reloj.AvanzarHasta(HoraDia);
            ciclo.Aplicar(HoraDia);
            yield return null;
            yield return null;
            Assert.Less(sigue.GetComponent<Volume>().weight, 0.1f);
            Assert.Less(Shader.GetGlobalFloat("_T501Noche"), 0.1f);
        }

        [UnityTest]
        public IEnumerator Luces_DeLaRaizNoPasanElPresupuesto()
        {
            yield return null;
            GameObject raiz = GameObject.Find("T501_Suburbios");
            Assert.IsNotNull(raiz);
            Assert.LessOrEqual(raiz.GetComponentsInChildren<Light>().Count(l => l.enabled), LucesMaximas);
        }
    }
}

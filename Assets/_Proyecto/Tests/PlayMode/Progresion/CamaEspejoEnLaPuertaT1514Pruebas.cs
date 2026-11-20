using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Prologo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-1514: sin cuarto, dormir/guardar y el espejo se ofrecen junto a la puerta de la casa, activos y sin modelo visible.</summary>
    public class CamaEspejoEnLaPuertaT1514Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float DistanciaMaxima = 2.5f; // el alcance de E (IInteractuable)

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator CamaYEspejo_EstanActivosJuntoALaPuertaDeLaCasa_SinModelo()
        {
            GameObject puerta = GameObject.Find("Casa_Puerta");
            Assert.IsNotNull(puerta, "hay aparición Casa_Puerta");
            CamaJugador cama = Object.FindFirstObjectByType<CamaJugador>();
            EspejoJugador espejo = Object.FindFirstObjectByType<EspejoJugador>();
            Assert.IsNotNull(cama, "hay cama (dormir y guardar)");
            Assert.IsNotNull(espejo, "hay espejo (editar personaje)");
            foreach (MonoBehaviour m in new MonoBehaviour[] { cama, espejo })
            {
                Assert.IsTrue(m.gameObject.activeInHierarchy, m.name + " activo (ya no cuelga del cuarto apagado)");
                Assert.LessOrEqual(Vector3.Distance(m.transform.position, puerta.transform.position), DistanciaMaxima, m.name + " junto a la puerta");
                Assert.IsFalse(m.GetComponentsInChildren<MeshRenderer>().Any(r => r.enabled && r.GetComponentInParent<Canvas>() == null), m.name + " sin modelo visible");
            }
            Assert.AreNotEqual(((IInteractuable)cama).Posicion, ((IInteractuable)espejo).Posicion, "dos interacciones distintas");
            yield return null;
        }
    }
}

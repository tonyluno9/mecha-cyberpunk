using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Ciudad.unity y Pruebas_Alan.unity: al dar Play traen el barrio, se ven y no dejan errores en consola.</summary>
    public class EscenasMundoPruebas
    {
        private const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad.unity";
        private const string RutaPruebas = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";
        private const float DistanciaMinimaJaula = 100f;

        private static IEnumerator Cargar(string ruta)
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        private static void VaciarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(raiz);
            }
        }

        [UnityTest]
        public IEnumerator Ciudad_AlDarPlay_TieneElBarrioLaNocheYNingunError()
        {
            yield return Cargar(RutaCiudad);
            Assert.IsNotNull(GameObject.Find("Barrio"));
            Assert.IsNotNull(GameObject.Find("Manzana_6"));
            Assert.AreEqual(0, Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, "Ciudad no lleva cámara de pruebas");
            Assert.IsTrue(RenderSettings.fog, "La niebla debe estar activa");
            Light luz = GameObject.Find("LuzNoche").GetComponent<Light>();
            Assert.AreEqual(LightType.Directional, luz.type);
            LogAssert.NoUnexpectedReceived();
            VaciarEscena();
        }

        [UnityTest]
        public IEnumerator PruebasAlan_AlDarPlay_TieneBarrioCamaraLibreYJaulaLejos()
        {
            yield return Cargar(RutaPruebas);
            Assert.IsNotNull(GameObject.Find("Barrio"));
            Assert.IsNull(GameObject.Find("CamaraVistaArena"), "La cámara fija de T-020 debe haberse quitado");
            CamaraLibre camara = Object.FindFirstObjectByType<CamaraLibre>();
            Assert.IsNotNull(camara);
            Assert.AreSame(camara, Registro<IJugador>.Actual);

            GameObject jaula = GameObject.Find("JaulaClandestino");
            Assert.IsNotNull(jaula, "Falta la jaula");
            Vector3 centroBarrio = GameObject.Find("Barrio").transform.position;
            Assert.Greater(Vector3.Distance(jaula.transform.position, centroBarrio), DistanciaMinimaJaula + 150f, "La jaula debe estar fuera del barrio, a más de 100 m");

            Vector3 puerta = GameObject.Find("Lugar_Casa").transform.Find("Entrada").position;
            Assert.Less(Vector3.Distance(camara.transform.position, puerta), 15f, "La cámara debe empezar frente a la casa");
            LogAssert.NoUnexpectedReceived();
            VaciarEscena();
        }
    }
}

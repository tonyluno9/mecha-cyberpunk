using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-532 (plan de la noche 3-nov, paso 1): cada carro dice su modelo, las motos de la calle se manejan, el tráfico civil no lleva
    /// patrullas, la patrulla de la persecución es el modelo V05 y el carro frena firme.
    /// </summary>
    public class CarrosT532Pruebas
    {
        private static string Nombre(GameObject g)
        {
            Component c = g.GetComponent("CarroViejo");
            return new SerializedObject(c).FindProperty("nombreModelo").stringValue;
        }

        [Test]
        public void Carros_DicenSuModelo()
        {
            Assert.AreEqual("Sedán viejo", Nombre(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_V02.prefab")));
            Assert.AreEqual("Patrulla", Nombre(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_V05.prefab")));
            Assert.AreEqual("Moto", Nombre(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_C19.prefab")));
            Assert.AreEqual("Carro deportivo", Nombre(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V01b.prefab")));
        }

        [Test]
        public void Manejo_FrenoFirmeYSeDetieneSolo()
        {
            SerializedObject so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/_Proyecto/Vehiculos/Datos/ConfigCarro_Viejo.asset"));
            float masa = so.FindProperty("masa").floatValue;
            Assert.GreaterOrEqual(so.FindProperty("fuerzaFreno").floatValue / masa, 10f, "Frena a 10 m/s² o más");
            Assert.GreaterOrEqual(so.FindProperty("resistenciaRodadura").floatValue / masa, 0.8f, "Sin acelerar se detiene en segundos, no se resbala");
        }

        [Test]
        public void Patrulla_ConElModeloV05_SinLosCubos()
        {
            GameObject patrulla = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab");
            // T-707: la patrulla es el carro V05 de Vehiculos (su modelo va en Visual_T511); ya no lleva el cuerpo de cubos.
            Assert.IsNotNull(patrulla.transform.Find("Visual_T511"), "Modelo nuevo");
            Renderer cubos = patrulla.transform.Find("Carroceria").GetComponent<Renderer>();
            Assert.IsFalse(cubos != null && cubos.enabled, "Sin la carrocería de cubos");
        }

        [Test]
        public void Ciudad_MotosManejables_YTraficoSinPatrullas()
        {
            Scene escena = EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", OpenSceneMode.Additive);
            try
            {
                Transform[] todos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).ToArray();
                Assert.AreEqual(0, todos.Count(t => t.name.StartsWith("C19_Moto")), "Sin motos de adorno");
                Assert.GreaterOrEqual(todos.Count(t => t.name.StartsWith("MotoRobable_") && t.GetComponent("CarroEstacionado") != null), 5, "Motos que se manejan y se roban");
                Component trafico = todos.Select(t => t.GetComponent("TraficoCalle")).First(c => c != null);
                SerializedProperty lista = new SerializedObject(trafico).FindProperty("prefabsCarro");
                for (int i = 0; i < lista.arraySize; i++)
                {
                    string n = lista.GetArrayElementAtIndex(i).objectReferenceValue.name;
                    Assert.IsFalse(n.EndsWith("_V05") || n.EndsWith("_V06") || n.EndsWith("_V08"), n + " es de la policía");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1331: V01a, V01b, V05 y la patrulla llevan la rueda de Blender (malla de V09), con el eje en X y del tamaño de su hueco.</summary>
    public class RuedasBlenderT1331Pruebas
    {
        private static readonly string[] Ruedas = { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" };

        private static string[] Rutas() => CreadorRuedasT1331.Prefabs();

        [Test]
        public void Prefabs_LlevanLaRuedaDeBlender_ConEjeEnX([ValueSource(nameof(Rutas))] string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, ruta);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorRuedasT1331.RutaFbxRueda);
            Assert.IsNotNull(fbx, "FBX de la rueda");
            Assert.IsNotNull(prefab.GetComponent<RuedasVisualesCarro>(), ruta);
            foreach (string nombre in Ruedas)
            {
                Transform giro = prefab.transform.Find(nombre + "/Direccion/Giro");
                Assert.IsNotNull(giro, ruta + " " + nombre);
                Transform modelo = giro.Find("Modelo");
                Assert.IsNotNull(modelo, ruta + " " + nombre + " Modelo");
                Assert.AreSame(fbx.transform.Find(nombre).GetComponent<MeshFilter>().sharedMesh, modelo.GetComponent<MeshFilter>().sharedMesh, ruta + " " + nombre + ": malla de Blender");
                Bounds b = CreadorCarrosBlenderT1312.LimitesEn(modelo.gameObject, giro);
                Assert.Less(b.size.x, b.size.y, ruta + " " + nombre + ": el eje de la rueda es X");
                Assert.Less(b.size.x, b.size.z, ruta + " " + nombre + ": el eje de la rueda es X");
                Assert.Greater(b.size.y * 0.5f, 0.2f, ruta + " " + nombre + ": radio razonable");
                Assert.Less(b.size.y * 0.5f, 0.5f, ruta + " " + nombre + ": radio razonable");
                Assert.AreEqual(0f, b.center.y, 0.01f, ruta + " " + nombre + ": centrada en el pivote");
            }
        }
    }
}

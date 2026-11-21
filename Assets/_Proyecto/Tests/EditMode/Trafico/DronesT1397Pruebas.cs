using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Trafico.Drones;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1397: prefabs de drones y naves con rotores que giran, el dron de la policía con DR01 y los 11 carros con puertas y cofre.</summary>
    public class DronesT1397Pruebas
    {
        private static readonly string[] Modelos = DronesT1397.Modelos;

        [Test]
        public void EjeDelDisco_EsElEjeMasDelgadoDeLaMalla()
        {
            // T-1554: rotores de Blender (Z arriba) tienen el disco en XY; girar sobre Y los hacía bambolear.
            Assert.AreEqual(Vector3.forward, CalculoGiroRotores.EjeDelDisco(new Vector3(0.4f, 0.4f, 0.02f)));
            Assert.AreEqual(Vector3.up, CalculoGiroRotores.EjeDelDisco(new Vector3(0.4f, 0.02f, 0.4f)));
            Assert.AreEqual(Vector3.right, CalculoGiroRotores.EjeDelDisco(new Vector3(0.02f, 0.4f, 0.4f)));
        }

        [Test]
        public void CadaPrefab_TieneGiroRotoresYAlMenosUnRotor([ValueSource(nameof(Modelos))] string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DronesT1397.RutaPrefab(codigo));
            Assert.IsNotNull(prefab, "falta " + DronesT1397.RutaPrefab(codigo));
            Assert.IsNotNull(prefab.GetComponent<GiroRotores>(), codigo + " sin GiroRotores");
            int rotores = prefab.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith(GiroRotores.PrefijoRotor));
            Assert.GreaterOrEqual(rotores, 1, codigo + " sin Rotor_*");
        }

        [Test]
        public void GiroRotores_GiraElRotorSobreSuYLocal()
        {
            GameObject raiz = new GameObject("Dron");
            GameObject rotor = new GameObject("Rotor_1");
            rotor.transform.SetParent(raiz.transform, false);
            GiroRotores giro = raiz.AddComponent<GiroRotores>();
            giro.Buscar();
            Assert.AreEqual(1, giro.CantidadRotores);
            giro.Girar(0.5f / giro.Velocidad * 90f);
            Assert.AreEqual(45f, rotor.transform.localEulerAngles.y, 0.01f);
            Assert.AreEqual(0f, rotor.transform.localEulerAngles.x, 0.01f);
            Object.DestroyImmediate(raiz);
        }

        [Test]
        public void GradosEnPaso_MultiplicaVelocidadPorTiempo()
        {
            Assert.AreEqual(90f, CalculoGiroRotores.GradosEnPaso(180f, 0.5f, 0, false), 0.001f);
        }

        [Test]
        public void GradosEnPaso_AlternarInvierteLosImpares()
        {
            Assert.AreEqual(90f, CalculoGiroRotores.GradosEnPaso(180f, 0.5f, 0, true), 0.001f);
            Assert.AreEqual(-90f, CalculoGiroRotores.GradosEnPaso(180f, 0.5f, 1, true), 0.001f);
            Assert.AreEqual(90f, CalculoGiroRotores.GradosEnPaso(180f, 0.5f, 2, true), 0.001f);
        }

        [Test]
        public void GradosEnPaso_SinTiempoNoGira()
        {
            Assert.AreEqual(0f, CalculoGiroRotores.GradosEnPaso(1800f, 0f, 3, true), 0.0001f);
        }

        [Test]
        public void DronPolicia_ConservaDronMundoYTieneDR01ConRotores()
        {
            GameObject dron = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/Dron.prefab");
            Assert.IsNotNull(dron.GetComponent<MechaCyberpunk.Trafico.Persecucion.DronMundo>(), "perdió DronMundo");
            Transform visual = dron.transform.Find(DronesT1397.NombreVisualNuevo);
            Assert.IsNotNull(visual, "falta " + DronesT1397.NombreVisualNuevo);
            Assert.IsNotNull(visual.GetComponent<GiroRotores>());
            Assert.GreaterOrEqual(visual.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith(GiroRotores.PrefijoRotor)), 1);
        }

        [Test]
        public void CarrosArreglados_TienenPuertasYCofre([ValueSource(typeof(CarrosBlenderT1397), nameof(CarrosBlenderT1397.Todos))] string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
            if (prefab == null)
            {
                prefab = AssetDatabase.FindAssets("Carro_" + codigo + " t:Prefab").Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(p => p != null && p.name == "Carro_" + codigo);
            }
            Assert.IsNotNull(prefab, "falta Carro_" + codigo);
            string[] nombres = prefab.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray();
            Assert.Contains("Puerta_I", nombres, codigo);
            Assert.Contains("Puerta_D", nombres, codigo);
            Assert.Contains("Cofre", nombres, codigo);
            Assert.IsNotNull(prefab.GetComponent<PuertasCarro>(), codigo + " sin PuertasCarro");
        }
    }
}

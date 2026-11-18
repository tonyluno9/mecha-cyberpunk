using System.Linq;
using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1343: carros viejos y motos con los FBX de Blender: cristales transparentes, tamaño conservado, bisagras y ruedas que giran.</summary>
    public class CarrosT1343Pruebas
    {
        private const float LargoMinimoCarro = 3f;
        private const float LargoMaximoCarro = 6f;
        private const float LargoMinimoMoto = 1.5f;
        private const float LargoMaximoMoto = 3f;
        private const float ToleranciaLargoMoto = 0.05f;

        // Solo los que tienen prefab (V11 no tiene Carro_V11 todavía).
        private static string[] Carros() => CreadorCarrosT1343.CarrosT1312.Concat(CreadorCarrosT1343.CarrosAnteriores).Where(CreadorCarrosT1343.ExistePrefab).ToArray();

        private static string[] MotosConLargo() => CreadorCarrosT1343.Motos.Where(m => m.largo > 0f).Select(m => m.codigo).ToArray();

        private static string[] Motos() => CreadorCarrosT1343.Motos.Select(m => m.codigo).ToArray();

        private static GameObject Prefab(string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosT1343.RutaPrefab(codigo));
            Assert.IsNotNull(prefab, "falta el prefab de " + codigo);
            return prefab;
        }

        private static Transform Visual(GameObject prefab) => prefab.transform.Find(CreadorCarrosPiezasT742.NombreVisual);

        [Test]
        public void Carros_AlMenosNueveTienenPrefab()
        {
            Assert.GreaterOrEqual(Carros().Length, 9, string.Join(", ", Carros()));
        }

        [Test]
        public void Cristales_UsanElMaterialTransparente([ValueSource(nameof(Carros))] string codigo)
        {
            Material cristal = AssetDatabase.LoadAssetAtPath<Material>(PiezasCarroT1342.RutaCristal);
            Assert.IsNotNull(cristal, "CristalCarro.mat");
            Assert.AreEqual(1f, cristal.GetFloat("_Surface"), "transparente");
            Assert.GreaterOrEqual(cristal.renderQueue, 3000);
            Renderer[] cristales = Prefab(codigo).GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith(PiezasCarroT1342.PrefijoCristal)).ToArray();
            Assert.GreaterOrEqual(cristales.Length, 2, codigo + " cristales");
            foreach (Renderer r in cristales)
            {
                Assert.AreSame(cristal, r.sharedMaterial, codigo + " " + r.name);
            }
        }

        [Test]
        public void Largo_EstaEntreTresYSeisMetros([ValueSource(nameof(Carros))] string codigo)
        {
            Transform visual = Visual(Prefab(codigo));
            Assert.IsNotNull(visual, codigo + " Visual_T511");
            Assert.IsNotNull(visual.Find("Carroceria"), codigo + " Carroceria del FBX nuevo");
            float largo = CreadorCarrosT511.LimitesExactos(visual.gameObject).size.z;
            Assert.GreaterOrEqual(largo, LargoMinimoCarro, codigo);
            Assert.LessOrEqual(largo, LargoMaximoCarro, codigo);
        }

        [Test]
        public void Ruedas_LasCuatroGiranSinNulos([ValueSource(nameof(Carros))] string codigo)
        {
            RuedasVisualesCarro ruedas = Prefab(codigo).GetComponent<RuedasVisualesCarro>();
            Assert.IsNotNull(ruedas, codigo + " RuedasVisualesCarro");
            SerializedProperty giro = new SerializedObject(ruedas).FindProperty("giro");
            Assert.AreEqual(4, giro.arraySize, codigo);
            for (int i = 0; i < giro.arraySize; i++)
            {
                Assert.IsNotNull(giro.GetArrayElementAtIndex(i).objectReferenceValue, codigo + " giro " + i);
            }
        }

        [Test]
        public void Cofre_V01aV01bV05_TienenBisagraConSuCofre([Values("V01a", "V01b", "V05")] string codigo)
        {
            Transform bisagra = Visual(Prefab(codigo)).Find("Bisagra_Cofre");
            Assert.IsNotNull(bisagra, codigo + " Bisagra_Cofre");
            Assert.IsNotNull(bisagra.Find("Cofre"), codigo + " Cofre bajo su bisagra");
            SerializedProperty p = new SerializedObject(Prefab(codigo).GetComponent<PuertasCarro>()).FindProperty("bisagraCofre");
            Assert.AreSame(bisagra, p.objectReferenceValue, codigo + " PuertasCarro.bisagraCofre");
        }

        [Test]
        public void V11_TieneBisagrasDePuertasYCofre()
        {
            Assume.That(CreadorCarrosT1343.ExistePrefab("V11"), "no hay Carro_V11: se salta");
            Transform visual = Visual(Prefab("V11"));
            foreach (var par in new[] { ("Bisagra_Puerta_I", "Puerta_I"), ("Bisagra_Puerta_D", "Puerta_D"), ("Bisagra_Cofre", "Cofre") })
            {
                Transform bisagra = visual.Find(par.Item1);
                Assert.IsNotNull(bisagra, par.Item1);
                Assert.IsNotNull(bisagra.Find(par.Item2), par.Item2);
            }
        }

        [Test]
        public void Motos_TienenPrefabYDosRuedasQueGiranSinNulos([ValueSource(nameof(Motos))] string codigo)
        {
            GameObject prefab = Prefab(codigo);
            RuedasVisualesCarro ruedas = prefab.GetComponent<RuedasVisualesCarro>();
            Assert.IsNotNull(ruedas, codigo + " RuedasVisualesCarro");
            SerializedProperty giro = new SerializedObject(ruedas).FindProperty("giro");
            Assert.AreEqual(2, giro.arraySize, codigo + " ruedas que giran");
            for (int i = 0; i < giro.arraySize; i++)
            {
                Assert.IsNotNull(giro.GetArrayElementAtIndex(i).objectReferenceValue, codigo + " giro " + i);
            }
            Assert.IsNotNull(prefab.GetComponent<ManejoMoto>(), codigo + " ManejoMoto");
        }

        [Test]
        public void Motos_ElCuerpoEsElDelFbxNuevo([ValueSource(nameof(Motos))] string codigo)
        {
            var datos = CreadorCarrosT1343.Motos.First(m => m.codigo == codigo);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosBlenderT1312.RutaFbx(codigo));
            Assert.IsNotNull(fbx, codigo + " FBX");
            Mesh esperada = fbx.transform.Find(datos.cuerpo).GetComponent<MeshFilter>().sharedMesh;
            MeshFilter filtro = Visual(Prefab(codigo)).Find("Visual").GetComponent<MeshFilter>();
            Assert.AreEqual(esperada.vertexCount, filtro.sharedMesh.vertexCount, codigo + " malla del cuerpo");
        }

        [Test]
        public void Motos_ConLargoPedido_MidenElLargoA5Porciento([ValueSource(nameof(MotosConLargo))] string codigo)
        {
            float largo = CreadorCarrosT1343.Motos.First(m => m.codigo == codigo).largo;
            float medido = CreadorCarrosT511.LimitesExactos(Visual(Prefab(codigo)).Find("Visual").gameObject).size.z;
            Assert.AreEqual(largo, medido, largo * ToleranciaLargoMoto, codigo + " largo");
        }

        [Test]
        public void Motos_V07V08_ConservanUnLargoDeMoto([Values("V07", "V08")] string codigo)
        {
            float medido = CreadorCarrosT511.LimitesExactos(Visual(Prefab(codigo)).Find("Visual").gameObject).size.z;
            Assert.GreaterOrEqual(medido, LargoMinimoMoto, codigo);
            Assert.LessOrEqual(medido, LargoMaximoMoto, codigo);
        }
    }
}

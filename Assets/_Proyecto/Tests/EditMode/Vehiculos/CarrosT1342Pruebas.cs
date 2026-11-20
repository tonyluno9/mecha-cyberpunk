using System.Linq;
using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1342: los carros V12 a V24 con piezas de Blender: tamaño, ruedas, bisagras de puertas y cofre y cristales transparentes.</summary>
    public class CarrosT1342Pruebas
    {
        private static string[] Codigos() => CreadorCarrosT1342.Carros.Select(c => c.codigo).ToArray();

        private static GameObject Prefab(string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosT1342.RutaPrefab(codigo));
            Assert.IsNotNull(prefab, "falta el prefab de " + codigo);
            return prefab;
        }

        private static Transform Visual(GameObject prefab) => prefab.transform.Find(CreadorCarrosPiezasT742.NombreVisual);

        [Test]
        public void Prefab_ExisteYMideElLargoDeLaTabla([ValueSource(nameof(Codigos))] string codigo)
        {
            float largo = CreadorCarrosT1342.Carros.First(c => c.codigo == codigo).largo;
            Transform visual = Visual(Prefab(codigo));
            Assert.IsNotNull(visual, codigo + " Visual_T511");
            Bounds b = CreadorCarrosT511.LimitesExactos(visual.gameObject);
            Assert.AreEqual(largo, b.size.z, largo * 0.05f, codigo + " largo");
        }

        [Test]
        public void Ruedas_TodasLasDelFbxEstanEnElGiroSinNulos([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosBlenderT1312.RutaFbx(codigo));
            Assert.IsNotNull(fbx, codigo + " FBX");
            int esperadas = CreadorCarrosT1342.NombresRuedasFbx(fbx).Length;
            Assert.GreaterOrEqual(esperadas, 4, codigo);
            RuedasVisualesCarro ruedas = Prefab(codigo).GetComponent<RuedasVisualesCarro>();
            Assert.IsNotNull(ruedas, codigo + " RuedasVisualesCarro");
            SerializedObject so = new SerializedObject(ruedas);
            SerializedProperty giro = so.FindProperty("giro");
            Assert.AreEqual(esperadas, giro.arraySize, codigo + " ruedas que giran");
            for (int i = 0; i < giro.arraySize; i++)
            {
                Assert.IsNotNull(giro.GetArrayElementAtIndex(i).objectReferenceValue, codigo + " giro " + i);
            }
            SerializedProperty direccion = so.FindProperty("direccionDelantera");
            Assert.AreEqual(2, direccion.arraySize, codigo + " ruedas que doblan");
            Assert.IsNotNull(direccion.GetArrayElementAtIndex(0).objectReferenceValue, codigo);
            Assert.IsNotNull(direccion.GetArrayElementAtIndex(1).objectReferenceValue, codigo);
        }

        [Test]
        public void Bisagras_ExistenConSuPieza([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform visual = Visual(Prefab(codigo));
            foreach (var par in new[] { ("Bisagra_Puerta_I", "Puerta_I"), ("Bisagra_Puerta_D", "Puerta_D"), ("Bisagra_Cofre", "Cofre") })
            {
                Transform bisagra = visual.Find(par.Item1);
                Assert.IsNotNull(bisagra, codigo + " " + par.Item1);
                Assert.IsNotNull(bisagra.Find(par.Item2), codigo + " " + par.Item2 + " bajo su bisagra");
            }
            if (codigo == "V22") { return; } // T-1507: el V22 rehecho solo lleva parabrisas; sus ventanas laterales van pintadas
            Assert.IsNotNull(visual.Find("Bisagra_Puerta_I/Puerta_I/" + PiezasCarroT1342.NombreCristalPuertaI), codigo + " cristal de la puerta izquierda");
            Assert.IsNotNull(visual.Find("Bisagra_Puerta_D/Puerta_D/" + PiezasCarroT1342.NombreCristalPuertaD), codigo + " cristal de la puerta derecha");
        }

        [Test]
        public void Cristales_UsanElMaterialTransparente([ValueSource(nameof(Codigos))] string codigo)
        {
            Material cristal = AssetDatabase.LoadAssetAtPath<Material>(PiezasCarroT1342.RutaCristal);
            Assert.IsNotNull(cristal, "CristalCarro.mat");
            Assert.AreEqual(1f, cristal.GetFloat("_Surface"), "transparente");
            Assert.GreaterOrEqual(cristal.renderQueue, 3000);
            Renderer[] cristales = Prefab(codigo).GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Cristal_")).ToArray();
            Assert.GreaterOrEqual(cristales.Length, codigo == "V22" ? 1 : 2, codigo + " cristales");
            foreach (Renderer r in cristales)
            {
                Assert.AreSame(cristal, r.sharedMaterial, codigo + " " + r.name);
            }
        }

        [Test]
        public void BisagraIzquierda_EstaALaIzquierdaYAlFrenteDeSuPuerta([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject prefab = Prefab(codigo);
            Transform visual = Visual(prefab);
            Transform bisagra = visual.Find("Bisagra_Puerta_I");
            Assert.IsNotNull(bisagra, codigo);
            Vector3 local = prefab.transform.InverseTransformPoint(bisagra.position);
            Assert.Less(local.x, 0f, codigo + " bisagra izquierda en x < 0");
            Bounds puerta = CreadorCarrosBlenderT1312.LimitesEn(bisagra.Find("Puerta_I").gameObject, prefab.transform);
            Assert.AreEqual(puerta.max.z, local.z, 0.05f, codigo + " bisagra en el borde delantero de la puerta");
            Transform derecha = visual.Find("Bisagra_Puerta_D");
            Assert.Greater(prefab.transform.InverseTransformPoint(derecha.position).x, 0f, codigo + " bisagra derecha en x > 0");
        }
    }
}

using System.Linq;
using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1604: V01a, V01b y V05 por piezas (FBX del 22-nov, sin Cristal_*) y Ciudad_Prologo sin los prefabs viejos de una malla.</summary>
    public class ModelosBlenderT1604Pruebas
    {
        private static readonly string[] Codigos = ModelosBlenderT1604.Codigos;

        private static GameObject Prefab(string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelosBlenderT1604.RutaPrefab(codigo));
            Assert.IsNotNull(prefab, "falta el prefab de " + codigo);
            return prefab;
        }

        [Test]
        public void Ruedas_Cuatro([ValueSource(nameof(Codigos))] string codigo)
        {
            RuedasVisualesCarro ruedas = Prefab(codigo).GetComponent<RuedasVisualesCarro>();
            Assert.IsNotNull(ruedas, codigo + " RuedasVisualesCarro");
            SerializedObject so = new SerializedObject(ruedas);
            Assert.AreEqual(4, so.FindProperty("giro").arraySize, codigo + " ruedas que giran");
            Assert.AreEqual(2, so.FindProperty("direccionDelantera").arraySize, codigo + " ruedas que dirigen");
            foreach (string nombre in CreadorCarrosPiezasT742.NombresRuedas)
            {
                Assert.IsNotNull(Prefab(codigo).transform.Find(nombre + "/Direccion/Giro/Modelo"), codigo + " " + nombre);
            }
        }

        [Test]
        public void PuertasYCofre_ConBisagraYPieza([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform visual = Prefab(codigo).transform.Find(CreadorCarrosPiezasT742.NombreVisual);
            Assert.IsNotNull(visual, codigo + " Visual_T511");
            foreach (var par in new[] { ("Bisagra_Puerta_I", "Puerta_I"), ("Bisagra_Puerta_D", "Puerta_D"), ("Bisagra_Cofre", "Cofre") })
            {
                Transform bisagra = visual.Find(par.Item1);
                Assert.IsNotNull(bisagra, codigo + " " + par.Item1);
                Assert.IsNotNull(bisagra.Find(par.Item2), codigo + " " + par.Item2);
            }
        }

        [Test]
        public void SinCristal_NiReferenciasRotas([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject prefab = Prefab(codigo);
            Assert.IsFalse(prefab.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Cristal_")), codigo + ": quedan Cristal_*");
            var rotas = ModelosBlenderT1604.ReferenciasRotas(prefab)
                .Where(r => r.Contains("PuertasCarro") || r.Contains("RuedasVisualesCarro") || r.Contains("scripts faltantes")).ToList();
            Assert.IsEmpty(rotas, codigo + ": " + string.Join("; ", rotas));
        }

        [Test]
        public void Largo_EntreTresOchoYCincoMetros([ValueSource(nameof(Codigos))] string codigo)
        {
            Transform visual = Prefab(codigo).transform.Find(CreadorCarrosPiezasT742.NombreVisual);
            float largo = CreadorCarrosT511.LimitesExactos(visual.gameObject).size.z;
            Assert.GreaterOrEqual(largo, ModelosBlenderT1604.LargoMinimo, codigo);
            Assert.LessOrEqual(largo, ModelosBlenderT1604.LargoMaximo, codigo);
        }

        [Test]
        public void Cajas_NoSalenDeLaCarroceria([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject prefab = Prefab(codigo);
            Transform visual = prefab.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
            Bounds modelo = CreadorCarrosT511.LimitesExactos(visual.gameObject);
            foreach (string nombre in new[] { "Carroceria", "Cabina" })
            {
                BoxCollider caja = prefab.transform.Find(nombre).GetComponent<BoxCollider>();
                Assert.IsNotNull(caja, codigo + " " + nombre);
                Vector3 tamano = Vector3.Scale(caja.transform.lossyScale, caja.size);
                Assert.LessOrEqual(tamano.x, modelo.size.x + 0.02f, codigo + " " + nombre + " ancho");
                Assert.LessOrEqual(tamano.z, modelo.size.z + 0.02f, codigo + " " + nombre + " largo");
            }
        }

        [Test]
        public void CiudadPrologo_SinInstanciasDePrefabsViejosConReemplazo()
        {
            Scene escena = EditorSceneManager.OpenScene(ModelosBlenderT1604.RutaEscena, OpenSceneMode.Additive);
            try
            {
                var viejos = ModelosBlenderT1604.ViejosConReemplazo(escena.GetRootGameObjects(), false);
                Assert.IsEmpty(viejos, string.Join(", ", viejos.Select(v => v.name)));
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Vehiculos.Carro;
using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-1683 (T45): menos carros (sin V01b, V16 a V18 ni V22 a V24 en las listas de tráfico ni en Ciudad_Prologo, policía intacta) y la chatarra queda quemada
    /// en todas sus mallas, también en los carros y motos con el modelo de Tripo. Las pruebas de listas y escena pasan después de correr MenosCarrosT1683.Aplicar.
    /// </summary>
    public class MenosCarrosT1683Pruebas
    {
        private const string RutaConfigZonas = "Assets/_Proyecto/Trafico/Resources/ConfigCarrosZonas.asset";

        // ------------------------------------------------------------------ reglas

        [TestCase("Carro_V01b", "V01b")]
        [TestCase("CarroTrafico_V02_Rojo", "V02")]
        [TestCase("V01b_CarroPro", "V01b")]
        [TestCase("Estacionado_12_V17", "V17")]
        [TestCase("Carro_MT01", "MT01")]
        [TestCase("Carro_C19", "C19")]
        [TestCase("CarroTrafico", null)]
        [TestCase("CarroViejo", null)]
        [TestCase("CarroRobable_5", null)]
        [TestCase("Patrulla", null)]
        [TestCase("ZonaTrafico_Avenida", null)]
        public void CodigoDe_LeeElCodigoDelNombre(string nombre, string esperado)
        {
            Assert.AreEqual(esperado, MenosCarrosT1683.CodigoDe(nombre));
        }

        [Test]
        public void Excluidos_NoSonDeLaPoliciaNiMotos_YCadaUnoTieneReemplazoQueSeQueda()
        {
            foreach (string codigo in MenosCarrosT1683.CodigosExcluidos)
            {
                Assert.IsFalse(ReglaModeloDePolicia.EsDePolicia("Carro_" + codigo), codigo + " es de la policía");
                Assert.IsFalse(CarrosTripoT1644.Motos.Contains(codigo), codigo + " es moto");
                string reemplazo = MenosCarrosT1683.ReemplazoDe(codigo);
                Assert.IsNotNull(reemplazo, codigo + " sin reemplazo");
                CollectionAssert.Contains(MenosCarrosT1683.CodigosQueSeQuedan, reemplazo, codigo + " -> " + reemplazo);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(MenosCarrosT1683.RutaDeReemplazo("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab")),
                    "falta el prefab de " + reemplazo);
            }
            CollectionAssert.IsEmpty(MenosCarrosT1683.CodigosExcluidos.Intersect(MenosCarrosT1683.CodigosQueSeQuedan));
            Assert.IsTrue(MenosCarrosT1683.EsExcluido("CarroTrafico_V22"));
            Assert.IsFalse(MenosCarrosT1683.EsExcluido("CarroTrafico_V20"), "el tuneado con neón se queda");
            Assert.IsFalse(MenosCarrosT1683.EsExcluido("Carro_PA01"), "la patrulla buena no se toca");
        }

        [Test]
        public void SeQuedanLosBasicos_ElTuneadoConNeon_YUnSoloDeportivo()
        {
            foreach (string basico in new[] { "V01a", "V02", "V03", "V09", "V13", "V14", "V20", "V21" })
            {
                CollectionAssert.Contains(MenosCarrosT1683.CodigosQueSeQuedan, basico);
            }
            string[] deportivos = { "V01b", "V12", "V22", "V23", "V24" };
            Assert.AreEqual(1, deportivos.Count(d => MenosCarrosT1683.CodigosQueSeQuedan.Contains(d)), "un deportivo suelto");
            foreach (string grande in new[] { "V16", "V17", "V18" })
            {
                CollectionAssert.Contains(MenosCarrosT1683.CodigosExcluidos, grande, "microbús, camión y tráiler fuera");
            }
            CollectionAssert.AreEquivalent(MenosCarrosT1683.CodigosExcluidos, DisenoZonasTrafico.Excluidos, "la tabla de tráfico y la del editor dicen lo mismo");
        }

        [TestCase("Estacionado_12_V17", "Carro_V17", "Carro_V03", "Estacionado_12_V03")]
        [TestCase("Carro_V22", "Carro_V22", "Carro_V20", "Carro_V20")]
        [TestCase("CarroRobable_5", "Carro_V01b", "Carro_V13", "CarroRobable_5")]
        public void NombreNuevo_ConservaElNumeroYLosNombresPropios(string viejo, string prefabViejo, string prefabNuevo, string esperado)
        {
            Assert.AreEqual(esperado, MenosCarrosT1683.NombreNuevo(viejo, prefabViejo, prefabNuevo));
        }

        // ------------------------------------------------------------------ listas de tráfico

        [Test]
        public void DisenoDeZonas_SinExcluidos()
        {
            foreach (var zona in DisenoZonasTrafico.Zonas)
            {
                foreach (string codigo in MenosCarrosT1683.CodigosExcluidos)
                {
                    CollectionAssert.DoesNotContain(zona.carros, codigo, "zona " + zona.nombre);
                }
                Assert.Greater(zona.carros.Length, 0, "zona " + zona.nombre + " sin carros");
            }
            foreach (string codigo in MenosCarrosT1683.CodigosExcluidos)
            {
                CollectionAssert.DoesNotContain(DisenoZonasTrafico.Extras, codigo);
                CollectionAssert.DoesNotContain(DisenoZonasTrafico.CarrosNuevos, codigo);
            }
        }

        [Test]
        public void ConfigCarrosZonas_SinExcluidos()
        {
            ConfigCarrosZonas config = AssetDatabase.LoadAssetAtPath<ConfigCarrosZonas>(RutaConfigZonas);
            Assert.IsNotNull(config, RutaConfigZonas);
            List<CarroTrafico> todos = config.ExtrasGenerales.Concat(config.Zonas.SelectMany(z => z.carros)).ToList();
            Assert.Greater(todos.Count, 0);
            foreach (CarroTrafico carro in todos)
            {
                Assert.IsNotNull(carro, "carro nulo en la lista");
                Assert.IsFalse(MenosCarrosT1683.EsExcluido(carro.name), carro.name + " sigue en ConfigCarrosZonas (corre MenosCarrosT1683.Aplicar)");
            }
            Assert.AreEqual(0, VehiculosT1554.ReferenciasEnListas(new SerializedObject(config), MenosCarrosT1683.RutasExcluidas(), false));
        }

        [Test]
        public void CiudadPrologo_TraficoYCarrosSinExcluidos_YPatrullasIntactas()
        {
            Scene escena = EditorSceneManager.OpenScene(MenosCarrosT1683.RutaEscena, OpenSceneMode.Additive);
            try
            {
                GameObject[] raices = escena.GetRootGameObjects();
                TraficoCalle trafico = raices.SelectMany(g => g.GetComponentsInChildren<TraficoCalle>(true)).FirstOrDefault();
                Assert.IsNotNull(trafico, "falta TraficoCalle");
                SerializedProperty lista = new SerializedObject(trafico).FindProperty("prefabsCarro");
                Assert.Greater(lista.arraySize, 0);
                for (int i = 0; i < lista.arraySize; i++)
                {
                    Object o = lista.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.IsNotNull(o, "elemento nulo en prefabsCarro");
                    Assert.IsFalse(MenosCarrosT1683.EsExcluido(o.name), o.name + " sigue en el tráfico (corre MenosCarrosT1683.Aplicar)");
                }
                List<GameObject> excluidos = MenosCarrosT1683.Instancias(raices, MenosCarrosT1683.RutasExcluidas(), out int anidados);
                List<string> activos = excluidos.Where(g => g.activeInHierarchy).Select(g => g.name).ToList();
                Assert.AreEqual(0, activos.Count, "carros excluidos activos en la escena: " + string.Join(", ", activos));
                Assert.AreEqual(0, anidados, "carros excluidos dentro de otros prefabs");
                int estacionados = raices.SelectMany(g => g.GetComponentsInChildren<CarroEstacionado>(false)).Count(c => c.name.StartsWith(MenosCarrosT1683.PrefijoEstacionado));
                Assert.Greater(estacionados, 0, "siguen los estacionados (cambiados, no borrados)");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        // ------------------------------------------------------------------ explosión: chatarra quemada

        [TestCase("V12")]
        [TestCase("V20")]
        [TestCase("V09")]
        [TestCase("V01a")]
        [TestCase("MT01")]
        [TestCase("PA01")]
        public void Quemar_UnCarroInstanciado_QuedaConElArmazonEnTodasSusMallas(string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarrosTripoT1644.RutaPrefab(codigo));
            Assert.IsNotNull(prefab, "falta Carro_" + codigo);
            GameObject carro = Object.Instantiate(prefab);
            try
            {
                Renderer[] mallas = carro.GetComponentsInChildren<Renderer>(true).Where(DanioCarro.EsMallaQuemable).ToArray();
                Assert.Greater(mallas.Length, 0, codigo + " sin mallas");
                if (CarrosTripoT1644.Carros.Contains(codigo))
                {
                    Transform modelo = carro.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == CarrosTripoT1644.NombreModelo);
                    Assert.IsNotNull(modelo, codigo + ": falta el modelo de Tripo");
                    Assert.Greater(modelo.GetComponentsInChildren<Renderer>(true).Length, 0, codigo + ": el modelo de Tripo no tiene mallas");
                }

                int quemadas = DanioCarro.Quemar(carro.transform);

                Assert.AreEqual(mallas.Length, quemadas, codigo + ": se queman todas las mallas");
                Material armazon = DanioCarro.MaterialArmazon;
                Assert.IsNotNull(armazon, "no se pudo crear el material del armazón");
                Assert.AreEqual(DanioCarro.ColorDelArmazon, armazon.GetColor("_BaseColor"));
                Assert.IsNull(armazon.GetTexture("_BaseMap"), "el armazón no lleva la textura del carro");
                MaterialPropertyBlock bloque = new MaterialPropertyBlock();
                foreach (Renderer r in mallas)
                {
                    Assert.Greater(r.sharedMaterials.Length, 0, r.name);
                    foreach (Material m in r.sharedMaterials)
                    {
                        Assert.AreSame(armazon, m, codigo + ": " + r.name + " sigue con " + (m != null ? m.name : "null"));
                    }
                    r.GetPropertyBlock(bloque);
                    Assert.AreEqual(DanioCarro.ColorDelArmazon, bloque.GetColor("_BaseColor"), codigo + ": " + r.name + " sin tinte de armazón");
                }
            }
            finally
            {
                Object.DestroyImmediate(carro);
            }
        }

        [Test]
        public void Quemar_NoTocaPrefabNiParticulasNiHuecosDeBala()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarrosTripoT1644.RutaPrefab("V12"));
            Assert.IsNotNull(prefab);
            Material[] antes = prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).ToArray();
            GameObject carro = Object.Instantiate(prefab);
            GameObject hueco = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject humo = new GameObject("Humo", typeof(ParticleSystem));
            try
            {
                hueco.name = "HuecoBala";
                hueco.transform.SetParent(carro.transform, false);
                humo.transform.SetParent(carro.transform, false);
                Material materialHueco = hueco.GetComponent<Renderer>().sharedMaterial;
                Material materialHumo = humo.GetComponent<ParticleSystemRenderer>().sharedMaterial;

                DanioCarro.Quemar(carro.transform);

                Assert.AreSame(materialHueco, hueco.GetComponent<Renderer>().sharedMaterial, "el hueco de bala no se quema");
                Assert.AreSame(materialHumo, humo.GetComponent<ParticleSystemRenderer>().sharedMaterial, "las partículas no se queman");
                CollectionAssert.AreEqual(antes, prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).ToArray(), "el prefab queda igual");
            }
            finally
            {
                Object.DestroyImmediate(carro);
            }
        }
    }
}

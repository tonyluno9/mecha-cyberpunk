using System.Linq;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-511: los 8 prefabs de carro con modelo real (Carro_V*): visual, cajas de choque, ruedas de greybox invisibles y cámara a su altura.</summary>
    public class CarrosRealesPruebas
    {
        private static readonly string[] Codigos = { "V01a", "V01b", "V02", "V03", "V04", "V05", "V06", "V08" };

        private static GameObject Cargar(string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
            Assert.IsNotNull(prefab, "Falta el prefab Carro_" + codigo);
            return prefab;
        }

        [Test]
        public void Prefab_TieneElModeloReal_ConTrianglesYMateriales([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject prefab = Cargar(codigo);
            Transform visual = prefab.transform.Find("Visual_T511");
            Assert.IsNotNull(visual, "Lleva el visual del modelo real");
            MeshRenderer[] visibles = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            Assert.GreaterOrEqual(visibles.Length, 1, "El modelo se ve");
            Assert.IsTrue(visibles.All(r => r.sharedMaterial != null), "Cada malla tiene su material");
            Assert.IsNull(prefab.transform.Find("Visual_T250"), "Ya no lleva el visual viejo de V01a en todos");
        }

        [Test]
        public void Prefab_RuedasDeGreybox_SiguenPeroNoSeVen([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject prefab = Cargar(codigo);
            // T-742: V01a, V01b y V05 van por piezas: sus ruedas SÍ se ven (llantas RU01); lo prueba CarrosPorPiezasT742Pruebas.
            bool porPiezas = prefab.GetComponent<PuertasCarro>() != null;
            foreach (string nombre in new[] { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" })
            {
                Transform rueda = prefab.transform.Find(nombre);
                Assert.IsNotNull(rueda, "La física necesita " + nombre);
                foreach (Renderer r in rueda.GetComponentsInChildren<Renderer>(true))
                {
                    Assert.AreEqual(porPiezas, r.enabled, nombre + (porPiezas ? " se ve (llanta RU01)" : " no se ve"));
                }
            }
            foreach (string nombre in new[] { "Carroceria", "Cabina" })
            {
                Assert.IsFalse(prefab.transform.Find(nombre).GetComponent<Renderer>().enabled, nombre + " de greybox no se ve");
                Assert.IsNotNull(prefab.transform.Find(nombre).GetComponent<BoxCollider>(), nombre + " sigue chocando");
            }
            // Ningún cilindro de greybox visible en todo el prefab.
            if (porPiezas)
            {
                return;
            }
            foreach (MeshFilter f in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshRenderer r = f.GetComponent<MeshRenderer>();
                if (r != null && r.enabled)
                {
                    Assert.AreNotEqual("Cylinder", f.sharedMesh.name, "Un cilindro visible en " + f.name);
                }
            }
        }

        [Test]
        public void Prefab_EsRobable_ConAlarmaYCamaraPropia([ValueSource(nameof(Codigos))] string codigo)
        {
            GameObject prefab = Cargar(codigo);
            Assert.IsNotNull(prefab.GetComponent<CarroViejo>());
            Assert.IsNotNull(prefab.GetComponent<CarroEstacionado>(), "Alarma y calor al robarlo");
            CamaraCarro camara = prefab.GetComponentInChildren<CamaraCarro>(true);
            Assert.Greater(camara.AlturaFocoActual, 0.8f);
            Assert.Greater(camara.DistanciaAtrasActual, 3f);
            Bounds b = new Bounds();
            bool primero = true;
            foreach (Renderer r in prefab.transform.Find("Visual_T511").GetComponentsInChildren<Renderer>(true))
            {
                if (primero) { b = r.bounds; primero = false; } else { b.Encapsulate(r.bounds); }
            }
            Assert.AreEqual(b.size.y * 0.62f, camara.AlturaFocoActual, 0.3f, "La cámara mira a la altura del modelo");
        }

        [Test]
        public void Camara_AlturaDistintaEnCadaModelo()
        {
            float van = Cargar("V04").GetComponentInChildren<CamaraCarro>(true).AlturaFocoActual;
            float moto = Cargar("V08").GetComponentInChildren<CamaraCarro>(true).AlturaFocoActual;
            float deportivo = Cargar("V01b").GetComponentInChildren<CamaraCarro>(true).AlturaFocoActual;
            Assert.Greater(van, deportivo, "La van es más alta que el deportivo");
            Assert.Greater(van, moto, "La van es más alta que la moto");
        }

        [Test]
        public void Modelos_MasDeQuinceMilCaras_LlevanMeshLod([ValueSource(nameof(NuevosConLod))] string codigo)
        {
            ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath("Assets/Arte/Vehiculos/" + codigo + "/" + codigo + ".fbx");
            Assert.IsNotNull(importador);
            Assert.IsTrue(importador.generateMeshLods, codigo + " pasa de 15 mil caras: Mesh LOD");
        }

        private static readonly string[] NuevosConLod = { "V02", "V03", "V04", "V05", "V06", "V08" };

        [Test]
        public void ArtePrefab_TieneVisualYBoxCollider([ValueSource(nameof(NuevosConLod))] string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Vehiculos/Prefabs/" + codigo + ".prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.transform.Find("Visual"), "Hijo Visual");
            BoxCollider caja = prefab.GetComponent<BoxCollider>();
            Assert.IsNotNull(caja, "BoxCollider");
            Assert.Greater(caja.size.z, caja.size.x, "El eje largo está en Z (el frente mira a +Z)");
        }
    }
}

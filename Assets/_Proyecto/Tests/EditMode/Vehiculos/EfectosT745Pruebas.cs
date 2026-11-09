using System.Collections.Generic;
using MechaCyberpunk.Vehiculos.Efectos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-745: detección de barriles y que los efectos del Unity Particle Pack usan materiales URP (nada rosa).</summary>
    public class EfectosT745Pruebas
    {
        private const string RutaPack = "Assets/UnityTechnologies/ParticlePack/EffectExamples";

        private static bool EsUrp(Shader shader) =>
            shader != null && shader.name != "Hidden/InternalErrorShader" && (shader.name.StartsWith("Universal Render Pipeline/") || shader.name.StartsWith("Shader Graphs/"));

        [Test]
        public void Barriles_SeReconocenPorNombre()
        {
            Assert.IsTrue(InstaladorBarriles.EsNombreDeBarril("Barril1"));
            Assert.IsTrue(InstaladorBarriles.EsNombreDeBarril("Barril_Rojo"));
            Assert.IsTrue(InstaladorBarriles.EsNombreDeBarril("C16_Tambo"));
            Assert.IsTrue(InstaladorBarriles.EsNombreDeBarril("C16_Tambo (3)"));
            Assert.IsFalse(InstaladorBarriles.EsNombreDeBarril("Caja"));
            Assert.IsFalse(InstaladorBarriles.EsNombreDeBarril("Contenedor"));
            Assert.IsFalse(InstaladorBarriles.EsNombreDeBarril(null));
        }

        [Test]
        public void Instalar_PoneBarrilExplosivoSoloALosBarrilesConCollider()
        {
            GameObject con = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            con.name = "Barril_Prueba745";
            GameObject sin = new GameObject("Barril_SinCollider745");
            GameObject otro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            otro.name = "Caja_Prueba745";
            try
            {
                int puestos = InstaladorBarriles.Instalar(SceneManager.GetActiveScene());
                Assert.GreaterOrEqual(puestos, 1);
                Assert.IsNotNull(con.GetComponent<BarrilExplosivo>());
                Assert.IsNull(sin.GetComponent<BarrilExplosivo>(), "sin collider no se le puede disparar");
                Assert.IsNull(otro.GetComponent<BarrilExplosivo>());
                Assert.AreEqual(0, InstaladorBarriles.Instalar(SceneManager.GetActiveScene()), "repetirlo no duplica");
            }
            finally
            {
                Object.DestroyImmediate(con);
                Object.DestroyImmediate(sin);
                Object.DestroyImmediate(otro);
            }
        }


        [Test]
        public void Catalogo_TieneTodosLosPrefabsDelPack()
        {
            CatalogoEfectos c = AssetDatabase.LoadAssetAtPath<CatalogoEfectos>("Assets/_Proyecto/Vehiculos/Resources/CatalogoEfectos.asset");
            Assert.IsNotNull(c, "CatalogoEfectos en Resources");
            Assert.IsNotNull(c.ExplosionDiminuta);
            Assert.IsNotNull(c.ExplosionChica);
            Assert.IsNotNull(c.ExplosionGrande);
            Assert.IsNotNull(c.Humo);
            Assert.IsNotNull(c.FuegoChico);
            Assert.IsNotNull(c.FuegoMediano);
            Assert.IsNotNull(c.Chispas);
            Assert.IsNotNull(c.MaterialChamusco);
            Assert.IsTrue(EsUrp(c.MaterialChamusco.shader), c.MaterialChamusco.shader.name);
        }

        [Test]
        public void PrefabsDelCatalogo_SoloUsanMaterialesUrp_SinRosa()
        {
            CatalogoEfectos c = AssetDatabase.LoadAssetAtPath<CatalogoEfectos>("Assets/_Proyecto/Vehiculos/Resources/CatalogoEfectos.asset");
            foreach (GameObject prefab in new[] { c.ExplosionDiminuta, c.ExplosionChica, c.ExplosionGrande, c.Humo, c.FuegoChico, c.FuegoMediano, c.Chispas })
            {
                ParticleSystemRenderer[] renders = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
                Assert.Greater(renders.Length, 0, prefab.name + " tiene partículas");
                foreach (ParticleSystemRenderer r in renders)
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m == null)
                        {
                            Assert.AreEqual(ParticleSystemRenderMode.None, r.renderMode, prefab.name + "/" + r.name + " sin material");
                            continue;
                        }
                        Assert.IsTrue(EsUrp(m.shader), prefab.name + "/" + r.name + " usa " + m.name + " con " + (m.shader != null ? m.shader.name : "null"));
                    }
                }
            }
        }

        [Test]
        public void TodosLosMaterialesDelPack_SonUrp()
        {
            List<string> malos = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { RutaPack }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m != null && !EsUrp(m.shader))
                {
                    malos.Add(m.name + " (" + (m.shader != null ? m.shader.name : "null") + ")");
                }
            }
            Assert.IsEmpty(malos, "Materiales del pack que no son URP: " + string.Join(", ", malos));
        }
    }
}

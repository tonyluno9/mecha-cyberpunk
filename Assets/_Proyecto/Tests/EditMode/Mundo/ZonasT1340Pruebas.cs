using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1340: los props de zonas nuevos (medida, base, centro, material URP con mapas y colliders según la tabla).</summary>
    public class ZonasT1340Pruebas
    {
        private const float ToleranciaMedida = 0.05f;
        private const float ToleranciaBase = 0.02f;
        private const float ToleranciaCentro = 0.05f;

        private static readonly EntradaZonaT1340[] Entradas = ImportarZonasT1340.Entradas;

        private static GameObject Instanciar(EntradaZonaT1340 e)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportarZonasT1340.RutaPrefab(e.Nombre));
            Assert.IsNotNull(prefab, "Falta el prefab " + ImportarZonasT1340.RutaPrefab(e.Nombre));
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        [Test]
        public void Tabla_TieneTreintaYTresEntradasConNombresUnicos()
        {
            Assert.AreEqual(33, Entradas.Length);
            Assert.AreEqual(Entradas.Length, Entradas.Select(e => e.Nombre).Distinct().Count());
        }

        [Test]
        public void Prefab_Existe_YTieneCollider([ValueSource(nameof(Entradas))] EntradaZonaT1340 e)
        {
            GameObject g = Instanciar(e);
            try
            {
                Assert.Greater(g.GetComponentsInChildren<Collider>(true).Length, 0, e.Nombre + " sin collider");
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }

        [Test]
        public void Prefab_TieneLaMedidaDeLaTabla([ValueSource(nameof(Entradas))] EntradaZonaT1340 e)
        {
            GameObject g = Instanciar(e);
            try
            {
                Bounds b = ArteT250.Limites(g);
                float medido = e.Eje == ArteT250.Eje.Alto ? b.size.y : Mathf.Max(b.size.x, b.size.z);
                Assert.AreEqual(e.Medida, medido, e.Medida * ToleranciaMedida, e.Nombre);
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }

        [Test]
        public void Prefab_TieneElPivoteEnLaBaseYElCentro([ValueSource(nameof(Entradas))] EntradaZonaT1340 e)
        {
            GameObject g = Instanciar(e);
            try
            {
                Bounds b = ArteT250.Limites(g);
                Assert.AreEqual(0f, b.min.y, ToleranciaBase, e.Nombre + " min.y");
                Assert.AreEqual(0f, b.center.x, ToleranciaCentro * b.size.x, e.Nombre + " centro x");
                Assert.AreEqual(0f, b.center.z, ToleranciaCentro * b.size.z, e.Nombre + " centro z");
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }

        [Test]
        public void Prefab_UsaMaterialUrpLitConBaseNormalYMetalSuave([ValueSource(nameof(Entradas))] EntradaZonaT1340 e)
        {
            GameObject g = Instanciar(e);
            try
            {
                Renderer[] renderers = g.GetComponentsInChildren<Renderer>(true);
                Assert.Greater(renderers.Length, 0, e.Nombre + " sin renderers");
                foreach (Renderer r in renderers)
                {
                    Assert.Greater(r.sharedMaterials.Length, 0, e.Nombre);
                    foreach (Material m in r.sharedMaterials)
                    {
                        Assert.IsNotNull(m, e.Nombre + " material nulo");
                        Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name, e.Nombre);
                        Assert.IsNotNull(m.GetTexture("_BaseMap"), e.Nombre + " sin _BaseMap");
                        Assert.IsNotNull(m.GetTexture("_BumpMap"), e.Nombre + " sin _BumpMap");
                        Assert.IsNotNull(m.GetTexture("_MetallicGlossMap"), e.Nombre + " sin _MetallicGlossMap");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }

        [Test]
        public void Prefab_TieneElTipoDeColliderDeLaTabla([ValueSource(nameof(Entradas))] EntradaZonaT1340 e)
        {
            GameObject g = Instanciar(e);
            try
            {
                MeshCollider[] mallas = g.GetComponentsInChildren<MeshCollider>(true);
                BoxCollider[] cajas = g.GetComponentsInChildren<BoxCollider>(true);
                CapsuleCollider[] capsulas = g.GetComponentsInChildren<CapsuleCollider>(true);
                switch (e.TipoCollider)
                {
                    case TipoColliderZona.Caja:
                        Assert.AreEqual(0, mallas.Length, e.Nombre + " Caja con MeshCollider");
                        Assert.AreEqual(1, cajas.Length, e.Nombre);
                        Assert.IsNotNull(g.GetComponent<BoxCollider>(), e.Nombre + " la caja va en la raíz");
                        break;
                    case TipoColliderZona.Malla:
                        Assert.Greater(mallas.Length, 0, e.Nombre + " sin MeshCollider");
                        Assert.AreEqual(0, cajas.Length, e.Nombre + " Malla con BoxCollider");
                        foreach (MeshCollider m in mallas)
                        {
                            Assert.IsNotNull(m.sharedMesh, e.Nombre);
                            Assert.IsFalse(m.convex, e.Nombre + " convexo");
                        }
                        break;
                    default:
                        Assert.AreEqual(0, mallas.Length, e.Nombre + " Poste con MeshCollider");
                        Assert.AreEqual(1, cajas.Length, e.Nombre + " base");
                        Assert.AreEqual(1, capsulas.Length, e.Nombre + " tronco");
                        Assert.IsNotNull(g.transform.Find("Colision"), e.Nombre + " sin hijo Colision");
                        float alto = e.AltoTronco > 0f ? e.AltoTronco : ArteT250.Limites(g).size.y;
                        Assert.AreEqual(alto, capsulas[0].height, 0.01f, e.Nombre + " alto del tronco");
                        break;
                }
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }
    }
}

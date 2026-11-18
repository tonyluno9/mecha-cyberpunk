using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1353: los 18 prefabs de piezas modulares de zonas (Assets/Arte/ZonasModulos/Prefabs) existen y están bien armados.</summary>
    public class PiezasZonasT1353Pruebas
    {
        /// <summary>Tolerancia (m) del pivote en la base.</summary>
        private const float Tolerancia = 0.05f;

        /// <summary>Y mínima esperada según Blender (la escalera y el muelle bajan del cero a propósito).</summary>
        private static float MinimoEsperado(string nombre)
        {
            switch (nombre)
            {
                case "AU03_AutopistaRampa": return -0.3f;
                case "CE01_EscaleraCerro": return -0.3f;
                case "ML03_MuelleRecto": return -2f;
                case "ML04_MuelleEsquina": return -2f;
                case "ML05_Escollera": return -0.45f;
                default: return 0f;
            }
        }

        private static readonly string[] Nombres = ImportarT1353Zonas.Piezas.Select(p => p.Nombre).ToArray();

        private static GameObject Instanciar(string nombre)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportarT1353Zonas.CarpetaPrefabs + nombre + ".prefab");
            Assert.IsNotNull(prefab, "falta el prefab " + nombre);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        [Test]
        public void Piezas_Son18()
        {
            Assert.AreEqual(18, ImportarT1353Zonas.Piezas.Length);
        }

        [TestCaseSource(nameof(Nombres))]
        public void Prefab_TieneColliderYVisualConMalla(string nombre)
        {
            GameObject g = Instanciar(nombre);
            try
            {
                Assert.IsNotNull(g.GetComponentInChildren<Collider>(true), nombre + " sin collider");
                Transform visual = g.transform.Find("Visual");
                Assert.IsNotNull(visual, nombre + " sin hijo Visual");
                MeshFilter[] mallas = visual.GetComponentsInChildren<MeshFilter>(true);
                Assert.IsTrue(mallas.Any(f => f.sharedMesh != null && f.sharedMesh.vertexCount > 0), nombre + " sin malla");
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(visual.gameObject), nombre + ": Visual debe ser una instancia del FBX");
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }

        [TestCaseSource(nameof(Nombres))]
        public void Prefab_PivoteEnLaBase(string nombre)
        {
            GameObject g = Instanciar(nombre);
            try
            {
                Renderer[] rs = g.GetComponentsInChildren<Renderer>(true);
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs.Skip(1))
                {
                    b.Encapsulate(r.bounds);
                }
                Assert.AreEqual(MinimoEsperado(nombre), b.min.y, Tolerancia, nombre + " base fuera del pivote");
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }

        [TestCaseSource(nameof(Nombres))]
        public void Prefab_EsEstaticoYTieneLod(string nombre)
        {
            GameObject g = Instanciar(nombre);
            try
            {
                Assert.IsNotNull(g.GetComponent<LODGroup>(), nombre + " sin LODGroup");
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(g);
                Assert.IsTrue(flags.HasFlag(StaticEditorFlags.OccludeeStatic) && flags.HasFlag(StaticEditorFlags.BatchingStatic), nombre + " sin flags estáticos");
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }

        [TestCase("CE01_EscaleraCerro")]
        [TestCase("ML02_EscaleraPlaya")]
        public void Escalera_TieneRampaConPendienteSubible(string nombre)
        {
            GameObject g = Instanciar(nombre);
            try
            {
                Transform rampa = g.transform.Find("Rampa");
                Assert.IsNotNull(rampa, nombre + " sin rampa");
                float pendiente = Vector3.Angle(rampa.forward, Vector3.ProjectOnPlane(rampa.forward, Vector3.up));
                Assert.Greater(pendiente, 10f, "rampa casi plana");
                Assert.Less(pendiente, 45f, "rampa más empinada que el slopeLimit del jugador");
            }
            finally
            {
                Object.DestroyImmediate(g);
            }
        }
    }
}

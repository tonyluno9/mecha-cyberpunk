using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1365: el prefab del Cerro, ya procesado con RendimientoCerroT1365.Aplicar(), tiene LODGroups, LOD1 barato y pocas luces con sombra.</summary>
    public class RendimientoCerroT1365Pruebas
    {
        private const float FraccionMaximaLod1 = 0.15f;

        private GameObject prefab;
        private Transform casas;

        [SetUp]
        public void SetUp()
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RendimientoCerroT1365.RutaPrefab);
            Assert.IsNotNull(prefab, "No existe " + RendimientoCerroT1365.RutaPrefab);
            casas = prefab.transform.Find("Casas");
            Assert.IsNotNull(casas, "El prefab no tiene la carpeta Casas");
        }

        private IEnumerable<Transform> Casitas()
        {
            return casas.Cast<Transform>().Where(t => RendimientoCerroT1365.EsCasaKit(t.name) || RendimientoCerroT1365.EsCasaTripo(t.name));
        }

        [Test]
        public void CadaCasaYCadaCasaDeTripo_TieneLodGroup()
        {
            List<Transform> lista = Casitas().ToList();
            Assert.Greater(lista.Count, 0, "No hay casas en Casas");
            List<string> sin = lista.Where(t => t.GetComponent<LODGroup>() == null).Select(t => t.name).ToList();
            Assert.IsEmpty(sin, "Casas sin LODGroup (¿falta correr Aplicar?): " + string.Join(", ", sin.Take(10)));
        }

        [Test]
        public void NingunRendererDeLasCasas_QuedaFueraDeTodosLosLod()
        {
            int sinLod = 0;
            string primero = null;
            foreach (Transform casa in Casitas())
            {
                LODGroup g = casa.GetComponent<LODGroup>();
                Assert.IsNotNull(g, casa.name + " sin LODGroup");
                HashSet<Renderer> enLod = new HashSet<Renderer>(g.GetLODs().SelectMany(l => l.renderers).Where(r => r != null));
                foreach (Renderer r in casa.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!enLod.Contains(r))
                    {
                        sinLod++;
                        primero = primero ?? casa.name + "/" + r.name;
                    }
                }
            }
            Assert.AreEqual(0, sinLod, "Renderers fuera de todo LOD, el primero: " + primero);
        }

        [Test]
        public void TriangulosDeLod1_SumanMenosDel15PorCientoDeLod0()
        {
            long lod0 = 0;
            long lod1 = 0;
            foreach (Transform casa in Casitas())
            {
                LOD[] lods = casa.GetComponent<LODGroup>().GetLODs();
                lod0 += lods[0].renderers.Where(r => r != null).Sum(r => RendimientoCerroT1365.TriangulosRenderer(r, 0));
                if (lods.Length > 1)
                {
                    lod1 += lods[1].renderers.Where(r => r != null).Sum(r => RendimientoCerroT1365.TriangulosRenderer(r, 0));
                }
            }
            Assert.Greater(lod0, 0L, "LOD0 sin triángulos");
            Assert.Less(lod1, lod0 * FraccionMaximaLod1, "LOD1 " + lod1 + " triángulos contra LOD0 " + lod0);
        }

        [Test]
        public void Luces_ConSombraSonCuatroOMenos_YAlcanceHasta18m()
        {
            Light[] luces = prefab.GetComponentsInChildren<Light>(true).Where(l => l.type != LightType.Directional).ToArray();
            int conSombra = luces.Count(l => l.shadows != LightShadows.None);
            Assert.LessOrEqual(conSombra, RendimientoCerroT1365.MaxLucesConSombra, "Luces con sombra en tiempo real: " + conSombra);
            Light mayor = luces.OrderByDescending(l => l.range).FirstOrDefault();
            if (mayor != null)
            {
                Assert.LessOrEqual(mayor.range, RendimientoCerroT1365.AlcanceMaximoLuz, "Luz " + mayor.name + " con alcance " + mayor.range);
            }
        }
    }
}

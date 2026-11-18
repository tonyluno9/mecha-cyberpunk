using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1364: el prefab del Cerro (ya variado con VariarCerroT1364.Aplicar) tiene cuartos y pisos extra, extras con collider y bien puestos, sin primitivos y con paleta amplia.</summary>
    public class VariarCerroT1364Pruebas
    {
        private const float MargenMinimo = 0.8f;
        private GameObject contenido;
        private List<VariarCerroT1364.CasaT> casas;

        [OneTimeSetUp]
        public void AntesDeTodo()
        {
            contenido = PrefabUtility.LoadPrefabContents(VariarCerroT1364.RutaPrefab);
            casas = VariarCerroT1364.Leer(contenido.transform);
        }

        [OneTimeTearDown]
        public void DespuesDeTodo()
        {
            if (contenido != null)
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static bool EsExtraDeAzotea(Transform t)
        {
            return t.name.StartsWith(VariarCerroT1364.Prefijo + "Cuarto") || t.name.StartsWith(VariarCerroT1364.Prefijo + "Det_");
        }

        [Test]
        public void Casas_ConCuartoOPisoExtra_EntreVeintePorCientoYTreintaYCinco()
        {
            Assert.Greater(casas.Count, 100, "no se leyeron las casas del Cerro");
            int conExtra = casas.Count(c => c.Raiz.Cast<Transform>().Any(t => t.name.StartsWith(VariarCerroT1364.Prefijo + "Cuarto") || t.name.StartsWith(VariarCerroT1364.Prefijo + "Piso")));
            float fraccion = (float)conExtra / casas.Count;
            Assert.GreaterOrEqual(fraccion, 0.20f, "con cuarto o piso extra: " + conExtra + " de " + casas.Count);
            Assert.LessOrEqual(fraccion, 0.35f, "con cuarto o piso extra: " + conExtra + " de " + casas.Count);
        }

        [Test]
        public void Extras_MayoresDeUnMetro_TienenCollider()
        {
            List<string> sin = new List<string>();
            foreach (Transform t in contenido.GetComponentsInChildren<Transform>(true))
            {
                if (t == contenido.transform || !t.name.StartsWith(VariarCerroT1364.Prefijo) || t.name.StartsWith("Extra_Mov") || t.name == VariarCerroT1364.NombreRegistro
                    || t.name == "Extra_Cables" || t.parent.name.StartsWith(VariarCerroT1364.Prefijo))
                {
                    continue;
                }
                if (VariarCerroT1364.Cubierta(t, out Bounds b) && Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) > 1f && t.GetComponentsInChildren<Collider>(true).Length == 0)
                {
                    sin.Add(t.name + " en " + t.parent.name);
                }
            }
            Assert.IsEmpty(sin, "Extra_ sin collider: " + string.Join(", ", sin.Take(10)));
        }

        [Test]
        public void Extras_DeAzotea_NoEstanACercaDeUnBordeQueDaAOtraAzotea()
        {
            List<string> mal = new List<string>();
            foreach (VariarCerroT1364.CasaT c in casas.Where(x => x.Kit))
            {
                bool[] bo = VariarCerroT1364.Bordes(c, casas);
                foreach (Transform t in c.Raiz)
                {
                    if (!EsExtraDeAzotea(t) || !VariarCerroT1364.Cubierta(t, out Bounds b))
                    {
                        continue;
                    }
                    float[] distancia = { b.min.x - c.X0, c.X1 - b.max.x, b.min.z - c.Z0, c.Z1 - b.max.z };
                    for (int e = 0; e < 4; e++)
                    {
                        if (bo[e] && distancia[e] < MargenMinimo - 0.01f)
                        {
                            mal.Add(t.name + " en " + c.Raiz.name + " borde " + e + " a " + distancia[e].ToString("F2") + " m");
                        }
                    }
                }
            }
            Assert.IsEmpty(mal, string.Join("; ", mal.Take(10)));
        }

        [Test]
        public void Cuerpos_DeCasasDistintas_NoSeEncimanMasDeDecimaDeMetro()
        {
            List<(VariarCerroT1364.CasaT a, VariarCerroT1364.CasaT b)> pares = VariarCerroT1364.CuerposEncimados(casas);
            Assert.IsEmpty(pares, "cuerpos encimados: " + string.Join("; ", pares.Take(10).Select(p => p.a.Raiz.name + " con " + p.b.Raiz.name)));
        }

        [Test]
        public void Renderers_NoUsanMallasPrimitivasDeUnity()
        {
            string[] nombres = { "Cube", "Plane", "Quad", "Cylinder", "Sphere", "Capsule" };
            List<string> mal = new List<string>();
            foreach (MeshFilter mf in contenido.GetComponentsInChildren<MeshFilter>(true))
            {
                bool esExtra = false;
                for (Transform t = mf.transform; t != null && t != contenido.transform; t = t.parent)
                {
                    esExtra |= t.name.StartsWith(VariarCerroT1364.Prefijo);
                }
                // VentanaCalida es el vidrio luminoso del kit que viene dentro de las fachadas que se copian para el piso extra: no es nuevo.
                if (!esExtra || mf.name == "VentanaCalida")
                {
                    continue;
                }
                Mesh m = mf.sharedMesh;
                if (m != null && (AssetDatabase.GetAssetPath(m) == "Library/unity default resources" || nombres.Contains(m.name)))
                {
                    mal.Add(mf.transform.name + " usa " + m.name);
                }
            }
            Assert.IsEmpty(mal, string.Join("; ", mal.Take(10)));
        }

        [Test]
        public void Fachadas_TienenAlMenosOchoColoresDistintos()
        {
            HashSet<Color> colores = new HashSet<Color>();
            foreach (VariarCerroT1364.CasaT c in casas.Where(x => x.Kit))
            {
                foreach (Renderer r in c.Raiz.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m != null && m.name.StartsWith("T1364_Fachada_"))
                        {
                            colores.Add(m.GetColor(m.HasProperty("_BaseColor") ? "_BaseColor" : "_Color"));
                        }
                    }
                }
            }
            Assert.GreaterOrEqual(colores.Count, 8, "colores distintos en fachadas: " + colores.Count);
        }
    }
}

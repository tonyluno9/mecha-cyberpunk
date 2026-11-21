using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Barrio = MechaCyberpunk.Arte.Editor.NpcsBarriosT1542.Barrio;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-1542: NPC civiles por barrio. La primera parte es lógica pura (barrios, reparto, orientación de los que platican y de los vendedores).
    /// La segunda abre Ciudad_Prologo y necesita haber corrido NpcsBarriosT1542.Aplicar.
    /// </summary>
    public class NpcsBarriosT1542Pruebas
    {
        private static readonly string[] Codigos = { "NP07", "NP08", "NP09", "NP10", "NP11", "NP12", "NP13", "NP14" };

        private static System.Collections.Generic.IEnumerable<string> Visuales(Barrio b) => NpcsBarriosT1542.Reparto[b].Select(e => e.visual);

        [TestCase(-55.5f, -77f, Barrio.Suburbios)]   // la casa del jugador
        [TestCase(-37f, -71.7f, Barrio.Mercado)]     // x >= -40
        [TestCase(-92f, 8.8f, Barrio.Suburbios)]
        [TestCase(0f, 0f, Barrio.Mercado)]           // el tianguis
        [TestCase(-29.3f, 26f, Barrio.Mercado)]      // el mercado de armas
        [TestCase(57.5f, 47f, Barrio.NeonAlto)]      // el casino
        [TestCase(75f, 0f, Barrio.NeonAlto)]
        [TestCase(75f, -16f, Barrio.Nova)]
        [TestCase(94f, -43f, Barrio.Nova)]           // la avenida de Nova
        [TestCase(-300f, 0f, Barrio.Fuera)]          // las afueras
        public void BarrioDe_PuntosDelMapa(float x, float z, Barrio esperado)
        {
            Assert.AreEqual(esperado, NpcsBarriosT1542.BarrioDe(new Vector3(x, 0.2f, z)));
        }

        [Test]
        public void Reparto_SuburbiosYMercado_SonDeEstibadorPescadorRepartidorEloteroYAbuela()
        {
            foreach (Barrio b in new[] { Barrio.Suburbios, Barrio.Mercado })
            {
                foreach (string v in Visuales(b))
                {
                    Assert.IsTrue(new[] { "NP07", "NP08", "NP10", "NP11", "NP12", "NP13" }.Any(c => v.StartsWith(c + "_")), v + " no va en " + b);
                    Assert.IsFalse(v.StartsWith("NP14_") || v.StartsWith("NP09_"), "oficinista y corredor no van en " + b);
                }
            }
        }

        [Test]
        public void Reparto_NeonAltoYNova_SonDeOficinistaTuristaYCorredor()
        {
            foreach (Barrio b in new[] { Barrio.NeonAlto, Barrio.Nova })
            {
                foreach (string v in Visuales(b))
                {
                    Assert.IsTrue(new[] { "NP08", "NP09", "NP13", "NP14" }.Any(c => v.StartsWith(c + "_")), v + " no va en " + b);
                    Assert.IsFalse(v.StartsWith("NP07_") || v.StartsWith("NP10_") || v.StartsWith("NP11_") || v.StartsWith("NP12_"), "pescador, elotero, estibador y abuela no van en " + b);
                }
                Assert.IsTrue(Visuales(b).Any(v => v.StartsWith("NP14_")), "oficinista en " + b);
                Assert.IsTrue(Visuales(b).Any(v => v.StartsWith("NP09_")), "corredor en " + b);
            }
        }

        [Test]
        public void Reparto_CadaNpcCivilSaleEnAlgunBarrio_YTodosLosVisualesExisten()
        {
            var todos = NpcsBarriosT1542.Reparto.Values.SelectMany(v => v).Select(e => e.visual).Concat(NpcsBarriosT1542.VendedoresTianguis).Concat(NpcsBarriosT1542.ElencoPlaza).Distinct().ToList();
            foreach (string codigo in Codigos)
            {
                Assert.IsTrue(todos.Any(v => v.StartsWith(codigo + "_")), codigo + " no sale en ningún barrio");
            }
            foreach (string v in todos)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(v)), "falta Visual_" + v);
                Assert.IsTrue(v.Substring(0, 4).CompareTo("NP07") >= 0 && v.Substring(0, 4).CompareTo("NP14") <= 0, v + " no es un NP07 a NP14 con rig");
            }
        }

        [Test]
        public void Reparto_CadaBarrioTieneGenteYPesosPositivos()
        {
            foreach (Barrio b in new[] { Barrio.Suburbios, Barrio.Mercado, Barrio.NeonAlto, Barrio.Nova })
            {
                Assert.Greater(NpcsBarriosT1542.Reparto[b].Length, 3, b.ToString());
                Assert.IsTrue(NpcsBarriosT1542.Reparto[b].All(e => e.peso > 0f), b.ToString());
                Assert.IsTrue(NpcsBarriosT1542.Cantidad.ContainsKey(b), b.ToString());
            }
        }

        [Test]
        public void ElegirVisual_EsEstable_YRecorreLaLista()
        {
            Assert.AreEqual(NpcsBarriosT1542.ElegirVisual(Barrio.Nova, 0.37f), NpcsBarriosT1542.ElegirVisual(Barrio.Nova, 0.37f));
            Assert.AreEqual(NpcsBarriosT1542.Reparto[Barrio.Nova][0].visual, NpcsBarriosT1542.ElegirVisual(Barrio.Nova, 0f));
            Assert.AreEqual(NpcsBarriosT1542.Reparto[Barrio.Nova].Last().visual, NpcsBarriosT1542.ElegirVisual(Barrio.Nova, 1f));
            Assert.IsNull(NpcsBarriosT1542.ElegirVisual(Barrio.Fuera, 0.5f));
            Assert.IsNull(NpcsBarriosT1542.ElegirVisual(Barrio.Mercado, 0.5f, _ => false), "sin prefabs no sale nadie");
        }

        [Test]
        public void ElegirVisual_SoloUsaLosNp01ANp06CuandoYaTienenVisual()
        {
            bool Solo(string v) => !v.StartsWith("NP0") || v.StartsWith("NP07") || v.StartsWith("NP08") || v.StartsWith("NP09");
            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                string v = NpcsBarriosT1542.ElegirVisual(Barrio.Mercado, t, Solo);
                Assert.IsTrue(Solo(v), v);
            }
            bool sale = false;
            for (float t = 0f; t <= 1f; t += 0.01f)
            {
                sale |= NpcsBarriosT1542.ElegirVisual(Barrio.Mercado, t, _ => true) == "NP05_Vendedor";
            }
            Assert.IsTrue(sale, "con rig, NP05 entra al reparto del Mercado");
        }

        [TestCase(0f, 0f, 1f)]
        [TestCase(0f, 0f, -1f)]
        [TestCase(10f, -4f, 0.3f)]
        public void Pareja_SeMiranDeFrente(float x, float z, float angulo)
        {
            Vector3 tangente = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
            var p = NpcsBarriosT1542.Pareja(new Vector3(x, 0.2f, z), tangente);
            Assert.AreEqual(NpcsBarriosT1542.SeparacionPareja, Vector3.Distance(p.a, p.b), 1e-3f);
            Assert.IsTrue(NpcsBarriosT1542.SeMiranDeFrente(p.a, p.yawA, p.b, p.yawB, 0.5f));
            Vector3 mira = Quaternion.Euler(0f, p.yawA, 0f) * Vector3.forward;
            Assert.Greater(Vector3.Dot(mira, (p.b - p.a).normalized), 0.999f);
        }

        [Test]
        public void SeMiranDeFrente_DetectaQueUnoDaLaEspalda()
        {
            Vector3 a = Vector3.zero;
            Vector3 b = new Vector3(0f, 0f, 1f);
            Assert.IsTrue(NpcsBarriosT1542.SeMiranDeFrente(a, 0f, b, 180f));
            Assert.IsFalse(NpcsBarriosT1542.SeMiranDeFrente(a, 0f, b, 0f), "B le da la espalda a A");
            Assert.IsFalse(NpcsBarriosT1542.SeMiranDeFrente(a, 14f * 2f, b, 180f), "28 grados de error no es de frente");
        }

        [TestCase(3)]
        [TestCase(2)]
        public void Corro_TodosMiranAlCentro(int cuantos)
        {
            Vector3 centro = new Vector3(5f, 0.2f, -3f);
            var corro = NpcsBarriosT1542.Corro(centro, cuantos, 0.6f, 30f);
            Assert.AreEqual(cuantos, corro.Count);
            foreach ((Vector3 pos, float yaw) in corro)
            {
                Assert.AreEqual(0.6f, Vector3.Distance(pos, centro), 1e-3f);
                Vector3 mira = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                Assert.Greater(Vector3.Dot(mira, (centro - pos).normalized), 0.999f);
            }
        }

        [TestCase(7.7f, 180f)]
        [TestCase(4.7f, 180f)]
        [TestCase(-4.7f, 0f)]
        [TestCase(-7.7f, 0f)]
        public void Vendedor_VaDetrasDelPuesto_DeFrenteALaCalle(float zPuesto, float yawEsperado)
        {
            Vector3 puesto = new Vector3(-12f, 0.2f, zPuesto);
            (Vector3 pos, float yaw) = NpcsBarriosT1542.Vendedor(puesto);
            Assert.AreEqual(yawEsperado, Mathf.Abs(Mathf.DeltaAngle(yaw, 0f)) < 90f ? 0f : 180f, "mira hacia la calle (z = 0)");
            Assert.AreEqual(puesto.x, pos.x, 1e-4f);
            Assert.AreEqual(NpcsBarriosT1542.DistanciaVendedorAlPuesto, Mathf.Abs(pos.z - puesto.z), 1e-3f);
            Assert.Greater(Mathf.Abs(pos.z), Mathf.Abs(puesto.z), "detrás de la mesa, más lejos de la calle");
            Vector3 mira = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Assert.Less(Mathf.Abs((pos + mira * Mathf.Abs(pos.z)).z), 1e-3f, "caminando hacia donde mira se llega a la calle");
        }

        [Test]
        public void Azar_EsEstableYEstaEntreCeroYUno()
        {
            Assert.AreEqual(NpcsBarriosT1542.Azar("x"), NpcsBarriosT1542.Azar("x"));
            Assert.AreNotEqual(NpcsBarriosT1542.Azar("x"), NpcsBarriosT1542.Azar("y"));
            for (int i = 0; i < 50; i++)
            {
                float a = NpcsBarriosT1542.Azar("clave" + i);
                Assert.That(a, Is.InRange(0f, 0.9999f));
            }
        }

        [Test]
        public void GenteDeBanqueta_SinPareja_NoSeUneAUnaPlatica()
        {
            var go = new GameObject("Vendedor");
            try
            {
                GenteDeBanqueta g = go.AddComponent<GenteDeBanqueta>();
                Assert.IsFalse(g.SinPareja);
                g.FijarSinPareja(true);
                Assert.IsTrue(g.SinPareja);
                Assert.IsNull(g.Companero);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ------------------------------------------------------------------ escena (necesita Aplicar)

        private const string RutaEscena = NpcsBarriosT1542.RutaEscena;

        private static Scene AbrirConAplicar()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            if (!escena.GetRootGameObjects().Any(g => g.name == NpcsBarriosT1542.Grupo))
            {
                EditorSceneManager.CloseScene(escena, true);
                Assert.Inconclusive("Corre antes NpcsBarriosT1542.Aplicar (falta " + NpcsBarriosT1542.Grupo + " en Ciudad_Prologo).");
            }
            return escena;
        }

        [Test]
        public void Escena_LosQuePlaticanDeParejaSeMiranDeFrente()
        {
            Scene escena = AbrirConAplicar();
            try
            {
                var pares = NpcsBarriosT1542.ParejasQuePlatican(escena);
                Assert.Greater(pares.Count, 0, "hay parejas de T-508");
                foreach ((Transform a, Transform b) in pares)
                {
                    GenteDeBanqueta g = a.GetComponentInChildren<GenteDeBanqueta>(true);
                    if (g != null && g.Modo == ModoGente.Caminar)
                    {
                        continue; // las que caminan van juntas, no de frente
                    }
                    Assert.IsTrue(NpcsBarriosT1542.SeMiranDeFrente(a.position, a.eulerAngles.y, b.position, b.eulerAngles.y), a.name + " y " + b.name + " no se miran");
                }
                Transform grupo = escena.GetRootGameObjects().First(g => g.name == NpcsBarriosT1542.Grupo).transform;
                foreach (Transform t in grupo.GetComponentsInChildren<Transform>(true).Where(x => x.name.EndsWith("a") && x.name.Contains("Platica_")))
                {
                    Transform otro = t.parent.Find(t.name.Substring(0, t.name.Length - 1) + "b");
                    if (otro != null)
                    {
                        Assert.IsTrue(NpcsBarriosT1542.SeMiranDeFrente(t.position, t.eulerAngles.y, otro.position, otro.eulerAngles.y), t.name + " no mira a su pareja");
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_CadaPuestoDelTianguisTieneVendedorDetras_DeFrenteALaCalle()
        {
            Scene escena = AbrirConAplicar();
            try
            {
                var puestos = NpcsBarriosT1542.PuestosDelTianguis(escena);
                Assert.Greater(puestos.Count, 0);
                Transform grupo = escena.GetRootGameObjects().First(g => g.name == NpcsBarriosT1542.Grupo).transform;
                foreach (Transform puesto in puestos)
                {
                    Transform v = grupo.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Vendedor_" + puesto.name);
                    Assert.IsNotNull(v, "falta el vendedor de " + puesto.name);
                    (Vector3 esperado, float yaw) = NpcsBarriosT1542.Vendedor(puesto.position);
                    Assert.Less(Vector2.Distance(new Vector2(v.position.x, v.position.z), new Vector2(esperado.x, esperado.z)), 0.01f, puesto.name);
                    Assert.Less(Mathf.Abs(Mathf.DeltaAngle(v.eulerAngles.y, yaw)), 1f, puesto.name);
                    Assert.AreEqual(Barrio.Mercado, NpcsBarriosT1542.BarrioDe(v.position));
                    GenteDeBanqueta g = v.GetComponent<GenteDeBanqueta>();
                    Assert.IsNotNull(g, "el vendedor " + puesto.name + " no tiene comportamiento");
                    Assert.IsNotNull(g.ClipPlatica, "el vendedor " + puesto.name + " no tiene clip");
                    Assert.IsTrue(g.SinPareja);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_LosNuevosEstanEnSuBarrio_YLosSentadosSonDeNpc()
        {
            Scene escena = AbrirConAplicar();
            try
            {
                Transform grupo = escena.GetRootGameObjects().First(g => g.name == NpcsBarriosT1542.Grupo).transform;
                foreach (Transform barrio in grupo.Cast<Transform>().Where(h => System.Enum.IsDefined(typeof(Barrio), h.name)))
                {
                    var esperado = (Barrio)System.Enum.Parse(typeof(Barrio), barrio.name);
                    foreach (Transform t in barrio.GetComponentsInChildren<GenteDeBanqueta>(true).Select(g => g.transform).Where(x => x.name.StartsWith("Platica_") || x.name.StartsWith("Solo_") || x.name.StartsWith("Trio_")))
                    {
                        Assert.AreEqual(esperado, NpcsBarriosT1542.BarrioDe(t.position), t.name + " cayó fuera de " + esperado);
                    }
                }
                foreach (NpcSentadoBanca s in escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<NpcSentadoBanca>(true)))
                {
                    Assert.IsTrue(s.Banca != null && Vector3.Distance(s.transform.position, s.Banca.position) < 1.5f, s.name + " lejos de su banca");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

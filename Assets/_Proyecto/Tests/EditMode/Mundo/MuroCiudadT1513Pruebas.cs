using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1513: muro de la ciudad con modelos MU. Las pruebas de plan son puras; las de escena piden haber corrido
    /// MuroCiudadT1513.Aplicar (la escena guardada).
    /// </summary>
    public class MuroCiudadT1513Pruebas
    {
        private const float HuecoMaximo = 0.5f;
        private static readonly Vector2 TamTorre = new Vector2(6.6f, 6.9f);

        private static List<CalculoMuroT1513.Segmento> Anillo()
        {
            // Mismo trazo que el muro viejo: x = +-135, z = +-88, con salidas de 20 m en z = 0 y x = +-47.
            return new List<CalculoMuroT1513.Segmento>
            {
                S(135, -88, 135, -10), S(135, 10, 135, 88),
                S(-135, -88, -135, -10), S(-135, 10, -135, 88),
                S(-135, 88, -57, 88), S(-37, 88, 37, 88), S(57, 88, 135, 88),
                S(-135, -88, -57, -88), S(-37, -88, 37, -88), S(57, -88, 135, -88),
            };
        }

        private static CalculoMuroT1513.Segmento S(float ax, float az, float bx, float bz)
        {
            return new CalculoMuroT1513.Segmento { A = new Vector3(ax, 0f, az), B = new Vector3(bx, 0f, bz) };
        }

        [Test]
        public void Planear_Anillo_CuatroEsquinasYSeisSalidas()
        {
            CalculoMuroT1513.Plan plan = CalculoMuroT1513.Planear(Anillo(), TamTorre);
            Assert.AreEqual(4, plan.Torres.Count(t => t.Esquina), "torres en esquinas");
            Assert.AreEqual(12, plan.Torres.Count(t => !t.Esquina), "dos torres por salida");
            Assert.AreEqual(6, plan.Puertas.Count, "una puerta por salida");
            Assert.IsTrue(plan.Puertas.All(p => Mathf.Abs(p.Ancho - 20f) < 0.1f), "las puertas van en huecos de 20 m");
        }

        [Test]
        public void Planear_TorresEnEsquinas_QuedanEnLosVertices()
        {
            CalculoMuroT1513.Plan plan = CalculoMuroT1513.Planear(Anillo(), TamTorre);
            foreach (Vector3 esquina in new[] { new Vector3(135, 0, 88), new Vector3(-135, 0, 88), new Vector3(135, 0, -88), new Vector3(-135, 0, -88) })
            {
                Assert.IsTrue(plan.Torres.Any(t => t.Esquina && Vector3.Distance(t.Centro, esquina) < 0.5f), "falta torre en " + esquina);
            }
        }

        [Test]
        public void Repartir_TramoDe78m_CierraDentroDelAjuste()
        {
            CalculoMuroT1513.Reparto r = CalculoMuroT1513.Repartir(65f, 13.8f, 15f);
            Assert.IsTrue(r.DentroDelAjuste);
            float suma = 0f;
            for (int k = 0; k < r.EsB.Count; k++)
            {
                suma += (r.EsB[k] ? 15f : 13.8f) * r.Factor;
            }
            Assert.AreEqual(65f, suma, 0.01f, "las piezas suman el largo del tramo");
            Assert.LessOrEqual(Mathf.Abs(r.Factor - 1f), CalculoMuroT1513.AjusteMaximo + 0.001f);
        }

        [Test]
        public void Repartir_AlternaAyB()
        {
            CalculoMuroT1513.Reparto r = CalculoMuroT1513.Repartir(70f, 13.8f, 15f);
            for (int k = 0; k < r.EsB.Count; k++)
            {
                Assert.AreEqual(k % 2 == 1, r.EsB[k]);
            }
        }

        [Test]
        public void Planear_TramosMasTorres_CubrenCadaSegmentoSinHuecos()
        {
            List<CalculoMuroT1513.Segmento> segs = Anillo();
            CalculoMuroT1513.Plan plan = CalculoMuroT1513.Planear(segs, TamTorre);
            for (int i = 0; i < segs.Count; i++)
            {
                Vector3 d = (segs[i].B - segs[i].A).normalized;
                float largo = Vector3.Distance(segs[i].A, segs[i].B);
                var intervalos = new List<Vector2>();
                CalculoMuroT1513.Tramo t = plan.Tramos[i];
                float a = Vector3.Dot(t.Inicio - segs[i].A, d);
                intervalos.Add(new Vector2(a, a + t.Largo));
                foreach (CalculoMuroT1513.Torre torre in plan.Torres)
                {
                    Vector3 v = torre.Centro - segs[i].A;
                    float lateral = Mathf.Abs(Vector3.Dot(v, new Vector3(-d.z, 0f, d.x)));
                    float mitad = Mathf.Abs(d.x) * TamTorre.x * 0.5f + Mathf.Abs(d.z) * TamTorre.y * 0.5f;
                    if (lateral < 1f)
                    {
                        float c = Vector3.Dot(v, d);
                        intervalos.Add(new Vector2(c - mitad, c + mitad));
                    }
                }
                intervalos = intervalos.OrderBy(x => x.x).ToList();
                float cubierto = intervalos[0].y;
                Assert.LessOrEqual(intervalos[0].x, HuecoMaximo, "inicio del segmento " + i);
                for (int k = 1; k < intervalos.Count; k++)
                {
                    Assert.LessOrEqual(intervalos[k].x - cubierto, HuecoMaximo, "hueco en el segmento " + i);
                    cubierto = Mathf.Max(cubierto, intervalos[k].y);
                }
                Assert.GreaterOrEqual(cubierto, largo - HuecoMaximo, "final del segmento " + i);
            }
        }

        // ---- Escena (despues de MuroCiudadT1513.Aplicar) ----

        private static Scene Escena()
        {
            return EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Additive);
        }

        [Test]
        public void Escena_MuroNuevo_TieneTramosTorresYPuertas()
        {
            Scene escena = Escena();
            try
            {
                GameObject raiz = escena.GetRootGameObjects().FirstOrDefault(g => g.name == MuroCiudadT1513.NombreRaiz);
                Assert.IsNotNull(raiz, "falta " + MuroCiudadT1513.NombreRaiz + ": correr MuroCiudadT1513.Aplicar");
                Transform[] hijos = raiz.transform.Cast<Transform>().ToArray();
                Assert.Greater(hijos.Count(h => h.name.StartsWith("Muro_")), 20, "tramos");
                Assert.AreEqual(4, hijos.Count(h => h.name.StartsWith("TorreEsquina_")), "torres en esquinas");
                Assert.AreEqual(6, hijos.Count(h => h.name.StartsWith("Puerta_")), "puertas");
                Assert.IsTrue(raiz.GetComponentsInChildren<Collider>(true).All(c => !c.enabled), "las piezas nuevas no llevan colliders activos");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_MuroViejo_SinRenderersPeroConCollider()
        {
            Scene escena = Escena();
            try
            {
                GameObject orillas = escena.GetRootGameObjects().Select(g => g.transform.Find("Orillas")).Where(t => t != null).Select(t => t.gameObject).FirstOrDefault();
                Assert.IsNotNull(orillas, "no encontré Orillas");
                Transform[] muros = orillas.transform.Cast<Transform>().Where(t => t.name == "Muro").ToArray();
                Assert.AreEqual(10, muros.Length);
                foreach (Transform m in muros)
                {
                    Assert.IsTrue(m.GetComponents<Renderer>().All(r => !r.enabled), "renderer encendido en " + m.position);
                    BoxCollider c = m.GetComponent<BoxCollider>();
                    Assert.IsNotNull(c);
                    Assert.IsTrue(c.enabled, "collider apagado en " + m.position);
                    Assert.IsTrue(m.gameObject.activeSelf);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_LimitesInvisibles_SiguenActivos()
        {
            Scene escena = Escena();
            try
            {
                Transform[] limites = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "LimiteInvisible").ToArray();
                Assert.Greater(limites.Length, 0);
                Assert.IsTrue(limites.Where(t => t.GetComponent<BoxCollider>() != null).All(t => t.GetComponent<BoxCollider>().enabled),
                    "algún LimiteInvisible con collider apagado");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

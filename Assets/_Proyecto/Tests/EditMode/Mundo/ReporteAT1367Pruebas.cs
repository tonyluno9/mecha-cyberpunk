using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1367: los prefabs del Cerro y del Puerto ya arreglados (se corren después de ArreglarReporteAT1367.Aplicar).</summary>
    public class ReporteAT1367Pruebas
    {
        private GameObject cerro;
        private GameObject puerto;

        [OneTimeSetUp]
        public void Abrir()
        {
            cerro = PrefabUtility.LoadPrefabContents(ArreglarReporteAT1367.RutaCerro);
            puerto = PrefabUtility.LoadPrefabContents(ArreglarReporteAT1367.RutaPuerto);
            Physics.SyncTransforms();
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            PrefabUtility.UnloadPrefabContents(cerro);
            PrefabUtility.UnloadPrefabContents(puerto);
        }

        private Transform Buscar(string ruta)
        {
            Transform t = cerro.transform.Find(ruta);
            Assert.IsNotNull(t, "Falta " + ruta);
            return t;
        }

        [Test]
        public void Ladera_Vertices_NingunoEnElTuboNiEnLaBoca()
        {
            Transform t = Buscar("Terreno/Ladera");
            Mesh m = t.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(0, ArreglarReporteAT1367.VerticesEnTubo(m, t.localToWorldMatrix), "vértices dentro del tubo");
            Assert.AreEqual(0, ArreglarReporteAT1367.VerticesEnBoca(m, t.localToWorldMatrix), "vértices dentro de la boca");
            Assert.AreSame(m, t.GetComponent<MeshCollider>().sharedMesh, "el collider usa la misma malla");
        }

        [Test]
        public void Ladera_CorredorDeLasCalles_NingunPuntoMasDe5cmSobreLaCalle()
        {
            PhysicsScene f = cerro.scene.GetPhysicsScene();
            RaycastHit[] hits = new RaycastHit[64];
            int muestras = 0;
            foreach (var cz in cerro.GetComponentsInChildren<MechaCyberpunk.Core.Mundo.CalleZona>(false))
            {
                if (cz.Autopista)
                {
                    continue;
                }
                Vector3[] pts = cz.Puntos().ToArray();
                // Se miran el eje, los bordes del arroyo y 1 m de banqueta (a 4 m de rejilla, más afuera el borde de la malla se mezcla con la ladera).
                float lateralMax = cz.Ancho * 0.5f + 1f;
                for (int i = 1; i < pts.Length; i++)
                {
                    Vector3 d = pts[i] - pts[i - 1];
                    float largo = new Vector2(d.x, d.z).magnitude;
                    if (largo < 1e-3f)
                    {
                        continue;
                    }
                    Vector3 lado = new Vector3(-d.z, 0f, d.x).normalized;
                    for (float s = 0f; s < largo; s += 1f)
                    {
                        Vector3 p = pts[i - 1] + d * (s / largo);
                        foreach (float l in new[] { -lateralMax, -cz.Ancho * 0.5f, 0f, cz.Ancho * 0.5f, lateralMax })
                        {
                            Vector3 q = p + lado * l;
                            int n = f.Raycast(new Vector3(q.x, p.y + 60f, q.z), Vector3.down, hits, 120f, ~0, QueryTriggerInteraction.Ignore);
                            float yT = float.NegativeInfinity;
                            for (int h = 0; h < n; h++)
                            {
                                if (hits[h].collider.name == "Ladera" && hits[h].point.y > yT)
                                {
                                    yT = hits[h].point.y;
                                }
                            }
                            muestras++;
                            if (!float.IsNegativeInfinity(yT))
                            {
                                Assert.LessOrEqual(yT, p.y + 0.05f, cz.name + " en " + q + ": terreno " + yT + " sobre calle " + p.y);
                            }
                        }
                    }
                }
            }
            Assert.Greater(muestras, 100, "se esperaban muestras de calles");
        }

        [Test]
        public void UnionU4_Arroyos_AlturaCeroEnLaUnion()
        {
            foreach (string ruta in new[] { "Calles/Calle_Base/Arroyo", "Calles/Calle_U4/Arroyo" })
            {
                Transform t = Buscar(ruta);
                Mesh m = t.GetComponent<MeshFilter>().sharedMesh;
                foreach (Vector3 v in m.vertices)
                {
                    Vector3 w = t.localToWorldMatrix.MultiplyPoint3x4(v);
                    if (w.x >= -312f && w.x <= -296f && Mathf.Abs(w.z) <= 8f)
                    {
                        Assert.That(w.y, Is.InRange(-0.02f, 0.02f), ruta + " vértice " + w);
                    }
                }
            }
        }

        [Test]
        public void UnionU4_Banquetas_NoCruzanElArroyoVecino()
        {
            Transform b = Buscar("Calles/Calle_Base/Banquetas");
            Assert.AreEqual(0, ArreglarReporteAT1367.TriangulosEn(b.GetComponent<MeshFilter>().sharedMesh, b.localToWorldMatrix, ArreglarReporteAT1367.EnArroyoU4));
            Transform u = Buscar("Calles/Calle_U4/Banquetas");
            Assert.AreEqual(0, ArreglarReporteAT1367.TriangulosEn(u.GetComponent<MeshFilter>().sharedMesh, u.localToWorldMatrix, ArreglarReporteAT1367.EnArroyoBase));
        }

        [Test]
        public void Arroyos_Pares_SinPlanosCoplanaresEncimados()
        {
            string[] rutas = { "Calles/Calle_Base/Arroyo", "Calles/Calle_U4/Arroyo", "Calles/Calle_Zigzag/Arroyo" };
            for (int i = 0; i < rutas.Length; i++)
            {
                for (int j = 0; j < rutas.Length; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    Transform a = Buscar(rutas[i]), b = Buscar(rutas[j]);
                    float area = ArreglarReporteAT1367.AreaCoplanar(a.GetComponent<MeshFilter>().sharedMesh, a.localToWorldMatrix,
                        b.GetComponent<MeshFilter>().sharedMesh, b.localToWorldMatrix, ArreglarReporteAT1367.ToleranciaCoplanar);
                    Assert.Less(area, 0.05f, rutas[i] + " sobre " + rutas[j]);
                }
            }
        }

        [Test]
        public void PuertaNorte_PasoDe10m_SinColliders()
        {
            PhysicsScene f = puerto.scene.GetPhysicsScene();
            var cols = ArreglarReporteAT1367.ColisionadoresDelPaso(f);
            Assert.AreEqual(0, cols.Count, string.Join(", ", cols.Select(c => c.transform.parent != null ? c.transform.parent.name + "/" + c.name : c.name)));
        }

        [Test]
        public void Puerto_PuntosDelGolpeYDelAtoro_SinColliders()
        {
            PhysicsScene f = puerto.scene.GetPhysicsScene();
            foreach (Vector3 p in new[] { ArreglarReporteAT1367.PuntoGolpe, ArreglarReporteAT1367.PuntoAtoro })
            {
                var cols = ArreglarReporteAT1367.ColisionadoresDeCarro(f, p);
                Assert.AreEqual(0, cols.Count, p + ": " + string.Join(", ", cols.Select(c => c.name)));
            }
        }
    }
}

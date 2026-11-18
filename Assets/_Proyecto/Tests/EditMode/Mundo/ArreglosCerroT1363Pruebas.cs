using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Core.Trepar;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1363: el prefab del Cerro ya arreglado (instancia en una escena vacía, con física).</summary>
    public class ArreglosCerroT1363Pruebas
    {
        private const float TolerExtremoEscalera = 0.15f;
        private const float TolerAcceso = 0.3f;
        private const float DistanciaMinimaCarro = 2f;
        private const float AnchoMaximoPoste = 1.5f;

        private GameObject zona;

        [OneTimeSetUp]
        public void ArmarZona()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArreglarCerroT1363.RutaPrefab);
            Assert.IsNotNull(prefab, "Falta el prefab del Cerro");
            zona = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Physics.SyncTransforms();
        }

        [OneTimeTearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(zona);
        }

        [Test]
        public void Cimientos_Todos_TienenCollider()
        {
            Transform casas = zona.transform.Find("Casas");
            List<Transform> cimientos = casas.Cast<Transform>().Where(t => t.name == "Cimiento").ToList();
            Assert.Greater(cimientos.Count, 600);
            Assert.AreEqual(0, cimientos.Count(t => t.GetComponent<Collider>() == null));
        }

        [Test]
        public void Carros_Estacionados_NoEstanEnElCarril()
        {
            CalleZona[] calles = zona.GetComponentsInChildren<CalleZona>();
            Transform carros = zona.transform.Find("Carros");
            int n = 0;
            foreach (Transform car in carros)
            {
                if (!car.name.StartsWith("CarroViejo"))
                {
                    continue;
                }
                n++;
                float min = float.MaxValue;
                foreach (CalleZona cz in calles)
                {
                    foreach (Vector3 s in BarridoZonasT1360.Muestrear(cz.Puntos(), 0.5f, 0f))
                    {
                        min = Mathf.Min(min, new Vector2(s.x - car.position.x, s.z - car.position.z).magnitude);
                    }
                }
                Assert.GreaterOrEqual(min, DistanciaMinimaCarro, car.name + " está a " + min + " m del eje");
            }
            Assert.AreEqual(8, n);
        }

        [Test]
        public void Escaleras_Extremos_LlegaranALaCalle()
        {
            Transform esc = zona.transform.Find("Escaleras");
            Transform calles = zona.transform.Find("Calles");
            Transform terreno = zona.transform.Find("Terreno");
            var cadenas = esc.Cast<Transform>().Where(t => t.name == "CE01_EscaleraCerro").GroupBy(t => Mathf.RoundToInt(t.position.z)).ToList();
            Assert.AreEqual(3, cadenas.Count);
            foreach (var g in cadenas)
            {
                List<Transform> orden = g.OrderBy(t => t.position.x).ToList();
                float z = g.Key;
                float xArriba = orden[0].position.x - 2f;
                float xAbajo = orden[orden.Count - 1].position.x + 2f;
                float rampaArriba = Rampa(orden[0], xArriba + 0.1f, z);
                float rampaAbajo = Rampa(orden[orden.Count - 1], xAbajo - 0.1f, z);
                Assert.AreEqual(Suelo(calles, terreno, xArriba - 0.6f, z, rampaArriba + 5f), rampaArriba, TolerExtremoEscalera, "arriba z=" + z);
                Assert.AreEqual(Suelo(calles, terreno, xAbajo + 0.6f, z, rampaAbajo + 5f), rampaAbajo, TolerExtremoEscalera, "abajo z=" + z);
            }
        }

        private static float Rampa(Transform modulo, float x, float z)
        {
            BoxCollider r = modulo.GetComponentsInChildren<BoxCollider>().First(b => b.name.StartsWith("Rampa"));
            Assert.IsTrue(r.Raycast(new Ray(new Vector3(x, modulo.position.y + 30f, z), Vector3.down), out RaycastHit h, 60f), "sin rampa en x=" + x);
            return h.point.y;
        }

        private static float Suelo(Transform calles, Transform terreno, float x, float z, float yDesde)
        {
            float y = float.MinValue;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(x, yDesde, z), Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore))
            {
                if ((h.collider.transform.IsChildOf(calles) || h.collider.transform.IsChildOf(terreno)) && h.point.y > y)
                {
                    y = h.point.y;
                }
            }
            return y;
        }

        [Test]
        public void Accesos_LlegaranALaAzotea()
        {
            Transform detalles = zona.transform.Find("Detalles");
            Transform casas = zona.transform.Find("Casas");
            int n = 0;
            foreach (Transform t in detalles)
            {
                PuntoTrepar p = t.GetComponent<PuntoTrepar>();
                if (!t.name.StartsWith("Acceso_") || p == null)
                {
                    continue;
                }
                n++;
                Vector3 s = p.Salida;
                float techo = float.MinValue;
                foreach (RaycastHit h in Physics.RaycastAll(s + Vector3.up * 30f, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider.transform.IsChildOf(casas) && h.normal.y > 0.5f && h.point.y > techo)
                    {
                        techo = h.point.y;
                    }
                }
                Assert.AreEqual(techo, s.y, TolerAcceso, t.name);
            }
            Assert.Greater(n, 0);
        }

        [Test]
        public void Postes_Collider_NoEsUnMuroInvisible()
        {
            Transform postes = zona.transform.Find("Postes");
            int n = 0;
            foreach (Collider c in postes.GetComponentsInChildren<Collider>())
            {
                n++;
                Assert.LessOrEqual(Mathf.Max(c.bounds.size.x, c.bounds.size.z), AnchoMaximoPoste, c.transform.parent.name + "/" + c.name);
            }
            Assert.Greater(n, 0);
        }

        [Test]
        public void Barrido_SinColliderYCarroEnElCarril_EnCero()
        {
            List<BarridoZonasT1360.Hallazgo> r = BarridoZonasT1360.BarrerZona(zona).ToList();
            // Los tendederos (CC04) van sin collider a propósito: estorbaban los saltos entre azoteas y es ropa colgada (T-1373).
            Assert.AreEqual(0, r.Count(h => h.tipo == BarridoZonasT1360.TipoSinCollider && !h.ruta.Contains("CC04_Tendedero")));
            Assert.AreEqual(0, r.Count(h => h.tipo == BarridoZonasT1360.TipoCarro));
        }
    }
}

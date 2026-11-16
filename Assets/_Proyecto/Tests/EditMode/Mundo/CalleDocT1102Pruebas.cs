using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1102: la calle de x = -110.5 junto a la calle del Doc, rehecha con cuadras (raíz T1102_CalleDoc en Ciudad_Prologo).</summary>
    public class CalleDocT1102Pruebas
    {
        private const float XCalle = -110.5f;
        private const float ZCruce = -43f;
        private const float TolRemate = 3f;
        private const int PostesMinimos = 6;
        private const int FachadasMinimas = 8;
        private const int RayasPorCruce = 4;

        private static Transform Raiz()
        {
            ColocarS2.Abrir();
            GameObject g = GameObject.Find(ColocarT1102CalleDoc.Raiz);
            Assert.IsNotNull(g, "Falta la raíz " + ColocarT1102CalleDoc.Raiz + " en Ciudad_Prologo");
            return g.transform;
        }

        private static List<(string grupo, int n, int pisos, string color, Transform t)> Fachadas(Transform raiz)
        {
            var lista = new List<(string, int, int, string, Transform)>();
            foreach (Transform t in raiz.Find("Casas"))
            {
                string[] p = t.name.Split('_');
                if (p.Length < 4 || (p[0] != "Fachada" && p[0] != "Remate"))
                {
                    continue;
                }
                bool remate = p[0] == "Remate";
                string grupo = remate ? "Remate" : p[0] + p[1];
                int i = remate ? 1 : 2;
                lista.Add((grupo, int.Parse(p[i]), int.Parse(p[i + 1].TrimEnd('p')), p[i + 2], t));
            }
            return lista;
        }

        [Test]
        public void Raiz_Existe()
        {
            Assert.IsNotNull(Raiz());
        }

        [Test]
        public void Postes_AlMenosSeisConLuzEnLaFranjaYEnAmbosLados()
        {
            Transform postes = Raiz().Find("Postes");
            Assert.IsNotNull(postes);
            var conLuz = postes.Cast<Transform>().Where(p => p.GetComponentsInChildren<Light>(true).Any(l => l.type == LightType.Spot)).ToList();
            Assert.GreaterOrEqual(conLuz.Count, PostesMinimos, "Postes con luz");
            Assert.IsTrue(conLuz.All(p => Mathf.Abs(p.position.x - XCalle) < 8f && p.position.z < -10f && p.position.z > -90f), "Todos dentro de la calle");
            Assert.AreEqual(2, conLuz.Select(p => Mathf.Sign(p.position.x - XCalle)).Distinct().Count(), "Los postes se reparten en los dos lados");
        }

        [Test]
        public void FrentesViejos_DeLaFranja_ApagadosYSoloSeQuedanLosDeLosAccesos()
        {
            ColocarS2.Abrir();
            Transform casas = ColocarS2.Buscar("S2c_Suburbios/Casas");
            Assert.IsNotNull(casas);
            foreach (var c in ColocarT1102CalleDoc.CasasViejas)
            {
                Transform t = casas.Find(c.nombre);
                Assert.IsNotNull(t, c.nombre);
                Assert.IsFalse(t.gameObject.activeSelf, c.nombre + " debe estar apagada");
            }
            Transform postes = ColocarS2.Buscar("S2c_Suburbios/Postes");
            foreach (string p in ColocarT1102CalleDoc.PostesViejos)
            {
                Assert.IsFalse(postes.Find(p).gameObject.activeSelf, p + " debe estar apagado");
            }
            foreach (Transform t in casas)
            {
                if (!t.gameObject.activeSelf || t.name == "Esquina_K05")
                {
                    continue;
                }
                Renderer r = t.GetComponentInChildren<Renderer>();
                if (r == null)
                {
                    continue;
                }
                Vector3 c = r.bounds.center;
                if (c.x > -121f && c.x < -99f && c.z < -7f && c.z > -87f)
                {
                    CollectionAssert.Contains(ColocarT1102CalleDoc.CasasQueSeQuedan, t.name, t.name + " es un frente viejo activo en la franja");
                }
            }
        }

        [Test]
        public void IdiDelDoc_SigueActivo()
        {
            ColocarS2.Abrir();
            Transform f = ColocarS2.Buscar("S2p_Fachada_P12");
            Assert.IsNotNull(f);
            Assert.IsTrue(f.gameObject.activeInHierarchy);
        }

        [Test]
        public void Cruce_TieneDosSemaforosEnEsquinasOpuestas()
        {
            ColocarS2.Abrir();
            var semaforos = Object.FindObjectsByType<SemaforoCiclo>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Select(s => s.transform.position)
                .Where(p => p.x > -120f && p.x < -101f && p.z < -34f && p.z > -52f).ToList();
            Assert.GreaterOrEqual(semaforos.Count, 2, "Semáforos en el cruce con K01");
            // Esquinas opuestas: el signo de (x - XCalle) * (z - ZCruce) es el mismo y cada eje cambia de lado (NE con SO o NO con SE).
            bool opuestas = semaforos.Any(a => semaforos.Any(b =>
                (a.x - XCalle) * (b.x - XCalle) < 0f && (a.z - ZCruce) * (b.z - ZCruce) < 0f));
            Assert.IsTrue(opuestas, "Dos semáforos en esquinas opuestas");
        }

        [Test]
        public void Cruce_TieneLosCuatroCrucesPeatonalesPintados()
        {
            ColocarS2.Abrir();
            var rayas = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(r => r.name == "Linea").ToList();
            foreach ((string lado, Vector2 centro) in new[] { ("norte", new Vector2(XCalle, -37.4f)), ("sur", new Vector2(XCalle, -48.6f)), ("oeste", new Vector2(-117f, ZCruce)), ("este", new Vector2(-104f, ZCruce)) })
            {
                int n = rayas.Count(r => Mathf.Abs(r.bounds.center.x - centro.x) < 4.5f && Mathf.Abs(r.bounds.center.z - centro.y) < 4.5f && r.bounds.size.y < 0.1f);
                Assert.GreaterOrEqual(n, RayasPorCruce, "Rayas del cruce " + lado);
            }
        }

        [Test]
        public void Fachadas_AlMenosOchoDistintasYNingunaParejaVecinaIgual()
        {
            var f = Fachadas(Raiz());
            Assert.GreaterOrEqual(f.Count, FachadasMinimas, "Fachadas armadas");
            Assert.GreaterOrEqual(f.Select(x => x.pisos + "_" + x.color).Distinct().Count(), FachadasMinimas, "Combinaciones distintas de pisos y color");
            Assert.AreEqual(3, f.Select(x => x.pisos).Distinct().Count(), "Alturas de 1, 2 y 3 pisos mezcladas");
            foreach (var grupo in f.GroupBy(x => x.grupo))
            {
                var orden = grupo.OrderBy(x => x.n).ToList();
                for (int i = 0; i + 1 < orden.Count; i++)
                {
                    Assert.IsFalse(orden[i].pisos == orden[i + 1].pisos && orden[i].color == orden[i + 1].color,
                        "Vecinas con el mismo color y altura: " + orden[i].t.name + " y " + orden[i + 1].t.name);
                }
            }
        }

        [Test]
        public void Fachadas_TodasTienenCollider()
        {
            foreach (var f in Fachadas(Raiz()))
            {
                Assert.IsTrue(f.t.GetComponentsInChildren<Collider>(true).Any(c => c.enabled), f.t.name + " sin collider");
            }
        }

        [Test]
        public void Remate_ExisteYEstaCentradoEnLaCalle()
        {
            Transform remate = Raiz().Find("Remate");
            Assert.IsNotNull(remate, "Falta el remate del fondo");
            Renderer[] rs = remate.GetComponentsInChildren<Renderer>();
            Assert.Greater(rs.Length, 10);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            Assert.AreEqual(XCalle, b.center.x, TolRemate, "Centro del remate en x");
            Assert.Less(b.center.z, -85f, "El remate está al sur de la calle");
            Assert.GreaterOrEqual(b.size.y, 6f, "El remate tiene 2 o 3 pisos");
        }

        [Test]
        public void Charcos_EntreSeisYDiez()
        {
            int n = Raiz().Find("Piso").Cast<Transform>().Count(t => t.name == "Charco");
            Assert.That(n, Is.InRange(6, 10));
        }

        [Test]
        public void Arroyo_SinCollidersEnCharcosNiRayas()
        {
            foreach (Transform t in Raiz().Find("Piso"))
            {
                if (t.name == "Charco" || t.name == "Linea")
                {
                    Assert.IsNull(t.GetComponent<Collider>(), t.name + " no debe tener collider");
                }
            }
        }
    }
}

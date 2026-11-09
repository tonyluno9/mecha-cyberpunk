using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Lugares;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-910 (B7): lo que dejó la colocación de las piezas nuevas: semáforos ES01, basura BS01, pasarela PS01, puerta PC01 y lata O01.</summary>
    public class PiezasCalleT910Pruebas
    {
        private const string Prefabs = "Assets/Arte/Calle/Prefabs/";

        [OneTimeSetUp]
        public void Abrir() => EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);

        private static Bounds Caja(GameObject g)
        {
            Renderer[] rs = g.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        [Test]
        public void Medidas_SonRealistas()
        {
            GameObject semaforo = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "ES01_Semaforo.prefab");
            float alto = Caja(semaforo).size.y;
            Assert.That(alto, Is.InRange(4f, 5f), "semáforo");
            foreach (string c in new[] { "a", "b", "c", "d", "e" })
            {
                GameObject b = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "BS01" + c + "_Basura.prefab");
                Vector3 s = Caja(b).size;
                Assert.That(Mathf.Max(s.x, s.y, s.z), Is.InRange(0.05f, 0.3f), "basura BS01" + c);
                Assert.IsEmpty(b.GetComponents<Collider>(), "la basura no estorba");
            }
            Assert.That(Caja(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "O01_BebidaRayo.prefab")).size.y, Is.InRange(0.1f, 0.14f), "lata");
            Assert.That(Caja(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "PC01_PuertaCristal.prefab")).size.y, Is.InRange(2f, 2.4f), "puerta");
        }

        [Test]
        public void Semaforos_ES01EnElCruceConCicloYContrafase()
        {
            SemaforoCiclo[] s = ColocarS2.Todos().Select(t => t.GetComponent<SemaforoCiclo>()).Where(c => c != null && c.gameObject.activeInHierarchy).ToArray();
            Assert.GreaterOrEqual(s.Length, 4, "los cuatro semáforos del cruce");
            foreach (SemaforoCiclo c in s)
            {
                Assert.IsNotNull(c.transform.Find("Poste"), "lleva el modelo");
                Assert.AreEqual("ES01_Semaforo", PrefabUtility.GetCorrespondingObjectFromSource(c.transform.Find("Poste").gameObject).name);
                Assert.AreEqual(3, c.GetComponentsInChildren<Renderer>().Count(r => r.name.StartsWith("Lente_")), "tres lentes");
                Assert.AreEqual(1, c.GetComponentsInChildren<Collider>().Length, "solo el poste choca");
            }
            Assert.GreaterOrEqual(s.Count(c => c.Contrafase), 2, "la calle cruzada va a medio ciclo");
            Assert.GreaterOrEqual(s.Count(c => !c.Contrafase), 2);
            Assert.IsFalse(s.Any(c => c.GetComponentsInChildren<Transform>().Any(t => t.name.StartsWith("Mica_"))), "ya no son los postes N08 con micas");
        }

        [Test]
        public void Basura_BS01SueltaSinCollider()
        {
            Transform raiz = ColocarS2.Todos().First(t => t.name == "Basura_BS01");
            Transform[] piezas = raiz.Cast<Transform>().ToArray();
            Assert.AreEqual(ColocarT910.PiezasBasura, piezas.Length);
            Assert.IsTrue(piezas.All(p => p.name.StartsWith("BS01")));
            Assert.GreaterOrEqual(piezas.Select(p => p.name).Distinct().Count(), 3, "con variación de pieza");
            Assert.GreaterOrEqual(piezas.Select(p => Mathf.Round(p.eulerAngles.y / 20f)).Distinct().Count(), 4, "con variación de giro");
            Assert.IsEmpty(raiz.GetComponentsInChildren<Collider>(), "sin collider: no bloquea al jugador");
        }

        [Test]
        public void Pasarela_PS01SobreLaCalleConCollider()
        {
            Transform[] pasarelas = ColocarS2.Todos().Where(t => t.name == "PS01_Pasarela" && t.parent != null && t.parent.name == ColocarT910.RaizPasarelas).ToArray();
            Assert.AreEqual(2, pasarelas.Length);
            Assert.IsTrue(ColocarS2.Todos().Where(t => t.name == "N02_T505").All(t => !t.gameObject.activeSelf), "los N02 siguen apagados");
            Physics.SyncTransforms();
            foreach (Transform p in pasarelas)
            {
                MeshCollider[] colliders = p.GetComponentsInChildren<MeshCollider>();
                Assert.IsNotEmpty(colliders);
                Assert.IsTrue(colliders.All(c => c.enabled && c.sharedMesh != null));
                Bounds b = Caja(p.gameObject);
                float suelo = p.position.y;
                Assert.GreaterOrEqual(b.size.x, 19f, "de banqueta a banqueta");
                // Claro por encima de la calle: bajo la cubierta (centro de la calle) caben los carros.
                // La cubierta cruza la calle en el extremo +z del modelo (las escaleras bajan hacia -z).
                float zCubierta = b.max.z - 0.6f;
                Vector3 centro = new Vector3(b.center.x, suelo + 0.5f, zCubierta);
                Assert.IsTrue(Physics.Raycast(centro, Vector3.up, out RaycastHit abajo, 20f), "la cubierta está sobre la calle");
                Assert.Greater(abajo.distance, 4.5f, "claro suficiente para carros");
                Assert.IsTrue(Physics.Raycast(new Vector3(b.center.x, suelo + 20f, zCubierta), Vector3.down, out RaycastHit arriba, 30f), "se camina sobre la cubierta");
                Assert.Greater(arriba.point.y - suelo, 5f);
                // La escalera sube: rayos hacia abajo a lo largo de ella (al lado oeste) dan alturas crecientes.
                float xEscalera = b.min.x + 2f;
                var alturas = Enumerable.Range(0, 40).Select(i => b.min.z + (b.size.z * (i + 0.5f) / 40f))
                    .Select(z => Physics.Raycast(new Vector3(xEscalera, suelo + 20f, z), Vector3.down, out RaycastHit h, 30f) ? h.point.y - suelo : -1f).Where(h => h >= 0f).ToArray();
                Assert.GreaterOrEqual(alturas.Length, 20, "peldaños bajo los rayos");
                Assert.Greater(alturas.Max() - alturas.Min(), 4f, "la escalera sube de la banqueta a la cubierta");
            }
        }

        [Test]
        public void Puerta_PC01DosHojasQueSeDeslizan()
        {
            HojaPuerta hoja = ColocarS2.Buscar(ColocarS2oCasino.RutaHoja).GetComponent<HojaPuerta>();
            HojasCorredizas hc = hoja.transform.parent.GetComponentInChildren<HojasCorredizas>();
            Assert.IsNotNull(hc, "la puerta tiene sus dos hojas");
            Assert.AreEqual("PC01_PuertaCorrediza", PrefabUtility.GetCorrespondingObjectFromSource(hc.gameObject).name);
            Assert.IsNotNull(hc.Izquierda);
            Assert.IsNotNull(hc.Derecha);
            Assert.AreEqual("PC01_HojaIzq", hc.Izquierda.GetComponent<MeshFilter>().sharedMesh.name);
            Assert.AreEqual("PC01_HojaDer", hc.Derecha.GetComponent<MeshFilter>().sharedMesh.name);
            Assert.IsNotNull(hc.transform.Find("PC01_Marco"), "marco fijo");
            Assert.IsFalse(hoja.GetComponent<MeshRenderer>().enabled, "la hoja de cubo ya no se dibuja: manda las dos de cristal");
            Assert.AreEqual(2, hc.transform.Cast<Transform>().Count(t => t.name.StartsWith("PC01_Hoja")));
            Assert.Greater(hc.DeslizamientoIzquierda.magnitude, 0.2f);
        }

        [Test]
        public void Lata_O01EnLaBandejaDeLaExpendedora()
        {
            GameObject maquina = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "CY06_Expendedora.prefab");
            Transform lata = maquina.transform.Find(ColocarT910.NombreLata);
            Assert.IsNotNull(lata);
            Assert.AreEqual("O01_BebidaRayo", PrefabUtility.GetCorrespondingObjectFromSource(lata.gameObject).name);
            Bounds m = Caja(maquina);
            Bounds l = Caja(lata.gameObject);
            Assert.IsTrue(m.Contains(l.center), "dentro de la máquina");
            Assert.Less(l.center.y, m.size.y * 0.3f, "abajo, en la bandeja");
        }
    }
}

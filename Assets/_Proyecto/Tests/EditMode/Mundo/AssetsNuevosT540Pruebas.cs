using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-540 a T-544 (plan de la noche 3-nov, paso 2): torres del kit T20-T23 en Neón Alto, postes N09, letreros 3D L10-L14 que brillan de
    /// noche, motos de reparto V07 y pantallas LED con anuncios.
    /// </summary>
    public class AssetsNuevosT540Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private Transform Raiz(string n) => escena.GetRootGameObjects().First(g => g.name == n).transform;

        [Test]
        public void Torres_BaseDeSeisAVeintePisosYRemate_SinDosPantallasSeguidas()
        {
            Transform[] torres = Raiz("T541_Torres").Cast<Transform>().Where(t => t.name.StartsWith("Torre_")).ToArray();
            Assert.AreEqual(13, torres.Length, "Una torre por edificio de barrio de Neón Alto");
            Assert.Greater(torres.Select(t => t.childCount).Distinct().Count(), 4, "Alturas distintas");
            foreach (Transform t in torres)
            {
                string[] piezas = t.Cast<Transform>().Select(p => p.name).ToArray();
                Assert.AreEqual("Base", piezas.First(), t.name);
                Assert.AreEqual("Remate", piezas.Last(), t.name);
                int pisos = piezas.Length - 2;
                Assert.That(pisos, Is.InRange(6, 20), t.name);
                for (int i = 1; i < piezas.Length; i++)
                {
                    Assert.IsFalse(piezas[i] == "PisoPantalla" && piezas[i - 1] == "PisoPantalla", t.name + ": dos T22 seguidos");
                }
            }
            Transform barrio = Raiz("Barrio");
            Assert.IsFalse(barrio.Find("Manzana_3/Edificios/Edificio_25").gameObject.activeSelf, "Las casas de barrio de Neón Alto se fueron");
            Assert.IsTrue(barrio.Find("Manzana_1/Edificios/Edificio_7") != null && barrio.Find("Manzana_4/Edificios/Edificio_41").gameObject.activeSelf, "Suburbios sigue con casas");
            Assert.IsFalse(Raiz("T505_NeonAlto").Find("Torres").gameObject.activeSelf, "Sin las torres de bloques de código");
        }

        [Test]
        public void PostesN09_EnNeonAlto_ConLuzFriaEnElFoco()
        {
            Transform postes = Raiz("T541_Torres").Find("PostesN09");
            Assert.GreaterOrEqual(postes.childCount, 8);
            foreach (Transform farola in Raiz("Barrio").Find("Farolas").Cast<Transform>().Where(f => f.position.x > 36f && f.position.x < 58f && f.position.z > 10f && f.position.z < 90f))
            {
                Light l = farola.GetComponentInChildren<Light>(true);
                if (l == null)
                {
                    continue;
                }
                Assert.Greater(l.color.b, l.color.r, farola.name + ": luz blanca fría");
                Assert.Greater(l.transform.position.y, 5f, farola.name + ": la luz sale del foco, arriba");
            }
        }

        [Test]
        public void Letreros3D_EnSuLugar_BrillanDeNoche_SinLosPlanos()
        {
            Transform letreros = Raiz("T542_Letreros");
            foreach (string c in new[] { "L10", "L11", "L12", "L13", "L14" })
            {
                Transform l = letreros.Find(c);
                Assert.IsNotNull(l, c);
                Assert.IsNotNull(l.GetComponent<LuzNocturna>(), c + ": brilla solo de noche");
                Material m = l.GetComponentInChildren<Renderer>().sharedMaterial;
                Assert.IsTrue(m.IsKeywordEnabled("_EMISSION") && m.GetTexture("_EmissionMap") != null, c + ": texto con emisión");
            }
            Assert.Less(Vector3.Distance(letreros.Find("L10").position, new Vector3(57f, 5f, 47f)), 3f, "VivaCoin sobre la entrada del Casino");
            Assert.Less(Vector3.Distance(letreros.Find("L13").position, new Vector3(-100f, 3.3f, -10f)), 3f, "I+D+I en el local del Doc");
            Assert.Less(Vector3.Distance(letreros.Find("L14").position, new Vector3(-73.3f, 3.3f, 10f)), 3f, "CIC en el de Tino");
            Assert.IsFalse(Raiz("Barrio").Find("LetrerosNeon/LetreroNeon_3_Casino").gameObject.activeSelf, "Sin el letrero plano del Casino");
            Assert.IsFalse(Raiz("GTA_T414").Find("LetreroVivaCoin_Fondo").gameObject.activeSelf, "Sin la lámina VIVACOIN");
        }

        [Test]
        public void MotosDeReparto_ManejablesYRobables_EnSuburbiosYElTianguis()
        {
            Transform motos = Raiz("T543_MotosReparto");
            Assert.GreaterOrEqual(motos.childCount, 4);
            Assert.IsTrue(motos.Cast<Transform>().Any(m => m.name.Contains("Suburbios")) && motos.Cast<Transform>().Any(m => m.name.Contains("Tianguis")));
            foreach (Transform m in motos)
            {
                Assert.IsNotNull(m.GetComponent("CarroEstacionado"), m.name + " se roba");
                Assert.AreEqual("Moto de reparto", new SerializedObject(m.GetComponent("CarroViejo")).FindProperty("nombreModelo").stringValue);
            }
        }

        [Test]
        public void Pantallas_ConAnunciosQueRotan_VerticalesEnLasAltas()
        {
            PantallaLed[] pantallas = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PantallaLed>()).ToArray();
            Assert.GreaterOrEqual(pantallas.Length, 40, "E03, CY02, N08 y pisos T22 con anuncio");
            foreach (PantallaLed p in pantallas)
            {
                SerializedObject so = new SerializedObject(p);
                SerializedProperty lista = so.FindProperty("anuncios");
                Assert.GreaterOrEqual(lista.arraySize, 3, p.transform.parent.name);
                string primero = lista.GetArrayElementAtIndex(0).objectReferenceValue.name;
                bool alta = p.transform.lossyScale.y > p.transform.lossyScale.x;
                StringAssert.StartsWith(alta ? "pantalla-v-" : "pantalla-h-", primero, p.transform.parent.name + ": anuncio según la forma de la pantalla");
                Assert.That(so.FindProperty("segundosMinimos").floatValue, Is.InRange(7.9f, 8.1f));
                Assert.That(so.FindProperty("segundosMaximos").floatValue, Is.InRange(11.9f, 12.1f));
            }
        }
    }
}

using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-501: lo que el colocador de Suburbios dejó en Ciudad_Prologo (raíz T501_Suburbios): cables en catenaria, lámparas N01 con luz,
    /// 30 a 60 torres de horizonte, tinacos, posproceso local, y que nada de eso estorba puertas ni personajes.
    /// </summary>
    public class SuburbiosT501Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T501_Suburbios";
        private const int CablesMinimos = 12;       // 6 por cuadra, dos cuadras de calle principal
        private const int LamparasMinimas = 10;
        private const int TinacosMinimos = 15;
        private const int LucesMaximas = 30;
        private const float RadioPuerta = 1.5f;

        private Scene escena;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            raiz = escena.GetRootGameObjects().FirstOrDefault(g => g.name == NombreRaiz)?.transform;
            Assert.IsNotNull(raiz, "Ciudad_Prologo debe traer la raíz " + NombreRaiz + " (corre Mecha Cyberpunk/T-501/Colocar Suburbios)");
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        [Test]
        public void Raiz_EsUnaSola()
        {
            Assert.AreEqual(1, escena.GetRootGameObjects().Count(g => g.name == NombreRaiz));
        }

        [Test]
        public void Cables_SonCatenariasConMasDeSeisPorCuadra()
        {
            LineRenderer[] cables = raiz.Find("Cables").GetComponentsInChildren<LineRenderer>();
            Assert.GreaterOrEqual(cables.Length, CablesMinimos);
            foreach (LineRenderer lr in cables)
            {
                Assert.GreaterOrEqual(lr.positionCount, 8, lr.name);
                float medio = lr.GetPosition(lr.positionCount / 2).y;
                float extremos = Mathf.Min(lr.GetPosition(0).y, lr.GetPosition(lr.positionCount - 1).y);
                Assert.Less(medio, extremos + 0.5f, "el cable cuelga en medio: " + lr.name);
            }
        }

        [Test]
        public void Lamparas_N01_TienenLuzCalidaSinSombra()
        {
            Transform lamparas = raiz.Find("Lamparas");
            Assert.GreaterOrEqual(lamparas.childCount, LamparasMinimas);
            Light[] luces = lamparas.GetComponentsInChildren<Light>();
            Assert.Greater(luces.Length, 0);
            foreach (Light l in luces)
            {
                Assert.AreEqual(LightShadows.None, l.shadows);
                Assert.Greater(l.color.r, l.color.b, "luz cálida");
            }
            Assert.LessOrEqual(raiz.GetComponentsInChildren<Light>().Length, LucesMaximas, "presupuesto de luces en tiempo real");
        }

        [Test]
        public void Torres_SonDe30A60ConAntenasYSinColliders()
        {
            Transform horizonte = raiz.Find("Horizonte");
            // T-513: el fondo lejano (matte painting) las cubre y quedan apagadas; siguen en la escena.
            Assert.IsFalse(horizonte.gameObject.activeSelf, "Apagadas por el fondo de T-513");
            Assert.GreaterOrEqual(horizonte.childCount, 30);
            Assert.LessOrEqual(horizonte.childCount, 60);
            Assert.AreEqual(0, horizonte.GetComponentsInChildren<Collider>().Length, "el horizonte no estorba la física");
            Assert.Greater(horizonte.GetComponentsInChildren<Transform>().Count(t => t.name == "LuzRoja"), 5);
            Assert.Greater(horizonte.GetComponentsInChildren<Transform>().Count(t => t.name == "Pantalla"), 5);
        }

        [Test]
        public void Tinacos_HayEnLasAzoteas()
        {
            Assert.GreaterOrEqual(raiz.Find("Tinacos").childCount, TinacosMinimos);
        }

        [Test]
        public void Letreros_Charcos_Niebla_Existen()
        {
            Assert.Greater(raiz.Find("Letreros").childCount, 8);
            Assert.Greater(raiz.Find("Charcos").childCount, 8);
            Assert.Greater(raiz.Find("Niebla").childCount, 3);
            Assert.Greater(raiz.Find("Orillas").childCount, 5);
        }

        [Test]
        public void VolumenLocal_EsLocalYSigueLaNoche()
        {
            Volume v = raiz.Find("VolumenSuburbios").GetComponent<Volume>();
            Assert.IsFalse(v.isGlobal);
            Assert.IsNotNull(v.sharedProfile);
            Component sigue = v.GetComponents<MonoBehaviour>().FirstOrDefault(m => m != null && m.GetType().Name == "VolumenSigueNoche");
            Assert.IsNotNull(sigue);
            Assert.IsNotNull(new SerializedObject(sigue).FindProperty("origen").objectReferenceValue);
        }

        [Test]
        public void Nada_TapaPuertasNiPersonajes()
        {
            string[] tipos = { "PuertaLocal", "PersonajeApoyo", "Reclutable", "MostradorTienda", "PuntoAparicion", "JefePandilla" };
            Collider[] propios = raiz.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).ToArray();
            foreach (MonoBehaviour m in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if (m == null || !tipos.Contains(m.GetType().Name))
                {
                    continue;
                }
                Vector3 p = m.transform.position;
                foreach (Collider c in propios)
                {
                    Vector3 cerca = c.ClosestPoint(p);
                    Assert.Greater(Vector3.Distance(cerca, p), RadioPuerta, c.name + " tapa " + m.GetType().Name + " " + m.name);
                }
            }
        }

        [Test]
        public void Shader_TorreHorizonte_Compila()
        {
            Shader s = Shader.Find("MechaCyberpunk/TorreHorizonte");
            Assert.IsNotNull(s);
            Assert.IsTrue(s.isSupported);
        }
    }
}

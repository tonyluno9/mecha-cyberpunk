using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-504: lo que el colocador dejó en la entrada de El Mercado (raíz T504_Mercado) y que no tapa la puerta ni el carril desde la boca del callejón.</summary>
    public class MercadoT504Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T504_Mercado";
        private const int BanderasMinimas = 40;
        private const int HilosMinimos = 5;
        private const int LucesMaximas = 10;
        private const float AlturaMinimaHilos = 5.5f;
        private const float RadioPuerta = 2.2f;
        private const float AnchoCarril = 2.4f;
        private const float XCarril = -22.9f;
        private const float AlturaJugador = 1.8f;

        private Scene escena;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            raiz = escena.GetRootGameObjects().FirstOrDefault(g => g.name == NombreRaiz)?.transform;
            Assert.IsNotNull(raiz, "Ciudad_Prologo debe traer " + NombreRaiz + " (corre Mecha Cyberpunk/T-504/Colocar Entrada de El Mercado)");
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private int Contar(string prefijo)
        {
            return raiz.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith(prefijo));
        }

        private Vector3 PuntoEntrada()
        {
            Transform t = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .First(x => x.name == "Entrada" && x.parent != null && x.parent.name == "Lugar_MercadoNegro");
            return t.position;
        }

        [Test]
        public void Raiz_EsUnaSolaYElPortonE02SigueAhi()
        {
            Assert.AreEqual(1, escena.GetRootGameObjects().Count(g => g.name == NombreRaiz));
            Assert.IsTrue(escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Any(t => t.name.StartsWith("E02_PortonMercadoNegro")));
        }

        [Test]
        public void Trae_BarrerasConFranjaTurquesaEmisiva()
        {
            Assert.GreaterOrEqual(Contar("C18_BarreraCalle_T504"), 3);
            Transform[] franjas = raiz.GetComponentsInChildren<Transform>().Where(t => t.name == "Franja_Turquesa").ToArray();
            Assert.GreaterOrEqual(franjas.Length, 6);
            Color c = franjas[0].GetComponent<MeshRenderer>().sharedMaterial.GetColor("_BaseColor");
            Assert.Greater(c.g, 1f, "franja con brillo (HDR)");
            Assert.Greater(c.r, 0f);
            Assert.Less(c.r, c.g);
        }

        [Test]
        public void Trae_CamarasServidorImplantesYPlataformaDeDron()
        {
            Assert.GreaterOrEqual(Contar("CY08_CamaraCorporativa_T504"), 2);
            Assert.GreaterOrEqual(Contar("CY05_ServidorCallejero_T504"), 1);
            Assert.GreaterOrEqual(Contar("CY09_PuestoImplantes_T504"), 1);
            Assert.GreaterOrEqual(Contar("CY07a_PlataformaDron_T504"), 1);
        }

        [Test]
        public void PapelPicado_BanderasConTexturaEnHilerasAltas()
        {
            MeshRenderer[] banderas = raiz.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("Papel_")).ToArray();
            Assert.GreaterOrEqual(banderas.Length, BanderasMinimas);
            Assert.IsTrue(banderas.All(b => b.sharedMaterial.GetTexture("_BaseMap") != null));
            Assert.GreaterOrEqual(banderas.Select(b => b.sharedMaterial).Distinct().Count(), 4, "colores distintos");
            LineRenderer[] hilos = raiz.GetComponentsInChildren<LineRenderer>();
            Assert.GreaterOrEqual(hilos.Length, HilosMinimos);
            foreach (LineRenderer h in hilos)
            {
                for (int i = 0; i < h.positionCount; i++)
                {
                    Assert.Greater(h.GetPosition(i).y, AlturaMinimaHilos, h.name + " muy bajo");
                }
            }
        }

        [Test]
        public void Luces_SonPocasSinSombraYHayNaranjaEnLaPuerta()
        {
            Light[] luces = raiz.GetComponentsInChildren<Light>();
            Assert.LessOrEqual(luces.Length, LucesMaximas);
            Assert.IsTrue(luces.All(l => l.shadows == LightShadows.None));
            Vector3 e = PuntoEntrada();
            Assert.IsTrue(luces.Any(l => l.color.r > 0.9f && l.color.g > 0.35f && l.color.g < 0.65f && l.color.b < 0.3f
                && Vector3.Distance(new Vector3(l.transform.position.x, 0f, l.transform.position.z), new Vector3(e.x, 0f, e.z)) < 4f), "luz naranja junto al portón");
        }

        [Test]
        public void NingunaPieza_TapaElPuntoDeLaPuertaDeElMercado()
        {
            Vector3 e = PuntoEntrada();
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>())
            {
                Vector3 cerca = c.bounds.ClosestPoint(new Vector3(e.x, c.bounds.center.y, e.z));
                Assert.Greater(Vector3.Distance(new Vector3(cerca.x, 0f, cerca.z), new Vector3(e.x, 0f, e.z)), RadioPuerta, c.name + " pegado a la puerta de El Mercado");
            }
        }

        [Test]
        public void CarrilDesdeLaBocaDelCallejon_QuedaLibreDeT504()
        {
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>())
            {
                Bounds b = c.bounds;
                bool dentro = b.max.x > XCarril - AnchoCarril * 0.5f && b.min.x < XCarril + AnchoCarril * 0.5f && b.max.z > 9f && b.min.z < 21.5f && b.min.y < AlturaJugador;
                Assert.IsFalse(dentro, c.name + " tapa el carril de la boca del callejón");
            }
        }
    }
}

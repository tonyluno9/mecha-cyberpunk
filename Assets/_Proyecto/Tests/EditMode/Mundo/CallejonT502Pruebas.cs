using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-502: lo que el colocador del callejón dejó en Ciudad_Prologo (raíz T502_Callejon) y que no tapa el camino ni los lugares de la historia.</summary>
    public class CallejonT502Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T502_Callejon";
        private const int VentilasMinimas = 3;
        private const int TuberiasMinimas = 3;
        private const int ParticulasMaximas = 150;
        private const int LucesMaximas = 8;
        private const float DistanciaLugares = 1.5f;
        private const float XMin = -87f;
        private const float XMax = -59f;
        private const float ZRuta = -59f;
        private const float MitadCorredor = 2f;

        private Scene escena;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            raiz = escena.GetRootGameObjects().FirstOrDefault(g => g.name == NombreRaiz)?.transform;
            Assert.IsNotNull(raiz, "Ciudad_Prologo debe traer " + NombreRaiz + " (corre Mecha Cyberpunk/T-502/Colocar Callejón)");
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

        [Test]
        public void Raiz_EsUnaSola()
        {
            Assert.AreEqual(1, escena.GetRootGameObjects().Count(g => g.name == NombreRaiz));
        }

        [Test]
        public void Trae_TuberiasVentilasContenedorLlantasTambosYEscalera()
        {
            Assert.GreaterOrEqual(Contar("N04_T502"), TuberiasMinimas);
            Assert.GreaterOrEqual(Contar("N05_T502"), VentilasMinimas);
            Assert.GreaterOrEqual(Contar("L01_Contenedor_T502"), 1);
            Assert.GreaterOrEqual(Contar("L04_Llantas_T502"), 1);
            Assert.GreaterOrEqual(Contar("C16_Tambo_T502"), 1);
            Assert.GreaterOrEqual(Contar("L03_EscaleraEmergencia_T502"), 1);
        }

        [Test]
        public void Ventilas_TienenVaporLigero()
        {
            ParticleSystem[] vapor = raiz.GetComponentsInChildren<ParticleSystem>();
            Assert.GreaterOrEqual(vapor.Length, VentilasMinimas);
            Assert.LessOrEqual(vapor.Sum(p => p.main.maxParticles), ParticulasMaximas);
        }

        [Test]
        public void Luces_SonPocasYSinSombra()
        {
            Light[] luces = raiz.GetComponentsInChildren<Light>();
            Assert.Greater(luces.Length, 0);
            Assert.LessOrEqual(luces.Length, LucesMaximas);
            Assert.IsTrue(luces.All(l => l.shadows == LightShadows.None));
            Assert.IsTrue(luces.Any(l => l.color.r > l.color.g && l.color.b > l.color.g), "al menos una luz magenta");
        }

        [Test]
        public void NingunCollider_EntraAlCorredorDeLaRuta()
        {
            Bounds corredor = new Bounds(new Vector3((XMin + XMax) * 0.5f, 1.3f, ZRuta), new Vector3(XMax - XMin, 2.6f, MitadCorredor * 2f));
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger))
            {
                Assert.IsFalse(c.bounds.Intersects(corredor), "estorba el camino: " + c.name);
            }
        }

        [Test]
        public void NadaTapaLosLugaresDeLaHistoria()
        {
            foreach (string lugar in new[] { "Emboscada_Anclaje", "Llamado_Callejon" })
            {
                Transform t = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(x => x.name == lugar);
                Assert.IsNotNull(t, lugar);
                foreach (Collider c in raiz.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger))
                {
                    Vector3 p = c.ClosestPoint(t.position);
                    Assert.Greater(Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(t.position.x, 0f, t.position.z)), DistanciaLugares, c.name + " junto a " + lugar);
                }
            }
        }

        [Test]
        public void VolumenLocal_EsSoloDeNocheYOscurece()
        {
            Volume v = raiz.GetComponentInChildren<Volume>();
            Assert.IsNotNull(v);
            Assert.IsFalse(v.isGlobal);
            Assert.IsTrue(v.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments ca));
            Assert.Less(ca.postExposure.value, 0f);
            Assert.IsTrue(v.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "VolumenSigueNoche"));
        }
    }
}

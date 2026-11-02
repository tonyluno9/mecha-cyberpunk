using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-503: lo que el colocador de El Tianguis dejó en Ciudad_Prologo (raíz T503_Tianguis) y que no tapa el carril ni los puntos de la pelea; y el perfil de Suburbios arreglado.</summary>
    public class TianguisT503Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T503_Tianguis";
        private const string RutaPerfilSuburbios = "Assets/Arte/Suburbios/PerfilSuburbios.asset";
        private const int PuestosMinimos = 6;
        private const int FocosMinimos = 60;
        private const int LucesMaximas = 14;
        private const int ParticulasMaximas = 20;
        private const float MitadCarril = 3.4f;
        private const float DistanciaPersonajes = 1.5f;

        private Scene escena;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            raiz = escena.GetRootGameObjects().FirstOrDefault(g => g.name == NombreRaiz)?.transform;
            Assert.IsNotNull(raiz, "Ciudad_Prologo debe traer " + NombreRaiz + " (corre Mecha Cyberpunk/T-503/Colocar El Tianguis)");
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
        public void Trae_PuestosDeLosDosModelosExpendedorasYProyectores()
        {
            Assert.GreaterOrEqual(Contar("N03a_T503") + Contar("N03b_T503"), PuestosMinimos);
            Assert.GreaterOrEqual(Contar("N03a_T503"), 1);
            Assert.GreaterOrEqual(Contar("N03b_T503"), 1);
            Assert.GreaterOrEqual(Contar("CY06_Expendedora_T503"), 1);
            Assert.GreaterOrEqual(Contar("CY01_ProyectorHolo_T503"), 1);
        }

        [Test]
        public void Trae_HologramasDeComidaConTextura()
        {
            Transform[] holos = raiz.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Holograma_") || t.name.StartsWith("HoloPuesto_")).ToArray();
            Assert.GreaterOrEqual(holos.Length, 3);
            foreach (Transform h in holos)
            {
                Assert.IsNotNull(h.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_BaseMap"), h.name);
            }
        }

        [Test]
        public void Focos_SonEmisivosYLasLucesSonPocasYSinSombra()
        {
            MeshRenderer[] focos = raiz.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("Foco_")).ToArray();
            Assert.GreaterOrEqual(focos.Length, FocosMinimos);
            Assert.IsTrue(focos[0].sharedMaterial.IsKeywordEnabled("_EMISSION"));
            Light[] luces = raiz.GetComponentsInChildren<Light>();
            Assert.LessOrEqual(luces.Length, LucesMaximas);
            Assert.IsTrue(luces.All(l => l.shadows == LightShadows.None));
            Assert.GreaterOrEqual(raiz.GetComponentsInChildren<LineRenderer>().Length, 4, "series de focos en zigzag");
        }

        [Test]
        public void Vapor_EsLigeroYHayEnVariosPuestos()
        {
            ParticleSystem[] vapor = raiz.GetComponentsInChildren<ParticleSystem>();
            Assert.GreaterOrEqual(vapor.Length, 4);
            Assert.IsTrue(vapor.All(p => p.main.maxParticles <= ParticulasMaximas));
        }

        [Test]
        public void NadaDeLaRaiz_EntraAlCarrilCentral()
        {
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>())
            {
                Bounds b = c.bounds;
                bool dentro = b.min.z < MitadCarril && b.max.z > -MitadCarril && b.min.y < 3f;
                Assert.IsFalse(dentro, c.name + " tapa el carril central");
            }
        }

        [Test]
        public void NingunPuesto_TapaPuntosDePersonajesYPelea()
        {
            string[] tipos = { "PuntoAparicion", "PuntoHistoriaMundo", "JefePandilla", "PandillaAPie", "PuertaLocal" };
            Vector3[] puntos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(m => m != null && tipos.Contains(m.GetType().Name)).Select(m => m.transform.position).ToArray();
            Assert.Greater(puntos.Length, 0);
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>())
            {
                foreach (Vector3 p in puntos)
                {
                    Vector3 cerca = c.bounds.ClosestPoint(new Vector3(p.x, c.bounds.center.y, p.z));
                    Assert.Greater(Vector3.Distance(new Vector3(cerca.x, 0f, cerca.z), new Vector3(p.x, 0f, p.z)), DistanciaPersonajes, c.name + " pegado a un punto en " + p);
                }
            }
        }

        [Test]
        public void PerfilSuburbios_ListaSusComponentes()
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilSuburbios);
            Assert.IsNotNull(perfil);
            Assert.AreEqual(3, perfil.components.Count, "T-507 agregó WhiteBalance");
            Assert.IsTrue(perfil.components.All(c => c != null));
            Assert.IsTrue(perfil.TryGet(out ColorAdjustments _));
            Assert.IsTrue(perfil.TryGet(out Vignette _));
            Assert.IsTrue(perfil.TryGet(out WhiteBalance _));
        }
    }
}

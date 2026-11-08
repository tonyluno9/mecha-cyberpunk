using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-505: lo que el colocador dejó en Neón Alto por fuera (raíz T505_NeonAlto) sin tapar la puerta del casino ni el camino hacia EL HACKER.</summary>
    public class NeonAltoT505Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreRaiz = "T505_NeonAlto";
        private const int LucesMaximas = 8;
        private const int TorresMinimas = 10;
        private const float AlturaJugador = 1.8f;
        private const float XOeste = 37f;
        private const float XEste = 57f;
        private const float RadioPuerta = 2.2f;
        // Corredor de la calle a la puerta del casino (igual que ColocarT505.CorredorCasino; Arte.Editor no se referencia desde aquí).
        private static readonly Rect Corredor = new Rect(37f, 44.5f, 20.5f, 5f);

        private Scene escena;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            raiz = escena.GetRootGameObjects().FirstOrDefault(g => g.name == NombreRaiz)?.transform;
            Assert.IsNotNull(raiz, "Ciudad_Prologo debe traer " + NombreRaiz + " (corre Mecha Cyberpunk/T-505/Colocar Neón Alto)");
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
            return escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .First(x => x.name == "Entrada" && x.parent != null && x.parent.name == "Lugar_Casino").position;
        }

        [Test]
        public void Raiz_EsUnaSolaYElCasinoSigueConSuPuerta()
        {
            Assert.AreEqual(1, escena.GetRootGameObjects().Count(g => g.name == NombreRaiz));
            Transform lugar = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == "Lugar_Casino");
            Assert.IsNotNull(lugar.Find("Puerta"));
            Assert.IsNotNull(lugar.Find("Entrada"));
            Assert.IsNotNull(lugar.Find("Interior"));
        }

        [Test]
        public void Autopista_DosTramosSinColliderQueCubrenLaCalle()
        {
            // T-885 (playtest 7-nov): los dos tramos siguen en la escena (apagados) hasta que llegue la pasarela PS01 del pedido de arte.
            Transform[] puentes = raiz.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("N02_T505")).ToArray();
            Assert.AreEqual(2, puentes.Length);
            Assert.IsTrue(puentes.All(p => !p.gameObject.activeSelf), "apagados: atravesaban de un edificio al otro sin sentido");
            foreach (Transform p in puentes)
            {
                Assert.AreEqual(0, p.GetComponentsInChildren<Collider>().Count(c => c.enabled), "la autopista no debe tener collider activo");
            }
        }

        [Test]
        public void Trae_VallasAnunciosYPantallasGigantes()
        {
            Assert.GreaterOrEqual(Contar("N07_T505"), 6);
            Assert.GreaterOrEqual(Contar("N08_T505"), 3);
            Assert.GreaterOrEqual(Contar("CY02_PantallaVertical_T505"), 3);
            Assert.GreaterOrEqual(Contar("E03_PantallaGigante_T505"), 2);
        }

        [Test]
        public void Pantallas_TienenEmisionPropia()
        {
            Transform[] pantallas = raiz.GetComponentsInChildren<Transform>()
                .Where(x => x.name.StartsWith("E03_PantallaGigante_T505") || x.name.StartsWith("CY02_PantallaVertical_T505")).ToArray();
            Assert.IsNotEmpty(pantallas);
            foreach (Transform t in pantallas)
            {
                Renderer r0 = t.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name != "PantallaLed_T544");
                if (r0 == null)
                {
                    continue; // T-541: la pantalla colgaba de una casa de barrio que ya es torre
                }
                Material m = r0.sharedMaterial;
                Assert.IsTrue(m.IsKeywordEnabled("_EMISSION"), t.name);
                Assert.IsNotNull(m.GetTexture("_EmissionMap"), t.name);
            }
        }

        [Test]
        public void EntradaDelCasino_TieneFocosTubosCargadoresYMoto()
        {
            // T-542: los focos y tubos de formas se cambiaron por el letrero 3D L10 (Tony: "mezcla de asset y formas").
            Assert.IsFalse(raiz.GetComponentsInChildren<Transform>().Any(t => t.name.StartsWith("Foco_") || t.name.StartsWith("Tubo_")), "Sin focos ni tubos de formas");
            Assert.GreaterOrEqual(Contar("CY10_CargadorCalle_T505"), 1);
            // T-532: la moto de adorno es ahora una moto manejable (T532_Motos).
        }

        [Test]
        public void Torres_HayAlMenosDiezConPantalla()
        {
            // T-541: torres del kit con pisos de pantalla (T22) en lugar de las de bloques de T-505.
            Transform[] torres = escena.GetRootGameObjects().First(g => g.name == "T541_Torres").GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Torre_")).ToArray();
            Assert.GreaterOrEqual(torres.Length, TorresMinimas);
            Assert.GreaterOrEqual(torres.Count(t => t.Find("PisoPantalla") != null), TorresMinimas - 2);
        }

        [Test]
        public void Luces_SonPocasSinSombraVerdeYMagenta()
        {
            Light[] luces = raiz.GetComponentsInChildren<Light>();
            Assert.LessOrEqual(luces.Length, LucesMaximas);
            Assert.IsTrue(luces.All(l => l.shadows == LightShadows.None));
            // S2c (7-nov): Neón Alto es magenta con turquesa (M11, K03); el verde ácido de T-505 pasó a turquesa.
            Assert.IsTrue(luces.Any(l => l.color.g > 0.8f && l.color.b > 0.7f && l.color.r < 0.3f), "turquesa");
            Assert.IsTrue(luces.Any(l => l.color.r > 0.9f && l.color.b > 0.7f && l.color.g < 0.3f), "magenta");
        }

        [Test]
        public void Volumen_EsLocalDeNocheConPerfilVerde()
        {
            Volume v = raiz.GetComponentInChildren<Volume>();
            Assert.IsNotNull(v);
            Assert.IsFalse(v.isGlobal);
            Assert.IsNotNull(v.sharedProfile);
            Assert.IsTrue(v.sharedProfile.TryGet(out ColorAdjustments ca));
            Assert.Greater(ca.colorFilter.value.g, ca.colorFilter.value.r, "filtro verdoso");
            Assert.IsTrue(v.GetComponents<MonoBehaviour>().Any(m => m != null && m.GetType().Name == "VolumenSigueNoche"), "solo de noche");
        }

        [Test]
        public void Niebla_HayCapasVerdes()
        {
            MeshRenderer[] capas = raiz.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("Niebla_")).ToArray();
            Assert.GreaterOrEqual(capas.Length, 5);
            Color c = capas[0].sharedMaterial.GetColor("_BaseColor");
            Assert.Greater(c.g, c.r);
            Assert.Greater(c.g, c.b);
        }

        [Test]
        public void NingunCollider_TapaLaPuertaNiElCorredorDelCasino()
        {
            Vector3 e = PuntoEntrada();
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>().Where(x => x.enabled && !x.isTrigger))
            {
                Bounds b = c.bounds;
                bool dentro = b.max.x > Corredor.xMin && b.min.x < Corredor.xMax && b.max.z > Corredor.yMin && b.min.z < Corredor.yMax && b.min.y < AlturaJugador;
                Assert.IsFalse(dentro, c.name + " tapa el corredor al casino");
                Vector3 cerca = b.ClosestPoint(new Vector3(e.x, b.center.y, e.z));
                Assert.Greater(Vector2.Distance(new Vector2(cerca.x, cerca.z), new Vector2(e.x, e.z)), RadioPuerta, c.name + " pegado a la puerta del casino");
            }
        }
    }
}

using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-531 (plan de la noche 3-nov, paso 1): la habitación ya no parece taller y sin gente adentro; Tino tiene su local CIC junto al
    /// MECÁNICO en la calle principal de Suburbios y el Doc su I+D+I aparte de El Mercado; los dos locales se ven abiertos desde la calle.
    /// </summary>
    public class LocalesT531Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            Physics.SyncTransforms();
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private Transform Buscar(string nombre)
        {
            return escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == nombre);
        }

        private static Bounds Cuarto(Transform local)
        {
            Bounds b = local.Find("Piso").GetComponent<Renderer>().bounds;
            b.Expand(new Vector3(0f, 8f, 0f));
            return b;
        }

        [Test]
        public void Habitacion_SinEstacionDelMechaNiGenteAdentro()
        {
            Transform taller = Buscar("Lugar_Casa").Find("Interior/Taller");
            Assert.IsFalse(taller.Find("T02_EstacionExo_Visual_T250").gameObject.activeSelf, "Sin la estación del mecha");
            Assert.AreEqual(1, taller.GetComponentsInChildren<Transform>().Count(t => t.name == "T03_BancoTrabajo"), "Un solo banco de trabajo");
            Bounds casa = new Bounds(new Vector3(-63f, 2f, -77f), new Vector3(11f, 6f, 13f));
            Transform guera = escena.GetRootGameObjects().First(g => g.name == "GTA_T405").transform.Cast<Transform>().First(t => t.name.StartsWith("Reclutable_LA G"));
            Assert.IsFalse(casa.Contains(guera.position), "LA GÜERA ya no está en la habitación");
        }

        [Test]
        public void Tino_EnSuLocalCic_JuntoAlMecanico_ConSuTienda()
        {
            Bounds cic = Cuarto(Buscar("T531_Locales").Find("CIC"));
            Assert.IsTrue(cic.Contains(Buscar("Apoyo_TINO").position), "Tino dentro del CIC");
            Assert.IsTrue(cic.Contains(Buscar("MostradorCIC").position), "La tienda CIC dentro del local");
            Assert.Greater(Vector3.Distance(Buscar("Apoyo_TINO").position, new Vector3(-63f, 0f, -77f)), 60f, "Ya no está enfrente de tu casa");
        }

        [Test]
        public void Doc_EnSuIdi_LejosDeElMercado_ConSuClinica()
        {
            Transform idi = Buscar("T531_Locales").Find("IDI");
            Bounds cuarto = Cuarto(idi);
            Assert.IsTrue(cuarto.Contains(Buscar("Apoyo_JOEL").position), "El Doc dentro del I+D+I");
            // La clínica es parte del prefab de El Mercado (no se puede cambiar de padre): se mueve de lugar, no de jerarquía.
            Transform clinica = Buscar("MostradorClinica");
            Assert.IsTrue(cuarto.Contains(clinica.position), "El mostrador de implantes dentro del I+D+I");
            Assert.Greater(Vector3.Distance(Buscar("Apoyo_JOEL").position, new Vector3(-31f, 0f, 26f)), 50f, "Lejos de El Mercado");
        }

        [Test]
        public void Locales_AbiertosDesdeLaCalle()
        {
            foreach ((string nombre, float x, float z, float dz) in new[] { ("CIC", -73.3f, 3f, 1f), ("IDI", -100f, -3f, -1f) })
            {
                // Desde la banqueta hacia adentro, a 1.6 m del piso: lo primero sólido está al fondo del local (más de 10 m).
                bool choca = Physics.Raycast(new Vector3(x, 1.6f, z), new Vector3(0f, 0f, dz), out RaycastHit h, 30f, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsTrue(!choca || h.distance > 9f || h.collider.GetComponentInParent<Renderer>() == null || h.collider.name.StartsWith("Mesa") || h.collider.name.Contains("Mostrador") || h.collider.name.Contains("Camilla"),
                    nombre + ": algo tapa la entrada: " + (choca ? h.collider.name + " a " + h.distance.ToString("F1") : ""));
            }
        }
    }
}

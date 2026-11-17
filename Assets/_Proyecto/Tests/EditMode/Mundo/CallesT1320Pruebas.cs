using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-1320: la plaza (K09) abierta a la calle K01, la calle comercial (K10) con sus puestos en la calle y la calle de la armería con su
    /// letrero y el paso libre a la puerta.
    /// </summary>
    public class CallesT1320Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir() => escena = EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Additive);

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        private Transform Raiz() => escena.GetRootGameObjects().First(g => g.name == ColocarT1320Calles.Raiz).transform;

        [Test]
        public void Plaza_LasCasasDelFrenteYaNoEstan_YHayKiosco()
        {
            foreach (string ruta in ColocarT1320Calles.CasasPlaza)
            {
                string[] partes = ruta.Split(new[] { '/' }, 2);
                Transform casa = escena.GetRootGameObjects().Where(g => g.name == partes[0]).Select(g => g.transform.Find(partes[1])).FirstOrDefault(t => t != null);
                Assert.IsNotNull(casa, ruta);
                Assert.IsFalse(casa.gameObject.activeInHierarchy, ruta + " apagada para abrir la plaza");
            }
            Transform plaza = Raiz().Find("Plaza_K09");
            Assert.IsNotNull(plaza.Find("Kiosco"));
            Assert.GreaterOrEqual(plaza.Cast<Transform>().Count(t => t.name == "Arbol"), 4, "árboles");
            Assert.GreaterOrEqual(plaza.Cast<Transform>().Count(t => t.name == "Banca"), 4, "bancas");
        }

        [Test]
        public void Plaza_DeLaCalleAlKiosco_NoHayMuros()
        {
            Physics.SyncTransforms();
            // Del centro de la calle K01 hacia el kiosco, a la altura del pecho: nada estorba antes de la base del kiosco.
            Vector3 desde = new Vector3(ColocarT1320Calles.CentroKiosco.x, 1.2f, ColocarS2cCalle.ZCentro);
            Vector3 hasta = ColocarT1320Calles.CentroKiosco + Vector3.up * 1.2f;
            RaycastHit[] golpes = Physics.RaycastAll(desde, (hasta - desde).normalized, (hasta - desde).magnitude - 3.2f)
                .Where(h => !h.collider.isTrigger && h.collider.gameObject.scene == escena && h.collider.gameObject.activeInHierarchy).ToArray();
            Assert.IsEmpty(golpes.Select(h => h.collider.name + " en " + h.point.ToString("F1")).ToArray());
        }

        [Test]
        public void TodoLoPuesto_QuedaDondeSePidio()
        {
            // Los puestos van en la calle K01 (entre sus fachadas) o en el callejón de la armería, nunca regados lejos.
            Transform raiz = Raiz();
            foreach (Transform t in raiz.Find("CalleComercial_K10").Cast<Transform>().Where(t => t.name.StartsWith("C1") || t.name.StartsWith("CY")))
            {
                Assert.That(t.position.z, Is.InRange(ColocarT1320Calles.FachadaSur, ColocarT1320Calles.FachadaNorte), t.name);
                Assert.That(t.position.x, Is.InRange(ColocarT1320Calles.ComercioX0 - 1f, ColocarT1320Calles.ComercioX1 + 1f), t.name);
            }
            foreach (Transform t in raiz.Find("CalleArmeria").Cast<Transform>().Where(t => t.name.StartsWith("C1") || t.name.StartsWith("CY")))
            {
                Assert.IsTrue(ColocarT1320Calles.CallejonArmeria.Contains(new Vector3(t.position.x, 2f, t.position.z)), t.name + " en " + t.position);
            }
        }

        [Test]
        public void Armeria_TieneLetrero_YElPasoALaPuertaEstaLibre()
        {
            Transform letrero = Raiz().Find("CalleArmeria/Letrero_ARMERIA");
            Assert.IsNotNull(letrero);
            Physics.SyncTransforms();
            Collider[] estorbos = Physics.OverlapBox(ColocarT1320Calles.PasoArmeria.center, ColocarT1320Calles.PasoArmeria.extents - new Vector3(0f, 0.4f, 0f))
                .Where(c => !c.isTrigger && c.gameObject.scene == escena && c.bounds.max.y > 0.45f && c.bounds.size.y < 6f).ToArray();
            Assert.IsEmpty(estorbos.Select(c => c.name + " " + c.bounds.center.ToString("F1")).ToArray(), "nada estorba de la avenida a la puerta");
        }
    }
}

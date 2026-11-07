using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// S2c (7-nov), paso 3 de docs/plan-look-ciudad.md: Suburbios en cuadras cortas con el kit (calle de norte a sur con el cruce de K05, la
    /// calle de K01 hasta la calle de x = 47) sin tapar la casa, el I+D+I del Doc, el callejón ni el taller.
    /// </summary>
    public class SuburbiosS2cPruebas
    {
        private const float RadioJugador = 0.4f;
        private const float AlturaJugador = 1.8f;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            raiz = GameObject.Find(ColocarS2cSuburbios.Raiz)?.transform;
            Physics.SyncTransforms();
        }

        [Test]
        public void Suburbios_CasasDelKitEnLasCuadrasCortas()
        {
            Assert.IsNotNull(raiz, "falta " + ColocarS2cSuburbios.Raiz);
            Assert.GreaterOrEqual(raiz.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Casa_")), 60);
            foreach (string e in ColocarS2cSuburbios.Edificios)
            {
                Transform t = ColocarS2.Buscar(e);
                Assert.IsTrue(t == null || !t.gameObject.activeInHierarchy, e + " sigue prendido");
            }
        }

        [Test]
        public void Cruce_TieneLasCuatroEsquinasDeK05ConFarmacia()
        {
            Transform[] esquinas = raiz.GetComponentsInChildren<Transform>().Where(t => t.name == "Esquina_K05").ToArray();
            Assert.AreEqual(4, esquinas.Length);
            Assert.AreEqual(1, esquinas.Count(e => e.GetComponentsInChildren<Transform>().Any(t => t.name == "LetreroFarmacia")), "una esquina es la farmacia");
            Assert.AreEqual(4, esquinas.Count(e => e.GetComponentsInChildren<Transform>().Any(t => t.name == "Semaforo")), "semáforo en cada esquina");
        }

        [TestCase(-86f, -8f, true)]
        [TestCase(-39f, 39f, false)]
        public void Calles_SeCruzanSinChocar(float desde, float hasta, bool norteSur)
        {
            float piso = KitS2c.Suelo(norteSur ? new Vector3(ColocarS2cCalle.XCruce, 0f, -70f) : new Vector3(0f, 0f, ColocarS2cCalle.ZCentro), 0.2f);
            Vector3 abajo = norteSur ? new Vector3(ColocarS2cCalle.XCruce, piso + RadioJugador + 0.25f, desde) : new Vector3(desde, piso + RadioJugador + 0.25f, ColocarS2cCalle.ZCentro);
            Vector3 arriba = abajo + Vector3.up * (AlturaJugador - 2f * RadioJugador);
            Vector3 dir = norteSur ? Vector3.forward : Vector3.right;
            RaycastHit[] golpes = Physics.CapsuleCastAll(abajo, arriba, RadioJugador, dir, hasta - desde, ~(1 << ReflejoCharcos.CapaCharcos), QueryTriggerInteraction.Ignore);
            Assert.IsEmpty(golpes.Select(g => ColocarS2.Ruta(g.collider.transform)).ToArray());
        }

        [Test]
        public void Casas_NoTapanLaCasaNiElIdiNiElTaller()
        {
            Bounds idi = new Bounds(new Vector3((ColocarS2cSuburbios.XIdiPoniente + ColocarS2cSuburbios.XIdiOriente) * 0.5f, 2f, -13f), new Vector3(7.8f, 4f, 5.8f));
            Bounds casa = new Bounds(new Vector3(-65.5f, 2f, -77f), new Vector3(16.8f, 4f, 13.8f));
            Bounds taller = new Bounds(new Vector3(-24.5f, 2f, -71.5f), new Vector3(24.8f, 4f, 24.8f));
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>().Where(c => c.enabled))
            {
                Assert.IsFalse(c.bounds.Intersects(idi), ColocarS2.Ruta(c.transform) + " tapa el I+D+I del Doc");
                Assert.IsFalse(c.bounds.Intersects(casa), ColocarS2.Ruta(c.transform) + " tapa la casa");
                Assert.IsFalse(c.bounds.Intersects(taller), ColocarS2.Ruta(c.transform) + " tapa el taller El Canepa");
            }
        }

        [Test]
        public void Callejon_SiguePasandoAlPoniente()
        {
            // El andador que sale del callejón de los SKULLS al poniente (T-858) cruza la calle de norte a sur y llega a x = -131.
            float piso = KitS2c.Suelo(new Vector3(-95f, 0f, -61.5f), 0.2f);
            Vector3 abajo = new Vector3(-88f, piso + RadioJugador + 0.25f, -61.5f);
            Vector3 arriba = abajo + Vector3.up * (AlturaJugador - 2f * RadioJugador);
            RaycastHit[] golpes = Physics.CapsuleCastAll(abajo, arriba, RadioJugador, Vector3.left, 42f, ~(1 << ReflejoCharcos.CapaCharcos), QueryTriggerInteraction.Ignore);
            Assert.IsEmpty(golpes.Where(g => g.collider.transform.IsChildOf(raiz)).Select(g => ColocarS2.Ruta(g.collider.transform)).ToArray(), "las casas nuevas no tapan el andador");
        }
    }
}

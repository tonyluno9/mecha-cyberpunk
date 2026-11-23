using MechaCyberpunk.Mundo.Editor;
using MechaCyberpunk.Mundo.Lugares;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Sesión A 23-nov (ronda E de B): paso libre para los SKULLS del inicio y armería fuera de la historia. Necesitan que se haya corrido ColocarSesionBT1715.Aplicar.</summary>
    public class ColocarSesionBT1715Pruebas
    {
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir() => escena = EditorSceneManager.OpenScene(ColocarSesionBT1715.RutaEscena, OpenSceneMode.Additive);

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        [Test]
        public void Skulls_TienenAlMenosDosMetrosLibresHastaLaEspera()
        {
            Assert.IsTrue(ColocarSesionBT1715.CaminoSkullsLibre(2f, out string estorbo), "estorba: " + estorbo);
        }

        [Test]
        public void Patrulla_SigueEnLaCalle_YSinChocarConNada()
        {
            GameObject pat = GameObject.Find(ColocarSesionBT1715.RutaPatrulla);
            Assert.IsNotNull(pat);
            Assert.AreEqual(ColocarSesionBT1715.ZPatrullaNueva, pat.transform.position.z, 0.01f);
            Assert.AreEqual(-52.59f, pat.transform.position.x, 0.01f);
            Assert.AreEqual(180f, pat.transform.eulerAngles.y, 0.5f);
            Physics.SyncTransforms();
            foreach (Collider c in Physics.OverlapBox(new Vector3(-52.59f, 1.2f, ColocarSesionBT1715.ZPatrullaNueva), new Vector3(1.0f, 0.5f, 2.2f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(pat.transform) || c.bounds.max.y < 0.6f) { continue; }
                Assert.Fail("la patrulla choca con " + c.name);
            }
            Assert.IsTrue(Physics.Raycast(new Vector3(-52.59f, 5f, ColocarSesionBT1715.ZPatrullaNueva), Vector3.down, out RaycastHit h, 10f) && h.collider.name != null);
        }

        [Test]
        public void Armeria_SinLetreros_PuertaCerrada_YMostradorActivo()
        {
            foreach (string r in ColocarSesionBT1715.LetrerosArmeria)
            {
                GameObject g = GameObject.Find(r);
                Assert.IsTrue(g == null || !g.activeSelf, r + " debe estar apagado");
            }
            Assert.IsFalse(GameObject.Find(ColocarSesionBT1715.RutaPuertaLocal).GetComponent<PuertaLocal>().enabled, "la puerta ya no abre");
            Assert.IsNotNull(GameObject.Find(ColocarSesionBT1715.RaizCierre), "reja de cierre");
            GameObject mostrador = GameObject.Find(ColocarSesionBT1715.RutaMostrador);
            Assert.IsNotNull(mostrador);
            Assert.IsTrue(mostrador.activeInHierarchy, "MostradorMercadoNegro sigue activo (lo usa Tino)");
        }
    }
}

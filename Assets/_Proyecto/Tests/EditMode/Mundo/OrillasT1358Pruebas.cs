using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-1358: las orillas de la ciudad quedan abiertas hacia las zonas nuevas (U1, U2, U4, U9, U7) y existen los límites del mundo nuevos.
    /// Aplica el script sobre Ciudad_Prologo (idempotente) y deja la escena abierta en modo único hasta que acaba cada prueba.
    /// </summary>
    [Category("Escena")]
    public class OrillasT1358Pruebas
    {
        [OneTimeSetUp]
        public void Aplicar()
        {
            ColocarT1358Orillas.Aplicar();
            Physics.SyncTransforms();
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void Uniones_SinColliderActivoA1Y3MetrosDeAltura()
        {
            foreach ((string nombre, Vector4 r) in ColocarT1358Orillas.Uniones)
            {
                Vector3 centro = new Vector3((r.x + r.z) * 0.5f, 2f, (r.y + r.w) * 0.5f);
                Vector3 mitad = new Vector3((r.z - r.x) * 0.5f, 1f, (r.w - r.y) * 0.5f);
                Collider[] golpes = Physics.OverlapBox(centro, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsEmpty(golpes, nombre + " tapada por: " + string.Join(", ", golpes.Select(c => c.name + "@" + c.transform.position)));
            }
        }

        [Test]
        public void Limites_ExistenLosCincoBajoT1358ConBoxColliderSinMalla()
        {
            GameObject raiz = GameObject.Find(ColocarT1358Orillas.NombreRaiz);
            Assert.IsNotNull(raiz);
            BoxCollider[] muros = raiz.GetComponentsInChildren<BoxCollider>().Where(c => c.name == ColocarT1358Orillas.NombreLimite).ToArray();
            Assert.AreEqual(ColocarT1358Orillas.Limites.Length, muros.Length);
            Assert.IsEmpty(muros.Where(m => m.GetComponent<Renderer>() != null));
            Assert.IsTrue(muros.Any(m => Mathf.Approximately(m.transform.position.z, 290f)), "norte z=290");
            Assert.IsTrue(muros.Any(m => Mathf.Approximately(m.transform.position.x, -480f)), "oeste x=-480");
            Assert.IsTrue(muros.Any(m => Mathf.Approximately(m.transform.position.x, 345f)), "este x=345");
            Assert.IsTrue(muros.Any(m => Mathf.Approximately(m.transform.position.z, -268f)), "mar z=-268");
            Assert.IsTrue(muros.All(m => m.size.y >= 30f || m.transform.localScale.y * m.size.y >= 30f));
        }

        [Test]
        public void Aplicar_DosVeces_NoDuplicaLaRaiz()
        {
            ColocarT1358Orillas.Aplicar();
            int raices = EditorSceneManager.GetActiveScene().GetRootGameObjects().Count(g => g.name == ColocarT1358Orillas.NombreRaiz);
            Assert.AreEqual(1, raices);
        }

        [Test]
        public void SalidaDelAgua_Existe_ConPuntosDeOrilla()
        {
            SalidaDelAgua salida = Object.FindFirstObjectByType<SalidaDelAgua>();
            Assert.IsNotNull(salida);
            Assert.Greater(salida.PuntosOrilla.Length, 5);
        }
    }
}

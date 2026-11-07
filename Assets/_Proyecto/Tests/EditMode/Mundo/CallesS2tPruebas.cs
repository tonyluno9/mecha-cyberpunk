using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-861 (playtest 6-nov tarde): las calles tienen nombre en letreros del kit colgados de los postes de los cruces.</summary>
    public class CallesS2tPruebas
    {
        [Test]
        public void Cruces_TienenLetrerosConNombreColgadosDeUnPoste()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            Transform raiz = ColocarS2.Buscar(CallesS2t.NombreRaiz);
            Assert.IsNotNull(raiz);
            Transform[] letreros = raiz.Cast<Transform>().Where(t => t.name.StartsWith("Letrero_")).ToArray();
            Assert.GreaterOrEqual(letreros.Length, 14);
            Transform[] postes = PostesS2t.Postes();
            foreach (Transform l in letreros)
            {
                Vector3 p = l.position;
                float aPoste = postes.Min(q => new Vector2(q.position.x - p.x, q.position.z - p.z).magnitude);
                Assert.Less(aPoste, 2f, l.name + " cuelga de un poste");
                string calle = l.name.Substring("Letrero_".Length);
                Assert.IsTrue(CallesS2t.Calles.Any(c => c.nombre == calle), l.name + " es una calle de la lista");
                Assert.AreEqual(2, raiz.GetComponentsInChildren<UnityEngine.UI.Text>().Count(t => t.text == calle && Vector3.Distance(t.transform.position, p) < 2f), l.name + ": el nombre por las dos caras");
            }
        }
    }
}

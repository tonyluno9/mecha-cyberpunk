using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-857 (playtest 6-nov tarde): los puentes son el modelo N02 de Tony, sin las escaleras de cubos de T-809 (el "puente simulado") y sin
    /// carros arriba. La escalera para subir queda pedida al chat de arte (docs/pedido-arte-06nov-b.md, E01).
    /// </summary>
    public class PuentesS2tPruebas
    {
        [Test]
        public void Puentes_SonElN02SinEscalerasDeCubosNiCarrosArriba()
        {
            Scene escena = EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Additive);
            try
            {
                Transform[] activos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).Where(t => t.gameObject.activeInHierarchy).ToArray();
                Transform[] puentes = activos.Where(t => t.name == "N02_T505").ToArray();
                Assert.AreEqual(2, puentes.Length, "los dos puentes N02 de Neón Alto");
                Assert.IsFalse(activos.Any(t => t.name == ColocarS2tLimpieza.RaizPuentes), "sin la escalera de cubos de T-809");
                foreach (Transform p in puentes)
                {
                    Bounds b = p.GetComponentInChildren<Renderer>().bounds;
                    Rigidbody[] arriba = activos.Select(t => t.GetComponent<Rigidbody>()).Where(r => r != null && b.Contains(r.position) && r.position.y > b.center.y).ToArray();
                    CollectionAssert.IsEmpty(arriba.Select(r => r.name).ToArray(), p.name + ": sin carros arriba");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

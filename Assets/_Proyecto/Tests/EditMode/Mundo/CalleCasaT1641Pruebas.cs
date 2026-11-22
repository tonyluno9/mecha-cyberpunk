using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1641 (T21 puntos 1 y 2): nada estorba en la calle frente a la casa y el callejon de la vuelta tiene asfalto, raya y bordillos.</summary>
    public class CalleCasaT1641Pruebas
    {
        [Test]
        public void CiudadPrologo_LupeFueraDelArroyo_YCallejonConAsfalto()
        {
            Scene escena = EditorSceneManager.OpenScene(CalleCasaT1641.RutaEscena, OpenSceneMode.Additive);
            try
            {
                var todos = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
                Transform lupe = todos.First(t => t.name == "DonaLupe");
                Assert.IsFalse(lupe.position.x > -51f && lupe.position.x < -42f, "Doña Lupe no debe estar en el arroyo de la calle (x -51..-42): " + lupe.position);

                Transform raiz = todos.First(t => t.name == CalleCasaT1641.NombreRaiz);
                Transform asfalto = raiz.Find("Asfalto");
                Assert.IsNotNull(asfalto, "losa de asfalto del callejon");
                Assert.IsNotNull(asfalto.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap"), "el asfalto tiene textura");
                Assert.IsNotNull(raiz.Find("Raya_0"), "raya central");
                Assert.IsNotNull(raiz.Find("Bordillo_Sur"));
                Assert.IsNotNull(raiz.Find("Bordillo_Norte"));
                Bounds b = asfalto.GetComponent<Renderer>().bounds;
                Assert.Greater(b.size.x, 25f, "cubre el largo del callejon");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

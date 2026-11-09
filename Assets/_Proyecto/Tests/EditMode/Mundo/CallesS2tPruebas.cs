using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-921 (playtest 9-nov): ningún poste de Ciudad_Prologo lleva un letrero con nombre de calle (reemplaza la prueba de T-861).</summary>
    public class CallesS2tPruebas
    {
        [Test]
        public void Ciudad_NoTieneLetrerosConNombreDeCalle()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            var quedan = CallesS2t.Encontrar();
            Assert.AreEqual(0, quedan.Length, "letreros de calle que quedan: " + string.Join(", ", System.Array.ConvertAll(quedan, g => g.name)));
        }
    }
}

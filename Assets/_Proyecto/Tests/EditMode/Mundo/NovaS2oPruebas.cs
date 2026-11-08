using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-888 (playtest 7-nov): detrás de la calle de Nova no quedan rendijas entre torres que parezcan callejones rotos.</summary>
    public class NovaS2oPruebas
    {
        private const float HuecoMaximo = 0.3f;

        [Test]
        public void Nova_LasTorresVanJuntas()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            Transform nova = ColocarS2.Buscar(ColocarS2cNova.Raiz);
            Assert.IsNotNull(nova);
            foreach (Transform frente in nova.GetComponentsInChildren<Transform>().Where(t => t.name == "Frente_Nova"))
            {
                Bounds[] torres = frente.Cast<Transform>().Where(t => t.name.StartsWith("T2")).Select(t => ArteT250.Limites(t.gameObject)).OrderBy(b => b.center.x).ToArray();
                Assert.GreaterOrEqual(torres.Length, 3, frente.name);
                for (int i = 1; i < torres.Length; i++)
                {
                    float hueco = torres[i].min.x - torres[i - 1].max.x;
                    Assert.Less(hueco, HuecoMaximo, "rendija entre torres en x = " + torres[i - 1].max.x.ToString("F1"));
                }
            }
        }
    }
}

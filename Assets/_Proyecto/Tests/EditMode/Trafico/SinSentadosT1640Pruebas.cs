using System.Linq;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1640 (T29): Ciudad_Prologo no trae ningún civil sentado; las bancas para sentarse el jugador se quedan.</summary>
    public class SinSentadosT1640Pruebas
    {
        [Test]
        public void CiudadPrologo_NoTieneNpcsSentados_YConservaLasBancas()
        {
            Scene escena = EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", OpenSceneMode.Additive);
            try
            {
                var todos = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).Where(m => m != null).ToList();
                Assert.AreEqual(0, todos.Count(m => m is NpcSentado), "NpcSentado en la escena");
                Assert.AreEqual(0, todos.Count(m => m.GetType().Name == "NpcSentadoBanca"), "NpcSentadoBanca en la escena");
                Assert.IsNull(escena.GetRootGameObjects().FirstOrDefault(g => g.name == "NpcsSentados_T1632"), "raíz NpcsSentados_T1632");
                int bancas = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Count(t => t.name.StartsWith("Banca_Sentable"));
                Assert.Greater(bancas, 0, "las bancas Banca_Sentable se quedan");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

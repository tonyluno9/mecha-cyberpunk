using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-864 (playtest 6-nov tarde): el Doc trae su regalo de implante y su consultorio es sólido por fuera.</summary>
    public class DocS2tPruebas
    {
        [Test]
        public void Doc_ConRegaloYConsultorioSolido()
        {
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            Transform doc = ColocarS2.Buscar(ColocarS2tDoc.RutaDoc);
            Component regalo = doc.GetComponents<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "RegaloImplanteDoc");
            Assert.IsNotNull(regalo, "el Doc regala un implante");
            Object implante = new SerializedObject(regalo).FindProperty("regalo").objectReferenceValue;
            Assert.AreEqual(ColocarS2tDoc.RutaRegalo, AssetDatabase.GetAssetPath(implante));
            Transform idi = ColocarS2.Buscar(ColocarS2tDoc.RutaLocal);
            foreach (string m in ColocarS2tDoc.MurosDeFuera)
            {
                Renderer r = idi.Find(m).GetComponent<Renderer>();
                Assert.IsTrue(r.enabled && r.gameObject.activeInHierarchy, m + " se ve por fuera");
            }
        }
    }
}

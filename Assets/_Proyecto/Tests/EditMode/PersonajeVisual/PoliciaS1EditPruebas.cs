using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-680: el policía a pie usa C01 si trae esqueleto y, si no, el modelo man con uniforme azul marino.</summary>
    public class PoliciaS1EditPruebas
    {
        private const string RutaTemporal = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie_PruebaTemporal.prefab";

        [Test]
        public void C01_TraeEsqueleto_YSeDetectaComoTal()
        {
            Assume.That(File.Exists(ImportarS1n.RutaPolicia), "C01 todavía no llega");
            Assert.IsTrue(ColocarS1nPolicia.TieneEsqueleto(ImportarS1n.RutaPolicia));
            Assert.IsFalse(ColocarS1nPolicia.TieneEsqueleto("Assets/Arte/Armas/W02_Escopeta/W02_Escopeta.fbx"), "Un arma no tiene esqueleto");
        }

        [Test]
        public void PoliciaAPie_ConC01_UsaSuVisualHumanoide_ConPistolaYDatosPropios()
        {
            Assume.That(File.Exists(ImportarS1n.RutaPolicia), "C01 todavía no llega");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ColocarS1nPolicia.RutaPrefab);
            Assert.IsNotNull(prefab);
            Transform modelo = prefab.transform.Find("Modelo");
            Assert.IsNotNull(modelo);
            SkinnedMeshRenderer cuerpo = modelo.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.IsTrue(cuerpo.sharedMesh.name.Contains("df8dc0e4") || cuerpo.name.Contains("df8dc0e4"), "Es el modelo de C01");
            Assert.IsTrue(modelo.GetComponentsInChildren<Transform>(true).Any(t => t.name == "W01"), "Lleva pistola en la mano");
            Assert.AreEqual("POLICÍA", new SerializedObject(prefab.GetComponent("PandilleroAPie")).FindProperty("datos").objectReferenceValue is Object d
                ? new SerializedObject(d).FindProperty("nombre").stringValue : null);
        }

        [Test]
        public void Reemplazo_ManConUniformeAzulMarino_SeArmaSinC01()
        {
            Material marino = MechaCyberpunk.Personaje.Visual.Editor.ConectarVisualesT520.Variante("man", "PoliciaMarino", ColocarS1nPolicia.AzulMarino);
            try
            {
                GameObject prefab = ColocarS1nPolicia.Armar(RutaTemporal, "man", marino);
                SkinnedMeshRenderer[] r = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Assert.IsTrue(r.Length > 0 && r.All(x => x.sharedMaterial == marino), "Todo el cuerpo con el material azul marino");
                Assert.Less(marino.GetColor("_BaseColor").r, 0.2f);
                Assert.Greater(marino.GetColor("_BaseColor").b, marino.GetColor("_BaseColor").r, "Azul");
            }
            finally
            {
                AssetDatabase.DeleteAsset(RutaTemporal);
                ColocarS1nPolicia.Colocar();
            }
        }
    }
}

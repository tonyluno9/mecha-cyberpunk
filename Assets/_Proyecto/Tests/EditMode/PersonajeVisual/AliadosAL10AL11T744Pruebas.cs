using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-744: AL10 y AL11 se importan Humanoid con avatar válido y son el modelo de los dos aliados.</summary>
    public class AliadosAL10AL11T744Pruebas
    {
        [TestCase("AL10")]
        [TestCase("AL11")]
        public void Importacion_EsHumanoid_ConAvatarValido_YEscalaGuardada(string n)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(CreadorAliadosT744.RutaFbx(n));
            Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType);
            Assert.IsTrue(imp.userData.StartsWith("escala="), "escala en userData: " + imp.userData);
            Assert.AreEqual(MechaCyberpunk.Arte.Editor.PostprocesadorAssetsT250.EscalaPersonaje(imp.userData), imp.globalScale, 0.0001f);
            Avatar avatar = CreadorPJ01T732.AvatarDe(CreadorAliadosT744.RutaFbx(n));
            Assert.IsNotNull(avatar);
            Assert.IsTrue(avatar.isValid && avatar.isHuman, "Avatar Humanoid válido");
        }

        [TestCase("AL10")]
        [TestCase("AL11")]
        public void Visual_UsaSuModelo_SuMaterialUrp_YElControladorDeAliados(string n)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorAliadosT744.RutaVisual(n));
            Assert.IsNotNull(prefab);
            Animator animador = prefab.GetComponentInChildren<Animator>(true);
            Assert.AreEqual(CreadorPJ01T732.AvatarDe(CreadorAliadosT744.RutaFbx(n)), animador.avatar);
            Assert.AreEqual(ConstructorControladorBase.RutaOverride("Aliados"), AssetDatabase.GetAssetPath(animador.runtimeAnimatorController));
            Assert.AreSame(prefab.GetComponent<VisualPersonaje>().Animador, animador);
            foreach (SkinnedMeshRenderer r in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Assert.AreEqual(n + "_Base", r.sharedMaterial.name);
                Assert.AreEqual("Universal Render Pipeline/Lit", r.sharedMaterial.shader.name);
                Assert.IsNotNull(r.sharedMaterial.GetTexture("_BaseMap"));
                Assert.IsNotNull(r.sharedMaterial.GetTexture("_BumpMap"));
            }
        }

        [TestCase("Assets/_Proyecto/IA/Prefabs/Aliado.prefab", "AL10")]
        [TestCase("Assets/_Proyecto/IA/Prefabs/Aliado_AL11.prefab", "AL11")]
        public void PrefabsDeAliado_UsanAL10YAL11(string ruta, string n)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, ruta);
            VisualPersonaje visual = prefab.GetComponentInChildren<VisualPersonaje>(true);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<GameObject>(CreadorAliadosT744.RutaVisual(n)), PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject));
            Assert.IsNotNull(prefab.GetComponent("AliadoAPie"));
            Assert.AreEqual(1, prefab.GetComponentsInChildren<VisualPersonaje>(true).Length, "Un solo modelo");
        }
    }
}

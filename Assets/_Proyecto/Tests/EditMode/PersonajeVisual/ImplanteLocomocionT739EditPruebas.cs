using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-739 (A8): los cuatro clips de caminar y correr con y sin implante importan Humanoid con bucle, a la misma escala, y el controlador los usa por el parámetro ConImplante.</summary>
    public class ImplanteLocomocionT739EditPruebas
    {
        private const string Carpeta = "Assets/Arte/Personajes/Animaciones/Locomocion/";
        private static readonly string[] Clips = { "Loco_Caminar_SinImplante", "Loco_Caminar_ConImplante", "Loco_Correr_SinImplante", "Loco_Correr_ConImplante" };

        [TestCaseSource(nameof(Clips))]
        public void Clip_ImportaHumanoid_ConBucle_YDuracion(string nombre)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(Carpeta + nombre + ".fbx");
            Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType);
            AnimationClip clip = ConstructorControladorBase.Clip(nombre);
            Assert.IsNotNull(clip);
            Assert.IsTrue(clip.isLooping, nombre + " con bucle");
            Assert.Greater(clip.length, 0.3f);
            Assert.IsTrue(clip.isHumanMotion);
        }

        [Test]
        public void Escala_DelEsqueleto_LaCaderaMideLoMismoQueLoco_Caminar()
        {
            foreach (string nombre in Clips)
            {
                string raiz = nombre.Contains("Correr") ? "Loco_Correr" : "Loco_Caminar";
                Assert.AreEqual(CaderaY(Carpeta + raiz + ".fbx"), CaderaY(Carpeta + nombre + ".fbx"), 0.02f, nombre + ": mismo esqueleto a la misma escala (sin el problema de T-733)");
            }
        }

        private static float CaderaY(string ruta)
        {
            GameObject go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
            try
            {
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) { if (t.name.EndsWith(":Hips")) { return t.localPosition.y; } }
                return -1f;
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Velocidades_DeLosClips_SonLasMedidas()
        {
            Assert.AreEqual(ReglasAnimacion.VelocidadClipCaminarSinImplante, MedicionClips.Medir("Loco_Caminar_SinImplante"), 0.05f);
            Assert.AreEqual(ReglasAnimacion.VelocidadClipCaminarConImplante, MedicionClips.Medir("Loco_Caminar_ConImplante"), 0.05f);
            Assert.AreEqual(ReglasAnimacion.VelocidadClipCorrerSinImplante, MedicionClips.Medir("Loco_Correr_SinImplante"), 0.1f);
            Assert.AreEqual(ReglasAnimacion.VelocidadClipCorrerConImplante, MedicionClips.Medir("Loco_Correr_ConImplante"), 0.1f);
        }

        [Test]
        public void Controlador_TieneElParametroConImplante_ElArbolMezclaSinYCon_YElJugadorUsaLosSin()
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            Assert.IsTrue(System.Array.Exists(c.parameters, p => p.name == ReglasAnimacion.ConImplante && p.type == AnimatorControllerParameterType.Float));
            foreach (string estado in new[] { "Caminar", "Correr" })
            {
                AnimatorState s = null;
                foreach (ChildAnimatorState cs in c.layers[0].stateMachine.states) { if (cs.state.name == estado) { s = cs.state; } }
                var raiz = (BlendTree)s.motion;
                Assert.AreEqual(ReglasAnimacion.ConImplante, raiz.blendParameter);
                Assert.AreEqual(2, raiz.children.Length, "Un arbol sin implante y otro con implante");
            }
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride("Jugador"));
            var pares = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            string Valor(string clipBase) => pares.Find(p => p.Key != null && p.Key.name == clipBase).Value.name;
            Assert.AreEqual("PJ01_Caminar_SinImplante", Valor("Loco_Caminar")); // T-749: clips de Blender aplicados a PJ01
            Assert.AreEqual("PJ01_Correr_SinImplante", Valor("Loco_Correr"));
            Assert.AreEqual("PJ01_Caminar_ConImplante", Valor("Loco_Caminar_ConImplante"));
            var npc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride("NPC"));
            pares.Clear();
            npc.GetOverrides(pares);
            Assert.AreEqual("Loco_Caminar", Valor("Loco_Caminar"), "Los demas papeles siguen con Loco_Caminar");
        }
    }
}

using System.Collections.Generic;
using System.Linq;

using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-960 en adelante: los clips de Blender del 9-nov (imageassets/blender/pj01_clips) en el Animator del jugador.</summary>
    public class ClipsBlenderPJ01Pruebas
    {
        /// <summary>El Scale Factor de PJ01.fbx (ImportadorClipsPJ01.EscalaClips): los clips deben ir igual o la cadera queda a otra altura.</summary>
        private const float EscalaPJ01 = 1.88f;

        private static AnimatorController Base() => AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);

        private static AnimatorState Estado(int capa, string nombre) => Base().layers[capa].stateMachine.states.First(s => s.state.name == nombre).state;

        private static Dictionary<string, string> OverridesJugador()
        {
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride("Jugador"));
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            return pares.Where(p => p.Key != null && p.Value != null).ToDictionary(p => p.Key.name, p => p.Value.name);
        }

        private static ModelImporterClipAnimation ClipImportado(string nombre)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ConstructorControladorBase.CarpetaClipsPJ01 + "/" + nombre + ".fbx");
            Assert.IsNotNull(importer, "Falta " + nombre);
            Assert.AreEqual(1, importer.clipAnimations.Length, nombre);
            return importer.clipAnimations[0];
        }

        [TestCase("Idle", "Loco_Idle")]
        [TestCase("GirarDer", "Loco_GirarDer")]
        [TestCase("GirarIzq", "Loco_GirarIzq")]
        [TestCase("Frenar", "Loco_Frenar")]
        public void LocomocionBase_TieneVersionConImplante_PorElParametroConImplante(string estado, string clip)
        {
            var arbol = Estado(0, estado).motion as BlendTree;
            Assert.IsNotNull(arbol, estado + " debe ser un árbol por ConImplante");
            Assert.AreEqual(ReglasAnimacion.ConImplante, arbol.blendParameter);
            Assert.AreEqual(clip, arbol.children[0].motion.name);
            Assert.AreEqual(0f, arbol.children[0].threshold);
            Assert.AreEqual("PJ01_" + clip + "_ConImplante", arbol.children[1].motion.name);
            Assert.AreEqual(1f, arbol.children[1].threshold);
        }

        [Test]
        public void Caminar_ConImplante_UsaLosLadosYAtrasConImplante()
        {
            var raiz = (BlendTree)Estado(0, "Caminar").motion;
            var con = (BlendTree)raiz.children[1].motion;
            CollectionAssert.AreEquivalent(
                new[] { "Loco_Caminar_ConImplante", "PJ01_Loco_CaminarAtras_ConImplante", "PJ01_Loco_CaminarDer_ConImplante", "PJ01_Loco_CaminarIzq_ConImplante" },
                con.children.Select(h => h.motion.name).ToArray());
            var sin = (BlendTree)raiz.children[0].motion;
            CollectionAssert.Contains(sin.children.Select(h => h.motion.name).ToArray(), "Loco_CaminarAtras");
        }

        [TestCase("Loco_Idle", "PJ01_Loco_Idle")]
        [TestCase("Loco_CaminarAtras", "PJ01_Loco_CaminarAtras")]
        [TestCase("Loco_CaminarDer", "PJ01_Loco_CaminarDer")]
        [TestCase("Loco_CaminarIzq", "PJ01_Loco_CaminarIzq")]
        [TestCase("Loco_GirarDer", "PJ01_Loco_GirarDer")]
        [TestCase("Loco_GirarIzq", "PJ01_Loco_GirarIzq")]
        [TestCase("Loco_Frenar", "PJ01_Loco_Frenar")]
        [TestCase("Loco_SaltoInicio", "PJ01_Loco_SaltoInicio")]
        [TestCase("Loco_SaltoAire", "PJ01_Loco_SaltoAire")]
        [TestCase("Loco_SaltoAterrizaje", "PJ01_Loco_SaltoAterrizaje")]
        [TestCase("Loco_Caida", "PJ01_Loco_Caida")]
        public void Jugador_UsaElClipDeBlender(string clipBase, string clipPJ01)
        {
            Assert.AreEqual(clipPJ01, OverridesJugador()[clipBase]);
        }

        [Test]
        public void OtrosPapeles_NoCambianSuLocomocion()
        {
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride("NPC"));
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            Assert.AreEqual("Loco_Idle", pares.First(p => p.Key.name == "Loco_Idle").Value.name);
        }

        [TestCase("PJ01_Loco_SaltoAire", false)]
        [TestCase("PJ01_Loco_Caida", true)]
        [TestCase("PJ01_Loco_Idle", true)]
        [TestCase("PJ01_Loco_Idle_ConImplante", true)]
        [TestCase("PJ01_Loco_CaminarAtras_ConImplante", true)]
        [TestCase("PJ01_Loco_GirarDer", false)]
        [TestCase("PJ01_Loco_Frenar", false)]
        [TestCase("PJ01_Carro_Manejar", true)]
        [TestCase("PJ01_Moto_Manejar", true)]
        [TestCase("PJ01_Inter_VerTelefono", true)]
        [TestCase("PJ01_Muerte_Adelante", false)]
        public void Importador_BucleDeCadaClip(string clip, bool bucle)
        {
            Assert.AreEqual(bucle, ClipImportado(clip).loopTime, clip);
        }

        [Test]
        public void Importador_LosClipsNuevosVanConLaEscalaYElAvatarDePJ01()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ConstructorControladorBase.CarpetaClipsPJ01 }))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                Assert.AreEqual(EscalaPJ01, importer.globalScale, 1e-4f, importer.assetPath);
                Assert.AreEqual(ModelImporterAnimationType.Human, importer.animationType, importer.assetPath);
            }
        }

        [Test]
        public void Salto_DatosDelJugador_SubenCeroCuarentaYCincoConGravedadDieciseis()
        {
            var datos = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.DatosPersonaje>(AjustarSaltoT961.RutaDatosPersonaje);
            var config = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.ConfigControlesAPie>(AjustarSaltoT961.RutaConfigControles);
            Assert.AreEqual(0.45f, datos.AlturaSalto, 1e-4f);
            Assert.AreEqual(16f, config.Gravedad, 1e-4f);
            float segundosAire = 2f * Mathf.Sqrt(2f * datos.AlturaSalto / config.Gravedad);
            Assert.AreEqual(0.47f, segundosAire, 0.01f, "como la muestra de Blender (BITACORA 03)");
        }

        [Test]
        public void SaltoAire_DelJugador_NoEsBucle_YSeQuedaHastaAterrizar()
        {
            AnimationClip aire = AssetDatabase.LoadAllAssetsAtPath(ConstructorControladorBase.CarpetaClipsPJ01 + "/PJ01_Loco_SaltoAire.fbx")
                .OfType<AnimationClip>().First(c => c.name == "PJ01_Loco_SaltoAire");
            Assert.IsFalse(aire.isLooping);
            AnimatorState s = Estado(0, "SaltoAire");
            Assert.IsFalse(s.transitions.Any(t => t.destinationState.name == "SaltoAterrizaje" && t.hasExitTime), "solo aterriza al tocar el piso");
        }
    }
}

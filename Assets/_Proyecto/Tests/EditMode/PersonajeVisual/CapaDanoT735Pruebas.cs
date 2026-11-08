using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-735 (A5): la reaccion al dano son los Pistola_Dano_* de Tony en una capa aditiva con la mascara del torso.</summary>
    public class CapaDanoT735Pruebas
    {
        private static AnimatorController Base() => AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);

        private static AnimatorState Estado(int capa, string nombre) => Base().layers[capa].stateMachine.states.First(s => s.state.name == nombre).state;

        [TestCase("DanoFrente", "Pistola_Dano_Frente", 0)]
        [TestCase("DanoEspalda", "Pistola_Dano_Espalda", 1)]
        [TestCase("DanoFuerte", "Pistola_Dano_Fuerte", 2)]
        public void CapaDano_TieneLosClipsDeTony_ElegidosPorDanoTipo_YVuelveAVacio(string estado, string clip, int tipo)
        {
            AnimatorState s = Estado(2, estado);
            Assert.AreEqual(clip, s.motion.name);
            Assert.AreEqual(1f, s.speed, 0.001f, "Los clips duran 0.47 s tal como vienen");
            AnimatorStateTransition entrada = Base().layers[2].stateMachine.anyStateTransitions.First(t => t.destinationState == s);
            Assert.IsTrue(entrada.canTransitionToSelf, "Otro golpe del mismo tipo reinicia el estremecimiento, no se encima");
            Assert.IsTrue(entrada.conditions.Any(c => c.parameter == ReglasVisual.Herido));
            Assert.IsTrue(entrada.conditions.Any(c => c.parameter == ReglasAnimacion.DanoTipo && (int)c.threshold == tipo));
            AnimatorStateTransition salida = s.transitions.First(t => t.destinationState != null && t.destinationState.name == "Vacio");
            Assert.IsTrue(salida.hasExitTime);
            Assert.GreaterOrEqual(salida.exitTime, 0.9f, "Termina completo: acaba en la pose de apuntar");
        }

        [Test]
        public void LaCapaDeArriba_YaNoTieneEstadosDeDano_ElControladorNoUsaLosDanoViejos()
        {
            var arriba = Base().layers[1].stateMachine.states.Select(s => s.state.name).ToArray();
            CollectionAssert.DoesNotContain(arriba, "DanoFrente");
            CollectionAssert.DoesNotContain(arriba, "DanoEspalda");
            CollectionAssert.DoesNotContain(arriba, "DanoFuerte");
            var usados = Base().layers.SelectMany(l => l.stateMachine.states).Select(s => s.state.motion != null ? s.state.motion.name : "").ToArray();
            CollectionAssert.DoesNotContain(usados, "Dano_Frente");
            CollectionAssert.DoesNotContain(usados, "Dano_Espalda");
            CollectionAssert.DoesNotContain(usados, "Dano_Fuerte");
        }

        [TestCase("Pistola_Dano_Frente")]
        [TestCase("Pistola_Dano_Espalda")]
        [TestCase("Pistola_Dano_Fuerte")]
        public void LosClips_TienenPosDeReferenciaAditiva_ElPrimerCuadro(string nombre)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath("Assets/Arte/Personajes/Animaciones/Dano/" + nombre + ".fbx");
            ModelImporterClipAnimation clip = imp.clipAnimations[0];
            Assert.IsTrue(clip.hasAdditiveReferencePose);
            Assert.AreEqual(clip.firstFrame, clip.additiveReferencePoseFrame, 0.001f, "Empiezan en Pistola_Apuntar");
            Assert.IsFalse(clip.loopTime);
        }

        [Test]
        public void ElUmbralDelDanoFuerte_EsUnaCuartaParteDeLaVida()
        {
            Assert.AreEqual(0.25f, ReglasAnimacion.FraccionDanoFuerte, 0.0001f);
            Assert.AreEqual(DanoVisual.Fuerte, ReglasAnimacion.ElegirDano(Vector3.back, Vector3.forward, true));
            Assert.AreEqual(DanoVisual.Frente, ReglasAnimacion.ElegirDano(Vector3.back, Vector3.forward, false));
            Assert.AreEqual(DanoVisual.Espalda, ReglasAnimacion.ElegirDano(Vector3.forward, Vector3.forward, false));
        }
    }
}

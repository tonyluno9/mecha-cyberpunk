using System.Linq;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-675: la reacción al daño del controlador base es corta (sale a la mitad del clip) y vuelve al estado vacío.</summary>
    public class ReaccionDanoT675Pruebas
    {
        [TestCase("DanoFrente")]
        [TestCase("DanoEspalda")]
        [TestCase("DanoFuerte")]
        public void Dano_SaleAntesDeLaMitadYNoQuedaTrabado(string estado)
        {
            AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            AnimatorState s = c.layers[1].stateMachine.states.First(x => x.state.name == estado).state;
            AnimatorStateTransition salida = s.transitions.First(t => t.destinationState != null && t.destinationState.name == "Vacio");
            Assert.IsTrue(salida.hasExitTime);
            Assert.LessOrEqual(salida.exitTime, 0.6f, "Sale a lo más a 60 % del clip");
            Assert.GreaterOrEqual(s.speed, 2.5f, "Y a buena rapidez");
        }
    }
}

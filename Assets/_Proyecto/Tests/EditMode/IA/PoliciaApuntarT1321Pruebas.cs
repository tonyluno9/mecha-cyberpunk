using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-1321: el policía anima con su velocidad real y apuntando camina (el estado de apuntar es un árbol de Velocidad).</summary>
    public class PoliciaApuntarT1321Pruebas
    {
        [Test]
        public void VelocidadVista_SinPedirMovimiento_EsCero()
        {
            Assert.AreEqual(0f, ReglaAnimacionPolicia.VelocidadVista(2f, 0f));
        }

        [Test]
        public void VelocidadVista_PidiendoYMoviendose_EsLaReal()
        {
            Assert.AreEqual(1.2f, ReglaAnimacionPolicia.VelocidadVista(1.2f, 1.4f), 0.0001f);
        }

        [Test]
        public void VelocidadVista_PidiendoPeroBloqueado_EsCeroNoLoPedido()
        {
            Assert.AreEqual(0f, ReglaAnimacionPolicia.VelocidadVista(0f, 1.4f), 0.0001f);
        }

        [Test]
        public void VelocidadVista_NuncaPasaDeLoPedido()
        {
            Assert.AreEqual(1.4f, ReglaAnimacionPolicia.VelocidadVista(5f, 1.4f), 0.0001f);
        }

        // Necesita haber corrido MechaCyberpunk.IA.Editor.PoliciaApuntarCaminarT1321.Aplicar.
        [Test]
        public void ControladorPolicia_ApuntarPistola_EsUnArbolSobreVelocidad()
        {
            AnimatorController controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Arte/Personajes/Policia/Policia.controller");
            Assert.IsNotNull(controlador);
            BlendTree arbol = null;
            foreach (ChildAnimatorState hijo in controlador.layers[0].stateMachine.states)
            {
                if (hijo.state.name == "ApuntarPistola")
                {
                    arbol = hijo.state.motion as BlendTree;
                }
            }
            Assert.IsNotNull(arbol, "ApuntarPistola debe ser un BlendTree");
            Assert.AreEqual("Velocidad", arbol.blendParameter);
            Assert.AreEqual(3, arbol.children.Length);
            Assert.AreEqual(0f, arbol.children[0].threshold);
            Assert.Greater(arbol.children[1].threshold, 0.5f);
        }
    }
}

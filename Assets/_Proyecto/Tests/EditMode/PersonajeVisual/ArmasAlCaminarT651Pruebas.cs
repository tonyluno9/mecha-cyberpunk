using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-651: con arma en la mano (sin apuntar) las piernas usan Pistola_Caminar/Correr y Rifle_Caminar/Correr.</summary>
    public class ArmasAlCaminarT651Pruebas
    {
        private static AnimatorController Controlador() => AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);

        [Test]
        public void CapaPiernas_TieneMascaraSoloDePiernas_YPesoCeroPorDefecto()
        {
            AnimatorControllerLayer capa = Controlador().layers[2];
            Assert.AreEqual(0f, capa.defaultWeight);
            Assert.AreEqual(AnimatorLayerBlendingMode.Override, capa.blendingMode);
            Assert.IsTrue(capa.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg));
            Assert.IsTrue(capa.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg));
            Assert.IsFalse(capa.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body), "El torso es de la capa de arriba");
            Assert.IsFalse(capa.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm));
        }

        [TestCase("PistolaPiernas", "Pistola_Caminar", "Pistola_Correr")]
        [TestCase("LargaPiernas", "Rifle_Caminar", "Rifle_Correr")]
        public void CapaPiernas_MezclaCaminarYCorrerDelArma_PorVelocidad(string estado, string caminar, string correr)
        {
            AnimatorState s = Controlador().layers[2].stateMachine.states.First(x => x.state.name == estado).state;
            var arbol = (BlendTree)s.motion;
            Assert.AreEqual(ReglasVisual.Velocidad, arbol.blendParameter);
            Assert.AreEqual(2, arbol.children.Length);
            Assert.AreEqual(caminar, arbol.children[0].motion.name);
            Assert.AreEqual(ReglasAnimacion.VelocidadAndar, arbol.children[0].threshold, 0.001f);
            Assert.AreEqual(correr, arbol.children[1].motion.name);
            Assert.AreEqual(ReglasAnimacion.VelocidadCarrera, arbol.children[1].threshold, 0.001f);
        }

        [Test]
        public void UsaPiernasDeArma_ConArmaCaminando_EsVerdadero()
        {
            Assert.IsTrue(ReglasAnimacion.UsaPiernasDeArma(TipoArmaVisual.Pistola, true, false, false, false, false, false, false, 4.5f));
            Assert.IsTrue(ReglasAnimacion.UsaPiernasDeArma(TipoArmaVisual.ArmaLarga, true, false, false, false, false, false, false, 7.5f));
        }

        [Test]
        public void UsaPiernasDeArma_SinArmaOApuntandoOQuieto_EsFalso()
        {
            Assert.IsFalse(ReglasAnimacion.UsaPiernasDeArma(TipoArmaVisual.Ninguna, true, false, false, false, false, false, false, 4.5f), "puños");
            Assert.IsFalse(ReglasAnimacion.UsaPiernasDeArma(TipoArmaVisual.Pistola, true, true, false, false, false, false, false, 4.5f), "apuntando");
            Assert.IsFalse(ReglasAnimacion.UsaPiernasDeArma(TipoArmaVisual.Pistola, true, false, false, false, false, false, false, 0.2f), "quieto");
            Assert.IsFalse(ReglasAnimacion.UsaPiernasDeArma(TipoArmaVisual.Pistola, false, false, false, false, false, false, false, 4.5f), "en el aire");
            Assert.IsFalse(ReglasAnimacion.UsaPiernasDeArma(TipoArmaVisual.Pistola, true, false, false, true, false, false, false, 4.5f), "cubierto");
            Assert.IsFalse(ReglasAnimacion.UsaPiernasDeArma(TipoArmaVisual.Pistola, true, false, false, false, false, false, true, 4.5f), "manejando");
        }

        [Test]
        public void VelocidadClipArma_InterpolaEntreCaminarYCorrer()
        {
            Assert.AreEqual(ReglasAnimacion.VelocidadClipPistolaCaminar, ReglasAnimacion.VelocidadClipArma(TipoArmaVisual.Pistola, ReglasAnimacion.VelocidadAndar), 0.001f);
            Assert.AreEqual(ReglasAnimacion.VelocidadClipRifleCorrer, ReglasAnimacion.VelocidadClipArma(TipoArmaVisual.ArmaLarga, ReglasAnimacion.VelocidadCarrera), 0.001f);
        }

        [TestCase("Pistola_Caminar", ReglasAnimacion.VelocidadClipPistolaCaminar)]
        [TestCase("Pistola_Correr", ReglasAnimacion.VelocidadClipPistolaCorrer)]
        [TestCase("Rifle_Caminar", ReglasAnimacion.VelocidadClipRifleCaminar)]
        [TestCase("Rifle_Correr", ReglasAnimacion.VelocidadClipRifleCorrer)]
        public void ConstantesDeVelocidad_CoincidenConLoMedidoDelClip(string clip, float constante)
        {
            Assert.AreEqual(constante, MedicionClips.Medir(clip), constante * 0.1f, clip);
        }
    }
}

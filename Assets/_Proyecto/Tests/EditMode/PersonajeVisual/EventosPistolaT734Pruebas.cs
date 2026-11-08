using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-734 (A4): Pistola_Sacar activa la pistola a los 0.30 s y Pistola_Guardar la desactiva a los 0.53 s, con Animation Events escritos en el .meta del clip.</summary>
    public class EventosPistolaT734Pruebas
    {
        private const string Carpeta = "Assets/Arte/Personajes/Animaciones/Pistola/";

        private static AnimationClip Clip(string nombre)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(Carpeta + nombre + ".fbx"))
            {
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) { return c; }
            }
            return null;
        }

        [Test]
        public void Sacar_TieneUnEventoALos030Segundos_QueLlamaAlReceptor()
        {
            AnimationClip clip = Clip("Pistola_Sacar");
            AnimationEvent[] eventos = AnimationUtility.GetAnimationEvents(clip);
            Assert.AreEqual(1, eventos.Length);
            Assert.AreEqual(ReceptorEventosArma.FuncionSacar, eventos[0].functionName);
            Assert.AreEqual(0.30f, eventos[0].time, 0.02f, "Fotograma 10 de Tony");
        }

        [Test]
        public void Guardar_TieneUnEventoALos053Segundos_QueLlamaAlReceptor()
        {
            AnimationClip clip = Clip("Pistola_Guardar");
            AnimationEvent[] eventos = AnimationUtility.GetAnimationEvents(clip);
            Assert.AreEqual(1, eventos.Length);
            Assert.AreEqual(ReceptorEventosArma.FuncionGuardar, eventos[0].functionName);
            Assert.AreEqual(0.53f, eventos[0].time, 0.02f, "Fotograma 17 de Tony");
        }

        [Test]
        public void LosEventos_VanEnElMetaDelClip_YElReceptorTieneLasFuncionesDelEvento()
        {
            foreach (string n in new[] { "Pistola_Sacar", "Pistola_Guardar" })
            {
                string meta = System.IO.File.ReadAllText(Carpeta + n + ".fbx.meta");
                StringAssert.Contains("functionName: " + (n == "Pistola_Sacar" ? "AlSacarPistola" : "AlGuardarPistola"), meta);
            }
            Assert.IsNotNull(typeof(ReceptorEventosArma).GetMethod("AlSacarPistola"));
            Assert.IsNotNull(typeof(ReceptorEventosArma).GetMethod("AlGuardarPistola"));
        }

        [Test]
        public void LosDemasClipsDePistola_NoTienenEventos()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Carpeta.TrimEnd('/') }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
                if (nombre == "Pistola_Sacar" || nombre == "Pistola_Guardar") { continue; }
                AnimationClip clip = Clip(nombre);
                Assert.AreEqual(0, AnimationUtility.GetAnimationEvents(clip).Length, nombre);
            }
        }

        [Test]
        public void ReSacarSiSeVuelveAApuntarMientrasSeGuarda()
        {
            var c = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Arte/Personajes/Animaciones/ControladorBase.controller");
            var guardar = c.layers[1].stateMachine.states.First(s => s.state.name == "PistolaGuardar").state;
            Assert.IsTrue(guardar.transitions.Any(t => t.destinationState.name == "PistolaSacar" && t.conditions.Any(x => x.parameter == ReglasVisual.TipoArma)));
        }
    }
}

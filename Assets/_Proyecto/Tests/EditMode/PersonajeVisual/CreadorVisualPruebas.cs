using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class CreadorVisualPruebas
    {
        private const string RutaTemporal = "Assets/_Proyecto/Personaje/Visual/Prefabs/ControladorTemporal_Prueba.controller";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(RutaTemporal);
        }

        [Test]
        public void Construir_ControladorTemporal_TieneTodosLosEstadosYParametros()
        {
            AnimatorController c = ConstructorControladorVisual.Construir(RutaTemporal, new Dictionary<ClipVisual, AnimationClip>(), out List<ClipVisual> faltan);

            string[] estados = c.layers[0].stateMachine.states.Select(s => s.state.name).ToArray();
            foreach (string esperado in ReglasVisual.Estados)
            {
                CollectionAssert.Contains(estados, esperado);
            }
            Assert.AreEqual(ReglasVisual.Estados.Length, estados.Length);
            Assert.AreEqual(ReglasVisual.Estados.Length, faltan.Count, "sin clips, todos faltan");

            var parametros = c.parameters.ToDictionary(p => p.name, p => p.type);
            Assert.AreEqual(AnimatorControllerParameterType.Float, parametros[ReglasVisual.Velocidad]);
            foreach (string p in ReglasVisual.ParametrosBool) { Assert.AreEqual(AnimatorControllerParameterType.Bool, parametros[p], p); }
            Assert.AreEqual(AnimatorControllerParameterType.Int, parametros[ReglasVisual.TipoArma]);
            foreach (string p in ReglasVisual.ParametrosTrigger) { Assert.AreEqual(AnimatorControllerParameterType.Trigger, parametros[p], p); }
            Assert.AreEqual(11, parametros.Count);
        }

        [Test]
        public void Construir_DosVeces_NoDuplicaEstados()
        {
            ConstructorControladorVisual.Construir(RutaTemporal, null, out _);
            AnimatorController c = ConstructorControladorVisual.Construir(RutaTemporal, null, out _);

            Assert.AreEqual(ReglasVisual.Estados.Length, c.layers[0].stateMachine.states.Length);
            Assert.AreEqual(11, c.parameters.Length);
        }

        [Test]
        public void Construir_CadaTriggerTieneTransicionDeEntrada()
        {
            AnimatorController c = ConstructorControladorVisual.Construir(RutaTemporal, null, out _);
            HashSet<string> usados = new HashSet<string>();
            foreach (AnimatorStateTransition t in c.layers[0].stateMachine.anyStateTransitions)
            {
                foreach (AnimatorCondition cond in t.conditions) { usados.Add(cond.parameter); }
            }
            foreach (string p in ReglasVisual.ParametrosTrigger)
            {
                Assert.IsTrue(usados.Contains(p), p + " no dispara ningún estado");
            }
            Assert.IsTrue(usados.Contains(ReglasVisual.Caer));
            Assert.IsTrue(usados.Contains(ReglasVisual.Cubierto));
        }

        [Test]
        public void Construir_ConClip_AsignaElMovimientoAlEstado()
        {
            AnimationClip clip = new AnimationClip { name = "Correr" };
            var clips = new Dictionary<ClipVisual, AnimationClip> { [ClipVisual.Correr] = clip };

            AnimatorController c = ConstructorControladorVisual.Construir(RutaTemporal, clips, out List<ClipVisual> faltan);

            AnimatorState estado = c.layers[0].stateMachine.states.First(s => s.state.name == "Correr").state;
            Assert.AreSame(clip, estado.motion);
            CollectionAssert.DoesNotContain(faltan, ClipVisual.Correr);
            Object.DestroyImmediate(clip);
        }

        [TestCase("Caminar.fbx", ClipVisual.Caminar)]
        [TestCase("Walking.fbx", ClipVisual.Caminar)]
        [TestCase("Rifle Idle.fbx", ClipVisual.ApuntarLarga)]
        [TestCase("Pistol Idle.fbx", ClipVisual.ApuntarPistola)]
        [TestCase("Hit Reaction.fbx", ClipVisual.RecibirGolpe)]
        [TestCase("Idle.fbx", ClipVisual.Idle)]
        [TestCase("Rodar.fbx", ClipVisual.Rodar)]
        public void TryClipDeArchivo_NombreConocido_Reconoce(string archivo, ClipVisual esperado)
        {
            Assert.IsTrue(ReglasVisual.TryClipDeArchivo(archivo, out ClipVisual clip));
            Assert.AreEqual(esperado, clip);
        }

        [Test]
        public void ElegirModelo_PrefiereModeloYSaltaAnimaciones()
        {
            Assert.AreEqual("Modelo.fbx", ReglasVisual.ElegirModelo(new[] { "Idle.fbx", "Modelo.fbx", "Pandillero.fbx" }));
            Assert.AreEqual("Pandillero.fbx", ReglasVisual.ElegirModelo(new[] { "Idle.fbx", "Pandillero.fbx", "Correr.fbx" }));
            Assert.IsNull(ReglasVisual.ElegirModelo(new[] { "Idle.fbx", "Correr.fbx" }));
        }

        [Test]
        public void CrearVisualPrueba_GeneraPrefabConAnimatorYControladorCompleto()
        {
            CreadorVisualPrueba.Crear();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(CreadorVisualPrueba.Nombre));
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<VisualPersonaje>());
            Animator animador = prefab.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animador);
            AnimatorController c = (AnimatorController)animador.runtimeAnimatorController;
            Assert.IsNotNull(c);
            foreach (ChildAnimatorState s in c.layers[0].stateMachine.states)
            {
                Assert.IsNotNull(s.state.motion, "Visual_Prueba: estado sin clip " + s.state.name);
            }
        }
    }
}

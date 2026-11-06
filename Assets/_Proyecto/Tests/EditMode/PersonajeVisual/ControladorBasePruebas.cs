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
    /// <summary>T-561: estados, parámetros y overrides del controlador base humanoide, y que los visuales lo usan.</summary>
    public class ControladorBasePruebas
    {
        private AnimatorController Base() => AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);

        private static string[] Estados(AnimatorStateMachine m) => m.states.Select(s => s.state.name).ToArray();

        [Test]
        public void Controlador_TieneLasDosCapasConLaMascaraDelTorso()
        {
            AnimatorController c = Base();
            Assert.IsNotNull(c);
            Assert.AreEqual(2, c.layers.Length, "T-671: sin capa Piernas (caminar y correr con arma es la caminata normal)");
            Assert.IsTrue(c.layers[1].iKPass, "IK Pass también en la capa de arriba (mano izquierda al arma)");
            Assert.AreEqual(ReglasAnimacion.CapaBase, c.layers[0].name);
            Assert.AreEqual(ReglasAnimacion.CapaArriba, c.layers[1].name);
            Assert.IsTrue(c.layers[0].iKPass, "IK Pass para pies y mirada");
            AvatarMask mascara = c.layers[1].avatarMask;
            Assert.IsNotNull(mascara);
            Assert.IsTrue(mascara.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body));
            Assert.IsTrue(mascara.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm));
            Assert.IsFalse(mascara.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg), "Las piernas son de la capa base");
            Assert.IsFalse(mascara.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg));
        }

        [Test]
        public void Controlador_TieneTodosLosEstadosDeCadaCapa_ConClip()
        {
            AnimatorController c = Base();
            string[] baseE = Estados(c.layers[0].stateMachine);
            foreach (string e in ReglasAnimacion.EstadosBase) { CollectionAssert.Contains(baseE, e); }
            Assert.AreEqual(ReglasAnimacion.EstadosBase.Length, baseE.Length);
            string[] arribaE = Estados(c.layers[1].stateMachine);
            foreach (string e in ReglasAnimacion.EstadosArriba) { CollectionAssert.Contains(arribaE, e); }
            Assert.AreEqual(ReglasAnimacion.EstadosArriba.Length, arribaE.Length);
            foreach (ChildAnimatorState s in c.layers[0].stateMachine.states.Concat(c.layers[1].stateMachine.states))
            {
                if (s.state.name != "Vacio")
                {
                    Assert.IsNotNull(s.state.motion, "Estado sin clip: " + s.state.name);
                }
            }
        }

        [Test]
        public void Controlador_ConservaLosParametrosQueYaUsaElJuego_YAgregaLosNuevos()
        {
            AnimatorController c = Base();
            var tipos = c.parameters.ToDictionary(p => p.name, p => p.type);
            foreach (string viejo in new[] { "Velocidad", "EnPiso", "Apuntando", "Arma", "Disparo", "Recarga", "Golpe", "Herido", "KO", "Cubierto" })
            {
                Assert.IsTrue(tipos.ContainsKey(viejo), "Falta el parámetro que ya usa el juego: " + viejo);
            }
            foreach (string f in ReglasAnimacion.ParametrosFloat) { Assert.AreEqual(AnimatorControllerParameterType.Float, tipos[f], f); }
            foreach (string b in ReglasAnimacion.ParametrosBool) { Assert.AreEqual(AnimatorControllerParameterType.Bool, tipos[b], b); }
            foreach (string i in ReglasAnimacion.ParametrosInt) { Assert.AreEqual(AnimatorControllerParameterType.Int, tipos[i], i); }
            foreach (string t in ReglasAnimacion.ParametrosTrigger) { Assert.AreEqual(AnimatorControllerParameterType.Trigger, tipos[t], t); }
            Assert.AreEqual(1f, c.parameters.First(p => p.name == "VelZ").defaultFloat, "Quien no calcula la dirección camina de frente");
            Assert.IsTrue(c.parameters.First(p => p.name == "EnPiso").defaultBool);
        }

        [Test]
        public void Controlador_TodaCondicionUsaUnParametroQueExiste()
        {
            AnimatorController c = Base();
            var nombres = new HashSet<string>(c.parameters.Select(p => p.name));
            foreach (AnimatorControllerLayer capa in c.layers)
            {
                var transiciones = new List<AnimatorStateTransition>(capa.stateMachine.anyStateTransitions);
                foreach (ChildAnimatorState s in capa.stateMachine.states) { transiciones.AddRange(s.state.transitions); }
                Assert.Greater(transiciones.Count, 10);
                foreach (AnimatorStateTransition t in transiciones)
                {
                    foreach (AnimatorCondition cond in t.conditions)
                    {
                        Assert.IsTrue(nombres.Contains(cond.parameter), capa.name + ": parámetro inexistente " + cond.parameter);
                    }
                }
            }
        }

        [Test]
        public void Locomocion_EsUnArbolDosDeDirecciones_SobreVelXYVelZ()
        {
            AnimatorState caminar = Base().layers[0].stateMachine.states.First(s => s.state.name == "Caminar").state;
            BlendTree arbol = caminar.motion as BlendTree;
            Assert.IsNotNull(arbol);
            Assert.AreEqual(BlendTreeType.SimpleDirectional2D, arbol.blendType);
            Assert.AreEqual("VelX", arbol.blendParameter);
            Assert.AreEqual("VelZ", arbol.blendParameterY);
            Assert.AreEqual(4, arbol.children.Length, "adelante, atrás, derecha e izquierda");
            CollectionAssert.AreEquivalent(
                new[] { new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(1f, 0f), new Vector2(-1f, 0f) },
                arbol.children.Select(h => h.position).ToArray());
        }

        [Test]
        public void CoberturaMantener_ConservaLaUltimaPoseDeEntrar()
        {
            AnimatorState mantener = Base().layers[0].stateMachine.states.First(s => s.state.name == "CoberturaMantener").state;
            Assert.IsTrue(mantener.timeParameterActive);
            Assert.AreEqual(ReglasAnimacion.PoseCobertura, mantener.timeParameter);
            Assert.AreEqual("Cobertura_Entrar", mantener.motion.name);
            Assert.Greater(Base().parameters.First(p => p.name == ReglasAnimacion.PoseCobertura).defaultFloat, 0.9f);
        }

        [Test]
        public void Overrides_UnoPorPapel_SobreElControladorBase()
        {
            foreach (string papel in ConstructorControladorBase.Papeles)
            {
                var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride(papel));
                Assert.IsNotNull(o, "Falta el override de " + papel);
                Assert.AreEqual(ConstructorControladorBase.RutaControlador, AssetDatabase.GetAssetPath(o.runtimeAnimatorController), papel);
                var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                Assert.Greater(pares.Count, 40, "Cubre los clips del base");
                foreach (var par in pares) { Assert.IsNotNull(par.Value, papel + ": clip sin asignar " + par.Key.name); }
            }
            CollectionAssert.IsSupersetOf(ConstructorControladorBase.Papeles, new[] { "Jugador", "Aliados", "NPC", "SKULLS", "SNAKES", "Guardias", "Robots", "Profes" });
        }

        [Test]
        public void VisualesRepartidos_UsanElOverrideDeSuPapel_SinManosPorIK_ConIKDePiesYMirada()
        {
            foreach ((string modelo, string papel, bool mira) in ConectarControladorBase.Reparto)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(modelo));
                Animator a = prefab.GetComponentInChildren<Animator>(true);
                Assert.AreEqual(ConstructorControladorBase.RutaOverride(papel), AssetDatabase.GetAssetPath(a.runtimeAnimatorController), modelo);
                ApuntadoIK apuntado = prefab.GetComponentInChildren<ApuntadoIK>(true);
                if (apuntado != null) { Assert.IsFalse(apuntado.ManosPorIK, modelo + ": ya hay clip de apuntar"); }
                IKPiesYMirada ik = a.GetComponent<IKPiesYMirada>();
                Assert.IsNotNull(ik, modelo);
                Assert.AreEqual(mira, ik.MirarAlJugador, modelo);
                Assert.IsFalse(a.applyRootMotion, modelo);
            }
        }

        [Test]
        public void Visuales_LaVelocidadDeCaminarYCorrerSeMidioDelClip()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab("woman"));
            var so = new SerializedObject(prefab.GetComponent<VisualPersonaje>());
            Assert.Greater(so.FindProperty("velocidadClipCaminar").floatValue, 0.5f);
            Assert.Greater(so.FindProperty("velocidadClipCorrer").floatValue, so.FindProperty("velocidadClipCaminar").floatValue);
        }
    }
}

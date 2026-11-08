using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-733 (A3): el controlador base usa los clips de Tony para agachado, rodar, sacar y guardar la pistola y apuntar en cuatro direcciones.</summary>
    public class ControladorPJ01T733Pruebas
    {
        private static AnimatorController Base() => AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);

        private static AnimatorState Estado(int capa, string nombre) => Base().layers[capa].stateMachine.states.First(s => s.state.name == nombre).state;

        private static string[] Clips(Motion m)
        {
            if (m is BlendTree arbol) { return arbol.children.SelectMany(h => Clips(h.motion)).ToArray(); }
            return m != null ? new[] { m.name } : new string[0];
        }

        [Test]
        public void Agachado_UsaLosClipsDeAgachadoDeTony_YNoLaPoseDeCobertura()
        {
            CollectionAssert.AreEqual(new[] { "Loco_AgachadoIdle" }, Clips(Estado(0, "AgachadoQuieto").motion));
            CollectionAssert.AreEquivalent(
                new[] { "Loco_AgachadoCaminar", "Loco_AgachadoAtras", "Loco_AgachadoLateral_Derecha", "Loco_AgachadoLateral_Izquierda" },
                Clips(Estado(0, "AgachadoCaminar").motion));
            Assert.AreEqual("Loco_Agacharse", Estado(0, "Agacharse").motion.name);
            Assert.AreEqual("Loco_Pararse", Estado(0, "Pararse").motion.name);
            Assert.IsFalse(Estado(0, "AgachadoQuieto").timeParameterActive, "Ya no se congela una pose de Cobertura_Entrar");
            var arbol = (BlendTree)Estado(0, "AgachadoCaminar").motion;
            Assert.AreEqual(BlendTreeType.SimpleDirectional2D, arbol.blendType);
            Assert.AreEqual("VelX", arbol.blendParameter);
            Assert.AreEqual("VelZ", arbol.blendParameterY);
        }

        [Test]
        public void Agachado_SeBajaConAgacharse_YSeLevantaConPararse()
        {
            AnimatorState caminar = Estado(0, "Caminar");
            Assert.IsTrue(caminar.transitions.Any(t => t.destinationState != null && t.destinationState.name == "Agacharse"
                && t.conditions.Any(c => c.parameter == ReglasAnimacion.Agachado && c.mode == AnimatorConditionMode.If)));
            Assert.IsTrue(Estado(0, "Agacharse").transitions.Any(t => t.destinationState.name == "AgachadoQuieto"));
            Assert.IsTrue(Estado(0, "Agacharse").transitions.Any(t => t.destinationState.name == "AgachadoCaminar"));
            foreach (string s in new[] { "AgachadoQuieto", "AgachadoCaminar" })
            {
                Assert.IsTrue(Estado(0, s).transitions.Any(t => t.destinationState.name == "Pararse"
                    && t.conditions.Any(c => c.parameter == ReglasAnimacion.Agachado && c.mode == AnimatorConditionMode.IfNot)), s);
            }
            Assert.IsTrue(Estado(0, "Pararse").transitions.Any(t => t.destinationState.name == "Idle" && t.hasExitTime));
        }

        [Test]
        public void Rodar_EsLocoRodar_DesdeCualquierEstadoMenosLasMuertes_YTerminaAgachadoOPararse()
        {
            Assert.AreEqual("Loco_Rodar", Estado(0, "Rodar").motion.name);
            AnimatorStateTransition entrada = Base().layers[0].stateMachine.anyStateTransitions.First(t => t.destinationState.name == "Rodar");
            Assert.IsTrue(entrada.conditions.Any(c => c.parameter == ReglasAnimacion.Rodar));
            Assert.IsTrue(entrada.conditions.Any(c => c.parameter == ReglasVisual.Caer && c.mode == AnimatorConditionMode.IfNot), "Caido no rueda");
            string[] destinos = Estado(0, "Rodar").transitions.Select(t => t.destinationState.name).ToArray();
            CollectionAssert.AreEquivalent(new[] { "Pararse", "AgachadoCaminar", "AgachadoQuieto" }, destinos);
            Assert.AreEqual(ReglasAnimacion.DuracionRodada, Estado(0, "Rodar").motion.averageDuration, 0.05f, "Loco_Rodar dura 1 s");
            Assert.AreEqual(1f, Estado(0, "Rodar").speed, 0.001f);
        }

        [Test]
        public void Apuntar_EsUnArbolDosDeDirecciones_SobreApuntarXyApuntarZ_ConLosClipsLateralesYAtras()
        {
            var arbol = (BlendTree)Estado(1, "PistolaApuntar").motion;
            Assert.AreEqual(BlendTreeType.SimpleDirectional2D, arbol.blendType);
            Assert.AreEqual(ReglasAnimacion.ApuntarX, arbol.blendParameter);
            Assert.AreEqual(ReglasAnimacion.ApuntarZ, arbol.blendParameterY);
            string Clip(Vector2 p) => arbol.children.First(h => h.position == p).motion.name;
            Assert.AreEqual("Pistola_Apuntar", Clip(Vector2.zero));
            Assert.AreEqual("Pistola_Caminar", Clip(new Vector2(0f, 1f)));
            Assert.AreEqual("Pistola_ApuntarAtras", Clip(new Vector2(0f, -1f)));
            Assert.AreEqual("Pistola_ApuntarLateral_Derecha", Clip(new Vector2(1f, 0f)));
            Assert.AreEqual("Pistola_ApuntarLateral_Izquierda", Clip(new Vector2(-1f, 0f)));
            var parametros = Base().parameters.ToDictionary(p => p.name);
            Assert.AreEqual(0f, parametros[ReglasAnimacion.ApuntarZ].defaultFloat, "Quien no calcula la direccion apunta quieto");
        }

        [Test]
        public void Pistola_SeSacaAlApuntar_YSeGuardaAlDejarDeApuntar_ConLosClipsDeTony()
        {
            Assert.AreEqual("Pistola_Sacar", Estado(1, "PistolaSacar").motion.name);
            Assert.AreEqual("Pistola_Guardar", Estado(1, "PistolaGuardar").motion.name);
            Assert.IsTrue(Estado(1, "Vacio").transitions.Any(t => t.destinationState.name == "PistolaSacar"));
            Assert.IsFalse(Estado(1, "Vacio").transitions.Any(t => t.destinationState.name == "PistolaIdle"), "La pistola no sale sola a la mano");
            foreach (string s in new[] { "PistolaIdle", "PistolaApuntar", "PistolaAgachado", "PistolaSacar" })
            {
                Assert.IsTrue(Estado(1, s).transitions.Any(t => t.destinationState.name == "PistolaGuardar"), s + " pasa por Pistola_Guardar");
            }
            Assert.IsTrue(Estado(1, "PistolaGuardar").transitions.Any(t => t.destinationState.name == "Vacio" && t.hasExitTime));
        }

        [Test]
        public void ApuntarAgachado_UsaLosClipsDePistolaAgachada()
        {
            CollectionAssert.AreEquivalent(new[] { "Pistola_AgachadoIdle", "Pistola_AgachadoCaminar" }, Clips(Estado(1, "PistolaAgachado").motion));
        }

        [Test]
        public void ElControlador_YaNoUsaClipsProvisionales()
        {
            var usados = Base().layers.SelectMany(l => l.stateMachine.states).SelectMany(s => Clips(s.state.motion)).ToArray();
            CollectionAssert.DoesNotContain(usados, "Rifle_AgachadoCaminar");
            CollectionAssert.DoesNotContain(usados, "Pistola_ApuntarCaminar_Derecha");
            Assert.IsNotNull(Estado(0, "CoberturaMantener").motion, "La cobertura (Q) sigue con Cobertura_Entrar");
        }

        [Test]
        public void CaminarNuevo_SeMideAlaVelocidadDeLaConstante()
        {
            Assert.AreEqual(ReglasAnimacion.VelocidadClipCaminar, MedicionClips.Medir("Loco_Caminar"), ReglasAnimacion.VelocidadClipCaminar * 0.1f);
        }

        [Test]
        public void LosVisualesQueUsanLocoCaminar_TraenLaVelocidadNueva()
        {
            foreach (string n in new[] { "woman", "woman2", "PJ01", "Doctor", "Hacker", "Inventor", "RobotHombre" })
            {
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(n)).GetComponent<VisualPersonaje>();
                float v = new SerializedObject(visual).FindProperty("velocidadClipCaminar").floatValue;
                Assert.AreEqual(ReglasAnimacion.VelocidadClipCaminar, v, 0.005f, "Visual_" + n);
            }
        }

        [Test]
        public void Rodada_DelJugador_Es2Punto2MetrosEn1Segundo()
        {
            var datos = Resources.Load<MechaCyberpunk.Personaje.DatosCubierta>("DatosCubierta");
            Assert.AreEqual(2.2f, datos.DistanciaRodada, 0.001f);
            Assert.AreEqual(1f, datos.DuracionRodada, 0.001f);
            Assert.GreaterOrEqual(datos.EnfriamientoRodada, datos.DuracionRodada);
        }
    }
}

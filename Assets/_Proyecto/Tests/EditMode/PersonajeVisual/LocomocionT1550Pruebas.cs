using System.Linq;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-1550 (decisión de Tony del 20-nov): WASD camina (1.62 m/s), Shift trota (4.53), la velocidad rápida (implante o consumible) corre a 6.25 con PJ01_CorrerRapido; el salto
    /// despega 0.1 s después de pedirlo y tiene su versión corriendo, aterrizaje sin bloquear, rodada y aterrizaje sin input. La primera mitad es lógica pura; la segunda necesita haber
    /// corrido LocomocionT1550.Aplicar (importación de PJ01_CorrerRapido, velocidades, controlador y override del jugador).
    /// </summary>
    public class LocomocionT1550Pruebas
    {
        // ---------- Lógica pura ----------

        [Test]
        public void Velocidades_LasDelJugadorSonLasDeLosClips()
        {
            Assert.AreEqual(1.62f, ReglasAnimacion.VelocidadPJ01Caminar, 1e-4f);
            Assert.AreEqual(4.53f, ReglasAnimacion.VelocidadPJ01Trote, 1e-4f);
            Assert.AreEqual(6.25f, ReglasAnimacion.VelocidadPJ01CorrerRapido, 1e-4f);
            Assert.AreEqual(ReglasAnimacion.VelocidadPJ01CorrerRapido, ReglasAnimacion.VelocidadRapida, 1e-4f);
            Assert.AreEqual(ReglasAnimacion.VelocidadRapida, ReglaVelocidadRapida.Velocidad, 1e-4f);
            Assert.AreEqual(1.57f, ReglasAnimacion.VelocidadPJ01CaminarApuntandoDerecha, 1e-4f, "El caminar de lado apuntando a la derecha avanza 1.57 m/s (antes 2.0)");
        }

        [Test]
        public void DatosPersonaje_PorDefecto_CaminaA1y6YCorreA4y5()
        {
            var datos = ScriptableObject.CreateInstance<DatosPersonaje>();
            try
            {
                Assert.AreEqual(1.6f, datos.VelocidadCaminar, 0.05f, "WASD camina a ~1.6 m/s");
                Assert.AreEqual(4.5f, datos.VelocidadCorrer, 0.05f, "Shift trota a ~4.5 m/s");
                Assert.AreEqual(ReglasAnimacion.VelocidadPJ01Caminar, datos.VelocidadCaminar, 1e-4f, "Igual a la del clip: los pies no patinan");
                Assert.AreEqual(ReglasAnimacion.VelocidadPJ01Trote, datos.VelocidadCorrer, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(datos);
            }
        }

        [Test]
        public void Umbrales_ElTroteDeShiftNoEntraAlEstadoCorrer_PeroLaVelocidadRapidaSi()
        {
            float entra = ReglasAnimacion.UmbralCorrer;
            float sale = ReglasAnimacion.UmbralCorrer * ReglasAnimacion.HisteresisCorrer;
            Assert.Less(ReglasAnimacion.VelocidadPJ01Trote, sale, "A 4.53 m/s el trote sigue en Caminar y no parpadea con Correr");
            Assert.Greater(ReglasAnimacion.VelocidadRapida, entra, "A 6.25 m/s entra a Correr (PJ01_CorrerRapido)");
            Assert.Greater(ReglasAnimacion.VelocidadSaltoCorriendo, ReglasAnimacion.VelocidadPJ01Caminar, "Caminando se salta parado");
            Assert.Less(ReglasAnimacion.VelocidadSaltoCorriendo, ReglasAnimacion.VelocidadPJ01Trote, "Trotando se salta corriendo");
        }

        [TestCase(1.0f, 1.0f, 1.0f, false)]
        [TestCase(1.2f, 1.0f, 1.0f, true)]
        [TestCase(1.0f, 1.5f, 1.0f, true)]
        [TestCase(1.0f, 1.0f, 1.35f, true)]
        [TestCase(1.0f, 1.0f, 1.0005f, false)]
        public void VelocidadRapida_Activa_SoloConAlgunBono(float implante, float habilidad, float extra, bool esperada)
        {
            Assert.AreEqual(esperada, ReglaVelocidadRapida.Activa(implante, habilidad, extra));
        }

        [Test]
        public void VelocidadCorrer_SinBono_EsLaBase()
        {
            Assert.AreEqual(4.53f, ReglaVelocidadRapida.VelocidadCorrer(4.53f, 1f, 1f, 1f), 1e-4f);
        }

        [TestCase(1.2f, 1f, 1f)] // piernas I
        [TestCase(1.35f, 1f, 1f)] // piernas II
        [TestCase(1f, 1f, 1.35f)] // café
        [TestCase(1.2f, 1f, 1.35f)] // piernas I y café
        public void VelocidadCorrer_ConBono_NuncaBajaDe6y25(float implante, float habilidad, float extra)
        {
            float objetivo = ReglaVelocidadRapida.VelocidadCorrer(4.53f, implante, habilidad, extra);
            // El consumible multiplica después, sobre todo el movimiento: lo que importa es la velocidad final.
            Assert.GreaterOrEqual(objetivo * extra, ReglasAnimacion.VelocidadRapida - 1e-3f);
            Assert.Greater(objetivo * extra, ReglasAnimacion.UmbralCorrer, "Con velocidad rápida el Animator pasa al estado Correr");
        }

        [Test]
        public void VelocidadCorrer_ConBonoGrande_ConservaLaVelocidadCalculada()
        {
            // Habilidad de piernas (x1.5) con implante (x1.2): 4.53 x 1.2 x 1.5 = 8.15 m/s, por arriba del piso de 6.25.
            Assert.AreEqual(4.53f * 1.2f * 1.5f, ReglaVelocidadRapida.VelocidadCorrer(4.53f, 1.2f, 1.5f, 1f), 1e-3f);
        }

        [TestCase(-3.8f, 0.45f, 4.5f, ReglasAnimacion.AterrizajeVisual.Normal)] // un salto corriendo normal (0.45 m): nada especial
        [TestCase(-5.9f, 1.1f, 4.5f, ReglasAnimacion.AterrizajeVisual.Normal)] // casi, pero abajo de 6 m/s
        [TestCase(-6f, 1.2f, 4.5f, ReglasAnimacion.AterrizajeVisual.Rodada)]
        [TestCase(-7.8f, 1.9f, 4.5f, ReglasAnimacion.AterrizajeVisual.Rodada)]
        [TestCase(-8.9f, 2.4f, 3f, ReglasAnimacion.AterrizajeVisual.Rodada)]
        [TestCase(-10f, 3.1f, 4.5f, ReglasAnimacion.AterrizajeVisual.Rodada)] // corriendo, aunque pase de 9 m/s: rueda
        [TestCase(-7.8f, 1.9f, 1.62f, ReglasAnimacion.AterrizajeVisual.Normal)] // caminando: no rueda, aterriza normal
        [TestCase(-7.8f, 1.9f, 0f, ReglasAnimacion.AterrizajeVisual.Normal)]
        [TestCase(-10f, 3.1f, 1.62f, ReglasAnimacion.AterrizajeVisual.Fuerte)] // la cornisa de ~2.6 m sin correr: tres puntos
        [TestCase(-9.1f, 2.6f, 0f, ReglasAnimacion.AterrizajeVisual.Fuerte)]
        [TestCase(-14f, 0.3f, 4.5f, ReglasAnimacion.AterrizajeVisual.Normal)] // sin caída real no cuenta
        [TestCase(-6.5f, 0.4f, 4.5f, ReglasAnimacion.AterrizajeVisual.Normal)]
        public void TipoAterrizaje_SegunCaidaYVelocidadHorizontal(float velVertical, float metros, float velHorizontal, ReglasAnimacion.AterrizajeVisual esperado)
        {
            Assert.AreEqual(esperado, ReglasAnimacion.TipoAterrizaje(velVertical, metros, velHorizontal));
        }

        [Test]
        public void Parametros_PreparandoSaltoYAterrizajeRodada_EstanEnLaListaDelControlador()
        {
            CollectionAssert.Contains(ReglasAnimacion.ParametrosBool, ReglasAnimacion.PreparandoSalto);
            CollectionAssert.Contains(ReglasAnimacion.ParametrosTrigger, ReglasAnimacion.AterrizajeRodada);
        }

        [Test]
        public void Bloqueo_LaRodadaYElAterrizajeCorriendoNoBloquean_ElFuerteSi()
        {
            foreach (string libre in new[] { "SaltoCorrerInicio", "SaltoCorrerAire", "SaltoCorrerAterrizaje", "SaltoCorrerRodada", "SaltoInicio", "SaltoAire", "SaltoAterrizaje" })
            {
                Assert.IsFalse(ReglaBloqueoAnimacion.EstadoBloquea(libre), libre);
            }
            Assert.IsTrue(ReglaBloqueoAnimacion.EstadoBloquea("AterrizajeFuerte"));
        }

        [Test]
        public void SaltoEstados_ElJugadorLosDejaA1x_ConElParametroDeRapidez()
        {
            // Los clips del 20-nov vienen a velocidad real: el estado trae 2.5x y 2x de los clips viejos y el jugador lo compensa con su parámetro.
            Assert.AreEqual(1f, ReglasAnimacion.AjusteSaltoJugador("SaltoInicio") * ReglasAnimacion.VelocidadEstado("SaltoInicio"), 1e-4f);
            Assert.AreEqual(1f, ReglasAnimacion.AjusteSaltoJugador("SaltoAterrizaje") * ReglasAnimacion.VelocidadEstado("SaltoAterrizaje"), 1e-4f);
            Assert.AreEqual(0.4f, ReglasAnimacion.AjusteSaltoJugador("SaltoInicio"), 1e-4f);
            Assert.AreEqual(0.5f, ReglasAnimacion.AjusteSaltoJugador("SaltoAterrizaje"), 1e-4f);
        }

        [Test]
        public void SaltoRetardo_SonTresCuadrosA30fps_YDespegaEnElCuadroCuatroDelClip()
        {
            const float cuadrosPorSegundo = 30f;
            const int cuadrosDeAgachada = 3;
            Assert.AreEqual(cuadrosDeAgachada / cuadrosPorSegundo, MovimientoAPie.SegundosRetardoSalto, 1e-5f);
        }

        // ---------- Lo que Aplicar deja en los assets ----------

        private static AnimatorController Base() => AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);

        private static AnimatorState EstadoBase(string nombre) => Base().layers[0].stateMachine.states.Select(h => h.state).First(s => s.name == nombre);

        [Test]
        public void Aplicar_CorrerRapidoQuedaImportadoComoClipHumanoid_ConBucleYDosPisadas()
        {
            var importador = AssetImporter.GetAtPath(LocomocionT1550.RutaClipRapido) as ModelImporter;
            Assert.IsNotNull(importador, "Falta " + LocomocionT1550.RutaClipRapido + ": correr LocomocionT1550.Aplicar");
            Assert.AreEqual(ModelImporterAnimationType.Human, importador.animationType);
            ModelImporterClipAnimation[] clips = importador.clipAnimations;
            Assert.AreEqual(1, clips.Length);
            Assert.AreEqual(LocomocionT1550.NombreClipRapido, clips[0].name);
            Assert.AreEqual(LocomocionT1550.TomaClipRapido, clips[0].takeName);
            Assert.AreEqual(1f, clips[0].firstFrame);
            Assert.AreEqual(34f, clips[0].lastFrame);
            Assert.IsTrue(clips[0].loopTime);
            Assert.AreEqual(2, clips[0].events.Length);
            Assert.AreEqual(ReglaPasosAnimados.FuncionPaso, clips[0].events[0].functionName);
            Assert.AreEqual(0.424f, clips[0].events[0].time, 1e-4f);
            Assert.AreEqual(ReglaPasosAnimados.PieIzquierdo, clips[0].events[0].intParameter);
            Assert.AreEqual(0.909f, clips[0].events[1].time, 1e-4f);
            Assert.AreEqual(ReglaPasosAnimados.PieDerecho, clips[0].events[1].intParameter);
        }

        [Test]
        public void Aplicar_ElClipDeCorrerRapido_TraeLosEventosDePisadaYBucle()
        {
            AnimationClip clip = ConstructorControladorBase.Clip(LocomocionT1550.NombreClipRapido);
            Assert.IsNotNull(clip);
            Assert.IsTrue(clip.isLooping);
            Assert.IsTrue(ReglaPasosAnimados.TienePasos(clip), "Los pasos suenan en los eventos AlPasoPJ01 del clip");
            Assert.AreEqual(33f / 30f, clip.length, 0.02f, "34 cuadros a 30 fps");
        }

        [Test]
        public void Aplicar_LosClipsDeAireDelSalto_NoHacenCiclo()
        {
            foreach (string nombre in LocomocionT1550.ClipsAireSinBucle)
            {
                AnimationClip clip = ConstructorControladorBase.Clip(nombre);
                Assert.IsNotNull(clip, nombre);
                Assert.IsFalse(clip.isLooping, nombre + ": se queda en la pose de bajada");
            }
        }

        [Test]
        public void Aplicar_DatosPersonajeBase_CaminaA1y62YCorreA4y53()
        {
            var datos = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(AjustarSaltoT961.RutaDatosPersonaje);
            Assert.IsNotNull(datos);
            Assert.AreEqual(1.62f, datos.VelocidadCaminar, 1e-3f);
            Assert.AreEqual(4.53f, datos.VelocidadCorrer, 1e-3f);
            Assert.AreEqual(AjustarSaltoT961.AlturaSalto, datos.AlturaSalto, 1e-4f, "El salto sigue de 0.45 m");
        }

        [Test]
        public void Aplicar_ElOverrideDelJugador_UsaCorrerRapidoYNoElSprint()
        {
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride("Jugador"));
            var pares = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            Assert.AreEqual("PJ01_CorrerRapido", pares.First(p => p.Key != null && p.Key.name == "Loco_Correr").Value.name);
            Assert.AreEqual("PJ01_Caminar", pares.First(p => p.Key != null && p.Key.name == "Loco_Caminar").Value.name);
            Assert.AreEqual("PJ01_Trote", pares.First(p => p.Key != null && p.Key.name == ConstructorControladorBase.RolTrote).Value.name);
            Assert.IsFalse(pares.Any(p => p.Value != null && p.Value.name == "PJ01_Sprint"), "PJ01_Sprint deja de usarse (el archivo se queda)");
        }

        [Test]
        public void Aplicar_LosOverridesDeLosDemasPapeles_NoUsanCorrerRapido()
        {
            foreach (string papel in ConstructorControladorBase.Papeles.Where(p => p != "Jugador"))
            {
                var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride(papel));
                var pares = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                Assert.IsFalse(pares.Any(p => p.Value != null && p.Value.name == "PJ01_CorrerRapido"), papel + " no cambia su carrera");
            }
        }

        [Test]
        public void Aplicar_ElControlador_TraeLosParametrosDelSalto()
        {
            AnimatorControllerParameter prepara = Base().parameters.First(p => p.name == ReglasAnimacion.PreparandoSalto);
            Assert.AreEqual(AnimatorControllerParameterType.Bool, prepara.type);
            AnimatorControllerParameter rodada = Base().parameters.First(p => p.name == ReglasAnimacion.AterrizajeRodada);
            Assert.AreEqual(AnimatorControllerParameterType.Trigger, rodada.type);
            foreach (string p in new[] { ReglasAnimacion.RapidezSaltoInicio, ReglasAnimacion.RapidezSaltoAterrizaje })
            {
                Assert.AreEqual(1f, Base().parameters.First(x => x.name == p).defaultFloat, 1e-4f, p + ": valor por defecto 1 (como siempre)");
            }
        }

        [Test]
        public void Aplicar_SaltoInicio_NoAterrizaMientrasSigueAgachado()
        {
            AnimatorState inicio = EstadoBase("SaltoInicio");
            AnimatorStateTransition aterriza = inicio.transitions.First(t => t.destinationState.name == "SaltoAterrizaje" && !t.hasExitTime);
            Assert.IsTrue(aterriza.conditions.Any(c => c.parameter == ReglasAnimacion.PreparandoSalto && c.mode == AnimatorConditionMode.IfNot));
            Assert.IsTrue(aterriza.conditions.Any(c => c.parameter == ReglasVisual.EnSuelo && c.mode == AnimatorConditionMode.If));
        }

        [Test]
        public void Aplicar_DeCualquierEstadoDePiso_AlPedirElSaltoSeAgachaEnElPiso()
        {
            foreach (string nombre in new[] { "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq" })
            {
                AnimatorState s = EstadoBase(nombre);
                Assert.IsTrue(s.transitions.Any(t => t.destinationState.name == "SaltoInicio" && t.conditions.Any(c => c.parameter == ReglasAnimacion.PreparandoSalto && c.mode == AnimatorConditionMode.If)), nombre);
            }
        }

        [Test]
        public void Aplicar_SaltoCorriendo_EntraDesdeCaminarYCorrer_CorriendoHaciaAdelante()
        {
            foreach (string nombre in new[] { "Caminar", "Correr" })
            {
                AnimatorState s = EstadoBase(nombre);
                AnimatorStateTransition[] entradas = s.transitions.Where(t => t.destinationState.name == "SaltoCorrerInicio").ToArray();
                Assert.AreEqual(2, entradas.Length, nombre + ": una al pedir el salto y otra ya en el aire");
                foreach (AnimatorStateTransition t in entradas)
                {
                    AnimatorCondition velocidad = t.conditions.First(c => c.parameter == ReglasVisual.Velocidad);
                    Assert.AreEqual(ReglasAnimacion.VelocidadSaltoCorriendo, velocidad.threshold, 1e-4f, nombre + ": desde ~3 m/s");
                    Assert.AreEqual(AnimatorConditionMode.Greater, velocidad.mode);
                    Assert.IsTrue(t.conditions.Any(c => c.parameter == ReglasAnimacion.VelZ && c.mode == AnimatorConditionMode.Greater), nombre + ": solo hacia adelante");
                }
                Assert.IsTrue(s.transitions.Take(2).All(t => t.destinationState.name == "SaltoCorrerInicio"), nombre + ": van antes que SaltoInicio");
            }
        }

        [Test]
        public void Aplicar_SaltoCorriendo_AterrizaCorriendoSiHayAvance_YParadoSiNo()
        {
            foreach (string nombre in new[] { "SaltoCorrerInicio", "SaltoCorrerAire" })
            {
                AnimatorState s = EstadoBase(nombre);
                int corriendo = s.transitions.ToList().FindIndex(t => t.destinationState.name == "SaltoCorrerAterrizaje" && t.conditions.Any(c => c.parameter == ReglasVisual.Velocidad));
                int parado = s.transitions.ToList().FindIndex(t => t.destinationState.name == "SaltoAterrizaje");
                Assert.GreaterOrEqual(corriendo, 0, nombre + " aterriza corriendo con avance");
                Assert.GreaterOrEqual(parado, 0, nombre + " aterriza con PJ01_Salto_Aterrizaje sin input");
                Assert.Less(corriendo, parado, nombre + ": el aterrizaje corriendo se prueba antes");
            }
        }

        [Test]
        public void Aplicar_LaRodada_EntraPorSuTrigger_AntesQueElAterrizajeFuerte()
        {
            AnimatorStateTransition[] cualquiera = Base().layers[0].stateMachine.anyStateTransitions;
            int rodada = System.Array.FindIndex(cualquiera, t => t.destinationState != null && t.destinationState.name == "SaltoCorrerRodada");
            int fuerte = System.Array.FindIndex(cualquiera, t => t.destinationState != null && t.destinationState.name == "AterrizajeFuerte");
            Assert.GreaterOrEqual(rodada, 0);
            Assert.Less(rodada, fuerte);
            Assert.AreEqual(1, cualquiera.Count(t => t.destinationState != null && t.destinationState.name == "SaltoCorrerRodada"), "Una sola entrada");
            Assert.IsTrue(cualquiera[rodada].conditions.Any(c => c.parameter == ReglasAnimacion.AterrizajeRodada));
            Assert.IsTrue(cualquiera[fuerte].conditions.Any(c => c.parameter == ReglasAnimacion.AterrizajeFuerte), "El aterrizaje fuerte conserva su trigger");
        }

        [Test]
        public void Aplicar_SinNingunEstadoDelSaltoQueSeRepita_SaltoAireSeQuedaEnSuPose()
        {
            AnimatorState aire = EstadoBase("SaltoAire");
            Assert.IsFalse(aire.transitions.Any(t => t.destinationState == aire), "SaltoAire no se repite");
            AnimatorState aireCorrer = EstadoBase("SaltoCorrerAire");
            Assert.IsFalse(aireCorrer.transitions.Any(t => t.destinationState == aireCorrer), "SaltoCorrerAire no se repite");
        }

        [Test]
        public void AgregarEstados_AlRepetirse_NoDuplicaTransiciones()
        {
            AnimatorController c = Base();
            AnimatorStateMachine m = c.layers[0].stateMachine;
            int Total() => m.states.Sum(h => h.state.transitions.Length) + m.anyStateTransitions.Length;
            int antes = Total();
            int parametrosAntes = c.parameters.Length;
            AnimacionesT1540.AgregarEstados(c);
            Assert.AreEqual(antes, Total(), "Repetir AgregarEstados no cambia el número de transiciones");
            Assert.AreEqual(parametrosAntes, c.parameters.Length);
        }

        [Test]
        public void Aplicar_Diagnostico_NoTieneProblemas()
        {
            Assert.AreEqual(0, LocomocionT1550.ContarProblemas(), "LocomocionT1550.Diagnostico lista los problemas en el log");
        }
    }
}

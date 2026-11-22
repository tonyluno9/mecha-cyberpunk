using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
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
    /// T-1540: las animaciones de Blender sin usar ya están en los controladores. La primera mitad es lógica pura (reglas de explosión, motivo de muerte, tabla de impactos, bloqueo y raíz);
    /// la segunda necesita haber corrido AnimacionesT1540.Aplicar (estados, clips y overrides del ControladorBase).
    /// </summary>
    public class AnimacionesT1540Pruebas
    {
        private static readonly Vector3 Frente = Vector3.forward;

        // ---------- Lógica pura ----------

        [TestCase(40f, 100f, false, 0, ReaccionExplosion.Derribo)]
        [TestCase(30f, 100f, false, 0, ReaccionExplosion.Derribo)]
        [TestCase(15f, 100f, false, 0, ReaccionExplosion.Tambaleo)]
        [TestCase(5f, 100f, false, 0, ReaccionExplosion.Ninguna)]
        [TestCase(5f, 100f, true, 0, ReaccionExplosion.Muerte)]
        [TestCase(40f, 100f, false, 1, ReaccionExplosion.Tambaleo)] // le explota por la espalda: el derribo lo lanzaría hacia atrás (hacia la explosión)
        [TestCase(5f, 100f, true, 1, ReaccionExplosion.Ninguna)]
        public void ReaccionAExplosion_SegunDanioYDeDondeViene(float cantidad, float vidaMaxima, bool murio, int deEspalda, ReaccionExplosion esperada)
        {
            // La dirección va de la explosión al personaje: contra su frente = de frente; con su frente = por la espalda.
            Vector3 direccion = deEspalda == 1 ? Frente : -Frente;
            Assert.AreEqual(esperada, ReglasAnimacion.ReaccionAExplosion(cantidad, vidaMaxima, murio, direccion, Frente));
        }

        [Test]
        public void ReaccionAExplosion_SinVidaMaxima_CuentaComoGolpeCompleto()
        {
            Assert.AreEqual(ReaccionExplosion.Derribo, ReglasAnimacion.ReaccionAExplosion(1f, 0f, false, -Frente, Frente));
        }

        [Test]
        public void ElegirMuerte_ExplosionDeFrente_EsMuerteExplosion_PeroPorLaEspaldaNo()
        {
            Assert.AreEqual(MuerteVisual.Explosion, ReglasAnimacion.ElegirMuerte(-Frente, Frente, 0.1f, MotivoDanioVisual.Explosion));
            Assert.AreEqual(MuerteVisual.Explosion, ReglasAnimacion.ElegirMuerte(Vector3.right, Frente, 0.1f, MotivoDanioVisual.Explosion), "de un costado también sale volando");
            MuerteVisual porLaEspalda = ReglasAnimacion.ElegirMuerte(Frente, Frente, 0.1f, MotivoDanioVisual.Explosion);
            Assert.AreNotEqual(MuerteVisual.Explosion, porLaEspalda, "el clip lo manda hacia atrás: por la espalda cae como siempre");
            Assert.AreEqual(ReglasAnimacion.ElegirMuerte(Frente, Frente, 0.1f), porLaEspalda);
        }

        [Test]
        public void ElegirMuerte_AtropelloYCaida_TienenClipPropio_YLoNormalNoCambia()
        {
            Assert.AreEqual(MuerteVisual.Atropello, ReglasAnimacion.ElegirMuerte(Frente, Frente, 0.9f, MotivoDanioVisual.Atropello));
            Assert.AreEqual(MuerteVisual.CaidaAltura, ReglasAnimacion.ElegirMuerte(Vector3.down, Frente, 0.9f, MotivoDanioVisual.CaidaAltura));
            foreach (float azar in new[] { 0f, 0.4f, 0.99f })
            {
                Assert.AreEqual(ReglasAnimacion.ElegirMuerte(-Frente, Frente, azar), ReglasAnimacion.ElegirMuerte(-Frente, Frente, azar, MotivoDanioVisual.Normal));
            }
        }

        [Test]
        public void MuerteVisual_ValoresNuevos_SonLosQueEsperaElControlador()
        {
            Assert.AreEqual(3, (int)MuerteVisual.Explosion);
            Assert.AreEqual(4, (int)MuerteVisual.Atropello);
            Assert.AreEqual(5, (int)MuerteVisual.CaidaAltura);
            Assert.IsTrue(ReglasAnimacion.LevantarseBocaArriba(MuerteVisual.Explosion));
            Assert.IsTrue(ReglasAnimacion.LevantarseBocaArriba(MuerteVisual.Atropello));
            Assert.IsFalse(ReglasAnimacion.LevantarseBocaArriba(MuerteVisual.CaidaAltura), "la caída de altura es boca abajo (viene de Muerte_Frente)");
        }

        [Test]
        public void MotivoDe_ExplosionEsExplosion_GolpeVerticalHaciaAbajoEsCaida_LoDemasNormal()
        {
            Assert.AreEqual(MotivoDanioVisual.Explosion, ReglasAnimacion.MotivoDe(true, false, Frente));
            Assert.AreEqual(MotivoDanioVisual.CaidaAltura, ReglasAnimacion.MotivoDe(false, true, Vector3.down));
            Assert.AreEqual(MotivoDanioVisual.Normal, ReglasAnimacion.MotivoDe(false, true, Frente), "un golpe horizontal no es caída");
            Assert.AreEqual(MotivoDanioVisual.Normal, ReglasAnimacion.MotivoDe(false, false, Vector3.down), "un disparo hacia abajo no es caída");
            Assert.AreEqual(MotivoDanioVisual.Normal, ReglasAnimacion.MotivoDe(false, true, Vector3.zero));
        }

        [Test]
        public void EstadosT1540_EstanEnLaListaDeEstadosBase_YLosGestosNoSeRepiten()
        {
            foreach (var e in ReglasAnimacion.EstadosT1540)
            {
                CollectionAssert.Contains(ReglasAnimacion.EstadosBase, e.Estado);
            }
            foreach (var e in ReglasAnimacion.EstadosSaltoCorrerT1540)
            {
                CollectionAssert.Contains(ReglasAnimacion.EstadosBase, e.Estado);
            }
            Assert.AreEqual(ReglasAnimacion.EstadosBase.Length, ReglasAnimacion.EstadosBase.Distinct().Count(), "sin estados repetidos");
            int[] gestos = ReglasAnimacion.EstadosT1540.Where(e => e.Gesto >= 0).Select(e => e.Gesto).ToArray();
            CollectionAssert.AreEquivalent(new[] { (int)GestoVisual.Empujon, (int)GestoVisual.ExplosionCercana, (int)GestoVisual.DerriboExplosion }, gestos);
            CollectionAssert.AreEqual(gestos.Distinct().ToArray(), gestos);
            foreach (GestoVisual g in System.Enum.GetValues(typeof(GestoVisual)))
            {
                Assert.AreEqual(1, System.Enum.GetValues(typeof(GestoVisual)).Cast<GestoVisual>().Count(x => (int)x == (int)g), "valor repetido en GestoVisual: " + g);
            }
        }

        [Test]
        public void ClipsDeEstadosT1540_SonArchivosPJ01_DeLaCarpetaDeClips()
        {
            foreach (var e in ReglasAnimacion.EstadosT1540)
            {
                Assert.IsTrue(System.IO.File.Exists(ImportadorClipsPJ01.Carpeta + e.Clip + ".fbx"), "falta el FBX " + e.Clip);
            }
            foreach (var e in ReglasAnimacion.EstadosSaltoCorrerT1540)
            {
                Assert.IsTrue(System.IO.File.Exists(ImportadorClipsPJ01.Carpeta + e.Clip + ".fbx"), "falta el FBX " + e.Clip);
            }
            foreach (string clip in AnimacionesT1540.ClipsNuevos)
            {
                Assert.IsTrue(ReglasAnimacion.EstadosT1540.Any(e => e.Clip == clip), clip + " no tiene estado");
            }
        }

        [Test]
        public void Bloqueo_DerriboEmpujonYTambaleo_NoDejanCaminar()
        {
            foreach (string estado in new[] { "DerriboExplosion", "DanoEmpujon", "DanoExplosionCercana" })
            {
                Assert.IsTrue(ReglaBloqueoAnimacion.EstadoBloquea(estado), estado);
                Assert.IsTrue(ReglaBloqueoAnimacion.HashBloquea(Animator.StringToHash(estado)), estado);
            }
            Assert.IsFalse(ReglaBloqueoAnimacion.EstadoBloquea("SaltoCorrerRodada"), "la rodada corriendo se mueve con el jugador, no lo frena");
        }

        [Test]
        public void RaizCaida_LasMuertesYElDerriboNuevosMuevenLaCapsula()
        {
            foreach (string estado in new[] { "MuerteExplosion", "MuerteAtropello", "MuerteCaidaAltura", "DerriboExplosion", "DanoEmpujon", "DanoExplosionCercana" })
            {
                Assert.IsTrue(RaizCaidaJugador.MueveLaRaiz(Animator.StringToHash(estado)), estado);
            }
            Assert.IsFalse(RaizCaidaJugador.MueveLaRaiz(Animator.StringToHash("SaltoCorrerInicio")));
        }

        [Test]
        public void AjusteSaltoJugador_CompensaLaRapidezDelEstado_HastaUno()
        {
            foreach (string estado in new[] { "SaltoInicio", "SaltoAterrizaje" })
            {
                Assert.AreEqual(1f, ReglasAnimacion.AjusteSaltoJugador(estado) * ReglasAnimacion.VelocidadEstado(estado), 1e-4f, estado);
            }
        }

        [Test]
        public void TablaImpactos_CuadrosDentroDelClip_MaximoDosPorClip_YAliasDeLasMuertes()
        {
            foreach (TablaImpactosPJ01.Impacto i in TablaImpactosPJ01.Todos)
            {
                Assert.That(i.Cuadro, Is.InRange(1, i.Cuadros), i.Clip);
                Assert.That(i.Fase, Is.InRange(0f, 1f), i.Clip);
                Assert.IsNotEmpty(i.Sonido, i.Clip);
            }
            foreach (IGrouping<string, TablaImpactosPJ01.Impacto> g in TablaImpactosPJ01.Todos.GroupBy(i => i.Clip))
            {
                Assert.LessOrEqual(g.Count(), TablaImpactosPJ01.MaximoPorClip, g.Key);
            }
            var salida = new TablaImpactosPJ01.Impacto[TablaImpactosPJ01.MaximoPorClip];
            Assert.AreEqual(2, TablaImpactosPJ01.Buscar("NPC_Muerte_Frente", salida));
            Assert.AreEqual(2, TablaImpactosPJ01.Buscar("Guardia_Muerte", salida));
            Assert.AreEqual(1, TablaImpactosPJ01.Buscar("PJ01_Muerte_Explosion", salida));
            Assert.AreEqual(17f / 55f, salida[0].Fase, 1e-5f, "cuadro 18 de 56 (BITACORA): toca el piso");
            Assert.AreEqual(0, TablaImpactosPJ01.Buscar("PJ01_Caminar", salida));
            Assert.AreEqual(0, TablaImpactosPJ01.Buscar(null, salida));
            Assert.IsTrue(TablaImpactosPJ01.Tiene("E16_Golpe_Escudo"));
        }

        [Test]
        public void TablaImpactos_TodoClipDeLaTablaExisteEnLaCarpetaDeClips()
        {
            foreach (string clip in TablaImpactosPJ01.Todos.Select(i => i.Clip).Distinct())
            {
                // T-1553 movió los clips E16_*/E18_* a la carpeta de su personaje: se buscan en las tres carpetas.
                bool existe = ClipsRobotsNovaT1553.CarpetasDeClips().Any(c => System.IO.File.Exists(c + "/" + clip + ".fbx"));
                Assert.IsTrue(existe, "la tabla de impactos nombra un clip que no existe: " + clip);
            }
        }

        [Test]
        public void TablaImpactos_ElDetectorLosDisparaUnaVezAlCruzarElCuadro()
        {
            TablaImpactosPJ01.Impacto muerte = TablaImpactosPJ01.Todos.First(i => i.Clip == "PJ01_Muerte_Explosion");
            var detector = new DetectorPisadasPorTiempo();
            Assert.AreEqual(0, detector.Avanzar(0.10f, muerte.Fase, muerte.Fase), "la primera lectura solo fija el punto de partida");
            Assert.AreEqual(0, detector.Avanzar(0.20f, muerte.Fase, muerte.Fase));
            Assert.AreNotEqual(0, detector.Avanzar(0.40f, muerte.Fase, muerte.Fase), "cruza el cuadro 18");
            Assert.AreEqual(0, detector.Avanzar(0.60f, muerte.Fase, muerte.Fase), "ya sonó");
        }

        [Test]
        public void TablaPorPapel_Jugador_ConservaPunosGuardia_YLosDemasUsanLaBurlaOriginalYElDanoPJ01()
        {
            // decisión de Tony 21-nov (T-1560): la burla de todos los papeles, jugador incluido, es PJ01_Burla.
            Assert.AreEqual("PJ01_Burla", Fila("Jugador", "Skull_Burla"));
            Assert.AreEqual("PJ01_Dano_Frente", Fila("Jugador", "Pistola_Dano_Frente"));
            foreach (string papel in new[] { "Aliados", "SKULLS", "SNAKES", "Guardias", "Robots", "Profes", "Jefe", "NPC" })
            {
                Assert.AreEqual("PJ01_Burla", Fila(papel, "Skull_Burla"), papel); // decisión de Tony 21-nov
            }
            foreach (string papel in new[] { "Aliados", "SKULLS", "SNAKES", "Guardias", "Robots", "Profes", "Jefe" })
            {
                Assert.AreEqual("PJ01_Dano_Frente", Fila(papel, "Pistola_Dano_Frente"), papel);
                Assert.AreEqual("PJ01_Dano_Espalda", Fila(papel, "Pistola_Dano_Espalda"), papel);
                Assert.AreEqual("PJ01_Pistola_Dano_Fuerte", Fila(papel, "Pistola_Dano_Fuerte"), papel + ": no hay PJ01_Dano_Fuerte");
            }
            Assert.AreEqual("NPC_Balazo_Frente", Fila("NPC", "Pistola_Dano_Frente"), "los civiles conservan su balazo");
            Assert.AreEqual("NPC_Balazo_Espalda", Fila("NPC", "Pistola_Dano_Espalda"));
        }

        private static string Fila(string papel, string clipBase)
        {
            return ConstructorControladorBase.ClipsPorPapel[papel].Last(par => par.Base == clipBase).Papel;
        }

        // ---------- Necesitan haber corrido AnimacionesT1540.Aplicar ----------

        private static AnimatorController Base() => AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);

        private static AnimatorState EstadoBase(string nombre)
        {
            AnimatorState s = Base().layers[0].stateMachine.states.Select(h => h.state).FirstOrDefault(x => x.name == nombre);
            Assert.IsNotNull(s, "Falta el estado " + nombre + " (corre AnimacionesT1540.Aplicar)");
            return s;
        }

        private static Dictionary<string, string> Valores(string ruta)
        {
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
            Assert.IsNotNull(o, "Falta " + ruta);
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            return pares.Where(p => p.Key != null).ToDictionary(p => p.Key.name, p => p.Value != null ? p.Value.name : p.Key.name);
        }

        [Test]
        public void Clips_Importados_SonHumanoid_UnClipConElNombreDelArchivo_SinBucle()
        {
            foreach (string nombre in AnimacionesT1540.ClipsNuevos)
            {
                var importador = (ModelImporter)AssetImporter.GetAtPath(ImportadorClipsPJ01.Carpeta + nombre + ".fbx");
                Assert.IsNotNull(importador, nombre);
                Assert.AreEqual(ModelImporterAnimationType.Human, importador.animationType, nombre);
                Assert.AreEqual(1, importador.clipAnimations.Length, nombre);
                Assert.AreEqual(nombre, importador.clipAnimations[0].name);
                Assert.IsFalse(importador.clipAnimations[0].loopTime, nombre + " va una sola vez");
                Assert.IsFalse(importador.clipAnimations[0].hasAdditiveReferencePose, nombre + " es de cuerpo completo, no aditivo");
                Assert.IsFalse(importador.clipAnimations[0].lockRootPositionXZ, nombre + ": el avance va al root motion (RaizCaidaJugador)");
            }
        }

        [Test]
        public void Controlador_TieneLosEstadosNuevos_ConSuClipYSuEntrada()
        {
            AnimatorStateMachine m = Base().layers[0].stateMachine;
            foreach ((string estado, string clip, int gesto) in ReglasAnimacion.EstadosT1540)
            {
                AnimatorState s = EstadoBase(estado);
                Assert.AreEqual(clip, s.motion.name, estado);
                AnimatorStateTransition[] entradas = m.anyStateTransitions.Where(t => t.destinationState == s).ToArray();
                Assert.AreEqual(1, entradas.Length, estado + ": una sola entrada desde cualquier estado");
                AnimatorCondition[] c = entradas[0].conditions;
                if (gesto >= 0)
                {
                    Assert.IsTrue(c.Any(x => x.parameter == ReglasAnimacion.Gesto), estado + " usa el trigger Gesto");
                    Assert.IsTrue(c.Any(x => x.parameter == ReglasAnimacion.GestoTipo && Mathf.Approximately(x.threshold, gesto)), estado + " usa GestoTipo " + gesto);
                    Assert.IsTrue(c.Any(x => x.parameter == ReglasVisual.Caer && x.mode == AnimatorConditionMode.IfNot), estado + " no entra si está caído");
                    Assert.AreEqual(ReglasAnimacion.EtiquetaGesto, s.tag, estado + ": la etiqueta Gesto apaga el torso");
                }
                else
                {
                    Assert.IsTrue(c.Any(x => x.parameter == ReglasVisual.Caer && x.mode == AnimatorConditionMode.If), estado);
                    int tipo = estado == "MuerteExplosion" ? 3 : estado == "MuerteAtropello" ? 4 : 5;
                    Assert.IsTrue(c.Any(x => x.parameter == ReglasAnimacion.MuerteTipo && Mathf.Approximately(x.threshold, tipo)), estado + " usa MuerteTipo " + tipo);
                    string levantarse = tipo == 5 ? "LevantarseBocaAbajo" : "LevantarseBocaArriba";
                    Assert.IsTrue(s.transitions.Any(t => t.destinationState.name == levantarse), estado + " se levanta con " + levantarse);
                }
            }
        }

        [Test]
        public void Derribo_TerminaLevantandoseBocaArriba_ElEmpujonYElTambaleoVuelvenAIdle()
        {
            Assert.IsTrue(EstadoBase("DerriboExplosion").transitions.Any(t => t.destinationState.name == "LevantarseBocaArriba" && t.hasExitTime));
            Assert.IsTrue(EstadoBase("DanoEmpujon").transitions.Any(t => t.destinationState.name == "Idle" && t.hasExitTime));
            Assert.IsTrue(EstadoBase("DanoExplosionCercana").transitions.Any(t => t.destinationState.name == "Idle" && t.hasExitTime));
        }

        [Test]
        public void SaltoCorriendo_ConectadoDesdeCorrer_ConAterrizajeYRodada()
        {
            AnimatorState correr = EstadoBase("Correr");
            // T-1550 (decisión de Tony del 20-nov (T-1550)): el salto corriendo entra de dos formas, al pedir el salto en el piso (PreparandoSalto, 0.1 s antes del despegue) y ya en el aire; las dos van antes que SaltoInicio.
            Assert.AreEqual("SaltoCorrerInicio", correr.transitions[0].destinationState.name, "va antes que SaltoInicio");
            Assert.AreEqual("SaltoCorrerInicio", correr.transitions[1].destinationState.name, "va antes que SaltoInicio");
            Assert.IsTrue(correr.transitions.Take(2).Any(t => t.conditions.Any(c => c.parameter == ReglasVisual.EnSuelo && c.mode == AnimatorConditionMode.IfNot)));
            Assert.IsTrue(correr.transitions.Take(2).Any(t => t.conditions.Any(c => c.parameter == ReglasAnimacion.PreparandoSalto && c.mode == AnimatorConditionMode.If)));
            foreach ((string estado, string clip) in ReglasAnimacion.EstadosSaltoCorrerT1540)
            {
                Assert.AreEqual(clip, EstadoBase(estado).motion.name, estado);
            }
            Assert.AreEqual("SaltoCorrerAire", EstadoBase("SaltoCorrerInicio").transitions.First(t => t.hasExitTime).destinationState.name);
            Assert.IsTrue(EstadoBase("SaltoCorrerAire").transitions.Any(t => t.destinationState.name == "SaltoCorrerAterrizaje"));
            AnimatorStateTransition rodada = Base().layers[0].stateMachine.anyStateTransitions.First(t => t.destinationState != null && t.destinationState.name == "SaltoCorrerRodada");
            Assert.IsTrue(rodada.conditions.Any(c => c.parameter == ReglasAnimacion.AterrizajeRodada), "T-1550: la rodada tiene su propio trigger (caer corriendo desde ~6 m/s de caída, sin bloquear)");
            AnimatorStateTransition[] cualquiera = Base().layers[0].stateMachine.anyStateTransitions;
            Assert.Less(System.Array.IndexOf(cualquiera, rodada), System.Array.FindIndex(cualquiera, t => t.destinationState != null && t.destinationState.name == "AterrizajeFuerte"), "va antes que AterrizajeFuerte");
        }

        [Test]
        public void Saltos_LaRapidezDeSaltoInicioYAterrizajeSeMultiplicaPorSuParametro()
        {
            foreach ((string estado, string parametro) in new[] { ("SaltoInicio", ReglasAnimacion.RapidezSaltoInicio), ("SaltoAterrizaje", ReglasAnimacion.RapidezSaltoAterrizaje) })
            {
                AnimatorState s = EstadoBase(estado);
                Assert.IsTrue(s.speedParameterActive, estado);
                Assert.AreEqual(parametro, s.speedParameter, estado);
                Assert.AreEqual(ReglasAnimacion.VelocidadEstado(estado), s.speed, 1e-4f, estado + ": la rapidez de los demás papeles no cambia");
                AnimatorControllerParameter p = Base().parameters.First(x => x.name == parametro);
                Assert.AreEqual(1f, p.defaultFloat, 1e-4f);
            }
        }

        [TestCase("Jugador", "Skull_Burla", "PJ01_Burla")]
        [TestCase("Aliados", "Skull_Burla", "PJ01_Burla")]
        [TestCase("NPC", "Skull_Burla", "PJ01_Burla")]
        [TestCase("SKULLS", "Skull_Burla", "PJ01_Burla")]
        [TestCase("SNAKES", "Skull_Burla", "PJ01_Burla")]
        [TestCase("Guardias", "Skull_Burla", "PJ01_Burla")]
        [TestCase("Robots", "Skull_Burla", "PJ01_Burla")]
        [TestCase("Profes", "Skull_Burla", "PJ01_Burla")]
        [TestCase("Jefe", "Skull_Burla", "PJ01_Burla")]
        [TestCase("Aliados", "Pistola_Dano_Frente", "PJ01_Dano_Frente")]
        [TestCase("Aliados", "Pistola_Dano_Espalda", "PJ01_Dano_Espalda")]
        [TestCase("SKULLS", "Pistola_Dano_Frente", "PJ01_Dano_Frente")]
        [TestCase("SNAKES", "Pistola_Dano_Espalda", "PJ01_Dano_Espalda")]
        [TestCase("Guardias", "Pistola_Dano_Frente", "PJ01_Dano_Frente")]
        [TestCase("Jefe", "Pistola_Dano_Frente", "PJ01_Dano_Frente")]
        [TestCase("Profes", "Pistola_Dano_Espalda", "PJ01_Dano_Espalda")]
        [TestCase("Robots", "Pistola_Dano_Frente", "PJ01_Dano_Frente")]
        [TestCase("NPC", "Pistola_Dano_Frente", "NPC_Balazo_Frente")]
        [TestCase("NPC", "Pistola_Dano_Espalda", "NPC_Balazo_Espalda")]
        [TestCase("Jugador", "Pistola_Dano_Frente", "PJ01_Dano_Frente")]
        public void Override_DePapel_TieneLaBurlaYElDanoDeT1540(string papel, string clipBase, string esperado)
        {
            Assert.AreEqual(esperado, Valores(ConstructorControladorBase.RutaOverride(papel))[clipBase]);
        }

        [TestCase("E15", "Pistola_Dano_Frente", "E15_Impacto")]
        [TestCase("E15", "Muerte_Adelante", "E15_Muerte")]
        [TestCase("E15", "Muerte_Atras", "E15_Muerte")]
        [TestCase("E15", "PJ01_Muerte_Explosion", "E15_Muerte")]
        [TestCase("E15", "PJ01_Dano_Empujon", "E15_Impacto")]
        [TestCase("E16", "Pistola_Dano_Frente", "E16_Impacto")]
        [TestCase("E16", "Muerte_Desmayo", "E16_Muerte")]
        [TestCase("E16", "PJ01_Derribo_Explosion", "E16_Impacto")]
        [TestCase("E18", "Pistola_Dano_Frente", "PJ01_Dano_Frente")]
        [TestCase("Hacker", "Pistola_Dano_Espalda", "PJ01_Dano_Espalda")]
        [TestCase("NPC_Mujer", "Pistola_Dano_Frente", "NPC_Balazo_Frente")]
        [TestCase("NPC_Celular", "Skull_Burla", "PJ01_Burla")]
        public void OverrideExtra_TieneLoDeT1540(string nombre, string clipBase, string esperado)
        {
            Assert.AreEqual(esperado, Valores(ClipsNpcNovaT1506.RutaOverrideExtra(nombre))[clipBase]);
        }

        [Test]
        public void Overrides_DeLosPapelesHumanos_UsanLosClipsNuevosTalCual()
        {
            foreach (string papel in new[] { "Jugador", "Aliados", "NPC", "SKULLS", "SNAKES", "Guardias", "Profes", "Jefe" })
            {
                Dictionary<string, string> v = Valores(ConstructorControladorBase.RutaOverride(papel));
                foreach (var e in ReglasAnimacion.EstadosT1540)
                {
                    Assert.AreEqual(e.Clip, v[e.Clip], papel + " usa " + e.Clip + " (humanoide compatible: sin override)");
                }
            }
        }

        [Test]
        public void Diagnostico_NoEncuentraProblemas()
        {
            Assert.AreEqual(0, AnimacionesT1540.Diagnostico());
        }
    }
}

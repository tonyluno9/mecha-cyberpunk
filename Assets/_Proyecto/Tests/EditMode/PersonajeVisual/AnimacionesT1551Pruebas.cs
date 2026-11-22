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
    /// <summary>
    /// T-1551: las animaciones aprobadas de combate y eventos están conectadas. La primera mitad es de tablas y de la matriz acción -> clip -> estado (no necesita Unity corrido);
    /// la segunda necesita haber corrido AnimacionesT1551.Aplicar (overrides, Policia.controller y configs) sobre el ControladorBase de T-1540.
    /// </summary>
    public class AnimacionesT1551Pruebas
    {
        private static string Fila(string papel, string clipBase)
        {
            return ConstructorControladorBase.ClipsPorPapel[papel].Last(par => par.Base == clipBase).Papel;
        }

        private static (string Base, string Papel)[] FilasExtra(string nombre)
        {
            return ClipsNpcNovaT1506.OverridesExtra.First(o => o.Nombre == nombre).Clips;
        }

        // ---------- Tablas y matriz ----------

        [Test]
        public void TablaPorPapel_LosPapelesQueSeguianConLasMuertesViejas_UsanLasAprobadas()
        {
            foreach (string papel in AnimacionesT1551.PapelesConMuertesT1551)
            {
                Assert.AreEqual("PJ01_Muerte_Frente", Fila(papel, "Muerte_Adelante"), papel);
                Assert.AreEqual("PJ01_Muerte_Espaldas", Fila(papel, "Muerte_Atras"), papel);
                Assert.AreEqual("PJ01_Muerte_Desmayo", Fila(papel, "Muerte_Desmayo"), papel + ": el desmayo no cambia");
            }
        }

        [Test]
        public void TablaPorPapel_JugadorCivilYGuardias_ConservanSusMuertes()
        {
            Assert.AreEqual("PJ01_Muerte_Frente", Fila("Jugador", "Muerte_Adelante"));
            Assert.AreEqual("PJ01_Muerte_Espaldas", Fila("Jugador", "Muerte_Atras"));
            // decisión de Tony 21-nov (T-1560): los NPC usan las mismas muertes que el jugador (NPC_Muerte_* descartadas).
            Assert.AreEqual("PJ01_Muerte_Frente", Fila("NPC", "Muerte_Adelante"));
            Assert.AreEqual("PJ01_Muerte_Espaldas", Fila("NPC", "Muerte_Atras"));
            Assert.AreEqual("Guardia_Muerte", Fila("Guardias", "Muerte_Adelante"));
            CollectionAssert.DoesNotContain(AnimacionesT1551.PapelesConMuertesT1551, "Jugador");
            CollectionAssert.DoesNotContain(AnimacionesT1551.PapelesConMuertesT1551, "NPC");
            CollectionAssert.DoesNotContain(AnimacionesT1551.PapelesConMuertesT1551, "Guardias");
        }

        [Test]
        public void TablaPorPapel_ElDanoDeTodosLosPapelesHumanos_SigueSiendoPJ01OLoSuyo()
        {
            // "decisión de Tony 20-nov": PJ01_Dano_Frente/Espalda en lugar del genérico de Mixamo o de PJ01_Pistola_Dano_*; no hay PJ01_Dano_Fuerte.
            foreach (string papel in ConstructorControladorBase.Papeles.Where(p => p != "NPC"))
            {
                Assert.AreEqual("PJ01_Dano_Frente", Fila(papel, "Pistola_Dano_Frente"), papel);
                Assert.AreEqual("PJ01_Dano_Espalda", Fila(papel, "Pistola_Dano_Espalda"), papel);
                Assert.AreEqual("PJ01_Pistola_Dano_Fuerte", Fila(papel, "Pistola_Dano_Fuerte"), papel);
            }
            Assert.AreEqual("NPC_Balazo_Frente", Fila("NPC", "Pistola_Dano_Frente"));
            Assert.AreEqual("NPC_Balazo_Espalda", Fila("NPC", "Pistola_Dano_Espalda"));
        }

        [Test]
        public void E15_UsaE15CubrirseAgachadoYEnCobertura_YLosDemasNovaNo()
        {
            (string Base, string Papel)[] e15 = FilasExtra("E15");
            Assert.AreEqual("E15_Cubrirse", e15.Last(p => p.Base == "Loco_AgachadoIdle").Papel);
            Assert.AreEqual("E15_Cubrirse", e15.Last(p => p.Base == "PJ01_Cobertura_Quieto").Papel);
            foreach (string otro in new[] { "E16", "E18", "Hacker" })
            {
                CollectionAssert.IsEmpty(FilasExtra(otro).Where(p => p.Papel == "E15_Cubrirse").ToArray(), otro);
            }
        }

        [Test]
        public void ClavesT1551_SonClipsDelControladorBase_YLosClipsDeLasTablasExisten()
        {
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            Assert.IsNotNull(baseCtrl, "Falta el ControladorBase");
            string[] clipsBase = baseCtrl.animationClips.Select(c => c.name).Distinct().ToArray();
            foreach (string clave in AnimacionesT1551.ClavesT1551)
            {
                CollectionAssert.Contains(clipsBase, clave, "el controlador base no tiene el clip " + clave);
            }
            var filas = new List<(string, string)>(AnimacionesT1551.MuertesNoJugadorT1551) { ("Loco_AgachadoIdle", "E15_Cubrirse") };
            foreach ((string clipBase, string clip) in filas)
            {
                Assert.IsTrue(AnimacionesT1551.ExisteClip(clip), "falta el FBX " + clip + " (para " + clipBase + ")");
            }
            foreach ((string estado, string clip) in AnimacionesT1551.ClipsPolicia)
            {
                Assert.IsTrue(AnimacionesT1551.ExisteClip(clip), "falta el FBX " + clip + " (para " + estado + " del policía)");
            }
        }

        [Test]
        public void Matriz_SinAccionesRepetidas_ConEstadosValidos_YLosSinClipNoTraenClip()
        {
            string[] validos = { AnimacionesT1551.YaEstaba, AnimacionesT1551.ConectadoAhora, AnimacionesT1551.SinClip, AnimacionesT1551.PendienteTony, AnimacionesT1551.OtraTarea };
            Assert.AreEqual(AnimacionesT1551.Matriz.Length, AnimacionesT1551.Matriz.Select(m => m.Accion).Distinct().Count(), "acciones repetidas");
            foreach ((string accion, string clip, string estado) in AnimacionesT1551.Matriz)
            {
                CollectionAssert.Contains(validos, estado, accion);
                Assert.IsNotEmpty(clip, accion);
                if (estado == AnimacionesT1551.SinClip)
                {
                    Assert.IsTrue(clip == "-" || clip.StartsWith("PJ01_Muerte_"), accion + ": un faltante lleva \"-\" (salvo la muerte del civil de Trafico: el clip existe, falta quién lo dispare)");
                }
            }
            Assert.GreaterOrEqual(AnimacionesT1551.Matriz.Count(m => m.Estado == AnimacionesT1551.ConectadoAhora), 3, "lo conectado en esta tarea");
        }

        [Test]
        public void Matriz_LosClipsQueNombraYaEstanEnAssets_ExceptoLasProtesisPendientes()
        {
            foreach ((string accion, string clip, string estado) in AnimacionesT1551.Matriz)
            {
                if (clip == "-" || estado == AnimacionesT1551.SinClip)
                {
                    continue;
                }
                foreach (string nombre in clip.Split('/'))
                {
                    bool protesisPendiente = estado == AnimacionesT1551.PendienteTony && nombre.StartsWith("PJ01_Protesis_");
                    Assert.AreEqual(!protesisPendiente, AnimacionesT1551.ExisteClip(nombre), accion + ": " + nombre);
                }
            }
        }

        [Test]
        public void ConfigCompleta_SoloConLosOchoClips()
        {
            Assert.IsFalse(AnimacionesT1551.ConfigCompleta(null));
            var vacia = ScriptableObject.CreateInstance<ConfigClipsNovaNpc>();
            try
            {
                Assert.IsFalse(AnimacionesT1551.ConfigCompleta(vacia));
                var clip = new AnimationClip();
                vacia.Configurar(clip, clip, clip, clip);
                Assert.IsFalse(AnimacionesT1551.ConfigCompleta(vacia), "faltan los de plática, banca y agachado");
                vacia.ConfigurarNpc(clip, clip, clip, clip);
                Assert.IsTrue(AnimacionesT1551.ConfigCompleta(vacia));
                Object.DestroyImmediate(clip);
            }
            finally
            {
                Object.DestroyImmediate(vacia);
            }
        }

        // ---------- Necesitan haber corrido AnimacionesT1551.Aplicar ----------

        private static Dictionary<string, string> Valores(string ruta)
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta), "Falta " + ruta);
            return AnimacionesT1551.ValoresDeOverride(ruta);
        }

        [TestCase("Aliados"), TestCase("SKULLS"), TestCase("SNAKES"), TestCase("Robots"), TestCase("Profes"), TestCase("Jefe")]
        public void Override_DePapel_MuereConLasMuertesAprobadas(string papel)
        {
            Dictionary<string, string> v = Valores(ConstructorControladorBase.RutaOverride(papel));
            Assert.AreEqual("PJ01_Muerte_Frente", v["Muerte_Adelante"]);
            Assert.AreEqual("PJ01_Muerte_Espaldas", v["Muerte_Atras"]);
            Assert.AreEqual("PJ01_Muerte_Desmayo", v["Muerte_Desmayo"]);
        }

        [Test]
        public void Override_Hacker_HeredaLasMuertesDeSnakes()
        {
            Dictionary<string, string> v = Valores(ClipsNpcNovaT1506.RutaOverrideExtra("Hacker"));
            Assert.AreEqual("PJ01_Muerte_Frente", v["Muerte_Adelante"]);
            Assert.AreEqual("PJ01_Muerte_Espaldas", v["Muerte_Atras"]);
        }

        [Test]
        public void Override_E15_UsaE15Cubrirse_AgachadoYEnCobertura_ConservandoSuMuerte()
        {
            Dictionary<string, string> v = Valores(ClipsNpcNovaT1506.RutaOverrideExtra("E15"));
            Assert.AreEqual("E15_Cubrirse", v["Loco_AgachadoIdle"]);
            Assert.AreEqual("E15_Cubrirse", v["PJ01_Cobertura_Quieto"]);
            Assert.AreEqual("E15_Muerte", v["Muerte_Adelante"]);
            Assert.AreEqual("PJ01_Loco_AgachadoIdle", Valores(ConstructorControladorBase.RutaOverride("Guardias"))["Loco_AgachadoIdle"], "los guardias humanos siguen con PJ01");
            Assert.AreEqual("PJ01_Loco_AgachadoIdle", Valores(ClipsNpcNovaT1506.RutaOverrideExtra("E16"))["Loco_AgachadoIdle"]);
        }

        [Test]
        public void Override_Jugador_DanoDeFrenteYEspalda_EsPJ01_YNingunDanoEsDeMixamo()
        {
            Dictionary<string, string> v = Valores(ConstructorControladorBase.RutaOverride("Jugador"));
            Assert.AreEqual("PJ01_Dano_Frente", v["Pistola_Dano_Frente"]);
            Assert.AreEqual("PJ01_Dano_Espalda", v["Pistola_Dano_Espalda"]);
            Assert.AreEqual("PJ01_Pistola_Dano_Fuerte", v["Pistola_Dano_Fuerte"], "no hay PJ01_Dano_Fuerte");
            foreach (string clave in v.Keys.Where(k => k.Contains("Dano")))
            {
                Assert.IsTrue(v[clave].StartsWith("PJ01_"), clave + " -> " + v[clave]);
            }
        }

        [TestCase("PJ01"), TestCase("woman")]
        public void VisualesDelJugador_UsanOverrideJugador(string visual)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual));
            Assert.IsNotNull(prefab, visual);
            Assert.AreEqual("Override_Jugador", prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController.name);
        }

        [Test]
        public void Override_DeCadaPapelNoJugador_TraeSuDanoPJ01_ODelCivilYDeLosRobots()
        {
            string[] papelesPJ01 = { "Aliados", "SKULLS", "SNAKES", "Guardias", "Robots", "Profes", "Jefe" };
            foreach (string papel in papelesPJ01)
            {
                Dictionary<string, string> v = Valores(ConstructorControladorBase.RutaOverride(papel));
                Assert.AreEqual("PJ01_Dano_Frente", v["Pistola_Dano_Frente"], papel);
                Assert.AreEqual("PJ01_Dano_Espalda", v["Pistola_Dano_Espalda"], papel);
            }
            Assert.AreEqual("NPC_Balazo_Frente", Valores(ConstructorControladorBase.RutaOverride("NPC"))["Pistola_Dano_Frente"]);
            Assert.AreEqual("E15_Impacto", Valores(ClipsNpcNovaT1506.RutaOverrideExtra("E15"))["Pistola_Dano_Frente"]);
            Assert.AreEqual("E16_Impacto", Valores(ClipsNpcNovaT1506.RutaOverrideExtra("E16"))["Pistola_Dano_Frente"]);
        }

        [Test]
        public void Policia_ElDanoYLaCaidaUsanLosClipsAprobadosDePJ01()
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimacionesT1551.RutaPolicia);
            Assert.IsNotNull(c, "Falta Policia.controller");
            foreach ((string estado, string clip) in AnimacionesT1551.ClipsPolicia)
            {
                AnimatorState s = AnimacionesT1551.BuscarEstado(c, estado);
                Assert.IsNotNull(s, "Policia.controller no tiene el estado " + estado);
                Assert.IsNotNull(s.motion, estado);
                Assert.AreEqual(clip, s.motion.name, estado);
                Assert.Greater(s.transitions.Length, 0, estado + " conserva su salida");
            }
        }

        [Test]
        public void ConfigClipsNovaNpc_TieneLosOchoClipsDeUnaVez_YLaCaidaDeNovaEsLaAprobada()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigClipsNovaNpc>(ClipsNpcNovaT1506.RutaConfig);
            Assert.IsTrue(AnimacionesT1551.ConfigCompleta(config));
            Assert.AreEqual("E18_Caida_Aire", config.CaidaAire.name);
            Assert.AreEqual("E18_Caida_Aterrizaje", config.CaidaAterrizaje.name);
            Assert.AreEqual("E18_Caida_Levantarse", config.CaidaLevantarse.name);
            Assert.AreEqual("NPC_Asustarse", config.Asustarse.name);
            Assert.AreEqual("NPC_Agachado_CubriendoCabeza", config.Agachado.name);
        }

        [Test]
        public void ConfigAnimacionParkour_TieneElRoboDeCarro()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigAnimacionParkour>("Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionParkour.asset");
            Assert.IsNotNull(config);
            Assert.AreEqual("PJ01_Carro_SacarConductor", config.CarroSacarConductor.name);
            Assert.AreEqual("PJ01_Conductor_SerSacado", config.ConductorSerSacado.name);
        }

        [Test]
        public void ControladorBase_LosEstadosDeVehiculosRecargasInteraccionesYDerribos_ConservanSuClipParaElJugador()
        {
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            Dictionary<string, string> jugador = Valores(ConstructorControladorBase.RutaOverride("Jugador"));
            foreach ((string estado, string clip) in AnimacionesT1551.EstadosConfirmados)
            {
                Assert.AreEqual(clip, AnimacionesT1551.ClipEfectivoDeEstado(baseCtrl, estado, jugador), "estado " + estado);
            }
        }

        [Test]
        public void Aplicar_RepetidoNoCambiaNada()
        {
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            Assert.AreEqual(0, AnimacionesT1551.AplicarOverrides(baseCtrl), "los overrides ya estaban aplicados (corre Aplicar antes)");
            Assert.AreEqual(0, AnimacionesT1551.AplicarPolicia(), "Policia.controller ya estaba aplicado (corre Aplicar antes)");
            Assert.AreEqual(0, AnimacionesT1551.ReparaConfigs(), "la config de clips ya estaba completa");
        }
    }
}

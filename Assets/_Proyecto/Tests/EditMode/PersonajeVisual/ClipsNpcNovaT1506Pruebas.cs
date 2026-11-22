using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-1506: los clips de Blender del 19-nov (PJ01 no locomoción, NPC y Nova) están importados bien (Humanoid de PJ01 o Generic del perro, bucle y aditivos),
    /// van en los papeles y overrides que dice la tabla, y los prefabs usan el override de su tipo. Necesita haber corrido ClipsNpcNovaT1506.Importar y Aplicar.
    /// </summary>
    public class ClipsNpcNovaT1506Pruebas
    {
        // T-1553: E16_CaminarEscudo y los E18_* están en la carpeta Clips de su personaje.
        private static string RutaClip(string nombre) => nombre.StartsWith("E17_") ? ClipsNpcNovaT1506.CarpetaPerro + nombre + ".fbx" : ClipsRobotsNovaT1553.RutaClip(nombre);

        private static ModelImporterClipAnimation ClipImportado(string nombre)
        {
            var importador = (ModelImporter)AssetImporter.GetAtPath(RutaClip(nombre));
            Assert.IsNotNull(importador, "Falta " + nombre);
            Assert.AreEqual(1, importador.clipAnimations.Length, nombre);
            return importador.clipAnimations[0];
        }

        private static Dictionary<string, string> Valores(string ruta)
        {
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
            Assert.IsNotNull(o, "Falta " + ruta);
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            return pares.Where(p => p.Key != null && p.Value != null).ToDictionary(p => p.Key.name, p => p.Value.name);
        }

        [Test]
        public void ClipsHumanoides_SonHumanoidConElAvatarYLaEscalaDePJ01_ConUnClipDelNombreDelArchivo()
        {
            foreach (string nombre in ClipsNpcNovaT1506.ClipsHumanoides)
            {
                var importador = (ModelImporter)AssetImporter.GetAtPath(RutaClip(nombre));
                Assert.IsNotNull(importador, "Falta " + nombre);
                Assert.AreEqual(ModelImporterAnimationType.Human, importador.animationType, nombre);
                // T-1553: los cuatro clips con el rig del propio personaje llevan la escala de su personaje; los demás, la de PJ01.
                if (TablaClipsT1560.ClipsConRigPropio.Contains(nombre))
                {
                    continue; // T-1560: esqueleto propio del robot, con la escala de su personaje (la comprueba AnimacionesT1560Pruebas).
                }
                float escala = ClipsRobotsNovaT1553.TieneRigPropio(nombre)
                    ? ((ModelImporter)AssetImporter.GetAtPath(ClipsRobotsNovaT1553.RutaModeloDe(nombre))).globalScale
                    : ImportadorClipsPJ01.EscalaClips;
                Assert.AreEqual(escala, importador.globalScale, 1e-3f, nombre);
                Assert.AreEqual(nombre, ClipImportado(nombre).name, nombre);
            }
        }

        [Test]
        public void ClipsCiclicos_TienenBucle_YLosDeUnaVezNo()
        {
            foreach (string nombre in ClipsCiclicosT1506.ConBucle)
            {
                string archivo = nombre.StartsWith("CaminarApuntando_") || nombre.StartsWith("Cobertura_") || nombre.StartsWith("Manejar_") ? "PJ01_" + nombre : nombre;
                Assert.IsTrue(ClipImportado(archivo).loopTime, archivo + " es cíclico");
            }
            foreach (string nombre in new[]
            {
                "PJ01_Cobertura_Asomarse", "PJ01_Cobertura_Regresar", "PJ01_Golpe1_Directo", "PJ01_Golpe2_Gancho", "PJ01_Muerte_Frente", "PJ01_Muerte_Espaldas",
                "NPC_Asustarse", "NPC_Muerte_Frente", "E15_Muerte", "E16_Muerte", "E16_Golpe_Escudo", "E18_Caida_Aterrizaje", "E18_Caida_Levantarse", "Guardia_Muerte",
                "E17_Morder", "E17_Muerte", "E17_SaltoAtaque",
            })
            {
                Assert.IsFalse(ClipImportado(nombre).loopTime, nombre + " va una sola vez");
            }
        }

        [Test]
        public void ClipsDeDano_SonAditivos_ConElPrimerCuadroDeReferencia()
        {
            foreach (string nombre in new[] { "PJ01_Dano_Frente", "PJ01_Dano_Espalda", "PJ01_Dano_Izquierda", "PJ01_Dano_Derecha", "NPC_Balazo_Frente", "NPC_Balazo_Espalda", "E15_Impacto", "E16_Impacto" })
            {
                ModelImporterClipAnimation clip = ClipImportado(nombre);
                Assert.IsTrue(clip.hasAdditiveReferencePose, nombre);
                Assert.AreEqual(clip.firstFrame, clip.additiveReferencePoseFrame, 0.001f, nombre);
                Assert.IsFalse(clip.loopTime, nombre);
            }
        }

        [Test]
        public void ClipsDelPerro_SonGeneric_ConElAvatarDelModeloDelPerro()
        {
            var modelo = (ModelImporter)AssetImporter.GetAtPath(ClipsNpcNovaT1506.RutaModeloPerro);
            foreach (string nombre in ClipsNpcNovaT1506.ClipsPerro)
            {
                var importador = (ModelImporter)AssetImporter.GetAtPath(RutaClip(nombre));
                Assert.AreEqual(ModelImporterAnimationType.Generic, importador.animationType, nombre);
                Assert.AreEqual(modelo.globalScale, importador.globalScale, 1e-4f, nombre);
                Assert.IsNotNull(ClipsNpcNovaT1506.CargarClip(nombre), nombre);
            }
        }

        [Test]
        public void ClipsDelPerro_UsanLosHuesosDelModelo_SinRutasHuerfanas()
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ClipsNpcNovaT1506.RutaModeloPerro);
            var rutas = new HashSet<string>();
            void Recorrer(Transform t, string prefijo)
            {
                foreach (Transform hijo in t)
                {
                    string ruta = prefijo.Length == 0 ? hijo.name : prefijo + "/" + hijo.name;
                    rutas.Add(ruta);
                    Recorrer(hijo, ruta);
                }
            }
            Recorrer(modelo.transform, "");
            foreach (string nombre in ClipsNpcNovaT1506.ClipsPerro)
            {
                AnimationClip clip = ClipsNpcNovaT1506.CargarClip(nombre);
                int sinHueso = AnimationUtility.GetCurveBindings(clip).Count(b => b.path.Length > 0 && !rutas.Contains(b.path));
                Assert.AreEqual(0, sinHueso, nombre + ": curvas sin hueso en el modelo del perro");
            }
        }

        [TestCase("Jugador", "Punos_GolpeDer", "PJ01_Golpe_Der")] // decisión de Tony 21-nov (T-1560)
        [TestCase("Jugador", "Punos_GolpeIzq", "PJ01_Golpe_Izq")]
        [TestCase("Jugador", "Muerte_Adelante", "PJ01_Muerte_Frente")]
        [TestCase("Jugador", "Muerte_Atras", "PJ01_Muerte_Espaldas")]
        [TestCase("Jugador", "Pistola_Dano_Frente", "PJ01_Dano_Frente")]
        [TestCase("Jugador", "Pistola_Dano_Espalda", "PJ01_Dano_Espalda")]
        [TestCase("Jugador", "Carro_Manejar_Bucle", "PJ01_Manejar_Carro")]
        [TestCase("Jugador", "Pistola_ApuntarAtras", "PJ01_CaminarApuntando_Atras")]
        [TestCase("Jugador", "Pistola_ApuntarLateral_Derecha", "PJ01_CaminarApuntando_Derecha")]
        [TestCase("Jugador", "Pistola_ApuntarLateral_Izquierda", "PJ01_CaminarApuntando_Izquierda")]
        [TestCase("NPC", "Loco_Idle", "NPC_Esperar")]
        [TestCase("NPC", "Loco_Caminar", "NPC_Caminar_Hombre")]
        [TestCase("NPC", "Loco_Correr", "NPC_Huir_Corriendo")]
        [TestCase("NPC", "Pistola_Dano_Frente", "NPC_Balazo_Frente")]
        [TestCase("NPC", "Pistola_Dano_Espalda", "NPC_Balazo_Espalda")]
        [TestCase("NPC", "Muerte_Adelante", "PJ01_Muerte_Frente")] // decisión de Tony 21-nov (T-1560): mismas muertes que el jugador
        [TestCase("NPC", "Muerte_Atras", "PJ01_Muerte_Espaldas")]
        [TestCase("Guardias", "Loco_Caminar", "Guardia_Caminar")]
        [TestCase("Guardias", "Pistola_Disparo", "Guardia_Disparar")]
        [TestCase("Guardias", "Loco_Caida", "Guardia_Caida_Aire")]
        [TestCase("Guardias", "Muerte_Adelante", "Guardia_Muerte")]
        public void Override_DePapel_TieneElClipNuevoEnSuPapel(string papel, string clipBase, string esperado)
        {
            Assert.AreEqual(esperado, Valores(ConstructorControladorBase.RutaOverride(papel))[clipBase]);
        }

        [TestCase("NPC_Mujer", "Loco_Caminar", "NPC_Caminar_Mujer")]
        [TestCase("NPC_Mayor", "Loco_Caminar", "NPC_Caminar_Mayor_Baston")]
        [TestCase("NPC_Celular", "Loco_Caminar", "NPC_Caminar_Celular")]
        [TestCase("NPC_Mayor", "Loco_Idle", "NPC_Esperar")]
        [TestCase("E15", "Loco_Caminar", "E15_Caminar")]
        [TestCase("E15", "Loco_Correr", "E15_Correr")]
        [TestCase("E15", "Pistola_Disparo", "E15_Disparar")]
        [TestCase("E15", "Pistola_Dano_Frente", "E15_Impacto")]
        [TestCase("E16", "Loco_Caminar", "E16_Caminar")]
        // T-1553 (decisión de Tony 20-nov): con el escudo arriba (canal ConImplante = 1 en el controlador base) camina con E16_CaminarEscudo, al acercarse o al recibir disparos.
        [TestCase("E16", "Loco_Caminar_ConImplante", "E16_CaminarEscudo")]
        [TestCase("E16", "Loco_Correr_ConImplante", "E16_CaminarEscudo")]
        [TestCase("E16", "Loco_Correr", "E16_Caminar")]
        [TestCase("E16", "Loco_GirarDer", "E16_Girar_Derecha")]
        [TestCase("E16", "Loco_GirarIzq", "E16_Girar_Izquierda")]
        [TestCase("E16", "Pistola_Disparo", "E16_Disparar_Canon")]
        [TestCase("E16", "Punos_GolpeDer", "E16_Golpe_Escudo")]
        [TestCase("E16", "Pistola_Dano_Frente", "E16_Impacto")]
        [TestCase("E18", "Pistola_Disparo", "E18_Disparar")]
        [TestCase("E18", "Loco_Caida", "E18_Jetpack_Volar")]
        [TestCase("E18", "Loco_SaltoAterrizaje", "E18_Caida_Aterrizaje")]
        // T-1553: el E18 no lleva arma; camina, corre y golpea con sus propios clips.
        [TestCase("E18", "Loco_Caminar", "E18_Caminar")]
        [TestCase("E18", "Loco_Caminar_ConImplante", "E18_Caminar")]
        [TestCase("E18", "Loco_Correr", "E18_Correr")]
        [TestCase("E18", "Loco_Correr_ConImplante", "E18_Correr")]
        [TestCase("E18", "Punos_GolpeDer", "E18_Golpe_Der")]
        [TestCase("E18", "Punos_GolpeIzq", "E18_Golpe_Der")]
        [TestCase("E18", "Punos_Fuerte", "E18_Golpe_Der")]
        [TestCase("Hacker", "Loco_Idle", "NPC_Hacker_Tecleando")]
        public void OverrideExtra_TieneElClipDeSuTipo(string nombre, string clipBase, string esperado)
        {
            Assert.AreEqual(esperado, Valores(ClipsNpcNovaT1506.RutaOverrideExtra(nombre))[clipBase]);
        }

        [Test]
        public void Prefabs_UsanElOverrideDeSuTipo_ConLasVelocidadesDeSusClips()
        {
            foreach ((string visual, string nombreOverride, float caminar, float correr) in ClipsNpcNovaT1506.Visuales)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual));
                Assert.IsNotNull(prefab, visual);
                if (nombreOverride != null)
                {
                    Assert.AreEqual("Override_" + nombreOverride, prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController.name, visual);
                }
            }
        }

        [Test]
        public void Visuales_Abuela_UsaElMayorConBaston_Turista_ElCelular_YElHackerElClipSentado()
        {
            string Controlador(string visual) => AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual)).GetComponentInChildren<Animator>(true).runtimeAnimatorController.name;
            Assert.AreEqual("Override_NPC_Mayor", Controlador("NP12_Abuela_a"));
            Assert.AreEqual("Override_NPC_Celular", Controlador("NP08_Turista_a"));
            Assert.AreEqual("Override_E15", Controlador("E15_CentinelaNova_a"));
            Assert.AreEqual("Override_E16", Controlador("E16_RobotPesadoNova_a"));
            Assert.AreEqual("Override_E18", Controlador("E18_EliteNova_a"));
            Assert.AreEqual("Override_Guardias", Controlador("GuardiaNova"));
            Assert.AreEqual("Override_Hacker", Controlador("Hacker"));
        }

        [TestCase(ClipsNpcNovaT1506.ControladorMesera, "Caminar", "NPC_Mesera_Charola")]
        public void ControladoresPropios_TienenElClipNuevoEnSuEstado(string ruta, string estado, string clip)
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
            AnimatorState s = controlador.layers.SelectMany(l => l.stateMachine.states).First(h => h.state.name == estado).state;
            Assert.AreEqual(clip, s.motion.name);
        }

        [Test]
        public void Config_TieneLosClipsDeLaCaidaDeNovaYElSusto()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigClipsNovaNpc>(ClipsNpcNovaT1506.RutaConfig);
            Assert.IsNotNull(config);
            Assert.AreEqual("E18_Caida_Aire", config.CaidaAire.name);
            Assert.AreEqual("E18_Caida_Aterrizaje", config.CaidaAterrizaje.name);
            Assert.AreEqual("E18_Caida_Levantarse", config.CaidaLevantarse.name);
            Assert.AreEqual("NPC_Asustarse", config.Asustarse.name);
            Assert.IsTrue(config.CaidaAire.isLooping, "caer en el aire se repite");
        }

        [Test]
        public void ClipsDelJugador_NoTocanLaLocomocion()
        {
            foreach ((string b, string _) in ClipsNpcNovaT1506.ClipsSoloJugador)
            {
                StringAssert.DoesNotStartWith("Loco_", b, "la locomoción es de T-1505");
            }
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-985: auditoría del controlador efectivo del jugador (ControladorBase + Override_Jugador): ningún clip que no sea PJ01_* (Mixamo viejo)
    /// y los 80 clips PJ01_* de la carpeta de clips aparecen en algún estado, sub-máquina o árbol de mezcla.
    /// </summary>
    public class ClipsPJ01JugadorT985Pruebas
    {
        private static Dictionary<AnimationClip, AnimationClip> MapaOverride()
        {
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride("Jugador"));
            Assert.IsNotNull(o, "Falta Override_Jugador");
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            var mapa = new Dictionary<AnimationClip, AnimationClip>();
            foreach (var p in pares)
            {
                if (p.Key != null) { mapa[p.Key] = p.Value; }
            }
            return mapa;
        }

        private static AnimationClip Efectivo(AnimationClip c, Dictionary<AnimationClip, AnimationClip> mapa)
            => mapa.TryGetValue(c, out AnimationClip o) && o != null ? o : c;

        private static void Motion(string ruta, Motion m, Dictionary<AnimationClip, AnimationClip> mapa, List<(string, string)> salida)
        {
            if (m is BlendTree arbol)
            {
                foreach (ChildMotion h in arbol.children) { Motion(ruta + "/" + arbol.name, h.motion, mapa, salida); }
            }
            else if (m is AnimationClip c)
            {
                salida.Add((ruta, Efectivo(c, mapa).name));
            }
        }

        private static void Maquina(string ruta, AnimatorStateMachine sm, Dictionary<AnimationClip, AnimationClip> mapa, List<(string, string)> salida)
        {
            foreach (ChildAnimatorState s in sm.states) { Motion(ruta + "/" + s.state.name, s.state.motion, mapa, salida); }
            foreach (ChildAnimatorStateMachine hija in sm.stateMachines) { Maquina(ruta + "/" + hija.stateMachine.name, hija.stateMachine, mapa, salida); }
        }

        private static List<(string Estado, string Clip)> Tabla()
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            Assert.IsNotNull(c, "Falta ControladorBase");
            var mapa = MapaOverride();
            var salida = new List<(string, string)>();
            foreach (AnimatorControllerLayer capa in c.layers) { Maquina(capa.name, capa.stateMachine, mapa, salida); }
            return salida;
        }

        private const string PrefijoEscalera = "PJ01_Escalera_";
        // T-1505: PJ01_Escaleras_Subir/Bajar (stairs) van en ConfigAnimacionEscalera en lugar de PJ01_Escalera_Subir/Bajar.
        private const string PrefijoEscalerasT1505 = "PJ01_Escaleras_";

        // T-1310: el parkour, la caída y el robo de carro los juega un PlayableGraph desde ConfigAnimacionParkour (se revisan en ClipsRonda2T1310Pruebas).
        private static bool FueraDelControlador(string n)
        {
            return n.StartsWith("PJ01_Parkour_") || n == "PJ01_Carro_SacarConductor" || n == "PJ01_Conductor_SerSacado";
        }

        private static string[] ClipsDeLaCarpeta()
        {
            // T-1204: los PJ01_Escalera_* no van en el controlador: los juega AnimacionEscalera desde ConfigAnimacionEscalera (se revisan aparte).
            return Directory.GetFiles(ConstructorControladorBase.CarpetaClipsPJ01, "PJ01_*.fbx").Select(Path.GetFileNameWithoutExtension)
                .Where(n => !n.StartsWith(PrefijoEscalera) && !n.StartsWith(PrefijoEscalerasT1505) && !FueraDelControlador(n)).OrderBy(n => n).ToArray();
        }

        [Test]
        public void Jugador_NingunClipEfectivoEsDeMixamo_YLosOchentaPJ01SeUsan()
        {
            List<(string Estado, string Clip)> tabla = Tabla();
            var sb = new StringBuilder("[T-985] estado -> clip efectivo del jugador\n");
            foreach ((string estado, string clip) in tabla) { sb.AppendLine(estado + " -> " + clip); }
            Debug.Log(sb.ToString());

            // T-1560: los estados de NPC, pandilleros y robots (EstadosNpcT1560) viven en el mismo controlador base; el jugador no los usa y su clip puede ser NPC_*, E15_* o E16_*.
            var estadosNpc = new HashSet<string>(EstadosNpcT1560.Nombres);
            string[] noPJ01 = tabla.Where(t => !t.Clip.StartsWith(ConstructorControladorBase.PrefijoClipPJ01) && !estadosNpc.Contains(t.Estado.Substring(t.Estado.LastIndexOf('/') + 1)))
                .Select(t => t.Estado + " -> " + t.Clip).ToArray();
            string[] todos = ClipsDeLaCarpeta();
            var usados = new HashSet<string>(tabla.Select(t => t.Clip));
            // T-1484: el paso normal del jugador es el trote; los clips de caminar de PJ01 los usan los NPC.
            var soloNpc = new HashSet<string> { "PJ01_CaminarApuntando_Adelante", "PJ01_Cobertura_Quieto", "PJ01_Cobertura_Asomarse", "PJ01_Cobertura_Regresar", "PJ01_Dano_Izquierda", "PJ01_Dano_Derecha", "PJ01_Manejar_Moto", "PJ01_Punos_GolpeDer", "PJ01_Punos_GolpeIzq", "PJ01_Muerte_Adelante", "PJ01_Muerte_Atras", "PJ01_Carro_Manejar", "PJ01_Pistola_Dano_Frente", "PJ01_Pistola_Dano_Espalda", "PJ01_Pistola_ApuntarAtras", "PJ01_Pistola_ApuntarLateral_Derecha", "PJ01_Pistola_ApuntarLateral_Izquierda", "PJ01_Caminar_SinImplante", "PJ01_Correr_SinImplante", "PJ01_Loco_SaltoInicio", "PJ01_Loco_SaltoAire", "PJ01_Loco_SaltoAterrizaje" };
            // T-1506: PJ01_Moto_Manejar quedó reemplazado por PJ01_Manejar_Moto (estado MotoManejar); solo queda como fila vieja en los overrides.
            // T-1550 (decisión de Tony del 20-nov): PJ01_Sprint ya no se usa (lo reemplaza PJ01_CorrerRapido); el archivo se queda.
            var reemplazados = new HashSet<string> { "PJ01_Moto_Manejar", "PJ01_Sprint" };
            // decisión de Tony 21-nov (T-1560): descartados PJ01_Golpe1_Directo, PJ01_Golpe2_Gancho y PJ01_Punos_Fuerte; los clips de Mixamo del 21-nov que Tony todavía compara con los actuales
            // (rifle caminando, saltos, lanzar, granadas, recargas, sacar y guardar el fusil) se importan pero su estado lo decide T-1561 o Tony.
            reemplazados.UnionWith(new[] { "PJ01_Golpe1_Directo", "PJ01_Golpe2_Gancho", "PJ01_Punos_Fuerte", "PJ01_Rifle_CaminarMixamo", "PJ01_Salto_AdelanteMixamo", "PJ01_Salto_B_Mixamo", "PJ01_Lanzar",
                "PJ01_Granada_Agachado", "PJ01_Granada_DePie", "PJ01_Recargar_A", "PJ01_Recargar_B", "PJ01_Rifle_Sacar", "PJ01_Rifle_Guardar", "PJ01_Escalera_SubirMixamo", "PJ01_Parkour_SaltarBardaMixamo" });
            string[] sinUsar = todos.Where(n => !usados.Contains(n) && !soloNpc.Contains(n) && !reemplazados.Contains(n)).ToArray();
            Debug.Log("[T-985] clips no PJ01: " + noPJ01.Length + "; PJ01 sin usar: " + sinUsar.Length + " de " + todos.Length);

            Assert.AreEqual(154, todos.Length, "La carpeta de clips debe tener los 154 PJ01_* (121 de antes + 33 de Blender y Mixamo del 21-nov, T-1560)");
            Assert.IsEmpty(noPJ01, "Clips que no son PJ01 en el jugador:\n" + string.Join("\n", noPJ01));
            Assert.IsEmpty(sinUsar, "PJ01 sin ningún estado:\n" + string.Join("\n", sinUsar));
        }

        [Test]
        public void Escalera_LosClipsPJ01_Escalera_EstanEnLaConfigDeEscalera()
        {
            string[] escalera = Directory.GetFiles(ConstructorControladorBase.CarpetaClipsPJ01, PrefijoEscalera + "*.fbx").Select(Path.GetFileNameWithoutExtension).ToArray();
            var config = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.ConfigAnimacionEscalera>("Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionEscalera.asset");
            Assert.IsNotNull(config, "Falta ConfigAnimacionEscalera");
            var enConfig = new HashSet<string>(new[] { config.Entrar, config.Subir, config.Bajar, config.SalirArriba, config.SalirAbajo }.Where(c => c != null).Select(c => c.name));
            Assert.AreEqual(5, escalera.Length, "Los 5 PJ01_Escalera_*");
            foreach (string nombre in escalera)
            {
                // T-1505: subir y bajar de la config son PJ01_Escaleras_Subir/Bajar (el resto sigue siendo PJ01_Escalera_*).
                if (nombre == "PJ01_Escalera_Subir" || nombre == "PJ01_Escalera_Bajar") { continue; }
                Assert.IsTrue(enConfig.Contains(nombre), nombre + " no está en ConfigAnimacionEscalera");
            }
        }
    }
}

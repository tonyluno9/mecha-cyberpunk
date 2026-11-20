using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        // T-1310: el parkour, la caída y el robo de carro los juega un PlayableGraph desde ConfigAnimacionParkour (se revisan en ClipsRonda2T1310Pruebas).
        private static bool FueraDelControlador(string n)
        {
            return n.StartsWith("PJ01_Parkour_") || n == "PJ01_Carro_SacarConductor" || n == "PJ01_Conductor_SerSacado";
        }

        private static string[] ClipsDeLaCarpeta()
        {
            // T-1204: los PJ01_Escalera_* no van en el controlador: los juega AnimacionEscalera desde ConfigAnimacionEscalera (se revisan aparte).
            return Directory.GetFiles(ConstructorControladorBase.CarpetaClipsPJ01, "PJ01_*.fbx").Select(Path.GetFileNameWithoutExtension)
                .Where(n => !n.StartsWith(PrefijoEscalera) && !FueraDelControlador(n)).OrderBy(n => n).ToArray();
        }

        [Test]
        public void Jugador_NingunClipEfectivoEsDeMixamo_YLosOchentaPJ01SeUsan()
        {
            List<(string Estado, string Clip)> tabla = Tabla();
            var sb = new StringBuilder("[T-985] estado -> clip efectivo del jugador\n");
            foreach ((string estado, string clip) in tabla) { sb.AppendLine(estado + " -> " + clip); }
            Debug.Log(sb.ToString());

            string[] noPJ01 = tabla.Where(t => !t.Clip.StartsWith(ConstructorControladorBase.PrefijoClipPJ01)).Select(t => t.Estado + " -> " + t.Clip).ToArray();
            string[] todos = ClipsDeLaCarpeta();
            var usados = new HashSet<string>(tabla.Select(t => t.Clip));
            // T-1484: el paso normal del jugador es el trote; los clips de caminar de PJ01 los usan los NPC.
            var soloNpc = new HashSet<string> { "PJ01_Caminar_SinImplante", "PJ01_Caminar_ConImplante" };
            string[] sinUsar = todos.Where(n => !usados.Contains(n) && !soloNpc.Contains(n)).ToArray();
            Debug.Log("[T-985] clips no PJ01: " + noPJ01.Length + "; PJ01 sin usar: " + sinUsar.Length + " de " + todos.Length);

            Assert.AreEqual(83, todos.Length, "La carpeta de clips debe tener los 83 PJ01_* del controlador (T-1310: +3 de cobertura con rifle)");
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
                Assert.IsTrue(enConfig.Contains(nombre), nombre + " no está en ConfigAnimacionEscalera");
            }
        }
    }
}

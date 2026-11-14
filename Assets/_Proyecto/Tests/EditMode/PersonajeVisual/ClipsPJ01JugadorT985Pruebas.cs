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

        private static string[] ClipsDeLaCarpeta()
        {
            return Directory.GetFiles(ConstructorControladorBase.CarpetaClipsPJ01, "PJ01_*.fbx").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToArray();
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
            string[] sinUsar = todos.Where(n => !usados.Contains(n)).ToArray();
            Debug.Log("[T-985] clips no PJ01: " + noPJ01.Length + "; PJ01 sin usar: " + sinUsar.Length + " de " + todos.Length);

            Assert.AreEqual(80, todos.Length, "La carpeta de clips debe tener los 80 PJ01_*");
            Assert.IsEmpty(noPJ01, "Clips que no son PJ01 en el jugador:\n" + string.Join("\n", noPJ01));
            Assert.IsEmpty(sinUsar, "PJ01 sin ningún estado:\n" + string.Join("\n", sinUsar));
        }
    }
}

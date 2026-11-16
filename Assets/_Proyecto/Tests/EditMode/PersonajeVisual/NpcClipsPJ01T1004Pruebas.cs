using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-1004: todos los humanoides NPC (pandilleros, peatones, policía, guardias de Nova, meseras...) usan solo clips de PJ01 (los del jugador).
    /// Recorre los prefabs del proyecto y los controladores que usa la escena de la ciudad (las meseras llevan el controlador propio del modelo).
    /// </summary>
    public class NpcClipsPJ01T1004Pruebas
    {
        private const string PrefijoPJ01 = "PJ01_";
        private const string RutaEscenaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const int MinimoDePrefabsHumanoides = 15;
        // Los muñecos grises del creador de pruebas y el visual de prueba no son humanoides de juego.
        private static readonly string[] Excluidos = { "/MunecoHumano/", "Visual_Prueba.prefab" };

        private static IEnumerable<(string Ruta, Animator Animador)> AnimadoresHumanoides()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Proyecto", "Assets/Arte" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (Excluidos.Any(ruta.Contains))
                {
                    continue;
                }
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                foreach (Animator a in prefab.GetComponentsInChildren<Animator>(true))
                {
                    if (a.avatar != null && a.avatar.isHuman && a.runtimeAnimatorController != null)
                    {
                        yield return (ruta, a);
                    }
                }
            }
        }

        private static string[] ClipsAjenos(RuntimeAnimatorController c)
            => c.animationClips.Where(x => x != null && !x.name.StartsWith(PrefijoPJ01)).Select(x => x.name).Distinct().ToArray();

        [Test]
        public void CadaHumanoideDeLosPrefabs_SoloUsaClipsDePJ01()
        {
            var fallos = new List<string>();
            int revisados = 0;
            foreach ((string ruta, Animator a) in AnimadoresHumanoides())
            {
                revisados++;
                string[] ajenos = ClipsAjenos(a.runtimeAnimatorController);
                if (ajenos.Length > 0)
                {
                    fallos.Add(ruta + " (" + AssetDatabase.GetAssetPath(a.runtimeAnimatorController) + "): " + string.Join(", ", ajenos));
                }
            }
            Assert.Greater(revisados, MinimoDePrefabsHumanoides, "Se revisaron los prefabs humanoides");
            Assert.IsEmpty(fallos, "Clips que no son de PJ01 en NPC humanoides:\n" + string.Join("\n", fallos));
        }

        [Test]
        public void ElPeatonLosPandillerosElPoliciaYLosGuardias_EstanEntreLosRevisados()
        {
            var rutas = AnimadoresHumanoides().Select(t => t.Ruta).ToArray();
            foreach (string esperado in new[] { "Peaton.prefab", "PandilleroArmado_Skulls.prefab", "PandilleroArmado_JefeSkulls.prefab", "PoliciaAPie.prefab", "Visual_GuardiaNova.prefab", "Visual_MeseraRobot.prefab", "Visual_man.prefab" })
            {
                Assert.IsTrue(rutas.Any(r => r.EndsWith("/" + esperado)), "Falta revisar " + esperado);
            }
        }

        [Test]
        public void CadaControladorDeLaEscenaDeLaCiudad_SoloUsaClipsDePJ01()
        {
            Assume.That(File.Exists(RutaEscenaCiudad));
            var guids = new HashSet<string>();
            var regla = new Regex("guid: ([0-9a-f]{32})");
            bool siguiente = false;
            foreach (string linea in File.ReadLines(RutaEscenaCiudad))
            {
                if (linea.Contains("m_Controller:"))
                {
                    Match m = regla.Match(linea);
                    if (m.Success) { guids.Add(m.Groups[1].Value); }
                }
                else if (linea.Contains("propertyPath: m_Controller"))
                {
                    siguiente = true;
                }
                else if (siguiente && linea.Contains("objectReference"))
                {
                    siguiente = false;
                    Match m = regla.Match(linea);
                    if (m.Success) { guids.Add(m.Groups[1].Value); }
                }
            }
            Assert.IsNotEmpty(guids, "La escena usa controladores");
            var fallos = new List<string>();
            foreach (string guid in guids)
            {
                var c = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(guid));
                if (c == null) { continue; }
                string[] ajenos = ClipsAjenos(c);
                if (ajenos.Length > 0)
                {
                    fallos.Add(AssetDatabase.GUIDToAssetPath(guid) + ": " + string.Join(", ", ajenos));
                }
            }
            Assert.IsEmpty(fallos, string.Join("\n", fallos));
        }
    }
}

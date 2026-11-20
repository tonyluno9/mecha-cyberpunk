using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Personaje.Visual;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1482: la ciudad volvió a 4 barrios (se quitaron Malecón, Puerto, Autopista y Cerro). Rehace ConfigPeatonesZona_Base.asset con las zonas de tráfico que
    /// quedan (Avenida, NeonAlto, LasLomas, Deshuesadero) para que los NPC de las zonas quitadas salgan en los barrios. Fuera de toda caja de zona (Suburbios,
    /// Nova) salen de la lista general del prefab Peaton (ya trae todas las variantes NP07 a NP14).
    /// Menú: Mecha Cyberpunk/T-1482/Peatones por barrio. Por consola: -executeMethod MechaCyberpunk.Arte.Editor.PeatonesBarriosT1482.Aplicar.
    /// Escribe Logs/t1482-peatones.txt.
    /// </summary>
    public static class PeatonesBarriosT1482
    {
        public const string RutaInforme = "Logs/t1482-peatones.txt";

        private static readonly string[] EnTodas =
        {
            "NP13_Repartidor_a", "NP13_Repartidor_b", "NP14_Oficinista_a", "NP14_Oficinista_b",
        };

        /// <summary>
        /// El diseño: zona de tráfico que sigue en el mapa y las variantes propias de ella (más <see cref="EnTodas"/> en todas).
        /// Pescador y turista al Mercado (Avenida), corredor y turista a Neón Alto, abuela y elotero a las Lomas (suburbios), estibador al Deshuesadero.
        /// </summary>
        public static readonly (string zona, string[] propias)[] Diseno =
        {
            ("Avenida", new[] { "NP07_Pescador_a", "NP07_Pescador_b", "NP08_Turista_a", "NP10_Elotero_a", "NP12_Abuela_a" }),
            ("NeonAlto", new[] { "NP08_Turista_a", "NP08_Turista_b", "NP09_Corredor_a", "NP09_Corredor_b" }),
            ("LasLomas", new[] { "NP12_Abuela_a", "NP10_Elotero_a", "NP09_Corredor_a", "NP09_Corredor_b" }),
            ("Deshuesadero", new[] { "NP11_Estibador_a", "NP11_Estibador_b" }),
        };

        /// <summary>Las variantes de una zona del diseño con su peso (propias y de todas las zonas), o null si la zona no está.</summary>
        public static List<(string variante, float peso)> VariantesDe(string zona)
        {
            foreach ((string nombre, string[] propias) in Diseno)
            {
                if (nombre != zona)
                {
                    continue;
                }
                var lista = new List<(string, float)>();
                foreach (string v in propias) { lista.Add((v, PeatonesZonaT1409.PesoDeLaZona)); }
                foreach (string v in EnTodas) { lista.Add((v, PeatonesZonaT1409.PesoEnTodas)); }
                return lista;
            }
            return null;
        }

        [MenuItem("Mecha Cyberpunk/T-1482/Peatones por barrio")]
        public static void Aplicar()
        {
            var informe = new StringBuilder();
            try
            {
                var zonas = new List<ConfigPeatonesZona.Zona>();
                foreach ((string nombre, string[] _) in Diseno)
                {
                    var zona = new ConfigPeatonesZona.Zona { nombre = nombre, pesoPersona = PeatonesZonaT1409.PesoPersona };
                    foreach ((string variante, float peso) in VariantesDe(nombre))
                    {
                        GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaVisual(variante));
                        if (visual == null)
                        {
                            informe.AppendLine("  falta el visual " + variante + " (zona " + nombre + ")");
                            continue;
                        }
                        zona.entradas.Add(new ConfigPeatonesZona.Entrada { visual = visual, peso = peso });
                    }
                    zonas.Add(zona);
                    informe.AppendLine(nombre + ": " + zona.entradas.Count + " visuales + persona");
                }

                ConfigPeatonesZona config = AssetDatabase.LoadAssetAtPath<ConfigPeatonesZona>(PeatonesZonaT1409.RutaConfig);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<ConfigPeatonesZona>();
                    AssetDatabase.CreateAsset(config, PeatonesZonaT1409.RutaConfig);
                }
                config.Definir(zonas);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                GameObject contenido = PrefabUtility.LoadPrefabContents(PeatonesZonaT1409.RutaPeaton);
                try
                {
                    PeatonVariado variado = contenido.GetComponent<PeatonVariado>();
                    if (variado == null)
                    {
                        throw new InvalidOperationException("el prefab Peaton no tiene PeatonVariado");
                    }
                    variado.ConfigurarZona(config);
                    PrefabUtility.SaveAsPrefabAsset(contenido, PeatonesZonaT1409.RutaPeaton);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
                informe.AppendLine("Peaton.prefab conectado a " + PeatonesZonaT1409.RutaConfig);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                informe.AppendLine("ERROR: " + e);
                Debug.LogError("[Arte] T-1482: " + e.Message);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Arte] T-1482 peatones listo, informe en " + RutaInforme);
        }
    }
}

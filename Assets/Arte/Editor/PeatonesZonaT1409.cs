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
    /// T-1409: crea ConfigPeatonesZona_Base.asset con los pesos de cada zona y lo conecta al PeatonVariado del prefab Peaton.
    /// Menú: Mecha Cyberpunk/T-1409/Aplicar peatones por zona. Por consola: -executeMethod MechaCyberpunk.Arte.Editor.PeatonesZonaT1409.Aplicar.
    /// Escribe Logs/t1409-peatones-zona.txt.
    /// </summary>
    public static class PeatonesZonaT1409
    {
        public const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/ConfigPeatonesZona_Base.asset";
        public const string RutaPeaton = "Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab";
        public const string RutaInforme = "Logs/t1409-peatones-zona.txt";

        /// <summary>Peso de la persona de color de siempre en cada zona.</summary>
        public const float PesoPersona = 2f;
        /// <summary>Peso de los personajes propios de la zona (por variante).</summary>
        public const float PesoDeLaZona = 3f;
        /// <summary>Peso de los que salen en todas las zonas (NP13 y NP14, por variante).</summary>
        public const float PesoEnTodas = 1f;

        private static readonly string[] EnTodas =
        {
            "NP13_Repartidor_a", "NP13_Repartidor_b", "NP14_Oficinista_a", "NP14_Oficinista_b",
        };

        /// <summary>El diseño: zona y las variantes propias de ella (más <see cref="EnTodas"/> en todas). Los nombres casan con ZonaTrafico.</summary>
        public static readonly (string zona, string[] propias)[] Diseno =
        {
            ("Malecon", new[] { "NP07_Pescador_a", "NP07_Pescador_b", "NP08_Turista_a", "NP08_Turista_b", "NP09_Corredor_a", "NP09_Corredor_b" }),
            ("Puerto", new[] { "NP11_Estibador_a", "NP11_Estibador_b" }),
            ("Cerro", new[] { "NP10_Elotero_a", "NP12_Abuela_a" }),
            ("Autopista", new string[0]),
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
                foreach (string v in propias) { lista.Add((v, PesoDeLaZona)); }
                foreach (string v in EnTodas) { lista.Add((v, PesoEnTodas)); }
                return lista;
            }
            return null;
        }

        [MenuItem("Mecha Cyberpunk/T-1409/Aplicar peatones por zona")]
        public static void Aplicar()
        {
            var informe = new StringBuilder();
            try
            {
                var zonas = new List<ConfigPeatonesZona.Zona>();
                foreach ((string nombre, string[] _) in Diseno)
                {
                    var zona = new ConfigPeatonesZona.Zona { nombre = nombre, pesoPersona = PesoPersona };
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

                ConfigPeatonesZona config = AssetDatabase.LoadAssetAtPath<ConfigPeatonesZona>(RutaConfig);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<ConfigPeatonesZona>();
                    AssetDatabase.CreateAsset(config, RutaConfig);
                }
                config.Definir(zonas);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPeaton);
                try
                {
                    PeatonVariado variado = contenido.GetComponent<PeatonVariado>();
                    if (variado == null)
                    {
                        throw new InvalidOperationException("el prefab Peaton no tiene PeatonVariado");
                    }
                    variado.ConfigurarZona(config);
                    PrefabUtility.SaveAsPrefabAsset(contenido, RutaPeaton);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
                informe.AppendLine("Peaton.prefab conectado a " + RutaConfig);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                informe.AppendLine("ERROR: " + e);
                Debug.LogError("[Arte] T-1409: " + e.Message);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Arte] T-1409 listo, informe en " + RutaInforme);
        }
    }
}

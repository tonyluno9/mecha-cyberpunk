using System.IO;
using System.Text;
using MechaCyberpunk.Trafico.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1482: la ciudad volvió a 4 barrios (se quitaron Malecón, Puerto, Autopista y Cerro). Rehace ConfigCarrosZonas con la tabla nueva de
    /// <see cref="DisenoZonasTrafico"/> (solo Deshuesadero, NeonAlto, LasLomas y Avenida; las motos MT01/MT02 y V20/V21 van a los extras generales).
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.ZonasTraficoT1482.Aplicar
    /// Después de que la escena ya no tenga los ZonaTrafico_* de las zonas quitadas, .QuitarPrefabsViejos borra sus prefabs.
    /// </summary>
    public static class ZonasTraficoT1482
    {
        public const string RutaInforme = "Logs/t1482-trafico.txt";
        private static readonly string[] ZonasQuitadas = { "Malecon", "Puerto", "Autopista", "Cerro" };

        [MenuItem("Mecha Cyberpunk/T-1482/Carros por zona de los 4 barrios")]
        public static void Aplicar()
        {
            StringBuilder informe = new StringBuilder();
            informe.AppendLine(ZonasTraficoT1389.Crear());
            informe.AppendLine(ZonasTraficoT1389.CrearZonas());
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Trafico] T-1482:\n" + informe);
        }

        /// <summary>Borra ZonaTrafico_Malecon/Puerto/Autopista/Cerro.prefab. Solo cuando la escena ya no los usa.</summary>
        [MenuItem("Mecha Cyberpunk/T-1482/Quitar prefabs de zonas viejas")]
        public static void QuitarPrefabsViejos()
        {
            foreach (string nombre in ZonasQuitadas)
            {
                string ruta = ZonasTraficoT1345.RutaZona(nombre);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) != null)
                {
                    AssetDatabase.DeleteAsset(ruta);
                    Debug.Log("[Trafico] T-1482: borrado " + ruta);
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1512 parte A: los edificios ED y los muros MU de la tanda nuevos-19nov ya están copiados en Assets/Arte/Zonas/ED/ y MU/
    /// (FBX + basecolor, normal y metal-suave si el zip los traía). Este método configura los importadores, crea el material URP
    /// y el prefab de cada uno con la misma receta de T-1382 (escala uniforme a la medida, asentado en el piso, collider). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarEdificiosT1512.Aplicar
    /// </summary>
    public static class ImportarEdificiosT1512
    {
        public const string RutaInforme = "Logs/t1512-importar.txt";
        private const float SuavidadSinMapa = 0.3f;
        private const float MetalicoSinMapa = 0f;

        /// <summary>Entrada y variante de cada modelo identificado (ver docs/identificacion-nuevos-19nov.md). Medidas en metros.</summary>
        public static readonly (EntradaZonaT1382 entrada, string variante)[] Modelos =
        {
            (new EntradaZonaT1382("ED01_CasaSuburbios", ArteT250.Eje.Alto, 7f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("ED02_TienditaEsquina", ArteT250.Eje.Alto, 6f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("ED03_EdificioDeptos", ArteT250.Eje.Alto, 11f, TipoColliderZona.Caja), "b"),
            (new EntradaZonaT1382("ED04_LocalesMercado", ArteT250.Eje.Largo, 12f, TipoColliderZona.Caja), "b"),
            (new EntradaZonaT1382("ED05_EdificioMercado", ArteT250.Eje.Alto, 12f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("ED06_EdificioNeon", ArteT250.Eje.Alto, 14f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("ED07_HotelNeon", ArteT250.Eje.Alto, 18f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("ED08_OficinaNova", ArteT250.Eje.Alto, 22f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("ED08_OficinaNova", ArteT250.Eje.Alto, 20f, TipoColliderZona.Caja), "b"),
            (new EntradaZonaT1382("ED09_PlantaBajaNova", ArteT250.Eje.Largo, 13f, TipoColliderZona.Caja), "b"),
            (new EntradaZonaT1382("ED10_Bodega", ArteT250.Eje.Largo, 14f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("ED11_EdificioAbandonado", ArteT250.Eje.Alto, 11f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("ED12_Estacionamiento", ArteT250.Eje.Largo, 20f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("MU01_MuroPerimetral", ArteT250.Eje.Largo, 12f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("MU01_MuroPerimetral", ArteT250.Eje.Largo, 10f, TipoColliderZona.Caja), "b"),
            (new EntradaZonaT1382("MU02_TorreControl", ArteT250.Eje.Alto, 14f, TipoColliderZona.Caja), "a"),
            (new EntradaZonaT1382("MU03_PuertaMuro", ArteT250.Eje.Largo, 12f, TipoColliderZona.Caja), "a"),
        };

        [MenuItem("Mecha Cyberpunk/T-1512/Importar edificios ED y muros MU")]
        public static void Aplicar()
        {
            ImportarZonasT1382.Informe.Clear();
            ImportarZonasT1382.Informe.AppendLine("T-1512: tamano en metros (x y z), triangulos y collider de cada prefab ED/MU");
            AssetDatabase.Refresh();
            foreach ((EntradaZonaT1382 e, string v) in Modelos)
            {
                string carpeta = ImportarZonasT1382.RutaCarpeta(e, v);
                if (!File.Exists(carpeta + "/" + e.Nombre(v) + ".fbx"))
                {
                    ImportarZonasT1382.Informe.AppendLine(e.Nombre(v) + "\tFALTA FBX en " + carpeta);
                    continue;
                }
                ImportarZonasT1382.ConfigurarImportadores(e, v);
                Material material = ImportarZonasT1382.CrearMaterial(e, v);
                if (!File.Exists(carpeta + "/" + e.Nombre(v) + "_metalsuave.png"))
                {
                    // Sin mapa de metal el valor fijo de T-1382 (metal 1) lo dejaría negro.
                    material.SetFloat("_Metallic", MetalicoSinMapa);
                    material.SetFloat("_Smoothness", SuavidadSinMapa);
                    EditorUtility.SetDirty(material);
                }
                ImportarZonasT1382.Informe.AppendLine(ImportarZonasT1382.CrearPrefab(e, v, material));
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, ImportarZonasT1382.Informe.ToString());
            Debug.Log("[Arte] T-1512 importado:\n" + ImportarZonasT1382.Informe);
        }
    }
}

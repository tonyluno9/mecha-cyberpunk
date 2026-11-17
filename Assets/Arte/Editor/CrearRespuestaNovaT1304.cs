using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1304: arma el prefab del guardia robot de Nova que pelea (PandilleroArmado con el visual GuardiaNova y los datos de Nova) y la configuración
    /// IA/Resources/ConfigRespuestaNova con el guardia, el guardia pesado E12 y el jefe final E14. Se puede repetir.
    /// Batch: -executeMethod MechaCyberpunk.Arte.Editor.CrearRespuestaNovaT1304.Aplicar
    /// </summary>
    public static class CrearRespuestaNovaT1304
    {
        public const string RutaPrefabBase = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        public const string RutaGuardia = "Assets/_Proyecto/IA/Prefabs/GuardiaNova_Armado.prefab";
        public const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_GuardiaNova.prefab";
        public const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Nova.asset";
        public const string RutaPesado = "Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E12.prefab";
        public const string RutaJefe = "Assets/_Proyecto/IA/Prefabs/JefeFinalNova_E14.prefab";
        public const string CarpetaRecursos = "Assets/_Proyecto/IA/Resources";
        public const string RutaConfig = CarpetaRecursos + "/ConfigRespuestaNova.asset";

        [MenuItem("Mecha Cyberpunk/T1304/Crear respuesta de Nova")]
        public static void Aplicar()
        {
            CrearGuardia();
            if (!AssetDatabase.IsValidFolder(CarpetaRecursos))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/IA", "Resources");
            }
            ConfigRespuestaNova config = AssetDatabase.LoadAssetAtPath<ConfigRespuestaNova>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigRespuestaNova>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            GameObject guardia = AssetDatabase.LoadAssetAtPath<GameObject>(RutaGuardia);
            GameObject pesado = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPesado);
            GameObject jefe = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJefe);
            config.Configurar(guardia, pesado, jefe);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1304] ConfigRespuestaNova: guardia " + (guardia != null) + ", pesado " + (pesado != null) + ", jefe " + (jefe != null));
        }

        private static void CrearGuardia()
        {
            GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual);
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            if (visual == null || datos == null)
            {
                Debug.LogError("[T-1304] falta " + (visual == null ? RutaVisual : RutaDatos));
                return;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefabBase);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                MechaCyberpunk.Personaje.Visual.Editor.ConectarVisualesT520.Reemplazar(contenido, viejo, visual, "Modelo", true);
                contenido.name = "GuardiaNova_Armado";
                SerializedObject so = new SerializedObject(contenido.GetComponent<PandilleroAPie>());
                so.FindProperty("datos").objectReferenceValue = datos;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaGuardia);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Debug.Log("[T-1304] prefab " + RutaGuardia);
        }
    }
}

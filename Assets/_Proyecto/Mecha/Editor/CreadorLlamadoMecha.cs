using MechaCyberpunk.Mecha.Llamado;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma el llamado (T-066) en MechaJugador.prefab (y por herencia en MechaEstacionado.prefab): acción LlamarMecha (M) en el mapa
    /// "Llamado" de ControlesMecha, config, material de la marca y el componente LlamadoMecha. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorLlamadoMecha.Crear
    /// </summary>
    public static class CreadorLlamadoMecha
    {
        private const string RutaMecha = "Assets/_Proyecto/Mecha";
        private const string RutaPrefab = RutaMecha + "/Prefabs/MechaJugador.prefab";
        private const string RutaConfig = RutaMecha + "/Datos/ConfigLlamadoMecha_Base.asset";
        private const string RutaMaterialMarca = RutaMecha + "/Materiales/Llamado_Marca.mat";
        private const string MapaLlamado = "Llamado";
        private const string AccionLlamar = "LlamarMecha";

        [MenuItem("Mecha Cyberpunk/Crear T-066 llamado")]
        public static void Crear()
        {
            EntradaMechaEditor.AgregarAccion(AccionLlamar, "<Keyboard>/m", UnityEngine.InputSystem.InputActionType.Button, MapaLlamado);

            ConfigLlamadoMecha config = AssetDatabase.LoadAssetAtPath<ConfigLlamadoMecha>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigLlamadoMecha>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            Material marca = CrearMaterialMarca();
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                LlamadoMecha llamado = raiz.GetComponent<LlamadoMecha>();
                if (llamado == null)
                {
                    llamado = raiz.AddComponent<LlamadoMecha>();
                }
                SerializedObject so = new SerializedObject(llamado);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("llamar").objectReferenceValue = EntradaMechaEditor.Referencia(AccionLlamar);
                so.FindProperty("materialMarca").objectReferenceValue = marca;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-066 llamado creado.");
        }

        // Neón turquesa liso: URP Unlit.
        private static Material CrearMaterialMarca()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialMarca);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, RutaMaterialMarca);
            }
            material.SetColor("_BaseColor", new Color(0f, 1f, 0.9f));
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}

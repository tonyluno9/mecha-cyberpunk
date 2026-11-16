using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1013: crea los datos de las armas nuevas (subfusil, revólver, lanzagranadas, granada y rifle de plasma, con sus 4 niveles y sus balas en El Mercado)
    /// y la <see cref="ConfigArmasEspeciales"/> con la explosión del Particle Pack y el material del rayo de plasma.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.ArmasNuevasT1013.Crear
    /// </summary>
    public static class ArmasNuevasT1013
    {
        public const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/Resources/ConfigArmasEspeciales.asset";
        public const string RutaMaterialRayo = "Assets/_Proyecto/Personaje/Materiales/PlasmaRayo.mat";
        public const string RutaExplosion = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/SmallExplosion.prefab";
        private static readonly Color ColorRayoHdr = new Color(0.6f, 1.6f, 2.6f, 1f);

        [MenuItem("Mecha Cyberpunk/T-1013 Crear armas nuevas")]
        public static void Crear()
        {
            CreadorArmas.CrearDatos();
            CreadorMunicionArmas.Crear();
            CrearConfig();
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-1013: armas nuevas, sus balas y la config de armas especiales creadas.");
        }

        public static void CrearConfig()
        {
            ConfigArmasEspeciales config = AssetDatabase.LoadAssetAtPath<ConfigArmasEspeciales>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigArmasEspeciales>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("prefabExplosion").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaExplosion);
            so.FindProperty("materialRayo").objectReferenceValue = MaterialRayo();
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        // Partículas URP sin luz, aditivo y con color HDR para que el bloom lo haga brillar (el LineRenderer lo tiñe con sus colores).
        private static Material MaterialRayo()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialRayo);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(m, RutaMaterialRayo);
            }
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_BLENDMODE_ADD");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetColor("_BaseColor", ColorRayoHdr);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}

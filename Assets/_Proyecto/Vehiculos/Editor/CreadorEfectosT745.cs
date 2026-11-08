using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Vehiculos.Efectos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-745: prepara los efectos. (1) Pasa a URP los materiales del Unity Particle Pack que no lo eran (Particles/Unlit). (2) Crea los materiales
    /// del trazo de las balas y de la mancha del suelo. (3) Crea el CatalogoEfectos en Resources con los prefabs del pack que se usan.
    /// Se corre con -executeMethod MechaCyberpunk.Combate.Editor.CreadorEfectosT745.Crear (es seguro repetirlo).
    /// </summary>
    public static class CreadorEfectosT745
    {
        public const string CarpetaPack = "Assets/UnityTechnologies/ParticlePack";
        public const string RutaCatalogo = "Assets/_Proyecto/Vehiculos/Resources/CatalogoEfectos.asset";
        public const string RutaMaterialTrazo = "Assets/_Proyecto/Vehiculos/Resources/Trazo_URP.mat";
        public const string RutaMaterialChamusco = "Assets/_Proyecto/Vehiculos/Materiales/Chamusco_URP.mat";
        private const string Fuego = CarpetaPack + "/EffectExamples/Fire & Explosion Effects/Prefabs/";
        private const string Humo = CarpetaPack + "/EffectExamples/Smoke & Steam Effects/Prefabs/";
        private const string Misc = CarpetaPack + "/EffectExamples/Misc Effects/Prefabs/";
        private const string TexturaRedonda = CarpetaPack + "/EffectExamples/Fire & Explosion Effects/Textures/SmokePuff02.png";
        private const string ShaderUnlit = "Universal Render Pipeline/Particles/Unlit";

        [MenuItem("Mecha/Combate/Crear efectos T-745")]
        public static void Crear()
        {
            string informe = ConvertirMaterialesDelPack();
            Material trazo = CrearMaterial(RutaMaterialTrazo, null, new Color(1f, 0.85f, 0.45f, 1f), true);
            Material chamusco = CrearMaterial(RutaMaterialChamusco, AssetDatabase.LoadAssetAtPath<Texture2D>(TexturaRedonda), Color.white, false);
            CatalogoEfectos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoEfectos>(RutaCatalogo);
            if (catalogo == null)
            {
                System.IO.Directory.CreateDirectory("Assets/_Proyecto/Vehiculos/Resources");
                catalogo = ScriptableObject.CreateInstance<CatalogoEfectos>();
                AssetDatabase.CreateAsset(catalogo, RutaCatalogo);
            }
            catalogo.Asignar(Cargar(Fuego + "TinyExplosion.prefab"), Cargar(Fuego + "SmallExplosion.prefab"), Cargar(Fuego + "BigExplosion.prefab"),
                Cargar(Humo + "SmokeEffect.prefab"), Cargar(Fuego + "TinyFlames.prefab"), Cargar(Fuego + "MediumFlames.prefab"),
                Cargar(Misc + "SparksEffect.prefab"), chamusco);
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[T-745] Catálogo de efectos listo. " + informe);
        }

        private static GameObject Cargar(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                throw new System.Exception("No se encontró el prefab del pack: " + ruta);
            }
            return prefab;
        }

        /// <summary>True si el shader es de URP (o un Shader Graph), es decir, se ve bien en este proyecto.</summary>
        public static bool EsShaderUrp(Shader shader)
        {
            if (shader == null)
            {
                return false;
            }
            string n = shader.name;
            return n.StartsWith("Universal Render Pipeline/") || n.StartsWith("Shader Graphs/") || n.StartsWith("TextMeshPro/") || n.StartsWith("Skybox/");
        }

        /// <summary>Pasa a Particles/Unlit URP los materiales del pack con otro shader. Devuelve cuántos y cuáles.</summary>
        public static string ConvertirMaterialesDelPack()
        {
            StringBuilder sb = new StringBuilder();
            int convertidos = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { CarpetaPack + "/EffectExamples" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (mat == null || EsShaderUrp(mat.shader))
                {
                    continue;
                }
                string viejo = mat.shader != null ? mat.shader.name : "(sin shader)";
                Texture textura = Primera(mat, "_MainTex", "_BaseMap");
                Color color = mat.HasProperty("_TintColor") ? mat.GetColor("_TintColor") : mat.HasProperty("_Color") ? mat.GetColor("_Color") : mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
                bool aditivo = viejo.IndexOf("Additive", System.StringComparison.OrdinalIgnoreCase) >= 0 || mat.name.IndexOf("Flame", System.StringComparison.OrdinalIgnoreCase) >= 0;
                Shader urp = Shader.Find(ShaderUnlit);
                mat.shader = urp;
                Configurar(mat, textura, color, aditivo);
                EditorUtility.SetDirty(mat);
                convertidos++;
                sb.Append(mat.name).Append(" (").Append(viejo).Append("), ");
            }
            AssetDatabase.SaveAssets();
            return "Materiales convertidos a URP: " + convertidos + (convertidos > 0 ? " -> " + sb : "");
        }

        private static Texture Primera(Material mat, params string[] propiedades)
        {
            foreach (string p in propiedades)
            {
                if (mat.HasProperty(p) && mat.GetTexture(p) != null)
                {
                    return mat.GetTexture(p);
                }
            }
            return null;
        }

        private static Material CrearMaterial(string ruta, Texture textura, Color color, bool aditivo)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ruta));
                mat = new Material(Shader.Find(ShaderUnlit));
                AssetDatabase.CreateAsset(mat, ruta);
            }
            mat.shader = Shader.Find(ShaderUnlit);
            Configurar(mat, textura, color, aditivo);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Particles/Unlit transparente: aditivo (luz) o mezcla normal (mancha). Los valores son los que escribe el inspector de URP.
        private static void Configurar(Material mat, Texture textura, Color color, bool aditivo)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", aditivo ? 2f : 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(aditivo ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)(aditivo ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", color);
            if (textura != null)
            {
                mat.SetTexture("_BaseMap", textura);
            }
        }
    }
}

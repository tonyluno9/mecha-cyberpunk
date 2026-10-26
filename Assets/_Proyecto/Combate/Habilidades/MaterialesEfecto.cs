using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Combate.Habilidades
{
    /// <summary>
    /// Materiales compartidos de los efectos greybox (creados en ejecución, no son assets). El color va por MaterialPropertyBlock,
    /// así que ningún efecto cambia un material.
    /// </summary>
    public static class MaterialesEfecto
    {
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColorEmision = Shader.PropertyToID("_EmissionColor");
        private static Material opaco;
        private static Material transparente;

        public static Material Opaco => opaco != null ? opaco : (opaco = Crear(false));
        public static Material Transparente => transparente != null ? transparente : (transparente = Crear(true));

        private static Material Crear(bool alfa)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            Material material = new Material(shader) { name = alfa ? "EfectoTransparente" : "EfectoOpaco", hideFlags = HideFlags.DontSave };
            if (alfa)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_Cull", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            return material;
        }

        /// <summary>Pinta un renderer con un color (y su alfa) sin tocar el material.</summary>
        public static void Pintar(Renderer renderer, Color color, MaterialPropertyBlock bloque)
        {
            renderer.GetPropertyBlock(bloque);
            bloque.SetColor(IdColorBase, color);
            bloque.SetColor(IdColorEmision, color);
            renderer.SetPropertyBlock(bloque);
        }
    }
}

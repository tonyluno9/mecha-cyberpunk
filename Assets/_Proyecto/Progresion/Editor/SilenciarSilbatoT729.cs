using MechaCyberpunk.Progresion.Sonido;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// T-729: quita el "silbato" de la policía. El único silbido que suena con la policía es el de la bala que roza al jugador cuando tira el dron (ARM_BALA_ROZA, un barrido agudo
    /// de 3200 Hz en el provisional): se apaga en el catálogo de sonidos (interruptor). Los disparos, las sirenas y el zumbido de la bala (ARM_BALA_ZUMBIDO) siguen igual.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Editor.SilenciarSilbatoT729.Aplicar
    /// </summary>
    public static class SilenciarSilbatoT729
    {
        public const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";
        public static readonly string[] Silbatos = { "ARM_BALA_ROZA" };

        [MenuItem("Mecha Cyberpunk/T-729/Quitar silbato de la policia")]
        public static void Aplicar()
        {
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            if (catalogo == null)
            {
                Debug.LogError("[Progresion] T-729: falta " + RutaCatalogo);
                return;
            }
            foreach (string id in Silbatos)
            {
                catalogo.FijarEncendido(id, false);
            }
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-729: sonidos apagados: " + string.Join(", ", Silbatos));
        }
    }
}

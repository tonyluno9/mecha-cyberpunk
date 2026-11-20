using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1528: activa "provoca antes de atacar" en los datos de los SKULLS y SNAKES de calle (Calaveras y Viboras; no las variantes de misión E10/E19E20, ni jefes ni Nova).
    /// Idempotente. Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.IA.Editor.AplicarMaleantesCalleT1528.Aplicar
    /// </summary>
    public static class AplicarMaleantesCalleT1528
    {
        private static readonly string[] Rutas =
        {
            "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset",
            "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset"
        };

        [MenuItem("Mecha Cyberpunk/T1528/Maleantes de calle provocan primero")]
        public static void Aplicar()
        {
            foreach (string ruta in Rutas)
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(ruta);
                if (asset == null)
                {
                    Debug.LogError("[T-1528] falta " + ruta);
                    continue;
                }
                SerializedObject so = new SerializedObject(asset);
                SerializedProperty p = so.FindProperty("provocaAntesDeAtacar");
                if (p == null)
                {
                    Debug.LogError("[T-1528] " + ruta + " no tiene provocaAntesDeAtacar");
                    continue;
                }
                bool antes = p.boolValue;
                p.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                Debug.Log("[T-1528] " + asset.name + ": provocaAntesDeAtacar " + antes + " -> true");
            }
            AssetDatabase.SaveAssets();
        }
    }
}

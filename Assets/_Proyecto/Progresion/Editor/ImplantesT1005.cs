using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// T-1005 (playtest 16-nov): el EXO DE PIERNAS I cuesta 0 (el Doc "lo regala": lo compras tú, gratis) y el nivel II de cualquier prótesis
    /// se abre en cuanto tienes el nivel I, sin requisito de liga. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Editor.ImplantesT1005.Aplicar
    /// </summary>
    public static class ImplantesT1005
    {
        public const string IdExoPiernasI = "implante_piernas_1";
        public const int PrecioGratis = 0;
        public const int LigaSinRequisito = 1;

        [MenuItem("Mecha Cyberpunk/Progresion/T-1005 Exo de piernas gratis y nivel II sin liga")]
        public static void Aplicar()
        {
            string[] guids = AssetDatabase.FindAssets("t:DatosImplante", new[] { CreadorImplantes.Carpeta });
            foreach (string guid in guids)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                DatosImplante d = AssetDatabase.LoadAssetAtPath<DatosImplante>(ruta);
                if (d == null)
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(d);
                if (d.Id == IdExoPiernasI)
                {
                    so.FindProperty("precio").intValue = PrecioGratis;
                }
                if (d.Nivel >= 2)
                {
                    so.FindProperty("ligaMinima").intValue = LigaSinRequisito;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(d);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-1005: exo de piernas I gratis y nivel II sin liga.");
        }
    }
}

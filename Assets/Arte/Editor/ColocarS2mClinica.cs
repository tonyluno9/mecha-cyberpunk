using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-896 (C3): hornea el holograma de la clínica desde PJ01_Implantes.fbx: el cuerpo (PJ01_Malla, 4 submallas por espacio) y cada implante
    /// (IMP01 piernas, IMP02 brazos, IMP03 visor, niveles 1 y 2), todos en la misma pose y espacio, y los pone en la config.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2mClinica.Hornear
    /// </summary>
    public static class ColocarS2mClinica
    {
        private const string RutaFbx = "Assets/Arte/Personajes/PJ01/Implantes/PJ01_Implantes.fbx";
        private const string NombreCuerpo = "PJ01_Malla";

        private static readonly (string Nombre, EspacioImplante Espacio, int Nivel)[] Piezas =
        {
            ("IMP01_LVL1", EspacioImplante.Piernas, 1), ("IMP01_LVL2", EspacioImplante.Piernas, 2),
            ("IMP02_LVL1", EspacioImplante.Brazos, 1), ("IMP02_LVL2", EspacioImplante.Brazos, 2),
            ("IMP03_LVL1", EspacioImplante.Ojos, 1), ("IMP03_LVL2", EspacioImplante.Ojos, 2),
        };

        [MenuItem("Mecha Cyberpunk/S2m/4 Holograma de la clínica con PJ01")]
        public static void Hornear()
        {
            GameObject copia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx));
            try
            {
                copia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Dictionary<string, SkinnedMeshRenderer> porNombre = new Dictionary<string, SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer sk in copia.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    porNombre[sk.name] = sk;
                }

                Mesh cuerpo = ImportarS2nCuerpoHolograma.HornearDe(new[] { porNombre[NombreCuerpo] },
                    ImportarS2nCuerpoHolograma.CarpetaDestino + "CuerpoHolograma_PJ01.asset");

                ConfigPantallaClinica config = AssetDatabase.LoadAssetAtPath<ConfigPantallaClinica>(ImportarS2nCuerpoHolograma.RutaConfig);
                SerializedObject so = new SerializedObject(config);
                SerializedProperty cuerpos = so.FindProperty("cuerpos");
                for (int i = 0; i < cuerpos.arraySize; i++)
                {
                    cuerpos.GetArrayElementAtIndex(i).objectReferenceValue = cuerpo;
                }
                SerializedProperty lista = so.FindProperty("mallasImplante");
                lista.arraySize = Piezas.Length;
                for (int i = 0; i < Piezas.Length; i++)
                {
                    SerializedProperty e = lista.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("Espacio").enumValueIndex = (int)Piezas[i].Espacio;
                    e.FindPropertyRelative("Nivel").intValue = Piezas[i].Nivel;
                    e.FindPropertyRelative("Malla").objectReferenceValue = ImportarS2nCuerpoHolograma.HornearPieza(porNombre[Piezas[i].Nombre],
                        ImportarS2nCuerpoHolograma.CarpetaDestino + "ImplanteHolograma_" + Piezas[i].Espacio + "_" + Piezas[i].Nivel + ".asset");
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                Object.DestroyImmediate(copia);
            }
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// T-1529: pone en los DatosImplante existentes la vida máxima extra que da cada implante instalado (nivel I +10, nivel II +25, ver
    /// <see cref="ReglaImplantes.VidaMaximaExtra"/>). Idempotente. Batchmode:
    /// -executeMethod MechaCyberpunk.Progresion.Editor.VidaImplantesT1529.Diagnostico  (solo informa)
    /// -executeMethod MechaCyberpunk.Progresion.Editor.VidaImplantesT1529.Aplicar
    /// </summary>
    public static class VidaImplantesT1529
    {
        private const string Campo = "efectos.vidaMaximaExtra";

        [MenuItem("Mecha Cyberpunk/T-1529/Diagnóstico vida de implantes")]
        public static void Diagnostico() => Recorrer(false);

        [MenuItem("Mecha Cyberpunk/T-1529/Aplicar vida de implantes")]
        public static void Aplicar() => Recorrer(true);

        private static void Recorrer(bool aplicar)
        {
            string[] guids = AssetDatabase.FindAssets("t:DatosImplante", new[] { CreadorImplantes.Carpeta });
            int cambiados = 0;
            foreach (string guid in guids)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var d = AssetDatabase.LoadAssetAtPath<DatosImplante>(ruta);
                if (d == null)
                {
                    continue;
                }
                float debe = ReglaImplantes.VidaMaximaExtra(d.Nivel);
                var so = new SerializedObject(d);
                SerializedProperty p = so.FindProperty(Campo);
                if (p == null)
                {
                    Debug.LogError("[T-1529] " + d.Id + ": no existe el campo " + Campo + " (compila primero).");
                    continue;
                }
                bool igual = Mathf.Abs(p.floatValue - debe) < 1e-4f;
                Debug.Log("[T-1529] " + d.Id + " nivel " + d.Nivel + ": vida extra " + p.floatValue + (igual ? " (bien)" : " -> debe ser " + debe));
                if (!igual && aplicar)
                {
                    p.floatValue = debe;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(d);
                    cambiados++;
                }
            }
            if (aplicar)
            {
                AssetDatabase.SaveAssets();
            }
            Debug.Log("[T-1529] implantes revisados: " + guids.Length + ", cambiados: " + cambiados);
        }
    }
}

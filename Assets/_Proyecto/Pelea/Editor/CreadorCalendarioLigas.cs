using MechaCyberpunk.Economia;
using MechaCyberpunk.Economia.Editor;
using MechaCyberpunk.Pelea.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Editor
{
    /// <summary>
    /// Crea los presentadores de liga y de jefe y CalendarioLigas_Base.asset con los 20 carteles (T-158, D-tony-98). Idempotente: un
    /// asset que ya existe no se toca. Necesita los prefabs RivalLiga (T-157) y las apuestas (CreadorApuestasLiga). Carga el IA por ruta.
    /// Batchmode: -executeMethod MechaCyberpunk.Pelea.Editor.CreadorCalendarioLigas.Crear
    /// </summary>
    public static class CreadorCalendarioLigas
    {
        public const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        public const string RutaPresentadorLiga = "Assets/_Proyecto/Pelea/Datos/ConfigPresentador_Liga.asset";
        public const string RutaPresentadorJefe = "Assets/_Proyecto/Pelea/Datos/ConfigPresentador_Jefe.asset";

        private const int RivalesNormales = 4;
        private const int IndiceGritoLiga = 1;
        private const int IndiceGritoJefe = 2;

        private static readonly string[] LineasLiga = { "LIGA {liga} DEL CLANDESTINO", "¡{alias} CONTRA {rival}!" };
        private static readonly string[] LineasJefe =
        {
            "¡NOCHE DE CAMPEONES EN LA LIGA {liga}!", "EL CAMPEÓN... ¡{rival}!", "EL RETADOR... ¡¡{alias}!!"
        };

        // Premios de ganar por liga: 4 rivales y el jefe (D-tony-98).
        private static readonly int[][] Ganar =
        {
            new[] { 600, 700, 800, 900, 2000 },
            new[] { 1000, 1100, 1200, 1300, 3000 },
            new[] { 1500, 1650, 1800, 1950, 4500 },
            new[] { 2200, 2400, 2600, 2800, 7500 },
        };
        private static readonly int[] Perder = { 0, 100, 150, 200 };

        private static ConfigPresentador CrearPresentador(string ruta, string[] lineas, int indiceGrito)
        {
            var existente = AssetDatabase.LoadAssetAtPath<ConfigPresentador>(ruta);
            if (existente != null) return existente;
            var config = ScriptableObject.CreateInstance<ConfigPresentador>();
            AssetDatabase.CreateAsset(config, ruta);
            var so = new SerializedObject(config);
            SerializedProperty prop = so.FindProperty("lineas");
            prop.arraySize = lineas.Length;
            for (int i = 0; i < lineas.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = lineas[i];
            so.FindProperty("indiceGrito").intValue = indiceGrito;
            so.FindProperty("presentarEnRevancha").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        [MenuItem("Mecha Cyberpunk/Crear T-158 calendario de ligas")]
        public static void Crear()
        {
            ConfigPresentador presentadorLiga = CrearPresentador(RutaPresentadorLiga, LineasLiga, IndiceGritoLiga);
            ConfigPresentador presentadorJefe = CrearPresentador(RutaPresentadorJefe, LineasJefe, IndiceGritoJefe);

            if (AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario) == null)
            {
                var calendario = ScriptableObject.CreateInstance<CalendarioLigas>();
                AssetDatabase.CreateAsset(calendario, RutaCalendario);
                var so = new SerializedObject(calendario);
                SerializedProperty ligas = so.FindProperty("ligas");
                ligas.arraySize = Ganar.Length;
                for (int l = 1; l <= Ganar.Length; l++)
                {
                    SerializedProperty carteles = ligas.GetArrayElementAtIndex(l - 1).FindPropertyRelative("carteles");
                    carteles.arraySize = Ganar[l - 1].Length;
                    for (int n = 1; n <= Ganar[l - 1].Length; n++)
                    {
                        bool jefe = n > RivalesNormales;
                        SerializedProperty c = carteles.GetArrayElementAtIndex(n - 1);
                        c.FindPropertyRelative("prefabRival").objectReferenceValue =
                            AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga{l}_{n}.prefab");
                        c.FindPropertyRelative("premioGanar").intValue = Ganar[l - 1][n - 1];
                        c.FindPropertyRelative("premioPerder").intValue = Perder[l - 1];
                        c.FindPropertyRelative("apuesta").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosApuestaPelea>(
                            CreadorApuestasLiga.Ruta((jefe ? "Jefe" : "Liga") + l));
                        c.FindPropertyRelative("presentador").objectReferenceValue = jefe ? presentadorJefe : presentadorLiga;
                        c.FindPropertyRelative("liga").intValue = l;
                        c.FindPropertyRelative("esJefe").boolValue = jefe;
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(calendario);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Pelea] T-158 calendario de ligas creado.");
        }
    }
}

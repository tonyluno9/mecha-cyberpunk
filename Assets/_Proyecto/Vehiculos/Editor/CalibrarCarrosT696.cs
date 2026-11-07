using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-696: carros que frenan más rápido y aceleran más vivo (la reversa y la dirección no se tocan) y el chisporroteo del aviso de fuego
    /// encendido en el catálogo de sonidos. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CalibrarCarrosT696.Calibrar
    /// </summary>
    public static class CalibrarCarrosT696
    {
        private const string RutaConfig = "Assets/_Proyecto/Vehiculos/Datos/ConfigCarro_Viejo.asset";
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";
        public const float FuerzaMotor = 8800f;
        public const float FuerzaFreno = 20000f;
        public const float FuerzaFrenoMano = 9000f;

        [MenuItem("Mecha Cyberpunk/T-696/Calibrar carros")]
        public static void Calibrar()
        {
            SerializedObject so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(RutaConfig));
            // Motor: 6200 a 8800 N (5.6 a 8 m/s² con 1100 kg, como la moto de 7.8). Freno: 12500 a 20000 N (11 a 18 m/s²). La reversa, el giro y el agarre no cambian.
            so.FindProperty("fuerzaMotor").floatValue = FuerzaMotor;
            so.FindProperty("fuerzaFreno").floatValue = FuerzaFreno;
            so.FindProperty("fuerzaFrenoMano").floatValue = FuerzaFrenoMano;
            so.ApplyModifiedPropertiesWithoutUndo();

            // El aviso de fuego usa MUN_CHISPAS_ELECTRICAS (ya está en el catálogo con su clip); se enciende su interruptor.
            Object catalogo = AssetDatabase.LoadMainAssetAtPath(RutaCatalogo); // sin referenciar Progresion: solo se toca un campo serializado
            SerializedObject soCatalogo = new SerializedObject(catalogo);
            SerializedProperty lista = soCatalogo.FindProperty("interruptores");
            bool encontrado = false;
            for (int i = 0; i < lista.arraySize; i++)
            {
                SerializedProperty entrada = lista.GetArrayElementAtIndex(i);
                if (entrada.FindPropertyRelative("id").stringValue == "MUN_CHISPAS_ELECTRICAS")
                {
                    entrada.FindPropertyRelative("encendido").boolValue = true;
                    encontrado = true;
                }
            }
            if (!encontrado && lista.arraySize > 0)
            {
                lista.arraySize++;
                SerializedProperty nueva = lista.GetArrayElementAtIndex(lista.arraySize - 1);
                nueva.FindPropertyRelative("id").stringValue = "MUN_CHISPAS_ELECTRICAS";
                nueva.FindPropertyRelative("encendido").boolValue = true;
            }
            soCatalogo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t696-calibrar.txt", "motor " + FuerzaMotor + " freno " + FuerzaFreno + " mano " + FuerzaFrenoMano + " chispas encendido");
            Debug.Log("[Vehiculos] T-696: carros calibrados y chispas encendidas.");
        }
    }
}

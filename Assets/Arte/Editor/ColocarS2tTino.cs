using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-865 (playtest 6-nov tarde): Tino un poco más grande y con su frase ("El arma que quieras, la mejoramos; acércate al banco"). El menú
    /// de mejora ya se arma al entrar a la escena (BancoArmas.Start), así abre al instante. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2tTino.Colocar
    /// </summary>
    public static class ColocarS2tTino
    {
        public const string RutaTino = "GTA_T417/Apoyo_TINO";
        public const string RutaDatos = "Assets/_Proyecto/Progresion/Apoyo/Datos/DatosPersonajeApoyo_inventor.asset";
        public const string Frase = "El arma que quieras, la mejoramos. Acércate al banco.";
        /// <summary>Escala del modelo de Tino (antes 1.49, como los otros NPC): un 12 % más grande.</summary>
        public const float EscalaModelo = 1.67f;

        [MenuItem("Mecha Cyberpunk/S2t/12 Tino")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            Object datos = AssetDatabase.LoadMainAssetAtPath(RutaDatos);
            SerializedObject so = new SerializedObject(datos);
            SerializedProperty frases = so.FindProperty("frases");
            if (frases.arraySize == 0)
            {
                frases.InsertArrayElementAtIndex(0);
            }
            frases.GetArrayElementAtIndex(0).stringValue = Frase;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            ColocarS2.Abrir();
            Transform modelo = ColocarS2.Buscar(RutaTino + "/Visual/Giro/Modelo");
            if (modelo != null)
            {
                modelo.localScale = Vector3.one * EscalaModelo;
            }
            ColocarS2.Informe.AppendLine("Tino: " + Frase + "; modelo a " + EscalaModelo);
            ColocarS2.Guardar("s2t-tino");
        }

        public static void Fotos()
        {
            ColocarS2.Abrir();
            Transform t = ColocarS2.Buscar(RutaTino);
            Vector3 c = t.position;
            ColocarS2t.FotoHora(22f, c + t.forward * 3.2f + Vector3.up * 1.6f, c + Vector3.up * 1.1f, ColocarS2t.CarpetaCapturas + "s2t-tino.png", 60f);
        }
    }
}

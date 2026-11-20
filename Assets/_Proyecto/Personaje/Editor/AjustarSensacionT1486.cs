using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1486: escribe en Resources/DatosSensacion.asset el peso de cada arma (los 10 valores de <see cref="DatosSensacion.AjustesPorArma"/>:
    /// patada, retroceso del arma, temblor, casquillos, floración). Conserva el destello ya ajustado en T-676/T-678. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarSensacionT1486.Ajustar
    /// </summary>
    public static class AjustarSensacionT1486
    {
        private const string RutaDatos = "Assets/_Proyecto/Personaje/Datos/Resources/DatosSensacion.asset";

        [MenuItem("Mecha Cyberpunk/Ajustar T-1486 peso del disparo")]
        public static void Ajustar()
        {
            Object datos = AssetDatabase.LoadAssetAtPath<Object>(RutaDatos);
            if (datos == null)
            {
                Debug.LogError("[Personaje] Falta " + RutaDatos);
                return;
            }
            AjusteSensacion[] valores = DatosSensacion.AjustesPorArma();
            SerializedObject so = new SerializedObject(datos);
            SerializedProperty armas = so.FindProperty("armas");
            int tamanoAnterior = armas.arraySize;
            armas.arraySize = valores.Length;
            for (int i = 0; i < valores.Length; i++)
            {
                SerializedProperty e = armas.GetArrayElementAtIndex(i);
                AjusteSensacion v = valores[i];
                e.FindPropertyRelative("patada").floatValue = v.patada;
                e.FindPropertyRelative("desvio").floatValue = v.desvio;
                if (i >= tamanoAnterior) // el destello de las cinco primeras ya está ajustado (T-676, T-678)
                {
                    e.FindPropertyRelative("destello").floatValue = v.destello;
                    e.FindPropertyRelative("particulas").intValue = v.particulas;
                }
                e.FindPropertyRelative("retrocesoArma").floatValue = v.retrocesoArma;
                e.FindPropertyRelative("duracionRetrocesoArma").floatValue = v.duracionRetrocesoArma;
                e.FindPropertyRelative("recuperacion").floatValue = v.recuperacion;
                e.FindPropertyRelative("impulso").floatValue = v.impulso;
                e.FindPropertyRelative("casquillos").intValue = v.casquillos;
                e.FindPropertyRelative("floracion").floatValue = v.floracion;
                e.FindPropertyRelative("topeFloracion").floatValue = v.topeFloracion;
                e.FindPropertyRelative("recuperacionFloracion").floatValue = v.recuperacionFloracion;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] DatosSensacion: peso de " + valores.Length + " armas (T-1486).");
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1324: ajusta los datos de armas del playtest: el sniper hace zoom mucho más cerrado (FOV 8) y cuesta más; el rifle de plasma pega mucho más fuerte.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarArmasT1324.Aplicar
    /// </summary>
    public static class AjustarArmasT1324
    {
        public const string RutaCamara = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";
        public const string RutaSniperBase = "Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_sniper_0.asset";
        public const string RutaOfertaSniper = "Assets/_Proyecto/Economia/Datos/DatosOferta_Arma_sniper_0.asset";
        public const string PrefijoPlasma = "Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_rifleplasma_";
        public const float FovSniper = 8f;
        public const int PrecioSniper = 2200;
        public static readonly float[] DanioPlasma = { 400f, 470f, 550f, 650f };

        [MenuItem("Mecha Cyberpunk/Aplicar T-1324 armas")]
        public static void Aplicar()
        {
            Poner(RutaCamara, "fovSniper", FovSniper, 0);
            Poner(RutaSniperBase, "precio", 0f, PrecioSniper);
            Poner(RutaOfertaSniper, "precio", 0f, PrecioSniper);
            for (int nivel = 0; nivel < DanioPlasma.Length; nivel++)
            {
                Poner(PrefijoPlasma + nivel + ".asset", "danio", DanioPlasma[nivel], 0);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-1324: sniper FOV " + FovSniper + " y precio " + PrecioSniper + "; plasma " + DanioPlasma[0] + " de dano por segundo.");
        }

        // Pone un float (si entero == 0) o un entero en un campo serializado del asset.
        private static void Poner(string ruta, string campo, float valor, int entero)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(ruta);
            if (asset == null)
            {
                Debug.LogError("[Personaje] T-1324: falta " + ruta);
                return;
            }
            SerializedObject so = new SerializedObject(asset);
            SerializedProperty p = so.FindProperty(campo);
            if (p == null)
            {
                Debug.LogError("[Personaje] T-1324: " + ruta + " no tiene el campo " + campo);
                return;
            }
            if (p.propertyType == SerializedPropertyType.Integer)
            {
                p.intValue = entero;
            }
            else
            {
                p.floatValue = valor;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }
    }
}

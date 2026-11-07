using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-882 (playtest 7-nov, sesión 2): charcos más nítidos, con borde mojado y menos espejo plano. El reflejo planar sale a resolución
    /// completa (antes a la mitad: borroso), las máscaras de los charcos traen en G el borde mojado que oscurece el asfalto alrededor, y el
    /// shader Charco varía las ondas. Idempotente: las máscaras se reescriben en su misma ruta (mismo GUID).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2oCharcos.Colocar
    /// </summary>
    public static class ColocarS2oCharcos
    {
        public const int Formas = 4;
        public const float Resolucion = 1f;
        public const float Ondas = 0.0045f;
        public const float FresnelMin = 0.3f;
        /// <summary>Más fuerte: de noche la calle es oscura y el reflejo se perdía; en K01 los charcos reflejan las luces con fuerza.</summary>
        public const float Intensidad = 1.3f;

        [MenuItem("Mecha Cyberpunk/S2o/3 Charcos nítidos con borde mojado")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ConfigCharcos config = AssetDatabase.LoadAssetAtPath<ConfigCharcos>(ColocarS2cLook.RutaConfigCharcos);
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("escalaResolucion").floatValue = Resolucion;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            for (int i = 0; i < Formas; i++)
            {
                KitS2c.TexCharco(i, true);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(KitS2c.Carpeta + "/S2c_Charco_" + i + ".mat");
                if (m == null)
                {
                    continue;
                }
                m.SetFloat("_Ondas", Ondas);
                m.SetFloat("_FresnelMin", FresnelMin);
                m.SetFloat("_Intensidad", Intensidad);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("Charcos: reflejo a resolución " + Resolucion + ", " + Formas + " máscaras con borde mojado, ondas " + Ondas);
            ColocarS2.Guardar("s2o-charcos");
        }

        /// <summary>De cerca, de noche, los charcos de la calle de prueba (docs/capturas/s2o-charcos-*.png).</summary>
        public static void Fotos(string cuando)
        {
            float suelo = KitS2c.Suelo(new Vector3(-95f, 0f, ColocarS2cCalle.ZCentro), 0.2f);
            ColocarS2cLook.Foto(new Vector3(-62f, 1.7f + suelo, ColocarS2cCalle.ZCentro - 0.5f), new Vector3(-70f, suelo, ColocarS2cCalle.ZCentro), "docs/capturas/s2o-charcos-cerca-" + cuando + ".png", 60f);
            ColocarS2cLook.Foto(ColocarS2cCalle.CamaraFoto + Vector3.up * suelo, ColocarS2cCalle.MiraFoto + Vector3.up * suelo, "docs/capturas/s2o-charcos-calle-" + cuando + ".png", ColocarS2cCalle.CampoFoto);
        }
    }
}

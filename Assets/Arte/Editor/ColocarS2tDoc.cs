using System.Linq;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Apoyo;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-864 (playtest 6-nov tarde): el Doc. La silla te sienta también sano (revisión gratis; el código está en SillaClinica) con el aviso de
    /// ConfigSillaClinica_Base; el Doc te regala un implante (EXO DE PIERNAS I) la primera vez que hablas con él sin implantes
    /// (<see cref="RegaloImplanteDoc"/> en Apoyo_JOEL). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2tDoc.Colocar
    /// </summary>
    public static class ColocarS2tDoc
    {
        public const string RutaDoc = "GTA_T417/Apoyo_JOEL";
        public const string RutaConfigSilla = "Assets/_Proyecto/Progresion/Apoyo/Datos/ConfigSillaClinica_Base.asset";
        public const string RutaRegalo = "Assets/_Proyecto/Progresion/Datos/Implantes/DatosImplante_implante_piernas_1.asset";
        public const string RutaLocal = "T531_Locales/IDI";
        public static readonly string[] MurosDeFuera = { "MuroFondo", "MuroIzq", "MuroDer", "Techo" };
        public const string TextoSano = "SENTARTE · REVISIÓN GRATIS";
        public const string FraseDoc = "Siéntate en la silla, mijo; ahí te reviso.";

        [MenuItem("Mecha Cyberpunk/S2t/11 Doc: silla y regalo de implante")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            Object config = AssetDatabase.LoadMainAssetAtPath(RutaConfigSilla);
            if (config != null)
            {
                SerializedObject so = new SerializedObject(config);
                so.FindProperty("textoSano").stringValue = TextoSano;
                SerializedProperty frase = so.FindProperty("fraseDoc");
                if (frase != null)
                {
                    frase.stringValue = FraseDoc;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
            ColocarS2.Abrir();
            Transform doc = ColocarS2.Buscar(RutaDoc);
            if (doc == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaDoc);
                ColocarS2.Guardar("s2t-doc");
                return;
            }
            // Por fuera, el consultorio era solo el forro (planos de una cara que desde fuera no se ven): se prenden los muros del local T531.
            Transform idi = ColocarS2.Buscar(RutaLocal);
            if (idi != null)
            {
                foreach (string m in MurosDeFuera)
                {
                    Transform muro = idi.Find(m);
                    if (muro == null)
                    {
                        continue;
                    }
                    muro.gameObject.SetActive(true);
                    Renderer rm = muro.GetComponent<Renderer>();
                    if (rm != null)
                    {
                        rm.enabled = true;
                    }
                    if (muro.GetComponent<Collider>() == null)
                    {
                        muro.gameObject.AddComponent<BoxCollider>();
                    }
                    ColocarS2.Informe.AppendLine("muro de fuera del consultorio: " + m + (rm != null ? " " + rm.bounds.size.ToString("F2") : ""));
                }
            }
            RegaloImplanteDoc regalo = doc.GetComponent<RegaloImplanteDoc>() ?? doc.gameObject.AddComponent<RegaloImplanteDoc>();
            regalo.Configurar(doc.GetComponent<PersonajeApoyo>(), AssetDatabase.LoadAssetAtPath<DatosImplante>(RutaRegalo));
            EditorUtility.SetDirty(regalo);
            ColocarS2.Informe.AppendLine("Doc con su regalo de implante; silla: " + TextoSano);
            ColocarS2.Guardar("s2t-doc");
        }

        /// <summary>El consultorio por fuera, de día, para revisar sus paredes.</summary>
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Transform doc = ColocarS2.Buscar(RutaDoc);
            Vector3 c = doc != null ? doc.position : new Vector3(-100f, 0f, -13f);
            ColocarS2t.FotoHora(13f, c + new Vector3(12f, 2f, -10f), c + new Vector3(0f, 2f, 0f), ColocarS2t.CarpetaCapturas + "s2t-doc-fuera-1.png", 65f);
            ColocarS2t.FotoHora(13f, c + new Vector3(-12f, 2f, 10f), c + new Vector3(0f, 2f, 0f), ColocarS2t.CarpetaCapturas + "s2t-doc-fuera-2.png", 65f);
            ColocarS2t.FotoHora(13f, c + new Vector3(0f, 25f, -20f), c, ColocarS2t.CarpetaCapturas + "s2t-doc-fuera-alto.png", 65f);
        }
    }
}

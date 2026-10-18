using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// Crea lo de T-156: ConfigFuriaJefe_Base, datos del jefe de prueba y el prefab IA/Pruebas/RivalJefeDePrueba (variante de
    /// RivalBasico con FuriaJefe). Idempotente. Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorRivalJefePrueba.Crear
    /// </summary>
    public static class CreadorRivalJefePrueba
    {
        private const string RutaIA = "Assets/_Proyecto/IA";
        private const string RutaConfig = RutaIA + "/Datos/ConfigFuriaJefe_Base.asset";
        private const string RutaDatosMecha = RutaIA + "/Datos/DatosMecha_JefePrueba.asset";
        private const string RutaDatosRival = RutaIA + "/Datos/DatosRival_JefePrueba.asset";
        private const string RutaMaterialCuerpo = RutaIA + "/Materiales/RivalJefePrueba_Cuerpo.mat";
        private const string RutaPrefabBase = RutaIA + "/Prefabs/RivalBasico.prefab";
        private const string RutaPrefab = RutaIA + "/Pruebas/RivalJefeDePrueba.prefab";

        private const float VidaJefe = 150f;
        private const float DanioJefe = 12f;
        private const float PreparacionGolpeJefe = 0.3f;
        private const float RecargaGolpeJefe = 0.2f;
        // Espera fija (sin azar) para que las pruebas midan la diferencia de la furia sin ruido.
        private const float EsperaJefe = 3f;
        private static readonly Color ColorCuerpo = new Color(0.25f, 0.25f, 0.3f);

        [MenuItem("Mecha Cyberpunk/Crear T-156")]
        public static void Crear()
        {
            AssetDatabase.Refresh();

            CrearAsset<ConfigFuriaJefe>(RutaConfig);
            DatosMecha mecha = CrearAsset<DatosMecha>(RutaDatosMecha);
            SerializedObject soMecha = new SerializedObject(mecha);
            soMecha.FindProperty("nombre").stringValue = "Jefe de prueba";
            soMecha.FindProperty("vidaMaxima").floatValue = VidaJefe;
            soMecha.FindProperty("danioGolpe").floatValue = DanioJefe;
            soMecha.FindProperty("preparacionGolpe").floatValue = PreparacionGolpeJefe;
            soMecha.FindProperty("recargaGolpe").floatValue = RecargaGolpeJefe;
            soMecha.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mecha);

            DatosRival rival = CrearAsset<DatosRival>(RutaDatosRival);
            SerializedObject soRival = new SerializedObject(rival);
            soRival.FindProperty("alias").stringValue = "JEFE DE PRUEBA";
            soRival.FindProperty("mecha").objectReferenceValue = mecha;
            soRival.FindProperty("esperaEntreAtaquesMin").floatValue = EsperaJefe;
            soRival.FindProperty("esperaEntreAtaquesMax").floatValue = EsperaJefe;
            soRival.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rival);

            Material cuerpo = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCuerpo);
            if (cuerpo == null)
            {
                cuerpo = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(cuerpo, RutaMaterialCuerpo);
            }
            cuerpo.SetColor("_BaseColor", ColorCuerpo);
            EditorUtility.SetDirty(cuerpo);
            AssetDatabase.SaveAssets();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                ArmarPrefab(rival, mecha, cuerpo);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-156 creada.");
        }

        private static T CrearAsset<T>(string ruta) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, ruta);
            }
            return asset;
        }

        private static void ArmarPrefab(DatosRival rival, DatosMecha mecha, Material cuerpo)
        {
            GameObject baseRival = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabBase);
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(baseRival);
            try
            {
                instancia.name = "RivalJefeDePrueba";
                Asignar(instancia.GetComponent<RivalBasico>(), "datos", rival);
                Asignar(instancia.GetComponent<VidaMecha>(), "datos", mecha);
                instancia.GetComponent<PresentacionRival>().Cuerpo.sharedMaterial = cuerpo;
                FuriaJefe furia = instancia.AddComponent<FuriaJefe>();
                Asignar(furia, "config", AssetDatabase.LoadAssetAtPath<ConfigFuriaJefe>(RutaConfig));
                PrefabUtility.SaveAsPrefabAsset(instancia, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        private static void Asignar(Object componente, string campo, Object valor)
        {
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

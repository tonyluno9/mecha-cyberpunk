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
    /// Crea lo de T-068: ConfigPiedadRival, datos del campeón del tutorial, su material y el prefab RivalTutorial.
    /// El prefab es una variante de RivalBasico, que no se toca. Es idempotente: lo que ya existe no se vuelve a crear
    /// (así los fileIDs del prefab no cambian). Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorRivalTutorial.Crear
    /// </summary>
    public static class CreadorRivalTutorial
    {
        private const string RutaIA = "Assets/_Proyecto/IA";
        private const string RutaConfigPiedad = RutaIA + "/Datos/ConfigPiedadRival_Base.asset";
        private const string RutaDatosMecha = RutaIA + "/Datos/DatosMecha_Tutorial.asset";
        private const string RutaDatosRival = RutaIA + "/Datos/DatosRival_Tutorial.asset";
        private const string RutaMaterialCuerpo = RutaIA + "/Materiales/RivalTutorial_Cuerpo.mat";
        private const string RutaPrefabBase = RutaIA + "/Prefabs/RivalBasico.prefab";
        private const string RutaPrefab = RutaIA + "/Prefabs/RivalTutorial.prefab";

        private const string AliasCampeon = "EL CAMPEÓN";
        private const float VidaCampeon = 120f;
        private const float DanioCampeon = 12f;
        private const float PreparacionGolpeCampeon = 0.6f;
        private static readonly Color ColorCuerpo = new Color(0.15f, 0.3f, 0.7f);

        [MenuItem("Mecha Cyberpunk/Crear T-068")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaIA + "/Datos");
            AsegurarCarpeta(RutaIA + "/Materiales");
            AsegurarCarpeta(RutaIA + "/Prefabs");
            AssetDatabase.Refresh();

            CrearAsset<ConfigPiedadRival>(RutaConfigPiedad);
            DatosMecha mecha = CrearDatosMecha();
            DatosRival rival = CrearDatosRival(mecha);
            Material cuerpo = CrearMaterial();
            AssetDatabase.SaveAssets();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                ArmarPrefab(mecha, rival, cuerpo);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-068 creada.");
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
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

        private static DatosMecha CrearDatosMecha()
        {
            DatosMecha datos = CrearAsset<DatosMecha>(RutaDatosMecha);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = "Campeón";
            so.FindProperty("vidaMaxima").floatValue = VidaCampeon;
            so.FindProperty("danioGolpe").floatValue = DanioCampeon;
            so.FindProperty("preparacionGolpe").floatValue = PreparacionGolpeCampeon;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static DatosRival CrearDatosRival(DatosMecha mecha)
        {
            DatosRival datos = CrearAsset<DatosRival>(RutaDatosRival);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("alias").stringValue = AliasCampeon;
            so.FindProperty("mecha").objectReferenceValue = mecha;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static Material CrearMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCuerpo);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, RutaMaterialCuerpo);
            }
            material.SetColor("_BaseColor", ColorCuerpo);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ArmarPrefab(DatosMecha mecha, DatosRival rival, Material cuerpo)
        {
            GameObject baseRival = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabBase);
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(baseRival);
            try
            {
                instancia.name = "RivalTutorial";

                Asignar(instancia.GetComponent<RivalBasico>(), "datos", rival);
                Asignar(instancia.GetComponent<VidaMecha>(), "datos", mecha);
                instancia.GetComponent<PresentacionRival>().Cuerpo.sharedMaterial = cuerpo;

                PiedadRival piedad = instancia.AddComponent<PiedadRival>();
                Asignar(piedad, "config", AssetDatabase.LoadAssetAtPath<ConfigPiedadRival>(RutaConfigPiedad));

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

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
    /// Crea lo de T-069: datos del pandillero, su material y el prefab RivalPandilla (variante de RivalBasico con
    /// PiedadRival y turnos). Idempotente: lo que ya existe no se vuelve a crear (los fileIDs del prefab no cambian).
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorRivalPandilla.Crear
    /// </summary>
    public static class CreadorRivalPandilla
    {
        private const string RutaIA = "Assets/_Proyecto/IA";
        private const string RutaConfigPiedad = RutaIA + "/Datos/ConfigPiedadRival_Base.asset";
        private const string RutaDatosMecha = RutaIA + "/Datos/DatosMecha_Pandilla.asset";
        private const string RutaDatosRival = RutaIA + "/Datos/DatosRival_Pandilla.asset";
        private const string RutaMaterialCuerpo = RutaIA + "/Materiales/RivalPandilla_Cuerpo.mat";
        private const string RutaPrefabBase = RutaIA + "/Prefabs/RivalBasico.prefab";
        private const string RutaPrefab = RutaIA + "/Prefabs/RivalPandilla.prefab";

        private const string AliasPandillero = "PANDILLERO";
        private const float VidaPandillero = 60f;
        private const float DanioPandillero = 8f;
        private const float VelocidadPandillero = 4.6f;
        private const float AceleracionPandillero = 7f;
        private const float PreparacionGolpePandillero = 0.6f;
        private static readonly Color ColorCuerpo = new Color(0.55f, 0.15f, 0.65f);

        [MenuItem("Mecha Cyberpunk/Crear T-069")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaIA + "/Datos");
            AsegurarCarpeta(RutaIA + "/Materiales");
            AsegurarCarpeta(RutaIA + "/Prefabs");
            AssetDatabase.Refresh();

            DatosMecha mecha = CrearDatosMecha();
            DatosRival rival = CrearDatosRival(mecha);
            Material cuerpo = CrearMaterial();
            AssetDatabase.SaveAssets();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                ArmarPrefab(mecha, rival, cuerpo);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-069 creada.");
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
            so.FindProperty("nombre").stringValue = "Pandillero";
            so.FindProperty("vidaMaxima").floatValue = VidaPandillero;
            so.FindProperty("danioGolpe").floatValue = DanioPandillero;
            so.FindProperty("velocidadMaxima").floatValue = VelocidadPandillero;
            so.FindProperty("aceleracion").floatValue = AceleracionPandillero;
            so.FindProperty("preparacionGolpe").floatValue = PreparacionGolpePandillero;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static DatosRival CrearDatosRival(DatosMecha mecha)
        {
            DatosRival datos = CrearAsset<DatosRival>(RutaDatosRival);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("alias").stringValue = AliasPandillero;
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
                instancia.name = "RivalPandilla";

                RivalBasico comportamiento = instancia.GetComponent<RivalBasico>();
                Asignar(comportamiento, "datos", rival);
                SerializedObject soRival = new SerializedObject(comportamiento);
                soRival.FindProperty("usarTurnos").boolValue = true;
                soRival.ApplyModifiedPropertiesWithoutUndo();
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

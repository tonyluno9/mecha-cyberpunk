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
    /// Crea lo de T-129: datos de "EL CHATARRAS" (rival de la pelea casera), su material y RivalCasero.prefab.
    /// Es una variante de RivalBasico SIN PiedadRival (D-064: la casera se puede perder). Idempotente: lo que ya existe no se
    /// vuelve a crear. Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorRivalCasero.Crear
    /// </summary>
    public static class CreadorRivalCasero
    {
        public const string RutaPrefab = RutaIA + "/Prefabs/RivalCasero.prefab";

        private const string RutaIA = "Assets/_Proyecto/IA";
        private const string RutaDatosMecha = RutaIA + "/Datos/DatosMecha_Casero.asset";
        private const string RutaDatosRival = RutaIA + "/Datos/DatosRival_Casero.asset";
        private const string RutaMaterialCuerpo = RutaIA + "/Materiales/RivalCasero_Cuerpo.mat";
        private const string RutaPrefabBase = RutaIA + "/Prefabs/RivalBasico.prefab";

        // Valores propuesta (D-tony-66): pega como el jugador pero aguanta menos y es un poco más lento.
        private const string AliasChatarras = "EL CHATARRAS";
        private const float VidaChatarras = 90f;
        private const float DanioChatarras = 10f;
        private const float VelocidadChatarras = 3.5f;
        private const float PreparacionGolpeChatarras = 0.5f;
        private static readonly Color ColorCuerpo = new Color(0.45f, 0.22f, 0.1f);

        [MenuItem("Mecha Cyberpunk/Crear T-129 rival casero")]
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
            Debug.Log("[IA] T-129 rival casero creado.");
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
            so.FindProperty("nombre").stringValue = "Chatarras";
            so.FindProperty("vidaMaxima").floatValue = VidaChatarras;
            so.FindProperty("velocidadMaxima").floatValue = VelocidadChatarras;
            so.FindProperty("danioGolpe").floatValue = DanioChatarras;
            so.FindProperty("preparacionGolpe").floatValue = PreparacionGolpeChatarras;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static DatosRival CrearDatosRival(DatosMecha mecha)
        {
            DatosRival datos = CrearAsset<DatosRival>(RutaDatosRival);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("alias").stringValue = AliasChatarras;
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
                instancia.name = "RivalCasero";
                Asignar(instancia.GetComponent<RivalBasico>(), "datos", rival);
                Asignar(instancia.GetComponent<VidaMecha>(), "datos", mecha);
                instancia.GetComponent<PresentacionRival>().Cuerpo.sharedMaterial = cuerpo;
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

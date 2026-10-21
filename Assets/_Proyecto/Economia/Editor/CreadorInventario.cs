using MechaCyberpunk.Core;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Economia.Editor
{
    /// <summary>
    /// Crea los objetos y el catálogo del inventario (T-142, valores de D-tony-86) y agrega el ServicioInventario a
    /// ServiciosEconomia.prefab sin regenerarlo. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Economia.Editor.CreadorInventario.Crear
    /// </summary>
    public static class CreadorInventario
    {
        private const string Carpeta = "Assets/_Proyecto/Economia/Datos";
        private const string RutaPrefab = "Assets/_Proyecto/Economia/Prefabs/ServiciosEconomia.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-142 inventario")]
        public static void Crear()
        {
            var brazo = Objeto("BrazoChatarra", "brazo_chatarra", "Brazo de chatarra", TipoObjeto.Pieza, 300, 5, null);
            var chatarra = Objeto("Chatarra", "chatarra", "Chatarra", TipoObjeto.Chatarra, 50, 20, null);
            var kit = Objeto("KitReparacion", "kit_reparacion", "Kit de reparación", TipoObjeto.KitReparacion, 0, 3,
                AssetDatabase.LoadAssetAtPath<DatosOferta>(Carpeta + "/DatosOferta_KitReparacion.asset"));

            string rutaCatalogo = Carpeta + "/CatalogoObjetos_Base.asset";
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>(rutaCatalogo);
            if (catalogo == null)
            {
                catalogo = ScriptableObject.CreateInstance<CatalogoObjetos>();
                AssetDatabase.CreateAsset(catalogo, rutaCatalogo);
            }
            var so = new SerializedObject(catalogo);
            SerializedProperty lista = so.FindProperty("objetos");
            var todos = new[] { brazo, chatarra, kit };
            // Solo agrega lo que falta: conserva lo que otros creadores (piezas del mecha, T-166) ya pusieron.
            foreach (DatosObjeto objeto in todos)
            {
                bool presente = false;
                for (int i = 0; i < lista.arraySize; i++)
                {
                    if (lista.GetArrayElementAtIndex(i).objectReferenceValue == objeto) { presente = true; break; }
                }
                if (presente) continue;
                lista.arraySize++;
                lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = objeto;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                var servicio = raiz.GetComponent<ServicioInventario>();
                if (servicio == null) servicio = raiz.AddComponent<ServicioInventario>();
                var soServ = new SerializedObject(servicio);
                soServ.FindProperty("catalogo").objectReferenceValue = catalogo;
                soServ.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Economia] T-142 inventario creado.");
        }

        private static DatosObjeto Objeto(string archivo, string id, string nombre, TipoObjeto tipo, int venta, int maximo, DatosOferta oferta)
        {
            string ruta = $"{Carpeta}/DatosObjeto_{archivo}.asset";
            var o = AssetDatabase.LoadAssetAtPath<DatosObjeto>(ruta);
            if (o != null) return o; // ya existe: no se pisan los valores ajustados en el inspector (T-150)
            o = ScriptableObject.CreateInstance<DatosObjeto>();
            AssetDatabase.CreateAsset(o, ruta);
            var so = new SerializedObject(o);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("nombre").stringValue = nombre;
            so.FindProperty("tipo").enumValueIndex = (int)tipo;
            so.FindProperty("valorVenta").intValue = venta;
            so.FindProperty("maximo").intValue = maximo;
            so.FindProperty("oferta").objectReferenceValue = oferta;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(o);
            return o;
        }
    }
}

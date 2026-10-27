using MechaCyberpunk.Core;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Economia.Editor
{
    /// <summary>Crea los datos de ejemplo de la economía (precios de prueba).</summary>
    public static class CreadorDatosEconomia
    {
        private const string Carpeta = "Assets/_Proyecto/Economia/Datos";
        private const string DescripcionPrueba = "Precio de prueba";

        [MenuItem("Mecha Cyberpunk/Crear T-033")]
        public static void Crear()
        {
            var kit = Oferta("KitReparacion", "Kit de reparación", 250, TipoOferta.KitReparacion);
            var misil = Oferta("MunicionMisil", "Munición de misil", 500, TipoOferta.Municion);
            var brazo = Oferta("BrazoChatarra", "Brazo de chatarra", 1200, TipoOferta.Pieza);
            var ojos = Oferta("ImplanteOjos", "Implante de ojos", 5000, TipoOferta.Implante);
            var bebida = Oferta("Bebida", "Bebida", 50, TipoOferta.Consumible);
            var combustible = Oferta("MunicionCombustible", "Munición de combustible", 60, TipoOferta.Municion);
            var celdas = Oferta("MunicionCeldas", "Munición de celdas", 40, TipoOferta.Municion);
            var cargas = Oferta("MunicionCargas", "Munición de cargas", 50, TipoOferta.Municion);

            Tienda("MercadoNegro", IdLugar.MercadoNegro, "Mercado negro", kit, misil, brazo, combustible, celdas, cargas);
            Tienda("Clinica", IdLugar.Clinica, "Clínica", ojos);
            Tienda("Bar", IdLugar.Bar, "Bar", bebida);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Paso explícito (D-tony-109, T-174): ajusta el precio de la munición de misil de $500 (prueba) a $80. No toca otro precio.
        /// Batchmode: -executeMethod MechaCyberpunk.Economia.Editor.CreadorDatosEconomia.AjustarPrecioMunicionMisil
        /// </summary>
        [MenuItem("Mecha Cyberpunk/Ajustar T-174 precio de misiles")]
        public static void AjustarPrecioMunicionMisil()
        {
            var misil = AssetDatabase.LoadAssetAtPath<DatosOferta>($"{Carpeta}/DatosOferta_MunicionMisil.asset");
            if (misil == null) { Debug.LogError("[Economia] Falta DatosOferta_MunicionMisil: corre Crear T-033 primero."); return; }
            var so = new SerializedObject(misil);
            so.FindProperty("precio").intValue = 80;
            if (so.ApplyModifiedPropertiesWithoutUndo()) EditorUtility.SetDirty(misil);
            AssetDatabase.SaveAssets();
        }

        private static DatosOferta Oferta(string archivo, string nombre, int precio, TipoOferta tipo)
        {
            string ruta = $"{Carpeta}/DatosOferta_{archivo}.asset";
            var existente = AssetDatabase.LoadAssetAtPath<DatosOferta>(ruta);
            if (existente != null) return existente; // no se pisan los precios ajustados en el inspector (T-150)
            var o = Cargar<DatosOferta>($"DatosOferta_{archivo}");
            var so = new SerializedObject(o);
            so.FindProperty("id").stringValue = archivo;
            so.FindProperty("nombre").stringValue = nombre;
            so.FindProperty("descripcion").stringValue = DescripcionPrueba;
            so.FindProperty("precio").intValue = precio;
            so.FindProperty("tipo").enumValueIndex = (int)tipo;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(o);
            return o;
        }

        private static void Tienda(string archivo, IdLugar lugar, string nombre, params DatosOferta[] ofertas)
        {
            var t = Cargar<DatosTienda>($"DatosTienda_{archivo}");
            var so = new SerializedObject(t);
            so.FindProperty("lugar").enumValueIndex = (int)lugar;
            so.FindProperty("nombre").stringValue = nombre;
            var lista = so.FindProperty("ofertas");
            lista.arraySize = ofertas.Length;
            for (int i = 0; i < ofertas.Length; i++)
                lista.GetArrayElementAtIndex(i).objectReferenceValue = ofertas[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(t);
        }

        private static T Cargar<T>(string nombre) where T : ScriptableObject
        {
            string ruta = $"{Carpeta}/{nombre}.asset";
            var existente = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (existente != null) return existente;
            var nuevo = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(nuevo, ruta);
            return nuevo;
        }
    }
}

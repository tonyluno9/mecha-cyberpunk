using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-720: crea las balas de escopeta, fusil y sniper (DatosMunicionArma y su oferta) y las agrega al CatalogoObjetos_Base y a
    /// DatosTienda_MercadoNegro. Idempotente. Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorMunicionArmas.Crear
    /// </summary>
    public static class CreadorMunicionArmas
    {
        private const string Carpeta = "Assets/_Proyecto/Economia/Datos";
        private const string RutaCatalogoObjetos = Carpeta + "/CatalogoObjetos_Base.asset";
        private const string RutaTienda = Carpeta + "/DatosTienda_MercadoNegro.asset";

        private struct Balas
        {
            public ArmaAPie arma;
            public string clave;
            public string nombre;
            public int precio;
            public int porPaquete;
            public int iniciales;
            public int maximo;
        }

        private static readonly Balas[] Lista =
        {
            new Balas { arma = ArmaAPie.Escopeta, clave = "escopeta", nombre = "Balas de escopeta", precio = 60, porPaquete = 8, iniciales = 16, maximo = 40 },
            new Balas { arma = ArmaAPie.Fusil, clave = "fusil", nombre = "Balas de fusil", precio = 120, porPaquete = 60, iniciales = 60, maximo = 240 },
            new Balas { arma = ArmaAPie.Sniper, clave = "sniper", nombre = "Balas de sniper", precio = 150, porPaquete = 10, iniciales = 20, maximo = 40 }
        };

        [MenuItem("Mecha Cyberpunk/Crear T-720 balas de armas")]
        public static void Crear()
        {
            foreach (Balas b in Lista)
            {
                DatosOferta oferta = CargarOCrear<DatosOferta>(Carpeta + "/DatosOferta_Municion_" + b.clave + ".asset");
                var so = new SerializedObject(oferta);
                so.FindProperty("id").stringValue = "oferta_municion_" + b.clave;
                so.FindProperty("nombre").stringValue = b.nombre;
                so.FindProperty("descripcion").stringValue = b.porPaquete + " balas.";
                so.FindProperty("precio").intValue = b.precio;
                so.FindProperty("tipo").enumValueIndex = (int)TipoOferta.Municion;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(oferta);

                DatosMunicionArma datos = CargarOCrear<DatosMunicionArma>(Carpeta + "/DatosMunicionArma_" + b.clave + ".asset");
                var sd = new SerializedObject(datos);
                sd.FindProperty("id").stringValue = "municion_arma_" + b.clave;
                sd.FindProperty("nombre").stringValue = b.nombre;
                sd.FindProperty("tipo").enumValueIndex = (int)TipoObjeto.Municion;
                sd.FindProperty("valorVenta").intValue = 0;
                sd.FindProperty("maximo").intValue = b.maximo;
                sd.FindProperty("oferta").objectReferenceValue = oferta;
                sd.FindProperty("arma").enumValueIndex = (int)b.arma;
                sd.FindProperty("balasPorPaquete").intValue = b.porPaquete;
                sd.FindProperty("balasIniciales").intValue = b.iniciales;
                sd.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(datos);

                AgregarSiFalta(RutaCatalogoObjetos, "objetos", datos);
                AgregarSiFalta(RutaTienda, "ofertas", oferta);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-720: balas de escopeta, fusil y sniper creadas y puestas en El Mercado.");
        }

        private static void AgregarSiFalta(string rutaDestino, string campo, Object elemento)
        {
            ScriptableObject destino = AssetDatabase.LoadAssetAtPath<ScriptableObject>(rutaDestino);
            if (destino == null)
            {
                Debug.LogError("[Personaje] CreadorMunicionArmas: falta " + rutaDestino);
                return;
            }
            var so = new SerializedObject(destino);
            SerializedProperty lista = so.FindProperty(campo);
            for (int i = 0; i < lista.arraySize; i++)
            {
                if (lista.GetArrayElementAtIndex(i).objectReferenceValue == elemento)
                {
                    return;
                }
            }
            lista.arraySize++;
            lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = elemento;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(destino);
        }

        private static T CargarOCrear<T>(string ruta) where T : ScriptableObject
        {
            T existente = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (existente != null)
            {
                return existente;
            }
            T nuevo = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(nuevo, ruta);
            return nuevo;
        }
    }
}

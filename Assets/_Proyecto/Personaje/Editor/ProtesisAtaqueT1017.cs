using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Habilidades;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1017: crea las prótesis de ataque del Doc (brazo láser y hombrera de misiles): sus ofertas, sus objetos del inventario, la tienda "PRÓTESIS DE
    /// ATAQUE" y el catálogo en Resources que lee la clínica; y mete los objetos en CatalogoObjetos_Base para que se guarden con la partida.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.ProtesisAtaqueT1017.Crear
    /// </summary>
    public static class ProtesisAtaqueT1017
    {
        public const string Carpeta = "Assets/_Proyecto/Personaje/Datos/Protesis";
        public const string RutaCatalogo = "Assets/_Proyecto/Personaje/Datos/Resources/CatalogoProtesisAtaque.asset";
        private const string RutaCatalogoObjetos = "Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset";

        private readonly struct Protesis
        {
            public readonly string Clave;
            public readonly string Nombre;
            public readonly string Descripcion;
            public readonly int Precio;
            public readonly HabilidadProtesis Habilidad;

            public Protesis(string clave, string nombre, string descripcion, int precio, HabilidadProtesis habilidad)
            {
                Clave = clave;
                Nombre = nombre;
                Descripcion = descripcion;
                Precio = precio;
                Habilidad = habilidad;
            }
        }

        private static readonly Protesis[] Lista =
        {
            new Protesis("laser", "Brazo láser", "Prótesis de ataque. [4]: el brazo dispara un rayo que quema todo lo que toca.", 2500, HabilidadProtesis.Laser),
            new Protesis("misiles", "Hombrera de misiles", "Prótesis de ataque. [5]: lanza misiles teledirigidos a los enemigos de enfrente.", 3500, HabilidadProtesis.Misiles),
        };

        [MenuItem("Mecha Cyberpunk/T-1017 Crear prótesis de ataque")]
        public static void Crear()
        {
            if (!AssetDatabase.IsValidFolder(Carpeta))
            {
                Directory.CreateDirectory(Carpeta);
                AssetDatabase.Refresh();
            }
            var ofertas = new List<DatosOferta>();
            var protesis = new List<DatosProtesisAtaque>();
            foreach (Protesis p in Lista)
            {
                DatosOferta oferta = CargarOCrear<DatosOferta>(Carpeta + "/DatosOferta_Protesis_" + p.Clave + ".asset");
                SerializedObject so = new SerializedObject(oferta);
                so.FindProperty("id").stringValue = "oferta_protesis_" + p.Clave;
                so.FindProperty("nombre").stringValue = p.Nombre;
                so.FindProperty("descripcion").stringValue = p.Descripcion;
                so.FindProperty("precio").intValue = p.Precio;
                so.FindProperty("tipo").enumValueIndex = (int)TipoOferta.Implante;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(oferta);
                ofertas.Add(oferta);

                DatosProtesisAtaque datos = CargarOCrear<DatosProtesisAtaque>(Carpeta + "/DatosProtesisAtaque_" + p.Clave + ".asset");
                SerializedObject sd = new SerializedObject(datos);
                sd.FindProperty("id").stringValue = "protesis_" + p.Clave;
                sd.FindProperty("nombre").stringValue = p.Nombre;
                sd.FindProperty("tipo").enumValueIndex = (int)TipoObjeto.Protesis;
                sd.FindProperty("valorVenta").intValue = 0;
                sd.FindProperty("maximo").intValue = 1;
                sd.FindProperty("oferta").objectReferenceValue = oferta;
                sd.FindProperty("habilidad").enumValueIndex = (int)p.Habilidad;
                sd.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(datos);
                protesis.Add(datos);
                AgregarSiFalta(RutaCatalogoObjetos, "objetos", datos);
            }

            DatosTienda tienda = CargarOCrear<DatosTienda>(Carpeta + "/DatosTienda_ProtesisAtaque.asset");
            SerializedObject st = new SerializedObject(tienda);
            st.FindProperty("nombre").stringValue = "Prótesis de ataque";
            SerializedProperty listaOfertas = st.FindProperty("ofertas");
            listaOfertas.arraySize = ofertas.Count;
            for (int i = 0; i < ofertas.Count; i++)
            {
                listaOfertas.GetArrayElementAtIndex(i).objectReferenceValue = ofertas[i];
            }
            st.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tienda);

            CatalogoProtesisAtaque catalogo = CargarOCrear<CatalogoProtesisAtaque>(RutaCatalogo);
            SerializedObject sc = new SerializedObject(catalogo);
            SerializedProperty listaProtesis = sc.FindProperty("protesis");
            listaProtesis.arraySize = protesis.Count;
            for (int i = 0; i < protesis.Count; i++)
            {
                listaProtesis.GetArrayElementAtIndex(i).objectReferenceValue = protesis[i];
            }
            sc.FindProperty("tienda").objectReferenceValue = tienda;
            sc.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-1017: prótesis de ataque creadas (brazo láser y hombrera de misiles).");
        }

        private static void AgregarSiFalta(string rutaDestino, string campo, Object elemento)
        {
            ScriptableObject destino = AssetDatabase.LoadAssetAtPath<ScriptableObject>(rutaDestino);
            if (destino == null)
            {
                Debug.LogError("[Personaje] ProtesisAtaqueT1017: falta " + rutaDestino);
                return;
            }
            SerializedObject so = new SerializedObject(destino);
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

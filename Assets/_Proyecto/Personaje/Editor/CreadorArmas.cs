using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-403 y T-416: crea los 20 DatosArma (5 armas por 4 niveles, números de D-tony-214 y D-tony-253), el CatalogoArmas_Base, las 19 ofertas del Mercado
    /// (4 armas y 15 mejoras), y las agrega al CatalogoObjetos_Base y a DatosTienda_MercadoNegro. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorArmas.Crear
    /// </summary>
    public static class CreadorArmas
    {
        public const string CarpetaArmas = "Assets/_Proyecto/Personaje/Datos/Armas";
        public const string RutaCatalogoArmas = "Assets/_Proyecto/Personaje/Datos/CatalogoArmas_Base.asset";
        private const string RutaCatalogoObjetos = "Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset";
        private const string RutaTienda = "Assets/_Proyecto/Economia/Datos/DatosTienda_MercadoNegro.asset";
        private const string CarpetaOfertas = "Assets/_Proyecto/Economia/Datos";

        private class Familia
        {
            public ArmaAPie arma;
            public string clave;
            public string nombre;
            public float[] danio;
            public int perdigones = 1;
            public int[] cargador;
            public float[] recarga;
            public float entre;
            public bool automatica;
            public float dispersion;
            public float dispersionApuntando;
            public float alcance;
            public int[] precio;
        }

        private static readonly Familia[] Familias =
        {
            new Familia { arma = ArmaAPie.Punos, clave = "punos", nombre = "Puños", danio = new[] { 20f, 26f, 32f, 40f },
                cargador = new[] { 1, 1, 1, 1 }, recarga = new[] { 0.45f, 0.4f, 0.35f, 0.3f }, entre = 0.45f, alcance = 2f,
                precio = new[] { 0, 100, 200, 350 } },
            new Familia { arma = ArmaAPie.Pistola, clave = "pistola", nombre = "Pistola", danio = new[] { 25f, 30f, 36f, 44f },
                cargador = new[] { 8, 10, 12, 15 }, recarga = new[] { 1.4f, 1.2f, 1.0f, 0.8f }, entre = 0.25f, alcance = 60f,
                precio = new[] { 150, 150, 300, 500 } },
            new Familia { arma = ArmaAPie.Escopeta, clave = "escopeta", nombre = "Escopeta", danio = new[] { 9f, 11f, 13f, 16f }, perdigones = 8,
                cargador = new[] { 4, 5, 6, 8 }, recarga = new[] { 2.4f, 2.0f, 1.7f, 1.4f }, entre = 0.9f, alcance = 20f,
                dispersion = 6f, dispersionApuntando = 4f, precio = new[] { 350, 250, 450, 700 } },
            new Familia { arma = ArmaAPie.Fusil, clave = "fusil", nombre = "Fusil", danio = new[] { 15f, 18f, 21f, 25f },
                cargador = new[] { 30, 30, 35, 40 }, recarga = new[] { 2.0f, 1.8f, 1.6f, 1.3f }, entre = 0.11f, automatica = true, alcance = 70f,
                dispersion = 2.5f, dispersionApuntando = 1.2f, precio = new[] { 700, 350, 600, 850 } },
            new Familia { arma = ArmaAPie.Sniper, clave = "sniper", nombre = "Sniper", danio = new[] { 70f, 85f, 100f, 120f },
                cargador = new[] { 4, 5, 6, 7 }, recarga = new[] { 2.8f, 2.5f, 2.2f, 1.9f }, entre = 1.2f, alcance = 150f,
                dispersion = 8f, dispersionApuntando = 0f, precio = new[] { 900, 400, 650, 900 } }
        };

        private static readonly string[] Numerales = { string.Empty, " I", " II", " III" };

        [MenuItem("Mecha Cyberpunk/Crear T-403 armas")]
        public static void Crear()
        {
            CrearDatos();
            CreadorJugadorAPie.RegenerarPrefabTerceraPersona();
            Debug.Log("[Personaje] T-403/T-416: 5 armas, catálogo y El Mercado creados.");
        }

        public static void CrearDatos()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaArmas))
            {
                Directory.CreateDirectory(CarpetaArmas);
                AssetDatabase.Refresh();
            }
            BorrarSubfusil();
            var armas = new List<DatosArma>();
            var bases = new List<DatosOferta>();
            var mejoras = new List<DatosOferta>();
            foreach (Familia f in Familias)
            {
                for (int nivel = 0; nivel < 4; nivel++)
                {
                    DatosOferta oferta = f.precio[nivel] > 0 ? CrearOferta(f, nivel) : null;
                    if (oferta != null)
                    {
                        (nivel == 0 ? bases : mejoras).Add(oferta);
                    }
                    armas.Add(CrearArma(f, nivel, oferta));
                }
            }
            var ofertas = new List<DatosOferta>(bases);
            ofertas.AddRange(mejoras);

            var catalogo = CargarOCrear<CatalogoArmas>(RutaCatalogoArmas);
            AsignarLista(catalogo, "armas", armas);
            ReemplazarArmas(AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaCatalogoObjetos), "objetos", armas);
            ReemplazarArmas(AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaTienda), "ofertas", ofertas);
            AssetDatabase.SaveAssets();
        }

        // T-416: el subfusil pasó a fusil; los assets viejos se borran.
        private static void BorrarSubfusil()
        {
            foreach (string guid in AssetDatabase.FindAssets("subfusil", new[] { CarpetaArmas, CarpetaOfertas }))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        // Quita los vacíos (assets borrados) y las armas u ofertas de armas que ya estaban, y agrega las nuevas en su orden.
        private static void ReemplazarArmas<T>(ScriptableObject destino, string campo, List<T> nuevos) where T : Object
        {
            if (destino == null)
            {
                Debug.LogError("[Personaje] CreadorArmas: falta el asset que recibe '" + campo + "'.");
                return;
            }
            var so = new SerializedObject(destino);
            SerializedProperty lista = so.FindProperty(campo);
            for (int i = lista.arraySize - 1; i >= 0; i--)
            {
                Object elemento = lista.GetArrayElementAtIndex(i).objectReferenceValue;
                bool esArma = elemento is DatosArma || (elemento is DatosOferta && elemento.name.StartsWith("DatosOferta_Arma_"));
                if (elemento == null || esArma)
                {
                    lista.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    lista.DeleteArrayElementAtIndex(i);
                }
            }
            foreach (T elemento in nuevos)
            {
                lista.arraySize++;
                lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = elemento;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(destino);
        }

        private static string NombreNivel(Familia f, int nivel) => f.nombre + Numerales[nivel];

        private static DatosOferta CrearOferta(Familia f, int nivel)
        {
            var oferta = CargarOCrear<DatosOferta>(CarpetaOfertas + "/DatosOferta_Arma_" + f.clave + "_" + nivel + ".asset");
            var so = new SerializedObject(oferta);
            so.FindProperty("id").stringValue = "oferta_arma_" + f.clave + "_" + nivel;
            so.FindProperty("nombre").stringValue = NombreNivel(f, nivel);
            so.FindProperty("descripcion").stringValue = nivel == 0 ? "Arma nueva." : "Mejora: más daño, más cargador, recarga rápida.";
            so.FindProperty("precio").intValue = f.precio[nivel];
            so.FindProperty("tipo").enumValueIndex = (int)TipoOferta.Pieza;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(oferta);
            return oferta;
        }

        private static DatosArma CrearArma(Familia f, int nivel, DatosOferta oferta)
        {
            var arma = CargarOCrear<DatosArma>(CarpetaArmas + "/DatosArma_" + f.clave + "_" + nivel + ".asset");
            var so = new SerializedObject(arma);
            so.FindProperty("id").stringValue = "arma_" + f.clave + "_" + nivel;
            so.FindProperty("nombre").stringValue = NombreNivel(f, nivel);
            so.FindProperty("tipo").enumValueIndex = (int)TipoObjeto.Arma;
            so.FindProperty("valorVenta").intValue = 0;
            so.FindProperty("maximo").intValue = 1;
            so.FindProperty("oferta").objectReferenceValue = oferta;
            so.FindProperty("arma").enumValueIndex = (int)f.arma;
            so.FindProperty("nivel").intValue = nivel;
            so.FindProperty("precio").intValue = f.precio[nivel];
            so.FindProperty("danio").floatValue = f.danio[nivel];
            so.FindProperty("perdigones").intValue = f.perdigones;
            so.FindProperty("cargador").intValue = f.cargador[nivel];
            so.FindProperty("recarga").floatValue = f.recarga[nivel];
            so.FindProperty("tiempoEntreDisparos").floatValue = f.entre;
            so.FindProperty("automatica").boolValue = f.automatica;
            so.FindProperty("dispersion").floatValue = f.dispersion;
            so.FindProperty("dispersionApuntando").floatValue = f.dispersionApuntando;
            so.FindProperty("alcance").floatValue = f.alcance;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(arma);
            return arma;
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

        private static void AsignarLista<T>(ScriptableObject destino, string campo, List<T> elementos) where T : Object
        {
            var so = new SerializedObject(destino);
            SerializedProperty lista = so.FindProperty(campo);
            lista.arraySize = elementos.Count;
            for (int i = 0; i < elementos.Count; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = elementos[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(destino);
        }
    }
}

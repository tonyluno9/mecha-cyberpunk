using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// Crea los 8 implantes (D-tony-165), su catálogo y agrega ServicioImplantes a ServiciosProgresion.prefab (T-191).
    /// Idempotente: un asset que ya existe no se toca (Tony edita valores ahí), y el prefab solo se guarda si cambia algo.
    /// </summary>
    public static class CreadorImplantes
    {
        public const string Carpeta = "Assets/_Proyecto/Progresion/Datos/Implantes";
        public const string RutaCatalogo = Carpeta + "/CatalogoImplantes_Base.asset";
        private const string RutaPrefab = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const int LigaNivel1 = 1;
        private const int LigaNivel2 = 3;

        private static EfectosImplantes Efecto(float golpe = 1f, float correr = 1f, float salto = 1f, float reaccion = 1f,
            bool vida = false, bool numeros = false, float anticipacion = 0f,
            float alcance = 0f, float retroceso = 0f, float rodada = 0f, float recarga = 0f)
        {
            return new EfectosImplantes
            {
                multiplicadorGolpeAPie = golpe,
                multiplicadorCorrer = correr,
                multiplicadorSalto = salto,
                multiplicadorReaccionMecha = reaccion,
                verVidaRival = vida,
                vidaRivalEnNumeros = numeros,
                anticipacionAtaque = anticipacion,
                bonusAlcanceApuntado = alcance,
                reduccionRetroceso = retroceso,
                reduccionEnfriamientoRodada = rodada,
                reduccionRecarga = recarga
            };
        }

        private static DatosImplante[] Tabla()
        {
            return new[]
            {
                DatosImplante.Crear("implante_ojos_1", "OJOS DE LECHUZA", EspacioImplante.Ojos, 1, 2500, LigaNivel1,
                    "VES LA VIDA DEL RIVAL, APUNTAS DE MÁS LEJOS A PIE", Efecto(vida: true, anticipacion: 0.4f, alcance: 0.25f)),
                DatosImplante.Crear("implante_ojos_2", "OJOS DE ÁGUILA", EspacioImplante.Ojos, 2, 6000, LigaNivel2,
                    "LA VIDA EN NÚMEROS Y APUNTADO A PIE MUCHO MÁS LEJOS", Efecto(vida: true, numeros: true, anticipacion: 0.8f, alcance: 0.5f)),
                DatosImplante.Crear("implante_brazos_1", "BRAZOS DE PISTÓN", EspacioImplante.Brazos, 1, 2000, LigaNivel1,
                    "TUS PUÑOS PEGAN MÁS Y LA PISTOLA PATEA MENOS", Efecto(golpe: 1.5f, retroceso: 0.3f)),
                DatosImplante.Crear("implante_brazos_2", "BRAZOS DE PRENSA", EspacioImplante.Brazos, 2, 5000, LigaNivel2,
                    "TUS PUÑOS PEGAN EL DOBLE Y LA PISTOLA CASI NO PATEA", Efecto(golpe: 2f, retroceso: 0.5f)),
                DatosImplante.Crear("implante_piernas_1", "PIERNAS DE RESORTE", EspacioImplante.Piernas, 1, 2000, LigaNivel1,
                    "CORRES, SALTAS Y RUEDAS MÁS", Efecto(correr: 1.2f, salto: 1.3f, rodada: 0.25f)),
                DatosImplante.Crear("implante_piernas_2", "PIERNAS DE CHAPULÍN", EspacioImplante.Piernas, 2, 5000, LigaNivel2,
                    "CORRES, SALTAS Y RUEDAS MUCHO MÁS", Efecto(correr: 1.35f, salto: 1.6f, rodada: 0.4f)),
                DatosImplante.Crear("implante_sistemanervioso_1", "SINAPSIS CALLEJERA", EspacioImplante.SistemaNervioso, 1, 3000, LigaNivel1,
                    "RECARGAS MÁS RÁPIDO Y REACCIONAS MÁS EN EL MECHA", Efecto(reaccion: 0.85f, recarga: 0.2f)),
                DatosImplante.Crear("implante_sistemanervioso_2", "SINAPSIS MILITAR", EspacioImplante.SistemaNervioso, 2, 7000, LigaNivel2,
                    "RECARGA RELÁMPAGO Y REFLEJOS DE SOLDADO EN EL MECHA", Efecto(reaccion: 0.7f, recarga: 0.35f)),
            };
        }

        // T-430: los assets que ya existían reciben los efectos a pie nuevos y la frase; lo demás (precio, liga) no se toca.
        private static void ActualizarAPie(DatosImplante actual, DatosImplante tabla)
        {
            var so = new SerializedObject(actual);
            SerializedProperty ef = so.FindProperty("efectos");
            ef.FindPropertyRelative("bonusAlcanceApuntado").floatValue = tabla.Efectos.bonusAlcanceApuntado;
            ef.FindPropertyRelative("reduccionRetroceso").floatValue = tabla.Efectos.reduccionRetroceso;
            ef.FindPropertyRelative("reduccionEnfriamientoRodada").floatValue = tabla.Efectos.reduccionEnfriamientoRodada;
            ef.FindPropertyRelative("reduccionRecarga").floatValue = tabla.Efectos.reduccionRecarga;
            so.FindProperty("descripcion").stringValue = tabla.Descripcion;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(actual);
        }

        [MenuItem("Mecha Cyberpunk/Crear T-191 implantes")]
        public static void Crear()
        {
            if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/_Proyecto/Progresion/Datos", "Implantes");

            var lista = new List<DatosImplante>();
            foreach (DatosImplante d in Tabla())
            {
                string ruta = Carpeta + "/DatosImplante_" + d.Id + ".asset";
                var actual = AssetDatabase.LoadAssetAtPath<DatosImplante>(ruta);
                if (actual != null)
                {
                    ActualizarAPie(actual, d);
                    lista.Add(actual);
                    Object.DestroyImmediate(d);
                }
                else
                {
                    AssetDatabase.CreateAsset(d, ruta);
                    lista.Add(d);
                }
            }

            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoImplantes>(RutaCatalogo);
            if (catalogo == null)
            {
                catalogo = ScriptableObject.CreateInstance<CatalogoImplantes>();
                AssetDatabase.CreateAsset(catalogo, RutaCatalogo);
                var so = new SerializedObject(catalogo);
                SerializedProperty prop = so.FindProperty("implantes");
                prop.arraySize = lista.Count;
                for (int i = 0; i < lista.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = lista[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalogo);
            }
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                bool cambio = false;
                var servicio = raiz.GetComponent<ServicioImplantes>();
                if (servicio == null)
                {
                    servicio = raiz.AddComponent<ServicioImplantes>();
                    cambio = true;
                }
                var soServicio = new SerializedObject(servicio);
                SerializedProperty propCatalogo = soServicio.FindProperty("catalogo");
                if (propCatalogo.objectReferenceValue != catalogo)
                {
                    propCatalogo.objectReferenceValue = catalogo;
                    soServicio.ApplyModifiedPropertiesWithoutUndo();
                    cambio = true;
                }
                if (cambio) PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Progresion] CreadorImplantes: " + lista.Count + " implantes.");
        }

        /// <summary>Entrada para -executeMethod en batchmode.</summary>
        public static void CrearBatch() => Crear();
    }
}

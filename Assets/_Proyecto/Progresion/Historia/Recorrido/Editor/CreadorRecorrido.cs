using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido.Editor
{
    /// <summary>
    /// T-1625: crea (o actualiza) los assets del recorrido nuevo: las 9 cinemáticas C01 a C09, Resources/DatosRecorrido (con los prefabs de SKULLS del paso 3)
    /// y los prefabs marcadores. Se corre con <c>-executeMethod MechaCyberpunk.Progresion.Recorrido.Editor.CreadorRecorrido.Aplicar</c> o desde el menú.
    /// No toca escenas: los marcadores se colocan en el mapa aparte (ver el reporte de la tarea).
    /// </summary>
    public static class CreadorRecorrido
    {
        private const string Carpeta = "Assets/_Proyecto/Progresion/Historia";
        private const string CarpetaCinematicas = Carpeta + "/Cinematicas";
        private const string CarpetaRecursos = Carpeta + "/Recorrido/Resources";
        private const string CarpetaPrefabs = Carpeta + "/Recorrido/Prefabs";
        private static readonly string[] RutasSkulls =
        {
            "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls_E19_PandilleroSkulls_a_2.prefab",
            "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls_E19_PandilleroSkulls_b.prefab",
        };

        [MenuItem("Mecha Cyberpunk/Historia/Crear assets del recorrido")]
        public static void Aplicar()
        {
            Asegurar(CarpetaCinematicas);
            Asegurar(CarpetaRecursos);
            Asegurar(CarpetaPrefabs);

            var cinematicas = new List<DatosCinematica>();
            foreach (TablaRecorrido.FilaCinematica fila in TablaRecorrido.Cinematicas)
            {
                string ruta = CarpetaCinematicas + "/DatosCinematica_" + fila.Id + "_" + Limpio(fila.Nombre) + ".asset";
                DatosCinematica asset = AssetDatabase.LoadAssetAtPath<DatosCinematica>(ruta);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<DatosCinematica>();
                    AssetDatabase.CreateAsset(asset, ruta);
                }
                asset.Configurar(fila.Id, fila.Nombre, fila.Texto, fila.Duracion);
                EditorUtility.SetDirty(asset);
                cinematicas.Add(asset);
            }

            var skulls = new List<GameObject>();
            foreach (string ruta in RutasSkulls)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab != null)
                {
                    skulls.Add(prefab);
                }
                else
                {
                    Debug.LogWarning("[Historia] No está el prefab de SKULL " + ruta);
                }
            }

            string rutaDatos = CarpetaRecursos + "/" + DatosRecorrido.NombreRecurso + ".asset";
            DatosRecorrido datos = AssetDatabase.LoadAssetAtPath<DatosRecorrido>(rutaDatos);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosRecorrido>();
                AssetDatabase.CreateAsset(datos, rutaDatos);
            }
            datos.Configurar(cinematicas.ToArray(), skulls.ToArray());
            EditorUtility.SetDirty(datos);

            CrearMarcador(MarcadorHistoria.IdMolestia);
            CrearMarcador(MarcadorHistoria.IdRanura);
            CrearMarcador(MarcadorHistoria.IdTorre);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Historia] Recorrido: " + cinematicas.Count + " cinemáticas, " + skulls.Count + " prefabs de SKULL y 3 marcadores.");
        }

        private static void CrearMarcador(string id)
        {
            string ruta = CarpetaPrefabs + "/" + id + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) != null)
            {
                return;
            }
            var objeto = new GameObject(id);
            objeto.AddComponent<MarcadorHistoria>().FijarId(id);
            PrefabUtility.SaveAsPrefabAsset(objeto, ruta);
            Object.DestroyImmediate(objeto);
        }

        private static void Asegurar(string carpeta)
        {
            string[] partes = carpeta.Split('/');
            string actual = partes[0];
            for (int i = 1; i < partes.Length; i++)
            {
                string siguiente = actual + "/" + partes[i];
                if (!AssetDatabase.IsValidFolder(siguiente))
                {
                    AssetDatabase.CreateFolder(actual, partes[i]);
                }
                actual = siguiente;
            }
        }

        private static string Limpio(string nombre)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in nombre.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (char.IsLetterOrDigit(c) && c < 128)
                {
                    sb.Append(char.ToUpperInvariant(c) == c ? c : char.ToUpperInvariant(c));
                }
                else if (c == ' ')
                {
                    sb.Append('_');
                }
            }
            return sb.ToString();
        }
    }
}

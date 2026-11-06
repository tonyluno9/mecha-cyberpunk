using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Clinica;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-815: hornea el cuerpo de cada personaje del jugador (Visual_woman y Visual_man, en el orden de Apariencia.Cara) a una malla estática para el
    /// holograma de la clínica: copia solo las mallas (la de cada SkinnedMeshRenderer en su pose de enlace, más las MeshFilter), sin scripts,
    /// huesos, materiales ni colisiones, y reparte los triángulos en 4 submallas por <see cref="EspacioImplante"/> con
    /// <see cref="ReglaMenuImplantes.Region"/>. Guarda Assets/Arte/UI/Hologramas/CuerpoHolograma_*.asset (reusa el asset si ya existe) y llena la
    /// config de la clínica (cuerpos, shader holográfico y material aditivo). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarS2nCuerpoHolograma.Hornear
    /// </summary>
    public static class ImportarS2nCuerpoHolograma
    {
        public const string CarpetaDestino = "Assets/Arte/UI/Hologramas/";
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigPantallaClinica_Base.asset";
        public const string RutaMaterialHolograma = "Assets/_Proyecto/UI/Materiales/HologramaArma.mat";
        public const string RutaMaterialAditivo = "Assets/_Proyecto/UI/Resources/UIAditivo.mat";
        private const int MaximoIndices16 = 65535;

        /// <summary>Los visuales en el orden de Apariencia.Cara (0 = mujer, 1 = hombre).</summary>
        public static readonly string[] Visuales =
        {
            "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_woman.prefab",
            "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_man.prefab"
        };

        [MenuItem("Mecha Cyberpunk/S2n/Cuerpo holográfico de la clínica (T-815)")]
        public static void Hornear()
        {
            List<Mesh> cuerpos = new List<Mesh>();
            foreach (string ruta in Visuales)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab == null)
                {
                    Debug.LogWarning("[Arte] Falta " + ruta + ": el holograma usa el otro cuerpo.");
                    continue;
                }
                string destino = CarpetaDestino + "CuerpoHolograma_" + prefab.name.Replace("Visual_", string.Empty) + ".asset";
                cuerpos.Add(HornearUno(prefab, destino));
            }

            ConfigPantallaClinica config = AssetDatabase.LoadAssetAtPath<ConfigPantallaClinica>(RutaConfig);
            if (config == null)
            {
                Debug.LogError("[Arte] Falta " + RutaConfig);
                return;
            }
            SerializedObject so = new SerializedObject(config);
            SerializedProperty lista = so.FindProperty("cuerpos");
            lista.arraySize = cuerpos.Count;
            for (int i = 0; i < cuerpos.Count; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = cuerpos[i];
            }
            so.FindProperty("materialHolograma").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialHolograma);
            so.FindProperty("materialAditivoUI").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialAditivo);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] Cuerpo holográfico: " + cuerpos.Count + " cuerpos en " + RutaConfig);
        }

        private static Mesh HornearUno(GameObject prefab, string destino)
        {
            // Instanciado en el editor sin jugar: no corre Awake ni Start de sus scripts; se destruye al terminar.
            GameObject copia = Object.Instantiate(prefab);
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normales = new List<Vector3>();
            List<int> triangulos = new List<int>();
            try
            {
                copia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (SkinnedMeshRenderer sk in copia.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (sk.sharedMesh == null || !sk.enabled)
                    {
                        continue;
                    }
                    // La malla compartida está en la pose de enlace (la T de Mixamo): los brazos abiertos separan bien los espacios.
                    Agregar(sk.sharedMesh, sk.transform.localToWorldMatrix, vertices, normales, triangulos);
                }
                foreach (MeshFilter mf in copia.GetComponentsInChildren<MeshFilter>())
                {
                    MeshRenderer r = mf.GetComponent<MeshRenderer>();
                    if (mf.sharedMesh != null && r != null && r.enabled)
                    {
                        Agregar(mf.sharedMesh, mf.transform.localToWorldMatrix, vertices, normales, triangulos);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(copia);
            }

            // Reparte cada triángulo por la región de su centro, medida contra el alto del cuerpo.
            Bounds limites = new Bounds(vertices.Count > 0 ? vertices[0] : Vector3.zero, Vector3.zero);
            foreach (Vector3 v in vertices)
            {
                limites.Encapsulate(v);
            }
            float alto = Mathf.Max(limites.size.y, 0.0001f);
            List<int>[] regiones = new List<int>[VistaHologramaCuerpo.Espacios];
            for (int i = 0; i < regiones.Length; i++)
            {
                regiones[i] = new List<int>();
            }
            for (int t = 0; t + 2 < triangulos.Count; t += 3)
            {
                Vector3 centro = (vertices[triangulos[t]] + vertices[triangulos[t + 1]] + vertices[triangulos[t + 2]]) / 3f;
                EspacioImplante region = ReglaMenuImplantes.Region((centro.y - limites.min.y) / alto, (centro.x - limites.center.x) / alto);
                List<int> lista = regiones[(int)region];
                lista.Add(triangulos[t]);
                lista.Add(triangulos[t + 1]);
                lista.Add(triangulos[t + 2]);
            }

            Mesh malla = AssetDatabase.LoadAssetAtPath<Mesh>(destino);
            bool nueva = malla == null;
            if (nueva)
            {
                malla = new Mesh();
            }
            malla.Clear();
            malla.name = System.IO.Path.GetFileNameWithoutExtension(destino);
            malla.indexFormat = vertices.Count > MaximoIndices16 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            malla.SetVertices(vertices);
            malla.SetNormals(normales);
            malla.subMeshCount = regiones.Length;
            for (int i = 0; i < regiones.Length; i++)
            {
                malla.SetTriangles(regiones[i], i, true);
            }
            malla.RecalculateBounds();
            if (nueva)
            {
                AssetDatabase.CreateAsset(malla, destino);
            }
            else
            {
                EditorUtility.SetDirty(malla);
            }
            Debug.Log("[Arte] " + destino + ": " + vertices.Count + " vértices, alto " + alto.ToString("F2") + " m, triángulos por espacio "
                + regiones[0].Count / 3 + "/" + regiones[1].Count / 3 + "/" + regiones[2].Count / 3 + "/" + regiones[3].Count / 3);
            return malla;
        }

        private static void Agregar(Mesh malla, Matrix4x4 aMundo, List<Vector3> vertices, List<Vector3> normales, List<int> triangulos)
        {
            int inicio = vertices.Count;
            Vector3[] v = malla.vertices;
            Vector3[] n = malla.normals;
            for (int i = 0; i < v.Length; i++)
            {
                vertices.Add(aMundo.MultiplyPoint3x4(v[i]));
                normales.Add(i < n.Length ? aMundo.MultiplyVector(n[i]).normalized : Vector3.up);
            }
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                if (malla.GetTopology(s) != MeshTopology.Triangles)
                {
                    continue;
                }
                foreach (int indice in malla.GetTriangles(s))
                {
                    triangulos.Add(inicio + indice);
                }
            }
        }
    }
}

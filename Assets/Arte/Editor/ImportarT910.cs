using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-910 (B7): mete los modelos nuevos de calle y casino desde los zips de imageassets/ (semáforo ES01, basura BS01a-e, pasarela PS01,
    /// puerta de cristal PC01 y la lata O01). Usa el flujo de <see cref="ImportarS2p"/> (hijo Visual, URP Lit, pivote en la base, medida real).
    /// imageassets/ vive en la carpeta principal: se busca con <see cref="ImportarS1n.Buscar"/> y en ../../imageassets/ (worktree .sesiones/s1).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarT910.Importar
    /// </summary>
    public static class ImportarT910
    {
        public const string Basura = "BS01";

        public static readonly (string zip, string codigo, ArteT250.Eje eje, float medida)[] Piezas =
        {
            ("modelos3d/calle/ES01.zip", "ES01_Semaforo", ArteT250.Eje.Alto, 4.5f),
            ("modelos3d/calle/BS01a.zip", "BS01a_Basura", ArteT250.Eje.Largo, 0.25f),
            ("modelos3d/calle/BS01b.zip", "BS01b_Basura", ArteT250.Eje.Largo, 0.25f),
            ("modelos3d/calle/BS01c.zip", "BS01c_Basura", ArteT250.Eje.Largo, 0.25f),
            ("modelos3d/calle/BS01d.zip", "BS01d_Basura", ArteT250.Eje.Largo, 0.25f),
            ("modelos3d/calle/BS01e.zip", "BS01e_Basura", ArteT250.Eje.Largo, 0.25f),
            ("modelos3d/calle/PS01.zip", "PS01_Pasarela", ArteT250.Eje.Largo, 20f),
            ("modelos3d/calle/PC01.zip", "PC01_PuertaCristal", ArteT250.Eje.Alto, 2.2f),
            ("modelos3d/O01.zip", "O01_BebidaRayo", ArteT250.Eje.Alto, 0.12f),
        };

        public static string Ruta(string relativo)
        {
            return ImportarS1n.Buscar(relativo) ?? (File.Exists("../../imageassets/" + relativo) ? "../../imageassets/" + relativo : null);
        }

        public static void Revisar()
        {
            ImportarS2p.Revisar(Piezas.Select(p => new ImportarS2p.Entrada("", p.codigo, p.eje, p.medida)).ToArray());
        }

        /// <summary>Mide dónde están las lentes del semáforo: los vértices de ES01 cuyo color de textura es rojo vivo (la lente roja viene pintada encendida).</summary>
        public static void MedirSemaforo()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Calle/Prefabs/ES01_Semaforo.prefab");
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes("Assets/Arte/Calle/ES01_Semaforo/ES01_Semaforo_basecolor.jpg"));
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            MeshFilter mf = g.GetComponentInChildren<MeshFilter>();
            Mesh m = mf.sharedMesh;
            Vector3[] v = m.vertices;
            Vector2[] uv = m.uv;
            Vector3 suma = Vector3.zero;
            Vector3 min = Vector3.one * 1e9f, max = -Vector3.one * 1e9f;
            int n = 0;
            for (int i = 0; i < v.Length; i++)
            {
                Color c = tex.GetPixelBilinear(uv[i].x, uv[i].y);
                if (c.r > 0.6f && c.g < 0.3f && c.b < 0.3f)
                {
                    Vector3 w = mf.transform.TransformPoint(v[i]);
                    suma += w;
                    min = Vector3.Min(min, w);
                    max = Vector3.Max(max, w);
                    n++;
                }
            }
            Bounds b = ArteT250.Limites(g);
            Debug.Log("[T910] lente roja: " + n + " vértices, centro " + (suma / Mathf.Max(1, n)).ToString("F3") + " min " + min.ToString("F3") + " max " + max.ToString("F3") + " | caja " + b.center.ToString("F3") + " tam " + b.size.ToString("F3"));
            // Mediana de x de los vértices entre 1.0 y 1.1 m (el poste).
            var xs = new System.Collections.Generic.List<float>();
            foreach (Vector3 p in v)
            {
                Vector3 w = mf.transform.TransformPoint(p);
                if (w.y > 1.0f && w.y < 1.1f)
                {
                    xs.Add(w.x);
                }
            }
            xs.Sort();
            Debug.Log("[T910] poste: " + xs.Count + " vértices, x min " + (xs.Count > 0 ? xs[0] : 0) + " mediana " + (xs.Count > 0 ? xs[xs.Count / 2] : 0) + " max " + (xs.Count > 0 ? xs[xs.Count - 1] : 0));
            Object.DestroyImmediate(g);
        }


        public const string CarpetaPrefabs = "Assets/Arte/Calle/Prefabs/";
        public const string PrefabPuertaCorrediza = CarpetaPrefabs + "PC01_PuertaCorrediza.prefab";
        public const string MallasPuerta = "Assets/Arte/Calle/PC01_PuertaCristal/PC01_Hojas.asset";
        /// <summary>Centro del poste del semáforo ES01 respecto al pivote del prefab (medido con <see cref="MedirSemaforo"/>).</summary>
        public static readonly Vector3 CajaPoste = new Vector3(0.3f, 2.25f, 0f);
        /// <summary>Región de cristal de PC01 (m, con el pivote al centro de la base): lo de adentro son las dos hojas, lo de afuera el marco.</summary>
        public const float CristalMitadAncho = 0.93f;
        public const float CristalDesde = 0.04f;
        public const float CristalHasta = 1.84f;

        /// <summary>Colliders de cada prefab (la caja de T-250 estorbaría) y el prefab de la puerta de dos hojas.</summary>
        public static void Preparar()
        {
            foreach (var p in Piezas)
            {
                string ruta = CarpetaPrefabs + p.codigo + ".prefab";
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                foreach (Collider c in raiz.GetComponents<Collider>())
                {
                    Object.DestroyImmediate(c);
                }
                if (p.codigo.StartsWith("ES01"))
                {
                    BoxCollider caja = raiz.AddComponent<BoxCollider>();
                    caja.center = CajaPoste;
                    caja.size = new Vector3(0.8f, 4.5f, 0.8f);
                }
                else if (p.codigo.StartsWith("PS01"))
                {
                    foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(true))
                    {
                        MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
                        if (mc == null)
                        {
                            mc = mf.gameObject.AddComponent<MeshCollider>();
                        }
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            foreach (string fbx in new[] { "PS01_Pasarela", "PC01_PuertaCristal" })
            {
                string r = "Assets/Arte/Calle/" + fbx + "/" + fbx + ".fbx";
                ModelImporter imp = AssetImporter.GetAtPath(r) as ModelImporter;
                if (imp != null && !imp.isReadable)
                {
                    imp.isReadable = true;
                    imp.SaveAndReimport();
                }
            }
            CrearPuertaCorrediza();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Parte PC01 en tres mallas (marco fijo y dos hojas) cortando los triángulos del cristal por el centro, y arma el prefab PC01_PuertaCorrediza.</summary>
        private static void CrearPuertaCorrediza()
        {
            GameObject origen = PrefabUtility.LoadPrefabContents(CarpetaPrefabs + "PC01_PuertaCristal.prefab");
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Calle/PC01_PuertaCristal/PC01_PuertaCristal.mat");
            MeshFilter mf = origen.GetComponentInChildren<MeshFilter>();
            Mesh m = mf.sharedMesh;
            Matrix4x4 aRaiz = origen.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            Vector3[] v = m.vertices;
            Vector3[] n = m.normals;
            Vector2[] uv = m.uv;
            int[] t = m.triangles;
            System.Collections.Generic.List<int>[] grupos = { new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>() };
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 c = (aRaiz.MultiplyPoint3x4(v[t[i]]) + aRaiz.MultiplyPoint3x4(v[t[i + 1]]) + aRaiz.MultiplyPoint3x4(v[t[i + 2]])) / 3f;
                bool cristal = Mathf.Abs(c.x) < CristalMitadAncho && c.y > CristalDesde && c.y < CristalHasta;
                int g = !cristal ? 0 : c.x < 0f ? 1 : 2;
                grupos[g].Add(t[i]);
                grupos[g].Add(t[i + 1]);
                grupos[g].Add(t[i + 2]);
            }
            string[] nombres = { "PC01_Marco", "PC01_HojaIzq", "PC01_HojaDer" };
            AssetDatabase.DeleteAsset(MallasPuerta);
            GameObject raiz = new GameObject("PC01_PuertaCorrediza");
            for (int g = 0; g < 3; g++)
            {
                var mapa = new System.Collections.Generic.Dictionary<int, int>();
                var vv = new System.Collections.Generic.List<Vector3>();
                var nn = new System.Collections.Generic.List<Vector3>();
                var uu = new System.Collections.Generic.List<Vector2>();
                var ii = new System.Collections.Generic.List<int>();
                foreach (int k in grupos[g])
                {
                    if (!mapa.TryGetValue(k, out int nuevo))
                    {
                        nuevo = vv.Count;
                        mapa[k] = nuevo;
                        vv.Add(aRaiz.MultiplyPoint3x4(v[k]));
                        nn.Add(aRaiz.MultiplyVector(n[k]).normalized);
                        uu.Add(uv[k]);
                    }
                    ii.Add(nuevo);
                }
                Mesh parte = new Mesh { name = nombres[g], indexFormat = vv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                parte.SetVertices(vv);
                parte.SetNormals(nn);
                parte.SetUVs(0, uu);
                parte.SetTriangles(ii, 0);
                parte.RecalculateBounds();
                if (g == 0)
                {
                    AssetDatabase.CreateAsset(parte, MallasPuerta);
                }
                else
                {
                    AssetDatabase.AddObjectToAsset(parte, MallasPuerta);
                }
                GameObject hijo = new GameObject(nombres[g]);
                hijo.transform.SetParent(raiz.transform, false);
                hijo.AddComponent<MeshFilter>().sharedMesh = parte;
                hijo.AddComponent<MeshRenderer>().sharedMaterial = material;
                Informe(nombres[g] + ": " + ii.Count / 3 + " triángulos");
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabPuertaCorrediza);
            Object.DestroyImmediate(raiz);
            PrefabUtility.UnloadPrefabContents(origen);
        }

        private static void Informe(string s)
        {
            Debug.Log("[T910] " + s);
        }

        public static void Todo()
        {
            Importar();
            Preparar();
        }

        public static void RevisarExpendedora()
        {
            ImportarS2p.Revisar(new[] { new ImportarS2p.Entrada("", "CY06_Expendedora", ArteT250.Eje.Alto, 2f) });
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(KitS2c.Prefab("CY06_Expendedora"));
            Debug.Log("[T910] CY06 caja " + ArteT250.Limites(g).ToString());
        }

        public static void Importar()
        {
            var entradas = Piezas.Select(p => new ImportarS2p.Entrada(Ruta(p.zip) ?? p.zip, p.codigo, p.eje, p.medida)).ToArray();
            ImportarS2p.Importar(entradas);
            foreach (var e in entradas)
            {
                ModelImporter imp = AssetImporter.GetAtPath(e.Modelo.RutaFbx) as ModelImporter;
                int tris = AssetDatabase.LoadAllAssetsAtPath(e.Modelo.RutaFbx).OfType<Mesh>().Sum(m => m.triangles.Length / 3);
                if (imp != null && tris > 15000 && !imp.generateMeshLods)
                {
                    imp.generateMeshLods = true;
                    imp.SaveAndReimport();
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] T-910 importado:\n" + ImportarS2p.Informe);
        }
    }
}

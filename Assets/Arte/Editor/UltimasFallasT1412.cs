using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1412: últimas fallas del EditMode del 19-nov (docs/tareas/hechas/T-1412-ultimas-fallas.md). Se corre DESPUÉS de las rondas de zonas y de ArreglosT1411. Idempotente.
    /// 1) Puerto: los contenedores que PU08 reemplazó vuelven a llamarse PT01_Contenedor* (con sufijo, apagados) para que las filas y pilas de T-1366 estén completas.
    /// 2) Postes de las zonas: sodio a 400 de intensidad y 22 m (Cerro 18 m).
    /// 3) Cerro: ninguna luz pasa de 18 m.
    /// 4) MeshCollider de mallas degeneradas en los prefabs de zona: BoxCollider ajustado, o fuera si la malla está vacía.
    /// 5) Materiales HaloLuz sin textura (T1396_HaloRojo, T1387_HaloRojo, halos de drones): se les pone el polvo de la niebla.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.UltimasFallasT1412.Aplicar. Informe: Logs/t1412-aplicar.txt.
    /// </summary>
    public static class UltimasFallasT1412
    {
        private const string CarpetaZonas = "Assets/Arte/Mundo/Zonas";
        private const string RutaPuerto = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab";
        private const string RutaInforme = "Logs/t1412-aplicar.txt";
        private const float AreaMinima = 1e-8f;
        private const float AlcanceZona = 22f;

        private static readonly StringBuilder informe = new StringBuilder();
        private static readonly Dictionary<int, bool> cache = new Dictionary<int, bool>();

        // ---------------------------------------------------------------- Mallas

        /// <summary>
        /// True si PhysX puede cocinar la malla: tiene triángulos, vértices finitos y al menos un triángulo con área.
        /// Las pruebas que ponen MeshCollider temporales a todo lo visible la usan para no disparar "[Physics.PhysX] cleaning the mesh failed".
        /// </summary>
        public static bool MallaCocinable(Mesh m)
        {
            if (m == null)
            {
                return false;
            }
            int id = m.GetInstanceID();
            if (cache.TryGetValue(id, out bool ok))
            {
                return ok;
            }
            ok = CalcularCocinable(m);
            cache[id] = ok;
            return ok;
        }

        private static bool CalcularCocinable(Mesh m)
        {
            try
            {
                if (m.vertexCount < 3 || m.subMeshCount == 0)
                {
                    return false;
                }
                Vector3[] v = m.vertices;
                for (int i = 0; i < v.Length; i++)
                {
                    if (float.IsNaN(v[i].x) || float.IsNaN(v[i].y) || float.IsNaN(v[i].z)
                        || float.IsInfinity(v[i].x) || float.IsInfinity(v[i].y) || float.IsInfinity(v[i].z))
                    {
                        return false;
                    }
                }
                for (int s = 0; s < m.subMeshCount; s++)
                {
                    MeshTopology topo = m.GetTopology(s);
                    if (topo != MeshTopology.Triangles && topo != MeshTopology.Quads)
                    {
                        continue;
                    }
                    int[] t = m.GetIndices(s);
                    int paso = topo == MeshTopology.Quads ? 4 : 3;
                    for (int i = 0; i + 2 < t.Length; i += paso)
                    {
                        Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                        if (Vector3.Cross(b - a, c - a).magnitude * 0.5f > AreaMinima)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ---------------------------------------------------------------- Aplicar

        [MenuItem("Mecha Cyberpunk/T1412/Aplicar ultimas fallas")]
        public static void Aplicar()
        {
            informe.Clear();
            informe.AppendLine("T-1412 ultimas fallas");
            Paso("1 Puerto: contenedores reemplazados conservan su nombre", ContenedoresDelPuerto);
            Paso("2 y 3 Luces de las zonas: postes a 400 y alcance (Cerro 18 m)", LucesDeZonas);
            Paso("4 MeshCollider degenerados en los prefabs de zona", CollidersDegenerados);
            Paso("5 Halos con textura", HalosConTextura);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Arte] T-1412 " + informe);
        }

        private static void Paso(string nombre, Action accion)
        {
            try
            {
                informe.AppendLine("== " + nombre);
                accion();
            }
            catch (Exception e)
            {
                informe.AppendLine("ERROR en '" + nombre + "': " + e);
                Debug.LogError("[Arte] T-1412 " + nombre + ": " + e);
            }
        }

        private static IEnumerable<string> Prefabs()
        {
            return AssetDatabase.FindAssets("t:Prefab", new[] { CarpetaZonas }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetDirectoryName(p).Replace('\\', '/') == CarpetaZonas).OrderBy(p => p);
        }

        // 1
        private static void ContenedoresDelPuerto()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPuerto);
            try
            {
                int n = 0;
                Transform padre = raiz.transform.Find("Contenedores");
                if (padre == null)
                {
                    informe.AppendLine("No hay grupo Contenedores.");
                    return;
                }
                const string p87 = "T1387_Reemplazado_", p96 = "T1396_Reemplazado_";
                foreach (Transform t in padre.GetComponentsInChildren<Transform>(true).ToList())
                {
                    string nombre = t.name;
                    string sufijo = null;
                    while (true)
                    {
                        if (nombre.StartsWith(p96))
                        {
                            nombre = nombre.Substring(p96.Length);
                            sufijo = ZonaPuertoT1396.SufijoViejo;
                        }
                        else if (nombre.StartsWith(p87))
                        {
                            nombre = nombre.Substring(p87.Length);
                            sufijo = sufijo ?? ZonaPuertoT1387.SufijoViejo;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (sufijo != null && nombre.StartsWith("PT01_Contenedor"))
                    {
                        t.name = nombre + sufijo;
                        t.gameObject.SetActive(false);
                        n++;
                    }
                }
                informe.AppendLine("Contenedores reemplazados que recuperan su nombre PT01_Contenedor*: " + n + " (siguen apagados; el PU08 es el visual).");
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPuerto);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // 2 y 3
        private static void LucesDeZonas()
        {
            foreach (string ruta in Prefabs())
            {
                bool cerro = ruta == RendimientoCerroT1365.RutaPrefab;
                float tope = cerro ? RendimientoCerroT1365.AlcanceMaximoLuz : AlcanceZona;
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    int postes = 0, alcances = 0;
                    foreach (Light l in raiz.GetComponentsInChildren<Light>(true))
                    {
                        bool cambio = false;
                        if (ColocarS2oLuz.EsPoste(l) && !Mathf.Approximately(l.intensity, LookNocheT1392.IntensidadSodio) && !Mathf.Approximately(l.intensity, ColocarS2oLuz.IntensidadPoste))
                        {
                            l.intensity = LookNocheT1392.IntensidadSodio;
                            postes++;
                            cambio = true;
                        }
                        if (cerro && l.type != LightType.Directional && l.range > tope)
                        {
                            l.range = tope;
                            alcances++;
                            cambio = true;
                        }
                        else if (!cerro && ColocarS2oLuz.EsPoste(l) && l.range > tope)
                        {
                            l.range = tope;
                            alcances++;
                            cambio = true;
                        }
                        if (cambio)
                        {
                            l.shadows = LightShadows.None;
                        }
                    }
                    if (postes > 0 || alcances > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                    }
                    informe.AppendLine(Path.GetFileName(ruta) + ": postes a " + LookNocheT1392.IntensidadSodio + ": " + postes + "; alcances bajados a " + tope + " m: " + alcances);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
        }

        // 4
        private static void CollidersDegenerados()
        {
            foreach (string ruta in Prefabs())
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    int cajas = 0, quitados = 0, apagados = 0;
                    foreach (MeshCollider mc in raiz.GetComponentsInChildren<MeshCollider>(true).ToList())
                    {
                        if (MallaCocinable(mc.sharedMesh))
                        {
                            continue;
                        }
                        GameObject go = mc.gameObject;
                        MeshFilter mf = go.GetComponent<MeshFilter>();
                        bool trigger = mc.isTrigger;
                        bool activo = mc.enabled;
                        Mesh malla = mc.sharedMesh != null ? mc.sharedMesh : (mf != null ? mf.sharedMesh : null);
                        Bounds b = malla != null ? malla.bounds : default;
                        bool util = malla != null && malla.vertexCount > 0 && b.size.sqrMagnitude > 1e-6f
                            && !float.IsNaN(b.size.x + b.size.y + b.size.z) && !float.IsInfinity(b.size.x + b.size.y + b.size.z);
                        try
                        {
                            UnityEngine.Object.DestroyImmediate(mc);
                            if (util)
                            {
                                BoxCollider bc = go.AddComponent<BoxCollider>();
                                bc.center = b.center;
                                bc.size = new Vector3(Mathf.Max(b.size.x, 0.02f), Mathf.Max(b.size.y, 0.02f), Mathf.Max(b.size.z, 0.02f));
                                bc.isTrigger = trigger;
                                bc.enabled = activo;
                                cajas++;
                            }
                            else
                            {
                                quitados++;
                            }
                        }
                        catch (Exception)
                        {
                            // Collider de un prefab anidado: no se puede quitar desde aquí; se apaga.
                            if (mc != null)
                            {
                                mc.enabled = false;
                                apagados++;
                            }
                        }
                        informe.AppendLine("  " + Path.GetFileName(ruta) + ": " + go.name + " (" + (malla != null ? malla.name : "sin malla") + ") -> " + (util ? "BoxCollider" : "sin collider"));
                    }
                    if (cajas + quitados + apagados > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                    }
                    informe.AppendLine(Path.GetFileName(ruta) + ": cajas " + cajas + ", quitados " + quitados + ", apagados " + apagados);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
        }

        // 5
        private static void HalosConTextura()
        {
            Texture2D ruido = KitS2c.TexMuro();
            int n = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Arte" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m == null || m.shader == null || m.shader.name != "MechaCyberpunk/Mundo/HaloLuz" || !m.HasProperty("_Ruido") || m.GetTexture("_Ruido") != null)
                {
                    continue;
                }
                m.SetTexture("_Ruido", ruido);
                m.SetTextureScale("_Ruido", new Vector2(3f, 1f));
                EditorUtility.SetDirty(m);
                n++;
                informe.AppendLine("  " + ruta);
            }
            informe.AppendLine("Materiales HaloLuz sin textura arreglados: " + n);
        }
    }
}

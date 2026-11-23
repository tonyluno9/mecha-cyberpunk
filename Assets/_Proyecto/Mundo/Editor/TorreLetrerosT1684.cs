using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T47 (T-1684): la torre de Nova (TN01, Tripo) en el recinto de Nova junto a Historia_Torre, mucho más alta que cualquier edificio, con franja
    /// cian emisiva, luz en la base y haz arriba; y letreros nuevos LT05-LT09 pegados a fachadas de los barrios con neón, con emisión y una luz cada uno.
    /// Diagnostico (solo escribe Logs/t1684.txt), Aplicar (importa modelos, materiales y prefabs, coloca y guarda), Fotos (corrida APARTE).
    /// </summary>
    public static class TorreLetrerosT1684
    {
        public const string RutaLog = "Logs/t1684.txt";
        public const string Etiqueta = "[T-1684] ";

        // ------------------------------------------------------------------ diagnostico

        [MenuItem("Mecha Cyberpunk/T-1684 Torre y letreros: diagnostico")]
        public static void Diagnostico()
        {
            ReemplazarEdificiosT1502.Abrir();
            Physics.SyncTransforms();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("== T-1684 DIAGNOSTICO");
            Rect linea = FueraDelMuroT1543.LineaDelMuro();
            sb.AppendLine("Linea del muro: " + linea);
            sb.AppendLine("Niebla: " + RenderSettings.fog + " modo " + RenderSettings.fogMode + " dens " + RenderSettings.fogDensity + " ini " + RenderSettings.fogStartDistance
                + " fin " + RenderSettings.fogEndDistance + " color " + RenderSettings.fogColor);
            Light[] luces = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            sb.AppendLine("Luces totales (incl. inactivas): " + luces.Length + ", activas " + luces.Count(l => l.isActiveAndEnabled));
            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                sb.AppendLine("Camara " + Ruta(c.transform) + " far " + c.farClipPlane);
            }
            foreach (string n in new[] { "Historia_Torre", "Recinto_JefeFinal", "RecintoJefe" })
            {
                foreach (Transform t in Todos().Where(x => x.name == n))
                {
                    sb.AppendLine(n + ": " + Ruta(t) + " pos " + t.position + " rot " + t.eulerAngles + " caja " + Caja(t));
                }
            }
            Transform ht = Todos().FirstOrDefault(x => x.name == "Historia_Torre");
            Vector3 centro = ht != null ? ht.position : new Vector3(117.5f, 0f, -43f);
            sb.AppendLine("-- Objetos (nivel 2-3) con renderers a < 70 m de " + centro);
            HashSet<Transform> vistos = new HashSet<Transform>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled)
                {
                    continue;
                }
                Vector3 c = r.bounds.center;
                if (new Vector2(c.x - centro.x, c.z - centro.z).magnitude > 70f || r.bounds.size.magnitude > 600f)
                {
                    continue;
                }
                Transform u = Nivel(r.transform, 3);
                if (vistos.Add(u))
                {
                    sb.AppendLine("  " + Ruta(u) + "  caja " + Caja(u));
                }
            }
            sb.AppendLine("-- Mas altos (renderers dentro del muro, < 600 m)");
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && r.bounds.size.magnitude < 600f && !(r is ParticleSystemRenderer) && linea.Contains(new Vector2(r.bounds.center.x, r.bounds.center.z))
                    && !Regex.IsMatch(Ruta(r.transform), "^(Realismo|T513_Fondo|AMB_|T1398)"))
                .OrderByDescending(r => r.bounds.max.y).Take(25))
            {
                sb.AppendLine("  " + r.bounds.max.y.ToString("F1") + "  " + Ruta(r.transform) + " centro " + r.bounds.center);
            }
            sb.AppendLine("-- Raices");
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                sb.AppendLine("  " + g.name + (g.activeSelf ? "" : " (inactiva)") + " hijos " + g.transform.childCount + " caja " + Caja(g.transform));
            }
            sb.AppendLine("-- Taquerias / tacos");
            foreach (Transform t in Todos().Where(x => Regex.IsMatch(x.name, "taco|taquer", RegexOptions.IgnoreCase)).Take(40))
            {
                sb.AppendLine("  " + Ruta(t) + " " + t.position);
            }
            sb.AppendLine("-- Calles / barrios (nombres con Calle_ o Barrio)");
            foreach (Transform t in Todos().Where(x => Regex.IsMatch(x.name, "^(Calle_|Barrio|Zona_|S2[a-z]_)", RegexOptions.IgnoreCase) && x.parent != null && x.parent.parent == null).Take(80))
            {
                sb.AppendLine("  " + Ruta(t) + " caja " + Caja(t));
            }
            sb.AppendLine("-- Materiales de letreros/neon");
            Dictionary<Material, int> mats = new Dictionary<Material, int>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && (Regex.IsMatch(m.name, "neon|led|letrero|anuncio|L1[0-9]", RegexOptions.IgnoreCase) || Regex.IsMatch(r.name, "neon|letrero|anuncio", RegexOptions.IgnoreCase)))
                    {
                        mats[m] = mats.TryGetValue(m, out int k) ? k + 1 : 1;
                    }
                }
            }
            foreach (KeyValuePair<Material, int> p in mats.OrderByDescending(p => p.Value))
            {
                Material m = p.Key;
                sb.AppendLine("  " + p.Value + "x " + m.name + " shader " + m.shader.name + " _EMISSION " + m.IsKeywordEnabled("_EMISSION") + " gi " + m.globalIlluminationFlags
                    + (m.HasProperty("_EmissionColor") ? " emi " + m.GetColor("_EmissionColor") : "") + " " + AssetDatabase.GetAssetPath(m));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, sb.ToString());
            Debug.Log(Etiqueta + "diagnostico en " + RutaLog);
        }

        // ------------------------------------------------------------------ modelos

        public const string CarpetaArte = "Assets/Arte/Mundo/";
        public static readonly string[] Modelos = { "TN01_TorreNova", "LT05_PantallaAnuncio", "LT06_LetreroVertical", "LT07_LetreroTaqueria", "LT09_AnuncioNova" };

        public static string Codigo(string modelo) => modelo.Substring(0, 4);
        public static string Carpeta(string modelo) => CarpetaArte + modelo + "/";
        public static string RutaFbx(string modelo) => Carpeta(modelo) + modelo + ".fbx";
        public static string RutaMaterial(string modelo) => Carpeta(modelo) + modelo + ".mat";
        public static string RutaPrefab(string modelo) => Carpeta(modelo) + modelo + ".prefab";

        /// <summary>Importa los FBX (sin materiales, sin animación, legibles) y hace el material URP Lit de cada uno con su emisión. Escribe las medidas crudas.</summary>
        [MenuItem("Mecha Cyberpunk/T-1684 Torre y letreros: importar")]
        public static void Importar()
        {
            AssetDatabase.Refresh();
            StringBuilder sb = new StringBuilder("== T-1684 IMPORTAR\n");
            foreach (string m in Modelos)
            {
                ModelImporter imp = AssetImporter.GetAtPath(RutaFbx(m)) as ModelImporter;
                if (imp == null)
                {
                    sb.AppendLine(m + ": FALTA el FBX");
                    continue;
                }
                if (imp.materialImportMode != ModelImporterMaterialImportMode.None || imp.importAnimation || imp.importCameras || imp.importLights || !imp.isReadable)
                {
                    imp.materialImportMode = ModelImporterMaterialImportMode.None;
                    imp.importAnimation = false;
                    imp.importCameras = false;
                    imp.importLights = false;
                    imp.isReadable = true;
                    imp.SaveAndReimport();
                }
                Material mat = HacerMaterial(m);
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx(m));
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                Bounds b = Caja(inst.transform);
                sb.AppendLine(m + ": caja cruda " + b + " hijos " + inst.GetComponentsInChildren<MeshFilter>().Length + " rot raiz " + inst.transform.localEulerAngles
                    + " tris " + inst.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh != null ? f.sharedMesh.triangles.Length / 3 : 0) + " material " + (mat != null));
                Object.DestroyImmediate(inst);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.AppendAllText(RutaLog, sb.ToString());
            Debug.Log(Etiqueta + sb);
        }

        private static Material HacerMaterial(string m)
        {
            string c = Codigo(m);
            string dir = Carpeta(m);
            string color = dir + c + "_basecolor.jpg";
            string normal = dir + c + "_normal.jpg";
            TextureImporter ni = AssetImporter.GetAtPath(normal) as TextureImporter;
            if (ni != null && ni.textureType != TextureImporterType.NormalMap)
            {
                ni.textureType = TextureImporterType.NormalMap;
                ni.SaveAndReimport();
            }
            Texture2D mascara = HacerMascara(dir + c + "_metallic.jpg", dir + c + "_roughness.jpg", dir + c + "_mascara.png");
            Texture2D emision = HacerEmision(color, dir + c + "_emision.png", c == "TN01");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial(m));
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, RutaMaterial(m));
            }
            Texture2D mapa = AssetDatabase.LoadAssetAtPath<Texture2D>(color);
            mat.SetTexture("_BaseMap", mapa);
            mat.SetTexture("_MainTex", mapa);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            mat.EnableKeyword("_NORMALMAP");
            if (mascara != null)
            {
                mat.SetTexture("_MetallicGlossMap", mascara);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Metallic", 1f);
                mat.SetFloat("_Smoothness", 1f);
            }
            if (emision != null)
            {
                mat.SetTexture("_EmissionMap", emision);
                mat.SetColor("_EmissionColor", Color.white * (c == "TN01" ? IntensidadTorre : IntensidadLetrero));
                mat.EnableKeyword("_EMISSION");
                // RealtimeEmissive: con None la reimportación pierde _EMISSION (memoria "Emisión URP por código").
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
            return mat;
        }

        /// <summary>Intensidad HDR de la emisión de la torre: la niebla exponencial (0.009) se come casi todo a 300-500 m; la franja cian tiene que pasar.</summary>
        public const float IntensidadTorre = 6f;
        public const float IntensidadLetrero = 2.2f;
        /// <summary>Intensidad de la luz chica frente a cada letrero (rango 5-6 m, sin sombras).</summary>
        public const float IntensidadLuzLetrero = 4f;

        /// <summary>Emisión: en letreros, los píxeles de color saturado o muy claros (el neón y la pantalla); en la torre, la franja cian a tope y el cuerpo blanco tenue.</summary>
        private static Texture2D HacerEmision(string rutaColor, string rutaSalida, bool torre)
        {
            if (!File.Exists(rutaSalida))
            {
                if (!File.Exists(rutaColor))
                {
                    return null;
                }
                Texture2D t = Leer(rutaColor, false);
                Color[] px = t.GetPixels();
                for (int i = 0; i < px.Length; i++)
                {
                    Color.RGBToHSV(px[i], out float h, out float s, out float v);
                    float k;
                    if (torre)
                    {
                        bool cian = px[i].b > 0.55f && px[i].b - px[i].r > 0.28f && px[i].g > 0.45f;
                        k = cian ? 1f : 0.06f;
                    }
                    else
                    {
                        k = Mathf.Clamp01(Mathf.InverseLerp(0.25f, 0.6f, s) * Mathf.InverseLerp(0.35f, 0.7f, v)) + (v > 0.9f && s < 0.15f ? 0.5f : 0f);
                        k = Mathf.Clamp01(k);
                    }
                    px[i] = new Color(px[i].r * k, px[i].g * k, px[i].b * k, 1f);
                }
                Texture2D o = new Texture2D(t.width, t.height, TextureFormat.RGBA32, false);
                o.SetPixels(px);
                o.Apply();
                File.WriteAllBytes(rutaSalida, o.EncodeToPNG());
                Object.DestroyImmediate(t);
                Object.DestroyImmediate(o);
                AssetDatabase.ImportAsset(rutaSalida, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(rutaSalida);
        }

        private static Texture2D HacerMascara(string rutaMetal, string rutaRugoso, string rutaSalida)
        {
            if (!File.Exists(rutaSalida))
            {
                if (!File.Exists(rutaMetal) || !File.Exists(rutaRugoso))
                {
                    return null;
                }
                Texture2D metal = Leer(rutaMetal, true);
                Texture2D rugoso = Leer(rutaRugoso, true);
                Color[] m = metal.GetPixels();
                Color[] r = rugoso.width == metal.width && rugoso.height == metal.height ? rugoso.GetPixels() : null;
                Color[] salida = new Color[m.Length];
                for (int i = 0; i < m.Length; i++)
                {
                    salida[i] = new Color(m[i].r, 0f, 0f, 1f - (r != null ? r[i].r : 0.5f));
                }
                Texture2D mascara = new Texture2D(metal.width, metal.height, TextureFormat.RGBA32, false, true);
                mascara.SetPixels(salida);
                mascara.Apply();
                File.WriteAllBytes(rutaSalida, mascara.EncodeToPNG());
                Object.DestroyImmediate(metal);
                Object.DestroyImmediate(rugoso);
                Object.DestroyImmediate(mascara);
                AssetDatabase.ImportAsset(rutaSalida, ImportAssetOptions.ForceUpdate);
            }
            TextureImporter ti = AssetImporter.GetAtPath(rutaSalida) as TextureImporter;
            if (ti != null && (ti.sRGBTexture || ti.alphaSource != TextureImporterAlphaSource.FromInput))
            {
                ti.sRGBTexture = false;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(rutaSalida);
        }

        private static Texture2D Leer(string ruta, bool lineal)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false, lineal);
            t.LoadImage(File.ReadAllBytes(ruta));
            return t;
        }

        /// <summary>Fotos de cada FBX crudo desde +Z, -Z, +X y arriba (corrida aparte), para saber hacia dónde mira cada modelo.</summary>
        public static void FotosModelos()
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Directory.CreateDirectory(CarpetaFotos);
            float x = 0f;
            foreach (string m in Modelos)
            {
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx(m));
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial(m));
                if (fbx == null)
                {
                    continue;
                }
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterials = Enumerable.Repeat(mat, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                }
                inst.transform.position = new Vector3(x, 0f, 0f);
                Bounds b = Caja(inst.transform);
                float d = b.size.magnitude * 1.3f;
                Foto("modelo_" + m + "_desdeMasZ", b.center + Vector3.forward * d, b.center, 40f);
                Foto("modelo_" + m + "_desdeMenosZ", b.center - Vector3.forward * d, b.center, 40f);
                Foto("modelo_" + m + "_desdeMasX", b.center + Vector3.right * d, b.center, 40f);
                inst.SetActive(false);
                x += 100f;
            }
        }

        /// <summary>Vista de arriba del recinto de Nova y lista de lo que hay al sur de la calle (para decidir dónde va la torre).</summary>
        public static void Arriba()
        {
            ReemplazarEdificiosT1502.Abrir();
            Directory.CreateDirectory(CarpetaFotos);
            GameObject go = new GameObject("CamArriba");
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 50f;
            cam.farClipPlane = 1000f;
            go.transform.position = new Vector3(100f, 400f, -50f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            RenderTexture rt = new RenderTexture(1280, 1280, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 1280, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 1280), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CarpetaFotos, "arriba_nova_x50a150_z-100a0.png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(go);
            StringBuilder sb = new StringBuilder("== ARRIBA: objetos (nivel 3) con centro en x 95-136, z -90..-50\n");
            HashSet<Transform> vistos = new HashSet<Transform>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Vector3 c = r.bounds.center;
                if (!r.enabled || c.x < 95f || c.x > 136f || c.z < -90f || c.z > -50f || r.bounds.size.magnitude > 600f)
                {
                    continue;
                }
                Transform u = Nivel(r.transform, 3);
                if (vistos.Add(u))
                {
                    sb.AppendLine("  " + Ruta(u) + "  caja " + Caja(u) + " activos " + u.gameObject.activeInHierarchy);
                }
            }
            File.AppendAllText(RutaLog, sb.ToString());
        }

        // ------------------------------------------------------------------ aplicar

        public const string NombreRaiz = "T1684_TorreLetreros";
        public const string NombreTorre = "TN01_TorreNova";
        public const string NombreLetreros = "Letreros";
        public const string NombreLucesExistentes = "LucesLetrerosExistentes";

        /// <summary>Altura total de la torre (m): más de 3 veces el edificio más alto (~145 m, las torres T541).</summary>
        public const float AlturaTorre = 470f;
        /// <summary>Escala horizontal del modelo: el recinto entre el edificio de Nova (x 119) y el muro (x 134) deja ~15 m de ancho.</summary>
        public const float EscalaHorizontalTorre = 55f;
        /// <summary>Centro de la torre: el lote del Edificio_0 de Frente_Nova y lo que tenía detrás; la escalinata da a la calle, junto a Historia_Torre (117.5, -43).</summary>
        public static readonly Vector3 PosTorre = new Vector3(126.6f, 0.22f, -63f);
        public const float GiroTorre = 180f;
        public const int MaxLucesNuevas = 60;
        public const float MinSeparacionLetreros = 9f;

        public struct Region
        {
            public string Nombre;
            public Rect Area;
            public string[] Modelos;
            public int Cuota;

            public Region(string nombre, Rect area, int cuota, params string[] modelos)
            {
                Nombre = nombre;
                Area = area;
                Cuota = cuota;
                Modelos = modelos;
            }
        }

        public static readonly Region[] Regiones =
        {
            new Region("NeonAlto", Rect.MinMaxRect(15f, 15f, 80f, 80f), 8, "LT05_PantallaAnuncio", "LT06_LetreroVertical"),
            new Region("Mercado", Rect.MinMaxRect(-50f, -30f, 50f, 14f), 7, "LT06_LetreroVertical", "LT05_PantallaAnuncio"),
            new Region("Centro", Rect.MinMaxRect(-50f, 14f, 15f, 60f), 4, "LT05_PantallaAnuncio", "LT06_LetreroVertical"),
            new Region("Nova", Rect.MinMaxRect(55f, -68f, 118f, -18f), 6, "LT09_AnuncioNova", "LT09_AnuncioNova", "LT05_PantallaAnuncio"),
        };

        /// <summary>Medidas finales de cada letrero (m): el lado mayor.</summary>
        public static float Tamano(string modelo)
        {
            switch (Codigo(modelo))
            {
                case "LT05": return 4f;
                case "LT06": return 4f;
                case "LT07": return 2.8f;
                default: return 4f;
            }
        }

        [MenuItem("Mecha Cyberpunk/T-1684 Torre y letreros: aplicar")]
        public static void Aplicar()
        {
            Importar();
            Dictionary<string, Color> colores = new Dictionary<string, Color>();
            foreach (string m in Modelos)
            {
                HacerPrefab(m);
                colores[m] = ColorPromedio(Carpeta(m) + Codigo(m) + "_emision.png");
            }
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Physics.SyncTransforms();
            StringBuilder sb = new StringBuilder("== T-1684 APLICAR\n");
            GameObject viejo = GameObject.Find(NombreRaiz);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo);
            }
            int lucesAntes = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            GameObject raiz = new GameObject(NombreRaiz);
            SceneManager.MoveGameObjectToScene(raiz, escena);
            Physics.SyncTransforms();

            ColocarTorre(raiz.transform, sb);
            Physics.SyncTransforms();
            int lucesNuevas = 3;
            List<Transform> letreros = ColocarLetreros(raiz.transform, colores, sb);
            lucesNuevas += letreros.Count;
            sb.AppendLine(ArreglarMaterialesExistentes());
            lucesNuevas += LucesExistentes(raiz.transform, MaxLucesNuevas - lucesNuevas, sb);
            int lucesDespues = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            sb.AppendLine("Luces: antes " + lucesAntes + ", despues " + lucesDespues + " (nuevas " + (lucesDespues - lucesAntes) + ")");
            sb.AppendLine("Edificio mas alto: " + EdificioMasAlto().ToString("F1") + " m; torre " + AlturaDe(raiz.transform.Find(NombreTorre)).ToString("F1") + " m");
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            File.AppendAllText(RutaLog, sb.ToString());
            Debug.Log(Etiqueta + sb);
        }

        private static void HacerPrefab(string m)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx(m));
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial(m));
            GameObject raiz = new GameObject(m);
            // Modelo es un contenedor (el FBX trae la raíz girada 90° en X): la escala se aplica en ejes del mundo.
            GameObject modelo = new GameObject("Modelo");
            modelo.transform.SetParent(raiz.transform, false);
            GameObject malla = (GameObject)PrefabUtility.InstantiatePrefab(fbx, modelo.transform);
            malla.name = Codigo(m) + "_Malla";
            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterials = Enumerable.Repeat(mat, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                r.shadowCastingMode = Codigo(m) == "TN01" ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Bounds b = Caja(raiz.transform);
            if (Codigo(m) == "TN01")
            {
                // Pie en y = 0, centrado; la escalinata (-Z del modelo) es el frente.
                modelo.transform.localScale = new Vector3(EscalaHorizontalTorre, AlturaTorre / b.size.y, EscalaHorizontalTorre);
                Bounds e = Caja(raiz.transform);
                modelo.transform.localPosition = new Vector3(-e.center.x, -e.min.y, -e.center.z);
            }
            else
            {
                // Letrero: lado mayor = Tamano, centrado, la espalda en z = 0 y la cara hacia -Z (el frente del modelo de Tripo).
                float s = Tamano(m) / Mathf.Max(b.size.x, b.size.y);
                modelo.transform.localScale = Vector3.one * s;
                Bounds e = Caja(raiz.transform);
                modelo.transform.localPosition = new Vector3(-e.center.x, -e.center.y, -e.max.z);
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(m));
            Object.DestroyImmediate(raiz);
        }

        private static Color ColorPromedio(string ruta)
        {
            if (!File.Exists(ruta))
            {
                return new Color(0.3f, 0.9f, 1f);
            }
            Texture2D t = Leer(ruta, false);
            Color[] px = t.GetPixels();
            Color suma = Color.black;
            float peso = 0f;
            for (int i = 0; i < px.Length; i += 7)
            {
                float w = px[i].maxColorComponent;
                suma += px[i] * w;
                peso += w;
            }
            Object.DestroyImmediate(t);
            Color c = peso > 0f ? suma / peso : Color.white;
            float max = Mathf.Max(0.01f, c.maxColorComponent);
            return new Color(c.r / max, c.g / max, c.b / max, 1f);
        }

        // ---- torre

        private static void ColocarTorre(Transform raiz, StringBuilder sb)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(NombreTorre));
            GameObject torre = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz);
            torre.name = NombreTorre;
            torre.transform.position = PosTorre;
            torre.transform.rotation = Quaternion.Euler(0f, GiroTorre, 0f);
            Bounds b = Caja(torre.transform);
            sb.AppendLine("Torre en " + PosTorre + " caja " + b);

            // Lo que ocupaba el lote: se apaga (no se borra) lo que se cruza con el pie de la torre.
            Rect pie = Rect.MinMaxRect(b.min.x + 0.6f, b.min.z + 0.6f, b.max.x - 0.6f, b.max.z - 0.6f);
            foreach (Transform padre in Todos().Where(t => Regex.IsMatch(Ruta(t), "^(S2c_Nova/Frente_Nova|Barrio/Manzana_6/Edificios)$")).ToList())
            {
                foreach (Transform h in padre)
                {
                    if (!h.gameObject.activeSelf || Regex.IsMatch(h.name, "Banqueta|Bolardo|Poste|Bote|Camara", RegexOptions.IgnoreCase))
                    {
                        continue;
                    }
                    Bounds hb = Caja(h);
                    sb.AppendLine("    hijo " + Ruta(h) + " caja " + hb);
                    if (hb.size == Vector3.zero)
                    {
                        continue;
                    }
                    Rect r = Rect.MinMaxRect(hb.min.x, hb.min.z, hb.max.x, hb.max.z);
                    if (r.Overlaps(pie) && hb.size.y > 3f)
                    {
                        h.gameObject.SetActive(false);
                        sb.AppendLine("  apagado (ocupaba el pie de la torre): " + Ruta(h) + " caja " + hb);
                    }
                }
            }

            // Collider simple: el basamento y el fuste bajo.
            GameObject col = new GameObject("ColliderBase");
            col.transform.SetParent(torre.transform, false);
            BoxCollider caja = col.AddComponent<BoxCollider>();
            Vector3 local = torre.transform.InverseTransformPoint(b.center);
            caja.center = new Vector3(local.x, 20f, local.z);
            caja.size = new Vector3(b.size.x * 0.92f, 40f, b.size.z * 0.92f);
            GameObject colAlto = new GameObject("ColliderFuste");
            colAlto.transform.SetParent(torre.transform, false);
            BoxCollider fuste = colAlto.AddComponent<BoxCollider>();
            fuste.center = new Vector3(local.x, AlturaTorre * 0.5f, local.z);
            fuste.size = new Vector3(b.size.x * 0.45f, AlturaTorre, b.size.x * 0.45f);

            // Luces de la base: cian, sin sombras, moderadas.
            Color cian = new Color(0.35f, 0.95f, 1f);
            Vector3 frente = -torre.transform.forward; // la escalinata mira a -Z del modelo
            Vector3 entrada = new Vector3(b.center.x, PosTorre.y, b.center.z) + frente * (b.extents.z * 0.85f);
            for (int i = 0; i < 2; i++)
            {
                Light l = NuevaLuz(torre.transform, "LuzBase_" + i, LightType.Point, cian, 3.5f, 22f);
                l.transform.position = entrada + Vector3.up * 4f + torre.transform.right * (i == 0 ? -5f : 5f);
            }
            Light foco = NuevaLuz(torre.transform, "FocoFuste", LightType.Spot, cian, 6f, 160f);
            foco.spotAngle = 22f;
            foco.transform.position = entrada + frente * 6f + Vector3.up * 1f;
            foco.transform.rotation = Quaternion.LookRotation((new Vector3(b.center.x, 140f, b.center.z) - foco.transform.position).normalized);

            // Haz de luz arriba: cono aditivo, transparente hacia arriba.
            GameObject haz = new GameObject("HazDeLuz");
            haz.transform.SetParent(torre.transform, false);
            haz.transform.position = new Vector3(b.center.x, b.max.y - 6f, b.center.z);
            haz.AddComponent<MeshFilter>().sharedMesh = MallaHaz();
            MeshRenderer mr = haz.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialHaz();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        public const string RutaMallaHaz = CarpetaArte + "TN01_TorreNova/TN01_Haz.asset";
        public const string RutaMaterialHaz = CarpetaArte + "TN01_TorreNova/TN01_Haz.mat";
        public const string RutaTexturaHaz = CarpetaArte + "TN01_TorreNova/TN01_Haz.png";
        public const float LargoHaz = 260f;
        public const float RadioHaz = 22f;

        private static Mesh MallaHaz()
        {
            Mesh malla = AssetDatabase.LoadAssetAtPath<Mesh>(RutaMallaHaz);
            bool nueva = malla == null;
            if (nueva)
            {
                malla = new Mesh { name = "TN01_Haz" };
            }
            const int Lados = 24;
            List<Vector3> v = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tri = new List<int>();
            for (int i = 0; i <= Lados; i++)
            {
                float a = i * Mathf.PI * 2f / Lados;
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(d * 0.8f);
                uv.Add(new Vector2((float)i / Lados, 0f));
                v.Add(d * RadioHaz + Vector3.up * LargoHaz);
                uv.Add(new Vector2((float)i / Lados, 1f));
            }
            for (int i = 0; i < Lados; i++)
            {
                int a = i * 2;
                tri.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 });
            }
            malla.Clear();
            malla.SetVertices(v);
            malla.SetUVs(0, uv);
            malla.SetTriangles(tri, 0);
            malla.RecalculateNormals();
            malla.RecalculateBounds();
            if (nueva)
            {
                AssetDatabase.CreateAsset(malla, RutaMallaHaz);
            }
            EditorUtility.SetDirty(malla);
            return malla;
        }

        private static Material MaterialHaz()
        {
            if (!File.Exists(RutaTexturaHaz))
            {
                const int Ancho = 4;
                const int Alto = 128;
                Texture2D t = new Texture2D(Ancho, Alto, TextureFormat.RGBA32, false);
                for (int y = 0; y < Alto; y++)
                {
                    float k = 1f - (float)y / (Alto - 1);
                    float a = k * k * 0.9f;
                    for (int x = 0; x < Ancho; x++)
                    {
                        t.SetPixel(x, y, new Color(a, a, a, a));
                    }
                }
                t.Apply();
                File.WriteAllBytes(RutaTexturaHaz, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(RutaTexturaHaz, ImportAssetOptions.ForceUpdate);
                TextureImporter ti = AssetImporter.GetAtPath(RutaTexturaHaz) as TextureImporter;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialHaz);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, RutaMaterialHaz);
            }
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaHaz);
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(0.45f, 0.95f, 1f, 1f) * 1.6f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
            return mat;
        }

        private static Light NuevaLuz(Transform padre, string nombre, LightType tipo, Color color, float intensidad, float rango)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            Light l = go.AddComponent<Light>();
            l.type = tipo;
            l.color = color;
            l.intensity = intensidad;
            l.range = rango;
            l.shadows = LightShadows.None;
            l.lightmapBakeType = LightmapBakeType.Realtime;
            return l;
        }

        // ---- letreros

        public class Sitio
        {
            public Vector3 Punto;   // en la pared, a la altura del centro del letrero
            public Vector3 Normal;  // hacia la calle
            public float Piso;      // altura de la banqueta enfrente
            public float Base;      // altura de la orilla de abajo del letrero sobre la banqueta
            public string Region;

            public Sitio Para(string modelo) => new Sitio { Punto = new Vector3(Punto.x, Piso + Base + MedidasDe(modelo).y * 0.5f, Punto.z), Normal = Normal, Piso = Piso, Base = Base, Region = Region };
        }

        private static List<Transform> ColocarLetreros(Transform raiz, Dictionary<string, Color> colores, StringBuilder sb)
        {
            Transform padre = new GameObject(NombreLetreros).transform;
            padre.SetParent(raiz, false);
            List<Transform> puestos = new List<Transform>();
            List<Vector3> usados = new List<Vector3>();
            Rect linea = CalculoMuroT1543.Expandir(FueraDelMuroT1543.LineaDelMuro(), -3f);
            Renderer[] chicos = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && !(r is ParticleSystemRenderer) && r.bounds.size.magnitude < 9f).ToArray();
            Bounds[] puertas = Todos().Where(t => t.gameObject.activeInHierarchy && Regex.IsMatch(t.name, "puerta|door|porton|cortina|PC01|entrada", RegexOptions.IgnoreCase))
                .Select(t => Caja(t)).Where(b => b.size != Vector3.zero && b.size.magnitude < 15f).ToArray();
            Bounds torre = Caja(raiz.Find(NombreTorre));

            // Taquerías: un LT07 en la pared más cercana a cada puesto de tacos (distintos).
            Vector3[] tacos = Todos().Where(t => t.gameObject.activeInHierarchy && Regex.IsMatch(t.name, "^(Puesto_Tacos|C12_PuestoTacos)") && t.parent != null
                    && !Regex.IsMatch(t.parent.name, "^Visual"))
                .Select(t => t.position).ToArray();
            int nTacos = 0;
            foreach (Vector3 p in tacos)
            {
                if (nTacos >= 4)
                {
                    break;
                }
                Rect area = new Rect(p.x - 9f, p.z - 9f, 18f, 18f);
                Sitio s = Buscar(area, "LT07_LetreroTaqueria", usados, linea, chicos, puertas, torre).Where(c => !usados.Any(u => (u - c.Punto).magnitude < MinSeparacionLetreros)).OrderBy(c => Vector2.Distance(new Vector2(c.Punto.x, c.Punto.z), new Vector2(p.x, p.z))).FirstOrDefault();
                if (s != null)
                {
                    s.Region = "Tacos";
                    puestos.Add(Poner(padre, "LT07_LetreroTaqueria", s, colores, usados, sb));
                    nTacos++;
                }
            }
            foreach (Region reg in Regiones)
            {
                int n = 0;
                List<Sitio> lista = Buscar(reg.Area, reg.Modelos[0], usados, linea, chicos, puertas, torre);
                // De centro a orillas: los más cercanos al centro de la región primero, para que se vean en las calles principales.
                Vector2 centro = reg.Area.center;
                foreach (Sitio s in lista.OrderBy(c => Vector2.Distance(new Vector2(c.Punto.x, c.Punto.z), centro)))
                {
                    if (n >= reg.Cuota)
                    {
                        break;
                    }
                    string modelo = reg.Modelos[n % reg.Modelos.Length];
                    Sitio sm = s.Para(modelo);
                    if (usados.Any(u => (u - sm.Punto).magnitude < MinSeparacionLetreros) || !Plana(sm, modelo) || !Cabe(sm, modelo, chicos, puertas))
                    {
                        continue;
                    }
                    sm.Region = reg.Nombre;
                    puestos.Add(Poner(padre, modelo, sm, colores, usados, sb));
                    n++;
                }
                sb.AppendLine("Region " + reg.Nombre + ": " + n + " de " + reg.Cuota + " (" + lista.Count + " sitios; rechazos " + string.Join(", ", Rechazos.Select(k => k.Key + " " + k.Value)) + ")");
                Rechazos.Clear();
            }
            sb.AppendLine("Letreros nuevos: " + puestos.Count + " (tacos " + nTacos + ")");
            return puestos;
        }

        private static Transform Poner(Transform padre, string modelo, Sitio s, Dictionary<string, Color> colores, List<Vector3> usados, StringBuilder sb)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(modelo));
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = modelo + "_" + padre.childCount;
            go.transform.rotation = Quaternion.LookRotation(-s.Normal, Vector3.up);
            go.transform.position = s.Punto + s.Normal * 0.04f;
            usados.Add(s.Punto);
            Bounds b = Caja(go.transform);
            Light l = NuevaLuz(go.transform, "LuzLetrero", LightType.Point, colores[modelo], IntensidadLuzLetrero, 6f);
            l.transform.position = b.center + s.Normal * 1.2f;
            sb.AppendLine("  " + go.name + " (" + s.Region + ") en " + s.Punto.ToString("F1") + " normal " + s.Normal.ToString("F2") + " caja " + b.size.ToString("F2"));
            return go.transform;
        }

        /// <summary>Medidas del letrero ya escalado: ancho (x), alto (y), fondo (z).</summary>
        private static Vector3 Medidas(string modelo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(modelo));
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Vector3 s = Caja(go.transform).size;
            Object.DestroyImmediate(go);
            return s;
        }

        private static readonly Dictionary<string, Vector3> cacheMedidas = new Dictionary<string, Vector3>();

        private static Vector3 MedidasDe(string modelo)
        {
            if (!cacheMedidas.TryGetValue(modelo, out Vector3 m))
            {
                m = Medidas(modelo);
                cacheMedidas[modelo] = m;
            }
            return m;
        }

        /// <summary>Altura del centro del letrero sobre la banqueta (m): la orilla de abajo queda a ~4 m (no tapa puertas) y la de arriba bajo 8.</summary>
        public static float AlturaCentro(string modelo) => 4f + MedidasDe(modelo).y * 0.5f;

        /// <summary>Alturas que se prueban para la orilla de abajo (m): arriba de las puertas (3.2) y la de arriba bajo ~8 m.</summary>
        public static readonly float[] Bases = { 4f, 3.4f, 5f };

        private static readonly Vector3[] Direcciones = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

        private static List<Sitio> Buscar(Rect area, string modelo, List<Vector3> usados, Rect linea, Renderer[] chicos, Bounds[] puertas, Bounds torre)
        {
            List<Sitio> lista = new List<Sitio>();
            const float Paso = 2f;
            for (float x = area.xMin; x <= area.xMax; x += Paso)
            {
                for (float z = area.yMin; z <= area.yMax; z += Paso)
                {
                    if (!linea.Contains(new Vector2(x, z)))
                    {
                        continue;
                    }
                    if (!Physics.Raycast(new Vector3(x, 80f, z), Vector3.down, out RaycastHit piso, 100f, ~0, QueryTriggerInteraction.Ignore) || piso.point.y > 0.8f)
                    {
                        continue;
                    }
                    foreach (float bajo in Bases)
                    foreach (Vector3 d in Direcciones)
                    {
                        float y = piso.point.y + bajo + MedidasDe(modelo).y * 0.5f;
                        Vector3 o = new Vector3(x, y, z);
                        if (!Physics.Raycast(o, d, out RaycastHit hit, AlcancePared, ~0, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }
                        if (!EsPared(hit, d))
                        {
                            Contar("no es pared");
                            continue;
                        }
                        Sitio s = new Sitio { Punto = hit.point, Normal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized, Piso = piso.point.y, Base = bajo };
                        if (torre.size != Vector3.zero && torre.Contains(new Vector3(s.Punto.x, torre.center.y, s.Punto.z)))
                        {
                            continue;
                        }
                        // Calle de verdad enfrente: 6 m libres.
                        if (Physics.Raycast(s.Punto + s.Normal * 0.3f, s.Normal, CalleLibre, ~0, QueryTriggerInteraction.Ignore))
                        {
                            Contar("sin calle enfrente");
                            continue;
                        }
                        if (lista.Any(c => (new Vector2(c.Punto.x - s.Punto.x, c.Punto.z - s.Punto.z)).magnitude < 3f))
                        {
                            continue;
                        }
                        if (!Plana(s, modelo))
                        {
                            Contar("pared no plana");
                        }
                        else if (!Cabe(s, modelo, chicos, puertas))
                        {
                            Contar("no cabe");
                        }
                        else
                        {
                            lista.Add(s);
                        }
                    }
                }
            }
            return lista;
        }

        public const float AlcancePared = 10f;
        public const float CalleLibre = 4f;
        public static readonly Dictionary<string, int> Rechazos = new Dictionary<string, int>();

        private static void Contar(string motivo)
        {
            Rechazos[motivo] = Rechazos.TryGetValue(motivo, out int k) ? k + 1 : 1;
        }

        private static bool EsPared(RaycastHit hit, Vector3 d)
        {
            if (hit.collider.attachedRigidbody != null || Mathf.Abs(hit.normal.y) > 0.1f || Vector3.Dot(hit.normal, -d) < 0.85f)
            {
                return false;
            }
            string ruta = Ruta(hit.collider.transform);
            return !Regex.IsMatch(ruta, "^(T1513_Muro|T1524_Muro|T1601_Muro|LimiteInvisible|" + NombreRaiz + ")|Npc|Carro|Moto|Puesto|Cobertura|Poste|Puerta|Cortina", RegexOptions.IgnoreCase);
        }

        /// <summary>La pared es plana detrás de todo el letrero: rayos en las cuatro esquinas pegan en el mismo plano.</summary>
        private static bool Plana(Sitio s, string modelo)
        {
            Vector3 m = MedidasDe(modelo);
            Vector3 lado = Vector3.Cross(Vector3.up, s.Normal).normalized;
            foreach (float a in new[] { -0.5f, 0.5f })
            {
                foreach (float b in new[] { -0.5f, 0.5f })
                {
                    Vector3 o = s.Punto + s.Normal * 1f + lado * (a * m.x) + Vector3.up * (b * m.y);
                    if (!Physics.Raycast(o, -s.Normal, out RaycastHit h, 1.6f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(h.distance - 1f) > 0.2f
                        || Vector3.Dot(h.normal, s.Normal) < 0.95f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>No se cruza con letreros, ventanas o aparatos que ya están en la fachada, ni queda encima de una puerta.</summary>
        private static bool Cabe(Sitio s, string modelo, Renderer[] chicos, Bounds[] puertas)
        {
            Vector3 m = MedidasDe(modelo);
            Quaternion rot = Quaternion.LookRotation(-s.Normal, Vector3.up);
            Vector3 centro = s.Punto + s.Normal * (m.z * 0.5f + 0.06f);
            if (Physics.CheckBox(centro, new Vector3(m.x * 0.5f, m.y * 0.5f, m.z * 0.5f), rot, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            Bounds b = new Bounds(centro, Vector3.zero);
            Vector3 lado = Vector3.Cross(Vector3.up, s.Normal).normalized;
            foreach (float a in new[] { -0.5f, 0.5f })
            {
                foreach (float c in new[] { -0.5f, 0.5f })
                {
                    b.Encapsulate(s.Punto + lado * (a * m.x) + Vector3.up * (c * m.y));
                    b.Encapsulate(s.Punto + s.Normal * m.z + lado * (a * m.x) + Vector3.up * (c * m.y));
                }
            }
            Bounds holgura = new Bounds(b.center, b.size + new Vector3(0.3f, 0.3f, 0.3f));
            if (chicos.Any(r => r != null && r.bounds.Intersects(holgura)))
            {
                return false;
            }
            float fondo = b.min.y;
            foreach (Bounds p in puertas)
            {
                bool encima = p.max.x > b.min.x - 1f && p.min.x < b.max.x + 1f && p.max.z > b.min.z - 1f && p.min.z < b.max.z + 1f;
                if (encima && p.max.y > fondo - 0.5f && p.min.y < b.max.y)
                {
                    return false;
                }
            }
            return true;
        }

        // ---- letreros existentes

        private static readonly Regex MaterialLetrero = new Regex("^(NE01_NeonBandera|NE01B_NeonBandera|NE02_NeonPared|L1[0-5]|C15a_LetreroHorizontal|C15b_LetreroBandera|T407_Neon.*|T414_NeonBar|T448_LetreroGuarida|Entrada_Neon_.*|Callejon_Neon.*|Clandestino_Neon.*|Cerrado_NeonCasino|Bar_Neon.*|Neon_(Naranja|Magenta|Cian|Turquesa))$");

        /// <summary>Enciende la emisión de los materiales de letreros que la tenían apagada (color de emisión puesto pero sin _EMISSION), y a los letreros sin
        /// emisión (C15a, C15b, L15) les hace un mapa de emisión con sus partes de color. RealtimeEmissive siempre.</summary>
        private static string ArreglarMaterialesExistentes()
        {
            StringBuilder sb = new StringBuilder("Materiales existentes:\n");
            HashSet<Material> mats = new HashSet<Material>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && MaterialLetrero.IsMatch(m.name) && AssetDatabase.GetAssetPath(m).StartsWith("Assets/"))
                    {
                        mats.Add(m);
                    }
                }
            }
            foreach (Material m in mats.OrderBy(m => m.name))
            {
                if (!m.HasProperty("_EmissionColor"))
                {
                    continue;
                }
                Color e = m.GetColor("_EmissionColor");
                string antes = "_EMISSION " + m.IsKeywordEnabled("_EMISSION") + " gi " + m.globalIlluminationFlags + " emi " + e;
                if (e.maxColorComponent < 0.05f)
                {
                    Texture tb = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                    string rb = tb != null ? AssetDatabase.GetAssetPath(tb) : null;
                    if (string.IsNullOrEmpty(rb))
                    {
                        sb.AppendLine("  " + m.name + ": sin emision y sin textura, se deja");
                        continue;
                    }
                    string salida = Path.GetDirectoryName(AssetDatabase.GetAssetPath(m)).Replace('\\', '/') + "/" + m.name + "_emision.png";
                    Texture2D em = HacerEmision(rb, salida, false);
                    if (em == null)
                    {
                        continue;
                    }
                    m.SetTexture("_EmissionMap", em);
                    m.SetColor("_EmissionColor", Color.white * IntensidadLetrero);
                }
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                EditorUtility.SetDirty(m);
                sb.AppendLine("  " + m.name + ": " + antes + " -> _EMISSION True gi RealtimeEmissive emi " + m.GetColor("_EmissionColor"));
            }
            AssetDatabase.SaveAssets();
            return sb.ToString();
        }

        /// <summary>Una luz chica frente a letreros existentes de los barrios con neón que no tienen luz cerca, hasta el tope.</summary>
        private static int LucesExistentes(Transform raiz, int tope, StringBuilder sb)
        {
            Transform padre = new GameObject(NombreLucesExistentes).transform;
            padre.SetParent(raiz, false);
            Light[] luces = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<Vector3> hechos = new List<Vector3>();
            int n = 0;
            IEnumerable<Renderer> letreros = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && r is MeshRenderer && !r.transform.IsChildOf(raiz) && r.sharedMaterials.Any(m => m != null && MaterialLetrero.IsMatch(m.name))
                    && Regiones.Any(g => g.Area.Contains(new Vector2(r.bounds.center.x, r.bounds.center.z))) && r.bounds.center.y > 2f
                    && !Regex.IsMatch(Ruta(r.transform), "^(T546_Interiores|S2_Interiores|T930_Casino)|/Interior"))
                .OrderBy(r => r.bounds.center.x).ThenBy(r => r.bounds.center.z);
            foreach (Renderer r in letreros)
            {
                if (n >= tope)
                {
                    break;
                }
                Vector3 c = r.bounds.center;
                if (hechos.Any(h => (h - c).magnitude < 8f) || luces.Any(l => l.type != LightType.Directional && (l.transform.position - c).magnitude < 4f))
                {
                    continue;
                }
                // Hacia dónde mira: lejos de la pared más cercana.
                RaycastHit mejor = default;
                bool hay = false;
                foreach (Vector3 d in Direcciones)
                {
                    if (Physics.Raycast(c, d, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(h.normal.y) < 0.2f && (!hay || h.distance < mejor.distance))
                    {
                        mejor = h;
                        hay = true;
                    }
                }
                if (!hay)
                {
                    continue;
                }
                Material m = r.sharedMaterials.First(x => x != null && MaterialLetrero.IsMatch(x.name));
                Color col = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.white;
                float max = col.maxColorComponent;
                col = max > 0.05f ? new Color(col.r / max, col.g / max, col.b / max) : Color.white;
                if (col.r > 0.9f && col.g > 0.9f && col.b > 0.9f)
                {
                    col = n % 2 == 0 ? new Color(1f, 0.35f, 0.85f) : new Color(0.3f, 0.9f, 1f);
                }
                Vector3 normal = new Vector3(mejor.normal.x, 0f, mejor.normal.z).normalized;
                Light l = NuevaLuz(padre, "Luz_" + r.name, LightType.Point, col, IntensidadLuzLetrero, 5f);
                l.transform.position = new Vector3(c.x, c.y, c.z) + normal * 1.2f;
                hechos.Add(c);
                n++;
                sb.AppendLine("  luz frente a " + Ruta(r.transform) + " (" + m.name + ")");
            }
            sb.AppendLine("Luces frente a letreros existentes: " + n);
            return n;
        }

        // ---- medidas (las usa la prueba)

        /// <summary>Lo más alto de la ciudad dentro del muro, sin cielo, fondo, drones ni lo de esta tarea.</summary>
        public static float EdificioMasAlto()
        {
            Rect linea = FueraDelMuroT1543.LineaDelMuro();
            float max = 0f;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || r is ParticleSystemRenderer || r.bounds.size.magnitude > 600f || !linea.Contains(new Vector2(r.bounds.center.x, r.bounds.center.z)))
                {
                    continue;
                }
                if (Regex.IsMatch(Ruta(r.transform), "^(Realismo|T513_Fondo|AMB_|T1398|" + NombreRaiz + ")"))
                {
                    continue;
                }
                max = Mathf.Max(max, r.bounds.max.y);
            }
            return max;
        }

        public static float AlturaDe(Transform t)
        {
            if (t == null)
            {
                return 0f;
            }
            Bounds b = Caja(t.Find("Modelo") ?? t);
            return b.max.y - t.position.y;
        }

        // ------------------------------------------------------------------ fotos

        [MenuItem("Mecha Cyberpunk/T-1684 Torre y letreros: fotos")]
        public static void Fotos()
        {
            ReemplazarEdificiosT1502.Abrir();
            Directory.CreateDirectory(CarpetaFotos);
            GameObject raiz = GameObject.Find(NombreRaiz);
            if (raiz == null)
            {
                Debug.LogError(Etiqueta + "no hay " + NombreRaiz);
                return;
            }
            Transform torre = raiz.transform.Find(NombreTorre);
            Bounds b = Caja(torre);
            Vector3 mira = new Vector3(b.center.x, 170f, b.center.z);
            Foto("torre_desde_casa_suburbios", new Vector3(-55f, 1.9f, -77f), mira, 60f);
            Foto("torre_desde_centro", new Vector3(0f, 1.9f, -2f), mira, 60f);
            Foto("torre_desde_neonalto", new Vector3(47f, 1.9f, 40f), mira, 60f);
            Foto("torre_pie_recinto", new Vector3(108f, 1.9f, -40f), new Vector3(b.center.x, 30f, b.center.z), 70f);
            Foto("torre_pie_recinto_arriba", new Vector3(108f, 1.9f, -40f), new Vector3(b.center.x, 250f, b.center.z), 75f);
            Transform letreros = raiz.transform.Find(NombreLetreros);
            int n = 0;
            foreach (Transform l in letreros)
            {
                if (n > 0 && n != 5 && n != 12 && n != letreros.childCount - 1)
                {
                    n++;
                    continue;
                }
                Bounds lb = Caja(l);
                Vector3 normal = -l.forward;
                Vector3 pos = lb.center + normal * 7f + Vector3.Cross(Vector3.up, normal) * 2f;
                pos.y = 1.9f;
                Foto("letrero_" + n + "_" + l.name, pos, lb.center, 65f);
                n++;
            }
            // Comprobación de que la luz ilumina la pared: el mismo letrero con su luz y sin ella.
            foreach (int i in new[] { 0, 5, letreros.childCount - 1 })
            {
                Transform l = letreros.GetChild(i);
                Bounds lb = Caja(l);
                Vector3 normal = -l.forward;
                Vector3 pos = lb.center + normal * 5f;
                pos.y = 1.9f;
                Light luz = l.GetComponentInChildren<Light>();
                Foto("luz_" + i + "_con", pos, lb.center, 70f);
                luz.enabled = false;
                Foto("luz_" + i + "_sin", pos, lb.center, 70f);
                luz.enabled = true;
            }
            Debug.Log(Etiqueta + "fotos en " + CarpetaFotos);
        }

        // ------------------------------------------------------------------ utilidades

        public const string CarpetaFotos = "docs/capturas/_tareas/T47";

        private static void Foto(string nombre, Vector3 pos, Vector3 mira, float fov)
        {
            const int Ancho = 1280;
            const int Alto = 720;
            GameObject go = new GameObject("CamFotosT1684");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CarpetaFotos, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }

        private static IEnumerable<Transform> Todos()
        {
            return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true));
        }

        private static Transform Nivel(Transform t, int n)
        {
            List<Transform> cadena = new List<Transform>();
            for (Transform p = t; p != null; p = p.parent)
            {
                cadena.Insert(0, p);
            }
            return cadena[Mathf.Min(n - 1, cadena.Count - 1)];
        }

        public static string Ruta(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }

        public static Bounds Caja(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
            {
                return new Bounds(t.position, Vector3.zero);
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1346: importa los módulos de zonas hechos en Blender (imageassets/blender/zonas/*.fbx): autopista, escaleras, barandales, muelles,
    /// reja, muro de luz, contenedor y bita. Los FBX ya vienen en metros y con su pivote para encajar: no se reescalan ni se recentran.
    ///
    /// Materiales: el FBX trae materiales de Blender con nombres cortos (Concreto, Asfalto, Metal...) y la textura incrustada. En vez de
    /// depender de eso, se leen los nombres de material de los renderers del FBX importado (ImportViaMaterialDescription) y para cada
    /// nombre se crea un URP Lit compartido en Modulos/Materiales/&lt;nombre&gt;.mat con la textura de la tabla <see cref="Definiciones"/>.
    /// Después se remapea el importador del FBX (AddRemap) a esos .mat: el FBX y el prefab usan los mismos. Un nombre no listado queda gris liso.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarModulosT1346.ImportarYCapturar
    /// </summary>
    public static class ImportarModulosT1346
    {
        public const string CarpetaOrigen = "imageassets/blender/zonas/";
        public const string CarpetaSalida = "Assets/Arte/Zonas/Modulos/";
        public const string CarpetaTexturas = CarpetaSalida + "Texturas/";
        public const string CarpetaMateriales = CarpetaSalida + "Materiales/";
        public const string RutaInforme = "Logs/t1346-modulos.txt";
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

        private const float SuavidadBase = 0.15f;
        private const float SuavidadMetal = 0.35f;
        private const float MetalicoMetal = 0.4f;
        private const float SuavidadContenedor = 0.25f;
        private const float MetalicoContenedor = 0.3f;
        private const float CorteAlfa = 0.5f;
        private const float IntensidadEmision = 2f;
        private const float MedidaMaximaFbx = 200f;
        private const float SeparacionJugador = 0.8f;
        private const float IntensidadLuz = 1f;
        private static readonly Vector3 DireccionCamara = new Vector3(0.6f, 0.35f, -1f);
        private static readonly Color EmisionCalida = new Color(1f, 0.82f, 0.55f);

        /// <summary>Los 18 módulos entregados.</summary>
        public static readonly string[] Modulos =
        {
            "AU01_AutopistaRecta", "AU02_AutopistaCurva45", "AU03_AutopistaRampa", "AU04_ColumnaAutopista", "AU05_Guardarrail", "AU06_EntradaTunel",
            "CE01_EscaleraCerro", "CE02_BarandalTubo", "CE03_BarandalTuboEscalera", "DS01_RejaPuas", "LM01_MuroLuz",
            "ML01_BarandalMalecon", "ML02_EscaleraPlaya", "ML03_MuelleRecto", "ML04_MuelleEsquina", "ML05_Escollera",
            "PT01_Contenedor40", "PT02_BitaAmarre"
        };

        public const string NombreContenedor = "PT01_Contenedor40";
        public static readonly string[] ColoresContenedor = { "Rojo", "Azul", "Verde", "Naranja" };

        private static readonly Color[] ValoresColorContenedor =
        {
            new Color(0.62f, 0.12f, 0.10f), new Color(0.12f, 0.25f, 0.55f), new Color(0.12f, 0.40f, 0.20f), new Color(0.85f, 0.42f, 0.08f)
        };

        /// <summary>Nombre del material de Blender: textura (sin .png; vacío = color liso), color, suavidad, metálico, malla (recorte alfa), emisión.</summary>
        private sealed class Definicion
        {
            public string Textura;
            public Color Color = Color.white;
            public float Suavidad = SuavidadBase;
            public float Metalico;
            public bool Malla;
            public bool Emision;
        }

        private static readonly Dictionary<string, Definicion> Definiciones = new Dictionary<string, Definicion>
        {
            { "Asfalto", new Definicion { Textura = "T03_Asfalto" } },
            { "Concreto", new Definicion { Textura = "T_ConcretoManchado" } },
            { "ConcretoClaro", new Definicion { Textura = "T_ConcretoManchado" } },
            { "ConcretoCerro", new Definicion { Textura = "T05_ConcretoCerro" } },
            { "Metal", new Definicion { Textura = "T_MetalOxidado", Suavidad = SuavidadMetal, Metalico = MetalicoMetal } },
            { "Hierro", new Definicion { Textura = "T_HierroOxidado", Suavidad = SuavidadMetal, Metalico = MetalicoMetal } },
            { "Madera", new Definicion { Textura = "T_MaderaVieja" } },
            { "MaderaOscura", new Definicion { Textura = "T_MaderaVieja_Oscura" } },
            { "Pilote", new Definicion { Textura = "T_MaderaVieja_Oscura" } },
            { "Roca", new Definicion { Textura = "T_Roca" } },
            { "Malla", new Definicion { Textura = "T_Malla", Malla = true } },
            { "PinturaBlanca", new Definicion { Color = new Color(0.9f, 0.9f, 0.85f) } },
            { "Fondo", new Definicion { Color = new Color(0.03f, 0.03f, 0.04f) } },
            { "Luz", new Definicion { Color = EmisionCalida, Emision = true } },
            { "Contenedor", new Definicion { Color = new Color(0.45f, 0.45f, 0.47f), Suavidad = SuavidadContenedor, Metalico = MetalicoContenedor } },
        };

        /// <summary>Materiales de color liso: no llevan _BaseMap.</summary>
        public static bool EsColorLiso(string nombreMaterial)
        {
            Definicion d;
            string n = nombreMaterial.StartsWith("Contenedor_") ? "Contenedor" : nombreMaterial;
            return Definiciones.TryGetValue(n, out d) && d.Textura == null;
        }

        public static readonly StringBuilder Informe = new StringBuilder();

        public static string RutaCarpeta(string nombre)
        {
            return CarpetaSalida + nombre;
        }

        public static string RutaFbx(string nombre)
        {
            return RutaCarpeta(nombre) + "/" + nombre + ".fbx";
        }

        public static string RutaPrefab(string nombre)
        {
            return CarpetaSalida + nombre + ".prefab";
        }

        public static string RutaMaterial(string nombre)
        {
            return CarpetaMateriales + nombre + ".mat";
        }

        /// <summary>Nombres de todos los prefabs esperados: 18 módulos y 4 contenedores de color.</summary>
        public static IEnumerable<string> NombresPrefabs()
        {
            foreach (string m in Modulos)
            {
                yield return m;
            }
            foreach (string c in ColoresContenedor)
            {
                yield return NombreContenedor + "_" + c;
            }
        }

        public static void ImportarYCapturar()
        {
            Importar();
            Capturar();
        }

        // ------------------------------------------------------------------ importar

        [MenuItem("Mecha Cyberpunk/T-1346/Importar módulos de Blender")]
        public static void Importar()
        {
            Informe.Clear();
            if (!Directory.Exists(CarpetaOrigen))
            {
                Debug.LogError("[Arte] T-1346: falta la carpeta " + CarpetaOrigen);
                return;
            }
            Directory.CreateDirectory(CarpetaTexturas);
            Directory.CreateDirectory(CarpetaMateriales);
            List<string> nombres = Directory.GetFiles(CarpetaOrigen, "*.fbx").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToList();
            CopiarTexturas();
            foreach (string n in nombres)
            {
                Directory.CreateDirectory(RutaCarpeta(n));
                CopiarSiCambio(CarpetaOrigen + n + ".fbx", RutaFbx(n));
            }
            AssetDatabase.Refresh();
            ConfigurarTexturas();
            foreach (string n in nombres)
            {
                ConfigurarFbx(n);
            }
            AssetDatabase.SaveAssets();
            foreach (string n in nombres)
            {
                Informe.AppendLine(CrearPrefabs(n));
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, "nombre\tlimites (m)\tUV max\tmateriales\ttriangulos\tcollider\n" + Informe);
            Debug.Log("[Arte] T-1346 importados:\n" + Informe);
        }

        private static void CopiarSiCambio(string origen, string destino)
        {
            if (File.Exists(destino) && new FileInfo(destino).Length == new FileInfo(origen).Length)
            {
                return;
            }
            File.Copy(origen, destino, true);
        }

        private static IEnumerable<string> ArchivosTextura()
        {
            List<string> r = new List<string>();
            string malla = CarpetaOrigen + "T_Malla.png";
            if (File.Exists(malla))
            {
                r.Add(malla);
            }
            string carpeta = CarpetaOrigen + "texturas/";
            if (Directory.Exists(carpeta))
            {
                r.AddRange(Directory.GetFiles(carpeta, "*.png"));
            }
            return r;
        }

        private static void CopiarTexturas()
        {
            foreach (string origen in ArchivosTextura())
            {
                string nombre = Path.GetFileNameWithoutExtension(origen);
                if (BuscarTexturaExistente(nombre) != null)
                {
                    continue; // ya hay una igual en el proyecto: se reusa
                }
                CopiarSiCambio(origen, CarpetaTexturas + nombre + ".png");
            }
        }

        /// <summary>Ruta de una textura con ese nombre exacto fuera de Modulos/Texturas, o null.</summary>
        private static string BuscarTexturaExistente(string nombre)
        {
            foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Texture2D", new[] { "Assets" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(ruta) == nombre && !ruta.StartsWith(CarpetaTexturas))
                {
                    return ruta;
                }
            }
            return null;
        }

        private static Texture2D CargarTextura(string nombre)
        {
            string ruta = BuscarTexturaExistente(nombre) ?? CarpetaTexturas + nombre + ".png";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        private static void ConfigurarTexturas()
        {
            foreach (string origen in ArchivosTextura())
            {
                string nombre = Path.GetFileNameWithoutExtension(origen);
                string ruta = CarpetaTexturas + nombre + ".png";
                TextureImporter t = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (t == null)
                {
                    continue; // reusada de otra carpeta
                }
                bool malla = nombre == "T_Malla";
                if (t.alphaIsTransparency != malla || t.wrapMode != TextureWrapMode.Repeat)
                {
                    t.alphaIsTransparency = malla;
                    t.wrapMode = TextureWrapMode.Repeat;
                    t.SaveAndReimport();
                }
            }
        }

        private static void ConfigurarFbx(string nombre)
        {
            string ruta = RutaFbx(nombre);
            ModelImporter imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (imp == null)
            {
                return;
            }
            bool cambio = false;
            if (imp.materialImportMode == ModelImporterMaterialImportMode.None)
            {
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription; // hace falta para leer los nombres
                cambio = true;
            }
            if (imp.importCameras || imp.importLights)
            {
                imp.importCameras = false;
                imp.importLights = false;
                cambio = true;
            }
            if (cambio)
            {
                imp.SaveAndReimport();
            }

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (fbx == null)
            {
                return;
            }
            // Unidad: los módulos deben salir en metros. Si salen más de 200 m, se prueba sin la escala del archivo (cm -> 100x).
            if (MedidaMayor(fbx) > MedidaMaximaFbx && imp.useFileScale)
            {
                imp.useFileScale = false;
                imp.SaveAndReimport();
                fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                Informe.AppendLine("# " + nombre + ": useFileScale apagado (salia de mas de 200 m)");
            }

            Dictionary<string, Material> porNombre = new Dictionary<string, Material>();
            foreach (Renderer r in fbx.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && !porNombre.ContainsKey(m.name))
                    {
                        porNombre[m.name] = m;
                    }
                }
            }
            bool remapeado = false;
            foreach (KeyValuePair<string, Material> par in porNombre)
            {
                Material mio = CrearMaterial(par.Key, par.Value);
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), par.Key);
                if (imp.GetExternalObjectMap().TryGetValue(id, out Object o) && o == mio)
                {
                    continue;
                }
                imp.AddRemap(id, mio);
                remapeado = true;
            }
            if (remapeado)
            {
                imp.SaveAndReimport();
            }
        }

        private static float MedidaMayor(GameObject fbx)
        {
            Bounds b = ArteT250.Limites(fbx);
            return Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        }

        // ------------------------------------------------------------------ materiales

        private static Material CrearMaterial(string nombre, Material delFbx)
        {
            string ruta = RutaMaterial(nombre);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            Definicion d;
            if (!Definiciones.TryGetValue(nombre, out d))
            {
                // Nombre desconocido (llegó un módulo nuevo): gris liso con el color que traiga el FBX.
                Color c = delFbx != null && delFbx.HasProperty("_BaseColor") ? delFbx.GetColor("_BaseColor") : new Color(0.5f, 0.5f, 0.5f);
                d = new Definicion { Color = c };
                Informe.AppendLine("# material desconocido '" + nombre + "': gris liso, agregar a Definiciones");
            }
            AplicarDefinicion(material, d);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AplicarDefinicion(Material material, Definicion d)
        {
            Texture2D textura = d.Textura != null ? CargarTextura(d.Textura) : null;
            material.SetTexture("_BaseMap", textura);
            material.SetColor("_BaseColor", d.Color);
            material.SetFloat("_Smoothness", d.Suavidad);
            material.SetFloat("_Metallic", d.Metalico);
            material.enableInstancing = true;
            if (d.Malla)
            {
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", CorteAlfa);
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetFloat("_Cull", 0f);
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = 2450;
            }
            if (d.Emision)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", EmisionCalida * IntensidadEmision);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }

        private static Material CrearMaterialContenedor(string color, Color valor)
        {
            string ruta = RutaMaterial("Contenedor_" + color);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            AplicarDefinicion(material, new Definicion { Color = valor, Suavidad = SuavidadContenedor, Metalico = MetalicoContenedor });
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------ prefabs

        private static bool UsaCajaDeLimites(string nombre)
        {
            return nombre.StartsWith("PT01") || nombre.StartsWith("PT02") || nombre.StartsWith("AU04");
        }

        private static string CrearPrefabs(string nombre)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx(nombre));
            if (fbx == null)
            {
                return nombre + "\tFALTA FBX";
            }
            string linea = CrearPrefab(nombre, fbx, null);
            if (nombre == NombreContenedor)
            {
                for (int i = 0; i < ColoresContenedor.Length; i++)
                {
                    Material m = CrearMaterialContenedor(ColoresContenedor[i], ValoresColorContenedor[i]);
                    CrearPrefab(nombre + "_" + ColoresContenedor[i], fbx, m);
                }
                linea += "\t(+4 variantes de color)";
            }
            return linea;
        }

        private static string CrearPrefab(string nombrePrefab, GameObject fbx, Material material)
        {
            string nombreModulo = nombrePrefab.StartsWith(NombreContenedor) ? NombreContenedor : nombrePrefab;
            GameObject raiz = new GameObject(nombrePrefab);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            if (material != null)
            {
                foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                }
            }
            Bounds b = ArteT250.Limites(raiz);
            string collider;
            if (UsaCajaDeLimites(nombreModulo))
            {
                BoxCollider caja = raiz.AddComponent<BoxCollider>();
                caja.center = b.center;
                caja.size = b.size;
                collider = "Caja";
            }
            else
            {
                int n = 0;
                foreach (MeshFilter f in visual.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (f.sharedMesh == null)
                    {
                        continue;
                    }
                    MeshCollider mc = f.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = f.sharedMesh;
                    mc.convex = false;
                    n++;
                }
                collider = "Malla x" + n;
            }
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            }

            string rutaPrefab = RutaPrefab(nombrePrefab);
            PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
            string mats = string.Join(",", visual.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct());
            Vector2 uv = UvMaximo(fbx);
            Object.DestroyImmediate(raiz);
            return string.Format("{0}\t{1:F2}x{2:F2}x{3:F2} min({4:F2},{5:F2},{6:F2})\tuv {7:F1}x{8:F1}\t{9}\t{10}\t{11}",
                nombrePrefab, b.size.x, b.size.y, b.size.z, b.min.x, b.min.y, b.min.z, uv.x, uv.y, mats, Triangulos(fbx), collider);
        }

        private static Vector2 UvMaximo(GameObject fbx)
        {
            Vector2 max = Vector2.zero;
            List<Vector2> uvs = new List<Vector2>();
            foreach (MeshFilter f in fbx.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
                uvs.Clear();
                f.sharedMesh.GetUVs(0, uvs);
                foreach (Vector2 u in uvs)
                {
                    max = new Vector2(Mathf.Max(max.x, Mathf.Abs(u.x)), Mathf.Max(max.y, Mathf.Abs(u.y)));
                }
            }
            return max;
        }

        public static int Triangulos(GameObject fbx)
        {
            int total = 0;
            foreach (MeshFilter f in fbx.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
                for (int i = 0; i < f.sharedMesh.subMeshCount; i++)
                {
                    total += (int)f.sharedMesh.GetIndexCount(i) / 3;
                }
            }
            return total;
        }

        // ------------------------------------------------------------------ capturas

        /// <summary>docs/capturas/T-1346-&lt;Nombre&gt;.png: el módulo con el jugador a su lado, y T-1346-autopista-armada.png.</summary>
        [MenuItem("Mecha Cyberpunk/T-1346/Capturar módulos")]
        public static void Capturar()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ArteT250.PonerLuz();
            GameObject sol = GameObject.Find("Sol");
            if (sol != null)
            {
                sol.GetComponent<Light>().intensity = IntensidadLuz;
            }
            GameObject camaraObjeto = new GameObject("Camara");
            Camera camara = camaraObjeto.AddComponent<Camera>();
            GameObject jugadorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador);
            StringBuilder resumen = new StringBuilder();
            foreach (string nombre in NombresPrefabs())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(nombre));
                if (prefab == null)
                {
                    resumen.AppendLine(nombre + "\tsin captura: falta el prefab");
                    continue;
                }
                GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Bounds b = ArteT250.Limites(prop);
                GameObject jugador = null;
                if (jugadorPrefab != null)
                {
                    jugador = (GameObject)PrefabUtility.InstantiatePrefab(jugadorPrefab);
                    jugador.transform.position = new Vector3(b.max.x + SeparacionJugador, 0f, 0f);
                    jugador.transform.rotation = Quaternion.LookRotation(new Vector3(DireccionCamara.x, 0f, DireccionCamara.z));
                    b.Encapsulate(ArteT250.Limites(jugador));
                }
                ArteT250.Encuadrar(camara, b, DireccionCamara);
                ArteT250.Guardar(camara, Path.Combine(ArteT250.CarpetaCapturas, "T-1346-" + nombre + ".png"));
                Object.DestroyImmediate(prop);
                if (jugador != null)
                {
                    Object.DestroyImmediate(jugador);
                }
            }
            resumen.AppendLine(CapturarAutopista(camara));
            Object.DestroyImmediate(camaraObjeto);
            Directory.CreateDirectory("Logs");
            File.AppendAllText(RutaInforme, "\nCapturas:\n" + resumen);
            Debug.Log("[Arte] T-1346 capturas:\n" + resumen);
        }

        /// <summary>
        /// AU01 en el origen; AU02 pegada a su final (largo = límites del eje mayor de AU01); AU03 al final de AU02 girada 45° hacia el lado de la curva;
        /// 2 AU04 bajo AU01 (al 25 % y al 75 %), con la punta de la columna tocando la cara inferior de la autopista. Es una aproximación
        /// para ver si encajan; el punto de salida real de la curva lo decide quien coloque el mapa.
        /// </summary>
        private static string CapturarAutopista(Camera camara)
        {
            GameObject p1 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab("AU01_AutopistaRecta"));
            GameObject p2 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab("AU02_AutopistaCurva45"));
            GameObject p3 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab("AU03_AutopistaRampa"));
            GameObject p4 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab("AU04_ColumnaAutopista"));
            if (p1 == null || p2 == null || p3 == null || p4 == null)
            {
                return "autopista armada: faltan prefabs";
            }
            GameObject a1 = (GameObject)PrefabUtility.InstantiatePrefab(p1);
            Bounds b1 = ArteT250.Limites(a1);
            bool largoEnX = b1.size.x >= b1.size.z;
            Vector3 eje = largoEnX ? Vector3.right : Vector3.forward;
            float largo = largoEnX ? b1.size.x : b1.size.z;
            Vector3 lateral = largoEnX ? Vector3.forward : Vector3.right;

            GameObject a2 = (GameObject)PrefabUtility.InstantiatePrefab(p2);
            a2.transform.position = eje * largo;
            Bounds b2 = ArteT250.Limites(a2);

            GameObject a3 = (GameObject)PrefabUtility.InstantiatePrefab(p3);
            // salida de la curva aproximada: el extremo del eje y el lado más lejano; la rampa gira 45° hacia ese lado
            float lado = Vector3.Dot(b2.center - b1.center, lateral) >= 0f ? 1f : -1f;
            float girar = (largoEnX ? -1f : 1f) * lado * 45f;
            a3.transform.rotation = Quaternion.Euler(0f, girar, 0f);
            Vector3 salida = b2.center + eje * (Vector3.Dot(b2.extents, eje)) + lateral * lado * Vector3.Dot(b2.extents, lateral);
            a3.transform.position = new Vector3(salida.x, 0f, salida.z);

            GameObject c1 = (GameObject)PrefabUtility.InstantiatePrefab(p4);
            GameObject c2 = (GameObject)PrefabUtility.InstantiatePrefab(p4);
            Bounds bc = ArteT250.Limites(c1);
            float y = b1.min.y - bc.max.y; // la punta de la columna llega a la cara inferior del tramo
            foreach (var par in new[] { (c1, 0.25f), (c2, 0.75f) })
            {
                Vector3 centro = b1.center;
                Vector3 pos = new Vector3(centro.x, y, centro.z);
                pos += eje * (largo * (par.Item2 - 0.5f)) - new Vector3(bc.center.x, 0f, bc.center.z);
                par.Item1.transform.position = pos;
            }

            Bounds todo = ArteT250.Limites(a1);
            foreach (GameObject g in new[] { a2, a3, c1, c2 })
            {
                todo.Encapsulate(ArteT250.Limites(g));
            }
            ArteT250.Encuadrar(camara, todo, new Vector3(0.7f, 0.5f, -1f));
            ArteT250.Guardar(camara, Path.Combine(ArteT250.CarpetaCapturas, "T-1346-autopista-armada.png"));
            string r = string.Format("autopista armada: largo AU01 {0:F2} m en {1}, AU01 min.y {2:F2}, columna alto {3:F2}", largo, largoEnX ? "X" : "Z", b1.min.y, bc.size.y);
            foreach (GameObject g in new[] { a1, a2, a3, c1, c2 })
            {
                Object.DestroyImmediate(g);
            }
            return r;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1014: arma las armas nuevas con su modelo, igual que <see cref="ColocarS1nArmas"/> con W02-W04 (los .zip los mete <see cref="ImportarS1n"/>):
    /// - W05 subfusil, W06 revólver, W07 rifle de plasma y W08 lanzagranadas: material URP Lit (color, normal y máscara metálico/suavidad), prefab Wxx con la boca,
    ///   prefab del piso con <see cref="ArmaEnPiso"/> y el arma en la mano del jugador (JugadorAPie.prefab).
    /// - W09 granada: modelo de Blender (imageassets/blender/granada/W09_Granada.py), su prefab, el del piso, la mano y ConfigArmasEspeciales.prefabGranada.
    /// - El tambor del revólver y del lanzagranadas: la malla se parte por región en un "Cuerpo" y un "Tambor" cuyo origen está en el eje del tambor
    ///   (<see cref="TamborArma"/> lo gira alrededor del cañón).
    /// - Los cinco objetos del socket de la mano derecha (Subfusil, Revolver, Lanzagranadas, Granada, RiflePlasma) y <c>ArmasAPie.visualesNuevas</c>.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1014Armas.Colocar
    /// </summary>
    public static class ColocarT1014Armas
    {
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        public const string RutaConfigEspeciales = "Assets/_Proyecto/Personaje/Datos/Resources/ConfigArmasEspeciales.asset";
        public const string CarpetaGranada = "Assets/Arte/Armas/W09_Granada/";
        public const string CodigoGranada = "W09";

        // Luz de la boca: igual que las armas de CreadorJugadorAPie.
        private static readonly Color ColorLuzBoca = new Color(1f, 0.8f, 0.4f);
        private const float IntensidadLuzBoca = 8f;
        private const float AlcanceLuzBoca = 12f;
        // Altura (arriba) y avance (adelante) de la luz de la boca de la granada respecto a la palma (m): de ahí sale al lanzarla.
        private static readonly Vector3 BocaGranada = new Vector3(0f, 0.04f, 0.15f);
        // La granada se agarra con la palma por abajo: sube y avanza para quedar sobre el puño y verse (m, respecto a la palma).
        private static readonly Vector3 PosicionGranadaEnMano = new Vector3(0f, 0.09f, 0.04f);
        private const float MetalicoPorOmision = 1f;
        private const float SuavidadPorOmision = 1f;
        private const float EscalaPisoGranada = 1.6f;

        /// <summary>Región del modelo (espacio del .fbx: X a lo largo con el cañón a -X, Y arriba) que es el tambor: entre <c>XMin</c> y <c>XMax</c>, a menos de <c>Radio</c> del eje (y = <c>ZEje</c>).</summary>
        public readonly struct RegionTambor
        {
            public readonly float XMin;
            public readonly float XMax;
            public readonly float EjeAltura;
            public readonly float Radio;

            public RegionTambor(float xMin, float xMax, float ejeAltura, float radio)
            {
                XMin = xMin;
                XMax = xMax;
                EjeAltura = ejeAltura;
                Radio = radio;
            }
        }

        /// <summary>
        /// Medidas de un arma nueva, tomadas del render de lado de Blender (ortográfico, con rejilla cada 0.1 m) sobre el .fbx de Tripo (largo 0.98 m, cañón a -X, Y arriba).
        /// </summary>
        public readonly struct Medida
        {
            public readonly string Codigo;
            public readonly string Nombre;
            /// <summary>Nombre del objeto del arma en el socket del jugador (el que lee ArmasAPie).</summary>
            public readonly string Objeto;
            public readonly ArmaAPie Arma;
            public readonly string Articulo;
            /// <summary>Factor al tamaño del .fbx (0.98 m de largo): subfusil 0.61 (0.6 m), revólver 0.36 (0.35 m), lanzagranadas 0.82 (0.8 m), rifle de plasma 1.02 (1.0 m).</summary>
            public readonly float Escala;
            /// <summary>Palma en la empuñadura (X, Y del .fbx).</summary>
            public readonly Vector2 Empunadura;
            /// <summary>Punta del cañón (X, Y del .fbx).</summary>
            public readonly Vector2 Boca;
            public readonly bool TieneTambor;
            public readonly RegionTambor Tambor;

            public Medida(string codigo, string nombre, string objeto, ArmaAPie arma, string articulo, float escala, Vector2 empunadura, Vector2 boca, bool tieneTambor = false, RegionTambor tambor = default)
            {
                Codigo = codigo;
                Nombre = nombre;
                Objeto = objeto;
                Arma = arma;
                Articulo = articulo;
                Escala = escala;
                Empunadura = empunadura;
                Boca = boca;
                TieneTambor = tieneTambor;
                Tambor = tambor;
            }

            public string Carpeta => ImportarS1n.CarpetaArmas + Codigo + "_" + Nombre + "/";
            public string RutaFbx => Carpeta + Codigo + "_" + Nombre + ".fbx";
            public string RutaMaterial => Carpeta + Codigo + "_" + Nombre + ".mat";
            public string RutaPrefab => Carpeta + Codigo + "_" + Nombre + ".prefab";
            public string RutaPiso => Carpeta + Codigo + "_" + Nombre + "_Piso.prefab";
            public string RutaMascara => Carpeta + "Texturas/" + Codigo + "_mascara.png";
            public string RutaMallaCuerpo => Carpeta + Codigo + "_Cuerpo.asset";
            public string RutaMallaTambor => Carpeta + Codigo + "_Tambor.asset";

            /// <summary>La boca del cañón en el espacio del prefab (cañón a +Z, empuñadura en el origen).</summary>
            public Vector3 BocaEnPrefab => new Vector3(0f, Escala * (Boca.y - Empunadura.y), Escala * (Empunadura.x - Boca.x));
        }

        public static readonly Medida[] Medidas =
        {
            new Medida("W05", "Subfusil", "Subfusil", ArmaAPie.Subfusil, "el subfusil", 0.61f, new Vector2(0.31f, 0.25f), new Vector2(-0.49f, 0.417f)),
            new Medida("W06", "Revolver", "Revolver", ArmaAPie.Revolver, "el revólver", 0.36f, new Vector2(0.35f, 0.26f), new Vector2(-0.49f, 0.395f),
                true, new RegionTambor(0.04f, 0.265f, 0.388f, 0.083f)),
            new Medida("W07", "RiflePlasma", "RiflePlasma", ArmaAPie.RiflePlasma, "el rifle de plasma", 1.02f, new Vector2(0.16f, 0.157f), new Vector2(-0.49f, 0.30f)),
            new Medida("W08", "Lanzagranadas", "Lanzagranadas", ArmaAPie.Lanzagranadas, "el lanzagranadas", 0.82f, new Vector2(0.153f, 0.155f), new Vector2(-0.49f, 0.294f),
                true, new RegionTambor(-0.175f, -0.015f, 0.21f, 0.12f)),
        };

        private static readonly StringBuilder Informe = new StringBuilder();

        /// <summary>Todo T-1014: los modelos y el jugador (<see cref="Colocar"/>), los hologramas H05-H09 con el HUD y el banco de Tino (con las cinco armas nuevas).</summary>
        [MenuItem("Mecha Cyberpunk/T-1014/Todo (armas nuevas, hologramas y banco)")]
        public static void Todo()
        {
            Colocar();
            ImportarS2nHologramas.Importar();
            MechaCyberpunk.UI.Editor.CreadorMenuArmas.Crear();
        }

        [MenuItem("Mecha Cyberpunk/T-1014/Armar armas nuevas con modelo")]
        public static void Colocar()
        {
            Informe.Clear();
            Medida[] listas = Medidas.Where(m => File.Exists(m.RutaFbx)).ToArray();
            foreach (Medida m in Medidas.Except(listas))
            {
                Informe.AppendLine("falta " + m.RutaFbx + " (se arma cuando llegue)");
            }
            foreach (Medida m in listas)
            {
                Importacion(m.RutaFbx);
                Material material = Material(m);
                GameObject prefab = Prefab(m, material);
                Piso(prefab, m.RutaPiso, m.Arma, m.Articulo, 0f);
            }
            GranadaModelo();
            if (listas.Length > 0 || File.Exists(CarpetaGranada + "W09_Granada.fbx"))
            {
                Jugador(listas);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1014-armas.txt", Informe.ToString());
            Debug.Log("[Arte] T-1014 armas:\n" + Informe);
        }

        // Como W01: el .fbx sin materiales (los pone el prefab) y sin animación; legible para partir el tambor.
        private static void Importacion(string rutaFbx)
        {
            ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(rutaFbx);
            if (importador != null && (importador.materialImportMode != ModelImporterMaterialImportMode.None || importador.importAnimation || !importador.isReadable))
            {
                importador.materialImportMode = ModelImporterMaterialImportMode.None;
                importador.importAnimation = false;
                importador.isReadable = true;
                importador.SaveAndReimport();
            }
        }

        private static string Textura(Medida m, string tipo)
        {
            foreach (string extension in new[] { ".jpg", ".JPEG", ".png" })
            {
                string ruta = m.Carpeta + "Texturas/" + m.Codigo + "_" + tipo + extension;
                if (File.Exists(ruta))
                {
                    return ruta;
                }
            }
            return null;
        }

        // Material URP Lit con color, normal y la máscara metálico (R) y suavidad (A) hecha de metallic y roughness de Tripo.
        private static Material Material(Medida m)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(m.RutaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, m.RutaMaterial);
            }
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(Textura(m, "basecolor"));
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Textura(m, "normal"));
            Texture2D mascara = HacerMascara(m);
            if (normal != null)
            {
                TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal));
                if (ti.textureType != TextureImporterType.NormalMap)
                {
                    ti.textureType = TextureImporterType.NormalMap;
                    ti.SaveAndReimport();
                }
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (mascara != null)
            {
                material.SetTexture("_MetallicGlossMap", mascara);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Metallic", MetalicoPorOmision);
                material.SetFloat("_Smoothness", SuavidadPorOmision);
            }
            material.SetTexture("_BaseMap", color);
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            Informe.AppendLine("material " + m.RutaMaterial + ": color " + (color != null ? color.name : "FALTA") + ", normal " + (normal != null ? normal.name : "no")
                + ", máscara " + (mascara != null ? mascara.name : "no"));
            return material;
        }

        private static Texture2D HacerMascara(Medida m)
        {
            string rutaMetal = Textura(m, "metallic");
            string rutaRugoso = Textura(m, "roughness");
            if (rutaMetal == null || rutaRugoso == null)
            {
                return null;
            }
            if (!File.Exists(m.RutaMascara))
            {
                Texture2D metal = Leer(rutaMetal);
                Texture2D rugoso = Leer(rutaRugoso);
                Texture2D mascara = new Texture2D(metal.width, metal.height, TextureFormat.RGBA32, false, true);
                for (int y = 0; y < mascara.height; y++)
                {
                    for (int x = 0; x < mascara.width; x++)
                    {
                        float u = (x + 0.5f) / mascara.width;
                        float v = (y + 0.5f) / mascara.height;
                        float metalico = metal.GetPixel(x, y).r;
                        float suavidad = 1f - rugoso.GetPixelBilinear(u, v).r;
                        mascara.SetPixel(x, y, new Color(metalico, 0f, 0f, suavidad));
                    }
                }
                File.WriteAllBytes(m.RutaMascara, mascara.EncodeToPNG());
                Object.DestroyImmediate(metal);
                Object.DestroyImmediate(rugoso);
                Object.DestroyImmediate(mascara);
                AssetDatabase.ImportAsset(m.RutaMascara, ImportAssetOptions.ForceUpdate);
                TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(m.RutaMascara);
                ti.sRGBTexture = false;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(m.RutaMascara);
        }

        // Lee la imagen del disco (no del importador): así se puede leer sin marcarla como legible.
        private static Texture2D Leer(string ruta)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            t.LoadImage(File.ReadAllBytes(ruta));
            return t;
        }

        private static GameObject Prefab(Medida m, Material material)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(m.RutaFbx);
            GameObject raiz = new GameObject(m.Codigo + "_" + m.Nombre);
            // "Modelo" es el pivote que gira, escala y baja; adentro va el .fbx tal como se importó (su nodo ya trae el giro de eje) o, con tambor, el cuerpo y el tambor partidos.
            GameObject modelo = new GameObject("Modelo");
            modelo.transform.SetParent(raiz.transform, false);
            if (m.TieneTambor)
            {
                PartirTambor(m, fbx, modelo.transform, material);
            }
            else
            {
                GameObject malla = (GameObject)PrefabUtility.InstantiatePrefab(fbx, modelo.transform);
                PrefabUtility.UnpackPrefabInstance(malla, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                malla.name = "Malla";
                foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = material;
                }
            }
            // El .fbx tiene el cañón a -X: se gira para que mire a +Z, se escala y se baja para que la empuñadura quede en el origen.
            modelo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            modelo.transform.localScale = Vector3.one * m.Escala;
            modelo.transform.localPosition = new Vector3(0f, -m.Escala * m.Empunadura.y, m.Escala * m.Empunadura.x);
            GameObject boca = new GameObject("Boca");
            boca.transform.SetParent(raiz.transform, false);
            boca.transform.localPosition = m.BocaEnPrefab;
            Bounds b = Limites(raiz);
            GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, m.RutaPrefab);
            Object.DestroyImmediate(raiz);
            Informe.AppendLine("prefab " + m.RutaPrefab + ": largo " + b.size.z.ToString("F2") + " m, alto " + b.size.y.ToString("F2") + ", boca " + m.BocaEnPrefab.ToString("F3")
                + ", empuñadura (" + m.Empunadura.x.ToString("F3") + ", " + m.Empunadura.y.ToString("F3") + ") escala " + m.Escala.ToString("F2"));
            return guardado;
        }

        /// <summary>
        /// Parte la malla del .fbx en el cuerpo y el tambor: los triángulos con sus tres vértices dentro de la región del tambor van a "Tambor", con el origen en el eje del tambor;
        /// el resto, a "Cuerpo". Las mallas se guardan como assets y se cuelgan de <paramref name="modelo"/> (en el espacio del .fbx tal como lo ve el prefab).
        /// </summary>
        private static void PartirTambor(Medida m, GameObject fbx, Transform modelo, Material material)
        {
            GameObject temporal = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            try
            {
                temporal.transform.SetPositionAndRotation(Vector3.zero, temporal.transform.rotation);
                MeshFilter filtro = temporal.GetComponentInChildren<MeshFilter>();
                Mesh origen = filtro.sharedMesh;
                // La instancia en el mundo (el temporal está en el origen) es el espacio de "Modelo": el nodo del .fbx trae su propio giro de eje (90° en X).
                Matrix4x4 aEspacioModelo = filtro.transform.localToWorldMatrix;
                bool espejo = aEspacioModelo.determinant < 0f;
                Vector3[] vertices = origen.vertices;
                Vector3[] normales = origen.normals;
                Vector4[] tangentes = origen.tangents;
                Vector2[] uvs = origen.uv;
                int[] triangulos = origen.triangles;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = aEspacioModelo.MultiplyPoint3x4(vertices[i]);
                    if (normales.Length == vertices.Length) { normales[i] = aEspacioModelo.MultiplyVector(normales[i]).normalized; }
                    if (tangentes.Length == vertices.Length)
                    {
                        Vector3 t = aEspacioModelo.MultiplyVector(tangentes[i]).normalized;
                        tangentes[i] = new Vector4(t.x, t.y, t.z, tangentes[i].w);
                    }
                }
                RegionTambor r = m.Tambor;
                bool EnTambor(Vector3 p) => p.x >= r.XMin && p.x <= r.XMax && new Vector2(p.y - r.EjeAltura, p.z).magnitude <= r.Radio;
                // La distancia al eje es en el plano (altura, lateral); el eje pasa por la mitad del ancho del arma (z = 0).
                var tamborTris = new List<int>();
                var cuerpoTris = new List<int>();
                for (int i = 0; i < triangulos.Length; i += 3)
                {
                    bool dentro = EnTambor(vertices[triangulos[i]]) && EnTambor(vertices[triangulos[i + 1]]) && EnTambor(vertices[triangulos[i + 2]]);
                    List<int> destino = dentro ? tamborTris : cuerpoTris;
                    destino.Add(triangulos[i]);
                    destino.Add(triangulos[espejo ? i + 2 : i + 1]);
                    destino.Add(triangulos[espejo ? i + 1 : i + 2]);
                }
                Bounds limitesTambor = LimitesDe(vertices, tamborTris);
                Vector3 pivote = new Vector3(limitesTambor.center.x, r.EjeAltura, 0f);
                Mesh cuerpo = Reconstruir(m.Codigo + "_Cuerpo", vertices, normales, tangentes, uvs, cuerpoTris, Vector3.zero);
                Mesh tambor = Reconstruir(m.Codigo + "_Tambor", vertices, normales, tangentes, uvs, tamborTris, pivote);
                cuerpo = Guardar(cuerpo, m.RutaMallaCuerpo);
                tambor = Guardar(tambor, m.RutaMallaTambor);
                CrearParte("Cuerpo", modelo, cuerpo, material, Vector3.zero);
                CrearParte(TamborArma.NombreTambor, modelo, tambor, material, pivote);
                Informe.AppendLine("tambor " + m.Codigo + ": " + tamborTris.Count / 3 + " triángulos (de " + triangulos.Length / 3 + "), pivote " + pivote.ToString("F3")
                    + ", límites del tambor " + limitesTambor.min.ToString("F3") + " a " + limitesTambor.max.ToString("F3"));
            }
            finally
            {
                Object.DestroyImmediate(temporal);
            }
        }

        private static Bounds LimitesDe(Vector3[] vertices, List<int> triangulos)
        {
            if (triangulos.Count == 0)
            {
                return new Bounds();
            }
            Bounds b = new Bounds(vertices[triangulos[0]], Vector3.zero);
            foreach (int i in triangulos)
            {
                b.Encapsulate(vertices[i]);
            }
            return b;
        }

        private static Mesh Reconstruir(string nombre, Vector3[] vertices, Vector3[] normales, Vector4[] tangentes, Vector2[] uvs, List<int> triangulos, Vector3 origen)
        {
            var nuevoIndice = new Dictionary<int, int>();
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<Vector4>();
            var uv = new List<Vector2>();
            var tris = new int[triangulos.Count];
            for (int k = 0; k < triangulos.Count; k++)
            {
                int i = triangulos[k];
                if (!nuevoIndice.TryGetValue(i, out int j))
                {
                    j = v.Count;
                    nuevoIndice[i] = j;
                    v.Add(vertices[i] - origen);
                    if (normales.Length == vertices.Length) { n.Add(normales[i]); }
                    if (tangentes.Length == vertices.Length) { t.Add(tangentes[i]); }
                    if (uvs.Length == vertices.Length) { uv.Add(uvs[i]); }
                }
                tris[k] = j;
            }
            Mesh mesh = new Mesh { name = nombre };
            mesh.SetVertices(v);
            if (n.Count == v.Count) { mesh.SetNormals(n); }
            if (t.Count == v.Count) { mesh.SetTangents(t); }
            if (uv.Count == v.Count) { mesh.SetUVs(0, uv); }
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Guardar(Mesh mesh, string ruta)
        {
            Mesh existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (existente == null)
            {
                AssetDatabase.CreateAsset(mesh, ruta);
                return mesh;
            }
            existente.Clear();
            EditorUtility.CopySerialized(mesh, existente);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existente);
            return existente;
        }

        private static void CrearParte(string nombre, Transform padre, Mesh mesh, Material material, Vector3 posicion)
        {
            GameObject parte = new GameObject(nombre);
            parte.transform.SetParent(padre, false);
            parte.transform.localPosition = posicion;
            parte.AddComponent<MeshFilter>().sharedMesh = mesh;
            parte.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>El arma tirada de lado (acostada sobre su costado) con ArmaEnPiso; el modelo queda centrado en el pivote que gira.</summary>
        private static void Piso(GameObject arma, string rutaPiso, ArmaAPie tipo, string articulo, float agrandar)
        {
            GameObject raiz = new GameObject(Path.GetFileNameWithoutExtension(rutaPiso));
            GameObject pivote = new GameObject("Modelo");
            pivote.transform.SetParent(raiz.transform, false);
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(arma, pivote.transform);
            modelo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            if (agrandar > 0f)
            {
                modelo.transform.localScale = Vector3.one * agrandar;
            }
            Bounds b = Limites(raiz);
            modelo.transform.localPosition -= b.center - raiz.transform.position;
            ArmaEnPiso piso = raiz.AddComponent<ArmaEnPiso>();
            SerializedObject so = new SerializedObject(piso);
            so.FindProperty("arma").enumValueIndex = (int)tipo;
            so.FindProperty("nombre").stringValue = articulo;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, rutaPiso);
            Object.DestroyImmediate(raiz);
            Informe.AppendLine("prefab del piso " + rutaPiso);
        }

        // ---------- Granada (W09) ----------

        private static void GranadaModelo()
        {
            string rutaFbx = CarpetaGranada + "W09_Granada.fbx";
            if (!File.Exists(rutaFbx))
            {
                Informe.AppendLine("falta " + rutaFbx + " (se genera con imageassets/blender/granada/W09_Granada.py)");
                return;
            }
            Importacion(rutaFbx);
            Material cuerpo = MaterialGranada("W09_Granada", new Color(0.22f, 0.27f, 0.12f), 0.35f, 0.45f);
            Material metal = MaterialGranada("W09_Metal", new Color(0.55f, 0.55f, 0.5f), 0.9f, 0.65f);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(rutaFbx);
            GameObject raiz = new GameObject("W09_Granada");
            GameObject modelo = new GameObject("Modelo");
            modelo.transform.SetParent(raiz.transform, false);
            GameObject malla = (GameObject)PrefabUtility.InstantiatePrefab(fbx, modelo.transform);
            PrefabUtility.UnpackPrefabInstance(malla, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            malla.name = "Malla";
            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = r.name.StartsWith("Metal") ? metal : cuerpo;
            }
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
            GameObject boca = new GameObject("Boca");
            boca.transform.SetParent(raiz.transform, false);
            boca.transform.localPosition = BocaGranada;
            Bounds b = Limites(raiz);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, CarpetaGranada + "W09_Granada.prefab");
            Object.DestroyImmediate(raiz);
            Informe.AppendLine("prefab " + CarpetaGranada + "W09_Granada.prefab: tamaño " + b.size.ToString("F3") + " m");
            Piso(prefab, CarpetaGranada + "W09_Granada_Piso.prefab", ArmaAPie.Granada, "la granada", EscalaPisoGranada);
            // Esta granada es la que se lanza (ConfigArmasEspeciales.prefabGranada); sin colliders.
            ConfigArmasEspeciales config = AssetDatabase.LoadAssetAtPath<ConfigArmasEspeciales>(RutaConfigEspeciales);
            if (config != null)
            {
                SerializedObject so = new SerializedObject(config);
                so.FindProperty("prefabGranada").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                Informe.AppendLine("ConfigArmasEspeciales.prefabGranada = W09_Granada");
            }
        }

        private static Material MaterialGranada(string nombre, Color color, float metalico, float suavidad)
        {
            string ruta = CarpetaGranada + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metalico);
            material.SetFloat("_Smoothness", suavidad);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------- El jugador ----------

        private static void Jugador(Medida[] listas)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaJugador);
            try
            {
                ApuntadoIK ik = contenido.GetComponentsInChildren<ApuntadoIK>(true).FirstOrDefault(a => a.SocketArma != null);
                if (ik == null)
                {
                    Informe.AppendLine("ERROR: JugadorAPie no tiene ApuntadoIK con socket de arma");
                    return;
                }
                var porArma = new Dictionary<ArmaAPie, GameObject>();
                foreach (Medida m in listas)
                {
                    GameObject objeto = ObjetoDelSocket(ik.SocketArma, m.Objeto);
                    PonerModelo(objeto, m.Codigo, m.RutaPrefab, m.BocaEnPrefab, Vector3.zero);
                    porArma[m.Arma] = objeto;
                }
                GameObject granada = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaGranada + "W09_Granada.prefab");
                if (granada != null)
                {
                    GameObject objeto = ObjetoDelSocket(ik.SocketArma, "Granada");
                    PonerModelo(objeto, CodigoGranada, CarpetaGranada + "W09_Granada.prefab", BocaGranada, PosicionGranadaEnMano);
                    porArma[ArmaAPie.Granada] = objeto;
                }
                AplicarAgarresArmas.Aplicar(contenido);
                ArmasAPie armas = contenido.GetComponent<ArmasAPie>();
                if (armas != null)
                {
                    SerializedObject so = new SerializedObject(armas);
                    SerializedProperty lista = so.FindProperty("visualesNuevas");
                    int primera = (int)ArmasAPie.PrimeraNueva;
                    int total = System.Enum.GetValues(typeof(ArmaAPie)).Length - primera;
                    lista.arraySize = total;
                    for (int i = 0; i < total; i++)
                    {
                        ArmaAPie arma = (ArmaAPie)(primera + i);
                        if (porArma.TryGetValue(arma, out GameObject visual))
                        {
                            lista.GetArrayElementAtIndex(i).objectReferenceValue = visual;
                        }
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    Informe.AppendLine("ERROR: JugadorAPie no tiene ArmasAPie");
                }
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaJugador);
                Informe.AppendLine("jugador: " + porArma.Count + " armas nuevas en la mano y en ArmasAPie.visualesNuevas");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        // El objeto del socket (se crea si no existe) con su Boca y la luz de la boca, como las otras armas de fuego.
        private static GameObject ObjetoDelSocket(Transform socket, string nombre)
        {
            Transform objeto = socket.Find(nombre);
            if (objeto == null)
            {
                objeto = new GameObject(nombre).transform;
                objeto.SetParent(socket, false);
            }
            Transform boca = objeto.Find("Boca");
            if (boca == null)
            {
                boca = new GameObject("Boca").transform;
                boca.SetParent(objeto, false);
            }
            Light luz = boca.GetComponent<Light>();
            if (luz == null)
            {
                luz = boca.gameObject.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.color = ColorLuzBoca;
                luz.intensity = IntensidadLuzBoca;
                luz.range = AlcanceLuzBoca;
                luz.shadows = LightShadows.None;
                luz.enabled = false;
            }
            return objeto.gameObject;
        }

        // El modelo (el prefab Wxx) dentro del objeto del socket, sin sombras; la boca y su luz en la punta. Si ya está armado con ese prefab no cambia nada.
        private static void PonerModelo(GameObject objeto, string codigo, string rutaPrefab, Vector3 boca, Vector3 posicion)
        {
            Transform viejo = objeto.transform.Find(codigo);
            GameObject origen = viejo != null ? PrefabUtility.GetCorrespondingObjectFromSource(viejo.gameObject) : null;
            if (origen == null || AssetDatabase.GetAssetPath(origen) != rutaPrefab)
            {
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab), objeto.transform);
                modelo.name = codigo;
                modelo.transform.localPosition = posicion;
                modelo.transform.localRotation = Quaternion.identity;
                modelo.transform.localScale = Vector3.one;
                foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            objeto.transform.Find(codigo).localPosition = posicion;
            objeto.transform.Find("Boca").localPosition = boca;
        }

        private static Bounds Limites(GameObject raiz)
        {
            Renderer[] renderers = raiz.GetComponentsInChildren<Renderer>(true);
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}

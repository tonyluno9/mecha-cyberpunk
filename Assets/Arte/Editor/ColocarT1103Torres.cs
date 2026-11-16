using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1103: edificios lejanos nítidos. Las torres de Neón Alto (T541_Torres, piezas T20-T23) y los edificios de la calle de Nova (S2c_Nova)
    /// pasan al shader TorreNitida (niebla atenuada solo para ellos, con tope) y la torre más alta de Nova gana un letrero NOVA emisivo, un haz de luz
    /// vertical y luces rojas de aviso; las torres altas de Neón Alto llevan luces de aviso en las esquinas del remate. Sin luces reales nuevas: todo es emisión.
    /// Idempotente: reutiliza los materiales *_Nitida y rehace la raíz <see cref="Raiz"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1103Torres.Colocar
    /// </summary>
    public static class ColocarT1103Torres
    {
        public const string Raiz = "T1103_Luces";
        public const string NombreShader = "MechaCyberpunk/TorreNitida";
        public const string NombreShaderHaz = "MechaCyberpunk/TorreHaz";
        public const string CarpetaMateriales = "Assets/Arte/Suburbios/Materiales/T1103";
        public const string RutaLetrero = "Assets/Arte/Suburbios/Texturas/T1103_Nova.png";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string SufijoNitida = "_Nitida";
        public const float FactorNiebla = 0.3f;
        public const float NieblaMaxima = 0.45f;
        public const float AltoHaz = 100f;
        public const float RadioHaz = 3.5f;
        public const float AnchoLetrero = 26f;
        public const float AltoLetrero = 10.6f;
        public const float AlzadoLetrero = 12f;
        public const float LadoAviso = 1.4f;

        private static readonly string[] OriginalesT541 = { "T20", "T21", "T22", "T23" };
        private static readonly StringBuilder Informe = new StringBuilder();

        /// <summary>True si el material es del cuerpo de una torre de Neón Alto o de un edificio de Nova (original o ya nítido).</summary>
        public static bool EsMaterialDeTorre(string nombre)
        {
            string n = nombre.EndsWith(SufijoNitida) ? nombre.Substring(0, nombre.Length - SufijoNitida.Length) : nombre;
            return OriginalesT541.Contains(n) || n.StartsWith("NV01_") || n.StartsWith("NV02_") || n == "S2p_NovaCuerpo";
        }

        [MenuItem("Mecha Cyberpunk/T-1103/Torres nítidas, letrero y haz de Nova")]
        public static void Colocar()
        {
            Informe.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Directory.CreateDirectory(CarpetaMateriales);
            GameObject vieja = GameObject.Find(Raiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Transform raiz = new GameObject(Raiz).transform;

            int n541 = CambiarMateriales(GameObject.Find("T541_Torres"), r => r.GetComponentsInParent<Transform>().Any(t => t.name.StartsWith("Torre_")));
            GameObject nova = GameObject.Find("S2c_Nova");
            int nNova = CambiarMateriales(nova, r => r.GetComponentsInParent<Transform>().Any(t => t.name.StartsWith("Edificio_")));
            Informe.AppendLine("Renderers cambiados a TorreNitida: T541 " + n541 + ", Nova " + nNova);

            Transform principal = TorrePrincipalDeNova(nova);
            if (principal != null)
            {
                Bounds b = Limites(principal);
                LetreroYHaz(raiz, b);
                Avisos(raiz, "Nova", b);
            }
            else
            {
                Informe.AppendLine("FALTA la torre principal de Nova");
            }
            int torres = 0;
            foreach (Transform torre in GameObject.Find("T541_Torres").transform.Cast<Transform>().Where(t => t.name.StartsWith("Torre_")))
            {
                Transform remate = torre.Find("Remate");
                if (remate != null)
                {
                    Avisos(raiz, torre.name, Limites(remate));
                    torres++;
                }
            }
            Informe.AppendLine("Torres de Neón Alto con luces de aviso: " + torres);

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1103-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-1103 listo:\n" + Informe);
        }

        // ---------------------------------------------------------------- materiales

        private static int CambiarMateriales(GameObject raiz, System.Func<Renderer, bool> filtro)
        {
            int n = 0;
            if (raiz == null)
            {
                return n;
            }
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true).Where(filtro))
            {
                Material[] mats = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && EsMaterialDeTorre(mats[i].name))
                    {
                        Material nuevo = Nitida(mats[i].name);
                        if (nuevo != null && mats[i] != nuevo)
                        {
                            mats[i] = nuevo;
                            cambio = true;
                        }
                    }
                }
                if (cambio)
                {
                    r.sharedMaterials = mats;
                    n++;
                }
            }
            return n;
        }

        private static Material Original(string nombre)
        {
            foreach (string g in AssetDatabase.FindAssets(nombre + " t:Material"))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(ruta) == nombre && !ruta.StartsWith(CarpetaMateriales))
                {
                    return AssetDatabase.LoadAssetAtPath<Material>(ruta);
                }
            }
            return null;
        }

        private static Material Nitida(string nombreMaterial)
        {
            string baseNombre = nombreMaterial.EndsWith(SufijoNitida) ? nombreMaterial.Substring(0, nombreMaterial.Length - SufijoNitida.Length) : nombreMaterial;
            Material origen = Original(baseNombre);
            if (origen == null)
            {
                Informe.AppendLine("FALTA el material original " + baseNombre);
                return null;
            }
            Material m = Preparar(baseNombre + SufijoNitida);
            m.SetTexture("_BaseMap", origen.HasProperty("_BaseMap") ? origen.GetTexture("_BaseMap") : null);
            m.SetTextureScale("_BaseMap", origen.HasProperty("_BaseMap") ? origen.GetTextureScale("_BaseMap") : Vector2.one);
            m.SetColor("_BaseColor", origen.HasProperty("_BaseColor") ? origen.GetColor("_BaseColor") : Color.white);
            m.SetFloat("_Smoothness", origen.HasProperty("_Smoothness") ? origen.GetFloat("_Smoothness") : 0.3f);
            Texture emision = origen.HasProperty("_EmissionMap") ? origen.GetTexture("_EmissionMap") : null;
            Color colorEmision = origen.HasProperty("_EmissionColor") ? origen.GetColor("_EmissionColor") : Color.black;
            m.SetTexture("_EmissionMap", emision);
            m.SetColor("_EmissionColor", emision != null ? colorEmision : Color.black);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Preparar(string nombre)
        {
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            Shader shader = Shader.Find(NombreShader);
            if (m == null)
            {
                m = new Material(shader) { name = nombre };
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.shader = shader;
            m.SetFloat("_FactorNiebla", FactorNiebla);
            m.SetFloat("_NieblaMaxima", NieblaMaxima);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        // ---------------------------------------------------------------- torre principal, letrero, haz, avisos

        private static Bounds Limites(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        /// <summary>El edificio más alto de la calle de Nova; si empatan, el que está más al oriente (el remate de la calle).</summary>
        private static Transform TorrePrincipalDeNova(GameObject nova)
        {
            if (nova == null)
            {
                return null;
            }
            return nova.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Edificio_") && t.GetComponentInChildren<Renderer>(true) != null)
                .OrderByDescending(t => Mathf.Round(Limites(t).max.y)).ThenByDescending(t => Limites(t).center.x).FirstOrDefault();
        }

        private static void LetreroYHaz(Transform raiz, Bounds torre)
        {
            Material letrero = Preparar("T1103_LetreroNova");
            Texture2D tex = TexturaLetrero();
            letrero.SetTexture("_BaseMap", Texture2D.blackTexture);
            letrero.SetColor("_BaseColor", Color.black);
            letrero.SetTexture("_EmissionMap", tex);
            letrero.SetColor("_EmissionColor", new Color(0.6f, 2.6f, 3.4f));
            letrero.SetFloat("_EmisionDia", 1f);
            letrero.SetFloat("_FactorNiebla", 0.1f);
            EditorUtility.SetDirty(letrero);

            Transform grupo = new GameObject("Letrero_NOVA").transform;
            grupo.SetParent(raiz, false);
            float techo = torre.max.y;
            Vector3 centro = new Vector3(torre.center.x, techo + AlzadoLetrero + AltoLetrero * 0.5f, torre.center.z);
            GameObject quad = Primitiva(PrimitiveType.Quad, "Texto", grupo, letrero);
            // El quad de Unity se ve desde su -Z: gira para verse desde el poniente (donde está la ciudad).
            quad.transform.SetPositionAndRotation(centro, Quaternion.LookRotation(Vector3.right));
            quad.transform.localScale = new Vector3(AnchoLetrero, AltoLetrero, 1f);
            GameObject trasera = Primitiva(PrimitiveType.Quad, "TextoTrasero", grupo, letrero);
            trasera.transform.SetPositionAndRotation(centro + Vector3.right * 0.05f, Quaternion.LookRotation(Vector3.left));
            trasera.transform.localScale = new Vector3(AnchoLetrero, AltoLetrero, 1f);
            Material oscuro = Preparar("T1103_Mastil");
            oscuro.SetColor("_BaseColor", new Color(0.05f, 0.06f, 0.08f));
            oscuro.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(oscuro);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                GameObject mastil = Primitiva(PrimitiveType.Cube, "Mastil_" + lado, grupo, oscuro);
                mastil.transform.position = new Vector3(centro.x + 0.1f, techo + AlzadoLetrero * 0.5f + 1f, centro.z + lado * AnchoLetrero * 0.35f);
                mastil.transform.localScale = new Vector3(0.8f, AlzadoLetrero + 2f, 0.8f);
            }

            Material haz = AssetDatabase.LoadAssetAtPath<Material>(CarpetaMateriales + "/T1103_Haz.mat");
            Shader shaderHaz = Shader.Find(NombreShaderHaz);
            if (haz == null)
            {
                haz = new Material(shaderHaz) { name = "T1103_Haz" };
                AssetDatabase.CreateAsset(haz, CarpetaMateriales + "/T1103_Haz.mat");
            }
            haz.shader = shaderHaz;
            haz.SetColor("_Color", new Color(0.25f, 0.9f, 1.8f));
            EditorUtility.SetDirty(haz);
            GameObject cil = Primitiva(PrimitiveType.Cylinder, "Haz_Nova", raiz, haz);
            cil.transform.position = new Vector3(torre.center.x, techo + AltoHaz * 0.5f, torre.center.z);
            cil.transform.localScale = new Vector3(RadioHaz * 2f, AltoHaz * 0.5f, RadioHaz * 2f);
            Informe.AppendLine("Letrero NOVA y haz de " + AltoHaz + " m sobre la torre de Nova en " + torre.center.ToString("F1") + ", techo " + techo.ToString("F1"));
        }

        private static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Material material)
        {
            GameObject g = GameObject.CreatePrimitive(tipo);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.name = nombre;
            g.transform.SetParent(padre, true);
            MeshRenderer r = g.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return g;
        }

        private static void Avisos(Transform raiz, string torre, Bounds b)
        {
            Material rojo = Preparar("T1103_LuzAviso");
            rojo.SetColor("_BaseColor", new Color(0.2f, 0f, 0f));
            rojo.SetTexture("_EmissionMap", Texture2D.whiteTexture);
            rojo.SetColor("_EmissionColor", new Color(7f, 0.12f, 0.1f));
            rojo.SetFloat("_EmisionDia", 0.6f);
            rojo.SetFloat("_FactorNiebla", 0.05f);
            EditorUtility.SetDirty(rojo);
            Transform grupo = new GameObject("Avisos_" + torre).transform;
            grupo.SetParent(raiz, false);
            List<Renderer> rs = new List<Renderer>();
            float x0 = b.min.x + LadoAviso, x1 = b.max.x - LadoAviso, z0 = b.min.z + LadoAviso, z1 = b.max.z - LadoAviso;
            int k = 0;
            foreach (float x in new[] { x0, x1 })
            {
                foreach (float z in new[] { z0, z1 })
                {
                    GameObject luz = Primitiva(PrimitiveType.Cube, "LuzAviso_" + torre + "_" + k++, grupo, rojo);
                    luz.transform.position = new Vector3(x, b.max.y + LadoAviso * 0.4f, z);
                    luz.transform.localScale = Vector3.one * LadoAviso;
                    rs.Add(luz.GetComponent<Renderer>());
                }
            }
            ConfigNeon config = AssetDatabase.LoadAssetAtPath<ConfigNeon>("Assets/_Proyecto/Mundo/Datos/ConfigNeon_Mundo.asset");
            ParpadeoNeon parpadeo = grupo.gameObject.AddComponent<ParpadeoNeon>();
            parpadeo.Configurar(config, rs.ToArray(), true, 1103 + grupo.GetSiblingIndex(), false);
        }

        // ---------------------------------------------------------------- textura del letrero (letras de 5x7 celdas)

        private static readonly string[][] Letras =
        {
            new[] { "10001", "11001", "10101", "10101", "10011", "10001", "10001" },   // N
            new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },   // O
            new[] { "10001", "10001", "10001", "10001", "01010", "01010", "00100" },   // V
            new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },   // A
        };

        private static Texture2D TexturaLetrero()
        {
            const int celda = 16, margen = 2, cols = 4 * 5 + 3 + 2 * margen, filas = 7 + 2 * margen;
            Texture2D t = new Texture2D(cols * celda, filas * celda, TextureFormat.RGBA32, false);
            Color[] px = new Color[t.width * t.height];
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = Color.black;
            }
            for (int l = 0; l < Letras.Length; l++)
            {
                for (int fila = 0; fila < 7; fila++)
                {
                    for (int c = 0; c < 5; c++)
                    {
                        if (Letras[l][fila][c] != '1')
                        {
                            continue;
                        }
                        int cx = (margen + l * 6 + c) * celda;
                        int cy = (margen + (6 - fila)) * celda;
                        for (int y = 0; y < celda; y++)
                        {
                            for (int x = 0; x < celda; x++)
                            {
                                px[(cy + y) * t.width + cx + x] = Color.white;
                            }
                        }
                    }
                }
            }
            // Marco fino alrededor.
            for (int i = 0; i < t.width; i++)
            {
                for (int g = 0; g < 4; g++)
                {
                    px[g * t.width + i] = Color.white;
                    px[(t.height - 1 - g) * t.width + i] = Color.white;
                }
            }
            for (int j = 0; j < t.height; j++)
            {
                for (int g = 0; g < 4; g++)
                {
                    px[j * t.width + g] = Color.white;
                    px[j * t.width + t.width - 1 - g] = Color.white;
                }
            }
            t.SetPixels(px);
            Directory.CreateDirectory(Path.GetDirectoryName(RutaLetrero));
            File.WriteAllBytes(RutaLetrero, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(RutaLetrero);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(RutaLetrero);
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(RutaLetrero);
        }
    }
}

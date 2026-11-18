using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1351: material del mar (shader MechaCyberpunk/Mar) y funciones para colocar agua en las escenas.
    /// Las usan los scripts de colocación del Malecón y del Puerto: CrearPlanoMar (el agua) y CrearOrilla (franja de espuma).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorMarT1351.Crear
    /// </summary>
    public static class CreadorMarT1351
    {
        public const string NombreShader = "MechaCyberpunk/Mar";
        public const string RutaMaterialMar = "Assets/_Proyecto/Mundo/Materiales/Mar.mat";
        public const string RutaMaterialOrilla = "Assets/_Proyecto/Mundo/Materiales/Mar_Orilla.mat";
        public const string NombreCapaAgua = "Water";

        private const int CapaAguaRespaldo = 4;
        private const float LadoCeldaM = 4f;
        private const int CeldasMin = 2;
        private const int CeldasMax = 200;
        private const float MargenCajaM = 1f;
        private const float AnchoOrillaPorDefectoM = 3f;
        private const float AlturaOrillaM = 0.03f;
        private const int ColaOrilla = 2910;

        [MenuItem("Mecha Cyberpunk/Crear T-1351 (material del mar)")]
        public static void Crear()
        {
            ObtenerMaterialMar();
            ObtenerMaterialOrilla();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-1351 creada: Mar.mat y Mar_Orilla.mat con el shader " + NombreShader + ".");
        }

        /// <summary>Material del agua; lo crea con los valores por defecto del shader si no existe.</summary>
        public static Material ObtenerMaterialMar()
        {
            return ObtenerMaterial(RutaMaterialMar, 0f);
        }

        /// <summary>Material de la franja de espuma (solo espuma, dibuja después del agua).</summary>
        public static Material ObtenerMaterialOrilla()
        {
            Material material = ObtenerMaterial(RutaMaterialOrilla, 1f);
            if (material != null && material.renderQueue != ColaOrilla)
            {
                material.renderQueue = ColaOrilla;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        private static Material ObtenerMaterial(string ruta, float soloEspuma)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            Shader shader = Shader.Find(NombreShader);
            if (shader == null)
            {
                Debug.LogError("[Mundo] T-1351: no encuentro el shader " + NombreShader + ".");
                return material;
            }
            if (material == null)
            {
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(ruta) };
                material.SetFloat("_SoloEspuma", soloEspuma);
                AssetDatabase.CreateAsset(material, ruta);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        /// <summary>
        /// Crea un plano de agua centrado en <paramref name="centro"/> (XZ) a la altura <paramref name="y"/>, de tamaño
        /// <paramref name="tam"/> (X, Z en metros), en la capa Water, sin collider y con el material Mar.
        /// Malla de rejilla de unos 4 m por celda (tope de 200 por lado) para que las olas del vértice se vean.
        /// </summary>
        public static GameObject CrearPlanoMar(Transform padre, Vector3 centro, Vector2 tam, float y)
        {
            GameObject objeto = new GameObject("Mar");
            if (padre != null)
            {
                objeto.transform.SetParent(padre, false);
            }
            objeto.transform.position = new Vector3(centro.x, y, centro.z);
            objeto.layer = CapaAgua();

            MeshFilter filtro = objeto.AddComponent<MeshFilter>();
            filtro.sharedMesh = CrearMallaRejilla(tam);
            MeshRenderer render = objeto.AddComponent<MeshRenderer>();
            render.sharedMaterial = ObtenerMaterialMar();
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            return objeto;
        }

        /// <summary>
        /// Crea una franja de espuma a lo largo de <paramref name="linea"/> (puntos del mundo, en orden) a la altura
        /// <paramref name="y"/>. La franja se extiende hacia la derecha del sentido de la línea (hacia el agua):
        /// ordena los puntos con la tierra a la izquierda. Capa Water, sin collider, material Mar_Orilla.
        /// </summary>
        public static GameObject CrearOrilla(Transform padre, IList<Vector3> linea, float y, float ancho = AnchoOrillaPorDefectoM)
        {
            if (linea == null || linea.Count < 2)
            {
                Debug.LogError("[Mundo] T-1351: CrearOrilla necesita al menos 2 puntos.");
                return null;
            }
            GameObject objeto = new GameObject("OrillaMar");
            if (padre != null)
            {
                objeto.transform.SetParent(padre, false);
            }
            objeto.transform.position = Vector3.zero;
            objeto.layer = CapaAgua();
            objeto.AddComponent<MeshFilter>().sharedMesh = CrearMallaOrilla(linea, y + AlturaOrillaM, ancho);
            MeshRenderer render = objeto.AddComponent<MeshRenderer>();
            render.sharedMaterial = ObtenerMaterialOrilla();
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            return objeto;
        }

        private static int CapaAgua()
        {
            int capa = LayerMask.NameToLayer(NombreCapaAgua);
            return capa >= 0 ? capa : CapaAguaRespaldo;
        }

        // Malla local centrada en el origen (el objeto pone la posición). Sin UV: el shader trabaja con la posición del mundo.
        internal static Mesh CrearMallaRejilla(Vector2 tam)
        {
            int celdasX = Mathf.Clamp(Mathf.CeilToInt(tam.x / LadoCeldaM), CeldasMin, CeldasMax);
            int celdasZ = Mathf.Clamp(Mathf.CeilToInt(tam.y / LadoCeldaM), CeldasMin, CeldasMax);
            int vx = celdasX + 1;
            int vz = celdasZ + 1;
            Vector3[] vertices = new Vector3[vx * vz];
            Vector3[] normales = new Vector3[vx * vz];
            for (int j = 0; j < vz; j++)
            {
                for (int i = 0; i < vx; i++)
                {
                    float u = (float)i / celdasX - 0.5f;
                    float v = (float)j / celdasZ - 0.5f;
                    vertices[j * vx + i] = new Vector3(u * tam.x, 0f, v * tam.y);
                    normales[j * vx + i] = Vector3.up;
                }
            }
            int[] indices = new int[celdasX * celdasZ * 6];
            int k = 0;
            for (int j = 0; j < celdasZ; j++)
            {
                for (int i = 0; i < celdasX; i++)
                {
                    int a = j * vx + i;
                    int b = a + 1;
                    int c = a + vx;
                    int d = c + 1;
                    indices[k++] = a; indices[k++] = c; indices[k++] = b;
                    indices[k++] = b; indices[k++] = c; indices[k++] = d;
                }
            }
            Mesh malla = new Mesh { name = "MarRejilla", indexFormat = IndexFormat.UInt32 };
            malla.vertices = vertices;
            malla.normals = normales;
            malla.triangles = indices;
            malla.RecalculateBounds();
            Bounds caja = malla.bounds;
            caja.Expand(new Vector3(0f, MargenCajaM * 2f, 0f)); // las olas suben y bajan en el vértice
            malla.bounds = caja;
            return malla;
        }

        // Cinta a lo largo de la línea: R = 1 (espuma), G = 0 en la orilla y 1 en el borde exterior (se desvanece).
        internal static Mesh CrearMallaOrilla(IList<Vector3> linea, float y, float ancho)
        {
            int n = linea.Count;
            Vector3[] vertices = new Vector3[n * 2];
            Vector3[] normales = new Vector3[n * 2];
            Color32[] colores = new Color32[n * 2];
            for (int i = 0; i < n; i++)
            {
                Vector3 anterior = linea[Mathf.Max(i - 1, 0)];
                Vector3 siguiente = linea[Mathf.Min(i + 1, n - 1)];
                Vector3 tangente = siguiente - anterior;
                tangente.y = 0f;
                tangente = tangente.sqrMagnitude > 1e-6f ? tangente.normalized : Vector3.forward;
                Vector3 derecha = new Vector3(tangente.z, 0f, -tangente.x);
                Vector3 punto = new Vector3(linea[i].x, y, linea[i].z);
                vertices[i * 2] = punto;
                vertices[i * 2 + 1] = punto + derecha * ancho;
                normales[i * 2] = Vector3.up;
                normales[i * 2 + 1] = Vector3.up;
                colores[i * 2] = new Color32(255, 0, 0, 255);
                colores[i * 2 + 1] = new Color32(255, 255, 0, 255);
            }
            int[] indices = new int[(n - 1) * 6];
            int k = 0;
            for (int i = 0; i < n - 1; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                indices[k++] = a; indices[k++] = c; indices[k++] = b;
                indices[k++] = b; indices[k++] = c; indices[k++] = d;
            }
            Mesh malla = new Mesh { name = "MarOrilla" };
            malla.vertices = vertices;
            malla.normals = normales;
            malla.colors32 = colores;
            malla.triangles = indices;
            malla.RecalculateBounds();
            return malla;
        }
    }
}

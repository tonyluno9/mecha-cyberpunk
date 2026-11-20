using System.IO;
using MechaCyberpunk.UI.Mapa;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Genera la imagen cenital del mapa (T-747) desde la ciudad real sin tocar la escena: abre Ciudad_Prologo, cambia en memoria los materiales por uno
    /// que pinta la altura, la fotografía con una cámara ortográfica desde arriba (norte arriba) sobre el cuadro de <see cref="MapaImagen"/> y, con
    /// esas alturas, dibuja el estilo de M10: color por barrio, contornos de los edificios, sombras y calles oscuras. La escena nunca se guarda.
    /// Se corre con -executeMethod MechaCyberpunk.UI.Editor.CapturadorMapaCiudad.Generar.
    /// </summary>
    public static class CapturadorMapaCiudad
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaPng = "Assets/_Proyecto/UI/Resources/Mapa/MapaCiudad.png";
        private const string RutaShader = "Assets/_Proyecto/UI/Editor/AlturaMapaT747.shader";

        public static void Generar()
        {
            float[] alturas = FotografiarAlturas(out int ancho, out int alto);
            Texture2D pintada = EstiloM10.Pintar(alturas, ancho, alto);
            byte[] png = pintada.EncodeToPNG();
            Object.DestroyImmediate(pintada);
            Directory.CreateDirectory(Path.GetDirectoryName(RutaPng));
            File.WriteAllBytes(RutaPng, png);
            AssetDatabase.ImportAsset(RutaPng, ImportAssetOptions.ForceUpdate);
            TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(RutaPng);
            importador.textureType = TextureImporterType.Default;
            importador.sRGBTexture = true;
            importador.mipmapEnabled = true;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.filterMode = FilterMode.Trilinear;
            importador.maxTextureSize = 4096;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.alphaSource = TextureImporterAlphaSource.None;
            importador.SaveAndReimport();
            Debug.Log("[T-747] Mapa generado " + ancho + "x" + alto + " en " + RutaPng);
        }

        /// <summary>Las alturas (m) de cada píxel del cuadro, fila 0 al sur; ancho y alto salen de <see cref="MapaImagen"/>.</summary>
        public static float[] FotografiarAlturas(out int ancho, out int alto)
        {
            ancho = MapaImagen.AnchoPx;
            alto = MapaImagen.AltoPx;
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(RutaShader);
            Material material = new Material(shader);
            Preparar(material);

            GameObject objeto = new GameObject("CamaraMapaT747");
            Camera camara = objeto.AddComponent<Camera>();
            UniversalAdditionalCameraData extra = objeto.AddComponent<UniversalAdditionalCameraData>();
            extra.renderPostProcessing = false;
            extra.antialiasing = AntialiasingMode.None;
            extra.renderShadows = false;
            camara.orthographic = true;
            camara.orthographicSize = (MapaImagen.MaxZ - MapaImagen.MinZ) * 0.5f;
            camara.aspect = ancho / (float)alto;
            Vector2 centro = MapaImagen.Centro;
            objeto.transform.position = new Vector3(centro.x, 600f, centro.y);
            objeto.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camara.nearClipPlane = 1f;
            camara.farClipPlane = 1200f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            camara.allowMSAA = false;
            camara.allowHDR = false;
            RenderTexture rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) { antiAliasing = 1 };
            camara.targetTexture = rt;
            camara.Render();

            RenderTexture.active = rt;
            Texture2D leido = new Texture2D(ancho, alto, TextureFormat.RGBA32, false, true);
            leido.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            leido.Apply();
            RenderTexture.active = null;
            Color32[] pixeles = leido.GetPixels32();
            float[] alturas = new float[ancho * alto];
            for (int i = 0; i < alturas.Length; i++)
            {
                int v = pixeles[i].r * 256 + pixeles[i].g;
                alturas[i] = v <= 0 ? -2f : v / 100f - 2f;
            }
            camara.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(objeto);
            Object.DestroyImmediate(leido);
            Object.DestroyImmediate(material);
            return alturas;
        }

        // Deja visible solo lo que cuenta para el plano (mallas del suelo y de los edificios) y las pinta con el material de alturas.
        private static void Preparar(Material material)
        {
            foreach (LODGroup lod in Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                lod.ForceLOD(0);
            }
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!(r is MeshRenderer malla) || !Cuenta(malla))
                {
                    r.enabled = false;
                    continue;
                }
                Material[] materiales = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < materiales.Length; i++)
                {
                    materiales[i] = material;
                }
                r.sharedMaterials = materiales;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        /// <summary>Si una malla sale en el plano: nada bajo tierra, nada gigante (cielo, fondos) y nada fino colgado en el aire (cables, letreros sueltos).</summary>
        public static bool Cuenta(MeshRenderer r)
        {
            Bounds b = r.bounds;
            if (b.min.y < -5f || Mathf.Max(b.size.x, b.size.z) > 250f)
            {
                return false;
            }
            bool fino = Mathf.Min(b.size.x, b.size.z) < 0.8f && b.size.y < 1.2f;
            return !(fino && b.min.y > 2.5f);
        }
    }
}

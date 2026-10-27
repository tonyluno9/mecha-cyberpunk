using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-312: asfalto mojado con carácter, sin tocar la geometría que se camina. Crea por código dos texturas pequeñas
    /// (asfalto de 512 px, con el brillo del reflejo en el alfa, y máscara de charco de 128 px), se las pone a
    /// Barrio_Asfalto y Barrio_Charco, vuelve el charco una malla translúcida de bordes suaves y añade líneas de borde de calle
    /// sin collider. Lo corre <see cref="CreadorAmbiente"/> después de CreadorBarrio, que reinicia el color del asfalto.
    /// Se puede repetir: las texturas solo se reescriben si cambian y las líneas se rehacen.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.VestirAsfalto.Crear
    /// </summary>
    public static class VestirAsfalto
    {
        internal const string RutaTexturas = "Assets/_Proyecto/Mundo/Texturas";
        internal const string RutaTexturaAsfalto = RutaTexturas + "/Asfalto_Mojado.png";
        internal const string RutaTexturaCharco = RutaTexturas + "/Charco_Mascara.png";
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaMaterialAsfalto = RutaMateriales + "/Barrio_Asfalto.mat";
        private const string RutaMaterialCharco = RutaMateriales + "/Barrio_Charco.mat";
        private const string RutaMaterialRaya = RutaMateriales + "/Final_Raya.mat";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string NombreBordes = "BordesCalle";

        // Textura del asfalto: cada repetición cubre MetrosPorRepeticion en X y Z (una cara de cubo estirada).
        private const int LadoAsfalto = 512;
        private const int LadoCharco = 128;
        internal const float MetrosPorRepeticion = 12f;
        private const float MargenAsfalto = 4f;      // el cubo del asfalto sobresale 2 m por lado (CreadorBarrio)
        private const int SemillaTono = 3121;
        private const int SemillaBrillo = 3122;
        private const int SemillaParche = 3123;
        private const int SemillaGrano = 3124;
        private const int SemillaForma = 3125;
        private const int CeldasTono = 4;
        private const int CeldasBrillo = 6;
        private const int CeldasParche = 3;
        private const int Octavas = 3;
        private const float TonoBase = 0.62f;
        private const float AmplitudTono = 0.34f;
        private const float UmbralParche = 0.62f;
        private const float AnchoBordeParche = 0.04f;
        private const float OscurecerParche = 0.6f;
        private const float AmplitudGrano = 0.05f;
        private const float TintaRoja = 0.94f;
        private const float TintaVerde = 0.98f;
        private const float TintaAzul = 1.1f;
        internal const float BrilloBase = 0.8f;
        internal const float BrilloMinimo = 0.45f;
        private const float AmplitudBrillo = 0.3f;
        private const float BrilloParche = 0.15f;

        // Charco: elipse irregular de borde suave. El alfa llega a 1 en el centro.
        private const float RadioCharco = 0.78f;
        private const float VariacionRadio = 0.14f;
        private const float BordeSuaveCharco = 0.2f;
        private const int LobulosCharco = 5;
        private const float PesoLobuloPrincipal = 0.6f;
        private static readonly Color ColorCharco = new Color(0.012f, 0.016f, 0.03f);
        private const float SuavidadCharco = 0.98f;

        // Líneas de borde de calle (m). La calle este-oeste está en z = 0, las norte-sur en x = ±47; mismas medidas de CreadorBarrio.
        private const float AlturaLinea = 0.015f;
        private const float ColocacionBorde = 6.2f;
        private const float AnchoLinea = 0.15f;
        private const float AltoLinea = 0.02f;
        private const float CentroCalleNorteSur = 47f;
        private const float DespejeCruce = 13f;      // más allá de la cebra (9.5) y su ancho
        private const float MitadLargoBarrioX = 130f;
        private const float MitadLargoBarrioZ = 83f;
        private const float ZonaCruceNorteSur = 17f;

        [MenuItem("Mecha Cyberpunk/Crear T-312 (asfalto mojado)")]
        public static void Crear()
        {
            Aplicar();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-312 creada: asfalto mojado, charcos suaves y líneas de borde.");
        }

        /// <summary>Texturas, materiales y líneas de borde. Los charcos de cuneta los pone CreadorAmbiente con el resto de los charcos.</summary>
        internal static void Aplicar()
        {
            Texture2D asfalto = EscribirTextura(RutaTexturaAsfalto, DibujarAsfalto(), LadoAsfalto, 8);
            Texture2D charco = EscribirTextura(RutaTexturaCharco, DibujarCharco(), LadoCharco, 1);
            PrepararMaterialAsfalto(asfalto);
            PrepararMaterialCharco(AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCharco), charco);
            PonerLineasDeBorde();
        }

        // ---------------------------------------------------------------- materiales

        private static void PrepararMaterialAsfalto(Texture2D textura)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialAsfalto);
            if (material == null)
            {
                return;
            }
            // El brillo sale del alfa de la textura; el color base del material sigue oscuro (lo fija CreadorBarrio).
            material.SetTexture("_BaseMap", textura);
            // La cara de arriba del cubo estira el UV de 0 a 1 en todo el barrio: se repite una vez por cada MetrosPorRepeticion.
            material.SetTextureScale("_BaseMap", new Vector2(
                (2f * CreadorBarrio.MitadBarrioX + MargenAsfalto) / MetrosPorRepeticion,
                (2f * CreadorBarrio.MitadBarrioZ + MargenAsfalto) / MetrosPorRepeticion));
            material.SetFloat("_SmoothnessTextureChannel", 1f);
            material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            EditorUtility.SetDirty(material);
        }

        /// <summary>Hace del charco una malla translúcida con la máscara de bordes suaves, muy lisa y sin sombra.</summary>
        internal static void PrepararMaterialCharco(Material material, Texture2D mascara)
        {
            if (material == null)
            {
                return;
            }
            material.SetTexture("_BaseMap", mascara);
            material.SetColor("_BaseColor", ColorCharco);
            material.SetFloat("_Smoothness", SuavidadCharco);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            EditorUtility.SetDirty(material);
        }

        // ---------------------------------------------------------------- texturas

        private static Color[] DibujarAsfalto()
        {
            Color[] pixeles = new Color[LadoAsfalto * LadoAsfalto];
            for (int y = 0; y < LadoAsfalto; y++)
            {
                for (int x = 0; x < LadoAsfalto; x++)
                {
                    float u = (x + 0.5f) / LadoAsfalto;
                    float v = (y + 0.5f) / LadoAsfalto;
                    float tonoRuido = Ruido(u, v, CeldasTono, SemillaTono);
                    float parche = Mathf.SmoothStep(0f, 1f, (Ruido(u, v, CeldasParche, SemillaParche) - UmbralParche) / AnchoBordeParche + 0.5f);
                    float grano = (Hash(x, y, SemillaGrano) - 0.5f) * 2f * AmplitudGrano;

                    float tono = TonoBase + (tonoRuido - 0.5f) * 2f * AmplitudTono + grano;
                    tono *= Mathf.Lerp(1f, OscurecerParche, parche);
                    tono = Mathf.Clamp01(tono);

                    // Alfa = brillo: zonas más secas y más mojadas; el parche nuevo es más liso.
                    float brillo = BrilloBase + (Ruido(u, v, CeldasBrillo, SemillaBrillo) - 0.5f) * 2f * AmplitudBrillo + parche * BrilloParche;
                    brillo = Mathf.Clamp(brillo, BrilloMinimo, 1f);

                    pixeles[y * LadoAsfalto + x] = new Color(
                        Mathf.Clamp01(tono * TintaRoja), Mathf.Clamp01(tono * TintaVerde), Mathf.Clamp01(tono * TintaAzul), brillo);
                }
            }
            return pixeles;
        }

        private static Color[] DibujarCharco()
        {
            Color[] pixeles = new Color[LadoCharco * LadoCharco];
            float fase = Hash(1, 2, SemillaForma) * Mathf.PI * 2f;
            float faseDos = Hash(3, 4, SemillaForma) * Mathf.PI * 2f;
            for (int y = 0; y < LadoCharco; y++)
            {
                for (int x = 0; x < LadoCharco; x++)
                {
                    float dx = ((x + 0.5f) / LadoCharco - 0.5f) * 2f;
                    float dy = ((y + 0.5f) / LadoCharco - 0.5f) * 2f;
                    float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                    float angulo = Mathf.Atan2(dy, dx);
                    float radio = RadioCharco + VariacionRadio * (PesoLobuloPrincipal * Mathf.Sin(LobulosCharco * angulo + fase) + (1f - PesoLobuloPrincipal) * Mathf.Sin((LobulosCharco + 2) * angulo + faseDos));
                    float alfa = Mathf.Clamp01((radio - distancia) / BordeSuaveCharco);
                    alfa = alfa * alfa * (3f - 2f * alfa);
                    pixeles[y * LadoCharco + x] = new Color(1f, 1f, 1f, alfa);
                }
            }
            return pixeles;
        }

        // Ruido de valores que se repite sin costura: la cuadrícula da la vuelta cada `celdas` celdas.
        private static float Ruido(float u, float v, int celdas, int semilla)
        {
            float suma = 0f;
            float amplitud = 1f;
            float total = 0f;
            int frecuencia = celdas;
            for (int octava = 0; octava < Octavas; octava++)
            {
                suma += amplitud * RuidoSimple(u, v, frecuencia, semilla + octava);
                total += amplitud;
                amplitud *= 0.5f;
                frecuencia *= 2;
            }
            return suma / total;
        }

        private static float RuidoSimple(float u, float v, int celdas, int semilla)
        {
            float fx = u * celdas;
            float fy = v * celdas;
            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            float tx = Mathf.SmoothStep(0f, 1f, fx - x0);
            float ty = Mathf.SmoothStep(0f, 1f, fy - y0);
            int x1 = (x0 + 1) % celdas;
            int y1 = (y0 + 1) % celdas;
            x0 %= celdas;
            y0 %= celdas;
            float abajo = Mathf.Lerp(Hash(x0, y0, semilla), Hash(x1, y0, semilla), tx);
            float arriba = Mathf.Lerp(Hash(x0, y1, semilla), Hash(x1, y1, semilla), tx);
            return Mathf.Lerp(abajo, arriba, ty);
        }

        // Número pseudoaleatorio estable entre 0 y 1 para una celda; no depende de System.Random para dar siempre lo mismo.
        private static float Hash(int x, int y, int semilla)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + semilla * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static Texture2D EscribirTextura(string ruta, Color[] pixeles, int lado, int anisotropia)
        {
            if (!Directory.Exists(RutaTexturas))
            {
                Directory.CreateDirectory(RutaTexturas);
            }
            Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            textura.SetPixels(pixeles);
            byte[] bytes = textura.EncodeToPNG();
            Object.DestroyImmediate(textura);
            if (!File.Exists(ruta) || !System.Linq.Enumerable.SequenceEqual(File.ReadAllBytes(ruta), bytes))
            {
                File.WriteAllBytes(ruta, bytes);
            }
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
            if (importador.textureCompression != TextureImporterCompression.Uncompressed || !importador.mipmapEnabled
                || importador.wrapMode != TextureWrapMode.Repeat || importador.anisoLevel != anisotropia || !importador.sRGBTexture)
            {
                importador.textureType = TextureImporterType.Default;
                importador.sRGBTexture = true;
                importador.alphaSource = TextureImporterAlphaSource.FromInput;
                importador.alphaIsTransparency = false;
                importador.textureCompression = TextureImporterCompression.Uncompressed;
                importador.mipmapEnabled = true;
                importador.wrapMode = TextureWrapMode.Repeat;
                importador.filterMode = FilterMode.Bilinear;
                importador.anisoLevel = anisotropia;
                importador.maxTextureSize = lado;
                importador.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        // ---------------------------------------------------------------- líneas de borde

        private static void PonerLineasDeBorde()
        {
            Material raya = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialRaya);
            if (raya == null)
            {
                raya = AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + "/Barrio_RayaBlanca.mat");
            }
            GameObject barrio = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform calles = barrio.transform.Find("Calles");
            Transform anterior = calles.Find(NombreBordes);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior.gameObject);
            }
            Transform grupo = new GameObject(NombreBordes).transform;
            grupo.SetParent(calles, false);

            // Calle este-oeste: tres tramos entre los dos cruces y los extremos, a cada lado.
            float limite = CentroCalleNorteSur - DespejeCruce;
            float[][] tramosX = { new[] { -MitadLargoBarrioX, -CentroCalleNorteSur - DespejeCruce }, new[] { -limite, limite }, new[] { CentroCalleNorteSur + DespejeCruce, MitadLargoBarrioX } };
            foreach (float lado in new[] { -1f, 1f })
            {
                foreach (float[] tramo in tramosX)
                {
                    Linea(grupo, raya, new Vector3((tramo[0] + tramo[1]) / 2f, AlturaLinea, lado * ColocacionBorde), new Vector3(tramo[1] - tramo[0], AltoLinea, AnchoLinea));
                }
            }
            // Calles norte-sur: dos tramos por cada lado, uno a cada lado del cruce.
            foreach (float cx in new[] { -CentroCalleNorteSur, CentroCalleNorteSur })
            {
                foreach (float lado in new[] { -1f, 1f })
                {
                    foreach (float signoZ in new[] { -1f, 1f })
                    {
                        float inicio = ZonaCruceNorteSur;
                        float fin = MitadLargoBarrioZ;
                        Linea(grupo, raya, new Vector3(cx + lado * ColocacionBorde, AlturaLinea, signoZ * (inicio + fin) / 2f), new Vector3(AnchoLinea, AltoLinea, fin - inicio));
                    }
                }
            }
            PrefabUtility.SaveAsPrefabAsset(barrio, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(barrio);
        }

        private static void Linea(Transform padre, Material material, Vector3 posicion, Vector3 escala)
        {
            GameObject linea = GameObject.CreatePrimitive(PrimitiveType.Cube);
            linea.name = "Borde";
            Object.DestroyImmediate(linea.GetComponent<Collider>());
            linea.transform.SetParent(padre, false);
            linea.transform.localPosition = posicion;
            linea.transform.localScale = escala;
            linea.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}

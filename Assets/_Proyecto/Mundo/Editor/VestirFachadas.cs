using System;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Mundo.Fachadas;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-081: viste los edificios del barrio con detalle. Genera por código tres texturas de ventanas (pobre, media y alta) con su mapa
    /// de emisión y sus materiales, y a cada lado visible de cada edificio (los que no tocan a otro) le pone: una fila de
    /// Fachada_Modular en planta baja (más cubos que rellenan lo que sobre de 4 m), un quad de ventanas de 6 m a la azotea y una cornisa.
    /// Los Lugar_* no se tocan. Lo llama CreadorBarrio antes de guardar el prefab. Todo es determinista (semillas fijas).
    /// </summary>
    internal static class VestirFachadas
    {
        internal const string RutaTexturas = "Assets/_Proyecto/Mundo/Texturas";
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaFachadaModular = "Assets/_Proyecto/Mundo/Prefabs/Kit/Fachada_Modular.prefab";

        private const int LadoTextura = 512;
        private const int CeldasPorLado = 4;
        private const int PixelesCelda = LadoTextura / CeldasPorLado;
        private const float MetrosMosaico = 12f;
        private const float AlturaPlantaBaja = 6f;
        private const float AnchoModulo = 4f;
        private const float RellenoMinimo = 0.05f;
        private const float SalienteMaximo = 0.44f;     // la fachada no sobresale más de 0.45 m del muro
        private const float ProfundidadRelleno = 0.4f;
        private const float SeparacionVentanas = 0.02f;
        private const float AlturaVentanasMinima = 1f;
        private const float DesplazamientoRevision = 0.25f;
        private const float GrosorCornisa = 0.5f;
        private const float AltoCornisa = 0.3f;
        private const float SobresaleCornisa = 0.25f;
        private const float EmisionVentanas = 1f;
        private const int MargenVentanaX = 13;
        private const int MargenVentanaY = 10;
        private const int GrosorMarco = 6;
        private const int MargenEmision = 2;

        internal static readonly string[] Variantes = { "Pobre", "Media", "Alta" };

        private sealed class Variante
        {
            public Color Muro;
            public Color Marco;
            public Color Vidrio;
            public int[] Prendidas;
            public Color[] ColoresPrendidas;
            public int Semilla;
        }

        private static readonly Color Calido = new Color(1f, 0.72f, 0.38f);
        private static readonly Color Cian = new Color(0.2f, 0.9f, 1f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.75f);

        private static readonly Variante[] Datos =
        {
            new Variante
            {
                Muro = new Color(0.56f, 0.4f, 0.3f), Marco = new Color(0.22f, 0.15f, 0.11f), Vidrio = new Color(0.05f, 0.06f, 0.08f),
                Prendidas = new[] { 1, 4, 6, 11, 14 }, ColoresPrendidas = new[] { Calido, Calido, Calido, Calido, Cian }, Semilla = 1
            },
            new Variante
            {
                Muro = new Color(0.33f, 0.35f, 0.41f), Marco = new Color(0.14f, 0.15f, 0.2f), Vidrio = new Color(0.04f, 0.06f, 0.1f),
                Prendidas = new[] { 0, 3, 5, 9, 10, 13 }, ColoresPrendidas = new[] { Calido, Calido, Cian, Calido, Magenta, Calido }, Semilla = 2
            },
            new Variante
            {
                Muro = new Color(0.17f, 0.19f, 0.28f), Marco = new Color(0.1f, 0.42f, 0.52f), Vidrio = new Color(0.03f, 0.05f, 0.09f),
                Prendidas = new[] { 1, 2, 4, 7, 9, 12, 15 }, ColoresPrendidas = new[] { Calido, Cian, Magenta, Calido, Cian, Calido, Magenta }, Semilla = 3
            }
        };

        private static readonly Dictionary<string, Material> materialesFachada = new Dictionary<string, Material>();
        private static Vector3 minimosModulo;
        private static Vector3 maximosModulo;

        // ---------------------------------------------------------------- texturas y materiales

        internal static string RutaTextura(string variante, bool emision)
        {
            return RutaTexturas + "/Fachada_" + variante + (emision ? "_Emision" : "") + ".png";
        }

        /// <summary>Crea (si cambiaron) las texturas y los materiales Fachada_*. Se puede repetir sin cambiar nada.</summary>
        internal static void CrearAssets()
        {
            if (!Directory.Exists(RutaTexturas))
            {
                Directory.CreateDirectory(RutaTexturas);
            }
            materialesFachada.Clear();
            for (int i = 0; i < Variantes.Length; i++)
            {
                Texture2D baseTextura = EscribirTextura(RutaTextura(Variantes[i], false), Dibujar(Datos[i], false));
                Texture2D emision = EscribirTextura(RutaTextura(Variantes[i], true), Dibujar(Datos[i], true));
                materialesFachada[Variantes[i]] = CrearMaterial(Variantes[i], baseTextura, emision);
            }
            AssetDatabase.SaveAssets();
        }

        private static Color[] Dibujar(Variante variante, bool emision)
        {
            Color[] pixeles = new Color[LadoTextura * LadoTextura];
            System.Random azar = new System.Random(variante.Semilla);
            for (int y = 0; y < LadoTextura; y++)
            {
                for (int x = 0; x < LadoTextura; x++)
                {
                    int celda = (y / PixelesCelda) * CeldasPorLado + x / PixelesCelda;
                    int lx = x % PixelesCelda;
                    int ly = y % PixelesCelda;
                    bool enVentana = lx >= MargenVentanaX && lx < PixelesCelda - MargenVentanaX && ly >= MargenVentanaY && ly < PixelesCelda - MargenVentanaY - 5;
                    bool enVidrio = lx >= MargenVentanaX + GrosorMarco && lx < PixelesCelda - MargenVentanaX - GrosorMarco
                        && ly >= MargenVentanaY + GrosorMarco && ly < PixelesCelda - MargenVentanaY - 5 - GrosorMarco;
                    int indicePrendida = Array.IndexOf(variante.Prendidas, celda);

                    if (emision)
                    {
                        bool prendida = indicePrendida >= 0 && lx >= MargenVentanaX + MargenEmision && lx < PixelesCelda - MargenVentanaX - MargenEmision
                            && ly >= MargenVentanaY + MargenEmision && ly < PixelesCelda - MargenVentanaY - 5 - MargenEmision;
                        pixeles[y * LadoTextura + x] = prendida ? variante.ColoresPrendidas[indicePrendida] : Color.black;
                        continue;
                    }

                    float ruido = 0.94f + (float)azar.NextDouble() * 0.12f;
                    Color color;
                    if (enVidrio)
                    {
                        color = Color.Lerp(variante.Vidrio, variante.Vidrio * 2f, ly / (float)PixelesCelda);
                    }
                    else if (enVentana)
                    {
                        color = variante.Marco;
                    }
                    else
                    {
                        color = variante.Muro * ruido;
                        // Juntas finas: bloques en la pobre, paneles en la media y bandas en la alta.
                        bool junta = variante.Semilla == 1 ? ly % 16 == 0 : variante.Semilla == 2 ? lx == 0 : ly >= PixelesCelda - 4;
                        if (junta)
                        {
                            color = variante.Semilla == 3 ? variante.Marco : color * 0.7f;
                        }
                    }
                    color.a = 1f;
                    pixeles[y * LadoTextura + x] = color;
                }
            }
            return pixeles;
        }

        private static Texture2D EscribirTextura(string ruta, Color[] pixeles)
        {
            Texture2D textura = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false);
            textura.SetPixels(pixeles);
            byte[] bytes = textura.EncodeToPNG();
            Object.DestroyImmediate(textura);
            if (!File.Exists(ruta) || !AreIguales(File.ReadAllBytes(ruta), bytes))
            {
                File.WriteAllBytes(ruta, bytes);
            }
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
            if (importador.textureCompression != TextureImporterCompression.Uncompressed || !importador.mipmapEnabled
                || importador.wrapMode != TextureWrapMode.Repeat || importador.maxTextureSize < LadoTextura)
            {
                importador.textureType = TextureImporterType.Default;
                importador.textureCompression = TextureImporterCompression.Uncompressed;
                importador.mipmapEnabled = true;
                importador.wrapMode = TextureWrapMode.Repeat;
                importador.filterMode = FilterMode.Bilinear;
                importador.maxTextureSize = 1024;
                importador.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        private static bool AreIguales(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static Material CrearMaterial(string variante, Texture2D baseTextura, Texture2D emision)
        {
            string ruta = RutaMateriales + "/Fachada_" + variante + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetTexture("_BaseMap", baseTextura);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", emision);
            material.SetColor("_EmissionColor", new Color(EmisionVentanas, EmisionVentanas, EmisionVentanas));
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; // con None el editor borra la palabra clave _EMISSION
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------------------------------------------------------------- vestir el barrio

        private struct Obstaculo
        {
            public Bounds Limites;
            public Transform Dueno;
        }

        private static readonly Vector3[] Normales = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        private static readonly string[] NombresLado = { "Norte", "Sur", "Este", "Oeste" };

        /// <summary>Viste todos los Edificio_N bajo la raíz del barrio. Devuelve cuántos lados vistió.</summary>
        internal static int Vestir(Transform raiz)
        {
            if (materialesFachada.Count != Variantes.Length)
            {
                CrearAssets();
            }
            MedirModulo();
            GameObject modulo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFachadaModular);

            List<Obstaculo> obstaculos = new List<Obstaculo>();
            List<Transform> edificios = new List<Transform>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Cuerpo" || t.name == "FachadaIzquierda" || t.name == "FachadaDerecha" || t.name == "Dintel")
                {
                    obstaculos.Add(new Obstaculo { Limites = t.GetComponent<MeshRenderer>().bounds, Dueno = t.parent });
                }
                if (t.name.StartsWith("Edificio_") && t.parent != null && t.parent.name == "Edificios")
                {
                    edificios.Add(t);
                }
            }

            int lados = 0;
            foreach (Transform edificio in edificios)
            {
                string variante = VarianteDeManzana(edificio.parent.parent.name);
                Transform cuerpo = edificio.Find("Cuerpo");
                Vector3 tamano = cuerpo.localScale;
                for (int i = 0; i < Normales.Length; i++)
                {
                    Vector3 normal = Normales[i];
                    float mitad = (normal.x != 0f ? tamano.x : tamano.z) / 2f;
                    float largo = normal.x != 0f ? tamano.z : tamano.x;
                    Vector3 centroLado = edificio.position + normal * mitad;
                    if (EstaTapado(centroLado + normal * DesplazamientoRevision, edificio, obstaculos))
                    {
                        continue;
                    }
                    VestirLado(edificio, cuerpo, normal, NombresLado[i], mitad, largo, tamano.y, variante, modulo);
                    lados++;
                }
            }
            return lados;
        }

        private static string VarianteDeManzana(string manzana)
        {
            switch (manzana)
            {
                case "Manzana_1":
                case "Manzana_4":
                    return "Pobre";
                case "Manzana_6":
                    return "Media";
                default:
                    return "Alta";
            }
        }

        private static bool EstaTapado(Vector3 punto, Transform edificio, List<Obstaculo> obstaculos)
        {
            foreach (Obstaculo o in obstaculos)
            {
                if (o.Dueno == edificio)
                {
                    continue;
                }
                if (punto.x > o.Limites.min.x && punto.x < o.Limites.max.x && punto.z > o.Limites.min.z && punto.z < o.Limites.max.z)
                {
                    return true;
                }
            }
            return false;
        }

        private static void MedirModulo()
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFachadaModular);
            GameObject copia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            copia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Bounds limites = new Bounds();
            bool primero = true;
            foreach (Renderer r in copia.GetComponentsInChildren<Renderer>())
            {
                if (primero)
                {
                    limites = r.bounds;
                    primero = false;
                }
                else
                {
                    limites.Encapsulate(r.bounds);
                }
            }
            minimosModulo = limites.min;
            maximosModulo = limites.max;
            Object.DestroyImmediate(copia);
        }

        private static void VestirLado(Transform edificio, Transform cuerpo, Vector3 normal, string nombre, float mitad, float largo, float altura, string variante, GameObject modulo)
        {
            Transform lado = CreadorBarrio.Vacio("Lado_" + nombre, edificio, Vector3.zero, Quaternion.identity);
            Quaternion rotacion = Quaternion.LookRotation(normal, Vector3.up);
            Vector3 centroLado = normal * mitad;

            // Planta baja: módulos de 4 m centrados y un cubo del material del muro en cada punta con lo que sobre.
            int cuantos = Mathf.FloorToInt(largo / AnchoModulo);
            float sobra = largo - cuantos * AnchoModulo;
            float centroX = (minimosModulo.x + maximosModulo.x) / 2f;
            float escalaFondo = SalienteMaximo / maximosModulo.z;
            for (int i = 0; i < cuantos; i++)
            {
                float a = -largo / 2f + sobra / 2f + AnchoModulo / 2f + i * AnchoModulo;
                Vector3 local = rotacion * new Vector3(a - centroX, -minimosModulo.y, 0f);
                GameObject pieza = (GameObject)PrefabUtility.InstantiatePrefab(modulo, lado);
                pieza.name = "Fachada_Modular";
                pieza.transform.localPosition = centroLado + local;
                pieza.transform.localRotation = rotacion;
                pieza.transform.localScale = new Vector3(1f, 1f, escalaFondo); // el rótulo y los tubos llegan a 0.7 m: se achica el fondo
            }
            if (sobra >= RellenoMinimo)
            {
                string materialMuro = cuerpo.GetComponent<MeshRenderer>().sharedMaterial.name.Replace("Barrio_", "");
                float profundidad = ProfundidadRelleno;
                for (int punta = -1; punta <= 1; punta += 2)
                {
                    float a = punta * (largo / 2f - sobra / 4f);
                    Vector3 posicion = centroLado + rotacion * new Vector3(a, AlturaPlantaBaja / 2f, profundidad / 2f);
                    Vector3 escala = normal.x != 0f ? new Vector3(profundidad, AlturaPlantaBaja, sobra / 2f) : new Vector3(sobra / 2f, AlturaPlantaBaja, profundidad);
                    CreadorBarrio.Cubo("Relleno_Fachada", lado, posicion, escala, materialMuro, false);
                }
            }

            // Ventanas: un quad de 6 m a la azotea con la repetición ajustada a su tamaño.
            float altoVentanas = altura - AlturaPlantaBaja;
            if (altoVentanas >= AlturaVentanasMinima)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Ventanas";
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(lado, false);
                quad.transform.localPosition = centroLado + normal * SeparacionVentanas + Vector3.up * (AlturaPlantaBaja + altoVentanas / 2f);
                quad.transform.localRotation = Quaternion.LookRotation(-normal, Vector3.up);
                quad.transform.localScale = new Vector3(largo, altoVentanas, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = materialesFachada[variante];
                float desfase = (Mathf.Abs((int)(edificio.position.x * 7f + edificio.position.z * 13f + normal.x * 3f)) % CeldasPorLado) / (float)CeldasPorLado;
                quad.AddComponent<VentanasFachada>().Configurar(new Vector4(largo / MetrosMosaico, altoVentanas / MetrosMosaico, desfase, 0f));
            }

            // Cornisa en la orilla de la azotea.
            Vector3 centroCornisa = centroLado + normal * (SobresaleCornisa - GrosorCornisa / 2f) + Vector3.up * (altura - AltoCornisa / 2f + 0.1f);
            Vector3 medida = normal.x != 0f ? new Vector3(GrosorCornisa, AltoCornisa, largo + 2f * SobresaleCornisa) : new Vector3(largo + 2f * SobresaleCornisa, AltoCornisa, GrosorCornisa);
            CreadorBarrio.Cubo("Cornisa", lado, centroCornisa, medida, "ConcretoOscuro", false);
        }
    }
}

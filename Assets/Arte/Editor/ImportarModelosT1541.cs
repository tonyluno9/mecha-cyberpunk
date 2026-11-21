using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1541: los tres modelos de Tripo de nuevos-21nov, ya copiados con nombre limpio (el .fbx y sus texturas, sin .fbm):
    ///   W11 granada      Assets/Arte/Armas/W11_Granada/        (+ holograma H11 en Assets/Arte/UI/Hologramas/H11_Granada.png)
    ///   EM01 portón      Assets/Arte/Zonas/EM/EM01_PortonMercado_a/
    ///   EM02 puesto      Assets/Arte/Zonas/EM/EM02_PuestoEntrada_a/
    /// Orden en batchmode, cada uno con -executeMethod MechaCyberpunk.Arte.Editor.ImportarModelosT1541.&lt;Método&gt;:
    ///   Diagnostico        solo mira y escribe en el log qué falta (no cambia nada)
    ///   AplicarGranada     material y prefab W11 (10 cm), prefab del piso, ConfigArmasEspeciales.prefabGranada, granada de la mano de JugadorAPie, holograma H11 en HUD y banco
    ///   ImportarEntrada    materiales y prefabs de EM01 (con dos patas de collider, el arco queda libre) y EM02 (con caja)
    ///   ColocarEntrada     abre Ciudad_Prologo y arma la entrada del mercado de armas (portón + puestos), la guarda
    /// Todos son idempotentes. Log con prefijo [T-1541] e informe en Logs/t1541.txt.
    /// </summary>
    public static class ImportarModelosT1541
    {
        // ---- Granada W11
        public const string CarpetaGranada = "Assets/Arte/Armas/W11_Granada/";
        public const string RutaGranadaFbx = CarpetaGranada + "W11_Granada.fbx";
        public const string RutaGranadaMaterial = CarpetaGranada + "W11_Granada.mat";
        public const string RutaGranadaPrefab = CarpetaGranada + "W11_Granada.prefab";
        public const string RutaGranadaPiso = CarpetaGranada + "W11_Granada_Piso.prefab";
        public const string CodigoGranada = "W11";
        /// <summary>Lado más largo de la granada (m): una granada de mano real mide unos 10 cm.</summary>
        public const float LargoGranada = 0.10f;
        private const float EscalaPisoGranada = 1.6f;
        public const string RutaHolograma = "Assets/Arte/UI/Hologramas/H11_Granada.png";

        // ---- Entrada del mercado
        public static readonly EntradaZonaT1382 Porton = new EntradaZonaT1382("EM01_PortonMercado", ArteT250.Eje.Largo, 10f, TipoColliderZona.Caja);
        public static readonly EntradaZonaT1382 Puesto = new EntradaZonaT1382("EM02_PuestoEntrada", ArteT250.Eje.Largo, 3.4f, TipoColliderZona.Caja);
        public const string Variante = "a";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RaizEntrada = "T1541_EntradaMercado";
        /// <summary>Z del portón: dentro del callejón, pasada la boca (T-504 deja libre el carril desde z = 9).</summary>
        public const float ZPorton = 14f;
        /// <summary>Caras de los muros del callejón si los rayos no las encuentran: la oeste es XFrentePorton de T-1320; la este, el borde del tramo de la armería.</summary>
        public const float MuroOesteOmision = -24.15f;
        public const float MuroEsteOmision = -14f;
        /// <summary>Giro extra (grados) del portón sobre el eje del callejón: 0 deja el cartel hacia la avenida (-Z) si el modelo trae el frente a +Z... si en la captura sale al revés, 180.</summary>
        public const float GiroExtraPorton = 0f;
        /// <summary>Los puestos miran al centro del callejón: el frente del modelo (+Z) a -X.</summary>
        public const float YawPuestosEste = -90f;
        public const int PuestosPorLado = 2;
        public const float SeparacionPuestos = 0.6f;
        public const float ZPuestosDesde = 13f;
        public const float ZPuestosHasta = 40f;
        public const float PasoBusqueda = 0.5f;
        /// <summary>Captura de la entrada: el jugador (x, y, z) y su yaw (° desde +Z hacia +X). Mira al norte, hacia el portón.</summary>
        public static readonly Vector3 CapturaDesdeAvenida = new Vector3(-19.5f, 0.2f, 3f);
        public const float CapturaYawAvenida = 0f;
        /// <summary>Segunda captura, desde dentro del callejón mirando al sur, hacia la avenida (la cara interior del portón).</summary>
        public static readonly Vector3 CapturaDesdeDentro = new Vector3(-19.5f, 0.2f, 26f);
        public const float CapturaYawDentro = 180f;

        public const string RutaInforme = "Logs/t1541.txt";
        private static readonly StringBuilder Informe = new StringBuilder();

        // ================================================================== diagnóstico

        [MenuItem("Mecha Cyberpunk/T-1541/Diagnostico")]
        public static void Diagnostico()
        {
            Informe.Clear();
            Linea("fbx W11: " + File.Exists(RutaGranadaFbx) + ", prefab: " + (AssetDatabase.LoadAssetAtPath<GameObject>(RutaGranadaPrefab) != null)
                + ", holograma H11: " + File.Exists(RutaHolograma));
            ConfigArmasEspeciales config = AssetDatabase.LoadAssetAtPath<ConfigArmasEspeciales>(ColocarT1014Armas.RutaConfigEspeciales);
            Linea("ConfigArmasEspeciales.prefabGranada = " + (config != null && config.PrefabGranada != null ? AssetDatabase.GetAssetPath(config.PrefabGranada) : "(vacío)"));
            foreach (EntradaZonaT1382 e in new[] { Porton, Puesto })
            {
                Linea(e.Nombre(Variante) + ": fbx " + File.Exists(ImportarZonasT1382.RutaCarpeta(e, Variante) + "/" + e.Nombre(Variante) + ".fbx")
                    + ", prefab " + (AssetDatabase.LoadAssetAtPath<GameObject>(ImportarZonasT1382.RutaPrefab(e, Variante)) != null));
            }
            Escribir();
        }

        // ================================================================== W11

        [MenuItem("Mecha Cyberpunk/T-1541/Aplicar granada W11 y holograma H11")]
        public static void AplicarGranada()
        {
            Informe.Clear();
            if (!File.Exists(RutaGranadaFbx))
            {
                throw new FileNotFoundException("falta " + RutaGranadaFbx);
            }
            AssetDatabase.Refresh();
            ConfigurarFbx(RutaGranadaFbx);
            Material material = MaterialTripo(RutaGranadaMaterial, CarpetaGranada + "Texturas/", CodigoGranada);
            GameObject prefab = ArmarPrefabGranada(material);
            ArmarPisoGranada(prefab);
            PonerEnConfig(prefab);
            PonerEnLaMano();
            Hologramas();
            AssetDatabase.SaveAssets();
            Escribir();
        }

        private static void ConfigurarFbx(string ruta)
        {
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(ruta);
            if (importador.materialImportMode != ModelImporterMaterialImportMode.None || importador.importAnimation || importador.importCameras || importador.importLights)
            {
                importador.materialImportMode = ModelImporterMaterialImportMode.None;
                importador.importAnimation = false;
                importador.importCameras = false;
                importador.importLights = false;
                importador.SaveAndReimport();
            }
        }

        /// <summary>Material URP Lit con color, normal y máscara metálico (R) / suavidad (A) hechos de metallic y roughness de Tripo. Texturas: carpeta + prefijo + _basecolor, _normal, _metallic, _roughness (.jpg).</summary>
        private static Material MaterialTripo(string rutaMaterial, string carpetaTexturas, string prefijo)
        {
            string color = carpetaTexturas + prefijo + "_basecolor.jpg";
            string normal = carpetaTexturas + prefijo + "_normal.jpg";
            AssetDatabase.ImportAsset(color, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(normal, ImportAssetOptions.ForceUpdate);
            TextureImporter normalImp = (TextureImporter)AssetImporter.GetAtPath(normal);
            if (normalImp.textureType != TextureImporterType.NormalMap)
            {
                normalImp.textureType = TextureImporterType.NormalMap;
                normalImp.SaveAndReimport();
            }
            Texture2D mascara = HacerMascara(carpetaTexturas + prefijo + "_metallic.jpg", carpetaTexturas + prefijo + "_roughness.jpg", carpetaTexturas + prefijo + "_mascara.png");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, rutaMaterial);
            }
            Texture2D mapaColor = AssetDatabase.LoadAssetAtPath<Texture2D>(color);
            material.SetTexture("_BaseMap", mapaColor);
            material.SetTexture("_MainTex", mapaColor);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.EnableKeyword("_NORMALMAP");
            if (mascara != null)
            {
                material.SetTexture("_MetallicGlossMap", mascara);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_Smoothness", 1f);
            }
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            Linea("material " + rutaMaterial + ": color " + (mapaColor != null) + ", máscara " + (mascara != null));
            return material;
        }

        private static Texture2D HacerMascara(string rutaMetal, string rutaRugoso, string rutaSalida)
        {
            if (!File.Exists(rutaSalida))
            {
                if (!File.Exists(rutaMetal) || !File.Exists(rutaRugoso))
                {
                    return null;
                }
                Texture2D metal = Leer(rutaMetal);
                Texture2D rugoso = Leer(rutaRugoso);
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
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(rutaSalida);
            if (ti != null && (ti.sRGBTexture || ti.alphaSource != TextureImporterAlphaSource.FromInput))
            {
                ti.sRGBTexture = false;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(rutaSalida);
        }

        private static Texture2D Leer(string ruta)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            t.LoadImage(File.ReadAllBytes(ruta));
            return t;
        }

        // Como W09: raíz > Modelo > Malla (la granada centrada en el origen, de 10 cm) y Boca; sin colliders (la granada lanzada pone el suyo).
        private static GameObject ArmarPrefabGranada(Material material)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaGranadaFbx);
            GameObject raiz = new GameObject("W11_Granada");
            GameObject modelo = new GameObject("Modelo");
            modelo.transform.SetParent(raiz.transform, false);
            GameObject malla = (GameObject)PrefabUtility.InstantiatePrefab(fbx, modelo.transform);
            PrefabUtility.UnpackPrefabInstance(malla, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            malla.name = "Malla";
            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            }
            foreach (Collider c in raiz.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
            Bounds original = ArteT250.Limites(raiz);
            float mayor = Mathf.Max(original.size.x, Mathf.Max(original.size.y, original.size.z));
            float escala = mayor > 0.0001f ? LargoGranada / mayor : 1f;
            modelo.transform.localScale = Vector3.one * escala;
            modelo.transform.localPosition = Vector3.zero;
            Bounds escalado = ArteT250.Limites(raiz);
            modelo.transform.localPosition = -escalado.center;
            GameObject boca = new GameObject("Boca");
            boca.transform.SetParent(raiz.transform, false);
            boca.transform.localPosition = ColocarT1014Armas.BocaGranada;
            Bounds final = ArteT250.Limites(raiz);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaGranadaPrefab);
            Object.DestroyImmediate(raiz);
            Linea("prefab " + RutaGranadaPrefab + ": original " + original.size.ToString("F3") + " m, escala " + escala.ToString("F4") + ", final " + final.size.ToString("F3") + " m");
            return prefab;
        }

        // La granada tirada de lado, con ArmaEnPiso (como el piso de las otras armas).
        private static void ArmarPisoGranada(GameObject prefab)
        {
            GameObject raiz = new GameObject("W11_Granada_Piso");
            GameObject pivote = new GameObject("Modelo");
            pivote.transform.SetParent(raiz.transform, false);
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pivote.transform);
            modelo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            modelo.transform.localScale = Vector3.one * EscalaPisoGranada;
            Bounds b = ArteT250.Limites(raiz);
            modelo.transform.localPosition -= b.center - raiz.transform.position;
            ArmaEnPiso piso = raiz.AddComponent<ArmaEnPiso>();
            SerializedObject so = new SerializedObject(piso);
            so.FindProperty("arma").enumValueIndex = (int)ArmaAPie.Granada;
            so.FindProperty("nombre").stringValue = "la granada";
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaGranadaPiso);
            Object.DestroyImmediate(raiz);
            Linea("prefab del piso " + RutaGranadaPiso);
        }

        // La granada que se lanza (ProyectilAPie la instancia con ConfigArmasEspeciales.PrefabGranada).
        private static void PonerEnConfig(GameObject prefab)
        {
            ConfigArmasEspeciales config = AssetDatabase.LoadAssetAtPath<ConfigArmasEspeciales>(ColocarT1014Armas.RutaConfigEspeciales);
            if (config == null)
            {
                Linea("ERROR: falta " + ColocarT1014Armas.RutaConfigEspeciales);
                return;
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("prefabGranada").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            Linea("ConfigArmasEspeciales.prefabGranada = W11_Granada");
        }

        // En el objeto "Granada" del socket de la mano: se quita el W09 y se pone el W11 con la misma boca y posición.
        private static void PonerEnLaMano()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(ColocarT1014Armas.RutaJugador);
            try
            {
                ApuntadoIK ik = contenido.GetComponentsInChildren<ApuntadoIK>(true).FirstOrDefault(a => a.SocketArma != null);
                Transform objeto = ik != null ? ik.SocketArma.Find("Granada") : null;
                if (objeto == null)
                {
                    Linea("ERROR: JugadorAPie no tiene el objeto Granada en el socket de la mano (corre ColocarT1014Armas antes)");
                    return;
                }
                Transform w09 = objeto.Find(ColocarT1014Armas.CodigoGranada);
                if (w09 != null)
                {
                    Object.DestroyImmediate(w09.gameObject);
                }
                Transform actual = objeto.Find(CodigoGranada);
                GameObject origen = actual != null ? PrefabUtility.GetCorrespondingObjectFromSource(actual.gameObject) : null;
                if (origen == null || AssetDatabase.GetAssetPath(origen) != RutaGranadaPrefab)
                {
                    if (actual != null)
                    {
                        Object.DestroyImmediate(actual.gameObject);
                    }
                    GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaGranadaPrefab), objeto);
                    modelo.name = CodigoGranada;
                    modelo.transform.localPosition = ColocarT1014Armas.PosicionGranadaEnMano;
                    modelo.transform.localRotation = Quaternion.identity;
                    modelo.transform.localScale = Vector3.one;
                    foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
                    {
                        r.shadowCastingMode = ShadowCastingMode.Off;
                    }
                }
                objeto.Find(CodigoGranada).localPosition = ColocarT1014Armas.PosicionGranadaEnMano;
                objeto.Find("Boca").localPosition = ColocarT1014Armas.BocaGranada;
                PrefabUtility.SaveAsPrefabAsset(contenido, ColocarT1014Armas.RutaJugador);
                Linea("JugadorAPie: la granada de la mano es el W11");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        // H11 al HUD (PiezasUI) y al banco de Tino / rueda (ConfigMenuArmas): ambos leen H11_Granada.png y el prefab W11.
        private static void Hologramas()
        {
            AssetDatabase.Refresh();
            ImportarS2nHologramas.Configurar(RutaHolograma);
            ImportarS2nHologramas.Importar();
            CreadorMenuArmas.Crear();
            Linea("holograma H11 en el HUD y en el menú de armas");
        }

        // ================================================================== EM01 y EM02

        [MenuItem("Mecha Cyberpunk/T-1541/Importar portón EM01 y puesto EM02")]
        public static void ImportarEntrada()
        {
            Informe.Clear();
            ImportarZonasT1382.Informe.Clear();
            AssetDatabase.Refresh();
            foreach (EntradaZonaT1382 e in new[] { Porton, Puesto })
            {
                string carpeta = ImportarZonasT1382.RutaCarpeta(e, Variante) + "/";
                string nombre = e.Nombre(Variante);
                if (!File.Exists(carpeta + nombre + ".fbx"))
                {
                    Linea(nombre + ": FALTA FBX en " + carpeta);
                    continue;
                }
                HacerMascara(carpeta + nombre + "_metallic.jpg", carpeta + nombre + "_roughness.jpg", carpeta + nombre + "_metalsuave.png");
                AssetDatabase.ImportAsset(carpeta + nombre + "_metalsuave.png", ImportAssetOptions.ForceUpdate);
                ImportarZonasT1382.ConfigurarImportadores(e, Variante);
                Material material = ImportarZonasT1382.CrearMaterial(e, Variante);
                Linea(ImportarZonasT1382.CrearPrefab(e, Variante, material));
            }
            PatasDelPorton();
            AssetDatabase.SaveAssets();
            Escribir();
        }

        // El BoxCollider de caja entera taparía el arco: se cambia por dos patas (ver CalculoEntradaMercadoT1541).
        private static void PatasDelPorton()
        {
            string ruta = ImportarZonasT1382.RutaPrefab(Porton, Variante);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
            {
                return;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                foreach (BoxCollider c in contenido.GetComponents<BoxCollider>())
                {
                    Object.DestroyImmediate(c);
                }
                foreach (Transform hijo in contenido.transform.Cast<Transform>().Where(t => t.name.StartsWith("Pata")).ToList())
                {
                    Object.DestroyImmediate(hijo.gameObject);
                }
                Bounds b = ArteT250.Limites(contenido);
                bool arcoEnX = b.size.x >= b.size.z;
                float largo = arcoEnX ? b.size.x : b.size.z;
                float fondo = arcoEnX ? b.size.z : b.size.x;
                var patas = CalculoEntradaMercadoT1541.Patas(largo, fondo);
                string[] nombres = { "PataA", "PataB" };
                Vector2[] centros = { patas.centroA, patas.centroB };
                for (int i = 0; i < 2; i++)
                {
                    GameObject pata = new GameObject(nombres[i]);
                    pata.transform.SetParent(contenido.transform, false);
                    BoxCollider caja = pata.AddComponent<BoxCollider>();
                    caja.center = arcoEnX ? new Vector3(b.center.x + centros[i].x, b.center.y, b.center.z) : new Vector3(b.center.x, b.center.y, b.center.z + centros[i].x);
                    caja.size = arcoEnX ? new Vector3(patas.tamano.x, b.size.y, patas.tamano.y) : new Vector3(patas.tamano.y, b.size.y, patas.tamano.x);
                }
                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                Linea("EM01: dos patas de collider, arco en " + (arcoEnX ? "X" : "Z") + ", largo " + largo.ToString("F2") + " m, abertura libre " + CalculoEntradaMercadoT1541.AberturaLibre(largo).ToString("F2") + " m");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        // ================================================================== colocar en Ciudad_Prologo

        [MenuItem("Mecha Cyberpunk/T-1541/Colocar entrada del mercado en Ciudad_Prologo")]
        public static void ColocarEntrada()
        {
            Informe.Clear();
            GameObject prefabPorton = AssetDatabase.LoadAssetAtPath<GameObject>(ImportarZonasT1382.RutaPrefab(Porton, Variante));
            GameObject prefabPuesto = AssetDatabase.LoadAssetAtPath<GameObject>(ImportarZonasT1382.RutaPrefab(Puesto, Variante));
            if (prefabPorton == null || prefabPuesto == null)
            {
                throw new FileNotFoundException("faltan los prefabs EM01/EM02: corre ImportarEntrada antes");
            }
            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            foreach (GameObject viejo in escena.GetRootGameObjects().Where(g => g.name == RaizEntrada).ToList())
            {
                Object.DestroyImmediate(viejo);
            }
            Transform raiz = new GameObject(RaizEntrada).transform;
            Physics.SyncTransforms();

            // Ancho del callejón en la z del portón: mediana de lo que miden rayos a cuatro alturas hacia cada muro.
            float xCentro = (MuroOesteOmision + MuroEsteOmision) * 0.5f;
            float[] alturas = { 1.2f, 3f, 5f, 7f };
            float oeste = xCentro - CalculoEntradaMercadoT1541.Mediana(alturas.Select(y => Medir(new Vector3(xCentro, y, ZPorton), Vector3.left)), xCentro - MuroOesteOmision);
            float este = xCentro + CalculoEntradaMercadoT1541.Mediana(alturas.Select(y => Medir(new Vector3(xCentro, y, ZPorton), Vector3.right)), MuroEsteOmision - xCentro);
            float ancho = este - oeste;
            float centroX = (oeste + este) * 0.5f;
            float suelo = AlturaSuelo(centroX, ZPorton);
            Linea("callejón en z " + ZPorton + ": x " + oeste.ToString("F2") + " a " + este.ToString("F2") + " (ancho " + ancho.ToString("F2") + " m), suelo y " + suelo.ToString("F2"));

            // Portón: el arco a lo ancho del callejón (su eje largo a X), con la escala para cubrirlo de muro a muro.
            Bounds bPorton = MedirPrefab(prefabPorton);
            bool arcoEnX = bPorton.size.x >= bPorton.size.z;
            float largoArco = arcoEnX ? bPorton.size.x : bPorton.size.z;
            float escala = CalculoEntradaMercadoT1541.EscalaPorton(ancho, largoArco);
            GameObject porton = (GameObject)PrefabUtility.InstantiatePrefab(prefabPorton, raiz);
            porton.name = "EM01_PortonMercado";
            porton.transform.SetPositionAndRotation(new Vector3(centroX, suelo, ZPorton), Quaternion.Euler(0f, (arcoEnX ? 0f : 90f) + GiroExtraPorton, 0f));
            porton.transform.localScale = Vector3.one * escala;
            Linea("portón: x " + centroX.ToString("F2") + ", z " + ZPorton + ", escala " + escala.ToString("F3") + ", abertura libre "
                + (CalculoEntradaMercadoT1541.AberturaLibre(largoArco) * escala).ToString("F2") + " m");
            Physics.SyncTransforms();

            // Puestos a la orilla este del callejón, mirando al centro, en el primer hueco libre desde el portón (el oeste lo ocupa el carril de la armería).
            Bounds bPuesto = MedirPrefab(prefabPuesto);
            bool puestoEnX = bPuesto.size.x >= bPuesto.size.z;
            float largoPuesto = puestoEnX ? bPuesto.size.x : bPuesto.size.z;
            float fondoPuesto = puestoEnX ? bPuesto.size.z : bPuesto.size.x;
            // Con el giro de 90 el frente queda a lo largo de z: el largo va en z y el fondo en x.
            Vector2 mitad = new Vector2(fondoPuesto * 0.5f, largoPuesto * 0.5f);
            float yawPuesto = YawPuestosEste + (puestoEnX ? 0f : 90f);
            float z = ZPuestosDesde;
            int puestos = 0;
            while (puestos < PuestosPorLado && z <= ZPuestosHasta)
            {
                float x = este - mitad.x;
                float y = AlturaSuelo(x, z);
                if (EspacioLibre(new Vector3(x, y, z), mitad, bPuesto.size.y, raiz))
                {
                    GameObject puesto = (GameObject)PrefabUtility.InstantiatePrefab(prefabPuesto, raiz);
                    puesto.name = "EM02_PuestoEntrada_" + (puestos + 1);
                    puesto.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, yawPuesto, 0f));
                    Physics.SyncTransforms();
                    Linea(puesto.name + ": x " + x.ToString("F2") + ", z " + z.ToString("F2") + ", y " + y.ToString("F2"));
                    puestos++;
                    z += mitad.y * 2f + SeparacionPuestos;
                }
                else
                {
                    z += PasoBusqueda;
                }
            }
            if (puestos < PuestosPorLado)
            {
                Linea("AVISO: solo se encontró espacio libre para " + puestos + " puesto(s) EM02 entre z " + ZPuestosDesde + " y " + ZPuestosHasta);
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Linea("captura 1: jugador " + CapturaDesdeAvenida + " yaw " + CapturaYawAvenida + "; captura 2: jugador " + CapturaDesdeDentro + " yaw " + CapturaYawDentro);
            Escribir();
        }

        // Límites de un prefab a escala 1 en el origen (se instancia un momento: los bounds de un asset no son fiables).
        private static Bounds MedirPrefab(GameObject prefab)
        {
            GameObject temporal = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            temporal.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Bounds b = ArteT250.Limites(temporal);
            Object.DestroyImmediate(temporal);
            return b;
        }

        // Distancia hasta el primer collider sólido en esa dirección (0 si no hay): los muros del callejón.
        private static float Medir(Vector3 origen, Vector3 direccion)
        {
            return Physics.Raycast(origen, direccion, out RaycastHit golpe, 15f, ~0, QueryTriggerInteraction.Ignore) ? golpe.distance : 0f;
        }

        private static float AlturaSuelo(float x, float z)
        {
            return Physics.Raycast(new Vector3(x, 30f, z), Vector3.down, out RaycastHit golpe, 60f, ~0, QueryTriggerInteraction.Ignore) ? golpe.point.y : 0f;
        }

        // Nada con collider (salvo el suelo y lo nuestro) dentro de la huella del puesto ni en el carril de la armería.
        private static bool EspacioLibre(Vector3 pie, Vector2 mitad, float alto, Transform raiz)
        {
            Vector2 centro = new Vector2(pie.x, pie.z);
            Bounds paso = ColocarT1320Calles.PasoArmeria;
            if (CalculoEntradaMercadoT1541.Traslapan(centro, mitad, new Vector2(paso.center.x, paso.center.z), new Vector2(paso.extents.x, paso.extents.z)))
            {
                return false;
            }
            Collider[] cerca = Physics.OverlapBox(pie + Vector3.up * (alto * 0.5f + 0.3f), new Vector3(mitad.x, alto * 0.5f, mitad.y), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            return !cerca.Any(c => !c.transform.IsChildOf(raiz) && c.bounds.max.y > pie.y + 0.4f);
        }

        private static void Linea(string texto) => Informe.AppendLine(texto);

        private static void Escribir()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, Informe.ToString());
            Debug.Log("[T-1541] " + Informe);
        }
    }
}

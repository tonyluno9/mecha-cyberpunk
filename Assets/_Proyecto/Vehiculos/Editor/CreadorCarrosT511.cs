using System.IO;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-511: un prefab manejable por cada modelo de carro (V01a, V01b, V02, V03, V04, V05, V06, V08): copia del CarroViejo con el modelo real de
    /// Assets/Arte/Vehiculos/Prefabs como visual, las cajas de choque a la medida del modelo, las ruedas de greybox invisibles (siguen girando para la
    /// física), asiento, punto de subida y cámara a la altura del modelo, y CarroEstacionado (alarma y calor al robarlo).
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CreadorCarrosT511.Crear
    /// </summary>
    public static class CreadorCarrosT511
    {
        public const string CarpetaPrefabs = "Assets/_Proyecto/Vehiculos/Prefabs/";
        public const string RutaBase = CarpetaPrefabs + "CarroViejo.prefab";
        public const string RutaConfigCamara = "Assets/_Proyecto/Vehiculos/Datos/ConfigCamaraCarro_Viejo.asset";
        public const string RutaCaptura = "docs/capturas/T-511-modelos.png";

        /// <summary>Código del modelo, prefab del arte, giro extra del visual (°) y largo real (m; 0 deja el del prefab del arte).</summary>
        public static readonly (string codigo, string arte, float giro, float largo)[] Modelos =
        {
            ("V01a", "V01a_CarroViejito", 90f, 4.2f),
            ("V01b", "V01b_CarroPro", 90f, 4.5f),
            ("V02", "V02", 0f, 0f),
            ("V03", "V03", 0f, 0f),
            ("V04", "V04", 0f, 0f),
            ("V05", "V05", 0f, 0f),
            ("V06", "V06", 0f, 0f),
            ("V08", "V08", 0f, 0f),
        };

        private const float AlturaRueda = 0.35f;
        private const float FondoBajoCajas = 0.4f;
        private const float FraccionAltoCarroceria = 0.5f;
        private const float FraccionAltoCabina = 0.92f;
        private const float FraccionAnchoCabina = 0.8f;
        private const float FraccionLargoCabina = 0.5f;
        private const float FraccionVia = 0.38f;
        private const float FraccionEje = 0.32f;
        private const float ViaMinima = 0.3f;
        private const float FraccionOjos = 0.78f;
        private const float FraccionFoco = 0.62f;
        private const float DistanciaPorMetroDeLargo = 6f / 4.2f;
        private const float DistanciaMinimaCamara = 4.5f;
        private const float DistanciaMaximaCamara = 8f;
        private const float SeparacionPuerta = 1f;

        public static string RutaPrefab(string codigo) => CarpetaPrefabs + "Carro_" + codigo + ".prefab";

        [MenuItem("Mecha Cyberpunk/T-511/Crear carros manejables")]
        public static void Crear()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (var m in Modelos)
            {
                informe.AppendLine(CrearUno(m.codigo, m.arte, m.giro, m.largo));
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/t511-carros.txt", informe.ToString());
            Capturar();
            Debug.Log("[Vehiculos] T-511:\n" + informe);
        }

        private static string CrearUno(string codigo, string arte, float giro, float largo)
        {
            GameObject baseCarro = AssetDatabase.LoadAssetAtPath<GameObject>(RutaBase);
            GameObject arteAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Vehiculos/Prefabs/" + arte + ".prefab");
            ConfigCamaraCarro configCamara = AssetDatabase.LoadAssetAtPath<ConfigCamaraCarro>(RutaConfigCamara);
            if (baseCarro == null || arteAsset == null || configCamara == null)
            {
                return codigo + "\tFALTA base, modelo o config de cámara";
            }

            GameObject raiz = (GameObject)PrefabUtility.InstantiatePrefab(baseCarro);
            PrefabUtility.UnpackPrefabInstance(raiz, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            raiz.name = "Carro_" + codigo;

            Transform viejo = raiz.transform.Find("Visual_T250");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            foreach (string nombre in new[] { "Carroceria", "Cabina" })
            {
                raiz.transform.Find(nombre).GetComponent<Renderer>().enabled = false;
            }
            foreach (string nombre in new[] { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" })
            {
                // Las ruedas de greybox siguen ahí y giran (la física las usa) pero no se ven.
                foreach (Renderer r in raiz.transform.Find(nombre).GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = false;
                }
            }

            // El visual del modelo: se saca del prefab del arte (sin su collider) y se pone en un envoltorio girado y centrado.
            GameObject copiaArte = (GameObject)PrefabUtility.InstantiatePrefab(arteAsset);
            PrefabUtility.UnpackPrefabInstance(copiaArte, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            GameObject envoltorio = new GameObject("Visual_T511");
            envoltorio.transform.SetParent(raiz.transform, false);
            envoltorio.transform.SetSiblingIndex(0);
            Transform visual = copiaArte.transform.Find("Visual");
            visual.SetParent(envoltorio.transform, false);
            Object.DestroyImmediate(copiaArte);
            envoltorio.transform.localRotation = Quaternion.Euler(0f, giro, 0f);
            Bounds b = Limites(raiz);
            if (largo > 0f)
            {
                envoltorio.transform.localScale = Vector3.one * (largo / b.size.z);
                b = Limites(raiz);
            }
            envoltorio.transform.localPosition += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            b = Limites(raiz);
            float L = b.size.z;
            float W = b.size.x;
            float H = b.size.y;

            // Cajas de choque a la medida (los cubos de greybox llevan los colliders).
            float cima = H * FraccionAltoCarroceria;
            Caja(raiz.transform.Find("Carroceria"), FondoBajoCajas, cima, W * 0.96f, L * 0.98f, 0f);
            Caja(raiz.transform.Find("Cabina"), cima, H * FraccionAltoCabina, W * FraccionAnchoCabina, L * FraccionLargoCabina, -L * 0.05f);

            // Ruedas (anclajes de la suspensión).
            float via = Mathf.Max(ViaMinima, W * FraccionVia);
            float eje = L * FraccionEje;
            Rueda(raiz, "Rueda_DI", -via, eje);
            Rueda(raiz, "Rueda_DD", via, eje);
            Rueda(raiz, "Rueda_TI", -via, -eje);
            Rueda(raiz, "Rueda_TD", via, -eje);

            // Asiento, punto de subida y cámara a la altura del modelo.
            bool moto = codigo == "V08";
            float asientoX = moto ? 0f : -W * 0.22f;
            float asientoZ = -L * 0.03f;
            Transform asiento = raiz.transform.Find("Asiento");
            asiento.localPosition = new Vector3(asientoX, Mathf.Max(0.3f, H * FraccionOjos - configCamara.AlturaOjos), asientoZ);
            raiz.transform.Find("PuntoSubida").localPosition = new Vector3(-(W * 0.5f + SeparacionPuerta), 0f, asientoZ);
            Transform camaraObjeto = raiz.transform.Find("CamaraCarro");
            camaraObjeto.localPosition = asiento.localPosition + Vector3.up * configCamara.AlturaOjos;
            SerializedObject soCamara = new SerializedObject(camaraObjeto.GetComponent<CamaraCarro>());
            soCamara.FindProperty("alturaFocoPropia").floatValue = H * FraccionFoco;
            soCamara.FindProperty("distanciaAtrasPropia").floatValue = Mathf.Clamp(L * DistanciaPorMetroDeLargo, DistanciaMinimaCamara, DistanciaMaximaCamara);
            soCamara.ApplyModifiedPropertiesWithoutUndo();

            if (raiz.GetComponent<CarroEstacionado>() == null)
            {
                raiz.AddComponent<CarroEstacionado>();
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(codigo));
            Object.DestroyImmediate(raiz);
            return string.Format("{0}\tlargo {1:F2}\tancho {2:F2}\talto {3:F2}", codigo, L, W, H);
        }

        private static void Caja(Transform t, float desde, float hasta, float ancho, float largo, float z)
        {
            t.localPosition = new Vector3(0f, (desde + hasta) * 0.5f, z);
            t.localScale = new Vector3(ancho, hasta - desde, largo);
        }

        private static void Rueda(GameObject raiz, string nombre, float x, float z)
        {
            raiz.transform.Find(nombre).localPosition = new Vector3(x, AlturaRueda, z);
        }

        private static Bounds Limites(GameObject raiz)
        {
            return LimitesExactos(raiz.transform.Find("Visual_T511").gameObject);
        }

        // Medida exacta del modelo: se recorren los vértices (los bounds de un renderer girado salen inflados).
        public static Bounds LimitesExactos(GameObject objeto)
        {
            Bounds b = default;
            bool primero = true;
            foreach (MeshFilter f in objeto.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null)
                {
                    continue;
                }
                Matrix4x4 m = f.transform.localToWorldMatrix;
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    Vector3 w = m.MultiplyPoint3x4(v);
                    if (primero)
                    {
                        b = new Bounds(w, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        b.Encapsulate(w);
                    }
                }
            }
            return b;
        }


        // Hoja de los 8 carros (4 x 2) vistos de 3/4 por delante, para la revisión.
        private static void Capturar()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
            Light sol = new GameObject("Sol").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.transform.localScale = new Vector3(5f, 1f, 5f);
            Camera camara = new GameObject("Camara").AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            camara.fieldOfView = 30f;
            Texture2D hoja = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            for (int i = 0; i < Modelos.Length; i++)
            {
                GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(Modelos[i].codigo)));
                Vector3 centro = new Vector3(0f, 0.9f, 0f);
                camara.transform.position = centro + new Vector3(0.7f, 0.35f, 1f).normalized * 13f;
                camara.transform.LookAt(centro);
                RenderTexture rt = new RenderTexture(480, 540, 24, RenderTextureFormat.ARGB32);
                camara.targetTexture = rt;
                camara.Render();
                RenderTexture.active = rt;
                Texture2D t = new Texture2D(480, 540, TextureFormat.RGB24, false);
                t.ReadPixels(new Rect(0, 0, 480, 540), 0, 0);
                t.Apply();
                hoja.SetPixels((i % 4) * 480, (1 - i / 4) * 540, 480, 540, t.GetPixels());
                camara.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(t);
                rt.Release();
                Object.DestroyImmediate(carro);
            }
            hoja.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(RutaCaptura));
            File.WriteAllBytes(RutaCaptura, hoja.EncodeToPNG());
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-510: pone en el juego los personajes nuevos de Tripo (SkullHombre, Doctor, Inventor, Hacker, RobotHombre; GuardiaNova lo pone ColocarT510) y arregla
    /// "man": su textura de color era el mapa metálico (salía negro). Crea además las variantes de color de man (la textura con el magenta cambiado de
    /// tono) para SNAKES, NOVA, el vendedor y el reclutable. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.ConectarVisualesT510.Todo
    /// </summary>
    public static class ConectarVisualesT510
    {
        public const string CarpetaTexturas = "Assets/Arte/Personajes/man/Texturas";
        public const string RutaJefeSkulls = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_JefeSkulls.prefab";
        public const string RutaVisualPeaton = "Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab";
        public const float ProbabilidadRobot = 0.25f;

        /// <summary>Nombre de la variante y tono (0 a 1) al que se lleva el magenta de la textura de man.</summary>
        public static readonly (string nombre, float tono)[] Tonos =
        {
            ("verde", 0.33f), ("azul", 0.56f), ("ambar", 0.09f), ("rojo", 0.0f),
        };

        [MenuItem("Mecha Cyberpunk/Personajes/T-510 Todo")]
        public static void Todo()
        {
            CreadorVisualTripo.CrearTodos();
            CrearTexturasMan();
            ConectarPeaton();
            ConectarVisualesT520.ConectarEscena();
            ConectarEscena();
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-510: personajes nuevos conectados.");
        }

        public static Texture2D TexturaMan(string nombre) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaTexturas + "/man_" + nombre + ".png");

        // ---------- Texturas de man ----------

        /// <summary>Copias de la textura de man con el magenta llevado a otro tono; el piel, el negro y el gris no cambian.</summary>
        public static void CrearTexturasMan()
        {
            Texture2D origen = CreadorVisualTripo.TexturaDeColor("man");
            if (origen == null)
            {
                Debug.LogError("[Personaje] T-510: man no tiene textura de color.");
                return;
            }
            Texture2D copia = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            copia.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(origen)));
            Color32[] base32 = copia.GetPixels32();
            Directory.CreateDirectory(CarpetaTexturas);
            foreach ((string nombre, float tono) in Tonos)
            {
                var pixeles = new Color32[base32.Length];
                for (int i = 0; i < base32.Length; i++)
                {
                    Color32 c = base32[i];
                    Color.RGBToHSV(c, out float h, out float s, out float v);
                    // Magenta y rosa (de 250 a 350 grados) con algo de color: se cambian de tono.
                    if (s > 0.28f && h > 0.69f && h < 0.97f)
                    {
                        Color n = Color.HSVToRGB(tono, s, v);
                        pixeles[i] = new Color32((byte)(n.r * 255f), (byte)(n.g * 255f), (byte)(n.b * 255f), 255);
                    }
                    else
                    {
                        pixeles[i] = c;
                    }
                }
                Texture2D salida = new Texture2D(copia.width, copia.height, TextureFormat.RGB24, false);
                salida.SetPixels32(pixeles);
                string ruta = CarpetaTexturas + "/man_" + nombre + ".png";
                File.WriteAllBytes(ruta, salida.EncodeToPNG());
                Object.DestroyImmediate(salida);
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            }
            Object.DestroyImmediate(copia);
        }

        // ---------- Peatón ----------

        /// <summary>El prefab Peaton lleva también un RobotHombre desactivado y PeatonVariado, que elige uno de los dos al aparecer (1 de cada 4 robot).</summary>
        public static void ConectarPeaton()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaVisualPeaton);
            try
            {
                Transform persona = contenido.transform.Find("Visual");
                Transform robot = contenido.transform.Find("VisualRobot");
                if (robot == null)
                {
                    GameObject r = (GameObject)PrefabUtility.InstantiatePrefab(ConectarVisualesT520.Visual("RobotHombre"), contenido.transform);
                    r.name = "VisualRobot";
                    robot = r.transform;
                }
                PeatonVariado variado = contenido.GetComponent<PeatonVariado>();
                if (variado == null)
                {
                    variado = contenido.AddComponent<PeatonVariado>();
                }
                variado.Configurar(persona.gameObject, robot.gameObject, ProbabilidadRobot);
                robot.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaVisualPeaton);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        // ---------- Escena ----------

        public static void ConectarEscena()
        {
            var escena = EditorSceneManager.OpenScene(ConectarVisualesT520.RutaEscena, OpenSceneMode.Single);
            CrearPrefabJefeSkulls();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || t.gameObject.scene != escena)
                {
                    continue;
                }
                if ((t.name == "Jefe" || t.name == "PandilleroArmado_JefeSkulls") && t.GetComponent("PandilleroAPie") != null && EsJefeSkulls(t))
                {
                    PonerJefeSkulls(t);
                }
                else if (t.name == "Vendedor")
                {
                    CambiarVisual(t, "man", ConectarVisualesT520.Variante("man", "Vendedor", new Color(1f, 0.9f, 0.8f), default, TexturaMan("ambar")));
                }
                else if (t.name == "Reclutable_EL TOPO")
                {
                    VarianteEn(t.Find("Modelo"), ConectarVisualesT520.Variante("man", "Reclutable", Color.white, default, TexturaMan("rojo")));
                }
                else if (t.GetComponent("PersonajeApoyo") != null)
                {
                    CambiarApoyo(t);
                }
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        private static bool EsJefeSkulls(Transform t)
        {
            Object datos = new SerializedObject(t.GetComponent("PandilleroAPie")).FindProperty("datos").objectReferenceValue;
            return datos != null && datos.name.Contains("Calaveras");
        }

        private static void VarianteEn(Transform visual, Material material)
        {
            if (visual == null)
            {
                return;
            }
            VarianteVisual v = visual.GetComponent<VarianteVisual>();
            if (v == null)
            {
                v = visual.gameObject.AddComponent<VarianteVisual>();
            }
            v.Configurar(new[] { material }, 0, null);
            EditorUtility.SetDirty(v);
        }

        /// <summary>Cambia el visual "Visual" de un personaje suelto (sin prefab) por el de <paramref name="cuerpo"/> con el material dado.</summary>
        private static void CambiarVisual(Transform raiz, string cuerpo, Material material)
        {
            Transform viejo = raiz.Find("Visual");
            if (viejo != null)
            {
                VisualPersonaje vp = viejo.GetComponent<VisualPersonaje>();
                Animator a = vp != null ? vp.GetComponentInChildren<Animator>(true) : null;
                if (a != null && a.runtimeAnimatorController != null && AssetDatabase.GetAssetPath(a.runtimeAnimatorController).Contains("/" + cuerpo + "/") && viejo.GetComponent<VarianteVisual>() != null)
                {
                    VarianteEn(viejo, material);
                    return;
                }
                Object.DestroyImmediate(viejo.gameObject);
            }
            ConectarVisualesT520.ColocarSobreElPiso(raiz, cuerpo, material, new string[0]);
        }

        private static void CambiarApoyo(Transform raiz)
        {
            Object datos = new SerializedObject(raiz.GetComponent("PersonajeApoyo")).FindProperty("datos").objectReferenceValue;
            string id = datos != null ? new SerializedObject(datos).FindProperty("id").stringValue : string.Empty;
            foreach ((string idApoyo, string cuerpo, Color color) in ConectarVisualesT520.ApoyosDeLaDemo)
            {
                if (idApoyo != id || (cuerpo != "Doctor" && cuerpo != "Inventor" && cuerpo != "Hacker"))
                {
                    continue;
                }
                Transform viejo = raiz.Find("Visual");
                Animator a = viejo != null ? viejo.GetComponentInChildren<Animator>(true) : null;
                if (a != null && AssetDatabase.GetAssetPath(a.runtimeAnimatorController).Contains("/" + cuerpo + "/"))
                {
                    return; // ya es el visual nuevo
                }
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                ConectarVisualesT520.ColocarSobreElPiso(raiz, cuerpo, ConectarVisualesT520.Variante(cuerpo, "Apoyo_" + id, Color.white), new string[0]);
            }
        }

        // ---------- Jefe de los SKULLS ----------

        /// <summary>PandilleroArmado con SkullHombre en lugar de man (la escena lo pone sobre el jefe de los SKULLS).</summary>
        public static void CrearPrefabJefeSkulls()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(ConectarVisualesT520.RutaPandillero);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                ConectarVisualesT520.Reemplazar(contenido, viejo, ConectarVisualesT520.Visual("SkullHombre"), "Modelo", false);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaJefeSkulls);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void PonerJefeSkulls(Transform jefe)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJefeSkulls);
            Transform modelo = jefe.Find("Modelo");
            bool yaEs = modelo != null && modelo.GetComponentInChildren<Animator>(true) != null
                && AssetDatabase.GetAssetPath(modelo.GetComponentInChildren<Animator>(true).runtimeAnimatorController).Contains("/SkullHombre/");
            if (!yaEs)
            {
                PrefabUtility.ReplacePrefabAssetOfPrefabInstance(jefe.gameObject, prefab, InteractionMode.AutomatedAction);
                modelo = jefe.Find("Modelo");
            }
            jefe.name = "Jefe"; // el cambio de prefab le pone el nombre del prefab
            jefe.localScale = Vector3.one; // SkullHombre ya mide 1.95 m
            Material cuerpo = ConectarVisualesT520.Variante("SkullHombre", "JefeSkulls", Color.white, new Color(0.03f, 0f, 0.02f));
            VarianteVisual v = modelo.GetComponent<VarianteVisual>();
            if (v == null)
            {
                v = modelo.gameObject.AddComponent<VarianteVisual>();
            }
            v.Configurar(new[] { cuerpo }, 0, null);
            v.ConfigurarLuz(new Color(1f, 0.1f, 0.8f, 1f), 3f, 6f);
            EditorUtility.SetDirty(v);
            EditorUtility.SetDirty(jefe.gameObject);
        }
    }
}

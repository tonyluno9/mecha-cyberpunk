using System;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1497: los agentes de Nova a escala real (E15 2.6 m, E16 3.5 m, E18 2.2 m, perro E17 1.2 m; la tabla es <see cref="EscalasNova"/>) y el arma W10_ArmaNova en
    /// sus manos. Batchmode, en este orden, cada uno con -executeMethod:
    ///   MechaCyberpunk.Arte.Editor.EscalaNovaT1497.ImportarArma   material y prefab Assets/Arte/Armas/W10_ArmaNova/W10_ArmaNova.prefab
    ///   MechaCyberpunk.Arte.Editor.EscalaNovaT1497.Visuales       perro a 1.2 m (userData del .meta), visuales E15/E16/E18 a su altura, E16_b si el avatar queda válido
    ///   MechaCyberpunk.Arte.Editor.EscalaNovaT1497.Enemigos       cápsula, factor del cuerpo, arma en la mano, alcance y ConfigRespuestaNova (se puede repetir)
    /// La escala va en el userData del .meta que lee PostprocesadorAssetsT250 y en la tabla EscalasNova, así que un reimport o rehacer los visuales no la deshace.
    /// </summary>
    public static class EscalaNovaT1497
    {
        public const string CarpetaArma = "Assets/Arte/Armas/W10_ArmaNova/";
        public const string RutaArmaFbx = CarpetaArma + "W10_ArmaNova.fbx";
        public const string RutaArmaMaterial = CarpetaArma + "W10_ArmaNova.mat";
        public const string RutaArmaPrefab = CarpetaArma + "W10_ArmaNova.prefab";
        public const string NombreArma = "W10";
        public const string PesadoB = "E16_RobotPesadoNova_b";
        public const string RutaGuardiaBase = "Assets/_Proyecto/IA/Prefabs/GuardiaNova_Armado.prefab";
        public const string RutaInforme = "Logs/t1497.txt";

        /// <summary>Largo del arma en la mano de un guardia de 1.85 m (m); en los grandes se agranda con su factor.</summary>
        public const float LargoArma = 0.95f;
        // Fracciones medidas sobre el render W10_ArmaNova.png (de la boca a la culata y de abajo hacia arriba).
        private const float FraccionLargoEmpunadura = 0.69f;
        private const float FraccionAltoEmpunadura = 0.30f;
        private const float FraccionAltoBoca = 0.50f;
        // Pose de la mano de los enemigos de pistola (la de la W01 de los SKULLS): posición y giro del arma en ManoDer.
        private static readonly Vector3 PosicionEnMano = new Vector3(0f, -0.05f, 0f);
        private static readonly Vector3 GiroEnMano = new Vector3(90f, 0f, 0f);
        private const float AlcancePerro = 1.6f;
        private const float RadioPerro = 0.4f;

        private static readonly StringBuilder Informe = new StringBuilder();

        /// <summary>Variantes de enemigo de Nova que se reescalan (el perro no lleva arma).</summary>
        public static readonly string[] VariantesEnemigo = { "E15_CentinelaNova_a", "E15_CentinelaNova_b", "E16_RobotPesadoNova_a", PesadoB, "E18_EliteNova_a", "E17_PerroRobotNova_a" };

        // ------------------------------------------------------------------ Arma

        [MenuItem("Mecha Cyberpunk/T1497/Importar arma W10")]
        public static void ImportarArma()
        {
            Informe.Clear();
            if (!File.Exists(RutaArmaFbx))
            {
                throw new FileNotFoundException("falta " + RutaArmaFbx);
            }
            AssetDatabase.ImportAsset(RutaArmaFbx, ImportAssetOptions.ForceUpdate);
            ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(RutaArmaFbx);
            if (importador.materialImportMode != ModelImporterMaterialImportMode.None || importador.importAnimation || !importador.isReadable)
            {
                importador.materialImportMode = ModelImporterMaterialImportMode.None;
                importador.importAnimation = false;
                importador.isReadable = true;
                importador.SaveAndReimport();
            }
            Material material = ArmarMaterial();
            ArmarPrefab(material);
            AssetDatabase.SaveAssets();
            Escribir();
        }

        private static string Textura(string tipo) => CarpetaArma + "Texturas/W10_" + tipo + ".jpg";

        private static Material ArmarMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaArmaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, RutaArmaMaterial);
            }
            AssetDatabase.ImportAsset(Textura("basecolor"), ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(Textura("normal"), ImportAssetOptions.ForceUpdate);
            TextureImporter normalImp = (TextureImporter)AssetImporter.GetAtPath(Textura("normal"));
            if (normalImp.textureType != TextureImporterType.NormalMap)
            {
                normalImp.textureType = TextureImporterType.NormalMap;
                normalImp.SaveAndReimport();
            }
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(Textura("basecolor"));
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Textura("normal"));
            Texture2D mascara = HacerMascara();
            material.SetTexture("_BaseMap", color);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", normal);
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
            Informe.AppendLine("material " + RutaArmaMaterial + ": color " + (color != null) + ", normal " + (normal != null) + ", máscara " + (mascara != null));
            return material;
        }

        // Máscara metálico (R) y suavidad (A) hecha de metallic y roughness de Tripo, como las otras armas (ColocarT1014Armas).
        private static Texture2D HacerMascara()
        {
            string ruta = CarpetaArma + "Texturas/W10_mascara.png";
            if (!File.Exists(ruta))
            {
                Texture2D metal = Leer(Textura("metallic"));
                Texture2D rugoso = Leer(Textura("roughness"));
                Texture2D mascara = new Texture2D(metal.width, metal.height, TextureFormat.RGBA32, false, true);
                Color[] m = metal.GetPixels();
                Color[] r = rugoso.width == metal.width && rugoso.height == metal.height ? rugoso.GetPixels() : null;
                Color[] salida = new Color[m.Length];
                for (int i = 0; i < m.Length; i++)
                {
                    float suavidad = 1f - (r != null ? r[i].r : 0.5f);
                    salida[i] = new Color(m[i].r, 0f, 0f, suavidad);
                }
                mascara.SetPixels(salida);
                mascara.Apply();
                File.WriteAllBytes(ruta, mascara.EncodeToPNG());
                Object.DestroyImmediate(metal);
                Object.DestroyImmediate(rugoso);
                Object.DestroyImmediate(mascara);
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
                TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
                ti.sRGBTexture = false;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        private static Texture2D Leer(string ruta)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            t.LoadImage(File.ReadAllBytes(ruta));
            return t;
        }

        /// <summary>
        /// Prefab W10_ArmaNova con el mismo convenio que W07: el cañón a +Z, arriba +Y y la empuñadura en el origen; hijo "Boca" en la punta del cañón. Mide el .fbx
        /// (cañón a -X, largo en X) y saca de ahí la escala (0.95 m de largo), la empuñadura y la boca con las fracciones del render del arte.
        /// </summary>
        private static void ArmarPrefab(Material material)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaArmaFbx);
            GameObject medida = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            Bounds b = Limites(medida);
            Object.DestroyImmediate(medida);
            if (b.size.x < b.size.z)
            {
                Informe.AppendLine("AVISO: el .fbx de la W10 es más largo en Z que en X (" + b.size.ToString("F3") + "): el cañón no va a -X, revisa la orientación en la foto");
            }
            float escala = LargoArma / Mathf.Max(0.01f, b.size.x);
            Vector2 empunadura = new Vector2(b.min.x + FraccionLargoEmpunadura * b.size.x, b.min.y + FraccionAltoEmpunadura * b.size.y);
            Vector2 boca = new Vector2(b.min.x, b.min.y + FraccionAltoBoca * b.size.y);

            GameObject raiz = new GameObject("W10_ArmaNova");
            GameObject modelo = new GameObject("Modelo");
            modelo.transform.SetParent(raiz.transform, false);
            GameObject malla = (GameObject)PrefabUtility.InstantiatePrefab(fbx, modelo.transform);
            PrefabUtility.UnpackPrefabInstance(malla, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            malla.name = "Malla";
            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = material;
            }
            modelo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            modelo.transform.localScale = Vector3.one * escala;
            modelo.transform.localPosition = new Vector3(0f, -escala * empunadura.y, escala * empunadura.x);
            GameObject objetoBoca = new GameObject("Boca");
            objetoBoca.transform.SetParent(raiz.transform, false);
            objetoBoca.transform.localPosition = new Vector3(0f, escala * (boca.y - empunadura.y), escala * (empunadura.x - boca.x));
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaArmaPrefab);
            Object.DestroyImmediate(raiz);
            Informe.AppendLine("prefab " + RutaArmaPrefab + ": limites del .fbx " + b.size.ToString("F3") + ", escala " + escala.ToString("F3") + ", empuñadura " + empunadura
                + ", boca " + boca);
        }

        private static Bounds Limites(GameObject objeto)
        {
            Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ------------------------------------------------------------------ Visuales

        [MenuItem("Mecha Cyberpunk/T1497/Visuales a escala real")]
        public static void Visuales()
        {
            Informe.Clear();
            Paso("perro a escala real", EscalarPerro);
            foreach (string v in new[] { "E15_CentinelaNova_a", "E15_CentinelaNova_b", "E16_RobotPesadoNova_a", "E18_EliteNova_a" })
            {
                Paso("visual " + v, () => RehacerVisual(v, true));
            }
            Paso("visual " + PesadoB + " (opcional)", () => RehacerVisual(PesadoB, false));
            AssetDatabase.SaveAssets();
            Escribir();
        }

        private static void RehacerVisual(string variante, bool obligatorio)
        {
            // Primera pasada: deja la escala del importador (userData) en su valor final. Luego el esqueleto del avatar se pone al tamaño del modelo
            // actual y se vuelve a pasar Crear (que mide los clips con el avatar ya bueno).
            GameObject prefab = CreadorVisualTripo.Crear(variante);
            if (ActualizarEsqueleto(variante))
            {
                prefab = CreadorVisualTripo.Crear(variante);
            }
            if (prefab == null)
            {
                Informe.AppendLine("  " + variante + ": sin visual (avatar Humanoid no válido)" + (obligatorio ? " ERROR" : ", queda fuera de Nova"));
                return;
            }
            PersonajesNuevosT1389b.ConectarClipsPJ01(variante);
            Animator a = prefab.GetComponentInChildren<Animator>(true);
            bool valido = a != null && a.avatar != null && a.avatar.isValid && a.avatar.isHuman;
            ModelImporter imp = AssetImporter.GetAtPath(CreadorVisualTripo.RutaModelo(variante)) as ModelImporter;
            Informe.AppendLine("  " + variante + ": visual listo, avatar humano válido " + valido + ", userData " + (imp != null ? imp.userData : "?") + ", altura objetivo "
                + CreadorVisualTripo.AlturaDe(variante) + " m");
        }

        /// <summary>
        /// T-1497 (causa de que los agentes animados midieran lo de antes): el .meta guarda el esqueleto del avatar (humanDescription.skeleton) con las posiciones de los
        /// huesos del FBX viejo y Unity no lo regenera al cambiar el FBX. Con el modelo de escala real (huesos 1.4 veces más largos) el Humanoid animaba un esqueleto del
        /// tamaño viejo bajo una malla grande. Aquí se vuelve a escribir el esqueleto con las posiciones del modelo actual (a su escala de importación), conservando el
        /// mapeo de huesos. Devuelve true si el avatar resultante es humanoide válido y se guardó.
        /// </summary>
        public static bool ActualizarEsqueleto(string variante)
        {
            string ruta = CreadorVisualTripo.RutaModelo(variante);
            ModelImporter imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (imp == null || modelo == null) { return false; }
            GameObject go = (GameObject)Object.Instantiate(modelo);
            go.name = modelo.name;
            try
            {
                HumanDescription hd = imp.humanDescription;
                if (hd.human == null || hd.human.Length == 0) { return false; }
                var esqueleto = new System.Collections.Generic.List<SkeletonBone>();
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                {
                    esqueleto.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
                }
                hd.skeleton = esqueleto.ToArray();
                Avatar prueba = AvatarBuilder.BuildHumanAvatar(go, hd);
                bool valido = prueba != null && prueba.isValid && prueba.isHuman;
                if (prueba != null) { Object.DestroyImmediate(prueba); }
                Informe.AppendLine("  " + variante + ": esqueleto del avatar al tamaño del modelo actual, avatar humano válido " + valido);
                if (!valido) { return false; }
                imp.humanDescription = hd;
                imp.SaveAndReimport();
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>E17 es Generic y no pasa por Crear: se mide a escala 1 y se guarda la escala en el userData del .meta (la lee PostprocesadorAssetsT250).</summary>
        private static void EscalarPerro()
        {
            string ruta = CreadorVisualTripo.RutaModelo(PersonajesNuevosT1389b.PerroVariante);
            ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(ruta);
            imp.userData = "escala=1";
            imp.SaveAndReimport();
            float alto = CreadorVisualTripo.AltoDelModelo(ruta);
            float escala = alto > 0.01f ? EscalasNova.AlturaPerroE17 / alto : 1f;
            imp.userData = "escala=" + escala.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture);
            imp.SaveAndReimport();
            Informe.AppendLine("  perro: alto a escala 1 " + alto.ToString("F3") + " m -> escala " + escala.ToString("F4") + " (objetivo " + EscalasNova.AlturaPerroE17 + " m)");
        }

        // ------------------------------------------------------------------ Enemigos

        [MenuItem("Mecha Cyberpunk/T1497/Enemigos de Nova")]
        public static void Enemigos()
        {
            Informe.Clear();
            RetocarEnemigos(Informe);
            AssetDatabase.SaveAssets();
            Escribir();
        }

        /// <summary>Se puede repetir: pone cápsula, factor del cuerpo, arma W10, alcance y la lista de pesados de ConfigRespuestaNova. T1389b la llama al rehacer los prefabs.</summary>
        public static void RetocarEnemigos(StringBuilder informe)
        {
            GameObject arma = AssetDatabase.LoadAssetAtPath<GameObject>(RutaArmaPrefab);
            if (arma == null) { informe.AppendLine("  AVISO: no hay " + RutaArmaPrefab + " (corre ImportarArma): los enemigos quedan sin arma W10"); }

            DatosPandilleroAPie nova = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(PersonajesNuevosT1389b.RutaDatosNova);
            DatosPandilleroAPie pesadoBase = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(PersonajesNuevosT1389b.RutaDatosPesadoBase);
            PonerAlcance(PersonajesNuevosT1389b.RutaDatosElite, nova, EscalasNova.Factor(EscalasNova.AlturaEliteE18), informe);
            PonerAlcance(PersonajesNuevosT1389b.RutaDatosPesadoE16, pesadoBase, EscalasNova.AlturaRobotPesadoE16 / 2.05f, informe);
            DatosPandilleroAPie perro = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(PersonajesNuevosT1389b.RutaDatosPerro);
            if (perro != null)
            {
                SerializedObject so = new SerializedObject(perro);
                so.FindProperty("alcanceGolpe").floatValue = AlcancePerro;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(perro);
            }

            PrefabDePesadoB(pesadoBase, informe);
            foreach (string v in VariantesEnemigo)
            {
                string ruta = PersonajesNuevosT1389b.RutaPrefabEnemigo(v);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
                {
                    informe.AppendLine("  " + v + ": sin prefab de enemigo (" + ruta + "), se salta");
                    continue;
                }
                RetocarPrefab(ruta, v, arma, informe);
            }
            RetocarPrefab(RutaGuardiaBase, null, arma, informe);
            PonerPesadosEnConfig(informe);
        }

        private static void PonerAlcance(string rutaDatos, DatosPandilleroAPie baseDatos, float factor, StringBuilder informe)
        {
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(rutaDatos);
            if (datos == null || baseDatos == null) { return; }
            float alcance = new SerializedObject(baseDatos).FindProperty("alcanceGolpe").floatValue * factor;
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("alcanceGolpe").floatValue = alcance;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            informe.AppendLine("  " + datos.name + ": alcanceGolpe " + alcance.ToString("F2") + " m (x" + factor.ToString("F2") + ")");
        }

        /// <summary>Si E16_b quedó con visual de avatar humano válido y aún no tiene prefab de enemigo, se arma (copia de E12 con su visual).</summary>
        private static void PrefabDePesadoB(DatosPandilleroAPie pesadoBase, StringBuilder informe)
        {
            string ruta = PersonajesNuevosT1389b.RutaPrefabEnemigo(PesadoB);
            GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaVisual(PesadoB));
            Animator a = visual != null ? visual.GetComponentInChildren<Animator>(true) : null;
            if (a == null || a.avatar == null || !a.avatar.isValid || !a.avatar.isHuman)
            {
                informe.AppendLine("  " + PesadoB + ": sin visual con avatar humano válido: sin prefab de enemigo, fuera de Nova");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) != null) { return; }
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(PersonajesNuevosT1389b.RutaDatosPesadoE16);
            GameObject nuevo = PersonajesNuevosT1389b.ArmarEnemigo(PersonajesNuevosT1389b.RutaPesadoBase, PesadoB, ruta, datos != null ? datos : pesadoBase, false);
            informe.AppendLine("  " + PesadoB + ": prefab de enemigo " + (nuevo != null ? "creado " + ruta : "NO se pudo crear"));
        }

        private static void RetocarPrefab(string ruta, string variante, GameObject arma, StringBuilder informe)
        {
            float altura = variante != null ? EscalasNova.Altura(variante) : 0f;
            bool esPerro = variante != null && variante.StartsWith("E17");
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                if (altura > 0f)
                {
                    float alto = esPerro ? altura : EscalasNova.AlturaCapsula(altura);
                    float radio = esPerro ? RadioPerro : EscalasNova.RadioCapsula(altura);
                    CharacterController cc = contenido.GetComponent<CharacterController>();
                    cc.height = alto;
                    cc.radius = radio;
                    cc.center = new Vector3(0f, alto * 0.5f, 0f);
                    cc.stepOffset = Mathf.Min(EscalasNova.PasoCapsula(altura), alto * 0.5f - 0.01f);
                    SerializedObject so = new SerializedObject(contenido.GetComponent<PandilleroAPie>());
                    so.FindProperty("escalaCuerpo").floatValue = esPerro ? 1f : EscalasNova.Factor(altura);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                string arm = "sin arma";
                if (arma != null && !esPerro)
                {
                    float factor = altura > 0f ? EscalasNova.Factor(altura) : 1f;
                    arm = PonerArma(contenido, arma, factor) ? "W10 x" + factor.ToString("F2") : "SIN ManoDer: no se pudo poner la W10";
                }
                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                informe.AppendLine("  " + Path.GetFileNameWithoutExtension(ruta) + ": altura " + altura + " m, cápsula " + (altura > 0f ? "ajustada" : "igual") + ", " + arm);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        /// <summary>Pone la W10 en el socket ManoDer del modelo (idempotente: quita la que hubiera). El arma sube de tamaño con el agente.</summary>
        public static bool PonerArma(GameObject enemigo, GameObject prefabArma, float factor)
        {
            Transform mano = enemigo.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == CreadorVisualTripo.NombreSocket);
            if (mano == null) { return false; }
            Transform vieja = mano.Find(NombreArma);
            while (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
                vieja = mano.Find(NombreArma);
            }
            GameObject w = (GameObject)PrefabUtility.InstantiatePrefab(prefabArma, mano);
            w.name = NombreArma;
            w.transform.localPosition = PosicionEnMano;
            w.transform.localRotation = Quaternion.Euler(GiroEnMano);
            w.transform.localScale = Vector3.one * factor;
            return true;
        }

        private static void PonerPesadosEnConfig(StringBuilder informe)
        {
            ConfigRespuestaNova config = AssetDatabase.LoadAssetAtPath<ConfigRespuestaNova>(PersonajesNuevosT1389b.RutaConfigNova);
            if (config == null) { informe.AppendLine("  falta " + PersonajesNuevosT1389b.RutaConfigNova); return; }
            SerializedObject so = new SerializedObject(config);
            SerializedProperty lista = so.FindProperty("pesadosExtra");
            bool tieneB = false;
            string rutaB = PersonajesNuevosT1389b.RutaPrefabEnemigo(PesadoB);
            for (int i = 0; i < lista.arraySize; i++)
            {
                Object o = lista.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o != null && AssetDatabase.GetAssetPath(o) == rutaB) { tieneB = true; }
            }
            GameObject b = AssetDatabase.LoadAssetAtPath<GameObject>(rutaB);
            if (b != null && !tieneB)
            {
                lista.arraySize++;
                lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = b;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
            }
            informe.AppendLine("  ConfigRespuestaNova.pesadosExtra: " + lista.arraySize + " (E16_b " + (b != null ? "incluido" : "fuera") + ")");
        }

        private static void Paso(string nombre, Action accion)
        {
            try
            {
                accion();
                Informe.AppendLine("OK    " + nombre);
            }
            catch (Exception e)
            {
                Informe.AppendLine("FALLO " + nombre + ": " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                Debug.LogException(e);
            }
        }

        private static void Escribir()
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText(RutaInforme, Informe + "\n");
            Debug.Log("[T-1497]\n" + Informe);
        }
    }
}

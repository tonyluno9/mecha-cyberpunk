using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1389b: mete al juego los personajes que se rigearon en Blender (T-1381): enemigos de Nova (E15, E16, E17, E18), de los SKULLS (E19, E20) y
    /// peatones (NP07 a NP14). Copia cada FBX de imageassets/blender/personajes/ a Assets/Arte/Personajes/&lt;Codigo&gt;/, extrae sus texturas, arma el
    /// visual (Humanoid con avatar propio, o Generic con sus clips en el perro), los prefabs de enemigo, la lista de modelos del peatón y la respuesta de Nova.
    /// Batchmode (en este orden, cada uno con -executeMethod):
    ///   MechaCyberpunk.Arte.Editor.PersonajesNuevosT1389b.Aplicar          modelos, visuales, peatones, enemigos y Logs/t1389b-personajes.txt
    ///   MechaCyberpunk.Arte.Editor.PersonajesNuevosT1389b.EscenaSkulls     cambia los SKULLS de la escena Ciudad_Prologo por las variantes nuevas
    ///   MechaCyberpunk.Arte.Editor.PersonajesNuevosT1389b.Fotos            docs/capturas/t-1389b/&lt;Variante&gt;_frente.png y _lado.png
    /// </summary>
    public static class PersonajesNuevosT1389b
    {
        public const string CarpetaOrigen = "imageassets/blender/personajes";
        public const string CarpetaPersonajes = "Assets/Arte/Personajes";
        public const string RutaInforme = "Logs/t1389b-personajes.txt";
        public const string CarpetaFotos = "docs/capturas/t-1389b";
        public const string CarpetaPrefabsIA = "Assets/_Proyecto/IA/Prefabs";
        public const string CarpetaDatosIA = "Assets/_Proyecto/IA/Datos";
        public const string RutaPeaton = "Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab";
        public const string RutaConfigNova = "Assets/_Proyecto/IA/Resources/ConfigRespuestaNova.asset";
        public const string RutaPandilleroBase = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        public const string RutaPandilleroSkulls = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        public const string RutaPesadoBase = "Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E12.prefab";
        public const string RutaDatosNova = CarpetaDatosIA + "/DatosPandilleroAPie_Nova.asset";
        public const string RutaDatosPesadoBase = CarpetaDatosIA + "/DatosPandilleroAPie_GuardiaPesadoE12.asset";
        public const string RutaDatosCalaveras = CarpetaDatosIA + "/DatosPandilleroAPie_Calaveras.asset";
        public const string RutaDatosElite = CarpetaDatosIA + "/DatosPandilleroAPie_EliteNovaE18.asset";
        public const string RutaDatosPesadoE16 = CarpetaDatosIA + "/DatosPandilleroAPie_RobotPesadoE16.asset";
        public const string RutaDatosPerro = CarpetaDatosIA + "/DatosPandilleroAPie_PerroRobotE17.asset";
        public const string RutaDatosSkullsNuevos = CarpetaDatosIA + "/DatosPandilleroAPie_CalaverasE19E20.asset";
        public const string PerroVariante = "E17_PerroRobotNova_a";
        public const float AlturaPerro = 0.9f;
        public const float ProbabilidadVariantePeaton = 0.65f;
        public const int PerrosConCuatro = 2;
        public const int PerrosConCinco = 3;
        private const float VidaPerro = 45f;
        private const float DanioPerro = 8f;
        private const float VelocidadPerro = 6.5f;
        private const float AlcancePerro = 1.4f;
        private const float FactorVidaElite = 1.6f;
        private const float VidaPesadoE16 = 260f;
        private const float RapidezClipCaminarPerro = 0.8f;
        private const float RapidezMaximaPerro = 3f;
        private const float AlturaControladorPerro = 0.9f;
        private const float RadioControladorPerro = 0.35f;
        private const float DistanciaFoto = 4.2f;
        private const float DistanciaFotoPerro = 2.6f;
        private const float AperturaFoto = 35f;
        private const float AnchoCubo = 0.35f;
        private const float DesplazamientoCubo = 0.95f;
        private const float AvanceIdleFoto = 0.1f;

        /// <summary>Todas las variantes que hay en imageassets/blender/personajes/ (E19_a se omite: es la misma malla que E20_a).</summary>
        public static readonly string[] Variantes =
        {
            "E15_CentinelaNova_a", "E15_CentinelaNova_b", "E16_RobotPesadoNova_a", "E17_PerroRobotNova_a", "E18_EliteNova_a",
            "E19_PandilleroSkulls_a_2", "E19_PandilleroSkulls_b", "E20_MatonSkulls_a", "E20_MatonSkulls_b",
            "NP07_Pescador_a", "NP07_Pescador_b", "NP08_Turista_a", "NP08_Turista_b", "NP09_Corredor_a", "NP09_Corredor_b", "NP10_Elotero_a",
            "NP11_Estibador_a", "NP11_Estibador_b", "NP12_Abuela_a", "NP13_Repartidor_a", "NP13_Repartidor_b", "NP14_Oficinista_a", "NP14_Oficinista_b",
        };

        /// <summary>Los peatones nuevos (NP07 a NP14), todas sus variantes.</summary>
        public static readonly string[] Peatones =
        {
            "NP07_Pescador_a", "NP07_Pescador_b", "NP08_Turista_a", "NP08_Turista_b", "NP09_Corredor_a", "NP09_Corredor_b", "NP10_Elotero_a",
            "NP11_Estibador_a", "NP11_Estibador_b", "NP12_Abuela_a", "NP13_Repartidor_a", "NP13_Repartidor_b", "NP14_Oficinista_a", "NP14_Oficinista_b",
        };

        /// <summary>Variantes de guardia de Nova (centinela E15 y élite E18).</summary>
        public static readonly string[] GuardiasNova = { "E15_CentinelaNova_a", "E15_CentinelaNova_b", "E18_EliteNova_a" };

        /// <summary>Variantes de guardia pesado de Nova (robot pesado E16).</summary>
        public static readonly string[] PesadosNova = { "E16_RobotPesadoNova_a" };

        /// <summary>Variantes de pandillero SKULLS (E19 y E20).</summary>
        public static readonly string[] Skulls = { "E19_PandilleroSkulls_a_2", "E19_PandilleroSkulls_b", "E20_MatonSkulls_a", "E20_MatonSkulls_b" };

        public static string RutaVisual(string variante) => CreadorVisualPersonaje.RutaPrefab(variante);

        public static string RutaPrefabEnemigo(string variante) => CarpetaPrefabsIA + "/" + PrefijoEnemigo(variante) + variante + ".prefab";

        private static string PrefijoEnemigo(string variante)
        {
            if (variante.StartsWith("E16")) { return "GuardiaPesado_"; }
            if (variante.StartsWith("E17")) { return "PerroNova_"; }
            if (variante.StartsWith("E19") || variante.StartsWith("E20")) { return "PandilleroArmado_Skulls_"; }
            return "GuardiaNova_";
        }

        /// <summary>Rutas de los prefabs de SKULLS nuevos, en el orden de <see cref="Skulls"/>.</summary>
        public static string[] RutasPandilleroSkulls()
        {
            var rutas = new string[Skulls.Length];
            for (int i = 0; i < Skulls.Length; i++) { rutas[i] = RutaPrefabEnemigo(Skulls[i]); }
            return rutas;
        }

        public static string CodigoDe(string variante) => System.IO.Path.GetFileName(CreadorVisualTripo.CarpetaDe(variante));

        private static readonly StringBuilder Informe = new StringBuilder();

        // ------------------------------------------------------------------ Aplicar

        [MenuItem("Mecha Cyberpunk/T1389b/Aplicar personajes nuevos")]
        public static void Aplicar()
        {
            Informe.Clear();
            Informe.AppendLine("T-1389b personajes nuevos, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            Paso("Importar modelos", ImportarModelos);
            Paso("Visuales humanoides", CrearVisuales);
            Paso("Visual del perro robot", CrearVisualPerro);
            Paso("Peatones", ConectarPeatones);
            Paso("Enemigos de Nova", ConectarNova);
            Paso("Enemigos de SKULLS", ConectarSkulls);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Paso("Informe de prefabs", EscribirResumenPrefabs);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, Informe.ToString());
            Debug.Log("[T-1389b] listo, informe en " + RutaInforme);
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

        // ------------------------------------------------------------------ Modelos

        /// <summary>Copia los FBX, los importa y extrae las texturas embebidas a Texturas/&lt;variante&gt;/ (los nombres se repiten entre variantes).</summary>
        public static void ImportarModelos()
        {
            string raiz = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            foreach (string v in Variantes)
            {
                string origen = Path.Combine(raiz, CarpetaOrigen, v + ".fbx");
                if (!File.Exists(origen))
                {
                    Informe.AppendLine("  falta el FBX " + origen);
                    continue;
                }
                string destino = CreadorVisualTripo.RutaModelo(v);
                Directory.CreateDirectory(Path.Combine(raiz, Path.GetDirectoryName(destino)));
                File.Copy(origen, Path.Combine(raiz, destino), true);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            foreach (string v in Variantes)
            {
                string ruta = CreadorVisualTripo.RutaModelo(v);
                ModelImporter imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (imp == null)
                {
                    continue;
                }
                string texturas = CreadorVisualTripo.CarpetaDe(v) + "/Texturas/" + v;
                Directory.CreateDirectory(Path.Combine(raiz, texturas));
                AssetDatabase.Refresh();
                imp.ExtractTextures(texturas);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        // ------------------------------------------------------------------ Visuales

        public static void CrearVisuales()
        {
            foreach (string v in Variantes)
            {
                if (v == PerroVariante || AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualTripo.RutaModelo(v)) == null)
                {
                    continue;
                }
                GameObject prefab = CreadorVisualTripo.Crear(v);
                if (prefab != null)
                {
                    ConectarClipsPJ01(v);
                }
                Informe.AppendLine(prefab != null ? "  visual " + v + " creado" : "  FALLO visual " + v + ": revisa el log (avatar Humanoid no válido o sin modelo)");
            }
        }

        /// <summary>
        /// T-1411 (T-1004): los humanoides nuevos usan los clips de PJ01 como los demás NPC: el visual lleva el override de su papel sobre el ControladorBase
        /// (E15, E16 y E18 Guardias; E19 y E20 SKULLS; peatones NP NPC), no el controlador con clips de Mixamo que arma CreadorVisualTripo. Se puede repetir.
        /// </summary>
        public static void ConectarClipsPJ01(string variante)
        {
            if (variante == PerroVariante || AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual(variante)) == null)
            {
                return;
            }
            string papel = variante.StartsWith("E15") || variante.StartsWith("E16") || variante.StartsWith("E18") ? "Guardias"
                : variante.StartsWith("E19") || variante.StartsWith("E20") ? "SKULLS" : "NPC";
            ConectarControladorBase.ConectarModeloConClipsPJ01(variante, papel, true);
        }

        /// <summary>T-1411: pone los clips de PJ01 en todos los visuales nuevos ya creados (sin volver a importar).</summary>
        public static void ConectarTodosClipsPJ01()
        {
            foreach (string v in Variantes)
            {
                ConectarClipsPJ01(v);
            }
        }

        /// <summary>E17: Generic, con Perro_Idle y Perro_Caminar del FBX en bucle; el controlador usa el mismo esquema de estados que los humanos (Idle y Caminar; Correr = Caminar).</summary>
        public static void CrearVisualPerro()
        {
            string ruta = CreadorVisualTripo.RutaModelo(PerroVariante);
            ModelImporter imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (imp == null)
            {
                Informe.AppendLine("  FALLO perro: no existe " + ruta);
                return;
            }
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            ModelImporterClipAnimation[] porDefecto = imp.defaultClipAnimations;
            var lista = new List<ModelImporterClipAnimation>();
            foreach (ModelImporterClipAnimation c in porDefecto)
            {
                string toma = c.takeName ?? string.Empty;
                if (toma.Contains("Perro_Idle")) { c.name = "Perro_Idle"; }
                else if (toma.Contains("Perro_Caminar")) { c.name = "Perro_Caminar"; }
                else { continue; }
                c.loopTime = true;
                c.loopPose = true;
                lista.Add(c);
            }
            if (lista.Count > 0)
            {
                imp.clipAnimations = lista.ToArray();
            }
            imp.SaveAndReimport();

            Avatar avatar = null;
            AnimationClip idle = null, caminar = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (o is Avatar a) { avatar = a; }
                else if (o is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    if (c.name == "Perro_Idle") { idle = c; }
                    else if (c.name == "Perro_Caminar") { caminar = c; }
                }
            }
            var clips = new Dictionary<ClipVisual, AnimationClip>();
            if (idle != null) { clips[ClipVisual.Idle] = idle; }
            if (caminar != null) { clips[ClipVisual.Caminar] = caminar; clips[ClipVisual.Correr] = caminar; }
            foreach (ClipVisual tipo in Enum.GetValues(typeof(ClipVisual)))
            {
                if (!clips.ContainsKey(tipo) && idle != null) { clips[tipo] = idle; }
            }
            if (idle == null || caminar == null)
            {
                Informe.AppendLine("  perro: faltan clips (Perro_Idle " + (idle != null) + ", Perro_Caminar " + (caminar != null) + ")");
            }
            AnimatorController controlador = ConstructorControladorVisual.Construir(CreadorVisualPersonaje.RutaControlador(PerroVariante), clips, out _);
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(PerroVariante))
                ?? CreadorVisualPersonaje.ArmarPrefab(PerroVariante, modelo, avatar, controlador);
            Material material = CreadorVisualTripo.AsegurarMaterialBase(PerroVariante);
            string rutaPrefab = AssetDatabase.GetAssetPath(prefab);
            GameObject contenido = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                foreach (SkinnedMeshRenderer r in contenido.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    r.sharedMaterial = material;
                }
                contenido.GetComponent<VisualPersonaje>().ConfigurarClips(RapidezClipCaminarPerro, RapidezMaximaPerro, RapidezMaximaPerro);
                PrefabUtility.SaveAsPrefabAsset(contenido, rutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Informe.AppendLine("  visual " + PerroVariante + " creado (Generic, avatar " + (avatar != null && avatar.isValid) + ")");
        }

        // ------------------------------------------------------------------ Peatones

        /// <summary>
        /// Agrega los NP07 a NP14 a PeatonVariado del prefab Peaton (se crea solo el elegido al aparecer). El sistema de peatones no sabe de zonas
        /// (todos salen del mismo prefab), así que se mezclan en la lista general.
        /// </summary>
        public static void ConectarPeatones()
        {
            var visuales = new List<GameObject>();
            foreach (string v in Peatones)
            {
                GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual(v));
                if (g == null) { Informe.AppendLine("  peatón sin visual: " + v); continue; }
                visuales.Add(g);
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPeaton);
            try
            {
                PeatonVariado variado = contenido.GetComponent<PeatonVariado>();
                if (variado == null)
                {
                    throw new InvalidOperationException("el prefab Peaton no tiene PeatonVariado");
                }
                variado.ConfigurarVariantes(visuales.ToArray(), ProbabilidadVariantePeaton);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPeaton);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            Informe.AppendLine("  Peaton.prefab: " + visuales.Count + " variantes de NP07 a NP14, probabilidad " + ProbabilidadVariantePeaton);
        }

        // ------------------------------------------------------------------ Enemigos

        private static DatosPandilleroAPie CopiarDatos(string origen, string destino, Action<SerializedObject> ajustar)
        {
            if (AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(destino) == null)
            {
                if (!AssetDatabase.CopyAsset(origen, destino))
                {
                    throw new InvalidOperationException("no se pudo copiar " + origen);
                }
            }
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(destino);
            SerializedObject so = new SerializedObject(datos);
            ajustar(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static GameObject ArmarEnemigo(string rutaBase, string visual, string rutaPrefab, DatosPandilleroAPie datos, bool pasarArmas, Action<GameObject> extra = null)
        {
            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual(visual));
            if (visualPrefab == null)
            {
                Informe.AppendLine("  sin visual para " + visual);
                return null;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(rutaBase);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                ConectarVisualesT520.Reemplazar(contenido, viejo, visualPrefab, "Modelo", pasarArmas);
                contenido.name = Path.GetFileNameWithoutExtension(rutaPrefab);
                SerializedObject so = new SerializedObject(contenido.GetComponent<PandilleroAPie>());
                so.FindProperty("datos").objectReferenceValue = datos;
                so.ApplyModifiedPropertiesWithoutUndo();
                extra?.Invoke(contenido);
                return PrefabUtility.SaveAsPrefabAsset(contenido, rutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        public static void ConectarNova()
        {
            DatosPandilleroAPie nova = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosNova);
            DatosPandilleroAPie elite = CopiarDatos(RutaDatosNova, RutaDatosElite, so =>
            {
                so.FindProperty("nombre").stringValue = "ELITE NOVA";
                SerializedProperty vida = so.FindProperty("vidaMaxima");
                vida.floatValue = Mathf.Round(vida.floatValue * FactorVidaElite);
            });
            DatosPandilleroAPie pesado = CopiarDatos(RutaDatosPesadoBase, RutaDatosPesadoE16, so =>
            {
                so.FindProperty("nombre").stringValue = "ROBOT PESADO NOVA";
                so.FindProperty("vidaMaxima").floatValue = VidaPesadoE16;
            });
            DatosPandilleroAPie perro = CopiarDatos(RutaDatosNova, RutaDatosPerro, so =>
            {
                so.FindProperty("nombre").stringValue = "PERRO ROBOT";
                so.FindProperty("vidaMaxima").floatValue = VidaPerro;
                so.FindProperty("danioGolpe").floatValue = DanioPerro;
                so.FindProperty("velocidad").floatValue = VelocidadPerro;
                so.FindProperty("alcanceGolpe").floatValue = AlcancePerro;
                so.FindProperty("usaPistola").boolValue = false;
                so.FindProperty("dinero").intValue = 0;
            });

            var guardias = new List<GameObject>();
            foreach (string v in GuardiasNova)
            {
                GameObject g = ArmarEnemigo(RutaPandilleroBase, v, RutaPrefabEnemigo(v), v.StartsWith("E18") ? elite : nova, true);
                if (g != null) { guardias.Add(g); }
            }
            var pesados = new List<GameObject>();
            foreach (string v in PesadosNova)
            {
                GameObject g = ArmarEnemigo(RutaPesadoBase, v, RutaPrefabEnemigo(v), pesado, false);
                if (g != null) { pesados.Add(g); }
            }
            var perros = new List<GameObject>();
            GameObject elPerro = ArmarEnemigo(RutaPandilleroBase, PerroVariante, RutaPrefabEnemigo(PerroVariante), perro, false, raiz =>
            {
                CharacterController cc = raiz.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.height = AlturaControladorPerro;
                    cc.radius = RadioControladorPerro;
                    cc.center = new Vector3(0f, AlturaControladorPerro * 0.5f, 0f);
                }
            });
            if (elPerro != null) { perros.Add(elPerro); }

            ConfigRespuestaNova config = AssetDatabase.LoadAssetAtPath<ConfigRespuestaNova>(RutaConfigNova);
            if (config == null)
            {
                throw new InvalidOperationException("falta " + RutaConfigNova + " (corre CrearRespuestaNovaT1304.Aplicar)");
            }
            config.ConfigurarVariantes(guardias.ToArray(), pesados.ToArray(), perros.ToArray(), PerrosConCuatro, PerrosConCinco);
            EditorUtility.SetDirty(config);
            Informe.AppendLine("  ConfigRespuestaNova: " + guardias.Count + " guardias extra, " + pesados.Count + " pesados extra, " + perros.Count + " perro(s) ("
                + PerrosConCuatro + " con 4 estrellas, " + PerrosConCinco + " con 5)");
        }

        public static void ConectarSkulls()
        {
            DatosPandilleroAPie datos = CopiarDatos(RutaDatosCalaveras, RutaDatosSkullsNuevos, so =>
            {
                // El cuerpo de las variantes nuevas ya trae su textura: sin el tinte oscuro de man.
                so.FindProperty("colorNormal").colorValue = Color.white;
            });
            int hechos = 0;
            foreach (string v in Skulls)
            {
                if (ArmarEnemigo(RutaPandilleroSkulls, v, RutaPrefabEnemigo(v), datos, true) != null) { hechos++; }
            }
            Informe.AppendLine("  prefabs de SKULLS: " + hechos + " de " + Skulls.Length);
        }

        /// <summary>
        /// Cambia los pandilleros SKULLS de Ciudad_Prologo por las variantes nuevas (por turnos). Los que ya son una variante nueva no se tocan.
        /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.PersonajesNuevosT1389b.EscenaSkulls
        /// </summary>
        public static void EscenaSkulls()
        {
            var escena = EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            var prefabs = new List<GameObject>();
            foreach (string ruta in RutasPandilleroSkulls())
            {
                GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (g != null) { prefabs.Add(g); }
            }
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosSkullsNuevos);
            int cambiados = 0;
            if (prefabs.Count > 0 && datos != null)
            {
                foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
                {
                    if (p == null || p.gameObject.scene != escena || !PrefabUtility.IsOutermostPrefabInstanceRoot(p.gameObject))
                    {
                        continue;
                    }
                    GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(p.gameObject);
                    if (fuente == null || AssetDatabase.GetAssetPath(fuente) != RutaPandilleroSkulls)
                    {
                        continue;
                    }
                    string nombre = p.gameObject.name;
                    GameObject nuevo = prefabs[cambiados % prefabs.Count];
                    PrefabUtility.ReplacePrefabAssetOfPrefabInstance(p.gameObject, nuevo, InteractionMode.AutomatedAction);
                    p.gameObject.name = nombre;
                    SerializedObject so = new SerializedObject(p.GetComponent<PandilleroAPie>());
                    so.FindProperty("datos").objectReferenceValue = datos;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(p.gameObject);
                    cambiados++;
                }
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1389b-escena-skulls.txt", "SKULLS cambiados en " + escena.path + ": " + cambiados + " (variantes " + prefabs.Count + ")\n");
            Debug.Log("[T-1389b] SKULLS cambiados en la escena: " + cambiados);
        }

        // ------------------------------------------------------------------ Informe de prefabs

        private static void EscribirResumenPrefabs()
        {
            Informe.AppendLine();
            Informe.AppendLine("Variante | alto en pose (m) | avatar | huesos mapeados | prefab");
            foreach (string v in Variantes)
            {
                string visual = RutaVisual(v);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(visual);
                if (prefab == null)
                {
                    Informe.AppendLine(v + " | - | - | - | SIN PREFAB (no se pudo)");
                    continue;
                }
                float alto = MedirAlto(prefab);
                Animator a = prefab.GetComponentInChildren<Animator>(true);
                Avatar avatar = a != null ? a.avatar : null;
                bool esperaHumano = v != PerroVariante;
                bool valido = avatar != null && avatar.isValid && avatar.isHuman == esperaHumano;
                int huesos = -1;
                ModelImporter imp = AssetImporter.GetAtPath(CreadorVisualTripo.RutaModelo(v)) as ModelImporter;
                if (imp != null)
                {
                    huesos = imp.humanDescription.human != null ? imp.humanDescription.human.Length : 0;
                }
                Informe.AppendLine(v + " | " + alto.ToString("0.00") + " | " + (valido ? (esperaHumano ? "Humanoid válido" : "Generic válido") : "NO VÁLIDO") + " | "
                    + (esperaHumano ? huesos.ToString() : "n/a (Generic)") + " | " + visual);
            }
        }

        private static float MedirAlto(GameObject prefab)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Bounds? total = null;
                foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
                {
                    if (total.HasValue) { Bounds b = total.Value; b.Encapsulate(r.bounds); total = b; }
                    else { total = r.bounds; }
                }
                return total.HasValue ? total.Value.size.y : 0f;
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }
        }

        // ------------------------------------------------------------------ Fotos

        /// <summary>Una foto de frente y una de lado por variante, junto a un cubo de la altura del personaje (1.8 m; 0.9 m el perro).</summary>
        [MenuItem("Mecha Cyberpunk/T1389b/Fotos de los personajes nuevos")]
        public static void Fotos()
        {
            var cuenta = new StringBuilder();
            foreach (string v in Variantes)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual(v));
                if (prefab == null)
                {
                    cuenta.AppendLine("sin foto (sin prefab): " + v);
                    continue;
                }
                try
                {
                    CapturaPersonajes.EscenaDePrueba();
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    inst.transform.position = Vector3.zero;
                    Animator a = inst.GetComponentInChildren<Animator>(true);
                    if (a != null)
                    {
                        a.Rebind();
                        a.Update(AvanceIdleFoto);
                    }
                    bool perro = v == PerroVariante;
                    float altoCubo = perro ? AlturaPerro : 1.8f;
                    GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cubo.name = "Cubo";
                    cubo.transform.localScale = new Vector3(AnchoCubo, altoCubo, AnchoCubo);
                    cubo.transform.position = new Vector3(-DesplazamientoCubo, altoCubo * 0.5f, 0f);
                    cubo.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.9f, 0.5f, 0.1f) };
                    Vector3 centro = new Vector3(-DesplazamientoCubo * 0.5f, 0f, 0f);
                    float distancia = perro ? DistanciaFotoPerro : DistanciaFoto;
                    float altura = altoCubo * 0.55f;
                    Camera frente = CapturaPersonajes.ColocarCamara(centro, Vector3.forward, distancia, altura, AperturaFoto);
                    CapturaPersonajes.Guardar(frente, CarpetaFotos + "/" + v + "_frente.png");
                    Camera lado = CapturaPersonajes.ColocarCamara(new Vector3(0f, 0f, 0f), Vector3.right, distancia, altura, AperturaFoto);
                    CapturaPersonajes.Guardar(lado, CarpetaFotos + "/" + v + "_lado.png");
                    cuenta.AppendLine("fotos: " + v);
                }
                catch (Exception e)
                {
                    cuenta.AppendLine("FALLO foto " + v + ": " + e.Message);
                    Debug.LogException(e);
                }
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1389b-fotos.txt", cuenta.ToString());
        }
    }
}

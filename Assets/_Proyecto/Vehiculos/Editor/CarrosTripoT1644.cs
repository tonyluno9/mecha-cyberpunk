using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T24 / T-1644: todos los carros civiles y las motos vuelven al modelo ORIGINAL de Tripo (una sola malla, las ruedas no giran) con escala real; la ÚNICA excepción es la
    /// patrulla buena de docs/capturas/patrulla-buena-22nov.png: Patrulla.prefab, que usa el cuerpo de Carro_PA01 (puertas que abren, torreta y luces), y Carro_PA01 sigue intacto.
    /// No se tocan PA01, PA02 ni V11 (patrullas / camioneta de policía) ni V10 (descartado en T-1554).
    /// Aplicar (idempotente): reimporta los FBX de Tripo copiados a Assets/Arte/Vehiculos/&lt;código&gt;_Tripo/, y en cada Carro_Vxx cambia el visual (carrocería, puertas, cofre, cristales y llantas por
    /// piezas de Blender) por el modelo Tripo, ya girado y a escala (se compara su silueta con la del visual de Blender que había para saber hacia dónde mira), con el material URP de siempre.
    /// Se conserva todo lo demás: Rigidbody, CarroViejo (daño, choque, luces, explosión), PuertasCarro (tolera bisagras vacías), RuedasVisualesCarro (los giros quedan vacíos), CarroEstacionado,
    /// asiento, cámara y punto de subida (robo). Las cajas de choque se ajustan a la malla y las anclas de la suspensión bajan a la altura de reposo.
    /// Todos los carros llevan LucesCarro (faros y calaveras de noche, igual que la patrulla buena). Las motos siguen sin ellas (T-660).
    /// Motos (V07, V08, C19, MT01, MT02): la malla de ManejoMoto vuelve a ser la original de Tripo (con llantas horneadas), se quitan las llantas RU02 y se rehace la malla sin caballete.
    /// Diagnostico: solo informe en Logs/t1644.txt (qué FBX usa cada carro, escala, piezas, giro elegido y por qué es esa la patrulla buena).
    /// Orden: Diagnostico (opcional), Aplicar y Diagnostico otra vez. Las escenas no se editan aquí.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CarrosTripoT1644.Aplicar (y .Diagnostico).
    /// </summary>
    public static class CarrosTripoT1644
    {
        public readonly struct Fuente
        {
            public readonly string Codigo;
            public readonly string RutaFbx;
            public readonly string RutaMaterial;
            public readonly bool Moto;

            public Fuente(string codigo, string rutaFbx, string rutaMaterial, bool moto)
            {
                Codigo = codigo;
                RutaFbx = rutaFbx;
                RutaMaterial = rutaMaterial;
                Moto = moto;
            }
        }

        private const string CarpetaArte = "Assets/Arte/Vehiculos/";
        private const string CarpetaPiezas = CarpetaArte + "Piezas/";

        private static string Nuevo(string c) => CarpetaArte + c + "_Tripo/" + c + "_Tripo.fbx";

        // El material de cada Tripo nuevo es el de la carrocería de Blender: mismas texturas (bytes iguales a las del Tripo) y el mismo UV.
        private static string MatPieza(string c) => CarpetaPiezas + c + "/" + c + "_Blender_Carroceria.mat";

        public static readonly Fuente[] Fuentes =
        {
            new Fuente("V01a", CarpetaArte + "V01a_CarroViejito/V01a_CarroViejito.fbx", CarpetaArte + "V01a_CarroViejito/V01a_CarroViejito.mat", false),
            new Fuente("V01b", CarpetaArte + "V01b_CarroPro/V01b_CarroPro.fbx", CarpetaArte + "V01b_CarroPro/V01b_CarroPro.mat", false),
            new Fuente("V02", CarpetaArte + "V02/V02.fbx", CarpetaArte + "V02/V02.mat", false),
            new Fuente("V03", CarpetaArte + "V03/V03.fbx", CarpetaArte + "V03/V03.mat", false),
            new Fuente("V04", CarpetaArte + "V04/V04.fbx", CarpetaArte + "V04/V04.mat", false),
            new Fuente("V05", CarpetaArte + "V05/V05.fbx", CarpetaArte + "V05/V05.mat", false),
            new Fuente("V06", CarpetaArte + "V06/V06.fbx", CarpetaArte + "V06/V06.mat", false),
            new Fuente("V09", CarpetaArte + "V09/V09.fbx", CarpetaArte + "V09/V09.mat", false),
            new Fuente("V12", Nuevo("V12"), MatPieza("V12"), false),
            new Fuente("V13", Nuevo("V13"), MatPieza("V13"), false),
            new Fuente("V14", Nuevo("V14"), MatPieza("V14"), false),
            new Fuente("V15", Nuevo("V15"), MatPieza("V15"), false),
            new Fuente("V16", Nuevo("V16"), MatPieza("V16"), false),
            new Fuente("V17", Nuevo("V17"), MatPieza("V17"), false),
            new Fuente("V18", Nuevo("V18"), MatPieza("V18"), false),
            new Fuente("V19", Nuevo("V19"), MatPieza("V19"), false),
            new Fuente("V20", Nuevo("V20"), MatPieza("V20"), false),
            new Fuente("V21", Nuevo("V21"), MatPieza("V21"), false),
            new Fuente("V22", Nuevo("V22"), MatPieza("V22"), false),
            new Fuente("V23", Nuevo("V23"), MatPieza("V23"), false),
            new Fuente("V24", Nuevo("V24"), MatPieza("V24"), false),
            new Fuente("V07", CarpetaArte + "V07/V07.fbx", CarpetaArte + "V07/V07.mat", true),
            new Fuente("V08", CarpetaArte + "V08/V08.fbx", CarpetaArte + "V08/V08.mat", true),
            new Fuente("C19", "Assets/Arte/Calle/C19_Moto/C19_Moto.fbx", "Assets/Arte/Calle/C19_Moto/C19_Moto.mat", true),
            new Fuente("MT01", Nuevo("MT01"), MatPieza("MT01"), true),
            new Fuente("MT02", Nuevo("MT02"), MatPieza("MT02"), true),
        };

        /// <summary>Códigos de los carros de cuatro ruedas que pasan al modelo de Tripo.</summary>
        public static IEnumerable<string> Carros => Fuentes.Where(f => !f.Moto).Select(f => f.Codigo);

        /// <summary>Códigos de las motos que pasan al modelo de Tripo.</summary>
        public static IEnumerable<string> Motos => Fuentes.Where(f => f.Moto).Select(f => f.Codigo);

        /// <summary>La patrulla buena de la foto: Patrulla.prefab (Trafico) con el cuerpo de Carro_PA01. No se toca.</summary>
        public const string PatrullaBuena = "PA01";

        public const string RutaPatrullaBuena = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        public const string NombreModelo = "ModeloTripo";
        public const string RutaInformeDiagnostico = "Logs/t1644.txt";
        public const string RutaInformeAplicar = "Logs/t1644-aplicar.txt";

        private const float FondoBajoCajas = 0.4f;
        private const float FraccionAltoCarroceria = 0.5f;
        private const float FraccionAltoCabina = 0.92f;
        private const float FraccionAnchoCabina = 0.8f;
        private const float FraccionLargoCabina = 0.5f;
        private const float SeparacionPuerta = 1f;
        private const float LargoMinimoAuto = 3.6f;
        private const float LargoMaximoAuto = 5.2f;
        private const float ProporcionCasiCuadrado = 0.75f;
        private const float PasoEnderezado = 0.5f;
        private const float ToleranciaEnderezado = 4f;
        private const float MejoraMinimaEnderezado = 0.02f;
        private const int Celdas = 32;
        private const int MaximoPuntos = 40000;
        private static readonly string[] CodigosGrandes = { "V16", "V17", "V18", "V19" };

        public static string RutaPrefab(string codigo) => CreadorCarrosT511.CarpetaPrefabs + "Carro_" + codigo + ".prefab";

        public static Fuente De(string codigo) => Fuentes.First(f => f.Codigo == codigo);

        /// <summary>Largo máximo que debe medir la caja de un carro (los camiones V17 y V18 son más largos que un auto).</summary>
        public static float LargoMaximoDe(string codigo) => codigo == "V17" || codigo == "V18" ? 12f : LargoMaximoAuto;

        // ------------------------------------------------------------------ Aplicar

        [MenuItem("Mecha Cyberpunk/T-1644/Aplicar carros Tripo")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            informe.AppendLine("== T-1644: carros y motos con el modelo original de Tripo (menos la patrulla buena " + PatrullaBuena + ")");
            foreach (Fuente f in Fuentes)
            {
                informe.AppendLine(ConfigurarImportador(f));
            }
            AssetDatabase.Refresh();
            foreach (Fuente f in Fuentes)
            {
                informe.AppendLine(f.Moto ? AplicarMoto(f) : AplicarCarro(f));
            }
            AssetDatabase.SaveAssets();
            // Las motos con caballete en el modelo de Tripo: la malla sin caballete se rehace con los creadores de siempre.
            // Preparar también reescribe los ConfigCarro de moto con los números de T-592; se guardan antes y se devuelven después (los calibró T-1488).
            Dictionary<ConfigCarro, string> configs = new Dictionary<ConfigCarro, string>();
            foreach (string codigo in CreadorMotosT592.Motos)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
                CarroViejo carro = prefab != null ? prefab.GetComponent<CarroViejo>() : null;
                ConfigCarro config = carro != null ? new SerializedObject(carro).FindProperty("config").objectReferenceValue as ConfigCarro : null;
                if (config != null && !configs.ContainsKey(config))
                {
                    configs[config] = EditorJsonUtility.ToJson(config);
                }
            }
            foreach (string codigo in CreadorMotosT592.Motos)
            {
                informe.AppendLine(CreadorMotosT592.Preparar(codigo));
            }
            foreach (KeyValuePair<ConfigCarro, string> par in configs)
            {
                EditorJsonUtility.FromJsonOverwrite(par.Value, par.Key);
                EditorUtility.SetDirty(par.Key);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInformeAplicar, informe.ToString());
            Debug.Log("[Vehiculos] T-1644:\n" + informe);
        }

        /// <summary>Sin materiales (el material es el .mat de arriba), sin animación ni cámaras ni luces, legible (se miden sus vértices). Solo toca los FBX copiados para T-1644.</summary>
        public static string ConfigurarImportador(Fuente f)
        {
            ModelImporter importador = AssetImporter.GetAtPath(f.RutaFbx) as ModelImporter;
            if (importador == null)
            {
                return f.Codigo + "\tFALTA el FBX " + f.RutaFbx;
            }
            if (!f.RutaFbx.Contains("_Tripo/"))
            {
                return f.Codigo + "\tFBX de Tripo que ya estaba en el proyecto: importador sin tocar";
            }
            bool cambio = false;
            if (importador.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importador.materialImportMode = ModelImporterMaterialImportMode.None;
                cambio = true;
            }
            if (importador.importAnimation || importador.importCameras || importador.importLights)
            {
                importador.importAnimation = false;
                importador.importCameras = false;
                importador.importLights = false;
                cambio = true;
            }
            if (!importador.isReadable)
            {
                importador.isReadable = true;
                cambio = true;
            }
            if (cambio)
            {
                importador.SaveAndReimport();
            }
            return f.Codigo + "\tFBX " + f.RutaFbx + (cambio ? " reimportado (sin materiales ni animación, legible)" : " ya configurado");
        }

        // ------------------------------------------------------------------ carros

        private static string AplicarCarro(Fuente f)
        {
            string ruta = RutaPrefab(f.Codigo);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(f.RutaFbx);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(f.RutaMaterial);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null || fbx == null || material == null)
            {
                return f.Codigo + "\tFALTA prefab, FBX o material (" + ruta + ", " + f.RutaFbx + ", " + f.RutaMaterial + ")";
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Plan plan = Planear(raiz, f, fbx);
                if (plan.Nube.Length == 0)
                {
                    return f.Codigo + "\tel FBX no tiene mallas";
                }
                Transform envoltorio = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
                if (envoltorio == null)
                {
                    envoltorio = new GameObject(CreadorCarrosPiezasT742.NombreVisual).transform;
                    envoltorio.SetParent(raiz.transform, false);
                    envoltorio.SetSiblingIndex(0);
                }
                // Se quita todo el visual de Blender (carrocería, puertas, cofre, cristales, fijas y bisagras) y las llantas por piezas.
                for (int i = envoltorio.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(envoltorio.GetChild(i).gameObject);
                }
                envoltorio.localPosition = Vector3.zero;
                envoltorio.localRotation = Quaternion.identity;
                envoltorio.localScale = Vector3.one;
                foreach (Transform ancla in Anclas(raiz))
                {
                    Transform modelo = ancla.Find("Direccion/Giro/Modelo");
                    if (modelo != null)
                    {
                        Object.DestroyImmediate(modelo.gameObject);
                    }
                }

                GameObject contenedor = new GameObject(NombreModelo);
                contenedor.transform.SetParent(envoltorio, false);
                contenedor.transform.localRotation = Quaternion.Euler(0f, plan.Yaw, 0f);
                contenedor.transform.localScale = Vector3.one * plan.Escala;
                contenedor.transform.localPosition = plan.Posicion;
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(fbx, contenedor.transform);
                instancia.name = f.Codigo + "_Tripo";
                foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                }

                Bounds caja = plan.CajaFinal;
                float largo = caja.size.z;
                float ancho = caja.size.x;
                float alto = caja.size.y;
                string cajas = AjustarCajas(raiz, largo, ancho, alto);
                string suspension = BajarAnclas(raiz);
                SoltarBisagras(raiz);
                MoverPuntoSubida(raiz, ancho);
                bool luces = AsegurarLuces(raiz);
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                return string.Format("{0}\tOK  FBX {1}  giro {2:F0} grados  escala {3:F3}  largo {4:F2} ancho {5:F2} alto {6:F2}  puntaje {7:F3}  {8}  {9}  luces {10}",
                    f.Codigo, f.RutaFbx, plan.Yaw, plan.Escala, largo, ancho, alto, plan.Puntaje, cajas, suspension, luces ? "nuevas" : "ya estaban");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // Las anclas de las llantas: las cuatro de siempre y las de los ejes de más (V18 trae Rueda_T1I, Rueda_T3D...).
        private static List<Transform> Anclas(GameObject raiz)
        {
            List<Transform> anclas = new List<Transform>();
            foreach (Transform hijo in raiz.transform)
            {
                if (hijo.name.StartsWith("Rueda_"))
                {
                    anclas.Add(hijo);
                }
            }
            return anclas;
        }

        // Las cajas de choque a la medida de la malla (la misma receta de T-511).
        private static string AjustarCajas(GameObject raiz, float largo, float ancho, float alto)
        {
            Transform carroceria = raiz.transform.Find("Carroceria");
            Transform cabina = raiz.transform.Find("Cabina");
            float cima = alto * FraccionAltoCarroceria;
            if (carroceria != null)
            {
                PonerCaja(carroceria, FondoBajoCajas, cima, ancho * 0.96f, largo * 0.98f, 0f);
                Renderer r = carroceria.GetComponent<Renderer>();
                if (r != null)
                {
                    r.enabled = false;
                }
            }
            if (cabina != null)
            {
                PonerCaja(cabina, cima, alto * FraccionAltoCabina, ancho * FraccionAnchoCabina, largo * FraccionLargoCabina, -largo * 0.05f);
                Renderer r = cabina.GetComponent<Renderer>();
                if (r != null)
                {
                    r.enabled = false;
                }
            }
            return "cajas " + (carroceria != null ? "si" : "FALTA Carroceria") + "/" + (cabina != null ? "si" : "FALTA Cabina");
        }

        private static void PonerCaja(Transform t, float desde, float hasta, float ancho, float largo, float z)
        {
            t.localPosition = new Vector3(0f, (desde + hasta) * 0.5f, z);
            t.localScale = new Vector3(ancho, Mathf.Max(0.1f, hasta - desde), largo);
        }

        // La llanta visible de la malla toca el piso con el resorte en reposo cuando el ancla está a esta altura (el fondo de la malla va en y = 0).
        private static string BajarAnclas(GameObject raiz)
        {
            ConfigCarro config = CarrosPAT1510.ConfigDe(raiz);
            if (config == null)
            {
                return "anclas sin config";
            }
            // La altura es en metros del mundo; V16 a V19 tienen la raíz escalada (0.7 a 0.8), así que en local se divide entre esa escala.
            float altura = CarrosPAT1510.AlturaAnclaEnReposo(config) / Mathf.Max(0.0001f, raiz.transform.localScale.y);
            foreach (Transform ancla in Anclas(raiz))
            {
                ancla.localPosition = new Vector3(ancla.localPosition.x, altura, ancla.localPosition.z);
            }
            return "anclas y " + altura.ToString("F3");
        }

        // Ya no hay puertas ni cofre: PuertasCarro tolera bisagras vacías, pero se dejan sin referencias rotas.
        private static void SoltarBisagras(GameObject raiz)
        {
            PuertasCarro puertas = raiz.GetComponent<PuertasCarro>();
            if (puertas == null)
            {
                return;
            }
            SerializedObject so = new SerializedObject(puertas);
            foreach (string propiedad in new[] { "bisagraPuertaIzquierda", "bisagraPuertaDerecha", "bisagraCofre" })
            {
                SerializedProperty p = so.FindProperty(propiedad);
                if (p != null)
                {
                    p.objectReferenceValue = null;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void MoverPuntoSubida(GameObject raiz, float ancho)
        {
            CarroViejo carro = raiz.GetComponent<CarroViejo>();
            if (carro == null)
            {
                return;
            }
            Transform punto = new SerializedObject(carro).FindProperty("puntoSubida").objectReferenceValue as Transform;
            if (punto != null)
            {
                punto.localPosition = new Vector3(-(ancho * 0.5f + SeparacionPuerta), punto.localPosition.y, punto.localPosition.z);
            }
        }

        // LucesCarro explícito en el prefab (faros delanteros y calaveras de noche, como la patrulla buena). true si se agregó.
        private static bool AsegurarLuces(GameObject raiz)
        {
            if (raiz.GetComponent<LucesCarro>() != null || raiz.GetComponent<ManejoMoto>() != null || raiz.GetComponent<CarroViejo>() == null)
            {
                return false;
            }
            raiz.AddComponent<LucesCarro>();
            return true;
        }

        // ------------------------------------------------------------------ motos

        private static string AplicarMoto(Fuente f)
        {
            string ruta = RutaPrefab(f.Codigo);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(f.RutaFbx);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(f.RutaMaterial);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null || fbx == null || material == null)
            {
                return f.Codigo + "\tFALTA prefab, FBX o material (" + ruta + ", " + f.RutaFbx + ", " + f.RutaMaterial + ")";
            }
            MeshFilter nodo = fbx.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh != null);
            if (nodo == null)
            {
                return f.Codigo + "\tel FBX no tiene mallas";
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Transform envoltorio = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
                Transform visual = envoltorio != null ? envoltorio.Find("Visual") : null;
                ManejoMoto manejo = raiz.GetComponent<ManejoMoto>();
                if (visual == null || manejo == null || visual.GetComponent<MeshFilter>() == null)
                {
                    return f.Codigo + "\tla moto no tiene Visual_T511/Visual o ManejoMoto";
                }
                Plan plan = Planear(raiz, f, fbx);
                MeshFilter filtro = visual.GetComponent<MeshFilter>();
                MeshRenderer dibujo = visual.GetComponent<MeshRenderer>();
                // La malla del FBX (en el marco del FBX) se lleva al marco del envoltorio: giro, escala y posición del plan.
                Matrix4x4 aEnvoltorio = Matrix4x4.TRS(plan.Posicion, Quaternion.Euler(0f, plan.Yaw, 0f), Vector3.one * plan.Escala) * nodo.transform.localToWorldMatrix;
                visual.localPosition = aEnvoltorio.GetPosition();
                visual.localRotation = aEnvoltorio.rotation;
                visual.localScale = aEnvoltorio.lossyScale;
                filtro.sharedMesh = nodo.sharedMesh;
                if (dibujo != null)
                {
                    dibujo.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, nodo.sharedMesh.subMeshCount)).ToArray();
                }

                // Las llantas RU02 (que giraban) ya no: las llantas vienen en la malla.
                Transform llantas = envoltorio.Find(CreadorMotosPiezasT743.NombreRuedas);
                if (llantas != null)
                {
                    Object.DestroyImmediate(llantas.gameObject);
                }
                RuedasVisualesCarro ruedas = raiz.GetComponent<RuedasVisualesCarro>();
                if (ruedas != null)
                {
                    SerializedObject sr = new SerializedObject(ruedas);
                    sr.FindProperty("giro").arraySize = 0;
                    sr.FindProperty("direccionDelantera").arraySize = 0;
                    sr.ApplyModifiedPropertiesWithoutUndo();
                }
                SerializedObject sm = new SerializedObject(manejo);
                sm.FindProperty("filtroModelo").objectReferenceValue = filtro;
                sm.FindProperty("mallaCompleta").objectReferenceValue = nodo.sharedMesh;
                sm.FindProperty("mallaSinCaballete").objectReferenceValue = null;
                sm.FindProperty("bajarAlOcultar").floatValue = 0f;
                sm.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                Bounds caja = plan.CajaFinal;
                return string.Format("{0}\tOK moto  FBX {1}  giro {2:F0} grados  escala {3:F3}  largo {4:F2} ancho {5:F2} alto {6:F2}  puntaje {7:F3}",
                    f.Codigo, f.RutaFbx, plan.Yaw, plan.Escala, Mathf.Max(caja.size.x, caja.size.z), Mathf.Min(caja.size.x, caja.size.z), caja.size.y, plan.Puntaje);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // ------------------------------------------------------------------ plan: giro, escala y posición

        /// <summary>Cómo se coloca el modelo de Tripo: giro sobre Y (múltiplo de 90), escala, posición del contenedor y la caja que queda.</summary>
        public struct Plan
        {
            public float Yaw;
            public float Escala;
            public Vector3 Posicion;
            public Bounds CajaFinal;
            public float Puntaje;
            public float LargoReferencia;
            public bool HayReferencia;
            public Vector3[] Nube;
            public string Nota;
        }

        /// <summary>
        /// Calcula el plan sin tocar nada. La referencia es el visual que el prefab tiene ahora (las piezas de Blender y sus llantas; en las motos, la malla y las llantas RU02); el modelo de Tripo
        /// es el mismo auto, así que de los cuatro giros se elige el que más se parece a esa silueta (altura y ancho a lo largo del carro). Si el prefab ya lleva el modelo de Tripo
        /// (segunda corrida) se conserva su giro y su escala.
        /// </summary>
        public static Plan Planear(GameObject raiz, Fuente f, GameObject fbx)
        {
            Plan plan = new Plan { Escala = 1f, Nube = new Vector3[0] };
            List<Vector3> candidata = Nube(fbx.GetComponentsInChildren<MeshFilter>(true), null);
            plan.Nube = candidata.ToArray();
            if (candidata.Count == 0)
            {
                return plan;
            }

            Transform envoltorio = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
            Transform previo = envoltorio != null && !f.Moto ? envoltorio.Find(NombreModelo) : null;
            Vector3[] referencia = new Vector3[0];
            if (previo != null)
            {
                plan.Yaw = previo.localEulerAngles.y;
                plan.Escala = previo.localScale.x;
                plan.Nota = "segunda corrida: conserva giro y escala";
            }
            else
            {
                referencia = ReferenciaDe(raiz, f).ToArray();
                plan.HayReferencia = referencia.Length > 0;
            }

            Bounds cajaReferencia = referencia.Length > 0 ? Limites(referencia) : new Bounds(Vector3.zero, Vector3.zero);
            if (previo == null)
            {
                float mejor = float.MaxValue;
                float mejorYaw = 0f;
                if (referencia.Length > 0)
                {
                    int eje = cajaReferencia.size.z >= cajaReferencia.size.x ? 2 : 0;
                    Perfil(referencia, eje, out float[] alturaRef, out float[] anchoRef);
                    float enderezado = f.Moto ? 0f : GiroEnderezado(plan.Nube);
                    foreach (float cuarto in new[] { 0f, 90f, 180f, 270f })
                    {
                        float yaw = enderezado + cuarto;
                        Vector3[] girada = Girar(plan.Nube, yaw);
                        Bounds b = Limites(girada);
                        int ejeCandidata = b.size.z >= b.size.x ? 2 : 0;
                        // Casi cuadrado en planta (V02, V03 llevan carga a los lados): cualquiera de los dos ejes puede ser el largo, decide la silueta.
                        bool casiCuadrado = Mathf.Min(b.size.x, b.size.z) >= Mathf.Max(b.size.x, b.size.z) * ProporcionCasiCuadrado;
                        if (ejeCandidata != eje && !casiCuadrado)
                        {
                            continue;
                        }
                        Perfil(girada, eje, out float[] alturaCand, out float[] anchoCand);
                        float puntaje = 0f;
                        for (int i = 0; i < Celdas; i++)
                        {
                            puntaje += Mathf.Abs(alturaRef[i] - alturaCand[i]) + Mathf.Abs(anchoRef[i] - anchoCand[i]);
                        }
                        puntaje /= 2f * Celdas;
                        if (puntaje < mejor)
                        {
                            mejor = puntaje;
                            mejorYaw = yaw;
                        }
                    }
                    plan.Nota = (mejor < float.MaxValue ? "giro por silueta" : "sin giro que alinee el largo: giro 0") + ", enderezado " + enderezado.ToString("F1");
                }
                else
                {
                    Bounds sinGiro = Limites(plan.Nube);
                    mejorYaw = f.Moto ? 0f : (sinGiro.size.x > sinGiro.size.z ? 90f : 0f) + GiroEnderezado(plan.Nube);
                    plan.Nota = "SIN visual de referencia: largo hacia Z y escala por el largo de la tabla";
                }
                plan.Yaw = mejorYaw;
                plan.Puntaje = mejor < float.MaxValue ? mejor : -1f;
            }

            Vector3[] colocada = Girar(plan.Nube, plan.Yaw);
            Bounds propia = Limites(colocada);
            int ejeLargo = 2;
            if (f.Moto)
            {
                Bounds guia = referencia.Length > 0 ? cajaReferencia : propia;
                ejeLargo = guia.size.z >= guia.size.x ? 2 : 0;
            }
            float largoPropio = ejeLargo == 2 ? propia.size.z : propia.size.x;
            if (previo == null)
            {
                float largoReferencia = referencia.Length > 0 ? (ejeLargo == 2 ? cajaReferencia.size.z : cajaReferencia.size.x) : (f.Moto ? 2f : 4.4f);
                if (!f.Moto)
                {
                    largoReferencia = CodigosGrandes.Contains(f.Codigo) ? largoReferencia : Mathf.Clamp(largoReferencia, LargoMinimoAuto, LargoMaximoAuto);
                }
                plan.LargoReferencia = largoReferencia;
                plan.Escala = largoPropio > 0.0001f ? largoReferencia / largoPropio : 1f;
            }
            else
            {
                plan.LargoReferencia = largoPropio * plan.Escala;
            }

            Vector3 centro = propia.center * plan.Escala;
            float fondo = propia.min.y * plan.Escala;
            if (f.Moto && referencia.Length > 0)
            {
                plan.Posicion = new Vector3(cajaReferencia.center.x - centro.x, cajaReferencia.min.y - fondo, cajaReferencia.center.z - centro.z);
            }
            else
            {
                plan.Posicion = new Vector3(-centro.x, -fondo, -centro.z);
            }
            Bounds final = new Bounds(plan.Posicion + centro, propia.size * plan.Escala);
            plan.CajaFinal = final;
            return plan;
        }

        // Los vértices del visual actual en el marco del prefab (o del envoltorio, en las motos): visual de Blender, cristales, puertas, llantas.
        private static List<Vector3> ReferenciaDe(GameObject raiz, Fuente f)
        {
            Transform envoltorio = raiz.transform.Find(CreadorCarrosPiezasT742.NombreVisual);
            List<MeshFilter> filtros = new List<MeshFilter>();
            if (envoltorio != null)
            {
                filtros.AddRange(envoltorio.GetComponentsInChildren<MeshFilter>(true));
            }
            if (!f.Moto)
            {
                foreach (Transform ancla in Anclas(raiz))
                {
                    Transform modelo = ancla.Find("Direccion/Giro/Modelo");
                    if (modelo != null)
                    {
                        filtros.AddRange(modelo.GetComponentsInChildren<MeshFilter>(true));
                    }
                }
            }
            Transform marco = f.Moto && envoltorio != null ? envoltorio : raiz.transform;
            return Nube(filtros, marco);
        }

        // Vértices de varias mallas. Con marco: en el espacio local del marco. Sin marco: en el espacio de cada raíz del FBX (su matriz de mundo).
        private static List<Vector3> Nube(IEnumerable<MeshFilter> filtros, Transform marco)
        {
            List<MeshFilter> lista = filtros.Where(m => m != null && m.sharedMesh != null).ToList();
            long total = lista.Sum(m => (long)m.sharedMesh.vertexCount);
            int paso = (int)System.Math.Max(1, total / MaximoPuntos);
            List<Vector3> puntos = new List<Vector3>();
            foreach (MeshFilter m in lista)
            {
                Matrix4x4 matriz = marco != null ? marco.worldToLocalMatrix * m.transform.localToWorldMatrix : m.transform.localToWorldMatrix;
                Vector3[] v = m.sharedMesh.vertices;
                for (int i = 0; i < v.Length; i += paso)
                {
                    puntos.Add(matriz.MultiplyPoint3x4(v[i]));
                }
            }
            return puntos;
        }

        // Algunos FBX de Tripo traen el carro en diagonal (V02 a ~27°, V03 a ~41°): el giro de 0 a 90° con la menor caja en planta lo endereza. Cerca de 0 o 90 se deja en 0.
        public static float GiroEnderezado(Vector3[] puntos)
        {
            float mejorArea = float.MaxValue;
            float mejor = 0f;
            for (float a = 0f; a < 90f; a += PasoEnderezado)
            {
                Quaternion q = Quaternion.Euler(0f, a, 0f);
                float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
                foreach (Vector3 p in puntos)
                {
                    Vector3 g = q * p;
                    minX = Mathf.Min(minX, g.x);
                    maxX = Mathf.Max(maxX, g.x);
                    minZ = Mathf.Min(minZ, g.z);
                    maxZ = Mathf.Max(maxZ, g.z);
                }
                float area = (maxX - minX) * (maxZ - minZ);
                if (area < mejorArea * (1f - MejoraMinimaEnderezado))
                {
                    mejorArea = area;
                    mejor = a;
                }
            }
            return mejor <= ToleranciaEnderezado || mejor >= 90f - ToleranciaEnderezado ? 0f : mejor;
        }

        private static Vector3[] Girar(Vector3[] puntos, float yaw)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            Vector3[] salida = new Vector3[puntos.Length];
            for (int i = 0; i < puntos.Length; i++)
            {
                salida[i] = q * puntos[i];
            }
            return salida;
        }

        private static Bounds Limites(Vector3[] puntos)
        {
            Bounds b = new Bounds(puntos[0], Vector3.zero);
            for (int i = 1; i < puntos.Length; i++)
            {
                b.Encapsulate(puntos[i]);
            }
            return b;
        }

        // Silueta a lo largo del eje (0 = X, 2 = Z): por celda, la altura máxima (0 a 1 del alto) y el medio ancho máximo (0 a 1 del ancho).
        private static void Perfil(Vector3[] puntos, int eje, out float[] altura, out float[] ancho)
        {
            Bounds b = Limites(puntos);
            int transversal = eje == 2 ? 0 : 2;
            float minEje = b.min[eje];
            float largo = Mathf.Max(0.0001f, b.size[eje]);
            float alto = Mathf.Max(0.0001f, b.size.y);
            float medioAncho = Mathf.Max(0.0001f, b.size[transversal] * 0.5f);
            altura = new float[Celdas];
            ancho = new float[Celdas];
            foreach (Vector3 p in puntos)
            {
                int celda = Mathf.Clamp((int)((p[eje] - minEje) / largo * Celdas), 0, Celdas - 1);
                altura[celda] = Mathf.Max(altura[celda], (p.y - b.min.y) / alto);
                ancho[celda] = Mathf.Max(ancho[celda], Mathf.Abs(p[transversal] - b.center[transversal]) / medioAncho);
            }
        }

        // ------------------------------------------------------------------ diagnóstico

        [MenuItem("Mecha Cyberpunk/T-1644/Diagnostico carros Tripo")]
        public static void Diagnostico()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            informe.AppendLine("== T-1644: diagnóstico (no cambia nada)");
            informe.AppendLine();
            informe.AppendLine("== Patrulla buena (docs/capturas/patrulla-buena-22nov.png)");
            informe.AppendLine(DescribirPatrullaBuena());
            informe.AppendLine();
            informe.AppendLine("== Carros y motos: FBX de Tripo, plan de giro y escala, y lo que el prefab tiene ahora");
            foreach (Fuente f in Fuentes)
            {
                informe.AppendLine(DiagnosticoDe(f));
            }
            informe.AppendLine();
            informe.AppendLine("== Sin tocar a propósito");
            foreach (string codigo in new[] { "PA01", "PA02", "V10", "V11" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
                informe.AppendLine(codigo + "\t" + (prefab == null ? "no hay prefab" : "se queda como está (" + Piezas(prefab) + ")"));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInformeDiagnostico, informe.ToString());
            Debug.Log("[Vehiculos] T-1644 diagnóstico:\n" + informe);
        }

        private static string DescribirPatrullaBuena()
        {
            GameObject patrulla = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrullaBuena);
            GameObject pa01 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(PatrullaBuena));
            StringBuilder s = new StringBuilder();
            s.AppendLine("Es la patrulla sedán PA01: " + RutaPatrullaBuena + " (Trafico/Persecucion) usa el cuerpo de Carro_PA01 (T-1510): sedán azul oscuro con franjas turquesa, rejilla con defensa, torreta azul y roja.");
            s.AppendLine("Coincide con la foto (carrocería azul marino, franja diagonal turquesa en la puerta, barra azul/roja, cofre y defensa) y con imageassets/blender/_REVISION_TONY/capturas/V_PA01.png; V05 es blanca con escudo y banda verde.");
            s.AppendLine("Patrulla.prefab " + (patrulla != null ? "existe" : "NO existe") + "; Carro_PA01 " + (pa01 != null ? "existe (" + Piezas(pa01) + ")" : "NO existe") + ".");
            s.Append("Por eso PA01 no se toca (puertas que abren, torreta, luces); PA02 y V11 tampoco (otras patrullas). LucesCarro (faros y calaveras) y el daño/choque los trae CarroViejo en todos: misma masa 1100, mismos componentes.");
            return s.ToString();
        }

        private static string DiagnosticoDe(Fuente f)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(f.RutaFbx);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(f.Codigo));
            Material material = AssetDatabase.LoadAssetAtPath<Material>(f.RutaMaterial);
            StringBuilder s = new StringBuilder(f.Codigo + "\t" + (f.Moto ? "moto" : "carro") + "\tFBX " + f.RutaFbx + (fbx != null ? "" : "  (FALTA)") + "\tmaterial " + f.RutaMaterial + (material != null ? "" : "  (FALTA)"));
            if (fbx == null || prefab == null)
            {
                return s + "\n\tprefab " + (prefab != null ? "si" : "FALTA");
            }
            Vector3[] propia = Nube(fbx.GetComponentsInChildren<MeshFilter>(true), null).ToArray();
            if (propia.Length > 0)
            {
                Bounds b = Limites(propia);
                s.Append(string.Format("\n\tFBX medido en el marco del FBX: {0:F2} x {1:F2} x {2:F2} (x, y, z)", b.size.x, b.size.y, b.size.z));
                foreach (MeshFilter m in fbx.GetComponentsInChildren<MeshFilter>(true).Where(m => m.sharedMesh != null))
                {
                    Vector3[] nm = Nube(new[] { m }, null).ToArray();
                    Bounds bm = Limites(nm);
                    s.Append(string.Format("\n\t  malla {0}: {1:F2} x {2:F2} x {3:F2}, centro {4:F2},{5:F2},{6:F2}, {7} vértices", m.name, bm.size.x, bm.size.y, bm.size.z, bm.center.x, bm.center.y, bm.center.z, m.sharedMesh.vertexCount));
                }
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab(f.Codigo));
            try
            {
                s.Append("\n\tprefab ahora: " + Piezas(raiz));
                Plan plan = Planear(raiz, f, fbx);
                s.Append(string.Format("\n\tplan: giro {0:F0}, escala {1:F3}, largo {2:F2}, caja final {3:F2} x {4:F2} x {5:F2}, puntaje de silueta {6:F3} ({7}; referencia: {8})",
                    plan.Yaw, plan.Escala, plan.LargoReferencia, plan.CajaFinal.size.x, plan.CajaFinal.size.y, plan.CajaFinal.size.z, plan.Puntaje, plan.Nota, plan.HayReferencia ? "si" : "no"));
                s.Append("\n\tLucesCarro " + (raiz.GetComponent<LucesCarro>() != null ? "en el prefab" : "se agrega en Aplicar (el juego las agrega solo en Awake)")
                    + ", Rigidbody " + (raiz.GetComponent<Rigidbody>() != null ? "si" : "NO") + ", CarroViejo " + (raiz.GetComponent<CarroViejo>() != null ? "si" : "NO"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            return s.ToString();
        }

        /// <summary>Resumen de qué piezas lleva un prefab: puertas, cofre, cristales, llantas por piezas y modelo de Tripo.</summary>
        public static string Piezas(GameObject raiz)
        {
            Transform[] todos = raiz.GetComponentsInChildren<Transform>(true);
            int puertas = todos.Count(t => t.name == "Puerta_D" || t.name == "Puerta_I");
            int cofre = todos.Count(t => t.name == "Cofre");
            int cristales = todos.Count(t => t.name.StartsWith("Cristal_"));
            int llantas = todos.Count(t => t.name == "Modelo" && t.parent != null && t.parent.name == "Giro");
            bool tripo = todos.Any(t => t.name == NombreModelo);
            return string.Format("puertas {0}, cofre {1}, cristales {2}, llantas por piezas {3}, modelo Tripo {4}", puertas, cofre, cristales, llantas, tripo ? "si" : "no");
        }
    }
}

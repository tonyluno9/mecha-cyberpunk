using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1350 (orquestación de las zonas nuevas): corre en una sola sesión de Unity los <c>Aplicar</c> y los <c>Fotos</c> de las zonas
    /// (por reflexión, para tolerar zonas que aún no están). Batchmode, por ejemplo:
    /// -executeMethod MechaCyberpunk.Arte.Editor.CorrerZonasT1350.Correr -zonas T1358Orillas.Aplicar,T1354Malecon.Aplicar,T1354Malecon.Fotos
    /// </summary>
    public static class CorrerZonasT1350
    {
        private const string Prefijo = "MechaCyberpunk.Arte.Editor.Colocar";

        /// <summary>Cuenta las Light activas por raíz y tipo (Logs/luces-zonas.txt). Batchmode: -executeMethod ...CorrerZonasT1350.ContarLuces</summary>
        public static void ContarLuces()
        {
            ColocarS2.Abrir();
            Light[] luces = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            System.Text.StringBuilder sb = new System.Text.StringBuilder("Total " + luces.Length + System.Environment.NewLine);
            foreach (IGrouping<string, Light> g in luces.GroupBy(l => l.transform.root.name + " " + l.type).OrderByDescending(g => g.Count()))
            {
                sb.AppendLine(g.Key + ": " + g.Count());
            }
            System.IO.File.WriteAllText("Logs/luces-zonas.txt", sb.ToString());
        }

        /// <summary>
        /// Raíces de las zonas nuevas y separación mínima (m) entre luces reales que se conservan: spots (postes, faroles, reflectores) y
        /// puntuales (neones, focos). Con todas las luces de los kits la escena pasaba de 1400 Light y el URP ya no podía dibujar (búfer de luces).
        /// </summary>
        private static readonly string[] RaicesZonas = { "T1354_Malecon", "T1355_Cerro", "T1356_Autopista", "T1357_Puerto" };
        private const float SeparacionSpot = 10f;
        private const float SeparacionPunto = 20f;

        /// <summary>Quita las Light de más en las zonas nuevas (lo emisivo se queda). Idempotente. Se corre después de los Aplicar.</summary>
        public static void PodarLuces()
        {
            ColocarS2.Abrir();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string nombre in RaicesZonas)
            {
                Transform raiz = ColocarS2.Buscar(nombre);
                if (raiz == null)
                {
                    continue;
                }
                Light[] luces = raiz.GetComponentsInChildren<Light>(true);
                List<Vector3> spots = new List<Vector3>();
                List<Vector3> puntos = new List<Vector3>();
                int quitadas = 0;
                // Primero las más fuertes: esas son las que se ven en el piso.
                foreach (Light l in luces.OrderByDescending(l => l.intensity * l.range))
                {
                    bool esSpot = l.type == LightType.Spot;
                    List<Vector3> puestas = esSpot ? spots : puntos;
                    float sep = esSpot ? SeparacionSpot : SeparacionPunto;
                    Vector3 p = l.transform.position;
                    if (puestas.Any(q => (q - p).sqrMagnitude < sep * sep))
                    {
                        // El dato de luz de URP depende de la Light: se quita primero.
                        foreach (Component extra in l.GetComponents<Component>().Where(c => c != null && c.GetType().Name == "UniversalAdditionalLightData"))
                        {
                            UnityEngine.Object.DestroyImmediate(extra);
                        }
                        UnityEngine.Object.DestroyImmediate(l);
                        quitadas++;
                        continue;
                    }
                    puestas.Add(p);
                }
                sb.AppendLine(nombre + ": " + luces.Length + " luces, quedan " + (spots.Count + puntos.Count) + " (spots " + spots.Count + ", puntuales " + puntos.Count + "), quitadas " + quitadas);
            }
            ColocarS2.Informe.Clear();
            ColocarS2.Informe.Append(sb);
            ColocarS2.Guardar("t1350-podar-luces");
        }

        private const string Texturas = "Assets/Arte/ZonasModulos/Texturas/";

        /// <summary>
        /// Prefab de una pieza modular de Blender. Primero el de la sesión B (T-1346, Assets/Arte/Zonas/Modulos, con texturas); las escaleras
        /// (CE01, ML02) usan el de T-1353 (Assets/Arte/ZonasModulos/Prefabs) porque llevan la rampa invisible para subirlas caminando.
        /// </summary>
        public static string RutaModulo(string nombre)
        {
            string deB = "Assets/Arte/Zonas/Modulos/" + nombre + ".prefab";
            bool escalera = nombre.StartsWith("CE01") || nombre.StartsWith("ML02_Escalera");
            // AU0x: el material PinturaBlanca de T-1346 (rayas) no trae textura todavía (T-506); se usan los de T-1353 hasta que la tenga.
            bool autopista = nombre.StartsWith("AU0");
            if (!escalera && !autopista && UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(deB) != null)
            {
                return deB;
            }
            return "Assets/Arte/ZonasModulos/Prefabs/" + nombre + ".prefab";
        }

        /// <summary>
        /// Material (por nombre) → textura que se le pone si no tiene ninguna. Las piezas de Blender todavía vienen sin textura (el chat de Blender
        /// las está texturizando: al reimportar con textura, el importador de T-1353 la respeta); mientras, concreto, asfalto, arena, lámina,
        /// madera y metal del proyecto, para que no quede greybox liso (T-506).
        /// </summary>
        private static readonly (string material, string textura)[] TexturasDeMaterial =
        {
            ("ZM_Concreto", Texturas + "T05_ConcretoCerro.png"),
            ("ZM_ConcretoClaro", Texturas + "T05_ConcretoCerro.png"),
            ("ZM_Roca", Texturas + "T05_ConcretoCerro.png"),
            ("ZM_Fondo", Texturas + "T05_ConcretoCerro.png"),
            ("ZM_Asfalto", Texturas + "T03_Asfalto.png"),
            ("ZM_Madera", "Assets/Arte/Exteriores/Texturas/T506_Madera.png"),
            ("ZM_MaderaOscura", "Assets/Arte/Exteriores/Texturas/T506_Madera.png"),
            ("ZM_Metal", "Assets/Arte/Exteriores/Texturas/T506_Metal.png"),
            ("ZM_Hierro", "Assets/Arte/Exteriores/Texturas/T506_Metal.png"),
            ("ZM_PinturaBlanca", "Assets/Arte/Exteriores/Texturas/T506_Metal.png"),
            ("T1354_Arena", Texturas + "T04_Arena.png"),
            ("T1354_ArenaClara", Texturas + "T04_Arena.png"),
            ("T1354_FondoMar", Texturas + "T04_Arena.png"),
            ("Cerro_Terreno", Texturas + "T05_ConcretoCerro.png"),
            ("T1356_Roca", Texturas + "T05_ConcretoCerro.png"),
            ("T1356_Tierra", Texturas + "T05_ConcretoCerro.png"),
        };

        /// <summary>Valores de los postes de la ciudad (T-1331): las pruebas de luz los piden iguales en todos los postes.</summary>
        private const float IntensidadPoste = 140f;
        private const float AlcancePoste = 16f;
        private const float ConoPoste = 100f;

        /// <summary>
        /// Arreglos comunes de las zonas nuevas, después de todos los Aplicar: postes con la luz de la ciudad, barriles y tambos explosivos con
        /// collider (T-1317), materiales lisos con textura (T-506), y los arreglos de piezas flotantes y encimadas de toda la ciudad.
        /// </summary>
        public static void ArreglarZonas()
        {
            ColocarS2.Abrir();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int postes = 0;
            int barriles = 0;
            foreach (string nombre in RaicesZonas)
            {
                Transform raiz = ColocarS2.Buscar(nombre);
                if (raiz == null)
                {
                    continue;
                }
                foreach (Light l in raiz.GetComponentsInChildren<Light>(true).Where(ColocarS2oLuz.EsPoste))
                {
                    l.intensity = IntensidadPoste;
                    l.range = AlcancePoste;
                    l.spotAngle = ConoPoste;
                    postes++;
                }
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(ColocarT1317Ajustes.EsBarrilDeCiudad).ToList())
                {
                    if (t.GetComponentInChildren<Collider>(true) == null)
                    {
                        Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
                        if (rs.Length > 0)
                        {
                            Bounds b = rs[0].bounds;
                            foreach (Renderer r in rs)
                            {
                                b.Encapsulate(r.bounds);
                            }
                            BoxCollider c = t.gameObject.AddComponent<BoxCollider>();
                            c.center = t.InverseTransformPoint(b.center);
                            Vector3 s = t.lossyScale;
                            c.size = new Vector3(b.size.x / Mathf.Max(0.001f, Mathf.Abs(s.x)), b.size.y / Mathf.Max(0.001f, Mathf.Abs(s.y)), b.size.z / Mathf.Max(0.001f, Mathf.Abs(s.z)));
                        }
                    }
                    if (t.GetComponent<MechaCyberpunk.Vehiculos.Efectos.BarrilExplosivo>() == null && t.GetComponentInParent<MechaCyberpunk.Vehiculos.Efectos.BarrilExplosivo>() == null)
                    {
                        t.gameObject.AddComponent<MechaCyberpunk.Vehiculos.Efectos.BarrilExplosivo>();
                        barriles++;
                    }
                }
            }
            int texturas = 0;
            // El contenedor (se tiñe en el Puerto) lleva lámina clara: sin textura cuenta como greybox (T-506).
            Material contenedor = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/ZonasModulos/Materiales/ZM_Contenedor.mat");
            Texture2D lamina = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arte/Exteriores/Texturas/T506_Lamina.png");
            if (contenedor != null && lamina != null && contenedor.GetTexture("_BaseMap") == null)
            {
                contenedor.SetTexture("_BaseMap", lamina);
                UnityEditor.EditorUtility.SetDirty(contenedor);
            }
            foreach ((string material, string textura) in TexturasDeMaterial)
            {
                Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(textura);
                foreach (string guid in UnityEditor.AssetDatabase.FindAssets(material + " t:Material"))
                {
                    string ruta = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    Material m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(ruta);
                    if (m == null || m.name != material || tex == null || !m.HasProperty("_BaseMap") || m.GetTexture("_BaseMap") != null)
                    {
                        continue;
                    }
                    m.SetTexture("_BaseMap", tex);
                    m.SetTextureScale("_BaseMap", Vector2.one);
                    UnityEditor.EditorUtility.SetDirty(m);
                    texturas++;
                }
            }
            // Lo generado por los scripts de zona (losas, túnel, roca, piso) con material Lit liso: textura según el nombre del material.
            foreach (string nombre in RaicesZonas)
            {
                Transform raiz = ColocarS2.Buscar(nombre);
                if (raiz == null)
                {
                    continue;
                }
                foreach (Material m in raiz.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct())
                {
                    // Solo materiales propios de las zonas nuevas: los compartidos con los barrios (S2c_*, kit) no se tocan.
                    bool propio = (m.name.StartsWith("T135") || m.name.StartsWith("ZM_") || m.name.StartsWith("Cerro_"));
                    if (!propio || !m.HasProperty("_BaseMap") || m.GetTexture("_BaseMap") != null || !m.shader.name.Contains("Lit") || m.shader.name.Contains("Unlit"))
                    {
                        continue;
                    }
                    if (m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.5f)
                    {
                        // Tubos y focos: es luz, no greybox; que la emisión quede encendida.
                        m.EnableKeyword("_EMISSION");
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        UnityEditor.EditorUtility.SetDirty(m);
                        continue;
                    }
                    Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(TexturaPorNombre(m.name));
                    if (tex == null || !UnityEditor.AssetDatabase.Contains(m))
                    {
                        continue;
                    }
                    m.SetTexture("_BaseMap", tex);
                    UnityEditor.EditorUtility.SetDirty(m);
                    texturas++;
                }
            }
            UnityEditor.AssetDatabase.SaveAssets();
            sb.AppendLine("Postes con luz de ciudad: " + postes + "; barriles explosivos nuevos: " + barriles + "; materiales con textura nueva: " + texturas);
            ColocarS2.Informe.Clear();
            ColocarS2.Informe.Append(sb);
            ColocarS2.Guardar("t1350-arreglar-zonas");
        }

        private static string TexturaPorNombre(string material)
        {
            string n = material.ToLowerInvariant();
            if (n.Contains("asfalto") || n.Contains("calle")) { return Texturas + "T03_Asfalto.png"; }
            if (n.Contains("arena") || n.Contains("playa")) { return Texturas + "T04_Arena.png"; }
            if (n.Contains("madera") || n.Contains("muelle")) { return "Assets/Arte/Exteriores/Texturas/T506_Madera.png"; }
            if (n.Contains("lamina") || n.Contains("contenedor")) { return "Assets/Arte/Exteriores/Texturas/T506_Lamina.png"; }
            if (n.Contains("metal") || n.Contains("hierro") || n.Contains("tubo") || n.Contains("riel")) { return "Assets/Arte/Exteriores/Texturas/T506_Metal.png"; }
            if (n.Contains("adoquin")) { return Texturas + "T06_Adoquin.png"; }
            return Texturas + "T05_ConcretoCerro.png";
        }

        private const string CarpetaPrefabsZonas = "Assets/Arte/Mundo/Zonas/";

        /// <summary>
        /// Último paso: cada raíz de zona se guarda como prefab (Assets/Arte/Mundo/Zonas/&lt;raíz&gt;.prefab) y en la escena queda la instancia.
        /// Con las cuatro zonas dentro, Ciudad_Prologo.unity pasaba de 100 MB (límite de GitHub). Idempotente: el siguiente Aplicar borra la
        /// instancia, rehace la raíz y este paso vuelve a guardar el prefab.
        /// </summary>
        public static void GuardarComoPrefabs()
        {
            ColocarS2.Abrir();
            System.IO.Directory.CreateDirectory(CarpetaPrefabsZonas);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string nombre in RaicesZonas.Concat(new[] { "T1358_Orillas" }))
            {
                Transform raiz = ColocarS2.Buscar(nombre);
                if (raiz == null)
                {
                    continue;
                }
                string ruta = CarpetaPrefabsZonas + nombre + ".prefab";
                if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(raiz.gameObject))
                {
                    UnityEditor.PrefabUtility.UnpackPrefabInstance(raiz.gameObject, UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.AutomatedAction);
                }
                UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(raiz.gameObject, ruta, UnityEditor.InteractionMode.AutomatedAction, out bool bien);
                sb.AppendLine(nombre + " -> " + ruta + (bien ? "" : " (FALLÓ)"));
            }
            ColocarS2.Informe.Clear();
            ColocarS2.Informe.Append(sb);
            ColocarS2.Guardar("t1350-prefabs-zonas");
        }

        public static void Correr()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-zonas");
            if (i < 0 || i + 1 >= args.Length)
            {
                Debug.LogError("[Arte] CorrerZonasT1350: falta -zonas Clase.Metodo,...");
                return;
            }
            Type[] tipos = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
            {
                try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
            }).ToArray();
            foreach (string paso in args[i + 1].Split(','))
            {
                string[] partes = paso.Trim().Split('.');
                Type tipo = tipos.FirstOrDefault(t => t.FullName == Prefijo + partes[0] || t.FullName == "MechaCyberpunk.Arte.Editor." + partes[0]);
                MethodInfo metodo = tipo?.GetMethod(partes[1], BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (metodo == null)
                {
                    Debug.LogError("[Arte] CorrerZonasT1350: no existe " + paso);
                    continue;
                }
                Debug.Log("[Arte] CorrerZonasT1350: " + paso);
                try
                {
                    metodo.Invoke(null, null);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Arte] CorrerZonasT1350: " + paso + " falló: " + (e.InnerException ?? e));
                }
            }
        }
    }
}

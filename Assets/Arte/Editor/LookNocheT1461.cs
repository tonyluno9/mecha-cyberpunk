using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Mundo;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1461: el look de noche de las zonas nuevas (Malecón, Cerro, Autopista, Puerto, Orillas, Uniones) igual a las referencias K11, K12, K15, K18.
    /// Aplicar(): (1) faroles de sodio con más intensidad y alcance, luces de neón existentes reforzadas y piso más liso (en los prefabs de Malecón,
    /// Autopista, Orillas y Uniones; en Cerro y Puerto, que son de la sesión B, como cambios de la instancia en la escena); (2) el prefab nuevo
    /// T1461_Look con un hijo por zona: halos y conos de sodio, luces y derrames de neón, charcos grandes y sondas de reflexión; (3) niebla y posproceso.
    /// HornearSondas(): hornea las sondas a la hora de foto (23 h). Validar(): mide y falla en voz alta. Informes en Logs/t1461-*.txt.
    /// Orden: LookNocheT1392.Aplicar, LookNocheT1399.Aplicar, LookNocheT1461.Aplicar, LookNocheT1461.HornearSondas, LookNocheT1461.Validar.
    /// </summary>
    public static class LookNocheT1461
    {
        private const string CarpetaZonas = "Assets/Arte/Mundo/Zonas/";
        private const string RutaPrefab = CarpetaZonas + "T1461_Look.prefab";
        private const string CarpetaSondas = CarpetaZonas + "Sondas_T1461";
        private const string CarpetaLook = "Assets/Arte/Mundo/Look/T1461";
        private const string RutaPerfilZonas = "Assets/Arte/Mundo/Look/PerfilZonasNoche_T1392.asset";
        private const string RutaPerfilBruma = "Assets/Arte/Mundo/Look/PerfilBrumaZonas_T1399.asset";
        private const string RaizLook = "T1461_Look";
        private const string RaizBruma1399 = "T1399_Bruma";
        private const string LogInforme = "Logs/t1461-aplicar.txt";
        private const string LogValidar = "Logs/t1461-validar.txt";
        private const string LogMateriales = "Logs/t1461-materiales.txt";
        private const string LogBase = "Logs/t1461-base.txt";

        private static readonly string[] Zonas = { "T1354_Malecon", "T1355_Cerro", "T1356_Autopista", "T1357_Puerto", "T1358_Orillas", "T1388_Uniones" };
        // Prefabs que sí se pueden editar (los de Cerro y Puerto son de la sesión B: allí los cambios van como overrides de la instancia).
        private static readonly string[] ZonasEditables = { "T1354_Malecon", "T1356_Autopista", "T1358_Orillas", "T1388_Uniones" };
        private static readonly string[] ZonasConSondas = { "T1354_Malecon", "T1355_Cerro", "T1356_Autopista", "T1357_Puerto" };
        private static readonly string[] PrefabsIntocables = { "T1355_Cerro", "T1357_Puerto" };

        // Sodio
        private static readonly Color Sodio = new Color(1f, 0.55f, 0.2f);
        private const float IntensidadSodio = 900f;
        private const float AlcanceSodio = 28f;
        private const float AnguloSodio = 120f;
        private const float AnguloInteriorSodio = 50f;
        // Neón
        private const float IntensidadNeon = 16f;
        private const float AlcanceNeon = 11f;
        private const int CupoLucesNeon = 30; // Opus: la escena ya tenía 1330 Light; con T-1463 y T-1464 el tope de 1400 no da para 80
        private const int CupoDerrames = 220;
        private const float CeldaNeon = 4f;
        private const float DistanciaLuzNeon = 2.5f;
        private const float TamanoMaximoNeon = 20f;
        private const float DistanciaMaximaCalle = 45f;
        private const float EmpujeNeon = 0.8f;
        private const float DerramePiso = 9f;
        private const float DerrameParedAncho = 6f;
        private const float DerrameParedAlto = 4.5f;
        private const float FuerzaDerramePared = 0.7f;
        // Halos y conos
        private const float RadioHalo = 6.5f;
        private const float IntensidadHalo = 1.6f;
        private const float BajoElFarol = 0.3f;
        private const int LadosCono = 20;
        private const float FactorRadioCono = 0.55f;
        // Piso
        private const float BrilloAsfalto = 0.78f;
        private const float BrilloBanqueta = 0.5f;
        // Charcos
        private const float CharcoMin = 5.5f;
        private const float CharcoMax = 11f;
        private const int CharcosMaxPorZona = 40;
        private const int CharcosMinPorZona = 12;
        private const float SeparacionCharcos = 5.5f;
        private const float MargenBanqueta = 1.2f;
        private const float PisoMinimoCharco = -0.3f;
        private const float PisoMaximoCharco = 0.55f;
        private const float PendienteMaxima = 0.08f;
        private const int FormasCharco = 8;
        private const float AltoCharco = 0.025f;
        // Sondas
        private const float SeparacionSondas = 60f;
        private const int SondasMaxPorZona = 12;
        private const float AlturaSonda = 3.5f;
        private const int ResolucionSonda = 128;
        private const float MegasMaximosSondas = 40f;
        // Niebla y posproceso (a nivel de calle)
        private static readonly Color NieblaNoche = new Color(0.24f, 0.38f, 0.5f);
        private const float DensidadNoche = 0.0065f;
        private const int TopeLuces = 1400;

        private static readonly StringBuilder Informe = new StringBuilder();
        private static readonly List<string> CambiosMateriales = new List<string>();
        private static readonly Dictionary<Material, Material> Copias = new Dictionary<Material, Material>();

        private struct Muestra
        {
            public Vector3 Punto;
            public Vector3 Tangente;
            public float Ancho;
        }

        private class Neon
        {
            public Vector3 Centro;
            public Color Color;
            public Vector3 HaciaCalle;
            public float Suelo;
            public bool YaTieneLuz;
            public float Puntaje;
        }

        private class DatosZona
        {
            public string Nombre;
            public string Corto;
            public Transform Instancia;
            public List<Muestra> Muestras = new List<Muestra>();
            public List<Vector3> Sondas = new List<Vector3>();
            public List<Vector3> Faroles = new List<Vector3>();
            public List<Vector3> CharcosViejos = new List<Vector3>();
            public List<Neon> Neones = new List<Neon>();
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1461/Aplicar look de noche")]
        public static void Aplicar()
        {
            Informe.Clear();
            CambiosMateriales.Clear();
            Copias.Clear();
            Informe.AppendLine("T-1461: look de noche de las zonas nuevas (correr después de LookNocheT1392.Aplicar y LookNocheT1399.Aplicar)");
            Directory.CreateDirectory("Logs");
            Carpeta(CarpetaLook);
            Carpeta(CarpetaLook + "/Mallas");

            ColocarS2.Abrir();
            Quitar(GameObject.Find(RaizLook));
            int lucesAntes = ContarLuces();
            File.WriteAllText(LogBase, "luces=" + lucesAntes + "\n" + string.Join("\n", PrefabsIntocables.Select(p => "md5 " + p + " " + Md5(CarpetaZonas + p + ".prefab"))) + "\n"
                + string.Join("\n", InstantaneaS2c()));
            Informe.AppendLine("Luces de la escena antes: " + lucesAntes + " (tope " + TopeLuces + ")");

            // 1) Faroles, neones y piso en los prefabs editables
            foreach (string zona in ZonasEditables)
            {
                string ruta = CarpetaZonas + zona + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
                {
                    Informe.AppendLine(zona + ": no existe el prefab, se salta.");
                    continue;
                }
                GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    AjustarZona(zona, contenido.transform, false, AlcanceSodio);
                    PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
            }
            AssetDatabase.SaveAssets();

            // 2) Cerro y Puerto: overrides en la instancia de la escena (sus prefabs son de la sesión B)
            Dictionary<string, Transform> instancias = BuscarInstancias();
            foreach (string zona in PrefabsIntocables)
            {
                if (instancias.TryGetValue(zona, out Transform t))
                {
                    AjustarZona(zona, t, true, zona == "T1355_Cerro" ? RendimientoCerroT1365.AlcanceMaximoLuz : AlcanceSodio);
                }
                else
                {
                    Informe.AppendLine(zona + ": no hay instancia en la escena, sin ajuste de luces ni piso.");
                }
            }

            // 3) El halo de T-1399 se reemplaza por el de este prefab
            QuitarHalosViejos();

            // 4) El prefab T1461_Look
            List<DatosZona> datos = Recolectar(instancias);
            ConstruirPrefab(datos);

            // 5) Niebla y posproceso
            Global();
            Perfiles();

            int lucesDespues = ContarLuces();
            Informe.AppendLine("Luces de la escena después: " + lucesDespues + " (añadidas " + (lucesDespues - lucesAntes) + ")");
            File.WriteAllLines(LogMateriales, CambiosMateriales);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            File.WriteAllText(LogInforme, Informe.ToString());
            Debug.Log("[Arte] " + Informe);
        }

        // ================================================================ Ajuste de una zona: faroles, neones y piso

        private static bool EsFarol(Light l)
        {
            if (LookNocheT1392.EsSodio(l))
            {
                return true;
            }
            Color c = l.color;
            bool naranja = c.r >= 0.85f && c.g >= 0.35f && c.g <= 0.8f && c.b <= 0.5f;
            if (!naranja || (l.type != LightType.Spot && l.type != LightType.Point))
            {
                return false;
            }
            int subidas = 0;
            for (Transform t = l.transform.parent; t != null && subidas < 3; t = t.parent, subidas++)
            {
                string n = t.name;
                if (n.Contains("Farol") || n.StartsWith("Poste") || n.Contains("ML01") || n.Contains("AU01") || n.Contains("C07") || n.Contains("Lampara"))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool EsLuzNeonExistente(Light l)
        {
            if (l.name == "LuzNeon")
            {
                return true;
            }
            for (Transform t = l.transform.parent; t != null; t = t.parent)
            {
                if (t.name == "T1392_LucesNeon" || t.name == "T1383_LucesNeon")
                {
                    return true;
                }
            }
            return false;
        }

        private static void Marcar(Object o, bool enEscena)
        {
            if (enEscena)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(o);
            }
            else
            {
                EditorUtility.SetDirty(o);
            }
        }

        private static void AjustarZona(string zona, Transform raiz, bool enEscena, float topeAlcance)
        {
            Informe.AppendLine();
            Informe.AppendLine("== " + zona + (enEscena ? " (overrides en la instancia de la escena)" : " (prefab)"));
            int sodio = 0, neones = 0, suelos = 0;
            foreach (Light l in raiz.GetComponentsInChildren<Light>(true))
            {
                if (EsFarol(l))
                {
                    l.color = Sodio;
                    l.intensity = IntensidadSodio;
                    l.range = topeAlcance;
                    if (l.type == LightType.Spot)
                    {
                        l.spotAngle = AnguloSodio;
                        l.innerSpotAngle = AnguloInteriorSodio;
                    }
                    l.shadows = LightShadows.None;
                    Marcar(l, enEscena);
                    sodio++;
                }
                else if (EsLuzNeonExistente(l))
                {
                    l.intensity = IntensidadNeon;
                    l.range = Mathf.Min(AlcanceNeon, topeAlcance);
                    Marcar(l, enEscena);
                    neones++;
                }
            }
            HashSet<Material> emisivos = new HashSet<Material>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material m = ms[i];
                    if (m == null)
                    {
                        continue;
                    }
                    float? brillo = BrilloPara(m);
                    if (brillo.HasValue && m.HasProperty("_Smoothness") && m.GetFloat("_Smoothness") < brillo.Value - 0.001f)
                    {
                        string ruta = AssetDatabase.GetAssetPath(m);
                        bool propio = ruta.EndsWith(".mat") && !m.name.StartsWith("S2c_");
                        if (propio)
                        {
                            CambiosMateriales.Add(m.name + "|" + ruta + "|" + m.GetFloat("_Smoothness").ToString("F2") + "|" + brillo.Value.ToString("F2"));
                            m.SetFloat("_Smoothness", brillo.Value);
                            EditorUtility.SetDirty(m);
                        }
                        else
                        {
                            ms[i] = CopiaPropia(m, brillo.Value);
                            cambio = true;
                        }
                        suelos++;
                    }
                    else if (!brillo.HasValue && !m.name.StartsWith("S2c_") && AssetDatabase.GetAssetPath(m).EndsWith(".mat") && emisivos.Add(m) && LookNocheT1392.ColorDeNeon(m).HasValue)
                    {
                        // Los neones de las zonas: emisión en tiempo real (si no, se pierde _EMISSION)
                        m.EnableKeyword("_EMISSION");
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        EditorUtility.SetDirty(m);
                    }
                }
                if (cambio)
                {
                    r.sharedMaterials = ms;
                    Marcar(r, enEscena);
                }
            }
            Informe.AppendLine("Faroles de sodio: " + sodio + " (intensidad " + IntensidadSodio + ", alcance " + topeAlcance + ", cono " + AnguloSodio + "/" + AnguloInteriorSodio + "); luces de neón reforzadas: " + neones
                + " (" + IntensidadNeon + ", alcance " + Mathf.Min(AlcanceNeon, topeAlcance) + "); materiales de piso con más brillo: " + suelos);
        }

        // Asfalto y banquetas por el nombre del material. null: no es piso.
        private static float? BrilloPara(Material m)
        {
            string n = m.name.ToLowerInvariant();
            if (n.Contains("charco") || n.Contains("marca") || n.Contains("linea") || n.Contains("cebra") || n.Contains("fachada") || n.Contains("edificio"))
            {
                return null;
            }
            if (n.Contains("asfalto") || n.Contains("asphalt") || n.Contains("pavimento") || n.Contains("calle") || n.Contains("road") || n.Contains("carretera"))
            {
                return BrilloAsfalto;
            }
            if (n.Contains("banqueta") || n.Contains("acera") || n.Contains("sidewalk") || n.Contains("guarnicion") || n.Contains("muelle") || n.Contains("adoqu"))
            {
                return BrilloBanqueta;
            }
            return null;
        }

        // Copia con otro nombre cuando el material original es S2c_* (de los barrios, no se toca) o viene de un modelo importado.
        private static Material CopiaPropia(Material original, float brillo)
        {
            if (Copias.TryGetValue(original, out Material ya))
            {
                return ya;
            }
            string ruta = CarpetaLook + "/T1461_" + Limpio(original.name) + ".mat";
            Material copia = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (copia == null)
            {
                copia = new Material(original) { name = "T1461_" + Limpio(original.name) };
                AssetDatabase.CreateAsset(copia, ruta);
            }
            CambiosMateriales.Add(copia.name + "|" + ruta + "|" + original.GetFloat("_Smoothness").ToString("F2") + "|" + brillo.ToString("F2"));
            copia.SetFloat("_Smoothness", brillo);
            EditorUtility.SetDirty(copia);
            Copias[original] = copia;
            return copia;
        }

        private static string Limpio(string nombre)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in nombre)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }
            return sb.ToString();
        }

        // ================================================================ Lectura de las zonas en la escena

        private static Dictionary<string, Transform> BuscarInstancias()
        {
            Dictionary<string, Transform> d = new Dictionary<string, Transform>();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (System.Array.IndexOf(Zonas, t.name) < 0)
                {
                    continue;
                }
                if (!d.ContainsKey(t.name) || PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                {
                    d[t.name] = t;
                }
            }
            return d;
        }

        private static List<DatosZona> Recolectar(Dictionary<string, Transform> instancias)
        {
            Physics.SyncTransforms();
            List<(Vector3 pos, Vector3 dir)> camaras = Camaras();
            List<DatosZona> lista = new List<DatosZona>();
            foreach (string zona in Zonas)
            {
                if (!instancias.TryGetValue(zona, out Transform inst))
                {
                    Informe.AppendLine(zona + ": sin instancia en la escena; no se arma su parte del prefab.");
                    continue;
                }
                DatosZona z = new DatosZona { Nombre = zona, Corto = zona.Substring(6), Instancia = inst };
                // Calles: muestras cada 2 m y puntos de sonda cada SeparacionSondas
                foreach (CalleZona cz in inst.GetComponentsInChildren<CalleZona>(true))
                {
                    List<Vector3> p = cz.Puntos();
                    float acum = 0f, sigMuestra = 0f, sigSonda = SeparacionSondas * 0.4f;
                    for (int i = 0; i + 1 < p.Count; i++)
                    {
                        float seg = Vector3.Distance(p[i], p[i + 1]);
                        if (seg < 0.001f)
                        {
                            continue;
                        }
                        Vector3 tang = (p[i + 1] - p[i]) / seg;
                        while (sigMuestra <= acum + seg)
                        {
                            z.Muestras.Add(new Muestra { Punto = p[i] + tang * (sigMuestra - acum), Tangente = tang, Ancho = cz.Ancho });
                            sigMuestra += 2f;
                        }
                        while (sigSonda <= acum + seg)
                        {
                            z.Sondas.Add(p[i] + tang * (sigSonda - acum));
                            sigSonda += SeparacionSondas;
                        }
                        acum += seg;
                    }
                }
                List<Vector3> calle = z.Muestras.Select(m => m.Punto).ToList();
                // Faroles
                foreach (Light l in inst.GetComponentsInChildren<Light>(false))
                {
                    if (EsFarol(l))
                    {
                        z.Faroles.Add(l.transform.position + Vector3.down * BajoElFarol);
                    }
                }
                // Neones y charcos viejos
                List<Vector3> lucesChicas = inst.GetComponentsInChildren<Light>(true)
                    .Where(l => l.type == LightType.Point && l.range <= 14f).Select(l => l.transform.position).ToList();
                foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled)
                    {
                        continue;
                    }
                    Material[] ms = r.sharedMaterials;
                    if (ms.Any(m => m != null && m.name.StartsWith("S2c_Charco")))
                    {
                        z.CharcosViejos.Add(r.bounds.center);
                        continue;
                    }
                    if (LookNocheT1392.Excluido(r) || r.GetComponentInParent<Light>() != null)
                    {
                        continue;
                    }
                    Color? color = null;
                    foreach (Material m in ms)
                    {
                        color = LookNocheT1392.ColorDeNeon(m);
                        if (color.HasValue)
                        {
                            break;
                        }
                    }
                    if (!color.HasValue)
                    {
                        continue;
                    }
                    Bounds b = r.bounds;
                    if (b.size.magnitude > TamanoMaximoNeon)
                    {
                        continue;
                    }
                    float dist = calle.Count > 0 ? LookNocheT1392.DistanciaACalle(calle, b.center) : 0f;
                    if (dist > DistanciaMaximaCalle)
                    {
                        continue;
                    }
                    z.Neones.Add(new Neon
                    {
                        Centro = b.center,
                        Color = color.Value,
                        HaciaCalle = HaciaCalle(calle, b.center),
                        Suelo = Suelo(b.center, calle),
                        YaTieneLuz = lucesChicas.Any(q => (q - b.center).sqrMagnitude < DistanciaLuzNeon * DistanciaLuzNeon),
                        Puntaje = PuntajeCamaras(camaras, b.center)
                    });
                }
                // Sondas: si hay de más, un subconjunto repartido parejo
                if (z.Sondas.Count > SondasMaxPorZona)
                {
                    List<Vector3> parejas = new List<Vector3>();
                    for (int i = 0; i < SondasMaxPorZona; i++)
                    {
                        parejas.Add(z.Sondas[Mathf.Min(z.Sondas.Count - 1, Mathf.RoundToInt(i * (z.Sondas.Count - 1f) / (SondasMaxPorZona - 1f)))]);
                    }
                    z.Sondas = parejas;
                }
                Informe.AppendLine(zona + ": calle " + z.Muestras.Count * 2 + " m, faroles " + z.Faroles.Count + ", neones candidatos " + z.Neones.Count + ", charcos viejos " + z.CharcosViejos.Count + ", puntos de sonda " + z.Sondas.Count);
                lista.Add(z);
            }
            return lista;
        }

        private static List<(Vector3 pos, Vector3 dir)> Camaras()
        {
            // Las tomas de Vista19b: K11, K12, K13, K15, K18
            return new List<(Vector3, Vector3)>
            {
                (ZonaMaleconT1393.CamaraK11, new Vector3(-59f, 7f, -221.7f) - ZonaMaleconT1393.CamaraK11),
                (ZonaMaleconT1413.CamaraK12, ZonaMaleconT1413.MiraK12 - ZonaMaleconT1413.CamaraK12),
                (ZonaCerroT1416.K13bDesde, ZonaCerroT1416.K13bHacia - ZonaCerroT1416.K13bDesde),
                (ZonaPuertoT1396.CamaraK15, ZonaPuertoT1396.MiraK15 - ZonaPuertoT1396.CamaraK15),
                (new Vector3(-168f, 5.98f, 226f), new Vector3(-197.92f, 4.22f, 227.3f) - new Vector3(-168f, 5.98f, 226f)),
            };
        }

        private static float PuntajeCamaras(List<(Vector3 pos, Vector3 dir)> camaras, Vector3 p)
        {
            float mejor = float.MinValue;
            foreach (var c in camaras)
            {
                Vector3 v = p - c.pos;
                float d = v.magnitude;
                float s = d >= 4f && d <= 150f && Vector3.Angle(c.dir, v) <= 50f ? 1000f - d : -d;
                mejor = Mathf.Max(mejor, s);
            }
            return mejor;
        }

        private static Vector3 HaciaCalle(List<Vector3> calle, Vector3 p)
        {
            if (calle.Count == 0)
            {
                return Vector3.zero;
            }
            Vector3 mejor = calle[0];
            float d = float.MaxValue;
            foreach (Vector3 q in calle)
            {
                float dd = (q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z);
                if (dd < d)
                {
                    d = dd;
                    mejor = q;
                }
            }
            Vector3 v = new Vector3(mejor.x - p.x, 0f, mejor.z - p.z);
            return v.sqrMagnitude < 0.0001f ? Vector3.zero : v.normalized;
        }

        private static float Suelo(Vector3 desde, List<Vector3> calle)
        {
            if (Physics.Raycast(desde + Vector3.up * 0.3f, Vector3.down, out RaycastHit golpe, 40f, ~0, QueryTriggerInteraction.Ignore))
            {
                return golpe.point.y;
            }
            return calle.Count > 0 ? calle.OrderBy(q => (q.x - desde.x) * (q.x - desde.x) + (q.z - desde.z) * (q.z - desde.z)).First().y : desde.y - 6f;
        }

        // ================================================================ El prefab T1461_Look

        private static void ConstruirPrefab(List<DatosZona> zonas)
        {
            Informe.AppendLine();
            Informe.AppendLine("== Prefab " + RutaPrefab);
            Carpeta(CarpetaSondas);
            Material matHalo = HacerMaterial(CarpetaLook + "/T1461_Halos.mat", "MechaCyberpunk/Fondos/BrumaHalos");
            matHalo.SetColor("_Color", Sodio);
            matHalo.SetFloat("_Intensidad", IntensidadHalo);
            matHalo.SetFloat("_Tamano", RadioHalo);
            Material matCono = HacerMaterial(CarpetaLook + "/T1461_ConoSodio.mat", "MechaCyberpunk/Mundo/HaloLuz");
            matCono.SetColor("_Color", Sodio);
            matCono.SetFloat("_Intensidad", 0.3f);
            matCono.SetFloat("_Borde", 3.4f);
            matCono.SetFloat("_Caida", 2f);
            matCono.SetFloat("_Suave", 0.8f);
            matCono.SetTexture("_Ruido", KitS2c.TexMuro());
            Material matDerrame = HacerMaterial(CarpetaLook + "/T1461_Derrame.mat", "MechaCyberpunk/Fondos/DerrameNeon");
            matDerrame.SetFloat("_Intensidad", 0.55f);
            matDerrame.SetFloat("_Caida", 2f);
            Material[] matCharcos = new Material[FormasCharco];
            for (int f = 0; f < FormasCharco; f++)
            {
                Material m = HacerMaterial(CarpetaLook + "/T1461_Charco_" + f + ".mat", "MechaCyberpunk/Mundo/Charco");
                m.SetTexture("_Mascara", KitS2c.TexCharco(f));
                m.SetFloat("_Intensidad", 1.25f);
                m.SetFloat("_FresnelMin", 0.6f);
                m.SetFloat("_Opacidad", 0.95f);
                EditorUtility.SetDirty(m);
                matCharcos[f] = m;
            }
            EditorUtility.SetDirty(matHalo);
            EditorUtility.SetDirty(matCono);
            EditorUtility.SetDirty(matDerrame);
            Mesh quadCharco = MallaCharco();

            // Neones elegidos en todo el conjunto: los visibles desde las cámaras de las fotos primero
            HashSet<Neon> conLuz = new HashSet<Neon>();
            HashSet<Vector2Int> celdas = new HashSet<Vector2Int>();
            foreach (var par in zonas.SelectMany(z => z.Neones.Select(n => (z, n))).Where(x => !x.n.YaTieneLuz).OrderByDescending(x => x.n.Puntaje))
            {
                if (conLuz.Count >= CupoLucesNeon)
                {
                    break;
                }
                Vector2Int celda = new Vector2Int(Mathf.FloorToInt(par.n.Centro.x / CeldaNeon), Mathf.FloorToInt(par.n.Centro.z / CeldaNeon));
                if (celdas.Add(celda))
                {
                    conLuz.Add(par.n);
                }
            }
            HashSet<Neon> conDerrame = new HashSet<Neon>(zonas.SelectMany(z => z.Neones).OrderByDescending(n => n.Puntaje).Take(CupoDerrames));

            GameObject raiz = new GameObject(RaizLook);
            int totalLuces = 0, totalCharcos = 0, totalHalos = 0, totalDerrames = 0, totalSondas = 0;
            foreach (DatosZona z in zonas)
            {
                Transform zt = new GameObject(z.Corto).transform;
                zt.SetParent(raiz.transform, false);

                // Halos y conos de sodio
                if (z.Faroles.Count > 0)
                {
                    MeshRenderer mrH = Malla(zt, "HalosSodio", MallaHalos(z), matHalo);
                    MeshRenderer mrC = Malla(zt, "ConosSodio", MallaConos(z), matCono);
                    totalHalos += z.Faroles.Count;
                }

                // Luces de neón
                Transform luces = new GameObject("LucesNeon").transform;
                luces.SetParent(zt, false);
                foreach (Neon n in z.Neones.Where(n => conLuz.Contains(n)))
                {
                    Light l = KitS2c.Luz(luces, n.Centro + n.HaciaCalle * EmpujeNeon, LightType.Point, n.Color, IntensidadNeon, AlcanceNeon, 0f);
                    l.name = "LuzNeon";
                    l.shadows = LightShadows.None;
                    totalLuces++;
                }

                // Derrames
                List<Neon> derr = z.Neones.Where(n => conDerrame.Contains(n)).ToList();
                if (derr.Count > 0)
                {
                    Malla(zt, "DerramesNeon", MallaDerrames(z, derr), matDerrame);
                    totalDerrames += derr.Count;
                }

                // Charcos
                Transform cg = new GameObject("Charcos").transform;
                cg.SetParent(zt, false);
                totalCharcos += PonerCharcos(z, cg, quadCharco, matCharcos, conLuz);

                // Sondas
                if (System.Array.IndexOf(ZonasConSondas, z.Nombre) >= 0)
                {
                    Transform sg = new GameObject("Sondas").transform;
                    sg.SetParent(zt, false);
                    for (int i = 0; i < z.Sondas.Count; i++)
                    {
                        GameObject go = new GameObject("Sonda_" + z.Corto + "_" + i.ToString("00"));
                        go.transform.SetParent(sg, false);
                        go.transform.position = z.Sondas[i] + Vector3.up * AlturaSonda;
                        ReflectionProbe p = go.AddComponent<ReflectionProbe>();
                        p.mode = ReflectionProbeMode.Custom;
                        p.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                        p.boxProjection = true;
                        p.size = new Vector3(SeparacionSondas + 14f, 30f, SeparacionSondas + 14f);
                        p.center = new Vector3(0f, 10f, 0f);
                        p.blendDistance = 8f;
                        p.resolution = ResolucionSonda;
                        p.hdr = true;
                        p.importance = 1;
                        p.nearClipPlane = 0.3f;
                        p.farClipPlane = 220f;
                        totalSondas++;
                    }
                }
            }
            Informe.AppendLine("Luces de neón nuevas: " + totalLuces + " (cupo " + CupoLucesNeon + "), derrames: " + totalDerrames + ", halos+conos de sodio: " + totalHalos + ", charcos: " + totalCharcos + ", sondas: " + totalSondas);
            PrefabUtility.SaveAsPrefabAssetAndConnect(raiz, RutaPrefab, InteractionMode.AutomatedAction);
        }

        private static MeshRenderer Malla(Transform padre, string nombre, Mesh malla, Material material)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return mr;
        }

        // Un quad por farol que el shader abre hacia la cámara (billboard en el vértice): no hace falta componente en tiempo de juego.
        private static Mesh MallaHalos(DatosZona z)
        {
            Mesh m = MallaAsset(CarpetaLook + "/Mallas/T1461_Halos_" + z.Corto + ".asset");
            List<Vector3> pos = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> tri = new List<int>();
            Bounds caja = new Bounds(z.Faroles[0], Vector3.zero);
            foreach (Vector3 c in z.Faroles)
            {
                int i = pos.Count;
                for (int k = 0; k < 4; k++)
                {
                    pos.Add(c);
                }
                uv.Add(new Vector2(-1f, -1f));
                uv.Add(new Vector2(1f, -1f));
                uv.Add(new Vector2(1f, 1f));
                uv.Add(new Vector2(-1f, 1f));
                tri.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
                caja.Encapsulate(c);
            }
            m.indexFormat = pos.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            m.SetVertices(pos);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            caja.Expand(RadioHalo * 2f + 2f);
            m.bounds = caja;
            EditorUtility.SetDirty(m);
            return m;
        }

        // Un cono tenue de cada farol hacia el piso (UV.y 0 en la lámpara y 1 en la base, como pide HaloLuz).
        private static Mesh MallaConos(DatosZona z)
        {
            Mesh m = MallaAsset(CarpetaLook + "/Mallas/T1461_Conos_" + z.Corto + ".asset");
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            List<Vector3> calle = z.Muestras.Select(q => q.Punto).ToList();
            foreach (Vector3 lampara in z.Faroles)
            {
                float suelo = Suelo(lampara, calle);
                float alto = Mathf.Clamp(lampara.y - suelo, 3f, 14f);
                float radio = alto * FactorRadioCono;
                int baseIdx = v.Count;
                for (int i = 0; i <= LadosCono; i++)
                {
                    float a = i * Mathf.PI * 2f / LadosCono;
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    Vector3 normal = (dir + Vector3.up * 0.8f).normalized;
                    v.Add(lampara + dir * 0.15f);
                    v.Add(new Vector3(lampara.x, lampara.y - alto, lampara.z) + dir * radio);
                    n.Add(normal);
                    n.Add(normal);
                    uv.Add(new Vector2(i / (float)LadosCono, 0f));
                    uv.Add(new Vector2(i / (float)LadosCono, 1f));
                    if (i < LadosCono)
                    {
                        int k = baseIdx + i * 2;
                        t.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
                    }
                }
            }
            m.indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            EditorUtility.SetDirty(m);
            return m;
        }

        // Dos quads aditivos por letrero (piso frente al neón y pared), todos en una malla con el color en el color de vértice.
        private static Mesh MallaDerrames(DatosZona z, List<Neon> neones)
        {
            Mesh m = MallaAsset(CarpetaLook + "/Mallas/T1461_Derrames_" + z.Corto + ".asset");
            List<Vector3> v = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<Color> col = new List<Color>();
            List<int> t = new List<int>();
            foreach (Neon n in neones)
            {
                Vector3 hacia = n.HaciaCalle.sqrMagnitude > 0.01f ? n.HaciaCalle : Vector3.forward;
                // Piso
                Vector3 cp = new Vector3(n.Centro.x, n.Suelo + 0.04f, n.Centro.z) + hacia * (DerramePiso * 0.3f);
                Quad(v, uv, col, t, cp, Vector3.right * DerramePiso * 0.5f, Vector3.forward * DerramePiso * 0.5f, n.Color);
                // Pared: perpendicular a la dirección hacia la calle, un poco al frente del letrero
                Vector3 derecha = Vector3.Cross(Vector3.up, hacia).normalized;
                Vector3 cw = n.Centro + hacia * 0.15f;
                Quad(v, uv, col, t, cw, derecha * DerrameParedAncho * 0.5f, Vector3.up * DerrameParedAlto * 0.5f, n.Color * FuerzaDerramePared);
            }
            m.indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetColors(col);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void Quad(List<Vector3> v, List<Vector2> uv, List<Color> col, List<int> t, Vector3 c, Vector3 ejeA, Vector3 ejeB, Color color)
        {
            int i = v.Count;
            v.Add(c - ejeA - ejeB);
            v.Add(c + ejeA - ejeB);
            v.Add(c + ejeA + ejeB);
            v.Add(c - ejeA + ejeB);
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(1f, 0f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(0f, 1f));
            for (int k = 0; k < 4; k++)
            {
                col.Add(color);
            }
            t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
        }

        // Charcos grandes e irregulares (máscaras S2c_Charco_n generadas por KitS2c.TexCharco), más cerca de faroles y neones con luz.
        private static int PonerCharcos(DatosZona z, Transform grupo, Mesh quad, Material[] materiales, HashSet<Neon> conLuz)
        {
            List<Muestra> llanas = z.Muestras.Where(m => Mathf.Abs(m.Tangente.y) <= PendienteMaxima && m.Punto.y >= PisoMinimoCharco && m.Punto.y <= PisoMaximoCharco).ToList();
            if (llanas.Count == 0)
            {
                Informe.AppendLine(z.Nombre + ": sin tramos llanos a nivel de piso; sin charcos.");
                return 0;
            }
            System.Random azar = new System.Random(1461 + z.Corto.Length * 7 + z.Corto[0]);
            List<Vector3> anclas = new List<Vector3>(z.Faroles);
            anclas.AddRange(z.Neones.Where(n => conLuz.Contains(n) || n.YaTieneLuz).Select(n => n.Centro));
            List<Vector3> ocupados = new List<Vector3>(z.CharcosViejos);
            int puestos = 0;
            int intentos = 0;
            while (puestos < CharcosMaxPorZona && intentos < anclas.Count + 200)
            {
                intentos++;
                Vector3 ancla;
                bool deAncla = intentos <= anclas.Count;
                if (deAncla)
                {
                    ancla = anclas[intentos - 1];
                }
                else if (puestos < CharcosMinPorZona)
                {
                    ancla = llanas[azar.Next(llanas.Count)].Punto; // relleno si hay pocas luces
                }
                else
                {
                    break;
                }
                Muestra m = llanas.OrderBy(q => (q.Punto.x - ancla.x) * (q.Punto.x - ancla.x) + (q.Punto.z - ancla.z) * (q.Punto.z - ancla.z)).First();
                Vector3 d = new Vector3(m.Punto.x - ancla.x, 0f, m.Punto.z - ancla.z);
                if (d.magnitude > 16f)
                {
                    continue;
                }
                float tamano = Mathf.Lerp(CharcoMin, CharcoMax, (float)azar.NextDouble());
                tamano = Mathf.Min(tamano, Mathf.Max(3f, m.Ancho - 2f * MargenBanqueta));
                float lateralMax = Mathf.Max(0f, m.Ancho * 0.5f - MargenBanqueta - tamano * 0.5f);
                Vector3 plano = new Vector3(m.Tangente.x, 0f, m.Tangente.z);
                if (plano.sqrMagnitude < 0.0001f)
                {
                    continue;
                }
                plano.Normalize();
                Vector3 lado = Vector3.Cross(Vector3.up, plano);
                float haciaAncla = Mathf.Clamp(Vector3.Dot(ancla - m.Punto, lado), -lateralMax, lateralMax);
                Vector3 pos = m.Punto + lado * haciaAncla + plano * (((float)azar.NextDouble() * 2f - 1f) * 3f);
                pos.y = m.Punto.y + AltoCharco;
                if (ocupados.Any(o => (o.x - pos.x) * (o.x - pos.x) + (o.z - pos.z) * (o.z - pos.z) < SeparacionCharcos * SeparacionCharcos))
                {
                    continue;
                }
                GameObject go = new GameObject("Charco");
                go.layer = ReflejoCharcos.CapaCharcos;
                go.transform.SetParent(grupo, false);
                go.transform.rotation = Quaternion.LookRotation(plano, Vector3.up) * Quaternion.Euler(0f, ((float)azar.NextDouble() * 2f - 1f) * 40f, 0f);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(tamano * Mathf.Lerp(0.6f, 1f, (float)azar.NextDouble()), 1f, tamano);
                go.AddComponent<MeshFilter>().sharedMesh = quad;
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = materiales[azar.Next(materiales.Length)];
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                go.AddComponent<CharcoVisible>();
                ocupados.Add(pos);
                puestos++;
            }
            return puestos;
        }

        private static Mesh MallaCharco()
        {
            Mesh m = MallaAsset(CarpetaLook + "/Mallas/T1461_CharcoPlano.asset");
            m.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.RecalculateBounds();
            EditorUtility.SetDirty(m);
            return m;
        }

        // ================================================================ Halos viejos de T-1399

        private static void QuitarHalosViejos()
        {
            GameObject bruma = GameObject.Find(RaizBruma1399);
            int n = 0;
            if (bruma != null)
            {
                foreach (Transform hijo in bruma.transform.Cast<Transform>().Where(h => h.name.StartsWith("HalosSodio_")).ToList())
                {
                    Object.DestroyImmediate(hijo.gameObject);
                    n++;
                }
            }
            Informe.AppendLine("Halos de sodio de T-1399 reemplazados (quitados de la escena): " + n);
        }

        // ================================================================ Niebla y posproceso

        private static void Global()
        {
            Informe.AppendLine();
            Informe.AppendLine("== Niebla (ConfigCicloDia_Mundo; la comparten los barrios viejos: cambio moderado como T-1399)");
            ConfigCicloDia config = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(ColocarS2cLook.RutaCicloDia);
            if (config == null)
            {
                Informe.AppendLine("AVISO: no se pudo cargar ConfigCicloDia_Mundo.");
                return;
            }
            SerializedObject so = new SerializedObject(config);
            foreach (string campo in new[] { "colorNiebla" })
            {
                SerializedProperty prop = so.FindProperty(campo);
                Gradient g = prop.gradientValue;
                GradientColorKey[] claves = g.colorKeys;
                for (int i = 0; i < claves.Length; i++)
                {
                    if (claves[i].time <= 0.28f || claves[i].time >= 0.8f)
                    {
                        Informe.AppendLine("  " + campo + " t=" + claves[i].time.ToString("F2") + ": " + claves[i].color + " -> " + NieblaNoche);
                        claves[i].color = NieblaNoche;
                    }
                }
                g.SetKeys(claves, g.alphaKeys);
                prop.gradientValue = g;
            }
            SerializedProperty densidad = so.FindProperty("densidadNiebla");
            AnimationCurve curva = densidad.animationCurveValue;
            Keyframe[] llaves = curva.keys;
            for (int i = 0; i < llaves.Length; i++)
            {
                if (llaves[i].time <= 4.5f || llaves[i].time >= 21f)
                {
                    llaves[i].value = DensidadNoche;
                    llaves[i].inTangent = 0f;
                    llaves[i].outTangent = 0f;
                }
            }
            curva.keys = llaves;
            densidad.animationCurveValue = curva;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            RenderSettings.fog = true;
            RenderSettings.fogColor = NieblaNoche;
            RenderSettings.fogDensity = DensidadNoche;
            Informe.AppendLine("Densidad de noche " + DensidadNoche + " (exp. al cuadrado): 30 m " + Niebla(30f).ToString("P0") + ", 60 m " + Niebla(60f).ToString("P0") + ", 100 m " + Niebla(100f).ToString("P0")
                + ", 150 m " + Niebla(150f).ToString("P0") + ", 250 m " + Niebla(250f).ToString("P0") + " (T-1399: 0.009)");
        }

        private static float Niebla(float metros)
        {
            float x = DensidadNoche * metros;
            return 1f - Mathf.Exp(-x * x);
        }

        private static T Componente<T>(VolumeProfile perfil) where T : VolumeComponent
        {
            if (!perfil.TryGet(out T c))
            {
                c = perfil.Add<T>(false);
                AssetDatabase.AddObjectToAsset(c, perfil);
            }
            return c;
        }

        // Los volúmenes por zona de T-1392 (prioridad 11) y T-1399 (prioridad 12) ya existen: se ajustan sus perfiles.
        private static void Perfiles()
        {
            Informe.AppendLine();
            Informe.AppendLine("== Posproceso de las zonas (perfiles de T-1392 y T-1399)");
            VolumeProfile zonas = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilZonas);
            if (zonas != null)
            {
                ColorAdjustments ca = Componente<ColorAdjustments>(zonas);
                ca.postExposure.Override(0.6f);
                ca.contrast.Override(24f);
                ca.saturation.Override(14f);
                Bloom bloom = Componente<Bloom>(zonas);
                bloom.threshold.Override(0.9f);
                bloom.intensity.Override(2.4f);
                bloom.scatter.Override(0.72f);
                bloom.tint.Override(new Color(1f, 0.93f, 0.85f));
                EditorUtility.SetDirty(zonas);
                Informe.AppendLine(RutaPerfilZonas + ": exposición +0.6, contraste 24, saturación 14, bloom umbral 0.9 intensidad 2.4 difusión 0.72 con tinte cálido");
            }
            else
            {
                Informe.AppendLine("AVISO: falta " + RutaPerfilZonas + " (correr LookNocheT1392.Aplicar antes).");
            }
            VolumeProfile bruma = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilBruma);
            if (bruma != null)
            {
                // Este perfil (prioridad 12) ganaba sobre el de T-1392 con contraste 3 y los lift levantados: la causa del azul plano.
                ColorAdjustments ca = Componente<ColorAdjustments>(bruma);
                ca.postExposure.Override(0.6f);
                ca.contrast.Override(28f);
                ca.saturation.Override(14f);
                LiftGammaGain lgg = Componente<LiftGammaGain>(bruma);
                lgg.lift.Override(new Vector4(0.95f, 1f, 1.04f, -0.05f));
                lgg.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
                lgg.gain.Override(new Vector4(1.05f, 1f, 0.95f, 0f));
                ShadowsMidtonesHighlights smh = Componente<ShadowsMidtonesHighlights>(bruma);
                smh.shadows.Override(new Vector4(0.86f, 1f, 1.08f, -0.04f));
                smh.midtones.Override(new Vector4(1f, 1f, 1f, 0f));
                smh.highlights.Override(new Vector4(1.1f, 1f, 0.88f, 0.04f));
                EditorUtility.SetDirty(bruma);
                Informe.AppendLine(RutaPerfilBruma + ": contraste 28 (era 3), lift petróleo con negro más profundo, gain y altas luces cálidas, sombras hacia petróleo");
            }
            else
            {
                Informe.AppendLine("AVISO: falta " + RutaPerfilBruma + " (correr LookNocheT1399.Aplicar antes).");
            }
            AssetDatabase.SaveAssets();
        }

        // ================================================================ Sondas de reflexión

        [MenuItem("Mecha Cyberpunk/T1461/Hornear sondas")]
        public static void HornearSondas()
        {
            StringBuilder log = new StringBuilder("T-1461: horneado de sondas a las " + ColocarS2cLook.HoraFoto + " h\n");
            ColocarS2.Abrir();
            GameObject raiz = GameObject.Find(RaizLook);
            if (raiz == null)
            {
                Debug.LogError("[Arte] T-1461 HornearSondas: no hay " + RaizLook + " en la escena (correr Aplicar).");
                return;
            }
            Carpeta(CarpetaSondas);
            CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>();
            if (ciclo != null)
            {
                ciclo.Aplicar(ColocarS2cLook.HoraFoto);
                ColocarS2nCielo.SeguirNoche();
            }
            foreach (LuzNocturna ln in Object.FindObjectsByType<LuzNocturna>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Light l = new SerializedObject(ln).FindProperty("luz").objectReferenceValue as Light;
                if (l != null)
                {
                    l.enabled = true;
                }
            }
            Physics.SyncTransforms();
            Dictionary<string, string> rutas = new Dictionary<string, string>();
            int fallas = 0;
            foreach (ReflectionProbe p in raiz.GetComponentsInChildren<ReflectionProbe>(true))
            {
                string ruta = CarpetaSondas + "/" + p.name + ".exr";
                bool ok = Lightmapping.BakeReflectionProbe(p, ruta);
                log.AppendLine((ok ? "OK " : "FALLO ") + p.name + " -> " + ruta);
                if (ok)
                {
                    rutas[p.name] = ruta;
                }
                else
                {
                    fallas++;
                }
            }
            AssetDatabase.Refresh();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                foreach (ReflectionProbe p in contenido.GetComponentsInChildren<ReflectionProbe>(true))
                {
                    if (rutas.TryGetValue(p.name, out string ruta))
                    {
                        p.mode = ReflectionProbeMode.Custom;
                        p.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(ruta);
                        if (p.customBakedTexture == null)
                        {
                            log.AppendLine("FALLO: no se pudo cargar " + ruta);
                            fallas++;
                        }
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            AssetDatabase.SaveAssets();
            log.AppendLine("Sondas horneadas: " + rutas.Count + ", fallas: " + fallas + ", tamaño en disco: " + (TamanoSondas() / 1048576.0).ToString("F1") + " MB (tope " + MegasMaximosSondas + ")");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1461-sondas.txt", log.ToString());
            Debug.Log("[Arte] " + log);
            if (fallas > 0)
            {
                Debug.LogError("[Arte] T-1461 HornearSondas: " + fallas + " fallas, ver Logs/t1461-sondas.txt");
            }
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar: las luces quedaron prendidas
        }

        private static long TamanoSondas()
        {
            return Directory.Exists(CarpetaSondas) ? Directory.GetFiles(CarpetaSondas, "*.exr").Sum(f => new FileInfo(f).Length) : 0L;
        }

        // ================================================================ Validar

        [MenuItem("Mecha Cyberpunk/T1461/Validar")]
        public static void Validar()
        {
            StringBuilder log = new StringBuilder("T-1461: validación\n");
            List<string> fallas = new List<string>();
            ColocarS2.Abrir();
            Directory.CreateDirectory("Logs");
            string[] baseLog = File.Exists(LogBase) ? File.ReadAllLines(LogBase) : new string[0];

            // Luces
            int total = ContarLuces();
            GameObject raiz = GameObject.Find(RaizLook);
            if (raiz == null)
            {
                fallas.Add("No hay " + RaizLook + " en la escena.");
            }
            int nuevas = raiz != null ? raiz.GetComponentsInChildren<Light>(true).Length : 0;
            log.AppendLine("Luces totales en la escena: " + total + " (tope " + TopeLuces + "), añadidas por T1461_Look: " + nuevas + " (cupo " + CupoLucesNeon + ")");
            if (total > TopeLuces)
            {
                fallas.Add("Luces totales " + total + " pasan del tope " + TopeLuces);
            }
            if (nuevas > CupoLucesNeon)
            {
                fallas.Add("Luces añadidas " + nuevas + " pasan del cupo " + CupoLucesNeon);
            }
            string lineaBase = baseLog.FirstOrDefault(l => l.StartsWith("luces="));
            if (lineaBase != null)
            {
                log.AppendLine("Luces antes de Aplicar: " + lineaBase.Substring(6));
            }

            if (raiz != null)
            {
                ReflectionProbe[] sondas = raiz.GetComponentsInChildren<ReflectionProbe>(true);
                int horneadas = sondas.Count(p => p.customBakedTexture != null);
                int sinBox = sondas.Count(p => !p.boxProjection);
                double mb = TamanoSondas() / 1048576.0;
                log.AppendLine("Sondas: " + sondas.Length + ", horneadas: " + horneadas + ", sin box projection: " + sinBox + ", tamaño en disco: " + mb.ToString("F1") + " MB (tope " + MegasMaximosSondas + ")");
                if (sondas.Length == 0)
                {
                    fallas.Add("No hay sondas de reflexión.");
                }
                if (horneadas < sondas.Length)
                {
                    fallas.Add((sondas.Length - horneadas) + " sondas sin hornear (correr HornearSondas).");
                }
                if (sinBox > 0)
                {
                    fallas.Add(sinBox + " sondas sin box projection.");
                }
                if (mb > MegasMaximosSondas)
                {
                    fallas.Add("Las sondas pesan " + mb.ToString("F1") + " MB, tope " + MegasMaximosSondas);
                }
                foreach (string zona in ZonasConSondas)
                {
                    Transform z = raiz.transform.Find(zona.Substring(6));
                    int n = z != null ? z.GetComponentsInChildren<ReflectionProbe>(true).Length : 0;
                    log.AppendLine("  sondas " + zona + ": " + n);
                    if (n == 0)
                    {
                        fallas.Add("La zona " + zona + " no tiene sondas.");
                    }
                }

                MeshRenderer[] renderers = raiz.GetComponentsInChildren<MeshRenderer>(true);
                int charcos = renderers.Count(r => r.name == "Charco");
                int mal = renderers.Count(r => r.name == "Charco" && (r.gameObject.layer != ReflejoCharcos.CapaCharcos || r.GetComponent<CharcoVisible>() == null));
                int halos = 0, derrames = 0;
                foreach (MeshRenderer r in renderers)
                {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null)
                    {
                        continue;
                    }
                    if (r.name == "HalosSodio")
                    {
                        halos += mf.sharedMesh.vertexCount / 4;
                    }
                    else if (r.name == "DerramesNeon")
                    {
                        derrames += mf.sharedMesh.vertexCount / 8;
                    }
                }
                log.AppendLine("Charcos: " + charcos + " (sin capa Water o sin CharcoVisible: " + mal + "), halos de sodio: " + halos + ", derrames de neón: " + derrames);
                if (charcos < 60)
                {
                    fallas.Add("Pocos charcos: " + charcos);
                }
                if (mal > 0)
                {
                    fallas.Add(mal + " charcos sin capa Water o sin CharcoVisible.");
                }
                if (halos < 20)
                {
                    fallas.Add("Pocos halos de sodio: " + halos);
                }
                if (derrames < 10)
                {
                    fallas.Add("Pocos derrames de neón: " + derrames);
                }
            }

            // Halos viejos de T-1399
            GameObject bruma = GameObject.Find(RaizBruma1399);
            int viejos = bruma != null ? bruma.transform.Cast<Transform>().Count(h => h.name.StartsWith("HalosSodio_")) : 0;
            log.AppendLine("Halos de T-1399 que siguen en la escena (deben ser 0): " + viejos);
            if (viejos > 0)
            {
                fallas.Add("Quedan " + viejos + " halos de T-1399 duplicados.");
            }

            // Faroles de sodio con el valor nuevo
            int faroles = 0, debiles = 0;
            foreach (string zona in Zonas)
            {
                Transform inst = BuscarInstancias().TryGetValue(zona, out Transform t) ? t : null;
                if (inst == null)
                {
                    continue;
                }
                foreach (Light l in inst.GetComponentsInChildren<Light>(false))
                {
                    if (EsFarol(l))
                    {
                        faroles++;
                        if (l.intensity < IntensidadSodio - 1f || Mathf.Abs(l.color.g - Sodio.g) > 0.02f)
                        {
                            debiles++;
                        }
                    }
                }
            }
            log.AppendLine("Faroles de sodio en las zonas: " + faroles + ", sin el valor nuevo: " + debiles);
            if (debiles > 0)
            {
                fallas.Add(debiles + " faroles sin la intensidad o el color de sodio.");
            }

            // Materiales cambiados
            log.AppendLine("Materiales de piso cambiados (nombre | antes | ahora | smoothness actual):");
            if (File.Exists(LogMateriales))
            {
                foreach (string linea in File.ReadAllLines(LogMateriales))
                {
                    string[] c = linea.Split('|');
                    if (c.Length < 4)
                    {
                        continue;
                    }
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(c[1]);
                    float actual = m != null && m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : -1f;
                    log.AppendLine("  " + c[0] + " | " + c[2] + " | " + c[3] + " | " + actual.ToString("F2"));
                    if (actual < BrilloBanqueta - 0.001f)
                    {
                        fallas.Add("El material " + c[0] + " quedó con smoothness " + actual);
                    }
                }
            }
            else
            {
                log.AppendLine("  (no hay " + LogMateriales + ")");
            }

            // Ningún material S2c_* cambió, ni los prefabs de Cerro y Puerto
            string[] antes = baseLog.Where(l => l.StartsWith("s2c ")).ToArray();
            string[] ahora = InstantaneaS2c();
            int distintos = antes.Length == 0 ? 0 : ahora.Count(l => !antes.Contains(l));
            log.AppendLine("Materiales S2c_* revisados: " + ahora.Length + ", distintos a antes de Aplicar: " + distintos);
            if (antes.Length == 0)
            {
                fallas.Add("No hay instantánea de materiales S2c_* (" + LogBase + "): corre Aplicar.");
            }
            else if (distintos > 0)
            {
                fallas.Add(distintos + " materiales S2c_* cambiaron.");
            }
            foreach (string p in PrefabsIntocables)
            {
                string linea = baseLog.FirstOrDefault(l => l.StartsWith("md5 " + p + " "));
                bool igual = linea != null && linea == "md5 " + p + " " + Md5(CarpetaZonas + p + ".prefab");
                log.AppendLine("Prefab " + p + " intacto: " + igual);
                if (!igual)
                {
                    fallas.Add("El prefab " + p + " cambió (o falta la instantánea).");
                }
            }

            log.AppendLine();
            log.AppendLine(fallas.Count == 0 ? "RESULTADO: TODO BIEN" : "RESULTADO: " + fallas.Count + " FALLAS");
            foreach (string f in fallas)
            {
                log.AppendLine("FALLA: " + f);
                Debug.LogError("[Arte] T-1461 FALLA: " + f);
            }
            File.WriteAllText(LogValidar, log.ToString());
            Debug.Log("[Arte] " + log);
        }

        // ================================================================ Utilidades

        private static string[] InstantaneaS2c()
        {
            List<string> l = new List<string>();
            foreach (string g in AssetDatabase.FindAssets("t:Material S2c_"))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(g);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m == null || !m.name.StartsWith("S2c_"))
                {
                    continue;
                }
                string color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "";
                string brillo = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness").ToString("F3") : "";
                l.Add("s2c " + ruta + " " + color + " " + brillo);
            }
            return l.ToArray();
        }

        private static string Md5(string ruta)
        {
            if (!File.Exists(ruta))
            {
                return "falta";
            }
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            using (FileStream fs = File.OpenRead(ruta))
            {
                return System.BitConverter.ToString(md5.ComputeHash(fs));
            }
        }

        private static int ContarLuces() => Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

        private static void Quitar(GameObject go)
        {
            if (go == null)
            {
                return;
            }
            foreach (Light l in go.GetComponentsInChildren<Light>(true))
            {
                UniversalAdditionalLightData d = l.GetComponent<UniversalAdditionalLightData>();
                if (d != null)
                {
                    Object.DestroyImmediate(d);
                }
            }
            Object.DestroyImmediate(go);
        }

        private static void Carpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
                AssetDatabase.ImportAsset(ruta);
            }
        }

        private static Mesh MallaAsset(string ruta)
        {
            Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (m == null)
            {
                m = new Mesh { name = Path.GetFileNameWithoutExtension(ruta) };
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.Clear();
            return m;
        }

        private static Material HacerMaterial(string ruta, string shader)
        {
            Shader s = Shader.Find(shader);
            if (s == null)
            {
                throw new System.Exception("No se encontró el shader " + shader + " (¿se importó?)");
            }
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(s);
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.shader = s;
            return m;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Trafico.Drones;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1398: drones y naves aéreos de Nova, SKULLS, la policía y la ciudad (tráfico aéreo alto), como un solo prefab
    /// Assets/Arte/Mundo/Zonas/T1398_DronesAereos.prefab instanciado en Ciudad_Prologo. Cada uno es un contenedor con <see cref="RutaAerea"/>
    /// que lleva el prefab Dron_&lt;código&gt; de T-1397 (con <see cref="GiroRotores"/>), luces de navegación (halos del kit, sin primitivos) y, a baja altura, una Light chica sin sombras.
    /// Sin colliders. Requiere que DronesT1397.Aplicar ya haya corrido.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.DronesZonasT1398.Aplicar (luego .Validar y .Fotos). Informe en Logs/t1398-drones.txt.
    /// </summary>
    public static class DronesZonasT1398
    {
        public const string NombreRaiz = "T1398_DronesAereos";
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1398_DronesAereos.prefab";
        public const string CarpetaFotos = "docs/capturas/t-1398/";
        public const int TopeDrones = 25;
        public const float AlturaMinimaAlta = 80f;
        public const float AlturaMaximaAlta = 120f;

        private const float AmplitudBaja = 0.5f;
        private const float AmplitudAlta = 1.5f;
        private const float PeriodoOscilacion = 4.5f;
        private const float AlabeoMaximo = 14f;
        private const float AlabeoPorGiro = 0.6f;
        private const float CorteRotoresBajo = 120f;
        private const float CorteRotoresAlto = 250f;
        private const float LuzRango = 7f;
        private const float LuzIntensidad = 1.6f;
        private const float RadioLuzNavegacion = 0.12f;
        private const float RadioLuzNaveFactor = 3f;
        private const float HaloIntensidad = 1.2f;
        private const float HaloAlto = 1.4f;
        private static readonly Color ColorRojo = new Color(1f, 0.1f, 0.1f);
        private static readonly Color ColorVerde = new Color(0.1f, 1f, 0.2f);

        /// <summary>Grupos y reglas de altura (m) que Validar comprueba.</summary>
        public static readonly (string grupo, float min, float max, int cantidad)[] Reglas =
        {
            ("Nova", 15f, 90f, 6), ("Skulls", 8f, 12f, 3), ("Policia", 25f, 25f, 2), ("Ciudad", AlturaMinimaAlta, AlturaMaximaAlta, 7),
        };

        private enum Luz { Ninguna, Cian, Magenta, Roja }

        private readonly struct Dron
        {
            public readonly string Grupo, Nombre, Codigo;
            public readonly Vector2[] Puntos;
            public readonly float Altura, Velocidad, Fase;
            public readonly Luz Foco;

            public Dron(string grupo, string nombre, string codigo, Vector2[] puntos, float altura, float velocidad, float fase, Luz foco)
            {
                Grupo = grupo;
                Nombre = nombre;
                Codigo = codigo;
                Puntos = puntos;
                Altura = altura;
                Velocidad = velocidad;
                Fase = fase;
                Foco = foco;
            }
        }

        // ------------------------------------------------------------------ rutas

        // Elipse de n puntos. horario = true: giros a la derecha mirando desde arriba (x este, z norte).
        private static Vector2[] Elipse(Vector2 centro, float rx, float rz, bool horario, int n = 8)
        {
            Vector2[] r = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * i / n * (horario ? -1f : 1f) + Mathf.PI * 0.5f;
                r[i] = centro + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * rz);
            }
            return r;
        }

        private static Vector2[] Rectangulo(Vector2 min, Vector2 max, bool horario)
        {
            Vector2[] r = { new Vector2(min.x, min.y), new Vector2(min.x, max.y), new Vector2(max.x, max.y), new Vector2(max.x, min.y) };
            if (!horario)
            {
                System.Array.Reverse(r);
            }
            return r;
        }

        /// <summary>
        /// Territorios (coordenadas del mundo): SKULLS = el Tianguis, calle z = 0 entre x -40 y 40 (centro del barrio, ColocarS2Pandillas y CreadorCiudadPandillasT407).
        /// Nova = la Manzana_6 al sureste (x 54 a 134) con su calle en z = -43 y la torre al final, hacia x 130 (ColocarS2cNova). Barrio: 3 x 2 manzanas, avenidas en z = 0, +-94 y x = +-47, +-141.
        /// T-1482: la ciudad quedó en 4 barrios (Suburbios, El Mercado, Neón Alto, Nova), aprox. x -250 a 250, z -190 a 190: ninguna ruta sale de x -240..240, z -180..180.
        /// </summary>
        private static List<Dron> Plan()
        {
            Vector2 nova = new Vector2(95f, -43f);
            Vector2 tianguis = new Vector2(0f, 0f);
            Vector2 ciudad = new Vector2(-70f, -20f);
            return new List<Dron>
            {
                // Nova: patrullando su manzana y la torre (15 a 30 m), y dos naves cruzando despacio (60 a 90 m).
                new Dron("Nova", "Nova_DR02_a", "DR02_a", Elipse(nova, 45f, 22f, true), 20f, 6f, 0f, Luz.Cian),
                new Dron("Nova", "Nova_DR02_b", "DR02_b", Elipse(nova, 40f, 20f, false), 26f, 5f, 0.5f, Luz.Cian),
                new Dron("Nova", "Nova_DR04_a1", "DR04_a", Elipse(new Vector2(125f, -43f), 22f, 18f, true), 16f, 9f, 0.25f, Luz.Cian),
                new Dron("Nova", "Nova_DR04_a2", "DR04_a", Rectangulo(new Vector2(54f, -87f), new Vector2(134f, -7f), true), 29f, 10f, 0.7f, Luz.Cian),
                new Dron("Nova", "Nova_NV03_a", "NV03_a", Elipse(nova, 140f, 110f, true, 10), 70f, 12f, 0f, Luz.Ninguna),
                new Dron("Nova", "Nova_NV03_b", "NV03_b", Elipse(nova, 120f, 90f, false, 10), 90f, 10f, 0.5f, Luz.Ninguna),
                // SKULLS: muy bajo sobre el Tianguis.
                new Dron("Skulls", "Skulls_E13_1", "E13", Elipse(tianguis, 45f, 14f, true), 8f, 7f, 0f, Luz.Magenta),
                new Dron("Skulls", "Skulls_E13_2", "E13", Elipse(tianguis, 45f, 14f, true), 10f, 7f, 0.33f, Luz.Magenta),
                new Dron("Skulls", "Skulls_E13_3", "E13", Elipse(tianguis, 40f, 18f, false), 12f, 8f, 0.66f, Luz.Magenta),
                // Policía: dos DR01 a 25 m sobre las avenidas, además del dron de persecución (Trafico/Prefabs/Dron.prefab).
                new Dron("Policia", "Policia_DR01_1", "DR01", Rectangulo(new Vector2(-141f, -94f), new Vector2(141f, 94f), true), 25f, 14f, 0f, Luz.Roja),
                new Dron("Policia", "Policia_DR01_2", "DR01", Elipse(tianguis, 141f, 10f, false, 8), 25f, 12f, 0.4f, Luz.Roja),
                // Ciudad: carga y naves altas cruzando los 4 barrios (80 a 120 m). T-1482: todas las rutas dentro de x -240..240, z -180..180 (sin las zonas quitadas).
                new Dron("Ciudad", "Ciudad_DR03_1", "DR03_a", Rectangulo(new Vector2(-230f, -170f), new Vector2(230f, 170f), true), 100f, 14f, 0f, Luz.Ninguna),
                new Dron("Ciudad", "Ciudad_DR03_2", "DR03_a", Rectangulo(new Vector2(-230f, -170f), new Vector2(230f, 170f), false), 110f, 13f, 0.5f, Luz.Ninguna),
                new Dron("Ciudad", "Ciudad_DR03_3", "DR03_a", Elipse(new Vector2(-100f, 0f), 130f, 150f, true, 10), 85f, 11f, 0.2f, Luz.Ninguna),
                new Dron("Ciudad", "Ciudad_DR03_4", "DR03_a", Elipse(new Vector2(100f, -30f), 130f, 140f, false, 10), 95f, 11f, 0.6f, Luz.Ninguna),
                new Dron("Ciudad", "Ciudad_DR03_5", "DR03_a", Elipse(ciudad, 160f, 20f, true, 8), 120f, 16f, 0.3f, Luz.Ninguna),
                new Dron("Ciudad", "Ciudad_NV01_1", "NV01", Elipse(ciudad, 160f, 150f, true, 12), 110f, 8f, 0f, Luz.Ninguna),
                new Dron("Ciudad", "Ciudad_NV01_2", "NV01", Elipse(ciudad, 140f, 120f, false, 12), 100f, 8f, 0.5f, Luz.Ninguna),
            };
        }

        // ------------------------------------------------------------------ aplicar

        private static bool ConstruirPrefab(List<Dron> plan)
        {
            if (plan.Count > TopeDrones)
            {
                Debug.LogError("[Arte] T-1398: " + plan.Count + " drones pasan del tope " + TopeDrones);
                return false;
            }
            foreach (string codigo in plan.Select(d => d.Codigo).Distinct())
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(DronesT1397.RutaPrefab(codigo)) == null)
                {
                    Debug.LogError("[Arte] T-1398: falta " + DronesT1397.RutaPrefab(codigo) + ": corre DronesT1397.Aplicar primero.");
                    return false;
                }
            }
            Material rojo = MaterialHalo("T1398_HaloRojo", ColorRojo);
            Material verde = MaterialHalo("T1398_HaloVerde", ColorVerde);
            if (rojo == null || verde == null)
            {
                Debug.LogError("[Arte] T-1398: falta el shader MechaCyberpunk/Mundo/HaloLuz.");
                return false;
            }

            GameObject raiz = new GameObject(NombreRaiz);
            Dictionary<string, Transform> grupos = new Dictionary<string, Transform>();
            foreach (Dron d in plan)
            {
                if (!grupos.TryGetValue(d.Grupo, out Transform padre))
                {
                    padre = new GameObject(d.Grupo).transform;
                    padre.SetParent(raiz.transform, false);
                    grupos[d.Grupo] = padre;
                }
                CrearDron(d, padre, rojo, verde);
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);
            return true;
        }

        /// <summary>T-1482: rutas de este plan (x, z) fuera de los límites de los 4 barrios. Vacío = todo dentro. Las pruebas lo usan.</summary>
        public const float LimiteX = 240f;
        public const float LimiteZ = 180f;

        public static bool PlanDentroDeLosBarrios()
        {
            foreach (Dron d in Plan())
            {
                foreach (Vector2 p in d.Puntos)
                {
                    if (Mathf.Abs(p.x) > LimiteX || Mathf.Abs(p.y) > LimiteZ)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// T-1482: rehace solo el prefab T1398_DronesAereos.prefab (la escena no se toca: su instancia se actualiza sola al guardar el prefab).
        /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.DronesZonasT1398.SoloPrefab
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1482/Rehacer prefab de drones aéreos")]
        public static void SoloPrefab()
        {
            ColocarS2.Informe.Clear();
            if (ConstruirPrefab(Plan()))
            {
                Debug.Log("[Arte] T-1482: prefab de drones rehecho en " + RutaPrefab);
            }
        }

        [MenuItem("Mecha Cyberpunk/T-1398/Colocar drones aéreos")]
        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            List<Dron> plan = Plan();
            if (!ConstruirPrefab(plan))
            {
                return;
            }
            ColocarS2.Abrir();
            GameObject viejo = GameObject.Find(NombreRaiz);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo);
            }
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            instancia.transform.position = Vector3.zero;
            ColocarS2.Informe.AppendLine("T-1398: " + plan.Count + " drones en " + RutaPrefab + ", instanciado en la escena.");
            foreach (var g in plan.GroupBy(d => d.Grupo))
            {
                ColocarS2.Informe.AppendLine("  " + g.Key + ": " + string.Join(", ", g.Select(d => d.Codigo + "@" + d.Altura + "m")));
            }
            ColocarS2.Guardar("t1398-drones");
        }

        private static void CrearDron(Dron d, Transform padre, Material rojo, Material verde)
        {
            GameObject envoltorio = new GameObject(d.Nombre);
            envoltorio.transform.SetParent(padre, false);
            bool alto = d.Altura >= AlturaMinimaAlta;
            RutaAerea ruta = envoltorio.AddComponent<RutaAerea>();
            SerializedObject so = new SerializedObject(ruta);
            SerializedProperty lista = so.FindProperty("puntos");
            lista.arraySize = d.Puntos.Length;
            for (int i = 0; i < d.Puntos.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).vector2Value = d.Puntos[i];
            }
            so.FindProperty("altura").floatValue = d.Altura;
            so.FindProperty("velocidad").floatValue = d.Velocidad;
            so.FindProperty("amplitudOscilacion").floatValue = alto ? AmplitudAlta : AmplitudBaja;
            so.FindProperty("periodoOscilacion").floatValue = PeriodoOscilacion;
            so.FindProperty("alabeoMaximo").floatValue = AlabeoMaximo;
            so.FindProperty("alabeoPorGiro").floatValue = AlabeoPorGiro;
            so.FindProperty("fase").floatValue = d.Fase;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DronesT1397.RutaPrefab(d.Codigo)));
            modelo.name = "Dron";
            modelo.transform.SetParent(envoltorio.transform, false);
            if (modelo.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (Collider c in modelo.GetComponentsInChildren<Collider>(true))
                {
                    Object.DestroyImmediate(c);
                }
            }
            GiroRotores giro = modelo.GetComponent<GiroRotores>();
            if (giro != null)
            {
                SerializedObject sg = new SerializedObject(giro);
                sg.FindProperty("distanciaMaxima").floatValue = alto ? CorteRotoresAlto : CorteRotoresBajo;
                sg.ApplyModifiedPropertiesWithoutUndo();
            }
            // Luces de navegación a los lados del modelo (rojo a la izquierda, verde a la derecha), en la mitad de su altura.
            Bounds b = Limites(modelo);
            float radio = RadioLuzNavegacion * (d.Codigo.StartsWith("NV") ? RadioLuzNaveFactor : 1f);
            Vector3 izq = envoltorio.transform.InverseTransformPoint(new Vector3(b.min.x, b.center.y, b.center.z));
            Vector3 der = envoltorio.transform.InverseTransformPoint(new Vector3(b.max.x, b.center.y, b.center.z));
            CrearHalo(envoltorio.transform, "LuzNavegacion_Izq", izq, radio, ColorRojo, rojo);
            CrearHalo(envoltorio.transform, "LuzNavegacion_Der", der, radio, ColorVerde, verde);
            if (d.Foco != Luz.Ninguna)
            {
                GameObject go = new GameObject("Foco");
                go.transform.SetParent(envoltorio.transform, false);
                go.transform.localPosition = new Vector3(0f, b.min.y - radio, 0f);
                Light l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = LuzRango;
                l.intensity = LuzIntensidad;
                l.shadows = LightShadows.None;
                l.color = d.Foco == Luz.Cian ? new Color(0.3f, 0.9f, 1f) : d.Foco == Luz.Magenta ? new Color(1f, 0.1f, 0.8f) : new Color(1f, 0.15f, 0.1f);
            }
            // Posición de arranque (para verlo en el editor): la misma que calcula RutaAerea en Awake.
            CalculoRutaAerea calculo = new CalculoRutaAerea(d.Puntos, d.Altura, d.Velocidad, 0f, PeriodoOscilacion, AlabeoMaximo, AlabeoPorGiro);
            MuestraRutaAerea m = calculo.Muestra(0f, d.Fase);
            envoltorio.transform.localPosition = m.Posicion;
            envoltorio.transform.localRotation = Quaternion.LookRotation(m.Rumbo, Vector3.up);
        }

        // Luz de navegación como halo del kit (KitS2c.Halo, shader HaloLuz): un cono aditivo, sin primitivos. El tamaño crece con el del dron.
        private static void CrearHalo(Transform padre, string nombre, Vector3 pos, float radio, Color color, Material mat)
        {
            float alto = HaloAlto * radio / RadioLuzNavegacion;
            Vector3 mundo = padre.TransformPoint(pos);
            KitS2c.Halo(null, padre, mundo + Vector3.up * alto * 0.5f, mundo.y - alto * 0.5f, color);
            Transform h = padre.GetChild(padre.childCount - 1);
            h.name = nombre;
            MeshRenderer mr = h.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private static Material MaterialHalo(string nombre, Color color)
        {
            Shader shader = Shader.Find("MechaCyberpunk/Mundo/HaloLuz");
            if (shader == null)
            {
                return null;
            }
            return KitS2c.Mat(nombre, () =>
            {
                Material m = new Material(shader);
                m.SetColor("_Color", color);
                m.SetFloat("_Intensidad", HaloIntensidad);
                m.SetFloat("_Borde", 3.4f);
                m.SetFloat("_Caida", 2f);
                m.SetTexture("_Ruido", KitS2c.TexMuro()); // T-1412: el polvo de la niebla (con textura no cuenta como material liso en SinGreyboxT506)
                return m;
            });
        }

        private static Bounds Limites(GameObject objeto)
        {
            Renderer[] rs = objeto.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                return new Bounds(objeto.transform.position, Vector3.one);
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ------------------------------------------------------------------ validar

        [MenuItem("Mecha Cyberpunk/T-1398/Validar drones aéreos")]
        public static void Validar()
        {
            StringBuilder sb = new StringBuilder();
            int fallas = 0;
            void Falla(string t) { fallas++; sb.AppendLine("FALTA: " + t); }

            ColocarS2.Abrir();
            GameObject raiz = GameObject.Find(NombreRaiz);
            if (raiz == null)
            {
                Falla("no hay " + NombreRaiz + " en la escena");
            }
            else
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(raiz))
                {
                    Falla("la raíz no es instancia del prefab");
                }
                RutaAerea[] rutas = raiz.GetComponentsInChildren<RutaAerea>(true);
                sb.AppendLine("Drones: " + rutas.Length + " (tope " + TopeDrones + ")");
                if (rutas.Length > TopeDrones)
                {
                    Falla("más de " + TopeDrones + " drones");
                }
                int luces = 0, lodGroups = 0;
                foreach (var regla in Reglas)
                {
                    Transform g = raiz.transform.Find(regla.grupo);
                    RutaAerea[] del = g != null ? g.GetComponentsInChildren<RutaAerea>(true) : new RutaAerea[0];
                    sb.AppendLine(regla.grupo + ": " + del.Length + " drones, alturas " + string.Join(", ", del.Select(r => r.Altura.ToString("F0"))));
                    if (del.Length != regla.cantidad)
                    {
                        Falla(regla.grupo + " tiene " + del.Length + " y se esperaban " + regla.cantidad);
                    }
                    foreach (RutaAerea r in del)
                    {
                        if (r.Altura < regla.min - 0.01f || r.Altura > regla.max + 0.01f)
                        {
                            Falla(r.name + " a " + r.Altura + " m fuera de " + regla.min + "-" + regla.max);
                        }
                    }
                }
                foreach (RutaAerea r in rutas)
                {
                    if (r.CantidadPuntos < 3)
                    {
                        Falla(r.name + " con menos de 3 puntos");
                    }
                    if (r.GetComponentsInChildren<Collider>(true).Length > 0)
                    {
                        Falla(r.name + " tiene collider");
                    }
                    GiroRotores giro = r.GetComponentInChildren<GiroRotores>(true);
                    if (giro == null)
                    {
                        Falla(r.name + " sin GiroRotores");
                    }
                    else
                    {
                        giro.Buscar();
                        if (giro.CantidadRotores == 0)
                        {
                            Falla(r.name + " sin rotores Rotor_*");
                        }
                        if (giro.DistanciaMaxima <= 0f)
                        {
                            Falla(r.name + " sin corte de giro por distancia");
                        }
                    }
                    Light[] ls = r.GetComponentsInChildren<Light>(true);
                    luces += ls.Length;
                    if (ls.Length > 1 || ls.Any(l => l.shadows != LightShadows.None))
                    {
                        Falla(r.name + ": más de 1 Light o con sombras");
                    }
                    bool tieneEmisiva = r.GetComponentsInChildren<Renderer>(true).Any(x => x.name.StartsWith("LuzNavegacion") && x.sharedMaterial != null && x.sharedMaterial.shader.name == "MechaCyberpunk/Mundo/HaloLuz");
                    if (!tieneEmisiva)
                    {
                        Falla(r.name + " sin halo de luz de navegación");
                    }
                    if (r.GetComponentsInChildren<LODGroup>(true).Length > 0)
                    {
                        lodGroups++;
                    }
                    Bounds b = Limites(r.gameObject);
                    sb.AppendLine("  " + r.name + ": tamaño " + b.size.ToString("F1") + " m, puntos " + r.CantidadPuntos + ", " + r.Velocidad + " m/s");
                }
                sb.AppendLine("Lights: " + luces + ", modelos con LODGroup: " + lodGroups + " (los FBX de T-1397 no traen LOD si es 0)");
            }
            sb.AppendLine(fallas == 0 ? "RESULTADO: todo bien" : "RESULTADO: " + fallas + " fallas");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1398-validar.txt", sb.ToString());
            Debug.Log("[Arte] T-1398 validar:\n" + sb);
            if (fallas > 0)
            {
                Debug.LogError("[Arte] T-1398: " + fallas + " fallas, ver Logs/t1398-validar.txt");
            }
        }

        // ------------------------------------------------------------------ fotos

        [MenuItem("Mecha Cyberpunk/T-1398/Fotos de drones aéreos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory(CarpetaFotos);
            GameObject raiz = GameObject.Find(NombreRaiz);
            if (raiz == null)
            {
                Debug.LogError("[Arte] T-1398: no hay " + NombreRaiz + " en la escena: corre Aplicar.");
                return;
            }
            // Un dron por grupo y uno por modelo distinto: la cámara se pone detrás y arriba, mirándolo.
            HashSet<string> vistos = new HashSet<string>();
            foreach (RutaAerea r in raiz.GetComponentsInChildren<RutaAerea>(true))
            {
                string clave = r.transform.parent.name + "_" + r.GetComponentInChildren<GiroRotores>(true).transform.GetChild(0).name;
                if (!vistos.Add(clave))
                {
                    continue;
                }
                Bounds b = Limites(r.gameObject);
                float distancia = Mathf.Clamp(b.extents.magnitude * 5f, 12f, 120f);
                Vector3 desde = b.center - r.transform.forward * distancia + Vector3.up * distancia * 0.25f + r.transform.right * distancia * 0.3f;
                ColocarS2cLook.Foto(desde, b.center, CarpetaFotos + clave + ".png", 45f);
            }
            Debug.Log("[Arte] T-1398: fotos en " + CarpetaFotos);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1542: los NPC civiles de Ciudad_Prologo según su barrio (Suburbios, El Mercado, Neón Alto, Nova). Hace cuatro cosas, todas repetibles:
    ///   1. Los que platican se miran de frente (los pares Gente_Platica_*, que T-508 dejó con un giro de 14 grados hacia la calle).
    ///   2. Reparte los NP07 a NP14 entre la gente de banqueta ya puesta (Gente_*) según el barrio donde está (<see cref="Reparto"/>).
    ///   3. Vendedores detrás de los 11 puestos del tianguis (de frente a la calle, con NPC_Esperar o NPC_Platicar_1), una familia en la plaza K09
    ///      (parados, platicando en pareja y trío, paseando), un corro en la tiendita, y gente nueva de banqueta donde un barrio quedaba pelón
    ///      (corros de dos y de tres, y solos), siempre sobre banqueta, libres de edificios y fuera del tráfico.
    ///   4. Civiles sentados en las bancas PZ01 con NPC_Sentado_Banca (con el reparto del barrio de cada banca).
    /// Lo nuevo cuelga de <see cref="Grupo"/> (se rehace entero cada vez). NP01 a NP06 todavía no tienen rig (solo el zip de Tripo): <see cref="Diagnostico"/> lo avisa
    /// y, en cuanto exista su Visual_, <see cref="ReparticionExtra"/> los mete solos en el reparto.
    /// En este orden: Diagnostico (no cambia nada), Aplicar. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.NpcsBarriosT1542.&lt;Método&gt;
    /// Si se rehace T-508 (ColocarT508.Colocar) o se corre ColocarT510, vuelve a correr Aplicar. Informe en Logs/t1542-npcs.txt.
    /// </summary>
    public static class NpcsBarriosT1542
    {
        public const string Etiqueta = "[T-1542] ";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaInforme = "Logs/t1542-npcs.txt";
        public const string Grupo = "T1542_NpcsBarrio";
        public const string PrefijoSentado = "Sentado_T1542_";

        /// <summary>Los cuatro barrios de la ciudad (T-1482). Fuera = las afueras, detrás del muro.</summary>
        public enum Barrio { Fuera, Suburbios, Mercado, NeonAlto, Nova }

        // ------------------------------------------------------------------ el mapa (medido en Ciudad_Prologo)

        /// <summary>Neón Alto y Nova empiezan en x = 47 (T-1107, T-418); El Mercado va de x = -47 a 47 y el tianguis está en z entre -8 y 8; Suburbios queda al oeste.</summary>
        public const float LimiteOeste = -47f;
        public const float LimiteEste = 47f;
        /// <summary>La avenida este-oeste va en z = -43 (T-1463): al este y al sur de ella está Nova, y al sur del Mercado (taller, fonda) siguen los Suburbios.</summary>
        public const float LimiteNova = -25f;
        public const float LimiteSurMercado = -45f;
        public const float LimiteNorte = 120f;
        public const float LimiteSur = -140f;
        public const float LimiteOesteCiudad = -140f;
        public const float LimiteEsteCiudad = 150f;

        /// <summary>Centro del kiosco de la plaza K09 (ColocarT1320Calles.CentroKiosco).</summary>
        public static readonly Vector3 CentroPlaza = new Vector3(-75.5f, 0f, -29.5f);

        public static Barrio BarrioDe(Vector3 p)
        {
            if (p.x < LimiteOesteCiudad || p.x > LimiteEsteCiudad || p.z > LimiteNorte || p.z < LimiteSur)
            {
                return Barrio.Fuera;
            }
            if (p.x < LimiteOeste)
            {
                return Barrio.Suburbios;
            }
            if (p.x < LimiteEste)
            {
                return p.z < LimiteSurMercado ? Barrio.Suburbios : Barrio.Mercado;
            }
            return p.z < LimiteNova ? Barrio.Nova : Barrio.NeonAlto;
        }

        // ------------------------------------------------------------------ el reparto

        /// <summary>
        /// Quién sale en cada barrio y con qué peso (nombres sin "Visual_"). Suburbios y Mercado: estibador, pescador, repartidor, elotero y abuela;
        /// Neón Alto y Nova: oficinista, turista y corredor. El repartidor sale en los cuatro (su trabajo es ir a todos lados).
        /// </summary>
        public static readonly Dictionary<Barrio, (string visual, float peso)[]> Reparto = new Dictionary<Barrio, (string, float)[]>
        {
            { Barrio.Suburbios, new[] { ("NP11_Estibador_a", 3f), ("NP11_Estibador_b", 3f), ("NP07_Pescador_a", 2f), ("NP07_Pescador_b", 2f), ("NP13_Repartidor_a", 2f), ("NP13_Repartidor_b", 2f), ("NP10_Elotero_a", 2f), ("NP12_Abuela_a", 3f) } },
            { Barrio.Mercado, new[] { ("NP10_Elotero_a", 3f), ("NP12_Abuela_a", 2f), ("NP07_Pescador_a", 2f), ("NP07_Pescador_b", 2f), ("NP13_Repartidor_a", 2f), ("NP13_Repartidor_b", 2f), ("NP11_Estibador_a", 2f), ("NP08_Turista_a", 1f) } },
            { Barrio.NeonAlto, new[] { ("NP14_Oficinista_a", 3f), ("NP14_Oficinista_b", 3f), ("NP08_Turista_a", 2f), ("NP08_Turista_b", 2f), ("NP09_Corredor_a", 2f), ("NP09_Corredor_b", 2f), ("NP13_Repartidor_a", 1f) } },
            { Barrio.Nova, new[] { ("NP14_Oficinista_a", 3f), ("NP14_Oficinista_b", 3f), ("NP09_Corredor_a", 3f), ("NP09_Corredor_b", 3f), ("NP08_Turista_a", 1f), ("NP13_Repartidor_b", 1f) } },
        };

        /// <summary>NP01 a NP06 (solo con zip de Tripo hasta el 21-nov): el barrio donde van y su peso, y se usan solo si ya existe su Visual_.</summary>
        public static readonly (string visual, Barrio barrio, float peso)[] ReparticionExtra =
        {
            ("NP01_Senora", Barrio.Suburbios, 2f), ("NP01_Senora", Barrio.Mercado, 2f),
            ("NP02_Trabajador", Barrio.Suburbios, 2f), ("NP02_Trabajador", Barrio.Mercado, 2f),
            ("NP03_Joven", Barrio.Mercado, 2f), ("NP03_Joven", Barrio.NeonAlto, 2f),
            ("NP04_Senor", Barrio.Suburbios, 2f), ("NP04_Senor", Barrio.Mercado, 2f),
            ("NP05_Vendedor", Barrio.Mercado, 2f),
            ("NP06_ChicaPunk", Barrio.NeonAlto, 2f), ("NP06_ChicaPunk", Barrio.Nova, 2f),
        };

        /// <summary>Los que se paran detrás de los 11 puestos del tianguis, en orden de número de puesto.</summary>
        public static readonly string[] VendedoresTianguis =
        {
            "NP10_Elotero_a", "NP12_Abuela_a", "NP07_Pescador_a", "NP13_Repartidor_a", "NP11_Estibador_b", "NP10_Elotero_a",
            "NP07_Pescador_b", "NP12_Abuela_a", "NP13_Repartidor_b", "NP10_Elotero_a", "NP11_Estibador_a",
        };

        /// <summary>El elenco de la plaza K09 (el kiosco, las bancas y los puestos de comida de la referencia K24): gente del Mercado y de los Suburbios.</summary>
        public static readonly string[] ElencoPlaza = { "NP10_Elotero_a", "NP12_Abuela_a", "NP07_Pescador_a", "NP13_Repartidor_a", "NP11_Estibador_a", "NP08_Turista_a", "NP07_Pescador_b" };

        /// <summary>Cuántos corros de dos, de tres y solos se agregan donde hay banqueta libre en cada barrio.</summary>
        public static readonly Dictionary<Barrio, (int parejas, int trios, int solos)> Cantidad = new Dictionary<Barrio, (int, int, int)>
        {
            { Barrio.Suburbios, (3, 0, 3) },
            { Barrio.Mercado, (2, 0, 2) },
            { Barrio.NeonAlto, (4, 1, 3) },
            { Barrio.Nova, (4, 1, 3) },
        };

        /// <summary>Elige un visual del barrio con la tirada (0 a 1). <paramref name="existe"/> deja fuera los que no tienen prefab. Null si el barrio no tiene reparto.</summary>
        public static string ElegirVisual(Barrio barrio, float tirada, Func<string, bool> existe = null)
        {
            var lista = new List<(string visual, float peso)>();
            if (Reparto.TryGetValue(barrio, out var propios))
            {
                lista.AddRange(propios);
            }
            foreach ((string visual, Barrio donde, float peso) in ReparticionExtra)
            {
                if (donde == barrio && existe != null && existe(visual))
                {
                    lista.Add((visual, peso));
                }
            }
            if (existe != null)
            {
                lista = lista.Where(e => existe(e.visual)).ToList();
            }
            float total = lista.Sum(e => e.peso);
            if (lista.Count == 0 || total <= 0f)
            {
                return null;
            }
            float objetivo = Mathf.Clamp01(tirada) * total;
            float acumulado = 0f;
            foreach ((string visual, float peso) in lista)
            {
                acumulado += peso;
                if (objetivo < acumulado)
                {
                    return visual;
                }
            }
            return lista[lista.Count - 1].visual;
        }

        // ------------------------------------------------------------------ geometría (lógica pura)

        public const float SeparacionPareja = 0.9f;
        public const float RadioTrio = 0.6f;
        public const float DistanciaVendedorAlPuesto = 0.9f;
        public const float ToleranciaDeFrente = 8f;

        /// <summary>Giro en Y (grados) que deja a quien está en <paramref name="desde"/> de cara a <paramref name="hacia"/>.</summary>
        public static float YawHacia(Vector3 desde, Vector3 hacia)
        {
            Vector3 d = hacia - desde;
            d.y = 0f;
            return d.sqrMagnitude < 1e-6f ? 0f : Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        /// <summary>Dos personas separadas <paramref name="separacion"/> a lo largo de <paramref name="tangente"/>, cada una de cara a la otra.</summary>
        public static (Vector3 a, float yawA, Vector3 b, float yawB) Pareja(Vector3 centro, Vector3 tangente, float separacion = SeparacionPareja)
        {
            Vector3 t = new Vector3(tangente.x, 0f, tangente.z);
            t = t.sqrMagnitude < 1e-6f ? Vector3.right : t.normalized;
            Vector3 a = centro - t * (separacion * 0.5f);
            Vector3 b = centro + t * (separacion * 0.5f);
            return (a, YawHacia(a, b), b, YawHacia(b, a));
        }

        /// <summary>Un corro de <paramref name="cuantos"/> personas en círculo de radio <paramref name="radio"/>, todas de cara al centro.</summary>
        public static List<(Vector3 pos, float yaw)> Corro(Vector3 centro, int cuantos, float radio, float giroInicial = 0f)
        {
            var salida = new List<(Vector3, float)>();
            for (int i = 0; i < cuantos; i++)
            {
                float rad = (giroInicial + 360f / Mathf.Max(1, cuantos) * i) * Mathf.Deg2Rad;
                Vector3 p = centro + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radio;
                salida.Add((p, YawHacia(p, centro)));
            }
            return salida;
        }

        /// <summary>Dónde se para el vendedor de un puesto: detrás de la mesa (lejos de la calle) y de frente a la calle. La calle del tianguis va en z = 0.</summary>
        public static (Vector3 pos, float yaw) Vendedor(Vector3 puesto, float distancia = DistanciaVendedorAlPuesto)
        {
            Vector3 haciaCalle = puesto.z >= 0f ? Vector3.back : Vector3.forward;
            Vector3 pos = new Vector3(puesto.x, puesto.y, puesto.z) - haciaCalle * distancia;
            return (pos, YawHacia(pos, pos + haciaCalle));
        }

        /// <summary>True si A mira hacia B y B hacia A con menos de <paramref name="tolerancia"/> grados de error.</summary>
        public static bool SeMiranDeFrente(Vector3 posA, float yawA, Vector3 posB, float yawB, float tolerancia = ToleranciaDeFrente)
        {
            return Mathf.Abs(Mathf.DeltaAngle(yawA, YawHacia(posA, posB))) <= tolerancia && Mathf.Abs(Mathf.DeltaAngle(yawB, YawHacia(posB, posA))) <= tolerancia;
        }

        /// <summary>Un número estable (0 a 1) a partir de un texto, para que cada corrida reparta igual.</summary>
        public static float Azar(string clave)
        {
            uint h = 2166136261u;
            foreach (char c in clave)
            {
                h = (h ^ c) * 16777619u;
            }
            return (h % 10000u) / 10000f;
        }

        // ------------------------------------------------------------------ estado de la corrida

        private static readonly StringBuilder Informe = new StringBuilder();
        private static readonly List<Bounds> Ocupado = new List<Bounds>();
        private static readonly Dictionary<string, int> Colocados = new Dictionary<string, int>();
        private const float RadioPersona = 0.4f;
        private const float AltoPersona = 1.8f;
        private const float SeparacionEntreCorros = 14f;
        private const float DistanciaAOtraGente = 3.5f;
        private const float PasoMalla = 2f;
        private const float AlturaBanquetaMin = 0.1f;
        private const float AlturaBanquetaMax = 0.35f;
        private const float AlturaTecho = 4f;
        private const float FraccionRepartida = 0.7f;

        private static bool Existe(string visual) => AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual)) != null;

        private static void Suma(string clave) => Colocados[clave] = (Colocados.TryGetValue(clave, out int n) ? n : 0) + 1;

        // ------------------------------------------------------------------ Diagnostico

        [MenuItem("Mecha Cyberpunk/T-1542/1 Diagnostico de NPC por barrio")]
        public static void Diagnostico()
        {
            var salida = new StringBuilder();
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            salida.AppendLine("T-1542 diagnóstico de los NPC civiles en " + RutaEscena);

            salida.AppendLine();
            salida.AppendLine("NP01 a NP06 (¿ya llegaron con rig?):");
            foreach (var extra in ReparticionExtra.GroupBy(e => e.visual).Select(g => g.First()))
            {
                string visual = extra.visual;
                string codigo = visual.Substring(0, 4);
                bool carpeta = AssetDatabase.FindAssets(codigo, new[] { "Assets/Arte/Personajes" }).Length > 0;
                salida.AppendLine("  " + visual + ": carpeta en Assets/Arte/Personajes " + (carpeta ? "SI" : "no") + ", Visual_ " + (Existe(visual) ? "SI" : "no (sin rig: queda fuera del reparto)"));
            }
            salida.AppendLine("NP07 a NP14 con Visual_: " + string.Join(", ", Reparto.Values.SelectMany(v => v).Select(e => e.visual).Distinct().OrderBy(n => n).Select(n => n + (Existe(n) ? "" : " (FALTA)"))));

            var conteo = new SortedDictionary<string, int>();
            var porBarrioRol = new SortedDictionary<string, int>();
            int total = 0;
            foreach (Transform t in TodasLasRaicesDeVisual(escena))
            {
                string tipo = TipoDe(t);
                Barrio b = BarrioDe(t.position);
                string rol = RolDe(t);
                conteo[b + " | " + tipo] = (conteo.TryGetValue(b + " | " + tipo, out int n) ? n : 0) + 1;
                porBarrioRol[b + " | " + rol] = (porBarrioRol.TryGetValue(b + " | " + rol, out int m) ? m : 0) + 1;
                total++;
            }
            salida.AppendLine();
            salida.AppendLine("Civiles en la escena: " + total);
            salida.AppendLine("Por barrio y tipo:");
            foreach (var kv in conteo) { salida.AppendLine("  " + kv.Key + " = " + kv.Value); }
            salida.AppendLine("Por barrio y papel:");
            foreach (var kv in porBarrioRol) { salida.AppendLine("  " + kv.Key + " = " + kv.Value); }

            salida.AppendLine();
            salida.AppendLine("Parejas que platican (T-508, " + TodasLasRaicesDeVisual(escena).Count(t => t.name.StartsWith("Gente_Platica_")) + " personas):");
            int mal = 0;
            foreach ((Transform a, Transform b) in ParejasQuePlatican(escena))
            {
                float yawA = a.eulerAngles.y;
                float yawB = b.eulerAngles.y;
                bool ok = SeMiranDeFrente(a.position, yawA, b.position, yawB);
                mal += ok ? 0 : 1;
                float errA = Mathf.Abs(Mathf.DeltaAngle(yawA, YawHacia(a.position, b.position)));
                float errB = Mathf.Abs(Mathf.DeltaAngle(yawB, YawHacia(b.position, a.position)));
                salida.AppendLine("  " + a.name + " <-> " + b.name + " error " + errA.ToString("0") + " / " + errB.ToString("0") + " grados " + (ok ? "OK" : "NO SE MIRAN"));
            }
            salida.AppendLine("  pares que no se miran de frente: " + mal);

            salida.AppendLine();
            List<Transform> bancas = ClipsNpcPlaticarT1516.BuscarBancas(escena);
            int sentados = bancas.Sum(bn => bn.Cast<Transform>().Count(h => h.GetComponent<NpcSentadoBanca>() != null));
            salida.AppendLine("Bancas PZ01: " + bancas.Count + ", civiles sentados: " + sentados);
            foreach (Transform bn in bancas)
            {
                salida.AppendLine("  " + bn.name + " " + bn.position.ToString("F1") + " (" + BarrioDe(bn.position) + "): " + bn.Cast<Transform>().Count(h => h.GetComponent<NpcSentadoBanca>() != null) + " sentados");
            }

            int puestos = PuestosDelTianguis(escena).Count;
            int vendedores = TodasLasRaicesDeVisual(escena).Count(t => t.name.StartsWith("Vendedor_"));
            salida.AppendLine("Puestos del tianguis: " + puestos + ", vendedores de T-1542 detrás: " + vendedores);
            salida.AppendLine("Grupo " + Grupo + ": " + (escena.GetRootGameObjects().Any(g => g.name == Grupo) ? "ya está" : "falta (corre Aplicar)"));

            Escribir(salida.ToString());
            Debug.Log(Etiqueta + "diagnóstico: " + total + " civiles, " + mal + " pares sin mirarse, " + sentados + " sentados, " + vendedores + "/" + puestos + " puestos con vendedor. Informe en " + RutaInforme);
        }

        // ------------------------------------------------------------------ Aplicar

        [MenuItem("Mecha Cyberpunk/T-1542/2 Aplicar NPC por barrio")]
        public static void Aplicar()
        {
            Informe.Clear();
            Ocupado.Clear();
            Colocados.Clear();
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);

            GameObject viejo = escena.GetRootGameObjects().FirstOrDefault(g => g.name == Grupo);
            if (viejo != null)
            {
                UnityEngine.Object.DestroyImmediate(viejo);
            }
            Physics.SyncTransforms();
            ColocarNoche.ReunirObstaculos();

            Transform raiz = new GameObject(Grupo).transform;
            var padres = new Dictionary<Barrio, Transform>();
            foreach (Barrio b in new[] { Barrio.Suburbios, Barrio.Mercado, Barrio.NeonAlto, Barrio.Nova })
            {
                padres[b] = new GameObject(b.ToString()).transform;
                padres[b].SetParent(raiz, false);
            }

            int corregidos = CorregirPlaticas(escena);
            int repartidos = RepartirGenteExistente(escena);
            int vendedores = PonerVendedores(escena, padres[Barrio.Mercado]);
            int plaza = PonerPlaza(raiz);
            int tiendita = PonerTiendita(escena, raiz);
            int banqueta = PonerEnBanquetas(padres);
            int sentados = SentarEnBancas(escena);

            Informe.AppendLine();
            Informe.AppendLine("pares corregidos=" + corregidos + " repartidos(gente existente)=" + repartidos + " vendedores=" + vendedores + " plaza=" + plaza
                + " tiendita=" + tiendita + " banqueta=" + banqueta + " sentados=" + sentados);
            foreach (var kv in Colocados.OrderBy(k => k.Key)) { Informe.AppendLine("  " + kv.Key + " = " + kv.Value); }

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Escribir(Informe.ToString());
            Debug.Log(Etiqueta + "aplicado: pares corregidos " + corregidos + ", repartidos " + repartidos + ", vendedores " + vendedores + ", plaza " + plaza + ", tiendita " + tiendita
                + ", banqueta " + banqueta + ", sentados " + sentados + ". Informe en " + RutaInforme);
        }

        // ------------------------------------------------------------------ 1. los que platican se miran

        /// <summary>Los pares Gente_Platica_zona_n y ..._nb (el más cercano con el sufijo, a menos de 1.5 m; los nombres se repiten entre calles).</summary>
        public static List<(Transform a, Transform b)> ParejasQuePlatican(Scene escena)
        {
            var gente = TodosLosTransforms(escena).Where(t => t.name.StartsWith("Gente_Platica_")).ToList();
            var pares = new List<(Transform, Transform)>();
            foreach (Transform a in gente.Where(t => !t.name.EndsWith("b")))
            {
                Transform b = gente.Where(o => o.name == a.name + "b" && Vector3.Distance(o.position, a.position) < 1.5f).OrderBy(o => Vector3.Distance(o.position, a.position)).FirstOrDefault();
                if (b != null)
                {
                    pares.Add((a, b));
                }
            }
            return pares;
        }

        private static bool Camina(Transform t)
        {
            GenteDeBanqueta g = t.GetComponentInChildren<GenteDeBanqueta>(true);
            return g != null && g.Modo == ModoGente.Caminar;
        }

        private static int CorregirPlaticas(Scene escena)
        {
            int n = 0;
            foreach ((Transform a, Transform b) in ParejasQuePlatican(escena))
            {
                if (Camina(a) || Camina(b))
                {
                    Informe.AppendLine("pareja camina, no se gira: " + a.name);
                    continue;
                }
                float yawA = YawHacia(a.position, b.position);
                float yawB = YawHacia(b.position, a.position);
                a.rotation = Quaternion.Euler(0f, yawA, 0f);
                b.rotation = Quaternion.Euler(0f, yawB, 0f);
                n++;
                Suma("pareja_de_frente");
                Informe.AppendLine("de frente " + a.name + " " + a.position.ToString("F1") + " <-> " + b.name);
            }
            return n;
        }

        // ------------------------------------------------------------------ 2. el reparto de la gente que ya estaba

        private static int RepartirGenteExistente(Scene escena)
        {
            int total = 0;
            var candidatos = TodasLasRaicesDeVisual(escena).Where(t => t.name.StartsWith("Gente_")).OrderBy(t => t.name).ThenBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
            foreach (Transform t in candidatos)
            {
                GenteDeBanqueta g = t.GetComponent<GenteDeBanqueta>();
                string tipo = TipoDe(t);
                if (g == null || g.Modo == ModoGente.Caminar || tipo.StartsWith("NP") || tipo == "RobotHombre")
                {
                    continue; // los que caminan van en un envoltorio y los robots son de T-510
                }
                Barrio barrio = BarrioDe(t.position);
                if (barrio == Barrio.Fuera || Azar("reparto" + t.name + Mathf.RoundToInt(t.position.x) + "," + Mathf.RoundToInt(t.position.z)) >= FraccionRepartida)
                {
                    continue;
                }
                string visual = ElegirVisual(barrio, Azar("visual" + t.name + t.position.x.ToString("F1") + t.position.z.ToString("F1")), Existe);
                GameObject prefab = visual != null ? AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual)) : null;
                if (prefab == null)
                {
                    continue;
                }
                GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t.parent);
                nuevo.name = t.name;
                nuevo.transform.SetPositionAndRotation(t.position, t.rotation);
                ColocarT510.ConfigurarDeFondo(nuevo);
                GenteDeBanqueta c = nuevo.GetComponent<GenteDeBanqueta>();
                if (c == null)
                {
                    c = nuevo.AddComponent<GenteDeBanqueta>();
                }
                c.Configurar(g.Modo, g.Fase, g.Ritmo, g.ClipPlatica, Vector3.zero, Vector3.zero, 1f, 0f, null);
                UnityEngine.Object.DestroyImmediate(t.gameObject);
                total++;
                Suma("repartido_" + barrio + "_" + visual);
            }
            return total;
        }

        // ------------------------------------------------------------------ 3a. vendedores del tianguis

        public static List<Transform> PuestosDelTianguis(Scene escena)
        {
            return TodosLosTransforms(escena)
                .Where(t => t.name.StartsWith("Puesto_") && t.parent != null && t.parent.name == "Puestos" && t.parent.parent != null && t.parent.parent.name == "Tianguis"
                    && int.TryParse(t.name.Substring("Puesto_".Length), out _))
                .OrderBy(t => int.Parse(t.name.Substring("Puesto_".Length))).ToList();
        }

        private static int PonerVendedores(Scene escena, Transform padreMercado)
        {
            Transform padre = new GameObject("Vendedores_Tianguis").transform;
            padre.SetParent(padreMercado, false);
            AnimationClip esperar = ClipsNpcNovaT1506.CargarClip("NPC_Esperar");
            AnimationClip pregonar = ClipsNpcNovaT1506.CargarClip("NPC_Platicar_1");
            if (esperar == null || pregonar == null)
            {
                Informe.AppendLine("FALTA NPC_Esperar o NPC_Platicar_1: los vendedores quedan sin clip");
            }
            int n = 0;
            List<Transform> puestos = PuestosDelTianguis(escena);
            for (int i = 0; i < puestos.Count; i++)
            {
                Transform puesto = puestos[i];
                (Vector3 pos, float yaw) = Vendedor(puesto.position);
                float suelo = SueloCualquiera(pos);
                if (float.IsNaN(suelo))
                {
                    Informe.AppendLine("sin piso detrás de " + puesto.name + " " + pos.ToString("F1"));
                    continue;
                }
                pos.y = suelo;
                string visual = VendedoresTianguis[i % VendedoresTianguis.Length];
                AnimationClip clip = i % 3 == 2 ? pregonar : esperar; // uno de cada tres pregona
                GameObject go = Poner(padre, visual, pos, yaw, "Vendedor_" + puesto.name, ModoGente.Platicar, clip, Azar("vend" + puesto.name), true);
                if (go != null)
                {
                    n++;
                    Suma("vendedor_" + visual);
                    Informe.AppendLine("vendedor " + visual + " en " + puesto.name + " " + pos.ToString("F1") + " yaw " + yaw.ToString("0") + " " + (clip != null ? clip.name : "sin clip"));
                }
            }
            return n;
        }

        // ------------------------------------------------------------------ 3b. plaza K09

        private static int PonerPlaza(Transform raiz)
        {
            Transform padre = new GameObject("Plaza_K09").transform;
            padre.SetParent(raiz, false);
            AnimationClip esperar = ClipsNpcNovaT1506.CargarClip("NPC_Esperar");
            AnimationClip plat1 = ClipsNpcNovaT1506.CargarClip("NPC_Platicar_1");
            AnimationClip plat2 = ClipsNpcNovaT1506.CargarClip("NPC_Platicar_2");
            int n = 0;
            int e = 0;

            // Pareja platicando a las 0:00 del kiosco, trío a las 6:00, uno parado a las 3:00 y dos paseando a las 9:00 y 10:30 (las bancas están en las diagonales).
            foreach (float radio in new[] { 3.4f, 3.9f, 2.9f })
            {
                Vector3 centro = CentroPlaza + new Vector3(0f, 0f, radio);
                var p = Pareja(centro, Vector3.right);
                if (LibreSobreSuelo(p.a, out Vector3 a) && LibreSobreSuelo(p.b, out Vector3 b))
                {
                    Poner(padre, ElencoPlaza[e++ % ElencoPlaza.Length], a, p.yawA, "Plaza_Platica_a", ModoGente.Platicar, plat1, Azar("pla1"), false);
                    Poner(padre, ElencoPlaza[e++ % ElencoPlaza.Length], b, p.yawB, "Plaza_Platica_b", ModoGente.Platicar, plat2, Azar("pla2"), false);
                    n += 2;
                    Suma("plaza_pareja");
                    break;
                }
            }
            foreach (float radio in new[] { 3.6f, 4.1f, 3.1f })
            {
                Vector3 centro = CentroPlaza + new Vector3(0f, 0f, -radio);
                var corro = Corro(centro, 3, RadioTrio, 90f);
                if (corro.All(c => LibreSobreSuelo(c.pos, out Vector3 _)))
                {
                    for (int i = 0; i < corro.Count; i++)
                    {
                        LibreSobreSuelo(corro[i].pos, out Vector3 donde);
                        Poner(padre, ElencoPlaza[e++ % ElencoPlaza.Length], donde, corro[i].yaw, "Plaza_Trio_" + i, ModoGente.Platicar, i % 2 == 0 ? plat1 : plat2, Azar("tri" + i), false);
                        n++;
                    }
                    Suma("plaza_trio");
                    break;
                }
            }
            foreach (float radio in new[] { 3.3f, 3.8f, 2.9f })
            {
                Vector3 donde = CentroPlaza + new Vector3(radio, 0f, 0f);
                if (LibreSobreSuelo(donde, out Vector3 p))
                {
                    Poner(padre, ElencoPlaza[e++ % ElencoPlaza.Length], p, YawHacia(p, CentroPlaza), "Plaza_Parado", ModoGente.Platicar, esperar, Azar("par"), true);
                    n++;
                    Suma("plaza_parado");
                    break;
                }
            }
            for (int k = 0; k < 2; k++)
            {
                float grados = k == 0 ? 270f : 295f;
                Vector3 a = CentroPlaza + new Vector3(Mathf.Sin(grados * Mathf.Deg2Rad), 0f, Mathf.Cos(grados * Mathf.Deg2Rad)) * 3.4f;
                Vector3 b = CentroPlaza + new Vector3(Mathf.Sin((grados + 35f) * Mathf.Deg2Rad), 0f, Mathf.Cos((grados + 35f) * Mathf.Deg2Rad)) * 3.4f;
                if (PonerPasea(padre, ElencoPlaza[e++ % ElencoPlaza.Length], a, b, "Plaza_Pasea_" + k))
                {
                    n++;
                    Suma("plaza_pasea");
                }
            }
            return n;
        }

        private static bool PonerPasea(Transform padre, string visual, Vector3 a, Vector3 b, string nombre)
        {
            Vector3 medio = (a + b) * 0.5f;
            if (!LibreSobreSuelo(a, out Vector3 pa) || !LibreSobreSuelo(b, out Vector3 pb) || !LibreSobreSuelo(medio, out Vector3 _))
            {
                Informe.AppendLine("no pasea (sin lugar) " + nombre);
                return false;
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual));
            if (prefab == null)
            {
                return false;
            }
            var envoltorio = new GameObject(nombre);
            envoltorio.transform.SetParent(padre, false);
            envoltorio.transform.SetPositionAndRotation(pa, Quaternion.Euler(0f, YawHacia(pa, pb), 0f));
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, envoltorio.transform);
            go.name = "Visual";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            ColocarT510.ConfigurarDeFondo(go);
            GenteDeBanqueta g = go.GetComponent<GenteDeBanqueta>();
            if (g == null)
            {
                g = go.AddComponent<GenteDeBanqueta>();
            }
            g.Configurar(ModoGente.Caminar, Azar("pasea" + nombre), 1f, null, pa, pb, 0.8f, 3f, envoltorio.transform);
            Informe.AppendLine("pasea " + visual + " " + pa.ToString("F1") + " -> " + pb.ToString("F1"));
            return true;
        }

        // ------------------------------------------------------------------ 3c. tiendita

        private static int PonerTiendita(Scene escena, Transform raiz)
        {
            Transform tiendita = TodosLosTransforms(escena).FirstOrDefault(t => t.name == "Negocio_tiendita");
            if (tiendita == null)
            {
                Informe.AppendLine("no hay Negocio_tiendita: sin corro de la tiendita");
                return 0;
            }
            Transform padre = new GameObject("Tiendita").transform;
            padre.SetParent(raiz, false);
            AnimationClip plat1 = ClipsNpcNovaT1506.CargarClip("NPC_Platicar_1");
            AnimationClip plat2 = ClipsNpcNovaT1506.CargarClip("NPC_Platicar_2");
            Vector3 ancla = new Vector3(tiendita.position.x, 0f, tiendita.position.z);
            int n = 0;
            for (float radio = 3f; radio <= 6f && n == 0; radio += 1f)
            {
                for (int grados = 0; grados < 360 && n == 0; grados += 30)
                {
                    Vector3 centro = ancla + new Vector3(Mathf.Sin(grados * Mathf.Deg2Rad), 0f, Mathf.Cos(grados * Mathf.Deg2Rad)) * radio;
                    Vector3 haciaAncla = (ancla - centro).normalized;
                    var p = Pareja(centro, Vector3.Cross(Vector3.up, haciaAncla));
                    if (!SobreBanquetaOPlaza(p.a) || !SobreBanquetaOPlaza(p.b) || !LibreSobreSuelo(p.a, out Vector3 a) || !LibreSobreSuelo(p.b, out Vector3 b))
                    {
                        continue;
                    }
                    Poner(padre, ElencoPlaza[0], a, p.yawA, "Tiendita_Platica_a", ModoGente.Platicar, plat1, Azar("tie1"), false);
                    Poner(padre, ElencoPlaza[1], b, p.yawB, "Tiendita_Platica_b", ModoGente.Platicar, plat2, Azar("tie2"), false);
                    n = 2;
                    Suma("tiendita_pareja");
                    Informe.AppendLine("corro de la tiendita en " + centro.ToString("F1"));
                }
            }
            if (n == 0)
            {
                Informe.AppendLine("no hubo lugar libre junto a la tiendita " + ancla.ToString("F1"));
            }
            return n;
        }

        // ------------------------------------------------------------------ 3d. gente nueva de banqueta por barrio

        private sealed class Lugar
        {
            public Vector3 pos;
            public Vector3 tangente;
            public Barrio barrio;
        }

        private static readonly Dictionary<Barrio, Rect> Regiones = new Dictionary<Barrio, Rect>
        {
            { Barrio.Suburbios, Rect.MinMaxRect(-135f, -135f, -48f, 15f) },
            { Barrio.Mercado, Rect.MinMaxRect(-46f, -44f, 46f, 45f) },
            { Barrio.NeonAlto, Rect.MinMaxRect(48f, -24f, 110f, 95f) },
            { Barrio.Nova, Rect.MinMaxRect(48f, -62f, 148f, -26f) },
        };

        private static int PonerEnBanquetas(Dictionary<Barrio, Transform> padres)
        {
            AnimationClip plat1 = ClipsNpcNovaT1506.CargarClip("NPC_Platicar_1");
            AnimationClip plat2 = ClipsNpcNovaT1506.CargarClip("NPC_Platicar_2");
            var existentes = UnityEngine.Object.FindObjectsByType<VisualPersonaje>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(v => v.transform.position).ToList();
            var elegidos = new List<Vector3>();
            int total = 0;
            foreach (Barrio barrio in new[] { Barrio.Suburbios, Barrio.Mercado, Barrio.NeonAlto, Barrio.Nova })
            {
                var (parejas, trios, solos) = Cantidad[barrio];
                List<Lugar> lugares = LugaresDeBanqueta(barrio).OrderBy(l => Azar("l" + l.pos.x + "," + l.pos.z)).ToList();
                Informe.AppendLine("banqueta " + barrio + ": " + lugares.Count + " lugares libres");
                int p = 0, t = 0, s = 0, indice = 0;
                foreach (Lugar l in lugares)
                {
                    bool quedaAlgo = p < parejas || t < trios || s < solos;
                    if (!quedaAlgo)
                    {
                        break;
                    }
                    if (elegidos.Any(e => Vector3.Distance(e, l.pos) < SeparacionEntreCorros) || existentes.Any(e => Vector3.Distance(e, l.pos) < DistanciaAOtraGente))
                    {
                        continue;
                    }
                    string visualA = ElegirVisual(barrio, Azar("va" + indice + barrio), Existe);
                    string visualB = ElegirVisual(barrio, Azar("vb" + indice + barrio), Existe);
                    string visualC = ElegirVisual(barrio, Azar("vc" + indice + barrio), Existe);
                    indice++;
                    if (visualA == null)
                    {
                        break;
                    }
                    Transform padre = padres[barrio];
                    if (p < parejas)
                    {
                        var par = Pareja(l.pos, l.tangente);
                        if (LibreSobreBanqueta(par.a, out Vector3 a) && LibreSobreBanqueta(par.b, out Vector3 b))
                        {
                            Poner(padre, visualA, a, par.yawA, "Platica_" + barrio + "_" + p + "a", ModoGente.Platicar, plat1, Azar("pf" + p + barrio), false);
                            Poner(padre, visualB, b, par.yawB, "Platica_" + barrio + "_" + p + "b", ModoGente.Platicar, plat2, Azar("pg" + p + barrio), false);
                            elegidos.Add(l.pos);
                            p++;
                            total += 2;
                            Suma("banqueta_pareja_" + barrio);
                        }
                    }
                    else if (t < trios)
                    {
                        var corro = Corro(l.pos, 3, RadioTrio, 30f);
                        var puestos = new List<Vector3>();
                        foreach ((Vector3 pos, float _) in corro)
                        {
                            if (LibreSobreBanqueta(pos, out Vector3 donde)) { puestos.Add(donde); }
                        }
                        if (puestos.Count == 3)
                        {
                            string[] visuales = { visualA, visualB, visualC };
                            for (int i = 0; i < 3; i++)
                            {
                                Poner(padre, visuales[i] ?? visualA, puestos[i], corro[i].yaw, "Trio_" + barrio + "_" + t + "_" + i, ModoGente.Platicar, i % 2 == 0 ? plat1 : plat2, Azar("tf" + t + i + barrio), false);
                            }
                            elegidos.Add(l.pos);
                            t++;
                            total += 3;
                            Suma("banqueta_trio_" + barrio);
                        }
                    }
                    else if (s < solos && LibreSobreBanqueta(l.pos, out Vector3 solo))
                    {
                        Poner(padre, visualA, solo, YawHaciaLaCalle(solo, l.tangente), "Solo_" + barrio + "_" + s, ModoGente.Quieto, null, Azar("sf" + s + barrio), false);
                        elegidos.Add(l.pos);
                        s++;
                        total++;
                        Suma("banqueta_solo_" + barrio);
                    }
                }
                Informe.AppendLine("  " + barrio + ": parejas " + p + "/" + parejas + ", trios " + t + "/" + trios + ", solos " + s + "/" + solos);
            }
            return total;
        }

        /// <summary>Puntos de banqueta libres del barrio (malla de 2 m dentro de su región, sobre banqueta, sin techo, libres de edificios y de la otra gente).</summary>
        private static List<Lugar> LugaresDeBanqueta(Barrio barrio)
        {
            var lista = new List<Lugar>();
            Rect r = Regiones[barrio];
            for (float x = r.xMin; x <= r.xMax; x += PasoMalla)
            {
                for (float z = r.yMin; z <= r.yMax; z += PasoMalla)
                {
                    Vector3 p = new Vector3(x, 0f, z);
                    if (BarrioDe(p) != barrio || !SueloBanqueta(p, out float y, out Vector3 tangente))
                    {
                        continue;
                    }
                    p.y = y;
                    if (!Libre(p))
                    {
                        continue;
                    }
                    lista.Add(new Lugar { pos = p, tangente = tangente, barrio = barrio });
                }
            }
            return lista;
        }

        /// <summary>Se para mirando hacia el lado de la banqueta donde el piso baja (la calle), o a un lado cualquiera si no baja.</summary>
        private static float YawHaciaLaCalle(Vector3 p, Vector3 tangente)
        {
            Vector3 normal = Vector3.Cross(Vector3.up, tangente).normalized;
            foreach (Vector3 lado in new[] { normal, -normal })
            {
                float suelo = SueloCualquiera(p + lado * 2.5f);
                if (!float.IsNaN(suelo) && suelo < p.y - 0.1f)
                {
                    return YawHacia(p, p + lado);
                }
            }
            return YawHacia(p, p + normal);
        }

        // ------------------------------------------------------------------ 4. sentados

        private static int SentarEnBancas(Scene escena)
        {
            AnimationClip clipSentado = ClipsNpcNovaT1506.CargarClip("NPC_Sentado_Banca");
            var config = ConfigClipsNovaNpc.Actual;
            if (clipSentado == null || config == null || config.SentadoBanca == null)
            {
                Informe.AppendLine("falta NPC_Sentado_Banca o ConfigClipsNovaNpc (corre ClipsNpcPlaticarT1516.AplicarConfig): no se sienta a nadie");
                return 0;
            }
            int sentados = 0;
            List<Transform> bancas = ClipsNpcPlaticarT1516.BuscarBancas(escena);
            for (int n = 0; n < bancas.Count; n++)
            {
                Transform banca = bancas[n];
                foreach (Transform hijo in banca.Cast<Transform>().Where(h => h.name.StartsWith(ClipsNpcPlaticarT1516.PrefijoSentado) || h.name.StartsWith(PrefijoSentado)).ToList())
                {
                    UnityEngine.Object.DestroyImmediate(hijo.gameObject);
                }
                if (n % 4 == 3)
                {
                    continue; // una de cada cuatro bancas queda vacía
                }
                Barrio barrio = BarrioDe(banca.position);
                float[] lugares = n % 3 == 0 ? new[] { -ClipsNpcPlaticarT1516.SeparacionEnBanca, ClipsNpcPlaticarT1516.SeparacionEnBanca } : new[] { 0f };
                foreach (float lateral in lugares)
                {
                    string visual = ElegirVisual(barrio == Barrio.Fuera ? Barrio.Mercado : barrio, Azar("sent" + n + lateral), Existe);
                    GameObject prefab = visual != null ? AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual)) : null;
                    if (prefab == null)
                    {
                        continue;
                    }
                    var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, banca);
                    npc.name = PrefijoSentado + (sentados + 1);
                    npc.AddComponent<NpcSentadoBanca>().Sentar(banca, lateral);
                    sentados++;
                    Suma("sentado_" + barrio + "_" + visual);
                }
            }
            return sentados;
        }

        // ------------------------------------------------------------------ poner y validar

        private static GameObject Poner(Transform padre, string visual, Vector3 pos, float yaw, string nombre, ModoGente modo, AnimationClip clip, float fase, bool sinPareja)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual));
            if (prefab == null)
            {
                Informe.AppendLine("FALTA el visual " + visual + " (" + nombre + ")");
                return null;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = nombre;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            ColocarT510.ConfigurarDeFondo(go);
            GenteDeBanqueta g = go.GetComponent<GenteDeBanqueta>();
            if (g == null)
            {
                g = go.AddComponent<GenteDeBanqueta>();
            }
            g.Configurar(modo, fase, Mathf.Lerp(0.85f, 1.15f, Azar("ritmo" + nombre)), clip, Vector3.zero, Vector3.zero, 1f, 0f, null);
            g.FijarSinPareja(sinPareja);
            Ocupado.Add(Caja(pos));
            Suma("total_" + BarrioDe(pos) + "_" + visual);
            return go;
        }

        private static Bounds Caja(Vector3 pos) => new Bounds(pos + Vector3.up * AltoPersona * 0.5f, new Vector3(RadioPersona * 2f, AltoPersona, RadioPersona * 2f));

        private static bool Libre(Vector3 pos)
        {
            Bounds caja = Caja(pos);
            return ColocarNoche.Libre(caja) && !Ocupado.Any(b => b.Intersects(caja));
        }

        /// <summary>El piso (altura del golpe más alto con superficie casi horizontal y a menos de 0.5 m), sin importar qué sea; NaN si no hay.</summary>
        private static float SueloCualquiera(Vector3 p)
        {
            float mejor = float.NaN;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(p.x, 3f, p.z), Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.normal.y < 0.7f || h.point.y > 0.5f)
                {
                    continue;
                }
                if (float.IsNaN(mejor) || h.point.y > mejor)
                {
                    mejor = h.point.y;
                }
            }
            return mejor;
        }

        private static bool EsBanqueta(Collider c)
        {
            Transform t = c.transform;
            for (int i = 0; i < 2 && t != null; i++, t = t.parent)
            {
                if (t.name.StartsWith("Banqueta"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Piso que sea banqueta (el collider o su padre se llama Banqueta*), a la altura de banqueta, sin techo encima. La tangente sale del lado largo de la banqueta.</summary>
        private static bool SueloBanqueta(Vector3 p, out float y, out Vector3 tangente)
        {
            y = 0f;
            tangente = Vector3.right;
            RaycastHit? mejor = null;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(p.x, 3f, p.z), Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.normal.y < 0.7f || h.point.y > 0.5f)
                {
                    continue;
                }
                if (!mejor.HasValue || h.point.y > mejor.Value.point.y)
                {
                    mejor = h;
                }
            }
            if (!mejor.HasValue)
            {
                return false;
            }
            RaycastHit golpe = mejor.Value;
            if (!EsBanqueta(golpe.collider) || golpe.point.y < AlturaBanquetaMin || golpe.point.y > AlturaBanquetaMax)
            {
                return false;
            }
            if (Physics.Raycast(new Vector3(p.x, golpe.point.y + 1.8f, p.z), Vector3.up, AlturaTecho, ~0, QueryTriggerInteraction.Ignore))
            {
                return false; // techo, toldo o balcón encima: eso es un interior o un portal
            }
            y = golpe.point.y;
            Vector3 tam = golpe.collider.bounds.size;
            tangente = tam.x >= tam.z ? Vector3.right : Vector3.forward;
            return true;
        }

        /// <summary>True si el piso es banqueta o plaza: cualquier piso a la altura de la calle o menos que no se llame Calle*.</summary>
        private static bool SobreBanquetaOPlaza(Vector3 p)
        {
            RaycastHit? mejor = null;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(p.x, 3f, p.z), Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.normal.y >= 0.7f && h.point.y <= 0.5f && (!mejor.HasValue || h.point.y > mejor.Value.point.y))
                {
                    mejor = h;
                }
            }
            return mejor.HasValue && !mejor.Value.collider.name.StartsWith("Calle");
        }

        private static bool LibreSobreSuelo(Vector3 p, out Vector3 conAltura)
        {
            conAltura = p;
            float y = SueloCualquiera(p);
            if (float.IsNaN(y))
            {
                return false;
            }
            conAltura = new Vector3(p.x, y, p.z);
            return Libre(conAltura);
        }

        private static bool LibreSobreBanqueta(Vector3 p, out Vector3 conAltura)
        {
            conAltura = p;
            if (!SueloBanqueta(p, out float y, out Vector3 _))
            {
                return false;
            }
            conAltura = new Vector3(p.x, y, p.z);
            return Libre(conAltura);
        }

        // ------------------------------------------------------------------ inventario

        private static IEnumerable<Transform> TodosLosTransforms(Scene escena)
        {
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    yield return t;
                }
            }
        }

        /// <summary>Las raíces de instancia de prefab Visual_* de civiles (hombre, mujer, robot de peatón y los NPxx) activos o no.</summary>
        private static List<Transform> TodasLasRaicesDeVisual(Scene escena)
        {
            var lista = new List<Transform>();
            foreach (Transform t in TodosLosTransforms(escena))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                {
                    continue;
                }
                string tipo = TipoDe(t);
                if (tipo.StartsWith("NP") || tipo == "man" || tipo == "woman" || tipo == "woman2" || tipo == "RobotHombre")
                {
                    lista.Add(t);
                }
            }
            return lista;
        }

        private static string TipoDe(Transform t)
        {
            GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            string nombre = fuente != null ? fuente.name : t.name;
            return nombre.StartsWith("Visual_") ? nombre.Substring("Visual_".Length) : nombre;
        }

        private static string RolDe(Transform t)
        {
            if (t.name.StartsWith("Vendedor_")) { return "vendedor"; }
            if (t.name.StartsWith(PrefijoSentado) || t.name.StartsWith(ClipsNpcPlaticarT1516.PrefijoSentado)) { return "sentado"; }
            GenteDeBanqueta g = t.GetComponentInChildren<GenteDeBanqueta>(true);
            if (g == null) { return "sin comportamiento"; }
            switch (g.Modo)
            {
                case ModoGente.Platicar: return g.SinPareja ? "parado con clip" : "platica";
                case ModoGente.Caminar: return "pasea";
                case ModoGente.Celular: return "celular";
                default: return "parado";
            }
        }

        private static void Escribir(string texto)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, texto);
        }
    }
}

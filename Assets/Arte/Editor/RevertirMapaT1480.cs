using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1480: quita de Ciudad_Prologo la expansión del 18-19 nov (Malecón, Cerro, Autopista, Puerto y sus uniones, orillas, volúmenes, bruma, aéreas)
    /// y deja los 4 barrios con sus bordes como estaban. Los prefabs y modelos de las zonas se quedan en el proyecto.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.RevertirMapaT1480.Aplicar  (escribe Logs/t1480-revertir.txt)
    ///            -executeMethod MechaCyberpunk.Arte.Editor.RevertirMapaT1480.Validar  (escribe Logs/t1480-validar.txt)
    /// </summary>
    public static class RevertirMapaT1480
    {
        public const string RutaInforme = "Logs/t1480-revertir.txt";
        public const string RutaValidar = "Logs/t1480-validar.txt";
        private const float MargenAfueras = 5f;

        public static readonly string[] RaicesQuitadas =
        {
            "T1354_Malecon", "T1355_Cerro", "T1356_Autopista", "T1357_Puerto", "T1388_Uniones", "T1461_Look",
            "T1358_Orillas", "T1365_Uniones", "T1392_Volumenes", "T1399_Bruma", "T1462_Aereas",
        };

        public static readonly string[] ZonasTraficoQuitadas =
        {
            "ZonaTrafico_Malecon", "ZonaTrafico_Cerro", "ZonaTrafico_Puerto", "ZonaTrafico_Autopista", "ZonaTrafico_Autopista_RampaEste",
        };

        // (tipo, fileID local en la escena, ruta). TRANSFORM: vuelve a posición 0, rotación identidad y escala 1 (los 4 Tramo de T513_Fondo).
        private static readonly (string tipo, ulong id, string ruta)[] Lista =
        {
            ("GO", 60887865UL, "T530_Afueras/Salida_5/Edificio"),
            ("GO", 75088029UL, "T530_Afueras/Salida_4/Ventanas"),
            ("GO", 130994283UL, "T530_Afueras/Salida_0/LimiteInvisible"),
            ("GO", 185352145UL, "S2_Calles/Salida_4/Kit_79"),
            ("GO", 295912078UL, "T530_Afueras/Salida_2/LimiteInvisible"),
            ("GO", 333990399UL, "T530_Afueras/Salida_1/LimiteInvisible"),
            ("GO", 377196325UL, "T513_Fondo/Tramo_2_fondo-suburbios"),
            ("TRANSFORM", 377196326UL, "T513_Fondo/Tramo_2_fondo-suburbios"),
            ("TRANSFORM", 383978027UL, "T513_Fondo/Tramo_0_fondo-nova"),
            ("GO", 400408184UL, "T530_Afueras/Salida_3/LimiteInvisible"),
            ("GO", 487381281UL, "T530_Afueras/Salida_4/Edificio"),
            ("GO", 514826509UL, "T530_Afueras/Salida_0/Ventanas"),
            ("GO", 542852728UL, "T530_Afueras/Salida_1/LimiteInvisible"),
            ("GO", 568571213UL, "T530_Afueras/Salida_1/Edificio"),
            ("GO", 581471402UL, "T530_Afueras/Salida_4/Edificio"),
            ("GO", 582059295UL, "T530_Afueras/Salida_5/Ventanas"),
            ("GO", 630327832UL, "S2_Calles/Salida_1/Kit_29"),
            ("GO", 668281349UL, "T530_Afueras/Salida_4/Ventanas"),
            ("GO", 680488654UL, "T530_Afueras/Salida_5/LimiteInvisible"),
            ("GO", 720800362UL, "T530_Afueras/Salida_4/Ventanas"),
            ("GO", 732915998UL, "T530_Afueras/Salida_1/LimiteInvisible"),
            ("GO", 740495352UL, "T530_Afueras/Salida_4/Ventanas"),
            ("GO", 752901364UL, "T530_Afueras/Salida_5/LimiteInvisible"),
            ("GO", 755563920UL, "T530_Afueras/Salida_5/Ventanas"),
            ("GO", 764868399UL, "T530_Afueras/Salida_5/Ventanas"),
            ("GO", 785132921UL, "T530_Afueras/Salida_3/LimiteInvisible"),
            ("GO", 836375170UL, "T530_Afueras/Salida_3/LimiteInvisible"),
            ("GO", 837456334UL, "T530_Afueras/Salida_3/Ventanas"),
            ("GO", 898847052UL, "T530_Afueras/Salida_4/Edificio"),
            ("GO", 912493879UL, "T530_Afueras/Salida_0/Edificio"),
            ("GO", 916014740UL, "T530_Afueras/Salida_1/Edificio"),
            ("GO", 958980172UL, "T530_Afueras/Salida_2/Edificio"),
            ("GO", 973754173UL, "T530_Afueras/Salida_0/LimiteInvisible"),
            ("GO", 983353968UL, "T530_Afueras/Salida_5/LimiteInvisible"),
            ("RENDERER", 1028779549UL, "T530_Afueras/Salida_4/Ventanas"),
            ("GO", 1038017363UL, "S2_Calles/Salida_3/Kit_62"),
            ("GO", 1117243794UL, "T530_Afueras/Salida_2/LimiteInvisible"),
            ("GO", 1124338479UL, "T513_Fondo/Tramo_1_fondo-neon-alto"),
            ("TRANSFORM", 1124338480UL, "T513_Fondo/Tramo_1_fondo-neon-alto"),
            ("GO", 1192454172UL, "T530_Afueras/Salida_4/LimiteInvisible"),
            ("GO", 1197728744UL, "S2_Calles/Salida_5/Kit_97"),
            ("GO", 1249598257UL, "T530_Afueras/Salida_4/Edificio"),
            ("GO", 1285651491UL, "T530_Afueras/Salida_2/Ventanas"),
            ("GO", 1316251368UL, "T530_Afueras/Salida_4/LimiteInvisible"),
            ("GO", 1453143823UL, "S2_Calles/Salida_2/Kit_47"),
            ("GO", 1599576274UL, "T530_Afueras/Salida_2/Edificio"),
            ("GO", 1656986749UL, "T530_Afueras/Salida_5/Ventanas"),
            ("GO", 1678613019UL, "T530_Afueras/Salida_5/Edificio"),
            ("GO", 1690886057UL, "T513_Fondo/Tramo_3_fondo-suburbios"),
            ("TRANSFORM", 1690886058UL, "T513_Fondo/Tramo_3_fondo-suburbios"),
            ("GO", 1701765719UL, "T530_Afueras/Salida_2/Ventanas"),
            ("GO", 1736786977UL, "S2_Calles/Salida_2/Kit_50"),
            ("GO", 1775957073UL, "T530_Afueras/Salida_3/Edificio"),
            ("GO", 1830644389UL, "T530_Afueras/Salida_5/Ventanas"),
            ("GO", 1853575798UL, "T530_Afueras/Salida_4/LimiteInvisible"),
            ("GO", 1893215589UL, "T530_Afueras/Salida_5/Edificio"),
            ("GO", 1906009753UL, "T530_Afueras/Salida_5/Edificio"),
            ("GO", 1915812242UL, "T530_Afueras/Salida_2/LimiteInvisible"),
            ("GO", 1951817349UL, "T530_Afueras/Salida_5/Edificio"),
            ("GO", 2003617353UL, "S2_Calles/Salida_0/Kit_12"),
            ("GO", 2006602962UL, "T530_Afueras/Salida_1/Ventanas"),
            ("GO", 2016740606UL, "T530_Afueras/Salida_4/Ventanas"),
            ("GO", 2082154625UL, "T530_Afueras/Salida_5/Ventanas"),
            ("GO", 2102338779UL, "T530_Afueras/Salida_0/LimiteInvisible"),
            ("GO", 2124896490UL, "T530_Afueras/Salida_4/Ventanas"),
            ("GO", 2146095327UL, "T530_Afueras/Salida_5/Ventanas"),
        };

        public static void Aplicar()
        {
            ColocarS2.Abrir();
            Scene escena = EditorSceneManager.GetActiveScene();
            var log = new StringBuilder("T-1480 revertir mapa " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n");

            log.AppendLine("== Raíces destruidas");
            foreach (string nombre in RaicesQuitadas)
            {
                GameObject[] hallados = escena.GetRootGameObjects().Where(g => g.name == nombre).ToArray();
                if (hallados.Length == 0)
                {
                    log.AppendLine("  (no estaba) " + nombre);
                }
                foreach (GameObject g in hallados)
                {
                    Object.DestroyImmediate(g);
                    log.AppendLine("  destruida " + nombre);
                }
            }

            log.AppendLine("== ZonaTrafico destruidas");
            GameObject zonas = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "Zonas");
            if (zonas == null)
            {
                log.AppendLine("  NO ENCONTRADA la raíz Zonas");
            }
            else
            {
                foreach (string nombre in ZonasTraficoQuitadas)
                {
                    Transform[] hijos = zonas.transform.Cast<Transform>().Where(t => t.name == nombre).ToArray();
                    if (hijos.Length == 0)
                    {
                        log.AppendLine("  (no estaba) " + nombre);
                    }
                    foreach (Transform t in hijos)
                    {
                        Object.DestroyImmediate(t.gameObject);
                        log.AppendLine("  destruida " + nombre);
                    }
                }
            }

            log.AppendLine("== Objetos que se vuelven a encender / colocar");
            Dictionary<ulong, Object> porId = IndexarPorId(escena);
            int aplicados = 0, noEncontrados = 0, porRuta = 0;
            foreach ((string tipo, ulong id, string ruta) in Lista)
            {
                Object o = null;
                if (porId.TryGetValue(id, out Object porIdObj) && TipoCoincide(tipo, porIdObj))
                {
                    o = porIdObj;
                }
                else
                {
                    o = BuscarPorRuta(escena, tipo, ruta);
                    if (o != null)
                    {
                        porRuta++;
                        log.AppendLine("  (por ruta, no por id) " + tipo + " " + id + " " + ruta);
                    }
                }
                if (o == null)
                {
                    noEncontrados++;
                    log.AppendLine("  NO ENCONTRADO " + tipo + " " + id + " " + ruta);
                    continue;
                }
                Encender(tipo, o);
                aplicados++;
            }
            log.AppendLine("  aplicados " + aplicados + " de " + Lista.Length + " (por ruta " + porRuta + ", no encontrados " + noEncontrados + ")");

            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, log.ToString());
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] " + log);
        }

        public static void Validar()
        {
            ColocarS2.Abrir();
            Scene escena = EditorSceneManager.GetActiveScene();
            var log = new StringBuilder("T-1480 validar " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n");
            int duras = 0;

            log.AppendLine("== Raíces quitadas que siguen en la escena (FALLA)");
            foreach (GameObject g in escena.GetRootGameObjects().Where(g => RaicesQuitadas.Contains(g.name)))
            {
                duras++;
                log.AppendLine("  " + g.name);
            }

            log.AppendLine("== ZonaTrafico que quedan");
            GameObject zonas = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "Zonas");
            if (zonas != null)
            {
                foreach (Transform t in zonas.transform)
                {
                    bool quitada = ZonasTraficoQuitadas.Contains(t.name);
                    if (quitada)
                    {
                        duras++;
                    }
                    log.AppendLine("  " + t.name + (quitada ? "  <- FALLA, debía irse" : ""));
                }
            }
            else
            {
                log.AppendLine("  no hay raíz Zonas");
            }

            log.AppendLine("== Objetos de la lista que siguen apagados o sin encontrar (FALLA)");
            Dictionary<ulong, Object> porId = IndexarPorId(escena);
            foreach ((string tipo, ulong id, string ruta) in Lista)
            {
                if (!porId.TryGetValue(id, out Object o) || !TipoCoincide(tipo, o))
                {
                    o = BuscarPorRuta(escena, tipo, ruta);
                }
                if (o == null)
                {
                    duras++;
                    log.AppendLine("  NO ENCONTRADO " + tipo + " " + id + " " + ruta);
                }
                else if (!EstaEncendido(tipo, o))
                {
                    duras++;
                    log.AppendLine("  apagado " + tipo + " " + id + " " + ruta);
                }
            }

            Huerfanos(escena, log);
            Referencias(escena, log);

            log.AppendLine("== Resultado: " + (duras == 0 ? "OK" : "FALLAS DURAS " + duras));
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaValidar, log.ToString());
            Debug.Log("[Arte] " + log);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(duras == 0 ? 0 : 1);
            }
        }

        // ---------------------------------------------------------------- Huérfanos y referencias

        private static void Huerfanos(Scene escena, StringBuilder log)
        {
            log.AppendLine("== Huérfanos (fuera del rectángulo de los 4 barrios + afueras, +" + MargenAfueras + " m), solo para revisión");
            GameObject afueras = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "T530_Afueras");
            if (afueras == null)
            {
                log.AppendLine("  no hay T530_Afueras: no se puede calcular el rectángulo");
                return;
            }
            Bounds area = default;
            bool hay = false;
            foreach (Transform t in afueras.GetComponentsInChildren<Transform>(true).Where(x => x.name == "LimiteInvisible"))
            {
                foreach (Collider c in t.GetComponents<Collider>())
                {
                    if (!hay)
                    {
                        area = c.bounds;
                        hay = true;
                    }
                    else
                    {
                        area.Encapsulate(c.bounds);
                    }
                }
            }
            if (!hay)
            {
                log.AppendLine("  los LimiteInvisible no tienen collider: no se puede calcular el rectángulo");
                return;
            }
            area.Expand(new Vector3(MargenAfueras * 2f, 0f, MargenAfueras * 2f));
            log.AppendLine("  rectángulo X " + area.min.x.ToString("F1") + ".." + area.max.x.ToString("F1") + "  Z " + area.min.z.ToString("F1") + ".." + area.max.z.ToString("F1"));

            int n = 0;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Light l in raiz.GetComponentsInChildren<Light>(false))
                {
                    n += Anotar(log, "Light", l.gameObject, l.transform.position, area);
                }
                foreach (Collider c in raiz.GetComponentsInChildren<Collider>(false))
                {
                    if (!c.isTrigger)
                    {
                        n += Anotar(log, "Collider", c.gameObject, c.bounds.center, area);
                    }
                }
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (r.enabled)
                    {
                        n += Anotar(log, "Renderer", r.gameObject, r.bounds.center, area);
                    }
                }
            }
            log.AppendLine("  total " + n);
        }

        private static int Anotar(StringBuilder log, string tipo, GameObject go, Vector3 centro, Bounds area)
        {
            if (!go.activeInHierarchy || (centro.x >= area.min.x && centro.x <= area.max.x && centro.z >= area.min.z && centro.z <= area.max.z))
            {
                return 0;
            }
            log.AppendLine("  " + tipo + " " + Ruta(go.transform) + " (" + centro.x.ToString("F0") + ", " + centro.y.ToString("F0") + ", " + centro.z.ToString("F0") + ")");
            return 1;
        }

        private static void Referencias(Scene escena, StringBuilder log)
        {
            log.AppendLine("== Componentes con script faltante y referencias a objetos destruidos");
            int faltantes = 0, rotas = 0;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    int f = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (f > 0)
                    {
                        faltantes += f;
                        log.AppendLine("  script faltante x" + f + " en " + Ruta(t));
                    }
                }
                foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null)
                    {
                        continue;
                    }
                    var so = new SerializedObject(mb);
                    SerializedProperty p = so.GetIterator();
                    while (p.NextVisible(true))
                    {
                        if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0)
                        {
                            rotas++;
                            log.AppendLine("  referencia rota " + Ruta(mb.transform) + " :: " + mb.GetType().Name + "." + p.propertyPath);
                        }
                    }
                }
            }
            log.AppendLine("  scripts faltantes " + faltantes + ", referencias rotas " + rotas);
        }

        // ---------------------------------------------------------------- Ayudas

        private static Dictionary<ulong, Object> IndexarPorId(Scene escena)
        {
            var d = new Dictionary<ulong, Object>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    Agregar(d, t.gameObject);
                    Agregar(d, t);
                    Renderer r = t.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Agregar(d, r);
                    }
                }
            }
            return d;
        }

        private static void Agregar(Dictionary<ulong, Object> d, Object o)
        {
            ulong id = GlobalObjectId.GetGlobalObjectIdSlow(o).targetObjectId;
            if (id != 0 && !d.ContainsKey(id))
            {
                d[id] = o;
            }
        }

        private static bool TipoCoincide(string tipo, Object o)
        {
            switch (tipo)
            {
                case "GO": return o is GameObject;
                case "RENDERER": return o is Renderer;
                default: return o is Transform;
            }
        }

        private static Object BuscarPorRuta(Scene escena, string tipo, string ruta)
        {
            // Entre varios con la misma ruta, el primero que todavía no esté como se quiere.
            Object primero = null;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (Ruta(t) != ruta)
                    {
                        continue;
                    }
                    Object o = tipo == "GO" ? t.gameObject : tipo == "RENDERER" ? (Object)t.GetComponent<Renderer>() : t;
                    if (o == null)
                    {
                        continue;
                    }
                    if (primero == null)
                    {
                        primero = o;
                    }
                    if (!EstaEncendido(tipo, o))
                    {
                        return o;
                    }
                }
            }
            return primero;
        }

        private static void Encender(string tipo, Object o)
        {
            switch (tipo)
            {
                case "GO":
                    ((GameObject)o).SetActive(true);
                    break;
                case "RENDERER":
                    ((Renderer)o).enabled = true;
                    break;
                default:
                    var t = (Transform)o;
                    t.localPosition = Vector3.zero;
                    t.localRotation = Quaternion.identity;
                    t.localScale = Vector3.one;
                    break;
            }
        }

        private static bool EstaEncendido(string tipo, Object o)
        {
            switch (tipo)
            {
                case "GO": return ((GameObject)o).activeSelf;
                case "RENDERER": return ((Renderer)o).enabled;
                default:
                    var t = (Transform)o;
                    return t.localPosition == Vector3.zero && t.localRotation == Quaternion.identity && t.localScale == Vector3.one;
            }
        }

        private static string Ruta(Transform t)
        {
            string r = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                r = t.name + "/" + r;
            }
            return r;
        }
    }
}

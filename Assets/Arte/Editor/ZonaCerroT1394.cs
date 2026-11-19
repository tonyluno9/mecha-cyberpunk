using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1394: Colonia del Cerro, ronda 2. Corre DESPUÉS de <see cref="ZonaCerroT1385"/> y trabaja sobre el mismo prefab (T1355_Cerro).
    /// Todo lo nuevo cuelga de la raíz "T1394_Cerro", que Aplicar borra y rehace. Lo viejo que se quita no se destruye: se desactiva y se guarda en
    /// "T1394_Cerro/Reemplazados" con su lugar original en "T1394_Cerro/Registro", y Aplicar lo devuelve a su sitio al empezar (se puede correr dos veces).
    /// Lo que viene de T1385 (casas que invaden la calle, escaleras de servicio con la salida al revés, carros pegados a la cámara) sí se destruye:
    /// si se vuelve a correr T1385 hay que volver a correr este script.
    ///  1. Cambia CASAS EXISTENTES del kit por CC07_a, CC06_b, CC01 y CC02 mezcladas (mismo lugar, escala a la huella, sin encimarse), con tinaco, antena y ropa tendida.
    ///  2. Azoteas para parkour: escalones de concreto y tablones entre azoteas vecinas a 0-3 m de desnivel, con collider; todo grupo de azoteas unidas tiene salida a la calle.
    ///  3. Neones chicos (magenta y turquesa) en tiendas y junto a las escaleras, con su Point Light chica (LuzNocturna, sin sombras); tope 30 luces.
    ///  4. Cámara K13 en la calle base mirando hacia las escaleras que suben, sin carros pegados; se abre la vista al este (casas que tapan la ciudad se quitan, con tope).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ZonaCerroT1394.Aplicar, luego .Validar, luego .Fotos (corrida aparte).
    /// </summary>
    public static class ZonaCerroT1394
    {
        public const string RutaPrefab = ZonaCerroT1385.RutaPrefab;
        public const string Raiz = "T1394_Cerro";
        public const string RutaInforme = "Logs/t1394-aplicar.txt";
        public const string RutaValidar = "Logs/t1394-validar.txt";
        public const int Semilla = 13940;
        public const long TopeBytesPrefab = 60L * 1024 * 1024;
        public const int TopeLuces = 30;

        // Variantes: CC06_b y CC07_a como en T1385 (ver su comentario); CC01 y CC02 son únicos. CC03 no existe; CC04 (tendedero) y CC05 (lavadero) van en las azoteas.
        public static readonly string[] CodigosCasa = { "CC07_CasaCerro_a", "CC06_CasaCerro_b", "CC01_CasaCerro", "CC02_CasaCerro" };
        public const string RutaMadera = "Assets/Arte/Exteriores/Materiales/T506_Madera_Vieja.mat";

        // K13: en la calle base (x = -305, plana), mirando hacia el oeste-noroeste: a la izquierda la boca de la escalera del z = 0 (CE01, sube 19 m al oeste),
        // al frente la calle que sigue y, a la derecha, el este abierto hacia la ciudad. A13: desde la ladera oeste, alto, mirando al este sobre las azoteas que bajan hacia la ciudad.
        public static readonly Vector3 K13Desde = new Vector3(-303.5f, 1.85f, -12f);
        public static readonly Vector3 K13Hacia = new Vector3(-336f, 8.5f, 16f);
        public static readonly Vector3 A13Desde = new Vector3(-470f, 52f, -70f);
        public static readonly Vector3 A13Hacia = new Vector3(-310f, 6f, 0f);
        public const float CampoFoto = 66f;

        private const int TopeCambios = 90;
        private const int TopeEnlaces = 70;
        private const int TopeCasasVista = 14;
        private const int TopeNeones = 26;
        private const float AltoBanqueta = 0.2f;
        private const float AnchoEscalon = 1.1f;
        private const float PasoMaximoEscalon = 0.32f;
        private const float DesnivelMaximo = 3f;
        private const float SeparacionMaxima = 3f;

        private static StringBuilder informe;
        private static readonly RaycastHit[] bufRayos = new RaycastHit[64];
        private static readonly Collider[] bufCol = new Collider[256];
        private static readonly Dictionary<string, Vector3> medidas = new Dictionary<string, Vector3>();

        private sealed class Ctx
        {
            public GameObject Zona;
            public PhysicsScene Fisica;
            public Transform Terreno, Calles, Casas, Detalles, Carros, Nuevo, Reem, Reg, Det;
            public readonly List<List<Vector3>> Ejes = new List<List<Vector3>>();
            public KitS2c.Obra Obra;
            public Mesh MallaCaja;
            public Material Concreto, Madera;
            public int LucesAntes;
        }

        private sealed class Techo
        {
            public Transform Raiz;
            public string Nombre;
            public float X0, X1, Z0, Z1, Y, Normal;
            public bool Forzar;
            public int Enlaces;
        }

        // ================================================================ Aplicar

        [MenuItem("Mecha Cyberpunk/T1394/Cerro ronda 2: aplicar")]
        public static void Aplicar()
        {
            informe = new StringBuilder("T-1394: Colonia del Cerro, ronda 2 (" + RutaPrefab + ")\n");
            medidas.Clear();
            GameObject zona = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                int devueltos = Restaurar(zona);
                Ctx c = Abrir(zona);
                Log("Devueltos a su lugar (de una corrida anterior): " + devueltos + ".");
                if (zona.transform.Find(Raiz) != null)
                {
                    Log("ERROR: quedó una raíz " + Raiz + " con objetos sin devolver; no se guarda nada para no perder casas.");
                    Escribir(RutaInforme, informe);
                    return;
                }
                c.LucesAntes = zona.GetComponentsInChildren<Light>(true).Length;
                Log("Luces antes: " + c.LucesAntes + ". Tamaño del prefab antes: " + Megas(RutaPrefab) + " MB.");
                LimpiarT1385(c);
                c.Obra = KitS2c.Empezar(Raiz, Semilla, zona.transform);
                c.Nuevo = c.Obra.Raiz;
                c.Reem = Hijo(c.Nuevo, "Reemplazados");
                c.Reg = Hijo(c.Nuevo, "Registro");
                c.Det = Hijo(c.Nuevo, "Detalles");
                c.Concreto = KitS2c.MatBanqueta();
                c.Madera = AssetDatabase.LoadAssetAtPath<Material>(RutaMadera) ?? c.Concreto;
                Physics.SyncTransforms();
                Paso("carros cerca de K13", () => QuitarCarros(c));
                Paso("vista abierta", () => AbrirVista(c));
                List<Techo> nuevas = new List<Techo>();
                Paso("casas nuevas", () => CambiarCasas(c, nuevas));
                Paso("azoteas", () => Azoteas(c, nuevas));
                Paso("neones", () => Neones(c));
                Estatico(c);
                KitS2c.Terminar(c.Obra);
                int luces = zona.GetComponentsInChildren<Light>(true).Length;
                int nuevasLuces = c.Nuevo.GetComponentsInChildren<Light>(true).Length;
                Log("Luces después: " + luces + " (nuevas " + nuevasLuces + ", tope " + TopeLuces + ").");
                if (nuevasLuces > TopeLuces)
                {
                    Log("AVISO: se pasó del tope de luces nuevas.");
                }
                Physics.SyncTransforms();
                bool ok;
                GameObject guardado = PrefabUtility.SaveAsPrefabAsset(zona, RutaPrefab, out ok);
                Log(ok && guardado != null ? "Prefab guardado." : "ERROR: no se pudo guardar el prefab.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Log("Tamaño del prefab después: " + Megas(RutaPrefab) + " MB (tope 60).");
            }
            catch (Exception e)
            {
                Log("ERROR: " + e);
                Debug.LogError("[ZonaCerroT1394] " + e);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(zona);
            }
            Escribir(RutaInforme, informe);
        }

        /// <summary>Cada paso va aparte: si uno truena, se anota y los demás siguen.</summary>
        private static void Paso(string nombre, Action accion)
        {
            try
            {
                accion();
            }
            catch (Exception e)
            {
                Log("ERROR en el paso '" + nombre + "': " + e);
                Debug.LogError("[ZonaCerroT1394] " + nombre + ": " + e);
            }
        }

        private static Ctx Abrir(GameObject zona, bool borrarNuevo = true)
        {
            Physics.SyncTransforms();
            Ctx c = new Ctx
            {
                Zona = zona,
                Fisica = zona.scene.GetPhysicsScene(),
                Terreno = zona.transform.Find("Terreno"),
                Calles = zona.transform.Find("Calles"),
                Casas = zona.transform.Find("Casas"),
                Detalles = zona.transform.Find("Detalles"),
                Carros = zona.transform.Find("Carros"),
            };
            Transform viejo = zona.transform.Find(Raiz);
            if (viejo != null && borrarNuevo)
            {
                Transform reem = viejo.Find("Reemplazados");
                if (reem != null && reem.childCount > 0)
                {
                    // Restaurar no pudo devolver todo: no se borra para no perder casas.
                    Log("AVISO: quedan objetos en Reemplazados que no se pudieron devolver; se deja la raíz vieja.");
                }
                else
                {
                    Object.DestroyImmediate(viejo.gameObject);
                    Physics.SyncTransforms();
                }
            }
            foreach (MechaCyberpunk.Core.Mundo.CalleZona cz in zona.GetComponentsInChildren<MechaCyberpunk.Core.Mundo.CalleZona>(false))
            {
                c.Ejes.Add(cz.Puntos());
            }
            Log("Calles de zona: " + c.Ejes.Count + ", casas: " + (c.Casas == null ? 0 : c.Casas.childCount) + ".");
            return c;
        }

        // ================================================================ Registro: quitar sin perder

        /// <summary>Devuelve a su sitio lo que una corrida anterior quitó (en el orden de sus índices de hermano) y lo reactiva.</summary>
        private static int Restaurar(GameObject zona)
        {
            Transform raiz = zona.transform.Find(Raiz);
            if (raiz == null)
            {
                return 0;
            }
            Transform reem = raiz.Find("Reemplazados");
            Transform reg = raiz.Find("Registro");
            if (reem == null || reg == null)
            {
                return 0;
            }
            if (reem.childCount != reg.childCount)
            {
                Log("AVISO: el registro de reemplazos no coincide (" + reem.childCount + " vs " + reg.childCount + "); no se devuelve nada.");
                return 0;
            }
            List<Transform> objetos = new List<Transform>();
            List<string> rutas = new List<string>();
            List<int> indices = new List<int>();
            for (int i = 0; i < reem.childCount; i++)
            {
                string[] p = reg.GetChild(i).name.Split('|');
                int idx;
                if (p.Length < 3 || !int.TryParse(p[2], out idx))
                {
                    continue;
                }
                objetos.Add(reem.GetChild(i));
                rutas.Add(p[1]);
                indices.Add(idx);
            }
            int[] orden = Enumerable.Range(0, objetos.Count).OrderBy(i => rutas[i], StringComparer.Ordinal).ThenBy(i => indices[i]).ToArray();
            int n = 0;
            foreach (int i in orden)
            {
                Transform padre = rutas[i].Length == 0 ? zona.transform : zona.transform.Find(rutas[i]);
                if (padre == null)
                {
                    Log("AVISO: no existe " + rutas[i] + " para devolver " + objetos[i].name + ".");
                    continue;
                }
                objetos[i].SetParent(padre, true);
                objetos[i].SetSiblingIndex(Mathf.Min(indices[i], padre.childCount - 1));
                objetos[i].gameObject.SetActive(true);
                n++;
            }
            Physics.SyncTransforms();
            return n;
        }

        private static string RutaPadre(Transform zona, Transform padre)
        {
            List<string> partes = new List<string>();
            Transform t = padre;
            while (t != null && t != zona)
            {
                partes.Insert(0, t.name);
                t = t.parent;
            }
            return string.Join("/", partes);
        }

        /// <summary>Guarda en Reemplazados (desactivados) los objetos dados, anotando de dónde salieron. Primero se anotan todos y luego se mueven.</summary>
        private static void Confirmar(Ctx c, List<Transform> lista)
        {
            List<string> nombres = new List<string>();
            foreach (Transform t in lista)
            {
                nombres.Add("Orig|" + RutaPadre(c.Zona.transform, t.parent) + "|" + t.GetSiblingIndex() + "|" + t.name);
            }
            for (int i = 0; i < lista.Count; i++)
            {
                GameObject marca = new GameObject(nombres[i]);
                marca.transform.SetParent(c.Reg, false);
                lista[i].SetParent(c.Reem, true);
                lista[i].gameObject.SetActive(false);
            }
        }

        // ================================================================ 0. Limpieza de lo de T1385

        private static void LimpiarT1385(Ctx c)
        {
            Transform t85 = c.Zona.transform.Find(ZonaCerroT1385.Raiz);
            if (t85 == null)
            {
                Log("T1385: no existe " + ZonaCerroT1385.Raiz + "; no hay nada que limpiar.");
                return;
            }
            Transform casas = t85.Find("Casas");
            int accesos = 0, casasQuitadas = 0, extras = 0;
            if (casas != null)
            {
                // Casas que invaden la calle (los FALLO de t1385-validar.txt).
                foreach (Transform casa in casas.Cast<Transform>().Where(t => t.name.StartsWith("CC0")).ToList())
                {
                    Bounds b = Cubierta(casa);
                    float d = DistanciaAEjes(c, new Vector3(b.center.x, 0f, b.center.z)) - b.extents.x;
                    if (d >= 5.5f)
                    {
                        continue;
                    }
                    string numero = casa.name.Substring(casa.name.LastIndexOf('_') + 1);
                    Log("T1385: se quita " + casa.name + " (a " + d.ToString("0.00") + " m del eje, invade la calle).");
                    Bounds ancho = b;
                    ancho.Expand(new Vector3(1.5f, 4f, 1.5f));
                    Transform[] sueltos = t85.GetComponentsInChildren<Transform>(true).Where(t => t != t85 && t.parent != null && (t.parent == t85.Find("MurosCasas") || t.parent == t85)).ToArray();
                    foreach (Transform s in sueltos)
                    {
                        if (s.name == "DetalleAzotea_" + numero || (s.parent.name == "MurosCasas" && ancho.Contains(CentroDe(s))))
                        {
                            Object.DestroyImmediate(s.gameObject);
                            extras++;
                        }
                    }
                    Object.DestroyImmediate(casa.gameObject);
                    casasQuitadas++;
                }
                // Las escaleras de servicio de T1385 tenían la salida del lado de la calle: se rehacen más abajo con la orientación correcta.
                foreach (Transform a in casas.Cast<Transform>().Where(t => t.name.StartsWith("Acceso_T1385_")).ToList())
                {
                    Object.DestroyImmediate(a.gameObject);
                    accesos++;
                }
            }
            // Carros de T1385 que queden cerca de la cámara K13 se quitan en QuitarCarros.
            Log("T1385: " + casasQuitadas + " casas quitadas, " + extras + " cosas sueltas suyas, " + accesos + " escaleras de servicio rehechas.");
            Physics.SyncTransforms();
        }

        // ================================================================ 4a. Cámara K13 sin carros

        private static void QuitarCarros(Ctx c)
        {
            Vector3 adelante = Plano(K13Hacia - K13Desde).normalized;
            int quitados = 0;
            List<Transform> propios = new List<Transform>();
            if (c.Carros != null)
            {
                propios.AddRange(c.Carros.Cast<Transform>().Where(t => t.gameObject.activeSelf));
            }
            Transform t85 = c.Zona.transform.Find(ZonaCerroT1385.Raiz);
            List<Transform> deT85 = new List<Transform>();
            if (t85 != null && t85.Find("Carros") != null)
            {
                deT85.AddRange(t85.Find("Carros").Cast<Transform>());
            }
            List<Transform> retirar = new List<Transform>();
            foreach (Transform t in propios.Concat(deT85))
            {
                Vector3 p = Plano(t.position - K13Desde);
                float adelanteM = Vector3.Dot(p, adelante);
                float lado = Mathf.Abs(Vector3.Dot(p, new Vector3(-adelante.z, 0f, adelante.x)));
                bool cerca = p.magnitude < 7f || (adelanteM > 0f && adelanteM < 16f && lado < 4f);
                if (!cerca)
                {
                    continue;
                }
                if (deT85.Contains(t))
                {
                    Object.DestroyImmediate(t.gameObject);
                }
                else
                {
                    retirar.Add(t);
                }
                quitados++;
            }
            if (retirar.Count > 0)
            {
                Confirmar(c, retirar);
            }
            Physics.SyncTransforms();
            Log("Carros quitados cerca de la cámara K13 (a menos de 7 m o en el cono de 16 m delante): " + quitados + ".");
        }

        // ================================================================ 4b. Vista abierta hacia la ciudad (este)

        /// <summary>Quita (con tope) las casas del Cerro que están a la derecha de la cámara K13 y son más altas que sus ojos: tapan la ciudad.</summary>
        private static void AbrirVista(Ctx c)
        {
            Vector3 adelante = Plano(K13Hacia - K13Desde).normalized;
            List<VariarCerroT1364.CasaT> casas = VariarCerroT1364.Leer(c.Zona.transform);
            List<(VariarCerroT1364.CasaT casa, float dist)> tapan = new List<(VariarCerroT1364.CasaT, float)>();
            foreach (VariarCerroT1364.CasaT k in casas)
            {
                Vector3 d = Plano(new Vector3((k.X0 + k.X1) * 0.5f, 0f, (k.Z0 + k.Z1) * 0.5f) - K13Desde);
                float dist = d.magnitude;
                float ang = Vector3.SignedAngle(adelante, d, Vector3.up); // positivo = a la derecha
                if (dist > 6f && dist < 75f && ang > 25f && ang < 100f && k.Top > K13Desde.y + 0.5f)
                {
                    tapan.Add((k, dist));
                }
            }
            int quitadas = 0;
            foreach ((VariarCerroT1364.CasaT casa, float dist) t in tapan.OrderBy(t => t.dist))
            {
                if (quitadas >= TopeCasasVista)
                {
                    break;
                }
                Confirmar(c, Afectados(c, t.casa));
                quitadas++;
            }
            Physics.SyncTransforms();
            Log("Vista a la ciudad: " + tapan.Count + " casas tapaban el lado derecho de K13; se quitaron " + quitadas + " (tope " + TopeCasasVista + ").");
        }

        // ================================================================ 1. Casas nuevas en lugar de casas existentes

        private static List<Transform> Afectados(Ctx c, VariarCerroT1364.CasaT casa)
        {
            List<Transform> l = new List<Transform> { casa.Raiz };
            if (c.Detalles != null)
            {
                foreach (Transform t in c.Detalles.Cast<Transform>().ToList())
                {
                    if (!t.gameObject.activeSelf)
                    {
                        continue;
                    }
                    Vector3 p = CentroDe(t);
                    if (p.x > casa.X0 - 0.4f && p.x < casa.X1 + 0.4f && p.z > casa.Z0 - 0.4f && p.z < casa.Z1 + 0.4f && p.y > casa.Base + 0.8f && p.y < casa.Top + 10f)
                    {
                        l.Add(t);
                    }
                }
            }
            return l;
        }

        private static void CambiarCasas(Ctx c, List<Techo> nuevas)
        {
            List<GameObject> prefabs = new List<GameObject>();
            foreach (string codigo in CodigosCasa)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(ZonaCerroT1385.CarpetaCC + codigo + ".prefab");
                if (p == null)
                {
                    Log("FALTA " + ZonaCerroT1385.CarpetaCC + codigo + ".prefab; se salta.");
                }
                else
                {
                    prefabs.Add(p);
                }
            }
            if (prefabs.Count == 0)
            {
                Log("Casas: no hay prefabs CC; no se cambia nada.");
                return;
            }
            Transform padre = Hijo(c.Nuevo, "Casas");
            List<VariarCerroT1364.CasaT> casas = VariarCerroT1364.Leer(c.Zona.transform)
                .Where(k => k.Kit && !k.Bloqueada && k.Ancho >= 4.5f && (k.X1 - k.X0) >= 4.5f && k.Top - k.Base >= 3.5f && k.Top - k.Base <= 15f).ToList();
            casas = casas.OrderBy(k => Mathf.Min(Dist2(Centro(k), K13Desde), Dist2(Centro(k), A13Hacia))).ToList();
            int hechas = 0, rechazadas = 0, sinTamano = 0, intentos = 0;
            Dictionary<string, int> porTipo = new Dictionary<string, int>();
            for (int i = 0; i < casas.Count && hechas < TopeCambios; i++)
            {
                if (i % 3 != 0)
                {
                    continue; // una de cada tres, de las más cercanas a K13 y A13 hacia afuera: las vecinas quedan distintas
                }
                intentos++;
                VariarCerroT1364.CasaT casa = casas[i];
                GameObject prefab = prefabs[hechas % prefabs.Count];
                Vector3 m = Medida(prefab);
                if (m.x < 0.1f || m.y < 0.1f || m.z < 0.1f)
                {
                    sinTamano++;
                    continue;
                }
                float ancho = casa.Ancho, fondo = casa.X1 - casa.X0;
                float e = Mathf.Min((ancho - 0.3f) / m.x, (fondo - 0.2f) / m.z);
                if (e < 0.4f)
                {
                    rechazadas++;
                    continue;
                }
                e = Mathf.Min(e, 3f);
                float ys = Mathf.Clamp((casa.Top - casa.Base) / (m.y * e), 0.85f, 1.3f);
                float anchoN = m.x * e, fondoN = m.z * e, altoN = m.y * e * ys;
                if (altoN > 14.5f || altoN < 3f)
                {
                    rechazadas++;
                    continue;
                }
                float n = casa.Normal;
                float zc = (casa.Z0 + casa.Z1) * 0.5f;
                Vector3 centro = new Vector3(casa.Frente - n * fondoN * 0.5f, casa.Base + altoN * 0.5f, zc);
                Vector3 mitad = new Vector3(Mathf.Max(0.2f, fondoN * 0.5f - 0.2f), Mathf.Max(0.2f, altoN * 0.5f - 0.45f), Mathf.Max(0.2f, anchoN * 0.5f - 0.2f));
                List<Transform> afectados = Afectados(c, casa);
                bool teniaAcceso = afectados.Any(t => t != casa.Raiz && t.GetComponentInChildren<PuntoTrepar>(true) != null);
                foreach (Transform t in afectados)
                {
                    t.gameObject.SetActive(false);
                }
                Physics.SyncTransforms();
                if (!Libre(c, centro, mitad, Quaternion.identity))
                {
                    foreach (Transform t in afectados)
                    {
                        t.gameObject.SetActive(true);
                    }
                    Physics.SyncTransforms();
                    rechazadas++;
                    continue;
                }
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                g.name = prefab.name + "_" + hechas;
                g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(new Vector3(n, 0f, 0f)));
                g.transform.localScale = new Vector3(e, e * ys, e);
                Bounds b = Cubierta(g.transform);
                float frenteB = n > 0f ? b.max.x : b.min.x;
                g.transform.position += new Vector3(casa.Frente - frenteB, casa.Base - b.min.y, zc - b.center.z);
                Colisionar(g);
                Confirmar(c, afectados);
                Physics.SyncTransforms();
                b = Cubierta(g.transform);
                Techo techo = new Techo { Raiz = g.transform, Nombre = g.name, X0 = b.min.x, X1 = b.max.x, Z0 = b.min.z, Z1 = b.max.z, Normal = n, Forzar = teniaAcceso, Y = b.max.y };
                nuevas.Add(techo);
                DetalleAzotea(c, g.transform, b, hechas);
                porTipo[prefab.name] = porTipo.ContainsKey(prefab.name) ? porTipo[prefab.name] + 1 : 1;
                hechas++;
            }
            Log("Casas cambiadas: " + hechas + " de " + intentos + " intentos (" + rechazadas + " rechazadas por no caber o estorbar, " + sinTamano + " sin medida). Mezcla: "
                + string.Join(", ", porTipo.Select(p => p.Key + " x" + p.Value)) + ".");
        }

        private static void DetalleAzotea(Ctx c, Transform casa, Bounds b, int numero)
        {
            float nivel;
            if (!NivelTecho(c, b.min.x + 0.5f, b.max.x - 0.5f, b.min.z + 0.5f, b.max.z - 0.5f, b.max.y, out nivel))
            {
                Log("AVISO: " + casa.name + " no tiene azotea plana medible; sin tinaco ni antena.");
                return;
            }
            Transform d = Hijo(c.Det, "Azotea_" + numero);
            int puestos = 0;
            bool traeTinaco = casa.name.StartsWith("CC07");
            GameObject tinaco = KitS2c.Prefab("C09_Tinaco");
            GameObject baseTinaco = KitS2c.Prefab("TK01_BaseTinaco");
            if (!traeTinaco && tinaco != null && baseTinaco != null)
            {
                Vector3 mb = Medida(baseTinaco);
                Vector3 escalaBase = new Vector3(1.5f / Mathf.Max(0.1f, mb.x), 0.7f / Mathf.Max(0.1f, mb.y), 1.5f / Mathf.Max(0.1f, mb.z));
                GameObject gb = PonerEnAzotea(c, d, baseTinaco, escalaBase, b, nivel, numero, 0.7f);
                if (gb != null)
                {
                    Bounds bb = Cubierta(gb.transform);
                    GameObject gt = (GameObject)PrefabUtility.InstantiatePrefab(tinaco, d);
                    gt.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    gt.transform.localScale = Vector3.one * 1.1f;
                    Bounds bt = Cubierta(gt.transform);
                    gt.transform.position += new Vector3(bb.center.x - bt.center.x, bb.max.y - bt.min.y, bb.center.z - bt.center.z);
                    puestos += 2;
                }
            }
            GameObject antena = KitS2c.Prefab("CY04_AntenaAzotea");
            if (antena != null && PonerEnAzotea(c, d, antena, Vector3.one * 0.8f, b, nivel, numero + 2, 1.0f) != null)
            {
                puestos++;
            }
            GameObject tendedero = AssetDatabase.LoadAssetAtPath<GameObject>(ZonaCerroT1385.CarpetaCC + "CC04_Tendedero.prefab");
            GameObject lavadero = AssetDatabase.LoadAssetAtPath<GameObject>(ZonaCerroT1385.CarpetaCC + "CC05_Lavadero.prefab");
            if (tendedero != null && numero % 2 == 0 && PonerEnAzotea(c, d, tendedero, Vector3.one, b, nivel, numero + 4, 0.5f) != null)
            {
                puestos++;
            }
            else if (lavadero != null && numero % 3 == 1 && PonerEnAzotea(c, d, lavadero, Vector3.one, b, nivel, numero + 1, 0.5f) != null)
            {
                puestos++;
            }
            if (puestos == 0)
            {
                Object.DestroyImmediate(d.gameObject);
            }
        }

        /// <summary>Pone <paramref name="prefab"/> sobre la azotea (nivel dado) en un sitio donde el techo es plano y la caja está libre. Null si no hay sitio.</summary>
        private static GameObject PonerEnAzotea(Ctx c, Transform padre, GameObject prefab, Vector3 escala, Bounds casa, float nivel, int variante, float margenAlto)
        {
            Vector3 m = Medida(prefab);
            Vector3 tam = new Vector3(m.x * escala.x, m.y * escala.y, m.z * escala.z);
            float[] fx = { 0.25f, 0.75f, 0.25f, 0.75f, 0.5f, 0.5f };
            float[] fz = { 0.25f, 0.25f, 0.75f, 0.75f, 0.25f, 0.75f };
            for (int k = 0; k < fx.Length; k++)
            {
                int j = (k + variante) % fx.Length;
                float x = Mathf.Lerp(casa.min.x, casa.max.x, fx[j]);
                float z = Mathf.Lerp(casa.min.z, casa.max.z, fz[j]);
                if (x - tam.x * 0.5f < casa.min.x + 0.3f || x + tam.x * 0.5f > casa.max.x - 0.3f || z - tam.z * 0.5f < casa.min.z + 0.3f || z + tam.z * 0.5f > casa.max.z - 0.3f)
                {
                    continue;
                }
                bool plano = true;
                foreach (Vector2 q in new[] { new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f) })
                {
                    plano &= TechoEn(c, x + q.x * tam.x * 0.45f, z + q.y * tam.z * 0.45f, nivel);
                }
                if (!plano)
                {
                    continue;
                }
                float alto = Mathf.Max(0.2f, tam.y - 0.3f);
                if (!Libre(c, new Vector3(x, nivel + 0.2f + alto * 0.5f, z), new Vector3(tam.x * 0.5f - 0.05f, alto * 0.5f, tam.z * 0.5f - 0.05f), Quaternion.identity))
                {
                    continue;
                }
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                g.transform.localScale = escala;
                Bounds b = Cubierta(g.transform);
                g.transform.position += new Vector3(x - b.center.x, nivel - b.min.y, z - b.center.z);
                Physics.SyncTransforms();
                return g;
            }
            return null;
        }

        private static bool TechoEn(Ctx c, float x, float z, float nivel)
        {
            RaycastHit h;
            return c.Fisica.Raycast(new Vector3(x, nivel + 1.5f, z), Vector3.down, out h, 3f, ~0, QueryTriggerInteraction.Ignore) && h.normal.y > 0.9f && Mathf.Abs(h.point.y - nivel) < 0.25f && !EsPiso(c, h.collider.transform);
        }

        // ================================================================ 2. Azoteas para parkour

        private static void Azoteas(Ctx c, List<Techo> nuevas)
        {
            List<Techo> techos = Techos(c, nuevas);
            Log("Azoteas medidas: " + techos.Count + " (originales del kit, casas de T1385 y casas nuevas de esta ronda).");
            // ---- Pares vecinos
            List<(int a, int b, bool ejeX, float gap, float dy)> pares = new List<(int, int, bool, float, float)>();
            for (int i = 0; i < techos.Count; i++)
            {
                for (int j = i + 1; j < techos.Count; j++)
                {
                    Techo A = techos[i], B = techos[j];
                    float gx = Mathf.Max(A.X0, B.X0) - Mathf.Min(A.X1, B.X1);
                    float gz = Mathf.Max(A.Z0, B.Z0) - Mathf.Min(A.Z1, B.Z1);
                    float dy = Mathf.Abs(A.Y - B.Y);
                    if (dy > DesnivelMaximo)
                    {
                        continue;
                    }
                    if (gz >= -0.3f && gz <= SeparacionMaxima && -gx >= 3f)
                    {
                        pares.Add((i, j, false, Mathf.Max(0f, gz), dy));
                    }
                    else if (gx >= -0.3f && gx <= SeparacionMaxima && -gz >= 3f)
                    {
                        pares.Add((i, j, true, Mathf.Max(0f, gx), dy));
                    }
                }
            }
            // Primero los de desnivel grande (los que de verdad estorban para saltar), cerca de K13 y A13.
            pares = pares.Where(p => !(p.gap <= 0.5f && p.dy <= 0.5f))
                .OrderByDescending(p => p.dy >= 1.4f ? 1 : 0)
                .ThenBy(p => Mathf.Min(Dist2(CentroTecho(techos[p.a]), K13Desde), Dist2(CentroTecho(techos[p.a]), A13Hacia))).ToList();
            Log("Pares de azoteas vecinas que piden enlace: " + pares.Count + ".");
            Transform enlaces = Hijo(c.Nuevo, "Enlaces");
            int[] raiz = Enumerable.Range(0, techos.Count).ToArray();
            int escaleras = 0, tablones = 0, fallidos = 0, id = 0;
            foreach ((int a, int b, bool ejeX, float gap, float dy) p in pares)
            {
                if (escaleras + tablones >= TopeEnlaces)
                {
                    break;
                }
                Techo A = techos[p.a], B = techos[p.b];
                if (A.Enlaces >= 2 || B.Enlaces >= 2)
                {
                    continue;
                }
                Techo bajo = A.Y <= B.Y ? A : B, alto = A.Y <= B.Y ? B : A;
                bool hecho = Enlazar(c, enlaces, bajo, alto, p.ejeX, id, out bool conEscalera);
                if (!hecho)
                {
                    fallidos++;
                    continue;
                }
                id++;
                A.Enlaces++;
                B.Enlaces++;
                if (conEscalera)
                {
                    escaleras++;
                }
                else
                {
                    tablones++;
                }
                raiz[Buscar(raiz, p.a)] = Buscar(raiz, p.b);
            }
            Log("Enlaces entre azoteas: " + escaleras + " escaleras de concreto con tablón y " + tablones + " tablones; " + fallidos + " pares no cupieron (algo estorbaba).");

            // ---- Salidas: toda azotea de T1385 o casa nueva que tenía escalera de servicio, y todo grupo unido sin ninguna.
            Transform accesos = Hijo(c.Nuevo, "Accesos");
            int hechos = 0, sinSitio = 0;
            foreach (Techo t in techos.Where(t => t.Forzar))
            {
                if (AccesoTecho(c, accesos, t))
                {
                    hechos++;
                }
                else
                {
                    sinSitio++;
                }
            }
            foreach (IGrouping<int, int> grupo in Enumerable.Range(0, techos.Count).Where(i => techos[i].Enlaces > 0).GroupBy(i => Buscar(raiz, i)))
            {
                List<Techo> miembros = grupo.Select(i => techos[i]).ToList();
                if (miembros.Any(t => TieneAcceso(c, t)))
                {
                    continue;
                }
                bool puesto = false;
                foreach (Techo t in miembros.OrderBy(t => t.Y))
                {
                    if (AccesoTecho(c, accesos, t))
                    {
                        puesto = true;
                        hechos++;
                        break;
                    }
                }
                if (!puesto)
                {
                    sinSitio++;
                    Log("AVISO: un grupo de " + miembros.Count + " azoteas unidas (cerca de " + miembros[0].Nombre + ") no pudo recibir escalera de servicio; se baja saltando.");
                }
            }
            Log("Escaleras de servicio (PuntoTrepar) puestas: " + hechos + "; sin sitio: " + sinSitio + ".");
        }

        private static int Buscar(int[] raiz, int i)
        {
            while (raiz[i] != i)
            {
                raiz[i] = raiz[raiz[i]];
                i = raiz[i];
            }
            return i;
        }

        private static List<Techo> Techos(Ctx c, List<Techo> nuevas)
        {
            List<Techo> res = new List<Techo>();
            foreach (VariarCerroT1364.CasaT k in VariarCerroT1364.Leer(c.Zona.transform))
            {
                AgregarTecho(c, res, k.Raiz, k.X0, k.X1, k.Z0, k.Z1, k.Top, k.Normal, false);
            }
            Transform t85 = c.Zona.transform.Find(ZonaCerroT1385.Raiz);
            Transform g85 = t85 == null ? null : t85.Find("Casas");
            if (g85 != null)
            {
                foreach (Transform t in g85.Cast<Transform>().Where(t => t.name.StartsWith("CC0")))
                {
                    Bounds b = Cubierta(t);
                    AgregarTecho(c, res, t, b.min.x, b.max.x, b.min.z, b.max.z, b.max.y, t.forward.x >= 0f ? 1f : -1f, true);
                }
            }
            foreach (Techo t in nuevas)
            {
                // Las nuevas ya traen su footprint; el nivel se mide igual.
                float y;
                if (NivelTecho(c, t.X0 + 0.5f, t.X1 - 0.5f, t.Z0 + 0.5f, t.Z1 - 0.5f, t.Y, out y))
                {
                    t.Y = y;
                    res.Add(t);
                }
            }
            return res;
        }

        private static void AgregarTecho(Ctx c, List<Techo> res, Transform raiz, float x0, float x1, float z0, float z1, float top, float normal, bool forzar)
        {
            if (!raiz.gameObject.activeInHierarchy || x1 - x0 < 3f || z1 - z0 < 3f)
            {
                return;
            }
            float y;
            if (!NivelTecho(c, x0 + 0.5f, x1 - 0.5f, z0 + 0.5f, z1 - 0.5f, top, out y))
            {
                return;
            }
            res.Add(new Techo { Raiz = raiz, Nombre = raiz.name, X0 = x0, X1 = x1, Z0 = z0, Z1 = z1, Y = y, Normal = normal, Forzar = forzar });
        }

        /// <summary>Nivel de la azotea: el valor más común (≥ 3 de 9 rayos) entre los puntos planos que ve un rayo desde arriba.</summary>
        private static bool NivelTecho(Ctx c, float x0, float x1, float z0, float z1, float top, out float nivel)
        {
            nivel = top;
            List<float> ys = new List<float>();
            float[] f = { 0.2f, 0.5f, 0.8f };
            foreach (float fx in f)
            {
                foreach (float fz in f)
                {
                    RaycastHit h;
                    Vector3 origen = new Vector3(Mathf.Lerp(x0, x1, fx), top + 6f, Mathf.Lerp(z0, z1, fz));
                    if (c.Fisica.Raycast(origen, Vector3.down, out h, 12f, ~0, QueryTriggerInteraction.Ignore) && h.normal.y > 0.95f
                        && h.point.y > top - 4f && h.point.y < top + 1.3f && !EsPiso(c, h.collider.transform))
                    {
                        ys.Add(h.point.y);
                    }
                }
            }
            if (ys.Count < 3)
            {
                return false;
            }
            float mejor = 0f;
            int cuenta = 0;
            foreach (float y in ys)
            {
                int k = ys.Count(v => Mathf.Abs(v - y) < 0.15f);
                if (k > cuenta || (k == cuenta && y < mejor))
                {
                    cuenta = k;
                    mejor = y;
                }
            }
            if (cuenta < 3)
            {
                return false;
            }
            nivel = ys.Where(v => Mathf.Abs(v - mejor) < 0.15f).Average();
            return true;
        }

        private static Vector3 CentroTecho(Techo t)
        {
            return new Vector3((t.X0 + t.X1) * 0.5f, t.Y, (t.Z0 + t.Z1) * 0.5f);
        }

        private static float Lo(Techo t, bool ejeX) => ejeX ? t.X0 : t.Z0;
        private static float Hi(Techo t, bool ejeX) => ejeX ? t.X1 : t.Z1;
        private static Vector3 Mun(bool ejeX, float a, float t, float y) => ejeX ? new Vector3(a, y, t) : new Vector3(t, y, a);

        /// <summary>
        /// Une dos azoteas vecinas. Desnivel de 0.5 m o más: escalones de concreto sobre la azotea baja, pegados a su borde, que suben hasta el nivel de la alta,
        /// y un tablón plano sobre la separación (si la hay). Desnivel menor: un tablón inclinado de borde a borde. Todo con BoxCollider.
        /// </summary>
        private static bool Enlazar(Ctx c, Transform padre, Techo bajo, Techo alto, bool ejeX, int id, out bool conEscalera)
        {
            conEscalera = false;
            float sg = (Lo(alto, ejeX) + Hi(alto, ejeX)) >= (Lo(bajo, ejeX) + Hi(bajo, ejeX)) ? 1f : -1f; // hacia dónde está la alta sobre el eje
            float bordeBajo = sg > 0f ? Hi(bajo, ejeX) : Lo(bajo, ejeX);
            float bordeAlto = sg > 0f ? Lo(alto, ejeX) : Hi(alto, ejeX);
            float gap = Mathf.Max(0f, (bordeAlto - bordeBajo) * sg);
            bool perpX = !ejeX;
            float o0 = Mathf.Max(Lo(bajo, perpX), Lo(alto, perpX));
            float o1 = Mathf.Min(Hi(bajo, perpX), Hi(alto, perpX));
            float dy = alto.Y - bajo.Y;
            Transform g = Hijo(padre, "Enlace_" + id);
            if (dy < 0.5f)
            {
                if (gap < 0.5f)
                {
                    Object.DestroyImmediate(g.gameObject);
                    return false;
                }
                float tc = (o0 + o1) * 0.5f;
                if (!Libre(c, Mun(ejeX, bordeBajo + sg * gap * 0.5f, tc, Mathf.Min(bajo.Y, alto.Y) + 0.8f), ejeX ? new Vector3(gap * 0.5f - 0.03f, 0.5f, 0.5f) : new Vector3(0.5f, 0.5f, gap * 0.5f - 0.03f), Quaternion.identity))
                {
                    Object.DestroyImmediate(g.gameObject);
                    return false;
                }
                Tablon(c, g, "Tablon", Mun(ejeX, bordeBajo - sg * 0.4f, tc, bajo.Y), Mun(ejeX, bordeAlto + sg * 0.5f, tc, alto.Y), 1.1f);
                Physics.SyncTransforms();
                return true;
            }
            int n = Mathf.CeilToInt(dy / PasoMaximoEscalon);
            float h = dy / n;
            float huella = 0.4f;
            if (n * huella > o1 - o0 - 0.8f)
            {
                huella = 0.3f;
            }
            float largo = n * huella;
            if (largo > o1 - o0 - 0.8f)
            {
                Object.DestroyImmediate(g.gameObject);
                return false;
            }
            float aCentro = bordeBajo - sg * AnchoEscalon * 0.5f;
            float[] arranques = { o0 + 0.4f, o1 - 0.4f - largo, (o0 + o1) * 0.5f - largo * 0.5f };
            for (int s = 0; s < arranques.Length * 2; s++)
            {
                bool haciaMas = s < arranques.Length;
                float v0 = arranques[s % arranques.Length]; // inicio del tramo en el eje perpendicular
                float v1 = v0 + largo;
                Vector3 centroCaja = Mun(ejeX, aCentro, (v0 + v1) * 0.5f, bajo.Y + 0.45f + (dy + 0.15f) * 0.5f);
                Vector3 mitad = ejeX ? new Vector3(AnchoEscalon * 0.5f - 0.05f, (dy + 0.15f) * 0.5f, largo * 0.5f - 0.05f) : new Vector3(largo * 0.5f - 0.05f, (dy + 0.15f) * 0.5f, AnchoEscalon * 0.5f - 0.05f);
                if (!Libre(c, centroCaja, mitad, Quaternion.identity))
                {
                    continue;
                }
                float tTop = 0f;
                for (int k = 0; k < n; k++)
                {
                    float tCentro = haciaMas ? v0 + (k + 0.5f) * huella : v1 - (k + 0.5f) * huella;
                    float altoCaja = (k + 1) * h + 0.05f;
                    Vector3 pos = Mun(ejeX, aCentro, tCentro, bajo.Y - 0.05f + altoCaja * 0.5f);
                    Vector3 tam = ejeX ? new Vector3(AnchoEscalon, altoCaja, huella) : new Vector3(huella, altoCaja, AnchoEscalon);
                    Caja(c, g, "Escalon_" + k, pos, Quaternion.identity, tam, c.Concreto, true);
                    tTop = tCentro;
                }
                if (gap > 0.15f)
                {
                    bool libreHueco = gap < 0.3f || Libre(c, Mun(ejeX, bordeBajo + sg * gap * 0.5f, tTop, alto.Y + 0.6f), ejeX ? new Vector3(gap * 0.5f - 0.03f, 0.4f, 0.5f) : new Vector3(0.5f, 0.4f, gap * 0.5f - 0.03f), Quaternion.identity);
                    if (!libreHueco)
                    {
                        Object.DestroyImmediate(g.gameObject);
                        g = Hijo(padre, "Enlace_" + id);
                        continue;
                    }
                    Tablon(c, g, "Tablon", Mun(ejeX, bordeBajo - sg * 0.4f, tTop, alto.Y), Mun(ejeX, bordeAlto + sg * 0.5f, tTop, alto.Y), 1.0f);
                }
                Physics.SyncTransforms();
                conEscalera = true;
                return true;
            }
            Object.DestroyImmediate(g.gameObject);
            return false;
        }

        /// <summary>Tablón de madera (caja con collider) cuya cara de arriba va de <paramref name="a"/> a <paramref name="b"/>.</summary>
        private static void Tablon(Ctx c, Transform padre, string nombre, Vector3 a, Vector3 b, float ancho)
        {
            Vector3 d = b - a;
            float largo = d.magnitude;
            Quaternion giro = Quaternion.LookRotation(d.normalized, Vector3.up);
            Vector3 centro = (a + b) * 0.5f - giro * Vector3.up * 0.05f;
            Caja(c, padre, nombre, centro, giro, new Vector3(ancho, 0.1f, largo), c.Madera, true);
        }

        private static GameObject Caja(Ctx c, Transform padre, string nombre, Vector3 centro, Quaternion giro, Vector3 tam, Material mat, bool collider)
        {
            if (c.MallaCaja == null)
            {
                c.MallaCaja = KitS2c.MallaCaja(Vector3.one, 1f);
                c.MallaCaja.name = Raiz + "_CajaUnidad";
                c.Obra.Mallas.Add(c.MallaCaja);
            }
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(centro, giro);
            go.transform.localScale = tam;
            go.AddComponent<MeshFilter>().sharedMesh = c.MallaCaja;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider)
            {
                go.AddComponent<BoxCollider>().size = Vector3.one;
            }
            return go;
        }

        private static bool TieneAcceso(Ctx c, Techo t)
        {
            foreach (PuntoTrepar p in c.Zona.GetComponentsInChildren<PuntoTrepar>(false))
            {
                Vector3 q = p.transform.position;
                if (q.x > t.X0 - 1.2f && q.x < t.X1 + 1.2f && q.z > t.Z0 - 0.2f && q.z < t.Z1 + 0.2f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Escalera de servicio (PuntoTrepar) de la calle a la azotea. Convención del PuntoTrepar: el transform está al pie de la pared y su forward apunta HACIA
        /// la pared (hacia adentro de la casa); la salida cae sobre el techo, 0.8 m adentro. (T1385 la había puesto al revés; por eso fallaba "no hay techo bajo la salida".)
        /// </summary>
        private static bool AccesoTecho(Ctx c, Transform padre, Techo t)
        {
            float n = t.Normal;
            float frente = n > 0f ? t.X1 : t.X0;
            foreach (float z in new[] { t.Z1 - 0.7f, t.Z0 + 0.7f, (t.Z0 + t.Z1) * 0.5f })
            {
                float gy;
                if (!Piso(c, frente + n * 0.6f, z, t.Y + 2f, out gy))
                {
                    continue;
                }
                float alto = t.Y - gy;
                if (alto < 2.5f || alto > 13f)
                {
                    continue;
                }
                if (!Libre(c, new Vector3(frente + n * 0.45f, gy + alto * 0.5f + 0.2f, z), new Vector3(0.35f, alto * 0.5f - 0.1f, 0.35f), Quaternion.identity))
                {
                    continue;
                }
                GameObject go = new GameObject("Acceso_T1394_" + t.Nombre);
                go.transform.SetParent(padre, false);
                go.transform.SetPositionAndRotation(new Vector3(frente + n * 0.05f, gy, z), Quaternion.LookRotation(new Vector3(-n, 0f, 0f)));
                PuntoTrepar p = go.AddComponent<PuntoTrepar>();
                SerializedObject so = new SerializedObject(p);
                so.FindProperty("tipo").enumValueIndex = (int)TipoTrepar.Escalera;
                so.FindProperty("alturaSubida").floatValue = alto;
                so.FindProperty("salida").vector3Value = new Vector3(0f, alto, 0.8f);
                so.ApplyModifiedPropertiesWithoutUndo();
                p.AjustarTrigger();
                RaycastHit h;
                Vector3 s = p.Salida;
                if (!c.Fisica.Raycast(s + Vector3.up * 0.5f, Vector3.down, out h, 1.5f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(h.point.y - t.Y) > 0.4f)
                {
                    Object.DestroyImmediate(go);
                    continue;
                }
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    Caja(c, go.transform, "Riel", new Vector3(frente + n * 0.2f, gy + alto * 0.5f, z + lado * 0.25f), Quaternion.identity, new Vector3(0.06f, alto, 0.06f), c.Concreto, false);
                }
                for (float y = 0.4f; y < alto; y += 0.5f)
                {
                    Caja(c, go.transform, "Peldano", new Vector3(frente + n * 0.2f, gy + y, z), Quaternion.identity, new Vector3(0.05f, 0.05f, 0.5f), c.Concreto, false);
                }
                Physics.SyncTransforms();
                return true;
            }
            return false;
        }

        // ================================================================ 3. Neones

        private static void Neones(Ctx c)
        {
            Transform padre = Hijo(c.Nuevo, "Neones");
            List<Vector3> puestos = new List<Vector3>();
            int n = 0, tiendas = 0, escaleras = 0;
            // ---- Escaleras CE01: dos letreros por lado de cada escalera, sobre la pared de la casa de al lado.
            foreach (float z0 in ColocarT1355Cerro.ZEscaleras)
            {
                foreach (float lado in new[] { 1f, -1f })
                {
                    foreach (float x in new[] { -343f, -328f })
                    {
                        if (n >= TopeNeones)
                        {
                            break;
                        }
                        RaycastHit h;
                        if (!c.Fisica.Raycast(new Vector3(x, 60f, z0), Vector3.down, out h, 120f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }
                        if (NeonEnPared(c, padre, new Vector3(x, h.point.y + 2.6f, z0), new Vector3(0f, 0f, lado), 7f, n, puestos, true))
                        {
                            n++;
                            escaleras++;
                        }
                    }
                }
            }
            // ---- Tiendas: fachadas a la calle de las casas más cercanas a la cámara K13.
            List<Techo> casas = new List<Techo>();
            foreach (VariarCerroT1364.CasaT k in VariarCerroT1364.Leer(c.Zona.transform))
            {
                casas.Add(new Techo { Raiz = k.Raiz, Nombre = k.Raiz.name, X0 = k.X0, X1 = k.X1, Z0 = k.Z0, Z1 = k.Z1, Y = k.Base, Normal = k.Normal });
            }
            Transform nuevas = c.Nuevo.Find("Casas");
            if (nuevas != null)
            {
                foreach (Transform t in nuevas)
                {
                    Bounds b = Cubierta(t);
                    casas.Add(new Techo { Raiz = t, Nombre = t.name, X0 = b.min.x, X1 = b.max.x, Z0 = b.min.z, Z1 = b.max.z, Y = b.min.y, Normal = t.forward.x >= 0f ? 1f : -1f });
                }
            }
            foreach (Techo t in casas.Where(t => Dist2(CentroTecho(t), K13Desde) < 85f).OrderBy(t => Dist2(CentroTecho(t), K13Desde)))
            {
                if (n >= TopeNeones)
                {
                    break;
                }
                float frente = t.Normal > 0f ? t.X1 : t.X0;
                float zc = (t.Z0 + t.Z1) * 0.5f;
                float gy;
                if (!Piso(c, frente + t.Normal * 1.5f, zc, t.Y + 3f, out gy))
                {
                    continue;
                }
                Vector3 origen = new Vector3(frente + t.Normal * 2.4f, gy + 2.4f, zc);
                if (NeonEnPared(c, padre, origen, new Vector3(-t.Normal, 0f, 0f), 3.2f, n, puestos, false))
                {
                    n++;
                    tiendas++;
                }
            }
            Log("Neones nuevos: " + n + " (" + escaleras + " junto a las escaleras, " + tiendas + " en fachadas), cada uno con su Point Light chica con LuzNocturna y sin sombras (tope " + TopeNeones + ").");
        }

        private static bool NeonEnPared(Ctx c, Transform padre, Vector3 origen, Vector3 dir, float alcance, int numero, List<Vector3> puestos, bool escalera)
        {
            RaycastHit h;
            if (!c.Fisica.Raycast(origen, dir, out h, alcance, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            if (EsPiso(c, h.collider.transform) || Mathf.Abs(h.normal.y) > 0.25f || !EsCasa(c, h.collider.transform))
            {
                return false;
            }
            Vector3 nor = new Vector3(h.normal.x, 0f, h.normal.z).normalized;
            if (Vector3.Dot(nor, -dir) < 0.85f)
            {
                return false;
            }
            foreach (Vector3 q in puestos)
            {
                if ((q - h.point).magnitude < 6f)
                {
                    return false;
                }
            }
            if (!Libre(c, h.point + nor * 0.9f, new Vector3(0.55f, 0.4f, 0.55f), Quaternion.identity))
            {
                return false;
            }
            if (!escalera && DistanciaAEjes(c, h.point + nor * 1.0f) < 4.6f)
            {
                return false; // el letrero no entra al carril de la calle
            }
            Color color = numero % 2 == 0 ? KitS2c.Magenta : KitS2c.Turquesa;
            KitS2c.Neon(c.Obra, padre, h.point, nor, Vector3.forward, color, true);
            puestos.Add(h.point);
            return true;
        }

        private static bool EsCasa(Ctx c, Transform t)
        {
            while (t != null && t != c.Zona.transform)
            {
                if (t.name.StartsWith("Casa_") || t.name.StartsWith("CC0") || t.name.StartsWith("Extra_"))
                {
                    return true;
                }
                t = t.parent;
            }
            return false;
        }

        // ================================================================ Estático

        private static void Estatico(Ctx c)
        {
            const StaticEditorFlags banderas = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            int n = 0;
            foreach (Transform t in c.Nuevo.GetComponentsInChildren<Transform>(false))
            {
                if (t == c.Nuevo || t.GetComponent<Light>() != null || t.GetComponent<Rigidbody>() != null || t.GetComponent<PuntoTrepar>() != null || t.IsChildOf(c.Reem) || t.IsChildOf(c.Reg))
                {
                    continue;
                }
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, banderas);
                n++;
            }
            Log("Objetos marcados Static: " + n + ".");
        }

        // ================================================================ Ayudas

        private static void Log(string texto)
        {
            if (informe != null)
            {
                informe.AppendLine(texto);
            }
            Debug.Log("[ZonaCerroT1394] " + texto);
        }

        private static void Escribir(string ruta, StringBuilder sb)
        {
            string completa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllText(completa, sb.ToString());
        }

        private static string Megas(string ruta)
        {
            string completa = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
            return File.Exists(completa) ? (new FileInfo(completa).Length / 1048576f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "?";
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static Vector3 Plano(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private static float Dist2(Vector3 a, Vector3 b)
        {
            return Plano(a - b).magnitude;
        }

        private static Vector3 Centro(VariarCerroT1364.CasaT k)
        {
            return new Vector3((k.X0 + k.X1) * 0.5f, k.Base, (k.Z0 + k.Z1) * 0.5f);
        }

        private static Vector3 CentroDe(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
            {
                return t.position;
            }
            return Cubierta(t).center;
        }

        /// <summary>Tamaño del prefab sin girar ni escalar (por sus renderers).</summary>
        private static Vector3 Medida(GameObject prefab)
        {
            Vector3 ya;
            if (medidas.TryGetValue(prefab.name, out ya))
            {
                return ya;
            }
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Vector3 tam = Cubierta(g.transform).size;
            Object.DestroyImmediate(g);
            medidas[prefab.name] = tam;
            return tam;
        }

        private static Bounds Cubierta(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
            {
                return new Bounds(t.position, Vector3.zero);
            }
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
            {
                b.Encapsulate(rs[i].bounds);
            }
            return b;
        }

        /// <summary>Si el modelo no trae collider, cada malla visible recibe uno igual a ella (sin muros invisibles).</summary>
        private static void Colisionar(GameObject g)
        {
            if (g.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                return;
            }
            foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null && mf.GetComponent<Renderer>() != null)
                {
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
            }
        }

        private static bool EsPiso(Ctx c, Transform t)
        {
            return (c.Terreno != null && t.IsChildOf(c.Terreno)) || (c.Calles != null && t.IsChildOf(c.Calles));
        }

        private static bool Piso(Ctx c, float x, float z, float yDesde, out float y)
        {
            y = float.MinValue;
            bool ok = false;
            int n = c.Fisica.Raycast(new Vector3(x, yDesde, z), Vector3.down, bufRayos, 300f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (EsPiso(c, bufRayos[i].collider.transform) && bufRayos[i].point.y > y)
                {
                    y = bufRayos[i].point.y;
                    ok = true;
                }
            }
            return ok;
        }

        /// <summary>True si la caja no toca ningún collider que no sea el piso (terreno y calles).</summary>
        private static bool Libre(Ctx c, Vector3 centro, Vector3 mitad, Quaternion giro)
        {
            int n = c.Fisica.OverlapBox(centro, mitad, bufCol, giro, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (!EsPiso(c, bufCol[i].transform))
                {
                    return false;
                }
            }
            return true;
        }

        private static float DistanciaAEjes(Ctx c, Vector3 p)
        {
            float mejor = float.MaxValue;
            foreach (List<Vector3> eje in c.Ejes)
            {
                for (int i = 0; i + 1 < eje.Count; i++)
                {
                    Vector3 a = Plano(eje[i]), b = Plano(eje[i + 1]), ab = b - a;
                    float l2 = ab.sqrMagnitude;
                    float t = l2 < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(Plano(p) - a, ab) / l2);
                    mejor = Mathf.Min(mejor, (Plano(p) - (a + ab * t)).magnitude);
                }
            }
            return mejor;
        }

        // ================================================================ Validar

        /// <summary>
        /// Revisa el prefab: el registro de reemplazos cuadra; las casas nuevas tienen collider y están sobre su cimiento; los escalones no pasan de 0.45 m;
        /// los tablones se pisan; cada grupo de azoteas unidas tiene escalera de servicio con la salida sobre el techo; los neones tocan una pared y las luces
        /// no pasan de 30; la cámara K13 no tiene colliders pegados ni carros; hay vista libre hacia la derecha de K13 y hacia el frente de A13. Escribe Logs/t1394-validar.txt.
        /// </summary>
        public static void Validar()
        {
            StringBuilder sb = new StringBuilder("T-1394: validación de " + RutaPrefab + "\n");
            informe = sb;
            int fallos = 0;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            GameObject zona = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Ctx c = Abrir(zona, false);
                c.Nuevo = zona.transform.Find(Raiz);
                Physics.SyncTransforms();
                if (c.Nuevo == null)
                {
                    sb.AppendLine("FALLO: no existe " + Raiz + " (corre Aplicar).");
                    fallos++;
                }
                else
                {
                    c.Reem = c.Nuevo.Find("Reemplazados");
                    c.Reg = c.Nuevo.Find("Registro");
                    if (c.Reem == null || c.Reg == null || c.Reem.childCount != c.Reg.childCount)
                    {
                        sb.AppendLine("FALLO: el registro de reemplazos no cuadra.");
                        fallos++;
                    }
                    else
                    {
                        sb.AppendLine("Reemplazos guardados (casas, detalles y carros desactivados, con su lugar anotado): " + c.Reem.childCount + ".");
                    }
                    fallos += ValidarCasas(c, sb);
                    fallos += ValidarEnlaces(c, sb);
                    fallos += ValidarNeones(c, sb);
                    fallos += ValidarCamaras(c, sb);
                    Transform t85 = zona.transform.Find(ZonaCerroT1385.Raiz);
                    if (t85 != null && t85.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Acceso_T1385_")))
                    {
                        sb.AppendLine("FALLO: quedan escaleras de servicio de T1385 (salida al revés).");
                        fallos++;
                    }
                }
                string ruta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, RutaPrefab);
                long bytes = File.Exists(ruta) ? new FileInfo(ruta).Length : -1;
                sb.AppendLine("Tamaño del prefab: " + (bytes / 1048576f).ToString("0.0") + " MB " + (bytes >= TopeBytesPrefab ? "FALLO (>= 60 MB)" : "ok"));
                if (bytes >= TopeBytesPrefab)
                {
                    fallos++;
                }
                if (c.Nuevo != null)
                {
                    sb.AppendLine("Objetos nuevos activos: " + c.Nuevo.GetComponentsInChildren<Transform>(false).Length + " (tope ~3000).");
                }
            }
            finally
            {
                Object.DestroyImmediate(zona);
            }
            sb.AppendLine(fallos == 0 ? "RESULTADO: sin fallos." : "RESULTADO: " + fallos + " fallos.");
            Escribir(RutaValidar, sb);
            Debug.Log("[ZonaCerroT1394] " + sb);
        }

        private static int ValidarCasas(Ctx c, StringBuilder sb)
        {
            int fallos = 0;
            Transform casas = c.Nuevo.Find("Casas");
            if (casas == null)
            {
                sb.AppendLine("AVISO: no hay casas nuevas.");
                return 0;
            }
            List<Transform> cimientos = c.Casas == null ? new List<Transform>() : c.Casas.Cast<Transform>().Where(t => t.name == "Cimiento" && t.gameObject.activeSelf).ToList();
            int n = 0, mal = 0;
            foreach (Transform casa in casas)
            {
                n++;
                Bounds b = Cubierta(casa);
                if (casa.GetComponentsInChildren<Collider>(true).Length == 0)
                {
                    sb.AppendLine("FALLO: " + casa.name + " no tiene collider.");
                    fallos++;
                }
                float d = DistanciaAEjes(c, new Vector3(b.center.x, 0f, b.center.z)) - b.extents.x;
                if (d < 5.5f)
                {
                    sb.AppendLine("FALLO: " + casa.name + " invade la calle (a " + d.ToString("0.00") + " m del eje).");
                    fallos++;
                }
                bool apoyada = false;
                foreach (Transform cim in cimientos)
                {
                    Bounds bc = Cubierta(cim);
                    if (b.center.x > bc.min.x && b.center.x < bc.max.x && b.center.z > bc.min.z && b.center.z < bc.max.z && Mathf.Abs(bc.max.y - b.min.y) < 0.5f)
                    {
                        apoyada = true;
                        break;
                    }
                }
                if (!apoyada)
                {
                    float y;
                    apoyada = Piso(c, b.center.x, b.center.z, b.min.y + 3f, out y) && Mathf.Abs(y - b.min.y) < 1.2f;
                }
                if (!apoyada)
                {
                    mal++;
                    sb.AppendLine("AVISO: " + casa.name + " no se ve apoyada en un cimiento ni en el piso (base " + b.min.y.ToString("0.00") + ").");
                }
            }
            sb.AppendLine("Casas nuevas: " + n + ", sin apoyo claro: " + mal + ".");
            return fallos;
        }

        private static int ValidarEnlaces(Ctx c, StringBuilder sb)
        {
            int fallos = 0;
            Transform enlaces = c.Nuevo.Find("Enlaces");
            List<Techo> techos = Techos(c, new List<Techo>());
            int[] raiz = Enumerable.Range(0, techos.Count).ToArray();
            int grupos = 0;
            if (enlaces != null)
            {
                foreach (Transform g in enlaces)
                {
                    grupos++;
                    List<Transform> piezas = g.Cast<Transform>().ToList();
                    if (piezas.Count == 0 || piezas.Any(t => t.GetComponent<BoxCollider>() == null))
                    {
                        sb.AppendLine("FALLO: " + g.name + " tiene piezas sin collider.");
                        fallos++;
                    }
                    List<float> altos = piezas.Where(t => t.name.StartsWith("Escalon_")).Select(t => Cubierta(t).max.y).OrderBy(v => v).ToList();
                    for (int i = 1; i < altos.Count; i++)
                    {
                        if (altos[i] - altos[i - 1] > 0.45f)
                        {
                            sb.AppendLine("FALLO: " + g.name + " tiene un escalón de " + (altos[i] - altos[i - 1]).ToString("0.00") + " m.");
                            fallos++;
                        }
                    }
                    foreach (Transform t in piezas.Where(t => t.name == "Tablon"))
                    {
                        Bounds b = Cubierta(t);
                        RaycastHit h;
                        if (!c.Fisica.Raycast(new Vector3(b.center.x, b.max.y + 1f, b.center.z), Vector3.down, out h, 3f, ~0, QueryTriggerInteraction.Ignore) || h.collider.transform != t)
                        {
                            sb.AppendLine("FALLO: el tablón de " + g.name + " no se pisa por el centro (lo tapa otra cosa o no tiene collider).");
                            fallos++;
                        }
                    }
                    // Qué azoteas toca este enlace.
                    Bounds gb = Cubierta(g);
                    gb.Expand(new Vector3(1.2f, 0f, 1.2f));
                    List<int> toca = new List<int>();
                    for (int i = 0; i < techos.Count; i++)
                    {
                        Techo t = techos[i];
                        if (gb.max.x > t.X0 && gb.min.x < t.X1 && gb.max.z > t.Z0 && gb.min.z < t.Z1 && t.Y >= gb.min.y - 0.6f && t.Y <= gb.max.y + 0.6f)
                        {
                            toca.Add(i);
                        }
                    }
                    for (int i = 1; i < toca.Count; i++)
                    {
                        raiz[Buscar(raiz, toca[0])] = Buscar(raiz, toca[i]);
                    }
                }
            }
            int sinSalida = 0;
            foreach (IGrouping<int, int> grupo in Enumerable.Range(0, techos.Count).GroupBy(i => Buscar(raiz, i)).Where(gr => gr.Count() > 1))
            {
                if (!grupo.Any(i => TieneAcceso(c, techos[i])))
                {
                    sinSalida++;
                    sb.AppendLine("AVISO: un grupo de " + grupo.Count() + " azoteas unidas (cerca de " + techos[grupo.First()].Nombre + ") no tiene escalera de servicio; solo se baja saltando.");
                }
            }
            sb.AppendLine("Enlaces entre azoteas: " + grupos + "; grupos unidos sin escalera de servicio: " + sinSalida + ".");
            Transform accesos = c.Nuevo.Find("Accesos");
            int na = 0;
            if (accesos != null)
            {
                foreach (PuntoTrepar p in accesos.GetComponentsInChildren<PuntoTrepar>(false))
                {
                    na++;
                    RaycastHit h;
                    if (!c.Fisica.Raycast(p.Salida + Vector3.up * 0.5f, Vector3.down, out h, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        sb.AppendLine("FALLO: " + p.name + ": no hay techo bajo la salida " + p.Salida + ".");
                        fallos++;
                    }
                }
            }
            sb.AppendLine("PuntoTrepar de esta ronda: " + na + ".");
            return fallos;
        }

        private static int ValidarNeones(Ctx c, StringBuilder sb)
        {
            int fallos = 0;
            Transform neones = c.Nuevo.Find("Neones");
            int luces = c.Nuevo.GetComponentsInChildren<Light>(true).Length;
            sb.AppendLine("Luces nuevas: " + luces + " (tope " + TopeLuces + ").");
            if (luces > TopeLuces)
            {
                sb.AppendLine("FALLO: más luces nuevas que el tope.");
                fallos++;
            }
            foreach (Light l in c.Nuevo.GetComponentsInChildren<Light>(true))
            {
                if (l.shadows != LightShadows.None)
                {
                    sb.AppendLine("FALLO: la luz " + l.transform.parent?.name + " tiene sombras.");
                    fallos++;
                }
                if (l.GetComponent<LuzNocturna>() == null)
                {
                    sb.AppendLine("FALLO: la luz " + l.transform.parent?.name + " no tiene LuzNocturna.");
                    fallos++;
                }
            }
            if (neones != null)
            {
                int flotando = 0;
                foreach (Transform t in neones)
                {
                    bool pared = false;
                    int nc = c.Fisica.OverlapSphere(t.position, 1.8f, bufCol, ~0, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < nc && !pared; i++)
                    {
                        pared = !EsPiso(c, bufCol[i].transform) && EsCasa(c, bufCol[i].transform);
                    }
                    if (!pared)
                    {
                        flotando++;
                        sb.AppendLine("FALLO: el neón en " + t.position + " no toca ninguna pared (flota).");
                        fallos++;
                    }
                }
                sb.AppendLine("Neones: " + neones.childCount + ", flotando: " + flotando + ".");
            }
            return fallos;
        }

        private static int ValidarCamaras(Ctx c, StringBuilder sb)
        {
            int fallos = 0;
            Vector3 adelante = (K13Hacia - K13Desde).normalized;
            if (c.Fisica.OverlapSphere(K13Desde, 0.8f, bufCol, ~0, QueryTriggerInteraction.Ignore) > 0)
            {
                sb.AppendLine("FALLO: hay un collider a menos de 0.8 m de la cámara K13 (" + bufCol[0].transform.name + ").");
                fallos++;
            }
            RaycastHit h;
            if (c.Fisica.Raycast(K13Desde, adelante, out h, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                sb.AppendLine("FALLO: algo tapa la cámara K13 a " + h.distance.ToString("0.0") + " m (" + h.collider.transform.name + ").");
                fallos++;
            }
            foreach (Transform t in (c.Carros == null ? new List<Transform>() : c.Carros.Cast<Transform>().Where(t => t.gameObject.activeSelf)))
            {
                if (Dist2(t.position, K13Desde) < 7f)
                {
                    sb.AppendLine("FALLO: el carro " + t.name + " está a " + Dist2(t.position, K13Desde).ToString("0.0") + " m de la cámara K13.");
                    fallos++;
                }
            }
            // Vista a la derecha de K13: abanico horizontal de 30 a 90 grados a la derecha, a la altura de los ojos; cuántos rayos escapan sin tocar nada en 120 m.
            int libres = 0, total = 0;
            for (int ang = 30; ang <= 90; ang += 10)
            {
                Vector3 d = Quaternion.AngleAxis(ang, Vector3.up) * Plano(adelante).normalized;
                total++;
                if (!c.Fisica.Raycast(K13Desde, d, out h, 120f, ~0, QueryTriggerInteraction.Ignore))
                {
                    libres++;
                }
            }
            sb.AppendLine("Vista a la derecha de K13: " + libres + " de " + total + " rayos salen libres a 120 m (los que chocan tocan casas del Cerro o de Suburbios, que no son de este prefab)." + (libres * 2 < total ? " AVISO: la ciudad queda tapada, revisar la foto." : ""));
            float distA = Vector3.Distance(A13Desde, A13Hacia);
            if (c.Fisica.Raycast(A13Desde, (A13Hacia - A13Desde).normalized, out h, distA * 0.85f, ~0, QueryTriggerInteraction.Ignore))
            {
                sb.AppendLine("AVISO: algo se cruza en la cámara A13 a " + h.distance.ToString("0") + " m de " + distA.ToString("0") + " (" + h.collider.transform.name + ").");
            }
            return fallos;
        }

        // ================================================================ Fotos

        /// <summary>
        /// Foto K13 y A13 con la cámara de este archivo, igual que la referencia (referencia a la izquierda, juego a la derecha). No guarda nada; corre en una corrida
        /// de Unity aparte de Aplicar. Si algo estorba a la cámara K13 (collider a menos de 0.8 m o pared a menos de 6 m), la corre un poco por la calle.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T1394/Cerro ronda 2: fotos")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1394");
            Physics.SyncTransforms();
            Vector3 corrimiento = CamaraLibre(K13Desde, K13Hacia);
            Debug.Log("[ZonaCerroT1394] Cámara K13 corrida " + corrimiento + " desde " + K13Desde + ".");
            ColocarS2cLook.Foto(K13Desde + corrimiento, K13Hacia + corrimiento, "docs/capturas/t-1394/cerro-k13-juego.png", CampoFoto);
            ColocarS2cLook.Foto(A13Desde, A13Hacia, "docs/capturas/t-1394/cerro-a13-juego.png", CampoFoto);
            ColocarS2.LadoALado("imageassets/referencias/look/K13_ColoniaCerro.png", "docs/capturas/t-1394/cerro-k13-juego.png", "docs/capturas/t-1394/cerro-k13.png");
            ColocarS2.LadoALado("imageassets/referencias/look/A13_ColoniaCerro_Aerea.png", "docs/capturas/t-1394/cerro-a13-juego.png", "docs/capturas/t-1394/cerro-a13.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        private static Vector3 CamaraLibre(Vector3 desde, Vector3 hacia)
        {
            Vector3 dir = (hacia - desde).normalized;
            Vector3[] corrimientos =
            {
                Vector3.zero, new Vector3(1.5f, 0f, 0f), new Vector3(-1.5f, 0f, 0f), new Vector3(0f, 0f, -5f), new Vector3(1.5f, 0f, -5f),
                new Vector3(0f, 0f, 5f), new Vector3(1.5f, 0f, 5f), new Vector3(0f, 0f, -10f), new Vector3(1.5f, 0f, -10f), new Vector3(0f, 0f, 10f),
            };
            foreach (Vector3 o in corrimientos)
            {
                Vector3 p = desde + o;
                bool tapada = Physics.CheckSphere(p, 0.8f, ~0, QueryTriggerInteraction.Ignore) || Physics.Raycast(p, dir, 6f, ~0, QueryTriggerInteraction.Ignore);
                bool suelo = Physics.Raycast(p, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
                if (!tapada && suelo)
                {
                    return o;
                }
            }
            Debug.LogWarning("[ZonaCerroT1394] Ningún corrimiento dejó libre la cámara K13; se usa la posición original.");
            return Vector3.zero;
        }
    }
}

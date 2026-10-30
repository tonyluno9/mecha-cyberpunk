using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-419 (parte 1): coloca los modelos de la noche sobre lo que ya existe, igual que ColocarT250: nada se borra, lo viejo se apaga.
    /// - Barrio.prefab: fachadas C01/C02/C03 _cy en el lugar de las de T-250 (las de T-250 quedan inactivas), props de calle (CY01–CY10,
    ///   C13, C16–C19) calculados con la escena Ciudad_Prologo abierta para no tapar puertas, mostradores, NPC ni rutas, antenas CY04 en
    ///   azoteas, cámaras CY08 y pantalla E03 en Neón Alto, E01 en VivaCoin, E02 en El Mercado, A11a en la puerta del clandestino y los
    ///   muebles de la casa y el taller.
    /// - JaulaClandestino.prefab: marcador A08 sobre la jaula y escalera A11b en una esquina del sótano.
    /// - Trafico/Prefabs/Dron.prefab: CY07b como visual del dron de la policía.
    /// Se corre sobre los prefabs como están en git después de T-250 (ColocarT250.Colocar): no deshace una colocación anterior.
    /// El kit de pisos (C20a/b/d, C21, C22, C23) va en la parte 4b.
    /// </summary>
    public static class ColocarNoche
    {
        public const string NombreGrupo = "ArteNoche";
        public const string SufijoNoche = "_Noche";
        public const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        // Manzanas de 80 m centradas en x = -94, 0, 94 y z = ±47; la calle principal corre en X (z = 0) y las cruzadas en x = ±47.
        private static readonly Vector2[] CentrosManzana = { new Vector2(-94f, 47f), new Vector2(0f, 47f), new Vector2(94f, 47f), new Vector2(-94f, -47f), new Vector2(0f, -47f), new Vector2(94f, -47f) };
        private const float MitadManzana = 40f;
        private const float LimiteZona = 47f;
        private const float SeparacionFachada = 0.75f;   // las fachadas de T-250 salen 0.69 m del muro
        private const float OrillaBanqueta = 0.3f;
        private const float PasoPeatones = 1.05f;      // libre entre el muro y el modelo
        private const float RadioPuerta = 3.5f;
        private const float RadioPersonaje = 2.5f;

        private static readonly StringBuilder Informe = new StringBuilder();

        /// <summary>Un modelo a poner en coordenadas del mundo (el Barrio está en el origen sin giro).</summary>
        private sealed class Puesto
        {
            public string Grupo;
            public string Codigo;
            public Vector3 Posicion;
            public float Giro;
            public Vector3 Escala = Vector3.one;
            public bool SinCollider;
            public bool SinSombra;
        }

        private static List<Bounds> obstaculos;
        private static List<Vector3> puertas;
        private static List<Vector3> personajes;

        // ------------------------------------------------------------------ entrada

        [MenuItem("Mecha Cyberpunk/T-419/Colocar en el juego")]
        public static void Colocar()
        {
            Informe.Clear();
            List<Puesto> calle = CalcularCalle();
            ColocarBarrio(calle);
            ColocarJaula();
            ColocarDron();
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/noche-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-419 colocado:\n" + Informe);
        }

        // ------------------------------------------------------------------ calle (se calcula con la escena abierta)

        private static List<Puesto> CalcularCalle()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
            ReunirObstaculos();
            List<Puesto> lista = new List<Puesto>();

            // Banquetas de la calle principal y de las cruzadas: pegados al muro, mirando a la calle.
            foreach (Vector2 c in CentrosManzana)
            {
                Vector3 haciaCalle = new Vector3(0f, 0f, -Mathf.Sign(c.y));
                RecorrerBanqueta(lista, new Vector3(c.x, 0f, c.y) + haciaCalle * MitadManzana, Vector3.right, haciaCalle);
                if (Mathf.Abs(c.x) > 1f)
                {
                    Vector3 haciaCruzada = new Vector3(-Mathf.Sign(c.x), 0f, 0f);
                    RecorrerBanqueta(lista, new Vector3(c.x, 0f, c.y) + haciaCruzada * MitadManzana, Vector3.forward, haciaCruzada);
                }
                else
                {
                    foreach (float lado in new[] { -1f, 1f })
                    {
                        Vector3 n = new Vector3(lado, 0f, 0f);
                        RecorrerBanqueta(lista, new Vector3(c.x, 0f, c.y) + n * MitadManzana, Vector3.forward, n);
                    }
                }
            }

            PonerEnTianguis(lista);
            PonerBarreras(lista);
            PonerMotos(lista);
            PonerEnParedes(lista, "CY08_CamaraCorporativa", 4.2f, 14f, x => x > LimiteZona + 5f, 0.6f);
            PonerEnParedes(lista, "CY07a_PlataformaDron", 3.6f, 30f, x => x > -LimiteZona, 1f);
            PonerAntenas(lista);
            PonerPantallaGigante(lista);
            PonerImplantes(lista);
            return lista;
        }

        private static void ReunirObstaculos()
        {
            obstaculos = new List<Bounds>();
            puertas = new List<Vector3>();
            personajes = new List<Vector3>();
            foreach (Collider col in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (col.isTrigger || col.bounds.max.y < 0.35f)
                {
                    continue;
                }
                obstaculos.Add(col.bounds);
            }
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || !r.enabled || r.bounds.max.y < 0.3f)
                {
                    continue;
                }
                Vector3 s = r.bounds.size;
                if (Mathf.Max(s.x, s.y, s.z) > 9f || DebajoDe(r.transform, "Fachada_Modular") || DebajoDe(r.transform, "Lluvia"))
                {
                    continue;
                }
                obstaculos.Add(r.bounds);
            }
            string[] tiposPersonaje = { "PersonajeApoyo", "Reclutable", "MostradorTienda", "PuntoAparicion", "PuntoHistoriaMundo", "CarroEstacionado", "JefePandilla", "MarcaLugar", "PandillaAPie", "MostradorPeleas", "DoctorClinica" };
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (m == null)
                {
                    continue;
                }
                string tipo = m.GetType().Name;
                if (tipo == "PuertaLocal")
                {
                    puertas.Add(m.transform.position);
                    Transform entrada = m.transform.parent != null ? m.transform.parent.Find("Entrada") : null;
                    if (entrada != null)
                    {
                        puertas.Add(entrada.position);
                    }
                }
                else if (tiposPersonaje.Contains(tipo))
                {
                    personajes.Add(m.transform.position);
                }
            }
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "DonaLupe" || t.name == "Vendedor" || t.name == "Entrada")
                {
                    personajes.Add(t.position);
                }
            }
            Informe.AppendLine("obstáculos " + obstaculos.Count + ", puertas " + puertas.Count + ", personajes y puntos " + personajes.Count);
        }

        private static bool DebajoDe(Transform t, string nombre)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.name == nombre)
                {
                    return true;
                }
            }
            return false;
        }

        private static string[] PropsDeZona(float x, out float separacion)
        {
            if (x < -LimiteZona)
            {
                separacion = 15f;
                return new[] { "CY06_Expendedora", "CY10_CargadorCalle", "CY05_ServidorCallejero", "CY03_MazoCables" };
            }
            if (x <= LimiteZona)
            {
                separacion = 6f;
                return new[] { "CY02_PantallaVertical", "C16_Tambo", "CY06_Expendedora", "C17_Huacales", "CY10_CargadorCalle", "CY01_ProyectorHolo", "CY05_ServidorCallejero", "CY03_MazoCables" };
            }
            separacion = 6f;
            return new[] { "CY02_PantallaVertical", "CY01_ProyectorHolo", "CY06_Expendedora", "CY10_CargadorCalle", "CY05_ServidorCallejero", "CY03_MazoCables" };
        }

        private static readonly Dictionary<string, int> Turno = new Dictionary<string, int>();

        /// <summary>Camina la orilla de una manzana (tangente t, normal n hacia la calle) y pone props pegados al muro.</summary>
        private static void RecorrerBanqueta(List<Puesto> lista, Vector3 orilla, Vector3 t, Vector3 n)
        {
            float s = -MitadManzana + 3f;
            while (s <= MitadManzana - 3f)
            {
                Vector3 p = orilla + t * s;
                string[] props = PropsDeZona(p.x, out float separacion);
                string zona = p.x < -LimiteZona ? "suburbios" : (p.x <= LimiteZona ? "tianguis" : "neon");
                Turno.TryGetValue(zona, out int turno);
                string codigo = props[turno % props.Length];
                if (IntentarPegadoAlMuro(lista, codigo, p, t, n, 0f, "Calle_" + zona))
                {
                    Turno[zona] = turno + 1;
                    s += separacion;
                }
                else
                {
                    s += 1f;
                }
            }
        }

        private static Vector3 Tamano(string codigo)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArteNoche.Buscar(codigo).RutaPrefab);
            return prefab.GetComponent<BoxCollider>().size;
        }

        private static float GiroHacia(Vector3 frente)
        {
            return Mathf.Atan2(-frente.x, -frente.z) * Mathf.Rad2Deg;
        }

        /// <summary>Busca el muro desde la orilla de la banqueta hacia adentro y, si cabe, pone el modelo con la espalda al muro.</summary>
        private static bool IntentarPegadoAlMuro(List<Puesto> lista, string codigo, Vector3 orilla, Vector3 t, Vector3 n, float altura, string grupo, float separacion = SeparacionFachada, bool sinCollider = false)
        {
            Vector3 desde = orilla + n * 0.5f + Vector3.up * (altura > 0f ? altura : 1.2f);
            if (!Physics.Raycast(desde, -n, out RaycastHit muro, 6f, ~0, QueryTriggerInteraction.Ignore) || Vector3.Dot(muro.normal, n) < 0.9f
                || Mathf.Max(muro.collider.bounds.size.x, muro.collider.bounds.size.z) < 5f)
            {
                return false;
            }
            Vector3 tam = Tamano(codigo);
            float anchoBanqueta = Vector3.Dot(orilla - muro.point, n);
            Vector3 centro;
            if (altura <= 0f)
            {
                // En la banqueta van del lado del cordón, con el frente a la calle: entre el muro y el modelo queda la franja de caminar
                // de los peatones (KitCallePruebas: 38 ± 0.35 m del centro de la manzana, el muro está a 37.44).
                if (PasoPeatones + tam.z > anchoBanqueta - OrillaBanqueta)
                {
                    return false;
                }
                centro = muro.point + n * (anchoBanqueta - OrillaBanqueta - tam.z * 0.5f);
            }
            else
            {
                centro = muro.point + n * (separacion + tam.z * 0.5f);
            }
            float suelo = altura > 0f ? altura : Suelo(centro);
            if (float.IsNaN(suelo))
            {
                return false;
            }
            float giro = GiroHacia(n);
            Bounds caja = CajaGirada(tam, new Vector3(centro.x, suelo, centro.z), giro);
            if (!Libre(caja))
            {
                return false;
            }
            Vector3 pivote = new Vector3(centro.x, suelo, centro.z);
            lista.Add(new Puesto { Grupo = grupo, Codigo = codigo, Posicion = pivote, Giro = giro, SinCollider = sinCollider || altura > 0f });
            obstaculos.Add(caja);
            return true;
        }

        private static float Suelo(Vector3 punto)
        {
            if (Physics.Raycast(punto + Vector3.up * 3f, Vector3.down, out RaycastHit golpe, 4f, ~0, QueryTriggerInteraction.Ignore) && golpe.point.y < 0.5f)
            {
                return golpe.point.y;
            }
            return float.NaN;
        }

        /// <summary>Caja del mundo de un modelo con pivote en la base (tam: ancho, alto, fondo) girado en Y.</summary>
        private static Bounds CajaGirada(Vector3 tam, Vector3 pivote, float giro, Vector3? escala = null)
        {
            Vector3 e = escala ?? Vector3.one;
            Vector3 medio = Vector3.Scale(tam, e) * 0.5f;
            Quaternion q = Quaternion.Euler(0f, giro, 0f);
            Vector3 ejeX = q * new Vector3(medio.x, 0f, 0f);
            Vector3 ejeZ = q * new Vector3(0f, 0f, medio.z);
            Vector3 ext = new Vector3(Mathf.Abs(ejeX.x) + Mathf.Abs(ejeZ.x), medio.y, Mathf.Abs(ejeX.z) + Mathf.Abs(ejeZ.z));
            return new Bounds(pivote + Vector3.up * medio.y, ext * 2f);
        }

        private static string bloqueo;

        private static bool Libre(Bounds caja)
        {
            Bounds holgada = caja;
            holgada.Expand(0.2f);
            foreach (Bounds b in obstaculos)
            {
                if (b.Intersects(holgada))
                {
                    bloqueo = "caja " + b.center + " " + b.size;
                    return false;
                }
            }
            Vector3 c = new Vector3(caja.center.x, 0f, caja.center.z);
            float radio = Mathf.Max(caja.extents.x, caja.extents.z);
            foreach (Vector3 p in puertas)
            {
                if (Vector3.Distance(c, new Vector3(p.x, 0f, p.z)) < RadioPuerta + radio)
                {
                    bloqueo = "puerta " + p;
                    return false;
                }
            }
            foreach (Vector3 p in personajes)
            {
                if (Vector3.Distance(c, new Vector3(p.x, 0f, p.z)) < RadioPersonaje + radio)
                {
                    bloqueo = "personaje " + p;
                    return false;
                }
            }
            return true;
        }

        private static bool IntentarLibre(List<Puesto> lista, string grupo, string codigo, Vector3 pivote, float giro, bool sinCollider = false)
        {
            float suelo = Suelo(pivote);
            if (float.IsNaN(suelo))
            {
                return false;
            }
            Vector3 p = new Vector3(pivote.x, suelo, pivote.z);
            Bounds caja = CajaGirada(Tamano(codigo), p, giro);
            if (!Libre(caja))
            {
                return false;
            }
            lista.Add(new Puesto { Grupo = grupo, Codigo = codigo, Posicion = p, Giro = giro, SinCollider = sinCollider });
            obstaculos.Add(caja);
            return true;
        }

        /// <summary>El Tianguis: tambos y huacales junto a los puestos de T-407 y dos puestos ciber C13 en los huecos de la fila.</summary>
        private static void PonerEnTianguis(List<Puesto> lista)
        {
            Transform puestos = GameObject.Find("GTA_T407/Tianguis/Puestos")?.transform;
            int n = 0;
            if (puestos != null)
            {
                int i = 0;
                foreach (Transform p in puestos)
                {
                    string codigo = i % 2 == 0 ? "C16_Tambo" : "C17_Huacales";
                    foreach (float dx in new[] { -2.6f, 2.8f, -3.4f })
                    {
                        if (IntentarLibre(lista, "Tianguis", codigo, p.position + Vector3.right * dx, Random01(i) * 90f))
                        {
                            n++;
                            break;
                        }
                    }
                    i++;
                }
            }
            // Puestos ciber en los dos huecos grandes de la fila (lado norte x 27.5, lado sur x -26), mirando al centro de la calle.
            foreach ((Vector3 donde, float giro) in new[] { (new Vector3(27.5f, 0f, 4.7f), 0f), (new Vector3(-26f, 0f, -4.7f), 180f) })
            {
                for (float dx = 0f; dx < 6f; dx += 1f)
                {
                    if (IntentarLibre(lista, "Tianguis", "C13_PuestoCiber", donde + Vector3.right * dx, giro)
                        || IntentarLibre(lista, "Tianguis", "C13_PuestoCiber", donde - Vector3.right * dx, giro))
                    {
                        n++;
                        break;
                    }
                }
            }
            Informe.AppendLine("Tianguis: " + n + " tambos, huacales y puestos ciber");
        }

        private static float Random01(int i)
        {
            return ((i * 37) % 10) / 10f;
        }

        /// <summary>C18: dos barreras en cada esquina de las orillas de zona (x = ±47), sobre la banqueta de la calle cruzada.</summary>
        private static void PonerBarreras(List<Puesto> lista)
        {
            int n = 0;
            foreach (float bordeX in new[] { -54f, -40f, 40f, 54f })
            {
                float haciaManzana = Mathf.Abs(bordeX) < LimiteZona ? -Mathf.Sign(bordeX) : Mathf.Sign(bordeX);
                foreach (float lado in new[] { -1f, 1f })
                {
                    for (float z = 11f; z < 20f; z += 0.5f)
                    {
                        Vector3 p = new Vector3(bordeX + haciaManzana * 0.6f, 0f, lado * z);
                        if (IntentarLibre(lista, "Barreras", "C18_BarreraCalle", p, 90f))
                        {
                            n++;
                            break;
                        }
                    }
                }
            }
            Informe.AppendLine("Barreras C18: " + n);
        }

        /// <summary>C19: motos estacionadas junto a la banqueta de la calle principal (una por zona).</summary>
        private static void PonerMotos(List<Puesto> lista)
        {
            int n = 0;
            foreach (Vector3 inicio in new[] { new Vector3(-100f, 0f, -6.3f), new Vector3(-110f, 0f, 6.3f), new Vector3(44f, 0f, -6.3f), new Vector3(80f, 0f, 6.3f), new Vector3(110f, 0f, -6.3f) })
            {
                for (float dx = 0f; dx < 12f; dx += 1f)
                {
                    if (IntentarLibre(lista, "Motos", "C19_Moto", inicio + Vector3.right * dx, inicio.z > 0f ? 90f : 270f))
                    {
                        n++;
                        break;
                    }
                }
            }
            Informe.AppendLine("Motos C19: " + n);
        }

        /// <summary>Modelos de pared (cámaras, plataformas de dron) a una altura fija sobre los muros de la calle principal.</summary>
        private static void PonerEnParedes(List<Puesto> lista, string codigo, float altura, float paso, System.Func<float, bool> dondeX, float separacion)
        {
            int n = 0;
            foreach (Vector2 c in CentrosManzana)
            {
                Vector3 haciaCalle = new Vector3(0f, 0f, -Mathf.Sign(c.y));
                Vector3 orilla = new Vector3(c.x, 0f, c.y) + haciaCalle * MitadManzana;
                for (float s = -MitadManzana + 5f; s <= MitadManzana - 5f; s += 1f)
                {
                    Vector3 p = orilla + Vector3.right * s;
                    if (!dondeX(p.x))
                    {
                        continue;
                    }
                    if (IntentarPegadoAlMuro(lista, codigo, p, Vector3.right, haciaCalle, altura, "Paredes", separacion, true))
                    {
                        n++;
                        s += paso;
                    }
                }
            }
            Informe.AppendLine(codigo + " en paredes: " + n);
        }

        /// <summary>CY04: una antena en la azotea de uno de cada dos edificios del Barrio, lejos de tinacos y aires.</summary>
        private static void PonerAntenas(List<Puesto> lista)
        {
            Transform barrio = GameObject.Find("Barrio").transform;
            Vector3 tam = Tamano("CY04_AntenaAzotea");
            int n = 0;
            int i = 0;
            foreach (Transform edificio in barrio.GetComponentsInChildren<Transform>().Where(t => t.parent != null && t.parent.name == "Edificios").OrderBy(t => t.name))
            {
                i++;
                if (i % 2 != 0)
                {
                    continue;
                }
                Bounds b = ArteT250.Limites(edificio.gameObject);
                foreach (Vector2 f in new[] { new Vector2(0.3f, 0.3f), new Vector2(-0.3f, 0.3f), new Vector2(0.3f, -0.3f), new Vector2(-0.3f, -0.3f) })
                {
                    Vector3 arriba = new Vector3(b.center.x + b.extents.x * f.x, b.max.y + 5f, b.center.z + b.extents.z * f.y);
                    if (!Physics.Raycast(arriba, Vector3.down, out RaycastHit techo, 60f, ~0, QueryTriggerInteraction.Ignore) || techo.point.y < 6f || !techo.collider.transform.IsChildOf(edificio))
                    {
                        continue;
                    }
                    Bounds caja = CajaGirada(tam, techo.point + Vector3.up * 0.3f, 0f);
                    if (!Libre(caja))
                    {
                        continue;
                    }
                    lista.Add(new Puesto { Grupo = "Azoteas", Codigo = "CY04_AntenaAzotea", Posicion = techo.point, Giro = i * 40f, SinCollider = true });
                    obstaculos.Add(caja);
                    n++;
                    break;
                }
            }
            Informe.AppendLine("Antenas CY04: " + n);
        }

        /// <summary>E03: pantalla gigante sobre la azotea del edificio de Neón Alto más cercano a (100, 0, 12), mirando al suroeste.</summary>
        private static void PonerPantallaGigante(List<Puesto> lista)
        {
            Transform barrio = GameObject.Find("Barrio").transform;
            Transform mejor = barrio.GetComponentsInChildren<Transform>()
                .Where(t => t.parent != null && t.parent.name == "Edificios" && t.parent.parent.name == "Manzana_3")
                .OrderBy(t => Vector2.Distance(new Vector2(t.position.x, ArteT250.Limites(t.gameObject).min.z), new Vector2(100f, 12f)))
                .FirstOrDefault();
            if (mejor == null)
            {
                Informe.AppendLine("FALTA edificio para E03");
                return;
            }
            Bounds b = ArteT250.Limites(mejor.gameObject);
            Vector3 arriba = new Vector3(b.center.x, b.max.y + 5f, b.min.z + 6f);
            if (Physics.Raycast(arriba, Vector3.down, out RaycastHit techo, 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                lista.Add(new Puesto { Grupo = "NeonAlto", Codigo = "E03_PantallaGigante", Posicion = techo.point, Giro = 30f, SinCollider = true });
                Informe.AppendLine("E03 sobre " + mejor.name + " en " + techo.point);
            }
        }

        /// <summary>
        /// CY09: puesto de implantes (un carrito) en la calle, en la boca del callejón de El Mercado, donde está I+D+I; el callejón es
        /// angosto y ya tiene los puestos de T-250, y en la banqueta no cabe sin tapar la franja de los peatones.
        /// </summary>
        private static void PonerImplantes(List<Puesto> lista)
        {
            for (float dx = 0f; dx < 5f; dx += 0.5f)
            {
                foreach (float signo in new[] { 1f, -1f })
                {
                    if (IntentarLibre(lista, "Mercado", "CY09_PuestoImplantes", new Vector3(-22f + signo * dx, 0f, 4.7f), 0f))
                    {
                        Informe.AppendLine("CY09 en la boca del callejón de El Mercado, x " + (-22f + signo * dx));
                        return;
                    }
                }
            }
            Informe.AppendLine("CY09: sin lugar libre en la boca del callejón de El Mercado (último bloqueo: " + bloqueo + ")");
        }

        // ------------------------------------------------------------------ Barrio.prefab

        private static void ColocarBarrio(List<Puesto> calle)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(ColocarT250.RutaBarrio);
            Informe.AppendLine("## " + ColocarT250.RutaBarrio);
            CambiarFachadas(raiz);

            Transform grupo = new GameObject(NombreGrupo).transform;
            grupo.SetParent(raiz.transform, false);
            foreach (Puesto p in calle)
            {
                Transform sub = grupo.Find(p.Grupo) ?? NuevoHijo(grupo, p.Grupo);
                Poner(sub, p);
            }
            Informe.AppendLine("calle: " + calle.Count + " modelos");
            foreach (var g in calle.GroupBy(p => p.Codigo).OrderBy(g => g.Key))
            {
                Informe.AppendLine("  " + g.Key + "\t" + g.Count());
            }

            PonerEntradas(raiz);
            PonerCasa(raiz);
            PrefabUtility.SaveAsPrefabAsset(raiz, ColocarT250.RutaBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        private static Transform NuevoHijo(Transform padre, string nombre)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            return t;
        }

        private static GameObject Poner(Transform padre, Puesto p)
        {
            ArteT250.Modelo modelo = ArteNoche.Buscar(p.Codigo);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelo.RutaPrefab);
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            instancia.name = p.Codigo + SufijoNoche;
            if (p.SinCollider)
            {
                Object.DestroyImmediate(instancia.GetComponent<BoxCollider>());
            }
            if (p.SinSombra)
            {
                foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            instancia.transform.localScale = p.Escala;
            instancia.transform.SetPositionAndRotation(p.Posicion, Quaternion.Euler(0f, p.Giro, 0f));
            return instancia;
        }

        /// <summary>Cada fachada C01/C02/C03 de T-250 recibe al lado su versión _cy con la misma medida; la de T-250 queda inactiva.</summary>
        private static void CambiarFachadas(GameObject raiz)
        {
            int n = 0;
            foreach (GameObject vieja in raiz.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject)
                .Where(g => PrefabUtility.IsAnyPrefabInstanceRoot(g)).ToList())
            {
                string ruta = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(vieja);
                if (!ruta.EndsWith(".prefab"))
                {
                    continue;
                }
                string codigo = Path.GetFileNameWithoutExtension(ruta);
                if (codigo != "C01_FachadaLocal" && codigo != "C02_FachadaVivienda" && codigo != "C03_FachadaPisoAlto")
                {
                    continue;
                }
                Vector3 viejo = ArteT250.Buscar(codigo) != null ? Tamano(codigo) : Vector3.one;
                Vector3 nuevo = Tamano(codigo + "_cy");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArteNoche.Buscar(codigo + "_cy").RutaPrefab);
                GameObject cy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, vieja.transform.parent);
                cy.name = codigo + "_cy";
                if (vieja.GetComponent<BoxCollider>() == null)
                {
                    Object.DestroyImmediate(cy.GetComponent<BoxCollider>());
                }
                cy.transform.SetSiblingIndex(vieja.transform.GetSiblingIndex());
                cy.transform.localPosition = vieja.transform.localPosition;
                cy.transform.localRotation = vieja.transform.localRotation;
                Vector3 e = vieja.transform.localScale;
                cy.transform.localScale = new Vector3(e.x * viejo.x / nuevo.x, e.y * viejo.y / nuevo.y, e.z * viejo.z / nuevo.z);
                foreach (Renderer r in cy.GetComponentsInChildren<Renderer>(true))
                {
                    Renderer igual = vieja.GetComponentInChildren<Renderer>(true);
                    if (igual != null)
                    {
                        r.shadowCastingMode = igual.shadowCastingMode;
                    }
                }
                vieja.SetActive(false);
                n++;
            }
            Informe.AppendLine("fachadas _cy: " + n + " (las de T-250 inactivas)");
        }

        /// <summary>E01 en la puerta de VivaCoin, E02 en la de El Mercado, A11a en la del clandestino: sin collider y aplanados contra el muro.</summary>
        private static void PonerEntradas(GameObject raiz)
        {
            Transform grupo = raiz.transform.Find(NombreGrupo + "/Entradas") ?? NuevoHijo(raiz.transform.Find(NombreGrupo), "Entradas");
            PonerEnPuerta(raiz, grupo, "Manzana_3/Lugar_Casino", "E01_EntradaCasino", 1.0f, 1f);
            PonerEnPuerta(raiz, grupo, "Manzana_2/Lugar_MercadoNegro", "E02_PortonMercadoNegro", 0.8f, 0.95f);
            PonerEnPuerta(raiz, grupo, "Manzana_2/Lugar_Clandestino", "A11a_PortalSotano", 0.6f, 3.8f / 3.6f);
        }

        private static void PonerEnPuerta(GameObject raiz, Transform grupo, string lugar, string codigo, float fondo, float escala)
        {
            Transform l = raiz.transform.Find(lugar);
            Transform puerta = l != null ? l.Find("Puerta") : null;
            Transform entrada = l != null ? l.Find("Entrada") : null;
            if (puerta == null || entrada == null)
            {
                Informe.AppendLine("FALTA puerta de " + lugar);
                return;
            }
            Vector3 n = entrada.position - puerta.position;
            n.y = 0f;
            n.Normalize();
            Transform fachada = l.Find("FachadaIzquierda");
            float caraMuro = fachada != null ? Vector3.Dot(fachada.position - puerta.position, n) + 0.5f : 0.15f;
            Vector3 tam = Tamano(codigo);
            Vector3 e = new Vector3(escala, escala, Mathf.Min(escala, fondo / tam.z));
            Vector3 pos = puerta.position + n * (caraMuro + tam.z * e.z * 0.5f);
            Puesto p = new Puesto { Codigo = codigo, Posicion = new Vector3(pos.x, puerta.position.y, pos.z), Giro = GiroHacia(n), Escala = e, SinCollider = true };
            Poner(grupo, p);
            Informe.AppendLine("entrada\t" + codigo + "\t" + lugar + "\t" + p.Posicion);
        }

        // ------------------------------------------------------------------ casa y taller

        private static void PonerCasa(GameObject raiz)
        {
            Transform interior = raiz.transform.Find("Manzana_4/Lugar_Casa/Interior");
            Transform sala = interior.Find("Sala");
            Transform taller = interior.Find("Taller");

            // Buró en el lugar de la mesita (el cubo greybox junto a la cama).
            Transform mesita = sala.Find("Prop_Mesita");
            if (mesita != null)
            {
                Bounds b = LimitesVisibles(mesita);
                GameObject buro = Poner(mesita, new Puesto { Codigo = "H11_Buro", Posicion = new Vector3(b.center.x, b.min.y, b.center.z), Giro = 270f, Escala = Vector3.one * (0.7f / Tamano("H11_Buro").y), SinCollider = true });
                buro.name = "H11_Buro" + SufijoNoche;
                ApagarVisual(mesita, buro.transform);
            }
            // Estufa y tarja en el lugar de la cocineta (dos piezas a lo largo, mirando a la sala).
            Transform cocineta = sala.Find("Cocineta");
            if (cocineta != null)
            {
                Bounds b = LimitesVisibles(cocineta);
                Vector3 tam = Tamano("H08_EstufaTarja");
                float alto = b.size.y / tam.y;
                float ancho = b.size.x / 2f / tam.x;
                float fondo = Mathf.Min(alto, b.size.z / tam.z);
                for (int i = 0; i < 2; i++)
                {
                    Vector3 pos = new Vector3(b.min.x + b.size.x * (0.25f + 0.5f * i), b.min.y, b.center.z);
                    Poner(sala, new Puesto { Codigo = "H08_EstufaTarja", Posicion = pos, Giro = 180f, Escala = new Vector3(ancho, alto, fondo), SinCollider = true });
                }
                ApagarVisual(cocineta, null);
            }
            // Sofá contra el muro del frente (lado sur de la entrada) mirando al fondo; tele enfrente; repisa de veladoras en el fondo.
            // Sofá (al 80 %) en la esquina del frente junto a la cocineta: libre la línea cama → puerta; la tele lo mira desde el lado del espejo.
            Poner(sala, new Puesto { Codigo = "H07_Sofa", Posicion = new Vector3(-61.7f, 0.2f, -81.9f), Giro = 90f, Escala = Vector3.one * 0.8f });
            Poner(sala, new Puesto { Codigo = "H06_TeleVieja", Posicion = new Vector3(-66.0f, 0.2f, -79.6f), Giro = GiroHacia(new Vector3(4.3f, 0f, -2.3f)) });
            Poner(sala, new Puesto { Codigo = "H09_RepisaVeladoras", Posicion = new Vector3(-68.0f, 1.3f, -80.4f), Giro = 0f, Escala = Vector3.one * 0.7f, SinCollider = true });

            // Taller: repisa en el lugar de los estantes, soldadora junto al banco y grúa de motor en la orilla de la sala.
            Transform estante = taller.Find("Estante1");
            if (estante != null)
            {
                Bounds b = LimitesVisibles(taller.Find("Estante0"));
                b.Encapsulate(LimitesVisibles(estante));
                b.Encapsulate(LimitesVisibles(taller.Find("Estante2")));
                Vector3 tam = Tamano("T08_RepisaTaller");
                Poner(taller, new Puesto { Codigo = "T08_RepisaTaller", Posicion = new Vector3(b.center.x, 0.2f, -70.6f - 0.25f), Giro = 0f, Escala = new Vector3(b.size.x / tam.x, 2.45f / tam.y, 0.5f / tam.z), SinCollider = true });
                foreach (string nombre in new[] { "Estante0", "Estante1", "Estante2", "PiezasIzq", "PiezasDer" })
                {
                    Transform t = taller.Find(nombre);
                    if (t != null)
                    {
                        ApagarVisual(t, null);
                    }
                }
            }
            Poner(taller, new Puesto { Codigo = "T06_Soldadora", Posicion = new Vector3(-68.0f, 0.2f, -76.15f), Giro = 270f });
            // La grúa va en el piso libre del taller, al sur de la estación del exo, fuera de la línea de la puerta al punto de interacción y al espejo.
            Poner(taller, new Puesto { Codigo = "T07_GruaMotor", Posicion = new Vector3(-65.6f, 0.2f, -76.6f), Giro = 270f });
            Informe.AppendLine("casa: H06 H07 H08x2 H09 H11 (mesita y cocineta apagadas); taller: T06 T07 T08 (estantes apagados)");
        }

        private static Bounds LimitesVisibles(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && !r.name.Contains(SufijoNoche)).ToArray();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        /// <summary>Apaga los MeshRenderer viejos (si llevan collider quedan en solo sombra, como en T-250); el visual nuevo no se toca.</summary>
        private static void ApagarVisual(Transform t, Transform nuevo)
        {
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (nuevo != null && r.transform.IsChildOf(nuevo))
                {
                    continue;
                }
                if (r.GetComponent<Collider>() != null)
                {
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                else
                {
                    r.enabled = false;
                }
            }
        }

        // ------------------------------------------------------------------ jaula y dron

        private static void ColocarJaula()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(ColocarT250.RutaJaula);
            Transform grupo = NuevoHijo(raiz.transform, NombreGrupo);
            // Dos marcadores sobre las esquinas norte de la reja, a los lados de la pantalla, mirando al centro.
            foreach (float x in new[] { -12.5f, 12.5f })
            {
                Poner(grupo, new Puesto { Codigo = "A08_Marcador", Posicion = new Vector3(x, 6.6f, 12.5f), Giro = GiroHacia(new Vector3(-x, 0f, -12.5f)), Escala = Vector3.one * (1.8f / Tamano("A08_Marcador").y), SinCollider = true });
            }
            // Escalera de bajada al sótano en la esquina suroeste, entre las gradas y el muro, con los escalones hacia el centro.
            Poner(grupo, new Puesto { Codigo = "A11b_EscaleraSotano", Posicion = new Vector3(-19.5f, 0f, -19.5f), Giro = GiroHacia(new Vector3(1f, 0f, 1f)), SinCollider = true });
            PrefabUtility.SaveAsPrefabAsset(raiz, ColocarT250.RutaJaula);
            PrefabUtility.UnloadPrefabContents(raiz);
            Informe.AppendLine("## JaulaClandestino: A08 sobre la reja norte, A11b en la esquina suroeste del sótano");
        }

        private static void ColocarDron()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaDron);
            Bounds viejo = LimitesVisibles(raiz.transform);
            Vector3 tam = Tamano("CY07b_Dron");
            Transform visual = NuevoHijo(raiz.transform, "Visual" + SufijoNoche);
            visual.SetSiblingIndex(0);
            Puesto p = new Puesto { Codigo = "CY07b_Dron", Escala = Vector3.one * (Mathf.Max(viejo.size.x, viejo.size.z) / tam.x), SinCollider = true };
            GameObject dron = Poner(visual, p);
            dron.transform.localPosition = new Vector3(0f, -tam.y * p.Escala.y * 0.5f, 0f);
            dron.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.transform.IsChildOf(visual) && r.name != "Foco" && (r is MeshRenderer))
                {
                    r.enabled = false;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaDron);
            PrefabUtility.UnloadPrefabContents(raiz);
            Informe.AppendLine("## Dron: CY07b de " + (tam.x * p.Escala.x).ToString("F2") + " m, Cuerpo, Rotor y Lente apagados (Foco sigue)");
        }

        // ------------------------------------------------------------------ capturas de revisión (editor, sin el juego)

        private static readonly Dictionary<string, (Vector3 desde, Vector3 hacia, bool orto)> Vistas = new Dictionary<string, (Vector3, Vector3, bool)>
        {
            { "casa-plano", (new Vector3(-64.8f, 5.5f, -77f), new Vector3(-64.8f, 0f, -77.01f), true) },
            { "casa", (new Vector3(-61.6f, 2.6f, -76.5f), new Vector3(-66f, 0.8f, -81f), false) },
            { "taller", (new Vector3(-62f, 2.6f, -78f), new Vector3(-65f, 1f, -72f), false) },
            { "clandestino", (new Vector3(0f, 2.5f, 0f), new Vector3(0f, 2f, 10f), false) },
            { "vivacoin", (new Vector3(47f, 2.5f, 40f), new Vector3(57f, 2.5f, 47f), false) },
            { "mercado", (new Vector3(-20f, 2.5f, 20.5f), new Vector3(-25f, 2f, 26f), false) },
            { "implantes", (new Vector3(-20f, 2.5f, 11.5f), new Vector3(-24f, 1f, 17f), false) },
            { "tianguis-calle", (new Vector3(36f, 2f, 0.5f), new Vector3(0f, 1.2f, 0f), false) },
            { "neon-calle", (new Vector3(58f, 2f, -3f), new Vector3(100f, 2.5f, 4f), false) },
            { "pantalla", (new Vector3(72f, 2f, -3f), new Vector3(99.5f, 30f, 15.6f), false) },
            { "suburbios-calle", (new Vector3(-58f, 2f, 2f), new Vector3(-110f, 1.2f, 0f), false) },
            { "barreras", (new Vector3(-47f, 3f, 2f), new Vector3(-40f, 0.8f, 13f), false) },
            { "azoteas", (new Vector3(-40f, 45f, -20f), new Vector3(-94f, 15f, 47f), false) },
            { "tianguis-alto", (new Vector3(0f, 25f, -25f), new Vector3(0f, 0f, 0f), false) },
            { "neon-alto", (new Vector3(90f, 30f, -30f), new Vector3(90f, 5f, 5f), false) },
            { "suburbios-alto", (new Vector3(-95f, 25f, -25f), new Vector3(-95f, 0f, 0f), false) },
            { "jaula", (new Vector3(410f, 9f, -10f), new Vector3(400f, 3f, 5f), false) },
            { "jaula-esquina", (new Vector3(388f, 4f, -10f), new Vector3(381f, 1f, -19f), false) },
            { "dron", (new Vector3(203f, 2f, -3f), new Vector3(200f, 1.5f, 0f), false) },
        };

        /// <summary>Batchmode: capturas de revisión en docs/capturas/noche-revision-&lt;vista&gt;.png con luz plana (no son las de noche del juego).</summary>
        [MenuItem("Mecha Cyberpunk/T-419/Capturas de revisión")]
        public static void CapturarRevision()
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ColocarT250.RutaBarrio), escena);
            GameObject jaula = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ColocarT250.RutaJaula), escena);
            jaula.transform.position = new Vector3(400f, 0f, 0f);
            GameObject dron = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron), escena);
            dron.transform.position = new Vector3(200f, 1.5f, 0f);
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.transform.position = new Vector3(200f, 0f, 0f);
            ArteT250.PonerLuz();
            RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.75f);
            Camera camara = new GameObject("Camara").AddComponent<Camera>();
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = 500f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            foreach (var v in Vistas)
            {
                camara.orthographic = v.Value.orto;
                camara.orthographicSize = 7.5f;
                camara.fieldOfView = 65f;
                camara.transform.position = v.Value.desde;
                camara.transform.LookAt(v.Value.hacia);
                ArteT250.Guardar(camara, Path.Combine(ArteT250.CarpetaCapturas, "noche-revision-" + v.Key + ".png"));
            }
        }
    }
}

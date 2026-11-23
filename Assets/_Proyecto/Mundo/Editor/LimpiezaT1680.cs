using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MechaCyberpunk.Mundo.Lugares;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T37 (T-1680): limpieza de Ciudad_Prologo: fuera primitivos y cosas inventadas, nada flotando, nada afuera del muro y el casino VivaCoin
    /// sin edificios adentro ni pasillo.
    /// Batchmode (Unity cerrado), cada paso en su corrida:
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.LimpiezaT1680.Diagnostico  -> Logs/limpieza-T1680.txt (no cambia nada)
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.LimpiezaT1680.Aplicar      -> cambia, guarda y escribe Logs/limpieza-T1680-aplicar.txt
    ///   -executeMethod MechaCyberpunk.Mundo.Editor.LimpiezaT1680.Fotos        -> docs/capturas/_tareas/T37/*_&lt;sufijo&gt;.png (T1680_SUF=antes|despues)
    /// Nada se borra: lo que sobra se apaga (SetActive false o el renderer apagado, que deja el collider), así es fácil de revisar y de deshacer.
    /// </summary>
    public static class LimpiezaT1680
    {
        public const string RutaLog = "Logs/limpieza-T1680.txt";
        public const string RutaAplicar = "Logs/limpieza-T1680-aplicar.txt";
        public const string RutaVolcado = "Logs/limpieza-T1680-volcado.tsv";
        public const string CarpetaFotos = "docs/capturas/_tareas/T37";
        public const string RaizFachada = "T1680_FachadaCasino";
        public const string Etiqueta = "[T-1680] ";

        public const string RutaLugarCasino = "Barrio/Manzana_3/Lugar_Casino";
        private const string PrefabModulo = "Assets/Arte/Calle/Prefabs/NV01_FachadaOficinas.prefab";

        /// <summary>Huella del edificio del casino (x 57..89, z 31..63, medida de su cascarón).</summary>
        public static readonly Rect HuellaCasino = Rect.MinMaxRect(57f, 31f, 89f, 63f);
        /// <summary>Plano de la fachada a la calle (x) y nuevo frente interior (x) al quitar el pasillo.</summary>
        public const float XFachadaCasino = 57f;
        public const float XFrenteInterior = 58.05f;
        private const float XFrenteViejo = 61.1f;
        private const float ZPuertaMin = 45.5f;
        private const float ZPuertaMax = 48.5f;
        private const int PisosCasino = 3;

        /// <summary>Hueco mínimo (m) bajo un objeto para contarlo como flotante.</summary>
        public const float HuecoFlotante = 0.5f;
        private const float MargenContacto = 0.25f;

        public enum Cat
        {
            Bar,
            RellenoMalPuesto,
            Primitiva,
            Flotante,
            Afuera,
            Casino
        }

        public class Hallazgo
        {
            public Cat Cat;
            public GameObject Objeto;
            public string Ruta;
            public Vector3 Pos;
            public Vector3 Tam;
            public string Nota;
            /// <summary>true: SetActive(false); false: solo apagar los renderers (los colliders se quedan).</summary>
            public bool ApagarTodo;
        }

        // Rutas (prefijo) que son interiores cerrados: sus piezas solo se ven adentro y esperan su propia tarea de arte.
        private static readonly string[] Interiores =
        {
            "T546_Interiores/", "S2_Interiores/", "T931_Armero/", "T531_Locales/", "T930_Casino/", "S2o_Casino/", "S2n_TallerMecanico/Forro",
            "S2n_TallerMecanico/Muebles", "Barrio/Manzana_2/Lugar_MercadoNegro/Interior", "Barrio/Manzana_2/Lugar_Clandestino/Interior",
            "Barrio/Manzana_5/Lugar_Bar/Interior", "Barrio/Manzana_3/Lugar_Casino/Interior", "Barrio/Manzana_4/Lugar_Casa/Interior"
        };

        private static readonly Regex NombreLuz = new Regex("neon|luz|foco|led|brillo|halo|pantalla|aviso|lampara|tira|friso|cajon|faro|semaforo|reflejo",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NombreDecal = new Regex("letrero|charco|linea|raya|cebra|grieta|mancha|ventana|interior|vitrina|cristal|alfombra|brasa|vapor|humo|niebla|agua|azotea|sombra|piso|banqueta|bordillo|asfalto|calle",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NoFlota = new Regex(
            "cable|luz|letrero|neon|halo|foco|lampara|farol|anuncio|holo|nube|estrella|cielo|avion|dron|nave|aere|globo|pantalla|semaforo|toldo|bandera|poste|antena|reflector|camara|cy0|ne0|l1[0-9]|c15|c23|n01|n09|ventana|interior|balcon|barandal|macet|franja|pretil|cornisa|tendedero|puente|pasarela|autopista|muro|mu0|torre|pared|fachada|marquesina|aviso|medidor|c14|registro|aire|c10|tuberia|tubo|escalera|lona|manta|grafiti|cartel",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ScriptMovil = new Regex("Ruta|Dron|Avion|Vuelo|Giro|Trafico|Peaton|Patrulla|Vehiculo|Carro|Npc|Civil|Animator|Parpadeo|Pantalla",
            RegexOptions.Compiled);

        // ================================================================== entradas

        [MenuItem("Mecha Cyberpunk/T-1680 Limpieza: diagnostico")]
        public static void Diagnostico()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Physics.SyncTransforms();
            List<Hallazgo> lista = Buscar(escena);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1680 (T37) diagnostico de limpieza, " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            Rect linea = FueraDelMuroT1543.LineaDelMuro();
            sb.AppendLine("Linea del muro: x " + linea.xMin + ".." + linea.xMax + ", z " + linea.yMin + ".." + linea.yMax + ", grosor " + FueraDelMuroT1543.GrosorDelMuro().ToString("F1"));
            Escribir(sb, lista);
            sb.AppendLine();
            sb.AppendLine("=== Primitivas en lista blanca (por motivo) ===");
            foreach (var g in PrimitivasPermitidas(escena).GroupBy(p => p.Value).OrderByDescending(g => g.Count()))
            {
                sb.AppendLine("  " + g.Count() + "  " + g.Key + "   ej.: " + string.Join(" | ", g.Take(3).Select(p => p.Key)));
            }
            sb.AppendLine();
            sb.AppendLine("=== Lisos: renderers con material sin textura y claro (2-60 m de alto) ===");
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || r is ParticleSystemRenderer || r is LineRenderer || r is SkinnedMeshRenderer || r.sharedMaterial == null)
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (b.size.y < 2f || b.size.y > 60f)
                {
                    continue;
                }
                Material m = r.sharedMaterial;
                Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : (m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null);
                Color col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : (m.HasProperty("_Color") ? m.GetColor("_Color") : Color.black);
                if (tex == null && col.grayscale > 0.55f)
                {
                    sb.AppendLine("  " + LimpiezaRuta(r.transform) + "	" + m.name + " " + m.shader.name + "	pos " + V(b.center) + "	tam " + V(b.size));
                }
            }
            sb.AppendLine();
            sb.AppendLine("=== Imagenes de canvas en el mundo (activas) ===");
            foreach (CanvasRenderer g in Object.FindObjectsByType<CanvasRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Canvas c = g.GetComponentInParent<Canvas>();
                RectTransform rt = g.transform as RectTransform;
                if (c == null || rt == null || c.renderMode != RenderMode.WorldSpace)
                {
                    continue;
                }
                Vector3[] esq = new Vector3[4];
                rt.GetWorldCorners(esq);
                Bounds b = new Bounds(esq[0], Vector3.zero);
                foreach (Vector3 e in esq)
                {
                    b.Encapsulate(e);
                }
                string comps = string.Join(",", g.GetComponents<Component>().Where(k => k != null && !(k is Transform) && !(k is CanvasRenderer)).Select(k => k.GetType().Name));
                sb.AppendLine("  " + LimpiezaRuta(g.transform) + "	" + comps + "	pos " + V(b.center) + "	tam " + V(b.size));
            }
            sb.AppendLine();
            sb.AppendLine("=== Jerarquia del casino ===");
            GameObject casino = GameObject.Find(RutaLugarCasino);
            if (casino != null)
            {
                Jerarquia(sb, casino.transform, 0);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, sb.ToString());
            Debug.Log(Etiqueta + "diagnostico: " + lista.Count + " hallazgos; ver " + RutaLog);
        }

        [MenuItem("Mecha Cyberpunk/T-1680 Limpieza: aplicar")]
        public static void Aplicar()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Physics.SyncTransforms();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1680 (T37) aplicar, " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            List<Hallazgo> lista = Buscar(escena);
            int apagados = 0;
            int sinRender = 0;
            foreach (Hallazgo h in lista)
            {
                if (h.Objeto == null)
                {
                    continue;
                }
                if (h.ApagarTodo)
                {
                    if (h.Objeto.activeSelf)
                    {
                        Undo.RecordObject(h.Objeto, "T1680");
                        h.Objeto.SetActive(false);
                        Grabar(h.Objeto);
                        apagados++;
                    }
                }
                else
                {
                    sinRender += ApagarRenderers(h.Objeto);
                }
            }
            Escribir(sb, lista);
            sb.AppendLine();
            sb.AppendLine("Apagados (SetActive false): " + apagados + "; renderers apagados (collider se queda): " + sinRender);

            Physics.SyncTransforms();
            sb.AppendLine();
            sb.AppendLine("=== Casino ===");
            ArreglarCasino(sb);
            sb.AppendLine();
            sb.AppendLine("=== Asfalto sin parches ===");
            sb.AppendLine(AsfaltoSinParches());

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);

            Physics.SyncTransforms();
            List<Hallazgo> despues = Buscar(escena);
            sb.AppendLine();
            sb.AppendLine("=== Despues ===");
            foreach (var g in despues.GroupBy(h => h.Cat))
            {
                sb.AppendLine("  " + g.Key + ": " + g.Count());
            }
            sb.AppendLine("Primitivas visibles fuera de la lista blanca: " + PrimitivasVisibles(escena).Count);
            sb.AppendLine("Renderers afuera del muro: " + RenderersAfuera(escena).Count);
            File.WriteAllText(RutaAplicar, sb.ToString());
            Debug.Log(Etiqueta + "aplicado: " + apagados + " apagados, " + sinRender + " renderers; quedan " + despues.Count + " hallazgos; ver " + RutaAplicar);
        }

        // ================================================================== busqueda

        public static List<Hallazgo> Buscar(Scene escena)
        {
            List<Hallazgo> lista = new List<Hallazgo>();
            HashSet<GameObject> ya = new HashSet<GameObject>();
            void Agregar(Hallazgo h)
            {
                if (h.Objeto != null && ya.Add(h.Objeto))
                {
                    lista.Add(h);
                }
            }

            // 1. El bar improvisado de la calle del Doc (barra, parrilla, sillas, taburetes y bancos hechos de cubos).
            GameObject props = GameObject.Find("T1102_CalleDoc/Props");
            if (props != null)
            {
                foreach (Transform t in props.transform)
                {
                    if (t.gameObject.activeSelf && Regex.IsMatch(t.name, "^(Taqueria|Silla|Taburete|Banco)"))
                    {
                        Agregar(Nuevo(Cat.Bar, t.gameObject, true, "bar improvisado de cubos"));
                    }
                }
            }

            // 2. Edificios de relleno (T-1543) puestos a media calle, dentro del casino o encima de algo de juego.
            GameObject relleno = GameObject.Find(FueraDelMuroT1543.RaizRelleno);
            if (relleno != null)
            {
                List<Bounds> calles = Calles(escena);
                foreach (Transform lote in relleno.transform)
                {
                    foreach (Transform edificio in lote)
                    {
                        if (!edificio.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        Bounds b = Caja(edificio.gameObject);
                        string motivo = MotivoRelleno(b, calles, edificio);
                        if (motivo != null)
                        {
                            Agregar(Nuevo(Cat.RellenoMalPuesto, edificio.gameObject, true, motivo));
                        }
                    }
                }
            }

            // 3. Primitivas visibles fuera de la lista blanca.
            foreach (Renderer r in PrimitivasVisibles(escena))
            {
                if (LimpiezaRuta(r.transform).StartsWith(RutaLugarCasino + "/"))
                {
                    continue; // el casino se arregla aparte (ArreglarCasino): sus colliders se quedan
                }
                Agregar(Nuevo(Cat.Primitiva, r.gameObject, SoloVisual(r.gameObject), Malla(r) + " / " + (r.sharedMaterial != null ? r.sharedMaterial.name : "-")));
            }

            // 4. Objetos sin apoyo (flotando en techos o en el aire).
            foreach (Hallazgo h in Flotantes(escena))
            {
                Agregar(h);
            }

            // 4b. Tinacos inventados en las azoteas del Barrio: plataforma y patas de cubos.
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.name == "TinacoPobre")
                {
                    Agregar(Nuevo(Cat.Flotante, t.gameObject, true, "tinaco inventado en azotea, sobre plataforma de cubos"));
                }
            }

            // 5. Afuera del muro.
            foreach (Renderer r in RenderersAfuera(escena))
            {
                Agregar(Nuevo(Cat.Afuera, r.gameObject, SoloVisual(r.gameObject) || r.name == "Visual", "centro afuera del muro"));
            }

            // 6. Casino: lo que sobra arriba del cascarón (tinaco del techo a 29 m).
            GameObject tinaco = GameObject.Find(RutaLugarCasino + "/Azotea_AC_Tinaco");
            if (tinaco != null && tinaco.activeSelf)
            {
                Agregar(Nuevo(Cat.Casino, tinaco, true, "tinaco sobre el cascaron de 28 m (el casino queda de 3 pisos)"));
            }
            return lista;
        }

        private static Hallazgo Nuevo(Cat cat, GameObject go, bool todo, string nota)
        {
            Bounds b = Caja(go);
            return new Hallazgo { Cat = cat, Objeto = go, Ruta = LimpiezaRuta(go.transform), Pos = b.center, Tam = b.size, Nota = nota, ApagarTodo = todo };
        }

        /// <summary>Pisos de calle (asfalto) como cajas XZ: los arroyos S2c y las franjas entre manzanas de la rejilla del Barrio.</summary>
        public static List<Bounds> Calles(Scene escena)
        {
            List<Bounds> calles = new List<Bounds>();
            List<Bounds> manzanas = new List<Bounds>();
            Bounds? asfaltoGlobal = null;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled || r.sharedMaterial == null)
                    {
                        continue;
                    }
                    string m = r.sharedMaterial.name.ToLowerInvariant();
                    Bounds b = r.bounds;
                    if (m.Contains("asfalto") && b.size.y < 1.1f)
                    {
                        if (b.size.x > 200f)
                        {
                            asfaltoGlobal = b;
                        }
                        else
                        {
                            calles.Add(b);
                        }
                    }
                    if (r.name == "Banqueta" && b.size.x > 60f && b.size.z > 60f)
                    {
                        manzanas.Add(b);
                    }
                }
            }
            if (asfaltoGlobal.HasValue)
            {
                // Las franjas de calle de la rejilla: lo que no cae en ninguna manzana. Se muestrea en celdas de 2 m.
                Bounds g = asfaltoGlobal.Value;
                for (float x = g.min.x + 1f; x < g.max.x; x += 2f)
                {
                    for (float z = g.min.z + 1f; z < g.max.z; z += 2f)
                    {
                        Vector3 p = new Vector3(x, 0f, z);
                        if (!manzanas.Any(mz => DentroXZ(mz, p, 0f)))
                        {
                            calles.Add(new Bounds(new Vector3(x, 0.2f, z), new Vector3(2f, 0.2f, 2f)));
                        }
                    }
                }
            }
            return calles;
        }

        private static string MotivoRelleno(Bounds b, List<Bounds> calles, Transform edificio)
        {
            if (DentroXZ(new Bounds(new Vector3(HuellaCasino.center.x, 0f, HuellaCasino.center.y), new Vector3(HuellaCasino.width, 1f, HuellaCasino.height)), b.center, 0f))
            {
                return "dentro del casino VivaCoin";
            }
            foreach (Bounds c in calles)
            {
                if (DentroXZ(c, b.center, 0.5f))
                {
                    return "a media calle (piso de calle " + LimpiezaT1680.V(c.center) + ")";
                }
            }
            // Encima de algo de juego (lugares, puertas, mostradores, marcadores).
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (mb == null || mb.transform.IsChildOf(edificio))
                {
                    continue;
                }
                string ns = mb.GetType().Namespace ?? "";
                string tipo = mb.GetType().Name;
                if (!ns.StartsWith("MechaCyberpunk") || !Regex.IsMatch(tipo, "Lugar|Puerta|Mostrador|Entrada|Marcador|Zona|Punto|Mision"))
                {
                    continue;
                }
                Vector3 p = mb.transform.position;
                if (DentroXZ(b, p, -0.5f) && p.y < b.max.y)
                {
                    return "encima de " + tipo + " (" + LimpiezaRuta(mb.transform) + ")";
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ primitivas

        /// <summary>Malla de Unity (Cube, Quad, Plane, Cylinder, Sphere, Capsule) o de ProBuilder.</summary>
        public static bool EsPrimitiva(Renderer r)
        {
            if (!(r is MeshRenderer))
            {
                return false;
            }
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                return false;
            }
            string ruta = AssetDatabase.GetAssetPath(mf.sharedMesh);
            bool deUnity = ruta.Contains("unity default resources") || ruta.Contains("unity_builtin_extra");
            bool probuilder = mf.sharedMesh.name.StartsWith("pb_Mesh") || r.GetComponents<MonoBehaviour>().Any(c => c != null && c.GetType().Name == "ProBuilderMesh");
            return deUnity || probuilder;
        }

        /// <summary>null si la primitiva cuenta (hay que quitarla); si no, el motivo por el que se queda (lista blanca).</summary>
        public static string MotivoPermitida(Renderer r)
        {
            string ruta = LimpiezaRuta(r.transform);
            foreach (string pre in Interiores)
            {
                if (ruta.StartsWith(pre))
                {
                    return "interior cerrado (" + pre.TrimEnd('/') + "): solo se ve adentro";
                }
            }
            if (Regex.IsMatch(ruta, "(^|/)(Realismo_T1605/Cielo|T1513_MuroCiudad|T1524_MuroCierre|T1601_MuroCierre|LimiteInvisible)", RegexOptions.IgnoreCase))
            {
                return "cielo o muro";
            }
            Bounds b = r.bounds;
            Vector3 s = b.size;
            float[] d = { s.x, s.y, s.z };
            System.Array.Sort(d);
            if (b.max.y < 0.5f)
            {
                return "a ras de suelo (piso, banqueta, bordillo o calcomania)";
            }
            if (d[2] < 0.4f)
            {
                return "pieza diminuta (< 0.4 m): foco o remate";
            }
            if (d[1] < 0.06f)
            {
                return "tira o linea delgada (< 6 cm)";
            }
            if (Regex.IsMatch(ruta, "^Barrio/Manzana_[0-9]/Lugar_(Bar|Clandestino|MercadoNegro|Casa)/(Fachada|Cuerpo|Dintel$|Puerta/Marco_T536)"))
            {
                return "PENDIENTE: cascaron texturizado de un lugar con interior (bar, clandestino, mercado, casa); necesita fachada de arte (docs/assets-faltan.md)";
            }
            if (Regex.IsMatch(ruta, "^S2n_TallerMecanico/Cuerpo/"))
            {
                return "PENDIENTE: cuerpo del taller mecanico con textura de edificio (detras de su fachada); necesita modelo (docs/assets-faltan.md)";
            }
            if (r.name == "Relleno_Fachada" || r.name == "Cornisa")
            {
                return "junta o cornisa de fachada (tira de 0.3-0.5 m con la textura del edificio)";
            }
            if (Regex.IsMatch(ruta, "^Barrio/Manzana_1/Edificios/Edificio_7/Cuerpo_T531"))
            {
                return "PENDIENTE: cuerpo del edificio del CIC (cajas con textura de edificio); necesita modelo (docs/assets-faltan.md)";
            }
            if (r.GetComponents<MonoBehaviour>().Any(m => m != null && m.GetType().Name == "HojaPuerta"))
            {
                return "hoja de puerta que funciona";
            }
            string nombre = r.name + " " + (r.sharedMaterial != null ? r.sharedMaterial.name : "");
            if (NombreLuz.IsMatch(nombre) && d[1] < 0.35f)
            {
                return "tira o tubo de luz/neon (< 0.35 m de ancho)";
            }
            if (NombreLuz.IsMatch(nombre) && d[0] < 0.12f && (d[2] < 4f || nombre.Contains("Pantalla")))
            {
                return "pantalla o aviso luminoso plano";
            }
            if (NombreLuz.IsMatch(nombre) && b.min.y > 30f)
            {
                return "luz de aviso en lo alto de una torre (> 30 m)";
            }
            if (NombreDecal.IsMatch(nombre) && d[0] < 0.08f)
            {
                return "calcomania plana (ventana, charco, vapor, linea)";
            }
            if (r.GetComponentInParent<Camera>() != null || r.GetComponentsInParent<MonoBehaviour>(true).Any(m => m != null && m.GetType().Name == "Cabina"))
            {
                return "cabina o camara";
            }
            return null;
        }

        /// <summary>Primitivas activas y visibles que no estan en la lista blanca.</summary>
        public static List<Renderer> PrimitivasVisibles(Scene escena)
        {
            List<Renderer> lista = new List<Renderer>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (r.enabled && r.gameObject.activeInHierarchy && EsPrimitiva(r) && MotivoPermitida(r) == null)
                    {
                        lista.Add(r);
                    }
                }
            }
            return lista;
        }

        private static List<KeyValuePair<string, string>> PrimitivasPermitidas(Scene escena)
        {
            List<KeyValuePair<string, string>> lista = new List<KeyValuePair<string, string>>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (r.enabled && r.gameObject.activeInHierarchy && EsPrimitiva(r))
                    {
                        string m = MotivoPermitida(r);
                        if (m != null)
                        {
                            lista.Add(new KeyValuePair<string, string>(LimpiezaRuta(r.transform), m));
                        }
                    }
                }
            }
            return lista;
        }

        // ------------------------------------------------------------------ flotantes

        /// <summary>
        /// Objetos (raíz de prefab o renderer suelto) con el fondo más de <see cref="HuecoFlotante"/> arriba de lo que tienen debajo (raycast) y sin
        /// nada que los toque de lado. Se ignoran letreros, cables, luces, toldos y todo lo que tiene script de movimiento.
        /// </summary>
        public static List<Hallazgo> Flotantes(Scene escena)
        {
            List<Hallazgo> lista = new List<Hallazgo>();
            HashSet<GameObject> vistos = new HashSet<GameObject>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                if (Regex.IsMatch(raiz.name, "^(T1398_DronesAereos|Realismo|AMB_|T1513_Muro|T1524_Muro|T1601_Muro|S2n_Puentes|GTA_T1630|MechaEstacionado|JugadorAPie|MechaJugador)"))
                {
                    continue;
                }
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || r is SkinnedMeshRenderer)
                    {
                        continue;
                    }
                    GameObject unidad = Unidad(r.gameObject);
                    if (!vistos.Add(unidad))
                    {
                        continue;
                    }
                    string ruta = LimpiezaRuta(unidad.transform);
                    if (NoFlota.IsMatch(ruta) || ruta.StartsWith("T546_Interiores") || ruta.StartsWith("S2_Interiores") || ruta.StartsWith("T930_Casino"))
                    {
                        continue;
                    }
                    if (unidad.GetComponentsInChildren<MonoBehaviour>(true).Concat(unidad.GetComponentsInParent<MonoBehaviour>(true))
                        .Any(m => m != null && ScriptMovil.IsMatch(m.GetType().Name)))
                    {
                        continue;
                    }
                    Bounds b = Caja(unidad);
                    if (b.min.y < 1.5f || b.size.x > 20f || b.size.z > 20f)
                    {
                        continue;
                    }
                    float hueco = HuecoDebajo(unidad, b);
                    if (hueco <= HuecoFlotante || TocaDeLado(unidad, b))
                    {
                        continue;
                    }
                    lista.Add(new Hallazgo
                    {
                        Cat = Cat.Flotante, Objeto = unidad, Ruta = ruta, Pos = b.center, Tam = b.size, ApagarTodo = true,
                        Nota = "hueco " + hueco.ToString("F1") + " m debajo, nada a los lados"
                    });
                }
            }
            return lista;
        }

        /// <summary>El objeto que se apaga junto: la raíz del prefab más cercano o el padre de un "Visual".</summary>
        private static GameObject Unidad(GameObject go)
        {
            GameObject raizPrefab = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            Transform t = go.transform;
            if (t.name == "Visual" && t.parent != null)
            {
                t = t.parent;
            }
            if (raizPrefab != null && raizPrefab.transform.IsChildOf(t.root) && go.transform.IsChildOf(raizPrefab.transform)
                && Caja(raizPrefab).size.magnitude < 25f)
            {
                return raizPrefab.name == "Visual" && raizPrefab.transform.parent != null ? raizPrefab.transform.parent.gameObject : raizPrefab;
            }
            return t.gameObject;
        }

        private static float HuecoDebajo(GameObject unidad, Bounds b)
        {
            float peor = 0f;
            Vector3[] puntos =
            {
                b.center, new Vector3(b.min.x + 0.1f, 0f, b.min.z + 0.1f), new Vector3(b.max.x - 0.1f, 0f, b.max.z - 0.1f),
                new Vector3(b.min.x + 0.1f, 0f, b.max.z - 0.1f), new Vector3(b.max.x - 0.1f, 0f, b.min.z + 0.1f)
            };
            float mejor = float.MaxValue;
            foreach (Vector3 p in puntos)
            {
                Vector3 o = new Vector3(p.x, b.min.y + 0.05f, p.z);
                float h = b.min.y;
                foreach (RaycastHit hit in Physics.RaycastAll(o, Vector3.down, 300f, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance))
                {
                    if (hit.collider.transform.IsChildOf(unidad.transform))
                    {
                        continue;
                    }
                    h = hit.distance - 0.05f;
                    break;
                }
                mejor = Mathf.Min(mejor, h);
                peor = Mathf.Max(peor, h);
            }
            // Con que una esquina se apoye, no flota.
            return mejor;
        }

        private static bool TocaDeLado(GameObject unidad, Bounds b)
        {
            Collider[] cs = Physics.OverlapBox(b.center, b.extents + Vector3.one * MargenContacto, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            if (cs.Any(c => !c.transform.IsChildOf(unidad.transform)))
            {
                return true;
            }
            // Renderers vecinos sin collider (fachadas visuales): toca si su caja se cruza con la del objeto.
            Bounds grande = new Bounds(b.center, b.size + Vector3.one * MargenContacto * 2f);
            foreach (Renderer r in Vecinos(b))
            {
                if (!r.transform.IsChildOf(unidad.transform) && r.bounds.Intersects(grande))
                {
                    return true;
                }
            }
            return false;
        }

        private static Renderer[] cacheRenderers;

        private static IEnumerable<Renderer> Vecinos(Bounds b)
        {
            if (cacheRenderers == null)
            {
                cacheRenderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Where(r => r.enabled && !(r is ParticleSystemRenderer) && !(r is LineRenderer) && r.bounds.size.magnitude < 400f).ToArray();
            }
            Bounds g = new Bounds(b.center, b.size + Vector3.one * 2f);
            return cacheRenderers.Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy && r.bounds.Intersects(g));
        }

        // ------------------------------------------------------------------ afuera

        /// <summary>Lo que queda fuera aunque esté afuera: el fondo lejano de edificios (skyline T513), el muro, el cielo, el mecha estacionado
        /// del jugador, los drones que vuelan y las casas que forman el muro (S2o_CalleK01/Fondo: solo sus fachadas, pegadas a la línea).</summary>
        private static readonly Regex AfueraPermitido = new Regex(
            "^(T513_Fondo|T1513_MuroCiudad|T1524_MuroCierre|T1601_MuroCierre|T1543_MuroLuces|Realismo_T1605|AMB_|MechaEstacionado|MechaJugador|JugadorAPie|T1398_DronesAereos|LimiteInvisible)",
            RegexOptions.Compiled);

        /// <summary>Metros más allá de la línea del muro a partir de los que algo cuenta como afuera (el muro mide ~1.9 m de grueso).</summary>
        public const float MargenAfuera = 0.75f;

        /// <summary>Renderers activos con el centro más de <see cref="MargenAfuera"/> afuera de la línea del muro, sin contar <see cref="AfueraPermitido"/>,
        /// los efectos y los domos de cielo (más de 600 m).</summary>
        public static List<Renderer> RenderersAfuera(Scene escena)
        {
            Rect linea = CalculoMuroT1543.Expandir(FueraDelMuroT1543.LineaDelMuro(), MargenAfuera);
            List<Renderer> lista = new List<Renderer>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                if (AfueraPermitido.IsMatch(raiz.name))
                {
                    continue;
                }
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer)
                    {
                        continue;
                    }
                    Bounds b = r.bounds;
                    if (b.size.x > 600f || b.size.z > 600f)
                    {
                        continue;
                    }
                    if (!linea.Contains(new Vector2(b.center.x, b.center.z)) && !Regex.IsMatch(LimpiezaRuta(r.transform), "/Fondo/Casa_[0-9]+/(C0[1-5]_|Puerta|Ventana|Neon|Letrero|Toldo|FocoPared|C14|C10|C20|C23)"))
                    {
                        lista.Add(r);
                    }
                }
            }
            return lista;
        }

        // ================================================================== casino

        private static void ArreglarCasino(StringBuilder sb)
        {
            GameObject lugar = GameObject.Find(RutaLugarCasino);
            if (lugar == null)
            {
                sb.AppendLine("NO se encontro " + RutaLugarCasino);
                return;
            }
            Transform l = lugar.transform;

            // a) El pasillo: los bloques de 3 m entre la fachada y la sala se apagan completos (render y collider).
            foreach (string n in new[] { "CuerpoLadoIzquierdo", "CuerpoLadoDerecho", "CuerpoTecho" })
            {
                Transform t = l.Find(n);
                if (t != null && t.gameObject.activeSelf)
                {
                    Undo.RecordObject(t.gameObject, "T1680");
                    t.gameObject.SetActive(false);
                    Grabar(t.gameObject);
                    sb.AppendLine("  pasillo: apagado " + n);
                }
            }

            // b) La sala llega hasta la fachada: muros de enfrente a x 58.05 y piso, techo, muros laterales y frisos estirados hasta ahi.
            foreach (string n in new[] { "Interior/Revestimiento/MuroFrenteIzq", "Interior/Revestimiento/MuroFrenteDer" })
            {
                Transform t = l.Find(n);
                if (t == null)
                {
                    sb.AppendLine("  NO esta " + n);
                    continue;
                }
                Renderer r = t.GetComponent<Renderer>();
                float dx = XFrenteInterior - r.bounds.center.x;
                if (Mathf.Abs(dx) > 0.01f)
                {
                    Undo.RecordObject(t, "T1680");
                    t.position += new Vector3(dx, 0f, 0f);
                    Grabar(t);
                    sb.AppendLine("  " + n + " movido " + dx.ToString("F2") + " m en x");
                }
            }
            foreach (string n in new[]
            {
                "Interior/Revestimiento/Piso", "Interior/Revestimiento/Techo", "Interior/Revestimiento/MuroIzq", "Interior/Revestimiento/MuroDer",
                "Interior/Neon/FrisoIzquierdo", "Interior/Neon/FrisoDerecho", "Interior/Estructura/MuroIzquierdo", "Interior/Estructura/MuroDerecho",
                "Interior/Estructura/TechoInterior"
            })
            {
                Transform t = l.Find(n);
                if (t == null)
                {
                    sb.AppendLine("  NO esta " + n);
                    continue;
                }
                sb.AppendLine("  " + n + ": " + EstirarHastaX(t, XFrenteInterior - (n.Contains("Estructura") ? 1.05f : 0f)));
            }
            foreach (ZonaInterior z in lugar.GetComponentsInChildren<ZonaInterior>(true))
            {
                sb.AppendLine("  zona " + LimpiezaRuta(z.transform) + ": " + EstirarHastaX(z.transform, XFrenteInterior));
            }
            foreach (string ruta in new[] { "T546_Interiores/Casino/AlfombraEstampada" })
            {
                GameObject go = GameObject.Find(ruta);
                if (go != null)
                {
                    sb.AppendLine("  " + ruta + ": " + EstirarHastaX(go.transform, XFrenteInterior));
                }
            }

            // b2) El arco de adentro (I16) y el muro de arriba del arco pasan del frente viejo (x 61.1) al nuevo, junto a la puerta.
            foreach (string ruta in new[] { "S2o_Casino/I16", "T930_Casino/T1105_MuroSobreArco" })
            {
                GameObject go = GameObject.Find(ruta);
                if (go == null)
                {
                    sb.AppendLine("  NO esta " + ruta);
                    continue;
                }
                float dx = XFrenteInterior - XFrenteViejo;
                if (go.transform.position.x > XFrenteViejo - 0.5f)
                {
                    Undo.RecordObject(go.transform, "T1680");
                    go.transform.position += new Vector3(dx, 0f, 0f);
                    Grabar(go.transform);
                    sb.AppendLine("  " + ruta + " movido " + dx.ToString("F2") + " m en x (al frente nuevo)");
                }
            }

            // c) Afuera: el cascarón de cubos con la textura fea deja de verse (el collider se queda) y se viste con módulos NV01 de 3 pisos.
            int apagados = 0;
            foreach (string n in new[]
            {
                "FachadaIzquierda", "FachadaDerecha", "Dintel", "Interior/Estructura/MuroFondo", "Interior/Estructura/MuroIzquierdo",
                "Interior/Estructura/MuroDerecho", "Interior/Estructura/TechoInterior", "Puerta/Marco_T536", "EntradaNeon", "Cerrado"
            })
            {
                Transform t = l.Find(n);
                if (t != null)
                {
                    apagados += ApagarRenderers(t.gameObject);
                }
            }
            sb.AppendLine("  cascaron, marco de cubos, entrada de cubos y columnas/postes: " + apagados + " renderers apagados");
            sb.AppendLine("  " + VestirCasino());
        }

        public const string RutaAsfaltoColor = "Assets/_Proyecto/Mundo/Texturas/T1680_Asfalto_Color.png";
        public const string RutaAsfaltoNormal = "Assets/_Proyecto/Mundo/Texturas/T1680_Asfalto_Normal.png";
        public static readonly string[] MaterialesAsfalto =
        {
            "Assets/Arte/Mundo/S2c/S2c_Asfalto.mat", "Assets/_Proyecto/Mundo/Materiales/Barrio_Asfalto.mat",
            "Assets/_Proyecto/Mundo/Materiales/CalleCasa_Asfalto.mat", "Assets/Arte/Mundo/Look/T1461/T1461_S2c_Asfalto.mat"
        };

        /// <summary>
        /// La textura de asfalto (T535) traía parches cuadrados oscuros pintados (se veían como cuadros a media calle). T1680_Asfalto_* es la misma
        /// textura con los parches emparejados a su alrededor (Python, una vez) y sin sus bordes en el normal; aquí se importa y se pone en los asfaltos.
        /// </summary>
        public static string AsfaltoSinParches()
        {
            AssetDatabase.ImportAsset(RutaAsfaltoColor);
            AssetDatabase.ImportAsset(RutaAsfaltoNormal);
            TextureImporter ti = AssetImporter.GetAtPath(RutaAsfaltoNormal) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaAsfaltoColor);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaAsfaltoNormal);
            if (color == null || normal == null)
            {
                return "NO estan las texturas " + RutaAsfaltoColor;
            }
            int n = 0;
            foreach (string ruta in MaterialesAsfalto)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m == null)
                {
                    continue;
                }
                foreach (string prop in new[] { "_BaseMap", "_MainTex" })
                {
                    if (m.HasProperty(prop))
                    {
                        m.SetTexture(prop, color);
                    }
                }
                if (m.HasProperty("_BumpMap") && m.GetTexture("_BumpMap") != null)
                {
                    m.SetTexture("_BumpMap", normal);
                }
                EditorUtility.SetDirty(m);
                n++;
            }
            AssetDatabase.SaveAssets();
            return "asfalto sin parches en " + n + " materiales";
        }

        /// <summary>Estira un objeto alineado a los ejes para que su borde oeste (x mínima) quede en <paramref name="x"/>; el borde este no se mueve.</summary>
        private static string EstirarHastaX(Transform t, float x)
        {
            Bounds b;
            Renderer r = t.GetComponent<Renderer>();
            Collider c = t.GetComponent<Collider>();
            if (r != null)
            {
                b = r.bounds;
            }
            else if (c != null)
            {
                b = c.bounds;
            }
            else
            {
                return "sin caja";
            }
            if (b.min.x <= x + 0.02f)
            {
                return "ya llega (x min " + b.min.x.ToString("F2") + ")";
            }
            float anchoViejo = b.size.x;
            float anchoNuevo = b.max.x - x;
            int eje = EjeLocalDeMundoX(t);
            Undo.RecordObject(t, "T1680");
            Vector3 s = t.localScale;
            s[eje] *= anchoNuevo / anchoViejo;
            t.localScale = s;
            Physics.SyncTransforms();
            Bounds nb = r != null ? r.bounds : c.bounds;
            t.position += new Vector3((x + b.max.x) * 0.5f - nb.center.x, 0f, 0f);
            Grabar(t);
            return "x " + b.min.x.ToString("F2") + ".." + b.max.x.ToString("F2") + " -> " + x.ToString("F2") + ".." + b.max.x.ToString("F2");
        }

        private static int EjeLocalDeMundoX(Transform t)
        {
            float ax = Mathf.Abs(Vector3.Dot(t.right, Vector3.right));
            float ay = Mathf.Abs(Vector3.Dot(t.up, Vector3.right));
            float az = Mathf.Abs(Vector3.Dot(t.forward, Vector3.right));
            return ax >= ay && ax >= az ? 0 : (ay >= az ? 1 : 2);
        }

        /// <summary>Módulos NV01 (oficinas, modelo real) en los cuatro lados del casino, 3 pisos, con el hueco de la puerta libre en planta baja.</summary>
        private static string VestirCasino()
        {
            GameObject viejo = GameObject.Find(RaizFachada);
            while (viejo != null)
            {
                Object.DestroyImmediate(viejo);
                viejo = GameObject.Find(RaizFachada);
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabModulo);
            if (prefab == null)
            {
                return "NO esta " + PrefabModulo;
            }
            GameObject raiz = new GameObject(RaizFachada);
            int n = 0;
            float altoPiso = 3f;
            // Oeste (calle, x 57): dos tramos a los lados de la puerta y encima de ella.
            n += Tramo(raiz.transform, prefab, Vector3.left, XFachadaCasino, HuellaCasino.yMin, ZPuertaMin, 3, 0, PisosCasino, altoPiso);
            n += Tramo(raiz.transform, prefab, Vector3.left, XFachadaCasino, ZPuertaMax, HuellaCasino.yMax, 3, 0, PisosCasino, altoPiso);
            n += Tramo(raiz.transform, prefab, Vector3.left, XFachadaCasino, ZPuertaMin, ZPuertaMax, 1, 1, PisosCasino, altoPiso);
            // Sur (z 31), norte (z 63) y este (x 89).
            n += Tramo(raiz.transform, prefab, Vector3.back, HuellaCasino.yMin, HuellaCasino.xMin, HuellaCasino.xMax, 5, 0, PisosCasino, altoPiso);
            n += Tramo(raiz.transform, prefab, Vector3.forward, HuellaCasino.yMax, HuellaCasino.xMin, HuellaCasino.xMax, 5, 0, PisosCasino, altoPiso);
            n += Tramo(raiz.transform, prefab, Vector3.right, HuellaCasino.xMax, HuellaCasino.yMin, HuellaCasino.yMax, 5, 0, PisosCasino, altoPiso);
            return "fachada NV01: " + n + " modulos en " + RaizFachada;
        }

        /// <summary>Una fila de módulos por piso sobre una cara del cascarón. <paramref name="plano"/> es la x (caras este/oeste) o la z (norte/sur) de la cara.</summary>
        private static int Tramo(Transform padre, GameObject prefab, Vector3 normal, float plano, float desde, float hasta, int modulos, int pisoInicial, int pisos, float altoPiso)
        {
            bool caraX = Mathf.Abs(normal.x) > 0.5f;
            float ancho = (hasta - desde) / modulos;
            int n = 0;
            for (int p = pisoInicial; p < pisos; p++)
            {
                for (int m = 0; m < modulos; m++)
                {
                    float a = desde + (m + 0.5f) * ancho;
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
                    go.name = "NV01_" + (caraX ? (normal.x < 0 ? "O" : "E") : (normal.z < 0 ? "S" : "N")) + "_" + p + "_" + m;
                    go.transform.rotation = Quaternion.LookRotation(-normal);
                    go.transform.position = Vector3.zero;
                    Physics.SyncTransforms();
                    Bounds b0 = Caja(go);
                    float anchoModelo = caraX ? b0.size.z : b0.size.x;
                    float altoModelo = b0.size.y;
                    go.transform.localScale = new Vector3(ancho / anchoModelo, altoPiso / altoModelo, 1f);
                    Physics.SyncTransforms();
                    Bounds b = Caja(go);
                    // El lado de atrás (hacia el edificio) queda pegado a la cara; el pie en el piso p.
                    float atras = caraX ? (normal.x < 0 ? b.max.x : b.min.x) : (normal.z < 0 ? b.max.z : b.min.z);
                    Vector3 mover = new Vector3(0f, p * altoPiso + 0.15f - b.min.y, 0f);
                    if (caraX)
                    {
                        mover.x = plano - atras + normal.x * 0.02f;
                        mover.z = a - b.center.z;
                    }
                    else
                    {
                        mover.z = plano - atras + normal.z * 0.02f;
                        mover.x = a - b.center.x;
                    }
                    go.transform.position += mover;
                    foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                    {
                        c.enabled = false;
                    }
                    n++;
                }
            }
            return n;
        }

        // ================================================================== fotos

        [MenuItem("Mecha Cyberpunk/T-1680 Limpieza: fotos")]
        public static void Fotos()
        {
            EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Single);
            string suf = System.Environment.GetEnvironmentVariable("T1680_SUF");
            if (string.IsNullOrEmpty(suf))
            {
                suf = "despues";
            }
            Directory.CreateDirectory(CarpetaFotos);
            foreach (KeyValuePair<string, Vector3[]> v in Vistas())
            {
                Foto(v.Key + "_" + suf, v.Value[0], v.Value[1], v.Value[2].x);
            }
        }

        public static Dictionary<string, Vector3[]> Vistas()
        {
            Vector3 fov = new Vector3(70f, 0f, 0f);
            return new Dictionary<string, Vector3[]>
            {
                { "01_calledorada", new[] { new Vector3(-112f, 1.7f, -41.5f), new Vector3(-56f, 2f, -43f), fov } },
                { "01_calledorada_cerca", new[] { new Vector3(-75f, 1.7f, -41.5f), new Vector3(-50f, 2f, -43f), fov } },
                { "02_nova_calle", new[] { new Vector3(58f, 1.7f, -39.5f), new Vector3(100f, 3f, -44f), fov } },
                { "02_oriente_calle", new[] { new Vector3(-30f, 1.7f, -40f), new Vector3(10f, 3f, -43f), fov } },
                { "05_bar_calledoc", new[] { new Vector3(-110.5f, 1.7f, -34f), new Vector3(-110f, 1.2f, -22f), fov } },
                { "07_techos", new[] { new Vector3(15f, 1.7f, -4f), new Vector3(15f, 12f, -30f), fov } },
                { "07_techos_norte", new[] { new Vector3(15f, 1.7f, 4f), new Vector3(15f, 12f, 30f), fov } },
                { "05_cic_pilastra", new[] { new Vector3(-66f, 1.7f, 4f), new Vector3(-70f, 2f, 10.2f), fov } },
                { "05_idi_pilastra", new[] { new Vector3(-94f, 1.7f, -5f), new Vector3(-97f, 2f, -10.2f), fov } },
                { "cic", new[] { new Vector3(-73f, 1.7f, -2f), new Vector3(-73f, 5f, 12f), fov } },
                { "08_casino_dentro", new[] { new Vector3(68f, 1.7f, 47f), new Vector3(57f, 1.7f, 47f), fov } },
                { "08_casino_fuera", new[] { new Vector3(46f, 1.7f, 47f), new Vector3(58f, 4f, 47f), fov } },
                { "08_casino_esquina", new[] { new Vector3(45f, 2.5f, 25f), new Vector3(60f, 5f, 45f), fov } },
                { "aerea_casino", new[] { new Vector3(40f, 45f, 10f), new Vector3(72f, 0f, 47f), new Vector3(60f, 0f, 0f) } }
            };
        }

        public static void Foto(string nombre, Vector3 pos, Vector3 mira, float fov)
        {
            GameObject go = new GameObject("CamFotosT1680");
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.farClipPlane = 1500f;
            cam.nearClipPlane = 0.05f;
            go.transform.position = pos;
            go.transform.LookAt(mira);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CarpetaFotos, nombre + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log(Etiqueta + "foto " + nombre);
        }

        // ================================================================== utilidades

        /// <summary>Huecos que mide T-1543 (para revisar qué dejó abierto la limpieza).</summary>
        public static void Huecos()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            Physics.SyncTransforms();
            foreach (FueraDelMuroT1543.ZonaHueco z in FueraDelMuroT1543.MedirHuecos(escena).Take(15))
            {
                Debug.Log(Etiqueta + "hueco " + z.Area + " m2 en " + V(z.Caja.center) + " tam " + V(z.Caja.size) + " " + z.Barrio + (z.JuntoAlMuro ? " (junto al muro)" : ""));
            }
        }

        public static void Volcado()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ruta\tmalla\trutaMalla\tverts\tmat\tcentro\ttam\tprefab\tscripts");
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled)
                    {
                        continue;
                    }
                    Mesh m = null;
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        m = mf.sharedMesh;
                    }
                    if (r is SkinnedMeshRenderer smr)
                    {
                        m = smr.sharedMesh;
                    }
                    string rm = m != null ? AssetDatabase.GetAssetPath(m) : "";
                    string mat = r.sharedMaterial != null ? r.sharedMaterial.name : "";
                    string pref = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(r.gameObject) ?? "";
                    Bounds b = r.bounds;
                    sb.Append(LimpiezaRuta(r.transform)).Append('\t').Append(m != null ? m.name : "-").Append('\t').Append(rm).Append('\t')
                        .Append(m != null ? m.vertexCount : 0).Append('\t').Append(mat).Append('\t').Append(V(b.center)).Append('\t')
                        .Append(V(b.size)).Append('\t').Append(pref).Append('\t').Append(Scripts(r.transform)).AppendLine();
                }
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaVolcado, sb.ToString());
            Debug.Log(Etiqueta + "volcado en " + RutaVolcado);
        }

        private static void Escribir(StringBuilder sb, List<Hallazgo> lista)
        {
            foreach (var g in lista.GroupBy(h => h.Cat))
            {
                sb.AppendLine();
                sb.AppendLine("=== " + g.Key + " (" + g.Count() + ") ===");
                foreach (Hallazgo h in g.OrderBy(h => h.Ruta))
                {
                    sb.AppendLine("  " + h.Ruta + "\tpos " + V(h.Pos) + "\ttam " + V(h.Tam) + "\t" + (h.ApagarTodo ? "[apagar]" : "[sin render]") + "\t" + h.Nota);
                }
            }
        }

        private static void Jerarquia(StringBuilder sb, Transform t, int nivel)
        {
            string comps = string.Join(",", t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name));
            Bounds b = Caja(t.gameObject);
            sb.AppendLine(new string(' ', nivel * 2) + t.name + (t.gameObject.activeSelf ? "" : " (apagado)") + " [" + comps + "] " + V(b.center) + " " + V(b.size));
            if (nivel < 4)
            {
                foreach (Transform h in t)
                {
                    Jerarquia(sb, h, nivel + 1);
                }
            }
        }

        private static bool SoloVisual(GameObject go)
        {
            if (go.transform.childCount > 0)
            {
                return false;
            }
            return go.GetComponents<Component>().All(c => c == null || c is Transform || c is MeshFilter || c is Renderer || (c is Collider col && !col.isTrigger));
        }

        private static int ApagarRenderers(GameObject go)
        {
            int n = 0;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.enabled && !(r is ParticleSystemRenderer))
                {
                    Undo.RecordObject(r, "T1680");
                    r.enabled = false;
                    Grabar(r);
                    n++;
                }
            }
            return n;
        }

        private static void Grabar(Object o)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(o))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(o);
            }
        }

        public static Bounds Caja(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
            {
                return new Bounds(go.transform.position, Vector3.zero);
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        private static bool DentroXZ(Bounds b, Vector3 p, float margen)
        {
            return p.x >= b.min.x + margen && p.x <= b.max.x - margen && p.z >= b.min.z + margen && p.z <= b.max.z - margen;
        }

        private static string Malla(Renderer r)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            return mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "-";
        }

        internal static string V(Vector3 v)
        {
            return v.x.ToString("F1") + "," + v.y.ToString("F1") + "," + v.z.ToString("F1");
        }

        public static string LimpiezaRuta(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }

        private static string Scripts(Transform t)
        {
            HashSet<string> s = new HashSet<string>();
            for (Transform p = t; p != null; p = p.parent)
            {
                foreach (MonoBehaviour mb in p.GetComponents<MonoBehaviour>())
                {
                    if (mb != null)
                    {
                        s.Add(mb.GetType().Name);
                    }
                }
            }
            return string.Join(",", s);
        }
    }
}

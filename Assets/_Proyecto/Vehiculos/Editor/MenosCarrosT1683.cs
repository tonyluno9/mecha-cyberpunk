using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T45 / T-1683: menos carros. Se quedan los básicos (sedanes, compactos, pickups, taxi, la combi V04), los dos tuneados con neón debajo (V20, V21),
    /// un deportivo (V12), el sedán de lujo V15 y la ambulancia V19; salen el deportivo V01b, el microbús V16, el camión V17, el tráiler V18 y los
    /// superdeportivos V22 a V24 (tabla y motivos en docs/carros-T45.md). La policía (V05, V06, V08, V11, PA01, PA02, Patrulla*) y las motos no se tocan.
    /// 1. Listas: quita los prefabs excluidos (Carro_X, CarroTrafico_X y sus variantes) de las listas de los ScriptableObject de Trafico y Vehiculos/Datos
    ///    (ConfigCarrosZonas) y de las de los componentes de Ciudad_Prologo (TraficoCalle.prefabsCarro). Los prefabs se quedan en disco (otras pruebas los usan).
    /// 2. Ciudad_Prologo: cada carro de un modelo excluido (Estacionado_*, CarroRobable_*...) se cambia por uno de los que se quedan (<see cref="Reemplazos"/>)
    ///    con la misma rotación, el mismo padre y la huella centrada en el mismo lugar con el piso a la misma altura; conserva nombre propio, capa, si estaba activo,
    ///    componentes agregados y los valores de CarroEstacionado. Después corre <see cref="EstacionadosTripoT1644.Ajustar"/> (el mismo procedimiento de T-1644:
    ///    al piso, fuera de la banqueta, sin encimarse; si no cabe, lo apaga con SetActive(false), nunca lo borra).
    /// Diagnostico no cambia nada. Todo va a Logs/menos-carros-T1683.txt. Idempotente.
    /// Orden: Diagnostico, Aplicar, Diagnostico. Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.MenosCarrosT1683.Aplicar (y .Diagnostico).
    /// </summary>
    public static class MenosCarrosT1683
    {
        public const string RutaInforme = "Logs/menos-carros-T1683.txt";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string PrefijoEstacionado = "Estacionado_";
        public const string CarpetaPrefabsTrafico = "Assets/_Proyecto/Trafico/Prefabs/";

        private const string PrefijoCarro = "Carro_";
        private const string PrefijoCarroTrafico = "CarroTrafico_";

        /// <summary>Modelos que salen de las listas y de la escena.</summary>
        public static readonly string[] CodigosExcluidos = { "V01b", "V16", "V17", "V18", "V22", "V23", "V24" };

        /// <summary>Modelos civiles de cuatro ruedas que se quedan.</summary>
        public static readonly string[] CodigosQueSeQuedan = { "V01a", "V02", "V03", "V04", "V09", "V12", "V13", "V14", "V15", "V19", "V20", "V21" };

        /// <summary>Por cuál se cambia cada excluido en la escena (tamaño parecido; sin sumar deportivos).</summary>
        public static readonly (string excluido, string reemplazo)[] Reemplazos =
        {
            ("V01b", "V13"), // deportivo -> sedán familiar
            ("V16", "V02"),  // microbús -> sedán viejo
            ("V17", "V03"),  // camión -> pickup
            ("V18", "V14"),  // tráiler -> compacto
            ("V22", "V20"),  // superdeportivo -> tuneado con neón
            ("V23", "V21"),  // superdeportivo -> tuneado con neón
            ("V24", "V09"),  // superdeportivo -> taxi
        };

        private static readonly string[] CarpetasPrefabs =
        {
            "Assets/_Proyecto/Vehiculos/Prefabs", "Assets/_Proyecto/Trafico/Prefabs", "Assets/Arte/Vehiculos/Variantes", "Assets/Arte/Vehiculos/Prefabs",
        };

        private static readonly string[] CarpetasDatos = { "Assets/_Proyecto/Trafico", "Assets/_Proyecto/Vehiculos/Datos" };

        private static readonly string[] EspaciosConListas =
        {
            "MechaCyberpunk.Trafico", "MechaCyberpunk.Vehiculos", "MechaCyberpunk.Mundo", "MechaCyberpunk.Progresion",
        };

        private static readonly string[] PropiedadesEstacionado = { "calorAlRobar", "tinte", "esTuyo" };

        // ------------------------------------------------------------------ reglas (sin escena)

        /// <summary>
        /// El código del modelo en el nombre de un prefab o de un objeto: Carro_V01b, CarroTrafico_V02_Rojo, V01b_CarroPro, Estacionado_12_V17 → V01b, V02, V01b, V17.
        /// null si no es de un carro con código (CarroTrafico, CarroViejo, Patrulla...).
        /// </summary>
        public static string CodigoDe(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                return null;
            }
            string resto;
            if (nombre.StartsWith(PrefijoCarroTrafico, StringComparison.Ordinal))
            {
                resto = nombre.Substring(PrefijoCarroTrafico.Length);
            }
            else if (nombre.StartsWith(PrefijoCarro, StringComparison.Ordinal))
            {
                resto = nombre.Substring(PrefijoCarro.Length);
            }
            else if (nombre.StartsWith(PrefijoEstacionado, StringComparison.Ordinal))
            {
                string[] partes = nombre.Split('_');
                return partes.Length >= 3 ? partes[2] : null;
            }
            else
            {
                resto = nombre;
            }
            string codigo = resto.Split('_', ' ', '(')[0];
            return EsCodigo(codigo) ? codigo : null;
        }

        // V01a, V12, MT01, PA02, C19: letras y luego al menos dos dígitos (y una letra al final, como V01a).
        private static bool EsCodigo(string codigo)
        {
            int i = 0;
            while (i < codigo.Length && char.IsUpper(codigo[i]))
            {
                i++;
            }
            int letras = i;
            while (i < codigo.Length && char.IsDigit(codigo[i]))
            {
                i++;
            }
            int digitos = i - letras;
            if (i < codigo.Length && char.IsLower(codigo[i]))
            {
                i++;
            }
            return letras >= 1 && letras <= 2 && digitos >= 2 && i == codigo.Length;
        }

        /// <summary>true si el nombre (prefab u objeto) es de un modelo excluido.</summary>
        public static bool EsExcluido(string nombre)
        {
            string codigo = CodigoDe(nombre);
            return codigo != null && CodigosExcluidos.Contains(codigo);
        }

        /// <summary>El código que va en lugar de <paramref name="excluido"/> (null si no está excluido).</summary>
        public static string ReemplazoDe(string excluido)
        {
            foreach (var par in Reemplazos)
            {
                if (par.excluido == excluido)
                {
                    return par.reemplazo;
                }
            }
            return null;
        }

        /// <summary>Rutas de todos los prefabs de modelos excluidos (de carro, de tráfico, variantes de color y viejos de una pieza).</summary>
        public static HashSet<string> RutasExcluidas()
        {
            HashSet<string> rutas = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", CarpetasPrefabs.Where(AssetDatabase.IsValidFolder).ToArray()))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (EsExcluido(Path.GetFileNameWithoutExtension(ruta)))
                {
                    rutas.Add(ruta);
                }
            }
            return rutas;
        }

        /// <summary>El prefab que reemplaza al de <paramref name="rutaExcluida"/>: el mismo tipo (Carro_ o CarroTrafico_) del modelo de <see cref="Reemplazos"/>.</summary>
        public static string RutaDeReemplazo(string rutaExcluida)
        {
            string nombre = Path.GetFileNameWithoutExtension(rutaExcluida);
            string nuevo = ReemplazoDe(CodigoDe(nombre));
            if (nuevo == null)
            {
                return null;
            }
            return nombre.StartsWith(PrefijoCarroTrafico, StringComparison.Ordinal)
                ? CarpetaPrefabsTrafico + PrefijoCarroTrafico + nuevo + ".prefab"
                : CreadorCarrosT511.CarpetaPrefabs + PrefijoCarro + nuevo + ".prefab";
        }

        // ------------------------------------------------------------------ entrada

        [MenuItem("Mecha Cyberpunk/T-1683/Diagnóstico menos carros")]
        public static void Diagnostico() => Ejecutar(false);

        [MenuItem("Mecha Cyberpunk/T-1683/Aplicar menos carros")]
        public static void Aplicar() => Ejecutar(true);

        private static void Ejecutar(bool aplicar)
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            informe.AppendLine("== T-1683 (T45) menos carros: " + (aplicar ? "Aplicar" : "Diagnostico") + "  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            informe.AppendLine("Excluidos: " + string.Join(", ", CodigosExcluidos));
            informe.AppendLine("Se quedan: " + string.Join(", ", CodigosQueSeQuedan) + " (más policía y motos, que no se tocan)");
            informe.AppendLine("Reemplazos en la escena: " + string.Join(", ", Reemplazos.Select(r => r.excluido + "->" + r.reemplazo)));
            HashSet<string> rutas = RutasExcluidas();
            informe.AppendLine("Prefabs excluidos en disco (se quedan, solo salen de listas y escena): " + rutas.Count);
            foreach (string r in rutas.OrderBy(x => x))
            {
                informe.AppendLine("\t" + r);
            }

            informe.AppendLine("== 1. Listas en assets de datos");
            informe.Append(ListasAssets(rutas, aplicar));
            if (aplicar)
            {
                AssetDatabase.SaveAssets();
            }

            informe.AppendLine("== 2. " + RutaEscena);
            bool escenaCambio;
            try
            {
                escenaCambio = Escena(rutas, aplicar, informe);
            }
            catch (Exception e)
            {
                informe.AppendLine("ERROR en la escena: " + e);
                escenaCambio = false;
            }

            if (aplicar && escenaCambio)
            {
                informe.AppendLine("== 3. EstacionadosTripoT1644.Ajustar (al piso, fuera de la banqueta, sin encimarse)");
                try
                {
                    EstacionadosTripoT1644.Ajustar();
                    informe.Append(File.Exists(EstacionadosTripoT1644.RutaInforme) ? File.ReadAllText(EstacionadosTripoT1644.RutaInforme) : "(sin informe de T-1644)\n");
                }
                catch (Exception e)
                {
                    informe.AppendLine("ERROR en Ajustar: " + e);
                }
            }
            else if (!aplicar)
            {
                informe.AppendLine("== 3. (Aplicar corre después EstacionadosTripoT1644.Ajustar)");
            }
            Escribir(informe);
        }

        private static void Escribir(StringBuilder informe)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Vehiculos] T-1683 menos carros:\n" + informe);
        }

        // ------------------------------------------------------------------ 1. listas de datos

        private static string ListasAssets(HashSet<string> rutas, bool aplicar)
        {
            StringBuilder sb = new StringBuilder();
            int total = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", CarpetasDatos))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ruta);
                if (so == null)
                {
                    continue;
                }
                int n = VehiculosT1554.ReferenciasEnListas(new SerializedObject(so), rutas, aplicar);
                if (n == 0)
                {
                    continue;
                }
                total += n;
                sb.AppendLine("\t" + ruta + ": " + n + " referencias a excluidos" + (aplicar ? " quitadas" : ""));
                if (aplicar)
                {
                    EditorUtility.SetDirty(so);
                }
            }
            sb.AppendLine("\ttotal en assets de datos: " + total);
            return sb.ToString();
        }

        // ------------------------------------------------------------------ 2. escena

        private static bool Escena(HashSet<string> rutas, bool aplicar, StringBuilder sb)
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            bool cambio = false;

            // a) Listas de los componentes (tráfico, zonas, encargos...).
            int enListas = 0;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || mb.GetType().Namespace == null || !EspaciosConListas.Any(e => mb.GetType().Namespace.StartsWith(e, StringComparison.Ordinal)))
                    {
                        continue;
                    }
                    SerializedObject so = new SerializedObject(mb);
                    int n = VehiculosT1554.ReferenciasEnListas(so, rutas, aplicar);
                    if (n == 0)
                    {
                        continue;
                    }
                    enListas += n;
                    sb.AppendLine("\t" + mb.GetType().Name + " en " + Ruta(mb.transform) + ": " + n + " referencias a excluidos" + (aplicar ? " quitadas" : ""));
                    if (aplicar)
                    {
                        EditorUtility.SetDirty(mb);
                        cambio = true;
                    }
                }
            }
            sb.AppendLine("\treferencias a excluidos en listas de componentes: " + enListas);

            // b) Los carros de modelos excluidos que hay en la escena.
            List<GameObject> carros = Instancias(escena.GetRootGameObjects(), rutas, out int _);
            List<GameObject> anidadas = InstanciasAnidadas(escena.GetRootGameObjects(), rutas);
            sb.AppendLine("\tcarros de modelos excluidos en la escena: " + carros.Count + " (activos " + carros.Count(c => c.activeInHierarchy) + ")");
            sb.AppendLine("\tdentro de otra instancia de prefab (Barrio...): " + anidadas.Count + " (activos " + anidadas.Count(c => c.activeInHierarchy)
                + "): se pone el reemplazo al lado y se apagan");
            carros.AddRange(anidadas.Where(a => a.activeSelf));
            Dictionary<string, int> porModelo = new Dictionary<string, int>();
            foreach (GameObject go in carros)
            {
                string rutaVieja = RutaExcluidaDe(go, rutas);
                string codigo = CodigoDe(Path.GetFileNameWithoutExtension(rutaVieja));
                porModelo[codigo] = porModelo.TryGetValue(codigo, out int k) ? k + 1 : 1;
                cambio |= Reemplazar(go, rutaVieja, aplicar, sb);
            }
            sb.AppendLine("\tpor modelo: " + (porModelo.Count == 0 ? "ninguno" : string.Join(", ", porModelo.OrderBy(p => p.Key).Select(p => p.Key + " x" + p.Value))));

            // c) Lo que queda por modelo (todas las instancias de Carro_*), para el informe.
            Dictionary<string, int> quedan = new Dictionary<string, int>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                    {
                        continue;
                    }
                    Object fuente = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                    string c = fuente != null ? CodigoDe(fuente.name) : null;
                    if (c != null)
                    {
                        quedan[c] = quedan.TryGetValue(c, out int k) ? k + 1 : 1;
                    }
                }
            }
            sb.AppendLine("\tinstancias por modelo " + (aplicar ? "después" : "ahora") + ": " + string.Join(", ", quedan.OrderBy(p => p.Key).Select(p => p.Key + " x" + p.Value)));

            if (aplicar && cambio)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
                sb.AppendLine("\tescena guardada: " + (new FileInfo(RutaEscena).Length / 1048576f).ToString("0.0") + " MB");
            }
            return cambio;
        }

        /// <summary>
        /// Raíces de instancias de prefabs excluidos que no están dentro de otra instancia. <paramref name="anidados"/> cuenta las que están dentro de otra
        /// instancia que no es de un excluido (p. ej. los Carro_V01b del prefab Barrio): esas las da <see cref="InstanciasAnidadas"/>.
        /// </summary>
        public static List<GameObject> Instancias(IEnumerable<GameObject> raices, HashSet<string> rutas, out int anidados)
        {
            List<GameObject> lista = new List<GameObject>();
            List<GameObject> dentro = InstanciasAnidadas(raices, rutas, lista);
            anidados = dentro.Count;
            return lista;
        }

        /// <summary>
        /// Instancias de excluidos que viven dentro de otra instancia de prefab que no es de un excluido (Barrio.prefab trae Carro_V01b). No se pueden borrar
        /// desde la escena: Aplicar pone su reemplazo al lado y las apaga. Si se da <paramref name="sueltas"/>, ahí van las que no están anidadas.
        /// </summary>
        public static List<GameObject> InstanciasAnidadas(IEnumerable<GameObject> raices, HashSet<string> rutas, List<GameObject> sueltas = null)
        {
            List<GameObject> lista = new List<GameObject>();
            foreach (GameObject raiz in raices)
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = t.gameObject;
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(go) || RutaExcluidaDe(go, rutas) == null)
                    {
                        continue;
                    }
                    if (DentroDeUnExcluido(go, rutas))
                    {
                        continue; // es una pieza de un carro excluido: se va con él
                    }
                    if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) != go)
                    {
                        lista.Add(go);
                    }
                    else
                    {
                        sueltas?.Add(go);
                    }
                }
            }
            return lista;
        }

        /// <summary>La ruta del prefab excluido de donde sale la instancia (siguiendo la cadena de prefabs anidados y variantes), o null.</summary>
        public static string RutaExcluidaDe(GameObject go, HashSet<string> rutas)
        {
            Object o = go;
            for (int i = 0; i < 12 && o != null; i++)
            {
                o = PrefabUtility.GetCorrespondingObjectFromSource(o);
                if (o == null)
                {
                    break;
                }
                string ruta = AssetDatabase.GetAssetPath(o);
                if (rutas.Contains(ruta))
                {
                    return ruta;
                }
            }
            return null;
        }

        private static bool DentroDeUnExcluido(GameObject go, HashSet<string> rutas)
        {
            for (Transform p = go.transform.parent; p != null; p = p.parent)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(p.gameObject) && RutaExcluidaDe(p.gameObject, rutas) != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Reemplazar(GameObject go, string rutaVieja, bool aplicar, StringBuilder sb)
        {
            string rutaNueva = RutaDeReemplazo(rutaVieja);
            GameObject prefabNuevo = rutaNueva != null ? AssetDatabase.LoadAssetAtPath<GameObject>(rutaNueva) : null;
            string descripcion = "\t" + Ruta(go.transform) + " (" + Path.GetFileName(rutaVieja) + ", " + (go.activeSelf ? "activo" : "apagado") + ") en "
                + go.transform.position.ToString("F2") + " giro " + go.transform.eulerAngles.y.ToString("F0");
            if (prefabNuevo == null)
            {
                // Sin reemplazo: se apaga (nunca se borra).
                sb.AppendLine(descripcion + " -> SIN reemplazo (" + (rutaNueva ?? "sin regla") + "): " + (aplicar ? "APAGADO" : "se apagaría"));
                if (aplicar && go.activeSelf)
                {
                    go.SetActive(false);
                    return true;
                }
                return false;
            }
            // Un objeto apagado no tiene cajas de mundo confiables: se deja con la misma posición y rotación.
            bool medir = go.activeInHierarchy;
            Bounds viejo = medir ? Medidas(go) : new Bounds(go.transform.position, Vector3.zero);
            if (!aplicar)
            {
                sb.AppendLine(descripcion + " -> " + Path.GetFileName(rutaNueva) + " (caja vieja " + viejo.size.ToString("F2") + ")");
                return false;
            }

            Transform padre = go.transform.parent;
            GameObject nuevo = padre != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefabNuevo, padre) : (GameObject)PrefabUtility.InstantiatePrefab(prefabNuevo, go.scene);
            nuevo.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);
            if (padre == null || !PrefabUtility.IsPartOfPrefabInstance(padre))
            {
                nuevo.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
            }
            nuevo.layer = go.layer;
            // Huella centrada donde estaba la del viejo y el fondo a la misma altura (el ajuste fino al piso lo hace EstacionadosTripoT1644.Ajustar).
            Physics.SyncTransforms();
            Bounds medida = medir && nuevo.activeInHierarchy ? Medidas(nuevo) : new Bounds(nuevo.transform.position, Vector3.zero);
            if (viejo.size != Vector3.zero && medida.size != Vector3.zero)
            {
                nuevo.transform.position += new Vector3(viejo.center.x - medida.center.x, viejo.min.y - medida.min.y, viejo.center.z - medida.center.z);
            }
            nuevo.name = NombreNuevo(go.name, Path.GetFileNameWithoutExtension(rutaVieja), prefabNuevo.name);
            CopiarEstacionado(go, nuevo);
            foreach (AddedComponent agregado in PrefabUtility.GetAddedComponents(go))
            {
                if (agregado.instanceComponent != null && nuevo.GetComponent(agregado.instanceComponent.GetType()) == null
                    && UnityEditorInternal.ComponentUtility.CopyComponent(agregado.instanceComponent))
                {
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(nuevo);
                }
            }
            bool activo = go.activeSelf;
            if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) != go)
            {
                go.SetActive(false); // dentro de otro prefab (Barrio): no se puede borrar desde la escena
            }
            else
            {
                Object.DestroyImmediate(go);
            }
            nuevo.SetActive(activo);
            Physics.SyncTransforms();
            sb.AppendLine(descripcion + " -> " + nuevo.name + " (" + Path.GetFileName(rutaNueva) + ") en " + nuevo.transform.position.ToString("F2")
                + " caja " + viejo.size.ToString("F2") + " -> " + (nuevo.activeInHierarchy ? Medidas(nuevo).size.ToString("F2") : "(apagado)") + "  [cambiado]");
            return true;
        }

        /// <summary>El nombre del carro nuevo: Estacionado_12_V17 → Estacionado_12_V03; el nombre del prefab → el del prefab nuevo; un nombre propio (CarroRobable_5) se queda.</summary>
        public static string NombreNuevo(string nombreViejo, string prefabViejo, string prefabNuevo)
        {
            if (nombreViejo.StartsWith(PrefijoEstacionado, StringComparison.Ordinal))
            {
                string[] partes = nombreViejo.Split('_');
                if (partes.Length >= 3)
                {
                    partes[2] = CodigoDe(prefabNuevo) ?? partes[2];
                    return string.Join("_", partes);
                }
            }
            if (nombreViejo == prefabViejo || nombreViejo.StartsWith(prefabViejo + " (", StringComparison.Ordinal))
            {
                return prefabNuevo;
            }
            return nombreViejo;
        }

        // Los valores propios del robo (calor, tinte, si ya es tuyo) pasan al carro nuevo.
        private static void CopiarEstacionado(GameObject viejo, GameObject nuevo)
        {
            CarroEstacionado de = viejo.GetComponent<CarroEstacionado>();
            CarroEstacionado a = nuevo.GetComponent<CarroEstacionado>();
            if (de == null || a == null)
            {
                return;
            }
            SerializedObject origen = new SerializedObject(de);
            SerializedObject destino = new SerializedObject(a);
            foreach (string nombre in PropiedadesEstacionado)
            {
                SerializedProperty p = origen.FindProperty(nombre);
                if (p != null && destino.FindProperty(nombre) != null)
                {
                    destino.CopyFromSerializedProperty(p);
                }
            }
            destino.ApplyModifiedPropertiesWithoutUndo();
        }

        // Alto: de las mallas visibles. Largo y ancho: de las cajas de choque (como EstacionadosTripoT1644: algunos FBX vienen en diagonal y su malla sale más grande).
        public static Bounds Medidas(GameObject objeto)
        {
            Renderer[] rs = objeto.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
            {
                return new Bounds(objeto.transform.position, Vector3.zero);
            }
            Bounds visual = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                visual.Encapsulate(r.bounds);
            }
            BoxCollider[] cajas = objeto.GetComponentsInChildren<BoxCollider>(true).Where(c => c.enabled && !c.isTrigger).ToArray();
            if (cajas.Length == 0)
            {
                return visual;
            }
            Bounds choque = CajaMundo(cajas[0]);
            foreach (BoxCollider c in cajas)
            {
                choque.Encapsulate(CajaMundo(c));
            }
            Bounds b = new Bounds();
            b.SetMinMax(new Vector3(choque.min.x, visual.min.y, choque.min.z), new Vector3(choque.max.x, visual.max.y, choque.max.z));
            return b;
        }

        // La caja de un BoxCollider en el mundo sin depender de la física (sirve en objetos apagados).
        private static Bounds CajaMundo(BoxCollider c)
        {
            Vector3 e = c.size * 0.5f;
            Bounds b = new Bounds(c.transform.TransformPoint(c.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                b.Encapsulate(c.transform.TransformPoint(c.center + esquina));
            }
            return b;
        }

        // ------------------------------------------------------------------ fotos (en una corrida aparte de Unity)

        public const string CarpetaFotos = "docs/capturas/_tareas/T45";

        /// <summary>Hoja de 3 x 2 de estacionados que eran de modelos excluidos, vistos desde la calle a la altura de una persona (1.7 m).</summary>
        public static void Fotos()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            string[] nombres =
            {
                "Estacionado_11_V02", "Estacionado_12_V03", "Estacionado_17_V20", "Estacionado_18_V21", "Estacionado_19_V09", "Estacionado_32_V14",
            };
            List<Transform> todos = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name.StartsWith(PrefijoEstacionado, StringComparison.Ordinal)).ToList();
            const int Ancho = 800;
            const int Alto = 500;
            const float AlturaOjos = 1.7f;
            const float Lejos = 9f;
            Texture2D hoja = new Texture2D(Ancho * 3, Alto * 2, TextureFormat.RGB24, false);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            GameObject camObjeto = new GameObject("CamFotosT1683");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.farClipPlane = 400f;
            StringBuilder sb = new StringBuilder("T-1683 fotos\n");
            for (int i = 0; i < nombres.Length; i++)
            {
                Transform carro = todos.FirstOrDefault(t => t.name == nombres[i]);
                sb.AppendLine(nombres[i] + ": " + (carro != null ? carro.position.ToString("F1") : "NO ESTÁ"));
                if (carro == null)
                {
                    continue;
                }
                Vector3 pos = carro.position + carro.forward * Lejos * 0.8f - carro.right * Lejos * 0.6f;
                camObjeto.transform.position = new Vector3(pos.x, carro.position.y + AlturaOjos, pos.z);
                camObjeto.transform.LookAt(carro.position + Vector3.up * 0.8f);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                hoja.ReadPixels(new Rect(0f, 0f, Ancho, Alto), (i % 3) * Ancho, (1 - i / 3) * Alto);
            }
            hoja.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Directory.CreateDirectory(CarpetaFotos);
            File.WriteAllBytes(CarpetaFotos + "/calle_estacionados.png", hoja.EncodeToPNG());
            Object.DestroyImmediate(hoja);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObjeto);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/menos-carros-T1683-fotos.txt", sb.ToString());
        }

        private static string Ruta(Transform t)
        {
            string ruta = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                ruta = p.name + "/" + ruta;
            }
            return ruta;
        }
    }
}

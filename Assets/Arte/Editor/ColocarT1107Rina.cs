using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1107: crea el prefab PandilleroArmado_Snakes (visual E10) y coloca dos riñas SNAKES contra SKULLS en la ciudad: una en la frontera de Neón Alto
    /// con el tianguis y otra dentro de Neón Alto. Sitios planos y despejados (sondeo de física) a 45 m o más de la ruta de la demo y fuera de la
    /// calle de Nova. Idempotente: rehace la raíz <see cref="Raiz"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1107Rina.Colocar
    /// </summary>
    public static class ColocarT1107Rina
    {
        public const string Raiz = "T1107_Rinas";
        public const string RutaPrefabSnakes = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Snakes.prefab";
        public const string RutaPrefabSkulls = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private const string RutaPrefabBase = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaVisualE10 = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_E10_PandilleroSnakes.prefab";
        private const string RutaDatosViborasBase = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset";
        /// <summary>Datos de los SNAKES con el visual E10: iguales a los de Viboras pero con el color base en blanco (sin tinte verde sobre la textura).</summary>
        public const string RutaDatosViboras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_ViborasE10.asset";
        private const string RutaDatosCalaveras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        /// <summary>Puntos de la ruta de la demo (casa, inventor, Doc, tianguis, hacker) y el orden en que el bot los recorre.</summary>
        public static readonly Vector3[] RutaDemo =
        {
            new Vector3(-59f, 0f, -77f), new Vector3(-44.3f, 0f, -77f), new Vector3(-100.3f, 0f, -13.4f), new Vector3(-44.3f, 0f, -77f),
            new Vector3(-36f, 0f, 85f), new Vector3(74.5f, 0f, 47f)
        };

        public const float DistanciaRuta = 45f;
        private const float LimiteZonaX = 47f;
        private const float PasoMalla = 2f;
        private const float MitadEje = 4.4f;
        private const float MitadLado = 2.6f;
        private const float AlturaSuelo = 1f;
        private const float SueloMinY = -0.1f;
        private const float SueloMaxY = 0.45f;
        private const float PlanoTolerancia = 0.08f;
        private const float DespejeBajo = 0.4f;
        private const float DespejeAlto = 2.4f;
        private const float DistLugares = 14f;
        private const float DistPandillas = 25f;
        private static List<Vector3> pandilleros = new List<Vector3>();
        private const float DistPandilleros = 8f;
        private const float DistEntreRinas = 25f;
        private const float GrosorMaximoProp = 0.9f;

        private static readonly string[] TiposLugar =
        {
            "PersonajeApoyo", "Reclutable", "MostradorTienda", "PuntoAparicion", "PuntoHistoriaMundo", "JefePandilla", "MarcaLugar",
            "MostradorPeleas", "DoctorClinica"
        };

        /// <summary>Distancia mínima (m) de cualquier punto de la huella al eje de una calle de tráfico: los carros van a 2.4 m del eje y se pide 4 m más.</summary>
        public const float DistanciaCarriles = 6.4f;
        /// <summary>Distancia mínima (m) del centro de la huella a una puerta.</summary>
        public const float DistanciaPuertas = 7f;

        private static readonly List<(Vector3 a, Vector3 b)> Calles = new List<(Vector3, Vector3)>();
        private static readonly List<Vector3> PuertasEscena = new List<Vector3>();

        /// <summary>Lee de la escena abierta las calles por las que pasan los carros (el mismo grafo que usa el tráfico, con las calles de las afueras) y las puertas.</summary>
        internal static void CargarTraficoYPuertas()
        {
            Calles.Clear();
            PuertasEscena.Clear();
            Transform manzana = ColocarS2.Todos().FirstOrDefault(t => t.name.StartsWith("Manzana_"));
            MechaCyberpunk.Trafico.Persecucion.GrafoCalles grafo = manzana != null && manzana.parent != null
                ? MechaCyberpunk.Trafico.Persecucion.GestorPersecucion.ArmarCallesDe(manzana.parent) : null;
            if (grafo != null)
            {
                MechaCyberpunk.Trafico.Calle.TraficoCalle.AlargarHaciaLasAfueras(grafo);
                for (int i = 0; i < grafo.Cantidad; i++)
                {
                    foreach (int j in grafo.Vecinos(i))
                    {
                        if (j > i)
                        {
                            Calles.Add((grafo.Nodo(i), grafo.Nodo(j)));
                        }
                    }
                }
            }
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (m is IPuerta puerta)
                {
                    PuertasEscena.Add(puerta.Posicion);
                }
            }
            ColocarS2.Informe.AppendLine("Calles de tráfico: " + Calles.Count + " tramos; puertas: " + PuertasEscena.Count);
        }

        internal static float DistanciaACalles(Vector3 p)
        {
            float menor = float.MaxValue;
            foreach ((Vector3 a, Vector3 b) in Calles)
            {
                menor = Mathf.Min(menor, DistanciaASegmento(p, a, b));
            }
            return menor;
        }

        /// <summary>True si toda la huella (9 puntos del rectángulo) queda a <see cref="DistanciaCarriles"/> de las calles de tráfico y el centro lejos de las puertas.</summary>
        internal static bool HuellaLibreDeTrafico(Vector3 centro, Vector3 eje, float mitadEje, float mitadLado)
        {
            Vector3 lado = Vector3.Cross(Vector3.up, eje);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (DistanciaACalles(centro + eje * (i * mitadEje) + lado * (j * mitadLado)) < DistanciaCarriles)
                    {
                        return false;
                    }
                }
            }
            foreach (Vector3 puerta in PuertasEscena)
            {
                if (Vector3.Distance(new Vector3(puerta.x, 0f, puerta.z), new Vector3(centro.x, 0f, centro.z)) < DistanciaPuertas)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Distancia en el plano de <paramref name="p"/> al punto o segmento más cercano de la ruta de la demo.</summary>
        public static float DistanciaALaRuta(Vector3 p)
        {
            float menor = float.MaxValue;
            for (int i = 0; i < RutaDemo.Length; i++)
            {
                menor = Mathf.Min(menor, DistanciaASegmento(p, RutaDemo[i], RutaDemo[Mathf.Min(i + 1, RutaDemo.Length - 1)]));
            }
            return menor;
        }

        public static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pp = new Vector2(p.x, p.z);
            Vector2 aa = new Vector2(a.x, a.z);
            Vector2 ab = new Vector2(b.x, b.z) - aa;
            float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / ab.sqrMagnitude);
            return Vector2.Distance(pp, aa + ab * t);
        }

        [MenuItem("Mecha Cyberpunk/T1107/Colocar riñas SNAKES contra SKULLS")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            AsegurarDatosE10();
            AsegurarPrefabSnakes();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            CargarTraficoYPuertas();
            Transform vieja = ColocarS2.Buscar(Raiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            Physics.SyncTransforms();

            GameObject snakes = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabSnakes);
            GameObject skulls = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabSkulls);
            DatosPandilleroAPie viboras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosViboras);
            DatosPandilleroAPie calaveras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosCalaveras);
            if (snakes == null || skulls == null || viboras == null || calaveras == null)
            {
                Debug.LogError("[Arte] T-1107: faltan los prefabs o los datos de SNAKES y SKULLS.");
                return;
            }

            List<Vector3> evitar = Lugares();
            pandilleros = PandillasExistentes();
            GameObject raiz = new GameObject(Raiz);
            List<Vector3> hechas = new List<Vector3>();
            // Frontera: pegada al límite (x = 47) del lado de Neón Alto; dentro: más al este.
            (string nombre, float xMin, float xMax, Vector2 preferida)[] sitios =
            {
                ("Rina_Frontera", LimiteZonaX, LimiteZonaX + 40f, new Vector2(LimiteZonaX, 0f)),
                ("Rina_NeonAlto", 85f, 118f, new Vector2(100f, 0f))
            };
            foreach ((string nombre, float xMin, float xMax, Vector2 preferida) in sitios)
            {
                (Vector3 centro, Vector3 eje)? sitio = Buscar(xMin, xMax, preferida, evitar, hechas);
                if (sitio == null)
                {
                    ColocarS2.Informe.AppendLine("FALTA un sitio para " + nombre);
                    continue;
                }
                hechas.Add(sitio.Value.centro);
                ConstructorRina.Crear(raiz.transform, nombre, sitio.Value.centro, sitio.Value.eje, snakes, viboras, IdPandilla.Viboras, skulls, calaveras,
                    IdPandilla.Calaveras);
                ColocarS2.Informe.AppendLine(nombre + " en " + sitio.Value.centro.ToString("F1") + " eje " + sitio.Value.eje + ", a " + DistanciaALaRuta(sitio.Value.centro).ToString("F0") + " m de la ruta de la demo");
            }
            ColocarS2.Guardar("t1107-rinas");
        }


        // ------------------------------------------------------------------ prefab

        /// <summary>Copia los datos de Viboras con el color base en blanco: el pandillero pinta su cuerpo con ese color y con el verde taparía la textura E10.</summary>
        public static void AsegurarDatosE10()
        {
            if (AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosViboras) == null)
            {
                AssetDatabase.CopyAsset(RutaDatosViborasBase, RutaDatosViboras);
            }
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosViboras);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("colorNormal").colorValue = Color.white;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Crea (o rehace) el prefab del SNAKES con el visual E10 y la pistola W01 en la mano, con los datos de Viboras.</summary>
        public static void AsegurarPrefabSnakes()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefabBase);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                GameObject nuevo = MechaCyberpunk.Personaje.Visual.Editor.ConectarVisualesT520.Reemplazar(contenido, viejo,
                    AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisualE10), "Modelo", false);
                PonerPistola(nuevo);
                contenido.name = "PandilleroArmado_Snakes";
                SerializedObject so = new SerializedObject(contenido.GetComponent<PandilleroAPie>());
                so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosViboras);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefabSnakes);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("prefab " + RutaPrefabSnakes + " con el visual E10");
        }

        // La misma pistola W01 y la misma pose en la mano que los SKULLS.
        private static void PonerPistola(GameObject modelo)
        {
            Transform mano = modelo.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == MechaCyberpunk.Personaje.Visual.Editor.CreadorVisualTripo.NombreSocket);
            GameObject skulls = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabSkulls);
            Transform ejemplo = skulls != null ? skulls.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == ColocarT545.NombreModelo) : null;
            if (mano == null || ejemplo == null)
            {
                Debug.LogWarning("[Arte] T-1107: el visual E10 no tiene socket de mano o los SKULLS no tienen pistola.");
                return;
            }
            Transform viejo = mano.Find(ColocarT545.NombreModelo);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject w = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ColocarT545.RutaPrefab), mano);
            w.name = ColocarT545.NombreModelo;
            w.transform.localPosition = ejemplo.localPosition;
            w.transform.localRotation = ejemplo.localRotation;
            w.transform.localScale = ejemplo.localScale;
        }

        // ------------------------------------------------------------------ sitios

        // Devuelve el centro y el eje (X o Z) de un sitio plano y despejado: calles y banquetas del tamaño de la riña, sin paredes ni props de por medio.
        private static (Vector3 centro, Vector3 eje)? Buscar(float xMin, float xMax, Vector2 preferida, List<Vector3> evitar, List<Vector3> hechas)
        {
            List<(Vector3 centro, Vector3 eje)> validos = new List<(Vector3, Vector3)>();
            for (float x = xMin; x <= xMax; x += PasoMalla)
            {
                for (float z = -90f; z <= 90f; z += PasoMalla)
                {
                    Vector3 c = new Vector3(x, 0f, z);
                    if (DistanciaALaRuta(c) < DistanciaRuta || EnZonaProhibida(c)
                        || evitar.Any(e => Vector3.Distance(new Vector3(e.x, 0f, e.z), c) < DistLugares)
                        || pandilleros.Any(e => Vector3.Distance(new Vector3(e.x, 0f, e.z), c) < DistPandilleros)
                        || hechas.Any(e => Vector3.Distance(e, c) < DistEntreRinas))
                    {
                        continue;
                    }
                    foreach (Vector3 eje in new[] { Vector3.right, Vector3.forward })
                    {
                        if (Plano(c, eje, out float y) && Despejado(new Vector3(x, y, z), eje) && HuellaLibreDeTrafico(c, eje, MitadEje, MitadLado))
                        {
                            validos.Add((new Vector3(x, y, z), eje));
                            break;
                        }
                    }
                }
            }
            ColocarS2.Informe.AppendLine("Sitios válidos entre x=" + xMin + " y " + xMax + ": " + validos.Count);
            if (validos.Count == 0)
            {
                return null;
            }
            return validos.OrderBy(v => Vector2.Distance(new Vector2(v.centro.x, v.centro.z), preferida)).First();
        }

        // Piso plano en toda la huella (la calle sirve: es el espacio libre de las manzanas).
        private static bool Plano(Vector3 centro, Vector3 eje, out float y)
        {
            y = 0f;
            if (!Physics.Raycast(new Vector3(centro.x, AlturaSuelo, centro.z), Vector3.down, out RaycastHit h0, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            y = h0.point.y;
            if (y < SueloMinY || y > SueloMaxY)
            {
                return false;
            }
            Vector3 lado = Vector3.Cross(Vector3.up, eje);
            for (float de = -MitadEje; de <= MitadEje + 0.01f; de += MitadEje / 2f)
            {
                for (float dl = -MitadLado; dl <= MitadLado + 0.01f; dl += MitadLado / 2f)
                {
                    Vector3 o = centro + eje * de + lado * dl;
                    if (!Physics.Raycast(new Vector3(o.x, AlturaSuelo, o.z), Vector3.down, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore)
                        || Mathf.Abs(h.point.y - y) > PlanoTolerancia || h.normal.y < 0.95f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Despejado: nada sólido en el núcleo (donde se paran los dos bandos) y, en el resto de la huella, solo cosas delgadas (postes, bolardos, letreros).
        internal static bool Despejado(Vector3 centro, Vector3 eje, float mitadEje, float mitadLado, float nucleoEje, float nucleoLado)
        {
            Quaternion giro = Quaternion.LookRotation(eje, Vector3.up);
            float alto = (DespejeAlto - DespejeBajo) * 0.5f;
            Vector3 c = new Vector3(centro.x, centro.y + (DespejeBajo + DespejeAlto) * 0.5f, centro.z);
            if (Physics.CheckBox(c, new Vector3(nucleoLado, alto, nucleoEje), giro, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            foreach (Collider col in Physics.OverlapBox(c, new Vector3(mitadLado, alto, mitadEje), giro, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 t = col.bounds.size;
                if (t.x > GrosorMaximoProp || t.z > GrosorMaximoProp)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool Despejado(Vector3 centro, Vector3 eje) => Despejado(centro, eje, MitadEje, MitadLado, 3f, 1.8f);

        /// <summary>
        /// Lo que otras pruebas de la ciudad exigen libre: la calle de Nova (arroyo de z = -43, x de 54 a 134) y las salidas del borde (franja de z = 0 hacia
        /// x = +-120). Ahí no se coloca nada.
        /// </summary>
        internal static bool EnZonaProhibida(Vector3 p)
        {
            bool calleNova = p.x >= ColocarS2cNova.X0 - 12f && p.x <= ColocarS2cNova.X1 + 12f && Mathf.Abs(p.z - ColocarS2cNova.ZCentro) <= 14f;
            bool salidaEste = p.x >= 110f && Mathf.Abs(p.z) <= 8f;
            // La calle de los suburbios (z = -43, de x = -39 a 39) también se cruza en una prueba.
            bool calleSuburbios = p.x >= -50f && p.x <= 50f && Mathf.Abs(p.z - ColocarS2cCalle.ZCentro) <= 14f;
            return calleNova || salidaEste || calleSuburbios;
        }

        internal static List<Vector3> Lugares()
        {
            List<Vector3> puntos = new List<Vector3>();
            foreach (Transform t in ColocarS2.Todos())
            {
                if (t.name.StartsWith("Lugar_") || t.GetComponents<MonoBehaviour>().Any(m => m != null && TiposLugar.Contains(m.GetType().Name)))
                {
                    puntos.Add(t.position);
                }
            }
            return puntos;
        }

        internal static List<Vector3> PandillasExistentes()
        {
            List<Vector3> puntos = new List<Vector3>();
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                puntos.Add(p.transform.position);
            }
            return puntos;
        }
    }
}

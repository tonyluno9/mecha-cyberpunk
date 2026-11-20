using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1513: cambia la apariencia del muro de la ciudad (T530_Afueras/Orillas/Muro, cajas provisionales) por los modelos MU de
    /// Assets/Arte/Zonas/MU/. El trazo y el alto se conservan: los tramos MU01 siguen cada segmento viejo, hay torres MU02 en las
    /// esquinas y a los lados de cada salida, y una puerta MU03 en cada hueco (sin colliders: el paso sigue abierto).
    /// El muro viejo no se borra: solo se apagan sus MeshRenderer, y sus BoxCollider y los LimiteInvisible siguen cerrando la ciudad.
    /// Las piezas nuevas no llevan colliders (el muro viejo ya los tiene), así que el comportamiento físico no cambia.
    /// Idempotente: borra y rehace solo la raíz T1513_MuroCiudad.
    /// Orden en batchmode:
    ///   1. -executeMethod MechaCyberpunk.Mundo.Editor.MuroCiudadT1513.Aplicar   -> cambia la escena, la guarda y escribe Logs/t1513-muro.txt
    ///   2. -executeMethod MechaCyberpunk.Mundo.Editor.FotosT1513.Tomar          -> docs/capturas/_20nov/muro_*.png
    /// </summary>
    public static class MuroCiudadT1513
    {
        public const string NombreRaiz = "T1513_MuroCiudad";
        public const string RutaLog = "Logs/t1513-muro.txt";
        public const string RutaOrillas = "T530_Afueras/Orillas";
        public const string CarpetaMu = "Assets/Arte/Zonas/MU/";
        public const string PrefabA = CarpetaMu + "MU01_MuroPerimetral_a.prefab";
        public const string PrefabB = CarpetaMu + "MU01_MuroPerimetral_b.prefab";
        public const string PrefabTorre = CarpetaMu + "MU02_TorreControl_a.prefab";
        public const string PrefabPuerta = CarpetaMu + "MU03_PuertaMuro_a.prefab";
        private const float AltoRayo = 4f;
        private const float AlcanceRayo = 40f;

        [MenuItem("Mecha Cyberpunk/T-1513 Muro de la ciudad con MU")]
        public static void Aplicar()
        {
            Scene escena = ReemplazarEdificiosT1502.Abrir();
            GameObject a = Cargar(PrefabA);
            GameObject b = Cargar(PrefabB);
            GameObject torre = Cargar(PrefabTorre);
            GameObject puerta = Cargar(PrefabPuerta);

            GameObject viejoRaiz = GameObject.Find(NombreRaiz);
            if (viejoRaiz != null)
            {
                Object.DestroyImmediate(viejoRaiz);
            }
            List<Transform> muros = MurosViejos();
            if (muros.Count == 0)
            {
                throw new System.InvalidOperationException("[Mundo] T-1513: no hay muros en " + RutaOrillas);
            }
            float alto = muros.Max(m => m.lossyScale.y);
            List<CalculoMuroT1513.Segmento> segmentos = muros.Select(Segmento).ToList();

            GameObject raiz = new GameObject(NombreRaiz);
            SceneManager.MoveGameObjectToScene(raiz, escena);
            Physics.SyncTransforms();

            // Medidas de cada modelo, ya a la escala de altura del muro viejo.
            Vector3 tamA = Medir(a);
            Vector3 tamB = Medir(b);
            Vector3 tamTorre = Medir(torre);
            Vector3 tamPuerta = Medir(puerta);
            float escalaA = alto / tamA.y;
            float escalaB = alto / tamB.y;
            float escalaPuerta = alto / tamPuerta.y;
            float largoA = Mathf.Max(tamA.x, tamA.z) * escalaA;
            float largoB = Mathf.Max(tamB.x, tamB.z) * escalaB;

            CalculoMuroT1513.Plan plan = CalculoMuroT1513.Planear(segmentos, new Vector2(tamTorre.x, tamTorre.z));
            Vector3 centroCiudad = Vector3.zero;
            foreach (CalculoMuroT1513.Segmento s in segmentos)
            {
                centroCiudad += (s.A + s.B) * 0.5f;
            }
            centroCiudad /= segmentos.Count;

            int piezas = 0;
            int fueraDeAjuste = 0;
            int tramoNum = 0;
            foreach (CalculoMuroT1513.Tramo t in plan.Tramos)
            {
                tramoNum++;
                if (t.Largo < 0.5f)
                {
                    continue;
                }
                CalculoMuroT1513.Reparto r = CalculoMuroT1513.Repartir(t.Largo, largoA, largoB);
                if (!r.DentroDelAjuste)
                {
                    fueraDeAjuste++;
                }
                float recorrido = 0f;
                for (int k = 0; k < r.EsB.Count; k++)
                {
                    bool esB = r.EsB[k];
                    float largoPieza = (esB ? largoB : largoA) * r.Factor;
                    Vector3 centro = t.Inicio + t.Dir * (recorrido + largoPieza * 0.5f);
                    recorrido += largoPieza;
                    GameObject g = Colocar(esB ? b : a, raiz.transform, "Muro_" + tramoNum + "_" + k + (esB ? "b" : "a"),
                        centro, t.Dir, esB ? escalaB : escalaA, r.Factor, true, 0f);
                    if (g != null)
                    {
                        piezas++;
                    }
                }
            }

            int torres = 0;
            foreach (CalculoMuroT1513.Torre t in plan.Torres)
            {
                Colocar(torre, raiz.transform, (t.Esquina ? "TorreEsquina_" : "TorreSalida_") + torres, t.Centro, Vector3.forward, 1f, 1f, false, 0f);
                torres++;
            }
            int puertas = 0;
            foreach (CalculoMuroT1513.Puerta p in plan.Puertas)
            {
                Colocar(puerta, raiz.transform, "Puerta_" + puertas, p.Centro, p.Dir, escalaPuerta, 1f, true, 0f);
                puertas++;
            }

            // Apaga solo la apariencia del muro provisional: colliders y objetos se quedan para poder volver.
            int apagados = 0;
            foreach (Transform m in muros)
            {
                foreach (Renderer rd in m.GetComponents<Renderer>())
                {
                    if (rd.enabled)
                    {
                        rd.enabled = false;
                        apagados++;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1513 muro de la ciudad con MU (" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")");
            sb.AppendLine("Segmentos viejos: " + muros.Count + ", alto del muro viejo " + alto.ToString("F2") + " m");
            sb.AppendLine("Tramos MU01: " + piezas + " segmentos (" + fueraDeAjuste + " tramos fuera de +-15 % de largo)");
            sb.AppendLine("Torres MU02: " + torres + " (esquinas " + plan.Torres.Count(x => x.Esquina) + ", a los lados de salidas " + plan.Torres.Count(x => !x.Esquina) + ")");
            sb.AppendLine("Puertas MU03: " + puertas + " (sin colliders, el paso sigue abierto)");
            sb.AppendLine("Renderers del muro viejo apagados: " + apagados + " (colliders y objetos intactos)");
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, sb.ToString());

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[Mundo] T-1513: " + piezas + " segmentos, " + torres + " torres, " + puertas + " puertas; ver " + RutaLog);
        }

        /// <summary>Los muros provisionales de las orillas (hijos directos de T530_Afueras/Orillas llamados Muro).</summary>
        public static List<Transform> MurosViejos()
        {
            GameObject orillas = GameObject.Find(RutaOrillas);
            List<Transform> lista = new List<Transform>();
            if (orillas == null)
            {
                return lista;
            }
            foreach (Transform hijo in orillas.transform)
            {
                if (hijo.name == "Muro")
                {
                    lista.Add(hijo);
                }
            }
            return lista;
        }

        /// <summary>Segmento en el plano del suelo: el muro viejo mide lossyScale.z de largo a lo largo de su forward.</summary>
        public static CalculoMuroT1513.Segmento Segmento(Transform muro)
        {
            Vector3 f = muro.forward;
            f.y = 0f;
            f.Normalize();
            Vector3 c = muro.position;
            c.y = 0f;
            float mitad = muro.lossyScale.z * 0.5f;
            return new CalculoMuroT1513.Segmento { A = c - f * mitad, B = c + f * mitad };
        }

        internal static GameObject Cargar(string ruta)
        {
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (g == null)
            {
                throw new System.InvalidOperationException("[Mundo] T-1513: falta el prefab " + ruta);
            }
            return g;
        }

        /// <summary>Tamaño de los renderers del prefab tal como viene (instancia temporal en el origen).</summary>
        internal static Vector3 Medir(GameObject prefab)
        {
            GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tmp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Bounds b = Limites(tmp);
            Object.DestroyImmediate(tmp);
            return b.size;
        }

        private static Bounds Limites(GameObject g)
        {
            Renderer[] rs = g.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        /// <summary>
        /// Instancia una pieza con su largo a lo largo de dir (el eje horizontal más largo del modelo), con la escala uniforme
        /// de altura y el factor de largo, centrada en centro (xz) y con su base en el piso.
        /// </summary>
        internal static GameObject Colocar(GameObject prefab, Transform padre, string nombre, Vector3 centro, Vector3 dir,
            float escalaUniforme, float factorLargo, bool alinear, float giroExtra)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            g.name = nombre;
            Transform tr = g.transform;
            Vector3 escalaOriginal = tr.localScale;
            Quaternion rotOriginal = tr.rotation;
            tr.SetPositionAndRotation(Vector3.zero, rotOriginal);
            Bounds b0 = Limites(g);
            bool largoEnX = b0.size.x >= b0.size.z;
            Vector3 eje = largoEnX ? Vector3.right : Vector3.forward;
            // La torre no se gira: el plan cuenta con su tamaño sin girar.
            float yaw = alinear ? Vector3.SignedAngle(eje, dir, Vector3.up) : 0f;
            tr.rotation = Quaternion.AngleAxis(yaw + giroExtra, Vector3.up) * rotOriginal;
            Vector3 escala = escalaOriginal * escalaUniforme;
            if (alinear)
            {
                // El largo del modelo es su eje más largo: se estira solo ese eje del objeto.
                if (largoEnX)
                {
                    escala.x *= factorLargo;
                }
                else
                {
                    escala.z *= factorLargo;
                }
            }
            tr.localScale = escala;
            tr.position = Vector3.zero;

            // Asentado: centro en xz y base en el piso (raycast hacia abajo, sin contar el muro viejo ni las piezas nuevas).
            Bounds b = Limites(g);
            float piso = Piso(centro, padre);
            Vector3 mover = new Vector3(centro.x - b.center.x, piso - b.min.y, centro.z - b.center.z);
            tr.position = mover;

            foreach (Collider c in g.GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }
            return g;
        }

        private static float Piso(Vector3 centro, Transform raizNueva)
        {
            Physics.SyncTransforms();
            Vector3 origen = new Vector3(centro.x, AltoRayo, centro.z);
            RaycastHit[] golpes = Physics.RaycastAll(origen, Vector3.down, AlcanceRayo, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            float y = 0f;
            foreach (RaycastHit h in golpes)
            {
                if (h.collider.transform.IsChildOf(raizNueva) || h.collider.name == "Muro" || h.collider.name == "LimiteInvisible" || h.normal.y < 0.5f)
                {
                    continue;
                }
                if (h.distance < mejor)
                {
                    mejor = h.distance;
                    y = h.point.y;
                }
            }
            return y;
        }
    }
}

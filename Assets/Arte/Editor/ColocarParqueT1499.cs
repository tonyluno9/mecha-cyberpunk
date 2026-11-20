using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1499: la plaza K09 de Ciudad_Prologo con piezas reales (kiosco PZ02, bancas PZ01, jardineras PZ03, árboles AR01/AR02) en vez de las primitivas
    /// de ColocarT1320Calles. Las primitivas viejas (Kiosco, Arbol, Banca y los Foquitos que colgaban del kiosco viejo) se apagan, no se borran.
    /// Cada pieza baja al piso con un raycast; si choca con otro objeto de la plaza o no hay piso, no se pone. Idempotente: rehace el grupo
    /// <see cref="Grupo"/>. Si se corre ColocarT1320Calles.Aplicar otra vez hay que volver a correr este método.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarParqueT1499.Aplicar
    /// </summary>
    public static class ColocarParqueT1499
    {
        public const string Grupo = "Parque_T1499";
        public const string RutaPlaza = "Plaza_K09";
        /// <summary>Hijos de Plaza_K09 (primitivas de T-1320) que se apagan.</summary>
        public static readonly string[] ApagarNombres = { "Kiosco", "Arbol", "Banca", "Foquitos" };

        private static readonly Vector3 Centro = ColocarT1320Calles.CentroKiosco;
        private const float RadioBancas = 4.8f;
        private const float AlturaRayo = 30f;
        private const float AlturaMaximaPiso = 3f;
        private const float AlturaMinimaEstorbo = 0.3f;
        private const float EncogerCaja = 0.05f;
        private const float LadoMaximoEstorbo = 30f;

        private sealed class Pieza
        {
            public string ruta;
            public Vector3 posicion;
            public float giro;
        }

        public static readonly List<string> Informe = new List<string>();

        [MenuItem("Mecha Cyberpunk/T-1499/Colocar parque en K09")]
        public static void Aplicar()
        {
            Informe.Clear();
            Scene escena = SceneManager.GetActiveScene();
            if (escena.path != ColocarS2.RutaEscena)
            {
                escena = EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            }
            Transform plaza = BuscarPlaza(escena);
            if (plaza == null)
            {
                Debug.LogError("[Arte] T-1499: no hay " + ColocarT1320Calles.Raiz + "/" + RutaPlaza + " en la escena");
                return;
            }
            Transform viejo = plaza.Find(Grupo);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            foreach (Transform hijo in plaza.Cast<Transform>().ToArray())
            {
                if (ApagarNombres.Contains(hijo.name))
                {
                    hijo.gameObject.SetActive(false);
                }
            }
            Physics.SyncTransforms();

            Transform grupo = new GameObject(Grupo).transform;
            grupo.SetParent(plaza, false);
            foreach (Pieza p in Piezas())
            {
                Poner(grupo, p);
            }
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[Arte] T-1499 parque K09:\n" + string.Join("\n", Informe));
        }

        public static Transform BuscarPlaza(Scene escena)
        {
            GameObject raiz = escena.GetRootGameObjects().FirstOrDefault(g => g.name == ColocarT1320Calles.Raiz);
            return raiz == null ? null : raiz.transform.Find(RutaPlaza);
        }

        private static IEnumerable<Pieza> Piezas()
        {
            yield return new Pieza { ruta = PlazasT1341.RutaKiosco, posicion = Centro, giro = PlazasT1341.GiroKiosco };
            for (int i = 0; i < 4; i++)
            {
                float rad = (45f + 90f * i) * Mathf.Deg2Rad;
                Vector3 p = Centro + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * RadioBancas;
                yield return new Pieza { ruta = PlazasT1341.RutaBanca, posicion = p, giro = PlazasT1341.YawBancaHacia(p, Centro) };
            }
            // Jardineras a los lados de la entrada que da a la calle K01 (el eje del kiosco queda libre).
            yield return new Pieza { ruta = PlazasT1341.RutaJardinera, posicion = new Vector3(-79.5f, 0f, -35.9f), giro = 0f };
            yield return new Pieza { ruta = PlazasT1341.RutaJardinera, posicion = new Vector3(-71.5f, 0f, -35.9f), giro = 0f };
            yield return new Pieza { ruta = PlazasT1341.RutaArbolGrande, posicion = new Vector3(-83.5f, 0f, -22.5f), giro = 0f };
            foreach (Vector3 p in new[]
            {
                new Vector3(-85.5f, 0f, -35.5f), new Vector3(-69.3f, 0f, -28.5f), new Vector3(-67.8f, 0f, -35.5f), new Vector3(-75.5f, 0f, -20.2f),
                new Vector3(-71f, 0f, -20.5f), new Vector3(-86f, 0f, -28f),
            })
            {
                yield return new Pieza { ruta = PlazasT1341.RutaArbolChico, posicion = p, giro = 0f };
            }
        }

        private static void Poner(Transform grupo, Pieza p)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p.ruta);
            if (prefab == null)
            {
                Informe.Add("FALTA " + p.ruta);
                return;
            }
            if (!BuscarPiso(p.posicion, grupo, out float y))
            {
                Informe.Add("sin piso, no se pone " + p.ruta + " en " + p.posicion);
                return;
            }
            GameObject o = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
            o.transform.SetPositionAndRotation(new Vector3(p.posicion.x, y, p.posicion.z), Quaternion.Euler(0f, p.giro, 0f));
            Physics.SyncTransforms();
            string choque = ChocaCon(o);
            if (choque != null)
            {
                Informe.Add("choca con " + choque + ", no se pone " + o.name + " en " + p.posicion);
                Object.DestroyImmediate(o);
                return;
            }
            Informe.Add("ok " + o.name + " en " + o.transform.position.ToString("F2"));
        }

        /// <summary>El piso bajo (x, z): el golpe más alto con superficie casi horizontal y cerca del nivel de la calle, sin contar lo que cuelga de <paramref name="ignorar"/> (el parque).</summary>
        public static bool BuscarPiso(Vector3 p, Transform ignorar, out float y)
        {
            y = 0f;
            bool hay = false;
            foreach (RaycastHit h in Physics.RaycastAll(new Vector3(p.x, AlturaRayo, p.z), Vector3.down, AlturaRayo + 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.normal.y < 0.7f || h.point.y > AlturaMaximaPiso || (ignorar != null && h.collider.transform.IsChildOf(ignorar)))
                {
                    continue;
                }
                if (!hay || h.point.y > y)
                {
                    y = h.point.y;
                    hay = true;
                }
            }
            return hay;
        }

        /// <summary>Nombre del primer collider ajeno (o de otra pieza) que se encima con los colliders de <paramref name="pieza"/> y se alza del piso; null si nada.</summary>
        public static string ChocaCon(GameObject pieza)
        {
            foreach (Collider c in pieza.GetComponentsInChildren<Collider>())
            {
                Bounds b = c.bounds;
                if (b.max.y - pieza.transform.position.y < AlturaMinimaEstorbo)
                {
                    continue;
                }
                Vector3 extension = b.extents - new Vector3(EncogerCaja, 0f, EncogerCaja);
                foreach (Collider otro in Physics.OverlapBox(b.center, extension, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (otro.transform.IsChildOf(pieza.transform) || !otro.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    Bounds ob = otro.bounds;
                    if (ob.max.y - pieza.transform.position.y < AlturaMinimaEstorbo || ob.size.x > LadoMaximoEstorbo || ob.size.z > LadoMaximoEstorbo)
                    {
                        continue; // el piso y las losas
                    }
                    return otro.name;
                }
            }
            return null;
        }
    }
}

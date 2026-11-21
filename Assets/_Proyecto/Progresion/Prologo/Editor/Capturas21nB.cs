using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Ayudas para comprobar en Play lo del 21-nov B (alturas, robots Nova, locomoción, vehículos). Se llaman con `unity command eval`
    /// mientras el juego corre: ponen prefabs en fila en un punto abierto y los dejan en el estado a revisar. Usa reflexión para no
    /// depender de los ensamblados de cada área.
    /// </summary>
    public static class Capturas21nB
    {
        private const string Visuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";
        private const string RaizNombre = "Capturas21nB";

        /// <summary>Borra lo que pusieron las llamadas anteriores.</summary>
        public static string Limpiar()
        {
            GameObject r = GameObject.Find(RaizNombre);
            if (r != null)
            {
                Object.Destroy(r);
            }
            return "limpio";
        }

        private static Transform Raiz()
        {
            GameObject r = GameObject.Find(RaizNombre);
            return (r != null ? r : new GameObject(RaizNombre)).transform;
        }

        private static GameObject Poner(string ruta, Vector3 pos, float yaw)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                return null;
            }
            GameObject go = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f), Raiz());
            go.name = prefab.name;
            // Sin padre que se mueva: la velocidad se escribe a mano.
            Fijar(go, "LeerDelPadre", false);
            return go;
        }

        private static void Fijar(GameObject go, string propiedad, object valor)
        {
            foreach (MonoBehaviour m in go.GetComponentsInChildren<MonoBehaviour>())
            {
                var p = m.GetType().GetProperty(propiedad);
                if (p != null && p.CanWrite)
                {
                    p.SetValue(m, valor);
                    return;
                }
            }
        }

        private static float Alto(GameObject go)
        {
            bool hay = false;
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            foreach (SkinnedMeshRenderer r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Mesh m = new Mesh();
                r.BakeMesh(m, true);
                foreach (Vector3 v in m.vertices)
                {
                    Vector3 w = r.transform.localToWorldMatrix.MultiplyPoint3x4(v);
                    if (!hay) { b = new Bounds(w, Vector3.zero); hay = true; } else { b.Encapsulate(w); }
                }
                Object.Destroy(m);
            }
            return hay ? b.size.y : 0f;
        }

        /// <summary>Fila de alturas a lo largo de +x desde (x, z), mirando a -z. Devuelve la altura animada medida de cada uno.</summary>
        public static string FilaAlturas(float x, float y, float z)
        {
            string[] nombres = { "Visual_NP12_Abuela_a", "Visual_PJ01", "Visual_NP10_Elotero_a", "Visual_Inventor", "Visual_Doctor", "Visual_woman2", "Visual_E15_CentinelaNova_a", "Visual_E11_JefeSnakes", "Visual_E12_GuardiaPesadoNova", "Visual_E18_EliteNova_a", "Visual_E14_JefeFinalNova", "Visual_E16_RobotPesadoNova_a" };
            StringBuilder sb = new StringBuilder();
            float paso = 1.6f;
            for (int i = 0; i < nombres.Length; i++)
            {
                float ancho = i >= 10 ? 1.6f : 0f;
                GameObject go = Poner(Visuales + nombres[i] + ".prefab", new Vector3(x + i * paso + ancho * (i - 9), y, z), 180f);
                sb.Append(nombres[i].Replace("Visual_", "")).Append(go == null ? " (no hay)" : "").Append("; ");
            }
            return sb.ToString();
        }

        /// <summary>Mide la altura animada de lo que hay en la fila (llamar 1–2 s después de <see cref="FilaAlturas"/>).</summary>
        public static string MedirFila()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Transform t in Raiz())
            {
                sb.Append(t.name.Replace("Visual_", "")).Append(' ').Append(Alto(t.gameObject).ToString("F2")).Append("; ");
            }
            return sb.ToString();
        }

        /// <summary>Robots Nova: E16 caminando con el escudo arriba, E16 sin escudo, E18 caminando, E18 golpeando, E15 caminando con la W10.</summary>
        public static string Robots(float x, float y, float z)
        {
            GameObject e16 = Poner(Visuales + "Visual_E16_RobotPesadoNova_a.prefab", new Vector3(x, y, z), 180f);
            GameObject e16b = Poner(Visuales + "Visual_E16_RobotPesadoNova_a.prefab", new Vector3(x + 4.5f, y, z), 180f);
            GameObject e18 = Poner(Visuales + "Visual_E18_EliteNova_a.prefab", new Vector3(x + 8.5f, y, z), 180f);
            GameObject e18g = Poner(Visuales + "Visual_E18_EliteNova_a.prefab", new Vector3(x + 11f, y, z), 180f);
            GameObject e15 = Poner(Visuales + "Visual_E15_CentinelaNova_a.prefab", new Vector3(x + 13.5f, y, z), 180f);
            Fijar(e16, "EscudoArriba", true);
            Fijar(e16, "Velocidad", 1.1f);
            Fijar(e16b, "Velocidad", 1.1f);
            Fijar(e18, "Velocidad", 1.62f);
            Fijar(e15, "Velocidad", 1.1f);
            foreach (MonoBehaviour m in e18g.GetComponentsInChildren<MonoBehaviour>())
            {
                var g = m.GetType().GetMethod("Golpear", new[] { typeof(bool) });
                if (g != null) { g.Invoke(m, new object[] { false }); break; }
            }
            return "E16 escudo, E16, E18 camina, E18 golpe, E15";
        }

        /// <summary>PJ01 en varios estados del controlador, en fila: caminar, trote, correr rápido y los del salto.</summary>
        public static string Locomocion(float x, float y, float z)
        {
            (string estado, float vel)[] filas =
            {
                ("", 1.62f), ("", 4.53f), ("", 6.25f), ("SaltoInicio", 0f), ("SaltoAire", 0f), ("SaltoCorrerAire", 0f), ("SaltoCorrerRodada", 0f), ("AterrizajeFuerte", 0f),
            };
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < filas.Length; i++)
            {
                GameObject go = Poner(Visuales + "Visual_PJ01.prefab", new Vector3(x + i * 1.7f, y, z), 90f);
                Fijar(go, "Velocidad", filas[i].vel);
                Animator a = go.GetComponentInChildren<Animator>();
                if (!string.IsNullOrEmpty(filas[i].estado) && a != null)
                {
                    int h = Animator.StringToHash(filas[i].estado);
                    bool hay = a.HasState(0, h);
                    if (hay) { a.Play(h, 0, 0.45f); }
                    sb.Append(filas[i].estado).Append(hay ? "" : " (no existe)").Append("; ");
                }
                else
                {
                    sb.Append("vel ").Append(filas[i].vel).Append("; ");
                }
            }
            return sb.ToString();
        }

        /// <summary>Vehículos: PA01, PA02 y V22 con el cofre abierto y V16–V19 junto a un PJ01 para ver el tamaño; un dron DR01 arriba.</summary>
        public static string Vehiculos(float x, float y, float z)
        {
            string[] cofre = { "Carro_PA01", "Carro_PA02", "Carro_V22" };
            string[] grandes = { "Carro_V16", "Carro_V17", "Carro_V18", "Carro_V19" };
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < cofre.Length; i++)
            {
                GameObject c = Poner("Assets/_Proyecto/Vehiculos/Prefabs/" + cofre[i] + ".prefab", new Vector3(x + i * 4.5f, y, z), 200f);
                if (c == null) { sb.Append(cofre[i]).Append(" no hay; "); continue; }
                foreach (MonoBehaviour m in c.GetComponentsInChildren<MonoBehaviour>())
                {
                    var g = m.GetType().GetMethod("AbrirCofre");
                    if (g != null) { g.Invoke(m, null); sb.Append(cofre[i]).Append(" cofre abierto; "); break; }
                }
            }
            for (int i = 0; i < grandes.Length; i++)
            {
                GameObject c = Poner("Assets/_Proyecto/Vehiculos/Prefabs/" + grandes[i] + ".prefab", new Vector3(x + i * 6f, y, z + 9f), 180f);
                Poner(Visuales + "Visual_PJ01.prefab", new Vector3(x + i * 6f + 2.6f, y, z + 9f), 180f);
                sb.Append(grandes[i]).Append(c == null ? " no hay" : "").Append("; ");
            }
            GameObject d = Poner("Assets/Arte/Vehiculos/Drones/Prefabs/Dron_DR01.prefab", new Vector3(x + 4f, y + 2.2f, z - 3f), 160f);
            sb.Append(d == null ? "sin dron" : "dron");
            return sb.ToString();
        }
    }
}

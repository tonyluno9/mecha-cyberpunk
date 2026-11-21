using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1526: herramientas de editor del tráfico dentro del muro y de los modelos de policía en la ciudad.
    /// <c>Diagnostico</c> no cambia nada: escribe Logs/t1526-trafico.txt con el área del muro, cuántos nodos de calle caen fuera (las salidas que se cortan), las calles que terminan
    /// en una salida y los carros de la escena que son modelos de policía con componentes de carro civil (CarroEstacionado o CarroTrafico).
    /// <c>Aplicar</c> (idempotente) cambia cada modelo de policía suelto de la escena por un civil equivalente (V05 por V02, V06 por V03, V08 por C19, V11 por V12 (T-1554: V10 se descartó), PA01 por V02,
    /// PA02 por V03) en el mismo lugar, y guarda la escena. Las patrullas de verdad (Patrulla.prefab y PatrullaCamioneta.prefab con PatrullaMundo) no se tocan.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.TraficoDentroDelMuroT1526.Diagnostico   y   ...Aplicar
    /// </summary>
    public static class TraficoDentroDelMuroT1526
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaLog = "Logs/t1526-trafico.txt";
        private const string CarpetaCarros = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_";

        /// <summary>Modelo de policía suelto -> civil que lo reemplaza.</summary>
        public static readonly Dictionary<string, string> Reemplazos = new Dictionary<string, string>
        {
            { "V05", "V02" }, { "V06", "V03" }, { "V08", "C19" }, { "V11", "V12" }, { "PA01", "V02" }, { "PA02", "V03" },
        };

        [MenuItem("Mecha Cyberpunk/T-1526/Diagnóstico del tráfico dentro del muro")]
        public static void Diagnostico()
        {
            Scene escena = Abrir();
            StringBuilder sb = Informe(escena);
            sb.AppendLine("(diagnóstico: no se cambió nada)");
            Escribir(sb);
        }

        [MenuItem("Mecha Cyberpunk/T-1526/Aplicar modelos de policía fuera del tráfico civil")]
        public static void Aplicar()
        {
            Scene escena = Abrir();
            StringBuilder sb = Informe(escena);
            int cambiados = 0;
            int saltados = 0;
            foreach (GameObject go in ModelosDePoliciaSueltos(escena))
            {
                string codigo = CodigoDe(go);
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go) || !Reemplazos.TryGetValue(codigo, out string civil))
                {
                    saltados++;
                    sb.AppendLine("SALTADO (no es raíz de prefab suelta): " + RutaDe(go.transform));
                    continue;
                }
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaCarros + civil + ".prefab");
                if (prefab == null)
                {
                    saltados++;
                    sb.AppendLine("SALTADO (falta Carro_" + civil + ".prefab): " + RutaDe(go.transform));
                    continue;
                }
                Transform t = go.transform;
                GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                nuevo.transform.SetParent(t.parent, false);
                nuevo.transform.SetPositionAndRotation(t.position, t.rotation);
                nuevo.transform.SetSiblingIndex(t.GetSiblingIndex());
                nuevo.name = go.name.Replace(codigo, civil);
                sb.AppendLine("CAMBIADO " + go.name + " (" + codigo + ") -> " + nuevo.name + " (" + civil + ") en " + t.position);
                Object.DestroyImmediate(go);
                cambiados++;
            }
            sb.AppendLine("Cambiados: " + cambiados + ", saltados: " + saltados);
            if (cambiados > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
            }
            Escribir(sb);
        }

        private static Scene Abrir()
        {
            Scene activa = SceneManager.GetActiveScene();
            return activa.path == RutaEscena && activa.isLoaded ? activa : EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        }

        private static void Escribir(StringBuilder sb)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaLog, sb.ToString());
            Debug.Log("[T-1526] " + sb);
        }

        private static StringBuilder Informe(Scene escena)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1526 tráfico dentro del muro (" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")");
            AreaMuro.Definir(new Rect());
            Rect? area = AreaMuro.Actual();
            sb.AppendLine(area.HasValue ? "Área del muro (x, z): " + area.Value + " (centro " + area.Value.center + ")" : "SIN ÁREA: no encontré piezas MU01/MU02/MU03 ni muro viejo; el tráfico no se recorta.");
            GestorPersecucion gestor = Object.FindFirstObjectByType<GestorPersecucion>(FindObjectsInactive.Include);
            if (gestor != null && area.HasValue)
            {
                GrafoCalles grafo = GestorPersecucion.ArmarCallesDe(gestor.transform.parent != null ? gestor.transform.parent : gestor.transform);
                if (grafo != null)
                {
                    TraficoCalle.AlargarHaciaLasAfueras(grafo);
                    int antes = grafo.Cantidad;
                    int salidas = 0;
                    for (int i = 0; i < grafo.Cantidad; i++)
                    {
                        // Un nodo de adentro con un vecino afuera es una calle que llegaba a una salida: ahora termina ahí y da vuelta.
                        Vector3 p = grafo.Nodo(i);
                        if (!area.Value.Contains(new Vector2(p.x, p.z)))
                        {
                            continue;
                        }
                        foreach (int v in grafo.Vecinos(i))
                        {
                            Vector3 q = grafo.Nodo(v);
                            if (!area.Value.Contains(new Vector2(q.x, q.z)))
                            {
                                salidas++;
                                sb.AppendLine("Calle a una salida del muro: nodo " + i + " en " + p + " -> " + q);
                                break;
                            }
                        }
                    }
                    int quitados = grafo.Recortar(area.Value);
                    sb.AppendLine("Nodos del grafo: " + antes + ", fuera del muro y desconectados: " + quitados + ", calles que llegaban a una salida: " + salidas);
                }
            }
            else if (gestor == null)
            {
                sb.AppendLine("No hay GestorPersecucion en la escena (sin grafo de calles).");
            }
            int n = 0;
            foreach (GameObject go in ModelosDePoliciaSueltos(escena))
            {
                n++;
                sb.AppendLine("Modelo de policía como carro civil: " + RutaDe(go.transform) + " (" + CodigoDe(go) + ", " + (go.GetComponent<CarroTrafico>() != null ? "CarroTrafico" : "CarroEstacionado") + ")");
            }
            sb.AppendLine("Modelos de policía sueltos con componentes civiles: " + n);
            return sb;
        }

        // Raíces de prefab de la escena cuyo origen es un Carro_<modelo de policía>, con CarroEstacionado o CarroTrafico y sin PatrullaMundo.
        private static List<GameObject> ModelosDePoliciaSueltos(Scene escena)
        {
            List<GameObject> lista = new List<GameObject>();
            foreach (CarroEstacionado carro in Object.FindObjectsByType<CarroEstacionado>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (carro.gameObject.scene != escena || carro.GetComponent<PatrullaMundo>() != null)
                {
                    continue;
                }
                GameObject raiz = PrefabUtility.GetOutermostPrefabInstanceRoot(carro.gameObject) ?? carro.gameObject;
                string codigo = CodigoDe(raiz);
                if (!string.IsNullOrEmpty(codigo) && !lista.Contains(raiz))
                {
                    lista.Add(raiz);
                }
            }
            return lista;
        }

        // El código del modelo de policía por el prefab de origen (Carro_V05, CarroTrafico_V06...) o por el nombre; vacío si es civil.
        private static string CodigoDe(GameObject go)
        {
            GameObject origen = PrefabUtility.GetCorrespondingObjectFromSource(go);
            string nombre = origen != null ? origen.name : go.name;
            foreach (string codigo in ReglaModeloDePolicia.Codigos)
            {
                if (ReglaModeloDePolicia.EsDePolicia(nombre) && nombre.Contains(codigo))
                {
                    return codigo;
                }
            }
            return string.Empty;
        }

        private static string RutaDe(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }
    }
}

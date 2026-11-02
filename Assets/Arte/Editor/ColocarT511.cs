using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-511: todos los carros estacionados de la ciudad son manejables y robables. Sobre Ciudad_Prologo: apaga los carros de adorno del barrio
    /// (los Carro_N de Barrio/Ambientacion y sus Carro_N_Visual_T250; los de la autopista elevada quedan: no se pueden alcanzar) y en cada lugar
    /// pone uno de los 8 modelos (Vehiculos/Prefabs/Carro_V*.prefab, con alarma y calor al robarlo), repartidos con una baraja barajada con semilla
    /// fija, en la dirección de su carril. Quita los 6 carros robables de T-401 (su lugar queda cubierto). Conecta el tráfico de calle de T-451 a
    /// los 8 modelos. No regenera Barrio.prefab. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT511.Aplicar
    /// </summary>
    public static class ColocarT511
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string NombreRaiz = "GTA_T511";
        public const string NombreRaizViejaT401 = "GTA_T401";
        public const string PrefijoCarro = "CarroRobable_";
        public const int Semilla = 511;
        public static readonly string[] Codigos = { "V01a", "V01b", "V02", "V03", "V04", "V05", "V06", "V08" };

        private const string CarpetaCarros = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_";
        private const string CarpetaTrafico = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_";
        private const string TipoTrafico = "MechaCyberpunk.Trafico.Calle.TraficoCalle, MechaCyberpunk.Trafico";
        private const string TipoCarroTrafico = "MechaCyberpunk.Trafico.Calle.CarroTrafico, MechaCyberpunk.Trafico";
        private const string NombreRaizTrafico = "GTA_T451";
        private const string PatronAdorno = @"^Carro_\d+$";
        private const string NombreAutopista = "Autopista";
        private const float AlturaSobreElPiso = 0.45f;
        private const float AlturaBusqueda = 6f;
        private const float XCalleVertical = 47f;
        private const float AlturaDelBarrido = 1.2f;
        private const float DistanciaLibreAdelante = 10f;

        [MenuItem("Mecha Cyberpunk/T-511/Carros estacionados manejables")]
        public static void Aplicar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            GameObject viejaT401 = GameObject.Find(NombreRaizViejaT401);
            if (viejaT401 != null)
            {
                Object.DestroyImmediate(viejaT401);
            }

            List<Transform> adornos = Adornos();
            adornos.Sort((a, b) => a.name.CompareTo(b.name));
            List<string> baraja = Baraja(adornos.Count);
            GameObject raiz = new GameObject(NombreRaiz);
            Physics.SyncTransforms();
            for (int i = 0; i < adornos.Count; i++)
            {
                Transform adorno = adornos[i];
                Vector3 posicion = adorno.position;
                float giro = GiroDeCarril(adorno);
                Transform visual = adorno.parent != null ? adorno.parent.Find(adorno.name + "_Visual_T250") : null;
                adorno.gameObject.SetActive(false);
                if (visual != null)
                {
                    visual.gameObject.SetActive(false);
                }
                if (Physics.Raycast(new Vector3(posicion.x, AlturaBusqueda, posicion.z), Vector3.down, out RaycastHit piso, AlturaBusqueda * 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    posicion.y = piso.point.y + AlturaSobreElPiso;
                }
                GameObject carro = ColocarQueCabe(raiz.transform, PrefijoCarro + (i + 1), baraja[i], posicion, giro, adorno.rotation.eulerAngles.y);
                if (carro == null)
                {
                    continue;
                }
                Debug.Log("[Arte] T-511: " + carro.name + " = " + NombreModelo(carro) + " en " + carro.transform.position.ToString("F1") + " giro " + carro.transform.eulerAngles.y.ToString("F0"));
            }
            ConectarTrafico();
            Debug.Log("[Arte] T-511: " + adornos.Count + " carros estacionados manejables en " + NombreRaiz + ".");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        // Prueba, en orden, el modelo que tocó y luego los demás por tamaño, cada uno mirando al sentido del carril y al contrario, y corrido a lo largo de la calle
        // si hace falta. Se queda con el primero que no choca con nada (barreras, postes, otros carros) y que tiene 10 m libres hacia adelante.
        private static GameObject ColocarQueCabe(Transform raiz, string nombre, string modeloAsignado, Vector3 posicion, float giro, float giroDelAdorno)
        {
            List<string> orden = new List<string> { modeloAsignado };
            orden.AddRange(Codigos.Where(c => c != modeloAsignado).OrderBy(c => c == "V08" ? 0 : 1).ThenBy(c => c));
            float[] corrimientos = { 0f, 1.5f, -1.5f, 3f, -3f, 4.5f, -4.5f, 6f, -6f, 9f, -9f };
            foreach (float corrimiento in corrimientos)
            {
                foreach (string codigo in orden)
                {
                    foreach (float vuelta in new[] { 0f, 180f })
                    {
                        float g = giro + vuelta;
                        Vector3 p = posicion + Quaternion.Euler(0f, g, 0f) * Vector3.forward * corrimiento;
                        GameObject prueba = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaCarros + codigo + ".prefab"), raiz);
                        prueba.name = nombre;
                        prueba.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, g, 0f));
                        Physics.SyncTransforms();
                        if (Cabe(prueba))
                        {
                            return prueba;
                        }
                        Object.DestroyImmediate(prueba);
                    }
                }
            }
            Debug.LogWarning("[Arte] T-511: " + nombre + " en " + posicion.ToString("F1") + " no cabe en ningún modelo (barreras del retén); ese lugar queda sin carro.");
            return null;
        }

        private static bool Cabe(GameObject carro)
        {
            foreach (string cuerpo in new[] { "Carroceria", "Cabina" })
            {
                BoxCollider caja = carro.transform.Find(cuerpo).GetComponent<BoxCollider>();
                Vector3 centro = caja.transform.TransformPoint(caja.center);
                Vector3 medio = Vector3.Scale(caja.size, caja.transform.lossyScale) * 0.5f;
                foreach (Collider otro in Physics.OverlapBox(centro, medio, caja.transform.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (!otro.transform.IsChildOf(carro.transform))
                    {
                        return false;
                    }
                }
            }
            // Diez metros libres hacia adelante (que no arranque contra una barrera).
            BoxCollider cuerpoCaja = carro.transform.Find("Carroceria").GetComponent<BoxCollider>();
            Vector3 origen = carro.transform.position + Vector3.up * AlturaDelBarrido + carro.transform.forward * (cuerpoCaja.size.z * cuerpoCaja.transform.lossyScale.z * 0.5f);
            Vector3 medioBarrido = new Vector3(cuerpoCaja.size.x * cuerpoCaja.transform.lossyScale.x * 0.5f, AlturaDelBarrido * 0.5f, 0.1f);
            return !Physics.BoxCast(origen, medioBarrido, carro.transform.forward, carro.transform.rotation, DistanciaLibreAdelante, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private static string NombreModelo(GameObject carro)
        {
            Renderer r = carro.transform.Find("Visual_T511").GetComponentInChildren<Renderer>(true);
            return r.sharedMaterial.name;
        }

        /// <summary>Los carros de adorno del barrio (los de la autopista elevada no cuentan), incluso los que ya estaban apagados.</summary>
        public static List<Transform> Adornos()
        {
            List<Transform> lista = new List<Transform>();
            foreach (GameObject r in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
                {
                    if (Regex.IsMatch(t.name, PatronAdorno) && !EstaBajo(t, NombreAutopista) && !EstaBajo(t, NombreRaiz))
                    {
                        lista.Add(t);
                    }
                }
            }
            return lista;
        }

        private static bool EstaBajo(Transform t, string nombre)
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

        // Una baraja con los 8 modelos repetidos y barajada con la semilla fija: ningún modelo se queda sin salir.
        private static List<string> Baraja(int cantidad)
        {
            System.Random azar = new System.Random(Semilla);
            List<string> baraja = new List<string>();
            while (baraja.Count < cantidad)
            {
                List<string> ronda = Codigos.OrderBy(_ => azar.Next()).ToList();
                baraja.AddRange(ronda);
            }
            return baraja.GetRange(0, cantidad);
        }

        // Dirección del carril derecho: los de la calle de eje X, hacia +X en el lado sur y hacia -X en el norte; los de eje Z, hacia +Z a la derecha de la calle.
        private static float GiroDeCarril(Transform adorno)
        {
            Transform visual = adorno.parent != null ? adorno.parent.Find(adorno.name + "_Visual_T250") : null;
            Bounds b = visual != null ? ArteT250.Limites(visual.gameObject) : new Bounds(adorno.position, new Vector3(adorno.lossyScale.x, 1f, adorno.lossyScale.z));
            Vector3 p = adorno.position;
            if (b.size.x >= b.size.z)
            {
                return p.z < 0f ? 90f : 270f;
            }
            float desdeCalle = p.x - Mathf.Sign(p.x) * XCalleVertical;
            return desdeCalle > 0f ? 0f : 180f;
        }

        private static void ConectarTrafico()
        {
            GameObject raizTrafico = GameObject.Find(NombreRaizTrafico);
            System.Type tipoTrafico = System.Type.GetType(TipoTrafico);
            System.Type tipoCarro = System.Type.GetType(TipoCarroTrafico);
            if (raizTrafico == null || tipoTrafico == null || tipoCarro == null)
            {
                Debug.LogWarning("[Arte] T-511: no encuentro el tráfico de calle (" + NombreRaizTrafico + "); no se conectó.");
                return;
            }
            Component trafico = raizTrafico.GetComponent(tipoTrafico);
            SerializedObject so = new SerializedObject(trafico);
            SerializedProperty lista = so.FindProperty("prefabsCarro");
            lista.ClearArray();
            foreach (string codigo in Codigos)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaTrafico + codigo + ".prefab");
                if (prefab == null)
                {
                    Debug.LogError("[Arte] T-511: falta " + CarpetaTrafico + codigo + ".prefab (corre CreadorTraficoT511.Crear).");
                    continue;
                }
                lista.InsertArrayElementAtIndex(lista.arraySize);
                lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = prefab.GetComponent(tipoCarro);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-drops-24nov: crea los assets de lo que sueltan los enemigos, sin primitivas y con modelos que ya están en Assets/Arte:
    /// ConfigRecompensasEnemigo_Base (Personaje/Datos/Resources), el cargador (la pieza "Cargador" de W05_Piezas, del subfusil) y el botiquín
    /// (la bebida O01_BebidaRayo, el consumible que cura; no hay un modelo de botiquín en Assets/Arte), los dos en Personaje/Resources a escala
    /// de pickup. Idempotente: la config solo se crea si falta; los prefabs se rehacen.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.RecompensasEnemigoT24nov.Aplicar
    /// </summary>
    public static class RecompensasEnemigoT24nov
    {
        public const string CarpetaResources = "Assets/_Proyecto/Personaje/Resources";
        public const string CarpetaDatos = "Assets/_Proyecto/Personaje/Datos/Resources";
        public const string RutaConfig = CarpetaDatos + "/" + ConfigRecompensasEnemigo.NombreRecurso + ".asset";
        public const string RutaCargador = CarpetaResources + "/" + Combate.MunicionEnPiso.RecursoCargador + ".prefab";
        public const string RutaBotiquin = CarpetaResources + "/" + Combate.BotiquinEnPiso.RecursoModelo + ".prefab";
        private const string FuenteCargador = "Assets/Arte/Armas/W05_Subfusil/W05_Piezas.fbx";
        private const string FuenteCargadorRespaldo = "Assets/Arte/Armas/W03_Fusil/W03_Piezas.fbx";
        private const string PiezaCargador = "Cargador";
        private const string FuenteBotiquin = "Assets/Arte/Calle/Prefabs/O01_BebidaRayo.prefab";
        private const string FuenteBotiquinRespaldo = "Assets/Arte/Calle/O01_BebidaRayo/O01_BebidaRayo.fbx";
        /// <summary>Lado mayor (m) del modelo ya como pickup.</summary>
        private const float LadoCargador = 0.24f;
        private const float LadoBotiquin = 0.26f;

        [MenuItem("Mecha Cyberpunk/T-drops-24nov Recompensas de enemigos")]
        public static void Aplicar()
        {
            Directory.CreateDirectory(CarpetaResources);
            Directory.CreateDirectory(CarpetaDatos);
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<ConfigRecompensasEnemigo>(RutaConfig) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ConfigRecompensasEnemigo>(), RutaConfig);
                Debug.Log("[Personaje] T-drops-24nov: creada " + RutaConfig);
            }
            bool cargador = CrearCargador(FuenteCargador) || CrearCargador(FuenteCargadorRespaldo);
            bool botiquin = CrearDesde(Cargar(FuenteBotiquin) ?? Cargar(FuenteBotiquinRespaldo), RutaBotiquin, LadoBotiquin);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-drops-24nov: cargador " + (cargador ? "OK" : "FALTA") + ", botiquín " + (botiquin ? "OK" : "FALTA"));
        }

        private static GameObject Cargar(string ruta)
        {
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (g == null)
            {
                Debug.LogWarning("[Personaje] T-drops-24nov: falta " + ruta);
            }
            return g;
        }

        // Saca solo la pieza "Cargador" del FBX de piezas del arma y la guarda como prefab propio.
        private static bool CrearCargador(string fuente)
        {
            GameObject modelo = Cargar(fuente);
            if (modelo == null)
            {
                return false;
            }
            Transform pieza = null;
            foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == PiezaCargador)
                {
                    pieza = t;
                    break;
                }
            }
            if (pieza == null)
            {
                Debug.LogWarning("[Personaje] T-drops-24nov: " + fuente + " no tiene la pieza " + PiezaCargador);
                return false;
            }
            GameObject copia = Object.Instantiate(pieza.gameObject);
            copia.name = "Cargador";
            copia.transform.rotation = pieza.rotation; // conserva el giro que trae del FBX (Blender)
            copia.transform.localScale = pieza.lossyScale;
            bool ok = Guardar(copia, RutaCargador, LadoCargador);
            return ok;
        }

        private static bool CrearDesde(GameObject fuente, string destino, float lado)
        {
            if (fuente == null)
            {
                return false;
            }
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(fuente);
            PrefabUtility.UnpackPrefabInstance(instancia, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instancia.transform.localScale = Vector3.one;
            return Guardar(instancia, destino, lado);
        }

        // Escala el objeto a 'lado' m en su lado mayor, lo pone bajo una raíz con la base en y = 0 y centrado, y lo guarda como prefab.
        private static bool Guardar(GameObject objeto, string destino, float lado)
        {
            objeto.transform.position = Vector3.zero;
            foreach (Collider c in objeto.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
            Renderer[] rs = objeto.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
            {
                Debug.LogError("[Personaje] T-drops-24nov: " + objeto.name + " no tiene renderers");
                Object.DestroyImmediate(objeto);
                return false;
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float escala = lado / Mathf.Max(0.001f, mayor);
            var raiz = new GameObject(Path.GetFileNameWithoutExtension(destino));
            objeto.transform.SetParent(raiz.transform, true);
            objeto.transform.localScale *= escala;
            objeto.transform.localPosition = new Vector3(-b.center.x * escala, -b.min.y * escala, -b.center.z * escala);
            GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, destino);
            Object.DestroyImmediate(raiz);
            return guardado != null;
        }
    }
}

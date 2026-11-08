using System.Linq;
using MechaCyberpunk.Personaje.Combate;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-736 (A6): la pistola del jugador con el modelo W01_Pistola_agarre.fbx de Tony (pivote en la empuñadura) para que el agarre del caso A de agarre.txt (R_Hand de PJ01)
    /// deje la palma en la empuñadura, el cañón al frente y la corredera arriba. El FBX se importa con el cañón hacia -Z y la corredera hacia +Y: el hijo Modelo lo gira
    /// 180° sobre Y (cañón a +Z, corredera a +Y) y el nodo Pistola lleva el agarre de Tony compuesto con el giro del FBX de Tony (ver AgarresArmas.GiroPJ01). Crea el prefab W01_Pistola_agarre con la animación de la corredera y el
    /// cargador (AnimacionPistola con los ejes girados) y lo pone en lugar del W01 de la pistola de JugadorAPie, con la boca (Boca) en la punta del cañón. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.ColocarAgarrePistolaT736.Colocar
    /// </summary>
    public static class ColocarAgarrePistolaT736
    {
        public const string Carpeta = "Assets/Arte/Armas/W01_Pistola/";
        public const string RutaFbx = Carpeta + "W01_Pistola_agarre.fbx";
        public const string RutaPrefab = Carpeta + "W01_Pistola_agarre.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string NombreModelo = "W01";
        /// <summary>Giro 180° sobre Y: el cañón del FBX (-Z) a +Z y la corredera sigue en +Y.</summary>
        public static readonly Quaternion GiroModelo = Quaternion.Euler(0f, 180f, 0f);
        /// <summary>De los ejes de las curvas de W01 (cañón +X, arriba +Y) a los del FBX de agarre (cañón -Z, arriba +Y).</summary>
        public static readonly Quaternion EjesCurvas = Quaternion.Euler(0f, 90f, 0f);

        [MenuItem("Mecha Cyberpunk/T-736/Pistola con el modelo de agarre")]
        public static void Colocar()
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(RutaFbx);
            if (imp.materialImportMode != ModelImporterMaterialImportMode.None || imp.importAnimation)
            {
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importAnimation = false;
                imp.SaveAndReimport();
            }
            GameObject prefab = CrearPrefab();
            PonerEnJugador(prefab);
            AssetDatabase.SaveAssets();
        }

        private static GameObject CrearPrefab()
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx);
            GameObject raiz = new GameObject("W01_Pistola_agarre");
            try
            {
                GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, raiz.transform);
                PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                modelo.name = "Modelo";
                modelo.transform.localPosition = Vector3.zero;
                modelo.transform.localRotation = GiroModelo;
                Transform armazon = modelo.transform.Find("Armazon");
                Transform corredera = modelo.transform.Find("Corredera");
                Transform bateria = modelo.transform.Find("Bateria");
                armazon.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "W01_Armazon.mat");
                corredera.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "W01_Corredera.mat");
                bateria.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "W01_Cargador.mat");
                AnimacionPistola anim = raiz.AddComponent<AnimacionPistola>();
                anim.Configurar(corredera, bateria, 1f);
                anim.ConfigurarEjes(EjesCurvas);
                return PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void PonerEnJugador(GameObject prefab)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaJugador);
            try
            {
                Transform pistola = contenido.GetComponentsInChildren<Transform>(true).First(t => t.name == "Pistola" && t.Find("Canon") != null);
                Transform viejo = pistola.Find(NombreModelo);
                int indice = viejo != null ? viejo.GetSiblingIndex() : pistola.childCount;
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pistola);
                nuevo.name = NombreModelo;
                nuevo.transform.localPosition = Vector3.zero;
                nuevo.transform.localRotation = Quaternion.identity;
                nuevo.transform.localScale = Vector3.one;
                nuevo.transform.SetSiblingIndex(Mathf.Min(indice, pistola.childCount - 1));
                // La boca en la punta del cañón (en el espacio de la pistola) y a la altura de la corredera.
                Bounds corredera = nuevo.transform.Find("Modelo/Corredera").GetComponent<Renderer>().bounds;
                float zMax = float.MinValue;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = corredera.center + Vector3.Scale(corredera.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    zMax = Mathf.Max(zMax, pistola.InverseTransformPoint(esquina).z);
                }
                Transform boca = pistola.Find("Boca");
                if (boca != null)
                {
                    Vector3 centro = pistola.InverseTransformPoint(corredera.center);
                    boca.localPosition = new Vector3(0f, centro.y, zMax);
                    Debug.Log("[Personaje] T-736: Boca en " + boca.localPosition.ToString("F3"));
                }
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaJugador);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }
    }
}

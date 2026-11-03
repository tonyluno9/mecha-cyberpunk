using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-543 (plan de la noche 3-nov, paso 2): V07, la moto de reparto, manejable y robable como las demás (Carro_V07, se monta como moto),
    /// estacionada en Suburbios y en El Tianguis, en lugares libres junto a la banqueta. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.ColocarT543.Colocar
    /// </summary>
    public static class ColocarT543
    {
        public const string NombreRaiz = "T543_MotosReparto";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        /// <summary>Por zona: desde dónde se busca lugar (x) a lo largo de la calle principal, de qué lado (z) y hacia dónde mira.</summary>
        public static readonly (string zona, float x, float z, Vector3 frente)[] Lugares =
        {
            ("Suburbios", -96f, -6.7f, Vector3.right),
            ("Suburbios", -104f, 6.7f, Vector3.right),
            ("Tianguis", -41f, -6.7f, Vector3.right),
            ("Tianguis", 41f, 6.7f, Vector3.left),
        };

        [MenuItem("Mecha Cyberpunk/T-543/Motos de reparto")]
        public static void Colocar()
        {
            StringBuilder informe = new StringBuilder();
            informe.AppendLine(CreadorCarrosT511.CrearUno("V07", "V07", 0f, 2.1f));
            string ruta = CreadorCarrosT511.RutaPrefab("V07");
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            SerializedObject so = new SerializedObject(contenido.GetComponent<CarroViejo>());
            so.FindProperty("nombreModelo").stringValue = "Moto de reparto";
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
            PrefabUtility.UnloadPrefabContents(contenido);

            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Physics.SyncTransforms();
            Transform raiz = new GameObject(NombreRaiz).transform;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            int n = 0;
            foreach (var l in Lugares)
            {
                // Se recorre la orilla de 1.5 en 1.5 m hasta encontrar 3 m libres (sin puestos, carros ni postes).
                for (int i = 0; i < 20; i++)
                {
                    float x = l.x + i * 1.5f * Mathf.Sign(l.frente.x);
                    Vector3 p = new Vector3(x, 0.5f, l.z);
                    if (Physics.CheckBox(p + Vector3.up * 0.4f, new Vector3(3.5f, 0.6f, 0.7f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    GameObject moto = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz);
                    moto.name = "MotoReparto_" + l.zona + "_" + (++n);
                    moto.transform.SetPositionAndRotation(p, Quaternion.LookRotation(l.frente));
                    informe.AppendLine(moto.name + " en " + p);
                    break;
                }
            }
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t543-colocar.txt", informe.ToString());
            Debug.Log("[Vehiculos] T-543 listo:\n" + informe);
        }
    }
}

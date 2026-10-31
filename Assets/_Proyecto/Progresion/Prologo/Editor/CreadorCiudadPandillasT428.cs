using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-428 del creador de la ciudad de pandillas: LA REFINERÍA (cuarta zona, de NOVA) en Manzana_6. La zona y su jefe salen de T404Interno, los
    /// grupos de T402Interno y el punto de ataque de T427Interno; esto agrega los props de T-250 que la marcan (barreras C18, tambos C16, huacales C17,
    /// pantalla CY02, cámara CY08) bajo la raíz GTA_T428 y, en una escena hecha antes de T-428, pasa el grupo de Manzana_6 de SNAKES a guardias de NOVA.
    /// Idempotente. El menú rehace zonas, jefes, props y puntos de ataque en una escena vieja sin tocar lo demás.
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT428 = "GTA_T428";
        private const string CarpetaCalle = "Assets/Arte/Calle/Prefabs/";

        [MenuItem("Mecha Cyberpunk/Pandillas/T-428 La Refinería")]
        public static void T428()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T404Interno();
            T428Interno();
            T418Interno();
            T427Interno();
            Guardar(escena);
        }

        private static void T428Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT428);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            CreadorPandilleroArmado.Crear();
            DatosPandilleroAPie nova = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(CreadorPandilleroArmado.RutaDatosNova);

            // Una escena hecha antes de T-428 trae ahí un grupo de SNAKES: pasa a ser de NOVA.
            foreach (PandillaAPie g in Object.FindObjectsByType<PandillaAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Vector3 p = g.transform.position;
                if (g.Pandilla == IdPandilla.Viboras && p.x > LimiteZonaX && p.z < 0f)
                {
                    g.name = "Pandilla_Nova_4";
                    SerializedObject soG = new SerializedObject(g);
                    soG.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Nova;
                    soG.FindProperty("datos").objectReferenceValue = nova;
                    soG.ApplyModifiedPropertiesWithoutUndo();
                    foreach (PandilleroAPie m in g.GetComponentsInChildren<PandilleroAPie>(true))
                    {
                        SerializedObject soM = new SerializedObject(m);
                        soM.FindProperty("datos").objectReferenceValue = nova;
                        soM.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            Transform manzana = BuscarTransform("Manzana_6");
            Transform jefe = BuscarTransform("PuntoJefe_refineria");
            if (manzana == null || jefe == null)
            {
                Debug.LogError("[T-428] Falta Manzana_6 o PuntoJefe_refineria: corre antes T404.");
                return;
            }

            // Hacia la calle: el eje en el que el jefe está más lejos del centro de la manzana.
            Vector3 fuera = jefe.position - manzana.position;
            fuera.y = 0f;
            fuera = Mathf.Abs(fuera.x) > Mathf.Abs(fuera.z) ? new Vector3(Mathf.Sign(fuera.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(fuera.z));
            Vector3 lado = Vector3.Cross(Vector3.up, fuera);

            GameObject raiz = new GameObject(RaizT428);
            Vector3 baseP = jefe.position;
            // Barreras de la calle a lo largo de la banqueta, tambos y huacales detrás, una pantalla de NOVA y una cámara corporativa.
            for (int i = -2; i <= 2; i++)
            {
                Poner(raiz.transform, "C18_BarreraCalle", "Barrera_" + (i + 3), baseP + fuera * 3.5f + lado * (i * 4f), fuera);
            }
            for (int i = 0; i < 4; i++)
            {
                Poner(raiz.transform, "C16_Tambo", "Tambo_" + (i + 1), baseP - fuera * 2f + lado * (-5f + i * 3f), fuera);
            }
            Poner(raiz.transform, "C17_Huacales", "Huacales_1", baseP - fuera * 3f + lado * 8f, fuera);
            Poner(raiz.transform, "C17_Huacales", "Huacales_2", baseP - fuera * 3f - lado * 8f, fuera);
            Poner(raiz.transform, "CY02_PantallaVertical", "PantallaNova", baseP + lado * 11f, -fuera);
            Poner(raiz.transform, "CY08_CamaraCorporativa", "CamaraNova", baseP - lado * 11f, -fuera);
            Debug.Log("[T-428] LA REFINERÍA: jefe en " + baseP + ", " + raiz.transform.childCount + " props.");
        }

        private static void Poner(Transform padre, string codigo, string nombre, Vector3 posicion, Vector3 mira)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaCalle + codigo + ".prefab");
            if (prefab == null)
            {
                Debug.LogWarning("[T-428] No está el prefab " + codigo);
                return;
            }
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            instancia.name = nombre;
            instancia.transform.SetPositionAndRotation(PisoT426(posicion), Quaternion.LookRotation(mira, Vector3.up));
        }
    }
}

#if UNITY_EDITOR
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1107: arma una riña (<see cref="RinaPandillas"/> con dos <see cref="PandillaAPie"/>) con prefabs y datos dados. Solo del editor: lo usan el
    /// colocador de la ciudad y las pruebas. Los dos bandos quedan de frente a <paramref name="separacion"/> m, en filas perpendiculares al eje.
    /// </summary>
    public static class ConstructorRina
    {
        public const float SeparacionNormal = 5f;
        public const float LateralNormal = 1.6f;
        private const float AltoRayo = 2f;
        private const float LargoRayo = 5f;

        public static RinaPandillas Crear(Transform padre, string nombre, Vector3 centro, Vector3 eje,
            GameObject prefabA, DatosPandilleroAPie datosA, IdPandilla idA,
            GameObject prefabB, DatosPandilleroAPie datosB, IdPandilla idB,
            int porBando = 3, float separacion = SeparacionNormal, float lateral = LateralNormal)
        {
            eje.y = 0f;
            eje = eje.normalized;
            GameObject raiz = new GameObject(nombre);
            raiz.SetActive(false); // sin Awake hasta que todo esté configurado
            if (padre != null)
            {
                raiz.transform.SetParent(padre, false);
            }
            raiz.transform.position = centro;
            PandillaAPie a = CrearBando(raiz.transform, "Bando_" + idA, centro - eje * (separacion * 0.5f), eje, eje, prefabA, datosA, idA, porBando, lateral);
            PandillaAPie b = CrearBando(raiz.transform, "Bando_" + idB, centro + eje * (separacion * 0.5f), eje, -eje, prefabB, datosB, idB, porBando, lateral);
            RinaPandillas rina = raiz.AddComponent<RinaPandillas>();
            SerializedObject so = new SerializedObject(rina);
            so.FindProperty("bandoA").objectReferenceValue = a;
            so.FindProperty("bandoB").objectReferenceValue = b;
            so.ApplyModifiedPropertiesWithoutUndo();
            raiz.SetActive(true);
            return rina;
        }

        private static PandillaAPie CrearBando(Transform raiz, string nombre, Vector3 centro, Vector3 eje, Vector3 mira, GameObject prefab,
            DatosPandilleroAPie datos, IdPandilla id, int cantidad, float lateral)
        {
            GameObject bando = new GameObject(nombre);
            bando.transform.SetParent(raiz, false);
            bando.transform.position = centro;
            Vector3 ladoFila = Vector3.Cross(Vector3.up, eje);
            for (int i = 0; i < cantidad; i++)
            {
                Vector3 pos = centro + ladoFila * ((i - (cantidad - 1) * 0.5f) * lateral);
                if (Physics.Raycast(pos + Vector3.up * AltoRayo, Vector3.down, out RaycastHit h, LargoRayo, ~0, QueryTriggerInteraction.Ignore))
                {
                    pos = h.point;
                }
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bando.transform);
                p.name = "Pandillero_" + (i + 1);
                p.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira, Vector3.up));
                SerializedObject soP = new SerializedObject(p.GetComponent<PandilleroAPie>());
                soP.FindProperty("datos").objectReferenceValue = datos;
                soP.ApplyModifiedPropertiesWithoutUndo();
                p.AddComponent<ConductaRina>();
            }
            PandillaAPie pandilla = bando.AddComponent<PandillaAPie>();
            SerializedObject soG = new SerializedObject(pandilla);
            soG.FindProperty("datos").objectReferenceValue = datos;
            soG.FindProperty("idPandilla").enumValueIndex = (int)id;
            soG.ApplyModifiedPropertiesWithoutUndo();
            return pandilla;
        }
    }
}
#endif

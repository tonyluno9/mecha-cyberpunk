#if UNITY_EDITOR
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1108: arma el puesto del jefe SNAKES: el jefe (prefab con <see cref="ConductaJefeSnakes"/>), un grupo de 2 escoltas SNAKES a sus lados y un
    /// grupo apagado de 2 SNAKES de refuerzo que el jefe activa al 50% de vida. Solo del editor: lo usan el colocador de la ciudad y las pruebas.
    /// </summary>
    public static class ConstructorJefeSnakes
    {
        public const float SeparacionEscoltas = 2.5f;
        public const float DistanciaRefuerzos = 12f;
        private const float AltoRayo = 2f;
        private const float LargoRayo = 5f;

        /// <summary>Devuelve la conducta del jefe; su raíz es <c>conducta.transform.parent</c>.</summary>
        public static ConductaJefeSnakes Crear(Transform padre, string nombre, Vector3 centro, Vector3 mira, GameObject prefabJefe, DatosPandilleroAPie datosJefe,
            GameObject prefabSnakes, DatosPandilleroAPie datosViboras, bool conRefuerzos = true, bool conEscoltas = true)
        {
            mira.y = 0f;
            mira = mira.sqrMagnitude < 0.0001f ? Vector3.forward : mira.normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, mira);
            GameObject raiz = new GameObject(nombre);
            raiz.SetActive(false); // sin Awake hasta tener todo configurado
            if (padre != null)
            {
                raiz.transform.SetParent(padre, false);
            }
            raiz.transform.position = centro;

            GameObject jefe = (GameObject)PrefabUtility.InstantiatePrefab(prefabJefe, raiz.transform);
            jefe.name = "Jefe";
            jefe.transform.SetPositionAndRotation(Suelo(centro), Quaternion.LookRotation(mira, Vector3.up));
            SerializedObject soJefe = new SerializedObject(jefe.GetComponent<PandilleroAPie>());
            soJefe.FindProperty("datos").objectReferenceValue = datosJefe;
            soJefe.ApplyModifiedPropertiesWithoutUndo();
            ConductaJefeSnakes conducta = jefe.GetComponent<ConductaJefeSnakes>();

            if (conEscoltas)
            {
                PandillaAPie escoltas = CrearGrupo(raiz.transform, "Escoltas",
                    new[] { centro - mira * 1.5f - lado * SeparacionEscoltas, centro - mira * 1.5f + lado * SeparacionEscoltas }, mira, prefabSnakes, datosViboras, true);
                SerializedObject soEscoltas = new SerializedObject(conducta);
                soEscoltas.FindProperty("escoltas").objectReferenceValue = escoltas;
                soEscoltas.ApplyModifiedPropertiesWithoutUndo();
            }
            if (conRefuerzos)
            {
                Vector3 aparte = DireccionRefuerzos(centro, mira);
                PandillaAPie refuerzos = CrearGrupo(raiz.transform, "Refuerzos",
                    new[] { centro + aparte * DistanciaRefuerzos - lado * SeparacionEscoltas, centro + aparte * DistanciaRefuerzos + lado * SeparacionEscoltas },
                    mira, prefabSnakes, datosViboras, false);
                SerializedObject soConducta = new SerializedObject(conducta);
                soConducta.FindProperty("refuerzos").objectReferenceValue = refuerzos;
                soConducta.ApplyModifiedPropertiesWithoutUndo();
            }
            raiz.SetActive(true);
            return conducta;
        }

        /// <summary>De qué lado del jefe esperan los refuerzos: detrás si hay piso libre a <see cref="DistanciaRefuerzos"/>, y si no delante o a un costado.</summary>
        public static Vector3 DireccionRefuerzos(Vector3 centro, Vector3 mira)
        {
            Vector3 lado = Vector3.Cross(Vector3.up, mira);
            foreach (Vector3 d in new[] { -mira, mira, lado, -lado })
            {
                if (HayPisoLibre(centro + d * DistanciaRefuerzos))
                {
                    return d;
                }
            }
            return -mira;
        }

        /// <summary>True si a esa posición hay piso a la altura de la calle y nada sólido a la altura del cuerpo.</summary>
        public static bool HayPisoLibre(Vector3 p)
        {
            return Physics.Raycast(p + Vector3.up * AltoRayo, Vector3.down, out RaycastHit h, LargoRayo, ~0, QueryTriggerInteraction.Ignore)
                && !Physics.CheckCapsule(h.point + Vector3.up * 0.6f, h.point + Vector3.up * 1.8f, 0.6f, ~0, QueryTriggerInteraction.Ignore);
        }

        private static PandillaAPie CrearGrupo(Transform raiz, string nombre, Vector3[] puntos, Vector3 mira, GameObject prefab, DatosPandilleroAPie datos, bool activo)
        {
            GameObject grupo = new GameObject(nombre);
            grupo.SetActive(false);
            grupo.transform.SetParent(raiz, false);
            grupo.transform.position = (puntos[0] + puntos[puntos.Length - 1]) * 0.5f;
            for (int i = 0; i < puntos.Length; i++)
            {
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo.transform);
                p.name = "Pandillero_" + (i + 1);
                p.transform.SetPositionAndRotation(Suelo(puntos[i]), Quaternion.LookRotation(mira, Vector3.up));
                SerializedObject so = new SerializedObject(p.GetComponent<PandilleroAPie>());
                so.FindProperty("datos").objectReferenceValue = datos;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PandillaAPie pandilla = grupo.AddComponent<PandillaAPie>();
            SerializedObject soG = new SerializedObject(pandilla);
            soG.FindProperty("datos").objectReferenceValue = datos;
            soG.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Viboras;
            soG.ApplyModifiedPropertiesWithoutUndo();
            grupo.SetActive(activo);
            return pandilla;
        }

        private static Vector3 Suelo(Vector3 punto)
        {
            return Physics.Raycast(punto + Vector3.up * AltoRayo, Vector3.down, out RaycastHit h, LargoRayo, ~0, QueryTriggerInteraction.Ignore) ? h.point : punto;
        }
    }
}
#endif

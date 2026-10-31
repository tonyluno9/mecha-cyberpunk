using System.Collections.Generic;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Progresion.Prologo.Encargos;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-427 del creador de la ciudad de pandillas: el DirectorAtaques con un punto de ataque libre en EL TIANGUIS, NEÓN ALTO y LA REFINERÍA (T-428), 4 rivales a puños
    /// apagados y el asset DatosAtaquesZonas_Base. Idempotente: rehace solo la raíz GTA_T427. Va después de T426 (usa su DirectorEncargos).
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT427 = "GTA_T427";
        private const string RutaDatosAtaques = "Assets/_Proyecto/Progresion/Datos/DatosAtaquesZonas_Base.asset";

        [MenuItem("Mecha Cyberpunk/Pandillas/T-427 Ataques a zonas")]
        public static void T427()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T427Interno();
            Guardar(escena);
        }

        // Un punto libre cerca de la base, dentro de la zona (X entre los límites), buscando en espiral.
        private static Vector3 PuntoLibreT427(Vector3 baseXZ, float xMin, float xMax, float zMin, float zMax)
        {
            for (int k = 0; k < 60; k++)
            {
                float angulo = k * 47f * Mathf.Deg2Rad;
                float distancia = 4f + k * 2.5f;
                Vector3 candidato = PisoT426(baseXZ + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * distancia);
                if (candidato.x > xMin && candidato.x < xMax && candidato.z > zMin && candidato.z < zMax && candidato.y < 1f && LibreT426(candidato))
                {
                    return candidato;
                }
            }
            return PisoT426(baseXZ);
        }

        private static void T427Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT427);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            DirectorPandillas historia = Object.FindFirstObjectByType<DirectorPandillas>(FindObjectsInactive.Include);
            DirectorEncargos encargos = Object.FindFirstObjectByType<DirectorEncargos>(FindObjectsInactive.Include);
            if (historia == null || encargos == null)
            {
                Debug.LogError("[T-427] Falta el DirectorPandillas o el DirectorEncargos: corre antes T406 y T426.");
                return;
            }

            DatosAtaquesZonas datos = CrearAsset<DatosAtaquesZonas>(RutaDatosAtaques);
            EditorUtility.SetDirty(datos);

            GameObject raiz = new GameObject(RaizT427);
            string[] zonas = { "tianguis", "neon", "refineria" };
            Vector3[] bases = { new Vector3(0f, 0f, 30f), new Vector3(110f, 0f, 30f), new Vector3(110f, 0f, -30f) };
            float[] xMin = { -LimiteZonaX + 6f, LimiteZonaX + 6f, LimiteZonaX + 6f };
            float[] xMax = { LimiteZonaX - 6f, 400f, 400f };
            float[] zMin = { -400f, 6f, -400f };
            float[] zMax = { 400f, 400f, -6f };
            List<Transform> puntos = new List<Transform>();
            for (int i = 0; i < zonas.Length; i++)
            {
                GameObject p = new GameObject("Ataque_" + zonas[i]);
                p.transform.SetParent(raiz.transform, false);
                p.transform.position = PuntoLibreT427(bases[i], xMin[i], xMax[i], zMin[i], zMax[i]);
                puntos.Add(p.transform);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandilleroPunos);
            DatosPandilleroAPie datosRival = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosPunos);
            List<PandilleroAPie> rivales = new List<PandilleroAPie>();
            for (int i = 0; i < DirectorAtaques.RivalesOleada; i++)
            {
                GameObject rival = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                rival.name = "RivalAtaque_" + (i + 1);
                rival.transform.position = puntos[0].position;
                SerializedObject soP = new SerializedObject(rival.GetComponent<PandilleroAPie>());
                soP.FindProperty("datos").objectReferenceValue = datosRival;
                soP.ApplyModifiedPropertiesWithoutUndo();
                rival.SetActive(false);
                rivales.Add(rival.GetComponent<PandilleroAPie>());
            }

            DirectorAtaques director = raiz.AddComponent<DirectorAtaques>();
            SerializedObject so = new SerializedObject(director);
            so.FindProperty("historia").objectReferenceValue = historia;
            so.FindProperty("encargos").objectReferenceValue = encargos;
            so.FindProperty("datos").objectReferenceValue = datos;
            SerializedProperty lp = so.FindProperty("puntos");
            lp.arraySize = zonas.Length;
            for (int i = 0; i < zonas.Length; i++)
            {
                SerializedProperty e = lp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("zona").stringValue = zonas[i];
                e.FindPropertyRelative("punto").objectReferenceValue = puntos[i];
            }
            SerializedProperty rv = so.FindProperty("rivales");
            rv.arraySize = rivales.Count;
            for (int i = 0; i < rivales.Count; i++)
            {
                rv.GetArrayElementAtIndex(i).objectReferenceValue = rivales[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[T-427] Puntos de ataque: tianguis " + puntos[0].position + ", neón " + puntos[1].position + ", refinería " + puntos[2].position);
        }
    }
}

using System.Collections.Generic;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Prologo.Encargos;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-426 del creador de la ciudad de pandillas: el tablero de encargos junto a tu casa, 3 locales de cuota y el punto de defensa en SUBURBIOS
    /// y 4 rivales a puños apagados. Idempotente: rehace solo la raíz GTA_T426.
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT426 = "GTA_T426";
        private const float AlturaTablero = 1.4f;

        [MenuItem("Mecha Cyberpunk/Pandillas/T-426 Encargos")]
        public static void T426()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T426Interno();
            Guardar(escena);
        }

        // El suelo más bajo bajo el punto (los techos quedan arriba).
        private static Vector3 PisoT426(Vector3 p)
        {
            Physics.SyncTransforms();
            RaycastHit[] golpes = Physics.RaycastAll(p + Vector3.up * 30f, Vector3.down, 60f);
            bool hay = false;
            Vector3 mejor = p;
            foreach (RaycastHit g in golpes)
            {
                if (!hay || g.point.y < mejor.y)
                {
                    mejor = g.point;
                    hay = true;
                }
            }
            return mejor;
        }

        // Un punto sin edificios ni carros en 5 m a la redonda (solo suelo bajo).
        private static bool LibreT426(Vector3 p)
        {
            foreach (Collider c in Physics.OverlapSphere(p + Vector3.up * 3f, 5f))
            {
                if (c.bounds.max.y > 0.8f)
                {
                    return false;
                }
            }
            return true;
        }

        private static void T426Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT426);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            DirectorPandillas historia = Object.FindFirstObjectByType<DirectorPandillas>(FindObjectsInactive.Include);
            Transform puerta = BuscarTransform(IdAparicion);
            if (historia == null || puerta == null)
            {
                Debug.LogError("[T-426] Falta el DirectorPandillas o " + IdAparicion + ": corre antes T406.");
                return;
            }

            GameObject raiz = new GameObject(RaizT426);

            // Tablero: un poste con letrero junto a la puerta de tu casa.
            Vector3 lado = Vector3.Cross(Vector3.up, puerta.forward).normalized;
            Vector3 posTablero = PisoT426(puerta.position + puerta.forward * 2.5f + lado * 3f);
            GameObject tableroObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableroObj.name = "TableroEncargos";
            tableroObj.transform.SetParent(raiz.transform, false);
            tableroObj.transform.SetPositionAndRotation(posTablero + Vector3.up * AlturaTablero * 0.5f, Quaternion.LookRotation(puerta.forward));
            tableroObj.transform.localScale = new Vector3(1.1f, AlturaTablero, 0.12f);
            string rutaMat = "Assets/_Proyecto/Progresion/Prologo/Materiales/Mat_TableroEncargos.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.1f, 0.85f, 1f) };
                AssetDatabase.CreateAsset(mat, rutaMat);
            }
            tableroObj.GetComponent<Renderer>().sharedMaterial = mat;
            GameObject texto = new GameObject("Letrero");
            texto.transform.SetParent(tableroObj.transform, false);
            texto.transform.localPosition = new Vector3(0f, 0.2f, -0.6f);
            texto.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
            TextMesh tm = texto.AddComponent<TextMesh>();
            tm.text = "ENCARGOS";
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 60;
            tm.characterSize = 0.1f;
            tm.color = Color.black;

            // Locales de cuota: lugares de SUBURBIOS (a la izquierda del límite); si faltan, puntos alrededor del tablero.
            List<Transform> locales = new List<Transform>();
            List<Transform> candidatos = new List<Transform>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in g.GetComponentsInChildren<Transform>(false))
                {
                    if (t.name.StartsWith("Lugar_") && t.position.x < -LimiteZonaX && !t.IsChildOf(raiz.transform))
                    {
                        candidatos.Add(t);
                    }
                }
            }
            candidatos.Sort((a, b) => (a.position - posTablero).sqrMagnitude.CompareTo((b.position - posTablero).sqrMagnitude));
            Vector3[] anillo = { new Vector3(18f, 0f, 14f), new Vector3(-16f, 0f, 20f), new Vector3(-8f, 0f, -22f) };
            for (int i = 0; i < 3; i++)
            {
                GameObject punto = new GameObject("Local_" + (i + 1));
                punto.transform.SetParent(raiz.transform, false);
                Vector3 pos = i < candidatos.Count ? candidatos[i].position : PisoT426(posTablero + anillo[i]);
                punto.transform.SetPositionAndRotation(pos, Quaternion.identity);
                locales.Add(punto.transform);
            }

            GameObject defensa = new GameObject("PuntoDefensa");
            defensa.transform.SetParent(raiz.transform, false);
            Vector3 puntoDefensa = PisoT426(posTablero + new Vector3(-22f, 0f, 6f));
            for (int k = 0; k < 36; k++)
            {
                float angulo = k * 37f * Mathf.Deg2Rad;
                float distancia = 40f + (k % 6) * 4f;
                Vector3 candidato = PisoT426(posTablero + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * distancia);
                if (candidato.x < -LimiteZonaX - 8f && candidato.y < 1f && LibreT426(candidato))
                {
                    puntoDefensa = candidato;
                    break;
                }
            }
            defensa.transform.position = puntoDefensa;

            // Rivales a puños apagados (los mismos de la pelea de DOÑA LUPE).
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandilleroPunos);
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosPunos);
            List<PandilleroAPie> rivales = new List<PandilleroAPie>();
            for (int i = 0; i < DirectorEncargos.RivalesOleada; i++)
            {
                GameObject rival = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                rival.name = "RivalEncargo_" + (i + 1);
                rival.transform.position = defensa.transform.position;
                SerializedObject soP = new SerializedObject(rival.GetComponent<PandilleroAPie>());
                soP.FindProperty("datos").objectReferenceValue = datos;
                soP.ApplyModifiedPropertiesWithoutUndo();
                rival.SetActive(false);
                rivales.Add(rival.GetComponent<PandilleroAPie>());
            }

            PersonajeApoyo mecanico = null;
            foreach (PersonajeApoyo p in Object.FindObjectsByType<PersonajeApoyo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.Datos != null && p.Datos.Id == "mecanico")
                {
                    mecanico = p;
                }
            }

            DirectorEncargos director = raiz.AddComponent<DirectorEncargos>();
            SerializedObject so = new SerializedObject(director);
            so.FindProperty("historia").objectReferenceValue = historia;
            so.FindProperty("puntoDefensa").objectReferenceValue = defensa.transform;
            so.FindProperty("tablero").objectReferenceValue = tableroObj.transform;
            so.FindProperty("mecanico").objectReferenceValue = mecanico;
            SerializedProperty lista = so.FindProperty("locales");
            lista.arraySize = locales.Count;
            for (int i = 0; i < locales.Count; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = locales[i];
            }
            SerializedProperty rv = so.FindProperty("rivales");
            rv.arraySize = rivales.Count;
            for (int i = 0; i < rivales.Count; i++)
            {
                rv.GetArrayElementAtIndex(i).objectReferenceValue = rivales[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            TableroEncargos tablero = tableroObj.AddComponent<TableroEncargos>();
            SerializedObject soT = new SerializedObject(tablero);
            soT.FindProperty("director").objectReferenceValue = director;
            soT.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[T-426] Puerta " + puerta.position + ", tablero " + posTablero + ", locales " + locales[0].position + " " + locales[1].position + " " + locales[2].position
                + " (de " + candidatos.Count + " lugares), defensa " + defensa.transform.position + ", mecánico " + (mecanico != null ? mecanico.Posicion.ToString() : "?"));
        }
    }
}

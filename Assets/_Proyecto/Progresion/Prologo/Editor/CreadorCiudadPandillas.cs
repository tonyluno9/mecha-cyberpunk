using System.IO;
using MechaCyberpunk.Progresion.Pruebas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Creador aditivo de la ciudad del juego de pandillas (D-tony-205). Abre Ciudad_Prologo (que ya tiene el arte de T-250, no se regenera),
    /// rehace solo la raíz GTA_T40x de cada tarea y apaga (nunca borra) lo que estorba. Cada tarea agrega su método (T401...) y Todo() los corre en orden.
    /// Idempotente: correrlo dos veces deja la escena igual.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorCiudadPandillas.Todo
    /// </summary>
    public static class CreadorCiudadPandillas
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RaizT400 = "GTA_T400";
        public const string RaizT401 = "GTA_T401";
        public const float HoraInicial = 19f;
        public const float DistanciaMuneco1 = 8f;
        public const float DistanciaMuneco2 = 12f;

        private const string RutaCarroViejo = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string TipoCarroEstacionado = "MechaCyberpunk.Vehiculos.Carro.CarroEstacionado, MechaCyberpunk.Vehiculos";
        private const string RutaMuneco = "Assets/_Proyecto/Personaje/Pruebas/MunecoAPie.prefab";
        private const string RutaRelojPandillas = "Assets/_Proyecto/Progresion/Prologo/Datos/ConfigRelojMundo_Pandillas.asset";
        private const string IdAparicion = "Casa_Puerta";
        private static readonly string[] ApagadosT400 = { "MechaEstacionado", "Emboscada", "DirectorPrologo", "MostradorPeleas" };

        [MenuItem("Mecha Cyberpunk/Pandillas/Todo")]
        public static void Todo()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T400Interno();
            T401Interno();
            Guardar(escena);
        }

        [MenuItem("Mecha Cyberpunk/Pandillas/T-401 Carros para robar")]
        public static void T401()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T401Interno();
            Guardar(escena);
        }

        [MenuItem("Mecha Cyberpunk/Pandillas/T-400 Tercera persona")]
        public static void T400()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T400Interno();
            Guardar(escena);
        }

        private static void Guardar(Scene escena)
        {
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
        }

        private static void T400Interno()
        {
            // 1. El mecha, la emboscada y los directores del prólogo se apagan (D-tony-206). La ayuda de llamar al mecha no sale
            // sola: necesita al mecha en su taller, y está apagado.
            foreach (string nombre in ApagadosT400)
            {
                GameObject objeto = BuscarRaiz(nombre);
                if (objeto != null)
                {
                    objeto.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("[Progresion] CreadorCiudadPandillas: no encuentro " + nombre + " en Ciudad_Prologo.");
                }
            }

            // 2. Raíz propia, rehecha cada vez.
            GameObject anterior = BuscarRaiz(RaizT400);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT400);

            // La historia arranca en Libre para que nada mande al título ni al mecha.
            raiz.AddComponent<HistoriaLibreDePrueba>();

            // 3. Dos muñecos de prueba delante de la casa, para pegarles.
            Transform casa = BuscarTransform(IdAparicion);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMuneco);
            if (casa != null && prefab != null)
            {
                float[] distancias = { DistanciaMuneco1, DistanciaMuneco2 };
                for (int i = 0; i < distancias.Length; i++)
                {
                    GameObject muneco = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                    muneco.name = "MunecoPrueba_" + (i + 1);
                    Vector3 posicion = casa.position + casa.forward * distancias[i];
                    if (Physics.Raycast(posicion + Vector3.up * 20f, Vector3.down, out RaycastHit piso, 60f))
                    {
                        posicion = piso.point;
                    }
                    muneco.transform.SetPositionAndRotation(posicion, Quaternion.LookRotation(-casa.forward, Vector3.up));
                }
            }
            else
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas: falta " + IdAparicion + " o el prefab del muñeco.");
            }

            // 4. Hora inicial 19:00 con una copia de la config del reloj, para no tocar el asset de Alan.
            PonerHoraInicial();
            Debug.Log("[Progresion] CreadorCiudadPandillas.T400: " + RaizT400 + " lista, mecha apagado, hora " + HoraInicial + ".");
        }

        // T-401: 6 carros de adorno (2 por zona: oeste, centro, este por la X) se apagan y en su lugar van CarroViejo robables.
        private static void T401Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT401);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarroViejo);
            if (prefab == null)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas: falta " + RutaCarroViejo);
                return;
            }
            GameObject raiz = new GameObject(RaizT401);
            Transform casa = BuscarTransform(IdAparicion);
            Vector3 referencia = casa != null ? casa.position : Vector3.zero;

            // Los adornos son los Carro_N de la escena (los apagados por una corrida anterior también cuentan).
            System.Collections.Generic.List<Transform> adornos = new System.Collections.Generic.List<Transform>();
            foreach (GameObject objetoRaiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in objetoRaiz.GetComponentsInChildren<Transform>(true))
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(t.name, @"^Carro_\d+$") && !t.IsChildOf(raiz.transform))
                    {
                        adornos.Add(t);
                    }
                }
            }
            if (adornos.Count < 6)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas: solo hay " + adornos.Count + " Carro_N de adorno.");
            }
            adornos.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            int tercio = Mathf.Max(1, adornos.Count / 3);
            Color[] tintes =
            {
                new Color(0.1f, 0.35f, 0.7f), new Color(0.75f, 0.7f, 0.15f), new Color(0.15f, 0.55f, 0.3f),
                new Color(0.55f, 0.15f, 0.55f), new Color(0.85f, 0.85f, 0.85f), new Color(0.08f, 0.08f, 0.1f)
            };
            int numero = 0;
            for (int zona = 0; zona < 3; zona++)
            {
                int desde = zona * tercio;
                int hasta = zona == 2 ? adornos.Count : Mathf.Min(adornos.Count, desde + tercio);
                System.Collections.Generic.List<Transform> candidatos = adornos.GetRange(Mathf.Min(desde, adornos.Count), Mathf.Max(0, hasta - desde));
                candidatos.Sort((a, b) => (a.position - referencia).sqrMagnitude.CompareTo((b.position - referencia).sqrMagnitude));
                for (int i = 0; i < 2 && i < candidatos.Count; i++)
                {
                    Transform adorno = candidatos[i];
                    GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                    carro.name = "CarroRobable_" + (numero + 1);
                    carro.transform.SetPositionAndRotation(adorno.position, adorno.rotation);
                    // Vehiculos es de otra área: el componente se busca por nombre en vez de referenciar su asmdef.
                    System.Type tipoEstacionado = System.Type.GetType(TipoCarroEstacionado);
                    if (tipoEstacionado == null)
                    {
                        Debug.LogError("[Progresion] CreadorCiudadPandillas: no encuentro CarroEstacionado.");
                        break;
                    }
                    Component estacionado = carro.GetComponent(tipoEstacionado);
                    if (estacionado == null)
                    {
                        estacionado = carro.AddComponent(tipoEstacionado);
                    }
                    SerializedObject so = new SerializedObject(estacionado);
                    so.FindProperty("tinte").colorValue = tintes[numero % tintes.Length];
                    so.ApplyModifiedPropertiesWithoutUndo();
                    adorno.gameObject.SetActive(false);
                    Transform visual = adorno.parent != null ? adorno.parent.Find(adorno.name + "_Visual_T250") : null;
                    if (visual != null)
                    {
                        visual.gameObject.SetActive(false);
                    }
                    numero++;
                }
            }
            Debug.Log("[Progresion] CreadorCiudadPandillas.T401: " + numero + " carros robables en " + RaizT401 + ".");
        }

        private static void PonerHoraInicial()
        {
            Component reloj = null;
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (MonoBehaviour comportamiento in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (comportamiento != null && comportamiento.GetType().Name == "RelojMundo")
                    {
                        reloj = comportamiento;
                    }
                }
            }
            if (reloj == null)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas: no encuentro RelojMundo en la escena.");
                return;
            }
            SerializedObject soReloj = new SerializedObject(reloj);
            SerializedProperty propiedad = soReloj.FindProperty("config");
            ScriptableObject original = propiedad.objectReferenceValue as ScriptableObject;
            if (original == null)
            {
                Debug.LogWarning("[Progresion] CreadorCiudadPandillas: RelojMundo sin config.");
                return;
            }
            ScriptableObject copia = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaRelojPandillas);
            if (copia == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RutaRelojPandillas));
                copia = Object.Instantiate(original);
                AssetDatabase.CreateAsset(copia, RutaRelojPandillas);
            }
            SerializedObject soCopia = new SerializedObject(copia);
            soCopia.FindProperty("horaInicial").floatValue = HoraInicial;
            soCopia.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(copia);
            propiedad.objectReferenceValue = copia;
            soReloj.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuscarRaiz(string nombre)
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (raiz.name == nombre)
                {
                    return raiz;
                }
            }
            return null;
        }

        private static Transform BuscarTransform(string nombre)
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == nombre)
                    {
                        return t;
                    }
                }
            }
            return null;
        }
    }
}

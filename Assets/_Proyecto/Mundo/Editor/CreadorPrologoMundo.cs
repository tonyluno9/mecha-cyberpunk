using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-092: lo que el prólogo de Tony (D-tony-72) espera del barrio de Alan, sin programar nada de su lado. En Barrio.prefab pone:
    /// los puntos de historia "Prologo_Carro" (frente al clandestino) y "Prologo_Esquina_1" (el cruce de regreso a casa), las apariciones
    /// "Casa_Cama" y "Clandestino_Salida", los prefabs de Tony CamaJugador y EspejoJugador en los puntos de uso de la cama y el espejo de la casa
    /// (con su geometría greybox apagada: se ven los props de Alan). MostradorPeleas y el resto del prólogo (jugador, director, mecha, emboscada)
    /// van en Ciudad.unity con el mecha estacionado (T-093): su flujo de pelea avisa si no encuentra al jugador.
    /// Corre después de VestirFinales y se puede repetir (usa CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorPrologoMundo.Crear
    /// </summary>
    public static class CreadorPrologoMundo
    {
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaPrefabs = "Assets/_Proyecto/Progresion/Prologo/Prefabs/";
        private const string RutaCama = RutaPrefabs + "CamaJugador.prefab";
        private const string RutaEspejo = RutaPrefabs + "EspejoJugador.prefab";

        private const float CochesAlLado = 7f;           // m desde la Entrada del clandestino, a lo largo de la calle
        private const float SaliendoDeLaBanqueta = 4f;   // m hacia la calle, para que el carro quede junto a la banqueta
        private const float EsquinaX = -47f;             // el cruce de la calle norte-sur del oeste con la calle este-oeste
        private const float AlturaBanqueta = 0.2f;

        [MenuItem("Mecha Cyberpunk/Crear T-092")]
        public static void Crear()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform casa = Buscar(raiz.transform, "Lugar_Casa");
            Transform clandestino = Buscar(raiz.transform, "Lugar_Clandestino");
            Transform puntoCama = Buscar(casa, "Prop_Cama").Find("PuntoUso");
            Transform puntoEspejo = Buscar(casa, "Prop_Espejo").Find("PuntoUso");

            // Puntos de historia: el carro queda junto a la banqueta frente al clandestino y la ruta de regreso gira en el cruce del oeste.
            Transform historia = Reemplazar(raiz.transform, "PuntosHistoria");
            Transform entradaClandestino = clandestino.Find("Entrada");
            Vector3 carro = entradaClandestino.position + entradaClandestino.rotation * Vector3.forward * SaliendoDeLaBanqueta + entradaClandestino.rotation * Vector3.right * CochesAlLado;
            PuntoDeHistoria(historia, "Prologo_Carro", carro, Quaternion.LookRotation(entradaClandestino.rotation * Vector3.right, Vector3.up));
            PuntoDeHistoria(historia, "Prologo_Esquina_1", new Vector3(EsquinaX, AlturaBanqueta, 0f), Quaternion.LookRotation(Vector3.back, Vector3.up));

            // Apariciones a pie: despertar junto a la cama y salir del clandestino a la calle.
            Transform apariciones = raiz.transform.Find("PuntosAparicion");
            Quitar(apariciones, "Casa_Cama");
            Quitar(apariciones, "Clandestino_Salida");
            Aparicion(apariciones, "Casa_Cama", puntoCama.position, puntoCama.rotation);
            Aparicion(apariciones, "Clandestino_Salida", entradaClandestino.position, entradaClandestino.rotation);

            // Prefabs de Tony: cama y espejo en sus puntos de uso, con su geometría greybox apagada.
            Transform prologoCasa = Reemplazar(casa, "Prologo");
            GameObject cama = Colocar(RutaCama, prologoCasa, puntoCama.position, puntoCama.rotation);
            ApagarGeometria(cama, null);
            GameObject espejo = Colocar(RutaEspejo, prologoCasa, puntoEspejo.position, puntoEspejo.rotation);
            ApagarGeometria(espejo, "Marco");


            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-092 creada: puntos de historia, apariciones y prefabs del prólogo de Tony.");
        }

        private static Transform Buscar(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            throw new System.InvalidOperationException("No existe " + nombre + " en el barrio; corre los creadores anteriores antes.");
        }

        private static Transform Reemplazar(Transform padre, string nombre)
        {
            Quitar(padre, nombre);
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            return go.transform;
        }

        private static void Quitar(Transform padre, string nombre)
        {
            Transform viejo = padre.Find(nombre);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
        }

        private static void PuntoDeHistoria(Transform padre, string id, Vector3 posicion, Quaternion rotacion)
        {
            GameObject go = new GameObject(id);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(posicion, rotacion);
            PuntoHistoriaMundo punto = go.AddComponent<PuntoHistoriaMundo>();
            SerializedObject so = new SerializedObject(punto);
            so.FindProperty("id").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Aparicion(Transform padre, string id, Vector3 posicion, Quaternion rotacion)
        {
            GameObject go = new GameObject(id);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(posicion, rotacion);
            PuntoAparicion punto = go.AddComponent<PuntoAparicion>();
            SerializedObject so = new SerializedObject(punto);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("modo").enumValueIndex = (int)ModoJugador.APie;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Colocar(string ruta, Transform padre, Vector3 posicion, Quaternion rotacion)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                throw new System.InvalidOperationException("No existe el prefab de Tony " + ruta + "; falta mezclar origin/main.");
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.transform.SetPositionAndRotation(posicion, rotacion);
            return go;
        }

        // Apaga mallas y colliders greybox del prefab de Tony para que se vea el prop de Alan; 'solo' limita a los hijos con ese nombre.
        private static void ApagarGeometria(GameObject objeto, string solo)
        {
            foreach (MeshRenderer malla in objeto.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (solo == null || malla.name == solo)
                {
                    malla.enabled = false;
                    Collider collider = malla.GetComponent<Collider>();
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }
                }
            }
        }
    }
}

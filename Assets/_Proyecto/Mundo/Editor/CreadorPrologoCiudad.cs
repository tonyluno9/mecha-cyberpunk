using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-093: Ciudad_Prologo.unity, la ciudad de Alan con el conjunto del prólogo de Tony colocado (D-tony-72): el jugador a pie, el mecha estacionado
    /// en el taller de la casa, la emboscada en el anclaje del callejón, el director del prólogo y el mostrador de peleas del clandestino, y la jaula
    /// de la arena lejos del barrio. Es una copia de Ciudad.unity (que se queda sin jugador para las pruebas con la cámara libre de Alan) más esos
    /// prefabs, sin cambiar sus datos. Tony apunta DatosEscenas_Demo a esta escena (o la renombra) en T-136.
    /// Corre al final, después de CreadorPeleaClandestino, y se puede repetir (rehace la copia).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorPrologoCiudad.Crear
    /// </summary>
    public static class CreadorPrologoCiudad
    {
        public const string RutaOrigen = "Assets/_Proyecto/Escenas/Ciudad.unity";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaEmboscada = "Assets/_Proyecto/Pelea/Prefabs/Emboscada.prefab";
        private const string RutaDirector = "Assets/_Proyecto/Progresion/Prologo/Prefabs/DirectorPrologo.prefab";
        private const string RutaMostrador = "Assets/_Proyecto/Progresion/Prologo/Prefabs/MostradorPeleas.prefab";
        private const string RutaJaula = "Assets/_Proyecto/Arena/Prefabs/JaulaClandestino.prefab";
        private const string IdAparicionInicial = "Casa_Puerta";
        private const string TipoAparicion = "AparicionJugador";
        private const string TipoFlujo = "DirectorPelea";
        private static readonly Vector3 PosicionArena = new Vector3(400f, 0f, 0f);   // como en Pruebas_Alan: a más de 100 m del barrio
        private const float XMostrador = 3.2f;           // m a la derecha del eje, fuera del pasillo de la puerta
        private const float AdelanteDeLaFosa = 1.4f;     // m desde BordeFosa hacia la puerta

        /// <summary>Solo lo del prólogo (puntos de historia, Flashback.unity y Ciudad_Prologo.unity), sin rehacer todo el barrio.</summary>
        [MenuItem("Mecha Cyberpunk/Crear prólogo (T-092, T-090 y T-093)")]
        public static void CrearSoloPrologo()
        {
            CreadorPrologoMundo.Crear();
            CreadorFlashback.Crear();
            Crear();
        }

        [MenuItem("Mecha Cyberpunk/Crear T-093")]
        public static void Crear()
        {
            // Si la escena ya existe se sobrescribe el archivo y se conserva su .meta: así el GUID no cambia cada vez que se rehace.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) != null)
            {
                System.IO.File.Copy(RutaOrigen, RutaEscena, true);
                AssetDatabase.ImportAsset(RutaEscena, ImportAssetOptions.ForceUpdate);
            }
            else if (!AssetDatabase.CopyAsset(RutaOrigen, RutaEscena))
            {
                throw new System.InvalidOperationException("No pude copiar " + RutaOrigen + " a " + RutaEscena);
            }
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);

            Transform taller = Buscar("TallerMecha");
            Transform anclaje = Buscar("Emboscada_Anclaje");
            Transform bordeFosa = Buscar("BordeFosa");
            Transform clandestino = Buscar("Lugar_Clandestino");
            Transform apCasa = Buscar(IdAparicionInicial);

            // El mecha descansa en el Estacionamiento del taller (Tony lo vuelve a colocar solo al arrancar).
            Transform estacionamiento = taller.Find("Estacionamiento");
            GameObject mecha = Instanciar(RutaMecha);
            mecha.transform.SetPositionAndRotation(estacionamiento.position, estacionamiento.rotation);

            // El carro espera donde dice el punto Prologo_Carro (junto al clandestino), mirando a la calle del regreso; nadie más lo mueve.
            Transform carro = Buscar("CarroViejo");
            Transform puntoCarro = Buscar("Prologo_Carro");
            carro.SetPositionAndRotation(new Vector3(puntoCarro.position.x, carro.position.y, puntoCarro.position.z), Quaternion.LookRotation(Vector3.left, Vector3.up));

            GameObject emboscada = Instanciar(RutaEmboscada);
            emboscada.transform.SetPositionAndRotation(anclaje.position, anclaje.rotation);

            GameObject jaula = Instanciar(RutaJaula);
            jaula.transform.position = PosicionArena;

            GameObject jugador = Instanciar(RutaJugador);
            jugador.transform.SetPositionAndRotation(apCasa.position + Vector3.up * 0.05f, apCasa.rotation);
            Component aparicion = Componente(jugador, TipoAparicion);
            SerializedObject soAparicion = new SerializedObject(aparicion);
            soAparicion.FindProperty("idAparicion").stringValue = IdAparicionInicial;
            soAparicion.ApplyModifiedPropertiesWithoutUndo();

            GameObject director = Instanciar(RutaDirector);
            Componente(director, TipoFlujo, true).transform.position = PosicionArena;

            GameObject mostrador = Instanciar(RutaMostrador);
            mostrador.transform.SetPositionAndRotation(
                bordeFosa.position + clandestino.right * XMostrador + clandestino.forward * AdelanteDeLaFosa, clandestino.rotation);
            Componente(mostrador, TipoFlujo, true).transform.position = PosicionArena;

            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-093 creada: Ciudad_Prologo.unity con el conjunto del prólogo de Tony.");
        }

        private static GameObject Instanciar(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                throw new System.InvalidOperationException("No existe el prefab " + ruta + "; falta mezclar origin/main.");
            }
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        private static Transform Buscar(string nombre)
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
            throw new System.InvalidOperationException("Ciudad no trae '" + nombre + "'; corre los creadores anteriores antes.");
        }

        // Busca un componente por el nombre de su tipo, porque Mundo no referencia los ensamblados de Tony.
        private static Component Componente(GameObject objeto, string tipo, bool enHijos = false)
        {
            Component[] todos = enHijos ? objeto.GetComponentsInChildren<Component>(true) : objeto.GetComponents<Component>();
            foreach (Component c in todos)
            {
                if (c != null && c.GetType().Name == tipo)
                {
                    return c;
                }
            }
            throw new System.InvalidOperationException(objeto.name + " no trae un componente " + tipo + ".");
        }
    }
}

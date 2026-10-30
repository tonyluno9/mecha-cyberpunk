using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-413 del creador de la ciudad de pandillas: bajo GTA_T413 la cámara lenta del título (<see cref="CamaraTitulo"/>, que también decide
    /// si la escena abre en el título), el <see cref="ModeloSegunPerfil"/> que viste al jugador como A o B, y las pantallas del título y de
    /// la creación (las hace <see cref="CreadorPantallaTitulo"/>). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorCiudadPandillas.T413
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT413 = "GTA_T413";
        private const string MenuTituloEnEditor = "Mecha Cyberpunk/Pandillas/Título en el editor";
        private const float VelocidadCamaraTitulo = 2f;
        private const float AlturaCamaraTitulo = 12f;
        private const float LargoRecorrido = 115f;   // de -115 a 115 m en X, sin salir del barrio (mide 268 m)
        private const float CarrilRecorrido = 3f;    // ida por z = -3 y regreso por z = 3

        /// <summary>Viste los muñecos, crea los prefabs de UI y pone GTA_T413 en Ciudad_Prologo.</summary>
        [MenuItem("Mecha Cyberpunk/Pandillas/T-413 Título y creación")]
        public static void T413()
        {
            CreadorPantallaTitulo.Crear();
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T413Interno();
            Guardar(escena);
        }

        [MenuItem(MenuTituloEnEditor)]
        private static void AlternarTituloEnEditor()
        {
            bool valor = !SessionState.GetBool(FlujoPartida.ClaveTituloEnEditor, false);
            SessionState.SetBool(FlujoPartida.ClaveTituloEnEditor, valor);
            FlujoPartida.TituloEnEditor = valor;
            Debug.Log("[Progresion] Título en el editor: " + (valor ? "sí" : "no"));
        }

        [MenuItem(MenuTituloEnEditor, true)]
        private static bool ValidarTituloEnEditor()
        {
            Menu.SetChecked(MenuTituloEnEditor, SessionState.GetBool(FlujoPartida.ClaveTituloEnEditor, false));
            return true;
        }

        private static void T413Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT413);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT413);

            GameObject objetoCamara = new GameObject("CamaraTitulo", typeof(Camera));
            objetoCamara.transform.SetParent(raiz.transform, false);
            Camera camara = objetoCamara.GetComponent<Camera>();
            camara.depth = 10f;
            CamaraTitulo camaraTitulo = objetoCamara.AddComponent<CamaraTitulo>();
            Vector3[] puntos = Recorrido();
            camaraTitulo.Configurar(puntos, VelocidadCamaraTitulo, AlturaCamaraTitulo);
            EditorUtility.SetDirty(camaraTitulo);

            ModeloSegunPerfil modelo = raiz.AddComponent<ModeloSegunPerfil>();
            modelo.Configurar(new[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>(CreadorPantallaTitulo.RutaModeloA),
                AssetDatabase.LoadAssetAtPath<GameObject>(CreadorPantallaTitulo.RutaModeloB),
            });
            EditorUtility.SetDirty(modelo);

            foreach (string ruta in new[] { CreadorPantallaTitulo.RutaTitulo, CreadorPantallaTitulo.RutaCreacion })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab == null)
                {
                    Debug.LogError("[Progresion] CreadorCiudadPandillas.T413: falta " + ruta + "; corre antes CreadorPantallaTitulo.Crear.");
                    continue;
                }
                GameObject pantalla = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                pantalla.name = prefab.name;
            }
            Debug.Log("[Progresion] CreadorCiudadPandillas.T413: título con " + puntos.Length + " puntos de recorrido: " + string.Join(" ", puntos));
        }

        // La vuelta va por la calle principal (corre en X con z = 0 y 14 m de ancho, CreadorBarrio de Alan): de ida por un carril y de
        // regreso por el otro, para que a 12 m la cámara no atraviese edificios.
        private static Vector3[] Recorrido()
        {
            return new[]
            {
                new Vector3(-LargoRecorrido, 0f, -CarrilRecorrido), new Vector3(LargoRecorrido, 0f, -CarrilRecorrido),
                new Vector3(LargoRecorrido, 0f, CarrilRecorrido), new Vector3(-LargoRecorrido, 0f, CarrilRecorrido),
            };
        }
    }
}

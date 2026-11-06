using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// Dónde está la escena de la pantalla de inicio y cómo se carga una escena de forma asíncrona (en el editor las escenas que no
    /// están en Build Settings se cargan con <c>LoadSceneAsyncInPlayMode</c>). En el build la escena 0 es Inicio.unity.
    /// </summary>
    public static class EscenaInicio
    {
        public const string Ruta = "Assets/_Proyecto/Escenas/Inicio.unity";

        /// <summary>True si el build trae la escena de inicio.</summary>
        public static bool EstaEnBuild => SceneUtility.GetBuildIndexByScenePath(Ruta) >= 0;

        /// <summary>
        /// El título viejo de Ciudad_Prologo se cambia por la pantalla de inicio solo en un build que la trae; en el editor la ciudad
        /// conserva su título (las pruebas y las escenas de prueba no cambian).
        /// </summary>
        public static bool DebeVolverAlInicio => !Application.isEditor && EstaEnBuild;

        /// <summary>Empieza a cargar la escena sin activarla (<c>allowSceneActivation = false</c>).</summary>
        public static AsyncOperation CargarAsincrono(string ruta)
        {
            AsyncOperation operacion;
#if UNITY_EDITOR
            operacion = EditorSceneManager.LoadSceneAsyncInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
#else
            operacion = SceneManager.LoadSceneAsync(ruta, LoadSceneMode.Single);
#endif
            if (operacion != null)
            {
                operacion.allowSceneActivation = false;
            }
            return operacion;
        }
    }
}

using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Pasa de una escena a otra sin perder la partida (T-124): guarda antes de cambiar y deja marcada la transición para que el
    /// <see cref="ServicioGuardado"/> de la escena nueva cargue al arrancar.
    /// </summary>
    public static class CambioEscena
    {
        private static bool transicionPendiente;
        private static Action<string> interceptor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            transicionPendiente = false;
            interceptor = null;
        }

        /// <summary>Guarda la partida (si hay servicio), marca la transición y carga la escena por ruta.</summary>
        public static void Ir(string ruta)
        {
            if (string.IsNullOrEmpty(ruta))
            {
                Debug.LogWarning("[Progresion] CambioEscena.Ir sin ruta; no se hace nada.");
                return;
            }
            ServicioGuardado servicio = Registro<ServicioGuardado>.Actual;
            if (servicio != null) servicio.Guardar();
            transicionPendiente = true;
            if (interceptor != null)
            {
                interceptor(ruta);
                return;
            }
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneAsyncInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadSceneAsync(ruta);
#endif
        }

        /// <summary>True una sola vez tras una transición.</summary>
        public static bool ConsumirTransicion()
        {
            bool era = transicionPendiente;
            transicionPendiente = false;
            return era;
        }

        /// <summary>Pruebas: recibe la ruta pedida en vez de cargar la escena. Null lo quita.</summary>
        public static void Interceptar(Action<string> receptor)
        {
            interceptor = receptor;
        }
    }
}

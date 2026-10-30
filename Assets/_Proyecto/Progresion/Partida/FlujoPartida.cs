using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MechaCyberpunk.Progresion.Partida
{
    /// <summary>
    /// Empezar, cargar o ir al título (T-412, D-tony-244): vuelve a cargar Ciudad_Prologo y deja pendiente qué hacer; el
    /// <c>ServicioGuardado</c> de la escena nueva lo consume al arrancar (carga la ranura, o no carga nada si es nueva).
    /// No guarda antes de cambiar, a diferencia de <see cref="CambioEscena"/>. <see cref="IrAlTitulo"/> por ahora es una partida nueva
    /// (T-413 le pone el título). <see cref="Interceptar"/> es para pruebas, como en CambioEscena.
    /// </summary>
    public static class FlujoPartida
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        private static AccionPartida pendiente;
        private static int ranuraPendiente;
        private static Action<string> interceptor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            pendiente = AccionPartida.Ninguna;
            ranuraPendiente = 0;
            interceptor = null;
        }

        /// <summary>Lo que quedó pendiente (sin consumirlo).</summary>
        public static AccionPartida Pendiente => pendiente;

        public static int RanuraPendiente => ranuraPendiente;

        public static void IrANuevaPartida() => Ir(AccionPartida.Nueva, 0);

        /// <summary>Carga la escena y, al arrancar, la ranura dada (0 = AUTOGUARDADO).</summary>
        public static void IrACargar(int ranura) => Ir(AccionPartida.Cargar, ranura);

        /// <summary>Por ahora igual que una partida nueva (T-413 le pone el título).</summary>
        public static void IrAlTitulo() => Ir(AccionPartida.Titulo, 0);

        /// <summary>Devuelve lo pendiente y lo borra. False si no había nada.</summary>
        public static bool ConsumirPendiente(out AccionPartida accion, out int ranura)
        {
            accion = pendiente;
            ranura = ranuraPendiente;
            pendiente = AccionPartida.Ninguna;
            ranuraPendiente = 0;
            return accion != AccionPartida.Ninguna;
        }

        /// <summary>Pruebas: recibe la ruta pedida en vez de cargar la escena. Null lo quita.</summary>
        public static void Interceptar(Action<string> receptor)
        {
            interceptor = receptor;
        }

        private static void Ir(AccionPartida accion, int ranura)
        {
            pendiente = accion;
            ranuraPendiente = ranura;
            // Lo que dejara pendiente un cambio de escena no aplica: manda la acción de aquí.
            CambioEscena.ConsumirTransicion();
            if (interceptor != null)
            {
                interceptor(RutaEscena);
                return;
            }
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadSceneAsync(RutaEscena);
#endif
        }
    }
}

using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
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
    /// No guarda antes de cambiar, a diferencia de <see cref="CambioEscena"/>. <see cref="Interceptar"/> es para pruebas, como en CambioEscena.
    /// T-413 (D-tony-245): <see cref="EnTitulo"/> dice si Ciudad_Prologo abrió en el título (lo decide <see cref="DecidirTitulo"/> desde
    /// <c>CamaraTitulo.Awake</c>); <see cref="IrANuevaPartida(IReadOnlyDictionary{string, string})"/> lleva las piezas que llenó la creación
    /// (el perfil) a la escena nueva y se las entrega a cada <see cref="IGuardable"/> de esa clave al registrarse.
    /// </summary>
    public static class FlujoPartida
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        private static AccionPartida pendiente;
        private static int ranuraPendiente;
        private static Action<string> interceptor;
        private static readonly Dictionary<string, string> piezasNuevas = new Dictionary<string, string>();
        private static bool yaArranco;
#if UNITY_EDITOR
        public const string ClaveTituloEnEditor = "Cyberfix.TituloEnEditor";
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            pendiente = AccionPartida.Ninguna;
            ranuraPendiente = 0;
            interceptor = null;
            EnTitulo = false;
            yaArranco = false;
            DejarDeEscuchar();
            piezasNuevas.Clear();
#if UNITY_EDITOR
            TituloEnEditor = UnityEditor.SessionState.GetBool(ClaveTituloEnEditor, false);
#else
            TituloEnEditor = false;
#endif
        }

        /// <summary>
        /// En el editor, abrir Ciudad_Prologo sin nada pendiente sale en el título solo si esto es true (lo ponen la prueba y el menú
        /// Mecha Cyberpunk/Pandillas/Título en el editor); así las pruebas viejas no cambian. En un build no se usa.
        /// </summary>
        public static bool TituloEnEditor { get; set; }

        /// <summary>True mientras la escena está en el título: HUD, minimapa y pausa apagados, jugador oculto y la historia esperando.</summary>
        public static bool EnTitulo { get; private set; }

        /// <summary>
        /// Lo llama la escena al despertar (antes de que ServicioGuardado consuma lo pendiente): título si se pidió con
        /// <see cref="IrAlTitulo"/>, o si no hay nada pendiente y es la primera escena de un build (en el editor, con <see cref="TituloEnEditor"/>).
        /// </summary>
        public static bool DecidirTitulo()
        {
            bool primera = !yaArranco;
            yaArranco = true;
            if (pendiente == AccionPartida.Titulo)
            {
                EnTitulo = true;
            }
            else if (pendiente != AccionPartida.Ninguna)
            {
                EnTitulo = false;
            }
            else
            {
                EnTitulo = Application.isEditor ? TituloEnEditor : primera;
            }
            return EnTitulo;
        }

        /// <summary>Lo que quedó pendiente (sin consumirlo).</summary>
        public static AccionPartida Pendiente => pendiente;

        public static int RanuraPendiente => ranuraPendiente;

        public static void IrANuevaPartida() => Ir(AccionPartida.Nueva, 0);

        /// <summary>
        /// Partida nueva con piezas ya llenas (clave → datos, como en el guardado): al registrarse en la escena nueva, cada
        /// <see cref="IGuardable"/> de esa clave las recibe con <c>Cargar</c>. La creación de personaje manda así el perfil.
        /// </summary>
        public static void IrANuevaPartida(IReadOnlyDictionary<string, string> piezas)
        {
            piezasNuevas.Clear();
            if (piezas != null)
            {
                foreach (KeyValuePair<string, string> pieza in piezas)
                {
                    if (!string.IsNullOrEmpty(pieza.Key) && pieza.Value != null)
                    {
                        piezasNuevas[pieza.Key] = pieza.Value;
                    }
                }
            }
            // Quitar antes de poner: Registros.LimpiarTodos (pruebas) borra los suscriptores sin avisar.
            Registro<IGuardable>.Agregado -= EntregarPieza;
            if (piezasNuevas.Count > 0)
            {
                Registro<IGuardable>.Agregado += EntregarPieza;
            }
            Ir(AccionPartida.Nueva, 0);
        }

        /// <summary>Cuántas piezas de la partida nueva faltan por entregar (pruebas).</summary>
        public static int PiezasPorEntregar => piezasNuevas.Count;

        /// <summary>Carga la escena y, al arrancar, la ranura dada (0 = AUTOGUARDADO).</summary>
        public static void IrACargar(int ranura) => Ir(AccionPartida.Cargar, ranura);

        /// <summary>Vuelve a cargar Ciudad_Prologo en el título (T-413).</summary>
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

        private static void EntregarPieza(IGuardable guardable)
        {
            if (guardable == null || !piezasNuevas.TryGetValue(guardable.ClaveGuardado, out string datos))
            {
                return;
            }
            piezasNuevas.Remove(guardable.ClaveGuardado);
            guardable.Cargar(datos);
            if (piezasNuevas.Count == 0)
            {
                DejarDeEscuchar();
            }
        }

        private static void DejarDeEscuchar() => Registro<IGuardable>.Agregado -= EntregarPieza;

        private static void Ir(AccionPartida accion, int ranura)
        {
            if (accion != AccionPartida.Nueva)
            {
                piezasNuevas.Clear();
                DejarDeEscuchar();
            }
            EnTitulo = false;
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

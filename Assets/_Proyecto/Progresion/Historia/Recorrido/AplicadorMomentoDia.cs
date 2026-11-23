using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.MenuPausa;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T68: hace que todo el juego sea de noche (o de día, si el jugador lo pidió en OPCIONES, MOMENTO DEL DÍA). Vive solo, una vez, y cada cuadro
    /// revisa el reloj del mundo: si su hora cae fuera del momento elegido (<see cref="ReglaMomentoDia"/>), lo salta a la hora del momento. El reloj sigue
    /// corriendo, así los días pasan; solo se salta la parte de día (o de noche). Al cambiar el momento DURANTE una partida: pantalla negra, se cierra la
    /// pausa, se aplica la luz, y vuelve con el jugador tirado que se levanta del suelo (el mismo gesto del inicio). En el título solo se aplica la luz.
    /// </summary>
    public class AplicadorMomentoDia : MonoBehaviour
    {
        public const int OrdenCanvas = 30002;
        public const float SegundosFundido = 0.45f;
        public const float SegundosVolver = 0.8f;
        public const float SegundosDeEsperaLevantado = 1.8f;
        private const int CuadrosParaLaLuz = 3;

        private static AplicadorMomentoDia actual;

        private Coroutine transicion;
        private Canvas canvas;
        private Image negro;

        /// <summary>True mientras corre la transición (negro, luz y levantarse).</summary>
        public static bool EnTransicion => actual != null && actual.transicion != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => actual = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Crear()
        {
            if (actual != null)
            {
                return;
            }
            GameObject go = new GameObject("AplicadorMomentoDia");
            DontDestroyOnLoad(go);
            actual = go.AddComponent<AplicadorMomentoDia>();
        }

        private void OnEnable() => ReglaMomentoDia.Cambio += AlCambiarMomento;

        private void OnDisable() => ReglaMomentoDia.Cambio -= AlCambiarMomento;

        private void Update()
        {
            if (transicion == null)
            {
                Asegurar();
            }
        }

        /// <summary>Salta el reloj a la hora del momento si ahora marca otra. Devuelve true si lo movió.</summary>
        public static bool Asegurar()
        {
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj == null)
            {
                return false;
            }
            MomentoDelDia momento = ReglaMomentoDia.Actual;
            if (!ReglaMomentoDia.HayQueVolver(momento, reloj.Hora, reloj.HoraAmanecer, reloj.HoraAnochecer))
            {
                return false;
            }
            reloj.AvanzarHasta(ReglaMomentoDia.HoraDe(momento));
            return true;
        }

        private void AlCambiarMomento(MomentoDelDia momento, bool porJugador)
        {
            if (!porJugador || transicion != null)
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            bool hayPartida = !FlujoPartida.EnTitulo && jugador != null && jugador.Raiz != null && Registro<IRelojMundo>.Actual != null;
            if (!hayPartida)
            {
                return; // en el título o sin jugador solo se aplica la luz (lo hace Update)
            }
            transicion = StartCoroutine(Transicion());
        }

        private IEnumerator Transicion()
        {
            CrearNegro();
            yield return Fundir(0f, 1f, SegundosFundido);

            PantallaPausa pausa = FindFirstObjectByType<PantallaPausa>(FindObjectsInactive.Include);
            if (pausa != null && pausa.Abierta)
            {
                pausa.Cerrar();
            }

            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null)
            {
                reloj.Pausar(false);
                Asegurar();
            }
            for (int i = 0; i < CuadrosParaLaLuz; i++)
            {
                yield return null;
            }

            IJugador jugador = Registro<IJugador>.Actual;
            Transform raiz = jugador != null ? jugador.Raiz : null;
            ControladorAPie control = raiz != null ? raiz.GetComponentInChildren<ControladorAPie>(true) : null;
            VisualPersonaje visual = raiz != null ? raiz.GetComponentInChildren<VisualPersonaje>(true) : null;
            control?.ActivarControl(false);
            if (visual != null)
            {
                DirectorRecorrido.DejarTiradoEnElSuelo(visual);
            }

            yield return Fundir(1f, 0f, SegundosVolver);
            if (visual != null)
            {
                yield return new WaitForSecondsRealtime(ReglasAnimacion.RetardoLevantarse);
                visual.Caido = false;
                yield return new WaitForSecondsRealtime(SegundosDeEsperaLevantado);
            }
            control?.ActivarControl(true);
            QuitarNegro();
            transicion = null;
        }

        private void CrearNegro()
        {
            if (canvas != null)
            {
                return;
            }
            GameObject go = new GameObject("NegroMomentoDia", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = OrdenCanvas;
            go.AddComponent<GraphicRaycaster>();
            GameObject fondo = new GameObject("Negro", typeof(RectTransform), typeof(Image));
            fondo.transform.SetParent(go.transform, false);
            RectTransform rect = (RectTransform)fondo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            negro = fondo.GetComponent<Image>();
            negro.color = new Color(0f, 0f, 0f, 0f);
            negro.raycastTarget = true; // mientras tapa, ningún clic llega al menú de abajo
        }

        private void QuitarNegro()
        {
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
                negro = null;
            }
        }

        private IEnumerator Fundir(float de, float a, float segundos)
        {
            float t = 0f;
            while (t < segundos)
            {
                t += Time.unscaledDeltaTime;
                negro.color = new Color(0f, 0f, 0f, Mathf.Lerp(de, a, Mathf.Clamp01(t / segundos)));
                yield return null;
            }
            negro.color = new Color(0f, 0f, 0f, a);
        }
    }
}

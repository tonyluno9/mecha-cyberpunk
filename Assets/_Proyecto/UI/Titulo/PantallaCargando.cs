using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Titulo
{
    /// <summary>
    /// T-537: pantalla de "CARGANDO…" que sobrevive al cambio de escena (CONTINUAR recarga la ciudad y en el build tarda unos segundos:
    /// antes la pantalla quedaba vacía y parecía que "desaparecía todo"). Se quita sola cuando el jugador de la escena nueva se registra
    /// (o a los <see cref="SegundosMaximos"/> s).
    /// </summary>
    public class PantallaCargando : MonoBehaviour
    {
        public const string TextoCargando = "CARGANDO…";
        public const int OrdenCanvas = 32000;
        private const float SegundosMaximos = 30f;
        private const float SegundosMinimos = 0.6f;

        private static PantallaCargando actual;
        private float inicio;
        private bool jugadorNuevo;

        public static PantallaCargando Actual => actual;
        public static bool Visible => actual != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => actual = null;

        public static PantallaCargando Mostrar()
        {
            if (actual != null)
            {
                return actual;
            }
            GameObject raiz = new GameObject("PantallaCargando");
            DontDestroyOnLoad(raiz);
            actual = raiz.AddComponent<PantallaCargando>();
            Canvas canvas = KitUI.CrearCanvas(raiz.transform, "CanvasCargando", OrdenCanvas);
            // No tapa los clics: solo se ve.
            GraphicRaycaster rayos = canvas.GetComponent<GraphicRaycaster>();
            if (rayos != null)
            {
                Destroy(rayos);
            }
            Image fondo = KitUI.CrearFondoPantalla(canvas.transform, "Fondo");
            fondo.color = new Color(0.02f, 0.02f, 0.05f, 1f);
            Text texto = KitUI.CrearTexto(canvas.transform, "Texto", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Estirar(texto.rectTransform);
            texto.text = TextoCargando;
            texto.color = KitUI.Estilo.Turquesa;
            return actual;
        }

        private void Awake()
        {
            inicio = Time.realtimeSinceStartup;
        }

        private void OnEnable()
        {
            Registro<IJugador>.Agregado += AlLlegarJugador;
        }

        private void OnDisable()
        {
            Registro<IJugador>.Agregado -= AlLlegarJugador;
        }

        private void AlLlegarJugador(IJugador jugador) => jugadorNuevo = true;

        private void Update()
        {
            float t = Time.realtimeSinceStartup - inicio;
            if ((jugadorNuevo && t >= SegundosMinimos) || t >= SegundosMaximos)
            {
                actual = null;
                Destroy(gameObject);
            }
        }
    }
}

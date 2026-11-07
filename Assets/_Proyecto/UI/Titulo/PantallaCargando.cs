using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Titulo
{
    /// <summary>
    /// La pantalla de carga (T-537, rehecha con la pantalla de inicio): fondo animado, logo, consejos que rotan y una barra de progreso con
    /// porcentaje. Sobrevive al cambio de escena. Dos modos: con <see cref="Asignar"/> sigue una carga asíncrona real (la barra suaviza
    /// el avance de Unity y la escena se activa cuando la barra llega al final, ver <see cref="ReglaInicio"/>); sin operación avanza
    /// sola hacia el 90 % (recarga de la ciudad) y se completa cuando el jugador de la escena nueva se registra. Se desvanece al terminar.
    /// </summary>
    public class PantallaCargando : MonoBehaviour
    {
        public const string TextoCargando = "CARGANDO";
        public const string TextoConsejo = "CONSEJO";
        public const int OrdenCanvas = 32000;
        public const float SegundosMaximos = 90f;
        public const float SegundosMinimos = 2.2f;
        public const float SegundosPorConsejo = 4.5f;
        private const float SegundosSimulados = 14f;
        private const float VelocidadBarra = 0.55f;
        private const float SegundosDesvanecer = 0.55f;
        private const float SegundosPuntos = 0.35f;
        private const float AnchoBarra = 1200f;

        private static PantallaCargando actual;

        private AsyncOperation operacion;
        private CanvasGroup grupo;
        private RectTransform relleno;
        private RectTransform cabeza;
        private Text porcentaje;
        private Text titulo;
        private Text consejo;
        private float inicio;
        private float mostrado;
        private float desvanecer = -1f;
        private int indiceConsejo;
        private float segundosConsejo;
        private bool jugadorNuevo;
        private bool activada;

        public static PantallaCargando Actual => actual;
        public static bool Visible => actual != null;

        /// <summary>Lo que se ve de la barra, de 0 a 1.</summary>
        public float Progreso => mostrado;
        public string ConsejoActual => consejo != null ? consejo.text : string.Empty;
        public string TextoPorcentaje => porcentaje != null ? porcentaje.text : string.Empty;
        public bool ActivoAsincrono => operacion != null;
        public bool EscenaActivada => activada;
        public float Segundos => Time.realtimeSinceStartup - inicio;

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
            actual.Armar(raiz.transform);
            return actual;
        }

        /// <summary>
        /// Sigue una carga asíncrona real. Esta pantalla la deja sin activar y la activa cuando la barra termina de llenarse.
        /// </summary>
        public void Asignar(AsyncOperation nueva)
        {
            operacion = nueva;
            activada = false;
            if (nueva != null)
            {
                nueva.allowSceneActivation = false;
            }
        }

        private void Armar(Transform raiz)
        {
            Canvas canvas = KitUI.CrearCanvas(raiz, "CanvasCargando", OrdenCanvas);
            // No tapa los clics: solo se ve.
            GraphicRaycaster rayos = canvas.GetComponent<GraphicRaycaster>();
            if (rayos != null)
            {
                Destroy(rayos);
            }
            grupo = canvas.gameObject.AddComponent<CanvasGroup>();
            grupo.blocksRaycasts = false;
            grupo.interactable = false;
            FondoCarga.Crear(canvas.transform);
            LogoInicio logo = LogoInicio.Crear(canvas.transform, NombreJuego.Nombre, 130, new Vector2(110f, -90f));
            logo.name = "LogoCarga";

            titulo = UIInicio.Texto(canvas.transform, "Titulo", TextoCargando, UIInicio.FuenteTitulos, 64, UIInicio.Blanco, TextAnchor.LowerLeft);
            UIInicio.Punto(titulo.rectTransform, new Vector2(0.5f, 0f), new Vector2(-AnchoBarra * 0.5f + 260f, 190f), new Vector2(520f, 80f));
            UIInicio.Halo(titulo, UIInicio.Cian, 0.45f, 3f);

            Text etiqueta = UIInicio.Texto(canvas.transform, "EtiquetaConsejo", TextoConsejo, UIInicio.FuenteTitulos, 34, UIInicio.Naranja, TextAnchor.MiddleCenter);
            UIInicio.Punto(etiqueta.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 400f), new Vector2(600f, 44f));
            consejo = UIInicio.Texto(canvas.transform, "Consejo", ConsejosCarga.Obtener(0), UIInicio.FuenteTexto, 40, UIInicio.Blanco, TextAnchor.MiddleCenter);
            UIInicio.Punto(consejo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(1500f, 90f));
            consejo.horizontalOverflow = HorizontalWrapMode.Wrap;

            Image pista = UIInicio.Imagen(canvas.transform, "PistaBarra", new Color(1f, 1f, 1f, 0.14f));
            UIInicio.Anclar(pista.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(-AnchoBarra * 0.5f, 150f), new Vector2(AnchoBarra, 8f));
            Image llena = UIInicio.Imagen(pista.transform, "Relleno", UIInicio.Cian, SpritesInicio.Blanco, true);
            UIInicio.Anclar(llena.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 8f));
            relleno = llena.rectTransform;
            Image brillo = UIInicio.Imagen(pista.transform, "Cabeza", UIInicio.ConAlfa(Color.white, 0.9f), SpritesInicio.Brillo, true);
            UIInicio.Anclar(brillo.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
            cabeza = brillo.rectTransform;
            porcentaje = UIInicio.Texto(canvas.transform, "Porcentaje", "0%", UIInicio.FuenteTitulos, 56, UIInicio.Blanco, TextAnchor.LowerRight);
            UIInicio.Punto(porcentaje.rectTransform, new Vector2(0.5f, 0f), new Vector2(AnchoBarra * 0.5f - 150f, 190f), new Vector2(300f, 70f));
            Aplicar();
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

        private void OnDestroy()
        {
            if (actual == this)
            {
                actual = null;
            }
        }

        private void AlLlegarJugador(IJugador jugador) => jugadorNuevo = true;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float t = Segundos;
            segundosConsejo += dt;
            if (segundosConsejo >= SegundosPorConsejo)
            {
                segundosConsejo = 0f;
                indiceConsejo = ReglaInicio.SiguienteIndice(indiceConsejo, ConsejosCarga.Textos.Length);
                consejo.text = ConsejosCarga.Obtener(indiceConsejo);
            }
            titulo.text = ReglaInicio.TextoConPuntos(TextoCargando, t, SegundosPuntos);

            if (desvanecer >= 0f)
            {
                desvanecer += dt;
                grupo.alpha = 1f - Mathf.Clamp01(desvanecer / SegundosDesvanecer);
                if (desvanecer >= SegundosDesvanecer)
                {
                    Destroy(gameObject);
                }
                return;
            }

            if (operacion != null)
            {
                float objetivo = operacion.isDone || activada ? 1f : ReglaInicio.ProgresoDeCarga(operacion.progress);
                mostrado = ReglaInicio.Suavizar(mostrado, objetivo, dt, VelocidadBarra);
                if (!activada && ReglaInicio.PuedeActivar(operacion.progress, mostrado, t, SegundosMinimos))
                {
                    activada = true;
                    operacion.allowSceneActivation = true;
                }
                if (activada && jugadorNuevo && operacion.isDone)
                {
                    Terminar();
                }
            }
            else
            {
                float objetivo = jugadorNuevo ? 1f : ReglaInicio.ProgresoSimulado(t, SegundosSimulados);
                mostrado = ReglaInicio.Suavizar(mostrado, objetivo, dt, VelocidadBarra);
                if (jugadorNuevo && t >= SegundosMinimos && mostrado >= 0.999f)
                {
                    Terminar();
                }
            }
            if (t >= SegundosMaximos)
            {
                Terminar();
            }
            Aplicar();
        }

        private void Terminar()
        {
            if (desvanecer < 0f)
            {
                desvanecer = 0f;
                mostrado = 1f;
                Aplicar();
                actual = null;
            }
        }

        private void Aplicar()
        {
            if (relleno == null)
            {
                return;
            }
            relleno.sizeDelta = new Vector2(AnchoBarra * mostrado, relleno.sizeDelta.y);
            cabeza.anchoredPosition = new Vector2(AnchoBarra * mostrado, 0f);
            porcentaje.text = ReglaInicio.TextoPorcentaje(mostrado);
        }
    }
}

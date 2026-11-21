using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Titulo
{
    /// <summary>
    /// La pantalla de carga (T-537; minimalista desde el pedido de Tony "carga"): solo la ilustración L01-L06 (llena la pantalla sin
    /// deformarse, con fundido entre fotos), el logo del menú principal chico abajo a la derecha, una frase de consejo con letra fina
    /// abajo a la izquierda y tres puntos discretos que indican que carga. Sin barra, porcentaje, título ni efectos de glitch.
    /// Sobrevive al cambio de escena. Dos modos: con <see cref="Asignar"/> sigue una carga asíncrona real (el progreso interno suaviza
    /// el avance de Unity y la escena se activa cuando llega al final, ver <see cref="ReglaInicio"/>); sin operación avanza sola hacia
    /// el 90 % (recarga de la ciudad) y se completa cuando el jugador de la escena nueva se registra. Se desvanece al terminar.
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
        /// <summary>Ruta en Resources del logo del menú principal (el que eligió Tony; ver MenuInicio).</summary>
        public const string RutaLogo = "Inicio/Logo";
        private const float AnchoLogo = 230f;
        private const float Margen = 56f;
        private const int TamanoConsejo = 26;
        private const float AlfaConsejo = 0.82f;
        private const int CantidadPuntos = 3;
        private const float TamanoPunto = 7f;
        private const float SeparacionPuntos = 15f;
        private const float SegundosCicloPuntos = 1.2f;
        private const float AlfaPuntoMinimo = 0.2f;
        private const float AlfaPuntoMaximo = 0.85f;

        private static PantallaCargando actual;

        private AsyncOperation operacion;
        private CanvasGroup grupo;
        private Image[] puntos;
        private Image logo;
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

        /// <summary>El avance interno de la carga, de 0 a 1 (ya no se dibuja: marca cuándo se activa la escena).</summary>
        public float Progreso => mostrado;
        public string ConsejoActual => consejo != null ? consejo.text : string.Empty;
        public string TextoPorcentaje => ReglaInicio.TextoPorcentaje(mostrado);
        /// <summary>El logo de la carga (el mismo sprite del menú principal).</summary>
        public Image Logo => logo;
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

            // Sombra muy suave al pie para que el consejo se lea sobre cualquier foto (no es un panel: un degradado).
            Image sombra = UIInicio.Imagen(canvas.transform, "SombraPie", UIInicio.ConAlfa(Color.black, 0.45f), SpritesInicio.DegradadoVAbajo);
            UIInicio.Anclar(sombra.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 220f));

            logo = UIInicio.Imagen(canvas.transform, "LogoCarga", Color.white, Resources.Load<Sprite>(RutaLogo));
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            UIInicio.Anclar(logo.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-Margen, Margen), new Vector2(AnchoLogo, AnchoLogo * 0.5f));
            logo.enabled = logo.sprite != null;

            consejo = UIInicio.Texto(canvas.transform, "Consejo", ConsejosCarga.Obtener(0), UIInicio.FuenteTexto, TamanoConsejo, UIInicio.ConAlfa(UIInicio.Blanco, AlfaConsejo), TextAnchor.LowerLeft);
            UIInicio.Anclar(consejo.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(Margen, Margen + 2f), new Vector2(1100f, 60f));
            consejo.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Indicador discreto: tres puntos que se encienden por turno, a la izquierda del logo.
            puntos = new Image[CantidadPuntos];
            for (int i = 0; i < CantidadPuntos; i++)
            {
                Image punto = UIInicio.Imagen(canvas.transform, "Punto" + i, UIInicio.ConAlfa(UIInicio.Blanco, AlfaPuntoMinimo), SpritesInicio.Brillo);
                float x = -Margen - AnchoLogo - 28f - (CantidadPuntos - 1 - i) * SeparacionPuntos;
                UIInicio.Anclar(punto.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(x, Margen + 14f), new Vector2(TamanoPunto, TamanoPunto));
                puntos[i] = punto;
            }
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
            AnimarPuntos(t);

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
            // Sin barra ni porcentaje en pantalla: el avance solo decide cuándo activar la escena.
        }

        private void AnimarPuntos(float t)
        {
            if (puntos == null)
            {
                return;
            }
            float fase = Mathf.Repeat(t / SegundosCicloPuntos, 1f) * CantidadPuntos;
            for (int i = 0; i < puntos.Length; i++)
            {
                float cerca = 1f - Mathf.Clamp01(Mathf.Abs(fase - i - 0.5f));
                puntos[i].color = UIInicio.ConAlfa(UIInicio.Blanco, Mathf.Lerp(AlfaPuntoMinimo, AlfaPuntoMaximo, cerca));
            }
        }
    }
}

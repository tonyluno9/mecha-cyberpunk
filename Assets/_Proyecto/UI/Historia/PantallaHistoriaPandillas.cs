using System;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Historia
{
    /// <summary>
    /// Textos de la historia de pandillas (T-406) con el kit de UI (T-408, canvas propio de orden 20): un subtítulo abajo al centro, una ayuda
    /// arriba bajo el objetivo y la pantalla de "CONTINUARÁ...".
    /// Enter o Esc cierran el continuará (tras un segundo). Tiempo sin escala.
    /// </summary>
    public class PantallaHistoriaPandillas : MonoBehaviour
    {
        public const string TituloContinuara = "CONTINUARÁ...";
        public const string LineaContinuara = "NOVA ARMA A LOS SNAKES. NEÓN ALTO TE ESPERA.";
        public const string TeclasContinuara = "ENTER: SEGUIR JUGANDO";
        private const float SegundosMinimosContinuara = 1f;
        public const int OrdenCanvas = 20;
        private const float AnchoTextos = 1400f;
        private const float AnchoSubtitulo = 960f;
        private const float AltoPorLetra = 1.4f;
        private const float AlturaJuego = 0.84f;
        private const float AlturaTitulo = 0.6f;
        private const float AlturaLinea = 0.44f;
        private const float AlturaTeclas = 0.34f;
        private static readonly Color FondoContinuara = new Color(0f, 0f, 0f, 0.85f);

        private string subtitulo = string.Empty;
        private float finSubtitulo;
        private string ayuda = string.Empty;
        private float finAyuda;
        private float inicioContinuara;
        private Canvas canvas;
        private Text textoSubtitulo;
        private Text textoAyuda;
        private Image fondoContinuara;

        public string Subtitulo => Time.unscaledTime < finSubtitulo ? subtitulo : string.Empty;
        public string Ayuda => Time.unscaledTime < finAyuda ? ayuda : string.Empty;
        public bool ContinuaraVisible { get; private set; }

        /// <summary>Se dispara al cerrar la pantalla de continuará.</summary>
        public event Action ContinuaraCerrada;

        public void MostrarSubtitulo(string texto, float segundos)
        {
            subtitulo = texto;
            finSubtitulo = Time.unscaledTime + segundos;
            Refrescar();
        }

        public void MostrarAyuda(string texto, float segundos)
        {
            ayuda = texto;
            finAyuda = Time.unscaledTime + segundos;
            Refrescar();
        }

        public void MostrarContinuara()
        {
            ContinuaraVisible = true;
            inicioContinuara = Time.unscaledTime;
            PilaPantallas.Abrir(this);
            Refrescar();
        }

        public void CerrarContinuara()
        {
            if (!ContinuaraVisible)
            {
                return;
            }
            ContinuaraVisible = false;
            PilaPantallas.Cerrar(this);
            Refrescar();
            ContinuaraCerrada?.Invoke();
        }

        private void Awake()
        {
            EstiloCyberfix e = KitUI.Estilo;
            canvas = KitUI.CrearCanvas(transform, "CanvasHistoria", OrdenCanvas);
            textoSubtitulo = TextoEnRanura("Subtitulo", RolTexto.Texto, DisposicionHud.Subtitulo, e.Texto, AnchoSubtitulo);
            textoAyuda = TextoEnRanura("Ayuda", RolTexto.Texto, DisposicionHud.AyudaHistoria, e.Turquesa, AnchoTextos);

            fondoContinuara = KitUI.CrearImagen(canvas.transform, "Continuara", FondoContinuara);
            KitUI.Estirar(fondoContinuara.rectTransform);
            Transform c = fondoContinuara.transform;
            TextoCentrado(c, "Juego", RolTexto.Encabezado, e.Turquesa, AlturaJuego).text = NombreJuego.Nombre;
            TextoCentrado(c, "Titulo", RolTexto.Titulo, e.Magenta, AlturaTitulo).text = TituloContinuara;
            TextoCentrado(c, "Linea", RolTexto.Texto, e.Texto, AlturaLinea).text = LineaContinuara;
            TextoCentrado(c, "Teclas", RolTexto.Ayuda, e.Turquesa, AlturaTeclas).text = TeclasContinuara;
            Refrescar();
        }

        private Text TextoEnRanura(string nombre, RolTexto rol, RanuraHud ranura, Color color, float ancho)
        {
            Text texto = KitUI.CrearTexto(canvas.transform, nombre, rol, TextAnchor.MiddleCenter);
            KitUI.Colocar(texto.rectTransform, ranura, ancho);
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.color = color;
            return texto;
        }

        // Centrado en x, a una altura (fracción de la pantalla desde abajo).
        private static Text TextoCentrado(Transform padre, string nombre, RolTexto rol, Color color, float altura)
        {
            Text texto = KitUI.CrearTexto(padre, nombre, rol, TextAnchor.MiddleCenter);
            RectTransform rect = texto.rectTransform;
            rect.anchorMin = new Vector2(0f, altura);
            rect.anchorMax = new Vector2(1f, altura);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, KitUI.Estilo.Tamano(rol) * AltoPorLetra);
            texto.color = color;
            return texto;
        }

        private void OnDestroy() => PilaPantallas.Cerrar(this);

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (ContinuaraVisible && teclado != null && PilaPantallas.EsArriba(this) && Time.unscaledTime - inicioContinuara >= SegundosMinimosContinuara
                && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.escapeKey.wasPressedThisFrame))
            {
                CerrarContinuara();
            }
            Refrescar();
        }

        // El continuará tapa todo; el subtítulo y la ayuda se ven mientras dura su tiempo.
        private void Refrescar()
        {
            if (fondoContinuara == null)
            {
                return;
            }
            Mostrar(fondoContinuara.gameObject, ContinuaraVisible);
            string sub = ContinuaraVisible ? string.Empty : Subtitulo;
            string ayu = ContinuaraVisible ? string.Empty : Ayuda;
            if (textoSubtitulo.text != sub)
            {
                textoSubtitulo.text = sub;
            }
            if (textoAyuda.text != ayu)
            {
                textoAyuda.text = ayu;
            }
            // T-409: con el mapa abierto la ayuda quedaba tenue detrás de su título; se esconde mientras tanto (sigue contando su tiempo).
            bool verAyuda = !MechaCyberpunk.UI.Mapa.PantallaMapa.HayAbierto;
            if (textoAyuda.enabled != verAyuda)
            {
                textoAyuda.enabled = verAyuda;
            }
        }

        private static void Mostrar(GameObject objeto, bool visible)
        {
            if (objeto.activeSelf != visible)
            {
                objeto.SetActive(visible);
            }
        }
    }
}

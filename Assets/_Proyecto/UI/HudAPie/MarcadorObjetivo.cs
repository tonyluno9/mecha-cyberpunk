using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Marcador del objetivo (T-408, D-tony-240): un rombo naranja 2.2 m arriba del objetivo con la distancia turquesa abajo ("45 m").
    /// Si el objetivo queda detrás o fuera de la pantalla, una flecha naranja en el borde (a 64 px) apunta hacia él, con la distancia.
    /// A menos de 3 m se apaga. Suena UI_OBJETIVO_NUEVO al cambiar de objetivo y UI_OBJETIVO_CUMPLIDO al quitarlo. No se ve en el mecha.
    /// Crea su propio canvas (orden 15; anidado si su padre ya es un canvas, como en HudAPie.prefab). La lógica está en <see cref="CalculoMarcador"/>.
    /// </summary>
    public class MarcadorObjetivo : MonoBehaviour
    {
        public const int OrdenCanvas = 15;
        public const string SonidoNuevo = "UI_OBJETIVO_NUEVO";
        public const string SonidoCumplido = "UI_OBJETIVO_CUMPLIDO";
        private const float GradosArriba = 90f;

        [Tooltip("Altura del rombo sobre el objetivo (m).")]
        [Min(0f)] [SerializeField] private float alturaSobreObjetivo = 2.2f;
        [Tooltip("Tamaño del rombo de punta a punta (px a 1920x1080).")]
        [Min(4f)] [SerializeField] private float diagonalRombo = 36f;
        [Tooltip("Distancia de la flecha al borde de la pantalla (px a 1920x1080).")]
        [Min(0f)] [SerializeField] private float margenBorde = 64f;
        [Tooltip("Lado de la flecha del borde (px a 1920x1080).")]
        [Min(4f)] [SerializeField] private float ladoFlecha = 40f;
        [Tooltip("Separación entre el rombo o la flecha y su distancia (px a 1920x1080).")]
        [Min(0f)] [SerializeField] private float separacionDistancia = 34f;
        [Tooltip("Más cerca que esto el marcador se apaga (m).")]
        [Min(0f)] [SerializeField] private float distanciaApagado = 3f;

        private RectTransform lienzo;
        private RectTransform grupoRombo;
        private RectTransform grupoFlecha;
        private RectTransform punta;
        private Text distanciaRombo;
        private Text distanciaFlecha;
        private string ultimaClave;
        private bool primerCuadro = true;

        public bool RomboVisible => grupoRombo != null && grupoRombo.gameObject.activeSelf;
        public bool FlechaVisible => grupoFlecha != null && grupoFlecha.gameObject.activeSelf;

        /// <summary>El rombo (en un canvas Overlay, su <c>position</c> está en px de pantalla).</summary>
        public RectTransform Rombo => grupoRombo;
        public RectTransform Flecha => grupoFlecha;
        public RectTransform Lienzo => lienzo;
        public string TextoDistancia { get; private set; } = string.Empty;

        /// <summary>Hacia dónde apunta la flecha (°, 0 = derecha, 90 = arriba).</summary>
        public float AnguloFlecha { get; private set; }

        /// <summary>Distancia del jugador al objetivo (m), o -1 si no hay.</summary>
        public float Distancia { get; private set; } = -1f;

        private void Awake()
        {
            EstiloCyberfix estilo = KitUI.Estilo;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasMarcador", OrdenCanvas);
            lienzo = (RectTransform)canvas.transform;

            grupoRombo = Grupo("Rombo");
            KitUI.CrearRombo(grupoRombo, "Figura", diagonalRombo, estilo.Naranja);
            distanciaRombo = TextoDistanciaEn(grupoRombo, new Vector2(0f, -separacionDistancia));

            grupoFlecha = Grupo("Flecha");
            punta = (RectTransform)KitUI.CrearTriangulo(grupoFlecha, "Punta", ladoFlecha, estilo.Naranja).rectTransform.parent;
            distanciaFlecha = TextoDistanciaEn(grupoFlecha, Vector2.zero);

            grupoRombo.gameObject.SetActive(false);
            grupoFlecha.gameObject.SetActive(false);
        }

        private RectTransform Grupo(string nombre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(lienzo, false);
            RectTransform rect = (RectTransform)objeto.transform;
            KitUI.Colocar(rect, Vector2.zero, Vector2.zero, Vector2.one * diagonalRombo);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static Text TextoDistanciaEn(RectTransform padre, Vector2 posicion)
        {
            Text texto = KitUI.CrearTexto(padre, "Distancia", RolTexto.Hud, TextAnchor.MiddleCenter);
            texto.color = KitUI.Estilo.Turquesa;
            RectTransform rect = texto.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = new Vector2(KitUI.Estilo.TamanoFila.y * 3f, KitUI.Estilo.AltoFilaHud);
            return texto;
        }

        private void LateUpdate() => Actualizar();

        /// <summary>Recalcula el marcador ya (lo llama también la captura, que cambia el tamaño del lienzo).</summary>
        public void Actualizar()
        {
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            bool hay = objetivos != null && objetivos.HayObjetivo;
            Objetivo actual = hay ? objetivos.Actual : Objetivo.Ninguno;
            Vector3 posicion = Vector3.zero;
            bool conPosicion = hay && UbicacionObjetivo.Resolver(actual, Registro<ILugar>.Todos, out posicion);
            AvisarCambio(hay ? actual.Texto + "|" + actual.Lugar + "|" + (conPosicion ? posicion.ToString() : string.Empty) : null);

            IJugador jugador = Registro<IJugador>.Actual;
            Camera camara = jugador != null ? jugador.Camara : null;
            if (camara == null)
            {
                camara = Camera.main;
            }
            bool visible = conPosicion && jugador != null && jugador.Raiz != null && jugador.Modo != ModoJugador.Mecha && camara != null;
            Distancia = visible ? Vector3.Distance(jugador.Raiz.position, posicion) : -1f;
            if (visible && Distancia < distanciaApagado)
            {
                visible = false;
            }
            if (!visible)
            {
                Mostrar(false, false);
                TextoDistancia = string.Empty;
                return;
            }

            TextoDistancia = CalculoMarcador.TextoDistancia(Distancia);
            Vector3 vista = camara.WorldToViewportPoint(posicion + Vector3.up * alturaSobreObjetivo);
            CalculoMarcador.Resultado r = CalculoMarcador.Calcular(vista, lienzo.rect.size, margenBorde);
            Mostrar(r.EnPantalla, !r.EnPantalla);
            if (r.EnPantalla)
            {
                grupoRombo.anchoredPosition = r.Posicion;
                distanciaRombo.text = TextoDistancia;
                return;
            }
            AnguloFlecha = r.Angulo;
            grupoFlecha.anchoredPosition = r.Posicion;
            punta.localRotation = Quaternion.Euler(0f, 0f, r.Angulo - GradosArriba);
            // La distancia va del lado de adentro de la flecha.
            float rad = r.Angulo * Mathf.Deg2Rad;
            Vector2 haciaAfuera = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            distanciaFlecha.rectTransform.anchoredPosition = -haciaAfuera * (ladoFlecha * 0.5f + separacionDistancia);
            distanciaFlecha.text = TextoDistancia;
        }

        private void Mostrar(bool rombo, bool flecha)
        {
            if (grupoRombo.gameObject.activeSelf != rombo)
            {
                grupoRombo.gameObject.SetActive(rombo);
            }
            if (grupoFlecha.gameObject.activeSelf != flecha)
            {
                grupoFlecha.gameObject.SetActive(flecha);
            }
        }

        private void AvisarCambio(string clave)
        {
            if (primerCuadro)
            {
                primerCuadro = false;
                ultimaClave = clave;
                if (clave != null)
                {
                    Sonidos.Reproducir(SonidoNuevo);
                }
                return;
            }
            if (clave == ultimaClave)
            {
                return;
            }
            Sonidos.Reproducir(clave != null ? SonidoNuevo : SonidoCumplido);
            ultimaClave = clave;
        }
    }
}

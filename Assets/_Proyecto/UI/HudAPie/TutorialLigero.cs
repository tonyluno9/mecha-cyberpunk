using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Tutorial ligero (T-441): la primera vez que el jugador puede hacer cada acción (moverse, apuntar, disparar, recargar, cubrirse,
    /// rodar, subir al carro, robar, mapa y misiones) sale un aviso corto arriba. Se va al hacer la acción o a los pocos segundos y no
    /// vuelve. Lo visto se guarda en las opciones; la opción TUTORIAL lo apaga. Las reglas están en <see cref="ReglaTutorial"/>.
    /// Lo agrega <see cref="HudAPie"/>; construye su propio canvas.
    /// </summary>
    public class TutorialLigero : MonoBehaviour
    {
        public const int OrdenCanvas = 12;
        /// <summary>Si el texto del objetivo contiene esto (sin importar mayúsculas), se ofrece el aviso de robar.</summary>
        public const string PalabraObjetivoRobar = "roba";

        [Header("Tiempos")]
        [Tooltip("Segundos que se ve un aviso si la persona no hace la acción.")]
        [Min(1f)] [SerializeField] private float segundosAviso = 7f;
        [Tooltip("Pausa entre un aviso y el siguiente (s).")]
        [Min(0f)] [SerializeField] private float segundosPausa = 1.5f;

        [Header("Medidas (px a 1920x1080)")]
        [Tooltip("Distancia desde el borde de arriba a la que sale el aviso (px).")]
        [Min(0f)] [SerializeField] private float margenArriba = 150f;
        [Tooltip("Tamaño del cuadro del aviso (px).")]
        [SerializeField] private Vector2 tamanoCuadro = new Vector2(1100f, 64f);

        private ReglaTutorial regla;
        private GameObject cuadro;
        private Text texto;
        private ServicioOpciones servicio;
        private IJugador jugador;
        private Transform raizCacheada;
        private ControladorAPie controlador;
        private PistolaAPie pistola;
        private CubiertaYRodada cubierta;
        private InteraccionAPie interaccion;
        private int vistasGuardadas;

        public ReglaTutorial Regla => regla;
        public bool Visible => cuadro != null && cuadro.activeSelf;
        public Text Texto => texto;

        private void Awake()
        {
            regla = new ReglaTutorial(segundosAviso, segundosPausa);
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasTutorial", OrdenCanvas);
            RectTransform panel = KitUI.CrearPanel(canvas.transform, "Aviso", tamanoCuadro);
            KitUI.Colocar(panel, new Vector2(0.5f, 1f), new Vector2(0f, -margenArriba), tamanoCuadro);
            cuadro = panel.gameObject;
            texto = KitUI.CrearTexto(panel, "Texto", RolTexto.Hud, TextAnchor.MiddleCenter);
            KitUI.Estirar(texto.rectTransform);
            texto.color = KitUI.Estilo.Turquesa;
            cuadro.SetActive(false);
        }

        private void Update()
        {
            TomarServicio();
            regla.Activo = servicio == null || servicio.Tutorial;
            jugador = Registro<IJugador>.Actual;
            bool enJuego = jugador != null && jugador.Raiz != null && !FlujoPartida.EnTitulo && !Pausa.Activa && PilaPantallas.Vacia;
            if (!enJuego)
            {
                regla.Avanzar(0f, Nunca);
                Mostrar();
                return;
            }
            CachearComponentes();
            DetectarAcciones();
            regla.Avanzar(Time.unscaledDeltaTime, Disponible);
            Mostrar();
            GuardarSiCambio();
        }

        private static bool Nunca(AccionTutorial accion) => false;

        private void TomarServicio()
        {
            ServicioOpciones actual = Registro<IOpciones>.Actual as ServicioOpciones;
            if (actual != servicio)
            {
                servicio = actual;
                if (servicio != null)
                {
                    regla.Cargar(servicio.TutorialVisto);
                    vistasGuardadas = regla.CantidadVistas;
                }
            }
        }

        private void CachearComponentes()
        {
            if (jugador.Raiz == raizCacheada)
            {
                return;
            }
            raizCacheada = jugador.Raiz;
            controlador = raizCacheada.GetComponentInChildren<ControladorAPie>();
            pistola = raizCacheada.GetComponentInChildren<PistolaAPie>();
            cubierta = raizCacheada.GetComponentInChildren<CubiertaYRodada>();
            interaccion = raizCacheada.GetComponentInChildren<InteraccionAPie>();
        }

        // Lo que la persona hizo este cuadro: se da por visto aunque el aviso no estuviera en pantalla.
        private void DetectarAcciones()
        {
            Keyboard teclado = Keyboard.current;
            Mouse raton = Mouse.current;
            if (jugador.Modo == ModoJugador.Carro)
            {
                regla.Hecha(AccionTutorial.SubirAlCarro);
                if (ObjetivoDeRobar())
                {
                    regla.Hecha(AccionTutorial.Robar);
                }
                return;
            }
            if (jugador.Modo != ModoJugador.APie)
            {
                return;
            }
            if (teclado != null)
            {
                if (teclado.wKey.wasPressedThisFrame || teclado.aKey.wasPressedThisFrame || teclado.sKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame)
                {
                    regla.Hecha(AccionTutorial.Moverse);
                }
                if (teclado.mKey.wasPressedThisFrame)
                {
                    regla.Hecha(AccionTutorial.Mapa);
                }
                if (teclado.tKey.wasPressedThisFrame)
                {
                    regla.Hecha(AccionTutorial.Misiones);
                }
            }
            if (raton != null && raton.leftButton.wasPressedThisFrame)
            {
                regla.Hecha(AccionTutorial.Disparar);
            }
            if (controlador != null && controlador.Apuntando)
            {
                regla.Hecha(AccionTutorial.Apuntar);
            }
            if (pistola != null && pistola.Recargando)
            {
                regla.Hecha(AccionTutorial.Recargar);
            }
            if (cubierta != null && cubierta.Cubierto)
            {
                regla.Hecha(AccionTutorial.Cubrirse);
            }
            if (cubierta != null && cubierta.Rodando)
            {
                regla.Hecha(AccionTutorial.Rodar);
            }
        }

        // Si la acción se puede hacer ahora mismo.
        private bool Disponible(AccionTutorial accion)
        {
            if (jugador.Modo != ModoJugador.APie)
            {
                return false;
            }
            switch (accion)
            {
                case AccionTutorial.SubirAlCarro:
                    return HayCarroCerca();
                case AccionTutorial.Robar:
                    return HayCarroCerca() && ObjetivoDeRobar();
                case AccionTutorial.Cubrirse:
                case AccionTutorial.Rodar:
                    return cubierta != null;
                case AccionTutorial.Apuntar:
                case AccionTutorial.Disparar:
                case AccionTutorial.Recargar:
                    return pistola != null;
                default:
                    return true;
            }
        }

        private bool HayCarroCerca() => interaccion != null && interaccion.TipoCandidato == TipoCandidatoAPie.Montable;

        private static bool ObjetivoDeRobar()
        {
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            return objetivos != null && objetivos.HayObjetivo && objetivos.Actual.Texto != null
                && objetivos.Actual.Texto.IndexOf(PalabraObjetivoRobar, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Mostrar()
        {
            if (regla.HayAviso == cuadro.activeSelf)
            {
                if (regla.HayAviso)
                {
                    texto.text = ReglaTutorial.Texto(regla.Aviso);
                }
                return;
            }
            if (regla.HayAviso)
            {
                texto.text = ReglaTutorial.Texto(regla.Aviso);
            }
            cuadro.SetActive(regla.HayAviso);
        }

        private void GuardarSiCambio()
        {
            if (servicio != null && regla.CantidadVistas != vistasGuardadas)
            {
                vistasGuardadas = regla.CantidadVistas;
                servicio.FijarTutorialVisto(regla.Serializar());
            }
        }
    }
}

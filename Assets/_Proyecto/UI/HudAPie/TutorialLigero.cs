using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
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
    /// rodar, subir al carro, robar, mapa y misiones) sale un aviso corto abajo, al centro y pequeño (T-523). Se va al hacer la acción o a los pocos segundos y no
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
        [Tooltip("Distancia desde el borde de abajo a la que sale el aviso (px). T-523: abajo al centro, pequeño.")]
        [Min(0f)] [SerializeField] private float margenAbajo = 40f;
        [Tooltip("Tamaño del cuadro del aviso (px).")]
        [SerializeField] private Vector2 tamanoCuadro = new Vector2(760f, 40f);

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
        private ConsumiblesJugador consumibles;
        private GolpeAPie golpe;
        private ArmasAPie armas;
        private HabilidadesProtesis habilidades;
        private IEscudo escudo;
        private int aperturasRuedaVistas = MechaCyberpunk.UI.Rueda.RuedaArmasObjetos.Aperturas;
        private int vistasGuardadas;

        public ReglaTutorial Regla => regla;
        public bool Visible => cuadro != null && cuadro.activeSelf;
        public Text Texto => texto;

        private void Awake()
        {
            regla = new ReglaTutorial(segundosAviso, segundosPausa);
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasTutorial", OrdenCanvas);
            RectTransform panel = KitUI.CrearImagen(canvas.transform, "Aviso", new Color(0.02f, 0.02f, 0.05f, 0.55f)).rectTransform;
            KitUI.Colocar(panel, new Vector2(0.5f, 0f), new Vector2(0f, margenAbajo), tamanoCuadro);
            cuadro = panel.gameObject;
            texto = KitUI.CrearTexto(panel, "Texto", RolTexto.Ayuda, TextAnchor.MiddleCenter);
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
            consumibles = raizCacheada.GetComponentInChildren<ConsumiblesJugador>();
            golpe = raizCacheada.GetComponentInChildren<GolpeAPie>();
            armas = raizCacheada.GetComponentInChildren<ArmasAPie>();
            habilidades = raizCacheada.GetComponentInChildren<HabilidadesProtesis>();
            IEscudo nuevoEscudo = Registro<IEscudo>.Actual;
            if (nuevoEscudo != escudo)
            {
                if (escudo != null)
                {
                    escudo.Absorbio -= AlAbsorberEscudo;
                }
                escudo = nuevoEscudo;
                if (escudo != null)
                {
                    escudo.Absorbio += AlAbsorberEscudo;
                }
            }
        }

        private void OnDisable()
        {
            if (escudo != null)
            {
                escudo.Absorbio -= AlAbsorberEscudo;
                escudo = null;
            }
        }

        // El escudo absorbió un golpe o una bala: ya vio para qué sirve.
        private void AlAbsorberEscudo(float cantidad) => regla.Hecha(AccionTutorial.Escudo);

        /// <summary>
        /// T-718: la tecla real de cada acción del aviso, con GetBindingDisplayString del Input System (nunca un texto fijo): lo que el jugador cambió en sus controles es lo que ve.
        /// Lo que no es de una acción del mapa APie (agacharse, cubrirse, objetos, mapa) sale de la acción o la tecla que usa el componente.
        /// </summary>
        public string TeclaReal(string nombre)
        {
            string omision = ReglaTutorial.TeclaPorOmision(nombre);
            switch (nombre)
            {
                case ReglaTutorial.TeclaMover:
                    return TeclasHud.TextoMover(TeclasHud.Buscar("Mover"), omision);
                case ReglaTutorial.TeclaAgacharse:
                    return TeclasHud.Texto(cubierta != null ? cubierta.AccionAgachar : null, omision);
                case ReglaTutorial.TeclaCubrirse:
                    return TeclasHud.Texto(cubierta != null ? cubierta.AccionCubrirse : null, omision);
                case ReglaTutorial.TeclaRueda:
                    return TeclasHud.Texto(TeclasHud.Buscar("Rueda"), omision);
                case ReglaTutorial.TeclaMapa:
                    return Keyboard.current != null ? TeclasHud.Nombre(Keyboard.current.mKey.displayName) : omision;
                default:
                    return TeclasHud.Texto(TeclasHud.Buscar(nombre), omision);
            }
        }

        // Lo que la persona hizo este cuadro: se da por visto aunque el aviso no estuviera en pantalla.
        private void DetectarAcciones()
        {
            Keyboard teclado = Keyboard.current;
            Mouse raton = Mouse.current;
            if (jugador.Modo == ModoJugador.Carro)
            {
                regla.Hecha(AccionTutorial.SubirAlCarro);
                if (MontableOcupadoEsMoto())
                {
                    regla.Hecha(AccionTutorial.Moto);
                }
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
            // T-718: el clic con los puños es un golpe, no un disparo: Disparar solo cuenta con un arma de fuego en la mano.
            if (raton != null && raton.leftButton.wasPressedThisFrame && ArmaDeFuegoEnLaMano())
            {
                regla.Hecha(AccionTutorial.Disparar);
            }
            if (controlador != null && controlador.Apuntando)
            {
                regla.Hecha(AccionTutorial.Apuntar);
            }
            if (controlador != null && controlador.PidiendoCorrer && teclado != null
                && (teclado.wKey.isPressed || teclado.aKey.isPressed || teclado.sKey.isPressed || teclado.dKey.isPressed))
            {
                regla.Hecha(AccionTutorial.Correr);
            }
            if (cubierta != null && cubierta.AgachadoLibre)
            {
                regla.Hecha(AccionTutorial.Agacharse);
            }
            if (golpe != null && golpe.Fase != FaseGolpe.Listo)
            {
                regla.Hecha(AccionTutorial.Golpear);
            }
            if (MechaCyberpunk.UI.Rueda.RuedaArmasObjetos.Aperturas != aperturasRuedaVistas)
            {
                aperturasRuedaVistas = MechaCyberpunk.UI.Rueda.RuedaArmasObjetos.Aperturas;
                regla.Hecha(AccionTutorial.Consumibles);
            }
            if (habilidades != null && (habilidades.Estado(HabilidadProtesis.Piernas) != EstadoHabilidad.Lista
                || habilidades.Estado(HabilidadProtesis.Brazos) != EstadoHabilidad.Lista || habilidades.Estado(HabilidadProtesis.Cabeza) != EstadoHabilidad.Lista))
            {
                regla.Hecha(AccionTutorial.Habilidades);
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
                case AccionTutorial.Moto:
                    return HayMotoCerca();
                case AccionTutorial.Cubrirse:
                case AccionTutorial.Rodar:
                case AccionTutorial.Agacharse:
                    return cubierta != null;
                case AccionTutorial.Golpear:
                    return golpe != null;
                case AccionTutorial.Correr:
                    return controlador != null;
                case AccionTutorial.Consumibles:
                    return consumibles != null && consumibles.Disponibles.Count > 0;
                case AccionTutorial.Escudo:
                    return escudo != null && escudo.Maximo > 0f;
                case AccionTutorial.Habilidades:
                    return habilidades != null && (habilidades.Nivel(HabilidadProtesis.Piernas) > 0 || habilidades.Nivel(HabilidadProtesis.Brazos) > 0
                        || habilidades.Nivel(HabilidadProtesis.Cabeza) > 0);
                case AccionTutorial.Apuntar:
                case AccionTutorial.Disparar:
                case AccionTutorial.Recargar:
                    return pistola != null && ArmaDeFuegoEnLaMano();
                default:
                    return true;
            }
        }

        // Sin arma de fuego en la mano (puños) no tiene caso decir cómo apuntar, disparar o recargar.
        private bool ArmaDeFuegoEnLaMano() => armas == null || armas.ArmaActiva != ArmaAPie.Punos;

        private const float MetrosMotoCerca = 5f;
        private const string PalabraMoto = "moto";

        // T-718: la moto que se puede abordar a pie está a pocos metros.
        private bool HayMotoCerca()
        {
            foreach (IMontable montable in Registro<IMontable>.Todos)
            {
                if (montable != null && !montable.Ocupado && montable.Modo == ModoJugador.Carro && EsMoto(montable)
                    && (montable.PuntoSubida - jugador.Raiz.position).sqrMagnitude <= MetrosMotoCerca * MetrosMotoCerca)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool MontableOcupadoEsMoto()
        {
            foreach (IMontable montable in Registro<IMontable>.Todos)
            {
                if (montable != null && montable.Ocupado && montable.Modo == ModoJugador.Carro && EsMoto(montable))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool EsMoto(IMontable montable) =>
            montable.Nombre != null && montable.Nombre.IndexOf(PalabraMoto, System.StringComparison.OrdinalIgnoreCase) >= 0;

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
                    texto.text = TextoAviso();
                }
                return;
            }
            if (regla.HayAviso)
            {
                texto.text = TextoAviso();
            }
            cuadro.SetActive(regla.HayAviso);
        }

        // T-992: el aviso de habilidades solo nombra las que el jugador tiene.
        private string TextoAviso()
        {
            if (regla.Aviso == AccionTutorial.Habilidades && habilidades != null)
            {
                return ReglaTutorial.TextoHabilidades(TeclaReal, habilidades.Nivel(HabilidadProtesis.Piernas) > 0,
                    habilidades.Nivel(HabilidadProtesis.Brazos) > 0, habilidades.Nivel(HabilidadProtesis.Cabeza) > 0);
            }
            return ReglaTutorial.Texto(regla.Aviso, TeclaReal);
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

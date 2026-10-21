using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Peleas;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo
{
    /// <summary>
    /// El mostrador de peleas del clandestino (T-129, D-tony-66): un <see cref="IInteractuable"/> donde el jugador a pie pulsa E para
    /// pelear la pelea casera con su mecha de la historia. Lleva dentro su <see cref="EntradaArena"/> y su flujo casero (hijo).
    /// El texto y el permiso salen, en este orden: entrada en curso o jugador no a pie, etapa de la historia, hora del reloj y estado del mecha.
    /// </summary>
    [RequireComponent(typeof(EntradaArena))]
    public class MostradorPeleas : MonoBehaviour, IInteractuable
    {
        [SerializeField] private EntradaArena entrada;
        [Tooltip("Flujo de la pelea casera (FlujoPeleaCasera, hijo de este objeto).")]
        [SerializeField] private DirectorPelea flujo;
        [Tooltip("Premio que paga el flujo: sirve para mostrar el monto en el aviso.")]
        [SerializeField] private DatosPremioPelea premio;
        [SerializeField] private string textoOfrecer = "Pelea casera · premio {0}";
        [SerializeField] private string textoSinPremio = "Pelea casera";
        [SerializeField] private string textoSinPelea = "Hoy no hay peleas caseras";
        [Tooltip("{0} = hora a la que abre el clandestino.")]
        [SerializeField] private string textoDeDia = "Peleas caseras de noche · abre {0}";
        [SerializeField] private string textoMechaNoListo = "Tu mecha no está listo · repáralo en el taller";

        [Tooltip("Opcional: pantalla de apuesta (PantallaApuesta, hija de este objeto). Sin ella, E pelea directo.")]
        [SerializeField] private PantallaApuesta pantallaApuesta;

        [Tooltip("Opcional (T-159): el calendario de las ligas. Sin él, el mostrador solo ofrece la pelea casera.")]
        [SerializeField] private CalendarioLigas calendario;
        [SerializeField] private string textoSinAlias = "???";

        [Tooltip("Opcional (T-160): la cartelera (PantallaCartelera, hija de este objeto). Con ella y con ligas, E abre la cartelera en vez de la apuesta.")]
        [SerializeField] private PantallaCartelera pantallaCartelera;
        [Tooltip("Alias del rival de la pelea casera, para su fila de la cartelera (el creador lo copia del rival del flujo).")]
        [SerializeField] private string aliasCasera = "";
        [SerializeField] private string textoCartelera = "Peleas del clandestino";

        private OpcionCartelera opcionActual = OpcionCasera;
        private bool registrado;
        private bool apuestaAbierta;
        private bool carteleraAbierta;
        private List<OpcionCartelera> opcionesCartelera;
        private ControladorAPie controlCongelado;
        private readonly Dictionary<DatosApuestaPelea, DatosApuestaPelea> apuestasSinPieza = new Dictionary<DatosApuestaPelea, DatosApuestaPelea>();

        /// <summary>True mientras la pantalla de apuesta está abierta.</summary>
        public bool ApuestaAbierta => apuestaAbierta;

        /// <summary>True mientras la pantalla de la cartelera está abierta.</summary>
        public bool CarteleraAbierta => carteleraAbierta;

        private static OpcionCartelera OpcionCasera => new OpcionCartelera(TipoOpcionCartelera.Casera, 0, 0, false, 0);

        /// <summary>La pelea que se peleó la última vez (o la que está en curso).</summary>
        public OpcionCartelera UltimaOpcion { get; private set; } = OpcionCasera;

        /// <summary>
        /// Las peleas que ofrece el mostrador. Con calendario, ligas y la historia en Libre (o sin historia): siguiente, revanchas,
        /// casera y aviso de la liga que sigue. Si no, solo la casera.
        /// </summary>
        public IReadOnlyList<OpcionCartelera> Opciones
        {
            get
            {
                ServicioLigas ligas = Registro<ServicioLigas>.Actual;
                int premioCasera = premio != null ? premio.PremioGanar : 0;
                if (!HayLigas)
                {
                    return new List<OpcionCartelera> { new OpcionCartelera(TipoOpcionCartelera.Casera, 0, 0, false, premioCasera) };
                }
                return ReglaCartelera.Armar(ligas.CantidadLigas, ligas.RivalesPorLiga, ligas.LigaActual, ligas.Siguiente,
                    FraccionRevancha(ligas), premioCasera, (liga, indice) => PremioDelCartel(liga, indice));
            }
        }

        // Las ligas se ofrecen con calendario, servicio y la historia en Libre (o sin historia).
        private bool HayLigas
        {
            get
            {
                IHistoria historia = Registro<IHistoria>.Actual;
                return calendario != null && Registro<ServicioLigas>.Actual != null && (historia == null || historia.Etapa == EtapaPrologo.Libre);
            }
        }

        private bool UsaCartelera => pantallaCartelera != null && HayLigas;

        private static float FraccionRevancha(ServicioLigas ligas) => ligas.Datos != null ? ligas.Datos.FraccionPremioRevancha : 0.5f;

        private int PremioDelCartel(int liga, int indice)
        {
            CartelPelea cartel = calendario.Cartel(liga, indice);
            return cartel != null ? cartel.premioGanar : 0;
        }

        /// <summary>Alias del rival de la opción (del DatosRival del prefab de su cartel); "???" si no se sabe. En el aviso, el jefe que cierra la liga actual.</summary>
        public string AliasDe(OpcionCartelera opcion)
        {
            if (calendario == null)
            {
                return textoSinAlias;
            }
            int liga = opcion.Tipo == TipoOpcionCartelera.Bloqueada ? opcion.Liga - 1 : opcion.Liga;
            if (opcion.Tipo == TipoOpcionCartelera.Casera)
            {
                return textoSinAlias;
            }
            CartelPelea cartel = calendario.Cartel(liga, opcion.Indice);
            RivalBasico rival = cartel != null && cartel.prefabRival != null ? cartel.prefabRival.GetComponentInChildren<RivalBasico>(true) : null;
            return rival != null && rival.Datos != null ? rival.Datos.Alias : textoSinAlias;
        }

        /// <summary>Premio de ganar que se muestra de la opción (en la revancha, con la fracción).</summary>
        public int PremioDe(OpcionCartelera opcion) => opcion.Premio;

        /// <summary>Se avisa al terminar la pelea y la salida, con su resultado (para el director de la demo).</summary>
        public event Action<ResultadoPelea> PeleaTerminada;

        public Vector3 Posicion => transform.position;

        public bool PuedeUsar => !apuestaAbierta && !carteleraAbierta && Decidir(out _);

        public string Texto
        {
            get
            {
                Decidir(out string texto);
                return texto;
            }
        }

        private void Awake()
        {
            if (entrada == null)
            {
                entrada = GetComponent<EntradaArena>();
            }
            if (entrada == null || flujo == null)
            {
                Debug.LogError("[Progresion] Faltan referencias en MostradorPeleas de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            entrada.Terminada += AlTerminarEntrada;
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            entrada.Terminada -= AlTerminarEntrada;
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
            if (carteleraAbierta)
            {
                CerrarCartelera(true);
            }
            if (apuestaAbierta)
            {
                CerrarApuesta(true);
                flujo.CancelarApuesta();
                QuitarCartelDeLiga();
            }
        }

        private void AlTerminarEntrada(ResultadoPelea resultado)
        {
            OpcionCartelera peleada = opcionActual;
            UltimaOpcion = peleada;
            if (peleada.Tipo == TipoOpcionCartelera.Siguiente && resultado.Ganador == LadoPelea.Jugador)
            {
                Registro<ServicioLigas>.Actual?.RegistrarVictoria(peleada.Liga, peleada.Indice);
            }
            flujo.QuitarCartel();
            opcionActual = OpcionCasera;
            PeleaTerminada?.Invoke(resultado);
        }

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            if (UsaCartelera)
            {
                AbrirCartelera();
                return;
            }
            opcionActual = OpcionCasera;
            UltimaOpcion = opcionActual;
            if (!AbrirApuesta())
            {
                entrada.Empezar(flujo);
            }
        }

        /// <summary>
        /// Reta la pelea elegida de <see cref="Opciones"/> (T-159): solo si el mostrador se puede usar y la opción está en la lista y
        /// no es el aviso de la liga cerrada. Arma el cartel del calendario y sigue el camino de siempre (apuesta o pelea directa).
        /// Si algo falla, quita el cartel y no cobra nada.
        /// </summary>
        public bool Retar(OpcionCartelera opcion)
        {
            if (!PuedeUsar || opcion.Tipo == TipoOpcionCartelera.Bloqueada || !Opciones.Contains(opcion))
            {
                return false;
            }
            if (opcion.Tipo == TipoOpcionCartelera.Casera)
            {
                flujo.QuitarCartel();
            }
            else
            {
                ServicioLigas ligas = Registro<ServicioLigas>.Actual;
                CartelPelea cartel = calendario.Cartel(opcion.Liga, opcion.Indice);
                if (opcion.Tipo == TipoOpcionCartelera.Revancha)
                {
                    cartel = SinPiezaEnJuego(cartel);
                }
                float fraccion = opcion.Tipo == TipoOpcionCartelera.Revancha ? FraccionRevancha(ligas) : 1f;
                if (cartel == null || !flujo.PrepararCartel(cartel, fraccion))
                {
                    flujo.QuitarCartel();
                    return false;
                }
            }
            opcionActual = opcion;
            UltimaOpcion = opcion;
            if (AbrirApuesta() || entrada.Empezar(flujo))
            {
                return true;
            }
            flujo.QuitarCartel();
            opcionActual = OpcionCasera;
            return false;
        }

        // T-171 (D-tony-121): en una revancha de la cartelera no hay pieza en juego. Se arma un cartel igual con una copia en ejecución
        // de la apuesta sin pieza (se guarda por apuesta y se destruye al destruirse el mostrador).
        private CartelPelea SinPiezaEnJuego(CartelPelea cartel)
        {
            if (cartel == null || cartel.apuesta == null || cartel.apuesta.PiezaEnJuego == null)
            {
                return cartel;
            }
            if (!apuestasSinPieza.TryGetValue(cartel.apuesta, out DatosApuestaPelea copia) || copia == null)
            {
                DatosApuestaPelea o = cartel.apuesta;
                copia = DatosApuestaPelea.Crear(o.Momio, o.ApuestaMaxima, o.Montos, null, 0);
                copia.name = o.name + " (revancha)";
                apuestasSinPieza[o] = copia;
            }
            return CartelPelea.Crear(cartel.prefabRival, cartel.premioGanar, cartel.premioPerder, cartel.liga, cartel.esJefe, copia,
                cartel.presentador);
        }

        private void OnDestroy()
        {
            foreach (DatosApuestaPelea copia in apuestasSinPieza.Values)
            {
                if (copia != null)
                {
                    Destroy(copia);
                }
            }
            apuestasSinPieza.Clear();
        }

        // T-160: la cartelera congela el control como la apuesta; Enter reta la opción elegida y Esc cierra sin cobrar.
        private void AbrirCartelera()
        {
            ServicioLigas ligas = Registro<ServicioLigas>.Actual;
            opcionesCartelera = new List<OpcionCartelera>(Opciones);
            var filas = new List<FilaCartelera>(opcionesCartelera.Count);
            foreach (OpcionCartelera opcion in opcionesCartelera)
            {
                bool casera = opcion.Tipo == TipoOpcionCartelera.Casera;
                string alias = casera ? (string.IsNullOrEmpty(aliasCasera) ? textoSinAlias : aliasCasera) : AliasDe(opcion);
                filas.Add(pantallaCartelera.ArmarFila(opcion, ligas.NombreLiga(opcion.Liga), alias, ligas.RivalesPorLiga));
            }
            string encabezado = pantallaCartelera.TextoEncabezado(ligas.NombreLiga(ligas.LigaActual), ligas.CampeonesVencidos, ligas.CantidadLigas);
            carteleraAbierta = true;
            pantallaCartelera.Mostrar(encabezado, filas, AlElegirEnCartelera, AlSalirDeLaCartelera);

            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            ControladorAPie controlador = jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
        }

        private void AlElegirEnCartelera(int indice)
        {
            OpcionCartelera elegida = opcionesCartelera[indice];
            CerrarCartelera(true, false);
            if (!Retar(elegida))
            {
                DevolverControlDespues();
                return;
            }
            if (!apuestaAbierta)
            {
                // Peleó directo: subir al mecha apaga el control y bajar lo devuelve.
                controlCongelado = null;
            }
        }

        private void AlSalirDeLaCartelera() => CerrarCartelera(false);

        // inmediato = devolver ya el control; si no, un cuadro después (la E que cierra es la que abre, D-tony-67).
        private void CerrarCartelera(bool inmediato, bool devolverControl = true)
        {
            pantallaCartelera.Ocultar();
            carteleraAbierta = false;
            if (!devolverControl)
            {
                return;
            }
            if (inmediato)
            {
                DevolverControl();
            }
            else
            {
                DevolverControlDespues();
            }
        }

        // T-145: con datos de apuesta y un servicio de apuestas, E anuncia la pelea y abre la pantalla en vez de pelear directo.
        private bool AbrirApuesta()
        {
            DatosApuestaPelea datos = flujo.Apuesta;
            IApuestasPelea apuestas = Registro<IApuestasPelea>.Actual;
            if (pantallaApuesta == null || datos == null || apuestas == null || !flujo.AnunciarApuesta())
            {
                return false;
            }
            apuestaAbierta = true;
            int premioGanar = opcionActual.Tipo == TipoOpcionCartelera.Casera ? (premio != null ? premio.PremioGanar : 0) : opcionActual.Premio;
            pantallaApuesta.Mostrar(apuestas.Rival, premioGanar, apuestas.Momio, datos.Montos, datos.PiezaEnJuego,
                datos.ApuestaMinimaPorPieza, apuestas.PuedeApostar, AlPelear, AlSalirDeLaApuesta);

            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            ControladorAPie controlador = jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
            return true;
        }

        private void AlPelear(int monto)
        {
            IApuestasPelea apuestas = Registro<IApuestasPelea>.Actual;
            if (monto > 0 && (apuestas == null || !apuestas.Apostar(monto)))
            {
                pantallaApuesta.MostrarAviso(pantallaApuesta.TextoNoAlcanza);
                return;
            }
            pantallaApuesta.Ocultar();
            apuestaAbierta = false;
            if (entrada.Empezar(flujo))
            {
                // El control sigue apagado: subir al mecha lo apaga y bajar lo devuelve.
                controlCongelado = null;
                return;
            }
            // No empezó (por ejemplo, sin mecha): se devuelve lo apostado y el control.
            flujo.CancelarApuesta();
            QuitarCartelDeLiga();
            DevolverControlDespues();
        }

        private void AlSalirDeLaApuesta()
        {
            CerrarApuesta(false);
            flujo.CancelarApuesta();
            QuitarCartelDeLiga();
        }

        private void QuitarCartelDeLiga()
        {
            flujo.QuitarCartel();
            opcionActual = OpcionCasera;
        }

        // La E que cierra es la misma que abre: el control vuelve un cuadro después para que no reabra la pantalla (D-tony-67).
        private void CerrarApuesta(bool inmediato)
        {
            pantallaApuesta.Ocultar();
            apuestaAbierta = false;
            if (inmediato)
            {
                DevolverControl();
            }
            else
            {
                DevolverControlDespues();
            }
        }

        private void DevolverControlDespues()
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(EsperarYDevolverControl());
            }
            else
            {
                DevolverControl();
            }
        }

        private IEnumerator EsperarYDevolverControl()
        {
            yield return null;
            DevolverControl();
        }

        private void DevolverControl()
        {
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
        }

        // Decide si se puede usar y qué dice el aviso. Texto vacío = no se ofrece.
        private bool Decidir(out string texto)
        {
            texto = string.Empty;
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (entrada.EnCurso || jugador == null || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }

            IHistoria historia = Registro<IHistoria>.Actual;
            if (historia != null && historia.Etapa != EtapaPrologo.PeleaCasera && historia.Etapa != EtapaPrologo.Libre)
            {
                texto = textoSinPelea;
                return false;
            }

            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null && !reloj.EsDeNoche)
            {
                texto = string.Format(textoDeDia, HorarioLugar.TextoHora(reloj.HoraAnochecer));
                return false;
            }

            if (!MechaListo())
            {
                texto = textoMechaNoListo;
                return false;
            }

            if (UsaCartelera)
            {
                texto = textoCartelera;
                return true;
            }
            texto = premio != null ? string.Format(textoOfrecer, FormatoDinero.Texto(premio.PremioGanar)) : textoSinPremio;
            return true;
        }

        // La pelea casera se puede perder: con el mecha en KO (o que no se puede subir) no se ofrece hasta repararlo.
        private static bool MechaListo()
        {
            foreach (IMontable candidato in Registro<IMontable>.Todos)
            {
                if (candidato != null && candidato.Modo == ModoJugador.Mecha)
                {
                    return candidato.PuedeSubir;
                }
            }
            return false;
        }
    }
}

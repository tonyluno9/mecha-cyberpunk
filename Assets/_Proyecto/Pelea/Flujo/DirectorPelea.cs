using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Datos;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Pelea.Flujo
{
    /// <summary>
    /// Lleva una pelea completa (conteo, pelea, KO, resultado y revancha) entre el jugador y el rival en cualquier arena que
    /// cumpla <see cref="IArenaPelea"/>. Encuentra la arena por <c>Registro&lt;IArenaPelea&gt;</c> (la más cercana a él), al
    /// jugador como el <see cref="IPeleador"/> del lado Jugador y crea al rival desde su prefab, salvo que se asignen a mano.
    /// Trata al rival solo como <see cref="IPeleador"/>: el rival busca a su objetivo por su cuenta (T-021).
    /// </summary>
    public class DirectorPelea : MonoBehaviour
    {
        [Tooltip("Ritmo y textos del flujo.")]
        [SerializeField] private ConfigFlujoPelea config;
        [Tooltip("Pantalla donde se ven el conteo y el resultado.")]
        [SerializeField] private PantallaPelea pantalla;
        [Tooltip("Opcional: objeto con la arena (IArenaPelea). Si está vacío, usa la registrada más cercana a este objeto.")]
        [SerializeField] private GameObject arenaEnEscena;
        [Tooltip("Opcional: objeto del jugador (IPeleador). Si está vacío, busca el peleador del lado Jugador al arrancar.")]
        [SerializeField] private GameObject jugadorEnEscena;
        [Tooltip("Opcional: rival que ya está en la escena. Si está vacío, se crea desde el prefab en el inicio del rival.")]
        [SerializeField] private GameObject rivalEnEscena;
        [Tooltip("Prefab del rival (IA/Prefabs/RivalBasico) que se crea si rivalEnEscena está vacío.")]
        [SerializeField] private GameObject prefabRival;
        [Tooltip("Si es verdadero, la pelea empieza sola al dar Play. Si no, la inicia quien llame a EmpezarPelea.")]
        [SerializeField] private bool empezarAlArrancar = true;
        [Tooltip("Opcional: lo que dice el presentador antes del conteo. Sin esto no hay presentación.")]
        [SerializeField] private ConfigPresentador presentador;
        [Tooltip("Opcional: lo que paga esta pelea. Sin esto no hay premio.")]
        [SerializeField] private DatosPremioPelea premio;
        [Tooltip("Opcional: las reglas de la apuesta de esta pelea. Sin esto, o sin ServicioApuestas en la escena, no hay apuesta.")]
        [SerializeField] private DatosApuestaPelea apuesta;
        [Tooltip("Liga de la pelea cuando no hay cartel (0 = fuera de liga, 1 a 4).")]
        [Range(0, 4)] [SerializeField] private int liga;
        [Tooltip("Pelea de jefe cuando no hay cartel.")]
        [SerializeField] private bool esJefe;
        [Tooltip("Si es falso, el resultado no ofrece revancha: no se muestra \"R: revancha\" y la tecla R no hace nada.")]
        [SerializeField] private bool permitirRevancha = true;
        [Tooltip("Acción de la revancha (tecla R). Solo funciona en el resultado.")]
        [SerializeField] private InputAction revancha;

        private MaquinaFlujoPelea maquina;
        private IArenaPelea arena;
        private IPeleador jugador;
        private IPeleador rival;
        private GameObject rivalCreado;
        private bool rivalNuevo;
        private bool arrancado;
        private bool empezarPendiente;
        private bool avisoSinArena;
        private bool avisoSinJugador;
        private bool avisoArenaPerdida;
        private bool esperandoArena;
        private bool avisoSinCartera;
        private ServicioApuestas servicioApuestas;
        private CartelPelea cartelActual;
        private float fraccionPremio = 1f;
        private ConfigPresentador presentadorSerializado;
        private GameObject prefabRivalSerializado;
        private DatosApuestaPelea apuestaSerializada;

        public EstadoFlujoPelea Estado => maquina != null ? maquina.Estado : EstadoFlujoPelea.Preparando;
        public IArenaPelea Arena => arena;
        public IPeleador Jugador => jugador;
        public IPeleador Rival => rival;
        public PantallaPelea Pantalla => pantalla;
        public bool PermiteRevancha => permitirRevancha;
        /// <summary>Las reglas de apuesta de esta pelea (null si no tiene).</summary>
        public DatosApuestaPelea Apuesta => apuesta;
        /// <summary>El cartel aplicado con <see cref="PrepararCartel"/> (null sin cartel).</summary>
        public CartelPelea CartelActual => cartelActual;
        /// <summary>La pelea en curso o preparada: alias del rival (vacío si aún no existe), liga y si es de jefe.</summary>
        public InfoPelea Info => new InfoPelea(rival != null ? rival.Nombre : string.Empty,
            cartelActual != null ? cartelActual.liga : liga, cartelActual != null ? cartelActual.esJefe : esJefe);

        /// <summary>Avisa cada línea que dice el presentador, ya con alias y rival puestos y en mayúsculas (para el audio).</summary>
        public event Action<string> LineaDicha;

        /// <summary>Avisa el dinero abonado al jugador al terminar una pelea (solo si fue mayor que 0).</summary>
        public event Action<int> PremioPagado;

        /// <summary>Avisa el cambio neto de la apuesta al terminar la pelea (positivo ganó, negativo perdió), solo si había apuesta.</summary>
        public event Action<int> ApuestaResuelta;

        private void Awake()
        {
            if (config == null || pantalla == null)
            {
                Debug.LogError("[Pelea] Faltan referencias en DirectorPelea de " + name, this);
                enabled = false;
                return;
            }
            pantalla.FijarRevanchaVisible(permitirRevancha);
            presentadorSerializado = presentador;
            prefabRivalSerializado = prefabRival;
            apuestaSerializada = apuesta;
            ArmarMaquina();
        }

        // La máquina depende de las duraciones del presentador: se rearma si cambia el presentador (solo en Preparando).
        private void ArmarMaquina()
        {
            List<float> duraciones = null;
            if (presentador != null && presentador.Lineas != null)
            {
                duraciones = new List<float>(presentador.Lineas.Length);
                foreach (string linea in presentador.Lineas)
                {
                    duraciones.Add(presentador.SegundosPorLinea);
                }
            }
            if (maquina != null)
            {
                maquina.LineaCambio -= AlCambiarLinea;
                maquina.ConteoCambio -= EventosPelea.PublicarConteo;
                maquina.PeleaIniciada -= AlIniciarPelea;
                maquina.KOOcurrido -= AlOcurrirKO;
                maquina.ResultadoListo -= AlResultado;
            }
            maquina = new MaquinaFlujoPelea(config.SegundosPorNumero, config.PausaDespuesKO, duraciones,
                presentador != null && presentador.PresentarEnRevancha);
            maquina.LineaCambio += AlCambiarLinea;
            maquina.ConteoCambio += EventosPelea.PublicarConteo;
            maquina.PeleaIniciada += AlIniciarPelea;
            maquina.KOOcurrido += AlOcurrirKO;
            maquina.ResultadoListo += AlResultado;
        }

        /// <summary>
        /// Aplica el cartel de la próxima pelea: rival, premio (el de ganar y el de perder por <paramref name="fraccionPremio"/>,
        /// redondeado), apuesta y presentador del cartel (lo que el cartel no trae, no hay). Solo en Preparando, sin apuesta
        /// anunciada y sin rival puesto a mano; si ya había creado un rival de otro cartel, lo destruye. Si no se puede, devuelve
        /// false y no cambia nada. Reiniciar no quita el cartel: lo quita quien lo puso (<see cref="QuitarCartel"/>).
        /// </summary>
        public bool PrepararCartel(CartelPelea cartel, float fraccionPremio)
        {
            if (cartel == null || !PuedeCambiarCartel())
            {
                return false;
            }
            cartelActual = cartel;
            this.fraccionPremio = Mathf.Max(0f, fraccionPremio);
            prefabRival = cartel.prefabRival;
            apuesta = cartel.apuesta;
            AplicarPresentador(cartel.presentador);
            SoltarRivalCreado();
            return true;
        }

        /// <summary>
        /// Vuelve a lo serializado en el prefab (rival, premio, apuesta, presentador, liga y jefe). Solo en Preparando; devuelve
        /// false si no había cartel o no se puede.
        /// </summary>
        public bool QuitarCartel()
        {
            if (cartelActual == null || !PuedeCambiarCartel())
            {
                return false;
            }
            cartelActual = null;
            fraccionPremio = 1f;
            prefabRival = prefabRivalSerializado;
            apuesta = apuestaSerializada;
            AplicarPresentador(presentadorSerializado);
            SoltarRivalCreado();
            return true;
        }

        private bool PuedeCambiarCartel()
        {
            return maquina != null && maquina.Estado == EstadoFlujoPelea.Preparando && servicioApuestas == null
                && rivalEnEscena == null;
        }

        private void AplicarPresentador(ConfigPresentador nuevo)
        {
            if (ReferenceEquals(nuevo, presentador))
            {
                return;
            }
            presentador = nuevo;
            ArmarMaquina();
        }

        private void SoltarRivalCreado()
        {
            if (rivalCreado == null)
            {
                return;
            }
            Desuscribir(rival);
            Destroy(rivalCreado);
            rivalCreado = null;
            rival = null;
            rivalNuevo = false;
        }

        private void OnEnable()
        {
            revancha.Enable();
            Registro<IArenaPelea>.Quitado += AlQuitarseArena;
            if (esperandoArena)
            {
                Registro<IArenaPelea>.Agregado -= AlAgregarseArena;
                Registro<IArenaPelea>.Agregado += AlAgregarseArena;
            }
        }

        private void OnDisable()
        {
            revancha.Disable();
            Registro<IArenaPelea>.Agregado -= AlAgregarseArena;
            Registro<IArenaPelea>.Quitado -= AlQuitarseArena;
        }

        private void Start()
        {
            if (maquina == null)
            {
                return;
            }
            arrancado = true;
            ResolverJugador();
            ResolverArena();
            empezarPendiente = empezarAlArrancar;
            IntentarEmpezar();
        }

        private void OnDestroy()
        {
            DevolverApuesta();
            Desuscribir(jugador);
            Desuscribir(rival);
            if (rivalCreado != null)
            {
                Destroy(rivalCreado);
            }
        }

        /// <summary>
        /// Prepara a los dos peleadores en sus inicios y empieza el conteo. Si todavía falta la arena, la pelea empieza
        /// cuando aparezca. Solo funciona al principio, no durante una pelea ni en el resultado (ahí es la revancha).
        /// </summary>
        public void EmpezarPelea()
        {
            if (maquina == null)
            {
                return;
            }
            empezarPendiente = true;
            IntentarEmpezar();
        }

        /// <summary>
        /// Desde el resultado: destruye al rival que creó, limpia la pantalla y vuelve a Preparando, listo para otra pelea nueva
        /// con <see cref="EmpezarPelea"/> (con presentación si el config la trae). Fuera del resultado no hace nada.
        /// </summary>
        public bool Reiniciar()
        {
            if (maquina == null || !maquina.Reiniciar())
            {
                return false;
            }
            empezarPendiente = false;
            rivalNuevo = false;
            DevolverApuesta();
            if (rivalCreado != null)
            {
                Desuscribir(rival);
                Destroy(rivalCreado);
                rivalCreado = null;
                rival = null;
            }
            pantalla.Ocultar();
            return true;
        }

        /// <summary>
        /// Anuncia esta pelea al <see cref="ServicioApuestas"/> con el alias del rival, para que el jugador pueda apostar antes de
        /// <see cref="EmpezarPelea"/>. Sin datos de apuesta, sin servicio, sin arena o fuera de Preparando no hace nada.
        /// Crea al rival ya, porque su alias sale de él.
        /// </summary>
        public bool AnunciarApuesta()
        {
            if (apuesta == null || maquina == null || maquina.Estado != EstadoFlujoPelea.Preparando || arena == null)
            {
                return false;
            }
            ServicioApuestas servicio = Registro<ServicioApuestas>.Actual;
            if (servicio == null)
            {
                return false;
            }
            if (rival == null)
            {
                ResolverRival();
            }
            if (!servicio.Anunciar(rival != null ? rival.Nombre : string.Empty, apuesta))
            {
                return false;
            }
            servicioApuestas = servicio;
            return true;
        }

        /// <summary>
        /// Cancela la pelea anunciada con <see cref="AnunciarApuesta"/> (el jugador se echó para atrás): devuelve lo apostado y deja
        /// el servicio listo para anunciar otra. Sin anuncio no hace nada.
        /// </summary>
        public void CancelarApuesta()
        {
            DevolverApuesta();
        }

        private void DevolverApuesta()
        {
            if (servicioApuestas == null)
            {
                return;
            }
            ServicioApuestas servicio = servicioApuestas;
            servicioApuestas = null;
            if (servicio != null)
            {
                servicio.Devolver();
            }
        }

        private void Update()
        {
            if (maquina == null)
            {
                return;
            }
            if (permitirRevancha && maquina.Estado == EstadoFlujoPelea.Resultado && revancha.WasPressedThisFrame())
            {
                PedirRevancha();
                return;
            }
            maquina.Avanzar(Time.deltaTime);
        }

        private void PedirRevancha()
        {
            // Sin arena (se fue) no hay dónde colocar a nadie: espera a que aparezca otra.
            if (arena == null)
            {
                return;
            }
            if (maquina.PedirRevancha())
            {
                PrepararYContar();
            }
        }

        private void ResolverJugador()
        {
            if (jugadorEnEscena != null)
            {
                jugador = jugadorEnEscena.GetComponent<IPeleador>();
            }
            else
            {
                foreach (MonoBehaviour candidato in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (candidato is IPeleador peleador && peleador.Lado == LadoPelea.Jugador)
                    {
                        jugador = peleador;
                        break;
                    }
                }
            }
            if (jugador == null && !avisoSinJugador)
            {
                avisoSinJugador = true;
                Debug.LogWarning("[Pelea] DirectorPelea de " + name + " no encontró al jugador; no empieza la pelea.", this);
            }
            Suscribir(jugador);
        }

        private void ResolverArena()
        {
            if (arenaEnEscena != null)
            {
                arena = arenaEnEscena.GetComponent<IArenaPelea>();
            }
            else
            {
                float mejor = float.MaxValue;
                foreach (IArenaPelea candidata in Registro<IArenaPelea>.Todos)
                {
                    float distancia = (candidata.Centro - transform.position).sqrMagnitude;
                    if (distancia < mejor)
                    {
                        mejor = distancia;
                        arena = candidata;
                    }
                }
            }
            if (arena != null)
            {
                pantalla.Colocar(arena);
                return;
            }
            if (!avisoSinArena)
            {
                avisoSinArena = true;
                Debug.LogWarning("[Pelea] DirectorPelea de " + name + " no encontró ninguna arena; espera a que aparezca una.", this);
            }
            esperandoArena = true;
            Registro<IArenaPelea>.Agregado -= AlAgregarseArena;
            Registro<IArenaPelea>.Agregado += AlAgregarseArena;
        }

        private void AlAgregarseArena(IArenaPelea nueva)
        {
            if (arena != null)
            {
                return;
            }
            Registro<IArenaPelea>.Agregado -= AlAgregarseArena;
            esperandoArena = false;
            ResolverArena();
            IntentarEmpezar();
        }

        // Si la arena actual desaparece, se suelta (no se usa una destruida en la revancha) y se espera otra.
        private void AlQuitarseArena(IArenaPelea quitada)
        {
            if (arena == null || !ReferenceEquals(quitada, arena))
            {
                return;
            }
            arena = null;
            if (arenaEnEscena != null && ReferenceEquals(arenaEnEscena.GetComponent<IArenaPelea>(), quitada))
            {
                arenaEnEscena = null;
            }
            if (!avisoArenaPerdida)
            {
                avisoArenaPerdida = true;
                Debug.LogWarning("[Pelea] DirectorPelea de " + name + " perdió su arena; espera a que aparezca otra.", this);
            }
            avisoSinArena = true; // el aviso de arriba ya lo dice
            ResolverArena();
        }

        private void ResolverRival()
        {
            if (rivalEnEscena != null)
            {
                rival = rivalEnEscena.GetComponent<IPeleador>();
            }
            else if (prefabRival != null)
            {
                Pose inicio = arena.InicioRival;
                rivalCreado = Instantiate(prefabRival, inicio.position, inicio.rotation);
                rival = rivalCreado.GetComponent<IPeleador>();
                rivalNuevo = rival != null;
            }
            if (rival == null)
            {
                Debug.LogError("[Pelea] DirectorPelea de " + name + " no tiene rival (rivalEnEscena o prefabRival con IPeleador).", this);
                empezarPendiente = false;
                return;
            }
            Suscribir(rival);
        }

        private void IntentarEmpezar()
        {
            if (!arrancado || !empezarPendiente || maquina.Estado != EstadoFlujoPelea.Preparando
                || arena == null || jugador == null)
            {
                return;
            }
            if (rival == null)
            {
                ResolverRival();
                if (rival == null)
                {
                    return;
                }
            }
            empezarPendiente = false;
            PrepararYContar();
        }

        private void PrepararYContar()
        {
            jugador.PrepararPelea(arena.InicioJugador);
            if (servicioApuestas != null)
            {
                servicioApuestas.Cerrar();
            }
            if (rivalNuevo)
            {
                // Nació en su pose. Apagarle el control antes de su primer Start lo dejaría sin Start hasta que se lo
                // devuelvan, y entonces ese Start se lo quitaría de nuevo: él mismo se deja sin control en su Start.
                rivalNuevo = false;
            }
            else
            {
                rival.PrepararPelea(arena.InicioRival);
            }
            EventosPelea.PublicarPresentacion(Info);
            maquina.IniciarConteo();
        }

        private void AlCambiarLinea(int indice)
        {
            string plantilla = presentador.Lineas[indice] ?? string.Empty;
            IProgresoLigas ligas = Registro<IProgresoLigas>.Actual;
            int numeroLiga = Info.Liga;
            string nombreLiga = ligas != null && numeroLiga > 0 ? ligas.NombreLiga(numeroLiga) : string.Empty;
            IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
            string alias = perfil != null && !string.IsNullOrEmpty(perfil.Alias) ? perfil.Alias : presentador.AliasRespaldo;
            string nombreRival = rival != null ? rival.Nombre : string.Empty;
            string linea = LineaPresentador.Armar(plantilla, alias, nombreRival, nombreLiga);
            bool grito = indice == presentador.IndiceGrito;
            pantalla.MostrarLinea(linea, grito ? presentador.ColorGrito : presentador.ColorLinea,
                grito ? presentador.TamanoLetraGrito : presentador.TamanoLetraLinea);
            LineaDicha?.Invoke(linea);
        }

        private void AlIniciarPelea()
        {
            // Si alguien cayó durante el conteo, su control sigue apagado y la pelea termina en cuanto empieza.
            jugador.ActivarControl(true);
            rival.ActivarControl(true);
            EventosPelea.PublicarInicio();
            if (jugador.Vida.EstaKO)
            {
                maquina.NotificarKO(LadoPelea.Jugador);
            }
            else if (rival.Vida.EstaKO)
            {
                maquina.NotificarKO(LadoPelea.Rival);
            }
        }

        // Cada pelea (y cada revancha) termina una sola vez: paga una sola vez.
        private void AlResultado(ResultadoPelea resultado)
        {
            EventosPelea.PublicarFin(resultado);
            PagarPremio(resultado);
            ResolverApuesta(resultado);
        }

        private void ResolverApuesta(ResultadoPelea resultado)
        {
            if (servicioApuestas == null)
            {
                return;
            }
            ServicioApuestas servicio = servicioApuestas;
            servicioApuestas = null;
            if (servicio != null && servicio.Resolver(resultado.Ganador == LadoPelea.Jugador, out int neto))
            {
                pantalla.MostrarApuesta(neto);
                ApuestaResuelta?.Invoke(neto);
            }
        }

        private void PagarPremio(ResultadoPelea resultado)
        {
            if (premio == null && cartelActual == null)
            {
                return;
            }
            bool gano = resultado.Ganador == LadoPelea.Jugador;
            int monto;
            if (cartelActual != null)
            {
                monto = Mathf.RoundToInt(Mathf.Max(0, gano ? cartelActual.premioGanar : cartelActual.premioPerder) * fraccionPremio);
            }
            else
            {
                monto = gano ? premio.PremioGanar : premio.PremioPerder;
            }
            if (monto <= 0)
            {
                return;
            }
            ICartera cartera = Registro<ICartera>.Actual;
            if (cartera == null)
            {
                if (!avisoSinCartera)
                {
                    avisoSinCartera = true;
                    Debug.LogWarning("[Pelea] DirectorPelea de " + name + " no encontró la cartera; la pelea no paga premio.", this);
                }
                return;
            }
            cartera.Abonar(monto, MotivoDinero.Premio);
            pantalla.MostrarPremio(monto);
            PremioPagado?.Invoke(monto);
        }

        private void AlOcurrirKO(LadoPelea ganador)
        {
            jugador.ActivarControl(false);
            rival.ActivarControl(false);
            pantalla.MostrarKO();
        }

        private void Suscribir(IPeleador peleador)
        {
            if (peleador == null || peleador.Vida == null)
            {
                return;
            }
            if (peleador == jugador)
            {
                peleador.Vida.KO += AlCaerJugador;
            }
            else
            {
                peleador.Vida.KO += AlCaerRival;
            }
        }

        private void Desuscribir(IPeleador peleador)
        {
            if (peleador == null || peleador.Vida == null)
            {
                return;
            }
            peleador.Vida.KO -= AlCaerJugador;
            peleador.Vida.KO -= AlCaerRival;
        }

        private void AlCaerJugador() => maquina.NotificarKO(LadoPelea.Jugador);

        private void AlCaerRival() => maquina.NotificarKO(LadoPelea.Rival);
    }
}

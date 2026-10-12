using MechaCyberpunk.Core;
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

        public EstadoFlujoPelea Estado => maquina != null ? maquina.Estado : EstadoFlujoPelea.Preparando;
        public IArenaPelea Arena => arena;
        public IPeleador Jugador => jugador;
        public IPeleador Rival => rival;
        public PantallaPelea Pantalla => pantalla;

        private void Awake()
        {
            if (config == null || pantalla == null)
            {
                Debug.LogError("[Pelea] Faltan referencias en DirectorPelea de " + name, this);
                enabled = false;
                return;
            }
            maquina = new MaquinaFlujoPelea(config.SegundosPorNumero, config.PausaDespuesKO);
            maquina.ConteoCambio += EventosPelea.PublicarConteo;
            maquina.PeleaIniciada += AlIniciarPelea;
            maquina.KOOcurrido += AlOcurrirKO;
            maquina.ResultadoListo += EventosPelea.PublicarFin;
        }

        private void OnEnable()
        {
            revancha.Enable();
        }

        private void OnDisable()
        {
            revancha.Disable();
            Registro<IArenaPelea>.Agregado -= AlAgregarseArena;
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

        private void Update()
        {
            if (maquina == null)
            {
                return;
            }
            if (maquina.Estado == EstadoFlujoPelea.Resultado && revancha.WasPressedThisFrame())
            {
                PedirRevancha();
                return;
            }
            maquina.Avanzar(Time.deltaTime);
        }

        private void PedirRevancha()
        {
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
            ResolverArena();
            IntentarEmpezar();
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
            maquina.IniciarConteo();
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

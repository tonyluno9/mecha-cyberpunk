using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Pistola del jugador a pie. Con la pistola en la mano, Atacar lanza un rayo desde el centro de la cámara y otro desde la boca del arma hasta ese punto (T-400):
    /// el primer collider que no sea del propio jugador (se ignoran los triggers) decide. Si tiene
    /// <see cref="IRecibeDanio"/> en sus padres recibe daño de tipo Disparo; si no, el tiro se queda en la pared y deja una
    /// marca. R recarga y, con el cargador vacío, Atacar empieza la recarga sola. El atacante es null: la persona no es IPeleador.
    /// </summary>
    [RequireComponent(typeof(ControladorAPie))]
    [RequireComponent(typeof(VidaAPie))]
    public class PistolaAPie : MonoBehaviour
    {

        [SerializeField] private DatosPistola datos;
        [SerializeField] private InputActionReference atacar;
        [SerializeField] private InputActionReference recargar;
        [Tooltip("Visual de la pistola, hijo de Manos. Se mueve hacia atrás con el retroceso.")]
        [SerializeField] private Transform visual;
        [Tooltip("Luz breve en la boca del cañón.")]
        [SerializeField] private Light luzBoca;
        [Tooltip("Visual de la escopeta (T-403). Se mueve hacia atrás con el retroceso.")]
        [SerializeField] private Transform visualEscopeta;
        [SerializeField] private Light luzBocaEscopeta;
        [Tooltip("Visual del fusil de asalto (T-403, T-416).")]
        [SerializeField] private Transform visualFusil;
        [SerializeField] private Light luzBocaFusil;
        [Tooltip("Visual del sniper (T-416).")]
        [SerializeField] private Transform visualSniper;
        [SerializeField] private Light luzBocaSniper;
        [SerializeField] private Material materialMarca;
        [Tooltip("Cuánto dura la luz del cañón (s).")]
        [Min(0.01f)] [SerializeField] private float duracionLuz = 0.05f;
        [Tooltip("Cuánto retrocede la pistola en cada disparo (m).")]
        [Min(0f)] [SerializeField] private float retroceso = 0.08f;
        [Tooltip("Cuánto tarda la pistola en volver a su lugar tras el retroceso (s).")]
        [Min(0.01f)] [SerializeField] private float duracionRetroceso = 0.12f;
        [Tooltip("Cuánto dura la marca donde pegó un tiro (s).")]
        [Min(0.1f)] [SerializeField] private float duracionMarca = 2f;
        [Tooltip("Marcas de impacto visibles a la vez; al pasarse se reusa la más vieja.")]
        [Range(1, 32)] [SerializeField] private int maxMarcas = 16;
        [Tooltip("Lado de la marca de impacto (m).")]
        [Min(0.01f)] [SerializeField] private float tamanoMarca = 0.08f;

        private ControladorAPie controlador;
        private CubiertaYRodada cubierta;
        private VidaAPie vida;
        private ArmasAPie armas;
        private JugadorAPie jugadorApie;
        // T-445: disparar desde el carro (solo pistola y fusil); con la raíz del carro para que la bala no le pegue a él.
        private bool desdeCarro;
        private Transform raizCarro;
        private const float AlturaBocaCarro = 0.4f;
        private static readonly int CantidadArmas = Enum.GetValues(typeof(ArmaAPie)).Length;
        private readonly CargadorPistola[] cargadores = new CargadorPistola[CantidadArmas];
        private readonly DatosArma[] datosArmas = new DatosArma[CantidadArmas];
        private readonly Transform[] visuales = new Transform[CantidadArmas];
        private readonly Light[] luces = new Light[CantidadArmas];
        private readonly Vector3[] reposos = new Vector3[CantidadArmas];
        private DatosArma pistolaBase;
        private ArmasEspecialesAPie especiales;
        private ArsenalJugador arsenal;
        private ReservaBalas reserva;
        private float inicioRetroceso = -100f;
        // T-1486: peso por arma (retroceso visual y floración de la dispersión) desde DatosSensacion; sin él, los valores de este componente.
        private SensacionDisparo sensacion;
        private readonly ReglaFloracionDispersion floracion = new ReglaFloracionDispersion();

        private AjusteSensacion AjusteDe(ArmaAPie arma)
        {
            if (sensacion == null)
            {
                sensacion = GetComponent<SensacionDisparo>();
            }
            return sensacion != null && sensacion.Datos != null ? sensacion.Datos.Ajuste(arma) : null;
        }
        private const float PuntaDelArma = 0.5f;
        private const float MaximaIntensidadLuzBoca = 1.2f;
        private const float MaximoAlcanceLuzBoca = 2.5f;
        private float finLuz = -1f;
        private ArmaAPie armaRetroceso = ArmaAPie.Pistola;
        private Transform[] marcas;
        private float[] finMarcas;
        private int siguienteMarca;
        private bool enInterior;
        private float finEsperaCalor = -1f;

        public DatosPistola Datos => datos;
        // El cargador del arma de fuego en la mano; con los puños, el de la pistola.
        private CargadorPistola cargador => cargadores[Indice(armas != null ? armas.ArmaActiva : ArmaAPie.Pistola)];
        public CargadorPistola Cargador => cargador;
        public int Balas => cargador != null ? cargador.Balas : 0;
        public int TamanoCargador => cargador != null ? cargador.Tamano : 0;
        /// <summary>T-720: balas de reserva del arma en la mano; -1 si son infinitas (pistola).</summary>
        public int Reserva => cargador != null && cargador.Fuente != null ? cargador.Fuente.Disponibles : -1;
        public bool Recargando => cargador != null && cargador.Recargando;
        /// <summary>Datos del arma de fuego en la mano (T-403).</summary>
        public DatosArma DatosActuales => datosArmas[Indice(armas != null ? armas.ArmaActiva : ArmaAPie.Pistola)];

        public static bool EsDeFuego(ArmaAPie arma) => arma != ArmaAPie.Punos;

        /// <summary>T-1622: hasta dónde se oye el tiro de esta arma (m): 25 pistola, revólver y subfusil; 40 escopeta, fusil, sniper y plasma; 60 lanzagranadas.</summary>
        public static float RadioRuido(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Escopeta:
                case ArmaAPie.Fusil:
                case ArmaAPie.Sniper:
                case ArmaAPie.RiflePlasma:
                    return 40f;
                case ArmaAPie.Lanzagranadas:
                case ArmaAPie.Granada:
                    return 60f;
                default:
                    return 25f;
            }
        }

        private static int Indice(ArmaAPie arma) => arma == ArmaAPie.Punos ? (int)ArmaAPie.Pistola : (int)arma;

        /// <summary>T-991: el cargador de un arma de fuego (la rueda muestra las balas de cada una), o null si no la tiene.</summary>
        public CargadorPistola CargadorDe(ArmaAPie arma) => arma == ArmaAPie.Punos ? null : cargadores[Indice(arma)];

        /// <summary>Se dispara al disparar, al hacer clic vacío y al empezar o terminar la recarga.</summary>
        public event Action Cambio;
        /// <summary>Se dispara en cada tiro, haya dado a algo o no.</summary>
        public event Action Disparo;
        /// <summary>Se dispara en cada tiro con el arma y la posición de la boca (T-420).</summary>
        public event Action<ArmaAPie, Vector3> Fogonazo;
        /// <summary>T-1491: se dispara por cada bala o perdigón con su recorrido (cañón y punto final: el impacto o el alcance), para el trazo.</summary>
        public event Action<ArmaAPie, Vector3, Vector3> Trazo;
        /// <summary>Se dispara en cada bala o perdigón que pega a algo (T-420).</summary>
        public event Action<ArmaAPie, ImpactoBala> BalaImpacto;

        private readonly SeguidorImplantes implantes = new SeguidorImplantes();

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            cubierta = GetComponent<CubiertaYRodada>();
            vida = GetComponent<VidaAPie>();
            armas = GetComponent<ArmasAPie>();
            jugadorApie = GetComponent<JugadorAPie>();
            if (datos == null || atacar == null || recargar == null || armas == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en PistolaAPie de " + name, this);
                enabled = false;
                return;
            }
            reserva = GetComponent<ReservaBalas>();
            if (reserva == null)
            {
                reserva = gameObject.AddComponent<ReservaBalas>();
            }
            pistolaBase = DatosArma.Crear("pistola_base", "Pistola", ArmaAPie.Pistola, 0, 0, datos.Danio, 1, datos.BalasPorCargador,
                datos.TiempoRecarga, datos.TiempoEntreDisparos, false, 0f, 0f, datos.Alcance);
            visuales[(int)ArmaAPie.Pistola] = visual;
            visuales[(int)ArmaAPie.Escopeta] = visualEscopeta;
            visuales[(int)ArmaAPie.Fusil] = visualFusil;
            visuales[(int)ArmaAPie.Sniper] = visualSniper;
            luces[(int)ArmaAPie.Pistola] = luzBoca;
            luces[(int)ArmaAPie.Escopeta] = luzBocaEscopeta;
            luces[(int)ArmaAPie.Fusil] = luzBocaFusil;
            luces[(int)ArmaAPie.Sniper] = luzBocaSniper;
            // T-1013: las armas nuevas tienen su visual en ArmasAPie y su luz de boca dentro del visual.
            for (int i = (int)ArmasAPie.PrimeraNueva; i < CantidadArmas; i++)
            {
                GameObject v = armas.Visual((ArmaAPie)i);
                visuales[i] = v != null ? v.transform : null;
                luces[i] = v != null ? v.GetComponentInChildren<Light>(true) : null;
            }
            especiales = GetComponent<ArmasEspecialesAPie>();
            if (especiales == null)
            {
                especiales = gameObject.AddComponent<ArmasEspecialesAPie>();
            }
            for (int i = 1; i < CantidadArmas; i++)
            {
                if (visuales[i] != null)
                {
                    reposos[i] = visuales[i].localPosition;
                }
                if (luces[i] != null)
                {
                    luces[i].enabled = false;
                    // T-676: la luz del cañón es apenas un toque (antes bañaba de naranja al personaje).
                    luces[i].intensity = Mathf.Min(luces[i].intensity, MaximaIntensidadLuzBoca);
                    luces[i].range = Mathf.Min(luces[i].range, MaximoAlcanceLuzBoca);
                }
            }
            RevisarArmas();
            CrearMarcas();
            if (GetComponent<TrazosBala>() == null)
            {
                gameObject.AddComponent<TrazosBala>();
            }
            if (GetComponent<SensacionDisparo>() == null)
            {
                gameObject.AddComponent<SensacionDisparo>();
            }
        }

        // Crea o rehace el cargador de cada arma de fuego cuando cambia su nivel (el cargador nuevo viene lleno).
        private void RevisarArmas()
        {
            if (arsenal == null)
            {
                arsenal = Registro<ArsenalJugador>.Actual;
            }
            for (int i = (int)ArmaAPie.Pistola; i < CantidadArmas; i++)
            {
                ArmaAPie arma = (ArmaAPie)i;
                DatosArma nuevos = arsenal != null ? arsenal.Datos(arma) : null;
                if (nuevos == null && arma == ArmaAPie.Pistola && (arsenal == null || arsenal.PistolaAlInicio))
                {
                    nuevos = pistolaBase;
                }
                if (nuevos == datosArmas[i])
                {
                    continue;
                }
                datosArmas[i] = nuevos;
                if (nuevos == null)
                {
                    cargadores[i] = null;
                    continue;
                }
                CargadorPistola c = new CargadorPistola(nuevos.Cargador, nuevos.TiempoEntreDisparos, nuevos.Recarga);
                c.Fuente = reserva != null ? reserva.Fuente(arma) : null;
                c.Disparo += AlCambiar;
                c.RecargaIniciada += AlCambiar;
                c.RecargaTerminada += AlCambiar;
                c.RecargaCancelada += AlCambiar;
                c.ClicVacio += () =>
                {
                    // T-1308: la recarga automática al vaciarse también suena y pasa por la animación (AnimacionJugador lee Recargando).
                    if (c.EmpezarRecarga())
                    {
                        Sonidos.Reproducir(IdRecarga(arma));
                    }
                    Cambio?.Invoke();
                };
                cargadores[i] = c;
                Cambio?.Invoke();
            }
        }

        private void OnEnable()
        {
            implantes.Activar();
            atacar.action.Enable();
            recargar.action.Enable();
            EventosLugares.JugadorEntro += AlEntrarLugar;
            EventosLugares.JugadorSalio += AlSalirLugar;
        }

        private void OnDisable()
        {
            implantes.Desactivar();
            EventosLugares.JugadorEntro -= AlEntrarLugar;
            EventosLugares.JugadorSalio -= AlSalirLugar;
            atacar.action.Disable();
            recargar.action.Disable();
        }

        private void OnDestroy()
        {
            if (marcas == null)
            {
                return;
            }
            foreach (Transform marca in marcas)
            {
                if (marca != null)
                {
                    Destroy(marca.gameObject);
                }
            }
        }

        private MechaCyberpunk.Personaje.Habilidades.HabilidadesProtesis habilidades;

        private void Update()
        {
            RevisarArmas();
            ReproducirDiferidos();
            if (habilidades == null)
            {
                habilidades = GetComponent<MechaCyberpunk.Personaje.Habilidades.HabilidadesProtesis>();
            }
            float factorRecarga = EfectosAPie.FactorRecarga(implantes.Efectos) * (habilidades != null ? habilidades.FactorTiempoRecarga : 1f);
            for (int i = 0; i < CantidadArmas; i++)
            {
                if (cargadores[i] != null)
                {
                    cargadores[i].FactorRecarga = factorRecarga;
                }
                cargadores[i]?.Avanzar(Time.deltaTime);
            }
            ArmaAPie activa = armas.ArmaActiva;
            bool enCarro = EstaEnCarro();
            CancelarRecargasInterrumpidas(activa, enCarro);
            if (enCarro)
            {
                activa = ArmaDeCarro(activa);
            }
            desdeCarro = enCarro;
            CargadorPistola actual = EsDeFuego(activa) ? cargadores[(int)activa] : null;
            bool puede = actual != null && !vida.EstaKO && (controlador.ControlActivo || enCarro);
            RevisarApuntarSinBalas(activa, actual, puede && !enCarro);
            if (puede)
            {
                if (recargar.action.WasPressedThisFrame())
                {
                    if (actual.EmpezarRecarga())
                    {
                        Sonidos.Reproducir(IdRecarga(activa));
                    }
                    else if (actual.Fuente != null && actual.Fuente.Disponibles <= 0 && actual.Balas < actual.Tamano && !actual.Recargando)
                    {
                        Sonidos.Reproducir(SonidosArmasT1472.SinBalas); // T-1532: clic vacío (ARM_SIN_BALAS) // T-720: sin balas de reserva
                    }
                }
                DatosArma d = datosArmas[(int)activa];
                // T-1013: lanzagranadas, granada y rifle de plasma los maneja ArmasEspecialesAPie (proyectiles, arco y rayo).
                if (!enCarro && d.TipoDisparo != TipoDisparoArma.Bala)
                {
                    especiales.Actualizar(activa, actual, d, !controlador.EntradaBloqueada && atacar.action.IsPressed());
                    ActualizarVisual();
                    return;
                }
                bool pidio = !controlador.EntradaBloqueada && (d.Automatica ? atacar.action.IsPressed() : atacar.action.WasPressedThisFrame());
                pidio = EsperarArmaEnPose(pidio, enCarro, actual);
                bool sigueRafaga = pidio && activa == ArmaAPie.Fusil && actual.Balas > 0;
                if (colaRafaga.Avanzar(dispararEsteFrame, sigueRafaga))
                {
                    Sonidos.Reproducir(SonidosArmasT1472.ColaCalle); // T-1472: cola de la calle al soltar o acabarse la ráfaga
                }
                dispararEsteFrame = false;
                disparabaAutomatica = pidio && d.Automatica;
                // T-936: agachado, al pulsar disparar se para solo y dispara con esa misma pulsación (si hay techo encima y no cabe de pie, dispara agachado).
                bool agachado = !enCarro && cubierta != null && cubierta.AgachadoLibre;
                if (pidio && agachado)
                {
                    cubierta.AlternarAgachado();
                }
                if (pidio && actual.IntentarDisparar())
                {
                    if (!enCarro)
                    {
                        controlador.MirarAlDisparar(); // T-943: si dispara hacia atrás o de lado, el cuerpo gira hacia allá
                    }
                    Disparar(activa, d);
                    dispararEsteFrame = activa == ArmaAPie.Fusil;
                }
                else if (pidio && !actual.Recargando && actual.Balas <= 0 && Time.time >= finEsperaVacio)
                {
                    finEsperaVacio = Time.time + EsperaClicVacio;
                    Sonidos.Reproducir(SonidosArmasT1472.SinBalas); // T-1532: clic vacío (ARM_SIN_BALAS)
                }
            }
            if (especiales != null && (!puede || datosArmas[Indice(activa)] == null || datosArmas[Indice(activa)].TipoDisparo == TipoDisparoArma.Bala))
            {
                especiales.Soltar();
            }
            ActualizarVisual();
        }

        // ---------- T-1561: sacar -> apuntar -> disparar ----------

        [Tooltip("T-1561: disparar con el arma guardada primero la saca y apunta; el tiro sale cuando la animación llega a la pose. Apagado, el tiro sale al instante (como antes).")]
        [SerializeField] private bool sacarAntesDeDisparar;

        private VisualPersonaje visualJugador;
        private float disparoPedidoHasta = -1f;
        private float esperandoDesde = -1f;

        /// <summary>T-1561: el disparo con el arma guardada espera a que se saque y llegue a la pose de apuntar.</summary>
        public bool SacarAntesDeDisparar
        {
            get => sacarAntesDeDisparar;
            set => sacarAntesDeDisparar = value;
        }

        /// <summary>T-1561: segundos que lleva esperando el arma en la pose para disparar (0 si no espera).</summary>
        public float SegundosEsperandoArma => esperandoDesde >= 0f && Time.time < disparoPedidoHasta ? Time.time - esperandoDesde : 0f;

        // Devuelve si el disparo pedido sale este cuadro. El disparo pedido con el arma guardada se recuerda ReglasTransicionesT1561.SegundosRecordarDisparo s y sale al llegar a la pose.
        private bool EsperarArmaEnPose(bool pidio, bool enCarro, CargadorPistola actual)
        {
            if (!sacarAntesDeDisparar || enCarro)
            {
                return pidio;
            }
            if (visualJugador == null)
            {
                visualJugador = GetComponentInChildren<VisualPersonaje>(false);
            }
            if (visualJugador == null || !visualJugador.isActiveAndEnabled || visualJugador.Cubierto || actual.Recargando)
            {
                disparoPedidoHasta = -1f;
                return pidio;
            }
            bool pendiente = Time.time < disparoPedidoHasta;
            if (!pidio && !pendiente)
            {
                esperandoDesde = -1f;
                return false;
            }
            if (!pendiente)
            {
                esperandoDesde = Time.time;
            }
            if (pidio)
            {
                disparoPedidoHasta = Time.time + ReglasTransicionesT1561.SegundosRecordarDisparo;
            }
            bool listo = ReglasTransicionesT1561.PuedeDisparar(visualJugador.ArmaEnPose, false, AccionPersonaje.Locomocion, Time.time - esperandoDesde);
            if (!listo)
            {
                visualJugador.SacarArma();
                return false;
            }
            disparoPedidoHasta = -1f;
            return true;
        }

        /// <summary>T-1561: corta la recarga del arma en la mano (daño recibido o echar a correr con un arma larga): no repone balas.</summary>
        public void CancelarRecargaActual()
        {
            CargadorPistola actual = cargador;
            if (actual != null && actual.Recargando)
            {
                actual.CancelarRecarga();
            }
        }

        // T-1324: sin balas en el cargador no se apunta: se intenta recargar primero (si hay reserva). El rayo de plasma ya en marcha no se corta.
        private void RevisarApuntarSinBalas(ArmaAPie activa, CargadorPistola actual, bool aplica)
        {
            bool rayoEnMarcha = especiales != null && especiales.EstadoPlasma != EstadoPlasma.Reposo;
            bool bloquear = aplica && ReglaApuntarConBalas.Bloquea(actual.Balas, rayoEnMarcha);
            controlador.ApuntarBloqueado = bloquear;
            if (bloquear && controlador.QuiereApuntar && !actual.Recargando && actual.EmpezarRecarga())
            {
                Sonidos.Reproducir(IdRecarga(activa));
            }
        }

        private ArmaAPie armaAnterior = ArmaAPie.Pistola;
        private bool enCarroAnterior;

        /// <summary>
        /// T-1308: una recarga solo repone balas si la animación termina. Si se cambia de arma, se rueda, se entra o sale del carro, o se cae KO,
        /// la recarga en curso se cancela y no se reponen balas.
        /// </summary>
        private void CancelarRecargasInterrumpidas(ArmaAPie activa, bool enCarro)
        {
            bool rodando = cubierta != null && cubierta.Rodando;
            bool cambioArma = activa != armaAnterior;
            bool cambioCarro = enCarro != enCarroAnterior;
            if (cambioArma || cambioCarro || rodando || vida.EstaKO)
            {
                for (int i = 0; i < CantidadArmas; i++)
                {
                    // Al cambiar de arma solo se corta la que se dejó; rodar, el carro o el KO cortan la que esté en curso.
                    if (cargadores[i] != null && (cambioArma ? i == Indice(armaAnterior) : true))
                    {
                        cargadores[i].CancelarRecarga();
                    }
                }
            }
            armaAnterior = activa;
            enCarroAnterior = enCarro;
        }

        /// <summary>T-1013: la boca del arma (su luz o su visual), o null si no hay.</summary>
        public Transform BocaDe(ArmaAPie arma)
        {
            int i = Indice(arma);
            return luces[i] != null ? luces[i].transform : visuales[i];
        }

        /// <summary>T-1013: avisa del disparo de un arma especial (fogonazo, retroceso, ruido, calor y Disparo) como los tiros de bala.</summary>
        public void AvisarDisparoEspecial(ArmaAPie arma, Vector3 boca)
        {
            EventosRuido.PublicarDisparoConRadio(boca, RadioRuido(arma));
            EventosRuido.PublicarDisparo(boca);
            SumarCalor();
            inicioRetroceso = Time.time;
            armaRetroceso = arma;
            finLuz = Time.time + duracionLuz;
            Fogonazo?.Invoke(arma, boca);
            Disparo?.Invoke();
        }

        // T-445: ¿el jugador va manejando? Entonces la raíz del carro no recibe sus balas.
        private bool EstaEnCarro()
        {
            IMontable montable = jugadorApie != null ? jugadorApie.MontableActual : null;
            bool enCarro = montable != null && jugadorApie.Modo == ModoJugador.Carro && montable.Asiento != null;
            raizCarro = enCarro ? montable.Asiento.root : null;
            if (enCarro && !atacar.action.enabled)
            {
                // El controlador a pie, apagado en el carro, deja apagada la acción de atacar: aquí se vuelve a prender.
                atacar.action.Enable();
            }
            return enCarro;
        }

        // Desde el carro solo se dispara pistola o fusil: la que lleva en la mano si es una de esas; si no, la pistola.
        private ArmaAPie ArmaDeCarro(ArmaAPie activa)
        {
            if (activa == ArmaAPie.Pistola || activa == ArmaAPie.Fusil || activa == ArmaAPie.Subfusil || activa == ArmaAPie.Revolver)
            {
                return activa;
            }
            return armas.TieneArma(ArmaAPie.Pistola) ? ArmaAPie.Pistola : ArmaAPie.Punos;
        }

        // Un interior es cualquier lugar que no sea la calle.
        private void AlEntrarLugar(IdLugar lugar) => enInterior = lugar != IdLugar.Ninguno && lugar != IdLugar.Calle;

        private void AlSalirLugar(IdLugar lugar) => enInterior = false;

        // Cada disparo en la calle suma calor, como máximo una vez cada SegundosEntreCalor.
        private void SumarCalor()
        {
            if (enInterior || Time.time < finEsperaCalor || datos.CalorPorDisparo <= 0f)
            {
                return;
            }
            finEsperaCalor = Time.time + datos.SegundosEntreCalor;
            Registro<ICalor>.Actual?.Sumar(datos.CalorPorDisparo, MotivoCalor.Pelea);
        }

        // T-442: sonidos que suenan un poco después del disparo (casquillo, bombeo de la escopeta, cerrojo del sniper).
        private const float SegundosCasquillo = 0.25f;
        private const float SegundosMartillo = 0.3f; // T-1532: el martillo del revólver se arma tras el tiro
        private const float SegundosBombeo = 0.45f;
        private const float SegundosCerrojo = 0.5f;
        private const float AlturaCasquillo = 0.05f;
        private struct SonidoDiferido
        {
            public float Cuando;
            public string Id;
            public Vector3 Posicion;
        }
        private readonly System.Collections.Generic.List<SonidoDiferido> diferidos = new System.Collections.Generic.List<SonidoDiferido>();
        private bool disparabaAutomatica;
        private readonly ReglaColaRafaga colaRafaga = new ReglaColaRafaga();
        private bool dispararEsteFrame;

        private const float EsperaClicVacio = 0.3f;
        private float finEsperaVacio;

        /// <summary>T-593 / T-1472: el ID de sonido del disparo de cada arma (ver <see cref="SonidosArmasT1472"/>).</summary>
        public static string IdDisparo(ArmaAPie arma) => SonidosArmasT1472.IdDisparo(arma);

        /// <summary>T-593 / T-1472: el ID de sonido de la recarga de cada arma.</summary>
        public static string IdRecarga(ArmaAPie arma) => SonidosArmasT1472.IdRecarga(arma);

        // T-442: programa un sonido para dentro de unos segundos.
        private void Diferir(string id, float segundos, Vector3 posicion)
        {
            diferidos.Add(new SonidoDiferido { Cuando = Time.time + segundos, Id = id, Posicion = posicion });
        }

        private void ReproducirDiferidos()
        {
            for (int i = diferidos.Count - 1; i >= 0; i--)
            {
                if (Time.time >= diferidos[i].Cuando)
                {
                    Sonidos.ReproducirEn(diferidos[i].Id, diferidos[i].Posicion);
                    diferidos.RemoveAt(i);
                }
            }
        }

        // Lo que suena un poco después de cada disparo: el casquillo al caer y el bombeo o el cerrojo de las armas que los tienen.
        private void ProgramarSonidosTrasDisparo(ArmaAPie arma)
        {
            string casquillo = SonidosArmasT1472.IdCasquillo(arma); // T-1532: chico o grande según el arma; el revólver no suelta casquillos
            if (casquillo != null)
            {
                Diferir(casquillo, SegundosCasquillo, transform.position + Vector3.up * AlturaCasquillo);
            }
            string tras = SonidosArmasT1472.IdTrasDisparo(arma);
            if (tras != null)
            {
                Diferir(tras, arma == ArmaAPie.Escopeta ? SegundosBombeo : arma == ArmaAPie.Revolver ? SegundosMartillo : SegundosCerrojo, transform.position);
            }
        }

        // T-676: sin línea de vista no hay disparo. Si entre el cuerpo del jugador y la boca del arma hay un muro o una barricada
        // (el arma asoma por el otro lado), la bala pega ahí y no sale.
        private bool BocaTapada(Vector3 origen, out RaycastHit tapa)
        {
            tapa = default;
            // A la altura de la boca: así agachado tras una cobertura baja se puede disparar por encima de ella.
            Vector3 cuerpoCentro = new Vector3(transform.position.x, origen.y, transform.position.z);
            Vector3 hacia = origen - cuerpoCentro;
            float distancia = hacia.magnitude;
            return distancia > 0.01f && BuscarImpacto(cuerpoCentro, hacia / distancia, distancia, out tapa);
        }

        /// <summary>T-681: el punto del mundo que marca el centro de la cámara (donde pegaría una bala sin dispersión), o el punto a <paramref name="alcance"/> m si no hay nada.</summary>
        public Vector3 PuntoDeMira(float alcance)
        {
            Transform camara = controlador.Camara;
            return BuscarCentroMira(camara, alcance, out RaycastHit centro) ? centro.point : camara.position + camara.forward * alcance;
        }

        /// <summary>T-977: el sonido del tiro que falla y pega en una superficie (pared, piso, carro): uno por tiro, aunque la escopeta lance varios perdigones.</summary>
        public const string SonidoImpactoSuperficie = ReglaMaterialImpacto.Concreto;

        private bool sonoImpactoEsteTiro;

        private void Disparar(ArmaAPie arma, DatosArma d)
        {
            sonoImpactoEsteTiro = false;
            // Tercera persona: un rayo desde el centro de la cámara halla el punto y otro, desde la boca del arma, llega a ese punto.
            // La escopeta lanza varios perdigones en un cono; la dispersión es el semiángulo del cono.
            Transform camara = desdeCarro && jugadorApie.Camara != null ? jugadorApie.Camara.transform : controlador.Camara;
            Light luz = luces[(int)arma];
            Transform visualArma = visuales[(int)arma];
            Transform boca = luz != null ? luz.transform : (visualArma != null ? visualArma : camara);
            Vector3 punto = camara.position + camara.forward * d.Alcance;
            float alcanceVehiculos = d.Alcance * FactorAlcanceVehiculos;
            bool hayCentro = BuscarCentroMira(camara, alcanceVehiculos, out RaycastHit centro) && DentroDelAlcance(centro, d.Alcance);
            if (hayCentro)
            {
                punto = centro.point;
            }
            Vector3 origen = desdeCarro ? jugadorApie.MontableActual.Asiento.position + Vector3.up * AlturaBocaCarro : boca.position;
            Vector3 haciaPunto = punto - origen;
            float largo = haciaPunto.magnitude;
            Vector3 direccionBase = largo > 0.01f ? haciaPunto / largo : camara.forward;
            float largoAlPunto = largo;
            // T-1486: si lo que marca la mira queda detrás del cañón (pegado al arma), la bala sigue la mira y no sale hacia atrás.
            if (!desdeCarro && Vector3.Dot(direccionBase, camara.forward) <= 0f)
            {
                direccionBase = camara.forward;
            }
            if (desdeCarro)
            {
                // Desde el carro la bala va horizontal, del asiento hacia el punto que mira la cámara del carro (sin bajar hasta el piso).
                Vector3 plano = Vector3.ProjectOnPlane(punto - origen, Vector3.up);
                direccionBase = plano.sqrMagnitude > 0.0001f ? plano.normalized : Vector3.ProjectOnPlane(camara.forward, Vector3.up).normalized;
            }
            float cono = controlador.Apuntando ? d.DispersionApuntando : d.Dispersion;
            AjusteSensacion peso = AjusteDe(arma);
            if (peso != null && peso.floracion > 0f)
            {
                // T-1486: cada tiro seguido abre un poco el cono (la mitad apuntando) y se recupera con el tiempo.
                cono += ReglaFloracionDispersion.Aplicada(floracion.Actual(Time.time, peso.recuperacionFloracion), controlador.Apuntando);
                floracion.Disparar(Time.time, peso.floracion, peso.topeFloracion, peso.recuperacionFloracion);
            }

            // T-448: el armero de la guarida da más daño a las armas de fuego.
            IGuarida guarida = Registro<IGuarida>.Actual;
            float multiplicadorGuarida = 1f + (guarida != null ? guarida.BonoDanioArmas : 0f);
            Sonidos.Reproducir(IdDisparo(arma)); // T-1472: el disparo del jugador suena en 2D
            EventosRuido.PublicarDisparoConRadio(origen, RadioRuido(arma));
            EventosRuido.PublicarDisparo(origen);
            RaycastHit tapa = default;
            bool tapada = !desdeCarro && BocaTapada(origen, out tapa);
            if (tapada)
            {
                Vector3 haciaTapa = (origen - tapa.point).normalized;
                Impactar(arma, d.Danio * multiplicadorGuarida, tapa, haciaTapa);
            }
            for (int i = 0; i < (tapada ? 0 : d.Perdigones); i++)
            {
                Vector3 direccion = direccionBase;
                if (cono > 0f)
                {
                    Vector3 eje = Vector3.Cross(direccionBase, UnityEngine.Random.onUnitSphere);
                    if (eje.sqrMagnitude > 0.0001f)
                    {
                        direccion = Quaternion.AngleAxis(cono * Mathf.Sqrt(UnityEngine.Random.value), eje.normalized) * direccionBase;
                    }
                }
                if (BuscarImpactoDeBala(origen, direccion, alcanceVehiculos, cono <= 0.001f && hayCentro && !desdeCarro, largoAlPunto, centro, out RaycastHit mejor)
                    && DentroDelAlcance(mejor, d.Alcance))
                {
                    // T-1491: el daño baja con la distancia según el arma (el francotirador no pierde nada).
                    // T-1527: un vehículo no pierde daño por la distancia (si no, de lejos las balas casi no le hacían nada).
                    float factorDistancia = EsVehiculo(mejor.collider) ? 1f : ReglaDanioDistancia.Multiplicador(mejor.distance, d.Alcance, d.InicioCaida, d.DanioMinimo);
                    Impactar(arma, d.Danio * multiplicadorGuarida * factorDistancia, mejor, direccion);
                    Trazo?.Invoke(arma, origen, mejor.point);
                }
                else
                {
                    Trazo?.Invoke(arma, origen, origen + direccion * d.Alcance);
                }
            }

            ProgramarSonidosTrasDisparo(arma);
            SumarCalor();
            inicioRetroceso = Time.time;
            armaRetroceso = arma;
            finLuz = Time.time + duracionLuz;
            // El destello sale en la punta del arma (la luz del cañón queda un poco más adelante que el arma en la mano).
            Vector3 puntaArma = !desdeCarro && visualArma != null && luz != null ? Vector3.Lerp(visualArma.position, luz.transform.position, PuntaDelArma) : origen;
            Fogonazo?.Invoke(arma, puntaArma);
            Disparo?.Invoke();
        }

        // Daño, efectos y marca de una bala que pegó. T-676: el disparo suena siempre igual y pegarle a una persona no suena;
        // T-977: el tiro que falla y pega en una superficie suena una vez (ayuda a saber dónde pegó).
        private void Impactar(ArmaAPie arma, float danio, RaycastHit mejor, Vector3 direccion)
        {
            IRecibeDanio objetivo = mejor.collider.GetComponentInParent<IRecibeDanio>();
            bool mato = false;
            bool esCarne = objetivo != null && !(objetivo is IObjetoInerte);
            // T-677: la cabeza hace x3 (ReglaZonaDanio); cuerpo normal. Solo a personas, no a objetos.
            bool cabeza = esCarne && ReglaZonaDanio.EsCabeza(mejor.collider, mejor.point);
            if (objetivo != null)
            {
                mato = objetivo.RecibirDanio(new InfoDanio(danio * ReglaZonaDanio.Multiplicador(cabeza), TipoDanio.Disparo, mejor.point, direccion, null)).CausoKO;
            }
            BalaImpacto?.Invoke(arma, new ImpactoBala(mejor.point, mejor.normal, esCarne, mato, cabeza));
            // T-1472: un impacto por tiro, con el sonido del material golpeado (carne, cabeza, robot, metal, cristal o concreto).
            if (!sonoImpactoEsteTiro)
            {
                sonoImpactoEsteTiro = true;
                Sonidos.ReproducirEn(IdImpacto(mejor.collider, esCarne, cabeza), mejor.point);
            }
            if (ReglaHuecosBala.HayHueco(mejor.collider.isTrigger, esCarne, SuperficieImpacto.Visible(mejor.collider, mejor.point)))
            {
                PonerMarca(mejor.point, mejor.normal);
            }
        }

        /// <summary>T-1472: el ID del impacto según el collider golpeado (componentes y nombres del objeto, la raíz y el material).</summary>
        public static string IdImpacto(Collider collider, bool esPersona, bool esCabeza)
        {
            bool esVehiculo = collider.GetComponentInParent<IMontable>() != null;
            return ReglaMaterialImpacto.Id(esPersona, esCabeza, esVehiculo, NombresDe(collider));
        }

        private static string NombresDe(Collider collider)
        {
            string nombres = collider.name + " " + collider.transform.root.name;
            Renderer r = collider.GetComponent<Renderer>();
            if (r == null)
            {
                r = collider.GetComponentInChildren<Renderer>();
            }
            if (r != null && r.sharedMaterial != null)
            {
                nombres += " " + r.sharedMaterial.name;
            }
            return nombres;
        }

        // El primer collider que no sea del propio jugador; los triggers se ignoran.
        // T-1324: antes era un RaycastNonAlloc con un buffer de 16: con el sniper (150 m) o en una calle con muchos colliders el buffer se llenaba
        // sin traer el mas cercano y la bala "pasaba de largo" al policia. Ahora se avanza con rayos simples (siempre el mas cercano) saltando los propios.
        private bool BuscarImpacto(Vector3 origen, Vector3 direccion, float alcance, out RaycastHit mejor)
        {
            return ReglaImpactoBala.BuscarPrimero(origen, direccion, alcance, transform.root, raizCarro, out mejor);
        }

        /// <summary>
        /// T-1527: el rayo de la mira sale del plano del jugador (no de la cámara, que va detrás del hombro): así un poste, un cartel o una pared
        /// pegados a la cámara y detrás del jugador no se vuelven "el punto de la mira". Tampoco cuentan los triggers ni el propio jugador.
        /// </summary>
        private bool BuscarCentroMira(Transform camara, float alcance, out RaycastHit centro)
        {
            Vector3 adelante = camara.forward;
            float avance = Mathf.Clamp(Vector3.Dot(transform.position - camara.position, adelante), 0f, MaximoAvanceMira);
            return BuscarImpacto(camara.position + adelante * avance, adelante, alcance, out centro);
        }

        /// <summary>
        /// T-1527: sin dispersión, la bala llega al punto de la mira salvo que algo bloquee de verdad entre la boca y el punto (más de
        /// <see cref="ToleranciaBloqueo"/> m antes). Con dispersión se tira el rayo del cono como antes.
        /// </summary>
        private bool BuscarImpactoDeBala(Vector3 origen, Vector3 direccion, float alcance, bool haciaPunto, float largoAlPunto, RaycastHit centro, out RaycastHit mejor)
        {
            if (!haciaPunto)
            {
                return BuscarImpacto(origen, direccion, alcance, out mejor);
            }
            float tramo = Mathf.Max(0.01f, largoAlPunto - ToleranciaBloqueo);
            if (BuscarImpacto(origen, direccion, tramo, out mejor))
            {
                return true;
            }
            mejor = centro;
            mejor.distance = largoAlPunto;
            return true;
        }

        /// <summary>T-1527: los vehículos (patrullas, carros) se alcanzan hasta este múltiplo del alcance del arma, sin caída de daño.</summary>
        public const float FactorAlcanceVehiculos = 2f;

        private static bool EsVehiculo(Collider c) => c != null && c.GetComponentInParent<IMontable>() != null;

        // Dentro del alcance del arma, o un vehículo (que se alcanza más lejos).
        private static bool DentroDelAlcance(RaycastHit impacto, float alcance) => impacto.distance <= alcance + 0.01f || EsVehiculo(impacto.collider);

        /// <summary>T-1527: lo que se adelanta el rayo de la mira desde la cámara hasta el plano del jugador, como máximo (m).</summary>
        private const float MaximoAvanceMira = 8f;
        /// <summary>T-1527: margen (m) antes del punto de la mira en que un obstáculo todavía no bloquea la bala.</summary>
        private const float ToleranciaBloqueo = 0.35f;

        private void AlCambiar() => Cambio?.Invoke();

        private void ActualizarVisual()
        {
            for (int i = 1; i < CantidadArmas; i++)
            {
                bool dispara = i == (int)armaRetroceso;
                if (luces[i] != null)
                {
                    luces[i].enabled = dispara && Time.time < finLuz;
                }
                if (visuales[i] != null)
                {
                    AjusteSensacion peso = dispara ? AjusteDe((ArmaAPie)i) : null;
                    float distancia = peso != null && peso.retrocesoArma > 0f ? peso.retrocesoArma : retroceso;
                    float duracion = peso != null && peso.retrocesoArma > 0f ? peso.duracionRetrocesoArma : duracionRetroceso;
                    // T-1486: sube rápido y regresa suave (ReglaRetrocesoArma), con el peso de cada arma.
                    float atras = dispara ? ReglaRetrocesoArma.Desplazamiento(Time.time - inicioRetroceso, duracion) : 0f;
                    // S1: el cañón va por el +Z del arma (en la mano, el arma ya no cuelga en -Y); el retroceso va hacia atrás por ese eje.
                    visuales[i].localPosition = reposos[i] - visuales[i].localRotation * Vector3.forward * (distancia * atras);
                }
            }
            if (marcas == null)
            {
                return;
            }
            for (int i = 0; i < marcas.Length; i++)
            {
                if (marcas[i].gameObject.activeSelf && Time.time >= finMarcas[i])
                {
                    marcas[i].gameObject.SetActive(false);
                }
            }
        }

        private void CrearMarcas()
        {
            marcas = new Transform[maxMarcas];
            finMarcas = new float[maxMarcas];
            for (int i = 0; i < maxMarcas; i++)
            {
                GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marca.name = "MarcaImpacto";
                Destroy(marca.GetComponent<Collider>());
                marca.transform.localScale = new Vector3(tamanoMarca, tamanoMarca, 0.01f);
                MeshRenderer render = marca.GetComponent<MeshRenderer>();
                if (materialMarca != null)
                {
                    render.sharedMaterial = materialMarca;
                }
                render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                marca.SetActive(false);
                marcas[i] = marca.transform;
            }
        }

        private void PonerMarca(Vector3 punto, Vector3 normal)
        {
            Transform marca = marcas[siguienteMarca];
            finMarcas[siguienteMarca] = Time.time + duracionMarca;
            siguienteMarca = (siguienteMarca + 1) % marcas.Length;
            marca.position = punto + normal * 0.01f;
            marca.rotation = Quaternion.LookRotation(-normal);
            marca.gameObject.SetActive(true);
        }
    }
}

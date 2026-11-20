using System;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Combate.Voces;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Pandillero a pie (T-195, D-tony-169): cápsula greybox que espera en su punto, persigue al jugador a pie que entra a su radio,
    /// avisa su golpe con el brazo en naranja y pega a la vida del jugador por <see cref="IRecibeDanio"/>. Recibe daño con
    /// <see cref="ModeloVida"/>, parpadea, cae en KO y desaparece. Lo leen los OJOS por <see cref="ILecturaRival"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PandilleroAPie : MonoBehaviour, IVida, IRecibeDanio, ILecturaRival, IMarcaMapa, ICriminalCalle
    {
        private const float Gravedad = -20f;
        private const float AlturaCentroImpacto = 1f;
        private const float GradosKO = 90f;
        private const float GranDistancia = 100000f;
        private static readonly int IdColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private DatosPandilleroAPie datos;
        [Tooltip("T-714: siempre en alerta contra el jugador (los SKULLS de refuerzo que bajan del carro), aunque no tengan un grupo con territorio.")]
        [SerializeField] private bool siempreAlerta;
        [Tooltip("T-1497: cuánto más grande que el guardia de 1.85 m es este agente (x). Sube la boca del arma, el grito y la zona de la cabeza.")]
        [Min(0.25f)] [SerializeField] private float escalaCuerpo = 1f;
        [Tooltip("Cuerpo que parpadea al recibir daño.")]
        [SerializeField] private Renderer cuerpo;
        [Tooltip("Pivote del hombro del brazo que golpea. Sube durante el aviso.")]
        [SerializeField] private Transform brazo;
        [Tooltip("Piezas del brazo que se pintan en naranja durante el aviso.")]
        [SerializeField] private Renderer[] piezasBrazo;

        private CharacterController controlador;
        private ModeloVida modelo;
        private ReglaPandilleroAPie regla;
        private CalculoPatrullaPandillero patrulla;
        private TurnosPandillaAPie turnos;
        private MaterialPropertyBlock bloque;
        private Vector3 hogar;
        private bool tieneHogar;
        private float velocidadVertical;
        private float finParpadeo = -1f;
        private float desaparicion = -1f;
        private Transform raizJugador;
        // T-1106: ganchos de conducta y filtro de daño
        private IConductaAPie conducta;
        private IFiltroDanioAPie filtroDanio;
        private bool castigableExtra;
        [Tooltip("Pandilla de este pandillero si no tiene Grupo (T-1106). Ninguna = sin pandilla.")]
        [SerializeField] private IdPandilla pandillaPropia = IdPandilla.Ninguna;
        private IVida vidaJugador;
        private IRecibeDanio recibeJugador;
        private bool pintadoAviso;
        private PistolaPandillero pistola;
        // T-693: el jefe cuerpo a cuerpo
        private ReglaJefeNova reglaJefe;
        private Light brilloJefe;
        private Vector3 direccionCarga;
        private bool cargaGolpeo;
        private int pasoBrillo = -1;
        private Animator animador;
        private VisualPersonaje visual;
        private Renderer[] piezasModelo;
        private int lado = 1;
        private float proximoCambioLado;
        // T-423: táctica
        private ReglaTacticaPandillero tactica;
        private ReglaTacticaPandillero.Modo modoAnterior = ReglaTacticaPandillero.Modo.Rodear;
        private bool tieneCubierta;
        private Vector3 puntoCubierta;
        private Vector3 puntoAsomo;
        private float proximaBusqueda;
        private bool disparoReciente;
        private float proximoGrito;
        private float finGrito = -1f;
        private TextMesh textoGrito;
        private static readonly Collider[] candidatosCubierta = new Collider[24];
        private const float AlturaGrito = 2.3f;
        private const float PeriodoBusquedaCubierta = 1.5f;
        private const float EnfriamientoGrito = 4f;
        private const float SegundosGrito = 1.5f;
        private const float AlturaMinCubierta = 1.3f; // T-712: agachado la cabeza llega a 1.33 m (medido): tiene que taparlo un muro, carro o contenedor
        private const float TamanoMaxCubierta = 30f;
        private const float MargenCubierta = 0.8f;
        private const float LlegadaCubierta = 0.5f;
        private const float FactorHuida = 1.15f;

        /// <summary>Pandilla a la que pertenece (T-423): la asigna <see cref="PandillaAPie"/>.</summary>
        public PandillaAPie Grupo { get; internal set; }

        /// <summary>Modo táctico actual (T-423); Rodear si no es armado.</summary>
        public ReglaTacticaPandillero.Modo ModoTactico => tactica != null ? tactica.Actual : ReglaTacticaPandillero.Modo.Rodear;

        /// <summary>True si eligió un punto de cubierta (T-423).</summary>
        public bool TieneCubierta => tieneCubierta;

        /// <summary>T-712: True mientras está agachado detrás de su cubierta.</summary>
        public bool EstaAgachado => visual != null && visual.Agachado;
        public Vector3 PuntoCubierta => puntoCubierta;

        private static readonly int IdVelocidad = Animator.StringToHash("Velocidad");
        private static readonly int IdEnPiso = Animator.StringToHash("EnPiso");
        private static readonly int IdVelY = Animator.StringToHash("VelY");
        /// <summary>T-1306: bajando más rápido que esto (m/s) ya no está en el piso para el animador (escalones y rampas no cuentan como caída).</summary>
        private const float VelocidadCaidaVisible = -3f;
        private int tieneVelY = -1;

        // T-1306: el animador sabe si va cayendo (EnPiso y VelY): un SKULL que salta de un techo usa la caída (Loco_Caida) y el aterrizaje, no camina en el aire.
        private void PonerEnPiso()
        {
            bool enPiso = controlador == null || controlador.isGrounded || velocidadVertical > VelocidadCaidaVisible;
            animador.SetBool(IdEnPiso, enPiso);
            if (tieneVelY < 0)
            {
                tieneVelY = 0;
                foreach (AnimatorControllerParameter parametro in animador.parameters)
                {
                    if (parametro.nameHash == IdVelY && parametro.type == AnimatorControllerParameterType.Float)
                    {
                        tieneVelY = 1;
                    }
                }
            }
            if (tieneVelY == 1)
            {
                animador.SetFloat(IdVelY, enPiso ? 0f : velocidadVertical);
            }
        }
        private static readonly int IdApuntando = Animator.StringToHash("Apuntando");
        private static readonly int IdArma = Animator.StringToHash("Arma");
        private static readonly int IdKO = Animator.StringToHash("KO");
        private static readonly int IdDisparo = Animator.StringToHash("Disparo");
        private const float AlturaBoca = 1.4f;
        private const float AlturaPecho = 1.2f;
        private const float AlturaCuerpoDePie = 1.8f;
        private CharacterController cuerpoJugador;
        private const float AdelanteBoca = 0.4f;
        private const int ArmaPistola = 1;

        /// <summary>Los pandilleros activos (T-405): los aliados los buscan aquí.</summary>
        public static readonly List<PandilleroAPie> Activos = new List<PandilleroAPie>();

        /// <summary>A quién apunta ahora (el jugador o un aliado), o null (T-405).</summary>
        public Transform ObjetivoActual { get; private set; }

        /// <summary>T-933: dónde está, para que la policía lo vea.</summary>
        public Vector3 Posicion => transform.position;

        /// <summary>T-933: true si no es policía, sigue en pie y está apuntando a alguien.</summary>
        public bool EstaAtacando => datos != null && !datos.EsPolicia && !EstaKO && ObjetivoActual != null;

        public DatosPandilleroAPie Datos => datos;

        /// <summary>T-1497: factor de tamaño del agente respecto al guardia de referencia (1 = 1.85 m).</summary>
        public float EscalaCuerpo => escalaCuerpo;

        /// <summary>T-1497: de dónde sale el tiro: 1.4 m de alto y 0.4 m al frente a escala 1, más arriba y más lejos en los grandes.</summary>
        private Vector3 BocaDelArma() => transform.position + Vector3.up * AlturaBoca * escalaCuerpo + transform.forward * AdelanteBoca * escalaCuerpo;

        /// <summary>Fase del patrón del jefe cuerpo a cuerpo (T-693); Acecho si no lo es.</summary>
        public ReglaJefeNova.Fase FaseJefe => reglaJefe != null ? reglaJefe.Actual : ReglaJefeNova.Fase.Acecho;

        /// <summary>True mientras el jefe se recupera de un golpe o una carga: el momento de castigarlo (T-693).</summary>
        public bool Castigable => (reglaJefe != null && reglaJefe.Vulnerable) || castigableExtra;

        /// <summary>Cuántos golpes de área ha dado desde su última carga (T-693).</summary>
        public int GolpesDesdeCarga => reglaJefe != null ? reglaJefe.GolpesDesdeCarga : 0;
        public EstadoPandillero Estado => regla != null ? regla.Estado : EstadoPandillero.Esperando;
        public bool EnPelea => regla != null && !EstaKO && regla.EnPelea;
        public Vector3 Hogar => hogar;
        /// <summary>True cuando la pandilla dice que el jugador está en su territorio (solo pandilleros armados).</summary>
        public bool Alerta
        {
            get => alerta || Provocado || (percepcion != null && percepcion.Activa);
            set
            {
                if (Acosando)
                {
                    return;
                }
                if (value && !alerta)
                {
                    Sonidos.ReproducirEn(VozPandillero("PAN_RIVAL_ALERTA"), transform.position);
                    // T-624: se burla al verte (Skull_Burla; el jefe, Jefe_Burla).
                    if (visual != null)
                    {
                        visual.Burlar();
                    }
                }
                alerta = value || siempreAlerta || (datos != null && datos.EsPolicia);
            }
        }

        // T-1494: percepción de disparos (solo los guardias armados de Nova con datos.ReaccionaADisparos).
        private ReglaPercepcionDisparo percepcion;
        private const float DistanciaLlegadaBusqueda = 2f;
        private const float GradosBusqueda = 90f;
        private const float CercaniaDisparoJugador = 3f;

        /// <summary>T-1494: la memoria de disparos (null si este pandillero no reacciona a disparos).</summary>
        public ReglaPercepcionDisparo Percepcion => percepcion;

        /// <summary>T-1494: avisa que el jugador disparó o pegó desde <paramref name="punto"/>: el guardia va hacia allá, lo busca y lo ataca.</summary>
        public void AvisarDisparo(Vector3 punto)
        {
            if (percepcion != null && modelo != null && !modelo.EstaKO)
            {
                percepcion.Avisar(punto);
            }
        }

        private void AlOirDisparo(Vector3 donde)
        {
            if (percepcion == null || EstaKO || raizJugador == null || !EsAtacable())
            {
                return;
            }
            // Solo los disparos del jugador (los de otros enemigos no cuentan).
            if (DistanciaPlana(donde, raizJugador.position) > CercaniaDisparoJugador || DistanciaPlana(transform.position, donde) > datos.RadioOido)
            {
                return;
            }
            percepcion.Avisar(raizJugador.position);
        }

        private bool alerta;
        private float provocadoHasta = -1f;
        private bool esJefe;

        /// <summary>T-1009: true mientras dura la defensa tras recibir daño (solo si sus datos tienen seDefiende).</summary>
        public bool Provocado => datos != null && datos.SeDefiende && Time.time < provocadoHasta;
        private bool policiaYaSeCubrio;

        /// <summary>True si es el pandillero de un <see cref="JefePandilla"/>: sale más grande en el minimapa (T-409).</summary>
        public bool EsJefe => esJefe;

        /// <summary>Lo llama <see cref="JefePandilla"/> en su Awake.</summary>
        public void MarcarComoJefe() => esJefe = true;

        // ---- IMarcaMapa (T-409): sale en el minimapa mientras está activo y en pie ----
        TipoMarcaMapa IMarcaMapa.Tipo => esJefe ? TipoMarcaMapa.Jefe : TipoMarcaMapa.Enemigo;
        Vector3 IMarcaMapa.Posicion => transform.position;
        bool IMarcaMapa.Visible => isActiveAndEnabled && !EstaKO;
        public float Actual => modelo != null ? modelo.Actual : 0f;
        public float Maxima => modelo != null ? modelo.Maxima : 0f;
        public bool EstaKO => modelo != null && modelo.EstaKO;

        public event Action Cambio;
        public event Action KO;
        /// <summary>Se dispara una vez al caer en KO.</summary>
        public event Action<PandilleroAPie> Vencido;
        /// <summary>S2: se dispara después de aplicar cada daño que recibe (también el que lo deja KO), con quién y cuánto.</summary>
        public event Action<PandilleroAPie, InfoDanio> DanioRecibido;

        /// <summary>
        /// S2: mientras es true está acosando a un civil (lo maneja <see cref="GrupoAcoso"/>): no persigue, no patrulla, no pide refuerzos
        /// al recibir daño y no acepta la alerta del territorio.
        /// </summary>
        public bool Acosando { get; set; }

        /// <summary>S2: el visual animado (null si es un greybox sin él).</summary>
        public VisualPersonaje Visual => visual;

        // ---- ILecturaRival ----
        string ILecturaRival.Nombre => datos != null ? datos.Nombre : name;
        Transform ILecturaRival.Raiz => transform;
        IVida ILecturaRival.Vida => this;
        bool ILecturaRival.EsMecha => false;
        bool ILecturaRival.Preparando => regla != null && regla.Estado == EstadoPandillero.Preparando;
        TipoAtaqueLeido ILecturaRival.Proximo => TipoAtaqueLeido.Golpe;

        float ILecturaRival.SegundosParaAtacar
        {
            get
            {
                if (regla == null || EstaKO)
                {
                    return -1f;
                }
                switch (regla.Estado)
                {
                    case EstadoPandillero.Preparando:
                        return 0f;
                    case EstadoPandillero.Recargando:
                        return regla.RecargaRestante;
                    case EstadoPandillero.Persiguiendo:
                        return turnos == null || turnos.Tiene(this) ? 0f : -1f;
                    default:
                        return -1f;
                }
            }
        }

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            ResolverGanchos();
            if (datos == null || (!datos.UsaPistola && !datos.JefeCuerpoACuerpo && !datos.PerroRobot && (cuerpo == null || brazo == null || piezasBrazo == null || piezasBrazo.Length == 0)))
            {
                Debug.LogError("[IA] Faltan referencias en PandilleroAPie de " + name, this);
                enabled = false;
                return;
            }
            Iniciar();
        }

        // Start corre un cuadro después: una conducta agregada justo tras instanciar también se encuentra.
        private void Start() => ResolverGanchos();

        private void ResolverGanchos()
        {
            conducta = GetComponent<IConductaAPie>();
            if (conducta == null && datos != null && datos.EsPolicia)
            {
                conducta = gameObject.AddComponent<ConductaEscaleraPolicia>(); // T-1303: si estás en un techo, sube por la escalera
            }
            if (conducta == null && datos != null && datos.PerroRobot)
            {
                conducta = gameObject.AddComponent<ConductaPerroNova>(); // T-1494: el perro robot de Nova patrulla, te detecta y muerde
            }
            filtroDanio = GetComponent<IFiltroDanioAPie>();
        }

        /// <summary>T-1494: el paso de patrulla (m/s) alrededor de su punto, para las conductas que lo reusan.</summary>
        public Vector3 PasoDePatrulla() => VelocidadDePatrulla();

        private void Iniciar()
        {
            if (modelo != null || datos == null)
            {
                return;
            }
            modelo = new ModeloVida(datos.VidaMaxima);
            modelo.Cambio += AlCambiar;
            modelo.KO += AlLlegarAKO;
            if (datos.UsaPistola)
            {
                // El territorio lo decide la pandilla (Alerta): la regla solo ve distancias enormes de detección y correa.
                regla = new ReglaPandilleroAPie(GranDistancia, GranDistancia, datos.DistanciaMaxTiro, datos.PreparacionGolpe,
                    datos.RecargaGolpe, datos.MargenLlegada);
                tactica = new ReglaTacticaPandillero(datos.FraccionHuida, datos.SegundosCubierto, datos.SegundosAsomado);
                if (datos.ReaccionaADisparos && !datos.EsPolicia)
                {
                    percepcion = new ReglaPercepcionDisparo(datos.SegundosBusqueda, DistanciaLlegadaBusqueda);
                }
                pistola = GetComponent<PistolaPandillero>();
                if (pistola == null)
                {
                    pistola = gameObject.AddComponent<PistolaPandillero>();
                }
                animador = GetComponentInChildren<Animator>(true);
                visual = GetComponentInChildren<VisualPersonaje>(true);
                piezasModelo = GetComponentsInChildren<Renderer>(true);
            }
            else if (datos.JefeCuerpoACuerpo)
            {
                // T-693: la pandilla no decide; el jefe usa su patrón (ReglaJefeNova) y la alerta la pone JefePandilla.
                regla = new ReglaPandilleroAPie(GranDistancia, GranDistancia, datos.AlcanceJefe, datos.PreparacionGolpe,
                    datos.RecargaGolpe, datos.MargenLlegada);
                reglaJefe = new ReglaJefeNova(datos.AlcanceJefe, datos.TelegrafiaGolpeJefe, datos.RecuperaGolpeJefe, datos.GolpesPorCarga,
                    datos.TelegrafiaCarga, datos.DuracionCarga, datos.RecuperaCarga, datos.DistanciaMinCarga, datos.DistanciaMaxCarga);
                animador = GetComponentInChildren<Animator>(true);
                visual = GetComponentInChildren<VisualPersonaje>(true);
                piezasModelo = GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                regla = new ReglaPandilleroAPie(datos.RadioDeteccion, datos.Correa, datos.AlcanceGolpe, datos.PreparacionGolpe,
                    datos.RecargaGolpe, datos.MargenLlegada);
            }
            patrulla = new CalculoPatrullaPandillero(datos.RadioPatrulla, datos.EsperaPatrullaMinima, datos.EsperaPatrullaMaxima,
                datos.MargenLlegada, datos.SegundosAtascoPatrulla, () => UnityEngine.Random.value);
            alerta = datos.EsPolicia || siempreAlerta; // T-709: el policía siempre está en alerta contra el jugador; T-714: y el refuerzo de los SKULLS
            bloque = new MaterialPropertyBlock();
            if (!tieneHogar)
            {
                hogar = transform.position;
            }
            PintarCuerpo(datos.ColorNormal);
            PintarBrazo(datos.ColorNormal);
        }

        private void OnEnable()
        {
            Registro<ILecturaRival>.Agregar(this);
            Registro<IMarcaMapa>.Agregar(this);
            Activos.Add(this);
            Registro<ICriminalCalle>.Agregar(this);
            EventosJugador.Desperto += AlDespertarElJugador;
            if (percepcion != null)
            {
                EventosRuido.Disparo += AlOirDisparo;
            }
            if (datos != null && datos.ProvocaAntesDeAtacar)
            {
                EventosRuido.Disparo += AlRespuestaDelJugador; // T-1528
                EventosRuido.ArmaSacada += AlRespuestaDelJugador;
            }
        }

        /// <summary>
        /// T-1328: el jugador despertó con el Doc. La policía se va; los demás dejan de apuntarle, se calman y vuelven a su punto
        /// (no se curan ni reaparecen: solo sueltan la persecución).
        /// </summary>
        private void AlDespertarElJugador()
        {
            if (EstaKO || Acosando)
            {
                return;
            }
            if (datos != null && datos.EsPolicia)
            {
                Destroy(gameObject);
                return;
            }
            alerta = siempreAlerta;
            if (!hostilFijo)
            {
                hostilDesde = -1f; // T-1528: despertar calma a los maleantes; los hostiles "completos" (guion, pruebas) siguen atacando
            }
            provocadoHasta = -1f;
            regla?.Reiniciar();
            reglaJefe?.Reiniciar();
            tactica?.Reiniciar();
            percepcion?.Reiniciar();
            patrulla?.Reiniciar();
            tieneCubierta = false;
            disparoReciente = false;
            modoAnterior = ReglaTacticaPandillero.Modo.Rodear;
            turnos?.Soltar(this);
            ApagarBrillo();
            ObjetivoActual = null;
        }

        private void OnDisable()
        {
            EventosJugador.Desperto -= AlDespertarElJugador;
            EventosRuido.Disparo -= AlOirDisparo;
            EventosRuido.Disparo -= AlRespuestaDelJugador;
            EventosRuido.ArmaSacada -= AlRespuestaDelJugador;
            Registro<ICriminalCalle>.Quitar(this);
            Registro<ILecturaRival>.Quitar(this);
            Registro<IMarcaMapa>.Quitar(this);
            Activos.Remove(this);
            Cuerpos.Remove(this);
            ObjetivoActual = null;
        }

        private void OnDestroy()
        {
            EventosJugador.Desperto -= AlDespertarElJugador;
            EventosRuido.Disparo -= AlOirDisparo;
            EventosRuido.Disparo -= AlRespuestaDelJugador;
            EventosRuido.ArmaSacada -= AlRespuestaDelJugador;
            Registro<ILecturaRival>.Quitar(this);
            Registro<IMarcaMapa>.Quitar(this);
            Activos.Remove(this);
            Cuerpos.Remove(this);
            if (modelo != null)
            {
                modelo.Cambio -= AlCambiar;
                modelo.KO -= AlLlegarAKO;
            }
        }

        /// <summary>Fija su punto y los turnos de su pandilla. Se puede llamar antes del Awake.</summary>
        public void Configurar(Vector3 punto, TurnosPandillaAPie turnosPandilla)
        {
            hogar = punto;
            tieneHogar = true;
            turnos = turnosPandilla;
        }

        /// <summary>Vuelve a su punto con la vida llena, listo para esperar. El objeto debe estar activo.</summary>
        public void Reaparecer()
        {
            Iniciar();
            modelo.Restaurar();
            regla.Reiniciar();
            reglaJefe?.Reiniciar();
            ApagarBrillo();
            tactica?.Reiniciar();
            percepcion?.Reiniciar();
            patrulla?.Reiniciar();
            tieneCubierta = false;
            disparoReciente = false;
            modoAnterior = ReglaTacticaPandillero.Modo.Rodear;
            turnos?.Soltar(this);
            desaparicion = -1f;
            Cuerpos.Remove(this);
            finParpadeo = -1f;
            velocidadVertical = 0f;
            if (visual != null)
            {
                visual.Agachado = false;
                visual.Caido = false;
            }
            else if (animador != null)
            {
                animador.SetBool(IdKO, false);
            }
            controlador.enabled = false;
            transform.SetPositionAndRotation(hogar, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            controlador.enabled = true;
            PintarCuerpo(datos.ColorNormal);
            PintarBrazo(datos.ColorNormal);
            PonerBrazo(0f);
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (modelo == null || modelo.EstaKO || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }
            if (visual != null)
            {
                visual.RegistrarGolpe(info.Direccion); // antes de aplicar: si este golpe lo deja KO, decide cómo cae
            }
            float cantidad = info.Cantidad * (Castigable ? datos.MultiplicadorCastigo : 1f); // T-693: recuperándose, recibe más
            TipoBloqueo bloqueoFiltro = TipoBloqueo.Ninguno;
            if (filtroDanio != null)
            {
                // T-1106: el escudo decide cuánto entra de la cantidad ya multiplicada.
                cantidad = filtroDanio.Filtrar(this, new InfoDanio(cantidad, info.Tipo, info.Punto, info.Direccion, info.Atacante), out bloqueoFiltro);
                if (cantidad <= 0f)
                {
                    ResultadoDanio bloqueado = new ResultadoDanio(0f, bloqueoFiltro, false);
                    EventosPelea.PublicarImpacto(null, info, bloqueado);
                    return bloqueado;
                }
            }
            float aplicado = modelo.Aplicar(cantidad);
            if (EsMaleanteDeCalle && !ConductaRina.GolpeDeRinaEnCurso && !DanioDeLaPolicia)
            {
                VolverseHostil(); // T-1528: el jugador respondió
            }
            if (datos.SeDefiende)
            {
                // T-1009: si lo golpean o le disparan, pelea contra el jugador un buen rato.
                provocadoHasta = Time.time + datos.SegundosDefensa;
            }
            if (percepcion != null && raizJugador != null && !modelo.EstaKO)
            {
                // T-1494: un impacto desde cualquier distancia: va hacia donde vino, y bajo fuego busca cubierta.
                percepcion.Avisar(raizJugador.position);
                disparoReciente = true;
            }
            if (datos.UsaPistola && !modelo.EstaKO && Grupo != null && !Acosando)
            {
                // T-423: pide refuerzos a su grupo.
                Grupo.Avisar(this, "¡Refuerzos!", true);
            }
            if (DanioDeLaPolicia && modelo.EstaKO && !datos.EsPolicia)
            {
                CayoPorLaPolicia = true;
            }
            ResultadoDanio resultado = new ResultadoDanio(aplicado, bloqueoFiltro, modelo.EstaKO);
            if (!modelo.EstaKO && bloqueoFiltro != TipoBloqueo.Normal) // T-1109: un golpe bloqueado por el escudo no hace parpadear ni herir al guardia
            {
                finParpadeo = Time.time + datos.DuracionParpadeo;
                if (visual != null)
                {
                    visual.Herir(info.Direccion);
                }
                PintarCuerpo(datos.ColorGolpe);
            }
            if (datos.EsPolicia)
            {
                // T-709: dispararle a la policía sube la búsqueda; matarla, mucho más.
                ICalor calor = Registro<ICalor>.Actual;
                if (calor != null && !DanioDeLaPolicia)
                {
                    calor.Sumar(modelo.EstaKO ? datos.CalorAlMatar : datos.CalorAlGolpear, MotivoCalor.Pelea);
                    // T-1303: desde el primer golpe o bala a un policía ya te buscan y atacan. T-1322: dispararle = 2 estrellas; matarlo = 3.
                    EstrellasBusqueda.SubirA(calor, modelo.EstaKO ? EstrellasBusqueda.EstrellasPorMatarPolicia : EstrellasBusqueda.EstrellasPorDispararAPolicia);
                }
            }
            EventosPelea.PublicarImpacto(null, info, resultado);
            DanioRecibido?.Invoke(this, info);
            return resultado;
        }

        private void Update()
        {
            if (modelo == null)
            {
                return;
            }
            if (finParpadeo >= 0f && Time.time >= finParpadeo)
            {
                finParpadeo = -1f;
                PintarCuerpo(modelo.EstaKO ? datos.ColorKO : datos.ColorNormal);
            }
            if (modelo.EstaKO)
            {
                if (desaparicion >= 0f && Time.time >= desaparicion)
                {
                    desaparicion = -1f;
                    gameObject.SetActive(false);
                }
                return;
            }

            ActualizarGrito();
            BuscarJugador();
            if (conducta != null && conducta.Controla)
            {
                conducta.Actualizar(this, Time.deltaTime); // T-1106
                return;
            }
            if (Acosando)
            {
                ActualizarAcosando();
                return;
            }
            if (datos.JefeCuerpoACuerpo)
            {
                ActualizarJefeNova();
                return;
            }
            if (datos.UsaPistola)
            {
                if (EnProvocacion())
                {
                    ActualizarProvocando();
                    return;
                }
                ActualizarArmado();
                return;
            }
            bool atacable = EsAtacable();
            Vector3 posicionJugador = raizJugador != null ? raizJugador.position : hogar;
            ReglaPandilleroAPie.Entrada entrada = new ReglaPandilleroAPie.Entrada
            {
                DistanciaJugador = atacable ? DistanciaPlana(transform.position, posicionJugador) : float.MaxValue,
                DistanciaJugadorAlPunto = atacable ? DistanciaPlana(hogar, posicionJugador) : float.MaxValue,
                DistanciaAlPunto = DistanciaPlana(transform.position, hogar),
                JugadorAtacable = atacable
            };
            entrada.TieneTurno = regla.Atacando || (atacable && entrada.DistanciaJugador <= datos.AlcanceGolpe
                && (turnos == null || turnos.Pedir(this)));

            ReglaPandilleroAPie.Salida salida = regla.Avanzar(Time.deltaTime, entrada);
            if (!regla.Atacando)
            {
                turnos?.Soltar(this);
            }

            Vector3 destino = salida.Orden == ReglaPandilleroAPie.Orden.AlPunto ? hogar : posicionJugador;
            Vector3 paso = salida.Orden == ReglaPandilleroAPie.Orden.Quieto ? VelocidadDePatrulla() : Vector3.zero;
            Mover(salida.Orden, destino, regla.Estado != EstadoPandillero.Preparando && atacable && paso.sqrMagnitude < PasoMinimoCuadrado, paso);
            if (salida.Impacto)
            {
                Golpear(posicionJugador);
            }
            ActualizarBrazo();
        }

        // S2: mientras acosa solo cae al piso; la posición, la mirada y los gestos los pone el GrupoAcoso.
        private void ActualizarAcosando()
        {
            ObjetivoActual = null;
            velocidadVertical = controlador.isGrounded ? -1f : velocidadVertical + Gravedad * Time.deltaTime;
            controlador.Move(Vector3.up * velocidadVertical * Time.deltaTime);
        }

        // ---- Maleante de calle (T-1528): provoca primero, ataca solo si el jugador responde ----

        private float hostilDesde = -1f;
        private bool hostilFijo;
        private float ultimaAlertaHostil;
        private float proximoEmpujon = -1f;
        private float proximoTiroLimitado;

        /// <summary>T-1528: true si es un maleante de calle: SKULLS/SNAKES con el dato activo, de un grupo que no es de guion de misión (ni jefe, policía ni Nova).</summary>
        public bool EsMaleanteDeCalle => datos != null && datos.ProvocaAntesDeAtacar && datos.UsaPistola && !datos.EsPolicia && !datos.JefeCuerpoACuerpo
            && !datos.ReaccionaADisparos && !datos.PerroRobot && !esJefe && !siempreAlerta && Grupo != null && !Grupo.EsDeGuion
            && (Grupo.Pandilla == IdPandilla.Calaveras || Grupo.Pandilla == IdPandilla.Viboras);

        /// <summary>T-1528: true si el jugador ya respondió (le pegó, sacó el arma o disparó cerca) y este maleante ataca de verdad.</summary>
        public bool EsHostil => hostilDesde >= 0f;

        /// <summary>T-1528: segundos que lleva atacando (0 si no es hostil).</summary>
        public float SegundosHostil => !EsHostil ? 0f : hostilFijo ? ReglaProvocacionCalle.SegundosRampa + Time.time - hostilDesde : Time.time - hostilDesde;

        /// <summary>T-1528: el jugador respondió: este maleante y los de su grupo atacan desde ya.</summary>
        public void VolverseHostil()
        {
            if (!EsMaleanteDeCalle)
            {
                return;
            }
            Grupo.HacerHostiles();
            PonerHostil();
        }

        /// <summary>
        /// T-1528: lo vuelve hostil. Con <paramref name="completo"/> (pruebas y guion) ataca ya con el daño, la precisión y la cadencia completos y no se calma solo.
        /// </summary>
        internal void PonerHostil(bool completo = false)
        {
            if (EstaKO || (EsHostil && !completo))
            {
                return;
            }
            hostilFijo = completo;
            // Con completo, SegundosHostil ya cuenta la rampa entera (ver abajo). hostilDesde no puede ser negativo: con menos de 90 s de juego
            // "Time.time - rampa" daba negativo y EsHostil quedaba en falso (los maleantes nunca atacaban).
            hostilDesde = Time.time;
            ultimaAlertaHostil = Time.time;
            proximoTiroLimitado = completo ? Time.time : Time.time + ReglaProvocacionCalle.RecargaExtraInicial;
        }

        private void AlRespuestaDelJugador(Vector3 punto)
        {
            if (raizJugador == null || EsHostil || EstaKO || !EsMaleanteDeCalle)
            {
                return;
            }
            if (ReglaProvocacionCalle.EsRespuesta(punto, raizJugador.position, transform.position))
            {
                VolverseHostil();
            }
        }

        // Alerta contra el jugador pero sin respuesta de su parte: provoca en vez de atacar. Si ya era hostil, se calma tras un rato sin alerta.
        private bool EnProvocacion()
        {
            if (!EsMaleanteDeCalle)
            {
                return false;
            }
            if (EsHostil)
            {
                if (Alerta)
                {
                    ultimaAlertaHostil = Time.time;
                }
                else if (!hostilFijo && Time.time - ultimaAlertaHostil > ReglaProvocacionCalle.SegundosCalma)
                {
                    hostilDesde = -1f;
                }
                return false;
            }
            return Alerta && EsAtacable();
        }

        private void ActualizarProvocando()
        {
            ObjetivoActual = null; // no apunta a nadie: la policía no lo toma por criminal
            turnos?.Soltar(this);
            pistola?.QuitarAviso();
            Vector3 hacia = (raizJugador != null ? raizJugador.position : hogar) - transform.position;
            hacia.y = 0f;
            float distancia = hacia.magnitude;
            Vector3 adelante = distancia > 0.0001f ? hacia / distancia : transform.forward;
            Vector3 deseada = distancia > ReglaProvocacionCalle.DistanciaProvocar
                ? adelante * Mathf.Min(datos.Velocidad, ReglaProvocacionCalle.VelocidadAcercamiento) : Vector3.zero;
            if (distancia <= ReglaProvocacionCalle.DistanciaEmpujon + 4f && Time.time >= proximoGrito)
            {
                Gritar(ReglaProvocacionCalle.Gritos[UnityEngine.Random.Range(0, ReglaProvocacionCalle.Gritos.Length)]);
                visual?.Burlar();
            }
            if (distancia <= ReglaProvocacionCalle.DistanciaEmpujon && Time.time >= proximoEmpujon)
            {
                proximoEmpujon = Time.time + UnityEngine.Random.Range(ReglaProvocacionCalle.EmpujonMinimo, ReglaProvocacionCalle.EmpujonMaximo);
                AnimarGolpe(false); // no hay clip de empujón: se usa el golpe suave
                DaniarJugador(ReglaProvocacionCalle.DanioEmpujon, adelante);
            }
            if (adelante.sqrMagnitude > 0.0001f)
            {
                Quaternion meta = Quaternion.LookRotation(adelante, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, datos.VelocidadGiro * Time.deltaTime);
            }
            velocidadVertical = controlador.isGrounded ? -1f : velocidadVertical + Gravedad * Time.deltaTime;
            Vector3 mover = deseada;
            mover.y = velocidadVertical;
            controlador.Move(mover * Time.deltaTime);
            if (animador != null)
            {
                animador.SetFloat(IdVelocidad, deseada.magnitude);
                PonerEnPiso();
                if (visual != null)
                {
                    visual.Agachado = false;
                    visual.Apuntando = false;
                    visual.TipoArma = TipoArmaVisual.Pistola;
                }
                else
                {
                    animador.SetBool(IdApuntando, false);
                }
            }
        }

        // ---- Pandillero armado (T-402) ----

        private const float FactorAlcanceCriminal = 1.5f;

        /// <summary>T-729: True solo mientras un policía dispara (el daño que recibe un criminal en ese momento viene de la policía, no del jugador).</summary>
        public static bool DanioDeLaPolicia { get; private set; }

        /// <summary>T-729: True si este pandillero cayó por un disparo de la policía: no suma búsqueda al jugador.</summary>
        public bool CayoPorLaPolicia { get; private set; }

        /// <summary>
        /// T-729: el criminal más cercano para la policía: un pandillero que no es policía, está vivo y apunta ahora mismo a alguien (al jugador o a un aliado), dentro de
        /// una vez y media el alcance de tiro. Null si no hay.
        /// </summary>
        public PandilleroAPie CriminalMasCercano()
        {
            PandilleroAPie mejor = null;
            float menor = datos.DistanciaMaxTiro * FactorAlcanceCriminal;
            foreach (PandilleroAPie otro in Activos)
            {
                if (otro == null || otro == this || !otro.isActiveAndEnabled || otro.datos == null || otro.datos.EsPolicia || otro.EstaKO || otro.ObjetivoActual == null)
                {
                    continue;
                }
                float d = DistanciaPlana(transform.position, otro.transform.position);
                if (d < menor)
                {
                    menor = d;
                    mejor = otro;
                }
            }
            return mejor;
        }

        /// <summary>Altura del pecho del jugador: la normal escalada por lo que mide su cápsula (agachado o rodando baja).</summary>
        private float AlturaPechoJugador()
        {
            if (cuerpoJugador == null && raizJugador != null)
            {
                cuerpoJugador = raizJugador.GetComponent<CharacterController>();
            }
            return cuerpoJugador != null ? ReglaPandilleroAPie.AlturaPechoSegunCuerpo(AlturaPecho, cuerpoJugador.height, AlturaCuerpoDePie) : AlturaPecho;
        }

        private void ActualizarArmado()
        {
            bool atacable = Alerta && EsAtacable();
            Vector3 posicionJugador = raizJugador != null ? raizJugador.position : hogar;
            IRecibeDanio recibe = recibeJugador;
            ObjetivoActual = atacable ? raizJugador : null;
            // T-729: la policía ataca primero a quien comete el delito (un SKULL que está disparando al jugador, el asaltante), no al jugador.
            if (datos.EsPolicia)
            {
                PandilleroAPie criminal = CriminalMasCercano();
                if (criminal != null)
                {
                    atacable = true;
                    recibe = criminal;
                    posicionJugador = criminal.transform.position;
                    ObjetivoActual = criminal.transform;
                }
            }
            // T-1494: el guardia que solo sabe de ti por un disparo ve con línea de vista y, si te pierde, va a tu última posición y te busca.
            bool soloPercepcion = percepcion != null && !alerta && atacable && ObjetivoActual == raizJugador;
            bool ve = true;
            bool buscando = false;
            if (soloPercepcion)
            {
                Vector3 pechoReal = posicionJugador + Vector3.up * AlturaPechoJugador();
                Vector3 bocaReal = BocaDelArma();
                Vector3 haciaReal = pechoReal - bocaReal;
                ve = haciaReal.magnitude <= datos.DistanciaVision && pistola.HayLinea(bocaReal, haciaReal.normalized, haciaReal.magnitude, recibe, out _);
                percepcion.Avanzar(Time.deltaTime, ve, posicionJugador, transform.position);
                posicionJugador = percepcion.Ultima;
                buscando = percepcion.Actual == ReglaPercepcionDisparo.Estado.Buscando;
                if (!percepcion.Activa)
                {
                    atacable = false;
                    ObjetivoActual = null;
                    provocadoHasta = -1f; // se cansó de buscar: no sigue omnisciente por la defensa
                }
            }
            float distancia = DistanciaPlana(transform.position, posicionJugador);
            // T-673: si el jugador se agacha su pecho baja con su cápsula y una cobertura baja le tapa la línea de vista.
            Vector3 pecho = posicionJugador + Vector3.up * (ObjetivoActual == raizJugador ? AlturaPechoJugador() : AlturaPecho);
            Vector3 boca = BocaDelArma();
            bool puedeTirar = false;
            if (atacable && ve && distancia <= datos.DistanciaMaxTiro)
            {
                Vector3 hacia = pecho - boca;
                puedeTirar = pistola.HayLinea(boca, hacia.normalized, hacia.magnitude, recibe, out _);
            }

            // T-423: táctica (rodear, cubrirse, asomarse, huir).
            float fraccionVida = modelo.Maxima > 0f ? modelo.Actual / modelo.Maxima : 1f;
            bool acorralado = atacable && distancia <= datos.DistanciaAcorralado;
            if (atacable && modoAnterior == ReglaTacticaPandillero.Modo.Rodear && Time.time >= proximaBusqueda)
            {
                BuscarCubierta(posicionJugador);
            }
            ReglaTacticaPandillero.Modo modo = tactica.Avanzar(Time.deltaTime, fraccionVida, tieneCubierta, acorralado, disparoReciente,
                !esJefe && atacable);
            disparoReciente = false;
            if (modo == ReglaTacticaPandillero.Modo.Cubrirse && modoAnterior != ReglaTacticaPandillero.Modo.Cubrirse)
            {
                BuscarCubierta(posicionJugador);
                if (!tieneCubierta)
                {
                    tactica.Reiniciar();
                    modo = ReglaTacticaPandillero.Modo.Rodear;
                }
            }
            if (modo == ReglaTacticaPandillero.Modo.Huir && modoAnterior != ReglaTacticaPandillero.Modo.Huir)
            {
                Gritar("¡Me largo!");
            }
            if ((datos.EsPolicia || (datos.UsaPistola && !esJefe)) && !policiaYaSeCubrio && tieneCubierta && modo == ReglaTacticaPandillero.Modo.Rodear)
            {
                // T-709: al bajarse de la patrulla corre a cubrirse detrás de ella antes de disparar.
                policiaYaSeCubrio = true;
                tactica.ForzarCubrirse();
                modo = ReglaTacticaPandillero.Modo.Cubrirse;
            }
            modoAnterior = modo;
            bool huyendo = modo == ReglaTacticaPandillero.Modo.Huir;

            ReglaPandilleroAPie.Entrada entrada = new ReglaPandilleroAPie.Entrada
            {
                DistanciaJugador = atacable && !huyendo ? distancia : float.MaxValue,
                DistanciaJugadorAlPunto = atacable && !huyendo ? 0f : float.MaxValue,
                DistanciaAlPunto = DistanciaPlana(transform.position, hogar),
                JugadorAtacable = atacable && !huyendo
            };
            bool limitado = EsMaleanteDeCalle; // T-1528: daño, precisión y cadencia limitados al empezar a atacar (suben con el tiempo)
            float segundosAtacando = SegundosHostil;
            entrada.TieneTurno = regla.Atacando || (puedeTirar && tactica.PuedeDisparar && (!limitado || Time.time >= proximoTiroLimitado)
                && (turnos == null || turnos.Pedir(this)));
            ReglaPandilleroAPie.Salida salida = regla.Avanzar(Time.deltaTime, entrada);
            if (!regla.Atacando)
            {
                turnos?.Soltar(this);
            }

            // T-676: sin línea de vista no hay aviso ni disparo (barricadas y muros).
            bool apuntando = regla.Estado == EstadoPandillero.Preparando && atacable && !huyendo && puedeTirar;
            if (apuntando)
            {
                pistola.Apuntar(boca, pecho, regla.ProgresoAviso);
            }
            else
            {
                pistola.QuitarAviso();
            }
            if (salida.Impacto && atacable && !huyendo && puedeTirar)
            {
                disparoReciente = true;
                float probabilidad = PistolaPandillero.ProbabilidadAcierto(distancia, datos.DistanciaAciertoCerca,
                    datos.DistanciaAciertoLejos, datos.AciertoCerca, datos.AciertoLejos);
                float danioTiro = datos.DanioDisparo;
                if (limitado)
                {
                    probabilidad *= ReglaProvocacionCalle.EscalaPrecision(segundosAtacando);
                    danioTiro *= ReglaProvocacionCalle.EscalaDanio(segundosAtacando);
                    proximoTiroLimitado = Time.time + ReglaProvocacionCalle.RecargaExtra(segundosAtacando);
                }
                DanioDeLaPolicia = datos.EsPolicia; // T-729: lo que caiga por este disparo no suma búsqueda al jugador
                Sonidos.ReproducirEn(MechaCyberpunk.Personaje.Combate.SonidosArmasT1472.IdDisparoEnemigo(datos.Perdigones), boca); // T-1472: el disparo enemigo, en 3D
                for (int perdigon = 0; perdigon < datos.Perdigones; perdigon++)
                {
                    pistola.Disparar(boca, pecho, recibe, danioTiro, probabilidad);
                }
                DanioDeLaPolicia = false;
                if (visual != null)
                {
                    visual.Disparar();
                }
                else if (animador != null)
                {
                    animador.SetTrigger(IdDisparo);
                }
            }

            Vector3 deseada = Vector3.zero;
            Vector3 haciaJugador = posicionJugador - transform.position;
            haciaJugador.y = 0f;
            if (salida.Orden == ReglaPandilleroAPie.Orden.AlPunto)
            {
                deseada = hogar - transform.position;
                deseada.y = 0f;
                deseada = deseada.sqrMagnitude > 0.0001f ? deseada.normalized * datos.Velocidad : Vector3.zero;
            }
            else if (huyendo)
            {
                Vector3 lejos = -haciaJugador;
                deseada = distancia < datos.DistanciaHuida && lejos.sqrMagnitude > 0.0001f
                    ? lejos.normalized * datos.Velocidad * FactorHuida : Vector3.zero;
            }
            else if (atacable && regla.Estado != EstadoPandillero.Preparando && haciaJugador.sqrMagnitude > 0.0001f)
            {
                Vector3 adelante = haciaJugador.normalized;
                if (modo == ReglaTacticaPandillero.Modo.Cubrirse || modo == ReglaTacticaPandillero.Modo.Asomado)
                {
                    Vector3 meta = modo == ReglaTacticaPandillero.Modo.Cubrirse ? puntoCubierta : puntoAsomo;
                    Vector3 aMeta = meta - transform.position;
                    aMeta.y = 0f;
                    deseada = aMeta.magnitude > LlegadaCubierta ? aMeta.normalized * datos.Velocidad : Vector3.zero;
                }
                else if (soloPercepcion && !ve)
                {
                    // T-1494: no te ve: va a tu última posición conocida (y ahí te busca).
                    deseada = buscando ? Vector3.zero : adelante * datos.Velocidad;
                }
                else if (distancia > datos.DistanciaMaxTiro || !puedeTirar && distancia > datos.DistanciaMinTiro)
                {
                    deseada = adelante * datos.Velocidad;
                }
                else if (distancia < datos.DistanciaMinTiro)
                {
                    deseada = -adelante * datos.Velocidad;
                }
                else
                {
                    deseada = VelocidadRodeo(posicionJugador);
                }
                if (datos.EsPolicia)
                {
                    deseada = VelocidadPolicia(deseada, adelante, distancia, puedeTirar, modo);
                }
            }

            if (deseada.sqrMagnitude < PasoMinimoCuadrado && !soloPercepcion)
            {
                deseada = VelocidadDePatrulla();
            }
            Vector3 mirada = atacable && !huyendo ? haciaJugador : deseada;
            if (buscando && !ve)
            {
                // T-1494: buscando en su última posición conocida: gira mirando alrededor.
                mirada = Quaternion.Euler(0f, GradosBusqueda * Time.deltaTime, 0f) * transform.forward;
            }
            if (datos.EsPolicia && deseada.magnitude > VelocidadCaminarPolicia + 0.5f)
            {
                mirada = deseada; // T-1303: corriendo mira hacia donde corre (no corre de lado ni de espaldas)
            }
            mirada.y = 0f;
            if (mirada.sqrMagnitude > 0.0001f)
            {
                Quaternion meta = Quaternion.LookRotation(mirada.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, datos.VelocidadGiro * Time.deltaTime);
            }
            velocidadVertical = controlador.isGrounded ? -1f : velocidadVertical + Gravedad * Time.deltaTime;
            Vector3 mover = deseada;
            mover.y = velocidadVertical;
            controlador.Move(mover * Time.deltaTime);

            if (animador != null)
            {
                // T-1321: el policía anima con la velocidad real del cuerpo (caminar apuntando, sin deslizarse).
                float velocidadAnimada = deseada.magnitude;
                if (datos.EsPolicia)
                {
                    Vector3 real = controlador.velocity;
                    velocidadAnimada = ReglaAnimacionPolicia.VelocidadVista(new Vector2(real.x, real.z).magnitude, velocidadAnimada);
                }
                animador.SetFloat(IdVelocidad, velocidadAnimada);
                PonerEnPiso();
                if (visual != null)
                {
                    // T-712: detrás de la cubierta, ya llegado, se agacha (Cobertura_Entrar); al asomarse se para y apunta.
                    visual.Agachado = tieneCubierta && modo == ReglaTacticaPandillero.Modo.Cubrirse && !huyendo
                        && DistanciaPlana(transform.position, puntoCubierta) <= LlegadaCubierta + 0.5f;
                    visual.Apuntando = apuntando;
                    visual.TipoArma = TipoArmaVisual.Pistola;
                }
                else
                {
                    animador.SetBool(IdApuntando, apuntando);
                    animador.SetInteger(IdArma, ArmaPistola);
                }
            }
        }

        // ---- Jefe cuerpo a cuerpo (T-693) ----

        private void ActualizarJefeNova()
        {
            bool atacable = Alerta && EsAtacable();
            Vector3 posicionJugador = raizJugador != null ? raizJugador.position : hogar;
            float distancia = atacable ? DistanciaPlana(transform.position, posicionJugador) : float.MaxValue;
            ObjetivoActual = atacable ? raizJugador : null;
            ReglaJefeNova.Salida salida = reglaJefe.Avanzar(Time.deltaTime, atacable, distancia);
            Vector3 haciaJugador = posicionJugador - transform.position;
            haciaJugador.y = 0f;
            if (salida.InicioCarga)
            {
                direccionCarga = haciaJugador.sqrMagnitude > 0.0001f ? haciaJugador.normalized : transform.forward;
                cargaGolpeo = false;
                Gritar("¡¡¡AAARGH!!!");
            }
            Vector3 deseada = Vector3.zero;
            if (salida.Orden == ReglaJefeNova.Orden.Acercarse && haciaJugador.sqrMagnitude > 0.0001f)
            {
                deseada = haciaJugador.normalized * datos.Velocidad;
            }
            else if (salida.Orden == ReglaJefeNova.Orden.Cargar)
            {
                deseada = direccionCarga * datos.VelocidadCarga;
            }
            bool cargando = reglaJefe.Actual == ReglaJefeNova.Fase.Carga;
            Vector3 mirada = cargando ? direccionCarga : haciaJugador;
            mirada.y = 0f;
            if (!reglaJefe.Vulnerable && mirada.sqrMagnitude > 0.0001f)
            {
                Quaternion meta = Quaternion.LookRotation(mirada.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, datos.VelocidadGiro * Time.deltaTime);
            }
            if (salida.Impacto)
            {
                GolpeDeArea(posicionJugador);
            }
            if (cargando && !cargaGolpeo && atacable && distancia <= datos.RadioCarga)
            {
                cargaGolpeo = true;
                Vector3 puntoCarga = posicionJugador + Vector3.up * AlturaCentroImpacto;
                recibeJugador.RecibirDanio(new InfoDanio(datos.DanioCarga, TipoDanio.Golpe, puntoCarga, direccionCarga, null));
                Sonidos.ReproducirEn(VozPandillero("PAN_RIVAL_ALERTA"), transform.position);
            }
            MostrarBrillo(reglaJefe.ProgresoAviso, reglaJefe.Actual == ReglaJefeNova.Fase.TelegrafiaCarga);
            velocidadVertical = controlador.isGrounded ? -1f : velocidadVertical + Gravedad * Time.deltaTime;
            Vector3 mover = deseada;
            mover.y = velocidadVertical;
            controlador.Move(mover * Time.deltaTime);
            if (animador != null)
            {
                animador.SetFloat(IdVelocidad, deseada.magnitude);
                PonerEnPiso();
            }
        }

        // Golpe en área corta delante del jefe: conecta si el jugador está a su radio y dentro del arco frontal. Rodar (invulnerable) lo esquiva.
        private void GolpeDeArea(Vector3 posicionJugador)
        {
            if (visual != null)
            {
                visual.Golpear(true);
            }
            Sonidos.ReproducirEn(VozPandillero("PAN_RIVAL_ALERTA"), transform.position);
            if (!EsAtacable())
            {
                return;
            }
            Vector3 hacia = posicionJugador - transform.position;
            hacia.y = 0f;
            if (hacia.magnitude > datos.RadioGolpeJefe || (hacia.sqrMagnitude > 0.0001f && Vector3.Angle(transform.forward, hacia) > AnguloAreaJefe))
            {
                return;
            }
            Vector3 punto = posicionJugador + Vector3.up * AlturaCentroImpacto;
            recibeJugador.RecibirDanio(new InfoDanio(datos.DanioGolpeJefe, TipoDanio.Golpe, punto, transform.forward, null));
        }

        private const float AnguloAreaJefe = 80f;

        // Brillo del aviso: el cuerpo y una luz suben de naranja (golpe) o rojo (carga) mientras avisa.
        private void MostrarBrillo(float progreso, bool esCarga)
        {
            int paso = Mathf.RoundToInt(progreso * 10f) + (esCarga ? 100 : 0);
            if (paso == pasoBrillo)
            {
                return;
            }
            pasoBrillo = paso;
            if (progreso <= 0f)
            {
                PintarCuerpo(datos.ColorNormal);
                if (brilloJefe != null)
                {
                    brilloJefe.enabled = false;
                }
                return;
            }
            Color tinte = Color.Lerp(datos.ColorNormal, esCarga ? datos.ColorGolpe : datos.ColorAviso, progreso);
            PintarCuerpo(tinte);
            if (brilloJefe == null)
            {
                GameObject objeto = new GameObject("BrilloJefe");
                objeto.transform.SetParent(transform, false);
                objeto.transform.localPosition = Vector3.up * 1.3f;
                brilloJefe = objeto.AddComponent<Light>();
                brilloJefe.type = LightType.Point;
                brilloJefe.range = 7f;
                brilloJefe.shadows = LightShadows.None;
            }
            brilloJefe.color = esCarga ? datos.ColorGolpe : datos.ColorAviso;
            brilloJefe.intensity = 6f * progreso;
            brilloJefe.enabled = true;
        }

        private void ApagarBrillo()
        {
            pasoBrillo = -1;
            if (brilloJefe != null)
            {
                brilloJefe.enabled = false;
            }
        }

        /// <summary>Luz del brillo del aviso (T-693); null si todavía no avisó.</summary>
        public Light BrilloJefe => brilloJefe;

        // ---- API para conductas (T-1106) ----

        /// <summary>Raíz del jugador (Transform), o null si no hay.</summary>
        public Transform Jugador => raizJugador;

        /// <summary>True si el jugador existe, va a pie y sigue en pie: se le puede pegar.</summary>
        public bool JugadorAtacable => EsAtacable();

        /// <summary>Pandilla de este pandillero: la de su <see cref="Grupo"/> o, si no tiene, la que se le puso en el inspector.</summary>
        public IdPandilla Pandilla => Grupo != null ? Grupo.Pandilla : pandillaPropia;

        /// <summary>Fracción de vida que le queda, de 0 a 1.</summary>
        public float VidaFraccion => modelo != null && modelo.Maxima > 0f ? modelo.Actual / modelo.Maxima : 1f;

        /// <summary>Activa o apaga un castigo extra: mientras es true, <see cref="Castigable"/> es true y recibe más daño.</summary>
        public void SetCastigable(bool valor) => castigableExtra = valor;

        /// <summary>Mueve con gravedad y controlador a <paramref name="velocidadPlana"/> (m/s en XZ) y avisa al animador (Velocidad y EnPiso).</summary>
        public void MoverPlano(Vector3 velocidadPlana)
        {
            velocidadPlana.y = 0f;
            velocidadVertical = controlador.isGrounded ? -1f : velocidadVertical + Gravedad * Time.deltaTime;
            Vector3 mover = velocidadPlana;
            mover.y = velocidadVertical;
            controlador.Move(mover * Time.deltaTime);
            if (animador != null)
            {
                animador.SetFloat(IdVelocidad, velocidadPlana.magnitude);
                PonerEnPiso();
            }
        }

        /// <summary>Gira en el plano hacia <paramref name="dir"/> a lo más <paramref name="gradosPorSegundo"/> (°/s).</summary>
        public void MirarHacia(Vector3 dir, float gradosPorSegundo)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < PasoMinimoCuadrado)
            {
                return;
            }
            Quaternion meta = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, gradosPorSegundo * Time.deltaTime);
        }

        /// <summary>Le pega al jugador (si es atacable) con <paramref name="cantidad"/> de daño en <paramref name="direccion"/>.</summary>
        public void DaniarJugador(float cantidad, Vector3 direccion)
        {
            if (!EsAtacable())
            {
                return;
            }
            Vector3 punto = raizJugador.position + Vector3.up * AlturaCentroImpacto;
            recibeJugador.RecibirDanio(new InfoDanio(cantidad, TipoDanio.Golpe, punto, direccion, null));
        }

        /// <summary>Le pega a otro objetivo (por ejemplo un rival de otra pandilla) con <paramref name="cantidad"/> de daño.</summary>
        public void DaniarA(IRecibeDanio objetivo, float cantidad, Vector3 direccion)
        {
            if (objetivo == null)
            {
                return;
            }
            Vector3 punto = transform.position + Vector3.up * AlturaCentroImpacto + direccion.normalized * AdelanteBoca;
            objetivo.RecibirDanio(new InfoDanio(cantidad, TipoDanio.Golpe, punto, direccion, null));
        }

        /// <summary>Muestra el aviso de un ataque: el cuerpo y una luz suben hacia <paramref name="color"/> según <paramref name="progreso"/> (0 a 1; 0 lo apaga).</summary>
        public void MostrarAviso(float progreso, Color color)
        {
            int paso = progreso <= 0f ? -2 : Mathf.RoundToInt(progreso * 10f) + 1000 + Mathf.RoundToInt(color.r * 10f) * 7 + Mathf.RoundToInt(color.g * 10f) * 13 + Mathf.RoundToInt(color.b * 10f) * 29;
            if (paso == pasoBrillo)
            {
                return;
            }
            pasoBrillo = paso;
            if (progreso <= 0f)
            {
                PintarCuerpo(datos.ColorNormal);
                if (brilloJefe != null)
                {
                    brilloJefe.enabled = false;
                }
                return;
            }
            PintarCuerpo(Color.Lerp(datos.ColorNormal, color, progreso));
            if (brilloJefe == null)
            {
                GameObject objeto = new GameObject("BrilloJefe");
                objeto.transform.SetParent(transform, false);
                objeto.transform.localPosition = Vector3.up * 1.3f;
                brilloJefe = objeto.AddComponent<Light>();
                brilloJefe.type = LightType.Point;
                brilloJefe.range = 7f;
                brilloJefe.shadows = LightShadows.None;
            }
            brilloJefe.color = color;
            brilloJefe.intensity = 6f * progreso;
            brilloJefe.enabled = true;
        }

        /// <summary>Juega la animación de golpe (fuerte o normal) del visual, si tiene.</summary>
        public void AnimarGolpe(bool fuerte)
        {
            if (visual != null)
            {
                visual.Golpear(fuerte);
            }
        }

        // ---- Táctica (T-423) ----

        // Se reparte en un arco alrededor del jugador (centrado en el lado de su grupo) y avanza hacia su puesto.
        private Vector3 VelocidadRodeo(Vector3 posicionJugador)
        {
            int indice = 0;
            int total = 1;
            Grupo?.Repartir(this, out indice, out total);
            Vector3 deGrupo = (Grupo != null ? Grupo.Centro : hogar) - posicionJugador;
            float centro = Mathf.Atan2(deGrupo.x, deGrupo.z) * Mathf.Rad2Deg + datos.GiroRodeo * Time.time;
            float angulo = ReglaTacticaPandillero.AnguloRodeo(indice, total, centro, datos.ArcoRodeo) * Mathf.Deg2Rad;
            float radio = (datos.DistanciaMinTiro + datos.DistanciaMaxTiro) * 0.5f;
            Vector3 puesto = posicionJugador + new Vector3(Mathf.Sin(angulo), 0f, Mathf.Cos(angulo)) * radio;
            Vector3 aPuesto = puesto - transform.position;
            aPuesto.y = 0f;
            return aPuesto.magnitude > LlegadaCubierta * 2f ? aPuesto.normalized * datos.Velocidad * 0.8f : Vector3.zero;
        }

        // Busca la cubierta cercana (pared, carro, caja) cuyo lado lejano al jugador quede tapado. Rara vez: no es por cuadro.
        private void BuscarCubierta(Vector3 posicionJugador)
        {
            proximaBusqueda = Time.time + PeriodoBusquedaCubierta;
            tieneCubierta = false;
            int cantidad = Physics.OverlapSphereNonAlloc(transform.position, datos.RadioCubierta, candidatosCubierta, ~0,
                QueryTriggerInteraction.Ignore);
            Vector3 pechoJugador = posicionJugador + Vector3.up * AlturaPechoJugador();
            float mejor = float.MaxValue;
            for (int i = 0; i < cantidad; i++)
            {
                Collider c = candidatosCubierta[i];
                Bounds caja = CajaDeCubierta(c);
                // T-1511: una cobertura CB (barrera, costales, cajas, barricada) sirve aunque sea baja y se prefiere a otra más cercana.
                bool esCb = c.GetComponentInParent<CoberturaCB>() != null;
                float bono = esCb ? CoberturaCB.BonoPreferencia : 0f;
                if ((caja.size.y < AlturaMinCubierta && !esCb) || Mathf.Max(caja.size.x, caja.size.z) > TamanoMaxCubierta
                    || c.GetComponentInParent<CharacterController>() != null)
                {
                    continue;
                }
                Vector3 centroCaja = caja.center;
                centroCaja.y = transform.position.y;
                Vector3 d = centroCaja - posicionJugador;
                d.y = 0f;
                if (d.sqrMagnitude < 0.01f)
                {
                    continue;
                }
                d.Normalize();
                float extension = Mathf.Abs(d.x) * caja.extents.x + Mathf.Abs(d.z) * caja.extents.z;
                Vector3 puntoBase = centroCaja + d * (extension + MargenCubierta);
                if (DistanciaPlana(transform.position, puntoBase) - caja.extents.magnitude - bono >= mejor)
                {
                    continue;
                }
                Vector3 lateral = Vector3.Cross(Vector3.up, d);
                float extensionLateral = Mathf.Abs(lateral.x) * caja.extents.x + Mathf.Abs(lateral.z) * caja.extents.z;
                // T-711: cada uno toma su propio puesto detrás de la cubierta (los policías, una esquina cada uno).
                if (!ElegirPuesto(puntoBase, lateral, extensionLateral, pechoJugador, c, out Vector3 punto, out float desvio))
                {
                    continue;
                }
                float distancia = DistanciaPlana(transform.position, punto) - bono;
                if (distancia >= mejor)
                {
                    continue;
                }
                mejor = distancia;
                puntoCubierta = punto;
                lado = desvio < 0f ? -1 : 1;
                puntoAsomo = centroCaja + lateral * lado * (extensionLateral + MargenCubierta);
                tieneCubierta = true;
            }
        }

        private const float BordeEsquina = 0.15f;
        private const float AlturaPechoAgachado = 0.8f;
        private static readonly List<float> desvios = new List<float>(16);

        // T-711: busca el puesto libre a lo largo de la cubierta: los policías prefieren las esquinas (la más cercana a su lado primero) y los demás
        // el centro y luego a los lados. Un puesto está libre si nadie más tiene el suyo a menos de PuestosDeCubierta.Separacion y la cubierta lo tapa del jugador.
        private bool ElegirPuesto(Vector3 puntoBase, Vector3 lateral, float extensionLateral, Vector3 pechoJugador, Collider cubierta,
            out Vector3 punto, out float desvio)
        {
            float tope = Mathf.Max(0f, extensionLateral - BordeEsquina);
            float haciaMi = Vector3.Dot(transform.position - puntoBase, lateral) >= 0f ? 1f : -1f;
            PuestosDeCubierta.Ordenar(desvios, tope, haciaMi, datos != null && datos.EsPolicia);
            foreach (float candidato in desvios)
            {
                Vector3 propuesta = puntoBase + lateral * candidato;
                // T-714: un puesto desde el que no alcanza a disparar al jugador no sirve (se quedaría escondido sin pelear): tiene que estar dentro del tiro.
                if (DistanciaPlana(propuesta, pechoJugador) > datos.DistanciaMaxTiro || PuestoOcupado(propuesta))
                {
                    continue;
                }
                // Agachado el pecho queda a AlturaPechoAgachado. La esquina de un policía junto a su patrulla vale sin revisar la línea: el collider
                // de un carro es bajo en las puntas, pero el carro entero es su cubierta.
                bool esquina = datos != null && datos.EsPolicia && Mathf.Abs(candidato) >= tope - 0.01f && tope > 0.1f && cubierta.attachedRigidbody != null;
                Vector3 pecho = propuesta + Vector3.up * AlturaPechoAgachado;
                if (!esquina && (!Physics.Linecast(pechoJugador, pecho, out RaycastHit golpe, ~0, QueryTriggerInteraction.Ignore) || !MismaCubierta(golpe.collider, cubierta)))
                {
                    continue;
                }
                punto = propuesta;
                desvio = candidato;
                return true;
            }
            punto = puntoBase;
            desvio = 0f;
            return false;
        }

        private bool PuestoOcupado(Vector3 punto)
        {
            foreach (PandilleroAPie otro in Activos)
            {
                if (otro == this || otro == null || !otro.tieneCubierta || otro.EstaKO)
                {
                    continue;
                }
                if (DistanciaPlana(otro.puntoCubierta, punto) < PuestosDeCubierta.Separacion)
                {
                    return true;
                }
            }
            return false;
        }

        // T-709: un carro (varios colliders en un cuerpo) cubre como un solo bulto: sus colliders sueltos son bajitos, juntos tapan a una persona.
        private static Bounds CajaDeCubierta(Collider c)
        {
            Rigidbody cuerpoDelCarro = c.attachedRigidbody;
            if (cuerpoDelCarro == null)
            {
                return c.bounds;
            }
            Bounds caja = c.bounds;
            foreach (Collider otro in cuerpoDelCarro.GetComponentsInChildren<Collider>())
            {
                if (otro.enabled && !otro.isTrigger)
                {
                    caja.Encapsulate(otro.bounds);
                }
            }
            return caja;
        }

        private static bool MismaCubierta(Collider golpeado, Collider cubierta)
        {
            return golpeado == cubierta || (cubierta.attachedRigidbody != null && golpeado.attachedRigidbody == cubierta.attachedRigidbody);
        }

        /// <summary>Muestra un grito corto sobre la cabeza (T-423). Tiene enfriamiento.</summary>
        public void Gritar(string texto)
        {
            if (Time.time < proximoGrito || string.IsNullOrEmpty(texto))
            {
                return;
            }
            proximoGrito = Time.time + EnfriamientoGrito;
            finGrito = Time.time + SegundosGrito;
            if (textoGrito == null)
            {
                GameObject objeto = new GameObject("Grito");
                objeto.transform.SetParent(transform, false);
                objeto.transform.localPosition = Vector3.up * AlturaGrito * escalaCuerpo;
                textoGrito = objeto.AddComponent<TextMesh>();
                textoGrito.anchor = TextAnchor.LowerCenter;
                textoGrito.alignment = TextAlignment.Center;
                textoGrito.characterSize = 0.12f;
                textoGrito.fontSize = 48;
                textoGrito.color = new Color(1f, 0.85f, 0.2f);
                Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoGrito.font = fuente;
                objeto.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            }
            textoGrito.text = texto;
            textoGrito.gameObject.SetActive(true);
        }

        /// <summary>Texto del grito que se ve ahora, o null.</summary>
        public string GritoActual => textoGrito != null && textoGrito.gameObject.activeSelf ? textoGrito.text : null;

        private void ActualizarGrito()
        {
            if (textoGrito == null || !textoGrito.gameObject.activeSelf)
            {
                return;
            }
            if (Time.time >= finGrito || EstaKO)
            {
                textoGrito.gameObject.SetActive(false);
                return;
            }
            Camera camara = Camera.main;
            if (camara != null)
            {
                textoGrito.transform.rotation = camara.transform.rotation;
            }
        }

        private void BuscarJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Transform raiz = jugador != null ? jugador.Raiz : null;
            if (raiz == raizJugador)
            {
                return;
            }
            raizJugador = raiz;
            vidaJugador = raiz != null ? raiz.GetComponentInChildren<IVida>() : null;
            recibeJugador = raiz != null ? raiz.GetComponentInChildren<IRecibeDanio>() : null;
        }

        private bool EsAtacable()
        {
            // T-933: la policía solo va contra el jugador si lo están buscando.
            ICalor calor = Registro<ICalor>.Actual;
            if (datos != null && datos.EsPolicia && calor != null && calor.Nivel <= 0)
            {
                return false;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            // T-1307: el jugador en carro o moto también es blanco (las balas le dan; ver PistolaPandillero.HayLinea). El mecha no.
            return jugador != null && raizJugador != null && (jugador.Modo == ModoJugador.APie || jugador.Modo == ModoJugador.Carro) && recibeJugador != null
                && (vidaJugador == null || !vidaJugador.EstaKO);
        }

        /// <summary>T-1303: el policía corre a lo más a esto (m/s): el clip de correr va a 3.19 m/s; más rápido se ve flotando.</summary>
        public const float VelocidadCorrerPolicia = 3.6f;
        /// <summary>T-1303: caminando mientras dispara (el clip de caminar va a ~1.3-1.6 m/s).</summary>
        public const float VelocidadCaminarPolicia = 1.4f;
        /// <summary>T-1303: a esta distancia (m, en el plano) el policía ya te tocó: se detiene (el arresto lo cuenta Trafico).</summary>
        public const float DistanciaTocarPolicia = 0.9f;

        /// <summary>
        /// T-1303: el policía no corre de lado ni de espaldas con el clip de caminar de frente: lejos corre hacia ti (a la rapidez del clip), en rango camina
        /// hacia ti disparando y muy cerca se planta. A cubrirse sí corre (mirando hacia donde corre).
        /// </summary>
        private Vector3 VelocidadPolicia(Vector3 deseada, Vector3 adelante, float distancia, bool puedeTirar, ReglaTacticaPandillero.Modo modo)
        {
            if (modo == ReglaTacticaPandillero.Modo.Cubrirse || modo == ReglaTacticaPandillero.Modo.Asomado)
            {
                return Vector3.ClampMagnitude(deseada, VelocidadCorrerPolicia);
            }
            if (distancia > datos.DistanciaMaxTiro || (!puedeTirar && distancia > datos.DistanciaMinTiro))
            {
                return adelante * Mathf.Min(datos.Velocidad, VelocidadCorrerPolicia);
            }
            // Camina hacia ti disparando; ya cerca sigue hasta tocarte (así te arresta, T-1303) y ahí se planta.
            return distancia > DistanciaTocarPolicia ? adelante * VelocidadCaminarPolicia : Vector3.zero;
        }

        /// <summary>
        /// T-1303: si el calor todavía no llega a la primera estrella, lo sube lo justo para llegar (en pasos del 5 % del máximo; los umbrales viven en los datos
        /// de Progresion, que IA no conoce).
        /// </summary>
        public static void SubirAPrimeraEstrella(ICalor calor)
        {
            if (calor == null || calor.Maximo <= 0f)
            {
                return;
            }
            float paso = calor.Maximo * 0.05f;
            for (int i = 0; i < 40 && calor.Nivel <= 0 && calor.Valor < calor.Maximo; i++)
            {
                calor.Sumar(paso, MotivoCalor.Mision); // la víctima es la policía: no pregunta por testigos (igual que ReglaPrimeraEstrella de Trafico)
            }
        }

        // T-446: mientras espera en su zona, camina de un punto a otro cerca de su hogar. Sale de la patrulla en cuanto deja de esperar.
        private const float PasoMinimoCuadrado = 0.0001f;

        private Vector3 VelocidadDePatrulla()
        {
            if (patrulla == null || !patrulla.Activa)
            {
                return Vector3.zero;
            }
            if (regla.Estado != EstadoPandillero.Esperando)
            {
                if (patrulla.HayDestino || patrulla.Esperando)
                {
                    patrulla.Reiniciar();
                }
                return Vector3.zero;
            }
            return patrulla.Avanzar(Time.deltaTime, transform.position, hogar) * datos.VelocidadPatrulla;
        }

        private void Mover(ReglaPandilleroAPie.Orden orden, Vector3 destino, bool mirarAlJugador, Vector3 pasoPatrulla)
        {
            Vector3 hacia = destino - transform.position;
            hacia.y = 0f;
            Vector3 velocidad = Vector3.zero;
            if (orden == ReglaPandilleroAPie.Orden.Quieto && pasoPatrulla.sqrMagnitude >= PasoMinimoCuadrado)
            {
                velocidad = pasoPatrulla;
                hacia = pasoPatrulla;
            }
            else if (orden != ReglaPandilleroAPie.Orden.Quieto && hacia.sqrMagnitude > 0.0001f)
            {
                velocidad = hacia.normalized * datos.Velocidad;
            }
            Vector3 mirada = orden == ReglaPandilleroAPie.Orden.AlPunto || !mirarAlJugador ? velocidad : hacia;
            if (mirarAlJugador && raizJugador != null)
            {
                mirada = raizJugador.position - transform.position;
            }
            mirada.y = 0f;
            if (mirada.sqrMagnitude > 0.0001f)
            {
                Quaternion meta = Quaternion.LookRotation(mirada.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, datos.VelocidadGiro * Time.deltaTime);
            }
            velocidadVertical = controlador.isGrounded ? -1f : velocidadVertical + Gravedad * Time.deltaTime;
            velocidad.y = velocidadVertical;
            controlador.Move(velocidad * Time.deltaTime);
        }

        private void Golpear(Vector3 posicionJugador)
        {
            if (!EsAtacable())
            {
                return;
            }
            Vector3 hacia = posicionJugador - transform.position;
            hacia.y = 0f;
            float distancia = hacia.magnitude;
            if (distancia > datos.AlcanceGolpe + datos.MargenImpacto)
            {
                return;
            }
            if (distancia > 0.0001f && Vector3.Angle(transform.forward, hacia) > datos.AnguloGolpe)
            {
                return;
            }
            Vector3 punto = posicionJugador + Vector3.up * AlturaCentroImpacto;
            recibeJugador.RecibirDanio(new InfoDanio(datos.DanioGolpe, TipoDanio.Golpe, punto, transform.forward, null));
        }

        private void ActualizarBrazo()
        {
            bool aviso = regla.Estado == EstadoPandillero.Preparando;
            PonerBrazo(regla.ProgresoAviso);
            if (aviso != pintadoAviso)
            {
                pintadoAviso = aviso;
                PintarBrazo(aviso ? datos.ColorAviso : datos.ColorNormal);
            }
        }

        private void PonerBrazo(float progreso)
        {
            if (brazo == null)
            {
                return;
            }
            brazo.localRotation = Quaternion.Euler(-datos.AnguloBrazoAviso * progreso, 0f, 0f);
        }

        private void AlCambiar() => Cambio?.Invoke();

        // T-597: la voz del pandillero según su género; una de cada cuatro es femenina, repartida por el número de instancia (estable).
        private const int PandillerosPorCadaFemenina = 4;

        /// <summary>El género de la voz de este pandillero.</summary>
        public GeneroVoz Voz => EtiquetaVoz.PorSemilla(GetInstanceID(), PandillerosPorCadaFemenina);

        private string VozPandillero(string id) => EtiquetaVoz.De(id, Voz);

        /// <summary>T-746: cuántos cuerpos caídos se quedan tirados a la vez; al pasar el tope se retira el más viejo (de preferencia fuera de cámara).</summary>
        public const int MaximoCuerpos = 12;

        private static readonly List<PandilleroAPie> Cuerpos = new List<PandilleroAPie>();

        /// <summary>Los cuerpos que están tirados ahora (del más viejo al más nuevo).</summary>
        public static IReadOnlyList<PandilleroAPie> CuerposTirados => Cuerpos;

        private void RegistrarCuerpo()
        {
            Cuerpos.Remove(this);
            Cuerpos.Add(this);
            while (Cuerpos.Count > MaximoCuerpos)
            {
                int quitar = 0;
                Camera camara = Camera.main;
                if (camara != null)
                {
                    for (int i = 0; i < Cuerpos.Count - 1; i++)
                    {
                        Vector3 v = camara.WorldToViewportPoint(Cuerpos[i].transform.position);
                        if (v.z < 0f || v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f)
                        {
                            quitar = i;
                            break;
                        }
                    }
                }
                PandilleroAPie viejo = Cuerpos[quitar];
                Cuerpos.RemoveAt(quitar);
                viejo.desaparicion = -1f;
                viejo.gameObject.SetActive(false);
            }
        }

        private void AlLlegarAKO()
        {
            turnos?.Soltar(this);
            if (esJefe)
            {
                Sonidos.ReproducirEn("PAN_JEFE_CAE", transform.position);
            }
            controlador.enabled = false;
            ApagarBrillo();
            if (datos.UsaPistola || datos.JefeCuerpoACuerpo)
            {
                if (pistola != null)
                {
                    pistola.QuitarAviso();
                }
                if (visual != null)
                {
                    visual.Agachado = false;
                    visual.Caido = true;
                }
                else if (animador != null)
                {
                    animador.SetBool(IdKO, true);
                }
            }
            else
            {
                transform.rotation = Quaternion.Euler(-GradosKO, transform.eulerAngles.y, 0f);
            }
            desaparicion = Time.time + datos.SegundosParaDesaparecer;
            RegistrarCuerpo();
            PintarCuerpo(datos.ColorKO);
            PonerBrazo(0f);
            PintarBrazo(datos.ColorKO);
            pintadoAviso = false;
            Sonidos.ReproducirEn(VozPandillero("PAN_RIVAL_CAE"), transform.position);
            KO?.Invoke();
            Vencido?.Invoke(this);
            // T-686: suelta su dinero y su arma (el jefe, una escopeta); el jugador los recoge.
            EventosBotin.Publicar(new InfoBotin(transform.position, datos.Dinero,
                esJefe ? ArmaBotin.Escopeta : (datos.UsaPistola ? ArmaBotin.Pistola : ArmaBotin.Ninguna)));
        }

        private void PintarCuerpo(Color color)
        {
            Pintar(cuerpo, color);
            if (piezasModelo == null)
            {
                return;
            }
            foreach (Renderer pieza in piezasModelo)
            {
                if (!(pieza is LineRenderer))
                {
                    Pintar(pieza, color);
                }
            }
        }

        private void PintarBrazo(Color color)
        {
            if (piezasBrazo == null)
            {
                return;
            }
            foreach (Renderer pieza in piezasBrazo)
            {
                Pintar(pieza, color);
            }
        }

        private void Pintar(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }
            renderer.GetPropertyBlock(bloque);
            bloque.SetColor(IdColor, color);
            renderer.SetPropertyBlock(bloque);
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}

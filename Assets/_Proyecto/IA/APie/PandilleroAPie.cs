using System;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Pandillero a pie (T-195, D-tony-169): cápsula greybox que espera en su punto, persigue al jugador a pie que entra a su radio,
    /// avisa su golpe con el brazo en naranja y pega a la vida del jugador por <see cref="IRecibeDanio"/>. Recibe daño con
    /// <see cref="ModeloVida"/>, parpadea, cae en KO y desaparece. Lo leen los OJOS por <see cref="ILecturaRival"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PandilleroAPie : MonoBehaviour, IVida, IRecibeDanio, ILecturaRival
    {
        private const float Gravedad = -20f;
        private const float AlturaCentroImpacto = 1f;
        private const float GradosKO = 90f;
        private const float GranDistancia = 100000f;
        private static readonly int IdColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private DatosPandilleroAPie datos;
        [Tooltip("Cuerpo que parpadea al recibir daño.")]
        [SerializeField] private Renderer cuerpo;
        [Tooltip("Pivote del hombro del brazo que golpea. Sube durante el aviso.")]
        [SerializeField] private Transform brazo;
        [Tooltip("Piezas del brazo que se pintan en naranja durante el aviso.")]
        [SerializeField] private Renderer[] piezasBrazo;

        private CharacterController controlador;
        private ModeloVida modelo;
        private ReglaPandilleroAPie regla;
        private TurnosPandillaAPie turnos;
        private MaterialPropertyBlock bloque;
        private Vector3 hogar;
        private bool tieneHogar;
        private float velocidadVertical;
        private float finParpadeo = -1f;
        private float desaparicion = -1f;
        private Transform raizJugador;
        private IVida vidaJugador;
        private IRecibeDanio recibeJugador;
        private bool pintadoAviso;
        private PistolaPandillero pistola;
        private Animator animador;
        private Renderer[] piezasModelo;
        private int lado = 1;
        private float proximoCambioLado;

        private static readonly int IdVelocidad = Animator.StringToHash("Velocidad");
        private static readonly int IdEnPiso = Animator.StringToHash("EnPiso");
        private static readonly int IdApuntando = Animator.StringToHash("Apuntando");
        private static readonly int IdArma = Animator.StringToHash("Arma");
        private static readonly int IdKO = Animator.StringToHash("KO");
        private static readonly int IdDisparo = Animator.StringToHash("Disparo");
        private const float AlturaBoca = 1.4f;
        private const float AlturaPecho = 1.2f;
        private const float AdelanteBoca = 0.4f;
        private const int ArmaPistola = 1;

        public DatosPandilleroAPie Datos => datos;
        public EstadoPandillero Estado => regla != null ? regla.Estado : EstadoPandillero.Esperando;
        public bool EnPelea => regla != null && !EstaKO && regla.EnPelea;
        public Vector3 Hogar => hogar;
        /// <summary>True cuando la pandilla dice que el jugador está en su territorio (solo pandilleros armados).</summary>
        public bool Alerta { get; set; }
        public float Actual => modelo != null ? modelo.Actual : 0f;
        public float Maxima => modelo != null ? modelo.Maxima : 0f;
        public bool EstaKO => modelo != null && modelo.EstaKO;

        public event Action Cambio;
        public event Action KO;
        /// <summary>Se dispara una vez al caer en KO.</summary>
        public event Action<PandilleroAPie> Vencido;

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
            if (datos == null || (!datos.UsaPistola && (cuerpo == null || brazo == null || piezasBrazo == null || piezasBrazo.Length == 0)))
            {
                Debug.LogError("[IA] Faltan referencias en PandilleroAPie de " + name, this);
                enabled = false;
                return;
            }
            Iniciar();
        }

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
                pistola = GetComponent<PistolaPandillero>();
                if (pistola == null)
                {
                    pistola = gameObject.AddComponent<PistolaPandillero>();
                }
                animador = GetComponentInChildren<Animator>(true);
                piezasModelo = GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                regla = new ReglaPandilleroAPie(datos.RadioDeteccion, datos.Correa, datos.AlcanceGolpe, datos.PreparacionGolpe,
                    datos.RecargaGolpe, datos.MargenLlegada);
            }
            bloque = new MaterialPropertyBlock();
            if (!tieneHogar)
            {
                hogar = transform.position;
            }
            PintarCuerpo(datos.ColorNormal);
            PintarBrazo(datos.ColorNormal);
        }

        private void OnEnable() => Registro<ILecturaRival>.Agregar(this);

        private void OnDisable() => Registro<ILecturaRival>.Quitar(this);

        private void OnDestroy()
        {
            Registro<ILecturaRival>.Quitar(this);
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
            turnos?.Soltar(this);
            desaparicion = -1f;
            finParpadeo = -1f;
            velocidadVertical = 0f;
            if (animador != null)
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
            float aplicado = modelo.Aplicar(info.Cantidad);
            ResultadoDanio resultado = new ResultadoDanio(aplicado, TipoBloqueo.Ninguno, modelo.EstaKO);
            if (!modelo.EstaKO)
            {
                finParpadeo = Time.time + datos.DuracionParpadeo;
                PintarCuerpo(datos.ColorGolpe);
            }
            EventosPelea.PublicarImpacto(null, info, resultado);
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

            BuscarJugador();
            if (datos.UsaPistola)
            {
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
            Mover(salida.Orden, destino, regla.Estado != EstadoPandillero.Preparando && atacable);
            if (salida.Impacto)
            {
                Golpear(posicionJugador);
            }
            ActualizarBrazo();
        }

        // ---- Pandillero armado (T-402) ----

        private void ActualizarArmado()
        {
            bool atacable = Alerta && EsAtacable();
            Vector3 posicionJugador = raizJugador != null ? raizJugador.position : hogar;
            float distancia = DistanciaPlana(transform.position, posicionJugador);
            Vector3 pecho = posicionJugador + Vector3.up * AlturaPecho;
            Vector3 boca = transform.position + Vector3.up * AlturaBoca + transform.forward * AdelanteBoca;
            bool puedeTirar = false;
            if (atacable && distancia <= datos.DistanciaMaxTiro)
            {
                Vector3 hacia = pecho - boca;
                puedeTirar = pistola.HayLinea(boca, hacia.normalized, hacia.magnitude, recibeJugador, out _);
            }

            ReglaPandilleroAPie.Entrada entrada = new ReglaPandilleroAPie.Entrada
            {
                DistanciaJugador = atacable ? distancia : float.MaxValue,
                DistanciaJugadorAlPunto = atacable ? 0f : float.MaxValue,
                DistanciaAlPunto = DistanciaPlana(transform.position, hogar),
                JugadorAtacable = atacable
            };
            entrada.TieneTurno = regla.Atacando || (puedeTirar && (turnos == null || turnos.Pedir(this)));
            ReglaPandilleroAPie.Salida salida = regla.Avanzar(Time.deltaTime, entrada);
            if (!regla.Atacando)
            {
                turnos?.Soltar(this);
            }

            bool apuntando = regla.Estado == EstadoPandillero.Preparando && atacable;
            if (apuntando)
            {
                pistola.Apuntar(boca, pecho);
            }
            else
            {
                pistola.QuitarAviso();
            }
            if (salida.Impacto && atacable)
            {
                float probabilidad = PistolaPandillero.ProbabilidadAcierto(distancia, datos.DistanciaAciertoCerca,
                    datos.DistanciaAciertoLejos, datos.AciertoCerca, datos.AciertoLejos);
                for (int perdigon = 0; perdigon < datos.Perdigones; perdigon++)
                {
                    pistola.Disparar(boca, pecho, recibeJugador, datos.DanioDisparo, probabilidad);
                }
                if (animador != null)
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
            else if (atacable && regla.Estado != EstadoPandillero.Preparando && haciaJugador.sqrMagnitude > 0.0001f)
            {
                Vector3 adelante = haciaJugador.normalized;
                if (distancia > datos.DistanciaMaxTiro || !puedeTirar && distancia > datos.DistanciaMinTiro)
                {
                    deseada = adelante * datos.Velocidad;
                }
                else if (distancia < datos.DistanciaMinTiro)
                {
                    deseada = -adelante * datos.Velocidad;
                }
                else
                {
                    if (Time.time >= proximoCambioLado)
                    {
                        proximoCambioLado = Time.time + datos.PeriodoCambioLado;
                        lado = -lado;
                    }
                    deseada = Vector3.Cross(Vector3.up, adelante) * lado * datos.Velocidad * 0.6f;
                }
            }

            Vector3 mirada = atacable ? haciaJugador : deseada;
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
                animador.SetFloat(IdVelocidad, deseada.magnitude);
                animador.SetBool(IdEnPiso, true);
                animador.SetBool(IdApuntando, apuntando);
                animador.SetInteger(IdArma, ArmaPistola);
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
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && raizJugador != null && jugador.Modo == ModoJugador.APie && recibeJugador != null
                && (vidaJugador == null || !vidaJugador.EstaKO);
        }

        private void Mover(ReglaPandilleroAPie.Orden orden, Vector3 destino, bool mirarAlJugador)
        {
            Vector3 hacia = destino - transform.position;
            hacia.y = 0f;
            Vector3 velocidad = Vector3.zero;
            if (orden != ReglaPandilleroAPie.Orden.Quieto && hacia.sqrMagnitude > 0.0001f)
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

        private void AlLlegarAKO()
        {
            turnos?.Soltar(this);
            controlador.enabled = false;
            if (datos.UsaPistola)
            {
                pistola.QuitarAviso();
                if (animador != null)
                {
                    animador.SetBool(IdKO, true);
                }
            }
            else
            {
                transform.rotation = Quaternion.Euler(-GradosKO, transform.eulerAngles.y, 0f);
            }
            desaparicion = Time.time + datos.SegundosParaDesaparecer;
            PintarCuerpo(datos.ColorKO);
            PonerBrazo(0f);
            PintarBrazo(datos.ColorKO);
            pintadoAviso = false;
            KO?.Invoke();
            Vencido?.Invoke(this);
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

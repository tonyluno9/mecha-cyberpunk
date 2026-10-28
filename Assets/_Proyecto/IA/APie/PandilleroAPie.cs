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

        public DatosPandilleroAPie Datos => datos;
        public EstadoPandillero Estado => regla != null ? regla.Estado : EstadoPandillero.Esperando;
        public bool EnPelea => regla != null && !EstaKO && regla.EnPelea;
        public Vector3 Hogar => hogar;
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
            if (datos == null || cuerpo == null || brazo == null || piezasBrazo == null || piezasBrazo.Length == 0)
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
            regla = new ReglaPandilleroAPie(datos.RadioDeteccion, datos.Correa, datos.AlcanceGolpe, datos.PreparacionGolpe,
                datos.RecargaGolpe, datos.MargenLlegada);
            bloque = new MaterialPropertyBlock();
            if (!tieneHogar)
            {
                hogar = transform.position;
            }
            Pintar(cuerpo, datos.ColorNormal);
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
            controlador.enabled = false;
            transform.SetPositionAndRotation(hogar, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            controlador.enabled = true;
            Pintar(cuerpo, datos.ColorNormal);
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
                Pintar(cuerpo, datos.ColorGolpe);
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
                Pintar(cuerpo, modelo.EstaKO ? datos.ColorKO : datos.ColorNormal);
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
            brazo.localRotation = Quaternion.Euler(-datos.AnguloBrazoAviso * progreso, 0f, 0f);
        }

        private void AlCambiar() => Cambio?.Invoke();

        private void AlLlegarAKO()
        {
            turnos?.Soltar(this);
            controlador.enabled = false;
            transform.rotation = Quaternion.Euler(-GradosKO, transform.eulerAngles.y, 0f);
            desaparicion = Time.time + datos.SegundosParaDesaparecer;
            Pintar(cuerpo, datos.ColorKO);
            PonerBrazo(0f);
            PintarBrazo(datos.ColorKO);
            pintadoAviso = false;
            KO?.Invoke();
            Vencido?.Invoke(this);
        }

        private void PintarBrazo(Color color)
        {
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

using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Vida
{
    /// <summary>
    /// Vida del jugador a pie: envuelve un <see cref="ModeloVida"/> y cumple el contrato de <see cref="IRecibeDanio"/>.
    /// No es <see cref="IPeleador"/>, así que publica el impacto con objetivo null. Al llegar a 0 apaga el control
    /// (<see cref="ControladorAPie.ActivarControl"/>) y baja y ladea la cámara; <see cref="Restaurar"/> lo devuelve todo.
    /// </summary>
    [RequireComponent(typeof(ControladorAPie))]
    public class VidaAPie : MonoBehaviour, IVida, IRecibeDanio, IProtegeDeBalas
    {
        [SerializeField] private DatosPersonaje datos;

        private ControladorAPie controlador;
        private ModeloVidaRegenerable modelo;
        private CubiertaYRodada cubierta;

        // Distancia hacia atrás desde el impacto para reconstruir de dónde vino una bala (m).
        private const float DistanciaOrigenBala = 30f;

        public DatosPersonaje Datos => datos;
        public float Actual => modelo != null ? modelo.Actual : 0f;
        public float Maxima => modelo != null ? modelo.Maxima : 0f;
        public bool EstaKO => modelo != null && modelo.EstaKO;

        public event Action Cambio;
        public event Action KO;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            cubierta = GetComponent<CubiertaYRodada>();
            if (datos == null)
            {
                Debug.LogError("[Personaje] Faltan los datos en VidaAPie de " + name, this);
                enabled = false;
                return;
            }
            modelo = new ModeloVidaRegenerable(datos.VidaMaxima, datos.RegeneracionPorSegundo, datos.SegundosParaRegenerar);
            modelo.Cambio += AlCambiar;
            modelo.KO += AlLlegarAKO;
            // T-684: el escudo vive en el mismo objeto; se crea aquí para no tocar el prefab.
            escudo = GetComponent<EscudoJugador>();
            if (escudo == null)
            {
                escudo = gameObject.AddComponent<EscudoJugador>();
            }
            escudo.Iniciar(datos.EscudoMaximo, datos.EscudoRecargaPorSegundo, datos.EscudoSegundosParaRecargar);
            // T-691: el fuego del jugador, también creado aquí.
            quemadura = GetComponent<QuemaduraJugador>();
            if (quemadura == null)
            {
                quemadura = gameObject.AddComponent<QuemaduraJugador>();
            }
            quemadura.Iniciar(this);
        }

        private readonly SeguidorImplantes implantes = new SeguidorImplantes();

        /// <summary>T-1529: la vida máxima que dan solo los implantes instalados (suma de su vida máxima extra).</summary>
        public float VidaMaximaPorImplantes => Mathf.Max(0f, implantes.Efectos.vidaMaximaExtra);

        /// <summary>T-1529: la vida máxima que dan las prótesis de ataque que lleva en el inventario (+15 cada una, dato de DatosProtesisAtaque).</summary>
        public float VidaMaximaPorProtesis
        {
            get
            {
                MechaCyberpunk.Economia.ServicioInventario inventario = Registro<MechaCyberpunk.Economia.ServicioInventario>.Actual;
                return inventario == null ? 0f : ReglaProtesisVida.Sumar(inventario.Todos);
            }
        }

        private void OnEnable()
        {
            implantes.Cambio += AplicarVidaMaxima;
            implantes.Activar();
            AplicarVidaMaxima();
        }

        private void OnDisable()
        {
            implantes.Cambio -= AplicarVidaMaxima;
            implantes.Desactivar();
        }

        // T-1529: cada implante instalado suma vida máxima (+10 a +25 según el nivel). Se recalcula al instalar, al cargar la partida y al llegar el servicio,
        // así lo guardado (los ids de implantes) basta: no se guarda la vida máxima aparte.
        private void AplicarVidaMaxima()
        {
            if (modelo != null && datos != null)
            {
                modelo.PonerMaxima(datos.VidaMaxima + VidaMaximaPorImplantes + VidaMaximaPorProtesis);
            }
        }

        public QuemaduraJugador Quemadura => quemadura;

        /// <summary>Daño del fuego (T-691): lo absorbe el escudo primero, como cualquier daño; devuelve lo que bajó la vida. Sin sonido de golpe.</summary>
        public float AplicarQuemadura(float cantidad)
        {
            if (modelo == null || modelo.EstaKO || cantidad <= 0f || Time.time < invulnerableHasta)
            {
                return 0f;
            }
            if (escudo != null)
            {
                cantidad = escudo.Absorber(cantidad);
            }
            return cantidad > 0f ? modelo.Aplicar(cantidad) : 0f;
        }

        private EscudoJugador escudo;
        private QuemaduraJugador quemadura;
        private float invulnerableHasta = -1f;

        public EscudoJugador Escudo => escudo;

        /// <summary>Segundos que le quedan sin recibir daño (bebida energética, T-685).</summary>
        public float InvulnerableRestante => Mathf.Max(0f, invulnerableHasta - Time.time);

        /// <summary>No recibe daño durante estos segundos. Si ya tenía más tiempo, se queda con el mayor.</summary>
        public void DarInvulnerabilidad(float segundos)
        {
            if (segundos > 0f)
            {
                invulnerableHasta = Mathf.Max(invulnerableHasta, Time.time + segundos);
            }
        }

        /// <summary>Cura vida sin pasar del máximo y devuelve lo que curó (T-685).</summary>
        public float Curar(float puntos)
        {
            return modelo != null ? modelo.Curar(puntos) : 0f;
        }

        private void OnDestroy()
        {
            if (modelo != null)
            {
                modelo.Cambio -= AlCambiar;
                modelo.KO -= AlLlegarAKO;
            }
        }

        /// <summary>Sube la vida al máximo sin tocar el control (dormir en la cama mejorada, T-448).</summary>
        public void CurarTodo()
        {
            modelo?.Restaurar();
        }

        /// <summary>Vuelve a la vida máxima, devuelve el control y la cámara.</summary>
        public void Restaurar()
        {
            if (modelo == null)
            {
                return;
            }
            modelo.Restaurar();
            escudo?.Restaurar();
            quemadura?.Apagar();
            invulnerableHasta = -1f;
            controlador.ActivarControl(true);
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (modelo == null || modelo.EstaKO || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }
            if (cubierta == null)
            {
                cubierta = GetComponent<CubiertaYRodada>();
            }
            float cantidad = info.Cantidad;
            if (cubierta != null)
            {
                if (cubierta.Invulnerable)
                {
                    return ResultadoDanio.Ignorado;
                }
                // Una bala que llega del otro lado de la cubierta pasa con la fracción de daño de los datos (0 = ninguno).
                if (info.Tipo == TipoDanio.Disparo && cubierta.ProtegeDe(info.Punto - info.Direccion.normalized * DistanciaOrigenBala, out _))
                {
                    Sonidos.ReproducirEn("PUN_BLOQUEO", transform.position);
                    cantidad *= cubierta.Datos.DanioCubierto;
                    if (cantidad <= 0f)
                    {
                        return ResultadoDanio.Ignorado;
                    }
                }
            }
            if (Time.time < invulnerableHasta)
            {
                return ResultadoDanio.Ignorado;
            }
            // T-1307: dentro de un carro la carrocería frena parte de las balas; en la moto no hay nada que proteja.
            if (info.Tipo == TipoDanio.Disparo && EnCarroConCarroceria())
            {
                cantidad *= FraccionDanioDisparoEnCarro;
            }
            // T-684: el escudo absorbe primero; lo que sobra pasa a la vida.
            if (escudo != null)
            {
                cantidad = escudo.Absorber(cantidad);
            }
            return AplicarAVida(info, cantidad);
        }

        /// <summary>
        /// T-1327: daño que va solo a la vida, sin pasar por el escudo (ni lo absorbe ni lo reduce): el de las caídas. Respeta KO y la invulnerabilidad
        /// (bebida energética, rodada) y publica el impacto igual que <see cref="RecibirDanio"/>.
        /// </summary>
        public ResultadoDanio RecibirDanioSoloVida(InfoDanio info)
        {
            if (modelo == null || modelo.EstaKO || info.Cantidad <= 0f || Time.time < invulnerableHasta)
            {
                return ResultadoDanio.Ignorado;
            }
            if (cubierta == null)
            {
                cubierta = GetComponent<CubiertaYRodada>();
            }
            if (cubierta != null && cubierta.Invulnerable)
            {
                return ResultadoDanio.Ignorado;
            }
            return AplicarAVida(info, info.Cantidad);
        }

        private ResultadoDanio AplicarAVida(InfoDanio info, float cantidad)
        {
            float aplicado = modelo.Aplicar(cantidad);
            bool prendio = false;
            if (info.Tipo == TipoDanio.Explosion && quemadura != null && !modelo.EstaKO)
            {
                bool yaArdia = quemadura.Ardiendo;
                quemadura.Prender(info.Punto); // T-691: una explosión te prende fuego
                prendio = !yaArdia && quemadura.Ardiendo;
            }
            ResultadoDanio resultado = new ResultadoDanio(aplicado, TipoBloqueo.Ninguno, modelo.EstaKO);
            if (info.Tipo == TipoDanio.Explosion && aplicado > 0f)
            {
                Sonidos.Reproducir(ReglaSonidoVida.ZumbidoOido); // T-1535: el pitido en el oído tras una explosión que te alcanza
            }
            EventosPelea.PublicarImpacto(null, info, resultado);
            // T-voces-23nov: la voz grabada del jugador (2D): quemado si la explosión lo prendió; si no, dolor leve o fuerte según el daño. Al caer suena la muerte.
            if (!modelo.EstaKO)
            {
                string voz = prendio ? VozJugador.Quemado : VozJugador.IdDolor(aplicado, modelo.Maxima, info.Tipo == TipoDanio.Explosion);
                if (voz.Length > 0)
                {
                    Sonidos.Reproducir(VozJugador.Id(voz));
                }
            }
            return resultado;
        }

        /// <summary>T-1307: fracción del daño de una bala que le llega al jugador sentado dentro de un carro (la moto recibe el daño completo).</summary>
        public const float FraccionDanioDisparoEnCarro = 0.55f;

        private JugadorAPie jugadorAPie;
        private AnimacionCarro animacionCarro;

        private bool EnCarroConCarroceria()
        {
            if (jugadorAPie == null)
            {
                jugadorAPie = GetComponent<JugadorAPie>();
                animacionCarro = GetComponent<AnimacionCarro>();
            }
            return jugadorAPie != null && jugadorAPie.Modo == ModoJugador.Carro && (animacionCarro == null || !animacionCarro.EnMoto);
        }

        public bool ProtegeDe(Vector3 origen, out Vector3 puntoCubierta)
        {
            puntoCubierta = Vector3.zero;
            return cubierta != null && cubierta.ProtegeDe(origen, out puntoCubierta);
        }

        private void Update()
        {
            AplicarVidaMaxima(); // T-1529: las prótesis de ataque se tienen al llevarlas en el inventario (compra, carga): se revisa cada cuadro
            modelo?.Avanzar(Time.deltaTime);
            if (modelo != null && ReglaSonidoVida.TocaLatido(modelo.Actual, modelo.Maxima, FraccionVidaBaja, modelo.EstaKO, Time.time, ultimoLatido))
            {
                ultimoLatido = Time.time;
                Sonidos.Reproducir(ReglaSonidoVida.Latido); // T-1535: el corazón con la vida baja
            }
        }

        private void LateUpdate()
        {
            // En KO el controlador ya no mueve la cámara: aquí cae al piso y se ladea.
            // V12-B: en VR no: la cámara es la cabeza del visor y nunca la mueve una animación (el KO es el fundido a negro de AdaptadorVR).
            if (!EstaKO || controlador.Camara == null || ModoVR.Activo)
            {
                return;
            }
            float paso = 1f - Mathf.Exp(-datos.VelocidadCaidaKO * Time.deltaTime);
            Transform camara = controlador.Camara;
            Vector3 posicion = camara.localPosition;
            posicion.y = Mathf.Lerp(posicion.y, datos.AlturaCamaraKO, paso);
            camara.localPosition = posicion;
            camara.localRotation = Quaternion.Slerp(camara.localRotation, Quaternion.Euler(0f, 0f, datos.LadeoCamaraKO), paso);
        }

        // T-442: avisos de sonido de la vida. Vida baja una vez al cruzar el umbral hacia abajo; regenera una vez cuando empieza a curar.
        public const float FraccionVidaBaja = 0.4f; // T-1492: antes 0.25
        private float vidaAnterior = -1f;
        private bool avisoVidaBaja;
        private float ultimoLatido = float.NegativeInfinity;
        private bool avisoRegenera;

        private void AlCambiar()
        {
            SonidosDeVida();
            Cambio?.Invoke();
        }

        private void SonidosDeVida()
        {
            float actual = modelo.Actual;
            if (vidaAnterior >= 0f && !modelo.EstaKO)
            {
                if (actual < vidaAnterior)
                {
                    avisoRegenera = false;
                    if (!avisoVidaBaja && actual < modelo.Maxima * FraccionVidaBaja)
                    {
                        avisoVidaBaja = true;
                        Sonidos.Reproducir("HUD_VIDA_BAJA"); // T-1474: aviso del HUD en 2D, una vez al cruzar el umbral
                    }
                }
                else if (actual > vidaAnterior && !avisoRegenera)
                {
                    avisoRegenera = true;
                    Sonidos.ReproducirEn("JUG_REGENERA", transform.position);
                }
            }
            if (actual >= modelo.Maxima * FraccionVidaBaja)
            {
                avisoVidaBaja = false;
            }
            vidaAnterior = actual;
        }

        private void AlLlegarAKO()
        {
            controlador.ActivarControl(false);
            Sonidos.Reproducir(VozJugador.Id(VozJugador.Muerte)); // T-voces-23nov: antes JUG_CAE_DERROTADO (librería)
            Debug.Log("[Personaje] KO a pie");
            KO?.Invoke();
        }
    }
}

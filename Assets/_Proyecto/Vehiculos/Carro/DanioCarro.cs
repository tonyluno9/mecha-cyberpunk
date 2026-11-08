using System;
using System.Collections.Generic;
using MechaCyberpunk.Vehiculos.Efectos;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Datos;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// El daño de un carro (T-444): recibe balas y golpes (<see cref="IRecibeDanio"/>, sin sangre: <see cref="IObjetoInerte"/>), se daña
    /// al chocar según la rapidez relativa, saca humo con poca vida, se prende en llamas con menos y explota al quedar en 0: empuja y
    /// daña lo que tenga cerca (también a quien lo maneja, que sale de golpe) y queda como un armazón negro que sigue ardiendo un rato.
    /// Lo agrega <see cref="CarroViejo"/> al nacer; los números salen de <see cref="ConfigDanioCarro"/> (sin asset usa los de por defecto).
    /// </summary>
    public class DanioCarro : MonoBehaviour, IRecibeDanio, IObjetoInerte
    {
        private const float AlturaMotor = 0.9f;
        private const float AdelanteMotor = 1.4f;
        private const float FraccionAlturaMotorMoto = 0.4f;
        private const float SegundosDestelloExplosion = 0.25f;
        private const float ImpulsoVerticalExplosion = 4f;
        // T-692: los choques que cuentan. Una rapidez imposible es un glitch de física (carro lanzado, penetración), no un choque real.
        private const float RapidezChoqueImposible = 45f;
        private const float FraccionMaximaPorChoque = 0.3f;
        private const float SegundosGraciaInicial = 1.5f;
        private const float SegundosGraciaEmpuje = 1.2f;
        private const float UpwardsExplosion = 0.3f;
        // T-696: el aviso de fuego es chisporroteo (antes la alarma de robo, que ya no suena).
        public const string IdAvisoFuego = "MUN_CHISPAS_ELECTRICAS";
        public const string MensajeExplosion = "AlExplotarVehiculo";
        private static readonly Color ColorArmazon = new Color(0.06f, 0.06f, 0.07f, 1f);

        /// <summary>Aviso al conductor cuando el carro se prende (T-532).</summary>
        public const string TextoIncendio = "¡Bájate, se va a quemar!";

        [SerializeField] private ConfigDanioCarro config;

        private CarroViejo carro;
        private Rigidbody cuerpo;
        private CalculoDanioCarro calculo;
        private ParticleSystem humo;
        private ParticleSystem fuego;
        private Light luz;
        private float proximoChoque;
        private float ignorarChoquesHasta;
        private float finArdiendo;
        private float finDestello;
        private bool ardiendo;

        public EstadoCarro Estado => calculo != null ? calculo.Estado : EstadoCarro.Sano;
        public float Vida => calculo != null ? calculo.Actual : 0f;
        public float VidaMaxima => calculo != null ? calculo.Maxima : 0f;
        public bool Explotado { get; private set; }
        /// <summary>Los efectos del último estallido (T-591), o null si no ha explotado.</summary>
        public ExplosionCarro UltimaExplosion { get; private set; }
        public ConfigDanioCarro Config => config;
        public ParticleSystem Humo => humo;
        public ParticleSystem Fuego => fuego;

        /// <summary>Cambió el estado (Sano, Humo, Fuego, Destruido).</summary>
        public event Action<EstadoCarro> EstadoCambio;

        private void Awake()
        {
            carro = GetComponent<CarroViejo>();
            cuerpo = GetComponent<Rigidbody>();
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigDanioCarro>();
                config.name = "ConfigDanioCarro_PorDefecto";
            }
            calculo = new CalculoDanioCarro(config.VidaMaxima, config.FraccionHumo, config.FraccionFuego);
            CrearEfectos();
            ignorarChoquesHasta = Time.time + SegundosGraciaInicial;
        }

        /// <summary>No cuenta los choques durante unos segundos (el carro acaba de ser empujado por una explosión: ya recibió su daño).</summary>
        public void IgnorarChoques(float segundos)
        {
            ignorarChoquesHasta = Mathf.Max(ignorarChoquesHasta, Time.time + segundos);
        }

        /// <summary>Daño de un choque con esa rapidez relativa; 0 si no cuenta (T-692). Tope por choque y nada con rapideces imposibles.</summary>
        public static float DanioDeChoque(float rapidez, float umbral, float danioPorMetro, float vidaMaxima, float danioMaximo = float.PositiveInfinity)
        {
            if (rapidez > RapidezChoqueImposible)
            {
                return 0f;
            }
            return Mathf.Min(CalculoDanioCarro.DanioPorChoque(rapidez, umbral, danioPorMetro), vidaMaxima * FraccionMaximaPorChoque, danioMaximo);
        }

        /// <summary>
        /// Cambia cuántas veces su vida normal aguanta el carro (el garaje mejorado, T-448), sin perder la fracción de vida que lleva.
        /// </summary>
        public void EscalarVida(float factor)
        {
            if (calculo == null || config == null)
            {
                return;
            }
            float fraccion = calculo.Maxima > 0f ? calculo.Actual / calculo.Maxima : 1f;
            calculo = new CalculoDanioCarro(config.VidaMaxima * Mathf.Max(0.1f, factor), config.FraccionHumo, config.FraccionFuego);
            calculo.Aplicar(calculo.Maxima * (1f - fraccion));
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (calculo == null || calculo.Destruido || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }
            EstadoCarro antes = calculo.Estado;
            float aplicado = calculo.Aplicar(info.Cantidad);
            Revisar(antes);
            return new ResultadoDanio(aplicado, TipoBloqueo.Ninguno, calculo.Destruido);
        }

        private void OnCollisionEnter(Collision colision)
        {
            if (calculo == null || calculo.Destruido || Time.time < proximoChoque || Time.time < ignorarChoquesHasta)
            {
                return;
            }
            float rapidez = colision.relativeVelocity.magnitude;
            LanzarAlConductorDeLaMoto(rapidez);
            if (rapidez <= config.UmbralChoque)
            {
                return;
            }
            proximoChoque = Time.time + config.SegundosEntreChoques;
            Sonidos.ReproducirEn(rapidez >= config.UmbralChoqueFuerte ? "VEH_CHOQUE_FUERTE" : "VEH_CHOQUE_LEVE", transform.position);
            float danio = DanioDeChoque(rapidez, config.UmbralChoque, config.DanioPorChoque, calculo.Maxima, config.DanioMaximoPorChoque);
            if (danio <= 0f)
            {
                return;
            }
            EstadoCarro antes = calculo.Estado;
            calculo.Aplicar(danio);
            Revisar(antes);
        }

        // T-696: un choque fuerte en moto avienta al conductor: sale de golpe (el jugador lo recoge con SalidaForzada y cae).
        private void LanzarAlConductorDeLaMoto(float rapidez)
        {
            if (carro == null || !carro.Ocupado || GetComponent<ManejoMoto>() == null || rapidez < config.RapidezLanzaConductor || rapidez > RapidezChoqueImposible)
            {
                return;
            }
            carro.PermitirBajarYa();
            EventosMontaje.PublicarSalidaForzada(carro);
        }

        private void Update()
        {
            if (calculo == null)
            {
                return;
            }
            if (!Explotado && calculo.Estado == EstadoCarro.Fuego)
            {
                EstadoCarro antes = calculo.Estado;
                calculo.Aplicar(config.DanioFuegoPorSegundo * Time.deltaTime);
                Revisar(antes);
            }
            if (luz != null && luz.enabled)
            {
                bool destello = Time.time < finDestello;
                luz.intensity = destello ? 8f : 1.5f + Mathf.PerlinNoise(Time.time * 9f, 0f) * 1.5f;
            }
            if (ardiendo && Explotado && Time.time >= finArdiendo)
            {
                ardiendo = false;
                Apagar(fuego);
                Apagar(humo);
                if (luz != null)
                {
                    luz.enabled = false;
                }
            }
        }

        // Avisa del cambio de estado, ajusta humo y fuego y explota al llegar a Destruido.
        private void Revisar(EstadoCarro antes)
        {
            EstadoCarro ahora = calculo.Estado;
            if (ahora == antes)
            {
                return;
            }
            ActualizarEfectos(ahora);
            EstadoCambio?.Invoke(ahora);
            if (ahora == EstadoCarro.Fuego)
            {
                // T-692: aviso claro antes de explotar: ya salen humo y fuego; además chisporrotea (T-696).
                Sonidos.ReproducirEn(IdAvisoFuego, transform.position);
                // T-532: con alguien adentro, aviso para que se baje antes de que explote.
                IMontable montable = GetComponent<IMontable>();
                if (montable != null && montable.Ocupado)
                {
                    EventosMontaje.PublicarAviso(montable, TextoIncendio);
                }
            }
            if (ahora == EstadoCarro.Destruido && !Explotado)
            {
                Explotar();
            }
        }

        private void ActualizarEfectos(EstadoCarro estado)
        {
            Emitir(humo, estado == EstadoCarro.Humo ? 8f : estado == EstadoCarro.Fuego || estado == EstadoCarro.Destruido ? 20f : 0f);
            Emitir(fuego, estado == EstadoCarro.Fuego || estado == EstadoCarro.Destruido ? 25f : 0f);
            if (luz != null)
            {
                luz.enabled = estado == EstadoCarro.Fuego || estado == EstadoCarro.Destruido;
            }
        }

        private static void Emitir(ParticleSystem sistema, float porSegundo)
        {
            if (sistema == null)
            {
                return;
            }
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.rateOverTime = porSegundo;
        }

        private static void Apagar(ParticleSystem sistema)
        {
            if (sistema != null)
            {
                Emitir(sistema, 0f);
            }
        }

        private void Explotar()
        {
            Explotado = true;
            ardiendo = true;
            finArdiendo = Time.time + config.SegundosArdiendo;
            finDestello = Time.time + SegundosDestelloExplosion;
            Vector3 centro = transform.position + Vector3.up * AlturaMotor;

            // Quien lo maneja sale de golpe antes del estallido, para que el daño lo alcance a pie.
            if (carro != null && carro.Ocupado)
            {
                carro.PermitirBajarYa();
                EventosMontaje.PublicarSalidaForzada(carro);
            }
            if (carro != null)
            {
                carro.TomarControl(false);
            }
            gameObject.SendMessage(MensajeExplosion, SendMessageOptions.DontRequireReceiver); // T-692: las patrullas dejan de manejar

            Sonidos.ReproducirEn("VEH_CHOQUE_FUERTE", centro);
            UltimaExplosion = ExplosionCarro.Crear(centro, config.RadioExplosion);
            DanarAlrededor(centro);
            SumarCalorSiEstaCerca(centro);
            if (cuerpo != null)
            {
                cuerpo.AddForce(Vector3.up * ImpulsoVerticalExplosion * 0.5f, ForceMode.VelocityChange);
            }
            Ennegrecer();
            if (carro != null)
            {
                carro.enabled = false;
            }
            Debug.Log("[Vehiculos] Explotó " + name);
        }

        private void DanarAlrededor(Vector3 centro)
        {
            float radio = config.RadioExplosion;
            Collider[] cercanos = Physics.OverlapSphere(centro, radio, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            HashSet<IRecibeDanio> vistos = new HashSet<IRecibeDanio>();
            HashSet<Rigidbody> empujados = new HashSet<Rigidbody>();
            foreach (Collider c in cercanos)
            {
                if (c == null || c.transform.IsChildOf(transform))
                {
                    continue;
                }
                // T-692: un trigger solo cuenta si es la zona de daño de algo que recibe daño (patrullas); los demás triggers se ignoran.
                if (c.isTrigger && c.GetComponentInParent<IRecibeDanio>() == null)
                {
                    continue;
                }
                float distancia = Vector3.Distance(centro, c.ClosestPoint(centro));
                float factor = Mathf.Clamp01(1f - distancia / radio);
                IRecibeDanio victima = c.GetComponentInParent<IRecibeDanio>();
                if (victima != null && !ReferenceEquals(victima, this) && vistos.Add(victima))
                {
                    c.GetComponentInParent<DanioCarro>()?.IgnorarChoques(SegundosGraciaEmpuje);
                    Vector3 direccion = (c.bounds.center - centro).normalized;
                    victima.RecibirDanio(new InfoDanio(config.DanioExplosion * factor, TipoDanio.Explosion, c.ClosestPoint(centro), direccion, null));
                }
                Rigidbody rb = c.attachedRigidbody;
                if (!c.isTrigger && rb != null && rb != cuerpo && empujados.Add(rb))
                {
                    // T-692: la fuerza es un cambio de velocidad (m/s) con caída lineal. Antes era un impulso de 1400 x masa y mandaba todo al cielo.
                    rb.AddExplosionForce(config.FuerzaExplosion, centro, radio, UpwardsExplosion, ForceMode.VelocityChange);
                    rb.GetComponent<DanioCarro>()?.IgnorarChoques(SegundosGraciaEmpuje);
                }
            }
        }

        private void SumarCalorSiEstaCerca(Vector3 centro)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            ICalor calor = Registro<ICalor>.Actual;
            if (jugador == null || jugador.Raiz == null || calor == null || config.CalorExplosion <= 0f)
            {
                return;
            }
            if (Vector3.Distance(jugador.Raiz.position, centro) <= config.DistanciaCalor)
            {
                calor.Sumar(config.CalorExplosion, MotivoCalor.Choque);
            }
        }

        // Pinta la carrocería de armazón quemado.
        private void Ennegrecer()
        {
            foreach (Renderer render in GetComponentsInChildren<Renderer>(true))
            {
                if (render is ParticleSystemRenderer || render.name != "Carroceria")
                {
                    continue;
                }
                MaterialPropertyBlock bloque = new MaterialPropertyBlock();
                render.GetPropertyBlock(bloque);
                bloque.SetColor("_BaseColor", ColorArmazon);
                bloque.SetColor("_Color", ColorArmazon);
                render.SetPropertyBlock(bloque);
            }
        }

        /// <summary>
        /// Dónde está el motor, en el espacio del vehículo (T-695): el carro lo lleva al frente (bajo el cofre); la moto, en el centro de la moto
        /// (a media altura del modelo, a media longitud), no en el aire delante de la rueda.
        /// </summary>
        public Vector3 PuntoMotor()
        {
            if (GetComponent<ManejoMoto>() == null)
            {
                return new Vector3(0f, AlturaMotor, AdelanteMotor);
            }
            bool hay = false;
            Bounds caja = default;
            foreach (Renderer render in GetComponentsInChildren<Renderer>(true))
            {
                if (render is ParticleSystemRenderer)
                {
                    continue;
                }
                Bounds b = render.bounds;
                caja = hay ? CajaUnida(caja, b) : b;
                hay = true;
            }
            if (!hay)
            {
                return new Vector3(0f, FraccionAlturaMotorMoto, 0f);
            }
            Vector3 centro = transform.InverseTransformPoint(caja.center);
            float bajo = transform.InverseTransformPoint(new Vector3(caja.center.x, caja.min.y, caja.center.z)).y;
            return new Vector3(0f, bajo + caja.size.y * FraccionAlturaMotorMoto, centro.z);
        }

        private static Bounds CajaUnida(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }

        private void CrearEfectos()
        {
            Vector3 puntoMotor = PuntoMotor();
            // T-745: el humo y el fuego del carro averiado son prefabs del Unity Particle Pack (EfectosExplosion); el estado maneja su emisión.
            humo = EfectosExplosion.Humo(transform, puntoMotor, 0.8f);
            fuego = EfectosExplosion.Fuego(transform, puntoMotor, 0.8f);
            GameObject objetoLuz = new GameObject("LuzFuegoCarro");
            objetoLuz.transform.SetParent(transform, false);
            objetoLuz.transform.localPosition = puntoMotor;
            luz = objetoLuz.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.55f, 0.15f);
            luz.range = 8f;
            luz.intensity = 2f;
            luz.shadows = LightShadows.None;
            luz.enabled = false;
        }

    }
}

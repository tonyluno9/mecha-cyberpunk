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
    public class DanioCarro : MonoBehaviour, IRecibeDanio, IObjetoInerte, IDetonable
    {
        private float detonarEn = -1f;

        /// <summary>T-1325: ya recibió una orden de detonar y va a explotar en unos segundos (la cadena de carros).</summary>
        public bool Detonando => detonarEn >= 0f && !Explotado;

        /// <summary>
        /// T-1325: lo destroza una explosión cercana (granada, carro vecino): explota en <paramref name="retraso"/> segundos (0 o carro apagado: al instante).
        /// </summary>
        public void Detonar(float retraso)
        {
            if (calculo == null || Explotado)
            {
                return;
            }
            // Una detonación inmediata (granada encima) gana sobre una cadena que ya venía en camino.
            if (retraso <= 0f || !isActiveAndEnabled)
            {
                ExplotarYa();
                return;
            }
            if (Detonando)
            {
                return;
            }
            detonarEn = Time.time + retraso;
        }

        private void ExplotarYa()
        {
            detonarEn = -1f;
            if (calculo == null || Explotado)
            {
                return;
            }
            EstadoCarro antes = calculo.Estado;
            calculo.Aplicar(calculo.Actual);
            Revisar(antes);
        }

        private const float AlturaMotor = 0.9f;
        private const string NombreModeloLlanta = "Modelo";
        private const string NombreGiroLlanta = "Giro";
        private const float MasaPieza = 15f;
        private const float VelocidadPieza = 9f;
        private const float SegundosPiezas = 12f;
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
        private const float AmortiguarChatarra = 4f;
        // T-1201: el fuego se apaga en el primer 70% del tiempo ardiendo; el humo sigue, cada vez más tenue, hasta el final.
        private const float FraccionFuegoArdiendo = 0.7f;
        private const float HumoChatarra = 40f;
        private const float EscalaLlamasChatarra = 0.8f;
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
        private int bucleFuego = Sonidos.SinBucle;
        private float inicioArdiendo;
        private float quietoDesde = -1f;
        private ParticleSystem[] llamasChatarra;
        private float[] tasasLlamas;

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
            if (carro != null && rapidez <= RapidezChoqueImposible)
            {
                carro.TemblarPorChoque(rapidez); // T-1488
            }
            LanzarAlConductorDeLaMoto(rapidez);
            if (rapidez <= config.UmbralChoque)
            {
                return;
            }
            proximoChoque = Time.time + config.SegundosEntreChoques;
            // T-1475: choque leve, fuerte y, si es muy fuerte, también cristal roto.
            foreach (string idChoque in ReglasSonidoVehiculoT1475.IdsChoque(rapidez, config.UmbralChoque, config.UmbralChoqueFuerte))
            {
                Sonidos.ReproducirEn(idChoque, transform.position);
            }
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
            if (Detonando && Time.time >= detonarEn)
            {
                ExplotarYa();
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
            if (Explotado)
            {
                AsentarChatarra();
            }
            if (ardiendo && Explotado && Time.time < finArdiendo)
            {
                // T-1201: el fuego se apaga primero y el humo después, poco a poco, en vez de cortarse de golpe.
                float t = Mathf.InverseLerp(inicioArdiendo, finArdiendo, Time.time);
                float vivo = Mathf.Clamp01(1f - t / FraccionFuegoArdiendo);
                Emitir(fuego, 25f * vivo);
                Emitir(humo, Mathf.Lerp(HumoChatarra, 3f, t));
                if (llamasChatarra != null)
                {
                    for (int i = 0; i < llamasChatarra.Length; i++)
                    {
                        if (llamasChatarra[i] != null)
                        {
                            ParticleSystem.EmissionModule emision = llamasChatarra[i].emission;
                            emision.rateOverTimeMultiplier = tasasLlamas[i] * vivo;
                        }
                    }
                }
                if (luz != null && Time.time >= finDestello)
                {
                    luz.intensity *= Mathf.Clamp01(1f - t / FraccionFuegoArdiendo);
                }
            }
            if (ardiendo && Explotado && Time.time >= finArdiendo)
            {
                ardiendo = false;
                DetenerSonidoFuego();
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
                IniciarSonidoFuego();
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
            // T-1201: el tráfico apaga y vuelve a prender sus carros, y al apagarse el sistema se detiene (no tiene playOnAwake): se reanuda aquí.
            if (porSegundo > 0f && !sistema.isPlaying)
            {
                sistema.Play(true);
            }
        }

        // T-1472: el fuego chico suena en bucle siguiendo al carro mientras arde; se corta al apagarse, desactivarse o destruirse.
        private void IniciarSonidoFuego()
        {
            if (bucleFuego == Sonidos.SinBucle)
            {
                bucleFuego = Sonidos.ReproducirBucle(LimiteSonidoExplosion.IdFuegoChico, transform);
            }
        }

        // T-1532: al explotar, el fuego chico del aviso cede su lugar al fwoosh y al fuego grande en bucle.
        private void IniciarFuegoGrande(Vector3 centro)
        {
            DetenerSonidoFuego();
            Sonidos.ReproducirEn(LimiteSonidoExplosion.IdFuegoEncender, centro);
            bucleFuego = Sonidos.ReproducirBucle(LimiteSonidoExplosion.IdFuegoGrande, transform);
        }

        private void DetenerSonidoFuego()
        {
            if (bucleFuego != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucleFuego);
                bucleFuego = Sonidos.SinBucle;
            }
        }

        /// <summary>true mientras suena el bucle del fuego (para pruebas).</summary>
        public bool FuegoSonando => bucleFuego != Sonidos.SinBucle;

        private void OnDisable() => DetenerSonidoFuego();

        private void OnDestroy() => DetenerSonidoFuego();

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
            inicioArdiendo = Time.time;
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
            IniciarFuegoGrande(centro); // T-1532: fwoosh y fuego grande en bucle (antes ardía el chico)
            UltimaExplosion = ExplosionCarro.Crear(centro, config.RadioExplosion);
            DanarAlrededor(centro);
            SumarCalorSiEstaCerca(centro);
            if (cuerpo != null && !cuerpo.isKinematic)
            {
                // T-1201: el estallido lo frena en seco (no sigue rodando ni se desliza) y solo lo levanta un poco; luego queda como chatarra.
                cuerpo.linearVelocity = new Vector3(0f, Mathf.Max(0f, cuerpo.linearVelocity.y), 0f);
                cuerpo.angularVelocity = Vector3.zero;
                cuerpo.linearDamping = AmortiguarChatarra;
                cuerpo.angularDamping = AmortiguarChatarra;
                cuerpo.AddForce(Vector3.up * ImpulsoVerticalExplosion * 0.5f, ForceMode.VelocityChange);
            }
            Ennegrecer();
            LanzarPiezas();
            PrenderLlamasChatarra();
            if (carro != null)
            {
                carro.enabled = false;
            }
            Debug.Log("[Vehiculos] Explotó " + name);
        }

        /// <summary>T-1201: True cuando la chatarra ya cayó y quedó fija (cuerpo cinemático: nada la arrastra ni la empuja).</summary>
        public bool ChatarraFija => Explotado && cuerpo != null && cuerpo.isKinematic;

        // T-1201: tras caer del estallido, en cuanto casi no se mueve medio segundo, el carro queda fijo en su lugar.
        private void AsentarChatarra()
        {
            if (cuerpo == null || cuerpo.isKinematic)
            {
                return;
            }
            Vector3 v = cuerpo.linearVelocity;
            v.x *= 0.5f;
            v.z *= 0.5f;
            cuerpo.linearVelocity = v;
            bool quieto = cuerpo.linearVelocity.sqrMagnitude < 0.25f && cuerpo.angularVelocity.sqrMagnitude < 0.25f;
            if (!quieto || Time.time < finDestello + 0.3f)
            {
                quietoDesde = -1f;
                return;
            }
            if (quietoDesde < 0f)
            {
                quietoDesde = Time.time;
            }
            else if (Time.time - quietoDesde >= 0.5f)
            {
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
                cuerpo.isKinematic = true;
            }
        }

        // T-1201: la chatarra arde con las llamas medianas del pack (las del barril) sobre el motor; se apagan poco a poco junto con el fuego chico.
        private void PrenderLlamasChatarra()
        {
            GameObject llamas = EfectosExplosion.FuegoSuelo(transform.TransformPoint(new Vector3(0f, AlturaMotor * 0.8f, AdelanteMotor * 0.6f)), config.SegundosArdiendo);
            if (llamas == null)
            {
                return;
            }
            llamas.name = "LlamasChatarra";
            llamas.transform.SetParent(transform, true);
            llamas.transform.localScale = Vector3.one * EscalaLlamasChatarra;
            llamasChatarra = llamas.GetComponentsInChildren<ParticleSystem>(true);
            tasasLlamas = new float[llamasChatarra.Length];
            for (int i = 0; i < llamasChatarra.Length; i++)
            {
                tasasLlamas[i] = llamasChatarra[i].emission.rateOverTimeMultiplier;
            }
        }

        // T-1325: el carro revienta en pedazos con las piezas que ya tiene su modelo: las llantas (Giro/Modelo) y, si las tuviera, puertas y cofre.
        // Cada pieza se suelta del carro, se vuelve cuerpo físico con su propia malla como collider y sale volando; a los segundos se borra.
        private void LanzarPiezas()
        {
            List<Transform> piezas = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                bool llanta = t.name == NombreModeloLlanta && t.parent != null && t.parent.name == NombreGiroLlanta;
                bool carroceria = t.name.StartsWith("Puerta") || t.name.StartsWith("Cofre") || t.name.StartsWith("Capo");
                if ((llanta || carroceria) && t.GetComponentInChildren<MeshRenderer>(true) != null)
                {
                    piezas.Add(t);
                }
            }
            Collider[] propios = GetComponentsInChildren<Collider>(true);
            Vector3 centro = transform.position + Vector3.up * AlturaMotor;
            foreach (Transform pieza in piezas)
            {
                pieza.SetParent(null, true);
                MeshFilter malla = pieza.GetComponentInChildren<MeshFilter>(true);
                Bounds caja = malla != null && malla.sharedMesh != null ? malla.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * 0.5f);
                BoxCollider colision = pieza.gameObject.AddComponent<BoxCollider>();
                colision.center = malla != null ? pieza.InverseTransformPoint(malla.transform.TransformPoint(caja.center)) : Vector3.zero;
                colision.size = Vector3.Scale(caja.size, malla != null ? malla.transform.lossyScale : Vector3.one) / Mathf.Max(0.0001f, pieza.lossyScale.x);
                foreach (Collider propio in propios)
                {
                    Physics.IgnoreCollision(colision, propio);
                }
                Rigidbody rb = pieza.gameObject.AddComponent<Rigidbody>();
                rb.mass = MasaPieza;
                Vector3 fuera = pieza.position - centro;
                Vector3 impulso = (fuera.normalized + Vector3.up).normalized * VelocidadPieza + UnityEngine.Random.insideUnitSphere * 1.5f;
                rb.linearVelocity = impulso;
                rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f;
                Destroy(pieza.gameObject, SegundosPiezas);
            }
            PiezasLanzadas = piezas.Count;
        }

        /// <summary>T-1325: cuántas piezas (llantas, puertas, cofre) salieron volando en el estallido.</summary>
        public int PiezasLanzadas { get; private set; }

        /// <summary>T-1201: las llamas medianas que arden sobre la chatarra (null si no ha explotado o no hay catálogo).</summary>
        public ParticleSystem[] LlamasChatarra => llamasChatarra;

        private void DanarAlrededor(Vector3 centro)
        {
            float radio = config.RadioExplosion;
            Collider[] cercanos = Physics.OverlapSphere(centro, radio, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            HashSet<IRecibeDanio> vistos = new HashSet<IRecibeDanio>();
            HashSet<Rigidbody> empujados = new HashSet<Rigidbody>();
            HashSet<IDetonable> detonados = new HashSet<IDetonable>();
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
                // T-1325: los carros dentro del radio se destrozan en cadena, uno tras otro con un retraso corto.
                IDetonable vecino = c.GetComponentInParent<IDetonable>();
                if (vecino != null && !ReferenceEquals(vecino, this) && detonados.Add(vecino))
                {
                    vecino.Detonar(BarrilExplosivo.RetrasoCadena);
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
            // T-1201: la chatarra queda quemada entera (antes solo la malla "Carroceria", que los carros de tráfico no tienen).
            foreach (Renderer render in GetComponentsInChildren<Renderer>(true))
            {
                if (render is ParticleSystemRenderer || render is LineRenderer || render is TrailRenderer)
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

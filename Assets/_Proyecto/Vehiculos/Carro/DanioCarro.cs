using System;
using System.Collections.Generic;
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
    [RequireComponent(typeof(CarroViejo))]
    public class DanioCarro : MonoBehaviour, IRecibeDanio, IObjetoInerte
    {
        private const int LadoTextura = 32;
        private const float AlturaMotor = 0.9f;
        private const float AdelanteMotor = 1.4f;
        private const int ParticulasExplosion = 60;
        private const int ParticulasHumoExplosion = 30;
        private const float SegundosDestelloExplosion = 0.25f;
        private const float ImpulsoVerticalExplosion = 4f;
        private static readonly Color ColorArmazon = new Color(0.06f, 0.06f, 0.07f, 1f);

        [SerializeField] private ConfigDanioCarro config;

        private CarroViejo carro;
        private Rigidbody cuerpo;
        private CalculoDanioCarro calculo;
        private ParticleSystem humo;
        private ParticleSystem fuego;
        private Light luz;
        private Material material;
        private Texture2D textura;
        private float proximoChoque;
        private float finArdiendo;
        private float finDestello;
        private bool ardiendo;

        public EstadoCarro Estado => calculo != null ? calculo.Estado : EstadoCarro.Sano;
        public float Vida => calculo != null ? calculo.Actual : 0f;
        public float VidaMaxima => calculo != null ? calculo.Maxima : 0f;
        public bool Explotado { get; private set; }
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
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
            if (textura != null)
            {
                Destroy(textura);
            }
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
            if (calculo == null || calculo.Destruido || Time.time < proximoChoque)
            {
                return;
            }
            float rapidez = colision.relativeVelocity.magnitude;
            if (rapidez <= config.UmbralChoque)
            {
                return;
            }
            proximoChoque = Time.time + config.SegundosEntreChoques;
            Sonidos.ReproducirEn(rapidez >= config.UmbralChoqueFuerte ? "VEH_CHOQUE_FUERTE" : "VEH_CHOQUE_LEVE", transform.position);
            float danio = CalculoDanioCarro.DanioPorChoque(rapidez, config.UmbralChoque, config.DanioPorChoque);
            EstadoCarro antes = calculo.Estado;
            calculo.Aplicar(danio);
            Revisar(antes);
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

            Sonidos.ReproducirEn("VEH_CHOQUE_FUERTE", centro);
            EmitirRafaga(fuego, ParticulasExplosion, 7f);
            EmitirRafaga(humo, ParticulasHumoExplosion, 4f);
            DanarAlrededor(centro);
            SumarCalorSiEstaCerca(centro);
            if (cuerpo != null)
            {
                cuerpo.AddForce(Vector3.up * cuerpo.mass * ImpulsoVerticalExplosion, ForceMode.Impulse);
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
            Collider[] cercanos = Physics.OverlapSphere(centro, radio, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            HashSet<IRecibeDanio> vistos = new HashSet<IRecibeDanio>();
            HashSet<Rigidbody> empujados = new HashSet<Rigidbody>();
            foreach (Collider c in cercanos)
            {
                if (c == null || c.transform.IsChildOf(transform))
                {
                    continue;
                }
                float distancia = Vector3.Distance(centro, c.ClosestPoint(centro));
                float factor = Mathf.Clamp01(1f - distancia / radio);
                IRecibeDanio victima = c.GetComponentInParent<IRecibeDanio>();
                if (victima != null && !ReferenceEquals(victima, this) && vistos.Add(victima))
                {
                    Vector3 direccion = (c.bounds.center - centro).normalized;
                    victima.RecibirDanio(new InfoDanio(config.DanioExplosion * factor, TipoDanio.Explosion, c.ClosestPoint(centro), direccion, null));
                }
                Rigidbody rb = c.attachedRigidbody;
                if (rb != null && rb != cuerpo && empujados.Add(rb))
                {
                    rb.AddExplosionForce(config.FuerzaExplosion * rb.mass, centro, radio, 1f, ForceMode.Impulse);
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

        private static void EmitirRafaga(ParticleSystem sistema, int cantidad, float velocidad)
        {
            if (sistema == null)
            {
                return;
            }
            for (int i = 0; i < cantidad; i++)
            {
                ParticleSystem.EmitParams parametros = new ParticleSystem.EmitParams
                {
                    position = sistema.transform.position + UnityEngine.Random.insideUnitSphere * 0.8f,
                    velocity = (Vector3.up * 0.8f + UnityEngine.Random.insideUnitSphere) * velocidad * UnityEngine.Random.Range(0.3f, 1f)
                };
                sistema.Emit(parametros, 1);
            }
        }

        private void CrearEfectos()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }
            textura = CrearTextura();
            material = new Material(shader) { mainTexture = textura, color = Color.white };
            humo = CrearParticulas("HumoCarro", new Color(0.18f, 0.18f, 0.2f, 0.85f), 0.7f, 3f, 2.2f, -0.05f);
            fuego = CrearParticulas("FuegoCarro", new Color(1f, 0.5f, 0.1f, 0.95f), 0.45f, 0.8f, 1.6f, -0.2f);
            GameObject objetoLuz = new GameObject("LuzFuegoCarro");
            objetoLuz.transform.SetParent(transform, false);
            objetoLuz.transform.localPosition = new Vector3(0f, AlturaMotor, AdelanteMotor);
            luz = objetoLuz.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.55f, 0.15f);
            luz.range = 8f;
            luz.intensity = 2f;
            luz.shadows = LightShadows.None;
            luz.enabled = false;
        }

        private ParticleSystem CrearParticulas(string nombre, Color color, float tamano, float vida, float velocidad, float gravedad)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform, false);
            objeto.transform.localPosition = new Vector3(0f, AlturaMotor, AdelanteMotor);
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = sistema.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = vida;
            main.startSize = tamano;
            main.startSpeed = velocidad;
            main.startColor = color;
            main.gravityModifier = gravedad;
            main.maxParticles = 200;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.rateOverTime = 0f;
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.angle = 18f;
            forma.radius = 0.25f;
            forma.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystem.SizeOverLifetimeModule tamanoVida = sistema.sizeOverLifetime;
            tamanoVida.enabled = true;
            tamanoVida.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1.6f)));
            ParticleSystem.ColorOverLifetimeModule colorVida = sistema.colorOverLifetime;
            colorVida.enabled = true;
            Gradient degradado = new Gradient();
            degradado.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorVida.color = degradado;
            ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sistema.Play();
            return sistema;
        }

        // Círculo suave blanco; el color sale de las partículas.
        private static Texture2D CrearTextura()
        {
            Texture2D t = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "DanioCarro_Punto" };
            float c = (LadoTextura - 1) * 0.5f;
            Color32[] pixeles = new Color32[LadoTextura * LadoTextura];
            for (int y = 0; y < LadoTextura; y++)
            {
                for (int x = 0; x < LadoTextura; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(1.4f - d * 1.4f) * 255f);
                    pixeles[y * LadoTextura + x] = new Color32(255, 255, 255, a);
                }
            }
            t.SetPixels32(pixeles);
            t.Apply(false, true);
            return t;
        }
    }
}

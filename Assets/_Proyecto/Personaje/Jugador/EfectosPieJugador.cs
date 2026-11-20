using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-1489: detalles de sensación del jugador a pie. Polvo en los pies al aterrizar de una caída (proporcional a la altura), salpicadura y chapoteo
    /// al pisar un charco, y temblor de cámara al recibir daño (por TemblorCamara, que escucha la cámara). Lo agrega <see cref="ControladorAPie"/>.
    /// Los charcos del mundo son quads sin collider en la capa 4 (Water) con nombre "Charco*": se buscan al cargar escena y cada
    /// <see cref="SegundosRebuscarCharcos"/> s (Personaje no puede referenciar Mundo). Cada efecto usa un solo ParticleSystem que se reposiciona y emite.
    /// </summary>
    [RequireComponent(typeof(ControladorAPie))]
    public class EfectosPieJugador : MonoBehaviour
    {
        private const int CapaCharcos = 4; // capa integrada Water: la de los charcos (ReflejoCharcos.CapaCharcos)
        private const string PrefijoCharco = "Charco";
        private const float SegundosRebuscarCharcos = 20f;
        private const float AlcanceCharcos = 60f;
        private const float AlturaPie = 0.05f;
        private const float DistanciaOrigenImpacto = 3f;
        private const int MaximoParticulas = 128;

        private ControladorAPie controlador;
        private ParticleSystem polvo;
        private ParticleSystem salpicadura;
        private ParticleSystem anillo;
        private readonly ReglaOndasCharco ondas = new ReglaOndasCharco();
        private readonly Vector4[] bufferOndas = new Vector4[ReglaOndasCharco.Maximo];
        private bool ondasPublicadas;
        private static readonly int IdOndas = Shader.PropertyToID("_OndasPisadas");
        private static readonly int IdCuantasOndas = Shader.PropertyToID("_OndasPisadasN");
        private static readonly int IdTiempoOndas = Shader.PropertyToID("_TiempoOndasCharco");
        private CinemachineImpulseSource fuenteTemblor;
        private readonly List<Transform> charcos = new List<Transform>();
        private float ultimaBusqueda = float.NegativeInfinity;

        /// <summary>Últimos valores emitidos, para las pruebas.</summary>
        public int UltimoPolvo { get; private set; }
        public int UltimasGotas { get; private set; }
        public float UltimaFuerzaTemblor { get; private set; }

        /// <summary>T-1503: las últimas pisadas en charcos (el shader dibuja una onda por cada una).</summary>
        public ReglaOndasCharco Ondas => ondas;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            polvo = CrearParticulas("PolvoAterrizaje", new Color(0.72f, 0.68f, 0.6f, 0.55f), 0.35f, 0.9f, 0.6f, 1.6f, 0.08f, 0.22f, -0.05f);
            salpicadura = CrearParticulas("SalpicaduraCharco", new Color(0.8f, 0.9f, 1f, 0.95f), 0.3f, 0.7f, 1.2f, 3f, 0.05f, 0.1f, 1f, TexturaRadial(32, 0f, 0.5f, 0.15f));
            anillo = CrearParticulas("AnilloCharco", new Color(0.75f, 0.88f, 1f, 0.7f), 0.7f, 0.7f, 0f, 0f, 0.15f, 0.15f, 0f, TexturaRadial(64, 0.36f, 0.5f, 0.06f));
            ConfigurarAnillo(anillo);
            fuenteTemblor = GetComponent<CinemachineImpulseSource>();
            if (fuenteTemblor == null)
            {
                fuenteTemblor = gameObject.AddComponent<CinemachineImpulseSource>();
            }
            TemblorCamara.Preparar(fuenteTemblor);
        }

        private void OnEnable()
        {
            controlador.Aterrizo += AlAterrizar;
            controlador.PasoDado += AlDarPaso;
            EventosPelea.ImpactoRecibido += AlImpacto;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void OnDisable()
        {
            controlador.Aterrizo -= AlAterrizar;
            controlador.PasoDado -= AlDarPaso;
            EventosPelea.ImpactoRecibido -= AlImpacto;
            SceneManager.sceneLoaded -= AlCargarEscena;
            if (ondasPublicadas)
            {
                Shader.SetGlobalFloat(IdCuantasOndas, 0f);
                ondasPublicadas = false;
            }
        }

        // T-1503: pasa las ondas vivas a todos los charcos como globales del shader (un solo juego para todos, sin tocar cada material).
        private void Update()
        {
            float ahora = Time.time;
            int n = ondas.Llenar(bufferOndas, ahora);
            if (n > 0 || ondasPublicadas)
            {
                Shader.SetGlobalVectorArray(IdOndas, bufferOndas);
                Shader.SetGlobalFloat(IdCuantasOndas, n);
                Shader.SetGlobalFloat(IdTiempoOndas, ahora);
                ondasPublicadas = n > 0;
            }
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo) => ultimaBusqueda = float.NegativeInfinity;

        private void AlAterrizar(float caida)
        {
            int cuantas = ReglaEfectosPie.ParticulasPolvo(caida);
            UltimoPolvo = cuantas;
            if (cuantas > 0)
            {
                Emitir(polvo, transform.position + Vector3.up * AlturaPie, cuantas);
            }
        }

        private void AlDarPaso(Vector3 posicion, string idPaso, bool corriendo)
        {
            if (!EstaEnCharco(posicion))
            {
                return;
            }
            UltimasGotas = ReglaEfectosPie.GotasSalpicadura(corriendo);
            Emitir(salpicadura, posicion + Vector3.up * AlturaPie, UltimasGotas);
            Emitir(anillo, posicion + Vector3.up * 0.03f, 1);
            ondas.Agregar(posicion, Time.time);
            if (idPaso != ReglaSuperficiePaso.Mojado)
            {
                Sonidos.ReproducirEn(ReglaSuperficiePaso.Mojado, posicion); // chapoteo: el catálogo no tiene uno propio, se usa el paso mojado
            }
        }

        private void AlImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (objetivo != null || info.Cantidad <= 0f || resultado.DanioAplicado <= 0f
                || (info.Punto - transform.position).sqrMagnitude > DistanciaOrigenImpacto * DistanciaOrigenImpacto)
            {
                return;
            }
            float fuerza = ReglaEfectosPie.FuerzaTemblorDanio(resultado.DanioAplicado);
            UltimaFuerzaTemblor = fuerza;
            TemblorCamara.Generar(fuenteTemblor, fuerza, info.Direccion);
        }

        private bool EstaEnCharco(Vector3 posicion)
        {
            if (Time.time - ultimaBusqueda > SegundosRebuscarCharcos)
            {
                ultimaBusqueda = Time.time;
                BuscarCharcos();
            }
            float alcance2 = AlcanceCharcos * AlcanceCharcos;
            foreach (Transform charco in charcos)
            {
                if (charco == null || !charco.gameObject.activeInHierarchy || (charco.position - posicion).sqrMagnitude > alcance2)
                {
                    continue;
                }
                Vector3 local = charco.InverseTransformPoint(posicion);
                if (ReglaEfectosPie.PisaCharco(local.x, local.y, posicion.y - charco.position.y))
                {
                    return true;
                }
            }
            return false;
        }

        private void BuscarCharcos()
        {
            charcos.Clear();
            foreach (MeshRenderer r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r.gameObject.layer == CapaCharcos && r.name.StartsWith(PrefijoCharco, StringComparison.Ordinal))
                {
                    charcos.Add(r.transform);
                }
            }
        }

        // Anillo que se abre en el agua: un solo cuadro horizontal que crece y se desvanece.
        private static void ConfigurarAnillo(ParticleSystem sistema)
        {
            ParticleSystem.MainModule main = sistema.main;
            main.startSize = 1.5f;
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.enabled = false;
            ParticleSystem.SizeOverLifetimeModule tamano = sistema.sizeOverLifetime;
            tamano.enabled = true;
            tamano.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.12f, 1f, 1f));
            sistema.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        }

        // Textura procedural: alfa 1 entre radios interior y exterior (de 0 a 0.5), suave en los bordes.
        private static Texture2D TexturaRadial(int lado, float interior, float exterior, float suavidad)
        {
            Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "TexturaCharco" };
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float r = Vector2.Distance(new Vector2((x + 0.5f) / lado, (y + 0.5f) / lado), new Vector2(0.5f, 0.5f));
                    float a = Mathf.Clamp01((r - interior) / suavidad) * Mathf.Clamp01((exterior - r) / suavidad);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            return t;
        }

        private static void Emitir(ParticleSystem sistema, Vector3 posicion, int cuantas)
        {
            sistema.transform.position = posicion;
            sistema.Emit(cuantas);
        }

        private ParticleSystem CrearParticulas(string nombre, Color color, float vidaMin, float vidaMax, float velMin, float velMax, float tamMin, float tamMax, float gravedad, Texture2D textura = null)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform, false);
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = sistema.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(vidaMin, vidaMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(velMin, velMax);
            main.startSize = new ParticleSystem.MinMaxCurve(tamMin, tamMax);
            main.startColor = color;
            main.gravityModifier = gravedad;
            main.maxParticles = MaximoParticulas;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.enabled = false;
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.shapeType = ParticleSystemShapeType.Circle;
            forma.radius = 0.35f;
            forma.radiusThickness = 1f;
            ParticleSystem.ColorOverLifetimeModule colorVida = sistema.colorOverLifetime;
            colorVida.enabled = true;
            Gradient degradado = new Gradient();
            degradado.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorVida.color = degradado;
            ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
            Shader s = textura != null ? Shader.Find("Sprites/Default") : Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material material = new Material(s != null ? s : Shader.Find("Sprites/Default"));
            if (textura != null)
            {
                material.mainTexture = textura;
            }
            render.sharedMaterial = material;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            return sistema;
        }
    }
}

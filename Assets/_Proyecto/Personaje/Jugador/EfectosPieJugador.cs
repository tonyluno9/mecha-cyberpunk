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
    /// T-1489: detalles de sensación del jugador a pie. Polvo en los pies al aterrizar de una caída (proporcional a la altura), ondas y chapoteo
    /// al pisar agua, y temblor de cámara al recibir daño (por TemblorCamara, que escucha la cámara). Lo agrega <see cref="ControladorAPie"/>.
    /// T-1525: sin salpicadura de gotas; solo las ondas de T-1503. Las ondas salen en todo piso mojado: en los charcos con el shader de agua las dibuja ese shader
    /// (globales _OndasPisadas); en el resto (asfalto mojado, banquetas, decals de charco) las dibuja <see cref="AnillosSuelo"/> sobre el piso.
    /// Los charcos del mundo son quads sin collider (capa 4 con nombre "Charco*", o con material de charco/mojado y planos): se buscan al cargar escena y cada
    /// <see cref="SegundosRebuscarCharcos"/> s (Personaje no puede referenciar Mundo).
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
        private const string NombreShaderCharco = "MechaCyberpunk/Mundo/Charco";
        private const float GrosorMaximoDecal = 0.15f; // m: un decal de agua es plano
        private const float LadoMaximoDecal = 40f; // m: más grande que esto es un piso, no un decal
        private static readonly string[] PalabrasAgua = { "charco", "puddle", "mojad", "wet" };

        private ControladorAPie controlador;
        private ParticleSystem polvo;
        private readonly ReglaOndasCharco ondas = new ReglaOndasCharco();
        private readonly Vector4[] bufferOndas = new Vector4[ReglaOndasCharco.Maximo];
        private bool ondasPublicadas;
        private static readonly int IdOndas = Shader.PropertyToID("_OndasPisadas");
        private static readonly int IdCuantasOndas = Shader.PropertyToID("_OndasPisadasN");
        private static readonly int IdTiempoOndas = Shader.PropertyToID("_TiempoOndasCharco");
        private CinemachineImpulseSource fuenteTemblor;
        private readonly AnillosSuelo anillosSuelo = new AnillosSuelo();
        private readonly List<Charco> charcos = new List<Charco>();

        private readonly struct Charco
        {
            public readonly Renderer Render;
            public readonly bool EsQuadUnitario; // quad de lado 1 en local (los charcos del kit): se prueba con coordenadas locales
            public readonly bool ConShaderAgua; // sus ondas las dibuja el shader de charco

            public Charco(Renderer render, bool esQuadUnitario, bool conShaderAgua)
            {
                Render = render;
                EsQuadUnitario = esQuadUnitario;
                ConShaderAgua = conShaderAgua;
            }
        }
        private float ultimaBusqueda = float.NegativeInfinity;

        /// <summary>Últimos valores emitidos, para las pruebas.</summary>
        public int UltimoPolvo { get; private set; }
        public float UltimaFuerzaTemblor { get; private set; }

        /// <summary>T-1503: las últimas pisadas en charcos (el shader dibuja una onda por cada una).</summary>
        public ReglaOndasCharco Ondas => ondas;

        /// <summary>T-1525: las ondas dibujadas sobre pisos mojados que no son un charco con el shader de agua.</summary>
        public AnillosSuelo Anillos => anillosSuelo;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            polvo = CrearParticulas("PolvoAterrizaje", new Color(0.72f, 0.68f, 0.6f, 0.55f), 0.35f, 0.9f, 0.6f, 1.6f, 0.08f, 0.22f, -0.05f);
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
            anillosSuelo.Actualizar(float.PositiveInfinity);
        }

        private void OnDestroy() => anillosSuelo.Destruir();

        // T-1503: pasa las ondas vivas a todos los charcos como globales del shader (un solo juego para todos, sin tocar cada material).
        private void Update()
        {
            float ahora = Time.time;
            int n = ondas.Llenar(bufferOndas, ahora);
            int enSuelo = anillosSuelo.Actualizar(ahora);
            if (n > 0 || enSuelo > 0 || ondasPublicadas)
            {
                Shader.SetGlobalVectorArray(IdOndas, bufferOndas);
                Shader.SetGlobalFloat(IdCuantasOndas, n);
                Shader.SetGlobalFloat(IdTiempoOndas, ahora); // el mismo reloj para el shader de charco y el de las ondas de piso
                ondasPublicadas = n > 0 || enSuelo > 0;
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
            bool enCharco = EstaEnCharco(posicion, out bool conShaderAgua);
            if (!enCharco && idPaso != ReglaSuperficiePaso.Mojado)
            {
                return; // piso seco
            }
            if (enCharco && conShaderAgua)
            {
                ondas.Agregar(posicion, Time.time); // las dibuja el shader de agua de ese charco
            }
            else
            {
                anillosSuelo.Agregar(posicion, Time.time); // asfalto mojado, banqueta mojada o decal de charco sin shader de agua
            }
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

        private bool EstaEnCharco(Vector3 posicion, out bool conShaderAgua)
        {
            conShaderAgua = false;
            if (Time.time - ultimaBusqueda > SegundosRebuscarCharcos)
            {
                ultimaBusqueda = Time.time;
                BuscarCharcos();
            }
            float alcance2 = AlcanceCharcos * AlcanceCharcos;
            foreach (Charco charco in charcos)
            {
                Renderer r = charco.Render;
                if (r == null || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }
                Transform t = r.transform;
                if ((t.position - posicion).sqrMagnitude > alcance2)
                {
                    continue;
                }
                bool pisa;
                if (charco.EsQuadUnitario)
                {
                    Vector3 local = t.InverseTransformPoint(posicion);
                    pisa = ReglaEfectosPie.PisaCharco(local.x, local.y, posicion.y - t.position.y);
                }
                else
                {
                    Bounds b = r.bounds;
                    pisa = ReglaEfectosPie.PisaAreaPlana(posicion.x - b.center.x, posicion.z - b.center.z, b.extents.x, b.extents.z, posicion.y - b.center.y);
                }
                if (pisa)
                {
                    conShaderAgua = charco.ConShaderAgua;
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
                Material m = r.sharedMaterial;
                if (r.gameObject.layer == CapaCharcos && r.name.StartsWith(PrefijoCharco, StringComparison.Ordinal))
                {
                    charcos.Add(new Charco(r, true, TieneShaderAgua(m)));
                    continue;
                }
                Vector3 tam = r.bounds.size;
                if (m != null && tam.y <= GrosorMaximoDecal && tam.x < LadoMaximoDecal && tam.z < LadoMaximoDecal && ContieneAgua(m.name))
                {
                    // T-1525: decals planos de agua o mojado con otro material (T508, Tianguis, banqueta mojada...): se prueba con su caja plana.
                    charcos.Add(new Charco(r, false, TieneShaderAgua(m)));
                }
            }
        }

        private static bool TieneShaderAgua(Material m) => m != null && m.shader != null && m.shader.name == NombreShaderCharco;

        private static bool ContieneAgua(string nombre)
        {
            foreach (string p in PalabrasAgua)
            {
                if (nombre.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static void Emitir(ParticleSystem sistema, Vector3 posicion, int cuantas)
        {
            sistema.transform.position = posicion;
            sistema.Emit(cuantas);
        }

        private ParticleSystem CrearParticulas(string nombre, Color color, float vidaMin, float vidaMax, float velMin, float velMax, float tamMin, float tamMax, float gravedad)
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
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material material = new Material(s != null ? s : Shader.Find("Sprites/Default"));
            render.sharedMaterial = material;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            return sistema;
        }
    }
}

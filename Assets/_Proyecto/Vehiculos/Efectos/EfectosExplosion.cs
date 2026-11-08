using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Efectos
{
    /// <summary>Lo que se creó con una explosión: la raíz y sus partes, para pruebas y para quien quiera seguirlas.</summary>
    public sealed class InstanciaExplosion
    {
        public GameObject Raiz;
        public ParticleSystem Fuego;
        public ParticleSystem Humo;
        public ParticleSystem Chispas;
        public ParticleSystem Onda;
        public ParticleSystem Chamusco;
        public Light Luz;
    }

    /// <summary>
    /// T-745: la única entrada para explosiones, humo y fuego (carros, barriles, dron). Usa los prefabs flipbook del Unity Particle Pack
    /// del <see cref="CatalogoEfectos"/> con materiales URP; nada se dibuja por código salvo el destello de luz y la mancha del suelo.
    /// </summary>
    public static class EfectosExplosion
    {
        public const float RadioMaxDiminuta = 3f;
        public const float RadioMaxChica = 6f;
        private const float SegundosDestello = 0.35f;
        private const float IntensidadDestello = 14f;
        private const float SegundosHumoExplosion = 3f;
        private const float SegundosChamusco = 25f;

        /// <summary>La última explosión creada (para pruebas y capturas).</summary>
        public static InstanciaExplosion Ultima { get; private set; }

        public static bool Disponible => CatalogoEfectos.Actual != null && CatalogoEfectos.Actual.ExplosionChica != null;

        /// <summary>Explosión completa en <paramref name="centro"/>: bola de fuego, onda, brasas, humo, chispas, mancha en el suelo y destello de luz.</summary>
        public static InstanciaExplosion Explosion(Vector3 centro, float radio, Transform padre = null)
        {
            CatalogoEfectos catalogo = CatalogoEfectos.Actual;
            InstanciaExplosion resultado = new InstanciaExplosion();
            GameObject raiz = new GameObject("EfectoExplosion");
            raiz.transform.position = centro;
            if (padre != null)
            {
                raiz.transform.SetParent(padre, true);
            }
            resultado.Raiz = raiz;
            float vida = 3f;
            if (catalogo != null)
            {
                GameObject plantilla = radio <= RadioMaxDiminuta ? catalogo.ExplosionDiminuta : radio <= RadioMaxChica ? catalogo.ExplosionChica : catalogo.ExplosionGrande;
                if (plantilla != null)
                {
                    GameObject nucleo = Object.Instantiate(plantilla, centro, Quaternion.identity, raiz.transform);
                    nucleo.name = "Nucleo";
                    vida = Mathf.Max(vida, UnaVez(nucleo));
                    foreach (ParticleSystem sistema in nucleo.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        string n = sistema.name.ToLowerInvariant();
                        if (n.Contains("shockwave"))
                        {
                            if (radio <= RadioMaxDiminuta)
                            {
                                // La onda del prefab diminuto es una cúpula enorme comparada con su bola de fuego: en una explosión chica se quita.
                                Object.Destroy(sistema.gameObject);
                                continue;
                            }
                            resultado.Onda = sistema;
                        }
                        else if (n.Contains("embers") && resultado.Chispas == null)
                        {
                            resultado.Chispas = sistema;
                        }
                        else if (n.Contains("smoke") && resultado.Humo == null)
                        {
                            resultado.Humo = sistema;
                        }
                        else if (resultado.Fuego == null && !n.Contains("light") && !n.Contains("debris"))
                        {
                            resultado.Fuego = sistema;
                        }
                    }
                    if (resultado.Fuego == null)
                    {
                        resultado.Fuego = nucleo.GetComponentInChildren<ParticleSystem>(true);
                    }
                }
                if (catalogo.Humo != null)
                {
                    GameObject humo = Object.Instantiate(catalogo.Humo, centro, Quaternion.identity, raiz.transform);
                    humo.name = "HumoExplosion";
                    vida = Mathf.Max(vida, UnaVez(humo, SegundosHumoExplosion));
                    resultado.Humo = humo.GetComponentInChildren<ParticleSystem>(true);
                }
                if (catalogo.Chispas != null)
                {
                    GameObject chispas = Object.Instantiate(catalogo.Chispas, centro, Quaternion.identity, raiz.transform);
                    chispas.name = "ChispasExplosion";
                    vida = Mathf.Max(vida, UnaVez(chispas, 0.4f));
                    resultado.Chispas = chispas.GetComponentInChildren<ParticleSystem>(true);
                }
                resultado.Chamusco = CrearChamusco(raiz.transform, centro, radio, catalogo.MaterialChamusco);
            }
            resultado.Luz = CrearDestello(raiz.transform, centro, radio);
            Ultima = resultado;
            Object.Destroy(raiz, Mathf.Max(vida, resultado.Chamusco != null ? SegundosChamusco : 0f) + 0.5f);
            return resultado;
        }

        /// <summary>Humo continuo (la emisión se maneja con <c>emission.rateOverTime</c>), pegado a <paramref name="padre"/>. Null si no hay catálogo.</summary>
        public static ParticleSystem Humo(Transform padre, Vector3 posicionLocal, float escala = 1f) =>
            Continuo(CatalogoEfectos.Actual != null ? CatalogoEfectos.Actual.Humo : null, "HumoPack", padre, posicionLocal, escala);

        /// <summary>Fuego continuo chico (la emisión se maneja con <c>emission.rateOverTime</c>), pegado a <paramref name="padre"/>. Null si no hay catálogo.</summary>
        public static ParticleSystem Fuego(Transform padre, Vector3 posicionLocal, float escala = 1f) =>
            Continuo(CatalogoEfectos.Actual != null ? CatalogoEfectos.Actual.FuegoChico : null, "FuegoPack", padre, posicionLocal, escala);

        /// <summary>Llamas en el suelo que duran <paramref name="segundos"/> (un barril que reventó).</summary>
        public static GameObject FuegoSuelo(Vector3 punto, float segundos)
        {
            CatalogoEfectos catalogo = CatalogoEfectos.Actual;
            if (catalogo == null || catalogo.FuegoMediano == null)
            {
                return null;
            }
            GameObject llamas = Object.Instantiate(catalogo.FuegoMediano, punto, Quaternion.identity);
            llamas.name = "LlamasSuelo";
            foreach (ParticleSystem sistema in llamas.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = sistema.main;
                main.loop = true;
                sistema.Play();
            }
            llamas.AddComponent<ApagarLlamas>().Segundos = segundos;
            Object.Destroy(llamas, segundos + 4f);
            return llamas;
        }

        // Pone todos los sistemas del prefab en "una sola vez" (sin loop) y devuelve cuánto dura lo más largo.
        private static float UnaVez(GameObject objeto, float duracionEmision = -1f)
        {
            float maximo = 1f;
            foreach (ParticleSystem sistema in objeto.GetComponentsInChildren<ParticleSystem>(true))
            {
                sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = sistema.main;
                main.loop = false;
                main.stopAction = ParticleSystemStopAction.None;
                if (duracionEmision > 0f)
                {
                    main.duration = duracionEmision;
                }
                maximo = Mathf.Max(maximo, main.duration + main.startLifetime.constantMax);
                sistema.Play();
            }
            return Mathf.Min(maximo, 10f);
        }

        private static ParticleSystem Continuo(GameObject plantilla, string nombre, Transform padre, Vector3 posicionLocal, float escala)
        {
            if (plantilla == null)
            {
                return null;
            }
            GameObject objeto = Object.Instantiate(plantilla, padre);
            objeto.name = nombre;
            objeto.transform.localPosition = posicionLocal;
            objeto.transform.localScale = Vector3.one * escala;
            ParticleSystem principal = objeto.GetComponent<ParticleSystem>();
            if (principal == null)
            {
                principal = objeto.GetComponentInChildren<ParticleSystem>(true);
            }
            foreach (ParticleSystem sistema in objeto.GetComponentsInChildren<ParticleSystem>(true))
            {
                sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = sistema.main;
                main.loop = true;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                ParticleSystem.EmissionModule emision = sistema.emission;
                emision.SetBursts(new ParticleSystem.Burst[0]);
                emision.rateOverTime = 0f;
                sistema.Play();
            }
            return principal;
        }

        private static Light CrearDestello(Transform padre, Vector3 centro, float radio)
        {
            GameObject objeto = new GameObject("DestelloExplosion");
            objeto.transform.SetParent(padre, true);
            objeto.transform.position = centro;
            Light luz = objeto.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.6f, 0.2f);
            luz.range = Mathf.Max(radio * 2f, 12f);
            luz.intensity = IntensidadDestello;
            luz.shadows = LightShadows.None;
            objeto.AddComponent<DestelloLuz>();
            return luz;
        }

        // Una sola partícula plana en el suelo que se queda un buen rato y se desvanece: la mancha quemada.
        private static ParticleSystem CrearChamusco(Transform padre, Vector3 centro, float radio, Material material)
        {
            if (material == null)
            {
                return null;
            }
            Vector3 punto = centro;
            if (Physics.Raycast(centro + Vector3.up * 0.5f, Vector3.down, out RaycastHit suelo, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                punto = suelo.point;
            }
            GameObject objeto = new GameObject("Chamusco");
            objeto.transform.SetParent(padre, true);
            objeto.transform.position = punto + Vector3.up * 0.03f;
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = sistema.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = SegundosChamusco;
            main.startSpeed = 0f;
            main.startSize = Mathf.Max(radio, 2f) * 1.5f;
            main.startColor = new Color(0f, 0f, 0f, 0.85f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.rateOverTime = 0f;
            emision.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.enabled = false;
            ParticleSystem.ColorOverLifetimeModule color = sistema.colorOverLifetime;
            color.enabled = true;
            Gradient degradado = new Gradient();
            degradado.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.02f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = degradado;
            ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            render.sharedMaterial = material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sistema.Play();
            return sistema;
        }

        private sealed class DestelloLuz : MonoBehaviour
        {
            private float inicio;
            private Light luz;

            private void Awake()
            {
                inicio = Time.time;
                luz = GetComponent<Light>();
            }

            private void Update()
            {
                float t = (Time.time - inicio) / SegundosDestello;
                luz.intensity = Mathf.Lerp(IntensidadDestello, 0f, Mathf.Clamp01(t));
                if (t >= 1f)
                {
                    luz.enabled = false;
                }
            }
        }

        private sealed class ApagarLlamas : MonoBehaviour
        {
            public float Segundos;
            private float fin = -1f;

            private void Update()
            {
                if (fin < 0f)
                {
                    fin = Time.time + Segundos;
                }
                if (Time.time >= fin)
                {
                    foreach (ParticleSystem sistema in GetComponentsInChildren<ParticleSystem>(true))
                    {
                        sistema.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                    enabled = false;
                }
            }
        }
    }
}

using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// Los efectos del estallido de un carro (T-591), todo por código: bola de fuego, humo, chispas, onda expansiva (anillo que crece), luz
    /// que destella y un estruendo provisional generado por código (no hay archivo de explosión en Arte/Audio). Vive solo en su propio objeto
    /// y se borra al terminar, así que sobrevive aunque se destruya el carro. El empuje y el daño por radio los hace <see cref="DanioCarro"/>.
    /// </summary>
    public class ExplosionCarro : MonoBehaviour
    {
        public const float SegundosVida = 4f;
        private const float SegundosDestello = 0.35f;
        private const float IntensidadDestello = 14f;
        private const int LadoTextura = 64;
        private const int FrecuenciaSonido = 22050;
        private const float SegundosSonido = 1.6f;

        private static Texture2D texturaPunto;
        private static Texture2D texturaAnillo;
        private static AudioClip clipEstruendo;

        private Light luz;
        private float inicio;

        public ParticleSystem Fuego { get; private set; }
        public ParticleSystem Humo { get; private set; }
        public ParticleSystem Chispas { get; private set; }
        public ParticleSystem Onda { get; private set; }
        public Light Luz => luz;
        public AudioSource Fuente { get; private set; }

        /// <summary>Crea la explosión en <paramref name="centro"/>; <paramref name="radio"/> es hasta donde llega la onda.</summary>
        public static ExplosionCarro Crear(Vector3 centro, float radio)
        {
            GameObject objeto = new GameObject("ExplosionCarro");
            objeto.transform.position = centro;
            ExplosionCarro explosion = objeto.AddComponent<ExplosionCarro>();
            explosion.Armar(radio);
            return explosion;
        }

        private void Armar(float radio)
        {
            inicio = Time.time;
            Texture2D punto = TexturaPunto();
            Fuego = CrearSistema("Bola", punto, new Color(1f, 0.55f, 0.12f, 1f), 1.4f, 1.1f, 1.2f, 7f, -0.1f, 40, true);
            Humo = CrearSistema("Humo", punto, new Color(0.15f, 0.15f, 0.17f, 0.9f), 2.2f, 3.2f, 2.2f, 3.5f, -0.05f, 24, true);
            Chispas = CrearSistema("Chispas", punto, new Color(1f, 0.85f, 0.4f, 1f), 0.12f, 1.4f, 14f, 22f, 1.2f, 50, true);
            Onda = CrearSistema("Onda", TexturaAnillo(), new Color(1f, 0.8f, 0.5f, 0.8f), 0.5f, 0.45f, 0f, 0f, 0f, 1, false);
            ParticleSystem.SizeOverLifetimeModule tamano = Onda.sizeOverLifetime;
            tamano.enabled = true;
            tamano.size = new ParticleSystem.MinMaxCurve(Mathf.Max(radio, 2f) * 2f, new AnimationCurve(new Keyframe(0f, 0.05f), new Keyframe(1f, 1f)));
            ParticleSystemRenderer renderOnda = Onda.GetComponent<ParticleSystemRenderer>();
            renderOnda.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            Onda.transform.position = transform.position - Vector3.up * 0.3f;

            GameObject objetoLuz = new GameObject("DestelloExplosion");
            objetoLuz.transform.SetParent(transform, false);
            luz = objetoLuz.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.6f, 0.2f);
            luz.range = Mathf.Max(radio * 2f, 12f);
            luz.intensity = IntensidadDestello;
            luz.shadows = LightShadows.None;

            Fuente = gameObject.AddComponent<AudioSource>();
            Fuente.clip = ClipEstruendo();
            Fuente.spatialBlend = 1f;
            Fuente.minDistance = 8f;
            Fuente.maxDistance = 120f;
            Fuente.volume = 1f;
            Fuente.Play();

            Fuego.Play();
            Humo.Play();
            Chispas.Play();
            Onda.Play();
            Destroy(gameObject, SegundosVida);
        }

        private void Update()
        {
            if (luz == null)
            {
                return;
            }
            float t = (Time.time - inicio) / SegundosDestello;
            luz.intensity = Mathf.Lerp(IntensidadDestello, 0f, Mathf.Clamp01(t));
            if (t >= 1f)
            {
                luz.enabled = false;
            }
        }

        private void OnDestroy()
        {
            foreach (ParticleSystemRenderer render in GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (render.sharedMaterial != null)
                {
                    Destroy(render.sharedMaterial);
                }
            }
        }

        private ParticleSystem CrearSistema(string nombre, Texture2D textura, Color color, float tamano, float vida, float velocidadMin,
            float velocidadMax, float gravedad, int cantidad, bool esfera)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform, false);
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = sistema.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(vida * 0.6f, vida);
            main.startSize = new ParticleSystem.MinMaxCurve(tamano * 0.6f, tamano * 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(velocidadMin, Mathf.Max(velocidadMin, velocidadMax));
            main.startColor = color;
            main.gravityModifier = gravedad;
            main.maxParticles = 200;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.rateOverTime = 0f;
            emision.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cantidad) });
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.shapeType = esfera ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Circle;
            forma.radius = esfera ? 0.6f : 0.01f;
            ParticleSystem.ColorOverLifetimeModule colorVida = sistema.colorOverLifetime;
            colorVida.enabled = true;
            Gradient degradado = new Gradient();
            degradado.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorVida.color = degradado;
            ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = textura, color = Color.white };
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return sistema;
        }

        private static Texture2D TexturaPunto()
        {
            if (texturaPunto == null)
            {
                texturaPunto = CrearTextura("Explosion_Punto", false);
            }
            return texturaPunto;
        }

        private static Texture2D TexturaAnillo()
        {
            if (texturaAnillo == null)
            {
                texturaAnillo = CrearTextura("Explosion_Anillo", true);
            }
            return texturaAnillo;
        }

        private static Texture2D CrearTextura(string nombre, bool anillo)
        {
            Texture2D t = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = nombre };
            float c = (LadoTextura - 1) * 0.5f;
            Color32[] pixeles = new Color32[LadoTextura * LadoTextura];
            for (int y = 0; y < LadoTextura; y++)
            {
                for (int x = 0; x < LadoTextura; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = anillo ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) * 9f) : Mathf.Clamp01(1.4f - d * 1.4f);
                    pixeles[y * LadoTextura + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            t.SetPixels32(pixeles);
            t.Apply(false, true);
            return t;
        }

        /// <summary>Estruendo provisional: ruido con caída exponencial y golpe grave al inicio. Cambiar por un archivo de Arte/Audio cuando exista.</summary>
        public static AudioClip ClipEstruendo()
        {
            if (clipEstruendo != null)
            {
                return clipEstruendo;
            }
            int muestras = (int)(FrecuenciaSonido * SegundosSonido);
            float[] datos = new float[muestras];
            System.Random azar = new System.Random(591);
            float filtro = 0f;
            for (int i = 0; i < muestras; i++)
            {
                float t = i / (float)FrecuenciaSonido;
                float ruido = (float)(azar.NextDouble() * 2.0 - 1.0);
                filtro += (ruido - filtro) * Mathf.Lerp(0.5f, 0.04f, Mathf.Clamp01(t * 2f));
                float grave = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 35f, Mathf.Clamp01(t * 1.5f)) * t);
                datos[i] = Mathf.Clamp((filtro * 1.8f + grave * 0.7f) * Mathf.Exp(-t * 3.2f), -1f, 1f);
            }
            clipEstruendo = AudioClip.Create("Explosion_Provisional", muestras, 1, FrecuenciaSonido, false);
            clipEstruendo.SetData(datos, 0);
            return clipEstruendo;
        }
    }
}

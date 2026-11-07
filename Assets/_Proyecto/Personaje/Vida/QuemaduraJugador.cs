using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Vida
{
    /// <summary>
    /// El fuego sobre el jugador (T-691). <see cref="VidaAPie"/> lo prende al recibir daño de explosión; aquí corre <see cref="ReglaQuemadura"/>:
    /// daño por tick, llamas visibles en el cuerpo, y se apaga solo (4 s), al rodar o al salir del área en llamas. Lo crea VidaAPie al nacer.
    /// </summary>
    public class QuemaduraJugador : MonoBehaviour
    {
        private const float AlturaLlamas = 0.9f;
        private const int LadoTextura = 32;

        private VidaAPie vida;
        private CubiertaYRodada cubierta;
        private readonly ReglaQuemadura regla = new ReglaQuemadura();
        private ParticleSystem llamas;
        private Light luz;
        private Material material;
        private Texture2D textura;

        public bool Ardiendo => regla.Ardiendo;
        public float Restante => regla.Restante;
        public ReglaQuemadura.Apagado UltimoApagado => regla.UltimoApagado;
        public ParticleSystem Llamas => llamas;

        /// <summary>Daño total que ha hecho el fuego (para medir).</summary>
        public float DanioTotal { get; private set; }

        public void Iniciar(VidaAPie vidaJugador)
        {
            vida = vidaJugador;
            cubierta = GetComponent<CubiertaYRodada>();
            CrearEfectos();
        }

        /// <summary>Prende al jugador; el origen es el punto del estallido o del fuego.</summary>
        public void Prender(Vector3 origen)
        {
            if (vida == null || vida.EstaKO)
            {
                return;
            }
            regla.Prender(origen);
            Mostrar(true);
        }

        public void Apagar()
        {
            regla.Apagar();
            Mostrar(false);
        }

        private void Update()
        {
            if (!regla.Ardiendo || vida == null)
            {
                return;
            }
            if (vida.EstaKO)
            {
                Apagar();
                return;
            }
            float danio = regla.Avanzar(Time.deltaTime, cubierta != null && cubierta.Rodando, transform.position);
            if (danio > 0f)
            {
                DanioTotal += vida.AplicarQuemadura(danio);
            }
            if (!regla.Ardiendo)
            {
                Mostrar(false);
            }
            else if (luz != null)
            {
                luz.intensity = 1.2f + Mathf.PerlinNoise(Time.time * 11f, 0f) * 1.6f;
            }
        }

        private void OnDisable()
        {
            if (regla.Ardiendo)
            {
                Apagar();
            }
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

        private void Mostrar(bool encendido)
        {
            if (llamas != null)
            {
                ParticleSystem.EmissionModule emision = llamas.emission;
                emision.rateOverTime = encendido ? 90f : 0f;
            }
            if (luz != null)
            {
                luz.enabled = encendido;
            }
        }

        private void CrearEfectos()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }
            textura = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Quemadura_Punto" };
            float c = (LadoTextura - 1) * 0.5f;
            Color32[] pixeles = new Color32[LadoTextura * LadoTextura];
            for (int y = 0; y < LadoTextura; y++)
            {
                for (int x = 0; x < LadoTextura; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    pixeles[y * LadoTextura + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(1.4f - d * 1.4f) * 255f));
                }
            }
            textura.SetPixels32(pixeles);
            textura.Apply(false, true);
            material = new Material(shader) { mainTexture = textura, color = Color.white };

            GameObject objeto = new GameObject("LlamasJugador");
            objeto.transform.SetParent(transform, false);
            objeto.transform.localPosition = new Vector3(0f, AlturaLlamas, 0f);
            llamas = objeto.AddComponent<ParticleSystem>();
            llamas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = llamas.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.55f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startSpeed = 0.9f;
            main.startColor = new Color(1f, 0.5f, 0.1f, 0.95f);
            main.gravityModifier = -0.3f;
            main.maxParticles = 200;
            ParticleSystem.EmissionModule emision = llamas.emission;
            emision.rateOverTime = 0f;
            ParticleSystem.ShapeModule forma = llamas.shape;
            forma.shapeType = ParticleSystemShapeType.Sphere;
            forma.radius = 0.32f;
            forma.scale = new Vector3(1f, 2.2f, 1f);
            ParticleSystem.ColorOverLifetimeModule colorVida = llamas.colorOverLifetime;
            colorVida.enabled = true;
            Gradient degradado = new Gradient();
            degradado.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.25f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorVida.color = degradado;
            ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            llamas.Play();

            GameObject objetoLuz = new GameObject("LuzLlamasJugador");
            objetoLuz.transform.SetParent(transform, false);
            objetoLuz.transform.localPosition = new Vector3(0f, AlturaLlamas, 0.2f);
            luz = objetoLuz.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.55f, 0.15f);
            luz.range = 6f;
            luz.shadows = LightShadows.None;
            luz.enabled = false;
        }
    }
}

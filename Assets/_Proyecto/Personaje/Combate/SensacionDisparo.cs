using System;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Sensación de golpe y disparo (T-420). Lo crea <see cref="PistolaAPie"/> en el jugador. Da la patada de cámara
    /// (<see cref="Patada"/> y <see cref="Desvio"/>, que lee <see cref="CamaraHombro"/>), el destello del cañón, las chispas al pegar
    /// en pared o carro, la salpicadura al pegar en persona y avisa a la retícula (<see cref="Conecto"/>). Los puños dan un sacudón al conectar.
    /// Los números salen de <see cref="DatosSensacion"/> (Resources/DatosSensacion).
    /// </summary>
    public class SensacionDisparo : MonoBehaviour
    {
        private const int LadoTextura = 32;

        private PistolaAPie pistola;
        private GolpeAPie golpe;
        private ZoomSniper zoom;
        private DatosSensacion datos;
        private float magnitud;
        private float magnitudDesvio;
        private float inicio = -100f;
        private float finDestello = -1f;
        private Transform destello;
        private ParticleSystem chispas;
        private ParticleSystem sangre;
        private Texture2D textura;
        private Material material;

        /// <summary>Se dispara cuando un tiro o un golpe pega a una persona; el argumento es si la dejó en KO.</summary>
        public event Action<bool> Conecto;

        public DatosSensacion Datos => datos;
        public bool DestelloVisible => destello != null && destello.gameObject.activeSelf;
        public ParticleSystem Chispas => chispas;
        public ParticleSystem Sangre => sangre;

        /// <summary>Patada de cámara hacia arriba (°) en este instante.</summary>
        public float Patada => magnitud * Restante();
        /// <summary>Desvío lateral de la cámara (°) en este instante.</summary>
        public float Desvio => magnitudDesvio * Restante();

        private float Restante()
        {
            float t = Mathf.Clamp01((Time.time - inicio) / datos.DuracionPatada);
            return (1f - t) * (1f - t);
        }

        private void Awake()
        {
            datos = Resources.Load<DatosSensacion>("DatosSensacion");
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosSensacion>();
            }
            pistola = GetComponent<PistolaAPie>();
            golpe = GetComponent<GolpeAPie>();
            zoom = GetComponent<ZoomSniper>();
            CrearEfectos();
        }

        private readonly SeguidorImplantes implantes = new SeguidorImplantes();

        private void OnEnable()
        {
            implantes.Activar();
            if (pistola != null)
            {
                pistola.Fogonazo += AlDisparar;
                pistola.BalaImpacto += AlImpactar;
            }
            if (golpe != null)
            {
                golpe.Conecto += AlConectarPuno;
            }
        }

        private void OnDisable()
        {
            implantes.Desactivar();
            if (pistola != null)
            {
                pistola.Fogonazo -= AlDisparar;
                pistola.BalaImpacto -= AlImpactar;
            }
            if (golpe != null)
            {
                golpe.Conecto -= AlConectarPuno;
            }
        }

        private void OnDestroy()
        {
            if (destello != null)
            {
                Destroy(destello.gameObject);
            }
            if (chispas != null)
            {
                Destroy(chispas.gameObject);
            }
            if (sangre != null)
            {
                Destroy(sangre.gameObject);
            }
            if (textura != null)
            {
                Destroy(textura);
            }
            if (material != null)
            {
                Destroy(material);
            }
        }

        private void Update()
        {
            if (destello != null && destello.gameObject.activeSelf && Time.time >= finDestello)
            {
                destello.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (destello != null && destello.gameObject.activeSelf && Camera.main != null)
            {
                // El quad mira hacia la cámara (su cara visible es la de -Z).
                destello.rotation = Quaternion.LookRotation(destello.position - Camera.main.transform.position);
            }
        }

        private void Patear(AjusteSensacion ajuste)
        {
            magnitud = Mathf.Min(datos.TopePatada, Patada + ajuste.patada * EfectosAPie.FactorRetroceso(implantes.Efectos));
            magnitudDesvio = ajuste.desvio * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
            inicio = Time.time;
        }

        private void AlDisparar(ArmaAPie arma, Vector3 boca)
        {
            AjusteSensacion ajuste = datos.Ajuste(arma);
            // El zoom del sniper ya da su propia sacudida (T-416).
            if (!(arma == ArmaAPie.Sniper && zoom != null && zoom.Activo))
            {
                Patear(ajuste);
            }
            if (destello != null && ajuste.destello > 0f)
            {
                destello.position = boca;
                destello.localScale = Vector3.one * ajuste.destello;
                destello.gameObject.SetActive(true);
                finDestello = Time.time + datos.DuracionDestello;
            }
        }

        private void AlImpactar(ArmaAPie arma, ImpactoBala impacto)
        {
            int cantidad = datos.Ajuste(arma).particulas;
            if (impacto.EnPersona)
            {
                Emitir(sangre, impacto.Punto, impacto.Normal, cantidad, 2.5f);
                Conecto?.Invoke(impacto.Mato);
            }
            else
            {
                Emitir(chispas, impacto.Punto, impacto.Normal, cantidad, 4f);
            }
        }

        private void AlConectarPuno(bool mato)
        {
            Patear(datos.Ajuste(ArmaAPie.Punos));
            Conecto?.Invoke(mato);
        }

        private static void Emitir(ParticleSystem sistema, Vector3 punto, Vector3 normal, int cantidad, float velocidad)
        {
            if (sistema == null)
            {
                return;
            }
            for (int i = 0; i < cantidad; i++)
            {
                var parametros = new ParticleSystem.EmitParams
                {
                    position = punto + normal * 0.02f,
                    velocity = (normal + UnityEngine.Random.insideUnitSphere * 0.8f) * velocidad * UnityEngine.Random.Range(0.4f, 1f)
                };
                sistema.Emit(parametros, 1);
            }
        }

        private void CrearEfectos()
        {
            Material baseMat = datos.MaterialEfecto;
            if (baseMat == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    return;
                }
                baseMat = new Material(shader);
            }
            material = new Material(baseMat);
            textura = CrearTextura();
            material.mainTexture = textura;
            material.color = new Color(1f, 0.85f, 0.4f, 1f);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "DestelloCanon";
            Destroy(quad.GetComponent<Collider>());
            MeshRenderer render = quad.GetComponent<MeshRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            destello = quad.transform;
            quad.SetActive(false);

            chispas = CrearParticulas("ChispasImpacto", new Color(1f, 0.85f, 0.35f, 1f), 0.05f, 0.6f);
            sangre = CrearParticulas("SalpicaduraImpacto", new Color(0.8f, 0.05f, 0.05f, 1f), 0.07f, 1.6f);
        }

        private ParticleSystem CrearParticulas(string nombre, Color color, float tamano, float gravedad)
        {
            GameObject objeto = new GameObject(nombre);
            ParticleSystem sistema = objeto.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = sistema.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = datos.VidaParticula;
            main.startSize = tamano;
            main.startColor = color;
            main.gravityModifier = gravedad;
            main.maxParticles = 128;
            ParticleSystem.EmissionModule emision = sistema.emission;
            emision.enabled = false;
            ParticleSystem.ShapeModule forma = sistema.shape;
            forma.enabled = false;
            ParticleSystem.ColorOverLifetimeModule vida = sistema.colorOverLifetime;
            vida.enabled = true;
            var degradado = new Gradient();
            degradado.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            vida.color = degradado;
            ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sistema.Play();
            return sistema;
        }

        // Círculo suave blanco; el color sale del vértice o del material.
        private static Texture2D CrearTextura()
        {
            var t = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "SensacionDisparo_Punto" };
            float c = (LadoTextura - 1) * 0.5f;
            var pixeles = new Color32[LadoTextura * LadoTextura];
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
            t.Apply();
            return t;
        }
    }
}

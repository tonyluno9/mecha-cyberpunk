using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1309: lo que se ve del rayo del rifle de plasma, todo creado por código: un núcleo blanco brillante y un halo cian más ancho (dos
    /// <see cref="LineRenderer"/> con textura de gradiente generada, aditivos y con el material URP de partículas), ancho que pulsa, zigzag leve con varios
    /// puntos, fundido rápido al terminar y chispas (un <see cref="ParticleSystem"/>) donde pega en una pared. Cada cuadro con rayo se llama a
    /// <see cref="Mostrar"/>; si deja de llamarse, el rayo se desvanece en <see cref="SegundosFundido"/>.
    /// </summary>
    public class EfectoRayoPlasma : MonoBehaviour
    {
        /// <summary>Lo que tarda el rayo en desvanecerse al terminar (s).</summary>
        public const float SegundosFundido = 0.15f;
        public const int PuntosMaximos = 24;
        private const float MetrosPorPunto = 1.6f;
        private const float AnchoNucleo = 0.35f;
        private const float AnchoHalo = 2.4f;
        private const float AmplitudZigzag = 0.09f;
        private const float PulsoRapido = 22f;
        private const float AlfaHalo = 0.55f;
        private const int AlturaTextura = 64;
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        private LineRenderer nucleo;
        private LineRenderer halo;
        private ParticleSystem chispas;
        private Texture2D textura;
        private Material materialNucleo;
        private Material materialHalo;
        private readonly Vector3[][] puntosPorCantidad = new Vector3[PuntosMaximos + 1][];
        private float alfa;
        private int ultimoCuadro = -1;
        private Vector3 ultimoOrigen;
        private Vector3 ultimoFin;
        private float anchoBase = 0.22f;
        private Color colorHalo = new Color(0.35f, 0.85f, 1f, 1f);
        private int semilla;

        /// <summary>0 a 1: qué tan visible está el rayo (1 con el rayo en pie, baja al fundirse).</summary>
        public float Visibilidad => alfa;

        /// <summary>True si el efecto dibuja algo ahora.</summary>
        public bool Visible => nucleo != null && nucleo.enabled;

        /// <summary>El sistema de chispas del impacto (para pruebas).</summary>
        public ParticleSystem Chispas => chispas;

        /// <summary>Crea el efecto como hijo de <paramref name="padre"/>. <paramref name="material"/> es el material aditivo de partículas (puede ser null).</summary>
        public static EfectoRayoPlasma Crear(Transform padre, Material material, Color color, float ancho)
        {
            GameObject g = new GameObject("RayoPlasmaEfecto");
            g.transform.SetParent(padre, false);
            EfectoRayoPlasma e = g.AddComponent<EfectoRayoPlasma>();
            e.Iniciar(material, color, ancho);
            return e;
        }

        private void Iniciar(Material material, Color color, float ancho)
        {
            colorHalo = color;
            anchoBase = Mathf.Max(0.01f, ancho);
            semilla = Random.Range(0, 1000);
            textura = TexturaGradiente();
            materialNucleo = CrearMaterial(material);
            materialHalo = CrearMaterial(material);
            nucleo = CrearLinea("Nucleo", materialNucleo, 1);
            halo = CrearLinea("Halo", materialHalo, 0);
            chispas = CrearChispas(material);
        }

        /// <summary>Textura de gradiente: brillante al centro y suave hacia los bordes (el eje V cruza el ancho de la línea).</summary>
        public static Texture2D TexturaGradiente()
        {
            Texture2D t = new Texture2D(4, AlturaTextura, TextureFormat.RGBA32, false)
            {
                name = "RayoPlasma_Gradiente",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixeles = new Color[t.width * t.height];
            for (int y = 0; y < t.height; y++)
            {
                float v = (y + 0.5f) / t.height * 2f - 1f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v)), 1.6f);
                for (int x = 0; x < t.width; x++)
                {
                    pixeles[y * t.width + x] = new Color(1f, 1f, 1f, a);
                }
            }
            t.SetPixels(pixeles);
            t.Apply(false, false);
            return t;
        }

        private Material CrearMaterial(Material origen)
        {
            Material m;
            if (origen != null)
            {
                m = new Material(origen);
            }
            else
            {
                Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                m = new Material(s != null ? s : Shader.Find("Sprites/Default"));
            }
            m.name = "RayoPlasma_Instancia";
            m.mainTexture = textura;
            if (m.HasProperty(BaseMap))
            {
                m.SetTexture(BaseMap, textura);
            }
            // Si se pone emisión, con flags None se pierde _EMISSION al reimportar: RealtimeEmissive.
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        private LineRenderer CrearLinea(string nombre, Material material, int orden)
        {
            LineRenderer l = new GameObject(nombre).AddComponent<LineRenderer>();
            l.transform.SetParent(transform, false);
            l.useWorldSpace = true;
            l.textureMode = LineTextureMode.Stretch;
            l.numCapVertices = 4;
            l.numCornerVertices = 3;
            l.alignment = LineAlignment.View;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.sharedMaterial = material;
            l.sortingOrder = orden;
            l.enabled = false;
            return l;
        }

        private ParticleSystem CrearChispas(Material material)
        {
            GameObject g = new GameObject("ChispasImpacto");
            g.transform.SetParent(transform, false);
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, colorHalo);
            main.gravityModifier = 0.6f;
            main.maxParticles = 80;
            ParticleSystem.EmissionModule emision = ps.emission;
            emision.rateOverTime = 70f;
            emision.enabled = false;
            ParticleSystem.ShapeModule forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Hemisphere;
            forma.radius = 0.08f;
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient gradiente = new Gradient();
            gradiente.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(colorHalo, 0.5f), new GradientColorKey(colorHalo, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            color.color = gradiente;
            ParticleSystem.SizeOverLifetimeModule tamano = ps.sizeOverLifetime;
            tamano.enabled = true;
            tamano.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            ParticleSystemRenderer r = g.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 2f;
            r.velocityScale = 0.05f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null)
            {
                r.sharedMaterial = new Material(material) { name = "RayoPlasma_Chispas" };
            }
            return ps;
        }

        /// <summary>Dibuja el rayo este cuadro de <paramref name="origen"/> a <paramref name="fin"/>; <paramref name="pegaEnPared"/> prende las chispas en el punto final.</summary>
        public void Mostrar(Vector3 origen, Vector3 fin, bool pegaEnPared)
        {
            ultimoCuadro = Time.frameCount;
            ultimoOrigen = origen;
            ultimoFin = fin;
            alfa = 1f;
            Dibujar();
            ParticleSystem.EmissionModule emision = chispas.emission;
            emision.enabled = pegaEnPared;
            if (pegaEnPared)
            {
                chispas.transform.position = fin;
                if (!chispas.isPlaying)
                {
                    chispas.Play();
                }
            }
        }

        private void LateUpdate()
        {
            // Si este cuadro nadie pidió Mostrar, el rayo se desvanece rápido.
            if (alfa > 0f && ultimoCuadro != Time.frameCount)
            {
                alfa = Mathf.Max(0f, alfa - Time.deltaTime / SegundosFundido);
                if (alfa <= 0f)
                {
                    Ocultar();
                    return;
                }
                Dibujar();
                ParticleSystem.EmissionModule emision = chispas.emission;
                emision.enabled = false;
            }
        }

        /// <summary>Apaga todo al instante (sin fundido).</summary>
        public void Ocultar()
        {
            alfa = 0f;
            if (nucleo != null)
            {
                nucleo.enabled = false;
                halo.enabled = false;
            }
            if (chispas != null)
            {
                ParticleSystem.EmissionModule emision = chispas.emission;
                emision.enabled = false;
            }
        }

        private void Dibujar()
        {
            float largo = Vector3.Distance(ultimoOrigen, ultimoFin);
            int n = Mathf.Clamp(Mathf.CeilToInt(largo / MetrosPorPunto) + 2, 3, PuntosMaximos);
            Vector3[] usados = puntosPorCantidad[n];
            if (usados == null)
            {
                usados = new Vector3[n];
                puntosPorCantidad[n] = usados;
            }
            ReglaZigzagRayo.Puntos(ultimoOrigen, ultimoFin, Time.time, AmplitudZigzag, semilla, usados);
            float pulso = 0.85f + 0.15f * Mathf.Sin(Time.time * PulsoRapido) + 0.05f * Mathf.Sin(Time.time * PulsoRapido * 2.7f);
            AplicarLinea(nucleo, usados, anchoBase * AnchoNucleo * pulso, new Color(1f, 1f, 1f, alfa));
            Color c = colorHalo;
            AplicarLinea(halo, usados, anchoBase * AnchoHalo * pulso, new Color(c.r, c.g, c.b, AlfaHalo * alfa));
        }

        private static void AplicarLinea(LineRenderer l, Vector3[] pts, float ancho, Color color)
        {
            l.enabled = true;
            l.positionCount = pts.Length;
            l.SetPositions(pts);
            l.widthMultiplier = ancho;
            l.startColor = color;
            l.endColor = color;
        }

        private void OnDestroy()
        {
            if (textura != null)
            {
                Destroy(textura);
            }
            if (materialNucleo != null)
            {
                Destroy(materialNucleo);
            }
            if (materialHalo != null)
            {
                Destroy(materialHalo);
            }
        }
    }
}

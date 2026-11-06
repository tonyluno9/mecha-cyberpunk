using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// El fondo vivo de la pantalla de inicio: una avenida de la megaciudad de noche, armada por código al despertar (edificios con
    /// ventanas encendidas, anuncios de neón que parpadean, pasos elevados, tráfico por la calle y por el aire, lluvia, niebla y halos).
    /// La cámara recorre la avenida muy despacio de ida y vuelta, se mece y sigue un poco al ratón (paralaje). Todo el azar sale de
    /// <see cref="semilla"/>, así que la ciudad es igual en cada arranque. No usa nada de la ciudad del juego.
    /// </summary>
    public class FondoCiudad3D : MonoBehaviour
    {
        private const int Variantes = 3;
        private const int TexturaAncho = 256;
        private const int TexturaAlto = 512;
        private const int ColumnasVentana = 8;
        private const int FilasVentana = 16;
        private const float MetrosPorTextura = 16f;
        private const float MetrosAltoTextura = 36f;
        private const float AnchoCalle = 23f;
        private const float ZMinimo = -110f;
        private const float ZMaximo = 460f;
        private const float AlturaBloqueMinima = 28f;
        private const float AlturaBloqueRango = 100f;
        private const float EmisionNeon = 2.6f;
        private const float EmisionVentana = 1.7f;

        [Header("Materiales (plantillas)")]
        [Tooltip("Material URP/Lit con emisión activada: de él salen todos los materiales de la ciudad.")]
        [SerializeField] private Material plantillaLit;
        [Tooltip("Material del shader Aditivo3D: de él salen la lluvia y los halos.")]
        [SerializeField] private Material plantillaAditivo;
        [SerializeField] private Camera camara;

        [Header("Ciudad")]
        [Tooltip("Semilla del azar: la misma ciudad en cada arranque.")]
        [SerializeField] private int semilla = 20871;
        [Min(0)] [Tooltip("Cuántos autos circulan por la calle.")] [SerializeField] private int autosCalle = 46;
        [Min(0)] [Tooltip("Cuántos vehículos vuelan entre los edificios.")] [SerializeField] private int autosAire = 26;
        [Min(0)] [Tooltip("Partículas de lluvia por segundo.")] [SerializeField] private float lluviaPorSegundo = 2600f;

        [Header("Cámara")]
        [Tooltip("Dónde empieza (m): x, altura y z del inicio del recorrido.")]
        [SerializeField] private Vector3 posicionCamara = new Vector3(1f, 21f, -30f);
        [Min(0f)] [Tooltip("Cuánto avanza por la avenida (m) antes de volver.")] [SerializeField] private float recorrido = 85f;
        [Min(1f)] [Tooltip("Segundos de una ida y vuelta.")] [SerializeField] private float segundosRecorrido = 150f;
        [Min(0f)] [Tooltip("Balanceo lateral de la cámara (m).")] [SerializeField] private float balanceoLateral = 2.6f;
        [Tooltip("Inclinación hacia abajo de la cámara (°).")] [SerializeField] private float cabeceoBase = 1.5f;
        [Min(0f)] [Tooltip("Cuánto sigue al ratón: giro horizontal y vertical máximos (°).")] [SerializeField] private Vector2 paralaje = new Vector2(3.2f, 1.6f);
        [Min(0.1f)] [Tooltip("Qué tan rápido alcanza la cámara al ratón (1/s).")] [SerializeField] private float suavizadoParalaje = 2.2f;

        private sealed class Vehiculo
        {
            public Transform raiz;
            public float velocidad;
            public float direccion;
        }

        private sealed class Parpadeo
        {
            public Material material;
            public Color color;
            public float fase;
            public float velocidad;
        }

        private sealed class Baliza
        {
            public GameObject objeto;
            public float fase;
        }

        private readonly List<Object> creados = new List<Object>();
        private readonly List<Material> materialesVentana = new List<Material>();
        private readonly List<Material> materialesNeon = new List<Material>();
        private readonly List<Vehiculo> vehiculos = new List<Vehiculo>();
        private readonly List<Parpadeo> parpadeos = new List<Parpadeo>();
        private readonly List<Baliza> balizas = new List<Baliza>();
        private readonly List<Transform> halosCamara = new List<Transform>();
        private readonly List<Light> luces = new List<Light>();
        private readonly List<float> intensidadesLuz = new List<float>();
        private System.Random azar;
        private Transform mundo;
        private Material materialHalo;
        private Material materialCalle;
        private Material materialOscuro;
        private Texture2D texturaHalo;
        private Mesh cajaUnidad;
        private Transform lluvia;
        private Vector2 mirada;
        private float reloj;
        private bool construido;

        public bool Construido => construido;
        public int Edificios { get; private set; }
        public int Anuncios { get; private set; }
        public int Vehiculos => vehiculos.Count;
        public Camera Camara => camara;
        public float Reloj => reloj;

        private void Awake()
        {
            if (plantillaLit == null || plantillaAditivo == null || camara == null)
            {
                Debug.LogError("[UI] FondoCiudad3D sin plantillas de material o sin cámara.", this);
                enabled = false;
                return;
            }
            Construir();
        }

        private void OnDestroy()
        {
            foreach (Object objeto in creados)
            {
                if (objeto != null)
                {
                    Destroy(objeto);
                }
            }
            creados.Clear();
        }

        public void Configurar(Material lit, Material aditivo, Camera camaraFondo)
        {
            plantillaLit = lit;
            plantillaAditivo = aditivo;
            camara = camaraFondo;
        }

        /// <summary>Arma la ciudad una sola vez (Awake la llama sola).</summary>
        public void Construir()
        {
            if (construido)
            {
                return;
            }
            construido = true;
            azar = new System.Random(semilla);
            mundo = new GameObject("Mundo").transform;
            mundo.SetParent(transform, false);
            cajaUnidad = Registrar(CrearCaja(Vector3.one, 1f, 1f, Vector2.zero));
            PrepararMateriales();
            ArmarAtmosfera();
            ArmarCalle();
            ArmarEdificios();
            ArmarPasosElevados();
            ArmarTrafico();
            ArmarLluvia();
            ArmarLuces();
            Debug.Log("[UI] Fondo del inicio: " + Edificios + " edificios, " + Anuncios + " anuncios, " + vehiculos.Count + " vehículos.");
        }

        private T Registrar<T>(T objeto) where T : Object
        {
            creados.Add(objeto);
            return objeto;
        }

        // ------------------------------------------------------------------ materiales

        private void PrepararMateriales()
        {
            Color[][] paletas =
            {
                new[] { new Color(0.40f, 0.90f, 1f), new Color(0.55f, 0.75f, 1f), new Color(1f, 0.80f, 0.5f) },
                new[] { new Color(1f, 0.38f, 0.85f), new Color(0.75f, 0.45f, 1f), new Color(0.45f, 0.9f, 1f) },
                new[] { new Color(1f, 0.74f, 0.38f), new Color(1f, 0.55f, 0.25f), new Color(0.6f, 0.85f, 1f) },
            };
            for (int i = 0; i < Variantes; i++)
            {
                Texture2D albedo;
                Texture2D emision;
                CrearTexturasVentanas(paletas[i], out albedo, out emision);
                Material m = NuevoLit(new Color(0.05f, 0.055f, 0.08f), Color.white * EmisionVentana, 0.35f);
                m.SetTexture("_BaseMap", albedo);
                m.SetTexture("_EmissionMap", emision);
                materialesVentana.Add(m);
            }
            Color[] neones = { UIInicio.Cian, UIInicio.Magenta, UIInicio.Naranja, new Color(0.55f, 1f, 0.45f), UIInicio.Blanco };
            foreach (Color color in neones)
            {
                materialesNeon.Add(NuevoLit(color * 0.03f, color * EmisionNeon, 0.5f));
            }
            materialOscuro = NuevoLit(new Color(0.03f, 0.03f, 0.045f), Color.black, 0.25f);
            materialCalle = NuevoLit(new Color(0.025f, 0.027f, 0.04f), Color.black, 0.93f);
            texturaHalo = Registrar(CrearTexturaHalo());
            materialHalo = Registrar(new Material(plantillaAditivo));
            materialHalo.mainTexture = texturaHalo;
            materialHalo.SetColor("_Color", Color.white);
        }

        private Material NuevoLit(Color baseColor, Color emision, float lisura)
        {
            Material m = Registrar(new Material(plantillaLit));
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Smoothness", lisura);
            m.SetFloat("_Metallic", 0.1f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision);
            return m;
        }

        private void CrearTexturasVentanas(Color[] paleta, out Texture2D albedo, out Texture2D emision)
        {
            albedo = Registrar(new Texture2D(TexturaAncho, TexturaAlto, TextureFormat.RGBA32, true));
            emision = Registrar(new Texture2D(TexturaAncho, TexturaAlto, TextureFormat.RGBA32, true));
            Color32[] pa = new Color32[TexturaAncho * TexturaAlto];
            Color32[] pe = new Color32[TexturaAncho * TexturaAlto];
            int celdaAncho = TexturaAncho / ColumnasVentana;
            int celdaAlto = TexturaAlto / FilasVentana;
            Color32 pared = new Color32(14, 15, 24, 255);
            Color32 vidrioApagado = new Color32(8, 10, 18, 255);
            for (int fila = 0; fila < FilasVentana; fila++)
            {
                // Hay pisos enteros encendidos (oficinas) y pisos casi apagados.
                double probabilidad = azar.NextDouble() < 0.18 ? 0.85 : 0.28;
                for (int col = 0; col < ColumnasVentana; col++)
                {
                    bool encendida = azar.NextDouble() < probabilidad;
                    Color luz = paleta[azar.Next(paleta.Length)] * (0.5f + (float)azar.NextDouble() * 0.5f);
                    for (int y = 0; y < celdaAlto; y++)
                    {
                        for (int x = 0; x < celdaAncho; x++)
                        {
                            bool vidrio = x >= 7 && x < celdaAncho - 7 && y >= 8 && y < celdaAlto - 7;
                            int indice = (fila * celdaAlto + y) * TexturaAncho + col * celdaAncho + x;
                            if (!vidrio)
                            {
                                pa[indice] = pared;
                                pe[indice] = new Color32(0, 0, 0, 255);
                            }
                            else if (encendida)
                            {
                                pa[indice] = (Color32)(luz * 0.3f);
                                pe[indice] = (Color32)luz;
                            }
                            else
                            {
                                pa[indice] = vidrioApagado;
                                pe[indice] = new Color32(0, 0, 0, 255);
                            }
                        }
                    }
                }
            }
            foreach (Texture2D t in new[] { albedo, emision })
            {
                t.wrapMode = TextureWrapMode.Repeat;
                t.filterMode = FilterMode.Bilinear;
                t.anisoLevel = 4;
            }
            albedo.SetPixels32(pa);
            albedo.Apply(true);
            emision.SetPixels32(pe);
            emision.Apply(true);
        }

        private static Texture2D CrearTexturaHalo()
        {
            const int lado = 64;
            Texture2D t = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color32[] p = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float dx = (x - (lado - 1) * 0.5f) / ((lado - 1) * 0.5f);
                    float dy = (y - (lado - 1) * 0.5f) / ((lado - 1) * 0.5f);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)), 2.4f);
                    byte b = (byte)Mathf.RoundToInt(a * 255f);
                    p[y * lado + x] = new Color32(255, 255, 255, b);
                }
            }
            t.SetPixels32(p);
            t.Apply(false, true);
            return t;
        }

        // ------------------------------------------------------------------ geometría

        // Caja con las UV en metros (una ventana siempre mide lo mismo) y sin la cara de abajo.
        private static Mesh CrearCaja(Vector3 tam, float metrosU, float metrosV, Vector2 desfase)
        {
            Vector3 h = tam * 0.5f;
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            void Cara(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, float anchoU, float altoV, bool techo)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                for (int k = 0; k < 4; k++) n.Add(normal);
                float u0 = techo ? 0.02f : desfase.x;
                float v0 = techo ? 0.02f : desfase.y;
                float u1 = techo ? 0.03f : desfase.x + anchoU / metrosU;
                float v1 = techo ? 0.03f : desfase.y + altoV / metrosV;
                uv.Add(new Vector2(u0, v0)); uv.Add(new Vector2(u1, v0)); uv.Add(new Vector2(u1, v1)); uv.Add(new Vector2(u0, v1));
                t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2);
            }
            Cara(new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z), Vector3.back, tam.x, tam.y, false);
            Cara(new Vector3(h.x, -h.y, h.z), new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z), Vector3.forward, tam.x, tam.y, false);
            Cara(new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, h.y, h.z), Vector3.left, tam.z, tam.y, false);
            Cara(new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, h.y, -h.z), Vector3.right, tam.z, tam.y, false);
            Cara(new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z), Vector3.up, tam.x, tam.z, true);
            Mesh m = new Mesh { name = "Inicio_Caja" };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        private GameObject Pieza(string nombre, Mesh malla, Material material, Vector3 posicion, Vector3 escala, Transform padre = null)
        {
            GameObject g = new GameObject(nombre, typeof(MeshFilter), typeof(MeshRenderer));
            g.transform.SetParent(padre != null ? padre : mundo, false);
            g.transform.localPosition = posicion;
            g.transform.localScale = escala;
            g.GetComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer r = g.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return g;
        }

        private GameObject Caja(string nombre, Material material, Vector3 posicion, Vector3 tamano, Transform padre = null)
        {
            return Pieza(nombre, cajaUnidad, material, posicion, tamano, padre);
        }

        // Cuadro de luz suave. Mira siempre a la cámara salvo los que van pegados al suelo.
        private GameObject Halo(Vector3 posicion, Vector2 tamano, Color color, bool mirarCamara, Transform padre = null)
        {
            Mesh m = Registrar(new Mesh { name = "Inicio_Halo" });
            m.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) });
            m.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            m.SetColors(new[] { color, color, color, color });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateBounds();
            GameObject g = Pieza("Halo", m, materialHalo, posicion, new Vector3(tamano.x, tamano.y, 1f), padre);
            if (mirarCamara)
            {
                halosCamara.Add(g.transform);
            }
            else
            {
                g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            return g;
        }

        private float Azar(float minimo, float maximo) => minimo + (float)azar.NextDouble() * (maximo - minimo);

        // ------------------------------------------------------------------ escena

        private void ArmarAtmosfera()
        {
            Color niebla = new Color(0.075f, 0.04f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = niebla;
            RenderSettings.fogDensity = 0.0072f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.04f, 0.1f);
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = niebla;
            camara.farClipPlane = 900f;
            // Resplandor del horizonte: dos halos enormes al fondo de la avenida y una luna.
            Halo(new Vector3(0f, 45f, 520f), new Vector2(520f, 230f), new Color(0.75f, 0.12f, 0.62f, 0.55f), false, mundo).transform.localRotation = Quaternion.identity;
            Halo(new Vector3(-90f, 85f, 515f), new Vector2(300f, 200f), new Color(0.1f, 0.65f, 0.95f, 0.45f), false, mundo).transform.localRotation = Quaternion.identity;
            Halo(new Vector3(120f, 150f, 540f), new Vector2(90f, 90f), new Color(0.9f, 0.9f, 1f, 0.5f), false, mundo).transform.localRotation = Quaternion.identity;
        }

        private void ArmarCalle()
        {
            float largo = ZMaximo - ZMinimo + 120f;
            float centroZ = (ZMaximo + ZMinimo) * 0.5f;
            Caja("Calle", materialCalle, new Vector3(0f, -0.25f, centroZ), new Vector3(AnchoCalle * 2f + 14f, 0.5f, largo));
            // Rayas de los carriles (neón tenue) y reflejos de los anuncios en el pavimento mojado.
            for (float z = ZMinimo; z < ZMaximo; z += 12f)
            {
                Caja("Raya", materialesNeon[4], new Vector3(0f, 0.02f, z), new Vector3(0.18f, 0.02f, 4f));
                Caja("RayaIzq", materialesNeon[0], new Vector3(-AnchoCalle * 0.5f, 0.02f, z + 3f), new Vector3(0.12f, 0.02f, 2f));
                Caja("RayaDer", materialesNeon[1], new Vector3(AnchoCalle * 0.5f, 0.02f, z + 3f), new Vector3(0.12f, 0.02f, 2f));
            }
        }

        private void ArmarEdificios()
        {
            foreach (int lado in new[] { -1, 1 })
            {
                float z = ZMinimo;
                while (z < ZMaximo)
                {
                    float ancho = Azar(16f, 34f);
                    // Bocacalle cada tanto.
                    if (Mathf.Abs(Mathf.Repeat(z, 120f) - 60f) < 12f)
                    {
                        z += 22f;
                        continue;
                    }
                    float profundo = Azar(22f, 40f);
                    float alto = AlturaBloqueMinima + Mathf.Pow((float)azar.NextDouble(), 1.8f) * AlturaBloqueRango;
                    float fachada = lado * (AnchoCalle + Azar(3f, 7f));
                    Edificio(new Vector3(fachada + lado * profundo * 0.5f, alto * 0.5f, z + ancho * 0.5f), new Vector3(profundo, alto, ancho), lado, true);
                    z += ancho + Azar(0.5f, 2.5f);
                }
                // Segunda fila, más alta, que asoma por encima de la primera.
                z = ZMinimo;
                while (z < ZMaximo)
                {
                    float ancho = Azar(30f, 54f);
                    float alto = Azar(80f, 200f);
                    float profundo = Azar(30f, 50f);
                    Edificio(new Vector3(lado * (AnchoCalle + 62f + profundo * 0.5f), alto * 0.5f, z + ancho * 0.5f), new Vector3(profundo, alto, ancho), lado, false);
                    z += ancho + Azar(0f, 6f);
                }
            }
            // El fondo de la avenida.
            for (float x = -150f; x < 150f; x += 38f)
            {
                float alto = Azar(90f, 230f);
                Edificio(new Vector3(x + 19f, alto * 0.5f, ZMaximo + 40f + Azar(0f, 40f)), new Vector3(36f, alto, 30f), 0, false);
            }
        }

        private void Edificio(Vector3 centro, Vector3 tamano, int lado, bool conAnuncios)
        {
            Mesh malla = Registrar(CrearCaja(tamano, MetrosPorTextura, MetrosAltoTextura, new Vector2(Azar(0f, 1f), Azar(0f, 1f))));
            Material material = materialesVentana[azar.Next(materialesVentana.Count)];
            Pieza("Edificio", malla, material, centro, Vector3.one);
            Edificios++;
            float techo = centro.y + tamano.y * 0.5f;
            if (tamano.y > 60f && azar.NextDouble() < 0.5)
            {
                float antena = Azar(6f, 16f);
                Caja("Antena", materialOscuro, new Vector3(centro.x, techo + antena * 0.5f, centro.z), new Vector3(0.5f, antena, 0.5f));
                GameObject baliza = Caja("Baliza", materialesNeon[2], new Vector3(centro.x, techo + antena + 0.4f, centro.z), Vector3.one * 0.9f);
                GameObject resplandor = Halo(new Vector3(centro.x, techo + antena + 0.4f, centro.z), Vector2.one * 10f, new Color(1f, 0.2f, 0.15f, 0.8f), true);
                resplandor.transform.SetParent(baliza.transform, true);
                balizas.Add(new Baliza { objeto = baliza, fase = Azar(0f, 6f) });
            }
            if (!conAnuncios || lado == 0)
            {
                return;
            }
            float caraX = centro.x - lado * tamano.x * 0.5f;
            int cantidad = azar.NextDouble() < 0.7 ? azar.Next(1, 3) : 0;
            for (int i = 0; i < cantidad; i++)
            {
                int color = azar.Next(0, 4);
                float z = centro.z + Azar(-tamano.z * 0.35f, tamano.z * 0.35f);
                float alto = Azar(5f, 14f);
                float y = Azar(7f, Mathf.Max(8f, Mathf.Min(tamano.y - 2f, 55f)));
                Vector3 pos;
                Vector3 medida;
                if (azar.NextDouble() < 0.6)
                {
                    // Anuncio de bandera: sobresale hacia la calle y se ve de frente desde la cámara.
                    medida = new Vector3(Azar(0.9f, 1.8f), alto * 0.6f, 0.3f);
                    pos = new Vector3(caraX - lado * medida.x * 0.5f, y, z);
                }
                else
                {
                    // Franja pegada a la fachada.
                    medida = new Vector3(0.3f, Azar(0.5f, 1f), Azar(6f, 14f));
                    pos = new Vector3(caraX - lado * 0.2f, y, z);
                }
                Material materialAnuncio = materialesNeon[color];
                if (azar.NextDouble() < 0.18)
                {
                    materialAnuncio = Registrar(new Material(materialAnuncio));
                    Color colorEmision = materialAnuncio.GetColor("_EmissionColor");
                    parpadeos.Add(new Parpadeo { material = materialAnuncio, color = colorEmision, fase = Azar(0f, 20f), velocidad = Azar(0.6f, 2.2f) });
                }
                Caja("Anuncio", materialAnuncio, pos, medida);
                Color luz = ColorNeon(color);
                Halo(pos, new Vector2(Mathf.Max(medida.x, 2f) * 3f, medida.y * 1.5f + 4f), UIInicio.ConAlfa(luz, 0.2f), true);
                if (pos.y > 7f)
                {
                    Halo(new Vector3(pos.x * 0.45f, 0.06f, pos.z), new Vector2(5f, Mathf.Min(pos.y * 1.1f, 22f)), UIInicio.ConAlfa(luz, 0.16f), false);
                }
                Anuncios++;
            }
        }

        private static Color ColorNeon(int indice)
        {
            switch (indice)
            {
                case 0: return UIInicio.Cian;
                case 1: return UIInicio.Magenta;
                case 2: return UIInicio.Naranja;
                default: return new Color(0.55f, 1f, 0.45f);
            }
        }

        private void ArmarPasosElevados()
        {
            foreach (float z in new[] { 105f, 235f, 360f })
            {
                float y = Azar(20f, 28f);
                float largo = AnchoCalle * 2f + 30f;
                Caja("Paso", materialOscuro, new Vector3(0f, y, z), new Vector3(largo, 2.4f, 7f));
                Caja("PasoNeon", materialesNeon[azar.Next(0, 3)], new Vector3(0f, y - 1.35f, z - 3.4f), new Vector3(largo, 0.25f, 0.2f));
                Caja("PasoNeon", materialesNeon[azar.Next(0, 3)], new Vector3(0f, y + 1.3f, z - 3.4f), new Vector3(largo, 0.2f, 0.2f));
                foreach (int lado in new[] { -1, 1 })
                {
                    Caja("Pilar", materialOscuro, new Vector3(lado * (AnchoCalle + 2f), y * 0.5f, z), new Vector3(2.4f, y, 5f));
                }
                Halo(new Vector3(0f, y - 2f, z - 4f), new Vector2(largo * 0.9f, 6f), new Color(0.2f, 0.7f, 1f, 0.2f), true);
            }
        }

        private void ArmarTrafico()
        {
            float[] carriles = { -17f, -10.5f, -4f, 4f, 10.5f, 17f };
            for (int i = 0; i < autosCalle; i++)
            {
                float carril = carriles[azar.Next(carriles.Length)];
                float direccion = carril < 0f ? -1f : 1f;
                Vehiculo v = CrearAuto(new Vector3(carril, 0.55f, Azar(ZMinimo, ZMaximo)), direccion, Azar(6f, 13f), false);
                vehiculos.Add(v);
            }
            for (int i = 0; i < autosAire; i++)
            {
                float direccion = azar.NextDouble() < 0.5 ? 1f : -1f;
                Vehiculo v = CrearAuto(new Vector3((azar.NextDouble() < 0.5 ? -1f : 1f) * Azar(9f, AnchoCalle + 3f), Azar(36f, 85f), Azar(ZMinimo, ZMaximo)), direccion, Azar(14f, 34f), true);
                vehiculos.Add(v);
            }
        }

        private Vehiculo CrearAuto(Vector3 posicion, float direccion, float velocidad, bool aereo)
        {
            Transform raiz = new GameObject(aereo ? "VehiculoAereo" : "Auto").transform;
            raiz.SetParent(mundo, false);
            raiz.localPosition = posicion;
            Caja("Cuerpo", materialOscuro, Vector3.zero, aereo ? new Vector3(1.6f, 0.5f, 3.6f) : new Vector3(1.8f, 0.7f, 4.2f), raiz);
            Color delante = aereo ? ColorNeon(azar.Next(0, 3)) : new Color(1f, 0.95f, 0.8f);
            Color atras = aereo ? delante : new Color(1f, 0.1f, 0.08f);
            float medioLargo = aereo ? 1.8f : 2.1f;
            Material mDelante = aereo ? materialesNeon[azar.Next(0, 3)] : materialesNeon[4];
            Caja("Faro", mDelante, new Vector3(0f, 0.05f, direccion * medioLargo), new Vector3(1.2f, 0.18f, 0.08f), raiz);
            Caja("Piloto", materialesNeon[1], new Vector3(0f, 0.05f, -direccion * medioLargo), new Vector3(1.2f, 0.16f, 0.08f), raiz);
            Halo(new Vector3(0f, 0.1f, direccion * (medioLargo + 0.4f)), Vector2.one * (aereo ? 5f : 3.6f), UIInicio.ConAlfa(delante, 0.55f), true, raiz);
            Halo(new Vector3(0f, 0.1f, -direccion * (medioLargo + 0.4f)), Vector2.one * (aereo ? 4f : 2.8f), UIInicio.ConAlfa(atras, 0.5f), true, raiz);
            if (aereo)
            {
                Caja("Estela", materialesNeon[azar.Next(0, 3)], new Vector3(0f, 0f, -direccion * (medioLargo + 3.5f)), new Vector3(0.18f, 0.12f, 6f), raiz);
            }
            raiz.localRotation = Quaternion.identity;
            return new Vehiculo { raiz = raiz, velocidad = velocidad, direccion = direccion };
        }

        private void ArmarLluvia()
        {
            GameObject g = new GameObject("Lluvia", typeof(ParticleSystem));
            g.transform.SetParent(mundo, false);
            lluvia = g.transform;
            lluvia.rotation = Quaternion.Euler(90f, 0f, 0f);
            ParticleSystem ps = g.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.05f;
            main.startSpeed = 42f;
            main.startSize = 0.05f;
            main.startColor = new Color(0.65f, 0.82f, 1f, 0.5f);
            main.maxParticles = 6000;
            ParticleSystem.EmissionModule emision = ps.emission;
            emision.rateOverTime = lluviaPorSegundo;
            ParticleSystem.ShapeModule forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(70f, 70f, 1f);
            ParticleSystemRenderer render = g.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.Stretch;
            render.velocityScale = 0.035f;
            render.lengthScale = 1f;
            Material materialLluvia = Registrar(new Material(plantillaAditivo));
            materialLluvia.mainTexture = texturaHalo;
            materialLluvia.SetColor("_Color", new Color(0.8f, 0.9f, 1f, 0.9f));
            render.sharedMaterial = materialLluvia;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }

        private void ArmarLuces()
        {
            Color[] colores = { UIInicio.Cian, UIInicio.Magenta, UIInicio.Naranja, UIInicio.Cian, UIInicio.Magenta, UIInicio.Naranja };
            for (int i = 0; i < colores.Length; i++)
            {
                GameObject g = new GameObject("LuzNeon" + i, typeof(Light));
                g.transform.SetParent(mundo, false);
                g.transform.localPosition = new Vector3((i % 2 == 0 ? -1 : 1) * (AnchoCalle - 3f), Azar(6f, 16f), 20f + i * 55f);
                Light luz = g.GetComponent<Light>();
                luz.type = LightType.Point;
                luz.color = colores[i];
                luz.range = 55f;
                luz.intensity = 14f;
                luz.shadows = LightShadows.None;
                luces.Add(luz);
                intensidadesLuz.Add(luz.intensity);
            }
        }

        // ------------------------------------------------------------------ animación

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            reloj += dt;
            MoverCamara(dt);
            MoverTrafico(dt);
            Parpadear();
        }

        private void LateUpdate()
        {
            if (camara == null)
            {
                return;
            }
            Quaternion giro = camara.transform.rotation;
            foreach (Transform halo in halosCamara)
            {
                if (halo != null)
                {
                    halo.rotation = giro;
                }
            }
            if (lluvia != null)
            {
                lluvia.position = camara.transform.position + new Vector3(0f, 26f, 28f);
            }
        }

        private void MoverCamara(float dt)
        {
            float avance = 0.5f - 0.5f * Mathf.Cos(reloj * Mathf.PI * 2f / segundosRecorrido);
            Vector2 objetivo = Vector2.zero;
            Mouse raton = Mouse.current;
            if (raton != null && Screen.width > 0 && Screen.height > 0)
            {
                Vector2 p = raton.position.ReadValue();
                objetivo = new Vector2(Mathf.Clamp(p.x / Screen.width * 2f - 1f, -1f, 1f), Mathf.Clamp(p.y / Screen.height * 2f - 1f, -1f, 1f));
            }
            mirada = Vector2.Lerp(mirada, objetivo, 1f - Mathf.Exp(-suavizadoParalaje * dt));
            Vector3 posicion = posicionCamara + new Vector3(Mathf.Sin(reloj * 0.11f) * balanceoLateral, Mathf.Sin(reloj * 0.17f) * 0.6f, avance * recorrido);
            float guinada = Mathf.Sin(reloj * 0.13f) * 3f + mirada.x * paralaje.x;
            float cabeceo = cabeceoBase + Mathf.Sin(reloj * 0.09f) * 1.2f - mirada.y * paralaje.y;
            float alabeo = Mathf.Sin(reloj * 0.07f) * 0.9f;
            camara.transform.SetPositionAndRotation(posicion, Quaternion.Euler(cabeceo, guinada, alabeo));
        }

        private void MoverTrafico(float dt)
        {
            foreach (Vehiculo v in vehiculos)
            {
                Vector3 p = v.raiz.localPosition;
                p.z += v.direccion * v.velocidad * dt;
                if (p.z > ZMaximo)
                {
                    p.z = ZMinimo;
                }
                else if (p.z < ZMinimo)
                {
                    p.z = ZMaximo;
                }
                v.raiz.localPosition = p;
            }
        }

        private void Parpadear()
        {
            foreach (Parpadeo p in parpadeos)
            {
                float t = reloj * p.velocidad + p.fase;
                float onda = Mathf.Sin(t * 6f) * Mathf.Sin(t * 2.3f + 1f);
                float nivel = onda > -0.55f ? 1f : 0.08f;
                p.material.SetColor("_EmissionColor", p.color * nivel);
            }
            foreach (Baliza b in balizas)
            {
                b.objeto.SetActive(Mathf.Repeat(reloj + b.fase, 2.4f) < 0.45f);
            }
            for (int i = 0; i < luces.Count; i++)
            {
                luces[i].intensity = intensidadesLuz[i] * (0.9f + 0.1f * Mathf.Sin(reloj * (0.7f + i * 0.31f) + i));
            }
        }
    }
}

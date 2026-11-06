using UnityEngine;

namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>
    /// Nubes y estrellas del cielo (T-635): dos esferas grandes sin colisión que siguen a la cámara, con texturas hechas por código
    /// (ruido sin costuras para las nubes y puntos para las estrellas) y el shader MechaCyberpunk/CieloTextura (en Resources, para que
    /// entre en las builds). Las crea y las mueve <see cref="CicloDiaNoche"/>; si falta el shader no hace nada.
    /// </summary>
    public sealed class CieloTexturizado
    {
        private const string RutaShader = "Shaders/CieloTextura";
        private const float RadioNubes = 340f;
        private const float RadioEstrellas = 350f;
        private const int Lado = 512;
        private const int CantidadEstrellas = 700;
        // Repeticiones de la textura por unidad del plano de proyección (ver CieloTextura.shader) y movimiento de las nubes (uv/s).
        private const float EscalaNubes = 0.55f;
        private const float EscalaEstrellas = 1.6f;
        // Ruido por encima de esto es nube (0 a 1): más alto, cielo más despejado.
        private const float UmbralNubes = 0.52f;
        // Qué tan rápido pasa de cielo a nube: menos, bordes más suaves.
        private const float ContrasteNubes = 2.2f;
        private static readonly Vector4 MovimientoNubes = new Vector4(0.003f, 0.0015f, 0f, 0f);
        // Orden de dibujo: estrellas, luego el domo de nubes de noche (NubesNoche, Transparent-150) y encima estas nubes.
        private const int ColaEstrellas = 2840;
        private const int ColaNubes = 2860;
        private static readonly int IdColor = Shader.PropertyToID("_Color");
        private static readonly int IdEscala = Shader.PropertyToID("_Escala");
        private static readonly int IdDesplazar = Shader.PropertyToID("_Desplazar");

        private GameObject raiz;
        private Material materialNubes;
        private Material materialEstrellas;
        private Texture2D texturaNubes;
        private Texture2D texturaEstrellas;

        public bool Activo => raiz != null;
        public Color ColorNubes => materialNubes != null ? materialNubes.GetColor(IdColor) : default;
        public Color ColorEstrellas => materialEstrellas != null ? materialEstrellas.GetColor(IdColor) : default;
        public Vector3 Posicion => raiz != null ? raiz.transform.position : Vector3.zero;

        public static CieloTexturizado Crear()
        {
            Shader shader = Resources.Load<Shader>(RutaShader);
            if (shader == null)
            {
                Debug.LogWarning("[Mundo] No encuentro el shader del cielo con textura (Resources/" + RutaShader + ").");
                return null;
            }
            CieloTexturizado cielo = new CieloTexturizado();
            cielo.Armar(shader);
            return cielo;
        }

        private void Armar(Shader shader)
        {
            raiz = new GameObject("CieloTexturizado");
            texturaNubes = HacerNubes();
            texturaEstrellas = HacerEstrellas();
            materialNubes = new Material(shader) { name = "CieloNubes (hora)", mainTexture = texturaNubes };
            materialEstrellas = new Material(shader) { name = "CieloEstrellas (hora)", mainTexture = texturaEstrellas };
            materialNubes.SetFloat(IdEscala, EscalaNubes);
            materialNubes.SetVector(IdDesplazar, MovimientoNubes);
            materialEstrellas.SetFloat(IdEscala, EscalaEstrellas);
            materialEstrellas.SetVector(IdDesplazar, Vector4.zero);
            materialNubes.renderQueue = ColaNubes;
            materialEstrellas.renderQueue = ColaEstrellas;
            HacerEsfera("Nubes", materialNubes, RadioNubes);
            HacerEsfera("Estrellas", materialEstrellas, RadioEstrellas);
        }

        private void HacerEsfera(string nombre, Material material, float radio)
        {
            GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            esfera.name = nombre;
            // Se apaga antes de destruirlo: Destroy espera al final del cuadro y en ese cuadro la esfera de 350 m empujaba
            // fuera de ella al CharacterController de la cámara (la cámara libre empezaba 350 m bajo el piso).
            Collider colision = esfera.GetComponent<Collider>();
            colision.enabled = false;
            DestruirObjeto(colision);
            esfera.transform.SetParent(raiz.transform, false);
            esfera.transform.localScale = Vector3.one * radio * 2f;
            MeshRenderer render = esfera.GetComponent<MeshRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
        }

        /// <summary>Pone el cielo en la cámara y les da su color de esta hora.</summary>
        public void Actualizar(Camera camara, float pesoNoche)
        {
            if (raiz == null)
            {
                return;
            }
            if (camara != null)
            {
                raiz.transform.position = camara.transform.position;
            }
            materialNubes.SetColor(IdColor, CalculoCieloTextura.ColorNubes(pesoNoche));
            materialEstrellas.SetColor(IdColor, new Color(1f, 1f, 1f, CalculoCieloTextura.OpacidadEstrellas(pesoNoche)));
        }

        public void Destruir()
        {
            DestruirObjeto(raiz);
            DestruirObjeto(materialNubes);
            DestruirObjeto(materialEstrellas);
            DestruirObjeto(texturaNubes);
            DestruirObjeto(texturaEstrellas);
        }

        private static void DestruirObjeto(Object objeto)
        {
            if (objeto == null)
            {
                return;
            }
            // En el editor sin jugar (capturas S2n del cielo) Destroy no se permite.
            if (Application.isPlaying)
            {
                Object.Destroy(objeto);
            }
            else
            {
                Object.DestroyImmediate(objeto);
            }
        }

        // T-806: ruido que se repite en los dos ejes (la textura se proyecta sobre un plano y se repite): ruido de valor con su red de
        // puntos al azar cerrada sobre sí misma, en tres octavas.
        private static Texture2D HacerNubes()
        {
            System.Random azar = new System.Random(806);
            int[] periodos = { 4, 8, 16, 32 };
            float[] pesos = { 0.5f, 0.27f, 0.15f, 0.08f };
            float[][] redes = new float[periodos.Length][];
            for (int o = 0; o < periodos.Length; o++)
            {
                redes[o] = Red(azar, periodos[o]);
            }
            Color32[] pixeles = new Color32[Lado * Lado];
            for (int y = 0; y < Lado; y++)
            {
                for (int x = 0; x < Lado; x++)
                {
                    float n = 0f;
                    for (int o = 0; o < redes.Length; o++)
                    {
                        n += pesos[o] * Valor(redes[o], periodos[o], (float)x / Lado * periodos[o], (float)y / Lado * periodos[o]);
                    }
                    float densidad = Mathf.Clamp01((n - UmbralNubes) * ContrasteNubes);
                    pixeles[y * Lado + x] = new Color32(255, 255, 255, (byte)(densidad * 255f));
                }
            }
            return Textura(pixeles);
        }

        private static float[] Red(System.Random azar, int periodo)
        {
            float[] red = new float[periodo * periodo];
            for (int i = 0; i < red.Length; i++)
            {
                red[i] = (float)azar.NextDouble();
            }
            return red;
        }

        private static float Valor(float[] red, int periodo, float x, float y)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = Mathf.SmoothStep(0f, 1f, x - x0);
            float fy = Mathf.SmoothStep(0f, 1f, y - y0);
            float Punto(int px, int py) => red[((py % periodo + periodo) % periodo) * periodo + (px % periodo + periodo) % periodo];
            float abajo = Mathf.Lerp(Punto(x0, y0), Punto(x0 + 1, y0), fx);
            float arriba = Mathf.Lerp(Punto(x0, y0 + 1), Punto(x0 + 1, y0 + 1), fx);
            return Mathf.Lerp(abajo, arriba, fy);
        }

        private static Texture2D HacerEstrellas()
        {
            Color32[] pixeles = new Color32[Lado * Lado];
            System.Random azar = new System.Random(635);
            for (int i = 0; i < CantidadEstrellas; i++)
            {
                int x = azar.Next(Lado);
                int y = azar.Next(Lado);
                byte brillo = (byte)azar.Next(110, 256);
                // Un punto de 2x2 (la textura de estrellas va sin mipmaps: de lejos no se borran).
                Color32 estrella = new Color32(255, 250, 235, brillo);
                pixeles[y * Lado + x] = estrella;
                pixeles[y * Lado + (x + 1) % Lado] = estrella;
                pixeles[((y + 1) % Lado) * Lado + x] = estrella;
                pixeles[((y + 1) % Lado) * Lado + (x + 1) % Lado] = estrella;
            }
            return Textura(pixeles, false);
        }

        private static Texture2D Textura(Color32[] pixeles, bool mipmaps = true)
        {
            Texture2D textura = new Texture2D(Lado, Lado, TextureFormat.RGBA32, mipmaps)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
            textura.SetPixels32(pixeles);
            textura.Apply(mipmaps);
            return textura;
        }
    }
}

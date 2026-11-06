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
        private const int Ancho = 512;
        private const int Alto = 256;
        private const int CantidadEstrellas = 900;
        private const float EscalaNube = 2.2f;
        private static readonly int IdColor = Shader.PropertyToID("_Color");

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
            Object.Destroy(colision);
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
            if (raiz != null)
            {
                Object.Destroy(raiz);
            }
            DestruirObjeto(materialNubes);
            DestruirObjeto(materialEstrellas);
            DestruirObjeto(texturaNubes);
            DestruirObjeto(texturaEstrellas);
        }

        private static void DestruirObjeto(Object objeto)
        {
            if (objeto != null)
            {
                Object.Destroy(objeto);
            }
        }

        // Ruido que cierra la vuelta (se recorre un círculo en el espacio del ruido): sin costura a los lados.
        private static Texture2D HacerNubes()
        {
            Color32[] pixeles = new Color32[Ancho * Alto];
            for (int y = 0; y < Alto; y++)
            {
                float v = (float)y / Alto;
                for (int x = 0; x < Ancho; x++)
                {
                    float angulo = (float)x / Ancho * Mathf.PI * 2f;
                    float cx = Mathf.Cos(angulo) * EscalaNube;
                    float cz = Mathf.Sin(angulo) * EscalaNube;
                    float n = 0.6f * Mathf.PerlinNoise(cx + 50f + v * 3f, cz + 50f + v * 2f)
                        + 0.3f * Mathf.PerlinNoise(cx * 2.3f + 90f, cz * 2.3f + 40f + v * 5f)
                        + 0.1f * Mathf.PerlinNoise(cx * 5.1f + 10f, cz * 5.1f + 70f + v * 9f);
                    float densidad = Mathf.Clamp01((n - 0.5f) * 3f);
                    byte a = (byte)(densidad * 255f);
                    pixeles[y * Ancho + x] = new Color32(255, 255, 255, a);
                }
            }
            return Textura(pixeles);
        }

        private static Texture2D HacerEstrellas()
        {
            Color32[] pixeles = new Color32[Ancho * Alto];
            System.Random azar = new System.Random(635);
            for (int i = 0; i < CantidadEstrellas; i++)
            {
                int x = azar.Next(Ancho);
                int y = azar.Next(Alto / 4, Alto);
                byte brillo = (byte)azar.Next(120, 256);
                // Dos píxeles seguidos: la esfera las estira hacia los polos y así se ven como puntos.
                pixeles[y * Ancho + x] = new Color32(255, 250, 235, brillo);
                pixeles[y * Ancho + (x + 1) % Ancho] = new Color32(255, 250, 235, (byte)(brillo / 2));
            }
            return Textura(pixeles);
        }

        private static Texture2D Textura(Color32[] pixeles)
        {
            Texture2D textura = new Texture2D(Ancho, Alto, TextureFormat.RGBA32, true)
            {
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            textura.SetPixels32(pixeles);
            textura.Apply(true);
            return textura;
        }
    }
}

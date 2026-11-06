using UnityEngine;

namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>
    /// El icono de teléfono que flota sobre un civil mientras llama a la policía (T-663): un cartel que mira a la cámara y late un poco.
    /// El dibujo se pinta por código (un celular con ondas, en un círculo) y el material sale de Resources (lo crea CreadorTestigos).
    /// </summary>
    public class IndicadorLlamada : MonoBehaviour
    {
        public const string NombreMaterial = "Trafico_IconoLlamada";
        private const int Lado = 64;
        private const float Tamano = 0.6f;
        private const float AmplitudLatido = 0.12f;
        private float escalaBase;
        private const float VelocidadLatido = 6f;
        private static Texture2D textura;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => textura = null;

        /// <summary>Crea el icono como hijo de <paramref name="padre"/>, <paramref name="margen"/> m sobre lo más alto de su modelo.</summary>
        public static IndicadorLlamada Crear(Transform padre, float margen)
        {
            GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Quad);
            objeto.name = "IconoLlamada";
            Collider collider = objeto.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            objeto.transform.SetParent(padre, false);
            float techo = padre.position.y;
            foreach (Renderer r in padre.GetComponentsInChildren<Renderer>())
            {
                techo = Mathf.Max(techo, r.bounds.max.y);
            }
            objeto.transform.position = new Vector3(padre.position.x, techo + margen, padre.position.z);
            float escalaPadre = Mathf.Max(0.0001f, padre.lossyScale.y);
            objeto.transform.localScale = Vector3.one * (Tamano / escalaPadre);
            MeshRenderer dibujo = objeto.GetComponent<MeshRenderer>();
            dibujo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dibujo.receiveShadows = false;
            Material plantilla = Resources.Load<Material>(NombreMaterial);
            Material material = plantilla != null ? new Material(plantilla) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.mainTexture = Textura();
            dibujo.sharedMaterial = material;
            IndicadorLlamada indicador = objeto.AddComponent<IndicadorLlamada>();
            indicador.escalaBase = objeto.transform.localScale.x;
            return indicador;
        }

        private void LateUpdate()
        {
            Camera camara = Camera.main;
            if (camara != null)
            {
                transform.rotation = camara.transform.rotation;
            }
            float latido = 1f + Mathf.Sin(Time.time * VelocidadLatido) * AmplitudLatido;
            transform.localScale = Vector3.one * escalaBase * latido;
        }

        /// <summary>La textura del icono (se pinta una vez).</summary>
        public static Texture2D Textura()
        {
            if (textura != null)
            {
                return textura;
            }
            textura = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "IconoLlamada" };
            Color[] pixeles = new Color[Lado * Lado];
            Vector2 centro = new Vector2(Lado / 2f - 0.5f, Lado / 2f - 0.5f);
            Color fondo = new Color(0.05f, 0.07f, 0.14f, 0.92f);
            Color borde = new Color(0.1f, 0.85f, 1f, 1f);
            Color cuerpo = new Color(0.93f, 0.96f, 1f, 1f);
            Color pantalla = new Color(0.1f, 0.5f, 0.75f, 1f);
            Color onda = new Color(1f, 0.8f, 0.15f, 1f);
            for (int y = 0; y < Lado; y++)
            {
                for (int x = 0; x < Lado; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float r = Vector2.Distance(p, centro);
                    Color c = Color.clear;
                    if (r <= Lado / 2f - 1f)
                    {
                        c = r >= Lado / 2f - 5f ? borde : fondo;
                    }
                    if (r < Lado / 2f - 5f)
                    {
                        // El celular, un poco a la izquierda para dejar sitio a las ondas.
                        if (x >= 18 && x <= 36 && y >= 14 && y <= 50)
                        {
                            c = (x >= 21 && x <= 33 && y >= 19 && y <= 44) ? pantalla : cuerpo;
                        }
                        // Dos ondas de señal a la derecha.
                        Vector2 origen = new Vector2(37f, 40f);
                        float d = Vector2.Distance(p, origen);
                        bool derecha = x > 38 && y > 30 && y < 56;
                        if (derecha && ((d > 9f && d < 12f) || (d > 17f && d < 20f)))
                        {
                            c = onda;
                        }
                    }
                    pixeles[y * Lado + x] = c;
                }
            }
            textura.SetPixels(pixeles);
            textura.Apply(false, true);
            return textura;
        }
    }
}

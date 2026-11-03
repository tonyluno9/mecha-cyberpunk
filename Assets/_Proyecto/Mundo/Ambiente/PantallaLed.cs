using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T-544: pantalla LED de anuncios (en E03, CY02, N08 y los pisos T22 de las torres). Cambia de anuncio cada
    /// <see cref="segundosMinimos"/> a <see cref="segundosMaximos"/> s y parpadea un poco, como pantalla barata. Usa un MaterialPropertyBlock:
    /// todas comparten el mismo material.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class PantallaLed : MonoBehaviour
    {
        private static readonly int IdBaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        [Tooltip("Anuncios que rota (el primero sale al empezar, según la fase).")]
        [SerializeField] private Texture2D[] anuncios = new Texture2D[0];
        [Tooltip("Mínimo de segundos con un anuncio (s).")]
        [Min(0.5f)] [SerializeField] private float segundosMinimos = 8f;
        [Tooltip("Máximo de segundos con un anuncio (s).")]
        [Min(0.5f)] [SerializeField] private float segundosMaximos = 12f;
        [Tooltip("Brillo de la pantalla (1 = el color de la imagen; más de 1 hace bloom).")]
        [Min(0f)] [SerializeField] private float brillo = 1.6f;
        [Tooltip("Cuánto baja el brillo al parpadear (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float parpadeo = 0.12f;
        [Tooltip("Fase propia (0 a 1): cada pantalla cambia en su momento.")]
        [Range(0f, 1f)] [SerializeField] private float fase;

        private Renderer pantalla;
        private MaterialPropertyBlock bloque;
        private int indice;
        private float siguiente;
        private System.Random azar;

        public int Indice => indice;
        public int Cantidad => anuncios.Length;

        public void Configurar(Texture2D[] nuevos, float nuevaFase)
        {
            anuncios = nuevos ?? new Texture2D[0];
            fase = Mathf.Repeat(nuevaFase, 1f);
        }

        private void Awake()
        {
            pantalla = GetComponent<Renderer>();
            bloque = new MaterialPropertyBlock();
            azar = new System.Random(Mathf.RoundToInt(fase * 100000f) + GetInstanceID());
            indice = anuncios.Length > 0 ? Mathf.FloorToInt(fase * anuncios.Length) % anuncios.Length : 0;
            siguiente = Time.time + Mathf.Lerp(segundosMinimos, segundosMaximos, fase);
            Aplicar(1f);
        }

        private void Update()
        {
            if (anuncios.Length == 0)
            {
                return;
            }
            if (Time.time >= siguiente)
            {
                indice = (indice + 1) % anuncios.Length;
                siguiente = Time.time + Mathf.Lerp(segundosMinimos, segundosMaximos, (float)azar.NextDouble());
            }
            if (!pantalla.isVisible && !Application.isBatchMode)
            {
                return; // sin verse no hace falta tocar el material
            }
            // Parpadeo leve: un zumbido lento y, de vez en cuando, un bajón corto.
            float t = Time.time * 7f + fase * 50f;
            float bajon = Mathf.PerlinNoise(t, fase * 13f) > 0.82f ? parpadeo : parpadeo * 0.25f * (0.5f + 0.5f * Mathf.Sin(t * 3.1f));
            Aplicar(1f - bajon);
        }

        private void Aplicar(float factor)
        {
            if (pantalla == null || anuncios.Length == 0)
            {
                return;
            }
            pantalla.GetPropertyBlock(bloque);
            bloque.SetTexture(IdBaseMap, anuncios[indice]);
            bloque.SetColor(IdBaseColor, Color.white * (brillo * factor));
            pantalla.SetPropertyBlock(bloque);
        }
    }
}

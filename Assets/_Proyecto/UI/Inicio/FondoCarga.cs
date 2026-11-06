using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// El fondo animado de la pantalla de carga, en 2D: estelas de neón que cruzan la pantalla a distintas velocidades, dos manchas de
    /// luz que respiran, una franja de barrido que baja despacio y una rejilla de líneas finas. Todo con imágenes del canvas, sin cámara,
    /// para que siga moviéndose aunque la escena de abajo se esté cargando.
    /// </summary>
    public class FondoCarga : MonoBehaviour
    {
        public const int CantidadEstelas = 34;
        private const float AnchoPantalla = 1920f;
        private const float AltoPantalla = 1080f;
        private const float SegundosBarrido = 7f;

        private sealed class Estela
        {
            public RectTransform rect;
            public float velocidad;
            public float largo;
        }

        private readonly Estela[] estelas = new Estela[CantidadEstelas];
        private RectTransform mancha1;
        private RectTransform mancha2;
        private RectTransform barrido;
        private float tiempo;

        public static FondoCarga Crear(Transform padre)
        {
            RectTransform raiz = UIInicio.Nodo(padre, "FondoCarga");
            UIInicio.Estirar(raiz);
            FondoCarga fondo = raiz.gameObject.AddComponent<FondoCarga>();
            System.Random azar = new System.Random(77);
            Image noche = UIInicio.Imagen(raiz, "Noche", UIInicio.Noche);
            UIInicio.Estirar(noche.rectTransform);

            Image m1 = UIInicio.Imagen(raiz, "ManchaMagenta", UIInicio.ConAlfa(UIInicio.Magenta, 0.30f), SpritesInicio.Brillo, true);
            UIInicio.Punto(m1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-380f, 60f), new Vector2(1300f, 1300f));
            fondo.mancha1 = m1.rectTransform;
            Image m2 = UIInicio.Imagen(raiz, "ManchaCian", UIInicio.ConAlfa(UIInicio.Cian, 0.24f), SpritesInicio.Brillo, true);
            UIInicio.Punto(m2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(520f, -80f), new Vector2(1100f, 1100f));
            fondo.mancha2 = m2.rectTransform;

            Color[] colores = { UIInicio.Cian, UIInicio.Magenta, UIInicio.Naranja };
            for (int i = 0; i < CantidadEstelas; i++)
            {
                float largo = 160f + (float)azar.NextDouble() * 620f;
                Color color = UIInicio.ConAlfa(colores[azar.Next(colores.Length)], 0.18f + (float)azar.NextDouble() * 0.5f);
                Image e = UIInicio.Imagen(raiz, "Estela" + i, color, SpritesInicio.DegradadoH, true);
                float altura = 2f + (float)azar.NextDouble() * 4f;
                UIInicio.Punto(e.rectTransform, new Vector2(0f, 0f), new Vector2((float)azar.NextDouble() * (AnchoPantalla + largo), (float)azar.NextDouble() * AltoPantalla), new Vector2(largo, altura));
                fondo.estelas[i] = new Estela { rect = e.rectTransform, velocidad = 350f + (float)azar.NextDouble() * 1500f, largo = largo };
            }

            SpritesInicio.ConfigurarRepeticion(SpritesInicio.Lineas);
            Image rejilla = UIInicio.Imagen(raiz, "Rejilla", new Color(0.4f, 0.8f, 1f, 0.05f), SpritesInicio.Lineas);
            rejilla.type = Image.Type.Tiled;
            UIInicio.Estirar(rejilla.rectTransform);

            Image barridoImagen = UIInicio.Imagen(raiz, "Barrido", UIInicio.ConAlfa(UIInicio.Cian, 0.07f), SpritesInicio.DegradadoV, true);
            UIInicio.Punto(barridoImagen.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(AnchoPantalla, 260f));
            Image bordeBarrido = UIInicio.Imagen(barridoImagen.transform, "Borde", UIInicio.ConAlfa(UIInicio.Cian, 0.5f), SpritesInicio.Blanco, true);
            UIInicio.Anclar(bordeBarrido.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f));
            fondo.barrido = barridoImagen.rectTransform;

            // Sombra al pie de la pantalla para que la barra y los consejos se lean.
            Image vineta = UIInicio.Imagen(raiz, "Vineta", UIInicio.ConAlfa(Color.black, 0.6f), SpritesInicio.DegradadoVAbajo);
            UIInicio.Anclar(vineta.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 460f));
            return fondo;
        }

        /// <summary>Dónde está la primera estela en x (para comprobar que el fondo está vivo).</summary>
        public float PosicionPrimeraEstela => estelas[0] != null ? estelas[0].rect.anchoredPosition.x : 0f;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            tiempo += dt;
            foreach (Estela e in estelas)
            {
                if (e == null)
                {
                    continue;
                }
                Vector2 p = e.rect.anchoredPosition;
                p.x -= e.velocidad * dt;
                if (p.x < -e.largo)
                {
                    p.x = AnchoPantalla + e.largo * 0.2f;
                }
                e.rect.anchoredPosition = p;
            }
            if (mancha1 != null)
            {
                float s = 1f + 0.08f * Mathf.Sin(tiempo * 0.9f);
                mancha1.localScale = new Vector3(s, s, 1f);
                mancha2.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(tiempo * 0.7f + 2f));
            }
            if (barrido != null)
            {
                float t = Mathf.Repeat(tiempo / SegundosBarrido, 1f);
                barrido.anchoredPosition = new Vector2(0f, Mathf.Lerp(260f, -AltoPantalla, t));
            }
        }
    }
}

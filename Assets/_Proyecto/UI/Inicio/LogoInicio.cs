using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// El logo CYBERFIX de la pantalla de inicio, hecho solo con texto: una copia magenta y otra turquesa (aditivas) que se separan del
    /// texto blanco cuando hay glitch, barras de corte que saltan de lado, un halo que respira y parpadeos de neón. Al aparecer
    /// parpadea como un letrero que se prende. <see cref="RelojGlitch"/> decide cuándo hay una ráfaga.
    /// </summary>
    public class LogoInicio : MonoBehaviour
    {
        public const string Lema = "SOBREVIVE A LA NOCHE";
        private const int CantidadCortes = 4;
        private const float SegundosEncendido = 1.1f;
        private const float SeparacionReposo = 3f;
        private const float SeparacionGlitch = 22f;
        private const float AlfaCopias = 0.55f;
        private const float AlfaHalo = 0.5f;
        private const float EsperaGlitchMinima = 2.2f;
        private const float EsperaGlitchMaxima = 6.5f;
        private const float DuracionGlitch = 0.38f;

        [SerializeField] private Text principal;
        [SerializeField] private Text copiaMagenta;
        [SerializeField] private Text copiaCian;
        [SerializeField] private Text halo;
        [SerializeField] private Text lema;
        [SerializeField] private Image linea;
        [SerializeField] private Image[] cortes = new Image[0];

        private RelojGlitch reloj;
        private float tiempo;
        private System.Random azar;

        public Text Principal => principal;
        public bool EnRafaga => reloj != null && reloj.EnRafaga;
        public float Intensidad => reloj != null ? reloj.Intensidad : 0f;
        public bool Encendido => tiempo >= SegundosEncendido;

        /// <summary>Provoca una ráfaga de glitch ya (al elegir un botón, por ejemplo).</summary>
        public void ForzarRafaga()
        {
            reloj = new RelojGlitch(() => (float)azar.NextDouble(), 0f, 0f, DuracionGlitch);
            reloj.Actualizar(0.001f);
            reloj.Actualizar(0.001f);
        }

        public static LogoInicio Crear(Transform padre, string texto, int tamano, Vector2 posicion)
        {
            RectTransform raiz = UIInicio.Nodo(padre, "Logo");
            UIInicio.Punto(raiz, new Vector2(0f, 1f), posicion, new Vector2(1000f, tamano * 0.8f));
            LogoInicio logo = raiz.gameObject.AddComponent<LogoInicio>();
            Font fuente = UIInicio.FuenteTitulos;
            logo.halo = UIInicio.Texto(raiz, "Halo", texto, fuente, tamano, UIInicio.ConAlfa(UIInicio.Cian, AlfaHalo), TextAnchor.MiddleLeft, true);
            logo.copiaMagenta = UIInicio.Texto(raiz, "CopiaMagenta", texto, fuente, tamano, UIInicio.ConAlfa(UIInicio.Magenta, AlfaCopias), TextAnchor.MiddleLeft, true);
            logo.copiaCian = UIInicio.Texto(raiz, "CopiaCian", texto, fuente, tamano, UIInicio.ConAlfa(UIInicio.Cian, AlfaCopias), TextAnchor.MiddleLeft, true);
            logo.principal = UIInicio.Texto(raiz, "Principal", texto, fuente, tamano, UIInicio.Blanco, TextAnchor.MiddleLeft);
            foreach (Text t in new[] { logo.halo, logo.copiaMagenta, logo.copiaCian, logo.principal })
            {
                UIInicio.Estirar(t.rectTransform);
            }
            Outline contorno = logo.halo.gameObject.AddComponent<Outline>();
            contorno.effectColor = UIInicio.ConAlfa(UIInicio.Cian, 0.25f);
            contorno.effectDistance = new Vector2(7f, 7f);
            contorno.useGraphicAlpha = false;
            Shadow sombra = logo.principal.gameObject.AddComponent<Shadow>();
            sombra.effectColor = UIInicio.ConAlfa(UIInicio.Magenta, 0.6f);
            sombra.effectDistance = new Vector2(4f, -4f);

            logo.cortes = new Image[CantidadCortes];
            for (int i = 0; i < CantidadCortes; i++)
            {
                Image corte = UIInicio.Imagen(raiz, "Corte" + i, UIInicio.ConAlfa(i % 2 == 0 ? UIInicio.Cian : UIInicio.Magenta, 0f), SpritesInicio.Blanco, true);
                UIInicio.Punto(corte.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(300f, 6f));
                logo.cortes[i] = corte;
            }

            logo.linea = UIInicio.Imagen(raiz, "Linea", UIInicio.Cian, SpritesInicio.DegradadoH, true);
            UIInicio.Punto(logo.linea.rectTransform, new Vector2(0f, 0f), new Vector2(8f, -6f), new Vector2(0f, 5f));
            logo.lema = UIInicio.Texto(raiz, "Lema", Lema, UIInicio.FuenteTexto, 30, UIInicio.TextoSuave, TextAnchor.UpperLeft);
            UIInicio.Punto(logo.lema.rectTransform, new Vector2(0f, 0f), new Vector2(10f, -64f), new Vector2(800f, 40f));
            return logo;
        }

        private void Awake()
        {
            azar = new System.Random(1337);
            reloj = new RelojGlitch(() => (float)azar.NextDouble(), EsperaGlitchMinima, EsperaGlitchMaxima, DuracionGlitch);
            Aplicar(0f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            tiempo += dt;
            reloj.Actualizar(dt);
            Aplicar(dt);
        }

        private void Aplicar(float dt)
        {
            if (principal == null)
            {
                return;
            }
            // Encendido: el letrero parpadea y se estabiliza.
            float encendido = Mathf.Clamp01(tiempo / SegundosEncendido);
            float parpadeoEncendido = 1f;
            if (encendido < 1f)
            {
                float onda = Mathf.Sin(tiempo * 47f) * Mathf.Sin(tiempo * 13f + 2f);
                parpadeoEncendido = encendido < 0.15f ? 0f : (onda > -0.3f + encendido * 0.9f ? 1f : 0.12f);
            }
            float respiracion = 0.82f + 0.18f * Mathf.Sin(tiempo * 1.6f);
            float intensidad = reloj != null ? reloj.Intensidad : 0f;
            float separacion = SeparacionReposo + SeparacionGlitch * intensidad;
            float temblor = intensidad > 0f ? (float)(azar.NextDouble() * 2.0 - 1.0) : 0f;

            principal.color = UIInicio.ConAlfa(UIInicio.Blanco, parpadeoEncendido * (intensidad > 0f && azar.NextDouble() < 0.25 ? 0.6f : 1f));
            principal.rectTransform.anchoredPosition = new Vector2(temblor * 6f * intensidad, 0f);
            copiaMagenta.rectTransform.anchoredPosition = new Vector2(-separacion, separacion * 0.25f * temblor);
            copiaCian.rectTransform.anchoredPosition = new Vector2(separacion, -separacion * 0.25f * temblor);
            copiaMagenta.color = UIInicio.ConAlfa(UIInicio.Magenta, AlfaCopias * parpadeoEncendido);
            copiaCian.color = UIInicio.ConAlfa(UIInicio.Cian, AlfaCopias * parpadeoEncendido);
            halo.color = UIInicio.ConAlfa(UIInicio.Cian, AlfaHalo * respiracion * parpadeoEncendido * 0.6f);

            for (int i = 0; i < cortes.Length; i++)
            {
                Image corte = cortes[i];
                if (intensidad > 0f && azar.NextDouble() < 0.7)
                {
                    float x = (float)azar.NextDouble() * 520f - 20f + temblor * 30f;
                    float y = ((float)azar.NextDouble() - 0.5f) * principal.fontSize * 0.7f;
                    float ancho = 80f + (float)azar.NextDouble() * 360f;
                    corte.rectTransform.anchoredPosition = new Vector2(x, y);
                    corte.rectTransform.sizeDelta = new Vector2(ancho, 3f + (float)azar.NextDouble() * 10f);
                    corte.color = UIInicio.ConAlfa(corte.color, 0.55f * intensidad);
                }
                else
                {
                    corte.color = UIInicio.ConAlfa(corte.color, 0f);
                }
            }

            float largoLinea = Mathf.Lerp(0f, 520f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tiempo - 0.5f) / 0.9f)));
            linea.rectTransform.sizeDelta = new Vector2(largoLinea, 5f);
            lema.color = UIInicio.ConAlfa(UIInicio.TextoSuave, Mathf.Clamp01((tiempo - 1.1f) / 0.6f));
        }
    }
}

using System.Collections;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Historia
{
    /// <summary>
    /// Pantalla grande con un título en Teko, neón y glitch (T-689 "HAS MUERTO", T-690 "ARRESTADO"), con el mismo estilo del menú de inicio:
    /// copias de color separadas, barras de corte y halo. Se arma por código en su propio canvas (debajo del fundido), entra desde
    /// negro, se queda y se quita con <see cref="Cerrar"/>. Usa tiempo sin escala.
    /// </summary>
    public class PantallaAviso : MonoBehaviour
    {
        private const int CantidadCortes = 5;
        private const int TamanoTitulo = 190;
        private const float SeparacionReposo = 4f;
        private const float SeparacionGlitch = 26f;
        private const float AlfaCopias = 0.6f;
        private const float EsperaGlitchMinima = 0.5f;
        private const float EsperaGlitchMaxima = 1.6f;
        private const float DuracionGlitch = 0.3f;

        private Image fondo;
        private Image vineta;
        private Text principal;
        private Text copiaAcento;
        private Text copiaCian;
        private Text halo;
        private Text detalle;
        private Image linea;
        private Image[] cortes;
        private Color acento;
        private RelojGlitch reloj;
        private System.Random azar;
        private float tiempo;
        private float visibilidad;

        public string Titulo => principal != null ? principal.text : string.Empty;
        public string Detalle => detalle != null ? detalle.text : string.Empty;
        public Text TextoPrincipal => principal;
        /// <summary>0 invisible, 1 totalmente opaca con el título encendido.</summary>
        public float Visibilidad => visibilidad;
        public bool EnRafaga => reloj != null && reloj.EnRafaga;

        /// <summary>Crea la pantalla (invisible). Llamar a <see cref="Aparecer"/> para mostrarla.</summary>
        public static PantallaAviso Crear(string titulo, string subtitulo, Color color)
        {
            GameObject raiz = new GameObject("PantallaAviso_" + titulo);
            PantallaAviso pantalla = raiz.AddComponent<PantallaAviso>();
            pantalla.Armar(titulo, subtitulo, color);
            return pantalla;
        }

        private void Armar(string titulo, string subtitulo, Color color)
        {
            acento = color;
            azar = new System.Random(689);
            reloj = new RelojGlitch(() => (float)azar.NextDouble(), EsperaGlitchMinima, EsperaGlitchMaxima, DuracionGlitch);
            Canvas canvas = KitUI.CrearCanvas(transform, "Canvas", PantallaFundido.OrdenDibujo - 1);
            canvas.overrideSorting = true;
            canvas.sortingOrder = PantallaFundido.OrdenDibujo - 1;
            RectTransform lienzo = (RectTransform)canvas.transform;

            fondo = UIInicio.Imagen(lienzo, "Fondo", UIInicio.ConAlfa(UIInicio.Noche, 0f));
            UIInicio.Estirar(fondo.rectTransform);
            fondo.raycastTarget = true;
            vineta = UIInicio.Imagen(lienzo, "Vineta", UIInicio.ConAlfa(acento, 0f), SpritesInicio.DegradadoVAbajo, true);
            UIInicio.Estirar(vineta.rectTransform);

            RectTransform centro = UIInicio.Nodo(lienzo, "Centro");
            UIInicio.Punto(centro, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(1700f, TamanoTitulo));
            Font fuente = UIInicio.FuenteTitulos;
            halo = UIInicio.Texto(centro, "Halo", titulo, fuente, TamanoTitulo, UIInicio.ConAlfa(acento, 0f), TextAnchor.MiddleCenter, true);
            copiaAcento = UIInicio.Texto(centro, "CopiaAcento", titulo, fuente, TamanoTitulo, UIInicio.ConAlfa(acento, 0f), TextAnchor.MiddleCenter, true);
            copiaCian = UIInicio.Texto(centro, "CopiaCian", titulo, fuente, TamanoTitulo, UIInicio.ConAlfa(UIInicio.Cian, 0f), TextAnchor.MiddleCenter, true);
            principal = UIInicio.Texto(centro, "Principal", titulo, fuente, TamanoTitulo, UIInicio.ConAlfa(UIInicio.Blanco, 0f), TextAnchor.MiddleCenter);
            foreach (Text t in new[] { halo, copiaAcento, copiaCian, principal })
            {
                UIInicio.Estirar(t.rectTransform);
            }
            Outline contorno = halo.gameObject.AddComponent<Outline>();
            contorno.effectColor = UIInicio.ConAlfa(acento, 0.25f);
            contorno.effectDistance = new Vector2(8f, 8f);
            contorno.useGraphicAlpha = false;

            cortes = new Image[CantidadCortes];
            for (int i = 0; i < CantidadCortes; i++)
            {
                Image corte = UIInicio.Imagen(centro, "Corte" + i, UIInicio.ConAlfa(i % 2 == 0 ? UIInicio.Cian : acento, 0f), SpritesInicio.Blanco, true);
                UIInicio.Punto(corte.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 6f));
                cortes[i] = corte;
            }

            linea = UIInicio.Imagen(lienzo, "Linea", UIInicio.ConAlfa(acento, 0f), SpritesInicio.DegradadoH, true);
            UIInicio.Punto(linea.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0f, -TamanoTitulo * 0.55f), new Vector2(900f, 5f));
            detalle = UIInicio.Texto(lienzo, "Detalle", subtitulo, UIInicio.FuenteTexto, 38, UIInicio.ConAlfa(UIInicio.TextoSuave, 0f), TextAnchor.UpperCenter);
            UIInicio.Punto(detalle.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0f, -TamanoTitulo * 0.55f - 24f), new Vector2(1500f, 60f));
        }

        /// <summary>Entra desde negro y enciende el título como un letrero de neón.</summary>
        public IEnumerator Aparecer(float segundos)
        {
            float transcurrido = 0f;
            while (transcurrido < segundos)
            {
                transcurrido += Time.unscaledDeltaTime;
                visibilidad = Mathf.Clamp01(transcurrido / Mathf.Max(0.01f, segundos));
                yield return null;
            }
            visibilidad = 1f;
        }

        /// <summary>Ya visible del todo, sin animación de entrada.</summary>
        public void MostrarYa() => visibilidad = 1f;

        /// <summary>Quita la pantalla (destruye el objeto).</summary>
        public void Cerrar()
        {
            Destroy(gameObject);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            tiempo += dt;
            reloj.Actualizar(dt);
            Aplicar();
        }

        private void Aplicar()
        {
            if (principal == null)
            {
                return;
            }
            float v = visibilidad;
            // Encendido: parpadea mientras entra.
            float parpadeo = 1f;
            if (v < 1f)
            {
                float onda = Mathf.Sin(tiempo * 47f) * Mathf.Sin(tiempo * 13f + 2f);
                parpadeo = v < 0.2f ? 0f : (onda > -0.3f + v * 0.9f ? 1f : 0.15f);
            }
            float respiracion = 0.8f + 0.2f * Mathf.Sin(tiempo * 2f);
            float intensidad = reloj.Intensidad;
            float separacion = SeparacionReposo + SeparacionGlitch * intensidad;
            float temblor = intensidad > 0f ? (float)(azar.NextDouble() * 2.0 - 1.0) : 0f;

            fondo.color = UIInicio.ConAlfa(UIInicio.Noche, Mathf.Clamp01(v * 1.4f));
            vineta.color = UIInicio.ConAlfa(acento, 0.16f * v * respiracion);
            principal.color = UIInicio.ConAlfa(UIInicio.Blanco, parpadeo * v * (intensidad > 0f && azar.NextDouble() < 0.25 ? 0.6f : 1f));
            principal.rectTransform.anchoredPosition = new Vector2(temblor * 8f * intensidad, 0f);
            copiaAcento.rectTransform.anchoredPosition = new Vector2(-separacion, separacion * 0.25f * temblor);
            copiaCian.rectTransform.anchoredPosition = new Vector2(separacion, -separacion * 0.25f * temblor);
            copiaAcento.color = UIInicio.ConAlfa(acento, AlfaCopias * parpadeo * v);
            copiaCian.color = UIInicio.ConAlfa(UIInicio.Cian, AlfaCopias * parpadeo * v);
            halo.color = UIInicio.ConAlfa(acento, 0.35f * respiracion * parpadeo * v);

            for (int i = 0; i < cortes.Length; i++)
            {
                Image corte = cortes[i];
                if (intensidad > 0f && v >= 1f && azar.NextDouble() < 0.7)
                {
                    float x = ((float)azar.NextDouble() - 0.5f) * 1200f;
                    float y = ((float)azar.NextDouble() - 0.5f) * TamanoTitulo * 0.8f;
                    corte.rectTransform.anchoredPosition = new Vector2(x, y);
                    corte.rectTransform.sizeDelta = new Vector2(80f + (float)azar.NextDouble() * 420f, 3f + (float)azar.NextDouble() * 10f);
                    corte.color = UIInicio.ConAlfa(corte.color, 0.55f * intensidad);
                }
                else
                {
                    corte.color = UIInicio.ConAlfa(corte.color, 0f);
                }
            }

            float largo = Mathf.Lerp(0f, 900f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - 0.3f) / 0.7f)));
            linea.rectTransform.sizeDelta = new Vector2(largo, 5f);
            linea.color = UIInicio.ConAlfa(acento, v);
            detalle.color = UIInicio.ConAlfa(UIInicio.TextoSuave, Mathf.Clamp01((v - 0.5f) / 0.5f));
        }
    }
}

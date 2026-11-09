using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// Las animaciones comunes de los menús, con DOTween y tiempo sin escala (también corren en pausa). Todas se ligan al objeto que
    /// animan: si la pantalla se destruye, el tween muere con ella.
    /// </summary>
    public static class AnimH10
    {
        public const float SegundosEntrada = 0.32f;
        public const float SegundosEscalon = 0.05f;
        public const float SegundosHover = 0.12f;
        public const float SegundosNumero = 0.45f;
        public const float DesplazamientoEntrada = 28f;
        private const float AlfaParpadeo = 0.45f;
        private const float FraccionParpadeo = 0.35f;

        /// <summary>Entrada de panel: llega deslizándose desde <paramref name="desde"/> con un parpadeo corto, como una pantalla que enciende.</summary>
        public static Sequence EntrarPanel(RectTransform panel, Vector2 desde, float demora = 0f)
        {
            panel.DOKill();
            CanvasGroup grupo = Grupo(panel);
            Vector2 destino = panel.anchoredPosition;
            grupo.alpha = 0f;
            panel.anchoredPosition = destino + desde;
            Sequence secuencia = DOTween.Sequence().SetUpdate(true).SetLink(panel.gameObject).SetTarget(panel);
            secuencia.AppendInterval(demora);
            secuencia.Append(DOTween.To(() => panel.anchoredPosition, v => panel.anchoredPosition = v, destino, SegundosEntrada).SetEase(Ease.OutCubic));
            secuencia.Join(DOTween.To(() => grupo.alpha, v => grupo.alpha = v, AlfaParpadeo, SegundosEntrada * FraccionParpadeo));
            secuencia.Append(DOTween.To(() => grupo.alpha, v => grupo.alpha = v, 0.2f, SegundosEscalon));
            secuencia.Append(DOTween.To(() => grupo.alpha, v => grupo.alpha = v, 1f, SegundosEntrada * FraccionParpadeo));
            secuencia.OnKill(() =>
            {
                if (panel != null)
                {
                    panel.anchoredPosition = destino;
                    grupo.alpha = 1f;
                }
            });
            return secuencia;
        }

        /// <summary>Línea que crece desde su pivote.</summary>
        public static Tween CrecerLinea(Image linea, float demora = 0f, bool horizontal = true)
        {
            RectTransform r = linea.rectTransform;
            r.DOKill();
            r.localScale = horizontal ? new Vector3(0f, 1f, 1f) : new Vector3(1f, 0f, 1f);
            return DOTween.To(() => r.localScale, v => r.localScale = v, Vector3.one, SegundosEntrada)
                .SetDelay(demora).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(r.gameObject).SetTarget(r);
        }

        /// <summary>Cambia un color de forma animada (hover, selección).</summary>
        public static Tween Tenir(Graphic grafico, Color destino, float segundos = SegundosHover)
        {
            grafico.DOKill();
            return DOTween.To(() => grafico.color, v => grafico.color = v, destino, segundos)
                .SetUpdate(true).SetLink(grafico.gameObject).SetTarget(grafico);
        }

        /// <summary>Lleva una escala uniforme a un valor (hover).</summary>
        public static Tween Escala(RectTransform rect, float escala, float segundos = SegundosHover)
        {
            rect.DOKill();
            return DOTween.To(() => rect.localScale, v => rect.localScale = v, Vector3.one * escala, segundos)
                .SetEase(Ease.OutCubic).SetUpdate(true).SetLink(rect.gameObject).SetTarget(rect);
        }

        /// <summary>Número que corre de un valor a otro; <paramref name="formato"/> convierte el valor a texto.</summary>
        public static Tween Numero(Text texto, float desde, float hasta, Func<float, string> formato, float segundos = SegundosNumero)
        {
            texto.DOKill();
            float valor = desde;
            texto.text = formato(desde);
            return DOTween.To(() => valor, v => { valor = v; texto.text = formato(v); }, hasta, segundos)
                .SetEase(Ease.OutCubic).SetUpdate(true).SetLink(texto.gameObject).SetTarget(texto)
                .OnKill(() =>
                {
                    if (texto != null)
                    {
                        texto.text = formato(hasta);
                    }
                });
        }

        /// <summary>Destello: el gráfico pasa al color y regresa al que tenía.</summary>
        public static Tween Destellar(Graphic grafico, Color destello, float segundos = SegundosNumero)
        {
            grafico.DOKill();
            Color original = grafico.color;
            grafico.color = destello;
            return DOTween.To(() => grafico.color, v => grafico.color = v, original, segundos)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetLink(grafico.gameObject).SetTarget(grafico);
        }

        private static CanvasGroup Grupo(RectTransform panel)
        {
            CanvasGroup grupo = panel.GetComponent<CanvasGroup>();
            return grupo != null ? grupo : panel.gameObject.AddComponent<CanvasGroup>();
        }
    }
}

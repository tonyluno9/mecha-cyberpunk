using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-725: una barra del HUD que, al bajar, deja la parte perdida un momento y luego la baja suave con DOTween. El relleno principal
    /// refleja el valor al instante; la capa de pérdida (detrás) se queda donde estaba, espera y desciende hasta alcanzarlo. Si el valor sube
    /// (curación), las dos capas suben juntas.
    /// </summary>
    public sealed class BarraConPerdida
    {
        private readonly Image relleno;
        private readonly Image perdida;
        private readonly float espera;
        private readonly float duracion;
        private Tween bajada;
        private float ultimo = -1f;

        public BarraConPerdida(Image relleno, Image perdida, float espera, float duracion)
        {
            this.relleno = relleno;
            this.perdida = perdida;
            this.espera = Mathf.Max(0f, espera);
            this.duracion = Mathf.Max(0.01f, duracion);
        }

        public Image Relleno => relleno;
        public Image Perdida => perdida;
        /// <summary>True mientras la parte perdida espera o baja.</summary>
        public bool Bajando => bajada != null && bajada.IsActive() && bajada.IsPlaying();

        /// <summary>Pone la fracción (0 a 1). Llamarla cada cuadro con el mismo valor no hace nada.</summary>
        public void Poner(float fraccion)
        {
            fraccion = Mathf.Clamp01(fraccion);
            if (Mathf.Approximately(fraccion, ultimo))
            {
                return;
            }
            bool bajo = ultimo >= 0f && fraccion < ultimo;
            ultimo = fraccion;
            relleno.fillAmount = fraccion;
            if (!bajo || perdida.fillAmount <= fraccion)
            {
                Detener();
                perdida.fillAmount = fraccion;
                return;
            }
            Detener();
            bajada = DOTween.To(() => perdida.fillAmount, v => perdida.fillAmount = v, fraccion, duracion)
                .SetDelay(espera)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(perdida.gameObject);
        }

        /// <summary>Corta la bajada en curso (al destruir el HUD).</summary>
        public void Detener()
        {
            if (bajada != null && bajada.IsActive())
            {
                bajada.Kill();
            }
            bajada = null;
        }
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Historia
{
    /// <summary>
    /// Fundido a negro a pantalla completa (T-125). Canvas Overlay con el orden más alto; transparente no bloquea clics.
    /// Otras áreas lo encuentran con <see cref="Actual"/>. Usa tiempo sin escala.
    /// </summary>
    public class PantallaFundido : MonoBehaviour
    {
        public const int OrdenDibujo = short.MaxValue;
        private const float UmbralOpaco = 0.999f;

        [SerializeField] private ConfigPantallasHistoria config;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image negro;

        private int version;

        /// <summary>La última pantalla de fundido habilitada; null si no hay.</summary>
        public static PantallaFundido Actual { get; private set; }

        public ConfigPantallasHistoria Config => config;
        public float Alfa => negro != null ? negro.color.a : 0f;
        public bool Opaco => Alfa >= UmbralOpaco;

        /// <summary>Se avisa una vez al terminar cada fundido (no al ser cortado por otro).</summary>
        public event Action Terminado;

        private void Awake()
        {
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = OrdenDibujo;
            }
        }

        private void OnEnable()
        {
            Actual = this;
        }

        private void OnDisable()
        {
            version++;
            if (Actual == this)
            {
                Actual = null;
            }
        }

        public void Fijar(float alfa)
        {
            version++;
            Aplicar(alfa);
        }

        /// <summary>Lleva el alfa desde el actual hasta <paramref name="alfaFinal"/>. Uno nuevo corta el anterior sin saltos.</summary>
        public IEnumerator Fundir(float alfaFinal, float segundos)
        {
            int miVersion = ++version;
            alfaFinal = Mathf.Clamp01(alfaFinal);
            float inicio = Alfa;
            float transcurrido = 0f;
            while (transcurrido < segundos)
            {
                yield return null;
                if (miVersion != version)
                {
                    yield break;
                }
                transcurrido += Time.unscaledDeltaTime;
                Aplicar(Mathf.Lerp(inicio, alfaFinal, Mathf.Clamp01(transcurrido / segundos)));
            }
            if (miVersion != version)
            {
                yield break;
            }
            Aplicar(alfaFinal);
            Terminado?.Invoke();
        }

        private void Aplicar(float alfa)
        {
            alfa = Mathf.Clamp01(alfa);
            Color color = negro.color;
            color.a = alfa;
            negro.color = color;
            // Transparente no bloquea clics.
            negro.raycastTarget = alfa > 0f;
        }
    }
}

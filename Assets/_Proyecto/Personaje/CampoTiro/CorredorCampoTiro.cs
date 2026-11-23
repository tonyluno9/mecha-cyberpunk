using System.Collections;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T-1722 (T79): el MonoBehaviour que mueve <see cref="CampoDeTiro"/> cada cuadro (invulnerabilidad, límites de la sala, la tecla E) y hace el fundido a negro.
    /// </summary>
    public class CorredorCampoTiro : MonoBehaviour
    {
        public const int OrdenFundido = 31000;

        private Image negro;
        private float alfa;
        private int version;

        /// <summary>Opacidad actual del negro (0 a 1).</summary>
        public float Alfa => alfa;

        private void Awake()
        {
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasFundidoCampoTiro", OrdenFundido);
            canvas.overrideSorting = true;
            negro = KitUI.CrearImagen(canvas.transform, "Negro", new Color(0f, 0f, 0f, 0f));
            KitUI.Estirar(negro.rectTransform);
            negro.raycastTarget = false;
        }

        private void Update()
        {
            CampoDeTiro.Actualizar();
            if (CampoDeTiro.EstaDentro && !CampoDeTiro.EnTransicion && !CampoDeTiro.MenuAbierto && PilaPantallas.Vacia &&
                Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                CampoDeTiro.AbrirMenu();
            }
        }

        private void LateUpdate() => CampoDeTiro.AlFinalDelCuadro();

        /// <summary>Lleva el negro hasta <paramref name="alfaFinal"/> en <paramref name="segundos"/> (tiempo sin escala). Uno nuevo corta el anterior.</summary>
        public IEnumerator Fundir(float alfaFinal, float segundos)
        {
            int mia = ++version;
            float inicio = alfa;
            float t = 0f;
            while (t < segundos)
            {
                yield return null;
                if (mia != version)
                {
                    yield break;
                }
                t += Time.unscaledDeltaTime;
                Poner(Mathf.Lerp(inicio, alfaFinal, Mathf.Clamp01(t / segundos)));
            }
            if (mia == version)
            {
                Poner(alfaFinal);
            }
        }

        private void Poner(float valor)
        {
            alfa = valor;
            negro.color = new Color(0f, 0f, 0f, valor);
            negro.raycastTarget = valor > 0.01f;
        }
    }
}

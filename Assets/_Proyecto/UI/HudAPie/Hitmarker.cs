using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Marca de impacto en la retícula (T-420): cuatro rayitas en diagonal que aparecen un instante al pegar a una persona,
    /// blancas, y más grandes y naranjas si la dejó en KO. La crea <see cref="HudAPie"/>; busca al <see cref="SensacionDisparo"/> del jugador.
    /// </summary>
    public class Hitmarker : MonoBehaviour
    {
        public const int OrdenCanvas = 15;
        private const float Largo = 14f;
        private const float Grosor = 3f;
        private const float Separacion = 8f;
        private const float EscalaMuerte = 1.6f;

        private SensacionDisparo sensacion;
        private Canvas canvas;
        private RectTransform raiz;
        private Image[] rayas;
        private float fin = -1f;
        private float duracion = 0.15f;
        private bool mato;

        /// <summary>True mientras la marca está a la vista.</summary>
        public bool Visible => canvas != null && canvas.enabled;
        public bool UltimaMato => mato;

        private void Awake()
        {
            canvas = KitUI.CrearCanvas(transform, "CanvasHitmarker", OrdenCanvas);
            GameObject centro = new GameObject("Marca", typeof(RectTransform));
            centro.transform.SetParent(canvas.transform, false);
            raiz = (RectTransform)centro.transform;
            raiz.anchorMin = raiz.anchorMax = raiz.pivot = new Vector2(0.5f, 0.5f);
            raiz.anchoredPosition = Vector2.zero;
            rayas = new Image[4];
            for (int i = 0; i < rayas.Length; i++)
            {
                Image raya = KitUI.CrearImagen(raiz, "Raya" + i, Color.white);
                raya.raycastTarget = false;
                RectTransform r = raya.rectTransform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(Largo, Grosor);
                float angulo = 45f + 90f * i;
                Vector2 dir = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
                r.anchoredPosition = dir * (Separacion + Largo * 0.5f);
                r.localRotation = Quaternion.Euler(0f, 0f, angulo);
                rayas[i] = raya;
            }
            canvas.enabled = false;
        }

        private void OnDisable()
        {
            if (sensacion != null)
            {
                sensacion.Conecto -= AlConectar;
                sensacion = null;
            }
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }

        private void Update()
        {
            if (sensacion == null)
            {
                sensacion = FindFirstObjectByType<SensacionDisparo>();
                if (sensacion != null)
                {
                    sensacion.Conecto += AlConectar;
                }
            }
            if (canvas.enabled)
            {
                float restante = (fin - Time.time) / duracion;
                if (restante <= 0f)
                {
                    canvas.enabled = false;
                    return;
                }
                foreach (Image raya in rayas)
                {
                    Color c = raya.color;
                    c.a = Mathf.Clamp01(restante * 2f);
                    raya.color = c;
                }
            }
        }

        private void AlConectar(bool murio)
        {
            mato = murio;
            duracion = murio ? sensacion.Datos.DuracionHitmarkerMuerte : sensacion.Datos.DuracionHitmarker;
            fin = Time.time + duracion;
            Color color = murio ? KitUI.Estilo.Naranja : Color.white;
            foreach (Image raya in rayas)
            {
                raya.color = color;
            }
            raiz.localScale = Vector3.one * (murio ? EscalaMuerte : 1f);
            canvas.enabled = true;
        }
    }
}

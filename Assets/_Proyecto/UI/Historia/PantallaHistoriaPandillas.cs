using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.UI.Historia
{
    /// <summary>
    /// Textos de la historia de pandillas (T-406) dibujados con OnGUI: un subtítulo abajo, una ayuda arriba y la pantalla de "CONTINUARÁ...".
    /// Enter o Esc cierran el continuará (tras un segundo). Tiempo sin escala.
    /// </summary>
    public class PantallaHistoriaPandillas : MonoBehaviour
    {
        private const float SegundosMinimosContinuara = 1f;

        private string subtitulo = string.Empty;
        private float finSubtitulo;
        private string ayuda = string.Empty;
        private float finAyuda;
        private float inicioContinuara;
        private GUIStyle estiloSubtitulo;
        private GUIStyle estiloAyuda;
        private GUIStyle estiloTitulo;
        private GUIStyle estiloTeclas;

        public string Subtitulo => Time.unscaledTime < finSubtitulo ? subtitulo : string.Empty;
        public string Ayuda => Time.unscaledTime < finAyuda ? ayuda : string.Empty;
        public bool ContinuaraVisible { get; private set; }

        /// <summary>Se dispara al cerrar la pantalla de continuará.</summary>
        public event Action ContinuaraCerrada;

        public void MostrarSubtitulo(string texto, float segundos)
        {
            subtitulo = texto;
            finSubtitulo = Time.unscaledTime + segundos;
        }

        public void MostrarAyuda(string texto, float segundos)
        {
            ayuda = texto;
            finAyuda = Time.unscaledTime + segundos;
        }

        public void MostrarContinuara()
        {
            ContinuaraVisible = true;
            inicioContinuara = Time.unscaledTime;
        }

        public void CerrarContinuara()
        {
            if (!ContinuaraVisible)
            {
                return;
            }
            ContinuaraVisible = false;
            ContinuaraCerrada?.Invoke();
        }

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            if (ContinuaraVisible && teclado != null && Time.unscaledTime - inicioContinuara >= SegundosMinimosContinuara
                && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.escapeKey.wasPressedThisFrame))
            {
                CerrarContinuara();
            }
        }

        private static GUIStyle Estilo(int tamano, FontStyle fuente, TextAnchor ancla, Color color)
        {
            GUIStyle estilo = new GUIStyle(GUI.skin.label) { fontSize = tamano, fontStyle = fuente, alignment = ancla, wordWrap = true };
            estilo.normal.textColor = color;
            return estilo;
        }

        private void OnGUI()
        {
            if (estiloSubtitulo == null)
            {
                int u = Mathf.Max(14, Screen.height / 36);
                estiloSubtitulo = Estilo(u, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                estiloAyuda = Estilo(u, FontStyle.Bold, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.2f));
                estiloTitulo = Estilo(u * 4, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.1f, 0.8f));
                estiloTeclas = Estilo(u, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            }
            float ancho = Screen.width;
            float alto = Screen.height;
            if (ContinuaraVisible)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.85f);
                GUI.DrawTexture(new Rect(0f, 0f, ancho, alto), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(0f, alto * 0.3f, ancho, alto * 0.2f), "CONTINUARÁ...", estiloTitulo);
                GUI.Label(new Rect(0f, alto * 0.52f, ancho, alto * 0.1f), "EL TIANGUIS ES TUYO. NEÓN ALTO TE ESPERA.", estiloTeclas);
                GUI.Label(new Rect(0f, alto * 0.62f, ancho, alto * 0.1f), "ENTER: SEGUIR JUGANDO", estiloTeclas);
                return;
            }
            if (Time.unscaledTime < finSubtitulo)
            {
                GUI.Label(new Rect(ancho * 0.15f, alto * 0.78f, ancho * 0.7f, alto * 0.08f), subtitulo, estiloSubtitulo);
            }
            if (Time.unscaledTime < finAyuda)
            {
                GUI.Label(new Rect(ancho * 0.2f, alto * 0.14f, ancho * 0.6f, alto * 0.12f), ayuda, estiloAyuda);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Historia
{
    /// <summary>
    /// Subtítulos de las secuencias de historia (T-131): Canvas Overlay con una línea abajo al centro (con sombra) y un aviso pequeño
    /// en la esquina. Va por debajo del fundido.
    /// </summary>
    public class PantallaSubtitulos : MonoBehaviour
    {
        [SerializeField] private Text textoSubtitulo;
        [SerializeField] private Text textoAviso;

        public string Subtitulo => textoSubtitulo != null ? textoSubtitulo.text : string.Empty;
        public string Aviso => textoAviso != null ? textoAviso.text : string.Empty;
        public bool SubtituloVisible => textoSubtitulo != null && textoSubtitulo.gameObject.activeSelf;

        private void Awake()
        {
            if (textoSubtitulo == null || textoAviso == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaSubtitulos de " + name, this);
                enabled = false;
                return;
            }
            textoSubtitulo.gameObject.SetActive(false);
            textoAviso.gameObject.SetActive(false);
        }

        public void Mostrar(string texto)
        {
            textoSubtitulo.text = texto;
            textoSubtitulo.gameObject.SetActive(true);
        }

        public void Ocultar()
        {
            textoSubtitulo.gameObject.SetActive(false);
        }

        public void MostrarAviso(string texto)
        {
            textoAviso.text = texto;
            textoAviso.gameObject.SetActive(!string.IsNullOrEmpty(texto));
        }
    }
}

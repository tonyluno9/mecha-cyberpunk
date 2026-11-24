using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR4: un aviso corto ("NO DISPONIBLE EN VR") en un panel del kit frente a la cabeza. Se queda quieto donde salió y se va solo.
    /// </summary>
    public sealed class PanelAvisoVR
    {
        public const string NombreCanvas = "CanvasAvisoVR";
        private const float AnchoPx = 900f;
        private const float AltoPx = 150f;
        /// <summary>Por encima de los menús.</summary>
        private const int Orden = 200;

        private Canvas canvas;
        private Text texto;
        private float restante;

        public Canvas Canvas => canvas;
        public bool Visible => canvas != null && canvas.gameObject.activeSelf && restante > 0f;
        public string Texto => texto != null ? texto.text : string.Empty;

        private void Crear()
        {
            canvas = KitUI.CrearCanvas(null, NombreCanvas, Orden);
            canvas.renderMode = RenderMode.WorldSpace;
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }
            Object.DontDestroyOnLoad(canvas.gameObject);
            var rect = (RectTransform)canvas.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(AnchoPx, AltoPx);
            RectTransform panel = KitUI.CrearPanel(rect, "Fondo", new Vector2(AnchoPx, AltoPx));
            texto = KitUI.CrearTexto(panel, "Texto", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            canvas.gameObject.SetActive(false);
        }

        /// <summary>Muestra el aviso en esta pose (m por px de referencia) durante <paramref name="segundos"/> (tiempo real).</summary>
        public void Mostrar(string mensaje, Pose pose, float metrosPorPx, float segundos)
        {
            if (canvas == null)
            {
                Crear();
            }
            texto.text = mensaje;
            canvas.transform.SetPositionAndRotation(pose.position, pose.rotation);
            canvas.transform.localScale = Vector3.one * metrosPorPx;
            canvas.gameObject.SetActive(true);
            restante = segundos;
        }

        public void Actualizar(float dt)
        {
            if (restante <= 0f)
            {
                return;
            }
            restante -= dt;
            if (restante <= 0f && canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        public void Destruir()
        {
            if (canvas != null)
            {
                Object.Destroy(canvas.gameObject);
            }
            canvas = null;
            texto = null;
            restante = 0f;
        }
    }
}

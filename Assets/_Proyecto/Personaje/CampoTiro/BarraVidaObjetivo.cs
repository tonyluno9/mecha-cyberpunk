using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T86: la barra de vida de un objetivo del campo de tiro (en lugar de los números de daño). Barra pequeña turquesa sobre la cabeza, del estilo de la de T43
    /// (UI/BarrasEnemigos) pero en un Canvas World Space propio, porque los maniquíes no son enemigos de IA ni marcas del mapa. Mira siempre a la cámara.
    /// </summary>
    public class BarraVidaObjetivo : MonoBehaviour
    {
        private const float EscalaMundo = 0.01f;
        private static readonly Vector2 Tamano = new Vector2(100f, 12f);
        private static readonly Color Turquesa = new Color(0.30f, 0.92f, 0.90f, 1f);
        private static readonly Color Fondo = new Color(0.01f, 0.03f, 0.05f, 0.75f);

        private RectTransform relleno;
        private GameObject raiz;

        public bool Visible { get; private set; }
        /// <summary>Vida de 0 a 1.</summary>
        public float Fraccion { get; private set; } = 1f;

        /// <summary>Crea la barra como hija de <paramref name="padre"/>, a <paramref name="altura"/> metros sobre su origen.</summary>
        public static BarraVidaObjetivo Crear(Transform padre, float altura)
        {
            GameObject objeto = new GameObject("BarraVida");
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = new Vector3(0f, altura, 0f);
            objeto.transform.localScale = Vector3.one * EscalaMundo;
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)objeto.transform;
            rect.sizeDelta = Tamano;
            BarraVidaObjetivo barra = objeto.AddComponent<BarraVidaObjetivo>();
            barra.raiz = objeto;
            Image fondo = Hijo(objeto.transform, "Fondo", Fondo);
            Estirar(fondo.rectTransform);
            Image relleno = Hijo(objeto.transform, "Relleno", Turquesa);
            Estirar(relleno.rectTransform);
            barra.relleno = relleno.rectTransform;
            barra.Visible = true;
            return barra;
        }

        public void Poner(float fraccion, bool visible)
        {
            Fraccion = Mathf.Clamp01(fraccion);
            relleno.anchorMax = new Vector2(Fraccion, 1f);
            Visible = visible;
            raiz.SetActive(visible);
        }

        private void LateUpdate()
        {
            Camera camara = Camera.main;
            if (camara != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - camara.transform.position, Vector3.up);
            }
        }

        private static Image Hijo(Transform padre, string nombre, Color color)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Image imagen = objeto.AddComponent<Image>();
            imagen.color = color;
            imagen.raycastTarget = false;
            return imagen;
        }

        private static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

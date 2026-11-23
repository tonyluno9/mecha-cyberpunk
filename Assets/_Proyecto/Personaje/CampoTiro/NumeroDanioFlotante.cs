using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>T-1722 (T79): número de daño que sube y se apaga sobre el punto del impacto. uGUI Text en un Canvas World Space que mira a la cámara.</summary>
    public class NumeroDanioFlotante : MonoBehaviour
    {
        private const float Segundos = 0.9f;
        private const float MetrosSubida = 1.2f;
        private const float EscalaMundo = 0.01f;
        private const int TamanoLetra = 56;
        private static readonly Vector2 TamanoCaja = new Vector2(300f, 100f);

        private Text texto;
        private Vector3 inicio;
        private float edad;

        /// <summary>Crea un número en el mundo. El texto lleva el daño redondeado.</summary>
        public static NumeroDanioFlotante Crear(Vector3 punto, string contenido, Color color, Transform padre = null)
        {
            GameObject objeto = new GameObject("NumeroDanio");
            objeto.transform.SetParent(padre, true);
            objeto.transform.position = punto;
            objeto.transform.localScale = Vector3.one * EscalaMundo;
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)objeto.transform;
            rect.sizeDelta = TamanoCaja;
            NumeroDanioFlotante numero = objeto.AddComponent<NumeroDanioFlotante>();
            numero.texto = TrazoH10.Etiqueta(objeto.transform, "Texto", contenido, TamanoLetra, color, TextAnchor.MiddleCenter);
            TrazoH10.Estirar(numero.texto.rectTransform);
            numero.inicio = punto;
            return numero;
        }

        private void LateUpdate()
        {
            edad += Time.deltaTime;
            float t = Mathf.Clamp01(edad / Segundos);
            transform.position = inicio + Vector3.up * (MetrosSubida * t);
            Camera camara = Camera.main;
            if (camara != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - camara.transform.position, Vector3.up);
            }
            Color color = texto.color;
            color.a = 1f - t * t;
            texto.color = color;
            if (edad >= Segundos)
            {
                Destroy(gameObject);
            }
        }
    }
}

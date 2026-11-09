using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// T-934: todas las líneas finas del mapa grande en una sola malla (calles o edificios). Son cientos de tramos: como imágenes sueltas serían demasiadas.
    /// El grosor se da en unidades del dibujo; el mapa lo ajusta con el zoom para que la línea se vea igual de fina.
    /// </summary>
    public class LineasMapa : MaskableGraphic
    {
        private const int MaximoTramos = 16000;
        private const float LadoRecuadro = 2400f;

        private readonly List<Vector2> puntos = new List<Vector2>();
        private float grosor = 1f;

        /// <summary>Cuántos tramos se dibujan.</summary>
        public int Cantidad => puntos.Count / 2;

        public float Grosor => grosor;

        public static LineasMapa Crear(Transform padre, string nombre, Color color)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer));
            objeto.transform.SetParent(padre, false);
            LineasMapa lineas = objeto.AddComponent<LineasMapa>();
            lineas.color = color;
            lineas.raycastTarget = false;
            // El recuadro cubre toda la ciudad: si fuera de tamaño cero, la máscara del mapa lo daría por fuera de la vista al hacer zoom.
            RectTransform rect = lineas.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.one * LadoRecuadro;
            return lineas;
        }

        /// <summary>Pone los tramos (m: x este, y norte) a <paramref name="pixelesPorMetro"/>.</summary>
        public void Fijar(IReadOnlyList<CalculoCallesMapa.Tramo> tramos, float pixelesPorMetro)
        {
            puntos.Clear();
            int cuantos = Mathf.Min(tramos.Count, MaximoTramos);
            for (int i = 0; i < cuantos; i++)
            {
                puntos.Add(tramos[i].A * pixelesPorMetro);
                puntos.Add(tramos[i].B * pixelesPorMetro);
            }
            SetVerticesDirty();
        }

        public void FijarGrosor(float nuevo)
        {
            if (!Mathf.Approximately(grosor, nuevo))
            {
                grosor = nuevo;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Color32 c = color;
            for (int i = 0; i + 1 < puntos.Count; i += 2)
            {
                Vector2 a = puntos[i];
                Vector2 b = puntos[i + 1];
                Vector2 d = b - a;
                if (d.sqrMagnitude < 0.0001f)
                {
                    continue;
                }
                Vector2 n = new Vector2(-d.y, d.x).normalized * (grosor * 0.5f);
                int inicio = vh.currentVertCount;
                vh.AddVert(a - n, c, Vector2.zero);
                vh.AddVert(a + n, c, Vector2.zero);
                vh.AddVert(b + n, c, Vector2.zero);
                vh.AddVert(b - n, c, Vector2.zero);
                vh.AddTriangle(inicio, inicio + 1, inicio + 2);
                vh.AddTriangle(inicio + 2, inicio + 3, inicio);
            }
        }
    }
}

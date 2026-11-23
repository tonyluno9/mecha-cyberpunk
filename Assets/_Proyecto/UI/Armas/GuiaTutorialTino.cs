using System.Collections.Generic;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Armas
{
    /// <summary>
    /// T-84: la capa visual del tutorial de Tino. Oscurece toda la pantalla con celdas que bloquean el clic y deja libres solo los "huecos" de los
    /// controles del paso actual (el clic atraviesa el hueco). Cada hueco lleva un borde que pulsa y una etiqueta con la instrucción.
    /// Si el paso no tiene control a la vista no oscurece nada (para no dejar al jugador sin salida).
    /// </summary>
    internal sealed class GuiaTutorialTino
    {
        private const int MaxHuecos = 2;
        private const int CeldasMaximas = (2 * MaxHuecos + 1) * (2 * MaxHuecos + 1);
        /// <summary>Orden del canvas de la guía: encima de PantallaTienda (30000) y de las mejoras.</summary>
        public const int OrdenCanvas = 30100;
        private const float AlfaOscuro = 0.72f;
        private const float HolguraBorde = 6f;
        private const float AmplitudPulso = 4f;
        private const float VelocidadPulso = 6f;
        private const float GrosorBorde = 4f;
        private const float AnchoEtiqueta = 820f;
        private const float AltoEtiqueta = 58f;
        private const float SeparacionEtiqueta = 14f;
        private const float AlfaMinimoBorde = 0.55f;
        private const float TamanoMinimoCelda = 0.5f;

        private readonly RectTransform raiz;
        private readonly Image[] celdas = new Image[CeldasMaximas];
        private readonly Image[][] bordes = new Image[MaxHuecos][];
        private readonly RectTransform etiqueta;
        private readonly Text texto;
        private readonly List<float> cortesX = new List<float>();
        private readonly List<float> cortesY = new List<float>();
        private readonly Rect[] huecos = new Rect[MaxHuecos];
        private int cantidadHuecos;

        /// <summary>Cuántas celdas oscuras están encendidas (pruebas).</summary>
        public int CeldasActivas { get; private set; }

        /// <summary>True si la guía oscurece y bloquea la pantalla.</summary>
        public bool Bloqueando => CeldasActivas > 0;

        public GuiaTutorialTino(Transform padre)
        {
            raiz = TrazoH10.Nodo(padre, "TutorialTino");
            TrazoH10.Estirar(raiz);
            // Canvas propio por encima de la pantalla de compra (PantallaTienda, orden 30000) y de la de mejoras: si no, el oscurecido solo
            // tapaba las pestañas del banco y se podía usar todo lo demás.
            Canvas propio = raiz.gameObject.AddComponent<Canvas>();
            propio.overrideSorting = true;
            propio.sortingOrder = OrdenCanvas;
            raiz.gameObject.AddComponent<GraphicRaycaster>();
            for (int i = 0; i < celdas.Length; i++)
            {
                celdas[i] = TrazoH10.Imagen(raiz, "Oscuro" + i, new Color(0f, 0f, 0f, AlfaOscuro));
                celdas[i].raycastTarget = true;
                Anclar(celdas[i].rectTransform);
            }
            for (int h = 0; h < MaxHuecos; h++)
            {
                bordes[h] = new Image[4];
                for (int b = 0; b < 4; b++)
                {
                    bordes[h][b] = TrazoH10.Imagen(raiz, "Borde" + h + "_" + b, TrazoH10.Amarillo);
                    Anclar(bordes[h][b].rectTransform);
                }
            }
            etiqueta = TrazoH10.Nodo(raiz, "Etiqueta");
            Anclar(etiqueta);
            etiqueta.sizeDelta = new Vector2(AnchoEtiqueta, AltoEtiqueta);
            Image fondo = TrazoH10.Imagen(etiqueta, "Fondo", TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0.95f), TrazoH10.Relleno);
            TrazoH10.Estirar(fondo.rectTransform);
            Image marco = TrazoH10.Imagen(etiqueta, "Marco", TrazoH10.Amarillo, TrazoH10.Linea);
            TrazoH10.Estirar(marco.rectTransform);
            texto = TrazoH10.Etiqueta(etiqueta, "Texto", string.Empty, TrazoH10.TamanoSeccion, TrazoH10.Amarillo, TextAnchor.MiddleCenter);
            TrazoH10.Estirar(texto.rectTransform);
            VestidoH10.Fuente(etiqueta);
            Apagar();
        }

        private static void Anclar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
        }

        /// <summary>Apaga toda la guía.</summary>
        public void Apagar()
        {
            CeldasActivas = 0;
            cantidadHuecos = 0;
            foreach (Image c in celdas)
            {
                c.gameObject.SetActive(false);
            }
            foreach (Image[] grupo in bordes)
            {
                foreach (Image b in grupo)
                {
                    b.gameObject.SetActive(false);
                }
            }
            etiqueta.gameObject.SetActive(false);
        }

        /// <summary>Dibuja la guía: oscuro con huecos en <paramref name="objetivos"/>, bordes que pulsan y la <paramref name="linea"/> de instrucción.</summary>
        public void Actualizar(IReadOnlyList<RectTransform> objetivos, string linea, float tiempo)
        {
            Apagar();
            Rect area = raiz.rect;
            if (area.width < 1f || area.height < 1f)
            {
                return;
            }
            for (int i = 0; i < objetivos.Count && cantidadHuecos < MaxHuecos; i++)
            {
                if (objetivos[i] != null && TryRectLocal(objetivos[i], area, out Rect r))
                {
                    huecos[cantidadHuecos++] = r;
                }
            }
            texto.text = linea;
            etiqueta.gameObject.SetActive(true);
            if (cantidadHuecos == 0)
            {
                // Sin control a la vista: solo la etiqueta, arriba al centro, y nada de oscuro.
                etiqueta.anchoredPosition = new Vector2((area.width - AnchoEtiqueta) * 0.5f, area.height - AltoEtiqueta - 90f);
                return;
            }
            Oscurecer(area);
            float pulso = Mathf.Sin(tiempo * VelocidadPulso) * 0.5f + 0.5f;
            for (int h = 0; h < cantidadHuecos; h++)
            {
                PintarBorde(h, huecos[h], pulso);
            }
            ColocarEtiqueta(area, huecos[0]);
        }

        private bool TryRectLocal(RectTransform objetivo, Rect area, out Rect resultado)
        {
            resultado = default;
            Canvas canvas = objetivo.GetComponentInParent<Canvas>();
            Camera camara = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector3[] esquinas = new Vector3[4];
            objetivo.GetWorldCorners(esquinas);
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector2 pantalla = RectTransformUtility.WorldToScreenPoint(camara, esquinas[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, pantalla, null, out Vector2 local))
                {
                    return false;
                }
                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);
                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
            }
            minX = Mathf.Max(0f, minX - area.xMin - HolguraBorde);
            minY = Mathf.Max(0f, minY - area.yMin - HolguraBorde);
            maxX = Mathf.Min(area.width, maxX - area.xMin + HolguraBorde);
            maxY = Mathf.Min(area.height, maxY - area.yMin + HolguraBorde);
            if (maxX <= minX || maxY <= minY)
            {
                return false;
            }
            resultado = new Rect(minX, minY, maxX - minX, maxY - minY);
            return true;
        }

        // Corta la pantalla con las líneas de los huecos y oscurece cada celda que no cae dentro de un hueco.
        private void Oscurecer(Rect area)
        {
            cortesX.Clear();
            cortesY.Clear();
            cortesX.Add(0f);
            cortesX.Add(area.width);
            cortesY.Add(0f);
            cortesY.Add(area.height);
            for (int h = 0; h < cantidadHuecos; h++)
            {
                cortesX.Add(huecos[h].xMin);
                cortesX.Add(huecos[h].xMax);
                cortesY.Add(huecos[h].yMin);
                cortesY.Add(huecos[h].yMax);
            }
            cortesX.Sort();
            cortesY.Sort();
            int usadas = 0;
            for (int i = 0; i < cortesX.Count - 1; i++)
            {
                for (int j = 0; j < cortesY.Count - 1; j++)
                {
                    float ancho = cortesX[i + 1] - cortesX[i];
                    float alto = cortesY[j + 1] - cortesY[j];
                    if (ancho < TamanoMinimoCelda || alto < TamanoMinimoCelda || usadas >= celdas.Length)
                    {
                        continue;
                    }
                    Vector2 centro = new Vector2(cortesX[i] + ancho * 0.5f, cortesY[j] + alto * 0.5f);
                    if (EnAlgunHueco(centro))
                    {
                        continue;
                    }
                    Image celda = celdas[usadas++];
                    celda.gameObject.SetActive(true);
                    celda.rectTransform.anchoredPosition = new Vector2(cortesX[i], cortesY[j]);
                    celda.rectTransform.sizeDelta = new Vector2(ancho, alto);
                }
            }
            CeldasActivas = usadas;
        }

        private bool EnAlgunHueco(Vector2 punto)
        {
            for (int h = 0; h < cantidadHuecos; h++)
            {
                if (huecos[h].Contains(punto))
                {
                    return true;
                }
            }
            return false;
        }

        private void PintarBorde(int h, Rect r, float pulso)
        {
            float extra = pulso * AmplitudPulso;
            float x0 = r.xMin - extra;
            float y0 = r.yMin - extra;
            float ancho = r.width + 2f * extra;
            float alto = r.height + 2f * extra;
            Color color = TrazoH10.ConAlfa(TrazoH10.Amarillo, Mathf.Lerp(AlfaMinimoBorde, 1f, pulso));
            Colocar(bordes[h][0], x0, y0, ancho, GrosorBorde, color);
            Colocar(bordes[h][1], x0, y0 + alto - GrosorBorde, ancho, GrosorBorde, color);
            Colocar(bordes[h][2], x0, y0, GrosorBorde, alto, color);
            Colocar(bordes[h][3], x0 + ancho - GrosorBorde, y0, GrosorBorde, alto, color);
        }

        private static void Colocar(Image barra, float x, float y, float ancho, float alto, Color color)
        {
            barra.gameObject.SetActive(true);
            barra.color = color;
            barra.rectTransform.anchoredPosition = new Vector2(x, y);
            barra.rectTransform.sizeDelta = new Vector2(ancho, alto);
        }

        // Encima del hueco si cabe; si no, debajo.
        private void ColocarEtiqueta(Rect area, Rect hueco)
        {
            float x = Mathf.Clamp(hueco.center.x - AnchoEtiqueta * 0.5f, 0f, Mathf.Max(0f, area.width - AnchoEtiqueta));
            float yArriba = hueco.yMax + SeparacionEtiqueta;
            float y = yArriba + AltoEtiqueta <= area.height ? yArriba : Mathf.Max(0f, hueco.yMin - SeparacionEtiqueta - AltoEtiqueta);
            etiqueta.anchoredPosition = new Vector2(x, y);
        }
    }
}

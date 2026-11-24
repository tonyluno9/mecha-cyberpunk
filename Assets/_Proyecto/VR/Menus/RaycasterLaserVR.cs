using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR4: el raycast del láser contra los Canvas pasados a World Space. Busca con el rayo 3D del control (no con una posición de pantalla),
    /// respeta raycastTarget, CanvasGroup, máscaras y el orden de dibujo, y da a los eventos una cámara propia (apagada) pegada al control
    /// para que deslizadores y listas conviertan bien la posición. Al EventSystem del juego (ratón) no le da nada.
    /// </summary>
    public class RaycasterLaserVR : BaseRaycaster
    {
        private Camera camara;

        /// <summary>La cámara de eventos: apagada, en la pose del control y mirando por el rayo.</summary>
        public override Camera eventCamera => camara;

        public void Configurar(Camera camaraLaser)
        {
            camara = camaraLaser;
        }

        // El módulo de entrada del juego (ratón) también llama aquí: el láser no le devuelve nada.
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
        }

        /// <summary>
        /// El gráfico de más arriba que toca el rayo en estos Canvas (y sus anidados), hasta <paramref name="distanciaMaxima"/> m.
        /// </summary>
        public bool Lanzar(Ray rayo, float distanciaMaxima, List<LienzoConvertidoVR> lienzos, out RaycastResult resultado)
        {
            resultado = default;
            bool hay = false;
            int mejorCapa = int.MinValue;
            int mejorOrden = int.MinValue;
            int mejorProfundidad = int.MinValue;
            float mejorDistancia = float.MaxValue;
            if (camara == null)
            {
                return false;
            }
            foreach (LienzoConvertidoVR l in lienzos)
            {
                if (l.Canvas == null || l.Tipo != TipoLienzoVR.Frente || !l.Canvas.isActiveAndEnabled)
                {
                    continue;
                }
                foreach (Canvas c in l.Lienzos)
                {
                    if (c == null || !c.isActiveAndEnabled)
                    {
                        continue;
                    }
                    int capa = SortingLayer.GetLayerValueFromID(c.sortingLayerID);
                    int orden = OrdenDe(c);
                    IList<Graphic> graficos = GraphicRegistry.GetRaycastableGraphicsForCanvas(c);
                    for (int i = 0; i < graficos.Count; i++)
                    {
                        Graphic g = graficos[i];
                        if (!Toca(g, rayo, distanciaMaxima, out float distancia, out Vector3 punto, out Vector2 pantalla))
                        {
                            continue;
                        }
                        bool mejor = !hay || capa > mejorCapa || (capa == mejorCapa && (orden > mejorOrden
                            || (orden == mejorOrden && (g.depth > mejorProfundidad || (g.depth == mejorProfundidad && distancia < mejorDistancia)))));
                        if (!mejor)
                        {
                            continue;
                        }
                        hay = true;
                        mejorCapa = capa;
                        mejorOrden = orden;
                        mejorProfundidad = g.depth;
                        mejorDistancia = distancia;
                        resultado = new RaycastResult
                        {
                            gameObject = g.gameObject,
                            module = this,
                            distance = distancia,
                            worldPosition = punto,
                            worldNormal = -g.rectTransform.forward,
                            screenPosition = pantalla,
                            index = 0,
                            depth = g.depth,
                            sortingLayer = c.sortingLayerID,
                            sortingOrder = orden,
                        };
                    }
                }
            }
            // VR15: la pantalla grande curva de Inicio (sus Canvas van en una textura): gana si no hay un panel más cerca.
            InicioCineVR inicio = InicioCineVR.Actual;
            if (inicio != null && inicio.Lanzar(rayo, distanciaMaxima, out RaycastResult enPantalla)
                && (!hay || enPantalla.distance < resultado.distance))
            {
                resultado = enPantalla;
                hay = true;
            }
            return hay;
        }

        /// <summary>
        /// VR44: el orden de dibujo con el que cuenta un Canvas: el suyo si es raíz o lo sobreescribe; si no, el de su raíz
        /// (un anidado sin orden propio se dibuja con su raíz).
        /// </summary>
        public static int OrdenDe(Canvas c)
        {
            if (c == null)
            {
                return int.MinValue;
            }
            return c.isRootCanvas || c.overrideSorting || c.rootCanvas == null ? c.sortingOrder : c.rootCanvas.sortingOrder;
        }

        private bool Toca(Graphic g, Ray rayo, float distanciaMaxima, out float distancia, out Vector3 punto, out Vector2 pantalla)
        {
            distancia = 0f;
            punto = Vector3.zero;
            pantalla = Vector2.zero;
            if (g == null || !g.raycastTarget || !g.isActiveAndEnabled || g.depth == -1 || g.canvasRenderer.cull)
            {
                return false;
            }
            RectTransform rt = g.rectTransform;
            Vector3 normal = rt.forward;
            float denominador = Vector3.Dot(normal, rayo.direction);
            // Solo por delante (el panel mira en la misma dirección que el rayo).
            if (denominador <= 1e-5f)
            {
                return false;
            }
            distancia = Vector3.Dot(rt.position - rayo.origin, normal) / denominador;
            if (distancia < 0f || distancia > distanciaMaxima)
            {
                return false;
            }
            punto = rayo.origin + rayo.direction * distancia;
            Vector3 local = rt.InverseTransformPoint(punto);
            if (!rt.rect.Contains(new Vector2(local.x, local.y)))
            {
                return false;
            }
            pantalla = camara.WorldToScreenPoint(punto);
            // Filtros del juego: CanvasGroup (blocksRaycasts), máscaras, alfa de la imagen.
            return g.Raycast(pantalla, camara);
        }
    }
}

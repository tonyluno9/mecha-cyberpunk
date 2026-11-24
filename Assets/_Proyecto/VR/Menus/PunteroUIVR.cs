using UnityEngine;
using UnityEngine.EventSystems;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR4: manda a la UI los eventos de un puntero (el láser), como el módulo de entrada de uGUI: entrar/salir, bajar/subir, clic y arrastre.
    /// Va aparte del EventSystem del juego (que sigue con el ratón y el teclado): no compite con su InputSystemUIInputModule.
    /// </summary>
    public sealed class PunteroUIVR
    {
        /// <summary>Id del puntero del láser (los del ratón y el tacto son otros).</summary>
        public const int IdPuntero = -73;
        /// <summary>Lo que hay que mover el punto en el panel (m) para que empiece un arrastre.</summary>
        public const float UmbralArrastre = 0.01f;

        private PointerEventData datos;
        private EventSystem sistema;
        private bool pulsado;
        private Vector3 puntoPulsado;

        /// <summary>El objeto bajo el láser, o null.</summary>
        public GameObject Encima => datos != null ? datos.pointerEnter : null;

        /// <summary>True mientras el gatillo está abajo sobre la UI.</summary>
        public bool Pulsado => pulsado;

        /// <summary>True si el objeto (o un padre) responde a clic, bajar, arrastre o rueda.</summary>
        public static bool EsInteractivo(GameObject objeto)
        {
            return objeto != null && (ExecuteEvents.GetEventHandler<IPointerClickHandler>(objeto) != null
                || ExecuteEvents.GetEventHandler<IPointerDownHandler>(objeto) != null
                || ExecuteEvents.GetEventHandler<IDragHandler>(objeto) != null
                || ExecuteEvents.GetEventHandler<IScrollHandler>(objeto) != null);
        }

        private void Asegurar()
        {
            EventSystem actual = EventSystem.current;
            if (datos == null || sistema != actual)
            {
                sistema = actual;
                datos = new PointerEventData(actual) { pointerId = IdPuntero, button = PointerEventData.InputButton.Left };
            }
        }

        /// <summary>Un cuadro del puntero: lo que toca el rayo (si toca) y el gatillo.</summary>
        public void Procesar(bool hay, RaycastResult resultado, bool presiono, bool solto)
        {
            Asegurar();
            datos.Reset();
            datos.button = PointerEventData.InputButton.Left;
            Vector2 posicion = hay ? resultado.screenPosition : datos.position;
            datos.delta = posicion - datos.position;
            datos.position = posicion;
            datos.pointerCurrentRaycast = hay ? resultado : default;
            GameObject objetivo = hay ? resultado.gameObject : null;

            CambiarEncima(objetivo);
            if (presiono && !pulsado)
            {
                Presionar(objetivo, resultado);
            }
            if (pulsado && datos.pointerDrag != null)
            {
                Arrastrar(hay, resultado);
            }
            if (solto && pulsado)
            {
                Soltar(objetivo);
            }
        }

        /// <summary>El láser se apagó: sale de lo que tenía encima y suelta lo pulsado sin clic.</summary>
        public void Liberar()
        {
            if (datos == null)
            {
                return;
            }
            if (pulsado)
            {
                Soltar(null);
            }
            CambiarEncima(null);
        }

        private void CambiarEncima(GameObject nuevo)
        {
            if (nuevo == null || datos.pointerEnter == null)
            {
                for (int i = datos.hovered.Count - 1; i >= 0; i--)
                {
                    if (datos.hovered[i] != null)
                    {
                        ExecuteEvents.Execute(datos.hovered[i], datos, ExecuteEvents.pointerExitHandler);
                    }
                }
                datos.hovered.Clear();
                if (nuevo == null)
                {
                    datos.pointerEnter = null;
                    return;
                }
            }
            if (datos.pointerEnter == nuevo)
            {
                return;
            }
            Transform comun = RaizComun(nuevo, datos.pointerEnter);
            if (datos.pointerEnter != null)
            {
                Transform t = datos.pointerEnter.transform;
                while (t != null && t != comun)
                {
                    ExecuteEvents.Execute(t.gameObject, datos, ExecuteEvents.pointerExitHandler);
                    datos.hovered.Remove(t.gameObject);
                    t = t.parent;
                }
            }
            datos.pointerEnter = nuevo;
            Transform u = nuevo.transform;
            while (u != null && u != comun)
            {
                ExecuteEvents.Execute(u.gameObject, datos, ExecuteEvents.pointerEnterHandler);
                datos.hovered.Add(u.gameObject);
                u = u.parent;
            }
        }

        private static Transform RaizComun(GameObject a, GameObject b)
        {
            if (a == null || b == null)
            {
                return null;
            }
            for (Transform t = a.transform; t != null; t = t.parent)
            {
                if (b.transform.IsChildOf(t))
                {
                    return t;
                }
            }
            return null;
        }

        private void Presionar(GameObject objetivo, RaycastResult resultado)
        {
            pulsado = true;
            datos.eligibleForClick = true;
            datos.delta = Vector2.zero;
            datos.dragging = false;
            datos.useDragThreshold = true;
            datos.pressPosition = datos.position;
            datos.pointerPressRaycast = resultado;
            puntoPulsado = resultado.worldPosition;
            if (objetivo == null)
            {
                datos.pointerPress = null;
                datos.rawPointerPress = null;
                datos.pointerDrag = null;
                return;
            }
            GameObject bajo = ExecuteEvents.ExecuteHierarchy(objetivo, datos, ExecuteEvents.pointerDownHandler);
            GameObject clic = ExecuteEvents.GetEventHandler<IPointerClickHandler>(objetivo);
            datos.pointerPress = bajo != null ? bajo : clic;
            datos.rawPointerPress = objetivo;
            datos.clickTime = Time.unscaledTime;
            datos.clickCount = 1;
            datos.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(objetivo);
            if (datos.pointerDrag != null)
            {
                ExecuteEvents.Execute(datos.pointerDrag, datos, ExecuteEvents.initializePotentialDrag);
            }
        }

        private void Arrastrar(bool hay, RaycastResult resultado)
        {
            if (!datos.dragging && hay && Vector3.Distance(resultado.worldPosition, puntoPulsado) > UmbralArrastre)
            {
                ExecuteEvents.Execute(datos.pointerDrag, datos, ExecuteEvents.beginDragHandler);
                datos.dragging = true;
            }
            if (!datos.dragging)
            {
                return;
            }
            if (datos.pointerPress != null && datos.pointerPress != datos.pointerDrag)
            {
                ExecuteEvents.Execute(datos.pointerPress, datos, ExecuteEvents.pointerUpHandler);
                datos.eligibleForClick = false;
                datos.pointerPress = null;
                datos.rawPointerPress = null;
            }
            ExecuteEvents.Execute(datos.pointerDrag, datos, ExecuteEvents.dragHandler);
        }

        private void Soltar(GameObject objetivo)
        {
            if (datos.pointerPress != null)
            {
                ExecuteEvents.Execute(datos.pointerPress, datos, ExecuteEvents.pointerUpHandler);
            }
            GameObject clic = objetivo != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(objetivo) : null;
            if (datos.pointerPress != null && datos.pointerPress == clic && datos.eligibleForClick)
            {
                ExecuteEvents.Execute(datos.pointerPress, datos, ExecuteEvents.pointerClickHandler);
            }
            else if (datos.dragging && objetivo != null)
            {
                ExecuteEvents.ExecuteHierarchy(objetivo, datos, ExecuteEvents.dropHandler);
            }
            if (datos.pointerDrag != null && datos.dragging)
            {
                ExecuteEvents.Execute(datos.pointerDrag, datos, ExecuteEvents.endDragHandler);
            }
            datos.eligibleForClick = false;
            datos.pointerPress = null;
            datos.rawPointerPress = null;
            datos.pointerDrag = null;
            datos.dragging = false;
            pulsado = false;
        }
    }
}

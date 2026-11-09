using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>Encendido común de casillas y botones: con el puntero encima o con foco, la línea se ilumina y la casilla crece un poco.</summary>
    public class HoverH10 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public const float EscalaHover = 1.02f;

        private Graphic linea;
        private Color reposo;
        private Color encendido;
        private RectTransform rect;

        public bool Encendido { get; private set; }

        public static HoverH10 Agregar(GameObject objeto, Graphic linea, Color reposo, Color encendido)
        {
            HoverH10 hover = objeto.AddComponent<HoverH10>();
            hover.linea = linea;
            hover.reposo = reposo;
            hover.encendido = encendido;
            hover.rect = (RectTransform)objeto.transform;
            return hover;
        }

        public void OnPointerEnter(PointerEventData eventData) => Fijar(true);

        public void OnPointerExit(PointerEventData eventData) => Fijar(false);

        public void OnSelect(BaseEventData eventData) => Fijar(true);

        public void OnDeselect(BaseEventData eventData) => Fijar(false);

        private void OnDisable()
        {
            Encendido = false;
            if (linea != null)
            {
                linea.color = reposo;
            }
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }

        private void Fijar(bool valor)
        {
            Encendido = valor;
            if (linea != null)
            {
                AnimH10.Tenir(linea, valor ? encendido : reposo);
            }
            AnimH10.Escala(rect, valor ? EscalaHover : 1f);
        }
    }
}

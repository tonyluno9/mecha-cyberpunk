using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// VR44/VR50: resalta el botón del menú del campo de tiro que está bajo el láser de VR (o el ratón): el fondo se ilumina de turquesa y la casilla
    /// crece un poco, como en la máquina expendedora. Solo toca el fondo: la línea y el texto siguen marcando en amarillo lo elegido.
    /// </summary>
    public class ResaltadoBotonCampoTiro : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public const float EscalaResaltado = 1.06f;
        private const float AlfaResaltado = 0.45f;

        private Graphic fondo;
        private Color reposo;
        private RectTransform rect;

        /// <summary>True mientras está resaltado.</summary>
        public bool Resaltado { get; private set; }

        public static ResaltadoBotonCampoTiro Agregar(GameObject objeto, Graphic fondo)
        {
            ResaltadoBotonCampoTiro resaltado = objeto.AddComponent<ResaltadoBotonCampoTiro>();
            resaltado.fondo = fondo;
            resaltado.reposo = fondo != null ? fondo.color : Color.clear;
            resaltado.rect = (RectTransform)objeto.transform;
            return resaltado;
        }

        public void OnPointerEnter(PointerEventData eventData) => Fijar(true);

        public void OnPointerExit(PointerEventData eventData) => Fijar(false);

        public void OnSelect(BaseEventData eventData) => Fijar(true);

        public void OnDeselect(BaseEventData eventData) => Fijar(false);

        private void OnDisable()
        {
            Resaltado = false;
            if (fondo != null)
            {
                fondo.color = reposo;
            }
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }

        private void Fijar(bool valor)
        {
            Resaltado = valor;
            if (fondo != null)
            {
                AnimH10.Tenir(fondo, valor ? TrazoH10.ConAlfa(TrazoH10.Turquesa, AlfaResaltado) : reposo);
            }
            if (rect != null)
            {
                AnimH10.Escala(rect, valor ? EscalaResaltado : 1f);
            }
        }
    }
}

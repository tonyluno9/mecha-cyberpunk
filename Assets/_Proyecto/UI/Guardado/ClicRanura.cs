using UnityEngine;
using UnityEngine.EventSystems;

namespace MechaCyberpunk.UI.Guardado
{
    /// <summary>
    /// T50: recuerda cómo se pulsó una fila de ranura: si fue un clic de ratón y cuántos seguidos (1 = clic, 2 = doble clic). Enter, A del control y
    /// cualquier otra pulsación no dejan marca, así que cuentan como "aceptar".
    /// </summary>
    public class ClicRanura : MonoBehaviour, IPointerDownHandler
    {
        private int cuadro = -1;
        private int cantidad;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                cuadro = Time.frameCount;
                cantidad = eventData.clickCount;
            }
        }

        /// <summary>True si esta fila se acaba de pulsar con UN solo clic de ratón.</summary>
        public static bool EsClicSimple(Component fila)
        {
            ClicRanura clic = fila != null ? fila.GetComponent<ClicRanura>() : null;
            if (clic == null || clic.cuadro < 0)
            {
                return false;
            }
            bool simple = Time.frameCount - clic.cuadro <= 120 && clic.cantidad <= 1;
            clic.cuadro = -1;
            return simple;
        }
    }
}

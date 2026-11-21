using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// El fondo de la pantalla de carga: negro y, encima, las ilustraciones L01-L06 (<see cref="FotosCarga"/>) con fundido entre ellas.
    /// Pantalla de carga minimalista (Tony, carga): sin estelas, manchas de luz, barrido ni rejilla; solo la imagen.
    /// </summary>
    public class FondoCarga : MonoBehaviour
    {
        private FotosCarga fotos;

        /// <summary>T-1316: las fotos L01-L06 de la carga (null si no hay).</summary>
        public FotosCarga Fotos => fotos;

        public static FondoCarga Crear(Transform padre)
        {
            RectTransform raiz = UIInicio.Nodo(padre, "FondoCarga");
            UIInicio.Estirar(raiz);
            FondoCarga fondo = raiz.gameObject.AddComponent<FondoCarga>();
            // Negro detrás: si una foto no llena la pantalla, los bordes quedan negros (nunca se deforma).
            Image negro = UIInicio.Imagen(raiz, "Negro", Color.black);
            UIInicio.Estirar(negro.rectTransform);
            fondo.fotos = FotosCarga.Crear(raiz);
            return fondo;
        }
    }
}

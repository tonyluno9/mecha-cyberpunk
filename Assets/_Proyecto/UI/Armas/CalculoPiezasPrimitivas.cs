using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>
    /// T-942: colocación y tamaños del Cargador (una caja) y del Cañón (un cilindro) holográficos que el banco arma con primitivas en las
    /// armas cuyo modelo no trae esas piezas. Cálculo puro: el arma va de lado (su largo sobre X) y todo es proporcional a ese largo.
    /// </summary>
    public static class CalculoPiezasPrimitivas
    {
        /// <summary>Largo de la caja del cargador sobre el largo del arma.</summary>
        public const float CargadorLargo = 0.1f;
        /// <summary>Alto de la caja del cargador sobre el largo del arma.</summary>
        public const float CargadorAlto = 0.16f;
        /// <summary>Ancho (profundidad) de la caja del cargador sobre el largo del arma.</summary>
        public const float CargadorAncho = 0.07f;
        /// <summary>Qué tanto se hunde la caja en el arma, como fracción de su alto.</summary>
        public const float CargadorHundido = 0.6f;
        /// <summary>Dónde cae el cargador sobre el largo: 0 el centro, 0.5 la punta del lado de la boca (negativo, hacia atrás).</summary>
        public const float CargadorDesfase = -0.02f;
        /// <summary>Largo del cilindro del cañón sobre el largo del arma.</summary>
        public const float CanonLargo = 0.2f;
        /// <summary>Radio del cilindro del cañón sobre el largo del arma.</summary>
        public const float CanonRadio = 0.03f;

        /// <summary>Caja del cargador: centro y tamaño en el mismo espacio que <paramref name="arma"/>.</summary>
        public static Bounds Cargador(Bounds arma, float signoFrente)
        {
            float largo = arma.size.x;
            Vector3 tamano = new Vector3(largo * CargadorLargo, largo * CargadorAlto, largo * CargadorAncho);
            float arriba = arma.min.y + tamano.y * CargadorHundido;
            Vector3 centro = new Vector3(arma.center.x + signoFrente * largo * CargadorDesfase, arriba - tamano.y * 0.5f, arma.center.z);
            return new Bounds(centro, tamano);
        }

        /// <summary>Cilindro del cañón con su eje sobre X, terminando en la boca. Devuelve centro, radio y largo.</summary>
        public static void Canon(Bounds arma, Vector3 boca, out Vector3 centro, out float radio, out float largoCilindro)
        {
            float largo = arma.size.x;
            float signo = boca.x >= arma.center.x ? 1f : -1f;
            largoCilindro = largo * CanonLargo;
            radio = largo * CanonRadio;
            centro = new Vector3(boca.x - signo * largoCilindro * 0.5f, boca.y, boca.z);
        }

        /// <summary>Hacia dónde mira el frente del arma (+1 o -1 sobre X) según la boca.</summary>
        public static float SignoFrente(Bounds arma, Vector3 boca) => boca.x >= arma.center.x ? 1f : -1f;

        /// <summary>
        /// Limita un desplazamiento vertical hacia abajo para que la pieza (con su parte más baja en <paramref name="minimoPieza"/>) no baje de
        /// <paramref name="techoPedestal"/> más el margen. Devuelve el factor (0 a 1) que se aplica a todo el desplazamiento.
        /// </summary>
        public static float FactorSobrePedestal(float desplazamientoY, float minimoPieza, float techoPedestal, float margen)
        {
            if (desplazamientoY >= 0f)
            {
                return 1f;
            }
            float cabe = minimoPieza - techoPedestal - margen;
            return Mathf.Clamp01(cabe / -desplazamientoY);
        }
    }
}

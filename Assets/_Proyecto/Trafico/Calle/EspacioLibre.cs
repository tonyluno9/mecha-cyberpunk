using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-728: nunca se hace aparecer un vehículo encima de otro (un carro del tráfico, de refuerzo, del asalto o una patrulla sobre una moto, un carro robado, uno estacionado o el jugador).
    /// Antes de poner un vehículo en un punto se revisa con una caja (OverlapBox) que no haya ningún collider sólido, sea de lo que sea. La caja empieza un poco sobre el piso para que
    /// la calle y la banqueta no cuenten. Los triggers no cuentan.
    /// </summary>
    public static class EspacioLibre
    {
        /// <summary>Mitad del lado (m) de la caja de espacio libre: cubre un carro de 5 m en cualquier giro más el carril.</summary>
        public const float MitadLado = 3.2f;

        /// <summary>Qué tan arriba del piso (m) empieza la caja: lo que sea más bajo (calle, banqueta, rejilla) no estorba.</summary>
        public const float SobreElPiso = 0.4f;

        /// <summary>Alto (m) de la caja desde <see cref="SobreElPiso"/>: una moto o un carro entran, un techo o un cartel alto no.</summary>
        public const float Alto = 1.4f;

        private static readonly Collider[] Encontrados = new Collider[32];

        /// <summary>
        /// True si no hay nada sólido en el lugar donde aparecería un vehículo en <paramref name="piso"/> (punto sobre el piso).
        /// <paramref name="ignorar"/> es el vehículo que se va a poner (sus colliders no cuentan). <paramref name="mitadLado"/> cambia el tamaño de la caja.
        /// </summary>
        public static bool Libre(Vector3 piso, Transform ignorar = null, float mitadLado = MitadLado)
        {
            return Estorbo(piso, ignorar, mitadLado) == null;
        }

        /// <summary>El primer collider sólido que estorba (el que le impediría aparecer), o null si el lugar está libre.</summary>
        public static Collider Estorbo(Vector3 piso, Transform ignorar = null, float mitadLado = MitadLado)
        {
            Vector3 centro = piso + Vector3.up * (SobreElPiso + Alto * 0.5f);
            Vector3 mitad = new Vector3(mitadLado, Alto * 0.5f, mitadLado);
            int n = Physics.OverlapBoxNonAlloc(centro, mitad, Encontrados, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = Encontrados[i];
                if (c == null || !c.enabled)
                {
                    continue;
                }
                if (ignorar != null && c.transform.IsChildOf(ignorar))
                {
                    continue;
                }
                return c;
            }
            return null;
        }
    }
}

using UnityEngine;

namespace MechaCyberpunk.UI.Tiendas
{
    /// <summary>
    /// T-1529: cuentas puras de la tiendita de la esquina (edificio ED02): hacia dónde da su frente, dónde van el mostrador y el tendero respecto al
    /// edificio y qué sitio de la lista se escoge. Sin escena ni física: las pruebas EditMode las cubren.
    /// </summary>
    public static class ReglaTiendita
    {
        /// <summary>Metros entre la pared del frente y el mostrador.</summary>
        public const float SeparacionMostrador = 1.3f;
        /// <summary>Metros que el tendero queda detrás del mostrador, hacia la pared.</summary>
        public const float SeparacionTendero = 0.9f;
        /// <summary>Margen extra (m) que se revisa alrededor del edificio y del mostrador al buscar un sitio libre.</summary>
        public const float MargenLibre = 0.5f;

        /// <summary>El frente del edificio en el mundo: el -Z local (como las máquinas CY06) girado <paramref name="giro"/> grados en y.</summary>
        public static Vector3 Frente(float giro) => Quaternion.Euler(0f, giro, 0f) * Vector3.back;

        /// <summary>Metros del centro del edificio a su pared del frente, con la caja de la que se conocen sus medidas medias (<paramref name="mitad"/>, en el mundo).</summary>
        public static float DistanciaAlFrente(Vector3 frente, Vector3 mitad) => Mathf.Abs(frente.x) * mitad.x + Mathf.Abs(frente.z) * mitad.z;

        /// <summary>Dónde va el mostrador: delante de la pared del frente, a la altura del piso del sitio.</summary>
        public static Vector3 PosicionMostrador(Vector3 centro, Vector3 frente, Vector3 mitad, float suelo)
        {
            Vector3 p = centro + frente * (DistanciaAlFrente(frente, mitad) + SeparacionMostrador);
            p.y = suelo;
            return p;
        }

        /// <summary>Dónde va el tendero: del lado de la pared del mostrador, mirando hacia afuera.</summary>
        public static Vector3 PosicionTendero(Vector3 centro, Vector3 frente, Vector3 mitad, float suelo)
        {
            Vector3 p = centro + frente * (DistanciaAlFrente(frente, mitad) + SeparacionMostrador - SeparacionTendero);
            p.y = suelo;
            return p;
        }

        /// <summary>Índice del primer sitio libre; si ninguno lo está, el primero (se coloca igual y se avisa). -1 si no hay sitios.</summary>
        public static int ElegirSitio(bool[] libres)
        {
            if (libres == null || libres.Length == 0)
            {
                return -1;
            }
            for (int i = 0; i < libres.Length; i++)
            {
                if (libres[i])
                {
                    return i;
                }
            }
            return 0;
        }
    }
}

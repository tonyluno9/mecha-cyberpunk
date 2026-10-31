using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Cuentas puras de los negocios de tus zonas (T-429): ingreso por día y precios según el nivel. Nivel 0 = no es tuyo todavía.</summary>
    public static class ReglaNegocios
    {
        /// <summary>Biits por día de un negocio: base × multiplicador de su nivel (nivel 0 no paga).</summary>
        public static int Ingreso(int ingresoBase, int nivel, float[] multiplicadores)
        {
            if (nivel <= 0 || multiplicadores == null || multiplicadores.Length == 0)
            {
                return 0;
            }
            float m = multiplicadores[Mathf.Min(nivel, multiplicadores.Length) - 1];
            return Mathf.RoundToInt(Mathf.Max(0, ingresoBase) * Mathf.Max(0f, m));
        }

        /// <summary>Lo que cuesta pasar del nivel actual al siguiente (nivel 0 = tomarlo); 0 si ya está al máximo.</summary>
        public static int PrecioSiguiente(int precioTomar, int nivel, float[] factoresMejora, int nivelMaximo)
        {
            if (nivel >= nivelMaximo)
            {
                return 0;
            }
            if (nivel <= 0)
            {
                return Mathf.Max(1, precioTomar);
            }
            float f = factoresMejora != null && nivel - 1 < factoresMejora.Length ? factoresMejora[nivel - 1] : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(precioTomar * f));
        }

        /// <summary>Días que tarda un negocio en pagar lo invertido (para equilibrar precios).</summary>
        public static float DiasParaRecuperar(int invertido, int ingresoDia) => ingresoDia <= 0 ? float.PositiveInfinity : (float)invertido / ingresoDia;
    }
}

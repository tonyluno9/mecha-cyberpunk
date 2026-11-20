using System.Collections.Generic;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>Los tipos de arma que se cuentan para el límite de la rueda (T-1527).</summary>
    public enum TipoDeArma
    {
        CuerpoACuerpo,
        Pistola,
        Subfusil,
        Escopeta,
        Fusil,
        Francotirador,
        Pesada,
        Lanzada
    }

    /// <summary>
    /// T-1527: solo se lleva una arma por tipo. Pistola y revólver son del tipo Pistola; el fusil y el subfusil cada uno el suyo; el lanzagranadas y el rifle de plasma
    /// son Pesada; la granada es Lanzada; los puños son CuerpoACuerpo. Al conseguir otra del mismo tipo, reemplaza a la que se tenía (lógica pura).
    /// </summary>
    public static class ReglaTipoArma
    {
        public static TipoDeArma Tipo(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Pistola:
                case ArmaAPie.Revolver: return TipoDeArma.Pistola;
                case ArmaAPie.Subfusil: return TipoDeArma.Subfusil;
                case ArmaAPie.Escopeta: return TipoDeArma.Escopeta;
                case ArmaAPie.Fusil: return TipoDeArma.Fusil;
                case ArmaAPie.Sniper: return TipoDeArma.Francotirador;
                case ArmaAPie.Lanzagranadas:
                case ArmaAPie.RiflePlasma: return TipoDeArma.Pesada;
                case ArmaAPie.Granada: return TipoDeArma.Lanzada;
                default: return TipoDeArma.CuerpoACuerpo;
            }
        }

        public static bool MismoTipo(ArmaAPie a, ArmaAPie b) => Tipo(a) == Tipo(b);

        /// <summary>
        /// Las armas que se pierden al quedar con las que hay: de cada tipo con más de una en <paramref name="tenidas"/> se queda la
        /// que llegó al final (<paramref name="ordenLlegada"/> la más reciente al final de la lista) y las demás se devuelven.
        /// </summary>
        public static List<ArmaAPie> Reemplazadas(IReadOnlyList<ArmaAPie> ordenLlegada)
        {
            List<ArmaAPie> reemplazadas = new List<ArmaAPie>();
            for (int i = 0; i < ordenLlegada.Count; i++)
            {
                for (int j = i + 1; j < ordenLlegada.Count; j++)
                {
                    if (MismoTipo(ordenLlegada[i], ordenLlegada[j]) && !reemplazadas.Contains(ordenLlegada[i]))
                    {
                        reemplazadas.Add(ordenLlegada[i]);
                    }
                }
            }
            return reemplazadas;
        }
    }
}

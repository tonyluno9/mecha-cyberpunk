namespace MechaCyberpunk.Progresion.Apoyo
{
    /// <summary>
    /// Qué dice el aviso de cada personaje de apoyo y si se puede usar (T-417). Pura, sin Unity.
    /// </summary>
    public static class ReglaPersonajeApoyo
    {
        public const string TextoMejoras = "MEJORAS DE ARMAS";
        public const string TextoInformacion = "INFORMACIÓN";
        public const string TextoPlaticar = "PLATICAR";
        public const string TextoImplantes = "IMPLANTES";
        public const string TextoVidaLlena = "ESTÁS BIEN, MIJO";
        public const string TextoSinDinero = "NO TE ALCANZA";
        public const string TextoCarroLimpio = "TU CARRO ESTÁ LIMPIO";

        public readonly struct Aviso
        {
            public readonly string Texto;
            public readonly bool PuedeUsar;

            public Aviso(string texto, bool puedeUsar)
            {
                Texto = texto;
                PuedeUsar = puedeUsar;
            }
        }

        public static Aviso Evaluar(ServicioApoyo servicio, int costo, float vida, float vidaMaxima, int nivelCalor, int dinero)
        {
            switch (servicio)
            {
                case ServicioApoyo.Mejoras:
                    return new Aviso(TextoMejoras, true);
                case ServicioApoyo.Curar:
                    if (vida >= vidaMaxima)
                    {
                        return new Aviso(TextoVidaLlena, false);
                    }
                    return dinero < costo ? new Aviso(TextoSinDinero, false) : new Aviso("CURARTE · " + Core.Moneda.Texto(costo), true);
                case ServicioApoyo.Informacion:
                    return new Aviso(TextoInformacion, true);
                case ServicioApoyo.Taller:
                    if (nivelCalor <= 0)
                    {
                        return new Aviso(TextoCarroLimpio, false);
                    }
                    return dinero < costo ? new Aviso(TextoSinDinero, false) : new Aviso("PINTAR TU CARRO · " + Core.Moneda.Texto(costo), true);
                default:
                    return new Aviso(TextoPlaticar, true);
            }
        }

        /// <summary>
        /// EL DOC con la vida llena ofrece los implantes (T-430) si la clínica se puede usar; con la vida baja sigue curando.
        /// </summary>
        public static Aviso DocConImplantes(Aviso curar, float vida, float vidaMaxima, bool clinicaDisponible)
        {
            return vida >= vidaMaxima && clinicaDisponible ? new Aviso(TextoImplantes, true) : curar;
        }

        /// <summary>Una frase al azar (<paramref name="azar"/> de 0 a cantidad-1) que no es la última que dijo (-1: ninguna).</summary>
        public static int SiguienteFrase(int cantidad, int ultima, int azar)
        {
            if (cantidad <= 0)
            {
                return -1;
            }
            int indice = ((azar % cantidad) + cantidad) % cantidad;
            if (cantidad > 1 && indice == ultima)
            {
                indice = (indice + 1) % cantidad;
            }
            return indice;
        }

        /// <summary>La pista del hacker: el jefe si ya apareció, el respeto que falta si estás en zona rival, o una frase suya.</summary>
        public static string Pista(bool jefeAparecio, int metrosJefe, string zonaJefe, string zonaActual, int respeto, int umbral, bool zonaTuya, string frase)
        {
            if (jefeAparecio)
            {
                return "EL JEFE DE " + zonaJefe + " ANDA A " + metrosJefe + " M DE AQUÍ";
            }
            if (!string.IsNullOrEmpty(zonaActual) && !zonaTuya && respeto < umbral)
            {
                return "TE FALTAN " + (umbral - respeto) + " DE RESPETO EN " + zonaActual + " PARA QUE SALGA SU JEFE";
            }
            return frase;
        }
    }
}

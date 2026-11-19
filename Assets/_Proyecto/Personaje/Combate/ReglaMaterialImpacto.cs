using System;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1472: el material que golpea una bala y el ID de sonido de ese impacto. Pura, sin Unity: quien llama junta los nombres del collider,
    /// del objeto raíz y del material del renderer en un solo texto. Orden: robot, persona (cabeza o carne), cristal, metal o vehículo, concreto.
    /// </summary>
    public static class ReglaMaterialImpacto
    {
        public const string Carne = "IMP_BALA_CARNE";
        public const string Cabeza = "IMP_BALA_CABEZA";
        public const string Robot = "IMP_BALA_ROBOT";
        public const string Metal = "IMP_BALA_METAL";
        public const string Cristal = "IMP_BALA_CRISTAL";
        public const string Concreto = "IMP_BALA_CONCRETO";

        private static readonly string[] PalabrasRobot = { "dron", "robot", "mecha", "guardianova", "guardia_nova" };
        private static readonly string[] PalabrasCristal = { "vidrio", "cristal", "ventana", "glass", "parabrisas", "window" };
        private static readonly string[] PalabrasMetal = { "metal", "contenedor", "lamina", "lámina", "acero", "reja", "poste" };

        /// <summary>El ID del impacto. <paramref name="nombres"/> puede ser null o vacío.</summary>
        public static string Id(bool esPersona, bool esCabeza, bool esVehiculo, string nombres)
        {
            string n = string.IsNullOrEmpty(nombres) ? string.Empty : nombres.ToLowerInvariant();
            if (Contiene(n, PalabrasRobot))
            {
                return Robot;
            }
            if (esPersona)
            {
                return esCabeza ? Cabeza : Carne;
            }
            if (Contiene(n, PalabrasCristal))
            {
                return Cristal;
            }
            if (esVehiculo || Contiene(n, PalabrasMetal))
            {
                return Metal;
            }
            return Concreto;
        }

        private static bool Contiene(string texto, string[] palabras)
        {
            for (int i = 0; i < palabras.Length; i++)
            {
                if (texto.IndexOf(palabras[i], StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>true si una bala que falla pasa lo bastante cerca de la cámara como para oírse el zumbido.</summary>
        public static bool Zumba(float distanciaAlJugador, float radio = 2f) => distanciaAlJugador >= 0f && distanciaAlJugador <= radio;
    }
}

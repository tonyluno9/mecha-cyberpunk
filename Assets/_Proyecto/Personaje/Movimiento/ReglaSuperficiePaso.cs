using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Qué sonido de paso toca según el piso (T-596): interior por el lugar, y asfalto, mojado o metal por el nombre del material del piso.</summary>
    public static class ReglaSuperficiePaso
    {
        public const string Asfalto = "JUG_PASO_ASFALTO";
        public const string Mojado = "JUG_PASO_ASFALTO_MOJADO";
        public const string Interior = "JUG_PASO_INTERIOR";
        public const string Metal = "JUG_PASO_METAL";

        private static readonly string[] PalabrasMetal = { "metal", "rejilla", "acero", "lamina", "fierro", "hierro", "carro" };
        private static readonly string[] PalabrasMojado = { "mojad", "charco", "wet", "lluvia", "agua" };

        /// <summary>El ID del paso. <paramref name="nombrePiso"/> es el nombre del material o del objeto bajo los pies (puede ser nulo); <paramref name="esMontable"/> si el piso es un carro.</summary>
        public static string IdPaso(IdLugar lugar, string nombrePiso, bool esMontable)
        {
            if (EsInterior(lugar)) return Interior;
            if (esMontable || Contiene(nombrePiso, PalabrasMetal)) return Metal;
            if (Contiene(nombrePiso, PalabrasMojado)) return Mojado;
            return Asfalto;
        }

        public static bool EsInterior(IdLugar lugar)
        {
            return lugar == IdLugar.Casa || lugar == IdLugar.Taller || lugar == IdLugar.Bar || lugar == IdLugar.Clandestino
                || lugar == IdLugar.Casino || lugar == IdLugar.MercadoNegro || lugar == IdLugar.Clinica;
        }

        private static bool Contiene(string texto, string[] palabras)
        {
            if (string.IsNullOrEmpty(texto)) return false;
            foreach (string p in palabras)
            {
                if (texto.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }
}

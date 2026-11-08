using System;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// T-726: el texto de Opciones > Controles armado con las teclas reales del Input System. <c>teclaDe</c> devuelve el nombre visible de la tecla de
    /// una acción (el de GetBindingDisplayString) o nada si no la encuentra; entonces sale la tecla de respaldo. Lógica pura.
    /// </summary>
    public static class ReglaControlesInicio
    {
        public const string Mover = "Mover";
        public const string Correr = "Correr";
        public const string Saltar = "Saltar";
        public const string Interactuar = "Interactuar";
        public const string Apuntar = "Apuntar";
        public const string Disparar = "Atacar";
        public const string Recargar = "Recargar";
        public const string PrimeraArma = "ArmaPunos";
        public const string UltimaArma = "ArmaSniper";
        public const string Mapa = "Mapa";
        public const string Telefono = "Telefono";
        public const string Pausa = "Pausa";

        /// <summary>Las teclas de respaldo, si el Input System no da la acción.</summary>
        public static string Respaldo(string accion)
        {
            switch (accion)
            {
                case Mover: return "W A S D";
                case Correr: return "Shift";
                case Saltar: return "Espacio";
                case Interactuar: return "E";
                case Apuntar: return "Clic der.";
                case Disparar: return "Clic";
                case Recargar: return "R";
                case PrimeraArma: return "1";
                case UltimaArma: return "5";
                case Mapa: return "M";
                case Telefono: return "T";
                case Pausa: return "Esc";
                default: return string.Empty;
            }
        }

        /// <summary>El texto de las tres filas de controles, en mayúsculas.</summary>
        public static string Texto(Func<string, string> teclaDe)
        {
            string T(string accion)
            {
                string tecla = teclaDe != null ? teclaDe(accion) : null;
                return (string.IsNullOrEmpty(tecla) ? Respaldo(accion) : tecla).ToUpperInvariant();
            }

            return "MOVER  " + T(Mover) + "        CORRER  " + T(Correr) + "        SALTAR  " + T(Saltar) + "\n"
                + "INTERACTUAR  " + T(Interactuar) + "        APUNTAR  " + T(Apuntar) + "        DISPARAR  " + T(Disparar) + "\n"
                + "RECARGAR  " + T(Recargar) + "        ARMAS  " + T(PrimeraArma) + " A " + T(UltimaArma) + " O RUEDA        MAPA  " + T(Mapa)
                + "        TELÉFONO  " + T(Telefono) + "        PAUSA  " + T(Pausa);
        }
    }
}

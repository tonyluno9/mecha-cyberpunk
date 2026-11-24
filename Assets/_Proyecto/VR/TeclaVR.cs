using System.Text;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// V6, lógica pura: cambia la tecla de interactuar del teclado (E) por la del control derecho en VR (B) en los textos de aviso.
    /// Solo cambia la E que es una tecla suelta: "E · Hablar" → "B · Hablar", "E: ENCARGO · X" → "B: ENCARGO · X", "[E] Abrir" → "[B] Abrir".
    /// No toca la E dentro de palabras ("ENCARGO", "ABRE", "TE: ...").
    /// </summary>
    public static class TeclaVR
    {
        /// <summary>La tecla de interactuar en el teclado.</summary>
        public const char TeclaPC = 'E';
        /// <summary>La tecla de interactuar en VR: el botón B del control derecho.</summary>
        public const char TeclaVRInteractuar = 'B';
        /// <summary>Separador de los avisos del HUD ("E · Hablar").</summary>
        public const char Punto = '·';
        /// <summary>Prefijo que se pone en VR a una acción sin tecla ("Hablar" → "B · Hablar").</summary>
        public const string PrefijoVR = "B · ";

        // VR19: los botones de los controles (Quest Touch Plus) para los letreros y ayudas en VR. Ver la tabla en EntradaVR.
        public const string BotonSaltar = "A";
        public const string BotonInteractuar = "B";
        public const string BotonRecargar = "X";
        public const string BotonRueda = "MANTÉN Y";
        public const string BotonDisparar = "GATILLO DER.";
        public const string BotonAgarrar = "GRIP";
        public const string BotonMover = "STICK IZQ.";
        public const string BotonCorrer = "CLIC STICK IZQ.";
        public const string BotonGirar = "STICK DER.";
        public const string BotonPausa = "MENÚ";
        public const string BotonRecentrar = "STICK DER. ABAJO 1 S";
        public const string BotonRuedaProtesis = "GRIP IZQ. + GATILLO IZQ.";

        /// <summary>
        /// VR19: el botón de VR para una acción del mapa APie (los nombres de <c>ReglaTutorial</c>: "Saltar", "Recargar", "Interactuar", "Rueda"...),
        /// para que una ayuda escrita con la tecla del teclado diga el botón del control. Null si la acción no tiene botón en VR.
        /// </summary>
        public static string Boton(string accion)
        {
            switch (accion)
            {
                case "Mover": return BotonMover;
                case "Correr": return BotonCorrer;
                case "Saltar": return BotonSaltar;
                case "Interactuar": return BotonInteractuar;
                case "Recargar": return BotonRecargar;
                case "Rueda": return BotonRueda;
                case "Atacar":
                case "Disparar": return BotonDisparar;
                case "Agarrar": return BotonAgarrar;
                case "Pausa": return BotonPausa;
                case "Girar": return BotonGirar;
                case "Recentrar": return BotonRecentrar;
                case "Habilidades":
                case "RuedaProtesis": return BotonRuedaProtesis;
                default: return null;
            }
        }

        /// <summary>El texto con cada E-tecla cambiada por B. Null o vacío devuelve vacío.</summary>
        public static string Convertir(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return string.Empty;
            }
            StringBuilder salida = null;
            for (int i = 0; i < texto.Length; i++)
            {
                if (texto[i] == TeclaPC && EsTecla(texto, i))
                {
                    if (salida == null)
                    {
                        salida = new StringBuilder(texto);
                    }
                    salida[i] = TeclaVRInteractuar;
                }
            }
            return salida != null ? salida.ToString() : texto;
        }

        /// <summary>True si el texto lleva alguna E-tecla ("E · ", "E: ", "[E]").</summary>
        public static bool TieneTecla(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return false;
            }
            for (int i = 0; i < texto.Length; i++)
            {
                if (texto[i] == TeclaPC && EsTecla(texto, i))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// El texto del letrero en VR para una acción: si ya trae la tecla ("E: ENCARGO · X") se cambia a B; si no ("Hablar"),
        /// se le pone "B · " adelante. Vacío si no hay acción.
        /// </summary>
        public static string ConTecla(string accion)
        {
            if (string.IsNullOrEmpty(accion))
            {
                return string.Empty;
            }
            return TieneTecla(accion) ? Convertir(accion) : PrefijoVR + accion;
        }

        // La E en la posición i es una tecla si no va pegada a una letra o número por delante y la sigue ":", " ·", "·" o (entre corchetes) "]".
        private static bool EsTecla(string texto, int i)
        {
            char? antes = i > 0 ? texto[i - 1] : (char?)null;
            if (antes.HasValue && char.IsLetterOrDigit(antes.Value))
            {
                return false;
            }
            if (i + 1 >= texto.Length)
            {
                return false;
            }
            char despues = texto[i + 1];
            if (antes == '[' && despues == ']')
            {
                return true;
            }
            if (despues == ':' || despues == Punto)
            {
                return true;
            }
            return despues == ' ' && i + 2 < texto.Length && texto[i + 2] == Punto;
        }
    }
}

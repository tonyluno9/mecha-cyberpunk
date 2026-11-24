using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// V6, lógica pura: cambia la tecla de interactuar del teclado (E) por la del control derecho en VR (B) en los textos de aviso.
    /// Solo cambia la E que es una tecla suelta: "E · Hablar" → "B · Hablar", "E: ENCARGO · X" → "B: ENCARGO · X", "[E] Abrir" → "[B] Abrir".
    /// No toca la E dentro de palabras ("ENCARGO", "ABRE", "TE: ...").
    /// VR28: <see cref="Traducir"/> es la tabla central que cambia TODAS las teclas de PC de un texto (E, F, R, Q, Tab, Shift, Esc, Espacio,
    /// Enter, Ctrl, clic, ratón, W A S D...) por el botón o gesto de VR. La aplica a todos los textos en pantalla Textos.TraductorTextosVR.
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

        // VR19/VR28: los botones de los controles (Quest Touch Plus) para los letreros y ayudas en VR, con la tabla final de VR27.
        public const string BotonSaltar = "CLIC STICK DER.";
        public const string BotonInteractuar = "B";
        public const string BotonRecargar = "A + CADERA IZQ.";
        public const string BotonRueda = "MANTÉN X";
        public const string BotonDisparar = "GATILLO DER.";
        public const string BotonAgarrar = "GRIP";
        public const string BotonSacarArma = "GRIP DER. EN LA CADERA U HOMBRO";
        public const string BotonMover = "STICK IZQ.";
        public const string BotonCorrer = "CLIC STICK IZQ.";
        public const string BotonGirar = "STICK DER.";
        public const string BotonPausa = "Y";
        public const string BotonRecentrar = "STICK DER. ABAJO 1 S";
        public const string BotonRuedaProtesis = "GRIP IZQ. + GATILLO IZQ.";
        /// <summary>Aceptar en los menús (el láser apunta).</summary>
        public const string BotonAceptar = "GATILLO DER.";
        /// <summary>Cerrar o regresar en los menús.</summary>
        public const string BotonVolver = "B O Y";
        /// <summary>Lo que dice en VR en vez de las ayudas de flechas de los menús ("↑ ↓ ELEGIR · ENTER ACEPTAR · ESC VOLVER").</summary>
        public const string AyudaMenus = "LÁSER + GATILLO DER.: ELEGIR · B O Y: VOLVER";

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
                case "Rueda":
                case "ConsumirEquipado":
                case "ConsumirEquipado2":
                case "CambiarConsumible": return BotonRueda;
                case "Atacar":
                case "Disparar": return BotonDisparar;
                case "Agarrar": return BotonAgarrar;
                case "Pausa":
                case "Mapa":
                case "Telefono":
                case "Inventario": return BotonPausa;
                case "SacarArma": return BotonSacarArma;
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
            return Traducir(TieneTecla(accion) ? Convertir(accion) : PrefijoVR + accion); // VR28: y el resto de teclas por la tabla
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

        // ---- VR28: la tabla central de traducción de teclas de PC a botones de VR ----

        private sealed class Regla
        {
            public readonly Regex Patron;
            public readonly string Cambio;

            public Regla(string patron, string cambio)
            {
                Patron = new Regex(patron, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                Cambio = cambio;
            }
        }

        /// <summary>Textos conocidos (consejos de la pantalla de carga) que en VR se dicen distinto, completos.</summary>
        private static readonly Dictionary<string, string> Frases = new Dictionary<string, string>
        {
            { "Apunta con el botón derecho del ratón antes de disparar.", "Apunta con la mano y dispara con el gatillo derecho." },
            { "Mantén TAB para elegir un arma o usar un objeto; la rueda del ratón también cambia de arma.",
                "Mantén X para la rueda de armas y objetos; elige con la mano o con el stick." },
            { "Con implantes, las teclas 1, 2 y 3 activan las habilidades de piernas, brazos y cabeza.",
                "Con prótesis: manos en los muslos (piernas) o brazos abiertos (brazos) + los dos grips; o grip + gatillo izquierdos para el menú." },
            { "Mantén SHIFT para correr y ESPACIO para saltar.", "Clic del stick izquierdo para correr y clic del stick derecho para saltar." },
            { "Recarga con R antes de entrar a una pelea larga.", "Recarga antes de una pelea larga: A suelta el cargador y la mano izquierda toma otro de la cadera." },
        };

        /// <summary>Frases enteras (avisos del tutorial y ayudas de los menús): si el patrón aparece, el texto entero cambia.</summary>
        private static readonly Regla[] ReglasEnteras =
        {
            // Las ayudas de flechas de los menús ("↑ ↓ ELEGIR · ENTER ACEPTAR · ESC VOLVER").
            new Regla(@"(?=.*[↑↓←→])(?=.*\b(?:ESC|ENTER)\b)", AyudaMenus),
            new Regla(@"RUEDA: ZOOM", "B O Y: CERRAR · GATILLO DER.: MARCAR UN PUNTO O UN LUGAR"),
            new Regla(@"TE CUBRE DETRÁS", "AGÁCHATE DETRÁS DE UNA CAJA O MURO PARA CUBRIRTE"),
            new Regla(@"\bTE AGACHA\b", "AGÁCHATE CON EL CUERPO PARA AGACHARTE"),
            new Regla(@"RUEDA PARA ESQUIVAR", "AGACHADO + " + BotonSaltar + ": RUEDA PARA ESQUIVAR"),
            new Regla(@"PARA ELEGIR UN ARMA O USAR UN OBJETO", BotonRueda + " PARA ELEGIR UN ARMA O USAR UN OBJETO · ELIGE CON LA MANO O EL STICK"),
        };

        /// <summary>Teclas y frases de teclado y ratón que se cambian dentro del texto, en este orden.</summary>
        private static readonly Regla[] Reglas =
        {
            new Regla(@"\bW\s*/?\s*A\s*/?\s*S\s*/?\s*D\b", BotonMover),
            new Regla(@"\bBOT[ÓO]N DERECHO DEL RAT[ÓO]N\b", "LA MANO"),
            new Regla(@"\bRUEDA DEL RAT[ÓO]N\b", "RUEDA DE ARMAS (" + BotonRueda + ")"),
            new Regla(@"\b(CON|MUEVE) EL RAT[ÓO]N\b", "$1 LA MANO"),
            new Regla(@"\bRUEDA \(CLIC\)", BotonRueda),
            new Regla(@"\bHAZ CLIC\b", "APRIETA EL GATILLO"),
            new Regla(@"\bDOBLE CLIC\b", BotonDisparar),
            new Regla(@"\bCLIC DER(?:\.|ECHO\b)", "LA MANO"),
            new Regla(@"\bCLIC IZQ(?:\.|UIERDO\b)", BotonDisparar),
            new Regla(@"\bCLIC\b(?!\s+(?:DEL\s+)?STICK)", BotonDisparar),
            new Regla(@"\b(?:LEFT\s+)?SHIFT\b", BotonCorrer),
            new Regla(@"\b(?:L?CTRL|CONTROL IZQ\.?)(?![\p{L}\p{N}])", "AGÁCHATE"),
            new Regla(@"\bALT\b", BotonRueda),
            new Regla(@"\bENTER\b", BotonAceptar),
            new Regla(@"\bESC\b", BotonPausa),
            new Regla(@"\bTAB\b", BotonRuedaProtesis),
            new Regla(@"(?<=\b(?:MANT[ÉE]N|PULSA|PRESIONA|CON)\s+)ESPACIO\b|\bESPACIO(?=\s*(?:·|:|\]|\s(?:PARA|SALTA|RUEDA)\b))|(?<=[\[+]\s*)ESPACIO\b", BotonSaltar),
            new Regla(@"\bMANT[ÉE]N\s+MANT[ÉE]N\b", "MANTÉN"),
        };

        /// <summary>Palabras tras las que una letra suelta es una tecla ("PULSA E", "CON R", "· E").</summary>
        private static readonly HashSet<string> AntesDeTecla = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "PULSA", "PRESIONA", "MANTÉN", "MANTEN", "TECLA", "CON", "USA", "·", "+", "/"
        };

        /// <summary>Palabras que, tras una letra suelta, la hacen tecla ("E PARA", "R RECARGA").</summary>
        private static readonly HashSet<string> DespuesDeTecla = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "PARA", "TE", "ABRE", "RECARGA", "SALTA", "GOLPEA", "DISPARA", "APUNTA", "USAN", "USA", "CIERRA", "CERRAR", "·"
        };

        private static readonly Regex LetraSuelta = new Regex(@"(?<![\p{L}\p{N}.'])[EFRQCZVTMI](?![\p{L}\p{N}'])", RegexOptions.CultureInvariant);

        private const int MaximoCache = 512;
        private static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        /// <summary>El botón de VR de una letra de teclado (null si la letra no es tecla del juego).</summary>
        public static string BotonDeLetra(char letra)
        {
            switch (letra)
            {
                case 'E':
                case 'F': return BotonInteractuar;
                case 'R': return BotonRecargar;
                case 'Q': return BotonRuedaProtesis;
                case 'C':
                case 'Z': return BotonRueda;
                case 'V': return "AGÁCHATE";
                case 'T':
                case 'M':
                case 'I': return BotonPausa;
                default: return null;
            }
        }

        /// <summary>
        /// VR28: el texto con cada tecla de PC cambiada por el botón o gesto de VR (la tabla central). No toca letras dentro de palabras
        /// ni la "Y", "A", "O" del español; un texto que ya es de VR queda igual. Null o vacío devuelve vacío.
        /// </summary>
        public static string Traducir(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return string.Empty;
            }
            if (cache.TryGetValue(texto, out string hecho))
            {
                return hecho;
            }
            string salida = TraducirSinCache(texto);
            if (cache.Count >= MaximoCache)
            {
                cache.Clear();
            }
            cache[texto] = salida;
            return salida;
        }

        private static string TraducirSinCache(string texto)
        {
            if (Frases.TryGetValue(texto.Trim(), out string frase))
            {
                return frase;
            }
            foreach (Regla r in ReglasEnteras)
            {
                if (r.Patron.IsMatch(texto))
                {
                    return r.Cambio;
                }
            }
            string s = texto;
            foreach (Regla r in Reglas)
            {
                s = r.Patron.Replace(s, r.Cambio);
            }
            return CambiarLetras(s);
        }

        // Las letras sueltas que son tecla por lo que tienen alrededor: "[E]", "(E)", "E · ", "E: ", "PULSA E", "· E Y", "R RECARGA", "E para".
        private static string CambiarLetras(string s)
        {
            MatchCollection letras = LetraSuelta.Matches(s);
            if (letras.Count == 0)
            {
                return s;
            }
            var salida = new StringBuilder(s.Length + 16);
            int ultimo = 0;
            bool cambio = false;
            foreach (Match m in letras)
            {
                int i = m.Index;
                string boton = BotonDeLetra(s[i]);
                if (boton == null || !EsLetraTecla(s, i))
                {
                    continue;
                }
                salida.Append(s, ultimo, i - ultimo);
                salida.Append(boton);
                ultimo = i + 1;
                cambio = true;
            }
            if (!cambio)
            {
                return s;
            }
            salida.Append(s, ultimo, s.Length - ultimo);
            return salida.ToString();
        }

        private static bool EsLetraTecla(string s, int i)
        {
            bool hayDespues = i + 1 < s.Length;
            char despues = hayDespues ? s[i + 1] : '\0';
            if (despues == ']' || despues == ')' || despues == ':' || despues == Punto)
            {
                return true;
            }
            string siguiente = hayDespues && despues == ' ' ? Palabra(s, i + 2, true) : null;
            if (siguiente != null && DespuesDeTecla.Contains(siguiente))
            {
                return true;
            }
            if (!hayDespues)
            {
                // Al final solo tras "PULSA", "CON"... ("Y PULSA E"); una letra sola al final ("ELITE · E") o como texto entero (letras del mapa) no.
                string previa = i >= 2 && s[i - 1] == ' ' ? Palabra(s, i - 2, false) : null;
                return previa != null && previa.Length > 1 && AntesDeTecla.Contains(previa);
            }
            if (i == 0)
            {
                return despues == ' ';
            }
            char antes = s[i - 1];
            if (antes == '[' || antes == '(' || antes == '>' || antes == '\n')
            {
                return true;
            }
            if (antes != ' ')
            {
                return false;
            }
            string anterior = Palabra(s, i - 2, false);
            return anterior != null && AntesDeTecla.Contains(anterior);
        }

        // La palabra (o signo suelto) que empieza (adelante) o termina (atrás) en la posición dada; null si no hay.
        private static string Palabra(string s, int desde, bool adelante)
        {
            if (desde < 0 || desde >= s.Length)
            {
                return null;
            }
            if (adelante)
            {
                int fin = desde;
                while (fin < s.Length && s[fin] != ' ' && s[fin] != '\n')
                {
                    fin++;
                }
                return fin > desde ? s.Substring(desde, fin - desde) : null;
            }
            int inicio = desde;
            while (inicio >= 0 && s[inicio] != ' ' && s[inicio] != '\n')
            {
                inicio--;
            }
            return desde > inicio ? s.Substring(inicio + 1, desde - inicio) : null;
        }
    }
}

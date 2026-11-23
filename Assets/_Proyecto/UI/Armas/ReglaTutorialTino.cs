namespace MechaCyberpunk.UI.Armas
{
    /// <summary>T-84: los pasos del tutorial guiado de la primera visita a Tino. El valor numérico es el número de paso que se muestra (1 a 5).</summary>
    public enum PasoTutorialTino
    {
        Ninguno = 0,
        ElegirPistola = 1,
        Comprar = 2,
        IrAMejorar = 3,
        Instalar = 4,
        Salir = 5,
        /// <summary>Está en MEJORAR sin pistola: hay que volver a COMPRAR.</summary>
        VolverAComprar = 6,
        /// <summary>Está en MEJORAR con la pistola comprada pero otra arma elegida (cuenta como paso 4).</summary>
        ElegirPistolaEnMejorar = 7
    }

    /// <summary>
    /// T-84: lógica pura del tutorial guiado de Tino. Dice qué paso toca según el estado real y qué control resaltar (por nombre de GameObject).
    /// Solo la pistola y una mejora son obligatorias.
    /// </summary>
    public static class ReglaTutorialTino
    {
        public const int TotalPasos = 5;
        public const string NombrePestanaComprar = "Pestana_Comprar";
        public const string NombrePestanaMejorar = "Pestana_Mejorar";
        public const string NombreBotonComprar = "BotonComprar";
        public const string NombreBotonMejorar = "BotonMejorar";
        public const string NombreBotonSalir = "BotonSalirBanco";
        public const string NombreBotonCampoTiro = "Extra_CAMPO DE TIRO";
        public const string NombreFilaPistolaMejorar = "Arma_Pistola";

        /// <param name="activo">El tutorial está encendido.</param>
        /// <param name="completo">Ya no falta nada para salir (el bloqueo de salida está vacío).</param>
        /// <param name="tienePistola">La pistola ya está comprada.</param>
        /// <param name="enMejorar">La pestaña abierta es MEJORAR.</param>
        /// <param name="pistolaElegidaEnCompra">En COMPRAR la fila elegida es la pistola (no sus balas).</param>
        /// <param name="pistolaElegidaEnMejorar">En MEJORAR el arma elegida es la pistola.</param>
        public static PasoTutorialTino Calcular(bool activo, bool completo, bool tienePistola, bool enMejorar, bool pistolaElegidaEnCompra, bool pistolaElegidaEnMejorar)
        {
            if (!activo)
            {
                return PasoTutorialTino.Ninguno;
            }
            if (completo)
            {
                return PasoTutorialTino.Salir;
            }
            if (!tienePistola)
            {
                if (enMejorar)
                {
                    return PasoTutorialTino.VolverAComprar;
                }
                return pistolaElegidaEnCompra ? PasoTutorialTino.Comprar : PasoTutorialTino.ElegirPistola;
            }
            if (!enMejorar)
            {
                return PasoTutorialTino.IrAMejorar;
            }
            return pistolaElegidaEnMejorar ? PasoTutorialTino.Instalar : PasoTutorialTino.ElegirPistolaEnMejorar;
        }

        /// <summary>El número de paso que se muestra (1 a 5); 0 si no hay tutorial.</summary>
        public static int Numero(PasoTutorialTino paso)
        {
            switch (paso)
            {
                case PasoTutorialTino.VolverAComprar: return (int)PasoTutorialTino.ElegirPistola;
                case PasoTutorialTino.ElegirPistolaEnMejorar: return (int)PasoTutorialTino.Instalar;
                default: return (int)paso;
            }
        }

        /// <summary>La instrucción corta del paso.</summary>
        public static string Texto(PasoTutorialTino paso)
        {
            switch (paso)
            {
                case PasoTutorialTino.ElegirPistola: return "ELIGE LA PISTOLA";
                case PasoTutorialTino.Comprar: return "AHORA PULSA COMPRAR";
                case PasoTutorialTino.IrAMejorar: return "ABRE LA PESTAÑA MEJORAR";
                case PasoTutorialTino.ElegirPistolaEnMejorar: return "ELIGE LA PISTOLA PARA MEJORARLA";
                case PasoTutorialTino.Instalar: return "INSTALA LA MEJORA";
                case PasoTutorialTino.Salir: return "PRUEBA TU ARMA (OPCIONAL) O SAL DEL BANCO";
                case PasoTutorialTino.VolverAComprar: return "VUELVE A COMPRAR Y ELIGE LA PISTOLA";
                default: return string.Empty;
            }
        }

        /// <summary>La línea completa que se muestra: "PASO 2 DE 5: AHORA PULSA COMPRAR".</summary>
        public static string Linea(PasoTutorialTino paso)
        {
            return paso == PasoTutorialTino.Ninguno ? string.Empty : "PASO " + Numero(paso) + " DE " + TotalPasos + ": " + Texto(paso);
        }

        /// <summary>El nombre del GameObject a resaltar en el paso (null si el control es la fila de la pistola en COMPRAR, que se busca por su texto).</summary>
        public static string[] Objetivos(PasoTutorialTino paso)
        {
            switch (paso)
            {
                case PasoTutorialTino.Comprar: return new[] { NombreBotonComprar };
                case PasoTutorialTino.IrAMejorar: return new[] { NombrePestanaMejorar };
                case PasoTutorialTino.ElegirPistolaEnMejorar: return new[] { NombreFilaPistolaMejorar };
                case PasoTutorialTino.Instalar: return new[] { NombreBotonMejorar };
                case PasoTutorialTino.Salir: return new[] { NombreBotonCampoTiro, NombreBotonSalir };
                case PasoTutorialTino.VolverAComprar: return new[] { NombrePestanaComprar };
                default: return new string[0];
            }
        }

        /// <summary>True si en este paso el control a resaltar es la fila de la pistola en la lista de COMPRAR.</summary>
        public static bool ResaltaFilaPistolaCompra(PasoTutorialTino paso) => paso == PasoTutorialTino.ElegirPistola;

        /// <summary>True si el paso deja salir del banco (Esc o SALIR).</summary>
        public static bool PermiteSalir(PasoTutorialTino paso) => paso == PasoTutorialTino.Salir || paso == PasoTutorialTino.Ninguno;
    }
}

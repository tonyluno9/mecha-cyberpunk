using MechaCyberpunk.UI.Clinica;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-1658: lógica pura del tutorial de prótesis. Tres pasos en orden; cada uno avanza cuando el jugador hace lo que dice. Hacer algo de un paso
    /// posterior (activar las piernas sin abrir el menú aparte) también da por hechos los anteriores. El paso de BRAZOS no se ofrece mientras no tenga el implante.
    /// </summary>
    public sealed class ReglaTutorialProtesis
    {
        public const string TextoAbrirMenu = "MANTÉN {0} PARA ABRIR EL MENÚ DE PRÓTESIS, MUEVE EL RATÓN A LA QUE QUIERES Y HAZ CLIC / SUELTA {0} PARA ACTIVARLA";
        public const string TextoPiernas = "ACTIVA LAS PIERNAS Y CORRE PARA PROBARLAS";
        public const string TextoBrazos = "ACTIVA LOS BRAZOS Y GOLPEA PARA PROBARLOS";

        /// <summary>VR20: los gestos y botones de VR (VR19: el menú de prótesis es grip + gatillo izquierdos; los gestos activan directo).</summary>
        public const string GestoPiernasVR = "MANOS EN LOS MUSLOS + LOS DOS GRIPS: PIERNAS";
        public const string GestoBrazosVR = "BRAZOS ABIERTOS + LOS DOS GRIPS: BRAZOS";
        public const string MenuVR = "O MANTÉN GRIP + GATILLO IZQUIERDOS PARA EL MENÚ DE PRÓTESIS";
        public const string TextoPiernasVR = "ACTIVA LAS PIERNAS (MANOS EN LOS MUSLOS + LOS DOS GRIPS) Y CORRE CON EL CLIC DEL STICK IZQUIERDO";
        public const string TextoBrazosVR = "ACTIVA LOS BRAZOS (BRAZOS ABIERTOS + LOS DOS GRIPS) Y GOLPEA CON LAS MANOS";

        public PasoTutorialProtesis Paso { get; private set; } = PasoTutorialProtesis.AbrirMenu;

        /// <summary>VR20: el texto del paso actual en VR (gestos y botones de los controles, sin Tab ni ratón); vacío igual que <see cref="Texto"/>.</summary>
        public string TextoVR(bool tienePiernas, bool tieneBrazos)
        {
            switch (Paso)
            {
                case PasoTutorialProtesis.AbrirMenu: return TextoAbrirMenuVR(tienePiernas, tieneBrazos);
                case PasoTutorialProtesis.Piernas: return tienePiernas ? TextoPiernasVR : string.Empty;
                case PasoTutorialProtesis.Brazos: return tieneBrazos ? TextoBrazosVR : string.Empty;
                default: return string.Empty;
            }
        }

        /// <summary>VR20: el primer paso en VR, solo con los gestos de las prótesis que se tienen.</summary>
        public static string TextoAbrirMenuVR(bool tienePiernas, bool tieneBrazos)
        {
            string gestos = tienePiernas && tieneBrazos ? GestoPiernasVR + " · " + GestoBrazosVR
                : tienePiernas ? GestoPiernasVR : tieneBrazos ? GestoBrazosVR : string.Empty;
            return gestos.Length > 0 ? gestos + " · " + MenuVR : MenuVR;
        }

        public void Reiniciar() => Paso = PasoTutorialProtesis.AbrirMenu;

        /// <summary>El texto del paso actual con el nombre de la tecla del menú; vacío si no hay nada que mostrar (hecho, o BRAZOS sin implante).</summary>
        public string Texto(string tecla, bool tienePiernas, bool tieneBrazos)
        {
            switch (Paso)
            {
                case PasoTutorialProtesis.AbrirMenu: return string.Format(TextoAbrirMenu, tecla);
                case PasoTutorialProtesis.Piernas: return tienePiernas ? TextoPiernas : string.Empty;
                case PasoTutorialProtesis.Brazos: return tieneBrazos ? TextoBrazos : string.Empty;
                default: return string.Empty;
            }
        }

        /// <summary>Avanza según lo que hizo el jugador este cuadro. Devuelve true si cambió de paso.</summary>
        public bool Avanzar(bool abrioMenu, bool piernasActivas, bool corrioConPiernas, bool brazosActivos, bool golpeoConBrazos, bool tienePiernas)
        {
            PasoTutorialProtesis antes = Paso;
            if (Paso == PasoTutorialProtesis.AbrirMenu && (abrioMenu || piernasActivas || brazosActivos))
            {
                Paso = PasoTutorialProtesis.Piernas;
            }
            if (Paso == PasoTutorialProtesis.Piernas && (corrioConPiernas || brazosActivos || !tienePiernas))
            {
                Paso = PasoTutorialProtesis.Brazos;
            }
            if (Paso == PasoTutorialProtesis.Brazos && golpeoConBrazos)
            {
                Paso = PasoTutorialProtesis.Hecho;
            }
            return Paso != antes;
        }
    }
}

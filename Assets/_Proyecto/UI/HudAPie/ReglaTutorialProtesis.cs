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

        public PasoTutorialProtesis Paso { get; private set; } = PasoTutorialProtesis.AbrirMenu;

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

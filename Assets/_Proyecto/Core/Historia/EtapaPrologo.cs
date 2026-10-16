namespace MechaCyberpunk.Core
{
    /// <summary>
    /// En qué etapa del prólogo va la partida. El orden es el de la historia y nunca cambia; se agregan valores solo al
    /// final. Se lee con <see cref="IHistoria.Etapa"/>.
    /// </summary>
    public enum EtapaPrologo
    {
        /// <summary>Sin partida: menú principal. El mundo no hace nada especial.</summary>
        Ninguna = 0,

        /// <summary>El jugador crea su personaje (nombre, alias, apariencia). Todavía no hay mundo jugable.</summary>
        CreacionPersonaje,

        /// <summary>De noche; el jugador ya está en su mecha en la arena del clandestino, en su pelea tutorial. El clandestino está abierto.</summary>
        PeleaTutorial,

        /// <summary>A pie, saliendo del clandestino hacia su carro, que está estacionado cerca.</summary>
        SalidaClandestino,

        /// <summary>La pandilla del callejón embosca al jugador.</summary>
        Emboscada,

        /// <summary>El carro del jugador está cerca del clandestino; el jugador maneja a su casa.</summary>
        RegresoACasa,

        /// <summary>En casa; el jugador debe ir a la cama.</summary>
        Dormir,

        /// <summary>Escena del flashback de los papás (Flashback.unity).</summary>
        Flashback,

        /// <summary>Paso corto al despertar: Tony pone el estado inicial de la historia.</summary>
        Despertar,

        /// <summary>Anochecer del presente; el jugador va al clandestino a su primera pelea casera. El clandestino está abierto.</summary>
        PeleaCasera,

        /// <summary>Pantalla de fin de la demo.</summary>
        Continuara,

        /// <summary>Después de la demo: juego libre. Es lo que el mundo asume si no hay <see cref="IHistoria"/>.</summary>
        Libre,
    }
}

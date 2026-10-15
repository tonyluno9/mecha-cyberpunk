namespace MechaCyberpunk.Pelea.Emboscada
{
    /// <summary>Fases de la emboscada (T-071).</summary>
    public enum EstadoEmboscada
    {
        /// <summary>Todavía no empieza: nadie ha llegado ni la han iniciado.</summary>
        Esperando,
        /// <summary>La pandilla aparece; sin control.</summary>
        Aparicion,
        /// <summary>La pandilla espera a que el jugador llame al mecha y suba.</summary>
        EsperandoMecha,
        /// <summary>El jugador va en el mecha y la pandilla pelea.</summary>
        Peleando,
        /// <summary>El jugador se bajó a media pelea: la pandilla se detiene hasta que vuelva a subir.</summary>
        Pausada,
        /// <summary>Todos los rivales cayeron. Final.</summary>
        Superada
    }
}

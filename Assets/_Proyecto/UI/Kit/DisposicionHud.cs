namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// Dónde va cada texto del HUD a pie (T-408), todo medido desde el margen de 48 px y filas de <see cref="EstiloCyberfix.AltoFilaHud"/>,
    /// para que nada se encime (antes cada parche se movía a mano). La tabla está en docs/diseno/ui-cyberfix.md.
    /// Arriba a la izquierda: radio, renta y zona. Arriba al centro: objetivo, aviso de zona, aviso de aliados y ayuda de la historia.
    /// Arriba a la derecha: calor. Abajo a la izquierda: el minimapa (T-409); a su derecha vida, Biits y aviso de Biits, y arriba de él la ayuda de controles. Abajo a la derecha: arma y
    /// pandilla (crece hacia arriba). Abajo al centro: interacción, llamado del mecha y subtítulo.
    /// </summary>
    public static class DisposicionHud
    {
        private const float AltoVida = 52f;     // texto de 36 + 4 + barra de 12
        private const float AltoCalor = 52f;    // texto de 36 + 4 + marcas de 12
        private const float Hueco = 8f;
        private const int FilasAyuda = 2;
        private const float AltoPandilla = 300f;
        private const float AltoSubtitulo = 96f;
        private const float AltoAvisoGrande = 60f;
        private const float AltoAvisoMediano = 48f;
        private const float AltoAyudaHistoria = 84f;

        private static float M => KitUI.Estilo.MargenBorde;
        private static float F => KitUI.Estilo.AltoFilaHud;

        // Arriba a la izquierda.
        public static RanuraHud Cancion => new RanuraHud(EsquinaHud.ArribaIzquierda, M, F, M);
        public static RanuraHud Renta => new RanuraHud(EsquinaHud.ArribaIzquierda, M + F, F, M);
        public static RanuraHud Zona => new RanuraHud(EsquinaHud.ArribaIzquierda, M + 2f * F, F, M);

        // Arriba a la derecha.
        public static RanuraHud Calor => new RanuraHud(EsquinaHud.ArribaDerecha, M, AltoCalor, M);

        // Arriba al centro.
        public static RanuraHud Objetivo => new RanuraHud(EsquinaHud.ArribaCentro, M, F, 0f);
        // T-429: debajo de la fila de la zona (arriba a la izquierda) para que "<ZONA> ES DE ..." nunca se encime con ella.
        public static RanuraHud AvisoZona => new RanuraHud(EsquinaHud.ArribaCentro, System.Math.Max(Objetivo.Fin + Hueco * 2f, Zona.Fin + Hueco), AltoAvisoGrande, 0f);
        public static RanuraHud AvisoAliados => new RanuraHud(EsquinaHud.ArribaCentro, AvisoZona.Fin + Hueco, AltoAvisoMediano, 0f);
        public static RanuraHud AyudaHistoria => new RanuraHud(EsquinaHud.ArribaCentro, AvisoAliados.Fin + Hueco, AltoAyudaHistoria, 0f);

        /// <summary>Lado (px) del minimapa redondo (T-409).</summary>
        public const float LadoMinimapa = 260f;

        // Abajo a la izquierda (T-409): el minimapa en la esquina, la vida y los Biits a su derecha (48 + 260 + 24) y la ayuda arriba de él.
        public static RanuraHud Minimapa => new RanuraHud(EsquinaHud.AbajoIzquierda, M, LadoMinimapa, M);
        public static float JuntoAlMinimapa => M + LadoMinimapa + KitUI.Estilo.SeparacionBloques;
        public static RanuraHud Vida => new RanuraHud(EsquinaHud.AbajoIzquierda, M, AltoVida, JuntoAlMinimapa);
        public static RanuraHud Dinero => new RanuraHud(EsquinaHud.AbajoIzquierda, Vida.Fin + Hueco, F, JuntoAlMinimapa);
        public static RanuraHud AvisoDinero => new RanuraHud(EsquinaHud.AbajoIzquierda, Dinero.Fin, F, JuntoAlMinimapa);
        public static RanuraHud AyudaControles => new RanuraHud(EsquinaHud.AbajoIzquierda, Minimapa.Fin + KitUI.Estilo.SeparacionBloques, FilasAyuda * F, M);

        // Abajo a la derecha.
        public static RanuraHud Arma => new RanuraHud(EsquinaHud.AbajoDerecha, M, F, M);
        public static RanuraHud Pandilla => new RanuraHud(EsquinaHud.AbajoDerecha, Arma.Fin + Hueco, AltoPandilla, M);
        /// <summary>El aviso "GUARDADO" (T-412), arriba de tu pandilla.</summary>
        public static RanuraHud AvisoGuardado => new RanuraHud(EsquinaHud.AbajoDerecha, Pandilla.Fin + Hueco, F, M);

        // Abajo al centro.
        public static RanuraHud Interaccion => new RanuraHud(EsquinaHud.AbajoCentro, M + 2f * F, F, 0f);
        public static RanuraHud Llamado => new RanuraHud(EsquinaHud.AbajoCentro, Interaccion.Fin + Hueco, F, 0f);
        // Sobre el llamado y los Biits (T-409: la ayuda de controles ya está arriba del minimapa, a la izquierda del subtítulo).
        public static RanuraHud Subtitulo => new RanuraHud(EsquinaHud.AbajoCentro, System.Math.Max(Llamado.Fin, AvisoDinero.Fin) + Hueco, AltoSubtitulo, 0f);

        /// <summary>Ancho (px) de los textos de las esquinas de la izquierda abajo (ayuda de controles).</summary>
        public const float AnchoAyudaControles = 520f;

        /// <summary>Ancho (px) de la interacción y el llamado, para no tocar las esquinas de abajo (1920 − 2 × 580).</summary>
        public const float AnchoCentroAbajo = 760f;
    }
}

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// Dónde va cada texto del HUD a pie (T-408), todo medido desde el margen de 48 px y filas de <see cref="EstiloCyberfix.AltoFilaHud"/>,
    /// para que nada se encime (antes cada parche se movía a mano). La tabla está en docs/diseno/ui-cyberfix.md.
    /// Arriba a la izquierda: radio, renta y zona. Arriba al centro: objetivo, aviso de zona, aviso de aliados y ayuda de la historia.
    /// Arriba a la derecha: calor. Abajo a la izquierda: vida, Biits, aviso de Biits y ayuda de controles. Abajo a la derecha: arma y
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
        public static RanuraHud AvisoZona => new RanuraHud(EsquinaHud.ArribaCentro, Objetivo.Fin + Hueco * 2f, AltoAvisoGrande, 0f);
        public static RanuraHud AvisoAliados => new RanuraHud(EsquinaHud.ArribaCentro, AvisoZona.Fin + Hueco, AltoAvisoMediano, 0f);
        public static RanuraHud AyudaHistoria => new RanuraHud(EsquinaHud.ArribaCentro, AvisoAliados.Fin + Hueco, AltoAyudaHistoria, 0f);

        // Abajo a la izquierda.
        public static RanuraHud Vida => new RanuraHud(EsquinaHud.AbajoIzquierda, M, AltoVida, M);
        public static RanuraHud Dinero => new RanuraHud(EsquinaHud.AbajoIzquierda, Vida.Fin + Hueco, F, M);
        public static RanuraHud AvisoDinero => new RanuraHud(EsquinaHud.AbajoIzquierda, Dinero.Fin, F, M);
        public static RanuraHud AyudaControles => new RanuraHud(EsquinaHud.AbajoIzquierda, AvisoDinero.Fin + Hueco, FilasAyuda * F, M);

        // Abajo a la derecha.
        public static RanuraHud Arma => new RanuraHud(EsquinaHud.AbajoDerecha, M, F, M);
        public static RanuraHud Pandilla => new RanuraHud(EsquinaHud.AbajoDerecha, Arma.Fin + Hueco, AltoPandilla, M);

        // Abajo al centro.
        public static RanuraHud Interaccion => new RanuraHud(EsquinaHud.AbajoCentro, M + 2f * F, F, 0f);
        public static RanuraHud Llamado => new RanuraHud(EsquinaHud.AbajoCentro, Interaccion.Fin + Hueco, F, 0f);
        // Sobre la ayuda de controles: el subtítulo es más ancho que el hueco que deja.
        public static RanuraHud Subtitulo => new RanuraHud(EsquinaHud.AbajoCentro, System.Math.Max(Llamado.Fin, AyudaControles.Fin) + Hueco, AltoSubtitulo, 0f);

        /// <summary>Ancho (px) de los textos de las esquinas de la izquierda abajo (ayuda de controles).</summary>
        public const float AnchoAyudaControles = 520f;

        /// <summary>Ancho (px) de la interacción y el llamado, para no tocar las esquinas de abajo (1920 − 2 × 580).</summary>
        public const float AnchoCentroAbajo = 760f;
    }
}

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// Dónde va cada elemento del HUD a pie (T-408), todo medido desde el margen de 48 px y filas de <see cref="EstiloCyberfix.AltoFilaHud"/>,
    /// para que nada se encime. La tabla está en docs/diseno/ui-cyberfix.md.
    /// T-521 (HUD como el mockup): arriba a la izquierda vida, calor y Biits en columna, y debajo la radio y la renta. Arriba al centro: objetivo,
    /// aviso de zona, aviso de aliados y ayuda de la historia. Arriba a la derecha: la zona con su dueño y tu respeto, y debajo los implantes.
    /// Abajo a la izquierda: el minimapa (T-409), a su derecha los aliados y arriba de él la ayuda de controles. Abajo a la derecha: el panel
    /// del arma. Abajo al centro: interacción, llamado del mecha y subtítulo.
    /// </summary>
    public static class DisposicionHud
    {
        public const float AnchoVida = 560f;
        public const float AltoVida = 88f;
        public const float AnchoCalor = 440f;
        public const float AltoCalor = 74f;
        public const float AnchoDinero = 300f;
        public const float AltoDinero = 56f;
        public const float AnchoZona = 540f;
        public const float AltoZona = 116f;
        public const float AnchoArma = 380f;
        public const float AltoArma = 260f;
        public const float AnchoAliados = 360f;
        public const float AltoAliados = 200f;
        private const float Hueco = 8f;
        private const int FilasAyuda = 2;
        private const float AltoSubtitulo = 96f;
        private const float AltoAvisoGrande = 60f;
        private const float AltoAvisoMediano = 48f;
        private const float AltoAyudaHistoria = 84f;

        private static float M => KitUI.Estilo.MargenBorde;
        private static float F => KitUI.Estilo.AltoFilaHud;

        // Arriba a la izquierda: vida, calor, Biits y debajo la radio y la renta.
        public static RanuraHud Vida => new RanuraHud(EsquinaHud.ArribaIzquierda, M, AltoVida, M);
        public static RanuraHud Calor => new RanuraHud(EsquinaHud.ArribaIzquierda, Vida.Fin + 6f, AltoCalor, M);
        public static RanuraHud Dinero => new RanuraHud(EsquinaHud.ArribaIzquierda, Calor.Fin + 6f, AltoDinero, M);
        /// <summary>El aviso "+Ƀ600 · PREMIO": a la derecha del panel de Biits.</summary>
        public static RanuraHud AvisoDinero => new RanuraHud(EsquinaHud.ArribaIzquierda, Dinero.DesdeBorde + 10f, F, M + AnchoDinero + Hueco * 2f);
        public static RanuraHud Cancion => new RanuraHud(EsquinaHud.ArribaIzquierda, Dinero.Fin + Hueco * 2f, F, M);
        public static RanuraHud Renta => new RanuraHud(EsquinaHud.ArribaIzquierda, Cancion.Fin, F, M);

        // Arriba a la derecha: la zona, con el dueño y tu respeto.
        public static RanuraHud Zona => new RanuraHud(EsquinaHud.ArribaDerecha, M, AltoZona, M);

        // T-430: los iconos de implantes, debajo de la zona.
        public static RanuraHud Implantes => new RanuraHud(EsquinaHud.ArribaDerecha, Zona.Fin + Hueco, 40f, M);

        // Arriba al centro.
        public static RanuraHud Objetivo => new RanuraHud(EsquinaHud.ArribaCentro, M, F, 0f);
        // T-429: debajo del panel de la zona (arriba a la derecha) para que "<ZONA> ES DE ..." nunca se encime con él.
        public static RanuraHud AvisoZona => new RanuraHud(EsquinaHud.ArribaCentro, System.Math.Max(Objetivo.Fin + Hueco * 2f, Zona.Fin + Hueco), AltoAvisoGrande, 0f);
        public static RanuraHud AvisoAliados => new RanuraHud(EsquinaHud.ArribaCentro, AvisoZona.Fin + Hueco, AltoAvisoMediano, 0f);
        public static RanuraHud AyudaHistoria => new RanuraHud(EsquinaHud.ArribaCentro, AvisoAliados.Fin + Hueco, AltoAyudaHistoria, 0f);

        /// <summary>Lado (px) del minimapa redondo (T-409).</summary>
        public const float LadoMinimapa = 260f;

        // Abajo a la izquierda (T-409): el minimapa en la esquina y arriba de él la ayuda de controles.
        public static RanuraHud Minimapa => new RanuraHud(EsquinaHud.AbajoIzquierda, M, LadoMinimapa, M);
        public static float JuntoAlMinimapa => M + LadoMinimapa + KitUI.Estilo.SeparacionBloques;
        public static RanuraHud AyudaControles => new RanuraHud(EsquinaHud.AbajoIzquierda, Minimapa.Fin + KitUI.Estilo.SeparacionBloques, FilasAyuda * F, M);
        /// <summary>T-521: los aliados, compactos, a la derecha del minimapa y pegados abajo (la fila crece hacia arriba).</summary>
        public static RanuraHud Pandilla => new RanuraHud(EsquinaHud.AbajoIzquierda, M, AltoAliados, JuntoAlMinimapa);

        // Abajo a la derecha: el panel del arma.
        public static RanuraHud Arma => new RanuraHud(EsquinaHud.AbajoDerecha, M, AltoArma, M);
        /// <summary>El aviso "GUARDADO" (T-412), arriba del panel del arma.</summary>
        public static RanuraHud AvisoGuardado => new RanuraHud(EsquinaHud.AbajoDerecha, Arma.Fin + Hueco, F, M);

        // Abajo al centro.
        public static RanuraHud Interaccion => new RanuraHud(EsquinaHud.AbajoCentro, M + 2f * F, F, 0f);
        public static RanuraHud Llamado => new RanuraHud(EsquinaHud.AbajoCentro, Interaccion.Fin + Hueco, F, 0f);
        // Sobre el llamado (T-409: la ayuda de controles ya está arriba del minimapa, a la izquierda del subtítulo).
        public static RanuraHud Subtitulo => new RanuraHud(EsquinaHud.AbajoCentro, Llamado.Fin + Hueco, AltoSubtitulo, 0f);

        /// <summary>Ancho (px) de los textos de las esquinas de la izquierda abajo (ayuda de controles).</summary>
        public const float AnchoAyudaControles = 520f;

        /// <summary>Ancho (px) de la interacción y el llamado, para no tocar las esquinas de abajo (1920 − 2 × 580).</summary>
        public const float AnchoCentroAbajo = 760f;
    }
}

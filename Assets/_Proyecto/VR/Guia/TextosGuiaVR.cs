using UnityEngine;

namespace MechaCyberpunk.VR.Guia
{
    /// <summary>
    /// VR20/VR31, datos de la guía de controles: las páginas (una imagen de Assets/Arte/UI/ControlesVR/Resources/GuiaVR cada una, en orden:
    /// botones → sacar pistola → arma larga → recargar → prótesis piernas → prótesis brazos), la frase de cada gesto y, en la página de los
    /// botones, cada etiqueta junto a su punto cian de la imagen (coordenadas normalizadas medidas en VR_Controles_Botones.png, 1376×768:
    /// x de izquierda a derecha, y de abajo arriba) con la tabla final de botones de VR27.
    /// </summary>
    public static class TextosGuiaVR
    {
        public const string Titulo = "CONTROLES VR";
        public const string Subtitulo = "META QUEST · TOUCH PLUS";
        public const string Cerrar = "CERRAR";
        public const string Siguiente = "SIGUIENTE";
        public const string Anterior = "ANTERIOR";
        public const string AyudaCerrar = "GATILLO SOBRE UN BOTÓN · STICK DER. ← → PÁGINA · B O Y CIERRA";
        /// <summary>El botón que se agrega a la pausa en VR.</summary>
        public const string BotonPausa = "CONTROLES";
        /// <summary>Carpeta dentro de Resources con las imágenes de la guía (las importa ImportarGuiaVR31).</summary>
        public const string CarpetaImagenes = "GuiaVR";

        public const string GestoPiernas = "MANOS EN LOS MUSLOS + LOS DOS GRIPS: PRÓTESIS DE PIERNAS";
        public const string GestoBrazos = "BRAZOS ABIERTOS + LOS DOS GRIPS: PRÓTESIS DE BRAZOS";

        /// <summary>Una página: la imagen (nombre en Resources/GuiaVR), el título y la frase (vacía en la de los botones, que lleva etiquetas).</summary>
        public static readonly (string imagen, string titulo, string frase)[] Paginas =
        {
            ("VR_Controles_Botones", "BOTONES", string.Empty),
            ("VR_Gesto_SacarPistola", "SACAR PISTOLA", "MANO DERECHA A LA CADERA + GRIP: SACAR PISTOLA"),
            ("VR_Gesto_ArmaLarga", "ARMA LARGA", "MANO DERECHA AL HOMBRO + GRIP: TOMAR EL ARMA LARGA"),
            ("VR_Gesto_Recargar", "RECARGAR", "A SUELTA EL CARGADOR · MANO IZQUIERDA A LA CADERA + GRIP: CARGADOR NUEVO"),
            ("VR_Gesto_ProtesisPiernas", "PRÓTESIS DE PIERNAS", GestoPiernas),
            ("VR_Gesto_ProtesisBrazos", "PRÓTESIS DE BRAZOS", GestoBrazos),
        };

        /// <summary>
        /// Las etiquetas de la página de botones: el punto cian (normalizado en la imagen), dónde va la etiqueta (normalizado; puede salir
        /// un poco de la imagen hacia el margen del panel), si la etiqueta crece hacia la derecha desde ahí y el texto (tabla final de VR27).
        /// Sin punto (punto negativo) la etiqueta va sola, sin línea.
        /// </summary>
        public static readonly (Vector2 punto, Vector2 etiqueta, bool derecha, string texto)[] Etiquetas =
        {
            // Control izquierdo: columna de la izquierda.
            (new Vector2(0.190f, 0.874f), new Vector2(0.165f, 0.874f), false, "STICK IZQ.: CAMINAR\nCLIC DEL STICK: CORRER"),
            (new Vector2(0.332f, 0.820f), new Vector2(0.165f, 0.720f), false, "X (MANTÉN): RUEDA DE ARMAS"),
            (new Vector2(0.460f, 0.607f), new Vector2(0.165f, 0.580f), false, "GRIP IZQ.: AGARRAR / LANZAR\nCON LA MANO LIBRE"),
            (new Vector2(-1f, -1f), new Vector2(0.165f, 0.430f), false, "GRIP + GATILLO IZQ.:\nRUEDA DE PRÓTESIS"),
            (new Vector2(0.393f, 0.903f), new Vector2(0.420f, 0.960f), true, "Y: PAUSA"),
            // Control derecho: columna de la derecha.
            (new Vector2(0.654f, 0.714f), new Vector2(0.775f, 0.800f), true, "B: INTERACTUAR"),
            (new Vector2(0.740f, 0.635f), new Vector2(0.775f, 0.660f), true, "STICK DER.: GIRAR\nCLIC DEL STICK: SALTAR\nABAJO 1 s: RECENTRAR"),
            (new Vector2(0.719f, 0.533f), new Vector2(0.775f, 0.530f), true, "A: SOLTAR CARGADOR (RECARGAR)"),
            (new Vector2(0.525f, 0.442f), new Vector2(0.775f, 0.400f), true, "GATILLO DER.: DISPARAR / ACEPTAR"),
            (new Vector2(0.690f, 0.374f), new Vector2(0.775f, 0.270f), true, "GRIP DER. EN CADERA U HOMBRO:\nSACAR ARMA"),
        };
    }
}

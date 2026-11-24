using UnityEngine;

namespace MechaCyberpunk.VR.Guia
{
    /// <summary>VR31b: las 16 páginas de la guía de controles, en orden. Las tarjetas del tutorial (VR62) usan el mismo número.</summary>
    public enum PaginaGuiaVR
    {
        Botones = 0,
        MoverseGirar = 1,
        Interactuar = 2,
        SacarPistola = 3,
        ArmaLarga = 4,
        Disparar = 5,
        Recargar = 6,
        RuedaArmas = 7,
        Granada = 8,
        Golpe = 9,
        AgarrarLanzar = 10,
        ProtesisPiernas = 11,
        ProtesisBrazos = 12,
        ProtesisVision = 13,
        Recentrar = 14,
        Manejar = 15
    }

    /// <summary>
    /// VR20/VR31/VR31b, datos de la guía de controles: las 16 páginas (una imagen de Assets/Arte/UI/ControlesVR/Resources/GuiaVR cada una, en el
    /// orden de <see cref="PaginaGuiaVR"/>), la frase corta de cada gesto (estilo HUD, cian), una explicación de 1–2 líneas para las tarjetas del
    /// tutorial (VR62) y, en la página de los botones, cada etiqueta junto a su punto cian de la imagen (coordenadas normalizadas medidas en
    /// VR_Controles_Botones.png, 1376×768: x de izquierda a derecha, y de abajo arriba). VR31b: ya no hay rueda de prótesis (las prótesis van
    /// solo con gestos); la rueda de armas es mantener X y mover la mano izquierda con el GRIP izquierdo apretado; al soltar el grip se elige.
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

        public const string GestoPiernas = "MANOS DE LA CADERA A LA RODILLA + LOS DOS GRIPS: PRÓTESIS DE PIERNAS";
        public const string GestoBrazos = "BRAZOS ABIERTOS EN CRUZ + LOS DOS GRIPS: PRÓTESIS DE BRAZOS";
        public const string GestoVision = "LAS DOS MANOS A LOS LADOS DE LA CABEZA + LOS DOS GRIPS: VISIÓN";
        public const string GestoRuedaArmas = "MANTÉN X · GRIP IZQ. Y MUEVE LA MANO · SUELTA EL GRIP: ELIGE";

        /// <summary>
        /// Una página: la imagen (nombre en Resources/GuiaVR), el título, la frase (vacía en la de los botones, que lleva etiquetas) y la
        /// explicación corta de la tarjeta del tutorial.
        /// </summary>
        public static readonly (string imagen, string titulo, string frase, string explicacion)[] Paginas =
        {
            ("VR_Controles_Botones", "BOTONES", string.Empty,
                "Todos los botones de los dos controles."),
            ("VR_Gesto_MoverseGirar", "MOVERSE Y GIRAR", "STICK IZQ.: CAMINAR · STICK DER.: GIRAR",
                "Caminas hacia donde miras. Clic del stick izquierdo: correr. Clic del stick derecho: saltar."),
            ("VR_Gesto_Interactuar", "INTERACTUAR", "B: HABLAR, USAR Y SUBIR",
                "Acércate a una persona, una máquina o un carro y aprieta B. B o Y cierran las pantallas."),
            ("VR_Gesto_SacarPistola_v2", "SACAR PISTOLA", "MANO DERECHA A LA CADERA + GRIP: SACAR PISTOLA",
                "Lleva la mano derecha a tu cadera derecha y aprieta el grip. Hazlo otra vez para guardarla."),
            ("VR_Gesto_ArmaLarga_v2", "ARMA LARGA", "MANO DERECHA SOBRE EL HOMBRO + GRIP: ARMA LARGA",
                "Toma el arma larga de tu espalda con el grip derecho. Otra vez la guarda."),
            ("VR_Gesto_Disparar", "DISPARAR", "GATILLO DER.: DISPARAR",
                "Apunta con la mano: el arma sigue a tu control."),
            ("VR_Gesto_Recargar_v2", "RECARGAR", "A SUELTA EL CARGADOR · MANO IZQ. A LA CADERA + GRIP: CARGADOR NUEVO",
                "Acerca el cargador nuevo a la pistola y entra solo."),
            ("VR_Gesto_RuedaArmas", "RUEDA DE ARMAS", GestoRuedaArmas,
                "Con la rueda abierta, aprieta el grip izquierdo y mueve la mano: el arma resaltada se equipa al soltar el grip."),
            ("VR_Gesto_Granada", "GRANADA", "GRIP DER.: TOMAR LA GRANADA · SUELTA EL GRIP: LANZAR",
                "Elige la granada en la rueda, tómala con el grip y lánzala con el brazo."),
            ("VR_Gesto_Golpe", "GOLPE", TeclaVR.AyudaGolpes,
                "Pega con el puño desde cualquier ángulo. Con el grip sujetas a un enemigo y le pegas con la otra mano."),
            ("VR_Gesto_AgarrarLanzar", "AGARRAR Y LANZAR", "GRIP CERCA DEL ENEMIGO: AGARRAR · SUELTA CON FUERZA: LANZAR",
                "Con la prótesis de brazos levantas al enemigo y lo avientas al soltar el grip con fuerza."),
            ("VR_Gesto_ProtesisPiernas_v2", "PRÓTESIS DE PIERNAS", GestoPiernas,
                "Baja las dos manos entre la cadera y la rodilla y sostén los dos grips medio segundo. Corres y saltas más."),
            ("VR_Gesto_ProtesisBrazos_v2", "PRÓTESIS DE BRAZOS", GestoBrazos,
                "Abre los brazos a los lados, a la altura de los hombros, y sostén los dos grips medio segundo. Golpeas más fuerte."),
            ("VR_Gesto_ProtesisVision", "PRÓTESIS DE VISIÓN", GestoVision,
                "Lleva las dos manos a los lados de la cabeza y sostén los dos grips medio segundo. Ves a los enemigos tras las paredes."),
            ("VR_Gesto_Recentrar", "RECENTRAR", "MANTÉN EL STICK DER. ABAJO 1 S: RECENTRAR",
                "Si la vista quedó chueca o a otra altura, mira al frente y mantén el stick derecho abajo."),
            ("VR_Gesto_Manejar", "MANEJAR", "GATILLO DER.: ACELERAR · GATILLO IZQ.: FRENAR · STICK IZQ.: DIRIGIR",
                "Sube con B. A es el freno de mano; B te baja del carro."),
        };

        /// <summary>La página de una tarjeta.</summary>
        public static (string imagen, string titulo, string frase, string explicacion) De(PaginaGuiaVR pagina) => Paginas[(int)pagina];

        /// <summary>
        /// Las etiquetas de la página de botones: el punto cian (normalizado en la imagen), dónde va la etiqueta (normalizado; puede salir
        /// un poco de la imagen hacia el margen del panel), si la etiqueta crece hacia la derecha desde ahí y el texto (tabla final de VR27 con
        /// los cambios de VR31b). Sin punto (punto negativo) la etiqueta va sola, sin línea.
        /// </summary>
        public static readonly (Vector2 punto, Vector2 etiqueta, bool derecha, string texto)[] Etiquetas =
        {
            // Control izquierdo: columna de la izquierda.
            (new Vector2(0.190f, 0.874f), new Vector2(0.165f, 0.874f), false, "STICK IZQ.: CAMINAR\nCLIC DEL STICK: CORRER"),
            (new Vector2(0.332f, 0.820f), new Vector2(0.165f, 0.720f), false, "X (MANTÉN) + GRIP IZQ.:\nRUEDA DE ARMAS"),
            (new Vector2(0.460f, 0.607f), new Vector2(0.165f, 0.580f), false, TeclaVR.AyudaGolpes.Replace(" · ", " ·\n") + " / LANZAR"),
            (new Vector2(-1f, -1f), new Vector2(0.165f, 0.430f), false, "PRÓTESIS: GESTOS\nCON LOS DOS GRIPS"),
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

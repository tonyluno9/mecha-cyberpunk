using UnityEngine;

namespace MechaCyberpunk.VR.Guia
{
    /// <summary>
    /// VR20, datos de la guía de controles: el reparto de botones de VR19 y los gestos de las prótesis, y dónde está cada botón en el
    /// modelo de los Starter Assets del XR Interaction Toolkit (XR Controller Left/Right → UniversalController).
    /// Nombres de las piezas del modelo: ThumbStick, Button_A (A/X), Button_B (B/Y), Trigger, Bumper (grip), Button_Home (menú).
    /// Si un modelo no las tiene, se usa <see cref="Respaldo"/>: un punto fijo en el marco del control (m; +z al frente, +y arriba).
    /// </summary>
    public static class TextosGuiaVR
    {
        public const string Titulo = "CONTROLES VR";
        public const string Subtitulo = "META QUEST · TOUCH PLUS";
        public const string Izquierdo = "CONTROL IZQUIERDO";
        public const string Derecho = "CONTROL DERECHO";
        public const string TituloGestos = "GESTOS DE PRÓTESIS (CON LA PRÓTESIS INSTALADA)";
        public const string GestoPiernas = "MANOS EN LOS MUSLOS + LOS DOS GRIPS 0.5 s: PRÓTESIS DE PIERNAS";
        public const string GestoBrazos = "BRAZOS ABIERTOS A LOS LADOS + LOS DOS GRIPS 0.5 s: PRÓTESIS DE BRAZOS";
        public const string Cerrar = "CERRAR";
        public const string AyudaCerrar = "B O GATILLO SOBRE CERRAR";
        /// <summary>El botón que se agrega a la pausa en VR.</summary>
        public const string BotonPausa = "CONTROLES";

        /// <summary>Las etiquetas de los dos controles (VR19).</summary>
        public static readonly (bool derecha, BotonControlVR boton, string texto)[] Etiquetas =
        {
            (false, BotonControlVR.Stick, "STICK: CAMINAR · CLIC: CORRER"),
            (false, BotonControlVR.Inferior, "X: RECARGAR"),
            (false, BotonControlVR.Superior, "Y (MANTÉN): RUEDA DE ARMAS · ELIGE CON LA MANO O EL STICK"),
            (false, BotonControlVR.Gatillo, "GATILLO + GRIP: RUEDA DE PRÓTESIS"),
            (false, BotonControlVR.Grip, "GRIP: AGARRAR / LANZAR"),
            (false, BotonControlVR.Menu, "MENÚ: PAUSA / OPCIONES"),
            (true, BotonControlVR.Stick, "STICK: GIRAR · ABAJO 1 s: RECENTRAR"),
            (true, BotonControlVR.Inferior, "A: SALTAR"),
            (true, BotonControlVR.Superior, "B: INTERACTUAR"),
            (true, BotonControlVR.Gatillo, "GATILLO: DISPARAR"),
            (true, BotonControlVR.Grip, "GRIP: AGARRAR / LANZAR"),
        };

        /// <summary>Los nombres que puede tener cada pieza en el modelo (se busca sin mayúsculas, el primero que exista).</summary>
        public static string[] Nombres(BotonControlVR boton, bool derecha)
        {
            switch (boton)
            {
                case BotonControlVR.Stick: return new[] { "ThumbStick", "Thumbstick", "Joystick" };
                case BotonControlVR.Inferior: return derecha ? new[] { "Button_A", "ButtonA", "A" } : new[] { "Button_X", "Button_A", "ButtonX", "X" };
                case BotonControlVR.Superior: return derecha ? new[] { "Button_B", "ButtonB", "B" } : new[] { "Button_Y", "Button_B", "ButtonY", "Y" };
                case BotonControlVR.Gatillo: return new[] { "Trigger", "Gatillo" };
                case BotonControlVR.Grip: return new[] { "Bumper", "Grip" };
                default: return new[] { "Button_Home", "Menu", "Button_Menu" };
            }
        }

        /// <summary>Punto fijo de la pieza en el marco del control (m), si el modelo no trae la pieza con nombre.</summary>
        public static Vector3 Respaldo(BotonControlVR boton, bool derecha)
        {
            float lado = derecha ? -1f : 1f;
            switch (boton)
            {
                case BotonControlVR.Stick: return new Vector3(-0.009f * lado, 0.012f, 0.0f);
                case BotonControlVR.Inferior: return new Vector3(0.008f * lado, 0.01f, -0.005f);
                case BotonControlVR.Superior: return new Vector3(0.012f * lado, 0.01f, 0.009f);
                case BotonControlVR.Gatillo: return new Vector3(0f, -0.013f, 0.07f);
                case BotonControlVR.Grip: return new Vector3(0.0125f * lado, -0.028f, 0.023f);
                default: return new Vector3(0f, 0.008f, 0.025f);
            }
        }
    }
}

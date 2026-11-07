using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Las teclas que muestra el HUD (T-700) salen de las acciones reales del Input System con GetBindingDisplayString, nunca de textos fijos.
    /// <see cref="Nombre"/> y <see cref="Texto"/> son lógica pura; <see cref="Buscar"/> encuentra la acción cargada del jugador por su nombre.
    /// </summary>
    public static class TeclasHud
    {
        private const string MapaAPie = "APie";

        /// <summary>El nombre que se ve para lo que devuelve el Input System: "LMB" se ve "Clic", "Space" "Espacio".</summary>
        public static string Nombre(string display)
        {
            if (string.IsNullOrEmpty(display))
            {
                return string.Empty;
            }
            switch (display)
            {
                case "LMB":
                case "Left Button":
                    return "Clic";
                case "RMB":
                case "Right Button":
                    return "Clic der.";
                case "Space":
                    return "Espacio";
                case "Left Shift":
                case "Shift":
                    return "Shift";
                default:
                    return display;
            }
        }

        /// <summary>
        /// La tecla o botón de teclado y ratón de la acción, ya con su nombre visible. Si la acción no existe (todavía) devuelve
        /// <paramref name="respaldo"/>; si no tiene tecla ni ratón, el texto que dé el Input System.
        /// </summary>
        public static string Texto(InputAction accion, string respaldo = "")
        {
            if (accion == null)
            {
                return respaldo;
            }
            for (int i = 0; i < accion.bindings.Count; i++)
            {
                InputBinding binding = accion.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite || string.IsNullOrEmpty(binding.path))
                {
                    continue;
                }
                if (binding.path.StartsWith("<Keyboard>", System.StringComparison.Ordinal)
                    || binding.path.StartsWith("<Mouse>", System.StringComparison.Ordinal))
                {
                    return Nombre(accion.GetBindingDisplayString(i));
                }
            }
            string general = accion.GetBindingDisplayString();
            return string.IsNullOrEmpty(general) ? respaldo : Nombre(general);
        }

        /// <summary>La acción <paramref name="nombre"/> del mapa "APie" que cargó algún objeto de la escena (los componentes del jugador la referencian), o null.</summary>
        public static InputAction Buscar(string nombre)
        {
            InputActionReference[] todas = Resources.FindObjectsOfTypeAll<InputActionReference>();
            for (int i = 0; i < todas.Length; i++)
            {
                InputAction accion = todas[i] != null ? todas[i].action : null;
                if (accion != null && accion.name == nombre && accion.actionMap != null && accion.actionMap.name == MapaAPie)
                {
                    return accion;
                }
            }
            return null;
        }
    }
}

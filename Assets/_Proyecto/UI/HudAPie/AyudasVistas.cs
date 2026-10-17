using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>
    /// Qué ayudas de controles ya se mostraron en esta sesión de juego (T-134). Es estático para sobrevivir al cambio de escena
    /// (por ejemplo, al volver del flashback no reaparecen) y se limpia al entrar a Play.
    /// </summary>
    public static class AyudasVistas
    {
        private static readonly HashSet<ContextoAyuda> vistas = new HashSet<ContextoAyuda>();

        /// <summary>True si esa ayuda ya se mostró en la sesión.</summary>
        public static bool YaVista(ContextoAyuda contexto) => vistas.Contains(contexto);

        /// <summary>Anota que la ayuda ya se mostró.</summary>
        public static void Marcar(ContextoAyuda contexto) => vistas.Add(contexto);

        /// <summary>Olvida todo lo visto. Para pruebas y para limpiar al entrar a Play.</summary>
        public static void Olvidar() => vistas.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Olvidar();
    }
}

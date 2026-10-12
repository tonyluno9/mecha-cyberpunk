using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Lleva la cuenta de todos los <see cref="Registro{T}"/> que se han usado, para vaciarlos juntos.
    /// </summary>
    public static class Registros
    {
        private static readonly List<Action> limpiadores = new List<Action>();

        /// <summary>Lo llama el constructor estático de cada <see cref="Registro{T}"/>. No lo uses a mano.</summary>
        public static void Apuntar(Action limpiar)
        {
            if (limpiar != null) limpiadores.Add(limpiar);
        }

        /// <summary>
        /// Vacía todos los registros usados y quita sus suscriptores. Corre sola al entrar a Play
        /// (el proyecto no recarga el dominio). Úsala también en SetUp y TearDown de las pruebas.
        /// </summary>
        public static void LimpiarTodos()
        {
            for (int i = 0; i < limpiadores.Count; i++) limpiadores[i]();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => LimpiarTodos();
    }
}

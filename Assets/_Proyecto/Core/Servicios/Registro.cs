using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Lista global de quienes implementan un contrato <typeparamref name="T"/> (por ejemplo
    /// <c>Registro&lt;IArenaPelea&gt;</c>). Así un área encuentra a otra sin referenciarla.
    /// Contrato de uso: los MonoBehaviour se agregan en <c>OnEnable</c> y se quitan en <c>OnDisable</c>.
    /// Quien busca lo hace en <c>Start</c> o después (no en <c>Awake</c>, porque el otro puede no haberse
    /// activado todavía), o se suscribe a <see cref="Agregado"/> si lo que busca puede llegar después.
    /// Se vacía sola al entrar a Play (ver <see cref="Registros.LimpiarTodos"/>).
    /// </summary>
    public static class Registro<T> where T : class
    {
        private static readonly List<T> elementos = new List<T>();

        static Registro() => Registros.Apuntar(Limpiar);

        /// <summary>Quienes están registrados, en orden de llegada. No la modifiques: usa Agregar y Quitar.</summary>
        public static IReadOnlyList<T> Todos => elementos;

        /// <summary>El último que se agregó y sigue registrado, o null si no hay ninguno.</summary>
        public static T Actual => elementos.Count > 0 ? elementos[elementos.Count - 1] : null;

        /// <summary>Cuántos hay registrados.</summary>
        public static int Cantidad => elementos.Count;

        /// <summary>Avisa una vez por cada elemento realmente agregado, con ese elemento.</summary>
        public static event Action<T> Agregado;

        /// <summary>Avisa una vez por cada elemento realmente quitado, con ese elemento.</summary>
        public static event Action<T> Quitado;

        /// <summary>Registra el elemento. Ignora null y los que ya estaban (no avisa en esos casos).</summary>
        public static void Agregar(T elemento)
        {
            if (elemento == null || elementos.Contains(elemento)) return;
            elementos.Add(elemento);
            Agregado?.Invoke(elemento);
        }

        /// <summary>Quita el elemento. Si no estaba registrado no hace nada y no avisa.</summary>
        public static void Quitar(T elemento)
        {
            if (elemento == null || !elementos.Remove(elemento)) return;
            Quitado?.Invoke(elemento);
        }

        private static void Limpiar()
        {
            elementos.Clear();
            Agregado = null;
            Quitado = null;
        }
    }
}

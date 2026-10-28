using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>
    /// Regla pura del SISTEMA NERVIOSO (T-194, D-tony-165 y D-tony-171g): sobre los valores ya compuestos (serie + piezas), el
    /// multiplicador de reacción acorta la preparación del golpe y el tiempo de subir la guardia, y alarga la ventana del bloqueo
    /// perfecto. Nada más cambia (vida, daño, alcance, velocidad, giro, recarga y bloqueo quedan iguales).
    /// </summary>
    public static class ReglaReaccionImplantes
    {
        /// <summary>La preparación del golpe no baja de esto por los implantes (s).</summary>
        public const float PreparacionMinima = 0.15f;

        /// <summary>El tiempo de subir la guardia no baja de esto por los implantes (s).</summary>
        public const float GuardiaMinima = 0.08f;

        /// <summary>La ventana del bloqueo perfecto no pasa de esto por los implantes (s).</summary>
        public const float VentanaMaxima = 0.3f;

        /// <summary>
        /// Aplica el multiplicador. Un multiplicador de 1 o menos de 0 (o NaN) no cambia nada. Si un valor ya estaba más allá del
        /// piso o del tope (por las piezas), los implantes no lo empeoran.
        /// </summary>
        public static ValoresMecha Aplicar(ValoresMecha valores, float multiplicador)
        {
            if (float.IsNaN(multiplicador) || multiplicador <= 0f || Mathf.Approximately(multiplicador, 1f))
            {
                return valores;
            }
            ValoresMecha v = valores;
            v.preparacionGolpe = Acortar(valores.preparacionGolpe, multiplicador, PreparacionMinima);
            v.tiempoSubirGuardia = Acortar(valores.tiempoSubirGuardia, multiplicador, GuardiaMinima);
            float ventana = valores.ventanaBloqueoPerfecto / multiplicador;
            v.ventanaBloqueoPerfecto = ventana > valores.ventanaBloqueoPerfecto
                ? Mathf.Min(ventana, Mathf.Max(VentanaMaxima, valores.ventanaBloqueoPerfecto))
                : ventana;
            return v;
        }

        // Multiplica con piso; si el valor ya estaba bajo el piso, no lo sube.
        private static float Acortar(float valor, float multiplicador, float piso)
        {
            float nuevo = valor * multiplicador;
            return nuevo < valor ? Mathf.Max(nuevo, Mathf.Min(valor, piso)) : nuevo;
        }
    }
}

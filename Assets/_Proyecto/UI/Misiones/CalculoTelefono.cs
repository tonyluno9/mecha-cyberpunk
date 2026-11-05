using UnityEngine;

namespace MechaCyberpunk.UI.Misiones
{
    /// <summary>Cuentas puras del teléfono (T-633): la curva del deslizamiento y la hora en texto.</summary>
    public static class CalculoTelefono
    {
        /// <summary>De 0 a 1 con salida suave (frena al llegar): cuánto del recorrido va hecho a un avance lineal de 0 a 1.</summary>
        public static float Deslizamiento(float avance)
        {
            float t = Mathf.Clamp01(avance);
            float resto = 1f - t;
            return 1f - resto * resto * resto;
        }

        /// <summary>La hora del juego (0 a 24) como HH:MM de 24 horas; cualquier valor se acomoda a un día.</summary>
        public static string FormatoHora(float horas)
        {
            float dia = horas % 24f;
            if (dia < 0f)
            {
                dia += 24f;
            }
            int minutos = Mathf.FloorToInt(dia * 60f);
            return (minutos / 60 % 24).ToString("00") + ":" + (minutos % 60).ToString("00");
        }
    }
}

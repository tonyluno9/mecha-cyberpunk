using UnityEngine;

namespace MechaCyberpunk.Mecha.Danio
{
    /// <summary>Reglas puras del mecha dañado: cuándo la vida es baja, cuánto se reducen sus valores y cuándo la alarma está prendida.</summary>
    public static class ReglaMechaDaniado
    {
        /// <summary>True si 0 &lt; actual / maxima &lt;= umbral. En KO (actual 0) es false: es otro caso.</summary>
        public static bool VidaBaja(float actual, float maxima, float umbral)
        {
            if (maxima <= 0f || actual <= 0f)
            {
                return false;
            }
            return actual / maxima <= umbral;
        }

        /// <summary>Nivel de grietas del cristal: 0 sin grietas, 1 leves (fracción &lt;= umbral1), 2 fuertes (&lt;= umbral2). En KO es 2.</summary>
        public static int NivelGrietas(float actual, float maxima, float umbral1, float umbral2)
        {
            if (maxima <= 0f)
            {
                return 0;
            }
            float fraccion = actual / maxima;
            if (actual <= 0f || fraccion <= umbral2)
            {
                return 2;
            }
            return fraccion <= umbral1 ? 1 : 0;
        }

        /// <summary>El factor con vida baja; 1 si no.</summary>
        public static float Factor(bool vidaBaja, float factor)
        {
            return vidaBaja ? factor : 1f;
        }

        /// <summary>Parpadeo: la primera mitad de cada periodo está prendida y la segunda apagada.</summary>
        public static bool AlarmaPrendida(float segundos, float frecuencia)
        {
            if (frecuencia <= 0f)
            {
                return false;
            }
            float fase = Mathf.Repeat(segundos * frecuencia, 1f);
            return fase < 0.5f;
        }
    }
}

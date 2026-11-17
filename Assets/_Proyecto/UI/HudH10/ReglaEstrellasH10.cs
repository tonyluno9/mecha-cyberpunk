using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>Lógica pura de las 5 estrellas de búsqueda del HUD (T-700).</summary>
    public static class ReglaEstrellasH10
    {
        public const int Total = 5;
        public const float SegundosParpadeo = 0.4f;
        public const float AlfaApagada = 0.3f;

        /// <summary>
        /// Cuántas estrellas se ven llenas: ninguna sin calor; con calor, tantas como el nivel o como la fracción del calor en quintos, la mayor
        /// (el nivel 1 siempre muestra una).
        /// </summary>
        public static int Llenas(int nivel, float valor, float maximo) => MechaCyberpunk.Core.EstrellasBusqueda.De(nivel, valor, maximo); // T-1304: la regla vive en Core (Nova la usa)

        /// <summary>T-1304: con 4 o 5 estrellas viene Nova y las estrellas se pintan de azul intenso.</summary>
        public static readonly Color AzulNova = new Color(0.12f, 0.38f, 1f);

        /// <summary>T-1304: color de las estrellas: turquesa normal; azul intenso desde las estrellas de Nova.</summary>
        public static Color ColorEstrellas(int llenas, Color normal) => MechaCyberpunk.Core.EstrellasBusqueda.EsNova(llenas) ? AzulNova : normal;

        /// <summary>
        /// T-729: segundos seguidos sin que te vea la policía a partir de los cuales las estrellas parpadean (te están por perder). La policía te da por escapado a los
        /// 45 s (ConfigPersecucion.SegundosParaEscapar) y avisa los últimos 8 (ReglaBusquedaPolicial.SegundosAviso): 45 - 8 = 37. UI no ve Trafico; una prueba cuida que coincidan.
        /// </summary>
        public const float SegundosSinVerParaParpadear = 37f;

        /// <summary>T-729: las estrellas solo parpadean cuando te buscan y ya llevas rato sin que te vean (la policía está por perderte).</summary>
        public static bool DebeParpadear(bool buscando, bool teVen, float segundosSinVer)
        {
            return buscando && !teVen && segundosSinVer >= SegundosSinVerParaParpadear;
        }

        /// <summary>Opacidad de las estrellas llenas: parpadean mientras te buscan, fijas si no.</summary>
        public static float Alfa(float tiempo, bool parpadea)
        {
            if (!parpadea)
            {
                return 1f;
            }
            return Mathf.FloorToInt(tiempo / SegundosParpadeo) % 2 == 0 ? 1f : AlfaApagada;
        }
    }
}

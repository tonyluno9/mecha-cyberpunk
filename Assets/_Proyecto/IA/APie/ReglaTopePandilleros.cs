using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1495: tope de pandilleros vivos a la vez. Un grupo no pone más de <see cref="PorPunto"/> en pie, y entre todos los grupos no
    /// hay más de <see cref="EnZona"/> dentro de <see cref="RadioZona"/> metros. Pura, sin escena. Los grupos de misión no la usan.
    /// </summary>
    public static class ReglaTopePandilleros
    {
        public const int PorPunto = 5;
        public const int EnZona = 8;
        public const float RadioZona = 40f;

        /// <summary>Cuántos más pueden aparecer, dados los vivos del grupo y los vivos de todos los grupos cerca (incluye los del grupo).</summary>
        public static int Cupo(int vivosDelGrupo, int vivosEnZona)
        {
            return Mathf.Max(0, Mathf.Min(PorPunto - vivosDelGrupo, EnZona - vivosEnZona));
        }
    }
}

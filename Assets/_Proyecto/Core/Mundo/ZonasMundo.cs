using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Core.Mundo
{
    /// <summary>T-1409: pregunta en qué zona de la ciudad cae un punto, leyendo las <see cref="IZonaDelMundo"/> registradas.</summary>
    public static class ZonasMundo
    {
        /// <summary>El nombre de la primera zona registrada que contiene el punto, o null si no cae en ninguna.</summary>
        public static string NombreEn(Vector3 mundo)
        {
            IReadOnlyList<IZonaDelMundo> zonas = Registro<IZonaDelMundo>.Todos;
            for (int i = 0; i < zonas.Count; i++)
            {
                if (zonas[i].ContienePunto(mundo))
                {
                    return zonas[i].NombreZona;
                }
            }
            return null;
        }
    }
}

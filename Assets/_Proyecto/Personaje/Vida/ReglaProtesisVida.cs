using System.Collections.Generic;
using MechaCyberpunk.Economia;

namespace MechaCyberpunk.Personaje.Vida
{
    /// <summary>T-1529: suma la vida máxima extra de las prótesis de ataque que lleva el jugador (el inventario solo lista lo que tiene).</summary>
    public static class ReglaProtesisVida
    {
        public static float Sumar(IReadOnlyList<DatosObjeto> llevados)
        {
            float total = 0f;
            for (int i = 0; llevados != null && i < llevados.Count; i++)
            {
                if (llevados[i] is DatosProtesisAtaque p)
                {
                    total += p.VidaMaximaExtra;
                }
            }
            return total;
        }
    }
}

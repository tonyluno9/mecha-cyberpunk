using MechaCyberpunk.Core;

namespace MechaCyberpunk.Economia
{
    /// <summary>Texto del dinero en Biits: "Ƀ500", "Ƀ1,250", "-Ƀ150". Sin decimales.</summary>
    public static class FormatoDinero
    {
        public static string Texto(int dinero)
        {
            return Moneda.Texto(dinero);
        }
    }
}

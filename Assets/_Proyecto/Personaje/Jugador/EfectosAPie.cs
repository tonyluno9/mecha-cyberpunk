using MechaCyberpunk.Core;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>Cálculo puro de lo que cambian los implantes a pie (T-192): correr, salto y golpe. Caminar y la pistola no cambian.</summary>
    public static class EfectosAPie
    {
        public static float VelocidadCorrer(DatosPersonaje datos, EfectosImplantes efectos) => datos.VelocidadCorrer * efectos.multiplicadorCorrer;

        public static float AlturaSalto(DatosPersonaje datos, EfectosImplantes efectos) => datos.AlturaSalto * efectos.multiplicadorSalto;

        public static float DanioGolpe(DatosPersonaje datos, EfectosImplantes efectos) => datos.DanioGolpe * efectos.multiplicadorGolpeAPie;
    }
}

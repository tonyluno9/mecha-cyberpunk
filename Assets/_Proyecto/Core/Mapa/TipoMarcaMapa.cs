namespace MechaCyberpunk.Core
{
    /// <summary>Qué es una marca del minimapa y del mapa grande (T-409).</summary>
    public enum TipoMarcaMapa
    {
        Enemigo,
        Jefe,
        Mercado,
        Clinica,
        Cic,
        Casa,
        /// <summary>T-1706: policía a pie (punto azul). Va al final para no mover los valores guardados.</summary>
        Policia,
        /// <summary>T-1706: guardia o robot de Nova (punto violeta).</summary>
        Nova,
    }
}

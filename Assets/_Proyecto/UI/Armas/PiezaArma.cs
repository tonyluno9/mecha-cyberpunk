namespace MechaCyberpunk.UI.Armas
{
    /// <summary>
    /// Las piezas que se mejoran en el banco de Tino (T-814). Cada una es un nivel del arma, en este orden fijo: el cargador da el nivel I,
    /// el cañón el II y la mira el III (las estadísticas salen por nivel del catálogo, así que no se mejoran sueltas).
    /// </summary>
    public enum PiezaArma
    {
        Cargador,
        Canon,
        Mira
    }
}

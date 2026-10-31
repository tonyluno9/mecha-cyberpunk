namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Marca de lo que recibe daño pero no es una persona (T-444): un carro, una barrera. Quien dispara lo usa para no mostrar sangre ni
    /// sonar a carne cuando la bala le pega. Va junto a <see cref="IRecibeDanio"/>.
    /// </summary>
    public interface IObjetoInerte
    {
    }
}

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Todo lo que puede recibir daño.
    /// Contrato: si quien recibe ya está en KO o <c>info.Cantidad &lt;= 0</c>, devuelve
    /// <see cref="ResultadoDanio.Ignorado"/> y no publica nada. En cualquier otro caso aplica el daño
    /// (o lo bloquea) y después publica <see cref="EventosPelea.PublicarImpacto"/> con el resultado,
    /// aunque un bloqueo perfecto lo haya dejado en 0.
    /// </summary>
    public interface IRecibeDanio
    {
        ResultadoDanio RecibirDanio(InfoDanio info);
    }
}

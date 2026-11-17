namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1325: algo que se destroza de un golpe cuando una explosión lo alcanza (un carro): la granada y el carro vecino lo detonan con este contrato,
    /// sin que Personaje y Vehiculos se referencien. <paramref name="retraso"/> son los segundos hasta que revienta (0 = al instante; en la
    /// cadena se usa un retraso corto). Detonar algo ya detonado no hace nada.
    /// </summary>
    public interface IDetonable
    {
        void Detonar(float retraso);
    }
}
